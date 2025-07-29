using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppSystem;
using HarmonyLib;
using BepInEx.Logging;

using LogUtils;

namespace TerminalCompletion.Plugin;

[BepInPlugin(Guid, Name, Version)]
public class TerminalPlugin : BasePlugin
{
    public const string Guid = "Drummerx04.TerminalExpansionPlugin";
    public const string Name = "TerminalExpansionPlugin";
    public const string Version = "1.0.0";

    public TerminalPlugin() { Logger = Log; }

    public static ManualLogSource Logger { get; private set; }

    public override void Load()
    {
        Logger = Log;
        Harmony.CreateAndPatchAll(typeof(Patch), Guid);
    }
}
