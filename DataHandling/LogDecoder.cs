﻿using ConsoleFormat;
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
        /// <summary>The index for all logs associated with this class' Dbg thread.</summary>
        static int ldx;
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
            Dbg.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder has saved recent directory path :: {RecentDirectory}");

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
                    Dbg.SingleLog("LogDecoder.DecodeFromSharedFile()", $"Log Decoder recieved [{logDecData[0]}], and has loaded recent directory path :: {RecentDirectory}");
                    fetchedLogDecData = true;
                }

            if (!fetchedLogDecData)
                Dbg.SingleLog("LogDecoder.DecodeFromSharedFile()", $"Log Decoder recieved no data for directory path (error: {Tools.GetRecentWarnError(false, false)})");

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
            Dbg.StartLogging("LogDecoder.DecodeLogInfo(str[])", out ldx);
            Dbg.Log(ldx, $"Recieved collection object (data from log?) to decode; collection has elements? {logData.HasElements()}");
            _allowLogDecToolsDbugMessagesQ = true;
            _versionAlreadyExistsQ = false;

            if (logData.HasElements())
            {
                Dbg.Log(ldx, $"Recieved {logData.Length} lines of log data; proceeding to decode information...");
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
                Dbg.NudgeIndent(ldx, true);
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
                            Dbg.LogPart(ldx, $"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                            //if (!(logDataLine.StartsWith(omit) || logDataLine.StartsWith(omitBlockOpen)))
                                Dbg.Log(ldx, $"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... ({nameof(withinASectionQ)} set to false)" : "")}", logDataLine)}");
                            firstSearchingDbgRanQ = true;
                        }
                        endFileReading = nextSectionNumber >= maxSectionsCount;                        
                    }

                    Dbg.NudgeIndent(ldx, true);
                    if (logDataLine.IsNotNEW() && !endFileReading)
                    {
                        logDataLine = logDataLine.Trim();
                        bool invalidLine = logDataLine.Contains(Sep);

                        /// imparsable group (block omit)
                        if (logDataLine.Equals(omitBlockOpen) && !withinOmitBlock)
                        {
                            Dbg.Log(ldx, $"Line contained '{omitBlockOpen}' :: starting omission block; ");
                            Dbg.LogPart(ldx, "Block Omitting Lines :: ");
                            withinOmitBlock = true;
                        }
                        else if (logDataLine.Equals(omitBlockClose) && withinOmitBlock)
                        {
                            withinOmitBlock = false;
                            Dbg.Log(ldx, "; ");
                            Dbg.LogPart(ldx, $"Line contained '{omitBlockClose}' :: ending omission block; ");
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
                                        Dbg.Log(ldx, $"Found section #{currentSectionNumber + 1} ({currentSectionName});");
                                    }
                                }
                            }

                            // parse section's data
                            if (withinASectionQ)
                            {
                                DecodeInfo decodeInfo = new($"{logDataLine}", currentSectionName);
                                Dbg.Log(ldx, $"{{{(currentSectionName.Length > 5 ? currentSectionName.Remove(5) : currentSectionName)}}}  L{lineNum,-2}| {logDataLine}");
                                Dbg.NudgeIndent(ldx, true);
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

                                    Dbg.LogPart(ldx, "Version Data  //  ");
                                    logDataLine = RemoveEscapeCharacters(logDataLine);
                                    if (IsSectionTag() && !parsedSectionTagQ)
                                    {
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(":") && logDataLine.CountOccuringCharacter(':') == 1)
                                        {
                                            Dbg.LogPart(ldx, $"Contains ':', raw data ({logDataLine}); ");
                                            string[] verSplit = logDataLine.Split(':');
                                            if (verSplit.HasElements(2))
                                            {
                                                Dbg.LogPart(ldx, $"Has sufficent elements after split; ");
                                                if (verSplit[1].IsNotNEW())
                                                {
                                                    Dbg.Log(ldx, $"Try parsing split @ix1 ({verSplit[1]}) into {nameof(VerNum)} instance; ");
                                                    bool parsed = VerNum.TryParse(verSplit[1], out VerNum verNum, out string parsingIssue);
                                                    if (parsed)
                                                    {
                                                        Dbg.LogPart(ldx, $"--> Obtained {nameof(VerNum)} instance [{verNum}]");
                                                        decodeInfo.NoteResult($"{nameof(VerNum)} instance --> {verNum}");
                                                        logVersion = verNum;
                                                        parsedSectionTagQ = true;
                                                        sectionIssueQ = false;

                                                        // log version suggestions / version clashes
                                                        if (latestLibVersion.HasValue() && earliestLibVersion.HasValue())
                                                        {
                                                            if (logVersion.AsNumber.IsWithin(earliestLibVersion.AsNumber, latestLibVersion.AsNumber))
                                                            {
                                                                Dbg.LogPart(ldx, $"; Version {logVersion.ToStringNums()} information already exists within library [OVERWRITE Warning]");
                                                                decodeInfo.NoteIssue(ldx, $"Version {logVersion.ToStringNums()} information already exists in library (May be overwritten).");
                                                                _versionAlreadyExistsQ = true;
                                                            }                                                            
                                                            
                                                            if (logVersion.AsNumber > latestLibVersion.AsNumber && latestLibVersion.AsNumber + 1 != logVersion.AsNumber)
                                                            {
                                                                bool nextMajor = latestLibVersion.MinorNumber + 1 >= 100;
                                                                VerNum suggestedVer;
                                                                if (nextMajor)
                                                                    suggestedVer = new VerNum(latestLibVersion.MajorNumber + 1, 0);
                                                                else suggestedVer = new VerNum(latestLibVersion.MajorNumber, latestLibVersion.MinorNumber + 1);

                                                                Dbg.LogPart(ldx, $"; Suggesting version log number: {suggestedVer}");
                                                                decodeInfo.NoteIssue(ldx, $"Suggesting version log number: {suggestedVer}");
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

                                                                    Dbg.LogPart(ldx, $"; Suggesting version log number: {suggestedVer}");
                                                                    decodeInfo.NoteIssue(ldx, $"Suggesting version log number: {suggestedVer}");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Dbg.LogPart(ldx, $"{nameof(VerNum)} instance could not be parsed: {parsingIssue}");
                                                        decodeInfo.NoteIssue(ldx, $"{nameof(VerNum)} instance could not be parsed: {parsingIssue}");
                                                    }
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "No data in split @ix1");
                                                    decodeInfo.NoteIssue(ldx, "No data provided for version number");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains(':'))
                                            {
                                                Dbg.LogPart(ldx, "This line has too many ':'");
                                                decodeInfo.NoteIssue(ldx, "This line has too many ':'");
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower().StartsWith(secVer.ToLower()))
                                                {
                                                    Dbg.LogPart(ldx, "This line is missing ':'");
                                                    decodeInfo.NoteIssue(ldx, "This line is missing ':'");
                                                }
                                                else
                                                {
                                                    Dbg.Log(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                    decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (parsedSectionTagQ)
                                        {
                                            Dbg.LogPart(ldx, "Version section tag has already been parsed");
                                            decodeInfo.NoteIssue(ldx, "Version section tag has already been parsed");
                                        }
                                        else if (logDataLine.StartsWith(secBracL))
                                        {
                                            Dbg.LogPart(ldx, "This line does not end with ']'");
                                            decodeInfo.NoteIssue(ldx, "This line does not end with ']'");
                                        }
                                        else
                                        {
                                            Dbg.LogPart(ldx, "This line does not start with '['");
                                            decodeInfo.NoteIssue(ldx, "This line does not start with '['");
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
                                        Dbg.LogPart(ldx, "Identified added section tag; ");
                                        wasSectionTag = true;
                                        logDataLine = RemoveEscapeCharacters(logDataLine);

                                        if (IsSectionTag() && !parsedSectionTagQ)
                                        {
                                            if (addedPholderNSubTags.HasElements())
                                            {
                                                Dbg.LogPart(ldx, "Clearing ph/sub list; ");
                                                addedPholderNSubTags.Clear();
                                            }

                                            logDataLine = RemoveSquareBrackets(logDataLine);
                                            if (logDataLine.Contains(':') && logDataLine.CountOccuringCharacter(':') <= 1)
                                            {
                                                /// ADDED  <-->  x,t21; y,p84 
                                                Dbg.Log(ldx, "Contains ':', spliting header tag from placeholders and substitutes; ");
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
                                                            Dbg.LogPart(ldx, "Only section name must be written before ':'");
                                                            decodeInfo.NoteIssue(ldx, "Only section name must be written before ':'");
                                                        }
                                                    }

                                                    if (splitAddHeader[1].IsNotNEW() && nothingInvalidBehindColonQ)
                                                    {
                                                        Dbg.LogPart(ldx, "Sorting placeholder/substitute groups :: ");
                                                        /// if()    x,t21  <-->  y,p84
                                                        /// else()  x,t21
                                                        List<string> addedPhSubs = new();
                                                        if (splitAddHeader[1].Contains(';'))
                                                        {
                                                            Dbg.LogPart(ldx, "Detected multiple >> ");
                                                            string[] pholdSubs = splitAddHeader[1].Split(';');
                                                            if (pholdSubs.HasElements())
                                                                foreach (string phs in pholdSubs)
                                                                {
                                                                    if (phs.IsNotNEW())
                                                                    {
                                                                        Dbg.LogPart(ldx, $"{phs} ");
                                                                        addedPhSubs.Add(phs);
                                                                    }
                                                                }
                                                        }
                                                        else if (splitAddHeader[1].Contains(','))
                                                        {
                                                            Dbg.LogPart(ldx, $"Only one >> {splitAddHeader[1]}");
                                                            addedPhSubs.Add(splitAddHeader[1]);
                                                        }
                                                        Dbg.Log(ldx, $"; ");
                                                        //Dbg.Log(ldx, $" << End ph/sub group sorting ");

                                                        if (addedPhSubs.HasElements())
                                                        {
                                                            Dbg.Log(ldx, $"Spliting '{addedPhSubs.Count}' placeholder and substitution groups");
                                                            for (int ps = 0; ps < addedPhSubs.Count; ps++)
                                                            {
                                                                string phs = $"{addedPhSubs[ps]}".Trim();
                                                                Dbg.LogPart(ldx, $" -> Group #{ps + 1} (has ','): {phs.Contains(",")}");

                                                                /// x  <-->  t21
                                                                /// y  <-->  p84
                                                                if (phs.Contains(","))
                                                                {
                                                                    string[] phSub = phs.Split(',');
                                                                    if (phSub.HasElements(2))
                                                                    {
                                                                        if (phSub.Length > 2)
                                                                            Dbg.LogPart(ldx, ";  this group has too many of ',' ... skipping this ph/sub group");
                                                                        else if (phSub[0].IsNotNEW() && phSub[1].IsNotNEW())
                                                                        {
                                                                            Dbg.LogPart(ldx, $" --> Obtained placeholder and substitute :: Sub '{phSub[0].Trim()}' for '{phSub[1].Trim()}'");
                                                                            // multiple tags within tags? screw it.. first come, first serve
                                                                            addedPholderNSubTags.Add(new string[] { phSub[0].Trim(), phSub[1].Trim() }); 
                                                                        }
                                                                    }
                                                                }
                                                                else Dbg.LogPart(ldx, $"; rejected group ({phs})");
                                                                Dbg.Log(ldx, ";  ");
                                                            }

                                                            if (addedPholderNSubTags.HasElements())
                                                            {
                                                                string phSubs = "Got placeholder/substitute groups (as ph/'sub') :: ";
                                                                foreach (string[] phsubgroup in addedPholderNSubTags)
                                                                    if (phsubgroup.HasElements(2))
                                                                        phSubs += $"{phsubgroup[0]}/'{phsubgroup[1]}'; ";
                                                                Dbg.LogPart(ldx, phSubs);
                                                                decodeInfo.NoteResult(phSubs.Trim());
                                                                parsedSectionTagQ = true;
                                                                sectionIssueQ = false;
                                                            }
                                                            else
                                                            {
                                                                Dbg.LogPart(ldx, "No placeholder/substitute groups were created and stored");
                                                                decodeInfo.NoteIssue(ldx, "No placeholder/substitute groups were created and stored");
                                                            }
                                                        }
                                                        else decodeInfo.NoteIssue(ldx, "Recieved no placeholder/substitute groups");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower() != secAdd.ToLower())
                                                {
                                                    if (logDataLine.Contains(':'))
                                                    {
                                                        Dbg.LogPart(ldx, "This line contains too many ':'");
                                                        decodeInfo.NoteIssue(ldx, "This line contains too many ':'");
                                                    }
                                                    else
                                                    {
                                                        //if (logDataLine.ToLower().Replace(secAdd.ToLower(), "").IsNEW())
                                                        if (logDataLine.ToLower().StartsWith(secAdd.ToLower()))
                                                        {
                                                            Dbg.LogPart(ldx, "This line is missing ':'");
                                                            decodeInfo.NoteIssue(ldx, "This line is missing ':'");
                                                        }
                                                        else
                                                        {
                                                            Dbg.Log(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                            decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                        }
                                                    }                                                    
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "Added section tag identified");
                                                    decodeInfo.NoteResult("Added section tag identified");
                                                    parsedSectionTagQ = true;
                                                }
                                            }
                                        }   
                                        else
                                        {
                                            if (parsedSectionTagQ)
                                            {
                                                Dbg.LogPart(ldx, "Added section tag has already been parsed");
                                                decodeInfo.NoteIssue(ldx, "Added section tag has already been parsed");
                                            }
                                            else if (logDataLine.StartsWith(secBracL))
                                            {
                                                Dbg.LogPart(ldx, "This line does not end with ']'");
                                                decodeInfo.NoteIssue(ldx, "This line does not end with ']'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line does not start with '['");
                                                decodeInfo.NoteIssue(ldx, "This line does not start with '['");
                                            }
                                        }
                                    }

                                    /// added items (become content base groups)
                                    else if (parsedSectionTagQ)
                                    {
                                        // setup
                                        string addedContentName = null;
                                        List<string> addedDataIDs = new();

                                        Dbg.LogPart(ldx, "Identified added data; ");
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        if (logDataLine.Contains('|') && logDataLine.CountOccuringCharacter('|') == 1)
                                        {
                                            /// 1  <-->  ItemName       -(x25; y32,33 q14)
                                            /// 2  <-->  Other o>       -(x21; a> q21) 
                                            Dbg.LogPart(ldx, "Contains '|'; ");
                                            string[] splitAddedLine = logDataLine.Split('|');

                                            Dbg.Log(ldx, $"item line (number '{splitAddedLine[0]}') has info? {splitAddedLine[1].IsNotNEW()}");
                                            if (splitAddedLine[1].IsNotNEW() && !IsNumberless(splitAddedLine[0]))
                                            {
                                                // replace placehoder keys with substitution phrases
                                                /// Other Items     -(x21; y42 q21)
                                                if (addedPholderNSubTags.HasElements())
                                                {
                                                    Dbg.LogPart(ldx, "Replacing placeholders with their substitutes :: ");
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
                                                                Dbg.LogPart(ldx, $"[{phSub[0]}] '{phSub[1]}';  ");
                                                                spAddLnIx1 = spAddLnIx1.Replace(phSub[0], phSub[1]);
                                                            }
                                                    }
                                                    if (spAddLnIx1 != splitAddedLine[1])
                                                    {
                                                        splitAddedLine[1] = spAddLnIx1;
                                                        Dbg.Log(ldx, " --> replacements complete; ");
                                                    }
                                                    else Dbg.Log(ldx, " --> no changes; ");
                                                }


                                                // parse content name and data ids
                                                if (splitAddedLine[1].Contains('-') && splitAddedLine[1].CountOccuringCharacter('-') == 1)
                                                {
                                                    Dbg.LogPart(ldx, "Contains '-'; ");
                                                    /// ItemName  <-->  (x25; y32,33 q14)
                                                    /// Other Items  <-->  (x21; y42 q21)
                                                    string[] addedContsNDatas = splitAddedLine[1].Split('-');
                                                    if (addedContsNDatas[1].IsNotNEW() && addedContsNDatas[0].IsNotNEW())
                                                    {
                                                        bool okayContentDataIDs = true;
                                                        addedContsNDatas[1] = addedContsNDatas[1].Trim();
                                                        if (!addedContsNDatas[1].StartsWith('(') || !addedContsNDatas[1].EndsWith(')'))
                                                        {
                                                            Dbg.LogPart(ldx, "Content data IDs must be enclosed in parentheses; ");
                                                            decodeInfo.NoteIssue(ldx, "Content data IDs must be enclosed in parentheses");
                                                            okayContentDataIDs = false;
                                                        }

                                                        if (okayContentDataIDs)
                                                        {
                                                            addedContsNDatas[1] = RemoveParentheses(addedContsNDatas[1]);
                                                            addedContentName = CharacterKeycodeSubstitution(FixContentName(addedContsNDatas[0]));
                                                            bool okayContentName = true;
                                                            if (addedContentsNames.HasElements())
                                                                okayContentName = !addedContentsNames.Contains(addedContentName);

                                                            Dbg.Log(ldx, "Fetching content data IDs; ");
                                                            if (addedContsNDatas[1].CountOccuringCharacter(';') <= 1 && okayContentName) /// Zest -(x22)
                                                            {
                                                                bool missingMainIDq = false;
                                                                if (addedContsNDatas[1].CountOccuringCharacter(';') == 0)
                                                                    missingMainIDq = addedContsNDatas[1].CountOccuringCharacter(',') > 0;

                                                                if (!missingMainIDq)
                                                                {
                                                                    Dbg.NudgeIndent(ldx, true);
                                                                    /// (x25 y32,33 q14)
                                                                    /// (x21 y42 q21)
                                                                    addedContsNDatas[1] = addedContsNDatas[1].Replace(";", "");
                                                                    bool cksViolationQ = false;
                                                                    /// IF detect CKS-comma: warn ... IF detect CKS-(any bracket or colon): warn and major issue
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks01))
                                                                    {
                                                                        Dbg.LogPart(ldx, $"Usage of CKS '{cks01}' in data ID groups advised against; ");
                                                                        decodeInfo.NoteIssue(ldx, $"Usage of character keycode '{cks01}' in data ID groups is advised against");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks11))
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbg.LogPart(ldx, $"Usage of CKS '{cks11}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue(ldx, $"Usage of character keycode '{cks11}' in data ID groups is not allowed");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks12) && !cksViolationQ)
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbg.LogPart(ldx, $"Usage of CKS '{cks12}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue(ldx, $"Usage of character keycode '{cks12}' in data ID groups is not allowed");
                                                                    }
                                                                    if (DetectUsageOfCharacterKeycode(addedContsNDatas[1], cks02) && !cksViolationQ)
                                                                    {
                                                                        cksViolationQ = true;
                                                                        Dbg.LogPart(ldx, $"Usage of CKS '{cks02}' in data ID groups is not allowed; ");
                                                                        decodeInfo.NoteIssue(ldx, $"Usage of character keycode '{cks02}' in data ID groups is not allowed");
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
                                                                                Dbg.LogPart(ldx, $"Numberless ID group '{dataIDGroup}' is invalid");
                                                                                decodeInfo.NoteIssue(ldx, $"Numberless ID group '{dataIDGroup}' is invalid");
                                                                                Dbg.Log(ldx, "; ");
                                                                            }
                                                                            /// 100, 23
                                                                            else if (RemoveNumbers(dataIDGroup.Replace(",", "").Replace(legRangeKey.ToString(), "")).IsNEW())
                                                                            {
                                                                                Dbg.LogPart(ldx, $"Data ID group '{dataIDGroup}' is not a data ID (just numbers)");
                                                                                decodeInfo.NoteIssue(ldx, $"Data ID group '{dataIDGroup}' is just numbers (invalid)");
                                                                                Dbg.Log(ldx, "; ");
                                                                            }
                                                                            
                                                                            // for valid id groups
                                                                            /// y32,33
                                                                            else if (dataIDGroup.Contains(','))
                                                                            {
                                                                                Dbg.LogPart(ldx, $"Got complex ID group '{dataIDGroup}'");
                                                                                string[] dataIDs = dataIDGroup.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                                                                if (dataIDs.HasElements(2))
                                                                                {
                                                                                    Dbg.LogPart(ldx, "; ");
                                                                                    DisassembleDataID(dataIDs[0], out string dataKey, out _, out string suffix);
                                                                                    NoteLegendKey(dataKey, dataIDs[0]);
                                                                                    NoteLegendKey(suffix, dataIDs[0]);
                                                                                    if (dataKey.IsNotNEW())
                                                                                    {
                                                                                        Dbg.Log(ldx, $"Retrieved data key '{dataKey}'; ");
                                                                                        foreach (string datId in dataIDs)
                                                                                        {
                                                                                            Dbg.LogPart(ldx, ". Adding ID; ");
                                                                                            DisassembleDataID(datId, out _, out string dataBody, out string sfx);
                                                                                            NoteLegendKey(sfx, datId);
                                                                                            string datToAdd = dataKey + dataBody + sfx;
                                                                                            if (!IsNumberless(datToAdd))
                                                                                            {
                                                                                                addedDataIDs.Add(datToAdd);
                                                                                                Dbg.LogPart(ldx, $"Got and added '{datToAdd}'");
                                                                                            }
                                                                                            Dbg.Log(ldx, "; ");
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    Dbg.LogPart(ldx, "; This complex ID group does not have at least 2 IDs");
                                                                                    decodeInfo.NoteIssue(ldx, "This complex ID group does not have at least 2 IDs");
                                                                                    Dbg.Log(ldx, "; ");
                                                                                }
                                                                            }
                                                                            /// r20~22
                                                                            /// q21`~24`
                                                                            else if (dataIDGroup.Contains(legRangeKey))
                                                                            {
                                                                                Dbg.LogPart(ldx, $"Got range ID group '{dataIDGroup}'; ");
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
                                                                                        Dbg.LogPart(ldx, $"Retrieved data key '{dataKey}' and suffix [{(dkSuffix.IsNEW() ? "<none>" : dkSuffix)}]; ");

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
                                                                                                Dbg.LogPart(ldx, $"Parsed range numbers: {lowBound} up to {highBound}; Adding IDs :: ");

                                                                                                for (int rnix = lowBound; rnix <= highBound; rnix++)
                                                                                                {
                                                                                                    string dataID = $"{dataKey}{rnix}{dkSuffix}".Trim();
                                                                                                    addedDataIDs.Add(dataID);
                                                                                                    Dbg.LogPart(ldx, $"{dataID} - ");
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                Dbg.LogPart(ldx, "Both range values cannot be the same");
                                                                                                decodeInfo.NoteIssue(ldx, "Both range values cannot be the same");
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (parseRngA)
                                                                                            {
                                                                                                Dbg.LogPart(ldx, $"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                decodeInfo.NoteIssue(ldx, $"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                Dbg.LogPart(ldx, $"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                decodeInfo.NoteIssue(ldx, $"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (dataIdRng.HasElements())
                                                                                    {
                                                                                        Dbg.LogPart(ldx, $"This range group has too many '{legRangeKey}'");
                                                                                        decodeInfo.NoteIssue(ldx, $"This range group has too many '{legRangeKey}'");
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        Dbg.LogPart(ldx, $"This range is missing values or missing '{legRangeKey}'");
                                                                                        decodeInfo.NoteIssue(ldx, $"This range is missing values or missing '{legRangeKey}'");
                                                                                    }
                                                                                }
                                                                                Dbg.Log(ldx, "; ");
                                                                            }
                                                                            /// x25 q14 ...
                                                                            else
                                                                            {
                                                                                //GetDataKeyAndSuffix(dataIDGroup, out string dataKey, out string suffix);
                                                                                DisassembleDataID(dataIDGroup, out string dataKey, out _, out string suffix);
                                                                                NoteLegendKey(dataKey, dataIDGroup);
                                                                                NoteLegendKey(suffix, dataIDGroup);

                                                                                addedDataIDs.Add(dataIDGroup.Trim());
                                                                                Dbg.Log(ldx, $"Got and added ID '{dataIDGroup.Trim()}'; ");
                                                                            }
                                                                        }
                                                                    Dbg.NudgeIndent(ldx, false);

                                                                    addedDataIDs = addedDataIDs.ToArray().SortWords();
                                                                    Dbg.LogPart(ldx, "Sorted data IDs; ");
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, "Missing data for added content's 'main ID'");
                                                                    decodeInfo.NoteIssue(ldx, "Missing data for added content's 'main ID'");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (okayContentName)
                                                                {
                                                                    Dbg.LogPart(ldx, "Data IDs line contains too many ';'");
                                                                    decodeInfo.NoteIssue(ldx, "Data IDs line contains too many ';'");
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, "Content with this name has already been added");
                                                                    decodeInfo.NoteIssue(ldx, "Content with this name has already been added");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (addedContsNDatas[0].IsNEW())
                                                        {
                                                            Dbg.LogPart(ldx, "Missing data for 'content name'");
                                                            decodeInfo.NoteIssue(ldx, "Missing data for 'content name'");
                                                        }
                                                        else
                                                        {
                                                            Dbg.LogPart(ldx, "Missing data for 'content data IDs'");
                                                            decodeInfo.NoteIssue(ldx, "Missing data for 'content data IDs'");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (splitAddedLine[1].Contains('-'))
                                                    {
                                                        Dbg.LogPart(ldx, "This line contains too many '-'");
                                                        decodeInfo.NoteIssue(ldx, "This line contains too many '-'");
                                                    }
                                                    else
                                                    {
                                                        Dbg.LogPart(ldx, "This line is missing '-'");
                                                        decodeInfo.NoteIssue(ldx, "This line is missing '-'");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!IsNumberless(splitAddedLine[0]))
                                                    decodeInfo.NoteIssue(ldx, "Missing data for 'added content'");
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "Item line number was not a number");
                                                    decodeInfo.NoteIssue(ldx, "Item line number was not a number");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains('|'))
                                            {
                                                Dbg.LogPart(ldx, "This line contains too many '|'");
                                                decodeInfo.NoteIssue(ldx, "This line contains too many '|'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line is missing '|'");
                                                decodeInfo.NoteIssue(ldx, "This line is missing '|'");
                                            }
                                        }

                                        // complete (Dbug revision here)
                                        if (addedDataIDs.HasElements() && addedContentName.IsNotNEW())
                                        {
                                            addedContentsNames.Add(addedContentName);
                                            Dbg.LogPart(ldx, $" --> Obtained content name ({addedContentName}) and data Ids (");
                                            foreach (string datID in addedDataIDs)
                                                Dbg.LogPart(ldx, $"{datID} ");
                                            Dbg.LogPart(ldx, "); ");

                                            // generate a content base group (and repeat info using overriden CBG.ToString())
                                            ContentBaseGroup newContent = new(logVersion, addedContentName, addedDataIDs.ToArray());                                            
                                            resourceContents.Add(new ResContents(newResConShelfNumber, newContent));
                                            //addedContents.Add(newContent);
                                            Dbg.LogPart(ldx, $"Generated {nameof(ContentBaseGroup)} :: {newContent};");
                                            decodeInfo.NoteResult($"Generated {nameof(ContentBaseGroup)} :: {newContent}");

                                            sectionIssueQ = false;
                                        }
                                    }
                                    
                                    /// no data parsing before tag issue
                                    else
                                    {
                                        Dbg.LogPart(ldx, "Added section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue(ldx, "Added section tag must be parsed before decoding its data");
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
                                                Dbg.Log(ldx, "Additional section tag has already been parsed");
                                                decodeInfo.NoteIssue(ldx, "Additional section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbg.Log(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// additional contents
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Identified additional data; ");
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
                                                Dbg.LogPart(ldx, "Contains '-'; ");
                                                string[] splitAdditLine = logDataLine.Split('-');
                                                if (splitAdditLine.HasElements(2))
                                                    if (splitAdditLine[0].IsNotNEW() && splitAdditLine[1].IsNotNEW())
                                                    {
                                                        splitAdditLine[0] = splitAdditLine[0].Trim();
                                                        splitAdditLine[1] = splitAdditLine[1].Trim();
                                                        Dbg.Log(ldx, $"Got contents ({splitAdditLine[0]}) and related content ({splitAdditLine[1]});");

                                                        bool continueToNextAdtSubSectionQ = false;
                                                        List<string> adtConDataIDs = new();
                                                        string adtContentName = "", adtRelatedName = "", adtRelatedDataID = "";


                                                        /// SardineTins  <-->  (u41,42)
                                                        /// (q41~44)
                                                        /// (Cod_Tail)
                                                        /// Mackrel Chunks  <-->  (r230,231 y192)
                                                        // + parsed additional content +
                                                        splitAdditLine[0] = splitAdditLine[0].Replace("(", " (").Replace(")", ") ");
                                                        Dbg.LogPart(ldx, "Spaced-out parentheses in contents; ");
                                                        string[] additContsData = splitAdditLine[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                        if (additContsData.HasElements())
                                                        {
                                                            Dbg.LogPart(ldx, "Content Parts: ");
                                                            string adtDataIDs = "";
                                                            adtContentName = "";
                                                            for (int ax = 0; ax < additContsData.Length; ax++)
                                                            {
                                                                string adtConPart = additContsData[ax];
                                                                if (adtConPart.Contains('(') && adtConPart.Contains(')') && adtDataIDs.IsNE())
                                                                    adtDataIDs = adtConPart;
                                                                else adtContentName += $"{adtConPart} ";
                                                                Dbg.LogPart(ldx, $"{adtConPart}{(ax + 1 >= additContsData.Length ? "" : "|")}");
                                                            }

                                                            bool okayAdtContentName = true;
                                                            if (adtContentName.Contains('(') || adtContentName.Contains(')'))
                                                            {
                                                                Dbg.LogPart(ldx, "Content name cannot (directly) contain parentheses");
                                                                decodeInfo.NoteIssue(ldx, "Content name cannot (directly) contain parentheses");
                                                                okayAdtContentName = false;
                                                            }                                                            

                                                            /// u41  <-->  u42
                                                            /// q41  <-->  q42  <-->  q43  <-->  q44
                                                            /// CodTail
                                                            /// r230  <-->  r231  <-->  y192
                                                            if (adtDataIDs.IsNotNEW() && okayAdtContentName)
                                                            {
                                                                Dbg.Log(ldx, "; ");
                                                                adtDataIDs = RemoveParentheses(adtDataIDs.Trim().Replace(' ', ','));
                                                                adtContentName = CharacterKeycodeSubstitution(FixContentName(adtContentName));
                                                                Dbg.LogPart(ldx, $"Parsed addit. content name ({(adtContentName.IsNEW() ? "<no name>" : adtContentName)}) and data ID group ({adtDataIDs})");

                                                                /// u24,23`,29~31,CodTail
                                                                if (adtDataIDs.Replace(",","").IsNotNEW())
                                                                {
                                                                    Dbg.Log(ldx, $";{(adtDataIDs.Contains(',') ? " Data ID group contains ',';" : "")} Fetching data IDs; ");
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
                                                                        Dbg.LogPart(ldx, $"Retrieved data key '{dataKey}'{(dkSuffix.IsNEW() ? "" : $" and suffix [{dkSuffix}]")}; ");
                                                                        
                                                                        /// decode issue handling
                                                                        bool isOnlyNumber = false;
                                                                        if (RemoveNumbers(adtDataIDs.Replace(",", "").Replace(legRangeKey.ToString(), "")).IsNEW())
                                                                        {
                                                                            isOnlyNumber = true;
                                                                            Dbg.LogPart(ldx, $"Data ID group is not a data ID (just numbers); ");
                                                                            decodeInfo.NoteIssue(ldx, $"Data ID group is just numbers (invalid)");
                                                                        }

                                                                        // go through the data ID groups for this addit.
                                                                        Dbg.NudgeIndent(ldx, true);
                                                                        if (!isOnlyNumber && (!noDataKey || dataBody.IsNotNE()))
                                                                            foreach (string rawDatID in datIDs)
                                                                            {
                                                                                bool invalidDataKey = false, cksDataIDViolation = DetectUsageOfCharacterKeycode(rawDatID, cks01);
                                                                                string datId = CharacterKeycodeSubstitution(rawDatID);
                                                                                if (RemoveNumbers(datId).IsNotNEW())
                                                                                {
                                                                                    invalidDataKey = IsNumberless(datId);
                                                                                    if (invalidDataKey)
                                                                                        Dbg.LogPart(ldx, $"Treating Data ID as word (is numberless); ");
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
                                                                                            Dbg.LogPart(ldx, $"Got range ID group '{datId}'; ");
                                                                                            NoteLegendKey(legRangeKey.ToString(), datId);
                                                                                            string[] dataIdRng;

                                                                                            if (dataKey.IsNotNE())
                                                                                            {
                                                                                                dataIdRng = datId.Split(legRangeKey);
                                                                                                if (datId.Contains(dataKey))
                                                                                                {
                                                                                                    Dbg.LogPart(ldx, $"Removed data key '{dataKey}' before split; ");
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
                                                                                                    Dbg.LogPart(ldx, $"Retrieved suffix [{(dkSuffix.IsNEW() ? "<none>" : dkSuffix)}]; ");
                                                                                                    dataIdRng[0] = dataIdRng[0].Replace(dkSuffix, "");
                                                                                                    dataIdRng[1] = dataIdRng[1].Replace(dkSuffix, "");
                                                                                                }
                                                                                                bool parseRngA = int.TryParse(dataIdRng[0], out int rngA);
                                                                                                bool parseRngB = int.TryParse(dataIdRng[1], out int rngB);
                                                                                                if (parseRngA && parseRngB)
                                                                                                {
                                                                                                    //Dbg.Log(ldx, "..");
                                                                                                    if (rngA != rngB)
                                                                                                    {
                                                                                                        int lowBound = Math.Min(rngA, rngB), highBound = Math.Max(rngA, rngB);
                                                                                                        Dbg.LogPart(ldx, $"Parsed range numbers ({lowBound} up to {highBound}); ");

                                                                                                        Dbg.LogPart(ldx, "Adding IDs :: ");
                                                                                                        for (int rnix = lowBound; rnix <= highBound; rnix++)
                                                                                                        {
                                                                                                            string dataID = $"{dataKey}{rnix}{dkSuffix}".Trim();
                                                                                                            adtConDataIDs.Add(dataID);
                                                                                                            Dbg.LogPart(ldx, $"{dataID} - ");
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        Dbg.LogPart(ldx, "Both range values cannot be the same");
                                                                                                        decodeInfo.NoteIssue(ldx, "Both range values cannot be the same");
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (parseRngA)
                                                                                                    {
                                                                                                        Dbg.LogPart(ldx, $"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                        decodeInfo.NoteIssue(ldx, $"Right range value '{dataIdRng[1]}' was an invalid number");
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        Dbg.LogPart(ldx, $"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                        decodeInfo.NoteIssue(ldx, $"Left range value '{dataIdRng[0]}' was an invalid number");
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (dataIdRng.HasElements())
                                                                                                {
                                                                                                    Dbg.LogPart(ldx, $"This range group has too many '{legRangeKey}'");
                                                                                                    decodeInfo.NoteIssue(ldx, $"This range group has too many '{legRangeKey}'");
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    Dbg.LogPart(ldx, $"This range is missing values or missing '{legRangeKey}'");
                                                                                                    decodeInfo.NoteIssue(ldx, $"This range is missing values or missing '{legRangeKey}'");
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
                                                                                                Dbg.LogPart(ldx, $"Got and added '{dataID}'");
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
                                                                                        Dbg.LogPart(ldx, $"Got and added '{datId}'");
                                                                                    }
                                                                                }        
                                                                                else
                                                                                {
                                                                                    Dbg.LogPart(ldx, $"; Data ID group may not contain comma CKS ({cks01})");
                                                                                    decodeInfo.NoteIssue(ldx, $"Data ID group may not contain following character keycode: {cks01}");
                                                                                }

                                                                                Dbg.Log(ldx, ";");
                                                                            }
                                                                        Dbg.NudgeIndent(ldx, false);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, "; Data ID group had no values for data IDs");
                                                                    decodeInfo.NoteIssue(ldx, "Data ID group had no values for data IDs");
                                                                }

                                                                if (adtConDataIDs.HasElements())
                                                                {
                                                                    adtConDataIDs = adtConDataIDs.ToArray().SortWords();
                                                                    if (adtDataIDs.Contains(","))
                                                                        Dbg.LogPart(ldx, "--> Sorted Data IDs; ");
                                                                    else Dbg.Log(ldx, "; Sorted Data IDs; ");
                                                                }
                                                                


                                                                // complete "content" sub-section
                                                                if (adtConDataIDs.HasElements())
                                                                {
                                                                    Dbg.LogPart(ldx, $" --> Complete additional contents; Name ({(adtContentName.IsNEW() ? "<no name>" : adtContentName)}) and Data IDs (");
                                                                    foreach (string dId in adtConDataIDs)
                                                                        Dbg.LogPart(ldx, $"{dId} ");
                                                                    Dbg.Log(ldx, "); ");
                                                                    continueToNextAdtSubSectionQ = true;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Dbg.LogPart(ldx, "; Missing content's data ID group");
                                                                decodeInfo.NoteIssue(ldx, "Missing content's data ID group");
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
                                                                Dbg.LogPart(ldx, "Spaced-out parentheses in related contents; ");
                                                                string[] additContRelData = splitAdditLine[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                                if (additContRelData.HasElements())
                                                                {
                                                                    Dbg.LogPart(ldx, "Related content parts: ");
                                                                    for (int ax = 0; ax < additContRelData.Length; ax++)
                                                                    {
                                                                        string adtRelPart = additContRelData[ax];
                                                                        if (adtRelPart.Contains('(') && adtRelPart.Contains(')'))
                                                                            adtRelatedDataID = adtRelPart;
                                                                        else adtRelatedName += adtRelPart + " ";
                                                                        Dbg.LogPart(ldx, $"{adtRelPart}{(ax + 1 >= additContRelData.Length ? "" : "|")}");
                                                                    }
                                                                    Dbg.LogPart(ldx, "; ");

                                                                    bool okayRelDataID = false, okayRelName = false;
                                                                    // for name
                                                                    if (adtRelatedName.IsNotNEW())
                                                                    {
                                                                        if (adtRelatedName.Contains('(') || adtRelatedName.Contains(')'))
                                                                        {
                                                                            Dbg.LogPart(ldx, "Related content name cannot contain parentheses");
                                                                            decodeInfo.NoteIssue(ldx, "Related content name cannot contain parentheses");
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
                                                                                Dbg.LogPart(ldx, "Related Data ID was not a Data ID (just numbers)");
                                                                                decodeInfo.NoteIssue(ldx, "Related Data ID was just numbers (invalid)");
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (DetectUsageOfCharacterKeycode(adtRelatedDataID, cks01))
                                                                            {
                                                                                Dbg.LogPart(ldx, $"; Related data id may not contain comma CKS ({cks01})");
                                                                                decodeInfo.NoteIssue(ldx, $"Related Data ID may not contain following character keycode: {cks01}");
                                                                            }
                                                                            else if (!adtRelatedDataID.Contains(legRangeKey))
                                                                            {
                                                                                Dbg.LogPart(ldx, "There can only be one related Data ID (contains ',')");
                                                                                decodeInfo.NoteIssue(ldx, "There can only be one related Data ID (contains ',')");
                                                                            }
                                                                            else
                                                                            {
                                                                                Dbg.LogPart(ldx, $"There can only be one related Data ID (contains range key '{legRangeKey}')");
                                                                                decodeInfo.NoteIssue(ldx, $"There can only be one related Data ID (contains range key '{legRangeKey}')");
                                                                            }
                                                                            adtRelatedDataID = null;
                                                                        }
                                                                    }
                                                                    Dbg.Log(ldx, "..");

                                                                    //adtRelatedName = adtRelatedName.IsNEW() ? "<none>" : adtRelatedName;
                                                                    //adtRelatedDataID = adtRelatedDataID.IsNEW() ? "<none>" : adtRelatedDataID;

                                                                    // if (has relDataID, find related ConBase) else (only relName, create new ConBase)
                                                                    if (okayRelName || okayRelDataID)
                                                                    {
                                                                        Dbg.Log(ldx, $" --> Complete related contents;  Related Name ({(adtRelatedName.IsNEW() ? "<none>" : adtRelatedName)}) and Related Data ID ({(adtRelatedDataID.IsNEW() ? "<none>" : adtRelatedDataID)}); ");
                                                                        continueToNextAdtSubSectionQ = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        Dbg.Log(ldx, "At least one of the two - Related Name or Related Data ID - must have a value; ");
                                                                        decodeInfo.NoteIssue(ldx, "At least one of the two - Related Name or Related Data ID - must have a value; ");
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (splitAdditLine[1].CountOccuringCharacter('(') <= 1)
                                                                {
                                                                    Dbg.LogPart(ldx, "This related content contains too many ')'");
                                                                    decodeInfo.NoteIssue(ldx, "This related content contains too many ')'");
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, "This related content contains too many '('");
                                                                    decodeInfo.NoteIssue(ldx, "This related content contains too many '('");
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
                                                                Dbg.LogPart(ldx, "Using related name as content name; ");
                                                                adtContentName = adtRelatedName;
                                                            }

                                                            // generate additional content instance
                                                            ContentAdditionals newAdditCon = new ContentAdditionals(logVersion, adtRelatedDataID, adtContentName, adtConDataIDs.ToArray());
                                                            Dbg.LogPart(ldx, $"Generated ContentAdditionals :: {newAdditCon}; Searching for matching ConBase; ");

                                                            ResContents matchingResCon = null;
                                                            if (resourceContents.HasElements() && newAdditCon.RelatedDataID != null)
                                                            {
                                                                // matching with contents in current log decode
                                                                Dbg.LogPart(ldx, " in 'Decoded Library' -- ");
                                                                foreach (ResContents resCon in resourceContents)
                                                                    if (resCon.ContainsDataID(newAdditCon.RelatedDataID, out RCFetchSource source))
                                                                    {
                                                                        if (source.Equals(RCFetchSource.ConBaseGroup))
                                                                        {
                                                                            /// id only: save match, keep searching
                                                                            /// id and name: save match, end searching
                                                                            bool matchedNameQ = false;

                                                                            Dbg.LogPart(ldx, $"; Got match ('{newAdditCon.RelatedDataID}'");
                                                                            if (resCon.ContentName == adtRelatedName)
                                                                            {
                                                                                Dbg.LogPart(ldx, $", plus matched name '{resCon.ContentName}'");
                                                                                matchedNameQ = true;
                                                                            }
                                                                            Dbg.LogPart(ldx, ")");
                                                                            matchingResCon = resCon;

                                                                            if (matchedNameQ)
                                                                            {
                                                                                Dbg.LogPart(ldx, "; Search end");
                                                                                break;
                                                                            }
                                                                            else Dbg.LogPart(ldx, "; Search continue");
                                                                        }
                                                                    }
                                                            }
                                                            Dbg.Log(ldx, "; ");

                                                            // if (has relDataID, find related ConBase) else if (only relName, create new ConBase)
                                                            bool completedFinalStepQ = false;
                                                            if (newAdditCon.RelatedDataID.IsNotNE())
                                                            {
                                                                /// connect with ConBase from decoded
                                                                if (matchingResCon != null)
                                                                {
                                                                    matchingResCon.StoreConAdditional(newAdditCon);
                                                                    Dbg.LogPart(ldx, $"Completed connection of ConAddits ({newAdditCon}) to ConBase ({matchingResCon.ConBase}) [through ID '{newAdditCon.RelatedDataID}']; ");
                                                                    decodeInfo.NoteResult($"Connected ConAddits ({newAdditCon}) to ({matchingResCon.ConBase}) [by ID '{newAdditCon.RelatedDataID}']");
                                                                }
                                                                /// to be connected with ConBase from library
                                                                else
                                                                {
                                                                    looseConAddits.Add(newAdditCon);
                                                                    looseInfoRCDataIDs.Add(newAdditCon.RelatedDataID);
                                                                    Dbg.LogPart(ldx, $"No connection found: storing 'loose' ConAddits ({newAdditCon}) [using ID '{newAdditCon.RelatedDataID}']");
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
                                                                    Dbg.LogPart(ldx, " Relay:");
                                                                    decodeInfo.NoteIssue(ldx, heldIssueMsg);
                                                                    Dbg.LogPart(ldx, "; ");
                                                                }

                                                                Dbg.LogPart(ldx, "No Related Data ID, no match with existing ResContents; ");
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
                                                                    Dbg.Log(ldx, $"A ConAddits-to-ConBase ({ogConBase}) has similar content name; ");

                                                                    List<string> compiledDataIds = new();
                                                                    compiledDataIds.AddRange(ogConBase.DataIDString.Split(' '));
                                                                    compiledDataIds.AddRange(adtConDataIDs.ToArray());
                                                                    Dbg.LogPart(ldx, $"Integrating data IDs ({adtAsNewContent.DataIDString}) into existing ConAddits-to-ConBase; ");

                                                                    adtAsNewContent = new ContentBaseGroup(logVersion, newAdditCon.OptionalName, compiledDataIds.ToArray());
                                                                    resourceContents[resConArrIx] = new ResContents(newResConShelfNumber, adtAsNewContent);
                                                                    Dbg.LogPart(ldx, $"Regenerated ConBase (@ix{resConArrIx}) from ConAddits info :: {adtAsNewContent}");

                                                                    if (foundEditableDi)
                                                                    {
                                                                        DecodeInfo prevDi = decodingInfoDock[editDiIx];
                                                                        decodingInfoDock[editDiIx] = new DecodeInfo();

                                                                        DecodeInfo rewriteDI = new DecodeInfo($"{prevDi.logLine}\n{decodeInfo.logLine}", prevDi.sectionName);
                                                                        if (prevDi.NotedIssueQ)
                                                                            rewriteDI.NoteIssue(ldx, prevDi.decodeIssue);
                                                                        rewriteDI.NoteResult($"Regenerated ConAddits-to-ConBase :: {adtAsNewContent}");
                                                                        decodingInfoDock[editDiIx] = rewriteDI;
                                                                    }
                                                                }
                                                                // create new ConBase
                                                                else
                                                                {
                                                                    resourceContents.Add(new ResContents(newResConShelfNumber, adtAsNewContent));
                                                                    Dbg.LogPart(ldx, $"Generated new ConBase from ConAddits info :: {adtAsNewContent}");
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
                                                            Dbg.LogPart(ldx, "Line is missing information for 'related content';");
                                                            decodeInfo.NoteIssue(ldx, "Line is missing information for 'related content';");
                                                        }
                                                        else
                                                        {
                                                            Dbg.LogPart(ldx, "Line is missing information for 'additional content';");
                                                            decodeInfo.NoteIssue(ldx, "Line is missing information for 'additional content';");
                                                        }
                                                    }
                                            }
                                            else
                                            {
                                                if (logDataLine.Contains('-'))
                                                {
                                                    Dbg.LogPart(ldx, "This line contains too many '-'");
                                                    decodeInfo.NoteIssue(ldx, "This line contains too many '-'");
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "This line is missing '-'");
                                                    decodeInfo.NoteIssue(ldx, "This line is missing '-'");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbg.LogPart(ldx, "This line contains too many '>'");
                                                decodeInfo.NoteIssue(ldx, "This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line does not start with '>'");
                                                decodeInfo.NoteIssue(ldx, "This line does not start with '>'");
                                            }                                            
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Additional section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue(ldx, "Additional section tag must be parsed before decoding its data");
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

                                    Dbg.LogPart(ldx, "TTA Data  //  ");
                                    logDataLine = RemoveEscapeCharacters(logDataLine);
                                    if (IsSectionTag() && !parsedSectionTagQ)
                                    {
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(':') && logDataLine.CountOccuringCharacter(':') == 1)
                                        {
                                            Dbg.LogPart(ldx, $"Contains ':', raw data ({logDataLine}); ");
                                            string[] splitLogData = logDataLine.Split(':');
                                            if (splitLogData.HasElements())
                                                if (splitLogData[1].IsNotNEW())
                                                {
                                                    Dbg.Log(ldx, $"Has sufficent elements after split -- parsing; ");
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

                                                            Dbg.LogPart(ldx, $"--> Obtained TTA number '{ttaNum}'");
                                                            if (ranVerificationCountQ)
                                                            {
                                                                if (countedTTANum == ttaNum)
                                                                {
                                                                    Dbg.LogPart(ldx, " [Verified!]");
                                                                    checkVerifiedQ = true;
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, $" [Check counted '{countedTTANum}' instead]");
                                                                    decodeInfo.NoteIssue(ldx, $"TTA verification counted '{countedTTANum}' instead of obtained number '{ttaNum}'");
                                                                }
                                                            }
                                                            decodeInfo.NoteResult($"Got TTA number '{ttaNum}'");

                                                            ttaNumber = ttaNum;
                                                            sectionIssueQ = !checkVerifiedQ;
                                                            parsedSectionTagQ = true;
                                                        }
                                                        else
                                                        {
                                                            Dbg.LogPart(ldx, "TTA number is less than zero (0)");
                                                            decodeInfo.NoteIssue(ldx, "TTA number cannot be less than zero (0)");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Dbg.LogPart(ldx, "TTA number could not be parsed");
                                                        decodeInfo.NoteIssue(ldx, "TTA number could not be parsed");
                                                    }
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "No data in split @ix1");
                                                    decodeInfo.NoteIssue(ldx, "No data provied for TTA number");
                                                }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains(':'))
                                            {
                                                Dbg.LogPart(ldx, "This line has too many ':'");
                                                decodeInfo.NoteIssue(ldx, "This line has too many ':'");
                                            }
                                            else
                                            {
                                                if (logDataLine.ToLower().StartsWith(secTTA.ToLower()))
                                                {
                                                    Dbg.LogPart(ldx, "This line is missing ':'");
                                                    decodeInfo.NoteIssue(ldx, "This line is missing ':'");
                                                }
                                                else
                                                {
                                                    Dbg.Log(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                    decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must follow section tag open bracket");
                                                }                                                
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (parsedSectionTagQ)
                                        {
                                            Dbg.LogPart(ldx, "TTA section tag has already been parsed");
                                            decodeInfo.NoteIssue(ldx, "TTA section tag has already been parsed");
                                        }
                                        else if (logDataLine.StartsWith(secBracL))
                                        {
                                            Dbg.LogPart(ldx, "This line does not end with ']'");
                                            decodeInfo.NoteIssue(ldx, "This line does not end with ']'");
                                        }
                                        else
                                        {
                                            Dbg.LogPart(ldx, "This line does not start with '['");
                                            decodeInfo.NoteIssue(ldx, "This line does not start with '['");
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
                                                Dbg.Log(ldx, "Updated section tag has already been parsed");
                                                decodeInfo.NoteIssue(ldx, "Updated section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbg.Log(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// updated contents
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Identified updated data; ");
                                        logDataLine = RemoveEscapeCharacters(logDataLine);
                                        if (logDataLine.StartsWith('>') && logDataLine.CountOccuringCharacter('>') == 1)
                                        {
                                            
                                            /// Taco Sauce (y32) - Fixed to look much saucier
                                            /// (q42) - Redesigned to look cooler                                            
                                            if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                            {
                                                bool haltQ = false;
                                                bool _noticeNoManualSelfUpdQ = false;
                                                /// manual self-updating is discontinued // this removes any previous successful syntax, notifies of self-updating obsoletion
                                                if (logDataLine.Contains(updtSelfKey) && logDataLine.CountOccuringCharacter(updtSelfKey) == 1)
                                                {
                                                    if (logDataLine.Contains($">{updtSelfKey}"))
                                                    {
                                                        //selfUpdatingAllowed = true;
                                                        logDataLine = logDataLine.Replace(">:", ">");
                                                        //Dbg.LogPart(ldx, $"Self-updating enabled, contains '>{updtSelfKey}'; ");
                                                        Dbg.LogPart(ldx, $"Removed self-updating key, contains '>{updtSelfKey}' [obsolete]");
                                                        _noticeNoManualSelfUpdQ = true;
                                                    }
                                                    //else
                                                    else if (logDataLine.Contains($"{updtSelfKey}>"))
                                                    {
                                                        logDataLine = logDataLine.Replace(":>", ">");
                                                        Dbg.LogPart(ldx, $"Removed self-updating key, contains '{updtSelfKey}>' [obsolete]");
                                                        //Dbg.LogPart(ldx, $"Character '{updtSelfKey}' may only follow after '>' to enable self-updating function");
                                                        //decodeInfo.NoteIssue(ldx, $"Character '{updtSelfKey}' may only follow after '>' to enable self-updating function");
                                                        _noticeNoManualSelfUpdQ = true;
                                                    }
                                                }
                                                else
                                                {
                                                    if (logDataLine.CountOccuringCharacter(updtSelfKey) > 1)
                                                    {
                                                        Dbg.LogPart(ldx, $"Self-updating key limits '{updtSelfKey}' ignored [obsolete]");
                                                        //Dbg.LogPart(ldx, $"This line contains too many '{updtSelfKey}'");
                                                        //decodeInfo.NoteIssue(ldx, $"This line contains too many '{updtSelfKey}'");
                                                        //haltQ = true;
                                                        _noticeNoManualSelfUpdQ = true;
                                                    }
                                                }
                                                if (_noticeNoManualSelfUpdQ)
                                                    Dbg.Log(ldx, "; ");


                                                logDataLine = logDataLine.Replace(">", "");

                                                /// Taco Sauce (y32)  <-->  Fixed to look much saucier
                                                /// (q42)  <-->  Redesigned to look cooler
                                                Dbg.LogPart(ldx, "Contains '-'; ");
                                                string[] splitLogLine = logDataLine.Split('-');
                                                if (splitLogLine.HasElements(2) && !haltQ)
                                                    if (splitLogLine[0].IsNotNEW() && splitLogLine[1].IsNotNEW())
                                                    {
                                                        Dbg.Log(ldx, $"Got updated content info ({splitLogLine[0]}) and change description ({splitLogLine[1]}); ");

                                                        // updated contents info
                                                        Dbg.LogPart(ldx, "Content Parts: ");
                                                        string updtContentName = "", updtDataID = "", updtChangeDesc = "";
                                                        splitLogLine[0] = splitLogLine[0].Replace("(", " (").Replace(")", ") ");
                                                        string[] updtConData = splitLogLine[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                        for (int ux = 0; ux < updtConData.Length; ux++)
                                                        {
                                                            string updtDataPart = updtConData[ux];
                                                            if (updtDataPart.Contains('(') && updtDataPart.Contains(')') && updtDataID.IsNE())
                                                                updtDataID = updtDataPart;
                                                            else updtContentName += $"{updtDataPart} ";
                                                            Dbg.LogPart(ldx, $"{updtDataPart}{(ux + 1 == updtConData.Length ? "" : "|")}");
                                                        }
                                                        Dbg.LogPart(ldx, "; ");
                                                        updtDataID = RemoveParentheses(updtDataID);
                                                        updtContentName = CharacterKeycodeSubstitution(FixContentName(updtContentName));
                                                        Dbg.Log(ldx, "..");

                                                        bool okayDataIDQ = false;
                                                        if (updtDataID.Contains(',') || updtDataID.Contains(legRangeKey) || DetectUsageOfCharacterKeycode(updtDataID, cks01))
                                                        {
                                                            Dbg.LogPart(ldx, "Only one updated ID may be referenced per update");
                                                            if (DetectUsageOfCharacterKeycode(updtDataID, cks01))
                                                            {
                                                                Dbg.LogPart(ldx, $" (contains comma CKS '{cks01}')");
                                                                decodeInfo.NoteIssue(ldx, $"Only one updated ID can be referenced (no data groups; contains character keycode '{cks01}')");
                                                            }
                                                            else if (updtDataID.Contains(','))
                                                            {
                                                                Dbg.LogPart(ldx, " (contains ',')");
                                                                decodeInfo.NoteIssue(ldx, $"Only one updated ID can be referenced (no data groups; contains ',')");
                                                            }
                                                            else
                                                            {
                                                                Dbg.LogPart(ldx, $" (contains '{legRangeKey}')");
                                                                decodeInfo.NoteIssue(ldx, $"Only one updated ID can be referenced (no ranges; contains '{legRangeKey}')");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (updtDataID.IsNEW())
                                                            {
                                                                Dbg.LogPart(ldx, "No updated ID has been provided");
                                                                decodeInfo.NoteIssue(ldx, "No updated ID has been provided");
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
                                                                    Dbg.Log(ldx, $"Completed updated contents: Updated Name ({(updtContentName.IsNEW() ? "<null>" : updtContentName)}) and Data ID ({updtDataID}); ");
                                                                }
                                                                else
                                                                {
                                                                    Dbg.LogPart(ldx, "Updated ID was not a data ID (just numbers)");
                                                                    decodeInfo.NoteIssue(ldx, "Updated ID was just numbers (invalid)");
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
                                                            Dbg.Log(ldx, $"Generated {nameof(ContentChanges)} instance :: {newConChanges}; ");
                                                            bool selfUpdatingAllowed = false;

                                                            /// this checks for self-updating. In the event that it may have been intended but was not used. Force-enable
                                                            if (enableSelfUpdatingFunction)
                                                            {
                                                                Dbg.LogPart(ldx, "Checking for self-updating; ");
                                                                Dbg.LogPart(ldx, "In RContents? ");
                                                                if (resourceContents.HasElements())
                                                                {
                                                                    foreach (ResContents resCon in resourceContents)
                                                                    {
                                                                        if (resCon.ContainsDataID(updtDataID, out _))
                                                                        {
                                                                            Dbg.LogPart(ldx, $"Yes  // Found in: '{resCon}'");
                                                                            selfUpdatingAllowed = true;
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                                if (looseConAddits.HasElements())
                                                                {
                                                                    Dbg.LogPart(ldx, "No; In LooseAddits? ");
                                                                    foreach (ContentAdditionals conAddit in looseConAddits)
                                                                    {
                                                                        if (conAddit.ContainsDataID(updtDataID))
                                                                        {
                                                                            Dbg.LogPart(ldx, $"Yes  // Found in: '{conAddit}'");
                                                                            selfUpdatingAllowed = true;
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                                if (selfUpdatingAllowed)
                                                                    decodeInfo.NoteIssue(ldx, "Self-updating content: updating content in its introduced version is advised against");
                                                                Dbg.Log(ldx, $"{(!selfUpdatingAllowed ? "No (Pass)" : "")};");
                                                            }


                                                            /// testing... and now enabled, self updating contents aren't loose
                                                            bool isSelfConnected = false;
                                                            if (enableSelfUpdatingFunction && selfUpdatingAllowed)
                                                            {
                                                                Dbg.LogPart(ldx, "[SELF-UPDATING] Searching for connection in 'Decoded Library' -- ");

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

                                                                                Dbg.LogPart(ldx, $" Got match ('{newConChanges.RelatedDataID}' from source '{source}'");
                                                                                /// checks for matching content name against ConBase or ConAddits
                                                                                if (source.Equals(RCFetchSource.ConBaseGroup))
                                                                                {
                                                                                    if (resCon.ContentName == updtContentName)
                                                                                    {
                                                                                        Dbg.LogPart(ldx, $", plus matched name '{resCon.ContentName}'");
                                                                                        matchedNameQ = true;
                                                                                    }
                                                                                }
                                                                                else if (subMatchConAdd.IsSetup() && source.Equals(RCFetchSource.ConAdditionals))
                                                                                {
                                                                                    if (subMatchConAdd.OptionalName == updtContentName)
                                                                                    {
                                                                                        Dbg.LogPart(ldx, $", plus matched name '{subMatchConAdd.OptionalName}'");
                                                                                        matchedNameQ = true;
                                                                                    }
                                                                                }
                                                                                Dbg.LogPart(ldx, ")");

                                                                                if (matchedNameQ)
                                                                                {
                                                                                    Dbg.LogPart(ldx, "; Search end");
                                                                                    break;
                                                                                }
                                                                                else Dbg.LogPart(ldx, "; Search continue");
                                                                            }
                                                                        }
                                                                    Dbg.Log(ldx, "; ");
                                                                }

                                                                /// matching self-updated content connection
                                                                if (matchingResCon != null)
                                                                {
                                                                    matchingResCon.StoreConChanges(newConChanges);
                                                                    Dbg.LogPart(ldx, $"Completed connection of ConChanges ({newConChanges}) using ");
                                                                    if (subMatchConAdd.IsSetup())
                                                                        Dbg.LogPart(ldx, $"ConAddits ({subMatchConAdd})");
                                                                    else Dbg.LogPart(ldx, $"ConBase ({matchingResCon.ConBase})");
                                                                    Dbg.Log(ldx, $" [by ID '{newConChanges.RelatedDataID}'] (self-updated)");

                                                                    decodeInfo.NoteResult($"Connected ConChanges ({newConChanges}) to {(subMatchConAdd.IsSetup() ? $"ConAddits ({subMatchConAdd})" : $"ConBase {matchingResCon.ConBase}")} [by ID '{newConChanges.RelatedDataID}']");
                                                                    isSelfConnected = true;

                                                                    // these actions just for the decode display on log submission page
                                                                    ContentChanges copy4NonLoose = new(logVersion, newConChanges.InternalName + Sep, updtDataID, updtChangeDesc);
                                                                    looseConChanges.Add(copy4NonLoose); 
                                                                    looseInfoRCDataIDs.Add(copy4NonLoose.RelatedDataID);
                                                                    Dbg.LogPart(ldx, $" Added ConChanges copy (IntNam: '{copy4NonLoose.InternalName}') to loose for decode display");
                                                                }
                                                            }

                                                            /// the normal function: updates are always loose contents
                                                            if (!isSelfConnected)
                                                            {
                                                                looseConChanges.Add(newConChanges);
                                                                looseInfoRCDataIDs.Add(newConChanges.RelatedDataID);
                                                                
                                                                Dbg.LogPart(ldx, $"Storing 'loose' ConChanges");
                                                                decodeInfo.NoteResult($"Stored loose ConChanges ({newConChanges})");
                                                            }
                                                            sectionIssueQ = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (splitLogLine[0].IsNEW())
                                                        {
                                                            Dbg.LogPart(ldx, "This line is missing data for 'updated content'");
                                                            decodeInfo.NoteIssue(ldx, "This line is missing data for 'updated content'");
                                                        }
                                                        else
                                                        {
                                                            Dbg.LogPart(ldx, "This line is missing data for 'change description'");
                                                            decodeInfo.NoteIssue(ldx, "This line is missing data for 'change description'");
                                                        }
                                                    }
                                            }
                                            else
                                            {
                                                if (logDataLine.Contains('-'))
                                                {
                                                    Dbg.LogPart(ldx, "This line contains too many '-'");
                                                    decodeInfo.NoteIssue(ldx, "This line contains too many '-'");
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(ldx, "This line is missing '-'");
                                                    decodeInfo.NoteIssue(ldx, "This line is missing '-'");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbg.LogPart(ldx, "This line contains too many '>'");
                                                decodeInfo.NoteIssue(ldx, "This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line does not start with '>'");
                                                decodeInfo.NoteIssue(ldx, "This line does not start with '>'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Updated section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue(ldx, "Updated section tag must be parsed before decoding its data");
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
                                                Dbg.Log(ldx, "Legend section tag has already been parsed");
                                                decodeInfo.NoteIssue(ldx, "Legend section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbg.Log(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// legend data
                                    if (!isHeader && parsedSectionTagQ) 
                                    {
                                        if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                        {
                                            Dbg.LogPart(ldx, "Identified legend data; Contains '-'; ");
                                            logDataLine = RemoveEscapeCharacters(logDataLine);
                                            string[] splitLegKyDef = logDataLine.Split('-');
                                            if (splitLegKyDef.HasElements(2))
                                                if (splitLegKyDef[0].IsNotNEW() && splitLegKyDef[1].IsNotNEW())
                                                {
                                                    splitLegKyDef[0] = splitLegKyDef[0].Trim();
                                                    if (splitLegKyDef[0].Contains(" "))
                                                    {
                                                        splitLegKyDef[0] = splitLegKyDef[0].Replace(" ", "").Trim();
                                                        Dbg.LogPart(ldx, "Removed space in key; ");
                                                    }
                                                    splitLegKyDef[1] = splitLegKyDef[1].Trim();
                                                    Dbg.LogPart(ldx, $"Trimmed data; ");


                                                    LegendData newLegData = new(splitLegKyDef[0], logVersion, splitLegKyDef[1]);
                                                    Dbg.Log(ldx, $"Generated new {nameof(LegendData)} :: {newLegData.ToString()}; ");
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
                                                            decodeInfo.NoteIssue(ldx, "Legend definition should not have spaces if representing a data ID");
                                                    }


                                                    if (legendDatas.HasElements())
                                                    {
                                                        Dbg.LogPart(ldx, "Searching for existing key; ");
                                                        LegendData matchingLegDat = null;
                                                        foreach (LegendData legDat in legendDatas)
                                                        {
                                                            if (legDat.IsSetup())
                                                                if (legDat.Key == newLegData.Key)
                                                                {
                                                                    Dbg.LogPart(ldx, $"Found matching key ({newLegData.Key}) in ({legDat}); ");
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
                                                                Dbg.LogPart(ldx, $"Expanded legend data :: {matchingLegDat.ToString()}");
                                                                newDecInfo.NoteResult($"Edited existing legend data (new definition) :: {matchingLegDat.ToString()}");
                                                            }
                                                            else
                                                            {
                                                                Dbg.LogPart(ldx, $"New definition '{newLegData[0]}' could not be added (duplicate?)");
                                                                newDecInfo.NoteIssue(ldx, $"New definition '{newLegData[0]}' could not be added (duplicate?)");
                                                            }

                                                            if (newDecInfo.IsSetup())
                                                            {
                                                                decodeInfo = new();
                                                                decodingInfoDock[editDiIx] = newDecInfo;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Dbg.LogPart(ldx, "No similar key exists; ");
                                                            legendDatas.Add(newLegData);
                                                            Dbg.LogPart(ldx, "Added new legend data to decode library");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        legendDatas.Add(newLegData);
                                                        Dbg.LogPart(ldx, "Added new legend data to decode library");
                                                    }                                                    
                                                    //Dbg.LogPart(ldx, "; ");

                                                    sectionIssueQ = false;
                                                }
                                                else
                                                {
                                                    if (splitLegKyDef[0].IsNEW())
                                                    {
                                                        Dbg.LogPart(ldx, "This line is missing data for 'legend key'");
                                                        decodeInfo.NoteIssue(ldx, "This line is missing data for 'legend key'");
                                                    }
                                                    else
                                                    {
                                                        Dbg.LogPart(ldx, "This line is missing data for 'key definition'");
                                                        decodeInfo.NoteIssue(ldx, "This line is missing data for 'key definition'");
                                                    }
                                                }
                                            //Dbg.Log(ldx, "  //  End legend");
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains('-'))
                                            {
                                                Dbg.LogPart(ldx, "This line contains too many '-'");
                                                decodeInfo.NoteIssue(ldx, "This line contains too many '-'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line is missing '-'");
                                                decodeInfo.NoteIssue(ldx, "This line is missing '-'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Legend section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue(ldx, "Legend section tag must be parsed before decoding its data");
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
                                                Dbg.LogPart(ldx, "Summary section tag has already been parsed");
                                                decodeInfo.NoteIssue(ldx, "Summary section tag has already been parsed");
                                            }
                                        }
                                        else
                                        {
                                            Dbg.Log(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                            decodeInfo.NoteIssue(ldx, $"Only section name '{currentSectionName}' must be within section tag brackets");
                                        }
                                    }

                                    /// summary data
                                    if (!isHeader && parsedSectionTagQ)
                                    {
                                        logDataLine = logDataLine.Trim();
                                        Dbg.LogPart(ldx, "Identified summary data; ");
                                        if (logDataLine.StartsWith(">") && logDataLine.CountOccuringCharacter('>') == 1)
                                        {
                                            Dbg.LogPart(ldx, "Starts with '>'; ");
                                            string sumPart = FullstopCheck(logDataLine.Replace(">", ""));
                                            if (sumPart.IsNotNE())
                                            {
                                                bool isDupe = false;
                                                if (summaryDataParts.HasElements())
                                                {
                                                    Dbg.LogPart(ldx, "Valid summary part? ");
                                                    for (int sdpx = 0; sdpx < summaryDataParts.Count && !isDupe; sdpx++)
                                                    {
                                                        if (summaryDataParts[sdpx] == sumPart)
                                                            isDupe = true;
                                                    }
                                                    if (isDupe)
                                                    {
                                                        Dbg.LogPart(ldx, "No, this summary part is a duplicate");
                                                        decodeInfo.NoteIssue(ldx, "Summary part is a duplicate");
                                                    }
                                                    else Dbg.LogPart(ldx, "Yes (not a duplicate)");
                                                    Dbg.Log(ldx, "; ");
                                                }

                                                if (!isDupe)
                                                {
                                                    summaryDataParts.Add(sumPart);
                                                    Dbg.LogPart(ldx, $"--> Obtained summary part :: {sumPart}");
                                                    decodeInfo.NoteResult($"Obtained summary part :: {sumPart}");

                                                    sectionIssueQ = false;
                                                }                                                
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "Missing data for 'summary part'");
                                                decodeInfo.NoteIssue(ldx, "Missing data for 'summary part'");
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.StartsWith('>'))
                                            {
                                                Dbg.LogPart(ldx, "This line contains too many '>'");
                                                decodeInfo.NoteIssue(ldx, "This line contains too many '>'");
                                            }
                                            else
                                            {
                                                Dbg.LogPart(ldx, "This line does not start with '>'");
                                                decodeInfo.NoteIssue(ldx, "This line does not start with '>'");
                                            }
                                        }
                                    }

                                    /// no data parsing before tag issue
                                    if (!isHeader && !parsedSectionTagQ)
                                    {
                                        Dbg.LogPart(ldx, "Summary section tag must be parsed before decoding its data");
                                        decodeInfo.NoteIssue(ldx, "Summary section tag must be parsed before decoding its data");
                                    }

                                    EndSectionDbugLog("summary", !IsSectionTag());
                                }


                                /// decoding info dock reports
                                if (decodeInfo.IsSetup())
                                {
                                    Dbg.LogPart(ldx, $"{Ind34}//{Ind34}{{DIDCheck}} ");
                                    /// check if any decoding infos have been inserted                                    
                                    if (prevDecodingInfoDock.HasElements())
                                    {
                                        //Dbg.LogPart(ldx, "      {DIDCheck} ");
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

                                        Dbg.LogPart(ldx, $"[{changeType}]{changedIndex}");
                                        Dbg.LogPart(ldx, ";  ");
                                        //Dbg.Log(ldx, ";  //  ");
                                    }

                                    /// add new decoding info dock
                                    if (decodeInfo.IsSetup())
                                    {
                                        Dbg.LogPart(ldx, $"[Added]@{decodingInfoDock.Count};  ");
                                        decodingInfoDock.Add(decodeInfo);
                                    }

                                    /// set previous decoding info dock
                                    if (decodingInfoDock.HasElements())
                                    {
                                        //Dbg.LogPart(ldx, $"[<]prevDID set;  ");
                                        prevDecodingInfoDock = new List<DecodeInfo>();
                                        prevDecodingInfoDock.AddRange(decodingInfoDock.ToArray());
                                    }
                                }                                
                                Dbg.Log(ldx, " // ");


                                if (sectionIssueQ || decodeInfo.NotedIssueQ)
                                    if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1)))
                                        countSectionIssues[currentSectionNumber]++;

                                Dbg.NudgeIndent(ldx, false);
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
                                        Dbg.Log(ldx, " // ");
                                        preText = ".   ";
                                    }
                                }

                                //Dbg.Log(ldx, $"{preText}End {section} {subText}");
                                Dbg.LogPart(ldx, $"{preText}End {section} {subText}");
                            }
                        }
                        else
                        {
                            if (withinOmitBlock)
                            {
                                //Dbg.Log(ldx, $"Omitting L{lineNum} --> Block Omission: currently within line omission block.");
                                Dbg.LogPart(ldx, $"{lineNum} ");
                            }
                            else if (invalidLine)
                            {
                                /// report invalid key issue
                                DecodeInfo diMain = new($"L{lineNum}| {logDataLine}", "Main Decoding");
                                diMain.NoteIssue(ldx, $"The '{Sep}' character is an invalid character and may not be placed within decodable log lines.");
                                decodingInfoDock.Add(diMain);

                                Dbg.Log(ldx, $"Skipping L{lineNum} --> Contains invalid character: '{Sep}'");
                            }
                            else Dbg.Log(ldx, $"Omitting L{lineNum} --> Imparsable: Line starts with '{omit}'");
                        }
                    }
                    Dbg.NudgeIndent(ldx, false);
                    
                    // aka 'else'
                    if (logDataLine.IsNEW() && !endFileReading)
                    {
                        // as weird as it may seem, checks for non-described or unnoted legend keys goes here
                        if (!ranLegendChecksQ && usedLegendKeys.HasElements() && usedLegendKeysSources.HasElements() && legendDatas.HasElements() && currentSectionName == secLeg)
                        {
                            Dbg.NudgeIndent(ldx, true);
                            Dbg.Log(ldx, $"{{Legend_Checks}}");
                            const string clampSuffix = "...";
                            const int clampDistance = 7;

                            Dbg.NudgeIndent(ldx, true);
                            // get all keys
                            /// from notes
                            List<string> allKeys = new();
                            List<string> allKeysSources = null;
                            Dbg.LogPart(ldx, ">> Fetching used legend keys (and sources) :: ");
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
                                    Dbg.LogPart(ldx, $" {addedQ}'{usedKey}' ");
                                }
                            }
                            else
                            {
                                /// backup - legkeys only
                                Dbg.LogPart(ldx, "[aborted sources; UKLen!=UKSLen] :: ");
                                foreach (string usedKey in usedLegendKeys)
                                {
                                    string addedQ = null;
                                    if (!allKeys.Contains(usedKey))
                                    {
                                        allKeys.Add(usedKey);
                                        addedQ = "+";
                                    }
                                    Dbg.LogPart(ldx, $" {addedQ}'{usedKey}' ");
                                }
                            }
                            Dbg.Log(ldx, "; ");
                            /// from generations
                            Dbg.LogPart(ldx, ">> Fetching generated legend keys (& decInfoIx as '@#') :: ");
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
                                Dbg.LogPart(ldx, $" {addedQ}'{legDat.Key}' {diIx} ");
                            }
                            Dbg.Log(ldx, "; ");

                            // check all keys and determine if unnoted or unused
                            Dbg.Log(ldx, "Checking all legend keys for possible issues; ");
                            int getAccessIxFromListForOGdi = 0, usedSourceIx = 0;
                            foreach (string aKey in allKeys)
                            {
                                bool issueWasTriggered = false;
                                bool isUsed = usedLegendKeys.Contains(aKey);
                                bool isGenerated = generatedKeys.Contains(aKey);
                                DecodeInfo legCheckDi = new("<no source line>", secLeg);

                                if (isUsed || isGenerated)
                                {
                                    Dbg.LogPart(ldx, $"Checked: {(isUsed ? "  used" : "NO USE")}|{(isGenerated ? "gen'd " : "NO GEN")} // Result for key '{aKey}'  --> ");
                                    //Dbg.LogPart(ldx, $"key [{aKey}]  |  used? [{isUsed}]  |  generated? [{isGenerated}]  -->  ");
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
                                    Dbg.LogPart(ldx, "Unnoted Key Issue");
                                    legCheckDi.NoteIssue(ldx, $"Key '{aKey}' was used but not described (Unnoted Legend Key)");
                                    decodingInfoDock.Add(legCheckDi);
                                    issueWasTriggered = true;
                                }
                                /// unused key issue
                                else if (!isUsed && isGenerated)
                                {
                                    Dbg.LogPart(ldx, "Unused Key Issue");
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
                                            legCheckDi.NoteIssue(ldx, diIssueMsg);
                                            legCheckDi.NoteResult(ogDI.resultingInfo);

                                            decodingInfoDock[accessIx] = legCheckDi;
                                            fetchedAndEditedOGDi = true;
                                        }
                                    }

                                    if (!fetchedAndEditedOGDi)
                                        legCheckDi.NoteIssue(ldx, diIssueMsg);
                                    
                                    issueWasTriggered = true;
                                }
                                else if (isUsed && isGenerated)
                                    Dbg.LogPart(ldx, "Key is okay");

                                if (issueWasTriggered)
                                    countSectionIssues[currentSectionNumber]++;

                                usedSourceIx++;
                                Dbg.Log(ldx, "; ");
                            }

                            Dbg.NudgeIndent(ldx, false);
                            Dbg.NudgeIndent(ldx, false);

                            ranLegendChecksQ = true;
                        }


                        if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1)) && currentSectionName.IsNotNE() && !currentSectionName.Equals(lastSectionName))
                        {
                            Dbg.Log(ldx, $"..  Sec#{currentSectionNumber + 1} '{currentSectionName}' ended with [{countSectionIssues[currentSectionNumber]}] issues;");
                            lastSectionName = currentSectionName;
                        }

                        if (!firstSearchingDbgRanQ && !withinOmitBlock)
                        {
                            Dbg.LogPart(ldx, $"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                            Dbg.Log(ldx, $"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... (no longer within a section)" : "")}", logDataLine)}");
                        }
                        withinASectionQ = false;
                    }


                    // end file reading if (forced) else (pre-emptive)
                    float capacityPercentage = (llx + 1) / (float)readingTimeOut * 100;
                    if ((llx + 1 >= logData.Length || llx + 1 >= readingTimeOut) && !endFileReading)
                    {
                        endFileReading = true;
                        string forcedEndReason = llx + 1 >= readingTimeOut ? $"reading timeout - max {readingTimeOut} lines" : "file ends";
                        Dbg.Log(ldx, $" --> Decoding from file complete (forced: {forcedEndReason}) ... ending file reading <cap-{capacityPercentage:0}%>");

                        /// "File Too Long" issue
                        if (llx + 1 >= readingTimeOut)
                        {
                            DecodeInfo tooLongDi = new ("Decoder Issue; <no source line>", secVer);
                            tooLongDi.NoteIssue(ldx, $"File reading has been forced to end: Version Log is too long (limit of {readingTimeOut} lines)");
                            decodingInfoDock.Add(tooLongDi);
                        }    
                    }
                    else if (endFileReading)
                        Dbg.Log(ldx, $" --> Decoding from file complete ... ending file reading <cap-{capacityPercentage:0}%>");

                    TaskNum++;
                    ProgressBarUpdate(TaskNum / TaskCount, true, endFileReading);
                }
                Dbg.NudgeIndent(ldx, false);

                // -- compile decode library instance --
                if (endFileReading)
                {
                    Dbg.Log(ldx, "- - - - - - - - - - -");

                    DecodedLibrary = new ResLibrary();
                    ResContents looseInfoRC;
                    Dbg.LogPart(ldx, "Initialized Decode Library instance");

                    /// create loose ResCon
                    if (looseConAddits.HasElements() || looseConChanges.HasElements())
                    {
                        ContentBaseGroup looseCbg = new(logVersion, ResLibrary.LooseResConName, looseInfoRCDataIDs.ToArray());
                        looseInfoRC = new(newResConShelfNumber, looseCbg, looseConAddits.ToArray(), looseConChanges.ToArray());
                        resourceContents.Insert(0, looseInfoRC);
                        Dbg.LogPart(ldx, $"; Generated and inserted 'loose' ResCon instance :: {looseInfoRC}");
                    }
                    Dbg.Log(ldx, "; ");

                    if (testLibConAddUpdConnectionQ)
                    {
                        Dbg.Log(ldx, "ENABLED :: [TESTING LIBRARY CONADDITS AND CONCHANGES CONNECTIONS]");
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

                    Dbg.Log(ldx, $"Transferring decoded ResCons to Decode Library; {(resourceContents.HasElements() ? "" : "No ResCons to transfer to Decode Library; ")}");
                    if (resourceContents.HasElements())
                        DecodedLibrary.AddContent(true, resourceContents.ToArray());

                    Dbg.Log(ldx, $"Transferring decoded Legends to Decode Library; {(legendDatas.HasElements() ? "" : "No Legends to transfer to Decode Library; ")}");
                    if (legendDatas.HasElements())
                        DecodedLibrary.AddLegend(legendDatas.ToArray());

                    Dbg.LogPart(ldx, "Initializing summary data; ");
                    summaryData = new SummaryData(logVersion, ttaNumber, summaryDataParts.ToArray());
                    Dbg.Log(ldx, $"Generated {nameof(SummaryData)} instance :: {summaryData.ToStringShortened()};");
                    Dbg.Log(ldx, $"Transferring version summary to Decode Library; {(summaryData.IsSetup() ? "" : "No summary to transfer to Decode Library; ")}");
                    if (summaryData.IsSetup())
                        DecodedLibrary.AddSummary(summaryData);

                    Dbg.Log(ldx, "Transferring decode dbug info group to Decode Info Dock;");
                    DecodeInfoDock = decodingInfoDock;

                    // -- relay complete decode --
                    hasFullyDecodedLogQ = endFileReading && DecodedLibrary.IsSetup();
                    _hasDecodedQ = hasFullyDecodedLogQ;
                }
            }

            _allowLogDecToolsDbugMessagesQ = false;
            Dbg.EndLogging(ldx);
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
                    Dbg.LogPart(ldx, $"CKS: {dbgStr.Trim()}; ");
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
                Dbg.LogPart(ldx, "Removed square brackets; ");
            }
            return str;
        }
        /// <summary>also partly logs "Removed parentheses; "</summary>
        static string RemoveParentheses(string str)
        {
            if (str.IsNotNEW())
            {
                str = str.Replace("(", "").Replace(")", "");
                Dbg.LogPart(ldx, "Removed parentheses; ");
            }
            return str;
        }
        /// <summary>also partly logs "Name recieved: {name} -- Edited name: {fixedName}; "</summary>
        public static string FixContentName(string conNam, bool dbgLogQ = true)
        {
            if (dbgLogQ)
                Dbg.LogPart(ldx, $"Name recieved: ");
            string fixedConNam;
            if (conNam.IsNotNEW())
            {
                conNam = conNam.Trim();
                if (dbgLogQ)
                    Dbg.LogPart(ldx, $"{conNam} -- ");

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
                        Dbg.LogPart(ldx, $"Edited name: {fixedConNam}");
                    else Dbg.LogPart(ldx, "No edits");
                }                    
            }
            else
            {
                fixedConNam = conNam;
                if (dbgLogQ)
                    Dbg.LogPart(ldx, "<null>");
            }
            if (dbgLogQ)
                Dbg.LogPart(ldx, "; ");
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
                    Dbg.LogPart(ldx, $"Removed Esc.Chars [{subLogPart.Trim()}]; ");
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
                    Dbg.LogPart(ldx, "Added fullstop; ");
                }
                else Dbg.LogPart(ldx, "Checked fullstop; ");
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
                    Dbg.LogPart(ldx, $"Noted legend key: {legKey}");


                    if (usedLegendKeysSources != null && sourceDataID.IsNotNEW())
                    {
                        /// IF legend key sources list is not parallel in length to used legend keys list
                        if (usedLegendKeysSources.Count < usedLegendKeys.Count)
                        {
                            usedLegendKeysSources.Add(sourceDataID);
                            Dbg.LogPart(ldx, $" [+ID: {sourceDataID}]");
                        }
                        Dbg.LogPart(ldx, "; ");
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
                            Dbg.LogPart(ldx, $"GDnS: {dataKey}[{trueNum}]{suffix}; ");
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
                    Dbg.LogPart(ldx, $"DD:{dataKey}[{dataBody}]{suffix}; ");


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
