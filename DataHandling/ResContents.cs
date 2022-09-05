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
            - ResCon(CBG contentBase)
            - ResCon(int shelfID, CBG contentBase, CA[] additionals, CC[] changes)


        Methods
            - vd StoreConAdditional(CA ca)
            - vd StoreConChanges(CC cc)
            - List<str> FetchSimilarDataIDs(str piece, out RCFS source)
            - bl ContainsDataID(str dataIDtoFind, out RCFS source)
            - str[] EncodeGroups()
                Encodes a three-element array containing information from ConBase, ConAdds, and ConChanges, all to be tagged with shelfID 
            - bl ChangesDetected()
            - bl Equals(ResCon)
            - bl IsSetup()
        ***/

        #region fields / props
        // private
        const int noShelfNum = -1;
        ResContents _previousSelf;
        int _shelfID;
        ContentBaseGroup _conBase;
        List<ContentAdditionals> _conAddits;
        List<ContentChanges> _conChanges;

        // public
        public int ShelfID
        {
            get => _shelfID; 
            set => _shelfID = value;
        }
        public ContentBaseGroup ConBase
        {
            get => _conBase;
            private set
            {
                if (value != null)
                    if (value.IsSetup())
                        _conBase = value;
            }
        }
        public List<ContentAdditionals> ConAddits
        {
            get => _conAddits;
            private set
            {
                if (value.HasElements())
                    _conAddits = value;
            }
        }
        public List<ContentChanges> ConChanges
        {
            get => _conChanges;
            private set
            {
                if (value.HasElements())
                    _conChanges = value;
            }
        }
        public string ContentName
        {
            get
            {
                string contentName = null;
                if (_conBase != null)
                    if (_conBase.IsSetup())
                        contentName = _conBase.ContentName;
                return contentName;
            }
        }
        #endregion

        public ResContents()
        {
            ShelfID = noShelfNum;
            _previousSelf = (ResContents)this.MemberwiseClone();
        }      
        public ResContents(int? shelfID, ContentBaseGroup conBase)
        {
            if (shelfID.HasValue)
                ShelfID = shelfID.Value;
            else ShelfID = noShelfNum;
            ConBase = conBase;
            //ConAddits = new List<ContentAdditionals>();
            //ConChanges = new List<ContentChanges>();

            _previousSelf = (ResContents)this.MemberwiseClone();
        }
        public ResContents(int? shelfID, ContentBaseGroup conBase, ContentAdditionals[] additionals, ContentChanges[] changes) 
        {
            if (shelfID.HasValue)
                ShelfID = shelfID.Value;
            else ShelfID = noShelfNum;
            ConBase = conBase;
            if (additionals.HasElements())
            {
                ConAddits = new List<ContentAdditionals>();
                foreach (ContentAdditionals ca in additionals)
                    if (ca != null)
                        if (ca.IsSetup())
                            ConAddits.Add(ca);
            }
            if (changes.HasElements())
            {
                ConChanges = new List<ContentChanges>();
                foreach (ContentChanges cc in changes)
                    if (cc != null)
                        if (cc.IsSetup())
                            ConChanges.Add(cc);
            }
            
            _previousSelf = (ResContents)this.MemberwiseClone();
        }

        #region methods
        public void StoreConAdditional(ContentAdditionals newCA)
        {
            if (IsSetup())
            {
                if (newCA != null)
                    if (newCA.IsSetup())
                    {
                        bool isDupe = false, isOkay = ConBase.ContainsDataID(newCA.RelatedDataID);
                        if (ConAddits.HasElements())
                        {
                            for (int dx = 0; dx < ConAddits.Count && !isDupe; dx++)
                                isDupe = ConAddits[dx].Equals(newCA);
                        }
                        else ConAddits = new List<ContentAdditionals>();

                        if (!isDupe && isOkay)
                            ConAddits.Add(newCA);
                    }
            }
        }
        public void StoreConChanges(ContentChanges newCC)
        {
            if (IsSetup())
            {
                if (newCC != null)
                    if (newCC.IsSetup())
                    {
                        bool isDupe = false, isOkay;
                        isOkay = ConBase.ContainsDataID(newCC.RelatedDataID);
                        if (!isOkay && ConAddits.HasElements())
                        {
                            for (int okccix = 0; okccix < ConAddits.Count && !isOkay; okccix++)
                                isOkay = ConAddits[okccix].ContainsDataID(newCC.RelatedDataID);
                        }

                        if (ConChanges.HasElements())
                        {
                            for (int cx = 0; cx < ConChanges.Count && !isDupe; cx++)
                                isDupe = ConChanges[cx].Equals(newCC);
                        }
                        else ConChanges = new List<ContentChanges>();

                        if (!isDupe && isOkay)
                            ConChanges.Add(newCC);
                    }
            }
        }
        // List<string> FetchSimilarDataIDs(string piece, out RCFetchSource source) {} // later...
        // bool ContainsDataID(string dataIDtoFind, out RCFetchSource source) {} // tbd later...
        //public string[] EncodeGroup() { }
        public bool ChangedDetected()
        {
            return !Equals(_previousSelf);
        }
        public bool Equals(ResContents resCon)
        {
            bool areEquals = false;
            if (resCon != null)
            {
                areEquals = true;
                for (int rcix = 0; rcix < 4 && areEquals; rcix++)
                    switch (rcix)
                    {
                        case 0:
                            areEquals = resCon.IsSetup() == IsSetup();
                            break;

                        case 1:
                            areEquals = resCon.ConBase.Equals(ConBase);
                            break;

                        case 2:
                            areEquals = resCon.ConAddits.HasElements() == ConAddits.HasElements();
                            if (ConAddits.HasElements())
                            {
                                areEquals = ConAddits.Count == resCon.ConAddits.Count;
                                if (areEquals)
                                {
                                    for (int cax = 0; cax < ConAddits.Count && areEquals; cax++)
                                        areEquals = ConAddits[cax].Equals(resCon.ConAddits[cax]);
                                }
                            }
                            break;

                        case 3:
                            areEquals = resCon.ConChanges.HasElements() == ConChanges.HasElements();
                            if (ConChanges.HasElements())
                            {
                                areEquals = ConChanges.Count == resCon.ConChanges.Count;
                                if (areEquals)
                                {
                                    for (int ccx = 0; ccx < ConChanges.Count && areEquals; ccx++)
                                        areEquals = ConChanges[ccx].Equals(resCon.ConChanges[ccx]);
                                }
                            }
                            break;
                    }
            }
            return areEquals;
        }
        public bool IsSetup()
        {
            return ShelfID != noShelfNum && ConBase.IsSetup();
        }
        #endregion
    }
}
