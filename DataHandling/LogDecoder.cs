﻿using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;

namespace HCResourceLibraryApp.DataHandling
{
    public class LogDecoder : DataHandlerBase
    {
        // Fields / props of Content Library and\or related library data elements (Probably just collections of ResContents, LegendData, and SummaryData 
        // Why inherit from DataHandlerBase? 
        //  IDEA: Have the Log Decoder save the last used directory, and make it easier to submit text files (static item)
        // ^^ Last thing to do before next git commit
        
        static string _prevRecentDirectory, _recentDirectory;
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

        public LogDecoder()
        {
            commonFileTag = "logDec";
            _recentDirectory = null;
        }

        // file saving - loading
        protected override bool EncodeToSharedFile()
        {
            string encodeRD = RecentDirectory.IsNEW() ? Sep : RecentDirectory;
            bool hasEnconded = FileWrite(false, commonFileTag, encodeRD);
            Dbug.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder has saved recent directory path :: {RecentDirectory}");
            return hasEnconded;
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
                    Dbug.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder recieved [{logDecData[0]}], and has loaded recent directory path :: {RecentDirectory}");
                    fetchedLogDecData = true;
                }
            return fetchedLogDecData;
        }
        public static new bool ChangesMade()
        {
            return _recentDirectory != _prevRecentDirectory;
        }

        // log file decoding
        public bool DecodeLogInfo(string[] logData)
        {
            bool hasFullyDecodedLogQ = false;
            Dbug.StartLogging("LogDecoder.DecodeLogInfo(str[])");
            Dbug.Log($"Recieved collection object (data from log?) to decode; collection has elements? {logData.HasElements()}");

            if (logData.HasElements())
            {
                Dbug.Log($"Recieved {logData.Length} lines of log data; proceeding to decode information...");
                short nextSectionNumber = 0, currentSectionNumber = 0;
                const string secVer = "Version", secAdd = "Added", secAdt = "Additional", secTTA = "TTA", secUpd = "Updated", secLeg = "Legend", secSum = "Summary";
                const string omit = "--", secBracL = "[", secBracR = "]";
                const string omitBlockOpen = "-- !open --", omitBlockClose = "-- !close --";
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
                const char legRangeKey = '~';
                const int dataKeyMaxLength = 3, newResConShelfNumber = 0, maxSectionsCount = 7;
                string[] logSections =
                {
                    secVer, secAdd, secAdt, secTTA, secUpd, secLeg, secSum
                };
                bool withinASectionQ = false, withinOmitBlock = false;
                string currentSectionName = null;
                int[] countSectionIssues = new int[8];
                List<DecodeInfo> decodingInfoDock = new();

                #region ToBeReplacedWithAppropriateDataTypes
                bool endFileReading = false;
                List<string[]> addedPholderNSubTags = new();

                // vv to be integrated into library later vv
                VerNum logVersion = VerNum.None;
                List<ResContents> resourceContents = new();
                List<LegendData> legendDatas = new();
                SummaryData summaryData = new();
                int ttaNumber = 0;
                #endregion


                // -- reading version log file --
                Dbug.NudgeIndent(true);
                for (int llx = 0; llx < logData.Length && !endFileReading; llx++)
                {
                    /// setup
                    int lineNum = llx + 1;
                    string logDataLine = logData[llx];
                    string nextSectionName = nextSectionNumber.IsWithin(0, (short)(logSections.Length - 1)) ? logSections[nextSectionNumber] : "";

                    if (!withinASectionQ && !withinOmitBlock)
                    {
                        Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                        endFileReading = nextSectionNumber >= maxSectionsCount;
                        //Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... ({nameof(withinASectionQ)} set to false)" : "")}", logDataLine)}");
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
                        bool omitThisLine = false;                        
                        if (logDataLine.StartsWith(omit) && !withinOmitBlock)
                        {
                            //if (!logDataLine.Contains(secBracL) && !logDataLine.Contains(secBracR))
                                omitThisLine = true;
                        }

                        /// if {parsable line} else {imparsable issue message}
                        if (!omitThisLine && !invalidLine && !withinOmitBlock)
                        {
                            // find a section
                            if (!withinASectionQ)
                            {
                                if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                {
                                    if (logDataLine.ToLower().Contains(nextSectionName.ToLower()))
                                    {
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
                                DecodeInfo decodeInfo = new DecodeInfo($"L{lineNum,-2}| {logDataLine}", currentSectionName);
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
                                                bool parsed = VerNum.TryParse(verSplit[1], out VerNum verNum);
                                                if (parsed)
                                                {
                                                    Dbug.LogPart($"--> Obtained {nameof(VerNum)} instance [{verNum}]");
                                                    decodeInfo.NoteResult($"{nameof(VerNum)} instance --> {verNum}");
                                                    logVersion = verNum;
                                                    sectionIssueQ = false;
                                                }
                                                else
                                                {
                                                    Dbug.LogPart($"{nameof(VerNum)} instance could not be parsed");
                                                    decodeInfo.NoteIssue($"{nameof(VerNum)} instance could not be parsed");
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
                                            Dbug.LogPart("This line is missing ':'");
                                            decodeInfo.NoteIssue("This line is missing ':'");
                                        }
                                    }
                                    Dbug.Log($"  //  End version");
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

                                    /// added tag (may contain substitution placeholders and substitutes)
                                    if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                    {
                                        Dbug.LogPart("Identified added section tag; ");
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(':'))
                                        {
                                            /// ADDED  <-->  x,t21; y,p84 
                                            Dbug.Log("Contains ':', spliting header tag from placeholders and substitutes; ");
                                            string[] splitAddHeader = logDataLine.Split(':');
                                            if (splitAddHeader.HasElements(2))
                                                if (splitAddHeader[1].IsNotNEW())
                                                {
                                                    Dbug.LogPart("Sorting placeholder/substitute groups :: ");
                                                    /// if()    x,t21  <-->  y,p84
                                                    /// else()  x,t21
                                                    List<string> addedPhSubs = new();
                                                    if (splitAddHeader[1].Contains(';'))
                                                    {
                                                        Dbug.LogPart("Detected multiple ph/sub groups >> ");
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
                                                        Dbug.LogPart($"Only one ph/sub group: {splitAddHeader[1]}");
                                                        addedPhSubs.Add(splitAddHeader[1]);
                                                    }
                                                    Dbug.Log($" << End ph/sub group sorting ");

                                                    if (addedPhSubs.HasElements())
                                                    {
                                                        Dbug.Log($"Spliting '{addedPhSubs.Count}' placeholder and substitution groups");
                                                        for (int ps = 0; ps < addedPhSubs.Count; ps++)
                                                        {
                                                            string phs = addedPhSubs[ps];
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
                                                                        addedPholderNSubTags.Add(new string[] { phSub[0].Trim(), phSub[1].Trim() });                                                                         sectionIssueQ = false;
                                                                    }
                                                                }
                                                            }
                                                            Dbug.Log(";  ");
                                                        }

                                                        string phSubs = "Got placeholder/substitute groups (as ph/'sub') :: ";
                                                        foreach (string[] phsubgroup in addedPholderNSubTags)
                                                            if (phsubgroup.HasElements(2))
                                                                phSubs += $"{phsubgroup[0]}/'{phsubgroup[1]}'; ";
                                                        decodeInfo.NoteResult(phSubs.Trim());
                                                    }
                                                    else decodeInfo.NoteIssue("Recieved no placeholder/substitute groups");
                                                }
                                        }
                                        else decodeInfo.NoteIssue("Missing ':'");
                                        Dbug.Log("  //  End added (section tag)");
                                    }

                                    /// added items (become content base groups)
                                    else
                                    {
                                        // setup
                                        string addedContentName = null;
                                        List<string> addedDataIDs = new();

                                        Dbug.LogPart("Identified added data; ");
                                        if (logDataLine.Contains('|') && logDataLine.CountOccuringCharacter('|') == 1)
                                        {
                                            /// 1  <-->  ItemName       -(x25; y32,33 q14)
                                            /// 2  <-->  Other o>       -(x21; a> q21) 
                                            Dbug.LogPart("Contains '|'; ");
                                            string[] splitAddedLine = logDataLine.Split('|');

                                            Dbug.Log($"item line (number '{splitAddedLine[0]}') has info? {splitAddedLine[1].IsNotNEW()}");
                                            if (splitAddedLine[1].IsNotNEW())
                                            {
                                                // replace placehoder keys with substitution phrases
                                                /// Other Items     -(x21; y42 q21)
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


                                                // parse content name and data ids
                                                if (splitAddedLine[1].Contains('-') && splitAddedLine[1].CountOccuringCharacter('-') == 1)
                                                {
                                                    Dbug.LogPart("Contains '-'; ");
                                                    /// ItemName  <-->  (x25; y32,33 q14)
                                                    /// Other Items  <-->  (x21; y42 q21)
                                                    string[] addedContsNDatas = RemoveParentheses(splitAddedLine[1]).Split('-');
                                                    addedContentName = FixContentName(addedContsNDatas[0]);

                                                    Dbug.Log("Fetching content data IDs; ");
                                                    if (addedContsNDatas[1].IsNotNEW())
                                                        if (addedContsNDatas[1].CountOccuringCharacter(';') <= 1) /// Zest -(x22)
                                                        {                                                            
                                                            Dbug.NudgeIndent(true);
                                                            /// (x25 y32,33 q14)
                                                            /// (x21 y42 q21)
                                                            addedContsNDatas[1] = addedContsNDatas[1].Replace(";", "");
                                                            string[] dataIdGroups = addedContsNDatas[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                                            if (dataIdGroups.HasElements())
                                                                foreach (string dataIDGroup in dataIdGroups)
                                                                {
                                                                    /// y32,33
                                                                    if (dataIDGroup.Contains(','))
                                                                    {
                                                                        Dbug.LogPart($"Got complex ID group '{dataIDGroup}'");
                                                                        string[] dataIDs = dataIDGroup.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                                                        if (dataIDs.HasElements())
                                                                        {
                                                                            string dataKey = RemoveNumbers(dataIDs[0]);
                                                                            if (dataKey.IsNotNEW())
                                                                            {
                                                                                Dbug.LogPart($"; Retrieved data key '{dataKey}'; Adding IDs :: ");
                                                                                foreach (string datId in dataIDs)
                                                                                {
                                                                                    string datToAdd = $"{dataKey}{datId.Trim().Replace(dataKey, "")}";
                                                                                    addedDataIDs.Add(datToAdd);
                                                                                    Dbug.LogPart($"{datToAdd} - ");
                                                                                }
                                                                            }                                                                            
                                                                        }
                                                                        Dbug.Log("; ");
                                                                    }
                                                                    /// r20~22
                                                                    /// q21`~24`
                                                                    else if (dataIDGroup.Contains(legRangeKey))
                                                                    {
                                                                        Dbug.LogPart($"Got range ID group '{dataIDGroup}'; ");
                                                                        string[] dataIdRng = dataIDGroup.Split(legRangeKey);
                                                                        if (dataIdRng.HasElements() && dataIDGroup.CountOccuringCharacter(legRangeKey) == 1)
                                                                        {
                                                                            string dataKey = RemoveNumbers(dataIdRng[0]);
                                                                            if (dataKey.IsNotNEW())
                                                                            {
                                                                                string dkSuffix = RemoveNumbers(dataIdRng[0].Replace(dataKey, ""));
                                                                                if (dkSuffix.IsNEW())
                                                                                    dkSuffix = RemoveNumbers(dataIdRng[1].Replace(dataKey, ""));
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
                                                            Dbug.LogPart("Data IDs line contains too many ';'");
                                                            decodeInfo.NoteIssue("Data IDs line contains too many ';'");
                                                        }
                                                }
                                                else
                                                {
                                                    if (splitAddedLine[1].Contains('-')) 
                                                    {
                                                        Dbug.LogPart("Item line contains too many '-'");
                                                        decodeInfo.NoteIssue("Item line contains too many '-'");
                                                    }
                                                    else 
                                                    {
                                                        Dbug.LogPart("Item line is missing '-'");
                                                        decodeInfo.NoteIssue("Item line is missing '-'");
                                                    }
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
                                        Dbug.Log("  //  End added (data)");
                                    }
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
                                    /// additional header (imparsable, but not an issue)
                                    if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                        sectionIssueQ = false;

                                    /// additional contents
                                    if (logDataLine.StartsWith('>'))
                                    {
                                        /// Sardine Tins (u41,42) - Sardines (x89)
                                        /// (q41~44) - (x104)
                                        /// (Cod_Tail) - CodFish (x132)
                                        /// Mackrel Chunks (r230,231 y192) - Mackrel (x300)
                                        logDataLine = logDataLine.Replace(">", "");
                                        Dbug.LogPart("Identified additional data; ");
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
                                                    // parsed additional content
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
                                                            if (adtConPart.Contains('(') || adtConPart.Contains(')'))
                                                                adtDataIDs += $"{adtConPart} ";
                                                            else adtContentName += $"{adtConPart} ";
                                                            Dbug.LogPart($"{adtConPart}{(ax + 1 >= additContsData.Length ? "" : "|")}");
                                                        }

                                                        /// u41  <-->  u42
                                                        /// q41  <-->  q42  <-->  q43  <-->  q44
                                                        /// CodTail
                                                        /// r230  <-->  r231  <-->  y192
                                                        if (adtDataIDs.IsNotNEW())
                                                        {
                                                            Dbug.Log("; ");
                                                            adtDataIDs = RemoveParentheses(adtDataIDs.Trim().Replace(' ', ','));
                                                            adtContentName = FixContentName(adtContentName);
                                                            Dbug.LogPart($"Parsed addit. content name ({(adtContentName.IsNEW() ? "<no name>" : adtContentName)}) and data ID group ({adtDataIDs})");

                                                            /// u24,23`,29~31
                                                            if (adtDataIDs.Contains(","))
                                                            {
                                                                Dbug.Log("; Data ID group contains ','; Fetching data IDs; ");
                                                                string[] datIDs = adtDataIDs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                                                if (datIDs.HasElements())
                                                                {
                                                                    string dataKey = RemoveNumbers(datIDs[0]);
                                                                    string dkSuffix = null;
                                                                    //Dbug.LogPart($"Retrieved data key '{(dataKey.IsNotNEW() ? dataKey : "<none>")}'");
                                                                    //Dbug.Log("; Fetching data IDs :: ");
                                                                    Dbug.NudgeIndent(true);
                                                                    foreach (string datId in datIDs)
                                                                    {
                                                                        Dbug.LogPart($"{datId} --  ");
                                                                        bool invalidDataKey = false;
                                                                        if (RemoveNumbers(datId).IsNotNEW())
                                                                        {
                                                                            invalidDataKey = RemoveNumbers(datId).Length > dataKeyMaxLength;
                                                                            if (invalidDataKey)
                                                                                Dbug.LogPart($"Lengthy Data ID, treat as word (len > {dataKeyMaxLength}); ");

                                                                            // fetch data key and suffix
                                                                            if (!invalidDataKey)
                                                                            {
                                                                                string preDataKey = RemoveNumbers(datId);
                                                                                string numAtSplit = null;
                                                                                if (preDataKey.IsNotNEW())
                                                                                {
                                                                                    numAtSplit = datId;
                                                                                    foreach (char c in preDataKey)
                                                                                        numAtSplit = numAtSplit.Replace(c.ToString(), "");
                                                                                }

                                                                                if (numAtSplit.IsNotNEW())
                                                                                {
                                                                                    string[] splitDat = datId.Split(numAtSplit);
                                                                                    if (splitDat.HasElements(2))
                                                                                    {
                                                                                        if (splitDat[0].IsNotNEW())
                                                                                            dataKey = splitDat[0];
                                                                                        if (splitDat[1].IsNotNEW())
                                                                                            dkSuffix = splitDat[1];

                                                                                        Dbug.LogPart($"Retrieved data key '{dataKey}'{(dkSuffix.IsNEW()? "" : $" and suffix [{dkSuffix}]")}; ");
                                                                                    }
                                                                                }
                                                                            }
                                                                        }                                                                            

                                                                        // data IDs with numbers
                                                                        /// q21 x24`
                                                                        if (!invalidDataKey)
                                                                        {
                                                                            /// q41~44
                                                                            if (datId.Contains(legRangeKey))
                                                                            {
                                                                                Dbug.LogPart($"Got range ID group '{datId}'; ");
                                                                                string[] dataIdRng;

                                                                                if (datId.Contains(dataKey))
                                                                                {
                                                                                    Dbug.LogPart($"Removed data key '{dataKey}' before split; ");
                                                                                    dataIdRng = datId.Replace(dataKey, "").Split(legRangeKey);
                                                                                }
                                                                                else dataIdRng = datId.Split(legRangeKey);

                                                                                if (dataIdRng.HasElements() && datId.CountOccuringCharacter(legRangeKey) == 1)
                                                                                {
                                                                                    dkSuffix = RemoveNumbers(dataIdRng[0]);
                                                                                    if (dkSuffix.IsNEW())
                                                                                        dkSuffix = RemoveNumbers(dataIdRng[1]);

                                                                                    if (dkSuffix.IsNotNEW())
                                                                                    {
                                                                                        Dbug.LogPart($"Retrieved suffix [{dkSuffix}]; ");
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
                                                                                string dataID = $"{dataKey}{datId.Replace(dataKey, "")}".Trim();
                                                                                adtConDataIDs.Add(dataID);
                                                                                Dbug.LogPart($"Got and added '{dataID}'");
                                                                            }
                                                                        }
                                                                        // data IDs without numbers
                                                                        /// CodTail
                                                                        else
                                                                        {
                                                                            adtConDataIDs.Add(datId);
                                                                            Dbug.LogPart($"Got and added '{datId}'");
                                                                        }

                                                                        Dbug.Log(";");
                                                                    }
                                                                    Dbug.NudgeIndent(false);
                                                                }
                                                                else
                                                                {
                                                                    Dbug.LogPart("Data group had no values for data IDs");
                                                                    decodeInfo.NoteIssue("Data group had no values for data IDs");
                                                                }

                                                            }           
                                                            
                                                            /// q41~44
                                                            else if (adtDataIDs.Contains(legRangeKey))
                                                            {
                                                                Dbug.Log("; Data ID group is a number range; ");
                                                                string[] dataIdRng = adtDataIDs.Split(legRangeKey);

                                                                if (dataIdRng.HasElements() && adtDataIDs.CountOccuringCharacter(legRangeKey) == 1)
                                                                {
                                                                    string dataKey = RemoveNumbers(dataIdRng[0]);
                                                                    if (dataKey.IsNotNEW())
                                                                    {
                                                                        string dkSuffix = RemoveNumbers(dataIdRng[0].Replace(dataKey, ""));
                                                                        if (dkSuffix.IsNEW())
                                                                            dkSuffix = RemoveNumbers(dataIdRng[1].Replace(dataKey, ""));
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
                                                            
                                                            /// y23
                                                            else adtConDataIDs.Add(adtDataIDs.Trim());

                                                            adtConDataIDs = adtConDataIDs.ToArray().SortWords();
                                                            if (adtDataIDs.Contains(","))
                                                                Dbug.LogPart("--> Sorted Data IDs; ");
                                                            else Dbug.Log("; Sorted Data IDs; ");


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
                                                            Dbug.LogPart("Missing content's data ID group");
                                                            decodeInfo.NoteIssue("Missing content's data ID group");
                                                        }
                                                    }

                                                    /// Sardines  <-->  (x89)
                                                    /// (x104)
                                                    /// CodFish  <-->  (x132)
                                                    // parse and find related contents
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
                                                                    adtRelatedName = FixContentName(adtRelatedName);
                                                                    okayRelName = true;
                                                                }
                                                                // for data id
                                                                if (adtRelatedDataID.IsNotNEW())
                                                                {
                                                                    if (!adtRelatedDataID.Contains(legRangeKey) && !adtRelatedDataID.Contains(','))
                                                                    {
                                                                        adtRelatedDataID = RemoveParentheses(adtRelatedDataID);
                                                                        okayRelDataID = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!adtRelatedDataID.Contains(legRangeKey))
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

                                                    // associated to related content \ create new content base group
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
                                                            Dbug.LogPart(" -- 1)in 'Decoded Library'");
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


                                                            // matching with contents within library
                                                            if (matchingResCon == null)
                                                            {
                                                                Dbug.LogPart($" -- 2)in 'Existing Library' [TO BE DONE]");
                                                            }
                                                        }
                                                        //else Dbug.LogPart("No Related Data ID provided");
                                                        Dbug.Log("; ");

                                                        // if (has relDataID, find related ConBase) else if (only relName, create new ConBase)
                                                        bool completedFinalStepQ = false;
                                                        if (newAdditCon.RelatedDataID.IsNotNE())
                                                        {
                                                            if (matchingResCon != null)
                                                            {
                                                                matchingResCon.StoreConAdditional(newAdditCon);
                                                                Dbug.LogPart($"Completed connection of ConAddits ({newAdditCon}) to ConBase ({matchingResCon.ConBase}) [through ID '{newAdditCon.RelatedDataID}']; ");
                                                                decodeInfo.NoteResult($"Connected ConAddits ({newAdditCon}) to ({matchingResCon.ConBase}) [by ID '{newAdditCon.RelatedDataID}']");
                                                                completedFinalStepQ = true;
                                                            }
                                                        }
                                                        else if (newAdditCon.OptionalName.IsNotNE())
                                                        {
                                                            Dbug.LogPart("No Related Data ID, no match with existing ResContents; ");
                                                            ContentBaseGroup adtAsNewContent = new(logVersion, newAdditCon.OptionalName, adtConDataIDs.ToArray());
                                                            resourceContents.Add(new ResContents(newResConShelfNumber, adtAsNewContent));
                                                            Dbug.LogPart($"Generated new ConBase from ConAddits info :: {adtAsNewContent}");
                                                            decodeInfo.NoteResult($"Generated ConBase from ConAddits info :: {adtAsNewContent}");
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
                                        Dbug.Log("  //  End additional");
                                    }
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
                                                            // only counts within the recently decoded contents
                                                            foreach (ResContents resCon in resourceContents)
                                                            {
                                                                if (resCon != null)
                                                                {
                                                                    // count from content base group
                                                                    if (resCon.ConBase != null)
                                                                        if (resCon.ConBase.IsSetup())
                                                                            countedTTANum += resCon.ConBase.CountIDs;

                                                                    // count from additional contents
                                                                    if (resCon.ConAddits.HasElements())
                                                                        foreach (ContentAdditionals conAddit in resCon.ConAddits)
                                                                        {
                                                                            if (conAddit != null)
                                                                                if (conAddit.IsSetup())
                                                                                    countedTTANum += conAddit.CountIDs;
                                                                        }
                                                                }
                                                            }
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
                                            Dbug.LogPart("This line is missing ':'");
                                            decodeInfo.NoteIssue("This line is missing ':'");
                                        }
                                    }
                                    Dbug.Log("  //  End tta");
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

                                    /// updated header (imparsable, but not an issue)
                                    if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                        sectionIssueQ = false;

                                    /// updated contents
                                    if (logDataLine.StartsWith('>'))
                                    {
                                        /// Taco Sauce (y32) - Fixed to look much saucier
                                        /// (q42) - Redesigned to look cooler
                                        logDataLine = logDataLine.Replace(">", "");
                                        Dbug.LogPart("Identified updated data; ");
                                        if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                        {
                                            /// Taco Sauce (y32)  <-->  Fixed to look much saucier
                                            /// (q42)  <-->  Redesigned to look cooler
                                            Dbug.LogPart("Contains '-'; ");
                                            string[] splitLogLine = logDataLine.Split('-');
                                            if (splitLogLine.HasElements(2))
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
                                                        Dbug.LogPart($"{updtDataPart}{(ux + 1 == updtConData.Length? "" : "|")}");
                                                    }
                                                    Dbug.LogPart("; ");
                                                    updtDataID = RemoveParentheses(updtDataID);
                                                    updtContentName = FixContentName(updtContentName);
                                                    Dbug.Log("..");

                                                    bool okayDataIDQ = true;
                                                    if (updtDataID.Contains(',') || updtDataID.Contains(legRangeKey))
                                                    {
                                                        Dbug.LogPart("Only one updated ID may be referenced per update description");
                                                        if (updtDataID.Contains(','))
                                                        {
                                                            Dbug.LogPart(" (contains ',')");
                                                            decodeInfo.NoteIssue($"Only one updated ID can be referenced per description (no data groups; contained ',')");
                                                        }
                                                        else
                                                        {
                                                            Dbug.LogPart($" (contains '{legRangeKey}'");
                                                            decodeInfo.NoteIssue($"Only one updated ID can be referenced per description (no ranges; contained '{legRangeKey}')");
                                                        }
                                                        okayDataIDQ = false;
                                                    }
                                                    else Dbug.Log($"Completed updated contents: Updated Name ({(updtContentName.IsNEW()? "<null>" : updtContentName)}) and Data ID ({updtDataID}); ");

                                                    // updated description
                                                    if (okayDataIDQ)
                                                        updtChangeDesc = FullstopCheck(splitLogLine[1]);

                                                    // generate ContentChanges, connect to ConBase or ConAddits, then attach to ResCon
                                                    if (okayDataIDQ)
                                                    {
                                                        ContentChanges newConChanges = new(logVersion, updtContentName, updtDataID, updtChangeDesc);
                                                        Dbug.Log($"Generated {nameof(ContentChanges)} instance :: {newConChanges}; ");
                                                        Dbug.LogPart("Searching for connection in 'Existing Library'** -- ");

                                                        ResContents matchingResCon = null;
                                                        ContentAdditionals subMatchConAdd = new();
                                                        /// should only be checking with existing library; can't update something that was just added
                                                        if (resourceContents != null)
                                                        {
                                                            if (resourceContents.HasElements())
                                                                foreach (ResContents resCon in resourceContents)
                                                                {
                                                                    if (resCon.ContainsDataID(updtDataID, out RCFetchSource source, out ContentAdditionals conAddix))
                                                                    {
                                                                        subMatchConAdd = conAddix;
                                                                        matchingResCon = resCon;

                                                                        /// id only: save match, keep searching
                                                                        /// id and name: save match, end searching
                                                                        bool matchedNameQ = false;

                                                                        Dbug.LogPart($" Got match ('{newConChanges.RelatedDataID}' from source '{source}'");
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

                                                        if (matchingResCon != null)
                                                        {
                                                            matchingResCon.StoreConChanges(newConChanges);
                                                            Dbug.LogPart($"Completed connection of ConChanges ({newConChanges}) using ");
                                                            if (subMatchConAdd.IsSetup())
                                                                Dbug.LogPart($"ConAddits ({subMatchConAdd})");
                                                            else Dbug.LogPart($"ConBase ({matchingResCon.ConBase})");
                                                            Dbug.Log($" [through ID '{newConChanges.RelatedDataID}']");
                                                            decodeInfo.NoteResult($"Connected ConChanges ({newConChanges}) to {(subMatchConAdd.IsSetup() ? $"ConAddits ({subMatchConAdd})" : $"ConBase {matchingResCon.ConBase}")} [by ID '{newConChanges.RelatedDataID}'");

                                                            sectionIssueQ = false;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (splitLogLine[0].IsNotNEW())
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
                                        Dbug.Log("  //  End updated");
                                    }
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
                                    if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                    {
                                        sectionIssueQ = false;
                                        isHeader = true;
                                    }

                                    /// legend data
                                    if (!isHeader) 
                                    {
                                        if (logDataLine.Contains('-') && logDataLine.CountOccuringCharacter('-') == 1)
                                        {
                                            Dbug.LogPart("Identified legend data; Contains '-'; ");
                                            string[] splitLegKyDef = logDataLine.Split('-');
                                            if (splitLegKyDef.HasElements(2))
                                                if (splitLegKyDef[0].IsNotNEW() && splitLegKyDef[1].IsNotNEW())
                                                {
                                                    splitLegKyDef[0] = splitLegKyDef[0].Trim();
                                                    splitLegKyDef[1] = splitLegKyDef[1].Trim();
                                                    Dbug.LogPart($"Trimmed data; ");


                                                    LegendData newLegData = new(splitLegKyDef[0], splitLegKyDef[1]);
                                                    Dbug.Log($"Generated new {nameof(LegendData)} :: {newLegData.ToStringLengthy()}; ");
                                                    decodeInfo.NoteResult($"Generated {nameof(LegendData)} :: {newLegData.ToStringLengthy()}");

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
                                                            bool addedDef = matchingLegDat.AddKeyDefinition(newLegData[0]);
                                                            if (addedDef)
                                                            {
                                                                Dbug.LogPart($"Expanded legend data :: {matchingLegDat.ToStringLengthy()}");
                                                                decodeInfo.NoteResult($"Edited existing legend data (new definition) :: {matchingLegDat.ToStringLengthy()}");
                                                            }
                                                            else
                                                            {
                                                                Dbug.LogPart("New definition could not be added to existing LegData instance");
                                                                decodeInfo.NoteIssue("New definition could not be added to existing LegData instance");
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
                                                    if (splitLegKyDef[0].IsNotNEW())
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
                                        Dbug.Log("  //  End legend");
                                    }

                                }

                                /// SUMMARY 
                                if (currentSectionName == secSum)
                                {
                                    // tbd...ltr
                                }


                                /** <SecName> (Log Template & Design Doc)
                                        LOG TEMPLATE
                                        --------------------
                                        ...                            

                            
                                        DESIGN DOC
                                        --------------------
                                        ...
                                */



                                // ---   ---   ---   ---   ---                               
                                /// DecodeInfo checklist
                                ///     SECTION             DONE?
                                ///     ----                ----
                                ///     Version             Y
                                ///     Added (header)      Y
                                ///     Added (content)     Y
                                ///     Additional          Y
                                ///     TTA                 Y
                                ///     Updated             Y
                                ///     Legend              Y
                                ///     Summary
                                ///     


                                if (decodeInfo.IsSetup())
                                    decodingInfoDock.Add(decodeInfo);

                                if (sectionIssueQ)
                                    if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1)))
                                        countSectionIssues[currentSectionNumber]++;

                                Dbug.NudgeIndent(false);
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
                            else Dbug.Log($"Omitting L{lineNum} --> Imparsable: Line contains '{omit}'");
                            //else Dbug.Log($"Omitting L{lineNum} --> Imparsable: Line contains '{omit}' and does not contain '{secBracL}' or '{secBracR}'");
                        }
                    }
                    Dbug.NudgeIndent(false);
                    
                    // aka 'else'
                    if (logDataLine.IsNEW() && !endFileReading)
                    {
                        if (currentSectionNumber.IsWithin(0, (short)(countSectionIssues.Length - 1))) 
                            Dbug.Log($"..  Sec#{currentSectionNumber + 1} '{currentSectionName}' ended with [{countSectionIssues[currentSectionNumber]}] issues;");
                        //Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");

                        Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                        Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... (no longer within a section)" : "")}", logDataLine)}");
                        withinASectionQ = false;
                    }

                    if (endFileReading)
                        Dbug.Log(" --> Decoding from file complete ... ending file reading");
                }
                Dbug.NudgeIndent(false);

                // -- compile decode library instance --
                if (endFileReading)
                {
                    Dbug.Log("- - - - - - - - - - -");
                }

                // -- assign result values --
                hasFullyDecodedLogQ = endFileReading;
            }
            Dbug.EndLogging();
            return hasFullyDecodedLogQ;


            // (static) methods?
            /// also partly logs "Removed square brackets; "
            static string RemoveSquareBrackets(string str)
            {
                if (str.IsNotNEW())
                {
                    str = str.Replace("[", "").Replace("]", "");
                    Dbug.LogPart("Removed square brackets; ");
                }
                return str;
            }
            /// also partly logs "Removed parentheses; "
            static string RemoveParentheses(string str)
            {
                if (str.IsNotNEW())
                {
                    str = str.Replace("(", "").Replace(")", "");
                    Dbug.LogPart("Removed parentheses; ");
                }
                return str;
            }
            /// also partly logs "Name recieved: {name} -- Edited name: {fixedName}; "
            static string FixContentName(string conNam)
            {
                Dbug.LogPart($"Name recieved: ");
                string fixedConNam;
                if (conNam.IsNotNEW())
                {
                    conNam = conNam.Trim();
                    Dbug.LogPart($"{conNam} -- ");

                    fixedConNam = "";
                    bool hadSpaceBefore = false;
                    foreach (char c in conNam)
                    {
                        /// if (upperCase && notSpaceChar)
                        ///     if (noSpaceBefore) {"C" --> " C"}
                        ///     else (spaceBefore) {"C"}
                        /// else (lowerCase || spaceChar)
                        ///     if (notSpaceChar && spaceBefore) { "c" --> "C" }
                        ///     else (spaceChar || notSpaceBefore) {"c"}

                        if (c.ToString() == c.ToString().ToUpper() && c != ' ')
                        {
                            if (!hadSpaceBefore)
                                fixedConNam += $" {c}";
                            else fixedConNam += c.ToString();
                        }
                        else
                        {
                            if (c != ' ' && hadSpaceBefore)
                                fixedConNam += c.ToString().ToUpper();
                            else fixedConNam += c.ToString();
                        }

                        hadSpaceBefore = c == ' ';
                    }
                    fixedConNam = fixedConNam.Trim();

                    if (fixedConNam != conNam)
                        Dbug.LogPart($"Edited name: {fixedConNam}");
                    else Dbug.LogPart("No edits");
                }
                else
                {
                    fixedConNam = conNam;
                    Dbug.LogPart("<null>");
                }
                Dbug.LogPart("; ");
                return fixedConNam;
            }
            static string RemoveNumbers(string str)
            {
                if (str.IsNotNEW())
                {
                    for (int i = 0; i < 10; i++) // 0~9 removed
                        str = str.Replace(i.ToString(), "");

                    if (str.IsNEW())
                        str = null;
                    else str = str.Trim();
                }
                return str;
            }
            /// also partly logs "Checked/Added fullstop; "
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
        }
    }        
}
