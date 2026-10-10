// Citizen Soul -- ReentrySystem (M5c): the long walk back after trouble.
// Probation and its check-ins, slips and backslides, reform programs,
// community service, mentors, steady work, and redemption enough to clear
// a record. Reads the M3 SoulCrime / Imprisoned surfaces; extends neither.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F36/F38: after prison, the walk back -- probation, check-ins, reform
    /// programs, service to the city, a mentor who believed, steady work, and
    /// the redemption that finally clears the record.
    /// </summary>
    public partial class ReentrySystem : SoulSystemBase
    {
        private const int ReentryLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private EntityQuery m_Reentry;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Reentry = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulReentry>());

            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            NativeArray<Entity> all = default(NativeArray<Entity>);
            try
            {
                // Migration: souls with a conviction on file begin the walk back.
                if (m_All.CalculateEntityCount() > 0)
                {
                    all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulReentry>(e)) continue;
                        if (!em.HasComponent<SoulCrime>(e)) continue;
                        SoulCrime crime = em.GetComponentData<SoulCrime>(e);
                        if (crime.m_Records == 0) continue;
                        try
                        {
                            SoulReentry r = SoulReentry.Fresh(day);
                            r.m_SinceDay = day;
                            em.AddComponentData(e, r);
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("reentry.migrate." + e.Index, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("reentry.migrate", "ReentrySystem: migration pass failed: " + ex.Message, 60000);
            }

            try
            {
                // Per-soul reentry: this lane only.
                NativeArray<Entity> souls = m_Reentry.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, ReentryLanes)) continue;
                        try
                        {
                            Advance(em, e, day, all);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("reentry.fault." + e.Index, ex);
                        }
                    }
                }
                finally
                {
                    souls.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("reentry.loop", ex);
            }
            finally
            {
                if (all.IsCreated) all.Dispose();
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day, NativeArray<Entity> all)
        {
            if (!em.HasComponent<SoulReentry>(e)) return;
            SoulReentry r = em.GetComponentData<SoulReentry>(e);
            if (r.m_LastDay == day) return;
            r.m_LastDay = day;

            // Behind bars the justice lane owns the schedule; the walk pauses.
            if (em.HasComponent<Imprisoned>(e) && em.IsComponentEnabled<Imprisoned>(e))
            {
                em.SetComponentData(e, r);
                return;
            }

            uint salt = (uint)e.Index * 2654435761u;
            bool probation = (r.m_Flags & SoulReentryFlags.ProbationActive) != 0;

            // A. the walk begins: probation opens once a record exists.
            if (!probation &&
                (r.m_Flags & SoulReentryFlags.ProbationDone) == 0 &&
                (r.m_Flags & SoulReentryFlags.Cleared) == 0)
            {
                r.m_Flags |= SoulReentryFlags.ProbationActive;
                r.m_ProbationUntil = day + (uint)SoulTune.ProbationDays;
                r.m_LastCheck = day;
                probation = true;
                SoulFeel.Note(em, e, day, SoulEventKind.ProbationStarted, (short)SoulTune.ProbationDays, e);
                SoulFeel.Pulse(em, e, SoulTune.ProbationStartMood);
            }

            // B. reform programs run their own course, probation or not.
            if ((r.m_Flags & SoulReentryFlags.InProgram) != 0 &&
                day >= r.m_ProgramDay + (uint)SoulTune.ReformProgramDays)
            {
                r.m_Flags |= SoulReentryFlags.ProgramDone;
                r.m_Flags = (byte)(r.m_Flags & (0xFF ^ SoulReentryFlags.InProgram));
                r.m_Redemption = Cap(r.m_Redemption + SoulTune.ProgramGain);
                SoulFeel.Note(em, e, day, SoulEventKind.ProgramCompleted, (short)SoulTune.ProgramGain, e);
                SoulFeel.Pulse(em, e, SoulTune.ProgramMood);
            }
            else if (probation &&
                     (r.m_Flags & (SoulReentryFlags.InProgram | SoulReentryFlags.ProgramDone)) == 0 &&
                     Roll(day, salt + 11u, 1000u) < (uint)SoulTune.ProgramPerMille)
            {
                r.m_Flags |= SoulReentryFlags.InProgram;
                r.m_ProgramDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ReformProgram, (short)SoulTune.ReformProgramDays, e);
                SoulFeel.Pulse(em, e, SoulTune.ProgramStartMood);
            }

            // C. check-ins: show up clean, or slip. Enough slips and the old
            //    ways come back for their due.
            if (probation && day >= r.m_LastCheck + (uint)SoulTune.CheckInDays)
            {
                r.m_LastCheck = day;
                if (Roll(day, salt + 12u, 1000u) < (uint)SoulTune.ViolationPerMille)
                {
                    r.m_Violations = (byte)Math.Min(255, r.m_Violations + 1);
                    SoulFeel.Note(em, e, day, SoulEventKind.Violation, (short)r.m_Violations, e);
                    SoulFeel.Pulse(em, e, SoulTune.ViolationMood);
                    if (r.m_Violations >= SoulTune.ViolationLimit &&
                        (r.m_Flags & SoulReentryFlags.Backslid) == 0)
                    {
                        r.m_Flags |= SoulReentryFlags.Backslid;
                        r.m_Redemption /= 2;
                        SoulFeel.Note(em, e, day, SoulEventKind.Backslid, (short)r.m_Violations, e);
                        SoulFeel.Pulse(em, e, SoulTune.BackslideMood);
                    }
                }
                else
                {
                    r.m_CheckIns = (byte)Math.Min(255, r.m_CheckIns + 1);
                    r.m_Redemption = Cap(r.m_Redemption + SoulTune.CheckInGain);
                    SoulFeel.Note(em, e, day, SoulEventKind.CheckedIn, (short)r.m_CheckIns, e);
                    SoulFeel.Pulse(em, e, SoulTune.CheckInMood);
                }
            }

            // D. community service: honest hours against a dishonest past.
            if (probation && Roll(day, salt + 13u, 1000u) < (uint)SoulTune.ServicePerMille)
            {
                r.m_ServiceDays = (byte)Math.Min(255, r.m_ServiceDays + 1);
                r.m_Redemption = Cap(r.m_Redemption + SoulTune.ServiceGain);
                SoulFeel.Note(em, e, day, SoulEventKind.CommunityService, (short)r.m_ServiceDays, e);
                SoulFeel.Pulse(em, e, SoulTune.ServiceMood);
            }

            // E. a mentor: someone older, steady, and out of trouble who
            //    decides to believe. Their belief is worth more than the system's.
            if ((r.m_Flags & SoulReentryFlags.MentorBond) == 0 &&
                (r.m_Flags & SoulReentryFlags.Cleared) == 0 &&
                Roll(day, salt + 14u, 1000u) < (uint)SoulTune.MentorPerMille &&
                TryFindMentor(em, e, day, salt, all, out Entity mentor))
            {
                r.m_Mentor = mentor;
                r.m_Flags |= SoulReentryFlags.MentorBond;
                r.m_Redemption = Cap(r.m_Redemption + SoulTune.MentorGain);
                SoulFeel.Note(em, e, day, SoulEventKind.MentorFound, (short)SoulTune.MentorGain, mentor);
                SoulFeel.Pulse(em, e, SoulTune.MentorMood);
            }

            // F. steady work: a job kept long enough becomes a lifeline.
            if ((r.m_Flags & SoulReentryFlags.SteadyJob) == 0 &&
                (r.m_Flags & SoulReentryFlags.Cleared) == 0 &&
                em.HasComponent<SoulCareer>(e))
            {
                SoulCareer c = em.GetComponentData<SoulCareer>(e);
                if ((c.m_Flags & SoulCareerFlags.Employed) != 0 &&
                    c.m_Streak >= SoulTune.SteadyJobMinStreak &&
                    Roll(day, salt + 16u, 1000u) < (uint)SoulTune.SteadyJobPerMille)
                {
                    r.m_Flags |= SoulReentryFlags.SteadyJob;
                    r.m_Redemption = Cap(r.m_Redemption + SoulTune.SteadyJobGain);
                    SoulFeel.Note(em, e, day, SoulEventKind.SteadyWork, (short)c.m_Streak, e);
                    SoulFeel.Pulse(em, e, SoulTune.SteadyJobMood);
                }
            }

            // G. the record lifts: enough redemption and the past stops deciding.
            if ((r.m_Flags & SoulReentryFlags.Cleared) == 0 &&
                r.m_Redemption >= SoulTune.RedeemNeed)
            {
                r.m_Flags |= SoulReentryFlags.Cleared;
                r.m_ClearedDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.RecordCleared, (short)r.m_Redemption, e);
                SoulFeel.Pulse(em, e, SoulTune.ClearedMood);
                if (em.HasComponent<SoulCrime>(e))
                {
                    SoulCrime crime = em.GetComponentData<SoulCrime>(e);
                    crime.m_Notoriety = (byte)Math.Max(0, crime.m_Notoriety - SoulTune.NotorietyRelief);
                    em.SetComponentData(e, crime);
                }
            }

            // H. probation runs out: clean time, or time that never stopped counting.
            if (probation && day >= r.m_ProbationUntil &&
                (r.m_Flags & SoulReentryFlags.ProbationDone) == 0)
            {
                r.m_Flags = (byte)(r.m_Flags & (0xFF ^ SoulReentryFlags.ProbationActive));
                r.m_Flags |= SoulReentryFlags.ProbationDone;
                if ((r.m_Flags & SoulReentryFlags.Backslid) == 0)
                {
                    r.m_Redemption = Cap(r.m_Redemption + SoulTune.ProbationGain);
                }
                SoulFeel.Note(em, e, day, SoulEventKind.ProbationFinished, (short)r.m_CheckIns, e);
                SoulFeel.Pulse(em, e, SoulTune.ProbationDoneMood);
            }

            em.SetComponentData(e, r);
        }

        private static bool TryFindMentor(EntityManager em, Entity self, uint day, uint salt,
                                          NativeArray<Entity> all, out Entity mentor)
        {
            mentor = Entity.Null;
            if (!all.IsCreated || all.Length < 8) return false;

            int start = (int)Roll(day, salt + 15u, (uint)all.Length);
            for (int probe = 0; probe < 24; probe++)
            {
                Entity cand = all[(start + probe) % all.Length];
                if (cand == self) continue;
                if (em.HasComponent<Imprisoned>(cand) && em.IsComponentEnabled<Imprisoned>(cand)) continue;
                if (!em.HasComponent<SoulMeta>(cand)) continue;
                SoulMeta meta = em.GetComponentData<SoulMeta>(cand);
                if (meta.m_BirthDay == 0u || day < meta.m_BirthDay) continue;
                if (day - meta.m_BirthDay < (uint)SoulTune.MentorMinAgeDays) continue;
                if (em.HasComponent<SoulReentry>(cand))
                {
                    SoulReentry cr = em.GetComponentData<SoulReentry>(cand);
                    if ((cr.m_Flags & SoulReentryFlags.ProbationActive) != 0) continue;
                    if ((cr.m_Flags & SoulReentryFlags.Backslid) != 0 &&
                        (cr.m_Flags & SoulReentryFlags.Cleared) == 0) continue;
                }
                mentor = cand;
                return true;
            }
            return false;
        }

        private static byte Cap(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
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
