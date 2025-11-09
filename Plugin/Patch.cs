using BepInEx.Logging;
using HarmonyLib;
using LevelGeneration;
using System.ComponentModel.Design;

namespace TerminalCompletion.Plugin;

[HarmonyPatch]
internal class Patch
{

    [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    [HarmonyPostfix]
    public static void CheckForPlayerReady(GameStateManager __instance, eGameStateName nextState)
    {
        //Trigger the clearing of persistent item data. This should prevent resource leaks and unintended completions.
        if (nextState == eGameStateName.InLevel || nextState == eGameStateName.Lobby)
        {
            CommandExecutor.ClearAllData();
        }
    }

    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.SyncIncomingTerminalCommand))]
    [HarmonyPrefix]
    public static bool ProcessSyncCommand(LG_ComputerTerminal __instance, TERM_Command cmd, string inputLine, string param1, string param2)
    {

        if (CommandExecutor.EnableDebugOutput)
            TerminalPlugin.Logger.LogDebug($"LinesSinceCommand: {__instance.m_command.m_linesSinceCommand}\nCMD: {cmd} \"{inputLine}\" \"{param1}\" \"{param2}\"");

        //Exit early if mod disabled
        if (!CommandExecutor.EnableModBehavior) { return true; }

        __instance.m_command.ResetLinesSinceCommand();
        switch (cmd)
        {
            case TERM_Command.ShowList:
            case TERM_Command.Query:
            case TERM_Command.MAX_COUNT + 1:
                CommandExecutor.ExecCommand(__instance, inputLine);
                return false;
            case TERM_Command.ListLogs:
                CommandExecutor.RunLogList(__instance);
                return false;
            case TERM_Command.ReadLog:
                CommandExecutor.RunReadLog(__instance, param1);
                return false;
            default:
                return true;
        }


    }

    [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnReturn))]
    [HarmonyPrefix]
    public static bool ProcessCommand(ref LG_TERM_PlayerInteracting __instance)
    {
        LG_ComputerTerminal term = __instance.m_terminal;

        if (CommandExecutor.EnableModBehavior && term.m_currentLine.StartsWith('!'))
        {
            term.m_currentLine = CommandExecutor.GetFromHistory(term, term.m_currentLine[1..]);
        }


        //CommandExecutor.ExecCommand(__instance.m_terminal, __instance.m_terminal.m_currentLine.ToUpper());
        var words = term.m_currentLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0)
        {
            if (words[0] == "TC")
            {
                CommandExecutor.ExecCommand(term, term.m_currentLine);
                return false;
            }
            //Exit Early if mod "disabled"
            if (!CommandExecutor.EnableModBehavior) { return true; }
            if (words[0] == ("HIST"))
            {
                LG_ComputerTerminalManager.WantToSendTerminalCommand(term.SyncID, TERM_Command.MAX_COUNT + 1, term.m_currentLine, string.Empty, string.Empty);
                return false;
            }
            else if (words[0] == ("LS"))
            {
                words[0] = "LIST";
                LG_ComputerTerminalManager.WantToSendTerminalCommand(term.SyncID, TERM_Command.ShowList, string.Join(" ", words),
                    words.Length >= 2 ? words[1] : string.Empty,
                    words.Length >= 3 ? words[2] : string.Empty
                    );
                return false;
            }
        }
        return true; //Returning true appears to trigger existing functionality
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

    //[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ShowList))]
    //[HarmonyPrefix]
    //public static bool ShowList(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2)
    //{
    //    LG_ComputerTerminal term = __instance.m_terminal;
    //    return false;
    //}



    //[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.AddOutput), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
    //[HarmonyPrefix]
    //public static bool ProcessAddOutput(LG_ComputerTerminalCommandInterpreter __instance, Il2CppSystem.Collections.Generic.List<string> lines)
    //{
    //    foreach (var line in lines)
    //        TerminalPlugin.Logger.LogDebug($"Holy crap? AddOutput: {line}");
    //    return true;
    //}

    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnRegularInput))]
    //[HarmonyPrefix]
    //public static bool ProcessTabCompletion(ref LG_TERM_PlayerInteracting __instance, bool hasOffset, int offsetIndex, char character)
    //{
    //    TerminalPlugin.Logger.LogInfo($"Input: {__instance.m_terminal.IsWaitingForAnyKeyInLinePause} | {hasOffset} {offsetIndex} {character}");
    //    return true;
    //}
    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.OnIsWaitingForAnyKeyInLinePause))]
    //[HarmonyPrefix]
    //public static bool IsWaiting(ref LG_TERM_PlayerInteracting __instance)
    //{
    //    TerminalPlugin.Logger.LogInfo($"Waiting: {__instance.m_terminal.IsWaitingForAnyKeyInLinePause}");
    //    //if (__instance.m_terminal.IsWaitingForAnyKeyInLinePause)
    //    //{
    //    //    __instance.m_terminal.IsWaitingForAnyKeyInLinePause = false;
    //    //   
    //    //}
    //    //__instance.m_terminal.m_isWaitingForAnyKeyInLinePause = false;
    //    
    //    return true;
    //}

    //[HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.MoveCaretLeft))]
    //[HarmonyPrefix]
    //public static bool LeftCaret(ref LG_TERM_PlayerInteracting __instance, LG_TERM_PlayerInteracting.ModifierKeys modKeys)
    //{
    //    //TerminalPlugin.Logger.LogInfo($"{__instance} {hasOffset} {offsetIndex} {character}");
    //    return true;
    //}
}
