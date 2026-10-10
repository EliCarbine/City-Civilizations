// Citizen Soul -- SoulLifeComponents (M5c): reentry, midlife & wishes state.
// Save-compat rule: brand-new components only, version header kept as byte.
// The M3 SoulCrime / Imprisoned surfaces are READ, never extended.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    // Bit flags for SoulReentry.m_Flags.
    public static class SoulReentryFlags
    {
        public const byte ProbationActive = 1 << 0;  // reporting to a probation officer
        public const byte MentorBond = 1 << 1;       // someone finally believed in them
        public const byte SteadyJob = 1 << 2;        // a job that took a chance on them
        public const byte ProgramDone = 1 << 3;      // finished a reform program
        public const byte Cleared = 1 << 4;          // record cleared -- a clean slate
        public const byte Backslid = 1 << 5;         // old trouble pulled them back in
        public const byte ProbationDone = 1 << 6;    // finished probation clean
        public const byte InProgram = 1 << 7;        // currently in a reform program
    }

    // F36/F38: the long walk back after trouble -- probation, service, reform.
    public struct SoulReentry : IComponentData, ISerializable
    {
        public byte m_Flags;
        public Entity m_Mentor;
        public uint m_SinceDay;        // day the walk back began (0 = never in trouble)
        public uint m_ProbationUntil;  // probation end day
        public uint m_LastCheck;       // last probation check-in day
        public uint m_ProgramDay;      // day the current program started
        public uint m_ClearedDay;      // day the record cleared (0 = not yet)
        public uint m_LastDay;
        public byte m_CheckIns;        // check-ins completed
        public byte m_Violations;      // probation violations
        public byte m_ServiceDays;     // community service days done
        public byte m_Redemption;      // 0..RedeemNeed -- the record lifts at the cap

        public static SoulReentry Fresh(uint day)
        {
            SoulReentry r = default(SoulReentry);
            r.m_LastDay = day;
            return r;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_Flags);
            writer.Write(m_Mentor);
            writer.Write(m_SinceDay);
            writer.Write(m_ProbationUntil);
            writer.Write(m_LastCheck);
            writer.Write(m_ProgramDay);
            writer.Write(m_ClearedDay);
            writer.Write(m_LastDay);
            writer.Write(m_CheckIns);
            writer.Write(m_Violations);
            writer.Write(m_ServiceDays);
            writer.Write(m_Redemption);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("reentry.version." + version, "SoulReentry: read version " + version + ".");
            }
            reader.Read(out m_Flags);
            reader.Read(out m_Mentor);
            reader.Read(out m_SinceDay);
            reader.Read(out m_ProbationUntil);
            reader.Read(out m_LastCheck);
            reader.Read(out m_ProgramDay);
            reader.Read(out m_ClearedDay);
            reader.Read(out m_LastDay);
            reader.Read(out m_CheckIns);
            reader.Read(out m_Violations);
            reader.Read(out m_ServiceDays);
            reader.Read(out m_Redemption);
        }
    }

    // Wish kinds for SoulWishes slots (F57).
    public static class SoulWishKind
    {
        public const byte None = 0;
        public const byte MakePeace = 1;    // put the record behind them
        public const byte StayFree = 2;     // finish probation clean
        public const byte FindLove = 3;     // someone to come home to
        public const byte MasterCraft = 4;  // get truly good at the craft
        public const byte ServeCity = 5;    // pay the city back properly
    }

    // Bit flags for SoulWishes.m_Flags.
    public static class SoulWishFlags
    {
        public const byte MidlifeStirred = 1 << 0;  // the years asked for something new
        public const byte PivotDue = 1 << 1;        // a pivot is coming
        public const byte PivotDone = 1 << 2;       // the pivot happened
        public const byte SecondWind = 1 << 3;      // the warm years arrived
    }

    // F56/F57: midlife stirrings and the bucket list held against them.
    public struct SoulWishes : IComponentData, ISerializable
    {
        public byte m_Flags;
        public byte m_Count;         // active wishes
        public byte m_Fulfilled;
        public byte m_Faded;
        public uint m_NextWishDay;   // earliest day a new wish may form
        public uint m_PivotDay;      // day the midlife pivot lands
        public uint m_LastDay;
        public byte m_Kind0;
        public byte m_Kind1;
        public byte m_Kind2;
        public byte m_Kind3;
        public uint m_Made0;
        public uint m_Made1;
        public uint m_Made2;
        public uint m_Made3;

        public static SoulWishes Fresh(uint day)
        {
            SoulWishes w = default(SoulWishes);
            w.m_NextWishDay = day;
            w.m_LastDay = day;
            return w;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_Flags);
            writer.Write(m_Count);
            writer.Write(m_Fulfilled);
            writer.Write(m_Faded);
            writer.Write(m_NextWishDay);
            writer.Write(m_PivotDay);
            writer.Write(m_LastDay);
            writer.Write(m_Kind0);
            writer.Write(m_Kind1);
            writer.Write(m_Kind2);
            writer.Write(m_Kind3);
            writer.Write(m_Made0);
            writer.Write(m_Made1);
            writer.Write(m_Made2);
            writer.Write(m_Made3);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("wishes.version." + version, "SoulWishes: read version " + version + ".");
            }
            reader.Read(out m_Flags);
            reader.Read(out m_Count);
            reader.Read(out m_Fulfilled);
            reader.Read(out m_Faded);
            reader.Read(out m_NextWishDay);
            reader.Read(out m_PivotDay);
            reader.Read(out m_LastDay);
            reader.Read(out m_Kind0);
            reader.Read(out m_Kind1);
            reader.Read(out m_Kind2);
            reader.Read(out m_Kind3);
            reader.Read(out m_Made0);
            reader.Read(out m_Made1);
            reader.Read(out m_Made2);
            reader.Read(out m_Made3);
        }
    }
}
