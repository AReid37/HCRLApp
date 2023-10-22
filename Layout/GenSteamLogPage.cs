using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;
using static HCResourceLibraryApp.DataHandling.SFormatterHandler;

namespace HCResourceLibraryApp.Layout
{
    internal static class GenSteamLogPage
    {
        static ResLibrary _resLibrary;
        static SFormatterData _formatterData;
        static readonly char subMenuUnderline = '+';
        static SFormatterLibRef _sfLibraryRef = new();
        static bool isUsingFormatterNo1Q = true;
        static List<SFormatterHistory> _editorHistory1 = new(), _editorHistory2 = new();
        static int historyActionNumber1, historyActionNumber2;

        public static void GetResourceLibraryReference(ResLibrary mainLibrary)
        {
            _resLibrary = mainLibrary;
        }
        public static void GetSteamFormatterReference(SFormatterData formatterData)
        {
            _formatterData = formatterData;
        }
        public static void OpenPage()
        {
            /// How does this get setup?
            /// MAIN PAGE
            ///     -----------
            ///     Title and desc
            ///     Parameter View (shows what needs to be done before generating steam log)
            ///     --
            ///     Menu
            ///     > Opt1: Log Version Number  -> Button and Prompt
            ///     > Opt2: Steam Log Formatter -> Page
            ///     > Opt3: Generate Steam Log -> Page
            ///         -- Review parameters, generate steam log ('preview' and 'source code' views)
            ///     -----------
            ///     
            /// STEAM LOG FORMATTER
            /// Full-on acts like an IDE
            ///     -----------
            ///     Title, Desc
            ///     > Opt1: Edit Formatting -> Page
            ///         -- Edited line by line. Accompanied with editing info (syntax, language rules)
            ///             ^^ Choose line to edit as '{Add/Edit/Delete}{LineNumber}'
            ///             ^^ Type line right into line then enter a phrase to finish editing.
            ///             ^^ After edit end, color-coding and syntax checks will provide any errors, issues or so-on under the line if any
            ///         -- Has a history, color-codes elements
            ///     > Opt2: ?
            ///     ----------
            ///     
            /// 
            ///     -- Link to Steam Text Formatting --
            ///     https://steamcommunity.com/comment/Guide/formattinghelp
            ///     

            bool exitSteamGenPage = false;
            do
            {
                BugIdeaPage.OpenPage();
                
                Program.LogState("Generate Steam Log");
                Clear();
                Title("Generate Steam Log", cTHB, 1);
                FormatLine($"{Ind24}Facilitates generation of a version log using Steam's formatting rules.", ForECol.Accent);
                NewLine();

                #region preview parameters
                Title("--- Parameters Review ---");
                /// ver log here
                Format($"{Ind14}");
                if (_sfLibraryRef.IsSetup())
                {
                    Format($"Version Log {_sfLibraryRef.Version}", ForECol.Highlight);
                    Format($" | ", ForECol.Accent);
                    Format($"'{_sfLibraryRef.TTA}' content{(_sfLibraryRef.TTA == "1" ? "" : "s")}", _sfLibraryRef.TTA != "0" ? ForECol.Normal : ForECol.Accent);
                    Format($" | ", ForECol.Accent);
                    Format($"'{_sfLibraryRef.UpdatedCount}' update{(_sfLibraryRef.UpdatedCount == "1" ? "" : "s")}", _sfLibraryRef.UpdatedCount != "0"? ForECol.Normal : ForECol.Accent);
                    NewLine();
                }
                else FormatLine("No version log selected", ForECol.Accent);
                /// formatter here
                Format($"{Ind14}");
                if (_formatterData != null)
                {
                    isUsingFormatterNo1Q = _formatterData.EditProfileNo1Q;
                    Format("Using formatter profile ");
                    Format($"'{(isUsingFormatterNo1Q ? _formatterData.Name1 : _formatterData.Name2)}'", ForECol.Highlight);

                    if (_formatterData.IsSetup(isUsingFormatterNo1Q))
                    {
                        CheckSyntax(isUsingFormatterNo1Q ? _formatterData.LineData1.ToArray() : _formatterData.LineData2.ToArray());
                        if (ErrorCount != 0)
                            Format($" ('{ErrorCount}' errors)", ForECol.Incorrection);
                    }
                    else
                        Format(" (empty)");
                    NewLine();
                }
                else FormatLine("No formatter data", ForECol.Accent);
                FormatLine("---");
                NewLine();
                #endregion

                bool validMenuKey = TableFormMenu(out short genMenuKey, "Generation Menu", subMenuUnderline, true, $"{Ind24}Choose parameter to edit >> ", "1~4", 2, $"Log Version,Steam Log Formatter,Generate Steam Log,{exitPagePhrase}".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (genMenuKey)
                    {
                        //case "a":
                        case 1:
                            SubPage_LogVersion();
                            break;

                        //case "b":
                        case 2:
                            SubPage_SteamFormatter();
                            break;

                        //case "c":
                        case 3:
                            SubPage_GenerateSteamLog();
                            break;

                        //case "d":
                        case 4:
                            exitSteamGenPage = true;
                            break;
                    }
                }

            } while (!exitSteamGenPage);

            if (_formatterData != null)
                if (_formatterData.ChangesMade())
                    Program.SaveData(false);
        }

        // done
        static void SubPage_LogVersion()
        {
            /** CHOOSING A LOG VERSION
                - User enters the version number to represent log information to print
                - Once version number is provided, collect all information for version and display it akin to that of the decoded log preview
                - If the user 'okays' the version log, load all the information into an instance that can be used for formatter
             */

            bool exitLogVerSubPageQ;
            bool noLibraryData = true;
            if (_resLibrary != null)
                if (_resLibrary.IsSetup())
                    noLibraryData = false;
            do
            {
                BugIdeaPage.OpenPage();

                Program.LogState("Generate Steam Log|Log Version");
                Clear();
                Title("Select Log Version", subMenuUnderline, 2);

                /// library has data
                bool allowExitQ = false;
                if (!noLibraryData)
                {
                    if (_resLibrary.GetVersionRange(out VerNum lowVer, out VerNum highVer))
                    {
                        FormatLine($"The library versions range from '{lowVer}' to '{highVer}'. Enter the version log to generate a steam log for below.");
                        FormatLine($"{Ind14}Enter version numbers as 'a.bb' (major.minor).", ForECol.Accent);
                        Format($"{Ind14}Generate log version >> ");

                        // get ver num
                        VerNum selectedVerNum = VerNum.None;
                        string input = StyledInput("a.bb");
                        if (input.IsNotNE())
                        {
                            if (VerNum.TryParse(input, out VerNum parsedVerNum))
                            {
                                if (parsedVerNum.AsNumber.IsWithin(lowVer.AsNumber, highVer.AsNumber))
                                {
                                    NewLine();
                                    Confirmation($"{Ind14}Confirm '{parsedVerNum}' as selected version log number? ", false, out bool yesNo, $"{Ind24}Version '{parsedVerNum.ToStringNums()}' log selected to be generated.", $"{Ind24}Version log number will not be selected.");

                                    if (yesNo)
                                        selectedVerNum = parsedVerNum;
                                }
                                else IncorrectionMessageQueue("Version number is not within library version range");
                            }
                            else IncorrectionMessageQueue("Version number did not follow 'a.bb' format");
                        }
                        else allowExitQ = true;

                        IncorrectionMessageTrigger($"{Ind24}", ".");
                        IncorrectionMessageQueue(null);

                        // display and confirm ver log details
                        if (selectedVerNum.HasValue())
                        {
                            // gather version information
                            Dbug.DeactivateNextLogSession();
                            Dbug.StartLogging("GenSteamLogPage:SubPage_LogVersion()");
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
                                                    if (resCon.ConBase.VersionNum.Equals(selectedVerNum))
                                                    {
                                                        fetchedConBaseQ = true;
                                                        clone = new ResContents(resCon.ShelfID, resCon.ConBase);
                                                        allDataIDs.AddRange(resCon.ConBase.DataIDString.Split(' '));
                                                        
                                                        /// ConAddits (same ver)
                                                        if (resCon.ConAddits.HasElements())
                                                        {
                                                            foreach (ContentAdditionals rca in resCon.ConAddits)
                                                                if (rca.VersionAdded.Equals(selectedVerNum))
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
                                                                if (rcc.VersionChanged.Equals(selectedVerNum))
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
                                                                if (ca.VersionAdded.Equals(selectedVerNum))
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
                                                                if (cc.VersionChanged.Equals(selectedVerNum))
                                                                {
                                                                    string contentName = cc.InternalName.IsNE() ? resCon.ContentName : cc.InternalName;
                                                                    ContentChanges ccClone = new(cc.VersionChanged, contentName, cc.RelatedDataID, cc.ChangeDesc);

                                                                    looseDataIDs.Add(ccClone.RelatedDataID);
                                                                    looseConChanges.Add(ccClone);

                                                                    allDataIDs.Add(cc.RelatedDataID);
                                                                }
                                                    }
                                                }
                                        }

                                        /// load into instance
                                        ResContents looseResCon = new(0, new ContentBaseGroup(selectedVerNum, ResLibrary.LooseResConName, looseDataIDs.ToArray()));
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
                                            if (_resLibrary.Summaries[sumx].SummaryVersion.Equals(selectedVerNum))
                                            {
                                                fetchedSummaryQ = true;
                                                verLogDetails.AddSummary(_resLibrary.Summaries[sumx]);
                                            }
                                        }
                                        break;
                                }
                            }
                            Dbug.EndLogging();

                            // display ver details
                            NewLine(5);
                            Important("Review Selected Version Log", subMenuUnderline);
                            HorizontalRule('-');
                            if (verLogDetails.IsSetup())
                            {
                                ForECol displayCol = ForECol.Highlight;
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
                                            FormatLine($"Version : {selectedVerNum.ToStringNums()}", displayCol);
                                            NewLine();
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
                                            NewLine();
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
                                                            string fixedContentName = resCa.ContentName == ResLibrary.LooseResConName? "" : resCa.ContentName + " ";

                                                            FormatLine($"{fixedOptName}({ca.DataIDString}) - {fixedContentName}({ca.RelatedDataID})", displayCol);
                                                            dataIDList += ca.DataIDString + " ";
                                                        }
                                                }
                                                NewLine();
                                            }                                            
                                            break;

                                        // tta
                                        case 3:
                                            int ttaNum = verLogDetails.Summaries[0].TTANum;
                                            FormatLine($"TTA : {ttaNum}", displayCol);
                                            NewLine();
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
                                                NewLine();
                                            }                                            
                                            break;

                                        // legend
                                        case 5:
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
                                            break;

                                        // summary
                                        default:
                                            FormatLine($"Summary".ToUpper(), displayCol);
                                            foreach (string sumPart in verLogDetails.Summaries[0].SummaryParts)
                                            {
                                                Format(" - ", displayCol);
                                                FormatLine(sumPart, displayCol);
                                            }
                                            break;
                                    }
                                }
                            }
                            else FormatLine($"{Ind24}Unable to display contents of version log '{selectedVerNum.ToStringNums()}'.", ForECol.Warning);
                            HorizontalRule('-');


                            // confirm ver details
                            FormatLine($"Review the details above and confirm a match with selected version log.");
                            Confirmation("Details above match selected version log? ", false, out bool yesNo, $"{Ind24}Version log to generate confirmed.", $"{Ind24}Version log to generate denied.");
                            if (yesNo)
                            {
                                /// package info and send it away for formatting reference info
                                _sfLibraryRef = new SFormatterLibRef(verLogDetails);

                                if (_sfLibraryRef.IsSetup())
                                    allowExitQ = true;
                                else
                                {
                                    Format($"{Ind24}The version log details could not be packaged. Please try again.", ForECol.Incorrection);
                                    Pause();
                                }
                            }
                        }
                    }
                    else
                    {
                        Format($"Failed to fetch library versions. Please try again.", ForECol.Warning);
                        Pause();
                        allowExitQ = true;
                    }
                }
                /// library has no data
                else
                {
                    Format("The library has no version data. A log version cannot be selected.");
                    Pause();
                }

                exitLogVerSubPageQ = noLibraryData || allowExitQ;
            } while (!exitLogVerSubPageQ);            
        }
        // done
        static void SubPage_SteamFormatter()
        {
            bool exitFormatterSubPageQ = false;
            do
            {
                BugIdeaPage.OpenPage();

                Program.LogState("Generate Steam Log|Steam Formatter");
                Clear();
                Title("Steam Formatter", subMenuUnderline, 1);
                FormatLine("Allows editting and selecting a format to use when generating a steam log.", ForECol.Accent);
                NewLine(2);

                // display editing info
                if (_formatterData != null)
                {
                    Title("Formatter Editor Settings");
                    isUsingFormatterNo1Q = _formatterData.EditProfileNo1Q;
                    CheckSyntax(isUsingFormatterNo1Q ? _formatterData.LineData1.ToArray() : _formatterData.LineData2.ToArray());
                    string formatterInUse = $"Editing Formatter Profile #{(isUsingFormatterNo1Q ? 1 : 2)} - {(isUsingFormatterNo1Q ? _formatterData.Name1 : _formatterData.Name2)} ('{ErrorCount}' error{(ErrorCount == 1 ? "" : "s")})";
                    string nativeColCode = $"Using Native Color Code? {(_formatterData.UseNativeColorCodeQ ? "Yes" : "No (Custom)")}";

                    HoldNextListOrTable();
                    List(OrderType.Unordered, formatterInUse, nativeColCode);
                    if (LatestListPrintText.IsNotNE())
                        FormatLine(LatestListPrintText.Replace("\t", Ind14));
                    NewLine();
                }
               
                bool validMenuKey = ListFormMenu(out string fMenuKey, "Formatter Menu", null, null, null, true, $"Toggle Native Color Coding,Toggle Formatter Profile,Open Formatting Editor, {exitSubPagePhrase} [Enter]".Split(','));
                MenuMessageQueue(!validMenuKey && LastInput.IsNotNE(), false, null);

                if (validMenuKey)
                {
                    // toggle native color coding
                    if (fMenuKey.Equals("a") && _formatterData != null)
                    {
                        _formatterData.UseNativeColorCodeQ = !_formatterData.UseNativeColorCodeQ;
                        Format($"{Ind34}{(_formatterData.UseNativeColorCodeQ ? "Enabled" : "Disabled")} native color coding.", ForECol.Correction);
                        Pause();
                    }

                    // toggle formatter to edit
                    else if (fMenuKey.Equals("b") && _formatterData != null)
                    {
                        _formatterData.EditProfileNo1Q = !_formatterData.EditProfileNo1Q;
                        isUsingFormatterNo1Q = _formatterData.EditProfileNo1Q;
                        Format($"{Ind34}Now editing Formatter Profile #{(isUsingFormatterNo1Q ? 1 : 2)} :: '{(isUsingFormatterNo1Q ? _formatterData.Name1 : _formatterData.Name2)}'.", ForECol.Correction);
                        Pause();
                    }

                    // open formatting editor
                    else if (fMenuKey.Equals("c") && _formatterData != null)
                        FormattingEditor();

                    //// exit
                    else exitFormatterSubPageQ = true;
                }

                if (!validMenuKey && LastInput.IsNE())
                    exitFormatterSubPageQ = true;
                
                
            } while (!exitFormatterSubPageQ);
        }
        // done
        static void SubPage_GenerateSteamLog()
        {
            bool exitGenSteamLogSubPageQ;
            do
            {
                BugIdeaPage.OpenPage();

                /// pre checks
                bool hasFormatterQ = false, hasLibDataQ = _sfLibraryRef.IsSetup(), isFormatterErrorlessQ = false;
                if (_formatterData != null)
                {
                    hasFormatterQ = _formatterData.IsSetup(isUsingFormatterNo1Q);
                    if (hasFormatterQ)
                    {
                        if (isUsingFormatterNo1Q)
                            CheckSyntax(_formatterData.LineData1.ToArray());
                        else CheckSyntax(_formatterData.LineData2.ToArray());
                        isFormatterErrorlessQ = ErrorCount == 0;
                    }
                }

                if (hasFormatterQ && hasLibDataQ && isFormatterErrorlessQ)
                {
                    Program.LogState("Generate Steam Log|Log Generation");

                    Format($"{Ind24}Generating Steam Log... ", ForECol.Correction);
                    Wait(0.75f);
                    Format($"Please wait...");

                    if (_formatterData.ChangesMade())
                    {
                        NewLine(2);
                        Format("Changes were made to the formatter profile used...", ForECol.Accent);
                        Wait(1f);
                        Program.SaveData(true);
                    }

                    // parse formatting then preview
                    string[] display = FormattingParser(isUsingFormatterNo1Q? _formatterData.LineData1.ToArray() : _formatterData.LineData2.ToArray(), _sfLibraryRef);

                    /// display
                    Program.ToggleFormatUsageVerification();
                    ToggleWordWrappingFeature(false);
                    NewLine(3);
                    GetCursorPosition(-1);

                    Important($"GENERATED LOG - V{_sfLibraryRef.Version}", subMenuUnderline);
                    HorizontalRule(subMenuUnderline);
                    foreach (string dLine in display)
                    {
                        ForECol touchUpCol = ForECol.Normal;
                        /// styling for headings and horizontal rule
                        if (dLine.Trim().StartsWith("[h") && dLine.Trim().Contains("[/h"))
                            touchUpCol = ForECol.Heading1;
                            /// styling for tables and lists
                            if (dLine.Contains("[/td") || dLine.Contains("[/th") || dLine.Trim().StartsWith("[table") || dLine.Trim().StartsWith("[/table") || dLine.Trim().StartsWith("[*]") || dLine.Trim().EndsWith("list]"))
                                touchUpCol = ForECol.Highlight;

                        Format(dLine, touchUpCol);
                    }
                    NewLine();
                    HorizontalRule(subMenuUnderline);
                    NewLine();

                    SetCursorPosition();
                    Pause();
                    ToggleWordWrappingFeature(true);
                    Program.ToggleFormatUsageVerification();

                    exitGenSteamLogSubPageQ = true;
                }
                else
                {
                    NewLine();
                    if (hasLibDataQ)
                    {
                        if (isFormatterErrorlessQ)
                            Format($"{Ind14}This page will not function with an empty formatter profile.", ForECol.Incorrection);
                        else Format($"{Ind14}This page will not function until all errors in selected formatter profile are resolved.", ForECol.Incorrection);
                    }
                    else
                        Format($"{Ind14}This page will not function without selecting a version log to generate.", ForECol.Incorrection);
                    Pause();

                    exitGenSteamLogSubPageQ = true;
                }

            } while (!exitGenSteamLogSubPageQ);
        }


        static string formatterLanguageVersion = "fl.v1";
        // public, so we can test them easily... wait, maybe not, too many dependencies... okay..
        /// <summary>The house of steam format editing; all formatting tools meet here. This is the page where display meets function.</summary>
        static void FormattingEditor()
        {
            /** FORMATTING PLANNING, PROBABLY A CRAP TONNE
            -------------------------------------- 
            Sec1 - THE FORMATTER LANGUAGE
            -----
            Language direction, behavior, properties
                - Functional Classes :: SFormatterHandler.cs, SFormatterData.cs
                    SFormatterHandler.cs - acts as the IDE system, the brains
                    SFormatterData.cs - storage and live changes of format editing
                - Dependencies :: requires instance for information from library (seleted version log) [struct SFormatterLibRef.cs].
                - A functional-oriented language. (A fixed set of data and need procedures to work with them)
                - The language will be tailored towards the data available within the resource library and formatting rules of steam
                - The language parses in linear fashion, from first line to last line without skipping or returning
                - Case-sensitive language
                - No expressive error handling (ei, crashes) on use; provides error messages with syntax issues during edit. 

            Language Syntax
            > General
                Handled by SFormatterHandler.cs
                syntax          outcome
                _________________________________
                // abc123       line comment. Must be placed at beginning of line
                abc123          code
                "abc123"        plain text
                &00;            plain text '"'
                {abc123}        library reference
                $abc123         steam formatting reference
                if # = #:       keyword, control; compares two given values to be equal. Prints following line if condition is true (values are equal). Placed at start of line.
                else:           keyword, control; Prints following line if the condition of an immediately preceding 'if # = #' is false (values are not equal). Placed at start of line.
                repeat #:       keyword, control; repeats line '#' times incrementing from one to given number '#'. Any occuring '#' in following line is replaced with this incrementing number. Placed at start of line.
            

            > Specific to LIBRARY
                Provided by SFormatterLibRef.cs. Handled by SFormatterHandler.cs
                - Sources information from resource library via targetted "Log Version"

                syntax              outcome
                _________________________________
                {Version}           single; fetches version numbers only (ex 1.00)
                {AddedCount}        single; fetches number of added items
                {Added:#,prop}      array; access property from one-based Added entry '#'. Properties: name, ids
                {AdditCount}        single; fetches number of additional items
                {Addit:#,prop}      array; access property from one-bassed Additional entry '#'. Properties: ids, optionalName, relatedID, relatedContent
                {TTA}               single; fetches the number of total textures/contents added
                {UpdatedCount}      single; fetches the number of updated items
                {Updated:#,prop}    array; access property from one-based Updated entry '#'. Properties: id, name, changeDesc
                {LegendCount}       single; fetches the number of legends used in version log
                {Legend:#,prop}     array; access property from one-based Legend entry '#'. Properties; key, definition, keyNum
                {SummaryCount}      single; fetches the number of summaries in given version log
                {Summary:#}         array; access summary part from one-based Summary entry '#'


            > Specific to STEAM FORMAT RULES
                Handled by SFormatterHandler.cs
                - https://steamcommunity.com/comment/Guide/formattinghelp
                - Applies to a line, must be at the beginning of line. Any plain text must follow the 'plain text' syntax ("")

                syntax          outcome
                _________________________________
                $nl             next line / new line
                $h              header text
                $b              bold text
                $u              underline
                $i              italics
                $s              strikethrough text
                $sp             spoiler text
                $np             no parse, doesn't parse format tags
                $hr             horizontal rule; On its own line
                $url=abc:abc    website link (url={link}:{linkName})
                $list[or]       starts/ends a list (or - ordered); On its own line
                $*              ^ list item
                $q="abc":"abc"  quoted text (q={author}:{quotedText})
                $c              code text; fixed witdth font, preserves space
                $table[nb,ec]   starts/ends a table (nb - no border, ec - equal cells); On its own line
                $th="abc","abc" ^ adds a table header row. Separate columns with ','
                $td="abc","abc" ^ adds a table row. Separate columns with ','
                


            --------------------------------------
            Sec2 - ACTING AS AN IDE
            -----
            > Interface and Editing
                - The formatting language interface simply composes of the code view and the editor bar
                    . The code view is where all the formatting code is contained. This viewer extends to a finite length, which means the lines of code is limited to a certain number (based on console buffer)
                    . the editor bar is a navigation bar that takes input from the user to make changes to lines of code in the code view
                - Editing
                    . Editing starts with the editor bar. This bar is a simple prompting system with a syntax to make edits to the code view
                    . Syntaxes: 
                        {Add/Edit/Delete}{LineNumber} - to manipulate code view line
                            . Add# - Adds an empty line of code after line number '#'. If no number is provided, adds the line after the last line of code
                            . Edit# - Allows making edits to line number '#'. When editing, a text area opens below the line to edit. The user enter the line of code there (see 'Editing Area Concept'). After making edits, the user may press enter. The user may also enter '!help' to view formatting language syntax
                            . Delete# - Removes the line of code on line number '#'.
                        {</>} - to undo or redo history actions 
                            . < - Undos an edit or action. 
                            . > - Redos an edit or action. 
                            NOTE :: the history buffer is limited, up to 25 actions or edits can be undone/redone. Any undo or redo will display the history list.

            > Color coding
                - Color codes apply to every line of code after the user has finished editing.
                - Color coding colors and their typing
                    data type       foreEcolor          nativeColor    
                    ------------------------------------------------
                    comment         Accent              DarkGray
                    escape          Accent              Gray
                    operator        Accent              Gray
                    code            Normal              White
                    keyword         Correction          Magenta
                    plain text      Input               Yellow
                    reference       Highlight           Blue
                    error (msg)     Incorrection        Red

            > Error checking and messaging
                - Error checking occurs where syntax is to be followed and an argument is expected.
                    . Examples where error checking occurs is for code, proper references, keywords and their placementse
                    . Any line that starts with a comments remains unparsed; no error checking, it is ignorable
                - Error messaging occurs where expected syntax is not followed
                    . Each error message will be identified with a unique three-digit number and a token signifitying the type of error: G - General, R - Reference (Library and Steam Format specific). Every error has a unique four-digit number regardless of type. 'G001' and 'R001' cannot simultaneously exist, they are both '001'
                    . Each error message will be displayed below the line of code with those errors (see 'Editing Area Concept')
            
            > Data-saving
                - Formatting profiles are auto-saved when the user exits to the main menu page. 
                - There will be 2 formatting profiles for the user. These formatting profiles allow the user options to format without redoing a formatting preference (often). These profiles can be given a name. When saved to file, these profiles are identified with a common tag followed by the number '1' or '2'
                - Only the formatting code is saved to file. Editor cache such as the history is not saved, but instead persists until the application closes.


            > Some visual concepts
                - Code View and Editor Bar concept
                    ...........................................................
                    Formatting #1 - New Formatting 1
                    ---------------------------
                    L1 "hello world"
                    L2 
                    --------------------------
                    Enter '!help' to learn editing commands
                    Command >> ___
                    ...........................................................
                
                - Editing Area Concept
                    ...........................................................
                    L3 $h"Untitled"
                    L4 "Mustache" $i
                        --------------------------------------
                        Editing Line 4 -- [Enter] :: end edits. '!help' - formatter language help.
                        >> ___

                        --------------------------------------
                        R0023: Missing argument after steam format reference '$i' to italicize.
            
                    L5 "That thing is a mustache?""
                        G005  G003
                    ...........................................................

                - History List Concept
                    The history list layout displays the most recent action to the oldes action from top to bottom; top is recency
                    Actions that have been done or redone are shown in 'Normal' color, the current history action is in 'Highlight' with an arrow pointing at it, and undone actions are in 'Accent'.
                    ...........................................................
                    L19 """there was a great explosion"""
                    L20 """nothing was left after the smoke cleared"""
                    ----------------------------
                    Enter '!help' to learn editing commands
                    Command >> <
                    
                    HISTORY LIST (in order of recency)
                    ----------------------------------
                    H1 | Edited Line 20
                    H2 | Deleted Line 21
                    H3 | -> Edited Line 20
                    H4 | Added Line 21
                    H5 | Added Line 20
                    H6 | Edited Line 19
                    H7 | Added Line 19
                    ...........................................................

                - Formatting Syntax Help Concept
                    Color coding will also be displayed here
                    ...........................................................
                    GENERAL SYNTAX
                    Syntax             | Outcome
                    -------------------|-----------------------------------------
                    //                 | Comment; Anything written after these are imparsable.
                    abc123             | Code; Is parsable and subject to error checking.
                    "abc123"           | Plain text. Strings that will display after generation.
                    {abc123}           | Library reference (see LIBRARY REFERENCE SYNTAX)
                    ...........................................................

             **/

            isUsingFormatterNo1Q = _formatterData.EditProfileNo1Q;
            const char divCodeView = '-', divHelpChar = ':', divEditingArea = '.';
            const string editorHelpPhrase = "!help"; 
            const string editorCmdAdd = "add", editorCmdEdit = "edit", editorCmdDelete = "delete", editorCmdUndo = "<", editorCmdRedo = ">", editorCmdRename = "rename", editorCmdCopy = "copy", editorCmdAppender = "~~", editorCmdGroup = "group";
            const string multiHistKey = "\n\n\n", histNLRep = SFormatterHistory.histNLRep;
            bool exitFormatEditorQ = false, formatterIsSetupQ = false, showHistoryQ = false;

            // <lineMaximum> DBG"10"   OG"(int)(PageSizeLimit * 0.4f)"  //      <lineMaxWarn...> DBG"lineMaximum"    OG"10"
            const int noLineToEdit = -1, lineMaximum = (int)(PageSizeLimit * 0.4f), lineMaxWarnThreshold = 10; 
            const int historyLimit = 25, historyActionInitial = 1;
            int lineToEdit = noLineToEdit, lineToEditSpan = HSNL(3, 7).Clamp(0, 5), countCycles = 0, historySpan = HSNL(1, 5).Clamp(1, 3);

            Program.LogState($"Generate Steam Log|Steam Formatter|Formatting Editor {formatterLanguageVersion}");
            Dbug.StartLogging();
            Dbug.Log($"RECORDING :: Formatting Editor Actions and processes -- FProfile#1? {isUsingFormatterNo1Q}");
            Dbug.NudgeIndent(true);

            List<SFormatterHistory> _editorHistory;
            int historyActionNumber;
            if (isUsingFormatterNo1Q)
            {
                if (_editorHistory1 == null)
                    _editorHistory1 = new List<SFormatterHistory>();

                if (!_editorHistory1.HasElements())
                {
                    historyActionNumber1 = historyActionInitial;
                    _editorHistory1.Add(new SFormatterHistory("Opened Formatter", "--", "--"));
                    Dbug.Log($"Added 'open formatter' history to history list; Initialized history action number; ");
                }

                _editorHistory = _editorHistory1;
                historyActionNumber = historyActionNumber1;
            }
            else
            {
                if (_editorHistory2 == null)
                    _editorHistory2 = new List<SFormatterHistory>();

                if (!_editorHistory2.HasElements())
                {
                    historyActionNumber2 = historyActionInitial;
                    _editorHistory2.Add(new SFormatterHistory("Opened Formatter", "--", "--"));
                    Dbug.Log($"Added 'open formatter' history to history list; Initialized history action number; ");
                }

                _editorHistory = _editorHistory2;
                historyActionNumber = historyActionNumber2;
            }

            // THE EDITOR
            do
            {
                Dbug.Log($"## Start Cycle {countCycles + 1};");
                Dbug.NudgeIndent(true);
                if (_formatterData != null)
                    formatterIsSetupQ = _formatterData.IsSetup();

                /** code view and editor bar snippets
                 > Interface and Editing
                    - The formatting language interface simply composes of the code view and the editor bar
                        . The code view is where all the formatting code is contained. This viewer extends to a finite length, which means the lines of code is limited to a certain number (based on console buffer)
                        . the editor bar is a navigation bar that takes input from the user to make changes to lines of code in the code view
                    - Editing
                        . Editing starts with the editor bar. This bar is a simple prompting system with a syntax to make edits to the code view
                        . Syntaxes: 
                            {Add/Edit/Delete}{LineNumber} - to manipulate code view line
                                . Add# - Adds an empty line of code after line number '#'. If no number is provided, adds the line after the last line of code
                                . Edit# - Allows making edits to line number '#'. When editing, a text area opens below the line to edit. The user enter the line of code there (see 'Editing Area Concept'). After making edits, the user may press enter. The user may also enter '!help' to view formatting language syntax
                                . Delete# - Removes the line of code on line number '#'.
                            {</>} - to undo or redo history actions 
                                . < - Undos an edit or action. 
                                . > - Redos an edit or action. 
                                NOTE :: the history buffer is limited, up to 25 actions or edits can be undone/redone. Any undo or redo will display the history list.
                
                
                    - Code View and Editor Bar concept
                        ...........................................................
                        Formatting #1 - New Formatting 1
                        ---------------------------
                        L1 "hello world"
                        L2 
                        --------------------------
                        Enter '!help' to learn editing commands
                        Command >> ___
                        ...........................................................      
                

                    - History List Concept
                        The history list layout displays the most recent action to the oldes action from top to bottom; top is recency
                        Actions that have been done or redone are shown in 'Normal' color, the current history action is in 'Highlight' with an arrow pointing at it, and undone actions are in 'Accent'.
                        ...........................................................
                        L19 """there was a great explosion"""
                        L20 """nothing was left after the smoke cleared"""
                        ----------------------------
                        Enter '!help' to learn editing commands
                        Command >> <
                    
                        HISTORY LIST (in order of recency)
                        ----------------------------------
                        H1 | Edited Line 20
                        H2 | Deleted Line 21
                        H3 | -> Edited Line 20
                        H4 | Added Line 21
                        H5 | Added Line 20
                        H6 | Edited Line 19
                        H7 | Added Line 19
                        ...........................................................
                 */

                Clear();
                Title("Formatting Editor", subMenuUnderline, 2);

                string commandPrompt = null, appenderHistNamePart = null;

                // ++   CODE VIEW   ++
                /// header
                if (_formatterData != null)
                    FormatLine($"Formatting #{(isUsingFormatterNo1Q? "1" : "2")} - {(isUsingFormatterNo1Q ? _formatterData.Name1 : _formatterData.Name2)}");
                else FormatLine($"Formatting #{(isUsingFormatterNo1Q? "1" : "2")} - New Formatting", ForECol.Heading1);
                /// code view area (IF actual ELSE placeholder)
                HorizontalRule(divCodeView);                
                if (formatterIsSetupQ)
                {
                    List<string> formatToDisplay = isUsingFormatterNo1Q ? _formatterData.LineData1 : _formatterData.LineData2;
                    if (formatToDisplay.HasElements())
                    {
                        /** editing area sinppets
                         - Editing Area Concept
                            ...........................................................
                            L3 $h"Untitled"
                            L4 "Mustache" $i
                                --------------------------------------
                                Editing Line 4 -- [Enter] :: end edits. '!help' - formatter language help.
                                >> ___

                                --------------------------------------
                                R0023: Missing argument after steam format reference '$i' to italicize.
            
                            L5 "That thing is a mustache?""
                                G0005: Unexpected token '""'. Plain text is missing end token '"'.
                            ...........................................................


                         - Formatting Syntax Help Concept
                            Color coding will also be displayed here
                            ...........................................................
                            GENERAL SYNTAX
                            Syntax             | Outcome
                            -------------------|-----------------------------------------
                            //                 | Comment; Anything written after these are imparsable.
                            abc123             | Code; Is parsable and subject to error checking.
                            "abc123"           | Plain text. Strings that will display after generation.
                            {abc123}           | Library reference (see LIBRARY REFERENCE SYNTAX)
                            ...........................................................

                         */

                        // Editing Area (part 1)
                        bool makingEdit = lineToEdit != noLineToEdit;
                        int groupStartLine = 0;
                        CheckSyntax(formatToDisplay.ToArray());
                        for (int lx = 0; lx < formatToDisplay.Count; lx++)
                        {
                            int lineNumber = lx + 1;
                            bool isLineToEditQ = lineToEdit == lineNumber;
                            bool displayLines = !makingEdit || (makingEdit && lineNumber.IsWithin(lineToEdit - lineToEditSpan, lineToEdit + lineToEditSpan));
                            bool spanEdge = makingEdit && lineNumber.IsWithin(lineToEdit - lineToEditSpan - 1, lineToEdit + lineToEditSpan + 1);

                            bool isInGroupQ = _formatterData.IsLineInGroup(lineNumber, out string groupName, out int pos, out bool expandedQ);
                            bool collapseGroupQ = isInGroupQ && !expandedQ && !makingEdit;

                            /// group expanded top region tag
                            if (isInGroupQ && pos == 1)
                            {
                                groupStartLine = lineNumber;
                                if (!collapseGroupQ)
                                {
                                    if (displayLines || spanEdge)
                                        FormatLine($"{Ind24}--  Group '{groupName}'  -- ", ForECol.Accent);
                                }
                            }

                            /// group inner-lines tag
                            string innerGroupKey = "";
                            if (isInGroupQ && !collapseGroupQ)
                                innerGroupKey = $"{groupName[0]}| ";

                            /// line displays
                            if (!collapseGroupQ)
                            {
                                if (displayLines)
                                {
                                    Format($"L{lx + 1,-3} {innerGroupKey}", ForECol.Accent);
                                    ColorCode(formatToDisplay[lx], _formatterData.UseNativeColorCodeQ, true);
                                }
                                else if (spanEdge)
                                    FormatLine($"L{lx + 1,-3} {innerGroupKey}...", ForECol.Accent);
                            }


                            /// the editing area
                            if (makingEdit && isLineToEditQ)
                            {
                                HorizontalRule(divEditingArea);
                                FormatLine($"Editing Line {lineNumber} -- [Enter] to end edits. '!help' for formatter language help.", ForECol.Accent);
                                Format(">> ");
                                GetCursorPosition();
                                NewLine(3);
                                HorizontalRule(divEditingArea);
                            }

                            /// error displays
                            SFormatterInfo[] linErrors = GetErrors(lineNumber);
                            if (linErrors.HasElements() && displayLines && !collapseGroupQ)
                                for (int ex = 0; ex < linErrors.Length; ex++)
                                {
                                    SFormatterInfo error = linErrors[ex];

                                    /// IF editing this line: Expand errors with code and messages; ELSE show only error codes below line
                                    if (isLineToEditQ && makingEdit)
                                        ColorCode($"{Ind34}{error.errorCode} {error.errorMessage}", _formatterData.UseNativeColorCodeQ, true, true);
                                    else
                                    {
                                        if (ex == 0)
                                            Format(Ind34);
                                        ColorCode($"{error.errorCode}{Ind24}", _formatterData.UseNativeColorCodeQ, false, true);
                                        if (ex + 1 == linErrors.Length)
                                            NewLine();
                                    }
                                }

                            /// group expanded bottom region tag \ group collapsed tag
                            if (isInGroupQ && pos == -1)
                            {
                                if (!collapseGroupQ)
                                {
                                    if (displayLines || spanEdge)
                                        FormatLine($"{Ind24}--  END Group '{groupName}'  --", ForECol.Accent);
                                }
                                else
                                {
                                    int countErrors = 0;
                                    for (int glx = groupStartLine; glx <= lineNumber; glx++)
                                    {
                                        SFormatterInfo[] getLineErrors = GetErrors(glx);
                                        if (getLineErrors.HasElements())
                                            countErrors += getLineErrors.Length;
                                    }
                                    string groupErrors = "";
                                    if (countErrors != 0)
                                        groupErrors = $"({countErrors} error{ConditionalText(countErrors == 1, "", "s")})";
                                    FormatLine($"{Ind24}====  Group '{groupName}' {groupErrors} ====", ForECol.Accent);
                                }
                            }
                        }


                        // Editing Area (part 2)
                        if (makingEdit)
                        {
                            SetCursorPosition();

                            string editInput = StyledInput("___");
                            /// IF value: open format syntax help -OR- make edit; ELSE leave edit as is
                            if (editInput.IsNotNEW())
                            {
                                /// formatting syntax help
                                if (editInput.Equals(editorHelpPhrase))
                                {
                                    Dbug.Log("Editing Area; Formatting Language Syntax Help openned; ");
                                    Clear();
                                    Title("Formatting Language Syntax", subMenuUnderline, 2);

                                    Table2Division divStyle = WSLL(0, 2) == 2 ? Table2Division.KCTiny : Table2Division.KCSmall;
                                    const char nxt = '%'; // used to force parts of table data into parts (Outcome side only)
                                    TableRowDivider(true);
                                    TableRowDivider(divCodeView, true, GetPrefsForeColor(ForECol.Accent));

                                    // the great help page info
                                    string[,] syntaxes = new string[,]
                                    {
                                        // GENERAL
                                        /** Language sytnax snippet (General)
                                        Language Syntax
                                        > General
                                            Handled by SFormatterHandler.cs
                                            syntax          outcome
                                            _________________________________
                                            // abc123       line comment. Must be placed at beginning of line
                                            abc123          code
                                            "abc123"        plain text
                                            &00;            plain text '"'
                                            {abc123}        library reference
                                            $abc123         steam formatting reference
                                            if # = #;       keyword, control; compares two given values to be equal. Prints following line if condition is true (values are equal). Placed at start of line.
                                            else;           keyword, control; Prints following line if the condition of a preceding 'if # = #' is false (values are not equal). Placed at start of line.
                                            repeat #;       keyword, control; repeats line '#' times incrementing from one to given number '#'. Any occuring '#' in following line is replaced with this incrementing number. Placed at start of line.
                                        */
                                        { "GENERAL SYNTAX", null},
                                        { null, $"This functional language is case-sensitive.\nA value describes any input that derives from: a number, plain text, or (the property of) a library reference."},
                                        { "// text",    "Line comment. Must be placed at the start of the line. Commenting renders a line imparsable."},
                                        { "text",       "Code. Anything that is not commented is code and is parsable on steam log generation."},
                                        { "\"text\"",   "Plain text. Represents any text that will be parsed into the generated steam log."},
                                        { "&##;",       $"Escape character. Used within plain text to print a syntax-regulated character.{nxt}Use '&00;' for double quote character (\"). Use '&01;' for hashtag / pound symbol (#)."},
                                        { "{text}",     $"Library reference. References a value based on the information received from a submitted version log.{nxt}Refer to 'Library Reference' below for more information."},
                                        { "$text",      $"Steam format reference. References a styling element to use against plain text or another value when generating steam log.{nxt}Refer to 'Steam Format References' below for more information."},
                                        { "if # = #;",  $"Keyword. Must be placed at the start of the line.{nxt}A control command that compares two values for a true or false condition. If the condition is 'true' then the line's remaining data will be parsed into the formatting string.{nxt}Operators and their comparisons:|Equal to '=', Unequal to '!='|Greater than '>', Lesser than '<'|Greater than or equal to '>='|Lesser than or equal to '<='".Replace("|", $"{nxt}{Ind14}")},
                                        { "else;",      $"Keyword. Must be placed at the start of the line. Must be placed following an 'if' keyword line.{nxt}A control command that will parse the line's remaining data when the condition of a preceding 'if' command is false."},
                                        { "repeat #;",  $"Keyword. Must be placed at the start of the line.{nxt}A control command that repeats a line's remaining data '#' number of times. An incrementing number from one to given number '#' will replace any occuring '#' in the line's remaining data."},
                                        { "jump #;",    $"Keyword. Can only be placed following an 'if' or 'else' keyword.{nxt}A control command the allows skipping ahead to a given line. Only direct numbers are accepted as a value. Providing a line number beyond final line will skip all remaining lines."},
                                        { "next;", $"Keyword. Can only be placed following an 'if', 'else', or 'repeat' keyword.{nxt}A control command that subtitutes the next line as the current line. The next line may not contain any keywords.{nxt}Keyword lines ending with this keyword may be placed within table blocks and list blocks (see 'Steam Format References')."},


                                        // LIBRARY
                                        /** Language sytnax snippet (Library)
                                        Language Syntax
                                        > Specific to LIBRARY
                                            Provided by SFormatterLibRef.cs. Handled by SFormatterHandler.cs
                                            - Sources information from resource library via targetted "Log Version"

                                            syntax              outcome
                                            _________________________________
                                            {Version}           single; fetches version numbers only (ex 1.00)
                                            {AddedCount}        single; fetches number of added items
                                            {Added:#,prop}      array; access property from one-based Added entry '#'. Properties: name, ids
                                            {AdditCount}        single; fetches number of additional items
                                            {Addit:#,prop}      array; access property from one-bassed Additional entry '#'. Properties: ids, optionalName, relatedID, relatedContent
                                            {TTA}               single; fetches the number of total textures/contents added
                                            {UpdatedCount}      single; fetches the number of updated items
                                            {Updated:#,prop}    array; access property from one-based Updated entry '#'. Properties: id, name, changeDesc
                                            {LegendCount}       single; fetches the number of legends used in version log
                                            {Legend:#,prop}     array; access property from one-based Legend entry '#'. Properties; key, definition, keyNum
                                            {SummaryCount}      single; fetches the number of summaries in given version log
                                            {Summary:#}         array; access summary part from one-based Summary entry '#'

                                     */
                                        { "LIBRARY REFERENCES", null},
                                        { null, $"Library reference values are provided by information obtained from the version log submitted for steam log generation.\nValues returned from library references are as plain text. The value '{SFormatterLibRef.ValIsNE}' is returned when a given reference has no value."},
                                        { "{Version}",          "Value. Gets the log version number (ex 1.00)."},
                                        { "{AddedCount}",       "Value. Gets the number of added item entries available."},
                                        { "{Added:#,prop}",     $"Value Array. Gets value 'prop' from one-based added entry number '#'.{nxt}Values for 'prop': ids, name."},
                                        { "{AdditCount}",       "Value. Gets the number of additional item entries available."},
                                        { "{Addit:#,prop}",     $"Value Array. Gets value 'prop' from one-based additional entry number '#'.{nxt}Values for 'prop': ids, optionalName, relatedContent, relatedID.{nxt}Property 'optionalName' may not have a value."},
                                        { "{TTA}",              "Value. Gets the number of total textures/contents added."},
                                        { "{UpdatedCount}",     "Value. Gets the number of updated item entries available."},
                                        { "{Updated:#,prop}",   $"Value Array. Gets value 'prop' from one-based updated entry number '#'.{nxt}Values for 'prop': changeDesc, id, relatedContent."},
                                        { "{LegendCount}",      "Value. Gets the number of legend entries available."},
                                        { "{Legend:#,prop}",    $"Value Array. Gets value 'prop' from one-based legend entry number '#'.{nxt}Values for 'prop': definition, key"},
                                        { "{SummaryCount}",     "Value. Gets the number of summary parts available."},
                                        { "{Summary:#}",        "Value Array. Gets the value for one-based summary part number '#'."},


                                        // STEAM FORMAT
                                        /** Language sytnax snippet (Steam format)
                                        Language Syntax
                                        > Specific to STEAM FORMAT RULES
                                            Handled by SFormatterHandler.cs
                                            - https://steamcommunity.com/comment/Guide/formattinghelp
                                            - Applies to a line, must be at the beginning of line. Any plain text must follow the 'plain text' syntax ("")

                                            syntax          outcome
                                            _________________________________
                                            $nl             next line / new line
                                            $h              header text
                                            $b              bold text
                                            $u              underline
                                            $i              italics
                                            $s              strikethrough text
                                            $sp             spoiler text
                                            $np             no parse, doesn't parse format tags
                                            $hr             horizontal rule; On its own line
                                            $url=abc:abc    website link (url={link}:{linkName})
                                            $list[or]       starts/ends a list (or - ordered); On its own line
                                            $*              ^ list item
                                            $q="abc":"abc"  quoted text (q={author}:{quotedText})
                                            $c              code text; fixed witdth font, preserves space
                                            $table[nb,ec]   starts/ends a table (nb - no border, ec - equal cells); On its own line
                                            $th="abc","abc" ^ adds a table header row. Separate columns with ','
                                            $td="abc","abc" ^ adds a table row. Separate columns with ',' 

                                        */
                                        { "STEAM FORMAT REFERENCES", null},
                                        { null, $"Steam format references are styling element calls that will affect the look of any text or value placed after it on log generation.\nSimple command references may be combined with other simple commands unless otherwise unpermitted.\nComplex commands require a text or value to be placed in a described parameter surrounded by single quote characters (')."},
                                        /// simple
                                        { "$h",     $"Simple command. Header text. Must be placed at the start of the line. May not be combined with other simple commands.{nxt}There are three levels of header text. The header level follows the number of 'h's in reference. Example, a level three header text is '$hhh'."},
                                        { "$b",     "Simple command. Bold text."},
                                        { "$u",     "Simple command. Underlined text."},
                                        { "$i",     "Simple command. Italicized text."},
                                        { "$s",     "Simple command. Strikethrough text."},
                                        { "$sp",    "Simple command. Spoiler text."},
                                        { "$np",    "Simple command. No parse. Doesn't parse steam format tags when generating steam log."},
                                        { "$c",     "Simple command. Code text. Fixed width font, preserves space."},
                                        { "$hr",    "Simple command. Horizontal rule. Must be placed on its own line. May not be combined with other simple commands."},
                                        { "$nl",    "Simple command. New line. Does not require a value."},
                                        { "$d",     $"Simple command. Indent. Does not require a value.{nxt}There are four indentation levels which relates to the number of 'd's in reference. Example, a level 2 indent is '$dd'.{nxt}An indentation is the equivalent of two spaces (' 'x2)."},
                                        { "$r",    "Simple command. Regular. Used to negate the effects of preceding simple commands."},
                                        /// complex
                                        { "$url= 'link':'name'",     $"Complex command. Must be placed on its own line.{nxt}Creates a website link by using URL address 'link' to create a hyperlink text described as 'name'."},
                                        { "$list[or]",              $"Complex command. Must be placed on its own line.{nxt}Starts a list block. The optional parameter within square brackets, 'or', will initiate an ordered (numbered) list. Otherwise, an unordered list is initiated."},
                                        { "$*",                     $"Simple command. Must be placed at the start of the line.{nxt}Used within a list block to create a list item. Simple commands may follow to style the list item value or text."},
                                        { "$q= 'author':'quote'",    $"Complex command. Must be placed on its own line.{nxt}Generates a quote block that will reference an 'author' and display their original text 'quote'."},
                                        { "$table[nb,ec]",          $"Complex command. Must be placed on its own line.{nxt}Starts a table block. There are two optional parameters within square brackets: parameter 'nb' will generate a table with no borders, parameter 'ec' will generate a table with equal cells."},
                                        { "$th= 'clm1','clm2'",      $"Complex command. Must be placed on its own line.{nxt}Used within a table block to create a table header row. Separate multiple columns of data with ','. Must follow immediately after a table block has started."},
                                        { "$td= 'clm1','clm2'",      $"Complex command. Must be placed on its own line.{nxt}Used within a table block to create a table data row. Separate multiple columns of data with ','."},


                                        // SYNTAX EXCEPTIONS
                                        { "SYNTAX EXCEPTIONS", null},
                                        { "if # = #; if # = #;", "The keyword 'if' may follow after another 'if' keyword once more. The second 'if' may trigger a following 'else' keyword line." },
                                        { "else; if # = #;", "The keyword 'else' may precede the keyword 'if'. This 'if' keyword may trigger a following 'else' keyword line."},
                                        { "repeat#; if # = #;", "The keyword 'repeat' may precede the keyword 'if'. This 'if' keyword cannot trigger an 'else' keyword line."},
                                        { "else; if # = #; next;", "The keywords 'next' and 'jump' may follow after a second 'if' keyword."},


                                        // EDITING SUPPLEMENT
                                        { "EDITING SUPPLEMENT", null},
                                        { null, "The following only applies to the editing area when editing a line."},
                                        { editorCmdAppender, $"Appendder supplement. Allows adding or inserting code into the currently edited line. There are four functions:{nxt}Append the new edit to the end of the line: '*text'.{nxt}Insert the new edit to the start of the line: 'text*'.{nxt}Insert the new edit between two words within the line: 'word1*text*word2'.{nxt}Insert and replace occuring word in the line: 'word*text'.".Replace("*", editorCmdAppender)}
                                    };

                                    for (int i = 0; i < syntaxes.GetLength(0); i++)
                                    {
                                        string data1 = syntaxes[i, 0];
                                        string data2 = syntaxes[i, 1];

                                        /// pre-help printing setup
                                        if (data2.IsNEW())
                                        {
                                            /// section header   {"...", null}
                                            if (i != 0)
                                                NewLine(2);
                                            Important(data1);

                                            /// section additional info   {null, "..."}
                                            if ((i + 1).IsWithin(0, syntaxes.GetLength(0) - 1))
                                                if (syntaxes[i + 1, 0].IsNEW())
                                                {
                                                    FormatLine($"{Ind24}{syntaxes[i + 1, 1]}");
                                                    NewLine();
                                                }

                                            Table(divStyle, $"SYNTAX", divHelpChar, "OUTCOME");
                                        }

                                        /// help printing   {"...", "..."}
                                        if (data1.IsNotNE() && data2.IsNotNE())
                                        {
                                            /// IF data1 contains splitter key: outcome below;
                                            ///    keyData  |  data1 part1
                                            ///             |  data1 part2
                                            /// ELSE outcome below;
                                            ///    keyData  |  data1
                                            if (data2.Contains(nxt))
                                            {
                                                string[] data2Parts = data2.Split(nxt);
                                                if (data2Parts.HasElements())
                                                    for (int d2x = 0; d2x < data2Parts.Length; d2x++)
                                                    {
                                                        if (d2x == 0)
                                                        {
                                                            TableRowDivider(false);
                                                            Table(divStyle, $"{Ind14}{data1}", divHelpChar, data2Parts[d2x]);
                                                        }
                                                        else
                                                        {
                                                            if (d2x + 1 >= data2Parts.Length)
                                                                TableRowDivider(true);
                                                            Table(divStyle, null, divHelpChar, $"{Ind14}{data2Parts[d2x]}");
                                                        }
                                                    }
                                            }
                                            else 
                                                Table(divStyle, $"{Ind14}{data1}", divHelpChar, data2);
                                        }
                                    }
                                    TableRowDivider(false);

                                    Format("End of Formatting Language Syntax", ForECol.Accent);
                                    SetCursorPosition(1); /// just below title
                                    Pause();

                                    /// to bypass prompts and return to editing
                                    commandPrompt = $"{editorCmdEdit}{lineToEdit}";
                                }
                                
                                /// make edit
                                else
                                {
                                    /// appender functions
                                    if (editInput.Contains(editorCmdAppender))
                                    {
                                        Dbug.Log($"Appender used in editing area; Received input :: {editInput}");
                                        Dbug.LogPart("  >|");

                                        bool oneAppenderOnLineQ = editInput.CountOccuringCharacter(editorCmdAppender[0]) == editorCmdAppender.Length;
                                        string newEditInput = null, newEdit = "";
                                        string lineInfo = _formatterData.GetLine(lineToEdit);

                                        /// append after
                                        if (editInput.StartsWith(editorCmdAppender) && oneAppenderOnLineQ)
                                        {
                                            newEdit = editInput.Substring(editorCmdAppender.Length);
                                            Dbug.LogPart("Append After; ");
                                            if (newEdit.IsNotNE())
                                            {
                                                newEditInput = lineInfo + newEdit;
                                                appenderHistNamePart = "Append";
                                                Dbug.LogPart($"Resulting Edit :: '{lineInfo}' + '{newEdit}'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("No new edit to append");
                                                IncorrectionMessageQueue("There is no new edit to append to line");
                                            }
                                        }
                                        /// insert before
                                        else if (editInput.EndsWith(editorCmdAppender) && oneAppenderOnLineQ)
                                        {
                                            newEdit = editInput.Substring(0, editInput.Length - editorCmdAppender.Length);
                                            Dbug.LogPart("Insert Before; ");
                                            if (newEdit.IsNotNE())
                                            {
                                                newEditInput = newEdit + lineInfo;
                                                appenderHistNamePart = "Insert Before";
                                                Dbug.LogPart($"Resulting Edit :: '{newEdit}' + '{lineInfo}'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("No new edit to insert");
                                                IncorrectionMessageQueue("There is no new edit to insert before line");
                                            }
                                        }
                                        /// replace occurence
                                        else if (oneAppenderOnLineQ)
                                        {
                                            Dbug.LogPart("$Replace occurence within; ");
                                            string[] editParts = editInput.Split(editorCmdAppender);
                                            if (editParts.HasElements(2))
                                            {
                                                string word1 = editParts[0];
                                                newEdit = editParts[1];

                                                if (word1.IsNotNE() && newEdit.IsNotNE())
                                                {
                                                    Dbug.LogPart($"Replacing any '{word1}' with '{newEdit}'");

                                                    if (lineInfo.Contains(word1) && word1 != newEdit)
                                                    {
                                                        newEditInput = lineInfo.Replace(word1, newEdit);
                                                        appenderHistNamePart = "Replace";
                                                        Dbug.LogPart($"Resulting Edit :: '{lineInfo.Replace(word1, $"[{newEdit}]")}'");
                                                    }
                                                    else
                                                    {
                                                        if (word1 == newEdit)
                                                        {
                                                            Dbug.LogPart("Unnecessary replacement (word equals new edit)");
                                                            IncorrectionMessageQueue("Word to replace and new edit are the same");
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart($"Could not find placement '{word1}' within line");
                                                            IncorrectionMessageQueue($"Could not find word '{word1}' within line");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("Could not proceed to replace word with new edit");
                                                    if (word1.IsNotNE())
                                                        IncorrectionMessageQueue("There is no new edit to insert within line");
                                                    else IncorrectionMessageQueue("Missing identifiable word to replace with new edit");
                                                }
                                            }
                                        }
                                        /// insert between
                                        else if (editInput.CountOccuringCharacter(editorCmdAppender[0]) == 2 * editorCmdAppender.Length)
                                        {
                                            Dbug.LogPart($"Insert within; ");
                                            string[] editParts = editInput.Split(editorCmdAppender);
                                            if (editParts.HasElements(3))
                                            {
                                                string word1 = editParts[0], word2 = editParts[2];
                                                newEdit = editParts[1];

                                                if (word1.IsNotNE() && word2.IsNotNE() && newEdit.IsNotNE())
                                                {
                                                    Dbug.LogPart($"Parting words: after '{word1}' and before '{word2}'; ");
                                                    string[] lineParts = lineInfo.Split(word1 + word2);
                                                    if (lineParts.HasElements(2))
                                                    {
                                                        lineParts[0] += word1;
                                                        lineParts[1] = word2 + lineParts[1];

                                                        newEditInput = lineParts[0] + newEdit + lineParts[1];
                                                        appenderHistNamePart = "Insert Within";
                                                        Dbug.LogPart($"Resulting Edit :: '{lineParts[0]}' + '{newEdit}' + '{lineParts[1]}'");
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart($"Could not find placement '{word1 + word2}' within line");
                                                        IncorrectionMessageQueue($"Could not find phrase '{word1 + word2}' within line");
                                                    }
                                                }
                                                else
                                                {
                                                    if (newEdit.IsNotNE())
                                                    {
                                                        Dbug.LogPart("No parting words to identify placement within line");
                                                        if (word1.IsNE())
                                                            IncorrectionMessageQueue("Missing identifiable word to place new edit 'after'");
                                                        else IncorrectionMessageQueue("Missing identifiable word to place new edit 'before'");
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart("No new edit to insert between parting words");
                                                        IncorrectionMessageQueue("There is no new edit to insert within line");
                                                    }
                                                }
                                            }
                                            else Dbug.LogPart("Parting words and new edit could not be fetched");
                                        }
                                        /// what? what! do what!?
                                        else
                                        {
                                            Dbug.LogPart("Uncertain appender usage");
                                            IncorrectionMessageQueue($"Uncertain appender usage (plain text may not contain '{editorCmdAppender}')");
                                        }
                                        Dbug.Log("; ");

                                        /// giving user feedback for appender usage
                                        IncorrectionMessageTrigger($"{Ind24}Appender Issue: ", ".");
                                        IncorrectionMessageQueue(null);

                                        if (newEditInput.IsNotNE())
                                            editInput = newEditInput;
                                        else editInput = null;
                                    }

                                    /// IF got a valid editing command: send it; ELSE bypass prompts and return to editing (probs cause appender failed) 
                                    if (editInput != null)
                                        commandPrompt = $"{editorCmdEdit}{lineToEdit}\n{editInput}";
                                    else commandPrompt = $"{editorCmdEdit}{lineToEdit}";
                                }
                            }
                            else
                            {
                                commandPrompt = $"{editorCmdEdit}{lineToEdit}\n{editInput}";
                            }
                            lineToEdit = noLineToEdit;
                        }
                    }
                    else
                    {
                        Format("L-   ", ForECol.Accent);
                        ColorCode("// Add a new line to get started.", _formatterData.UseNativeColorCodeQ, true);
                    }
                }
                else
                {
                    Format("L-   ", ForECol.Accent);
                    ColorCode("\"hello world!\"", _formatterData.UseNativeColorCodeQ, true);
                    Format("L-   ", ForECol.Accent);
                    ColorCode("// Add a new line to get started.", _formatterData.UseNativeColorCodeQ, true);
                }
                HorizontalRule(divCodeView);



                // ++   HISTORY LIST   ++
                const bool enableHistoryExtraInfoQ = false, temp_alwaysShowHist = false, temp_ignoreHistSpan = false;
                if ((showHistoryQ || temp_alwaysShowHist) && _editorHistory.HasElements())
                {
                    /** history list snippet
                    - History List Concept
                        The history list layout displays the most recent action to the oldes action from top to bottom; top is recency
                        Actions that have been done or redone are shown in 'Normal' color, the current history action is in 'Highlight' with an arrow pointing at it, and undone actions are in 'Accent'.
                        ...........................................................
                        L19 """there was a great explosion"""
                        L20 """nothing was left after the smoke cleared"""
                        ----------------------------
                        Enter '!help' to learn editing commands
                        Command >> >
                    
                        HISTORY LIST (in order of recency)
                        ----------------------------------
                        H1 | Edited Line 20
                        H2 | Deleted Line 21
                        H3 | -> Edited Line 20
                        H4 | Added Line 21
                        H5 | Added Line 20
                        H6 | Edited Line 19
                        H7 | Added Line 19
                        ...........................................................
                    */

                    NewLine();
                    Title("HISTORY LIST", divCodeView);
                    for (int hx = 0; hx < _editorHistory.Count; hx++)
                    {
                        SFormatterHistory history = _editorHistory[hx];
                        int thisHistNum = hx + 1;
                        ForECol foreCol = thisHistNum == historyActionNumber ? ForECol.Highlight : (thisHistNum > historyActionNumber ? ForECol.Normal : ForECol.Accent);

                        if (thisHistNum.IsWithin(historyActionNumber - historySpan, historyActionNumber + historySpan) || temp_ignoreHistSpan)
                        {
                            FormatLine($"H{thisHistNum,-2}| {(thisHistNum == historyActionNumber ? "-> " : "")}{history.actionName}", foreCol);
                            if (Program.isDebugVersionQ && enableHistoryExtraInfoQ)
                            {
                                if (history.undoneCommand.Contains(multiHistKey))
                                {
                                    FormatLine($"{Ind34}UNDO {history.undoneCommand.Replace("\n", histNLRep)}", ForECol.Accent);
                                    FormatLine($"{Ind34}REDO {history.redoneCommand.Replace("\n", histNLRep)}", ForECol.Accent);
                                }
                                else
                                    FormatLine($"{Ind34}UNDO {history.undoneCommand.Replace("\n", histNLRep)}  |  REDO {history.redoneCommand.Replace("\n", histNLRep)}", ForECol.Accent);
                            }
                        }
                        else
                        {
                            if ((thisHistNum + 1 == historyActionNumber - historySpan) || (thisHistNum - 1 == historyActionNumber + historySpan))
                                FormatLine($"H{thisHistNum,-2}| ...", foreCol);
                        }
                    }
                    NewLine();
                }
                showHistoryQ = false;



                // ++   EDITOR BAR   ++
                string editorInput = null;
                if (commandPrompt.IsNE())
                {
                    FormatLine($"Enter '{editorHelpPhrase}' to learn editing commands. Enter any key to exit formatting editor.", ForECol.Accent);
                    Format($"Command >> ");
                    editorInput = StyledInput("___");
                }
                if (editorInput.IsNotNEW())
                {
                    Dbug.LogPart($"Editor Bar recieved input :: {editorInput}");

                    // editor help
                    string editorInputRaw = editorInput;
                    editorInput = editorInput.ToLower();
                    if (editorInput.Equals(editorHelpPhrase))
                    {
                        Dbug.LogPart(" // Editor commands help openned");
                        NewLine(3);
                        Important("Editor Commands");
                        
                        Table2Division divStyle = Table2Division.KCSmall;
                        TableRowDivider(true);
                        TableRowDivider(divCodeView, true, GetPrefsForeColor(ForECol.Accent));
                        Table(divStyle, "COMMAND", divHelpChar, "USAGE");

                        string[,] editorCmds = new string[,]
                        {
                            { $"{editorCmdRename} {{newName}}", "Allows renaming this formatting profile to value 'newName'." },
                            { $"{editorCmdAdd}#", $"Inserts a new line before line number '#'. No provided line number will add a new line to bottom of code view. Line limit of {lineMaximum}."},
                            { $"{editorCmdEdit}#", "Opens editing area on line number '#', allowing for an edit."},
                            { $"{editorCmdCopy}#,#", "Copies the line data of a given line to another line."},
                            { $"{editorCmdDelete}#", "Deletes line number '#'."},
                            { $"{editorCmdUndo}", $"Undoes an available editor action in history (limit of {historyLimit - 1})."},
                            { $"{editorCmdRedo}", $"Redoes an available editor action in history (limit of {historyLimit - 1})."},
                            { $"{editorCmdGroup}#,#,{{name}}", $"Allows labelling a sequence of lines with given value 'name'. Calling an existing group by 'name' will toggle its expansion. Name cannot contain '{DataHandlerBase.Sep}' character. To remove a group, precede the group name with '0,0'. Minimum size of '2' lines."},
                            { $"NOTE", "  BEWARE: Having groups too close to boundaries, too close to each other, or too small may unexpectedly and unrecoverably break or alter them. In worst of cases, a file reversion is your best recovery."}
                        };
                        for (int i = 0; i < editorCmds.GetLength(0); i++)
                            Table(divStyle, $"{Ind24}{editorCmds[i, 0]}", divHelpChar, editorCmds[i, 1]);
                        TableRowDivider(false);

                        Format("End of Editor Commands", ForECol.Accent);
                        Pause();
                    }

                    // command prompt / exit editor
                    else
                    {
                        int lineCount = isUsingFormatterNo1Q? 
                            (_formatterData.LineData1.HasElements()? _formatterData.LineData1.Count : 0) 
                            : (_formatterData.LineData2.HasElements()? _formatterData.LineData2.Count : 0);
                        const int lineMinimum = 1;
                        bool hasLinesQ = lineCount > 0, hasTooManyLinesQ = lineCount >= lineMaximum;
                        Dbug.LogPart($" // FYI; lineCount = {lineCount}; lineLimit = {lineMaximum}; // ");

                        /// rename command?
                        if (editorInputRaw.ToLower().StartsWith(editorCmdRename))
                        {
                            string subName = editorInputRaw.Substring(editorCmdRename.Length);
                            if (subName.IsNotNEW())
                                commandPrompt = editorInputRaw;
                            else IncorrectionMessageQueue("A new formatting profile name was not provided.");
                        }                            

                        /// add command?
                        else if (editorInput.StartsWith(editorCmdAdd))
                        {
                            if (hasTooManyLinesQ)
                                IncorrectionMessageQueue($"The line limit of '{lineMaximum}' has been reached.");
                            else if (int.TryParse(editorInput.Replace(editorCmdAdd, ""), out int addLineNum))
                            {
                                if (addLineNum.IsWithin(lineMinimum, lineCount) && hasLinesQ)
                                {
                                    //if (addLineNum == lineCount)
                                    //    commandPrompt = editorCmdAdd;
                                    //else commandPrompt = $"{editorCmdAdd}{addLineNum}";
                                    commandPrompt = $"{editorCmdAdd}{addLineNum}";

                                    if ((lineCount + 1).IsWithin(lineMaximum - lineMaxWarnThreshold, lineMaximum))
                                    {
                                        Format($"{Ind24}Nearing line limit ({lineMaximum - lineCount - 1} lines before limit)", ForECol.Warning);
                                        Wait(2.5f);
                                    }
                                }
                                else IncorrectionMessageQueue($"Line #{addLineNum} does not exist.");
                            }
                            else if (editorInput.Trim().Equals(editorCmdAdd))
                                commandPrompt = editorCmdAdd;
                            else IncorrectionMessageQueue("Line number was not a number.");
                        }

                        /// edit command?
                        else if (editorInput.StartsWith(editorCmdEdit))
                        {
                            if (int.TryParse(editorInput.Replace(editorCmdEdit, ""), out int editLineNum))
                            {
                                if (editLineNum.IsWithin(lineMinimum, lineCount) && hasLinesQ)
                                    commandPrompt = $"{editorCmdEdit}{editLineNum}";
                                else IncorrectionMessageQueue($"Line #{editLineNum} does not exist.");
                            }
                            else IncorrectionMessageQueue("Line number was not a number.");
                        }

                        /// copy command?
                        else if (editorInput.StartsWith(editorCmdCopy))
                        {
                            string numsInput = editorInput.Replace(editorCmdCopy, "").Trim();
                            if (numsInput.IsNotNE())
                            {
                                if (numsInput.Contains(",") && numsInput.CountOccuringCharacter(',') == 1)
                                {
                                    string[] splitNumsInput = numsInput.Split(',');
                                    if (int.TryParse(splitNumsInput[0], out int numToCopyFrom) && int.TryParse(splitNumsInput[1], out int numToCopyTo))
                                    {
                                        if (numToCopyFrom.IsWithin(lineMinimum, lineCount) && hasLinesQ)
                                        {
                                            if (numToCopyTo.IsWithin(lineMinimum, lineCount) && hasLinesQ)
                                            {
                                                if (numToCopyFrom != numToCopyTo)
                                                    commandPrompt = $"{editorCmdCopy}{numToCopyFrom},{numToCopyTo}";
                                                else IncorrectionMessageQueue("Cannot copy to the same line.");
                                            }
                                            else IncorrectionMessageQueue($"Line #{numToCopyTo} does not exist.");
                                        }
                                        else IncorrectionMessageQueue($"Line #{numToCopyFrom} does not exist.");
                                    }
                                    else IncorrectionMessageQueue("One or more of the line numbers were not a number.");
                                }
                                else
                                {
                                    if (numsInput.Contains(","))
                                        IncorrectionMessageQueue("Only one comma (,) may be used in this command.");
                                    else IncorrectionMessageQueue("This command requires two line numbers (missing ',' character).");
                                }
                            }
                            else IncorrectionMessageQueue("No line numbers were provided for this command.");
                        }

                        /// delete command?
                        else if (editorInput.StartsWith(editorCmdDelete))
                        {
                            if (int.TryParse(editorInput.Replace(editorCmdDelete, ""), out int deleteLineNum))
                            {
                                if (deleteLineNum.IsWithin(lineMinimum, lineCount) && hasLinesQ)
                                {
                                    /// check if about to destroy group; warn
                                    bool deleteAnyWayQ = true;
                                    if (_formatterData.IsLineInGroup(deleteLineNum, out string groupName, out int pos, out _))
                                    {
                                        bool onlyOtherLineInGroupQ = false;
                                        if (pos == 1 && _formatterData.IsLineInGroup(deleteLineNum + 1, out string groupName2, out int pos2, out _))
                                            onlyOtherLineInGroupQ = groupName == groupName2 && pos2 == -1;
                                        else if (pos == -1 && _formatterData.IsLineInGroup(deleteLineNum - 1, out string groupName3, out int pos3, out _))
                                            onlyOtherLineInGroupQ = groupName == groupName3 && pos3 == 1;

                                        if (onlyOtherLineInGroupQ)
                                        {
                                            NewLine();
                                            FormatLine($"{Ind34}Deleting Line {deleteLineNum} may unrecoverably destroy group '{groupName}'.", ForECol.Warning);
                                            Confirmation($"{Ind34}Continue to deleting Line {deleteLineNum}? ", false, out deleteAnyWayQ);
                                        }
                                    }     

                                    if (deleteAnyWayQ)
                                        commandPrompt = $"{editorCmdDelete}{deleteLineNum}";
                                }
                                else IncorrectionMessageQueue($"Line #{deleteLineNum} does not exist.");
                            }
                            else IncorrectionMessageQueue("Line number was not a number.");
                        }

                        /// undo / redo command?
                        else if (editorInput.Trim().Equals(editorCmdUndo) || editorInput.Trim().Equals(editorCmdRedo))
                        {
                            if (editorInput.Contains(editorCmdRedo))
                            {
                                if (historyActionNumber > historyActionInitial)
                                    commandPrompt = editorCmdRedo;
                                else IncorrectionMessageQueue("There is no action in history to redo.");
                            }
                            else if (editorInput.Contains(editorCmdUndo))
                            {
                                if (historyActionNumber < historyLimit && historyActionNumber < _editorHistory.Count)
                                    commandPrompt = editorCmdUndo;
                                else IncorrectionMessageQueue("There is no action in history to undo.");
                            }
                        }

                        /// group command?
                        else if (editorInputRaw.ToLower().StartsWith(editorCmdGroup))
                        {
                            /// 1st & 3rd function - create group
                            if (editorInputRaw.CountOccuringCharacter(',') == 2)
                            {
                                string[] groupParameters = editorInputRaw.Substring(editorCmdGroup.Length).Split(',');
                                if (int.TryParse(groupParameters[0], out int lineStart) && int.TryParse(groupParameters[1], out int lineEnd))
                                {
                                    string newGroupName = groupParameters[2];
                                    if (newGroupName.IsNotNEW())
                                    {
                                        newGroupName = newGroupName.Trim();
                                        if (!newGroupName.Contains(DataHandlerBase.Sep))
                                        {
                                            /// IF ...: 3rd function (remove); ELSE IF ...: 1st function (create)
                                            if (lineStart == 0 && lineStart == lineEnd)
                                            {
                                                if (_formatterData.GroupExists(newGroupName))
                                                    commandPrompt = $"{editorCmdGroup}{lineStart},{lineEnd},{newGroupName}";
                                                else IncorrectionMessageQueue($"A group named '{newGroupName}' does not exist.");
                                            }
                                            else if (lineStart.IsWithin(lineMinimum, lineCount) && lineEnd.IsWithin(lineMinimum, lineCount))
                                            {
                                                bool startLineInOtherGroupQ = _formatterData.IsLineInGroup(lineStart, out _, out _, out _);
                                                bool endLineInOtherGroupQ = _formatterData.IsLineInGroup(lineEnd, out _, out _, out _);
                                                if (!startLineInOtherGroupQ && !endLineInOtherGroupQ)
                                                {
                                                    if (lineStart < lineEnd && !_formatterData.GroupExists(newGroupName))
                                                        commandPrompt = $"{editorCmdGroup}{lineStart},{lineEnd},{newGroupName}";
                                                    else
                                                    {
                                                        if (lineStart < lineEnd)
                                                            IncorrectionMessageQueue($"A group with name '{newGroupName}' already exists.");
                                                        else IncorrectionMessageQueue($"Starting Line #{lineStart} must occur before Ending Line #{lineEnd}.");
                                                    }
                                                }
                                                else
                                                {
                                                    if (startLineInOtherGroupQ)
                                                        IncorrectionMessageQueue($"Line #{lineStart} is already within another group.");
                                                    else IncorrectionMessageQueue($"Line #{lineEnd} is already within another group.");
                                                }                                                
                                            }
                                            else
                                            {
                                                if (lineStart.IsWithin(lineMinimum, lineCount))
                                                    IncorrectionMessageQueue($"Line #{lineEnd} does not exist.");
                                                else IncorrectionMessageQueue($"Line #{lineStart} does not exist.");
                                            }
                                        }
                                        else IncorrectionMessageQueue($"Group name may not contain '{DataHandlerBase.Sep}' character.");
                                    }
                                    else IncorrectionMessageQueue("A group name was not provided.");
                                }
                                else IncorrectionMessageQueue("One or more line numbers were not a number.");
                            }
                            /// 2nd function - toggle group expansion
                            else
                            {
                                if (!editorInputRaw.Contains(','))
                                {
                                    string groupName = editorInputRaw.Substring(editorCmdGroup.Length);
                                    if (groupName.IsNotNEW())
                                    {
                                        groupName = groupName.Trim();
                                        if (_formatterData.GroupExists(groupName))
                                            commandPrompt = $"{editorCmdGroup} {groupName}";
                                        else IncorrectionMessageQueue($"A group named '{groupName}' does not exist.");
                                    }
                                    else IncorrectionMessageQueue("A group name was not provided.");
                                }
                                else IncorrectionMessageQueue("Group command requires only two commas (',').");
                            }
                        }

                        /// exit editor
                        else
                        {
                            NewLine();
                            Confirmation($"{Ind24}Exit the steam formatting editor? ", true, out bool yesNo);
                            if (yesNo)
                                exitFormatEditorQ = true;

                            Dbug.LogPart($" // Exiting editor? {exitFormatEditorQ}");
                        }

                        IncorrectionMessageTrigger($"{Ind24}Command syntax issue: ", null);
                        IncorrectionMessageQueue(null);
                    }

                    Dbug.Log("; ");
                }

                // Editor Command Execution
                if (commandPrompt.IsNotNEW())
                {
                    int lineCount = isUsingFormatterNo1Q ?
                            (_formatterData.LineData1.HasElements() ? _formatterData.LineData1.Count : 0)
                            : (_formatterData.LineData2.HasElements() ? _formatterData.LineData2.Count : 0);

                    string histName = null, histRedo = null, histUndo = null;
                    bool isHistoryActionQ;
                    int countHistoryCmdActions = 0;
                    List<string> historyCommandPrompts = new();
                    do
                    {
                        bool wasHistoryActionQ = false;
                        isHistoryActionQ = false;

                        int historyIndex = historyCommandPrompts.Count - countHistoryCmdActions;
                        if (historyCommandPrompts.HasElements() && countHistoryCmdActions > 0)
                        {
                            countHistoryCmdActions--;
                            commandPrompt = historyCommandPrompts[historyIndex];
                            wasHistoryActionQ = true;
                        }

                        Dbug.Log($"H@ix{countHistoryCmdActions} --> Recieved command prompt :: {commandPrompt.Replace("\n", histNLRep)};  //  FYI: lineCount = {lineCount}; historyCmdActsCount = {historyCommandPrompts.Count}; histCmdIndex = {historyIndex}; ");
                        Dbug.LogPart("  >|");

                        /// rename command
                        if (commandPrompt.StartsWith(editorCmdRename))
                        {
                            Dbug.LogPart("Rename Cmd --> ");
                            string subsName = commandPrompt.Substring(editorCmdRename.Length).Trim();

                            if (subsName.IsNotNE())
                            {
                                histName = "Renamed Formatting Profile";
                                histRedo = commandPrompt;
                                histUndo = $"{editorCmdRename} {(isUsingFormatterNo1Q ? _formatterData.Name1 : _formatterData.Name2)}";

                                if (isUsingFormatterNo1Q)
                                {
                                    if (_formatterData.Name2 == subsName)
                                    {
                                        _formatterData.Name1 = subsName + " 2";
                                        Dbug.LogPart($"Name already taken, adding ' 2'; ");
                                    }
                                    else _formatterData.Name1 = subsName;
                                    Dbug.LogPart($"Renamed FProf1 :: {_formatterData.Name1}");

                                }
                                else
                                {
                                    if (_formatterData.Name1 == subsName)
                                    {
                                        _formatterData.Name2 = subsName + " 2";
                                        Dbug.LogPart($"Name already taken, adding ' 2'; ");
                                    }
                                    else _formatterData.Name2 = subsName;
                                    Dbug.LogPart($"Renamed FProf2 :: {_formatterData.Name2}");
                                }
                            }
                            else
                            {
                                if (wasHistoryActionQ)
                                {
                                    if (isUsingFormatterNo1Q)
                                    {
                                        _formatterData.Name1 = null;
                                        Dbug.Log($"Unnamed FProf1 :: {_formatterData.Name1}");
                                    }
                                    else
                                    {
                                        _formatterData.Name2 = null;
                                        Dbug.Log($"Unnamed FProf2 :: {_formatterData.Name2}");
                                    }
                                }   
                                else Dbug.LogPart("name??");
                            }
                        }
                        /// add command
                        else if (commandPrompt.StartsWith(editorCmdAdd))
                        {
                            Dbug.LogPart("Add Cmd --> ");
                            string reEditLine = null;
                            if (commandPrompt.Contains("\n") && wasHistoryActionQ)
                            {
                                Dbug.LogPart("Re-edit action :: ");
                                string[] reEditParts = commandPrompt.Split("\n");
                                if (int.TryParse(reEditParts[0].Replace(editorCmdAdd, ""), out int reEditLineNum))
                                {
                                    commandPrompt = editorCmdAdd + reEditLineNum.ToString();
                                    Dbug.LogPart($"Rebuilt command prompt :: {commandPrompt} ");
                                    if (reEditParts[1].IsNotNE())
                                    {
                                        reEditLine = reEditParts[1];
                                        Dbug.LogPart($" -- Re-edit phrase :: {reEditLine} // ");
                                    }
                                    else Dbug.LogPart(" -- No re-edit phrase required // ");
                                }
                                else
                                {
                                    commandPrompt = editorCmdAdd;
                                    Dbug.LogPart($"Rebuilt command prompt :: {commandPrompt}");
                                    
                                    if (reEditParts[1].IsNotNE())
                                    {
                                        Dbug.LogPart($" -- Re-edit phrase :: {reEditLine} // ");
                                        reEditLine = reEditParts[1];
                                    }
                                    else Dbug.LogPart(" -- No re-edit phrase required // ");
                                }
                                Dbug.Log("; ");
                                Dbug.LogPart("Add Cmd 2 --> ");
                            }

                            if (int.TryParse(commandPrompt.Replace(editorCmdAdd, ""), out int insLineNum))
                            {
                                _formatterData.AddLine(insLineNum, reEditLine);
                                Dbug.LogPart($"Insert new before line #{insLineNum}");
                                histName = $"Insert Before Line {insLineNum}";
                                histRedo = commandPrompt;
                                histUndo = $"{editorCmdDelete}{insLineNum}";
                            }
                            else
                            {
                                histName = $"Add Line {lineCount + 1}";
                                histRedo = commandPrompt;
                                histUndo = $"{editorCmdDelete}{lineCount + 1}";

                                _formatterData.AddLine(null, reEditLine);
                                Dbug.LogPart($"Add new line at end");
                            }
                            if (wasHistoryActionQ && reEditLine.IsNotNE())
                                Dbug.LogPart(" .. Re-edited the added line");
                        }
                        /// edit command
                        else if (commandPrompt.StartsWith(editorCmdEdit))
                        {
                            Dbug.LogPart("Edit Cmd --> ");
                            if (commandPrompt.Contains("\n"))
                            {
                                Dbug.LogPart(wasHistoryActionQ? "From History Action :: " : "From Editing Area :: ");
                                string[] editParts = commandPrompt.Split("\n");
                                if (int.TryParse(editParts[0].Replace(editorCmdEdit, ""), out int editLineNum))
                                {
                                    Dbug.LogPart($"line #{editLineNum} ");
                                    if (editParts[1].IsNotNEW())
                                    {
                                        _formatterData.EditLine(editLineNum, editParts[1].Trim(), out string prevEdit);
                                        Dbug.LogPart($"edit :: {editParts[1].Trim()}");

                                        histName = $"Edited Line {editLineNum}";
                                        if (appenderHistNamePart.IsNotNE())
                                            histName += $" ({appenderHistNamePart})";
                                        histRedo = commandPrompt;
                                        histUndo = $"{editorCmdEdit}{editLineNum}\n{prevEdit}";
                                    }
                                    else Dbug.LogPart("unedited (null or empty argument)");
                                }
                                else Dbug.LogPart("line #??");
                            }
                            else
                            {
                                Dbug.LogPart("To Editing Area ->|");
                                if (int.TryParse(commandPrompt.Replace(editorCmdEdit, ""), out int editLineNum))
                                {
                                    Dbug.LogPart($" Edit Line #{editLineNum}");
                                    lineToEdit = editLineNum;
                                }

                            }
                        }
                        /// copy command
                        else if (commandPrompt.StartsWith(editorCmdCopy))
                        {
                            Dbug.LogPart("Copy Cmd --> ");
                            string[] copyParts = commandPrompt.Replace(editorCmdCopy, "").Split(',');
                            if (int.TryParse(copyParts[0], out int copyFrom) && int.TryParse(copyParts[1], out int copyTo))
                            {
                                string copyData = _formatterData.GetLine(copyFrom);
                                _formatterData.EditLine(copyTo, copyData, out string prevEdit);
                                Dbug.LogPart($"Copied from line #{copyFrom} ('{copyData}') to line #{copyTo} (replaces '{prevEdit}')");

                                histName = $"Copied Line {copyFrom} to Line {copyTo}";
                                histRedo = commandPrompt;
                                histUndo = $"{editorCmdEdit}{copyTo}\n{prevEdit}";
                            }
                            else Dbug.LogPart("line numbers??");
                        }
                        /// deleted command
                        else if (commandPrompt.StartsWith(editorCmdDelete))
                        {
                            Dbug.LogPart("Delete Cmd --> ");
                            if (int.TryParse(commandPrompt.Replace(editorCmdDelete, ""), out int delLineNum))
                            {
                                _formatterData.DeleteLine(delLineNum, out string deletedLine);
                                Dbug.LogPart($"Delete at line #{delLineNum}");

                                histName = $"Delete Line {delLineNum}";
                                histRedo = commandPrompt;
                                histUndo = $"{editorCmdAdd}{(delLineNum == lineCount ? "" : delLineNum)}{(deletedLine.IsNE()? "" : $"\n{deletedLine}")}";
                            }
                            else Dbug.LogPart("line #??");
                        }
                        /// group command
                        else if (commandPrompt.StartsWith(editorCmdGroup))
                        {
                            Dbug.LogPart("Group Cmd --> ");
                            /// IF ..: 1st & 3rd (create/remove) functions; ELSE 2nd function (toggle expansion)
                            if (commandPrompt.CountOccuringCharacter(',') == 2)
                            {
                                Dbug.LogPart("Create or Remove Group :: ");
                                string[] groupParts = commandPrompt.Substring(editorCmdGroup.Length).Split(',');
                                string groupName = groupParts[2];
                                if (int.TryParse(groupParts[0], out int lineStart) && int.TryParse(groupParts[1], out int lineEnd) && groupName.IsNotNEW())
                                {
                                    if (lineStart == 0 && lineEnd == lineStart)
                                    {
                                        _formatterData.DeleteGroup(groupName, out SfdGroupInfo deletedGroup);
                                        if (deletedGroup != null)
                                        {
                                            Dbug.LogPart($"Removed group named '{groupName}'");

                                            histName = $"Removed Group '{groupName}'";
                                            histRedo = commandPrompt;
                                            histUndo = $"{editorCmdGroup}{deletedGroup.startLineNum},{deletedGroup.endLineNum},{groupName}";
                                        }
                                        else Dbug.LogPart($"No group to remove (DNE)");
                                    }
                                    else
                                    {
                                        _formatterData.CreateGroup(groupName, lineStart, lineEnd);
                                        Dbug.LogPart($"Created group named '{groupName}' ranging from lines {lineStart} to {lineEnd}");

                                        histName = $"Created Group '{groupName}'";
                                        histRedo = commandPrompt;
                                        histUndo = $"{editorCmdGroup}0,0,{groupName}";
                                    }                                    
                                }
                                else Dbug.LogPart("line #?? group name??");
                            }
                            else
                            {
                                Dbug.LogPart("Toggle Expansion :: ");
                                string groupName = commandPrompt.Substring(editorCmdGroup.Length);
                                if (groupName.IsNotNEW())
                                {
                                    groupName = groupName.Trim();
                                    bool? expansionState = _formatterData.ToggleGroupExpansion(groupName);

                                    string action = "Expanded / Collapsed";
                                    if (expansionState.HasValue)
                                        action = expansionState.Value ? "Expanded" : "Collapsed";

                                    Dbug.LogPart($"{action} group named '{groupName}'");
                                    histName = $"{action} Group '{groupName}'";
                                    histRedo = commandPrompt;
                                    histUndo = commandPrompt;
                                }
                                else Dbug.LogPart("group name??");   
                            }
                        }
                        /// undo \ redo commands
                        else
                        {
                            isHistoryActionQ = true;
                            Dbug.LogPart("History Cmd --> ");

                            if (commandPrompt.Equals(editorCmdUndo))
                            {
                                Dbug.LogPart("Undo :: ");
                                int currIx = historyActionNumber - 1;
                                if (currIx.IsWithin(0, _editorHistory.Count - 1))
                                {
                                    Dbug.LogPart($"Fetched history #{historyActionNumber} [@ix-{currIx}]; ");
                                    SFormatterHistory currHistory = _editorHistory[currIx];
                                    commandPrompt = currHistory.undoneCommand;
                                    historyActionNumber++;
                                }
                                else Dbug.LogPart("'Undo' action failed (not in history list?!)");
                                //isHistoryActionQ = false;
                            }
                            else
                            {
                                Dbug.LogPart("Redo :: ");
                                int redoIx = historyActionNumber - 2;
                                if (redoIx.IsWithin(0, _editorHistory.Count - 1))
                                {
                                    Dbug.LogPart($"Fetched history #{historyActionNumber - 1} [@ix-{redoIx}]; ");
                                    SFormatterHistory redoHistory = _editorHistory[redoIx];
                                    commandPrompt = redoHistory.redoneCommand;
                                    historyActionNumber--;
                                }
                                else Dbug.LogPart("'Redo' action failed (not in history list?!)");
                                //isHistoryActionQ = false;
                            }
                            Dbug.LogPart($"Loaded command prompt :: {commandPrompt.Replace("\n",histNLRep)}");

                            historyActionNumber = historyActionNumber.Clamp(historyActionInitial, historyLimit);
                            showHistoryQ = true;                            
                        }

                        Dbug.Log("; ");

                        if (isHistoryActionQ)
                        {
                            /// IF ...: load multiple commands; ELSE load 1 command 
                            if (commandPrompt.Contains(multiHistKey))
                            {
                                string[] commands = commandPrompt.Split(multiHistKey);
                                if (commands.HasElements())
                                {
                                    countHistoryCmdActions = commands.Length;
                                    historyCommandPrompts.AddRange(commands);
                                }
                            }
                            else
                            {
                                countHistoryCmdActions = 1;
                                historyCommandPrompts.Add(commandPrompt);
                            }

                            Dbug.Log($"Executing '{countHistoryCmdActions}' history action(s)  //  FYI: histActNum = {historyActionNumber}; histLim = {historyLimit}; editorHistCount = {_editorHistory.Count}; ");
                        }


                        // history accumulates here
                        SFormatterHistory history = new(histName, histRedo, histUndo);
                        if (history.IsSetup() && !wasHistoryActionQ)
                        {
                            Dbug.LogPart($"Recieved history instance --> {history} // ");
                            /// add new history before lastest history instance, remove old histories where necessary
                            if (historyActionNumber == historyActionInitial)
                            {
                                Dbug.LogPart("No history actions called; ");
                                if (_editorHistory.Count >= historyLimit)
                                {
                                    Dbug.LogPart($"Removing oldest history, inserting new history");
                                    _editorHistory.Insert(0, history);
                                    _editorHistory.RemoveAt(historyLimit);
                                }
                                else
                                {
                                    Dbug.LogPart($"Inserting new history");
                                    if (_editorHistory.HasElements())
                                        _editorHistory.Insert(0, history);
                                    else _editorHistory.Add(history);
                                }
                            }
                            /// remove a series of undone history and insert new history instance
                            else
                            {
                                Dbug.LogPart($"History actions were called; ");
                                _editorHistory.RemoveRange(0, historyActionNumber - 1);
                                Dbug.LogPart($"Removed '{historyActionNumber - 1}' histories, index range [0 -> {historyActionNumber - 2}]; ");

                                _editorHistory.Insert(0, history);
                                historyActionNumber = historyActionInitial;
                                Dbug.LogPart("Inserted new history instance @ix0");



                                /// WHAT A WASTE.... ALL OF IT... WHAT A WASTE...
                                /** The new plan - adding new history after an undo
                                    Rule of thumb: redo, get next redo. undo, get this undo
                                
                                    ORIGINAL CASES |:
                                    
                                    Case 1
                                    ---------------
                                    H1| Renamed Smack 2 
                                        undo[rename Smek 1]        redo[rename Smack 2]
                                    H2| Renamed Smek 1
                                        undo[rename New Format]     redo[rename Smek 1]
                                    H3| Open
                                        undo[--]      redo[--]

                                    --> If '<' (undo) then 'rename Smacking 3'

                                    Pre-Case 2
                                    ..........
                                    CrrHist ::  H2| undo[rename New Format] redo[rename Smek 1]
                                    NxtHist ::  H1| undo[rename Smek 1]     redo[rename Smack 2] 
                                    NewHist ::  H?| undo[rename Smek 1]     redo[rename Smacking 3] 


                                    Case 2
                                    ---------------                                    
                                    H1| Renamed Smack 2 / Renamed Smacking 3 
                                        undo[rename Smack 2/rename Smek 1]              ud = rd(nxt) + ud(new)
                                        redo[rename Smack 2/rename Smacking 3]          rd = rd(nxt) + rd(new)
                                    H2| Renamed Smek 1 
                                        undo[rename New Format]     redo[rename Smek 1]
                                    H3| Open 
                                        undo[--]      redo[--]

                                    --> If '<' (undo) again then 'rename Four Slap'

                                    Pre-Case 3
                                    ..........
                                    CrrHist ::  H2| undo[rename New Format]                 redo[rename Smek 1]
                                    NxtHist ::  H1| undo[rename Smack 2/rename Smek 1]      redo [rename Smack 2/rename Smacking 3]
                                    NewHist ::  H?| undo[rename Smek 1]                     redo[rename Four Slap]


                                    Case 3
                                    ---------------
                                    
                                    H1| Mutli-action / Rename Four Slap
                                        undo[rename Smacking 3 / rename Smack 2    / rename Smek 1]         ud = rd(nxt,1) + rd(nxt,0) + ud(new) 
                                        redo[rename Smack 2    / rename Smacking 3 / rename Four Slap]      rd = rd(nxt,0) + rd(nxt,1) + rd(new)
                                    H2| Renamed Smek 1
                                        undo[rename New Format]     redo[rename Smek 1]
                                    H3| Open
                                        undo[--]      redo[--]


                                    Construct summary after these cases
                                    -----
                                    UNDO construct of newHist (4 lvls)
                                    0 = ud(new)
                                    1 = rd(nxt) + ud(new)
                                    2 = rd(nxt,1) + rd(nxt,0) + ud(new)
                                    3 = rd(nxt,2) + rd(nxt,1) + rd(nxt,0) + ud(new)

                                    REDO construct of newHist (4 lvls)
                                    0 = rd(new)
                                    1 = rd(nxt) + rd(new) 
                                    2 = rd(nxt,0) + rd(nxt,1) + rd(new)
                                    3 = rd(nxt,0) + rd(nxt,1) + rd(nxt,2) + rd(new)

                                    :|

                                    MORE CASES WITH OTHER CMDs |: 
                                    
                                    Case 1
                                    ----------------
                                    H1| Add After L5    ud[del5]    rd[add5]
                                    H2| Edit L4         ud[edt4"]   rd[edt4"]    
                                    H3| Open            ud[--]      rd[--]

                                    -> If '<' undo and 'delete4'
                                        Actions :: Edit L4  |<-->  Add After L5  <-->  Delete L4
                                    
                                    Pre-Case 2
                                    ..........
                                    CrrHist ::  H2| ud[edt4"]   rd[edt4"]
                                    NxtHist ::  H1| ud[del5]    rd[add5]
                                    NewHist ::  H?| ud[add4"]   rd[del4]
                                
                                    
                                    Case 2
                                    ----------------
                                    H1| Add After L5 / Delete L4    ud[add4"/del5]    rd[add5/del4]            
                                    H2| Edit L4                     ud[edt4"]         rd[edt4"]
                                    H3| Open                        ud[--]            rd[--]

                                    ** ud = ud(new) + ud(nxt)       rd = rd(nxt) + rd(new)

                                    -> If '<' undo and 'copy4,3'
                                        Actions :: Edit L4  |<-->  Add After L5  <-->  Delete L4  <--> Copy L4 to L3

                                    Pre-Case 3
                                    ..........
                                    CrrHist ::  H2| ud[edt4"]       rd[edt4"]
                                    NxtHist ::  H1| ud[add4"/del5]  rd[add5/del4]
                                    NewHist ::  H?| ud[edt3"]       rd[cp4,3]            


                                    Case 3
                                    ----------------
                                    H1| Multi-action (x2) / Copy L4 to L3   ud[edt3"/add4"/del5]    rd[add5/del4/cp4,3]
                                    H2| Edit L4                             ud[edt4"]               rd[edt4"]
                                    H3| Open                                ud[--]                  rd[--]

                                    ** ud = ud(new) + ud(nxt,0) + ud(nxt,1)         rd = rd(nxt,0) + rd(nxt,1) + rd(new)
                                
                                
                                    Changes to construct summary
                                    -----
                                    -> The prior construct only fetched events from the nextHist's redo, and did not include its undo. This one includes that factor
                                    -> Using different cmds with opposing commands exposes the initially mistaken relation much more clearly (big_dumb)

                                    Revised construct summary after these cases
                                    -----
                                    UNDO construct of newHist (4 lvls)
                                    0 = ud(new)
                                    1 = ud(new) + ud(nxt)       
                                    2 = ud(new) + ud(nxt,0) + ud(nxt,1)
                                    3 = ud(new) + ud(nxt,0) + ud(nxt,1) + ud(nxt,2)

                                    REDO construct of newHist (4 lvls)
                                    0 = rd(new)
                                    1 = rd(nxt) + rd(new)
                                    2 = rd(nxt,0) + rd(nxt,1) + rd(new)
                                    3 = rd(nxt,0) + rd(nxt,1) + rd(nxt,2) + rd(new)

                                    
                                    .
                                    .
                                    .
                                    .
                                    
                                    
                                    But then... why do I need all to know all those other history actions???

                                    RE:
                                    Case 1
                                    ----------------
                                    H1| Add After L5    ud[del6]    rd[add5]
                                    H2| Edit L4         ud[edt4"]   rd[edt4"]    
                                    H3| Open            ud[--]      rd[--]

                                    -> If '<' undo and 'delete4'
                                        Actions :: Edit L4  |<-->  Add After L5  <-->  Delete L4
                                    
                                    Pre-Case 2
                                    ..........
                                    CrrHist ::  H2| ud[edt4"]   rd[edt4"]
                                    NxtHist ::  H1| ud[del6]    rd[add5]                /// del5 is not opposite of add5; It's add4/del5 and add5/del6, but there is del6/add6" 
                                    NewHist ::  H?| ud[add4"]   rd[del4]
                                
                                    
                                    Case 2
                                    ----------------
                                    H1| Add After L5 / Delete L4    ud[add4"/del6]      rd[add5/del4]       
                                    H2| Edit L4                     ud[edt4"]           rd[edt4"]
                                    H3| Open                        ud[--]              rd[--]

                                    ** ud = ud(new) + ud(nxt)       rd = rd(nxt) + rd(new)

                                    -> If '<' undo and 'copy4,3'
                                        Actions :: Edit L4  |<-->  Add After L5   (remove 'Delete L4')  <-->  Copy L4 to L3
                                    
                                    Pre-Case 3
                                    ..........
                                    CrrHist ::  H2| ud[edit4"]      rd[edt4"]
                                    NxtHist ::  H1| ud[add4"/del6]  rd[add5/del4]
                                    NewHist ::  H?| ud[edt3"]       rd[cp4,3]
                                    
                                    
                                    Case 3
                                    ----------------
                                    H1| Add After L5 / Copy L4 to L3    ud[edt3"/del6]  rd[add5/cp4,3]
                                    H2| Edit L4                         ud[edt4"]       rd[edt4"]
                                    H3| Open                            ud[--]          rd[--]

                                    ** ud = ud(new) + ud(nxt,1)         rd = rd(nxt,0) + rd(new)

                                    -> If '<' and 'rename Flats'
                                        Actions :: Edit L4  |<-->  Add After L5   (remove Copy L4 to L3)  <-->  Renamed Profile
                                    
                                    Pre-Case 4
                                    ..........
                                    CrrHist :: H1| ud[edit4"]       rd[edit4"]
                                    NxtHist :: H2| ud[edt3"/del6]   rd[add5/cp4,3]
                                    NewHist :: H?| ud[rn"]          rd[rn""]

                                    
                                    Case 4
                                    --------------------
                                    H1| Add After L5 / Renamed Profile  ud[rn"/del6]    rd[add5/rn""]
                                    H2| Edit L4                         ud[edt4"]       rd[edt4"]
                                    H3| Open                            ud[--]          rd[--]

                                    ** ud = ud(new) + ud(nxt,1)         rd = rd(nxt,0) + rd(new)

                                    
                                    Changes to construct summary
                                    -----
                                    -> The previous version did not consider that only two actions in entirety needed to be executed between two histories
                                    -> This version acknowledges that between the next history and the new history, there are ONLY two changes to consider

                                    -----
                                    UNDO construct of newHist (4 lvls)
                                    0 = ud(new)
                                    1 = ud(new) + ud(nxt)
                                    2 = ud(new) + ud(nxt,1)
                                    3 = ud(new) + ud(nxt,1)

                                    REDO construct of newHist (4 lvls)
                                    0 = rd(new)
                                    1 = rd(nxt) + rd(new)
                                    2 = rd(nxt,0) + rd(new)
                                    3 = rd(nxt,0) + rd(new)

                                    :|     

                                 Well these have been tested multiple times, and the add and delete functions keep messing me up. 
                                 These need to synergize properly, meaning we'll be changing their functions. 
                                    'Add 13' will add BEFORE line 13 and not AFTER. Adding at the end will still be 'Add'. 'Add0' now obsolete
                                    'Delete 13' will oppose 'Add 13', and 'Delete {lineCount}' will oppose 'Add'.
                                .. These changes should resolve much, for the other functions do quite fine.
                                  

                                 ***/
                                if ((historyActionNumber - 2).IsWithin(0, _editorHistory.Count - 1) && false)
                                {
                                    Dbug.LogPart($"History actions were called; ");

                                    SFormatterHistory nextHistory = _editorHistory[historyActionNumber - 2];
                                    Dbug.Log($"Fetched history #{historyActionNumber - 1} [next]");

                                    string newHistName, newHistUndo, newHistRedo;
                                    /// new history name
                                    string nextHistName = nextHistory.actionName;
                                    if (nextHistory.actionName.Contains("/"))
                                    {
                                        string[] nextNames = nextHistory.actionName.Split('/');
                                        nextHistName = nextNames[0];
                                    }
                                    newHistName = nextHistName != histName ? $"{nextHistName} / {histName}" : $"{histName} / (x2)";
                                    /// new history undone/redone commands
                                    if (nextHistory.undoneCommand.Contains(multiHistKey) && nextHistory.redoneCommand.Contains(multiHistKey))
                                    {
                                        string[] nextRedos = nextHistory.redoneCommand.Split(multiHistKey);
                                        string[] nextUndos = nextHistory.undoneCommand.Split(multiHistKey);

                                        newHistUndo = $"{histUndo}{multiHistKey}{nextUndos[1]}";
                                        newHistRedo = $"{nextRedos[0]}{multiHistKey}{histRedo}";
                                    }
                                    else
                                    {
                                        newHistUndo = $"{histUndo}{multiHistKey}{nextHistory.undoneCommand}";
                                        newHistRedo = $"{nextHistory.redoneCommand}{multiHistKey}{histRedo}";
                                    }

                                    Dbug.Log($" + Generated new history name :: {newHistName.Replace("\n", histNLRep)}");
                                    Dbug.Log($" + Generated new history undo :: {newHistUndo.Replace("\n", histNLRep)}");
                                    Dbug.Log($" + Generated new history redo :: {newHistRedo.Replace("\n", histNLRep)}");

                                    history = new SFormatterHistory(newHistName, newHistRedo, newHistUndo);
                                    Dbug.LogPart("New history instance compiled with details above; ");

                                    _editorHistory.RemoveRange(0, historyActionNumber - 1);
                                    Dbug.LogPart($"Removed '{historyActionNumber - 1}' histories, index range [0 -> {historyActionNumber - 2}]; ");

                                    _editorHistory.Insert(0, history);
                                    historyActionNumber = historyActionInitial;
                                    Dbug.LogPart("Inserted new history instance @ix0");
                                }
                                #region old_code
                                //Dbug.LogPart($"History actions were called; ");
                                //if ((historyActionNumber - 2).IsWithin(0, _editorHistory.Count - 1))
                                //{
                                //    SFormatterHistory nextRedoHist = _editorHistory[historyActionNumber - 2];
                                //    Dbug.Log("; ");
                                //    Dbug.LogPart($"Fetched history #{historyActionNumber - 1}; replacing new history 'undo' with history #{historyActionNumber - 1}'s 'undo'; ");

                                //    string histNewName = nextRedoHist.actionName != histName ? $"{nextRedoHist.actionName} / {histName}" : histName;
                                //    history = new SFormatterHistory(histNewName, histRedo, nextRedoHist.undoneCommand);
                                //    Dbug.Log($"New history instance :: {history}");
                                //}

                                //_editorHistory.RemoveRange(0, historyActionNumber - 1);
                                //Dbug.LogPart($"Removed '{historyActionNumber - 1}' histories, index range [0 -> {historyActionNumber - 2}]; ");

                                //_editorHistory.Insert(0, history);
                                //Dbug.LogPart($"Inserted new history");
                                //historyActionNumber = historyActionInitial;
                                #endregion
                            }
                            Dbug.Log("; ");
                        }

                    } while (isHistoryActionQ || countHistoryCmdActions > 0);
                }

                countCycles++;
                Dbug.NudgeIndent(false);
            }
            while (!exitFormatEditorQ);

            Dbug.NudgeIndent(false);
            if (isUsingFormatterNo1Q)
                historyActionNumber1 = historyActionNumber;
            else historyActionNumber2 = historyActionNumber;
            Dbug.Log($"Saved history action number '{historyActionNumber}' (editor history counts '{_editorHistory.Count}') to formatter profile #{(isUsingFormatterNo1Q? 1 : 2)}");
            
            Dbug.Log($"RECORDING END");
            Dbug.EndLogging();
        }
        /// <summary>The formatter parser; where the satisfaction of creating a steam log is realized.</summary>
        static string[] FormattingParser(string[] formatterLines, SFormatterLibRef libRef)
        {
            List<string> finalLines = new();
            if (formatterLines.HasElements() && libRef.IsSetup())
            {
                Dbug.StartLogging("GenSteamLogPage.FormattingParser()");
                Dbug.LogPart($"Received '{formatterLines.Length}' formatting lines; library reference is setup; ");

                ProgressBarInitialize(true, false, 24, 2);
                ProgressBarUpdate(0);
                TaskCount = formatterLines.Length;

                // get last parsable line
                int lastParsableLineNum = 0;
                for (int lx0 = 0; lx0 < formatterLines.Length; lx0++)
                {
                    if (!formatterLines[lx0].TrimStart().StartsWith("//"))
                        lastParsableLineNum = lx0 + 1;
                }
                Dbug.Log($"Fetched last parsable line (line {lastParsableLineNum}); Proceeding to parsing; ");

                int skipLinesNum = 0;
                bool? prevIfCondition = null;
                const string nlRep = "\x2590\x2584";
                string heldListOrTable = null;
                // parsing loop
                for (int lx = 0; lx < formatterLines.Length; lx++)
                {
                    int lineNum = lx + 1;
                    string line = formatterLines[lx], nextLine = null;
                    int frontPedalCount = 1;
                    if (lx + 1 < formatterLines.Length)
                    {
                        while (lx + frontPedalCount < formatterLines.Length && nextLine.IsNE())
                        {
                            string aNextLine = formatterLines[lx + frontPedalCount];
                            if (!aNextLine.TrimStart().StartsWith("//"))
                                nextLine = aNextLine;

                            if (nextLine.IsNE())
                                frontPedalCount++;
                        }
                        //nextLine = formatterLines[lx + 1];
                    }
                    bool lineIsSkipped = skipLinesNum > 0, lineIsComment = line.TrimStart().StartsWith("//");

                    /// line info
                    Dbug.LogPart($"LINE {lineNum}  ::  {line}; ");
                    Dbug.Log($" [{(lineIsSkipped ? $"skip'{skipLinesNum}" : "")}.{(lineIsComment ? "comment" : "")}.{(heldListOrTable.IsNE() ? "" : $"held'{(heldListOrTable.Contains("list") ? "L" : "T")}")}]; ");

                    if (skipLinesNum > 0)
                        skipLinesNum--;

                    Dbug.NudgeIndent(true);
                    if (!lineIsComment && !lineIsSkipped)
                    {
                        List<string> lineOutcomes = new();

                        // KEYWORD CONTROLS EXECUTIONS
                        Dbug.NudgeIndent(true);
                        if (line.StartsWith("if") || line.StartsWith("else") || line.StartsWith("repeat"))
                        {
                            /// identify and get keyword block(s), separate from rest of line
                            Dbug.LogPart("Keyword Controls identified; Keyword Blocks --> ");
                            string snipFullControl = line.RemoveFromPlainText(';').SnippetText(line[0].ToString(), ";", Snip.Inc, Snip.EndLast);
                            string remainingLine = line.Substring(snipFullControl.Length);
                            string[] keyControls = new string[3];
                            if (snipFullControl.CountOccuringCharacter(';') > 1)
                            {
                                string[] controls = snipFullControl.LineBreak(';', true);
                                for (int c = 0; c < controls.Length && c < 3; c++)
                                    if (controls[c].Contains(';'))
                                        keyControls[c] = controls[c];
                            }
                            else keyControls[0] = snipFullControl;

                            foreach (string keyCon in keyControls)
                                if (keyCon.IsNotNE())
                                    Dbug.LogPart($"[{keyCon}]");
                            Dbug.LogPart($"; Remaining Line [{remainingLine}]");
                            Dbug.Log("; ");


                            /// execute control keywords
                            Dbug.NudgeIndent(true);
                            bool? currentBoolCondition = null;
                            bool isJumpedQ = false, isNextedQ = false;
                            int repeatCount = 0;
                            string repeatIfVal1 = "", repeatIfVal2 = "", repeatIfOp = "";
                            for (int kbx = 0; kbx < keyControls.Length; kbx++)
                            {
                                bool isFirstControlBlockQ = kbx == 0;
                                bool isLastControlBlockQ = kbx == keyControls.Length - 1;
                                string keyConBlock = keyControls[kbx];

                                if (keyConBlock.IsNotNEW())
                                {
                                    keyConBlock = ReplaceLibRefs(keyConBlock.Trim());
                                    Dbug.LogPart($"Executing Block {kbx + 1} [{keyConBlock}] -->  ");

                                    /// if {value1} =/!= {value2}
                                    ///     pos: 1st 2nd
                                    ///     preceded by: if, else, repeat
                                    if (keyConBlock.StartsWith("if"))
                                    {
                                        string argsIf = keyConBlock.SnippetText("if", ";");
                                        string val1, val2, op = "";

                                        string noPlainArgs = argsIf.RemovePlainText();
                                        if (noPlainArgs.RemovePlainText().Contains('='))
                                        {
                                            if (noPlainArgs.Contains("!="))
                                                op = "!=";
                                            else if (noPlainArgs.Contains("<="))
                                                op = "<=";
                                            else if (noPlainArgs.Contains(">="))
                                                op = ">=";
                                            else op = "=";
                                        }
                                        else
                                        {
                                            if (noPlainArgs.Contains(">"))
                                                op = ">";
                                            else if (noPlainArgs.Contains("<"))
                                                op = "<";
                                        }

                                        string[] argParts = argsIf.LineBreak('=', true, true);
                                        if (!argParts.HasElements(2))
                                        {
                                            if (argsIf.Contains(">"))
                                                argParts = argsIf.LineBreak('>', true, true);
                                            else argParts = argsIf.LineBreak('<', true, true);
                                        }
                                        argParts[0] = argParts[0][..^op.Length];
                                        

                                        if (int.TryParse(argParts[0], out int val1Num))
                                            val1 = $"\"{val1Num}\"";
                                        else val1 = argParts[0].Trim();
                                        if (int.TryParse(argParts[1], out int val2Num))
                                            val2 = $"\"{val2Num}\"";
                                        else val2 = argParts[1].Trim();

                                        bool conditionIf = ResultIfComparison(val1, op, val2);
                                        Dbug.LogPart($"Comparison [{val1} / {op} / {val2}]; Result [");

                                        if (isFirstControlBlockQ)
                                        {
                                            currentBoolCondition = conditionIf;
                                            Dbug.LogPart(conditionIf.ToString());
                                        }
                                        else
                                        {                                            
                                            if (currentBoolCondition.HasValue)
                                            {
                                                Dbug.LogPart($"{conditionIf} && {currentBoolCondition.Value} -> {currentBoolCondition.Value && conditionIf}");
                                                currentBoolCondition = conditionIf && currentBoolCondition.Value;
                                            }
                                            else
                                            {
                                                if (repeatCount > 0)
                                                {
                                                    Dbug.LogPart("DELAYED! to Repeat");
                                                    repeatIfVal1 = val1;
                                                    repeatIfVal2 = val2;
                                                    repeatIfOp = op;
                                                }
                                                else
                                                {
                                                    Dbug.LogPart(" ???? ");
                                                    //currentBoolCondition = conditionIf;
                                                    //Dbug.LogPart(conditionIf.ToString());
                                                }
                                            }
                                        }
                                        Dbug.LogPart("] ");
                                    }

                                    /// else;
                                    ///     pos: 1st
                                    if (keyConBlock.StartsWith("else") && isFirstControlBlockQ)
                                    {
                                        if (prevIfCondition.HasValue)
                                        {
                                            currentBoolCondition = !prevIfCondition.Value;
                                            Dbug.LogPart($"Prev Condition [{prevIfCondition.Value}]; Result [{currentBoolCondition.Value}]");
                                        }
                                    }

                                    /// repeat #;
                                    ///     pos: 1st
                                    if (keyConBlock.StartsWith("repeat") && isFirstControlBlockQ)
                                    {
                                        string argsRepeat = keyConBlock.SnippetText("repeat", ";");
                                        if (argsRepeat.Contains('"'))
                                        {
                                            Dbug.LogPart("From Lib Ref");
                                            argsRepeat = argsRepeat.Replace('\"', ' ');
                                        }
                                        else Dbug.LogPart("From Pure Number");
                                        Dbug.LogPart("; Result ");

                                        if (int.TryParse(argsRepeat, out int repeatNum))
                                        {
                                            repeatCount = repeatNum;
                                            Dbug.LogPart($"[{repeatNum}]");
                                        }
                                        else Dbug.LogPart("[??]");
                                    }

                                    /// jump #;
                                    ///     pos: 2nd 3rd
                                    ///     preceded by: if, else
                                    if (keyConBlock.StartsWith("jump") && !isFirstControlBlockQ)
                                    {
                                        string argsJump = keyConBlock.SnippetText("jump", ";");
                                        Dbug.LogPart("From Pure Number; Result ");
                                        if (int.TryParse(argsJump, out int jumpLine))
                                        {
                                            int lineJumpCount = jumpLine - lineNum - 1;
                                            Dbug.LogPart($"[{jumpLine}] --> 1st{(!isFirstControlBlockQ ? " & 2nd" : "")} Condition ");
                                            if (currentBoolCondition.HasValue)
                                            {
                                                Dbug.LogPart($"[{currentBoolCondition.Value}] Result: ");
                                                if (currentBoolCondition.Value)
                                                {
                                                    skipLinesNum = lineJumpCount;
                                                    Dbug.LogPart($" Skip '{skipLinesNum}' line(s)");
                                                    isJumpedQ = true;
                                                }
                                                else Dbug.LogPart(" No lines will be skipped");
                                            }
                                            else Dbug.LogPart("[??]");
                                        }
                                        else Dbug.LogPart("[??]");
                                    }

                                    /// next;
                                    ///     pos: 2nd 3rd
                                    ///     preceded by: if, else, repeat
                                    if (keyConBlock.StartsWith("next") && !isFirstControlBlockQ)
                                    {
                                        remainingLine = "";
                                        if (currentBoolCondition.HasValue)
                                        {
                                            Dbug.LogPart($"1st Condition [{currentBoolCondition.Value}]; Result: ");
                                            if (currentBoolCondition.Value)
                                            {
                                                Dbug.LogPart("Utilize next line, then --> ");
                                                isNextedQ = true;
                                            }
                                            Dbug.LogPart("Skip next line");
                                        }
                                        else
                                        {
                                            Dbug.LogPart("Result: Utilize next line and then skip it");
                                            isNextedQ = true;
                                        }
                                        skipLinesNum = frontPedalCount;
                                        Dbug.LogPart($" (Skip '{skipLinesNum}' lines)");
                                    }

                                    Dbug.Log("; ");
                                }

                                /// final edit choices here
                                if (isLastControlBlockQ)
                                {
                                    /// conditions and remaining line
                                    Dbug.LogPart($"Conditional Outcome [");
                                    if (currentBoolCondition.HasValue)
                                    {
                                        Dbug.LogPart(currentBoolCondition.Value ? "Use Remaining" : "Exempt Remaining");
                                        if (!currentBoolCondition.Value)
                                            remainingLine = "";
                                    }
                                    else Dbug.LogPart("N/a");
                                    if (isJumpedQ)
                                        remainingLine = "";
                                    if (isNextedQ)
                                        remainingLine = nextLine;
                                    Dbug.Log($"]; Jump'ed? [{isJumpedQ}]; Next'ed? [{isNextedQ}]; Final Remaining Line [{remainingLine}]");

                                    /// repeat execute
                                    if (remainingLine.IsNotNEW())
                                    {
                                        if (repeatCount > 0)
                                        {
                                            Dbug.LogPart($"Execution Delayed --> Repeat remaining line '{repeatCount}' times");

                                            int countRepeatsUnPrinted = 0;
                                            for (int rx = 1; rx <= repeatCount; rx++)
                                            {
                                                string condRemainingLine = remainingLine;
                                                if (repeatIfOp.IsNotNEW())
                                                {
                                                    string repVal1 = ReplaceLibRefs(repeatIfVal1.Replace("#", $"{rx}"));
                                                    string repVal2 = ReplaceLibRefs(repeatIfVal2.Replace("#", $"{rx}"));

                                                    condRemainingLine = ResultIfComparison(repVal1, repeatIfOp, repVal2) ? remainingLine : "";
                                                }

                                                if (condRemainingLine.IsNotNE())
                                                    lineOutcomes.Add(ReplaceLibRefs(condRemainingLine.Replace("#", $"{rx}")));
                                                else countRepeatsUnPrinted++;
                                            }

                                            if (repeatIfOp.IsNotNEW())
                                                Dbug.LogPart($"; With Condition [IF {repeatIfVal1} {repeatIfOp} {repeatIfVal2}] --> Exempted '{countRepeatsUnPrinted}' lines");
                                            Dbug.Log("; ");

                                            repeatIfOp = "";
                                            repeatIfVal1 = "";
                                            repeatIfVal2 = "";
                                        }
                                        else lineOutcomes.Add(ReplaceLibRefs(remainingLine));
                                    }
                                }
                            }
                            Dbug.NudgeIndent(false);

                            prevIfCondition = currentBoolCondition;


                            /// METHOD
                            bool ResultIfComparison(string val1, string op, string val2)
                            {
                                bool compIfResult = false;
                                if (val1.IsNotNE() && op.IsNotNEW() && val2.IsNotNE())
                                {
                                    if (op == "=")
                                        compIfResult = val1 == val2;
                                    else if (op == "!=")
                                        compIfResult = val1 != val2;
                                    else
                                    {
                                        bool parsedVal1 = int.TryParse(val1.RemovePlainText(true), out int val1Num);
                                        bool parsedVal2 = int.TryParse(val2.RemovePlainText(true), out int val2Num);
                                        if (!parsedVal1)
                                            parsedVal1 = int.TryParse(val1, out val1Num);
                                        if (!parsedVal2)
                                            parsedVal2 = int.TryParse(val2, out val2Num);

                                        if (parsedVal1 && parsedVal2)
                                        {
                                            if (op == ">")
                                                compIfResult = val1Num > val2Num;
                                            else if (op == "<")
                                                compIfResult = val1Num < val2Num;
                                            else if (op == ">=")
                                                compIfResult = val1Num >= val2Num;
                                            else if (op == "<=")
                                                compIfResult = val1Num <= val2Num;
                                        }
                                    }
                                }
                                return compIfResult;
                            }
                        }
                        else
                        {
                            prevIfCondition = null;
                            lineOutcomes.Add(line);
                        }
                        Dbug.NudgeIndent(false);

                        if (lastParsableLineNum <= lineNum + skipLinesNum && !lineOutcomes.HasElements())
                            lineOutcomes.Add("\" \"");

                        // STEAM FORMAT REFERENCES  &&  FINAL LINE MODIFICATIONS
                        if (lineOutcomes.HasElements())
                        {
                            // Steam Format References
                            Dbug.NudgeIndent(true);
                            bool searchForSteamRefs = line.RemovePlainText().Contains("$");
                            if (!searchForSteamRefs)
                            {
                                if (nextLine.IsNotNE())
                                    searchForSteamRefs = nextLine.RemovePlainText().Contains("$");
                                else searchForSteamRefs = lastParsableLineNum <= lineNum + skipLinesNum;
                            }                                

                            if (searchForSteamRefs)
                            {
                                List<string> newLineOutcomes = new();
                                Dbug.Log("Searching for Steam Format References in lines; ");

                                if (lineOutcomes.HasElements())
                                {
                                    for (int sfx = 0; sfx < lineOutcomes.Count; sfx++)
                                    {
                                        string keyLine = ReplaceLibRefs(lineOutcomes[sfx]);
                                        string keyLineNum = $"{lineNum}.{sfx + 1}";
                                        Dbug.LogPart($"N-LINE {keyLineNum}  ::  {keyLine}{(keyLine.RemovePlainText(true).IsEW() && keyLine.RemovePlainText().IsEW() ? " <lastExecutableLine_substitute>" : "")}; ");
                                        Dbug.Log($"[{(heldListOrTable.IsNE() ? "" : $"held'{(heldListOrTable.Contains("list") ? "L" : "T")}")}]; ");

                                        Dbug.NudgeIndent(true);
                                        bool isLastLineQ = lastParsableLineNum <= lineNum + skipLinesNum && sfx == lineOutcomes.Count - 1;
                                        string[] steamFormatLines = ReplaceSteamRefs(keyLine, out heldListOrTable, isLastLineQ, heldListOrTable);
                                        if (steamFormatLines.HasElements())
                                        {
                                            int countLine = 0;
                                            foreach (string sfLine in steamFormatLines)
                                            {
                                                countLine++;
                                                Dbug.Log($"PART {countLine, -2}  ::  {sfLine.Replace("\n", nlRep)}; ");
                                                newLineOutcomes.Add(sfLine);
                                            }
                                        }
                                        else Dbug.Log(" ??? ");
                                        Dbug.NudgeIndent(false);
                                    }
                                }
                                else Dbug.Log("No lines received post-keywords lines; ");

                                if (newLineOutcomes.HasElements())
                                {
                                    lineOutcomes = newLineOutcomes;
                                    Dbug.Log("Updated Line Outcomes to Steam Format edits; ");
                                }
                            }
                            Dbug.NudgeIndent(false);


                            
                            // Final modifications
                            for (int flx = 0; flx < lineOutcomes.Count; flx++)
                            {
                                string subLine = ReplaceLibRefs(lineOutcomes[flx]).RemovePlainText(true);
                                subLine = subLine.Replace("&00;", "\"").Replace("&01;", "#");

                                if (subLine.IsNotNEW())
                                {
                                    string subLineNum = $"{lineNum} {IntToAlphabet(flx % 26)}{(flx / 26 == 0 ? "" : (flx / 26) + 1)}";
                                    Dbug.Log($"FINAL LINE {subLineNum}  ::  {subLine.Replace("\n", nlRep)}; ");

                                    finalLines.Add(subLine);
                                }
                            }
                        }
                        Dbug.Log("- - -");
                    }
                    Dbug.NudgeIndent(false);

                    TaskNum++;
                    ProgressBarUpdate(TaskNum / TaskCount, true);
                }
                Dbug.EndLogging();
                ProgressBarUpdate(1, true);


                // METHODS FOR PARSER
                string ReplaceLibRefs(string line)
                {
                    string editedLine = line;
                    if (line.IsNotNE() && libRef.IsSetup())
                    {
                        editedLine = "";
                        string lValue = "";
                        string[] lineParts = line.LineBreak(' ');
                        foreach (string linePart in lineParts)
                        {
                            if (linePart.IsNotNE())
                            {
                                string reference = linePart.RemovePlainText().Trim();
                                if (reference.Contains("{") && reference.Contains("}"))
                                {
                                    reference = reference.SnippetText("{", "}", Snip.Inc, Snip.EndAft);
                                    if (reference.Contains(":"))
                                    {
                                        DecodedSection? secType = null;
                                        int entNum = 0;
                                        LibRefProp? prop = null;

                                        string[] refParts = reference.SnippetText("{", "}").Split(':');
                                        if (refParts[1].Contains(','))
                                        {
                                            string[] refParams = refParts[1].Split(',');
                                            if (int.TryParse(refParams[0], out int theEntNum))
                                                entNum = theEntNum;
                                            prop = refParams[1] switch
                                            {
                                                "ids" => LibRefProp.Ids,
                                                "name" or "optionalName" => LibRefProp.Name,
                                                "relatedID" or "id" => LibRefProp.RelatedID,
                                                "relatedContent" => LibRefProp.RelatedName,
                                                "changeDesc" => LibRefProp.ChangeDesc,
                                                "key" => LibRefProp.Key,
                                                "definition" => LibRefProp.Definition,
                                                _ => null
                                            };

                                            /// Lib Ref props
                                            /// Added --> ids, name
                                            /// Addit --> ids, optionalName, relatedContent, relatedID
                                            /// Updated --> changeDesc, id, relatedContent
                                            /// Legend --> definition, key
                                            /// Summary --> 
                                            ///
                                        }
                                        else if (int.TryParse(refParts[1], out int theEntNum))
                                        {
                                            entNum = theEntNum;
                                            prop = LibRefProp.SummaryPart;
                                        }
                                        secType = refParts[0] switch
                                        {
                                            "Added" => DecodedSection.Added,
                                            "Addit" => DecodedSection.Additional,
                                            "Updated" => DecodedSection.Updated,
                                            "Legend" => DecodedSection.Legend,
                                            "Summary" => DecodedSection.Summary,
                                            _ => null
                                        };


                                        if (secType.HasValue && entNum > 0)
                                        {
                                            if (prop.HasValue)
                                                lValue = libRef.GetPropertyValue(secType.Value, entNum, prop.Value);
                                            else lValue = libRef.GetPropertyValue(secType.Value, entNum, LibRefProp.SummaryPart);
                                        }
                                    }
                                    else
                                    {
                                        reference = reference.RemoveFromPlainText('{').SnippetText("{", "}", Snip.Inc);
                                        string refValue = reference switch
                                        {
                                            "{Version}" => libRef.Version,
                                            "{AddedCount}" => libRef.AddedCount,
                                            "{AdditCount}" => libRef.AdditCount,
                                            "{TTA}" => libRef.TTA,
                                            "{UpdatedCount}" => libRef.UpdatedCount,
                                            "{LegendCount}" => libRef.LegendCount,
                                            "{SummaryCount}" => libRef.SummaryCount,
                                            _ => "",
                                        };

                                        if (refValue.IsNotNE())
                                            lValue = refValue.Replace("\"", "&00;").Replace("#", "&01;");
                                    }
                                }

                                if (lValue.IsNotNEW())
                                    lValue = $"\"{lValue}\"";

                                if (lValue.IsNotNEW())
                                {
                                    editedLine += linePart.Replace(reference, lValue);
                                    lValue = "";
                                }
                                else editedLine += linePart;
                            }
                        }     
                    }
                    return editedLine;
                }
                string[] ReplaceSteamRefs(string line, out string heldListOrTable, bool lastLineQ,  string listOrTableTag = "")
                {
                    heldListOrTable = listOrTableTag;
                    List<string> editedLines = new();
                    string tagsBackPile = "", tagsFrontPile = "";
                    if (line.IsNotNEW())
                    {
                        string[] lineParts = line.LineBreak('$');
                        List<string> finalLineParts = new();
                        for (int sx = 0; sx < lineParts.Length; sx++)
                        {
                            /** STEAM FORMAT REFERENCE SYNTAX - revised
                            # S T E A M   F O R M A T   R E F E R E N C E S
                            `    Steam format references are styling element calls that will affect the look of any text or value placed after it on
                            ▌    log generation.
                            ▌    Simple command references may be combined with other simple commands unless otherwise unpermitted. Simple commands
                            ▌    affect only one value that follows them.
                            ▌    Complex commands require a text or value to be placed in a described parameter surrounded by single quote characters
                            ▌    (').

                            SYNTAX                          :  OUTCOME
                            --------------------------------:------------------------------------------------------------------------------------------
                                $h                            :  Simple command. Header text. Must be placed at the start of the line. May not be
                                                            :  combined with other simple commands.
                                                            :    There are three levels of header text. The header level follows the number of 'h's in
                                                            :  reference. Example, a level three header text is '$hhh'.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $b                            :  Simple command. Bold text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $u                            :  Simple command. Underlined text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $i                            :  Simple command. Italicized text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $s                            :  Simple command. Strikethrough text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $sp                           :  Simple command. Spoiler text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $np                           :  Simple command. No parse. Doesn't parse steam format tags when generating steam log.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $c                            :  Simple command. Code text. Fixed width font, preserves space.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $hr                           :  Simple command. Horizontal rule. Must be placed on its own line. May not be combined
                                                            :  with other simple commands.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $nl                           :  Simple command. New line.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $d                            :  Simple command. Indent.
                                                            :    There are four indentation levels which relates to the number of 'd's in reference.
                                                            :  Example, a level 2 indent is '$dd'.
                                                            :    An indentation is the equivalent of two spaces (' 'x2).
                            --------------------------------:------------------------------------------------------------------------------------------
                                $r                            :  Simple command. Regular. Used to forcefully demark the end of preceding simple commands.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $url='link':'name'            :  Complex command. Must be placed on its own line.
                                                            :    Creates a website link by using URL address 'link' to create a hyperlink text
                                                            :  described as 'name'.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $list[or]                     :  Complex command. Must be placed on its own line.
                                                            :    Starts a list block. The optional parameter within square brackets, 'or', will
                                                            :  initiate an ordered (numbered) list. Otherwise, an unordered list is initiated.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $*                            :  Simple command. Must be placed on its own line.
                                                            :    Used within a list block to create a list item. Simple commands may follow to style
                                                            :  the list item value or text.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $q='author':'quote'           :  Complex command. Must be placed on its own line.
                                                            :    Generates a quote block that will reference an 'author' and display their original
                                                            :  text 'quote'.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $table[nb,ec]                 :  Complex command. Must be placed on its own line.
                                                            :    Starts a table block. There are two optional parameters within square brackets:
                                                            :  parameter 'nb' will generate a table with no borders, parameter 'ec' will generate a
                                                            :  table with equal cells.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $th='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                            :    Used within a table block to create a table header row. Separate multiple columns of
                                                            :  data with ','. Must follow immediately after a table block has started.
                            --------------------------------:------------------------------------------------------------------------------------------
                                $td='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                            :    Used within a table block to create a table data row. Separate multiple columns of
                                                            :  data with ','.
                            --------------------------------:------------------------------------------------------------------------------------------


                            STEAM FORMAT TAGS
                            ==================
                            $h, $hh, $hhh       --> [h1][/h1]   [h2][/h2]   [h3][/h3]
                            $b, $u, $i, $s      --> [b][/b]     [u][/u]     [i][/i]     [strike][/strike]
                            $sp, $np, $c        --> [spoiler][/spoiler]     [noparse][/noparse]     [code][/code]
                            $hr, $nl            --> [hr][/hr]       \n
                            $d (any), $r        --> {Ind{1-4}4}     {null}
                            $*                  --> [*]
                            $url                --> [url={link}]{name}[/url]
                            $list[or]           --> [list][/list]   [olist][/olist]
                            $q                  --> [quote={author}]{quote}[/quote]
                            $table[ec,nb]       --> [table equalcells=1 noborder=1][/table]     
                            $th                 --> [tr] [th]{clm1}[/th] [th]{clm2}[/th] [/tr]
                            $td                 --> [tr] [td]{clm1}[/td] [td]{clm2}[/td] [/tr]

                             **/

                            string lPart = lineParts[sx];
                            bool keepResultsSeparatedQ = false, isEndQ = sx + 1 >= lineParts.Length;
                            bool releaseHeldListOrTableQ = true;
                            string newHeldListOrTable = "";

                            /// no steam references in 'this' part
                            if (!lPart.RemovePlainText().Contains('$'))
                                finalLineParts.Add(lPart.RemovePlainText(true));

                            /// complex 
                            ///     list[]  table[]
                            else if (lPart.RemovePlainText().Contains("["))
                            {
                                string[] complexParts = lPart.LineBreak('[');
                                if (complexParts[0].Contains("table"))
                                {
                                    newHeldListOrTable = "table";
                                    string tableParams = "";
                                    if (complexParts[1].Contains("ec"))
                                        tableParams += " equalcells=1";
                                    if (complexParts[1].Contains("nb"))
                                        tableParams += " noborder=1";

                                    finalLineParts.Add($"\n[{newHeldListOrTable}{tableParams}]");
                                }
                                else
                                {
                                    if (complexParts[1].Contains("or"))
                                        newHeldListOrTable = "olist";
                                    else newHeldListOrTable = "list";

                                    finalLineParts.Add($"\n[{newHeldListOrTable}]");
                                }

                                /// for now
                                //finalLineParts.Add(lPart);
                            }

                            /// complex others
                            ///     url=  q=  th=  td=
                            else if (lPart.RemovePlainText().Contains("="))
                            {
                                string[] complexParts = lPart.LineBreak('=', true, true);
                                string refTag = complexParts[0].Substring(1, complexParts[0].Length - 2);
                                string partArgs = complexParts[1];

                                /// url= :  q= : 
                                if (partArgs.RemovePlainText().Contains(":"))
                                {
                                    string[] arguments = partArgs.LineBreak(':', false, true);
                                    arguments[0] = arguments[0].RemovePlainText(true);
                                    arguments[1] = arguments[1].RemovePlainText(true);

                                    if (refTag == "q")
                                        finalLineParts.Add($"[quote={arguments[0]}]{arguments[1]}[/quote]");
                                    else finalLineParts.Add($"[url={arguments[0]}]{arguments[1]}[/url]");
                                }
                                /// th= ,  td= , 
                                else
                                {
                                    string[] clms = new string[1];
                                    if (partArgs.RemovePlainText().Contains(','))
                                        clms = partArgs.LineBreak(',', true, true);
                                    else clms[0] = partArgs;

                                    keepResultsSeparatedQ = clms.Length > 1;
                                    for (int cx = 0; cx < clms.Length; cx++)
                                    {
                                        bool isFirstClmQ = cx == 0, isLastClmQ = cx == clms.Length - 1;

                                        string rowText = "";
                                        if (isFirstClmQ)
                                            rowText += "[tr]";
                                        else rowText += $"{Ind34}";

                                        rowText += $"{Ind14}[{refTag}]{clms[cx].RemovePlainText(true)}[/{refTag}]{Ind14}";

                                        if (isLastClmQ)
                                            rowText += "[/tr]";

                                        if (rowText.IsNotNE())
                                            finalLineParts.Add($"\n{Ind24}{rowText}");
                                    }
                                    releaseHeldListOrTableQ = false;
                                }
                            }

                            /// simple commands
                            else
                            {
                                /// own liners
                                ///     $h  $hr
                                if (lPart.StartsWith("$h"))
                                {
                                    if (lPart.StartsWith("$hr"))
                                        finalLineParts.Add("\n[hr][/hr]");
                                    else
                                    {
                                        string headerRef = lPart.RemovePlainText();
                                        int headerLevel = headerRef.CountOccuringCharacter('h');

                                        finalLineParts.Add($"\n[h{headerLevel}]{lPart.RemovePlainText(true)}[/h{headerLevel}]");
                                    }
                                }
                                /// all others
                                else
                                {
                                    string simRef = lPart.RemovePlainText();
                                    string tag = simRef.Trim() switch 
                                    {
                                        "$s" => "strike",
                                        "$sp" => "spoiler",
                                        "$np" => "noparse",
                                        "$c" => "code",
                                        _ => simRef.Replace("$", "")
                                    };
                                    bool isNewlineQ = tag == "nl", isListItemQ = tag == "*", isIndentQ = tag.StartsWith("d"), isRegularQ = tag.Equals("r");

                                    /// IF value and simple steam format: ...; ELSE (simple steam format only) 
                                    if (lPart.Contains('"'))
                                    {
                                        if (isListItemQ || isNewlineQ || isIndentQ)
                                        {
                                            if (isListItemQ)
                                                finalLineParts.Add($"\n{Ind24}[{tag}]{lPart.RemovePlainText(true)}");
                                            else if (isNewlineQ)
                                                finalLineParts.Add($"\n{lPart.RemovePlainText(true)}");
                                            else finalLineParts.Add($"{tag.Replace("d", "  ")}{lPart.RemovePlainText(true)}");
                                        }
                                        else
                                        {
                                            if (isRegularQ)
                                                finalLineParts.Add($"{lPart.RemovePlainText(true)}");
                                            else finalLineParts.Add($"{tagsFrontPile}[{tag}]{lPart.RemovePlainText(true)}[/{tag}]{tagsBackPile}");

                                            tagsBackPile = "";
                                            tagsFrontPile = "";
                                        }
                                    }
                                    else
                                    {
                                        if (isListItemQ || isNewlineQ || isIndentQ)
                                        {
                                            if (isListItemQ)
                                                finalLineParts.Add($"\n{Ind24}[{tag}]");
                                            else if (isNewlineQ)
                                                finalLineParts.Add("\n");
                                            else finalLineParts.Add($"{tag.Replace("d", "  ")}");
                                        }
                                        else
                                        {

                                            if (!isRegularQ)
                                            {
                                                tagsFrontPile += $"[{tag}]";
                                                tagsBackPile = $"[/{tag}]" + tagsBackPile;
                                            }
                                            else
                                            {
                                                tagsFrontPile = "";
                                                tagsBackPile = "";                                                
                                            }
                                        }
                                    }

                                    if (isListItemQ)
                                        releaseHeldListOrTableQ = false;
                                }
                            }

                            // string compile per parts
                            if (finalLineParts.HasElements() && isEndQ)
                            {
                                string newLine = "";
                                foreach (string finLinP in finalLineParts)
                                {
                                    if (finLinP.IsNotNE())
                                    {
                                        if (!keepResultsSeparatedQ)
                                            newLine += $"\"{finLinP}\"";
                                        else editedLines.Add($"\"{finLinP}\"");
                                    }
                                }
                                if (!keepResultsSeparatedQ && newLine.IsNotNE())
                                    editedLines.Add(newLine);
                            }
                            /// releasing held lists or tables  //  assigning new held lists or tables
                            if (isEndQ)
                            {
                                if (heldListOrTable.IsNotNE())
                                {
                                    if (releaseHeldListOrTableQ)
                                        editedLines.Insert(0, $"\"\n[/{heldListOrTable}]\"");
                                    else if (lastLineQ)
                                        editedLines.Add($"\"\n[/{heldListOrTable}]\"");

                                    if (releaseHeldListOrTableQ || lastLineQ)
                                        heldListOrTable = "";
                                }
                                if (newHeldListOrTable.IsNotNEW())
                                    heldListOrTable = newHeldListOrTable;
                            }
                        }
                    }
                    else editedLines.Add(line);

                    return editedLines.ToArray();
                }
            }

            return finalLines.ToArray();
        }
    }
}
