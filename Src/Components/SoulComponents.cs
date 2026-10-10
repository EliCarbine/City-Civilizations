using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    // =========================================================================
    // Soul core. All structs implement ISerializable following the documented
    // in-game pattern: writer.Write(field) / reader.Read(out field), reads
    // version-guarded via reader.context.version when evolving the layout.
    // Our own evolution guard: a leading byte SoulConst.Version in each
    // component that is expected to change shape across mod releases.
    // =========================================================================

    public static class SoulConst
    {
        public const byte Version = 1;        // mod serialization version
        public const int MaxEdges = 32;       // relationship buffer cap
        public const int MaxMemories = 64;    // ring log cap
        public const int UpdatesPerDay = 4;   // mirror vanilla slice count
    }

    /// Presence = soul initialized for this citizen.
    public struct SoulTag : IComponentData { }

    public struct Personality : IComponentData, IQueryTypeParameter, ISerializable
    {
        // Big-Five axes, 0..255, seeded from Citizen.m_PseudoRandom on init.
        public byte Openness;
        public byte Conscientiousness;
        public byte Extraversion;
        public byte Agreeableness;
        public byte Neuroticism;
        // Quirk bitfield: 0 early-bird, 1 night-owl, 2 fit, 3 bookworm, 4 hothead, 5 charmer, ...
        public uint Quirks;
        public ushort VoiceSeed;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConst.Version);
            writer.Write(Openness);
            writer.Write(Conscientiousness);
            writer.Write(Extraversion);
            writer.Write(Agreeableness);
            writer.Write(Neuroticism);
            writer.Write(Quirks);
            writer.Write(VoiceSeed);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version); // staged for future layout changes
            _ = version;
            reader.Read(out Openness);
            reader.Read(out Conscientiousness);
            reader.Read(out Extraversion);
            reader.Read(out Agreeableness);
            reader.Read(out Neuroticism);
            reader.Read(out Quirks);
            reader.Read(out VoiceSeed);
        }

        public readonly bool HasQuirk(int bit) => (Quirks & (1u << bit)) != 0u;
    }

    /// Six trainable skills. value = level*100 + xp, cap 1000.
    public struct Skills : IComponentData, ISerializable
    {
        public ushort Logic, Charisma, Fitness, Craft, Arts, Cooking;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Logic); writer.Write(Charisma); writer.Write(Fitness);
            writer.Write(Craft); writer.Write(Arts);    writer.Write(Cooking);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Logic); reader.Read(out Charisma); reader.Read(out Fitness);
            reader.Read(out Craft); reader.Read(out Arts);     reader.Read(out Cooking);
        }
    }

    public struct Mood : IComponentData, ISerializable
    {
        public sbyte Valence;   // -100..100, current feeling
        public byte Arousal;    // 0..255
        public byte Dominant;   // emotion id: 0 calm, 1 joy, 2 pride, 3 love, 4 anger, 5 fear, 6 sadness

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Valence); writer.Write(Arousal); writer.Write(Dominant);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Valence); reader.Read(out Arousal); reader.Read(out Dominant);
        }
    }

    /// Short-term emotional events; decay when ExpiresDay passes.
    [InternalBufferCapacity(8)]
    public struct SoulAffector : IBufferElementData
    {
        public byte Kind;
        public sbyte Intensity;
        public uint ExpiresDay;
    }

    /// One directed relationship edge. Mirror edge maintained on the other citizen.
    [InternalBufferCapacity(8)]
    public struct SoulEdge : IBufferElementData
    {
        public Entity Other;
        public sbyte Affinity;         // -100..100
        public byte Flags;             // 1 friend, 2 close, 4 lover, 8 rival, 16 family, 32 colleague
        public uint LastInteractionDay;
        public ushort SharedMoments;
    }

    /// Enableable marker with payload: this citizen is currently hurt.
    public struct Injured : IComponentData, IEnableableComponent, ISerializable
    {
        public byte Severity;   // 1 minor .. 3 severe
        public byte Cause;      // 0 accident, 1 fight, 2 illness
        public uint SinceDay;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Severity); writer.Write(Cause); writer.Write(SinceDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Severity); reader.Read(out Cause); reader.Read(out SinceDay);
        }
    }

    /// Enableable marker: live fight between two citizens (both get the component).
    public struct InFight : IComponentData, IEnableableComponent, ISerializable
    {
        public Entity Opponent;
        public uint StartedDay;
        public byte Round;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Opponent); writer.Write(StartedDay); writer.Write(Round);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Opponent); reader.Read(out StartedDay); reader.Read(out Round);
        }
    }

    /// Life-story ring buffer. Feeds the journal UI.
    [InternalBufferCapacity(16)]
    public struct SoulMemory : IBufferElementData
    {
        public uint Day;
        public ushort Kind;    // 0 met, 1 befriended, 2 fought, 3 healed, 4 graduated, 5 promoted, ...
        public Entity Subject;
        public short Value;
    }

    /// Mod singleton: tuning + bookkeeping.
    public struct SoulConfig : IComponentData
    {
        public byte EnableFights;
        public byte EnableLearning;
        public byte SliceCount;
    }

    // -------------------------------------------------------------------------
    // M0 SPIKE — persistence probe. Bump Counter daily; save; reload; if it
    // survived, mod components ride the save pipeline. Remove after verified.
    // -------------------------------------------------------------------------
    public struct SoulSpike : IComponentData, ISerializable
    {
        public uint Counter;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Counter);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Counter);
        }
    }
}
