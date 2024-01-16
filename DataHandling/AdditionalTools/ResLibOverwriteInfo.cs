using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{    
    /// <summary>Relays information regarding the overwriting of information during a library integration.</summary>
    public struct ResLibOverwriteInfo
    {
        readonly bool isSetupQ;
        /// <summary>The content that may be overwritten.</summary>
        public readonly string contentExisting;
        /// <summary>The new content that may replace an existing content.</summary>
        public readonly string contentOverwriting;
        /// <summary>The final form of the content if a change has occured.</summary>
        public string contentResulting;
        bool? overwrittenQ;
        public bool ignoreOverwriteQ, looseContentQ;
        public readonly SourceOverwrite source;
        public SourceCategory? subSource;
        // PROPS
        public bool OverwrittenQ
        {
            get
            {
                bool isOverwrittenQ = false;
                if (overwrittenQ.HasValue)
                    isOverwrittenQ = overwrittenQ.Value;
                return isOverwrittenQ;
            }
        }


        public ResLibOverwriteInfo(string existing, string overwriting, SourceOverwrite sourceType = SourceOverwrite.Content)
        {
            isSetupQ = true;
            if (existing.IsNotNEW())
                contentExisting = existing;
            else contentExisting = null;
            if (overwriting.IsNotNEW())
                contentOverwriting = overwriting;
            else contentOverwriting = null;
            contentResulting = null;
            overwrittenQ = null;
            ignoreOverwriteQ = false;
            looseContentQ = false;
            source = sourceType;
            subSource = null;
        }
        

        /// <returns>A boolean relaying whether a value for an existing or overwriting content has been provided and a overwritten status has been set.</returns>
        public bool IsSetup()
        {
            return isSetupQ && (contentExisting.IsNotNEW() || contentOverwriting.IsNotNEW()) && overwrittenQ.HasValue;
        }
        public void SetOverwriteStatus(bool overwritten = true)
        {
            overwrittenQ = overwritten;
        }
        public void SetIgnoreOverwrite(bool overwriteDisableQ = true)
        {
            ignoreOverwriteQ = overwriteDisableQ;
        }
        /// <summary>Set a sub-source instance as being a loose content. Only applies for a source type of <see cref="SourceOverwrite.Content"/>.</summary>
        public void SetLooseContentStatus(bool looseCon = true)
        {
            if (source == SourceOverwrite.Content)
                looseContentQ = looseCon;
        }
        public void SetResult(string result)
        {
            if (result.IsNotNEW())
                contentResulting = result;
        }
        /// <summary>Set a sub-source category for <see cref="subSource"/>. Only applies for a source type of <see cref="SourceOverwrite.Content"/>.</summary>
        public void SetSourceSubCategory(SourceCategory category)
        {
            if (source == SourceOverwrite.Content)
                subSource = category;
        }

        public override string ToString()
        {
            string rloiStr = "(OI) ";
            /// (OI) {existing}|{overwriting}|{overwrittenQ};{ignoreOverwritten}[Ign/Ovr];{source}[-]{subSource?};
            if (IsSetup())
            {
                rloiStr += (contentExisting.IsNotNEW() ? contentExisting : "??") + "|";
                rloiStr += (contentOverwriting.IsNotNEW() ? contentOverwriting : "??") + "|";
                rloiStr += $"{OverwrittenQ};{(ignoreOverwriteQ ? "Ign" : "Ovr")};{source}";
                if (subSource.HasValue)
                    rloiStr += $"-{subSource.Value}";
            }
            else rloiStr += "??";
            return rloiStr;
        }
    }
}
