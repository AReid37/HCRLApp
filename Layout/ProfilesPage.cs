using System;
using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System.IO;

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
            ProfileInfo currProfile = new();

            do
            {
                BugIdeaPage.OpenPage();

                if (activeMenuKey.IsNE())
                    Program.LogState("Profiles Page|Editor");
                Clear();


                anyProfilesDetected = ProfileHandler.AllProfiles.HasElements();
                if (anyProfilesDetected)
                    currProfile = ProfileHandler.GetCurrentProfile();


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
                            // the get to rename it, at the very least
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


                            /// 1c. profile name confirmation
                            if (validProfileNameQ)
                            {
                                NewLine();
                                FormatLine($"{Ind14}- - - - - - -", ForECol.Accent);
                                Format($"{Ind14}The name of the new profile is: ");
                                Highlight(false, $"'{profileName}'.", profileName);
                                if (profileName == newProfInfo.profileName)
                                    Format(" Default Generated", ForECol.Accent);
                                NewLine();

                                // IF confirmed: create profile and exit; ELSE (IF other profiles exist: don't create profile and exit; ELSE return to create prompt)
                                Confirmation($"{Ind14}Confirm profile name >> ", true, out bool yesNo);
                                if (yesNo)
                                {
                                    newProfInfo.profileName = profileName;
                                    bool profileCreatedQ = ProfileHandler.CreateProfile(newProfInfo, ProfileHandler.ExternalProfileDetectedQ);

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
                        if (anyProfilesDetected)
                        {
                            Program.LogState("Profiles Page|Editor|Edit");
                            Format("< profile editor >");
                            //FormatLine("< profile editor >");
                            //NewLine();
                            Pause();
                        }
                        else
                        {
                            Program.LogState("Profiles Page|Editor|Edit (None)");
                            FormatLine("NO PROFILES TO EDIT");
                            Format("< profile editor >");
                            //FormatLine("< profile editor >");
                            //NewLine();
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
                        exitProfEditorQ = false;
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
        // tbd -- public for testing
        public static void DisplayCurrentProfileInfo()
        {

        }
        // tbd
        public static void PrintCurrentProfileIcon(bool includeProfileNameQ, ProfileIconSize profIconSize = ProfileIconSize.Normal)
        {

        }
    }
}
