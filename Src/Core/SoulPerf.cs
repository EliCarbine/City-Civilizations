// Citizen Soul — M6 (Ship): perf sampler (the "perf gate" instrumentation).
//
// Every soul system records its pass duration (two Stopwatch timestamp reads,
// no allocation in steady state). Every 5 minutes of wall time one Info line
// summarizes the window — total average per pass + the six slowest systems —
// and a Warn fires if any single system exceeds the per-pass budget, naming
// the options.ini group that can switch it off. See PERF.md for the audit
// and the budget rationale.
//
// This is deliberately tiny: one dictionary of cells, one timestamp. It is
// measurement for the player + the QA gate, not a full profiler — deep
// traces still come from the game dev tools.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CitizenSoul
{
    internal static class SoulPerf
    {
        /// <summary>Per-pass average above this triggers a warning (see PERF.md).</summary>
        public const double PerPassWarnMs = 4.0;

        private const double ReportEverySeconds = 300.0;
        private const int TopN = 6;

        private sealed class Cell
        {
            public long Ticks;
            public int Calls;
        }

        private static readonly Dictionary<string, Cell> s_Cells = new Dictionary<string, Cell>(64);
        private static long s_LastReport;

        public static long Begin()
        {
            return Stopwatch.GetTimestamp();
        }

        public static void Record(string name, long beginTicks)
        {
            long dt = Stopwatch.GetTimestamp() - beginTicks;
            Cell c;
            if (!s_Cells.TryGetValue(name, out c))
            {
                c = new Cell();
                s_Cells[name] = c;
            }
            c.Ticks += dt;
            c.Calls++;

            long now = Stopwatch.GetTimestamp();
            if (s_LastReport == 0)
            {
                s_LastReport = now;
                return;
            }
            if (now - s_LastReport >= (long)(ReportEverySeconds * Stopwatch.Frequency)) Report();
        }

        /// <summary>Emits the window summary and resets the window (keeps counters tiny forever).</summary>
        public static void Report()
        {
            s_LastReport = Stopwatch.GetTimestamp();
            if (s_Cells.Count == 0) return;

            double f = 1000.0 / Stopwatch.Frequency;
            double total = 0.0;
            string[] names = new string[TopN];
            double[] avgs = new double[TopN];

            foreach (KeyValuePair<string, Cell> kv in s_Cells)
            {
                double avg = kv.Value.Calls > 0 ? (kv.Value.Ticks * f) / kv.Value.Calls : 0.0;
                total += avg;
                for (int i = 0; i < TopN; i++)
                {
                    if (avg > avgs[i])
                    {
                        for (int j = TopN - 1; j > i; j--)
                        {
                            avgs[j] = avgs[j - 1];
                            names[j] = names[j - 1];
                        }
                        avgs[i] = avg;
                        names[i] = kv.Key;
                        break;
                    }
                }
                kv.Value.Ticks = 0;
                kv.Value.Calls = 0;
            }

            string top = "";
            for (int i = 0; i < TopN; i++)
            {
                if (names[i] == null) break;
                if (i > 0) top += "; ";
                top += names[i] + " " + avgs[i].ToString("F2") + "ms";
            }
            SoulLog.Info("perf/5min: total avg per pass " + total.ToString("F2") + "ms across " + s_Cells.Count + " systems; slowest: " + top);

            if (names[0] != null && avgs[0] > PerPassWarnMs)
            {
                SoulLog.Warn("perf: " + names[0] + " averages " + avgs[0].ToString("F2") + "ms/pass (budget " + PerPassWarnMs.ToString("F1") + "ms) — consider switching its group off in options.ini");
            }
        }
    }
}
