# Soul Panel UI (M4)

The C# side lives in `Src/Systems/SoulPanelUISystem.cs`. It exposes the followed
citizen's soul as UI bindings:

| Group      | Binding   | Type   | Content                                  |
|------------|-----------|--------|------------------------------------------|
| `soulpanel`| `summary` | string | One-line identity + mood + level         |
| `soulpanel`| `details` | string | Mind block (traits, mental weather)      |
| `soulpanel`| `skills`  | string | Skill levels (logic/craft/social/fitness)|
| `soulpanel`| `recent`  | string | Recent diary entries (newline-joined)    |

Refresh cadence: every 30 UI frames; no-op unless exactly one citizen carries
`Game.Citizens.Followed`.

## Pending (M6 QA on a machine with the game)

- Verify the `Colossal.UI.Binding` / `Game.UI.UISystemBase` surface against real
  assemblies (stubs in `Tools/StubCheck/Stubs/ColossalUIStubs.cs` are doc-modeled).
- Confirm `SystemUpdatePhase.UIUpdate` exists in the shipping enum.
- Author the cohtml view (JS/JSX) for these bindings with the official Cities:
  Skylines II UI toolchain, and register the UI module in the mod manifest.
