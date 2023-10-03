using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp.Layout
{
    public static class BugIdeaPage
    {
        static BugIdeaData _bugIdeaData;
        static readonly char subMenuUnderline = '^';

        public static void GetBugIdeaDataReference(BugIdeaData bugIdeaRef)
        {
            _bugIdeaData = bugIdeaRef;
        }
        /// <summary>Opens the Bug/Idea hidden page. Execute before printing following pages (top of loop).</summary>
        public static void OpenPage()
        {
            bool exitBugIdeaPageQ = false;
            if (IsEnterBugIdeaPageQueued() && _bugIdeaData != null)
            {
                WithinBugIdeaPageQ = true;

                do
                {
                    Program.LogState("Bug / Idea");
                    Clear();
                    Title("Bug / Idea", cTHB, 1);
                    FormatLine("Report bugs, suggest ideas, and view previous bug / idea submissions.", ForECol.Accent);
                    NewLine();

                    bool validMenuKey = TableFormMenu(out short optNum, "Bug / Idea Menu", subMenuUnderline, false, $"{Ind14}Select action >> ", null, 4, $"Submit Entry,View Entries,Clear Entries,{exitSubPagePhrase}".Split(','));

                    if (validMenuKey)
                    {
                        NewLine(2);
                        HorizontalRule(subMenuUnderline, 1);

                        // -- SUBMIT BUG \ IDEA --
                        if (optNum == 1)
                        {
                            Confirmation("Is this a bug report? ", true, out bool isBugQ);
                            FormatLine($"#{(isBugQ ? "Bug" : "Idea")} description may not contain '{DataHandlerBase.Sep}' character.", ForECol.Accent);
                            Format($"{(isBugQ ? "Report bug" : "Suggest idea")} >> ", ForECol.Warning);
                            string desc = StyledInput($"_ _ _ _ _");

                            if (desc.IsNotNEW())
                            {
                                if (!desc.Contains(DataHandlerBase.Sep))
                                {
                                    BugIdeaInfo newBii = new(isBugQ, desc, true);
                                    bool isDupeQ = false;
                                    for (int bx = 0; bx < _bugIdeaData.biInfo.Count && !isDupeQ; bx++)
                                        isDupeQ = _bugIdeaData.biInfo[bx].Equals(newBii);

                                    if (!isDupeQ)
                                    {
                                        NewLine();
                                        Confirmation($"{Ind14}Confirm submission of this {(isBugQ ? "bug report" : "suggested idea")}? ", true, out bool yesNo);
                                        if (yesNo)
                                            _bugIdeaData.AddInfo(isBugQ, desc);
                                        ConfirmationResult(yesNo, $"{Ind24}Submission ", "confirmed and saved.", "cancelled.");
                                    }
                                    else IncorrectionMessageQueue($"This {(isBugQ ? "bug report" : "suggested idea")} is a duplicate entry");
                                }
                                else IncorrectionMessageQueue($"Description may not contain '{DataHandlerBase.Sep}' character.");
                            }
                            else IncorrectionMessageQueue("A description is required");
                            IncorrectionMessageTrigger($"{Ind14}[X] ", ".");

                            IncorrectionMessageQueue(null);
                        }


                        // -- VIEW SUBMISSIONS --
                        if (optNum == 2)
                        {
                            if (_bugIdeaData.IsSetup())
                            {
                                /// gather them
                                List<BugIdeaInfo> biiBugs = new(), biiIdeas = new();
                                foreach (BugIdeaInfo bii in _bugIdeaData.biInfo)
                                {
                                    if (bii.IsSetup())
                                    {
                                        if (bii.isBugQ)
                                            biiBugs.Add(bii);
                                        else biiIdeas.Add(bii);
                                    }
                                }

                                const char tableDiv = ' ';
                                const Table2Division divType = Table2Division.KCTiny;
                                const ForECol colBase = ForECol.Normal, colAlt = ForECol.Highlight;
                                GetCursorPosition();


                                // bugs here
                                NewLine();
                                Important("Bug Reports");
                                if (biiBugs.HasElements())
                                {
                                    for (int bx = 0; bx < biiBugs.Count; bx++)
                                    {
                                        HoldNextListOrTable();
                                        Table(divType, $"{bx + 1}#", tableDiv, biiBugs[bx].description);

                                        Format(LatestTablePrintText, bx % 2 == 0 ? colBase : colAlt);
                                    }
                                }
                                else FormatLine($"{Ind24}No bugs reported.", ForECol.Accent);
                                NewLine(2);


                                // ideas here
                                Important("Suggested Ideas");
                                if (biiIdeas.HasElements())
                                {
                                    for (int ix = 0; ix < biiIdeas.Count; ix++)
                                    {
                                        HoldNextListOrTable();
                                        Table(divType, $"{ix + 1}#", tableDiv, biiIdeas[ix].description);

                                        Format(LatestTablePrintText, ix % 2 == 0 ? colBase : colAlt);
                                    }
                                }
                                else FormatLine($"{Ind24}No ideas suggested.", ForECol.Accent);
                                NewLine();


                                // end
                                Format("#End Bug / Idea", ForECol.Accent);
                                SetCursorPosition();
                                Pause();
                            }
                            else
                            {
                                Format($"{Ind24}There are no bug reports or suggested ideas to view.");
                                Pause();
                            }
                        }


                        // -- CLEAR ENTRIES --
                        if (optNum == 3)
                        {
                            Confirmation($"{Ind14}Clear all bug reports and idea suggestions? ", true, out bool yesNo);
                            bool reYesNo = false;

                            if (yesNo)
                            {
                                Confirmation($"{Ind24}Certianly clear these entries? ", true, out reYesNo);

                                if (yesNo && reYesNo)
                                    _bugIdeaData.biInfo = new List<BugIdeaInfo>();
                            }
                            NewLine();
                            ConfirmationResult(yesNo && reYesNo, $"{Ind24}", "Cleared all bug / idea entries.", "Cancelled clearing bug / idea entries.");
                        }


                        // -- EXIT --
                        exitBugIdeaPageQ = optNum == 4;
                    }
                }
                while (!exitBugIdeaPageQ);

                WithinBugIdeaPageQ = false;
            }
            UnqueueEnterBugIdeaPage();

            if (_bugIdeaData != null)
                if (_bugIdeaData.ChangesMade())
                    Program.SaveData(true);
        }

    }
}
