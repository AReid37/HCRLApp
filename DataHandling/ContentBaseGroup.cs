using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public class ContentBaseGroup : DataHandlerBase
    {
        /*** CONTENT BASE GROUP - PLANS
        Data form of First group for content file encoding
        Syntax: {VersionAdded}*{ContentName}*{RelatedDataIDs}

        FROM DESIGN DOC
        ........
        > {VersionAdded}
			[REQUIRED] string value as "a.bb"
			"a" represents major version number
			"bb" represents minor version number
			May not contain the '*' symbol
		> {ContentName}
			[REQUIRED] string value 
			May not contain the '*' symbol
		> {RelatedDataIDs}
			[REQUIRED] comma-separated string values 
			No spacing between different Data IDs
			May not contain the '*' symbol
		NOTES
			- {VersionAdded} denotes the version in which the new content was added
        ........


        Fields / Props
            - CBG previousSelf
            - verNum versionAdded (prv set; get)
            - str contentName (prv set; get;)
            - List<str> dataIDs (prv set;)  Necessary??
            - str this[int] (get -> dataID;) 

        Constructor
            - CBG()
            - CBG(verNum verNum, str contentName, params str[] dataIDs)

        Methods

            - void AddDataIDs(params str[] newDataID)   Necessary??
                Add data ids to the dataIDs collection
            - void RemoveDataIDs(params str[] dataIDs)  Necessary??
                Removes data ids from the dataIDs collection
            - List<str> FetchSimilarDataIDs(str piece)
                Iterates through dataIDs collection and searches for dataIDs that are exact or contain 'piece'
            - bl ContainsDataID(str dataIDtoFind)
            - str EncodeFirstGroup()
            - bl DecodeFirstGroup(str info)
            - bl ChangesDetected()
                Compares itself against an orignal copy of itself for changes
            - bl IsSetup()
            - bl Equals(CBG other)
            - bl ovrd ToString()
        ***/

        #region fields / props
        // private
        VerNum _versionNumber, _prevVersionNumber;
        string _contentName, _prevContentName;
        List<string> _dataIDs, _prevDataIDs;

        // public
        /// <summary>Version number this content was added.</summary>
        public VerNum VersionNum
        {
            private set
            {
                if (value.HasValue())
                    _versionNumber = value;
            }
            get => _versionNumber;
        }
        /// <summary>The (internal) name of this content.</summary>
        public string ContentName
        {
            private set
            {
                if (value.IsNotNEW())
                    _contentName = value;
            }
            get => _contentName;
        }
        /// <summary>Retrieves the data ID of this content base at <paramref name="index"/>.</summary>
        /// <param name="index">This value will be clamped within the index range of the Data IDs array.</param>
        public string this[int index]
        {
            get
            {
                string dataID = null;
                if (_dataIDs.HasElements())
                {
                    index = index.Clamp(0, _dataIDs.Count - 1);
                    dataID = _dataIDs[index];
                }
                return dataID;
            }            
        }
        public int CountIDs
        {
            get
            {
                int idCount = 0;
                if (_dataIDs.HasElements())
                    idCount = _dataIDs.Count;
                return idCount;
            }
        }
        public string DataIDString
        {
            get
            {
                string dataIDs = "";
                if (_dataIDs.HasElements())
                    foreach (string datId in _dataIDs)
                        dataIDs += $"{datId} ";
                return dataIDs.Trim();
            }
        }
        #endregion

        public ContentBaseGroup() { }
        public ContentBaseGroup(VerNum versionNumber, string contentName, params string[] dataIDs)
        {
            VersionNum = versionNumber;
            ContentName = contentName;
            if (dataIDs.HasElements())
            {
                _dataIDs = new List<string>();
                foreach (string dataID in dataIDs.SortWords())
                    if (dataID.IsNotNEW())
                        _dataIDs.Add(dataID);
            }
        }


        #region methods
        //List<string> FetchSimilarDataIDs(string piece) { } // postpone for much later, when doing searches
        public bool ContainsDataID(string dataIDtoFind)
        {
            bool containsDataIdQ = false;
            if (dataIDtoFind.IsNotNEW() && IsSetup())
                for (int dix = 0; dix < _dataIDs.Count && !containsDataIdQ; dix++)
                    containsDataIdQ = this[dix] == dataIDtoFind;
            return containsDataIdQ;
        }
        public string EncodeFirstGroup()
        {
            // Syntax: {VersionAdded}*{ContentName}*{RelatedDataIDs}
            string firstEncode = "";
            if (IsSetup())
            {
                firstEncode = $"{VersionNum}{Sep}{ContentName}{Sep}";
                for (int s = 0; s < _dataIDs.Count; s++)
                    firstEncode += this[s] + (s + 1 < _dataIDs.Count ? "," : "");
            }
            return firstEncode;
        }
        public bool DecodeFirstGroup(string cbgInfo)
        {
            /**
             Syntax: {VersionAdded}*{ContentName}*{RelatedDataIDs}

            FROM DESIGN DOC
            ........
            > {VersionAdded}
			    [REQUIRED] string value as "a.bb"
			    "a" represents major version number
			    "bb" represents minor version number
			    May not contain the '*' symbol
		    > {ContentName}
			    [REQUIRED] string value 
			    May not contain the '*' symbol
		    > {RelatedDataIDs}
			    [REQUIRED] comma-separated string values 
			    No spacing between different Data IDs
			    May not contain the '*' symbol
		    NOTES
			    - {VersionAdded} denotes the version in which the new content was added
            ........
             **/

            if (cbgInfo.IsNotNEW())
            {
                if (cbgInfo.Contains(Sep) && cbgInfo.CountOccuringCharacter(Sep[0]) == 2)
                {
                    string[] firstParts = cbgInfo.Split(Sep);
                    /// version
                    if (firstParts[0].IsNotNEW())
                    {
                        if (VerNum.TryParse(firstParts[0], out VerNum cbgVerNum))
                            VersionNum = cbgVerNum;
                    }
                    /// contentName
                    if (firstParts[1].IsNotNEW())
                        ContentName = firstParts[1];
                    /// relatedDataIDs
                    if (firstParts[2].IsNotNEW())
                    {
                        _dataIDs = new List<string>();
                        if (firstParts[2].Contains(','))
                        {
                            string[] cbgDataIDs = firstParts[2].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                            if (cbgDataIDs.HasElements())
                                _dataIDs.AddRange(cbgDataIDs);
                        }
                        else _dataIDs.Add(firstParts[2]);
                    }
                    SetPreviousSelf();
                }
            }
            return IsSetup();
        }
        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }        
        void SetPreviousSelf()
        {
            _prevVersionNumber = VersionNum;
            _prevContentName = ContentName;
            if (_dataIDs.HasElements())
            {
                _prevDataIDs = new List<string>();
                _prevDataIDs.AddRange(_dataIDs.ToArray());
            }
        }
        ContentBaseGroup GetPreviousSelf()
        {
            string[] prevDataIDs = null;
            if (_prevDataIDs.HasElements())
                prevDataIDs = _prevDataIDs.ToArray();
            return new ContentBaseGroup(_prevVersionNumber, _prevContentName, prevDataIDs);
        }
        /// <summary>Has this instance of <see cref="ContentBaseGroup"/> been initialized with the appropriate information?</summary>
		/// <returns>A boolean stating whether the version number, data IDs, and content name has been given values.</returns>
        public override bool IsSetup()
        {
            return _dataIDs.HasElements() && _versionNumber.HasValue() && _contentName.IsNotNEW();
        }
        /// <summary>Compares two instances for similarities against: Setup state, Content Name, Version Number, Data IDs.</summary>
        public bool Equals(ContentBaseGroup cbg)
        {
            bool areEqual = false;
            if (cbg != null)
            {
                areEqual = true;
                for (int cmp = 0; cmp < 4 && areEqual; cmp++)
                {
                    switch (cmp)
                    {
                        case 1:
                            areEqual = cbg.IsSetup() == IsSetup();
                            break;

                        case 2:
                            areEqual = cbg.ContentName == ContentName;
                            break;

                        case 3:
                            areEqual = cbg.VersionNum.Equals(VersionNum);
                            break;

                        case 4:
                            areEqual = cbg._dataIDs.HasElements() == _dataIDs.HasElements();
                            if (_dataIDs.HasElements())
                            {
                                if (areEqual)
                                    areEqual = cbg._dataIDs.Count == _dataIDs.Count;
                                if (areEqual)
                                {
                                    for (int ix = 0; ix < _dataIDs.Count && areEqual; ix++)
                                        areEqual = this[ix] == cbg[ix];
                                }
                            }
                            break;
                    }
                }
            }
            return areEqual;
        /// <summary>Compares two instances for similarities against: .</summary>
        }

        public override string ToString()
        {
            return EncodeFirstGroup().Replace(Sep, ";");
        }
        #endregion
    }
}
