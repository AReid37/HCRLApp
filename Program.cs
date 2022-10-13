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
        static readonly string consoleTitle = "High Contrast Resource Library App [v1.1.3]";
        #region fields / props
        // PRIVATE \ PROTECTED
        static string prevWhereAbouts;
        const string saveIcon = "▐▄▐▌"; //  1▐▀▀▄▄▌;    2▐▄▐▌;  3 ▐▄▄▌
        static bool _programRestartQ;
        static DataHandlerBase dataHandler;
        static Preferences preferences;
        static LogDecoder logDecoder;
        static ResLibrary resourceLibrary;

        // PUBLIC
        public static bool AllowProgramRestart { get => _programRestartQ; private set => _programRestartQ = value; }
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
                resourceLibrary = new ResLibrary();                
                LoadData();
                /// --v printing and pages
                VerifyFormatUsage = true;          
                GetPreferencesReference(preferences);
                ApplyPreferences();
                SettingsPage.GetPreferencesReference(preferences);
                SettingsPage.GetResourceLibraryReference(resourceLibrary);

                // testing site
                RunTests();


                // Lvl.1 - title page and main menu
                /// home page               
                HomePage.OpenPage();
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

                        LogState("Main Menu");
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
                                    LogSubmissionPage.OpenPage(resourceLibrary);
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
                    LogState("Restarting");
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

            Dbug.EndLogging();
            TextLine("\n\n**REMEMBER** Test the published version of the application frequently!!\n\tVersion last tested: mid-v1.0.7".ToUpper(), Color.White);
        }


        public static void RequireRestart()
        {
            // for now
            //SaveData();
            AllowProgramRestart = true;
        }
        /// <summary>Note as "Page|Subpage" or "State|Substate"</summary>
        public static void LogState(string whereAbouts)
        {
            if (whereAbouts.IsNotNE())
            {
                // Program State Log Session Name
                const string pslsn = "Program State";
                const string stateDepthKey = "|";
                if (prevWhereAbouts != whereAbouts)
                {
                    Dbug.SingleLog(pslsn, whereAbouts.Replace(stateDepthKey, " --> "));
                    prevWhereAbouts = whereAbouts;
                }
            }
        }

        // Global Data handling resources
        public static bool SaveData(bool discreteQ)
        {
            LogState("Saving Data");
            bool savedDataQ = dataHandler.SaveToFile(preferences, logDecoder, resourceLibrary);
            NewLine(2);
            Format($"{saveIcon}\t", ForECol.Accent);
            if (savedDataQ)
                FormatLine(discreteQ? "auto-save: S." : "Auto-saving data ... success.", discreteQ? ForECol.Accent : ForECol.Correction);
            else FormatLine(discreteQ? "auto-save: F." : "Auto-saving data ... failed.", discreteQ ? ForECol.Accent : ForECol.Incorrection);
               
            Wait(savedDataQ ? 1.5f : 3);
            return savedDataQ;
        }
        public static void LoadData()
        {
            LogState("Loading Data");
            dataHandler.LoadFromFile(preferences, logDecoder, resourceLibrary);
        }
        public static bool SaveReversion()
        {
            return dataHandler.RevertSaveFile();
        }
        


        // TESTING STUFF
        static readonly bool runTest = false;
        static readonly Tests testToRun = Tests.LogSubmissionPage_DisplayLogInfo;
        enum Tests
        {
            PageBase_HighlightMethod,
            PageBase_ListFormMenu,
            PageBase_Wait,
            PageBase_TableFormMenu,
            PageBase_ColorMenu,
            //PageBase_NavigationBar,

            Extensions_SortWords,
            LogDecoder_DecodeLogInfo,
            LogSubmissionPage_DisplayLogInfo,
            Dbug_NestedSessions,
            Dbug_DeactivateSessions
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
                LogState($"Running Test|{testName.Trim()}");

                // tests branches
                bool hasDebugQ = true;
                /// PageBase tests
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
                /// Extensions tests
                else if (testToRun == Tests.Extensions_SortWords)
                {
                    TextLine("Below is a summary of the tests ran:", Color.DarkGray);
                    List(OrderType.Ordered_RomanNumerals, "Verifying correct scoring for all characters", "Rigid testing (hit all sort-decision paths)", "General testing (scoring & sorting algorithm)");
                    NewLine(3);

                    // test 1 - verify character scorings
                    string[] testWords =
                    {
                        "\0~`!@#$%^&* _-+=()[]{}<>\\|/;:'\".,?",
                        "~0 1 2 3 4 5 6 7 8 9".Replace(" ", "."),
                        "abcdefghijklmnopqrstuvwxyz",
                        $"{cLS}{cMS}{cDS}{cTHB}{cRHB}{cBHB}{cLHB}"
                    };
                    ShowTestWords("Test I words - not sorted");
                    testWords = testWords.SortWords().ToArray();
                    ShowTestWords("Test I sorted words");
                    NewLine(2);

                    // test 2 - rigid testing (hit all paths)
                    testWords = new string[]
                    {
                        /// EXPECTED SORTING PATH HITS
                        /// focus A paths :: 
                        ///     [1] 100|80.5
                        ///     [2] 80.5|zebra
                        ///     [3] zinc|zit
                        /// focus B paths :: {can never be hit!}
                        /// focus C paths ::
                        ///     [1] 90|@gump
                        ///     [2] 90|100
                        ///     [3] arts|80.5
                        ///     [4] zit|zebra   90|80.5
                        ///
                        
                        "zebra", "zit", "zinc", "80.5", "100", "@gump", "90", "arts"

                        /** RESULTS (BASED ON ABOVE)
                        Results
                            Path	Ran?	True?	1st Compared:
                            ----    ----    ----    ----
                            A1	    Y	    Y	    100|80.5	
                            A2	    Y	    Y	    80.5|zebra
                            A3	    Y	    Y	    zinc|zit
                            B1	    N       Y       -
                            B2	    N       Y       -
                            C1	    Y	    Y	    90|@gump
                            C2	    Y	    Y	    90|100
                            C3	    Y	    Y	    arts|80.5
                            C4	    Y	    Y	    zit|zebra
                        
                        
                        Path definitions
                        A. (tS<oS)
                        1 | tS==vc (oS<tVcNm) {nxt}
                        2 | tS==vc (oS>=tVcNm) {ins}
                        3 | tS!=vc {ins}

                        B. (tS==oS)
                        1 | tS==vc (tVcNm<oVcNm) {ins}
                        2 | tS==vc (tVcNm>oVcNm) {nxt}

                        C. (tS>oS)
                        1 | tS==vc {nxt}
                        2 | tS!=vc (oS==vc (tS<oVcNm)) {ins}
                        3 | tS!=vc (oS==vc (tS>=oVcNm)) {nxt}
                        4 | tS!=vc (oS!=vc) {nxt}
                         */
                    };
                    ShowTestWords("Test II words - not sorted");
                    testWords = testWords.SortWords().ToArray();
                    ShowTestWords("Test II sorted words");
                    NewLine(2);

                    // test 3 - general testing                    
                    testWords = new string[]
                    {
                        "625", "apple sauce", "apple_sauce", "$3.50", "8a7c", "86ab",
                        "4", "212", "3at gr@pes", "(0n$vm3!", "sma taster", "Smat-ester?",
                        "u34", "u9.3", "u28", "j10d468", "j11a162"
                    };
                    ShowTestWords("Test III words - not sorted");
                    testWords = testWords.SortWords().ToArray();
                    ShowTestWords("Test III sorted words");
                    Text("\nextra...");
                    Pause();

                    NewLine(10);                
                    testWords = new string[]
                    {
                        /** SORT HIGH CONTRAST OST NAMES!
                        High Contrast Theme, Alt High Contrast Theme, Saturate Expedition, Saturate Expedition Alt, Synthwave Sunburn, Surface Day, Alternate Surface Day, Surface Night, Alt Caves Below, Caves Below, Infliction, Infection, Luminance, Sands, Rehptihlian Temple, Labyrinth of the Cadaverous, Navy Horizons, Navy Horizons Night, Outer World, Outer World Day, Bioluminescent Fungus, World Down Under, Wilderness, Wilderness Night, Tundra, Wilderness Below, Tundra Below, Deep Infliction, Deep Infection, Deep Illumination, Sunken Sands, Vacant Tombs, Shadow of the Moon, Downpour, Thunderstorm, Haunting Hour, Army of Goblins, Plundering Mob, Xenophobia, Lunar Descent, Village Day, Village Night, Icey Blue Moon, Crooked Smiling Moon, Gales, Morning Dew, Battalions of the Old One, Gelatinous Downpour, Roiling Sands, First Encounter, Zealous Terminator, Mecha Madness, Buzzed and Brutal, Wrath of Luminosity, Matriarch of Gelatin, Mother of all Plants, Gargantuan Idol, Oceanic Horror, Lunar Judgement

                        "High Contrast Theme", "Alt High Contrast Theme", "Saturate Expedition", "Saturate Expedition Alt", "Synthwave Sunburn", "Surface Day", "Alternate Surface Day", "Surface Night", "Alt Caves Below", "Caves Below", "Infliction", "Infection", "Luminance", "Sands", "Rehptihlian Temple", "Labyrinth of the Cadaverous", "Navy Horizons", "Navy Horizons Night", "Outer World", "Outer World Day", "Bioluminescent Fungus", "World Down Under", "Wilderness", "Wilderness Night", "Tundra", "Wilderness Below", "Tundra Below", "Deep Infliction", "Deep Infection", "Deep Illumination", "Sunken Sands", "Vacant Tombs", "Shadow of the Moon", "Downpour", "Thunderstorm", "Haunting Hour", "Army of Goblins", "Plundering Mob", "Xenophobia", "Lunar Descent", "Village Day", "Village Night", "Icey Blue Moon", "Crooked Smiling Moon", "Gales", "Morning Dew", "Battalions of the Old One", "Gelatinous Downpour", "Roiling Sands", "First Encounter", "Zealous Terminator", "Mecha Madness", "Buzzed and Brutal", "Wrath of Luminosity", "Matriarch of Gelatin", "Mother of all Plants", "Gargantuan Idol", "Oceanic Horror", "Lunar Judgement"
                        */
                        "High Contrast Theme", "Alt High Contrast Theme", "Saturate Expedition", "Saturate Expedition Alt", "Synthwave Sunburn", "Surface Day", "Alternate Surface Day", "Surface Night", "Alt Caves Below", "Caves Below", "Infliction", "Infection", "Luminance", "Sands", "Rehptihlian Temple", "Labyrinth of the Cadaverous", "Navy Horizons", "Navy Horizons Night", "Outer World", "Outer World Day", "Bioluminescent Fungus", "World Down Under", "Wilderness", "Wilderness Night", "Tundra", "Wilderness Below", "Tundra Below", "Deep Infliction", "Deep Infection", "Deep Illumination", "Sunken Sands", "Vacant Tombs", "Shadow of the Moon", "Downpour", "Thunderstorm", "Haunting Hour", "Army of Goblins", "Plundering Mob", "Xenophobia", "Lunar Descent", "Village Day", "Village Night", "Icey Blue Moon", "Crooked Smiling Moon", "Gales", "Morning Dew", "Battalions of the Old One", "Gelatinous Downpour", "Roiling Sands", "First Encounter", "Zealous Terminator", "Mecha Madness", "Buzzed and Brutal", "Wrath of Luminosity", "Matriarch of Gelatin", "Mother of all Plants", "Gargantuan Idol", "Oceanic Horror", "Lunar Judgement"
                    };
                    ShowTestWords("Test IV words - not sorted, HC.OST names");
                    Pause();
                    NewLine(2);
                    testWords = testWords.SortWords().ToArray();
                    ShowTestWords("Test IV words - sorted, HC.OST names");

                    void ShowTestWords(string titleText)
                    {
                        if (testWords.HasElements())
                        {
                            if (titleText.IsNEW())
                                titleText = "Showing Test Words";
                            Title(titleText);
                            int bufferWidth = Console.BufferWidth;
                            int remainingBufferWidth = bufferWidth;
                            int w = 0;
                            foreach (string word in testWords)
                            {        
                                if (word.IsNotNEW())
                                {
                                    int wordLen = word.Length;
                                    if (remainingBufferWidth - wordLen - 2 <= 0)
                                    {
                                        NewLine();
                                        remainingBufferWidth = bufferWidth;
                                    }

                                    Text($"{word}", w % 2 == 0 ? Color.Green : Color.Yellow);
                                    Text(" ", Color.DarkGray);
                                    remainingBufferWidth -= (wordLen + 2);
                                    w++;
                                }
                            }
                            NewLine();
                        }
                    }
                }
                /// Log Decoder
                else if (testToRun == Tests.LogDecoder_DecodeLogInfo)
                {
                    TextLine("The results of this test are recorded through Dbug.cs. Nothing to preview here...", Color.DarkGray);
                    if (SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex1 Version Log.txt"))
                        if (FileRead(null, out string[] testLogData))
                            logDecoder.DecodeLogInfo(testLogData);
                }
                /// Log Submission Page
                else if (testToRun == Tests.LogSubmissionPage_DisplayLogInfo)
                {
                    hasDebugQ = false;
                    TextLine("Displays information from a log decoder", Color.DarkGray);
                    bool locationSet = SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex3 Version Log.txt");
                    //@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex1 Version Log.txt"
                    //@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex3 Version Log.txt"
                    if (locationSet)
                        if (FileRead(null, out string[] testLogData))
                            if (logDecoder.DecodeLogInfo(testLogData))
                            {
                                NewLine(1);
                                Important("Regular post-decoding preview");
                                HorizontalRule('.');
                                LogSubmissionPage.DisplayLogInfo(logDecoder, false);
                                HorizontalRule('.');
                                Pause();

                                NewLine(10);
                                Important("Informative post-decoding preview");
                                HorizontalRule('.');
                                LogSubmissionPage.DisplayLogInfo(logDecoder, true);
                                HorizontalRule('.');
                            }
                }
                /// Dbug
                else if (testToRun == Tests.Dbug_NestedSessions)
                {
                    TextLine("The results of this test are recorded through Dbug.cs. Nothing to preview here...", Color.DarkGray);
                    Dbug.StartLogging("Base debug session");
                    Dbug.Log("base line 1");
                    Dbug.NudgeIndent(true);
                    Dbug.Log("base line 2");
                    Dbug.NudgeIndent(false);
                    Dbug.LogPart("base line 3  //  ");
                    Dbug.Log("base line 4");
                    Dbug.SingleLog("Baseless log", "baseless line A");

                    /// nested start
                    Dbug.StartLogging("Nested debug session");
                    Dbug.Log("nested line 1");
                    Dbug.NudgeIndent(true);
                    Dbug.Log("nested line 2");
                    Dbug.NudgeIndent(false);
                    Dbug.LogPart("nested line 3  //  ");
                    Dbug.Log("nested line 4");
                    Dbug.SingleLog("Baseless log", "baseless line B");
                    Dbug.EndLogging();
                    /// nested end

                    Dbug.Log("base line 5");
                    Dbug.EndLogging();

                    Dbug.StartLogging("Base 1");
                    Dbug.Log("Allowed log line a");
                    Dbug.StartLogging("Nest 1");
                    Dbug.Log("Allowed nested line a");
                    Dbug.StartLogging("(Unallow) Nest 2");
                    Dbug.Log("Uallowed nested line b");
                    Dbug.EndLogging();
                    Dbug.Log("Allowed log line b");
                    Dbug.IgnoreNextLogSession();
                    Dbug.StartLogging("Nest 2 (ignored)");
                    Dbug.Log("Ignored nested line I");
                    Dbug.NudgeIndent(true);
                    Dbug.Log("Ignored nested line II");
                    Dbug.NudgeIndent(false);
                    Dbug.LogPart("Ignored nested line III // ");
                    Dbug.Log("Ignored nested line IV");
                    Dbug.EndLogging();
                    Dbug.Log("Allowed log line c");
                    Dbug.EndLogging();

                    Dbug.IgnoreNextLogSession();
                    Dbug.StartLogging("Ignored Base 2");
                    Dbug.EndLogging();
                }
                else if (testToRun == Tests.Dbug_DeactivateSessions)
                {
                    TextLine("The results of this test are recorded through Dbug.cs. Nothing to preview here...", Color.DarkGray);
                    Dbug.StartLogging("Enabled session 1");
                    Dbug.Log("Enabled log line A");
                    Dbug.LogPart("Enabled part-log line B // ");
                    Dbug.Log("Enabled log line C");
                    Dbug.EndLogging();
                    Dbug.DeactivateNextLogSession();
                    Dbug.StartLogging("Disabled session 1");
                    Dbug.Log("Disabled log line A");
                    Dbug.LogPart("Disabled part-log line B // ");
                    Dbug.Log("Disabled log line C");
                    Dbug.EndLogging();


                    Dbug.StartLogging("Enabled session 1");
                    Dbug.Log("Enabled log line A");
                    Dbug.LogPart("Enabled part-log line B // ");
                    Dbug.Log("Enabled log line C");
                    /// nested start
                    Dbug.StartLogging("Enabled nested session 1");
                    Dbug.Log("Enabled nested log line a");
                    Dbug.LogPart("Enabled nested part-log line b // ");
                    Dbug.Log("Enabled nested log line c");
                    Dbug.EndLogging();
                    /// nested end
                    Dbug.Log("Enabled log line D");
                    /// nested and deactivated start
                    Dbug.DeactivateNextLogSession();
                    Dbug.StartLogging("Disabled nested session 1");
                    Dbug.Log("Disabled nested log line a");
                    Dbug.LogPart("Disabled nested part-log line b // ");
                    Dbug.Log("Disabled nested log line c");
                    Dbug.EndLogging();
                    /// nested and deactivated end
                    Dbug.Log("Enabled log line E");
                    Dbug.EndLogging();
                }

                if (hasDebugQ)
                    TextLine("\n\n## Debug(s) to output have been ran ##", Color.Maroon);

                // end tests
                Pause();
                Clear();
            }
        }


        /* Just saving some preferences, y'know
            pref<cftag> Gry%Yllw%DrkGry%Rd%Mrn%Yllw%Wht%Gry%Orng
            pref<cftag> Squished%Thin
            logDec<cftag> C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\
         */

        // THOUGHTS...
        // I am able to set the Cursor position within the console window
        // With this, maybe I can truly create the kind of search page I have only dreamed about!
        // Console.CursorLeft and Console.CursorTop
    }
}
