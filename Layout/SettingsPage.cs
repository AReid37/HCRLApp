using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HCResourceLibraryApp.Layout
{
    public static class SettingsPage
    {
        static Preferences _preferencesRef;
        static ResLibrary _resLibrary;
        static readonly char subMenuUnderline = '=';

        public static void GetPreferencesReference(Preferences preferences)
        {
            _preferencesRef = preferences;
        }
        public static void GetResourceLibraryReference(ResLibrary resourceLibrary)
        {
            _resLibrary = resourceLibrary;
        }
        public static void OpenPage()
        {
            bool exitSettingsMain = false;
            do
            {
                Program.LogState("Settings");
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

            // auto-saves:
            //      -> preferences
            if (_preferencesRef.ChangesMade())
                Program.SaveData(false);
        }

        // done
        static void SubPage_Preferences()
        {
            bool exitSetPrefsMenu = false;
            string activeMenuKey = null;
            Preferences newForeCols = _preferencesRef.ShallowCopy();
            do
            {
                if (activeMenuKey.IsNE())
                    Program.LogState("Settings|Preferences");

                // settings - preferences main menu
                Clear();
                FormatLine("NOTE :: Changes made to these settings will require a program restart.\n", ForECol.Accent);

                string[] prefMenuOpts = {"Window Dimensions Editor", "Foreground Elements Editor", $"{exitSubPagePhrase} [Enter]"};
                bool validKey = false, quitMenuQ = false;
                string setPrefsKey = null;
                if (activeMenuKey.IsNE())
                {
                    validKey = ListFormMenu(out setPrefsKey, "Preferences Settings Menu", subMenuUnderline, $"{Ind24}Select settings to edit >> ", "a/b", true, prefMenuOpts);
                    quitMenuQ = LastInput.IsNE() || setPrefsKey == "c";
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
                        Program.LogState("Settings|Preferences|Window Dimensions Editor");
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
                        Program.LogState("Settings|Preferences|Foreground Elements Color Editor");
                        
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
                        HSNL(0, 2);
                        FormatLine($"#These element colors also affects the title: {HomePage.primaryCol}, {HomePage.secondaryCol}, {HomePage.tertiaryCol}.", ForECol.Accent);
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
            bool exitContentIntegrityMenu = false;
            do
            {
                Program.LogState("Settings|Content Integrity (wip)");
                Clear();

                string[] conIntOptions = { "Verify Content Integrity", "View All Data IDs", $"{exitSubPagePhrase} [Enter]" };
                bool validKey, quitMenuQ;
                string setConIntKey;
                validKey = ListFormMenu(out setConIntKey, "Content Integrity Menu", subMenuUnderline, $"{Ind24}Select an option >> ", "1/2", true, conIntOptions);
                quitMenuQ = LastInput.IsNE() || setConIntKey == "c";
                MenuMessageQueue(!validKey && !quitMenuQ, false, null);

                if (!quitMenuQ && validKey)
                {
                    bool librarySetup = false;
                    if (_resLibrary != null)
                        librarySetup = _resLibrary.IsSetup();
                    string titleText = "Content Integrity: " + (setConIntKey == "a" ? conIntOptions[0] : conIntOptions[1]);
                    Clear();
                    Title(titleText, subMenuUnderline, 0);

                    // verify content integrity
                    if (setConIntKey.Equals("a") && librarySetup)
                    {
                        NewLine(2);
                        Text("WIP...");
                        Pause();
                        // tbd...
                    }

                    // view all data Ids
                    else if (setConIntKey.Equals("b") && librarySetup)
                    {
                        NewLine();
                        Dbug.StartLogging("SettingsPage.SubPage_ContentIntegrity():ViewAllDataIds");

                        // gather data
                        /// fetch legend keys
                        const string miscPhrase = "Miscellaneous";
                        List<string> legendKeys = new() { miscPhrase }, legendDefs = new() { miscPhrase };
                        List<string> legendSymbols = new();
                        Dbug.Log("Fetching Legend Keys (and Definitions) --> ");
                        Dbug.NudgeIndent(true);
                        foreach (LegendData legDat in _resLibrary.Legends)
                        {
                            Dbug.LogPart($"Key '{legDat.Key}' | Added? {IsNotSymbol(legDat.Key)}");
                            if (IsNotSymbol(legDat.Key))
                            {
                                legendKeys.Add(legDat.Key);
                                /// adding key before definition so they are sorted similarly
                                legendDefs.Add($"{legDat.Key} {legDat[0]}");
                                Dbug.LogPart($" | and Definition '{legDat[0]}'");
                            }
                            else legendSymbols.Add(legDat.Key);
                            Dbug.Log("; ");
                        }
                        Dbug.NudgeIndent(false);
                        Dbug.Log("Done, and sorted; ");
                        legendKeys = legendKeys.ToArray().SortWords();
                        legendDefs = legendDefs.ToArray().SortWords();

                        /// fetch all data ids
                        List<string> allDataIds = new();
                        Dbug.Log("Fetching all Data IDs (Data Id's in angled brackets '<>' were rejected); Note that legend symbols are disregarded; ");
                        Dbug.NudgeIndent(true);
                        foreach (ResContents resCon in _resLibrary.Contents)
                        {
                            Dbug.LogPart($"From #{resCon.ShelfID} {$"'{resCon.ContentName}'", -30}  //  CBG :: ");
                            for (int cbx = 0; cbx < resCon.ConBase.CountIDs; cbx++)
                            {
                                string datID = RemoveLegendSymbols(resCon.ConBase[cbx]);
                                if (!allDataIds.Contains(datID))
                                {
                                    allDataIds.Add(datID);
                                    Dbug.LogPart($"{datID} ");
                                }
                                else Dbug.LogPart($"<{datID}> ");
                            }

                            if (resCon.ConAddits.HasElements())
                            {
                                Dbug.LogPart("  //  CAs ::");
                                for (int casx = 0; casx < resCon.ConAddits.Count; casx++)
                                {
                                    Dbug.LogPart($" |[{casx + 1}]");
                                    ContentAdditionals conAdt = resCon.ConAddits[casx];
                                    for (int cax = 0; cax < conAdt.CountIDs; cax++)
                                    {
                                        string datID = RemoveLegendSymbols(conAdt[cax]);
                                        if (!allDataIds.Contains(datID))
                                        {
                                            allDataIds.Add(datID);
                                            Dbug.LogPart($"{datID} ");
                                        }
                                        else Dbug.LogPart($"<{datID}> ");
                                    }
                                }
                            }
                            Dbug.Log("; ");
                        }
                        Dbug.NudgeIndent(false);
                        Dbug.Log("Done, and sorted; ");
                        allDataIds = allDataIds.ToArray().SortWords();
                        

                        // print data IDs in categories by legend
                        if (legendKeys.HasElements() && legendDefs.HasElements() && allDataIds.HasElements())
                        {
                            _resLibrary.GetVersionRange(out _, out VerNum latestVer);
                            FormatLine($"All data IDs from shelves of library (version: {latestVer.ToStringNums()}).", ForECol.Accent);

                            Dbug.Log("Printing Data ID categories; ");
                            Dbug.NudgeIndent(true);
                            for (int lx = 0; lx < legendKeys.Count; lx++)
                            {
                                string legendKey = legendKeys[lx];
                                string legendDef = legendKey != miscPhrase ? legendDefs[lx].Replace($"{legendKey} ", "") : legendDefs[lx];
                                Dbug.Log($"Category '{legendKey}' [{legendDef}]");

                                Dbug.NudgeIndent(true);
                                string dataIDList = "";
                                for (int dx = 0; dx < allDataIds.Count; dx++)
                                {
                                    string datIDToPrint = "";
                                    string datID = allDataIds[dx];

                                    // print numeric data IDs
                                    if (legendKey != miscPhrase)
                                    {
                                        if (!LogDecoder.IsNumberless(datID))
                                            if (LogDecoder.RemoveNumbers(datID) == legendKey)
                                                datIDToPrint = datID.Replace(legendKey, "");
                                    }

                                    // print wordy data IDs (numberless)
                                    else
                                    {
                                        if (LogDecoder.IsNumberless(datID))
                                            datIDToPrint = datID;
                                    }

                                    Dbug.LogPart($"{datIDToPrint} ");
                                    if (datIDToPrint.IsNotNE())
                                        dataIDList += $"{datIDToPrint} ";
                                }

                                if (legendKey != miscPhrase)
                                {
                                    Dbug.LogPart(" ..  Condensing with ranges");
                                    string dataIDListWithRanges = Extensions.CreateNumericRanges(dataIDList.Split(" "));
                                    if (dataIDList.Contains(dataIDListWithRanges))
                                        Dbug.Log("; Remains uncondensed; ");
                                    else Dbug.Log("; ");

                                    if (dataIDListWithRanges.IsNotNE())
                                    {
                                        Dbug.LogPart($":: {dataIDListWithRanges}");
                                        dataIDList = dataIDListWithRanges;
                                    }
                                }

                                // all printing here
                                if (legendKey != miscPhrase || (legendKey == miscPhrase && dataIDList.IsNotNE()))
                                {
                                    Highlight(HSNL(0, 2) > 1, $"[{legendDef}]{Ind14}", $"[{legendDef}]");
                                    Format(dataIDList.Trim(), ForECol.Normal);
                                    NewLine(lx + 1 != legendKeys.Count ? 2 : 1);
                                }
                                Dbug.Log($" //  End '{legendKey}'");
                                Dbug.NudgeIndent(false);
                            }
                            Dbug.NudgeIndent(false);

                            // Display TTA
                            NewLine(HSNL(0, 2) > 1 ? 3 : 2);
                            FormatLine("-------", ForECol.Accent);
                            Highlight(false, $"Total Textures Added :: {allDataIds.Count}.", allDataIds.Count.ToString());

                            //Format("All Data IDs found within library are displayed above.", ForECol.Accent);
                            Pause();
                        }
                        Dbug.EndLogging();

                        // method
                        string RemoveLegendSymbols(string str)
                        {
                            if (legendSymbols.HasElements() && str.IsNotNE())
                            {
                                foreach (string legSym in legendSymbols)
                                    str = str.Replace(legSym, "");
                                str = str.Trim();
                            }
                            return str;
                        }
                    }

                    // no library??
                    else if (!librarySetup)
                    {
                        NewLine(2);
                        Format("The library shelves are empty. This page requires the library to contain some data.", ForECol.Normal);
                        Pause();
                    }

                    /// ALSO 
                    ///  -- Count the TOTAL number of textures and display it...


                    // (static) methods
                    bool IsSymbol(string str)
                    {
                        bool isSym = false;
                        if (str.IsNotNE())
                        {
                            isSym = true;
                            if (int.TryParse(str, out _))
                                isSym = false;
                            else if (str.ToLower() != str.ToUpper())
                                isSym = false;
                        }
                        return isSym;
                    }
                    bool IsNotSymbol(string str)
                    {
                        return !IsSymbol(str);
                    }
                }
                else if (quitMenuQ)
                    exitContentIntegrityMenu = true;
            }
            while (!exitContentIntegrityMenu);
        }
        // done
        static void SubPage_Reversion()
        {
            bool exitReversionMenu = false;
            do
            {
                Program.LogState("Settings|Reversion");
                Clear();
                //FormatLine("\n", ForECol.Accent);

                string[] revertMenuOpts = { "File Save Reversion", "Version Reversion", $"{exitSubPagePhrase} [Enter]" };
                bool validKey, quitMenuQ;
                validKey = ListFormMenu(out string setRevKey, "Reversion Settings Menu", subMenuUnderline, $"{Ind24}Select an option >> ", "a/b", true, revertMenuOpts);
                quitMenuQ = LastInput.IsNE() || setRevKey == "c";
                MenuMessageQueue(!validKey && !quitMenuQ, false, null);

                if (!quitMenuQ && validKey)
                {
                    string titleText = "Reversion: " + (setRevKey.Equals("a") ? revertMenuOpts[0] : revertMenuOpts[1]);
                    Clear();
                    Title(titleText, subMenuUnderline, 2);

                    // file save reversion
                    if (setRevKey.Equals("a"))
                    {
                        FormatLine($"Reversion of a file save accommodates for reloading the backup of the previous program save file. This reverts all information that has been changed for this current version. A reversion can only be done once an old save state has been stored.", ForECol.Normal);

                        // continue to reversion
                        if (DataHandlerBase.AvailableReversion)
                        {
                            Program.LogState("Settings|Reversion|File Save Revert - Allowed");
                            NewLine();
                            FormatLine("NOTE :: A file save reversion will require a program restart.", ForECol.Accent);

                            NewLine(2);
                            FormatLine("File reversion is available (there is a save state to revert to). ", ForECol.Normal);
                            FormatLine($"Please note that data lost from a file reversion is unrecoverable.", ForECol.Warning);
                            Confirmation($"{Ind24}Are you sure you wish to revert file save? ", true, out bool yesNo);

                            /// 1st confirmation
                            if (yesNo)
                            {
                                /// 2nd confirmation
                                Confirmation($"{Ind24}Absolutely sure of reversion? ", true, out bool absoluteYesNo, $"{Ind34}Program will revert file save and and restart.", $"{Ind34}Reversion cancelled.");
                                
                                if (absoluteYesNo)
                                {
                                    if (Program.SaveReversion())
                                        Program.RequireRestart();
                                    else
                                    {
                                        NewLine();
                                        Format($"\tFile save reversion could not be executed!", ForECol.Incorrection);
                                        Pause();
                                    }    
                                }
                            }
                        }

                        // deny reversion access
                        else
                        {
                            Program.LogState("Settings|Reversion|File Save Revert - Denied");
                            NewLine();
                            Format("File reversion is not available (no save state to revert to).", ForECol.Normal);
                            Pause();
                        }
                    }

                    // version reversion
                    if (setRevKey.Equals("b"))
                    {
                        FormatLine($"Version Reversion is specific to the resource library and the contents within, thus only being available once the library has sufficient contents. Version reversion removes contents of a specific version until the given version state has been reached.", ForECol.Normal);
                        FormatLine($"Example\n{Ind14}The library has contents at v0.20, reverting to v0.16 would unload:\n{Ind24}v0.20, v0.19, v0.18, v0.17.", ForECol.Normal);

                        const string clearLibPhrase = "Empty Library";
                        bool okayToResetVersion = false;
                        string reversionDeniedMessage = "empty library";
                        VerNum lowest = VerNum.None, highest = VerNum.None;
                        if (_resLibrary != null)
                        {
                            if (_resLibrary.GetVersionRange(out lowest, out highest))
                            {
                                reversionDeniedMessage = "library needs more information";
                                if (!lowest.Equals(highest))
                                    okayToResetVersion = true;
                            }
                        }

                        // continue to version reversion
                        if (okayToResetVersion)
                        {
                            Program.LogState("Settings|Reversion|Version Reversion - Allowed");
                            NewLine();
                            FormatLine($"Version reversion is availble.", ForECol.Normal);
                            HorizontalRule('\'', 1);
                            //FormatLine("NOTE :: A version reversion will require a program restart.", ForECol.Accent);

                            //NewLine(2);
                            Highlight(false, $"The latest library version is {highest}. The library may be reverted to the oldest library version, {lowest}. ", $"{highest}", $"{lowest}");
                            FormatLine("The library's shelves may also be completely cleared to remove all version data.", ForECol.Normal);
                            NewLine();

                            VerNum closestReversion;
                            if (highest.MinorNumber - 1 < 0)
                                closestReversion = new VerNum(highest.MajorNumber - 1, 99);
                            else closestReversion = new VerNum(highest.MajorNumber, highest.MinorNumber - 1);

                            string exmplRange = lowest.AsNumber != closestReversion.AsNumber ? $"{lowest.ToStringNums()} ~ {closestReversion.ToStringNums()}" : $"{lowest.ToStringNums()}";
                            FormatLine($"Enter version to revert to ({exmplRange}) or enter any key to clear library.", ForECol.Normal);
                            Format($"{Ind24}Revert to: ", ForECol.Normal);
                            string input = StyledInput($"a.bb");

                            if (input.IsNotNEW())
                            {
                                bool isRevertingToVersion = VerNum.TryParse(input, out VerNum verRevert);                                

                                /// revert to version
                                if (isRevertingToVersion)
                                {
                                    //Program.LogState("Settings|Reversion|Version Reversion - Allowed|Revert to Version");
                                    if (verRevert.AsNumber.IsWithin(lowest.AsNumber, highest.AsNumber - 1))
                                    {
                                        SubReversionTitle();
                                        FormatLine("Reverting to an older version of the library will remove contents added later than the given version. This change is only reversible through an immediate File Save Reversion.", ForECol.Normal);
                                        Confirmation($"Are you sure you wish to revert to version {verRevert.ToStringNums()}? ", true, out bool yesNo);
                                        if (yesNo)
                                        {
                                            Confirmation($"{Ind24}Are you absolutely sure? ", true, out bool absoluteYesNo);
                                            ConfirmationResult(absoluteYesNo, $"{Ind24}", $"Library will be reverted to version {verRevert.ToStringNums()}.", "Library reversion cancelled.");

                                            if (absoluteYesNo)
                                            {
                                                bool reverted = _resLibrary.RevertToVersion(verRevert);
                                                if (!reverted)
                                                {
                                                    NewLine();
                                                    Format($"{Ind24}Failed to revert library to version {verRevert.ToStringNums()}. Reversed reversion progress.", ForECol.Incorrection);
                                                    Pause();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Format($"{Ind24}Version to revert must be within the version range: {exmplRange}.", ForECol.Incorrection);
                                        Pause();
                                    }
                                }

                                /// clear library
                                else
                                {
                                    SubReversionTitle();
                                    FormatLine("Clearing the Library will remove ALL content information from the library, which is only reversible through an immediate File Save Reversion.", ForECol.Normal);
                                    Confirmation($"Are you sure you wish to clear the library? ", true, out bool yesNo);
                                    if (yesNo)
                                    {
                                        NewLine();
                                        Highlight(true, $"\t\b\b\b\bEnter the phrase '{clearLibPhrase}' below to clear library shelves.", clearLibPhrase);
                                        Format($"{Ind24}Phrase to reset library >> ", ForECol.Warning);
                                        input = StyledInput(null);                                  
                                    }
                                    ConfirmationResult(yesNo && input == clearLibPhrase, $"{Ind34}The library ", "will be cleared.", "will not be cleared.");

                                    // library cleared here
                                    if (yesNo && input == clearLibPhrase)
                                        _resLibrary.ClearLibrary();
                                }


                                void SubReversionTitle()
                                {
                                    NewLine(10);
                                    HorizontalRule(subMenuUnderline);
                                    Important(isRevertingToVersion ? "Revert To Version" : "Clear Library");
                                    NewLine();
                                }
                            }
                        }

                        // deny version reversion access
                        else
                        {
                            Program.LogState("Settings|Reversion|Version Reversion - Denied");
                            NewLine();
                            Format($"Version reversion is not available ({reversionDeniedMessage}).", ForECol.Normal);
                            Pause();
                        }

                    }

                    // save changes if any
                    if (_resLibrary.ChangesMade())
                        Program.SaveData(true);
                }
                else if (quitMenuQ)
                    exitReversionMenu = true;
            } 
            while (!exitReversionMenu && !Program.AllowProgramRestart);
        }

    }
}
