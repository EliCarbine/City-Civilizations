// Citizen Soul -- EP1 systems D1: F48 holidays and festivals.
// The calendar interrupts the routine now and then so a city can remember
// it is one people. Fireworks, the parade route, the long table.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F48 -- the festive year: fireworks, the parade, the feast, and the
    /// door that opens one day a year onto everybody they know.
    /// </summary>
    public partial class HolidaySystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int FireworksPerMille = 120;
        private const int ParadePerMille = 250;
        private const int FeastPerMille = 200;
        private const int HostPerMille = 150;
        private const int FeastAffinity = 20;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Festive;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Festive = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulFestive>());
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
                            if (em.HasComponent<SoulFestive>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulFestive.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("fest.migrate." + e.Index, ex);
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
                SoulLog.Once("fest.migrate", "HolidaySystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Festive.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Festive.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("fest.fault." + e.Index, ex);
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
                SoulLog.Exception("fest.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulFestive>(e)) return;
            SoulFestive c = em.GetComponentData<SoulFestive>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            // A. Fireworks. Even the ones who stayed home looked up.
            if ((c.m_Flags & SoulFestiveFlags.Fireworks) == 0 &&
                (day + salt) % 89u == 0u &&
                SoulEp1Util.Roll(day, salt + 4801u, 1000u) < FireworksPerMille)
            {
                c.m_Flags |= SoulFestiveFlags.Fireworks;
                c.m_FestDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FestivalOn, 1, e);
                SoulFeel.Pulse(em, e, 5);
            }

            // B. The parade route. Kids on shoulders; the same song every year.
            if ((c.m_Flags & SoulFestiveFlags.Fireworks) != 0 &&
                (c.m_Flags & SoulFestiveFlags.Parade) == 0 &&
                day >= c.m_FestDay + 1u &&
                (day + salt) % 13u == 0u &&
                SoulEp1Util.Roll(day, salt + 4802u, 1000u) < ParadePerMille)
            {
                c.m_Flags |= SoulFestiveFlags.Parade;
                c.m_FestDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ParadeOn, 1, e);
                SoulFeel.Pulse(em, e, 4);
            }

            // C. The long table. Someone always brings too much of one thing.
            if ((c.m_Flags & SoulFestiveFlags.Parade) != 0 &&
                (c.m_Flags & SoulFestiveFlags.Feast) == 0 &&
                (day + salt) % 31u == 0u &&
                SoulEp1Util.Roll(day, salt + 4803u, 1000u) < FeastPerMille)
            {
                c.m_Flags |= SoulFestiveFlags.Feast;
                c.m_FeastDay = day;
                Entity pal = SoulEp1Util.Companion(em, e, FeastAffinity);
                c.m_FeastMate = pal;
                SoulFeel.Note(em, e, day, SoulEventKind.FeastOn, 1,
                    pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 4);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 3, day);
            }

            // D. The door stays open one day a year. They learn to host.
            if ((c.m_Flags & SoulFestiveFlags.Feast) != 0 &&
                (c.m_Flags & SoulFestiveFlags.Hosts) == 0 &&
                day >= c.m_FeastDay + 7u &&
                (day + salt) % 47u == 0u &&
                SoulEp1Util.Roll(day, salt + 4804u, 1000u) < HostPerMille)
            {
                c.m_Flags |= SoulFestiveFlags.Hosts;
                c.m_HostDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.HostedOn, 1, e);
                SoulFeel.Pulse(em, e, 5);
            }

            em.SetComponentData(e, c);
        }
    }
}
