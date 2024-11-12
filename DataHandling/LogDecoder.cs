using ConsoleFormat;
using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.DataHandling
{
    public class LogDecoder : DataHandlerBase
    {
        /** LINE OMISSION RULES
        ...........................
        Single Line Omission: (--)
            The single line omission key must be placed before any comments to be omitted by the decoder.
            Ex.
                -- Ignored, individual line 1
                --Ignored, individual line 2
                    
        ...........................
        Block Line Omission: (-- !open --) and (-- !close --)
            The 'open' and 'close' omission blocks must be on their own line to function. All comments within these block lines will be omitted by the decoder.
            Ex.
                -- !open --
                Ignored, group line 1
                Ignored, group line 2
                -- Ignored, group line 3
                -- !close --
        **/
        public const string omit = "--", omitBlockOpen = "-- !open --", omitBlockClose = "-- !close --";
        /** CHARACTER KEYCODE SUBSTITUTION RULES
        ...........................
        The Keycode must start with the ampersand symbol (&) which is followed by two numbers (##) 
        Keycode syntax therefore is '&##'

            Current Keycodes (sourced from 'HCRLA - Log Template, Revised.txt')
            Sym     Code
            ---     ---
            '-'     &00
            ','     &01
            ':'     &02
            '('     &11
            ')'     &12
            ''      &99

        Keycode syntax is only available in the Added, Additional, and Updated sections
        Example usages
            [ADDED]
            1 |K&99C&99R&00Unit - (i4500)      // Produces ConBase as "v0.00;KCR-Unit;i4500"
            2a|McHattin's Fedora - (i4554)  // Produce ConBase as "v0.00;Mc Hattin's Fedora;i4554"
            2b|Mc&99Hattin's Fedora - (i4554) // Produce ConBase as "v0.00;McHattin's Fedora;i4554"    (notice difference to 2a)

            [ADDITIONAL]
            > (Grate&11bloody&12) - The &11Bloody&12 Grate     // Produces ConAddit as "v0.00;;The (Bloody) Grate;Grate(bloody)"
        v0.99;;Npc #12;n12
         **/
        public const string cks00 = "&00 -", cks01 = "&01 ,", cks02 = "&02 :", cks11 = "&11 (", cks12 = "&12 )", cks99 = "&99 ";
        const char legRangeKey = '~', updtSelfKey = ':';
        readonly bool enableSelfUpdatingFunction = true, testLibConAddUpdConnectionQ = false;
        static string _prevRecentDirectory, _recentDirectory;
        bool _hasDecodedQ, _versionAlreadyExistsQ;
        static bool _allowLogDecToolsDbugMessagesQ = false;
        public static string RecentDirectory
        {
            get => _recentDirectory.IsNEW() ? null : _recentDirectory;
            set
            {
                _prevRecentDirectory = _recentDirectory;
                _recentDirectory = value.IsNEW() ? null : value;
            }
        }
        public ResLibrary DecodedLibrary { get; private set; }
        public List<DecodeInfo> DecodeInfoDock;
        public bool HasDecoded { get => _hasDecodedQ; }
        public bool OverwriteWarning { get => _versionAlreadyExistsQ; }

        /// for decoding
        List<string> usedLegendKeys = new(), usedLegendKeysSources = new();

        public LogDecoder()
        {
            commonFileTag = "logDec";
            //_recentDirectory = null;
        }

        // file saving - loading
        protected override bool EncodeToSharedFile()
        {
            string encodeRD = RecentDirectory.IsNEW() ? Sep : RecentDirectory;
            bool hasEncoded = FileWrite(false, commonFileTag, encodeRD);
            if (ChangesMade())
                _prevRecentDirectory = RecentDirectory;
            Dbug.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder has saved recent directory path :: {RecentDirectory}");

            return hasEncoded;
        }
        protected override bool DecodeFromSharedFile()
        {
            bool fetchedLogDecData = false;
            if (FileRead(commonFileTag, out string[] logDecData))
                if (logDecData.HasElements())
                {
                    string decode = logDecData[0].Contains(Sep) ? null : logDecData[0];
                    RecentDirectory = decode;
                    _prevRecentDirectory = RecentDirectory;
                    Dbug.SingleLog("LogDecoder.DecodeFromSharedFile()", $"Log Decoder recieved [{logDecData[0]}], and has loaded recent directory path :: {RecentDirectory}");
                    fetchedLogDecData = true;
                }

            if (!fetchedLogDecData)
                Dbug.SingleLog("LogDecoder.DecodeFromSharedFile()", $"Log Decoder recieved no data for directory path (error: {Tools.GetRecentWarnError(false, false)})");

            return fetchedLogDecData;
        }
        public static new bool ChangesMade()
        {
            return _recentDirectory != _prevRecentDirectory;
        }

        // log file decoding
        public bool DecodeLogInfo(string[] logData, VerNum earliestLibVersion, VerNum latestLibVersion)
        {
            bool hasFullyDecodedLogQ = false;
            Dbug.StartLogging("LogDecoder.DecodeLogInfo(str[])");
            Dbug.Log($"Recieved collection object (data from log?) to decode; collection has elements? {logData.HasElements()}");
            _allowLogDecToolsDbugMessagesQ = true;
            _versionAlreadyExistsQ = false;

            if (logData.HasElements())
            {
                Dbug.Log($"Recieved {logData.Length} lines of log data; proceeding to decode information...");
                short nextSectionNumber = 0, currentSectionNumber = 0;
                #region sectionNames
                // were previously "const" strings
                string secVer = DecodedSection.Version.ToString();  //"Version"; 
                string secAdd = DecodedSection.Added.ToString();    //"Added"; 
                string secAdt = DecodedSection.Additional.ToString(); //"Additional"; 
                string secTTA = DecodedSection.TTA.ToString();      //"TTA"; 
                string secUpd = DecodedSection.Updated.ToString();  //"Updated"; 
                string secLeg = DecodedSection.Legend.ToString();   //"Legend"; 
                string secSum = DecodedSection.Summary.ToString();  //"Summary";
                #endregion
                const string secBracL = "[", secBracR = "]";
                const int newResConShelfNumber = 0, maxSectionsCount = 7;
                string[] logSections =
                {
                    secVer, secAdd, secAdt, secTTA, secUpd, secLeg, secSum
                };
                bool withinASectionQ = false, parsedSectionTagQ = false, withinOmitBlock = false;
                string currentSectionName = null, lastSectionName = null;
                int[] countSectionIssues = new int[8];
                List<DecodeInfo> decodingInfoDock = new(), prevDecodingInfoDock = new();

                #region Decode - Tracking & Temporary Storage
                bool endFileReading = false, ranLegendChecksQ = false;
                List<string[]> addedPholderNSubTags = new();
                List<string> addedContentsNames = new();
                List<string> looseInfoRCDataIDs = new();
                List<ContentAdditionals> looseConAddits = new();
                List<ContentChanges> looseConChanges = new();
                usedLegendKeys = new List<string>();
                usedLegendKeysSources = new List<string>();
                // vv to be integrated into library later vv
                VerNum logVersion = VerNum.None;
                List<ResContents> resourceContents = new();                
                List<LegendData> legendDatas = new();
                SummaryData summaryData = new();
                int ttaNumber = 0;
                List<string> summaryDataParts = new();
                // vv progress tracking
                ProgressBarInitialize(false, false, 25, 4, 1);
                ProgressBarUpdate(0);
                TaskCount = logData.Length + 1;
                #endregion


                // -- reading version log file --
                Dbug.NudgeIndent(true);
                const int readingTimeOut = 250; // OG "250"
                for (int llx = 0; llx < logData.Length && llx < readingTimeOut && !endFileReading; llx++)
                {
                    /// setup
                    int lineNum = llx + 1;
                    string logDataLine = logData[llx];
                    string nextSectionName = nextSectionNumber.IsWithin(0, (short)(logSections.Length - 1)) ? logSections[nextSectionNumber] : "";

                    bool firstSearchingDbgRanQ = false;
                    if (!withinASectionQ)
                    {
                        if (!withinOmitBlock)
                        {
                            Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                            //if (!(logDataLine.StartsWith(omit) || logDataLine.StartsWith(omitBlockOpen)))
                                Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... ({nameof(withinASectionQ)} set to false)" : "")}", logDataLine)}");
                            firstSearchingDbgRanQ = true;
                        }
                        endFileReading = nextSectionNumber >= maxSectionsCount;                        
                    }

                    Dbug.NudgeIndent(true);
                    if (logDataLine.IsNotNEW() && !endFileReading)
                    {
                        logDataLine = logDataLine.Trim();
                        bool invalidLine = logDataLine.Contains(Sep);

                        /// imparsable group (block omit)
                        if (logDataLine.Equals(omitBlockOpen) && !withinOmitBlock)
                        {
                            Dbug.Log($"Line contained '{omitBlockOpen}' :: starting omission block; ");
                            Dbug.LogPart("Block Omitting Lines :: ");
                            withinOmitBlock = true;
                        }
                        else if (logDataLine.Equals(omitBlockClose) && withinOmitBlock)
                        {
                            withinOmitBlock = false;
                            Dbug.Log("; ");
                            Dbug.LogPart($"Line contained '{omitBlockClose}' :: ending omission block; ");
                        }

                        /// imparsable line (single omit)
                        bool omitThisLine = logDataLine.StartsWith(omit) && !withinOmitBlock;

                        /// if {parsable line} else {imparsable issue message}
                        if (!omitThisLine && !invalidLine && !withinOmitBlock)
                        {
                            // find a section
                            if (!withinASectionQ)
                            {
                                if (IsSectionTag())
                                {
                                    string ldlSectionName = logDataLine.Replace(secBracL, "").Replace(secBracR, " ");
                                    // identified when [sectionName] -or- [sectionName:...] -or- [sectionName : ...]
                                    //if (ldlSectionName.ToLower().StartsWith($"{nextSectionName.ToLower()} ") || ldlSectionName.ToLower().StartsWith($"{nextSectionName.ToLower()}:"))
                                    if (ldlSectionName.Trim().ToLower().StartsWith($"{nextSectionName.ToLower()}"))
                                    {
                                        parsedSectionTagQ = false;
                                        withinASectionQ = true;
                                        currentSectionName = nextSectionName;
                                        currentSectionNumber = nextSectionNumber;
                                        nextSectionNumber++;
                                        Dbug.Log($"Found section #{currentSectionNumber + 1} ({currentSectionName});");
                                    }
                                }
                            }

                            // parse section's data
                            if (withinASectionQ)
                            {
                                DecodeInfo decodeInfo = new($"{logDataLine}", currentSectionName);
                                Dbug.Log($"{{{(currentSectionName.Length > 5 ? currentSectionName.Remove(5) : currentSectionName)}}}  L{lineNum,-2}| {logDataLine}");
                                Dbug.NudgeIndent(true);
                                bool sectionIssueQ = false;

                                /// VERSION
                                if (currentSectionName == secVer)
                                {
                                    /** VERSION (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        [Version : N.n]                            

                            
                                        DESIGN DOC
                                        --------------------
                                        .	[Version]
			                                Syntax 		[Version: N.n]
			                                Ex. 		[Version: 1.10]
				                                {REQUIRED} A tag describing the version the log associates with, where 'N' is the major verison number, and 'n' is minor version number.
                                     */

                                    sectionIssueQ = true;

                                    Dbug.LogPart("Version Data  //  ");
                                    logDataLine = RemoveEscapeCharacters(logDataLine);
                                    if (IsSectionTag() && !parsedSectionTagQ)
                                    {
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(":") && logDataLine.CountOccuringCharacter(':') == 1)
                                        {
                                            Dbug.LogPart($"Contains ':', raw data ({logDataLine}); ");
                                            string[] verSplit = logDataLine.Split(':');
                                            if (verSplit.HasElements(2))
                                            {
                                                Dbug.LogPart($"Has sufficent elements after split; ");
                                                if (verSplit[1].IsNotNEW())
                                                {
                                                    Dbug.Log($"Try parsing split @ix1 ({verSplit[1]}) into {nameof(VerNum)} instance; ");
                                                    bool parsed = VerNum.TryParse(verSplit[1], out VerNum verNum, out string parsingIssue);
                                                    if (parsed)
                                                    {
                                                        Dbug.LogPart($"--> Obtained {nameof(VerNum)} instance [{verNum}]");
                                                        decodeInfo.NoteResult($"{nameof(VerNum)} instance --> {verNum}");
                                                        logVersion = verNum;
                                                        parsedSectionTagQ = true;
                                                        sectionIssueQ = false;

                                                        // log version suggestions / version clashes
                                                        if (latestLibVersion.HasValue() && earliestLibVersion.HasValue())
                                                        {
                                                            if (logVersion.AsNumber.IsWithin(earliestLibVersion.AsNumber, latestLibVersion.AsNumber))
                                                            {
                                                                Dbug.LogPart($"; Version {logVersion.ToStringNums()} information already exists within library [OVERWRITE Warning]");
                                                                decodeInfo.NoteIssue($"Version {logVersion.ToStringNums()} information already exists in library (May be overwritten).");
                                                                _versionAlreadyExistsQ = true;
                                                            }                                                            
                                                            
                                                            if (logVersion.AsNumber > latestLibVersion.AsNumber && latestLibVersion.AsNumber + 1 != logVersion.AsNumber)
                                                            {
                                                                bool nextMajor = latestLibVersion.MinorNumber + 1 >= 100;
                                                                VerNum suggestedVer;
                                                                if (nextMajor)
                                                                    suggestedVer = new VerNum(latestLibVersion.MajorNumber + 1, 0);
                                                                else suggestedVer = new VerNum(latestLibVersion.MajorNumber, latestLibVersion.MinorNumber + 1);

                                                                Dbug.LogPart($"; Suggesting version log number: {suggestedVer}");
                                                                decodeInfo.NoteIssue($"Suggesting version log number: {suggestedVer}");
                                                            }                                                            
                                                            else if (logVersion.AsNumber < earliestLibVersion.AsNumber && earliestLibVersion.AsNumber - 1 != logVersion.AsNumber)
                                                            {
                                                                bool lowestVer = latestLibVersion.AsNumber - 1 == 0;
                                                                bool prevMajor = latestLibVersion.MinorNumber - 1 < 0 && latestLibVersion.MajorNumber >= 1;

                                                                if (!lowestVer)
                                                                {
                                                                    VerNum suggestedVer;
                                                                    if (prevMajor)
                                                                        suggestedVer = new VerNum(latestLibVersion.MajorNumber - 1, 99);
                                                                    else suggestedVer = new VerNum(latestLibVersion.MajorNumber, latestLibVersion.MinorNumber - 1);

                                                                    Dbug.LogPart($"; Suggesting version log number: {suggestedVer}");
                                                                    decodeInfo.NoteIssue($"Suggesting version log number: {suggestedVer}");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart($"{nameof(VerNum)} instance could not be parsed: {parsingIssue}");
                                                        decodeInfo.NoteIssue($"{nameof(VerNum)} instance could not be parsed: {parsingIssue}");
                                                    }
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("No data in split @ix1");
                                                    decodeInfo.NoteIssue("No data provided for version number");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains(':'))
                                            {
                                                Dbug.LogPart("This line has too many ':'");
                                                decodeInfo.NoteIssue("This line has too many ':'");
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower().StartsWith(secVer.ToLower()))
                                                {
                                                    Dbug.LogPart("This line is missing ':'");
                                                    decodeInfo.NoteIssue("This line is missing ':'");
                                                }
                                                else
                                                {
                                                    Dbug.Log($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                    decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (parsedSectionTagQ)
                                        {
                                            Dbug.LogPart("Version section tag has already been parsed");
                                            decodeInfo.NoteIssue("Version section tag has already been parsed");
                                        }
                                        else if (logDataLine.StartsWith(secBracL))
                                        {
                                            Dbug.LogPart("This line does not end with ']'");
                                            decodeInfo.NoteIssue("This line does not end with ']'");
                                        }
                                        else
                                        {
                                            Dbug.LogPart("This line does not start with '['");
                                            decodeInfo.NoteIssue("This line does not start with '['");
                                        }
                                    }
                                    
                                    EndSectionDbugLog("version", null);
                                }

                                /// ADDED
                                if (currentSectionName == secAdd)
                                {
                                    /** ADDED (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        [ADDED: x,t89; #,Item]
                                        -- 0 |<InternalName>-(<shortItemID>; <shortRelatedIds: tiles, projectiles, walls, etc.>)
                                        Ex.
                                        1 |One Item 		-(i1; t0)
                                        2 |This #		-(i5; t2,x)
                                        3 |Some #		-(i6; t344,344_2; w290)
                                        4 |
                                        ...
                                        25| Maximum items per update. Effective until no more items to add.  
                                        `x` will be replaced with 't89' in auto-logging. Seperate many variables with `;`
                                                               
                                        
                                    
                                        DESIGN DOC
                                        --------------------
                                    .	[ADDED]
			                            Syntax		##|{InternalName}-({shortItemID}; {shortRelatedIds})
			                            Ex. 		1 |One Item			-(i1; t0 p3,4)
				                            Tag denoting table of added content. Locates majority of the information for each version (added items, related content). Each line of new content must follow after the 'ADDED' tag unbroken by newlines.
				                            . {InternalName} - Name given to the content being added (Ex. Egg Block)
				                            . {shortItemID} - Using the legend, specify Data Id (Ex. "Item_67" as "i67")
				                            . {shortRelatedIDs}	- Using the legend, specify contents with Data Ids that are associated with added content ("Tiles_89 Projectile_14" as "t89 p14")
			                            Syntax		[ADDED: <ph>,<substitute>; <ph>,<substitute>]
			                            Ex.			[ADDED: $,Money; g*,Gray Star]
				                            Within the 'ADDED' tag brackets, these symbols are used to shorten and simplify logging the information of added content, replaced later through the HCAutoLogger. Especially useful where multiple content have the same word(s).
				                            . <ph> - Unique character(s) that identify a place for related substitutes
				                            . <substitue> - Word(s) that will replace its placeholder
                                     
                                     */

                                    sectionIssueQ = true;
                                    bool wasSectionTag = false;

                                    /// added tag (may contain placeholders and substitutes)
                                    if (logDataLine.Contains(secBracL) || logDataLine.Contains(secBracR))
                                    {
                                        Dbug.LogPart("Identified added section tag; ");
                                        wasSectionTag = true;
                                        logDataLine = RemoveEscapeCharacters(logDataLine);

                                        if (IsSectionTag() && !parsedSectionTagQ)
                                        {
                                            if (addedPholderNSubTags.HasElements())
                                            {
                                                Dbug.LogPart("Clearing ph/sub list; ");
                                                addedPholderNSubTags.Clear();
                                            }

                                            logDataLine = RemoveSquareBrackets(logDataLine);
                                            if (logDataLine.Contains(':') && logDataLine.CountOccuringCharacter(':') <= 1)
                                            {
                                                /// ADDED  <-->  x,t21; y,p84 
                                                Dbug.Log("Contains ':', spliting header tag from placeholders and substitutes; ");
                                                string[] splitAddHeader = logDataLine.Split(':');
                                                if (splitAddHeader.HasElements(2))
                                                {
                                                    bool nothingInvalidBehindColonQ = false;
                                                    if (splitAddHeader[0].IsNotNEW())
                                                    {
                                                        if (splitAddHeader[0].ToLower().Replace(secAdd.ToLower(), "").IsNEW())
                                                            nothingInvalidBehindColonQ = true;
                                                        else
                                                        {
                                                            Dbug.LogPart("Only section name must be written before ':'");
                                                            decodeInfo.NoteIssue("Only section name must be written before ':'");
                                                        }
                                                    }

                                                    if (splitAddHeader[1].IsNotNEW() && nothingInvalidBehindColonQ)
                                                    {
                                                        Dbug.LogPart("Sorting placeholder/substitute groups :: ");
                                                        /// if()    x,t21  <-->  y,p84
                                                        /// else()  x,t21
                                                        List<string> addedPhSubs = new();
                                                        if (splitAddHeader[1].Contains(';'))
                                                        {
                                                            Dbug.LogPart("Detected multiple >> ");
                                                            string[] pholdSubs = splitAddHeader[1].Split(';');
                                                            if (pholdSubs.HasElements())
                                                                foreach (string phs in pholdSubs)
                                                                {
                                                                    if (phs.IsNotNEW())
                                                                    {
                                                                        Dbug.LogPart($"{phs} ");
                                                                        addedPhSubs.Add(phs);
                                                                    }
                                                                }
                                                        }
                                                        else if (splitAddHeader[1].Contains(','))
                                                        {
                                                            Dbug.LogPart($"Only one >> {splitAddHeader[1]}");
                                                            addedPhSubs.Add(splitAddHeader[1]);
                                                        }
                                                        Dbug.Log($"; ");
                                                        //Dbug.Log($" << End ph/sub group sorting ");

                                                        if (addedPhSubs.HasElements())
                                                        {
                                                            Dbug.Log($"Spliting '{addedPhSubs.Count}' placeholder and substitution groups");
                                                            for (int ps = 0; ps < addedPhSubs.Count; ps++)
                                                            {
                                                                string phs = $"{addedPhSubs[ps]}".Trim();
                                                                Dbug.LogPart($" -> Group #{ps + 1} (has ','): {phs.Contains(",")}");

                                                                /// x  <-->  t21
                                                                /// y  <-->  p84
                                                                if (phs.Contains(","))
                                                                {
                                                                    string[] phSub = phs.Split(',');
                                                                    if (phSub.HasElements(2))
                                                                    {
                                                                        if (phSub.Length > 2)
                                                                            Dbug.LogPart(";  this group has too many of ',' ... skipping this ph/sub group");
                                                                        else if (phSub[0].IsNotNEW() && phSub[1].IsNotNEW())
                                                                        {
                                                                            Dbug.LogPart($" --> Obtained placeholder and substitute :: Sub '{phSub[0].Trim()}' for '{phSub[1].Trim()}'");
                                                                            // multiple tags within tags? screw it.. first come, first serve
                                                                            addedPholderNSubTags.Add(new string[] { phSub[0].Trim(), phSub[1].Trim() }); 
                                                                        }
                                                                    }
                                                                }
                                                                else Dbug.LogPart($"; rejected group ({phs})");
                                                                Dbug.Log(";  ");
                                                            }

                                                            if (addedPholderNSubTags.HasElements())
                                                            {
                                                                string phSubs = "Got placeholder/substitute groups (as ph/'sub') :: ";
                                                                foreach (string[] phsubgroup in addedPholderNSubTags)
                                                                    if (phsubgroup.HasElements(2))
                                                                        phSubs += $"{phsubgroup[0]}/'{phsubgroup[1]}'; ";
                                                                Dbug.LogPart(phSubs);
                                                                decodeInfo.NoteResult(phSubs.Trim());
                                                                parsedSectionTagQ = true;
                                                                sectionIssueQ = false;
                                                            }
                                                            else
                                                            {
                                                                Dbug.LogPart("No placeholder/substitute groups were created and stored");
                                                                decodeInfo.NoteIssue("No placeholder/substitute groups were created and stored");
                                                            }
                                                        }
                                                        else decodeInfo.NoteIssue("Recieved no placeholder/substitute groups");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower() != secAdd.ToLower())
                                                {
                                                    if (logDataLine.Contains(':'))
                                                    {
                                                        Dbug.LogPart("This line contains too many ':'");
                                                        decodeInfo.NoteIssue("This line contains too many ':'");
                                                    }
                                                    else
                                                    {
                                                        //if (logDataLine.ToLower().Replace(secAdd.ToLower(), "").IsNEW())
                                                        if (logDataLine.ToLower().StartsWith(secAdd.ToLower()))
                                                        {
                                                            Dbug.LogPart("This line is missing ':'");
                                                            decodeInfo.NoteIssue("This line is missing ':'");
                                                        }
                                                        else
                                                        {
                                                            Dbug.Log($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                            decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                        }
                                                    }                                                    
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("Added section tag identified");
                                                    decodeInfo.NoteResult("Added section tag identified");
                                                    parsedSectionTagQ = true;
                                                }
                                            }
                                        }   
                                        else
                                        {
                                            if (parsedSectionTagQ)
                                            {
                                                Dbug.LogPart("Added section tag has already been parsed");
                                                decodeInfo.NoteIssue("Added section tag has already been parsed");
                                            }
                                            else if (logDataLine.StartsWith(secBracL))
                                            {
                                                Dbug.LogPart("This line does not end with ']'");
                                                decodeInfo.NoteIssue("This line does not end with ']'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line does not start with '['");
                                                decodeInfo.NoteIssue("This line does not start with '['");
                                            }
                                        }
                                    }

                                    /// added items (become content base groups)
                                    else if (parsedSectionTagQ)
                                    {
                                        // setup
                                        string addedContentName = null;
                                        List<string> addedDataIDs = new();

                                        Dbug.LogPart("Identified added data; ");
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        if (logDataLine.Contains('|') && logDataLine.CountOccuringCharacter('|') == 1)
                                        {
                                            /// 1  <-->  ItemName       -(x25; y32,33 q14)
                                            /// 2  <-->  Other o>       -(x21; a> q21) 
                                            Dbug.LogPart("Contains '|'; ");
                                            string[] splitAddedLine = logDataLine.Split('|');

                                            Dbug.Log($"item line (number '{splitAddedLine[0]}') has info? {splitAddedLine[1].IsNotNEW()}");
                                            if (splitAddedLine[1].IsNotNEW() && !IsNumberless(splitAddedLine[0]))
                                            {
                                                // replace placehoder keys with substitution phrases
                                                /// Other Items     -(x21; y42 q21)
                                                if (addedPholderNSubTags.HasElements())
                                                {
                                                    Dbug.LogPart("Replacing placeholders with their substitutes :: ");
                                                    string spAddLnIx1 = splitAddedLine[1];
                                                    foreach (string[] phSub in addedPholderNSubTags)
                                                    {
                                                        if (phSub.HasElements(2))
                                                            if (spAddLnIx1.Contains(phSub[0]))
                                                            {
                                                                /// CURR vs OLD
                                                                ///     [p>] 'Potato'
                                                                ///     Subbed 'p>' for 'Potato'
                                                                ///     
                                                                ///     []] for 'Brace'
                                                                ///     Subbed ']' for 'Brace'
                                                                /// 
                                                                ///     [$] 'Coin';  [w>] 'Watermelon'
                                                                ///     Subbed '$' for 'Coin'; Subbed 'w>' for 'Watermelon'
                                                                /// 
                                                                /// 
                                                                // CURR $"[{phSub[0]}] '{phSub[1]}';  "       // OLD $"Subbed '{phSub[0]}' for '{phSub[1]}'; "
                                                                Dbug.LogPart($"[{phSub[0]}] '{phSub[1]}';  ");
                                                                spAddLnIx1 = spAddLnIx1.Replace(phSub[0], phSub[1]);
                                                            }
                                                    }
                                                    if (spAddLnIx1 != splitAddedLine[1])
                                                    {
                                                        splitAddedLine[1] = spAddLnIx1;
                                                        Dbug.Log(" --> replacements complete; ");
                                                    }
                                                    else Dbug.Log(" --> no changes; ");
                                                }


                                                // parse content name and data ids
                                                if (splitAddedLine[1].Contains('-') && splitAddedLine[1].CountOccuringCharacter('-') == 1)
                                                {
                                                    Dbug.LogPart("Contains '-'; ");
                                                    /// ItemName  <-->  (x25; y32,33 q14)
                                                    /// Other Items  <-->  (x21; y42 q21)
                                                    string[] addedContsNDatas = splitAddedLine[1].Split('-');
                                                    if (addedContsNDatas[1].IsNotNEW() && addedContsNDatas[0].IsNotNEW())
                                                    {
                                                        bool okayContentDataIDs = true;
                                                        addedContsNDatas[1] = addedContsNDatas[1].Trim();
                                                        if (!addedContsNDatas[1].StartsWith('(') || !addedContsNDatas[1].EndsWith(')'))
                                                        {
                                                            Dbug.LogPart("Content data IDs must be enclosed in parentheses; ");
                                                            decodeInfo.NoteIssue("Content data IDs must be enclosed in parentheses");
                                                            okayContentDataIDs = false;
                                                        }

                                                        if (okayContentDataIDs)
                                                        {
                                                            addedContsNDatas[1] = RemoveParentheses(addedContsNDatas[1]);
                                                            addedContentName = CharacterKeycodeSubstitution(FixContentName(addedContsNDatas[0]));
                                                            bool okayContentName = true;
                                                            if (addedContentsNames.HasElements())
                                                                okayContentName = !addedContentsNames.Contains(addedContentName);

                                                            Dbug.Log("Fetching content data IDs; ");
                                                            if (addedContsNDatas[1].CountOccuringCharacter(';') <= 1 && okayContentName) /// Zest -(x22)
                                                            {
                                                                bool missingMainIDq = false;
                                                                if (addedContsNDatas[1].CountOccuringCharacter(';') == 0)
                                                                    missingMainIDq = addedContsNDatas[1].CountOccuringCharacter(',') > 0;

                                                                if (!missingMainIDq)
                                                                {
                                                                    Dbug.NudgeIndent(true);
                                                                    /// (x25 y32,33 q14)
                                                                    /// (x21 y42 q21)
                                                                    addedContsNDatas[1] = addedContsNDatas[1].Replace(";", "");
                                                                    bool cksViolationQ = false;
                                                                    /// IF detect CKS-comma: warn ... IF detect CKS-(any bracket or colon): warn and major issue
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks01))
                                                                    {
                                                                        Dbug.LogPart($"Usage of CKS '{cks01}' in data ID groups advised against; ");
                                                                        decodeInfo.NoteIssue($"Usage of character keycode '{cks01}' in data ID groups is advised against");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks11))
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbug.LogPart($"Usage of CKS '{cks11}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue($"Usage of character keycode '{cks11}' in data ID groups is not allowed");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks12) && !cksViolationQ)
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbug.LogPart($"Usage of CKS '{cks12}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue($"Usage of character keycode '{cks12}' in data ID groups is not allowed");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks02) && !cksViolationQ)
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbug.LogPart($"Usage of CKS '{cks02}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue($"Usage of character keycode '{cks02}' in data ID groups is not allowed");
                                                                    }

                                                                    addedContsNDatas[1] = CharacterKeycodeSubstitution(addedContsNDatas[1]);
                                                                    string[] dataIdGroups = addedContsNDatas[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                                    if (dataIdGroups.HasElements() && !cksViolationQ)
                                                                        foreach (string dataIDGroup in dataIdGroups)
                                                                        {
                                                                            // For invalid id groups
                                                                            /// yxx,tyy
                                                                            /// qes
                                                                            if (IsNumberless(dataIDGroup))
                                                                            {
                                                                                Dbug.LogPart($"Numberless ID group '{dataIDGroup}' is invalid");
                                                                                decodeInfo.NoteIssue($"Numberless ID group '{dataIDGroup}' is invalid");
                                                                                Dbug.Log("; ");
                                                                            }
                                                                            /// 100, 23
                                                                            else if (RemoveNumbers(dataIDGroup.Replace(",", "").Replace(legRangeKey.ToString(), "")).IsNEW())
                                                                            {
                                                                                Dbug.LogPart($"Data ID group '{dataIDGroup}' is not a data ID (just numbers)");
                                                                                decodeInfo.NoteIssue($"Data ID group '{dataIDGroup}' is just numbers (invalid)");
                                                                                Dbug.Log("; ");
                                                                            }
                                                                            
                                                                            // for valid id groups
                                                                            /// y32,33
                                                                            else if (dataIDGroup.Contains(','))
                                                                            {
                                                                                Dbug.LogPart($"Got complex ID group '{dataIDGroup}'");
                                                                                string[] dataIDs = dataIDGroup.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                                                                if (dataIDs.HasElements(2))
                                                                                {
                                                                                    Dbug.LogPart("; ");
                                                                                    DisassembleDataID(dataIDs[0], out string dataKey, out _, out string suffix);
                                                                                    NoteLegendKey(dataKey, dataIDs[0]);
                                                                                    NoteLegendKey(suffix, dataIDs[0]);
                                                                                    if (dataKey.IsNotNEW())
                                                                                    {
                                                                                        Dbug.Log($"Retrieved data key '{dataKey}'; ");
                                                                                        foreach (string datId in dataIDs)
                                                                                        {
                                                                                            Dbug.LogPart(". Adding ID; ");
                                                                                            DisassembleDataID(datId, out _, out string dataBody, out string sfx);
                                                                                            NoteLegendKey(sfx, datId);
                                                                                            string datToAdd = dataKey + dataBody + sfx;
                                                                                            if (!IsNumberless(datToAdd))
                                                                                            {
                                                                                                addedDataIDs.Add(datToAdd);
                                                                                                Dbug.LogPart($"Got and added '{datToAdd}'");
                                                                                            }
                                                                                            Dbug.Log("; ");
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    Dbug.LogPart("; This complex ID group does not have at least 2 IDs");
                                                                                    decodeInfo.NoteIssue("This complex ID group does not have at least 2 IDs");
                                                                                    Dbug.Log("; ");
                                                                                }
                                                                            }
                                                                            /// r20~22
                                                                            /// q21`~24`
                                                                            else if (dataIDGroup.Contains(legRangeKey))
                                                                            {
                                                                                Dbug.LogPart($"Got range ID group '{dataIDGroup}'; ");
                                                                                NoteLegendKey(legRangeKey.ToString(), dataIDGroup);

                                                                                string[] dataIdRng = dataIDGroup.Split(legRangeKey);
                                                                                if (dataIdRng.HasElements() && dataIDGroup.CountOccuringCharacter(legRangeKey) == 1)
                                                                                {
                                                                                    //GetDataKeyAndSuffix(dataIdRng[0], out string dataKey, out string dkSuffix);
                                                                                    DisassembleDataID(dataIdRng[0], out string dataKey, out _, out string dkSuffix);
                                                                                    NoteLegendKey(dataKey, dataIdRng[0]);
                                                                                    NoteLegendKey(dkSuffix, dataIdRng[0]);
                                                                                    if (dataKey.IsNotNEW())
                                                                                    {
                                                                                        Dbug.LogPart($"Retrieved data key '{dataKey}' and suffix [{(dkSuffix.IsNEW() ? "<none>" : dkSuffix)}]; ");

                                                                                        dataIdRng[0] = dataIdRng[0].Replace(dataKey, "");
                                                                                        dataIdRng[1] = dataIdRng[1].Replace(dataKey, "");
                                                                                        if (dkSuffix.IsNotNEW())
                                                                                        {
                                                                                            dataIdRng[0] = dataIdRng[0].Replace(dkSuffix, "");
                                                                                            dataIdRng[1] = dataIdRng[1].Replace(dkSuffix, "");
                                                                                        }

                                                                                        bool parseRngA = int.TryParse(dataIdRng[0], out int rngA);
                                                                                        bool parseRngB = int.TryParse(dataIdRng[1], out int rngB);
                                                                                        if (parseRngA && parseRngB)
                                                                                        {
                                                                                            if (rngA != rngB)
                                                                                            {
                                                                                                int lowBound = Math.Min(rngA, rngB), highBound = Math.Max(rngA, rngB);
                                                                                                Dbug.LogPart($"Parsed range numbers: {lowBound} up to {highBound}; Adding IDs :: ");

                                                                                                for (int rnix = lowBound; rnix <= highBound; rnix++)
                                                                                                {
                                                                                                    string dataID = $"{dataKey}{rnix}{dkSuffix}".Trim();
                                                                                                    addedDataIDs.Add(dataID);
                                                                                                    Dbug.LogPart($"{dataID} - ");
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                Dbug.LogPart("Both range values cannot be the same");
                                                                                                decodeInfo.NoteIssue("Both range values cannot be the same");
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (parseRngA)
                                                                                            {
                                                                                                Dbug.LogPart($"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                decodeInfo.NoteIssue($"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                Dbug.LogPart($"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                decodeInfo.NoteIssue($"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (dataIdRng.HasElements())
                                                                                    {
                                                                                        Dbug.LogPart($"This range group has too many '{legRangeKey}'");
                                                                                        decodeInfo.NoteIssue($"This range group has too many '{legRangeKey}'");
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        Dbug.LogPart($"This range is missing values or missing '{legRangeKey}'");
                                                                                        decodeInfo.NoteIssue($"This range is missing values or missing '{legRangeKey}'");
                                                                                    }
                                                                                }
                                                                                Dbug.Log("; ");
                                                                            }
                                                                            /// x25 q14 ...
                                                                            else
                                                                            {
                                                                                //GetDataKeyAndSuffix(dataIDGroup, out string dataKey, out string suffix);
                                                                                DisassembleDataID(dataIDGroup, out string dataKey, out _, out string suffix);
                                                                                NoteLegendKey(dataKey, dataIDGroup);
                                                                                NoteLegendKey(suffix, dataIDGroup);

                                                                                addedDataIDs.Add(dataIDGroup.Trim());
                                                                                Dbug.Log($"Got and added ID '{dataIDGroup.Trim()}'; ");
                                                                            }
                                                                        }
                                                                    Dbug.NudgeIndent(false);

                                                                    addedDataIDs = addedDataIDs.ToArray().SortWords();
                                                                    Dbug.LogPart("Sorted data IDs; ");
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("Missing data for added content's 'main ID'");
                                                                    decodeInfo.NoteIssue("Missing data for added content's 'main ID'");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (okayContentName)
                                                                {
                                                                    Dbug.LogPart("Data IDs line contains too many ';'");
                                                                    decodeInfo.NoteIssue("Data IDs line contains too many ';'");
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("Content with this name has already been added");
                                                                    decodeInfo.NoteIssue("Content with this name has already been added");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (addedContsNDatas[0].IsNEW())
                                                        {
                                                            Dbug.LogPart("Missing data for 'content name'");
                                                            decodeInfo.NoteIssue("Missing data for 'content name'");
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart("Missing data for 'content data IDs'");
                                                            decodeInfo.NoteIssue("Missing data for 'content data IDs'");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (splitAddedLine[1].Contains('-'))
                                                    {
                                                        Dbug.LogPart("This line contains too many '-'");
                                                        decodeInfo.NoteIssue("This line contains too many '-'");
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart("This line is missing '-'");
                                                        decodeInfo.NoteIssue("This line is missing '-'");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!IsNumberless(splitAddedLine[0]))
                                                    decodeInfo.NoteIssue("Missing data for 'added content'");
                                                else
                                                {
                                                    Dbug.LogPart("Item line number was not a number");
                                                    decodeInfo.NoteIssue("Item line number was not a number");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains('|'))
                                            {
                                                Dbug.LogPart("This line contains too many '|'");
                                                decodeInfo.NoteIssue("This line contains too many '|'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line is missing '|'");
                                                decodeInfo.NoteIssue("This line is missing '|'");
                                            }
                                        }

                                        // complete (Dbug revision here)
                                        if (addedDataIDs.HasElements() && addedContentName.IsNotNEW())
                                        {
                                            addedContentsNames.Add(addedContentName);
                                            Dbug.LogPart($" --> Obtained content name ({addedContentName}) and data Ids (");
                                            foreach (string datID in addedDataIDs)
                                                Dbug.LogPart($"{datID} ");
                                            Dbug.LogPart("); ");

                                            // generate a content base group (and repeat info using overriden CBG.ToString())
                                            ContentBaseGroup newContent = new(logVersion, addedContentName, addedDataIDs.ToArray());                                            
                                            resourceContents.Add(new ResContents(newResConShelfNumber, newContent));
                                            //addedContents.Add(newContent);
                                            Dbug.LogPart($"Generated {nameof(ContentBaseGroup)} :: {newContent};");
                                            decodeInfo.NoteResult($"Generated {nameof(ContentBaseGroup)} :: {newContent}");

                                            sectionIssueQ = false;
                                        }
                                    }
                                    
                                    /// no data parsing before tag issue
                                    else
                                    {
                                        Dbug.LogPart("Added section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue("Added section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("added", !wasSectionTag && !IsSectionTag());
                                }

                                /// ADDITIONAL
                                if (currentSectionName == secAdt)
                                {
                                    /** ADDITIONAL (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        [ADDITIONAL]
                                        -- > <Opt.Name> (DataID) - <RelatedInternalName> (RelatedDataID)
                                        Ex.
                                        > Tile Zero (t0) - Other Something (i23)
                                        > t1 - Something Else (i42)
                                    

                            
                                        DESIGN DOC
                                        --------------------
                                    .	[ADDITIONAL]
			                            Syntax		> {Opt.Name} ({Data ID}) - {RelatedInternalName} ({RelatedDataID})
			                            Ex.			> Mashed Potato (t32) - Potato (i34)
						                            > (t116) - Gravel (i20)
				                            Tag denoting addition of contents that cannot be adjoined with items in 'ADDED'. Another major section that provides info on contents. Each line of additional content must follow after the 'ADDITIONAL' tag unbroken by newlines.
				                            . {Opt.Name} - An optional name to describe the additional contents
				                            . {DataID} - The extra (additional) content(s) being added (usually those that cannot be directly tied to a content' name, such as backgrounds and dusts)
				                            . {RelatedInternalName} - Name of content to associate additional content to 
				                            . {RelatedDataID} - Data ID of the content to associate additional content to                                        
                                     */

                                    sectionIssueQ = true;
                                    bool isHeader = false;
                                    /// additional header (imparsable, but not an issue)
                                    if (IsSectionTag())
                                    {
                                        logDataLine = RemoveEscapeCharacters(logDataLine);

                                        isHeader = true;
                                        if (IsValidSectionTag())
                                        {
                                            if (!parsedSectionTagQ)
                                            {
                                                decodeInfo.NoteResult("Additional section tag identified");
                                                parsedSectionTagQ = true;
                                                sectionIssueQ = false;
                                            }
                                            else
                                            {
                                                Dbug.Log("Additional section tag has already been parsed");
                                                decodeInfo.NoteIssue("Additional section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbug.Log($"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// additional contents
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Identified additional data; ");
                                        logDataLine = RemoveEscapeCharacters(logDataLine); 
                                        if (logDataLine.StartsWith('>') && logDataLine.CountOccuringCharacter('>') == 1)
                                        {
                                            /// Sardine Tins (u41,42) - Sardines (x89)
                                            /// (q41~44) - (x104)
                                            /// (Cod_Tail) - CodFish (x132)
                                            /// Mackrel Chunks (r230,231 y192) - Mackrel (x300)
                                            logDataLine = logDataLine.Replace(">", "");
                                            if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                            {
                                                /// SardineTins (u41,42)  <-->  Sardines (x89)
                                                /// (q41~44)  <-->  (x104)
                                                /// (Cod_Tail) <-->  CodFish (x132)
                                                /// Mackrel Chunks (r230,231 y192) <--> Mackrel (x300)
                                                Dbug.LogPart("Contains '-'; ");
                                                string[] splitAdditLine = logDataLine.Split('-');
                                                if (splitAdditLine.HasElements(2))
                                                    if (splitAdditLine[0].IsNotNEW() && splitAdditLine[1].IsNotNEW())
                                                    {
                                                        splitAdditLine[0] = splitAdditLine[0].Trim();
                                                        splitAdditLine[1] = splitAdditLine[1].Trim();
                                                        Dbug.Log($"Got contents ({splitAdditLine[0]}) and related content ({splitAdditLine[1]});");

                                                        bool continueToNextAdtSubSectionQ = false;
                                                        List<string> adtConDataIDs = new();
                                                        string adtContentName = "", adtRelatedName = "", adtRelatedDataID = "";


                                                        /// SardineTins  <-->  (u41,42)
                                                        /// (q41~44)
                                                        /// (Cod_Tail)
                                                        /// Mackrel Chunks  <-->  (r230,231 y192)
                                                        // + parsed additional content +
                                                        splitAdditLine[0] = splitAdditLine[0].Replace("(", " (").Replace(")", ") ");
                                                        Dbug.LogPart("Spaced-out parentheses in contents; ");
                                                        string[] additContsData = splitAdditLine[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                        if (additContsData.HasElements())
                                                        {
                                                            Dbug.LogPart("Content Parts: ");
                                                            string adtDataIDs = "";
                                                            adtContentName = "";
                                                            for (int ax = 0; ax < additContsData.Length; ax++)
                                                            {
                                                                string adtConPart = additContsData[ax];
                                                                if (adtConPart.Contains('(') && adtConPart.Contains(')') && adtDataIDs.IsNE())
                                                                    adtDataIDs = adtConPart;
                                                                else adtContentName += $"{adtConPart} ";
                                                                Dbug.LogPart($"{adtConPart}{(ax + 1 >= additContsData.Length ? "" : "|")}");
                                                            }

                                                            bool okayAdtContentName = true;
                                                            if (adtContentName.Contains('(') || adtContentName.Contains(')'))
                                                            {
                                                                Dbug.LogPart("Content name cannot (directly) contain parentheses");
                                                                decodeInfo.NoteIssue("Content name cannot (directly) contain parentheses");
                                                                okayAdtContentName = false;
                                                            }                                                            

                                                            /// u41  <-->  u42
                                                            /// q41  <-->  q42  <-->  q43  <-->  q44
                                                            /// CodTail
                                                            /// r230  <-->  r231  <-->  y192
                                                            if (adtDataIDs.IsNotNEW() && okayAdtContentName)
                                                            {
                                                                Dbug.Log("; ");
                                                                adtDataIDs = RemoveParentheses(adtDataIDs.Trim().Replace(' ', ','));
                                                                adtContentName = CharacterKeycodeSubstitution(FixContentName(adtContentName));
                                                                Dbug.LogPart($"Parsed addit. content name ({(adtContentName.IsNEW() ? "<no name>" : adtContentName)}) and data ID group ({adtDataIDs})");

                                                                /// u24,23`,29~31,CodTail
                                                                if (adtDataIDs.Replace(",","").IsNotNEW())
                                                                {
                                                                    Dbug.Log($";{(adtDataIDs.Contains(',') ? " Data ID group contains ',';" : "")} Fetching data IDs; ");
                                                                    string[] datIDs = adtDataIDs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                                                    if (datIDs.HasElements())
                                                                    {
                                                                        string dataKey = null;
                                                                        string dataBody = null;
                                                                        string dkSuffix = null;

                                                                        // if (get datakey and suffix for ranges) else (get datakey and suffix for other id groups)
                                                                        if (datIDs[0].Contains(legRangeKey))
                                                                        {
                                                                            string[] rangeParts = CharacterKeycodeSubstitution(datIDs[0]).Split(legRangeKey);
                                                                            if (rangeParts.HasElements(2))
                                                                            {
                                                                                //GetDataKeyAndSuffix(rangeParts[0], out dataKey, out dkSuffix);
                                                                                DisassembleDataID(rangeParts[0], out dataKey, out _, out dkSuffix);
                                                                                /// below doesn't interfere with out-of-scope values
                                                                                //GetDataKeyAndSuffix(rangeParts[1], out string dk, out string dksfx);
                                                                                DisassembleDataID(rangeParts[1], out string dk, out _, out string dksfx);
                                                                                NoteLegendKey(dk, rangeParts[1]);
                                                                                NoteLegendKey(dksfx, rangeParts[1]);
                                                                            }
                                                                        }
                                                                        else DisassembleDataID(CharacterKeycodeSubstitution(datIDs[0]), out dataKey, out dataBody, out dkSuffix);
                                                                        //else GetDataKeyAndSuffix(datIDs[0], out dataKey, out dkSuffix);
                                                                        NoteLegendKey(dataKey, datIDs[0]);
                                                                        NoteLegendKey(dkSuffix, datIDs[0]);

                                                                        bool noDataKey = dataKey.IsNEW();
                                                                        Dbug.LogPart($"Retrieved data key '{dataKey}'{(dkSuffix.IsNEW() ? "" : $" and suffix [{dkSuffix}]")}; ");
                                                                        
                                                                        /// decode issue handling
                                                                        bool isOnlyNumber = false;
                                                                        if (RemoveNumbers(adtDataIDs.Replace(",", "").Replace(legRangeKey.ToString(), "")).IsNEW())
                                                                        {
                                                                            isOnlyNumber = true;
                                                                            Dbug.LogPart($"Data ID group is not a data ID (just numbers); ");
                                                                            decodeInfo.NoteIssue($"Data ID group is just numbers (invalid)");
                                                                        }

                                                                        // go through the data ID groups for this addit.
                                                                        Dbug.NudgeIndent(true);
                                                                        if (!isOnlyNumber && (!noDataKey || dataBody.IsNotNE()))
                                                                            foreach (string rawDatID in datIDs)
                                                                            {
                                                                                bool invalidDataKey = false, cksDataIDViolation = DetectUsageOfCharacterKeycode(rawDatID, cks01);
                                                                                string datId = CharacterKeycodeSubstitution(rawDatID);
                                                                                if (RemoveNumbers(datId).IsNotNEW())
                                                                                {
                                                                                    invalidDataKey = IsNumberless(datId);
                                                                                    if (invalidDataKey)
                                                                                        Dbug.LogPart($"Treating Data ID as word (is numberless); ");
                                                                                }
                                                                                
                                                                                if (!cksDataIDViolation)
                                                                                {
                                                                                    // data IDs with numbers
                                                                                    /// q21 x24`
                                                                                    if (!invalidDataKey)
                                                                                    {
                                                                                        /// q41~44
                                                                                        if (datId.Contains(legRangeKey))
                                                                                        {
                                                                                            Dbug.LogPart($"Got range ID group '{datId}'; ");
                                                                                            NoteLegendKey(legRangeKey.ToString(), datId);
                                                                                            string[] dataIdRng;

                                                                                            if (dataKey.IsNotNE())
                                                                                            {
                                                                                                dataIdRng = datId.Split(legRangeKey);
                                                                                                if (datId.Contains(dataKey))
                                                                                                {
                                                                                                    Dbug.LogPart($"Removed data key '{dataKey}' before split; ");
                                                                                                    dataIdRng = datId.Replace(dataKey, "").Split(legRangeKey);
                                                                                                }
                                                                                            }
                                                                                            else dataIdRng = datId.Split(legRangeKey);

                                                                                            /// if (valid syntax) else (decoding issue, syntax error)
                                                                                            if (dataIdRng.HasElements() && datId.CountOccuringCharacter(legRangeKey) == 1)
                                                                                            {
                                                                                                if (dataKey.IsNotNE())
                                                                                                    dkSuffix = RemoveNumbers(dataIdRng[0].Replace(dataKey, ""));
                                                                                                if (dkSuffix.IsNEW())
                                                                                                    dkSuffix = RemoveNumbers(dataIdRng[1]);

                                                                                                if (dkSuffix.IsNotNEW())
                                                                                                {
                                                                                                    Dbug.LogPart($"Retrieved suffix [{(dkSuffix.IsNEW() ? "<none>" : dkSuffix)}]; ");
                                                                                                    dataIdRng[0] = dataIdRng[0].Replace(dkSuffix, "");
                                                                                                    dataIdRng[1] = dataIdRng[1].Replace(dkSuffix, "");
                                                                                                }
                                                                                                bool parseRngA = int.TryParse(dataIdRng[0], out int rngA);
                                                                                                bool parseRngB = int.TryParse(dataIdRng[1], out int rngB);
                                                                                                if (parseRngA && parseRngB)
                                                                                                {
                                                                                                    //Dbug.Log("..");
                                                                                                    if (rngA != rngB)
                                                                                                    {
                                                                                                        int lowBound = Math.Min(rngA, rngB), highBound = Math.Max(rngA, rngB);
                                                                                                        Dbug.LogPart($"Parsed range numbers ({lowBound} up to {highBound}); ");

                                                                                                        Dbug.LogPart("Adding IDs :: ");
                                                                                                        for (int rnix = lowBound; rnix <= highBound; rnix++)
                                                                                                        {
                                                                                                            string dataID = $"{dataKey}{rnix}{dkSuffix}".Trim();
                                                                                                            adtConDataIDs.Add(dataID);
                                                                                                            Dbug.LogPart($"{dataID} - ");
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        Dbug.LogPart("Both range values cannot be the same");
                                                                                                        decodeInfo.NoteIssue("Both range values cannot be the same");
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (parseRngA)
                                                                                                    {
                                                                                                        Dbug.LogPart($"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                        decodeInfo.NoteIssue($"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        Dbug.LogPart($"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                        decodeInfo.NoteIssue($"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (dataIdRng.HasElements())
                                                                                                {
                                                                                                    Dbug.LogPart($"This range group has too many '{legRangeKey}'");
                                                                                                    decodeInfo.NoteIssue($"This range group has too many '{legRangeKey}'");
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    Dbug.LogPart($"This range is missing values or missing '{legRangeKey}'");
                                                                                                    decodeInfo.NoteIssue($"This range is missing values or missing '{legRangeKey}'");
                                                                                                }
                                                                                            }
                                                                                        }

                                                                                        /// u41 u42 q41...
                                                                                        else
                                                                                        {
                                                                                            if (!IsNumberless(datId))
                                                                                            {
                                                                                                /// below does not intefere with important data
                                                                                                //GetDataKeyAndSuffix(datId, out string dkey, out string sfx);

                                                                                                DisassembleDataID(datId, out string dkey, out string dbody, out string sfx);
                                                                                                NoteLegendKey(dkey, datId);
                                                                                                NoteLegendKey(sfx, datId);

                                                                                                //string dataID = $"{dataKey}{datId.Replace(dataKey, "")}".Trim();
                                                                                                string dataID = dataKey + dbody + sfx;
                                                                                                adtConDataIDs.Add(dataID);
                                                                                                Dbug.LogPart($"Got and added '{dataID}'");
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    // data IDs without numbers
                                                                                    /// CodTail
                                                                                    else
                                                                                    {
                                                                                        DisassembleDataID(datId, out _, out _, out string sf);
                                                                                        NoteLegendKey(sf, datId);

                                                                                        adtConDataIDs.Add(datId);
                                                                                        Dbug.LogPart($"Got and added '{datId}'");
                                                                                    }
                                                                                }        
                                                                                else
                                                                                {
                                                                                    Dbug.LogPart($"; Data ID group may not contain comma CKS ({cks01})");
                                                                                    decodeInfo.NoteIssue($"Data ID group may not contain following character keycode: {cks01}");
                                                                                }

                                                                                Dbug.Log(";");
                                                                            }
                                                                        Dbug.NudgeIndent(false);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("; Data ID group had no values for data IDs");
                                                                    decodeInfo.NoteIssue("Data ID group had no values for data IDs");
                                                                }

                                                                if (adtConDataIDs.HasElements())
                                                                {
                                                                    adtConDataIDs = adtConDataIDs.ToArray().SortWords();
                                                                    if (adtDataIDs.Contains(","))
                                                                        Dbug.LogPart("--> Sorted Data IDs; ");
                                                                    else Dbug.Log("; Sorted Data IDs; ");
                                                                }
                                                                


                                                                // complete "content" sub-section
                                                                if (adtConDataIDs.HasElements())
                                                                {
                                                                    Dbug.LogPart($" --> Complete additional contents; Name ({(adtContentName.IsNEW() ? "<no name>" : adtContentName)}) and Data IDs (");
                                                                    foreach (string dId in adtConDataIDs)
                                                                        Dbug.LogPart($"{dId} ");
                                                                    Dbug.Log("); ");
                                                                    continueToNextAdtSubSectionQ = true;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Dbug.LogPart("; Missing content's data ID group");
                                                                decodeInfo.NoteIssue("Missing content's data ID group");
                                                            }
                                                        }

                                                        /// Sardines  <-->  (x89)
                                                        /// (x104)
                                                        /// CodFish  <-->  (x132)
                                                        // + parse and find related contents +
                                                        if (continueToNextAdtSubSectionQ)
                                                        {
                                                            continueToNextAdtSubSectionQ = false;
                                                            if (splitAdditLine[1].CountOccuringCharacter('(') <= 1 && splitAdditLine[1].CountOccuringCharacter(')') <= 1)
                                                            {
                                                                splitAdditLine[1] = splitAdditLine[1].Replace("(", " (").Replace(")", ") ");
                                                                Dbug.LogPart("Spaced-out parentheses in related contents; ");
                                                                string[] additContRelData = splitAdditLine[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                                if (additContRelData.HasElements())
                                                                {
                                                                    Dbug.LogPart("Related content parts: ");
                                                                    for (int ax = 0; ax < additContRelData.Length; ax++)
                                                                    {
                                                                        string adtRelPart = additContRelData[ax];
                                                                        if (adtRelPart.Contains('(') && adtRelPart.Contains(')'))
                                                                            adtRelatedDataID = adtRelPart;
                                                                        else adtRelatedName += adtRelPart + " ";
                                                                        Dbug.LogPart($"{adtRelPart}{(ax + 1 >= additContRelData.Length ? "" : "|")}");
                                                                    }
                                                                    Dbug.LogPart("; ");

                                                                    bool okayRelDataID = false, okayRelName = false;
                                                                    // for name
                                                                    if (adtRelatedName.IsNotNEW())
                                                                    {
                                                                        if (adtRelatedName.Contains('(') || adtRelatedName.Contains(')'))
                                                                        {
                                                                            Dbug.LogPart("Related content name cannot contain parentheses");
                                                                            decodeInfo.NoteIssue("Related content name cannot contain parentheses");
                                                                        }
                                                                        else
                                                                        {
                                                                            adtRelatedName = CharacterKeycodeSubstitution(FixContentName(adtRelatedName));
                                                                            okayRelName = true;
                                                                        }
                                                                    }
                                                                    // for data id
                                                                    if (adtRelatedDataID.IsNotNEW())
                                                                    {
                                                                        if (!adtRelatedDataID.Contains(legRangeKey) && !adtRelatedDataID.Contains(',') && !DetectUsageOfCharacterKeycode(adtRelatedDataID, cks01))
                                                                        {
                                                                            adtRelatedDataID = RemoveParentheses(adtRelatedDataID);
                                                                            if (RemoveNumbers(adtRelatedDataID).IsNotNEW())
                                                                            {
                                                                                /// doesn't interfere with out of scope stuff, ofc...
                                                                                //GetDataKeyAndSuffix(adtRelatedDataID, out string dk, out string sfx);
                                                                                DisassembleDataID(CharacterKeycodeSubstitution(adtRelatedDataID), out string dk, out _, out string sfx);
                                                                                NoteLegendKey(dk, adtRelatedDataID);
                                                                                NoteLegendKey(sfx, adtRelatedDataID);
                                                                                okayRelDataID = true;
                                                                            }
                                                                            else
                                                                            {
                                                                                Dbug.LogPart("Related Data ID was not a Data ID (just numbers)");
                                                                                decodeInfo.NoteIssue("Related Data ID was just numbers (invalid)");
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (DetectUsageOfCharacterKeycode(adtRelatedDataID, cks01))
                                                                            {
                                                                                Dbug.LogPart($"; Related data id may not contain comma CKS ({cks01})");
                                                                                decodeInfo.NoteIssue($"Related Data ID may not contain following character keycode: {cks01}");
                                                                            }
                                                                            else if (!adtRelatedDataID.Contains(legRangeKey))
                                                                            {
                                                                                Dbug.LogPart("There can only be one related Data ID (contains ',')");
                                                                                decodeInfo.NoteIssue("There can only be one related Data ID (contains ',')");
                                                                            }
                                                                            else
                                                                            {
                                                                                Dbug.LogPart($"There can only be one related Data ID (contains range key '{legRangeKey}')");
                                                                                decodeInfo.NoteIssue($"There can only be one related Data ID (contains range key '{legRangeKey}')");
                                                                            }
                                                                            adtRelatedDataID = null;
                                                                        }
                                                                    }
                                                                    Dbug.Log("..");

                                                                    //adtRelatedName = adtRelatedName.IsNEW() ? "<none>" : adtRelatedName;
                                                                    //adtRelatedDataID = adtRelatedDataID.IsNEW() ? "<none>" : adtRelatedDataID;

                                                                    // if (has relDataID, find related ConBase) else (only relName, create new ConBase)
                                                                    if (okayRelName || okayRelDataID)
                                                                    {
                                                                        Dbug.Log($" --> Complete related contents;  Related Name ({(adtRelatedName.IsNEW() ? "<none>" : adtRelatedName)}) and Related Data ID ({(adtRelatedDataID.IsNEW() ? "<none>" : adtRelatedDataID)}); ");
                                                                        continueToNextAdtSubSectionQ = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        Dbug.Log("At least one of the two - Related Name or Related Data ID - must have a value; ");
                                                                        decodeInfo.NoteIssue("At least one of the two - Related Name or Related Data ID - must have a value; ");
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (splitAdditLine[1].CountOccuringCharacter('(') <= 1)
                                                                {
                                                                    Dbug.LogPart("This related content contains too many ')'");
                                                                    decodeInfo.NoteIssue("This related content contains too many ')'");
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("This related content contains too many '('");
                                                                    decodeInfo.NoteIssue("This related content contains too many '('");
                                                                }
                                                            }
                                                        }

                                                        // + associated to related content \ create new content base group +
                                                        if (continueToNextAdtSubSectionQ)
                                                        {
                                                            /// 1st: generate (temporary) additional content instance
                                                            /// 2nd: find a ResCon with appropriate ConBase to attach additional instance to
                                                            ///        (related dataID is main matcher, having related name is double-verification; use both where feasible and applicable)
                                                            /// -- or --
                                                            ///      create new ConBase if there is no ConBase to associate with through the data ID
                                                            if (adtContentName.IsNEW() && adtRelatedDataID.IsNEW())
                                                            {
                                                                // Exchanged addit content name for related (optional) name
                                                                // Exchanged content name for related (opt) name
                                                                // Using related name as content name
                                                                Dbug.LogPart("Using related name as content name; ");
                                                                adtContentName = adtRelatedName;
                                                            }

                                                            // generate additional content instance
                                                            ContentAdditionals newAdditCon = new ContentAdditionals(logVersion, adtRelatedDataID, adtContentName, adtConDataIDs.ToArray());
                                                            Dbug.LogPart($"Generated ContentAdditionals :: {newAdditCon}; Searching for matching ConBase; ");

                                                            ResContents matchingResCon = null;
                                                            if (resourceContents.HasElements() && newAdditCon.RelatedDataID != null)
                                                            {
                                                                // matching with contents in current log decode
                                                                Dbug.LogPart(" in 'Decoded Library' -- ");
                                                                foreach (ResContents resCon in resourceContents)
                                                                    if (resCon.ContainsDataID(newAdditCon.RelatedDataID, out RCFetchSource source))
                                                                    {
                                                                        if (source.Equals(RCFetchSource.ConBaseGroup))
                                                                        {
                                                                            /// id only: save match, keep searching
                                                                            /// id and name: save match, end searching
                                                                            bool matchedNameQ = false;

                                                                            Dbug.LogPart($"; Got match ('{newAdditCon.RelatedDataID}'");
                                                                            if (resCon.ContentName == adtRelatedName)
                                                                            {
                                                                                Dbug.LogPart($", plus matched name '{resCon.ContentName}'");
                                                                                matchedNameQ = true;
                                                                            }
                                                                            Dbug.LogPart(")");
                                                                            matchingResCon = resCon;

                                                                            if (matchedNameQ)
                                                                            {
                                                                                Dbug.LogPart("; Search end");
                                                                                break;
                                                                            }
                                                                            else Dbug.LogPart("; Search continue");
                                                                        }
                                                                    }
                                                            }
                                                            Dbug.Log("; ");

                                                            // if (has relDataID, find related ConBase) else if (only relName, create new ConBase)
                                                            bool completedFinalStepQ = false;
                                                            if (newAdditCon.RelatedDataID.IsNotNE())
                                                            {
                                                                /// connect with ConBase from decoded
                                                                if (matchingResCon != null)
                                                                {
                                                                    matchingResCon.StoreConAdditional(newAdditCon);
                                                                    Dbug.LogPart($"Completed connection of ConAddits ({newAdditCon}) to ConBase ({matchingResCon.ConBase}) [through ID '{newAdditCon.RelatedDataID}']; ");
                                                                    decodeInfo.NoteResult($"Connected ConAddits ({newAdditCon}) to ({matchingResCon.ConBase}) [by ID '{newAdditCon.RelatedDataID}']");
                                                                }
                                                                /// to be connected with ConBase from library
                                                                else
                                                                {
                                                                    looseConAddits.Add(newAdditCon);
                                                                    looseInfoRCDataIDs.Add(newAdditCon.RelatedDataID);
                                                                    Dbug.LogPart($"No connection found: storing 'loose' ConAddits ({newAdditCon}) [using ID '{newAdditCon.RelatedDataID}']");
                                                                    decodeInfo.NoteResult($"Stored loose ConAddits ({newAdditCon}) [using ID '{newAdditCon.RelatedDataID}']");
                                                                }
                                                                completedFinalStepQ = true;
                                                            }
                                                            else if (newAdditCon.OptionalName.IsNotNE())
                                                            {
                                                                string heldIssueMsg = decodeInfo.decodeIssue;
                                                                decodeInfo = new DecodeInfo($"{decodeInfo.logLine} [from Additional]", secAdd);
                                                                if (heldIssueMsg.IsNotNEW())
                                                                {
                                                                    Dbug.LogPart(" Relay:");
                                                                    decodeInfo.NoteIssue(heldIssueMsg);
                                                                    Dbug.LogPart("; ");
                                                                }

                                                                Dbug.LogPart("No Related Data ID, no match with existing ResContents; ");
                                                                ContentBaseGroup adtAsNewContent = new(logVersion, newAdditCon.OptionalName, adtConDataIDs.ToArray());

                                                                /// integrating similar ConAddits that became ConBase (ConAddits-Base) (by name)
                                                                //bool sameNameAdditsContent = false;
                                                                ResContents sameConBaseFromConAddits = null;
                                                                int resConArrIx = 0;
                                                                foreach (ResContents resCon in resourceContents)
                                                                {
                                                                    if (resCon != null)
                                                                    {
                                                                        if (!addedContentsNames.Contains(resCon.ContentName))
                                                                            if (resCon.ContentName == adtAsNewContent.ContentName)
                                                                            {
                                                                                sameConBaseFromConAddits = resCon;
                                                                                break;
                                                                            }
                                                                    }
                                                                    resConArrIx++;
                                                                }

                                                                // integrate with existing ConAddits-Base
                                                                if (sameConBaseFromConAddits != null)
                                                                {
                                                                    int editDiIx = 0;
                                                                    bool foundEditableDi = false;
                                                                    foreach (DecodeInfo di in decodingInfoDock)
                                                                    {
                                                                        if (di.NotedResultQ)
                                                                            if (di.resultingInfo.Contains(sameConBaseFromConAddits.ConBase.ToString()))
                                                                            {
                                                                                foundEditableDi = true;
                                                                                break;
                                                                            }
                                                                        editDiIx++;
                                                                    }                                                                    
                                                                    
                                                                    ContentBaseGroup ogConBase = sameConBaseFromConAddits.ConBase;
                                                                    Dbug.Log($"A ConAddits-to-ConBase ({ogConBase}) has similar content name; ");

                                                                    List<string> compiledDataIds = new();
                                                                    compiledDataIds.AddRange(ogConBase.DataIDString.Split(' '));
                                                                    compiledDataIds.AddRange(adtConDataIDs.ToArray());
                                                                    Dbug.LogPart($"Integrating data IDs ({adtAsNewContent.DataIDString}) into existing ConAddits-to-ConBase; ");

                                                                    adtAsNewContent = new ContentBaseGroup(logVersion, newAdditCon.OptionalName, compiledDataIds.ToArray());
                                                                    resourceContents[resConArrIx] = new ResContents(newResConShelfNumber, adtAsNewContent);
                                                                    Dbug.LogPart($"Regenerated ConBase (@ix{resConArrIx}) from ConAddits info :: {adtAsNewContent}");

                                                                    if (foundEditableDi)
                                                                    {
                                                                        DecodeInfo prevDi = decodingInfoDock[editDiIx];
                                                                        decodingInfoDock[editDiIx] = new DecodeInfo();

                                                                        DecodeInfo rewriteDI = new DecodeInfo($"{prevDi.logLine}\n{decodeInfo.logLine}", prevDi.sectionName);
                                                                        if (prevDi.NotedIssueQ)
                                                                            rewriteDI.NoteIssue(prevDi.decodeIssue);
                                                                        rewriteDI.NoteResult($"Regenerated ConAddits-to-ConBase :: {adtAsNewContent}");
                                                                        decodingInfoDock[editDiIx] = rewriteDI;
                                                                    }
                                                                }
                                                                // create new ConBase
                                                                else
                                                                {
                                                                    resourceContents.Add(new ResContents(newResConShelfNumber, adtAsNewContent));
                                                                    Dbug.LogPart($"Generated new ConBase from ConAddits info :: {adtAsNewContent}");
                                                                    decodeInfo.NoteResult($"Generated ConBase from ConAddits :: {adtAsNewContent}");
                                                                }
                                                                completedFinalStepQ = true;
                                                            }

                                                            sectionIssueQ = !completedFinalStepQ;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (splitAdditLine[0].IsNotNEW())
                                                        {
                                                            Dbug.LogPart("Line is missing information for 'related content';");
                                                            decodeInfo.NoteIssue("Line is missing information for 'related content';");
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart("Line is missing information for 'additional content';");
                                                            decodeInfo.NoteIssue("Line is missing information for 'additional content';");
                                                        }
                                                    }
                                            }
                                            else
                                            {
                                                if (logDataLine.Contains('-'))
                                                {
                                                    Dbug.LogPart("This line contains too many '-'");
                                                    decodeInfo.NoteIssue("This line contains too many '-'");
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("This line is missing '-'");
                                                    decodeInfo.NoteIssue("This line is missing '-'");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbug.LogPart("This line contains too many '>'");
                                                decodeInfo.NoteIssue("This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line does not start with '>'");
                                                decodeInfo.NoteIssue("This line does not start with '>'");
                                            }                                            
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Additional section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue("Additional section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("additional", !IsSectionTag());
                                }

                                /// TOTAL TEXTURES ADDED
                                if (currentSectionName == secTTA)
                                {
                                    /** TOTAL TEXTURES ADDED [TTA] (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        [TTA : n]

                            
                                        DESIGN DOC
                                        --------------------
                                    .	[TTA]
			                            Syntax		[TTA: #]
			                            Ex.			[TTA: 38]
				                            Tag denoting a tally of the number of content added in the specified version of the resource pack. This number only accounts for the contents tally under "ADDED" and "ADDITIONAL".
					                            - It may stand for "Total Textures Added", but it includes the count of ALL contents added.
                                    */

                                    sectionIssueQ = true;

                                    Dbug.LogPart("TTA Data  //  ");
                                    logDataLine = RemoveEscapeCharacters(logDataLine);
                                    if (IsSectionTag() && !parsedSectionTagQ)
                                    {
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(':') && logDataLine.CountOccuringCharacter(':') == 1)
                                        {
                                            Dbug.LogPart($"Contains ':', raw data ({logDataLine}); ");
                                            string[] splitLogData = logDataLine.Split(':');
                                            if (splitLogData.HasElements())
                                                if (splitLogData[1].IsNotNEW())
                                                {
                                                    Dbug.Log($"Has sufficent elements after split -- parsing; ");
                                                    if (int.TryParse(splitLogData[1], out int ttaNum))
                                                    {
                                                        if (ttaNum >= 0)
                                                        {
                                                            bool ranVerificationCountQ = false, checkVerifiedQ = false;
                                                            int countedTTANum = 0;
                                                            if (resourceContents.HasElements())
                                                            {
                                                                /// the count does not account for data IDs compared by suffixes (q1 and q1` are seen as different Ids)      
                                                                /// this should be okay since a data ID should be consistently written with its suffix
                                                                
                                                                List<string> discreteDataIDs = new();
                                                                // counts the data IDs from recently decoded contents
                                                                foreach (ResContents resCon in resourceContents)
                                                                {
                                                                    if (resCon != null)
                                                                    {
                                                                        if (resCon.ConBase != null)
                                                                            if (resCon.ConBase.IsSetup())
                                                                            {
                                                                                for (int ax = 0; ax < resCon.ConBase.CountIDs; ax++)
                                                                                    if (!discreteDataIDs.Contains(resCon.ConBase[ax]))
                                                                                        discreteDataIDs.Add(resCon.ConBase[ax]);
                                                                            }

                                                                        if (resCon.ConAddits != null)
                                                                            foreach (ContentAdditionals conAddit in resCon.ConAddits)
                                                                            {
                                                                                if (conAddit != null)
                                                                                    if (conAddit.IsSetup())
                                                                                        for (int bx = 0; bx < conAddit.CountIDs; bx++)
                                                                                            if (!discreteDataIDs.Contains(conAddit[bx]))
                                                                                                discreteDataIDs.Add(conAddit[bx]);
                                                                            }
                                                                    }
                                                                }

                                                                // also counts data Ids from loose conAddits
                                                                foreach (ContentAdditionals conAdt in looseConAddits)
                                                                {
                                                                    if (conAdt != null)
                                                                        if (conAdt.IsSetup())
                                                                            for (int cx = 0; cx < conAdt.CountIDs; cx++)
                                                                                if (!discreteDataIDs.Contains(conAdt[cx]))
                                                                                    discreteDataIDs.Add(conAdt[cx]);
                                                                }

                                                                #region old code
                                                                //// only counts within the recently decoded contents
                                                                //foreach (ResContents resCon in resourceContents)
                                                                //{
                                                                //    if (resCon != null)
                                                                //    {
                                                                //        // count from content base group
                                                                //        if (resCon.ConBase != null)
                                                                //            if (resCon.ConBase.IsSetup())
                                                                //                countedTTANum += resCon.ConBase.CountIDs;

                                                                //        // count from additional contents
                                                                //        if (resCon.ConAddits.HasElements())
                                                                //            foreach (ContentAdditionals conAddit in resCon.ConAddits)
                                                                //            {
                                                                //                if (conAddit != null)
                                                                //                    if (conAddit.IsSetup())
                                                                //                        countedTTANum += conAddit.CountIDs;
                                                                //            }
                                                                //    }
                                                                //}

                                                                //// also count loose conAddits
                                                                //foreach (ContentAdditionals conAdt in looseConAddits)
                                                                //{
                                                                //    if (conAdt != null)
                                                                //        if (conAdt.IsSetup())
                                                                //            countedTTANum += conAdt.CountIDs;
                                                                //}
                                                                #endregion
                                                                countedTTANum = discreteDataIDs.Count;
                                                                ranVerificationCountQ = true;
                                                            }

                                                            Dbug.LogPart($"--> Obtained TTA number '{ttaNum}'");
                                                            if (ranVerificationCountQ)
                                                            {
                                                                if (countedTTANum == ttaNum)
                                                                {
                                                                    Dbug.LogPart(" [Verified!]");
                                                                    checkVerifiedQ = true;
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart($" [Check counted '{countedTTANum}' instead]");
                                                                    decodeInfo.NoteIssue($"TTA verification counted '{countedTTANum}' instead of obtained number '{ttaNum}'");
                                                                }
                                                            }
                                                            decodeInfo.NoteResult($"Got TTA number '{ttaNum}'");

                                                            ttaNumber = ttaNum;
                                                            sectionIssueQ = !checkVerifiedQ;
                                                            parsedSectionTagQ = true;
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart("TTA number is less than zero (0)");
                                                            decodeInfo.NoteIssue("TTA number cannot be less than zero (0)");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart("TTA number could not be parsed");
                                                        decodeInfo.NoteIssue("TTA number could not be parsed");
                                                    }
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("No data in split @ix1");
                                                    decodeInfo.NoteIssue("No data provied for TTA number");
                                                }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains(':'))
                                            {
                                                Dbug.LogPart("This line has too many ':'");
                                                decodeInfo.NoteIssue("This line has too many ':'");
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower().StartsWith(secTTA.ToLower()))
                                                {
                                                    Dbug.LogPart("This line is missing ':'");
                                                    decodeInfo.NoteIssue("This line is missing ':'");
                                                }
                                                else
                                                {
                                                    Dbug.Log($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                    decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                }                                                
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (parsedSectionTagQ)
                                        {
                                            Dbug.LogPart("TTA section tag has already been parsed");
                                            decodeInfo.NoteIssue("TTA section tag has already been parsed");
                                        }
                                        else if (logDataLine.StartsWith(secBracL))
                                        {
                                            Dbug.LogPart("This line does not end with ']'");
                                            decodeInfo.NoteIssue("This line does not end with ']'");
                                        }
                                        else
                                        {
                                            Dbug.LogPart("This line does not start with '['");
                                            decodeInfo.NoteIssue("This line does not start with '['");
                                        }
                                    }
                                    
                                    EndSectionDbugLog("tta", null);
                                }

                                /// UPDATED 
                                if (currentSectionName == secUpd)
                                {
                                    /** UPDATED (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        [UPDATED]
                                        -- > <InternalName> (<RelatedDataId>) - change description
                                        Ex.
                                        > UFO (t23) - Laser color was incorrect.
                                        > UFO (t24) - Ship chasis' border was missing.                         


                            
                                        DESIGN DOC
                                        --------------------
                                    .	[UPDATED]
			                            Syntax		> {InternalName} ({RelatedDataID}) - {change description}
			                            Ex. 		> Spoiled Potato (i38) - Recolored to look less appetizing.
				                            Tag denoting changes that are made to existing content. The information written would be appended as a history of changes made to the existing content.
				                            . {InternalName} - The name of the content being updated
				                            . {RelatedDataID} - The data ID relevant to the named content being updated
				                            . {change description} - A description of the change(s) made to given
                                    */

                                    sectionIssueQ = true;
                                    bool isHeader = false;

                                    /// updated header (imparsable, but not an issue)
                                    if (IsSectionTag())
                                    {
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        
                                        isHeader = true;
                                        if (IsValidSectionTag())
                                        {
                                            /// ....
                                            if (!parsedSectionTagQ)
                                            {
                                                decodeInfo.NoteResult("Updated section tag identified");
                                                sectionIssueQ = false;
                                                parsedSectionTagQ = true;
                                            }
                                            else
                                            {
                                                Dbug.Log("Updated section tag has already been parsed");
                                                decodeInfo.NoteIssue("Updated section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbug.Log($"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// updated contents
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Identified updated data; ");
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        if (logDataLine.StartsWith('>') && logDataLine.CountOccuringCharacter('>') == 1)
                                        {
                                            
                                            /// Taco Sauce (y32) - Fixed to look much saucier
                                            /// (q42) - Redesigned to look cooler                                            
                                            if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                            {
                                                bool haltQ = false;
                                                bool selfUpdatingAllowed = false;
                                                if (logDataLine.Contains(updtSelfKey) && logDataLine.CountOccuringCharacter(updtSelfKey) == 1)
                                                {
                                                    if (logDataLine.Contains($">{updtSelfKey}"))
                                                    {
                                                        selfUpdatingAllowed = true;
                                                        logDataLine = logDataLine.Replace(":", "");
                                                        Dbug.LogPart($"Self-updating enabled, contains '>{updtSelfKey}'; ");
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart($"Character '{updtSelfKey}' may only follow after '>' to enable self-updating function");
                                                        decodeInfo.NoteIssue($"Character '{updtSelfKey}' may only follow after '>' to enable self-updating function");
                                                    }
                                                }
                                                else
                                                {
                                                    if (logDataLine.CountOccuringCharacter(updtSelfKey) > 1)
                                                    {
                                                        Dbug.LogPart($"This line contains too many '{updtSelfKey}'");
                                                        decodeInfo.NoteIssue($"This line contains too many '{updtSelfKey}'");
                                                        haltQ = true;
                                                    }
                                                }
                                                    

                                                logDataLine = logDataLine.Replace(">", "");

                                                /// Taco Sauce (y32)  <-->  Fixed to look much saucier
                                                /// (q42)  <-->  Redesigned to look cooler
                                                Dbug.LogPart("Contains '-'; ");
                                                string[] splitLogLine = logDataLine.Split('-');
                                                if (splitLogLine.HasElements(2) && !haltQ)
                                                    if (splitLogLine[0].IsNotNEW() && splitLogLine[1].IsNotNEW())
                                                    {
                                                        Dbug.Log($"Got updated content info ({splitLogLine[0]}) and change description ({splitLogLine[1]}); ");

                                                        // updated contents info
                                                        Dbug.LogPart("Content Parts: ");
                                                        string updtContentName = "", updtDataID = "", updtChangeDesc = "";
                                                        splitLogLine[0] = splitLogLine[0].Replace("(", " (").Replace(")", ") ");
                                                        string[] updtConData = splitLogLine[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                        for (int ux = 0; ux < updtConData.Length; ux++)
                                                        {
                                                            string updtDataPart = updtConData[ux];
                                                            if (updtDataPart.Contains('(') && updtDataPart.Contains(')') && updtDataID.IsNE())
                                                                updtDataID = updtDataPart;
                                                            else updtContentName += $"{updtDataPart} ";
                                                            Dbug.LogPart($"{updtDataPart}{(ux + 1 == updtConData.Length ? "" : "|")}");
                                                        }
                                                        Dbug.LogPart("; ");
                                                        updtDataID = RemoveParentheses(updtDataID);
                                                        updtContentName = CharacterKeycodeSubstitution(FixContentName(updtContentName));
                                                        Dbug.Log("..");

                                                        bool okayDataIDQ = false;
                                                        if (updtDataID.Contains(',') || updtDataID.Contains(legRangeKey) || DetectUsageOfCharacterKeycode(updtDataID, cks01))
                                                        {
                                                            Dbug.LogPart("Only one updated ID may be referenced per update");
                                                            if (DetectUsageOfCharacterKeycode(updtDataID, cks01))
                                                            {
                                                                Dbug.LogPart($" (contains comma CKS '{cks01}')");
                                                                decodeInfo.NoteIssue($"Only one updated ID can be referenced (no data groups; contains character keycode '{cks01}')");
                                                            }
                                                            else if (updtDataID.Contains(','))
                                                            {
                                                                Dbug.LogPart(" (contains ',')");
                                                                decodeInfo.NoteIssue($"Only one updated ID can be referenced (no data groups; contains ',')");
                                                            }
                                                            else
                                                            {
                                                                Dbug.LogPart($" (contains '{legRangeKey}')");
                                                                decodeInfo.NoteIssue($"Only one updated ID can be referenced (no ranges; contains '{legRangeKey}')");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (updtDataID.IsNEW())
                                                            {
                                                                Dbug.LogPart("No updated ID has been provided");
                                                                decodeInfo.NoteIssue("No updated ID has been provided");
                                                            }
                                                            else
                                                            {
                                                                /// numberless is okay, a nonID content could be updated; .. just numbers though, is not allowed
                                                                if (RemoveNumbers(updtDataID).IsNotNEW())
                                                                {
                                                                    updtDataID = CharacterKeycodeSubstitution(updtDataID);

                                                                    //GetDataKeyAndSuffix(updtDataID, out string dk, out string sfx);
                                                                    DisassembleDataID(updtDataID, out string dk, out _, out string sfx);
                                                                    NoteLegendKey(dk, updtDataID);
                                                                    NoteLegendKey(sfx, updtDataID);

                                                                    okayDataIDQ = true;
                                                                    Dbug.Log($"Completed updated contents: Updated Name ({(updtContentName.IsNEW() ? "<null>" : updtContentName)}) and Data ID ({updtDataID}); ");
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("Updated ID was not a data ID (just numbers)");
                                                                    decodeInfo.NoteIssue("Updated ID was just numbers (invalid)");
                                                                }                                                                
                                                            }
                                                        }

                                                        // updated description
                                                        if (okayDataIDQ)
                                                            updtChangeDesc = FullstopCheck(CharacterKeycodeSubstitution(splitLogLine[1]));

                                                        // generate ContentChanges, connect to ConBase or ConAddits, then attach to ResCon
                                                        if (okayDataIDQ)
                                                        {
                                                            ContentChanges newConChanges = new(logVersion, updtContentName, updtDataID, updtChangeDesc);
                                                            Dbug.Log($"Generated {nameof(ContentChanges)} instance :: {newConChanges}; ");

                                                            /// testing... and now enabled, self updating contents aren't loose
                                                            bool isSelfConnected = false;
                                                            if (enableSelfUpdatingFunction && selfUpdatingAllowed)
                                                            {
                                                                Dbug.LogPart("[SELF-UPDATING] Searching for connection in 'Decoded Library' -- ");

                                                                ResContents matchingResCon = null;
                                                                ContentAdditionals subMatchConAdd = new();
                                                                /// should only be checking with existing library; *can't* update something that was just added
                                                                if (resourceContents != null)
                                                                {
                                                                    if (resourceContents.HasElements())
                                                                        foreach (ResContents resCon in resourceContents)
                                                                        {
                                                                            if (resCon.ContainsDataID(updtDataID, out RCFetchSource source, out DataHandlerBase conAddix))
                                                                            {
                                                                                subMatchConAdd = new ContentAdditionals();
                                                                                if (source == RCFetchSource.ConAdditionals)
                                                                                    subMatchConAdd = (ContentAdditionals)conAddix;
                                                                                matchingResCon = resCon;

                                                                                /// id only: save match, keep searching
                                                                                /// id and name: save match, end searching
                                                                                bool matchedNameQ = false;

                                                                                Dbug.LogPart($" Got match ('{newConChanges.RelatedDataID}' from source '{source}'");
                                                                                /// checks for matching content name against ConBase or ConAddits
                                                                                if (source.Equals(RCFetchSource.ConBaseGroup))
                                                                                {
                                                                                    if (resCon.ContentName == updtContentName)
                                                                                    {
                                                                                        Dbug.LogPart($", plus matched name '{resCon.ContentName}'");
                                                                                        matchedNameQ = true;
                                                                                    }
                                                                                }
                                                                                else if (subMatchConAdd.IsSetup() && source.Equals(RCFetchSource.ConAdditionals))
                                                                                {
                                                                                    if (subMatchConAdd.OptionalName == updtContentName)
                                                                                    {
                                                                                        Dbug.LogPart($", plus matched name '{subMatchConAdd.OptionalName}'");
                                                                                        matchedNameQ = true;
                                                                                    }
                                                                                }
                                                                                Dbug.LogPart(")");

                                                                                if (matchedNameQ)
                                                                                {
                                                                                    Dbug.LogPart("; Search end");
                                                                                    break;
                                                                                }
                                                                                else Dbug.LogPart("; Search continue");
                                                                            }
                                                                        }
                                                                    Dbug.Log("; ");
                                                                }

                                                                /// matching self-updated content connection
                                                                if (matchingResCon != null)
                                                                {
                                                                    matchingResCon.StoreConChanges(newConChanges);
                                                                    Dbug.LogPart($"Completed connection of ConChanges ({newConChanges}) using ");
                                                                    if (subMatchConAdd.IsSetup())
                                                                        Dbug.LogPart($"ConAddits ({subMatchConAdd})");
                                                                    else Dbug.LogPart($"ConBase ({matchingResCon.ConBase})");
                                                                    Dbug.Log($" [by ID '{newConChanges.RelatedDataID}'] (self-updated)");
                                                                    decodeInfo.NoteResult($"Connected ConChanges ({newConChanges}) to {(subMatchConAdd.IsSetup() ? $"ConAddits ({subMatchConAdd})" : $"ConBase {matchingResCon.ConBase}")} [by ID '{newConChanges.RelatedDataID}']");

                                                                    decodeInfo.NoteIssue("Self-updating content: updating content in its introduced version is advised against");
                                                                    isSelfConnected = true;

                                                                    // these actions just for the decode display on log submission page
                                                                    ContentChanges copy4NonLoose = new(logVersion, newConChanges.InternalName + Sep, updtDataID, updtChangeDesc);
                                                                    looseConChanges.Add(copy4NonLoose); 
                                                                    looseInfoRCDataIDs.Add(copy4NonLoose.RelatedDataID);
                                                                    Dbug.LogPart($" Added ConChanges copy (IntNam: '{copy4NonLoose.InternalName}') to loose for decode display");
                                                                }
                                                            }

                                                            /// the normal function: updates are always loose contents
                                                            if (!isSelfConnected)
                                                            {
                                                                looseConChanges.Add(newConChanges);
                                                                looseInfoRCDataIDs.Add(newConChanges.RelatedDataID);
                                                                //Dbug.LogPart($"Storing 'loose' ConChanges [using ID '{newConChanges.RelatedDataID}']");
                                                                //decodeInfo.NoteResult($"Stored loose ConChanges ({newConChanges}) [using ID '{newConChanges.RelatedDataID}']");
                                                                Dbug.LogPart($"Storing 'loose' ConChanges");
                                                                decodeInfo.NoteResult($"Stored loose ConChanges ({newConChanges})");
                                                            }
                                                            sectionIssueQ = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (splitLogLine[0].IsNEW())
                                                        {
                                                            Dbug.LogPart("This line is missing data for 'updated content'");
                                                            decodeInfo.NoteIssue("This line is missing data for 'updated content'");
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart("This line is missing data for 'change description'");
                                                            decodeInfo.NoteIssue("This line is missing data for 'change description'");
                                                        }
                                                    }
                                            }
                                            else
                                            {
                                                if (logDataLine.Contains('-'))
                                                {
                                                    Dbug.LogPart("This line contains too many '-'");
                                                    decodeInfo.NoteIssue("This line contains too many '-'");
                                                }
                                                else
                                                {
                                                    Dbug.LogPart("This line is missing '-'");
                                                    decodeInfo.NoteIssue("This line is missing '-'");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbug.LogPart("This line contains too many '>'");
                                                decodeInfo.NoteIssue("This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line does not start with '>'");
                                                decodeInfo.NoteIssue("This line does not start with '>'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Updated section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue("Updated section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("updated", !IsSectionTag());

                                }

                                /// LEGEND
                                if (currentSectionName == secLeg)
                                {
                                    /** LEGEND (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        -- Must be written as 'key - Keyname'
                                        ...

                            

                                        DESIGN DOC
                                        --------------------
                                    .	[LEGEND]
			                            Syntax		> {Key} - {Keyname}
			                            Ex.			> i - Item
				                            {REQUIRED} This tag translates directly to the auto logger, denoting a definition of the symbols / keys used to describe the additions and changes made in the version through the previous 3 tags (ADDED, ADDITIONAL, and UPDATED).
                                    */

                                    sectionIssueQ = true;
                                    bool isHeader = false;

                                    /// legend header (imparsable, but not an issue)
                                    if (IsSectionTag())
                                    {
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        
                                        isHeader = true;
                                        if (IsValidSectionTag())
                                        {
                                            if (!parsedSectionTagQ)
                                            {
                                                decodeInfo.NoteResult("Legend section tag identified");
                                                sectionIssueQ = false;
                                                parsedSectionTagQ = true;
                                            }
                                            else
                                            {
                                                Dbug.Log("Legend section tag has already been parsed");
                                                decodeInfo.NoteIssue("Legend section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbug.Log($"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// legend data
                                    if (!isHeader && parsedSectionTagQ) 
                                    {
                                        if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                        {
                                            Dbug.LogPart("Identified legend data; Contains '-'; ");
                                            logDataLine = RemoveEscapeCharacters(logDataLine);
                                            string[] splitLegKyDef = logDataLine.Split('-');
                                            if (splitLegKyDef.HasElements(2))
                                                if (splitLegKyDef[0].IsNotNEW() && splitLegKyDef[1].IsNotNEW())
                                                {
                                                    splitLegKyDef[0] = splitLegKyDef[0].Trim();
                                                    if (splitLegKyDef[0].Contains(" "))
                                                    {
                                                        splitLegKyDef[0] = splitLegKyDef[0].Replace(" ", "").Trim();
                                                        Dbug.LogPart("Removed space in key; ");
                                                    }
                                                    splitLegKyDef[1] = splitLegKyDef[1].Trim();
                                                    Dbug.LogPart($"Trimmed data; ");


                                                    LegendData newLegData = new(splitLegKyDef[0], logVersion, splitLegKyDef[1]);
                                                    Dbug.Log($"Generated new {nameof(LegendData)} :: {newLegData.ToString()}; ");
                                                    decodeInfo.NoteResult($"Generated {nameof(LegendData)} :: {newLegData.ToString()}");
                                                    if (splitLegKyDef[1].Contains(" "))
                                                    {
                                                        /// this warns for a data ID legend key with a legend containing spaces (to check spacing)
                                                        bool warnSpaceInDataIDKeyDefinitionQ = false;
                                                        foreach (char keyChar in splitLegKyDef[0])
                                                        {
                                                            if (Extensions.CharScore(keyChar) > 10)
                                                                warnSpaceInDataIDKeyDefinitionQ = true;
                                                        }

                                                        if (warnSpaceInDataIDKeyDefinitionQ)
                                                            decodeInfo.NoteIssue("Legend definition should not have spaces if representing a data ID");
                                                    }


                                                    if (legendDatas.HasElements())
                                                    {
                                                        Dbug.LogPart("Searching for existing key; ");
                                                        LegendData matchingLegDat = null;
                                                        foreach (LegendData legDat in legendDatas)
                                                        {
                                                            if (legDat.IsSetup())
                                                                if (legDat.Key == newLegData.Key)
                                                                {
                                                                    Dbug.LogPart($"Found matching key ({newLegData.Key}) in ({legDat}); ");
                                                                    matchingLegDat = legDat;
                                                                    break;
                                                                }
                                                        }

                                                        if (matchingLegDat != null)
                                                        {
                                                            /// find matching decode issue
                                                            int editDiIx = -1;
                                                            DecodeInfo newDecInfo = new();
                                                            for (int dx = 0; dx < decodingInfoDock.Count && editDiIx == -1; dx++)
                                                            {
                                                                DecodeInfo diToCheck = decodingInfoDock[dx];
                                                                if (diToCheck.NotedResultQ)
                                                                    if (diToCheck.resultingInfo.Contains(matchingLegDat.ToString()))
                                                                    {
                                                                        editDiIx = dx;
                                                                        newDecInfo = new DecodeInfo($"{diToCheck.logLine}\n{logDataLine}", currentSectionName);
                                                                        newDecInfo.NoteResult(diToCheck.resultingInfo);
                                                                    }
                                                            }

                                                            
                                                            bool addedDef = matchingLegDat.AddKeyDefinition(newLegData[0]);
                                                            if (addedDef)
                                                            {
                                                                Dbug.LogPart($"Expanded legend data :: {matchingLegDat.ToString()}");
                                                                newDecInfo.NoteResult($"Edited existing legend data (new definition) :: {matchingLegDat.ToString()}");
                                                            }
                                                            else
                                                            {
                                                                Dbug.LogPart($"New definition '{newLegData[0]}' could not be added (duplicate?)");
                                                                newDecInfo.NoteIssue($"New definition '{newLegData[0]}' could not be added (duplicate?)");
                                                            }

                                                            if (newDecInfo.IsSetup())
                                                            {
                                                                decodeInfo = new();
                                                                decodingInfoDock[editDiIx] = newDecInfo;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart("No similar key exists; ");
                                                            legendDatas.Add(newLegData);
                                                            Dbug.LogPart("Added new legend data to decode library");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        legendDatas.Add(newLegData);
                                                        Dbug.LogPart("Added new legend data to decode library");
                                                    }                                                    
                                                    //Dbug.LogPart("; ");

                                                    sectionIssueQ = false;
                                                }
                                                else
                                                {
                                                    if (splitLegKyDef[0].IsNEW())
                                                    {
                                                        Dbug.LogPart("This line is missing data for 'legend key'");
                                                        decodeInfo.NoteIssue("This line is missing data for 'legend key'");
                                                    }
                                                    else
                                                    {
                                                        Dbug.LogPart("This line is missing data for 'key definition'");
                                                        decodeInfo.NoteIssue("This line is missing data for 'key definition'");
                                                    }
                                                }
                                            //Dbug.Log("  //  End legend");
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains('-'))
                                            {
                                                Dbug.LogPart("This line contains too many '-'");
                                                decodeInfo.NoteIssue("This line contains too many '-'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line is missing '-'");
                                                decodeInfo.NoteIssue("This line is missing '-'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Legend section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue("Legend section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("legend", !IsSectionTag());
                                }

                                /// SUMMARY 
                                if (currentSectionName == secSum)
                                {
                                    /** <SecName> (Log Template & Design Doc)
                                       LOG TEMPLATE
                                       --------------------
                                       [SUMMARY]
                                       > <infoP1>
                                       > <infoP2>
                                       > ...                           

                           
                                       DESIGN DOC
                                       --------------------
                                       ...
                                    .	[SUMMARY]
			                            Syntax		> <infoPartN>
			                            Ex. 		> Potatoes, Carrots, Onions, Money and Coins
						                            > Gravel, Dirt, Some Sedimentary Rocks, Igneous Rocks
				                            {NEW TAG} This tag allows the addition of a summary of the contents contained from a version log where each line of summarizing text start with '>' below the 'SUMMARY' header tag. 
                                    */

                                    sectionIssueQ = true;
                                    bool isHeader = false;

                                    /// summary header (imparsable, but not an issue)
                                    if (IsSectionTag())
                                    {
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        
                                        isHeader = true;
                                        if (IsValidSectionTag())
                                        {
                                            if (!parsedSectionTagQ)
                                            {
                                                decodeInfo.NoteResult("Summary section tag identified");
                                                sectionIssueQ = false;
                                                parsedSectionTagQ = true;
                                            }
                                            else
                                            {
                                                Dbug.LogPart("Summary section tag has already been parsed");
                                                decodeInfo.NoteIssue("Summary section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbug.Log($"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue($"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// summary data
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        logDataLine = logDataLine.Trim();
                                        Dbug.LogPart("Identified summary data; ");
                                        if (logDataLine.StartsWith(">") && logDataLine.CountOccuringCharacter('>') == 1)
                                        {
                                            Dbug.LogPart("Starts with '>'; ");
                                            string sumPart = FullstopCheck(logDataLine.Replace(">", ""));
                                            if (sumPart.IsNotNE())
                                            {
                                                bool isDupe = false;
                                                if (summaryDataParts.HasElements())
                                                {
                                                    Dbug.LogPart("Valid summary part? ");
                                                    for (int sdpx = 0; sdpx < summaryDataParts.Count && !isDupe; sdpx++)
                                                    {
                                                        if (summaryDataParts[sdpx] == sumPart)
                                                            isDupe = true;
                                                    }
                                                    if (isDupe)
                                                    {
                                                        Dbug.LogPart("No, this summary part is a duplicate");
                                                        decodeInfo.NoteIssue("Summary part is a duplicate");
                                                    }
                                                    else Dbug.LogPart("Yes (not a duplicate)");
                                                    Dbug.Log("; ");
                                                }

                                                if (!isDupe)
                                                {
                                                    summaryDataParts.Add(sumPart);
                                                    Dbug.LogPart($"--> Obtained summary part :: {sumPart}");
                                                    decodeInfo.NoteResult($"Obtained summary part :: {sumPart}");

                                                    sectionIssueQ = false;
                                                }                                                
                                            }
                                            else
                                            {
                                                Dbug.LogPart("Missing data for 'summary part'");
                                                decodeInfo.NoteIssue("Missing data for 'summary part'");
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbug.LogPart("This line contains too many '>'");
                                                decodeInfo.NoteIssue("This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbug.LogPart("This line does not start with '>'");
                                                decodeInfo.NoteIssue("This line does not start with '>'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbug.LogPart("Summary section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue("Summary section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("summary", !IsSectionTag());
                                }


                                /// decoding info dock reports
                                if (decodeInfo.IsSetup())
                                {
                                    Dbug.LogPart($"{Ind34}//{Ind34}{{DIDCheck}} ");
                                    /// check if any decoding infos have been inserted                                    
                                    if (prevDecodingInfoDock.HasElements())
                                    {
                                        //Dbug.LogPart("      {DIDCheck} ");
                                        string changedIndex = "", changeType = "None";
                                        for (int px = 0; px < prevDecodingInfoDock.Count && changedIndex.IsNE(); px++)
                                        {
                                            /// determine if there has been a change
                                            DecodeInfo prevDI = prevDecodingInfoDock[px];
                                            if (px < decodingInfoDock.Count)
                                            {
                                                DecodeInfo dI = decodingInfoDock[px];
                                                if (!prevDI.Equals(dI))
                                                {
                                                    changedIndex = $"@{px}";
                                                    /// determine if changes is an insert or an edit
                                                    /// IF next DI is fetchable: (IF next DI is same as previous' DI: consider change 'insert'; ELSE consider change 'edit n add'); 
                                                    /// ELSE consider change 'edit'
                                                    if (px + 1 < decodingInfoDock.Count)
                                                    {
                                                        DecodeInfo dIAfter = decodingInfoDock[px + 1];
                                                        if (dIAfter.Equals(prevDI))
                                                            changeType = "Insert";
                                                        else
                                                        {
                                                            changeType = "Edit n Add";
                                                            changedIndex += $"~@{decodingInfoDock.Count - 1}";
                                                        }
                                                    }
                                                    else changeType = "Edit";
                                                }
                                            }
                                        }

                                        Dbug.LogPart($"[{changeType}]{changedIndex}");
                                        Dbug.LogPart(";  ");
                                        //Dbug.Log(";  //  ");
                                    }

                                    /// add new decoding info dock
                                    if (decodeInfo.IsSetup())
                                    {
                                        Dbug.LogPart($"[Added]@{decodingInfoDock.Count};  ");
                                        decodingInfoDock.Add(decodeInfo);
                                    }

                                    /// set previous decoding info dock
                                    if (decodingInfoDock.HasElements())
                                    {
                                        //Dbug.LogPart($"[<]prevDID set;  ");
                                        prevDecodingInfoDock = new List<DecodeInfo>();
                                        prevDecodingInfoDock.AddRange(decodingInfoDock.ToArray());
                                    }
                                }                                
                                Dbug.Log(" // ");


                                if (sectionIssueQ || decodeInfo.NotedIssueQ)
                                    if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1)))
                                        countSectionIssues[currentSectionNumber]++;

                                Dbug.NudgeIndent(false);
                            }

                            
                            // method
                            bool IsValidSectionTag()
                            {
                                return logDataLine.ToLower().Contains($"[{currentSectionName.ToLower()}]");
                            }
                            bool IsSectionTag()
                            {
                                return RemoveEscapeCharacters(logDataLine).StartsWith(secBracL) && RemoveEscapeCharacters(logDataLine).EndsWith(secBracR);
                            }
                            void EndSectionDbugLog(string section, bool? isDataQ)
                            {
                                string subText = "", preText = "  //  ";
                                if (isDataQ.HasValue)
                                {
                                    subText = isDataQ.Value ? "(data)" : "(section tag)";
                                    if (isDataQ.Value)
                                    {
                                        Dbug.Log(" // ");
                                        preText = ".   ";
                                    }
                                }

                                //Dbug.Log($"{preText}End {section} {subText}");
                                Dbug.LogPart($"{preText}End {section} {subText}");
                            }
                        }
                        else
                        {
                            if (withinOmitBlock)
                            {
                                //Dbug.Log($"Omitting L{lineNum} --> Block Omission: currently within line omission block.");
                                Dbug.LogPart($"{lineNum} ");
                            }
                            else if (invalidLine)
                            {
                                /// report invalid key issue
                                DecodeInfo diMain = new($"L{lineNum}| {logDataLine}", "Main Decoding");
                                diMain.NoteIssue($"The '{Sep}' character is an invalid character and may not be placed within decodable log lines.");
                                decodingInfoDock.Add(diMain);

                                Dbug.Log($"Skipping L{lineNum} --> Contains invalid character: '{Sep}'");
                            }
                            else Dbug.Log($"Omitting L{lineNum} --> Imparsable: Line starts with '{omit}'");
                        }
                    }
                    Dbug.NudgeIndent(false);
                    
                    // aka 'else'
                    if (logDataLine.IsNEW() && !endFileReading)
                    {
                        // as weird as it may seem, checks for non-described or unnoted legend keys goes here
                        if (!ranLegendChecksQ && usedLegendKeys.HasElements() && usedLegendKeysSources.HasElements() && legendDatas.HasElements() && currentSectionName == secLeg)
                        {
                            Dbug.NudgeIndent(true);
                            Dbug.Log($"{{Legend_Checks}}");
                            const string clampSuffix = "...";
                            const int clampDistance = 7;

                            Dbug.NudgeIndent(true);
                            // get all keys
                            /// from notes
                            List<string> allKeys = new();
                            List<string> allKeysSources = null;
                            Dbug.LogPart(">> Fetching used legend keys (and sources) :: ");
                            if (usedLegendKeys.Count == usedLegendKeysSources.Count)
                            {
                                allKeysSources = new();
                                for (int ukx = 0; ukx < usedLegendKeys.Count; ukx++)
                                {
                                    string usedKey = usedLegendKeys[ukx];
                                    string ukSource = usedLegendKeysSources[ukx];

                                    string addedQ = null;
                                    if (!allKeys.Contains(usedKey))
                                    {
                                        allKeys.Add(usedKey);
                                        allKeysSources.Add(ukSource);
                                        addedQ = "+";
                                    }
                                    Dbug.LogPart($" {addedQ}'{usedKey}' ");
                                }
                            }
                            else
                            {
                                /// backup - legkeys only
                                Dbug.LogPart("[aborted sources; UKLen!=UKSLen] :: ");
                                foreach (string usedKey in usedLegendKeys)
                                {
                                    string addedQ = null;
                                    if (!allKeys.Contains(usedKey))
                                    {
                                        allKeys.Add(usedKey);
                                        addedQ = "+";
                                    }
                                    Dbug.LogPart($" {addedQ}'{usedKey}' ");
                                }
                            }
                            Dbug.Log("; ");
                            /// from generations
                            Dbug.LogPart(">> Fetching generated legend keys (& decInfoIx as '@#') :: ");
                            List<int> decodeInfoIndicies = new();
                            List<string> generatedKeys = new();
                            foreach (LegendData legDat in legendDatas)
                            {
                                string addedQ = null;
                                if (!allKeys.Contains(legDat.Key))
                                {
                                    allKeys.Add(legDat.Key);
                                    addedQ = "+";
                                }

                                string diIx = "";
                                for (int lix = 0; lix < decodingInfoDock.Count && addedQ.IsNotNE(); lix++)
                                {
                                    DecodeInfo lgdDi = decodingInfoDock[lix];
                                    if (lgdDi.IsSetup() && lgdDi.sectionName == secLeg)
                                        if (lgdDi.NotedResultQ)
                                            if (lgdDi.resultingInfo.Contains(legDat.ToString()))
                                            {
                                                diIx = $"@{lix}";
                                                decodeInfoIndicies.Add(lix);
                                            }
                                }

                                generatedKeys.Add(legDat.Key);
                                Dbug.LogPart($" {addedQ}'{legDat.Key}' {diIx} ");
                            }
                            Dbug.Log("; ");

                            // check all keys and determine if unnoted or unused
                            Dbug.Log("Checking all legend keys for possible issues; ");
                            int getAccessIxFromListForOGdi = 0, usedSourceIx = 0;
                            foreach (string aKey in allKeys)
                            {
                                bool issueWasTriggered = false;
                                bool isUsed = usedLegendKeys.Contains(aKey);
                                bool isGenerated = generatedKeys.Contains(aKey);
                                DecodeInfo legCheckDi = new("<no source line>", secLeg);

                                if (isUsed || isGenerated)
                                {
                                    Dbug.LogPart($"Checked: {(isUsed ? "  used" : "NO USE")}|{(isGenerated ? "gen'd " : "NO GEN")} // Result for key '{aKey}'  --> ");
                                    //Dbug.LogPart($"key [{aKey}]  |  used? [{isUsed}]  |  generated? [{isGenerated}]  -->  ");
                                }

                                /// unnnoted key issue
                                if (isUsed && !isGenerated)
                                {
                                    /// grab from source
                                    if (allKeysSources.HasElements())
                                    {
                                        bool haltSourceSearchQ = false;
                                        for (int sx = 0; sx < decodingInfoDock.Count && !haltSourceSearchQ; sx++)
                                        {
                                            DecodeInfo di = decodingInfoDock[sx];
                                            string ukSource = allKeysSources[usedSourceIx];
                                            if (di.IsSetup())
                                                if (di.logLine.Contains(ukSource))
                                                {
                                                    di.logLine = RemoveEscapeCharacters(di.logLine);
                                                    haltSourceSearchQ = true;
                                                    string newDiSourceLine = null;
                                                    string focus = ukSource;
                                                    string[] ukSourceSplit = ukSource.Split(aKey);

                                                    string sourceOrigin = $"{Ind24}[from {di.sectionName}]";
                                                    if (di.sectionName == secAdd && di.logLine.Contains(secAdt))
                                                        sourceOrigin = $"{Ind24}[from {secAdt} -> {di.sectionName}]";

                                                    /// source style 2 -- focuses legend key in source only
                                                    if (ukSourceSplit.HasElements(2))
                                                    {
                                                        focus = $"{{{aKey}}}";
                                                        string fullLogLine = "";
                                                        for (int c = 0; c < ukSourceSplit.Length; c++)
                                                        {
                                                            if (c == 0)
                                                                fullLogLine += $"{ukSourceSplit[c]}{focus}";
                                                            else
                                                            {
                                                                if (c + 1 == ukSourceSplit.Length)
                                                                    fullLogLine += ukSourceSplit[c];
                                                                else fullLogLine += $"{ukSourceSplit[c]}{aKey}";
                                                            }
                                                        }
                                                        newDiSourceLine = di.logLine.Replace(ukSource, fullLogLine);
                                                    }

                                                    /// source style 1 -- focuses full source
                                                    if (newDiSourceLine.IsNE())
                                                        newDiSourceLine = di.logLine.Replace(ukSource, $"{{{ukSource}}}");
                                                    newDiSourceLine = newDiSourceLine.Clamp(clampDistance + clampSuffix.Length, clampSuffix, focus, null) + sourceOrigin;
                                                    legCheckDi = new DecodeInfo(newDiSourceLine, legCheckDi.sectionName);
                                                }
                                        }
                                    }                                    

                                    /// issue reported here
                                    Dbug.LogPart("Unnoted Key Issue");
                                    legCheckDi.NoteIssue($"Key '{aKey}' was used but not described (Unnoted Legend Key)");
                                    decodingInfoDock.Add(legCheckDi);
                                    issueWasTriggered = true;
                                }
                                /// unused key issue
                                else if (!isUsed && isGenerated)
                                {
                                    Dbug.LogPart("Unused Key Issue");
                                    string diIssueMsg = $"Key '{aKey}' was described but not used (Unnecessary Legend Key)";

                                    bool fetchedAndEditedOGDi = false;
                                    if (decodeInfoIndicies.HasElements())
                                    {
                                        int accessIx = decodeInfoIndicies[getAccessIxFromListForOGdi];
                                        DecodeInfo ogDI = decodingInfoDock[accessIx];
                                        getAccessIxFromListForOGdi++;

                                        //decodingInfoDock[accessIx] = new DecodeInfo();
                                        if (!ogDI.NotedIssueQ)
                                        {
                                            legCheckDi = new DecodeInfo(ogDI.logLine, ogDI.sectionName);
                                            legCheckDi.NoteIssue(diIssueMsg);
                                            legCheckDi.NoteResult(ogDI.resultingInfo);

                                            decodingInfoDock[accessIx] = legCheckDi;
                                            fetchedAndEditedOGDi = true;
                                        }
                                    }

                                    if (!fetchedAndEditedOGDi)
                                        legCheckDi.NoteIssue(diIssueMsg);
                                    
                                    issueWasTriggered = true;
                                }
                                else if (isUsed && isGenerated)
                                    Dbug.LogPart("Key is okay");

                                if (issueWasTriggered)
                                    countSectionIssues[currentSectionNumber]++;

                                usedSourceIx++;
                                Dbug.Log("; ");
                            }

                            Dbug.NudgeIndent(false);
                            Dbug.NudgeIndent(false);

                            ranLegendChecksQ = true;
                        }


                        if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1)) && currentSectionName.IsNotNE() && !currentSectionName.Equals(lastSectionName))
                        {
                            Dbug.Log($"..  Sec#{currentSectionNumber + 1} '{currentSectionName}' ended with [{countSectionIssues[currentSectionNumber]}] issues;");
                            lastSectionName = currentSectionName;
                        }

                        if (!firstSearchingDbgRanQ && !withinOmitBlock)
                        {
                            Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                            Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... (no longer within a section)" : "")}", logDataLine)}");
                        }
                        withinASectionQ = false;
                    }


                    // end file reading if (forced) else (pre-emptive)
                    float capacityPercentage = (llx + 1) / (float)readingTimeOut * 100;
                    if ((llx + 1 >= logData.Length || llx + 1 >= readingTimeOut) && !endFileReading)
                    {
                        endFileReading = true;
                        string forcedEndReason = llx + 1 >= readingTimeOut ? $"reading timeout - max {readingTimeOut} lines" : "file ends";
                        Dbug.Log($" --> Decoding from file complete (forced: {forcedEndReason}) ... ending file reading <cap-{capacityPercentage:0}%>");

                        /// "File Too Long" issue
                        if (llx + 1 >= readingTimeOut)
                        {
                            DecodeInfo tooLongDi = new ("Decoder Issue; <no source line>", secVer);
                            tooLongDi.NoteIssue($"File reading has been forced to end: Version Log is too long (limit of {readingTimeOut} lines)");
                            decodingInfoDock.Add(tooLongDi);
                        }    
                    }
                    else if (endFileReading)
                        Dbug.Log($" --> Decoding from file complete ... ending file reading <cap-{capacityPercentage:0}%>");

                    TaskNum++;
                    ProgressBarUpdate(TaskNum / TaskCount, true, endFileReading);
                }
                Dbug.NudgeIndent(false);

                // -- compile decode library instance --
                if (endFileReading)
                {
                    Dbug.Log("- - - - - - - - - - -");

                    DecodedLibrary = new ResLibrary();
                    ResContents looseInfoRC;
                    Dbug.LogPart("Initialized Decode Library instance");

                    /// create loose ResCon
                    if (looseConAddits.HasElements() || looseConChanges.HasElements())
                    {
                        ContentBaseGroup looseCbg = new(logVersion, ResLibrary.LooseResConName, looseInfoRCDataIDs.ToArray());
                        looseInfoRC = new(newResConShelfNumber, looseCbg, looseConAddits.ToArray(), looseConChanges.ToArray());
                        resourceContents.Insert(0, looseInfoRC);
                        Dbug.LogPart($"; Generated and inserted 'loose' ResCon instance :: {looseInfoRC}");
                    }
                    Dbug.Log("; ");

                    if (testLibConAddUpdConnectionQ)
                    {
                        Dbug.Log("ENABLED :: [TESTING LIBRARY CONADDITS AND CONCHANGES CONNECTIONS]");
                        /**
                        FROM 'HCRLA - EX1 VERSION LOG'
                        ..  Additional
                            -- The following for testing purposes, at the moment...
                            > TwoItem Dust (d2,3) - Two Item (i2)
                            > (d0) - (i0)
                        ..  Update
                            -- The following for testing purposes, at the moment...
                            > (d0) - Tinier dust particles


                        FROM 'HCRLA - VERLOG EXTRA INFO'
                            ==== ADDED ITEM ====
                            # |Two Item -(i2; t1,2 d2,3)
                            # |Zero Item -(i0; d0)

                         **/
                        ResContents t1RC = new(0, new ContentBaseGroup(new VerNum(0, 0), "Zero Item", "d0", "i0"));
                        ResContents t2RC = new(0, new ContentBaseGroup(new VerNum(0, 0), "Two Item", "d2", "d3", "i2", "t1", "t2"));

                        DecodedLibrary.AddContent(t1RC, t2RC);
                    }

                    Dbug.Log($"Transferring decoded ResCons to Decode Library; {(resourceContents.HasElements() ? "" : "No ResCons to transfer to Decode Library; ")}");
                    if (resourceContents.HasElements())
                        DecodedLibrary.AddContent(true, resourceContents.ToArray());

                    Dbug.Log($"Transferring decoded Legends to Decode Library; {(legendDatas.HasElements() ? "" : "No Legends to transfer to Decode Library; ")}");
                    if (legendDatas.HasElements())
                        DecodedLibrary.AddLegend(legendDatas.ToArray());

                    Dbug.LogPart("Initializing summary data; ");
                    summaryData = new SummaryData(logVersion, ttaNumber, summaryDataParts.ToArray());
                    Dbug.Log($"Generated {nameof(SummaryData)} instance :: {summaryData.ToStringShortened()};");
                    Dbug.Log($"Transferring version summary to Decode Library; {(summaryData.IsSetup() ? "" : "No summary to transfer to Decode Library; ")}");
                    if (summaryData.IsSetup())
                        DecodedLibrary.AddSummary(summaryData);

                    Dbug.Log("Transferring decode dbug info group to Decode Info Dock;");
                    DecodeInfoDock = decodingInfoDock;

                    // -- relay complete decode --
                    hasFullyDecodedLogQ = endFileReading && DecodedLibrary.IsSetup();
                    _hasDecodedQ = hasFullyDecodedLogQ;
                }
            }

            _allowLogDecToolsDbugMessagesQ = false;
            Dbug.EndLogging();
            return hasFullyDecodedLogQ;            
        }
        public bool DecodeLogInfo(string[] logData)
        {
            return DecodeLogInfo(logData, VerNum.None, VerNum.None);
        }


        // LOG DECODING TOOL METHODS
        /// <summary>Also partly logs "CKS: code(key); "</summary>
        static string CharacterKeycodeSubstitution(string str)
        {
            string subbedStr = null;
            if (str.IsNotNEW())
            {
                subbedStr = str;
                List<string[]> codesNKeys = new()
                {
                    cks00.Split(' '),
                    cks01.Split(' '),
                    cks02.Split(' '),
                    cks11.Split(' '),
                    cks12.Split(' '),
                    cks99.Split(' '),
                };

                string dbgStr = "";
                foreach (string[] codeNKey in codesNKeys)
                {
                    if (codeNKey.HasElements(2))
                    {
                        string code = codeNKey[0], key = codeNKey[1];
                        if (code.IsNotNE())
                        {
                            string prevSubbedStr = subbedStr;
                            subbedStr = subbedStr.Replace(code, key);

                            if (prevSubbedStr.Length != subbedStr.Length)
                                dbgStr += $"{code}[{key}] ";
                        }
                    }
                }

                if (dbgStr.IsNotNE())
                    Dbug.LogPart($"CKS: {dbgStr.Trim()}; ");
                    // ..; CKS: &00[-] &11[(]; ...
            }
            return subbedStr;
        }
        static bool DetectUsageOfCharacterKeycode(string str, string cks)
        {
            bool detectedUsage = false;
            if (str.IsNotNE() && cks.IsNotNE())
            {
                if (cks.Contains(' '))
                {
                    string[] codeNKey = cks.Split(' ');
                    if (str.Contains(codeNKey[0]))
                        detectedUsage = true;
                }
            }
            return detectedUsage;
        }
        /// <summary>also partly logs "Removed square brackets; "</summary>
        static string RemoveSquareBrackets(string str)
        {
            if (str.IsNotNEW())
            {
                str = str.Replace("[", "").Replace("]", "");
                Dbug.LogPart("Removed square brackets; ");
            }
            return str;
        }
        /// <summary>also partly logs "Removed parentheses; "</summary>
        static string RemoveParentheses(string str)
        {
            if (str.IsNotNEW())
            {
                str = str.Replace("(", "").Replace(")", "");
                Dbug.LogPart("Removed parentheses; ");
            }
            return str;
        }
        /// <summary>also partly logs "Name recieved: {name} -- Edited name: {fixedName}; "</summary>
        public static string FixContentName(string conNam, bool dbgLogQ = true)
        {
            if (dbgLogQ)
                Dbug.LogPart($"Name recieved: ");
            string fixedConNam;
            if (conNam.IsNotNEW())
            {
                conNam = conNam.Trim();
                if (dbgLogQ)
                    Dbug.LogPart($"{conNam} -- ");

                fixedConNam = "";
                bool hadSpaceBefore = false;
                bool hadNonLetterBefore = false;
                bool isFirstCharacter = true;
                foreach (char c in conNam)
                {
                    /// if (upperCase && notSpaceChar)
                    ///     if (noSpaceBefore && isLetter && nonLetterBefore) {"C" --> " C"}
                    ///     else (spaceBefore || notLetter || letterBefore) {"C"}
                    /// else (lowerCase || spaceChar)
                    ///     if (notSpaceChar && spaceBefore) {"c" --> "C"}
                    ///     else (spaceChar || notSpaceBefore)
                    ///         if (isfirstCharInWord) {"c" --> "C"}
                    ///         else (notFirstCharInWord) {"c"}

                    if (c.ToString() == c.ToString().ToUpper() && c != ' ')
                    {
                        if (!hadSpaceBefore && Extensions.CharScore(c) >= Extensions.CharScore('a') && !hadNonLetterBefore)
                            fixedConNam += $" {c}";
                        else fixedConNam += c.ToString();
                    }
                    else
                    {
                        if (c != ' ' && hadSpaceBefore)
                            fixedConNam += c.ToString().ToUpper();
                        else
                        {
                            if (isFirstCharacter)
                                fixedConNam += c.ToString().ToUpper();
                            else fixedConNam += c.ToString();
                        }
                    }

                    /// hadSpaceBefore = isNotLetter (includes spaceChar, symbols, and numbers)
                    //hadSpaceBefore = Extensions.CharScore(c) < Extensions.CharScore('a');
                    hadSpaceBefore = c == ' ';
                    hadNonLetterBefore = Extensions.CharScore(c) < Extensions.CharScore('a') && c != ' ';
                    isFirstCharacter = false;
                }
                fixedConNam = fixedConNam.Trim();

                if (dbgLogQ)
                {
                    if (fixedConNam != conNam)
                        Dbug.LogPart($"Edited name: {fixedConNam}");
                    else Dbug.LogPart("No edits");
                }                    
            }
            else
            {
                fixedConNam = conNam;
                if (dbgLogQ)
                    Dbug.LogPart("<null>");
            }
            if (dbgLogQ)
                Dbug.LogPart("; ");
            return fixedConNam;
        }
        /// <summary>also partly logs "Removed Esc.Chars: [{n t a f r}]; "</summary>
        static string RemoveEscapeCharacters(string str)
        {
            if (str.IsNotNE())
            {
                string subLogPart = "";
                string newStr = str;
                for (int rec = 0; rec <= 5 && newStr.IsNotNE(); rec++)
                {
                    char escChar = '\0';
                    string prevNewStr = newStr;
                    //bool removedAnEscCharQ = false;
                    switch (rec)
                    {
                        case 0: 
                            newStr = newStr.Replace("\n", "");
                            escChar = 'n';
                            break;

                        case 1:
                            newStr = newStr.Replace("\t", "");
                            escChar = 't';
                            break;

                        case 2:
                            newStr = newStr.Replace("\a", "");
                            escChar = 'a';
                            break;

                        case 3:
                            newStr = newStr.Replace("\f", "");
                            escChar = 'f';
                            break;

                        case 4:
                            newStr = newStr.Replace("\r", "");
                            escChar = 'r';
                            break;

                        // trim case
                        case 5:
                            newStr = newStr.Trim();
                            break;
                    }

                    if (escChar.IsNotNull() && prevNewStr.Length != newStr.Length)
                        subLogPart += $"{escChar} ";
                }

                if (subLogPart.IsNotNE())
                    Dbug.LogPart($"Removed Esc.Chars [{subLogPart.Trim()}]; ");
                str = newStr;
            }
            return str;
        }
        public static string RemoveNumbers(string str)
        {
            if (str.IsNotNEW())
            {
                for (int i = 0; i < 10; i++) // 0~9 removed
                    str = str.Replace(i.ToString(), "");

                if (str.IsNEW())
                    str = "";
                else str = str.Trim();
            }
            return str;
        }
        /// <summary>also partly logs "Checked/Added fullstop; "</summary>
        static string FullstopCheck(string str)
        {
            if (str.IsNotNEW())
            {
                if (!str.EndsWith("."))
                {
                    str = $"{str}.";
                    Dbug.LogPart("Added fullstop; ");
                }
                else Dbug.LogPart("Checked fullstop; ");
                str = str.Trim();
            }
            return str;
        }
        public static bool IsNumberless(string str)
        {
            bool isNumless = false;
            if (str.IsNotNEW())
            {
                str = str.Trim();
                isNumless = str.ToLower() == RemoveNumbers(str).ToLower();
            }
            return isNumless;
        }
        /// <summary>Also partly logs "Noted legend key: {key} [+ID: {id}]; "</summary>
        void NoteLegendKey(string legKey, string sourceDataID)
        {
            if (usedLegendKeys != null && legKey.IsNotNEW())
                if (!usedLegendKeys.Contains(legKey))
                {
                    usedLegendKeys.Add(legKey);
                    Dbug.LogPart($"Noted legend key: {legKey}");


                    if (usedLegendKeysSources != null && sourceDataID.IsNotNEW())
                    {
                        /// IF legend key sources list is not parallel in length to used legend keys list
                        if (usedLegendKeysSources.Count < usedLegendKeys.Count)
                        {
                            usedLegendKeysSources.Add(sourceDataID);
                            Dbug.LogPart($" [+ID: {sourceDataID}]");
                        }
                        Dbug.LogPart("; ");
                    }
                }
        }
        /// <summary>Also partly logs "GDnS: {dataKey}[{number}]{suffix}; " only when decoding a version log (retired)</summary>
        public static void GetDataKeyAndSuffix(string dataIDGroup, out string dataKey, out string suffix)
        {
            string nonNumbers = RemoveNumbers(dataIDGroup);
            dataKey = null;
            suffix = null;
            if (dataIDGroup != null)
            {
                string trueNum = dataIDGroup;
                foreach (char c in nonNumbers)
                    trueNum = trueNum.Replace(c.ToString(), "");

                if (trueNum.IsNotNE())
                {
                    string[] splitDKnSfx = dataIDGroup.Split(trueNum);
                    if (splitDKnSfx.HasElements(2))
                    {
                        dataKey = splitDKnSfx[0];
                        suffix = splitDKnSfx[1];
                        if (_allowLogDecToolsDbugMessagesQ)
                            Dbug.LogPart($"GDnS: {dataKey}[{trueNum}]{suffix}; ");
                    }
                }
            }
        }
        /// <summary>Also partly logs "DD:{datakey}[{dataBody}]{suffix}; " only when decoding a version log</summary>
        /// <remarks>Successor method to the formerly used <seealso cref="GetDataKeyAndSuffix(string, out string, out string)"/></remarks>
        public static void DisassembleDataID(string dataIDGroup, out string dataKey, out string dataBody, out string suffix)
        {
            dataKey = null;
            suffix = null;
            dataBody = null;
            if (dataIDGroup.IsNotNEW())
            {
                // ?[DB]SF
                /// Cod_Tail
                /// Shoe_Spike`
                /// Watermelon^
                if (IsNumberless(dataIDGroup))
                {
                    suffix = GetSuffixChars(dataIDGroup);

                    /// in case suffix is the entire string, iow. just symbols
                    if (suffix.IsNotNE())
                    {
                        if (suffix != dataIDGroup)
                            dataBody = dataIDGroup.Substring(0, dataIDGroup.Length - suffix.Length);
                    }                        
                    else dataBody = dataIDGroup;
                }
                // DK[DB]SF
                /// q2`
                /// q67_3*
                /// xl4-alt0`
                else
                {
                    // get suffix first and remove from main data
                    suffix = GetSuffixChars(dataIDGroup);

                    if (suffix.IsNotNE())
                    {
                        if (suffix != dataIDGroup)
                            dataBody = dataIDGroup.Substring(0, dataIDGroup.Length - suffix.Length); // also 'dataIDGroup[..^suffix.Length]
                    }
                    else dataBody = dataIDGroup;

                    // get data key
                    /// in case suffix is the entire string, iow. just symbols
                    if (dataBody.IsNotNE())
                    {
                        string preDataKey = "";
                        foreach (char c in dataBody)
                        {
                            if (IsNumberless(c.ToString()))
                                preDataKey += c;
                            else break;
                        }
                        if (preDataKey.IsNotNE())
                            dataKey = preDataKey.Trim();

                        if (dataKey.IsNotNE())
                            dataBody = dataBody.Substring(dataKey.Length);
                    }
                }                

                if (_allowLogDecToolsDbugMessagesQ)
                    Dbug.LogPart($"DD:{dataKey}[{dataBody}]{suffix}; ");


                // local method
                static string GetSuffixChars(string mainDIdGroup)
                {
                    string suffix = null;
                    if (mainDIdGroup[^1].IsNotNull())
                        if (Extensions.CharScore(mainDIdGroup[^1]) < 0)
                        {
                            bool fetchSymbolChars = true;
                            string preSuffix = "";
                            /// FOR (..; IF fetching symbols AND char to get is not beyond (front end) length of data ID group; ..)
                            for (int sfx = 1; fetchSymbolChars && mainDIdGroup.Length >= sfx; sfx++)
                            {
                                char theLastChars = mainDIdGroup[^sfx];
                                if (Extensions.CharScore(theLastChars) < 0)
                                    preSuffix = theLastChars + preSuffix;
                                else fetchSymbolChars = false;
                            }
                            if (preSuffix.IsNotNE())
                                suffix = preSuffix.Trim();
                        }
                    #region old_code
                    //char lastChar = dataIDGroup[^1];
                    //if (lastChar.IsNotNull())
                    //    if (Extensions.CharScore(lastChar) < 0)
                    //        suffix = lastChar.ToString();
                    #endregion
                    return suffix;
                }
            }
        }
        /// <summary>Functions as it's counterpart, however it logs nothing</summary>
        public static void DisassembleDataIDQuiet(string dataIDGroup, out string dataKey, out string dataBody, out string suffix)
        {
            bool prevAllowDbugRelay = _allowLogDecToolsDbugMessagesQ;
            _allowLogDecToolsDbugMessagesQ = false;
            DisassembleDataID(dataIDGroup, out dataKey, out dataBody, out suffix);
            _allowLogDecToolsDbugMessagesQ = prevAllowDbugRelay;            
        }


        // DECODE INFO STUFF        
        public DecodeInfo GetDecodeInfo(DecodedSection sectionName, int infoIndex, out int sectionIssueCount)
        {
            DecodeInfo decInfo = new();
            sectionIssueCount = 0;
            if (DecodeInfoDock.HasElements())
            {
                // get all section infos
                List<DecodeInfo> sectionDecInfos = new();
                /// separate results from issues, then append issues to the end of results
                List<DecodeInfo> sdi_issues = new();
                foreach (DecodeInfo dcI in DecodeInfoDock)
                {
                    if (dcI.IsSetup())
                        if (dcI.sectionName == sectionName.ToString())
                        {                            
                            if (dcI.NotedIssueQ)
                            {
                                if (!dcI.NotedResultQ)
                                    sdi_issues.Add(dcI);
                                else sectionDecInfos.Add(dcI);
                                sectionIssueCount++;
                            }
                            else sectionDecInfos.Add(dcI);
                        }
                }
                if (sdi_issues.HasElements())
                    sectionDecInfos.AddRange(sdi_issues);
                

                // get specific decode info
                if (sectionDecInfos.HasElements())
                {
                    if (infoIndex.IsWithin(0, sectionDecInfos.Count - 1))
                        decInfo = sectionDecInfos[infoIndex];
                }
            }
            return decInfo;
        }        

    }        
}
