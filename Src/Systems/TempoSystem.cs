// Citizen Soul -- TempoSystem (M5j-ii): voices and hours (F125-F126).
// Talkers hold the floor; listeners are the ones people tell things to;
// a talker and a listener may find each other before the coffee cools.
// And every soul keeps its own clock -- early birds, night owls,
// snoozers, the chronically late, and the punctual ones who wait.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F125-F126: every soul settles into a voice -- talker or listener,
    /// maybe half of a duo -- and into its own hours: how it sleeps, how
    /// often it keeps people waiting, and who waits for it in turn.
    /// </summary>
    public partial class TempoSystem : SoulSystemBase
    {
        private const int TempoLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return TempoLanes; } }

        private EntityQuery m_Tempo;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Tempo = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulTempo>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulTempo>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulTempo.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("tempo.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("tempo.migrate", "TempoSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Tempo.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Tempo.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, TempoLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("tempo.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("tempo.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulTempo>(e)) return;
            SoulTempo c = em.GetComponentData<SoulTempo>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte agreeableness = 50;
            byte voice = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                agreeableness = p.m_Agreeableness;
                voice = p.m_Extraversion;
            }

            // A. voices (F125): the loud earn a voice; the quiet and warm
            //    become the ones people tell things to.
            if (age >= (uint)SoulTune.TempoMinAgeDays &&
                (c.m_Flags & (SoulTempoFlags.Talker | SoulTempoFlags.Listener)) == 0 &&
                (voice >= SoulTune.TempoTalkGateMin ||
                 (voice <= SoulTune.TempoQuietGateMax && agreeableness >= SoulTune.TempoWarmGateMin) ||
                 Roll(day, salt + 191u, 100u) < 20u) &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 193u, 1000u) < (uint)SoulTune.TempoFormPerMille)
            {
                bool talker = voice >= SoulTune.TempoTalkGateMin ||
                    (voice > SoulTune.TempoQuietGateMax && Roll(day, salt + 197u, 2u) == 0u);
                if (talker)
                {
                    c.m_Flags |= SoulTempoFlags.Talker;
                    SoulFeel.Note(em, e, day, SoulEventKind.TalkOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TempoTalkMood);
                }
                else
                {
                    c.m_Flags |= SoulTempoFlags.Listener;
                    SoulFeel.Note(em, e, day, SoulEventKind.ListenOn, 1, e);
                    SoulFeel.Pulse(em, e, SoulTune.TempoListenMood);
                }
            }

            // B. duos (F125): a talker and a listener find each other; the
            //    pairing is the quiet point of it.
            if ((c.m_Flags & SoulTempoFlags.Duo) == 0 && (day + salt) % 45u == 0u &&
                Roll(day, salt + 199u, 1000u) < (uint)SoulTune.TempoDuoPerMille)
            {
                Entity pal = Entity.Null;
                if ((c.m_Flags & SoulTempoFlags.Talker) != 0)
                    pal = FindVoiced(em, e, SoulTune.CurioPartnerMinAffinity, SoulTempoFlags.Listener);
                else if ((c.m_Flags & SoulTempoFlags.Listener) != 0)
                    pal = FindVoiced(em, e, SoulTune.CurioPartnerMinAffinity, SoulTempoFlags.Talker);
                if (pal != Entity.Null) c = FormDuo(em, e, pal, day, c);
            }

            // C. kept truths (F125): people start telling the listener the
            //    quiet things; the tally is the point.
            if ((c.m_Flags & SoulTempoFlags.Listener) != 0 && (day + salt) % 30u == 0u &&
                Roll(day, salt + 223u, 1000u) < (uint)SoulTune.TempoConfidePerMille)
            {
                Entity pal = FindFriend(em, e, SoulTune.CurioPartnerMinAffinity);
                if (pal != Entity.Null)
                {
                    bool firstTruth = (c.m_Flags2 & SoulTempoFlags2.Confided) == 0;
                    c.m_Flags2 |= SoulTempoFlags2.Confided;
                    if (c.m_Confided < 255) c.m_Confided++;
                    BumpBoth(em, e, pal, SoulTune.TempoConfideBump);
                    SoulFeel.Pulse(em, e, SoulTune.TempoConfideMood);
                    if (firstTruth)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.ConfideOn, (short)c.m_Confided, pal);
                    }
                }
            }

            // D. hours (F126): every soul settles into a rhythm -- early
            //    bird, night owl, or the one mornings never met.
            if (age >= (uint)SoulTune.TempoMinAgeDays &&
                (c.m_Flags & (SoulTempoFlags.EarlyBird | SoulTempoFlags.NightOwl | SoulTempoFlags.Snoozer)) == 0 &&
                (day + salt) % 60u == 0u &&
                Roll(day, salt + 217u, 1000u) < (uint)SoulTune.TempoSleepFormPerMille)
            {
                byte rhythm;
                if (voice >= SoulTune.TempoTalkGateMin) rhythm = SoulTempoFlags.NightOwl;
                else if (agreeableness >= SoulTune.TempoWarmGateMin) rhythm = SoulTempoFlags.EarlyBird;
                else rhythm = SoulTempoFlags.Snoozer;
                c.m_Flags |= rhythm;
                SoulFeel.Note(em, e, day, SoulEventKind.SleepOn, rhythm, e);
                if (rhythm == SoulTempoFlags.NightOwl) SoulFeel.Pulse(em, e, SoulTune.TempoOwlMood);
                else if (rhythm == SoulTempoFlags.EarlyBird) SoulFeel.Pulse(em, e, SoulTune.TempoEarlyMood);
                else SoulFeel.Pulse(em, e, SoulTune.TempoSnoozeMood);
            }

            // E. lateness (F126): snoozers miss the clock the most; enough
            //    times and friends simply stop minding.
            if (age >= (uint)SoulTune.TempoMinAgeDays && (c.m_Flags & SoulTempoFlags.Punctual) == 0 &&
                (day + salt) % 45u == 0u)
            {
                uint miss = (c.m_Flags & SoulTempoFlags.Snoozer) != 0
                    ? (uint)SoulTune.TempoLateSnoozePerMille
                    : (uint)SoulTune.TempoLateOtherPerMille;
                if (Roll(day, salt + 227u, 1000u) < miss)
                {
                    if (c.m_Late < 255) c.m_Late++;
                    SoulFeel.Note(em, e, day, SoulEventKind.LateOn, (short)c.m_Late, e);
                    SoulFeel.Pulse(em, e, SoulTune.TempoLateMood);
                    if (c.m_Late >= (uint)SoulTune.TempoLatePeaceAt &&
                        (c.m_Flags2 & SoulTempoFlags2.LateAccounted) == 0)
                    {
                        c.m_Flags2 |= SoulTempoFlags2.LateAccounted;
                        SoulFeel.Note(em, e, day, SoulEventKind.LateAccounted, (short)c.m_Late, e);
                        SoulFeel.Pulse(em, e, SoulTune.TempoPeaceMood);
                    }
                }
            }

            // F. punctuality (F126): the ones who are never late -- and the
            //    wait they carry for everyone else.
            if ((c.m_Flags & SoulTempoFlags.Punctual) == 0 && c.m_Late == 0 &&
                age >= (uint)SoulTune.TempoMinAgeDays && (day + salt) % 45u == 0u &&
                Roll(day, salt + 229u, 1000u) < (uint)SoulTune.TempoPunctualPerMille)
            {
                c.m_Flags |= SoulTempoFlags.Punctual;
                SoulFeel.Note(em, e, day, SoulEventKind.PunctualOn, 1, e);
                SoulFeel.Pulse(em, e, SoulTune.TempoPunctualMood);
            }
            if ((c.m_Flags & SoulTempoFlags.Punctual) != 0 && (day + salt) % 30u == 0u &&
                Roll(day, salt + 233u, 1000u) < (uint)SoulTune.TempoWaitPerMille)
            {
                Entity late = FindLatecomer(em, e, SoulTune.CurioPartnerMinAffinity);
                if (late != Entity.Null)
                {
                    short count = 1;
                    if (em.HasComponent<SoulTempo>(late))
                        count = (short)em.GetComponentData<SoulTempo>(late).m_Late;
                    SoulFeel.Note(em, e, day, SoulEventKind.WaitOn, count, late);
                    SoulFeel.Pulse(em, e, SoulTune.TempoWaitMood);
                }
            }


            em.SetComponentData(e, c);
        }

        // ---- helpers --------------------------------------------------------
        private static Entity FindFriend(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.Exists(o)) continue;
                if (ed.m_Affinity < minAffinity) continue;
                return o;
            }
            return Entity.Null;
        }

        private static Entity FindVoiced(EntityManager em, Entity e, int minAffinity, byte want)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.Exists(o)) continue;
                if (ed.m_Affinity < minAffinity) continue;
                if (!em.HasComponent<SoulTempo>(o)) continue;
                SoulTempo t = em.GetComponentData<SoulTempo>(o);
                if ((t.m_Flags & want) == 0) continue;
                if ((t.m_Flags & SoulTempoFlags.Duo) != 0) continue;
                return o;
            }
            return Entity.Null;
        }

        private static Entity FindLatecomer(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.Exists(o)) continue;
                if (ed.m_Affinity < minAffinity) continue;
                if (!em.HasComponent<SoulTempo>(o)) continue;
                SoulTempo t = em.GetComponentData<SoulTempo>(o);
                if (t.m_Late == 0) continue;
                return o;
            }
            return Entity.Null;
        }

        private static SoulTempo FormDuo(EntityManager em, Entity e, Entity pal, uint day, SoulTempo c)
        {
            c.m_Flags |= SoulTempoFlags.Duo;
            c.m_Buddy = pal;
            SoulTempo theirs = em.GetComponentData<SoulTempo>(pal);
            theirs.m_Flags |= SoulTempoFlags.Duo;
            theirs.m_Buddy = e;
            em.SetComponentData(pal, theirs);
            BumpBoth(em, e, pal, SoulTune.TempoDuoBump);
            SoulFeel.Note(em, e, day, SoulEventKind.DuoOn, 1, pal);
            SoulFeel.Pulse(em, e, SoulTune.TempoDuoMood);
            return c;
        }

        private static void BumpBoth(EntityManager em, Entity a, Entity b, int delta)
        {
            if (a == Entity.Null || b == Entity.Null) return;
            BumpEdge(em, a, b, delta);
            BumpEdge(em, b, a, delta);
        }

        private static void BumpEdge(EntityManager em, Entity from, Entity to, int delta)
        {
            if (!em.HasBuffer<SoulEdge>(from)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(from);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other != to) continue;
                int next = ed.m_Affinity + delta;
                if (next < 0) next = 0;
                if (next > 100) next = 100;
                ed.m_Affinity = (sbyte)next;
                edges[i] = ed;
                return;
            }
        }

        private static uint Roll(uint day, uint salt, uint mod)
        {
            if (mod == 0u) return 0u;
            uint h = day * 747796405u + salt + 2246822519u;
            h ^= h >> 15;
            h *= 2654435761u;
            h ^= h >> 13;
            return h % mod;
        }
    }
}
