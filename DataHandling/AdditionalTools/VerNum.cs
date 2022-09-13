using System;

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
        public static VerNum None { get => new VerNum(-1, -1); }


        /// <param name="verMajor">Cannot be a negative number.</param>
        /// <param name="verMinor">Cannot be a negative number.</param>
        public VerNum(int verMajor, int verMinor)
        {
            majorNumber = 0;
            minorNumber = 0;
            hasValue = false;
            
            if (verMajor >= 0 && verMinor >= 0)
            {
                majorNumber = verMajor;
                minorNumber = verMinor;
                hasValue = true;
            }
        }

        /// <summary>Specifies whether this instance of <see cref="VerNum"/> has been instantiated with valid values.</summary>
        public bool HasValue()
        {
            return hasValue;
        }
        public static bool TryParse(string s, out VerNum result)
        {
            result = None;
            if (s.IsNotNEW())
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
                            if (majNum >= 0 && minNum >= 0)
                                result = new VerNum(majNum, minNum);
                        }
                    }
                }            
            return result.HasValue();
        }

        public override string ToString()
        {
            return $"v{MajorNumber:0}.{MinorNumber:00}";
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
