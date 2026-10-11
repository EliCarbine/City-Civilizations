// Citizen Soul -- EP2 components B: the exchange (F167-F178).
// One little bourse the whole city watches: an index that breathes, shares
// that change hands, red days and golden ones, and the souls who ride it.
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace CitizenSoul
{
    /// <summary>Exchange-day bits carried in SoulExchange.m_Flags.</summary>
    public static class SoulExchangeFlags
    {
        public const byte CrashToday = 1 << 0; // the screens went red (F175)
        public const byte BoomToday = 1 << 1;  // the bull ran (F176)
    }

    /// <summary>
    /// F167-F176: the city bourse as one singleton ledger. The index breathes
    /// with the crowd's mood; crashes and booms are city events. Created and
    /// written only by ExchangeSystem (count==0 -> registry idiom).
    /// </summary>
    public struct SoulExchange : IComponentData, ISerializable
    {
        public int m_Index;       // market index; opens at 1000
        public int m_PrevClose;
        public sbyte m_Mood;      // -100 bear .. +100 bull
        public byte m_Listings;   // how many ventures trade
        public byte m_Flags;      // SoulExchangeFlags
        public uint m_CrashDay;
        public uint m_BoomDay;
        public uint m_Days;
        public uint m_LastDay;

        public static SoulExchange Fresh(uint day)
        {
            SoulExchange x = default(SoulExchange);
            x.m_Index = 1000;
            x.m_PrevClose = 1000;
            x.m_LastDay = day;
            return x;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Index);
            writer.Write(m_PrevClose);
            writer.Write(m_Mood);
            writer.Write(m_Listings);
            writer.Write(m_Flags);
            writer.Write(m_CrashDay);
            writer.Write(m_BoomDay);
            writer.Write(m_Days);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("exchange.version", "SoulExchange read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Index);
            reader.Read(out m_PrevClose);
            reader.Read(out m_Mood);
            reader.Read(out m_Listings);
            reader.Read(out m_Flags);
            reader.Read(out m_CrashDay);
            reader.Read(out m_BoomDay);
            reader.Read(out m_Days);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>
    /// F163-F174: one listed company. Lives as its own entity so the tape is
    /// cheap to walk; re-materialized from the owner's Listed flag after a
    /// load (self-healing), so the ledger on the soul is the source of truth.
    /// </summary>
    public struct SoulStock : IComponentData, ISerializable
    {
        public Entity m_Owner;    // the founder
        public byte m_Ticker;     // index into the ticker table
        public byte m_Vol;        // volatility 1..5 -- how nervous the price is
        public int m_Price;       // dollars
        public int m_Base;        // IPO price, for "since the bell" math
        public int m_High;
        public uint m_ListedDay;
        public uint m_LastDivDay;
        public uint m_LastDay;

        public static SoulStock Make(Entity owner, byte ticker, byte vol, int price, uint day)
        {
            SoulStock s = default(SoulStock);
            s.m_Owner = owner;
            s.m_Ticker = ticker;
            s.m_Vol = vol;
            s.m_Price = price;
            s.m_Base = price;
            s.m_High = price;
            s.m_ListedDay = day;
            s.m_LastDivDay = day;
            s.m_LastDay = day;
            return s;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Owner);
            writer.Write(m_Ticker);
            writer.Write(m_Vol);
            writer.Write(m_Price);
            writer.Write(m_Base);
            writer.Write(m_High);
            writer.Write(m_ListedDay);
            writer.Write(m_LastDivDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("stock.version", "SoulStock read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Owner);
            reader.Read(out m_Ticker);
            reader.Read(out m_Vol);
            reader.Read(out m_Price);
            reader.Read(out m_Base);
            reader.Read(out m_High);
            reader.Read(out m_ListedDay);
            reader.Read(out m_LastDivDay);
            reader.Read(out m_LastDay);
        }
    }

    /// <summary>Holding bits carried in SoulHolding.m_Flags.</summary>
    public static class SoulHoldingFlags
    {
        public const byte DayTrader = 1 << 0; // short holds, fast exits (F174)
    }

    /// <summary>
    /// F170-F174, F177-F178: one soul's single position -- they buy a piece
    /// of somebody's dream, hold it while it grows, or exit fast when it
    /// doesn't. The ticker/base are carried here so a position can outlive a
    /// missing tape row and rebuild it.
    /// </summary>
    public struct SoulHolding : IComponentData, ISerializable
    {
        public Entity m_Stock;
        public byte m_Ticker;
        public byte m_Flags;     // SoulHoldingFlags
        public ushort m_Shares;
        public int m_Cost;       // total paid, for win/loss math
        public uint m_BoughtDay;
        public uint m_LastDay;

        public static SoulHolding Fresh(uint day)
        {
            SoulHolding h = default(SoulHolding);
            h.m_LastDay = day;
            return h;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SoulConsts.ComponentVersion);
            writer.Write(m_Stock);
            writer.Write(m_Ticker);
            writer.Write(m_Flags);
            writer.Write(m_Shares);
            writer.Write(m_Cost);
            writer.Write(m_BoughtDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            byte version;
            reader.Read(out version);
            if (version > SoulConsts.ComponentVersion)
            {
                SoulLog.Once("holding.version", "SoulHolding read with version " + version + " > known " + SoulConsts.ComponentVersion);
            }
            reader.Read(out m_Stock);
            reader.Read(out m_Ticker);
            reader.Read(out m_Flags);
            reader.Read(out m_Shares);
            reader.Read(out m_Cost);
            reader.Read(out m_BoughtDay);
            reader.Read(out m_LastDay);
        }
    }
}
