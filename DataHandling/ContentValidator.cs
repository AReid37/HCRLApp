using System;
using System.Collections.Generic;
using System.IO;
using ConsoleFormat;
using static HCResourceLibraryApp.Extensions;

namespace HCResourceLibraryApp.DataHandling
{
    public class ContentValidator : DataHandlerBase
    {
        /** Content Validator Plan
            "a class purposed for Content Integrity Verification (CIV)"
            FEILDS / PROPS
            - ResLibr _libraryRef
            - str[] _folderPaths, _prevFolderPaths
            - str[] _fileTypeExts, _prevFileTypeExts    
            - CVI[] _conValReport

            CONSTRUCTORS
            - CV ()
            - CV (ResLib libraryReference)

            METHODS
            - pb void Validate(VerNum[] versionRange, str[] folderPaths, str[] fileTypeExtensions)
            - ovr bl EncodeToSharedFile()
            - ovr bl DecodeFromSharedFile()
            - ovr bl ChangesDetected()
            - vd SetPreviousSelf()
            - CV GetPreviousSelf()
         */

        #region fields / props
        ResLibrary _libraryRef;
        List<string> _folderPaths, _fileExts, _prevFolderPaths, _prevFileExts;
        List<ConValInfo> _conValInfoDock;
        bool? anyInvalidatedQ;
        float _percentValidation;

        // public 
        /// <summary>Replaces the space character (' ') when expanding data IDs for validating content files.</summary>
        public const string SpaceReplace = "_";
        public const string FolderPathDisabledToken = "[fdpEn:Fl]";

        
        // props
        public string[] FolderPaths
        {
            get
            {
                string[] fPaths = null;
                if (_folderPaths.HasElements())
                    fPaths = _folderPaths.ToArray();
                return fPaths;
            }
            private set
            {
                if (value.HasElements())
                {
                    _folderPaths = new List<string>();
                    _folderPaths.AddRange(value);
                }
            }
        }
        public string[] FileExtensions
        {
            get
            {
                string[] fExts = null;
                if (_fileExts.HasElements())
                    fExts = _fileExts.ToArray();
                return fExts;
            }
            private set
            {
                if (value.HasElements())
                {
                    _fileExts = new List<string>();
                    _fileExts.AddRange(value);
                }
            }
        }
        public ConValInfo[] CivInfoDock
        {
            get
            {
                ConValInfo[] conValInfos = null;
                if (_conValInfoDock.HasElements())
                    conValInfos = _conValInfoDock.ToArray();
                return conValInfos;
            }
        }
        public bool? AreAnyInvalidated
        {
            get => anyInvalidatedQ;
        }
        public float ValidationPercentage
        {
            get => _percentValidation;
        }
        #endregion

        public ContentValidator() : base()
        {
            commonFileTag = "cnv";
            _conValInfoDock = new List<ConValInfo>();
        }

        // FILE SAVING SYNTAX - CONTENT VALIDATOR
        // tag  'cnv'
        //      -> print folder paths first (each has it's own line)
        //      -> print all file extensions (all on one line separated by 'Sep' character)
        
        protected override bool EncodeToSharedFile()
        {
            Dbug.StartLogging("ContentValidator.EncodeToSharedFile()");
            bool noDataButOkayQ = !IsSetup(), encodedConValQ = false;
            if (IsSetup())
            {
                Dbug.Log($"Is setup; Preparing to encode '{_folderPaths.Count}' folder paths and '{_fileExts.Count}' file extensions; ");
                List<string> conValDataLines = new();
                bool fetchedFolderDataQ = false, fetchedFileExtsDataQ = false;

                // compile the data
                /// folder paths
                if (_folderPaths.HasElements())
                {
                    Dbug.Log("Encoding all folder paths; ");
                    Dbug.NudgeIndent(true);
                    foreach (string fdPath in _folderPaths)
                        if (fdPath.IsNotNEW())
                        {
                            conValDataLines.Add(fdPath);
                            fetchedFolderDataQ = true;
                            Dbug.Log($" + Encoded folder :: L{conValDataLines.Count}| {fdPath}; ");
                        }
                    Dbug.NudgeIndent(false);
                }
                /// file extensions
                if (_fileExts.HasElements())
                {
                    string allFExts = "";
                    foreach (string fExt in _fileExts)
                        if (fExt.IsNotNEW())
                            allFExts += $"{fExt} ";
                    if (allFExts.IsNotNE())
                    {
                        Dbug.Log($" + Encoding all file extensions :: L{conValDataLines.Count + 1}| {allFExts}; ");
                        allFExts = allFExts.Trim().Replace(" ", Sep);
                        conValDataLines.Add(allFExts);
                        fetchedFileExtsDataQ = true;
                    }
                }

                // encode data
                if (fetchedFileExtsDataQ && fetchedFolderDataQ)
                    encodedConValQ = Base.FileWrite(false, commonFileTag, conValDataLines.ToArray());

                if (encodedConValQ)
                    SetPreviousSelf();
                Dbug.Log($"Successfully encoded to file? {(encodedConValQ? "True; Previous self is up-to-date" : "False")}; ");
            }
            else Dbug.Log("Not setup; No data to encode but that's okay...");
            Dbug.EndLogging();
            return encodedConValQ || noDataButOkayQ;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.StartLogging("ContentValidator.DecodeFromSharedFile()");
            bool decodedConValDataQ = Base.FileRead(commonFileTag, out string[] conValDataLines);
            Dbug.Log($"Fetching file data (using tag '{commonFileTag}'); Successfully read from file? {decodedConValDataQ}; {nameof(conValDataLines)} has elements? {conValDataLines.HasElements()}");

            _folderPaths = new List<string>();
            _fileExts = new List<string>();
            if (decodedConValDataQ && conValDataLines.HasElements())
            {
                Dbug.Log("Preparing to decode content validator data; ");
                Dbug.NudgeIndent(true);
                for (int lx = 0; lx < conValDataLines.Length && decodedConValDataQ; lx++)
                {
                    decodedConValDataQ = false;
                    string line = conValDataLines[lx];
                    bool lastLine = lx + 1 == conValDataLines.Length;
                    Dbug.LogPart($"Decoding {(!lastLine ? "folder path" : "file extensions")}; ");

                    if (!lastLine)
                    {
                        _folderPaths.Add(line);
                        Dbug.Log($"Fetched folder :: {line}; ");
                        decodedConValDataQ = true;
                    }
                    else
                    {
                        if (line.Contains(Sep))
                        {
                            Dbug.Log("There are multiple extensions; ");
                            string[] fExts = line.Split(Sep);
                            if (fExts.HasElements())
                            {
                                _fileExts.AddRange(fExts);
                                Dbug.LogPart($" > Fetched file extensions :: {line.Replace(Sep, " ")}");
                                decodedConValDataQ = true;
                            }
                        }
                        else
                        {
                            _fileExts.Add(line);
                            Dbug.LogPart($"Fetched file extension :: {line}");
                            decodedConValDataQ = true;
                        }
                        Dbug.Log("; ");
                    }
                }

                SetPreviousSelf();
                Dbug.Log($"Previous self has been set; Confirmed as the same? {!ChangesMade()}; ");
                Dbug.NudgeIndent(false);
            }
            Dbug.Log($"End decoding for Content Validator -- successful decoding? {decodedConValDataQ}; ");
            Dbug.EndLogging();
            return decodedConValDataQ;
        }

        public void GetResourceLibraryReference(ResLibrary mainLibrary)
        {
            if (mainLibrary != null)
                if (mainLibrary.IsSetup())
                    _libraryRef = mainLibrary;
        }
        /// the... THE MAIN PURPOSE!!
        public bool Validate(VerNum[] versionRange, string[] folderPaths, string[] fileTypeExts)
        {
            Dbug.StartLogging("ContentValidator.Validate()");
            Dbug.Log($"Initiating Content Integrity Validation process...");

            bool civProcessBegunQ = false;
            bool isLibrarySetupQ = false;
            if (_libraryRef != null)
                isLibrarySetupQ = _libraryRef.IsSetup();
            Dbug.Log($"Content Validator: Has reference to library and library is setup? [{_libraryRef != null} & {isLibrarySetupQ}]; Received version range? [{versionRange.HasElements()}]; Received folder paths? [{folderPaths.HasElements()}]; Received file extensions? [{fileTypeExts.HasElements()}]; ");

            // it all begins here
            if (isLibrarySetupQ && versionRange.HasElements() && folderPaths.HasElements() && fileTypeExts.HasElements())
            {
                /// some global vars
                const string relativePathClampSuffix = "...";
                const int relativePathDist = 20, subRelativePathDist = 250;
                bool continueToStep_gatherDataIDs = false;

                // + store folder paths and file extensions +
                Dbug.Log("Folder paths and file extensions received have been stored; ");
                _conValInfoDock = null;
                anyInvalidatedQ = null;
                folderPaths = folderPaths.SortWords().ToArray();
                FolderPaths = folderPaths;
                FileExtensions = fileTypeExts;
                civProcessBegunQ = true;


                // + fetch all sub-folders within given folder paths +
                Dbug.Log("Preparing to fetch all sub-folder paths and contents; ");
                Dbug.NudgeIndent(true);
                List<DirectoryInfo> allFoldersList = new();
                foreach (string folderPath in folderPaths)
                {                    
                    DirectoryInfo fPathInfo = new(folderPath);
                    Dbug.Log($"Received folder path :: {folderPath} [{(fPathInfo.Exists? "Exists" : (fPathInfo.ToString().StartsWith(FolderPathDisabledToken) ? "Disabled" : "D.N.E!"))}]");
                    if (fPathInfo.Exists)
                    {
                        allFoldersList.Add(fPathInfo);

                        /// this EnumerationOptions is set to: A) ignore inaccessibles
                        EnumerationOptions EOdir_ignoreInaccessible = new();
                        EOdir_ignoreInaccessible.IgnoreInaccessible = true;
                        EOdir_ignoreInaccessible.RecurseSubdirectories = false;

                        DirectoryInfo[] fPathSubFolders = fPathInfo.GetDirectories($"*", EOdir_ignoreInaccessible);
                        FileInfo[] fPathFiles = fPathInfo.GetFiles($"*", EOdir_ignoreInaccessible);

                        Dbug.LogPart($"Main folder contains ");
                        if (fPathFiles != null)
                            Dbug.LogPart($"[{fPathFiles.Length}] files and ");
                        if (fPathSubFolders != null)
                            Dbug.LogPart($"[{fPathSubFolders.Length}] sub-folders");
                        else Dbug.LogPart("no sub-folders");
                        if (!fPathFiles.HasElements() && !fPathSubFolders.HasElements())
                            Dbug.LogPart(" (Empty? Inaccessible?)");
                        Dbug.Log($" [Attributes: {fPathInfo.Attributes}]; ");


                        /// IF main sub-folders have been fetched: get all sub-folders within the sub-folders
                        if (fPathSubFolders.HasElements())
                        {
                            Dbug.NudgeIndent(true);                            
                            for (int fpsx = 0; fpsx < fPathSubFolders.Length; fpsx++)
                            {
                                DirectoryInfo fPathSub = fPathSubFolders[fpsx];
                                allFoldersList.Add(fPathSub);

                                string relativeSubPath = fPathSub.ToString().Clamp(relativePathDist, relativePathClampSuffix, fPathSub.Name, false);
                                Dbug.Log($"SubF {fpsx + 1}| {relativeSubPath}");
                                Dbug.NudgeIndent(true);

                                /// this EnumerationOptions is set to: A) fetch all sub-folders -- B) ignore inaccessibles
                                EnumerationOptions EOsubDir_getAllDirs_ignoreInaccessibles = new();
                                EOsubDir_getAllDirs_ignoreInaccessibles.IgnoreInaccessible = true;
                                EOsubDir_getAllDirs_ignoreInaccessibles.RecurseSubdirectories = true;

                                DirectoryInfo[] subDirs = fPathSub.GetDirectories("*", EOsubDir_getAllDirs_ignoreInaccessibles);
                                FileInfo[] subDirFiles = fPathSub.GetFiles("*", EOsubDir_getAllDirs_ignoreInaccessibles);

                                Dbug.LogPart($"Contains ");
                                if (subDirFiles != null)
                                    Dbug.LogPart($"[{subDirFiles.Length}] files and ");
                                if (subDirs != null)
                                    Dbug.LogPart($"[{subDirs.Length}] sub-folders{(subDirs.HasElements()? " (listed below)" : "")}");
                                else Dbug.LogPart("no sub-folders");
                                if (!subDirFiles.HasElements() && !subDirs.HasElements())
                                    Dbug.LogPart(" (Empty? Inaccessible?)");
                                Dbug.Log($" [Attributes: {fPathSub.Attributes}]; ");


                                /// IF sub-folder's sub-folders have been fetched: add valid directories to full list and search
                                if (subDirs.HasElements())
                                {
                                    List<string> deepSubDirs = new();
                                    foreach (DirectoryInfo dir in subDirs)
                                    {
                                        string dirName = $"{dir}";
                                        if (dir.Attributes != FileAttributes.Directory)
                                            dirName += $" [Attributes: {dir.Attributes}]";

                                        allFoldersList.Add(dir);
                                        deepSubDirs.Add(dirName);
                                    }
                                    deepSubDirs = deepSubDirs.ToArray().SortWords();

                                    Dbug.NudgeIndent(true);
                                    foreach (string aDSDir in deepSubDirs)
                                        Dbug.Log($"{relativePathClampSuffix}{aDSDir.Clamp(subRelativePathDist, relativePathClampSuffix, fPathSub.Name, true)}");
                                    Dbug.NudgeIndent(false);
                                }
                                Dbug.NudgeIndent(false);
                            }                            
                            Dbug.NudgeIndent(false);
                        }
                    }
                }
                /// confirm folders obtained (through Dbug)
                if (allFoldersList.HasElements())
                {
                    continueToStep_gatherDataIDs = true;
                    Dbug.Log($"Completed fetching all folder paths (total of '{allFoldersList.Count}'); ");
                    Dbug.NudgeIndent(true);
                    foreach (DirectoryInfo dir in allFoldersList)
                        Dbug.Log(dir.ToString());
                    Dbug.NudgeIndent(false);
                }
                Dbug.NudgeIndent(false);


                // + gather all Data IDs accoring to version range, expand them, and store them as a list of ConValInfo instances +
                List<ConValInfo> allExpandedDataIDs = new();
                if (continueToStep_gatherDataIDs)
                { /// wrapping ... oh what a sad excuse to do so...                    
                    VerNum verLow, verHigh;
                    if (versionRange.HasElements(2))
                    {
                        verLow = versionRange[0];
                        verHigh = versionRange[1];

                        if (verLow.AsNumber > verHigh.AsNumber)
                        {
                            verLow = verHigh;
                            verHigh = versionRange[0];
                        }
                    }
                    else
                    {
                        verLow = versionRange[0];
                        verHigh = verLow;
                    }

                    Dbug.Log($"Gathering and expanding all Data IDs within version range: {(verLow.Equals(verHigh)? $"{verLow} only" : $"{verLow} to {verHigh}")}; ");
                    Dbug.NudgeIndent(true);
                    Dbug.Log($"Gathering Data IDs from library shelves; ");

                    /// gather data IDs
                    Dbug.NudgeIndent(true);
                    List<string> allDataIDs = new();
                    foreach (ResContents resCon in _libraryRef.Contents)
                    {
                        bool somethingMightHaveBeenAddedq = false;
                        string dbugText = "";
                        if (resCon.IsSetup())
                        {
                            /// ConBase
                            if (resCon.ConBase.VersionNum.AsNumber.IsWithin(verLow.AsNumber, verHigh.AsNumber))
                            {
                                dbugText += $"From #{resCon.ShelfID} ({resCon.ConBase.VersionNum.ToStringNums()}) {"'" + resCon.ContentName + "'", -30}  //  CBG :: ";
                                for (int cbgx = 0; cbgx < resCon.ConBase.CountIDs; cbgx++)
                                {
                                    LogDecoder.DisassembleDataID(resCon.ConBase[cbgx], out string dk, out string db, out _);
                                    string dataID = dk + db;
                                    if (!allDataIDs.Contains(dataID))
                                    {
                                        allDataIDs.Add(dataID);
                                        dbugText += $"{dataID} ";
                                    }
                                }
                                somethingMightHaveBeenAddedq = true;
                            }

                            /// ConAddits
                            if (resCon.ConAddits.HasElements())
                            {
                                int caNum = 0;
                                foreach (ContentAdditionals conAddit in resCon.ConAddits)
                                {
                                    caNum++;
                                    if (conAddit.VersionAdded.AsNumber.IsWithin(verLow.AsNumber, verHigh.AsNumber))
                                    {
                                        if (!somethingMightHaveBeenAddedq)
                                            dbugText += $"From #{resCon.ShelfID} ({resCon.ConBase.VersionNum.ToStringNums()}) {"'" + resCon.ContentName + "'",-30}";
                                        dbugText += $"  //  CA#{caNum} :: ";
                                        for (int cax = 0; cax < conAddit.CountIDs; cax++)
                                        {
                                            LogDecoder.DisassembleDataID(conAddit[cax], out string dk, out string db, out _);
                                            string dataID = dk + db;
                                            if (!allDataIDs.Contains(dataID))
                                            {
                                                allDataIDs.Add(dataID);
                                                dbugText += $"{dataID} ";
                                            }
                                        }
                                        somethingMightHaveBeenAddedq = true;
                                    }
                                }
                            }
                        }

                        if (somethingMightHaveBeenAddedq)
                            Dbug.Log(dbugText + "; ");
                    }
                    Dbug.NudgeIndent(false);

                    /// expand the data IDs
                    allDataIDs = allDataIDs.ToArray().SortWords();
                    ConValInfo[] preExpandedIDs = new ConValInfo[allDataIDs.Count];
                    Dbug.Log($"Gathered and sorted '{allDataIDs.Count}' Data IDs; Proceeding to expand data IDs by legend datas and create ConValInfo instances; ");
                    Dbug.NudgeIndent(true);
                    for (int lx = 0; lx < _libraryRef.Legends.Count; lx++)
                    {
                        /// non-wordy data IDs ... the regulars here
                        LegendData legData = _libraryRef.Legends[lx];
                        if (CharScore(legData.Key[0]) > 0)
                        {
                            string expandedLegDef = $"{legData[0].Replace(" ", SpaceReplace)}{SpaceReplace}";
                            string dbugPretText = $"{legData.Key} '{expandedLegDef}'";
                            Dbug.LogPart($"For {dbugPretText, -20}   //   ");
                            for (int ax = 0; ax < allDataIDs.Count; ax++)
                            {
                                string ogDataID = allDataIDs[ax];
                                if (ogDataID.IsNotNE())
                                {
                                    LogDecoder.DisassembleDataID(ogDataID, out string dk, out string db, out _);
                                    if (dk.IsNotNE())
                                    {
                                        /// q7 o30-wet dl0  -->  Quail_7  OtherObj_30-wet  Door_Long_0
                                        if (legData.Key == dk)
                                        {
                                            string trueDID = $"{expandedLegDef}{db}";
                                            Dbug.LogPart($"{db} ");
                                            preExpandedIDs[ax] = new ConValInfo(dk + db, trueDID);
                                            allDataIDs[ax] = null;
                                        }
                                    }                                   
                                }                            
                            }
                            Dbug.Log("; ");
                        }

                        /// wordy IDs are fetched at the end
                        if (lx + 1 == _libraryRef.Legends.Count)
                        {
                            Dbug.LogPart("Wordy Data IDs (non-legend)  //  ");
                            for (int ax2 = 0; ax2 < allDataIDs.Count; ax2++)
                            {
                                if (allDataIDs[ax2].IsNotNE())
                                {
                                    Dbug.LogPart($"{allDataIDs[ax2]} ");
                                    preExpandedIDs[ax2] = new ConValInfo(allDataIDs[ax2], allDataIDs[ax2]);
                                }
                            }
                            Dbug.Log("; ");
                        }
                    }   
                    Dbug.NudgeIndent(false);

                    Dbug.Log($"Created '{preExpandedIDs.Length}' ConValInfos with expanded data IDs; Moving info list to next stage; ");
                    allExpandedDataIDs.AddRange(preExpandedIDs);
                    Dbug.NudgeIndent(false);
                }


                // THE TRUE CIV PROCESS
                // + iterate through collected folder paths, and confirm the expanded data IDs with regards to given file extensions + 
                if (allExpandedDataIDs.HasElements() && allFoldersList.HasElements())
                {
                    Dbug.Log("- - - - - - - - - - - - - - - - -");
                    Dbug.Log("Content Integrity Verification process is ready to begin; Proceeding to validate contents; ");
                    Dbug.LogPart("File extensions in use:");
                    foreach (string fileExt in fileTypeExts)
                        Dbug.LogPart($" {fileExt}");
                    Dbug.Log("; ");

                    const int allValidatedNum = 0;
                    int countInvalidated = allValidatedNum;

                    Dbug.NudgeIndent(true);
                    /// FOR each expanded data ID (ConValInfo)                    
                    for (int cdx = 0; cdx < allExpandedDataIDs.Count; cdx++)
                    {
                        ConValInfo conToVal = allExpandedDataIDs[cdx];
                        Dbug.LogPart($"Validating {$"'{conToVal.DataID}'", -25}  //  ");
                        bool foundThisContentQ = false;
                        string filePath = null;

                        /// FOR each folder in folders list
                        for (int fdx = 0; fdx < allFoldersList.Count && !foundThisContentQ; fdx++)
                        {
                            /// this EnumerationOptions is set to: A) ignore inaccessibles
                            EnumerationOptions EOdirToCheck_ignoreInaccessible = new();
                            EOdirToCheck_ignoreInaccessible.IgnoreInaccessible = true;

                            DirectoryInfo dirToCheck = allFoldersList[fdx];
                            FileInfo[] dirFiles = dirToCheck.GetFiles("*", EOdirToCheck_ignoreInaccessible);
                            if (dirFiles.HasElements())
                                /// FOR each file in given folder
                                for (int flx = 0; flx < dirFiles.Length && !foundThisContentQ; flx++)
                                {
                                    FileInfo fileToCheck = dirFiles[flx];
                                    for (int fex = -1; fex < fileTypeExts.Length && !foundThisContentQ; fex++)
                                    {
                                        /// IF file extension index is greater than or equal to zero: fetch appropriate file extension
                                        string fileExt = "";
                                        if (fex >= 0)
                                            fileExt = fileTypeExts[fex];
                                        
                                        foundThisContentQ = fileToCheck.Name == $"{conToVal.DataID}{fileExt}";
                                        if (foundThisContentQ)
                                        {
                                            string relativePath = fileToCheck.FullName.Clamp(relativePathDist, relativePathClampSuffix, fileToCheck.Name, false);
                                            filePath = relativePath;
                                            Dbug.LogPart($"VALIDATED!  Found file '{conToVal.DataID}{fileExt}' at following relative path :: {relativePath}");
                                        }
                                        else
                                        {
                                            foundThisContentQ = fileToCheck.Name == $"{conToVal.DataID.Replace("_", " ")}{fileExt}";
                                            if (foundThisContentQ)
                                            {
                                                string relativePath = fileToCheck.FullName.Clamp(relativePathDist, relativePathClampSuffix, fileToCheck.Name, false);
                                                filePath = relativePath;
                                                Dbug.LogPart($"VALIDATED*  Found file '{conToVal.DataID.Replace("_", " ")}{fileExt}' at following relative path :: {relativePath}");
                                            }
                                        }
                                    }
                                }
                        }

                        if (foundThisContentQ)
                        {
                            conToVal.ConfirmValidation(filePath);
                            allExpandedDataIDs[cdx] = conToVal;
                            if (allExpandedDataIDs[cdx].IsValidated)
                                Dbug.LogPart(" [!]");
                        }
                        else
                        {
                            Dbug.LogPart("Invalidated");
                            countInvalidated++;
                        }
                        Dbug.Log("; ");
                    }

                    anyInvalidatedQ = countInvalidated != allValidatedNum;
                    Dbug.NudgeIndent(false);

                    float truePercentValidated = (float)(allExpandedDataIDs.Count - countInvalidated) / allExpandedDataIDs.Count * 100f;
                    float clampedPercentValidated = countInvalidated > 0 ? truePercentValidated.Clamp(0, 99.99f) : truePercentValidated;
                    string validationTurnout = $"'{allExpandedDataIDs.Count - countInvalidated}' of '{allExpandedDataIDs.Count}' (actual: {truePercentValidated:0.000}% | sent: {clampedPercentValidated:0.00}%)";
                    Dbug.Log($"Content Integrity Verification process complete; {validationTurnout} contents were validated; Moving list of ConValInfos data to be accessed by other classes; ");
                    _percentValidation = clampedPercentValidated;
                    _conValInfoDock = allExpandedDataIDs;
                }
            }

            Dbug.EndLogging();
            return civProcessBegunQ;
        }

        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }
        public override bool IsSetup()
        {
            return _folderPaths.HasElements() && _fileExts.HasElements();
        }        
        void SetPreviousSelf()
        {
            _prevFolderPaths = null;
            _prevFileExts = null;
            if (_folderPaths.HasElements())
            {
                _prevFolderPaths = new List<string>();
                _prevFolderPaths.AddRange(_folderPaths.ToArray());
            }
            if (_fileExts.HasElements())
            {
                _prevFileExts = new List<string>();
                _prevFileExts.AddRange(_fileExts.ToArray());
            }
        }
        ContentValidator GetPreviousSelf()
        {
            ContentValidator prevConVal = new();
            if (_prevFolderPaths != null)
                prevConVal.FolderPaths = _prevFolderPaths.ToArray();
            if (_prevFileExts != null)
                prevConVal.FileExtensions = _prevFileExts.ToArray();
            return prevConVal;
        }
        bool Equals(ContentValidator other)
        {
            bool areEquals = false;
            if (other != null)
            {
                areEquals = true;
                for (int cx = 0; cx < 3 && areEquals; cx++)
                {
                    switch (cx)
                    {
                        case 0:
                            areEquals = IsSetup() == other.IsSetup();
                            break;

                        case 1:
                            areEquals = FolderPaths.HasElements() == other.FolderPaths.HasElements();
                            if (areEquals && FolderPaths.HasElements())
                            {
                                areEquals = FolderPaths.Length == other.FolderPaths.Length;
                                for (int fdx = 0; fdx < FolderPaths.Length && areEquals; fdx++)
                                    areEquals = FolderPaths[fdx] == other.FolderPaths[fdx];
                            }
                            break;

                        case 2:
                            areEquals = FileExtensions.HasElements() == other.FileExtensions.HasElements();
                            if (areEquals && FileExtensions.HasElements())
                            {
                                areEquals = FileExtensions.Length == other.FileExtensions.Length;
                                for (int fex = 0; fex < FileExtensions.Length && areEquals; fex++)
                                    areEquals = FileExtensions[fex] == other.FileExtensions[fex];
                            }
                            break;
                    }
                }
            }
            return areEquals;
        }
    }
}
