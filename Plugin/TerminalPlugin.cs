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
    public const string Guid = "Drummerx04.TerminalCompletionPlugin";
    public const string Name = "TerminalCompletionPlugin";
    public const string Version = "1.2.2";

    public TerminalPlugin() { Logger = Log; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static ManualLogSource Logger { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public override void Load()
    {
        Logger = Log;
        Harmony.CreateAndPatchAll(typeof(Patch), Guid);
    }
}
