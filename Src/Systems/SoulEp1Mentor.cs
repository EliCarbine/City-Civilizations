// Citizen Soul -- EP1 systems D5: F54 mentorship.
// The craft passes hand to hand. A bond, a lesson repeated, the day the
// student stops being one, and the small parting at the end of it all.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F54 -- mentorship: taking a protege, teaching, watching them win,
    /// gifting the craft, and the quiet parting.
    /// </summary>
    public partial class MentorSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int BondPerMille = 150;
        private const int TeachPerMille = 300;
        private const int WinPerMille = 250;
        private const int GiftPerMille = 200;
        private const int PartPerMille = 150;
        private const int ProtegeAffinity = 25;
        private const int WinsForWon = 2;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Souls;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulMentor>());
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
                            if (em.HasComponent<SoulMentor>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulMentor.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("mentor.migrate." + e.Index, ex);
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
                SoulLog.Once("mentor.migrate", "MentorSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Souls.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("mentor.fault." + e.Index, ex);
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
                SoulLog.Exception("mentor.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulMentor>(e)) return;
            SoulMentor c = em.GetComponentData<SoulMentor>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);
            Entity protege = (c.m_Protege != Entity.Null && em.Exists(c.m_Protege))
                ? c.m_Protege : Entity.Null;

            // A. The bond forms. Somebody sees something in somebody.
            if ((c.m_Flags & SoulMentorFlags.Mentored) == 0 &&
                (day + salt) % 31u == 0u &&
                SoulEp1Util.Roll(day, salt + 5201u, 1000u) < BondPerMille)
            {
                Entity found = SoulEp1Util.Companion(em, e, ProtegeAffinity);
                c.m_Flags |= SoulMentorFlags.Mentored;
                c.m_BondDay = day;
                c.m_Protege = found;
                SoulFeel.Note(em, e, day, SoulEventKind.MentorBondOn, 1,
                    found == Entity.Null ? e : found);
                SoulFeel.Pulse(em, e, 3);
                if (found != Entity.Null) SoulEp1Util.BumpBoth(em, e, found, 3, day);
            }

            // B. The lesson, repeated until it stops being a lesson.
            if ((c.m_Flags & SoulMentorFlags.Mentored) != 0 &&
                (c.m_Flags & SoulMentorFlags.Mentoring) == 0 &&
                day >= c.m_BondDay + 3u &&
                (day + salt) % 7u == 0u &&
                SoulEp1Util.Roll(day, salt + 5202u, 1000u) < TeachPerMille)
            {
                c.m_Flags |= SoulMentorFlags.Mentoring;
                c.m_LessonDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.LessonTaughtOn, 1,
                    protege == Entity.Null ? e : protege);
                SoulFeel.Pulse(em, e, 2);
                if (protege != Entity.Null) SoulEp1Util.BumpBoth(em, e, protege, 2, day);
            }

            // C. The student wins. Twice, and they are not a student anymore.
            if ((c.m_Flags & SoulMentorFlags.Mentoring) != 0 &&
                (c.m_Flags & SoulMentorFlags.ProtegeWon) == 0 &&
                day >= c.m_LessonDay + 30u &&
                (day + salt) % 61u == 0u &&
                SoulEp1Util.Roll(day, salt + 5203u, 1000u) < WinPerMille)
            {
                if (c.m_Wins < 250) c.m_Wins++;
                if (c.m_Wins >= WinsForWon)
                {
                    c.m_Flags |= SoulMentorFlags.ProtegeWon;
                    c.m_LessonDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.ProtegeWonOn, 1,
                        protege == Entity.Null ? e : protege);
                    SoulFeel.Pulse(em, e, 5);
                    if (protege != Entity.Null) SoulEp1Util.BumpBoth(em, e, protege, 4, day);
                }
            }

            // D. The gift. What was carried, handed over.
            if ((c.m_Flags & SoulMentorFlags.ProtegeWon) != 0 &&
                (c.m_Flags & SoulMentorFlags.Gift) == 0 &&
                (day + salt) % 37u == 0u &&
                SoulEp1Util.Roll(day, salt + 5204u, 1000u) < GiftPerMille)
            {
                c.m_Flags |= SoulMentorFlags.Gift;
                SoulFeel.Note(em, e, day, SoulEventKind.CraftGiftedOn, 1,
                    protege == Entity.Null ? e : protege);
                SoulFeel.Pulse(em, e, 4);
                if (protege != Entity.Null) SoulEp1Util.BumpBoth(em, e, protege, 3, day);
            }

            // E. Parting. Two people who will always nod at each other.
            if ((c.m_Flags & SoulMentorFlags.Gift) != 0 &&
                (c.m_Flags & SoulMentorFlags.Parted) == 0 &&
                (day + salt) % 89u == 0u &&
                SoulEp1Util.Roll(day, salt + 5205u, 1000u) < PartPerMille)
            {
                c.m_Flags |= SoulMentorFlags.Parted;
                c.m_PartDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MentorPartedOn, 1,
                    protege == Entity.Null ? e : protege);
                SoulFeel.Pulse(em, e, -1);
            }

            em.SetComponentData(e, c);
        }
    }
}
