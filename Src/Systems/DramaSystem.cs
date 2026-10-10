// ---------------------------------------------------------------------------
// DramaSystem - M2 adjuncts (features F08, F26, F42, F52, F53, F55): the
// slow-burn social texture that runs beside the romance arc.
//
//   F08 firsts      - the first true-blue friend, the first crush; forever.
//   F26 workplace   - work spouses form on their own; toxic coworkers sour rooms.
//   F42 birthdays   - every soul has a date; friends (mostly) remember; parties
//                     are sized by personality.
//   F52 confidences - close friends confide; a rare leak is a catastrophe.
//   F53 feuds       - sustained hostility becomes a feud; old feuds can mend.
//   F55 wingmen     - friends run set-ups and pep talks; busybodies matchmake.
//
// One full pass per soul per game day, on that soul's lane, guarded by
// SoulFirsts.m_LastDay. Souls from older saves are migrated lazily, a small
// budget per pass, so a loaded city never pays a one-frame hitch.
//
// All "chance" is a deterministic hash of (day, citizen): stable across
// reloads, allocation-free, and safe to run on the main thread.
// ---------------------------------------------------------------------------
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>The M2 adjunct drama lane: firsts, workplace, birthdays, confidences, feuds, wingmen.</summary>
    public sealed class DramaSystem : SoulSystemBase
    {
        private const int MigrateBudget = 16;

        private EntityQuery m_Souls;
        private EntityQuery m_Unmigrated;

        protected override int Lanes { get { return SoulConsts.DramaLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulMeta>(),
                    ComponentType.ReadOnly<SoulFirsts>()
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
                    ComponentType.ReadOnly<SoulFirsts>()
                }
            });
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane))
            {
                return;
            }

            Migrate(day);

            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    if (!Matches(souls[i], lane, Lanes))
                    {
                        continue;
                    }
                    try
                    {
                        Advance(souls[i], day);
                    }
                    catch (System.Exception)
                    {
                        SoulLog.Throttled("drama.fault", "DramaSystem skipped a faulted citizen this pass.", 10000);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
        }

        // Souls that existed before M2 carry no SoulFirsts yet. Hand them one
        // on the day they are first visited, budgeted so a loaded city never
        // hitches.
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
                        em.AddComponentData(pending[i], SoulFirsts.Fresh(day));
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
            SoulFirsts f = em.GetComponentData<SoulFirsts>(e);
            if (f.m_LastDay == day)
            {
                return; // this soul already lived today
            }
            f.m_LastDay = day;

            PassFirsts(em, e, ref f, day);
            PassWorkPlace(em, e, day);
            PassBirthday(em, e, day);
            PassConfidant(em, e, day);
            PassFeud(em, e, day);
            PassWingman(em, e, day);

            em.SetComponentData(e, f);
        }

        // F08 - the moments you only get once: the first true-blue friend and
        // the first spark of romance, each celebrated exactly once.
        private static void PassFirsts(EntityManager em, Entity e, ref SoulFirsts f, uint day)
        {
            if ((f.m_Bits & SoulFirstsFlags.CloseFriend) == 0)
            {
                Entity friend;
                int affinity;
                if (BestEdge(em, e, out friend, out affinity)
                    && affinity >= SoulConsts.CloseAffinity
                    && Mutual(em, friend, e))
                {
                    f.m_Bits |= SoulFirstsFlags.CloseFriend;
                    SoulFeel.Pulse(em, e, SoulTune.FirstCloseMood);
                    SoulFeel.Note(em, e, day, SoulEventKind.FirstCloseFriend, (short)SoulTune.FirstCloseMood, friend);
                }
            }

            if ((f.m_Bits & SoulFirstsFlags.Crush) == 0)
            {
                SoulRomance rom;
                if (TryReadRom(em, e, out rom) && rom.m_Stage >= 1 && rom.m_Partner != Entity.Null)
                {
                    f.m_Bits |= SoulFirstsFlags.Crush;
                    SoulFeel.Pulse(em, e, SoulTune.FirstCrushMood);
                    SoulFeel.Note(em, e, day, SoulEventKind.FirstCrush, (short)SoulTune.FirstCrushMood, rom.m_Partner);
                }
            }
        }

        // F26 - the workplace web: work spouses form on their own; one toxic
        // coworker can sour the whole room.
        private static void PassWorkPlace(EntityManager em, Entity e, uint day)
        {
            Entity work;
            if (!SoulVanilla.TryGetWorkplace(em, e, out work))
            {
                return;
            }

            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            Entity spouse = Entity.Null;
            int bestAff = int.MinValue;
            int toxic = 0;
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if ((ed.m_Flags & SoulEdgeFlags.Colleague) == 0)
                {
                    continue;
                }
                if (ed.m_Affinity >= SoulConsts.CloseAffinity
                    && ed.m_Moments >= SoulTune.WorkSpouseMinMoments
                    && ed.m_Affinity > bestAff)
                {
                    spouse = ed.m_Other;
                    bestAff = ed.m_Affinity;
                }
                if (ed.m_Affinity <= SoulTune.ToxicityAffinity)
                {
                    toxic++;
                }
            }

            if (spouse != Entity.Null && Roll(day, e.Index, 1000u) < (uint)SoulTune.WorkSpouseGainPerMille)
            {
                SoulFeel.Pulse(em, e, SoulTune.WorkSpouseMood);
                SoulFeel.Note(em, e, day, SoulEventKind.WorkSpouse, (short)SoulTune.WorkSpouseMood, spouse);
            }

            if (toxic > 0 && Roll(day, e.Index + 3, 1000u) < (uint)(SoulTune.ToxicMoodChancePerMille * toxic))
            {
                SoulFeel.Pulse(em, e, SoulTune.ToxicMood);
            }
        }

        // F42 - birthdays: friends (mostly) remember; forgetting a close
        // friend's date is a real ding, and the parties come in two sizes.
        private static void PassBirthday(EntityManager em, Entity e, uint day)
        {
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            if (meta.m_BirthDay == 0u || day < meta.m_BirthDay)
            {
                return;
            }
            if ((day - meta.m_BirthDay) % (uint)SoulTune.BirthdayDays != 0u)
            {
                return;
            }

            uint partyChance = (uint)SoulTune.BirthdayPartyPerMille;
            SoulPersonality sp;
            if (TryReadPersonality(em, e, out sp) && sp.m_Extraversion >= 160)
            {
                partyChance *= 2u; // extraverts throw the bigger bashes
            }

            if (Roll(day, e.Index, 1000u) >= partyChance)
            {
                SoulFeel.Pulse(em, e, SoulTune.BirthdayMoodQuiet);
                SoulFeel.Note(em, e, day, SoulEventKind.Birthday, (short)SoulTune.BirthdayMoodQuiet, e);
                return;
            }

            SoulFeel.Pulse(em, e, SoulTune.BirthdayMoodSelf);
            SoulFeel.Note(em, e, day, SoulEventKind.BirthdayParty, (short)SoulTune.BirthdayMoodSelf, e);

            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            int invited = 0;
            for (int i = 0; i < edges.Length && invited < SoulTune.PartyGuestCap; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other == Entity.Null)
                {
                    continue;
                }
                if (ed.m_Affinity < SoulConsts.FriendAffinity && (ed.m_Flags & SoulEdgeFlags.Family) == 0)
                {
                    continue;
                }
                invited++;
                if (Roll(day, e.Index + 31 + i, 1000u) < (uint)SoulTune.PartyAttendPerMille)
                {
                    SoulFeel.Pulse(em, ed.m_Other, SoulTune.BirthdayMoodGuest);
                    SoulFeel.Note(em, ed.m_Other, day, SoulEventKind.BirthdayParty, (short)SoulTune.BirthdayMoodGuest, e);
                }
                else if ((ed.m_Flags & SoulEdgeFlags.Close) != 0)
                {
                    // you forgot their birthday, and it showed
                    SoulFeel.Pulse(em, ed.m_Other, SoulTune.BirthdayForgetMood);
                    SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(ed.m_Other), e, SoulTune.BirthdayForgetDrag, day, 0, false);
                }
            }
        }

        // F52 - confidences: a trust act between close souls, held for life...
        // mostly. A leak breaks the trust both ways and can seed a feud.
        private static void PassConfidant(EntityManager em, Entity e, uint day)
        {
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if ((ed.m_Flags & SoulEdgeFlags.Close) == 0 || ed.m_Affinity < SoulConsts.CloseAffinity)
                {
                    continue;
                }

                if ((ed.m_Flags & SoulEdgeFlags.Confidant) == 0)
                {
                    if (Mutual(em, ed.m_Other, e)
                        && Roll(day, e.Index + i, 1000u) < (uint)SoulTune.ConfideChancePerMille)
                    {
                        ed.m_Flags |= SoulEdgeFlags.Confidant;
                        edges[i] = ed;
                        SoulEdgeUtil.Touch(edges, ed.m_Other, SoulTune.ConfideDelta, day, 0, false);
                        SoulFeel.Pulse(em, e, SoulTune.ConfideMood);
                        SoulFeel.Note(em, e, day, SoulEventKind.Confided, (short)SoulTune.ConfideMood, ed.m_Other);
                        return; // one trust act a day is plenty
                    }
                }
                else if (Roll(day, e.Index + 100 + i, 1000u) < (uint)SoulTune.LeakChancePerMille)
                {
                    // the confidence gets out: trust breaks both ways
                    ed.m_Flags = (byte)(ed.m_Flags & ~SoulEdgeFlags.Confidant);
                    edges[i] = ed;
                    SoulEdgeUtil.Touch(edges, ed.m_Other, SoulTune.LeakDelta, day, 0, false);
                    SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(ed.m_Other), e, SoulTune.LeakDelta, day, 0, false);
                    SoulFeel.Pulse(em, e, SoulTune.LeakMood);
                    SoulFeel.Pulse(em, ed.m_Other, SoulTune.LeakMood);
                    return;
                }
            }
        }

        // F53 - feuds: sustained hostility curdles into a feud; clashes keep it
        // hot; mutual friends and time can mend it.
        private static void PassFeud(EntityManager em, Entity e, uint day)
        {
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                bool feuding = (ed.m_Flags & SoulEdgeFlags.Feud) != 0;
                if (!feuding)
                {
                    if (ed.m_Other != Entity.Null
                        && ed.m_Affinity <= SoulTune.FeudFloor
                        && ed.m_Moments >= SoulTune.FeudMinMoments)
                    {
                        ed.m_Flags |= SoulEdgeFlags.Feud;
                        edges[i] = ed;
                        SoulFeel.Pulse(em, e, SoulTune.FeudMood);
                        SoulFeel.Note(em, e, day, SoulEventKind.FeudStarted, (short)SoulTune.FeudMood, ed.m_Other);
                    }
                    continue;
                }

                if (Roll(day, e.Index + i, 1000u) < (uint)SoulTune.FeudClashNotePerMille)
                {
                    SoulFeel.Pulse(em, e, SoulTune.FeudClashMood);
                    SoulFeel.Note(em, e, day, SoulEventKind.FeudClash, (short)SoulTune.FeudClashMood, ed.m_Other);
                    SoulEdgeUtil.Touch(edges, ed.m_Other, -SoulTune.FeudClashDrag, day, 0, false);

                    // badmouth the rival to a friend (thankfully rare)
                    if (Roll(day, e.Index + 500 + i, 4u) == 0u)
                    {
                        for (int j = 0; j < edges.Length; j++)
                        {
                            SoulEdge audience = edges[j];
                            if (audience.m_Other == Entity.Null || audience.m_Other == ed.m_Other)
                            {
                                continue;
                            }
                            if (audience.m_Affinity < SoulConsts.FriendAffinity)
                            {
                                continue;
                            }
                            if ((audience.m_Flags & SoulEdgeFlags.Family) != 0)
                            {
                                continue; // you don't badmouth family
                            }
                            SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(audience.m_Other), ed.m_Other, -SoulTune.FeudClashDrag, day, 0, false);
                            break;
                        }
                    }
                }

                // reconciliation: mutual friends triple the odds (mediation)
                uint reconcileChance = (uint)SoulTune.ReconcileChancePerMille;
                if (Mutual(em, ed.m_Other, e))
                {
                    reconcileChance *= 3u;
                }
                if (Roll(day, e.Index + 900 + i, 1000u) < reconcileChance)
                {
                    ed.m_Flags = (byte)(ed.m_Flags & ~SoulEdgeFlags.Feud);
                    edges[i] = ed;
                    SoulEdgeUtil.Touch(edges, ed.m_Other, SoulTune.ReconcileLift, day, 0, false);
                    SoulFeel.Pulse(em, e, SoulTune.ReconcileMood);
                    SoulFeel.Note(em, e, day, SoulEventKind.Reconciled, (short)SoulTune.ReconcileMood, ed.m_Other);
                }
            }
        }

        // F55 - wingmen & matchmakers: friends talk you up while you're sweet
        // on someone; busybodies set their friends up.
        private static void PassWingman(EntityManager em, Entity e, uint day)
        {
            SoulRomance rom;
            if (!TryReadRom(em, e, out rom))
            {
                return;
            }

            if (rom.m_Stage == 1 || rom.m_Stage == 2)
            {
                if (rom.m_Partner == Entity.Null
                    || Roll(day, e.Index + 40, 1000u) >= (uint)SoulTune.WingmanChancePerMille)
                {
                    return;
                }
                DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
                for (int j = 0; j < edges.Length; j++)
                {
                    SoulEdge cand = edges[j];
                    if (cand.m_Other == Entity.Null || cand.m_Other == rom.m_Partner)
                    {
                        continue;
                    }
                    if (cand.m_Affinity < SoulConsts.FriendAffinity || !Mutual(em, cand.m_Other, e))
                    {
                        continue;
                    }
                    // a pep talk: your friend lifts your game
                    SoulFeel.Pulse(em, e, SoulTune.WingmanMood);
                    SoulEdgeUtil.Touch(edges, rom.m_Partner, SoulTune.WingmanBump, day, 0, false);
                    SoulFeel.Note(em, e, day, SoulEventKind.WingmanAssist, (short)SoulTune.WingmanMood, cand.m_Other);
                    return;
                }
                return;
            }

            if (rom.m_Stage != 0)
            {
                return;
            }

            SoulPersonality sp;
            if (!TryReadPersonality(em, e, out sp) || sp.m_Extraversion < SoulTune.MatchmakerExtraversion)
            {
                return;
            }
            if (Roll(day, e.Index + 70, 1000u) >= (uint)SoulTune.WingmanChancePerMille)
            {
                return;
            }

            DynamicBuffer<SoulEdge> fan = em.GetBuffer<SoulEdge>(e);
            Entity a = Entity.Null;
            int ai = -1;
            for (int j = 0; j < fan.Length; j++)
            {
                if (fan[j].m_Affinity >= SoulConsts.FriendAffinity && (fan[j].m_Flags & SoulEdgeFlags.Family) == 0)
                {
                    a = fan[j].m_Other;
                    ai = j;
                    break;
                }
            }
            if (a == Entity.Null)
            {
                return;
            }
            Entity b = Entity.Null;
            for (int j = ai + 1; j < fan.Length; j++)
            {
                if (fan[j].m_Other != a
                    && fan[j].m_Affinity >= SoulConsts.FriendAffinity
                    && (fan[j].m_Flags & SoulEdgeFlags.Family) == 0)
                {
                    b = fan[j].m_Other;
                    break;
                }
            }
            if (b == Entity.Null)
            {
                return;
            }

            // a set-up: sparks both ways; gifted or chaotic, the city decides
            SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(a), b, SoulTune.WingmanBump, day, 0, false);
            SoulEdgeUtil.Touch(em.GetBuffer<SoulEdge>(b), a, SoulTune.WingmanBump, day, 0, false);
            SoulFeel.Pulse(em, e, SoulTune.WingmanMood);
            SoulFeel.Note(em, e, day, SoulEventKind.WingmanAssist, (short)SoulTune.WingmanMood, a);
        }

        // The soul's strongest tie, whoever it is.
        private static bool BestEdge(EntityManager em, Entity e, out Entity other, out int affinity)
        {
            other = Entity.Null;
            affinity = int.MinValue;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other == Entity.Null)
                {
                    continue;
                }
                if (ed.m_Affinity > affinity)
                {
                    affinity = ed.m_Affinity;
                    other = ed.m_Other;
                }
            }
            return other != Entity.Null;
        }

        // Does the other side hold the same tie back?
        private static bool Mutual(EntityManager em, Entity other, Entity e)
        {
            return SoulEdgeUtil.Find(em.GetBuffer<SoulEdge>(other), e) >= 0;
        }

        private static bool TryReadRom(EntityManager em, Entity e, out SoulRomance rom)
        {
            try
            {
                rom = em.GetComponentData<SoulRomance>(e);
                return true;
            }
            catch (System.Exception)
            {
                rom = default(SoulRomance);
                return false;
            }
        }

        private static bool TryReadPersonality(EntityManager em, Entity e, out SoulPersonality sp)
        {
            try
            {
                sp = em.GetComponentData<SoulPersonality>(e);
                return true;
            }
            catch (System.Exception)
            {
                sp = default(SoulPersonality);
                return false;
            }
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
