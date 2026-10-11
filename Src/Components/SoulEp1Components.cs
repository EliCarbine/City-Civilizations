// Citizen Soul — EP1 (Extended Play) components.
// One small struct per deferred feature, in the house style: flags byte(s)
// batched eight bits at a time, day stamps, one Entity link, one variant byte.
// F20 mortality, F28 strikes, F29 gambling, F34 underworld, F37 graft,
// F39 routines, F40 food, F41 faith, F44 nightlife, F45 fandoms, F46 style,
// F48 holidays, F49 hood, F50 gossip, F51 standing, F54 mentorship, F59 chronicle.

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    // ---- F20: mortality — the last chapter -------------------------------

    public static class SoulMortalityFlags
    {
        public const byte Declining = 1 << 0;
        public const byte BucketList = 1 << 1;
        public const byte BucketDone = 1 << 2;
        public const byte Farewell = 1 << 3;
        public const byte LastWords = 1 << 4;
        public const byte Eulogy = 1 << 5;
    }

    public struct SoulMortality : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulMortalityFlags
        public uint m_DeclineDay;     // the quiet began
        public uint m_BucketDay;      // wrote the list
        public uint m_FarewellDay;    // the tour of goodbyes
        public uint m_WordsDay;       // said the thing that mattered
        public uint m_EulogyDay;      // remembered by someone
        public Entity m_EulogyBy;     // the one who spoke
        public uint m_LastDay;

        public static SoulMortality Fresh(uint day)
        {
            SoulMortality c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_DeclineDay);
            writer.Write(m_BucketDay);
            writer.Write(m_FarewellDay);
            writer.Write(m_WordsDay);
            writer.Write(m_EulogyDay);
            writer.Write(m_EulogyBy);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_DeclineDay);
            reader.Read(out m_BucketDay);
            reader.Read(out m_FarewellDay);
            reader.Read(out m_WordsDay);
            reader.Read(out m_EulogyDay);
            reader.Read(out m_EulogyBy);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F28: strikes — the shop walks out -------------------------------

    public static class SoulLaborFlags
    {
        public const byte Rumors = 1 << 0;
        public const byte Unionized = 1 << 1;
        public const byte Picket = 1 << 2;
        public const byte Striking = 1 << 3;
        public const byte Settled = 1 << 4;
        public const byte Blacklisted = 1 << 5;
    }

    public struct SoulLabor : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulLaborFlags
        public uint m_UnionDay;       // first meeting in the back room
        public uint m_StrikeDay;      // the line went up
        public uint m_SettleDay;      // hands shaken / terms read
        public Entity m_Comrade;      // the one on the line beside them
        public uint m_LastDay;

        public static SoulLabor Fresh(uint day)
        {
            SoulLabor c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_UnionDay);
            writer.Write(m_StrikeDay);
            writer.Write(m_SettleDay);
            writer.Write(m_Comrade);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_UnionDay);
            reader.Read(out m_StrikeDay);
            reader.Read(out m_SettleDay);
            reader.Read(out m_Comrade);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F29: gambling — the pull of the table ---------------------------

    public static class SoulGambleFlags
    {
        public const byte Regular = 1 << 0;
        public const byte BigWin = 1 << 1;
        public const byte BigLoss = 1 << 2;
        public const byte Debt = 1 << 3;
        public const byte LuckyName = 1 << 4;
    }

    public struct SoulGamble : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulGambleFlags
        public byte m_Variant;        // 1..4: cards, dice, tickets, wagers
        public byte m_Streak;         // nights in a row
        public uint m_LastBetDay;
        public uint m_WinDay;         // the night it all landed
        public uint m_LossDay;        // the night it all went
        public uint m_LastDay;

        public static SoulGamble Fresh(uint day)
        {
            SoulGamble c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Variant);
            writer.Write(m_Streak);
            writer.Write(m_LastBetDay);
            writer.Write(m_WinDay);
            writer.Write(m_LossDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Variant);
            reader.Read(out m_Streak);
            reader.Read(out m_LastBetDay);
            reader.Read(out m_WinDay);
            reader.Read(out m_LossDay);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F34: the underworld — quiet work, long memory -------------------

    public static class SoulUnderFlags
    {
        public const byte Runner = 1 << 0;
        public const byte Made = 1 << 1;
        public const byte Launderer = 1 << 2;
        public const byte Witness = 1 << 3;
        public const byte Reformed = 1 << 4;
        public const byte Fallen = 1 << 5;
    }

    public struct SoulUnderworld : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulUnderFlags
        public byte m_Rank;           // 1..3
        public uint m_JoinDay;
        public uint m_JobDay;         // the last job that went fine
        public uint m_RankDay;        // the day they stopped being a runner
        public Entity m_Mate;         // the one who brought them in
        public uint m_LastDay;

        public static SoulUnderworld Fresh(uint day)
        {
            SoulUnderworld c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Rank);
            writer.Write(m_JoinDay);
            writer.Write(m_JobDay);
            writer.Write(m_RankDay);
            writer.Write(m_Mate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Rank);
            reader.Read(out m_JoinDay);
            reader.Read(out m_JobDay);
            reader.Read(out m_RankDay);
            reader.Read(out m_Mate);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F37: graft — the envelope on the desk ---------------------------

    public static class SoulGraftFlags
    {
        public const byte Offered = 1 << 0;
        public const byte Took = 1 << 1;
        public const byte Refused = 1 << 2;
        public const byte Exposed = 1 << 3;
        public const byte Survived = 1 << 4;
        public const byte CleanHands = 1 << 5;
    }

    public struct SoulGraft : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulGraftFlags
        public uint m_OfferDay;
        public uint m_TakeDay;
        public uint m_ExposeDay;
        public Entity m_Target;       // the offerer
        public uint m_LastDay;

        public static SoulGraft Fresh(uint day)
        {
            SoulGraft c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_OfferDay);
            writer.Write(m_TakeDay);
            writer.Write(m_ExposeDay);
            writer.Write(m_Target);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_OfferDay);
            reader.Read(out m_TakeDay);
            reader.Read(out m_ExposeDay);
            reader.Read(out m_Target);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F39: routines — the shape of an ordinary week -------------------

    public static class SoulRoutineFlags
    {
        public const byte HasSpot = 1 << 0;
        public const byte Upended = 1 << 1;
        public const byte NewRoutine = 1 << 2;
        public const byte Misses = 1 << 3;
    }

    public struct SoulRoutine : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulRoutineFlags
        public byte m_Streak;         // weeks the routine held
        public uint m_SpotDay;        // found the place that fits
        public uint m_BreakDay;       // the routine cracked
        public uint m_LastDay;

        public static SoulRoutine Fresh(uint day)
        {
            SoulRoutine c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Streak);
            writer.Write(m_SpotDay);
            writer.Write(m_BreakDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Streak);
            reader.Read(out m_SpotDay);
            reader.Read(out m_BreakDay);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F40: food — the table is where souls sit ------------------------

    public static class SoulPalateFlags
    {
        public const byte HomeCook = 1 << 0;
        public const byte StreetFan = 1 << 1;
        public const byte Signature = 1 << 2;
        public const byte SharedMeal = 1 << 3;
    }

    public struct SoulPalate : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulPalateFlags
        public byte m_Variant;        // 1..4: the signature dish family
        public uint m_DishDay;        // the dish became theirs
        public uint m_BiteDay;        // last night at the stalls
        public uint m_MealDay;        // last dinner that mattered
        public Entity m_DishMate;     // who they cooked for
        public uint m_LastDay;

        public static SoulPalate Fresh(uint day)
        {
            SoulPalate c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Variant);
            writer.Write(m_DishDay);
            writer.Write(m_BiteDay);
            writer.Write(m_MealDay);
            writer.Write(m_DishMate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Variant);
            reader.Read(out m_DishDay);
            reader.Read(out m_BiteDay);
            reader.Read(out m_MealDay);
            reader.Read(out m_DishMate);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F41: faith — seeking, finding, doubting, mourning ---------------

    public static class SoulFaithFlags
    {
        public const byte Seeking = 1 << 0;
        public const byte Praying = 1 << 1;
        public const byte Converted = 1 << 2;
        public const byte Devout = 1 << 3;
        public const byte Doubting = 1 << 4;
        public const byte Bereaved = 1 << 5;
    }

    public struct SoulFaith : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulFaithFlags
        public byte m_Streak;         // weeks of observance
        public uint m_SeekDay;        // started asking
        public uint m_ConvertDay;     // stopped asking
        public uint m_DoubtDay;       // the question came back
        public uint m_BereaveDay;     // lost someone, lit a candle
        public Entity m_Guide;        // who walked with them
        public uint m_LastDay;

        public static SoulFaith Fresh(uint day)
        {
            SoulFaith c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Streak);
            writer.Write(m_SeekDay);
            writer.Write(m_ConvertDay);
            writer.Write(m_DoubtDay);
            writer.Write(m_BereaveDay);
            writer.Write(m_Guide);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Streak);
            reader.Read(out m_SeekDay);
            reader.Read(out m_ConvertDay);
            reader.Read(out m_DoubtDay);
            reader.Read(out m_BereaveDay);
            reader.Read(out m_Guide);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F44: nightlife — the city after dark ----------------------------

    public static class SoulNightFlags
    {
        public const byte Regular = 1 << 0;
        public const byte BigNight = 1 << 1;
        public const byte Hangover = 1 << 2;
        public const byte Afterparty = 1 << 3;
    }

    public struct SoulNight : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulNightFlags
        public byte m_Streak;         // weekends in a row
        public uint m_NightDay;       // the big one
        public uint m_AftersDay;      // when one place wasn't enough
        public Entity m_Crew;         // the one they closed the place with
        public uint m_LastDay;

        public static SoulNight Fresh(uint day)
        {
            SoulNight c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Streak);
            writer.Write(m_NightDay);
            writer.Write(m_AftersDay);
            writer.Write(m_Crew);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Streak);
            reader.Read(out m_NightDay);
            reader.Read(out m_AftersDay);
            reader.Read(out m_Crew);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F45: fandoms — colors worth arguing about -----------------------

    public static class SoulFandomFlags
    {
        public const byte Fan = 1 << 0;
        public const byte MatchNight = 1 << 1;
        public const byte WatchParty = 1 << 2;
        public const byte Switched = 1 << 3;
        public const byte Rival = 1 << 4;
    }

    public struct SoulFandom : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulFandomFlags
        public byte m_Variant;        // 1..4: the colors they wear
        public uint m_FanDay;         // picked a side
        public uint m_MatchDay;       // last big night of sport
        public uint m_SwitchDay;      // the heresy, or the homecoming
        public Entity m_RivalFan;     // the friend who chose wrong
        public uint m_LastDay;

        public static SoulFandom Fresh(uint day)
        {
            SoulFandom c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Variant);
            writer.Write(m_FanDay);
            writer.Write(m_MatchDay);
            writer.Write(m_SwitchDay);
            writer.Write(m_RivalFan);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Variant);
            reader.Read(out m_FanDay);
            reader.Read(out m_MatchDay);
            reader.Read(out m_SwitchDay);
            reader.Read(out m_RivalFan);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F46: fashion — how a soul shows up ------------------------------

    public static class SoulStyleFlags
    {
        public const byte FoundStyle = 1 << 0;
        public const byte Statement = 1 << 1;
        public const byte Throwback = 1 << 2;
        public const byte Complimented = 1 << 3;
    }

    public struct SoulStyle : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulStyleFlags
        public byte m_Variant;        // 1..5: the look
        public uint m_StyleDay;       // the look became theirs
        public uint m_StatementDay;   // wore it loud
        public uint m_LastDay;

        public static SoulStyle Fresh(uint day)
        {
            SoulStyle c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Variant);
            writer.Write(m_StyleDay);
            writer.Write(m_StatementDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Variant);
            reader.Read(out m_StyleDay);
            reader.Read(out m_StatementDay);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F48: holidays — the calendar of a life --------------------------

    public static class SoulFestiveFlags
    {
        public const byte Hosts = 1 << 0;
        public const byte Fireworks = 1 << 1;
        public const byte Parade = 1 << 2;
        public const byte Feast = 1 << 3;
    }

    public struct SoulFestive : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulFestiveFlags
        public uint m_FestDay;        // last festival they gave themselves to
        public uint m_HostDay;        // the door was open
        public uint m_FeastDay;       // the long table
        public Entity m_FeastMate;    // who got the last plate
        public uint m_LastDay;

        public static SoulFestive Fresh(uint day)
        {
            SoulFestive c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_FestDay);
            writer.Write(m_HostDay);
            writer.Write(m_FeastDay);
            writer.Write(m_FeastMate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_FestDay);
            reader.Read(out m_HostDay);
            reader.Read(out m_FeastDay);
            reader.Read(out m_FeastMate);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F49: the hood — loyalty to a few blocks -------------------------

    public static class SoulHoodFlags
    {
        public const byte Rooted = 1 << 0;
        public const byte Banner = 1 << 1;
        public const byte BlockParty = 1 << 2;
        public const byte Moved = 1 << 3;
    }

    public struct SoulHood : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulHoodFlags
        public uint m_RootDay;        // put down roots
        public uint m_PrideDay;       // something glad happened here
        public uint m_GoneDay;        // said goodbye to the block
        public Entity m_BlockMate;    // the neighbor who knows everyone
        public uint m_LastDay;

        public static SoulHood Fresh(uint day)
        {
            SoulHood c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_RootDay);
            writer.Write(m_PrideDay);
            writer.Write(m_GoneDay);
            writer.Write(m_BlockMate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_RootDay);
            reader.Read(out m_PrideDay);
            reader.Read(out m_GoneDay);
            reader.Read(out m_BlockMate);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F50: gossip — words with weight ---------------------------------

    public static class SoulGossipFlags
    {
        public const byte Source = 1 << 0;
        public const byte Regret = 1 << 1;
        public const byte Mediator = 1 << 2;
        public const byte Burned = 1 << 3;
    }

    public struct SoulGossip : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulGossipFlags
        public uint m_ShareDay;       // told the thing they shouldn't have
        public uint m_HurtDay;        // heard the thing about themselves
        public uint m_MendDay;        // the apology stuck
        public Entity m_Subject;      // about whom
        public uint m_LastDay;

        public static SoulGossip Fresh(uint day)
        {
            SoulGossip c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_ShareDay);
            writer.Write(m_HurtDay);
            writer.Write(m_MendDay);
            writer.Write(m_Subject);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_ShareDay);
            reader.Read(out m_HurtDay);
            reader.Read(out m_MendDay);
            reader.Read(out m_Subject);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F51: standing — what the city says about you --------------------

    public static class SoulStandingFlags
    {
        public const byte Known = 1 << 0;
        public const byte Legend = 1 << 1;
        public const byte Tarnished = 1 << 2;
        public const byte Redeemed = 1 << 3;
    }

    public struct SoulStanding : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulStandingFlags
        public byte m_Peak;           // 0-255, the talk at its height
        public uint m_PeakDay;
        public uint m_LegendDay;
        public uint m_TarnishDay;
        public uint m_RedeemDay;
        public uint m_LastDay;

        public static SoulStanding Fresh(uint day)
        {
            SoulStanding c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Peak);
            writer.Write(m_PeakDay);
            writer.Write(m_LegendDay);
            writer.Write(m_TarnishDay);
            writer.Write(m_RedeemDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Peak);
            reader.Read(out m_PeakDay);
            reader.Read(out m_LegendDay);
            reader.Read(out m_TarnishDay);
            reader.Read(out m_RedeemDay);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F54: mentorship — passing the craft on --------------------------

    public static class SoulMentorFlags
    {
        public const byte Mentored = 1 << 0;
        public const byte Mentoring = 1 << 1;
        public const byte ProtegeWon = 1 << 2;
        public const byte Parted = 1 << 3;
        public const byte Gift = 1 << 4;
    }

    public struct SoulMentor : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulMentorFlags
        public byte m_Wins;           // protege milestones witnessed
        public uint m_BondDay;        // the craft changed hands
        public uint m_LessonDay;      // last day of teaching
        public uint m_PartDay;        // the goodbye, when it came
        public Entity m_Protege;      // the one learning / the one taught
        public uint m_LastDay;

        public static SoulMentor Fresh(uint day)
        {
            SoulMentor c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Wins);
            writer.Write(m_BondDay);
            writer.Write(m_LessonDay);
            writer.Write(m_PartDay);
            writer.Write(m_Protege);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Wins);
            reader.Read(out m_BondDay);
            reader.Read(out m_LessonDay);
            reader.Read(out m_PartDay);
            reader.Read(out m_Protege);
            reader.Read(out m_LastDay);
        }
    }

    // ---- F59: the chronicle — the paper remembers ------------------------

    public static class SoulChronicleFlags
    {
        public const byte Covered = 1 << 0;
        public const byte FrontPage = 1 << 1;
        public const byte Obituary = 1 << 2;
    }

    public struct SoulChronicle : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulChronicleFlags
        public uint m_CoverDay;       // first time the paper spelled it right
        public uint m_ObituaryDay;    // the last write-up
        public uint m_LastDay;

        public static SoulChronicle Fresh(uint day)
        {
            SoulChronicle c = default;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_CoverDay);
            writer.Write(m_ObituaryDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_CoverDay);
            reader.Read(out m_ObituaryDay);
            reader.Read(out m_LastDay);
        }
    }
}
