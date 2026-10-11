// Citizen Soul -- MidlifeSystem (M5c): midlife stirrings and the wish list.
// The stir arrives around mid-life, the pivot lands, a second wind follows --
// and all the while wishes form, get chased down, or fade quietly when life
// moves on. Wishes read other souls' surfaces (career, romance, reentry);
// they never write them.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F56/F57: the years ask for something new -- stir, pivot, second wind --
    /// and the bucket list every soul carries privately.
    /// </summary>
    public partial class MidlifeSystem : SoulSystemBase
    {
        private const int MidlifeLanes = 128;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return MidlifeLanes; } }
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;
        private const int WishSlots = 4;

        private EntityQuery m_Wishes;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Wishes = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulWishes>());

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
                // Migration: every soul keeps a private list of wishes.
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulWishes>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulWishes.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("midlife.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("midlife.migrate", "MidlifeSystem: migration pass failed: " + ex.Message, 60000);
            }

            try
            {
                // Per-soul wishes: this lane only.
                NativeArray<Entity> souls = m_Wishes.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, MidlifeLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("midlife.fault." + e.Index, ex);
                        }
                    }
                }
                finally
                {
                    souls.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("midlife.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulWishes>(e)) return;
            SoulWishes w = em.GetComponentData<SoulWishes>(e);
            if (w.m_LastDay == day) return;
            w.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            // A. the stir: mid-life arrives and asks what else there might be.
            if ((w.m_Flags & SoulWishFlags.MidlifeStirred) == 0 &&
                meta.m_BirthDay != 0u &&
                age >= (uint)SoulTune.MidlifeStartDays)
            {
                w.m_Flags |= SoulWishFlags.MidlifeStirred;
                w.m_Flags |= SoulWishFlags.PivotDue;
                w.m_PivotDay = day + (uint)SoulTune.MidlifeDelayDays;
                SoulFeel.Note(em, e, day, SoulEventKind.MidlifeStir, (short)(age / 365u), e);
                SoulFeel.Pulse(em, e, SoulTune.StirMood);
            }

            // B. the pivot: the itch turns into motion.
            if ((w.m_Flags & SoulWishFlags.PivotDue) != 0 && day >= w.m_PivotDay)
            {
                w.m_Flags = (byte)(w.m_Flags & (0xFF ^ SoulWishFlags.PivotDue));
                w.m_Flags |= SoulWishFlags.PivotDone;
                SoulFeel.Note(em, e, day, SoulEventKind.MidlifePivot, (short)SoulTune.MidlifeDelayDays, e);
                SoulFeel.Pulse(em, e, SoulTune.PivotMood);
            }

            // C. the second wind: some time after the pivot, the warm years.
            if ((w.m_Flags & SoulWishFlags.PivotDone) != 0 &&
                (w.m_Flags & SoulWishFlags.SecondWind) == 0 &&
                day >= w.m_PivotDay + (uint)SoulTune.MidlifeWarmDays)
            {
                w.m_Flags |= SoulWishFlags.SecondWind;
                SoulFeel.Note(em, e, day, SoulEventKind.SecondWind, (short)SoulTune.MidlifeWarmDays, e);
                SoulFeel.Pulse(em, e, SoulTune.SecondWindMood);
            }

            // D. a new wish forms when life allows.
            if (w.m_Count < SoulTune.WishMax && day >= w.m_NextWishDay &&
                Roll(day, salt + 21u, 1000u) < (uint)SoulTune.WishPerMille)
            {
                byte kind = PickWish(em, e, day, salt);
                if (kind != SoulWishKind.None)
                {
                    SetSlot(ref w, w.m_Count, kind, day);
                    w.m_Count++;
                    w.m_NextWishDay = day + (uint)SoulTune.WishFormDays;
                    SoulFeel.Note(em, e, day, SoulEventKind.WishFormed, (short)kind, e);
                    SoulFeel.Pulse(em, e, SoulTune.WishFormedMood);
                }
            }

            // E. wishes land, or wishes fade.
            for (int i = 0; i < WishSlots; i++)
            {
                byte kind = GetKind(ref w, i);
                if (kind == SoulWishKind.None) continue;

                bool done = WishDone(em, e, kind);
                bool faded = !done && day >= GetMade(ref w, i) + (uint)SoulTune.WishFadeDays;
                if (!done && !faded) continue;

                if (done)
                {
                    w.m_Fulfilled = (byte)Math.Min(255, w.m_Fulfilled + 1);
                    SoulFeel.Note(em, e, day, SoulEventKind.WishFulfilled, (short)kind, e);
                    SoulFeel.Pulse(em, e, SoulTune.WishDoneMood);
                }
                else
                {
                    w.m_Faded = (byte)Math.Min(255, w.m_Faded + 1);
                    SoulFeel.Note(em, e, day, SoulEventKind.WishFaded, (short)kind, e);
                    SoulFeel.Pulse(em, e, SoulTune.WishFadeMood);
                }

                ClearSlot(ref w, i);
                if (w.m_Count > 0) w.m_Count--;
                i--;   // the slot now holds the next wish; re-check it
            }

            em.SetComponentData(e, w);
        }

        private static bool WishDone(EntityManager em, Entity e, byte kind)
        {
            switch (kind)
            {
                case SoulWishKind.MakePeace:
                    if (em.HasComponent<SoulReentry>(e))
                    {
                        SoulReentry r = em.GetComponentData<SoulReentry>(e);
                        return (r.m_Flags & SoulReentryFlags.Cleared) != 0;
                    }
                    return false;

                case SoulWishKind.StayFree:
                    if (em.HasComponent<SoulReentry>(e))
                    {
                        SoulReentry r = em.GetComponentData<SoulReentry>(e);
                        return (r.m_Flags & SoulReentryFlags.ProbationDone) != 0 &&
                               (r.m_Flags & SoulReentryFlags.Backslid) == 0;
                    }
                    return false;

                case SoulWishKind.FindLove:
                    if (em.HasComponent<SoulRomance>(e))
                    {
                        SoulRomance rom = em.GetComponentData<SoulRomance>(e);
                        return rom.m_Stage >= 2;
                    }
                    return false;

                case SoulWishKind.MasterCraft:
                    if (em.HasComponent<SoulCareer>(e))
                    {
                        SoulCareer c = em.GetComponentData<SoulCareer>(e);
                        return c.m_Level >= SoulTune.MasterCraftLevel;
                    }
                    return false;

                case SoulWishKind.ServeCity:
                    if (em.HasComponent<SoulReentry>(e))
                    {
                        SoulReentry r = em.GetComponentData<SoulReentry>(e);
                        return r.m_ServiceDays >= SoulTune.ServiceGoal;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private static byte PickWish(EntityManager em, Entity e, uint day, uint salt)
        {
            byte[] candidates = new byte[5];
            int n = 0;

            if (em.HasComponent<SoulReentry>(e))
            {
                SoulReentry r = em.GetComponentData<SoulReentry>(e);
                if ((r.m_Flags & SoulReentryFlags.Cleared) == 0) candidates[n++] = SoulWishKind.MakePeace;
                if ((r.m_Flags & SoulReentryFlags.ProbationActive) != 0) candidates[n++] = SoulWishKind.StayFree;
                candidates[n++] = SoulWishKind.ServeCity;
            }

            if (em.HasComponent<SoulRomance>(e))
            {
                SoulRomance rom = em.GetComponentData<SoulRomance>(e);
                if (rom.m_Stage < 2) candidates[n++] = SoulWishKind.FindLove;
            }

            if (em.HasComponent<SoulCareer>(e)) candidates[n++] = SoulWishKind.MasterCraft;

            if (n == 0) return SoulWishKind.None;
            return candidates[(int)Roll(day, salt + 22u, (uint)n)];
        }

        private static byte GetKind(ref SoulWishes w, int i)
        {
            switch (i)
            {
                case 0: return w.m_Kind0;
                case 1: return w.m_Kind1;
                case 2: return w.m_Kind2;
                default: return w.m_Kind3;
            }
        }

        private static uint GetMade(ref SoulWishes w, int i)
        {
            switch (i)
            {
                case 0: return w.m_Made0;
                case 1: return w.m_Made1;
                case 2: return w.m_Made2;
                default: return w.m_Made3;
            }
        }

        private static void SetSlot(ref SoulWishes w, int i, byte kind, uint day)
        {
            switch (i)
            {
                case 0: w.m_Kind0 = kind; w.m_Made0 = day; break;
                case 1: w.m_Kind1 = kind; w.m_Made1 = day; break;
                case 2: w.m_Kind2 = kind; w.m_Made2 = day; break;
                default: w.m_Kind3 = kind; w.m_Made3 = day; break;
            }
        }

        private static void ClearSlot(ref SoulWishes w, int i)
        {
            for (int j = i; j < WishSlots - 1; j++)
            {
                byte k = GetKind(ref w, j + 1);
                uint d = GetMade(ref w, j + 1);
                SetSlot(ref w, j, k, d);
            }
            SetSlot(ref w, WishSlots - 1, SoulWishKind.None, 0u);
        }

        private static uint Roll(uint day, uint salt, uint span)
        {
            uint h = day * 2654435761u ^ salt * 2246822519u;
            h ^= h >> 15;
            h *= 3266489917u;
            h ^= h >> 13;
            return h % span;
        }
    }
}
