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

                Program.LogState("Profile Select");
                Clear();
                Title("Profile Selection", cTHB, 1);
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
            Program.LogState("Profile Select|Select");
            NewLine(4);
            Format($"-- Openned Profile Selection Page --");
            Pause();

        }
        // tbd
        static void SubPage_ProfileEditor()
        {
            bool exitProfEditorQ = false;
            do
            {
                BugIdeaPage.OpenPage();

                Program.LogState("Profile Select|Editor");
                Clear();

                // if no profiles detected

                Title("Profile Editor", subMenuUnderline, 1);
                Format($"-- Openned Profile Editor Page --");
                Pause();



                exitProfEditorQ = true; // temp
            }
            while (!exitProfEditorQ && !Program.AllowProgramRestart);
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
