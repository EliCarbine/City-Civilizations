// Citizen Soul -- SoulWorkComponents (M5b): work & economy state.
// Save-compat rule: brand-new components only, version header kept as byte.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    // Bit flags for SoulCareer.m_Flags.
    public static class SoulCareerFlags
    {
        public const byte Employed = 1 << 0;    // holding a job right now
        public const byte Fired = 1 << 1;       // has been let go at least once
        public const byte Promoted = 1 << 2;    // has earned at least one promotion
        public const byte Hustling = 1 << 3;    // running a side hustle
        public const byte ShopOwner = 1 << 4;   // hustle has grown into a shop
        public const byte BurntOut = 1 << 5;    // currently running on fumes
        public const byte DebtCrunch = 1 << 6;  // currently squeezed by money trouble
    }

    public struct SoulCareer : IComponentData, ISerializable
    {
        public byte m_Flags;
        public Entity m_Workplace;
        public uint m_SinceDay;
        public uint m_LastRaise;
        public byte m_Level;
        public byte m_Streak;
        public uint m_LastDay;

        public static SoulCareer Fresh(uint day)
        {
            SoulCareer c = default(SoulCareer);
            c.m_SinceDay = day;
            c.m_LastRaise = day;
            c.m_LastDay = day;
            return c;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_Flags);
            writer.Write(m_Workplace);
            writer.Write(m_SinceDay);
            writer.Write(m_LastRaise);
            writer.Write(m_Level);
            writer.Write(m_Streak);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("work.version." + version, "SoulWork: read version " + version + ".");
            }
            reader.Read(out m_Flags);
            reader.Read(out m_Workplace);
            reader.Read(out m_SinceDay);
            reader.Read(out m_LastRaise);
            reader.Read(out m_Level);
            reader.Read(out m_Streak);
            reader.Read(out m_LastDay);
        }
    }

    // City-level economy registry (singleton entity, like the dynasty registry).
    public struct SoulEconomy : IComponentData, ISerializable
    {
        public uint m_StartDay;     // first day work tracking ran (uint.MaxValue = unset)
        public uint m_Promotions;
        public uint m_Firings;
        public uint m_Shops;
        public uint m_Burnouts;
        public uint m_Giving;
        public uint m_LastDay;

        public static SoulEconomy Fresh()
        {
            SoulEconomy d = default(SoulEconomy);
            d.m_StartDay = uint.MaxValue;
            return d;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            byte version = SoulConsts.ComponentVersion;
            writer.Write(version);
            writer.Write(m_StartDay);
            writer.Write(m_Promotions);
            writer.Write(m_Firings);
            writer.Write(m_Shops);
            writer.Write(m_Burnouts);
            writer.Write(m_Giving);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out byte version);
            if (version != SoulConsts.ComponentVersion)
            {
                SoulLog.Once("economy.version." + version, "SoulWork: read version " + version + ".");
            }
            reader.Read(out m_StartDay);
            reader.Read(out m_Promotions);
            reader.Read(out m_Firings);
            reader.Read(out m_Shops);
            reader.Read(out m_Burnouts);
            reader.Read(out m_Giving);
            reader.Read(out m_LastDay);
        }
    }
}
