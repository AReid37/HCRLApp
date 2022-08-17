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
        static readonly char subMenuUnderline = '=';

        public static void GetPreferencesReference(Preferences preferences)
        {
            _preferencesRef = preferences;
        }
        public static void OpenPage()
        {
            bool exitSettingsMain = false;
            do
            {
                Clear();
                Title("Application Settings", cTHB, 1);
                FormatLine($"{Ind24}Facilitates customization of visual preferences, and has additional tools for content verification and save state reversions.", ForECol.Accent);
                NewLine(2);

                bool validMenuKey = ListFormMenu(out string setMenuKey, "Settings Menu", null, null, "a~d", true, $"Preferences,Content Integrity Verification,Reversion,{exitPagePhrase}".Split(','));
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

            // auto-save preferences
            if (_preferencesRef.ChangesMade())
                Program.SaveData();
        }

        // done
        static void SubPage_Preferences()
        {
            bool exitSetPrefsMenu = false;
            string activeMenuKey = null;
            Preferences newForeCols = _preferencesRef.ShallowCopy();
            do
            {
                // settings - preferences main menu
                Clear();
                FormatLine("NOTE :: Changes made to these settings will require a program restart.\n", ForECol.Accent);

                string[] prefMenuOpts = {"Window Dimensions Editor", "Foreground Elements Editor", $"{exitSubPagePhrase} [Enter]"};
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
                        // foreground preview visual
                        #region foreground preview
                        Title("Foreground Preview");
                        const string previewDiv = "---------------------------------------";

                        FormatLine(previewDiv, ForECol.Accent);
                        TextLine("Heading 1".ToUpper(), newForeCols.Heading1);
                        TextLine("=========", newForeCols.Accent);

                        Text("* ", newForeCols.Accent);
                        Text("Normal", newForeCols.Normal);
                        Text(" highlight", newForeCols.Highlight);
                        TextLine(" normal.", newForeCols.Normal);
                        NewLine();

                        //Text("# ", newForeCols.Accent);
                        TextLine("Heading 2", newForeCols.Heading2);
                        Text("Warning Message? ", newForeCols.Warning);
                        TextLine("Input", newForeCols.Input);

                        Text($"{Ind24}Confirm", newForeCols.Correction);
                        Text(" / ", newForeCols.Accent);
                        TextLine("Deny", newForeCols.Incorrection);
                        FormatLine(previewDiv, ForECol.Accent);
                        #endregion
                        NewLine();
                        ShowPrefsColors(newForeCols);                        
                        FormatLine("0|Reset to defaults", ForECol.Normal);
                        NewLine();

                        // Menu validation and actions
                        FormatLine($"{Ind24}A restart is required after changing the foreground element colors.", ForECol.Accent);
                        Format($"{Ind24}Select an option (0~9) >> #", ForECol.Normal);
                        bool menuPass = MenuOptions(StyledInput(null), out short tableOptNumber, "0 1 2 3 4 5 6 7 8 9".Split(' '));
                        if (menuPass)
                        {
                            string[] foreColOptions =
                            {
                                "Reset to Defaults", "Normal", "Highlight", "Accent", "Correction", "Incorrection", "Warning", "Heading 1", "Heading 2", "Input"
                            };
                            string foreOption = tableOptNumber == 0 ? foreColOptions[tableOptNumber] : $"Edit {foreColOptions[tableOptNumber]} Color";

                            // edit colors
                            if (tableOptNumber != 0)
                            {
                                Clear();
                                HorizontalRule(subMenuUnderline);
                                //Important(foreOption);

                                int fecIndex = tableOptNumber - 1;
                                Color exemptCol = newForeCols[fecIndex];
                                bool valid = exemptCol == Color.Black ? ColorMenu(foreOption, out Color fecNewCol) : ColorMenu(foreOption, out fecNewCol, exemptCol);
                                if (valid)
                                {
                                    string forElement = foreColOptions[tableOptNumber];
                                    string prompt = fecNewCol == Color.Black ? $"Reset the '{forElement}' element color to its default? " : $"Change '{forElement}' element color from '{newForeCols[fecIndex]}' to '{fecNewCol}'? ";

                                    NewLine();
                                    Confirmation($"{Ind24}{prompt}", false, out bool yesNo, $"{Ind34}Color of '{forElement}' foreground element will be changed to '{fecNewCol}'.", $"{Ind34}Color of '{forElement}' foreground element will not be changed.");
                                    if (yesNo)
                                        newForeCols[fecIndex] = fecNewCol;
                                }
                                else
                                {
                                    Format($"{Ind34}No option selected from color menu.", ForECol.Incorrection);
                                    Pause();
                                }
                            }
                            
                            // reset to defaults
                            else
                            {
                                NewLine(2);
                                Important(foreOption);
                                Confirmation($"{Ind24}Do you wish to reset the foreground element colors to their defaults? ", false, out bool yesNo, $"{Ind34}Resetting foreground element colors to their defaults.", $"{Ind34}Foreground element colors will not be reset.");
                                
                                if (yesNo)
                                    newForeCols = new Preferences();
                            }
                        }

                        // review and finally confirm changes before exiting
                        else
                        {
                            if (!_preferencesRef.Compare(newForeCols))
                            {
                                Clear();
                                Title("Confirm Foreground Element Color Edits", '.', 0);
                                FormatLine("Below are comparisons between the current and edited foreground preferences.", ForECol.Accent);
                                
                                FormatLine("Current Foreground Colors:".ToUpper(), ForECol.Heading2);
                                ShowPrefsColors(_preferencesRef);
                                NewLine();
                                FormatLine("Edited Foreground Colors:".ToUpper(), ForECol.Heading2);
                                ShowPrefsColors(newForeCols);

                                NewLine(2);
                                Confirmation($"{Ind24}Do you confirm the changes to foreground element colors? ", true, out bool yesNo, $"{Ind34}Foreground element colors will be updated.", $"{Ind34}Foreground element colors will remain as they were.");

                                if (yesNo)
                                {
                                    NewLine();
                                    FormatLine($"{Ind14}NOTICE ::\n{Ind24}Without a restart, foreground elements will be a messy mix of the new and old!", ForECol.Warning);
                                    Confirmation($"{Ind24}Do you wish to restart the program now? ", true, out bool restartQ, $"{Ind34}Program will restart...", $"{Ind34}Program will not restart.");

                                    // ask to restart
                                    if (restartQ)
                                        Program.RequireRestart();

                                    _preferencesRef.Normal = newForeCols.Normal;
                                    _preferencesRef.Highlight = newForeCols.Highlight;
                                    _preferencesRef.Accent = newForeCols.Accent;
                                    _preferencesRef.Correction = newForeCols.Correction;
                                    _preferencesRef.Incorrection = newForeCols.Incorrection;
                                    _preferencesRef.Warning = newForeCols.Warning;
                                    _preferencesRef.Heading1 = newForeCols.Heading1;
                                    _preferencesRef.Heading2 = newForeCols.Heading2;
                                    _preferencesRef.Input = newForeCols.Input;
                                }
                                else
                                    newForeCols = _preferencesRef.ShallowCopy();
                            }
                            else
                            {
                                NewLine();
                                Format($"{Ind24}No changes were made to the foreground element colors.", ForECol.Normal);
                                Pause();
                            }
                            endActiveMenuKey = true;
                        }


                        // colors visual method
                        static void ShowPrefsColors(Preferences prefs)
                        {
                            /// get colors
                            Color[,] prefCols = new Color[3, 3]
                            {
                                { prefs.Normal, prefs.Highlight, prefs.Accent },
                                { prefs.Correction, prefs.Incorrection, prefs.Warning },
                                { prefs.Heading1, prefs.Heading2, prefs.Input }
                            };

                            /// print table of colors
                            for (int fecIx = 0; fecIx < 3; fecIx++)
                            {
                                HoldNextListOrTable();
                                const char div = '%', tableDiv = ' ', exChar = cDS; // exChar meaning "example (representational) character"
                                switch (fecIx)
                                {
                                    // normal, highlight, accent
                                    case 0:
                                        Table(Table3Division.Even, $"1{div} Normal", tableDiv, $"2{div} Highlight", $"3{div} Accent");
                                        break;

                                    // correction, incorrection, warning
                                    case 1:
                                        Table(Table3Division.Even, $"4{div} Correction", tableDiv, $"5{div} Incorrection", $"6{div} Warning");
                                        break;

                                    // heading 1, heading 2, input
                                    case 2:
                                        Table(Table3Division.Even, $"7{div} Heading 1", tableDiv, $"8{div} Heading 2", $"9{div} Input");
                                        break;
                                }

                                if (LatestTablePrintText.IsNotNEW())
                                    if (LatestTablePrintText.Contains(div.ToString()))
                                    {
                                        string[] tableDatas = LatestTablePrintText.Split(div);
                                        if (tableDatas.HasElements())
                                            if (tableDatas.Length >= 4)
                                            {
                                                // this might be messy
                                                for (int partIx = 0; partIx < 4; partIx++)
                                                {
                                                    string tableData = tableDatas[partIx].Replace("\n", "");
                                                    if (partIx + 1 < 4)
                                                    {
                                                        Format($"{tableData}|", ForECol.Normal);
                                                        Text(exChar.ToString(), prefCols[fecIx, partIx]);
                                                    }
                                                    else FormatLine(tableData, ForECol.Normal);
                                                }
                                            }
                                    }
                            }
                        }
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
        // not done...
        static void SubPage_ContentIntegrity()
        {
            Minimal.HorizontalRule(subMenuUnderline, 2);
            TextLine("To Be Done :: Content Integrity Verification Page\n  Elements --");
            Minimal.List(OrderType.Unordered, "Display Contents in Library (Data IDs only)", "Verify Content Integrity", " Content Locations and File Types", " Run Verification Integrity");
            Pause();
        }
        // not done...
        static void SubPage_Reversion()
        {
            Minimal.HorizontalRule(subMenuUnderline, 2);
            TextLine("To Be Done :: Reversion Page\n  Elements --");
            Minimal.List(OrderType.Unordered, "Version Reversion -or- File save reversion (same page)");
            Pause();
        }

        // consider...  FullReset (clear ALL data)
    }
}
