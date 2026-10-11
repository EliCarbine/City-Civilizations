// Citizen Soul -- EP1 systems D6: F59 chronicle.
// Every life leaves a paper trail: the first time the city paper spelled
// the name right, the front page, and the last piece they ever run.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F59 -- the chronicle: first coverage, the front page, and the
    /// closing write-up. Feeds on standing (F51) and farewells (F20);
    /// works standalone for souls with neither.
    /// </summary>
    public partial class ChronicleSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int CoverPerMille = 80;
        private const int FrontPerMille = 150;
        private const int ObituaryPerMille = 200;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Souls;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulChronicle>());
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
                            if (em.HasComponent<SoulChronicle>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulChronicle.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("chron.migrate." + e.Index, ex);
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
                SoulLog.Once("chron.migrate", "ChronicleSystem: migration pass failed: " + ex.Message);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("chron.fault." + e.Index, ex);
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
                SoulLog.Exception("chron.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulChronicle>(e)) return;
            SoulChronicle c = em.GetComponentData<SoulChronicle>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            uint salt = SoulEp1Util.Salt(e);

            bool known = true;
            if (em.HasComponent<SoulStanding>(e))
            {
                SoulStanding st = em.GetComponentData<SoulStanding>(e);
                known = (st.m_Flags & SoulStandingFlags.Known) != 0;
            }

            // A. First coverage. The city paper learns the name.
            if ((c.m_Flags & SoulChronicleFlags.Covered) == 0 && known &&
                (day + salt) % 73u == 0u &&
                SoulEp1Util.Roll(day, salt + 5301u, 1000u) < CoverPerMille)
            {
                c.m_Flags |= SoulChronicleFlags.Covered;
                c.m_CoverDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ChronicledOn, 1, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // B. Front page. Above the fold, once in a lifetime.
            if ((c.m_Flags & SoulChronicleFlags.Covered) != 0 &&
                (c.m_Flags & SoulChronicleFlags.FrontPage) == 0 &&
                (day + salt) % 149u == 0u &&
                SoulEp1Util.Roll(day, salt + 5302u, 1000u) < FrontPerMille)
            {
                c.m_Flags |= SoulChronicleFlags.FrontPage;
                SoulFeel.Note(em, e, day, SoulEventKind.FrontPageOn, 1, e);
                SoulFeel.Pulse(em, e, 6);
            }

            // C. The closing write-up: on a farewell, or for the legends.
            bool farewell = false;
            if (em.HasComponent<SoulMortality>(e))
            {
                SoulMortality mort = em.GetComponentData<SoulMortality>(e);
                farewell = (mort.m_Flags & SoulMortalityFlags.Farewell) != 0;
            }
            bool legend = false;
            if (em.HasComponent<SoulStanding>(e))
            {
                SoulStanding st2 = em.GetComponentData<SoulStanding>(e);
                legend = (st2.m_Flags & SoulStandingFlags.Legend) != 0;
            }

            if ((c.m_Flags & SoulChronicleFlags.Covered) != 0 &&
                (c.m_Flags & SoulChronicleFlags.Obituary) == 0 &&
                (farewell || legend) &&
                (day + salt) % 173u == 0u &&
                SoulEp1Util.Roll(day, salt + 5303u, 1000u) < ObituaryPerMille)
            {
                c.m_Flags |= SoulChronicleFlags.Obituary;
                c.m_ObituaryDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.ObituaryOn, 1, e);
                SoulFeel.Pulse(em, e, -1);
            }

            em.SetComponentData(e, c);
        }
    }
}
