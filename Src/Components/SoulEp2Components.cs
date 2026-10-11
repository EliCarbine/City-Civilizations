// Citizen Soul -- EP2 components A: the money & enterprise layer (F152-F184).
// Every soul keeps a wallet that can grow, strain and lean on luck; the
// enterprising hang a sign, hire help, and learn whether the city buys it.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Money-life bits carried in SoulWallet.m_Flags.</summary>
    public static class SoulWalletFlags
    {
        public const byte Leaning = 1 << 0;  // below zero; the lean stretch (F153)
        public const byte Recovered = 1 << 1; // climbed back out of the red once (F153)
    }

    /// <summary>
    /// F152-F156: the wallet -- paydays, upkeep, windfalls, mishaps, the
    /// lean stretches and the milestones nobody hears about. Luck is a slow
    /// tide here, not a slot machine. Written only by FortuneSystem.
    /// </summary>
    public struct SoulWallet : IComponentData, ISerializable
    {
        public int m_Balance;       // dollars; may dip below zero to the debt floor
        public int m_Lifetime;      // lifetime earned, for the story
        public sbyte m_Luck;        // -20..+20, drifts back toward zero over years
        public byte m_Milestone;    // 0 none, 1 = 1k, 2 = 10k, 3 = 100k
        public byte m_Flags;        // SoulWalletFlags
        public ushort m_LeanDays;   // consecutive red days
        public uint m_LastPayDay;
        public uint m_LastNoteDay;  // rate limit for routine beats
        public uint m_LastDay;

        public static SoulWallet Fresh(uint day)
        {
            SoulWallet w = default(SoulWallet);
            w.m_LastDay = day;
            w.m_LastPayDay = day;
            w.m_LastNoteDay = day;
            return w;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Balance);
            writer.Write(m_Lifetime);
            writer.Write(m_Luck);
            writer.Write(m_Milestone);
            writer.Write(m_Flags);
            writer.Write(m_LeanDays);
            writer.Write(m_LastPayDay);
            writer.Write(m_LastNoteDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("wallet.version", "SoulWallet read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Balance);
            reader.Read(out m_Lifetime);
            reader.Read(out m_Luck);
            reader.Read(out m_Milestone);
            reader.Read(out m_Flags);
            reader.Read(out m_LeanDays);
            reader.Read(out m_LastPayDay);
            reader.Read(out m_LastNoteDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>Venture kinds a soul can hang a sign on (F157-F166).</summary>
    public static class SoulVentureKind
    {
        public const byte None = 0;
        public const byte Bakery = 1;
        public const byte Cafe = 2;
        public const byte Barbershop = 3;
        public const byte PrintShop = 4;
        public const byte RecordShop = 5;
        public const byte Garage = 6;
        public const byte FoodTruck = 7;
        public const byte FlowerStand = 8;
        public const byte Count = 9;
    }

    /// <summary>Venture-life bits carried in SoulVenture.m_Flags.</summary>
    public static class SoulVentureFlags
    {
        public const byte Open = 1 << 0;      // the sign is up
        public const byte Thriving = 1 << 1;  // currently on a good stretch (F160)
        public const byte Strained = 1 << 2;  // currently grinding through a bad one (F161)
        public const byte IpoReady = 1 << 3;  // good enough long enough; waiting on the exchange (F163)
        public const byte Listed = 1 << 4;    // rang the bell; shares trade (F163)
        public const byte Busted = 1 << 5;    // pulled the gate down (F162)
    }

    /// <summary>
    /// F157-F166: one soul's enterprise -- the counter, the ledger, the
    /// staff, the stretches, the closing bell or the bell that rings on the
    /// exchange floor. Written only by VentureSystem (Listing edits go
    /// through the exchange system's stamp).
    /// </summary>
    public struct SoulVenture : IComponentData, ISerializable
    {
        public byte m_Kind;       // SoulVentureKind
        public byte m_Flags;      // SoulVentureFlags
        public sbyte m_Health;    // -100..+100 -- is the city buying it
        public byte m_Staff;      // people on the payroll
        public byte m_GoodDays;   // current good stretch (toward IPO)
        public byte m_BadDays;    // current bad stretch (toward closing)
        public ushort m_DaysOpen; // lifetime trading days
        public int m_Cash;        // the till
        public uint m_OpenDay;
        public uint m_BustDay;
        public uint m_ListedDay;
        public uint m_LastDay;

        public static SoulVenture Fresh(uint day)
        {
            SoulVenture v = default(SoulVenture);
            v.m_LastDay = day;
            return v;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Kind);
            writer.Write(m_Flags);
            writer.Write(m_Health);
            writer.Write(m_Staff);
            writer.Write(m_GoodDays);
            writer.Write(m_BadDays);
            writer.Write(m_DaysOpen);
            writer.Write(m_Cash);
            writer.Write(m_OpenDay);
            writer.Write(m_BustDay);
            writer.Write(m_ListedDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("venture.version", "SoulVenture read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Kind);
            reader.Read(out m_Flags);
            reader.Read(out m_Health);
            reader.Read(out m_Staff);
            reader.Read(out m_GoodDays);
            reader.Read(out m_BadDays);
            reader.Read(out m_DaysOpen);
            reader.Read(out m_Cash);
            reader.Read(out m_OpenDay);
            reader.Read(out m_BustDay);
            reader.Read(out m_ListedDay);
            reader.Read(out m_LastDay);
        }
    }
}
