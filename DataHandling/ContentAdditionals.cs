using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCResourceLibraryApp.DataHandling
{
    public class ContentAdditionals
    {
		/*** CONTENT ADDITIONALS - PLANS
        Data form of Second group for content file encoding
        Syntax: {VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***

        FROM DESIGN DOC
        .........
        > {VersionAddit}
			[REQUIRED] string value as "a.bb"
			"a" represents major version number
			"bb" represents minor version number
			May not contain the '*' symbol
		> {RelatedDataID}
			[REQUIRED] string value
			May not contain '*' symbol
		> {Opt.Name}
			string value
		> {DataID}
			[REQUIRED] comma-separated string values
			No spacing between different Data IDs
			May not contain the '*' symbol
		NOTES
			- Multiple additional contents information must be separated with '***'
			- {VersionAddit} denotes the version in which additional content was added
        .........


		Fields / Props
			- CA previousSelf
			- verNum versionNumber (priv set; get;)
			- str relatedDataID (priv set; get;)
			- str optName (priv set; get;)
			- List<str> dataIds (priv set;)
			- str this[int] (get -> dataIds[]) 

		Constructors
			- CA()
			- CA(verNum verNum, str relatedDataID, str optionalName, params str[] dataIDs)

		Methods
			- void AddDataIDs(params str[] dataIDs)
				Add data ids to the dataIds collection
			- void RemoveDataIDs(params str[] dataIDs)
				Removes data ids from the dataIDs collection
			- List<str> FetchSimilarDataIDs(str piece)
                Iterates through dataIDs collection and searches for dataIDs that are exact or contain 'piece'
            - string EncodeSecondGroup()
			- bool ContainsDataID(str dataIDtoFind)
			- bool ChangesDetected()
                Compares itself against an orignal copy of itself for changes          
         ***/
	}
}
