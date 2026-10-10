// Citizen Soul — M1 personal-vitals lane: F18 sleep, F19 meditation, F16 fitness.
//
// One full pass per citizen per game day (128 lanes × 2048-tick interval),
// mirroring EmotionSystem's cadence. Decision math lives in SoulRules/SoulTune
// so it stays pure and unit-testable; this file only does entity plumbing.
//
// Story discipline: the journal is a 64-slot lifetime ring, so only edges make
// it — exceptionally good or rough nights, zen crossings, meditative healing,
// and fitness level-ups (through the shared Celebrate-style pulse). Ordinary
// days leave no trace, exactly like ordinary social days.

using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Daily vitals lane: energy restores with sleep (quality shaped by quirks,
    /// injuries, stress), calm builds with meditation and decays toward baseline,
    /// fitness trains when rested and healthy. Chronic exhaustion and stress bleed
    /// daily pulses into mood; a calm mind mends injuries faster. Upgraded saves
    /// (souls created before this lane existed) are migrated in budgeted batches
    /// on the first passes after load.
    /// </summary>
    public partial class VitalsSystem : SoulSystemBase
    {
        private EntityQuery m_Souls;
        private EntityQuery m_Unmigrated;

        protected override int Interval { get { return 2048; } }                  // 128 passes/day
        protected override int Lanes { get { return SoulConsts.VitalsLanes; } }  // 128 lanes -> 1 pass/citizen/day

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Souls = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<SoulVitals>(),
                    ComponentType.ReadOnly<SoulPersonality>(),
                    ComponentType.ReadOnly<SoulTag>()
                }
            });
            // Souls from pre-M1 saves: SoulTag present, vitals lane not yet attached.
            m_Unmigrated = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<SoulTag>() },
                None = new ComponentType[] { ComponentType.ReadOnly<SoulVitals>() }
            });
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            // Upgrade path: attach the lane to older souls in budgeted batches.
            NativeArray<Entity> pending = m_Unmigrated.ToEntityArray(Allocator.Temp);
            try
            {
                int budget = SoulTune.InitBudgetPerPass;
                for (int i = 0; i < pending.Length && budget > 0; i++, budget--)
                {
                    SoulVitals fresh = default(SoulVitals);
                    fresh.m_Energy = (byte)SoulTune.VitStartEnergy;
                    fresh.m_Calm = (byte)SoulTune.VitStartCalm;
                    fresh.m_SleepQuality = (byte)SoulTune.SleepQualityBase;
                    fresh.m_LastDay = day;
                    em.AddComponentData(pending[i], fresh);
                }
            }
            finally { pending.Dispose(); }

            NativeArray<Entity> souls = m_Souls.ToEntityArray(Allocator.Temp);
            int faults = 0;
            try
            {
                for (int i = 0; i < souls.Length; i++)
                {
                    Entity e = souls[i];
                    if (!Matches(e, lane, Lanes)) continue;
                    try { Advance(em, e, day); }
                    catch (Exception ex)
                    {
                        faults++;
                        if (faults == 1) SoulLog.Warn("VitalsSystem first fault: " + ex.Message);
                    }
                }
            }
            finally { souls.Dispose(); }
            if (faults > 0) SoulLog.Warn("VitalsSystem: " + faults + " fault(s) this pass.");
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            SoulVitals v = em.GetComponentData<SoulVitals>(e);
            if (v.m_LastDay == day) return;   // one pass per citizen per day
            v.m_LastDay = day;

            SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
            bool injured = em.IsComponentEnabled<Injured>(e);
            int energy = v.m_Energy;
            int calm = v.m_Calm;

            // --- F18 sleep: quality roll shaped by quirks, injury, stress ---
            Unity.Mathematics.Random rng = SoulRng.For(p.m_SoulSeed, e, day * 293u + 41u);
            int quality = SoulRules.SleepQuality(rng.NextInt(-SoulTune.SleepQualitySpread, SoulTune.SleepQualitySpread + 1), p, injured, calm);
            v.m_SleepQuality = SoulMath.ToByte(quality);
            energy = SoulMath.Clamp(energy + SoulRules.SleepRestore(quality) - SoulTune.EnergyDrainPerDay, 0, 255);

            // --- F19 meditation: builds calm; a calm mind mends the body faster ---
            if (rng.NextInt(0, 1000) < SoulRules.MeditateChancePerMille(p))
            {
                int before = calm;
                calm = SoulMath.Clamp(calm + SoulTune.MeditateCalmGain, 0, 255);
                SoulFeel.Pulse(em, e, SoulTune.MeditateMood);
                if (injured && calm >= SoulTune.MeditateHealMinCalm)
                {
                    Injured inj = em.GetComponentData<Injured>(e);
                    if (inj.m_RecoverDay > day + 1u)
                    {
                        inj.m_RecoverDay -= 1u;   // patience heals the body early
                        em.SetComponentData(e, inj);
                        SoulFeel.Note(em, e, day, SoulEventKind.Meditated, (short)calm, Entity.Null);
                    }
                }
                else if (before < SoulTune.ZenCalm && calm >= SoulTune.ZenCalm)
                {
                    SoulFeel.Note(em, e, day, SoulEventKind.Meditated, (short)calm, Entity.Null);
                    SoulFeel.Pulse(em, e, SoulTune.ZenMood);
                }
            }
            else
            {
                calm = SoulMath.Approach(calm, SoulTune.CalmBaseline, SoulTune.CalmDecayPerDay);
            }

            // --- F16 fitness: train when rested and healthy; level-ups celebrate ---
            if (energy >= SoulTune.TrainMinEnergy && !injured)
            {
                int chance = SoulTune.TrainChancePerMille
                    + (SoulQuirks.Has(p.m_Quirk, SoulQuirks.Fitness) ? SoulTune.FitnessQuirkTrainBonus : 0);
                if (rng.NextInt(0, 1000) < chance)
                {
                    Skills s = em.GetComponentData<Skills>(e);
                    int level;
                    if (SoulRules.SkillAddXp(ref s.m_Fitness, SoulTune.TrainXp, out level))
                    {
                        SoulFeel.Note(em, e, day, SoulEventKind.LevelUp, (short)level, Entity.Null);
                        SoulFeel.Pulse(em, e, SoulTune.LevelUpMood);
                    }
                    em.SetComponentData(e, s);
                    energy = SoulMath.Clamp(energy - SoulTune.TrainEnergyCost, 0, 255);
                }
            }

            // --- chronic states bleed into mood until sleep/practice fixes them ---
            if (energy < SoulTune.EnergyWeary) SoulFeel.Pulse(em, e, SoulTune.WearyMood);
            if (calm < SoulTune.StressCalm) SoulFeel.Pulse(em, e, SoulTune.StressedMood);

            // --- story: only exceptional nights reach the journal ring ---
            if (quality >= SoulTune.SleepWellQuality)
                SoulFeel.Note(em, e, day, SoulEventKind.SleptWell, (short)quality, Entity.Null);
            else if (quality <= SoulTune.SleepBadQuality)
                SoulFeel.Note(em, e, day, SoulEventKind.SleptBad, (short)quality, Entity.Null);

            v.m_Energy = SoulMath.ToByte(energy);
            v.m_Calm = SoulMath.ToByte(calm);
            em.SetComponentData(e, v);
        }
    }
}
