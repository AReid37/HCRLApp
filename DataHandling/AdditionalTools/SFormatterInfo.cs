using System;

namespace HCResourceLibraryApp.DataHandling
{
    public struct SFormatterInfo
    {
        /** SFormatterInfo planning
            
            Fields/Props
            - rd pl int lineNumber
            - rd pl str errorCode
            - rd pl str errorMessage
            - rd pl str lineData??

            Constructor
            - SFI (int lineNum, str eCode, str eMessage)

            Methods
            - bl IsSetup()
            - bl Equals(SFI sfi)

            where 'SFI' is SFormatterInfo.cs for returning error message
         */

        public readonly int lineNumber;
        public readonly string errorCode;
        public readonly string errorMessage;

        public SFormatterInfo(int lineNum, string errCode, string errMessage)
        {
            lineNumber = lineNum;
            errorCode = errCode;
            errorMessage = errMessage;
        }

        public bool IsSetup()
        {
            return lineNumber != 0 && errorCode.IsNotNEW() && errorMessage.IsNotNEW();
        }
        public bool Equals(SFormatterInfo sfi)
        {
            bool areEquals = IsSetup() == sfi.IsSetup();
            for (int ax = 0; ax < 3 && areEquals; ax++)
            {
                areEquals = ax switch
                {
                    0 => lineNumber == sfi.lineNumber,
                    1 => errorCode == sfi.errorCode,
                    2 => errorMessage == sfi.errorMessage,
                    _ => true,
                };
            }
            return areEquals;
        }
    }
}
