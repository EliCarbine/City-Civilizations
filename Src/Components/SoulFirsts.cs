// ---------------------------------------------------------------------------
// SoulFirsts - F08 firsts: the once-in-a-lifetime social milestones (a soul's
// first true friend, its first crush), plus the shared once-per-day guard for
// the M2 adjunct drama lane (birthdays, confidants, feuds, wingmen).
//
// Purely additive: souls from older saves are migrated lazily by DramaSystem
// in budgeted batches, the same as every other M-lane component.
// ---------------------------------------------------------------------------
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>Once-in-a-life social milestones (F08) + the daily drama-pass guard.</summary>
    public struct SoulFirsts : IComponentData, ISerializable
    {
        public byte m_Bits;     // SoulFirstsFlags: which firsts have happened
        public uint m_LastDay;  // last day the drama pass ran for this soul

        public static SoulFirsts Fresh(uint day)
        {
            return new SoulFirsts { m_Bits = 0, m_LastDay = day };
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Bits);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("firsts.version", "SoulFirsts save data is newer than this build; unknown fields are skipped.");
            }
            reader.Read(out m_Bits);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>Bits in SoulFirsts.m_Bits: the moments you only get once.</summary>
    public static class SoulFirstsFlags
    {
        public const byte CloseFriend = 1;  // the first true-blue close friend
        public const byte Crush = 2;        // the first spark of romance
    }
}
