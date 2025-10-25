using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        bool _nativeColCodeQ, _prevNativeColCodeQ, _editProf1Q, _prevEditProf1Q;
        string _fName1, _fName2, _prevFName1, _prevFName2;
        List<string> _lineData1, _lineData2, _prevLineData1, _prevLineData2;
        List<SfdGroupInfo> _groupInfo1, _groupInfo2, _prevGroupInfo1, _prevGroupInfo2;

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
        public bool EditProfileNo1Q
        {
            get => _editProf1Q;
            set
            {
                if (_editProf1Q != value)
                {
                    _prevEditProf1Q = _editProf1Q;
                    _editProf1Q = value;
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
        public List<string> LineData1 
        {
            get => _lineData1.HasElements() ? _lineData1 : new List<string>(); 
        }
        public List<string> LineData2
        {
            get => _lineData2.HasElements() ? _lineData2 : new List<string>();
        }
        #endregion


        // CONSTRUCTORS
        public SFormatterData()
        {
            _editProf1Q = true;
            _prevEditProf1Q = true;
        }


        #region methods
        // Line Methods
        /// <summary>Adds or inserts a new formatting line to formatting profile.</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will add or insert the line into formatting profile #1.</param>
        /// <param name="lineNum">The line number to insert a new line after. If <c>null</c>, will add a line at end of line data list.</param>
        /// <param name="reEditLine">Immediately edits the added line in line data. Used for undoing a deletion.</param>
        public void AddLine(int? lineNum, string reEditLine = null)
        {            
            if (EditProfileNo1Q)
            {
                //if (_lineData1 == null)
                //    _lineData1 = new List<string>();
                _lineData1 ??= new List<string>();
                int index;
                if (lineNum.HasValue && _lineData1.HasElements())
                {
                    index = lineNum.Value - 1;
                    if (index.IsWithin(0, _lineData1.Count - 1))
                        _lineData1.Insert(index, addNewLineText);
                }
                else
                {
                    index = _lineData1.Count;
                    _lineData1.Add(addNewLineText);
                }
                EditLine(index + 1, reEditLine, out _);
                EditGroupInfos(index + 1, true);
            }
            else
            {
                //if (_lineData2 == null)
                //    _lineData2 = new List<string>();
                _lineData2 ??= new List<string>();
                int index;
                if (lineNum.HasValue && _lineData2.HasElements())
                {
                    index = lineNum.Value - 1;
                    if (index.IsWithin(0, _lineData2.Count - 1))
                        _lineData2.Insert(index, addNewLineText);
                }
                else
                {
                    index = _lineData2.Count;
                    _lineData2.Add(addNewLineText);
                }
                EditLine(index + 1, reEditLine, out _);
                EditGroupInfos(index + 1, true);
            }
        }
        /// <summary>Edits the formatting line in the line data list of a formatting profile</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will edit the line data of formatting profile #1.</param>
        /// <param name="lineNum">The line number to edit.</param>
        /// <param name="editedLine">The edited formatting line that will replace the existing formatting line in line data list.</param>
        /// <param name="prevEditedLine">The previous formatting line that was replace. Is returned with a value if <paramref name="editedLine"/> has a value.</param>
        public void EditLine(int lineNum, string editedLine, out string prevEditedLine)
        {
            prevEditedLine = null;
            if (editedLine.IsNotNE())
            {
                int index = lineNum - 1;
                if (EditProfileNo1Q && _lineData1.HasElements())
                {
                    if (index.IsWithin(0, _lineData1.Count - 1))
                    {
                        prevEditedLine = _lineData1[index];
                        _lineData1[index] = editedLine;
                    }
                }

                if (!EditProfileNo1Q && _lineData2.HasElements())
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
        public void DeleteLine(int lineNum, out string deletedLine)
        {
            deletedLine = null;
            int index = lineNum - 1;
            if (EditProfileNo1Q && _lineData1.HasElements())
            {
                if (index.IsWithin(0, _lineData1.Count - 1))
                {
                    deletedLine = _lineData1[index];
                    _lineData1.RemoveAt(index);
                    EditGroupInfos(lineNum, false);
                }
            }
            if (!EditProfileNo1Q && _lineData2.HasElements())
            {
                if (index.IsWithin(0, _lineData2.Count - 1))
                {
                    deletedLine = _lineData2[index];
                    _lineData2.RemoveAt(index);
                    EditGroupInfos(lineNum, false);
                }
            }
        }
        /// <summary>Fetches the formatting line from the line data list of a formattng profile.</summary>
        /// <param name="isFormat1Q">If <c>true</c>, will delete from line data of formatting profile #1.</param>
        /// <param name="lineNum">The line number to fetch.</param>
        /// <returns>The formatting profile's value from <paramref name="lineNum"/> in line data list. Returns <c>null</c> if line does not exist.</returns>
        public string GetLine(int lineNum)
        {
            string lineData = null;
            int index = lineNum - 1;
            if (EditProfileNo1Q && _lineData1.HasElements())
            {
                if (index.IsWithin(0, _lineData1.Count - 1))
                    lineData = _lineData1[index];
            }
            if (!EditProfileNo1Q && _lineData2.HasElements())
            {
                if (index.IsWithin(0, _lineData2.Count - 1))
                    lineData = _lineData2[index];
            }
            return lineData;
        }

        // Group Info Methods
        /// <summary>Creates a new line group for a given formatting profile.</summary>
        /// <param name="groupName">Name of group.</param>
        /// <param name="startLine">Starting line of group line.</param>
        /// <param name="endLine">Ending line of group line.</param>
        public void CreateGroup(string groupName, int startLine, int endLine)
        {
            SfdGroupInfo newGroup = new(groupName, startLine, endLine);
            if (newGroup.IsSetup())
            {
                bool isDupeQ = false;
                if (EditProfileNo1Q)
                {
                    _groupInfo1 ??= new List<SfdGroupInfo>();
                    if (_groupInfo1.HasElements())
                        foreach (SfdGroupInfo fp1Group in _groupInfo1)
                            if (fp1Group.Equals(newGroup))
                            {
                                isDupeQ = true;
                                break;
                            }

                    if (!isDupeQ)
                        _groupInfo1.Add(newGroup);
                }
                else
                {
                    _groupInfo2 ??= new List<SfdGroupInfo>();
                    if (_groupInfo2.HasElements())
                        foreach (SfdGroupInfo fp2Group in _groupInfo2)
                            if (fp2Group.Equals(newGroup))
                            {
                                isDupeQ = true;
                                break;
                            }

                    if (!isDupeQ)
                        _groupInfo2.Add(newGroup);
                }
            }
        }
        /// <summary>Checks if a group with a given name exists within the formatting line data of a formatter profile.</summary>
        /// <param name="groupName"></param>
        /// <returns>A boolean confirming the existence of a named group.</returns>
        public bool GroupExists(string groupName)
        {
            bool existsQ = false;
            if (groupName.IsNotNEW())
            {
                if (EditProfileNo1Q && _groupInfo1.HasElements())
                {
                    for (int g1x = 0; g1x < _groupInfo1.Count && !existsQ; g1x++)
                    {
                        existsQ = _groupInfo1[g1x].groupName == groupName;
                    }
                }
                if (!EditProfileNo1Q && _groupInfo2.HasElements())
                {
                    for (int g2x = 0; g2x < _groupInfo2.Count && !existsQ; g2x++)
                    {
                        existsQ = _groupInfo2[g2x].groupName == groupName;
                    }
                }
            }
            return existsQ;
        }
        /// <summary>Removes a line group from a formatting profile.</summary>
        /// <param name="groupName"></param>
        public void DeleteGroup(string groupName, out SfdGroupInfo deletedGroup)
        {
            deletedGroup = null;
            if (groupName.IsNotNEW())
            {
                bool deletedQ = false;
                if (EditProfileNo1Q && _groupInfo1.HasElements())
                {
                    for (int g1x = 0; g1x < _groupInfo1.Count && !deletedQ; g1x++)
                    {
                        if (_groupInfo1[g1x].groupName == groupName)
                        {
                            deletedQ = true;
                            deletedGroup = _groupInfo1[g1x];
                            _groupInfo1.RemoveAt(g1x);
                        }
                    }
                }
                if (!EditProfileNo1Q && _groupInfo2.HasElements())
                {
                    for (int g2x = 0; g2x < _groupInfo2.Count && !deletedQ; g2x++)
                    {
                        if (_groupInfo2[g2x].groupName == groupName)
                        {
                            deletedQ = true;
                            deletedGroup = _groupInfo2[g2x];
                            _groupInfo2.RemoveAt(g2x);
                        }
                    }
                }
            }
        }
        /// <summary>Toggles a named group's expansion state (expand or collapse).</summary>
        /// <param name="groupName"></param>
        /// <returns>A boolean that reflects the line group's new expansion state (is expanded if <c>true</c>).</returns>
        public bool? ToggleGroupExpansion(string groupName)
        {
            bool? expansionState = null;
            if (groupName.IsNotNE())
            {
                if (EditProfileNo1Q && _groupInfo1.HasElements())
                {
                    for (int g1x = 0; g1x < _groupInfo1.Count; g1x++)
                    {
                        if (_groupInfo1[g1x].groupName == groupName)
                        {
                            _groupInfo1[g1x].isExpandedQ = !_groupInfo1[g1x].isExpandedQ;
                            expansionState = _groupInfo1[g1x].isExpandedQ;
                        }
                    }
                }
                if (!EditProfileNo1Q && _groupInfo2.HasElements())
                {
                    for (int g2x = 0; g2x < _groupInfo2.Count; g2x++)
                    {
                        if (_groupInfo2[g2x].groupName == groupName)
                        {
                            _groupInfo2[g2x].isExpandedQ = !_groupInfo2[g2x].isExpandedQ;
                            expansionState = _groupInfo2[g2x].isExpandedQ;
                        }
                    }
                }
            }
            return expansionState;
        }
        /// <summary>Checks if a formatting line in the line data list is within a line group. If the line is in a line group, details about the group are returned.</summary>
        /// <param name="lineNum">The formatting line to check as being within a group.</param>
        /// <param name="groupName">Name of group formatting line exists in.</param>
        /// <param name="position">Formatting line's position in group: <c>1</c> if at group starting line, <c>-1</c> if at group ending line, <c>0</c> if within group bounding lines.</param>
        /// <param name="isExpandedQ">The group's expansion state (expanded or collapsed).</param>
        /// <returns>A boolean determining the existence of this line within a named group. Also determines if group info will be returned.</returns>
        public bool IsLineInGroup(int lineNum, out string groupName, out int position, out bool isExpandedQ)
        {
            bool isLineInGroupQ = false;
            groupName = null;
            position = 0;
            isExpandedQ = true;
            if (IsLineInGroup(lineNum, out SfdGroupInfo groupInfo))
            {
                isLineInGroupQ = true;
                groupName = groupInfo.groupName;
                position = lineNum == groupInfo.startLineNum ? 1 : (lineNum == groupInfo.endLineNum ? -1 : 0);
                isExpandedQ = groupInfo.isExpandedQ;
            }
            return isLineInGroupQ;
        }
        /// <summary>Checks if a formatting line in the line data list is within a line group. If the line is in a line group, details about the group are returned.</summary>
        /// <param name="lineNum">The formatting line to check as being within a group.</param>
        /// <param name="lineGroup">The group the formatting line exists within.</param>
        /// <returns>A boolean determining the existence of this line within a named group. Also determines if group info will be returned.</returns>
        bool IsLineInGroup(int lineNum, out SfdGroupInfo lineGroup)
        {
            lineGroup = null;
            if (EditProfileNo1Q && _groupInfo1.HasElements())
            {
                for (int g1x = 0; g1x < _groupInfo1.Count && lineGroup == null; g1x++)
                {
                    SfdGroupInfo group1s = _groupInfo1[g1x];
                    if (lineNum.IsWithin(group1s.startLineNum, group1s.endLineNum))
                        lineGroup = group1s;
                }
            }
            if (!EditProfileNo1Q && _groupInfo2.HasElements())
            {
                for (int g2x = 0; g2x < _groupInfo2.Count && lineGroup == null; g2x++)
                {
                    SfdGroupInfo group2s = _groupInfo2[g2x];
                    if (lineNum.IsWithin(group2s.startLineNum, group2s.endLineNum))
                        lineGroup = group2s;
                }
            }
            return lineGroup != null;
        }
        /// <summary>
        ///     Edits a line group's information dependent on an add or delete action.
        /// </summary>
        void EditGroupInfos(int lineNum, bool lineAddedQ)
        {
            /** Revised group management plan
                Where '->' represent 'if' and '=>' represent 'else'

                -> ADD LINE
                    -> Line is before or at group start
                        -> line is at group start
                            -> group start is top-most line or previous line is in different group
                                shift group down 
                            => extend group end
                        => line is before start 
                            shift group down
                    
                    -> Line is within group and not at start
                        extend group end

                    => (Line is after group, do nothing)


                -> DELETE LINE
                    -> Line is before or at group start
                        -> line is at group start
                            -> group start is top-most line or previous line is in different group
                                -> group has only two lines
                                    destroy group
                                => retract group end
                            => -> group has only two lines
                                    destroy group
                               => retract group end
                        => line is before start
                            shift group up
                        
                    -> Line is within group and not at start
                        -> group has only two lines
                            destroy group 
                        => retract group end

                    => (Line is after group, do nothing)
             ********/
            int countDestroyedGroups = 0;
            bool previousLineDNE;
            if (EditProfileNo1Q && _groupInfo1.HasElements())
            {
                for (int g1x = 0; g1x - countDestroyedGroups < _groupInfo1.Count; g1x++)
                {
                    SfdGroupInfo group1s = _groupInfo1[g1x - countDestroyedGroups];
                    if (lineNum <= group1s.startLineNum)
                    {
                        if (lineNum == group1s.startLineNum)
                        {
                            previousLineDNE = group1s.startLineNum == 1 || IsLineInGroup(lineNum - 1, out _);
                            if (previousLineDNE)
                            {
                                if (lineAddedQ)
                                {
                                    group1s.startLineNum += 1;
                                    group1s.endLineNum += 1;
                                }
                                else
                                {
                                    if (group1s.startLineNum + 1 == group1s.endLineNum)
                                    {
                                        _groupInfo1.RemoveAt(g1x);
                                        countDestroyedGroups++;
                                    }
                                    else group1s.endLineNum -= 1;
                                }
                            }
                            else
                            {
                                if (lineAddedQ)
                                    group1s.endLineNum += 1;
                                else
                                {
                                    if (group1s.startLineNum + 1 == group1s.endLineNum)
                                    {
                                        _groupInfo1.RemoveAt(g1x);
                                        countDestroyedGroups++;
                                    }
                                    else group1s.endLineNum -= 1;
                                }
                            }
                        }
                        else
                        {
                            if (lineAddedQ)
                            {
                                group1s.startLineNum += 1;
                                group1s.endLineNum += 1;
                            }
                            else
                            {
                                group1s.startLineNum -= 1;
                                group1s.endLineNum -= 1;
                            }
                        }
                    }
                    else if (lineNum.IsWithin(group1s.startLineNum, group1s.endLineNum))
                    {
                        if (lineAddedQ)
                            group1s.endLineNum += 1;
                        else
                        {
                            if (group1s.startLineNum + 1 == group1s.endLineNum)
                            {
                                _groupInfo1.RemoveAt(g1x);
                                countDestroyedGroups++;
                            }
                            else group1s.endLineNum -= 1;
                        }
                    }
                }   
            }
            if (!EditProfileNo1Q && _groupInfo2.HasElements())
            {
                for (int g2x = 0; g2x - countDestroyedGroups < _groupInfo2.Count; g2x++)
                {
                    SfdGroupInfo group2s = _groupInfo2[g2x - countDestroyedGroups];
                    if (lineNum <= group2s.startLineNum)
                    {
                        if (lineNum == group2s.startLineNum)
                        {
                            previousLineDNE = group2s.startLineNum == 1 || IsLineInGroup(lineNum - 1, out _);
                            if (previousLineDNE)
                            {
                                if (lineAddedQ)
                                {
                                    group2s.startLineNum += 1;
                                    group2s.endLineNum += 1;
                                }
                                else
                                {
                                    if (group2s.startLineNum + 1 == group2s.endLineNum)
                                    {
                                        _groupInfo2.RemoveAt(g2x);
                                        countDestroyedGroups++;
                                    }
                                    else group2s.endLineNum -= 1;
                                }
                            }
                            else
                            {
                                if (lineAddedQ)
                                    group2s.endLineNum += 1;
                                else
                                {
                                    if (group2s.startLineNum + 1 == group2s.endLineNum)
                                    {
                                        _groupInfo2.RemoveAt(g2x);
                                        countDestroyedGroups++;
                                    }
                                    else group2s.endLineNum -= 1;
                                }
                            }
                        }
                        else
                        {
                            if (lineAddedQ)
                            {
                                group2s.startLineNum += 1;
                                group2s.endLineNum += 1;
                            }
                            else
                            {
                                group2s.startLineNum -= 1;
                                group2s.endLineNum -= 1;
                            }
                        }
                    }
                    else if (lineNum.IsWithin(group2s.startLineNum, group2s.endLineNum))
                    {
                        if (lineAddedQ)
                            group2s.endLineNum += 1;
                        else
                        {
                            if (group2s.startLineNum + 1 == group2s.endLineNum)
                            {
                                _groupInfo2.RemoveAt(g2x);
                                countDestroyedGroups++;
                            }
                            else group2s.endLineNum -= 1;
                        }
                    }
                }
            }

            /// [OLD] when to edit line group?
            ///     -> if a line group exists
            ///     ----
            ///     -> if line number is within group: ON 'Add' (start inclusive, end inclusive: grow end range by 1 [within(start,end)])
            ///     -> if line number is out of group: ON 'Add' (line before start, shift range down 1)
            ///     -----
            ///     -> if line number is within group: ON 'Del' (start inclusive, end inclusive, end-1 > start: shrink end range by 1 [within(start,end)])
            ///     -> if line number is out of group: ON 'Del' (line before start, shift range up 1)
            #region old group management code
            ///// IF line is within group: 
            /////     IF add line: (IF line is at group start: shift group down; ELSE IF group end exists: extend group end;);
            /////     ELSE (IF more than two lines in group: retract group end; ELSE destroy group) 
            //if (lineNum.IsWithin(group2s.startLineNum, group2s.endLineNum))
            //{
            //    if (lineAddedQ)
            //    {
            //        if (lineNum == group2s.startLineNum)
            //        {
            //            group2s.startLineNum += 1;
            //            group2s.endLineNum += 1;
            //        }
            //        else if (lineCount > group2s.endLineNum)
            //            group2s.endLineNum += 1;
            //    }
            //    else
            //    {
            //        if (group2s.endLineNum - 1 > group2s.startLineNum)
            //            group2s.endLineNum -= 1;
            //        else
            //        {
            //            _groupInfo2.RemoveAt(g2x);
            //            countDestroyedGroups++;
            //        }
            //    }
            //}
            ///// ELSE IF line is before group start and group end exists (IF add line: shift group down; ELSE shift group up;)
            //else if (lineNum < group2s.startLineNum && lineCount >= group2s.endLineNum)
            //{
            //    if (lineAddedQ)
            //    {
            //        group2s.startLineNum += 1;
            //        group2s.endLineNum += 1;
            //    }
            //    else
            //    {
            //        group2s.startLineNum -= 1;
            //        group2s.endLineNum -= 1;
            //    }
            //}
            ///// ELSE IF group start is at last line and delete line: destroy group;
            //else if (lineCount <= group2s.startLineNum && !lineAddedQ)
            //{
            //    _groupInfo2.RemoveAt(g2x);
            //    countDestroyedGroups++;
            //}
            #endregion
        }


        /// <summary>Compares two instances for similarities against: setup state, using native color, name1, name2, lineData1, lineData2, groupInfo1, groupInfo2.</summary>
        public bool Equals(SFormatterData sfd)
        {
            bool areEquals = false;
            if (sfd != null)
                areEquals = sfd.IsSetup() == IsSetup();

            if (areEquals)
            {
                for (int ax = 0; ax < 7 && areEquals; ax++)
                {
                    switch (ax)
                    {
                        case 0:
                            areEquals = UseNativeColorCodeQ == sfd.UseNativeColorCodeQ;
                            if (areEquals)
                                areEquals = EditProfileNo1Q == sfd.EditProfileNo1Q;
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


                        case 5:
                            areEquals = _groupInfo1.HasElements() == sfd._groupInfo1.HasElements();
                            if (_groupInfo1.HasElements())
                            {
                                areEquals = _groupInfo1.Count == sfd._groupInfo1.Count;
                                if (areEquals)
                                    for (int g1x = 0; g1x < _groupInfo1.Count && areEquals; g1x++)
                                        areEquals = _groupInfo1[g1x].Equals(sfd._groupInfo1[g1x]);
                            }
                            break;

                        case 6:
                            areEquals = _groupInfo2.HasElements() == sfd._groupInfo2.HasElements();
                            if (_groupInfo2.HasElements())
                            {
                                areEquals = _groupInfo2.Count == sfd._groupInfo2.Count;
                                if (areEquals)
                                    for (int g2x = 0; g2x < _groupInfo2.Count && areEquals; g2x++)
                                        areEquals = _groupInfo2[g2x].Equals(sfd._groupInfo2[g2x]);
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
            _prevEditProf1Q = _editProf1Q;
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
            if (_groupInfo1.HasElements())
            {
                _prevGroupInfo1 = new List<SfdGroupInfo>();
                _prevGroupInfo1.AddRange(_groupInfo1.ToArray());
            }
            if (_groupInfo2.HasElements())
            {
                _prevGroupInfo2 = new List<SfdGroupInfo>();
                _prevGroupInfo2.AddRange(_groupInfo2.ToArray());
            }
        }
        SFormatterData GetPreviousSelf()
        {
            SFormatterData prevSelf = new();
            prevSelf._nativeColCodeQ = _prevNativeColCodeQ;
            prevSelf._editProf1Q = _prevEditProf1Q;
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
            if (_prevGroupInfo1.HasElements())
            {
                prevSelf._groupInfo1 = new List<SfdGroupInfo>();
                prevSelf._groupInfo1.AddRange(_prevGroupInfo1.ToArray());
            }
            if (_prevGroupInfo2.HasElements())
            {
                prevSelf._groupInfo2 = new List<SfdGroupInfo>();
                prevSelf._groupInfo2.AddRange(_prevGroupInfo2.ToArray());
            }

            return prevSelf;
        }


        // DATA HANDLING
        protected override bool EncodeToSharedFile()
        {
            /// file encoding syntax
            /// tag "frmt"    
            ///     - usingNative {bool}
            ///     - editProf1 {bool}
            ///     - groupInfo1 1***{group1Info}***
            ///     - groupInfo2 2***{group2Info}***
            /// tag "frmt1"
            ///     - name1 {string}
            ///     - lineData1 {lines of strings}
            /// tag "frmt2"
            ///     - name2 {string}
            ///     - lineData2 {lines of strings}

            Dbg.StartLogging("SFormatterData.EncodeToSharedFile()", out int sfdx);
            bool noIssuesQ = true;
            for (int enx = 0; enx < 3 && noIssuesQ; enx++)
            {
                switch (enx)
                {
                    /// frmt -> usingNative, editProf1, group Infos 1 & 2
                    case 0:
                        const string triSep = Sep + Sep + Sep;
                        for (int f0x = 0; f0x < 4 && noIssuesQ; f0x++)
                        {
                            switch (f0x)
                            {
                                case 0:
                                    noIssuesQ = Base.FileWrite(false, commonFileTag, _nativeColCodeQ.ToString());
                                    Dbg.Log(sfdx, $"Encoded 'Use Native Color Code Q' :: {_nativeColCodeQ}");
                                    break;

                                case 1:
                                    noIssuesQ = Base.FileWrite(false, commonFileTag, _editProf1Q.ToString());
                                    Dbg.Log(sfdx, $"Encoded 'Edit Profile 1 Q' :: {_editProf1Q}");
                                    break;

                                case 2:
                                    string group1DataLine = "GOnes";
                                    if (_groupInfo1.HasElements())
                                    {
                                        Dbg.Log(sfdx, "Encoding Profile 1 Groups; ");
                                        foreach (SfdGroupInfo group1s in _groupInfo1)
                                        {
                                            group1DataLine += $"{triSep}{group1s.Encode()}";
                                            Dbg.Log(sfdx, $" + Encoded :: {group1s}");
                                        }
                                    }
                                    noIssuesQ = Base.FileWrite(false, commonFileTag, group1DataLine);
                                    break;

                                case 3:
                                    string group2DataLine = "GTwos";
                                    if (_groupInfo2.HasElements())
                                    {
                                        Dbg.Log(sfdx, "Encoding Profile 2 Groups; ");
                                        foreach (SfdGroupInfo group2s in _groupInfo2)
                                        {
                                            group2DataLine += $"{triSep}{group2s.Encode()}";
                                            Dbg.Log(sfdx, $" + Encoded :: {group2s}");
                                        }
                                    }
                                    noIssuesQ = Base.FileWrite(false, commonFileTag, group2DataLine);
                                    break;
                            }
                        }
                        break;

                    /// frmt1 -> formatting prof 1 lines
                    case 1:
                        Dbg.Log(sfdx, "Encoding Formatting profile 1; ");
                        noIssuesQ = Base.FileWrite(false, formatterTag1, _fName1.IsNEW() ? defaultName1 : _fName1);
                        if (noIssuesQ)
                        {
                            Dbg.Log(sfdx, $" + Encoded Name :: {Name1}; ");
                            if (_lineData1.HasElements())
                            {
                                noIssuesQ = Base.FileWrite(false, formatterTag1, _lineData1.ToArray());
                                for (int l1x = 0; l1x < _lineData1.Count && noIssuesQ; l1x++)
                                    Dbg.Log(sfdx, $" + Encoded line {l1x + 1} :: {_lineData1[l1x]}; ");
                            }
                            else Dbg.Log(sfdx, $" + No line data to encode; ");
                        }
                        break;
                    
                    /// frmt2 -> formatting profile 2 lines
                    case 2:
                        Dbg.Log(sfdx, "Encoding Formatting profile 2; ");
                        noIssuesQ = Base.FileWrite(false, formatterTag2, _fName2.IsNEW() ? defaultName2 : _fName2);
                        if (noIssuesQ)
                        {
                            Dbg.Log(sfdx, $" + Encoded Name :: {Name2}; ");
                            if (_lineData2.HasElements())
                            {
                                noIssuesQ = Base.FileWrite(false, formatterTag2, _lineData2.ToArray());
                                for (int l2x = 0; l2x < _lineData2.Count && noIssuesQ; l2x++)
                                    Dbg.Log(sfdx, $" + Encoded line {l2x + 1} :: {_lineData2[l2x]}; ");
                            }
                            else Dbg.Log(sfdx, $" + No line data to encode; ");
                        }
                        break;
                }

                if (!noIssuesQ)
                    Dbg.Log(sfdx, $"Encountered an error while saving (stage #{enx+1}).");
            }

            if (noIssuesQ)
                SetPreviousSelf();

            Dbg.EndLogging(sfdx);
            return noIssuesQ;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbg.StartLogging("SFormatterData.DecodeFromSharedFile()", out int sfdx);
            bool decodedQ = true, crossCompatibilityIssue = false;
            for (int dcx = 0; dcx < 3 && decodedQ; dcx++)
            {
                switch (dcx)
                {
                    case 0:
                        Dbg.Log(sfdx, "Fetching General Data and Groups Data; ");
                        Dbg.NudgeIndent(sfdx, true);
                        decodedQ = Base.FileRead(commonFileTag, out string[] frmtData);
                        if (decodedQ && frmtData.HasElements(4))
                        {
                            /// use nativeQ
                            Dbg.LogPart(sfdx, "- Fetching 'Use Native Color Code Q' :: ");
                            if (bool.TryParse(frmtData[0], out bool useNativeQ))
                            {
                                Dbg.LogPart(sfdx, useNativeQ.ToString());
                                _nativeColCodeQ = useNativeQ;
                            }
                            else Dbg.LogPart(sfdx, $" ??? ");
                            Dbg.Log(sfdx, "; ");

                            /// edit prof 1Q
                            Dbg.LogPart(sfdx, "- Fetching 'Edit Profile 1 Q' :: ");
                            if (bool.TryParse(frmtData[1], out bool edit1Q))
                            {
                                Dbg.LogPart(sfdx, edit1Q.ToString());
                                _editProf1Q = edit1Q;
                            }
                            else Dbg.LogPart(sfdx, $" ??? ");
                            Dbg.Log(sfdx, "; ");

                            const string triSep = Sep + Sep + Sep;

                            /// group 1
                            Dbg.LogPart(sfdx, "- Fetching 'Group 1 Data Line'");
                            string[] groupOneInfo = frmtData[2].Split(triSep);
                            if (groupOneInfo.HasElements(2))
                            {
                                Dbg.Log(sfdx, "; ");
                                _groupInfo1 ??= new List<SfdGroupInfo>();
                                for (int g1x = 0; g1x < groupOneInfo.Length; g1x++)
                                {
                                    if (g1x != 0)
                                    {
                                        SfdGroupInfo group1s = new();
                                        if (group1s.Decode(groupOneInfo[g1x]))
                                        {
                                            _groupInfo1.Add(group1s);
                                            Dbg.Log(sfdx, $"    Decoded :: {group1s}");
                                        }                                  
                                    }
                                }
                            }
                            else Dbg.Log(sfdx, " :: No Group 1s data to decode; ");

                            /// group 2
                            Dbg.LogPart(sfdx, "- Fetching 'Group 2 Data Line'");
                            string[] groupTwoInfo = frmtData[3].Split(triSep);
                            if (groupTwoInfo.HasElements(2))
                            {
                                Dbg.Log(sfdx, "; ");
                                _groupInfo2 ??= new List<SfdGroupInfo>();
                                for (int g2x = 0; g2x < groupTwoInfo.Length; g2x++)
                                {
                                    if (g2x != 0)
                                    {
                                        SfdGroupInfo group2s = new();
                                        if (group2s.Decode(groupTwoInfo[g2x]))
                                        {
                                            _groupInfo2.Add(group2s);
                                            Dbg.Log(sfdx, $"    Decoded :: {group2s}");
                                        }
                                    }
                                }
                            }
                            else Dbg.Log(sfdx, " :: No Group 2s data to decode; ");
                        }
                        else
                        {
                            crossCompatibilityIssue = true;
                            Dbg.Log(sfdx, $"Could not fetch general formatting data values; ");
                        }
                        Dbg.NudgeIndent(sfdx, false);
                        break;

                    case 1:
                        Dbg.Log(sfdx, "Fetching Formatting Profile 1 data; ");
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

                                Dbg.Log(sfdx, $" + Decoded {(fx1 == 0 ? "Name" : $"Line {fx1}")} :: {f1Data}");
                            }
                        }                        
                        else Dbg.Log(sfdx, $" -> Could not fetch formatting profile data; ");
                        break;

                    case 2:
                        Dbg.Log(sfdx, "Fetching Formatting Profile 2 data; ");
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

                                Dbg.Log(sfdx, $" + Decoded {(fx2 == 0 ? "Name" : $"Line {fx2}")} :: {f2Data}");
                            }
                        }
                        else Dbg.Log(sfdx, $" -> Could not fetch formatting profile data; ");
                        break;
                }
            }

            if (crossCompatibilityIssue)
            {
                Dbg.Log(sfdx, $"Cross-compatibility issue: previous versions do not contain guaranteed lines for general data; Decoding is okay'd; ");
                decodedQ = true;
            }

            SetPreviousSelf();
            Dbg.EndLogging(sfdx);
            return decodedQ;
        }
        #endregion

    }
}
