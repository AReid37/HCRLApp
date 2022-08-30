using System.Diagnostics;
using System.Collections.Generic;
using HCResourceLibraryApp.DataHandling;
using static ConsoleFormat.Base;

namespace HCResourceLibraryApp
{
    /// <summary>
    ///     An intermediary, more focused version of the Debug Logger to Output.
    /// </summary>
    public static class Dbug
    {
        // IDEA -- Give Dbug.cs file-writing ability so that debug's can be logged and viewed outside of IDE
        //          ^^ only if a file does not exist with this functionality ... Checked >> FILE DOES NOT EXIST
        const int indentSize = 4, nestLimitNum = 2;
        static bool relayDbugLogs, ongoingNestedLogsQ, ignoreNextSessionQ;
        static string sessionName, nestedSessionName;
        static string partialLog;
        static int countSessions = 0, nestedBaseIndentLevel = 0;

        // fileSaving
        static Stopwatch dbugWatch;
        static bool firstFlushOfSessionQ = true;
        static int flushIndentLevel = 0;
        static List<string> logSessionFlush = new List<string>();
        const string DbugLogTag = "Dbg| ", NestedLogTag = ">|  ", StartNestedTag = "``\n", EndNestedTag = "`\n";
        const string FileName = "hcrla-dbugFlush.txt";
        const string FileSaveLocation = DataHandlerBase.FileDirectory + FileName;

        public static void StartLogging()
        {            
            countSessions++;
            ongoingNestedLogsQ = countSessions > 1;

            if (IsWithinNestedLimit())
            {
                if (!ignoreNextSessionQ)
                    relayDbugLogs = true;
                else ignoreNextSessionQ = false;

                if (!ongoingNestedLogsQ)
                {
                    SetIndent(-1);
                    if (relayDbugLogs)
                        Debug.WriteLine("\n--------------------"); // 20x '-'
                    AddToLogFlusher("\n--------------------");
                    SetIndent(0);
                }
                else
                {
                    NudgeIndent(true);
                    nestedBaseIndentLevel = Debug.IndentLevel;
                    if (relayDbugLogs)
                    {
                        Debug.WriteLine(StartNestedTag);
                        Debug.WriteLine(">> -------------------- <<");
                    }
                    AddToLogFlusher(StartNestedTag);
                    AddToLogFlusher(">> -------------------- <<");
                }
            }            
        }
        public static void StartLogging(string logSessionName)
        {            
            countSessions++;
            ongoingNestedLogsQ = countSessions > 1;

            if (IsWithinNestedLimit())
            {
                if (!ignoreNextSessionQ)
                    relayDbugLogs = true;
                else ignoreNextSessionQ = false;

                if (!ongoingNestedLogsQ)
                {
                    SetIndent(-1);
                    if (relayDbugLogs)
                        Debug.WriteLine("\n--------------------"); // 20x '-'
                    AddToLogFlusher("\n--------------------");
                    if (logSessionName.IsNotNEW())
                    {
                        sessionName = logSessionName;
                        if (relayDbugLogs)
                            Debug.WriteLine($"## {logSessionName.ToUpper()} ##");
                        AddToLogFlusher($"## {logSessionName.ToUpper()} ##");
                    }
                    SetIndent(0);
                }
                else
                {
                    NudgeIndent(true);
                    nestedBaseIndentLevel = Debug.IndentLevel;
                    if (relayDbugLogs)
                    {
                        Debug.WriteLine(StartNestedTag);
                        Debug.WriteLine(">> -------------------- <<");
                    }
                    AddToLogFlusher(StartNestedTag);
                    AddToLogFlusher(">> -------------------- <<");

                    if (logSessionName.IsNotNEW())
                    {
                        nestedSessionName = logSessionName;
                        if (relayDbugLogs)
                            Debug.WriteLine($"## {logSessionName.ToUpper()} ##");
                        AddToLogFlusher($"## {logSessionName.ToUpper()} ##");
                    }
                }
            }
        }
        public static void EndLogging()
        {
            if (!ongoingNestedLogsQ)
            {
                SetIndent(-1);
                if (sessionName.IsNotNEW() && relayDbugLogs)
                    Debug.WriteLine($"## END :: {sessionName.ToUpper()} ##");
                AddToLogFlusher($"## END :: {(sessionName.IsNotNEW() ? sessionName : "...")} ##");
                NoteRunTime();

                FlushAndSave();
                relayDbugLogs = false;
                sessionName = null;
            }
            else
            {
                if (nestedSessionName.IsNotNEW() && relayDbugLogs)
                {
                    Debug.WriteLine($"## END :: {nestedSessionName.ToUpper()} ##");
                    Debug.WriteLine(EndNestedTag);
                }
                AddToLogFlusher($"## END :: {(nestedSessionName.IsNotNEW() ? nestedSessionName : "...")} ##");
                AddToLogFlusher(EndNestedTag);
                NoteRunTime();
                NudgeIndent(false);

                ongoingNestedLogsQ = false;
                nestedSessionName = null;
            }

            if (!IsWithinNestedLimit())
                countSessions = nestLimitNum;

            if (countSessions >= 1)
                countSessions--;
        }
        /// <summary>Place before any <see cref="StartLogging"/> to skip logging this session into the Debug Ouput window.</summary>
        public static void IgnoreNextLogSession()
        {
            ignoreNextSessionQ = true;
        }

        public static void SingleLog(string logSessionName, string log)
        {
            //SetIndent(0);
            Debug.WriteLine($"Sg| @{logSessionName} :: {log}");
            AddToLogFlusher($"Sg| @{logSessionName} :: {log}");
            NoteRunTime();
            //SetIndent(-1);
        }
        
        /// <summary>Increments or decrement the indentation level based on <paramref name="isIncrement"/>'s value.</summary>
        public static void NudgeIndent(bool isIncrement)
        {
            if (IsWithinNestedLimit())
            {
                int trueLevel = Debug.IndentLevel - 1;

                // if (decrement) // if (increment)
                if (!isIncrement && trueLevel > 0)
                    trueLevel -= 1;
                if (isIncrement)
                    trueLevel += 1;

                SetIndent(trueLevel);
            }
        }
        ///<summary>Stores partial logs that will be flushed together with the new logged line after using <see cref="Log(string)"/>.</summary>
        public static void LogPart(string log)
        {
            if (IsWithinNestedLimit())
            {
                if (log.IsNotNEW())
                    if (partialLog == null)
                        partialLog = log;
                    else partialLog += log;
            }
        }
        public static void Log(string log)
        {
            if (IsWithinNestedLimit())
            {
                if (log.IsNotNEW())
                {
                    if (partialLog.IsNotNEW())
                        log = partialLog + log;
                    partialLog = null;

                    if (ongoingNestedLogsQ)
                    {
                        int currIndentLvl = Debug.IndentLevel;
                        Debug.IndentLevel = nestedBaseIndentLevel;
                        flushIndentLevel = nestedBaseIndentLevel;

                        string spaceTexts = "";
                        int inds = currIndentLvl - nestedBaseIndentLevel;

                        for (int i = 0; i < inds; i++)
                            spaceTexts += $"{Ind24}";
                        log = $"{NestedLogTag}{spaceTexts}{log}";

                        if (relayDbugLogs)
                            Debug.WriteLine($"{log}");
                        AddToLogFlusher($"{log}");

                        Debug.IndentLevel = currIndentLvl;
                        flushIndentLevel = currIndentLvl;
                    }
                    else
                    {
                        if (relayDbugLogs)
                            Debug.WriteLine($"{log}");
                        AddToLogFlusher($"{log}");
                    }

                    //if (ongoingNestedLogsQ)
                    //    log = NestedLogTag + log;


                }
            }
        }

        static void SetIndent(int level)
        {
            // start and end logs are always on level 0. All other text are level 1 and above
            if (Debug.IndentSize != 4)
                Debug.IndentSize = indentSize;

            if (level >= -1)
            {
                Debug.IndentLevel = level + 1;
                flushIndentLevel = level + 1;
            }
        }
        static bool IsWithinNestedLimit()
        {
            return countSessions <= nestLimitNum;
        }        

        // File saving
        static void FlushAndSave()
        {
            if (logSessionFlush.HasElements())
            {
                /// makes debug saving directory 'invisible' (not interferring with other save locations)
                string prevDir = Directory, prevFileNAT = FileNameAndType;
                if (SetFileLocation(FileSaveLocation))
                {
                    // first entry
                    bool overwrite = false;
                    if (firstFlushOfSessionQ)
                    {
                        overwrite = true;
                        firstFlushOfSessionQ = false;
                        logSessionFlush.Insert(0, $"{DbugLogTag}Dbug File Saving started // 'Runtime Note' watch (Rtn) started, @[0h 0m 00s]");
                        dbugWatch = Stopwatch.StartNew();
                    }
                    FileWrite(overwrite, null, logSessionFlush.ToArray());
                    SetFileLocation(prevDir, prevFileNAT);
                }
            }
            logSessionFlush = new List<string>();
        }
        static void AddToLogFlusher(string log)
        {
            if (log.IsNotNEW())
            {
                string indents = "";
                for (int indIx = 0; indIx < flushIndentLevel; indIx++)
                    indents += "\t";
                logSessionFlush.Add(indents + log);
            }
        }
        static void NoteRunTime()
        {
            if (dbugWatch != null && IsWithinNestedLimit())
            {
                string timeStamp = $"{(int)dbugWatch.Elapsed.TotalHours}h {(int)dbugWatch.Elapsed.TotalMinutes % 60:0}m {(int)dbugWatch.Elapsed.TotalSeconds % 60:00}s";
                //logSessionFlush.Add($"{DbugLogTag}Rtn: {timeStamp}");
                AddToLogFlusher($"{DbugLogTag}Rtn: {timeStamp}");
            }
        }
    }
}
