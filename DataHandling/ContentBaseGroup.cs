using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            - List<str> dataIDs (prv set;)
            - str this[int] (get -> dataID;) 

        Constructor
            - CBG()
            - CBG(verNum verNum, str contentName, params str[] dataIDs)

        Methods
            - void AddDataIDs(params str[] newDataID)
                Add data ids to the dataIDs collection
            - void RemoveDataIDs(params str[] dataIDs)
                Removes data ids from the dataIDs collection
            - List<str> FetchSimilarDataIDs(str piece)
                Iterates through dataIDs collection and searches for dataIDs that are exact or contain 'piece'
            - bool ContainsDataID(str dataIDtoFind)
            - string EncodeFirstGroup()
            - bool ChangesDetected()
                Compares itself against an orignal copy of itself for changes
        ***/

        #region fields / props
        // private
        ContentBaseGroup _previousSelf;
        VerNum _versionNumber;
        string _contentName;
        List<string> _dataIDs;
        
        // public

        #endregion


        public ContentBaseGroup() { }
        

    }
}
