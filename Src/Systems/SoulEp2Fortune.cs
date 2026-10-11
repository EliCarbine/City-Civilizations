// Citizen Soul -- EP2 Fortune (F152-F157). Every soul keeps a wallet:
// a wage on payday, the cost of living, sudden windfalls and sudden costs,
// the lean stretch when the balance goes red, and milestones on the way up.
// This system is the only routine writer of SoulWallet; other EP2 systems
// read the wallet and may move balance, but never touch its day bookkeeping.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    public partial class FortuneSystem : SoulSystemBase
    {
        private const int MigrateBudget = 64;

        private EntityQuery m_All;
        private EntityQuery m_Desk;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return 64; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
            m_Desk = GetEntityQuery(ComponentType.ReadOnly<SoulNews>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            NativeArray<Entity> souls = m_All.ToEntityArray(Allocator.Temp);
            try
            {
                // First sight: open a wallet. Souls are born with one at
                // soul-init; this also catches saves made before the fortune
                // layer existed.
                int budget = 0;
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (em.HasComponent<SoulWallet>(e)) continue;
                    if (budget >= MigrateBudget) break;
                    budget++;
                    try { em.AddComponentData(e, SoulWallet.Fresh(day)); }
                    catch (Exception ex)
                    {
                        DisableSelf("fortune migrate: " + ex.Message);
                        return;
                    }
                }

                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    if (!em.HasComponent<SoulWallet>(e)) continue;
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
            SoulWallet w = em.GetComponentData<SoulWallet>(e);
            if (w.m_LastDay == day) return;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            bool grown = age >= (uint)SoulEp2Util.TeenDay;
            int luck = w.m_Luck;
            uint salt = SoulEp2Util.Salt(e);

            // F152 -- payday: a wage lands every PayCycle days, nudged by
            // luck, never twice the same.
            if (grown && (w.m_LastPayDay == 0u || day - w.m_LastPayDay >= (uint)SoulEp2Util.PayCycle))
            {
                w.m_LastPayDay = day;
                int pay = SoulEp2Util.WageBase + luck * SoulEp2Util.WageLuckStep;
                pay += (int)SoulEp2Util.Range(salt, day, 21, 20u);
                if (pay < 4) pay = 4;
                w.m_Balance += pay;
                w.m_Lifetime += pay;
                if (Gate(ref w, day))
                    SoulFeel.Note(em, e, day, SoulEventKind.PaidOn, (short)Math.Min(pay, 30000), e);
            }

            // F153 -- the cost of living, every UpkeepDay, staggered so the
            // whole city never settles up on one morning.
            if (grown && (day + salt % (uint)SoulEp2Util.UpkeepDay) % (uint)SoulEp2Util.UpkeepDay == 0u)
                w.m_Balance -= SoulEp2Util.UpkeepDay;

            // F154 -- sudden money, sudden costs. Luck tilts the coin.
            int wind = SoulEp2Util.WindfallPerMille + (luck > 0 ? luck / 4 : 0);
            int mish = SoulEp2Util.MishapPerMille + (luck < 0 ? -luck / 4 : 0);
            if (SoulEp2Util.Roll(salt, day, Math.Max(1, wind)))
            {
                w.m_Balance += SoulEp2Util.WindfallGain;
                w.m_Lifetime += SoulEp2Util.WindfallGain;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.WindfallOn, (short)Math.Min(SoulEp2Util.WindfallGain, 30000), e);
                    SoulFeel.Pulse(em, e, 4);
                }
                Scoop(em, SoulWire.Money, 2, SoulEp2Util.WindfallGain, e, day);
            }
            else if (SoulEp2Util.Roll(salt, day + 1u, Math.Max(1, mish)))
            {
                w.m_Balance -= SoulEp2Util.MishapCost;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.MishapOn, (short)Math.Min(SoulEp2Util.MishapCost, 30000), e);
                    SoulFeel.Pulse(em, e, -5);
                }
                Scoop(em, SoulWire.Money, 3, -SoulEp2Util.MishapCost, e, day);
            }

            // F155 -- the lean stretch: red numbers flagged, honored, and
            // then the way back up.
            if (w.m_Balance < 0)
            {
                if (w.m_LeanDays < ushort.MaxValue) w.m_LeanDays++;
                w.m_Flags |= SoulWalletFlags.Leaning;
                if (w.m_LeanDays == 30 && Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.TightMonthOn, (short)Math.Max(w.m_Balance, -30000), e);
                    SoulFeel.Pulse(em, e, -4);
                    Scoop(em, SoulWire.Money, 2, w.m_Balance, e, day);
                }
            }
            else if ((w.m_Flags & SoulWalletFlags.Leaning) != 0)
            {
                w.m_Flags = (byte)((w.m_Flags & ~SoulWalletFlags.Leaning) | SoulWalletFlags.Recovered);
                w.m_LeanDays = 0;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.PickedUpOn, 0, e);
                    SoulFeel.Pulse(em, e, 8);
                    Scoop(em, SoulWire.Money, 3, 100, e, day);
                }
            }
            else if (w.m_LeanDays != 0) w.m_LeanDays = 0;

            // debts never spiral in this city
            if (w.m_Balance < SoulEp2Util.DebtFloor) w.m_Balance = SoulEp2Util.DebtFloor;

            byte target = 0;
            if (w.m_Lifetime >= SoulEp2Util.Milestone4) target = 3;
            else if (w.m_Lifetime >= SoulEp2Util.Milestone3) target = 2;
            else if (w.m_Lifetime >= SoulEp2Util.Milestone2) target = 1;
            if (target > w.m_Milestone)
            {
                w.m_Milestone = target;
                if (Gate(ref w, day))
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.MilestoneOn, (short)target, e);
                    SoulFeel.Pulse(em, e, 5 * target);
                }
                if (target >= 2) Scoop(em, SoulWire.Money, (byte)(2 + target), target, e, day);
            }

            w.m_LastDay = day;
            em.SetComponentData(e, w);
        }

        // F157 -- ambient beats are rationed per soul (NoteGapDays) so a
        // long life never reads like a receipt printer. Landmark beats
        // skip the gate.
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
