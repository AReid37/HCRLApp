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
        static readonly string consoleTitle = "High Contrast Resource Library App";
        static readonly string developmentVersion = "[v1.2.7]";
        static readonly string lastPublishedVersion = "[v1.2.7]";
        /// <summary>If <c>true</c>, the application launches for debugging/development. Otherwise, the application launches for the published version.</summary>
        public static readonly bool isDebugVersionQ = true;
        static readonly bool verifyFormatUsageBase = true;

        #region fields / props
        // PRIVATE \ PROTECTED
        static string prevWhereAbouts;
        const string saveIcon = "▐▄▐▌"; //  1▐▀▀▄▄▌;    2▐▄▐▌;  3 ▐▄▄▌
        static bool _programRestartQ;
        static DataHandlerBase dataHandler;
        static Preferences preferences;
        static LogDecoder logDecoder;
        static ContentValidator contentValidator;
        static ResLibrary resourceLibrary;
        static SFormatterData formatterData;

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
                LogState($"Start Up - {consoleTitle + (isDebugVersionQ ? $" {developmentVersion} (debug)" : $" {lastPublishedVersion}")}");

                // setup                
                /// program function
                Console.Title = consoleTitle + (isDebugVersionQ? $" {developmentVersion} (debug)" : $" {lastPublishedVersion}");
                Tools.DisableWarnError = !isDebugVersionQ? DisableWE.All : DisableWE.None;
                /// data
                dataHandler = new DataHandlerBase();
                preferences = new Preferences();
                logDecoder = new LogDecoder();
                contentValidator = new ContentValidator();
                resourceLibrary = new ResLibrary();
                formatterData = new SFormatterData();
                LoadData();
                contentValidator.GetResourceLibraryReference(resourceLibrary);
                /// --v printing and pages
                VerifyFormatUsage = verifyFormatUsageBase && isDebugVersionQ;          
                GetPreferencesReference(preferences);
                ApplyPreferences();
                SettingsPage.GetPreferencesReference(preferences);
                SettingsPage.GetResourceLibraryReference(resourceLibrary);
                SettingsPage.GetContentValidatorReference(contentValidator);
                LogLegendNSummaryPage.GetResourceLibraryReference(resourceLibrary);
                LibrarySearch.GetResourceLibraryReference(resourceLibrary);
                GenSteamLogPage.GetResourceLibraryReference(resourceLibrary);
                GenSteamLogPage.GetSteamFormatterReference(formatterData);

                // testing site
                if (isDebugVersionQ)
                    RunTests();


                // Lvl.1 - title page and main menu
                /// home page               
                HomePage.OpenPage();
                Format($"{Ind24}Press [Enter] to continue >> ", ForECol.Normal);
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
                        /// ->  Log Legend View     --> Log Legends and Summaries
                        /// ->  Version Summaries   ^^
                        /// ->  Generate Steam Log
                        /// ->  Settings Page
                        ///     Quit

                        LogState("Main Menu");
                        Clear();
                        bool isValidMMOpt = ListFormMenu(out string mainMenuOptKey, "Main Menu", null, $"{Ind24}Option >> ", "a~f", true,
                            "Logs Submission, Library Search (Rudimentary), Log Legend and Summaries, Generate Steam Log, Settings, Quit".Split(", "));
                        MenuMessageQueue(mainMenuOptKey == null, false, null);

                        if (isValidMMOpt)
                        {
                            // other options
                            if (!mainMenuOptKey.Contains('f'))
                            {
                                // logs submission page
                                if (mainMenuOptKey.Equals("a"))
                                    LogSubmissionPage.OpenPage(resourceLibrary);
                                /// rudimentary library search page
                                else if (mainMenuOptKey.Equals("b"))
                                    LibrarySearch.OpenPage();
                                // log legend and summaries view page
                                else if (mainMenuOptKey.Equals("c"))
                                    LogLegendNSummaryPage.OpenPage();
                                // generate steam log page
                                else if (mainMenuOptKey.Equals("d"))
                                    GenSteamLogPage.OpenPage();
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
                                TextLine("\n\nExiting Program...");
                                Wait(0.4f);
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
            if (isDebugVersionQ)
                TextLine($"\n\n**REMEMBER** Test the published version of the application frequently!!\n\tVersion last tested: {lastPublishedVersion}".ToUpper(), Color.White);

            // report all warnings and errors into dbug file??  interesting idea....
            Dbug.StartLogging("Report Caught Errors and Warnings");
            #region report caught errors and warnings
            /// these test warning and error reporting
            //SetFileLocation("ffjjtqkx");
            //Title("jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj", '\0', -1);
            //Title("supercalifragilisticexpialidocious; was it spelled properly? hbfkelkfrlessiies", '\0', -1);


            /// report errors and warnings
            Dbug.Log($"Published version last tested in '{lastPublishedVersion}'; ");
            Dbug.Log("Reporting any recorded errors and warnings that have occured during this session :: ");
            string errorMsg = "0", warnMsg = "0";

            Dbug.Log("ERRORS");
            Dbug.NudgeIndent(true);
            for (int ex = 0; errorMsg.IsNotNE(); ex++)
            {
                errorMsg = Tools.GetWarnError(false, ex, true);
                if (errorMsg.IsNotNE())
                {
                    if (ex > 0)
                        Dbug.Log("-----");
                    Dbug.Log($"#{ex}  //  {errorMsg}");
                }
                else if (ex == 0)
                    Dbug.Log("No errors to report...");
            }
            Dbug.NudgeIndent(false);

            Dbug.Log("WARNINGS");
            Dbug.NudgeIndent(true);
            for (int wx = 0; warnMsg.IsNotNE(); wx++)
            {
                warnMsg = Tools.GetWarnError(true, wx, true);
                if (warnMsg.IsNotNE())
                {
                    if (wx > 0)
                        Dbug.Log("- - -");
                    Dbug.Log($"#{wx}  //  {warnMsg}");
                }
                else if (wx == 0)
                    Dbug.Log("No warnings to report...");
            }
            Dbug.NudgeIndent(false);

            #endregion
            Dbug.EndLogging();
        }


        public static void RequireRestart()
        {
            LogState("Requiring Program Restart");
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
        /// <param name="discreteQ">If <c>true</c>, will show the confirmation of saving or not in short form. Otherwise, a short sentence phrase is used for confirmation.</param>
        public static bool SaveData(bool discreteQ)
        {
            LogState("Saving Data");
            NewLine(2);
            Format($"{saveIcon}\t", ForECol.Accent);

            bool savedDataQ = dataHandler.SaveToFile(preferences, logDecoder, contentValidator, resourceLibrary, formatterData);            
            if (savedDataQ)
                Format(discreteQ? "auto-save: S." : "Auto-saving data ... success.", discreteQ? ForECol.Accent : ForECol.Correction);
            else Format(discreteQ? "auto-save: F." : "Auto-saving data ... failed.", discreteQ ? ForECol.Accent : ForECol.Incorrection);

            Wait(savedDataQ ? 2f : 5);
            return savedDataQ;
        }
        public static void LoadData()
        {
            LogState("Loading Data");
            bool outCome = dataHandler.LoadFromFile(preferences, logDecoder, contentValidator, resourceLibrary, formatterData);
            Dbug.SingleLog("Program.LoadData()", $"Outcome of data loading :: {outCome};");
        }
        public static bool SaveReversion()
        {
            return dataHandler.RevertSaveFile();
        }
        
        public static void ToggleFormatUsageVerification()
        {
            if (isDebugVersionQ && verifyFormatUsageBase)
            {
                if (VerifyFormatUsage)
                {
                    VerifyFormatUsage = false;
                    //TextLine("#'Verify Format Usage' has been disabled.", Color.DarkGray);
                    //Text(" [VFU:off] ", Color.DarkGray);
                }
                else
                {
                    VerifyFormatUsage = true;
                    //Text(" [VFU:on] ", Color.DarkGray);
                    //TextLine("#'Verify Format Usage' has been re-enabled.", Color.DarkGray);
                }
            }            
        }


        // TESTING STUFF
        static readonly bool runTest = false;
        static readonly Tests testToRun = Tests.MiscRoom;
        enum Tests
        {
            /// <summary>For random tests that need their own space, but no specific test name (variable tests)</summary>
            MiscRoom, 

            PageBase_HighlightMethod,
            PageBase_ListFormMenu,
            PageBase_Wait,
            PageBase_TableFormMenu,
            PageBase_ColorMenu,

            Extensions_SortWords,

            LogDecoder_DecodeLogInfo,
            LogSubmissionPage_DisplayLogInfo_Ex1,
            LogSubmissionPage_DisplayLogInfo_Ex3,
            LogSubmissionPage_DisplayLogInfo_AllTester,
            LogSubmissionPage_DisplayLogInfo_ErrorTester,

            ContentValidator_Validate,

            Dbug_NestedSessions,
            Dbug_DeactivateSessions, 

            SFormatter_ColorCode,
            SFormatter_CheckSyntax,

            None
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
                else if (testToRun == Tests.LogSubmissionPage_DisplayLogInfo_Ex1 || testToRun == Tests.LogSubmissionPage_DisplayLogInfo_Ex3 || testToRun == Tests.LogSubmissionPage_DisplayLogInfo_AllTester || testToRun == Tests.LogSubmissionPage_DisplayLogInfo_ErrorTester)
                {
                    hasDebugQ = false;
                    TextLine("Displays information from a log decoder", Color.DarkGray);

                    bool locationSet;
                    if (testToRun == Tests.LogSubmissionPage_DisplayLogInfo_Ex1)
                        locationSet = SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex1 Version Log.txt");
                    else if (testToRun == Tests.LogSubmissionPage_DisplayLogInfo_Ex3)
                        locationSet = SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Ex3 Version Log.txt");
                    else if (testToRun == Tests.LogSubmissionPage_DisplayLogInfo_ErrorTester)
                        locationSet = SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - Error Tester Version Log.txt");
                    else locationSet = SetFileLocation(@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\HCRLA - All-Tester Version Log.txt");

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
                
                /// Content Validator
                else if (testToRun == Tests.ContentValidator_Validate)
                {
                    TextLine("Preparing to run CIV process (noted in Dbug)");
                    string[] folderPaths =
                    {
                        @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField",
                        @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents",
                        @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\ATVL",
                        @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\Ex1VL",

                        //@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\Ex2VL",
                        //@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\Ex3VL",
                        //@"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\Ex4VL",
                    };
                    string[] fileExtensions =
                    {
                        ".txt", ".tst", ".expc", /*".nfe"*/
                    };
                    const string folderPathEnd = "<<";

                    Text($"CIV Parameters :: \n{Ind14}Version Range :: (");
                    /// FOLDER PATHS FOR TESTING
                    /// Non-Existent Main Folder:
                    ///     @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\TheNonExistent"
                    /// 
                    /// Inaccessible Main Folder:
                    ///     @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\NoAccess\Inaccessible"
                    ///     
                    /// Main Folder with Inaccessible Sub-Folder:
                    ///     @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\NoAccess"
                    ///     
                    /// Main Folder with Inaccessible File:
                    ///     @"C:\Users\ntrc2\source\repos\HCResourceLibraryApp\TextFileExtras\CIVTestingField\ExContents\NoAccess\AccessibleContainingInaccessible"
                    ///     
                    /// ---------------------
                    /// ORIGINAL VALUE
                    ///     folderPaths[Extensions.Random(0, folderPaths.Length - 1)]
                    ///     
                    string aFolderPath = /*ContentValidator.FolderPathDisabledToken +*/ folderPaths[Extensions.Random(0, folderPaths.Length - 1)];
                    string aFileExtension = fileExtensions[Extensions.Random(0, fileExtensions.Length - 1)];
                    VerNum[] verRange = null;
                    if (resourceLibrary.IsSetup())
                    {
                        if (resourceLibrary.GetVersionRange(out VerNum low, out VerNum high))
                        {
                            if (low.Equals(high))
                            {
                                verRange = new VerNum[1] { low };
                                Text(low.ToString());
                            }
                            else
                            {
                                /// IF ..: (IF ..: choose one from range; ELSE create range between ranges); ELSE full version range
                                if (Extensions.Random(0, 2) == 1)
                                {
                                    if (Extensions.Random(0, 1) == 0)
                                        verRange = new VerNum[1] { new VerNum(Extensions.Random(low.AsNumber, high.AsNumber)) };
                                    else 
                                        verRange = new VerNum[2]
                                        {
                                            new VerNum(Extensions.Random(low.AsNumber, (low.AsNumber + high.AsNumber) / 2)), 
                                            new VerNum(Extensions.Random((low.AsNumber + high.AsNumber) / 2, high.AsNumber))
                                        };
                                }
                                else
                                    verRange = new VerNum[2] { low, high };

                                if (verRange.HasElements(2))
                                    Text($"{verRange[0]}, {verRange[1]}");
                                else Text(verRange[0].ToString());                 
                            }
                        }
                    }
                    TextLine($")\n{Ind14}Relative Folder Path :: {(aFolderPath + folderPathEnd).Clamp(50, "~", folderPathEnd, false).Replace(folderPathEnd, "")}");
                    TextLine($"{Ind14}File Extension :: {aFileExtension}");
                    Text($"Press [Enter] to run CIV", Color.DarkGray);
                    Pause();
                    NewLine();


                    if (contentValidator.Validate(verRange, new string[1] { aFolderPath }, new string[1] { aFileExtension }))
                    {
                        NewLine(2);
                        for (int vx = 0; vx < 3; vx++)
                        {
                            SettingsPage.CivDisplayType displayType = (SettingsPage.CivDisplayType)vx;

                            Important($"CIV Results - {displayType} View");
                            HorizontalRule('-');
                            SettingsPage.DisplayCivResults(contentValidator.CivInfoDock, displayType);
                            HorizontalRule('-');

                            Pause();
                            NewLine(5);
                        }
                        #region old code
                        //TextLine("Post-CIV results; [Validated in green, Invalidated in Red]");
                        //if (contentValidator.CivInfoDock.HasElements())
                        //{
                        //    int nli = 0;
                        //    foreach (ConValInfo cvi in contentValidator.CivInfoDock)
                        //        if (cvi.IsSetup())
                        //        {
                        //            if (nli >= 3)
                        //            {
                        //                NewLine();
                        //                nli = 0;
                        //            }
                        //            Text($"{cvi.DataID} ({cvi.OriginalDataID}){Ind24}", cvi.IsValidated ? Color.Green : Color.Red);
                        //            nli++;
                        //        }
                        //}
                        //Pause();
                        //NewLine(2);
                        //TextLine("Do not continue usage of application after running this test!", Color.Orange);
                        #endregion
                    }
                    else TextLine("CV did not have enough data to run CIV process...");

                    NewLine(2);
                    TextLine("Do not continue usage of application after running this test!", Color.Orange);
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

                /// SFormatter
                else if (testToRun == Tests.SFormatter_ColorCode)
                {
                    hasDebugQ = false;
                    TextLine("Color coding testing of SFormatter.ColorCode() method");
                    NewLine();

                    /** Language sytnax snippet (General)
                        Language Syntax
                        > General
                            Handled by SFormatterHandler.cs
                            syntax          outcome
                            _________________________________
                            // abc123       line comment. Must be placed at beginning of line
                            abc123          code
                            "abc123"        plain text
                            &00;            plain text '"'
                            {abc123}        library reference
                            $abc123         steam formatting reference
                            if # = #:       keyword, control; compares two given values to be equal. Prints following line if condition is true (values are equal). Placed at start of line.
                            else:           keyword, control; Prints following line if the condition of an immediately preceding 'if # = #' is false (values are not equal). Placed at start of line.
                            repeat #:       keyword, control; repeats line '#' times incrementing from one to given number '#'. Any occuring '#' in following line is replaced with this incrementing number. Placed at start of line.
                    */
                    string[] lines = new string[]
                    {
                        /// comments
                        "// this is a comment",
                        "  // even if indented...",

                        /// code
                        " -- // not a comment",
                        "this is code",


                        /// plain text
                        "\"plain text\"",
                        "\"plain\" code \"plain\" and code",
                        "\"plaintext\"codestuff",
                        "codestuff\"plaintext\"",

                        /// plain text w/ escape
                        "\"&00;\"",
                        "&00; \"plain text & &00; escape\" &00;",

                        /// keywords (if, else, repeat)
                        "if keyword here",
                        "else keyword here",
                        "repeat keyword here",
                        "Still keywords when if else repeat",
                        "Not keywords when \"if else repeat\"",
                        "repeat special when '#' or \"'#'\"",
                        "2# New keywords: jump next",

                        /// operators (=)
                        "Operator equal is '='",
                        "This \"=\" is not operator equal",
                        "Operator unequal is '!='",
                        "This \"!=\" is not operator unequal",

                        /// references (library, steam)
                        "{this} is library reference",
                        "$this is steam reference",
                        "these $can be {placed} anywhere",
                        "\"{this}\" is not a \"$reference\"",
                        "{the$se} have equal $prece{dence}",

                        /// precedence list
                        "Precedences (in descending order)",
                        "comment, plain, escape, reference,",
                        "keyword, operator, code"
                    };

                    Table2Division tDiv = Table2Division.Even;
                    char div = '.';
                    for (int lx = 0; lx < lines.Length; lx++)
                    {
                        string data1 = lines[lx];
                        
                        // header
                        if (lx == 0)
                        {
                            TableRowDivider(true);
                            TableRowDivider('-', true, null);
                            Table(tDiv, "INPUT", div, "OUTPUT");
                            TableRowDivider(false);
                        }
                        
                        if (data1.IsNotNE())
                        {
                            HoldNextListOrTable();
                            Table(tDiv, data1, div, null);
                            if (LatestTablePrintText.IsNotNE())
                            {
                                Text(LatestTablePrintText.Replace("\n", ""));
                                SFormatterHandler.ColorCode(data1, true);
                                NewLine();
                            }
                        }
                    }
                }
                else if (testToRun == Tests.SFormatter_CheckSyntax)
                {
                    TextLine("Testing syntax checking of SFormatter.CheckSyntax() method");

                    NewLine();
                    bool displayAllMessageQ = true; /// not 'const' to avoid 'unreachable code' issue
                    const string secHeader = "|header|", secBreak = "|break|", lineRep = "%%%%%%%", firstOnlyIssueFix = "Post '1st Only' Fix";
                    string skipToHeaderStartingAs = $"{secHeader}Key";
                    string[] lines = new string[]
                    { /// enter many incorrect entries, and at least one correct entry

                        // GENERAL SYNTAX CHECKING
                        /// Code errors [000]
                        $"{secHeader}Code Errors [000]",
                        "/ \"comment not\"", "\"the\" /", "\"what\" // \"oh no\"", "// good... goooood!!", /// comments
                        "\"butter", "butter\"", "\"butter\" \"", "\"lemon\"", /// plain text  
                        "&", "&;", ";", "& 00;", "; &", "\"&;&\"", "\"&;;\"", "\"&00;\"", /// escape character
                        $"{secBreak}{firstOnlyIssueFix} (next 3)", "\"&00; &;\"", "\"&00; &rs;\"", "\"&00; &00;\"",
                        "{", "}", "{{}", "{}}", "{ }", "{    }", "} tta {", "}{}{", "{::,}", "{,:,}", "{:,}", "{:Version}", "{TTA}", "{Added:2,name}", /// lib ref
                        "$", "$ h q", "q$", "$$i", "$[]", "$][", "]$[", "[$]", "]$", "[ $", "[", "]", "[]", "][", "[ ]", "] [", "$h", "$list[]", /// steam ref 
                        $"{secBreak}{firstOnlyIssueFix} (next 5)", "2 [ ]", "3 ] [", "$list[]]", "$table[] [  ]", "$list[]   ] [",
                        "if ", "if   ;", "if;", ";if", "if 0 = 0; if ;", "if 3 = 4;;", "if 1 = 0; \"no\";", "if 1 = 1; \"if\"", /// if keyword
                        "else", "else ;", "else \" \"", "; else", "else; ;", "if 0 = 0; \"else\"", "else; \"if\"", /// else keyword
                        "repeat", "repeat;", "repeat  ;", "; repeat", "repeat 1;;", "#", "repeat 2; \"rep\"#", /// repeat keyword
                        "jump", "jump;", "jump ;", "; jump", "jump ;; ;", "jump r;; 0;", "if 1 = 1; jump 200; \"33\"", /// jump keyword
                        "next", "next;", "next ;", "; next", "next ;; ;", "next \" \" ;", "if 1 = 1; next;", "\"bravo!\"", /// next keyword
                        /// Plain Text Errors [001-002]
                        $"{secHeader}Plain Text Errors [001-002]",
                        "\"\"", "\"butter", "shea \"butter", "\"butter\"",
                        /// Escape Character Errors [003]
                        $"{secHeader}Escape Character Errors [003]",
                        "\"&;\"", "\"&  ;\"", "\"&01;\"", "\"&abxr;\"", "\"&00;\"",
                        /// Library Reference Errors [004-006]
                        $"{secHeader}Library Reference Errors [004-006]",
                        "{melon", "{}", "{    }", "{ nuke }", "{TTA} { nuke'em }", "{ Version }", $"{{Addit:1,ids}}",
                        /// Steam Format Reference Errors [007-008]
                        $"{secHeader}Steam Format Reference Errors [007-008]",
                        "$ ", "$carl", "$urll", "$hh $tible", "$hhhh", "$", "$nl", "$i $u \" \"",
                        /// Keyword errors [009-023,076-081]
                        $"{secHeader}Keyword Errors [009-023,076-081]",
                        "if 1 = 1", "else", "repeat 2", "if 2 != 2; \"#ok\"", /// general keyword - no end colon  
                        "$i if", "\"plant\" else", "else else;", "\"plant\" repeat", "repeat; repeat", "if 8 = 8; if 1 = 2; \"Go!\"", /// general keyword - keyword at front
                        "if 0 = 0;", "else;  ", "repeat 2; ", /// general keyword - missing execution line
                        "if 1 != 0; \"plant\" if 0 = 0;", "else; \"plant\" jump 256;", "repeat 3; \"u\" next;", /// general keyword - misplaced if / jump / next
                        "else; if1=2; jump4; else;", "repeat1; if2=3;next;jump9;", "if 0 = 1; if 2 != 3; if 0 = 0;", /// general keyword - exceed keyword limit
                        $"{secBreak}If", "if", "if ; 2", "if $i; 0", "if", "if {CTA};", "if 4 = 1; if \"no\" = 2; 0", "if {Added:2,name}; 0", /// if - first value expected
                        "if 4 =! 4; 0", "if 0 = 2; if 4 == 10;", "if !! ;", "if 0 = 1; 0", "if # != 1; 1", /// if - op expected / op invalid
                        "if 9 = q; 2", "if 7 != $t; ", "if {tta} == 232; ", "if 3 = 2; if q != r; 1", "if {SummaryCount}=5; if2=2; \"#yes\"", /// if - second value expected
                        $"{secBreak}If (New Ops)", "if 2 >> 1;", "if 3 <== 1;", "if 4 <>!;", "if 0 !< 4; ?", "if {Added} > 3;", "if 0 < 1; if \"three\" <= 4;", "if 1 = 1; if \"4\" >= 2;", "if {AddedCount} < {AdditCount}; \"b\"", "if 0 < 1; $d", "if 1 > 0; $d", "if 2 >= 1; $nl", "if 3 <= 5; $nl", /// new operators (> >= < <=): non-numeric value / op invalid\expected
                        $"{secBreak}Else","else; \"no\"", "repeat 1; if 0 = 2; \"uh\"", "else; \"nope\"", "if 0 = 0; if 1 = 1; \"yup\"", "else; if 2 = 3; \"yep\"", "else; \"mhmm\"", /// else
                        $"{secBreak}Repeat","repeat;", "repeat ;", "repeat \"w\"; \"no\"", "repeat r; 4", "repeat 0; 10", "repeat {Added:#,ids}; \"l\"", "repeat \"8\"; {TTA}", "repeat 4; \"he\"", "repeat {AddedCount}; \"8\"", "repeat 13; if # != 1; \"donzo\"", /// repeat
                        $"{secBreak}Jump","jump ;", "jump w;", "jump \"13\"", "jump {TTA};", "jump 20;", "repeat 4; jump 300;", "if 0 = 0; jump 300;", "else; jump 350;", /// jump
                        $"{secBreak}Next", "next ;", "next w;", "// e?", "repeat 2; next;", " ", "if 1 = 1; next;", "else; next;", "if 0 = 0; next;", "\"b\"", /// next
                        


                        // LIBRARY REFERENCE ERRORS
                        /// LR Array Errors [024-037]
                        $"{secHeader}LR Added Arr Errors [024-026]",
                        $"{secBreak}Singles (no errors)", "{Version}", "{AddedCount}", "{AdditCount}", "{TTA}", "{UpdatedCount}", "{LegendCount}", "{SummaryCount}",
                        $"{secBreak}Added Arr", "{Added}", "{Added  }", "{Added:}", $"{{Added:,}}", "{Added:#,}", "{Added:,ids}", "{Added:0,name}", "{Added:w,prop}", "{Added:13,ids}", /// added array
                        $"{secBreak}Addit Arr", "{Addit}", "{Addit  }", "{Addit:}", $"{{Addit:,}}", "{Addit:#,}", "{Addit:,ids}", "{Addit:0,name}", "{Addit:c,prop}", "{Addit:10,optionalName}", /// addit array
                        $"{secBreak}Updated Arr", "{Updated }", "{Updated:}", $"{{Updated:,}}", "{Updated:#,}", "{Updated:,id}", "{Updated:0,id}", "{Updated:t,prop}", "{Updated:8,name}", /// updated array
                        $"{secBreak}Legend Arr", "{Legend}", "{Legend:}", $"{{Legend:,}}", "{Legend:#,}", "{Legend:,key}", "{Legend:0,key}", "{Legend:u,prop}" , "{Legend:18,definition}", /// legend array
                        $"{secBreak}Summary Arr", "{Summary}", "{Summary  }", "{Summary:}", "{Summary,}", $"{{Summary:0}}", "{Summary:f}", "{Summary:#}", $"{{Summary:7}}", /// summary array 
                        $"{secBreak}{firstOnlyIssueFix}", /// first only issue fix check, for all
                        "{Added:1,name} {Added:,}", "{Added:2,ids} {Added:prop,2}", "{Added:3,ids} {Added:3,name}", /// added arr
                        "{Addit:1,ids} {Addit:,}", "{Addit:2,ids} {Addit:prop,2}", "{Addit:3,optionalName} {Addit:3,ids}", /// addit arr
                        "{Updated:1,id} {Updated}", "{Updated:2,id} {Updated:prop,2}", "{Updated:3,id} {Updated:3,name}", /// updated arr
                        "{Legend:1,key} {Legend:}", "{Legend:2,key} {Legend:prop,2}", "{Legend:3,key} {Legend:3,definition}", /// legend arr
                        $"{{Summary:1}} {{Summary }}", $"{{Summary:2}} {{Summary:q}}", $"{{Summary:3}} {{Summary:3}}", /// summary arr



                        // STEAM FORMAT REFERENCE ERRORS
                        /// Simple Command Errors [038-044]
                        $"{secHeader}Simple Command Errors [038-044]",
                        "$i", "$np  ", "$u $i  ", "$b x", "$b $i wxy", "$b \"SOUP!\" $s", "$i \"thicc\" $i $u", "$b \"txt\"", "$i $u \"nice\"", "\"sm\" $u \"1\"", "\"cool..\" $i \"bro\"", "$nl", "$hr", "$i {TTA} \"woah\"", "$u $b \"slapper\" {TTA}", /// simple - missing \ invalid value
                        "\"e\" $h", "\"s\" $hhh", "$hh $i \"l\"", "$hh $u $s", "$h $i", "$hhh \"llama\"", "$hh \"snek\"", /// heading element
                        "\"el\" $hr", "$hr \"mlem\"", "$hr $nl", "$hr", /// hor'z.rule
                        "\"napkin?\" $*", "\"bagel\"", "$*", "$list[]", "$* \"saucy!\"", /// list item                        
                        /// URL Errors [045-049]
                        $"{secHeader}URL Errors [045-049]",
                        "\"goof\" $url", "$url $i \"nubb\"", $"$url= $u \"smelly\"", "$url", "$url =", "$url=  ", "$url= =", /// own line / operator? / empty / UnExTk ('=')
                        "$url= w", "$url= :smek", "$url= # : ey", "$url= \"goob.com\" : w?", "$url= \"lmn\" : 3:", /// link? / name? / UnExTk(':')
                        "$url= \"w3.goob.com\":\"Goobers\"", "$url= \"https:// grubSnax/quirkyChips.com\":\"GrubSnax!\"",
                        /// List Errors [050-053]
                        $"{secHeader}List Errors [050-053]", 
                        "$list $i", "\"th\" $list", "$list", "$list[", "$list []", "$list[ ]", "$list[o]", "$list[ol]", "$list[or]", "$list[]", "$* \"sweet\"",
                        /// Quote Errors [054-058]
                        $"{secHeader}Quote Errors [054-058]",
                        "\"then\" $q", "$q $q", "$q= $u water", "$q", "$q =", "$q=", "$q= =", "", /// own line / operator? / empty / UnExTk ('=') 
                        "$q= x", "$q= :mop", "$q= #: yup", "$q= \"me\": poop", "$q= \"b\":\"c\":\"d\"",  /// author? / quote? / UnExTk(':')
                        "$q=\"Cow1: Cowsong\":\"moo! moo! moooo...\"", 
                        /// Table Errors [059-063]
                        $"{secHeader}Table Errors [059-063]",
                        "\"i\" $table", "$table $nl", "$table []", "$table[", "$table ]", /// own line / params expected
                        "$table[tr]", "$table[l, ]", "$table[ec,]", "$table[ ,nb]", "$table[,,]", /// invalid params / UnExTk (',') / two or less params / table row?
                        "$table[ec,nb]", "$td=\"one\"", 
                        /// Table Header Errors [064-069]
                        $"{secHeader}Table Header Errors [064-069]",
                        "$th $i ww", "\"apl\" $th", "$th =", "$th= =", "$th=", /// own line / operator? / empty / UnExTk ('=')
                        "$th=\"grub\"", "$table[]", "$th= , weep", "$th= \"er\", um, stop", "$th= \"no\"", /// not after\in table block / invalid value
                        "$table[]", "$th= \"Folder\", \"Path, Type\"", 
                        /// Table Data Errors [070-075]
                        $"{secHeader}Table Data Errors [070-075]",
                        "$hr $td", "\"cope\" $td", "$td =", "$td==", "$td=   ", /// own line / operator? / empty / UnExTk ('=')
                        "\"lmn\"",  "$td=\"soup\", goop", "$td= puke, hard", "$td= #, ",/// not in table block / invalid value
                        "$td= \"uno\"", "$td= \"1\", 2, \"three\"", "$td= \"dos\", 2", /// mismatch columns
                        "$table[]", "$td= \"That's all folks!\"", "$td= \"kinda...\"",

                        // Code Errors - Unexpected Token 'anything else?'
                        $"{secHeader}UnExAny Code Errors [000]", 
                        "abc", "123", "kay that's all", "\"kek..\"",


                        /// MIXED TESTS
                        $"{secHeader}Mixed Tests",
                        $"{secBreak}3rd Next / Jump", 
                        "if0=0; if1=1; next;", "else; if0!=1; next;", "repeat4; if1=1; next;", "if0!=1; if2=2; jump410;", "else; if0=0; jump420;",
                        $"{secBreak}Next within Lists/Tables", 
                        "$list[]", "\"nope\"", "\"also nope\"", "$list[]", "if 0 = 0; next;", "$* \"First items\"", "repeat 3; next;", "$* \"Next item #\"", "if 0 = 0; \"break list\"", "$* \"Lost item\"", /// list
                        "$table[]", "\"nope\"", "\"also nope\"", "$table[]", "if0=0; next;", "$th= \"1st\"", "if 1 = 1; next;", "$td= \"slap\",\"slap\"", "repeat 1; \"break it\"", "$td= \"sad\"", "if 0 = 0; next;", "$th= \"numb\"", "\"break again\"", "$th= \"sad^2\"",  /// table
                        
                        

                    };

                    SFormatterHandler.CheckSyntax(lines);
                    Table2Division tDiv = Table2Division.KCSmall;

                    bool skippingSections = skipToHeaderStartingAs.Substring(secHeader.Length).IsNotNEW();
                    const char div = '|';
                    for (int lx = -1; lx < lines.Length; lx++)
                    {
                        if (lx >= 0)
                        {
                            string data1 = lines[lx];
                            int lineNum = lx + 1;

                            if (data1.IsNotNEW())
                            {
                                if (skippingSections && data1.StartsWith(skipToHeaderStartingAs))
                                    skippingSections = false;

                                if (!skippingSections)
                                {
                                    if (!data1.StartsWith(secHeader))
                                    {
                                        if (!data1.StartsWith(secBreak))
                                        {
                                            SFormatterInfo[] errors = SFormatterHandler.GetErrors(lineNum);
                                            if (errors.HasElements())
                                            {
                                                string data2 = "";
                                                for (int x = 0; x < errors.Length; x++)
                                                {
                                                    SFormatterInfo sfi = errors[x];
                                                    if (!displayAllMessageQ)
                                                    {
                                                        if (x == 0)
                                                            data2 = $"{sfi.errorCode} - {sfi.errorMessage} ";
                                                        if (x != 0)
                                                            data2 += $" +{sfi.errorCode}";
                                                        if (x + 1 == errors.Length)
                                                            data2 += $" {lineRep}";
                                                    }
                                                    else
                                                    {
                                                        HoldNextListOrTable();
                                                        Table(tDiv, x == 0 ? data1 : "", div, $"{(x == 0 ? "" : Ind14)}{sfi.errorCode} - {sfi.errorMessage} {(x == 0 ? lineRep : "")}");
                                                        PrintTable();
                                                    }
                                                }
                                                if (!displayAllMessageQ)
                                                {
                                                    HoldNextListOrTable();
                                                    Table(tDiv, data1, div, data2);
                                                    PrintTable();
                                                }
                                            }
                                            else
                                            {
                                                HoldNextListOrTable();
                                                Table(tDiv, data1, div, "--");
                                                PrintTable();
                                            }

                                            void PrintTable()
                                            {
                                                if (LatestTablePrintText.IsNotNE())
                                                    Text(LatestTablePrintText.Replace(lineRep, $"   L{lineNum,-3}"), lineNum % 2 == 0 ? Color.Gray : Color.Yellow);
                                            }

                                        }
                                        else
                                        {
                                            TextLine($"----- -- {data1.Replace(secBreak, "")} -- -----", Color.DarkGray);
                                        }

                                    }
                                    else
                                    {
                                        Pause();
                                        NewLine(1);
                                        Important(data1.Replace(secHeader, ""));
                                        TableRowDivider(true);
                                        TableRowDivider('_', true, Color.DarkGray);
                                        Table(tDiv, $"LINE    (#{lineNum}+)", div, "ERRORS");
                                        TableRowDivider(false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            NewLine();
                            TextLine("Syntax Checking Tools have been tested, please check the debug.", Color.Orange);
                            SFormatterHandler.TestSyntaxCheckTools();
                            HorizontalRule('_', 2);
                        }
                    }

                }

                /// Misc Room
                else if (testToRun == Tests.MiscRoom)
                {
                    char miscKey = 'd';
                    string miscTestName = "<None>";
                    switch (miscKey)
                    {
                        case 'a':
                            miscTestName = "Settings Page: Create Numeric Data ID Ranges";
                            break;

                        case 'b':
                            miscTestName = "Log Decoder: Compare Non-wordy Data ID Parsing Functions";
                            break;

                        case 'c':
                            miscTestName = "Extensions: str.Clamp(4 params) Variations";
                            break;

                        case 'd':
                            miscTestName = "Page Base: Word Wrap Testing";
                            break;
                    }

                    /// DON'T DELETE THIS HEADER | provide test name
                    TextLine(miscTestName, Color.DarkGray);

                    #region miscA: settingsPage: CreateNumericDataIDRanges
                    if (miscKey == 'a')
                    {
                        Color resultCol = Color.Yellow, difficultyCol = Color.NavyBlue;

                        TextLine("Lvl1 - Simple ranges", difficultyCol);
                        string numbers = "0 1 2 4 6 10 12 13 14 16 17 18 19 24 26 27 29 30 31 33 35 36 37";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        numbers = "20 22 30 31 32 33 34 35 36 37 39 40 41 42 43 45 46 48 57 58 60 61 62 64";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        numbers = "56 57 59 60 61 64 66 69 70";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        TextLine("Lvl2 - Odd numbers, imparsable numbers", difficultyCol);
                        numbers = "73 74 75 78 78_2 79 81 83-wet 86 87_3 88 89 94 95 96 97 97_01 98 99 100 102 103 103_0 104 105";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        numbers = "89_q 90 91 92 95 96 96_2 97 98 99 99_alt 99_alt2 100 101 104 105 106 107_4";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        TextLine("Lvl3 - Ranges within the imparsables", difficultyCol);
                        numbers = "109 110 111 111_0 111_1 113 116 117 117_0 117_1 117_2 117_3 117_4 121 122 124 126 127 128";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        numbers = "124_0 125_0 125_1 125_2 125_3 126_1 128_2 131_0 131_1 131_3 131_4 133_0 141_0 141_1 141_2";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);

                        NewLine(2);
                        numbers = "u138 u139 u140 u146 u157 u158 u160 x200 x201 x202 x202_0 x202_2 x202_3 x202_4 x204 x205 x215 x216 x217";
                        TextLine($"Creating range from numbers:\n\t{numbers}");
                        TextLine($"  Result: {Extensions.CreateNumericDataIDRanges(numbers.Split(" "))}", resultCol);
                    }
                    #endregion

                    #region miscB: logDecoder: CompareNon-WordyDataIDParsingFunctions
                    if (miscKey == 'b')
                    {
                        hasDebugQ = false;
                        Color oldPCol = Color.Red, newPCol = Color.Blue;
                        string dataIDsToParse = "i14` q32 pe566* Cod_Tail Shark_Fin` tt4_0 tb5-gross* slap1 slap2 x7_small n113 n113_alt_0` s_alt4 isOkay8`` i` 4* 8 *`^";

                        TextLine($"Each data ID to parse follows a format:\n\tOriginal  |  OldParsingResult [in {oldPCol}] | NewParsingResult [in {newPCol}]");
                        TextLine($"{Ind24}Old Method: LogDec.GetDataKeyAndSuffix() as 'dataKey[number]suffix'");
                        TextLine($"{Ind24}New Method: LogDec.DisassembleDataID() as 'dataKey[dataBody]suffix'");
                        NewLine();

                        const string ndr = " "; // no data replace
                        if (dataIDsToParse.IsNotNE())
                            foreach (string parsingDataID in dataIDsToParse.Split(' '))
                            {
                                if (parsingDataID.IsNotNE())
                                {
                                    // og
                                    Text($"{parsingDataID,-12} |  ");

                                    // old parsing
                                    LogDecoder.GetDataKeyAndSuffix(parsingDataID, out string oDK, out string oSF);
                                    string oNM = parsingDataID;
                                    if (LogDecoder.IsNumberless(parsingDataID))
                                        oNM = null;
                                    if (oNM.IsNotNE() && oDK.IsNotNE())
                                        oNM = oNM.Replace(oDK, "");
                                    if (oNM.IsNotNE() && oSF.IsNotNE())
                                        oNM = oNM.Replace(oSF, "");
                                    string oldText = $"{(oDK.IsNE() ? ndr : oDK)}[{(oNM.IsNE() ? ndr : oNM)}]{(oSF.IsNE() ? ndr : oSF)}";
                                    Text($"{oldText,-15}", oldPCol);

                                    // div
                                    Text(" | ");

                                    // new parsing
                                    LogDecoder.DisassembleDataID(parsingDataID, out string nDK, out string nDB, out string nSF);
                                    Text($"{(nDK.IsNE() ? ndr : nDK)}[{(nDB.IsNE() ? ndr : nDB)}]{(nSF.IsNE() ? ndr : nSF)}", newPCol);

                                    // next id
                                    NewLine();
                                }
                            }
                    }
                    #endregion

                    #region miscC: extensions: str.Clamp(4 params)variations
                    if (miscKey == 'c')
                    {
                        hasDebugQ = false;
                        Color colLeft = Color.Red, colMid = Color.Green, colRight = Color.Blue;
                        /// "words", "focusedWord", "#", "suffix"
                        string[,] testString = new string[,]
                        {
                            {"Cabbage Rolls are (not) the best", "are", "5", "" },
                            {"An apple a day keeps the doctor away", "apple", "7", "~" },
                            {"I should be asleep right now :(", "sleep", "9", "~"},
                            {"This is not the end of it", "of", "5", "~"},
                            {"Racecar is racecar", "is", "6", ""},
                            {"The difference between tomatoes and potatoes", "tom", "7", ""},
                            {"Surely you have not slapped yourself in the face", "slapped", "8", "...."},
                            {"moleson, matts and marks and maisons", "and", "8", ".."},
                            {"Ten carrots", "c", "5", "--"},
                            {"Mut hut", " ", "2", ".:.:."},
                            {"Checking from the start", "Ch", "10", "..." },
                            {"Looking towards the end", "nd", "10", "..."},
                        };

                        TextLine($"Spread of '[distance]' from '[focusWord]' with clamper '[suffix]' [darkGray]\nOG [white]{Ind34}\n\tLeft [{colLeft}] | Middle [{colMid}] | Right [{colRight}]");
                        NewLine();

                        if (testString.HasElements())
                        {
                            for (int tsx = 0; tsx < testString.GetLength(0); tsx++)
                            {
                                string ogWords = (string)testString.GetValue(tsx, 0);
                                string focusWd = (string)testString.GetValue(tsx, 1);
                                string distnce = (string)testString.GetValue(tsx, 2);
                                string cSuffix = (string)testString.GetValue(tsx, 3);

                                if (ogWords.IsNotNE() && focusWd.IsNotNE() && int.TryParse(distnce, out int distNum))
                                {
                                    NewLine();
                                    TextLine($"Spread of '{distnce}' from '{focusWd}' with clamper '{cSuffix}'", Color.DarkGray);
                                    TextLine($"{ogWords}");

                                    string fc_left = ogWords.Clamp(distNum, cSuffix, focusWd, false);
                                    string fc_midl = ogWords.Clamp(distNum, cSuffix, focusWd, null);
                                    string fc_rght = ogWords.Clamp(distNum, cSuffix, focusWd, true);

                                    HoldNextListOrTable();
                                    Table(Table3Division.Even, fc_left, cLS, fc_midl, fc_rght);
                                    if (LatestTablePrintText.IsNotNE())
                                    {
                                        string[] tablePrints = LatestTablePrintText.Split(cLS);
                                        Text(tablePrints[0], colLeft);
                                        Text("|");
                                        Text(tablePrints[1], colMid);
                                        Text("|");
                                        Text(tablePrints[2], colRight);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region miscD: pageBase: wordWrapTesting
                    if (miscKey == 'd')
                    {
                        hasDebugQ = false;
                        Color wrapLevelCol = Color.NavyBlue, wrapOff = Color.Gray;
                        ForECol wrapOn = ForECol.Highlight;

                        TextLine("The narrowest window settings required for this test.", Color.Forest);
                        TextLine($"Word Wrapping off [{wrapOff}]; Word Wrapping On [{GetPrefsForeColor(wrapOn)}];");
                        NewLine(2);

                        bool endExamples = false;
                        for (int ex = 0; !endExamples; ex++)
                        {
                            char divChar = '\0';
                            string wrapLevelText = "", exampleText = "";
                            bool enableFormatOverloadQ = false;
                            switch (ex)
                            {
                                case 0:
                                    wrapLevelText = "Determine Wrapping Usage";
                                    exampleText = "On basis of resource control: not every text requires wrapping; exhibit A.";
                                    break;
                                //                |-                   -                         -                        -          -| <--  clipped here (Ch:135 cuts to next line)
                                case 1:
                                    wrapLevelText = "Wrap Clipped Word to Next Line";
                                    exampleText = "The first level wrapping test only requires a clipped word to be wholly displayed upon the next line.";
                                    break;
                                //                |-                   -                         -                        -          -|
                                case 2:
                                case 3:
                                    wrapLevelText = "Wrap Word and Indent on Next Line";
                                    if (ex == 2)
                                        exampleText = "\tThere is a small amount of joy to have once a word wraps to the next line, however, an indentation is furthermore appreciated.";
                                    //                |-                   -                         -                        -          -|
                                    else
                                    {
                                        exampleText = "\t\t  Over-indenting isn't supported though. Further indents should be taken care of in another way... or just deal with it scrub!";
                                        wrapLevelText += " II";
                                    }
                                    break;
                                case 4:
                                case 5:
                                case 6:
                                    wrapLevelText = "Breaking Apart Words that are too large";
                                    if (ex == 4)
                                    {
                                        exampleText = "Sometimes words cannot be simply wrapped, they must be separated among multiple lines. For example a lengthy folder path: ";
                                        exampleText +="'C:\\Documents\\TextCharacters\\En_Num_Letters\\AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz'. ";
                                        exampleText +="These super-lengthy words need to be broken apart and have their remainders wrapped to the next lines.";
                                    //                |-                   -                         -                        -          -|
                                    }
                                    else if (ex == 5)
                                    {
                                        wrapLevelText += " II";
                                        exampleText =$"{Ind24}Here is another, but this time we are indented: ";
                                        exampleText +="'C:\\Users\\TheConsumerOfMagicalConfections\\HyperDrive\\Pictures\\OtherProjects\\TextCharactersIcons\\Nums_Letters\\En_Letters_Nums\\RegularSizeCharacters\\LastUnnecessaryDirectory\\Boldened2.txt'";
                                    //                |-                   -                         -                        -          -|
                                    }
                                    else
                                    {
                                        wrapLevelText += " III";
                                        exampleText = $"{Ind24}Again indented, in another case where the word itself matches screen width: ";
                                        exampleText +=@"'C:\Users\JalapenoFaceBusiness\Documents\TheHistoryOfPeppersAtWarWithOmnivores.png'";
                                     //                |-                   -                         -                        -          -|
                                    }
                                    break;
                                case 7:
                                case 8:
                                case 9:
                                    wrapLevelText = "Dealing with Newline escape characters";
                                    if (ex == 7)
                                    {
                                        exampleText = "While the tab escape character '\\t' is the equivalent of 8 spaces. The new line key '\\n' is a bit wonkier. Upon a newline: \n";
                                        exampleText +="The wrapping should reset it's printing position to the start.";
                                    //                |-                   -                         -                        -          -|
                                    }
                                    else if (ex == 8)
                                    {
                                        wrapLevelText += " II";
                                        exampleText = "\tIf that works, then the next question is: \nHow does this work in an indented wrapping situation? Perfectly!";
                                    //                |-                   -                         -                        -          -|
                                    }
                                    else
                                    {
                                        wrapLevelText += " III";
                                        exampleText = "  In some rarer instances, an 'in-text' newline is placed at the end of a string\n where it is not required. Those newlines are removed and the nature of wrapping will take care of the rest.";
                                    //                |-                   -                         -                        -          -|
                                    }
                                    break;
                                case 10:
                                    wrapLevelText = $"Multi-printed line wrapping";
                                    exampleText = "For some lines% text is printed% multiple times% before requiring% a wrap.% This %is% one% of those% examples.% Things should% continue as normal!";
                                    divChar = '%';
                                    enableFormatOverloadQ = true;
                                    break;
                                //                |-                   -                         -                        -          -|
                                case 11:
                                    wrapLevelText = $"Indented multi-printed line wrapping";
                                    exampleText = "  Multiple lines% may be printed% but what% about indent% lines?% The% wrapper% needs% to% know the% indentation% level.";
                                    divChar = '%';
                                    enableFormatOverloadQ = true;
                                    break;
                                //                |-                   -                         -                        -          -|
                                case 12:
                                    wrapLevelText = $"Indented multi-printed line wrapping (with long word)";
                                    exampleText = "Ex.2: %";
                                    exampleText+=@"'C:\Users\JalapenoFaceBusiness\Documents\TheHistoryOfPeppersAtWarWithCold_Chap3.bt'";
                                    divChar = '%';
                                    enableFormatOverloadQ = true;
                                    break;
                                //                |-                   -                         -                        -          -|


                                // end
                                default: endExamples = true; break;
                            }

                            if (exampleText.IsNotNE() && wrapLevelText.IsNotNE() && !endExamples)
                            {
                                TextLine($"Lvl{ex + 1}: {wrapLevelText}", wrapLevelCol);                      

                                // wrap prints here
                                for (int wx = 0; wx < (enableFormatOverloadQ ? 3 : 2); wx++)
                                {
                                    bool wrapOnQ = wx >= 1;
                                    if (!wrapOnQ) 
                                        TextLine($"Using Text() [wrap off] vs. Format() [wrap ON] {(enableFormatOverloadQ ? "+ ovrld Format() [wrap ON, 2nd]" : "")}", Color.DarkGray);

                                    /// IF ... test multiple texts wrapping; ELSE test single line wrapping
                                    if (divChar.IsNotNull())
                                    {
                                        if (exampleText.Contains(divChar))
                                        {
                                            string[] manyExampleTexts = exampleText.Split(divChar, StringSplitOptions.RemoveEmptyEntries);
                                            if (manyExampleTexts.HasElements())
                                                for (int ix = 0; ix < manyExampleTexts.Length; ix++)
                                                {
                                                    string anExampleText = manyExampleTexts[ix];
                                                    if (wrapOnQ)
                                                    {
                                                        if (enableFormatOverloadQ && wx == 2)
                                                            Format(anExampleText, GetPrefsForeColor(wrapOn));
                                                        else Format(anExampleText, wrapOn);
                                                    }
                                                    else
                                                        Text(anExampleText, wrapOff);
                                                }
                                        }
                                    }
                                    else
                                    {
                                        if (wrapOnQ)
                                        {
                                            if (enableFormatOverloadQ && wx == 2)
                                                Format(exampleText, GetPrefsForeColor(wrapOn));
                                            else Format(exampleText, wrapOn);
                                        }
                                        else
                                            Text(exampleText, wrapOff);

                                    }

                                    if (!wrapOnQ)
                                        ExampleDiv();
                                    else NewLine(2);
                                }


                                Text(" >> Continue To Next Wrap Level [Enter] >> ", Color.DarkGray);
                                Pause();
                                NewLine(2);

                                static void ExampleDiv()
                                {
                                    NewLine();
                                    TextLine("----------", Color.DarkGray);
                                }
                            }                          
                        }

                        Text("End of Word Wrapping Tests...");
                    }
                    #endregion
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
