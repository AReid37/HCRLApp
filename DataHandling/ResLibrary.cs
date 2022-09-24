using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Short-hand for "ResourceLibrary".</summary>
    public sealed class ResLibrary : DataHandlerBase
    {
        /*** RESOURCE LIBRARY
        Data form containing all information regarding resource contents, legends, and summaries from version logs
        
        Fields / Props
            - ResLib previousSelf
            - List<ResCon> contents (prv set; get;)
            - List<LegDt> legendData (prv set; get;)
            - List<SmryD> summaryData (prv set; get;)   
            - ResCon this[int] (get -> contents[])      Necessary??
            - ResCon this[str] (get -> contents[])      Necessary??

        Constructors
            - ResLib()

        Methods
            - bl AddContent(params ResCon[] newContents)
            - bl RemoveContent(ResCon content)
            - bl RemoveContent(int shelfID)
            - bl AddLegend(LegDt legDat)
            - bl AddSummary(SmryD summary)
            - vd Integrate(ResLib other)
            - ovr EncodeToSharedFile(...)
            - ovr DecodeFromSharedFile(...)            
            - ovr ChangesMade()            
        ***/

        #region fields / props
        // private
        public const string LooseResConName = "!LooseContent!";
        ResLibrary _previousSelf;
        List<ResContents> _contents;
        List<LegendData> _legends;
        List<SummaryData> _summaries;

        // public
        public List<ResContents> Contents
        {
            private set => _contents = value;
            get => _contents;
        }
        public List<LegendData> Legends
        {
            private set => _legends = value;
            get => _legends;
        }
        public List<SummaryData> Summaries
        {
            private set => _summaries = value;
            get => _summaries;
        }
        #endregion

        public ResLibrary()
        {
            _previousSelf = (ResLibrary)this.MemberwiseClone();
        }


        #region methods
        public bool AddContent(bool keepLooseRCQ, params ResContents[] newContents)
        {
            bool addedContentsQ = false;
            if (newContents.HasElements())
            {
                Dbug.StartLogging("ResLibrary.AddContent(prms RC[])");
                Dbug.LogPart($"Recieved [{newContents.Length}] new ResCons for library");

                List<int> shelfNums = new();
                if (Contents.HasElements())
                {
                    Dbug.LogPart("; Fetching existing shelf numbers ::");
                    foreach (ResContents resCon in Contents)
                    {
                        shelfNums.Add(resCon.ShelfID);
                        Dbug.LogPart($" {resCon.ShelfID}");
                    }
                }
                else
                {
                    Dbug.LogPart("; No pre-existing contents in library");
                    Contents = new List<ResContents>();
                }
                Dbug.Log("; ");

                Dbug.Log("Proceeding to add new ResCons to library; ");
                Dbug.NudgeIndent(true);
                foreach (ResContents newRC in newContents)
                {
                    if (newRC != null)
                    {
                        // find connections for ConAdts and ConChgs
                        if (newRC.ContentName == LooseResConName)
                        {
                            Dbug.LogPart("Identified 'loose' ResCon; ");
                            if (Contents.HasElements() && !keepLooseRCQ)
                            {
                                Dbug.Log("Library has pre-existing contents that may serve as connections; ");

                                /// find matching and connect ConAddits
                                List<ResContents> RCextras = new();
                                if (newRC.ConAddits.HasElements())
                                {
                                    Dbug.Log("Making connections :: ConAddits to ConBase");
                                    Dbug.NudgeIndent(true);
                                    foreach (ContentAdditionals looseCa in newRC.ConAddits)
                                    {
                                        if (looseCa.IsSetup())
                                        {
                                            Dbug.LogPart($"Connecting ConAddits ({looseCa}) >> ");
                                            ResContents matchingResCon = null;
                                            foreach (ResContents resCon in Contents)
                                            {
                                                if (resCon.ContainsDataID(looseCa.RelatedDataID, out RCFetchSource source))
                                                    if (source == RCFetchSource.ConBaseGroup)
                                                    {
                                                        matchingResCon = resCon;
                                                        break;
                                                    }
                                            }

                                            if (matchingResCon != null)
                                            {
                                                Dbug.LogPart($"Found ResCon {{{matchingResCon}}} >> ");
                                                matchingResCon.StoreConAdditional(looseCa);
                                                RCextras.Add(matchingResCon);
                                                Dbug.LogPart($"Connected with ConBase ({matchingResCon.ConBase}) [by ID '{looseCa.RelatedDataID}']");
                                            }
                                            else Dbug.LogPart("No ConBase connections found");
                                        }
                                        Dbug.Log("; ");
                                    }
                                    Dbug.NudgeIndent(false);

                                    if (RCextras.HasElements())
                                        foreach(ResContents rc in RCextras)
                                            Dbug.Log($"Edited RC :: {rc}");
                                }

                                /// find matching and connect ConChanges
                                RCextras.Clear();
                                if (newRC.ConChanges.HasElements())
                                {
                                    Dbug.Log("Making connections :: ConChanges to ConBase/ConAddits");
                                    Dbug.NudgeIndent(true);
                                    foreach (ContentChanges looseCc in newRC.ConChanges)
                                    {
                                        if (looseCc.IsSetup())
                                        {
                                            Dbug.LogPart($"Connecting ConChanges ({looseCc.ToStringShortened()}) >> ");
                                            ResContents matchingResCon = null;
                                            foreach (ResContents resCon in Contents)
                                            {
                                                if (resCon.ContainsDataID(looseCc.RelatedDataID, out _))
                                                {
                                                    matchingResCon = resCon;
                                                    break;
                                                }
                                            }

                                            if (matchingResCon != null)
                                            {
                                                Dbug.LogPart($"Found ResCon {{{matchingResCon}}} >> ");
                                                if (matchingResCon.ContainsDataID(looseCc.RelatedDataID, out RCFetchSource source, out DataHandlerBase dataSource))
                                                {
                                                    if (source == RCFetchSource.ConBaseGroup)
                                                    {
                                                        ContentBaseGroup matchCbg = (ContentBaseGroup)dataSource;
                                                        if (matchCbg != null)
                                                        {
                                                            matchingResCon.StoreConChanges(looseCc);
                                                            RCextras.Add(matchingResCon);
                                                            Dbug.LogPart($"Connected with ConBase ({matchCbg}) [by ID '{looseCc.RelatedDataID}']");
                                                        }
                                                    }
                                                    else if (source == RCFetchSource.ConAdditionals)
                                                    {
                                                        ContentAdditionals matchCa = (ContentAdditionals)dataSource;
                                                        if (matchCa != null)
                                                        {
                                                            matchingResCon.StoreConChanges(looseCc);
                                                            RCextras.Add(matchingResCon);
                                                            Dbug.LogPart($"Connected with ConAddits ({matchCa}) [by ID '{looseCc.RelatedDataID}']");
                                                        }
                                                    }
                                                }
                                            }
                                            else Dbug.LogPart("No connections found");
                                        }
                                        Dbug.Log("; ");
                                    }
                                    Dbug.NudgeIndent(false);

                                    if (RCextras.HasElements())
                                        foreach (ResContents rc in RCextras)
                                            Dbug.Log($"Edited RC :: {rc}");
                                }
                            }
                            else
                            {
                                if (!keepLooseRCQ)
                                    Dbug.Log("No pre-existing library contents to search for connections; ");
                                else
                                {
                                    Dbug.Log("Unallowed from sorting loose ResCon; ");
                                    newRC.ShelfID = GetNewShelfNum();
                                    shelfNums.Add(newRC.ShelfID);

                                    Contents.Add(newRC);
                                    Dbug.Log($"*Added* :: {newRC}; ");
                                }
                            }
                            //Dbug.Log("; ");
                        }

                        // just add
                        else
                        {
                            /// get shelf id
                            newRC.ShelfID = GetNewShelfNum();
                            shelfNums.Add(newRC.ShelfID);

                            /// add to content library
                            Contents.Add(newRC);
                            Dbug.Log($"Added :: {newRC}");
                        }

                        addedContentsQ = true;
                    }
                }
                Dbug.NudgeIndent(false);
                Dbug.EndLogging();

                // static method
                int GetNewShelfNum()
                {
                    int newShelfNum = -1;
                    bool gotNewNum = false;
                    for (int shx = 0; !gotNewNum; shx++)
                    {
                        if (!shelfNums.Contains(shx))
                        {
                            newShelfNum = shx;
                            gotNewNum = true;
                        }
                    }
                    return newShelfNum;
                }
            }
            return addedContentsQ;
        }
        public bool AddContent(params ResContents[] newContents)
        {
            return AddContent(false, newContents);
        }

        // bool RemoveContent(ResCon rcToRemove)
        // bool RemoveContent(int shelfID)
        public bool AddLegend(LegendData[] newLegends)
        {
            bool addedLegendQ = false;
            if (newLegends.HasElements())
            {
                foreach (LegendData leg in newLegends)
                {
                    if (leg.IsSetup())
                    {
                        bool isDupe = false;
                        LegendData dupedLegData = null;
                        if (Legends.HasElements())
                        {
                            foreach (LegendData ogLeg in Legends)
                                if (ogLeg.Key == leg.Key)
                                {
                                    isDupe = true;
                                    dupedLegData = ogLeg;
                                    break;
                                }
                        }
                        else Legends = new List<LegendData>();

                        if (!isDupe)
                            Legends.Add(leg);
                        else
                            dupedLegData.AddKeyDefinition(leg[0]);
                        addedLegendQ = true;
                    }
                }
            }
            return addedLegendQ;
        }
        public bool AddSummary(SummaryData newSummary)
        {
            bool addedSummaryQ = false;
            if (newSummary != null)
            {
                if (newSummary.IsSetup())
                {
                    bool isDupe = false;
                    if (Summaries.HasElements())
                    {
                        foreach (SummaryData sumDat in Summaries)
                            if (sumDat.Equals(newSummary))
                            {
                                isDupe = true;
                                break;
                            }
                    }
                    else Summaries = new List<SummaryData>();

                    if (!isDupe)
                    {
                        addedSummaryQ = true;
                        Summaries.Add(newSummary);
                    }
                }
            }
            return addedSummaryQ;
        }
        // void Integrate(ResLibrary other)
        #endregion
    }
}
