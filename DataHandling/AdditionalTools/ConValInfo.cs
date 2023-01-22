using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public struct ConValInfo
    {
        /** CONTENT VALIDATION INFO PLAN
        FIELDS / PROPS    
        - bl validatedQ
        - str dataID
        - str expandedDataID (?)
        - str filePath (?)

        CONSTRUCTORS
        - CVI(bl isValidatedQ, str storedDataID)
        - CVI(bl isValidatedQ, str storedDataID, str longDataID, str contentPath)
                        
        METHODS
        - bl Equals(CIV other)
        - bl IsSetup()
         */

        public readonly bool validatedQ;
        public readonly string dataID;
        readonly bool initializedQ;

        public static ConValInfo Empty { get => new ConValInfo(); }
        
        public ConValInfo(string storedDataID, bool isValidatedQ)
        {
            validatedQ = false;
            dataID = null;
            initializedQ = false;

            if (storedDataID.IsNotNEW())
            {
                validatedQ = isValidatedQ;
                dataID = storedDataID;
                initializedQ = true;
            }
        }


        public bool Equals(ConValInfo other)
        {
            bool areEquals = true;
            for (int cx = 0; cx < 3 && areEquals; cx++)
            {
                switch (cx)
                {
                    case 0:
                        areEquals = IsSetup() == other.IsSetup();
                        break;

                    case 1:
                        if (IsSetup())
                            areEquals = dataID == other.dataID;
                        break;

                    case 2:
                        if (IsSetup())
                            areEquals = validatedQ == other.validatedQ;
                        break;
                }
            }
            return areEquals;
        }
        public bool IsSetup()
        {
            return initializedQ && dataID.IsNotNEW();
        }
    }
}
