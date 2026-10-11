// Citizen Soul -- EP1 systems C3: F46 fashion.
// Clothes are the smallest story a soul tells about itself before saying a
// single word. A look is found, altered, abandoned, and one day returned to.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F46 -- personal style: finding the look, the statement piece, the old
    /// jacket that comes back around, and the friend who notices first.
    /// </summary>
    public partial class FashionSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int LookPerMille = 6;
        private const int ThrowbackPerMille = 4;
        private const int StatementPerMille = 5;
        private const int MinComplimentAffinity = 20;
        private const int ComplimentedPerMille = 6;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Style;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Style = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulStyle>());
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
                            if (em.HasComponent<SoulStyle>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulStyle.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("style.migrate." + e.Index, ex);
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
                SoulLog.Once("style.migrate", "FashionSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Style.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Style.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("style.fault." + e.Index, ex);
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
                SoulLog.Exception("style.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulStyle>(e)) return;
            SoulStyle c = em.GetComponentData<SoulStyle>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;
            uint salt = SoulEp1Util.Salt(e);

            // A. The look, found in a mirror one ordinary morning.
            if ((c.m_Flags & SoulStyleFlags.FoundStyle) == 0 &&
                (day + salt) % 17u == 0u &&
                SoulEp1Util.Roll(day, salt + 4701u, 1000u) < LookPerMille)
            {
                c.m_Flags |= SoulStyleFlags.FoundStyle;
                c.m_Variant = (byte)(1u + SoulEp1Util.Roll(day, salt + 4702u, 4u));
                c.m_StyleDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.LookFoundOn, 1, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // B. The statement piece: worn once, remembered for years.
            if ((c.m_Flags & SoulStyleFlags.FoundStyle) != 0 &&
                (c.m_Flags & SoulStyleFlags.Statement) == 0 &&
                (day + salt) % 43u == 0u &&
                SoulEp1Util.Roll(day, salt + 4703u, 1000u) < StatementPerMille)
            {
                c.m_Flags |= SoulStyleFlags.Statement;
                c.m_StatementDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.StatementMadeOn, 1, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // C. The old look returns -- the city was not ready the first time.
            if ((c.m_Flags & SoulStyleFlags.FoundStyle) != 0 &&
                (c.m_Flags & SoulStyleFlags.Throwback) == 0 &&
                (day + salt) % 131u == 0u &&
                SoulEp1Util.Roll(day, salt + 4704u, 1000u) < ThrowbackPerMille)
            {
                c.m_Flags |= SoulStyleFlags.Throwback;
                SoulFeel.Note(em, e, day, SoulEventKind.LookFoundOn, 2, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // D. Somebody notices. Style finally lands as language.
            if ((c.m_Flags & SoulStyleFlags.Statement) != 0 &&
                (c.m_Flags & SoulStyleFlags.Complimented) == 0 &&
                (day + salt) % 21u == 0u &&
                SoulEp1Util.Roll(day, salt + 4705u, 1000u) < ComplimentedPerMille)
            {
                Entity pal = SoulEp1Util.Companion(em, e, MinComplimentAffinity);
                if (pal != Entity.Null)
                {
                    c.m_Flags |= SoulStyleFlags.Complimented;
                    SoulFeel.Note(em, e, day, SoulEventKind.StatementMadeOn, 2, pal);
                    SoulFeel.Pulse(em, e, 2);
                    SoulEp1Util.BumpBoth(em, e, pal, 2, day);
                }
            }

            em.SetComponentData(e, c);
        }
    }
}
