# Publishing Citizen Soul to Paradox Mods (M6)

## One-time setup
- CS2 Modding Toolchain installed alongside the game — provides the real game DLL
  references and `ModPublisher.exe`.
- Paradox account linked in-game (Paradox Mods) with a verified email.
  You cannot publish until that link exists.

## Ship artifacts (committed)
- `Properties/PublishConfiguration.xml` — store page + update metadata. Shape follows
  the community standard (MapImageLayer / InfoLoom):
  `<ModId Value="" />`, `DisplayName`, `ShortDescription`, `LongDescription` (markdown
  subset), `Thumbnail`, repeatable `Tag`, `ModVersion`, `GameVersion`, `ChangeLog`,
  `ExternalLink` (types: discord, github, youtube, twitch, x, paypal, patreon).
- `Properties/Thumbnail.png` — 512×512 store thumbnail. Regenerate with
  `python3 Tools/make_thumbnail.py` (PIL).
- `lang/en-US.json` — localization. **Ships inside the mod**: must sit in a `lang/`
  folder next to `CitizenSoul.dll` (SoulLocale reads `lang/<culture>.json`).
- `options.ini` — **not shipped**. The game creates it next to the dll on first load.

## Build for publish
1. On a machine with the game install (Windows, or the Wine prefix on Apple Silicon):
   ```
   dotnet build CitizenSoul.csproj -c Release -p:GameDir="<path to Cities Skylines II>"
   ```
2. Collect a clean staging folder:
   - `CitizenSoul.dll`
   - `lang/en-US.json` (keep the folder name `lang`)

## Publish
1. Run `ModPublisher.exe` from the toolchain and point it at the staging folder
   (it reads `Properties/PublishConfiguration.xml`; copy it next to the staging
   folder if the tool asks for it).
   - First publish: leave `<ModId Value="" />` — Paradox assigns the id.
2. After the first publish, write the returned id back and commit:
   `<ModId Value="12345" />` — **updates require it**.
3. For every update: bump `<ModVersion>`, set a `<ChangeLog>` entry, and keep
   `<GameVersion>` equal to the game version you tested against.

## Release checklist
- [ ] StubCheck compile-check is green:
      `dotnet build Tools/StubCheck/CitizenSoul.StubCheck.csproj`
- [ ] Real Release build with the toolchain; mod loads in-game; a city runs for 10+ min
- [ ] Perf gate read from the log (see PERF.md) — no `perf:` warnings
- [ ] `PublishConfiguration.xml`: ModVersion bumped, ChangeLog set, GameVersion current
- [ ] `lang/en-US.json` present in the staging folder
- [ ] Smoke test from a clean mods-folder install (not the dev symlink)
