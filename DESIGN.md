# Citizen Soul — Cities: Skylines II mod
**Design & build spec, v0.3 — 2026-10-10**

Give every citizen a persistent soul: personality, emotions, skills that grow, friendships,
rivalries, injuries, fights, and a life story — evolved daily by the sim and saved with the city.

**Feature catalog: FEATURES.md + FEATURES-2.md — the full roster: 151 features across 17 themes, all
emergent from six shared engines, mapped to milestones.**

---

## 1. Feasibility verdict

**Yes — and the game was practically built for it.** CS2 ships a full C# modding API, the
simulation is Unity DOTS ECS (every citizen is an entity with components), the save system
persists per-entity components via a documented serialization interface, and a native
"followed citizen" + Lifepath Journal concept already exists to hang UI on. Verified anchors:

- **Mod entry + system registration**: `class Mod : IMod` → `OnLoad(UpdateSystem)` →
  `updateSystem.UpdateBefore/At/After<T>(SystemUpdatePhase.ModificationN)` (official wiki).
- **Lifecycle hooks**: `GameSystemBase` overrides `OnGamePreload(Purpose, GameMode)`,
  `OnGameLoaded(Context)`, `OnGameLoadingComplete(Purpose, GameMode)` — new-game vs load-time init.
- **Per-citizen components already serialize**: `Citizen`, `Student`, `Followed`, `CrimeVictim`,
  `SchoolSeekerCooldown`, `CommuterHousehold`, `Leisure`, etc. all implement
  `Colossal.Serialization.Entities.ISerializable` with version-aware `Deserialize` guarded by
  `reader.context.version >= Version.<feature>`.
- **Entity references persist** (e.g. `Student.m_School`), so friendship edges can be Entity refs.
- **Enableable components** (`IEnableableComponent`, e.g. `CrimeVictim`) — clean pattern for
  on/off states like `Injured` / `InFight`.
- **Vanilla already does personality-adjacent sim**: `WellBeing`/`Health` bytes on Citizen,
  `LookForPartnerSystem` (partner-seeking, probabilistic, per-citizen pseudo-random),
  `InitializeCitizenJob` (new-citizen seeding), Lifepath Journal, Chirper.
- **Perf pattern is proven in-game**: `LookForPartnerSystem` runs **kUpdatesPerDay = 4 slices**
  with Burst jobs; `UpdateGroupSystem` maintains `UpdateFrame` stride slots for entities. Our
  systems copy this discipline so 100k+ citizens stay cheap.

## 2. Toolchain

- **Official**: CS2 Modding Toolchain + project template (cs2.paradoxwikis.com/Modding_Toolchain),
  C#, Unity DOTS, publish to Paradox Mods. Officially Windows-centric.
- **Mac dev box: viable.** CS2 runs on Apple Silicon under **Wine 11 + DXMT** with working
  in-game Paradox Mods downloads (verified community report). Build loop: compile with native
  `dotnet` against game assemblies copied out of the Wine install; run the game under Wine to
  test; set `--developerMode`/log file for debug output. Final publish QA on a Windows VM is the
  boring-safe route if anything toolchain-side misbehaves under Wine.

## 3. Architecture

### 3.1 Custom components (all `ISerializable`, all version-guarded with our own header byte)

| Component | Shape | Purpose |
|---|---|---|
| `SoulTag` | marker | presence = soul initialized |
| `Personality` | 5 bytes (O/C/E/A/N) + `uint Quirks` + `ushort VoiceSeed` | unique character; seeded from `Citizen.m_PseudoRandom` |
| `Skills` | 6 × `ushort` (Logic, Charisma, Fitness, Craft, Arts, Cooking) | grow daily |
| `Mood` | `sbyte Valence`, `byte Arousal`, `byte Dominant` | current emotion state |
| `SoulAffector` (buffer) | `byte Kind`, `sbyte Intensity`, `uint ExpiresDay` | short-term emotional events, decay over days |
| `SoulEdge` (buffer, cap 32) | `Entity Other`, `sbyte Affinity`, `byte Flags`, `uint LastInteractionDay`, `ushort SharedMoments` | friendships/rivalries/romance/family |
| `Injured` (enableable) | `byte Severity`, `byte Cause`, `uint SinceDay` | hurts: accidents, fights, illness |
| `InFight` (enableable) | `Entity Opponent`, `uint StartedDay`, `byte Round` | live fight state |
| `SoulMemory` (buffer, cap 64 ring) | `uint Day`, `ushort Kind`, `Entity Subject`, `short Value` | life story; feeds journal + UI |
| `SoulConfig` (singleton) | tuning knobs | rates, caps, toggles |

*The expanded catalog (FEATURES.md + FEATURES-2.md) adds more components — reputation, fame, trauma, habits,
routines, aspirations, faith, talents, created works — plus city-level singletons (gossip
network, chronicle feed, economy). Shapes pinned per feature during its milestone.*

### 3.2 Systems

- **SoulInitializeSystem** — after load (`OnGameLoadingComplete`) + catch-all query for
  `Citizen` entities lacking `SoulTag` (covers new spawns and pre-mod saves). Seeds
  personality deterministically from the citizen's pseudo-random seed.
- **DailyLearningSystem** — once per in-game day, per citizen (UpdateFrame-strided): school
  age → skill XP from education; adults → job/park/library/hobby rolls modified by traits.
- **SocializeSystem** — citizens co-located (same building/location) roll interactions when
  they have an edge or a chance to form one: affinity deltas from traits + mood; logs memories;
  can create friendships, romance, or friction.
- **FightResolutionSystem** — friction + low affinity + hothead quirks → `InFight`; rounds of
  contested rolls (Fitness/Charisma/traits); loser wounded (`Injured`), grudge added, bystander
  mood ripples; may hook vanilla crime/police via `CrimeVictim` pattern.
- **InjurySystem** — severity → movement/work penalties, hospital target, recovery over days,
  health coupling with vanilla `m_Health`; rare death at high severity.
- **EmotionSystem** — decays affectors, recomputes mood, emits dominant emotion for UI/Chirper.
- **MemoryDecaySystem** — edges fade without contact ("drifted apart"); strong ones persist.
- **SoulPanelUISystem** (M4) — inspect a followed citizen's soul: traits, mood, skills,
  relationship graph, life journal. Lives next to the vanilla Lifepath/Followed section.

### 3.3 Persistence — the make-or-break, solved by spike

Everything above rides the save **if** mod-added components are auto-serialized like vanilla
ones. **M0 spike decides this in half a day**: add `SoulSpike` (single counter) to citizens,
save, reload, assert value survived. Fallbacks if not: (a) mod-owned singleton arrays serialized
via the mod's own save participation; (b) JSON sidecar keyed to city GUID under the user data
dir. Design assumes (a)-preferred, spike-gated.

## 4. Performance budget

- Clone the in-game pattern: **day-slice everything** (kUpdatesPerDay-style) + `UpdateFrame`
  strides; Burst + `IJobChunk` for scans; no per-frame loops over all citizens.
- Cap buffers (edges ≤ 32, memory ≤ 64). Relationship ops are symmetric writes on two entities —
  batch per interaction, never global.
- LOD: full soul sim for followed/visible/recently-active citizens (thousands); batched
  statistical sim for the background population.
- Target: <5% frame-time overhead at 250k citizens.

## 5. Roadmap

Feature IDs (F01–F151) refer to **FEATURES.md** + **FEATURES-2.md**.

- **M0 — Persistence spike** (half day): save/load of custom component proved. Go/no-go.
- **M1 — Soul core**: personality, mood, daily learning, config. Observable: differing behaviors.
  (+ F16 fitness, F18 sleep, F19 meditation — cheap personal-state features.)
- **M2 — Relations**: friendships, romance, friction, decay. Observable: social circles.
  (+ F01–F04 romance arc, F08, F26, F42, F52, F53, F55.)
- **M3 — Damage**: injuries, fights, hospital, crime hooks. Observable: bar fights, grudges.
  (+ F11–F15, F17, F31–F33, F35, F47.)
- **M4 — Story**: memory journal + soul inspector UI panel. (+ F43, F58, F61.)
- **M5 — Depth**: aging/dynasties, education integration, perf tuning at 250k.
  (+ F05–F07, F09, F10, F21–F25, F27, F30, F36, F38, F56, F57, F60.)
- **M6 — Ship**: Paradox Mods publish, options, localization.
- **EP1 — City Life** (post-ship): city-wide media, culture, deep crime.
  (+ F20, F28, F29, F34, F37, F39–F41, F44–F46, F48–F51, F54, F59.)

## 6. Risks

- Save-bloat: capped buffers + ring logs; measure save size delta at M1.
- Game patches changing internals: version-guarded deserialize + defensive queries (guide docs).
- Mod conflicts: unique namespace/component names; no vanilla component mutation beyond toggling.
- Perf cliff at big cities: M5 gate — if the slice budget isn't met, background sim degrades to
  statistical mode by config.

## 7. Sources

- cs2.paradoxwikis.com/Modding_Toolchain — official toolchain
- cs2.paradoxwikis.com/Systems — IMod registration, GameSystemBase lifecycle
- ps1ke.github.io/Cities-Skylines-2-Modding-Guide — reverse-engineered internals:
  /Game/Citizens/Citizen, /Followed, /CrimeVictim, /Student, /SchoolSeekerCooldown,
  /CommuterHousehold, /Leisure + system docs (LookForPartnerSystem, UpdateGroupSystem,
  InitializeCitizenJob)
- paradoxinteractive.com — Modding Dev Diary #3: Code Modding (MacSergey)
- Community report: CS2 on Apple Silicon via Wine 11 + DXMT, in-game mod downloads working
