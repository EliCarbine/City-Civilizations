using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M3 (F11/F13/F14/F15): mental weather — trauma scars, depression arcs, anxiety,
    /// coping habits and addiction progression. One lane slice per call; every soul is
    /// visited exactly once per in-game day (Interval × Lanes = TicksPerDay).
    /// </summary>
    public partial class MindSystem : SoulSystemBase
    {
        protected override int Interval { get { return 2048; } }
        protected override int Lanes { get { return SoulConsts.MindLanes; } }

        private EntityQuery m_Souls;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day; int lane;
            if (!AdvanceDay(out day, out lane)) return;

            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    try
                    {
                        if (!Matches(e, lane, Lanes)) continue;
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (!em.HasComponent<Mood>(e) || !em.HasComponent<SoulPersonality>(e)) continue;
                        if (!em.HasComponent<SoulMind>(e)) em.AddComponentData(e, default(SoulMind));
                        Tick(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("mind.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("mind.faults", faults + " mind fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day)
        {
            SoulMind mind = em.GetComponentData<SoulMind>(e);
            if (mind.m_MindDay == day) return;   // once per day, even if cadence slips
            mind.m_MindDay = day;

            Mood mood = em.GetComponentData<Mood>(e);
            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            int valence = mood.m_Valence;
            uint salt = (uint)e.Index * 2654435761u;

            // ---- F13: the grey gathers under sustained bad days, eases on good ones ----
            int dep = mind.m_Depression;
            int worry = mind.m_Worry;
            if (valence <= SoulTune.SourMood)
            {
                dep += SoulTune.DepressGainPerDay + 1;
                worry += SoulTune.WorryGainPerDay;
            }
            else if (valence >= 10)
            {
                dep -= SoulTune.DepressEasePerDay;
                worry -= SoulTune.WorryCalmPerDay;
            }
            dep = Clamp(dep);
            worry = Clamp(worry);

            if (dep >= SoulTune.DepressMin && mind.m_Depression < SoulTune.DepressMin)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.DepressionOnset, (short)dep, Entity.Null);
                SoulFeel.Pulse(em, e, -6);
            }
            if (worry >= SoulTune.CrowdShyAt && mind.m_Worry < SoulTune.CrowdShyAt)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.AnxietySpike, (short)worry, Entity.Null);
                SoulFeel.Pulse(em, e, SoulTune.CrowdShyMood);
            }

            // ---- F13: withdrawal sets in deep; lifts when the grey breaks ----
            byte flags = mind.m_Flags;
            if (dep >= SoulTune.DepressWithdrawAt)
            {
                flags |= SoulMind.FlagWithdrawn;
            }
            else if (dep <= SoulTune.DepressLiftAt && (flags & SoulMind.FlagWithdrawn) != 0)
            {
                flags &= (byte)(0xFF ^ SoulMind.FlagWithdrawn);
                SoulFeel.Note(em, e, day, SoulEventKind.DepressionLifted, (short)dep, Entity.Null);
                SoulFeel.Pulse(em, e, 6);
            }
            if (worry >= SoulTune.CrowdShyAt) flags |= SoulMind.FlagCrowdShy;
            else if (worry <= SoulTune.CrowdShyAt / 2) flags &= (byte)(0xFF ^ SoulMind.FlagCrowdShy);
            mind.m_Flags = flags;

            // ---- F14: stress finds a valve; low conscientiousness makes it likelier ----
            int habit = mind.m_Habit;
            int addiction = mind.m_Addiction;
            if (habit == 0)
            {
                if (valence <= SoulTune.SourMood && p.m_Conscientiousness < 128 &&
                    Roll(day, salt + 23u, 1000u) < SoulTune.HabitFormPerMille)
                {
                    habit = 1 + (int)Roll(day, salt + 24u, 3u);
                    SoulFeel.Note(em, e, day, SoulEventKind.CopingHabit, (short)habit, Entity.Null);
                }
            }
            else if (day - mind.m_LastHabitDay >= SoulTune.HabitReliefDays)
            {
                mind.m_LastHabitDay = day;
                SoulFeel.Pulse(em, e, SoulTune.HabitRelief);          // short-term relief
                worry = Clamp(worry + 1);                             // …and a small nag
                addiction = Math.Min(255, addiction + 1);             // the valve tightens
            }

            // ---- F15: addiction deepens; sobriety counters it ----
            int sober = mind.m_SoberDays;
            if (addiction > 0)
            {
                uint relapseChance = SoulTune.AddictRelapsePerMille + (valence <= SoulTune.SourMood ? 6u : 0u);
                if (Roll(day, salt + 31u, 1000u) < relapseChance)
                {
                    if (sober > 0)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.Relapsed, (short)Math.Min(sober, 32000), Entity.Null);
                        SoulFeel.Pulse(em, e, SoulTune.AddictRelapseMood);
                        sober = 0;
                    }
                    addiction = Math.Min(255, addiction + 3);
                }
                else
                {
                    if (sober < 65535) sober++;
                    if (sober > 0 && sober % SoulTune.AddictStreakDays == 0)
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.SoberStreak, (short)Math.Min(sober, 32000), Entity.Null);
                        SoulFeel.Pulse(em, e, SoulTune.AddictStreakMood);
                    }
                    if (sober > 0 && sober % 30 == 0) addiction = Math.Max(0, addiction - 1);   // clean months shave it down
                }
                if (mind.m_Addiction < 128 && addiction >= 128)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.HabitEscalated, (short)addiction, Entity.Null);
                    SoulFeel.Pulse(em, e, -8);
                }
            }

            // ---- F11: friendship soothes; anniversaries still sting ----
            int trauma = mind.m_Trauma;

            // ---- F11: a fresh wound adds trauma once (persistent injuries don't re-add daily) ----
            if (em.HasComponent<Injured>(e) && em.IsComponentEnabled<Injured>(e))
            {
                if (mind.m_LastInjuryDay == 0 || day - mind.m_LastInjuryDay > 7u)
                {
                    trauma = Math.Min(255, trauma + SoulTune.TraumatizedGain);
                    SoulFeel.Note(em, e, day, SoulEventKind.Traumatized, (short)trauma, Entity.Null);
                    SoulFeel.Pulse(em, e, SoulTune.AnnivFlareMood);
                }
                mind.m_LastInjuryDay = day;
            }
            int friends = em.HasBuffer<SoulEdge>(e) ? em.GetBuffer<SoulEdge>(e).Length : 0;
            if (trauma > 0 && valence > SoulTune.SourMood && friends >= 3)
            {
                trauma = Math.Max(0, trauma - SoulTune.TraumaSupportHeal);
            }
            if (trauma > 0 && day >= mind.m_ScarDay + SoulTune.YearDays && day - mind.m_LastAnnivDay >= 300u)
            {
                mind.m_LastAnnivDay = day;
                if (trauma >= SoulTune.AnnivTraumaMin && Roll(day, salt + 41u, 1000u) < SoulTune.AnnivFlarePerMille)
                {
                    if (Roll(day, salt + 42u, 1000u) < SoulTune.AnnivGrowthPerMille)
                    {
                        trauma = Math.Max(0, trauma - SoulTune.AnnivGrowthTrauma);
                        SoulFeel.Note(em, e, day, SoulEventKind.TraumaGrowth, (short)trauma, Entity.Null);
                        SoulFeel.Pulse(em, e, 8);
                    }
                    else
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.AnniversaryFlare, (short)trauma, Entity.Null);
                        SoulFeel.Pulse(em, e, SoulTune.AnnivFlareMood);
                    }
                }
            }

            // ---- write-back ----
            mind.m_Trauma = (byte)Clamp(trauma);
            mind.m_Depression = (byte)dep;
            mind.m_Worry = (byte)worry;
            mind.m_Addiction = (byte)addiction;
            mind.m_Habit = (byte)habit;
            mind.m_SoberDays = (ushort)sober;
            em.SetComponentData(e, mind);
        }

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
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

    /// <summary>
    /// M3 (F17): chronic conditions — settled souls roll gently for a diagnosis, then
    /// live the routine: mostly managed days with the occasional slip.
    /// </summary>
    public partial class ChronicSystem : SoulSystemBase
    {
        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.ChronicLanes; } }

        private EntityQuery m_Souls;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulMeta>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            uint day; int lane;
            if (!AdvanceDay(out day, out lane)) return;

            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    try
                    {
                        if (!Matches(e, lane, Lanes)) continue;
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (!em.HasComponent<SoulChronic>(e)) em.AddComponentData(e, default(SoulChronic));
                        Tick(em, e, day);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        SoulLog.Exception("chronic.tick", ex);
                    }
                }
            }
            finally
            {
                souls.Dispose();
            }
            if (faults > 0)
            {
                SoulLog.Throttled("chronic.faults", faults + " chronic fault(s) this slice", 60000);
            }
        }

        private static void Tick(EntityManager em, Entity e, uint day)
        {
            SoulChronic ch = em.GetComponentData<SoulChronic>(e);
            if (ch.m_ChronicDay == day) return;
            ch.m_ChronicDay = day;
            uint salt = (uint)e.Index * 2654435761u;

            if (ch.m_Kind == SoulChronic.KindNone)
            {
                if (Roll(day, salt + 61u, 100000u) < SoulTune.ChronicRoll)
                {
                    uint kindPick = Roll(day, salt + 62u, 5u);
                    switch (kindPick)
                    {
                        case 0u: ch.m_Kind = SoulChronic.KindAsthma; break;
                        case 1u: ch.m_Kind = SoulChronic.KindArthritis; break;
                        case 2u: ch.m_Kind = SoulChronic.KindHypertension; break;
                        case 3u: ch.m_Kind = SoulChronic.KindDiabetes; break;
                        default: ch.m_Kind = SoulChronic.KindVisionLoss; break;
                    }
                    ch.m_Severity = (byte)(1 + Roll(day, salt + 63u, 3u));
                    ch.m_DxDay = day;
                    ch.m_LastRoutineDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.ChronicDx, (short)ch.m_Kind, Entity.Null);
                    SoulFeel.Pulse(em, e, -(4 + 4 * ch.m_Severity));
                }
            }
            else if (day - ch.m_LastRoutineDay >= SoulTune.ChronicClinicEveryDays)
            {
                ch.m_LastRoutineDay = day;
                if (Roll(day, salt + 64u, 1000u) < SoulTune.ChronicMissedPerMille)
                {
                    SoulFeel.Pulse(em, e, SoulTune.ChronicMissMood);   // the routine slips
                }
                else if (day - ch.m_LastNoteDay >= SoulTune.ChronicManagedEveryDays)
                {
                    ch.m_LastNoteDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.ChronicManaged, (short)ch.m_Kind, Entity.Null);
                    SoulFeel.Pulse(em, e, 1);
                }
            }
            em.SetComponentData(e, ch);
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
