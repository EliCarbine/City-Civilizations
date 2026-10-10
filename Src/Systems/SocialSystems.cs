using Game;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Social interactions between co-located citizens. When two citizens are at the same
    /// location, roll an interaction seeded by both personalities + current moods:
    ///   - no edge yet: chance to form acquaintance (extraversion-weighted)
    ///   - existing edge: affinity drift up/down; SharedMoments++
    ///   - very low affinity + hothead: pass tension to FightResolutionSystem
    ///
    /// TODO(M2): real co-location source. Candidates from documented internals:
    ///   - same CurrentBuilding / location entities
    ///   - vanilla Leisure.m_TargetAgent style target tracking
    /// Keep the pair scan day-sliced (kUpdatesPerDay pattern) — never per-frame.
    /// </summary>
    public partial class SocializeSystem : GameSystemBase
    {
        private EntityQuery _soulQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _soulQuery = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadWrite<SoulEdge>(),
                ComponentType.ReadWrite<Mood>());
        }

        protected override void OnUpdate()
        {
            // Skeleton: no-op until co-location source lands in M2.
        }
    }

    /// <summary>
    /// Fights: citizens with InFight enabled trade rounds of contested rolls.
    /// Outcomes: wounds (Injured), affinity crater + rival flag, bystander mood shock,
    /// optional vanilla crime hook (CrimeVictim pattern) so police can respond.
    /// </summary>
    public partial class FightResolutionSystem : GameSystemBase
    {
        private EntityQuery _fightQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _fightQuery = GetEntityQuery(
                ComponentType.ReadWrite<InFight>(),
                ComponentType.ReadWrite<Injured>(),
                ComponentType.ReadOnly<SoulTag>());
        }

        protected override void OnUpdate()
        {
            // Skeleton: resolve round:
            //   score = Fitness/100 + quirk(fit) + rng VS opponent's same
            //   loser: enable Injured(severity 1-2), disable InFight both
            //   apply affinity hit (-20..-40), set rival flag, log SoulMemory(Kind=fought)
        }
    }

    /// <summary>
    /// Injuries: apply movement/work penalty while enabled, track recovery, couple into
    /// vanilla m_Health. Severe cases: seek hospital, rare death.
    /// </summary>
    public partial class InjurySystem : GameSystemBase
    {
        private EntityQuery _injuredQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _injuredQuery = GetEntityQuery(
                ComponentType.ReadWrite<Injured>(),
                ComponentType.ReadWrite<Game.Citizens.Citizen>());
        }

        protected override void OnUpdate()
        {
            // Skeleton: per injury, decay severity over days; at 0 → disable Injured,
            // log SoulMemory(healed); while active, degrade Citizen.m_Health slowly.
        }
    }
}
