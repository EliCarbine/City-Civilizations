using Game;          // UpdateSystem, SystemUpdatePhase (verify using vs template)
using Game.Modding;  // IMod
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// Mod entry point. System registration follows the documented pattern:
    /// updateSystem.UpdateBefore/At/After&lt;T&gt;(SystemUpdatePhase.&lt;phase&gt;).
    /// </summary>
    public class Mod : IMod
    {
        public void OnLoad(UpdateSystem updateSystem)
        {
            // Init early: souls must exist before social systems see citizens.
            updateSystem.UpdateBefore<SoulInitializeSystem>(SystemUpdatePhase.Modification2);

            // Daily cadence systems (self-throttled internally to day slices).
            updateSystem.UpdateAt<DailyLearningSystem>(SystemUpdatePhase.Modification3);
            updateSystem.UpdateAt<SocializeSystem>(SystemUpdatePhase.Modification3);
            updateSystem.UpdateAt<FightResolutionSystem>(SystemUpdatePhase.Modification3);
            updateSystem.UpdateAt<InjurySystem>(SystemUpdatePhase.Modification3);

            // Mood bookkeeping late, after all emotion sources have run.
            updateSystem.UpdateAfter<EmotionSystem>(SystemUpdatePhase.Modification4);
            updateSystem.UpdateAfter<MemoryDecaySystem>(SystemUpdatePhase.Modification4);
        }

        public void OnDispose()
        {
        }
    }
}
