// Citizen Soul -- WorkSystem (M5b): work & economy layer (F21-F25, F27, F30).
// Runs on the same 128-lane / 2048-day cadence as FamilySystem:
//   - tracks employment transitions (new job, firing) as diary beats.
//   - runs promotion rolls gated by tenure, skill and the Workaholic quirk.
//   - grows side hustles into shops; catches burnout streaks.
//   - flags money trouble when work dries up, and quiet charity when it flows.
using System;
using Game.Citizens;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public class WorkSystem : SoulSystemBase
    {
        private const int WorkLanes = 128;
        private const int MigrateBudget = 64;

        private EntityQuery m_Souls;
        private EntityQuery m_All;
        private EntityQuery m_Economy;

        protected override int Lanes => WorkLanes;
        protected override int Interval => 2048;

        protected override void OnCreate()
        {
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulCareer>());

            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());

            m_Economy = GetEntityQuery(ComponentType.ReadWrite<SoulEconomy>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_Economy.CalculateEntityCount() == 0)
                {
                    Entity reg = em.CreateEntity();
                    em.AddComponentData(reg, SoulEconomy.Fresh());
                    SoulLog.Info("WorkSystem: economy registry created.");
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("work.registry", ex);
                return;
            }

            Entity dyn = m_Economy.GetSingletonEntity();
            SoulEconomy d = em.GetComponentData<SoulEconomy>(dyn);
            if (d.m_StartDay == uint.MaxValue)
            {
                d.m_StartDay = day;
                d.m_LastDay = day;
                em.SetComponentData(dyn, d);
            }

            // Budgeted migration: souls gain work state on first visit.
            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulCareer>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulCareer.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("work.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("work.migrate", "WorkSystem: migration pass failed: " + ex.Message, 60000);
            }

            // Per-soul work: this lane only.
            try
            {
                NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
                int faults = 0;
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, WorkLanes)) continue;
                    try
                    {
                        Advance(em, e, day, ref d);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults <= 4) SoulLog.Exception("work.fault." + e.Index, ex);
                    }
                }
                souls.Dispose();

                d.m_LastDay = day;
                em.SetComponentData(dyn, d);
            }
            catch (Exception ex)
            {
                SoulLog.Exception("work.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day, ref SoulEconomy d)
        {
            if (!em.HasComponent<SoulCareer>(e)) return;
            SoulCareer c = em.GetComponentData<SoulCareer>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            bool student = em.HasComponent<Student>(e);
            bool workaholic = false;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                workaholic = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Workaholic);
            }

            // A. employment: a new job begins, or the old one ends.
            bool employed = TryEmployed(em, e, out Entity workplace);
            if (employed && (c.m_Flags & SoulCareerFlags.Employed) == 0)
            {
                c.m_Workplace = workplace;
                c.m_SinceDay = day;
                c.m_LastRaise = day;
                c.m_Streak = 0;
                c.m_Flags |= SoulCareerFlags.Employed;
                c.m_Flags = (byte)(c.m_Flags & (0xFF ^ SoulCareerFlags.DebtCrunch));
                SoulFeel.Note(em, e, day, SoulEventKind.NewJob, SoulTune.JobStartMood, workplace != Entity.Null ? workplace : e);
            }
            else if (!employed && (c.m_Flags & SoulCareerFlags.Employed) != 0)
            {
                d.m_Firings++;
                c.m_Level = 0;
                c.m_Streak = 0;
                c.m_Workplace = Entity.Null;
                c.m_Flags |= SoulCareerFlags.Fired;
                c.m_Flags = (byte)(c.m_Flags & (0xFF ^ SoulCareerFlags.Employed));
                SoulFeel.Note(em, e, day, SoulEventKind.FiredFromJob, SoulTune.FiredMood, e);
            }
            else if (employed)
            {
                c.m_Workplace = workplace;
                if (c.m_Streak < byte.MaxValue) c.m_Streak++;
            }

            // B. promotion: tenure plus skill, quickened by the Workaholic quirk.
            if ((c.m_Flags & SoulCareerFlags.Employed) != 0 && !student && c.m_Level < SoulTune.MaxJobLevel &&
                day >= c.m_LastRaise + (uint)SoulTune.PromoteAfterDays && SkillLevel(em, e) > c.m_Level)
            {
                int span = workaholic ? 500 : 1000;
                if (Roll(day, e.Index, span) < SoulTune.PromotePerMille)
                {
                    c.m_Level++;
                    c.m_LastRaise = day;
                    c.m_Flags |= SoulCareerFlags.Promoted;
                    d.m_Promotions++;
                    SoulFeel.Note(em, e, day, SoulEventKind.Promoted, SoulTune.PromoteMood, c.m_Workplace != Entity.Null ? c.m_Workplace : e);
                }
            }

            // C. side hustle: ambition grows on its own; some hustles become shops.
            if ((c.m_Flags & SoulCareerFlags.Employed) != 0 && (c.m_Flags & SoulCareerFlags.ShopOwner) == 0 &&
                Roll(day, e.Index, 1000) < SoulTune.HustlePerMille)
            {
                if ((c.m_Flags & SoulCareerFlags.Hustling) == 0)
                {
                    c.m_Flags |= SoulCareerFlags.Hustling;
                    SoulFeel.Note(em, e, day, SoulEventKind.HustleStarted, SoulTune.HustleMood, e);
                }
                else if (!student && Roll(day, e.Index + 7, 1000) < SoulTune.ShopPerMille)
                {
                    c.m_Flags |= SoulCareerFlags.ShopOwner;
                    d.m_Shops++;
                    SoulFeel.Note(em, e, day, SoulEventKind.ShopOpened, SoulTune.ShopOpenMood, e);
                }
            }

            // D. burnout: a long grind without rest catches up with anyone.
            if ((c.m_Flags & SoulCareerFlags.BurntOut) != 0)
            {
                if (Roll(day, e.Index, 1000) < SoulTune.BurnoutRecoverPerMille)
                {
                    c.m_Flags = (byte)(c.m_Flags & (0xFF ^ SoulCareerFlags.BurntOut));
                    c.m_Streak = 0;
                    SoulFeel.Pulse(em, e, SoulTune.BurnoutRecoverMood);
                }
            }
            else if (c.m_Streak >= SoulTune.BurnoutStreakDays)
            {
                int span = workaholic ? 500 : 1000;
                if (Roll(day, e.Index, span) < SoulTune.BurnoutPerMille)
                {
                    c.m_Flags |= SoulCareerFlags.BurntOut;
                    d.m_Burnouts++;
                    SoulFeel.Note(em, e, day, SoulEventKind.Burnout, SoulTune.BurnoutMood, e);
                }
            }

            // E. money: quiet trouble when work dries up, quiet giving when it flows.
            if (!employed && !student && (c.m_Flags & SoulCareerFlags.DebtCrunch) == 0 &&
                Roll(day, e.Index, 1000) < SoulTune.DebtPerMille)
            {
                c.m_Flags |= SoulCareerFlags.DebtCrunch;
                SoulFeel.Note(em, e, day, SoulEventKind.DebtCrunch, SoulTune.DebtMood, e);
            }
            else if (employed && c.m_Level >= 2 && Roll(day, e.Index, 1000) < SoulTune.CharityPerMille)
            {
                d.m_Giving++;
                SoulFeel.Note(em, e, day, SoulEventKind.CharityGiven, SoulTune.CharityMood, e);
            }

            em.SetComponentData(e, c);
        }

        private static bool TryEmployed(EntityManager em, Entity e, out Entity workplace)
        {
            workplace = Entity.Null;
            if (!em.HasComponent<Worker>(e)) return false;
            workplace = em.GetComponentData<Worker>(e).m_Workplace;
            return true;
        }

        private static int SkillLevel(EntityManager em, Entity e)
        {
            if (!em.HasComponent<Skills>(e)) return 0;
            Skills s = em.GetComponentData<Skills>(e);
            return ((s.m_Craft / 100) + (s.m_Logic / 100)) / 2;
        }

        private static int Roll(uint day, int index, int span)
        {
            if (span <= 0) return 0;
            return (int)(Hash(day, index) % (uint)span);
        }

        private static uint Hash(uint day, int index)
        {
            uint h = day * 2654435761u + (uint)index * 2246822519u;
            h ^= h >> 15;
            h *= 3266489917u;
            h ^= h >> 13;
            return h;
        }
    }
}
