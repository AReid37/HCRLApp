﻿using System.Diagnostics;

namespace HCResourceLibraryApp
{
    /// <summary>
    ///     An intermediary, more focused version of the Debug Logger to Output.
    /// </summary>
    public static class Dbug
    {
        // IDEA -- Give Dbug.cs file-writing ability so that debug's can be logged and viewed outside of IDE
        //          ^^ only if a file does not exist with this functionality
        static bool relayDbugLogs;
        static string sessionName;
        static string partialLog;

        public static void StartLogging()
        {
            relayDbugLogs = true;
            SetIndent(-1);
            Debug.WriteLine("\n--------------------"); // 20x '-'
            SetIndent(0);
        }
        public static void StartLogging(string logSessionName)
        {
            relayDbugLogs = true;
            SetIndent(-1);
            Debug.WriteLine("\n--------------------"); // 20x '-'
            if (logSessionName.IsNotNEW())
            {
                sessionName = logSessionName;
                Debug.WriteLine($"## {logSessionName.ToUpper()} ##");
            }
            SetIndent(0);
        }
        public static void EndLogging()
        {
            SetIndent(-1);
            if (sessionName.IsNotNEW() && relayDbugLogs)
                Debug.WriteLine($"## END :: {sessionName.ToUpper()} ##");

            relayDbugLogs = false;
            sessionName = null;
        }

        public static void SingleLog(string logSessionName, string log)
        {
            SetIndent(0);
            Debug.WriteLine($"Sg| @{logSessionName} :: {log}");
            SetIndent(-1);
        }

        public static void SetIndent(int level)
        {
            // start and end logs are always on level 0. All other text are level 1 and above
            if (Debug.IndentSize != 4)
                Debug.IndentSize = 4;

            if (level >= -1)
                Debug.IndentLevel = level + 1;
        }
        ///<summary>Stores partial logs that will be flushed together with the new logged line after using <see cref="Log(string)"/>.</summary>
        public static void LogPart(string log)
        {
            if (relayDbugLogs)
            {
                if (log.IsNotNEW())
                    if (partialLog == null)
                        partialLog = log;
                    else partialLog += log;
            }
        }
        public static void Log(string log)
        {
            if (log.IsNotNEW() && relayDbugLogs)
            {
                if (partialLog.IsNotNEW())
                    log = partialLog + log;
                
                partialLog = null;
                Debug.WriteLine($"{log}");
            }
        }
    }
}
