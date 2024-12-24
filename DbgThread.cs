using System.Collections.Generic;

namespace HCResourceLibraryApp
{
    /// <summary>A tool to assist in the threading funtionality of the <see cref="Dbg"/> class.</summary>
    internal class DbgThread
    {
        #region fields/props
        // private fields
        /// required
        string _name, _partial;
        int _index, _indent, _count;
        List<string> _logs;
        bool _activeQ;
        /// extras
        bool _outputOmissionQ;
        const int noIndex = -1, indentFactor = Dbg.indentFactor;



        // PROPERTIES
        /// <summary>The name of the thread's session. Ideally the class and the method being logged: '<c>Class.Method()</c>'.</summary>
        internal string Name { get => _name; set => _name = value; }
        /// <summary>Thread index (within list of threads). Greater than or equal to <c>0</c>.</summary>
        internal int Index { get => _index; set => _index = value < 0 ? 0 : value; }
        
        /// <summary>A suspended log.</summary>
        internal string Partial { get => _partial; set => _partial = value; }
        /// <summary>All debug strings logged to this thread while <see cref="IsActiveQ"/> is <c>true</c>.</summary>
        internal List<string> Logs { get => _logs; set => _logs = value; }
        /// <summary>Indent level of thread logs. Equivalent to indent level times <see cref="indentFactor"/> spaces. Greater than or equal to <c>0</c>.</summary>
        internal int Indent { get => _indent; set => _indent = value < 0 ? 0 : value; }
        /// <summary>States whether a thread's session is ongoing.</summary>
        internal bool IsActiveQ { get => _activeQ;  set => _activeQ = value; }
        /// <summary>Tally the times this debug thread was activated (sessions and single logs).</summary>
        internal int Count { get => _count; }


        /// <summary>Disables the thread logs from being printed to output. Will still be printed to file.</summary>
        internal bool OmitFromOutputQ { get => _outputOmissionQ; set => _outputOmissionQ = value; }
        #endregion



        // CONSTRUCTOR
        public DbgThread()
        {
            _logs = new List<string>();
            _index = noIndex;
        }
        public DbgThread(string name, int index)
        {
            if (name.IsNotNEW())
                _name = name;
            if (index >= 0)
                _index = index;
            else _index = noIndex;
            _logs = new List<string>();
        }




        // METHODS
        /// <summary>Has this instance of <see cref="DbgThread"/> been initialized with necessary information?</summary>
        /// <returns>A boolean stating whether name, index, and logs have a value.</returns>
        internal bool IsSetup()
        {
            return Name.IsNotNEW() && Index >= 0 && Logs != null;
        }
        /// <summary>
        ///     Handles adding log lines and indenting of this thread. 
        /// </summary>
        /// <param name="log">The log to add to this thread's logs.</param>
        /// <param name="isPartialQ">Whether to suspend this log or add it (and any other suspended logs) to this thread's logs.</param>
        internal void AddToLog(string log, bool isPartialQ = false)
        {
            if (IsSetup() && log.IsNotNEW())
            {
                string finalLogStr = null;
                if (isPartialQ)
                {
                    if (Partial.IsNotNEW())
                        Partial += log;
                    else Partial = log;
                }
                else
                {
                    finalLogStr = Partial ?? ""; // if left non-null, uses left, else uses right
                    finalLogStr += log;
                    Partial = null;
                }

                if (finalLogStr.IsNotNEW())
                    Logs.Add(finalLogStr.PadLeft((Indent * indentFactor) + finalLogStr.Length));

            }
        }
        /// <summary>Resets the thread to be ready for another session.</summary>
        /// <remarks>The following values remain unedited: <see cref="Name"/>, <see cref="Index"/>. The value of <see cref="Count"/> increments.</remarks>
        internal void Reset()
        {
            if (_activeQ)
                _count++;

            _logs = new List<string>();
            _partial = null;
            _indent = 0;
            _activeQ = false;
            _outputOmissionQ = false;
        }


        public override string ToString()
        {
            string toStr = "DbgThread // ";
            toStr += _name ?? "??";
            toStr += $" @{_index} ";
            toStr += $" [{(_logs.HasElements() ? _logs.Count : 0)}{(Partial.IsNotNEW() ? " 1/2" : "")} lns, {_indent} ind";
            toStr += $", {(_activeQ ? "Active" : "Off")}, {_count}sc]";
            return toStr;
        }
    }
}
