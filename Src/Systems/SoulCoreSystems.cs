using Game;
using Unity.Entities;
using Unity.Jobs;

namespace CitizenSoul
{
    /// <summary>
    /// Ensures every citizen has a soul. Runs after load (covers pre-mod saves) and
    /// continuously catches newly spawned citizens via a query for Citizen without SoulTag.
    /// Mirrors vanilla InitializeCitizenJob's seeding approach: deterministic from
    /// Citizen.m_PseudoRandom so a citizen without saved soul data rolls the same soul twice.
    /// </summary>
    public partial class SoulInitializeSystem : GameSystemBase
    {
        private EntityQuery _unsouledQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _unsouledQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Citizens.Citizen>(),
                ComponentType.Exclude<SoulTag>());
        }

        // Called when a save finishes loading — first sweep over loaded citizens.
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game) // gameplay save, not editor/menu
            {
                InitializeAll();
            }
        }

        protected override void OnUpdate()
        {
            if (!_unsouledQuery.IsEmpty)
            {
                InitializeAll();
            }
        }

        private void InitializeAll()
        {
            var em = EntityManager;
            using var entities = _unsouledQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in entities)
            {
                var citizen = em.GetComponentData<Game.Citizens.Citizen>(e);
                var rng = new Unity.Mathematics.Random(
                    CitizenSoul.Rng.SeedFrom(citizen.m_PseudoRandom, e));

                em.AddComponent<SoulTag>(e);
                em.AddComponentData(e, CitizenSoul.Rng.RollPersonality(ref rng));
                em.AddComponentData(e, new Skills());
                em.AddComponentData(e, new Mood { Valence = 0, Arousal = 128, Dominant = 0 });
                em.AddComponent<Injured>(e);   // disabled by default
                em.AddComponent<InFight>(e);    // disabled by default
                em.SetComponentEnabled<Injured>(e, false);
                em.SetComponentEnabled<InFight>(e, false);
                em.AddBuffer<SoulEdge>(e);
                em.AddBuffer<SoulMemory>(e);
                em.AddBuffer<SoulAffector>(e);
            }
        }
    }

    /// <summary>Deterministic rolls so unsaved souls are reproducible.</summary>
    internal static class Rng
    {
        public static uint SeedFrom(ushort pseudoRandom, Entity e)
            => (uint)(pseudoRandom * 2654435761u) ^ (uint)e.Index;

        public static Personality RollPersonality(ref Unity.Mathematics.Random rng)
        {
            return new Personality
            {
                Openness = (byte)rng.NextInt(40, 216),
                Conscientiousness = (byte)rng.NextInt(40, 216),
                Extraversion = (byte)rng.NextInt(40, 216),
                Agreeableness = (byte)rng.NextInt(40, 216),
                Neuroticism = (byte)rng.NextInt(40, 216),
                Quirks = rng.NextUInt() & 0x3Fu,
                VoiceSeed = (ushort)rng.NextUInt(0, 65535),
            };
        }
    }

    /// <summary>
    /// Daily learning: once per in-game day slice, give XP based on what the citizen
    /// is doing (school age → education; adults → job/hobby). Slice count mirrors the
    /// vanilla kUpdatesPerDay=4 pattern to keep per-frame cost flat.
    ///
    /// TODO(M1): replace the skeleton increment with real sources:
    ///   - school: query Student + education level (vanilla components)
    ///   - job:    query Worker/occupation
    ///   - hobby:  probabilistic by personality + leisure (Leisure component)
    /// </summary>
    public partial class DailyLearningSystem : GameSystemBase
    {
        private uint _lastDay;
        private EntityQuery _soulQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _soulQuery = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadWrite<Skills>());
        }

        protected override void OnUpdate()
        {
            // TODO(M1): slice by day & UpdateFrame stride instead of one pass:
            // process ~1/SoulConst.UpdatesPerDay of matching chunks per slice tick.
            var day = (uint)(UnityEngine.Time.frameCount / 60); // placeholder clock
            if (day == _lastDay) return;
            _lastDay = day;

            using var entities = _soulQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var em = EntityManager;
            foreach (var e in entities)
            {
                var skills = em.GetComponentData<Skills>(e);
                // Skeleton: logic XP from "studying" — real sources in M1.
                skills.Logic = (ushort)Unity.Mathematics.math.min(1000, skills.Logic + 1);
                em.SetComponentData(e, skills);
            }
        }
    }

    /// <summary>
    /// Mood bookkeeping: decay affectors, recompute valence/dominant emotion.
    /// Cheap pass — runs on a stride, not every frame.
    /// </summary>
    public partial class EmotionSystem : GameSystemBase
    {
        private EntityQuery _soulQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _soulQuery = GetEntityQuery(
                ComponentType.ReadWrite<Mood>(),
                ComponentType.ReadWrite<SoulAffector>());
        }

        protected override void OnUpdate()
        {
            // TODO(M1): stride via UpdateFrame; for now, cheap decay pass.
            var em = EntityManager;
            using var entities = _soulQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in entities)
            {
                var mood = em.GetComponentData<Mood>(e);
                mood.Valence = (sbyte)(mood.Valence * 0.99f); // gentle return to baseline
                mood.Arousal = (byte)Unity.Mathematics.math.max(64, mood.Arousal - 1);
                em.SetComponentData(e, mood);
            }
        }
    }

    /// <summary>
    /// Relationships fade without contact. Strong ties decay slower; flags set/cleared.
    /// </summary>
    public partial class MemoryDecaySystem : GameSystemBase
    {
        private EntityQuery _edgeQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _edgeQuery = GetEntityQuery(ComponentType.ReadWrite<SoulEdge>());
        }

        protected override void OnUpdate()
        {
            // TODO(M2): decay affinity toward 0 when LastInteractionDay is old;
            // clear "close" flag when affinity drops; keep family permanent.
        }
    }
}
