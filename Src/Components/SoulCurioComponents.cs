// Citizen Soul -- learning & curiosity state (M5f): F82-F90.
// One soul, one hunger: books, night classes, languages, museums, trivia,
// the stars, late-night philosophy, a craft mastered by apprenticeship --
// and the small wonder of noticing the world.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Curiosity tracks a soul can follow (F82-F90).</summary>
    public static class SoulCurioTrack
    {
        public const byte None = 0;
        public const byte Library = 1;     // book clubs & libraries (F82)
        public const byte NightClass = 2;  // evening courses & retraining (F83)
        public const byte Language = 3;    // a second tongue (F84)
        public const byte Museum = 4;      // museums & galleries (F85)
        public const byte Trivia = 5;      // trivia & game nights (F86)
        public const byte Stargazer = 6;   // stars, weather, radio (F87)
        public const byte Philosophy = 7;  // late-night talks (F88)
        public const byte Apprentice = 8;  // master-student tracks (F89)
        public const byte Count = 9;
    }

    /// <summary>Curiosity bits carried in SoulCurio.m_Flags.</summary>
    public static class SoulCurioFlags
    {
        public const byte Club = 1 << 0;      // in a book club (F82)
        public const byte Schism = 1 << 1;    // the club split over an ending (F82)
        public const byte Cohort = 1 << 2;    // the night-class cohort bonded (F83)
        public const byte Bilingual = 1 << 3; // a second tongue, fluent (F84)
        public const byte Team = 1 << 4;      // trivia team regular (F86)
        public const byte Feuded = 1 << 5;    // a board game ended a friendship (F86)
        public const byte Argued = 1 << 6;    // the great argument already struck (F88)
        public const byte Mastered = 1 << 7;  // mastery day passed (F89)
    }

    /// <summary>
    /// F82-F90: the learning life -- clubs, classes, languages, museums,
    /// trivia, stargazing, philosophy, apprenticeship, and plain noticing.
    /// Written only by CuriositySystem.
    /// </summary>
    public struct SoulCurio : IComponentData, ISerializable
    {
        public byte m_Track;      // SoulCurioTrack.None until something catches them
        public byte m_Flags;      // SoulCurioFlags
        public uint m_Depth;      // 0..100 in the chosen pursuit
        public uint m_Sessions;   // evenings given to it
        public uint m_Books;      // books read, exhibits seen, nights watched
        public Entity m_Partner;  // club friend, cohort mate, study partner, master
        public uint m_LastDay;

        public static SoulCurio Fresh(uint day)
        {
            SoulCurio c = default(SoulCurio);
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Track);
            writer.Write(m_Flags);
            writer.Write(m_Depth);
            writer.Write(m_Sessions);
            writer.Write(m_Books);
            writer.Write(m_Partner);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("curio.version", "SoulCurio read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Track);
            reader.Read(out m_Flags);
            reader.Read(out m_Depth);
            reader.Read(out m_Sessions);
            reader.Read(out m_Books);
            reader.Read(out m_Partner);
            reader.Read(out m_LastDay);
        }
    }
}
