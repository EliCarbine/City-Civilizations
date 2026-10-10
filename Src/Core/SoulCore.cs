using System;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace CitizenSoul
{
    /// <summary>Single source of truth for tuning constants. Tweak here only.</summary>
    public static class SoulConsts
    {
        /// <summary>On-disk component format version. Bump + add migration when fields change.</summary>
        public const byte ComponentVersion = 2;

        // Relationship graph
        public const int MaxEdges = 32;
        public const int AcquaintanceAffinity = 10;
        public const int FriendAffinity = 50;
        public const int CloseAffinity = 80;
        public const int RivalAffinity = -60;

        // Memory / journal rings
        public const int MaxMemories = 64;
        public const int MaxJournal = 64;

        // Decay: a tie with no contact for delay days starts losing affinity,
        // 1 point/day. Family bonds idle 4x longer, rivalries 3x.
        public const int IdleDaysBeforeDecay = 15;
        public const int FamilyIdleMultiplier = 4;
        public const int RivalIdleMultiplier = 3;

        // Fights
        public const int FightCooldownDays = 30;
        public const int FightRounds = 3;
        public const int MinRecoveryDays = 2;
        public const int MaxRecoveryDays = 6;

        // Mood
        public const int MoodMin = -100;
        public const int MoodMax = 100;
        public const int ArousalCalm = 128;

        // Daily cadence (kUpdatesPerDay = 4, kTicksPerDay = 262144 — grounded CS2 clock)
        public const int TicksPerDay = 262144;
        public const int SlicesPerDay = 4;
        public const int SliceTicks = TicksPerDay / SlicesPerDay;

        // Lanes: each system processes its lane slice of citizens per call.
        public const int SocialLanes = 64;
        public const int FightLanes = 64;
        public const int InjuredLanes = 32;
        public const int EmotionLanes = 128;
        public const int DecayLanes = 32;
        public const int JournalLanes = 32;
        public const int VitalsLanes = 128;
        public const int DramaLanes = 64;       // M2 adjunct drama lane (F08/F26/F42/F52/F53/F55)
        public const int MindLanes = 128;       // M3 mental-weather lane (F11/F13/F14/F15)
        public const int ChronicLanes = 64;     // M3 chronic-condition lane (F17)
        public const int TherapyLanes = 64;     // M3 counseling lane (F12)
        public const int StreetLanes = 64;      // M3 street-crime lane (F31-F33)
        public const int JusticeLanes = 64;     // M3 arrest & prison lane (F35)
        public const int PetLanes = 64;         // M3 companion-animal lane (F47)
    }

    /// <summary>Pure integer math — no Unity.Mathematics dependency, fully unit-testable.</summary>
    public static class SoulMath
    {
        public static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static int ClampToMood(int v) { return Clamp(v, SoulConsts.MoodMin, SoulConsts.MoodMax); }

        public static sbyte ToSbyte(int v) { return (sbyte)Clamp(v, sbyte.MinValue, sbyte.MaxValue); }

        public static byte ToByte(int v) { return (byte)Clamp(v, byte.MinValue, byte.MaxValue); }

        /// <summary>Move current toward target by at most step. Core smoothing primitive.</summary>
        public static int Approach(int current, int target, int step)
        {
            if (step < 0) step = -step;
            if (current < target) return current + step > target ? target : current + step;
            if (current > target) return current - step < target ? target : current - step;
            return current;
        }

        public static int Abs(int v) { return v < 0 ? -v : v; }
    }

    /// <summary>Deterministic, cheap RNG seeding for soul decisions.</summary>
    public static class SoulRng
    {
        /// <summary>Guard: Unity.Mathematics.Random requires a non-zero state.</summary>
        public static Random From(uint seed)
        {
            return new Random(seed | 1u);
        }

        /// <summary>
        /// Stable per-citizen stream: same soul + same salt always yields the same
        /// sequence, so unlucky-but-legal states can be re-derived from a save.
        /// </summary>
        public static Random For(ushort soulSeed, Entity e, uint salt)
        {
            uint h = (uint)soulSeed * 747796405u;
            h ^= (uint)e.Index * 2891336453u;
            h ^= (uint)e.Version * 2246822519u;
            h ^= salt * 3266489917u;
            h ^= h >> 15;
            h *= 2654435761u;
            return new Random(h | 1u);
        }
    }

    /// <summary>Game-clock helpers over SimulationSystem.frameIndex.</summary>
    public static class SoulClock
    {
        public static uint DayOf(uint frame) { return frame / (uint)SoulConsts.TicksPerDay; }

        public static int SliceOf(uint frame)
        {
            return (int)((frame % (uint)SoulConsts.TicksPerDay) / (uint)SoulConsts.SliceTicks);
        }

        /// <summary>Deterministic citizen→lane hash (Fibonacci hashing, low bits avoided).</summary>
        public static bool InLane(Entity e, int lane, int lanes)
        {
            uint h = (uint)e.Index * 2654435761u;
            h ^= (uint)e.Version * 2246822519u;
            h ^= h >> 13;
            return (int)(h % (uint)lanes) == lane;
        }
    }

    /// <summary>
    /// Spreads each system's work: OnUpdate fires every GetUpdateInterval ticks;
    /// this hands out lane N on the Nth call of each game day, so a citizen whose
    /// hash lands in lane N is processed ⌈callsPerDay/lanes⌉ times, predictably.
    /// Value type: lives as a field on each system, no allocation, no per-call cost.
    /// </summary>
    public struct SoulLaneScheduler
    {
        private uint m_LastDay;
        private int m_CallsToday;
        private bool m_Started;

        public int NextLane(uint day, int lanes)
        {
            if (!m_Started || day != m_LastDay)
            {
                m_Started = true;
                m_LastDay = day;
                m_CallsToday = 0;
            }
            int lane = m_CallsToday % lanes;
            m_CallsToday++;
            return lane;
        }

        /// <summary>True when this is the first call observed for the given day.</summary>
        public bool DayChanged(uint day)
        {
            return !m_Started || day != m_LastDay;
        }
    }
}
