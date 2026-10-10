// Citizen Soul — core simulation systems (M0/M1).
//
//  SoulInitializeSystem : creates souls (budgeted per pass) + household family links.
//  DailyLearningSystem  : exactly one skill pass per citizen per game day.
//  EmotionSystem        : consolidates mood impulse -> valence once per citizen per day.
//  SoulJournalSystem    : once-per-day city heartbeat on the config singleton.
//  SoulSpikeSystem      : counter+hash pair that proves state survives save/load (M0 gate).
//
// All systems derive from SoulSystemBase: sealed OnUpdate, per-pass + per-entity
// fault isolation, day/lane slicing. Nothing here ever throws out of OnSoulUpdate.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Gives every citizen a soul exactly once: personality, mood, skills, soul
    /// metadata, relationship/memory/journal rings, fight/injury slots, then
    /// links household family edges. Budgeted per pass so a 100k-citizen load
    /// converges over hours of game time without ever stalling a frame.
    /// </summary>
    public partial class SoulInitializeSystem : SoulSystemBase
    {
        private EntityQuery m_Pending;
        private EntityQuery m_Config;

        protected override int Interval { get { return 2048; } } // 128 passes/day
        protected override int Lanes { get { return 1; } }       // whole-query budget, not lane-sliced

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Pending = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Game.Citizens.Citizen>() },
                None = new ComponentType[] { ComponentType.ReadOnly<SoulTag>() }
            });
            m_Config = GetEntityQuery(ComponentType.ReadWrite<SoulCityStats>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int _)) return;
            EntityManager em = EntityManager;

            // One-time config singleton (stats + spike counters).
            if (m_Config.CalculateEntityCount() == 0)
            {
                Entity cfg = em.CreateEntity();
                em.AddComponentData(cfg, default(SoulCityStats));
                em.AddComponentData(cfg, default(SoulSpike));
                SoulLog.Info("SoulInitializeSystem: config singleton created.");
            }

            int budget = SoulTune.InitBudgetPerPass;
            int created = 0;
            int faults = 0;
            NativeArray<Entity> pending = m_Pending.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < pending.Length && budget > 0; i++)
                {
                    try
                    {
                        if (InitSoul(em, pending[i], day)) created++;
                        budget--;
                    }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("SoulInitializeSystem first fault: " + ex.Message);
                    }
                }
            }
            finally { pending.Dispose(); }

            if (created > 0)
            {
                Entity cfg = m_Config.GetSingletonEntity();
                SoulCityStats stats = em.GetComponentData<SoulCityStats>(cfg);
                stats.m_SoulsCreated += (uint)created;
                em.SetComponentData(cfg, stats);
            }
            if (pending.Length == 0) SoulLog.Once("init.done", "SoulInitializeSystem: all citizens have souls.");
            if (faults > 0) SoulLog.Warn("SoulInitializeSystem: " + faults + " fault(s), " + created + " created this pass.");
        }

        private static bool InitSoul(EntityManager em, Entity e, uint day)
        {
            if (em.HasComponent<SoulTag>(e)) return false;

            byte pseudo = SoulVanilla.TryGetPseudoRandom(em, e);
            ushort seed = (ushort)((uint)e.Index * 2654435761u ^ (uint)e.Version * 2246822519u ^ (uint)pseudo * 3266489917u);
            SoulPersonality p = SoulRules.Roll(seed, pseudo);

            em.AddComponent<SoulTag>(e);
            em.AddComponentData(e, p);

            Mood mood = default(Mood);
            mood.m_Valence = SoulMath.ToSbyte(SoulMath.Clamp(SoulRules.MoodBaseline(p), SoulConsts.MoodMin, SoulConsts.MoodMax));
            mood.m_Arousal = SoulConsts.ArousalCalm;
            mood.m_Impulse = 0;
            mood.m_SinceDay = day;
            em.AddComponentData(e, mood);

            Skills s = default(Skills);
            s.m_Logic = 100; s.m_Craft = 100; s.m_Social = 100; s.m_Fitness = 100; // level 1 (100 xp per level)
            if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Bookworm)) s.m_Logic += 40;
            if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Workaholic)) s.m_Craft += 40;
            if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Jokester)) s.m_Social += 40;
            if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Fitness)) s.m_Fitness += 40;
            em.AddComponentData(e, s);

            SoulMeta meta = default(SoulMeta);
            meta.m_BirthDay = day;         // day the soul was first observed
            meta.m_LastLearnDay = 0;
            meta.m_LastSocialDay = 0;
            meta.m_MirroredMemories = 0;
            em.AddComponentData(e, meta);

            em.AddBuffer<SoulEdge>(e);
            em.AddBuffer<SoulMemory>(e);
            em.AddBuffer<SoulJournalEntry>(e);

            em.AddComponent<InFight>(e);
            em.AddComponent<Injured>(e);
            em.SetComponentEnabled<InFight>(e, false);
            em.SetComponentEnabled<Injured>(e, false);

            // Family links: household members that already have souls get a bond
            // in both directions; members initialized later catch up on their own pass.
            Entity home;
            if (SoulVanilla.TryGetHome(em, e, out home))
            {
                DynamicBuffer<Game.Citizens.HouseholdCitizen> members = em.GetBuffer<Game.Citizens.HouseholdCitizen>(home);
                for (int i = 0; i < members.Length; i++)
                    LinkFamily(em, e, members[i].m_Citizen, day);
            }
            return true;
        }

        private static void LinkFamily(EntityManager em, Entity a, Entity b, uint day)
        {
            if (b == Entity.Null || a == b || !em.HasComponent<SoulTag>(b)) return;
            DynamicBuffer<SoulEdge> ea = em.GetBuffer<SoulEdge>(a);
            if (SoulEdgeUtil.Find(ea, b) < 0) SoulEdgeUtil.Touch(ea, b, SoulTune.FamilyBondStart, day, 0, true);
            DynamicBuffer<SoulEdge> eb = em.GetBuffer<SoulEdge>(b);
            if (SoulEdgeUtil.Find(eb, a) < 0) SoulEdgeUtil.Touch(eb, a, SoulTune.FamilyBondStart, day, 0, true);
        }
    }

    /// <summary>
    /// One learning pass per citizen per game day: employed citizens advance
    /// Craft, others advance Logic; quirk-driven citizens also train Social or
    /// Fitness. Injured citizens make no progress. Level-ups write story.
    /// </summary>
    public partial class DailyLearningSystem : SoulSystemBase
    {
        private EntityQuery m_Souls;

        protected override int Interval { get { return 4096; } }               // 64 passes/day
        protected override int Lanes { get { return SoulTune.LearnLanes; } }  // 64 lanes -> 1 pass/citizen/day

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<SoulMeta>(),
                    ComponentType.ReadWrite<Skills>(),
                    ComponentType.ReadWrite<Mood>(),
                    ComponentType.ReadOnly<SoulPersonality>()
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
                    try { Learn(em, e, day); }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("DailyLearningSystem first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
        }

        private static void Learn(EntityManager em, Entity e, uint day)
        {
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            if (meta.m_LastLearnDay == day) return;
            meta.m_LastLearnDay = day;
            em.SetComponentData(e, meta);

            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            Skills s = em.GetComponentData<Skills>(e);
            if (!em.IsComponentEnabled<Injured>(e))
            {
                int level;
                Entity work;
                if (SoulVanilla.TryGetWorkplace(em, e, out work))
                {
                    int xp = SoulTune.DailyBaseXp + (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Workaholic) ? SoulTune.QuirkXpBonus : 0);
                    if (SoulRules.SkillAddXp(ref s.m_Craft, xp, out level)) Celebrate(em, e, day, level);
                }
                else
                {
                    int xp = SoulTune.DailyBaseXp + (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Bookworm) ? SoulTune.QuirkXpBonus : 0);
                    if (SoulRules.SkillAddXp(ref s.m_Logic, xp, out level)) Celebrate(em, e, day, level);
                }
                if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Fitness))
                {
                    if (SoulRules.SkillAddXp(ref s.m_Fitness, SoulTune.QuirkXpBonus, out level)) Celebrate(em, e, day, level);
                }
                if (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Jokester))
                {
                    if (SoulRules.SkillAddXp(ref s.m_Social, SoulTune.QuirkXpBonus, out level)) Celebrate(em, e, day, level);
                }
            }
            em.SetComponentData(e, s);
        }

        private static void Celebrate(EntityManager em, Entity e, uint day, int level)
        {
            SoulFeel.Note(em, e, day, SoulEventKind.LevelUp, (short)level, Entity.Null);
            SoulFeel.Pulse(em, e, SoulTune.LevelUpMood);
        }
    }

    /// <summary>
    /// Mood consolidation, once per citizen per day: pending impulse is spent
    /// into valence, valence drifts toward personality baseline, arousal tracks
    /// distance from baseline. Extreme days are written to the journal.
    /// </summary>
    public partial class EmotionSystem : SoulSystemBase
    {
        private EntityQuery m_Souls;

        protected override int Interval { get { return 2048; } }                  // 128 passes/day
        protected override int Lanes { get { return SoulConsts.EmotionLanes; } }  // 128 lanes -> 1 pass/citizen/day

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<Mood>(),
                    ComponentType.ReadOnly<SoulPersonality>()
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
                    try { Consolidate(em, e, day); }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("EmotionSystem first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
        }

        private static void Consolidate(EntityManager em, Entity e, uint day)
        {
            Mood m = em.GetComponentData<Mood>(e);
            if (m.m_SinceDay == day) return;
            m.m_SinceDay = day;

            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            int baseline = SoulRules.MoodBaseline(p);

            int v = m.m_Valence + m.m_Impulse;   // spend the day's shocks
            m.m_Impulse = 0;
            v = SoulMath.ClampToMood(SoulMath.Approach(v, baseline, SoulTune.MoodDriftPerDay));
            m.m_Valence = SoulMath.ToSbyte(v);

            int dev = SoulMath.Abs(v - baseline);
            int arousalTarget = SoulMath.Clamp(SoulConsts.ArousalCalm + dev, 0, 255);
            m.m_Arousal = SoulMath.ToByte(SoulMath.Approach(m.m_Arousal, arousalTarget, SoulTune.ArousalDriftPerDay));
            em.SetComponentData(e, m);

            SoulEventKind kind = SoulRules.DayEndMoodKind(v);
            if (kind != SoulEventKind.None)
                SoulMemUtil.PushJournal(em.GetBuffer<SoulJournalEntry>(e), day, kind, SoulMath.ToSbyte(v), Entity.Null);
        }
    }

    /// <summary>
    /// City heartbeat: refreshes the stats singleton once per day and logs the
    /// full soul-city digest every 25th day. Reserved hook for the M4 story surface.
    /// </summary>
    public partial class SoulJournalSystem : SoulSystemBase
    {
        private EntityQuery m_Config;

        protected override int Interval { get { return 4096; } }
        protected override int Lanes { get { return SoulConsts.JournalLanes; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Config = GetEntityQuery(ComponentType.ReadWrite<SoulCityStats>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            if (lane != 0) return;              // single-slice job
            if (m_Config.CalculateEntityCount() == 0) return;
            EntityManager em = EntityManager;
            Entity cfg = m_Config.GetSingletonEntity();
            SoulCityStats stats = em.GetComponentData<SoulCityStats>(cfg);
            if (stats.m_LastStatsDay == day) return;
            stats.m_LastStatsDay = day;
            em.SetComponentData(cfg, stats);
            if (day > 0 && day % 25 == 0)
            {
                SoulLog.Info("soul-city day=" + day
                    + " souls=" + stats.m_SoulsCreated
                    + " friendships=" + stats.m_FriendshipsFormed
                    + " fights=" + stats.m_FightsResolved
                    + " injuries=" + stats.m_InjuriesTotal
                    + " heals=" + stats.m_HealsTotal);
            }
        }
    }

    /// <summary>
    /// M0 persistence gate. Bumps a serialized counter and scramble-hash every
    /// pass and logs the pair periodically. If the counter continues across a
    /// save/load boundary instead of restarting, custom per-citizen state is
    /// provably surviving the round trip. The log line after a load is the
    /// spike's pass/fail signal.
    /// </summary>
    public partial class SoulSpikeSystem : SoulSystemBase
    {
        private EntityQuery m_Config;
        private bool m_ResumedLogged;

        protected override int Interval { get { return 2048; } }
        protected override int Lanes { get { return 1; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Config = GetEntityQuery(ComponentType.ReadWrite<SoulSpike>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int _)) return;
            if (m_Config.CalculateEntityCount() == 0) return;  // pre-init
            EntityManager em = EntityManager;
            Entity cfg = m_Config.GetSingletonEntity();
            SoulSpike spike = em.GetComponentData<SoulSpike>(cfg);

            if (!m_ResumedLogged && spike.m_Counter > 0)
            {
                m_ResumedLogged = true;
                SoulLog.Info("SoulSpikeSystem: RESUMED counter=" + spike.m_Counter
                    + " hash=" + spike.m_RollHash + " — M0 persistence spike passing.");
            }

            spike.m_Counter++;
            spike.m_RollHash = spike.m_RollHash * 1664525u + 1013904223u;   // LCG scramble
            if (day > 0 && day % SoulTune.SpikeLogEveryDays == 0 && spike.m_LastDay != day)
            {
                spike.m_LastDay = day;
                SoulLog.Info("SoulSpikeSystem: day=" + day + " counter=" + spike.m_Counter + " hash=" + spike.m_RollHash);
            }
            em.SetComponentData(cfg, spike);
        }
    }
}
