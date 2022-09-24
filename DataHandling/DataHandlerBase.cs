using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    ///<summary>Sets down a few guidelines for data-handling classes and serves as a controller to file interactions.</summary>
    public class DataHandlerBase
    {
        #region fields / props
        /// <summary>Separator character in file encoding/decoding.</summary>
        internal const string Sep = "%"; // percentage symbol (seperator character; also dissallowed in HCAutoLogger)  [old seperator character --> * asterik (but is used in LEGEND...facepalm)]
        public const string FileDirectory = @"C:\Users\ntrc2\OneDrive\Pictures\High Contrast Textures\HCToolApps\HCRLA\hcd-tests\"; // public for Dbug.cs
        const string FileName = "hcrlaData.txt"; // change '.txt' to '.hcd' at end of development? Nn....NnnAahhh!
        // OG = @"hcd\hcrlaData.txt"
        // DBG = @"C:\Users\ntrc2\OneDrive\Pictures\High Contrast Textures\HCToolApps\HCRLA\hcd-tests\hcrlaData.txt"
        protected const string FileLocation = FileDirectory + FileName;
        protected string commonFileTag;
        #endregion

        public DataHandlerBase()
        {
            Base.SetFileLocation(FileLocation);
        }
        
        /// <summary>Handles file saving of all data-handling subclasses passed as parameters.</summary>
        public bool SaveToFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;
            if (dataHandlers.HasElements())
            {
                // refresh file
                noIssues = Base.SetFileLocation(FileLocation);
                if (noIssues)
                    noIssues = RestartMainEncoding();

                // other data
                for (int i = 0; i < dataHandlers.Length && noIssues; i++)
                {
                    string underlyingType = "<None>";
                    if (dataHandlers[i] != null)
                    {
                        underlyingType = dataHandlers[i].GetType().UnderlyingSystemType.ToString();
                        //noIssues = Base.SetFileLocation(FileLocation);
                        //if (noIssues)
                        noIssues = dataHandlers[i].EncodeToSharedFile();
                    }
                    else noIssues = false;

                    if (!noIssues)
                        Dbug.SingleLog("DataHandlerBase.SaveToFile()", $"Underlying type: {underlyingType}  //  Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
                }                
            }
            
            return noIssues;
        }
        /// <summary>Handles file loading of all data-handling subclasses passed as parameters.</summary>
        public bool LoadFromFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;
            if (dataHandlers.HasElements())
            {
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
                }
            }

            return noIssues;
        }
        /// <summary>Clears information from main file.</summary>
        private bool RestartMainEncoding()
        {
            return Base.FileWrite(true, null, "HCRLA-datafile");
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
