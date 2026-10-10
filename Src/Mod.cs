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
            updateSystem.UpdateAfter<VitalsSystem, SoulInitializeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<RomanceSystem, VitalsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<DramaSystem, RomanceSystem>(SystemUpdatePhase.ModificationEnd);

            // M3: mental weather, counseling, street crime, justice, companions (F11-F15, F17, F31-F35, F47)
            updateSystem.UpdateAfter<MindSystem, EmotionSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ChronicSystem, MindSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<TherapySystem, ChronicSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<StreetSystem, TherapySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<JusticeSystem, StreetSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<PetSystem, JusticeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<MemorialSystem, PetSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FamilySystem, MemorialSystem>(SystemUpdatePhase.ModificationEnd);

            // M5b: work & economy (F21-F25, F27, F30)
            updateSystem.UpdateAfter<WorkSystem, FamilySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ReentrySystem, WorkSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<MidlifeSystem, ReentrySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ArtsSystem, MidlifeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<SportsSystem, ArtsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CuriositySystem, SportsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<TechSystem, CuriositySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CivicSystem, TechSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<NatureSystem, CivicSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<QuirksSystem, NatureSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<TempoSystem, QuirksSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<SoulPanelUISystem>(SystemUpdatePhase.UIUpdate);

            SoulLog.Info("Citizen Soul loaded: 31 systems registered.");
        }

        public void OnDispose()
        {
            SoulLog.Info("Citizen Soul unloaded.");
        }
    }
}
