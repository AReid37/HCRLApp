using System;
using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;

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
        const string logStateParent = "Profiles Page";


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

                Program.LogState(logStateParent);
                Clear();
                Title("Profiles Page", cTHB, 1);
                FormatLine($"{Ind24}This page allow for all things pertaining to profiles: creating, editing, switching, deleting.", ForECol.Accent);
                NewLine(HSNL(1,2));
                Program.DisplayCurrentProfile();



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

            // auto-saving alas   
            if (ProfileHandler.ChangesMade() || ProfileHandler.ProfileSwitchQueuedQ)
                Program.SaveData(true);
        }


        static void SubPage_ProfileSelect()
        {
            bool exitProfSelectorQ = false;
            ProfileInfo currProfile = new();
            ProfileInfo[] otherProfiles = Array.Empty<ProfileInfo>();
            ProfileIconSize currProfDispSize = ProfileIconSize.Mini, otherProfDispSize = (ProfileIconSize)HSNL(0, 4).Clamp(1, 2);
            ProfileDisplayStyle currProfDispStyle = ProfileDisplayStyle.NameAndID, otherProfDispStyle = ProfileDisplayStyle.NoStyleInfo;
            int otherProfIndex = 0, otherProfMaxIndex = 0;
            const string nextProf = ">", prevProf = "<";

            bool anyProfilesDetectedQ = ProfileHandler.AllProfiles.HasElements(); 
            bool availableProfilesToSwitchToQ = false, currentProfileExistsQ = false, allowProfileBrowsingQ = false, unsavedChangesOnCurrProfQ = false;
            if (anyProfilesDetectedQ)
            {
                unsavedChangesOnCurrProfQ = ProfileHandler.ChangesMade();
                currProfile = ProfileHandler.GetCurrentProfile(out _);
                currentProfileExistsQ = currProfile.IsSetupQ();
                availableProfilesToSwitchToQ = ProfileHandler.AllProfiles.HasElements(2) || !currentProfileExistsQ;

                if (availableProfilesToSwitchToQ)
                {
                    List<ProfileInfo> switchableProfiles = new();
                    foreach (ProfileInfo otherProfile in ProfileHandler.AllProfiles)
                    {
                        if (otherProfile.profileID != currProfile.profileID)
                            switchableProfiles.Add(otherProfile);
                    }

                    if (switchableProfiles.HasElements())
                    {
                        otherProfiles = switchableProfiles.ToArray();
                        allowProfileBrowsingQ = otherProfiles.HasElements(2);
                        otherProfMaxIndex = otherProfiles.Length - 1;
                    }
                }
            }

            do
            {
                BugIdeaPage.OpenPage();
                
                Program.LogState(logStateParent + "|Select");
                Clear();
                Title("Profile Selection", subMenuUnderline, 1);
                FormatLine($"{Ind14}Select a profile. {(!currentProfileExistsQ ? "" : "Switching profiles will require a restart.")}");
                NewLine(HSNL(1, 2));

                // display current profile (if exists)
                if (currentProfileExistsQ)
                {
                    FormatLine("Current Profile".ToUpper());
                    DisplayProfileInfo(currProfile, currProfDispSize, currProfDispStyle);
                }
                else FormatLine($"{Ind14}No Profile Currently In Use", ForECol.Accent);
                HorizontalRule('-', 2);


                // SWITCH PROFILES HERE
                /// if other profiles available AND no unsaved changes on current
                if (availableProfilesToSwitchToQ && !unsavedChangesOnCurrProfQ)
                {
                    // display 1 other profile that can be switched to
                    /// fetch
                    ProfileInfo otherProf = new();
                    if (otherProfIndex.IsWithin(0, otherProfiles.Length - 1))
                        otherProf = otherProfiles[otherProfIndex];
                    /// display
                    FormatLine($"{Ind14}Profile No.{otherProfIndex + 1}", ForECol.Accent);
                    if (otherProf.IsSetupQ())
                        DisplayProfileInfo(otherProf, otherProfDispSize, otherProfDispStyle);
                    else FormatLine($"{Ind24}Profile could not be displayed.", ForECol.Incorrection);
                    NewLine(HSNL(1,2));


                    // the rest of this couldn't run if the above somehow failed
                    if (otherProf.IsSetupQ())
                    {
                        // prompts
                        /// if 2 or more other profiles, enable browsing profiles
                        if (allowProfileBrowsingQ)
                            FormatLine($"{Ind14}Use [{prevProf}] to view previous profile. Use [{nextProf}] to view next profile.");
                        /// [<] for prev profile, [>] for next profile, [any key] to select profile, [enter] to exit page
                        FormatLine($"{Ind14}Enter any key to select this profile. Press [Enter] to exit page.");
                        Format($"{Ind24}{(allowProfileBrowsingQ ? "Browse or select a" : "Select this")} profile >> ");
                        string input = StyledInput(allowProfileBrowsingQ ? $"{prevProf} t {nextProf}" : null);


                        // input parsing
                        bool selectedProfToSwitchToQ = false;
                        if (input.IsNotNEW())
                        {
                            /// it just wraps between all available profiles
                            if (allowProfileBrowsingQ && (input.Equals(prevProf) || input.Equals(nextProf)))
                            {
                                if (input.Equals(prevProf))
                                    otherProfIndex = otherProfIndex > 0 ? otherProfIndex - 1 : otherProfMaxIndex;
                                else otherProfIndex = otherProfIndex < otherProfMaxIndex ? otherProfIndex + 1 : 0;
                            }
                            else selectedProfToSwitchToQ = true;
                        }
                        else exitProfSelectorQ = true;


                        // confirmation before switch to prof (restart is always required)
                        if (selectedProfToSwitchToQ)
                        {
                            NewLine();
                            Highlight(true, $"{Ind24}The profile '{otherProf.profileName}' has been selected for switching.", $"'{otherProf.profileName}'");
                            Confirmation($"{Ind24}Confirm this switch to selected profile? ", false, out bool yesNo);

                            ConfirmationResult(yesNo, $"{Ind34}", "Will switch to selected profile.", "Profile switching cancelled.");
                            if (yesNo)
                            {
                                /// only save profile ID, actual switch after another save.
                                ProfileHandler.QueueSwitchProfile(otherProf.profileID);
                                Format($"{Ind34}A restart is required.", ForECol.Warning);
                                Pause();

                                Program.RequireRestart();
                                exitProfSelectorQ = true;
                            }
                        }
                    }

                }

                // no other profiles to switch to
                else
                {
                    if (unsavedChangesOnCurrProfQ)
                    {
                        FormatLine($"{Ind14}There are unsaved profile edits made to the current profile.", ForECol.Warning);
                        Format($"{Ind14}To proceeed, save these changes by exiting to the Main Menu.");

                        if (availableProfilesToSwitchToQ)
                        {
                            Pause();
                            exitProfSelectorQ = true;
                        }
                        else NewLine(2);
                    }
                    
                    if (!availableProfilesToSwitchToQ)
                    {
                        Format($"{Ind14}There are no other profiles available to select and switch.", ForECol.Highlight);
                        Pause();
                        exitProfSelectorQ = true;
                    }
                }
            }
            while (!exitProfSelectorQ && !Program.AllowProgramRestart);
        }
        static void SubPage_ProfileEditor()
        {
            const int profNameMin = ProfileHandler.ProfileNameMinimum, profNameMax = ProfileHandler.ProfileNameLimit, profDescMax = ProfileHandler.ProfileDescriptionLimit;
            const string invalidChar = DataHandlerBase.Sep;
            bool exitProfEditorQ = false, anyProfilesDetected, skipExternalProfNoticeQ = false, unsavedChangesDetectedQ = false;
            bool anyProfilesCreatedOrDeletedQ = false; // program restart initiator
            string activeMenuKey = null;
            string prevDescTooLong = null;
            int cursorTopPrev = 0;
            ProfileInfo currProfile = new(), prevCurrProfile = new();
            
            anyProfilesDetected = ProfileHandler.AllProfiles.HasElements();
            if (anyProfilesDetected)
            {
                unsavedChangesDetectedQ = ProfileHandler.ChangesMade();
                currProfile = ProfileHandler.GetCurrentProfile(out _);
                prevCurrProfile = currProfile;
            }

            /// for prof editing
            ProfileDisplayStyle previewStyle = ProfileDisplayStyle.NameAndID;
            ProfileIconSize previewSize = ProfileIconSize.Mini;


            do
            {
                BugIdeaPage.OpenPage();

                const string logStateMethod = logStateParent + "|Editor";
                if (activeMenuKey.IsNE())
                    Program.LogState(logStateMethod);
                Clear();

                string[] profEditMenuOpts = { "Create Profile", "Edit Profile", "Delete Profile", $"{exitSubPagePhrase} [Enter]" };
                bool validKey = false, quitMenuQ = false;
                string profEditKey = null;
                if (activeMenuKey.IsNE())
                {
                    Program.DisplayCurrentProfile();
                    validKey = ListFormMenu(out profEditKey, "Profile Editor Menu", subMenuUnderline, null, "a~c", true, profEditMenuOpts);
                    quitMenuQ = LastInput.IsNE() || profEditKey == "d";
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


                    string titleText = "Profile " + (profEditKey.Equals("a") ? "Creation" : (profEditKey.Equals("b") ? "Editor" : "Deletion"));
                    Clear();
                    Title(titleText, subMenuUnderline, 1);


                    // create profile -- simply, register new profile.
                    if (profEditKey.Equals("a"))
                    {
                        Program.LogState(logStateMethod + "|Create");
                        FormatLine("A new profile may be created or an existing profile may be duplicated and saved as a new profile.");
                        FormatLine("Creating a profile will require a program restart.", ForECol.Warning);
                        FormatLine($"Remaining Profile Spaces :: '{ProfileHandler.RemainingProfileSpacesLeft}' available.", ForECol.Highlight);
                        NewLine();


                        // profile space available / no unsaved changes detected
                        if (ProfileHandler.RemainingProfileSpacesLeft > 0 && !unsavedChangesDetectedQ)
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
                                FormatLine($"{Ind14}Please provide a profile name. Minimum of {profNameMin} characters, maximum of {profNameMax} characters. May not include '{invalidChar}' character.");
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
                                        if (!profileName.Contains(invalidChar))
                                        {
                                            if (profileName.Length.IsWithin(profNameMin, profNameMax))
                                                validProfileNameQ = true;
                                            else IncorrectionMessageQueue($"Profile name must be between {profNameMin} and {profNameMax} characters in length");
                                        }
                                        else IncorrectionMessageQueue($"Profile name may not contain '{invalidChar}' character");
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

                        // no profile space available / unsaved changes detected
                        else
                        {
                            if (!unsavedChangesDetectedQ)
                            {
                                int profMaxNum = ProfileHandler.AllProfiles.Count;
                                FormatLine($"{Ind24}There are no more profile spaces available.", ForECol.Warning);
                                Format($"{Ind24}The maximum number of profiles ({profMaxNum}) has been reached. To create a new profile, an existing profile must be deleted.");
                            }
                            else
                            {
                                FormatLine($"{Ind14}There are unsaved profile edits made to the current profile.", ForECol.Warning);
                                Format($"{Ind14}To proceeed, save these changes by exiting to the Main Menu.");
                            }
                            Pause();

                            endActiveMenuKeyQ = true;
                        }
                    }


                    // edit profile -- all editing, new and old
                    if (profEditKey.Equals("b"))
                    {
                        Program.LogState(logStateMethod + "|Edit");
                        FormatLine($"{Ind14}An active profile may be customized from name to icon color schema on this page.");

                        if (anyProfilesDetected)
                        {
                            FormatLine($"{Ind14}Select one of the options below to begin customizing this profile.");
                            FormatLine($"{Ind14}Note :: The profile ID (5-digit number) is unchangeable.");
                            NewLine();

                            // profile display here
                            Title("Current Profile");
                            DisplayProfileInfo(currProfile, previewSize, previewStyle);
                            NewLine(2);

                            /// menu and stuff
                            /// - change profile display style (tiny, normal) ['double' excluded]
                            /// - profile name -- profile icon type -- profile icon colors -- profile description
                            /// - confirm profile changes
                            string[] profEditsOpts = new string[] { "Preview As", "Profile Name", "Profile Icon", "Profile Colors", "Profile Description", "Confirm Changes" };
                            bool validOption = TableFormMenu(out short editOptNum, "Profile Editing Options", subMenuUnderline, false, $"{Ind24}Select an option >> ", "1~6", 3, profEditsOpts);

                            if (validOption)
                            {
                                /// little header for each option
                                NewLine(4);
                                Important(profEditsOpts[editOptNum - 1], subMenuUnderline);


                                // preview style
                                if (editOptNum == 1)
                                {
                                    string currPreviewStyle = previewStyle == ProfileDisplayStyle.Full ? "All Info" : "Name and ID";
                                    currPreviewStyle += ", " + (previewSize == ProfileIconSize.Mini ? "Mini" : "Normal");

                                    FormatLine("Choose how to preview the profile currently being edited.");
                                    Highlight(true, $"Current preview style: {currPreviewStyle}.", currPreviewStyle);
                                    NewLine();
                                    FormatLine($"{Ind14}NOTE :: These will also affect the display of icon style selection. The most common display setting used throughout the app is 'Name and ID, Mini'.", ForECol.Accent);
                                    NewLine();

                                    bool validPreviewOptQ = ListFormMenu(out string prvwKey, "Preview Style", subMenuUnderline, $"{Ind14}Select >> ", null, true, "All Info, Normal", "All Info, Mini", "Name and ID, Normal", "Name and ID, Mini");

                                    if (validPreviewOptQ)
                                    {
                                        switch (prvwKey)
                                        {
                                            case "a": // all/norm
                                                previewStyle = ProfileDisplayStyle.Full;
                                                previewSize = ProfileIconSize.Normal;
                                                break;

                                            case "b": // all/mini
                                                previewStyle = ProfileDisplayStyle.Full;
                                                previewSize = ProfileIconSize.Mini;
                                                break;

                                            case "c": // lil/norm
                                                previewStyle = ProfileDisplayStyle.NameAndID;
                                                previewSize = ProfileIconSize.Normal;
                                                break;

                                            case "d": // lil/mini
                                                previewStyle = ProfileDisplayStyle.NameAndID;
                                                previewSize = ProfileIconSize.Mini;
                                                break;
                                        }
                                    }
                                }

                                // profile name
                                if (editOptNum == 2)
                                {
                                    /// profile name -- character range [3 ~ 30], may not include: [%]
                                    FormatLine("Provide a new name for current profile.");
                                    FormatLine($"Minimum of {profNameMin} characters, maximum of {profNameMax} characters. May not include '{invalidChar}' character.");
                                    NewLine();

                                    Highlight(true, $"{Ind14}Current Profile Name >> {currProfile.profileName}.", currProfile.profileName);
                                    Format($"{Ind14}New Profile Name >> ");

                                    bool validProfileNameQ = false;
                                    string newProfName = StyledInput("|__ .    :    .    :    .    |");

                                    // parse name
                                    if (newProfName.IsNotNEW())
                                    {
                                        if (!newProfName.Contains(invalidChar))
                                        {
                                            if (newProfName.Length.IsWithin(profNameMin, profNameMax))
                                                validProfileNameQ = true;
                                            else
                                            {
                                                if (newProfName.Length > profNameMax)
                                                    IncorrectionMessageQueue($"Name is too long (Maximum of '{profNameMax}' characters)");
                                                else IncorrectionMessageQueue($"Name is too short (Minimum of '{profNameMin}' characters)");
                                            }
                                        }
                                        else IncorrectionMessageQueue($"Name may not contain '{invalidChar}' character");
                                    }
                                    else if (newProfName.IsEW())
                                        IncorrectionMessageQueue("A name for the profile is required");

                                    if (!validProfileNameQ)
                                        IncorrectionMessageTrigger($"{Ind24}[X] ", ".");


                                    // confirm name
                                    if (validProfileNameQ)
                                    {
                                        Confirmation($"{Ind14}Confirm new profile name? ", false, out bool yesNo);
                                        if (yesNo)
                                            currProfile.profileName = newProfName;
                                        ConfirmationResult(yesNo, $"{Ind24}", $"Profile name changed to '{newProfName}'.", "Profile name will not be changed.");
                                    }
                                }

                                // profile icon
                                if (editOptNum == 3)
                                {
                                    string currProfIcon = LogDecoder.FixContentName(currProfile.profileIcon.ToString(), false);
                                    FormatLine("Choose an icon for your profile. The available icons are previewed below with the current profiles color scheme and the preview size.");
                                    Highlight(true, $"Current profile icon and display size: {currProfIcon}, {previewSize}.", currProfIcon, previewSize.ToString());
                                    NewLine();

                                    // save table menu space and confirmation
                                    GetCursorPosition();
                                    NewLine(8);

                                    // showcase icons here
                                    ProfileIcon[] profIcons = Enum.GetValues<ProfileIcon>();
                                    List<ProfileIcon> profIconOpts = new();
                                    List<string> profIconNames = new();
                                    if (profIcons.HasElements())
                                    {
                                        foreach (ProfileIcon profIcon in profIcons)
                                        {
                                            if (profIcon != currProfile.profileIcon)
                                            {
                                                string profIconName = LogDecoder.FixContentName(profIcon.ToString(), false);
                                                profIconNames.Add(profIconName.Replace("Icon", "").Trim());
                                                profIconOpts.Add(profIcon);
                                                ProfileInfo profIconShowcase = new("00000", profIconName, profIcon, currProfile.profileStyleKey, null);

                                                // display
                                                NewLine();
                                                FormatLine(profIconShowcase.profileName);
                                                PrintProfileIcon(profIconShowcase, previewSize);
                                            }
                                        }
                                    }

                                    // table menu
                                    SetCursorPosition();
                                    bool validIconQ = TableFormMenu(out short iconNum, "Profile Icons", subMenuUnderline, false, $"{Ind14}Select profile icon >> ", $"1~{profIconNames.Count}", 3, profIconNames.ToArray());


                                    // confirmation
                                    if (validIconQ)
                                    {
                                        ProfileIcon selectedIcon = profIconOpts[iconNum - 1];
                                        NewLine();
                                        Confirmation($"{Ind14}Confirm '{profIconNames[iconNum - 1]}' as new profile icon? ", false, out bool yesNo);

                                        if (yesNo)
                                            currProfile.profileIcon = selectedIcon;
                                        ConfirmationResult(yesNo, $"{Ind14}Profile icon ", "has been changed.", "remains unchanged.");
                                    }
                                }

                                // profile color scheme
                                if (editOptNum == 4)
                                {
                                    // setup
                                    ForECol[] profColors = Enum.GetValues<ForECol>();
                                    string[] menuPromptsNOpts = new string[4]
                                    {
                                        $"{Ind14}First color option (yellow) >> ",
                                        $"{Ind14}Second color option (red) >> ",
                                        $"{Ind14}Third color option (blue) >> ",
                                        "0~9"
                                    };
                                    string[] menuValidOpts = "1,2,3,4,5,6,7,8,9".Split(',');


                                    // prompts and intro
                                    FormatLine("Choose the colors and order of the profile's icon based on preference colors.");
                                    Highlight(true, $"Current profile icon colors and order: {GetIconStyleColors(currProfile.profileStyleKey)}.", GetIconStyleColors(currProfile.profileStyleKey, 1), GetIconStyleColors(currProfile.profileStyleKey, 2), GetIconStyleColors(currProfile.profileStyleKey, 3));
                                    NewLine();

                                    FormatLine($"NOTE :: Yellow squares are first in color order and blue squares are last.", ForECol.Warning);
                                    PrintProfileIcon(currProfile, previewSize, true);
                                    NewLine();


                                    // color menu reference print
                                    Title("Color Options", subMenuUnderline, 1);
                                    FormatLine("Use the table of colors below to choose the three icon colors.", ForECol.Accent);
                                    SettingsPage.ShowPrefsColors(_preferencesRef);
                                    NewLine();

                                    // -- CONFIRMATION CHAIN --
                                    string iconStyleKeyBuild = "";
                                    short fColNum;
                                    bool validColorOpt1, validColorOpt2 = false, validColorOpt3 = false;

                                    // confirm 1
                                    Format(menuPromptsNOpts[0]);
                                    validColorOpt1 = MenuOptions(StyledInput(menuPromptsNOpts[3]), out fColNum, menuValidOpts);
                                    if (validColorOpt1)
                                    {
                                        ForECol firstCol = (ForECol)fColNum;
                                        iconStyleKeyBuild += fColNum.ToString();
                                        Highlight(true, $"{Ind14}First order color selected: '{firstCol}'.", firstCol.ToString());
                                        NewLine();
                                    }

                                    // confirm 2
                                    if (validColorOpt1)
                                    {
                                        Format(menuPromptsNOpts[1]);
                                        validColorOpt2 = MenuOptions(StyledInput(menuPromptsNOpts[3]), out fColNum, menuValidOpts);
                                        if (validColorOpt2)
                                        {
                                            ForECol secondCol = (ForECol)fColNum;
                                            iconStyleKeyBuild += fColNum.ToString();
                                            Highlight(true, $"{Ind14}Second order color selected: '{secondCol}'.", secondCol.ToString());
                                            NewLine();
                                        }
                                    }

                                    // confirm 3
                                    if (validColorOpt2)
                                    {
                                        Format(menuPromptsNOpts[2]);
                                        validColorOpt3 = MenuOptions(StyledInput(menuPromptsNOpts[3]), out fColNum, menuValidOpts);
                                        if (validColorOpt3)
                                        {
                                            ForECol thirdCol = (ForECol)fColNum;
                                            iconStyleKeyBuild += fColNum.ToString();
                                            Highlight(true, $"{Ind14}Third order color selected: '{thirdCol}'.", thirdCol.ToString());
                                            NewLine();
                                        }
                                    }

                                    // final confirmation
                                    bool colsConfirmedQ = false;
                                    if (validColorOpt1 && validColorOpt2 && validColorOpt3)
                                    {
                                        NewLine();
                                        Title("Confirm Final Colors");
                                        Highlight(true, $"{Ind14}The new icon style colors in order: {GetIconStyleColors(iconStyleKeyBuild, 0)}", GetIconStyleColors(iconStyleKeyBuild, 1), GetIconStyleColors(iconStyleKeyBuild, 2), GetIconStyleColors(iconStyleKeyBuild, 3));
                                        Confirmation($"{Ind14}Confirm the selection and order of icon colors? ", false, out colsConfirmedQ);
                                    }

                                    // commit changes / confirmation-cancellation message
                                    if (colsConfirmedQ)
                                    {
                                        currProfile.profileStyleKey = iconStyleKeyBuild;
                                    }
                                    ConfirmationResult(colsConfirmedQ, $"{Ind24}", "Profile style colors have been changed.", "Profile style colors cancelled.");
                                }

                                // profile description
                                if (editOptNum == 5)
                                {
                                    /// profile description -- character range [0 ~ 120], may not include: [%]
                                    FormatLine("Provide a short description for the current profile.");
                                    FormatLine($"Maximum of {profDescMax} characters. May not include '{invalidChar}' character.");
                                    NewLine();

                                    Format($"Current Profile Descripiton >> ");
                                    if (currProfile.profileDescription.IsNotNEW())
                                    {
                                        Format($"\"");
                                        Format($"{currProfile.profileDescription}", ForECol.Highlight);
                                        Format("\"");
                                    }
                                    else Format($"n/a", ForECol.Accent);
                                    NewLine();

                                    // prompt
                                    int cursorTopInput = 0;
                                    Format($"New Profile Description >> ");
                                    if (prevDescTooLong.IsNotNEW())
                                    {
                                        GetCursorPosition();
                                        Format(prevDescTooLong.Clamp(profDescMax), ForECol.Accent);
                                        SetCursorPosition();
                                    }

                                    // input
                                    bool validProfDescQ = false;
                                    string newProfDesc = StyledInput(null);

                                    // fix placements
                                    cursorTopInput = Console.CursorTop;
                                    int cursorTopFinal = cursorTopInput >= cursorTopPrev ? cursorTopInput : cursorTopPrev;
                                    SetCursorPosition(cursorTopFinal, 0);
                                    if (prevDescTooLong.IsNotNEW())
                                    {
                                        prevDescTooLong = null;
                                        cursorTopPrev = 0;
                                    }

                                    // parse description
                                    if (newProfDesc.IsNotNEW())
                                    {
                                        if (!newProfDesc.Contains(invalidChar))
                                        {
                                            if (newProfDesc.Length.IsWithin(1, profDescMax))
                                                validProfDescQ = true;
                                            else IncorrectionMessageQueue($"Description is too long (Maximum of '{profDescMax}' characters)");
                                        }
                                        else IncorrectionMessageQueue($"Description may not include '{invalidChar}' character");
                                    }
                                    else
                                    {
                                        if (newProfDesc.IsNE())
                                        {
                                            newProfDesc = null;
                                            validProfDescQ = true;
                                        }
                                        else IncorrectionMessageQueue("A profile description is required");
                                    }

                                    if (!validProfDescQ)
                                    {
                                        prevDescTooLong = newProfDesc;
                                        cursorTopPrev = Console.CursorTop - 1;
                                        IncorrectionMessageTrigger($"{Ind14}[X] ", ".");
                                    }


                                    // confirm description
                                    if (validProfDescQ)
                                    {
                                        bool profDescChangedQ = false;
                                        if (newProfDesc != currProfile.profileDescription)
                                        {
                                            NewLine();
                                            FormatLine($"{Ind14}The new profile descripiton is: ");
                                            if (newProfDesc.IsNotNEW())
                                            {
                                                Format($"{Ind14}\"");
                                                Format($"{newProfDesc}", ForECol.Highlight);
                                                FormatLine("\"");
                                            }
                                            else FormatLine($"{Ind14}n/a", ForECol.Accent);
                                            NewLine();

                                            Confirmation($"{Ind14}Confirm changes to the profile description? ", false, out profDescChangedQ);
                                        }

                                        if (profDescChangedQ)
                                            currProfile.profileDescription = newProfDesc;
                                        ConfirmationResult(profDescChangedQ, $"{Ind24}Profile description ", "has been updated.", "has not been changed.");
                                    }
                                }


                                // confirm changes
                                if (editOptNum == 6)
                                {
                                    /// no change; exit -- change; confirmation, exit
                                    if (!currProfile.Equals(prevCurrProfile))
                                    {
                                        ProfileIconSize confirmProfIconSize = ProfileIconSize.Mini;
                                        ProfileDisplayStyle confirmProfDispStyle = ProfileDisplayStyle.Full;

                                        // display old and new profiles
                                        //FormatLine("Confirm the changes made to the current profile.");
                                        //NewLine();

                                        FormatLine("Before Changes", ForECol.Heading2);
                                        DisplayProfileInfo(prevCurrProfile, confirmProfIconSize, confirmProfDispStyle);
                                        NewLine();

                                        FormatLine("After Changes", ForECol.Heading2);
                                        DisplayProfileInfo(currProfile, confirmProfIconSize, confirmProfDispStyle);
                                        NewLine();


                                        // confirmation : save \ discard changes
                                        FormatLine($"{Ind14}The changes made for the current profile are displayed above.");
                                        Confirmation($"{Ind14}Confirm the current profile's changes? ", true, out bool yesNo);

                                        if (yesNo)
                                        {
                                            bool updatedQ = ProfileHandler.UpdateProfile(currProfile);                                            
                                            if (updatedQ)
                                                prevCurrProfile = currProfile;

                                            ConfirmationResult(updatedQ, $"{Ind24}The current profile ", "has been updated.", "could not be updated.");
                                            endActiveMenuKeyQ = true;
                                        }
                                        else
                                        {
                                            Confirmation($"{Ind24}Discard changes to current profile? ", true, out bool discardYesNo);
                                            if (discardYesNo)
                                            {
                                                currProfile = prevCurrProfile; // ProfileHandler.GetCurrentProfile();
                                                endActiveMenuKeyQ = true;
                                            }
                                            ConfirmationResult(!discardYesNo, $"{Ind24}Changes to current profile ", "remain to be edited.", "will be discarded.");
                                        }
                                    }
                                    else
                                    {
                                        Format("No changes were made to the current profile.");
                                        Pause();
                                        endActiveMenuKeyQ = true;
                                    }
                                }
                            }
                            else
                            {
                                if (currProfile.AreEquals(prevCurrProfile))
                                    endActiveMenuKeyQ = true;
                                else
                                {
                                    NewLine();
                                    Format($"{Ind14}There are unsaved profile changes. Confirm the changes to exit.", ForECol.Warning);
                                    Pause();
                                }
                            }
                        }
                        else
                        {
                            Format($"{Ind24}There are no profiles to edit. Please create a profile to access this page.", ForECol.Warning);
                            Pause();

                            endActiveMenuKeyQ = true;
                        }

                        // TEMP //
                        //Pause();
                        //endActiveMenuKeyQ = true;
                    }


                    // delete profile -- destroy profile
                    if (profEditKey.Equals("c"))
                    {
                        Program.LogState(logStateMethod + "|Delete");
                        FormatLine("The current profile may be deleted.");
                        FormatLine("Deleting a profile is an irreversible action and cannot be undone. Will require a restart.", ForECol.Warning);
                        NewLine();

                        if (ProfileHandler.AllProfiles.Count > 1)
                        {
                            bool confirmDelete1, confirmDelete2 = false, confirmDelete3 = false;

                            // display
                            Wait(0.3f);
                            FormatLine("Current Profile".ToUpper(), ForECol.Accent);
                            DisplayProfileInfo(currProfile);
                            NewLine();

                            // confirmation 1
                            FormatLine("The profile to be deleted (current profile) is displayed above.");
                            Format("Press [Enter] to proceed to deleting this profile >> ");
                            confirmDelete1 = StyledInput(null).IsNE();


                            // confirmation 2
                            if (confirmDelete1)
                            {
                                NewLine();
                                Highlight(true, $"{Ind14}The profile '{currProfile.profileName}' is about to be deleted.", $"'{currProfile.profileName}'");
                                Confirmation($"{Ind14}Confirm the deletion of this profile? ", true, out confirmDelete2);
                            }

                            // confirmation 3
                            if (confirmDelete2)
                            {
                                NewLine();
                                Highlight(true, $"{Ind14}Once profile '{currProfile.profileName}' is deleted, it is unrecoverable.", $"'{currProfile.profileName}'");
                                Confirmation($"{Ind14}Absolutely certain of deletion? ", true, out confirmDelete3);
                            }

                            // IF confirm all deletes: delete profile;
                            // ELSE abandon deletion (any stage of confirmation);
                            if (confirmDelete1 && confirmDelete2 && confirmDelete3)
                            {
                                string deletedProfName = currProfile.profileName, deletedProfID = currProfile.profileID;
                                bool profileDeletedQ = ProfileHandler.DeleteProfile();
                                if (profileDeletedQ)
                                {
                                    currProfile = new ProfileInfo();
                                    FormatLine($"{Ind24}Profile '{deletedProfName}' ({deletedProfID}) has been deleted.", ForECol.Correction);
                                    Format($"{Ind24}Proceeding to program restart...");
                                    anyProfilesCreatedOrDeletedQ = true;
                                }
                                else Format($"{Ind24}Profile '{deletedProfName}' could not be deleted.", ForECol.Incorrection);
                                Pause();
                            }
                            else
                            {
                                Format($"{Ind24}Deletion of profile '{currProfile.profileName}' has been cancelled.", ForECol.Warning);
                                Pause();
                            }
                        }
                        else
                        {
                            FormatLine("There are not enough profiles to delete a profile.", ForECol.Warning);
                            Format("At least one profile must exist.");
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
        public static void PrintProfileIcon(ProfileInfo profileInfo, ProfileIconSize profIconSize = ProfileIconSize.Normal, bool useHeatMapQ = false)
        {            
            if (profileInfo.IsSetupQ())
            {
                // +++ PREPARATIONS +++
                const int normalIconStartIx = 4;
                bool useBasicColorsQ = false || useHeatMapQ;
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
                /// IF curr prof: get colors from style key; ELSE get colors from console colors CSV;
                Color color1 = Color.Black, color2 = Color.Black, color3 = Color.Black;
                if (ProfileHandler.CurrProfileID == profileInfo.profileID || profileInfo.consoleColorsCSV.IsNEW())
                {
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

                    ForECol fcol1 = (ForECol)fec1;
                    ForECol fcol2 = (ForECol)fec2;
                    ForECol fcol3 = (ForECol)fec3;

                    /// setting (ink) colors to forECol
                    color1 = GetPrefsForeColor(fcol1);
                    color2 = GetPrefsForeColor(fcol2);
                    color3 = GetPrefsForeColor(fcol3);

                }
                else
                {
                    Color[] profColors = profileInfo.ParseConsoleColorsFromSave();
                    if (profColors.HasElements(3))
                    {
                        /// setting (ink) colors to forECol
                        color1 = profColors[0];
                        color2 = profColors[1];
                        color3 = profColors[2];
                    }
                }


                // loading ink for use
                /// INKING IDS
                /// ' ' empty (no color)
                /// 0 - 1st color, heavy    1 - 1st color, light    
                /// 2 - 2nd color, heavy    3 - 2nd color, light
                /// 4 - 3rd color, heavy    5 - 3rd color, light
                inkList = new HPInk[6]
                {
                    new HPInk(0, color1, Dither.Dark),
                    new HPInk(0, color1, Dither.Light),
                    new HPInk(0, color2, Dither.Dark),
                    new HPInk(0, color2, Dither.Light),
                    new HPInk(0, color3, Dither.Dark),
                    new HPInk(0, color3, Dither.Light),
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

        // PRIVATE TOOL METHODS
        /// <summary>Takes the 3-character icon style key of a profile and provides the named values of the colors.</summary>
        /// <param name="atOrder">If <c>0</c>, will have all colors returned and comma separated, otherwise one of the three in order. Clamped [0, 3].</param>
        /// <returns>Either a comma separated list of all the <see cref="ForECol"/>s in style key, or one of the three based on value of <paramref name="atOrder"/>.</returns>
        static string GetIconStyleColors(string profIconStyleKey, int atOrder = 0)
        { 
            atOrder = atOrder.Clamp(0, 3);
            string iconColors = " ";

            if (profIconStyleKey.IsNotNEW())
                if (profIconStyleKey.Length == 3)
                {
                    int orderNum = 1;
                    foreach (char fc in profIconStyleKey)
                    {
                        if (int.TryParse(fc.ToString(), out int fColIx))
                        {
                            ForECol fCol = (ForECol)fColIx;

                            if (atOrder == 0)
                                iconColors += $"{fCol} ";
                            else if (atOrder == orderNum)
                                iconColors += fCol.ToString();
                        }
                        orderNum++;
                    }

                    iconColors = iconColors.Trim().Replace(" ", ", ");
                }
            return iconColors;
        }
    }
}
