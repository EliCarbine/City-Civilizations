// Citizen Soul -- M5h: SoulCivic component (F100-F108).
// The civic life: volunteering and fatigue, the watch, the garden plots
// and tomato feuds, petitions and marches, town halls, giving, jury duty,
// block parties -- and the crisis that binds a street together.
// One component per soul; all civic surfaces read/write it.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F100-F108: volunteering, watch, gardens, petitions, town halls,
    /// giving, jury duty, block life, crisis response.
    /// </summary>
    public struct SoulCivic : IComponentData, ISerializable
    {
        public byte m_Flags;       // SoulCivicFlags
        public byte m_Flags2;      // SoulCivicFlags2 (jury, foreman)
        public byte m_Cause;       // F103: the issue they organize around
        public byte m_Repute;      // quiet-hero standing, capped
        public byte m_Clout;       // F104: town-hall standing after speeches
        public ushort m_Acts;      // good works tallied, capped
        public Entity m_Mate;      // watch partner / garden rival / volunteer friend
        public uint m_PetitionDay; // F103: when the current petition started (0 = none)
        public uint m_LastDay;

        public static SoulCivic Fresh(uint day)
        {
            SoulCivic c = default;
            c.m_LastDay = day;
            c.m_Mate = Entity.Null;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_Cause);
            writer.Write(m_Repute);
            writer.Write(m_Clout);
            writer.Write(m_Acts);
            writer.Write(m_Mate);
            writer.Write(m_PetitionDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_Cause);
            reader.Read(out m_Repute);
            reader.Read(out m_Clout);
            reader.Read(out m_Acts);
            reader.Read(out m_Mate);
            reader.Read(out m_PetitionDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>Long-lived civic postures.</summary>
    public static class SoulCivicFlags
    {
        public const byte None = 0;
        public const byte Volunteer = 1 << 0;
        public const byte Watch = 1 << 1;
        public const byte WatchOver = 1 << 2;
        public const byte Gardener = 1 << 3;
        public const byte Petitioner = 1 << 4;
        public const byte TownRegular = 1 << 5;
        public const byte Charitable = 1 << 6;
        public const byte CrisisHelper = 1 << 7;
    }

    /// <summary>Second flag byte: one-shot honors that become memories.</summary>
    public static class SoulCivicFlags2
    {
        public const byte None = 0;
        public const byte JuryServed = 1 << 0;
        public const byte Foreman = 1 << 1;
    }
}
