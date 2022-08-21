using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;

namespace HCResourceLibraryApp.DataHandling
{
    public class LogDecoder : DataHandlerBase
    {
        // Fields / props of Content Library and\or related library data elements (Probably just collections of ResContents, LegendData, and SummaryData 
        // Why inherit from DataHandlerBase? 
        //  IDEA: Have the Log Decoder save the last used directory, and make it easier to submit text files (static item)
        // ^^ Last thing to do before next git commit
        
        static string _prevRecentDirectory, _recentDirectory;
        public static string RecentDirectory
        {
            get => _recentDirectory.IsNEW() ? null : _recentDirectory;
            set
            {
                _prevRecentDirectory = _recentDirectory;
                _recentDirectory = value.IsNEW() ? null : value;
            }
        }
        public ResLibrary DecodedLibrary { get; private set; }

        public LogDecoder()
        {
            commonFileTag = "logDec";
            _recentDirectory = null;
        }

        // file saving - loading
        protected override bool EncodeToSharedFile()
        {
            string encodeRD = RecentDirectory.IsNEW() ? AstSep : RecentDirectory;
            bool hasEnconded = FileWrite(false, commonFileTag, encodeRD);
            Dbug.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder has saved recent directory path :: {RecentDirectory}");
            return hasEnconded;
        }
        protected override bool DecodeFromSharedFile()
        {
            bool fetchedLogDecData = false;
            if (FileRead(commonFileTag, out string[] logDecData))
                if (logDecData.HasElements())
                {
                    string decode = logDecData[0].Contains(AstSep) ? null : logDecData[0]; 
                    RecentDirectory = decode;
                    _prevRecentDirectory = RecentDirectory;
                    Dbug.SingleLog("LogDecoder.EncodeToSharedFile()", $"Log Decoder recieved [{logDecData[0]}], and has loaded recent directory path :: {RecentDirectory}");
                    fetchedLogDecData = true;
                }
            return fetchedLogDecData;
        }
        public static new bool ChangesMade()
        {
            return _recentDirectory != _prevRecentDirectory;
        }

        // log file decoding
        public bool DecodeLogInfo(string[] logData)
        {
            bool hasFullyDecodedLogQ = false;
            Dbug.StartLogging("LogDecoder.DecodeLogInfo(str[])");
            if (logData.HasElements())
            {
                short nextSectionNumber = 0;
                string[] logSections =
                {
                    "Version", "Added", "Additional", "TTA", "Updated", "Legend", "Summary"
                };

                // -- reading version log file --
                for (int llx = 0; llx < logData.Length; llx++)
                {
                    /// setup
                    string logDataLine = logData[llx];
                    string nextSectionName = nextSectionNumber.IsWithin(0, (short)logSections.Length) ? logSections[nextSectionNumber] : "";


                    // -> Section

                    // -> Omit

                }
            }
            Dbug.EndLogging();
            return hasFullyDecodedLogQ;


            /// static methods?
            static string RemoveSquareBrackets(string str)
            {
                if (str.IsNotNEW())
                    str = str.Replace("[", "").Replace("]", "");
                return str;
            }
        }
    }
}
