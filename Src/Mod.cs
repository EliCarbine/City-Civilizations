// Citizen Soul — mod entry point.
// Registers the full system roster with the game's update loop.
// Ordering: initialization first; everything else after it. Fights resolve
// after socials so same-day grudges ripen on later ticks, and decay runs last
// so a day's touches are never eaten by idle cooling in the same pass.

using Game;
using Game.Modding;

namespace CitizenSoul
{
    public class Mod : IMod
    {
        public void OnLoad(UpdateSystem updateSystem)
        {
            SoulLog.Info("Citizen Soul loading (M0/M1 system roster)...");

            updateSystem.UpdateBefore<SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);

            updateSystem.UpdateAfter<SoulSpikeSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<DailyLearningSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<EmotionSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<SoulJournalSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<SocializeSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FightResolutionSystem, SocializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<MemoryDecaySystem, SocializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<InjurySystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);

            SoulLog.Info("Citizen Soul loaded: 9 systems registered.");
        }

        public void OnDispose()
        {
            SoulLog.Info("Citizen Soul unloaded.");
        }
    }
}
