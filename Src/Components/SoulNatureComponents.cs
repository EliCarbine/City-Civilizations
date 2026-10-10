// Citizen Soul -- M5i: SoulNature component (F109-F116).
// The green and the wild: windowsill gardens and prized roses, bird life
// lists, trails and campfires, the pier at dawn, mushroom season, street
// cats and rescue glow-ups, weather that moves whole moods, and the fight
// for one old tree. Postures live in m_Flags; story-memories in m_Flags2.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulNatureFlags
    {
        public const byte Grower = 1 << 0;     // F109: keeps a garden
        public const byte Birder = 1 << 1;     // F110: keeps a life list
        public const byte Hiker = 1 << 2;      // F111: weekend warrior
        public const byte Angler = 1 << 3;     // F112: pier regular
        public const byte Forager = 1 << 4;    // F113: knows the spots
        public const byte CatFriend = 1 << 5;  // F114: feeds the street cats
        public const byte TreeAngel = 1 << 6;  // F116: fights for old trees
        public const byte Adopted = 1 << 7;    // F114: one of them came home
    }

    public static class SoulNatureFlags2
    {
        public const byte TripStory = 1 << 0;  // F111: the near-miss trip
        public const byte EagleSeen = 1 << 1;  // F110: there for the eagles
        public const byte BadMush = 1 << 2;    // F113: the wrong mushroom
        public const byte RosePrized = 1 << 3; // F109: the prized rose bush
        public const byte GlowDone = 1 << 4;   // F114: the rescue glow-up
    }

    public struct SoulNature : IComponentData, ISerializable
    {
        public byte m_Flags;
        public byte m_Flags2;
        public uint m_List;      // F110: life-list count of birds spotted
        public byte m_Catch;     // F112: the big one, growing in the telling
        public byte m_Luck;      // F109: green thumb vs plant murderer index
        public Entity m_Buddy;   // trail friend / fishing pal / swap partner
        public uint m_LastDay;

        public static SoulNature Fresh(uint day)
        {
            SoulNature n = default;
            n.m_Luck = (byte)(day % 200u);
            n.m_LastDay = day;
            return n;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_List);
            writer.Write(m_Catch);
            writer.Write(m_Luck);
            writer.Write(m_Buddy);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_List);
            reader.Read(out m_Catch);
            reader.Read(out m_Luck);
            reader.Read(out m_Buddy);
            reader.Read(out m_LastDay);
        }
    }
}
