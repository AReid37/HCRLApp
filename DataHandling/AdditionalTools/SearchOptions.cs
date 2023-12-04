using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public struct SearchOptions
    {
        /** ORIGINAL PLAN 
             > Search filter options and setup
                - Available filter options
                    . Case sensitivity (on/off) [default:On]
                    . Source category searches (on/off) [default:On (for all)]
                        . Categories: Content Name (Nam), Related Data IDs (RDI), Additional Related Content (ARC), Updated info (Upd), Version Number (Ver)
                    . Ignore Relevance, order by shelf ID instead (on/off) [default:Off]
                    . Reset filter options (single command)
                - Implementations: A struct
                - Things that may changes
                            . Categories reduced (-2): Base Content (Bse), Additional Related Content (ARC), Updated info (Upd)
                                . 'Base Content' adopts 'Content Name' and 'Related Data IDs' into a group
                                . 'Version Number' is discarded
        
            Fields / Props
            - bl caseSensitiveQ
            - bl ignoreRelevanceQ
            - SC[] sourceCategoriesInUse
            - pv bl isSetupQ

            Constructors
            - SO (bl isCaseSensitive, bl isIgnoringRelevance, prms SC[] sourcesUsed)

            Method
            - bl IsSetup()
            - bl Equals()         
            - bl IsUsingSource(SC source)
            - bl ToggleSource(SC source)
            - str ToStringVar()
         */

        // FIELDS / PROPS
        private bool isSetupQ;
        public bool caseSensitiveQ;
        public bool ignoreRelevanceQ;
        public SourceContents sourceContent;
        public SourceCategory[] sourcesUsed;


        // CONSTRUTOR
        /// <param name="sourcesCategoryUsed">If left empty, all source categories will be applied.</param>
        public SearchOptions(bool isCaseSensitive, bool isIgnoringRelevance, SourceContents resultContents, params SourceCategory[] sourcesCategoryUsed)
        {
            isSetupQ = true;
            caseSensitiveQ = isCaseSensitive;
            ignoreRelevanceQ = isIgnoringRelevance;
            sourceContent = resultContents;
            sourcesUsed = new SourceCategory[3]
            {
                SourceCategory.Bse,
                SourceCategory.Adt,
                SourceCategory.Upd,
            };

            /// IF given at least one source used: use only those sources; ELSE use all sources
            if (sourcesCategoryUsed.HasElements())
            {
                List<SourceCategory> sources = new();
                foreach (SourceCategory sCat in sourcesCategoryUsed)
                {
                    if (!sources.Contains(sCat))
                        sources.Add(sCat);
                }

                if (sources.HasElements())
                    sourcesUsed = sources.ToArray();
            }
        }


        // METHODS
        /// <summary>Is the Search Options struct setup?</summary>
        /// <returns>A booleans stating whether this SearchOptions instance was given values and at least one Source Category is being used.</returns>
        public readonly bool IsSetup()
        {
            return isSetupQ && sourcesUsed.HasElements();
        }
        /// <summary>Compares two instances for similarities against: setup state, case sensitivity state, ignore relevance state, sources used.</summary>
        public bool Equals(SearchOptions so)
        {
            bool areEquals = IsSetup() == so.IsSetup();
            if (areEquals)
            {
                for (int x = 0; x < 4 && areEquals; x++)
                {
                    switch (x)
                    {
                        case 0:
                            areEquals = caseSensitiveQ == so.caseSensitiveQ;
                            break;

                        case 1:
                            areEquals = ignoreRelevanceQ == so.ignoreRelevanceQ;
                            break;

                        case 2:
                            areEquals = sourceContent == so.sourceContent;
                            break;

                        case 3:
                            areEquals = sourcesUsed.HasElements() == so.sourcesUsed.HasElements();
                            if (areEquals && sourcesUsed.HasElements())
                            {
                                areEquals = sourcesUsed.Length == so.sourcesUsed.Length;
                                for (int sx = 0; sx < sourcesUsed.Length && areEquals; sx++)
                                    areEquals = IsUsingSource(sourcesUsed[sx]) == so.IsUsingSource(sourcesUsed[sx]);
                            }
                            break;
                    }
                }
            }
            return areEquals;
        }
        /// <summary>Checks for the usage of a <paramref name="source"/> within this SearchOptions instance.</summary>
        /// <returns>A boolean verifying the usage of <paramref name="source"/>.</returns>
        public bool IsUsingSource(SourceCategory source)
        {
            bool isUsingSourceQ = false;
            if (IsSetup())
            {
                for (int sx = 0; sx < sourcesUsed.Length && !isUsingSourceQ; sx++)
                {
                    SourceCategory sourceUsed = sourcesUsed[sx];
                    isUsingSourceQ = sx switch
                    {
                        0 => source == sourceUsed,
                        1 => source == sourceUsed,
                        2 => source == sourceUsed,
                        _ => false
                    };
                }
            }
            return isUsingSourceQ;
        }
        /// <summary>Toggles usage state of a source category. If no categories are used as a result of this function, all sources will be re-enabled.</summary>
        /// <returns>A boolean stating whether the toggled source is accepted and completed (<c>false</c>, when all sources disabled).</returns>
        public bool ToggleSource(SourceCategory source)
        {
            bool toggledSourcesQ = false;
            if (IsSetup())
            {
                List<SourceCategory> newSources = new();
                if (!IsUsingSource(source))
                {
                    newSources.Add(source);
                    newSources.AddRange(sourcesUsed);
                }
                else
                {
                    foreach (SourceCategory sCat in sourcesUsed)
                        if (sCat != source)
                            newSources.Add(sCat);
                }

                if (newSources.HasElements())
                {
                    toggledSourcesQ = true;
                    sourcesUsed = newSources.ToArray();
                }
                else
                {
                    sourcesUsed = new SourceCategory[3]
                    {
                        SourceCategory.Bse,
                        SourceCategory.Adt,
                        SourceCategory.Upd,
                    };
                }
            }
            return toggledSourcesQ;
        }
        /// <summary>Toggles <see cref="sourceContent"/>'s value through the source content options that can be retrieved. </summary>
        /// <remarks>Refer to the values of <see cref="SourceContents"/>.</remarks>
        public void ToggleContent()
        {
            if (IsSetup())
            {
                Array scEnum = Enum.GetValues(typeof(SourceContents));
                sourceContent = (SourceContents)((sourceContent.GetHashCode() + 1) % scEnum.Length);
            }
        }

        public override string ToString()
        {
            string toStrV = "SO::";
            if (isSetupQ)
            {
                toStrV += caseSensitiveQ ? "CasSen " : "";
                toStrV += ignoreRelevanceQ ? "IgnRel " : "";
                toStrV += $"{sourceContent} ";
                toStrV += IsUsingSource(SourceCategory.Bse) ? "SrcBse " : "";
                toStrV += IsUsingSource(SourceCategory.Adt) ? "SrcAdt " : "";
                toStrV += IsUsingSource(SourceCategory.Upd) ? "SrcUpd " : "";

                toStrV = toStrV.Trim().Replace(" ", ", ");
            }
            else toStrV += "?";
            return toStrV;
        }
    }
}
