using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M3 (F12): therapy &amp; counseling — distressed souls book help, sessions run on a
    /// weekly cadence against a qualified therapist (high social skill, or a recovered
    /// addict paying it forward), and every session builds a real rapport edge.
    /// Therapist shortage means waitlists: no qualified therapist, no sessions.
    /// </summary>
    public partial class TherapySystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.TherapyLanes; } }

        private EntityQuery m_Souls;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day; int lane;
            if (!AdvanceDay(out day, out lane)) return;

            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    try
                    {
                        if (!Matches(e, lane, Lanes)) continue;
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        SoulMindGate.Scaffold(em, e);
                        Tick(em, e, day, souls);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("therapy.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("therapy.faults", faults + " therapy fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day, NativeArray<Entity> souls)
        {
            SoulMind mind = em.GetComponentData<SoulMind>(e);
            if (mind.m_TherapyDay == day) return;
            mind.m_TherapyDay = day;

            int dep = mind.m_Depression;
            int trauma = mind.m_Trauma;
            bool inTherapy = (mind.m_Flags & SoulMind.FlagInTherapy) != 0;

            if (!inTherapy)
            {
                // F12: distress that lingers books help — if a therapist exists (else: waitlist).
                if ((dep >= SoulTune.DepressMin || trauma >= SoulTune.AnnivTraumaMin) &&
                    Roll(day, (uint)e.Index * 2654435761u + 71u, 1000u) < SoulTune.TherapyStartPerMille)
                {
                    Entity therapist = FindTherapist(em, souls, e, day);
                    if (therapist != Entity.Null)
                    {
                        mind.m_Flags |= SoulMind.FlagInTherapy;
                        mind.m_Sessions = 0;
                        mind.m_LastSessionDay = day;
                        SoulFeel.Note(em, e, day, SoulEventKind.TherapyStarted, (short)0, Entity.Null);
                        SoulFeel.Pulse(em, e, 4);
                    }
                }
            }
            else if (day - mind.m_LastSessionDay >= (uint)SoulTune.TherapySessionEveryDays)
            {
                mind.m_LastSessionDay = day;
                Entity therapist = FindTherapist(em, souls, e, day);
                if (therapist != Entity.Null)
                {
                    dep = Math.Max(0, dep - SoulTune.TherapyDepressHeal);
                    trauma = Math.Max(0, trauma - SoulTune.TraumaTherapyHeal);
                    mind.m_Worry = (byte)Math.Max(0, mind.m_Worry - 2);
                    mind.m_Sessions++;
                    SoulFeel.Pulse(em, e, SoulTune.TherapyMood);
                    if (mind.m_Sessions % SoulTune.TherapyNoteEverySessions == 0)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.TherapyHelped, (short)dep, Entity.Null);
                    }
                    TouchEdge(em, e, therapist, day);   // F12: rapport is a real relationship edge
                    if (dep <= SoulTune.DepressLiftAt && trauma < SoulTune.AnnivTraumaMin)
                    {
                        mind.m_Flags &= (byte)(0xFF ^ SoulMind.FlagInTherapy);   // the arc completes
                    }
                }
            }

            mind.m_Depression = (byte)dep;
            mind.m_Trauma = (byte)trauma;
            em.SetComponentData(e, mind);
        }

        private static bool Qualified(EntityManager em, Entity c, Entity self)
        {
            if (c == self) return false;
            if (!em.HasComponent<SoulMeta>(c)) return false;
            if (!em.HasComponent<Skills>(c)) return false;
            Skills sk = em.GetComponentData<Skills>(c);
            if (sk.m_Social >= SoulTune.TherapistMinSocialLevel * 100) return true;
            // F15: recovered addicts become the best counselors.
            if (!em.HasComponent<SoulMind>(c)) return false;
            SoulMind cm = em.GetComponentData<SoulMind>(c);
            return cm.m_Addiction == 0 && cm.m_SoberDays >= SoulTune.RecoveredCounselorSoberDays;
        }

        private static Entity FindTherapist(EntityManager em, NativeArray<Entity> souls, Entity self, uint day)
        {
            int count = 0;
            for (int j = 0; j < souls.Length; j++)
            {
                if (Qualified(em, souls[j], self)) count++;
            }
            if (count == 0) return Entity.Null;   // waitlist

            uint pick = Roll(day, (uint)self.Index ^ 0x9E3779B9u, (uint)count);
            for (int j = 0; j < souls.Length; j++)
            {
                Entity c = souls[j];
                if (!Qualified(em, c, self)) continue;
                if (pick == 0) return c;
                pick--;
            }
            return Entity.Null;
        }

        private static void TouchEdge(EntityManager em, Entity e, Entity other, uint day)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int k = 0; k < edges.Length; k++)
            {
                if (edges[k].m_Other != other) continue;
                SoulEdge ed = edges[k];
                int aff = ed.m_Affinity + SoulTune.TherapyRapport;
                if (aff > 127) aff = 127;
                if (aff < -127) aff = -127;
                ed.m_Affinity = (sbyte)aff;
                ed.m_Moments++;
                ed.m_LastDay = day;
                edges[k] = ed;
                return;
            }
            // No existing edge: the social systems will formalize the bond when they meet.
        }

        private static uint Roll(uint day, uint salt, uint span)
        {
            uint h = day * 2654435761u ^ salt * 2246822519u;
            h ^= h >> 15;
            h *= 3266489917u;
            h ^= h >> 13;
            return h % span;
        }
    }
}
