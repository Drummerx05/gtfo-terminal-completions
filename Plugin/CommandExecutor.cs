using Il2CppSystem.Data;
using LevelGeneration;
using PlayFab.ClientModels;
using SickDev.CommandSystem;
using static RootMotion.FinalIK.IKSolverVR;

namespace TerminalCompletion.Plugin;

internal class CommandExecutor
{
    private readonly static List<iTerminalItem> s_FoundItems = new();
    private readonly static List<string> s_LogList = new();

    private static string m_command = "";
    private static string m_lastListCommand = "";
    private static string[] m_args = Array.Empty<string>();
    //private static TERM_Command m_command = TERM_Command.None;
    private static Il2CppSystem.Collections.Generic.List<string> m_genericListDefaults = new();
    private static bool m_enableBulkQuery = true;
    private static float m_bulkDelayPerEntry = 0.5f;

    public static void ExecCommand(string command, LG_ComputerTerminal term)
    {
        m_command = command;
        m_args = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        //m_command = TERM_Command.None;


        if (m_args.Length > 0)
        {
            switch (m_args[0])
            {
                case "!LIST":
                case "!LS":
                    ExecCommand(m_lastListCommand, term);
                    return;
                case "LIST":
                case "LS":
                    //m_command = TERM_Command.ShowList;
                    RunList(term);
                    m_lastListCommand = command;
                    break;
                case "QUERY":
                    // m_command = TERM_Command.Query;
                    RunQuery(term);
                    break;
                case "PING":
                    RunPing(term);
                    //m_command = TERM_Command.Ping;
                    break;
                case "LOGS":
                    RunLogList(term);
                    //m_command = TERM_Command.ListLogs;
                    break;
                case "READ":
                    RunReadLog(term);
                    //m_command = TERM_Command.ReadLog;
                    break;
                case "TCSET":
                    RunTCSetCommand(term);
                    AddToHistory(term.m_command);
                    //term.m_command.AddCommand(TERM_Command.ShowList, "LIST", new Localization.LocalizedText("This is a LIST command"), TERM_CommandRule.Normal);
                    break;
                default:
                    //m_command = TERM_Command.None;
                    term.m_command.EvaluateInput(m_command);
                    break;
            }
        }

        term.m_currentLine = "";
    }


    private static void RunTCSetCommand(LG_ComputerTerminal term)
    {
        if (m_args.Count() < 2) { return; }
        switch (m_args[1])
        {
            case "DUMP":
                RunDebugDump(term);
                break;
            case "BULK":
                m_enableBulkQuery = int.TryParse(m_args[2], out int val) && val == 1;
                break;
            case "DELAY":
                if (float.TryParse(m_args[2], out float delay))
                {
                    m_bulkDelayPerEntry = delay;
                }
                break;
        }
    }

    public static void RunDebugDump(LG_ComputerTerminal term)
    {
        var itemList = LG_LevelInteractionManager.GetAllTerminalInterfaces();
        List<string> sortedKeys = new();

        foreach (var pair in itemList) sortedKeys.Add(pair.key);
        sortedKeys.Sort();
        int i = 1;

        foreach (var key in sortedKeys)
        {
            Il2CppSystem.Collections.Generic.List<string>? details = null;
            iTerminalItem item = itemList[key];

            if (item.TerminalItemKey.StartsWith("AMMOPACK") || item.TerminalItemKey.StartsWith("TOOL_REFILL") || item.TerminalItemKey.StartsWith("MEDIPACK"))
            {
                details = item.GetDetailedInfo(m_genericListDefaults);
            }

            TerminalPlugin.Logger.LogInfo(string.Format("{0,5}: {1,-20} {2,-10} {3,-20} {4,-20} {5,-20} {6, -20}",
                i++,
                item.TerminalItemKey,
                item.FloorItemLocation,
                item.SpawnNode?.m_dimension?.DimensionIndex,
                item.FloorItemType,
                item.FloorItemStatus,
                details?[2]));

        }
        //LG_ComputerTerminalManager.WantToSendTerminalString(term.SyncID, m_command);

        LG_ComputerTerminalManager.WantToSendTerminalCommand(term.SyncID, TERM_Command.None, m_command, "", "");
        term.m_command.AddOutput(TerminalLineType.ProgressWait, $"Debug Dump: {sortedKeys.Count} Entities printed!", 4.0F);
    }


    public static bool TryAutoCompletion(string command, ref string output)
    {
        var words = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (words == null || words.Length == 0) { return false; }

        if (words.Length > 1 && (words[0] == "QUERY" || words[0] == "PING"))
        {
            return TryCompletePingQuery(words, ref output);
        }
        else if (words[0] == "READ")
        {
            return TryCompleteRead(words, ref output);
        }

        return false;




    }

    private static bool TryCompleteRead(string[] words, ref string output)
    {
        if (s_LogList.Count == 0) { return false; }

        //No argument, auto pop first log
        if (words.Length == 1)
        {
            output = $"READ {s_LogList[0]}";
            return true;
        }

        int found_I = s_LogList.FindIndex(name => name.StartsWith(words[1]));

        //Argument not found in list. Populate first log.
        if (found_I == -1)
        {
            output = $"READ {s_LogList[0]}";
        }
        else if (s_LogList[found_I] == words[1])
        {
            //Complete next log in cycle
            output = $"READ {s_LogList[(found_I + 1) % s_LogList.Count]}";
        }
        else
        {
            output = $"READ {s_LogList[found_I]}";
        }
        return true;

    }

    private static bool TryCompletePingQuery(string[] words, ref string output)
    {
        bool autoCompletePerformed = false;

        //Iterate through all arguments, and expand each argument that can be expanded based on the List
        for (int i = 1; i < words.Length; i++)
        {
            var word = words[i];
            iTerminalItem? completion = s_FoundItems.Find(item => item.TerminalItemKey.StartsWith(word) && !item.WasCollected);
            if (completion != null)
            {
                autoCompletePerformed = word.Length != completion.TerminalItemKey.Length; //If the word has the same length as the completion, then the completion did nothing
                words[i] = completion.TerminalItemKey;
            }
        }

        //If an autocomplete operation was performed in the previous step, create the new command line and return
        if (autoCompletePerformed)
        {
            output = string.Join(" ", words);
            return true;
        }
        else
        {
            //No auto complete was performed, so check the final word in the arg list to see whether it can be cycled to a similar item.
            //Example: List contains MEDIPACK_123 and MEDIPACK_23, so pressing Tab should swap between the two.
            string finalArg = words[^1];
            int completionIndex = s_FoundItems.FindIndex(item => item.TerminalItemKey == finalArg && !item.WasCollected);
            if (completionIndex < 0) { return false; }

            int underscoreindex = finalArg.LastIndexOf('_');
            if (underscoreindex < 0) { underscoreindex = finalArg.Length - 1; }
            string baseWord = finalArg[..underscoreindex];

            if (finalArg.StartsWith("KEY")) baseWord = "KEY";

            for (int i = (completionIndex + 1) % s_FoundItems.Count; i != completionIndex; i = (i + 1) % s_FoundItems.Count)
            {
                if (s_FoundItems[i].TerminalItemKey.StartsWith(baseWord))
                {
                    words[^1] = s_FoundItems[i].TerminalItemKey;
                    output = string.Join(" ", words);
                    return true;
                }
            }
        }
        return false;
    }


    private static bool ItemMatchesFilter(iTerminalItem item, string filter, LG_ComputerTerminalCommandInterpreter command)
    {
        bool invertResult = false;
        if (filter[0] == '^')
        {
            filter = filter[1..];
            invertResult = true;
        }
        bool pass = item.TerminalItemKey.Contains(filter)
            || item.FloorItemLocation.Contains(filter)
            || command.GetLocalizedFloorItemType(item.FloorItemType).Contains(filter, StringComparison.OrdinalIgnoreCase)
            || command.GetLocalizedFloorItemStatus(item.FloorItemStatus).Contains(filter, StringComparison.OrdinalIgnoreCase);
        return invertResult ? !pass : pass;
    }

    //This is a critical function that generates a list of matching items which is used for Asterix and Tab completion
    private static void RunList(LG_ComputerTerminal term)
    {
        s_FoundItems.Clear();
        if (m_args.Length == 1)
        {
            term.m_command.EvaluateInput("LIST");
            return;
        }

        bool sortList = false;
        bool showAll = false;
        List<string> args = new();
        LG_ComputerTerminalManager.WantToSendTerminalCommand(term.SyncID, (TERM_Command)TERM_Command.MAX_COUNT + 1, m_command, "", "");


        //Replace -Z args with the ZONE_XXX string of the current terminal
        for (int i = 1; i < m_args.Length; i++)
        {
            if (m_args[i].StartsWith("-"))
            {
                foreach (var c in m_args[i])
                {
                    switch (c)
                    {
                        case 'Z':
                            args.Add(term.m_terminalItem.FloorItemLocation);
                            break;
                        case 'D':
                            args.Add("^DOOR");
                            break;
                        case 'C':
                            args.Add("^LOCKER");
                            args.Add("^BOX");
                            break;
                        case 'S':
                            sortList = true;
                            break;
                        case 'A':
                            showAll = true;
                            break;
                    }
                }
            }
            else if (int.TryParse(m_args[i], out int result))
            {
                //Convert bare numbers into ZONE tags
                args.Add($"ZONE_{result}");
            }
            else
            {
                args.Add(m_args[i]);
            }
        }
        Il2CppSystem.Collections.Generic.List<string> itemList = new();

        //Search through all items and add the ones matching the dimmension and filter strings to the Internal List
        foreach (var a in LG_LevelInteractionManager.GetAllTerminalInterfaces())
        {
            if (term.SpawnNode?.m_dimension?.DimensionIndex != a.value.SpawnNode?.m_dimension?.DimensionIndex)
            {
                continue;
            }

            bool allPass = true;
            if (!showAll)
            {
                foreach (var arg in args)
                {
                    allPass &= ItemMatchesFilter(a.value, arg, term.m_command);
                }
            }
            //bool skipDimmensionCheck = a.value.SpawnNode == null;
            if (showAll || allPass || args.Count == 0)
            {
                s_FoundItems.Add(a.value);
                itemList.Add(string.Format("{0,-35}{1,-35}{2}",
                    a.value.TerminalItemKey,
                    a.value.FloorItemType,
                    a.value.FloorItemStatus)
                );
            }
        }
        if (sortList)
        {
            itemList.Sort();
        }
        term.m_command.AddOutput(TerminalLineType.ProgressWait, $"Listing items using filter {string.Join(", ", m_args, 1, m_args.Length - 1)}", 1.5f);
        term.m_command.AddOutput(string.Format("{0,-35}{1,-35}{2}", "ID", "ITEM TYPE", "STATUS"));
        term.m_command.AddOutput(itemList);
        //term.m_command.AddOutput("ORIGINAL");
        //term.m_command.EvaluateInput(string.Join(" ", m_args));
        //Add original List command to output buffer
        AddToHistory(term.m_command);

    }

    //Adds the current command to the in game terminal history. Press Up or Down arrows to navigate previous commands.
    private static void AddToHistory(LG_ComputerTerminalCommandInterpreter command)
    {
        command.m_inputBuffer.Add(m_command);
        command.m_inputBufferStep = 0;
    }

    private static void RunBulkQuery(LG_ComputerTerminal term, string arg)
    {
        string trimmed = arg.Trim('*');
        LG_ComputerTerminalManager.WantToSendTerminalCommand(term.SyncID, TERM_Command.MAX_COUNT + 1, m_command, "", "");
        Il2CppSystem.Collections.Generic.List<string> AllDetails = new();

        int foundCount = 1;
        foreach (var item in s_FoundItems)
        {
            if (item.TerminalItemKey.StartsWith(trimmed))
            {
                foundCount++;
                var details = item.GetDetailedInfo(m_genericListDefaults);
                //AllDetails.Add($"\n----ENTITY: {item.TerminalItemKey}----------");
                for (int d = 0; d < details.Count; d++)
                {
                    AllDetails.Add(details[d]);
                }
                AllDetails.Add(
                    string.Concat("----------------------------------------------------------------",
                     $"\nID: {item.TerminalItemKey}\nITEM STATUS: {term.m_command.GetLocalizedFloorItemStatus(item.FloorItemStatus)}\nLOCATION: {item.FloorItemLocation}\n"));
            }
        }

        AddToHistory(term.m_command);
        term.m_command.AddOutput(TerminalLineType.ProgressWait, $"Batch Query On: {arg}", m_bulkDelayPerEntry * foundCount);
        term.m_command.AddOutput(AllDetails);

    }

    private static void RunQuery(LG_ComputerTerminal term)
    {
        if (m_args.Length == 1)
        {
            term.m_command.EvaluateInput("QUERY");
            return;
        }
        for (int i = 1; i < m_args.Length; i++)
        {
            var arg = m_args[i];

            //If the current argument has an asterix, run {command} with each item from the List that starts with the prefix.
            if (arg.Last<char>() == '*')
            {
                if (m_enableBulkQuery)
                {
                    RunBulkQuery(term, arg);
                }
                else
                {
                    arg = arg.Trim('*');
                    foreach (var item in s_FoundItems)
                    {
                        if (item.TerminalItemKey.StartsWith(arg))
                            term.m_command.EvaluateInput($"QUERY {item.TerminalItemKey}");
                    }
                }

            }
            else
            {
                term.m_command.EvaluateInput($"QUERY {arg}");
            }

        }
    }

    private static void RunPing(LG_ComputerTerminal term)
    {
        if (m_args.Length == 1)
        {
            term.m_command.EvaluateInput("PING");
            return;
        }

        for (int i = 1; i < m_args.Length; i++)
        {
            var arg = m_args[i];

            //If the current argument has an asterix, run {command} with each item from the List that starts with the prefix.
            if (arg.Last<char>() == '*')
            {
                arg = arg.Trim('*');
                foreach (var item in s_FoundItems)
                {
                    if (item.TerminalItemKey.StartsWith(arg))
                        term.m_command.EvaluateInput($"PING {item.TerminalItemKey}");
                }
            }
            else
            {
                term.m_command.EvaluateInput($"PING {arg}");
            }
        }

    }

    private static void RunLogList(LG_ComputerTerminal term)
    {
        s_LogList.Clear();
        //s_LogList.Add("EXAMPLE_LOG_1.LOG");
        //s_LogList.Add("EXAMPLE_LOG_2.LOG");
        foreach (var pair in term.GetLocalLogs())
        {
            s_LogList.Add(pair.key);
        }

        term.m_command.EvaluateInput(m_command);

    }

    private static void RunReadLog(LG_ComputerTerminal term)
    {
        if (m_args.Length == 2)
        {
            var arg = m_args[1];
            if (arg.Last<char>() == '*')
            {
                arg = arg.Trim('*');
            }
            var logName = s_LogList.Find(log => log.Contains(arg));
            term.m_command.EvaluateInput($"READ {logName}");
        }
        else
        {
            term.m_command.EvaluateInput(m_command);
        }
    }

}
