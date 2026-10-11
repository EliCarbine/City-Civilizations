// Citizen Soul -- EP1 systems D2: F49 neighborhood character.
// A block raises its own. The corners know your name before the census
// does. This is the soul's attachment to where it lives.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F49 -- the block: putting down roots, hanging colors, closing the
    /// street for a party, and the day someone finally moves away.
    /// No district API exists in Cities Skylines II, so the neighborhood
    /// is carried as an event-driven identity on the soul itself.
    /// </summary>
    public partial class HoodSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int RootPerMille = 250;
        private const int BannerPerMille = 200;
        private const int PartyPerMille = 250;
        private const int MovePerMille = 80;
        private const int MateAffinity = 20;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Rooted;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Rooted = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulHood>());
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
                            if (em.HasComponent<SoulHood>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulHood.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("hood.migrate." + e.Index, ex);
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
                SoulLog.Once("hood.migrate", "HoodSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Rooted.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Rooted.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("hood.fault." + e.Index, ex);
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
                SoulLog.Exception("hood.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulHood>(e)) return;
            SoulHood c = em.GetComponentData<SoulHood>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            // A. Roots. The corners learn the name before the census does.
            if ((c.m_Flags & SoulHoodFlags.Rooted) == 0 &&
                (day + salt) % 19u == 0u &&
                SoulEp1Util.Roll(day, salt + 4901u, 1000u) < RootPerMille)
            {
                c.m_Flags |= SoulHoodFlags.Rooted;
                c.m_RootDay = day;
                Entity mate = SoulEp1Util.Companion(em, e, MateAffinity);
                c.m_BlockMate = mate;
                SoulFeel.Note(em, e, day, SoulEventKind.BlockRootedOn, 1,
                    mate == Entity.Null ? e : mate);
                SoulFeel.Pulse(em, e, 3);
            }

            // B. Colors on the lamppost. Somebody has to go first.
            if ((c.m_Flags & SoulHoodFlags.Rooted) != 0 &&
                (c.m_Flags & SoulHoodFlags.Banner) == 0 &&
                (day + salt) % 23u == 0u &&
                SoulEp1Util.Roll(day, salt + 4902u, 1000u) < BannerPerMille)
            {
                c.m_Flags |= SoulHoodFlags.Banner;
                c.m_PrideDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.BlockBannerOn, 1, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // C. The street closes itself. Tables in the road.
            if ((c.m_Flags & SoulHoodFlags.Banner) != 0 &&
                (c.m_Flags & SoulHoodFlags.BlockParty) == 0 &&
                (day + salt) % 41u == 0u &&
                SoulEp1Util.Roll(day, salt + 4903u, 1000u) < PartyPerMille)
            {
                c.m_Flags |= SoulHoodFlags.BlockParty;
                Entity mate = SoulEp1Util.Companion(em, e, MateAffinity);
                c.m_BlockMate = mate;
                SoulFeel.Note(em, e, day, SoulEventKind.BlockPartyOn, 1,
                    mate == Entity.Null ? e : mate);
                SoulFeel.Pulse(em, e, 5);
                if (mate != Entity.Null) SoulEp1Util.BumpBoth(em, e, mate, 3, day);
            }

            // D. Moving away. The block stays; the soul carries it.
            if ((c.m_Flags & SoulHoodFlags.BlockParty) != 0 &&
                (c.m_Flags & SoulHoodFlags.Moved) == 0 &&
                (day + salt) % 167u == 0u &&
                SoulEp1Util.Roll(day, salt + 4904u, 1000u) < MovePerMille)
            {
                c.m_Flags |= SoulHoodFlags.Moved;
                c.m_GoneDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MovedAwayOn, 1, e);
                SoulFeel.Pulse(em, e, -2);
            }

            em.SetComponentData(e, c);
        }
    }
}
