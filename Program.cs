﻿using System;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;
using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.Layout;

namespace HCResourceLibraryApp
{
    // THE ENTRANCE POINT, THE CONTROL ROOM
    public class Program
    {
        static string consoleTitle = "High Contrast Resource Library App [v1.0.7]";
        #region fields / props
        // PRIVATE \ PROTECTED
        const string saveIcon = "▐▀▀▄▄▌";
        static bool _programRestartQ;
        static DataHandlerBase dataHandler;
        static Preferences preferences;
        static LogDecoder logDecoder;

        // PUBLIC
        public static bool AllowProgramRestart { get => _programRestartQ; private set => _programRestartQ = value; }
        #endregion

        #region Suppressant for Window Size and Buffer Size Edits to Console
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        #endregion
        static void Main()
        {
            // Lvl.0 - program launch
            bool restartProgram;
            do
            {
                Clear();
                AllowProgramRestart = false;
                TextLine("Hello, High Contrast Resource Library App!", Color.DarkGray);

                // setup                
                /// program function
                Console.Title = consoleTitle;
                Tools.DisableWarnError = DisableWE.None;
                /// data
                dataHandler = new DataHandlerBase();
                preferences = new Preferences();
                logDecoder = new LogDecoder();
                LoadData();
                /// --v priting and pages
                VerifyFormatUsage = true;          
                GetPreferencesReference(preferences);
                ApplyPreferences();
                SettingsPage.GetPreferencesReference(preferences);                

                // testing site
                RunTests();


                // Lvl.1 - title page and main menu
                /// home page
                NewLine(10);
                Title("H i g h   C o n t r a s t", cBHB, 0);
                Title("  Resource Library  App  ", cTHB, 3);
                //Title("High Contrast Resource Library App", cBHB, 3);
                Format($"{Ind24}Press [Enter] to continue >> ", Layout.ForECol.Normal);
                StyledInput("__");
                
                /// main menu
                if (!LastInput.IsNotNE())
                {
                    bool mainMenuQ = true;
                    do
                    {
                        /// Main Menu
                        /// ->  Logs Submission
                        /// ->  Library Search
                        /// ->  Log Legend View
                        /// ->  Version Summaries
                        /// ->  Settings Page
                        ///     Quit

                        Clear();
                        bool isValidMMOpt = ListFormMenu(out string mainMenuOptKey, "Main Menu", null, $"{Ind24}Option >> ", "a~f", true,
                            "Logs Submission, Library Search, Log Legend View, Version Summaries, Settings, Quit".Split(", "));
                        MenuMessageQueue(mainMenuOptKey == null, false, null);

                        if (isValidMMOpt)
                        {
                            // other options
                            if (!mainMenuOptKey.Contains('f'))
                            {
                                // logs submission page
                                if (mainMenuOptKey.Equals("a"))
                                    LogSubmissionPage.OpenPage();
                                // settings page
                                else if (mainMenuOptKey.Equals("e"))
                                    SettingsPage.OpenPage();


                                else
                                {
                                    TextLine("\n\nEntering --- page");
                                    Pause();
                                }
                            }

                            // quit
                            else
                            {
                                TextLine("\n\nExiting Program");
                                mainMenuQ = false;
                            }
                        }

                    } while (mainMenuQ && !AllowProgramRestart);
                }

                restartProgram = AllowProgramRestart;
                if (AllowProgramRestart)
                {
                    Clear();
                    NewLine(10);
                    HorizontalRule(cLS);
                    Title(" Restarting Program ", cMS, 0);
                    HorizontalRule(cLS, 2);

                    Format("\tPlease wait a few seconds...", ForECol.Accent);
                    Wait(3);
                }
            }
            while (restartProgram);

            TextLine("\n\n**REMEMBER** Test the published version of the application frequently!!\n\tVersion last tested: mid-v1.0.7".ToUpper(), Color.White);

        }

        public static void RequireRestart()
        {
            // for now
            //SaveData();
            AllowProgramRestart = true;
        }

        // Global Data handling resources
        public static bool SaveData(bool discreteQ)
        {
            bool savedDataQ = dataHandler.SaveToFile(preferences, logDecoder);

            NewLine(2);
            Format($"{saveIcon}\t", ForECol.Accent);
            if (savedDataQ)
                FormatLine(discreteQ? "auto-save: S." : "Auto-saving data ... success.", discreteQ? ForECol.Accent : ForECol.Correction);
            else FormatLine(discreteQ? "auto-save: F." : "Auto-saving data ... failed.", discreteQ ? ForECol.Accent : ForECol.Incorrection);
               
            Wait(savedDataQ ? 1 : 3);
            return savedDataQ;
        }
        public static void LoadData()
        {
            dataHandler.LoadFromFile(preferences, logDecoder);
        }


        // TESTING STUFF
        static bool runTest = false;
        static Tests testToRun = Tests.PageBase_ColorMenu;
        enum Tests
        {
            PageBase_HighlightMethod,
            PageBase_ListFormMenu,
            PageBase_Wait,
            PageBase_TableFormMenu,
            PageBase_ColorMenu,
            //PageBase_NavigationBar
        }
        static void RunTests()
        {
            if (runTest)
            {
                // tests title
                Clear();
                Title("Running Test", cMS, 0);
                //Important("Running Test", cMS);
                string testName = "";
                foreach (char c in testToRun.ToString())
                {
                    if (c.IsNotNull())
                        if (c.ToString() == "_")
                            testName += $":";
                        else if (c.ToString() == c.ToString().ToUpper())
                            testName += $" {c}";
                        else testName += c;
                }    
                Title($"{testName.Trim()}", cLS, 3);



                // tests branches
                bool hasDebugQ = true;
                if (testToRun == Tests.PageBase_HighlightMethod)
                {
                    Highlight(false, "Highlight me you fool!");
                    Highlight(true, "Highlight me you oblivious fool!", "", "me", "fool", "ighli", "ou");
                    Highlight(false, "Break the system, destroy what will remain", "s", "what will remain");

                }
                else if (testToRun == Tests.PageBase_ListFormMenu)
                {
                    string optKey;
                    bool valid = false;
                    TextLine("ListFormMenu(out optKey, false, \"Example Menu\", null, null, null, false, \"Option1, Option2, Option3, Option4\".Split(','));", Color.Blue);
                    while (!valid)
                        valid = ListFormMenu(out optKey, "Example Menu", null, null, null, false, "Option1,Option2,Option3,Option4".Split(','));
                    NewLine(2);

                    valid = false;
                    TextLine("ListFormMenu(out optKey, false, \"Other Example Menu\", '=', $\"{Ind24}Custom selection prompt >> \", \"~~~~\", true, \"Option5,Option6,Option7,Option8,,Option9\".Split(','));", Color.Blue);
                    while (!valid)
                        valid = ListFormMenu(out optKey, "Other Example Menu", '=', $"{Ind24}Custom selection prompt >> ", "~~~~", true, "Option5,Option6,Option7,Option8,,Option9".Split(','));
                }
                else if (testToRun == Tests.PageBase_Wait)
                {
                    Text("Testing wait (for 1 second)");
                    Wait(1);
                    Text("Testing wait (for 0.25 second)");
                    Wait(0.25f);
                    Text("Testing wait (for 4 second)");
                    Wait(4);
                    Text("Testing end.");

                }
                else if (testToRun == Tests.PageBase_TableFormMenu)
                {
                    short optNum;
                    bool valid = false;
                    TextLine("TableFormMenu(out optNum, \"Example Menu\", null, false, null, null, 0, \"OptionA,,OptionB,OptionC,OptionD\".Split(','))", Color.Blue);
                    while (!valid)
                        valid = TableFormMenu(out optNum, "Example Menu", null, false, null, null, 0, "OptionA, ,OptionB,,OptionC,OptionD".Split(','));
                    NewLine(5);

                    valid = false;
                    TextLine("TableFormMenu(out optNum, \"Example Menu\", '=', true, $\"{Ind24}Choose choice >> \", \"~~~\", 4, \"OptionA,OptionB,OptionC,OptionD,OptionE,OptionF,OptionG\".Split(','));", Color.Blue);
                    while (!valid)
                        valid = TableFormMenu(out optNum, "Example Menu", '=', true, $"{Ind24}Choose choice >> ", "~~~", 4, "OptionA,OptionB,OptionC,OptionD,OptionE,OptionF,OptionG".Split(','));
                }
                else if (testToRun == Tests.PageBase_ColorMenu)
                {
                    bool valid = false;
                    int attempts = 4;
                    while (!valid || attempts > 0)
                    {
                        Color cToExempt = (Color)Extensions.Random(1, 15);
                        valid = ColorMenu("example menu", out Color col, cToExempt);
                        attempts--;
                        NewLine(3);
                    }
                }

                if (hasDebugQ)
                    TextLine("\n\n## Debug(s) to output have been ran ##", Color.Maroon);

                // end tests
                Pause();
                Clear();
            }
        }


        // THOUGHTS...
        // I am able to set the Cursor position within the console window
        // With this, maybe I can truly create the kind of search page I have only dreamed about!
        // Console.CursorLeft and Console.CursorTop
    }
}
