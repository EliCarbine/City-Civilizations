// Citizen Soul -- SoulFamilyComponents (M5a): family & dynasty state.
// Save-compat rule: brand-new components only, version header kept as byte.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    // Bit flags for SoulFamily.m_Flags.
    public static class SoulFamilyFlags
    {
        public const byte Resolved = 1 << 0;    // family record settled (parents linked, or founder)
        public const byte Widowed = 1 << 1;     // lost a spouse to death
        public const byte Grandparent = 1 << 2; // has grandchildren in the home
        public const byte Affair = 1 << 3;      // carrying on an affair
        public const byte AffairFound = 1 << 4; // the affair was discovered
        public const byte Divorced = 1 << 5;    // ended a marriage
    }

    public struct SoulFamily : IComponentData, ISerializable
    {
        public byte m_Flags;
        public Entity m_ParentA;
        public Entity m_ParentB;
        public Entity m_Affair;
        public uint m_Lineage;
        public byte m_Generation;
        public uint m_LastDay;

        public static SoulFamily Fresh(uint day)
        {
            SoulFamily f = default(SoulFamily);
            f.m_LastDay = day;
            return f;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_Flags);
            writer.Write(m_ParentA);
            writer.Write(m_ParentB);
            writer.Write(m_Affair);
            writer.Write(m_Lineage);
            writer.Write(m_Generation);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("family.version." + version, "SoulFamily: read version " + version + ".");
            }
            reader.Read(out m_Flags);
            reader.Read(out m_ParentA);
            reader.Read(out m_ParentB);
            reader.Read(out m_Affair);
            reader.Read(out m_Lineage);
            reader.Read(out m_Generation);
            reader.Read(out m_LastDay);
        }
    }

    // City-level dynasty registry (singleton entity, like the passing registry).
    public struct SoulDynasty : IComponentData, ISerializable
    {
        public uint m_NextLineage; // next lineage id to hand out
        public uint m_Lineages;    // highest lineage id handed out
        public uint m_StartDay;    // first day family tracking ran (uint.MaxValue = unset)
        public uint m_LastDay;

        public static SoulDynasty Fresh()
        {
            SoulDynasty d = default(SoulDynasty);
            d.m_StartDay = uint.MaxValue;
            return d;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_NextLineage);
            writer.Write(m_Lineages);
            writer.Write(m_StartDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("dynasty.version." + version, "SoulDynasty: read version " + version + ".");
            }
            reader.Read(out m_NextLineage);
            reader.Read(out m_Lineages);
            reader.Read(out m_StartDay);
            reader.Read(out m_LastDay);
        }
    }
}
