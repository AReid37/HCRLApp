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
		ContentChanges _previousSelf;
		VerNum _versionChanged;
		string _internalName, _relatedDataID, _changeDesc;

		//public
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
        #endregion

		public ContentChanges()
        {
			_previousSelf = (ContentChanges)this.MemberwiseClone();
        }
		public ContentChanges(VerNum verNumChanged, string internalName, string relatedDataID, string description)
        {
			VersionChanged = verNumChanged;
			InternalName = internalName;
			RelatedDataID = relatedDataID;
			ChangeDesc = description;
			_previousSelf = (ContentChanges)this.MemberwiseClone();
        }


		#region methods
		public string EncodeThirdGroup()
        {
			// Syntax: {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
			string thirdEncode = "";
			if (IsSetup())
				thirdEncode = $"{VersionChanged}{Sep}{InternalName}{Sep}{RelatedDataID}{Sep}{ChangeDesc}";
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

					_previousSelf = (ContentChanges)this.MemberwiseClone();
				}
			}
			return IsSetup();
		}
		public bool ChangesDetected()
        {
			return !Equals(_previousSelf);
        }
		public bool Equals(ContentChanges cc)
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
							areEquals = cc.VersionChanged.Equals(VersionChanged);
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

        public override string ToString()
        {
			return EncodeThirdGroup().Replace(Sep, ";");
        }
		public string ToStringShortened()
		{
			return ToString().Clamp(50, "...");
		}
        #endregion
    }
}
