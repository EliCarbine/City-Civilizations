using System;
using System.Collections.Generic;
using UnityEngine;

namespace CitizenSoul
{
    /// <summary>
    /// Rate-limited logging for every soul system. Hot paths must never spew:
    ///   - Once(key, msg):    logs the first occurrence, then every 256th, and
    ///                        reports a running suppressed count. Use in catch blocks.
    ///   - Throttled(key,msg): logs at most once per minDeltaMs for a given key.
    ///   - Info/Warn/Err:     plain pass-throughs for one-off diagnostics.
    ///
    /// Main-thread only by design (all callers are ECS systems on the sim thread),
    /// so no locking is needed. State is intentionally tiny: two dictionaries and
    /// two ints, cleared by Reset() when the mod is disposed.
    /// </summary>
    public static class SoulLog
    {
        public const string Prefix = "[CitizenSoul] ";

        private static readonly Dictionary<string, int> s_OnceCounts = new Dictionary<string, int>(64);
        private static readonly Dictionary<string, int> s_LastTickMs = new Dictionary<string, int>(64);

        /// <summary>Total messages suppressed by Once/Throttled since load.</summary>
        public static int Suppressed { get; private set; }

        public static void Info(string msg) { Debug.Log(Prefix + msg); }

        public static void Warn(string msg) { Debug.LogWarning(Prefix + msg); }

        public static void Err(string msg) { Debug.LogError(Prefix + msg); }

        /// <summary>Logs the message on first call per key, then every 256th call.</summary>
        public static void Once(string key, string msg)
        {
            int count;
            if (!s_OnceCounts.TryGetValue(key, out count))
            {
                s_OnceCounts[key] = 1;
                Err(msg); // unexpected states are errors until proven fine
                return;
            }

            s_OnceCounts[key] = count + 1;
            Suppressed++;
            if ((count & 0xFF) == 0)
            {
                Err(msg + " [repeated x" + count + "]");
            }
        }

        /// <summary>Logs a caught exception once per key (full stack on first hit).</summary>
        public static void Exception(string key, Exception ex)
        {
            Once(key, ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
        }

        /// <summary>Logs at most once per minDeltaMs per key. For noisy-but-expected paths.</summary>
        public static void Throttled(string key, string msg, int minDeltaMs)
        {
            int now = Environment.TickCount;
            int last;
            if (s_LastTickMs.TryGetValue(key, out last) && unchecked(now - last) < minDeltaMs)
            {
                Suppressed++;
                return;
            }
            s_LastTickMs[key] = now;
            Warn(msg);
        }

        /// <summary>Drops all throttling state. Called from Mod.OnDispose.</summary>
        public static void Reset()
        {
            s_OnceCounts.Clear();
            s_LastTickMs.Clear();
            Suppressed = 0;
        }
    }
}
