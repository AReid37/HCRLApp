using System;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Short-hand for "ResourceLibrary".</summary>
    public sealed class ResLibrary : DataHandlerBase
    {
        /*** RESOURCE LIBRARY
         * Initial plan
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
        const string legDataTag = "lgd";
        const string sumDataTag = "sum";
        const string stampRLSavedData = "ResLibrary Has Saved Data", stampRLSavedDataTag = "RLDS"; 
        //static bool disableAddDbug = false;
        List<ResContents> _contents, _prevContents;
        List<LegendData> _legends, _prevLegends;
        List<SummaryData> _summaries, _prevSummaries;
        List<ResLibIntegrationInfo> rliInfoDock;
        List<ResLibAddInfo> rlaInfoDock;
        //List<ResLibOverwriteInfo> rloInfoDock;

        // public
        public const string LooseResConName = "!Loose%Content!";
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

        public ResLibrary() { }


        #region methods
        // CONTENT / LIBRARY INFORMATION HANDLING METHODS
        public bool AddContent(bool keepLooseRCQ, out int[] newShelfIDs, params ResContents[] newContents)
        {
            bool addedContentsQ = false;
            List<int> newShelfIdList = new();
            if (newContents.HasElements())
            {
                Dbg.StartLogging("ResLibrary.AddContent(prms RC[])", out int rlx);
                Dbg.ToggleThreadOutputOmission(rlx);
                Dbg.LogPart(rlx, $"Recieved [{newContents.Length}] new ResCons for library; Refresh integration info dock? {keepLooseRCQ}");

                rlaInfoDock = new List<ResLibAddInfo>();
                if (!keepLooseRCQ)
                    rliInfoDock = new List<ResLibIntegrationInfo>();

                List<int> shelfNums = new();
                if (Contents.HasElements())
                {
                    Dbg.LogPart(rlx, "; Fetching existing shelf numbers");
                    //Dbg.LogPart(rlx, "; Fetching existing shelf numbers ::");
                    foreach (ResContents resCon in Contents)
                    {
                        shelfNums.Add(resCon.ShelfID);
                        //Dbg.LogPart(rlx, $" {resCon.ShelfID}");
                    }
                }
                else
                {
                    Dbg.LogPart(rlx, "; No pre-existing contents in library");
                    Contents = new List<ResContents>();
                }
                Dbg.Log(rlx, "; ");

                Dbg.Log(rlx, "Proceeding to add new ResCons to library; ");
                Dbg.NudgeIndent(rlx, true);
                for (int x = 0; x < newContents.Length; x++)
                {
                    ResContents newRC = newContents[x].CloneResContent(true);
                    if (newRC != null)
                    {
                        // find connections for ConAdts and ConChgs
                        if (newRC.ContentName == LooseResConName)
                        {
                            Dbg.LogPart(rlx, "Identified 'loose' ResCon; ");

                            /// IF existing contents and able to sort loose contents: sort loose contents; 
                            if (Contents.HasElements() && !keepLooseRCQ)
                            {
                                Dbg.Log(rlx, "Library has pre-existing contents that may serve as connections; ");
                                ResLibIntegrationInfo rliInfo = new();
                                RCFetchSource rliType = RCFetchSource.None;

                                /// find matching and connect ConAddits
                                List<string> RCextras = new();
                                if (newRC.ConAddits.HasElements())
                                {
                                    rliType = RCFetchSource.ConAdditionals;
                                    Dbg.Log(rlx, "Making connections :: ConAddits to ConBase");
                                    Dbg.NudgeIndent(rlx, true);
                                    foreach (ContentAdditionals looseCa in newRC.ConAddits)
                                    {
                                        if (looseCa.IsSetup())
                                        {
                                            Dbg.LogPart(rlx, $"Connecting ConAddits ({looseCa}) >> ");
                                            ResContents matchingResCon = null;

                                            /// matching here
                                            LogDecoder.DisassembleDataID(looseCa.RelatedDataID, out string dk, out string db, out _);
                                            string trueRelDataID = dk + db;
                                            foreach (ResContents resCon in Contents)
                                            {
                                                if (resCon.ContainsDataID(looseCa.RelatedDataID, out RCFetchSource source))
                                                {
                                                    if (source == RCFetchSource.ConBaseGroup)
                                                    {
                                                        matchingResCon = resCon;
                                                        break;
                                                    }
                                                }
                                                else if (resCon.ContainsDataID(trueRelDataID, out source))
                                                {
                                                    if (source == RCFetchSource.ConBaseGroup)
                                                    {
                                                        matchingResCon = resCon;
                                                        Dbg.LogPart(rlx, "[by trueID] ");
                                                        break;
                                                    }
                                                }
                                            }

                                            /// connection here
                                            if (matchingResCon != null)
                                            {
                                                Dbg.LogPart(rlx, $"Found ResCon {{{matchingResCon}}} >> ");
                                                if (matchingResCon.StoreConAdditional(looseCa))
                                                {
                                                    RCextras.Add(matchingResCon.ToString());
                                                    Dbg.LogPart(rlx, $"Connected with ConBase ({matchingResCon.ConBase}) [by ID '{looseCa.RelatedDataID}']");

                                                    rliInfo = new ResLibIntegrationInfo(rliType, looseCa.DataIDString, looseCa.OptionalName, matchingResCon.ContentName, looseCa.RelatedDataID);
                                                }
                                                else Dbg.LogPart(rlx, $"Rejected ConAddits");
                                            }
                                            else Dbg.LogPart(rlx, "No ConBase connections found (discarded)");

                                            if (!rliInfo.IsSetup())
                                                rliInfo = new ResLibIntegrationInfo(rliType, looseCa.DataIDString, looseCa.OptionalName, null, null);
                                            Dbg.LogPart(rlx, " <rlii> "); /// this logs regardless of what happens
                                        }
                                        Dbg.Log(rlx, ";");

                                        /// add ConAddits info to dock
                                        if (rliInfo.IsSetup())
                                        {
                                            rliInfoDock.Add(rliInfo);
                                            rliInfo = new ResLibIntegrationInfo();
                                        }
                                    }
                                    Dbg.NudgeIndent(rlx, false);

                                    if (RCextras.HasElements())
                                        foreach(string rcEdit in RCextras)
                                            Dbg.Log(rlx, $". Edited RC :: {rcEdit}");
                                }

                                /// find matching and connect ConChanges
                                RCextras.Clear();
                                if (newRC.ConChanges.HasElements())
                                {
                                    rliType = RCFetchSource.ConChanges;
                                    Dbg.Log(rlx, "Making connections :: ConChanges to ConBase/ConAddits");
                                    Dbg.NudgeIndent(rlx, true);
                                    foreach (ContentChanges looseCc in newRC.ConChanges)
                                    {
                                        if (looseCc.IsSetup())
                                        {
                                            bool selfUpdatedQ = false;
                                            if (looseCc.InternalName.IsNotNEW())
                                                selfUpdatedQ = looseCc.InternalName.EndsWith(Sep) || looseCc.InternalName.Equals(Sep);

                                            if (!selfUpdatedQ)
                                            {
                                                Dbg.LogPart(rlx, $"Connecting ConChanges ({looseCc.ToStringShortened()}) >> ");
                                                ResContents matchingResCon = null;
                                                rliInfo = new ResLibIntegrationInfo();
                                                string matchedID = looseCc.RelatedDataID;

                                                /// matching here
                                                LogDecoder.DisassembleDataID(looseCc.RelatedDataID, out string dk, out string db, out _);
                                                string trueRelDataID = dk + db;
                                                foreach (ResContents resCon in Contents)
                                                {
                                                    if (resCon.ContainsDataID(looseCc.RelatedDataID, out _))
                                                    {
                                                        matchingResCon = resCon;
                                                        break;
                                                    }
                                                    else if (resCon.ContainsDataID(trueRelDataID, out _))
                                                    {
                                                        matchingResCon = resCon;
                                                        matchedID = trueRelDataID;
                                                        Dbg.LogPart(rlx, "[by trueID] ");
                                                        break;
                                                    }
                                                }

                                                /// connection here
                                                if (matchingResCon != null)
                                                {
                                                    Dbg.LogPart(rlx, $"Found ResCon {{{matchingResCon}}} >> ");
                                                    if (matchingResCon.ContainsDataID(matchedID, out RCFetchSource source, out DataHandlerBase dataSource))
                                                    {
                                                        bool connectCCq = false;
                                                        if (source == RCFetchSource.ConBaseGroup)
                                                        {
                                                            ContentBaseGroup matchCbg = (ContentBaseGroup)dataSource;
                                                            if (matchCbg != null)
                                                            {
                                                                if (matchingResCon.StoreConChanges(looseCc))
                                                                {
                                                                    RCextras.Add(matchingResCon.ToString());
                                                                    Dbg.LogPart(rlx, $"Connected with ConBase ({matchCbg}) [by ID '{looseCc.RelatedDataID}']");
                                                                    connectCCq = true;
                                                                }
                                                                else Dbg.LogPart(rlx, $"Rejected ConChanges");
                                                            }
                                                            else Dbg.LogPart(rlx, "ConBase could not be fetched");
                                                        }
                                                        else if (source == RCFetchSource.ConAdditionals)
                                                        {
                                                            ContentAdditionals matchCa = (ContentAdditionals)dataSource;
                                                            if (matchCa != null)
                                                            {
                                                                if (matchingResCon.StoreConChanges(looseCc))
                                                                {
                                                                    RCextras.Add(matchingResCon.ToString());
                                                                    Dbg.LogPart(rlx, $"Connected with ConAddits ({matchCa}) [by ID '{looseCc.RelatedDataID}']");
                                                                    connectCCq = true;
                                                                }
                                                                else Dbg.LogPart(rlx, $"Rejected ConChanges");
                                                            }
                                                            else Dbg.LogPart(rlx, "ConAddit could not be fetched");
                                                        }
                                                        else Dbg.LogPart(rlx, $"Source is neither ConBase nor ConAddits (source: '{source}')");

                                                        if (connectCCq)
                                                            rliInfo = new ResLibIntegrationInfo(rliType, looseCc.RelatedDataID, looseCc.ChangeDesc, matchingResCon.ContentName, looseCc.RelatedDataID);
                                                    }
                                                }
                                                else Dbg.LogPart(rlx, "No connections found (discarded)");

                                                if (!rliInfo.IsSetup())
                                                    rliInfo = new ResLibIntegrationInfo(rliType, looseCc.RelatedDataID, looseCc.ChangeDesc, null, null);
                                                Dbg.LogPart(rlx, " <rlii> "); /// this logs regardless of what happens
                                            }
                                            else Dbg.LogPart(rlx, $"Skipping self-updating ConChanges ({looseCc.ToStringShortened()})");
                                        }
                                        Dbg.Log(rlx, "; ");

                                        /// add ConChanges info to dock
                                        if (rliInfo.IsSetup())
                                        {
                                            rliInfoDock.Add(rliInfo);
                                            rliInfo = new ResLibIntegrationInfo();
                                        }
                                    }
                                    Dbg.NudgeIndent(rlx, false);

                                    if (RCextras.HasElements())
                                        foreach (string rcEdits in RCextras)
                                            Dbg.Log(rlx, $". Edited RC :: {rcEdits}");
                                }
                            }
                            /// ELSE don't sort loose contents
                            else
                            {
                                if (keepLooseRCQ)
                                {
                                    Dbg.Log(rlx, "Unallowed from sorting loose ResCon; ");
                                    newRC.ShelfID = GetNewShelfNum();
                                    shelfNums.Add(newRC.ShelfID);

                                    Contents.Add(newRC);
                                    Dbg.Log(rlx, $"*Added* :: {newRC}; ");
                                }
                                else
                                {
                                    Dbg.LogPart(rlx, "No pre-existing library contents to search for connections");
                                    if (newRC.ConAddits.HasElements())
                                    {
                                        Dbg.LogPart(rlx, $"; Discarding loose conAddits [{newRC.ConAddits.Count}]");
                                        foreach (ContentAdditionals disCa in newRC.ConAddits)
                                        {
                                            ResLibIntegrationInfo info = new(RCFetchSource.ConAdditionals, disCa.DataIDString, disCa.OptionalName, null, null);
                                            if (info.IsSetup())
                                                rliInfoDock.Add(info);
                                        }    
                                    }
                                    if (newRC.ConChanges.HasElements())
                                    {
                                        Dbg.LogPart(rlx, $"; Discarding loose conChanges [{newRC.ConChanges.Count}]");
                                        int countUpdt = 0;
                                        foreach (ContentChanges disCc in newRC.ConChanges)
                                        {
                                            bool selfUpdatedQ = false;
                                            if (disCc.InternalName.IsNotNEW())
                                                selfUpdatedQ = disCc.InternalName.EndsWith(Sep) || disCc.InternalName.Equals(Sep);

                                            ResLibIntegrationInfo info = new(RCFetchSource.ConChanges, disCc.RelatedDataID, disCc.ChangeDesc, null, null);
                                            if (info.IsSetup() && !selfUpdatedQ)
                                            {
                                                countUpdt++;
                                                rliInfoDock.Add(info);
                                            }
                                        }

                                        if (countUpdt < newRC.ConChanges.Count)
                                            Dbg.LogPart(rlx, $"; Omitted [{newRC.ConChanges.Count - countUpdt}] self-updated conChanges");
                                    }
                                    Dbg.Log(rlx, "; ");
                                }
                            }
                        }

                        // just add
                        else
                        {
                            ResLibAddInfo infoAdd = new(newRC.ToString(), SourceOverwrite.Content);
                            if (!IsDuplicateInLibrary(newRC))
                            {
                                /// get shelf id
                                newRC.ShelfID = GetNewShelfNum();
                                shelfNums.Add(newRC.ShelfID);
                                infoAdd.SetAddedObject(newRC.ToString());

                                /// add to content library
                                if (newRC.IsSetup())
                                {
                                    infoAdd.SetAddedOutcome();
                                    newShelfIdList.Add(newRC.ShelfID);
                                    Contents.Add(newRC);
                                    Dbg.Log(rlx, $"Added :: {newRC}");
                                }
                                else
                                {
                                    infoAdd.SetAddedOutcome(false);
                                    infoAdd.SetExtraInfo("Missing base content");
                                    Dbg.Log(rlx, $"Rejected :: Unset RC; No ContentBaseGroup.");
                                }
                            }
                            else
                            {
                                infoAdd.SetAddedOutcome(false, true);
                                Dbg.Log(rlx, $"Rejected duplicate :: {newRC}");
                            }

                            /// get info for conBase, conAddits, and conChanges
                            if (infoAdd.IsSetup())
                            {
                                rlaInfoDock.Add(infoAdd);
                                
                                // ConBase
                                if (newRC.ConBase != null)
                                {
                                    ResLibAddInfo infoAddCBG = new(newRC.ConBase.ToString(), SourceOverwrite.Content);
                                    infoAddCBG.SetSubSourceCategory(SourceCategory.Bse);
                                    infoAddCBG.SetAddedOutcome(infoAdd.addedQ);
                                    if (infoAddCBG.IsSetup())
                                        rlaInfoDock.Add(infoAddCBG);
                                }

                                // ConAddits
                                if (newRC.ConAddits.HasElements())
                                {
                                    foreach (ContentAdditionals conAddit in newRC.ConAddits)
                                    {
                                        ResLibAddInfo infoAddCA = new(conAddit.ToString(), SourceOverwrite.Content);
                                        infoAddCA.SetSubSourceCategory(SourceCategory.Adt);
                                        infoAddCA.SetAddedOutcome(infoAdd.addedQ);
                                        if (infoAddCA.IsSetup())
                                            rlaInfoDock.Add(infoAddCA);
                                    }
                                }

                                // ConChanges
                                if (newRC.ConChanges.HasElements())
                                {
                                    foreach (ContentChanges conChange in newRC.ConChanges)
                                    {
                                        ResLibAddInfo infoAddCC = new(conChange.ToString(), SourceOverwrite.Content);
                                        infoAddCC.SetSubSourceCategory(SourceCategory.Upd);
                                        infoAddCC.SetAddedOutcome(infoAdd.addedQ);
                                        if (infoAddCC.IsSetup())
                                            rlaInfoDock.Add(infoAddCC);
                                    }
                                }
                            }
                        }
                        addedContentsQ = true;
                    }
                }
                Dbg.NudgeIndent(rlx, false);                               
                Dbg.EndLogging(rlx);

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
            newShelfIDs = newShelfIdList.ToArray();
            return addedContentsQ;
        }
        public bool AddContent(bool keepLooseRCQ, params ResContents[] newContents)
        {
            return AddContent(keepLooseRCQ, out _, newContents);
        }
        public bool AddContent(params ResContents[] newContents)
        {
            return AddContent(false, out _, newContents);
        }
        bool IsDuplicateInLibrary(ResContents resCon)
        {
            bool foundDupe = false;
            if (Contents.HasElements() && resCon != null)
            {
                resCon = resCon.CloneResContent(true);
                if (resCon.IsSetup())
                {
                    for (int x = 0; x < Contents.Count && !foundDupe; x++)
                    {
                        ResContents resConX = Contents[x];
                        //resCon.ShelfID = resConX.ShelfID;
                        foundDupe = resConX.Equals(resCon) | resConX.ConBase.DataIDString == resCon.ConBase.DataIDString | resConX.ContentName == resCon.ContentName;
                    }
                }
            }
            return foundDupe;
        }        
        public bool AddLegend(params LegendData[] newLegends)
        {
            bool addedLegendQ = false;
            if (newLegends.HasElements())
            {
                Dbg.StartLogging("ResLibrary.AddLegend(prms LegData[])", out int rlx);
                Dbg.ToggleThreadOutputOmission(rlx);

                for (int lx = 0; lx < newLegends.Length; lx++)
                {
                    LegendData leg = newLegends[lx].CloneLegend();
                    if (leg != null)
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

                            ResLibAddInfo infoAdd = new(leg.ToString(), SourceOverwrite.Legend);
                            if (!isDupe)
                            {
                                infoAdd.SetAddedOutcome();
                                leg.AdoptIndex(Legends.Count);
                                Legends.Add(leg);
                                Dbg.Log(rlx, $"Added Lgd :: {leg}");
                                addedLegendQ = true;
                            }
                            else
                            {
                                bool edited, byOverwriteQ = false;
                                if (leg.VersionIntroduced.AsNumber < dupedLegData.VersionIntroduced.AsNumber)
                                {
                                    dupedLegData.Overwrite(leg, out ResLibOverwriteInfo info);
                                    edited = info.OverwrittenQ;
                                    byOverwriteQ = true;
                                }
                                else edited = dupedLegData.AddKeyDefinition(leg[0]);

                                if (edited)
                                {
                                    infoAdd.SetAddedOutcome();
                                    infoAdd.SetAddedObject(dupedLegData.ToString());
                                    infoAdd.SetExtraInfo($"Edited existing with new: '{leg}'");
                                    Dbg.Log(rlx, $"Edited Lgd {(byOverwriteQ ? "(ovr) " : "")}:: {dupedLegData}");
                                    addedLegendQ = true;
                                }
                                else
                                {
                                    bool isPartial = leg.ToString() != dupedLegData.ToString();
                                    infoAdd.SetAddedOutcome(false, true);
                                    infoAdd.SetExtraInfo(isPartial ? $"Partial duplicate by definition" : "");
                                    Dbg.Log(rlx, $"Rejected {(isPartial ? "partial " : "")}duplicate {(byOverwriteQ ? "(ovr) " : "")}:: {leg}");
                                }
                            }

                            if (infoAdd.IsSetup())
                                rlaInfoDock.Add(infoAdd);
                        }
                }
                Dbg.EndLogging(rlx);
            }
            return addedLegendQ;
        }
        public bool AddSummary(params SummaryData[] newSummaries)
        {
            bool addedSummaryQ = false;
            if (newSummaries.HasElements())
            {
                Dbg.StartLogging("ResLibrary.AddSummary(SumData[])", out int rlx);
                Dbg.ToggleThreadOutputOmission(rlx);

                for (int sx = 0; sx < newSummaries.Length; sx++)
                {
                    SummaryData sum = newSummaries[sx].CloneSummary();
                    if (sum != null)
                        if (sum.IsSetup())
                        {
                            bool isDupe = false;
                            if (Summaries.HasElements())
                            {
                                foreach (SummaryData sumDat in Summaries)
                                    if (sumDat.Equals(sum))
                                    {
                                        isDupe = true;
                                        break;
                                    }
                            }
                            else Summaries = new List<SummaryData>();

                            ResLibAddInfo infoAdd = new(sum.ToString(), SourceOverwrite.Summary);
                            if (!isDupe)
                            {
                                addedSummaryQ = true;
                                sum.AdoptIndex(Summaries.Count);
                                Summaries.Add(sum);
                                infoAdd.SetAddedOutcome();
                                Dbg.Log(rlx, $"Added Smry :: {sum.ToStringShortened()}");
                            }
                            else
                            {
                                infoAdd.SetAddedOutcome(false, true);
                                Dbg.Log(rlx, $"Rejected duplicate :: {sum.ToStringShortened()}");
                            }

                            if (infoAdd.IsSetup())
                                rlaInfoDock.Add(infoAdd);
                        }
                }
                Dbg.EndLogging(rlx);
            }
            return addedSummaryQ;
        }        
        public void Integrate(ResLibrary other, out ResLibAddInfo[] resLibAddedInfoDock, out ResLibIntegrationInfo[] resLibIntegrationInfoDock)
        {
            Dbg.StartLogging("ResLibrary.Integrate()", out int rlx);
            resLibIntegrationInfoDock = null;
            resLibAddedInfoDock = null;
            if (other != null)
            {
                Dbg.LogPart(rlx, "Other ResLibrary is instantiated; ");
                if (other.IsSetup())
                {
                    Dbg.Log(rlx, "Instance is also setup -- integrating libraries; ");

                    // add resCons
                    AddContent(other.Contents.ToArray());
                    if (rliInfoDock.HasElements())
                        resLibIntegrationInfoDock = rliInfoDock.ToArray();

                    // add legend
                    AddLegend(other.Legends.ToArray());

                    // add summary
                    AddSummary(other.Summaries.ToArray());

                    if (rlaInfoDock.HasElements())
                        resLibAddedInfoDock = rlaInfoDock.ToArray();
                }
                else Dbg.Log(rlx, "Other ResLibrary is not setup; ");
            }
            else Dbg.Log(rlx, "Other ResLibrary is null; ");
            Dbg.EndLogging(rlx);
        }        
        public void Overwrite(ResLibrary other, out ResLibOverwriteInfo[] resLibOverwriteInfoDock, out ResLibIntegrationInfo[] looseIntegrationInfoDock)
        {
            /// PLANNING
            ///     Need : version number, overwriting library (X)
            ///         Note : version number can be fetched from overwriting library information.   
            /// 
            ///     - Get all the contents, legends, and summaries added in version N of this instance (A)
            ///     - Contents
            ///         With exception to loose contents, 'A's contents should be organized similarly to 'Xs' contents for simple match up
            ///         #Base Contents
            ///             - For every X contents: Adopt As[x] shelf number to Xs[x] then compare (duplicate or different)
            ///                 If As[x] is same as Xs[x]; discard Xs[x] as 'no changes', else replace object As[x] with Xs[x]
            ///                 If Xs[x] does not have a matching As[x], use AddContent() to add the new object.
            ///             - Overwite should note any differences between the two objects if any, and information on the two objects
            ///                 Notable info: CBG (name, ids), CAs / CCs (loosened* due to CBG ids changes, ids, names, desc)
            ///         #Loose Contents
            ///             - For every X contents: Check if As[x] contains data ID of Xs[0] addit/change content
            ///                 If As[x] addits/change is same as matched Xs[0] addit/change; discard Xs[0] a/c as 'no changes', else replace matching As[x] addits/change object
            ///                 If Xs[0] addit/change has no base content to connect to; note and discard the addit/change object.
            ///             - Overwrite should note any difference between the two objects of any, and information on the two objects
            ///                 Notable info: CA (ids, optName, relName, relID), CC (relID, relName, changeDesc)
            ///     - Legends
            ///         Legends in 'A' with a matching version number are the only objects necessary to being checked. Additional definition will be completely ignored.
            ///         - For every legend in A: If As[x] verNum equals overwriting verNum
            ///             If As[x] legend key equals Xs[x] legend key and As[x] contains Xs[x] definition, no change; else replace As[x] definition with Xs[x] 
            ///     - Summaries
            ///         The summary in 'A' with a matching version number is completely overwritten
            ///             - For every summary in A: If As[x] summary verNum equals Xs[x] summary verNum
            ///                 If As[x] summary parts is equivalent to Xs[x] summary parts, 'no changes'; else replace As[x] summary object with Xs[x] summary object
            ///      

            resLibOverwriteInfoDock = null;
            looseIntegrationInfoDock = null;            
            if (other != null)
            {
                Dbg.StartLogging("ResLibrary.Overwrite()", out int rlx);
                Dbg.Log(rlx, $"Received other ResLib instance; Is Setup? {other.IsSetup()};");
                if (other.Contents.HasElements())
                {
                    VerNum verNum = other.Contents[0].ConBase.VersionNum;
                    if (other.Contents[0].ContentName == LooseResConName && other.Contents.HasElements(2))
                        verNum = other.Contents[1].ConBase.VersionNum;

                    Dbg.LogPart(rlx, $"Other ResLib instance information :: Contents [{other.Contents.Count}], ");
                    if (other.Legends.HasElements())
                        Dbg.LogPart(rlx, $"Legends [{other.Legends.Count}], ");
                    if (other.Summaries.HasElements())
                        Dbg.LogPart(rlx, $"Summaries [{other.Summaries.Count}], ");
                    Dbg.Log(rlx, $"Version to overwrite [{verNum}];");


                    ResLibrary libVer = GetVersion(verNum);
                    if (libVer != null)
                    {
                        VerNum libVerVerify = libVer.Contents[0].ConBase.VersionNum;
                        if (libVer.Contents[0].ContentName == LooseResConName && libVer.Contents.HasElements(2))
                            libVerVerify = libVer.Contents[1].ConBase.VersionNum;

                        Dbg.Log(rlx, $"Fetched library version {verNum.ToStringNums()} [Confirmed? {libVerVerify.Equals(verNum)}] -- Is Setup? [{libVer.IsSetup()}] :: Contents? [{libVer.Contents.HasElements()}], Legends? [{libVer.Legends.HasElements()}], Summaries? [{libVer.Summaries.HasElements()}]; ");
                        List<ResLibOverwriteInfo> rloInfoDock = new List<ResLibOverwriteInfo>();
                        ResContents integrationQueueRC = null;
                        Dbg.Log(rlx, "Initialized ResLibrary Overwrite Info Dock; ");

                        // OVERWRITING : Base Contents
                        Dbg.Log(rlx, $"Overwriting: Base Contents; {(libVer.Contents.HasElements() ? "Proceed..." : "Skip")}");
                        if (libVer.Contents.HasElements())
                        {
                            Dbg.NudgeIndent(rlx, true);

                            // net change detection loop
                            Dbg.LogPart(rlx, "Generating net changes list (aligns with existing list)  //  ");
                            Dbg.Log(rlx, "NCLegend :: [=]Existing & Overwriting has,  [-]Only Existing has,  [+]Only Overwriting has,  (indexExisting:indexOverwriting),  [*]alternate ResCon Matching,  [l]loose ResCon; ");
                            Dbg.LogPart(rlx, "> Net Changes Results ::");
                            /// Net Change List usage
                            /// - Aligns with existing content list, sources numbers from overwriting list
                            ///     Existing has, Overwriting Matches ->indexO [in existing range]
                            ///     Existing has, Overwriting does not have -> null 
                            ///     No existing, Overwriting has -> indexO [out of existing range]
                            List<int?> netChangeList = new();
                            string otherIndexList = "";
                            bool endNetChangeQ = false;
                            int netIxOtherLooseBackset = 0;
                            for (int ncx = 0; !endNetChangeQ; ncx++)
                            {
                                /// matches are determined by exact equivalence, otherwise by content name or data IDs
                                string ncResult = null;
                                ResContents resConE = null;
                                int? indexOther = null, indexExisting = null;
                                bool looseRCEq = false;

                                /// get existing
                                if ((ncx - netIxOtherLooseBackset).IsWithin(0, libVer.Contents.Count - 1))
                                {
                                    resConE = libVer.Contents[ncx - netIxOtherLooseBackset];
                                    indexExisting = ncx - netIxOtherLooseBackset;
                                    if (resConE.ContentName == LooseResConName)
                                        looseRCEq = true;
                                }

                                /// get overwriting: either matching, or addition; 
                                /// determine index and match result
                                if (other.Contents.HasElements())
                                {
                                    bool foundOther = false;
                                    for (int nox = 0; !foundOther && nox < other.Contents.Count; nox++)
                                    {
                                        ResContents resConO = other.Contents[nox];
                                        bool looseRCOq = false;
                                        if (resConO != null)
                                        {
                                            if (resConO.ConBase != null)
                                                looseRCOq = resConO.ContentName == LooseResConName;
                                        }

                                        if (!looseRCOq)
                                        {
                                            /// get matching
                                            if (resConE != null)
                                            {
                                                ncResult = "-";
                                                if (resConO != null)
                                                {
                                                    if (!looseRCEq)
                                                    {
                                                        resConO = resConO.CloneResContent(true);
                                                        resConO.ShelfID = resConE.ShelfID;

                                                        bool alternateResConMatchQ = false;
                                                        if (resConO.IsSetup() && resConE.IsSetup() && !resConO.Equals(resConE))
                                                            alternateResConMatchQ = resConO.ContentName == resConE.ContentName || resConO.ConBase.DataIDString == resConE.ConBase.DataIDString;


                                                        if ((resConO.Equals(resConE) || alternateResConMatchQ) && !otherIndexList.Contains($" {nox} "))
                                                        {
                                                            ncResult = "=" + (alternateResConMatchQ ? "*" : "");
                                                            indexOther = nox;
                                                            foundOther = true;
                                                            otherIndexList += $" {nox} ";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ncResult += "l";
                                                        foundOther = true;
                                                    }
                                                }
                                            }
                                            /// get addition
                                            else
                                            {
                                                if (resConO != null)
                                                {
                                                    if (!otherIndexList.Contains($" {nox} "))
                                                    {
                                                        ncResult = "+";
                                                        indexOther = nox;
                                                        foundOther = true;
                                                        otherIndexList += $" {nox} ";
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!otherIndexList.Contains($" {nox} "))
                                            {
                                                if (looseRCEq)
                                                    ncResult = "=l";
                                                else
                                                {
                                                    ncResult = "+l";
                                                    netIxOtherLooseBackset += 1;
                                                }
                                                indexOther = nox;
                                                foundOther = true;
                                                otherIndexList += $" {nox} ";
                                            }
                                        }
                                    }
                                    
                                    /// ---
                                }

                                /// add result to list
                                if (ncResult.IsNotNEW())
                                {
                                    netChangeList.Add(indexOther);

                                    Dbg.LogPart(rlx, $"  {ncResult}(");
                                    if (indexExisting.HasValue)
                                        Dbg.LogPart(rlx, indexExisting.Value.ToString());
                                    else Dbg.LogPart(rlx, "'");
                                    Dbg.LogPart(rlx, ":");
                                    if (indexOther.HasValue)
                                        Dbg.LogPart(rlx, indexOther.Value.ToString());
                                    else Dbg.LogPart(rlx, "'");
                                    Dbg.LogPart(rlx, ")");
                                }
                                else endNetChangeQ = true;
                            }
                            Dbg.Log(rlx, "; ");


                            // overwriting loop
                            bool noMoreRCq = false;
                            int ixExistingLoose = 0, ixOtherLoose = 0, ixInfoDockLatest = 0;
                            for (int lvx = 0; !noMoreRCq; lvx++)
                            {
                                /// ALTERNATIVELY  (and probably for the better)
                                /// Introduce a method for ResContents, ConBaseGroup, ConAdditionals, and ConChanges that handles self-overwriting (Suggest; .Overwrite())
                                /// ... rather than squeezing all that work into this method
                                ///     PROS
                                ///         - Overwriting content is simplified; 
                                ///             Instead of removing and replacing content information, the instance edits itself with the new information.
                                ///         - Overwriting of loose contents is simplified; 
                                ///             Instead of a complicated process of searching, remomving, and replacing, the connected instance edits itself with the new information
                                ///     CONS
                                ///         - Loss of super-detailed Dbug logs possible
                                ///         - Loss of detailed ResLib Overwrite Info possible; Existing / Overwriting variables may have to change from DataHandlerBase type to string type.
                                ///             > Counter: the new methods could be made to return ResLib Overwrite Info on use.
                                ///             
                                /// ------ aside note ------
                                /// Regarding cloning; ResContents, LegendData, and SummaryData classes should adapt a new method for proper cloning which will be utilized when adding to library.
                                /// 
                                /// 
                                /// *******
                                /// Both addressed, now to redo this section of overwriting!  (after a quick commit)
                                ///     

                                ResContents resConE = null, resConO = null;
                                string strExist = "", strOther = "";
                                if ((lvx - ixOtherLoose).IsWithin(0, libVer.Contents.Count - 1))
                                {
                                    resConE = libVer.Contents[lvx - ixOtherLoose];
                                    strExist = resConE.ToString();
                                }
                                if (lvx.IsWithin(0, netChangeList.Count - 1))
                                {
                                    int? oIx = netChangeList[lvx];
                                    if (oIx.HasValue)
                                    {
                                        if (oIx.Value.IsWithin(0, other.Contents.Count - 1))
                                        {
                                            resConO = other.Contents[oIx.Value];
                                            strOther = resConO.ToString();
                                        }
                                    }
                                }

                                Dbg.Log(rlx, $"@{lvx} | Existing {{{strExist}}}  //  Overwriting {{{strOther}}}");                                

                                noMoreRCq = resConE == null && resConO == null;
                                if (!noMoreRCq)
                                {
                                    bool enableLooseHandlingQ = false;

                                    Dbg.LogPart(rlx, " -> ");
                                    /// IF has existing:
                                    ///     IF existing is loose: Enable Loose Handling*;
                                    ///     ELSE 'IF existing is not loose:'
                                    ///         IF has overwriting:
                                    ///             IF overwriting is loose: Enable Loose Handling*;    
                                    ///             ELSE 'IF overwriting is not loose': edit existing with overwriting;
                                    ///         ELSE remove existing;
                                    /// ELSE 'IF has overwriting:' 
                                    ///     IF overwriting is loose: Enable Loose Handling*;
                                    ///     ELSE 'IF overwriting is not loose:' add overwriting;                                    
                                    if (resConE != null)
                                    {
                                        Dbg.LogPart(rlx, "Existing RC exists; ");
                                        if (resConE.ContentName == LooseResConName)
                                        {
                                            enableLooseHandlingQ = true;
                                            Dbg.LogPart(rlx, "Existing RC is loose");
                                        }
                                        else
                                        {
                                            Dbg.LogPart(rlx, $"{(resConO == null ? "No overwriting RC" : "Overwriting RC exists")}; ");
                                            if (resConO != null)
                                            {
                                                if (resConO.ContentName == LooseResConName)
                                                    enableLooseHandlingQ = true;
                                                else
                                                {
                                                    Contents[resConE.ShelfID].Overwrite(resConO, out ResLibOverwriteInfo[] info);
                                                    AddToInfoDock(info);
                                                }
                                                Dbg.LogPart(rlx, $"{(enableLooseHandlingQ ? "Overwriting RC is loose" : "Existing RC may be overwritten by overwriting RC")}");
                                            }
                                            else
                                            {
                                                Contents[resConE.ShelfID] = new ResContents();
                                                ResLibOverwriteInfo info = new(resConE.ToString(), null);
                                                info.SetOverwriteStatus();
                                                Dbg.LogPart(rlx, $"Removed existing RC (shelf ix {resConE.ShelfID}) from library contents");

                                                AddToInfoDock(info);
                                            }
                                        }
                                    }
                                    else
                                    {                                        
                                        if (resConO.ContentName == LooseResConName)
                                            enableLooseHandlingQ = true;
                                        else
                                        {
                                            AddContent(false, out int[] shelfNums, resConO);
                                            resConO = resConO.CloneResContent(true);
                                            if (shelfNums.HasElements())
                                                resConO.ShelfID = shelfNums[0];

                                            ResLibOverwriteInfo info = new(null, resConO.ToString());
                                            info.SetOverwriteStatus();
                                            AddToInfoDock(info);
                                        }
                                        Dbg.LogPart(rlx, $"Overwriting RC exists (no existing RC); {(enableLooseHandlingQ ? "Overwriting RC is loose" : "Added overwriting RC to library as new content")}");
                                    }
                                    Dbg.Log(rlx, "; ");


                                    /// IF Enable Loose Handling*:
                                    ///     IF has existing: (IF existing is loose: increment Existing Loose Index (ELI));
                                    ///     IF has overwriting: (IF overwriting is loose: increment Overwriting Loose Index (OLI));
                                    ///     IF ELI equals OLI: Set both OLI and ELI to '0';
                                    ///     
                                    ///     FOR: loop until 'No More Loose Contents' is true
                                    ///         IF has existing loose:
                                    ///             IF has overwriting loose: edit existing loose with overwriting loose (*If loosened as result of changes, add to integration queue);
                                    ///             ELSE remove existing loose;
                                    ///         
                                    ///      a  ELSE; 
                                    ///             IF has overwriting loose addit: add overwriting loose addit to integration queue; ELSE -nothing-;
                                    ///      b  ELSE:
                                    ///             IF has overwriting loose change: 
                                    ///                 IF overwriting loose change is not self-updated: add overwriting loose change to integration queue; ELSE -nothing-;
                                    ///                 ELSE -nothing-
                                    ///     
                                    if (enableLooseHandlingQ)
                                    {
                                        bool isLooseE = false, isLooseO = false;
                                        List<string> queueLooseDataIDs = new();
                                        List<ContentAdditionals> queueLooseAddits = new();
                                        List<ContentChanges> queueLooseChanges = new();

                                        Dbg.LogPart(rlx, " >> ");
                                        Dbg.LogPart(rlx, "Handling Loose : ");
                                        if (resConE != null)
                                        {
                                            isLooseE = resConE.ContentName == LooseResConName;
                                            ixExistingLoose = isLooseE ? 1 : 0;
                                        }
                                        if (resConO != null)
                                        {
                                            isLooseO = resConO.ContentName == LooseResConName;
                                            ixOtherLoose = isLooseO ? 1 : 0;
                                        }
                                        Dbg.LogPart(rlx, $"Existing is loose? [{ixExistingLoose == 1}], Overwriting is loose? [{ixOtherLoose == 1}]; ");
                                        if (ixExistingLoose == ixOtherLoose)
                                        {
                                            ixExistingLoose = 0;
                                            ixOtherLoose = 0;
                                        }
                                        Dbg.Log(rlx, $"Back sets (Ex/Ov) [{ixOtherLoose} / {ixExistingLoose}]; ");

                                        
                                        bool noMoreLooseQ = false;
                                        string matchMethodAdt = "", matchMethodUpd = "", ixAdtListO = "", ixUpdListO = "";
                                        Dbg.NudgeIndent(rlx, true);
                                        for (int lx = 0; !noMoreLooseQ; lx++)
                                        {
                                            ContentAdditionals caE = null, caO = null;
                                            ContentChanges ccE = null, ccO = null;
                                            string strAdtE = "", strAdtO = "", strUpdE = "", strUpdO = "";

                                            if (isLooseE)
                                            {
                                                if (resConE.ConAddits.HasElements(lx + 1))
                                                {
                                                    caE = resConE.ConAddits[lx];
                                                    strAdtE = caE.ToString();
                                                }
                                                if (resConE.ConChanges.HasElements(lx + 1))
                                                {
                                                    ccE = resConE.ConChanges[lx];
                                                    strUpdE = ccE.ToString();
                                                }
                                            }
                                            if (isLooseO)
                                            {
                                                if (resConO.ConAddits.HasElements())
                                                {
                                                    for (int cox = 0; cox < resConO.ConAddits.Count && caO == null; cox++)
                                                    {
                                                        ContentAdditionals caO_ = resConO.ConAddits[cox];
                                                        string ixKey = $" {cox} ";

                                                        if (!ixAdtListO.Contains(ixKey))
                                                        {
                                                            if (caE != null)
                                                            {
                                                                if (caO_.RelatedDataID == caE.RelatedDataID)
                                                                {
                                                                    matchMethodAdt = $" (ID Match @{cox})";
                                                                    caO = caO_;
                                                                    strAdtO = caO_.ToString();
                                                                }
                                                            }
                                                            else
                                                            {
                                                                caO = caO_;
                                                                strAdtO = caO_.ToString();
                                                            }

                                                            if (caO != null)
                                                                ixAdtListO += ixKey;
                                                        }
                                                    }                                                    
                                                }

                                                if (resConO.ConChanges.HasElements())
                                                {
                                                    for (int cox = 0; cox < resConO.ConChanges.Count && ccO == null; cox++)
                                                    {
                                                        ContentChanges ccO_ = resConO.ConChanges[cox];
                                                        string ixKey = $" {cox} ";

                                                        if (!ixUpdListO.Contains(ixKey))
                                                        {
                                                            if (ccE != null)
                                                            {
                                                                if (ccO_.RelatedDataID == ccE.RelatedDataID)
                                                                {
                                                                    matchMethodUpd = $" (ID Match @{cox})";
                                                                    ccO = ccO_;
                                                                    strUpdO = ccO_.ToString();
                                                                }
                                                            }
                                                            else
                                                            {
                                                                ccO = ccO_;
                                                                strUpdO = ccO_.ToString();
                                                            }

                                                            if (ccO != null)
                                                                ixUpdListO += ixKey;
                                                        }
                                                    }
                                                }

                                                //if (resConO.ConAddits.HasElements(lx + 1))
                                                //{
                                                //    caO = resConO.ConAddits[lx];
                                                //    strAdtO = caO.ToString();
                                                //}
                                                //if (resConO.ConChanges.HasElements(lx + 1))
                                                //{
                                                //    ccO = resConO.ConChanges[lx];
                                                //    strUpdO = ccO.ToString();
                                                //}
                                            }

                                            noMoreLooseQ = caE == null && ccE == null && caO == null && ccO == null;

                                            // content additionals
                                            Dbg.Log(rlx, $"ConAdt @{lx} | Existing [{strAdtE}]  //  Overwriting [{strAdtO}]{matchMethodAdt}; ");
                                            Dbg.LogPart(rlx, " -> ");
                                            if (caE != null)
                                            {
                                                Dbg.LogPart(rlx, $"Existing Addit exists; ");
                                                if (caO != null)
                                                {
                                                    ResContents parentRC = null;
                                                    ContentAdditionals loosened;
                                                    if (caE.RelatedShelfID != ResContents.NoShelfNum)
                                                    {
                                                        parentRC = Contents[caE.RelatedShelfID];
                                                        if (caE.Index != ResContents.NoShelfNum && parentRC.ConAddits.HasElements(caE.Index + 1))
                                                        {
                                                            if (caE.Equals(parentRC.ConAddits[caE.Index]))
                                                                caE = parentRC.ConAddits[caE.Index];
                                                        }
                                                    }
                                                    loosened = caE.OverwriteLoose(caO, parentRC, out ResLibOverwriteInfo info);

                                                    Dbg.LogPart(rlx, $"Existing Addit may be overwritten; {(loosened != null ? "Edited existing has been loosened, adding to loose queue" : "")}");
                                                    if (loosened != null)
                                                    {
                                                        queueLooseDataIDs.Add(loosened.RelatedDataID);
                                                        queueLooseAddits.Add(loosened);
                                                    }

                                                    AddToInfoDock(info);
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(rlx, "Removing existing addit from connected RC");
                                                    ResContents parentRC = null;
                                                    if (caE.RelatedShelfID != ResContents.NoShelfNum)
                                                        parentRC = Contents[caE.RelatedShelfID];

                                                    ResLibOverwriteInfo info = new(caE.ToString(), null);
                                                    info.SetSourceSubCategory(SourceCategory.Adt);
                                                    if (parentRC != null)
                                                    {
                                                        bool disposedQ = parentRC.DisposeConAdditional(caE);  
                                                        info.SetOverwriteStatus(disposedQ);
                                                        Dbg.LogPart(rlx, $" {(disposedQ ? "(Done)" : "(Undisposed, Failed)")}");
                                                    }
                                                    else
                                                    {
                                                        info.SetOverwriteStatus(false);
                                                        Dbg.LogPart(rlx, " (Failed)");
                                                    }

                                                    AddToInfoDock(info);
                                                }
                                            }
                                            else
                                            {
                                                if (caO != null)
                                                {
                                                    Dbg.LogPart(rlx, "Overwriting Addit exists (no existing addit); Adding overwriting addit to loose queue");
                                                    ResLibOverwriteInfo info = new(null, caO.ToString());
                                                    info.SetSourceSubCategory(SourceCategory.Adt);
                                                    info.SetLooseContentStatus();

                                                    queueLooseDataIDs.Add(caO.RelatedDataID);
                                                    queueLooseAddits.Add(caO);
                                                    info.SetOverwriteStatus();

                                                    AddToInfoDock(info);
                                                }
                                                else Dbg.LogPart(rlx, "No overwriting or existing addit");
                                            }
                                            Dbg.Log(rlx, "; ");


                                            // content changes
                                            Dbg.Log(rlx, $"ConChg @{lx} | Existing [{strUpdE}]  //  Overwriting [{strUpdO}]{matchMethodUpd}; ");
                                            Dbg.LogPart(rlx, " -> ");
                                            if (ccE != null)
                                            {
                                                Dbg.LogPart(rlx, "Existing changes exists; ");
                                                if (ccO != null)
                                                {
                                                    ResContents parentRC = null;
                                                    ContentChanges loosened;
                                                    if (ccE.RelatedShelfID != ResContents.NoShelfNum)
                                                    {
                                                        parentRC = Contents[ccE.RelatedShelfID];
                                                        if (ccE.Index != ResContents.NoShelfNum && parentRC.ConChanges.HasElements(ccE.Index + 1))
                                                        {
                                                            if (ccE.Equals(parentRC.ConChanges[ccE.Index]))
                                                                ccE = parentRC.ConChanges[ccE.Index];
                                                        }
                                                    }
                                                    loosened = ccE.OverwriteLoose(ccO, parentRC, out ResLibOverwriteInfo info);

                                                    Dbg.LogPart(rlx, $"Existing Changes may be overwritten; {(loosened != null ? "Edited existing has been loosened, adding to loose queue" : "")}");
                                                    if (loosened != null)
                                                    {
                                                        queueLooseDataIDs.Add(loosened.RelatedDataID);
                                                        queueLooseChanges.Add(loosened);
                                                    }

                                                    AddToInfoDock(info);
                                                }
                                                else
                                                {
                                                    Dbg.LogPart(rlx, "Removing existing changes from connected RC");
                                                    ResContents parentRC = null;
                                                    if (ccE.RelatedShelfID != ResContents.NoShelfNum)
                                                        parentRC = Contents[ccE.RelatedShelfID];

                                                    ResLibOverwriteInfo info = new(ccE.ToString(), null);
                                                    info.SetSourceSubCategory(SourceCategory.Upd);
                                                    if (parentRC != null)
                                                    {
                                                        bool disposedQ = parentRC.DisposeConChanges(ccE);
                                                        info.SetOverwriteStatus(disposedQ);
                                                        Dbg.LogPart(rlx, $" {(disposedQ ? "(Done)" : "(Undisposed, Failed)")}");
                                                    }
                                                    else
                                                    {
                                                        info.SetOverwriteStatus(false);
                                                        Dbg.LogPart(rlx, " (Failed)");
                                                    }

                                                    AddToInfoDock(info);
                                                }
                                            }
                                            else
                                            {
                                                if (ccO != null)
                                                {
                                                    bool isSelfUpdatedQ = false;
                                                    if (ccO.InternalName.IsNotNEW())
                                                        isSelfUpdatedQ = ccO.InternalName.EndsWith(Sep) || ccO.InternalName.Equals(Sep);

                                                    if (!isSelfUpdatedQ)
                                                    {
                                                        Dbg.LogPart(rlx, "Overwriting changes exists (no existing changes); Adding overwriting changes to loose queue");
                                                        ResLibOverwriteInfo info = new(null, ccO.ToString());
                                                        info.SetSourceSubCategory(SourceCategory.Upd);
                                                        info.SetLooseContentStatus();

                                                        queueLooseDataIDs.Add(ccO.RelatedDataID);
                                                        queueLooseChanges.Add(ccO);
                                                        info.SetOverwriteStatus();

                                                        AddToInfoDock(info);
                                                    }
                                                    else Dbg.LogPart(rlx, "Overwriting changes were self-updated, skipping");
                                                }
                                                else Dbg.LogPart(rlx, "No overwriting or existing changes");
                                            }
                                            Dbg.Log(rlx, "; ");


                                            // compile loose queue
                                            if (noMoreLooseQ)
                                            {
                                                Dbg.LogPart(rlx, "Compiling any queued loose contents");
                                                if (queueLooseDataIDs.HasElements())
                                                {
                                                    ContentBaseGroup looseCbg = new(verNum, LooseResConName, queueLooseDataIDs.ToArray());
                                                    integrationQueueRC = new ResContents(0, looseCbg, queueLooseAddits.ToArray(), queueLooseChanges.ToArray());
                                                    Dbg.LogPart(rlx, $"; Loose queue compilied :: {integrationQueueRC}");
                                                }
                                                else Dbg.LogPart(rlx, " (None recieved)");
                                                Dbg.Log(rlx, "; ");
                                            }                                            
                                        }
                                        Dbg.NudgeIndent(rlx, false);
                                    }

                                }
                                else Dbg.Log(rlx, "No ResCons provided; Base Contents overwriting End; ");


                                // reports the new overwriting info items
                                Dbg.NudgeIndent(rlx, true);
                                if (rloInfoDock.HasElements() && !noMoreRCq)
                                {
                                    Dbg.Log(rlx, "Overwrite summary of these ResContents; ");
                                    Dbg.NudgeIndent(rlx, true);
                                    for (int ix = ixInfoDockLatest; ix < rloInfoDock.Count; ix++)
                                        Dbg.Log(rlx, rloInfoDock[ix].ToString() + "; ");
                                    ixInfoDockLatest = rloInfoDock.Count;
                                    Dbg.NudgeIndent(rlx, false);
                                }
                                Dbg.NudgeIndent(rlx, false);
                            }

                            Dbg.NudgeIndent(rlx, false);
                        }


                        // OVERWRITING : Legend Contents
                        Dbg.Log(rlx, $"Overwriting: Legends; {(libVer.Legends.HasElements() || other.Legends.HasElements() ? "Proceed..." : "Skip")}");
                        if (libVer.Legends.HasElements() || other.Legends.HasElements())
                        {
                            Dbg.NudgeIndent(rlx, true);

                            /// The overwriting process for the legends will need to be redone.
                            ///     - Previously, only the introduced legends would be fetched from the library, overlooking the array of used legends from the overwriting library (other)
                            ///     - The list of overwriting legends may not necessarily match that the existing library's legends. Mis-matches could make for near impossible overwrites
                            ///     - In this case, legends from each have to be matched up by their key before continuing to the overwriting process
                            ///     

                            bool noMoreLegsQ = false;
                            string ixKeyLegendsO = "";
                            for (int lgx = 0; !noMoreLegsQ; lgx++)
                            {
                                LegendData legE = null, legO = null;
                                string strLegE = "", strLegO = "", strLegOMatchMethod = "";
                                if (libVer.Legends.HasElements(lgx + 1))
                                {
                                    legE = libVer.Legends[lgx];
                                    strLegE = legE.ToString();
                                }
                                /// IF has existing: find matching overwriting legend for overwrite process; ELSE fetch next overwriting legend in list
                                if (legE != null)
                                {
                                    strLegOMatchMethod = "Match with existing";
                                    /// a one-time loop here that finds a overwriting legend data with the same key as existing (if existing exists)
                                    foreach (LegendData oLeg in other.Legends)
                                        if (oLeg.Key == legE.Key && !ixKeyLegendsO.Contains($" {oLeg.Key} "))
                                        {
                                            legO = oLeg;
                                            strLegO = legO.ToString();
                                            ixKeyLegendsO += $" {oLeg.Key} ";
                                            break;
                                        }
                                }
                                else
                                {
                                    strLegOMatchMethod = "Fetch next overwriting";
                                    foreach (LegendData oLeg in other.Legends)
                                    {
                                        if (!ixKeyLegendsO.Contains($" {oLeg.Key} "))
                                        {
                                            legO = oLeg;
                                            strLegO = legO.ToString();
                                            ixKeyLegendsO += $" {oLeg.Key} ";
                                        }
                                    }

                                    //if (other.Legends.HasElements(lgx + 1))
                                    //{
                                    //    legO = other.Legends[lgx];
                                    //    strLegO = legO.ToString();
                                    //}
                                }


                                Dbg.Log(rlx, $"@ix{lgx}  |  Existing [{strLegE}]  //  Overwriting [{strLegO}] (Match method : {strLegOMatchMethod}); ");
                                ResLibOverwriteInfo info = new(strLegE, strLegO, SourceOverwrite.Legend);
                                info.SetOverwriteStatus(false);

                                noMoreLegsQ = legE == null && legO == null;

                                if (!noMoreLegsQ)
                                {
                                    Dbg.LogPart(rlx, " -> ");
                                    if (legE != null)
                                    {
                                        Dbg.LogPart(rlx, "Existing Legend exists; ");
                                        if (legO != null)
                                        {
                                            Dbg.LogPart(rlx, "Overwriting Legend exists; Existing Legend may be overwritten with Overwriting Legend");
                                            if (legE.Index != ResContents.NoShelfNum)
                                                Legends[legE.Index].Overwrite(legO, out info);
                                            else Dbg.LogPart(rlx, " (Failed)");

                                            AddToInfoDock(info);
                                        }
                                        else
                                        {
                                            Dbg.LogPart(rlx, "No overwriting legend; ");
                                            if (legE.VersionIntroduced.Equals(verNum))
                                            {
                                                Dbg.LogPart(rlx, $"Existing Legend introduced in current version ({verNum}); Removing Existing Legend");
                                                if (legE.Index != ResContents.NoShelfNum)
                                                {
                                                    LegendData trueLegE = Legends[legE.Index];
                                                    if (trueLegE.Equals(legE))
                                                    {
                                                        Legends[legE.Index] = new LegendData();
                                                        info.SetOverwriteStatus();
                                                    }
                                                }

                                                AddToInfoDock(info);
                                            }
                                            else Dbg.LogPart(rlx, $"Existing Legend introduced in a different version, no changes made");
                                        }
                                    }
                                    else
                                    {
                                        Dbg.LogPart(rlx, "Overwriting Legend exists (no existing Legend); Adding new Legend item to library");
                                        bool addedLegOq = AddLegend(legO);
                                        info.SetOverwriteStatus(addedLegOq);

                                        AddToInfoDock(info);
                                    }
                                    Dbg.Log(rlx, "; ");
                                    Dbg.Log(rlx, $" -> Legend Overwriting Outcome: {info}; ");
                                }
                                else Dbg.Log(rlx, "No Legends provided; Legends overwriting End; ");
                            }
                            Dbg.NudgeIndent(rlx, false);
                        }


                        // OVERWRITING : Summary Contents
                        Dbg.Log(rlx, $"Overwriting: Summaries; {(libVer.Summaries.HasElements() || other.Summaries.HasElements() ? "Proceed..." : "Skip")}");
                        if (libVer.Summaries.HasElements() || other.Summaries.HasElements())
                        {
                            Dbg.NudgeIndent(rlx, true);

                            bool noMoreSumsQ = false;
                            for (int smx = 0; !noMoreSumsQ; smx++)
                            {
                                SummaryData sumE = null, sumO = null;
                                string strSumE = "", strSumO = "";
                                if (libVer.Summaries.HasElements(smx + 1))
                                {
                                    sumE = libVer.Summaries[smx];
                                    strSumE = sumE.ToString();
                                }
                                if (other.Summaries.HasElements(smx + 1))
                                {
                                    sumO = other.Summaries[smx];
                                    strSumO = sumO.ToString();
                                }
                                Dbg.Log(rlx, $"@ix{smx}  |  Existing [{strSumE}]  //  Overwriting [{strSumO}]; ");
                                ResLibOverwriteInfo info = new(strSumE, strSumO, SourceOverwrite.Summary);
                                info.SetOverwriteStatus(false);

                                noMoreSumsQ = sumE == null && sumO == null;

                                if (!noMoreSumsQ)
                                {
                                    // note: summaries can be replaced, but they can never be removed, and therefore an 'add' also may never occur
                                    // There must always be a summary, a maximum of 1!
                                    Dbg.LogPart(rlx, " -> ");
                                    if (sumE != null)
                                    {
                                        Dbg.LogPart(rlx, "Existing Summary exists; ");
                                        if (sumO != null)
                                        {
                                            Dbg.LogPart(rlx, "Overwriting Summary exists; Existing Summary may be overwritten with Overwriting Summary");
                                            if (sumE.Index != ResContents.NoShelfNum)
                                                Summaries[sumE.Index].Overwrite(sumO, out info);
                                            else Dbg.LogPart(rlx, " (Failed)");
                                        }
                                        else Dbg.LogPart(rlx, "No overwriting summary; No changes made");
                                    }
                                    else Dbg.LogPart(rlx, "Overwriting Summary exists (no existing summary); No changes made (!! no existing?! : ISSUE !!)");
                                    Dbg.Log(rlx, "; ");
                                    Dbg.Log(rlx, $" -> Summary Overwriting Outcome: {info}; ");
                                }
                                else Dbg.Log(rlx, "No Summaries provided; Summaries overwriting End; ");

                                AddToInfoDock(info);
                            }
                            Dbg.NudgeIndent(rlx, false);
                        }


                        // OVERWRITING : Loose integration
                        Dbg.Log(rlx, $"Overwriting: Looose Content Integration :: {(integrationQueueRC != null ? "Proceed..." : "Skip")}");
                        if (integrationQueueRC != null)
                        {
                            bool integratedLooseQ = AddContent(false, integrationQueueRC);
                            Dbg.Log(rlx, $" -> Received loose ResCon: {integrationQueueRC}; Integration successful? {(integratedLooseQ ? "Yes" : "No")}; ");
                        }



                        // information send-off
                        Dbg.LogPart(rlx, "Compiling information docks (overwriting [");
                        if (rloInfoDock.HasElements())
                        {
                            resLibOverwriteInfoDock = rloInfoDock.ToArray();
                            Dbg.LogPart(rlx, $"{resLibOverwriteInfoDock.Length}");
                        }
                        else Dbg.LogPart(rlx, "0");
                        Dbg.LogPart(rlx, "], integration [");
                        if (rliInfoDock.HasElements())
                        {
                            looseIntegrationInfoDock = rliInfoDock.ToArray();
                            Dbg.LogPart(rlx, $"{rliInfoDock.ToArray().Length}");
                        }
                        else Dbg.LogPart(rlx, "0");
                        Dbg.Log(rlx, "]); ");


                        // LOCAL METHOD
                        void AddToInfoDock(params ResLibOverwriteInfo[] infos)
                        {
                            if (infos.HasElements())
                                foreach (ResLibOverwriteInfo info in infos)
                                {
                                    if (info.IsSetup())
                                        rloInfoDock.Add(info);
                                }
                        }
                    }
                    else Dbg.Log(rlx, $"Could not fetch library version details; Cancelling overwriting; ");
                }
                Dbg.EndLogging(rlx);
            }
        }
        public bool GetVersionRange(out VerNum lowest, out VerNum highest)
        {
            lowest = VerNum.None;
            highest = VerNum.None;
            if (IsSetup())
            {
                if (Summaries.HasElements())
                {
                    foreach (SummaryData sumDat in Summaries)
                    {
                        // setting lowest vernum
                        if (!lowest.HasValue())
                            lowest = sumDat.SummaryVersion;
                        else if (lowest.AsNumber > sumDat.SummaryVersion.AsNumber)
                            lowest = new VerNum(sumDat.SummaryVersion.MajorNumber, sumDat.SummaryVersion.MinorNumber);

                        // settting highest vernum
                        if (!highest.HasValue())
                            highest = sumDat.SummaryVersion;
                        else if (highest.AsNumber < sumDat.SummaryVersion.AsNumber)
                            highest = new VerNum(sumDat.SummaryVersion.MajorNumber, sumDat.SummaryVersion.MinorNumber);
                    }
                }
            }
            return lowest.HasValue() && highest.HasValue();
        }
        public void ClearLibrary()
        {
            Dbg.SingleLog("ResLibrary.ClearLibrary()", "ResLibrary's data has been cleared (reset)");
            _contents = new List<ResContents>();
            _legends = new List<LegendData>();
            _summaries = new List<SummaryData>();
        }
        public bool RevertToVersion(VerNum verReversion)
        {
            Dbg.StartLogging("ResLibrary.RevertToVersion(VerNum)", out int rlx);
            bool revertedQ = false;
            if (IsSetup())
            {
                if (verReversion.HasValue())
                {
                    List<ResContents> remainingContents = new();
                    List<LegendData> remainingLegends = new();
                    List<SummaryData> remainingSummaries = new();

                    // reversion time!
                    if (GetVersionRange(out _, out VerNum currVer))
                    {
                        Dbg.LogPart(rlx, $"Retrieved latest library version ({currVer}), and reversion version ({verReversion}); ");
                        if (currVer.AsNumber > verReversion.AsNumber)
                        {
                            Dbg.Log(rlx, "Fetching array of versions to unload; ");
                            Dbg.LogPart(rlx, "- Versions to unload ::");
                            List<VerNum> unloadableVers = new();
                            for (int cvx = currVer.AsNumber; cvx > verReversion.AsNumber; cvx--)
                            {
                                VerNum unloadableVerNum = new VerNum(cvx);
                                if (unloadableVerNum.HasValue())
                                {
                                    unloadableVers.Add(unloadableVerNum);
                                    Dbg.LogPart(rlx, $"  {unloadableVerNum.ToStringNums()}");
                                }
                            }
                            Dbg.Log(rlx, "; Proceeding to revert versions;");

                            ProgressBarInitialize();
                            ProgressBarUpdate(0);
                            TaskCount = unloadableVers.Count;


                            // version unloading loop here                            
                            for (int ulx = 0; ulx < unloadableVers.Count; ulx++)
                            {
                                VerNum verToUnload = unloadableVers[ulx];
                                Dbg.Log(rlx, $"Unloading -- Version {verToUnload} contents; ");

                                bool firstUnload = ulx == 0;
                                int countExempted = 0, countExempted2 = 0;
                                List<ResContents> contentsCopy = new();
                                List<LegendData> legendsCopy = new();
                                List<SummaryData> summariesCopy = new();

                                // INDENT MAJOR
                                Dbg.NudgeIndent(rlx, true);


                                // -- REMOVE CONTENTS --
                                Dbg.Log(rlx, "ResContents; ");
                                Dbg.NudgeIndent(rlx, true);
                                if (firstUnload)
                                    contentsCopy.AddRange(Contents.ToArray());
                                else
                                {
                                    contentsCopy.AddRange(remainingContents.ToArray());
                                    remainingContents.Clear();
                                }
                                /// resCon exemption loop
                                if (contentsCopy.HasElements())
                                {
                                    Dbg.Log(rlx, $"Copied [{contentsCopy.Count}] ResContents from {(firstUnload ? "main contents" : "remaining contents")}; ");
                                    foreach (ResContents resCon in contentsCopy)
                                    {
                                        if (resCon.IsSetup())
                                        {
                                            /// any additionals and updates are removed here
                                            if (!resCon.ConBase.VersionNum.Equals(verToUnload))
                                            {
                                                if (resCon.ConAddits.HasElements())
                                                {
                                                    int caRemovalAdjust = 0;
                                                    for (int rmvcax = 0; rmvcax <= resCon.ConAddits.Count; rmvcax++)
                                                    {
                                                        ContentAdditionals caToRemove = resCon.ConAddits[(rmvcax - caRemovalAdjust).Clamp(0, resCon.ConAddits.Count - 1)];
                                                        if (caToRemove.VersionAdded.Equals(verToUnload))
                                                            if (resCon.DisposeConAdditional(caToRemove))
                                                            {
                                                                Dbg.Log(rlx, $". Removed ConAddits :: {caToRemove} [from {resCon}]");
                                                                caRemovalAdjust--;
                                                                countExempted2++;
                                                            }
                                                    }
                                                }
                                                if (resCon.ConChanges.HasElements())
                                                {
                                                    int ccRemovalAdjust = 0;
                                                    for (int rmvccx = 0; rmvccx <= resCon.ConChanges.Count; rmvccx++)
                                                    {
                                                        ContentChanges ccToRemove = resCon.ConChanges[(rmvccx - ccRemovalAdjust).Clamp(0, resCon.ConChanges.Count - 1)];
                                                        if (ccToRemove.VersionChanged.Equals(verToUnload))
                                                            if (resCon.DisposeConChanges(ccToRemove))
                                                            {
                                                                Dbg.Log(rlx, $". Removed ConChanges :: {ccToRemove} [from {resCon}]");
                                                                ccRemovalAdjust--;
                                                                countExempted2++;
                                                            }
                                                    }
                                                }

                                                /// resCon is then added to remaing RCs
                                                remainingContents.Add(resCon);
                                            }
                                            /// the full resCon is removed here
                                            else
                                            {
                                                countExempted++;
                                                Dbg.Log(rlx, $". Exempted :: {resCon}");
                                            }
                                        }
                                    }
                                    Dbg.Log(rlx, $"[{countExempted}] ResContents (and [{countExempted2}] ConAddits/ConChanges) were removed, [{remainingContents.Count}] remain; ");
                                }
                                else Dbg.Log(rlx, "There are no ResContents to remove; ");
                                Dbg.NudgeIndent(rlx, false);


                                // -- REMOVE LEGENDS --
                                countExempted = 0;
                                Dbg.Log(rlx, "LegendDatas; ");
                                Dbg.NudgeIndent(rlx, true);
                                if (firstUnload)
                                    legendsCopy.AddRange(Legends.ToArray());
                                else
                                {
                                    legendsCopy.AddRange(remainingLegends.ToArray());
                                    remainingLegends.Clear();
                                }
                                /// legends exemption loop
                                if (legendsCopy.HasElements())
                                {
                                    Dbg.Log(rlx, $"Copied [{legendsCopy.Count}] Legend Datas from {(firstUnload ? "main contents" : "remaining contents")}; ");
                                    foreach (LegendData legDat in legendsCopy)
                                    {
                                        if (legDat.IsSetup())
                                        {
                                            if (!legDat.VersionIntroduced.Equals(verToUnload))
                                                remainingLegends.Add(legDat);
                                            else
                                            {
                                                countExempted++;
                                                Dbg.Log(rlx, $". Exempted Legd :: {legDat}");
                                            }
                                        }
                                    }
                                    Dbg.Log(rlx, $"[{countExempted}] Legend Datas were removed, [{remainingLegends.Count}] remain; ");
                                }
                                else Dbg.Log(rlx, "There are no Legend Datas to remove; ");
                                Dbg.NudgeIndent(rlx, false);


                                // -- REMOVE SUMMARIES --
                                countExempted2 = 0;
                                Dbg.Log(rlx, "SummaryDatas; ");
                                Dbg.NudgeIndent(rlx, true);
                                if (firstUnload)
                                    summariesCopy.AddRange(Summaries.ToArray());
                                else
                                {
                                    summariesCopy.AddRange(remainingSummaries.ToArray());
                                    remainingSummaries.Clear();
                                }
                                /// summaries exemption loop
                                if (summariesCopy.HasElements())
                                {
                                    Dbg.Log(rlx, $"Copied [{summariesCopy.Count}] Summary Datas from {(firstUnload ? "main contents" : "remaining contents")}; ");
                                    foreach (SummaryData sumDat in summariesCopy)
                                    {
                                        if (sumDat.IsSetup())
                                            if (!sumDat.SummaryVersion.Equals(verToUnload))
                                                remainingSummaries.Add(sumDat);
                                            else
                                            {
                                                countExempted2++;
                                                Dbg.Log(rlx, $". Exempted Smry :: {sumDat.ToStringShortened()}");    
                                            }
                                    }
                                    Dbg.Log(rlx, $"[{countExempted2}] Summaries Datas were removed, [{remainingSummaries.Count}] remain; ");
                                }
                                else Dbg.Log(rlx, "There are no Summary Datas to remove; ");
                                Dbg.NudgeIndent(rlx, false);


                                // UNINDENT MAJOR
                                Dbg.NudgeIndent(rlx, false);

                                TaskNum++;
                                ProgressBarUpdate(TaskNum / TaskCount, true);
                            }

                            revertedQ = true;
                        }
                        else Dbg.Log(rlx, "Version to revert to is ahead of or equal to current version; ");
                    }
                    else Dbg.Log(rlx, "Could not retrieve latest library version; ");


                    // if reversion successfull, set contents, legends, and summaries to remaining data
                    // else, no changes
                    if (revertedQ)
                    {
                        Dbg.LogPart(rlx, "Version reversion complete; Updating library with remaining contents");
                        /// contents
                        if (remainingContents.HasElements())
                            Contents = remainingContents;
                        else Contents.Clear();
                        /// legends
                        if (remainingLegends.HasElements())
                            Legends = remainingLegends;
                        else Legends.Clear();
                        /// summaries
                        if (remainingSummaries.HasElements())
                            Summaries = remainingSummaries;
                        else Summaries.Clear();

                        if (GetVersionRange(out _, out VerNum postRevertCurr))
                        {
                            revertedQ = postRevertCurr.Equals(verReversion);
                            Dbg.LogPart(rlx, $"; Verified? {revertedQ}");
                        }
                        Dbg.Log(rlx, "; ");
                    }
                }
                else Dbg.Log(rlx, "Received an invalid version to revert to; ");
            }
            else Dbg.Log(rlx, $"ResLibrary is not setup; ");
            Dbg.EndLogging(rlx);
            return revertedQ;
        }
        public SearchResult[] SearchLibrary(string searchArg, SearchOptions searchOpts, int maxResults = 99)
        {
            List<SearchResult> results = new();
            if (IsSetup() && maxResults > 0)
            {
                Dbg.StartLogging("ResLibrary.SearchLibrary()", out int rlx);
                Dbg.Log(rlx, $"Received search query; search Arg [{searchArg}]  --  search Opts [{searchOpts}]  --  max Results [{maxResults}]; ");

                /// IF recieved search arguement and search options: do a proper search query; ELSE select items at random
                if (searchArg.IsNotNEW() && searchOpts.IsSetup())
                {
                    /** Search query display changes to note
                        - Two types of searches alphanumeric, and numeric
                        - Alpha-numeric search display examples
                            ......................................
					        Search :: tomato 	(showing 6 results)
					
					        1 [Bse]	Tomato
					        2 [Adt]	"Tomato Stem Dust" (from 'Tomato')
					        3 [Upd]	"~tomato more delici~" (from 'Tomato')
					        4 [Bse]	Canned Tomato
					        5 [Bse]	Steamed Tomato				
					        6 [Upd]	"~tomato soggier loo~" (from 'Steamed Tomato')				
					        ......................................		
                            ......................................
					        Search :: t4		(showing 3 results)
					
					        1 [Bse]	t46 (from 'Garlic Clove')
					        2 [Bse]	t47 (from 'Garlic Clove')
					        3 [Adt]	t49 "Garlic Stem" (from 'Garlic Clove')
					        ......................................

                        - Numeric search display example
                            ......................................
					        Search :: 14		(showing 5 results)
					
					        1 [Bse]	1.14 (from 'Lettuce')
					        2 [Bse]	i214 (from 'Lettuce')
					        3 [Bse]	t114 (from 'Peanut')					
					        4 [Bse]	t14 (from 'Rice')
					        5 [Bse]	i14 (from 'Turnip')
					        ......................................	


                         - Result sorting and shaping explained
                            1st Alphabetic order, exact matching (in search 'cut', 'Cut' is exact, 'Cutter' or 'Hot Cut' is not exact)
                                . Category: Base, Additional, Update [matching word output determined by 'ContentName' match]
                                    1st Within Base sort (Content Name > Data IDs > Version Number) 
                                    2nd Within Additional sort (Opt Name > Data IDs > RelData ID* > Version Number) 
                                    3rd Within Updated sort (Change Description > RelData ID* > Version Number)
                            2nd Alphabetic order, partial matching 
                                . Category: Base, Additional, Update [matching word output determined by 'ContentName' match]
                                    1st Within Base sort (Content Name > Data IDs > Version Number) 
                                    2nd Within Additional sort (Opt Name > Data IDs > RelData ID* > Version Number) 
                                    3rd Within Updated sort (Change Description > RelData ID* > Version Number)
                            NOTE : RelData ID* is only checked when the Base Content Category is exempted from search options
                     */

                    Dbg.Log(rlx, "Search arguement and search options provided; proceeding with search query; ");
                    List<SearchResult> initialResultsAll = new(), irrelevantResults = new();
                    List<string> contentNames = new();

                    ProgressBarInitialize(true, false, 30, 4, 0);
                    ProgressBarUpdate(0);
                    TaskCount = Contents.Count + maxResults + 1;


                    // FIRST STEP - Find any results within results limit
                    Dbg.Log(rlx, "1st Step: Find all results; ");
                    Dbg.NudgeIndent(rlx, true);
                    for (int cx = 0; cx < Contents.Count; cx++)
                    {
                        // setup
                        const int updExcerptLim = 25;
                        const string updExcerptLimEnd = "~";
                        List<SearchResult> resultBatch = new();
                        ResContents content = Contents[cx];
                        bool getIdsQ = searchOpts.sourceContent == SourceContents.All || searchOpts.sourceContent == SourceContents.Ids;
                        bool getInfoQ = searchOpts.sourceContent == SourceContents.All || searchOpts.sourceContent == SourceContents.NId;
                        int dbgPrevBatchCount = 0, dbgCountBase = 0, dbgCountAdt = 0, dbgCountUpd = 0;
                        

                        // source - base content
                        if (searchOpts.IsUsingSource(SourceCategory.Bse))
                        {
                            /// base content name
                            bool? matchContentNameQ = CheckMatch(searchArg, content.ContentName, searchOpts.caseSensitiveQ);
                            if (matchContentNameQ.HasValue && getInfoQ)
                                resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchContentNameQ.Value, content.ContentName, content.ContentName, SourceCategory.Bse, content.ShelfID));

                            /// base content ids
                            for (int idx = 0; idx < content.ConBase.CountIDs && getIdsQ; idx++)
                            {
                                string id = content.ConBase[idx];
                                bool? matchIDQ = CheckMatch(searchArg, id, searchOpts.caseSensitiveQ);

                                if (matchIDQ.HasValue)
                                    resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchIDQ.Value, id, content.ContentName, SourceCategory.Bse, content.ShelfID));
                            }

                            /// base content version
                            bool? matchVersionQ = CheckMatch(searchArg, content.ConBase.VersionNum.ToStringNums(), searchOpts.caseSensitiveQ);
                            if (matchVersionQ.HasValue && getInfoQ)
                                resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchVersionQ.Value, content.ConBase.VersionNum.ToStringNums(), content.ContentName, SourceCategory.Bse, content.ShelfID));

                            dbgCountBase = resultBatch.Count;
                            dbgPrevBatchCount = resultBatch.Count;
                        }

                        // source - additional content
                        if (searchOpts.IsUsingSource(SourceCategory.Adt) && content.ConAddits.HasElements())
                        {
                            foreach (ContentAdditionals conAddits in content.ConAddits)
                            {
                                string additName = conAddits.OptionalName.IsNotNEW() ? $"\"{conAddits.OptionalName}\"" : "";

                                /// optional name
                                bool? matchOptName = CheckMatch(searchArg, conAddits.OptionalName, searchOpts.caseSensitiveQ);
                                if (matchOptName.HasValue && getInfoQ)
                                    resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchOptName.Value, additName, content.ContentName, SourceCategory.Adt, content.ShelfID));

                                /// data IDs
                                for (int idx = 0; idx < conAddits.CountIDs && getIdsQ; idx++)
                                {
                                    string id = conAddits[idx];
                                    bool? matchIdQ = CheckMatch(searchArg, id, searchOpts.caseSensitiveQ);
                                    if (matchIdQ.HasValue)
                                        resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchIdQ.Value, $"{id} {additName}".Trim(), content.ContentName, SourceCategory.Adt, content.ShelfID));
                                }

                                /// related data ID
                                if (!searchOpts.IsUsingSource(SourceCategory.Bse))
                                {
                                    bool? matchRelIdQ = CheckMatch(searchArg, conAddits.RelatedDataID, searchOpts.caseSensitiveQ);
                                    if (matchRelIdQ.HasValue && getIdsQ)
                                        resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchRelIdQ.Value, $"{conAddits.RelatedDataID} {additName}".Trim(), content.ContentName, SourceCategory.Adt, content.ShelfID));
                                }

                                /// version added number
                                bool? matchVerQ = CheckMatch(searchArg, conAddits.VersionAdded.ToStringNums(), searchOpts.caseSensitiveQ);
                                if (matchVerQ.HasValue && getInfoQ)
                                    resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchVerQ.Value, $"{conAddits.VersionAdded.ToStringNums()} {additName}".Trim(), content.ContentName, SourceCategory.Adt, content.ShelfID));
                            }

                            dbgCountAdt = resultBatch.Count - dbgPrevBatchCount;
                            dbgPrevBatchCount = resultBatch.Count;
                        }

                        // source - updated content
                        if (searchOpts.IsUsingSource(SourceCategory.Upd) && content.ConChanges.HasElements())
                        {
                            foreach (ContentChanges conChanges in content.ConChanges)
                            {
                                /// change description
                                bool? matchDescQ = CheckMatch(searchArg, conChanges.ChangeDesc, searchOpts.caseSensitiveQ);
                                if (matchDescQ.HasValue && getInfoQ)
                                {
                                    string searchArgMatch = LibrarySearch.HighlightSearchArg(searchArg, conChanges.ChangeDesc);
                                    string shortenedDesc;
                                    string shortenedDesc_Start = conChanges.ChangeDesc.Clamp(updExcerptLim, updExcerptLimEnd, searchArgMatch, true); 
                                    string shortenedDesc_End = conChanges.ChangeDesc.Clamp(updExcerptLim, updExcerptLimEnd, searchArgMatch, false);

                                    if (shortenedDesc_Start.Length > shortenedDesc_End.Length)
                                        shortenedDesc = $"\"~{shortenedDesc_Start}\"";
                                    else shortenedDesc = $"\"{shortenedDesc_End}~\"";

                                    resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchDescQ.Value, shortenedDesc, content.ContentName, SourceCategory.Upd, content.ShelfID));
                                }

                                /// related data id
                                if (!searchOpts.IsUsingSource(SourceCategory.Bse))
                                {
                                    bool? matchRelIdQ = CheckMatch(searchArg, conChanges.RelatedDataID, searchOpts.caseSensitiveQ);
                                    if (matchRelIdQ.HasValue && getIdsQ)
                                        resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchRelIdQ.Value, conChanges.RelatedDataID, content.ContentName, SourceCategory.Upd, content.ShelfID));
                                }

                                /// version changed number
                                bool? matchVerQ = CheckMatch(searchArg, conChanges.VersionChanged.ToStringNums(), searchOpts.caseSensitiveQ);
                                if (matchVerQ.HasValue && getInfoQ)
                                    resultBatch.Add(new SearchResult(searchArg, searchOpts.ToString(), matchVerQ.Value, conChanges.VersionChanged.ToStringNums(), content.ContentName, SourceCategory.Upd, content.ShelfID));
                            }

                            dbgCountUpd = resultBatch.Count - dbgPrevBatchCount;
                        }

                        // batch any results found for this content
                        if (resultBatch.HasElements())
                        {
                            int dbgDupeCount = 0;

                            /// for sorting
                            contentNames.Add(content.ContentName);
                            /// batch results
                            foreach (SearchResult result in resultBatch)
                            {
                                bool isDupeQ = false;
                                foreach (SearchResult iniRes in initialResultsAll)
                                {
                                    if (!isDupeQ && iniRes.Equals(result))
                                    {
                                        dbgDupeCount++;
                                        isDupeQ = true;
                                        break;
                                    }
                                }

                                if (!isDupeQ)
                                    initialResultsAll.Add(result);
                            }

                            Dbg.LogPart(rlx, $"RC #{content.ShelfID} '{content.ContentName}' provided '{resultBatch.Count}' results:");
                            Dbg.LogPart(rlx, $" {(dbgCountBase > 0 ? $"[Bse = {dbgCountBase}]" : "")}");
                            Dbg.LogPart(rlx, $" {(dbgCountAdt > 0 ? $"[Adt = {dbgCountAdt}]" : "")}");
                            Dbg.LogPart(rlx, $" {(dbgCountUpd > 0 ? $"[Upd = {dbgCountUpd}]" : "")}");
                            if (dbgDupeCount > 0)
                                Dbg.LogPart(rlx, $" -- Removed '{dbgDupeCount}' duplicate results");
                            Dbg.Log(rlx, "; ");
                        }

                        TaskNum++;
                        ProgressBarUpdate(TaskNum / TaskCount);
                    }
                    if (initialResultsAll.HasElements())
                    {
                        Dbg.LogPart(rlx, $" -> Shrinking initial results within max results limit '{maxResults}'; ");
                        for (int ix = 0; ix < initialResultsAll.Count && irrelevantResults.Count < maxResults; ix++)
                            irrelevantResults.Add(initialResultsAll[ix]);

                        Dbg.Log(rlx, $"{(irrelevantResults.Count <= maxResults ? "Done" : $"ERROR: improperly limited [{irrelevantResults.Count} <= {maxResults} : False]")}; ");
                    }
                    Dbg.NudgeIndent(rlx, false);


                    // SECOND STEP - Sort results by relevance and alphanumeric order
                    Dbg.Log(rlx, "2nd Step: relevance and alphanumeric sorting; ");
                    contentNames = contentNames.ToArray().SortWords();
                    List<SearchResult> exactResults = new(), priorityPartialResults = new(), partialResults = new();
                    if (contentNames.HasElements())
                    {
                        List<SearchResult> iniExactRes = new(), iniPriorityPartialRes = new(), iniPartialRes = new();

                        Dbg.Log(rlx, " -> Sorting all results into initial exacts, priority partials*, and partials; ");
                        Dbg.NudgeIndent(rlx, true);
                        for (int nx = 0; nx < contentNames.Count; nx++)
                        {
                            string contentName = contentNames[nx];
                            int dbgPrevExactCount = iniExactRes.Count, dbgPrevPartialCount = iniPartialRes.Count, dbgPrevPriorityCount = iniPriorityPartialRes.Count;
                            foreach (SearchResult iniResult in initialResultsAll)
                            {
                                if (iniResult.IsSetup() && iniResult.contentName == contentName)
                                {
                                    if (iniResult.exactMatchQ)
                                        iniExactRes.Add(iniResult);
                                    else
                                    {
                                        /// for example, 'Tomato' is exact and its additional "Tomato Stem" is partial, this partial result is prioritized over other partial results
                                        if (iniExactRes.Count - dbgPrevExactCount > 0)
                                            iniPriorityPartialRes.Add(iniResult); 
                                        else iniPartialRes.Add(iniResult);
                                    }
                                }
                            }

                            Dbg.LogPart(rlx, $"@{nx + 1} '{contentName}', Exct [{iniExactRes.Count - dbgPrevExactCount}], Prtl [{iniPriorityPartialRes.Count - dbgPrevPriorityCount}* + {iniPartialRes.Count - dbgPrevPartialCount}]");
                            if (nx % 3 == 2 || contentNames.Count == nx + 1)
                                Dbg.Log(rlx, "; ");
                            else Dbg.LogPart(rlx, "    //    ");
                        }
                        Dbg.NudgeIndent(rlx, false);

                        Dbg.Log(rlx, $" -> Limiting exacts [E] ('{iniExactRes.Count}' items), priority partials [R] ('{iniPriorityPartialRes.Count}' items), and partial [P] ('{iniPartialRes.Count}' items) results to cumulative total '{maxResults}'; ");
                        Dbg.NudgeIndent(rlx, true);
                        int resultTotal = 0;
                        bool addedResultQ = true;
                        Dbg.LogPart(rlx, "Adding ::");
                        for (int x = 0; resultTotal < maxResults && addedResultQ; x++)
                        {
                            int indexPriority = resultTotal - exactResults.Count;
                            int indexPartial = resultTotal - exactResults.Count - priorityPartialResults.Count;
                            Dbg.LogPart(rlx, $" [{resultTotal + 1}]");

                            if (resultTotal < iniExactRes.Count)
                            {
                                exactResults.Add(iniExactRes[resultTotal]);
                                Dbg.LogPart(rlx, $"E{resultTotal}");
                            }
                            else if (indexPriority < iniPriorityPartialRes.Count)
                            {
                                priorityPartialResults.Add(iniPriorityPartialRes[indexPriority]);
                                Dbg.LogPart(rlx, $"R{indexPriority}");
                            }
                            else if (indexPartial < iniPartialRes.Count)
                            {
                                partialResults.Add(iniPartialRes[indexPartial]);
                                Dbg.LogPart(rlx, $"P{indexPartial}");
                            }
                            else addedResultQ = false;

                            resultTotal = exactResults.Count + priorityPartialResults.Count + partialResults.Count;

                            TaskNum++;
                            ProgressBarUpdate(TaskNum / TaskCount);
                        }
                        Dbg.Log(rlx, ";  Done; ");
                        Dbg.NudgeIndent(rlx, false);
                    }
                    else Dbg.Log(rlx, "No initial results from 1st step. No results found.");


                    // THIRD STEP - Compile results in exact/partial relevance -UNLESS- ignoring relevance
                    if (contentNames.HasElements())
                    {
                        Dbg.Log(rlx, "3rd Step: compile results exact/partial -or- ignore relevance; ");
                        Dbg.LogPart(rlx, " -> ");
                        if (!searchOpts.ignoreRelevanceQ)
                        {
                            Dbg.LogPart(rlx, $"Relevance; Exact / Partial - submitting 'exact results' ['{exactResults.Count}' items] and 'partial results' ['{priorityPartialResults.Count}* + {partialResults.Count}' items] to 'results'");
                            if (exactResults.HasElements())
                                results.AddRange(exactResults);
                            if (priorityPartialResults.HasElements())
                                results.AddRange(priorityPartialResults);
                            if (partialResults.HasElements())
                                results.AddRange(partialResults);
                        }
                        else
                        {
                            Dbg.LogPart(rlx, $"Ignoring Relevance; Order By Shelf ID - submitting 'initial results' ['{irrelevantResults.Count}' items] to 'results'");
                            results.AddRange(irrelevantResults);
                        }
                        Dbg.Log(rlx, "; ");
                    }
                    ProgressBarUpdate(1, true);
                }
                else
                {
                    Dbg.Log(rlx, "No search arguement or search options available; selecting results at random; ");
                    Dbg.LogPart(rlx, $"Shelf IDs selected ('{maxResults}' items):");
                    List<int> randomInts = new();
                    string dbgSelectedItems = "";
                    for (int rx = 0; rx < maxResults && randomInts.Count < Contents.Count; rx++)
                    {
                        int timeOut = 25;
                        int randInt = Extensions.Random(0, Contents.Count - 1);
                        while (randomInts.Contains(randInt) && timeOut > 0)
                        {
                            randInt = Extensions.Random(0, Contents.Count - 1);
                            timeOut--;
                        }

                        Dbg.LogPart(rlx, $" {randInt}");
                        randomInts.Add(randInt);
                        ResContents randomContent = Contents[randInt];
                        results.Add(new SearchResult(Sep, Sep, false, randomContent.ContentName, randomContent.ContentName, SourceCategory.Bse, randomContent.ShelfID));
                        dbgSelectedItems += $" '{randomContent.ContentName.Replace(" ", "_")}'";
                    }
                    Dbg.Log(rlx, "; ");

                    Dbg.Log(rlx, $"Selected Items: ");
                    Dbg.NudgeIndent(rlx, true);
                    Dbg.Log(rlx, $"{dbgSelectedItems.Trim().Replace(" ", ", ")}");
                    Dbg.NudgeIndent(rlx, false);
                }
                Dbg.EndLogging(rlx);
            }

            return results.ToArray();
        }
        /// <summary>Generates a copy of the current instance.</summary>
        public ResLibrary CloneLibrary()
        {
            ResLibrary clone = null;
            Dbg.StartLogging("ResLibrary.CloneLibrary()", out int rlx);
            Dbg.Log(rlx, $"Cloning current ResLibrary instance; Instance is setup? {IsSetup()} [#:{GetHashCode()}]; ");
            if (IsSetup())
            {
                Dbg.LogPart(rlx, ">> Cloning  ::  ");
                clone = new ResLibrary();

                Dbg.LogPart(rlx, $"Contents [{Contents.Count}] / ");
                foreach (ResContents rc in Contents)
                    clone.AddContent(rc.CloneResContent());

                Dbg.LogPart(rlx, $"Legends [{Legends.Count}] / ");
                foreach (LegendData lg in Legends)
                    clone.AddLegend(lg.CloneLegend());

                Dbg.LogPart(rlx, $"Summaries [{Summaries.Count}]");
                foreach (SummaryData sm in Summaries)
                    clone.AddSummary(sm.CloneSummary());

                Dbg.Log(rlx, $"  //  Clone ResLibrary instance returned [#:{clone.GetHashCode()}]; ");
            }
            else Dbg.Log(rlx, "Cloning cancelled; NULL instance returned [#:--]; ");
            Dbg.EndLogging(rlx);

            return clone;
        }
        /// <summary>Fetches the contents, legends, and summaries introduced in a given <paramref name="version"/>. The returned instance may not have elements for all collections.</summary>
        /// <param name="getUsedLegendQ">If <c>true</c>, will get all legends that are used within the given version. Otherwise, only legends that were introduced in given version.</param>
        public ResLibrary GetVersion(VerNum version, bool getUsedLegendQ = true)
        {
            ResLibrary verLogDetails = new();
            List<string> allDataIDs = new();
            
            if (IsSetup())
            {
                for (int rdx = 0; rdx < 3; rdx++)
                {
                    switch (rdx)
                    {
                        // contents - get matching ver log number
                        case 0:
                            List<ResContents> resContents = new();
                            List<string> looseDataIDs = new();
                            List<ContentAdditionals> looseConAddits = new();
                            List<ContentChanges> looseConChanges = new();

                            /// filtering occurs here
                            foreach (ResContents resCon in Contents)
                            {
                                ResContents clone = null;
                                if (resCon != null)
                                    if (resCon.IsSetup())
                                    {
                                        bool fetchedConBaseQ = false;

                                        // ConBase
                                        if (resCon.ConBase.VersionNum.Equals(version))
                                        {
                                            fetchedConBaseQ = true;
                                            clone = new ResContents(resCon.ShelfID, resCon.ConBase);
                                            allDataIDs.AddRange(resCon.ConBase.DataIDString.Split(' '));

                                            /// ConAddits (same ver)
                                            if (resCon.ConAddits.HasElements())
                                            {
                                                foreach (ContentAdditionals rca in resCon.ConAddits)
                                                    if (rca.VersionAdded.Equals(version))
                                                    {
                                                        ContentAdditionals rcaClone = rca.Clone();
                                                        rcaClone.ContentName = clone.ContentName;

                                                        clone.StoreConAdditional(rcaClone);
                                                        allDataIDs.AddRange(rca.DataIDString.Split(' '));
                                                    }
                                            }

                                            /// ConChanges (same ver)
                                            if (resCon.ConChanges.HasElements())
                                            {
                                                foreach (ContentChanges rcc in resCon.ConChanges)
                                                    if (rcc.VersionChanged.Equals(version))
                                                    {
                                                        clone.StoreConChanges(rcc.Clone());
                                                        allDataIDs.Add(rcc.RelatedDataID);
                                                    }
                                            }

                                            if (clone.IsSetup())
                                                resContents.Add(clone);
                                        }

                                        // ConAddits (loose)
                                        if (!fetchedConBaseQ)
                                        {
                                            if (resCon.ConAddits.HasElements())
                                                foreach (ContentAdditionals ca in resCon.ConAddits)
                                                    if (ca.VersionAdded.Equals(version))
                                                    {
                                                        looseDataIDs.Add(ca.RelatedDataID);
                                                        ContentAdditionals caClone = ca.Clone();
                                                        caClone.ContentName = resCon.ContentName;
                                                        looseConAddits.Add(caClone);

                                                        allDataIDs.Add(ca.RelatedDataID);
                                                        allDataIDs.AddRange(ca.DataIDString.Split(' '));
                                                    }
                                        }

                                        // ConChanges (loose)
                                        if (!fetchedConBaseQ)
                                        {
                                            if (resCon.ConChanges.HasElements())
                                                foreach (ContentChanges cc in resCon.ConChanges)
                                                    if (cc.VersionChanged.Equals(version))
                                                    {
                                                        looseDataIDs.Add(cc.RelatedDataID);
                                                        looseConChanges.Add(cc.Clone());

                                                        allDataIDs.Add(cc.RelatedDataID);
                                                    }
                                        }
                                    }
                            }

                            /// load into instance
                            ResContents looseResCon = new(0, new ContentBaseGroup(version, LooseResConName, looseDataIDs.ToArray()));
                            foreach (ContentAdditionals lca in looseConAddits)
                                looseResCon.StoreConAdditional(lca);
                            foreach (ContentChanges lcc in looseConChanges)
                                looseResCon.StoreConChanges(lcc);

                            if (looseResCon.IsSetup())
                                resContents.Insert(0, looseResCon);
                            verLogDetails.Contents = new();
                            verLogDetails.Contents.AddRange(resContents.ToArray());
                            break;


                        // legends - get of matching ver log number
                        case 1:
                            /// fetch used legend keys
                            string legendKeysStr = " ";
                            if (allDataIDs.HasElements())
                            {
                                /// for the range indicator in specific                                
                                string condensedAllDataIDsStr = Extensions.CreateNumericDataIDRanges(allDataIDs.ToArray());
                                if (condensedAllDataIDsStr.IsNotNEW())
                                {
                                    if (condensedAllDataIDsStr.Contains("~"))
                                        legendKeysStr += $"~ ";
                                }

                                foreach (string dataID in allDataIDs)
                                {
                                    LogDecoder.DisassembleDataID(dataID, out string dk, out _, out string sfx);
                                    if (!legendKeysStr.Contains($" {dk} "))
                                        legendKeysStr += $"{dk} ";
                                    if (!legendKeysStr.Contains($" {sfx} "))
                                        legendKeysStr += $"{sfx} ";
                                }


                            }
                            /// fetch legend datas
                            verLogDetails.Legends = new();
                            for (int legx = 0; legx < Legends.Count; legx++)
                            {
                                if (Legends[legx].IsSetup())
                                {
                                    /// IF get used legends: legend data that have been used; ELSE get legend data introduced in version;
                                    if (getUsedLegendQ)
                                    {
                                        if (legendKeysStr.Contains($" {Legends[legx].Key} "))
                                            verLogDetails.Legends.Add(Legends[legx]);
                                    }
                                    else
                                    {
                                        if (Legends[legx].VersionIntroduced.Equals(version))
                                            verLogDetails.Legends.Add(Legends[legx]);
                                    }
                                }                                
                            }                                                        
                            break;


                        // summaries - get of matching ver log number
                        case 2:
                            bool fetchedSummaryQ = false;
                            verLogDetails.Summaries = new();
                            for (int sumx = 0; !fetchedSummaryQ && sumx < Summaries.Count; sumx++)
                            {
                                if (Summaries[sumx].IsSetup())
                                    if (Summaries[sumx].SummaryVersion.Equals(version))
                                    {
                                        fetchedSummaryQ = true;
                                        verLogDetails.Summaries.Add(Summaries[sumx]);
                                    }
                            }
                            break;
                    }
                }
            }

            return verLogDetails;
        }


        /// -
        // OTHER MISCELLANEOUS METHODS
        /// <summary>Has this instance of <see cref="ResLibrary"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the contents, legends, and summaries have elements within them, at minimum.</returns>
        public override bool IsSetup()
        {
            return Contents.HasElements() && Legends.HasElements() && Summaries.HasElements();
        }
        /// <summary>Compares two instances for similarities against: Setup state, Contents, Legends, Summaries.</summary>
        public bool Equals(ResLibrary resLib)
        {
            bool areEquals = false;
            if (resLib != null)
            {
                areEquals = true;
                for (int rlx = 0; rlx < 4 && areEquals; rlx++)
                {
                    switch (rlx)
                    {
                        case 0:
                            areEquals = IsSetup() == resLib.IsSetup();
                            break;

                        case 1:
                            if (IsSetup())
                            {
                                areEquals = Contents.Count == resLib.Contents.Count;
                                if (areEquals)
                                {
                                    for (int rlcx = 0; rlcx < Contents.Count && areEquals; rlcx++)
                                        areEquals = Contents[rlcx].Equals(resLib.Contents[rlcx]);
                                }
                            }                            
                            break;

                        case 2:
                            if (IsSetup())
                            {
                                areEquals = Legends.Count == resLib.Legends.Count;
                                if (areEquals)
                                {
                                    for (int rllx = 0; rllx < Legends.Count && areEquals; rllx++)
                                        areEquals = Legends[rllx].Equals(resLib.Legends[rllx]);
                                }
                            }
                            break;

                        case 3:
                            if (IsSetup())
                            {
                                areEquals = Summaries.Count == resLib.Summaries.Count;
                                if (areEquals)
                                {
                                    for (int rlsx = 0; rlsx < Summaries.Count && areEquals; rlsx++)
                                        areEquals = Summaries[rlsx].Equals(resLib.Summaries[rlsx]);
                                }
                            }
                            break;
                    }
                }
            }
            return areEquals;
        }
        public override bool ChangesMade()
        {
            return !Equals(GetPreviousSelf());
        }
        void SetPreviousSelf()
        {
            _prevContents = null;
            _prevLegends = null;
            _prevSummaries = null;
            if (_contents != null)
            {
                _prevContents = new List<ResContents>();
                _prevContents.AddRange(_contents.ToArray());
            }
            if (_legends != null)
            {
                _prevLegends = new List<LegendData>();
                _prevLegends.AddRange(_legends.ToArray());
            }
            if (_summaries != null)
            {
                _prevSummaries = new List<SummaryData>();
                _prevSummaries.AddRange(_summaries.ToArray());
            }
        }
        ResLibrary GetPreviousSelf()
        {
            ResLibrary prevSelf = new();

            if (_prevContents != null)
                prevSelf.AddContent(_prevContents.ToArray());
            if (_prevLegends != null)
                prevSelf.AddLegend(_prevLegends.ToArray());
            if (_prevSummaries != null)
                prevSelf.AddSummary(_prevSummaries.ToArray());
            
            return prevSelf;
        }
        /// <summary>For <see cref="SearchLibrary(string, SearchOptions, int)"/>; Checks for any matches of <paramref name="arg"/> in <paramref name="text"/>.</summary>
        /// <param name="arg">The (search) argument.</param>
        /// <param name="text">The text to check for any matches against <paramref name="arg"/>.</param>
        /// <param name="caseSensitiveQ">Whether to make a case-sensitive check.</param>
        /// <returns>A nullable boolean defining the results of the check: <c>TRUE</c> - exact match, <c>FALSE</c> - partial match, <c>NULL</c> - no matches.</returns>
        bool? CheckMatch(string arg, string text, bool caseSensitiveQ)
        {
            /// null (no match at all)  /  false (partial match)  /  true (exact match)
            bool? matchingQ = null;
            if (arg.IsNotNEW() && text.IsNotNEW())
            {
                if (text.ToLower().Contains(arg.ToLower()))
                {
                    // exact: true & true = TRUE    partial: false & true = FALSE       no match: false & false = FALSE => NULL
                    bool exactQ, partialQ;
                    if (caseSensitiveQ)
                    {
                        exactQ = text.Equals(arg);
                        partialQ = text.Contains(arg);
                        if (exactQ || partialQ)
                            matchingQ = exactQ && partialQ;
                    }
                    else
                    {
                        exactQ = text.ToLower().Equals(arg.ToLower());
                        partialQ = text.ToLower().Contains(arg.ToLower());
                        if (exactQ || partialQ)
                            matchingQ = exactQ && partialQ;
                    }
                }
            }
            return matchingQ;
        }


        /// -
        // DATA HANDLING METHODS
        protected override bool EncodeToSharedFile()
        {
            Dbg.StartLogging("ResLibrary.EncodeToSharedFile()", out int rlx);
            Dbg.ToggleThreadOutputOmission(rlx);
            bool encodedQ = true;
            
            if (IsSetup())
            {
                Dbg.LogPart(rlx, "ResLibrary is setup, creating stamp: ");
                bool noIssue = true;
                // 0th verify saving of data
                if (Base.FileWrite(false, stampRLSavedDataTag, stampRLSavedData))
                    Dbg.LogPart(rlx, "[STAMPED]; ");
                else Dbg.LogPart(rlx, "[ ? ? ? ] (no stamp); ");
                Dbg.Log(rlx, "Proceeding with encoding; ");

                // 1st encode contents
                Dbg.Log(rlx, $"Encoding [{Contents.Count}] ResContents; ");
                for (int rcix = 0; rcix < Contents.Count && noIssue; rcix++)
                {
                    ResContents resCon = Contents[rcix];
                    Dbg.LogPart(rlx, $"+ Encoding :: {resCon}");

                    if (resCon.IsSetup())
                    {
                        noIssue = Base.FileWrite(false, resCon.ShelfID.ToString(), resCon.EncodeGroups());
                        Dbg.Log(rlx, $"{Base.ConditionalText(noIssue, "", "[!issue]")}; ");

                        if (noIssue)
                        {
                            Dbg.NudgeIndent(rlx, true);
                            foreach (string rcLine in resCon.EncodeGroups())
                                if (rcLine.IsNotNE())
                                    Dbg.Log(rlx, $"tag [{resCon.ShelfID}]| {rcLine}");
                            Dbg.NudgeIndent(rlx, false);
                        }
                    }
                    else Dbg.Log(rlx, "; Skipped -- Was not setup; ");
                }

                // 2nd encode legends
                if (noIssue)
                {
                    Dbg.Log(rlx, $"Encoding [{Legends.Count}] Legend Datas; ");
                    List<string> resLibLegendsLines = new();

                    Dbg.NudgeIndent(rlx, true);
                    foreach (LegendData legDat in Legends)
                    {
                        if (legDat.IsSetup())
                        {
                            resLibLegendsLines.Add(legDat.Encode());
                            Dbg.Log(rlx, $"{legDat.Encode()}");
                        }
                        else Dbg.Log(rlx, $"Skipped ({legDat}) -- was not setup; ");
                    }
                    Dbg.NudgeIndent(rlx, false);

                    noIssue = Base.FileWrite(false, legDataTag, resLibLegendsLines.ToArray());
                    Dbg.Log(rlx, $"{Base.ConditionalText(noIssue, $"Successfully encoded legend datas (with tag '{legDataTag}')", "Failed to encode legend datas")};");
                }

                // 3rd encode summaries
                if (noIssue)
                {
                    Dbg.Log(rlx, $"Encoding [{Summaries.Count}] Summary Datas; ");
                    List<string> resLibSummaryLines = new();

                    Dbg.NudgeIndent(rlx, true);
                    foreach (SummaryData sumDat in Summaries)
                    {
                        if (sumDat.IsSetup())
                        {
                            resLibSummaryLines.Add(sumDat.Encode());
                            Dbg.Log(rlx, $"{sumDat.Encode()}");
                        }
                        else Dbg.Log(rlx, $"Skipped ({sumDat.ToStringShortened()}) -- was not setup; ");
                    }
                    Dbg.NudgeIndent(rlx, false);

                    noIssue = Base.FileWrite(false, sumDataTag, resLibSummaryLines.ToArray());
                    Dbg.Log(rlx, $"{Base.ConditionalText(noIssue, $"Successfully encoded summary datas (with tag '{sumDataTag}')", "Failed to encode summary datas")};");
                }

                Dbg.Log(rlx, $"Encoded ResLibrary? {noIssue}");
                encodedQ = noIssue;
            }
            else Dbg.Log(rlx, "Not enough data within ResLibrary to proceed with encoding; ");

            if (encodedQ)
                SetPreviousSelf();

            Dbg.EndLogging(rlx);
            return encodedQ;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbg.StartLogging("ResLibrary.DecodeFromSharedFile()", out int rlx);
            Dbg.ToggleThreadOutputOmission(rlx);
            bool noDataButIsOkay = true;
            if (Base.FileRead(stampRLSavedDataTag, out string[] hasDataLine))
            {
                if (hasDataLine.HasElements())
                    noDataButIsOkay = false;
            }

            /// decode if there is data
            if (!noDataButIsOkay)
            {
                bool expandDecodeDebug = false, showDecodedLine = false;

                // contents decode
                if (!Contents.HasElements())
                { /// wrapping
                    Dbg.Log(rlx, "Decoding ResContents; ");
                    Contents = new List<ResContents>();

                    /// if true, will show the full result of decoding the ResCon instance (detailed result output)
                    const int noDataTimeout = 25;                    
                    int lastFetchedIx = -1;
                    int shelfNum = 0;
                    Dbg.NudgeIndent(rlx, true);
                    for (int lx = 0; lx - noDataTimeout <= lastFetchedIx; lx++)
                    {
                        if (Base.FileRead(lx.ToString(), out string[] rawRCData))
                        {
                            if (rawRCData.HasElements())
                            {
                                if (lastFetchedIx + 1 < lx)
                                    Dbg.Log(rlx, "; Timeout restart; ");

                                bool dbugGroupCondition = showDecodedLine || expandDecodeDebug;
                                if (dbugGroupCondition)
                                {
                                    Dbg.Log(rlx, $"Data with tag [{lx}] retrieved; ");
                                    Dbg.NudgeIndent(rlx, true);
                                }
                               
                                for (int rclx = 0; rclx < rawRCData.Length && showDecodedLine; rclx++)
                                    Dbg.Log(rlx, $"L{rclx + 1}| {rawRCData[rclx]}");

                                string[] resConData = new string[3]
                                {
                                    rawRCData[0],
                                    rawRCData.HasElements(2) ? (rawRCData[1].IsNEW() ? null : rawRCData[1]) : null,
                                    rawRCData.HasElements(3) ? (rawRCData[2].IsNEW() ? null : rawRCData[2]) : null
                                };
                                ResContents decodedRC = new();
                                if (decodedRC.DecodeGroups(shelfNum, resConData))
                                {
                                    Contents.Add(decodedRC);
                                    Dbg.Log(rlx, $"Decoded ResCon instance :: {decodedRC}; ");
                                    shelfNum++;
                                    lastFetchedIx = lx;

                                    if (expandDecodeDebug)
                                    {
                                        Dbg.NudgeIndent(rlx, true);
                                        Dbg.Log(rlx, $">> {decodedRC.ConBase}");
                                        if (decodedRC.ConAddits.HasElements())
                                        {
                                            Dbg.LogPart(rlx, ">> ");
                                            for (int xca = 0; xca < decodedRC.ConAddits.Count; xca++)
                                                Dbg.LogPart(rlx, $"{decodedRC.ConAddits[xca]}{(xca + 1 < decodedRC.ConAddits.Count ? "  //  " : "")}");
                                            Dbg.Log(rlx, "..");
                                        }
                                        if (decodedRC.ConChanges.HasElements())
                                        {
                                            Dbg.LogPart(rlx, ">> ");
                                            for (int xcc = 0; xcc < decodedRC.ConChanges.Count; xcc++)
                                                Dbg.LogPart(rlx, $"{decodedRC.ConChanges[xcc]}{(xcc + 1 < decodedRC.ConChanges.Count ? "  //  " : "")}");
                                            Dbg.Log(rlx, "  ..");
                                        }
                                        Dbg.NudgeIndent(rlx, false);
                                    }
                                }
                                else Dbg.Log(rlx, "ResCon instance could not be decoded; ");
                                if (dbugGroupCondition) 
                                    Dbg.NudgeIndent(rlx, false);
                            }
                            else
                            {
                                if (lastFetchedIx + 1 == lx || lastFetchedIx == lx)
                                    Dbg.LogPart(rlx, $"No data retrieved; Timing out: {noDataTimeout - (lx - lastFetchedIx)}");
                                else Dbg.LogPart(rlx, $" {noDataTimeout - (lx - lastFetchedIx)}");
                            }
                        }
                    }
                    Dbg.Log(rlx, "; Timeout end; ");
                    Dbg.NudgeIndent(rlx, false);
                }

                // legends decode
                if (!Legends.HasElements())
                { /// wrapping
                    Dbg.LogPart(rlx, "Decoding Legend Data; ");
                    Legends = new List<LegendData>();

                    if (Base.FileRead(legDataTag, out string[] legendsData))
                    {
                        if (legendsData.HasElements())
                        {
                            int countLine = 1;
                            Dbg.Log(rlx, $"Fetched [{legendsData.Length}] lines of legend data; ");
                            foreach (string legData in legendsData)
                            {
                                if (expandDecodeDebug)
                                    Dbg.Log(rlx, $"L{countLine}| {legData}");

                                Dbg.NudgeIndent(rlx, true);
                                LegendData decodedLegd = new();
                                if (decodedLegd.Decode(legData))
                                {
                                    decodedLegd.AdoptIndex(Legends.Count);
                                    Legends.Add(decodedLegd);
                                    Dbg.Log(rlx, $"Decoded Legend :: {decodedLegd}; ");
                                }
                                else Dbg.Log(rlx, $"Legend Data could not be decoded{(!expandDecodeDebug ? $" :: source ({legData})" : "")};");
                                Dbg.NudgeIndent(rlx, false);
                                countLine++;
                            }
                        }
                        else Dbg.Log(rlx, "Recieved no legend data; ");
                    }
                    else Dbg.Log(rlx, $"Could not read from file; Issue :: {Tools.GetRecentWarnError(false, false)}");
                }

                // summaries decode
                if (!Summaries.HasElements())
                { /// wrapping
                    Dbg.LogPart(rlx, "Decoding Summary Data; ");
                    Summaries = new List<SummaryData>();

                    if (Base.FileRead(sumDataTag, out string[] summariesData))
                    {
                        if (summariesData.HasElements())
                        {
                            Dbg.Log(rlx, $"Fetched [{summariesData.Length}] lines of summary data;");
                            int countLine = 1;
                            foreach (string sumData in summariesData)
                            {
                                if (expandDecodeDebug)
                                    Dbg.Log(rlx, $"L{countLine}| {sumData}");
                                
                                Dbg.NudgeIndent(rlx, true);
                                SummaryData decodedSmry = new();
                                if (decodedSmry.Decode(sumData))
                                {
                                    decodedSmry.AdoptIndex(Summaries.Count);
                                    Summaries.Add(decodedSmry);
                                    Dbg.Log(rlx, $"Decoded Summary :: {decodedSmry.ToStringShortened()}; ");
                                }
                                else Dbg.Log(rlx, $"Summary Data could not be decoded{(!expandDecodeDebug ? $" :: source ({sumData})" : "")}; ");
                                Dbg.NudgeIndent(rlx, false);

                                countLine++;
                            }
                        }
                        else Dbg.Log(rlx, "Recieved no summary data; ");
                    }
                    else Dbg.Log(rlx, $"Could not read from file; Issue :: {Tools.GetRecentWarnError(false, false)}");
                }
            }      
            else Dbg.Log(rlx, "ResLibrary has no saved data to decode; ");
            Dbg.EndLogging(rlx);

            SetPreviousSelf();
            return IsSetup() || noDataButIsOkay;
        }
        #endregion
    }
}
