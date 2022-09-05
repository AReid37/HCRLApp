using System.Collections.Generic;
using static HCResourceLibraryApp.DataHandling.DataHandlerBase;

namespace HCResourceLibraryApp.DataHandling
{
    public class ContentBaseGroup
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
            - string EncodeFirstGroup()
            - bl ChangesDetected()
                Compares itself against an orignal copy of itself for changes
            - bl IsSetup()
            - bl Equals(CBG other)
            - bl ovrd ToString()
        ***/

        #region fields / props
        // private
        ContentBaseGroup _previousSelf;
        VerNum _versionNumber;
        string _contentName;
        List<string> _dataIDs;

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
        #endregion

        public ContentBaseGroup()
        {
            _previousSelf = (ContentBaseGroup)this.MemberwiseClone();
        }
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
            _previousSelf = (ContentBaseGroup)this.MemberwiseClone();
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
        public bool ChangesDetected()
        {
            bool anyChanges = false;
            for (int i = 0; i < 3 && !anyChanges; i++)
            {
                switch (i)
                {
                    case 1:
                        anyChanges = _previousSelf.ContentName != ContentName;
                        break;

                    case 2:
                        anyChanges = !_previousSelf.VersionNum.Equals(VersionNum);
                        break;

                    case 3:
                        anyChanges = _previousSelf._dataIDs.HasElements() != _dataIDs.HasElements();
                        if (!anyChanges)
                            anyChanges = _previousSelf._dataIDs.Count != _dataIDs.Count;
                        if (!anyChanges)
                        {
                            for (int cpd = 0; cpd < _previousSelf._dataIDs.Count && !anyChanges; cpd++)
                                anyChanges = _previousSelf[cpd] != this[cpd];
                        }
                        break;
                }
            }
            return anyChanges;
        }
        /// <summary>Has this instance of <see cref="ContentBaseGroup"/> been initialized with the appropriate information?</summary>
        public bool IsSetup()
        {
            return _dataIDs.HasElements() && _versionNumber.HasValue() && _contentName.IsNotNEW();
        }
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
        }

        public override string ToString()
        {
            return EncodeFirstGroup().Replace(Sep, ";");
        }
        #endregion
    }
}
