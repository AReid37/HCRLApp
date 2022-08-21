using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCResourceLibraryApp.DataHandling
{
    public class ContentChanges
    {
		/*** 
        Data form of Third group for content file encoding
        Syntax: {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***

		FROM DESIGN DOC
        ........
        > {VersionUpd}
			[REQUIRED] string value as "a.bb"
			"a" represents major version number
			"bb" represents minor version number
			May not contain the '*' symbol
		> {InternalName}
			[REQUIRED] string value
			May not contain '*' symbol
		> {RelatedDataID}
			[REQUIRED] string value
			May not contain '*' symbol
		> {ChangeDesc}
			[REQUIRED] string value
			May not contain '*' symbol
		NOTES
			- Multiple updates contents information must be separated with '***'
		........


		Fields / Props
			- CC previousSelf
			- verNum versionNumber (prv set; get;)
			- str internalName (prv set; get;)
			- str relatedDataID (prv set; get;)
			- str changeDesc (prv set; get;)

		Constructors
			- CC()
			- CC(verNum verNum, str internalName, str relDataID, str description)

		Methods
			- string EncodeThirdGroup()
			- bool ChangesDetected()
                Compares itself against an orignal copy of itself for changes

        ***/
	}
}
