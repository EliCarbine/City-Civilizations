// Citizen Soul -- arts & talent state (M5d): F62-F71.
// SoulTalent is the hidden gift every soul carries. SoulArts is one soul's
// creative life -- skill, stage, renown and the story so far. SoulBand is a
// two-soul link between bandmates (added to both). All three persist via the
// Colossal IWriter/IReader pattern used across the mod.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>F62: the gift nobody knew was there -- latent forms plus the one that surfaced.</summary>
    public struct SoulTalent : IComponentData, ISerializable
    {
        public uint m_Latent;     // one bit per art form (see SoulArtForm)
        public byte m_Form;       // the form that surfaced (0 = not yet)
        public uint m_FoundDay;   // the day the gift surfaced

        public static SoulTalent Fresh(uint day)
        {
            SoulTalent t = default;
            t.m_FoundDay = day;
            return t;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Latent);
            writer.Write(m_Form);
            writer.Write(m_FoundDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Latent);
            reader.Read(out m_Form);
            reader.Read(out m_FoundDay);
        }
    }

    /// <summary>F62-F71: one soul's creative life -- craft, stage, renown and story.</summary>
    public struct SoulArts : IComponentData, ISerializable
    {
        public uint m_Flags;         // SoulArtsFlags bitset
        public byte m_Form;          // active art form (SoulArtForm)
        public byte m_Skill;         // 0-100 mastery of the craft
        public byte m_Stage;         // 0 unknown, 1 devoted, 2 accomplished, 3 renowned
        public byte m_Renown;        // 0-100 city renown
        public byte m_Regulars;      // busking regulars (cap 100)
        public byte m_Sessions;      // recent creative output (feeds finished works)
        public byte m_BuskedDays;    // afternoons spent busking
        public ushort m_Xp;          // practice xp toward the next skill point
        public Entity m_Partner;     // bandmate or dance partner (Null if none)
        public uint m_FoundDay;      // day the gift surfaced (mirror of talent)
        public uint m_LastDay;       // once-a-day dedupe, same as every system
        public uint m_NextBeatDay;   // pacing gate for practice beats
        public uint m_NextEventDay;  // pacing gate for commissions/fairs/comps

        public static SoulArts Fresh(uint day)
        {
            SoulArts a = default;
            a.m_FoundDay = day;
            return a;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Form);
            writer.Write(m_Skill);
            writer.Write(m_Stage);
            writer.Write(m_Renown);
            writer.Write(m_Regulars);
            writer.Write(m_Sessions);
            writer.Write(m_BuskedDays);
            writer.Write(m_Xp);
            writer.Write(m_Partner);
            writer.Write(m_FoundDay);
            writer.Write(m_LastDay);
            writer.Write(m_NextBeatDay);
            writer.Write(m_NextEventDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Form);
            reader.Read(out m_Skill);
            reader.Read(out m_Stage);
            reader.Read(out m_Renown);
            reader.Read(out m_Regulars);
            reader.Read(out m_Sessions);
            reader.Read(out m_BuskedDays);
            reader.Read(out m_Xp);
            reader.Read(out m_Partner);
            reader.Read(out m_FoundDay);
            reader.Read(out m_LastDay);
            reader.Read(out m_NextBeatDay);
            reader.Read(out m_NextEventDay);
        }
    }

    /// <summary>F63: two souls who decided to make noise together.</summary>
    public struct SoulBand : IComponentData, ISerializable
    {
        public Entity m_Mate;       // the other half of the band
        public byte m_Gigs;         // proper gigs played
        public uint m_FormedDay;
        public uint m_NextGigDay;

        public static SoulBand Fresh(uint day, Entity mate)
        {
            SoulBand b = default;
            b.m_Mate = mate;
            b.m_FormedDay = day;
            b.m_NextGigDay = day + 10u;
            return b;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Mate);
            writer.Write(m_Gigs);
            writer.Write(m_FormedDay);
            writer.Write(m_NextGigDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Mate);
            reader.Read(out m_Gigs);
            reader.Read(out m_FormedDay);
            reader.Read(out m_NextGigDay);
        }
    }

    /// <summary>F62-F71: the eight forms a gift can take.</summary>
    public static class SoulArtForm
    {
        public const int None = 0;
        public const int Music = 1;    // F64 busking / F63 band
        public const int Writing = 2;  // F65 novels
        public const int Painting = 3; // F66 murals
        public const int Photo = 4;    // F67 photography
        public const int Theater = 5;  // F68 the stage
        public const int Film = 6;     // F69 short films
        public const int Craft = 7;    // F70 handmade things
        public const int Dance = 8;    // F71 dancing
        public const int Count = 8;

        public static uint Bit(int form) => 1u << form;
    }

    /// <summary>F62-F71: story flags for a creative life.</summary>
    public static class SoulArtsFlags
    {
        public const uint TalentFound = 1u << 0;     // the gift surfaced
        public const uint GiftReshaped = 1u << 1;    // the craft became the life
        public const uint Busking = 1u << 2;         // plays the street corners
        public const uint StreetLegend = 1u << 3;    // a fixture on their corner
        public const uint Banded = 1u << 4;          // has a band
        public const uint BrokeOut = 1u << 5;        // made it out of small rooms
        public const uint FirstWork = 1u << 6;       // finished a manuscript
        public const uint Published = 1u << 7;       // saw their name in print
        public const uint Bestseller = 1u << 8;      // the whole city read it
        public const uint WritersGroup = 1u << 9;    // found a circle of writers
        public const uint Mural = 1u << 10;          // painted the city
        public const uint Commissioned = 1u << 11;   // painted for paying strangers
        public const uint PhotoExhibited = 1u << 12; // hung photos in public
        public const uint Opened = 1u << 13;         // opening night, once
        public const uint StageFright = 1u << 14;    // the lights were too bright
        public const uint Ovation = 1u << 15;        // standing ovation
        public const uint ShortFilm = 1u << 16;      // first short finished
        public const uint Viral = 1u << 17;          // a clip went around
        public const uint CraftFair = 1u << 18;      // sold at a street fair
        public const uint GiftMade = 1u << 19;       // handmade, just for someone
        public const uint Heirloom = 1u << 20;       // made to outlive them
        public const uint Partnered = 1u << 21;      // has a dance partner
        public const uint Competed = 1u << 22;       // danced before judges
        public const uint FirstDance = 1u << 23;     // the wedding dance
    }
}
