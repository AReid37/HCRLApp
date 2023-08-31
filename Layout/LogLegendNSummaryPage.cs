using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp.Layout
{
    internal static class LogLegendNSummaryPage
    {
        static ResLibrary _resLibrary;
        static readonly char subMenuUnderline = '"';

        public static void GetResourceLibraryReference(ResLibrary mainLibrary)
        {
            _resLibrary = mainLibrary;
        }
        public static void OpenPage()
        {
            bool exitLegNSumMain = false;
            do
            {
                BugIdeaPage.OpenPage();
                
                Program.LogState("Legend And Summaries View");
                Clear();
                Title("Log Legend And Summaries", cTHB, 1);
                FormatLine($"{Ind24}View the summaries of submitted version logs and the collection of legend keys and their definitions.", ForECol.Accent);
                NewLine(2);

                bool validMenuKey = TableFormMenu(out short optNum, "Legend And Summaries Menu", null, false, $"{Ind24}Selection >> ", "1~3", 3, $"Log Legend,Log Summaries,{exitPagePhrase}".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (optNum)
                    {
                        case 1:
                            SubPage_LogLegend();
                            break;

                        case 2:
                            SubPage_LogSummaries();
                            break;

                        case 3:
                            exitLegNSumMain = true;
                            break;
                    }
                }

            } while (!exitLegNSumMain);
        }

        static void SubPage_LogLegend()
        {
            Program.LogState("Legend And Summaries View|Log Legend View");
            Clear();
            Title("Version Log Glossary (Legend)", subMenuUnderline, 1);
            FormatLine("A dictionary of all the legend keys and definitions collected from version logs.", ForECol.Accent);
            NewLine();

            bool noLibraryData = true;
            // legend glossary
            if (_resLibrary != null)
            {
                if (_resLibrary.IsSetup())
                {
                    noLibraryData = false;

                    // gather data
                    LegendData[] sortedLegendData = null;
                    VerNum latestVersion = VerNum.None;
                    if (sortedLegendData == null)
                    { /// wrapping
                        List<string> sortedKeys = new();
                        foreach (LegendData legDat in _resLibrary.Legends)
                            sortedKeys.Add(legDat.Key);
                        sortedKeys = sortedKeys.ToArray().SortWords();

                        sortedLegendData = new LegendData[sortedKeys.Count];
                        for (int kix = 0; kix < sortedKeys.Count; kix++)
                        {
                            string key = sortedKeys[kix];
                            LegendData matchingLegDat = null;
                            for (int mlgx = 0; mlgx < _resLibrary.Legends.Count && matchingLegDat == null; mlgx++)
                            {
                                if (key == _resLibrary.Legends[mlgx].Key)
                                    matchingLegDat = _resLibrary.Legends[mlgx];
                            }
                            if (matchingLegDat != null)
                            {
                                sortedLegendData[kix] = matchingLegDat;
                                //Dbug.SingleLog("--", $"Sorted @ix{kix} --> {matchingLegDat}");
                            }
                        }

                        _resLibrary.GetVersionRange(out _, out latestVersion);
                    }

                    // print table
                    if (sortedLegendData.HasElements())
                    {
                        /// table setup
                        Table2Division tDivSize = Table2Division.KCSmall;
                        TableRowDivider(true);
                        TableRowDivider(subMenuUnderline, false, GetPrefsForeColor(ForECol.Accent));

                        Program.ToggleFormatUsageVerification();
                        ToggleWordWrappingFeature(false);

                        /// table header
                        HoldNextListOrTable();
                        Table(tDivSize, "Version And Key", '|', "Definitions");
                        Format(LatestTablePrintText.ToUpper(), ForECol.Heading2);
                        TableRowDivider(false);

                        /// table contents                        
                        foreach (LegendData tableLeg in sortedLegendData)
                        {
                            const string backtick = "`";
                            const string replaceBacktick = DataHandlerBase.Sep;

                            string tabKeyInfo = $"{tableLeg.VersionIntroduced.ToStringNums()} {tableLeg.Key}".Replace(backtick, replaceBacktick);
                            string tabDat1Info = tableLeg.DefinitionsString.Replace(backtick, replaceBacktick);

                            HoldNextListOrTable();
                            Table(tDivSize, tabKeyInfo, '|', tabDat1Info);
                            Highlight(true, LatestTablePrintText.Replace("\n", " ").Replace(replaceBacktick, backtick), $" {tableLeg.Key} ", tableLeg[0]);
                            if (HSNL(0, 2) - 2 == 0)
                                HorizontalRule('-');
                        }

                        Program.ToggleFormatUsageVerification();
                        ToggleWordWrappingFeature(true);
                    }
                    Format($"{Ind14}All Log Legends from latest library version ({latestVersion}). ", ForECol.Accent);
                    Pause();
                }
            }
            // no legend glossary
            if (noLibraryData)
            {
                Format($"{Ind24}The library shelves are empty. No legend keys and definitions found.", ForECol.Normal);
                Pause();
            }
        }
        static void SubPage_LogSummaries()
        {
            // check for summaries
            bool noLibraryData = true;
            if (_resLibrary != null)
                noLibraryData = !_resLibrary.IsSetup();


            // page function
            bool exitSummarySubPageQ = true;
            VerNum selectedVer = VerNum.None;
            do
            {
                BugIdeaPage.OpenPage();

                Program.LogState("Legend And Summaries View|Log Summaries View");                
                Clear();
                Title("Version Log Summaries", subMenuUnderline, 1);
                FormatLine("Browse the version summaries obtained from submitted version logs.", ForECol.Accent);
                NewLine();

                // summaries available
                if (!noLibraryData)
                {
                    /// smart display:
                    ///     Few summaries, can be displayed entirely: display plainly, no browsing
                    ///     Many summaries, too many to show: version number decides "top-most" summary, shows the rest to a given degree.
                    ///     - or -
                    ///     <= 4 summaries; display plainly, no browsing
                    ///     > 4 summaries; version number decides top-most summary, show next 3 following summaries
                    const int displayCountMinimum = 2;
                    int displayCount = displayCountMinimum + HSNL(0, 7).Clamp(0, 5); //+ WSLL(0, 3).Clamp(0, 1);
                    bool fetchedVerRangeQ = _resLibrary.GetVersionRange(out VerNum verLow, out VerNum verHigh);
                    if (fetchedVerRangeQ)
                    {
                        bool allowSummaryBrowsingQ = false;
                        FormatLine($"The oldest and latest versions within the library ranges from '{verLow}' to '{verHigh}'.");
                        //NewLine();

                        /// Display plainly without searching
                        if (verHigh.AsNumber - verLow.AsNumber + 1 <= displayCount)
                            FormatLine($"{Ind14}Due to the short version range, the ability to browse has been disabled.", ForECol.Highlight);
                        /// Display top-most and a few of the following
                        else
                            allowSummaryBrowsingQ = true;
                        HorizontalRule(subMenuUnderline);

                        // display version summaries first
                        for (int sumx = 0; sumx < displayCount; sumx++)
                        {
                            // fetch
                            SummaryData sumDataToDisp = null;
                            if (_resLibrary.Summaries.HasElements())
                                foreach (SummaryData sumDat in _resLibrary.Summaries)
                                {
                                    if (selectedVer.HasValue())
                                    {
                                        if (sumDat.SummaryVersion.AsNumber == selectedVer.AsNumber + sumx)
                                        {
                                            sumDataToDisp = sumDat;
                                            break;
                                        }
                                    }
                                    else 
                                    {
                                        if (sumx + 1 <= _resLibrary.Summaries.Count)
                                        {
                                            if (sumDat.Equals(_resLibrary.Summaries[sumx]))
                                            {
                                                sumDataToDisp = sumDat;
                                                break;
                                            }
                                        }
                                    }                                        
                                }

                            // style 
                            if (sumDataToDisp != null)
                                if (sumDataToDisp.IsSetup())
                                {
                                    HoldNextListOrTable();
                                    string summaryHeaderText = $"Version {sumDataToDisp.SummaryVersion.ToStringNums()} Summary";

                                    Table(Table2Division.KCMedium, sumx == 0 ? summaryHeaderText.ToUpper() : summaryHeaderText, ' ', $"'{sumDataToDisp.TTANum}' contents added");

                                    Format(LatestTablePrintText, ForECol.Highlight);
                                    foreach (string summaryPart in sumDataToDisp.SummaryParts)
                                    {
                                        HoldWrapIndent(true);
                                        Format(" - ", ForECol.Accent);
                                        FormatLine($"{summaryPart}");
                                        HoldWrapIndent(false);
                                    }

                                    if (sumx + 1 < displayCount)
                                        HorizontalRule('_');
                                        //NewLine();
                                }
                        }
                        HorizontalRule(subMenuUnderline, 1);
                        if (Program.isDebugVersionQ)
                            FormatLine($"## Library has '{_resLibrary.Summaries.Count}' summaries. Display maximum is '{displayCount}' ({displayCountMinimum} + {displayCount - displayCountMinimum})", ForECol.Accent);

                        // prompt to select a certain version summary after
                        bool allowExitQ = false;
                        if (allowSummaryBrowsingQ)
                        {
                            FormatLine($"{Ind14}Version number as 'a.bb' (major.minor). '{displayCount - 1}' additional {(displayCount - 1 == 1 ? $"summary" : $"summaries")} may follow.", ForECol.Accent);
                            Format($"{Ind14}View version summary ({verLow.ToStringNums()} ~ {verHigh.ToStringNums()}) >> ");
                            if (StyledInput("a.bb").IsNotNE())
                            {
                                selectedVer = VerNum.None;
                                if (VerNum.TryParse(LastInput, out VerNum selectVerNum))
                                {
                                    if (selectVerNum.AsNumber.IsWithin(verLow.AsNumber, verHigh.AsNumber))
                                    {
                                        selectedVer = selectVerNum;
                                        Format($"{Ind24}Version '{selectedVer.ToStringNums()}' selected as version summary to view.", ForECol.Correction);
                                        Pause();
                                    }
                                    else IncorrectionMessageQueue("Version number is not within library version range");
                                }
                                else IncorrectionMessageQueue("Version number did not follow 'a.bb' format");
                            }
                            else allowExitQ = true;

                            IncorrectionMessageTrigger($"{Ind34}", ".");
                            IncorrectionMessageQueue(null);
                        }
                        else Pause();


                        // auto-exit page or not?
                        exitSummarySubPageQ = !allowSummaryBrowsingQ || allowExitQ;
                        //Text("End---");
                        //Pause();
                    }
                    else
                    {
                        Format($"{Ind24}Failed to fetch library versions. Please try again.", ForECol.Warning);
                        Pause();
                    }
                }
                // no summaries available
                else 
                {
                    Format($"{Ind24}The library shelves are empty. There are no version summaries to view.", ForECol.Normal);
                    Pause();
                }

            } while (!exitSummarySubPageQ);            
        }
    }
}
