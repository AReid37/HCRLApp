using System;
using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HCResourceLibraryApp.Layout
{
    public static class ProfilesPage
    {
        /**
         * FIELDS / PROPERTIES
         * - stc ProfileHandler _profsHandler
         * - stc Preferences _preferencesRef
         * - stc rdon chr subMenuUnderline = '~'
         * 
         * METHODS
         * - pbl stc vd OpenPage()
         * - stc vd SubPage_ProfileEditor()
         */

        static Preferences _preferencesRef;
        static readonly char subMenuUnderline = '~';


        public static void GetPreferencesReference(Preferences preferences)
        {
            _preferencesRef = preferences;
        }
        public static void OpenPage()
        {
            bool exitProfilesPageMain = false;
            do
            {
                BugIdeaPage.OpenPage();

                Program.LogState("Profiles Page");
                Clear();
                Title("Profiles Page", cTHB, 1);
                FormatLine($"{Ind24}... some description ...", ForECol.Accent);
                NewLine(2);

                bool anyProfilesDetectedQ = ProfileHandler.AllProfiles.HasElements();
                bool anyProfileSelectedQ = ProfileHandler.CurrProfileID != ProfileHandler.NoProfID;

                string profMenuKey = null;
                bool validMenuKey = true;
                // IF profiles detected AND one is selected: Regular Menu;
                // ELSE (IF no profiles: create profile; ELSE select profile)
                if (anyProfilesDetectedQ && anyProfileSelectedQ)
                {
                    validMenuKey = ListFormMenu(out profMenuKey, "Profile Select Menu", null, null, "a~c", true, $"Select Profile,Edit Profile,{exitPagePhrase}".Split(','));
                    MenuMessageQueue(!validMenuKey, false, null);
                }
                else
                {
                    if (!anyProfilesDetectedQ)
                    {
                        FormatLine($"{Ind24}No Profiles Detected.", ForECol.Incorrection);
                        Format($"{Ind24}Proceeding to profile creation page.");
                        Pause();
                        profMenuKey = "b";
                    }
                    else
                    {
                        FormatLine($"{Ind24}No Profile Selected.", ForECol.Warning);
                        Format($"{Ind24}Proceeding to profile selection page.");
                        Pause();
                        profMenuKey = "a";
                    }
                    exitProfilesPageMain = true; /// temporary
                }


                if (validMenuKey)
                {
                    switch (profMenuKey)
                    {
                        case "a":
                            SubPage_ProfileSelect();
                            break;

                        case "b":
                            SubPage_ProfileEditor();
                            break;

                        case "c":
                            exitProfilesPageMain = true;
                            break;
                    }
                }
            }
            while (!exitProfilesPageMain && !Program.AllowProgramRestart);

            // no auto-saving here??
        }


        // tbd
        static void SubPage_ProfileSelect()
        {
            Program.LogState("Profiles Page|Select");
            NewLine(4);
            Format($"-- Openned Profile Selection Page --");
            Pause();

        }
        // tbd
        static void SubPage_ProfileEditor()
        {
            const int profNameMin = ProfileHandler.ProfileNameMinimum, profNameMax = ProfileHandler.ProfileNameLimit;
            bool exitProfEditorQ = false, anyProfilesDetected, skipExternalProfNoticeQ = false;
            bool anyProfilesCreatedOrDeletedQ = false; // program restart initiator
            string activeMenuKey = null;
            ProfileInfo currProfile = new(), prevCurrProfile = new();
            
            anyProfilesDetected = ProfileHandler.AllProfiles.HasElements();
            if (anyProfilesDetected)
            {
                currProfile = ProfileHandler.GetCurrentProfile();
                prevCurrProfile = currProfile;
            }


            do
            {
                BugIdeaPage.OpenPage();

                if (activeMenuKey.IsNE())
                    Program.LogState("Profiles Page|Editor");
                Clear();


                string[] profEditMenuOpts = { "Create Profile", "Edit Profile", $"{exitSubPagePhrase} [Enter]" };
                bool validKey = false, quitMenuQ = false;
                string profEditKey = null;
                if (activeMenuKey.IsNE())
                {
                    validKey = ListFormMenu(out profEditKey, "Profile Editor Menu", subMenuUnderline, null, "a/b", true, profEditMenuOpts);
                    quitMenuQ = LastInput.IsNE() || profEditKey == "c";
                    MenuMessageQueue(!validKey && !quitMenuQ, false, null);
                }
               

                if ((validKey && !quitMenuQ) || activeMenuKey.IsNotNE())
                {
                    // auto-return (only one page)
                    bool endActiveMenuKeyQ = false;
                    if (activeMenuKey.IsNE())
                        activeMenuKey = profEditKey;
                    else if (activeMenuKey.IsNotNE())
                        profEditKey = activeMenuKey;


                    string titleText = "Profile " + (profEditKey.Equals("a") ? "Creation" : "Editor");
                    Clear();
                    Title(titleText, subMenuUnderline, 1);

                    // create profile -- simply, register new profile.
                    if (profEditKey.Equals("a"))
                    {
                        Program.LogState("Profiles Page|Editor|Create");
                        FormatLine("A new profile may be created or an existing profile may be duplicated and saved as a new profile.");
                        FormatLine("Creating a profile will require a program restart.", ForECol.Warning);
                        FormatLine($"Remaining Profile Spaces :: '{ProfileHandler.RemainingProfileSpacesLeft}' available.", ForECol.Highlight);
                        NewLine();


                        // profile space available
                        if (ProfileHandler.RemainingProfileSpacesLeft > 0)
                        {
                            // force create if no profile, else give option to create before gettin' into it
                            bool createProfQ = true;
                            if (anyProfilesDetected)
                            {
                                Confirmation($"{Ind14}Proceed to profile creation? ", false, out createProfQ);
                                NewLine();
                            }                            

                            if (createProfQ)
                            {
                                // they get to rename it, at the very least
                                ProfileInfo newProfInfo = ProfileHandler.GetDefaultProfile();
                                string profileName;

                                /// 1a. external profile integration
                                if (ProfileHandler.ExternalProfileDetectedQ)
                                {
                                    Title("Profile Integration", subMenuUnderline);
                                    FormatLine($"{Ind14}An external profile has been detected. ", ForECol.Highlight);
                                    FormatLine($"{Ind14}The single-file data operation of this application has been discontinued with the introduction of a profile system. The external profile is the described 'single-file data' that has been stored in the parent folder of the application's data storage (Folder 'hcd').");

                                    NewLine();
                                    FormatLine($"{Ind14}This external profile's information will be integrated to the 1st profile. The original file WILL NOT be deleted.");
                                    Format($"{Ind14}Press [Enter] to continue to profile creation. ");

                                    if (!skipExternalProfNoticeQ)
                                    {
                                        Pause();
                                        skipExternalProfNoticeQ = true;
                                    }
                                }


                                /// 1b. profile name
                                NewLine(2);
                                Title("Profile Name", subMenuUnderline);
                                FormatLine($"{Ind14}Default profile name generated: {newProfInfo.profileName}.", ForECol.Highlight);
                                FormatLine($"{Ind14}Please provide a profile name. Minimum of {profNameMin} characters, maximum of {profNameMax} characters. May not include '{DataHandlerBase.Sep}' character.");
                                Format($"{Ind14}Profile Name >> ");
                                profileName = StyledInput("|   .    :    .    :    .    |");
                                bool validProfileNameQ = false;

                                if (profileName == "")
                                    profileName = null;

                                if (profileName.IsNotEW())
                                {
                                    /// IF 'null': use generated name; ELSE IF 'a value' user-provided name (and checks); ELSE alert, require value
                                    if (profileName == null)
                                    {
                                        profileName = newProfInfo.profileName;
                                        validProfileNameQ = true;
                                    }

                                    else if (profileName.IsNotNEW())
                                    {
                                        if (!profileName.Contains(DataHandlerBase.Sep))
                                        {
                                            if (profileName.Length.IsWithin(profNameMin, profNameMax))
                                                validProfileNameQ = true;
                                            else IncorrectionMessageQueue($"Profile name must be between {profNameMin} and {profNameMax} characters in length");
                                        }
                                        else IncorrectionMessageQueue($"Profile name may not contain '{DataHandlerBase.Sep}' character");
                                    }
                                }
                                else IncorrectionMessageQueue("A value must be provided for new profile name");
                                if (!validProfileNameQ)
                                    IncorrectionMessageTrigger($"{Ind24}[X] ", ".");


                                /// 1c. duplicate profile information
                                bool duplicateCurrentProfileQ = false;
                                if (anyProfilesDetected)
                                {
                                    NewLine(2);
                                    Title("Profile Duplication", subMenuUnderline);
                                    FormatLine($"{Ind14}The contents of the current profile can be duplicated into the new profile.");
                                    Confirmation($"{Ind24}Duplicate current profile information to new profile? ", false, out bool dupeInfoQ);
                                    if (dupeInfoQ)
                                    {
                                        Confirmation($"{Ind24}Absolutely sure of profile duplication? ", false, out bool absolutelyDupeInfoQ);
                                        if (absolutelyDupeInfoQ)
                                            duplicateCurrentProfileQ = true;
                                        ConfirmationResult(absolutelyDupeInfoQ, $"{Ind34}", "Duplicating profile information to new.", "Current profile will not be duplicated.");
                                    }
                                }


                                /// 1d. profile name confirmation
                                if (validProfileNameQ)
                                {
                                    string confirmPromptExtra = "";
                                    NewLine();
                                    FormatLine($"{Ind14}- - - - - - -", ForECol.Accent);
                                    Format($"{Ind14}The name of the new profile is: ");
                                    Highlight(false, $"'{profileName}'.", profileName);
                                    if (profileName == newProfInfo.profileName)
                                        Format(" Default Generated", ForECol.Accent);
                                    NewLine();

                                    if (duplicateCurrentProfileQ)
                                    {
                                        FormatLine($"{Ind14}Current profile information will be duplicated to new profile.");
                                        confirmPromptExtra = " and info duplication";
                                    }

                                    // IF confirmed: create profile and exit; ELSE (IF other profiles exist: don't create profile and exit; ELSE return to create prompt)
                                    Confirmation($"{Ind14}Confirm profile name{confirmPromptExtra} >> ", true, out bool yesNo);
                                    if (yesNo)
                                    {
                                        newProfInfo.profileName = profileName;
                                        bool profileCreatedQ = ProfileHandler.CreateProfile(newProfInfo, ProfileHandler.ExternalProfileDetectedQ || duplicateCurrentProfileQ);

                                        if (!profileCreatedQ)
                                        {
                                            NewLine();
                                            FormatLine($"{Ind24}There was an issue creating your profile. Please try again.", ForECol.Incorrection);
                                            Pause();
                                        }
                                        endActiveMenuKeyQ = profileCreatedQ;
                                        anyProfilesCreatedOrDeletedQ = profileCreatedQ;
                                    }
                                    else
                                    {
                                        if (anyProfilesDetected)
                                            endActiveMenuKeyQ = true;
                                    }
                                }
                            }
                            else
                            {
                                endActiveMenuKeyQ = true;
                            }
                        }

                        // no profile space available
                        else
                        {
                            int profMaxNum = ProfileHandler.AllProfiles.Count;
                            FormatLine($"{Ind24}There are no more profile spaces available.", ForECol.Warning);
                            Format($"{Ind24}The maximum number of profiles ({profMaxNum}) has been reached. To create a new profile, an existing profile must be deleted.");
                            Pause();

                            endActiveMenuKeyQ = true;
                        }
                    }

                    // edit profile -- all editing, new and old, and deletion
                    if (profEditKey.Equals("b"))
                    {
                        Program.LogState("Profiles Page|Editor|Edit");
                        FormatLine($"{Ind14}An active profile may be customized from name to icon color schema on this page.");

                        if (anyProfilesDetected)
                        {
                            FormatLine($"{Ind14}Select one of the options below to begin customizing this profile.");
                            NewLine();

                            // small profile display here
                            // ---

                            // menu and stuff
                            // - change profile display style (tiny, normal, double)
                            // - profile name -- profile icon type -- profile icon colors -- profile description
                            // - confirm profile changes

                            Pause();
                        }
                        else
                        {
                            Format($"{Ind24}There are no profiles to edit. Please create a profile to access this page.", ForECol.Warning);
                            Pause();
                        }
                        endActiveMenuKeyQ = true;
                    }


                    /// force restart and save
                    if (anyProfilesCreatedOrDeletedQ)
                    {
                        Program.RequireRestart();
                        Program.SaveData(false);
                        endActiveMenuKeyQ = true;
                    }
                    if (endActiveMenuKeyQ)
                        activeMenuKey = null;
                }
                else if (quitMenuQ)
                {
                    /// --- may not exit editor until a profile exists ---
                    if (anyProfilesDetected)
                        exitProfEditorQ = true;
                    else
                    {
                        NewLine();
                        Format($"{Ind24}You may not exit this page until at least 1 profile has been created.", ForECol.Incorrection);
                        Pause();
                    }
                }
            }
            while (!exitProfEditorQ && !Program.AllowProgramRestart);
        }



        
        // TOOL METHODS
        /// <summary>For a horizontal display of the profile.</summary>
        public static void DisplayProfileInfo(ProfileInfo profileInfo, ProfileIconSize profIconSize = ProfileIconSize.Normal, ProfileDisplayStyle profDisplay = ProfileDisplayStyle.Full)
        {
            /** Text layout planning by profile icon size
             * 
             * MINI
             * 4x4  Name Only       All Details
             * ---  ---             ---
             * 1 |  -               {profName}  #{profID}
             *   |  {profName}      {iconStyle} {iconStyleColorsCSV}
             *   |  #{profID}       "{descLine1}"
             * 4 |  -               "{descLine2-clipped}"
             * Left Indent :: 2 + (4*2) -> 10 chars
             * 
             * 
             * NORMAL
             * 8x8  Name Only       All Details
             * ---  ---             ---
             * 1 |                  -
             *   |  -               {profName} #{profID}
             *   |                  -
             * 4 |  {profName}      {iconStyle}
             * 5 |  #{profID}       {iconStyleColorsCSV}
             *   |                  "{descLine1}"
             *   |  -               "{descLine2-clipped}"
             * 8 |                  -
             * Left Indent :: 2 + (8*2) -> 18 chars
             * 
             * 
             * DOUBLED
             * 16x16    Name Only       All Details
             * ---      ---             ---
             * 1 |                      
             *   |                      -
             *   |                      {profName}
             * 4 |                      #{profID}
             * 5 |      -               
             *   |                      -
             *   |                      {iconStyle}
             * 8 |      {profName}      {iconStyleColorsCSV}
             * 9 |      #{profID}       
             *   |                      -
             *   |                      "{descLine1}"
             * 12|      -               "{descLine2}"
             * 13|                      "{descLine3-clipped}"
             *   |                      
             *   |                      -
             * 16|
             * Left Indent :: 2 + (16*2) -> 34 chars 
             * 
             */

            if (profileInfo.IsSetupQ())
            {
                // meta setup
                const bool _useDbugDescSplitingQ = false;  // a debug control; isolated

                // set up
                const string clampSuffix = "...", clampWordSuffix = "-";
                int leftIndent = 0, lineCount = 0;
                switch (profIconSize)
                {
                    case ProfileIconSize.Mini:
                        leftIndent = 2 + (4 * 2);
                        lineCount = 4;
                        break;

                    case ProfileIconSize.Normal:
                        leftIndent = 2 + (8 * 2);
                        lineCount = 8;
                        break;

                    case ProfileIconSize.Doubled:
                        leftIndent = 2 + (16 * 2);
                        lineCount = 16;
                        break;
                }
                leftIndent += 2; // for indent space away from profile;
                int cursorTop = Console.CursorTop;
                int remainingRight = Console.BufferWidth - leftIndent;


                // preparation - profile information
                bool onlyProfNameAndIDq = profDisplay == ProfileDisplayStyle.NameAndID;
                bool includeStyleInfoQ = profDisplay != ProfileDisplayStyle.NoStyleInfo;
                string profID, profName, profIconStyle, profIconStyleColors;
                string[] profDescLines = Array.Empty<string>();
                #region prep prof info
                /// initialize profile ID, name, and icon style
                profID = $"#{profileInfo.profileID}";
                profName = profileInfo.profileName; // .Clamp(remainingRight - profID.Length, "~~");  // I believe this is unnecessary
                profIconStyle = LogDecoder.FixContentName(profileInfo.profileIcon.ToString(), false);

                /// fetch full names for profile icon style colors
                profIconStyleColors = "";
                foreach (char piscIndexNo in profileInfo.profileStyleKey)
                {
                    if (int.TryParse(piscIndexNo.ToString(), out int piscIx))
                    {
                        ForECol forECol = (ForECol)piscIx;
                        profIconStyleColors += $"{forECol} ";
                    }
                }
                profIconStyleColors = profIconStyleColors.Trim().Replace(" ", ", ");

                /// break up description into necessary number of lines
                int _maxDescLines = (profIconSize == ProfileIconSize.Doubled ? 3 : 2);
                string _currentDescLine = "";
                List<string> _profDescWords = new(), _profDescLinesBuild = new();
                if (profileInfo.profileDescription.IsNotNEW())
                    _profDescWords.AddRange(profileInfo.profileDescription.Split(" "));
                for (int dx = 0; dx < _profDescWords.Count && _profDescLinesBuild.Count < _maxDescLines; dx++)
                {
                    int remainingRightAlt = remainingRight - 4; /// I almost forgot, need them double quotes (" ") .. also + (-2) for a bit of right indent
                    bool finalWordQ = dx == _profDescWords.Count - 1;
                    bool finalLineQ = _profDescLinesBuild.Count + 1 >= _maxDescLines;
                    string descWord = _profDescWords[dx] + (finalWordQ ? "" : (_useDbugDescSplitingQ ? cLS : " ")); // OG" "    DBG"░"

                    int descWordLen = descWord.Length;
                    int currLineLen = _currentDescLine.Length;
                    bool endCurrLineQ = false, backPedalQ = false;


                    /// LOGIC STATEMENTS
                    /// 
                    /// IF word fits within curr line: add word to curr line
                    ///     --- vv ---
                    ///     IF final word:
                    ///         IF final line: .. proceed [end]
                    ///         ELSE .. proceed [end]
                    ///     ELSE 
                    ///         IF final line: .. proceed (do nothing)
                    ///         ELSE proceed (do nothing)
                    ///     --- vv ---
                    ///     IF final word: .. proceed [end]
                    ///         
                    /// ELSE (word exceeds curr line)
                    ///     IF word smaller than line max: 
                    ///     --- vv ---
                    ///         IF final word: 
                    ///             IF final line: add word to curr line and clamp curr line [end]
                    ///             ELSE proceed to next line, backpedal -1;
                    ///         ELSE 
                    ///             IF final line: add word to curr line and clamp curr line [end]
                    ///             ELSE proceed to next line, backpedal -1;
                    ///     --- vv ---
                    ///         IF final line: add word to curr line and clamp curr line [end]
                    ///         ELSE proceed (to next line), backpedal -1;
                    ///         
                    ///     ELSE (word equal to or longer than line max)
                    ///         IF final line: add word to curr line and clamp curr line [end]
                    ///         ELSE 
                    ///             IF final word:
                    ///                 clamp word with '-', add clamped to curr line, add remainder to word list
                    ///             ELSE clamp word with '-', add clamped to curr line, insert remainder into word list
                    ///             .. proceed (to next line)
                    ///
                    if (currLineLen + descWordLen < remainingRightAlt)
                    {
                        _currentDescLine += descWord;

                        if (finalWordQ)
                            endCurrLineQ = true;
                    }
                    else
                    {
                        if (descWordLen < remainingRightAlt)
                        {
                            if (finalLineQ)
                            {
                                _currentDescLine += descWord;
                                _currentDescLine = _currentDescLine.Clamp(remainingRightAlt, clampSuffix);
                                endCurrLineQ = true;
                            }
                            else
                            {
                                backPedalQ = true;
                                endCurrLineQ = true;
                            }
                        }
                        else
                        {
                            if (finalLineQ)
                            {
                                _currentDescLine += descWord;
                                _currentDescLine = _currentDescLine.Clamp(remainingRightAlt, clampSuffix);
                                endCurrLineQ = true;
                            }
                            else
                            {
                                string longDescLine1 = descWord.Clamp(remainingRightAlt - currLineLen - 1, null);
                                string longDescLine2 = descWord.Replace(longDescLine1, "");
                                longDescLine1 += clampWordSuffix;

                                _currentDescLine += longDescLine1;
                                if (finalWordQ)
                                    _profDescWords.Add(longDescLine2);
                                else _profDescWords.Insert(dx + 1, longDescLine2);

                                endCurrLineQ = true;
                            }
                        }
                    }
                    
                    // back pedaling, return to word that could not be added to curr line
                    if (backPedalQ)
                    {
                        dx--;
                    }
                    // line end, submit, restart line
                    if (endCurrLineQ)
                    {
                        _currentDescLine = $"\"{_currentDescLine}\"";                        
                        _profDescLinesBuild.Add(_currentDescLine);
                        _currentDescLine = "";
                    }
                }
                if (_profDescLinesBuild.HasElements())
                    profDescLines = _profDescLinesBuild.ToArray();
                #endregion


                // print and edit
                const string profDisplayHR = "----------", profNoDesc = "n/a";
                PrintProfileIcon(profileInfo, profIconSize);
                GetCursorPosition();
                for (int ln = 1; ln <= lineCount; ln++)
                {
                    bool isDescQ = false, isStyleQ = false;
                    string lineText = "";                    


                    // SET INFORMATION BY DISPLAY SIZE
                    /** MINI arrangement
                    * 4x4  Name Only       All Details
                    * ---  ---             ---
                    * 1 |  -               {profName}  #{profID}
                    *   |  {profName}      {iconStyle} {iconStyleColorsCSV}
                    *   |  #{profID}       "{descLine1}"
                    * 4 |  -               "{descLine2-clipped}"
                    * Left Indent :: 2 + (4*2) -> 10 chars
                    */
                    if (profIconSize == ProfileIconSize.Mini)
                    {
                        switch (ln)
                        {
                            // M1 |  -               {profName}  #{profID}
                            case 1:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                else lineText = $"{profName}{Ind24}{profID}";
                                break;

                            // M2 |  {profName}      {iconStyle} {iconStyleColorsCSV}
                            case 2:
                                if (onlyProfNameAndIDq)
                                    lineText = profName;
                                else if (includeStyleInfoQ)
                                {
                                    lineText = $"{profIconStyle}{Ind24}{profIconStyleColors}";
                                    isStyleQ = true;
                                }
                                break;

                            // M3 |  #{profID}       "{descLine1}"
                            case 3:
                                if (onlyProfNameAndIDq)
                                    lineText = profID;
                                else
                                {
                                    if (profDescLines.HasElements())
                                        lineText = profDescLines[0];
                                    else lineText = profNoDesc;
                                    isDescQ = true;
                                }
                                break;

                            // M4 |  -               "{descLine2-clipped}"
                            case 4:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                else
                                {
                                    if (profDescLines.HasElements(2))
                                        lineText = profDescLines[1];
                                    isDescQ = true;
                                }
                                break;

                            default: break;
                        }
                    }
                    /** NORMAL arrangement
                     * 8x8  Name Only       All Details
                     * ---  ---             ---
                     * 1 |                  -
                     *   |  -               {profName} #{profID}
                     *   |                  -
                     * 4 |  {profName}      {iconStyle}
                     * 5 |  #{profID}       {iconStyleColorsCSV}
                     *   |                  "{descLine1}"
                     *   |  -               "{descLine2-clipped}"
                     * 8 |                  -
                     * Left Indent :: 2 + (8*2) -> 18 chars
                     */
                    if (profIconSize == ProfileIconSize.Normal)
                    {
                        switch (ln)
                        {
                            // N1 |                  -
                            case 1:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // N2 |  -               {profName} #{profID}
                            case 2:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                else lineText = $"{profName}{Ind24}{profID}";
                                break;

                            // N3 |                  -
                            case 3:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // N4 |  {profName}      {iconStyle}
                            case 4:
                                if (onlyProfNameAndIDq)
                                    lineText = profName;
                                else if (includeStyleInfoQ)
                                {
                                    lineText = profIconStyle;
                                    isStyleQ = true;
                                }
                                break;

                            // N5 |  #{profID}       {iconStyleColorsCSV}
                            case 5:
                                if (onlyProfNameAndIDq)
                                    lineText = profID;
                                else if (includeStyleInfoQ)
                                {
                                    lineText = profIconStyleColors;
                                    isStyleQ = true;
                                }
                                break;

                            // N6 |                  "{descLine1}"
                            case 6:
                                if (!onlyProfNameAndIDq)
                                {
                                    if (profDescLines.HasElements())
                                        lineText = profDescLines[0];
                                    else lineText = profNoDesc;
                                    isDescQ = true;
                                }
                                break;

                            // N7 |  -               "{descLine2-clipped}"
                            case 7:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                else
                                {
                                    if (profDescLines.HasElements(2))
                                        lineText = profDescLines[1];
                                    isDescQ = true;
                                }
                                break;

                            // N8 |                  -
                            case 8:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            default: break;
                        }
                    }
                    /** DOUBLED arrangement
                     * 16x16    Name Only       All Details
                     * ---      ---             ---
                     * 1 |                      
                     *   |                      -
                     *   |                      {profName}
                     * 4 |                      #{profID}
                     * 5 |      -               
                     *   |                      -
                     *   |                      {iconStyle}
                     * 8 |      {profName}      {iconStyleColorsCSV}
                     * 9 |      #{profID}       
                     *   |                      -
                     *   |                      "{descLine1}"
                     * 12|      -               "{descLine2}"
                     * 13|                      "{descLine3-clipped}"
                     *   |                      
                     *   |                      -
                     * 16|
                     * Left Indent :: 2 + (16*2) -> 34 chars                      
                     */
                    if (profIconSize == ProfileIconSize.Doubled)
                    {
                        switch (ln)
                        {
                            // D1 |                      
                            // D2 |                      -
                            case 2:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // D3 |                      {profName}
                            case 3:
                                if (!onlyProfNameAndIDq)
                                    lineText = profName;
                                break;

                            // D4 |                      #{profID}
                            case 4:
                                if (!onlyProfNameAndIDq)
                                    lineText = profID;
                                break;

                            // D5 |      -
                            case 5:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // D6 |                      -
                            case 6:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // D7 |                      {iconStyle}
                            case 7:
                                if (!onlyProfNameAndIDq && includeStyleInfoQ)
                                {
                                    lineText = profIconStyle;
                                    isStyleQ = true;
                                }    
                                break;

                            // D8 |      {profName}      {iconStyleColorsCSV}
                            case 8:
                                if (onlyProfNameAndIDq)
                                    lineText = profName;
                                else if (includeStyleInfoQ)
                                {
                                    lineText = profIconStyleColors;
                                    isStyleQ = true;
                                }
                                break;

                            // D9 |      #{profID}       
                            case 9:
                                if (onlyProfNameAndIDq)
                                    lineText = profID;
                                break;

                            // D10|                      -
                            case 10:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // D11|                      "{descLine1}"
                            case 11:
                                if (!onlyProfNameAndIDq)
                                {
                                    if (profDescLines.HasElements())
                                        lineText = profDescLines[0];
                                    else lineText = profNoDesc;
                                    isDescQ = true;
                                }
                                break;

                            // D12|      -               "{descLine2}"
                            case 12:
                                if (onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                else
                                {
                                    if (profDescLines.HasElements(2))
                                        lineText = profDescLines[1];
                                    isDescQ = true;
                                }
                                break;

                            // D13|                      "{descLine3-clipped}"
                            case 13:
                                if (!onlyProfNameAndIDq)
                                {
                                    if (profDescLines.HasElements(3))
                                        lineText = profDescLines[2];
                                    isDescQ = true;
                                }
                                break;

                            // D14|                      
                            // D15|                      -
                            case 15:
                                if (!onlyProfNameAndIDq)
                                    lineText = profDisplayHR;
                                break;

                            // D16|

                            default: break;
                        }
                    }


                    // FORMAT AND PRINT THE SET INFO
                    if (lineText.IsNotNEW())
                    {
                        SetCursorPosition(ln + cursorTop, leftIndent);
                        ForECol colorCode = ForECol.Accent;

                        // styling prof name and id and horizontal rule
                        if (!isDescQ && !isStyleQ)
                        {
                            /// bypass horiz-rule  /// style name and id
                            if (lineText != profDisplayHR)
                                colorCode = ForECol.Heading2;
                        }
                        // styling desc and styles
                        else
                        {
                            // styling styles
                            if (isStyleQ)
                                colorCode = ForECol.Highlight;

                            // styling desc
                            if (isDescQ)
                            {
                                /// bypass no desc /// style desc
                                if (lineText != profNoDesc)
                                    colorCode = ForECol.InputColor;
                            }
                        }

                        /// true print here
                        FormatLine(lineText, colorCode);
                    }
                }
                SetCursorPosition();
            }
        }
        public static void PrintProfileIcon(ProfileInfo profileInfo, ProfileIconSize profIconSize = ProfileIconSize.Normal)
        {            
            if (profileInfo.IsSetupQ())
            {
                // +++ PREPARATIONS +++
                const int normalIconStartIx = 4;
                bool useBasicColorsQ = false;
                string[] iconStyleSheet = null;
                HPInk[] inkList;

                #region preparations continued
                /// profile icon style designs
                /// only two sizes exist per design (mini, normal). The doubled design will depend on 'normal' size
                List<string[]> allIconStyleSheets = new()
                {
                    /// these were designed in aseprite before being added. Glad i did it that way.. not the other way ...  *flashbacks*
                    
                    // #0 Standard User Icon
                    new string[]
                    {
                        // 4x4 standard
                        " 00 ",
                        "0020",
                        "0220",
                        "1001",

                        // 8x8 standard
                    /// |   ..   |
                        "   00   ",
                        "  0020  ",
                        "  0220  ",
                        "   00   ", /// ....-
                        "        ", /// .....
                        "  0000  ",
                        " 004400 ",
                        " 044440 ",
                    /// |   ..   |
                    },

                    // #1 Sword Icon
                    new string[]
                    {
                        // 4x4 standard
                        "3300",
                        " 040",
                        "240 ",
                        "02  ",

                        // 8x8 standard
                    /// |   ..   |
                        " 3333 00",
                        "3  3 040",
                        "    040 ",
                        " 0 040  ", /// ....-
                        " 0040   ", /// .....
                        " 220    ",
                        "00200   ",
                        "00      ",
                    /// |   ..   |
                    },

                    // #2 Pickaxe Icon
                    new string[]
                    {
                        // 4x4 standard
                        "00  ",
                        " 42 ",
                        "1440",
                        "01 0",

                        // 8x8 standard
                    /// |   ..   |
                        "0000    ",
                        "  00022 ",
                        "   0222 ",
                        "   4420 ", /// ....-
                        "  424000", /// .....
                        " 044  00",
                        "040    0",
                        "00     0",
                    /// |   ..   |
                    },

                    // #3 Mountain Icon
                    new string[]
                    {
                        // 4x4 standard
                        "55  ",
                        " 205",
                        "2200",
                        "2000",

                        // 8x8 standard
                    /// |   ..   |
                        "  4     ",
                        "4444   4",
                        "    0 44",
                        "   220  ", /// ....-
                        "  22200 ", /// .....
                        " 2220000",
                        "22220000",
                        "22200000",
                    /// |   ..   |
                    },

                    // #4 Music Note Icon
                    new string[]
                    {
                        // 4x4 standard
                        "  00",
                        "550 ",
                        " 003",
                        "300",

                        // 8x8 standard
                    /// |   ..   |
                        "44      ",
                        "44000 33",
                        " 40 30  ",
                        " 430    ", /// ....-
                        "3   0 4 ", /// .....
                        "  000343",
                        " 300044 ",
                        "3    44 ",
                    /// |   ..   |
                    },

                    // #5 Document Icon
                    new string[]
                    {
                        // 4x4 standard
                        "100 ",
                        "0200",
                        "0  0",
                        "0440",

                        // 8x8 standard
                    /// |   ..   |
                        " 000000 ",
                        "0    000",
                        "0 22 000",
                        "0      0", /// ....-
                        "0 44 4 0", /// .....
                        "0      0",
                        "0 444  0",
                        "0      0",
                    /// |   ..   |
                    },

                    // #6 Abstract Icon
                    new string[]
                    {
                        // 4x4 standard
                        " 1 2",
                        " 02 ",
                        "5001",
                        "4445",

                        // 8x8 standard
                    /// |   ..   |
                        " 4  0   ",
                        "  0   22",
                        " 02 22  ",
                        " 0222  4", /// ....-
                        "4002 0  ", /// .....
                        "40000 44",
                        "444444  ",
                        " 444",
                    /// |   ..   |
                    }

                    /// INKING IDS
                    /// ' ' empty (no color)
                    /// 0 - 1st color, heavy    1 - 1st color, light
                    /// 2 - 2nd color, heavy    3 - 2nd color, light
                    /// 4 - 3rd color, heavy    5 - 3rd color, light
                };

                // select icon style
                int styleIndex = profileInfo.profileIcon.GetHashCode();
                if (styleIndex.IsWithin(0, allIconStyleSheets.Count - 1))
                    iconStyleSheet = allIconStyleSheets[styleIndex];

                // getting ink colors from profile
                int fec1 = 0, fec2 = 0, fec3 = 0;
                for (int fix = 0; fix < profileInfo.profileStyleKey.Length; fix++)
                {
                    char fColorIx = profileInfo.profileStyleKey[fix];
                    if (int.TryParse(fColorIx.ToString(), out int fColIx))
                    {
                        switch (fix)
                        {
                            case 0: fec1 = fColIx; break;
                            case 1: fec2 = fColIx; break;
                            case 2: fec3 = fColIx; break;
                            default: break;
                        }
                    }
                }

                // setting (ink) colors to forECol
                ForECol color1 = (ForECol)fec1;
                ForECol color2 = (ForECol)fec2;
                ForECol color3 = (ForECol)fec3;

                // loading ink for use
                /// INKING IDS
                /// ' ' empty (no color)
                /// 0 - 1st color, heavy    1 - 1st color, light    
                /// 2 - 2nd color, heavy    3 - 2nd color, light
                /// 4 - 3rd color, heavy    5 - 3rd color, light
                inkList = new HPInk[6]
                {
                    new HPInk(0, GetPrefsForeColor(color1), Dither.Dark),
                    new HPInk(0, GetPrefsForeColor(color1), Dither.Light),
                    new HPInk(0, GetPrefsForeColor(color2), Dither.Dark),
                    new HPInk(0, GetPrefsForeColor(color2), Dither.Light),
                    new HPInk(0, GetPrefsForeColor(color3), Dither.Dark),
                    new HPInk(0, GetPrefsForeColor(color3), Dither.Light),
                };
                if (useBasicColorsQ)
                {
                    inkList = new HPInk[6]
                    {
                        new HPInk(0, Color.Yellow, Dither.Dark),
                        new HPInk(0, Color.Yellow, Dither.Light),
                        new HPInk(0, Color.Red, Dither.Dark),
                        new HPInk(0, Color.Red, Dither.Light),
                        new HPInk(0, Color.Blue, Dither.Dark),
                        new HPInk(0, Color.Blue, Dither.Light),
                    };
                }
                #endregion


                // +++ PRINTING HERE +++
                if (inkList.HasElements() && iconStyleSheet.HasElements())
                {
                    /// effectively splits the prints based on icon size
                    bool doublePrintedRowQ = false;
                    int startIx = 0, endIx = iconStyleSheet.Length;
                    if (profIconSize == ProfileIconSize.Mini)
                        endIx = normalIconStartIx;
                    else startIx = normalIconStartIx;

                    /// for (row) [for (row-column)]
                    for (int issx = startIx; issx < endIx && issx < iconStyleSheet.Length; issx++)
                    {
                        if (issx == startIx && !doublePrintedRowQ)
                            NewLine(); /// first print newline

                        string iconSheetRow = iconStyleSheet[issx];
                        Text(Ind14); /// indent at least 2

                        if (iconSheetRow.IsNotNE())
                        {
                            /// for (row-column)
                            for (int isrcx = 0; isrcx < iconSheetRow.Length; isrcx++)
                            {
                                char iconRowColumn = iconSheetRow[isrcx];
                                /// determine prints here    
                                /// IF ...: (color); ELSE (empty);
                                if (int.TryParse(iconRowColumn.ToString(), out int ircInkID))
                                {
                                    HPInk inkToUse = inkList[ircInkID];
                                    DrawPixel(Color.Black, inkToUse.inkPattern.Value, inkToUse.inkCol);

                                    /// doubles the printing per row column
                                    if (profIconSize == ProfileIconSize.Doubled)
                                        DrawPixel(Color.Black, inkToUse.inkPattern.Value, inkToUse.inkCol);
                                }
                                else
                                {
                                    Text("  "); // space character
                                    /// doubles the printing per row column (spaces only)
                                    if (profIconSize == ProfileIconSize.Doubled)
                                        Text("  ");
                                }
                            }
                        }

                        /// doubles the printing row
                        if (profIconSize == ProfileIconSize.Doubled && !doublePrintedRowQ)
                        {
                            doublePrintedRowQ = true;
                            issx--;
                        }
                        else doublePrintedRowQ = false;

                        NewLine(); /// next priting row
                    }

                }
            }
        }
    }
}
