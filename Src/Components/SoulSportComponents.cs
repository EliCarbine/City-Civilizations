// Citizen Soul -- sports & competition state (M5e): F72-F81.
// One soul, one sport at a time: a skill that grows, a stage that rises,
// a streak worth defending and a rival worth beating for decades.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Sport kinds a soul can make their own (F72-F81).</summary>
    public static class SoulSportKind
    {
        public const byte None = 0;
        public const byte Basketball = 1;
        public const byte Soccer = 2;
        public const byte Running = 3;
        public const byte MartialArts = 4;
        public const byte Esports = 5;
        public const byte Chess = 6;
        public const byte Count = 7;
    }

    /// <summary>Sport-life bits carried in SoulSport.m_Flags.</summary>
    public static class SoulSportFlags
    {
        public const byte Sidelined = 1 << 0;  // a sport knock, comeback pending (F81)
        public const byte Scouted = 1 << 1;    // the career seed already rolled (F78)
        public const byte Quit = 1 << 2;       // walked away young (F78)
        public const byte Veteran = 1 << 3;    // last season announced (F81)
        public const byte Champion = 1 << 4;   // a title to their name (F79)
        public const byte Club = 1 << 5;       // joined the run club (F74)
        public const byte Legend = 1 << 6;     // local-legend status crowned (F72)
        public const byte Crew = 1 << 7;       // the regulars made the court theirs (F72)
    }

    /// <summary>
    /// F72-F81: the sporting life -- pickup runs, leagues, clubs, dojos,
    /// ladders, park chess, school teams, titles, rivalries, the knocks and
    /// the comeback. Written only by SportsSystem.
    /// </summary>
    public struct SoulSport : IComponentData, ISerializable
    {
        public byte m_Sport;        // SoulSportKind.None until the city hooks them
        public byte m_Stage;        // 0 casual -> 4 champion
        public byte m_Flags;        // SoulSportFlags
        public uint m_Skill;        // 0..100 in the chosen sport
        public uint m_Conditioning; // 0..100 -- wind, legs, hands
        public uint m_Games;        // games played
        public uint m_Wins;
        public uint m_Streak;       // current win streak
        public uint m_Best;         // longest ever (F72 local-legend check)
        public Entity m_Nemesis;    // the rival across seasons and decades (F77/F80)
        public uint m_LastDay;      // last day this soul played

        public static SoulSport Fresh(uint day)
        {
            SoulSport s = default(SoulSport);
            s.m_LastDay = day;
            return s;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Sport);
            writer.Write(m_Stage);
            writer.Write(m_Flags);
            writer.Write(m_Skill);
            writer.Write(m_Conditioning);
            writer.Write(m_Games);
            writer.Write(m_Wins);
            writer.Write(m_Streak);
            writer.Write(m_Best);
            writer.Write(m_Nemesis);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("sport.version", "SoulSport read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Sport);
            reader.Read(out m_Stage);
            reader.Read(out m_Flags);
            reader.Read(out m_Skill);
            reader.Read(out m_Conditioning);
            reader.Read(out m_Games);
            reader.Read(out m_Wins);
            reader.Read(out m_Streak);
            reader.Read(out m_Best);
            reader.Read(out m_Nemesis);
            reader.Read(out m_LastDay);
        }
    }
}
