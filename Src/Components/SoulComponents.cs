using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>Kinds of life events recorded in soul memory + the journal ring.</summary>
    public enum SoulEventKind : byte
    {
        None = 0,
        Born = 1,        // soul initialized
        Met = 2,         // first contact formed a tie
        Befriended = 3,  // affinity crossed the friend threshold
        FellOut = 4,     // affinity collapsed under acquaintance level
        Fought = 5,      // physical fight resolved
        RivalMade = 6,   // rivalry flag set
        Injured = 7,     // injury onset
        Healed = 8,      // injury recovered
        LevelUp = 9,     // skill gained a level
        GoodDay = 10,    // mood stayed high at day's end
        HardDay = 11     // mood stayed low at day's end
    }

    /// <summary>Quirk bits packed into SoulPersonality.m_Quirk.</summary>
    public static class SoulQuirks
    {
        public const int Hothead = 0;
        public const int Charming = 1;
        public const int Loner = 2;
        public const int Workaholic = 3;
        public const int NightOwl = 4;
        public const int Fitness = 5;
        public const int Artistic = 6;
        public const int Tidy = 7;
        public const int Bookworm = 8;
        public const int Jokester = 9;
        public const int Count = 10;

        public static bool Has(uint quirk, int bit) { return (quirk & (1u << bit)) != 0u; }

        /// <summary>Deterministic starting quirks from the soul's seed (1-2 quirks, not Loner+Charming).</summary>
        public static uint Roll(ushort soulSeed)
        {
            uint r = (uint)soulSeed * 1103515245u + 12345u;
            uint q = 0u;
            int first = (int)(r >> 16) % Count;
            q |= 1u << first;
            r = r * 1103515245u + 12345u;
            if (((r >> 20) & 3u) != 0u) // 75% of souls carry a second quirk
            {
                int second = (int)((r >> 8) % (uint)Count);
                if (second != first) q |= 1u << second;
            }
            return q;
        }
    }

    /// <summary>Marker: this citizen has a soul (all components below hang off it).</summary>
    public struct SoulTag : IComponentData { }

    /// <summary>Five OCEAN-ish axes (0-255) + packed quirks + stable soul seed.</summary>
    public struct SoulPersonality : IComponentData, ISerializable
    {
        public byte m_Openness;
        public byte m_Conscientiousness;
        public byte m_Extraversion;
        public byte m_Agreeableness;
        public byte m_Neuroticism;
        public uint m_Quirk;
        public ushort m_SoulSeed;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Openness);
            writer.Write(m_Conscientiousness);
            writer.Write(m_Extraversion);
            writer.Write(m_Agreeableness);
            writer.Write(m_Neuroticism);
            writer.Write(m_Quirk);
            writer.Write(m_SoulSeed);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("personality.version", "SoulPersonality read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Openness);
            reader.Read(out m_Conscientiousness);
            reader.Read(out m_Extraversion);
            reader.Read(out m_Agreeableness);
            reader.Read(out m_Neuroticism);
            reader.Read(out m_Quirk);
            reader.Read(out m_SoulSeed);
        }
    }

    /// <summary>
    /// Current mood. Valence is the smoothed emotional state; impulse is a pending
    /// one-shot shock applied and halved by EmotionSystem (bounded, allocation-free).
    /// </summary>
    public struct Mood : IComponentData, ISerializable
    {
        public sbyte m_Valence;    // -100..100
        public sbyte m_Impulse;    // pending shock, decays to 0
        public byte m_Arousal;     // 0-255, calm = 128
        public uint m_SinceDay;    // last day EmotionSystem ran for this citizen

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Valence);
            writer.Write(m_Impulse);
            writer.Write(m_Arousal);
            writer.Write(m_SinceDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("mood.version", "Mood read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Valence);
            reader.Read(out m_Impulse);
            reader.Read(out m_Arousal);
            reader.Read(out m_SinceDay);
        }
    }

    /// <summary>Skills stored as level*100 + xp. Level cap 99 by construction.</summary>
    public struct Skills : IComponentData, ISerializable
    {
        public ushort m_Logic;
        public ushort m_Craft;
        public ushort m_Social;
        public ushort m_Fitness;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Logic);
            writer.Write(m_Craft);
            writer.Write(m_Social);
            writer.Write(m_Fitness);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("skills.version", "Skills read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Logic);
            reader.Read(out m_Craft);
            reader.Read(out m_Social);
            reader.Read(out m_Fitness);
        }
    }

    /// <summary>Per-soul bookkeeping: birth day + per-system day cursors.</summary>
    public struct SoulMeta : IComponentData, ISerializable
    {
        public uint m_BirthDay;
        public uint m_LastLearnDay;
        public uint m_LastSocialDay;
        public ushort m_MirroredMemories; // journal cursor: memories already copied to journal

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_BirthDay);
            writer.Write(m_LastLearnDay);
            writer.Write(m_LastSocialDay);
            writer.Write(m_MirroredMemories);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("meta.version", "SoulMeta read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_BirthDay);
            reader.Read(out m_LastLearnDay);
            reader.Read(out m_LastSocialDay);
            reader.Read(out m_MirroredMemories);
        }
    }

    /// <summary>Enableable: citizen is mid-fight. Round counts up to SoulConsts.FightRounds.</summary>
    public struct InFight : IComponentData, IEnableableComponent, ISerializable
    {
        public Entity m_Opponent;
        public byte m_Round;
        public uint m_StartDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Opponent);
            writer.Write(m_Round);
            writer.Write(m_StartDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("infight.version", "InFight read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Opponent);
            reader.Read(out m_Round);
            reader.Read(out m_StartDay);
        }
    }

    /// <summary>Enableable: injured, recovering toward m_RecoverDay.</summary>
    public struct Injured : IComponentData, IEnableableComponent, ISerializable
    {
        public byte m_Severity;   // 1-2: sprain vs fracture
        public uint m_RecoverDay; // day the injury clears
        public Entity m_Source;   // who/what did it (Null for accidents)

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Severity);
            writer.Write(m_RecoverDay);
            writer.Write(m_Source);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("injured.version", "Injured read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Severity);
            reader.Read(out m_RecoverDay);
            reader.Read(out m_Source);
        }
    }

    /// <summary>City-wide soul counters, singleton on the config entity. O(1) per event.</summary>
    public struct SoulCityStats : IComponentData, ISerializable
    {
        public uint m_SoulsCreated;
        public uint m_FightsResolved;
        public uint m_InjuriesTotal;
        public uint m_HealsTotal;
        public uint m_FriendshipsFormed;
        public uint m_LastStatsDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_SoulsCreated);
            writer.Write(m_FightsResolved);
            writer.Write(m_InjuriesTotal);
            writer.Write(m_HealsTotal);
            writer.Write(m_FriendshipsFormed);
            writer.Write(m_LastStatsDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("stats.version", "SoulCityStats read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_SoulsCreated);
            reader.Read(out m_FightsResolved);
            reader.Read(out m_InjuriesTotal);
            reader.Read(out m_HealsTotal);
            reader.Read(out m_FriendshipsFormed);
            reader.Read(out m_LastStatsDay);
        }
    }

    /// <summary>
    /// M0 persistence spike: a monotonic counter + rolling hash living on the config
    /// entity. If m_Counter keeps climbing across a save/load AND m_RollHash matches
    /// the value expected from the same number of updates, per-citizen persistence is
    /// proven end-to-end (see SoulSpikeSystem, 50-day checkpoint logging).
    /// </summary>
    public struct SoulSpike : IComponentData, ISerializable
    {
        public uint m_Counter;
        public uint m_RollHash;
        public uint m_LastDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Counter);
            writer.Write(m_RollHash);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("spike.version", "SoulSpike read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Counter);
            reader.Read(out m_RollHash);
            reader.Read(out m_LastDay);
        }
    }
}
