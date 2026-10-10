# Citizen Soul (working title) — CS2 mod scaffold

```
DESIGN.md          ← full spec: architecture, roadmap, persistence plan
FEATURES.md        ← 61-feature catalog: the full "real human" roster + engine mapping
FEATURES-2.md      ← Part II: 90 more (F62–F151) + addenda
CitizenSoul.csproj ← project skeleton (verify TFM + reference paths vs official template)
Src/Mod.cs         ← IMod entry; registers all systems
Src/Components/    ← soul data structs (ISerializable patterns from verified game code)
Src/Systems/       ← sim systems (init, learning, social, fights, emotion)
```

## Status: scaffold
Source structure + API-correct skeletons based on documented patterns (ps1ke guide, official
wiki). It is **not compiled yet** — it needs the official CS2 modding toolchain, which
provides the game assembly references.

## Build (once toolchain is installed)
1. Install CS2 Modding Toolchain (cs2.paradoxwikis.com/Modding_Toolchain) + project template.
2. Compare this csproj against the template's — align TFM + assembly reference paths.
3. `dotnet build` → deploy DLL to the local mods folder per toolchain docs.
4. Launch CS2, enable mod, load/create city.

## M0 spike (the go/no-go experiment)
`SoulSpike` component (see Src/Components/SoulComponents.cs) is a one-counter probe:
1. Build + run with the spike system enabled (it bumps the counter daily).
2. Save the city, fully exit, reload.
3. Log should show the counter continuing — proving mod components ride the save.
4. If it resets to 0: switch persistence to the fallback plan (DESIGN.md §3.3).

## Mac dev note
Game runs on Apple Silicon via Wine 11 + DXMT (verified report, in-game mod downloads work).
Compile natively with dotnet against assemblies copied from the Wine install; test in the Wine
game. Windows VM for publish QA if needed.
