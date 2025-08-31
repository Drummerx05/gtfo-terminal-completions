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


    [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    [HarmonyPostfix]
    public static void CheckForPlayerReady(GameStateManager __instance, eGameStateName nextState)
    {
        //Trigger the clearing of persistent item data. This should prevent resource leaks and unintended completions.
        if(nextState == eGameStateName.InLevel || nextState == eGameStateName.Lobby) 
        {
            CommandExecutor.ClearAllData();
        }
    }

    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.SyncIncomingTerminalCommand))]
    [HarmonyPrefix]
    public static bool ProcessSyncCommand(LG_ComputerTerminal __instance, TERM_Command cmd, string inputLine, string param1, string param2)
    {
        if(cmd == TERM_Command.MAX_COUNT + 1 && !CommandExecutor.commandId.Equals(param1))
        {
            CommandExecutor.ExecCommand(__instance, inputLine, false);
            return false;
        }
        else
        {
            return true;
        }
    }
    //[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.AddOutput), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
    //[HarmonyPrefix]
    //public static bool ProcessAddOutput(LG_ComputerTerminalCommandInterpreter __instance, Il2CppSystem.Collections.Generic.List<string> lines)
    //{
    //    foreach (var line in lines)
    //        TerminalPlugin.Logger.LogDebug($"Holy crap? AddOutput: {line}");
    //    return true;
    //}

    [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnReturn))]
    [HarmonyPrefix]
    public static bool ProcessCommand(ref LG_TERM_PlayerInteracting __instance)
    {
        var term = __instance.m_terminal; 
        string commandStr = term.m_currentLine.ToUpper();
        CommandExecutor.ExecCommand(term, commandStr);
        return false; //Returning true appears to trigger existing functionality
    }
    //[HarmonyPatch(typeof(LG_TERM_Base), nameof(LG_TERM_Base.OnInteract))]
    //[HarmonyPrefix]
    //public static bool TerminalInteractStart(LG_TERM_Base __instance)
    //{
    //    CommandExecutor.m_currentTerminal = __instance.m_terminal;
    //    return true;
    //}
    ////
    //[HarmonyPatch(typeof(LG_TERM_Base), nameof(LG_TERM_Base.OnProximityExit))]
    //[HarmonyPrefix]
    //public static bool TerminalInteractStop(LG_TERM_Base __instance)
    //{
    //    CommandExecutor.m_currentTerminal = null;
    //    return true;
    //}


    [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnRegularInput))]
    [HarmonyPrefix]
    public static bool ProcessTabCompletion(ref LG_TERM_PlayerInteracting __instance, bool hasOffset, int offsetIndex, char character)
    {
        //TerminalPlugin.Logger.LogInfo($"{__instance} {hasOffset} {offsetIndex} {character}");
        return true;
    }

    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.MoveCaretLeft))]
    //[HarmonyPrefix]
    //public static bool LeftCaret(ref LG_TERM_PlayerInteracting __instance, LG_TERM_PlayerInteracting.ModifierKeys modKeys)
    //{
    //    //TerminalPlugin.Logger.LogInfo($"{__instance} {hasOffset} {offsetIndex} {character}");
    //    return true;
    //}


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
