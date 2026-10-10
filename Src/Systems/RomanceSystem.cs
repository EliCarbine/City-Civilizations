// ---------------------------------------------------------------------------
// RomanceSystem - M2 (features F01-F04): the romance arc.
//
//   F01 crush      - a soul quietly falls for their closest mutual friend.
//   F02 courtship  - the crush may deepen into courtship while the bond holds.
//   F03 bond       - courtship becomes engagement, then marriage.
//   F04 heartbreak - a bond cut short wounds both; a partner's passing leaves
//                    the survivor widowed.
//
// One full pass per soul per game day, on that soul's lane. Both partners are
// always written in the same frame, so the arc never half-advances. Souls
// from a save made before M2 are migrated lazily, a small budget per pass, so
// a loaded city never pays a one-frame hitch.
//
// All "chance" is a deterministic hash of (day, citizen): stable across
// reloads, allocation-free, and safe to run on the main thread.
// ---------------------------------------------------------------------------
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>One heart, one arc: F01 crush, F02 courtship, F03 bond, F04 heartbreak.</summary>
    public sealed class RomanceSystem : SoulSystemBase
    {
        private const int RomanceLanes = 128;
        private const int MigrateBudget = 64;

        private EntityQuery m_Souls;
        private EntityQuery m_Unmigrated;

        protected override int Lanes
        {
            get { return RomanceLanes; }
        }

        protected override int Interval
        {
            get { return 2048; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulMeta>(),
                    ComponentType.ReadOnly<SoulRomance>()
                }
            });

            m_Unmigrated = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulMeta>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulRomance>()
                }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day;
            int lane;
            AdvanceDay(out day, out lane);

            Migrate(day);

            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, RomanceLanes))
                    {
                        continue;
                    }
                    try
                    {
                        Advance(e, day);
                    }
                    catch (System.Exception ex)
                    {
                        // One citizen's fault never stops the city's day.
                        SoulLog.Once("romance.fault." + e.Index, "Romance fault for citizen " + e.Index + ": " + ex.Message);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
        }

        // Souls that existed before M2 (or were born between save load and the
        // first pass) carry no SoulRomance yet. Hand them one on the day they
        // are first visited, budgeted so a loaded city never hitches.
        private void Migrate(uint day)
        {
            NativeArray<Entity> pending = m_Unmigrated.ToEntityArray(Allocator.Temp);
            try
            {
                if (pending.Length == 0)
                {
                    return;
                }
                EntityManager em = EntityManager;
                int budget = pending.Length < MigrateBudget ? pending.Length : MigrateBudget;
                for (int i = 0; i < budget; i++)
                {
                    try
                    {
                        em.AddComponentData(pending[i], SoulRomance.Fresh(day));
                    }
                    catch (System.Exception)
                    {
                        // The citizen vanished between the query and the add.
                    }
                }
            }
            finally
            {
                pending.Dispose();
            }
        }

        private void Advance(Entity e, uint day)
        {
            EntityManager em = EntityManager;
            SoulRomance r = em.GetComponentData<SoulRomance>(e);
            if (r.m_LastDay == day)
            {
                return; // this soul already lived today
            }
            r.m_LastDay = day;

            if (r.m_Partner != Entity.Null)
            {
                AdvanceBond(e, em, ref r, day);
            }
            else if (r.m_Stage == 0)
            {
                TryCrush(e, em, ref r, day);
            }
            else
            {
                AdvanceCrush(e, em, ref r, day);
            }

            em.SetComponentData(e, r);
        }

        // F01 - love finds the closest friend, on its own quiet schedule.
        private static void TryCrush(Entity e, EntityManager em, ref SoulRomance r, uint day)
        {
            Entity friend;
            int affinity;
            if (!TopEdge(e, em, out friend, out affinity) || affinity < SoulTune.CrushMinAffinity)
            {
                return;
            }
            if (!Mutual(em, friend, e))
            {
                return;
            }
            if (Roll(day, e.Index, 1000u) >= (uint)SoulTune.CrushChancePerMille)
            {
                return;
            }
            r.m_Stage = 1;
            r.m_SinceDay = day;
            r.m_Intimacy = (byte)SoulTune.IntimacyCrush;
            SoulFeel.Pulse(em, e, 4);
            SoulFeel.Note(em, e, day, SoulEventKind.CrushFormed, 4, friend);
        }

        // F02 - a crush becomes courtship while the bond holds; if the bond
        // breaks (drifted apart, or the friend moved on), the spark fades.
        private static void AdvanceCrush(Entity e, EntityManager em, ref SoulRomance r, uint day)
        {
            Entity friend;
            int affinity;
            if (!TopEdge(e, em, out friend, out affinity) || affinity < SoulTune.CrushMinAffinity || !Mutual(em, friend, e))
            {
                r.m_Stage = 0;
                r.m_SinceDay = 0;
                r.m_Intimacy = 0;
                return;
            }
            if ((day % 3u) != 0u)
            {
                return; // shared time grows on its own rhythm
            }
            r.m_Intimacy = Clamp255(r.m_Intimacy + SoulTune.IntimacyPerDay);

            if (r.m_Stage == 1 && r.m_Intimacy >= SoulTune.IntimacyCourt)
            {
                r.m_Stage = 2; // courting
                r.m_SinceDay = day;
                SoulFeel.Pulse(em, e, 5);
                SoulFeel.Pulse(em, friend, 5);
                SoulFeel.Note(em, e, day, SoulEventKind.CourtStarted, 5, friend);
                SoulFeel.Note(em, friend, day, SoulEventKind.CourtStarted, 5, e);
            }
            else if (r.m_Stage == 2 && r.m_Intimacy >= SoulTune.IntimacyEngage)
            {
                Engage(e, em, ref r, friend, day); // F03 begins
            }
        }

        // A coupled soul: keep the bond alive, or grieve it.
        private static void AdvanceBond(Entity e, EntityManager em, ref SoulRomance r, uint day)
        {
            SoulRomance pr;
            if (!TryRead(em, r.m_Partner, out pr))
            {
                EndBond(e, em, ref r, day, true); // F04 - the partner is gone
                return;
            }
            if (pr.m_Partner != e || pr.m_Stage < 3)
            {
                EndBond(e, em, ref r, day, false); // F04 - the bond came undone
                return;
            }
            if ((day % 3u) != 0u)
            {
                return;
            }
            r.m_Intimacy = Clamp255(r.m_Intimacy + SoulTune.IntimacyPerDay);

            if (r.m_Stage == 3 && r.m_Intimacy >= SoulTune.IntimacyWed)
            {
                r.m_Stage = 4; // F03 - rings on
                r.m_SinceDay = day;
                SoulFeel.Pulse(em, e, 6);
                SoulFeel.Pulse(em, r.m_Partner, 6);
                SoulFeel.Note(em, e, day, SoulEventKind.Married, 6, r.m_Partner);
                SoulFeel.Note(em, r.m_Partner, day, SoulEventKind.Married, 6, e);
            }
            else if (r.m_Stage == 4 && r.m_SinceDay != 0u && day - r.m_SinceDay >= (uint)SoulTune.AnniversaryDays)
            {
                r.m_SinceDay = day; // the year turns, together
                SoulFeel.Note(em, e, day, SoulEventKind.Anniversary, 5, r.m_Partner);
                SoulFeel.Note(em, r.m_Partner, day, SoulEventKind.Anniversary, 5, e);
            }

            pr.m_Stage = r.m_Stage;
            pr.m_SinceDay = r.m_SinceDay;
            pr.m_LastDay = day;
            pr.m_Intimacy = r.m_Intimacy;
            em.SetComponentData(r.m_Partner, pr);
        }

        // F03 - engagement binds both records in one frame.
        private static void Engage(Entity e, EntityManager em, ref SoulRomance r, Entity partner, uint day)
        {
            r.m_Stage = 3;
            r.m_Partner = partner;
            r.m_SinceDay = day;

            SoulRomance pr;
            if (TryRead(em, partner, out pr))
            {
                pr.m_Stage = 3;
                pr.m_Partner = e;
                pr.m_SinceDay = day;
                pr.m_LastDay = day;
                pr.m_Intimacy = r.m_Intimacy;
                em.SetComponentData(partner, pr);
            }

            SoulFeel.Pulse(em, e, 6);
            SoulFeel.Pulse(em, partner, 6);
            SoulFeel.Note(em, e, day, SoulEventKind.Engaged, 6, partner);
            SoulFeel.Note(em, partner, day, SoulEventKind.Engaged, 6, e);
        }

        // F04 - a bond ends: widowed when the partner is gone, heartbroken
        // when the bond itself came undone. Both partners grieve in the live
        // case; in the widowed case only the survivor remains to grieve.
        private static void EndBond(Entity e, EntityManager em, ref SoulRomance r, uint day, bool widowed)
        {
            Entity partner = r.m_Partner;
            r.m_Stage = 0;
            r.m_Partner = Entity.Null;
            r.m_SinceDay = 0;
            r.m_Intimacy = 0;

            SoulFeel.Pulse(em, e, -8);
            SoulFeel.Note(em, e, day, widowed ? SoulEventKind.Widowed : SoulEventKind.Heartbroken, -8, partner);

            if (widowed)
            {
                return;
            }
            SoulRomance pr;
            if (TryRead(em, partner, out pr) && pr.m_Partner == e)
            {
                pr.m_Stage = 0;
                pr.m_Partner = Entity.Null;
                pr.m_SinceDay = 0;
                pr.m_Intimacy = 0;
                pr.m_LastDay = day;
                em.SetComponentData(partner, pr);
                SoulFeel.Pulse(em, partner, -8);
                SoulFeel.Note(em, partner, day, SoulEventKind.Heartbroken, -8, e);
            }
        }

        // The soul's closest relationship, if any: highest affinity among the
        // ring of remembered edges. Read-only; no state is touched.
        private static bool TopEdge(Entity e, EntityManager em, out Entity friend, out int affinity)
        {
            friend = Entity.Null;
            affinity = 0;

            DynamicBuffer<SoulEdge> edges;
            try
            {
                edges = em.GetBuffer<SoulEdge>(e);
            }
            catch (System.Exception)
            {
                return false; // gone, or no relationships remembered
            }

            bool found = false;
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge edge = edges[i];
                int a = edge.m_Affinity;
                if (edge.m_Other != Entity.Null && a > affinity)
                {
                    affinity = a;
                    friend = edge.m_Other;
                    found = true;
                }
            }
            return found;
        }

        // True when the friend's own closest edge points back at this soul and
        // the friend is still unbound: love has to be a shared direction.
        private static bool Mutual(EntityManager em, Entity friend, Entity e)
        {
            SoulRomance fr;
            if (!TryRead(em, friend, out fr) || fr.m_Partner != Entity.Null)
            {
                return false;
            }
            Entity back;
            int backAffinity;
            return TopEdge(friend, em, out back, out backAffinity)
                && back == e
                && backAffinity >= SoulTune.CrushMinAffinity;
        }

        private static bool TryRead(EntityManager em, Entity e, out SoulRomance rom)
        {
            rom = default(SoulRomance);
            if (e == Entity.Null)
            {
                return false;
            }
            try
            {
                rom = em.GetComponentData<SoulRomance>(e);
                return true;
            }
            catch (System.Exception)
            {
                return false; // the citizen is gone
            }
        }

        private static byte Clamp255(int v)
        {
            return (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
        }

        // Deterministic, allocation-free hash of (day, citizen): the same
        // citizen gets the same roll on the same day in every session.
        private static uint Hash(uint day, int index)
        {
            uint h = day * 2654435761u + (uint)index * 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;
            return h;
        }

        private static uint Roll(uint day, int index, uint span)
        {
            return Hash(day + 977u, index) % span;
        }
    }
}
