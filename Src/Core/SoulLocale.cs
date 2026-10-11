// Citizen Soul — M6 (Ship): player-facing string table.
//
// Built-in en-US defaults live here; an optional lang/<culture>.json next to
// the mod dll can override any key (flat JSON: {"CitizenSoul.KEY": "text"}).
// Missing file, unreadable file, or missing keys all fall back cleanly — the
// game can never fail to draw a panel because a translation is absent.
//
// Culture pick: OS UI culture first (e.g. "de-DE"), then its two-letter form
// ("de"), then "en-US". Game-language integration (reading the game's own
// locale setting) is an EP1 item; see LOCALIZATION notes in README.
//
// The same lang/en-US.json doubles as the translator template: copy it to
// lang/<your-culture>.json and translate the values (keep {0}-style slots).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CitizenSoul
{
    public static class SoulLocale
    {
        /// <summary>All runtime keys are namespaced ("CitizenSoul.KEY") for future i18n tooling.</summary>
        public const string Prefix = "CitizenSoul.";

        private static Dictionary<string, string> s_Table;
        private static string s_Source = "built-in en-US";

        /// <summary>Looks up a key (short form; the prefix is added automatically).</summary>
        public static string Get(string key)
        {
            EnsureLoaded();
            string v;
            if (s_Table.TryGetValue(Prefix + key, out v)) return v;
            return Prefix + key; // visible marker for a missing string
        }

        /// <summary>Get() + string.Format with slot-safety (a bad translation logs once and renders raw).</summary>
        public static string Fmt(string key, params object[] args)
        {
            string fmt = Get(key);
            try
            {
                return string.Format(fmt, args);
            }
            catch (FormatException)
            {
                SoulLog.Once("loc.fmt." + key, "locale: bad format string for '" + key + "' (check {0}-style slots)");
                return fmt;
            }
        }

        /// <summary>One-line state summary for the load log.</summary>
        public static string DescribeState()
        {
            EnsureLoaded();
            return "localization: " + s_Source + " (" + s_Table.Count + " keys)";
        }

        private static void EnsureLoaded()
        {
            if (s_Table != null) return;
            s_Table = new Dictionary<string, string>(48);
            Defs();
            try
            {
                TryFile();
            }
            catch (Exception ex)
            {
                SoulLog.Exception("loc.load", ex);
                s_Source = "built-in en-US";
            }
        }

        private static void Defs()
        {
            s_Table[Prefix + "PANEL_EMPTY"] = "(no soul followed)";
            s_Table[Prefix + "INSPECTOR_NO_SOUL"] = "(no soul)";
            s_Table[Prefix + "INSPECTOR_SUMMARY_FMT"] = "mood {0}; ties {1} (best {2}, worst {3});{4}";
            s_Table[Prefix + "INSPECTOR_TRAITS_FMT"] = " extrav {0}, consist {1}";
            s_Table[Prefix + "INSPECTOR_MIND_FMT"] = "trauma {0}, depression {1}, worry {2}, addiction {3}";
            s_Table[Prefix + "INSPECTOR_THERAPY"] = ", in therapy";
            s_Table[Prefix + "INSPECTOR_PET_FMT"] = "; companion kind {0} (bond {1})";
            s_Table[Prefix + "INSPECTOR_CONDITION_FMT"] = "; managing condition {0}";
            s_Table[Prefix + "INSPECTOR_CRIME_FMT"] = "; notoriety {0}, convictions {1}";
            s_Table[Prefix + "INSPECTOR_SKILLS_FMT"] = "social {0}, fitness {1}, logic {2}, craft {3}";
        }

        private static void TryFile()
        {
            string dir = Path.Combine(Path.GetDirectoryName(typeof(SoulLocale).Assembly.Location) ?? ".", "lang");
            string culture = "";
            try { culture = CultureInfo.CurrentUICulture.Name; } catch { }
            string two = culture.Length >= 2 ? culture.Substring(0, 2) : "";

            string[] candidates = culture.Length > 0 && two.Length > 0 && culture != two
                ? new string[] { culture, two, "en-US" }
                : new string[] { culture.Length > 0 ? culture : "en-US", "en-US" };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].Length == 0) continue;
                string path = Path.Combine(dir, candidates[i] + ".json");
                if (!File.Exists(path)) continue;
                int merged = MergeJson(File.ReadAllText(path, Encoding.UTF8));
                s_Source = "lang/" + candidates[i] + ".json (+" + merged + " overrides)";
                return;
            }
        }

        /// <summary>
        /// Flat-JSON merge: top-level "key": "value" pairs only. Braces, commas
        /// and whitespace between pairs are skipped; nested values end the scan
        /// (the file contract is flat). Returns the number of overrides merged.
        /// </summary>
        private static int MergeJson(string s)
        {
            int i = 0;
            int merged = 0;
            while (i < s.Length)
            {
                i = SkipWs(s, i);
                if (i >= s.Length) break;
                char c = s[i];
                if (c == '{' || c == '}' || c == ',') { i++; continue; }
                if (c != '"') break;
                string key;
                i = ReadString(s, i, out key);
                if (i < 0) break;
                i = SkipWs(s, i);
                if (i >= s.Length || s[i] != ':') break;
                i = SkipWs(s, i + 1);
                if (i >= s.Length || s[i] != '"') break; // flat file contract: string values only
                string val;
                i = ReadString(s, i, out val);
                if (i < 0) break;
                s_Table[key] = val;
                merged++;
            }
            return merged;
        }

        private static int SkipWs(string s, int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
            return i;
        }

        /// <summary>Reads a JSON string starting at s[i]=='"'; returns index past the closing quote, or -1.</summary>
        private static int ReadString(string s, int i, out string result)
        {
            result = "";
            StringBuilder sb = new StringBuilder(64);
            i++; // skip opening quote
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '"') { result = sb.ToString(); return i + 1; }
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    if (n == 'n') sb.Append('\n');
                    else if (n == 'r') sb.Append('\r');
                    else if (n == 't') sb.Append('\t');
                    else if (n == 'b') sb.Append('\b');
                    else if (n == 'f') sb.Append('\f');
                    else if (n == 'u' && i + 5 < s.Length)
                    {
                        int code = 0;
                        int ok = 1;
                        for (int k = i + 2; k < i + 6; k++)
                        {
                            int d = Hex(s[k]);
                            if (d < 0) { ok = 0; break; }
                            code = (code << 4) | d;
                        }
                        if (ok == 1) { sb.Append((char)code); i += 6; continue; }
                        sb.Append(n); // malformed escape: keep the raw char
                    }
                    else sb.Append(n); // covers \" \\ \/ and anything unexpected
                    i += 2;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return -1; // unterminated
        }

        private static int Hex(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
