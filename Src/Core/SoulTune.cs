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
        public const byte ChangeClose = 2;      // becameclose (bit 2): the bond crossed the close line
        public const byte ChangeFellOut = 4;
        public const byte ChangeRival = 8;

        // --- vitals: sleep / meditation / fitness (M1) ---
        public const int VitStartEnergy = 200;          // newly-minted souls are rested

        // --- M2: romance arc (F01-F04) -------------------------------------
        public const int CrushChancePerMille = 30;  // daily odds once the closest friend is mutual
        public const int CrushMinAffinity = 40;     // closeness floor for a crush to form
        public const int IntimacyCrush = 10;        // a fresh spark
        public const int IntimacyCourt = 120;       // spark -> courting
        public const int IntimacyEngage = 220;      // courting -> engaged
        public const int IntimacyWed = 250;         // engaged -> married
        public const int IntimacyPerDay = 6;        // growth on each romance day
        public const int AnniversaryDays = 400;     // game days between anniversaries
        public const int VitStartCalm = 110;
        public const int EnergyDrainPerDay = 50;        // living costs energy; sleep restores it
        public const int SleepMaxRestore = 100;         // restore at perfect quality 255; ~50 at neutral 128
        public const int SleepQualityBase = 128;        // perfectly neutral night's sleep
        public const int SleepQualitySpread = 45;       // uniform roll ±this around base, then modifiers
        public const int NightOwlSleepPenalty = 20;     // night owls short themselves on rest
        public const int InjuredSleepPenalty = 25;      // pain disrupts rest
        public const int StressedSleepPenalty = 10;     // restless when calm is low
        public const int StressCalm = 70;               // below this calm, daily stress pulse
        public const int SleepWellQuality = 165;        // journal threshold: great night
        public const int SleepBadQuality = 75;          // journal threshold: rough night
        public const int EnergyWeary = 60;              // below this energy, daily weary pulse
        public const int EnergyExhausted = 25;          // below this, accidents get likelier
        public const int WearyMood = -4;
        public const int StressedMood = -3;
        public const int MeditateChancePerMille = 140;  // base per-day chance to sit
        public const int LonerMeditateBonus = 60;       // solitude is their temple
        public const int MeditateCalmGain = 16;
        public const int MeditateMood = 3;
        public const int CalmBaseline = 80;             // calm drifts here on days without practice
        public const int CalmDecayPerDay = 5;
        public const int ZenCalm = 160;                 // journal milestone: mind at peace
        public const int ZenMood = 10;
        public const int MeditateHealMinCalm = 120;     // only a calm mind mends faster
        public const int TrainChancePerMille = 160;     // base per-day chance to work out
        public const int FitnessQuirkTrainBonus = 240;
        public const int TrainXp = 8;                   // a bit more than a workday's learning
        public const int TrainEnergyCost = 14;
        public const int TrainMinEnergy = 40;           // too spent to train

        // --- M2 adjuncts (F08/F26/F42/F52/F53/F55) -------------------------
        public const int WorkSpouseMood = 4;            // a trusted ally at work lifts the day
        public const int FeudFloor = -75;               // affinity at or below can harden into a feud
        public const int FeudMinMoments = 4;            // a feud needs a history, not one bad day
        public const int FeudClashDrag = 2;             // every run-in pulls feuders down a notch
        public const int FeudClashNotePerMille = 200;   // and sometimes makes the journal
        public const int FeudMood = -6;                 // the day a feud hardens
        public const int FeudClashMood = -4;            // a brush with the enemy sours the day
        public const int ReconcileChancePerMille = 5;   // daily odds an enemy lets it go
        public const int ReconcileLift = 25;            // a grudge lifted leaves room to breathe
        public const int ReconcileMood = 3;             // relief
        public const int ConfideChancePerMille = 20;    // daily odds a close friend shares a secret
        public const int ConfideDelta = 4;              // secrets bind
        public const int ConfideMood = 2;
        public const int WingmanChancePerMille = 30;    // daily odds a friend plays matchmaker
        public const int WingmanBump = 2;               // each nudge warms the pair
        public const int BirthdayDays = 360;            // game days between birthdays
        public const int PartyAttendPerMille = 300;     // odds a friend drops by
        public const int PartyGuestCap = 6;             // a party, not a festival
        public const int BirthdayMoodSelf = 5;          // turned another year with people around
        public const int BirthdayMoodQuiet = 1;         // a quiet birthday still counts
        public const int BirthdayMoodGuest = 2;         // cake at a friend's place
        public const int FirstCloseMood = 8;            // F08: the first true-blue friend
        public const int FirstCrushMood = 7;

    // --- F26 workplace web ----------------------------------------------------
    public const int WorkSpouseGainPerMille = 8;      // per-mille/day a close colleague edge celebrates
    public const int WorkSpouseMinMoments = 8;        // minimum shared moments before a work spouse can form
    public const int ToxicityAffinity = -40;          // colleague edge at or below this is a toxic coworker
    public const int ToxicMoodChancePerMille = 30;    // per-mille/day a toxic coworker sours the office
    public const int ToxicMood = -3;

    // --- F42 birthdays ----------------------------------------------------------
    public const int BirthdayPartyPerMille = 120;     // chance a birthday becomes a party vs a quiet day
    public const int BirthdayForgetMood = -3;         // mood ding when you forget a close friend's birthday
    public const int BirthdayForgetDrag = -2;         // affinity drag with the friend you forgot

    // --- F52 confidences --------------------------------------------------------
    public const int LeakChancePerMille = 2;          // per-mille/day a held confidence leaks
    public const int LeakMood = -8;                   // mood hit when your confidence leaks
    public const int LeakDelta = -20;                 // affinity hit with the confidant who leaked

    // --- F55 wingmen & matchmakers ---------------------------------------------
    public const int MatchmakerExtraversion = 180;    // extraversion (0..255) at which a soul matchmakes
    public const int WingmanMood = 2;                 // mood lift from a pep talk / set-up
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
