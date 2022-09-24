using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public enum RCFetchSource
    {
        None,
        ConBaseGroup,
        ConAdditionals,
        ConChanges
    }

    /// <summary>Short-hand for "ResourceContents".</summary>
    public class ResContents : DataHandlerBase
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
            - List<ConAddit> conAdditions (get;)
            - List<ConChange> conChanges (get;)
            - str contentName (get -> conBase.contentName;)

        Constructors
            - ResCon()
            - ResCon(CBG contentBase)
            - ResCon(int shelfID, CBG contentBase, CA[] additionals, CC[] changes)


        Methods
            - vd StoreConAdditional(CA ca)
            - vd StoreConChanges(CC cc)
            - vd DisposeConAdditional(CA ca)
            - vd DisposeConChanges(CC cc)
            - List<str> FetchSimilarDataIDs(str piece, out RCFS source)
            - bl ContainsDataID(str dataIDtoFind, out RCFS source)
            - str[] EncodeGroups()
                Encodes a three-element array containing information from ConBase, ConAdds, and ConChanges, all to be tagged with shelfID 
            - bl DecodeGroups(str[] info)
            - bl ChangesDetected()
            - bl Equals(ResCon)
            - bl IsSetup()
            - ovr str ToString()
        ***/

        #region fields / props
        // private
        const string Sep3 = Sep + Sep + Sep;
        const int noShelfNum = -1;
        ResContents _previousSelf;
        int _shelfID;
        ContentBaseGroup _conBase;
        List<ContentAdditionals> _conAddits;
        List<ContentChanges> _conChanges;

        // public
        /// <summary>Also serves as the saving tag for this ResCon.</summary>
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
            private set => _conAddits = value;
        }
        public List<ContentChanges> ConChanges
        {
            get => _conChanges;
            private set => _conChanges = value;
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
            ConBase = new ContentBaseGroup();
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
        // public void DisposeConAdditional(CA ca)
        // public void DisposeConChanges(CC cc)

        /// List<string> FetchSimilarDataIDs(string piece, out RCFetchSource source) {} // later...
        //bool ContainsDataID(string dataIDtoFind, out RCFetchSource source, out ContentAdditionals conAddSource)
        //{
        //    bool containsDIDq = false;
        //    source = RCFetchSource.None;
        //    conAddSource = new ContentAdditionals();

        //    if (IsSetup() && dataIDtoFind.IsNotNEW())
        //    {
        //        for (int cdx = 0; cdx < 3 && !containsDIDq; cdx++)
        //            switch (cdx)
        //            {
        //                case 0:
        //                    source = RCFetchSource.ConBaseGroup;
        //                    if (ConBase != null)
        //                        if (ConBase.IsSetup())
        //                            containsDIDq = ConBase.ContainsDataID(dataIDtoFind);
        //                    break;

        //                case 1:
        //                    source = RCFetchSource.ConAdditionals;
        //                    if (ConAddits.HasElements())
        //                        for (int cax = 0; cax < ConAddits.Count && !containsDIDq; cax++)
        //                        {
        //                            containsDIDq = ConAddits[cax].ContainsDataID(dataIDtoFind);
        //                            if (containsDIDq)
        //                                conAddSource = ConAddits[cax];
        //                        }
        //                    break;

        //                    /// technically speaking, this should never be hit; ContentChanges always refer to a DataID that already exists within either ConBase or ConAddits
        //                    //case 2:
        //                    //    if (ConChanges.HasElements())
        //                    //        for (int ccx = 0; ccx < ConChanges.Count && !containsDIDq; ccx++)
        //                    //            containsDIDq = ConChanges[ccx].RelatedDataID == dataIDtoFind;
        //                    //    break;
        //            }
        //    }
        //    if (!containsDIDq)
        //        source = RCFetchSource.None;
        //    return containsDIDq;
        //}
        public bool ContainsDataID(string dataIDtoFind, out RCFetchSource source, out DataHandlerBase dataSource)
        {
            bool containsDIDq = false;
            source = RCFetchSource.None;
            dataSource = new DataHandlerBase();

            if (IsSetup() && dataIDtoFind.IsNotNEW())
            {
                for (int cdx = 0; cdx < 3 && !containsDIDq; cdx++)
                    switch (cdx)
                    {
                        case 0:
                            source = RCFetchSource.ConBaseGroup;
                            if (ConBase != null)
                                if (ConBase.IsSetup())
                                {
                                    containsDIDq = ConBase.ContainsDataID(dataIDtoFind);
                                    if (containsDIDq)
                                        dataSource = ConBase;
                                }
                            break;

                        case 1:
                            source = RCFetchSource.ConAdditionals;
                            if (ConAddits.HasElements())
                                for (int cax = 0; cax < ConAddits.Count && !containsDIDq; cax++)
                                {
                                    containsDIDq = ConAddits[cax].ContainsDataID(dataIDtoFind);
                                    if (containsDIDq)
                                        dataSource = ConAddits[cax];
                                }
                            break;

                            /// technically speaking, this should never be hit; ContentChanges always refer to a DataID that already exists within either ConBase or ConAddits
                            //case 2:
                            //    if (ConChanges.HasElements())
                            //        for (int ccx = 0; ccx < ConChanges.Count && !containsDIDq; ccx++)
                            //            containsDIDq = ConChanges[ccx].RelatedDataID == dataIDtoFind;
                            //    break;
                    }
            }
            if (!containsDIDq)
                source = RCFetchSource.None;
            return containsDIDq;
        }
        public bool ContainsDataID(string dataIDtoFind, out RCFetchSource source)
        {
            bool containsDIDq = ContainsDataID(dataIDtoFind, out source, out _);
            return containsDIDq;
        }
        public string[] EncodeGroups()
        {
            /** Syntax
            ........
            File-encoding of information for content reqires three (3) lines:
	            L1| {VersionAdded}*{ContentName}*{RelatedDataIDs}
	            L2|	{VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
	            L3| {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
            ........
             **/
            string[] groupsEncode = null;
            if (IsSetup())
            {                
                groupsEncode = new string[3];

                // 1st group
                groupsEncode[0] = ConBase.EncodeFirstGroup();

                // 2nd group
                if (ConAddits.HasElements())
                {
                    groupsEncode[1] = "";
                    for (int caix = 0; caix < ConAddits.Count; caix++)
                        groupsEncode[1] += ConAddits[caix].EncodeSecondGroup() + (caix + 1 >= ConAddits.Count ? "" : Sep3);
                }
                ///else groupsEncode[1] = Sep;

                // 3rd group
                if (ConChanges.HasElements())
                {
                    groupsEncode[2] = "";
                    for (int ccix = 0; ccix < ConChanges.Count; ccix++)
                        groupsEncode[2] += ConChanges[ccix].EncodeThirdGroup() + (ccix + 1 >= ConChanges.Count ? "" : Sep3);
                }
                ///else groupsEncode[2] = Sep;
            }
            return groupsEncode;
        }
        /// <remarks>Requires at least three elements to function.</remarks>
        public bool DecodeGroups(int? shelfID, string[] rcInfo)
        {
            /** Syntax
            ........
            File-encoding of information for content reqires three (3) lines:
	            L1| {VersionAdded}*{ContentName}*{RelatedDataIDs}
                    ^^ Con Base Group
	            L2|	{VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
                    ^^ Con Additionals Groups (multiple)
	            L3| {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
                    ^^ Con Changes Groups (multiple)
            ........
             **/

            if (rcInfo.HasElements(3))
            {
                // shelf number
                if (shelfID.HasValue)
                    ShelfID = shelfID.Value;

                // first group
                if (rcInfo[0].IsNotNEW())
                {
                    ContentBaseGroup newCbg = new();
                    if (newCbg.DecodeFirstGroup(rcInfo[0]))
                        ConBase = newCbg;
                }
                // second group
                if (rcInfo[1].IsNotNEW())
                {
                    /// gather data
                    List<string> infoConAdts = new();
                    if (rcInfo[1].Contains(Sep3))
                    {
                        string[] rcAdtParts = rcInfo[1].Split(Sep3, System.StringSplitOptions.RemoveEmptyEntries);
                        if (rcAdtParts.HasElements())
                            infoConAdts.AddRange(rcAdtParts);
                    }
                    else infoConAdts.Add(rcInfo[1]);

                    /// decode and add
                    if (!ConAddits.HasElements())
                        ConAddits = new List<ContentAdditionals>();
                    foreach (string conAdtInfo in infoConAdts)
                    {
                        ContentAdditionals newCa = new();
                        if (newCa.DecodeSecondGroup(conAdtInfo))
                            ConAddits.Add(newCa);
                    }
                }
                // third group
                if (rcInfo[2].IsNotNEW())
                {
                    /// gather data
                    List<string> infoConChgs = new();
                    if (rcInfo[2].Contains(Sep3))
                    {
                        string[] rcChgParts = rcInfo[2].Split(Sep3, System.StringSplitOptions.RemoveEmptyEntries);
                        if (rcChgParts.HasElements())
                            infoConChgs.AddRange(rcChgParts);
                    }
                    else infoConChgs.Add(rcInfo[2]);

                    /// decode and add
                    if (!ConChanges.HasElements())
                        ConChanges = new List<ContentChanges>();
                    foreach (string conChgInfo in infoConChgs)
                    {
                        ContentChanges newChg = new();
                        if (newChg.DecodeThirdGroup(conChgInfo))
                            ConChanges.Add(newChg);
                    }
                }

                _previousSelf = (ResContents)this.MemberwiseClone();
            }
            return IsSetup();
        }

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
        /// <summary>Has this instance of <see cref="ResContents"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the shelf ID and <see cref="ContentBaseGroup"/> has been given values, at minimum.</returns>
        public override bool IsSetup()
        {
            return ShelfID != noShelfNum && ConBase.IsSetup();
        }

        public override string ToString()
        {
            string resConToStr = $"(RC) #{(ShelfID == noShelfNum ? "_" : ShelfID)};";
            if (ConBase != null)
            {
                if (ConBase.IsSetup())
                    resConToStr += $" {ConBase.ContentName}, {ConBase.VersionNum.ToStringNumbersOnly()}";
            }
            if (ConAddits.HasElements())
                resConToStr += $", [{ConAddits.Count}] adts";
            if (ConChanges.HasElements())
                resConToStr += $", [{ConChanges.Count}] upds";
            return resConToStr.Trim(); 
        }
        #endregion
    }
}
