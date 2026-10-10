// Citizen Soul -- technology & modern life state (M5g): F91-F99.
// Screens and posting; group chats and streaming nights; gadget eras and
// holdouts; remote work; arcades and forums; and the digital memory a
// soul leaves the city. Written only by TechSystem.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Tech-life bits carried in SoulTech.m_Flags.</summary>
    public static class SoulTechFlags
    {
        public const byte ScreenHeavy = 1 << 0;  // always on the phone (F91)
        public const byte Viral = 1 << 1;        // had the post that ran the town (F92)
        public const byte Chat = 1 << 2;         // in the group chat (F93)
        public const byte Binger = 1 << 3;       // the streaming habit (F94)
        public const byte Arcader = 1 << 4;      // arcade regular (F97)
        public const byte Forum = 1 << 5;        // forum member (F98)
        public const byte Remote = 1 << 6;       // works from the cafés (F96)
        public const byte Legacy = 1 << 7;       // the archive speaks for them (F99)
    }

    /// <summary>
    /// F91-F99: phones, posts, chats, streams, gadgets, remote work,
    /// arcades, forums, and digital legacy. Written only by TechSystem.
    /// </summary>
    public struct SoulTech : IComponentData, ISerializable
    {
        public byte m_Flags;      // SoulTechFlags
        public uint m_Screen;     // 0..100 screen habit
        public uint m_Followers;  // the little audience
        public uint m_Posts;      // posts, nights, visits counted
        public Entity m_Mate;     // chat anchor, forum friend, arcade rival
        public uint m_LastDay;

        public static SoulTech Fresh(uint day)
        {
            SoulTech t = default(SoulTech);
            t.m_LastDay = day;
            return t;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Flags);
            writer.Write(m_Screen);
            writer.Write(m_Followers);
            writer.Write(m_Posts);
            writer.Write(m_Mate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("tech.version", "SoulTech read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Flags);
            reader.Read(out m_Screen);
            reader.Read(out m_Followers);
            reader.Read(out m_Posts);
            reader.Read(out m_Mate);
            reader.Read(out m_LastDay);
        }
    }
}
