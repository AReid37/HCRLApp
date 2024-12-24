using System;
using System.Collections.Generic;
using System.IO;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    public class BugIdeaData : DataHandlerBase
    {
        /** PLANNING
            Fields / Props
            - list[BII] bugIdeaInfo, prevBugIdeaInfo
            - rd str printFileName 

            Contructor
            - BID()

            Methods
            - bl IsSetup()
            - bl AddInfo(bl isBugQ, str description)
            - bl RemoveInfo(bl isBugQ, int index) [remove]
            - bl ChangesMade()
            - pv BID GetPreviousSelf()
            - pv vd SetPreviousSelf()
            - ovr bl EncodeToSharedFile()
            - ovr bl DecodeFromSharedFile()
            - bl PrintInfo()
         **/

        // FIELDS / PROPS
        public List<BugIdeaInfo> biInfo;
        List<BugIdeaInfo> prevBiInfo;
        const string PrintFileName = "hcrlaBugIdeaPrint.txt";


        // CONSTRUCTORS
        public BugIdeaData() : base()
        {
            commonFileTag = "dbi";
            biInfo = new List<BugIdeaInfo>();
            prevBiInfo = new List<BugIdeaInfo>();
        }


        // METHODS - general
        public bool AddInfo(bool isBugQ, string description)
        {
            bool addedQ = false;
            if (description.IsNotNEW())
            {
                if (biInfo != null)
                {
                    BugIdeaInfo bii = new BugIdeaInfo(isBugQ, description.Trim(), true);
                    biInfo.Add(bii);
                }
            }
            return addedQ;
        }
        /// <returns>A boolean stating whether the following has values: biInfo.</returns>
        public override bool IsSetup()
        {
            return biInfo.HasElements();
        }
        public override bool ChangesMade()
        {
            BugIdeaData prev = GetPreviousSelf();
            bool areEquals = true;
            for (int cx = 0; cx < 2 && areEquals; cx++)
            {
                switch (cx)
                {
                    case 0:
                        areEquals = IsSetup() == prev.IsSetup();
                        break;

                    case 1:
                        if (IsSetup())
                        {
                            areEquals = biInfo.Count == prev.biInfo.Count;
                            if (areEquals)
                            {
                                for (int ax = 0; ax < biInfo.Count && areEquals; ax++)
                                    areEquals = biInfo[ax].Equals(prev.biInfo[ax]);
                            }
                        }
                        break;
                }
            }
            return !areEquals;
        }
        void SetPreviousSelf()
        {
            if (biInfo.HasElements())
            {
                prevBiInfo = new List<BugIdeaInfo>();
                prevBiInfo.AddRange(biInfo.ToArray());
            }
        }
        BugIdeaData GetPreviousSelf()
        {
            BugIdeaData prevSelf = new();
            if (prevBiInfo.HasElements())
                prevSelf.biInfo.AddRange(prevBiInfo.ToArray());
            return prevSelf;
        }


        // METHODS - data handling
        protected override bool EncodeToSharedFile()
        {
            Dbg.StartLogging("BugIdeaData.EncodeToSharedFile()", out int bidx);
            bool noDataButOkayQ = !IsSetup(), encodedBugIdea = false;
            if (IsSetup())
            {
                Dbg.Log(bidx, $"Is setup; Proceeding to encoding '{biInfo.Count}' bug and idea entries; ");
                Dbg.NudgeIndent(bidx, true);
                List<string> biLines = new();
                for (int bx = 0; bx < biInfo.Count; bx++)
                {
                    Dbg.LogPart(bidx, IsSetup() ? "+ Enc" : "- OMIT ");
                    if (biInfo[bx].IsSetup())
                        biLines.Add(biInfo[bx].Encode());
                    Dbg.Log(bidx, $"  {biInfo[bx].Encode()}; ");
                }
                Dbg.NudgeIndent(bidx, false);

                if (biLines.HasElements())
                {
                    encodedBugIdea = Base.FileWrite(false, commonFileTag, biLines.ToArray());
                    if (encodedBugIdea)
                        Dbg.Log(bidx, "Encoding complete; Proceeding to second encoding state: PRINT INFO...");

                    bool printedEntriesQ = PrintInfo();
                    Dbg.Log(bidx, $"Printed Entries Result? {(printedEntriesQ ? "Complete / Success" : "Skipped / Failed")}  [Does not affect encoding outcome]; ");
                }

                SetPreviousSelf();
                Dbg.Log(bidx, $"Updated previous self to current; Confirmed? {!ChangesMade()}; ");
            }
            else Dbg.Log(bidx, "Not setup; No data to encode, successful encoding...");
            Dbg.EndLogging(bidx);
            return noDataButOkayQ || encodedBugIdea;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbg.StartLogging("BugIdeaData.DecodeFromSharedFile", out int bidx);
            bool decodedBugIdeaDataQ = Base.FileRead(commonFileTag, out string[] bugIdeaLines);
            Dbg.Log(bidx, $"Fetching file data (using tag '{commonFileTag}'); Successfully read from file? {decodedBugIdeaDataQ}; {nameof(bugIdeaLines)} has elements? {bugIdeaLines.HasElements()}");

            if (decodedBugIdeaDataQ && bugIdeaLines.HasElements())
            {
                Dbg.Log(bidx, "Preparing to decoded Bug Idea Data; ");

                Dbg.NudgeIndent(bidx, true);
                for (int lx = 0; lx < bugIdeaLines.Length && decodedBugIdeaDataQ; lx++)
                {
                    Dbg.LogPart(bidx, $"+ Decoded :: '{bugIdeaLines[lx]}'? ");
                    BugIdeaInfo newBii = new BugIdeaInfo();
                    bool decodedQ = newBii.Decode(bugIdeaLines[lx]);

                    if (decodedQ)
                    {
                        Dbg.LogPart(bidx, $"True{(newBii.isNewQ ? "; No longer 'New'" : "")}");
                        newBii.isNewQ = false;
                    }
                    else Dbg.LogPart(bidx, "FALSE");
                    Dbg.Log(bidx, "; ");

                    biInfo.Add(newBii);
                    decodedBugIdeaDataQ = decodedQ;
                }
                Dbg.NudgeIndent(bidx, false);

                SetPreviousSelf();
                Dbg.Log(bidx, $"Previous self has been set; Confirmed? {!ChangesMade()}; ");
            }

            Dbg.Log(bidx, $"End decoding for Bug Idea Data -- successful decoding? {decodedBugIdeaDataQ}; ");
            Dbg.EndLogging(bidx);
            return decodedBugIdeaDataQ;
        }
        bool PrintInfo()
        {
            bool printedInfoQ = true;
            if (IsSetup())
            {
                printedInfoQ = false;
                string prevFileName = Base.FileNameAndType;

                Dbg.StartLogging("BugIdeaData.PrintInfo()", out int bidx);
                Dbg.Log(bidx, $"PRINT INFO; Stored previous file location; Preparing to print BugIdeaData to seperate file '~/{PrintFileName}'; ");
                if (Base.SetFileLocation(Base.Directory, PrintFileName))
                {
                    Dbg.LogPart(bidx, $"Checking for existence of print file: ");
                    bool printAllEntriesQ = false;
                    FileInfo fileInfo = new(Base.Directory + PrintFileName);
                    if (fileInfo != null)
                    {
                        printAllEntriesQ = !fileInfo.Exists;
                        Dbg.LogPart(bidx, printAllEntriesQ ? "Does Not Exist, printing all entries" : "Exists, printing new entries only");
                    }
                    else Dbg.LogPart(bidx, "??");
                    Dbg.Log(bidx, "; ");


                    Dbg.Log(bidx, "Gathering print outs of bug / idea entries; ");
                    Dbg.NudgeIndent(bidx, true);                   
                    List<string> printOut = new();
                    foreach (BugIdeaInfo bii in biInfo)
                    {
                        if (bii.IsSetup())
                        {
                            if (printAllEntriesQ || bii.isNewQ)
                            {
                                string printLine = $"[{(bii.isBugQ ? "BUG" : "IDEA")}] {bii.description}";
                                printOut.Add($"\n{printLine}");
                                Dbg.Log(bidx, $"Added (new? {bii.isNewQ.ToString()[0]})  ::  '{printLine}'; ");

                                bii.isNewQ = false;
                            }
                        }
                    }
                    Dbg.NudgeIndent(bidx, false);


                    if (printOut.HasElements())
                    {
                        Dbg.Log(bidx, $"Proceeding to print bug / idea entries to file; Printing all entries? {printAllEntriesQ}; {(printAllEntriesQ ? "Adding header to print out; " : "")}");

                        string header = "BUG REPORT / IDEA SUGGESTION - PRINT OUT\n";
                        header += "----------------------------------------";

                        if (printAllEntriesQ)
                            printOut.Insert(0, header);

                        printedInfoQ = Base.FileWrite(printAllEntriesQ, null, printOut.ToArray());
                    }
                    else
                    {
                        printedInfoQ = !printAllEntriesQ;
                        Dbg.Log(bidx, $"No print out information provided // {(printAllEntriesQ ? "Printing all: missing entries [issue]" : "Printing new only: no new entries")}; ");
                    }
                }
                else Dbg.Log(bidx, "!Failed to set printing file location; ");

                Dbg.Log(bidx, $"Resetting to previous file location; Confirmed? {Base.SetFileLocation(AppDirectory, prevFileName)}");
                Dbg.EndLogging(bidx);
            }
            return printedInfoQ;
        }
    }
}
