using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public struct SearchResult
    {
        /** PLANNING
            Fields / Props
            - bl isSetupQ
            - str searchArg
            - pv str searchOptState
            - bl exactMatchQ
            - str matchingText
            - SC sourceType
            - int shelfID

            Constructors
            - SR (str searchArgs, str searchOpts, bl isExactMatch, str matchedText, SC source, int shelfId)  
        
            Methods
            - bl IsSetup
            - bl Equals(SR)

         */

        // FIELDS / PROPS
        bool isSetupQ;
        public string searchArg;
        public string searchOpt;
        public bool exactMatchQ;
        public string matchingText;
        public string contentName;
        public SourceCategory sourceType;
        public int shelfID;


        // CONSTRUCTORS
        public SearchResult(string searchArgs, string searchOpts, bool isExactMatch, string matchedText, string contentBaseName, SourceCategory source, int shelfId)
        {
            isSetupQ = true;
            searchArg = searchArgs;
            searchOpt = searchOpts;
            exactMatchQ = isExactMatch;
            matchingText = matchedText;
            contentName = contentBaseName;
            sourceType = source;
            shelfID = shelfId;
        }


        // METHODS
        /// <returns>A booleans confirming that the following has values: searchArg, matchingText, contentName, shelfID.</returns>
        public bool IsSetup()
        {
            return isSetupQ && searchArg.IsNotNEW() && matchingText.IsNotNEW() && contentName.IsNotNEW() && shelfID >= 0;
        }
        /// <summary>Compares two instances for similarities against: setup state, searchArg, searchOpt, exactMatchQ, matchingText, contentName, sourceType, shelfID.</summary>
        public bool Equals(SearchResult sr)
        {
            bool areEquals = IsSetup() == sr.IsSetup();
            if (areEquals)
            {
                for (int ax = 0; ax < 6 && areEquals; ax++)
                {
                    areEquals = ax switch
                    {
                        0 => searchArg == sr.searchArg,
                        1 => searchOpt == sr.searchOpt,
                        2 => exactMatchQ == sr.exactMatchQ,
                        3 => matchingText == sr.matchingText,
                        4 => contentName == sr.contentName,
                        5 => sourceType == sr.sourceType,
                        6 => shelfID == sr.shelfID,
                        _ => false
                    };
                }
            }
            return areEquals;
        }
    }
}
