using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;

namespace HCResourceLibraryApp.Layout
{
    public static class SettingsPage
    {
        static Preferences _preferencesRef;
        static char subMenuUnderline = '=';

        public static void GetPreferencesReference(Preferences preferences)
        {
            _preferencesRef = preferences;
        }
        public static void OpenPage()
        {
            //TextLine("\nSettings Page has been opened");
            //Pause();

            bool exitSettingsMain = false;
            do
            {
                Clear();
                bool validMenuKey = ListFormMenu(out string setMenuKey, "Settings Menu", null, null, "a~d", true, "Preferences,Content Integrity Verification,Reversion,Quit".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (setMenuKey)
                    {
                        case "a":
                            SubPage_Preferences();
                            break;

                        case "b":
                            SubPage_ContentIntegrity();
                            break;

                        case "c":
                            SubPage_Reversion();
                            break;

                        case "d":
                            exitSettingsMain = true;
                            break;
                    }
                }
            }
            while (!exitSettingsMain && !Program.AllowProgramRestart);            
        }

        static void SubPage_Preferences()
        {
            bool exitSetPrefsMenu = false;
            string activeMenuKey = null;
            do
            {
                // settings - preferences main menu
                Clear();
                FormatLine("NOTE :: Changes made to these settings will require a program restart.\n", ForECol.Accent);

                string[] prefMenuOpts = {"Window Dimensions Editor", "Foreground Elements Editor", "Quit [Enter]"};
                bool validKey = false, quitMenuQ = false;
                string setPrefsKey = null;
                if (activeMenuKey.IsNE())
                {
                    validKey = ListFormMenu(out setPrefsKey, "Preferences Settings Menu", subMenuUnderline, $"{Ind24}Select settings to edit >> ", "a/b", true, prefMenuOpts);
                    quitMenuQ = setPrefsKey == null || setPrefsKey == "c";
                    MenuMessageQueue(!validKey && !quitMenuQ, false, null);
                }

                if ((!quitMenuQ && validKey) || activeMenuKey.IsNotNE())
                {
                    // auto return to page
                    bool endActiveMenuKey = false;
                    if (activeMenuKey.IsNE())
                        activeMenuKey = setPrefsKey;
                    else if (activeMenuKey.IsNotNE())
                        setPrefsKey = activeMenuKey;

                    string titleText = "Preferences: " + (setPrefsKey.Equals("a") ? prefMenuOpts[0] : prefMenuOpts[1]);
                    Clear();
                    Title(titleText, subMenuUnderline, 2);

                    // window dimensions editor
                    if (setPrefsKey.Equals("a"))
                    {
                        //TextLine("Accessing Window Dimensions Editor\n");

                        DimHeight currHeightScale = _preferencesRef.HeightScale;
                        DimWidth currWidthScale = _preferencesRef.WidthScale;
                        string dimsPercents = $"{currHeightScale.GetScaleFactorH() * 100:0}% {currWidthScale.GetScaleFactorW() * 100:0}%";
                        Format("Current Window Dimensions Settings (as HxW): ", ForECol.Normal);
                        FormatLine($"{currHeightScale} {currWidthScale} ({dimsPercents})", ForECol.Highlight);

                        NewLine();
                        Title("Dimensions Values Table");
                        #region dimsTable
                        const string tableDiv = " | ";
                        string[] tableClm1 = { "%", "40", "50", "60", "80", "100" };
                        for (int tclm = 0; tclm < tableClm1.Length; tclm++)
                        {
                            Text(Ind24);
                            if (tclm >= 0)
                            {
                                // 1st column
                                Format(tableClm1[tclm].PadRight(3), ForECol.Normal);
                                Format(tableDiv , ForECol.Accent);

                                int tclmMinus1 = tclm - 1;
                                const int optionsPad = 14;

                                // 2nd column
                                string hClm = tclm == 0 ? "Height" : $"[{tclm}] {(DimHeight)tclmMinus1}";
                                Format(hClm.PadRight(optionsPad), currHeightScale == (DimHeight)tclmMinus1 ? ForECol.Highlight : ForECol.Normal);
                                Format(tableDiv, ForECol.Accent);

                                // 3rd column
                                string wClm = tclm == 0 ? "Width" : $"[{IntToAlphabet(tclmMinus1).ToString().ToLower()}] {(DimWidth)tclmMinus1}";
                                Format(wClm.PadRight(optionsPad), currWidthScale == (DimWidth)tclmMinus1 ? ForECol.Highlight : ForECol.Normal);

                                // table divider
                                if (tclm == 0)
                                    Format($"\n{Ind14}---------------------------------------------", ForECol.Accent);
                            }
                            NewLine();
                        }
                        #endregion

                        NewLine();
                        FormatLine("A restart is required after changing the window dimensions.", ForECol.Accent);
                        FormatLine("Enter the new dimensions as H,W below using the table above.", ForECol.Normal);

                        Format($"{Ind14}Change dimensions >> ", ForECol.Normal);
                        string input = StyledInput("#,_");
                        
                        // accept and validate input, make changes
                        if (input.IsNotNEW())
                        {
                            input = input.ToLower().Trim();
                            DimHeight newDimH = 0;
                            DimWidth newDimW = 0;
                            bool validInput = false;
                            string invalidationMsg = "";

                            if (input.Contains(","))
                            {
                                string[] dimOptChoices = input.Split(",");
                                if (dimOptChoices.HasElements())
                                    if (dimOptChoices.Length >= 2)
                                    {
                                        bool parsedH = int.TryParse(dimOptChoices[0], out int heightChoiceNum);
                                        bool parsedW = MenuOptions(dimOptChoices[1], out short widthChoiceOpt, "a,b,c,d,e".Split(',')); // using MenuOpts as if it were AlphabetToInt()


                                        if (parsedH && parsedW)
                                        {
                                            validInput = true;
                                            heightChoiceNum = heightChoiceNum.Clamp(1, 5) - 1;
                                            newDimH = (DimHeight)heightChoiceNum;
                                            newDimW = (DimWidth)widthChoiceOpt;
                                        }
                                        else
                                        {
                                            if (!parsedH)
                                                invalidationMsg = "Height choice was not a number option (1~5).";
                                            if (!parsedW)
                                                invalidationMsg += " Width choice was not a letter option (a~e).";
                                            invalidationMsg = invalidationMsg.Trim();
                                        }
                                    }
                            }
                            else invalidationMsg = "Separate height and width dimensions with a comma.";
                            IncorrectionMessageQueue(invalidationMsg);

                            // make changes to window dimension preferences
                            if (validInput)
                            {
                                NewLine(2);
                                
                                /// no change
                                if (newDimH == currHeightScale && newDimW == currWidthScale)
                                {
                                    Format($"{Ind24}The selected window dimensions are the current window dimensions!", ForECol.Normal);
                                    Pause();
                                }

                                /// change made
                                else
                                {
                                    string newDimsTxt = $"{newDimH} {newDimW} ({newDimH.GetScaleFactorH() * 100:0}% {newDimW.GetScaleFactorW() * 100:0}%)";
                                    Highlight(true, $"{Ind24}The newly selected window dimensions are (as HxW): {newDimsTxt}", newDimsTxt);
                                    //Format($"{Ind24}Do you wish to update preferences to these dimensions? ", ForECol.Warning);
                                    //string confirmInput = StyledInput("y/n");
                                    
                                    bool validResponse = Confirmation($"{Ind24}Do you wish to update preferences to these dimensions? ", false, out bool bResult);
                                    if (validResponse && bResult)
                                    {
                                        // changes here
                                        _preferencesRef.HeightScale = newDimH;
                                        _preferencesRef.WidthScale = newDimW;
                                    }
                                    ConfirmationResult(validResponse && bResult, Ind34, $"Updated window dimensions to {newDimsTxt}.", !validResponse ? $"No confirmation recieved." : "Window dimensions will not be updated.");

                                    // ask to restart program
                                    if (validResponse && bResult)
                                    {
                                        endActiveMenuKey = true;
                                        NewLine();
                                        Confirmation($"{Ind24}Do you wish to restart the program now? ", true, out bool restartQ, $"{Ind34}Program will restart...", $"{Ind34}Program will not restart.");

                                        if (restartQ)
                                            Program.RequireRestart();
                                    }
                                }
                            }

                            // incorrection messages
                            else
                            {
                                IncorrectionMessageTrigger($"{Ind24}Invalid input entered: \n{Ind34}");
                                //Format($"{Ind24}Invalid input entered: \n{Ind34}{invalidationMsg}", ForECol.Incorrection);
                                //Pause();
                            }
                        }
                        else if (input.IsNEW())
                            endActiveMenuKey = true;
                    }


                    // foreground elements (color) editor
                    else if (setPrefsKey.Equals("b"))
                    {
                        TextLine("Accessing Foreground Elements Editor");
                        endActiveMenuKey = true;
                        Pause();
                    }

                    // end auto-return
                    if (endActiveMenuKey)
                        activeMenuKey = null;
                }
                else if (quitMenuQ)
                    exitSetPrefsMenu = true;
            }
            while (!exitSetPrefsMenu && !Program.AllowProgramRestart);
        }
        static void SubPage_ContentIntegrity()
        {
            Minimal.HorizontalRule(subMenuUnderline, 2);
            TextLine("To Be Done :: Content Integrity Verification Page\n  Elements --");
            Minimal.List(OrderType.Unordered, "Display Contents in Library (Data IDs only)", "Verify Content Integrity", " Content Locations and File Types", " Run Verification Integrity");
            Pause();
        }
        static void SubPage_Reversion()
        {
            Minimal.HorizontalRule(subMenuUnderline, 2);
            TextLine("To Be Done :: Reversion Page\n  Elements --");
            Minimal.List(OrderType.Unordered, "Version Reversion -or- File save reversion (same page)");
            Pause();
        }
    }
}
