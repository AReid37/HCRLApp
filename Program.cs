using System;
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
        static string consoleTitle = "High Contrast Resource Library App [v1.0.4]";
        static bool runTest = false;
        static Tests testToRun = Tests.PageBase_Wait;

        #region fields / props
        static bool _programRestartQ;
        public static bool AllowProgramRestart { get => _programRestartQ; private set => _programRestartQ = value; }

        static DataHandlerBase dataHandler;
        static Preferences preferences;
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
                /// data
                dataHandler = new DataHandlerBase();
                preferences = new Preferences();
                LoadData();
                /// program function
                Console.Title = consoleTitle;
                Tools.DisableWarnError = DisableWE.None;
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
                Input("__", preferences.Highlight);
                
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
                                // settings page
                                if (mainMenuOptKey.Equals("e"))
                                {
                                    SettingsPage.OpenPage();
                                }


                                else
                                {
                                    TextLine("\n\nEntering --- page");
                                    Pause();
                                }
                            }

                            // quit
                            else
                            {
                                TextLine("\n\nExiting Program (also saving data .. temp)");
                                SaveData();
                                TextLine("\n\n**REMEMBER** Test the published version of the application frequently!!".ToUpper(), Color.White);
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
        }

        public static void RequireRestart()
        {
            // for now
            SaveData();

            AllowProgramRestart = true;
        }

        // Global Data handling resources
        public static void SaveData()
        {
            dataHandler.SaveToFile(preferences);
        }
        public static void LoadData()
        {
            dataHandler.LoadFromFile(preferences);
        }

        // testing stuff
        enum Tests
        {
            PageBase_HighlightMethod,
            PageBase_ListFormMenu,
            PageBase_Wait,
            //PageBase_TableFormMenu,
            //PageBase_ColorMenu,
            //PageBase_NavigationBar
        }
        static void RunTests()
        {
            if (runTest)
            {
                // tests title
                Clear();
                Title("Running Test", cMS, 0);
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

                if (hasDebugQ)
                    TextLine("\n\n## Debug(s) to output have been ran ##", Color.Maroon);

                // end tests
                Pause();
                Clear();
            }
        }
    }
}
