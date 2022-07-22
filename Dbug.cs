using System;
using System.Diagnostics;

namespace HCResourceLibraryApp
{
    /// <summary>
    ///     An intermediary, more focused version of the Debug Logger to Output
    /// </summary>
    class Dbug
    {
        /// Dbug.StartLogging()
        /// Dbug.EndLogging()


        public void LogLine()
        {
            Debug.WriteLine("message");
        }
    }
}
