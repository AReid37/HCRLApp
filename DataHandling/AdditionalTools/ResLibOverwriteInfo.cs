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
        public readonly bool overwrittenQ;
        /// <summary>A specific difference between content existing and content overwriting (Name, ids...).</summary>
        //public readonly string changes;

        public ResLibOverwriteInfo(DataHandlerBase existing, DataHandlerBase overwritten, bool overwriteQ)
        {
            isSetupQ = true;
            if (existing != null)
                contentExisting = existing.ToString();
            else contentExisting = null;
            if (overwritten != null)
                contentOverwriting = overwritten.ToString();
            else contentOverwriting = null;
            //if (change.IsNotNEW())
            //    changes = change.Trim();
            //else changes = null;
            overwrittenQ = overwriteQ;
        }

        /// <returns>A boolean relaying whether a value for an existing or overwriting content has been provided.</returns>
        public bool IsSetup()
        {
            return isSetupQ && (contentExisting.IsNotNEW() || contentOverwriting.IsNotNEW());
        }
    }
}
