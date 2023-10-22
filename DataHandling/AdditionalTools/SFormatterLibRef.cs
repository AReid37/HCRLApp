using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>An instance responsible for handling version log reference information for steam formatter.</summary>
    public struct SFormatterLibRef
    {
        /** FORMATTING PLANNING : Library Syntax snippet
            > Specific to LIBRARY
                Provided by SFormatterLibRef.cs. Handled by SFormatterHandler.cs
                - Sources information from resource library via targetted "Log Version"

                syntax              outcome
                _________________________________
                {Version}           single; fetches version numbers only (ex 1.00)
                {AddedCount}        single; fetches number of added items
                {Added:#,prop}      array; access property from zero-based Added entry '#'. Properties: name, ids
                {AdditCount}        single; fetches number of additional items
                {Addit:#,prop}      array; access property from zero-bassed Additional entry '#'. Properties: ids, optionalName, relatedID, relatedContent
                {TTA}               single; fetches the number of total textures/contents added
                {UpdatedCount}      single; fetches the number of updated items
                {Updated:#,prop}    array; access property from zero-based Updated entry '#'. Properties: id, name, changeDesc
                {LegendCount}       single; fetches the number of legends used in version log
                {Legend:#,prop}     array; access property from zero-based Legend entry '#'. Properties; key, definition, keyNum
                {SummaryCount}      single; fetches the number of summaries in given version log
                {Summary:#}         array; access summary part from zero-based Summary entry '#' 
         ****/

        #region fields/props
        // FIELDS
        /// <summary>A value that substitutes any ver log data that is null or empty.</summary>
        public const string ValIsNE = "/?";
        const string PropSep = DataHandlerBase.Sep;
        readonly string _version, _tta;
        readonly int _countAdded, _countAddit, _countUpdated, _countLegend, _countSummary;
        readonly List<string> _added, _addit, _updated, _legend, _summary;
        readonly bool _isSetupQ;


        // PROPS
        public string Version { get => _version; }
        public string TTA { get => _tta; }
        /// counts
        public string AddedCount { get => _countAdded.ToString(); }
        public string AdditCount { get => _countAddit.ToString(); }
        public string UpdatedCount { get => _countUpdated.ToString(); }
        public string LegendCount { get => _countLegend.ToString(); }
        public string SummaryCount { get => _countSummary.ToString(); }
        #endregion


        // CONSTRUCTORS
        public SFormatterLibRef(ResLibrary verLogDetails)
        {
            
            Dbug.StartLogging("SFormatterLibRef()");
            // initialize
            _isSetupQ = false;
            _version = null;
            _tta = null;
            _countAdded = _countAddit = _countUpdated = _countLegend = _countSummary = 0;
            _added = new List<string>();
            _addit = new List<string>();
            _updated = new List<string>();
            _legend = new List<string>();
            _summary = new List<string>();
            Dbug.LogPart("Initialized instance values; ");

            // packaging
            if (verLogDetails != null)
            {
                if (verLogDetails.IsSetup())
                {
                    Dbug.Log($"Recieved version log details; Proceeding to packaging; ");
                    Dbug.NudgeIndent(true);

                    /// version
                    if (verLogDetails.GetVersionRange(out VerNum lowVer, out VerNum highVer))
                    {
                        if (lowVer.Equals(highVer))
                        {
                            _version = lowVer.ToStringNums();
                            Dbug.Log($"Assigned value to 'Version' :: {_version}; ");
                        }
                    }

                    /// added, additional, updated
                    if (verLogDetails.Contents.HasElements())
                    { // wrapping
                        Dbug.Log($"Setting values for 'Added', 'Additional' and 'Updated' collections [ids uncondensed]; ");
                        Dbug.NudgeIndent(true);
                        foreach (ResContents resCon in verLogDetails.Contents)
                        {
                            /// Added Props -- ids, name
                            /// Addit Props -- ids, optname, relID, relName
                            /// Updat Props -- relID, name, changeDesc

                            bool isLooseQ = resCon.ContentName == ResLibrary.LooseResConName;
                            if (!isLooseQ)
                            {
                                string addedValue = $"{resCon.ConBase.DataIDString}{PropSep}{resCon.ContentName}";
                                //string addedValue = $"{Extensions.CreateNumericDataIDRanges(resCon.ConBase.DataIDString.Split(' '))}";
                                //addedValue += $"{PropSep}{resCon.ContentName}";
                                _added.Add(addedValue);
                                Dbug.Log($"New entry to 'Added' :: {addedValue}   [ids/name]");
                            }

                            if (resCon.ConAddits.HasElements())
                                foreach (ContentAdditionals rca in resCon.ConAddits)
                                {
                                    string additValue = $"{rca.DataIDString}{PropSep}{rca.OptionalName}{PropSep}{rca.RelatedDataID}{PropSep}";
                                    //string additValue = $"{Extensions.CreateNumericDataIDRanges(rca.DataIDString.Split(' '))}";
                                    //additValue += $"{PropSep}{rca.OptionalName}{PropSep}{rca.RelatedDataID}{PropSep}";
                                    additValue += isLooseQ ? rca.ContentName : resCon.ContentName;
                                    _addit.Add(additValue);
                                    Dbug.Log($"New entry to 'Additional' :: {additValue}   [ids/optName/relid/relName]");
                                }

                            if (resCon.ConChanges.HasElements())
                                foreach (ContentChanges rcc in resCon.ConChanges)
                                {
                                    string contentName = resCon.ContentName == ResLibrary.LooseResConName || rcc.InternalName.IsNotNEW() ? rcc.InternalName : resCon.ContentName;
                                    string updtValue = $"{rcc.RelatedDataID}{PropSep}{contentName}{PropSep}{rcc.ChangeDesc}";
                                    _updated.Add(updtValue);
                                    Dbug.Log($"New entry to 'Updated' :: {updtValue}   [relID/relName/changeDesc]");
                                }
                        }
                        Dbug.NudgeIndent(false);

                        _countAdded = _added.Count;
                        _countAddit = _addit.Count;
                        _countUpdated = _updated.Count;
                        Dbug.Log($"Assigned values to :: 'AddedCount' [{_countAdded}]  --  'AdditCount' [{_countAddit}]  --  'UpdatedCount' [{_countUpdated}]; ");
                    }

                    /// legend
                    if (verLogDetails.Legends.HasElements())
                    {
                        /// Legen Props -- key, definition, keyNum
                        Dbug.Log($"Setting values for 'Legend' collection; ");
                        Dbug.NudgeIndent(true);
                        foreach (LegendData legData in verLogDetails.Legends)
                        {
                            string legValue = $"{legData.Key}{PropSep}{legData[0]}";
                            _legend.Add(legValue);
                            Dbug.Log($"New entry :: {legValue}   [key/definition]");
                        }
                        Dbug.NudgeIndent(false);

                        _countLegend = _legend.Count;
                        Dbug.Log($"Assigned value to 'LegendCount' :: {_countLegend}; ");
                    }

                    /// tta, summary
                    if (verLogDetails.Summaries[0].IsSetup())
                    {
                        /// Summa Props -- sumPart
                        SummaryData sumData = verLogDetails.Summaries[0];
                        
                        _tta = sumData.TTANum.ToString();
                        Dbug.Log($"Assigned value to 'TTA' :: {_tta}; ");

                        Dbug.Log($"Setting values for 'Summary' collection; ");
                        Dbug.NudgeIndent(true);
                        foreach (string sumPart in sumData.SummaryParts)
                        {
                            _summary.Add(sumPart);
                            Dbug.Log($"New entry :: {sumPart}   [sumPart]");
                        }
                        Dbug.NudgeIndent(false);

                        _countSummary = _summary.Count;
                        Dbug.Log($"Assigned value to 'SummaryCount' :: {_countSummary}; ");
                    }
                   
                    Dbug.NudgeIndent(false);
                    _isSetupQ = true;
                }
                else Dbug.Log($"Version log details were incomplete (!IsSetup()); ");
            }
            else Dbug.Log("No version log details were recieved (null); ");
            Dbug.EndLogging();
        }

        // METHODS
        /// <summary></summary>
        /// <param name="section">Specifies a collection based on sections as determined by log decoder sectioning.</param>
        /// <param name="entryNum">The one-based entry number of the collection to retrieve a property from. This value is clamped within collection's range.</param>
        /// <param name="prop">The property to retrieve from a collection at <paramref name="entryNum"/></param>
        /// <returns>A string value of the property from given an <paramref name="entryNum"/> in a given <paramref name="section"/>.</returns>
        public string GetPropertyValue(DecodedSection section, int entryNum, LibRefProp prop)
        {
            /// Added Props -- ids, name
            /// Addit Props -- ids, optname, relID, relName
            /// Updat Props -- relID, name, changeDesc
            /// Legen Props -- key, definition, [removed: keyNum]
            /// Summa Props -- sumPart

            const string div = "|";
            const int entryBaseNum = 1;
            string dbgStr = $"Fetching property '{prop}' at entry '#{entryNum}' from '{section}' (entry number is clamped) // ";
            string propValue = null;
            switch (section)
            {
                case DecodedSection.Added:
                    if (_added.HasElements())
                    {
                        entryNum = entryNum.Clamp(entryBaseNum, _countAdded) - entryBaseNum;
                        dbgStr += $"Added @{entryNum} = {_added[entryNum].Replace(PropSep, div)}   [ids/name]";

                        string[] addedProps = _added[entryNum].Split(PropSep);
                        if (addedProps.HasElements(2))
                        {
                            if (prop == LibRefProp.Ids)
                                propValue = addedProps[0];
                            else if (prop == LibRefProp.Name)
                                propValue = addedProps[1];
                        }
                    }
                    else dbgStr += $"No data in '{section}'";
                    break;

                case DecodedSection.Additional:
                    if (_addit.HasElements())
                    {
                        entryNum = entryNum.Clamp(entryBaseNum, _countAddit) - entryBaseNum;
                        dbgStr += $"Additional @{entryNum} = {_addit[entryNum].Replace(PropSep, div)}   [ids/optName/relId/relName]";

                        string[] additProps = _addit[entryNum].Split(PropSep);
                        if (additProps.HasElements(4))
                        {
                            if (prop == LibRefProp.Ids)
                                propValue = additProps[0];
                            else if (prop == LibRefProp.Name)
                                propValue = additProps[1];
                            else if (prop == LibRefProp.RelatedID)
                                propValue = additProps[2];
                            else if (prop == LibRefProp.RelatedName)
                                propValue = additProps[3];
                        }
                    }
                    else dbgStr += $"No data in '{section}'";
                    break;

                case DecodedSection.Updated:
                    if (_updated.HasElements())
                    {
                        entryNum = entryNum.Clamp(entryBaseNum, _countUpdated) - entryBaseNum;
                        dbgStr += $"Updated @{entryNum} = {_updated[entryNum].Replace(PropSep, div)}   [relid/relName/changeDesc]";
                        
                        string[] updtProps = _updated[entryNum].Split(PropSep);
                        if (updtProps.HasElements(3))
                        {
                            if (prop == LibRefProp.RelatedID)
                                propValue = updtProps[0];
                            else if (prop == LibRefProp.RelatedName)
                                propValue = updtProps[1];
                            else if (prop == LibRefProp.ChangeDesc)
                                propValue = updtProps[2];
                        }
                    }
                    else dbgStr += $"No data in '{section}'";
                    break;

                case DecodedSection.Legend:
                    if (_legend.HasElements())
                    {
                        entryNum = entryNum.Clamp(entryBaseNum, _countLegend) - entryBaseNum;
                        dbgStr += $"Legend @{entryNum} = {_legend[entryNum].Replace(PropSep, div)}   [key/definition/keyNum]";

                        string[] legProps = _legend[entryNum].Split(PropSep);
                        if (legProps.HasElements(2))
                        {
                            if (prop == LibRefProp.Key)
                                propValue = legProps[0];
                            else if (prop == LibRefProp.Definition)
                                propValue = legProps[1];
                        }
                    }
                    else dbgStr += $"No data in '{section}'";
                    break;

                case DecodedSection.Summary:
                    if (_summary.HasElements())
                    {
                        entryNum = entryNum.Clamp(entryBaseNum, _countSummary) - entryBaseNum;
                        dbgStr += $"Legend @{entryNum} = {_summary[entryNum].Replace(PropSep, div)}   [sumPart]";

                        if (prop == LibRefProp.SummaryPart)
                            propValue = _summary[entryNum];
                    }
                    else dbgStr += $"No data in '{section}'";
                    break;

                default:
                    break;
            }

            dbgStr += $";  --> Returned value :: ";
            dbgStr += (propValue.IsNE() ? (propValue == null ? "<null>" : "<empty>") : propValue) + "; ";
            //Dbug.SingleLog("SFormatterLibRef.GetPropertyValue()", dbgStr);

            if (propValue.IsNE())
                propValue = ValIsNE;
            return propValue;
        }
        /// <returns>A boolean value determining whether this instance has been provided with version log information.</returns>
        public bool IsSetup()
        {
            return _isSetupQ;
        }
    }

    /// <summary>Properties for Library References (Steam Formatter Usage).</summary>
    public enum LibRefProp
    {
        /// <summary>Applies to: <see cref="DecodedSection.Added"/>, <see cref="DecodedSection.Additional"/>; Fetches content's data IDs.</summary>
        Ids,
        /// <summary>Applies to: <see cref="DecodedSection.Added"/>, <see cref="DecodedSection.Additional"/>; Fetches content name (optional content name for <see cref="DecodedSection.Additional"/>).</summary>
        Name,
        /// <summary>Applies to: <see cref="DecodedSection.Additional"/>, <see cref="DecodedSection.Updated"/>; Fetches related content ID.</summary>
        RelatedID,
        /// <summary>Applies to: <see cref="DecodedSection.Additional"/>, <see cref="DecodedSection.Updated"/>; Fetches related content name.</summary>
        RelatedName,
        /// <summary>Applies only to <see cref="DecodedSection.Updated"/>; Fetches change description.</summary>
        ChangeDesc,
        /// <summary>Applies only to <see cref="DecodedSection.Legend"/>; Fetches legend key.</summary>
        Key,
        /// <summary>Applies only to <see cref="DecodedSection.Legend"/>; Fetches legend key definition.</summary>
        Definition,
        /// <summary>Applies only to <see cref="DecodedSection.Summary"/>; Fetches summary parts.</summary>
        SummaryPart
    }
}
