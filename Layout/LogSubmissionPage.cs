using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System;
using System.Text;

namespace HCResourceLibraryApp.Layout
{
    public static class LogSubmissionPage
    {
        static readonly char subMenuUnderline = '*';
        static string pathToVersionLog = null;

        public static void OpenPage(ResLibrary mainLibrary)
        {
            bool exitLogSubMain = false;
            do
            {
                Program.LogState("Log Submission");
                Clear();
                Title("Version Log Submission", cTHB, 1);
                FormatLine($"{Ind24}Facilitates the submission of a version log to store content information regarding the resource pack.", ForECol.Accent);
                NewLine(2);

                bool validMenuKey = TableFormMenu(out short resNum, "Log Submission Menu", null, false, $"{Ind24}Selection >> ", "1/2", 2, $"Submit a Version Log,{exitPagePhrase}".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (resNum)
                    {
                        case 1:
                            SubPage_SubmitLog(mainLibrary);
                            break;

                        case 2:
                            exitLogSubMain = true;
                            break;
                    }
                }
                
            } while (!exitLogSubMain && !Program.AllowProgramRestart);

            // auto-saves: 
            //      -> LogDecoder recentDirectory
            //      -> ResLibrary <new data>
            if (LogDecoder.ChangesMade() || mainLibrary.ChangesMade())
                Program.SaveData(false);
        }

        static void SubPage_SubmitLog(ResLibrary mainLibrary)
        {
            bool exitSubmissionPage = false;
            do
            {
                /** Stages of verion log submission
                        - Provide path to version log (log location)
                        - Original version log review (raw)
                        - Processed version log review (decoded)
                 */
                
                Program.LogState("Log Submission|Submit A Log");
                Clear();
                Title("Submit a Version Log", subMenuUnderline, 1);
                FormatLine($"{Ind14}There are a few stages to submitting a version log:", ForECol.Normal);
                List(OrderType.Ordered_Numeric, "Provide file path to log,Original log review (raw),Processed log review (decoded)".Split(','));
                NewLine();

                Format($"{Ind24}Enter any key to continue to log submission >> ", ForECol.Normal);
                string input = StyledInput(null);
                if (input.IsNotNEW())
                {
                    LogDecoder logDecoder = new();
                    bool stopSubmission = false, compactDisplayQ = true;
                    int stageNum = 1;
                    const char minorChar = '-';
                    while (stageNum <= 3 && !stopSubmission)
                    {
                        string[] stageNames = 
                        { 
                            "Provide File Path", "Log Review (Raw)", "Log Review (Decoded)"
                        };
                        string stageName = stageNames[stageNum - 1];

                        Clear();
                        Program.LogState($"Log Submission|Submit A Log|Phase.{stageNum}, {stageName}");
                        Title("Log Submission", subMenuUnderline, 2);
                        Important($"STAGE {stageNum}: {stageName}", subMenuUnderline);
                        HorizontalRule(minorChar, 1);
                        bool stagePass = false;

                        // 1 - provide file path
                        if (stageNum == 1)
                        {
                            string placeHolder = @"C:\__\__.__";
                            const string recentDirectoryKey = @"RDir\";
                            /// if recent directory exists
                            if (LogDecoder.RecentDirectory.IsNotNEW())
                            {
                                Format($"The directory of the last submitted file was saved. To use this directory, precede the name of the submitted file with ", ForECol.Normal);
                                Highlight(true, $"'{recentDirectoryKey}'.", recentDirectoryKey);
                                Highlight(true, $"{recentDirectoryKey} :: {LogDecoder.RecentDirectory}", LogDecoder.RecentDirectory);
                                placeHolder += $"  -or-  {recentDirectoryKey}__.__";
                                NewLine();
                            }
                            FormatLine("Please enter the file path to the version log being submitted below. The file should be of type 'text file' (.txt) or any similar text-only file type.", ForECol.Normal);
                            Format($"{Ind14}Path >> ", ForECol.Normal);
                            string inputPath = StyledInput(placeHolder);

                            /// file path verification
                            bool validPath = false;
                            if (inputPath.IsNotNEW())
                            {
                                // substitute if using recent directory
                                if (inputPath.Contains(recentDirectoryKey) && LogDecoder.RecentDirectory.IsNotNEW())
                                    inputPath = inputPath.Replace(recentDirectoryKey, LogDecoder.RecentDirectory);

                                // validation
                                if (inputPath.Contains(":\\"))
                                {
                                    if (inputPath.Replace(":\\","").Contains("\\"))
                                    {
                                        if (inputPath.Contains("."))
                                        {
                                            bool validFilePath = SetFileLocation(inputPath);
                                            if (validFilePath)
                                            {
                                                validPath = FileRead(null, out string[] xLog);
                                                if (validPath)
                                                {
                                                    bool emptyFileQ = !xLog.HasElements();
                                                    if (emptyFileQ)
                                                    {
                                                        IncorrectionMessageQueue("There is no data within this file. Please choose another file path.");
                                                        validPath = false;
                                                    }
                                                }
                                                else
                                                    IncorrectionMessageQueue($"{Tools.GetRecentWarnError(false, false)}");
                                            }
                                            else IncorrectionMessageQueue($"{Tools.GetRecentWarnError(false, false)}");
                                        }
                                        else IncorrectionMessageQueue("No file type specified at end of path.");
                                    }
                                    else IncorrectionMessageQueue("No path directories specified.");
                                }
                                else IncorrectionMessageQueue("No hard drive specified.");
                            }
                            else
                            {
                                IncorrectionMessageQueue("No path entered.");
                                stopSubmission = true;
                            }
                            
                            /// file path confirmation
                            if (validPath)
                            {
                                // confirmation here
                                NewLine(3);
                                //Heading2("Confirm Log Path", false);
                                Important("Confirm Path".ToUpper(), subMenuUnderline);
                                Highlight(true, $"Provided path to version log ::\n{Ind14}{inputPath}", inputPath);
                                NewLine();
                                Confirmation($"{Ind14}Confirm path submission? \n{Ind24}Yes / No >> ", false, out bool yesNo);

                                if (yesNo)
                                {                                    
                                    pathToVersionLog = inputPath;
                                    SetFileLocation(pathToVersionLog);
                                    LogDecoder.RecentDirectory = Directory;
                                    stagePass = true;
                                }
                                ConfirmationResult(yesNo, $"{Ind34}", "Version log file path accepted.", "Path to version log denied.");
                            }
                            else IncorrectionMessageTrigger($"{Ind24}Invalid file path entered:\n{Ind34}");
                        }
                        // 2 - original log review (raw)
                        else if (stageNum == 2)
                        {
                            SetFileLocation(pathToVersionLog);
                            bool fetchedLogData = FileRead(null, out string[] logLines);
                            if (fetchedLogData)
                            {
                                if (logLines.HasElements())
                                {
                                    FormatLine($"Below sourced from :: \n{Ind14}{pathToVersionLog}", ForECol.Accent);
                                    NewLine();

                                    // display file info
                                    bool omitBlock = false;
                                    for (int lx = 0; lx < logLines.Length; lx++)
                                    {                                        
                                        string line = logLines[lx];
                                        /// omit block detect
                                        if (line.StartsWith(LogDecoder.omitBlockOpen))
                                            omitBlock = true;
                                        if (line.StartsWith(LogDecoder.omitBlockClose))
                                            omitBlock = false;
                                        /// section detect
                                        if (line.StartsWith("[") && line.EndsWith("]") && lx != 0 && !omitBlock)
                                            NewLine();
                                        /// omit line detect
                                        bool omit = line.StartsWith(LogDecoder.omit);
                                        bool invalidLine = line.Contains(DataHandlerBase.Sep);                                                                                      

                                        FormatLine(line, invalidLine? ForECol.Incorrection : (omit || omitBlock ? ForECol.Normal : ForECol.Highlight));
                                    }
                                    HorizontalRule(minorChar, 1);

                                    // confirm review
                                    NewLine();
                                    Important("Confirm Review".ToUpper(), subMenuUnderline);
                                    FormatLine("Please confirm that the contents above matches the version log submitted.", ForECol.Normal);
                                    Confirmation($"{Ind14}Reviewed version log confirmed? ", false, out bool yesNo);
                                    if (yesNo)
                                        stagePass = true;
                                    else stageNum = 1;
                                    ConfirmationResult(yesNo, $"{Ind34}", "Version log contents have been confirmed.", "Version log contents unconfirmed. Returning to previous stage.");
                                }                             
                            }
                            else
                                DataReadingIssue();

                        }
                        // 3 - processed log review (decoded)
                        else if (stageNum == 3)
                        {                            
                            bool unallowStagePass = false;
                            if (!logDecoder.HasDecoded)
                            {
                                mainLibrary.GetVersionRange(out _, out VerNum latestLibVer);

                                SetFileLocation(pathToVersionLog);
                                if (FileRead(null, out string[] fileData))
                                    logDecoder.DecodeLogInfo(fileData, latestLibVer);
                                else DataReadingIssue();
                            }

                            if (logDecoder.HasDecoded)
                            {
                                /// small section relaying how decoding went
                                if (compactDisplayQ)
                                    DisplayLogInfo(logDecoder, false);
                                /// large section showing how decoding went
                                else DisplayLogInfo(logDecoder, true);

                                NewLine(3);
                                HorizontalRule(minorChar, 1);
                                FormatLine("Plese review the contents retrieved from the log decoder above before proceeding.", ForECol.Highlight);
                                Format("Press [Enter] to toggle display style, and enter any key to continue >> ", ForECol.Normal);
                                input = StyledInput(null);

                                // if {view different displays} else {confirm info and add to library}
                                if (input.IsNE())
                                {
                                    unallowStagePass = true;
                                    compactDisplayQ = !compactDisplayQ;
                                }
                                else
                                {
                                    NewLine(2);
                                    Title("Integrate new contents to library");
                                    Format($"Enter any key to add the new contents to resource library >> ", ForECol.Normal);
                                    input = StyledInput(null);

                                    /// cancel integration
                                    if (input.IsNEW())
                                    {
                                        FormatLine($"{Ind24}By discontinuing integration of new contents, log submission will end.", ForECol.Accent);
                                        Confirmation($"{Ind24}Are you sure you wish to cancel content integration? ", true, out bool yesNo);
                                        if (yesNo)
                                        {
                                            Format($"{Ind34}Cancelled integration of new contents.", ForECol.Incorrection);
                                            stopSubmission = true;
                                            pathToVersionLog = null;
                                            Pause();
                                        }
                                        else unallowStagePass = true;
                                    }
                                    /// integrate into library
                                    else
                                    {
                                        mainLibrary.Integrate(logDecoder.DecodedLibrary);
                                        Format($"{Ind24}Integrated contents into library.", ForECol.Correction);
                                        Pause();
                                        exitSubmissionPage = true;
                                    }
                                }
                            }
                            else
                            {
                                Format($"The version log could not be decoded.", ForECol.Incorrection);
                                string possibleReason = null;
                                if (logDecoder.DecodedLibrary != null)
                                {
                                    if (!logDecoder.DecodedLibrary.Contents.HasElements())
                                        possibleReason = "Version log contents are missing or has incorrect logging syntax";
                                    else if (!logDecoder.DecodedLibrary.Legends.HasElements())
                                        possibleReason = "Version log legend is missing or has incorrect logging syntax";
                                    else if (!logDecoder.DecodedLibrary.Summaries.HasElements())
                                        possibleReason = "Version log summary is missing or has incorrect logging syntax";
                                }
                                if (possibleReason.IsNotNE())
                                {
                                    NewLine();
                                    Format($"{Ind24}Hint: {possibleReason}", ForECol.Incorrection);
                                }    
                                Pause();
                            }

                            if (!unallowStagePass)
                                stagePass = true;
                        }

                                                
                        /// pause and continue
                        if (stageNum < 3 && stagePass)
                        {
                            NewLine(3);
                            HorizontalRule(minorChar, 1);
                            Title("Continue Version Log Submission");
                            Format($"{Ind24}Enter any key to continue with log submission >> ", ForECol.Normal);
                            input = StyledInput(null);

                            stopSubmission = input.IsNEW();
                            if (stopSubmission)
                                pathToVersionLog = null;
                        }

                        /// move to next stage
                        stageNum += stagePass ? 1 : 0;


                        // local method
                        static void DataReadingIssue()
                        {
                            FormatLine($"{Ind14}Could not read data from log file!", ForECol.Incorrection);
                            Format($"{Ind14}If the file is open or locked, please close and enable access to the file and try again.", ForECol.Warning);
                            Pause();
                        }
                    }
                }
                else exitSubmissionPage = true;
            }
            while (!exitSubmissionPage);

            // auto-saves: 
            //      -> LogDecoder recentDirectory
            //      -> ResLibrary <new data>
            /// moved to OpenPage()
            //if (LogDecoder.ChangesMade() || mainLibrary.ChangesMade())
            //    Program.SaveData(true);
        }
        
        // public for now, so i may test it
        public static void DisplayLogInfo(LogDecoder logDecoder, bool showDecodeInfosQ)
        {
            /** RELATED INFO FROM DESIGN DOC
             >> Sorted.review.concepts
			    A conceptualization of how the obtained and sorted info will be displayed for review.
			
			    ..........................................
			    Version Number: 1.02
			
			    ADDED
			    #1 Peanut	i78	p54 t114
			    #2 Walnut	i79	t115
			    #3 Almond	i77 t113
			    #4 Large Macadamia Nut	i80 t117 t118
			
			    ADDITIONAL 
			    > Grains Biome Backgrounds 	b5 b11` b13
			    > Turnip Dusts (d6 d7) - Turnip (i14) 
			
			    TTA: 15
			
			    UPDATED 
			    > Mashed Potato		t32
				    Redesigned to look more creamy.
			    > Spoiled Potato	i38
				    Recolored to look less appetizing. 
			
			    LEGEND
			    i/Item
			    t/Tile
			    b/Background
			    `/LOTB
	
			    SUMMARY
			    - Nuts, Mash Potato redesign, Spoiled Potato redesign
			    - Grains Biome Backgrounds
			    ..........................................			
			
		    >> Explain Contents Review Concept (above)
		    .	"Version Number: 1.00" - The version number of the log is plainly listed.
		    .	"ADDED..." - Each new piece of content is numbered (from 1 upwards) and listed in the syntax: {ContentName}{ContentDataIds}
				    > {ContentName} is the name given to the added content.
				    > {ContentDataIds} are the Data Ids associated with the added content. According to key recognizing the type of data, the Ids are listed alphanumerically. 
					    Note that ranges of data IDs will be separated into individuals enclosed in parentheses beside the range. 
						    Ex.		a3~a6 (a3 a4 a5 a6)
								    g9`~g11` (g9` g10` g11`) 
		    .	"ADDITIONAL..." - Each additional piece of content is indented with a '>' (unordered), followed by the syntax: {AdditionalNameAndDataIds}{RelatedAdditionalNameAndID}
				    Morphing the syntax directly from the log, the review syntax would be: {Opt.Name}{DataID} {RelatedInternalName}{RelatedDataID}
				    > {RelatedAdditionalNameAndID} - The relative name and ids to associate with the additional contents. May not always have a value.
				    > {AdditionalNameAndDataIds} - The additional content ids and an optional name to describe them.
		    .	"UPDATED..." - The changes made to existing content following the syntax: {RelatedInternalName}{DataId}\n{ChangeDescription}
				    > {RelatedInternalName} - The name of the content updated.
				    > {DataId} - The Data Id of the content updated.
				    > \n{ChangedDescription} - On a newline, a description of the change made to the data ID. This changes is attributed to the Data ID.
		    .	"LEGEND..." - The definitions for the keys used in the version log. The legend keys are documented by the system along with all other information. 
				    > Syntax is similar to that of the log (the characters ' - ' are replaced with '/').
		    . 	"SUMMARY..." - The summary of all the additions and changes that have taken place in the version log being reviewed.

		    .. OTHER REVIEW RULES ..
			    > n/a			
             
             **/

            /** EDITED CONCEPTUALIZATION FOR SHOWING DECODE INFO     
                ..........................................
			    Version Number: 1.02 {!0}
                  sl [Version : 1.02]
                  d+ Generated VerNum instance :: v1.02.
			
			    ADDED {!0}
			    #1 Peanut	i78	p54 t114
                  sl 1 |Peanut -(i78; t114 p54)
                  d+ Generated ConBase instance :: v1.02;Peanut;i78,p54,t114.
			    #2 Walnut	i79	t115
                  ...
			    #3 Almond	i77 t113
                  ...
			    #4 Large Macadamia Nut	i80 t117 t118
                  ...
			
			    ADDITIONAL {!1}
			    > Grains Biome Backgrounds 	b5 b11` b13
                  sl > b5,b11`,b13 - Grains Biome Backgrounds
                  d+ > 
			    > Turnip Dusts (d6 d7)
                    related to Turnip (i14)
                  ...
                sl > d2, - Grass i32
                  d- This line is missing 'related Data Id'
                
			
			    TTA: 15 {!1}
                  sl [TTA : 15]
                  d- Decoder counted 17 instead of 15
                  d+ Retrieved value of 15 
			
			    UPDATED {!0}
			    > Mashed Potato		t32
				    Redesigned to look more creamy.
                  sl > Mashed Potato (t32) - Redesigned to look more creamy.
                  d+ Generated ConChanges instance :: v1.02;Mashed Potato;t32;Redesigned to look more creamy.
			    > Spoiled Potato	i38
				    Recolored to look less appetizing. 
                  ...
			
			    LEGEND {!0}
			    i/Item
                  sl i - Item
                  d+ Generated Legend instance :: i;Item.
			    t/Tile
                  ...
			    b/Background
                  ...
			    `/LOTB
                  ...
	
			    SUMMARY {!0}
			    - Nuts, Mash Potato redesign, Spoiled Potato redesign
                  sl > Nuts, Mash Potato redesign, Spoiled Potato redesign
                  d+ Added summary part :: Nuts, Mash Potato redesign, Spoiled Potato redesign.
			    - Grains Biome Backgrounds
                  ...
			    ..........................................			
             
                {!x}    Beside every section name, denotes the number of issues within decoded section (also as '{x issues}') [Col: Warning / Highlight]
                sl      Source Line (original related line from decoded file) [Col: Accent / Input]
                d-      The decoder info issue message [Col: Incorrection]
                d+      The decoder info result (success) message [Col: Correction]

                NOTE 
                - The legend symbols 'sl', 'd-', and 'd+' are not required to precede their messages within the display. This may only be for this conceptutalization display, color and a separate legend will create the distinctions in the output.       
                - The above displays the expanded form of display concept. The original copy will inherit only the issue count beside each section name.
                - When a source line is not accompanied by a line from the content review, it is unindented and the following issues / results are indented after it
                  Ex. W/ Review Line        W/out Review Line
                    |review                 |sl
                    |  sl                   |  d-
                    |  d-                   |  d+
                    |  d+
                
             **/

            if (logDecoder != null)
            {
                if (logDecoder.DecodedLibrary != null)
                {
                    if (logDecoder.DecodedLibrary.IsSetup())
                    {
                        /** SNIPPET - Related Info from Design Doc
                        ..........................................
                        Version Number: 1.02
                
                        ADDED
                        #1 Peanut	i78	p54 t114
                        #2 Walnut	i79	t115
                        #3 Almond	i77 t113
                        #4 Large Macadamia Nut	i80 t117 t118
                
                        ADDITIONAL 
                        > Turnip Dusts (d6 d7) - Turnip (i14)
                
                        TTA: 15
                
                        UPDATED 
                        > Mashed Potato		t32
                            Redesigned to look more creamy.
                        > Spoiled Potato	i38
                            Recolored to look less appetizing. 
                
                        LEGEND
                        i/Item
                        t/Tile
                        b/Background
                        `/LOTB
        
                        SUMMARY
                        - Nuts, Mash Potato redesign, Spoiled Potato redesign
                        - Grains Biome Backgrounds
                        ..........................................			
                         **/

                        /// text configuration
                        ForECol colReviewLine = ForECol.Normal, colIssueNumber = ForECol.Warning;
                        ForECol colSourceLine = ForECol.Accent, colIssueMsg = ForECol.Incorrection, colResultMsg = ForECol.Correction;

                        ResLibrary decLibrary = logDecoder.DecodedLibrary;
                        const int sectionNewLines = 1;
                        const string looseContentIndicator = "{l~}";
                        DecodedSection[] sections = (DecodedSection[])typeof(DecodedSection).GetEnumValues();
                        foreach (DecodedSection section in sections)
                        {
                            List<string> reviewTexts = new();
                            string sectionName = section.ToString();

                            // get text to print
                            /// VERSION 
                            if (section == DecodedSection.Version)
                            {
                                /** VER FORMAT
                                    Version Number: 1.02
                                 */
                                //ResContents anyRC = decLibrary.Contents[0];
                                //if (anyRC.IsSetup())
                                //    reviewTexts.Add($"Version Number: {anyRC.ConBase.VersionNum.ToStringNumbersOnly()}");
                                reviewTexts.Add($"Version Number: {decLibrary.Summaries[0].SummaryVersion.ToStringNums()}");
                            }
                            /// ADDED
                            if (section == DecodedSection.Added)
                            {
                                /** ADD FORMAT
                                    ADDED
                                    #1 Peanut	i78	p54 t114
                                    #2 Walnut	i79	t115
                                    #3 Almond	i77 t113
                                    #4 Large Macadamia Nut	i80 t117 t118
                                 */

                                reviewTexts.Add(sectionName.ToUpper());
                                int addedItemNum = 1;
                                for (int aix = 0; aix < decLibrary.Contents.Count; aix++)
                                {
                                    ResContents resCon = decLibrary.Contents[aix];
                                    if (resCon.IsSetup())
                                        if (resCon.ContentName != ResLibrary.LooseResConName)
                                        {
                                            //const int autoPadNum = 16;
                                            //int conNameLen = resCon.ContentName.Length;
                                            //int padFactor = (int)Math.Ceiling((conNameLen) / (float)autoPadNum);
                                            //reviewTexts.Add($"#{aix, -2} {resCon.ContentName.PadRight(padFactor * autoPadNum)}{Ind14}{resCon.ConBase.DataIDString}");

                                            reviewTexts.Add($"#{addedItemNum} {resCon.ContentName} {Ind14}{resCon.ConBase.DataIDString}");
                                            addedItemNum++;
                                        }
                                }
                            }
                            /// ADDITIONAL
                            if (section == DecodedSection.Additional)
                            {
                                /** ADT FORMAT
                                    ADDITIONAL 
                                    > Turnip Dusts (d6 d7)
                                        related to Turnip (i14)
                                 */

                                // connected ConAddits
                                reviewTexts.Add(sectionName.ToUpper());
                                foreach (ResContents resCon in decLibrary.Contents)
                                {
                                    if (resCon.IsSetup() && resCon.ContentName != ResLibrary.LooseResConName)
                                    {
                                        if (resCon.ConAddits.HasElements())
                                            foreach (ContentAdditionals rcConAdt in resCon.ConAddits)
                                            {
                                                string partRt = $"> {(rcConAdt.OptionalName.IsNotNEW() ? $"{rcConAdt.OptionalName} " : "")}";
                                                reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - {resCon.ContentName} ({rcConAdt.RelatedDataID})");
                                            }
                                    }
                                }
                                // loose ConAddits
                                ResContents looseResCon = decLibrary.Contents[0];
                                if (looseResCon.ContentName == ResLibrary.LooseResConName)
                                {
                                    if (looseResCon.ConAddits.HasElements())
                                        foreach (ContentAdditionals rcConAdt in looseResCon.ConAddits)
                                        {
                                            string partRt = $"> {(rcConAdt.OptionalName.IsNotNEW() ? $"{rcConAdt.OptionalName} " : "")}";
                                            reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - ({rcConAdt.RelatedDataID}) {looseContentIndicator}");
                                        }
                                }
                            }
                            /// TTA
                            if (section == DecodedSection.TTA)
                            {
                                /** TTA FORMAT
                                    TTA: 15
                                 */
                                                                
                                SummaryData summary = decLibrary.Summaries[0];
                                reviewTexts.Add($"TTA: {summary.TTANum}");
                            }
                            /// UPDATED
                            if (section == DecodedSection.Updated)
                            {
                                /** UPD FORMAT
                                    UPDATED 
                                    > Mashed Potato		t32
                                        Redesigned to look more creamy.
                                    > Spoiled Potato	i38
                                        Recolored to look less appetizing. 
                                 */

                                //reviewTexts.Add($"{sectionName.ToUpper()} [loose]");
                                reviewTexts.Add(sectionName.ToUpper());
                                ResContents looseResCon = decLibrary.Contents[0];
                                if (looseResCon.ContentName == ResLibrary.LooseResConName)
                                {
                                    if (looseResCon.ConChanges.HasElements())
                                        foreach (ContentChanges rcConChg in looseResCon.ConChanges)
                                        {
                                            string rtPart1 = $"> {(rcConChg.InternalName.IsNotNEW() ? $"{rcConChg.InternalName}{Ind24}" : "")}{rcConChg.RelatedDataID}";
                                            string rtPart2 = $"{Ind24}{rcConChg.ChangeDesc}";
                                            string rtPart3 = $" {looseContentIndicator}";
                                            if (rcConChg.InternalName.IsNotNE())
                                                if (rcConChg.InternalName.Contains(DataHandlerBase.Sep))
                                                {
                                                    rtPart1 = rtPart1.Replace(DataHandlerBase.Sep, "");
                                                    rtPart3 = "";
                                                }                                            
                                            reviewTexts.Add($"{rtPart1}\n{rtPart2}{rtPart3}");
                                        }
                                }
                            }
                            /// LEGEND
                            if (section == DecodedSection.Legend)
                            {
                                /** LGD FORMAT
                                    LEGEND
                                    i/Item
                                    t/Tile
                                    b/Background
                                    `/LOTB
                                 */

                                reviewTexts.Add(sectionName.ToUpper());
                                if (decLibrary.Legends.HasElements())
                                {
                                    foreach (LegendData legDat in decLibrary.Legends)
                                    {
                                        string legDefs = "";
                                        if (legDat.CountDefinitions > 0)
                                            for (int i = 0; i < legDat.CountDefinitions; i++)
                                                legDefs += $"{legDat[i]}" + (i + 1 < legDat.CountDefinitions ? "/" : "");
                                        reviewTexts.Add($"{legDat.Key}/{legDefs}");
                                    }
                                }
                            }
                            /// SUMMARY
                            if (section == DecodedSection.Summary)
                            {
                                /** SUM FORMAT
                                    SUMMARY
                                    - Nuts, Mash Potato redesign, Spoiled Potato redesign
                                    - Grains Biome Backgrounds
                                 */
                                reviewTexts.Add(sectionName.ToUpper());
                                SummaryData summary = decLibrary.Summaries[0];
                                if (summary.IsSetup())
                                {
                                    foreach (string sumPart in summary.SummaryParts)
                                        reviewTexts.Add($"- {sumPart}");
                                }
                            }

                            
                            // text printing
                            if (reviewTexts.HasElements() || true)
                            {
                                /// text printing
                                bool bypassLoopMax = showDecodeInfosQ == true;
                                for (int rtx = 0; rtx < reviewTexts.Count || bypassLoopMax; rtx++)
                                {
                                    DecodeInfo decodeInfo = logDecoder.GetDecodeInfo(section, rtx, out int issueCount);

                                    /// review line
                                    bool reviewLinePrintedQ = false;
                                    if (rtx < reviewTexts.Count)
                                    {
                                        string rtext = reviewTexts[rtx].ToString();
                                        Format(rtext, colReviewLine);
                                        reviewLinePrintedQ = true;
                                    }

                                    /// section issues count
                                    if (rtx == 0)
                                        Format($" {{!{issueCount}}}", colIssueNumber);
                                    NewLine();

                                    if (showDecodeInfosQ)
                                    {
                                        if (decodeInfo.IsSetup())
                                        {
                                            /// source line
                                            FormatLine($"{(reviewLinePrintedQ ? Ind14 : "")}{decodeInfo.logLine}", colSourceLine);
                                            /// decode issue
                                            if (decodeInfo.NotedIssueQ)
                                                FormatLine($"{Ind14}{decodeInfo.decodeIssue}", colIssueMsg);
                                            /// decode result
                                            if (decodeInfo.NotedResultQ)
                                                FormatLine($"{Ind14}{decodeInfo.resultingInfo}", colResultMsg);
                                            //NewLine();
                                        }
                                        else
                                            bypassLoopMax = false;
                                    }
                                }

                                /// newline per section
                                if (section != DecodedSection.Summary)
                                {
                                    if (showDecodeInfosQ)
                                        NewLine(2);
                                    else NewLine(sectionNewLines);
                                }
                            }
                        }


                        // decode legend print
                        NewLine(2);
                        Title("Decode Symbols");
                        for (int i = 0; i < 5; i++)
                        {
                            string symbol = "", symDesc = "";
                            ForECol symCol = ForECol.Normal;

                            /// for issues number
                            if (i == 0)
                            {
                                symbol = "{!x}";
                                symDesc = "Number of Section Issues";
                                symCol = colIssueNumber;
                            }
                            if (showDecodeInfosQ)
                            {
                                /// for source line
                                if (i == 1)
                                {
                                    symbol = "sl";
                                    symDesc = "Source Line from Version Log";
                                    symCol = colSourceLine;
                                }

                                /// for decode issue
                                if (i == 2)
                                {
                                    symbol = "d-";
                                    symDesc = "Decode issue message";
                                    symCol = colIssueMsg;
                                }

                                /// for decode success
                                if (i == 3)
                                {
                                    symbol = "d+";
                                    symDesc = "Decode result message";
                                    symCol = colResultMsg;
                                }
                            }
                            /// explains 'loose' contents
                            if (i == 4)
                            {
                                symbol = looseContentIndicator;
                                symDesc = "Loose Content (not yet connected to base content)";
                                symCol = colReviewLine;
                            }


                            if (symbol.IsNotNE())
                            {
                                Text(Ind14);
                                Format(symbol.PadRight(6), symCol);
                                FormatLine(symDesc, ForECol.Normal);
                            }
                        }
                    }
                }
            }
        }

    }
}
