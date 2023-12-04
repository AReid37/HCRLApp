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
        bool? overwrittenQ;
        public bool ignoreOverwriteQ;
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
            if (existing != null)
                contentExisting = existing.ToString();
            else contentExisting = null;
            if (overwriting != null)
                contentOverwriting = overwriting.ToString();
            else contentOverwriting = null;
            overwrittenQ = null;
            ignoreOverwriteQ = false;
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
        /// <summary>Set a sub-source category for <see cref="subSource"/>. Only applies for a source type of <see cref="SourceOverwrite.Content"/>.</summary>
        public void SetSourceSubCategory(SourceCategory category)
        {
            if (source == SourceOverwrite.Content)
                subSource = category;
        }

        public override string ToString()
        {
            string rloiStr = "(OI) ";
            /// (OI) {existing}|{overwriting}|{overwrittenQ}{ignoreOverwritten}[Ign/Ovr];{source}[-]{subSource?};
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
