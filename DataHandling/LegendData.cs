using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public class LegendData : DataHandlerBase
    {
        /*** LEGEND DATA
        Data form for legend keys and definitions
        Syntax: {key}*{keynames}***

        FROM DESIGN DOC
        ..........
        > {key}
		    [REQUIRED] string value
		    May not contain '*' symbol or comma (',')
	    > {keynames}
		    [REQUIRED] seperator-separated string values
		    May not contain '*' symbol
	    NOTES
		    - Multiple legend keys and keynames must be separated with '***'
        ..........
        

        Fields / Props
            - LD previousSelf
            - str key (prv set; get;)
            - List<str> definitions (prv set;)
            - this[int] (get;)
                Retrieves the definition for this key by index

        Constructors
            - LD()
            - LD(str legKey)
            - LD(str legKey, params str[] definitions]

        Methods
            - vd AddKeyDefinition(str newDefinition)
            - str Encode()
            - bl Decode (str info)
            - bl Equals(LD legDat)
            - bl ChangesDetected()
            - bl IsSetup()
            - ovr str ToString()
            - str ToStringLengthy()
         ***/

        #region fields / props
        // private
        string _key, _prevKey;
        List<string> _definitions, _prevDefinitions;

        // public        
        public string Key
        {
            get => _key;
            private set
            {
                if (value.IsNotNEW())
                    _key = value;
            }
        }
        /// <summary>Retrieves the definition of this legend key at <paramref name="index"/>.</summary>
		/// <param name="index">This value will be clamped within the index range of the Legend definitions.</param>
        public string this[int index]
        {
            get
            {
                string definition = null;
                if (_definitions.HasElements())
                {
                    index = index.Clamp(0, _definitions.Count - 1);
                    definition = _definitions[index];
                }
                return definition;
            }
        }
        public int CountDefinitions
        {
            get
            {
                int count = 0;
                if (_definitions.HasElements())
                    count = _definitions.Count;
                return count;
            }
        }
        #endregion

        public LegendData() { }
        public LegendData(string legKey)
        {
            Key = legKey;
        }
        public LegendData(string legKey, params string[] definitions)
        {
            Key = legKey;
            if (definitions.HasElements())
            {
                
                _definitions = new List<string>();
                foreach (string def in definitions)
                    if (def.IsNotNEW())
                        _definitions.Add(def.Trim());
            }
        }

        #region methods
        public bool AddKeyDefinition(string newDefinition)
        {
            bool addedNewDef = false;
            if (newDefinition.IsNotNEW())
            {
                newDefinition = newDefinition.Trim();
                if (_definitions.HasElements())
                {
                    bool isDupe = false;
                    foreach (string def in _definitions)
                    {
                        if (def == newDefinition)
                        {
                            isDupe = true;
                            break;
                        }
                    }

                    if (!isDupe)
                    {
                        _definitions.Add(newDefinition);
                        addedNewDef = true;
                    }
                }
                else
                {
                    _definitions = new List<string>();
                    _definitions.Add(newDefinition);
                    addedNewDef = true;
                }
            }
            return addedNewDef;
        }
        public string Encode()
        {
            // Syntax: {key}*{keynames}***
            // > {keynames}
            //      [REQUIRED] seperator - separated string values
            string fullEncode = "";
            if (IsSetup())
            {
                fullEncode = $"{Key}{Sep}";
                for (int dix = 0; dix < _definitions.Count; dix++)
                    fullEncode += $"{this[dix]}{(dix + 1 >= _definitions.Count ? "" : Sep)}";
            }
            return fullEncode;
        }
        public bool Decode(string legInfo)
        {
            /**
             Syntax: {key}*{keynames}***

            FROM DESIGN DOC
            ..........
            > {key}
		        [REQUIRED] string value
		        May not contain '*' symbol or comma (',')
	        > {keynames}
		        [REQUIRED] seperator-separated string values
		        May not contain '*' symbol
	        NOTES
		        - Multiple legend keys and keynames must be separated with '***'
            ..........             
             **/

            if (legInfo.IsNotNEW())
            {
                if (legInfo.Contains(Sep) && legInfo.CountOccuringCharacter(Sep[0]) >= 1)
                {
                    string[] legParts = legInfo.Split(Sep, System.StringSplitOptions.RemoveEmptyEntries);
                    if (legParts.HasElements())
                        for (int lix = 0; lix < legParts.Length; lix++)
                        {
                            if (legParts[lix].IsNotNEW())
                            {
                                /// key
                                if (lix == 0)
                                    Key = legParts[lix];
                                /// definitions
                                else
                                {
                                    if (!_definitions.HasElements())
                                        _definitions = new List<string>();
                                    _definitions.Add(legParts[lix]);
                                }
                            }
                        }

                    SetPreviousSelf();
                }
            }
            return IsSetup();
        }
        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }
        void SetPreviousSelf()
        {
            _prevKey = _key;
            if (_definitions.HasElements())
            {
                _prevDefinitions = new List<string>();
                _prevDefinitions.AddRange(_definitions.ToArray());
            }
        }
        LegendData GetPreviousSelf()
        {
            string[] prevDefs = null;
            if (_prevDefinitions.HasElements())
                prevDefs = _prevDefinitions.ToArray();
            return new LegendData(_prevKey, prevDefs);
        }
        /// <summary>Compares two instances for similarities against: Setup state, Key, Definitions.</summary>
        public bool Equals(LegendData legDat)
        {
            bool areEquals = false;
            if (legDat != null)
            {
                areEquals = true;
                for (int ldx = 0; ldx < 3 && areEquals; ldx++)
                {
                    switch (ldx)
                    {
                        case 1:
                            areEquals = IsSetup() == legDat.IsSetup();
                            break;

                        case 2:
                            areEquals = Key == legDat.Key;
                            break;

                        case 3:
                            areEquals = _definitions.HasElements() == legDat._definitions.HasElements();
                            if (areEquals && _definitions.HasElements())
                            {
                                areEquals = _definitions.Count == legDat._definitions.Count;
                                if (areEquals)
                                {
                                    for (int dx = 0; dx < _definitions.Count && areEquals; dx++)
                                        areEquals = _definitions[dx] == legDat._definitions[dx];
                                }
                            }
                            break;
                    }
                }
            }
            return areEquals;
        }
        /// <summary>Has this instance of <see cref="ResContents"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the legend key and definitions have been given values, at minimum.</returns>
        public override bool IsSetup()
        {
            return _key.IsNotNEW() && _definitions.HasElements();
        }

        public override string ToString()
        {
            return Encode().Replace(Sep, ";").Clamp(50, "...");
        }
        public string ToStringLengthy()
        {
            return Encode().Replace(Sep, ";");
        }
        #endregion
    }
}
