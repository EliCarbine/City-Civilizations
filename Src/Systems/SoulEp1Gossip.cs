// Citizen Soul -- EP1 systems D3: F50 gossip.
// News travels at the speed of a shared wall. Told, burned, regretted,
// mended. The soul's private ledger of what it said about whom.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F50 -- word of mouth: becoming a source, getting burned when it
    /// travels back, the regret, and the mending.
    /// </summary>
    public partial class GossipSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int SharePerMille = 150;
        private const int BurnPerMille = 120;
        private const int RegretPerMille = 300;
        private const int MendPerMille = 350;
        private const int SubjectAffinity = 10;
        private const int MendAffinity = 20;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Talkers;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Talkers = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulGossip>());
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
                            if (em.HasComponent<SoulGossip>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulGossip.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("gossip.migrate." + e.Index, ex);
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
                SoulLog.Once("gossip.migrate", "GossipSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Talkers.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Talkers.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("gossip.fault." + e.Index, ex);
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
                SoulLog.Exception("gossip.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulGossip>(e)) return;
            SoulGossip c = em.GetComponentData<SoulGossip>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            // A. Becoming a source. Everybody knows somebody who knows.
            if ((c.m_Flags & SoulGossipFlags.Source) == 0 &&
                (day + salt) % 11u == 0u &&
                SoulEp1Util.Roll(day, salt + 5001u, 1000u) < SharePerMille)
            {
                c.m_Flags |= SoulGossipFlags.Source;
                c.m_ShareDay = day;
                Entity subject = SoulEp1Util.Companion(em, e, SubjectAffinity);
                c.m_Subject = subject;
                SoulFeel.Note(em, e, day, SoulEventKind.GossipSharedOn, 1,
                    subject == Entity.Null ? e : subject);
                SoulFeel.Pulse(em, e, 1);
            }

            // B. Burned. It always comes back, wearing a different coat.
            if ((c.m_Flags & SoulGossipFlags.Source) != 0 &&
                (c.m_Flags & SoulGossipFlags.Burned) == 0 &&
                (day + salt) % 43u == 0u &&
                SoulEp1Util.Roll(day, salt + 5002u, 1000u) < BurnPerMille)
            {
                c.m_Flags |= SoulGossipFlags.Burned;
                c.m_HurtDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.GossipBurnedOn, 1, e);
                SoulFeel.Pulse(em, e, -4);
            }

            // C. The regret. Longer than the telling, every time.
            if ((c.m_Flags & SoulGossipFlags.Burned) != 0 &&
                (c.m_Flags & SoulGossipFlags.Regret) == 0 &&
                (day + salt) % 13u == 0u &&
                SoulEp1Util.Roll(day, salt + 5003u, 1000u) < RegretPerMille)
            {
                c.m_Flags |= SoulGossipFlags.Regret;
                Entity about = (c.m_Subject != Entity.Null && em.Exists(c.m_Subject))
                    ? c.m_Subject : e;
                SoulFeel.Note(em, e, day, SoulEventKind.GossipRegretOn, 1, about);
                SoulFeel.Pulse(em, e, -2);
            }

            // D. Mending. The apology that is mostly silence.
            if ((c.m_Flags & SoulGossipFlags.Regret) != 0 &&
                (c.m_Flags & SoulGossipFlags.Mediator) == 0 &&
                (day + salt) % 29u == 0u &&
                SoulEp1Util.Roll(day, salt + 5004u, 1000u) < MendPerMille)
            {
                c.m_Flags |= SoulGossipFlags.Mediator;
                c.m_MendDay = day;
                Entity who = (c.m_Subject != Entity.Null && em.Exists(c.m_Subject))
                    ? c.m_Subject : SoulEp1Util.Companion(em, e, MendAffinity);
                SoulFeel.Note(em, e, day, SoulEventKind.GossipMendedOn, 1,
                    who == Entity.Null ? e : who);
                SoulFeel.Pulse(em, e, 3);
                if (who != Entity.Null) SoulEp1Util.BumpBoth(em, e, who, 4, day);
            }

            em.SetComponentData(e, c);
        }
    }
}
