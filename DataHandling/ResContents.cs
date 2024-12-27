using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
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
        int _shelfID, _prevShelfID;
        ContentBaseGroup _conBase, _prevConBase;
        List<ContentAdditionals> _conAddits, _prevConAddits;
        List<ContentChanges> _conChanges, _prevConChanges;

        // public
        public const int NoShelfNum = -1;
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
            ShelfID = NoShelfNum;
            ConBase = new ContentBaseGroup();
        }      
        public ResContents(int? shelfID, ContentBaseGroup conBase)
        {
            if (shelfID.HasValue)
                ShelfID = shelfID.Value;
            else ShelfID = NoShelfNum;

            if (conBase != null)
                ConBase = conBase;
            else ConBase = new();
            if (ConBase != null)
                ConBase.AdoptShelfID(ShelfID);
        }
        public ResContents(int? shelfID, ContentBaseGroup conBase, ContentAdditionals[] additionals, ContentChanges[] changes) 
        {
            if (shelfID.HasValue)
                ShelfID = shelfID.Value;
            else ShelfID = NoShelfNum;

            if (conBase != null)
                ConBase = conBase;
            else ConBase = new();
            if (ConBase != null)
                ConBase.AdoptShelfID(ShelfID);

            if (additionals.HasElements())
            {
                ConAddits = new List<ContentAdditionals>();
                foreach (ContentAdditionals ca in additionals)
                    if (ca != null)
                        if (ca.IsSetup())
                        {
                            ca.AdoptIDs(ShelfID, ConAddits.Count);
                            ConAddits.Add(ca);
                        }
            }
            if (changes.HasElements())
            {
                ConChanges = new List<ContentChanges>();
                foreach (ContentChanges cc in changes)
                    if (cc != null)
                        if (cc.IsSetup())
                        {
                            cc.AdoptIDs(ShelfID, ConChanges.Count);
                            ConChanges.Add(cc);
                        }
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
                        newCA = newCA.Clone();

                        bool isDupe = false, isOkay = ConBase.ContainsDataID(newCA.RelatedDataID);
                        if (ConAddits.HasElements())
                        {
                            for (int dx = 0; dx < ConAddits.Count && !isDupe; dx++)
                                isDupe = ConAddits[dx].Equals(newCA, true);
                        }
                        else ConAddits = new List<ContentAdditionals>();

                        if (!isDupe && isOkay)
                        {
                            if (ContentName != ResLibrary.LooseResConName)
                            {
                                newCA.AdoptIDs(ShelfID, ConAddits.Count);
                                newCA.ContentName = ContentName;
                            }
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
                        newCC = newCC.Clone();

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
                                isDupe = ConChanges[cx].Equals(newCC, true);
                        }
                        else ConChanges = new List<ContentChanges>();

                        if (!isDupe && isOkay)
                        {
                            if ((newCC.InternalName == ResLibrary.LooseResConName || newCC.InternalName.IsNEW()) && ContentName != ResLibrary.LooseResConName)
                                newCC = new ContentChanges(newCC.VersionChanged, ContentName, newCC.RelatedDataID, newCC.ChangeDesc);

                            if (ContentName != ResLibrary.LooseResConName) 
                                newCC.AdoptIDs(ShelfID, ConChanges.Count);
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
        public void Overwrite(ResContents rcNew, out ResLibOverwriteInfo[] info)
        {
            List<ResLibOverwriteInfo> infoDock = new();
            if (IsSetup() && rcNew != null)
            {
                ResContents prevSelf = CloneResContent(this);
                rcNew = CloneResContent(this, true);
                rcNew.ShelfID = ShelfID;
                bool beginOverwriteQ = IsSetup() && rcNew.IsSetup() && !Equals(rcNew);                
                ResLibOverwriteInfo rcInfo = new(ToString(), rcNew.ToString());

                if (beginOverwriteQ)
                {
                    VerNum verNum = ConBase.VersionNum;
                    /// steps
                    ///     Renew and replace ConBase object -- 1 RLOInfo
                    ///     Renew addits list, sort through addits and replace of same version -- 1 RLOInfo each
                    ///     Renew changes list, sort through changes and replace of same version -- 1 RLOInfo each

                    // Base
                    ResLibOverwriteInfo cbgInfo = new(ConBase.ToString(), rcNew.ConBase.ToString());
                    cbgInfo.SetSourceSubCategory(SourceCategory.Bse);
                    /// IF not same as existing base: replace with overwriting; ELSE retain existing
                    if (!ConBase.Equals(rcNew.ConBase))
                    {
                        ConBase = rcNew.ConBase.Clone();
                        cbgInfo.SetOverwriteStatus();
                    }
                    else cbgInfo.SetOverwriteStatus(false);


                    // Additions
                    /// IF existing or overwriting has additions: Proceed to overwriting edits; 
                    if (ConAddits.HasElements() || rcNew.ConAddits.HasElements())
                    {
                        List<ContentAdditionals> conAdditsNew = new();

                        int nonMatchBackSetIx = 0, sameVerInsIx = 0;
                        bool noMoreCaQ = false;
                        for (int cax = 0; !noMoreCaQ; cax++)
                        {
                            ContentAdditionals ca = null, caNew = null;
                            string caStr = null, caNewStr = null;
                            if (ConAddits.HasElements(cax + 1))
                            {
                                ca = ConAddits[cax];
                                caStr = ca.ToString();
                            }
                            if (rcNew.ConAddits.HasElements(cax + 1 - nonMatchBackSetIx))
                            {
                                caNew = rcNew.ConAddits[cax - nonMatchBackSetIx];
                                caNewStr = caNew.ToString();
                            }

                            ResLibOverwriteInfo caInfo = new(caStr, caNewStr);
                            caInfo.SetSourceSubCategory(SourceCategory.Adt);
                            noMoreCaQ = ca == null && caNew == null;

                            if (!noMoreCaQ)
                            {
                                /// IF existing conAddit exists:..; ...
                                if (ca != null)
                                {
                                    /// IF same version as overwritten:..; ...
                                    if (ca.VersionAdded.Equals(verNum))
                                    {
                                        /// IF got overwriting instance: 
                                        ///     IF not same as existing addit:
                                        ///         IF can be connected to base: replace with overwriting; 
                                        ///         ELSE check existing connect to TRUE
                                        ///     ELSE check existing connect to TRUE;
                                        /// ELSE remove existing
                                        if (caNew != null)
                                        {
                                            bool checkExistingConnectQ = false;
                                            if (!ca.Equals(caNew))
                                            {
                                                if (ContainsDataID(caNew.RelatedDataID, out _))
                                                {
                                                    conAdditsNew.Add(caNew.Clone());
                                                    caInfo.SetOverwriteStatus();
                                                }
                                                else checkExistingConnectQ = true;
                                            }
                                            else checkExistingConnectQ = true;

                                            /// IF check existing connect: 
                                            ///     IF existing connects to base: retain existing; ELSE remove existing
                                            if (checkExistingConnectQ)
                                            {
                                                if (ContainsDataID(ca.RelatedDataID, out _))
                                                {
                                                    caInfo.SetOverwriteStatus(false);
                                                    conAdditsNew.Add(ca);
                                                }
                                                else
                                                {
                                                    caInfo.SetOverwriteStatus();
                                                    caInfo.SetLooseContentStatus();
                                                }
                                            }
                                        }
                                        else caInfo.SetOverwriteStatus();
                                    }
                                    /// ELSE;
                                    ///     IF existing connects to base: retain existing; ELSE remove existing;
                                    else
                                    {
                                        if (ContainsDataID(ca.RelatedDataID, out _))
                                        {
                                            caInfo.SetOverwriteStatus(false);
                                            conAdditsNew.Add(ca);
                                            nonMatchBackSetIx++;
                                        }
                                        else
                                        {
                                            caInfo.SetOverwriteStatus();
                                            caInfo.SetLooseContentStatus();
                                        }
                                    }

                                    if (ca.VersionAdded.AsNumber <= verNum.AsNumber)
                                        sameVerInsIx++;
                                }

                                /// 'ELSE' IF no existing but have overwriting: (IF can be connected to base: add overwriting); 
                                if (ca == null && caNew != null)
                                {
                                    if (ContainsDataID(caNew.RelatedDataID, out _))
                                    {
                                        /// adds the overwrite instance at the end of matching version number group where applicable
                                        if (conAdditsNew.Count <= sameVerInsIx)
                                            conAdditsNew.Add(caNew.Clone());
                                        else
                                        {
                                            conAdditsNew.Insert(sameVerInsIx, caNew.Clone());
                                            sameVerInsIx++;
                                        }
                                        caInfo.SetOverwriteStatus();
                                    }
                                }                                
                            }                            

                            // add overwriting info
                            if (caInfo.IsSetup())
                                infoDock.Add(caInfo);
                        }

                        /// updates to new list of conAddits
                        if (conAdditsNew.HasElements())
                        {
                            if (ConAddits.HasElements())
                                ConAddits.Clear();
                            else ConAddits = new List<ContentAdditionals>();
                            //ConAddits.AddRange(conAdditsNew.ToArray());

                            foreach (ContentAdditionals ca in conAdditsNew)
                                StoreConAdditional(ca);
                        }
                    }


                    // Changes
                    /// IF existing or overwriting has changes: Proceed to overwriting edits; 
                    if (ConChanges.HasElements() || rcNew.ConChanges.HasElements())
                    {
                        List<ContentChanges> conChangesNew = new();

                        int nonMatchBacksetIx = 0, sameVerInsIx = 0;
                        bool noMoreCcQ = false;
                        for (int ccx = 0; !noMoreCcQ; ccx++)
                        {
                            ContentChanges cc = null, ccNew = null;
                            string ccStr = null, ccNewStr = null;
                            if (ConChanges.HasElements(ccx + 1))
                            {
                                cc = ConChanges[ccx];
                                ccStr = cc.ToString();
                            }
                            if (rcNew.ConChanges.HasElements(ccx + 1 - nonMatchBacksetIx))
                            {
                                ccNew = rcNew.ConChanges[ccx - nonMatchBacksetIx];
                                ccNewStr = ccNew.ToString();
                            }

                            ResLibOverwriteInfo ccInfo = new(ccStr, ccNewStr);
                            ccInfo.SetSourceSubCategory(SourceCategory.Upd);
                            noMoreCcQ = cc == null && ccNew == null;

                            if (!noMoreCcQ)
                            {
                                /// IF existing conChange exists
                                if (cc != null)
                                {
                                    /// IF same version as overwritten:..; ...
                                    if (cc.VersionChanged.Equals(verNum))
                                    {
                                        /// IF got overwriting instance: 
                                        ///     IF not same as existing addit:
                                        ///         IF can be connected to base: replace with overwriting; 
                                        ///         ELSE check existing connect to TRUE;
                                        ///     ELSE check existing connect to TRUE
                                        /// ELSE remove existing
                                        if (ccNew != null)
                                        {
                                            bool checkExistingConnectQ = false;
                                            if (!cc.Equals(ccNew))
                                            {
                                                if (ContainsDataID(ccNew.RelatedDataID, out _))
                                                {
                                                    conChangesNew.Add(ccNew.Clone());
                                                    ccInfo.SetOverwriteStatus();
                                                }
                                                else checkExistingConnectQ = true;
                                            }
                                            else checkExistingConnectQ = true;

                                            /// IF check existing connect: 
                                            ///     IF existing connects to base: retain existing; ELSE remove existing
                                            if (checkExistingConnectQ)
                                            {
                                                if (ContainsDataID(cc.RelatedDataID, out _))
                                                {
                                                    conChangesNew.Add(cc);
                                                    ccInfo.SetOverwriteStatus(false);
                                                }
                                                else
                                                {
                                                    ccInfo.SetOverwriteStatus();
                                                    ccInfo.SetLooseContentStatus();
                                                }
                                            }
                                        }

                                        /// this conChanges instance is then... DELETED!
                                        else ccInfo.SetOverwriteStatus(true);
                                    }
                                    /// ELSE;
                                    ///     IF existing connects to base: retain existing; ELSE remove existing
                                    else
                                    {
                                        if (ContainsDataID(cc.RelatedDataID, out _))
                                        {
                                            ccInfo.SetOverwriteStatus(false);
                                            conChangesNew.Add(cc);
                                            nonMatchBacksetIx++;
                                        }
                                        else
                                        {
                                            ccInfo.SetOverwriteStatus();
                                            ccInfo.SetLooseContentStatus();
                                        }
                                    }

                                    if (cc.VersionChanged.AsNumber <= verNum.AsNumber)
                                        sameVerInsIx++;
                                }
                                /// 'ELSE' IF no existing but have overwriting: (IF can be connected to base: add overwriting)
                                if (cc == null && ccNew != null)
                                {
                                    if (ContainsDataID(ccNew.RelatedDataID, out _))
                                    {
                                        /// adds the overwrite instance at the end of matching version number group where applicable
                                        if (conChangesNew.Count <= sameVerInsIx)
                                            conChangesNew.Add(ccNew.Clone());
                                        else
                                        {
                                            conChangesNew.Insert(sameVerInsIx, ccNew.Clone());
                                            sameVerInsIx++;
                                        }
                                        ccInfo.SetOverwriteStatus();
                                    }
                                }
                            }

                            // add overwriting info
                            if (ccInfo.IsSetup())
                                infoDock.Add(ccInfo);
                        }

                        /// updates to new list of conChanges
                        if (conChangesNew.HasElements())
                        {
                            if (ConChanges.HasElements())
                                ConChanges.Clear();
                            else ConChanges = new List<ContentChanges>();
                            //ConChanges.AddRange(conChangesNew.ToArray());

                            foreach (ContentChanges cc in conChangesNew)
                                StoreConChanges(cc);
                        }
                    }


                    // Insert conBase info to infoDock
                    if (cbgInfo.IsSetup())
                        infoDock.Insert(0, cbgInfo);
                }

                // Insert resCon info to infoDock
                rcInfo.SetOverwriteStatus(beginOverwriteQ && !Equals(prevSelf));
                if (rcInfo.IsSetup())
                {
                    rcInfo.SetResult(ToString());
                    infoDock.Insert(0, rcInfo);
                }
            }

            /// Compile final info dock (if it has info)
            if (infoDock.HasElements())
                info = infoDock.ToArray();
            else info = null;
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
                    if (!rcInfo[1].StartsWith(ContentChanges.ccIdentityKey))
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
                            {
                                newCa.AdoptIDs(ShelfID, ConAddits.Count);
                                newCa.ContentName = ContentName;
                                ConAddits.Add(newCa);
                            }
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
                        {
                            newChg.AdoptIDs(ShelfID, ConChanges.Count);
                            ConChanges.Add(newChg);
                        }
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

            return ShelfID != NoShelfNum && conBaseSetup;
        }
        /// <summary>Clones a resContents instance. Static to bypass <c>null</c> value issues.</summary>
        public static ResContents CloneResContent(ResContents resConToClone, bool bypassSetupQ = false)
        {
            ResContents clone = null;
            if (resConToClone is not null)
            {
                if (resConToClone.IsSetup() || (bypassSetupQ && resConToClone.ConBase != null))
                {
                    ContentBaseGroup cloneCBG = resConToClone._conBase.Clone();
                    List<ContentAdditionals> cloneCAs = new();
                    List<ContentChanges> cloneCCs = new();

                    if (resConToClone.ConAddits.HasElements())
                    {
                        foreach (ContentAdditionals ca in resConToClone.ConAddits)
                            cloneCAs.Add(ca.Clone());
                    }

                    if (resConToClone.ConChanges.HasElements())
                    {
                        foreach (ContentChanges cc in resConToClone.ConChanges)
                            cloneCCs.Add(cc.Clone());
                    }

                    clone = new ResContents(resConToClone.ShelfID, cloneCBG, cloneCAs.ToArray(), cloneCCs.ToArray());
                }
            }
            return clone;
        }


        public override string ToString()
        {
            string resConToStr = $"(RC) #{(ShelfID == NoShelfNum ? "_" : ShelfID)};";
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
