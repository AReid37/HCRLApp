using System;
using System.Collections.Generic;
using System.IO;
using ConsoleFormat;

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
        public void Validate(VerNum[] versionRange, string[] folderPaths, string[] fileTypeExts)
        {
            Dbug.StartLogging("ContentValidator.Validate()");
            Dbug.Log($"Initiating Content Integrity Validation process...");

            bool isLibrarySetupQ = false;
            if (_libraryRef != null)
                isLibrarySetupQ = _libraryRef.IsSetup();
            Dbug.Log($"Content Validator: Has reference to library and library is setup? [{_libraryRef != null} & {isLibrarySetupQ}]; Received version range? [{versionRange.HasElements()}]; Received folder paths? [{folderPaths.HasElements()}]; Received file extensions? [{fileTypeExts.HasElements()}]; ");

            // it all begins here
            if (isLibrarySetupQ && versionRange.HasElements() && folderPaths.HasElements() && fileTypeExts.HasElements())
            {
                // just for now
                Dbug.Log("(4now) Folder paths and file extension received have been stored; ");
                FolderPaths = folderPaths;
                FileExtensions = fileTypeExts;
            }
            Dbug.EndLogging();
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
