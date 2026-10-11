// Citizen Soul -- EP2 Venture (F158-F165). Citizens with a little money and
// a little nerve hang out a sign: bakeries, cafes, barbershops, print shops,
// record shops, garages, food trucks, flower stands. Ventures live day to
// day on sentiment and cash, hire when healthy, and can go the distance:
// ready for the exchange, then the bell. They can also fail and close --
// and the rail is real: a closed soul tries again someday.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class VentureSystem : SoulSystemBase
    {
        private const int MigrateBudget = 64;
        private const int StaffMax = 6;        // small businesses stay small
        private const int RingPerMille = 25;   // daily chance the bell rings once ready
        private const int BustRollPerMille = 15;

        private EntityQuery m_All;
        private EntityQuery m_Desk;
        private EntityQuery m_Stocks;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
            m_Stocks = GetEntityQuery(ComponentType.ReadOnly<SoulStock>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            NativeArray<Entity> souls = m_All.ToEntityArray(Allocator.Temp);
            try
            {
                int budget = 0;
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (em.HasComponent<SoulVenture>(e)) continue;
                    if (budget >= MigrateBudget) break;
                    budget++;
                    try { em.AddComponentData(e, SoulVenture.Fresh(day)); }
                    catch (Exception ex)
                    {
                        DisableSelf("venture migrate: " + ex.Message);
                        return;
                    }
                }

                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (!em.HasComponent<SoulVenture>(e)) continue;
                    try { Advance(em, e, day); }
                    catch (Exception) { /* one sick soul never stops the city */ }
                }
            }
            finally
            {
                souls.Dispose();
            }
        }

        private void Advance(EntityManager em, Entity e, uint day)
        {
            SoulVenture c = em.GetComponentData<SoulVenture>(e);
            if (c.m_LastDay == day) return;

            if (!em.HasComponent<SoulWallet>(e)) { c.m_LastDay = day; em.SetComponentData(e, c); return; }
            SoulWallet w = em.GetComponentData<SoulWallet>(e);
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = SoulEp2Util.Salt(e);
            int luck = w.m_Luck;

            // Closed shop: the rail. A busted soul tries again someday.
            if (c.m_BustDay != 0)
            {
                if (SoulEp2Util.Roll(salt, day, Math.Max(1, BustRollPerMille)))
                {
                    c.m_BustDay = 0;
                    c.m_Flags = SoulVentureFlags.Open;
                    c.m_Health = SoulEp2Util.VentureHealthStart;
                    c.m_Cash = 0;
                    c.m_GoodDays = 0;
                    c.m_BadDays = 0;
                    c.m_Staff = 0;
                    c.m_OpenDay = day;
                    if (Gate(ref w, day))
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.OpenedOn, (short)c.m_Kind, e);
                        SoulFeel.Pulse(em, e, 5);
                        Scoop(em, SoulWire.Venture, 2, 1, e, day);
                    }
                }
                c.m_LastDay = day;
                em.SetComponentData(e, c);
                return;
            }

            // First sign: hang it out when grown, able, and willing.
            if ((c.m_Flags & SoulVentureFlags.Open) == 0)
            {
                if (age < (uint)SoulEp2Util.TeenDay) { c.m_LastDay = day; em.SetComponentData(e, c); return; }
                bool selfMade = luck >= SoulEp2Util.SelfMadeLuck;
                if (!selfMade && w.m_Balance < SoulEp2Util.VentureStake)
                { c.m_LastDay = day; em.SetComponentData(e, c); return; }
                if (!SoulEp2Util.Roll(salt, day, 8))
                { c.m_LastDay = day; em.SetComponentData(e, c); return; }
                c.m_Flags = SoulVentureFlags.Open;
                c.m_Kind = (byte)(1 + SoulEp2Util.Range(salt, day, 8, 30u));
                c.m_OpenDay = day;
                c.m_Health = SoulEp2Util.VentureHealthStart;
                c.m_Cash = selfMade ? 4 : 12;
                if (!selfMade)
                {
                    w.m_Balance -= SoulEp2Util.VentureStake;
                    em.SetComponentData(e, w);
                }
                SoulFeel.Note(em, e, day, SoulEventKind.OpenedOn, (short)c.m_Kind, e);
                SoulFeel.Pulse(em, e, 6);
                Scoop(em, SoulWire.Venture, 3, (int)c.m_Kind, e, day);
                c.m_LastDay = day;
                em.SetComponentData(e, c);
                return;
            }

            // Day to day: sentiment drifts; the till fills or drains.
            int drift = (int)SoulEp2Util.Range(salt, day, 27, 41u) - 13 + luck / 8;
            int health = SoulEp2Util.Clamp(c.m_Health + drift, -100, 100);
            c.m_Health = (sbyte)health;

            int profit = (int)SoulEp2Util.Range(salt, day ^ 5u, 19, 42u) - 6 + health / 12 + luck / 6;
            int cash = c.m_Cash + profit;
            if (cash < 0) { cash = 0; health -= 4; c.m_Health = (sbyte)SoulEp2Util.Clamp(health, -100, 100); profit = -1; }
            c.m_Cash = cash;

            if (profit >= 5)
            {
                if (c.m_GoodDays < 255) c.m_GoodDays++;
                if (c.m_GoodDays == 1 && Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.FirstSaleOn, (short)profit, e);
                    SoulFeel.Pulse(em, e, 4);
                }
                if (c.m_GoodDays == 60 && Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.BusyOn, (short)c.m_GoodDays, e);
                    SoulFeel.Pulse(em, e, 3);
                    Scoop(em, SoulWire.Venture, 2, 2, e, day);
                }
                // the owner eats first
                w.m_Balance += profit;
                w.m_Lifetime += profit;
            }
            else if (profit < 0)
            {
                if (c.m_BadDays < 255) c.m_BadDays++;
                if (c.m_BadDays == 60 && Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.SlowOn, (short)health, e);
                    SoulFeel.Pulse(em, e, -3);
                }
            }

            if (health >= SoulEp2Util.VentureHireHealth && c.m_Staff < 8 &&
                SoulEp2Util.Roll(salt, day + 2u, 40))
            {
                c.m_Staff++;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.HiredOn, (short)c.m_Staff, e);
                    SoulFeel.Pulse(em, e, 5);
                    Scoop(em, SoulWire.Venture, 2, 1, e, day);
                }
            }

            if (health >= SoulEp2Util.VentureGood)
                c.m_Flags = (byte)((c.m_Flags | SoulVentureFlags.Thriving) & ~SoulVentureFlags.Strained);
            else if (health <= SoulEp2Util.VentureStrain)
                c.m_Flags = (byte)((c.m_Flags | SoulVentureFlags.Strained) & ~SoulVentureFlags.Thriving);

            if (c.m_ListedDay == 0u && (c.m_Flags & SoulVentureFlags.IpoReady) == 0
                && health >= SoulEp2Util.IpoHealth && c.m_GoodDays >= SoulEp2Util.IpoGoodDays)
            {
                c.m_Flags = (byte)(c.m_Flags | SoulVentureFlags.IpoReady);
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.IpoReadyOn, (short)c.m_GoodDays, e);
                    SoulFeel.Pulse(em, e, 6);
                    Scoop(em, SoulWire.Venture, 3, 1, e, day);
                }
            }

            if (c.m_ListedDay == 0u && (c.m_Flags & SoulVentureFlags.IpoReady) != 0
                && SoulEp2Util.Roll(SoulEp2Util.Salt(e), day + 2u, RingPerMille))
            {
                int bell = SoulEp2Util.Clamp(20 + health * 2 + c.m_Cash / 3, 20, 400);
                Entity st = em.CreateEntity();
                em.AddComponentData(st, SoulStock.Make(e, SoulEp2Util.TickerIndex(e), (byte)SoulEp2Util.Clamp(1 + c.m_GoodDays / 30, 1, 5), bell, day));
                c.m_Flags = (byte)((c.m_Flags | SoulVentureFlags.Listed) & ~SoulVentureFlags.IpoReady);
                c.m_ListedDay = day;
                SoulHolding h = SoulHolding.Fresh(day);
                h.m_Stock = st;
                h.m_Ticker = SoulEp2Util.TickerIndex(e);
                h.m_Shares = 12;
                h.m_Cost = 12 * bell;
                h.m_BoughtDay = day;
                em.AddComponentData(e, h);
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.RangBellOn, 12, st);
                    SoulFeel.Pulse(em, e, 9);
                    Scoop(em, SoulWire.Venture, 3, 1, e, day);
                }
            }

            if (c.m_BustDay == 0u && (c.m_Flags & SoulVentureFlags.Busted) == 0
                && (health <= SoulEp2Util.VentureCloseHealth
                    || (c.m_Cash <= 0 && (c.m_Flags & SoulVentureFlags.Strained) != 0
                        && SoulEp2Util.Roll(SoulEp2Util.Salt(e), day + 4u, Math.Max(1, BustRollPerMille)))))
            {
                c.m_Flags = (byte)((c.m_Flags | SoulVentureFlags.Busted) & ~SoulVentureFlags.Thriving);
                c.m_BustDay = day;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.ToughCallOn, 1, e);
                    SoulFeel.Pulse(em, e, -8);
                    Scoop(em, SoulWire.Venture, 2, 1, e, day);
                }
            }

            c.m_LastDay = day;
            em.SetComponentData(e, c);
            em.SetComponentData(e, w);
        }

        private static bool Gate(ref SoulWallet w, uint day)
        {
            if (w.m_LastNoteDay != 0u && day - w.m_LastNoteDay < (uint)SoulEp2Util.NoteGapDays) return false;
            w.m_LastNoteDay = day;
            return true;
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
