// Citizen Soul -- EP2 components C: watchers and life paths (F179-F184).
// What the city watches together -- one favorite show, cartoons on slow
// mornings, the news that lands -- and the long road of becoming.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Viewer bits carried in SoulViewer.m_Flags.</summary>
    public static class SoulViewerFlags
    {
        public const byte Watcher = 1 << 0;    // watches the lineup at all (F179)
        public const byte Devoted = 1 << 1;    // found THE show and stayed (F181)
        public const byte CartoonKid = 1 << 2; // grew up on Saturday cartoons (F182)
        public const byte NewsHound = 1 << 3;  // the evening news is a ritual (F183)
    }

    /// <summary>
    /// F179-F183: one soul's relationship with the screen -- the show they
    /// found, the cartoons they grew up on, the news they don't admit to
    /// watching. Written only by BroadcastSystem.
    /// </summary>
    public struct SoulViewer : IComponentData, ISerializable
    {
        public byte m_Favorite;       // show id 0..23; 255 = none yet
        public byte m_Flags;          // SoulViewerFlags
        public ushort m_Watched;      // episodes seen, ever
        public uint m_LastWatchDay;
        public uint m_LastDay;

        public static SoulViewer Fresh(uint day)
        {
            SoulViewer v = default(SoulViewer);
            v.m_Favorite = 255;
            v.m_LastDay = day;
            return v;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Favorite);
            writer.Write(m_Flags);
            writer.Write(m_Watched);
            writer.Write(m_LastWatchDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("viewer.version", "SoulViewer read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Favorite);
            reader.Read(out m_Flags);
            reader.Read(out m_Watched);
            reader.Read(out m_LastWatchDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>Fields a soul can dream of making their life in (F184).</summary>
    public static class SoulDreamField
    {
        public const byte None = 0;
        public const byte Arts = 1;
        public const byte Craft = 2;
        public const byte Trade = 3;
        public const byte Business = 4;
        public const byte Sport = 5;
        public const byte Knowledge = 6;
        public const byte Count = 7;
    }

    /// <summary>Path bits carried in SoulPath.m_Flags.</summary>
    public static class SoulPathFlags
    {
        public const byte Crossroads = 1 << 0;  // stood at the fork (F184)
        public const byte Chosen = 1 << 1;      // picked a direction
        public const byte Rerouted = 1 << 2;    // the road bent under them
        public const byte Deferred = 1 << 3;    // the dream on the shelf
        public const byte Rising = 1 << 4;      // starting to be known for it
        public const byte Topped = 1 << 5;      // made it to the top of the field
        public const byte LateBloom = 1 << 6;   // rose late and rose anyway
        public const byte SelfMade = 1 << 7;    // pulled up by their own straps (F193)
    }

    /// <summary>
    /// F184: the road of becoming -- school, the fork, the choice, the grind,
    /// the reroute, and the top of the field for those whose talent, work
    /// and luck line up. Written only by PathSystem.
    /// </summary>
    public struct SoulPath : IComponentData, ISerializable
    {
        public byte m_Dream;      // SoulDreamField
        public byte m_Flags;      // SoulPathFlags
        public ushort m_Progress; // 0..600 -- career of the self
        public uint m_ChosenDay;
        public uint m_RiseDay;
        public uint m_LastDay;

        public static SoulPath Fresh(uint day)
        {
            SoulPath p = default(SoulPath);
            p.m_LastDay = day;
            return p;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Dream);
            writer.Write(m_Flags);
            writer.Write(m_Progress);
            writer.Write(m_ChosenDay);
            writer.Write(m_RiseDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("path.version", "SoulPath read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Dream);
            reader.Read(out m_Flags);
            reader.Read(out m_Progress);
            reader.Read(out m_ChosenDay);
            reader.Read(out m_RiseDay);
            reader.Read(out m_LastDay);
        }
    }
}
