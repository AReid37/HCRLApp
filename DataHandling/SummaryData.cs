using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public class SummaryData : DataHandlerBase
    {
		/*** SUMMARY DATA
        Data form for version summaries and TTA
        Syntax: {Version}*{tta}*{summaryParts}

        FROM DESIGN DOC
        ..........
        > {Version}
			[REQUIRED] string value as "a.bb"
			"a" represents major version number
			"bb" represents minor version number
			May not contain the '*' symbol
		> {tta}
			[REQUIRED] integer value
			technically should satisfy rule: x > 0.
		> {summaryParts}
			[REQUIRED] separator-key-separated string values
			Each string value may not contain '*' symbol
		NOTES
			- {summaryParts} has no limit of separated summary parts, but must at least have one summary part. If only one string, an asterik (*) does not need to follow.
			- {tta} may not be less than or equal to 0, but this is a rule that can be broken (not strict in program; only warnings).
        ..........


		Fields / Props
			- SD previousSelf
			- VerNum summaryVersion (prv set; get;)
			- int ttaNumber (prv set; get;)
			- List<str> summaryParts (prv set; get;)

		Constructors
			- SD()
			- SD(VerNum summaryVersion, int ttaCount, params str[] summaryParts)

		Methods
			- vd AddSummaryPart(str part)	Necessary??
			- str Encode()
			- bl Decode(str info)
			- bl Equals(SD other)
			- bl ChangedDetected()
			- bl IsSetup()
			- ovr str ToString()
         ***/

		#region fields / props
		// private
		VerNum _summaryVersion, _prevSummaryVersion;
		int _ttaNumber, _prevTtaNumber;
		List<string> _summaryParts, _prevSummaryParts;

        // public
		public VerNum SummaryVersion
		{
			get => _summaryVersion;
			private set
			{
				if (value.HasValue())
					_summaryVersion = value;
			}
		}
		public int TTANum
		{
			get => _ttaNumber;
			private set => _ttaNumber = value;
		}
		public List<string> SummaryParts
		{
			get => _summaryParts;
			private set
			{
				if (value.HasElements())
					_summaryParts = value;
			}
		}
		#endregion

		public SummaryData() { }
		public SummaryData(VerNum summaryVersion, int ttaCount, params string[] summaryParts)
		{
			SummaryVersion = summaryVersion;
			TTANum = ttaCount;
			if (summaryParts.HasElements())
			{
				_summaryParts = new List<string>();
				foreach (string sumPart in summaryParts)
					if (sumPart.IsNotNEW())
						_summaryParts.Add(sumPart);
			}
		}

        #region methods
		public string Encode()
		{
            // Syntax: {Version}*{tta}*{summaryParts}
            // > {summaryParts}
			//		[REQUIRED] separator-key-separated string values
            string fullEncode = "";
			if (IsSetup())
			{
				fullEncode = $"{SummaryVersion}{Sep}{TTANum}{Sep}";
				for (int sx = 0; sx < SummaryParts.Count; sx++)
					fullEncode += $"{SummaryParts[sx]}{(sx + 1 >= SummaryParts.Count ? "" : Sep)}";
			}
			return fullEncode;
		}
		public bool Decode(string sumInfo)
		{
            /**
			Syntax: {Version}*{tta}*{summaryParts}

			FROM DESIGN DOC
			..........
			> {Version}
				[REQUIRED] string value as "a.bb"
				"a" represents major version number
				"bb" represents minor version number
				May not contain the '*' symbol
			> {tta}
				[REQUIRED] integer value
				technically should satisfy rule: x > 0.
			> {summaryParts}
				[REQUIRED] separator-key-separated string values
				Each string value may not contain '*' symbol
			NOTES
				- {summaryParts} has no limit of separated summary parts, but must at least have one summary part. If only one string, an asterik (*) does not need to follow.
				- {tta} may not be less than or equal to 0, but this is a rule that can be broken (not strict in program; only warnings).
			..........
			 **/

			if (sumInfo.IsNotNEW())
			{
				if (sumInfo.Contains(Sep) && sumInfo.CountOccuringCharacter(Sep[0]) >= 2)
				{
					string[] sumParts = sumInfo.Split(Sep, System.StringSplitOptions.RemoveEmptyEntries);
					if (sumParts.HasElements())
						for (int six = 0; six < sumParts.Length; six++)
						{
							if (sumParts[six].IsNotNEW())
							{
								/// version
								if (six == 0)
								{
									if (VerNum.TryParse(sumParts[six], out VerNum sumVerNum))
										SummaryVersion = sumVerNum;
								}
								/// tta
								else if (six == 1)
								{
									if (int.TryParse(sumParts[six], out int ttaNum))
										TTANum = ttaNum;
								}
								/// summaryParts
								else
								{
									if (!_summaryParts.HasElements())
										_summaryParts = new List<string>();
									SummaryParts.Add(sumParts[six]);
								}
							}
						}

					SetPreviousSelf();
				}
			}
			return IsSetup();
        }
        /// <summary>Compares two instances for similarities against: Setup state, Summary Version, TTA Number, Summary Parts.</summary>
        public bool Equals(SummaryData sumDat)
		{
			bool areEquals = false;
			if (sumDat != null)
			{
				areEquals = true;
				for (int six = 0; six < 4 && areEquals; six++)
				{
					switch (six)
					{
						case 0:
							areEquals = IsSetup() == sumDat.IsSetup();
							break;

						case 1:
							areEquals = SummaryVersion.Equals(sumDat.SummaryVersion);
							break;

						case 2:
							areEquals = TTANum == sumDat.TTANum;
							break;

						case 3:
							areEquals = SummaryParts.HasElements() == sumDat.SummaryParts.HasElements();
							if (areEquals && SummaryParts.HasElements())
							{
								areEquals = SummaryParts.Count == sumDat.SummaryParts.Count;
								if (areEquals)
								{
									for (int sx = 0; sx < SummaryParts.Count && areEquals; sx++)
										areEquals = SummaryParts[sx] == sumDat.SummaryParts[sx];
								}
							}
							break;
					}
				}
			}
			return areEquals;
		}
		public override bool ChangesMade()
		{
			return !Equals(GetPreviousSelf());
		}
		void SetPreviousSelf()
		{
			_prevSummaryVersion = _summaryVersion;
			_prevTtaNumber = _ttaNumber;
			if (_summaryParts.HasElements())
			{
				_prevSummaryParts = new List<string>();
				_prevSummaryParts.AddRange(_summaryParts.ToArray());
			}
		}
		SummaryData GetPreviousSelf()
		{
			string[] prevSummaryParts = null;
			if (_prevSummaryParts.HasElements())
				prevSummaryParts = _prevSummaryParts.ToArray();
			return new SummaryData(_prevSummaryVersion, _prevTtaNumber, prevSummaryParts);
		}
		/// <summary>Has this instance of <see cref="ResContents"/> been initialized with the appropriate information?</summary>
		/// <returns>A boolean stating whether the summary version, tta number, and summary parts have been given values, at minimum.</returns>
		public override bool IsSetup()
		{
			return _summaryVersion.HasValue() && _ttaNumber >= 0 && _summaryParts.HasElements();
		}

		public override string ToString()
		{
			return Encode().Replace(Sep, ";");
		}
		public string ToStringShortened()
		{
            string fullEncode = "";
            if (IsSetup())
            {
                fullEncode = $"{SummaryVersion}{Sep}{TTANum}{Sep}";
                for (int sx = 0; sx < SummaryParts.Count; sx++)
                    fullEncode += $"{SummaryParts[sx].Clamp(30, "...")}{(sx + 1 >= SummaryParts.Count ? "" : Sep)}";
            }
            return fullEncode.Replace(Sep, ";");
        }
		#endregion
	}
}
