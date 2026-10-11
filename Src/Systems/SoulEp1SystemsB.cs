// Citizen Soul -- EP1 systems B: the texture of days (F39, F40, F41).
// Routines carve grooves into a life. Food becomes memory. Faith is sought,
// found, doubted, weathered, and kept -- quietly, on a clock of one's own.
// Every beat is deterministic (day + salt hashing, lane-staggered), fires at
// most once unless a gate opens, and journals through SoulFeel.Note so the
// story writer can quote it back.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    // =====================================================================
    // F39 -- RoutineSystem: grooves worn into the week.
    // =====================================================================
    /// <summary>
    /// A haunt found, a streak counted in quiet visits, a rhythm broken by
    /// life's interruptions, and a new groove cut to replace it. The miss is
    /// private -- a flavor note, not a failure. Routine is the shape a life
    /// holds when nobody is watching.
    /// </summary>
    public partial class RoutineSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int HauntPerMille = 9;
        private const int BreakPerMille = 5;
        private const int StreakMilestoneA = 12;
        private const int StreakMilestoneB = 52;
        private const int StreakMilestoneC = 104;
        private const int StreakCap = 200;
        private const uint RebuildDelayDays = 14u;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Routine;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Routine = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulRoutine>());
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
                            if (em.HasComponent<SoulRoutine>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulRoutine.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("routine.migrate." + e.Index, ex);
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
                SoulLog.Once("routine.migrate", "RoutineSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Routine.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Routine.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("routine.fault." + e.Index, ex);
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
                SoulLog.Exception("routine.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulRoutine>(e)) return;
            SoulRoutine c = em.GetComponentData<SoulRoutine>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;
            uint salt = SoulEp1Util.Salt(e);

            // A. The haunt: somewhere the week keeps returning to.
            if ((c.m_Flags & SoulRoutineFlags.HasSpot) == 0 &&
                (day + salt) % 13u == 0u &&
                SoulEp1Util.Roll(day, salt + 3901u, 1000u) < HauntPerMille)
            {
                c.m_Flags |= SoulRoutineFlags.HasSpot;
                c.m_SpotDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.HauntFoundOn, 1, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // B. The count: visits logged in private; three thresholds noted.
            if ((c.m_Flags & SoulRoutineFlags.HasSpot) != 0 &&
                (c.m_Flags & SoulRoutineFlags.Upended) == 0 &&
                (day + salt) % 7u == 0u)
            {
                if (c.m_Streak < StreakCap) c.m_Streak++;
                if (c.m_Streak == StreakMilestoneA)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.RoutineSetOn, 1, e);
                    SoulFeel.Pulse(em, e, 1);
                }
                else if (c.m_Streak == StreakMilestoneB)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.RoutineSetOn, 2, e);
                    SoulFeel.Pulse(em, e, 2);
                }
                else if (c.m_Streak == StreakMilestoneC)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.RoutineSetOn, 3, e);
                    SoulFeel.Pulse(em, e, 3);
                }
            }

            // C. The break: life upends the groove, and the count with it.
            if ((c.m_Flags & SoulRoutineFlags.HasSpot) != 0 &&
                (c.m_Flags & SoulRoutineFlags.Upended) == 0 &&
                (day + salt) % 97u == 0u &&
                SoulEp1Util.Roll(day, salt + 3902u, 1000u) < BreakPerMille)
            {
                c.m_Flags |= SoulRoutineFlags.Upended;
                c.m_BreakDay = day;
                c.m_Streak = 0;
                SoulFeel.Note(em, e, day, SoulEventKind.RoutineBrokenOn, 1, e);
                SoulFeel.Pulse(em, e, -4);
            }

            // D. A new groove, cut slowly, two weeks after the break.
            if ((c.m_Flags & SoulRoutineFlags.Upended) != 0 &&
                (c.m_Flags & SoulRoutineFlags.NewRoutine) == 0 &&
                day >= c.m_BreakDay + RebuildDelayDays)
            {
                c.m_Flags |= SoulRoutineFlags.NewRoutine;
                SoulFeel.Note(em, e, day, SoulEventKind.RoutineSetOn, 0, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // E. The miss, private as ever. Flavor only -- no audience.
            if ((c.m_Flags & SoulRoutineFlags.NewRoutine) != 0 &&
                (c.m_Flags & SoulRoutineFlags.Misses) == 0 &&
                (day + salt) % 53u == 0u &&
                SoulEp1Util.Roll(day, salt + 3903u, 1000u) < 4u)
            {
                c.m_Flags |= SoulRoutineFlags.Misses;
                SoulFeel.Pulse(em, e, -2);
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F40 -- FoodSystem: the table as biography.
    // =====================================================================
    /// <summary>
    /// Every soul forks early: the home fire or the street smoke. From there
    /// comes a signature -- a dish mastered, or a standing order at a stall --
    /// and eventually the shared meal, which turns eating into belonging. A
    /// second dish era opens later for those who kept cooking.
    /// </summary>
    public partial class FoodSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int StreetStallPerMille = 8;
        private const int DishPerMille = 7;
        private const int MealPerMille = 9;
        private const int MealMinAffinity = 30;
        private const int SecondDishPerMille = 3;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Food;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Food = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulPalate>());
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
                            if (em.HasComponent<SoulPalate>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulPalate.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("food.migrate." + e.Index, ex);
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
                SoulLog.Once("food.migrate", "FoodSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Food.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Food.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("food.fault." + e.Index, ex);
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
                SoulLog.Exception("food.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulPalate>(e)) return;
            SoulPalate c = em.GetComponentData<SoulPalate>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;
            uint salt = SoulEp1Util.Salt(e);

            // A. The first fork in the road: home fire or street smoke.
            if ((c.m_Flags & SoulPalateFlags.HomeCook) == 0 &&
                (c.m_Flags & SoulPalateFlags.StreetFan) == 0 &&
                (day + salt) % 11u == 0u)
            {
                bool home = SoulEp1Util.Roll(day, salt + 4001u, 100u) < 50u;
                if (home) c.m_Flags |= SoulPalateFlags.HomeCook;
                else c.m_Flags |= SoulPalateFlags.StreetFan;
                c.m_Variant = (byte)(1u + SoulEp1Util.Roll(day, salt + 4002u, 4u));
            }

            // B. Home: the signature dish. Street: the standing order.
            if (((c.m_Flags & SoulPalateFlags.HomeCook) != 0 || (c.m_Flags & SoulPalateFlags.StreetFan) != 0) &&
                (c.m_Flags & SoulPalateFlags.Signature) == 0 &&
                (day + salt) % 9u == 0u &&
                SoulEp1Util.Roll(day, salt + 4003u, 1000u) < DishPerMille)
            {
                c.m_Flags |= SoulPalateFlags.Signature;
                if ((c.m_Flags & SoulPalateFlags.HomeCook) != 0)
                {
                    c.m_DishDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.DishMasteredOn, 1, e);
                }
                else
                {
                    c.m_BiteDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.StallRunOn, 1, e);
                }
                SoulFeel.Pulse(em, e, 3);
            }

            // C. The shared meal: food finds a companion.
            if ((c.m_Flags & SoulPalateFlags.Signature) != 0 &&
                (c.m_Flags & SoulPalateFlags.SharedMeal) == 0 &&
                (day + salt) % 17u == 0u &&
                SoulEp1Util.Roll(day, salt + 4004u, 1000u) < MealPerMille)
            {
                Entity pal = SoulEp1Util.Companion(em, e, MealMinAffinity);
                if (pal != Entity.Null)
                {
                    c.m_Flags |= SoulPalateFlags.SharedMeal;
                    c.m_MealDay = day;
                    c.m_DishMate = pal;
                    SoulFeel.Note(em, e, day, SoulEventKind.FeastMateOn, 1, pal);
                    SoulFeel.Pulse(em, e, 2);
                    SoulEp1Util.BumpBoth(em, e, pal, 4, day);
                }
            }

            // D. A second dish era -- the repertoire widens, years on.
            if ((c.m_Flags & SoulPalateFlags.HomeCook) != 0 &&
                (c.m_Flags & SoulPalateFlags.SharedMeal) != 0 &&
                (day + salt) % 101u == 0u &&
                SoulEp1Util.Roll(day, salt + 4005u, 1000u) < SecondDishPerMille)
            {
                c.m_DishDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.DishMasteredOn, 2, e);
                SoulFeel.Pulse(em, e, 2);
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F41 -- FaithSystem: the search, the finding, the doubt, the keeping.
    // =====================================================================
    /// <summary>
    /// Faith arrives on its own schedule: first the seeking, then the finding,
    /// then -- once, on one cold night -- the doubt. Those who weather it
    /// return to the same seat with clearer eyes. A mourning day is kept
    /// quietly, year after year, for the ones who kept believing.
    /// </summary>
    public partial class FaithSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int SeekPerMille = 5;
        private const uint FoundDelayDays = 40u;
        private const int DoubtPerMille = 3;
        private const int ReaffirmPerMille = 30;
        private const int MournPerMille = 40;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Faith;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Faith = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulFaith>());
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
                            if (em.HasComponent<SoulFaith>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulFaith.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("faith.migrate." + e.Index, ex);
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
                SoulLog.Once("faith.migrate", "FaithSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Faith.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Faith.ToEntityArray(Allocator.Temp);
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
                                if (faults <= FaultLogCap) SoulLog.Exception("faith.fault." + e.Index, ex);
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
                SoulLog.Exception("faith.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulFaith>(e)) return;
            SoulFaith c = em.GetComponentData<SoulFaith>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;
            uint salt = SoulEp1Util.Salt(e);

            // A. The seeking, first -- a door tried in private.
            if ((c.m_Flags & SoulFaithFlags.Seeking) == 0 &&
                (c.m_Flags & SoulFaithFlags.Converted) == 0 &&
                (day + salt) % 19u == 0u &&
                SoulEp1Util.Roll(day, salt + 4101u, 1000u) < SeekPerMille)
            {
                c.m_Flags |= SoulFaithFlags.Seeking;
                c.m_SeekDay = day;
                c.m_Guide = SoulEp1Util.Companion(em, e, 21);
                SoulFeel.Note(em, e, day, SoulEventKind.FaithSeekOn, 1, e);
                SoulFeel.Pulse(em, e, 1);
            }

            // B. The finding, on their own clock.
            if ((c.m_Flags & SoulFaithFlags.Seeking) != 0 &&
                (c.m_Flags & SoulFaithFlags.Converted) == 0 &&
                day >= c.m_SeekDay + FoundDelayDays)
            {
                c.m_Flags |= SoulFaithFlags.Converted;
                c.m_ConvertDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FaithFoundOn, 1, e);
                SoulFeel.Pulse(em, e, 3);
            }

            // C. The doubt -- a cold night of the soul, now and then.
            if ((c.m_Flags & SoulFaithFlags.Converted) != 0 &&
                (c.m_Flags & SoulFaithFlags.Doubting) == 0 &&
                (day + salt) % 211u == 0u &&
                SoulEp1Util.Roll(day, salt + 4103u, 1000u) < DoubtPerMille)
            {
                c.m_Flags |= SoulFaithFlags.Doubting;
                c.m_DoubtDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FaithDoubtsOn, 1, e);
                SoulFeel.Pulse(em, e, -3);
            }

            // D. The return: doubt weathered, not erased. The seat is warmer.
            if ((c.m_Flags & SoulFaithFlags.Doubting) != 0 &&
                (day + salt) % 89u == 0u &&
                SoulEp1Util.Roll(day, salt + 4104u, 1000u) < ReaffirmPerMille)
            {
                c.m_Flags &= unchecked((byte)~SoulFaithFlags.Doubting);
                SoulFeel.Note(em, e, day, SoulEventKind.FaithFoundOn, 2, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // E. The mourning day, kept yearly for the ones who kept believing.
            if ((c.m_Flags & SoulFaithFlags.Converted) != 0 &&
                (day + salt) % 347u == 0u &&
                SoulEp1Util.Roll(day, salt + 4105u, 1000u) < MournPerMille)
            {
                c.m_Flags |= SoulFaithFlags.Bereaved;
                c.m_BereaveDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.FaithMournsOn, 1, e);
                SoulFeel.Pulse(em, e, -2);
            }

            em.SetComponentData(e, c);
        }
    }
}
