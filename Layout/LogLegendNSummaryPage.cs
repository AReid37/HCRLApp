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
            bool viewDetailsOfTopQ = false;
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
                        {
                            FormatLine($"{Ind14}Due to the short version range, the ability to browse has been disabled.", ForECol.Highlight);
                            NewLine();
                        }
                        /// Display top-most and a few of the following
                        else
                            allowSummaryBrowsingQ = true;


                        // display version summaries first
                        HorizontalRule(subMenuUnderline);
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
                                    {
                                        if (sumx == 0 && viewDetailsOfTopQ)
                                        {
                                            NewLine();
                                            FormatLine("Version Details".ToUpper(), ForECol.Highlight);
                                            DisplayVersionDetails(sumDataToDisp.SummaryVersion, ForECol.Normal, false, true);
                                        }
                                        HorizontalRule('_');
                                    }
                                }
                        }
                        HorizontalRule(subMenuUnderline, 1);
                        if (Program.isDebugVersionQ)
                            FormatLine($"## Library has '{_resLibrary.Summaries.Count}' summaries. Display maximum is '{displayCount}' ({displayCountMinimum} + {displayCount - displayCountMinimum})", ForECol.Accent);


                        // prompt to select a certain version summary after
                        bool allowExitQ = false;
                        const string viewDetailsKey = "vd";
                        FormatLine($"{Ind14}Version number as 'a.bb' (major.minor). '{displayCount - 1}' additional {(displayCount - 1 == 1 ? $"summary" : $"summaries")} may follow.", ForECol.Accent);
                        FormatLine($"{Ind14}Enter '{viewDetailsKey}' to toggle details of top-most version. Press [Enter] to exit page.", ForECol.Accent);

                        Format($"{Ind14}View version summary ({verLow.ToStringNums()} ~ {verHigh.ToStringNums()}) >> ");
                        if (StyledInput($"a.bb / {viewDetailsKey}").IsNotNE())
                        {
                            VerNum prevSelectedVerNum = selectedVer;
                            selectedVer = VerNum.None;
                            if (VerNum.TryParse(LastInput, out VerNum selectVerNum) && allowSummaryBrowsingQ)
                            {
                                if (selectVerNum.AsNumber.IsWithin(verLow.AsNumber, verHigh.AsNumber))
                                {
                                    selectedVer = selectVerNum;
                                    Format($"{Ind24}Version '{selectedVer.ToStringNums()}' selected as version summary to view.", ForECol.Correction);
                                    Pause();
                                }
                                else IncorrectionMessageQueue("Version number is not within library version range");
                            }
                            else
                            {
                                if (LastInput.Equals(viewDetailsKey))
                                    viewDetailsOfTopQ = !viewDetailsOfTopQ;
                                else if (allowSummaryBrowsingQ)
                                    IncorrectionMessageQueue("Version number did not follow 'a.bb' format");
                                else IncorrectionMessageQueue("Browsing disabled due to short version range");
                            }

                            if (!selectedVer.HasValue() && prevSelectedVerNum.HasValue())
                                selectedVer = prevSelectedVerNum;
                        }
                        else allowExitQ = true;
                        IncorrectionMessageTrigger($"{Ind34}", ".");
                        IncorrectionMessageQueue(null);


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

        /// <summary>Displays content details of a given version of library contents.</summary>
        /// <param name="verNum">The version to display.</param>
        /// <param name="displayCol">The base color of the display version details.</param>
        /// <param name="useHorizontalRuleQ">Whether to utilize the default horizontal rule accompanied with this display.</param>
        /// <param name="showContentsOnlyQ">If <c>true</c>, will only display the Added, Additional, and Updated sections.</param>
        static void DisplayVersionDetails(VerNum verNum, ForECol displayCol = ForECol.Highlight, bool useHorizontalRuleQ = false, bool showContentsOnlyQ = true)
        {
            if (verNum.HasValue())
            {
                // Fetch contents in specified verison
                Dbug.DeactivateNextLogSession();
                Dbug.StartLogging("LogLegendNSummaryPage:DisplayVersionDetails()");
                ResLibrary verLogDetails = new();
                List<string> allDataIDs = new();
                for (int rdx = 0; rdx < 3; rdx++)
                {
                    switch (rdx)
                    {
                        // contents - get matching ver log number
                        case 0:
                            List<ResContents> resContents = new();
                            List<string> looseDataIDs = new();
                            List<ContentAdditionals> looseConAddits = new();
                            List<ContentChanges> looseConChanges = new();

                            /// filtering occurs here
                            foreach (ResContents resCon in _resLibrary.Contents)
                            {
                                ResContents clone = null;

                                if (resCon != null)
                                    if (resCon.IsSetup())
                                    {
                                        bool fetchedConBaseQ = false;

                                        // ConBase
                                        if (resCon.ConBase.VersionNum.Equals(verNum))
                                        {
                                            fetchedConBaseQ = true;
                                            clone = new ResContents(resCon.ShelfID, resCon.ConBase);
                                            allDataIDs.AddRange(resCon.ConBase.DataIDString.Split(' '));

                                            /// ConAddits (same ver)
                                            if (resCon.ConAddits.HasElements())
                                            {
                                                foreach (ContentAdditionals rca in resCon.ConAddits)
                                                    if (rca.VersionAdded.Equals(verNum))
                                                    {
                                                        ContentAdditionals rcaClone =
                                                            new(rca.VersionAdded, rca.RelatedDataID, rca.OptionalName, rca.DataIDString.Split(' '));
                                                        rcaClone.ContentName = clone.ContentName;

                                                        clone.StoreConAdditional(rcaClone);
                                                        allDataIDs.AddRange(rca.DataIDString.Split(' '));
                                                    }
                                            }

                                            /// ConChanges (same ver)
                                            if (resCon.ConChanges.HasElements())
                                            {
                                                foreach (ContentChanges rcc in resCon.ConChanges)
                                                    if (rcc.VersionChanged.Equals(verNum))
                                                    {
                                                        clone.StoreConChanges(rcc);
                                                        allDataIDs.Add(rcc.RelatedDataID);
                                                    }
                                            }

                                            resContents.Add(clone);
                                        }

                                        // ConAddits (loose)
                                        if (!fetchedConBaseQ)
                                        {
                                            if (resCon.ConAddits.HasElements())
                                                foreach (ContentAdditionals ca in resCon.ConAddits)
                                                    if (ca.VersionAdded.Equals(verNum))
                                                    {
                                                        looseDataIDs.Add(ca.RelatedDataID);
                                                        ContentAdditionals caClone =
                                                            new(ca.VersionAdded, ca.RelatedDataID, ca.OptionalName, ca.DataIDString.Split(' '));
                                                        caClone.ContentName = resCon.ContentName;
                                                        looseConAddits.Add(caClone);

                                                        allDataIDs.Add(ca.RelatedDataID);
                                                        allDataIDs.AddRange(ca.DataIDString.Split(' '));
                                                    }
                                        }

                                        // ConChanges (loose)
                                        if (!fetchedConBaseQ)
                                        {
                                            if (resCon.ConChanges.HasElements())
                                                foreach (ContentChanges cc in resCon.ConChanges)
                                                    if (cc.VersionChanged.Equals(verNum))
                                                    {
                                                        looseDataIDs.Add(cc.RelatedDataID);
                                                        looseConChanges.Add(cc);

                                                        allDataIDs.Add(cc.RelatedDataID);
                                                    }
                                        }
                                    }
                            }

                            /// load into instance
                            ResContents looseResCon = new(0, new ContentBaseGroup(verNum, ResLibrary.LooseResConName, looseDataIDs.ToArray()));
                            foreach (ContentAdditionals lca in looseConAddits)
                                looseResCon.StoreConAdditional(lca);
                            foreach (ContentChanges lcc in looseConChanges)
                                looseResCon.StoreConChanges(lcc);

                            resContents.Insert(0, looseResCon);
                            verLogDetails.AddContent(true, resContents.ToArray());
                            break;


                        // legends - get all of them // actually no.. just get what's there
                        case 1:
                            /// gather all unique legend keys used
                            string legendKeysInUse = " ";
                            foreach (string dId in allDataIDs)
                            {
                                LogDecoder.DisassembleDataID(dId, out string dk, out _, out string sfx);
                                if (!legendKeysInUse.Contains($" {dk} "))
                                    legendKeysInUse += $" {dk} ";

                                if (sfx.IsNotNE())
                                {
                                    if (!legendKeysInUse.Contains($" {sfx} "))
                                        legendKeysInUse += $" {sfx} ";

                                    foreach (char sfxC in sfx)
                                        if (!legendKeysInUse.Contains($" {sfxC} "))
                                            legendKeysInUse += $" {sfxC} ";
                                }
                            }
                            /// filter legend datas by legend keys in use
                            List<LegendData> legendsUsed = new();
                            if (legendKeysInUse.IsNotNEW())
                                foreach (LegendData legData in _resLibrary.Legends.ToArray())
                                {
                                    if (legendKeysInUse.Contains($" {legData.Key} "))
                                        legendsUsed.Add(legData);
                                }
                            /// load into instance
                            if (legendsUsed.HasElements())
                                verLogDetails.AddLegend(legendsUsed.ToArray());
                            //verLogDetails.AddLegend(_resLibrary.Legends.ToArray());
                            break;


                        // summaries - get of matching ver log number
                        case 2:
                            bool fetchedSummaryQ = false;
                            for (int sumx = 0; !fetchedSummaryQ && sumx < _resLibrary.Summaries.Count; sumx++)
                            {
                                if (_resLibrary.Summaries[sumx].SummaryVersion.Equals(verNum))
                                {
                                    fetchedSummaryQ = true;
                                    verLogDetails.AddSummary(_resLibrary.Summaries[sumx]);
                                }
                            }
                            break;
                    }
                }
                Dbug.EndLogging();


                // display contents of specified version
                if (useHorizontalRuleQ)
                    HorizontalRule('-');
                
                if (verLogDetails.IsSetup())
                {
                    string dataIDList = "";
                    ResContents looseResCon = new();
                    if (verLogDetails.Contents[0].ContentName == ResLibrary.LooseResConName)
                        looseResCon = verLogDetails.Contents[0];

                    bool thereAreAdditionalsQ = false, thereAreUpdatesQ = false;
                    for (int dispx = 0; dispx < 7; dispx++)
                    {
                        switch (dispx)
                        {
                            // version
                            case 0:
                                if (!showContentsOnlyQ)
                                {
                                    FormatLine($"Version : {verNum.ToStringNums()}", displayCol);
                                    NewLine();
                                }
                                break;

                            // added 
                            case 1:
                                FormatLine($"Added".ToUpper(), displayCol);
                                int minusLooseResCon = 0;
                                for (int rx = 0; rx < verLogDetails.Contents.Count; rx++)
                                {
                                    ResContents resCbg = verLogDetails.Contents[rx];
                                    if (resCbg.ContentName != ResLibrary.LooseResConName)
                                    {
                                        FormatLine($"#{rx + 1 - minusLooseResCon,-2} {resCbg.ContentName}    {resCbg.ConBase.DataIDString}", displayCol);
                                        dataIDList += resCbg.ConBase.DataIDString + " ";

                                        if (resCbg.ConAddits.HasElements() && !thereAreAdditionalsQ)
                                            thereAreAdditionalsQ = true;
                                        if (resCbg.ConChanges.HasElements() && !thereAreUpdatesQ)
                                            thereAreUpdatesQ = true;
                                    }
                                    else minusLooseResCon = 1;
                                }                                
                                if (showContentsOnlyQ)
                                {
                                    if (thereAreAdditionalsQ || looseResCon.ConAddits.HasElements() || thereAreUpdatesQ || looseResCon.ConChanges.HasElements())
                                        NewLine();
                                }        
                                else NewLine();
                                break;

                            // additional
                            case 2:
                                if (looseResCon.ConAddits.HasElements() || thereAreAdditionalsQ)
                                {
                                    FormatLine($"Additional".ToUpper(), displayCol);
                                    foreach (ResContents resCa in verLogDetails.Contents)
                                    {
                                        if (resCa.ConAddits.HasElements())
                                            foreach (ContentAdditionals ca in resCa.ConAddits)
                                            {
                                                if (resCa.ContentName == ResLibrary.LooseResConName)
                                                    Format($">> ", displayCol);
                                                else Format($"> ", displayCol);

                                                string fixedOptName = ca.OptionalName.IsNE() ? "" : ca.OptionalName + " ";
                                                string fixedContentName = resCa.ContentName == ResLibrary.LooseResConName ? "" : resCa.ContentName + " ";

                                                FormatLine($"{fixedOptName}({ca.DataIDString}) - {fixedContentName}({ca.RelatedDataID})", displayCol);
                                                dataIDList += ca.DataIDString + " ";
                                            }
                                    }                                    
                                    
                                    if (showContentsOnlyQ)
                                    {
                                        if (thereAreUpdatesQ || looseResCon.ConChanges.HasElements())
                                            NewLine();
                                    }
                                    else NewLine();
                                }
                                break;

                            // tta
                            case 3:
                                if (!showContentsOnlyQ)
                                {
                                    int ttaNum = verLogDetails.Summaries[0].TTANum;
                                    FormatLine($"TTA : {ttaNum}", displayCol);
                                    NewLine();
                                }
                                break;

                            // updated
                            case 4:
                                if (looseResCon.ConChanges.HasElements() || thereAreUpdatesQ)
                                {
                                    FormatLine($"Updated".ToUpper(), displayCol);
                                    foreach (ResContents resCc in verLogDetails.Contents)
                                    {
                                        if (resCc.ConChanges.HasElements())
                                            foreach (ContentChanges cc in resCc.ConChanges)
                                            {
                                                if (resCc.ContentName == ResLibrary.LooseResConName)
                                                    Format($">> ", displayCol);
                                                else Format($"> ", displayCol);
                                                FormatLine($"{cc.InternalName} ({cc.RelatedDataID}) - {cc.ChangeDesc}", displayCol);
                                            }
                                    }

                                    if (!showContentsOnlyQ)
                                        NewLine();
                                }
                                break;

                            // legend
                            case 5:
                                if (!showContentsOnlyQ)
                                {
                                    FormatLine($"Legend".ToUpper(), displayCol);
                                    List<string> legKeys = new();
                                    foreach (string dataID in dataIDList.Split(' '))
                                    {
                                        LogDecoder.DisassembleDataID(dataID, out string dk, out _, out string sfx);
                                        if (!legKeys.Contains(dk))
                                            legKeys.Add(dk);
                                        if (!legKeys.Contains(sfx))
                                            legKeys.Add(sfx);
                                    }
                                    legKeys = legKeys.ToArray().SortWords();
                                    if (legKeys.HasElements())
                                    {
                                        foreach (string legKey in legKeys)
                                        {
                                            foreach (LegendData legData in verLogDetails.Legends)
                                            {
                                                if (legData.Key.Equals(legKey))
                                                {
                                                    FormatLine($"{legData.Key}/{legData[0]}", displayCol);
                                                    break;
                                                }
                                            }
                                        }

                                    }
                                    NewLine();
                                }
                                break;

                            // summary
                            default:
                                if (!showContentsOnlyQ)
                                {
                                    FormatLine($"Summary".ToUpper(), displayCol);
                                    foreach (string sumPart in verLogDetails.Summaries[0].SummaryParts)
                                    {
                                        Format(" - ", displayCol);
                                        FormatLine(sumPart, displayCol);
                                    }
                                }
                                break;
                        }
                    }
                }
                else FormatLine($"{Ind24}Unable to display contents of version log '{verNum.ToStringNums()}'.", ForECol.Warning);
                
                if (useHorizontalRuleQ)
                    HorizontalRule('-');
            }
        }
    }
}
