namespace HCResourceLibraryApp.DataHandling
{
    public class ContentChanges : DataHandlerBase
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
			- str EncodeThirdGroup()
			- bl DecodeThirdGroup(str info)
			- bool ChangesDetected()
                Compares itself against an orignal copy of itself for changes
			- bl IsSetup()
			- bl Equals(CA other)
			- bl ovrd ToString()
        ***/

		#region fields / props
		// private
		VerNum _versionChanged, _prevVersionChanged;
		string _internalName, _relatedDataID, _changeDesc, _prevInternalName, _prevRelatedDataID, _prevChangeDesc;
		int _relatedShelfID = ResContents.NoShelfNum, _index = ResContents.NoShelfNum;

		//public
		public const string ccIdentityKey = "^";
        /// <summary>Version number this update/change took place.</summary>
        public VerNum VersionChanged
        {
			get => _versionChanged;
			private set
            {
				if (value.HasValue())
					_versionChanged = value;
            }
        }
		/// <summary>The name of the content being updated.</summary>
		/// <remarks>On a self-updated log decode, this value will end with <see cref="DataHandlerBase.Sep"/>.</remarks>
		public string InternalName
        {
			get => _internalName;
			private set
            {
				if (value.IsNotNEW())
					_internalName = value;
            }
        }
		/// <summary>The data ID relevant to the named content being updated.</summary>
		public string RelatedDataID
        {
			get => _relatedDataID;
			private set
            {
				if (value.IsNotNEW())
					_relatedDataID = value;
            }
        }
		/// <summary>A description of the change(s) made.</summary>
		public string ChangeDesc
        {
			get => _changeDesc;
			private set
            {
				if (value.IsNotNEW())
					_changeDesc = value;
            }
        }
        /// <summary>The Shelf ID of the related <see cref="ResContents"/> instance. Default value of <see cref="ResContents.NoShelfNum"/>.</summary>
        public int RelatedShelfID
        {
            get => _relatedShelfID;
        }
        /// <summary>The index number of this instance within the <see cref="ResContents.ConAddits"/> of a related <see cref="ResContents"/>. Default value of <see cref="ResContents.NoShelfNum"/>.</summary>
        public int Index
        {
            get => _index;
        }
        #endregion

        public ContentChanges() { }
		public ContentChanges(VerNum verNumChanged, string internalName, string relatedDataID, string description)
        {
			VersionChanged = verNumChanged;
			InternalName = internalName;
			RelatedDataID = relatedDataID;
			ChangeDesc = description;
        }


		#region methods
		public string EncodeThirdGroup()
        {
			// Syntax: {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***

			// Syntax Change: ^^{VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
			//		? -Mix up between ConAdts and ConChgs instances' decoding
            string thirdEncode = "";
			if (IsSetup())
			{
                thirdEncode = $"{ccIdentityKey}{VersionChanged}{Sep}{InternalName}{Sep}{RelatedDataID}{Sep}{ChangeDesc}";
				SetPreviousSelf();
            }
			return thirdEncode;
        }
		public bool DecodeThirdGroup(string ccInfo)
		{
            /**
			 Syntax: {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***

			FROM DESIGN DOC
			........
			> {VersionUpd}
				[REQUIRED] string value as "a.bb"
				"a" represents major version number
				"bb" represents minor version number
				May not contain the '*' symbol
			> {InternalName}
				string value
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

			Syntax Change: ^^{VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
				? - Mix up between ConAdts and ConChgs instances' decoding
			**/

            /// multiple CCs (separated with '***' are taken care of in ResContents decoding)
            if (ccInfo.IsNotNEW())
			{
				if (ccInfo.Contains(Sep) && ccInfo.CountOccuringCharacter(Sep[0]) == 3)
				{				
					string[] thirdParts = ccInfo.Split(Sep);
					/// version
					if (thirdParts[0].IsNotNEW())
					{
						thirdParts[0] = thirdParts[0].Replace(ccIdentityKey, "");
						if (VerNum.TryParse(thirdParts[0], out VerNum ccVerNum))
							VersionChanged = ccVerNum;
					}
					/// internalName
					if (thirdParts[1].IsNotNEW())
						InternalName = thirdParts[1];
					/// relatedDataID
					if (thirdParts[2].IsNotNEW())
						RelatedDataID = thirdParts[2];
					/// changeDesc
					if (thirdParts[3].IsNotNEW())
						ChangeDesc = thirdParts[3];

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
			_prevVersionChanged = _versionChanged;
			_prevInternalName = _internalName;
			_prevRelatedDataID = _relatedDataID;
			_prevChangeDesc = _changeDesc;
		}
		ContentChanges GetPreviousSelf()
		{
			return new ContentChanges(_prevVersionChanged, _prevInternalName, _prevRelatedDataID, _prevChangeDesc);
		}
		/// <summary>Compares two instances for similarities against: Setup state, Version Changed, Internal Name, Related Data ID, Change Description.</summary>
		/// <param name="ignoreVerQ">If <c>true</c>, will compare the instances without comparing <see cref="VersionChanged"/> values.</param>
		public bool Equals(ContentChanges cc, bool ignoreVerQ = false)
        {
			bool areEquals = false;
			if (cc != null)
            {
				areEquals = true;
				for (int cix = 0; cix < 5 && areEquals; cix++)
					switch (cix)
                    {
						case 0:
							areEquals = cc.IsSetup() == IsSetup();
							break;

						case 1:
							areEquals = ignoreVerQ || cc.VersionChanged.Equals(VersionChanged);
							break;

						case 2:
							areEquals = cc.InternalName == InternalName;
							break;

						case 3:
							areEquals = cc.RelatedDataID == RelatedDataID;
							break;

						case 4:
							areEquals = cc.ChangeDesc == ChangeDesc;
							break;
                    }
            }
			return areEquals;
        }
		/// <summary>Has this instance of <see cref="ContentChanges"/> been initialized with the appropriate information?</summary>
		/// <returns>A boolean stating whether the version changed, related data ID, and change description has been given values.</returns>
		public override bool IsSetup()
        {
			return _versionChanged.HasValue() && /*_internalName.IsNotNEW() &&*/ _relatedDataID.IsNotNEW() && _changeDesc.IsNotNEW();
        }
		public ContentChanges Clone()
		{
			ContentChanges clone = null;
			if (IsSetup())
			{
                clone = new ContentChanges(VersionChanged, InternalName, RelatedDataID, ChangeDesc);
				clone.AdoptIDs(_relatedShelfID, _index);
            }
			return clone;
		}
        /// <returns>A loosened instance of this (existing or overwritten) object if it no longer relates to its parent <see cref="ResContents"/>. Returns a <c>null</c> value otherwise.</returns>
        public ContentChanges OverwriteLoose(ContentChanges looseCc, ResContents parentRC, out ResLibOverwriteInfo info)
		{
			ContentChanges loosenedCc = null;
			info = new ResLibOverwriteInfo();
			if (IsSetup() && looseCc != null)
			{
                /// considerations
                ///		- the overwriting loose changes should have the same related data ID and version number as the existing
                ///			- If it does, then overwriting can replace the existing info
                ///			- If not, then the overwriting is ignored; the existing info cannot be overwritten (must enter through regular integration)
				///			----
                ///			Post consideration
                ///			- If the related IDs are not the same, but otherwise the addits are the same, overwrite the related data ID [not implemented]

                if (looseCc.IsSetup() && !Equals(looseCc))
				{
					info = new ResLibOverwriteInfo(ToString(), looseCc.ToString());
					info.SetSourceSubCategory(SourceCategory.Upd);
					if (RelatedDataID == looseCc.RelatedDataID && VersionChanged.Equals(looseCc.VersionChanged))
					{
						if (looseCc.InternalName.IsNotEW() && looseCc.InternalName != ResLibrary.LooseResConName)
							InternalName = looseCc.InternalName;
						ChangeDesc = looseCc.ChangeDesc;
						info.SetOverwriteStatus(info.contentExisting != ToString());
						info.SetResult(ToString());
					}
					else info.SetOverwriteStatus(false);

                    /// check if loosened
                    if (parentRC != null)
                    {
                        if (parentRC.IsSetup())
                            if (!parentRC.ContainsDataID(RelatedDataID, out _))
                            {
                                loosenedCc = Clone();
                                parentRC.DisposeConChanges(loosenedCc);
								info.SetLooseContentStatus();
                            }
                    }
                }
            }

			return loosenedCc;
        }
        /// <summary>Stores the Shelf ID of the related <see cref="ResContents"/> instance and its Index number within the related instance. Both at a minimum value of <c>0</c>.</summary>
        public void AdoptIDs(int shelfID, int index)
        {
            if (shelfID >= 0)
                _relatedShelfID = shelfID;

			if (index >= 0)
				_index = index;
        }

        public override string ToString()
        {
			return EncodeThirdGroup().Replace(Sep, ";").Replace(ccIdentityKey, "");
        }
		public string ToStringShortened()
		{
			return ToString().Clamp(50, "...");
		}
        #endregion
    }
}
