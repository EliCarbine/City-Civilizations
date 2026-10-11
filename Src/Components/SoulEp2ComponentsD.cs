// Citizen Soul -- EP2 components D: the broadcast house (F179-F183).
// A city station: a season of shows nobody planned, a news desk with one
// headline a day, and the premieres the whole neighborhood talks about.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>What slot a show airs in (F179).</summary>
    public static class SoulShowKind
    {
        public const byte News = 0;
        public const byte Drama = 1;
        public const byte Comedy = 2;
        public const byte Reality = 3;
        public const byte Cartoon = 4;
        public const byte Count = 5;
    }

    /// <summary>
    /// F179-F182: the city's one station as a singleton -- a season rolls
    /// every few weeks, a lineup nobody planned but everybody half-watches.
    /// Created and written only by BroadcastSystem.
    /// </summary>
    public struct SoulTv : IComponentData, ISerializable
    {
        public uint m_Season;      // season number, 1-based
        public uint m_SeasonStart; // day the current season premiered
        public byte m_Seed1;       // lineup seeds (city's creative streak)
        public byte m_Seed2;
        public uint m_LastDay;

        public static SoulTv Fresh(uint day)
        {
            SoulTv t = default(SoulTv);
            t.m_Season = 1;
            t.m_SeasonStart = day;
            uint h = (day + 7u) * 2654435761u;
            t.m_Seed1 = (byte)(h % 24u);
            t.m_Seed2 = (byte)((h >> 8) % 24u);
            t.m_LastDay = day;
            return t;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Season);
            writer.Write(m_SeasonStart);
            writer.Write(m_Seed1);
            writer.Write(m_Seed2);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("tv.version", "SoulTv read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Season);
            reader.Read(out m_SeasonStart);
            reader.Read(out m_Seed1);
            reader.Read(out m_Seed2);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>One show on the current season's lineup (F179-F182).</summary>
    public struct SoulAir : IBufferElementData, ISerializable
    {
        public byte m_Kind;       // SoulShowKind
        public byte m_Name1;      // title word A (0..23)
        public byte m_Name2;      // title word B (0..23)
        public byte m_ShowId;     // 0..23 identity; favorite tracking uses this
        public short m_Hotness;   // buzz; premieres spike it, time cools it
        public uint m_PremiereDay;

        public static SoulAir Make(byte kind, byte n1, byte n2, byte id, uint day)
        {
            SoulAir a = default(SoulAir);
            a.m_Kind = kind;
            a.m_Name1 = n1;
            a.m_Name2 = n2;
            a.m_ShowId = id;
            a.m_PremiereDay = day;
            a.m_Hotness = 30;
            return a;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Name1);
            writer.Write(m_Name2);
            writer.Write(m_ShowId);
            writer.Write(m_Hotness);
            writer.Write(m_PremiereDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("air.version", "SoulAir read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Name1);
            reader.Read(out m_Name2);
            reader.Read(out m_ShowId);
            reader.Read(out m_Hotness);
            reader.Read(out m_PremiereDay);
        }
    }

    /// <summary>Story sectors the wire sorts in (F183).</summary>
    public static class SoulWire
    {
        public const byte Money = 1;    // a fortune moved
        public const byte Venture = 2;  // a sign went up or came down
        public const byte Market = 3;   // the exchange did something
        public const byte Screen = 4;   // television did something
        public const byte Life = 5;     // somebody's road bent
    }

    /// <summary>
    /// F183: the news desk as a singleton -- scoops arrive from every other
    /// system through the wire, one leads the day. Created and written only
    /// by NewsroomSystem (the wire itself is written by anyone with a scoop).
    /// </summary>
    public struct SoulNews : IComponentData, ISerializable
    {
        public byte m_TopKind;
        public byte m_TopPrio;
        public short m_TopValue;
        public Entity m_TopAbout;
        public uint m_TopDay;
        public uint m_LastDay;

        public static SoulNews Fresh(uint day)
        {
            SoulNews n = default(SoulNews);
            n.m_LastDay = day;
            return n;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_TopKind);
            writer.Write(m_TopPrio);
            writer.Write(m_TopValue);
            writer.Write(m_TopAbout);
            writer.Write(m_TopDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("news.version", "SoulNews read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_TopKind);
            reader.Read(out m_TopPrio);
            reader.Read(out m_TopValue);
            reader.Read(out m_TopAbout);
            reader.Read(out m_TopDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>A story waiting to be sorted by the desk (F183).</summary>
    public struct SoulScoop : IBufferElementData, ISerializable
    {
        public byte m_Kind;      // SoulWire
        public byte m_Prio;      // weight for "what leads"
        public short m_Value;    // magnitude (index points, dollars, ...)
        public Entity m_About;   // who it's about (Entity.Null for the city)
        public uint m_Day;

        public static SoulScoop Make(byte kind, byte prio, short value, Entity about, uint day)
        {
            SoulScoop s = default(SoulScoop);
            s.m_Kind = kind;
            s.m_Prio = prio;
            s.m_Value = value;
            s.m_About = about;
            s.m_Day = day;
            return s;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Prio);
            writer.Write(m_Value);
            writer.Write(m_About);
            writer.Write(m_Day);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("scoop.version", "SoulScoop read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Prio);
            reader.Read(out m_Value);
            reader.Read(out m_About);
            reader.Read(out m_Day);
        }
    }

    /// <summary>A headline that led a day -- the city's flickering archive (F183).</summary>
    public struct SoulHeadline : IBufferElementData, ISerializable
    {
        public byte m_Kind;      // SoulWire
        public short m_Value;
        public Entity m_About;
        public uint m_Day;

        public static SoulHeadline Make(byte kind, short value, Entity about, uint day)
        {
            SoulHeadline h = default(SoulHeadline);
            h.m_Kind = kind;
            h.m_Value = value;
            h.m_About = about;
            h.m_Day = day;
            return h;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Value);
            writer.Write(m_About);
            writer.Write(m_Day);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("headline.version", "SoulHeadline read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Value);
            reader.Read(out m_About);
            reader.Read(out m_Day);
        }
    }
}
