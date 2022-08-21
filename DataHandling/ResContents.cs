using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Short-hand for "ResourceContents".</summary>
    public class ResContents
    {
        /*** RESOURCE CONTENTS
        Data form containing information regarding a primary piece of content
        
        FROM DESIGN DOC
        ........
        File-encoding of information for content reqires three (3) lines:
			L1| {VersionAdded}*{ContentName}*{RelatedDataIDs}
			L2|	{VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
			L3| {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
        ........


        Fields / Props
            - ResCon previousSelf
            - int shelfID (prv set; get;)
            - ContentBaseGroup conBase (get;)
            - List<ContentAdditionals) conAdditions (get;)
            - List<ContentChanges) conChanges (get;)
            - str contentName (get -> conBase.contentName;)

        Constructors
            - ResCon()
            - ResCon(int shelfID, params str[] triConData)
                triConData only expects three elements for three groups. Decoding happens here

        Methods
            - List<str> FetchSimilarDataIDs(str piece, out bl fromConBaseQ)
            - bool ContainsDataID(str dataIDtoFind)
            - str[] EncodeGroups()
                Encodes a three-element array containing information from ConBase, ConAdds, and ConChanges, including shelfID 
            - bool ChangesDetected()

        ***/
    }
}
