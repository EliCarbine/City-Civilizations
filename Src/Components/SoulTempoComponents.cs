// Citizen Soul -- M5j-ii: SoulTempo component (F125-F126).
// How a person talks and how a person keeps time: a talker's voice or a
// listener's, the duo the two of them became, the quiet truths kept --
// and the hours (early bird, night owl, snoozer), the lateness, and the
// punctual ones who end up waiting for everyone else.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulTempoFlags
    {
        public const byte Talker = 1 << 0;      // F125: found their voice in a room
        public const byte Listener = 1 << 1;    // F125: people tell them things
        public const byte Duo = 1 << 2;         // F125: the two found each other
        public const byte EarlyBird = 1 << 3;   // F126: first one up
        public const byte NightOwl = 1 << 4;    // F126: the city is theirs at night
        public const byte Snoozer = 1 << 5;     // F126: mornings never met them
        public const byte Punctual = 1 << 6;    // F126: never late, ever
    }

    public static class SoulTempoFlags2
    {
        public const byte Confided = 1 << 0;      // F125: trusted with something quiet
        public const byte LateAccounted = 1 << 1; // F126: friends stopped minding
    }

    public struct SoulTempo : IComponentData, ISerializable
    {
        public byte m_Flags;     // SoulTempoFlags
        public byte m_Flags2;    // SoulTempoFlags2
        public byte m_Late;      // F126: times this one made someone wait
        public byte m_Confided;  // F125: quiet truths kept, a tally
        public Entity m_Buddy;   // F125: the other half of the duo
        public uint m_LastDay;

        public static SoulTempo Fresh(uint day)
        {
            SoulTempo t = default;
            t.m_LastDay = day;
            return t;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_Late);
            writer.Write(m_Confided);
            writer.Write(m_Buddy);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_Late);
            reader.Read(out m_Confided);
            reader.Read(out m_Buddy);
            reader.Read(out m_LastDay);
        }
    }
}
