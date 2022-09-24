namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Relays information regarding the decoding process of each line from version log files.</summary>
    public struct DecodeInfo
    {
        /// What does this struct do?
        ///     - Relays information regarding the decoding process of each line from version log files 
        ///     - The struct will store information regarding: Line being decoded and from which section, issues messages while decoding this line, and the resulting item
        ///     
        /// So then, what do we actually need?
        ///     Fields/props
        ///         bl successfulDecodeQ
        ///         str logLine
        ///         str sectionName
        ///         str decodeIssue
        ///         str resultingItemInfo
        ///         
        ///     Constructors
        ///         DI(str vLogLine, str logSection)
        /// 
        ///     Methods
        ///         - IsSetup()
        ///         - NoteIssue(str message)
        ///         - NoteResult(bl decodedQ, str result)
        ///     

        public bool successfulDecodeQ;
        public string logLine, sectionName;
        public string decodeIssue, resultingInfo;


        public DecodeInfo(string vLogLine, string logSection)
        {
            successfulDecodeQ = false;
            logLine = vLogLine;
            sectionName = logSection;
            decodeIssue = null;
            resultingInfo = null;
        }

        /// <summary>Also partly logs ' &lt;!di&gt; ' when an issue has been noted.</summary>
        /// <param name="message">A period ('.') will be added at the end of this message.</param>
        public void NoteIssue(string message)
        {
            if (message.IsNotNE())
            {
                decodeIssue = message;
                Dbug.LogPart(" <!di> ");
            }
        }
        /// <param name="result">A period ('.') will be added at the end of this message.</param>
        public void NoteResult(string result)
        {
            successfulDecodeQ = true;
            if (result.IsNotNE())
                resultingInfo = result;
        }
        /// <param name="result">A period ('.') will be added at the end of this message.</param>
        public void NoteResult(bool decodedQ, string result)
        {
            successfulDecodeQ = decodedQ;
            if (result.IsNotNE())
                resultingInfo = result;
        }
        /// <returns>A boolean relaying whether the log line, section name, and either the decode issue message or resulting info message have values.</returns>
        public bool IsSetup()
        {
            return logLine.IsNotNEW() && sectionName.IsNotNEW() && (decodeIssue.IsNotNE() || resultingInfo.IsNotNE());
        }
    }
}
