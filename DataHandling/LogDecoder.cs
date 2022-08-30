using System;
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
                short nextSectionNumber = 0;
                const string secVer = "Version", secAdd = "Added", secAdt = "Additional", secTTA = "TTA", secUpd = "Updated", secLeg = "Legend", secSum = "Summary";
                const string omit = "--", secBracL = "[", secBracR = "]";
                string[] logSections =
                {
                    secVer, secAdd, secAdt, secTTA, secUpd, secLeg, secSum
                };
                bool withinASectionQ = false;
                string currentSectionName = null;

                #region ToBeReplacedWithAppropriateDataTypes
                List<string[]> addedPholderNSubTags = new();                
                VerNum logVersion = VerNum.None;

                #endregion


                // -- reading version log file --
                Dbug.NudgeIndent(true);
                for (int llx = 0; llx < logData.Length; llx++)
                {
                    /// setup
                    int lineNum = llx + 1;
                    string logDataLine = logData[llx];
                    string nextSectionName = nextSectionNumber.IsWithin(0, (short)(logSections.Length - 1)) ? logSections[nextSectionNumber] : "";

                    if (!withinASectionQ)
                    {
                        Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                        Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... ({nameof(withinASectionQ)} set to false)" : "")}", logDataLine)}");
                    }
                        

                    Dbug.NudgeIndent(true);
                    if (logDataLine.IsNotNEW())
                    {
                        logDataLine = logDataLine.Trim();

                        /// imparsable line
                        bool omitThisLine = false;
                        if (logDataLine.StartsWith(omit))
                        {
                            if (!logDataLine.Contains(secBracL) && !logDataLine.Contains(secBracR))
                                omitThisLine = true;
                        }

                        /// parsable line
                        if (!omitThisLine)
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
                                        nextSectionNumber++;
                                        Dbug.Log($"Found section #{nextSectionNumber - 1} ({currentSectionName});");
                                    }
                                }
                            }

                            // parse section's data
                            if (withinASectionQ)
                            {
                                Dbug.Log($"{{{(currentSectionName.Length > 5 ? currentSectionName.Remove(5) : currentSectionName)}}}  L{lineNum,-2}| {logDataLine}");
                                Dbug.NudgeIndent(true);

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

                                    Dbug.LogPart("Version Data  //  ");
                                    logDataLine = RemoveSquareBrackets(logDataLine);
                                    if (logDataLine.Contains(":"))
                                    {
                                        Dbug.LogPart($"Contains ':', raw data :: {logDataLine}; ");
                                        string[] verSplit = logDataLine.Split(':');
                                        if (verSplit.HasElements(2))
                                        {
                                            Dbug.LogPart($"Has sufficent data after split; ");
                                            if (verSplit[1].IsNotNEW())
                                            {
                                                Dbug.Log($"Try parsing split @ix1 ({verSplit[1]}) into {nameof(VerNum)} instance; ");
                                                bool parsed = VerNum.TryParse(verSplit[1], out VerNum verNum);
                                                if (parsed)
                                                {
                                                    Dbug.LogPart($"--> Obtained {nameof(VerNum)} instance [{verNum}]");
                                                    logVersion = verNum;
                                                }
                                            }
                                        }
                                    }
                                    Dbug.Log($"  //  End '{currentSectionName}'");
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

                                    /// added tag (may contain substitution placeholders and substitutes)
                                    if (logDataLine.Contains(secBracL) && logDataLine.Contains(secBracR))
                                    {
                                        Dbug.LogPart("Identified added section tag; ");
                                        logDataLine = RemoveSquareBrackets(logDataLine);
                                        if (logDataLine.Contains(':'))
                                        {
                                            /// ADDED  <-s->  x,t21; y,p84 
                                            Dbug.Log("Contains ':', spliting header tag from placeholders and substitutes; ");
                                            string[] splitAddHeader = logDataLine.Split(':');
                                            if (splitAddHeader.HasElements(2))
                                                if (splitAddHeader[1].IsNotNEW())
                                                {
                                                    Dbug.LogPart("Sorting placeholder/substitute groups :: ");
                                                    /// if()    x,t21  <-s->  y,p84
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
                                                    else if (splitAddHeader[1].Contains(':'))
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

                                                            /// x  <-s->  t21
                                                            /// y  <-s->  p84
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
                                                                        addedPholderNSubTags.Add(new string[] { phSub[0].Trim(), phSub[1].Trim() }); // multiple tags? screw it.. first come, first serve
                                                                    }
                                                                }
                                                            }
                                                            Dbug.Log(";  ");
                                                        }
                                                    }
                                                }
                                        }
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
                                            /// 1  <-s->  ItemName       -(x25; y32,33 q14)
                                            /// 2  <-s->  Other o>       -(x21; a> q21) 
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
                                                            Dbug.LogPart($"subbed '{phSub[0]}' for '{phSub[1]}'; ");
                                                            spAddLnIx1 = spAddLnIx1.Replace(phSub[0], phSub[1]);
                                                        }
                                                }
                                                if (spAddLnIx1 != splitAddedLine[1])
                                                {
                                                    splitAddedLine[1] = spAddLnIx1;
                                                    Dbug.Log(" --> ph/sub application complete; ");
                                                }
                                                else Dbug.Log(" --> no changes");


                                                // parse content name and data ids
                                                if (splitAddedLine[1].Contains('-') && splitAddedLine[1].CountOccuringCharacter('-') == 1)
                                                {
                                                    Dbug.LogPart("Contains '-', fetching content name; ");
                                                    /// ItemName  <-s->  (x25; y32,33 q14)
                                                    /// Other Items  <-s->  (x21; y42 q21)
                                                    string[] addedContsNDatas = RemoveParentheses(splitAddedLine[1]).Split('-');
                                                    addedContentName = FixContentName(addedContsNDatas[0]);

                                                    Dbug.Log("Proceeding to fetch content data IDs; ");
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
                                                                                Dbug.LogPart($"; Retrieved data key '{dataKey}'; ");
                                                                                foreach (string datId in dataIDs)
                                                                                {
                                                                                    string datToAdd = $"{dataKey}{datId.Trim().Replace(dataKey, "")}";
                                                                                    addedDataIDs.Add(datToAdd);
                                                                                    Dbug.LogPart($"Added ID '{datToAdd}' -- ");
                                                                                }
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
                                                        else Dbug.LogPart("Data IDs line contains too many ';'");
                                                }
                                                else
                                                {
                                                    if (splitAddedLine[1].Contains('-'))
                                                        Dbug.LogPart("Item line contains too many '-'");
                                                    else Dbug.LogPart("Item line is missing '-'");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (logDataLine.Contains('|'))
                                                Dbug.LogPart("This line contains too many '|'");
                                            else Dbug.LogPart("This line is missing '|'");
                                        }

                                        // complete (Dbug revision here)
                                        if (addedDataIDs.HasElements() && addedContentName.IsNotNEW())
                                        {
                                            Dbug.LogPart($"Parsing complete -->  Obtained content name ({addedContentName}) and data Ids (");
                                            foreach (string datID in addedDataIDs)
                                                Dbug.LogPart($"{datID} ");
                                            Dbug.LogPart(")");
                                        }
                                        Dbug.Log("  //  End added (data)");
                                    }
                                }

                                Dbug.NudgeIndent(false);
                            }
                        }
                        else Dbug.Log($"Omitting L{lineNum} --> Imparsable: Line contains '{omit}' and does not contain '{secBracL}' or '{secBracR}'");
                    }
                    Dbug.NudgeIndent(false);
                    // aka 'else'
                    if (logDataLine.IsNEW())
                    {
                        //Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                        Dbug.LogPart($"Searching for Sec#{nextSectionNumber + 1} ({nextSectionName})  //  ");
                        Dbug.Log($"L{lineNum,-2}| {ConditionalText(logDataLine.IsNEW(), $"<null>{(withinASectionQ ? $" ... (no longer within a section)" : "")}", logDataLine)}");
                        withinASectionQ = false;
                    }
                }
                Dbug.NudgeIndent(false);
            }
            Dbug.EndLogging();
            return hasFullyDecodedLogQ;


            // static methods?
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
                    Dbug.LogPart($"{conNam} -- Edited name: ");

                    fixedConNam = "";
                    bool hadSpaceBefore = false;
                    foreach (char c in conNam)
                    {
                        if (c.ToString() == c.ToString().ToUpper() && c != ' ')
                        {
                            if (!hadSpaceBefore)
                                fixedConNam += $" {c}";
                            else fixedConNam += c.ToString();
                        }
                        else fixedConNam += c.ToString();
                        hadSpaceBefore = c == ' ';
                    }
                    fixedConNam = fixedConNam.Trim();
                    Dbug.LogPart(fixedConNam);
                }
                else
                {
                    fixedConNam = conNam;
                    Dbug.LogPart("(no change)");
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
                }
                return str;
            }
        }
    }
}
