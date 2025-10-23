using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using System.Collections.Generic;
using System;

namespace HCResourceLibraryApp.Layout
{
    public static class LogSubmissionPage
    {
        static readonly char subMenuUnderline = '*';
        static string pathToVersionLog = null;
        const string logStateParent = "Log Submission";

        public static void OpenPage(ResLibrary mainLibrary)
        {
            bool exitLogSubMain = false;
            do
            {
                BugIdeaPage.OpenPage();
                
                Program.LogState(logStateParent);
                Clear();

                Title("Version Log Submission", cTHB, 1);
                FormatLine($"{Ind24}Facilitates the submission of a version log to store content information regarding the resource pack.", ForECol.Accent);
                NewLine(2);
                Program.DisplayCurrentProfile();

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
            if (mainLibrary.ChangesMade() || LogDecoder.ChangesMade())
                Program.SaveData(false);
        }

        static void SubPage_SubmitLog(ResLibrary mainLibrary)
        {
            bool exitSubmissionPage = false;
            do
            {
                BugIdeaPage.OpenPage();

                /** Stages of verion log submission
                        - Provide path to version log (log location)
                        - Original version log review (raw)
                        - Processed version log review (decoded)
                 */

                string input = LastInput;
                Program.LogState(logStateParent + "|Submit A Log");

                // initial introductory section  // skipped if library has at least 10 contents (easy pass)
                if (!mainLibrary.Contents.HasElements(10))
                {
                    Clear();
                    Title("Submit a Version Log", subMenuUnderline, 1);
                    FormatLine($"{Ind14}There are a few stages to submitting a version log:", ForECol.Normal);
                    List(OrderType.Ordered_Numeric, "Provide file path to log,Original log review (raw),Processed log review (decoded)".Split(','));
                    NewLine();
                    Format($"{Ind24}Enter any key to continue to log submission >> ", ForECol.Normal);
                    input = StyledInput(null);
                }

                // log submission section
                if (input.IsNotNEW())
                {
                    LogDecoder logDecoder = new();
                    bool stopSubmission = false, expandedDisplayQ = false;
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
                        string logStatePhase = $"{logStateParent}|Submit A Log|Phase.{stageNum}, {stageName}";
                        Program.LogState(logStatePhase);
                        Title("Log Submission", subMenuUnderline, 2);
                        Important($"STAGE {stageNum}: {stageName}", subMenuUnderline);
                        HorizontalRule(minorChar, 1);
                        if (stageNum > 1)
                        {
                            FormatLine($"Below sourced from :: \n{Ind14}{pathToVersionLog}", ForECol.Accent);
                            NewLine();
                        }

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
                            placeHolder += $"  -or-  {openFileChooserPhrase}";
                            FormatLine("Please enter the file path to the version log being submitted below. The file should be of type 'text file' (.txt) or any similar text-only file type.", ForECol.Normal);
                            Format($"{Ind14}Path >> ", ForECol.Normal);

                            /// enter file path OR browse file path
                            ToggleFileChooserPage(true);
                            string inputPath = StyledInput(placeHolder);
                            FileChooserPage.ItemType = FileChooserType.Files;
                            FileChooserPage.OpenPage(LogDecoder.RecentDirectory);
                            ToggleFileChooserPage(false);
                            if (inputPath == openFileChooserPhrase)
                                inputPath = FileChooserPage.SelectedItem;

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
                                    if (inputPath.Replace(":\\", "").Contains("\\"))
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
                            else IncorrectionMessageTrigger($"{Ind24}Invalid file path entered:\n{Ind34}", null);
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
                                    // display file info
                                    bool omitBlock = false;
                                    for (int lx = 0; lx < logLines.Length; lx++)
                                    {
                                        string line = logLines[lx];
                                        bool sectionDetect_legacy = line.StartsWith("[") && line.EndsWith("]");
                                        bool sectionDetect_latest = line.IsNotNEW() && line.ToUpper() == line && LogDecoder.RemoveNumbers(line.Replace(".", "")) == line;

                                        /// omit line detect
                                        bool omit = line.StartsWith(LogDecoder.omit);
                                        bool invalidLine = line.Contains(DataHandlerBase.Sep);
                                        /// omit block detect
                                        if (line.StartsWith(LogDecoder.omitBlockOpen))
                                            omitBlock = true;
                                        if (line.StartsWith(LogDecoder.omitBlockClose))
                                            omitBlock = false;
                                        /// section detect
                                        if ((sectionDetect_latest || sectionDetect_legacy) && lx != 0 && !omitBlock && !omit)
                                            NewLine();

                                        // print!
                                        FormatLine(line, invalidLine ? ForECol.Incorrection : (omit || omitBlock ? ForECol.Normal : ForECol.Highlight));
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
                                mainLibrary.GetVersionRange(out VerNum earliestLibVer, out VerNum latestLibVer);

                                SetFileLocation(pathToVersionLog);
                                if (FileRead(null, out string[] fileData))
                                    logDecoder.DecodeLogInfo(fileData, earliestLibVer, latestLibVer);
                                else DataReadingIssue();
                            }

                            if (logDecoder.HasDecoded)
                            {
                                // ++ DECODED LOG DISPLAY ++ //
                                DisplayLogInfo(logDecoder, expandedDisplayQ);

                                NewLine(3);
                                HorizontalRule(minorChar, 1);
                                FormatLine("Plese review the contents retrieved from the log decoder above before proceeding.", ForECol.Highlight);
                                Format("Press [Enter] to toggle display style, and enter any key to continue >> ", ForECol.Normal);
                                input = StyledInput(null);

                                /// IF no input: view different displays; ELSE confirm info and integrate to library
                                if (input.IsNE())
                                {
                                    unallowStagePass = true;
                                    expandedDisplayQ = !expandedDisplayQ;
                                }
                                else
                                {
                                    Program.LogState(logStatePhase + "|Integration Evaluation");

                                    bool notNormalIntegrationQ = false, overwriteNetChangeQ = false;
                                    NewLine(3);
                                    Title("Content Integration Evaluation", subMenuUnderline, 0);
                                    Format($"{Ind14}Generating outcome of library integration. This may take a moment...", ForECol.Accent);
                                    ResLibrary tangentLibrary = new ResLibrary();
                                    if (mainLibrary.IsSetup())
                                        tangentLibrary = mainLibrary.CloneLibrary();
                                    NewLine();

                                    if (tangentLibrary != null)
                                    { /// wrapping
                                      /// IF; integrate overwrite - evaluation
                                        if (logDecoder.OverwriteWarning)
                                        {
                                            tangentLibrary.Overwrite(logDecoder.DecodedLibrary, out ResLibOverwriteInfo[] resLibOverInfoDock, out ResLibIntegrationInfo[] looseIntegrationInfoDock);

                                            /// report of overwriting contents
                                            if (resLibOverInfoDock.HasElements())
                                            {
                                                notNormalIntegrationQ = true;
                                                NewLine();
                                                Title("Overwriting Contents Outcome");
                                                DisplayOverwritingInfo(resLibOverInfoDock);

                                                /// report of loosened contents due to overwriting
                                                if (looseIntegrationInfoDock.HasElements())
                                                {
                                                    NewLine(2);
                                                    Title("Integrating Loosened Contents Outcome");
                                                    DisplayLooseIntegrationInfo(looseIntegrationInfoDock);
                                                }
                                                NewLine(2);

                                                overwriteNetChangeQ = !tangentLibrary.Equals(mainLibrary);
                                            }
                                        }
                                        /// ELSE; intergrate normal and loose - evaluation
                                        else
                                        {
                                            tangentLibrary.Integrate(logDecoder.DecodedLibrary, out ResLibAddInfo[] resLibAddInfoDock, out ResLibIntegrationInfo[] resLibIntInfoDock);


                                            /// report of normal content integration
                                            if (resLibAddInfoDock.HasElements())
                                            {
                                                NewLine();
                                                Title("Integration Outcome");
                                                DisplayIntegrationInfo(resLibAddInfoDock);
                                                NewLine();
                                            }
                                            /// report of connections for loose contents
                                            if (resLibIntInfoDock.HasElements())
                                            {
                                                notNormalIntegrationQ = true;
                                                NewLine();
                                                Title("Loose Contents Connection Outcome");
                                                DisplayLooseIntegrationInfo(resLibIntInfoDock);
                                                NewLine();
                                            }
                                            NewLine();
                                        }
                                    }

                                    // user validation of content integration
                                    /// IF loose content integration -or- overwrite integration: integration requires confirmation; ELSE integration automatic
                                    if (notNormalIntegrationQ)
                                    {
                                        Format($"{Ind14}Evaluation complete. Note: ", ForECol.Accent);
                                        FormatLine($"{(logDecoder.OverwriteWarning ? "Overwriting contents may require a restart." : "Integrating contents does not require a restart.")}", ForECol.Accent);
                                        FormatLine($"{Ind14}The section above estimates the outcome of {(logDecoder.OverwriteWarning ? "overwrit" : "integrat")}ing the decoded contents.", ForECol.Warning);
                                        Confirmation($"{Ind14}Confirm {(logDecoder.OverwriteWarning ? "overwriting with" : "integration of")} decoded contents? ", true, out bool yesNo);

                                        if (!yesNo)
                                        {
                                            NewLine();
                                            FormatLine($"{Ind14}By cancelling integration / overwriting of new contents, log submission will end.", ForECol.Accent);
                                            Confirmation($"{Ind14}Are you sure you wish to cancel this process? ", true, out bool yesNoProcess);

                                            if (yesNoProcess)
                                            {
                                                Program.LogState(logStatePhase + "|Overwritting / Loose Integration Cancelled");
                                                stopSubmission = true;
                                                pathToVersionLog = null;
                                            }
                                            else unallowStagePass = true;
                                        }

                                        if (yesNo)
                                        {
                                            Program.LogState(logStatePhase + "|Overwrite / Loose Integration Confirmed");

                                            /// OVERWRITE integration
                                            if (logDecoder.OverwriteWarning)
                                                mainLibrary.Overwrite(logDecoder.DecodedLibrary, out _, out _);
                                            /// LOOSE CONTENT integration
                                            else mainLibrary.Integrate(logDecoder.DecodedLibrary, out _, out _);
                                            exitSubmissionPage = true;
                                        }

                                        if (!unallowStagePass)
                                        {
                                            ConfirmationResult(yesNo, $"{Ind24}", $"Contents {(logDecoder.OverwriteWarning ? "overwritten " : "integrated in")}to library.", $"Content {(logDecoder.OverwriteWarning ? "overwriting" : "integration")} cancelled.");

                                            if (logDecoder.OverwriteWarning && yesNo)
                                            {
                                                NewLine();
                                                FormatLine($"{Ind24}The library has been overwritten and {(overwriteNetChangeQ ? "a program restart is required" : "remains unchanged")}.", ForECol.Warning);
                                                Format($"{Ind24}{(overwriteNetChangeQ ? "Proceed to restarting the program >> " : "A program restart is not required. Press [Enter] to continue >> ")}");
                                                Pause();

                                                if (overwriteNetChangeQ)
                                                    Program.RequireRestart();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Program.LogState(logStatePhase + "|Auto-Integration");
                                        FormatLine($"{Ind24}Evaluation complete. No additional steps required.");

                                        mainLibrary.Integrate(logDecoder.DecodedLibrary, out _, out _);
                                        Format($"{Ind24}Integrated new contents into library.", ForECol.Correction);
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
                                    FormatLine($"{Ind24}Hint: {possibleReason}", ForECol.Incorrection);
                                    Format($"{Ind34}Also ensure all sections are separated from each other.", ForECol.Incorrection);
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
            while (!exitSubmissionPage && !Program.AllowProgramRestart);

        }
        

        // public, so i may test it
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
                        bool isUsingLegacyDecodeQ = logDecoder.LegacyDecoding;
                        const int sectionNewLines = 1;
                        const string looseContentIndicator = "{l~}";
                        const string crossRefKey = "%%";
                        DecodedSection[] sections = (DecodedSection[])typeof(DecodedSection).GetEnumValues();
                        foreach (DecodedSection section in sections)
                        {
                            List<string> reviewTexts = new();
                            string sectionName = section.ToString();

                            // need to find a way to display both legacy and current without breaking either
                            if (!isUsingLegacyDecodeQ)
                            {
                                if (section == DecodedSection.TTA)
                                    reviewTexts.Add(sectionName);
                                else reviewTexts.Add(LogDecoder.FixContentName(sectionName, false).ToUpper());
                            }
                            else
                            {
                                /// legacy does its own thing for: version, tta  (main decoding DNE in legacy)
                                if (section != DecodedSection.Version && section != DecodedSection.TTA && section != DecodedSection.MainDecoding)
                                    reviewTexts.Add(LogDecoder.FixContentName(sectionName).ToUpper());
                            }


                            // get text to print
                            /// MAIN DECODING
                            if (section == DecodedSection.MainDecoding)
                            {
                                /// Legacy does not have a "Main Decoding" section, so manually (and always) recommend using newer syntax
                                if (isUsingLegacyDecodeQ)
                                {
                                    reviewTexts.Add(LogDecoder.FixContentName(sectionName).ToUpper());
                                }
                            }

                            /// VERSION 
                            if (section == DecodedSection.Version)
                            {
                                /** VER FORMAT
                                    Version Number: 1.02
                                 */
                                if (isUsingLegacyDecodeQ)
                                    reviewTexts.Add($"Version Number: {decLibrary.Summaries[0].SummaryVersion.ToStringNums()}");
                                else reviewTexts.Add(decLibrary.Summaries[0].SummaryVersion.ToStringNums() 
                                    + crossRefKey + decLibrary.Summaries[0].SummaryVersion.ToString());
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
                                /// getting added info
                                int addedItemNum = 1;
                                for (int aix = 0; aix < decLibrary.Contents.Count; aix++)
                                {
                                    ResContents resCon = decLibrary.Contents[aix];
                                    if (resCon.IsSetup())
                                        if (resCon.ContentName != ResLibrary.LooseResConName)
                                        {
                                            if (isUsingLegacyDecodeQ)
                                                reviewTexts.Add($"#{addedItemNum} {resCon.ContentName} {Ind14}{resCon.ConBase.DataIDString}");
                                            else reviewTexts.Add($"#{addedItemNum} {resCon.ContentName} {Ind14}{resCon.ConBase.DataIDString}" 
                                                + crossRefKey + resCon.ConBase.ToString());
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
                                foreach (ResContents resCon in decLibrary.Contents)
                                {
                                    if (resCon.IsSetup() && resCon.ContentName != ResLibrary.LooseResConName)
                                    {
                                        if (resCon.ConAddits.HasElements())
                                            foreach (ContentAdditionals rcConAdt in resCon.ConAddits)
                                            {
                                                string partRt = $"> {(rcConAdt.OptionalName.IsNotNEW() ? $"{rcConAdt.OptionalName} " : "")}";

                                                if (isUsingLegacyDecodeQ)
                                                    reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - {resCon.ContentName} ({rcConAdt.RelatedDataID})");
                                                else reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - {resCon.ContentName} ({rcConAdt.RelatedDataID})"
                                                    + crossRefKey + rcConAdt.ToString());
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

                                            if (isUsingLegacyDecodeQ)
                                                reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - ({rcConAdt.RelatedDataID}) {looseContentIndicator}");
                                            else reviewTexts.Add($"{partRt}({rcConAdt.DataIDString}) - ({rcConAdt.RelatedDataID}) {looseContentIndicator}"
                                                + crossRefKey + rcConAdt.ToString());
                                        }
                                }
                                /// there, I made legacy even more unstable; I removed it, get with the change...
                            }
                            /// TTA
                            if (section == DecodedSection.TTA)
                            {
                                /** TTA FORMAT
                                    TTA: 15
                                 */
                                                                
                                SummaryData summary = decLibrary.Summaries[0];
                                if (isUsingLegacyDecodeQ)
                                    reviewTexts.Add($"TTA: {summary.TTANum}");
                                else reviewTexts.Add(summary.TTANum.ToString() + crossRefKey + summary.TTANum.ToString());
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
                                //reviewTexts.Add(sectionName.ToUpper());
                                ResContents looseResCon = decLibrary.Contents[0];
                                /// there is also a copy of self-connected updated infos in the looseResCon CC list
                                if (looseResCon.ContentName == ResLibrary.LooseResConName)
                                {
                                    if (looseResCon.ConChanges.HasElements())
                                        foreach (ContentChanges rcConChg in looseResCon.ConChanges)
                                        {
                                            string clean_intName = rcConChg.InternalName;
                                            if (clean_intName.IsNotNEW())
                                                clean_intName = rcConChg.InternalName.Replace(DataHandlerBase.Sep, "");
                                            ContentChanges rcConChg_Clean = new(rcConChg.VersionChanged, clean_intName, rcConChg.RelatedDataID, rcConChg.ChangeDesc);

                                            string rtPart1 = $"> {(rcConChg.InternalName.IsNotNEW() ? $"{rcConChg.InternalName}{Ind24}" : "")}{rcConChg.RelatedDataID}";
                                            string rtPart2 = $"{Ind24}{rcConChg.ChangeDesc}";
                                            string rtPart3 = $" {looseContentIndicator}";
                                            if (rcConChg.InternalName.IsNotNE())
                                                if (rcConChg.InternalName.Contains(DataHandlerBase.Sep))
                                                {
                                                    rtPart1 = rtPart1.Replace(DataHandlerBase.Sep, "");
                                                    rtPart3 = "";
                                                }       
                                            

                                            if (isUsingLegacyDecodeQ)
                                                reviewTexts.Add($"{rtPart1}\n{rtPart2}{rtPart3}");
                                            else reviewTexts.Add($"{rtPart1}\n{rtPart2}{rtPart3}" + crossRefKey + rcConChg_Clean.ToString());

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

                                //reviewTexts.Add(sectionName.ToUpper());
                                if (decLibrary.Legends.HasElements())
                                {
                                    foreach (LegendData legDat in decLibrary.Legends)
                                    {
                                        string legDefs = "";
                                        if (legDat.CountDefinitions > 0)
                                            for (int i = 0; i < legDat.CountDefinitions; i++)
                                                legDefs += $"{legDat[i]}" + (i + 1 < legDat.CountDefinitions ? "/" : "");
                                        
                                        if (isUsingLegacyDecodeQ)
                                            reviewTexts.Add($"{legDat.Key}/{legDefs}");
                                        else reviewTexts.Add($"{legDat.Key}/{legDefs}" + crossRefKey + legDat.ToString());
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
                                //reviewTexts.Add(sectionName.ToUpper());
                                SummaryData summary = decLibrary.Summaries[0];
                                if (summary.IsSetup())
                                {
                                    foreach (string sumPart in summary.SummaryParts)
                                    {
                                        if (isUsingLegacyDecodeQ)
                                            reviewTexts.Add($"- {sumPart}");
                                        else reviewTexts.Add($"- {sumPart}" + crossRefKey + sumPart);
                                    }
                                }
                            }

                            
                            // text printing
                            if (reviewTexts.HasElements())
                            {
                                /// text printing
                                bool bypassLoopMax = showDecodeInfosQ == true, warnedOfLegacyQ = false;
                                int rtxOffset = 0; //, rtxCRefOffset = 0;
                                for (int rtx = 0; rtx < reviewTexts.Count || bypassLoopMax; rtx++)
                                {
                                    int issueCount;
                                    DecodeInfo decodeInfo;
                                    bool isReplacementLineQ = false;
                                    if (isUsingLegacyDecodeQ)
                                        decodeInfo = logDecoder.GetDecodeInfo(section, rtx, out issueCount);
                                    else
                                    {
                                        /// All results except for ADDED:replacements will have a cross reference. 
                                        /// - If it is not a replacement line, then the IF statement will find the right decode info
                                        /// - Otherwise (is a replacement line), the IF is skipped (goes to the single-line ELSE)

                                        decodeInfo = logDecoder.GetDecodeInfo(section, rtx, out issueCount);
                                        if (decodeInfo.logLine.IsNotNEW())
                                            isReplacementLineQ = section == DecodedSection.Added && decodeInfo.logLine.Trim().StartsWith(LogDecoder.Keyword_Replace);

                                        if (!isReplacementLineQ || !bypassLoopMax)
                                        {
                                            if (rtx - rtxOffset < reviewTexts.Count)
                                            {
                                                if (reviewTexts[rtx - rtxOffset].Contains(crossRefKey))
                                                {
                                                    string[] review_x_crossRef = reviewTexts[rtx - rtxOffset].Split(crossRefKey);
                                                    decodeInfo = logDecoder.GetDecodeInfo(section, review_x_crossRef[1], out issueCount);
                                                    reviewTexts[rtx - rtxOffset] = review_x_crossRef[0];
                                                }
                                            }
                                        }
                                        //if (!isReplacementLineQ)
                                        //{
                                            
                                        //}
                                        //else rtxCRefOffset += 1;
                                    }

                                    /// legacy decoding issue, manual entry
                                    if (isUsingLegacyDecodeQ && !warnedOfLegacyQ && section == DecodedSection.MainDecoding)
                                    {
                                        decodeInfo = new("n/a", DecodedSection.MainDecoding.ToString());
                                        decodeInfo.decodeIssue = "Using the legacy logging syntax is not recommended.";
                                        issueCount = 1;

                                        warnedOfLegacyQ = true;
                                    }

                                    /// special no-review-print cases (not legacy)
                                    /// 1. Added: placehohlder/substitution replacements
                                    bool printNonReviewLineQ = false;
                                    if (decodeInfo.logLine.IsNotNEW() && !isUsingLegacyDecodeQ)
                                    { /// mostly just a wrapper

                                        // for Added: PH/SUB lines
                                        if (showDecodeInfosQ && isReplacementLineQ)
                                            printNonReviewLineQ = true;


                                        if (printNonReviewLineQ)
                                            rtxOffset += 1;
                                    }

                                    /// review line
                                    bool reviewLinePrintedQ = false;
                                    if (rtx - rtxOffset < reviewTexts.Count && !printNonReviewLineQ)
                                    {
                                        string rtext = reviewTexts[rtx - rtxOffset].ToString();
                                        Format(rtext, colReviewLine);
                                        reviewLinePrintedQ = true;
                                    }

                                    /// section issues count
                                    if (rtx == 0)
                                        Format($" {{!{issueCount}}}", colIssueNumber);
                                    //if (!printNonReviewLineQ) 
                                    NewLine();

                                    if (showDecodeInfosQ)
                                    {
                                        if (decodeInfo.IsSetup())
                                        {
                                            /// source line
                                            FormatLine($"{(reviewLinePrintedQ ? Ind14 : "")}{decodeInfo.logLine.Replace("\n", $"\n{Ind14}")}", colSourceLine);
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
                                if (section != sections[^1])
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
        static void DisplayIntegrationInfo(ResLibAddInfo[] integrationInfoDock)
        {
            /** ADDED CONTENTS REPORT - Display Concept
                What do I Have? 
                    Within each ResLibAddInfo instance
                    - the added object,
                    - an added status (true/false),
                    - a duplicate rejection status (true/false),
                    - a source type and sub-source type,
                    - extra information, 
                    - loose content indicator (true/false),


                Visualize: 
                ........................................
                Form:   ## [{Source}] {Result}: {Object} {'*' LooseIndicator}
                            {Extra}

                {##}     : Object number, increments from 1, applies to 3 main sources only
                    - Accent FG Color
                {Result} : Either 'Added' or 'Rejected'
                    - Correction FG Color on 'Added', Incorrection FG Color on 'Rejected'
                {Source} : The type of instance added 
                    - Normal FG Color
                {Object} : The content, legend, or summary that has been added or rejected
                    - Highlight FG Color
                {*}      : Determines the object as not yet being connected to a base content
                    - Normal FG Color
                {Extra}  : Displays additional information for adding an object. Includes duplicate message and other misc messages where available
                    - Accent FG Color


                Examples: 
                .......................................
                1  [Content] Added: 1.00;Lettuce; [1] adts
                        [Content:Bse] Added: v1.00;Lettuce;i9,t16
                        [Content:Adt] Added: v1.00;t16;LeafDust;d3
                2  [Content] Rejected: 1.00;Urnger;
                      Is Duplicate
                3  [Legend] Rejected: 1.00;d;Particles
                      Is Duplicate; partial duplicate                        

             */

            Dbg.SingleLog("LogSubmissionPage.DisplayIntegrationInfo()", "(Method called)");
            if (integrationInfoDock.HasElements())
            {
                SourceOverwrite prevSource = SourceOverwrite.Content;
                int subSourceBacksetIx = 0, countPrints = 0;
                for (int ax = 0; ax < integrationInfoDock.Length; ax++)
                {
                    ResLibAddInfo rlai = integrationInfoDock[ax];
                    if (rlai.IsSetup())
                    {                        
                        ForECol colObj = ForECol.Highlight, colAdd = ForECol.Correction, colRej = ForECol.Incorrection, colExt = ForECol.Accent;
                        string source, addedObject, extraCompile;

                        /// determine source
                        source = rlai.source.ToString();
                        /// determine added object string
                        if (rlai.addedObject.Contains("(RC)"))
                            addedObject = rlai.addedObject.Replace("(RC)", "").Trim();
                        else addedObject = rlai.addedObject;
                        /// determine extra text
                        extraCompile = rlai.dupeRejectionQ ? "Is Duplicate" : "";
                        if (rlai.extraInfo.IsNotNEW())
                            extraCompile += (extraCompile.IsNotNEW() ? "; " : "") + rlai.extraInfo;


                        // DISPLAY content add info
                        bool subSourceQ = rlai.subSource.HasValue;
                        subSourceBacksetIx += subSourceQ ? 1 : 0;
                        if (!subSourceQ)
                        {
                            /// separation between categories (sources) : content, legend, summary
                            if (prevSource != rlai.source)
                                NewLine();
                            /// item number and indentation
                            Format($"{Ind14}{ax + 1 - subSourceBacksetIx, -2} ", ForECol.Accent);
                            HoldWrapIndent(true);
                            /// information formating
                            Format($"[{source}] ");
                            Format((rlai.addedQ ? "Added" : "Rejected").ToUpper(), rlai.addedQ ? colAdd : colRej);
                            Format(": ");
                            FormatLine(addedObject, colObj);
                            if (extraCompile.IsNotNEW())
                                FormatLine($"{(subSourceQ ? $"\t{Ind14}" : "\t")}{extraCompile}", colExt);
                            HoldWrapIndent(false);
                        }                       


                        // DISPLAY legend
                        if (ax + 1 >= integrationInfoDock.Length)
                        {
                            NewLine();
                            Title("Integration Legend");
                            /// content add display legend
                            FormatLine("[ContentSource] {Result}: {ContentObject}");
                            FormatLine($"{Ind24}{{Extra Info}}");

                            /// content format legend
                            FormatLine("- - - - - - -", ForECol.Accent);
                            string[,] itemSyntaxes = new string[3, 2]
                            {
                                /// (RC) #0; One Item, 1.00, [1] adts, [2] upds
                                {$"{SourceOverwrite.Content}", "#{ShelfNo}; {ContentName}, {VerAdded}, {NumOfAdditionals}, {NumOfUpdates}"},
                                /// {VersionAdded}*{ContentName}*{RelatedDataIDs}
                                //{$"{SourceOverwrite.Content}:{SourceCategory.Bse}", "{VerAdded};{ContentName};{DataIDs,,}"},
                                /// {VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
                                //{$"{SourceOverwrite.Content}:{SourceCategory.Adt}", "{VerAdded};{RelatedDataID};{Name.Optional};{DataIDs,,}"},
                                /// {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
                                //{$"{SourceOverwrite.Content}:{SourceCategory.Upd}", "{VerUpdated};{Name};{RelatedDataID};{ChangeDesc}"},
                                /// {key}*{verIntro}*{keynames}
                                {$"{SourceOverwrite.Legend}", "{Key};{VerAdded};{Definitions;;}"},
                                /// {Version}*{tta}*{summaryParts}
                                {$"{SourceOverwrite.Summary}", "{VerNum};{ContentTally};{SummaryParts;;}"},
                            };
                            for (int c = 0; c < itemSyntaxes.GetLength(0); c++)
                            {
                                Format($"{itemSyntaxes[c, 0],-14}", ForECol.Normal);
                                FormatLine($"| {itemSyntaxes[c, 1]}", ForECol.Accent);
                            }
                        }

                        prevSource = rlai.source;
                        countPrints++;
                    }
                }
            }
        }
        static void DisplayLooseIntegrationInfo(ResLibIntegrationInfo[] integrationInfoDock)
        {
            Dbg.SingleLog("LogSubmissionPage.DisplayLooseIntegrationInfo()", "(Method called)");
            if (integrationInfoDock.HasElements())
            {
                /** LOOSE CONTENTS CONNECTION REPORT - Display concept
                    What do i want to know?
                    - The loose content {LC}
                    - The loose content type (CA or CC?)  {LCType}
                    - Is connected or discarded? If connected, connected to what? {Result} {Connection}
                                                
                    Visualize:
                    ........................
                    Form: {Result} {LCType}: '{LC}' {Connection}.
                    - Base Foreground color: Normal

                    {Result}    -> ## {Discarded / Connected}
                    - Foreground colors: incorrection, correction ('#' only?)

                    {LCType}    -> Additional content / Updated content
                    {LC}        :: '{CA / CC details}'
                        Adt     -> {Opt.Name} {DataIDs} 
                        Upd     -> {DataID} {Desc.Short}
                    - Foreground colors: Highlight

                    {Connection}-> to '{BaseContent}' by '{RelatedID}' / [null]
                    - Foreground colors: Normal


                    EXAMPLES
                    .................
                Ex1
                    ## Discarded additional content: 'Ben's Buckle; BensBeltBuckle'.
                    ## Connected additional content: 'Flyer Trails; y33 y42' to 'Flyer' by 's45'.
				    ## Discarded additional content: 'y72` y73'.
                    ## Discarded updated content: 'r356; Redesigned to fit bett...'.
                    ## Connected updated content: 's44; Improved brightness of...' to 'Bald Cap' by 's44'.

                Ex2
                    ## Discarded additional content: 
                        'Ben's Buckle; BensBeltBuckle'.
                    ## Connected additional content: 
                        'Flyer Trails; y33 y42' to 'Flyer' by 's45'.
				    ## Discarded additional content:
					    'y72` y73'.
                    ## Discarded updated content: 
                        'r356; Redesigned to fit bett...'.
                    ## Connected updated content: 
                        's44; Improved brightness of...' to 'Bald Cap' by 's44'.
                    */

                float factor = WSLL(4, 10) / 5f; /// Range [80%, 200%]
                int updtDescLim = (int)(25 * factor), adtIDsLim = (int)(15 * factor), adtNameLim = (int)(20 * factor);
                foreach (ResLibIntegrationInfo rlii in integrationInfoDock)
                {
                    if (rlii.IsSetup())
                    {
                        /** Snippet from Loose Content Connection Report
                            EXAMPLES
                            .................
                            Ex1
                                ## Discarded additional content: 'Ben's Buckle; BensBeltBuckle'.
                                ## Connected additional content: 'Flyer Trails; y33 y42' to 'Flyer' by 's45'.
				                ## Discarded additional content: 'y72` y73'.
                                ## Discarded updated content: 'r356; Redesigned to fit bett...'.
                                ## Connected updated content: 's44; Improved brightness of...' to 'Bald Cap' by 's44'.

                            Ex2
                                ## Discarded additional content: 
                                    'Ben's Buckle; BensBeltBuckle'.
                                ## Connected additional content: 
                                    'Flyer Trails; y33 y42' to 'Flyer' by 's45'.
				                ## Discarded additional content:
					                'y72` y73'.
                                ## Discarded updated content: 
                                    'r356; Redesigned to fit bett...'.
                                ## Connected updated content: 
                                    's44; Improved brightness of...' to 'Bald Cap' by 's44'.

                            
                            Ex3 'Compact Ex1 alt'
                                ## Discarded additional: 'Ben's Buckle; BensBeltBuckle'.
                                ## Connected additional: 'Flyer Trails; y33 y42' to 'Flyer' by 's45'.
                                ## Discarded updated: 'r356; Redesigned to fit bett...'.
                                ## Connected updated: 's44; Improved brightness of...' to 'Bald Cap' by 's44'.
                            
                         */

                        bool compactFormQ = HSNL(0, 5) < 3 || WSLL(0, 5) >= 3;
                        bool isAdtq = rlii.infoType == RCFetchSource.ConAdditionals;

                        Format("## ", rlii.isConnectedQ ? ForECol.Correction : ForECol.Incorrection);
                        Format($"{(rlii.isConnectedQ ? "Connected" : "Discarded")} {(isAdtq ? "additional" : "updated")}{(compactFormQ ? "" : " content")}: ", ForECol.Normal);
                        if (!compactFormQ)
                        {
                            NewLine();
                            Format($"{Ind34}'");
                        }
                        else Format("'");


                        if (isAdtq)
                            Format($"{(rlii.adtOptName.IsNE() ? "" : $"{rlii.adtOptName.Clamp(adtNameLim, "...")}; ")}{rlii.adtDataIDs.Clamp(adtIDsLim, "...")}", ForECol.Highlight);
                        else Format($"{rlii.updDataID}; {rlii.updShortDesc.Clamp((!rlii.isConnectedQ && !compactFormQ ? (int)(updtDescLim * 1.25f) : updtDescLim), "...")}", ForECol.Highlight);

                        if (rlii.isConnectedQ)
                            Format($"' to '{rlii.connectionName}' by '{rlii.connectionDataID}");                    
                        FormatLine("'.");
                    }
                }
            }
        }
        static void DisplayOverwritingInfo(ResLibOverwriteInfo[] overwritingInfoDock)
        {
            Dbg.SingleLog("LogSubmissionPage.DisplayOverwritingInfo()", "(Method called)");
            if (overwritingInfoDock.HasElements())
            {
                /** OVERWRITTEN CONTENTS REPORT - Display Concept
                    What do I have?
                        Within each ResLibOverwriteInfo instance 
                        - the (previously) existing content, 
                        - the (new) ovewriting content, 
                        - an overwritten status (true/false), 
                        - the final form of the content in question (resulting)
                        - and ignore overwriting status (to say if ovewritting and object has been bypassed) [currently unused], 
                        - the information source: contents (base, addit, update), legends, summaries

                    What do I want to know?
                        - The possibly overwritten instance in question
                        - If an instance has been overwritten (edited, with 'overwrittenStatus'), added (no existing), or removed (no overwriting), and where this is situated (information source)
                        - If this particular ovewrite instance has been igrnored [currently unused]


                    Visualize:
                    .............................
                    Form: [{Source}] {OverwriteSymbol} {Result} 
                            {Replaced} 

                    {Source} : Where this overwriting instance occurs
                        - Normal Foreground Color

                    {OvewriteSymbol} : Signifies a change to the existing contents and reflects overwriting status and ignorance
                        = No changes - Highlight ForECol
                        + Overwritten / Added - Correction ForECol
                        - Removed - Incorrection ForECol
                        ~ Ignored - Normal ForECol

                    {Result} : The final result of this overwrite. If there has been an overwrite, the overwriting or resulting will be displayed. Otherwise, the existing or resulting.
                        - Colors reflect that of the Overwrite Symbol

                    {Replaced} Displays for an overwritten or ignored instance, showing the existing or overwriting instance that was replaced or ignored due to the final result
                        - Accent Foreground Color
                    .............................

                    
                    EXAMPLES
                    .........................
                Ex1
                    [Contents:Bse] + Ben's Buckle; BensShoeBuckle
                        Ben's Buckle; BensBeltBuckle
                    [Legend] = s;Sizzle
                    [Contents:Adt] + y32 y44; Flyer Trails; s45; 
                    [Legend] * y;Yuck
                        y;Yucky
                    .........................
                 */

                SourceOverwrite prevSource = SourceOverwrite.Content;
                int subSourceBacksetIx = 0, countPrints = 0;
                for (int rx = 0; rx < overwritingInfoDock.Length; rx++)
                {
                    ResLibOverwriteInfo rloi = overwritingInfoDock[rx];
                    if (rloi.IsSetup())
                    {
                        const string symEql = "=", symEdt = "+", symIgn = "~", symRem = "-", symLos = "*";
                        ForECol colEqual = ForECol.Highlight, colEdit = ForECol.Correction, colIgn = ForECol.Normal, colRem = ForECol.Incorrection;
                        string overwriteSym, source, overwriteResult, overwriteReplaced = "";
                        ForECol overwriteCol;

                        /// determine source
                        source = rloi.source.ToString();
                        if (rloi.subSource.HasValue)
                            source += $":{rloi.subSource.Value}";

                        /// overwriting sign, color, and resulting text
                        if (rloi.contentExisting.IsNotNEW())
                        {
                            if (rloi.contentOverwriting.IsNotNEW())
                            {
                                overwriteSym = rloi.OverwrittenQ ? symEdt : symEql;
                                overwriteCol = rloi.OverwrittenQ ? colEdit : colEqual;
                                if (rloi.OverwrittenQ)
                                {
                                    overwriteResult = rloi.contentResulting.IsNotNEW() ? rloi.contentResulting : rloi.contentOverwriting;
                                    overwriteReplaced = rloi.contentExisting;
                                }
                                else
                                    overwriteResult = rloi.contentExisting;
                            }
                            else
                            {
                                overwriteSym = rloi.OverwrittenQ ? symRem : symEql;
                                overwriteCol = rloi.OverwrittenQ ? colRem : colEqual;
                                overwriteResult = rloi.contentExisting;
                            }
                        }
                        else //if (rloi.contentOverwriting.IsNotNEW())
                        {
                            overwriteSym = rloi.OverwrittenQ ? symEdt : symEql;
                            overwriteCol = rloi.OverwrittenQ ? colEdit : colEqual;
                            overwriteResult = rloi.contentOverwriting;
                        }
                        
                        /// ignoring overwrite, specific 
                        if (rloi.ignoreOverwriteQ)
                        {
                            overwriteSym = symIgn;
                            overwriteCol = colIgn;
                        }

                        /// removing '(RC)' from the front of ResContents strings
                        if (overwriteReplaced.IsNotNEW())
                            overwriteReplaced = overwriteReplaced.Replace("(RC)", "").Trim();
                        if (overwriteResult.IsNotNEW())
                            overwriteResult = overwriteResult.Replace("(RC)", "").Trim();




                        // DISPLAY overwrite info                        
                        bool subSourceQ = source.Contains(":");
                        subSourceBacksetIx += subSourceQ ? 1 : 0;
                        /// indentation for loose contents (always at top)
                        if (subSourceQ && countPrints == 0)
                        {
                            Format($"{Ind14}--|", ForECol.Accent);
                            FormatLine("[Loose Contents]", ForECol.Normal);
                        }
                        /// separation between categories (sources) : content, legend, summary
                        if (prevSource != rloi.source)
                            NewLine();     
                        /// item number and indentation
                        if (!subSourceQ)
                            Format($"{Ind14}{rx + 1 - subSourceBacksetIx,-2}|", ForECol.Accent);
                        else Format($"\t");
                        HoldWrapIndent(true); 
                        /// information formatting
                        Format($"[{source}] ");
                        Format($"{overwriteSym} {overwriteResult}", overwriteCol);
                        if (rloi.looseContentQ)
                            Format($" {symLos}");
                        NewLine();
                        if (overwriteReplaced.IsNotNEW())
                            FormatLine($"{(subSourceQ ? $"\t{Ind14}" : Ind34)}{overwriteReplaced}", ForECol.Accent);
                        HoldWrapIndent(false);


                        // DISPLAY legend
                        if (rx + 1 >= overwritingInfoDock.Length)
                        {
                            NewLine();
                            Title("Overwriting Legend");
                            /// overwriting display legend
                            //FormatLine($"Outcome Symbols :: '{symEql}' No change  |  '{symEdt}' Overwritten / Added  |  '{symRem}' Removed  |  '{symIgn}' Ignored", ForECol.Accent);
                            FormatLine($"Outcome Symbols :: '{symEql}' No change  |  '{symEdt}' Overwritten / Added  |  '{symRem}' Removed", ForECol.Accent);
                            Format("[ContentSource] ");
                            FormatLine($"{{Outcome Symbol}} Resulting Outcome {{'{symLos}' Loosened Content Indicator}}");
                            FormatLine($"{Ind24}{{Discarded due to change}}");

                            /// content format legend
                            //NewLine();
                            //FormatLine("Item Syntax");
                            FormatLine("- - - - - - -", ForECol.Accent);
                            string[,] itemSyntaxes = new string[6,2]
                            {
                                /// (RC) #0; One Item, 1.00, [1] adts, [2] upds
                                {$"{SourceOverwrite.Content}", "#{ShelfNo}; {ContentName}, {VerAdded}, {NumOfAdditionals}, {NumOfUpdates}"},
                                /// {VersionAdded}*{ContentName}*{RelatedDataIDs}
                                {$"{SourceOverwrite.Content}:{SourceCategory.Bse}", "{VerAdded};{ContentName};{DataIDs,,}"},
                                /// {VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
                                {$"{SourceOverwrite.Content}:{SourceCategory.Adt}", "{VerAdded};{RelatedDataID};{Name.Optional};{DataIDs,,}"},
                                /// {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
                                {$"{SourceOverwrite.Content}:{SourceCategory.Upd}", "{VerUpdated};{Name};{RelatedDataID};{ChangeDesc}"},
                                /// {key}*{verIntro}*{keynames}
                                {$"{SourceOverwrite.Legend}", "{Key};{VerAdded};{Definitions;;}"},
                                /// {Version}*{tta}*{summaryParts}
                                {$"{SourceOverwrite.Summary}", "{VerNum};{ContentTally};{SummaryParts;;}"},
                            };
                            for (int c = 0; c < itemSyntaxes.GetLength(0); c++)
                            {
                                //HoldNextListOrTable();
                                //Table(Table2Division.KCSmall, itemSyntaxes[c, 0], ' ', itemSyntaxes[c, 1]);
                                Format($"{itemSyntaxes[c, 0],-14}", ForECol.Normal);
                                FormatLine($"| {itemSyntaxes[c, 1]}", ForECol.Accent);
                            }
                        }

                        prevSource = rloi.source;
                        countPrints++;
                    }
                }

            }
        }
        
    }
}
