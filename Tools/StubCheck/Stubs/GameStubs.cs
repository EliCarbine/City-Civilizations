// Compile-check stubs: Game.* surface used by Citizen Soul.
// Mirrors the real CS2 game-assembly signatures; bodies are inert (never executed).

using Unity.Entities;

namespace Game
{
    public enum SystemUpdatePhase
    {
        Modification1 = 1,
        Modification2 = 2,
        Modification3 = 3,
        Modification4 = 4,
        Modification5 = 5,
        Modification6 = 6,
        Modification7 = 7,
        Modification8 = 8,
        Modification9 = 9,
        Modification10 = 10,
        Modification11 = 11,
        Modification12 = 12,
        Modification13 = 13,
        Modification14 = 14,
        Modification15 = 15,
        Modification16 = 16,
        ModificationEnd = 100
    }

    public abstract class GameSystemBase : SystemBase
    {
        /// <summary>Update interval in frames; 0 means every frame.</summary>
        protected virtual int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 0;
        }
    }

    public class UpdateSystem
    {
        public void UpdateAt<T>(SystemUpdatePhase phase) where T : ComponentSystemBase { }
        public void UpdateAt<T, TDep>(SystemUpdatePhase phase) where T : ComponentSystemBase where TDep : ComponentSystemBase { }
        public void UpdateBefore<T>(SystemUpdatePhase phase) where T : ComponentSystemBase { }
        public void UpdateBefore<T, TDep>(SystemUpdatePhase phase) where T : ComponentSystemBase where TDep : ComponentSystemBase { }
        public void UpdateAfter<T>(SystemUpdatePhase phase) where T : ComponentSystemBase { }
        public void UpdateAfter<T, TDep>(SystemUpdatePhase phase) where T : ComponentSystemBase where TDep : ComponentSystemBase { }
    }
}

namespace Game.Modding
{
    public interface IMod
    {
        void OnLoad(UpdateSystem updateSystem);
        void OnDispose();
    }
}

namespace Game.Simulation
{
    public class SimulationSystem : GameSystemBase
    {
        public uint frameIndex;
    }
}

namespace Game.Citizens
{
    public struct Citizen : IComponentData
    {
        public byte m_Health;
        public byte m_PseudoRandom;
    }

    public struct HouseholdMember : IComponentData
    {
        public Entity m_Household;
    }

    public struct Worker : IComponentData
    {
        public Entity m_Workplace;
    }

    public struct CurrentBuilding : IComponentData
    {
        public Entity m_CurrentBuilding;
    }

    public struct HouseholdCitizen : IBufferElementData
    {
        public Entity m_Citizen;
    }
}
