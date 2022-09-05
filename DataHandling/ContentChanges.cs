﻿using System.Collections.Generic;
using static HCResourceLibraryApp.DataHandling.DataHandlerBase;

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
		public string EncodeThridGroup()
        {
			// Syntax: {VersionUpd}*{InternalName}*{RelatedDataID}*{ChangeDesc}***
			string thirdEncode = "";
			if (IsSetup())
				thirdEncode = $"{VersionChanged}{Sep}{InternalName}{Sep}{RelatedDataID}{Sep}{ChangeDesc}";
			return thirdEncode;
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
		public bool IsSetup()
        {
			return _versionChanged.HasValue() && _internalName.IsNotNEW() && _relatedDataID.IsNotNEW() && _changeDesc.IsNotNEW();
        }

        public override string ToString()
        {
			return EncodeThridGroup().Replace(Sep, ";");
        }
        #endregion
    }
}
