using System.Collections.Generic;
using static HCResourceLibraryApp.DataHandling.DataHandlerBase;

namespace HCResourceLibraryApp.DataHandling
{
    public class SummaryData
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
			- bl Equals(SD other)
			- bl ChangedDetected()
			- bl IsSetup()
			- ovr str ToString()
         ***/

		#region fields / props
		// private
		SummaryData _previousSelf;
		VerNum _summaryVersion;
		int _ttaNumber;
		List<string> _summaryParts;

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

		public SummaryData()
		{
			_previousSelf = (SummaryData)this.MemberwiseClone();
		}
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
			_previousSelf = (SummaryData)this.MemberwiseClone();
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
		public bool ChangesDetected()
		{
			return !Equals(_previousSelf);
		}
		public bool IsSetup()
		{
			return _summaryVersion.HasValue() && _ttaNumber > 0 && _summaryParts.HasElements();
		}

		public override string ToString()
		{
			return Encode().Replace(Sep, ";");
		}
		#endregion
	}
}
