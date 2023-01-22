﻿using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System;
using System.IO;

namespace HCResourceLibraryApp.Layout
{
    public static class SettingsPage
    {
        static Preferences _preferencesRef;
        static ResLibrary _resLibrary;
        static ContentValidator _contentValidator;
        static readonly char subMenuUnderline = '=';

        public static void GetPreferencesReference(Preferences preferences)
        {
            _preferencesRef = preferences;
        }
        public static void GetResourceLibraryReference(ResLibrary resourceLibrary)
        {
            _resLibrary = resourceLibrary;
        }
        public static void GetContentValidatorReference(ContentValidator contentValidator)
        {
            _contentValidator = contentValidator;
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
            //      -> content validator
            if (_preferencesRef != null && _contentValidator != null)
                if (_preferencesRef.ChangesMade() || _contentValidator.ChangesMade())
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
                                IncorrectionMessageTrigger($"{Ind24}Invalid input entered: \n{Ind34}", null);
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
                            if (!_preferencesRef.Equals(newForeCols))
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
            string activeMenuKey = null;

            VerNum verLow, verHigh = verLow = VerNum.None;
            List<string> folderPaths = new(), fileExtensions = new();
            // folder paths and file extensions preload
            if (_contentValidator != null)
            {
                if (_contentValidator.IsSetup())
                {
                    if (_contentValidator.FolderPaths.HasElements())
                        folderPaths.AddRange(_contentValidator.FolderPaths);
                    if (_contentValidator.FileExtensions.HasElements())
                        fileExtensions.AddRange(_contentValidator.FileExtensions);
                }
            }

            do
            {
                // settings - ConInt main menu
                if (activeMenuKey.IsNE())
                    Program.LogState("Settings|Content Integrity (wip)");
                Clear();

                string[] conIntOptions = { "Verify Content Integrity", "View All Data IDs", $"{exitSubPagePhrase} [Enter]" };
                bool validKey = false, quitMenuQ = false;
                string setConIntKey = null;
                if (activeMenuKey.IsNE())
                {
                    validKey = ListFormMenu(out setConIntKey, "Content Integrity Menu", subMenuUnderline, $"{Ind24}Select an option >> ", "1/2", true, conIntOptions);
                    quitMenuQ = LastInput.IsNE() || setConIntKey == "c";
                    MenuMessageQueue(!validKey && !quitMenuQ, false, null);
                }

                if ((!quitMenuQ && validKey) || activeMenuKey.IsNotNE())
                {
                    // auto-return (only for one page)
                    bool endActiveMenuKey = false;
                    if (activeMenuKey.IsNE())
                        activeMenuKey = setConIntKey;
                    else if (activeMenuKey.IsNotNE()) 
                        setConIntKey = activeMenuKey;

                    bool librarySetup = false;
                    if (_resLibrary != null)
                        librarySetup = _resLibrary.IsSetup();
                    string titleText = "Content Integrity: " + (setConIntKey == "a" ? conIntOptions[0] : conIntOptions[1]);
                    Clear();
                    Title(titleText, subMenuUnderline, 1);


                    // verify content integrity
                    if (setConIntKey.Equals("a") && librarySetup)
                    {
                        Program.LogState("Settings|Content Integrity (wip)|Verify Content Integrity");
                        //NewLine();
                        FormatLine($"{Ind24}Content Integrity Verification (CIV) is a process that validates the existence of contents in the resource library within a given folder.", ForECol.Normal);
                        NewLine();

                        /** Verification Location Review (snippet) [from Design Doc] + Planning
				            ...................................
				            Relative Paths
				            1 |~ Games\Terraria\ResourcePacks\High Contrast\Content				
				            2 |~Terraria\ResourcePacks\High Contrast\Content\Images
				
				            File Extensions In Use
				            1 |.png
				            2 |.mp3
				            3 |.xnb				
				            ...................................		
                        

                            ## ContentValidator.cs : DataHandler  "a class purposed for Content Integrity Verification (CIV)"
                            FEILDS / PROPS
                            - ResLibr _libraryRef
                            - str[] _folderPaths, _prevFolderPaths
                            - str[] _fileTypeExts, _prevFileTypeExts    
                            - CVI[] _conValReport

                            CONSTRUCTORS
                            - CV ()
                            - CV (ResLib libraryReference)
                            
                            METHODS
                            - pb void Validate(VerNum[] versionRange, str[] folderPaths, str[] fileTypeExtensions)
                            - ovr bl EncodeToSharedFile()
                            - ovr bl DecodeFromSharedFile()
                            - ovr bl ChangesDetected()
                            - vd SetPreviousSelf()
                            - CV GetPreviousSelf()


                            ....................
                            ## ConValInfo.cs (struct) 
                            FIELDS / PROPS    
                            - bl validatedQ
                            - str dataID
                            - str expandedDataID (?)
                            - str filePath (?)

                            CONSTRUCTORS
                            - CVI(bl isValidatedQ, str storedDataID)
                            - CVI(bl isValidatedQ, str storedDataID, str longDataID, str contentPath)
                        
                            METHODS
                            - bl Equals(CIV other)
                            - bl IsSetup()
                            
                         */


                        // CIV info review display
                        bool createdDisplayQ = false;
                        if (HSNL(0, 2) > 0)
                            Title("CIV Parameters Review", subMenuUnderline);
                        else Title("CIV Parameters Review");                                                
                        /// IF got version range: show info display; ELSE don't show info display and relay issue
                        if (_resLibrary.GetVersionRange(out VerNum verEarliest, out VerNum verLatest))
                        {
                            Program.ToggleFormatUsageVerification();
                            const ForECol heading3Col = ForECol.Heading2;

                            // + VERSION RANGE +
                            /// set ver range bounds
                            if (!verHigh.HasValue() && !verLow.HasValue())
                            {
                                verLow = verEarliest;
                                verHigh = verLatest;
                            }
                            /// format ver range text
                            string verRangeText = null;
                            if (verHigh.HasValue() && verLow.HasValue())
                            { /// wrapping
                                if (verEarliest.Equals(verLow) && verLatest.Equals(verHigh) && !verHigh.Equals(verLow))
                                    verRangeText = $"All versions (to latest: {verHigh.ToStringNums()})";
                                else if (verHigh.Equals(verLow))
                                    verRangeText = $"Version {verLow.ToStringNums()} only";
                                else verRangeText = $"Versions {verLow.ToStringNums()} through {verHigh.ToStringNums()}";
                            }
                            /// printing
                            Format($"Version Range", heading3Col);
                            NewLine();
                            Highlight(true, $". |{verRangeText}", verLow.ToStringNums(), verHigh.ToStringNums());
                            NewLine();                            


                            // + FOLDER PATHS +
                            FormatLine("Folder Paths", heading3Col);
                            if (folderPaths.HasElements())
                            {
                                for (int fpx = 0; fpx < folderPaths.Count; fpx++)
                                {
                                    Format($"{fpx + 1,-2}|", ForECol.Normal);
                                    FormatLine(folderPaths[fpx], ForECol.Highlight);
                                }
                            }
                            else FormatLine("1 |(none)", ForECol.Normal);
                            NewLine();


                            // + FILE EXTENSIONS +
                            FormatLine("File Extensions", heading3Col);
                            if (fileExtensions.HasElements())
                            {
                                for (int fex = 0; fex < fileExtensions.Count; fex++)
                                {
                                    Format($"{fex + 1, -2}|", ForECol.Normal);
                                    FormatLine(fileExtensions[fex], ForECol.Highlight);
                                }
                            }
                            else FormatLine("1 |(none)", ForECol.Normal);
                            NewLine(2);

                            createdDisplayQ = true;
                            Program.ToggleFormatUsageVerification();
                        }
                        else
                        {
                            Format($"Display could not be create. Unable to retrieve library version range.", ForECol.Incorrection);
                            Pause();
                        }


                        // CIV info inputs and CIV execution
                        if (createdDisplayQ)
                        {
                            string[] options = new string[] { "Version Range", "Folder Path", "File Extension" };
                            bool validOpt = TableFormMenu(out short optNum, "Edit CIV Parameters", subMenuUnderline, false, $"{Ind14}Select parameter to edit or enter any other key to continue >> ", "1~3/?", 3, options);
                            
                            /// IF valid input recieved: edit appropriate CIV parameter; ELSE run content integrity or exit page
                            if (validOpt)
                            {
                                NewLine(5);
                                HorizontalRule(cTHB, 0);
                                Important($"Edit {options[optNum - 1]}");

                                // version range edit
                                if (optNum == 1)
                                {
                                    if (!verEarliest.Equals(verLatest))
                                    {
                                        const string rngSplit = "-";
                                        FormatLine($"The earliest library version is {verEarliest}, and the latest library version is {verLatest}. The library version(s) to validate must exist within the aformentioned versions.", ForECol.Normal);
                                        NewLine();

                                        Format($"{Ind14}The current version range is ", ForECol.Normal);
                                        Highlight(true, verLow.Equals(verHigh) ? $"{verHigh} only." : $"{verLow} to {verHigh}.", verLow.ToString(), verHigh.ToString());
                                        FormatLine($"{Ind14}Follow prompt syntax with 'a' and 'bb' as major and minor numbers respectively.", ForECol.Accent);
                                        Format($"{Ind14}Enter the new version range >> ", ForECol.Normal);

                                        // validation of input
                                        string inputVer = StyledInput($"a.bb {rngSplit} a.bb  /OR/  a.bb");
                                        bool parsedAndFetchedNewVerRangeQ = false;
                                        if (inputVer.IsNotNE())
                                        {
                                            if (inputVer.Contains(rngSplit))
                                            {
                                                string[] verString = inputVer.Split(rngSplit, StringSplitOptions.RemoveEmptyEntries);
                                                if (verString.HasElements(2))
                                                {
                                                    if (VerNum.TryParse(verString[0], out VerNum verRngA))
                                                        if (VerNum.TryParse(verString[1], out VerNum verRngB))
                                                        {
                                                            if (verRngA.AsNumber.IsWithin(verEarliest.AsNumber, verLatest.AsNumber))
                                                                if (verRngB.AsNumber.IsWithin(verEarliest.AsNumber, verLatest.AsNumber))
                                                                {
                                                                    if (verRngA.Equals(verRngB))
                                                                        verLow = verHigh = verRngB;
                                                                    else
                                                                    {
                                                                        verLow = new VerNum(Math.Min(verRngA.AsNumber, verRngB.AsNumber));
                                                                        verHigh = new VerNum(Math.Max(verRngA.AsNumber, verRngB.AsNumber));
                                                                    }
                                                                    parsedAndFetchedNewVerRangeQ = true;
                                                                }
                                                                else IncorrectionMessageQueue($"Right version was beyond library version range ({verEarliest.ToStringNums()} - {verLatest.ToStringNums()})");
                                                            else IncorrectionMessageQueue($"Left version was beyond library version range ({verEarliest.ToStringNums()} - {verLatest.ToStringNums()})");
                                                        }
                                                        else IncorrectionMessageQueue("Right version range did not follow syntax of 'a.bb'");
                                                    else IncorrectionMessageQueue("Left version range did not follow syntax of 'a.bb'");
                                                }
                                                else IncorrectionMessageQueue("Input did not follow syntax of 'a.bb - a.bb'");
                                            }
                                            else
                                            {
                                                if (VerNum.TryParse(inputVer, out VerNum verSingle))
                                                    if (verSingle.AsNumber.IsWithin(verEarliest.AsNumber, verLatest.AsNumber))
                                                    {
                                                        verLow = verHigh = verSingle;
                                                        parsedAndFetchedNewVerRangeQ = true;
                                                    }
                                                    else IncorrectionMessageQueue($"Given version was beyond library version range ({verEarliest.ToStringNums()} - {verLatest.ToStringNums()})");
                                                else IncorrectionMessageQueue("Input did not follow syntax of 'a.bb'");
                                            }
                                        }

                                        // validation of input relayed
                                        IncorrectionMessageTrigger($"{Ind24}Version range remains unchanged:\n{Ind34}", ".");
                                        IncorrectionMessageQueue(null);
                                        if (parsedAndFetchedNewVerRangeQ)
                                        {
                                            if (verLow.Equals(verHigh))
                                                Format($"{Ind24}Version range has been changed to {verLow} only.", ForECol.Correction);
                                            else Format($"{Ind24}Version range has been changed to {verLow} through {verHigh}.", ForECol.Correction);
                                            Pause();
                                        }
                                    }
                                    else
                                    {
                                        Format($"Only one version exists in the library, thus the version range cannot be edited.", ForECol.Normal);
                                        Pause();
                                    }                                        
                                }

                                // folder path edits
                                else if (optNum == 2)
                                {
                                    FormatLine("Folder paths provide the CIV process with a destination to execute content validation. Multipler folder paths may be provided for content validation.", ForECol.Normal);
                                    NewLine();

                                    string placeHolder = @"C:\__\__";
                                    if (folderPaths.HasElements())
                                    {
                                        FormatLine($"{Ind14}The following folder paths have been provided: ", ForECol.Normal);
                                        List(OrderType.Ordered_Numeric, folderPaths.ToArray());
                                        NewLine();

                                        FormatLine($"{Ind14}An existing folder path may be removed by using their list number shown above.", ForECol.Accent);
                                        Format($"{Ind14}Remove/submit folder path >> ", ForECol.Normal);
                                        placeHolder = $"#  /OR/  " + placeHolder;
                                    }
                                    else
                                    {
                                        FormatLine($"{Ind14}There are no provided folder paths.", ForECol.Normal);
                                        FormatLine($"{Ind14}A folder's path could also be fetched from a given file's path.", ForECol.Accent);
                                        Format($"{Ind14}Submit folder path >> ", ForECol.Normal);                                        
                                    }

                                    // input validation
                                    string inputFolder = StyledInput(placeHolder);
                                    bool fetchedFolderPathOrIndexQ = false;
                                    const int nonFolderIx = -1;
                                    int folderIx = nonFolderIx;
                                    if (inputFolder.IsNotNE())
                                    {
                                        if (inputFolder.Contains(":\\"))
                                        {
                                            string[] pathParts = inputFolder.Split("\\", StringSplitOptions.RemoveEmptyEntries);
                                            if (pathParts.HasElements(2))
                                            {
                                                /// fetch folder path  ... then check if folder is real
                                                string fetchedFolderPath = "";
                                                for (int fdx = 0; fdx < pathParts.Length; fdx++)
                                                {
                                                    string pathPart = pathParts[fdx];
                                                    if (fdx + 1 == pathParts.Length)
                                                    {
                                                        if (!pathPart.Contains("."))
                                                            fetchedFolderPath += pathPart + "\\";
                                                    }
                                                    else fetchedFolderPath += pathPart + "\\";
                                                }

                                                if (!folderPaths.Contains(fetchedFolderPath))
                                                {
                                                    DirectoryInfo fetchedFolderInfo = new DirectoryInfo(fetchedFolderPath);
                                                    if (fetchedFolderInfo.Exists)
                                                    {
                                                        folderPaths.Add(fetchedFolderPath);
                                                        fetchedFolderPathOrIndexQ = true;
                                                    }
                                                    else IncorrectionMessageQueue($"Folder path '{fetchedFolderPath}' does not exist");
                                                }
                                                else IncorrectionMessageQueue("This folder path already exists within folder paths list");
                                            }
                                            else IncorrectionMessageQueue("No folders were specified (besides the hard drive)");
                                        }
                                        else if (folderPaths.HasElements())
                                        {
                                            if (int.TryParse(inputFolder, out int folderNum))
                                            {
                                                if (folderNum.IsWithin(1, folderPaths.Count))
                                                {
                                                    folderIx = folderNum - 1;
                                                    fetchedFolderPathOrIndexQ = true;
                                                }
                                                else IncorrectionMessageQueue("Folder number does not exist in preceding list of paths");
                                            }
                                            else IncorrectionMessageQueue("Neither a folder number nor a folder path was provided");
                                        }
                                        else IncorrectionMessageQueue("A hard drive was not specified in folder path");
                                    }

                                    // input validation relay
                                    IncorrectionMessageTrigger($"{Ind24}Folder Paths list remains unchanged:\n{Ind34}", ".");
                                    IncorrectionMessageQueue(null);
                                    if (fetchedFolderPathOrIndexQ)
                                    {
                                        if (folderIx == nonFolderIx && folderPaths.HasElements())
                                            Format($"{Ind24}Added new item to folder paths list:\n{Ind34}{folderPaths[^1]}.", ForECol.Correction);
                                        else
                                        {
                                            string removedPath = folderPaths[folderIx];
                                            folderPaths.RemoveAt(folderIx);
                                            Format($"{Ind24}Removed folder path #{folderIx + 1} from paths list:\n{Ind34}{removedPath}.", ForECol.Correction);
                                        }
                                        Pause();
                                    }
                                }

                                // file extension edits
                                else if (optNum == 3)
                                {
                                    FormatLine("A file extension is a suffix of a file name that relates to the file type. Providing file extensions allows the CIV process to target only specific kinds of files for content validation.", ForECol.Normal);
                                    NewLine();

                                    if (fileExtensions.HasElements())
                                    {
                                        Format($"{Ind14}The following file extensions are being used:\n{Ind24}", ForECol.Normal);
                                        for (int fx = 0; fx < fileExtensions.Count; fx++)
                                            Highlight(false, $"{fileExtensions[fx]}{(fx + 1 >= fileExtensions.Count ? "" : ", ")}", fileExtensions[fx]);
                                        NewLine();
                                    }
                                    else FormatLine($"{Ind14}There are currently no file extensions in use.", ForECol.Normal);
                                    FormatLine($"{Ind14}List file extensions in lower-case and separate with spaces (Ex: 'txt png').", ForECol.Accent);
                                    Format($"{Ind14}List new file extensions >> ", ForECol.Normal);

                                    // validation of input
                                    string inputFext = StyledInput("extA extB...");
                                    bool parsedAndFetchedFileExtensionsQ = false;
                                    if (inputFext.IsNotNE())
                                    {
                                        if (!inputFext.Contains(DataHandlerBase.Sep))
                                        {
                                            string[] fileExts = inputFext.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                            if (fileExts.HasElements())
                                            {
                                                fileExtensions.Clear();
                                                for (int fx = 0; fx < fileExts.Length; fx++)
                                                {
                                                    string fExt = fileExts[fx].Replace(".", "").ToLower();
                                                    fileExtensions.Add($".{fExt}");
                                                }
                                                parsedAndFetchedFileExtensionsQ = true;
                                            }
                                            else IncorrectionMessageQueue("No file extensions were listed");
                                        }
                                        else IncorrectionMessageQueue($"File extensions containing the '{DataHandlerBase.Sep}' character are not allowed.");
                                    }

                                    // relay validation of input
                                    IncorrectionMessageTrigger($"{Ind24}File Extensions list remains unchanged: ", ".");
                                    IncorrectionMessageQueue(null);
                                    if (parsedAndFetchedFileExtensionsQ)
                                    {
                                        Format($"{Ind24}File extensions list has been updated.", ForECol.Correction);
                                        Pause();
                                    }
                                }                                
                            }
                            else
                            {
                                /// IF no input: exit page; ELSE check data and run content integrity
                                if (LastInput.IsNE())
                                    endActiveMenuKey = true;
                                else
                                {
                                    NewLine();
                                    /// check data before running CIV
                                    bool readyToRunCIVQ = folderPaths.HasElements() && fileExtensions.HasElements();
                                    if (!readyToRunCIVQ)
                                    {
                                        if (folderPaths.HasElements())
                                            IncorrectionMessageQueue("At least one file extension is required before running CIV.");
                                        else IncorrectionMessageQueue("At least one folder path is required before running CIV.");
                                    }

                                    /// data check and validation 
                                    IncorrectionMessageTrigger($"{Ind14}CIV process could not be executed.\n{Ind24}Issue: ", null);
                                    IncorrectionMessageQueue(null);
                                    if (readyToRunCIVQ)
                                    {
                                        _contentValidator.Validate(new VerNum[] { verLow, verHigh }, folderPaths.ToArray(), fileExtensions.ToArray());
                                        Text("Wip...CIV was called");
                                        Pause();
                                    }
                                }
                            }
                        }
                        else endActiveMenuKey = true;
                    }

                    // view all data Ids
                    else if (setConIntKey.Equals("b") && librarySetup)
                    {
                        endActiveMenuKey = true;
                        Program.LogState("Settings|Content Integrity (wip)|View All Data IDs");
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
                        Dbug.Log("Fetching all Data IDs (Data Ids in angled brackets '<>' were rejected); Note that legend symbols are disregarded; ");
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

                            Dbug.Log("Printing Data ID categories; Data IDs in angled brackets '<>' are misc. IDs that required disassembling; ");
                            Dbug.NudgeIndent(true);
                            for (int lx = 0; lx < legendKeys.Count; lx++)
                            {
                                string legendKey = legendKeys[lx];
                                string legendDef = legendKey != miscPhrase ? legendDefs[lx].Replace($"{legendKey} ", "") : legendDefs[lx];
                                Dbug.Log($"Category '{legendKey}' [{legendDef}]");

                                Dbug.NudgeIndent(true);
                                string dataIDList = "";
                                int dataIDCount = 0;
                                for (int dx = 0; dx < allDataIds.Count; dx++)
                                {
                                    bool disableOrignalLogPrintQ = false;
                                    string datIDToPrint = "";
                                    string datID = allDataIds[dx];

                                    // print numeric data IDs
                                    if (legendKey != miscPhrase)
                                    {
                                        if (!LogDecoder.IsNumberless(datID))
                                        {
                                            LogDecoder.DisassembleDataID(datID, out string dk, out string db, out _);
                                            if (dk == legendKey)
                                                datIDToPrint = db;
                                        }
                                    }

                                    // print wordy data IDs (numberless)
                                    else
                                    {
                                        if (LogDecoder.IsNumberless(datID))
                                            datIDToPrint = datID;
                                        else
                                        {
                                            LogDecoder.DisassembleDataID(datID, out string dk, out string db, out _);
                                            if (!legendKeys.Contains(dk) && db.IsNotNE())
                                            {
                                                datIDToPrint = datID;
                                                
                                                disableOrignalLogPrintQ = true;
                                                Dbug.LogPart($"<{datIDToPrint}> ");
                                            }
                                        }
                                    }

                                    if (!disableOrignalLogPrintQ)
                                        Dbug.LogPart($"{datIDToPrint} ");
                                    if (datIDToPrint.IsNotNE())
                                    {
                                        dataIDList += $"{datIDToPrint} ";
                                        dataIDCount++;
                                    }
                                }

                                if (legendKey != miscPhrase)
                                {
                                    Dbug.Log(" ..  Condensing with ranges; ");
                                    string dataIDListWithRanges = Extensions.CreateNumericDataIDRanges(dataIDList.Split(" "));
                                    bool uncondensedQ = false;
                                    if (dataIDList.Contains(dataIDListWithRanges))
                                    {
                                        Dbug.LogPart("Remains uncondensed; ");
                                        uncondensedQ = true;
                                    }

                                    if (dataIDListWithRanges.IsNotNE() && !uncondensedQ)
                                    {
                                        Dbug.LogPart($":: {dataIDListWithRanges}");
                                        dataIDList = dataIDListWithRanges;
                                    }
                                }
                                Dbug.LogPart($" .. Counted '{dataIDCount}' data IDs; ");

                                // all printing here
                                if (legendKey != miscPhrase || (legendKey == miscPhrase && dataIDList.IsNotNE()))
                                {
                                    //Highlight(HSNL(0, 2) > 1, $"[{legendDef}] ('{dataIDCount}' IDs){Ind14}", $"[{legendDef}] ('{dataIDCount}' IDs)");
                                    Format($"[{legendDef}] ", ForECol.Highlight);
                                    Format($"<{dataIDCount}>{Ind14}", ForECol.Accent);
                                    HSNLPrint(0, HSNL(0, 2).Clamp(0, 1));

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
                            Highlight(false, $"Total Contents :: {allDataIds.Count}.", allDataIds.Count.ToString());
                            //Highlight(false, $"Total Textures Added :: {allDataIds.Count}.", allDataIds.Count.ToString());

                            //Format("All Data IDs found within library are displayed above.", ForECol.Accent);
                            Pause();
                        }
                        Dbug.EndLogging();

                        // method - more like 'remove legend suffixes'
                        string RemoveLegendSymbols(string str)
                        {
                            if (str.IsNotNE())
                            {
                                LogDecoder.DisassembleDataID(str, out string dk, out string db, out _);
                                str = dk + db;
                            }
                            //if (legendSymbols.HasElements() && str.IsNotNE())
                            //{
                            //    foreach (string legSym in legendSymbols)
                            //        str = str.Replace(legSym, "");
                            //    str = str.Trim();
                            //}
                            return str;
                        }
                    }

                    // no library??     :C
                    else if (!librarySetup)
                    {
                        NewLine(2);
                        Format("The library shelves are empty. This page requires the library to contain some data.", ForECol.Normal);
                        Pause();
                        endActiveMenuKey = true;
                    }

                    if (endActiveMenuKey)
                        activeMenuKey = null;

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
                                        Format($"\tFile save reversion could not be executed! (Restart Recommended)", ForECol.Incorrection);
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
                        bool okayToResetVersion = false, okayToClearLibrary = false;
                        string reversionDeniedMessage = "empty library";
                        VerNum lowest = VerNum.None, highest = VerNum.None;
                        if (_resLibrary != null)
                        {
                            if (_resLibrary.GetVersionRange(out lowest, out highest))
                            {
                                //reversionDeniedMessage = "library needs more information";
                                if (!lowest.Equals(highest))
                                    okayToResetVersion = true;
                                okayToClearLibrary = true;
                            }
                        }

                        // continue to version reversion
                        if (okayToResetVersion || okayToClearLibrary)
                        {
                            Program.LogState($"Settings|Reversion|Version Reversion - Allowed{(okayToResetVersion ? "" : " (Clearance Only)")}");
                            NewLine();
                            FormatLine($"Version reversion is {(okayToResetVersion ? "" : "(partially) ")}availble.", ForECol.Normal);
                            HorizontalRule('\'', 1);

                            string exmplRange = null, inputPlacholder = "a.bb", inputPrompt = "Revert to: ";
                            /// IF able to revert to a version: Version reversion menu and prompts; ELSE library clearance menu and prompts
                            if (okayToResetVersion)
                            {
                                /// pretext
                                Highlight(false, $"The latest library version is {highest}. The library may be reverted to the oldest library version, {lowest}. ", $"{highest}", $"{lowest}");
                                FormatLine("The library's shelves may also be completely cleared to remove all version data.", ForECol.Normal);
                                NewLine();

                                /// menu and prompt
                                VerNum closestReversion;
                                if (highest.MinorNumber - 1 < 0)
                                    closestReversion = new VerNum(highest.MajorNumber - 1, 99);
                                else closestReversion = new VerNum(highest.MajorNumber, highest.MinorNumber - 1);

                                exmplRange = lowest.AsNumber != closestReversion.AsNumber ? $"{lowest.ToStringNums()} ~ {closestReversion.ToStringNums()}" : $"{lowest.ToStringNums()}";
                                FormatLine($"Enter version to revert to ({exmplRange}) or enter any key to clear library.", ForECol.Normal);
                            }
                            else
                            {
                                /// pretext
                                FormatLine($"The library contains a single version's data ({lowest}), thus a true version reversion is unallowed. However, the library's shelf may be cleared to remove the existing version's data.", ForECol.Normal);
                                NewLine();

                                /// menu and prompt
                                FormatLine($"Enter any key to clear the library's data.", ForECol.Normal);
                                inputPrompt = "Clear Library Shelf: ";
                                inputPlacholder = null;
                            }
                            /// local prompt and input
                            Format($"{Ind24}{inputPrompt}", ForECol.Normal);
                            string input = StyledInput(inputPlacholder);

                            if (input.IsNotNEW())
                            {
                                /// revert to version
                                bool isRevertingToVersion = VerNum.TryParse(input, out VerNum verRevert);
                                if (isRevertingToVersion && okayToResetVersion)
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
                                        else
                                        {
                                            Format($"{Ind24}Cancelled library reversion to {verRevert.ToStringNums()}.", ForECol.Incorrection);
                                            Pause();
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
                                    Important(isRevertingToVersion && okayToResetVersion ? "Revert To Version" : "Clear Library");
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
