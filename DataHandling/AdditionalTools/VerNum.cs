namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Represents a version number data type.</summary>
    public struct VerNum
    {
        /*  VERNUM
            Fields / Properties
            - int majorNumber (get;)
            - int minorNumber (get;)
            - prv bl hasValue

            Constructor
            - VerNum (int verMajor, int verMinor)

            Methods
            - bl TryParse(out VerNum verNum, str versionNumber)
            - bl HasValue()
            - ovrd ToString()      
            - bool Equals(VerNum vernum)
         */

        int majorNumber;
        int minorNumber;
        bool hasValue;

        public int MajorNumber { get => majorNumber; }
        public int MinorNumber { get => minorNumber; }

        /// <summary>Returns an integer representation of this VerNum instance as '<c>(a*100)+b</c>', where '<c>a</c>' is major version number, and '<c>b</c>' is minor version number..</summary>
        /// <remarks>Examples :: 1.1 --> 101 | 1.10 --> 110 | 0.10 --> 10 | 14.3 --> 1403</remarks>
        public int AsNumber
        {
            get
            {
                return (majorNumber * 100) + minorNumber;
                /// EXAMPLES
                ///  0.10   -> 10
                ///  1.10   -> 110
                ///  1.1    -> 101
                ///  14.3   -> 1403
            }
        }
        public static VerNum None { get => new VerNum(-1, -1); }


        /// <param name="verMajor">Cannot be a negative number.</param>
        /// <param name="verMinor">Cannot be a negative number.</param>
        public VerNum(int verMajor, int verMinor)
        {
            majorNumber = 0;
            minorNumber = 0;
            hasValue = false;
            
            if (verMajor >= 0 && verMinor.IsWithin(0, 99))
            {
                majorNumber = verMajor;
                minorNumber = verMinor;
                hasValue = true;
            }
        }
        public VerNum(int fromAsNumber)
        {
            majorNumber = 0;
            minorNumber = 0;
            hasValue = false;

            if (fromAsNumber >= 0)
            {
                majorNumber = fromAsNumber / 100; // 101 / 100 = 1
                minorNumber = fromAsNumber % 100; // 101 % 100 = 1
                hasValue = true;
            }
        }

        /// <summary>Specifies whether this instance of <see cref="VerNum"/> has been instantiated with valid values.</summary>
        public bool HasValue()
        {
            return hasValue;
        }
        public static bool TryParse(string s, out VerNum result, out string parseIssue)
        {
            result = None;
            parseIssue = null;
            if (s.IsNotNEW())
            {
                if (s.Contains("."))
                {
                    if (s.ToLower().Contains("v"))
                        s = s.ToLower().Replace("v", "");

                    string[] splitStr = s.Split('.');
                    if (splitStr.HasElements(2))
                    {
                        bool parsedMaj = int.TryParse(splitStr[0], out int majNum);
                        bool parsedMin = int.TryParse(splitStr[1], out int minNum);
                        if (parsedMaj && parsedMin)
                        {
                            if (majNum >= 0 && minNum.IsWithin(0, 99))
                                result = new VerNum(majNum, minNum);
                            else
                            {
                                if (majNum >= 0)
                                    parseIssue = "Minor number must be within range: 0~99";
                                else parseIssue = "Major number must be non-negative";
                            }
                        }
                        else
                        {
                            if (parsedMaj)
                                parseIssue = "Minor number imparsable, NaN";
                            else parseIssue = "Major number imparsable, NaN";
                        }
                    }
                } 
                else parseIssue = "Missing '.' in syntax 'a.bb'";
            }
            else parseIssue = "Recieved nothing to parse from";
            
            return result.HasValue();
        }
        public static bool TryParse(string s, out VerNum result)
        {
            return TryParse(s, out result, out _);
        }

        /// <returns>A string of VerNum values as '<c>v{majNum}.{minNum}</c>'.</returns>
        public override string ToString()
        {
            return $"v{MajorNumber:0}.{MinorNumber:00}";
        }
        /// <returns>A string of VerNum values as '<c>{majNum}.{minNum}</c>'.</returns>
        public string ToStringNums()
        {
            return $"{MajorNumber:0}.{MinorNumber:00}";
        }
        public bool Equals(VerNum vernum)
        {
            bool equalsQ = false;
            if (vernum.hasValue == hasValue)
                if (vernum.MajorNumber == MajorNumber && vernum.MinorNumber == MinorNumber)
                    equalsQ = true;
            return equalsQ;
        }
    }
}
