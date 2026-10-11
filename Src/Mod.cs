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
            SoulOptions.Load();
            SoulLog.Info("Citizen Soul 1.0.0 loading...");
            SoulLog.Info(SoulOptions.DescribeState());
            SoulLog.Info(SoulLocale.DescribeState());

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
            updateSystem.UpdateAfter<TravelSystem, TempoSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<MediaSystem, TravelSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<RitualsSystem, MediaSystem>(SystemUpdatePhase.ModificationEnd);

            // EP1: fate & appetite (F20, F28, F29, F34, F37)
            updateSystem.UpdateAfter<MortalitySystem, RitualsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<StrikeSystem, MortalitySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<GamblingSystem, StrikeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CrimeSystem, GamblingSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CorruptionSystem, CrimeSystem>(SystemUpdatePhase.ModificationEnd);

            // EP1: routines, food, faith (F39, F40, F41)
            updateSystem.UpdateAfter<RoutineSystem, CorruptionSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FoodSystem, RoutineSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FaithSystem, FoodSystem>(SystemUpdatePhase.ModificationEnd);

            // EP1: culture & collective life (F44-F46, F48-F51, F54, F59)
            updateSystem.UpdateAfter<NightlifeSystem, FaithSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FandomSystem, NightlifeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<FashionSystem, FandomSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<HolidaySystem, FashionSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<HoodSystem, HolidaySystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<GossipSystem, HoodSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ReputationSystem, GossipSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<MentorSystem, ReputationSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ChronicleSystem, MentorSystem>(SystemUpdatePhase.ModificationEnd);

            // EP2: money, markets & the wider life (F166-F183)
            updateSystem.UpdateAfter<FortuneSystem, ChronicleSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<VentureSystem, FortuneSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<PathSystem, VentureSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<ExchangeSystem, PathSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<BroadcastSystem, ExchangeSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<NewsroomSystem, BroadcastSystem>(SystemUpdatePhase.ModificationEnd);

            updateSystem.UpdateAt<SoulPanelUISystem>(SystemUpdatePhase.UIUpdate);

            SoulLog.Info("Citizen Soul loaded: 57 systems registered.");
        }

        public void OnDispose()
        {
            SoulLog.Info("Citizen Soul unloaded.");
        }
    }
}
