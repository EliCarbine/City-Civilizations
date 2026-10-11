// Citizen Soul -- EP1 systems C1: F44 nightlife.
// Rooms with music, a stool that remembers you, and the walk home at
// closing time. The night is where a soul meets its own kind.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F44 -- the night world: the first night out, the standing loop, the
    /// big night, the afterparty, and the morning that answers for it.
    /// </summary>
    public partial class NightlifeSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const uint NightStartAgeDays = 6570u;   // ~18 years
        private const int StartPerMille = 6;
        private const int MinCrewAffinity = 20;
        private const int OutPerMille = 200;
        private const int BigNightPerMille = 100;
        private const int AfterPerMille = 300;
        private const int HangoverMood = -3;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Night;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Night = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulNight>());
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
                            if (em.HasComponent<SoulNight>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulNight.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("night.migrate." + e.Index, ex);
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
                SoulLog.Once("night.migrate", "NightlifeSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Night.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Night.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("night.fault." + e.Index, ex);
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
                SoulLog.Exception("night.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulNight>(e)) return;
            SoulNight c = em.GetComponentData<SoulNight>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = SoulEp1Util.Salt(e);

            // A. The first night out, of age and curious.
            if (age >= NightStartAgeDays &&
                (c.m_Flags & SoulNightFlags.Regular) == 0 &&
                (day + salt) % 17u == 0u &&
                SoulEp1Util.Roll(day, salt + 4501u, 1000u) < StartPerMille)
            {
                c.m_Flags |= SoulNightFlags.Regular;
                c.m_Streak = 1;
                Entity pal = SoulEp1Util.Companion(em, e, MinCrewAffinity);
                c.m_Crew = pal;
                SoulFeel.Note(em, e, day, SoulEventKind.NightOutOn, 1, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 4);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 3, day);
            }

            // B. The loop: the same stool, the same hour, week after week.
            if ((c.m_Flags & SoulNightFlags.Regular) != 0 &&
                (day + salt) % 7u == 0u &&
                SoulEp1Util.Roll(day, salt + 4502u, 1000u) < OutPerMille)
            {
                if (c.m_Streak < 255) c.m_Streak++;
                SoulFeel.Note(em, e, day, SoulEventKind.NightOutOn, 2,
                    c.m_Crew == Entity.Null ? e : c.m_Crew);
                SoulFeel.Pulse(em, e, 2);
            }

            // C. The big night. Nobody plans these; they happen.
            if ((c.m_Flags & SoulNightFlags.Regular) != 0 &&
                (c.m_Flags & SoulNightFlags.BigNight) == 0 &&
                (day + salt) % 61u == 0u &&
                SoulEp1Util.Roll(day, salt + 4503u, 1000u) < BigNightPerMille)
            {
                c.m_Flags |= SoulNightFlags.BigNight;
                c.m_NightDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.NightOutOn, 3,
                    c.m_Crew == Entity.Null ? e : c.m_Crew);
                SoulFeel.Pulse(em, e, 6);
            }

            // D. One place was not enough. The crew stays; the story grows.
            if ((c.m_Flags & SoulNightFlags.BigNight) != 0 &&
                (c.m_Flags & SoulNightFlags.Afterparty) == 0 &&
                day >= c.m_NightDay + 1u &&
                (day + salt) % 3u == 0u &&
                SoulEp1Util.Roll(day, salt + 4504u, 1000u) < AfterPerMille)
            {
                c.m_Flags |= SoulNightFlags.Afterparty;
                c.m_AftersDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.LastCallOn, 1,
                    c.m_Crew == Entity.Null ? e : c.m_Crew);
                SoulFeel.Pulse(em, e, 4);
            }

            // E. The morning answers for the night. Quiet, private bill.
            if ((c.m_Flags & SoulNightFlags.Afterparty) != 0 &&
                (c.m_Flags & SoulNightFlags.Hangover) == 0 &&
                day >= c.m_AftersDay + 1u)
            {
                c.m_Flags |= SoulNightFlags.Hangover;
                SoulFeel.Pulse(em, e, HangoverMood);
            }

            em.SetComponentData(e, c);
        }
    }
}
