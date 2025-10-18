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

        public string logLine, sectionName;
        public string decodeIssue, resultingInfo;
        /// <summary>Tool for proper matching of decoding info with results to the appropriate item when displaying outcome of decoding.</summary>
        public string crossRef;

        public bool NotedIssueQ { get => decodeIssue.IsNotNEW(); }
        public bool NotedResultQ { get => resultingInfo.IsNotNEW(); }

        public DecodeInfo(string vLogLine, string logSection)
        {
            logLine = vLogLine;
            sectionName = logSection;
            decodeIssue = null;
            resultingInfo = null;
            crossRef = null;
        }

        /// <summary>Also partly logs ' &lt;!di&gt; ' when an issue has been noted.</summary>
        /// <param name="message">A period ('.') will be added at the end of this message.</param>
        /// <remarks>The noted issue may not be overwritten.</remarks>
        public void NoteIssue(int threadIx, string message)
        {
            if (message.IsNotNE() && decodeIssue.IsNE())
            {
                decodeIssue = message;
                Dbg.LogPart(threadIx, " <!di> ");
            }
        }
        /// <param name="result">A period ('.') will be added at the end of this message.</param>
        /// <param name="crossReference">A unique string for identifying and matching decoded contents and decode infos for displaying the results.</param>
        /// <remarks>The noted result may be overwritten.</remarks>
        public void NoteResult(string result, string crossReference = null)
        {
            if (result.IsNotNE())
                resultingInfo = result;

            if (crossReference.IsNotNEW())
                crossRef = crossReference;
        }
        /// <returns>A boolean relaying whether the log line, section name, and either the decode issue message or resulting info message have values.</returns>        
        public bool IsSetup()
        {
            return logLine.IsNotNEW() && sectionName.IsNotNEW() && (decodeIssue.IsNotNE() || resultingInfo.IsNotNE());
        }        
        public bool Equals(DecodeInfo other)
        {
            bool areEquals = true;
            for (int d = 0; d < 6 && areEquals; d++)
            {
                switch (d)
                {
                    case 0:
                        areEquals = IsSetup() == other.IsSetup();
                        break;

                    case 1:
                        areEquals = logLine == other.logLine;
                        break;

                    case 2:
                        areEquals = sectionName == other.sectionName;
                        break;

                    case 3:
                        areEquals = decodeIssue == other.decodeIssue;
                        break;

                    case 4:
                        areEquals = resultingInfo == other.resultingInfo;
                        break;

                    case 5:
                        areEquals = crossRef == other.crossRef;
                        break;
                }
            }
            return areEquals;
        }
    }
}
