// Citizen Soul -- EP1 systems A: matters of fate and appetite (F20, F28, F29,
// F34, F37). Mortality walks a soul down the last road. Labor organizes, walks
// out, and settles. Chance gives and takes. The underworld recruits, promotes,
// and collects. Graft offers a handshake that costs. Every beat is deterministic
// (day + salt hashing, lane-staggered), fires at most once unless a gate opens,
// and journals through SoulFeel.Note so the story writer can quote it back.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    // =====================================================================
    // F20 -- MortalitySystem: the last road, walked well.
    // =====================================================================
    /// <summary>
    /// Age brings the quiet: a decline that starts in private, a bucket list
    /// written and honored, a tour of goodbyes, the thing that mattered said
    /// out loud, and -- one day -- a eulogy, when the person they chose is
    /// gone. Nothing here kills anyone; the game owns death. This is grief,
    /// legacy, and the grace of finishing well.
    /// </summary>
    public partial class MortalitySystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const uint DeclineAgeDays = 23725;    // ~65 years
        private const int DeclinePerMille = 2;        // per cadence window
        private const int BucketPerMille = 3;
        private const int BucketListDelayDays = 21;   // quiet weeks before the list
        private const uint FarewellDelayDays = 60u;
        private const uint WordsDelayDays = 7u;
        private const int FarewellMinAffinity = 40;
        private const int EulogyMood = -8;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Mortality;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Mortality = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulMortality>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulMortality>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulMortality.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("mort.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("mort.migrate", "MortalitySystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Mortality.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Mortality.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("mort.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("mort.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulMortality>(e)) return;
            SoulMortality c = em.GetComponentData<SoulMortality>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = SoulEp1Util.Salt(e);

            // A. The quiet begins -- private, slow, and noticed only by them.
            if (age >= DeclineAgeDays && (c.m_Flags & SoulMortalityFlags.Declining) == 0 &&
                (day + salt) % 3u == 0u &&
                SoulEp1Util.Roll(day, salt + 2001u, 1000u) < DeclinePerMille)
            {
                c.m_Flags |= SoulMortalityFlags.Declining;
                c.m_DeclineDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MortalityDeclineOn, 1, e);
                SoulFeel.Pulse(em, e, -4);
            }

            // B. The list gets written, in their own time. No audience for this.
            if ((c.m_Flags & SoulMortalityFlags.Declining) != 0 &&
                (c.m_Flags & SoulMortalityFlags.BucketList) == 0 &&
                day >= c.m_DeclineDay + BucketListDelayDays)
            {
                c.m_Flags |= SoulMortalityFlags.BucketList;
            }

            // C. And then honored: one thing done that was always going to wait.
            if ((c.m_Flags & SoulMortalityFlags.BucketList) != 0 &&
                (c.m_Flags & SoulMortalityFlags.BucketDone) == 0 &&
                (day + salt) % 5u == 0u &&
                SoulEp1Util.Roll(day, salt + 2002u, 1000u) < BucketPerMille)
            {
                c.m_Flags |= SoulMortalityFlags.BucketDone;
                c.m_BucketDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MortalityBucketDoneOn, 1, e);
                SoulFeel.Pulse(em, e, 5);
            }

            // D. The tour of goodbyes -- one companion hears it first.
            if ((c.m_Flags & SoulMortalityFlags.Declining) != 0 &&
                (c.m_Flags & SoulMortalityFlags.Farewell) == 0 &&
                day >= c.m_DeclineDay + FarewellDelayDays)
            {
                c.m_Flags |= SoulMortalityFlags.Farewell;
                c.m_FarewellDay = day;
                Entity pal = SoulEp1Util.Companion(em, e, FarewellMinAffinity);
                c.m_EulogyBy = pal;
                SoulFeel.Note(em, e, day, SoulEventKind.MortalityFarewellOn, 1, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 3);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 4, day);
            }

            // E. The words said out loud, a week later, when it was easier.
            if ((c.m_Flags & SoulMortalityFlags.Farewell) != 0 &&
                (c.m_Flags & SoulMortalityFlags.LastWords) == 0 &&
                day >= c.m_FarewellDay + WordsDelayDays)
            {
                c.m_Flags |= SoulMortalityFlags.LastWords;
                c.m_WordsDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MortalityFarewellOn, 2,
                    c.m_EulogyBy == Entity.Null ? e : c.m_EulogyBy);
                SoulFeel.Pulse(em, e, 2);
            }

            // F. The eulogy: the one they chose is gone first, and they speak.
            if (c.m_EulogyBy != Entity.Null && !em.Exists(c.m_EulogyBy) &&
                (c.m_Flags & SoulMortalityFlags.Eulogy) == 0)
            {
                c.m_Flags |= SoulMortalityFlags.Eulogy;
                c.m_EulogyDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.MortalityEulogyOn, 1, c.m_EulogyBy);
                SoulFeel.Pulse(em, e, EulogyMood);
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F28 -- StrikeSystem: the shop floor finds its voice.
    // =====================================================================
    /// <summary>
    /// Whispers by the machines, then a back-room meeting, then a picket
    /// line, then the walk-out, then terms read aloud. A comrade stands on
    /// the line beside them; the link tightens either way. Strikers stay
    /// marked -- the ones who walked once are the first blamed later.
    /// </summary>
    public partial class StrikeSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int RumorsPerMille = 2;
        private const int UnionPerMille = 8;
        private const int UnionMinAffinity = 30;
        private const uint PicketDelayDays = 5u;
        private const int PicketPerMille = 10;
        private const uint StrikeDelayDays = 10u;
        private const int StrikePerMille = 12;
        private const uint SettleDelayDays = 14u;
        private const int BlacklistPerMille = 2;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Labor;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Labor = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulLabor>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulLabor>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulLabor.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("labor.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("labor.migrate", "StrikeSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Labor.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Labor.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("labor.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("labor.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulLabor>(e)) return;
            SoulLabor c = em.GetComponentData<SoulLabor>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            if (!em.HasComponent<SoulCareer>(e)) return;
            uint salt = SoulEp1Util.Salt(e);

            byte conscientiousness = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                conscientiousness = p.m_Conscientiousness;
            }

            // A. Rumors move through the shop before anything moves at all.
            if ((c.m_Flags & SoulLaborFlags.Rumors) == 0 &&
                (day + salt) % 17u == 0u &&
                SoulEp1Util.Roll(day, salt + 2101u, 1000u) < RumorsPerMille)
            {
                c.m_Flags |= SoulLaborFlags.Rumors;
            }

            // B. A back-room meeting with the one already trusted.
            if ((c.m_Flags & SoulLaborFlags.Rumors) != 0 &&
                (c.m_Flags & SoulLaborFlags.Unionized) == 0 &&
                (day + salt) % 10u == 0u &&
                SoulEp1Util.Roll(day, salt + 2102u, 1000u) < UnionPerMille)
            {
                Entity pal = SoulEp1Util.Companion(em, e, UnionMinAffinity);
                c.m_Flags |= SoulLaborFlags.Unionized;
                c.m_UnionDay = day;
                c.m_Comrade = pal;
                SoulFeel.Note(em, e, day, SoulEventKind.StrikeWalkOn, 0, pal == Entity.Null ? e : pal);
                SoulFeel.Pulse(em, e, 2);
                if (pal != Entity.Null) SoulEp1Util.BumpBoth(em, e, pal, 3, day);
            }

            // C. The picket: signs in the rain, nobody famous yet.
            if ((c.m_Flags & SoulLaborFlags.Unionized) != 0 &&
                (c.m_Flags & SoulLaborFlags.Picket) == 0 &&
                day >= c.m_UnionDay + PicketDelayDays &&
                (day + salt) % 6u == 0u &&
                SoulEp1Util.Roll(day, salt + 2103u, 1000u) < PicketPerMille)
            {
                c.m_Flags |= SoulLaborFlags.Picket;
            }

            // D. The walk-out. Conscientious souls hesitate; hunger doesn't.
            if ((c.m_Flags & SoulLaborFlags.Unionized) != 0 &&
                (c.m_Flags & SoulLaborFlags.Striking) == 0 &&
                (c.m_Flags & SoulLaborFlags.Settled) == 0 &&
                day >= c.m_UnionDay + StrikeDelayDays &&
                (day + salt) % 9u == 0u &&
                SoulEp1Util.Roll(day, salt + 2104u, 1000u) < StrikePerMille)
            {
                c.m_Flags |= SoulLaborFlags.Striking;
                c.m_StrikeDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.StrikeWalkOn, 1,
                    c.m_Comrade == Entity.Null ? e : c.m_Comrade);
                SoulFeel.Pulse(em, e, -3);
            }

            // E. Terms read aloud, hands shaken. The line holds until it doesn't.
            if ((c.m_Flags & SoulLaborFlags.Striking) != 0 &&
                (c.m_Flags & SoulLaborFlags.Settled) == 0 &&
                day >= c.m_StrikeDay + SettleDelayDays)
            {
                c.m_Flags |= SoulLaborFlags.Settled;
                c.m_SettleDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.StrikeSettleOn, 1,
                    c.m_Comrade == Entity.Null ? e : c.m_Comrade);
                SoulFeel.Pulse(em, e, 4);
                if (c.m_Comrade != Entity.Null) SoulEp1Util.BumpBoth(em, e, c.m_Comrade, 3, day);
            }

            // F. Being on the list is quiet. It just never comes off.
            if ((c.m_Flags & SoulLaborFlags.Settled) != 0 &&
                (c.m_Flags & SoulLaborFlags.Blacklisted) == 0 &&
                (day + salt) % 45u == 0u &&
                SoulEp1Util.Roll(day, salt + 2105u, 1000u) < BlacklistPerMille)
            {
                c.m_Flags |= SoulLaborFlags.Blacklisted;
                SoulFeel.Pulse(em, e, -2);
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F29 -- GamblingSystem: chance, upstairs, behind the good door.
    // =====================================================================
    /// <summary>
    /// Cards, dice, tickets, wagers -- one appetite finds each soul. Nights
    /// land and nights go. A streak is a story until it is a debt; the debt
    /// is quiet until it isn't. Luck gets a name the regulars whisper.
    /// </summary>
    public partial class GamblingSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const uint PlayAgeDays = 6570;        // ~18 years
        private const int StartPerMille = 6;
        private const int BaseWinChancePerMille = 450;
        private const int StreakBigWin = 3;
        private const int StreakJackpot = 5;
        private const int DebtPerMille = 30;
        private const int LossPerMille = 1000 - BaseWinChancePerMille;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Gamble;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Gamble = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulGamble>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulGamble>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulGamble.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("gamble.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("gamble.migrate", "GamblingSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Gamble.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Gamble.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("gamble.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("gamble.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulGamble>(e)) return;
            SoulGamble c = em.GetComponentData<SoulGamble>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = SoulEp1Util.Salt(e);

            // A. Some souls find the tables on their own. No knock; the door
            //    just happens to be open the night they walk past.
            if (age >= PlayAgeDays && (c.m_Flags & SoulGambleFlags.Regular) == 0 &&
                (day + salt) % 11u == 0u &&
                SoulEp1Util.Roll(day, salt + 2201u, 1000u) < StartPerMille)
            {
                c.m_Flags |= SoulGambleFlags.Regular;
                c.m_Variant = (byte)(SoulEp1Util.Roll(day, salt + 2202u, 4u) + 1u);
            }

            // B. A night at the tables: the streak climbs, breaks, or holds.
            if ((c.m_Flags & SoulGambleFlags.Regular) != 0 &&
                (day + salt) % 7u == 0u)
            {
                c.m_LastBetDay = day;
                uint draw = SoulEp1Util.Roll(day, salt + 2203u, 1000u);
                if (draw < BaseWinChancePerMille)
                {
                    if (c.m_Streak < 255) c.m_Streak++;
                    c.m_WinDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.BetWinOn, (short)c.m_Streak, e);
                    SoulFeel.Pulse(em, e, 3);

                    if (c.m_Streak >= StreakBigWin && (c.m_Flags & SoulGambleFlags.BigWin) == 0)
                    {
                        c.m_Flags |= SoulGambleFlags.BigWin;
                    }

                    // The night the regulars start calling them lucky.
                    if (c.m_Streak >= StreakJackpot && (c.m_Flags & SoulGambleFlags.LuckyName) == 0)
                    {
                        c.m_Flags |= SoulGambleFlags.LuckyName;
                        SoulFeel.Note(em, e, day, SoulEventKind.JackpotOn, 1, e);
                        SoulFeel.Pulse(em, e, 8);
                    }
                }
                else
                {
                    c.m_Streak = 0;
                    c.m_LossDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.BetLossOn, 1, e);
                    SoulFeel.Pulse(em, e, -3);

                    if ((c.m_Flags & SoulGambleFlags.BigWin) != 0 &&
                        (c.m_Flags & SoulGambleFlags.BigLoss) == 0)
                    {
                        c.m_Flags |= SoulGambleFlags.BigLoss;
                    }

                    // A debt is not a night. It is every night after.
                    if ((c.m_Flags & SoulGambleFlags.BigLoss) != 0 &&
                        (c.m_Flags & SoulGambleFlags.Debt) == 0 &&
                        (day + salt) % 13u == 0u &&
                        SoulEp1Util.Roll(day, salt + 2204u, 1000u) < DebtPerMille)
                    {
                        c.m_Flags |= SoulGambleFlags.Debt;
                        SoulFeel.Note(em, e, day, SoulEventKind.BetLossOn, 2, e);
                        SoulFeel.Pulse(em, e, -5);
                    }
                }
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F34 -- CrimeSystem: the other ladder, climbed quietly.
    // =====================================================================
    /// <summary>
    /// A runner first -- small tasks, paid in silence. Then made. Then the
    /// money needs washing and they have the hands for it. The favor comes
    /// due eventually; some witness what they shouldn't and take the fall;
    /// some walk away old and done. Nobody in this system advertises.
    /// </summary>
    public partial class CrimeSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const uint RecruitAgeDays = 6570;     // ~18 years
        private const int RunnerPerMille = 3;
        private const int MadeMinAffinity = 20;
        private const uint MadeDelayDays = 90u;
        private const int MadePerMille = 4;
        private const int LaunderPerMille = 3;
        private const int FavorPerMille = 4;
        private const int WitnessPerMille = 10;
        private const int FallPerMille = 8;
        private const uint ReformDelayDays = 365u;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Underworld;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Underworld = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulUnderworld>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulUnderworld>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulUnderworld.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("under.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("under.migrate", "CrimeSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Underworld.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Underworld.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("under.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("under.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulUnderworld>(e)) return;
            SoulUnderworld c = em.GetComponentData<SoulUnderworld>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = SoulEp1Util.Salt(e);

            byte conscientiousness = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                conscientiousness = p.m_Conscientiousness;
            }

            // A. The runner years: errands that never quite have a name.
            if (age >= RecruitAgeDays && (c.m_Flags & SoulUnderFlags.Runner) == 0 &&
                (day + salt) % 23u == 0u &&
                SoulEp1Util.Roll(day, salt + 2301u, 1000u) < RunnerPerMille)
            {
                Entity mate = SoulEp1Util.Companion(em, e, MadeMinAffinity);
                c.m_Flags |= SoulUnderFlags.Runner;
                c.m_Rank = 1;
                c.m_JoinDay = day;
                c.m_Mate = mate;
                SoulFeel.Note(em, e, day, SoulEventKind.UnderworldInOn, 1, mate == Entity.Null ? e : mate);
                SoulFeel.Pulse(em, e, -2);
            }

            // B. Made. The ceremony is a nod in a hallway, and it means everything.
            if ((c.m_Flags & SoulUnderFlags.Runner) != 0 &&
                (c.m_Flags & SoulUnderFlags.Made) == 0 &&
                day >= c.m_JoinDay + MadeDelayDays &&
                (day + salt) % 19u == 0u &&
                SoulEp1Util.Roll(day, salt + 2302u, 1000u) < MadePerMille)
            {
                c.m_Flags |= SoulUnderFlags.Made;
                c.m_Rank = 2;
                c.m_RankDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.UnderworldInOn, 2,
                    c.m_Mate == Entity.Null ? e : c.m_Mate);
                SoulFeel.Pulse(em, e, 3);
                if (c.m_Mate != Entity.Null) SoulEp1Util.BumpBoth(em, e, c.m_Mate, 3, day);
            }

            // C. Laundering: the money needs clean hands, and now they qualify.
            if ((c.m_Flags & SoulUnderFlags.Made) != 0 &&
                (c.m_Flags & SoulUnderFlags.Launderer) == 0 &&
                (day + salt) % 27u == 0u &&
                SoulEp1Util.Roll(day, salt + 2303u, 1000u) < LaunderPerMille)
            {
                c.m_Flags |= SoulUnderFlags.Launderer;
                c.m_Rank = 3;
                c.m_RankDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.UnderworldInOn, 3,
                    c.m_Mate == Entity.Null ? e : c.m_Mate);
                SoulFeel.Pulse(em, e, 3);
            }

            // D. The favor, called in. It is never light, and it is never no.
            if ((c.m_Flags & SoulUnderFlags.Made) != 0 &&
                (c.m_Flags & SoulUnderFlags.Reformed) == 0 &&
                (c.m_Flags & SoulUnderFlags.Fallen) == 0 &&
                (c.m_Flags & SoulUnderFlags.Witness) == 0 &&
                (day + salt) % 21u == 0u &&
                SoulEp1Util.Roll(day, salt + 2304u, 1000u) < FavorPerMille)
            {
                SoulFeel.Note(em, e, day, SoulEventKind.FavorCalledOn, 1,
                    c.m_Mate == Entity.Null ? e : c.m_Mate);
                SoulFeel.Pulse(em, e, -4);

                // Some of those favors include watching something.
                if (SoulEp1Util.Roll(day, salt + 2305u, 1000u) < WitnessPerMille)
                {
                    c.m_Flags |= SoulUnderFlags.Witness;
                    SoulFeel.Pulse(em, e, -3);
                }
            }

            // E. Taking the fall: the ladder has a bottom, and it found them.
            if ((c.m_Flags & SoulUnderFlags.Witness) != 0 &&
                (c.m_Flags & SoulUnderFlags.Fallen) == 0 &&
                (c.m_Flags & SoulUnderFlags.Reformed) == 0 &&
                (day + salt) % 29u == 0u &&
                SoulEp1Util.Roll(day, salt + 2306u, 1000u) < FallPerMille)
            {
                c.m_Flags |= SoulUnderFlags.Fallen;
                SoulFeel.Note(em, e, day, SoulEventKind.UnderworldOutOn, 0,
                    c.m_Mate == Entity.Null ? e : c.m_Mate);
                SoulFeel.Pulse(em, e, -6);
            }

            // F. Reformed -- not a word they use. Just the jobs stop calling.
            if (((c.m_Flags & SoulUnderFlags.Fallen) != 0 || (c.m_Flags & SoulUnderFlags.Made) != 0) &&
                (c.m_Flags & SoulUnderFlags.Reformed) == 0 &&
                day >= c.m_JoinDay + ReformDelayDays &&
                conscientiousness > 50 && age >= 23725u / 2u)
            {
                c.m_Flags |= SoulUnderFlags.Reformed;
                SoulFeel.Note(em, e, day, SoulEventKind.UnderworldOutOn, 1, e);
                SoulFeel.Pulse(em, e, 6);
            }

            em.SetComponentData(e, c);
        }
    }

    // =====================================================================
    // F37 -- CorruptionSystem: the handshake that costs.
    // =====================================================================
    /// <summary>
    /// An envelope under a desk. The soul sitting there takes it, refuses
    /// it, or lets it sit untouched until someone takes it away. What is
    /// taken stays taken; what is refused pays off quietly; what is taken
    /// and found out has a long season to survive.
    /// </summary>
    public partial class CorruptionSystem : SoulSystemBase
    {
        private const int SoulLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        private const int OfferPerMille = 4;
        private const uint DecideDelayDays = 2u;
        private const int RefuseMood = 3;
        private const int TakeMood = -3;
        private const int ExposePerMille = 7;
        private const uint SurviveDelayDays = 30u;

        protected override int Lanes { get { return SoulLanes; } }

        private EntityQuery m_Graft;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Graft = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulGraft>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int budget = 0;
                    try
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (budget >= MigrateBudget) break;
                            Entity e = all[i];
                            if (!em.Exists(e)) continue;
                            if (em.HasComponent<SoulGraft>(e)) continue;
                            try
                            {
                                em.AddComponentData(e, SoulGraft.Fresh(day));
                                budget++;
                            }
                            catch (Exception ex)
                            {
                                SoulLog.Exception("graft.migrate." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        all.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Once("graft.migrate", "CorruptionSystem: migration pass failed: " + ex.Message);
            }

            try
            {
                if (m_Graft.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> souls = m_Graft.ToEntityArray(Allocator.Temp);
                    int faults = 0;
                    try
                    {
                        for (int i = 0; i < souls.Length; i++)
                        {
                            Entity e = souls[i];
                            if (!em.Exists(e)) continue;
                            if (!Matches(e, lane, SoulLanes)) continue;
                            try
                            {
                                Advance(em, e, day);
                            }
                            catch (Exception ex)
                            {
                                faults++;
                                if (faults <= FaultLogCap) SoulLog.Exception("graft.fault." + e.Index, ex);
                            }
                        }
                    }
                    finally
                    {
                        souls.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("graft.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulGraft>(e)) return;
            SoulGraft c = em.GetComponentData<SoulGraft>(e);
            if (c.m_LastDay == day) return;
            c.m_LastDay = day;

            // The offer only comes to souls with something to sell.
            if (!em.HasComponent<SoulCareer>(e))
            {
                em.SetComponentData(e, c);
                return;
            }

            uint salt = SoulEp1Util.Salt(e);

            byte conscientiousness = 50;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                conscientiousness = p.m_Conscientiousness;
            }

            // A. The offer: a door held open, nothing said out loud.
            if ((c.m_Flags & SoulGraftFlags.Offered) == 0 &&
                (c.m_Flags & SoulGraftFlags.Took) == 0 &&
                (c.m_Flags & SoulGraftFlags.Refused) == 0 &&
                (day + salt) % 19u == 0u &&
                SoulEp1Util.Roll(day, salt + 2401u, 1000u) < OfferPerMille)
            {
                SoulCareer career = em.GetComponentData<SoulCareer>(e);
                if (career.m_Level >= 2)
                {
                    Entity offerer = SoulEp1Util.Companion(em, e, 20);
                    c.m_Flags |= SoulGraftFlags.Offered;
                    c.m_OfferDay = day;
                    c.m_Target = offerer;
                }
            }

            // B. The decision. Conscience is a load-bearing wall.
            if ((c.m_Flags & SoulGraftFlags.Offered) != 0 &&
                (c.m_Flags & SoulGraftFlags.Took) == 0 &&
                (c.m_Flags & SoulGraftFlags.Refused) == 0 &&
                day >= c.m_OfferDay + DecideDelayDays)
            {
                bool refuse = SoulEp1Util.Roll(day, salt + 2402u, 100u) < (uint)conscientiousness;
                if (refuse)
                {
                    c.m_Flags |= SoulGraftFlags.Refused;
                    SoulFeel.Note(em, e, day, SoulEventKind.GraftRefusedOn, 1,
                        c.m_Target == Entity.Null ? e : c.m_Target);
                    SoulFeel.Pulse(em, e, RefuseMood);
                }
                else
                {
                    c.m_Flags |= SoulGraftFlags.Took;
                    c.m_TakeDay = day;
                    SoulFeel.Note(em, e, day, SoulEventKind.GraftTakenOn, 1,
                        c.m_Target == Entity.Null ? e : c.m_Target);
                    SoulFeel.Pulse(em, e, TakeMood);
                }
            }

            // C. Found out. The envelope always has a paper trail.
            if ((c.m_Flags & SoulGraftFlags.Took) != 0 &&
                (c.m_Flags & SoulGraftFlags.Exposed) == 0 &&
                (day + salt) % 41u == 0u &&
                SoulEp1Util.Roll(day, salt + 2403u, 1000u) < ExposePerMille)
            {
                c.m_Flags |= SoulGraftFlags.Exposed;
                c.m_ExposeDay = day;
                SoulFeel.Note(em, e, day, SoulEventKind.GraftTakenOn, 2, e);
                SoulFeel.Pulse(em, e, -6);
            }

            // D. Survived: the season passes, the desk is still theirs.
            if ((c.m_Flags & SoulGraftFlags.Exposed) != 0 &&
                (c.m_Flags & SoulGraftFlags.Survived) == 0 &&
                day >= c.m_ExposeDay + SurviveDelayDays)
            {
                c.m_Flags |= SoulGraftFlags.Survived;
                SoulFeel.Note(em, e, day, SoulEventKind.GraftTakenOn, 3, e);
                SoulFeel.Pulse(em, e, 2);
            }

            // E. Clean hands on the record: refused and still standing.
            if ((c.m_Flags & SoulGraftFlags.Refused) != 0 &&
                (c.m_Flags & SoulGraftFlags.CleanHands) == 0)
            {
                c.m_Flags |= SoulGraftFlags.CleanHands;
                SoulFeel.Note(em, e, day, SoulEventKind.CleanHandsOn, 1, e);
            }

            em.SetComponentData(e, c);
        }
    }
}
