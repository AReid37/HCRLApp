using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    public class SFormatterData : DataHandlerBase
    {
        /* STEAM FORMATTER DATA - PLANNING
        commonTag "frmt" 

        Fields / Props
        - const str formatterTag1, formatterTag2;
        - bl nativeColCodeQ, _prevNativeColCode
        - str _fName1, _fName2, _prevFName1, _prevFName2;
        - list<str> _lineData1, _lineData2, _prevLineData1, _prevLineData2;
        - pl UseNativeColorCode; get,set
        - pl FName1; get,set
        - pl FName2; get,set
        - pl LineData1; get
        - pl LineData2; get

        Constructors
        - SFD()

        Methods
        - vd AddLine(int?)
        - vd EditLine(int, str)
        - vd DeleteLine(int)
        - ovr EncodeToSharedFile(...)
        - ovr DecodeFromSharedFile(...)
        - bl ChangesDetected()
        - bl Equals(SFD)
        - vd SetPrevSelf()
        - SFD GetPrevSelf()
        - IsSetup1()
        - IsSetup2()         
         */

        #region fields/props
        // private
        readonly new string commonFileTag = "frmt";
        const string formatterTag1 = "frmt1", formatterTag2 = "frmt2";
        const string defaultName1 = "New Formatting 1", defaultName2 = "New Formatting 2", addNewLineText = "// +";
        bool _nativeColCodeQ, _prevNativeColCodeQ;
        string _fName1, _fName2, _prevFName1, _prevFName2;
        List<string> _lineData1, _lineData2, _prevLineData1, _prevLineData2;

        // public
        public bool UseNativeColorCodeQ
        {
            get => _nativeColCodeQ;
            set
            {
                if (_nativeColCodeQ != value)
                {
                    _prevNativeColCodeQ = _nativeColCodeQ;
                    _nativeColCodeQ = value;
                }                
            }
        }
        public string Name1
        {
            get
            {
                string name1 = defaultName1;
                if (_fName1.IsNotNEW())
                    name1 = _fName1;
                return name1;
            }
            set
            {
                _prevFName1 = _fName1;
                _fName1 = value;
            }
        }
        public string Name2
        {
            get
            {
                string name2 = defaultName2;
                if (_fName2.IsNotNEW())
                    name2 = _fName2;
                return name2;
            }
            set
            {
                _prevFName2 = _fName2;
                _fName2 = value;
            }
        }
        public List<string> LineData1 { get => _lineData1; }
        public List<string> LineData2 { get => _lineData2; }
        #endregion


        // CONSTRUCTORS
        public SFormatterData() { }


        #region methods
        /// <summary>Adds or inserts a new formatting line to formatting profile.</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will add or insert the line into formatting profile #1.</param>
        /// <param name="lineNum">The line number to insert a new line after. If <c>null</c>, will add a line at end of line data list.</param>
        /// <param name="reEditLine">Immediately edits the added line in line data. Used for undoing a deletion.</param>
        public void AddLine(bool isFormat1Q, int? lineNum, string reEditLine = null)
        {
            if (isFormat1Q)
            {
                //if (_lineData1 == null)
                //    _lineData1 = new List<string>();
                _lineData1 ??= new List<string>();
                int index;
                if (lineNum.HasValue && _lineData1.HasElements())
                {
                    index = lineNum.Value;
                    if (index.IsWithin(0, _lineData1.Count - 1))
                        _lineData1.Insert(index, addNewLineText);
                }
                else
                {
                    _lineData1.Add(addNewLineText);
                    index = _lineData1.Count;
                }
                EditLine(isFormat1Q, index + 1, reEditLine, out _);
            }
            else
            {
                //if (_lineData2 == null)
                //    _lineData2 = new List<string>();
                _lineData2 ??= new List<string>();

                int index;
                if (lineNum.HasValue && _lineData2.HasElements())
                {
                    index = lineNum.Value;
                    if (index.IsWithin(0, _lineData2.Count - 1))
                        _lineData2.Insert(index, addNewLineText);
                }
                else
                {
                    _lineData2.Add(addNewLineText);
                    index = _lineData2.Count;
                }
                EditLine(isFormat1Q, index + 1, reEditLine, out _);
            }
        }
        /// <summary>Edits the formatting line in the line data list of a formatting profile</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will edit the line data of formatting profile #1.</param>
        /// <param name="lineNum">The line number to edit.</param>
        /// <param name="editedLine">The edited formatting line that will replace the existing formatting line in line data list.</param>
        /// <param name="prevEditedLine">The previous formatting line that was replace. Is returned with a value if <paramref name="editedLine"/> has a value.</param>
        public void EditLine(bool isFormat1Q, int lineNum, string editedLine, out string prevEditedLine)
        {
            prevEditedLine = null;
            if (editedLine.IsNotNE())
            {
                int index = lineNum - 1;
                if (isFormat1Q && _lineData1.HasElements())
                {
                    if (index.IsWithin(0, _lineData1.Count - 1))
                    {
                        prevEditedLine = _lineData1[index];
                        _lineData1[index] = editedLine;
                    }
                }

                if (!isFormat1Q && _lineData2.HasElements())
                {
                    if (index.IsWithin(0, _lineData2.Count - 1))
                    {
                        prevEditedLine = _lineData2[index];
                        _lineData2[index] = editedLine;
                    }
                }
            }
        }
        /// <summary>Deletes a formatting line from the line data list of a formatting profile.</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will delete from line data of formatting profile #1.</param>
        /// <param name="lineNum">The line number to delete.</param>
        /// <param name="deletedLine">The formatting line value that was deleted.</param>
        public void DeleteLine(bool isFormat1Q, int lineNum, out string deletedLine)
        {
            deletedLine = null;
            int index = lineNum - 1;
            if (isFormat1Q && _lineData1.HasElements())
            {
                if (index.IsWithin(0, _lineData1.Count - 1))
                {
                    deletedLine = _lineData1[index];
                    _lineData1.RemoveAt(index);
                }
            }
            if (!isFormat1Q && _lineData2.HasElements())
            {
                if (index.IsWithin(0, _lineData2.Count - 1))
                {
                    deletedLine = _lineData2[index];
                    _lineData2.RemoveAt(index);
                }
            }
        }
        /// <summary>Compares two instances for similarities against: setup state, using native color, name1, name2, lineData1, lineData2.</summary>
        public bool Equals(SFormatterData sfd)
        {
            bool areEquals = false;
            if (sfd != null)
                areEquals = sfd.IsSetup() == IsSetup();

            if (areEquals)
            {
                for (int ax = 0; ax < 5 && areEquals; ax++)
                {
                    switch (ax)
                    {
                        case 0:
                            areEquals = UseNativeColorCodeQ == sfd.UseNativeColorCodeQ;
                            break;

                        case 1:
                            areEquals = Name1 == sfd.Name1;
                            break;

                        case 2:
                            areEquals = Name2 == sfd.Name2;
                            break;

                        case 3:
                            areEquals = IsSetup(true) == sfd.IsSetup(true);
                            if (LineData1.HasElements() && areEquals)
                            {
                                areEquals = LineData1.Count == sfd.LineData1.Count;
                                if (areEquals)
                                    for (int l1x = 0; l1x < LineData1.Count && areEquals; l1x++)
                                        areEquals = LineData1[l1x].Equals(sfd.LineData1[l1x]);
                            }
                            break;

                        case 4:
                            areEquals = IsSetup(false) == sfd.IsSetup(false);
                            if (LineData2.HasElements() && areEquals)
                            {
                                areEquals = LineData2.Count == sfd.LineData2.Count;
                                if (areEquals)
                                    for (int l2x = 0; l2x < LineData2.Count && areEquals; l2x++)
                                        areEquals = LineData2[l2x] == sfd.LineData2[l2x];
                            }
                            break;
                    }
                }
            }

            return areEquals;
        }
        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }
        /// <param name="isFormat1Q">If <c>true</c>, checks if formatting profile 1 has data.</param>
        /// <returns>A boolean stating if a formatting profile has elements in its line data list.</returns>
        public bool IsSetup(bool isFormat1Q)
        {
            return isFormat1Q? _lineData1.HasElements() : _lineData2.HasElements();
        }
        /// <returns>A boolean stating if any formatting profile has elements in its line Data list.</returns>
        public override bool IsSetup()
        {
            return _lineData1.HasElements() || _lineData2.HasElements();
        }
        void SetPreviousSelf()
        {
            _prevNativeColCodeQ = _nativeColCodeQ;
            _prevFName1 = _fName1;
            _prevFName2 = _fName2;

            if (_lineData1.HasElements())
            {
                _prevLineData1 = new List<string>();
                _prevLineData1.AddRange(_lineData1.ToArray());
            }
            if (_lineData2.HasElements())
            {
                _prevLineData2 = new List<string>();
                _prevLineData2.AddRange(_lineData2.ToArray());
            }
        }
        SFormatterData GetPreviousSelf()
        {
            SFormatterData prevSelf = new();
            prevSelf._nativeColCodeQ = _prevNativeColCodeQ;
            prevSelf._fName1 = _prevFName1;
            prevSelf._fName2 = _prevFName2;
            if (_prevLineData1.HasElements())
            {
                prevSelf._lineData1 = new List<string>();
                prevSelf._lineData1.AddRange(_prevLineData1.ToArray());
            }
            if (_prevLineData2.HasElements())
            {
                prevSelf._lineData2 = new List<string>();
                prevSelf._lineData2.AddRange(_prevLineData2.ToArray());
            }

            return prevSelf;
        }


        // DATA HANDLING
        protected override bool EncodeToSharedFile()
        {
            /// file encoding syntax
            /// tag "frmt"    
            ///     - usingNative {bool}
            /// tag "frmt1"
            ///     - name1 {string}
            ///     - lineData1 {lines of strings}
            /// tag "frmt2"
            ///     - name2 {string}
            ///     - lineData2 {lines of strings}

            Dbug.StartLogging("SFormatterData.EncodeToSharedFile()");
            bool noIssuesQ = true;
            for (int enx = 0; enx < 3 && noIssuesQ; enx++)
            {
                switch (enx)
                {
                    case 0:
                        noIssuesQ = Base.FileWrite(false, commonFileTag, _nativeColCodeQ.ToString());
                        Dbug.Log($"Encoded 'Use Native Color Code' :: {_nativeColCodeQ}");
                        break;

                    case 1:
                        Dbug.Log("Encoding Formattig profile 1; ");
                        noIssuesQ = Base.FileWrite(false, formatterTag1, _fName1.IsNEW() ? defaultName1 : _fName1);
                        if (noIssuesQ)
                        {
                            Dbug.Log($" + Encoded Name :: {Name1}; ");
                            if (_lineData1.HasElements())
                            {
                                noIssuesQ = Base.FileWrite(false, formatterTag1, _lineData1.ToArray());
                                for (int l1x = 0; l1x < _lineData1.Count && noIssuesQ; l1x++)
                                    Dbug.Log($" + Encoded line {l1x + 1} :: {_lineData1[l1x]}; ");
                            }
                            else Dbug.Log($" + No line data to encode; ");
                        }
                        break;

                    case 2:
                        Dbug.Log("Encoding Formattig profile 2; ");
                        noIssuesQ = Base.FileWrite(false, formatterTag2, _fName2.IsNEW() ? defaultName2 : _fName2);
                        if (noIssuesQ)
                        {
                            Dbug.Log($" + Encoded Name :: {Name2}; ");
                            if (_lineData2.HasElements())
                            {
                                noIssuesQ = Base.FileWrite(false, formatterTag2, _lineData2.ToArray());
                                for (int l2x = 0; l2x < _lineData2.Count && noIssuesQ; l2x++)
                                    Dbug.Log($" + Encoded line {l2x + 1} :: {_lineData1[l2x]}; ");
                            }
                            else Dbug.Log($" + No line data to encode; ");
                        }
                        break;
                }

                if (!noIssuesQ)
                    Dbug.Log($"Encountered an error while saving (stage #{enx+1}).");
            }

            if (noIssuesQ)
                SetPreviousSelf();

            Dbug.EndLogging();
            return noIssuesQ;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.StartLogging("SFormatterData.DecodeFromSharedFile()");
            bool decodedQ = true, crossCompatibilityIssue = false;
            for (int dcx = 0; dcx < 3 && decodedQ; dcx++)
            {
                switch (dcx)
                {
                    case 0:
                        Dbug.LogPart("Fetching 'Use Native Color Code'");
                        decodedQ = Base.FileRead(commonFileTag, out string[] frmtData);
                        if (decodedQ && frmtData.HasElements())
                        {
                            if (bool.TryParse(frmtData[0], out bool useNativeQ))
                            {
                                Dbug.LogPart($"; Fetched 'Use Native Color Code' :: {useNativeQ}");
                                _nativeColCodeQ = useNativeQ;
                            }
                            else Dbug.LogPart($"; Value could not be parsed");
                        }
                        else
                        {
                            crossCompatibilityIssue = true;
                            Dbug.LogPart($"; Could not fetch value");
                        }
                        Dbug.Log("; ");
                        break;

                    case 1:
                        Dbug.Log("Fetching Formatting Profile 1 data; ");
                        decodedQ = Base.FileRead(formatterTag1, out string[] frmt1Data);
                        if (decodedQ && frmt1Data.HasElements())
                        {
                            for (int fx1 = 0; fx1 < frmt1Data.Length; fx1++)
                            {
                                string f1Data = frmt1Data[fx1];
                                if (fx1 == 0)
                                    _fName1 = f1Data == defaultName1 ? null : f1Data;
                                else
                                {
                                    if (_lineData1 == null)
                                        _lineData1 = new List<string>();
                                    _lineData1.Add(f1Data);
                                }

                                Dbug.Log($" + Decoded {(fx1 == 0 ? "Name" : $"Line {fx1}")} :: {f1Data}");
                            }
                        }                        
                        else Dbug.Log($" -> Could not fetch formatting profile data; ");
                        break;

                    case 2:
                        Dbug.Log("Fetching Formatting Profile 2 data; ");
                        decodedQ = Base.FileRead(formatterTag2, out string[] frmt2Data);
                        if (decodedQ && frmt2Data.HasElements())
                        {
                            for (int fx2 = 0; fx2 < frmt2Data.Length; fx2++)
                            {
                                string f2Data = frmt2Data[fx2];
                                if (fx2 == 0)
                                    _fName2 = f2Data == defaultName2 ? null : f2Data;
                                else
                                {
                                    if (_lineData2 == null)
                                        _lineData2 = new List<string>();
                                    _lineData2.Add(f2Data);
                                }

                                Dbug.Log($" + Decoded {(fx2 == 0 ? "Name" : $"Line {fx2}")} :: {f2Data}");
                            }
                        }
                        else Dbug.Log($" -> Could not fetch formatting profile data; ");
                        break;
                }
            }

            if (crossCompatibilityIssue)
            {
                Dbug.Log($"Cross-compatibility issue: previous versions do not contain guaranteed line for 'SFormatterData:UseNativeColorCode'; Decoding is okay'd; ");
                decodedQ = true;
            }

            SetPreviousSelf();
            Dbug.EndLogging();
            return decodedQ;
        }
        #endregion
    }
}
