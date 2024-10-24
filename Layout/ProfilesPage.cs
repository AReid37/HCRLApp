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

        static ProfileHandler _profilesRef;
        static Preferences _preferencesRef;
        static readonly char subMenuUnderline = '~';


        public static void GetProfilesReference(ProfileHandler profiles)
        {
            _profilesRef = profiles;
        }
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

                Program.LogState("Profile Select");
                Clear();
                Title("Profile Selection", cTHB, 1);
                FormatLine($"{Ind24}... some description ...", ForECol.Accent);
                NewLine(2);

                /// if no profile exist and openned: Prompt to create profile (ProfEditor)
                ///      ^ presumes that there is existing profile data (always true; whether default or user-generated)
                /// if profile(s) and none selected: Prompt to select profile (ProfSelect)
                /// if profile(s) and one selected: Default Menu (ProfSelect / ProfEditor)
                bool anyProfilesDetectedQ = false, anyProfileSelectedQ = false;
                if (_profilesRef != null)
                {
                    anyProfilesDetectedQ = _profilesRef.AllProfiles.HasElements();
                    anyProfileSelectedQ = _profilesRef.CurrProfileID != ProfileHandler.NoProfID;
                }
                

                string profMenuKey = null;
                bool validMenuKey = true;
                // IF 
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
        }


        // tbd
        static void SubPage_ProfileSelect()
        {
            NewLine(4);
            Format($"-- Openned Profile Selection Page --");
            Pause();

        }
        // tbd
        static void SubPage_ProfileEditor()
        {
            NewLine(4);
            Format($"-- Openned Profile Editor Page --");
            Pause();
        }



        
        // TOOL METHODS
        // tbd -- public for testing
        public static void DisplayCurrentProfileInfo()
        {

        }
        // tbd
        public static void PrintCurrentProfileIcon(int sizeMultiplier)
        {

        }
    }
}
