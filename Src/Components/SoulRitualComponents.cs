// Citizen Soul -- M5m: SoulRitual component (F142-F151).
// Life's ceremonies, in order: the driving test at sixteen, the first
// apartment and the noodle era after it, the walk across a graduation
// stage, the first paycheck, the toast, the plaque thirty years on,
// dinner in a place of one's own, a renovation and a garden, the year
// turning over with a resolution attached, the city's own birthdays,
// a send-off and the letters that follow, the reunion night, and the
// day a kid walks out of childhood on the street's terms. Extends F129
// (the driving half; travel still holds the keys at the end) and
// F117/F124 (the private rituals belong to quirks; these are public).
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulRitualFlags
    {
        public const byte DriversEd = 1 << 0;    // F142: signed up and stalled twice
        public const byte Licensed = 1 << 1;     // F142: passed the driving test
        public const byte Apartment = 1 << 2;    // F143: a place of their own
        public const byte Housewarming = 1 << 3; // F143: borrowed chairs, real friends
        public const byte NearDropout = 1 << 4;  // F144: almost walked away from school
        public const byte Graduated = 1 << 5;    // F144: walked the stage
        public const byte PrideWave = 1 << 6;    // F144: the dinner thrown in their honor
        public const byte FirstPay = 1 << 7;     // F145: the first real paycheck
    }

    public static class SoulRitualFlags2
    {
        public const byte Toast = 1 << 0;       // F145: raised a glass to the climb
        public const byte Retired = 1 << 1;     // F145: handed in the keys at last
        public const byte Plaque = 1 << 2;      // F145: thirty years, one plaque
        public const byte FirstDinner = 1 << 3; // F146: the first meal in the new place
        public const byte Renovating = 1 << 4;  // F146: mid-renovation, send help
        public const byte Renovated = 1 << 5;   // F146: survived it
        public const byte Bloomed = 1 << 6;     // F146: the garden's first bloom
        public const byte Resolution = 1 << 7;  // F147: one is in force right now
    }

    public static class SoulRitualFlags3
    {
        public const byte ResolutionKept = 1 << 0; // F147: actually kept one
        public const byte SentOff = 1 << 1;        // F149: threw the party at the station
        public const byte Letters = 1 << 2;        // F149: the letters that followed
        public const byte ReunionPlan = 1 << 3;    // F150: it is happening, they said
        public const byte ReunionNight = 1 << 4;   // F150: what-happened-to-everyone night
        public const byte Rekindled = 1 << 5;      // F150: an old spark, relit
        public const byte RiteDone = 1 << 6;       // F151: walked out of childhood
        public const byte RitePrep = 1 << 7;       // F151: rehearsals and fittings
    }

    public struct SoulRitual : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulRitualFlags
        public byte m_Flags2;         // SoulRitualFlags2
        public byte m_Flags3;         // SoulRitualFlags3
        public byte m_Variant;        // F151: which coming-of-age (1..3), 0 = not set
        public byte m_Streak;         // F147: resolutions kept in a row
        public uint m_CarDay;         // F142: lessons began; reset on a failed test
        public uint m_ApartmentDay;   // F143: the day the boxes came in
        public uint m_GradDay;        // F144: the day of the walk
        public uint m_JobDay;         // F145: the first paycheck landed
        public uint m_RenoDay;        // F146: the renovation began
        public uint m_ResolveDay;     // F147: the resolution was made
        public uint m_SentDay;        // F149: the send-off
        public uint m_RiteDay;        // F151: prep finished; the big day is set
        public uint m_ReunionDay;     // F150: the night they picked
        public Entity m_SendMate;     // F149: the one who left
        public Entity m_ReunionMate;  // F150: the one who made it back
        public uint m_LastDay;

        public static SoulRitual Fresh(uint day)
        {
            SoulRitual r = default;
            r.m_LastDay = day;
            return r;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_Flags3);
            writer.Write(m_Variant);
            writer.Write(m_Streak);
            writer.Write(m_CarDay);
            writer.Write(m_ApartmentDay);
            writer.Write(m_GradDay);
            writer.Write(m_JobDay);
            writer.Write(m_RenoDay);
            writer.Write(m_ResolveDay);
            writer.Write(m_SentDay);
            writer.Write(m_RiteDay);
            writer.Write(m_ReunionDay);
            writer.Write(m_SendMate);
            writer.Write(m_ReunionMate);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_Flags3);
            reader.Read(out m_Variant);
            reader.Read(out m_Streak);
            reader.Read(out m_CarDay);
            reader.Read(out m_ApartmentDay);
            reader.Read(out m_GradDay);
            reader.Read(out m_JobDay);
            reader.Read(out m_RenoDay);
            reader.Read(out m_ResolveDay);
            reader.Read(out m_SentDay);
            reader.Read(out m_RiteDay);
            reader.Read(out m_ReunionDay);
            reader.Read(out m_SendMate);
            reader.Read(out m_ReunionMate);
            reader.Read(out m_LastDay);
        }
    }
}
