using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Minimal;
using static ConsoleFormat.Base;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp.Layout
{
    public static class LibrarySearch
    {
        static ResLibrary _resLibrary;
        static readonly char subMenuUnderline = ':';
        static bool rud_searchAlpha = false;
        const string findKey = "find:";
        static string findPhrase = "";

        public static void GetResourceLibraryReference(ResLibrary resourceLibrary)
        {
            _resLibrary = resourceLibrary;
        }
        public static void OpenPage()
        {
            bool exitSearchPageQ = false;
            do
            {
                Program.LogState("Library Search");
                Clear();
                Title($"Library Search (Rudimentary)", cTHB, 1);
                FormatLine($"{Ind24}WIP .. At the moment, allows browsing and viewing the library's shelves");
                NewLine(2);
                // page layout standards end here (for now)

                /// this for now
                exitSearchPageQ = SubPage_RudimentarySearching();
                /// NOTE :: Page Layout buffer dimensions have been changed to accomadate the rudimentary setup. Remember to reset once official library search page is completed.
            }
            while (!exitSearchPageQ);
        }

        static bool SubPage_RudimentarySearching()
        {
            bool exitPagesQ = false;
            bool libraryIsSetup = false;
            if (_resLibrary != null)
                libraryIsSetup = _resLibrary.IsSetup();

            if (libraryIsSetup)
            {
                NewLine(4);

                // find phrase search
                List<ResContents> filteredContents = new();
                if (findPhrase.IsNotNEW())
                {
                    foreach (ResContents resCon in _resLibrary.Contents)
                    {
                        string megaRCString = "";
                        if (resCon.ConBase.IsSetup())
                            megaRCString += resCon.ConBase.EncodeFirstGroup().Replace(DataHandlerBase.Sep, "\n") + "\n";

                        if (resCon.ConAddits.HasElements())
                            foreach (ContentAdditionals rca in resCon.ConAddits)
                                megaRCString += rca.EncodeSecondGroup().Replace(DataHandlerBase.Sep, "\n") + "\n";

                        if (resCon.ConChanges.HasElements())
                            foreach (ContentChanges rcc in resCon.ConChanges)
                                megaRCString += rcc.EncodeThirdGroup().Replace(DataHandlerBase.Sep, "\n") + "\n";

                        if (megaRCString.ToLower().Contains(findPhrase.ToLower()))
                            filteredContents.Add(resCon);
                    }
                }
                else filteredContents.AddRange(_resLibrary.Contents);

                // alphabetically sort res contents
                List<ResContents> resContents = new();
                if (rud_searchAlpha)
                {
                    List<string> resStrs = new();
                    foreach (ResContents rc in filteredContents)
                        resStrs.Add($"{rc.ContentName}{DataHandlerBase.Sep}{rc.ShelfID}");
                    resStrs = resStrs.ToArray().SortWords();

                    ResContents[] resArr = new ResContents[resStrs.Count];
                    foreach (ResContents src in filteredContents)
                    {
                        bool foundSpotQ = false;
                        int ixPos = 0;
                        foreach (string sort in resStrs)
                        {
                            if (sort.Equals($"{src.ContentName}{DataHandlerBase.Sep}{src.ShelfID}"))
                            {
                                foundSpotQ = true;
                                break;
                            }
                            else ixPos++;
                        }

                        if (foundSpotQ)
                            resArr[ixPos] = src;
                    }
                    resContents.AddRange(resArr);
                }
                else resContents.AddRange(filteredContents.ToArray());

                // print all ResContents available
                HorizontalRule(subMenuUnderline);
                if (resContents.HasElements())
                {
                    for (int rx = 0; rx < resContents.Count; rx++)
                    {
                        ResContents rc = resContents[rx];
                        string caNccText = "";
                        if (rc.ConAddits.HasElements())
                            caNccText += $"[{rc.ConAddits.Count}] adts; ";
                        if (rc.ConChanges.HasElements())
                            caNccText += $"[{rc.ConChanges.Count}] updts;";

                        HoldNextListOrTable();
                        Table(Table4Division.KCSmall_TDMediumSmall, $"[#{rc.ShelfID}]", '|', rc.ContentName, $"{rc.ConBase.VersionNum}", caNccText);
                        Text(LatestTablePrintText, rx % 2 == 1 ? Color.DarkGray : Color.Gray);
                    }
                }
                else FormatLine($"{Ind14}No results matched '{findPhrase}' ... ");
                HorizontalRule(subMenuUnderline);

                Wait(0.05f);
                SetCursorPosition(0, 0);
                Wait(0.05f);

                NewLine(4);
                FormatLine("Press [Enter] to exit search page. Enter any key to toggle sorting style. ", ForECol.Accent);
                FormatLine($"Precede text with '{findKey}' to act as a search filter (case-insensitive).", ForECol.Accent);
                Format($"Enter a number to select a content from below: ");

                // input validation
                if (StyledInput("###").IsNotNE())
                {
                    if (int.TryParse(LastInput, out int rcOptIx))
                    {
                        if (rcOptIx.IsWithin(0, _resLibrary.Contents.Count - 1))
                        {
                            /**
                             >> Example.And.Labelling
				                .......................................
				                [v1.3] Garlic Clove		
					                d11 i69 t46 t47
				
					                ++	[v1.3] t47 >> Stripped Garlic Clove Dust (d12)					
					                ^^	[v1.5] d11 - Brightened the shadows of the texture.
					                ^^  [v1.6] Stripped Garlic Clove Dust (d12) - Brightened the shadows of the texture.
				                .......................................				
				
				                .......................................
				                {aaa} {bbb}		
					                {ccc ccc}
				
					                ++ {aaa} {ddd}					
					                ^^ {eee} {fff}
					                ^^ {eee} {fff}
				                .......................................
					                "aaa"		Verison (added version)
					                "bbb"		Content Name
					                "ccc"		Related Data IDs (separated by spaces; sorted order)
					                "ddd"		Additional Related Content
					                "eee"		Version (updated version)
					                "fff"		Update information and changes
                             */

                            ResContents rc = _resLibrary.Contents[rcOptIx];

                            // display
                            Clear();
                            Title($"Shelf Index #{rcOptIx}");
                            HorizontalRule('.');

                            FormatLine($"[{rc.ConBase.VersionNum}] {rc.ContentName}");
                            FormatLine($"{Ind24}{rc.ConBase.DataIDString}");

                            if (rc.ConChanges.HasElements() || rc.ConAddits.HasElements())
                                NewLine();
                            if (rc.ConAddits.HasElements())
                                foreach (ContentAdditionals ca in rc.ConAddits)
                                {
                                    string optName = ca.OptionalName.IsNE() ? "" : $"{ca.OptionalName} ";
                                    FormatLine($"{Ind24}++  [{ca.VersionAdded}] {ca.RelatedDataID} >> {optName}({ca.DataIDString})");
                                }
                            if (rc.ConChanges.HasElements())
                                foreach (ContentChanges cc in rc.ConChanges)
                                {
                                    string intName = cc.InternalName.IsNE() ? "" : $"{cc.InternalName} ";
                                    FormatLine($"{Ind24}^^  [{cc.VersionChanged}] {intName}({cc.RelatedDataID}) - {cc.ChangeDesc}");
                                }

                            NewLine();
                            HorizontalRule('.', 1);
                            Format("Return...");
                            Pause();
                        }
                    }
                    else
                    {
                        if (LastInput.StartsWith(findKey))
                        {
                            if (LastInput.Substring(findKey.Length).IsNotNEW())
                            {
                                findPhrase = LastInput.Substring(findKey.Length);
                                Format($"{Ind24}Searching for any contents containing '{findPhrase}'...", ForECol.Correction);
                            }
                            else
                            {
                                Format($"{Ind24}Search filter '{findPhrase}' cleared...", ForECol.Correction);
                                findPhrase = "";
                            }
                        }
                        else
                        {
                            rud_searchAlpha = !rud_searchAlpha;
                            if (rud_searchAlpha)
                                Format($"{Ind24}Contents will be sorted and listed in alphabetical order...  ", ForECol.Correction);
                            else Format($"{Ind24}Contents will be sorted by shelf ID...  ", ForECol.Correction);

                        }
                        Pause();
                    }
                }
                else exitPagesQ = true;
            }
            else
            {
                Format($"{Ind24}Library is empty.");
                Pause();
                exitPagesQ = true;
            }
            return exitPagesQ;
        }
    }
}
