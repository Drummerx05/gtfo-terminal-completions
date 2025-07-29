using Il2CppSystem.Data;
using LevelGeneration;
using PlayFab.ClientModels;
using SickDev.CommandSystem;

namespace TerminalCompletion.Plugin;

internal class CommandExecutor
{
    private readonly static List<iTerminalItem> s_FoundItems = new();
    private readonly static List<string> s_LogList = new();

    private static string m_command = "";
    private static string[] m_args = Array.Empty<string>();
    //private static TERM_Command m_command = TERM_Command.None;


    public CommandExecutor(string command)
    {

    }

    public static void ExecCommand(string command, LG_ComputerTerminal term)
    {
        m_command = command;
        m_args = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        //m_command = TERM_Command.None;

        if (m_args.Length > 0)
        {
            switch (m_args[0])
            {
                case "LIST":
                case "LS":
                    //m_command = TERM_Command.ShowList;
                    RunList(term);
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
                default:
                    //m_command = TERM_Command.None;
                    term.m_command.EvaluateInput(m_command);
                    break;
            }
        }

        term.m_currentLine = "";
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
        if(found_I == -1) { 
            output = $"READ {s_LogList[0]}";
        }
        else
        {
            //Complete next log in cycle
            output = $"READ {s_LogList[(found_I + 1) % s_LogList.Count]}";
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
            iTerminalItem? completion = s_FoundItems.Find(item => item.TerminalItemKey.StartsWith(word));
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
            int completionIndex = s_FoundItems.FindIndex(item => item.TerminalItemKey == finalArg);
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


    private static bool ItemMatchesFilter(iTerminalItem item, string filter)
    {
        return item.TerminalItemKey.Contains(filter)
            || item.FloorItemLocation.Contains(filter)
            || item.FloorItemType.ToString().ToUpper().Contains(filter);
    }

    //This is a critical function that generates a list of matching items which is used for Asterix and Tab completion
    private static void RunList(LG_ComputerTerminal term)
    {
        s_FoundItems.Clear();
        if (m_args.Length > 1)
        {
            //Replace -Z args with the ZONE_XXX string of the current terminal
            for (int i = 1; i < m_args.Length; i++)
            {
                if (m_args[i] == "-Z")
                {
                    m_args[i] = term.m_terminalItem.FloorItemLocation;
                }
                else if (int.TryParse(m_args[i], out int result))
                {
                    //Convert bare numbers into ZONE tags
                    m_args[i] = $"ZONE_{result}";
                }
            }

            //Search through all items and add the ones matching the dimmension and filter strings to the Internal List
            foreach (var a in LG_LevelInteractionManager.GetAllTerminalInterfaces())
            {
                if (term.SpawnNode.m_dimension.DimensionIndex == a.value.SpawnNode.m_dimension.DimensionIndex &&
                     ItemMatchesFilter(a.value, m_args[1]) && (m_args.Length == 2 || ItemMatchesFilter(a.value, m_args[2])))
                {
                    //TerminalPlugin.Logger.LogInfo(string.Concat(
                    //    a.value.TerminalItemKey, " ",
                    //    a.value.FloorItemType, " ",
                    //    a.value.FloorItemStatus, " ",
                    //    a.value.FloorItemLocation, " ",
                    //    a.value.WasCollected, " ",
                    //    a.value.SpawnNode.m_dimension.DimensionIndex));
                    s_FoundItems.Add(a.value);
                }
            }
        }
        m_args[0] = "LIST";
        term.m_command.EvaluateInput(string.Join(" ", m_args));
    }

    private static void RunQuery(LG_ComputerTerminal term)
    {
        RunGenericExpandedCommand(term, "QUERY");
    }

    private static void RunPing(LG_ComputerTerminal term)
    {
        RunGenericExpandedCommand(term, "PING");
    }

    private static void RunGenericExpandedCommand(LG_ComputerTerminal term, string command)
    {
        if (m_args.Length >= 2)
        {
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
                            term.m_command.EvaluateInput($"{command} {item.TerminalItemKey}");
                    }
                }
                else
                {
                    term.m_command.EvaluateInput($"{command} {arg}");
                }
            }
        }
    }

    private static void RunLogList(LG_ComputerTerminal term)
    {
        s_LogList.Clear();
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
