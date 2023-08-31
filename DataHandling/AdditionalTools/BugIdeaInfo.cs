using System;
using static HCResourceLibraryApp.DataHandling.DataHandlerBase;

namespace HCResourceLibraryApp.DataHandling
{
    public class BugIdeaInfo
    {
        /** PLANNING
            Fields / Props
            - pv bl isSetupQ
            - bl isBugQ
            - str description
            - bl isNewQ
        
            Constructor
            - BII()
            - BII(bl isBugQ, str description) 

            Methods
            - bl IsSetup()
            - bl Equals(BII biInfo)
            - str Encode()
            - bl Decode(str line)
         */


        // FIELDS / PROPS
        private bool isSetupQ;
        public bool isBugQ;
        public string description;
        public bool isNewQ;


        // CONSTRUCTORS
        public BugIdeaInfo() { }
        public BugIdeaInfo(bool isBug, string desc, bool isNew = false)
        {
            isSetupQ = true;
            isBugQ = isBug;
            description = desc;
            isNewQ = isNew;
        }


        // METHODS
        public string Encode()
        {
            /** ENCODE / DECODE FORMAT             
                tag = "dbi"            
                L1|{isBugQ}*{isNewQ}*{description}
             */

            string encodeStr = "";
            if (IsSetup())
                encodeStr = $"{isBugQ}{Sep}{isNewQ}{Sep}{description}";
            return encodeStr;
        }
        public bool Decode(string line)
        {
            if (line.IsNotNEW())
            {
                string[] lineData = line.Split(Sep);
                int countDecode = 0;
                if (lineData.HasElements(3))
                {
                    /// is bug Q
                    if (bool.TryParse(lineData[0], out bool isBug))
                    {
                        isBugQ = isBug;
                        countDecode++;
                    }

                    /// is new Q
                    if (bool.TryParse(lineData[1], out bool isNew))
                    {
                        isNewQ = isNew;
                        countDecode++;
                    }

                    /// description
                    if (lineData[2].IsNotNEW())
                    {
                        description = lineData[2];
                        countDecode++;
                    }

                    isSetupQ = description.IsNotNEW() && countDecode == 3;
                }
            }
            return IsSetup();
        }
        /// <returns>A boolean stating whether this instance: is Setup, has a value for description.</returns>
        public bool IsSetup()
        {
            return isSetupQ && description.IsNotNEW();
        }
        /// <summary>Compares to instances for similarities against: isBugQ, description.</summary>
        public bool Equals(BugIdeaInfo bid)
        {
            bool areEquals = IsSetup() == bid.IsSetup();
            if (areEquals)
            {
                for (int bx = 0; bx < 2 && areEquals; bx++)
                {
                    areEquals = bx switch
                    {
                        0 => isBugQ == bid.isBugQ,
                        1 => description == bid.description,
                        _ => false
                    };
                }
            }
            return areEquals;
        }
    }
}
