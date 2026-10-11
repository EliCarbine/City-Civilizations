# Citizen Soul — a living population for Cities: Skylines II

Every citizen gets a persistent personality, memory, relationships, moods, skills, and a
life story you can follow. Friends drift apart, kids grow up, couples form and break,
people get hurt, learn, grieve, and celebrate.

**v1.0.0 — 151 features across 51 systems (M0–M6 + EP1 City Life).**

## What it does
- **Personality** — each citizen gets a generated temperament that biases daily life
- **Social** — friendships, rivalries, fights, and memory of who did what to whom
- **Romance & family** — dating, couples, breakups, children, dynasties, memorials
- **Mind & mood** — moods, trauma, therapy, chronic conditions, addiction and recovery
- **Crime & justice** — street life, notoriety, convictions, reentry
- **Work, culture, sports, curiosity, civic life** — hobbies, arts, tech, nature
- **Companions** — pets with bonded names you can see in the inspector
- **Rituals & milestones** — first school day, coming of age, weddings, old age
- **Media** — city media, and a diary you can read from the follow panel

## Player tools
- **Follow panel / inspector** — pick any citizen; read mood, ties, skills, recent events
- **options.ini** (created next to the mod dll on first load) — master switch + 18
  feature groups; restart the game after editing
- **Localization** — built-in en-US; override any string via `lang/<culture>.json`
- **Perf report** — one log summary every 5 minutes; warns when a system exceeds its
  per-pass budget (see PERF.md)

## Repo layout
```
Src/                 mod source (51 systems, components, core logic)
lang/en-US.json      localization file that ships with the mod
Properties/          PublishConfiguration.xml + store Thumbnail.png
Tools/StubCheck/     offline compile-check harness (no game install needed)
Tools/make_thumbnail.py   regenerates the store thumbnail
DESIGN.md            architecture, persistence plan, roadmap
FEATURES.md          feature catalog part I (F01–F61)
FEATURES-2.md        feature catalog part II (F62–F151) + addenda
Citizen-Soul-Feature-List.md   consolidated catalog (all 151)
PERF.md              performance gate: measurement, acceptance, tuning
PUBLISHING.md        Paradox Mods release checklist
```

## Build
- **Compile check (macOS/Linux, no game needed):**
  `dotnet build Tools/StubCheck/CitizenSoul.StubCheck.csproj`
- **Full build (machine with the game + CS2 Modding Toolchain):**
  `dotnet build CitizenSoul.csproj -c Release -p:GameDir="<install dir>"`
  (ships `lang/` next to the dll per the csproj copy rule)
- **Deploy:** toolchain local mods folder; the game creates `options.ini` there on
  first load.
- **Publish:** see PUBLISHING.md.

## Status
- **M0–M6 complete** — M6 Ship added the Paradox Mods publish config, options.ini,
  localization, and the perf gate.
- **EP1 City Life complete** — 17 systems: mortality, strikes, gambling, organized crime,
  corruption, routines, food, faith, nightlife, fandoms, fashion, holidays, neighborhood
  character, gossip, reputation, mentorship, and the city chronicle.
