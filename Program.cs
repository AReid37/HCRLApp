using System;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp
{
    // THE ENTRANCE POINT, THE CONTROL ROOM
    class Program
    {
        #region fields / props
        static DataHandlerBase dataHandler;
        static Preferences preferences;
        #endregion

        static void Main()
        {
            // Lvl.0 - program launch
            bool restartProgram = false;
            do
            {
                TextLine("Hello, High Contrast Resource Library App!", Color.DarkGray);

                // setup
                /// data
                dataHandler = new DataHandlerBase();
                preferences = new Preferences();
                /// program function
                Tools.DisableWarnError = DisableWE.None;
                VerifyFormatUsage = true;
                //CustomizeMinimal(MinimalMethod.All, preferences.Normal, preferences.Accent);
                CustomizeMinimal(MinimalMethod.List, preferences.Normal, preferences.Accent);
                CustomizeMinimal(MinimalMethod.Important, preferences.Heading1, preferences.Accent);
                CustomizeMinimal(MinimalMethod.Table, preferences.Normal, preferences.Accent);
                CustomizeMinimal(MinimalMethod.Title, preferences.Heading1, preferences.Accent);
                CustomizeMinimal(MinimalMethod.HorizontalRule, preferences.Accent, preferences.Accent);
                ApplyPreferencesReference(preferences);

                // home page
                NewLine(10);
                Title("H i g h   C o n t r a s t", cBHB, 0);
                Title("  Resource Library  App  ", cTHB, 3);
                //Title("High Contrast Resource Library App", cBHB, 3);
                Format($"{Ind24}Press [Enter] to continue >> ", Layout.ForECol.Normal);
                Input("__", preferences.Highlight);
                
                if (!LastInput.IsNotNE())
                {
                    bool mainMenuQ = true;
                    string mainMenuOptKey = null;
                    const string wrongMMOKey = "~~";
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
                        Title("Main Menu", cTHB);
                        if (mainMenuOptKey == wrongMMOKey)
                            FormatLine("[x] Invalid option.", Layout.ForECol.Incorrection);
                        List(OrderType.Ordered_Alphabetical_LowerCase, "Logs Submission, Library Search, Log Legend View, Version Summaries, Setttings Page, Quit".Split(", "));
                        Text($"{Ind24}Option >> ");
                        bool isValidMMOpt = MenuOptions(Input("a~f", preferences.Highlight), out mainMenuOptKey, "a,b,c,d,e,f".Split(','));

                        Dbug.SingleLog("Test `Main Menu` menu", $"Input [{LastInput}]    Valid? [{isValidMMOpt}]    ResultingKey [{mainMenuOptKey}]");
                        if (isValidMMOpt)
                        {
                            if (!mainMenuOptKey.Contains('f'))
                            {
                                TextLine("\n\nEntering --- page");

                                #region testing 
                                Highlight(false, "Highlight me you fool!");
                                Highlight(true, "Highlight me you oblivious fool!", "", "me", "fool", "ighli", "ou");
                                Highlight(false, "Break the system, destroy what will remain", "s", "what will remain");

                                TextLine("## A debug to output has been ran ##", Color.Maroon);
                                #endregion
                                Pause();
                            }

                            // quit
                            else
                            {
                                TextLine("\n\nExiting Program");
                                Pause();

                                TextLine("\n\n**REMEMBER** Test the published version of the application frequently!!".ToUpper(), Color.White);
                                mainMenuQ = false;
                            }
                        }
                        else
                            mainMenuOptKey = wrongMMOKey;

                    } while (mainMenuQ);
                }

            }
            while (restartProgram);
        }
    }
}
