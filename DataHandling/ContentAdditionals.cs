﻿using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
	public class ContentAdditionals : DataHandlerBase
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
			- List<str> dataIds (priv set;)		Neccessary??
			- str this[int] (get -> dataIds[]) 

		Constructors
			- CA()
			- CA(verNum verNum, str relatedDataID, str optionalName, params str[] dataIDs)

		Methods
			- void AddDataIDs(params str[] dataIDs)			Neccessary??
				Add data ids to the dataIds collection
			- void RemoveDataIDs(params str[] dataIDs)		Neccessary??
				Removes data ids from the dataIDs collection
			- List<str> FetchSimilarDataIDs(str piece)
                Iterates through dataIDs collection and searches for dataIDs that are exact or contain 'piece'
            - str EncodeSecondGroup()
			- bl DecodeSecondGroup(str info)
			- bool ContainsDataID(str dataIDtoFind)
			- bl ChangesDetected()
                Compares itself against an orignal copy of itself for changes    
			- bl IsSetup()
			- bl Equals(CA other)
			- bl ovrd ToString()
         ***/

		#region fields / props
		VerNum _versionAdded, _prevVersionAdded;
		string _optionalName, _relatedDataID, _prevOptionalName, _prevRelatedDataID;
		List<string> _dataIDs, _prevDataIDs;

		// public
		/// <summary>Version number this additional content was added (to base content).</summary>
		public VerNum VersionAdded
		{
			get => _versionAdded;
			private set
            {
				if (value.HasValue())
					_versionAdded = value;
            }
        }
        /// <summary>The optional name given to this group of additional content(s).</summary>
		public string OptionalName
		{
			get => _optionalName;
			private set
            {
				if (value.IsNotNEW())
					_optionalName = value;
            }
        }
        /// <summary>The data ID from the main content to which these additional content(s) related.</summary>
		public string RelatedDataID
		{
			get => _relatedDataID;
			set
            {
				if (value.IsNotNEW())
					_relatedDataID = value;
				else _relatedDataID = null;
            }
        }
		/// <summary>Retrieves the data ID of this additional content at <paramref name="index"/>.</summary>
		/// <param name="index">This value will be clamped within the index range of the Data IDs array.</param>
		public string this[int index]
        {
			get
            {
				string dataID = null;
				if (_dataIDs.HasElements())
                {
					index = index.Clamp(0, _dataIDs.Count - 1);
					dataID = _dataIDs[index];
                }
				return dataID;
            }
        }
		public int CountIDs
		{
			get
			{
				int idCount = 0;
				if (_dataIDs.HasElements())
					idCount = _dataIDs.Count;
				return idCount;
			}
		}
		public string DataIDString
		{
			get
			{
				string dataIds = "";
				if (_dataIDs.HasElements())
					foreach(string datId in _dataIDs)
						dataIds += $"{datId} ";
				return dataIds.Trim();
			}
		}
		#endregion

		public ContentAdditionals() { }
		public ContentAdditionals(VerNum verNumAddit, string relatedDataID, string optionalName, params string[] dataIDs)
        {
			VersionAdded = verNumAddit;
			RelatedDataID = relatedDataID;
			OptionalName = optionalName;
			if (dataIDs.HasElements())
            {
				_dataIDs = new List<string>();
				foreach (string dataID in dataIDs.SortWords())
					if (dataID.IsNotNEW())
						_dataIDs.Add(dataID);

            }
        }


		#region methods
		//List<string> FetchSimilarDataIDs(string piece) { } // postpone for much later, when doing searches
		public bool ContainsDataID(string dataIDtoFind)
        {
			bool containsDataIDq = false;
			if (dataIDtoFind.IsNotNEW() && IsSetup())
				for (int dix = 0; dix < _dataIDs.Count && !containsDataIDq; dix++)
					containsDataIDq = this[dix] == dataIDtoFind;
			return containsDataIDq;
        }
		public string EncodeSecondGroup()
        {
			// Syntax: {VersionAddit}*{RelatedDataId}*{Opt.Name}*{DataID}***
			string secondEncode = "";
			if (IsSetup())
            {
				secondEncode = $"{VersionAdded}{Sep}{RelatedDataID}{Sep}{OptionalName}{Sep}";
				for (int s = 0; s < _dataIDs.Count; s++)
					secondEncode += this[s] + (s + 1 < _dataIDs.Count ? "," : "");
            }
			return secondEncode;
		}
		public bool DecodeSecondGroup(string caInfo)
		{
			/**
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
			 **/

			if (caInfo.IsNotNEW())
			{
				if (caInfo.Contains(Sep) && caInfo.CountOccuringCharacter(Sep[0]) == 3)
				{
					string[] secondParts = caInfo.Split(Sep);
					/// version
					if (secondParts[0].IsNotNEW())
					{
						if (VerNum.TryParse(secondParts[0], out VerNum caVerNum))
							VersionAdded = caVerNum;
					}
					/// relatedDataID
					if (secondParts[1].IsNotNEW())
						RelatedDataID = secondParts[1];
					/// opt.name
					if (secondParts[2].IsNotNEW())
						OptionalName = secondParts[2];
					/// dataID(s)
					if (secondParts[3].IsNotNEW())
					{
                        _dataIDs = new List<string>();
                        if (secondParts[3].Contains(','))
                        {
                            string[] caDataIDs = secondParts[3].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                            if (caDataIDs.HasElements())
                                _dataIDs.AddRange(caDataIDs);
                        }
                        else _dataIDs.Add(secondParts[3]);
                    }

					SetPreviousSelf();
				}
			}
			return IsSetup();
        }
        /// <summary>Has this instance of <see cref="ContentAdditionals"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the version added, data IDs, and either the optional name or related ID has been given values.</returns>
        public override bool IsSetup()
        {
			return _versionAdded.HasValue() && (_optionalName.IsNotNEW() || _relatedDataID.IsNotNEW()) && _dataIDs.HasElements();
		}
		public override bool ChangesMade()
		{
			return !Equals(GetPreviousSelf());
		}
		void SetPreviousSelf()
		{
			_prevVersionAdded = _versionAdded;
			_prevOptionalName = _optionalName;
			_prevRelatedDataID = _relatedDataID;
			if (_dataIDs.HasElements())
			{
				_prevDataIDs = new List<string>();
				_prevDataIDs.AddRange(_dataIDs.ToArray());
			}	
		}
		ContentAdditionals GetPreviousSelf()
		{
			string[] prevDataIDs = null;
			if (_prevDataIDs.HasElements())
				prevDataIDs = _prevDataIDs.ToArray();
			return new ContentAdditionals(_prevVersionAdded, _prevRelatedDataID, _prevOptionalName, prevDataIDs);
		}
		/// <summary>Compares two instances for similarities against: Setup state, Version Added, Optional Name, Related Data ID, Data IDs.</summary>
		public bool Equals(ContentAdditionals ca)
        {
			bool areEquals = false;
			if (ca != null)
            {
				areEquals = true;
				for (int adx = 0; adx < 5 && areEquals; adx++)
					switch (adx)
					{
						// setup verAddit, OptName, RelDataID, DataIDs
						case 0:
							areEquals = IsSetup() == ca.IsSetup();
							break;

						case 1:
							areEquals = VersionAdded.Equals(ca.VersionAdded);
							break;

						case 2:
							areEquals = OptionalName == ca.OptionalName;
							break;

						case 3:
							areEquals = RelatedDataID == ca.RelatedDataID;
							break;

						case 4:
							areEquals = _dataIDs.HasElements() == ca._dataIDs.HasElements();
							if (_dataIDs.HasElements())
                            {
								if (areEquals)
									areEquals = _dataIDs.Count == ca._dataIDs.Count;
								if (areEquals)
								{
									for (int dix = 0; dix < _dataIDs.Count && areEquals; dix++)
										areEquals = this[dix] == ca[dix];
								}
							}
							break;
					}
			}
			return areEquals;
        }

        public override string ToString()
        {
			return EncodeSecondGroup().Replace(Sep, ";");
        }
        #endregion
    }
}
