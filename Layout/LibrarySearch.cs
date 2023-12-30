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

        // LIBRARY SEARCH fields
        static SearchOptions _searchOpts, _prevSearchOpts;
        static string _searchArg, _prevSearchArg;
        static SearchResult[] searchResults;
        static int maximumResults, prevMaximumResults;

        public static void ClearCookies()
        {
            _searchOpts = new SearchOptions();
            _prevSearchOpts = new SearchOptions();
            searchResults = Array.Empty<SearchResult>();
            _searchArg = null;
            _prevSearchArg = null;
        }
        public static void GetResourceLibraryReference(ResLibrary resourceLibrary)
        {
            _resLibrary = resourceLibrary;
        }
        public static void OpenPage()
        {
            bool exitSearchPageQ; // = false;
            do
            {
                BugIdeaPage.OpenPage();
                
                Program.LogState("Library Search");
                Clear();
                Title($"Library Search", cTHB, 0);
                FormatLine($"{Ind24}Allows browsing, searching, and viewing of the library's shelves", ForECol.Accent);
                //FormatLine($"{Ind24}WIP .. At the moment, allows browsing and viewing the library's shelves");
                //NewLine(2);

                /// All the library searchin' and browsin' happens right here
                exitSearchPageQ = SubPage_LibrarySearch();


                // page layout standards end here (for now)
                /// this for now
                //exitSearchPageQ = SubPage_RudimentarySearching();
                /// NOTE :: Page Layout buffer dimensions have been changed to accomadate the rudimentary setup. Remember to reset once official library search page is completed.
            }
            while (!exitSearchPageQ);
        }

        static bool SubPage_LibrarySearch()
        {
            bool exitMainPageQ = false, exitSearchOptsPageQ = true, exitEntryViewPageQ = true;
            bool libraryIsSetup = false;
            if (_resLibrary != null)
                libraryIsSetup = _resLibrary.IsSetup();


            if (libraryIsSetup)
            {
                /** LIBRARY SEARCH PAGE PLANS
                  
                [FROM 'DESIGN DOC']
                 >> Identified.categories.display.example.and.labelling
			    ..	Resource Pack Contents
				    -> Structure
					    ......................................
					    Search :: {aaa}	({bbb})
					
					    {ccc} [{ddd}] {eee} 
					    {ccc} [{ddd}] {fff} {ggg}
					    ......................................
						    "aaa"	Search keywords
						    "bbb"	(Number of) Returned Results
						    "ccc"	Result Relevance Number
						    "ddd"	Result Source Description Key
						    "eee"	Relevant Content Name
 						    "fff"	Excerpt from Relevant Content
						    "ggg"	Content Name relating to Excerpt from Relevant Content 
			
				    -> NOTES
				    .	Search Keywords
						    AKA - Keyword, Keywords
						    The characters to match information in any content with as a filter. Support alphanumeric characters and symbols using UTF-8 encoding. The character limit for keywords is 30.
						
				    .	Returned Results 
						    AKA - Results
						    Displays the number of results returned that matched in anyway with the keyword through any information from content available in the library. There can only be up to 50 results gathered at once. There are four outcomes when stating results returned:
							    0 returned: "no results"
							    1 returned: "showing 1 result"
							    2+ returned: "showing {x} results"
								    Where {x} represents the number of results shown on a page.
							    too many returned: "showing {a}~{b} of {x} results"
								    Too many results is a condition where the number of matching results are too much to be showed on one page. So then the results will be shown in groups of a set number (10 for example). {a} is the relevance number of the result at the top of the page, and {b} the relevance number of the results at the bottom of the page.
						
				    .	Result Relevance Number
						    AKA - Relevance Number (RelNum)
						    A whole number starting from one (1) that indicates the relevancy of the results of the search. The range of the RelNum is tied to the number of results: 1 to 50. 
						    How is relevance determined? Two factors determine relevance: Keyword matching exactly or contained within, and the RSDK relevance.				
							    Keyword match Relevance:
								    1st - Exact match with keyword
								    2nd - Containing keyword
							    RSDK relevance can be found under its section.
						    ALSO
						    - Besides ordering through relevance, the RelNum is also determined through alphanumeric order (symbols > numbers > letters) and grouping based on individual content (All results pertaining to a content, are grouped under each other rather then being scattered about).
							
					
				    .	Result Source Description Key
						    AKA - RSDK
						    The RSDK is a three-character identifier used to describe where the information relevant to the search result is pulled from regarding any content. The keys and definitions are as follows:
							    Nam: Content Name 
							    RDI: Related Data IDs
							    ARC: Additional Related Content
							    Upd: Updated information and changes
							    Ver: Version Number, generalized
						    ALSO
						    - The order of the RSDKs as listed also determing their relevance. [Nam] has greater prevalance over all other RSDKs, and [Ver] has least prevalance under all RSDKs.
					
				    .	Relevant Content Name
						    AKA - Source Name
						    The name of the content that is related to the keyword in search. Only Content Name information is plainly displayed in a search.
						
				    .	Excerpt from Relevant Content
						    AKA - Excerpt (RelEx)
						    When a keyword matches information under a named content (and not the content's name itself), the keyword followed by information from the section of information provides context to where this matching information was found. This excerpt may take on a few formats:
							    Context: keyword followed by a few other words from source
							    Single: only the word that matches or contains keyword
							    Combo: the 'Single' info followed by quoted 'Context' info
						    ALSO
						    - All RelEx is followed by an Excerpt Source.
						    - If the Context info is too lengthy, it is parsed and denoted to be so with the '~' character afterwards.
						
				    .	Content Name relating to Excerpt from Relevant Content
						    AKA - Excerpt Source (SourceEx)
						    The associated Source Name that indicates where information for the preceding Excerpt is sourced. The Source Name may not be shortened. There is only one format for the SourceEx: 
							    (from '{ContentName}')
							
			
				    > Search using letters
					    ......................................
					    Search :: tomato 	(showing 6 results)
					
					    1 [Nam]	Tomato
					    2 [ARC]	Tomato Stem Dust (from 'Tomato')
					    3 [Upd]	tomato more delici~ (from 'Tomato')
					    4 [Nam]	Canned Tomato
					    5 [Nam]	Steamed Tomato				
					    6 [Upd]	tomato soggier loo~ (from 'Steamed Tomato')				
					    ......................................					
					
				    > Search using numbers
				    .	Ex1 (plain number)
					    ......................................
					    Search :: 14		(showing 5 results)
					
					    1 [RDI]	t14 (from 'Rice')
					    2 [RDI]	i14 (from 'Turnip')
					    3 [RDI]	i214 (from 'Lettuce')
					    4 [Ver]	1.14 (from 'Lettuce')
					    5 [RDI]	t114 (from 'Peanut')					
					    ......................................				
				
				    .	Ex2 (version number)
					    ......................................
					    Search :: 1.1		(showing 6 results)
					
					    1 [Ver]	1.1 (from 'Apple')
					    2 [Ver]	1.1 (from 'Blueberry')
					    3 [Ver]	1.1 (from 'Passion Fruit')
					    4 [Upd]	1.1 "Increased the contra~" (from 'Strawberry')
					    5 [Ver]	1.10 (from 'Apple Sauce Block')
					    6 [ARC]	1.11 "AppleSauce_Chunks" (from 'Apple Sauce Block')
					    ......................................
					    NOTES
						    - The syntax varies slightly, where the version number is unbracketed, and the exceprt (quoted) and source name exists within the same parentheses.
						    - Results #4 and #6 are version numbers retreived from the 'Update information and changes' and 'Additional Related Content' sections respectively.

				    > Search using alphanumeric characters
					    ......................................
					    Search :: t4		(showing 3 results)
					
					    1 [RDI]	t46 (from 'Garlic Clove')
					    2 [RDI]	t47 (from 'Garlic Clove')
					    3 [ARC]	t49 "Garlic Stem" (from 'Garlic Clove')
					    ......................................



                [ADDITIONAL IDEAS]
                > Landing view
                    - Two options: Display the first few items by shelf ID (same every time) [bzzt!]    -OR-     Select a number of random shelf IDs to display (different every time) [ding!]

                > Search filter options and setup
                    - Available filter options
                        . Case sensitivity (on/off) [default:On]
                        . Source category searches (on/off) [default:On (for all)]
                            . Categories: Content Name (Nam), Related Data IDs (RDI), Additional Related Content (ARC), Updated info (Upd), Version Number (Ver)
                        . Ignore Relevance, order by shelf ID instead (on/off) [default:Off]
                        . Reset filter options (single command)
                    - Implementations: A struct
                    - Things that may changes
                        . Categories reduced (-2): Base Content (Bse), Additional Related Content (ARC), Updated info (Upd)
                            . Base Contents adopts 'Content Name' and 'Related Data IDs' into a group
                            . 'Version Number' is discarded

                > Tangent Browsing (See Also)
                    - Types of tangent browsing to consider (all can be displayed simultaneously): search query, shelf, dataID
                        . Shelf tangent
                            When an entry is selected, the next item on shelf and previous item can be referenced
                        . Search Query tangent
                            When an entry is selected, other search results that follow the entry can be referenced
                        . Data ID tangent [redact]
                            When an entry is selected, other entries containing data IDs of the current entry can be referenced 

                > Single item minor edit [redact]
                    - A selected entry in viewership can be edited to change the information of the object. The editable sections and properties are listed below
                        . Content Base Information [2*] : Content Name, content IDs (only with no additional contents)
                        . Addition Information [1ea] : Addition Content(s) information (each: optional name)
                        . Updated Information [1ea] : Each: change description

                > Search query display changes to note
                    - Two types of searches alphanumeric, and numeric
                    - Alpha-numeric search display examples
                        ......................................
					    Search :: tomato 	(showing 6 results)
					
					    1 [Bse]	Tomato
					    2 [Adt]	"Tomato Stem Dust" (from 'Tomato')
					    3 [Upd]	"~tomato more delici~" (from 'Tomato')
					    4 [Bse]	Canned Tomato
					    5 [Bse]	Steamed Tomato				
					    6 [Upd]	"~tomato soggier loo~" (from 'Steamed Tomato')				
					    ......................................		
                        ......................................
					    Search :: t4		(showing 3 results)
					
					    1 [Bse]	t46 (from 'Garlic Clove')
					    2 [Bse]	t47 (from 'Garlic Clove')
					    3 [Adt]	t49 "Garlic Stem" (from 'Garlic Clove')
					    ......................................

                    - Numeric search display example
                        ......................................
					    Search :: 14		(showing 5 results)
					
					    1 [Bse]	1.14 (from 'Lettuce')
					    2 [Bse]	i214 (from 'Lettuce')
					    3 [Bse]	t114 (from 'Peanut')					
					    4 [Bse]	t14 (from 'Rice')
					    5 [Bse]	i14 (from 'Turnip')
					    ......................................	
                    
                    - Result sorting and shaping explained
                        1st Alphabetic order, exact matching (in search 'cut', 'Cut' is exact, 'Cutter' or 'Hot Cut' is not exact)
                            . Category: Base, Additional, Update [matching word output determined by 'ContentName' match]
                                1st Within Base sort (Content Name > Data IDs > Version Number) 
                                2nd Within Additional sort (Opt Name > Data IDs > RelData ID* > Version Number) 
                                3rd Within Updated sort (Change Description > RelData ID* > Version Number)
                        2nd Alphabetic order, partial matching 
                            . Category: Base, Additional, Update [matching word output determined by 'ContentName' match]
                                1st Within Base sort (Content Name > Data IDs > Version Number) 
                                2nd Within Additional sort (Opt Name > Data IDs > RelData ID* > Version Number) 
                                3rd Within Updated sort (Change Description > RelData ID* > Version Number)
                        NOTE : RelData ID* is only checked when the Base Content Category is exempted from search options
                   

                 */

                // setup
                const string searchKey = DataHandlerBase.Sep;
                const string searchOptsKey = searchKey + searchKey;
                const string filterCaseSnKey = "AaB", filterIgnRelKey = "123";
                const int noEntNum = 0, noVary = 0;
                int viewEntNum = noEntNum, varyEntryNum = noVary, varyShelfNum = noVary;
                string entryNavigationIssue = null;
                prevMaximumResults = maximumResults;
                maximumResults = 50 + HSNL(0, 49);
                if (!_searchOpts.IsSetup())
                    _searchOpts = new SearchOptions(true, false, SourceContents.All);


                // ++  BROWSE MENU  ++
                NewLine();
                FormatLine($"{Ind14}Start a search with '{searchKey}' character. Enter the entry number to view.", ForECol.Accent);
                FormatLine($"{Ind14}Enter '{searchOptsKey}' to view search options. Press [Enter] to exit page.", ForECol.Accent);
                Format($"{Ind14}Search or view entries >> ");
                const string browseMenuPlaceholder = searchKey + "#";
                GetCursorPosition();
                NewLine(2);
                // results query
                if (_searchArg != _prevSearchArg || !_searchOpts.Equals(_prevSearchOpts) || maximumResults != prevMaximumResults)
                {
                    searchResults = _resLibrary.SearchLibrary(_searchArg, _searchOpts, maximumResults);
                    _prevSearchArg = _searchArg;
                    _prevSearchOpts = _searchOpts;
                    prevMaximumResults = maximumResults;

                    /// are reset anytime results are changed
                    varyEntryNum = noVary;
                    varyShelfNum = noVary;
                    viewEntNum = noEntNum;
                }




                // ++  SEARCH RESULTS  ++
                HorizontalRule(subMenuUnderline);
                /// search query info
                if (_searchArg.IsNotNEW())
                {
                    /// search argument
                    Format("Search :: ");
                    Format(_searchArg, ForECol.Highlight);

                    /// result count
                    int resultCount = 0;
                    if (searchResults.HasElements())
                        resultCount = searchResults.Length;
                    FormatLine($"{Ind34}(showing '{resultCount}' results | Max '{maximumResults}')", ForECol.Accent);

                    /// search filters
                    string activeSearchOpts = "";
                    for (int so = 0; so < 5; so++)
                    {
                        string activeOpt = so switch
                        {
                            0 => _searchOpts.caseSensitiveQ ? filterCaseSnKey : "",
                            1 => _searchOpts.ignoreRelevanceQ ? filterIgnRelKey : "",
                            2 => _searchOpts.IsUsingSource(SourceCategory.Bse) ? SourceCategory.Bse.ToString() : "",
                            3 => _searchOpts.IsUsingSource(SourceCategory.Adt) ? SourceCategory.Adt.ToString() : "",
                            4 => _searchOpts.IsUsingSource(SourceCategory.Upd) ? SourceCategory.Upd.ToString() : "",
                            _ => ""
                        };

                        if (activeOpt.IsNotNE())
                            activeSearchOpts += $"[{activeOpt}] ";
                    }
                    FormatLine($"{Ind14}{activeSearchOpts}", ForECol.Accent);
                }
                else FormatLine($"Empty search: Results chosen randomly ('{maximumResults}' items)", ForECol.Accent);
                NewLine();
                /// results
                if (searchResults.HasElements())
                {
                    /** > Search query display examples
                    - Alpha-numeric search display examples
                        ......................................
					    Search :: tomato 	(showing 6 results)
					
					    1 [Bse]	Tomato
					    2 [Adt]	"Tomato Stem Dust" (from 'Tomato')
					    3 [Upd]	"~tomato more delici~" (from 'Tomato')
					    4 [Bse]	Canned Tomato
					    5 [Bse]	Steamed Tomato				
					    6 [Upd]	"~tomato soggier loo~" (from 'Steamed Tomato')				
					    ......................................		
                        ......................................
					    Search :: t4		(showing 3 results)
					
					    1 [Bse]	t46 (from 'Garlic Clove')
					    2 [Bse]	t47 (from 'Garlic Clove')
					    3 [Adt]	t49 "Garlic Stem" (from 'Garlic Clove')
					    ......................................

                    - Numeric search display example
                        ......................................
					    Search :: 14		(showing 5 results)
					
					    1 [Bse]	1.14 (from 'Lettuce')
					    2 [Bse]	i214 (from 'Lettuce')
					    3 [Bse]	t114 (from 'Peanut')					
					    4 [Bse]	t14 (from 'Rice')
					    5 [Bse]	i14 (from 'Turnip')
					    ......................................	                     
                     ****/

                    for (int rx = 0; rx < searchResults.Length; rx++)
                    {
                        int rNum = rx + 1;
                        SearchResult result = searchResults[rx];

                        /// result number, source type
                        Format($"{rNum,2} [{result.sourceType}]{Ind14}", ForECol.Accent);
                        HoldWrapIndent(true);

                        /// the possible variations of 'matching text' that may appear in results (for highlighting)
                        string highlightStr = HighlightSearchArg(_searchArg, result.matchingText);                 

                        /// the matching text and highlighting -- IF 'match is content': format 'ContentName'; ELSE format 'matchingText' (from 'ContentName')
                        if (result.matchingText == result.contentName)
                        {
                            //Format($"{result.contentName}", ForECol.InputColor);
                            ChangeNextHighlightColors(ForECol.Highlight, ForECol.InputColor);
                            Highlight(false, result.contentName, highlightStr);
                        }
                        else
                        {
                            ChangeNextHighlightColors(ForECol.Highlight, ForECol.InputColor);
                            Highlight(false, result.matchingText, highlightStr);

                            //Format($" (from '{result.contentName}')");
                            ChangeNextHighlightColors(ForECol.Accent, ForECol.Normal);
                            Highlight(false, $" (from '{result.contentName}')", result.contentName);
                        }
                        NewLine();

                        //FormatLine($"\tMT : {result.matchingText}", ForECol.Accent);
                        HoldWrapIndent(false);
                    }
                }
                else FormatLine("No results found...", ForECol.Accent);
                HorizontalRule(subMenuUnderline);
                // ++  SEARCH RESULTS  ++



                // ++  USER INPUT ++
                SetCursorPosition(0, 0);
                SetCursorPosition();
                string uInput = StyledInput(browseMenuPlaceholder);
                if (uInput.IsNotNEW())
                {
                    if (int.TryParse(uInput, out int entNum))
                    {
                        if (entNum.IsWithin(1, maximumResults) && searchResults.HasElements())
                        {
                            exitEntryViewPageQ = false;
                            viewEntNum = entNum;
                        }
                        else viewEntNum = noEntNum;
                    }
                    if (uInput == searchOptsKey)
                        exitSearchOptsPageQ = false;
                    if (uInput.CountOccuringCharacter(searchKey[0]) == 1 && uInput.StartsWith(searchKey))
                    {
                        _searchArg = uInput[1..];
                        if (_searchArg.IsNEW())
                            _searchArg = null;
                    }
                }
                
                /// EXIT CONTROL -- this bool's loop is in parent method
                if (exitEntryViewPageQ && exitSearchOptsPageQ && uInput.IsNEW())                
                    exitMainPageQ = true;



                // ++ SUB PAGES ++ 
                /// SEARCH OPTIONS PAGE
                while (!exitSearchOptsPageQ)
                {
                    BugIdeaPage.OpenPage();

                    Program.LogState("Library Search|Search Options");
                    Clear();
                    Title("Search Options", cTHB, 0);
                    FormatLine($"{Ind14}Edit search query filters to refine the results of a library search.", ForECol.Accent);

                    NewLine();
                    Title("Search Filter Options");
                    for (int sfx = 0; sfx < 6; sfx++)
                    {
                        string filterSymbol = "";
                        string filterOptName = "";
                        bool filterStatusOnQ = false;

                        switch (sfx)
                        {
                            case 0:
                                filterSymbol = filterCaseSnKey;
                                filterOptName = "Case Sensitivity";
                                filterStatusOnQ = _searchOpts.caseSensitiveQ;
                                break;

                            case 1:
                                filterSymbol = filterIgnRelKey;
                                filterOptName = "Ignore Relevance (Order By Shelf#)";
                                filterStatusOnQ = _searchOpts.ignoreRelevanceQ;
                                break;

                            case 2:
                                filterSymbol = SourceCategory.Bse.ToString();
                                filterOptName = "Source: Base Contents";
                                filterStatusOnQ = _searchOpts.IsUsingSource(SourceCategory.Bse);
                                break;

                            case 3:
                                filterSymbol = SourceCategory.Adt.ToString();
                                filterOptName = "Source: Additional Contents";
                                filterStatusOnQ = _searchOpts.IsUsingSource(SourceCategory.Adt);
                                break;

                            case 4:
                                filterSymbol = SourceCategory.Upd.ToString();
                                filterOptName = "Source: Updated Contents";
                                filterStatusOnQ = _searchOpts.IsUsingSource(SourceCategory.Upd);
                                break;

                            case 5:
                                filterSymbol = _searchOpts.sourceContent.ToString();
                                filterOptName = "Source Type: ";
                                filterOptName += _searchOpts.sourceContent switch
                                {
                                    SourceContents.All => "All",
                                    SourceContents.Ids => "Data IDs Only",
                                    SourceContents.NId => "No Data IDs",
                                    _ => ""
                                };
                                filterStatusOnQ = true;
                                break;
                        }

                        if (filterSymbol.IsNotNEW() && filterOptName.IsNotNEW())
                        {
                            Format($"{sfx + 1}{Ind14}", ForECol.Accent);
                            Format(filterStatusOnQ? $"[{filterSymbol}]" : $"|{filterSymbol}|", filterStatusOnQ? ForECol.Correction : ForECol.Incorrection);
                            FormatLine($"{Ind14}{filterOptName}", filterStatusOnQ ? ForECol.Highlight : ForECol.Normal);
                        }
                    }
                    FormatLine($"{Ind14}Enabled filter options are surrounded in box brackets '[]'.", ForECol.Accent);
                    NewLine();

                    FormatLine($"{Ind14}Enter the number of the search filter option to toggle. Any other key to exit.", ForECol.Accent);
                    Format($"{Ind14}Toggle search filter >> ");
                    string soInput = StyledInput("#");

                    // toggle filter settings here
                    exitSearchOptsPageQ = true;
                    if (soInput.IsNotNE())
                        if (MenuOptions(soInput, out short filterOptNum, "1,2,3,4,5,6".Split(',')))
                        {
                            bool toggledSourceQ = true;
                            exitSearchOptsPageQ = false;
                            switch (filterOptNum)
                            {
                                // case sensitive
                                case 0:
                                    _searchOpts = new(!_searchOpts.caseSensitiveQ, _searchOpts.ignoreRelevanceQ, _searchOpts.sourceContent, _searchOpts.sourcesUsed);
                                    break;
                                // ignore relevance
                                case 1:
                                    _searchOpts = new(_searchOpts.caseSensitiveQ, !_searchOpts.ignoreRelevanceQ, _searchOpts.sourceContent, _searchOpts.sourcesUsed);
                                    break;
                                // base content
                                case 2:
                                    toggledSourceQ = _searchOpts.ToggleSource(SourceCategory.Bse);
                                    break;
                                // additional content
                                case 3:
                                    toggledSourceQ = _searchOpts.ToggleSource(SourceCategory.Adt);
                                    break;
                                // updated content
                                case 4:
                                    toggledSourceQ = _searchOpts.ToggleSource(SourceCategory.Upd);
                                    break;
                                // source content
                                case 5:
                                    _searchOpts.ToggleContent();
                                    break;
                            }

                            if (!toggledSourceQ)
                            {
                                Format($"{Ind24}At least one source category must be enabled.", ForECol.Incorrection);
                                Pause();
                            }
                        }
                }
                /// ENTRY VIEW PAGE
                while (!exitEntryViewPageQ && viewEntNum > noEntNum)
                {
                    BugIdeaPage.OpenPage();

                    /// toNext/PrevResults,  toNext/PrevShelf
                    const string toNextRs = ">", toPrevRs = "<", toNextSh = ">>", toPrevSh = "<<";
                    int toNextEntMod = noVary, toPrevEntMod = noVary;

                    Program.LogState("Library Search|Entry View");
                    Clear();
                    Title("Entry View", cTHB, 0);
                    FormatLine($"{Ind14}View content's information and tangent to other search results and contents.", ForECol.Accent);
                    NewLine(1);

                    /// navigation menu
                    FormatLine($"{Ind14}Previous / Next Result: '{toPrevRs}' / '{toNextRs}'. Previous / Next Shelf: '{toPrevSh}' / '{toNextSh}'.", ForECol.Accent);
                    Format($"{Ind14}Navigate To :: ");
                    GetCursorPosition();
                    if (entryNavigationIssue.IsNotNE())
                        Format($"{Ind34}[X] {entryNavigationIssue}.", ForECol.Incorrection);
                    NewLine(2);

                    // ++ DISPLAY ENTRY / CONTENT ++
                    int entShelfID = searchResults[viewEntNum - 1 + varyEntryNum].shelfID;
                    if (varyShelfNum != noVary)
                        entShelfID += varyShelfNum;
                    ResContents entry = _resLibrary.Contents[entShelfID];
                    if (entry.IsSetup())
                    {
                        Title($"Shelf #{entry.ShelfID}", subMenuUnderline, 1);

                        // base
                        Format($"[{entry.ConBase.VersionNum}] ", ForECol.Accent);
                        FormatLine(entry.ContentName, ForECol.Highlight);
                        FormatLine($"{Ind24}{entry.ConBase.DataIDString}");

                        // additional
                        if (entry.ConAddits.HasElements())
                        {
                            NewLine();
                            FormatLine($"{Ind14}Additionals".ToUpper(), ForECol.Heading2);
                            for (int ax = 0; ax <  entry.ConAddits.Count; ax++)
                            {
                                ContentAdditionals conAddit = entry.ConAddits[ax];
                                Format($"{Ind14}[{conAddit.VersionAdded}] ", ForECol.Accent);
                                Format($"{conAddit.OptionalName} ({conAddit.DataIDString})".Trim(), ForECol.Highlight);
                                FormatLine($" by '{conAddit.RelatedDataID}'");

                                //if (ax + 1 < entry.ConAddits.Count)
                                //    FormatLine($"{Ind14}- - - - -", ForECol.Accent);
                            }
                        }

                        // updates
                        if (entry.ConChanges.HasElements())
                        {
                            NewLine();
                            FormatLine($"{Ind14}Updates".ToUpper(), ForECol.Heading2);
                            for (int ux = 0; ux < entry.ConChanges.Count; ux++)
                            {
                                ContentChanges conChanges = entry.ConChanges[ux];
                                Format($"{Ind14}[{conChanges.VersionChanged}] ", ForECol.Accent);
                                FormatLine($"'{conChanges.RelatedDataID}'");
                                FormatLine($"{Ind24}{conChanges.ChangeDesc}", ForECol.Highlight);

                                //if (ux + 1 < entry.ConChanges.Count)
                                //    FormatLine($"{Ind14}- - - - -", ForECol.Accent);
                            }
                        }
                        
                        HorizontalRule(subMenuUnderline);
                    }
                    /** >> Example.And.Labelling (from Design Doc) [not closely followed]
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
                    **/



                    // ++ VARIATION FROM INITIAL ENTRY NUMBER ++
                    const char divSeeAlso = '|';
                    NewLine(3);
                    /// see also - adjacent search results
                    if (searchResults.HasElements())
                    {
                        int entNumViewed = viewEntNum + varyEntryNum;
                        Title("Other Search Results", '-', 0);

                        /// get adjacent (non-identical) results
                        string prevRes = $"[{toPrevRs}]{Ind24}", nextRes = $"[{toNextRs}]{Ind24}";
                        SearchResult currResult = searchResults[entNumViewed - 1];
                        bool foundNextQ = false, foundPrevQ = false;

                        while (entNumViewed + toNextEntMod <= searchResults.Length && !foundNextQ)
                        {
                            SearchResult nextResult = searchResults[entNumViewed + toNextEntMod - 1];
                            if (nextResult.contentName == currResult.contentName)
                                toNextEntMod++;
                            else
                            {
                                nextRes += $"#{entNumViewed + toNextEntMod} '{nextResult.contentName}'";
                                foundNextQ = true;
                            }
                        }
                        while (entNumViewed - toPrevEntMod > noEntNum && !foundPrevQ)
                        {
                            SearchResult prevResult = searchResults[entNumViewed - toPrevEntMod - 1];
                            if (prevResult.contentName == currResult.contentName)
                                toPrevEntMod++;
                            else
                            {
                                prevRes += $"#{entNumViewed - toPrevEntMod} '{prevResult.contentName}'";
                                foundPrevQ = true;
                            }
                        }
                        #region oldCode                        
                        //if (entNumViewed + 1 <= searchResults.Length)
                        //{
                        //    SearchResult nextResult = searchResults[entNumViewed];
                        //    nextRes += $"#{entNumViewed + 1} '{nextResult.contentName}'";
                        //}
                        //if (entNumViewed - 1 > noEntNum)
                        //{
                        //    SearchResult prevResult = searchResults[entNumViewed - 2];
                        //    prevRes += $"#{entNumViewed - 1} '{prevResult.contentName}'";
                        //}
                        #endregion

                        /// print adjacent results
                        Table(Table2Division.Even, prevRes, divSeeAlso, nextRes);
                        NewLine();
                    }
                    /// see also - adjacent shelf contents
                    if (_resLibrary.Contents.HasElements())
                    { /// wrapping
                        Title("Other Shelves", '-', 0);

                        /// get adjacent shelves
                        string prevShelf = $"[{toPrevSh}]{Ind24}", nextShelf = $"[{toNextSh}]{Ind24}";
                        if (entShelfID + 1 < _resLibrary.Contents.Count)
                        {
                            ResContents nextCon = _resLibrary.Contents[entShelfID + 1];
                            nextShelf += $"@{nextCon.ShelfID} '{nextCon.ContentName}'";
                        }
                        if (entShelfID - 1 >= 0)
                        {
                            ResContents prevCon = _resLibrary.Contents[entShelfID - 1];
                            prevShelf += $"@{prevCon.ShelfID} '{prevCon.ContentName}'";
                        }

                        /// print adjacent shelves
                        Table(Table2Division.Even, prevShelf, divSeeAlso, nextShelf);
                        HorizontalRule(cBHB);
                    }

                    // input handling
                    SetCursorPosition(0, 0);
                    SetCursorPosition();
                    string navInput = StyledInput(null);
                    if (navInput.IsNotNEW())
                    {
                        entryNavigationIssue = null;

                        /// next/prev result
                        if (navInput.Equals(toNextRs) || navInput.Equals(toPrevRs))
                        {
                            bool toNextQ = navInput.Equals(toNextRs);
                            if (toNextQ)
                            {
                                if (viewEntNum + varyEntryNum + toNextEntMod <= searchResults.Length)
                                    varyEntryNum += toNextEntMod;
                                else entryNavigationIssue = "No next results";
                            }
                            else
                            {
                                if (viewEntNum + varyEntryNum - toPrevEntMod > noEntNum)
                                    varyEntryNum -= toPrevEntMod;
                                else entryNavigationIssue = "No previous results";
                            }
                            varyShelfNum = noVary;
                        }

                        /// next/prev shelf
                        if (navInput.Equals(toNextSh) || navInput.Equals(toPrevSh))
                        {
                            bool toNextQ = navInput.Equals(toNextSh);
                            if (toNextQ)
                            {
                                if (entShelfID + 1 < _resLibrary.Contents.Count)
                                    varyShelfNum += 1;
                                else entryNavigationIssue = "No shelves afterwards";
                            }
                            else
                            {
                                if (entShelfID - 1 >= 0)
                                    varyShelfNum -= 1;
                                else entryNavigationIssue = "No preceding shelves";
                            }
                            //varyEntryNum = noVary;
                        }
                    }
                    else exitEntryViewPageQ = true;
                }
            }
            else
            {
                Format($"{Ind24}Library is empty. No searches to be made.");
                Pause();
                exitMainPageQ = true;
            }

            return exitMainPageQ;
        }


        // tool
        /// <returns>A variation of <paramref name="arg"/> that exists within <paramref name="text"/>.</returns>
        public static string HighlightSearchArg(string searchArg, string text, string noValue = DataHandlerBase.Sep)
        {
            string highlightStr = noValue;
            if (searchArg.IsNotNEW() && text.IsNotNEW())
            {
                string finalBuildMatchArg = null, buildMatchArg = "", buildMatchArgAlt = "";
                int matchPoint_Arg = 0, matchPoint_Alt = 0; 
                for (int mx = 0; mx < searchArg.Length; mx++)
                {
                    string character = searchArg[mx].ToString();

                    /// bMA - lower then upper
                    if (text.Contains(buildMatchArg + character.ToLower()))
                        buildMatchArg += character.ToLower();                    
                    else if (text.Contains(buildMatchArg + character.ToUpper()))
                        buildMatchArg += character.ToUpper();

                    /// bMA Alt - upper then lower
                    if (text.Contains(buildMatchArgAlt + character.ToUpper()))
                        buildMatchArgAlt += character.ToUpper();
                    else if (text.Contains(buildMatchArgAlt + character.ToLower()))
                        buildMatchArgAlt += character.ToLower();

                    /// match-making checkups: if only one bMA matches, the other non-matching is updated to the matching, and deviation restarts
                    /// match-making points: A single point awarded for each of the following: A) being the only matching build,  B) matching with search Arg
                    // -> Points B
                    if (searchArg.Contains(buildMatchArg))
                        matchPoint_Arg++;
                    if (searchArg.Contains(buildMatchArgAlt))
                        matchPoint_Alt++;
                    // -> checkups and Points A
                    if ((!text.Contains(buildMatchArg) || buildMatchArg.Length < buildMatchArgAlt.Length) && text.Contains(buildMatchArgAlt))
                    {
                        buildMatchArg = buildMatchArgAlt;
                        matchPoint_Alt++;
                    }
                    if (text.Contains(buildMatchArg) && (!text.Contains(buildMatchArgAlt) || buildMatchArgAlt.Length < buildMatchArg.Length))
                    {
                        buildMatchArgAlt = buildMatchArg;
                        matchPoint_Arg++;
                    }
                    

                    /// assign final matching text
                    if (mx + 1 == searchArg.Length)
                    {
                        /// IF mPArg >= mPAlt: prioritize bMA; ELSE prioritize bMAA;
                        bool matchPriorityArg = matchPoint_Arg >= matchPoint_Alt;
                        
                        if (matchPriorityArg)
                        {
                            /// bMA then bMAA
                            if (buildMatchArg.Length == searchArg.Length && text.Contains(buildMatchArg))
                                finalBuildMatchArg = buildMatchArg;
                            else if (buildMatchArgAlt.Length == searchArg.Length && text.Contains(buildMatchArgAlt))
                                finalBuildMatchArg = buildMatchArgAlt;
                        }
                        else
                        {
                            /// bMAA then bMA
                            if (buildMatchArgAlt.Length == searchArg.Length && text.Contains(buildMatchArgAlt))
                                finalBuildMatchArg = buildMatchArgAlt;
                            else if (buildMatchArg.Length == searchArg.Length && text.Contains(buildMatchArg))
                                finalBuildMatchArg = buildMatchArg;
                        }
                    }
                }

                if (finalBuildMatchArg.IsNotNEW() && text.Contains(finalBuildMatchArg))
                    highlightStr = finalBuildMatchArg;
            }
            return highlightStr;
        }
    }
}
