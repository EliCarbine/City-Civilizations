// Citizen Soul -- mod-folder resolution shared by the locale and options loaders.
//
// The game can load local mod assemblies from memory, so Assembly.Location can
// be an empty string -- and Mono's Path.GetDirectoryName("") THROWS
// ArgumentException ("Invalid path") instead of returning null, which the
// ?? "." guard cannot catch. On the MSI the Location/CodeBase probes resolved
// to game-install paths, so options.ini landed at the game root instead of
// next to the dll. The chain now prefers the deterministic deploy target
// (Tools/Publish.ps1 -Deploy stages into Mods\CitizenSoul) and only trusts
// Location/CodeBase when the resolved dir actually contains CitizenSoul.dll.
// Final fallback creates the Mods folder; null only if even that fails.

using System;
using System.IO;

namespace CitizenSoul
{
    internal static class SoulPaths
    {
        private static string s_ModDir;
        private static bool s_Tried;

        private static string TryResolve(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return null;
            try { if (File.Exists(Path.Combine(dir, "CitizenSoul.dll"))) return dir; } catch { }
            return null;
        }

        /// <summary>Best-effort mod folder (cached). Null when undeterminable.</summary>
        public static string ModDir()
        {
            if (s_Tried) return s_ModDir;
            s_Tried = true;
            try
            {
                // Probe 1: the deploy target. Deterministic and always correct
                // for local play; a hit here keeps options.ini next to the dll.
                string mods = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Colossal Order", "Cities Skylines II", "Mods", "CitizenSoul");
                string hit = TryResolve(mods);
                if (hit != null) { s_ModDir = hit; return hit; }

                System.Reflection.Assembly asm = typeof(SoulPaths).Assembly;
                try
                {
                    string loc = asm.Location;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        hit = TryResolve(Path.GetDirectoryName(loc));
                        if (hit != null) { s_ModDir = hit; return hit; }
                    }
                }
                catch { }
                try
                {
                    // CodeBase is obsolete on modern .NET only (SDK harness warning); it is
                    // a legit second probe on net48/Mono for memory-loaded assemblies.
#pragma warning disable SYSLIB0012
                    string cb = asm.CodeBase;
#pragma warning restore SYSLIB0012
                    if (!string.IsNullOrEmpty(cb))
                    {
                        hit = TryResolve(Path.GetDirectoryName(new Uri(cb).LocalPath));
                        if (hit != null) { s_ModDir = hit; return hit; }
                    }
                }
                catch { }

                // Last resort: create the deploy-target folder so options.ini
                // and lang/ never fall back to CWD-relative "." (game root).
                try
                {
                    Directory.CreateDirectory(mods);
                    s_ModDir = mods;
                    return mods;
                }
                catch { }
            }
            catch { }
            s_ModDir = null;
            return null;
        }
    }
}
