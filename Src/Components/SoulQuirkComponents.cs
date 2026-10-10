// Citizen Soul -- M5j: SoulQuirk component (F117-F124).
// The odd little bits of a person: the lucky ritual, the collection, the
// one big fear, the catchphrase, the notebooks, the lists, the comfort
// object, and the private yearly tradition.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulQuirkFlags
    {
        public const byte Ritual = 1 << 0;      // F117: runs on lucky rituals
        public const byte Collector = 1 << 1;   // F118: never stopped collecting
        public const byte Fearful = 1 << 2;     // F119: one big fear, named it
        public const byte Phrase = 1 << 3;      // F120: coined a catchphrase
        public const byte StatKeeper = 1 << 4;  // F121: logs everything
        public const byte Lister = 1 << 5;      // F122: builds little systems
        public const byte CharmKeeper = 1 << 6; // F123: keeps a comfort object
        public const byte Tradition = 1 << 7;   // F124: keeps a personal tradition
    }

    public static class SoulQuirkFlags2
    {
        public const byte LuckyWin = 1 << 0;    // F117: the lucky socks came through
        public const byte SetDone = 1 << 1;     // F118: the set is complete
        public const byte FearShrunk = 1 << 2;  // F119: faced it and won
        public const byte SlangCity = 1 << 3;   // F120: the phrase went city-wide
        public const byte Archived = 1 << 4;    // F121: notebooks joined the archive
        public const byte CharmLost = 1 << 5;   // F123: the charm went missing
        public const byte TradBroken = 1 << 6;  // F124: the tradition broke
    }

    public static class SoulQuirkFlags3
    {
        public const byte RitualShared = 1 << 0; // F117: friends picked it up
        public const byte Exposure = 1 << 1;     // F119: started facing the fear
        public const byte TradShared = 1 << 2;   // F124: loved ones joined in
        public const byte Nicknamed = 1 << 3;    // F120: the nickname stuck
    }

    public struct SoulQuirk : IComponentData, ISerializable
    {
        public byte m_Flags;       // SoulQuirkFlags
        public byte m_Flags2;      // SoulQuirkFlags2
        public byte m_Flags3;      // SoulQuirkFlags3
        public uint m_Pieces;      // F118: collection size
        public byte m_Fear;        // F119: 1-8 which fear, 0 = none
        public byte m_Charm;       // F123: 1-8 which comfort object, 0 = none
        public byte m_Word;        // F120: 1-16 which catchphrase, 0 = none
        public Entity m_Companion; // F120: the friend the nickname stuck to
        public uint m_LastDay;

        public static SoulQuirk Fresh(uint day)
        {
            SoulQuirk q = default;
            q.m_LastDay = day;
            return q;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_Flags3);
            writer.Write(m_Pieces);
            writer.Write(m_Fear);
            writer.Write(m_Charm);
            writer.Write(m_Word);
            writer.Write(m_Companion);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_Flags3);
            reader.Read(out m_Pieces);
            reader.Read(out m_Fear);
            reader.Read(out m_Charm);
            reader.Read(out m_Word);
            reader.Read(out m_Companion);
            reader.Read(out m_LastDay);
        }
    }
}
