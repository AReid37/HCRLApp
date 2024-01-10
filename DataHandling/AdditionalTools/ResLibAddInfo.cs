using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Relays information regarding the addition of contents, legends, and summaries for a version log submission.</summary>
    public struct ResLibAddInfo
    {
        public readonly bool isSetupQ;
        public readonly SourceOverwrite source;
        public readonly bool addedQ;
        public readonly string addedObject;
        public readonly bool dupeRejectionQ;
    }
}
