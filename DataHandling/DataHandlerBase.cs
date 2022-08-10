using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    ///<summary>Sets down a few guidelines for data-handling classes and serves as a controller to file interactions.</summary>
    public class DataHandlerBase
    {
        #region fields / props
        protected const string AstSep = "*"; // asterik (seperator character)
        // OG = @"hcd\hcrlaData.txt"
        // DBG = @"C:\Users\ntrc2\OneDrive\Pictures\High Contrast Textures\HCToolApps\HCRLA\hcd-tests\hcrlaData.txt"
        protected const string FileLocation = @"C:\Users\ntrc2\OneDrive\Pictures\High Contrast Textures\HCToolApps\HCRLA\hcd-tests\hcrlaData.txt";
        protected string commonFileTag;
        #endregion

        public DataHandlerBase()
        {
            Base.SetFileLocation(FileLocation);
        }
        
        /// <summary>Handles file saving of all data-handling subclasses passed as parameters.</summary>
        public virtual bool SaveToFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;
            if (dataHandlers.HasElements())
            {
                Base.SetFileLocation(FileLocation);
                noIssues = RestartMainEncoding();
                for (int i = 0; i < dataHandlers.Length && noIssues; i++)
                {
                    if (dataHandlers[i] != null)
                        noIssues = dataHandlers[i].EncodeToSharedFile();
                }
                if (!noIssues)
                    Dbug.SingleLog("DataHandlerBase.SaveToFile()", $"Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
            }
            
            return noIssues;
        }
        /// <summary>Handles file loading of all data-handling subclasses passed as parameters.</summary>
        public virtual bool LoadFromFile(params DataHandlerBase[] dataHandlers)
        {
            bool noIssues = true;
            if (dataHandlers.HasElements())
            {
                Base.SetFileLocation(FileLocation);
                for (int i = 0; i < dataHandlers.Length && noIssues; i++)
                {
                    if (dataHandlers[i] != null)
                        noIssues = dataHandlers[i].DecodeFromSharedFile();
                }
                if (!noIssues)
                    Dbug.SingleLog("DataHandlerBase.LoadFromFile()", $"Issue [Error]: {Tools.GetRecentWarnError(false, true)}");
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
    }


    public struct DataLine
    {
        internal string fileTag;
        internal string infoLine;

        public DataLine(string filetag, string infoline)
        {
            fileTag = filetag.IsNotNEW() ? filetag : null;
            infoLine = infoline.IsNotNEW() ? infoline : null;
        }
        public void SetFileTag(string filetag)
        {
            fileTag = filetag.IsNotNEW() ? filetag : null;
        }
        public void SetInfoLine(string infoline)
        {
            infoLine = infoline.IsNotNEW() ? infoline : null;
        }

        public bool HasTag()
        {
            return fileTag.IsNotNEW();
        }
        public bool HasInfo()
        {
            return infoLine.IsNotNEW();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
