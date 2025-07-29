using BepInEx.Logging;
using HarmonyLib;
using LevelGeneration;

namespace TerminalCompletion.Plugin;

[HarmonyPatch]
internal class Patch
{
    //private static LG_ComputerTerminal? m_terminal;
    //
    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnProximityEnter))]
    //[HarmonyPrefix]
    //public static bool TerminalApproached(ref LG_TERM_PlayerInteracting __instance)
    //{
    //    m_terminal = __instance.m_terminal;
    //    return true;
    //}
    //
    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnProximityExit))]
    //[HarmonyPrefix]
    //public static bool TerminalLeft(ref LG_TERM_PlayerInteracting __instance)
    //{
    //    m_terminal = null;
    //    return true;
    //}

    [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnReturn))]
    [HarmonyPrefix]
    public static bool ProcessCommand(ref LG_TERM_PlayerInteracting __instance)
    {
        var term = __instance.m_terminal;

        string commandStr = term.m_currentLine.ToUpper();
        CommandExecutor.ExecCommand(commandStr, term);

        return false; //Returning true appears to trigger existing functionality
    }

    [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnRegularInput))]
    [HarmonyPrefix]
    public static bool ProcessTabCompletion(ref LG_TERM_PlayerInteracting __instance, bool hasOffset, int offsetIndex, char character)
    {
        //TerminalPlugin.Logger.LogInfo($"{__instance} {hasOffset} {offsetIndex} {character}");
        return true;
    }


    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TryUpdateLineForAutoComplete))]
    [HarmonyPrefix]
    public static bool ProcessTabCompletion(ref LG_ComputerTerminalCommandInterpreter __instance, string input, ref string autoCompletedLine)
    {
        bool completionSuccess = CommandExecutor.TryAutoCompletion(input, ref autoCompletedLine);
        if (completionSuccess)
        {
            __instance.m_terminal.m_currentLine = autoCompletedLine;
        }

        return !completionSuccess;
    }


}
