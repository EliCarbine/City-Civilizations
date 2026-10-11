// Citizen Soul — M6 (Ship): player-facing options.
//
// options.ini lives next to the mod dll (same folder as CitizenSoul.dll) and
// is created with commented defaults on first load. Restart the game after
// editing. The parser is intentionally tiny and forgiving: unknown keys are
// ignored (logged once), malformed values keep their default, and any IO
// problem leaves the mod running fully enabled — options can never brick a
// city, they can only switch feature groups off.

using System;
using System.Collections.Generic;
using System.IO;

namespace CitizenSoul
{
    public static class SoulOptions
    {
        private static readonly string[] s_Groups =
        {
            "social", "body", "romance", "mind", "crime", "companions", "family",
            "work", "culture", "sports", "curiosity", "civic", "quirks", "tempo",
            "travel", "media", "rituals", "ui"
        };

        private static readonly Dictionary<string, string> s_SystemGroup = new Dictionary<string, string>(64);
        private static readonly HashSet<string> s_Core = new HashSet<string>();
        private static readonly Dictionary<string, bool> s_State = new Dictionary<string, bool>(32);

        private static bool s_Loaded;
        private static bool s_Master = true;
        private static string s_Path = "";

        /// <summary>Master switch (enabled= in options.ini).</summary>
        public static bool Master { get { EnsureLoaded(); return s_Master; } }

        public static string FilePath { get { EnsureLoaded(); return s_Path; } }

        /// <summary>Force the load (called once from Mod.OnLoad so the log carries the result).</summary>
        public static void Load() { EnsureLoaded(); }

        /// <summary>Group switch used by the panel + light checks ("ui", "social", ...).</summary>
        public static bool IsOn(string group)
        {
            EnsureLoaded();
            if (!s_Master) return false;
            bool on;
            if (s_State.TryGetValue(group, out on)) return on;
            return true; // unknown group: fail-open
        }

        /// <summary>
        /// Gate used by SoulSystemBase.OnUpdate. Known systems follow their
        /// group; unmapped names run always-on and warn once (keeps a future
        /// system from silently vanishing because a mapping was forgotten).
        /// </summary>
        public static bool SystemEnabled(string typeName)
        {
            EnsureLoaded();
            if (!s_Master) return false;
            string grp;
            if (s_SystemGroup.TryGetValue(typeName, out grp)) return IsOn(grp);
            if (!s_Core.Contains(typeName))
                SoulLog.Once("opt.map." + typeName, "options: no toggle mapped for " + typeName + " (running always-on)");
            return true;
        }

        /// <summary>One-line state summary for the load log.</summary>
        public static string DescribeState()
        {
            EnsureLoaded();
            string s = "options: enabled=" + (s_Master ? "on" : "off");
            for (int i = 0; i < s_Groups.Length; i++)
                s += " " + s_Groups[i] + "=" + (IsOn(s_Groups[i]) ? "on" : "off");
            return s + " (" + s_Path + ")";
        }

        private static void EnsureLoaded()
        {
            if (s_Loaded) return;
            s_Loaded = true; // set first: recursion-safe
            BuildMaps();
            for (int i = 0; i < s_Groups.Length; i++) s_State[s_Groups[i]] = true;
            s_Path = ResolvePath();
            try
            {
                if (File.Exists(s_Path)) ParseFile(File.ReadAllLines(s_Path));
                else WriteDefault();
            }
            catch (Exception ex)
            {
                s_Master = true;
                for (int i = 0; i < s_Groups.Length; i++) s_State[s_Groups[i]] = true;
                SoulLog.Exception("opt.load", ex);
                SoulLog.Warn("options: load failed; running fully enabled");
            }
        }

        private static void BuildMaps()
        {
            // Core = always-on soul lifeblood (initialization, learning, mood, diary).
            s_Core.Add("SoulInitializeSystem");
            s_Core.Add("DailyLearningSystem");
            s_Core.Add("EmotionSystem");
            s_Core.Add("SoulJournalSystem");

            Map("social", "SocializeSystem", "SoulSpikeSystem", "FightResolutionSystem", "MemoryDecaySystem");
            Map("body", "InjurySystem", "VitalsSystem");
            Map("romance", "RomanceSystem", "DramaSystem");
            Map("mind", "MindSystem", "ChronicSystem", "TherapySystem");
            Map("crime", "StreetSystem", "JusticeSystem");
            Map("companions", "PetSystem");
            Map("family", "FamilySystem", "MemorialSystem");
            Map("work", "WorkSystem", "ReentrySystem", "MidlifeSystem");
            Map("culture", "ArtsSystem");
            Map("sports", "SportsSystem");
            Map("curiosity", "CuriositySystem", "TechSystem");
            Map("civic", "CivicSystem", "NatureSystem");
            Map("quirks", "QuirksSystem");
            Map("tempo", "TempoSystem");
            Map("travel", "TravelSystem");
            Map("media", "MediaSystem");
            Map("rituals", "RitualsSystem");
            Map("ui", "SoulPanelUISystem");
            // EP1 (City Life): extended-play systems join their home groups.
            Map("family", "MortalitySystem");
            Map("work", "StrikeSystem", "MentorSystem");
            Map("crime", "GamblingSystem", "CrimeSystem", "CorruptionSystem");
            Map("tempo", "RoutineSystem");
            Map("culture", "FoodSystem", "FandomSystem", "FashionSystem");
            Map("rituals", "FaithSystem", "HolidaySystem");
            Map("civic", "HoodSystem");
            Map("social", "GossipSystem", "ReputationSystem", "NightlifeSystem");
            Map("media", "ChronicleSystem");
        }

        private static void Map(string group, params string[] systems)
        {
            for (int i = 0; i < systems.Length; i++) s_SystemGroup[systems[i]] = group;
        }

        private static string ResolvePath()
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(SoulOptions).Assembly.Location);
                if (string.IsNullOrEmpty(dir)) dir = ".";
                return Path.Combine(dir, "options.ini");
            }
            catch
            {
                return "options.ini";
            }
        }

        private static void ParseFile(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    SoulLog.Once("opt.bad", "options: ignoring malformed line " + (i + 1));
                    continue;
                }
                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string val = line.Substring(eq + 1).Trim().ToLowerInvariant();
                bool on;
                if (!TryBool(val, out on))
                {
                    SoulLog.Once("opt.val." + key, "options: unrecognized value for '" + key + "' (\"" + val + "\") — keeping default");
                    continue;
                }
                if (key == "enabled") { s_Master = on; continue; }
                if (s_State.ContainsKey(key)) { s_State[key] = on; continue; }
                SoulLog.Once("opt.unk." + key, "options: unknown key '" + key + "' ignored");
            }
        }

        private static bool TryBool(string v, out bool result)
        {
            if (v == "on" || v == "true" || v == "1" || v == "yes") { result = true; return true; }
            if (v == "off" || v == "false" || v == "0" || v == "no") { result = false; return true; }
            result = true;
            return false;
        }

        private static void WriteDefault()
        {
            try
            {
                string text =
                    "# Citizen Soul options — restart the game after editing.\n" +
                    "# Values: on/off (true/false, 1/0 also accepted). Unknown keys are ignored.\n" +
                    "# This file was created with defaults on first load.\n" +
                    "\n" +
                    "# Master switch. off = mod loads but every soul system idles (zero sim cost).\n" +
                    "enabled=on\n" +
                    "\n" +
                    "# --- feature groups: off = that group's systems skip their work ---\n" +
                    "social=on\n" +
                    "body=on\n" +
                    "romance=on\n" +
                    "mind=on\n" +
                    "crime=on\n" +
                    "companions=on\n" +
                    "family=on\n" +
                    "work=on\n" +
                    "culture=on\n" +
                    "sports=on\n" +
                    "curiosity=on\n" +
                    "civic=on\n" +
                    "quirks=on\n" +
                    "tempo=on\n" +
                    "travel=on\n" +
                    "media=on\n" +
                    "rituals=on\n" +
                    "ui=on\n";
                File.WriteAllText(s_Path, text);
                SoulLog.Info("options: created default file at " + s_Path);
            }
            catch (Exception ex)
            {
                SoulLog.Info("options: no options.ini and could not create one (" + ex.GetType().Name + "); built-in defaults in effect");
            }
        }
    }
}
