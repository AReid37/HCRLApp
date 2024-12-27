using HCResourceLibraryApp.DataHandling;
using System.Diagnostics;
using System.Collections.Generic;
using static ConsoleFormat.Base;

namespace HCResourceLibraryApp
{
    /// <summary>An upgraded version of the formerly used <see cref="Dbug"/> class. Uses multi-thread-like functionality to allow uninterrupted logging from any session threads.</summary>
    public static class Dbg
    {
        /** THE REVISION PLAN **\
         * 
         *  +++++++
         *  The issues with Dbug.cs
         *      - Log sessions have a limited concurrent number, 
         *          ^ Any more than 2 log session will ignore the others
         *          ^ Results in a loss of debuging information
         *      - The ability to deactivate log sessions is counter-productive
         *          ^ Results in a loss of debugging information
         *         
         *         
         *  +++++++
         *  The resolution: log session threads system.
         *      - Each log session identified will be assigned a 'thread'.
         *      - A thread consists of these elements in parallel: 
         *          ^ An identifier, the name of the session (no silly ID numbers)
         *          ^ An index number, to coordinate all parallel lists for thread
         *          ^ A designated string list, stores its logging information
         *          ^ A string to store a suspended log (logs not resolved, as in 'partial logging')
         *          ^ Another number, it notes the indent level of the session
         *          ^ A boolean to verify activity of the thread (verify start/end logs)
         *          ^ Another number, simply counts the number of times the thread was activated
         *      - Re: Per thread: (str 'name', int 'index', list{str} 'logs', str 'partial', short 'indentLevel', bl 'active', int 'count')
         *      - All thread elements are placed in another list. So there is a list of names, indexes, logs, partials, indents, activity, and counts. All thread element lists are expanded when a new session is identified.
         *      - With this many elements, they all should be placed in a class/struct named 'DbgThread'.
         *      
         *  ~~~~   
         *  How these elements will work
         *      - StartLogging(str 'name', out int 'logIx')
         *          ^ When a logging session starts, the name of the session will be checked against the 'list of names'. 
         *              * If it doesn't exist in names list, a new thread will be built on the session name, assigning it an index spot across all thread elements (names, indexes, logs, etc.). 
         *              * If it does exist in names list, then it's activity will be noted and and is ready to receive oncoming logs.
         *          ^ In any case, once the session is given an index, the index value will be returned through the method as 'logIx' for further logging
         *          ^ When logging session begins, a session header is added to the thread's logs list, and the start time of the thread session will be noted
         *      - Log(int 'logIx', str 'log')       or     LogPart(int 'logIx', 'log')      or      SingleLog(str 'name', str 'log')
         *          ^ There are three different ways to log:
         *              * "Log" : during a log session. Uses the 'logIx' provided to store a completed log string to the thread. Will finalize a 'partial' log.
         *              * "LogPart" : during a log session. Uses the 'logIx' provided to store a partially completed string to the thread. Stored as a 'partial' log, not yet added to logs list.
         *              * "SingleLog" : A stand alone debug log that does not require being in a session. The session (if new) is identified and assigned a threading space and its start time is noted.
         *                  ^ Note: A single log is simultaneously a start log, log, and end log. Meaning it has to handle the functionality of many different methods. Prints to file immediately.
         *      - NudgeIndent (int 'logIx', bl 'isIncrement')
         *          ^ During log sessions, the output logs can be indented to signify portions of long sessions. This adjusts the indent level of the specific thread.
         *      - EndLogging(int 'logIx')
         *          ^ At the end of a logging session, a session footer (or horizontal rule) is added to thread's logs list, and the end time of the thread session is noted
         *          ^ When a session ends, the thread is considered inactive. The thread values are reset and the logs of the session thread are printed out to the debug file and output.
         *          
         *      - Note Worthy
         *          ^ If another log session begins while any others are active. The active thread will add a log line denoting the session name within its own logs list, but ignores everything else
         *              * In this way, the separate sessions can be acknowledged without any major interuption. The thread can be viewed separately with knowledge of where and when it occured
         *              * This does not apply to Single Logs. Only full session logs with a start and end.
         *          ^ At the bottom of the debug log file (after program close), all identified threads are listed with their name, index, and count. Just an FYI. 
         *          
         *          
         *          
         *          - Added another thread element, 'outputOmission': logs aren't printed to output, but will still be printed to file.
         *          - Added another thread element, 'interruptedQ': relays whethers a thread is being interrupted (with notice from openning threads)
         *          
        \** ****************** */

        #region fields/props
        // file saving
        const string FileName = "hcrlaDbgFlush.txt"; /// different from old 'hcrla-dbugFlush.txt'
        static readonly string FileSaveLocation = DataHandlerBase.AppDirectory + FileName;

        // threading and misc info
        static List<DbgThread> Threads;
        static List<string> ThreadSpamsKeywordList;
        static Stopwatch _runtimeWatch;
        static bool _firstFlushHandledQ, _finalFlushHandledQ;

        internal const int indentFactor = 4;
        const int noIndex = -1, spamThreadKeywordMinimum = 4;
        const string _keyName = "{tnm}", _keyTime = "{rtn}", _keyLog = "{dlg}", _keyActiveC = "{cat}", _keyThreadCount = "{thc}", _keyThrdNumSym = "'";
        const string _keyLineNoticeHeaderDiv = ". . . . .";
        const char _keyLineStartHeaderDiv = ':';
        // [hdr]    #### {name} '{onCount} {time} ####
        /// <summary>Needs: <see cref="_keyName"/>, <see cref="_keyThreadCount"/>.</summary>
        static readonly string _keyLineStartLog = $"#### {_keyName} {_keyThrdNumSym}{_keyThreadCount} {_keyTime} ####";
        //          ## END {name} '{onCount} {time} ## \n
        /// <summary>Needs: <see cref="_keyName"/>, <see cref="_keyThreadCount"/>.</summary>
        static readonly string _keyLineEndLog = $"## END {_keyName} {_keyThrdNumSym}{_keyThreadCount} {_keyTime} ##\n";
        //          ## SNG {name} {time}  //  {log} ##
        /// <summary>Needs: <see cref="_keyName"/> and <see cref="_keyLog"/>.</summary>
        static readonly string _keyLineSingleLog = $"## SNG {_keyName} {_keyTime}  //  {_keyLog} ##";
        // [hdr]    ## OPN {name} '{onCount} {time} [{count}opn] ##     [ftr]
        /// <summary>Needs: <see cref="_keyName"/>, <see cref="_keyThreadCount"/>.</summary>
        static readonly string _keyLineNoticeLog = $"## OPN {_keyName} {_keyThrdNumSym}{_keyThreadCount} {_keyTime} [{_keyActiveC}opn] ##";  
        #endregion



        #region methods
        /// <summary>
        ///    Initializes thread list and starts the runtime stopwatch.
        /// </summary>
        public static void Initialize()
        {
            Threads = new List<DbgThread>();
            ThreadSpamsKeywordList = new List<string>();
            _runtimeWatch = new Stopwatch();
            _runtimeWatch.Start();
        }
        /// <summary> Forcefully ends any active threads and lists all threads that have been generated.</summary>
        /// <remarks>To be used moments before exiting application.</remarks>
        public static void ShutDown()
        {
            if (!_finalFlushHandledQ)
            {
                StartLogging("<DbgShutDown>", out int tx);
                Log(tx, "All threads generated in runtime order.");
                NudgeIndent(tx, true);

                if (Threads.HasElements())
                {
                    foreach (DbgThread thread in Threads)
                    {
                        if (thread.IsSetup() && thread.Index != tx)
                        {
                            if (thread.IsActiveQ)
                            {
                                thread.Indent = 1;
                                thread.AddToLog($"** THIS THREAD HAS BEEN FORCED TO END **");
                                EndLogging(thread.Index);
                            }
                            LogPart(tx, $"> Index [{thread.Index}], Times Active [{thread.Count}]  // Name :: {thread.Name}");
                            if (IsMarkedAsSpam(thread.Index))
                                LogPart(tx, "   ---   {MARKED_AS_SPAM}");
                            Log(tx, "; ");
                        }
                    }
                }
                Log(tx, "Spam Keywords ::  " + string.Join("  ", ThreadSpamsKeywordList));
                EndLogging(tx);
                _finalFlushHandledQ = true;
            }
        }


        // MAIN METHODS
        /// <summary>
        ///     Finds or generates a new thread instance to start a logging session. 
        /// </summary>
        /// <param name="name">Required. The name of the session thread to activate.</param>
        /// <param name="threadIx">The index of this session's thread. Used in other logging methods.</param>
        /// <remarks>Only assigns <paramref name="threadIx"/> if thread is already generated and active.</remarks>
        public static void StartLogging(string name, out int threadIx)
        {
            threadIx = noIndex;
            if (name.IsNotNEW())
            {
                // fetch or generate thread
                DbgThread currThread = FindThread(name);
                currThread ??= GenerateThread(name);


                // thread session begin
                if (currThread is not null)
                {
                    /// assign thread index
                    threadIx = currThread.Index;
                    
                    if (!currThread.IsActiveQ)
                    {
                        /// start-up thread
                        currThread.IsActiveQ = true;

                        /// add thread header
                        string headerDiv = "", headerText = GenerateKeyLine(_keyLineStartLog, currThread.Name.ToUpper(), null, currThread.Count);
                        for (int hx = 0; hx < headerText.Length; hx++)
                            headerDiv += _keyLineStartHeaderDiv;
                        currThread.AddToLog($"\n{headerDiv}");
                        currThread.AddToLog(headerText);
                        currThread.Indent = 1;

                        /// check for any active threads, log this thread's startup as notice
                        DbgThread[] activeThreads = FetchActiveThreads();
                        if (activeThreads.HasElements())
                        {
                            foreach (DbgThread activeThread in activeThreads)
                            {
                                if (activeThread.Name != currThread.Name && !IsMarkedAsSpam(currThread.Index))
                                {
                                    /// interruption opens on other logs that are active
                                    if (!activeThread.IsInterruptedQ)
                                    {
                                        /// separted, so other thread indents are maintained
                                        activeThread.AddToLog("\t");
                                        activeThread.AddToLog(_keyLineNoticeHeaderDiv);
                                        activeThread.IsInterruptedQ = true;
                                    }
                                    activeThread.AddToLog(GenerateKeyLine(_keyLineNoticeLog, currThread.Name, null, currThread.Count));
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        ///     Finds a thread instance to end its logging session. Includes printing to output, saving to file, and resetting thread.
        /// </summary>
        /// <param name="threadIx">Index of thread to end logging.</param>
        /// <remarks>Does nothing if the thread is already deactivated and reset.</remarks>
        public static void EndLogging(int threadIx)
        {
            if (threadIx != noIndex)
            {
                DbgThread currThread = FindThread(threadIx);
                if (currThread is not null)
                {
                    if (currThread.IsActiveQ)
                    {
                        /// interuption closes when the active session ends
                        if (currThread.IsInterruptedQ)
                        {
                            currThread.AddToLog(_keyLineNoticeHeaderDiv);
                            currThread.IsInterruptedQ = false;
                        }
                        if (currThread.Partial.IsNotNEW())
                            currThread.AddToLog(".");                        
                        currThread.Indent = 0;
                        currThread.AddToLog(GenerateKeyLine(_keyLineEndLog, currThread.Name, null, currThread.Count));
                        PrintAndSaveThread(threadIx);
                        ResetThread(threadIx);
                    }
                }
            }
        }
        /// <summary>
        ///     Finds a thread instance to add a log to the active session.
        /// </summary>
        /// <param name="threadIx">Index of the thread to which a log is added.</param>
        /// <param name="log">Required. The log line.</param>
        public static void Log(int threadIx, string log)
        {
            DbgThread currThread = FindThread(threadIx);
            if (currThread is not null && log.IsNotNEW())
            {
                if (currThread.IsActiveQ)
                {
                    /// interuption closes on a full log when active
                    if (currThread.IsInterruptedQ)
                    {
                        currThread.AddToLog(_keyLineNoticeHeaderDiv + "\n");
                        currThread.IsInterruptedQ = false;
                    }
                    currThread.AddToLog(log);
                }
            }
        }
        /// <summary>
        ///     Finds a thread instance to add a suspended log to the active session.
        /// </summary>
        /// <param name="threadIx">Index of the thread to which a partial log is added.</param>
        /// <param name="log">Required. The partial log line.</param>
        public static void LogPart(int threadIx, string log)
        {
            DbgThread currThread = FindThread(threadIx);
            if (currThread is not null && log.IsNotNEW())
            {
                if (currThread.IsActiveQ)
                    currThread.AddToLog(log, true);
            }
        }
        /// <summary>
        ///     Finds or generates a new thread instance to log, print, and save a singular log line.
        /// </summary>
        /// <remarks>Has fail-safes in the rare event that the thread is already active.</remarks>
        /// <param name="name">The name of the session thread to singularly log.</param>
        /// <param name="log">Required. The singular log line.</param>
        /// <param name="omitFromOutputQ">If <c>true</c>, will not print the single log to the output (still prints to save file).</param>
        public static void SingleLog(string name, string log, bool omitFromOutputQ = false)
        {
            if (name.IsNotNEW() && log.IsNotNEW())
            {
                // fetch or generate thread
                DbgThread currThread = FindThread(name);
                currThread ??= GenerateThread(name);

                // thread session begin, print, and end
                if (currThread is not null)
                {
                    /// all this happens whether thread active or not, though it shouldn't be if this is happening
                    // begin
                    currThread.AddToLog(GenerateKeyLine(_keyLineSingleLog, currThread.Name, log));
                    currThread.OmitFromOutputQ = omitFromOutputQ;

                    /// fail-safe in the unpredictable event that the thread is active during this action
                    if (!currThread.IsActiveQ)
                    {
                        // print
                        PrintAndSaveThread(currThread.Index);

                        // end
                        currThread.IsActiveQ = true; /// solely for activity counter
                        ResetThread(currThread.Index);
                    }
                }
            }
        }
        /// <summary>
        ///     Finds a thread instance to adjust the indentation level.
        /// </summary>
        /// <param name="threadIx">Index of thread instance to adjust its indentation.</param>
        /// <param name="isIncrementQ">If <c>true</c>, will increment the indent level by 1, otherwise decrement by 1.</param>
        /// <remarks>Never falls below <c>0</c>.</remarks>
        public static void NudgeIndent(int threadIx, bool isIncrementQ)
        {
            DbgThread currThread = FindThread(threadIx);
            if (currThread is not null)
            {
                if (currThread.IsSetup() && currThread.IsActiveQ)
                    currThread.Indent += isIncrementQ ? 1 : -1;
            }
        }
        /// <summary>
        ///     Finds a thread instance to toggle its omission from the debug ouput prints.
        /// </summary>
        /// <param name="threadIx">Index of thread to toggle output omission.</param>
        /// <remarks>Use after the threads has started logging. Is reset at the end of logging.<br></br>The thread still prints to file. </remarks>
        public static void ToggleThreadOutputOmission(int threadIx)
        {
            DbgThread currThread = FindThread(threadIx);
            if (currThread is not null)
            {
                if (currThread.IsSetup())
                    currThread.OmitFromOutputQ = !currThread.OmitFromOutputQ;
            }
        }
        /// <summary>
        ///     Recieves a list of keywords, each of which will be used case-insensitively to find matches within the name of a thread.
        ///     <br></br>If the thread's name contains any keywords in the list, it will be identified as a 'spam thread'.
        ///     <br></br>If a thread is seen as a 'spam thread' then it is not printed to output, not saved to file, does not trigger notice in other ongoing threads, and is uncounted from active threads.
        /// </summary>
        /// <param name="threadNames">The keywords to match and disable spammy threads. Each must be at least <see cref="spamThreadKeywordMinimum"/> (4) characters in length.</param>
        /// <remarks>Use just after <see cref="Initialize"/> if it is known which threads to ignore. Being specific means more entries, but less accidental omissions.</remarks>
        public static void SetThreadsKeywordSpamList(params string[] threadNames)
        {
            const int spamThreadMinKeyword = spamThreadKeywordMinimum;
            if (threadNames.HasElements() && ThreadSpamsKeywordList is not null)
            {
                foreach (string threadName in threadNames)
                {
                    if (threadName.IsNotNEW())
                        if (threadName.Length >= spamThreadMinKeyword)
                            ThreadSpamsKeywordList.Add(threadName);
                }
            }
        }



        // ADDITIONAL
        /// <summary>
        ///     Generates a new <see cref="DbgThread"/> instance by given <paramref name="name"/> and adds it to the <see cref="Threads"/> list.
        /// </summary>
        /// <param name="name">Name of thread to generate.</param>
        /// <returns>Returns <c>null</c> if the instace could not be generated or added to the list.</returns>
        static DbgThread GenerateThread(string name)
        {
            DbgThread thread = null;
            if (name.IsNotNEW())
            {
                int threadIx = 0;
                if (Threads.HasElements())
                    threadIx = Threads.Count;

                thread = new DbgThread(name, threadIx);
                if (thread.IsSetup())
                    Threads.Add(thread);
                else thread = null;
            }

            return thread;
        }
        /// <summary>
        ///     Searches within the list of <see cref="Threads"/> to find a match by <see cref="DbgThread.Name"/>.
        /// </summary>
        /// <param name="name">Name of thread to search for.</param>
        /// <returns>Returns <c>null</c> if thread could not be found.</returns>
        static DbgThread FindThread(string name)
        {
            DbgThread thread = null;
            if (name.IsNotNEW() && Threads.HasElements())
            {
                for (int tx = 0; tx < Threads.Count && thread == null; tx++)
                {
                    DbgThread toFind = Threads[tx];
                    if (toFind.IsSetup())
                    {
                        if (toFind.Name.Equals(name))
                            thread = toFind;
                    }
                }
            }

            return thread;
        }
        /// <summary>
        ///     Searches within the list of <see cref="Threads"/> to find a match by <see cref="DbgThread.Index"/>.
        /// </summary>
        /// <param name="threadIx">Index of thread to search for.</param>
        /// <returns>Returns <c>null</c> if thread could not be found.</returns>
        static DbgThread FindThread(int threadIx)
        {
            DbgThread thread = null;
            if (threadIx != noIndex && Threads.HasElements())
            {
                for (int tx = 0; tx < Threads.Count && thread == null; tx++)
                {
                    DbgThread toFind = Threads[tx];
                    if (toFind.IsSetup())
                    {
                        if (toFind.Index.Equals(threadIx))
                            thread = toFind;
                    }
                }
            }

            return thread;
        }
        /// <summary>
        ///     Searches within the list of <see cref="Threads"/> to find any <see cref="DbgThread"/> that is actively logging a session.
        /// </summary>
        /// <param name="ignoreSpamThreads">If <c>true</c>, will not fetch threads that have been marked as 'spam thread'.</param>
        /// <returns>An array of all <see cref="DbgThread"/> found to be active.</returns>
        static DbgThread[] FetchActiveThreads(bool ignoreSpamThreads = false)
        {
            List<DbgThread> activeThreads = new();
            if (Threads.HasElements())
            {
                foreach (DbgThread thread in Threads)
                {
                    if (thread.IsSetup() && thread.IsActiveQ)
                    {
                        /// ign = t     spm = t     add? F
                        /// ign = t     spm = f     add? T
                        /// ign = f     spm = t     add? T
                        /// ign = f     spm = f     add? T

                        if (!(ignoreSpamThreads && IsMarkedAsSpam(thread.Index)))
                            activeThreads.Add(thread);
                    }
                }
            }
            return activeThreads.ToArray();
        }
        /// <summary>
        ///     Searches for a <see cref="DbgThread"/> at the given <paramref name="threadIx"/> and executes <see cref="DbgThread.Reset()"/>.
        /// </summary>
        /// <param name="threadIx">Index of thread to deactivate and reset.</param>
        static void ResetThread(int threadIx)
        {
            DbgThread threadToReset = FindThread(threadIx);
            threadToReset?.Reset();
            //if (threadToReset is not null)
            //    threadToReset.Reset();
        }
        /// <summary>
        ///     Searches for the <see cref="DbgThread"/> at index <paramref name="threadIx"/> and prints its session logs to console output and file.
        /// </summary>
        /// <remarks>Will always print to file, but output prints can be bypassed by using <see cref="ToggleThreadOutputOmission(int)"/>.</remarks>
        /// <param name="threadIx">Index of thread to print and save.</param>
        static void PrintAndSaveThread(int threadIx)
        {
            DbgThread threadToFlush = FindThread(threadIx);
            if (threadToFlush is not null)
            {
                if (threadToFlush.Logs.HasElements() && !IsMarkedAsSpam(threadToFlush.Index))
                {
                    // print thread logs to output
                    for (int tdx = 0; tdx < threadToFlush.Logs.Count && !threadToFlush.OmitFromOutputQ; tdx++)
                    {
                        string threadLine = threadToFlush.Logs[tdx];
                        string trueThreadLine = threadLine.TrimStart(' ');
                        int indentLevel = (threadLine.Length - trueThreadLine.Length) / indentFactor;

                        Debug.IndentLevel = indentLevel;
                        Debug.WriteLine(trueThreadLine);
                    }

                    // print thread logs to file
                    string prevDir = Directory, prevFileNAT = FileNameAndType;
                    if (SetFileLocation(FileSaveLocation))
                    {
                        if (!_firstFlushHandledQ)
                            FileWrite(true, null, GenerateKeyLine(_keyLineStartLog[1..], "<DbugInitialized>", null, 0));
                        _firstFlushHandledQ = true;

                        FileWrite(false, null, threadToFlush.Logs.ToArray());
                        SetFileLocation(prevDir, prevFileNAT);
                    }
                }
            }
        }
        /// <summary>
        ///     Checks whether a <see cref="DbgThread"/> at index <paramref name="threadIx"/> is a spammy thread. Thread name is compared to value in <see cref="ThreadSpamsKeywordList"/>.
        /// </summary>
        /// <param name="threadIx">Index of potentially spammy thread.</param>
        /// <returns>A boolean stating whether the thread contains a spam keyword, and thus whether it is a spammy thread.</returns>
        static bool IsMarkedAsSpam(int threadIx)
        {
            bool isSpamThreadQ = false;
            DbgThread threadSpam = FindThread(threadIx);
            if (threadSpam is not null && ThreadSpamsKeywordList.HasElements())
            {
                for (int spx = 0; spx < ThreadSpamsKeywordList.Count && !isSpamThreadQ; spx++)
                {
                    string spamWord = ThreadSpamsKeywordList[spx].ToLower();
                    if (threadSpam.IsSetup() && threadSpam.Name.ToLower().Contains(spamWord))
                        isSpamThreadQ = true;
                }
            }
            return isSpamThreadQ;
        }

        /// <summary>
        ///     Assists with constructing a string for some common log lines. (Lousy implementation)
        /// </summary>
        /// <param name="keyLineForm">The key line form to use: 
        ///     start log (<see cref="_keyLineStartLog"/>), end log (<see cref="_keyLineEndLog"/>), 
        ///     single log (<see cref="_keyLineSingleLog"/>), notice log (<see cref="_keyLineNoticeLog"/>).
        /// </param>
        /// <param name="name">Required. The <see cref="DbgThread.Name"/> to use.</param>
        /// <param name="log">May be null. The single log line.</param>
        /// <returns>A constructed string for a common key log line with whatever values have been provided.</returns>
        static string GenerateKeyLine(string keyLineForm, string name, string log = null, int? threadCount = null)
        {
            string finalKeyLine = "";
            if (keyLineForm.IsNotNEW() && name.IsNotNEW())
            {
                string time = GetRunTime();
                int countActive = FetchActiveThreads().Length;

                finalKeyLine = keyLineForm.Replace(_keyName, name).Replace(_keyTime, time).Replace(_keyActiveC, countActive.ToString());
                if (log.IsNotNEW())
                    finalKeyLine = finalKeyLine.Replace(_keyLog, log);
                if (threadCount is not null)
                    finalKeyLine = finalKeyLine.Replace(_keyThreadCount, threadCount.ToString());
            }
            return finalKeyLine;
        }
        /// <summary>
        ///     Get the application runtime since <see cref="Dbg.Initialize"/> was called.
        /// </summary>
        /// <returns>A string representing runtime as hours, minutes, and seconds in the following format: '<c>[0:00:00]</c>'. Hours do not show under at '0'.</returns>
        static string GetRunTime()
        {
            string timeStamp = null;
            if (_runtimeWatch is not null)
            {
                if (_runtimeWatch.Elapsed.TotalHours >= 1)
                    timeStamp = $"{(int)_runtimeWatch.Elapsed.TotalHours:0}:";
                else timeStamp = "";
                timeStamp += $"{(int)_runtimeWatch.Elapsed.TotalMinutes % 60:00}:{(int)_runtimeWatch.Elapsed.TotalSeconds % 60:00}";
            }
            return $"[{timeStamp}]"; // [00:00]  // [0:00:00]
        }
        #endregion
    }
}
