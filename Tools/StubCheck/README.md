# StubCheck — offline compile check

The real mod builds against the Cities: Skylines II game assemblies (Reference Assemblies from the CS2 install, .NET Framework 4.8). That build needs the game and Windows-format assemblies — it cannot run on a bare macOS/Linux box.

This project compiles everything under `Src/` against **signature-faithful stubs** of the external API surface the mod touches:

- `Unity.Entities` — Entity, ComponentType, EntityQuery, EntityManager, DynamicBuffer, NativeArray, SystemBase/ComponentSystemBase, World, Allocator
- `Unity.Mathematics` — `Random`
- `UnityEngine` — `Debug`
- `Colossal.Serialization.Entities` — `ISerializable`, `IWriter`, `IReader`, `Context`
- `Game.*` — `IMod` (`Game.Modding`), `UpdateSystem`, `SystemUpdatePhase`, `GameSystemBase`, `Game.Simulation.SimulationSystem`, `Game.Citizens.*` vanilla components

Bodies are inert — nothing executes. A green build therefore proves **syntax + internal consistency + API shape**, not runtime behavior.

## Run

```sh
dotnet build Tools/StubCheck/CitizenSoul.StubCheck.csproj
```

## Keeping stubs honest

When mod code starts using a new engine API, the stub must be extended in the same commit. The stub signatures mirror the real ones (`using` namespaces, generic constraints, overloads); if the real game API and a stub ever disagree, fix the stub or the call site — never let the stub mask a real-build break.
