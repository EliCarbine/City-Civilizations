// Citizen Soul -- FamilySystem (M5a): family & dynasty layer (F05-F07, F09, F10, F60).
// Runs on the same 128-lane / 2048-day cadence as RomanceSystem:
//  - links fresh souls (age <= FamilyLinkWindowDays) to a married household pair
//    as their parents; otherwise settles them as lineage founders (generation 1).
//  - detects widowhood from the Widowed beat RomanceSystem writes on partner loss.
//  - runs affair temptation -> discovery -> rupture beats on cold marriages.
//  - flags co-resident grandparents and gives them grandchild time.
using System;
using Game.Citizens;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public class FamilySystem : SoulSystemBase
    {
        private const int FamilyLanes = 128;
        private const int MigrateBudget = 64;
        private const int HouseholdScan = 16;

        private EntityQuery m_Souls;
        private EntityQuery m_All;
        private EntityQuery m_Dynasty;

        protected override int Lanes => FamilyLanes;
        protected override int Interval => 2048;

        protected override void OnCreate()
        {
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulFamily>());

            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());

            m_Dynasty = GetEntityQuery(ComponentType.ReadWrite<SoulDynasty>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_Dynasty.CalculateEntityCount() == 0)
                {
                    Entity reg = em.CreateEntity();
                    em.AddComponentData(reg, SoulDynasty.Fresh());
                    SoulLog.Once("family.registry", "FamilySystem: dynasty registry created.");
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("family.registry", ex);
                return;
            }

            Entity dyn = m_Dynasty.GetSingletonEntity();
            SoulDynasty d = em.GetComponentData<SoulDynasty>(dyn);
            if (d.m_StartDay == uint.MaxValue)
            {
                d.m_StartDay = day;
                d.m_LastDay = day;
                em.SetComponentData(dyn, d);
            }

            // Budgeted migration: souls gain family state on first visit.
            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulFamily>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulFamily.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("family.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("family.migrate", "FamilySystem: migration pass failed: " + ex.Message, 60000);
            }

            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            for (int i = 0; i < souls.Length; i++)
            {
                Entity e = souls[i];
                if (!Matches(e, lane, FamilyLanes)) continue;
                try
                {
                    Advance(em, e, day, ref d);
                }
                catch (Exception ex)
                {
                    faults++;
                    if (faults <= 4) SoulLog.Exception("family.fault." + e.Index, ex);
                }
            }
            souls.Dispose();

            d.m_LastDay = day;
            em.SetComponentData(dyn, d);
        }

        private static void Advance(EntityManager em, Entity e, uint day, ref SoulDynasty d)
        {
            if (!em.HasComponent<SoulFamily>(e)) return;
            SoulFamily f = em.GetComponentData<SoulFamily>(e);
            if (f.m_LastDay == day) return;
            f.m_LastDay = day;

            // A. settle the record: link to household parents, or seed a founder lineage.
            if ((f.m_Flags & SoulFamilyFlags.Resolved) == 0)
            {
                SoulMeta meta = em.GetComponentData<SoulMeta>(e);
                Resolve(em, e, meta, day, ref f, ref d);
            }

            // B. widowhood: surface the Widowed beat RomanceSystem writes on partner loss.
            if ((f.m_Flags & SoulFamilyFlags.Resolved) != 0 && (f.m_Flags & SoulFamilyFlags.Widowed) == 0)
            {
                ScanWidow(em, e, ref f);
            }

            // C. marriage beats: affairs, discovery, cold divorce.
            SoulRomance r = default(SoulRomance);
            if (TryMarried(em, e, out r))
            {
                TryMarriageBeats(em, e, day, ref f, ref r);
            }
            else
            {
                f.m_Affair = Entity.Null;
                f.m_Flags = (byte)(f.m_Flags & (0xFF ^ SoulFamilyFlags.Affair) & (0xFF ^ SoulFamilyFlags.AffairFound));
            }

            // D. grandparent time: an occasional mood beat while flagged.
            if ((f.m_Flags & SoulFamilyFlags.Grandparent) != 0 &&
                Roll(day, e.Index, 1000) < SoulTune.GrandHelpPerMille)
            {
                SoulFeel.Pulse(em, e, SoulTune.GrandHelpMood);
            }

            em.SetComponentData(e, f);
        }

        private static void Resolve(EntityManager em, Entity e, SoulMeta meta, uint day, ref SoulFamily f, ref SoulDynasty d)
        {
            bool trackable = d.m_StartDay != uint.MaxValue && meta.m_BirthDay > d.m_StartDay + (uint)SoulTune.FamilyStartGrace;
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0;

            if (trackable && age <= (uint)SoulTune.FamilyLinkWindowDays)
            {
                bool linked = false;
                SoulRomance r = default(SoulRomance);
                if (!(em.HasComponent<SoulRomance>(e) && TryMarried(em, e, out r)))
                {
                    linked = TryLinkParents(em, e, day, ref f, ref d);
                }
                if (linked) return;
            }

            if (age > (uint)SoulTune.FamilyLinkGiveUpDays || !trackable)
            {
                SeedFounder(ref f, ref d);
            }
        }

        private static void SeedFounder(ref SoulFamily f, ref SoulDynasty d)
        {
            if (f.m_Generation == 0) f.m_Generation = 1;
            if (f.m_Lineage == 0)
            {
                d.m_NextLineage++;
                f.m_Lineage = d.m_NextLineage;
                if (d.m_Lineages < d.m_NextLineage) d.m_Lineages = d.m_NextLineage;
            }
            f.m_Flags |= SoulFamilyFlags.Resolved;
        }

        private static bool TryLinkParents(EntityManager em, Entity e, uint day, ref SoulFamily f, ref SoulDynasty d)
        {
            if (f.m_ParentA != Entity.Null || f.m_ParentB != Entity.Null) return false;
            if (!SoulVanilla.TryGetHome(em, e, out Entity home)) return false;
            if (!em.HasBuffer<HouseholdCitizen>(home)) return false;

            DynamicBuffer<HouseholdCitizen> members = em.GetBuffer<HouseholdCitizen>(home);
            int count = members.Length < HouseholdScan ? members.Length : HouseholdScan;
            Entity a = Entity.Null;
            Entity b = Entity.Null;
            for (int i = 0; i < count && a == Entity.Null; i++)
            {
                Entity mi = members[i].m_Citizen;
                if (mi == e || !em.Exists(mi)) continue;
                if (!em.HasComponent<SoulRomance>(mi)) continue;
                SoulRomance ri = em.GetComponentData<SoulRomance>(mi);
                Entity partner = ri.m_Partner;
                if (partner == Entity.Null || !em.Exists(partner)) continue;
                for (int j = i + 1; j < count; j++)
                {
                    if (members[j].m_Citizen == partner && em.HasComponent<SoulFamily>(partner))
                    {
                        a = mi;
                        b = partner;
                        break;
                    }
                }
            }
            if (a == Entity.Null || b == Entity.Null) return false;

            SoulFamily fa = em.GetComponentData<SoulFamily>(a);
            SoulFamily fb = em.GetComponentData<SoulFamily>(b);
            SeedFounder(ref fa, ref d);
            SeedFounder(ref fb, ref d);
            em.SetComponentData(a, fa);
            em.SetComponentData(b, fb);

            f.m_ParentA = a;
            f.m_ParentB = b;
            f.m_Lineage = fa.m_Lineage;
            int gen = (fa.m_Generation > fb.m_Generation ? fa.m_Generation : fb.m_Generation) + 1;
            f.m_Generation = (byte)gen;
            f.m_Flags |= SoulFamilyFlags.Resolved;

            SoulFeel.Note(em, a, day, SoulEventKind.BecameParent, 10, e);
            SoulFeel.Pulse(em, a, SoulTune.ParentMood);
            SoulFeel.Note(em, b, day, SoulEventKind.BecameParent, 10, e);
            SoulFeel.Pulse(em, b, SoulTune.ParentMood);

            TryFlagGrandparent(em, fa.m_ParentA, e, day, home);
            TryFlagGrandparent(em, fa.m_ParentB, e, day, home);
            TryFlagGrandparent(em, fb.m_ParentA, e, day, home);
            TryFlagGrandparent(em, fb.m_ParentB, e, day, home);
            return true;
        }

        private static void TryFlagGrandparent(EntityManager em, Entity g, Entity child, uint day, Entity home)
        {
            if (g == Entity.Null || g == child || !em.Exists(g)) return;
            if (!em.HasComponent<SoulFamily>(g)) return;
            if (!SoulVanilla.TryGetHome(em, g, out Entity gh) || gh != home) return;
            SoulFamily gf = em.GetComponentData<SoulFamily>(g);
            if ((gf.m_Flags & SoulFamilyFlags.Grandparent) != 0) return;
            gf.m_Flags |= SoulFamilyFlags.Grandparent;
            em.SetComponentData(g, gf);
            SoulFeel.Note(em, g, day, SoulEventKind.Grandfamily, 6, child);
            SoulFeel.Pulse(em, g, SoulTune.GrandHelpMood);
        }

        private static void ScanWidow(EntityManager em, Entity e, ref SoulFamily f)
        {
            if (em.HasBuffer<SoulMemory>(e))
            {
                DynamicBuffer<SoulMemory> ring = em.GetBuffer<SoulMemory>(e);
                for (int i = 0; i < ring.Length; i++)
                {
                    if (ring[i].m_Kind == SoulEventKind.Widowed)
                    {
                        f.m_Flags |= SoulFamilyFlags.Widowed;
                        return;
                    }
                }
            }
            if (em.HasBuffer<SoulJournalEntry>(e))
            {
                DynamicBuffer<SoulJournalEntry> journal = em.GetBuffer<SoulJournalEntry>(e);
                for (int i = 0; i < journal.Length; i++)
                {
                    if (journal[i].m_Kind == SoulEventKind.Widowed)
                    {
                        f.m_Flags |= SoulFamilyFlags.Widowed;
                        return;
                    }
                }
            }
        }

        private static bool TryMarried(EntityManager em, Entity e, out SoulRomance r)
        {
            r = default(SoulRomance);
            if (!em.HasComponent<SoulRomance>(e)) return false;
            r = em.GetComponentData<SoulRomance>(e);
            return r.m_Partner != Entity.Null;
        }

        private static void TryMarriageBeats(EntityManager em, Entity e, uint day, ref SoulFamily f, ref SoulRomance r)
        {
            if ((f.m_Flags & SoulFamilyFlags.Affair) != 0 && (f.m_Flags & SoulFamilyFlags.AffairFound) == 0)
            {
                if (Roll(day, e.Index, 1000) < SoulTune.AffairDiscoverPerMille)
                {
                    DiscoverAffair(em, e, day, ref f, ref r);
                    return;
                }
            }
            else if ((f.m_Flags & SoulFamilyFlags.Affair) == 0)
            {
                if (r.m_Intimacy <= SoulTune.AffairColdIntimacy &&
                    Roll(day, e.Index + 3, 1000) < SoulTune.AffairTryPerMille)
                {
                    StartAffair(em, e, day, ref f, ref r);
                }
            }

            if (TryMarried(em, e, out r))
            {
                uint since = (uint)r.m_SinceDay;
                if (day >= since && day - since >= (uint)SoulTune.DivorceMinDays &&
                    r.m_Intimacy <= SoulTune.DivorceColdIntimacy &&
                    Roll(day, e.Index + 7, 1000) < SoulTune.DivorceColdPerMille)
                {
                    Rupture(em, e, day, ref f);
                }
            }
        }

        private static void StartAffair(EntityManager em, Entity e, uint day, ref SoulFamily f, ref SoulRomance r)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            Entity best = Entity.Null;
            int bestAff = SoulTune.AffairMinAffinity - 1;
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge x = edges[i];
                Entity o = x.m_Other;
                if (o == Entity.Null || o == e || o == r.m_Partner) continue;
                if (!em.Exists(o)) continue;
                if (o == f.m_ParentA || o == f.m_ParentB) continue;
                if (x.m_Affinity < SoulTune.AffairMinAffinity) continue;
                if (em.HasComponent<SoulFamily>(o))
                {
                    SoulFamily of = em.GetComponentData<SoulFamily>(o);
                    if (of.m_ParentA == e || of.m_ParentB == e) continue;
                    if (f.m_ParentA != Entity.Null &&
                        (of.m_ParentA == f.m_ParentA || of.m_ParentB == f.m_ParentB)) continue;
                }
                if (x.m_Affinity > bestAff)
                {
                    bestAff = x.m_Affinity;
                    best = o;
                }
            }
            if (best == Entity.Null) return;

            f.m_Affair = best;
            f.m_Flags = (byte)(f.m_Flags & (0xFF ^ SoulFamilyFlags.AffairFound));
            f.m_Flags |= SoulFamilyFlags.Affair;

            if (em.HasComponent<SoulFamily>(best))
            {
                SoulFamily of = em.GetComponentData<SoulFamily>(best);
                of.m_Affair = e;
                of.m_Flags = (byte)(of.m_Flags & (0xFF ^ SoulFamilyFlags.AffairFound));
                of.m_Flags |= SoulFamilyFlags.Affair;
                em.SetComponentData(best, of);
            }

            SoulFeel.Note(em, e, day, SoulEventKind.AffairStarted, 0, best);
            SoulFeel.Pulse(em, e, SoulTune.AffairThrillMood);
        }

        private static void DiscoverAffair(EntityManager em, Entity e, uint day, ref SoulFamily f, ref SoulRomance r)
        {
            Entity lover = f.m_Affair;
            Entity spouse = r.m_Partner;
            f.m_Flags |= SoulFamilyFlags.AffairFound;
            f.m_Affair = Entity.Null;

            if (em.Exists(lover) && em.HasComponent<SoulFamily>(lover))
            {
                SoulFamily lf = em.GetComponentData<SoulFamily>(lover);
                lf.m_Affair = Entity.Null;
                lf.m_Flags |= SoulFamilyFlags.AffairFound;
                lf.m_Flags = (byte)(lf.m_Flags & (0xFF ^ SoulFamilyFlags.Affair));
                em.SetComponentData(lover, lf);
                Rupture(em, lover, day, ref lf);
            }

            if (em.Exists(spouse))
            {
                SoulFeel.Note(em, spouse, day, SoulEventKind.AffairDiscovered, -20, e);
                SoulFeel.Pulse(em, spouse, SoulTune.AffairSpouseMood);
            }
            SoulFeel.Note(em, e, day, SoulEventKind.AffairDiscovered, -6, spouse);
            SoulFeel.Pulse(em, e, SoulTune.AffairGuiltMood);

            Rupture(em, e, day, ref f);
        }

        private static void Rupture(EntityManager em, Entity e, uint day, ref SoulFamily f)
        {
            if (!em.HasComponent<SoulRomance>(e)) return;
            SoulRomance r = em.GetComponentData<SoulRomance>(e);
            Entity p = r.m_Partner;
            if (p == Entity.Null) return;

            em.SetComponentData(e, SoulRomance.Fresh(day));

            if (em.Exists(p) && em.HasComponent<SoulRomance>(p))
            {
                em.SetComponentData(p, SoulRomance.Fresh(day));
                if (em.HasComponent<SoulFamily>(p))
                {
                    SoulFamily fp = em.GetComponentData<SoulFamily>(p);
                    fp.m_Affair = Entity.Null;
                    fp.m_Flags |= SoulFamilyFlags.Divorced;
                    fp.m_Flags = (byte)(fp.m_Flags & (0xFF ^ SoulFamilyFlags.Affair) & (0xFF ^ SoulFamilyFlags.AffairFound));
                    em.SetComponentData(p, fp);
                    SoulFeel.Note(em, p, day, SoulEventKind.Divorced, -10, e);
                    SoulFeel.Pulse(em, p, SoulTune.DivorceMood);
                }
            }

            f.m_Affair = Entity.Null;
            f.m_Flags |= SoulFamilyFlags.Divorced;
            f.m_Flags = (byte)(f.m_Flags & (0xFF ^ SoulFamilyFlags.Affair) & (0xFF ^ SoulFamilyFlags.AffairFound));
            SoulFeel.Note(em, e, day, SoulEventKind.Divorced, -10, p);
            SoulFeel.Pulse(em, e, SoulTune.DivorceMood);
        }

        private static uint Roll(uint day, int index, uint span)
        {
            uint h = (uint)index * 2654435761u;
            h ^= day * 2246822519u;
            h ^= h >> 15;
            h *= 2654435761u;
            h ^= h >> 13;
            return h % span;
        }
    }
}
