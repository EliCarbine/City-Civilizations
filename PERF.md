# Citizen Soul — performance gate (M6)

Design budget: **≤5% overhead** at 250k citizens (DESIGN.md). The M6 perf gate is the
instrumentation that keeps that verifiable in real cities, plus the ship-time acceptance run.

## How it is measured
- Every soul system pass is timed by `SoulSystemBase.OnUpdate`
  (`SoulPerf.Begin()` / `SoulPerf.Record(name, t0)`) — two `Stopwatch.GetTimestamp()`
  reads, zero allocation in steady state.
- Every 300 s (`SoulPerf.ReportEverySeconds`) the sampler emits one Info line and
  resets its window:
  `perf/5min: total avg per pass 0.42ms across 57 systems; slowest: <top 6 with ms>`
- If the slowest system averages more than **4.0 ms/pass** (`SoulPerf.PerPassWarnMs`),
  a Warn line names it and points at the options.ini group to switch off.

## Ship gate (acceptance run)
Protocol: a mature or synthetic **250k-pop city**, speed 3, 30 minutes hands-off,
default options. Pass criteria:
- No `perf:` warning lines (no system above 4 ms/pass).
- `perf/5min` total average per pass in low single-digit ms (expect ~1–2 ms).
- Panel and inspector stay responsive; no visible hitches while following a citizen.

## Tuning levers (no rebuild needed)
- `options.ini` next to the dll: `enabled=off` (master — all soul systems idle, zero
  sim cost) or per-group `off` for any of the 18 groups (`social`, `body`, `romance`,
  `mind`, `crime`, `companions`, `family`, `work`, `culture`, `sports`, `curiosity`,
  `civic`, `quirks`, `tempo`, `travel`, `media`, `rituals`, `ui`).
  Restart the game after editing.
- Systems without a group mapping run always-on and log once — a forgotten mapping
  can never silently switch a system off.

## Reading the report
- "total avg per pass" is the sum of per-call averages across systems on their own
  cadences — a comparative indicator, not a frame cost.
- Systems run on staggered intervals (`Interval` overrides + the shared day clock in
  `SoulSystemBase.AdvanceDay`), so compare a system against itself over time, not
  cross-system, when deciding what to tune.
- `SoulPerf.Report()` can be called manually while debugging to flush a window early.
