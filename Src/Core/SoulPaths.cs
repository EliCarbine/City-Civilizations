// Citizen Soul -- mod-folder resolution shared by the locale and options loaders.
//
// The game loads local mod assemblies from memory, so Assembly.Location can be
// an empty string -- and Mono's Path.GetDirectoryName("") THROWS
// ArgumentException ("Invalid path") instead of returning null, which the
// ?? "." guard cannot catch. Every candidate here is guarded; the chain ends
// at the standard local-Mods folder that Tools/Publish.ps1 -Deploy targets.
// Returns null when undeterminable; callers fall back to CWD-relative ".".

using System;
using System.IO;

namespace CitizenSoul
{
    internal static class SoulPaths
    {
        private static string s_ModDir;
        private static bool s_Tried;

        /// <summary>Best-effort mod folder (cached). Null when undeterminable.</summary>
        public static string ModDir()
        {
            if (s_Tried) return s_ModDir;
            s_Tried = true;
            try
            {
                System.Reflection.Assembly asm = typeof(SoulPaths).Assembly;
                try
                {
                    string loc = asm.Location;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        string d = Path.GetDirectoryName(loc);
                        if (!string.IsNullOrEmpty(d)) { s_ModDir = d; return s_ModDir; }
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
                        string d = Path.GetDirectoryName(new Uri(cb).LocalPath);
                        if (!string.IsNullOrEmpty(d)) { s_ModDir = d; return s_ModDir; }
                    }
                }
                catch { }
                try
                {
                    string mods = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "AppData", "LocalLow", "Colossal Order", "Cities Skylines II", "Mods", "CitizenSoul");
                    if (Directory.Exists(mods)) { s_ModDir = mods; return s_ModDir; }
                }
                catch { }
            }
            catch { }
            s_ModDir = null;
            return null;
        }
    }
}
