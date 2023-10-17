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
            Dbug.StartLogging("BugIdeaData.EncodeToSharedFile()");
            bool noDataButOkayQ = !IsSetup(), encodedBugIdea = false;
            if (IsSetup())
            {
                Dbug.Log($"Is setup; Proceeding to encoding '{biInfo.Count}' bug and idea entries; ");
                Dbug.NudgeIndent(true);
                List<string> biLines = new();
                for (int bx = 0; bx < biInfo.Count; bx++)
                {
                    Dbug.LogPart(IsSetup() ? "+ Enc" : "- OMIT ");
                    if (biInfo[bx].IsSetup())
                        biLines.Add(biInfo[bx].Encode());
                    Dbug.Log($"  {biInfo[bx].Encode()}; ");
                }
                Dbug.NudgeIndent(false);

                if (biLines.HasElements())
                {
                    encodedBugIdea = Base.FileWrite(false, commonFileTag, biLines.ToArray());
                    if (encodedBugIdea)
                        Dbug.Log("Encoding complete; Proceeding to second encoding state: PRINT INFO...");

                    bool printedEntriesQ = PrintInfo();
                    Dbug.Log($"Printed Entries Result? {(printedEntriesQ ? "Complete / Success" : "Skipped / Failed")}  [Does not affect encoding outcome]; ");
                }

                SetPreviousSelf();
                Dbug.Log($"Updated previous self to current; Confirmed? {!ChangesMade()}; ");
            }
            else Dbug.Log("Not setup; No data to encode, successful encoding...");
            Dbug.EndLogging();
            return noDataButOkayQ || encodedBugIdea;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.StartLogging("BugIdeaData.DecodeFromSharedFile");
            bool decodedBugIdeaDataQ = Base.FileRead(commonFileTag, out string[] bugIdeaLines);
            Dbug.Log($"Fetching file data (using tag '{commonFileTag}'); Successfully read from file? {decodedBugIdeaDataQ}; {nameof(bugIdeaLines)} has elements? {bugIdeaLines.HasElements()}");

            if (decodedBugIdeaDataQ && bugIdeaLines.HasElements())
            {
                Dbug.Log("Preparing to decoded Bug Idea Data; ");

                Dbug.NudgeIndent(true);
                for (int lx = 0; lx < bugIdeaLines.Length && decodedBugIdeaDataQ; lx++)
                {
                    Dbug.LogPart($"+ Decoded :: '{bugIdeaLines[lx]}'? ");
                    BugIdeaInfo newBii = new BugIdeaInfo();
                    bool decodedQ = newBii.Decode(bugIdeaLines[lx]);

                    if (decodedQ)
                    {
                        Dbug.LogPart($"True{(newBii.isNewQ ? "; No longer 'New'" : "")}");
                        newBii.isNewQ = false;
                    }
                    else Dbug.LogPart("FALSE");
                    Dbug.Log("; ");

                    biInfo.Add(newBii);
                    decodedBugIdeaDataQ = decodedQ;
                }
                Dbug.NudgeIndent(false);

                SetPreviousSelf();
                Dbug.Log($"Previous self has been set; Confirmed? {!ChangesMade()}; ");
            }

            Dbug.Log($"End decoding for Bug Idea Data -- successful decoding? {decodedBugIdeaDataQ}; ");
            Dbug.EndLogging();
            return decodedBugIdeaDataQ;
        }
        bool PrintInfo()
        {
            bool printedInfoQ = true;
            if (IsSetup())
            {
                printedInfoQ = false;
                string prevFileName = Base.FileNameAndType;

                Dbug.StartLogging();
                Dbug.Log($"PRINT INFO; Stored previous file location; Preparing to print BugIdeaData to seperate file '~/{PrintFileName}'; ");
                if (Base.SetFileLocation(Base.Directory, PrintFileName))
                {
                    Dbug.LogPart($"Checking for existence of print file: ");
                    bool printAllEntriesQ = false;
                    FileInfo fileInfo = new(Base.Directory + PrintFileName);
                    if (fileInfo != null)
                    {
                        printAllEntriesQ = !fileInfo.Exists;
                        Dbug.LogPart(printAllEntriesQ ? "Does Not Exist, printing all entries" : "Exists, printing new entries only");
                    }
                    else Dbug.LogPart("??");
                    Dbug.Log("; ");


                    Dbug.Log("Gathering print outs of bug / idea entries; ");
                    Dbug.NudgeIndent(true);                   
                    List<string> printOut = new();
                    foreach (BugIdeaInfo bii in biInfo)
                    {
                        if (bii.IsSetup())
                        {
                            if (printAllEntriesQ || bii.isNewQ)
                            {
                                string printLine = $"[{(bii.isBugQ ? "BUG" : "IDEA")}] {bii.description}";
                                printOut.Add($"\n{printLine}");
                                Dbug.Log($"Added (new? {bii.isNewQ.ToString()[0]})  ::  '{printLine}'; ");

                                bii.isNewQ = false;
                            }
                        }
                    }
                    Dbug.NudgeIndent(false);


                    if (printOut.HasElements())
                    {
                        Dbug.Log($"Proceeding to print bug / idea entries to file; Printing all entries? {printAllEntriesQ}; {(printAllEntriesQ ? "Adding header to print out; " : "")}");

                        string header = "BUG REPORT / IDEA SUGGESTION - PRINT OUT\n";
                        header += "----------------------------------------";

                        if (printAllEntriesQ)
                            printOut.Insert(0, header);

                        printedInfoQ = Base.FileWrite(printAllEntriesQ, null, printOut.ToArray());
                    }
                    else
                    {
                        printedInfoQ = !printAllEntriesQ;
                        Dbug.Log($"No print out information provided // {(printAllEntriesQ ? "Printing all: missing entries [issue]" : "Printing new only: no new entries")}; ");
                    }
                }
                else Dbug.Log("!Failed to set printing file location; ");

                Dbug.Log($"Resetting to previous file location; Confirmed? {Base.SetFileLocation(FileDirectory, prevFileName)}");
                Dbug.EndLogging();
            }
            return printedInfoQ;
        }
    }
}
