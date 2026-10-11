// Citizen Soul -- EP1 systems D4: F51 reputation and renown.
// The name arrives before the person does. Built slowly, spent quickly,
// and — sometimes — paid back in full.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F51 -- standing: becoming known, climbing to legend, the fall from
    /// tarnish, and the long road of redemption.
    /// </summary>
    public partial class ReputationSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int KnownPerMille = 100;
        private const int ClimbPerMille = 200;
        private const int TarnishPerMille = 80;
        private const int RedeemPerMille = 200;
        private const int LegendPeak = 3;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Known;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Known = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulStanding>());
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
                            if (em.HasComponent<SoulStanding>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulStanding.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("stand.migrate." + e.Index, ex);
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
                SoulLog.Once("stand.migrate", "ReputationSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Known.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Known.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("stand.fault." + e.Index, ex);
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
                SoulLog.Exception("stand.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulStanding>(e)) return;
            SoulStanding c = em.GetComponentData<SoulStanding>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            // A. Known. Slowly, locally, mostly by accident.
            if ((c.m_Flags & SoulStandingFlags.Known) == 0 &&
                (day + salt) % 17u == 0u &&
                SoulEp1Util.Roll(day, salt + 5101u, 1000u) < KnownPerMille)
            {
                c.m_Flags |= SoulStandingFlags.Known;
                c.m_Peak = 1;
                c.m_PeakDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.KnownForOn, 1, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // B. The climb. Reputation is compound interest with witnesses.
            if ((c.m_Flags & SoulStandingFlags.Known) != 0 &&
                (day + salt) % 97u == 0u &&
                SoulEp1Util.Roll(day, salt + 5102u, 1000u) < ClimbPerMille)
            {
                if (c.m_Peak < 250) c.m_Peak++;
                if (c.m_Peak >= LegendPeak && (c.m_Flags & SoulStandingFlags.Legend) == 0)
                {
                    c.m_Flags |= SoulStandingFlags.Legend;
                    c.m_LegendDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.LegendOn, 1, e);
                    SoulFeel.Pulse(em, e, 6);
                }
                else
                {
                    c.m_PeakDay = day;
                }
            }

            // C. Tarnish. One afternoon undoes a decade.
            if ((c.m_Flags & SoulStandingFlags.Legend) != 0 &&
                (c.m_Flags & SoulStandingFlags.Tarnished) == 0 &&
                (day + salt) % 137u == 0u &&
                SoulEp1Util.Roll(day, salt + 5103u, 1000u) < TarnishPerMille)
            {
                c.m_Flags |= SoulStandingFlags.Tarnished;
                c.m_TarnishDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.TarnishedOn, 1, e);
                SoulFeel.Pulse(em, e, -5);
            }

            // D. Redeemed. The town decides, eventually, to remember kindly.
            if ((c.m_Flags & SoulStandingFlags.Tarnished) != 0 &&
                (c.m_Flags & SoulStandingFlags.Redeemed) == 0 &&
                (day + salt) % 53u == 0u &&
                SoulEp1Util.Roll(day, salt + 5104u, 1000u) < RedeemPerMille)
            {
                c.m_Flags |= SoulStandingFlags.Redeemed;
                c.m_RedeemDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.RedeemedOn, 1, e);
                SoulFeel.Pulse(em, e, 5);
            }

            em.SetComponentData(e, c);
        }
    }
}
