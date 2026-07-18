using BepInEx;
using HarmonyLib;
using System.IO;

namespace MMMedical
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.mmm.edical.world";
        private const string PluginName = "MMMedical";
        private const string PluginVersion = "1.0.5";

        private void Awake()
        {
            var logPath = @"C:\Users\Administrator\AppData\LocalLow\Orsoniks\CasualtiesUnknown\MMMedical.log";
            try { File.WriteAllText(logPath, "=== MMMedical v" + PluginVersion + " ===\n"); } catch { }

            Logger.LogInfo("MMMedical mod loading...");
            try { File.AppendAllText(logPath, "PatchAll starting\n"); } catch { }

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            var patched = harmony.GetPatchedMethods();
            int count = 0;
            foreach (var p in patched)
            {
                count++;
                try { File.AppendAllText(logPath, "  Patched: " + p.DeclaringType?.Name + "." + p.Name + "\n"); } catch { }
            }
            try { File.AppendAllText(logPath, "PatchAll done. Patched " + count + " methods\n"); } catch { }

            Logger.LogInfo($"MMMedical v{PluginVersion} ready.");
        }
    }
}
