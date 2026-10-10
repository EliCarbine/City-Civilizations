// Citizen Soul -- travel components (M5k): F127-F134.
// One travel lane per soul: vacations counted down then packed for,
// day trips and school buses, first keys and Sunday rituals, waves of
// homesickness and the calls that ride them out, arrivals welcomed or
// not, guests from far away and the mail that follows, the long trip
// back, and the remembering that only lifers have.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>Bitfield: what a soul's travel life has become.</summary>
    public static class SoulTravelFlags
    {
        public const byte Planner = 1;     // F127: the calendar has a trip on it
        public const byte Traveler = 2;    // F127: has been away long enough to count
        public const byte DayTripper = 4;  // F128: weekends belong to somewhere close
        public const byte FirstCar = 8;    // F129: keys of their own
        public const byte CarPerson = 16;  // F129: bucket, driveway, Sunday
        public const byte Homesick = 32;   // F130: mid-wave, missing somewhere
        public const byte LetterHome = 64; // F130: the letters still arrive
        public const byte Pilgrim = 128;   // F133: made the long trip back
    }

    /// <summary>Second bitfield: houses overflow at eight bits.</summary>
    public static class SoulTravelFlags2
    {
        public const byte Host = 1;          // F132: put a student up for a year
        public const byte Welcomed = 2;      // F131: someone showed them around
        public const byte ColdShoulder = 4;  // F131: nobody said welcome
        public const byte Lifer = 8;         // F134: never lived anywhere else
        public const byte Nostalgic = 16;    // F134: unlocked the remembering
        public const byte Abroad = 32;       // F127: the far tier, at least once
    }

    /// <summary>
    /// M5k travel lane (F127-F134). One pass per citizen per day, staggered
    /// by lane. m_TripStage drives the vacation cycle: 0 home, 1 counting
    /// down, 2 away; m_TripDay is the day the current stage flips. The rest
    /// is gated by period + per-mille rolls exactly like the tempo lane.
    /// </summary>
    public struct SoulTravel : IComponentData, ISerializable
    {
        public byte m_Flags;
        public byte m_Flags2;
        public byte m_TripTier;     // 0 camping .. 2 abroad (career level picks)
        public byte m_TripStage;    // 0 home, 1 counting down, 2 away
        public uint m_TripDay;      // day the current stage advances
        public uint m_LastTripDay;  // day the last trip ended (annual rhythm)
        public uint m_LastDay;

        public static SoulTravel Fresh(uint day)
        {
            SoulTravel t = default(SoulTravel);
            t.m_LastTripDay = day;
            t.m_TripDay = day;
            t.m_LastDay = day;
            return t;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_TripTier);
            writer.Write(m_TripStage);
            writer.Write(m_TripDay);
            writer.Write(m_LastTripDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("travel.version", "SoulTravel read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_TripTier);
            reader.Read(out m_TripStage);
            reader.Read(out m_TripDay);
            reader.Read(out m_LastTripDay);
            reader.Read(out m_LastDay);
        }
    }
}
