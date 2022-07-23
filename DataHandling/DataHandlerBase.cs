using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    ///<summary>Sets down a few guidelines for data-handling classes and serves as a controller to file interactions.</summary>
    public class DataHandlerBase
    {
        protected const string FileLocation = @"\u_\hcrlaData.txt";
        protected string fileTag;
        public List<string> dataLines;
        public DataHandlerBase()
        {
            dataLines = new List<string>();
            Base.SetFileLocation(FileLocation);
        }
        
        /// <summary>Clears information from main file.</summary>
        protected virtual void RestartMainEncoding()
        {
            Base.FileWrite(true, null, "HCRLA-datafile");
        }

        // same as "Save to File"
        internal virtual void EncodeToSharedFile()
        {
            // write using CF class library and info from dataLines
        }
        // same as "Load from File"
        internal virtual void DecodeFromSharedFile()
        {
            // read using CF class library and datakey
        }
    }
}
