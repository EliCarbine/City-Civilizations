// Citizen Soul — M0/M1 build-time tuning (extends SoulConsts in SoulCore.cs).
// Kept in its own file so milestone systems never contend over one giant
// constants file. Values are chosen for a 100k-citizen city and retuned with
// profiling data at M6 (integration/polish).

using Unity.Entities;

namespace CitizenSoul
{
    internal static class SoulTune
    {
        // --- initialization ---
        public const int InitBudgetPerPass = 1024;    // souls created per pass (~128 passes/day)
        public const int FamilyBondStart = 45;        // starting parent/sibling bond

        // --- daily learning ---
        public const int LearnLanes = 64;             // 64 passes/day → one learn pass per citizen per day
        public const int DailyBaseXp = 6;
        public const int QuirkXpBonus = 3;
        public const int LevelUpMood = 10;

        // --- mood consolidation ---
        public const int MoodDriftPerDay = 6;
        public const int ArousalDriftPerDay = 12;
        public const int FriendMood = 6;
        public const int FalloutMood = -6;
        public const int RivalMood = -10;

        // --- social / venues ---
        public const int VenuePairBudget = 2048;      // max pairs formed per pass

        // --- fights ---
        public const int FightChancePercent = 10;     // per citizen per day
        public const int HotheadFightChanceBonus = 10;
        public const int FightMoodThreshold = -30;    // miserable citizens square up twice as often
        public const int FightGrudge = -35;           // affinity hit applied after a resolved fight
        public const int FightWinMood = 12;
        public const int FightLossMood = -25;
        public const int FightDrawMood = -5;
        public const int InjuryChancePercent = 35;    // loser injury roll after a decisive fight

        // --- injury / accidents ---
        public const int AccidentChancePerMille = 1;  // per pass (32 lanes → 2/day); rare on purpose
        public const int HealMoodBonus = 8;

        // --- decay ---
        public const int MaxDecayPerPass = 1;         // affinity points lost per idle day

        // --- diagnostics ---
        public const int SpikeLogEveryDays = 50;

        // --- edge change-mask bits (must match SoulEdgeUtil.ApplyThresholds) ---
        public const byte ChangeBefriended = 1;
        public const byte ChangeFellOut = 4;
        public const byte ChangeRival = 8;
    }

    /// <summary>Shared one-liners so systems don't each reinvent mood/memory writes.</summary>
    internal static class SoulFeel
    {
        /// <summary>Applies a mood impulse; consolidation spends impulses once per day.</summary>
        public static void Pulse(EntityManager em, Entity e, int delta)
        {
            if (delta == 0) return;
            Mood m = em.GetComponentData<Mood>(e);
            m.m_Impulse = SoulMath.ToSbyte(SoulMath.Clamp(m.m_Impulse + delta, SoulConsts.MoodMin, SoulConsts.MoodMax));
            em.SetComponentData(e, m);
        }

        /// <summary>Writes one event to both the memory ring and the journal (dual-write invariant).</summary>
        public static void Note(EntityManager em, Entity e, uint day, SoulEventKind kind, short value, Entity about)
        {
            SoulMemUtil.Push(em.GetBuffer<SoulMemory>(e), day, kind, value, about);
            SoulMemUtil.PushJournal(em.GetBuffer<SoulJournalEntry>(e), day, kind, value, about);
        }
    }
}
