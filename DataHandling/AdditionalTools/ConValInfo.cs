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

        // FIELDS / PROPS
        bool validatedQ;
        readonly string dataID;
        readonly string ogDataID;
        readonly bool initializedQ;

        public bool IsValidated { get => validatedQ; }
        public string DataID { get => dataID; }
        public string OriginalDataID { get => ogDataID; }
        public static ConValInfo Empty { get => new(); }
        
        // CONSTRUCTOR
        public ConValInfo(string storedDataID, string expandedDataID)
        {
            validatedQ = false;
            dataID = null;
            ogDataID = null;
            initializedQ = false;

            if (storedDataID.IsNotNEW())
            {
                ogDataID = storedDataID;
                dataID = expandedDataID;
                initializedQ = true;
            }
        }

        // METHODS
        public void ConfirmValidation()
        {
            validatedQ = true;
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
            return initializedQ && dataID.IsNotNEW() && ogDataID.IsNotNEW();
        }        
    }
}
