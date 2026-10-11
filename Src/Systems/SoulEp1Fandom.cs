// Citizen Soul -- EP1 systems C2: F45 fandom.
// Everyone needs somewhere to shout. Colors are chosen, a seat becomes
// sacred, and the friend who chose wrong is still a friend.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F45 -- the stands: picking sides, the match-night loop, watching
    /// together, the friendly rivalry, and the rare switch of colors.
    /// </summary>
    public partial class FandomSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int StartPerMille = 8;
        private const int MatchPerMille = 120;
        private const int WatchPerMille = 350;
        private const int RivalPerMille = 60;
        private const int SwitchPerMille = 150;
        private const int RivalAffinity = 20;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Fans;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Fans = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulFandom>());
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
                            if (em.HasComponent<SoulFandom>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulFandom.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("fan.migrate." + e.Index, ex);
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
                SoulLog.Once("fan.migrate", "FandomSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Fans.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Fans.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("fan.fault." + e.Index, ex);
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
                SoulLog.Exception("fan.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulFandom>(e)) return;
            SoulFandom c = em.GetComponentData<SoulFandom>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            // A. Picking a side. Nobody remembers the day they did not care.
            if ((c.m_Flags & SoulFandomFlags.Fan) == 0 &&
                (day + salt) % 23u == 0u &&
                SoulEp1Util.Roll(day, salt + 4601u, 1000u) < StartPerMille)
            {
                c.m_Flags |= SoulFandomFlags.Fan;
                c.m_Variant = (byte)(1u + SoulEp1Util.Roll(day, salt + 4602u, 4u));
                c.m_FanDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FanFoundOn, 1, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // B. Match night: the city wears one color for a few hours.
            if ((c.m_Flags & SoulFandomFlags.Fan) != 0 &&
                (c.m_Flags & SoulFandomFlags.MatchNight) == 0 &&
                (day + salt) % 29u == 0u &&
                SoulEp1Util.Roll(day, salt + 4603u, 1000u) < MatchPerMille)
            {
                c.m_Flags |= SoulFandomFlags.MatchNight;
                c.m_MatchDay = day;
                Entity pal = SoulEp1Util.Companion(em, e, RivalAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.MatchDayOn, 1,
                    pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 5);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 3, day);
            }

            // C. The watch party: the next match, chosen company.
            if ((c.m_Flags & SoulFandomFlags.MatchNight) != 0 &&
                (c.m_Flags & SoulFandomFlags.WatchParty) == 0 &&
                day >= c.m_MatchDay + 1u &&
                (day + salt) % 14u == 0u &&
                SoulEp1Util.Roll(day, salt + 4604u, 1000u) < WatchPerMille)
            {
                c.m_Flags |= SoulFandomFlags.WatchParty;
                Entity pal = SoulEp1Util.Companion(em, e, RivalAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.MatchDayOn, 2,
                    pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 3);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 2, day);
            }

            // D. The friend who chose wrong. Still a friend. Barely.
            if ((c.m_Flags & SoulFandomFlags.Fan) != 0 &&
                (c.m_Flags & SoulFandomFlags.Rival) == 0 &&
                (day + salt) % 37u == 0u &&
                SoulEp1Util.Roll(day, salt + 4605u, 1000u) < RivalPerMille)
            {
                Entity pal = SoulEp1Util.Companion(em, e, RivalAffinity);
                if (pal != Entity.Null && em.HasComponent<SoulFandom>(pal))
                {
                    SoulFandom theirs = em.GetComponentData<SoulFandom>(pal);
                    if ((theirs.m_Flags & SoulFandomFlags.Fan) != 0 &&
                        theirs.m_Variant != c.m_Variant)
                    {
                        c.m_Flags |= SoulFandomFlags.Rival;
                        c.m_RivalFan = pal;
                        SoulFeel.Note(em, e, day, SoulEventKind.RivalryOn, 1, pal);
                        SoulFeel.Pulse(em, e, -1);
                    }
                }
            }

            // E. The heresy, or the homecoming. Colors change once.
            if ((c.m_Flags & SoulFandomFlags.Fan) != 0 &&
                (c.m_Flags & SoulFandomFlags.Switched) == 0 &&
                (day + salt) % 347u == 0u &&
                SoulEp1Util.Roll(day, salt + 4606u, 1000u) < SwitchPerMille)
            {
                c.m_Flags |= SoulFandomFlags.Switched;
                c.m_Variant = (byte)(1u + SoulEp1Util.Roll(day, salt + 4607u, 4u));
                c.m_SwitchDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ColorsSwitched, 1, e);
                SoulFeel.Pulse(em, e, 4);
            }

            em.SetComponentData(e, c);
        }
    }
}
