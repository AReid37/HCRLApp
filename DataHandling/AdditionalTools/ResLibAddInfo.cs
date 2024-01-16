using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Relays information regarding the addition of contents, legends, and summaries for a version log submission.</summary>
    public struct ResLibAddInfo
    {
        public readonly bool isSetupQ;
        public SourceOverwrite source;
        public SourceCategory? subSource;
        public bool addedQ;
        public string addedObject;
        public bool dupeRejectionQ;
        public bool looseContentQ;
        public string extraInfo;

        public ResLibAddInfo(string addedObj, SourceOverwrite sourceType)
        {
            isSetupQ = true;
            addedObject = addedObj;
            source = sourceType;
            subSource = null;
            addedQ = false;
            dupeRejectionQ = false;
            looseContentQ = false;
            extraInfo = null;
        }

        /// <returns>A boolean relaying whether a value the added object has been set.</returns>
        public bool IsSetup()
        {
            return isSetupQ && addedObject.IsNotNEW();
        }
        public void SetAddedObject(string addedObj)
        {
            if (addedObj.IsNotNEW())
                addedObject = addedObj;
        }
        /// <summary>Set a sub-source category for <see cref="subSource"/>. Only applies for a source type of <see cref="SourceOverwrite.Content"/>.</summary>
        public void SetSubSourceCategory(SourceCategory subSourceType)
        {
            if (source == SourceOverwrite.Content)
                subSource = subSourceType;
        }
        public void SetAddedOutcome(bool addQ = true, bool dupeReject = false)
        {
            addedQ = addQ;
            dupeRejectionQ = dupeReject;
        }
        /// <summary>Set a sub-source instance as being a loose content. Only applies for a source type of <see cref="SourceOverwrite.Content"/>.</summary>
        public void SetLooseContentStatus(bool looseCon = true)
        {
            if (source == SourceOverwrite.Content)
                looseContentQ = looseCon;
        }
        public void SetExtraInfo(string extra)
        {
            if (extra.IsNotNEW())
                extraInfo = extra;
        }

        public override string ToString()
        {
            /// (AI) {obj}|{source}-{subSource}|{addedQ}[Add/Rej];{dupeReject}[Dupe];{extraInfo};
            string rlaiStr = "(AI) ";
            if (IsSetup())
            {
                rlaiStr += $"{addedObject}|{source}";
                if (subSource.HasValue)
                    rlaiStr += $"-{subSource.Value}";
                rlaiStr += $"|{(addedQ ? "Add" : "Rej")}{(dupeRejectionQ ? ";Dupe" : "")};";
                rlaiStr += extraInfo.IsNotNEW() ? $"{extraInfo};" : "";
            }
            else rlaiStr += "??";
            return rlaiStr;
        }
    }
}
