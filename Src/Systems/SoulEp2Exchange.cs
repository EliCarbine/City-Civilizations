// Citizen Soul -- EP2 Exchange (F166-F172). The city exchange: an index
// that breathes with the days, red days and golden days, listed shares
// walking their own tape, and listings that arrive when a venture rings the
// bell. The index is citywide weather; citizens feel it through the news.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class ExchangeSystem : SoulSystemBase
    {
        private const int TapeBudget = 96;       // stocks walked per cadence
        private const int MaxCatchUpDays = 45;   // never replay more than this

        private EntityQuery m_Exchange;
        private EntityQuery m_Stocks;
        private EntityQuery m_Desk;
        private EntityQuery m_Souls;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Exchange = GetEntityQuery(ComponentType.ReadOnly<SoulExchange>());
            m_Stocks = GetEntityQuery(
                ComponentType.ReadWrite<SoulStock>());
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
            m_Souls = GetEntityQuery(
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulWallet>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            if (lane != 0) return; // one desk, one tape: a single lane walks the exchange

            EntityManager em = EntityManager;

            try { ExchangeStep(em, day); }
            catch (Exception ex) { DisableSelf("exchange: " + ex.Message); return; }

            try { TapeStep(em, day, lane); }
            catch (Exception ex2) { DisableSelf("tape: " + ex2.Message); }

            try
            {
                NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
                try { TradeStep(em, day, souls); }
                finally { souls.Dispose(); }
            }
            catch (Exception ex3) { DisableSelf("trade: " + ex3.Message); }

            try { DividendStep(em, day); }
            catch (Exception ex4) { DisableSelf("dividends: " + ex4.Message); }
        }

        // F166-F167 -- the index breathes; crashes and rallies are weather.
        private void ExchangeStep(EntityManager em, uint day)
        {
            if (m_Exchange.CalculateEntityCount() == 0)
            {
                Entity reg = em.CreateEntity();
                em.AddComponentData(reg, SoulExchange.Fresh(day));
                SoulLog.Once("exchange.registry", "ExchangeSystem: exchange registry created.");
                return;
            }
            if (m_Exchange.CalculateEntityCount() != 1) return;
            Entity x = m_Exchange.GetSingletonEntity();
            SoulExchange c = em.GetComponentData<SoulExchange>(x);
            if (c.m_LastDay == day) return;

            c.m_PrevClose = c.m_Index; // last night's close, for the morning read
            c.m_Days++;
            c.m_Flags = 0; // today's weather is fresh each pass

            int drift = (int)SoulEp2Util.Range(SoulEp2Util.Salt(x), day, SoulEp2Util.IndexDrift * 2 + 1, 61u) - SoulEp2Util.IndexDrift;
            int idx = SoulEp2Util.Clamp(c.m_Index + drift, 200, 4000);

            if (SoulEp2Util.Roll(SoulEp2Util.Salt(x), day + 1u, SoulEp2Util.CrashPerMille))
            {
                idx -= SoulEp2Util.CrashDepth;
                c.m_Flags |= SoulExchangeFlags.CrashToday;
                c.m_CrashDay = day;
                Scoop(em, SoulWire.Market, 6, -SoulEp2Util.CrashDepth, x, day);
                Broadcast(em, day, SoulEventKind.MarketCrashOn, (short)(-SoulEp2Util.CrashDepth), x);
            }
            else if (SoulEp2Util.Roll(SoulEp2Util.Salt(x), day + 2u, SoulEp2Util.BoomPerMille))
            {
                idx += SoulEp2Util.BoomHeight;
                c.m_Flags |= SoulExchangeFlags.BoomToday;
                c.m_BoomDay = day;
                Scoop(em, SoulWire.Market, 5, SoulEp2Util.BoomHeight, x, day);
                Broadcast(em, day, SoulEventKind.MarketRallyOn, (short)SoulEp2Util.BoomHeight, x);
            }
            c.m_Index = SoulEp2Util.Clamp(idx, 200, 4000);

            c.m_Mood = (sbyte)SoulEp2Util.Clamp((c.m_Index - 1000) / 50 + (c.m_Index - c.m_PrevClose) / 100, -100, 100);

            byte listings = (byte)Math.Min(255, m_Stocks.CalculateEntityCount());
            if (listings > c.m_Listings) Scoop(em, SoulWire.Market, 3, listings, x, day);
            c.m_Listings = listings;

            c.m_LastDay = day;
            em.SetComponentData(x, c);
        }

        private const int TradeBudget = 128;       // citizens trading per day
        private const int TradeCheckPerMille = 30; // how many even look at the tape
        private const int TradeMinCash = 350;      // need this much to buy in
        private const int TradeCushion = 120;      // never spend the last dollar

        private void TapeStep(EntityManager em, uint day, int lane)
        {
            int budget = TapeBudget;
            NativeArray<Entity> stocks = m_Stocks.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < stocks.Length && budget > 0; i++, budget--)
                {
                    Entity s = stocks[i];
                    SoulStock st = em.GetComponentData<SoulStock>(s);
                    if (st.m_LastDay == day) continue;

                    uint from = st.m_LastDay == 0u ? day : st.m_LastDay + 1u;
                    if (day - from > (uint)MaxCatchUpDays) from = day - (uint)MaxCatchUpDays;

                    int price = st.m_Price > 0 ? st.m_Price : st.m_Base;
                    int vol = st.m_Vol < 1 ? 1 : (st.m_Vol > 5 ? 5 : st.m_Vol);

                    for (uint d = from; d <= day; d++)
                    {
                        int before = price;
                        int span = vol * SoulEp2Util.StockCatchUp + 6;
                        int step = (int)SoulEp2Util.Range(SoulEp2Util.Salt(s), d ^ 11u, span, 41u) - (vol * 2 + 2);
                        price = SoulEp2Util.Clamp(price + step, 5, 5000);

                        if (price * 4 <= before * 3)
                            Scoop(em, SoulWire.Market, 3, price, s, day);
                        else if (before * 4 <= price * 3)
                            Scoop(em, SoulWire.Market, 2, price, s, day);

                        if (SoulEp2Util.Range(SoulEp2Util.Salt(s), d ^ 7u, 3, 42u) == 0u)
                            vol = SoulEp2Util.Clamp(vol + (SoulEp2Util.Range(SoulEp2Util.Salt(s), d ^ 5u, 2, 43u) == 0u ? -1 : 1), 1, 5);
                    }

                    if (price > st.m_High) st.m_High = price;
                    st.m_Price = price;
                    st.m_Vol = (byte)vol;
                    st.m_LastDay = day;
                    em.SetComponentData(s, st);
                }
            }
            finally { stocks.Dispose(); }
        }

        private void TradeStep(EntityManager em, uint day, NativeArray<Entity> souls)
        {
            if (m_Stocks.CalculateEntityCount() == 0) return;
            int moves = 0;
            for (int i = 0; i < souls.Length && moves < TradeBudget; i++)
            {
                Entity e = souls[i];
                if (!SoulEp2Util.Roll(SoulEp2Util.Salt(e), day + 7u, TradeCheckPerMille)) continue;
                SoulWallet w = em.GetComponentData<SoulWallet>(e);
                moves++;

                if (em.HasComponent<SoulHolding>(e))
                {
                    SoulHolding h = em.GetComponentData<SoulHolding>(e);
                    if (!em.HasComponent<SoulStock>(h.m_Stock)) { em.RemoveComponent<SoulHolding>(e); continue; }
                    SoulStock st = em.GetComponentData<SoulStock>(h.m_Stock);
                    int value = h.m_Shares * st.m_Price;
                    int profit = value - h.m_Cost;
                    if (st.m_Price * 2 <= st.m_Base)
                        SoulFeel.Note(em, e, day, SoulEventKind.SharesSlidOn, (short)SoulEp2Util.Clamp((st.m_Price - st.m_Base) / 10, -3000, 0), h.m_Stock);
                    if (profit * 4 >= h.m_Cost || profit * 3 <= -h.m_Cost ||
                        ((h.m_Flags & SoulHoldingFlags.DayTrader) != 0 && profit * 1000 <= -h.m_Cost * SoulEp2Util.ExitLossPerMille))
                    {
                        w.m_Balance += value;
                        em.RemoveComponent<SoulHolding>(e);
                        em.SetComponentData(e, w);
                        SoulEventKind one = value >= h.m_Cost * 2 ? SoulEventKind.DoubledOn :
                            profit >= 0 ? SoulEventKind.CashedOutOn : SoulEventKind.RodeItDownOn;
                        SoulFeel.Note(em, e, day, one, (short)SoulEp2Util.Clamp(profit / 10, -3000, 3000), h.m_Stock);
                    }
                }
                else if (w.m_Balance >= TradeMinCash + TradeCushion)
                {
                    NativeArray<Entity> stocks = m_Stocks.ToEntityArray(Allocator.Temp);
                    Entity st = stocks[(int)SoulEp2Util.Range(SoulEp2Util.Salt(e), day + 8u, stocks.Length, 45u)];
                    stocks.Dispose();
                    SoulStock ss = em.GetComponentData<SoulStock>(st);
                    int shares = 1 + (int)SoulEp2Util.Range(SoulEp2Util.Salt(e), day + 9u, (int)SoulEp2Util.LotsPerTrade, 46u);
                    int cost = shares * ss.m_Price;
                    if (cost >= SoulEp2Util.TradeMin && cost <= SoulEp2Util.TradeMax && cost <= w.m_Balance - TradeCushion)
                    {
                        w.m_Balance -= cost;
                        SoulHolding h = SoulHolding.Fresh(day);
                        h.m_Stock = st;
                        h.m_Ticker = ss.m_Ticker;
                        h.m_Shares = (ushort)shares;
                        h.m_Cost = cost;
                        h.m_BoughtDay = day;
                        h.m_Flags = SoulEp2Util.Roll(SoulEp2Util.Salt(e), day + 33u, 300) ? (byte)SoulHoldingFlags.DayTrader : (byte)0;
                        em.AddComponentData(e, h);
                        em.SetComponentData(e, w);
                        SoulFeel.Note(em, e, day, SoulEventKind.BoughtInOn, (short)shares, st);
                        SoulFeel.Pulse(em, e, 3);
                    }
                }
            }
        }

        // F172 -- the dividend envelope: every DivCycle days the exchange pays
        // DivPerShare per share held, and each holder feels the envelope arrive.
        private void DividendStep(EntityManager em, uint day)
        {
            if (m_Exchange.CalculateEntityCount() != 1) return;
            SoulExchange c = em.GetComponentData<SoulExchange>(m_Exchange.GetSingletonEntity());
            if (c.m_Days < SoulEp2Util.DivCycle) return;
            if (c.m_Days % SoulEp2Util.DivCycle != 0u) return;

            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0, n = souls.Length; i < n; i++)
                {
                    Entity e = souls[i];
                    if (!em.HasComponent<SoulHolding>(e)) continue;
                    SoulHolding h = em.GetComponentData<SoulHolding>(e);
                    if (!em.HasComponent<SoulStock>(h.m_Stock)) continue;
                    int paid = h.m_Shares * SoulEp2Util.DivPerShare;
                    SoulStock ss = em.GetComponentData<SoulStock>(h.m_Stock);
                    if (ss.m_LastDivDay != day)
                    {
                        ss.m_LastDivDay = day;
                        em.SetComponentData(h.m_Stock, ss);
                    }
                    if (paid <= 0) continue;
                    SoulWallet w = em.GetComponentData<SoulWallet>(e);
                    w.m_Balance += paid;
                    w.m_Lifetime += paid;
                    em.SetComponentData(e, w);
                    SoulFeel.Note(em, e, day, SoulEventKind.DividendOn, (short)SoulEp2Util.Clamp(paid, 0, 3000), h.m_Stock);
                }
            }
            finally { souls.Dispose(); }
        }

        // F175/F176 -- red or golden, the weather reaches every soul alike.
        private void Broadcast(EntityManager em, uint day, SoulEventKind kind, short value, Entity about)
        {
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                int stride = souls.Length / 64 + 1;
                for (int i = 0; i < souls.Length; i += stride)
                {
                    Entity e = souls[i];
                    SoulFeel.Note(em, e, day, kind, value, about);
                    SoulFeel.Pulse(em, e, value < 0 ? (short)-4 : (short)4);
                }
            }
            finally { souls.Dispose(); }
        }

        private void Scoop(EntityManager em, byte kind, byte prio, int value, Entity about, uint day)
        {
            if (m_Desk.CalculateEntityCount() != 1) return;
            Entity desk = m_Desk.GetSingletonEntity();
            if (!em.HasComponent<SoulNews>(desk)) return;
            SoulEp2Util.Scoop(em, desk, kind, prio, value, about, day);
        }
    }
}
