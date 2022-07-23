using System.Diagnostics;

namespace HCResourceLibraryApp
{
    /// <summary>
    ///     An intermediary, more focused version of the Debug Logger to Output.
    /// </summary>
    public static class Dbug
    {
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
            if (logSessionName.HasValue())
            {
                sessionName = logSessionName;
                Debug.WriteLine($"## {logSessionName.ToUpper()} ##");
            }
            SetIndent(0);
        }
        public static void EndLogging()
        {
            SetIndent(-1);
            if (sessionName.HasValue() && relayDbugLogs)
                Debug.WriteLine($"## END :: {sessionName.ToUpper()} ##");

            relayDbugLogs = false;
            sessionName = null;
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
            if (log.HasValue())
                if (partialLog == null)
                    partialLog = log;
                else partialLog += log;
        }
        public static void Log(string log)
        {
            if (log.HasValue())
            {
                if (partialLog.HasValue())
                    log = partialLog + log;
                
                partialLog = null;
                Debug.WriteLine($"{log}");
            }
        }
    }
}
