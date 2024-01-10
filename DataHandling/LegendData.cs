using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public class LegendData : DataHandlerBase
    {
        /*** LEGEND DATA
        Data form for legend keys and definitions
        Syntax: {key}*{keynames}***
        New Syntax: {key}*{verIntro}*{keynames}

        FROM DESIGN DOC
        ..........
        > {key}
		    [REQUIRED] string value
		    May not contain '*' symbol or comma (',')
	    > {keynames}
		    [REQUIRED] seperator-separated string values
		    May not contain '*' symbol
	    NOTES [Rejected]
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

        /// -----
        #region fields / props
        // private
        VerNum _versionIntroduced, _prevVersionIntroduced;
        string _key, _prevKey;
        List<string> _definitions, _prevDefinitions;
        int index = ResContents.NoShelfNum;

        // public        
        public VerNum VersionIntroduced
        {
            get => _versionIntroduced;
            private set
            {
                if (value.HasValue())
                    _versionIntroduced = value;
            }
        }
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
        /// <summary>Returns the legend key's definitions as a comma-separated bundle.</summary>
        public string DefinitionsString
        {
            get
            {
                string defs = "";
                if (_definitions.HasElements())
                    for (int dx = 0; dx < _definitions.Count; dx++)
                    {
                        defs += $"{_definitions[dx]}{(dx + 1 < _definitions.Count ? ", " : "")}";
                    }
                defs = defs.Trim();
                return defs;
            }
        }
        /// <summary>The index number of this instance within a <see cref="ResLibrary.Legends"/>. Default value of <see cref="ResContents.NoShelfNum"/>.</summary>
        public int Index { get => index; }
        #endregion

        public LegendData() { }
        public LegendData(string legKey)
        {
            Key = legKey;
        }
        public LegendData(string legKey, VerNum verIntroduced, params string[] definitions)
        {
            Key = legKey;
            VersionIntroduced = verIntroduced;
            if (definitions.HasElements())
            {
                /// this process should be as through as 'AddKeyDefinition(str)' ... not a push over definition entry 
                _definitions = new List<string>();
                foreach (string def in definitions)
                {
                    if (def.IsNotNEW())
                    {
                        bool addNewDef = true;
                        if (_definitions.HasElements())
                            foreach (string addedDef in _definitions)
                            {
                                if (addedDef.ToLower() == def.Trim().ToLower())
                                {
                                    addNewDef = false;
                                    break;
                                }
                            }
                        if (addNewDef)
                            _definitions.Add(def.Trim());
                    }   
                }
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
                        if (def.ToLower() == newDefinition.ToLower())
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
                    _definitions = new();
                    _definitions.Add(newDefinition);
                    addedNewDef = true;
                }
            }
            return addedNewDef;
        }
        public string Encode()
        {
            // Syntax: {key}*{keynames}***
            // New Syntax: {key}*{verIntro}*{keynames}
            // > {keynames}
            //      [REQUIRED] seperator - separated string values
            string fullEncode = "";
            if (IsSetup())
            {
                fullEncode = $"{Key}{Sep}{VersionIntroduced}{Sep}";
                for (int dix = 0; dix < _definitions.Count; dix++)
                    fullEncode += $"{this[dix]}{(dix + 1 >= _definitions.Count ? "" : Sep)}";

                SetPreviousSelf();
            }
            return fullEncode;
        }
        public bool Decode(string legInfo)
        {
            /**
             Syntax: {key}*{keynames}***
             New Syntax: {key}*{verIntro}*{keynames}

            FROM DESIGN DOC
            ..........
            > {key}
		        [REQUIRED] string value
		        May not contain '*' symbol or comma (',')
            > {verIntro}
                The version in which this legend was introduced
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
                                /// version introduced
                                else if (lix == 1)
                                {
                                    if (VerNum.TryParse(legParts[lix], out VerNum verIntro))
                                        VersionIntroduced = verIntro;
                                }
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
            _prevVersionIntroduced = _versionIntroduced;
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
            return new LegendData(_prevKey, _prevVersionIntroduced, prevDefs);
        }
        /// <summary>Compares two instances for similarities against: Setup state, Key, Definitions.</summary>
        /// <param name="compareVerNumQ">If <c>true</c>, will include the values of <see cref="VersionIntroduced"/> in equality comparison.</param>
        public bool Equals(LegendData legDat, bool compareVerNumQ = false)
        {
            bool areEquals = false;
            if (legDat != null)
            {
                areEquals = true;
                int cmpPlus = compareVerNumQ ? 1 : 0;
                for (int ldx = 0; ldx < 3 + cmpPlus && areEquals; ldx++)
                {
                    switch (ldx)
                    {
                        case 0:
                            areEquals = IsSetup() == legDat.IsSetup();
                            break;

                        case 1:
                            areEquals = Key == legDat.Key;
                            break;

                        case 2:
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

                        case 3:
                            areEquals = _versionIntroduced.Equals(legDat._versionIntroduced);
                            break;
                    }
                }
            }
            return areEquals;
        }
        /// <summary>Has this instance of <see cref="LegendData"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the legend key, version introduced, and definitions have been given values, at minimum.</returns>
        public override bool IsSetup()
        {
            return _key.IsNotNEW() && _definitions.HasElements();
        }
        public LegendData CloneLegend()
        {
            LegendData clone = null;
            if (IsSetup())
            {
                clone = new LegendData(Key, VersionIntroduced, _definitions.ToArray());
                clone.AdoptIndex(index);
            }
            return clone;
        }
        public void Overwrite(LegendData legNew, out ResLibOverwriteInfo info)
        {
            info = new ResLibOverwriteInfo();
            if (IsSetup() && legNew != null)
            {
                /// considerations
                ///		- the overwriting info must have the same key as existing
                ///		    - if same key 'and' same version or before, the existing key's first definition is replaced with the overwriting's first definition 
                ///		    - if same key 'and' version after, the overwriting's definition is added to existing's definitions list
                ///		    - if not same key, the overwriting definition is ignored; the existing remains unedited

                info = new ResLibOverwriteInfo(ToString(), legNew.ToString(), SourceOverwrite.Legend);
                info.SetOverwriteStatus(false);
                if (legNew.IsSetup() && !Equals(legNew, true))
                {
                    if (Key == legNew.Key)
                    {
                        if (VersionIntroduced.AsNumber >= legNew.VersionIntroduced.AsNumber)
                        {
                            /// This might have seemed easy, but after breaking it down, it's got a couple steps
                            bool dupeDefQ = false;
                            int dupeDefIx = 0;
                            for (int ldx = 0; ldx < _definitions.Count && !dupeDefQ; ldx++)
                            {
                                string def = _definitions[ldx];
                                if (legNew[0].ToLower() == def.ToLower())
                                {
                                    dupeDefQ = true;
                                    dupeDefIx = ldx;
                                }    
                            }

                            /// IF 1st existing definition (to lower) is not same as 1st overwriting definition (to lower): ..
                            ///     IF 1st overwriting definition is a duplicate of any existing definition 'and' at least two existing definitions: Shift 1st overwriting definition to index zero; 
                            ///     ELSE Set 1st existing definition to 1st overwriting definition
                            VersionIntroduced = legNew.VersionIntroduced;
                            if (_definitions[0].ToLower() != legNew[0].ToLower())
                            {
                                if (dupeDefQ && _definitions.HasElements(2))
                                {
                                    _definitions.RemoveAt(dupeDefIx);
                                    _definitions.Insert(0, legNew[0]);
                                }
                                else _definitions[0] = legNew[0];                                
                            }
                            info.SetOverwriteStatus();
                        }
                        else
                        {
                            bool addNewDefQ = AddKeyDefinition(legNew[0]);
                            info.SetOverwriteStatus(addNewDefQ);
                        }
                        info.SetResult(ToString());
                    }
                }
            }
        }
        /// <summary>Stores the index of which this instance exists within <see cref="ResLibrary.Legends"/>. Minimum value of <c>0</c>.</summary>
        public void AdoptIndex(int ix)
        {
            if (ix >= 0)
                index = ix;
        }


        public override string ToString()
        {
            return Encode().Replace(Sep, ";");
        }
        #endregion
    }
}
