using System;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.DataHandling
{
    ///<summary>Sets down a few guidelines for data-handling classes and serves as a controller to file interactions.</summary>
    public class DataHandlerBase
    {
        #region fields / props
        /// <summary>Separator character in file encoding/decoding.</summary>
        internal const string Sep = "%"; // percentage symbol (seperator character; also dissallowed in HCAutoLogger)  [old seperator character --> * asterik (but is used in LEGEND...facepalm)]
        // public for Dbug.cs
        public static string FileDirectory = !Program.isDebugVersionQ? @"hcd\" : @"C:\Users\ntrc2\Pictures\High Contrast Textures\HCRLA\hcd-tests\"; 
        const string FileName = "hcrlaData.txt", BackupFileName = "hcrlaDataBackup.txt"; // change '.txt' to '.hcd' at end of development? Nn....NnnAahhh!
        const string SessionKeyTag = "skt";
        const int sessionKeyLength = 5;
        // OG = @"hcd\hcrlaData.txt"
        // DBG = @"C:\Users\ntrc2\Pictures\High Contrast Textures\HCRLA\hcd-tests\hcrlaData.txt"
        protected static string FileLocation = FileDirectory + FileName;
        protected static string BackupFileLocation = FileDirectory + BackupFileName;
        protected string commonFileTag;

        static bool _reversionAvailableQ;
        public static bool AvailableReversion
        {
            get => _reversionAvailableQ;
        }
        #endregion

        public DataHandlerBase()
        {
            //Base.SetFileLocation(FileLocation);
        }
        
        /// <summary>Handles file saving of all data-handling subclasses passed as parameters.</summary>
        public bool SaveToFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;
            if (dataHandlers.HasElements())
            {
                ProgressBarInitialize(false, true);
                TaskCount = 1 + 1 + dataHandlers.Length + 2;
                TaskNum = 0;
                ProgressBarUpdate(0);

                // fetch data from current file
                /// index 0 :: file tag   //  index 1 :: file data
                List<string[]> currentFileData = new();
                noIssues = Base.SetFileLocation(FileLocation);
                if (noIssues)
                {
                    if (Base.FileRead("::show", out string[] currDataLines))
                        if (currDataLines.HasElements())
                        {
                            foreach (string currLine in currDataLines)
                            {
                                string[] clSplit = currLine.Split(' ');
                                if (clSplit.HasElements(2))
                                {
                                    string dataTag = "";
                                    string dataInfo = "";
                                    for (int clx = 0; clx < clSplit.Length; clx++)
                                    {
                                        if (clx == 0)
                                            dataTag = clSplit[clx];
                                        else dataInfo += clSplit[clx] + " ";
                                    }
                                    dataInfo = dataInfo.Trim();

                                    if (dataTag.IsNotNE() && dataInfo.IsNotNE())
                                        currentFileData.Add(new string[] { dataTag, dataInfo });
                                }
                            }

                            TaskNum++;
                        }                    
                }
                ProgressBarUpdate(TaskNum / TaskCount);

                // refresh file
                if (noIssues)
                {
                    noIssues = RestartMainEncoding();
                    TaskNum++;
                }
                ProgressBarUpdate(TaskNum / TaskCount);

                // other data
                for (int i = 0; i < dataHandlers.Length && noIssues; i++)
                {
                    string underlyingType = "<None>";
                    if (dataHandlers[i] != null)
                    {
                        underlyingType = dataHandlers[i].GetType().UnderlyingSystemType.ToString();
                        noIssues = dataHandlers[i].EncodeToSharedFile();
                    }
                    else noIssues = false;

                    if (!noIssues)
                        Dbug.SingleLog("DataHandlerBase.SaveToFile()", $"Underlying type: {underlyingType}  //  Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
                    else TaskNum++;

                    ProgressBarUpdate(TaskNum / TaskCount);
                }

                // if (no issues) {Save current data to backup} else {Revert to current data}  
                if (noIssues)
                {
                    /// will not save data if there is just the reversion tag in original file
                    if (currentFileData.HasElements(2))
                    {
                        if (noIssues)
                        {
                            noIssues = Base.SetFileLocation(BackupFileLocation);
                            if (noIssues)
                                noIssues = RestartBackupEncoding();

                            for (int cx = 0; cx < currentFileData.Count && noIssues; cx++)
                            {
                                string[] currData = currentFileData[cx];
                                if (currData.HasElements(2))
                                    noIssues = Base.FileWrite(false, currData[0], currData[1]);
                            }

                            if (noIssues)
                            {
                                _reversionAvailableQ = true;
                                Dbug.SingleLog("DataHandlerBase.SaveToFile()", $"Previous file save version has been saved to backup file");
                            }
                            TaskNum++;
                        }
                    }
                }
                else
                {
                    //noIssues = Base.SetFileLocation(FileLocation);
                    bool revertingToCurr = RestartMainEncoding();
                    if (revertingToCurr)
                    {
                        for (int cx = 0; cx < currentFileData.Count && revertingToCurr; cx++)
                        {
                            string[] currData = currentFileData[cx];
                            if (currData.HasElements(2))
                                revertingToCurr = Base.FileWrite(false, currData[0], currData[1]);
                        }
                        TaskNum++;
                    }
                }
                ProgressBarUpdate(TaskNum / TaskCount, true, true);
            }
            
            return noIssues;
        }
        /// <summary>Handles file loading of all data-handling subclasses passed as parameters.</summary>
        public bool LoadFromFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;

            // load other data handlers
            if (dataHandlers.HasElements())
            {
                ProgressBarInitialize(false, true, 20, 4, 1, ForECol.Accent, ForECol.Accent);
                TaskCount = dataHandlers.Length + 1;
                TaskNum = 0;

                ProgressBarUpdate(0);
                for (int i = 0; i < dataHandlers.Length && noIssues; i++)
                {
                    string underlyingType = "<None>";
                    if (dataHandlers[i] != null)
                    {
                        underlyingType = dataHandlers[i].GetType().UnderlyingSystemType.ToString();
                        noIssues = Base.SetFileLocation(FileLocation);
                        if (noIssues)
                            noIssues = dataHandlers[i].DecodeFromSharedFile();
                    }
                    else noIssues = false;

                    if (!noIssues)
                        Dbug.SingleLog("DataHandlerBase.LoadFromFile()", $"Underlying type: {underlyingType}  //  Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
                    else TaskNum++;

                    ProgressBarUpdate(TaskNum / TaskCount);
                }

                _reversionAvailableQ = false;
                if (Base.SetFileLocation(BackupFileLocation))
                {
                    if (Base.FileRead(null, out string[] revertData))
                    {
                        if (revertData.HasElements(2))
                        {
                            _reversionAvailableQ = true;
                            Dbug.SingleLog("DataHandlerBase.LoadFromFile()", "A file save reversion is available");
                        }
                    }
                    TaskNum++;
                }
                
                ProgressBarUpdate(TaskNum / TaskCount, true, true);
            }

            return noIssues;
        }
        public bool RevertSaveFile()
        {
            bool revertedQ = false;
            if (_reversionAvailableQ)
            {
                // fetch data from backup file
                /// index 0 :: file tag   //  index 1 :: file dataList<string[]> backupFileData = new();
                List<string[]> backupFileData = new();
                revertedQ = Base.SetFileLocation(BackupFileLocation);
                if (revertedQ)
                {
                    revertedQ = Base.FileRead("::show", out string[] bckpDataLines);
                    if (revertedQ)
                        foreach (string bckpLine in bckpDataLines)
                        {
                            string[] clSplit = bckpLine.Split(' ');
                            if (clSplit.HasElements(2))
                            {
                                string dataTag = "";
                                string dataInfo = "";
                                for (int clx = 0; clx < clSplit.Length; clx++)
                                {
                                    if (clx == 0)
                                        dataTag = clSplit[clx];
                                    else dataInfo += clSplit[clx] + " ";
                                }
                                dataInfo = dataInfo.Trim();

                                if (dataTag.IsNotNE() && dataInfo.IsNotNE())
                                    backupFileData.Add(new string[] { dataTag, dataInfo });
                            }
                        }
                }

                // copy and save over main file data
                if (backupFileData.HasElements() && revertedQ)
                {
                    revertedQ = Base.SetFileLocation(FileLocation);
                    if (revertedQ)
                        RestartMainEncoding();

                    if (revertedQ)
                    {
                        for (int bx = 0; bx < backupFileData.Count && revertedQ; bx++)
                        {
                            string[] bckpDataLine = backupFileData[bx];
                            if (bckpDataLine.HasElements(2))
                                revertedQ = Base.FileWrite(false, bckpDataLine[0], bckpDataLine[1]);
                        }

                        if (!revertedQ)
                            Dbug.SingleLog("DataHandlerBase.RevertSaveFile()", $"Issue in copying reversion data to main file // Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
                        else Dbug.SingleLog("DataHandlerBase.RevertSaveFile()", $"Finished copying reversion data to main file");

                        // remove data from backup file when successfully reverted
                        if (revertedQ)
                        {
                            Base.SetFileLocation(BackupFileLocation);
                            RestartBackupEncoding();
                        }
                    }

                    // only if things have been changed
                    if (!revertedQ)
                    {
                        Dbug.SingleLog("DataHandlerBase.RevertSaveFile()", "Resetting save states due to reversion failure");
                        SaveToFile();
                    }
                }
            }
            return revertedQ;
        }        
        /// <summary>Clears information from main file.</summary>
        private bool RestartMainEncoding()
        {
            bool restartedMainQ = Base.FileWrite(true, null, "HCRLA-datafile");
            if (restartedMainQ)
                restartedMainQ = Base.FileWrite(false, SessionKeyTag, GetSaveSessionKey());
            return restartedMainQ;
        }
        private bool RestartBackupEncoding()
        {
            return Base.FileWrite(true, null, "HCRLA-datafile-backup");
        }
        string GetSaveSessionKey()
        {
            string sessionKey = "sessionKey_";
            for (int skx = 0; skx < sessionKeyLength; skx++)
            {
                /// two key routes
                ///  -> Route 1 (20%)   Digit (0~9)
                ///  -> Route 2 (80%)   Alphabetic (a~z) [50-50 upper-lower]
                string keyPiece;
                int route = Extensions.Random(1, 5);
                switch (route)
                {
                    // digit (20%)
                    case 1:
                        keyPiece = Extensions.Random(0, 9).ToString();
                        break;

                    // alphabetic (80%)
                    default:
                        keyPiece = Minimal.IntToAlphabet(Extensions.Random(1, 26)).ToString();
                        if (Extensions.Random(0, 1) == 1)
                            keyPiece = keyPiece.ToLower();
                        break;
                }
                sessionKey += keyPiece;
            }
            Dbug.SingleLog("DataBaseHandler.GetSaveSessionKey()", $"FYI -- {sessionKey}");
            return sessionKey;
        }

        // same as "Save to File"
        protected virtual bool EncodeToSharedFile()
        {
            // write using CF class library and info from dataLines
            return false;
        }
        // same as "Load from File"
        protected virtual bool DecodeFromSharedFile()
        {
            // read using CF class library and datakey
            return false;
        }
        public virtual bool ChangesMade()
        {
            return false;
        }
        public virtual bool IsSetup()
        {
            return false;
        }
    }
}
