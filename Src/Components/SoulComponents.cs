using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>Kinds of life events recorded in soul memory + the journal ring.</summary>
    public enum SoulEventKind : byte
    {
        None = 0,
        Born = 1,        // soul initialized
        Met = 2,         // first contact formed a tie
        Befriended = 3,  // affinity crossed the friend threshold
        FellOut = 4,     // affinity collapsed under acquaintance level
        Fought = 5,      // physical fight resolved
        RivalMade = 6,   // rivalry flag set
        Injured = 7,     // injury onset
        Healed = 8,      // injury recovered
        LevelUp = 9,     // skill gained a level
        GoodDay = 10,    // mood stayed high at day's end
        HardDay = 11,       // mood stayed low at day's end
        // M1 vitals (append-only: existing byte values stay save-stable)
        SleptWell = 12,     // exceptional night's rest
        SleptBad = 13,      // rough night, little rest
        Meditated = 14,      // mind reached (or held) its calm

        // --- M2 (F01-F04): the romance arc ---------------------------------
        CrushFormed = 15,
        CourtStarted = 16,
        Engaged = 17,
        Married = 18,
        Widowed = 19,
        Heartbroken = 20,
        Anniversary = 21,    // one year since the wedding

        // --- M2 adjuncts: firsts, work spouses, birthdays, secrets, feuds, wingmen ---
        WorkSpouse = 22,        // F26: a colleague bond matured into a trusted partner
        Birthday = 23,          // F42: another year older
        BirthdayParty = 24,     // F42: cake at a friend's place
        FirstCloseFriend = 25,  // F08: the first true-blue close friend
        FirstCrush = 26,        // F08: the first spark, ever
        Confided = 27,          // F52: a secret shared between confidants
        FeudStarted = 28,       // F53: a rivalry hardened into an outright feud
        FeudClash = 29,         // F53: a run-in between feuders
        Reconciled = 30,        // F53: two enemies finally let a grudge go
        WingmanAssist = 31,     // F55: a friend nudged two crushes together

        // --- M3: damage, coping, crime, justice, pets (F11-F15, F17, F31-F35, F47) ---
        Traumatized = 32,       // F11: a moment that cut deep
        AnniversaryFlare = 33,  // F11: a date that still stings
        TraumaGrowth = 34,      // F11: post-traumatic growth - a new purpose
        TherapyStarted = 35,    // F12: first session booked
        TherapyHelped = 36,     // F12: a session that moved the mood
        DepressionOnset = 37,   // F13: the grey settled in
        DepressionLifted = 38,  // F13: the grey broke
        AnxietySpike = 39,      // F13: crowds became too much
        CopingHabit = 40,       // F14: stress found a valve
        HabitEscalated = 41,    // F15: the valve became the master
        SoberStreak = 42,       // F15: a clean milestone
        Relapsed = 43,          // F15: the streak broke
        ChronicDx = 44,         // F17: a diagnosis that sticks
        ChronicManaged = 45,    // F17: a routine keeping days steady
        Mourned = 46,           // F11: mourning someone
        Theft = 47,             // F31: took what wasn't theirs
        Vandalism = 48,         // F32: left a mark on the city
        GangJoined = 49,        // F33: found belonging, paid the price
        Arrested = 50,          // F35: cuffs and consequences
        TrialConvicted = 51,    // F35: the gavel fell
        TrialAcquitted = 52,    // F35: walked, barely
        Released = 53,          // F35: back, changed
        PetAdopted = 54,        // F47: a warm anchor at home
        PetDied = 55,           // F47: the anchor slipped
        Reunited = 56,          // F61: old friends meet again after a long spell apart
        AffairStarted = 57,     // M5a: a wandering heart strays
        AffairDiscovered = 58,  // M5a: the truth comes out
        Divorced = 59,          // M5a: the marriage ends
        BecameParent = 60,      // M5a: a child joins the family
        Grandfamily = 61,  // M5a: time spent with grandchildren
        // M5b: work & economy (F21-F25, F27, F30)
        Promoted = 62,           // M5b: earned a step up at work
        FiredFromJob = 63,       // M5b: lost a job
        NewJob = 64,             // M5b: started a job
        HustleStarted = 65,      // M5b: started a side hustle
        ShopOpened = 66,         // M5b: grew a hustle into a shop
        Burnout = 67,            // M5b: worked themselves ragged
        DebtCrunch = 68,         // M5b: money ran tight
        CharityGiven = 69,       // M5b: helped someone get back on their feet
        ProbationStarted = 70,   // M5c: began the long walk back
        CheckedIn = 71,          // M5c: showed up clean
        Violation = 72,          // M5c: tripped a condition of probation
        Backslid = 73,           // M5c: fell back into old ways
        ProbationFinished = 74,  // M5c: finished the walk clean
        ReformProgram = 75,      // M5c: joined a reform program
        ProgramCompleted = 76,   // M5c: graduated the reform program
        CommunityService = 77,   // M5c: put in work for the city
        MentorFound = 78,        // M5c: found someone who believed
        SteadyWork = 79,         // M5c: held down steady work
        RecordCleared = 80,      // M5c: cleared their name
        MidlifeStir = 81,        // M5c: felt the years ask for something
        MidlifePivot = 82,       // M5c: turned a corner mid-life
        SecondWind = 83,         // M5c: found a second wind
        WishFormed = 84,         // M5c: set their heart on something
        WishFulfilled = 85,      // M5c: chased a wish down
        WishFaded = 86,          // M5c: let an old wish go
        TalentUnlocked = 87,     // M5d F62: a gift nobody knew was there
        GiftBentLife = 88,       // M5d F62: the craft quietly became the life
        BuskStarted = 89,        // M5d F64: first afternoon on the corner
        StreetLegend = 90,       // M5d F64: the corner knows their name
        BandFormed = 91,         // M5d F63: two souls started making noise
        BandGig = 92,            // M5d F63: a proper show, small room
        BandBreakup = 93,        // M5d F63: the band fell apart
        BreakoutAct = 94,        // M5d F63: out of the small rooms
        BookFinished = 95,       // M5d F65: first manuscript done
        BookPublished = 96,      // M5d F65: their name in print
        Bestseller = 97,         // M5d F65: the whole city read it
        WritersGroup = 98,       // M5d F65: found a circle of writers
        MuralDone = 99,          // M5d F66: paint on the city's walls
        ArtCommission = 100,     // M5d F66: painted for paying strangers
        PhotoExhibit = 101,      // M5d F67: prints hung in public
        OpeningNight = 102,      // M5d F68: the first curtain
        StageFright = 103,       // M5d F68: the lights were too bright
        Ovation = 104,           // M5d F68: standing ovation
        ShortFilm = 105,         // M5d F69: first short finished
        FilmViral = 106,         // M5d F69: a clip went around
        CraftFair = 107,         // M5d F70: sold at the street fair
        HandmadeGift = 108,      // M5d F70: made by hand, just for them
        Heirloom = 109,          // M5d F70: made to outlast them
        DancePartner = 110,      // M5d F71: someone to dance with
        DanceComp = 111,         // M5d F71: danced before judges
        FirstDance = 112,        // M5d F71: the wedding dance
        ArtStageUp = 113,        // M5d F62: a step up in the craft
        CrewFormed = 114,        // M5e F72: found a regular crew
        PickupLegend = 115,      // M5e F72: ran the court all summer
        LeagueJoined = 116,      // M5e F73: signed with a district team
        LeagueChamp = 117,       // M5e F73: won the district title
        RunStarted = 118,        // M5e F74: started running mornings
        Marathon = 119,          // M5e F74: went the whole marathon
        DojoJoined = 120,        // M5e F75: bowed into the dojo
        Knockout = 121,          // M5e F75: a hard night on the canvas
        LANChamp = 122,          // M5e F76: took the LAN crown
        ParkMatch = 123,         // M5e F77: chess in the park
        NemesisMet = 124,        // M5e F77: a rival across the board
        KidSportStart = 125,     // M5e F78: first season on the field
        KidScouted = 126,        // M5e F78: scouts noticed the kid
        KidQuit = 127,           // M5e F78: put the cleats away
        TitleRun = 128,          // M5e F79: a deep run in the playoffs
        TitleWon = 129,          // M5e F79: brought the title home
        ParadeDay = 130,         // M5e F79: rode the parade route
        DerbyDay = 131,          // M5e F80: derby day in the stands
        SplitFamily = 132,       // M5e F80: took sides over the derby
        SportInjury = 133,       // M5e F81: a knock on the field
        Comeback = 134,          // M5e F81: back on the field
        LastSeason = 135,        // M5e F81: the veteran's last season
        SportRetired = 136,      // M5e F81: hung the boots up
        SportStageUp = 137,      // M5e F72-F79: another rung on the ladder
        }

    /// <summary>Quirk bits packed into SoulPersonality.m_Quirk.</summary>
    public static class SoulQuirks
    {
        public const int Hothead = 0;
        public const int Charming = 1;
        public const int Loner = 2;
        public const int Workaholic = 3;
        public const int NightOwl = 4;
        public const int Fitness = 5;
        public const int Artistic = 6;
        public const int Tidy = 7;
        public const int Bookworm = 8;
        public const int Jokester = 9;
        public const int Count = 10;

        public static bool Has(uint quirk, int bit) { return (quirk & (1u << bit)) != 0u; }

        /// <summary>Deterministic starting quirks from the soul's seed (1-2 quirks, not Loner+Charming).</summary>
        public static uint Roll(ushort soulSeed)
        {
            uint r = (uint)soulSeed * 1103515245u + 12345u;
            uint q = 0u;
            int first = (int)(r >> 16) % Count;
            q |= 1u << first;
            r = r * 1103515245u + 12345u;
            if (((r >> 20) & 3u) != 0u) // 75% of souls carry a second quirk
            {
                int second = (int)((r >> 8) % (uint)Count);
                if (second != first) q |= 1u << second;
            }
            return q;
        }
    }

    /// <summary>Marker: this citizen has a soul (all components below hang off it).</summary>
    public struct SoulTag : IComponentData { }

    /// <summary>Five OCEAN-ish axes (0-255) + packed quirks + stable soul seed.</summary>
    public struct SoulPersonality : IComponentData, ISerializable
    {
        public byte m_Openness;
        public byte m_Conscientiousness;
        public byte m_Extraversion;
        public byte m_Agreeableness;
        public byte m_Neuroticism;
        public uint m_Quirk;
        public ushort m_SoulSeed;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Openness);
            writer.Write(m_Conscientiousness);
            writer.Write(m_Extraversion);
            writer.Write(m_Agreeableness);
            writer.Write(m_Neuroticism);
            writer.Write(m_Quirk);
            writer.Write(m_SoulSeed);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("personality.version", "SoulPersonality read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Openness);
            reader.Read(out m_Conscientiousness);
            reader.Read(out m_Extraversion);
            reader.Read(out m_Agreeableness);
            reader.Read(out m_Neuroticism);
            reader.Read(out m_Quirk);
            reader.Read(out m_SoulSeed);
        }
    }

    /// <summary>
    /// Current mood. Valence is the smoothed emotional state; impulse is a pending
    /// one-shot shock applied and halved by EmotionSystem (bounded, allocation-free).
    /// </summary>
    public struct Mood : IComponentData, ISerializable
    {
        public sbyte m_Valence;    // -100..100
        public sbyte m_Impulse;    // pending shock, decays to 0
        public byte m_Arousal;     // 0-255, calm = 128
        public uint m_SinceDay;    // last day EmotionSystem ran for this citizen

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Valence);
            writer.Write(m_Impulse);
            writer.Write(m_Arousal);
            writer.Write(m_SinceDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("mood.version", "Mood read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Valence);
            reader.Read(out m_Impulse);
            reader.Read(out m_Arousal);
            reader.Read(out m_SinceDay);
        }
    }

    /// <summary>Skills stored as level*100 + xp. Level cap 99 by construction.</summary>
    public struct Skills : IComponentData, ISerializable
    {
        public ushort m_Logic;
        public ushort m_Craft;
        public ushort m_Social;
        public ushort m_Fitness;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Logic);
            writer.Write(m_Craft);
            writer.Write(m_Social);
            writer.Write(m_Fitness);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("skills.version", "Skills read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Logic);
            reader.Read(out m_Craft);
            reader.Read(out m_Social);
            reader.Read(out m_Fitness);
        }
    }

    /// <summary>Per-soul bookkeeping: birth day + per-system day cursors.</summary>
    public struct SoulMeta : IComponentData, ISerializable
    {
        public uint m_BirthDay;
        public uint m_LastLearnDay;
        public uint m_LastSocialDay;
        public ushort m_MirroredMemories; // journal cursor: memories already copied to journal

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_BirthDay);
            writer.Write(m_LastLearnDay);
            writer.Write(m_LastSocialDay);
            writer.Write(m_MirroredMemories);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("meta.version", "SoulMeta read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_BirthDay);
            reader.Read(out m_LastLearnDay);
            reader.Read(out m_LastSocialDay);
            reader.Read(out m_MirroredMemories);
        }
    }

    /// <summary>Enableable: citizen is mid-fight. Round counts up to SoulConsts.FightRounds.</summary>
    public struct InFight : IComponentData, IEnableableComponent, ISerializable
    {
        public Entity m_Opponent;
        public byte m_Round;
        public uint m_StartDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Opponent);
            writer.Write(m_Round);
            writer.Write(m_StartDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("infight.version", "InFight read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Opponent);
            reader.Read(out m_Round);
            reader.Read(out m_StartDay);
        }
    }

    /// <summary>Enableable: injured, recovering toward m_RecoverDay.</summary>
    public struct Injured : IComponentData, IEnableableComponent, ISerializable
    {
        public byte m_Severity;   // 1-2: sprain vs fracture
        public uint m_RecoverDay; // day the injury clears
        public Entity m_Source;   // who/what did it (Null for accidents)

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Severity);
            writer.Write(m_RecoverDay);
            writer.Write(m_Source);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("injured.version", "Injured read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Severity);
            reader.Read(out m_RecoverDay);
            reader.Read(out m_Source);
        }
    }

    /// <summary>City-wide soul counters, singleton on the config entity. O(1) per event.</summary>
    public struct SoulCityStats : IComponentData, ISerializable
    {
        public uint m_SoulsCreated;
        public uint m_FightsResolved;
        public uint m_InjuriesTotal;
        public uint m_HealsTotal;
        public uint m_FriendshipsFormed;
        public uint m_LastStatsDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_SoulsCreated);
            writer.Write(m_FightsResolved);
            writer.Write(m_InjuriesTotal);
            writer.Write(m_HealsTotal);
            writer.Write(m_FriendshipsFormed);
            writer.Write(m_LastStatsDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("stats.version", "SoulCityStats read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_SoulsCreated);
            reader.Read(out m_FightsResolved);
            reader.Read(out m_InjuriesTotal);
            reader.Read(out m_HealsTotal);
            reader.Read(out m_FriendshipsFormed);
            reader.Read(out m_LastStatsDay);
        }
    }

    /// <summary>
    /// M0 persistence spike: a monotonic counter + rolling hash living on the config
    /// entity. If m_Counter keeps climbing across a save/load AND m_RollHash matches
    /// the value expected from the same number of updates, per-citizen persistence is
    /// proven end-to-end (see SoulSpikeSystem, 50-day checkpoint logging).
    /// </summary>
    public struct SoulSpike : IComponentData, ISerializable
    {
        public uint m_Counter;
        public uint m_RollHash;
        public uint m_LastDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Counter);
            writer.Write(m_RollHash);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("spike.version", "SoulSpike read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Counter);
            reader.Read(out m_RollHash);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>
    /// The romance arc (F01-F04). Stage: 0 single, 1 crush, 2 courting,
    /// 3 engaged, 4 married. Partner is Null while unbound. Intimacy is the
    /// slow currency of the arc; LastDay is the last day its pass ran.
    /// </summary>
    public struct SoulRomance : IComponentData, ISerializable
    {
        public Entity m_Partner;   // bound partner once engaged (Null while unbound)
        public byte m_Stage;       // 0 single .. 4 married
        public byte m_Intimacy;    // 0-255: the slow currency of the arc
        public uint m_SinceDay;    // day the current stage began
        public uint m_LastDay;     // last day this soul ran its romance pass

        public static SoulRomance Fresh(uint day)
        {
            SoulRomance r = default(SoulRomance);
            r.m_LastDay = day;
            return r;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Partner);
            writer.Write(m_Stage);
            writer.Write(m_Intimacy);
            writer.Write(m_SinceDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("romance.version", "SoulRomance read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Partner);
            reader.Read(out m_Stage);
            reader.Read(out m_Intimacy);
            reader.Read(out m_SinceDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>
    /// M1 personal-vitals lane: F18 sleep energy, F19 meditation calm.
    /// F16 fitness lives in Skills.m_Fitness (trained by VitalsSystem, read by
    /// fights and accident rolls). m_LastDay is this lane's day cursor, exactly
    /// like SoulMeta's cursors: one full pass per citizen per game day.
    /// Energy: drains with living, restored by sleep. Calm: builds by meditating,
    /// decays toward baseline on days without practice.
    /// </summary>
    public struct SoulVitals : IComponentData, ISerializable
    {
        public byte m_Energy;        // 0-255: 0 = spent, 255 = fully rested
        public byte m_Calm;          // 0-255: current serenity
        public byte m_SleepQuality;  // 0-255: last night's rest roll (story value)
        public uint m_LastDay;       // last day the vitals lane advanced

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Energy);
            writer.Write(m_Calm);
            writer.Write(m_SleepQuality);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("vitals.version", "SoulVitals read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Energy);
            reader.Read(out m_Calm);
            reader.Read(out m_SleepQuality);
            reader.Read(out m_LastDay);
        }
    }
}
