// Citizen Soul — social simulation systems (M1/M2/M3).
//
//  SocializeSystem        : co-located citizens meet; edges form, friends and
//                           rivals are made (venue-hash lane slicing, pair budget).
//  FightResolutionSystem  : advances brawls round by round, resolves them, and
//                           leaves grudges (and sometimes injuries) behind.
//  InjurySystem           : heals recoveries over time; rolls rare accidents.
//  MemoryDecaySystem      : cools idle friendships; prunes links to the dead.
//
// All systems inherit SoulSystemBase: fault-isolated, day-sliced, budgeted.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Venue co-location stage. Every citizen is checked at most once per day,
    /// during the pass that owns their venue's hash lane. Citizens sharing a
    /// venue (and not already paired this pass) touch an edge.
    /// </summary>
    public partial class SocializeSystem : SoulSystemBase
    {
        private EntityQuery m_Souls;
        private EntityQuery m_Stats;

        protected override int Interval { get { return 4096; } }    // 64 passes/day
        protected override int Lanes { get { return SoulConsts.SocialLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulPersonality>(),
                    ComponentType.ReadOnly<Mood>()
                }
            });
            m_Stats = GetEntityQuery(ComponentType.ReadWrite<SoulCityStats>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            NativeHashMap<long, Entity> present = new NativeHashMap<long, Entity>(
                souls.Length > 0 ? souls.Length / 4 + 8 : 8, Allocator.Temp);
            int pairs = 0;
            int formed = 0;
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    if (pairs >= SoulTune.VenuePairBudget) break;
                    Entity e = souls[i];
                    try
                    {
                        if (!em.HasComponent<SoulMeta>(e)) continue;       // mid-init or torn down
                        if (em.IsComponentEnabled<Injured>(e)) continue;   // injured citizens stay home
                        if (SoulMindGate.Homebound(em, e)) continue;       // M3 F13/F35: withdrawn or imprisoned souls stay home
                        SoulMeta meta = em.GetComponentData<SoulMeta>(e);
                        if (meta.m_LastSocialDay == day) continue;

                        Entity venue = Entity.Null;
                        byte venueKind = 2;
                        SoulVanilla.TryGetBuilding(em, e, out venue);
                        if (venue == Entity.Null)
                        {
                            SoulVanilla.TryGetHome(em, e, out venue);
                            venueKind = 0;
                        }
                        if (venue == Entity.Null) continue;
                        long key = SoulVanilla.VenueKey(venue, venueKind);
                        if (VenueLane(key, Lanes) != lane) continue;

                        meta.m_LastSocialDay = day;                        // checked; no double visits
                        em.SetComponentData(e, meta);

                        Entity other;
                        if (present.TryGetValue(key, out other))
                        {
                            present.Remove(key);
                            if (other != e && em.Exists(other) && em.HasComponent<SoulTag>(other))
                            {
                                if (Interact(em, e, other, day)) formed++;
                                pairs++;
                            }
                        }
                        else
                        {
                            present.TryAdd(key, e);
                        }
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("SocializeSystem first fault: " + ex.Message);
                    }
                }
            }
            finally
            {
                present.Dispose();
                souls.Dispose();
            }
            if (formed > 0) FlushFriendships(em, formed);
            if (faults > 0) SoulLog.Warn("SocializeSystem: " + faults + " fault(s) this pass.");
        }

        /// <summary>Deterministic venue→lane hash; stable across passes and saves.</summary>
        internal static int VenueLane(long key, int lanes)
        {
            unchecked
            {
                ulong h = (ulong)key * 2654435761ul;
                h ^= h >> 29;
                h *= 1099511628211ul;
                h ^= h >> 32;
                return (int)(h % (ulong)lanes);
            }
        }

        /// <summary>One touch between two present citizens; true if they became friends.</summary>
        private static bool Interact(EntityManager em, Entity a, Entity b, uint day)
        {
            SoulPersonality pa = em.GetComponentData<SoulPersonality>(a);
            SoulPersonality pb = em.GetComponentData<SoulPersonality>(b);
            Mood ma = em.GetComponentData<Mood>(a);
            Mood mb = em.GetComponentData<Mood>(b);

            Entity homeA = Entity.Null, homeB = Entity.Null;
            Entity workA = Entity.Null, workB = Entity.Null;
            SoulVanilla.TryGetHome(em, a, out homeA);
            SoulVanilla.TryGetHome(em, b, out homeB);
            SoulVanilla.TryGetWorkplace(em, a, out workA);
            SoulVanilla.TryGetWorkplace(em, b, out workB);
            bool sameHome = homeA != Entity.Null && homeA == homeB;
            bool sameWork = workA != Entity.Null && workA == workB;
            byte venueFlags = sameWork ? (byte)SoulEdgeFlags.Colleague : (byte)0;

            DynamicBuffer<SoulEdge> ea = em.GetBuffer<SoulEdge>(a);
            DynamicBuffer<SoulEdge> eb = em.GetBuffer<SoulEdge>(b);
            int preA = SoulEdgeUtil.Find(ea, b);
            int preB = SoulEdgeUtil.Find(eb, a);
            bool freshA = preA < 0;
            bool freshB = preB < 0;

            // M4 (F61): friends apart for a long spell meet again
            if (preA >= 0)
            {
                uint gap = day - ea[preA].m_LastDay;
                if (gap >= (uint)SoulTune.ReunionGapDays && ea[preA].m_Affinity >= SoulTune.ReunionMinAffinity)
                {
                    short g = (short)Math.Min(gap, 32000u);
                    SoulFeel.Note(em, a, day, SoulEventKind.Reunited, g, b);
                    SoulFeel.Note(em, b, day, SoulEventKind.Reunited, g, a);
                    SoulFeel.Pulse(em, a, SoulTune.ReunionMood);
                    SoulFeel.Pulse(em, b, SoulTune.ReunionMood);
                }
            }

            Unity.Mathematics.Random rng = SoulRng.For(pa.m_SoulSeed, a, day * 977u + 31u);
            int rng01 = rng.NextInt(0, 100);
            int compat = SoulRules.PersonalityCompat(pa, pb);
            int delta = SoulRules.InteractionDelta(compat, pa.m_Quirk, pb.m_Quirk,
                ma.m_Valence, mb.m_Valence, venueFlags, rng01);

            int ia = SoulEdgeUtil.Touch(ea, b, delta, day, venueFlags, sameHome);
            int ib = SoulEdgeUtil.Touch(eb, a, delta, day, venueFlags, sameHome);
            byte chA = SoulEdgeUtil.ApplyThresholds(ea, ia);
            byte chB = SoulEdgeUtil.ApplyThresholds(eb, ib);
            short affA = (short)ea[ia].m_Affinity;
            short affB = (short)eb[ib].m_Affinity;

            if (freshA) SoulFeel.Note(em, a, day, SoulEventKind.Met, affA, b);
            if (freshB) SoulFeel.Note(em, b, day, SoulEventKind.Met, affB, a);

            bool formed = false;
            if ((chA & SoulTune.ChangeBefriended) != 0)
            {
                SoulFeel.Pulse(em, a, SoulTune.FriendMood);
                SoulFeel.Note(em, a, day, SoulEventKind.Befriended, affA, b);
                formed = true;
            }
            else if ((chA & SoulTune.ChangeFellOut) != 0)
            {
                SoulFeel.Pulse(em, a, SoulTune.FalloutMood);
                SoulFeel.Note(em, a, day, SoulEventKind.FellOut, affA, b);
            }
            if ((chA & SoulTune.ChangeClose) != 0)
            {
                SoulFeel.Pulse(em, a, SoulTune.FirstCloseMood);
            }
            if ((chA & SoulTune.ChangeRival) != 0)
            {
                SoulFeel.Pulse(em, a, SoulTune.RivalMood);
                SoulFeel.Note(em, a, day, SoulEventKind.RivalMade, affA, b);
            }
            if ((chB & SoulTune.ChangeBefriended) != 0)
            {
                SoulFeel.Pulse(em, b, SoulTune.FriendMood);
                SoulFeel.Note(em, b, day, SoulEventKind.Befriended, affB, a);
                formed = true;
            }
            else if ((chB & SoulTune.ChangeFellOut) != 0)
            {
                SoulFeel.Pulse(em, b, SoulTune.FalloutMood);
                SoulFeel.Note(em, b, day, SoulEventKind.FellOut, affB, a);
            }
            if ((chB & SoulTune.ChangeClose) != 0)
            {
                SoulFeel.Pulse(em, b, SoulTune.FirstCloseMood);
            }
            if ((chB & SoulTune.ChangeRival) != 0)
            {
                SoulFeel.Pulse(em, b, SoulTune.RivalMood);
                SoulFeel.Note(em, b, day, SoulEventKind.RivalMade, affB, a);
            }
            return formed;
        }

        private void FlushFriendships(EntityManager em, int formed)
        {
            try
            {
                Entity cfg = m_Stats.GetSingletonEntity();
                SoulCityStats st = em.GetComponentData<SoulCityStats>(cfg);
                st.m_FriendshipsFormed += (uint)formed;
                em.SetComponentData(cfg, st);
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("social.stats", "stats flush skipped: " + ex.Message, 60000);
            }
        }
    }

    /// <summary>
    /// Brawl lifecycle. Fights advance one round per day (deterministic; the
    /// lower-indexed side resolves the pair) and resolve after FightRounds.
    /// </summary>
    public partial class FightResolutionSystem : SoulSystemBase
    {
        private EntityQuery m_Souls;
        private EntityQuery m_Fighters;
        private EntityQuery m_Stats;

        protected override int Interval { get { return 4096; } }    // 64 passes/day
        protected override int Lanes { get { return SoulConsts.FightLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulPersonality>(),
                    ComponentType.ReadOnly<Mood>()
                }
            });
            m_Fighters = GetEntityQuery(ComponentType.ReadWrite<InFight>());
            m_Stats = GetEntityQuery(ComponentType.ReadWrite<SoulCityStats>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;
            int resolved = 0;
            int injured = 0;
            int faults = AdvanceFights(em, day, ref resolved, ref injured);
            faults += StartFights(em, day, lane, ref injured);
            if (resolved > 0 || injured > 0) FlushStats(em, resolved, injured);
            if (faults > 0) SoulLog.Warn("FightResolutionSystem: " + faults + " fault(s) this pass.");
        }

        private int AdvanceFights(EntityManager em, uint day, ref int resolved, ref int injured)
        {
            int faults = 0;
            NativeArray<Entity> fighters = m_Fighters.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < fighters.Length; i++)
                {
                    Entity e = fighters[i];
                    try
                    {
                        InFight f = em.GetComponentData<InFight>(e);
                        Entity o = f.m_Opponent;
                        if (!em.Exists(o) || !em.HasComponent<SoulTag>(o) || !em.IsComponentEnabled<InFight>(o))
                        {
                            ClearFight(em, e);
                            continue;
                        }
                        if (e.Index > o.Index) continue;                    // one side resolves the pair
                        InFight fo = em.GetComponentData<InFight>(o);
                        if (fo.m_Opponent != e) { ClearFight(em, e); continue; }

                        uint due = f.m_StartDay + (uint)f.m_Round + 1u;
                        if (day < due) continue;                            // fights advance day by day
                        f.m_Round++;
                        if (f.m_Round >= SoulConsts.FightRounds)
                        {
                            Resolve(em, e, o, day, ref injured);
                            resolved++;
                        }
                        else
                        {
                            em.SetComponentData(e, f);
                            fo.m_Round = f.m_Round;
                            em.SetComponentData(o, fo);
                        }
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("FightResolutionSystem advance first fault: " + ex.Message);
                    }
                }
            }
            finally { fighters.Dispose(); }
            return faults;
        }

        private int StartFights(EntityManager em, uint day, int lane, ref int injured)
        {
            int faults = 0;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    try
                    {
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (em.IsComponentEnabled<InFight>(e) || em.IsComponentEnabled<Injured>(e)) continue;
                        SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                        Mood m = em.GetComponentData<Mood>(e);
                        Unity.Mathematics.Random rng = SoulRng.For(p.m_SoulSeed, e, day * 7u + 3u);
                        int chance = SoulTune.FightChancePercent;
                        if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Hothead)) chance += SoulTune.HotheadFightChanceBonus;
                        if (m.m_Valence < SoulTune.FightMoodThreshold) chance *= 2;
                        if (rng.NextInt(0, 100) >= chance) continue;

                        DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
                        int best = -1;
                        int bestAff = 0;
                        for (int j = 0; j < edges.Length; j++)
                        {
                            SoulEdge edge = edges[j];
                            if ((edge.m_Flags & SoulEdgeFlags.Family) != 0) continue;
                            if (edge.m_Affinity >= 0) continue;
                            if (day < edge.m_LastDay + SoulConsts.FightCooldownDays) continue;
                            Entity o = edge.m_Other;
                            if (!em.Exists(o) || !em.HasComponent<SoulTag>(o)) continue;
                            if (em.IsComponentEnabled<InFight>(o) || em.IsComponentEnabled<Injured>(o)) continue;
                            if (best < 0 || edge.m_Affinity < bestAff) { best = j; bestAff = edge.m_Affinity; }
                        }
                        if (best < 0) continue;
                        Entity other = edges[best].m_Other;

                        InFight fa = default(InFight);
                        fa.m_Opponent = other;
                        fa.m_StartDay = day;
                        fa.m_Round = 0;
                        InFight fb = default(InFight);
                        fb.m_Opponent = e;
                        fb.m_StartDay = day;
                        fb.m_Round = 0;
                        em.SetComponentData(e, fa);
                        em.SetComponentEnabled<InFight>(e, true);
                        em.SetComponentData(other, fb);
                        em.SetComponentEnabled<InFight>(other, true);
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("FightResolutionSystem start first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
            return faults;
        }

        private static void Resolve(EntityManager em, Entity a, Entity b, uint day, ref int injured)
        {
            SoulPersonality pa = em.GetComponentData<SoulPersonality>(a);
            SoulPersonality pb = em.GetComponentData<SoulPersonality>(b);
            Mood ma = em.GetComponentData<Mood>(a);
            Mood mb = em.GetComponentData<Mood>(b);
            Skills sa = em.GetComponentData<Skills>(a);
            Skills sb = em.GetComponentData<Skills>(b);

            Unity.Mathematics.Random rngA = SoulRng.For(pa.m_SoulSeed, a, day * 31u + 11u);
            Unity.Mathematics.Random rngB = SoulRng.For(pb.m_SoulSeed, b, day * 31u + 11u);
            int scoreA = SoulRules.FightScore(sa.m_Fitness, pa.m_Quirk, ma.m_Valence, rngA.NextInt(0, 100));
            int scoreB = SoulRules.FightScore(sb.m_Fitness, pb.m_Quirk, mb.m_Valence, rngB.NextInt(0, 100));
            int outcome = SoulRules.FightOutcome(scoreA, scoreB, rngA.NextInt(0, 100));

            DynamicBuffer<SoulEdge> ea = em.GetBuffer<SoulEdge>(a);
            DynamicBuffer<SoulEdge> eb = em.GetBuffer<SoulEdge>(b);
            int ia = SoulEdgeUtil.Touch(ea, b, SoulTune.FightGrudge, day, (byte)0, false);
            int ib = SoulEdgeUtil.Touch(eb, a, SoulTune.FightGrudge, day, (byte)0, false);
            byte chA = SoulEdgeUtil.ApplyThresholds(ea, ia);
            byte chB = SoulEdgeUtil.ApplyThresholds(eb, ib);
            short affA = (short)ea[ia].m_Affinity;
            short affB = (short)eb[ib].m_Affinity;

            SoulFeel.Note(em, a, day, SoulEventKind.Fought, affA, b);
            SoulFeel.Note(em, b, day, SoulEventKind.Fought, affB, a);

            if (outcome != 0)
            {
                Entity winner = outcome == 1 ? a : b;
                Entity loser = outcome == 1 ? b : a;
                SoulFeel.Pulse(em, winner, SoulTune.FightWinMood);
                SoulFeel.Pulse(em, loser, SoulTune.FightLossMood);
                if (em.HasComponent<SoulMind>(loser))
                {
                    SoulMind mind = em.GetComponentData<SoulMind>(loser);
                    mind.m_Trauma = (byte)Math.Min(255, mind.m_Trauma + SoulTune.TraumatizedByViolence);
                    em.SetComponentData(loser, mind);
                }
                if (rngB.NextInt(0, 100) < SoulTune.InjuryChancePercent)
                {
                    Injure(em, loser, day, winner, 1 + rngB.NextInt(0, 2), ref injured);
                }
            }
            else
            {
                SoulFeel.Pulse(em, a, SoulTune.FightDrawMood);
                SoulFeel.Pulse(em, b, SoulTune.FightDrawMood);
            }

            if ((chA & SoulTune.ChangeRival) != 0) SoulFeel.Note(em, a, day, SoulEventKind.RivalMade, affA, b);
            if ((chB & SoulTune.ChangeRival) != 0) SoulFeel.Note(em, b, day, SoulEventKind.RivalMade, affB, a);

            ClearFight(em, a);
            ClearFight(em, b);
        }

        private static void Injure(EntityManager em, Entity victim, uint day, Entity source, int severity, ref int injured)
        {
            SoulPersonality p = em.GetComponentData<SoulPersonality>(victim);
            Unity.Mathematics.Random rng = SoulRng.For(p.m_SoulSeed, victim, day * 13u + 5u);
            uint recover = day + (uint)rng.NextInt(SoulConsts.MinRecoveryDays, SoulConsts.MaxRecoveryDays + 1);
            Injured inj = default(Injured);
            inj.m_RecoverDay = recover;
            inj.m_Severity = (byte)severity;
            inj.m_Source = source;
            em.SetComponentData(victim, inj);
            em.SetComponentEnabled<Injured>(victim, true);
            SoulFeel.Note(em, victim, day, SoulEventKind.Injured, (short)severity, source);
            injured++;
        }

        private static void ClearFight(EntityManager em, Entity e)
        {
            if (!em.HasComponent<SoulTag>(e)) return;
            em.SetComponentEnabled<InFight>(e, false);
            em.SetComponentData(e, default(InFight));
        }

        private void FlushStats(EntityManager em, int resolved, int injured)
        {
            try
            {
                Entity cfg = m_Stats.GetSingletonEntity();
                SoulCityStats st = em.GetComponentData<SoulCityStats>(cfg);
                st.m_FightsResolved += (uint)resolved;
                st.m_InjuriesTotal += (uint)injured;
                em.SetComponentData(cfg, st);
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("fight.stats", "stats flush skipped: " + ex.Message, 60000);
            }
        }
    }

    /// <summary>
    /// Healing and accidents. Injured citizens skip social life; severe knock
    /// abouts are rare and brief, but a miserable streak makes them likelier.
    /// </summary>
    public partial class InjurySystem : SoulSystemBase
    {
        private EntityQuery m_Souls;
        private EntityQuery m_Stats;

        protected override int Interval { get { return 4096; } }    // 64 passes/day
        protected override int Lanes { get { return SoulConsts.InjuredLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadOnly<SoulPersonality>(),
                    ComponentType.ReadOnly<Mood>()
                }
            });
            m_Stats = GetEntityQuery(ComponentType.ReadWrite<SoulCityStats>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int healed = 0;
            int accidents = 0;
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    try
                    {
                        if (!em.HasComponent<SoulMeta>(e)) continue;
                        if (em.IsComponentEnabled<Injured>(e))
                        {
                            Injured inj = em.GetComponentData<Injured>(e);
                            if (day >= inj.m_RecoverDay)
                            {
                                em.SetComponentEnabled<Injured>(e, false);
                                em.SetComponentData(e, default(Injured));
                                SoulFeel.Pulse(em, e, SoulTune.HealMoodBonus);
                                SoulFeel.Note(em, e, day, SoulEventKind.Healed, (short)inj.m_Severity, inj.m_Source);
                                healed++;
                            }
                        }
                        else
                        {
                            if (em.IsComponentEnabled<InFight>(e)) continue;
                            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                            Unity.Mathematics.Random rng = SoulRng.For(p.m_SoulSeed, e, day * 523u + 17u);
                            // vitals: exhaustion raises the risk, fitness offsets it;
                            // the floor of 1 per mille keeps accidents possible for the fit.
                            int perMille = SoulTune.AccidentChancePerMille;
                            if (em.HasComponent<SoulVitals>(e))
                            {
                                SoulVitals vit = em.GetComponentData<SoulVitals>(e);
                                ushort fit = em.GetComponentData<Skills>(e).m_Fitness;
                                perMille += SoulRules.AccidentPerMilleDelta(SoulRules.SkillLevel(fit), vit.m_Energy);
                                if (perMille < 1) perMille = 1;
                            }
                            if (rng.NextInt(0, 1000) < perMille)
                            {
                                Accident(em, e, day, rng, ref accidents);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("InjurySystem first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
            if (healed > 0 || accidents > 0) FlushStats(em, healed, accidents);
            if (faults > 0) SoulLog.Warn("InjurySystem: " + faults + " fault(s) this pass.");
        }

        private static void Accident(EntityManager em, Entity e, uint day, Unity.Mathematics.Random rng, ref int accidents)
        {
            uint recover = day + (uint)rng.NextInt(SoulConsts.MinRecoveryDays, SoulConsts.MaxRecoveryDays + 1);
            Injured inj = default(Injured);
            inj.m_RecoverDay = recover;
            inj.m_Severity = 1;
            inj.m_Source = Entity.Null;
            em.SetComponentData(e, inj);
            em.SetComponentEnabled<Injured>(e, true);
            SoulFeel.Note(em, e, day, SoulEventKind.Injured, 1, Entity.Null);
            accidents++;
        }

        private void FlushStats(EntityManager em, int healed, int accidents)
        {
            try
            {
                Entity cfg = m_Stats.GetSingletonEntity();
                SoulCityStats st = em.GetComponentData<SoulCityStats>(cfg);
                st.m_InjuriesTotal += (uint)accidents;
                st.m_HealsTotal += (uint)healed;
                em.SetComponentData(cfg, st);
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("injury.stats", "stats flush skipped: " + ex.Message, 60000);
            }
        }
    }

    /// <summary>
    /// Slow cooling of idle friendships (one affinity point per idle day beyond
    /// the flags-based delay) and pruning of links to citizens who are gone.
    /// Rivalries persist in this version by design.
    /// </summary>
    public partial class MemoryDecaySystem : SoulSystemBase
    {
        private EntityQuery m_Souls;

        protected override int Interval { get { return 4096; } }    // 64 passes/day
        protected override int Lanes { get { return SoulConsts.DecayLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoulTag>(),
                    ComponentType.ReadWrite<SoulEdge>()
                }
            });
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;
            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    try { Decay(em, e, day); }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("MemoryDecaySystem first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
            if (faults > 0) SoulLog.Warn("MemoryDecaySystem: " + faults + " fault(s) this pass.");
        }

        private static void Decay(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulMeta>(e)) return;
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            if (meta.m_MirroredMemories == day) return;    // once per citizen-day
            meta.m_MirroredMemories = (ushort)day;
            em.SetComponentData(e, meta);

            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = edges.Length - 1; i >= 0; i--)
            {
                if (!em.Exists(edges[i].m_Other)) edges.RemoveAt(i);
            }
            if (edges.Length == 0) return;
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge edge = edges[i];
                if (edge.m_Affinity <= 0) continue;        // rivalries persist this version
                uint delay = SoulRules.DecayDelay(edge.m_Flags);
                if (day <= edge.m_LastDay + delay) continue;
                int cooled = edge.m_Affinity - SoulTune.MaxDecayPerPass;
                if (cooled < 0) cooled = 0;
                edge.m_Affinity = SoulMath.ToSbyte(cooled);
                edges[i] = edge;
                byte ch = SoulEdgeUtil.ApplyThresholds(edges, i);
                if ((ch & SoulTune.ChangeFellOut) != 0)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.FellOut, (short)edges[i].m_Affinity, edges[i].m_Other);
                }
            }
        }
    }
}
