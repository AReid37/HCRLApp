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
        int _shelfID, _prevShelfID;
        ContentBaseGroup _conBase, _prevConBase;
        List<ContentAdditionals> _conAddits, _prevConAddits;
        List<ContentChanges> _conChanges, _prevConChanges;

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
        }      
        public ResContents(int? shelfID, ContentBaseGroup conBase)
        {
            if (shelfID.HasValue)
                ShelfID = shelfID.Value;
            else ShelfID = noShelfNum;
            ConBase = conBase;
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
        }

        #region methods
        /// <summary>Stores a <see cref="ContentAdditionals"/> instance related to this <see cref="ResContents"/>'s ConBase.</summary>
        /// <param name="newCA">New instance is checked to be setup and not a duplicate.</param>
        /// <remarks>A <see cref="ContentAdditionals"/> will not be stored if this instance <see cref="IsSetup()"/> is '<c>false</c>'</remarks>
        public bool StoreConAdditional(ContentAdditionals newCA)
        {
            bool storedCAq = false;
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
                        {
                            ConAddits.Add(newCA);
                            storedCAq = true;
                        }
                    }
            }
            return storedCAq;
        }
        /// <summary>Stores a <see cref="ContentChanges"/> instance related to this <see cref="ResContents"/>'s ConBase or ConAddits.</summary>
        /// <param name="newCC">New instance is checked to be setup and not a duplicate.</param>
        /// <remarks>A <see cref="ContentChanges"/> will not be stored if this instance <see cref="IsSetup()"/> is '<c>false</c>'</remarks>
        public bool StoreConChanges(ContentChanges newCC)
        {
            bool storedCCq = false;
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
                        {
                            if ((newCC.InternalName == ResLibrary.LooseResConName || newCC.InternalName.IsNEW()) && ContentName != ResLibrary.LooseResConName)
                                newCC = new ContentChanges(newCC.VersionChanged, ContentName, newCC.RelatedDataID, newCC.ChangeDesc);

                            ConChanges.Add(newCC);
                            storedCCq = true;
                        }
                    }
            }
            return storedCCq;
        }
        /// <summary>Removes a <see cref="ContentAdditionals"/> instance related to this <see cref="ResContents"/>'s ConBase.</summary>
        /// <returns>A boolean relaying the success of removing this <see cref="ContentAdditionals"/> instance.</returns>
        public bool DisposeConAdditional(ContentAdditionals ca)
        {
            bool removedIt = false;
            if (IsSetup() && ca != null)
            {
                const int nonRemovalIx = -1;
                int removalIndex = nonRemovalIx;
                if (ConAddits.HasElements())
                    for (int cx = 0; cx < ConAddits.Count && removalIndex == nonRemovalIx; cx++)
                    {
                        if (ConAddits[cx].Equals(ca))
                            removalIndex = cx;
                    }

                if (removalIndex != nonRemovalIx)
                {
                    ConAddits.RemoveAt(removalIndex);
                    if (ConAddits.HasElements() && removalIndex.IsWithin(0, ConAddits.Count - 1))
                        removedIt = !ConAddits[removalIndex].Equals(ca);
                    else removedIt = removalIndex == ConAddits.Count;
                }
            }
            return removedIt;
        }
        /// <summary>Removes a <see cref="ContentChanges"/> instance related to this <see cref="ResContents"/>'s ConBase or ConAddits.</summary>
        /// <returns>A boolean relaying the success of removing this <see cref="ContentChanges"/> instance.</returns>
        public bool DisposeConChanges(ContentChanges cc)
        {
            bool removedIt = false;
            if (IsSetup() && cc != null)
            {
                const int nonRemovalIx = -1;
                int removalIndex = nonRemovalIx;
                if (ConChanges.HasElements())
                    for (int cy = 0; cy < ConChanges.Count && removalIndex == nonRemovalIx; cy++)
                    {
                        if (ConChanges[cy].Equals(cc))
                            removalIndex = cy;
                    }

                if (removalIndex != nonRemovalIx)
                {
                    ConChanges.RemoveAt(removalIndex);
                    if (ConChanges.HasElements() && removalIndex.IsWithin(0, ConChanges.Count - 1))
                        removedIt = !ConChanges[removalIndex].Equals(cc);
                    else removedIt = removalIndex == ConChanges.Count;
                }
            }
            return removedIt;
        }
        // List<string> FetchSimilarDataIDs(string piece, out RCFetchSource source, out DataHandlerBase dataSource) {} // later...
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

                // 3rd group
                if (ConChanges.HasElements())
                {
                    groupsEncode[2] = "";
                    for (int ccix = 0; ccix < ConChanges.Count; ccix++)
                        groupsEncode[2] += ConChanges[ccix].EncodeThirdGroup() + (ccix + 1 >= ConChanges.Count ? "" : Sep3);
                }

                SetPreviousSelf();
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
                // second group (or skip to third group)
                if (rcInfo[1].IsNotNEW())
                {
                    /// decode second group
                    if (!rcInfo[1].Contains(ContentChanges.ccIdentityKey))
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
                    /// deflect to 3rd group
                    else rcInfo[2] = rcInfo[1];
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

                SetPreviousSelf();
            }
            return IsSetup();
        }
        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }
        void SetPreviousSelf()
        {
            _prevShelfID = _shelfID;
            if (_conBase != null)
            {
                _prevConBase = new ContentBaseGroup(_conBase.VersionNum, _conBase.ContentName, _conBase.DataIDString.Split(' '));
            }
            if (_conAddits.HasElements())
            {
                _prevConAddits = new List<ContentAdditionals>();
                _prevConAddits.AddRange(_conAddits.ToArray());
            }
            if (_conChanges.HasElements())
            {
                _prevConChanges = new List<ContentChanges>();
                _prevConChanges.AddRange(_conChanges.ToArray());
            }
        }
        ResContents GetPreviousSelf()
        {
            ContentAdditionals[] prevConAdts = null;
            ContentChanges[] prevConChgs = null;
            if (_prevConAddits.HasElements())
                prevConAdts = _prevConAddits.ToArray();
            if (_prevConChanges.HasElements())
                prevConChgs = _prevConChanges.ToArray();
            return new ResContents(_prevShelfID, _prevConBase, prevConAdts, prevConChgs);
        }
        /// <summary>Compares two instances for similarities against: Setup state, ConBase, ConAddits, ConChanges.</summary>
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
                            if (ConAddits.HasElements() && areEquals)
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
                            if (ConChanges.HasElements() && areEquals)
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
            bool conBaseSetup = false;
            if (ConBase != null)
                conBaseSetup = ConBase.IsSetup();

            return ShelfID != noShelfNum && conBaseSetup;
        }

        public override string ToString()
        {
            string resConToStr = $"(RC) #{(ShelfID == noShelfNum ? "_" : ShelfID)};";
            if (ConBase != null)
            {
                if (ConBase.IsSetup())
                    resConToStr += $" {ConBase.ContentName}, {ConBase.VersionNum.ToStringNums()}";
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
