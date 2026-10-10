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

    // --- M3: damage, coping, crime, justice, pets (F11-F15, F17, F31-F35, F47) ---
    public const int YearDays = 360;                  // days per soul-year (anniversary cadence)
    public const int TraumatizedGain = 40;            // trauma added by a violent event
    public const int TraumatizedByViolence = 40;      // alias for fight-loss trauma sites
    public const int TraumaTherapyHeal = 4;           // trauma healed per therapy session
    public const int TraumaSupportHeal = 1;           // trauma eased per calm day with friends
    public const int AnnivTraumaMin = 64;             // trauma floor before anniversaries sting
    public const int AnnivFlarePerMille = 350;        // chance (per-mille) of an anniversary flare
    public const int AnnivFlareMood = -12;            // mood hit when an anniversary hurts
    public const int AnnivGrowthPerMille = 250;       // chance (per-mille) the scar instead grows
    public const int AnnivGrowthTrauma = 20;          // trauma shed by post-traumatic growth
    public const int DepressGainPerDay = 1;           // depression gained per bad day
    public const int DepressEasePerDay = 1;           // depression shed per good day
    public const int DepressMin = 96;                 // depression at which souls seek help
    public const int DepressWithdrawAt = 128;         // depression at which souls withdraw
    public const int DepressLiftAt = 32;              // depression below which withdrawal lifts
    public const int WorryGainPerDay = 1;             // anxiety gained per bad day
    public const int WorryCalmPerDay = 2;             // anxiety shed per good day
    public const int CrowdShyAt = 96;                 // anxiety at which crowds feel too loud
    public const int CrowdShyMood = -4;               // mood hit when anxiety peaks
    public const int HabitFormPerMille = 60;          // chance (per-mille) per sour day of a coping habit
    public const int HabitRelief = 2;                 // mood relief each habit use
    public const int HabitReliefDays = 6;             // days between habit uses
    public const int AddictRelapsePerMille = 8;       // chance (per-mille) of relapse per day
    public const int AddictRelapseMood = -10;         // mood cost of a relapse
    public const int AddictStreakDays = 60;           // clean days per sobriety milestone
    public const int AddictStreakMood = 6;            // mood lift at a sobriety milestone
    public const int SourMood = -20;                  // valence at/below which days count as bad
    public const int DesperateMood = -35;             // valence at/below which theft seems worth it
    public const int TherapyStartPerMille = 12;       // chance (per-mille) per day to book therapy
    public const int TherapistMinSocialLevel = 2;     // social skill level that can counsel
    public const int RecoveredCounselorSoberDays = 240; // sober days that also qualify a counselor
    public const int TherapySessionEveryDays = 7;     // days between sessions
    public const int TherapyDepressHeal = 6;          // depression healed per session
    public const int TherapyMood = 6;                 // mood lift per session
    public const int TherapyRapport = 6;              // affinity gained with the counselor per session
    public const int TherapyNoteEverySessions = 3;    // journal a 'helped' note every N sessions
    public const int ChronicRoll = 15;                // chance (per-100000) per day of a diagnosis
    public const int ChronicClinicEveryDays = 7;      // days between clinic routine visits
    public const int ChronicMissedPerMille = 25;      // chance (per-mille) a routine visit slips
    public const int ChronicMissMood = -2;            // mood cost when the routine slips
    public const int ChronicManagedEveryDays = 28;    // days between 'managed' journal notes
    public const int TheftPerMille = 8;               // chance (per-mille) per desperate day
    public const int VandalPerMille = 6;              // chance (per-mille) per sour day for low conscience
    public const int CrimeCaughtPerMille = 120;       // chance (per-mille) a crime is caught
    public const int CrimeFineMood = -6;              // mood cost of being caught
    public const int GangJoinAtNotoriety = 24;        // notoriety at which gangs come calling
    public const int GangJoinPerMille = 40;           // chance (per-mille) per day of recruitment
    public const int GangMood = 6;                    // mood lift of belonging
    public const int GangCrimeBonusPerGang = 6;       // extra crime urge (per-mille) per gang tier
    public const int NotorietyDecayDays = 30;         // days between notoriety cooldowns
    public const int ArrestAtNotoriety = 16;          // notoriety at which police take interest
    public const int ArrestPerMille = 10;             // base arrest chance (per-mille) per day
    public const int ArrestPerNotoriety = 2;          // extra arrest chance (per-mille) per point
    public const int ConvictBasePerMille = 500;       // base conviction chance (per-mille)
    public const int ConvictPerNotoriety = 10;        // extra conviction chance (per-mille) per point
    public const int ConvictCapPerMille = 900;        // conviction chance cap (per-mille)
    public const int PrisonDaysPerStrike = 60;        // prison days per prior conviction
    public const int AcquitNotorietyRelief = 8;       // notoriety shed when acquitted
    public const int PrisonMoodDrag = -2;             // mood drag per day inside
    public const int PrisonTraumaPerMille = 30;       // chance (per-mille) prison bites deep
    public const int PrisonTrauma = 2;                // trauma per prison scar
    public const int ReleaseMood = 8;                 // mood on release day
    public const int ReleaseNotorietyRelief = 12;     // notoriety shed by time served
    public const int PetLonelyEdges = 2;              // friends (or fewer) that invite a pet
    public const int PetAdoptPerMille = 35;           // chance (per-mille) per lonely day to adopt
    public const int PetMood = 8;                     // mood lift of adoption
    public const int PetComfort = 1;                  // mood comfort per bonding moment
    public const int PetComfortEveryDays = 4;         // days between bonding moments
    public const int PetDeathMood = -24;              // mood hit when a pet dies
    public const int PetDeathTrauma = 6;              // trauma when a deep bond is lost
    public const int PetDeathTraumaBond = 40;         // bond level at which the loss scars
    public const int DogLifespanDays = 520;           // soul-days a dog lives
    public const int CatLifespanDays = 640;           // soul-days a cat lives
    public const int BirdLifespanDays = 320;          // soul-days a bird lives

    // --- M4: story, funerals, reunions (F43, F58, F61) ---
    public const int ReunionGapDays = 300;            // days apart that make a meeting a reunion
    public const int ReunionMinAffinity = 60;         // affinity an old friendship needs to count
    public const int ReunionMood = 6;                 // mood lift of a reunion
    public const int MournMoodBase = -6;              // base mood hit of losing someone
    public const int MournCloseAffinity = 90;         // affinity above which a loss scars
    public const int MournTrauma = 10;                // trauma of losing someone close
    public const int MemorialAnnivMood = -8;          // mood hit on a passing's anniversary
    public const int RegistryCap = 64;                // passings remembered in the registry

    // --- M5a: family & dynasty (F05-F10, F60) ---
    public const int FamilyLinkWindowDays = 6;              // days after birth when parents can still be linked
    public const int FamilyLinkGiveUpDays = 12;              // days after birth when unresolved souls settle as founders
    public const int FamilyStartGrace = 2;                  // install grace: souls born this close to tracking start are legacy
    public const int ParentMood = 8;                        // mood for a parent greeting a new child
    public const int AffairColdIntimacy = 30;                // intimacy at or below which a spouse may stray
    public const int AffairTryPerMille = 10;                 // per-mille daily chance a cold spouse strays
    public const int AffairMinAffinity = 55;                 // minimum edge affinity for an affair partner
    public const int AffairDiscoverPerMille = 90;            // per-mille daily chance an affair is discovered
    public const int AffairThrillMood = 6;                  // mood for the straying spouse
    public const int AffairSpouseMood = -22;                  // mood for the betrayed spouse
    public const int AffairGuiltMood = -10;                   // mood for the straying spouse after discovery
    public const int DivorceMinDays = 30;                    // minimum days married before divorce can trigger
    public const int DivorceColdIntimacy = 10;               // intimacy at or below which divorce can trigger
    public const int DivorceColdPerMille = 60;               // per-mille daily chance of divorce while cold
    public const int DivorceMood = -12;                       // mood at divorce
    public const int GrandHelpPerMille = 300;                 // per-mille daily chance a grandparent helps with grandkids
    public const int GrandHelpMood = 4;                     // mood from grandparent time

    // === M5b: work & economy (F21-F25, F27, F30) ===
    public const int JobStartMood = 4;               // mood lift of starting a new job
    public const int PromoteMood = 8;                // mood lift of a promotion
    public const int FiredMood = -14;                // mood hit of losing a job
    public const int HustleMood = 3;                 // mood lift of starting a side hustle
    public const int ShopOpenMood = 12;              // mood lift of opening a shop
    public const int BurnoutMood = -12;              // mood hit of burning out
    public const int BurnoutRecoverMood = 6;         // mood when the fog finally lifts
    public const int DebtMood = -10;                 // mood hit of money trouble
    public const int CharityMood = 5;                // mood lift of giving
    public const int PromoteAfterDays = 24;          // days of tenure between raise chances
    public const int MaxJobLevel = 5;                // top rung of the career ladder
    public const int PromotePerMille = 8;            // per-mille daily chance of a promotion
    public const int BurnoutStreakDays = 36;         // work days before burnout can strike
    public const int BurnoutPerMille = 6;            // per-mille daily chance of burning out
    public const int BurnoutRecoverPerMille = 25;    // per-mille daily chance of recovering
    public const int HustlePerMille = 5;             // per-mille daily chance of starting a hustle
    public const int ShopPerMille = 4;               // per-mille daily chance a hustle becomes a shop
    public const int DebtPerMille = 5;               // per-mille daily chance of money trouble
    public const int CharityPerMille = 7;            // per-mille daily chance of giving

    // --- M5c: reentry -- the long walk back (F36) ---
    public const int ProbationDays = 45;             // days of probation after a record
    public const int CheckInDays = 6;                // days between check-ins
    public const int CheckInGain = 2;                // redemption per clean check-in
    public const int CheckInMood = 2;                // mood for showing up clean
    public const int ViolationPerMille = 45;         // per-mille daily chance of slipping
    public const int ViolationLimit = 3;             // slips before old ways return
    public const int ViolationMood = -6;             // mood for a slip
    public const int BackslideMood = -12;            // mood for falling back
    public const int ProbationStartMood = 1;         // mood when probation begins
    public const int ProgramPerMille = 30;           // per-mille daily chance of enrolling
    public const int ProgramStartMood = 2;           // mood for joining a program
    public const int ReformProgramDays = 30;         // days a program runs
    public const int ProgramGain = 8;                // redemption for finishing
    public const int ProgramMood = 4;                // mood for graduating
    public const int ServicePerMille = 120;          // per-mille daily chance of service
    public const int ServiceGain = 2;                // redemption per service day
    public const int ServiceMood = 3;                // mood for serving the city
    public const int ServiceGoal = 5;                // service days that count as paying the debt
    public const int MentorPerMille = 20;            // per-mille daily chance of a mentor
    public const int MentorMinAgeDays = 7300;        // ~20 years: old enough to guide
    public const int MentorGain = 10;                // redemption from a mentor's belief
    public const int MentorMood = 8;                 // mood for finding a believer
    public const int SteadyJobMinStreak = 14;        // days of work that count as steady
    public const int SteadyJobPerMille = 40;         // per-mille daily chance steady work lands
    public const int SteadyJobGain = 6;              // redemption from steady work
    public const int SteadyJobMood = 6;              // mood for holding it down
    public const int ProbationGain = 12;             // redemption for finishing probation
    public const int ProbationDoneMood = 8;          // mood for finishing clean
    public const int RedeemNeed = 40;                // redemption that clears a record
    public const int ClearedMood = 18;               // mood when the record lifts
    public const int NotorietyRelief = 40;           // heat washed off when cleared

    // --- M5c: midlife & wishes (F56/F57) ---
    public const int MidlifeStartDays = 16200;       // ~45 years: the stir arrives
    public const int MidlifeDelayDays = 10;          // days from stir to pivot
    public const int MidlifeWarmDays = 30;           // days from pivot to second wind
    public const int StirMood = -3;                  // mood of the midlife stir
    public const int PivotMood = 6;                  // mood when the pivot lands
    public const int SecondWindMood = 12;            // mood of the warm years
    public const int WishMax = 4;                    // wishes a soul can hold at once
    public const int WishFormDays = 20;              // days between new wishes
    public const int WishPerMille = 25;              // per-mille daily chance a wish forms
    public const int WishFadeDays = 120;             // days before an old wish fades
    public const int WishFormedMood = 3;             // mood when a wish forms
    public const int WishDoneMood = 14;              // mood when a wish lands
    public const int WishFadeMood = -4;              // mood when a wish fades
    public const int MasterCraftLevel = 4;           // career level that counts as master

    // -- M5d: arts & talent (F62-F71) --
    public const int TalentMinAgeDays = 5840;        // ~16 years: gifts surface
    public const int TalentPerMille = 4;             // daily chance a latent gift, once eligible
    public const int TalentQuirkBonus = 14;          // artistic souls surface sooner
    public const int TalentMood = 10;                // mood when the gift surfaces
    public const int ArtPracticePerMille = 20;       // daily chance of a quiet practice session
    public const int ArtQuirkBonus = 12;             // artistic souls practice more
    public const int ArtPracticeDays = 3;            // days between practice windows
    public const int ArtXpPerSession = 12;           // xp per session (100 = +1 skill)
    public const int GiftBentSkill = 40;             // skill where it stops being a hobby
    public const int GiftBentMood = 8;               // mood when the craft becomes the life
    public const int StageDevotedSkill = 25;         // skill at which a soul turns devoted
    public const int StageAccomplishedSkill = 55;    // ... accomplished
    public const int StageRenownedSkill = 85;        // ... renowned
    public const int StageUpRenown = 6;              // renown per stage-up
    public const int StageUpMood = 7;                // mood per stage-up
    public const int BuskSkill = 20;                 // skill before busking starts
    public const int BuskStartPerMille = 15;         // daily chance the first busk happens
    public const int BuskPerMille = 40;              // daily busking chance once started
    public const int BuskRegularDays = 12;           // busks per new regular
    public const int StreetLegendDays = 100;         // busks to become a street legend
    public const int BuskMood = 2;                   // mood of a busk day
    public const int StreetLegendRenown = 8;         // renown for street-legend status
    public const int StreetLegendMood = 9;           // mood for street-legend status
    public const int BandFormPerMille = 4;           // daily chance a musical soul forms a band
    public const int BandMinAffinity = 50;           // friendship needed to jam
    public const int BandMood = 10;                  // mood when a band forms
    public const int BandGigDays = 10;               // days between gig opportunities
    public const int BandGigPerMille = 70;           // per-mille chance a gig lands
    public const int BandGigRenown = 3;              // renown per small-room gig
    public const int BreakoutMinGigs = 5;            // gigs before a breakout is possible
    public const int BreakoutMinRenown = 40;         // renown before a breakout is possible
    public const int BreakoutRenownBonus = 15;       // renown when the breakout lands
    public const int BreakoutMood = 14;              // mood when the breakout lands
    public const int BandBreakupPerMille = 25;       // per-mille chance a fragile band breaks
    public const int BandBreakupNeuroticism = 140;   // only tense souls break bands up
    public const int BandBreakupMood = -10;          // mood when the band breaks up
    public const int WorkSessions = 10;              // practice sessions per finished work
    public const int WorkRenown = 2;                 // renown per finished work
    public const int WorkMood = 6;                   // mood per finished work
    public const int PublishSkill = 60;              // skill before a manuscript sells
    public const int PublishPerMille = 20;           // daily chance of publication
    public const int PublishRenown = 10;             // renown when published
    public const int PublishMood = 12;               // mood when published
    public const int BestsellerSkill = 90;           // skill before a bestseller
    public const int BestsellerPerMille = 10;        // daily chance of a bestseller
    public const int BestsellerRenown = 20;          // renown of a bestseller
    public const int BestsellerMood = 15;            // mood of a bestseller
    public const int WritersGroupPerMille = 8;       // daily chance of a writers' circle
    public const int WritersMinAffinity = 50;        // friendship needed for the circle
    public const int WritersBump = 5;                // affinity gained with the circle
    public const int MuralSkill = 50;                // skill before a mural is possible
    public const int MuralPerMille = 15;             // daily chance the mural happens
    public const int MuralRenown = 9;                // renown for a city mural
    public const int MuralMood = 10;                 // mood for a city mural
    public const int CommissionSkill = 40;           // skill before commissions start
    public const int CommissionPerMille = 30;        // daily chance a commission lands
    public const int CommissionGapDays = 10;         // days between commissions
    public const int CommissionRenown = 4;           // renown per commission
    public const int CommissionMood = 4;             // mood per commission
    public const int PhotoExhibitSkill = 45;         // skill before prints hang in public
    public const int PhotoExhibitPerMille = 12;      // daily chance of an exhibit
    public const int PhotoExhibitRenown = 8;         // renown of an exhibit
    public const int PhotoExhibitMood = 9;           // mood of an exhibit
    public const int OpeningSkill = 55;              // skill before a first opening night
    public const int OpeningPerMille = 18;           // daily chance of an opening
    public const int OpeningRenown = 9;              // renown of an opening night
    public const int OpeningMood = 11;               // mood of an opening night
    public const int FrightPerMille = 30;            // daily chance the fright hits
    public const int FrightNeuroticism = 120;        // only tense souls freeze up
    public const int FrightMood = -8;                // mood of stage fright
    public const int OvationSkill = 75;              // skill before an ovation is possible
    public const int OvationPerMille = 12;           // daily chance of an ovation
    public const int OvationRenown = 12;             // renown of an ovation
    public const int OvationMood = 14;               // mood of an ovation
    public const int ShortFilmSkill = 35;            // skill before a first short
    public const int ShortFilmPerMille = 16;         // daily chance the short is finished
    public const int ShortFilmRenown = 7;            // renown of a first short
    public const int ShortFilmMood = 9;              // mood of a first short
    public const int ViralMinSkill = 60;             // skill before a clip can go around
    public const int ViralPerMille = 8;              // daily chance the clip goes around
    public const int ViralMood = 12;                 // mood when the clip goes around
    public const int FairSkill = 30;                 // skill before a fair booth is possible
    public const int FairPerMille = 25;              // daily chance of a fair
    public const int FairGapDays = 14;               // days between fairs
    public const int FairRenown = 4;                 // renown per fair
    public const int FairMood = 4;                   // mood per fair
    public const int GiftMinAffinity = 60;           // friendship needed for a handmade gift
    public const int GiftBump = 8;                   // affinity gained by the gift
    public const int GiftMood = 5;                   // mood of giving something handmade
    public const int HeirloomSkill = 80;             // skill before an heirloom is made
    public const int HeirloomAgeDays = 18250;        // ~50 years: one for the ages
    public const int HeirloomRenown = 10;            // renown of an heirloom
    public const int HeirloomMood = 12;              // mood of an heirloom
    public const int DancePartnerPerMille = 12;      // daily chance of finding a dance partner
    public const int DancePartnerMinAffinity = 55;   // friendship needed to dance together
    public const int DancePartnerBump = 6;           // affinity gained with the partner
    public const int DancePartnerMood = 8;           // mood when a partner is found
    public const int DanceCompSkill = 40;            // skill before competitions
    public const int DanceCompPerMille = 20;         // daily chance of a competition
    public const int DanceCompGapDays = 12;          // days between competitions
    public const int DanceCompRenown = 5;            // renown per competition
    public const int DanceCompMood = 6;              // mood per competition
    public const int FirstDanceMood = 16;            // mood of the wedding dance

    // -- M5e: sports & competition (F72-F81) --
    public const int SportMinAgeDays = 2190;          // ~6 years: old enough to play
    public const int SportAdultDays = 6570;           // ~18 years: the district leagues start
    public const int SportRetireAgeDays = 27375;      // ~75 years: time to watch from the stands
    public const int SportPickPerMille = 22;          // daily chance an idle soul takes up a sport
    public const int SportFitnessPickBonus = 10;      // fitness souls find the field sooner
    public const int SportPracticePerMille = 70;      // daily chance of a session
    public const int SportSkillGrow = 2;              // skill per session
    public const int SportStage1Skill = 25;           // a regular
    public const int SportStage2Skill = 45;           // competitive
    public const int SportStage3Skill = 70;           // elite
    public const int SportStage4Skill = 90;           // champion tier
    public const int SportStageMood = 8;              // a step up the ladder
    public const int SportPracticeMood = 3;           // the runner's high
    public const int SportGamePerMille = 60;          // daily chance of a game, match or bout
    public const int SportWinBase = 280;              // base win odds per mille
    public const int SportGameMood = 5;               // playing is its own reward
    public const int SportCrewGames = 12;             // regulars become a crew
    public const int SportCrewMood = 6;
    public const int SportStreakLegend = 10;          // winner-stays streaks make legends
    public const int SportLegendMood = 12;
    public const int SportInjuryPerMille = 3;         // a knock while going hard
    public const int SportInjuryMood = -12;           // the knock hurts
    public const int SportHealPerMille = 25;          // daily odds the wrap comes off
    public const int SportComebackMood = 14;          // back on the field
    public const int SportSeasonDays = 360;           // a league season
    public const int SportTitleRunPerMille = 400;     // odds of a deep playoff run
    public const int SportTitlePerMille = 40;         // season's-end title odds
    public const int SportTitleMood = 22;             // the lift
    public const int SportParadeMood = 24;            // the parade route
    public const int SportLeagueTitlePerMille = 25;   // district-title odds
    public const int SportLeagueMood = 10;
    public const int SportRunClubPerMille = 35;       // runners drift into a club
    public const int SportMarathonSkill = 60;         // the marathon gate
    public const int SportMarathonPerMille = 250;     // odds once ready, once a year
    public const int SportMarathonMood = 18;
    public const int SportKnockoutPerMille = 1;       // hard sparring goes wrong
    public const int SportKnockoutMood = -14;
    public const int SportLANSkill = 45;              // the LAN crown gate
    public const int SportLANPerMille = 400;          // odds at each ladder night
    public const int SportLANMood = 16;
    public const int SportParkMood = 4;               // chess tables in the park
    public const int SportDerbyPerMille = 40;         // a derby day in the stands
    public const int SportDerbyMood = 6;
    public const int SportSplitPerMille = 20;         // the house splits over the game
    public const int SportSplitMinAffinity = 40;      // close enough to take sides
    public const int SportSplitBump = -6;
    public const int SportSplitMood = -8;
    public const int SportNemesisMinGames = 20;       // enough games to have a rival
    public const int SportNemesisMinAffinity = 30;
    public const int SportNemesisBump = 4;            // rivalry sharpens respect
    public const int SportNemesisPerMille = 30;
    public const int SportNemesisMood = 5;
    public const int SportKidStartMood = 6;
    public const int SportKidScoutSkill = 40;         // scouts notice at this skill
    public const int SportKidScoutPerMille = 350;     // and this chance each window
    public const int SportKidStarMood = 12;
    public const int SportKidGrownDays = 6205;        // ~17 years: youth ends
    public const int SportKidQuitSkill = 12;          // below this the fun fades
    public const int SportKidQuitMood = -8;
    public const int SportLastSeasonMood = -6;        // the body starts saying no
    public const int SportRetireMood = -10;
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
