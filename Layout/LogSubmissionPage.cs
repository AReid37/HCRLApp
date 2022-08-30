using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;

namespace HCResourceLibraryApp.Layout
{
    public static class LogSubmissionPage
    {
        static readonly char subMenuUnderline = '*';
        static string pathToVersionLog = null;

        public static void OpenPage()
        {
            bool exitLogSubMain = false;
            do
            {
                Clear();
                Title("Version Log Submission", cTHB, 1);
                FormatLine($"{Ind24}Facilitates the submission of a version log to store content information regarding the resource pack.", ForECol.Accent);
                NewLine(2);

                bool validMenuKey = TableFormMenu(out short resNum, "Log Submission Menu", null, false, $"{Ind24}Selection >> ", "1/2", 2, $"Submit a Version Log,{exitPagePhrase}".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (resNum)
                    {
                        case 1:
                            SubPage_SubmitLog();
                            break;

                        case 2:
                            exitLogSubMain = true;
                            break;
                    }
                }
                
            } while (!exitLogSubMain && !Program.AllowProgramRestart);
        }

        static void SubPage_SubmitLog()
        {
            bool exitSubmissionPage = false;
            do
            {
                /** Stages of verion log submission
                        - Provide path to version log (log location)
                        - Original version log review (raw)
                        - Processed version log review (decoded)
                 */
                Clear();
                Title("Submit a Version Log", subMenuUnderline, 1);
                FormatLine($"{Ind14}There are a few stages to submitting a version log:", ForECol.Normal);
                List(OrderType.Ordered_Numeric, "Provide file path to log,Original log review (raw),Processed log review (decoded)".Split(','));
                NewLine();

                Format($"{Ind24}Enter any key to continue to log submission >> ", ForECol.Normal);
                string input = StyledInput(null);
                if (input.IsNotNEW())
                {
                    bool stopSubmission = false;
                    int stageNum = 1;
                    const char minorChar = '-';
                    while (stageNum <= 3 && !stopSubmission)
                    {
                        string[] stageNames = 
                        { 
                            "Provide File Path", "Log Review (Raw)", "Log Review (Decoded)"
                        };
                        string stageName = stageNames[stageNum - 1];

                        Clear();
                        Title("Log Submission", subMenuUnderline, 2);
                        Important($"STAGE {stageNum}: {stageName}", subMenuUnderline);
                        HorizontalRule(minorChar, 1);
                        bool stagePass = false;

                        // 1 - provide file path
                        if (stageNum == 1)
                        {
                            string placeHolder = @"C:\__\__.__";
                            const string recentDirectoryKey = @"RDir\";
                            /// if recent directory exists
                            if (LogDecoder.RecentDirectory.IsNotNEW())
                            {
                                Format($"The directory of the last submitted file was saved. To use this directory, precede the name of the submitted file with", ForECol.Normal);
                                Highlight(true, $"'{recentDirectoryKey}'.", recentDirectoryKey);
                                Highlight(true, $"{recentDirectoryKey} :: {LogDecoder.RecentDirectory}", LogDecoder.RecentDirectory);
                                placeHolder += $"  -or-  {recentDirectoryKey}__.__";
                                NewLine();
                            }
                            FormatLine("Please enter the file path to the version log being submitted below. The file should be of type 'text file' (.txt) or any similar text-only file type.", ForECol.Normal);
                            Format($"{Ind14}Path >> ", ForECol.Normal);
                            string inputPath = StyledInput(placeHolder);

                            /// file path verification
                            bool validPath = false;
                            if (inputPath.IsNotNEW())
                            {
                                // substitute if using recent directory
                                if (inputPath.Contains(recentDirectoryKey) && LogDecoder.RecentDirectory.IsNotNEW())
                                    inputPath = inputPath.Replace(recentDirectoryKey, LogDecoder.RecentDirectory);

                                // validation
                                if (inputPath.Contains(":\\"))
                                {
                                    if (inputPath.Replace(":\\","").Contains("\\"))
                                    {
                                        if (inputPath.Contains("."))
                                        {
                                            bool validFilePath = SetFileLocation(inputPath);
                                            if (validFilePath)
                                            {
                                                validPath = FileRead(null, out string[] xLog);
                                                if (validPath)
                                                {
                                                    bool emptyFileQ = !xLog.HasElements();
                                                    if (emptyFileQ)
                                                    {
                                                        IncorrectionMessageQueue("There is no data within this file. Please choose another file path.");
                                                        validPath = false;
                                                    }
                                                }
                                                else
                                                    IncorrectionMessageQueue($"{Tools.GetRecentWarnError(false, false)}");
                                            }
                                            else IncorrectionMessageQueue($"{Tools.GetRecentWarnError(false, false)}");
                                        }
                                        else IncorrectionMessageQueue("No file type specified at end of path.");
                                    }
                                    else IncorrectionMessageQueue("No path directories specified.");
                                }
                                else IncorrectionMessageQueue("No hard drive specified.");
                            }
                            else IncorrectionMessageQueue("No path entered.");
                            
                            /// file path confirmation
                            if (validPath)
                            {
                                // confirmation here
                                NewLine(3);
                                //Heading2("Confirm Log Path", false);
                                Important("Confirm Path".ToUpper(), subMenuUnderline);
                                Highlight(true, $"Provided path to version log ::\n{Ind14}{inputPath}", inputPath);
                                NewLine();
                                Confirmation($"{Ind14}Confirm path submission? \n{Ind24}Yes / No >> ", false, out bool yesNo);

                                if (yesNo)
                                {                                    
                                    pathToVersionLog = inputPath;
                                    SetFileLocation(pathToVersionLog);
                                    LogDecoder.RecentDirectory = Directory;
                                    stagePass = true;
                                }
                                ConfirmationResult(yesNo, $"{Ind34}", "Version log file path accepted.", "Path to version log denied.");
                            }
                            else IncorrectionMessageTrigger($"{Ind24}Invalid file path entered:\n{Ind34}");
                        }
                        // 2 - original log review (raw)
                        else if (stageNum == 2)
                        {
                            SetFileLocation(pathToVersionLog);
                            bool fetchedLogData = FileRead(null, out string[] logLines);
                            if (fetchedLogData)
                            {
                                if (logLines.HasElements())
                                {
                                    FormatLine($"Below sourced from :: \n{Ind14}{pathToVersionLog}", ForECol.Accent);
                                    NewLine();

                                    // display file info
                                    for (int lx = 0; lx < logLines.Length; lx++)
                                    {
                                        string line = logLines[lx];
                                        if (line.Contains("[") && lx != 0)
                                            NewLine();
                                        bool omit = line.StartsWith("--") && !line.Contains("[");
                                        FormatLine(line, omit ? ForECol.Normal : ForECol.Highlight);
                                    }
                                    HorizontalRule(minorChar, 1);

                                    // confirm review
                                    NewLine();
                                    Important("Confirm Review".ToUpper(), subMenuUnderline);
                                    FormatLine("Please confirm that the contents above matches the version log submitted.", ForECol.Normal);
                                    Confirmation($"{Ind14}Reviewed version log confirmed? ", false, out bool yesNo);
                                    if (yesNo)
                                        stagePass = true;
                                    else stageNum = 1;
                                    ConfirmationResult(yesNo, $"{Ind34}", "Version log contents have been confirmed.", "Version log contents unconfirmed. Returning to previous stage.");
                                }                                
                            }
                            else
                                DataReadingIssue();

                        }
                        // 3 - processed log review (decoded)
                        else if (stageNum == 3)
                        {
                            LogDecoder logDecoder = new LogDecoder();
                            SetFileLocation(pathToVersionLog);
                            if (FileRead(null, out string[] fileData) && false)
                            {
                                /// small section relaying how decoding went
                                logDecoder.DecodeLogInfo(fileData);
                                
                                /// large section showing how decoding went
                                NewLine(2);
                                FormatLine("Completed decoding of version log...preview here", ForECol.Correction);
                                Pause();
                            }
                            else
                                DataReadingIssue();
                            stagePass = true;
                        }

                                                
                        /// pause and continue
                        if (stageNum < 3 && stagePass)
                        {
                            NewLine(3);
                            HorizontalRule(minorChar, 1);
                            Title("Continue Version Log Submission");
                            Format($"{Ind24}Enter any key to continue with log submission >> ", ForECol.Normal);
                            input = StyledInput(null);

                            stopSubmission = input.IsNEW();
                            if (stopSubmission)
                                pathToVersionLog = null;
                        }

                        /// move to next stage
                        stageNum += stagePass ? 1 : 0;


                        static void DataReadingIssue()
                        {
                            FormatLine($"{Ind14}Could not read data from log file!", ForECol.Incorrection);
                            Format($"{Ind14}If the file is open or locked, please close and enable access to the file and try again.", ForECol.Warning);
                            Pause();
                        }
                    }
                }
                else exitSubmissionPage = true;
            }
            while (!exitSubmissionPage);

            // auto-saves: 
            //      -> LogDecoder recentDirectory
            //      -> ResLibrary <new data>
            if (LogDecoder.ChangesMade())
                Program.SaveData(true);
        }
    }
}
