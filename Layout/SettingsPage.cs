using static HCResourceLibraryApp.Layout.PageBase;
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
        static bool enterCivFilesTransferPageQ = false;
        const string logStateParent = "Settings";

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
                BugIdeaPage.OpenPage();

                Program.LogState(logStateParent);
                Clear();
                Title("Application Settings", cTHB, 1);
                FormatLine($"{Ind24}Facilitates customization of visual preferences, and has additional tools for content verification and save state reversions.", ForECol.Accent);
                NewLine(2);
                Program.DisplayCurrentProfile();

                bool validMenuKey = ListFormMenu(out string setMenuKey, "Settings Menu", null, null, "a~d", true, $"Preferences,Content Integrity,Reversion,{exitPagePhrase}".Split(','));
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
            const string logStateMethod = logStateParent + "|Preferences";
            do
            {
                BugIdeaPage.OpenPage();

                if (activeMenuKey.IsNE())
                    Program.LogState(logStateMethod);

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
                        Program.LogState(logStateMethod + "|Window Dimensions Editor");
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
                        FormatLine("A restart is required to apply any changes to window dimensions.", ForECol.Accent);
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
                                    Highlight(true, $"{Ind24}The selected window dimensions are (as HxW): {newDimsTxt}", newDimsTxt);
                                    
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
                        Program.LogState(logStateMethod + "|Foreground Elements Color Editor");
                        
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
                        FormatLine($"#These Numbered Elements Affect: 'Title Art' [137], 'Color Coding' [123459].", ForECol.Accent);
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
        // done
        static void SubPage_ContentIntegrity()
        {
            bool exitContentIntegrityMenu = false;            
            string activeMenuKey = null;
            const string logStateMethod = logStateParent + "|Content Integrity";

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
                BugIdeaPage.OpenPage();

                // settings - ConInt main menu
                if (activeMenuKey.IsNE())
                    Program.LogState(logStateMethod);
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
                        const string logStateMethodSub = logStateMethod + "|Verify Content Integrity";
                        Program.LogState(logStateMethodSub);
                        FormatLine($"{Ind24}Content Integrity Verification (CIV) is a process that validates the existence of contents in the resource library within a given folder. [TO BE IMPLEMENTED] Validated contents can be moved to another folder after running the CIV process.", ForECol.Normal);
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
                        const string FPDTRep = "(Omit) "; /// folder path disabled token replacement
                        int countOmittedFolderPaths = 0;
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
                                countOmittedFolderPaths = 0;
                                for (int fpx = 0; fpx < folderPaths.Count; fpx++)
                                {
                                    string folderPath = folderPaths[fpx];
                                    if (!folderPath.StartsWith(ContentValidator.FolderPathDisabledToken))
                                    {
                                        Format($"{fpx + 1 - countOmittedFolderPaths,-2}|", ForECol.Normal);
                                        FormatLine(folderPath, ForECol.Highlight);
                                    }
                                    else countOmittedFolderPaths++;                                    
                                }
                                if (countOmittedFolderPaths > 0)
                                    FormatLine($"Excluding '{countOmittedFolderPaths}' folder paths from CIV process.", ForECol.Accent);
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
                            FormatLine("CIV parameters are saved after running CIV (Version Range excluded).", ForECol.Accent);
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
                        bool ranCIVq = false, prepToRunCivq = false;
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
                                    Program.LogState(logStateMethodSub + "|Version Range Edit");
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
                                    Program.LogState(logStateMethodSub + "|Folder Path Edits");
                                    int folderCondensingDist = WSLL(64, Console.LargestWindowWidth - 10);
                                    const int endPeekNum = 8;
                                    FormatLine("Folder paths provide the CIV process with a destination to execute content validation. Multiple folder paths may be provided for content validation.", ForECol.Normal);
                                    NewLine();

                                    ToggleFileChooserPage(true);
                                    string placeHolder = $@"C:\__\__  /OR/  {openFileChooserPhrase}";
                                    string startingFolderPath = null;
                                    if (folderPaths.HasElements())
                                    {
                                        startingFolderPath = folderPaths[0].Replace(ContentValidator.FolderPathDisabledToken, "");
                                        List<string> fPathsCondensed = new();
                                        foreach (string fPath in folderPaths)
                                        {
                                            if (fPath.Length > folderCondensingDist)
                                            {
                                                string shortFPath = fPath.Replace(ContentValidator.FolderPathDisabledToken, FPDTRep).Clamp((folderCondensingDist / 2) - endPeekNum, "...");
                                                shortFPath += fPath.Clamp(folderCondensingDist / 2, null, fPath.Substring(fPath.Length - endPeekNum).ToString(), false);
                                                fPathsCondensed.Add(shortFPath);
                                            }
                                            else fPathsCondensed.Add(fPath.Replace(ContentValidator.FolderPathDisabledToken, FPDTRep));
                                        }

                                        FormatLine($"{Ind14}The following (relative) folder paths have been provided: ", ForECol.Normal);
                                        List(OrderType.Ordered_Numeric, fPathsCondensed.ToArray());
                                        NewLine();

                                        FormatLine($"{Ind14}Select the folder number to edit or submit a new folder path.", ForECol.Accent);
                                        Format($"{Ind14}Edit/submit folder path >> ", ForECol.Normal);
                                        placeHolder = $"#  /OR/  " + placeHolder;
                                    }
                                    else
                                    {
                                        FormatLine($"{Ind14}There are no provided folder paths.", ForECol.Normal);
                                        FormatLine($"{Ind14}A folder's path can be fetched from a given file's path.", ForECol.Accent);
                                        Format($"{Ind14}Submit folder path >> ", ForECol.Normal);                                        
                                    }

                                    // input validation
                                    string inputFolder = StyledInput(placeHolder);
                                    FileChooserPage.ItemType = FileChooserType.Folders;
                                    FileChooserPage.OpenPage(startingFolderPath);
                                    ToggleFileChooserPage(false);
                                    if (inputFolder == openFileChooserPhrase)
                                        inputFolder = FileChooserPage.SelectedItem;

                                    bool fetchedFolderPathOrIndexQ = false;
                                    const int nonFolderIx = -1;
                                    int folderIx = nonFolderIx;
                                    if (inputFolder.IsNotNE())
                                    {
                                        /// IF input contains folder levels key: prep to add folder; ELSE IF there are existing folders: fetch folder to remove number; ELSE invalidate input
                                        if (inputFolder.Contains(@":\"))
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

                                                /// this IF checks for exact folder path...
                                                if (!folderPaths.Contains(fetchedFolderPath))
                                                {
                                                    /// ...so this bit also ensures that the submitted folder path isn't a sub-folder to other existing paths
                                                    bool isASubFolderPathQ = false;
                                                    foreach (string fPath in folderPaths)
                                                    {
                                                        if (fetchedFolderPath.Contains(fPath))
                                                        {
                                                            isASubFolderPathQ = true;
                                                            break;
                                                        }
                                                    }

                                                    DirectoryInfo fetchedFolderInfo = new DirectoryInfo(fetchedFolderPath);
                                                    if (fetchedFolderInfo.Exists && !isASubFolderPathQ)
                                                    {
                                                        folderPaths.Add(fetchedFolderPath);
                                                        fetchedFolderPathOrIndexQ = true;
                                                    }
                                                    else
                                                    {
                                                        if (!fetchedFolderInfo.Exists)
                                                            IncorrectionMessageQueue($"Folder path '{fetchedFolderPath}' does not exist");
                                                        else IncorrectionMessageQueue($"This folder path is a sub-folder to an existing (enabled) folder path in list");
                                                    }
                                                }
                                                else IncorrectionMessageQueue("This exact folder path already exists within folder paths list");
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
                                            NewLine();
                                            FormatLine($"{Ind14}Press [Enter] to toggle folder usage. Enter any key to remove folder.", ForECol.Accent);
                                            Format($"{Ind14}Remove / toggle usability of this folder path >> ");
                                            if (StyledInput("#a /OR/ __").IsNE())
                                            {
                                                string editedFPath = folderPaths[folderIx];
                                                bool disabledFPq = true;
                                                if (editedFPath.StartsWith(ContentValidator.FolderPathDisabledToken))
                                                {
                                                    editedFPath = editedFPath.Replace(ContentValidator.FolderPathDisabledToken, "");
                                                    disabledFPq = false;
                                                }
                                                else editedFPath = ContentValidator.FolderPathDisabledToken + editedFPath;

                                                folderPaths[folderIx] = editedFPath;
                                                Format($"{Ind24}{(disabledFPq? "Disabled" : "Enabled")} folder path #{folderIx + 1} in paths list: \n{Ind34}{editedFPath.Replace(ContentValidator.FolderPathDisabledToken, FPDTRep)}.", ForECol.Correction);
                                            }
                                            else
                                            {
                                                string removedPath = folderPaths[folderIx];
                                                folderPaths.RemoveAt(folderIx);
                                                Format($"{Ind24}Removed folder path #{folderIx + 1} from paths list:\n{Ind34}{removedPath.Replace(ContentValidator.FolderPathDisabledToken, FPDTRep)}.", ForECol.Correction);
                                            }
                                        }
                                        Pause();
                                    }
                                }

                                // file extension edits
                                else if (optNum == 3)
                                {
                                    Program.LogState(logStateMethodSub + "|File Extension Edits");
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
                                /// CONTEXT -> User chooses to exit page or run CIV process
                                /// IF no input: 
                                ///     IF (if CIV ran: prompt to move contents 'and' exit page; ELSE exit page)
                                /// ELSE check data and run content integrity
                                if (LastInput.IsNE())
                                {
                                    // tbd...
                                    if (ranCIVq)
                                    {
                                        NewLine();
                                        Format("-- [TBD] Prompt to move contents after running CIV goes here... -- ", ForECol.Accent);
                                        Wait(1.2f);
                                    }

                                    endActiveMenuKey = true;
                                }
                                else
                                {
                                    NewLine();
                                    /// check data before running CIV
                                    bool readyToRunCIVQ = folderPaths.HasElements() && fileExtensions.HasElements() && countOmittedFolderPaths < folderPaths.Count;
                                    if (!readyToRunCIVQ)
                                    {
                                        if (folderPaths.HasElements())
                                        {
                                            if (countOmittedFolderPaths >= folderPaths.Count)
                                                IncorrectionMessageQueue("At least one folder path must be enabled to run CIV.");
                                            else IncorrectionMessageQueue("At least one file extension is required before running CIV.");
                                        }
                                        else IncorrectionMessageQueue("At least one folder path is required before running CIV.");
                                    }

                                    /// data check and validation 
                                    IncorrectionMessageTrigger($"{Ind14}CIV process could not be executed.\n{Ind24}Issue: ", null);
                                    IncorrectionMessageQueue(null);
                                    if (readyToRunCIVQ)
                                    {
                                        prepToRunCivq = true;
                                        Format($"{Ind14}Running CIV process... ", ForECol.Correction);
                                        Wait(1f);
                                        Format($"Please wait...", ForECol.Normal);
                                        Program.LogState(logStateMethod + "|Verify Content Integrity - Executed");
                                        ranCIVq = _contentValidator.Validate(new VerNum[] { verLow, verHigh }, folderPaths.ToArray(), fileExtensions.ToArray());
                                    }
                                }
                            }
                        }

                        /** POST-CIV DISPLAY TYPES (from Design Doc)
                            - Expanded Form
					            Every Data ID is listed; there are no ranges among sequential groups of Data IDs. If a content was verified to exist their text will have a certain dark (or normal) color attributed to them. If a content doesn't exist a bright (or highlight) color will be attributed to them.
				            - Compact Form
					            Every Data ID is listed and there are ranges among sequential groups of Data IDs. The color coding for non-sequential content are the same as that in 'Expanded Form'. If a sequence of Data IDs are missing contents, the missing Data IDs within the sequence are listed and bracketed (unspaced) beside the range, and the sequence and listed missing contents are highlighted.
				            - Focused Form
					            Only Data IDs that were not verified to exist within the resource pack contents are listed. There are no ranges among sequential groups.	
			
			                > Display Example
			                .	Focused Form
				                ...................................
				                [Item]
				                9 45 78 79 80 115
				
				                [Miscellaneous]
				                AppleSauce_Chunks Dust
				
				                [Projectiles]
				                5 6
				
				                [Tiles]
				                114
				                ................................... 

                         */

                        // CIV results display
                        if (ranCIVq && _contentValidator.CivInfoDock.HasElements())
                        {
                            CivDisplayType civDisplayStyle = CivDisplayType.Compact;
                            bool exitCivResultPageQ = false;
                            int contentCount = _contentValidator.CivInfoDock.Length;

                            do
                            {
                                Clear();
                                Title(titleText, subMenuUnderline, 1);
                                Important($"CIV Results - {civDisplayStyle} View");

                                // should i display civ parameters somewhere here as well?  perhaps....

                                HorizontalRule('-');
                                DisplayCivResults(_contentValidator.CivInfoDock, civDisplayStyle);
                                HorizontalRule('-');

                                Highlight(true, $"Percentage of contents verified: {_contentValidator.ValidationPercentage:0.00}% of '{contentCount}' contents.", $"{_contentValidator.ValidationPercentage:0.00}%");
                                NewLine();

                                Format("Current display type is highlighted :: ");
                                ChangeNextHighlightColors(ForECol.Accent, ForECol.Normal);
                                Highlight(true, $"{CivDisplayType.Expanded} | {CivDisplayType.Compact} | {CivDisplayType.Focused}", civDisplayStyle.ToString());
                                Format($"{Ind14}Press [Enter] to toggle display type. Enter any key to exit CIV view >> ");

                                string civVInput = StyledInput(null);
                                if (civVInput.IsNEW())
                                {
                                    civDisplayStyle = (CivDisplayType)((civDisplayStyle.GetHashCode() + 1) % Enum.GetNames(typeof(CivDisplayType)).Length);
                                    NewLine();
                                    string displayShortDesc = "";
                                    switch (civDisplayStyle)
                                    {
                                        case CivDisplayType.Expanded:
                                            displayShortDesc = "All IDs, No Ranges";
                                            break;

                                        case CivDisplayType.Compact:
                                            displayShortDesc = "All IDs, Condensed";
                                            break;

                                        case CivDisplayType.Focused:
                                            displayShortDesc = "Unverified IDs Only, No Ranges";
                                            break;
                                    }
                                    Highlight(false, $"{Ind24}Switching to '{civDisplayStyle}' display: {displayShortDesc}... ", displayShortDesc, civDisplayStyle.ToString());
                                    Wait(2.5f);
                                }
                                else exitCivResultPageQ = true;
                            }
                            while (!exitCivResultPageQ);
                            


                        }
                        else if (prepToRunCivq)
                        {
                            NewLine();
                            Format($"{Ind24}Unable to collect and display CIV results. Please try again.", ForECol.Incorrection);
                            Pause();
                        }

                        /// exit page
                        if (!createdDisplayQ)
                            endActiveMenuKey = true;
                    }

                    // view all data Ids
                    else if (setConIntKey.Equals("b") && librarySetup)
                    {
                        endActiveMenuKey = true;
                        Program.LogState(logStateMethod + "|View All Data IDs");
                        Dbg.StartLogging("SettingsPage.SubPage_ContentIntegrity():ViewAllDataIds", out int stpgx);

                        // gather data
                        ProgressBarInitialize(false, false, 25, 0, 0, ForECol.Accent);
                        ProgressBarUpdate(0);
                        TaskCount = _resLibrary.Legends.Count;

                        /// fetch legend keys
                        const string miscPhrase = "Miscellaneous";
                        List<string> legendKeys = new() { miscPhrase }, legendDefs = new() { miscPhrase };
                        List<string> legendSymbols = new();
                        string legendKeysString = "";
                        Dbg.Log(stpgx, "Fetching Legend Keys (and Definitions) --> ");
                        Dbg.NudgeIndent(stpgx, true);
                        foreach (LegendData legDat in _resLibrary.Legends)
                        {
                            Dbg.LogPart(stpgx, $"Key '{legDat.Key}' | Added? {IsNotSymbol(legDat.Key)}");
                            if (IsNotSymbol(legDat.Key))
                            {
                                /// adding definition before key so they are sorted properly
                                legendKeys.Add($"{legDat[0]} {legDat.Key}");
                                legendDefs.Add($"{legDat[0]}");
                                legendKeysString += $"{legDat.Key} ";
                                Dbg.LogPart(stpgx, $" | and Definition '{legDat[0]}'");
                            }
                            else legendSymbols.Add(legDat.Key);
                            Dbg.Log(stpgx, "; ");

                            TaskNum++;
                            ProgressBarUpdate(TaskNum / TaskCount, true);
                        }
                        Dbg.NudgeIndent(stpgx, false);
                        Dbg.Log(stpgx, "Done, and sorted; ");
                        legendKeys = legendKeys.ToArray().SortWords();
                        legendDefs = legendDefs.ToArray().SortWords();

                        ProgressBarInitialize(false, false, 25);
                        TaskCount = _resLibrary.Contents.Count;
                        TaskNum = 0;

                        /// fetch all data ids
                        List<string> allDataIds = new();
                        Dbg.Log(stpgx, "Fetching all Data IDs (Data Ids in angled brackets '<>' were rejected); Note that legend symbols are disregarded; ");
                        Dbg.NudgeIndent(stpgx, true);
                        foreach (ResContents resCon in _resLibrary.Contents)
                        {
                            Dbg.LogPart(stpgx, $"From #{resCon.ShelfID} {$"'{resCon.ContentName}'", -30}  //  CBG :: ");
                            for (int cbx = 0; cbx < resCon.ConBase.CountIDs; cbx++)
                            {
                                string datID = RemoveLegendSymbols(resCon.ConBase[cbx]);
                                if (!allDataIds.Contains(datID))
                                {
                                    allDataIds.Add(datID);
                                    Dbg.LogPart(stpgx, $"{datID} ");
                                }
                                else Dbg.LogPart(stpgx, $"<{datID}> ");
                            }

                            if (resCon.ConAddits.HasElements())
                            {
                                Dbg.LogPart(stpgx, "  //  CAs ::");
                                for (int casx = 0; casx < resCon.ConAddits.Count; casx++)
                                {
                                    Dbg.LogPart(stpgx, $" |[{casx + 1}]");
                                    ContentAdditionals conAdt = resCon.ConAddits[casx];
                                    for (int cax = 0; cax < conAdt.CountIDs; cax++)
                                    {
                                        string datID = RemoveLegendSymbols(conAdt[cax]);
                                        if (!allDataIds.Contains(datID))
                                        {
                                            allDataIds.Add(datID);
                                            Dbg.LogPart(stpgx, $"{datID} ");
                                        }
                                        else Dbg.LogPart(stpgx, $"<{datID}> ");
                                    }
                                }
                            }
                            Dbg.Log(stpgx, "; ");

                            TaskNum++;
                            ProgressBarUpdate(TaskNum / TaskCount, true);
                        }
                        Dbg.NudgeIndent(stpgx, false);
                        Dbg.Log(stpgx, "Done, and sorted; ");
                        allDataIds = allDataIds.ToArray().SortWords();

                        ProgressBarUpdate(1, true);


                        // print data IDs in categories by legend
                        if (legendKeys.HasElements() && legendDefs.HasElements() && allDataIds.HasElements())
                        {
                            _resLibrary.GetVersionRange(out _, out VerNum latestVer);
                            FormatLine($"All data IDs from shelves of library (version: {latestVer.ToStringNums()}).", ForECol.Accent);

                            Dbg.Log(stpgx, "Printing Data ID categories; Data IDs in angled brackets '<>' are misc. IDs that required disassembling; ");
                            Dbg.NudgeIndent(stpgx, true);
                            for (int lx = 0; lx < legendKeys.Count; lx++)
                            {
                                string legendDef = legendDefs[lx];
                                string legendKey = legendDef != miscPhrase ? legendKeys[lx].Replace($"{legendDef} ", "") : legendKeys[lx];
                                bool isMiscCategoryQ = legendKey == miscPhrase;
                                Dbg.Log(stpgx, $"Category '{legendKey}' [{legendDef}]");
                                Dbg.NudgeIndent(stpgx, true);

                                // get data IDs for category (legend key)
                                string dataIDList = "";
                                int dataIDCount = 0;
                                for (int dx = 0; dx < allDataIds.Count; dx++)
                                {
                                    bool disableOrignalLogPrintQ = false;
                                    string datIDToPrint = "";
                                    string datID = allDataIds[dx];

                                    // print numeric data IDs
                                    if (!isMiscCategoryQ)
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
                                            if (!legendKeysString.Contains($"{dk} ") && db.IsNotNE())
                                            {
                                                datIDToPrint = datID;
                                                
                                                disableOrignalLogPrintQ = true;
                                                Dbg.LogPart(stpgx, $"<{datIDToPrint}> ");
                                            }
                                        }
                                    }

                                    if (!disableOrignalLogPrintQ)
                                        Dbg.LogPart(stpgx, $"{datIDToPrint} ");
                                    if (datIDToPrint.IsNotNE())
                                    {
                                        dataIDList += $"{datIDToPrint} ";
                                        dataIDCount++;
                                    }
                                }

                                // condense string of numbers (and stuff) with ranges
                                if (dataIDList.IsNotNE())
                                { /// wrapping
                                    // for misc
                                    if (isMiscCategoryQ)
                                    {
                                        Dbg.Log(stpgx, " .. Grouping and condensing misc data IDs; ");
                                        const string miscGroupSplitKey = "//misc//";
                                        string[] miscIDs = MiscDataIDGrouping(dataIDList, miscGroupSplitKey, true);

                                        string preReBuiltDataIdList = "";
                                        if (miscIDs.HasElements())
                                            for (int mx = 0; mx < miscIDs.Length; mx++)
                                            {
                                                preReBuiltDataIdList += $"\n{miscIDs[mx]}";
                                                if (mx + 1 < miscIDs.Length)
                                                    Dbg.Log(stpgx, $":: {miscIDs[mx]};");
                                                else Dbg.LogPart(stpgx, $":: {miscIDs[mx]}; ");
                                            }
                                        if (preReBuiltDataIdList.IsNotNE())
                                            dataIDList = preReBuiltDataIdList;
                                    }

                                    // for regulars
                                    else
                                    {
                                        Dbg.Log(stpgx, " ..  Condensing with ranges; ");
                                        string dataIDListWithRanges = Extensions.CreateNumericDataIDRanges(dataIDList.Split(" "));
                                        bool uncondensedQ = false;
                                        if (dataIDList.Contains(dataIDListWithRanges))
                                        {
                                            Dbg.LogPart(stpgx, "Remains uncondensed; ");
                                            uncondensedQ = true;
                                        }

                                        if (dataIDListWithRanges.IsNotNE() && !uncondensedQ)
                                        {
                                            Dbg.LogPart(stpgx, $":: {dataIDListWithRanges}");
                                            dataIDList = dataIDListWithRanges;
                                        }
                                    }
                                }
                                else Dbg.LogPart(stpgx, " .. No data IDs to condense .."); 
                                Dbg.LogPart(stpgx, $" .. Counted '{dataIDCount}' data IDs; ");

                                // all printing here
                                if (!isMiscCategoryQ || (isMiscCategoryQ && dataIDList.IsNotNE()))
                                {
                                    //Highlight(HSNL(0, 2) > 1, $"[{legendDef}] ('{dataIDCount}' IDs){Ind14}", $"[{legendDef}] ('{dataIDCount}' IDs)");
                                    Format($"[{legendDef}] ", ForECol.Highlight);
                                    Format($"<{dataIDCount}>{Ind14}", ForECol.Accent);
                                    if (isMiscCategoryQ)
                                        NewLine();
                                    else HSNLPrint(0, HSNL(0, 2).Clamp(0, 1));

                                    Format(dataIDList.Trim(), ForECol.Normal);
                                    NewLine(lx + 1 != legendKeys.Count ? 2 : 1);
                                }
                                Dbg.Log(stpgx, $" //  End '{legendKey}'");
                                Dbg.NudgeIndent(stpgx, false);
                            }
                            Dbg.NudgeIndent(stpgx, false);

                            // Display TTA
                            NewLine(HSNL(0, 2) > 1 ? 3 : 2);
                            FormatLine("-------", ForECol.Accent);
                            Highlight(false, $"Total Contents :: {allDataIds.Count}.", allDataIds.Count.ToString());
                            Pause();
                        }
                        Dbg.EndLogging(stpgx);

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
                BugIdeaPage.OpenPage();

                const string logStateMethod = logStateParent + "|Reversion";
                Program.LogState(logStateMethod);
                Clear();

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
                            Program.LogState(logStateMethod + "|File Save Revert - Allowed");
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
                            Program.LogState(logStateMethod + "|File Save Revert - Denied");
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
                                                Format(Ind34);
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
                                        Highlight(true, $"{Ind24}Enter the phrase '{clearLibPhrase}' below to clear library shelves.", clearLibPhrase);
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


        // civ display method and useful enum -- public, for testing
        public static void DisplayCivResults(ConValInfo[] civDatas, CivDisplayType displayType)
        {
            Dbg.StartLogging("SettingsPage.DisplayCivResults()", out int stpgx);
            if (civDatas.HasElements())
            {
                Dbg.Log(stpgx, $"Received ConValInfo array with '{civDatas.Length}' elements and a display type of '{displayType}'; ");
                Dbg.Log(stpgx, "Fetching all data IDs and generating Legend Data instances from ConValInfos received; ");

                // Get legend data from library
                const string miscPhrase = "Miscellaneous", invalidatedTag = DataHandlerBase.Sep;
                const string miscGroupSplitKey = "\\mSplit\\";
                LegendData[] legends = Array.Empty<LegendData>();
                if (_resLibrary.IsSetup())
                {
                    List<LegendData> allLegends = new();
                    List<string> legendDefs = new();
                    foreach (LegendData legDat in _resLibrary.Legends)
                        if (legDat.IsSetup())
                        {
                            allLegends.Add(legDat);
                            legendDefs.Add(legDat[0]);
                        }

                    allLegends.Add(new LegendData(miscPhrase, VerNum.None, miscPhrase));
                    legendDefs.Add(miscPhrase);
                    legendDefs = legendDefs.ToArray().SortWords();
                    legends = new LegendData[legendDefs.Count];

                    for (int ldx = 0; ldx < legendDefs.Count; ldx++)
                    {
                        LegendData aLegData = null;
                        for (int lgx = 0; lgx < allLegends.Count && aLegData == null; lgx++)
                        {
                            if (allLegends[lgx][0] == legendDefs[ldx])
                                aLegData = allLegends[lgx];
                        }
                        if (aLegData != null)
                            legends[ldx] = aLegData;
                    }                   
                }
                Dbg.Log(stpgx, $" -> Fetched '{legends.Length}' Legend Datas from ResLibrary; ");


                // Categorize, print and indicate verification of data IDs
                if (legends.HasElements())
                {
                    Dbg.Log(stpgx, $"Proceeding to categorize, print, and indicate verification of all IDs; Display type: {displayType}; ");
                    Dbg.Log(stpgx, $"Any Data ID marked with '{invalidatedTag}' has been invalidated by CIV process; ");
                    List<string> legendKeysList = new();
                    foreach (LegendData ld in legends)
                        legendKeysList.Add(ld.Key);

                    Program.ToggleFormatUsageVerification();
                    Dbg.NudgeIndent(stpgx, true);
                    for (int lx = 0; lx < legends.Length; lx++)
                    {
                        LegendData legDat = legends[lx];
                        List<string> invalidDataIDsList = new();
                        Dbg.Log(stpgx, $"Category '{legDat.Key}' [{legDat[0]}]");

                        Dbg.NudgeIndent(stpgx, true);
                        string dataIDList = "";
                        int dataIDCount = 0, dataIDinvalidatedCount = 0;
                        bool isMiscCategoryQ = legDat.Key == miscPhrase;
                        // obtain data IDs as a long string and mark all dependent on their verification status
                        for (int cx = 0; cx < civDatas.Length; cx++)
                        {
                            string dataIDToPrint = "";
                            ConValInfo civInfo = civDatas[cx];

                            /// BYPASS ID or not?
                            ///     Bypass if: 
                            ///         is validated && focused Display
                            ///         
                            ///     Allow if:
                            ///         not validated && focused Display
                            ///         ... && not focused Display
                            ///             validated && not focused Display
                            ///             not validated && not focused Display
                            ///         
                            bool bypassIDToPrintq = civInfo.IsValidated && displayType == CivDisplayType.Focused;
                            if (!bypassIDToPrintq)
                            {
                                // print numeric data IDs
                                if (!isMiscCategoryQ)
                                {
                                    LogDecoder.DisassembleDataID(civInfo.OriginalDataID, out string dk, out string db, out _);
                                    if (dk == legDat.Key)
                                        dataIDToPrint = db;
                                }

                                // print wordy data IDs
                                else
                                {
                                    LogDecoder.DisassembleDataID(civInfo.OriginalDataID, out string dk, out string db, out _);
                                    if (dk.IsNE())
                                        dataIDToPrint = db;
                                    else
                                    {
                                        if (!legendKeysList.Contains(dk) && db.IsNotNE())
                                            dataIDToPrint = civInfo.OriginalDataID;
                                    }
                                }

                                
                                if (!dataIDToPrint.IsNE())
                                {
                                    if (!civInfo.IsValidated)
                                    {
                                        invalidDataIDsList.Add($"{dataIDToPrint}");
                                        dataIDToPrint = invalidatedTag + dataIDToPrint;
                                        dataIDinvalidatedCount++;
                                    }

                                    Dbg.LogPart(stpgx, $"{dataIDToPrint} ");
                                    dataIDList += $"{dataIDToPrint} ";
                                    dataIDCount++;
                                }
                            }
                        }

                        // group misc data IDs and condense if allowed 
                        List<string> miscDataIDListWithRanges = new();
                        if (isMiscCategoryQ)
                        {
                            Dbg.Log(stpgx, "; ");
                            Dbg.Log(stpgx, "Splitting misc data IDs into letter groups; ");
                            string[] preMiscDataIDListWithRanges = MiscDataIDGrouping(dataIDList, miscGroupSplitKey, displayType == CivDisplayType.Compact, invalidatedTag, ' ', false);
                            if (preMiscDataIDListWithRanges.HasElements())
                                foreach (string mDataList in preMiscDataIDListWithRanges)
                                {
                                    bool brokenToPartsQ = false;
                                    string dbgText = "";
                                    if (mDataList.Contains(' '))
                                    {
                                        string[] mDataListParts = mDataList.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                        if (mDataListParts.HasElements())
                                        {
                                            brokenToPartsQ = true;
                                            dbgText = $"Broken into '{mDataListParts.Length}' data ID bits; ";
                                            miscDataIDListWithRanges.AddRange(mDataListParts);
                                        }
                                    }

                                    if (!brokenToPartsQ)
                                        miscDataIDListWithRanges.Add(mDataList);
                                    Dbg.Log(stpgx, $":: {mDataList}; {dbgText}");
                                }
                        }

                        // condense to ranges if allowed (Compact Only)
                        if (displayType == CivDisplayType.Compact && !isMiscCategoryQ)
                        {
                            Dbg.Log(stpgx, " ..  Condensing with ranges; ");
                            string dataIDListWithRanges = Extensions.CreateNumericDataIDRanges(dataIDList.Split(" "), false);
                            bool uncondensedQ = false;
                            if (dataIDList.Contains(dataIDListWithRanges))
                            {
                                Dbg.LogPart(stpgx, "Remains uncondensed; ");
                                uncondensedQ = true;
                            }

                            if (dataIDListWithRanges.IsNotNE() && !uncondensedQ)
                            {
                                Dbg.LogPart(stpgx, $":: {dataIDListWithRanges}");
                                dataIDList = dataIDListWithRanges;
                            }
                        }
                        Dbg.LogPart(stpgx, $" .. Counted '{dataIDCount}' data IDs, '{dataIDinvalidatedCount}' were invalidated; ");


                        // all printing here
                        if ((!isMiscCategoryQ && dataIDList.IsNotNE()) || (isMiscCategoryQ && dataIDList.IsNotNE()))
                        {
                            float percentValid = ((float)(dataIDCount - dataIDinvalidatedCount)) / dataIDCount * 100f;
                            Dbg.LogPart(stpgx, $"[{percentValid:0.#}% valid]; ");

                            Format($"[{legDat[0]}] ", ForECol.Highlight);
                            if (displayType != CivDisplayType.Focused)
                                Format($"<{dataIDCount} | {(int)percentValid}% valid>{Ind14}", ForECol.Accent);
                            else Format($"<{dataIDCount}>{Ind14}", ForECol.Accent);
                            HSNLPrint(0, HSNL(0, 2).Clamp(0, 1));

                            string[] dataIDsToPrint = dataIDList.Split(' ');
                            if (isMiscCategoryQ && miscDataIDListWithRanges.HasElements())
                                dataIDsToPrint = miscDataIDListWithRanges.ToArray();

                            int countPrints = 0;
                            foreach (string dataID in dataIDsToPrint)
                            {
                                if (dataID.IsNotNEW())
                                {
                                    const ForECol letterRangeCol = ForECol.Accent;
                                    bool isMiscLetterRangeQ = dataID.StartsWith("[");
                                    string miscIDPad = isMiscCategoryQ ? " " : "";

                                    /// {\n}[A~Z]...
                                    if (isMiscCategoryQ && isMiscLetterRangeQ)
                                        NewLine();

                                    if (dataID.Contains(invalidatedTag))
                                        Format($"{(countPrints == 0? "" : " ")}{dataID.Replace(invalidatedTag, "")}{miscIDPad}", isMiscLetterRangeQ ? letterRangeCol : ForECol.Incorrection);
                                    else Format($"{(countPrints == 0 ? "" : " ")}{dataID}{miscIDPad}", isMiscLetterRangeQ ? letterRangeCol : ForECol.Correction);

                                    /// ...[A~Z]{\n}
                                    if (isMiscCategoryQ && isMiscLetterRangeQ)
                                        NewLine();
                                }
                                if (!isMiscCategoryQ)
                                    countPrints++;
                            }
                            NewLine(lx + 1 != legends.Length ? 2 : 1);
                        }
                        Dbg.Log(stpgx, $" //  End '{legDat.Key}'");
                        Dbg.NudgeIndent(stpgx, false);
                    }
                    Dbg.NudgeIndent(stpgx, false);
                    Program.ToggleFormatUsageVerification();

                    /**
                        for (int lx = 0; lx < legendKeys.Count; lx++)
                        {
                            string legendKey = legendKeys[lx];
                            string legendDef = legendKey != miscPhrase ? legendDefs[lx].Replace($"{legendKey} ", "") : legendDefs[lx];
                            Dbg.Log(stpgx, $"Category '{legendKey}' [{legendDef}]");

                            Dbg.NudgeIndent(stpgx, true);
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
                                            Dbg.LogPart(stpgx, $"<{datIDToPrint}> ");
                                        }
                                    }
                                }

                                if (!disableOrignalLogPrintQ)
                                    Dbg.LogPart(stpgx, $"{datIDToPrint} ");
                                if (datIDToPrint.IsNotNE())
                                {
                                    dataIDList += $"{datIDToPrint} ";
                                    dataIDCount++;
                                }
                            }

                            if (legendKey != miscPhrase)
                            {
                                Dbg.Log(stpgx, " ..  Condensing with ranges; ");
                                string dataIDListWithRanges = Extensions.CreateNumericDataIDRanges(dataIDList.Split(" "));
                                bool uncondensedQ = false;
                                if (dataIDList.Contains(dataIDListWithRanges))
                                {
                                    Dbg.LogPart(stpgx, "Remains uncondensed; ");
                                    uncondensedQ = true;
                                }

                                if (dataIDListWithRanges.IsNotNE() && !uncondensedQ)
                                {
                                    Dbg.LogPart(stpgx, $":: {dataIDListWithRanges}");
                                    dataIDList = dataIDListWithRanges;
                                }
                            }
                            Dbg.LogPart(stpgx, $" .. Counted '{dataIDCount}' data IDs; ");

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
                            Dbg.Log(stpgx, $" //  End '{legendKey}'");
                            Dbg.NudgeIndent(stpgx, false);
                        }
                     
                     */
                }
            }
            else Dbg.Log(stpgx, "Received no ConValInfos data to create a display; ");
            Dbg.EndLogging(stpgx);
        }
        public static void ShowPrefsColors(Preferences prefs)
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

        static string[] MiscDataIDGrouping(string dataIDList, string groupSplitKey, bool condenseQ, string invalidatedTag = null, char splitChar = ' ', bool sortIDs = true, bool separateFromLetterRangeQ = true)
        {
            string[] miscGroupedAndSortedIDs = null;
            string[] filteredDataIDs = null;
            if (splitChar.IsNotNull() && dataIDList.IsNotNE())
                filteredDataIDs = dataIDList.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

            if (filteredDataIDs.HasElements() && groupSplitKey.IsNotNEW())
            {
                string miscDataIDList = "", partMiscDIDList = "", letterRange = "";
                int dataIDCount = filteredDataIDs.Length;
                int groupSize = dataIDCount / (dataIDCount / 10).Clamp(1, 5);
                for (int mx = 0; mx < dataIDCount; mx++)
                {
                    string mDatId = filteredDataIDs[mx];
                    bool endGroupingQ = mx + 1 == dataIDCount;    
                    partMiscDIDList += $"{mDatId} ";
                    if (mx % groupSize == 0 || endGroupingQ)
                    {
                        if (!letterRange.IsNE())
                        {
                            if (invalidatedTag.IsNotNEW())
                                letterRange += $"{mDatId.Replace(invalidatedTag, "")[0]}]";
                            else letterRange += $"{mDatId[0]}]";

                            if (separateFromLetterRangeQ)
                                miscDataIDList += $"{letterRange}{groupSplitKey}{partMiscDIDList}{groupSplitKey}";
                            else miscDataIDList += $"{letterRange}{Ind24}{partMiscDIDList}{groupSplitKey}";
                            partMiscDIDList = "";
                        }

                        if (!endGroupingQ)
                        {
                            string mDatIDForLetterTag = mDatId;
                            if (miscDataIDList.IsNotNE())
                                mDatIDForLetterTag = filteredDataIDs[mx + 1];

                            if (invalidatedTag.IsNotNEW())
                                letterRange = $"[{mDatIDForLetterTag.Replace(invalidatedTag, "")[0]}~";
                            else letterRange = $"[{mDatIDForLetterTag[0]}~";
                        }
                    }
                }

                if (miscDataIDList.IsNotNE())
                {
                    if (condenseQ)
                        miscGroupedAndSortedIDs = Extensions.CreateNumericDataIDRanges(miscDataIDList, groupSplitKey, ' ', sortIDs);
                    else miscGroupedAndSortedIDs = miscDataIDList.Split(groupSplitKey, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            return miscGroupedAndSortedIDs;
        }              
    }
}
