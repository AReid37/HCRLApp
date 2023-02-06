﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ConsoleFormat;

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
        const string legDataTag = "lgd";
        const string sumDataTag = "sum";
        const string stampRLSavedData = "ResLibrary Has Saved Data", stampRLSavedDataTag = "RLDS"; 
        static bool disableAddDbug = false;
        List<ResContents> _contents, _prevContents;
        List<LegendData> _legends, _prevLegends;
        List<SummaryData> _summaries, _prevSummaries;
        List<ResLibIntegrationInfo> rliInfoDock;

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
        public bool AddContent(bool keepLooseRCQ, params ResContents[] newContents)
        {
            bool addedContentsQ = false;
            if (newContents.HasElements())
            {
                if (disableAddDbug)
                    Dbug.DeactivateNextLogSession();
                Dbug.StartLogging("ResLibrary.AddContent(prms RC[])");
                Dbug.LogPart($"Recieved [{newContents.Length}] new ResCons for library; Refresh integration info dock? {keepLooseRCQ}");

                if (!keepLooseRCQ)
                    rliInfoDock = new List<ResLibIntegrationInfo>();

                List<int> shelfNums = new();
                if (Contents.HasElements())
                {
                    Dbug.LogPart("; Fetching existing shelf numbers");
                    //Dbug.LogPart("; Fetching existing shelf numbers ::");
                    foreach (ResContents resCon in Contents)
                    {
                        shelfNums.Add(resCon.ShelfID);
                        //Dbug.LogPart($" {resCon.ShelfID}");
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
                            /// IF existing contents and able to sort loose contents: sort loose contents; 
                            if (Contents.HasElements() && !keepLooseRCQ)
                            {
                                Dbug.Log("Library has pre-existing contents that may serve as connections; ");
                                ResLibIntegrationInfo rliInfo = new();
                                RCFetchSource rliType = RCFetchSource.None;

                                /// find matching and connect ConAddits
                                List<string> RCextras = new();
                                if (newRC.ConAddits.HasElements())
                                {
                                    rliType = RCFetchSource.ConAdditionals;
                                    Dbug.Log("Making connections :: ConAddits to ConBase");
                                    Dbug.NudgeIndent(true);
                                    foreach (ContentAdditionals looseCa in newRC.ConAddits)
                                    {
                                        if (looseCa.IsSetup())
                                        {
                                            Dbug.LogPart($"Connecting ConAddits ({looseCa}) >> ");
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
                                                        Dbug.LogPart("[by trueID] ");
                                                        break;
                                                    }
                                                }
                                            }

                                            /// connection here
                                            if (matchingResCon != null)
                                            {
                                                Dbug.LogPart($"Found ResCon {{{matchingResCon}}} >> ");
                                                if (matchingResCon.StoreConAdditional(looseCa))
                                                {
                                                    RCextras.Add(matchingResCon.ToString());
                                                    Dbug.LogPart($"Connected with ConBase ({matchingResCon.ConBase}) [by ID '{looseCa.RelatedDataID}']");

                                                    rliInfo = new ResLibIntegrationInfo(rliType, looseCa.DataIDString, looseCa.OptionalName, matchingResCon.ContentName, looseCa.RelatedDataID);
                                                }
                                                else Dbug.LogPart($"Rejected ConAddits");
                                            }
                                            else Dbug.LogPart("No ConBase connections found (discarded)");

                                            if (!rliInfo.IsSetup())
                                                rliInfo = new ResLibIntegrationInfo(rliType, looseCa.DataIDString, looseCa.OptionalName, null, null);
                                            Dbug.LogPart(" <rlii> "); /// this logs regardless of what happens
                                        }
                                        Dbug.Log(";");

                                        /// add ConAddits info to dock
                                        if (rliInfo.IsSetup())
                                        {
                                            rliInfoDock.Add(rliInfo);
                                            rliInfo = new ResLibIntegrationInfo();
                                        }
                                    }
                                    Dbug.NudgeIndent(false);

                                    if (RCextras.HasElements())
                                        foreach(string rcEdit in RCextras)
                                            Dbug.Log($". Edited RC :: {rcEdit}");
                                }

                                /// find matching and connect ConChanges
                                RCextras.Clear();
                                if (newRC.ConChanges.HasElements())
                                {
                                    rliType = RCFetchSource.ConChanges;
                                    Dbug.Log("Making connections :: ConChanges to ConBase/ConAddits");
                                    Dbug.NudgeIndent(true);
                                    foreach (ContentChanges looseCc in newRC.ConChanges)
                                    {
                                        if (looseCc.IsSetup())
                                        {
                                            Dbug.LogPart($"Connecting ConChanges ({looseCc.ToStringShortened()}) >> ");
                                            ResContents matchingResCon = null;
                                            rliInfo = new ResLibIntegrationInfo();

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
                                                    Dbug.LogPart("[by trueID] ");
                                                    break;
                                                }
                                            }

                                            /// connection here
                                            if (matchingResCon != null)
                                            {
                                                Dbug.LogPart($"Found ResCon {{{matchingResCon}}} >> ");
                                                if (matchingResCon.ContainsDataID(looseCc.RelatedDataID, out RCFetchSource source, out DataHandlerBase dataSource))
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
                                                                Dbug.LogPart($"Connected with ConBase ({matchCbg}) [by ID '{looseCc.RelatedDataID}']");
                                                                connectCCq = true;
                                                            }
                                                            else Dbug.LogPart($"Rejected ConChanges");
                                                        }
                                                    }
                                                    else if (source == RCFetchSource.ConAdditionals)
                                                    {
                                                        ContentAdditionals matchCa = (ContentAdditionals)dataSource;
                                                        if (matchCa != null)
                                                        {
                                                            if (matchingResCon.StoreConChanges(looseCc))
                                                            {
                                                                RCextras.Add(matchingResCon.ToString());
                                                                Dbug.LogPart($"Connected with ConAddits ({matchCa}) [by ID '{looseCc.RelatedDataID}']");
                                                                connectCCq = true;
                                                            }
                                                            else Dbug.LogPart($"Rejected ConChanges");
                                                        }
                                                    }

                                                    if (connectCCq)
                                                        rliInfo = new ResLibIntegrationInfo(rliType, looseCc.RelatedDataID, looseCc.ChangeDesc, matchingResCon.ContentName, looseCc.RelatedDataID);
                                                }
                                            }
                                            else Dbug.LogPart("No connections found (discarded)");

                                            if (!rliInfo.IsSetup())
                                                rliInfo = new ResLibIntegrationInfo(rliType, looseCc.RelatedDataID, looseCc.ChangeDesc, null, null);
                                            Dbug.LogPart(" <rlii> "); /// this logs regardless of what happens
                                        }
                                        Dbug.Log("; ");

                                        /// add ConChanges info to dock
                                        if (rliInfo.IsSetup())
                                        {
                                            rliInfoDock.Add(rliInfo);
                                            rliInfo = new ResLibIntegrationInfo();
                                        }
                                    }
                                    Dbug.NudgeIndent(false);

                                    if (RCextras.HasElements())
                                        foreach (string rcEdits in RCextras)
                                            Dbug.Log($". Edited RC :: {rcEdits}");
                                }
                            }
                            /// ELSE don't sort loose contents
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
                        }

                        // just add
                        else
                        {
                            if (!IsDuplicateInLibrary(newRC))
                            {
                                /// get shelf id
                                newRC.ShelfID = GetNewShelfNum();
                                shelfNums.Add(newRC.ShelfID);

                                /// add to content library
                                Contents.Add(newRC);
                                Dbug.Log($"Added :: {newRC}");
                            }
                            else Dbug.Log($"Rejected duplicate :: {newRC}");
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
        bool IsDuplicateInLibrary(ResContents resCon)
        {
            bool foundDupe = false;
            if (Contents.HasElements() && resCon != null)
            {
                if (resCon.IsSetup())
                {
                    for (int x = 0; x < Contents.Count && !foundDupe; x++)
                        foundDupe = Contents[x].Equals(resCon);
                }
            }
            return foundDupe;
        }        
        public bool AddLegend(params LegendData[] newLegends)
        {
            bool addedLegendQ = false;
            if (newLegends.HasElements())
            {
                if (disableAddDbug)
                    Dbug.DeactivateNextLogSession();
                Dbug.StartLogging("ResLibrary.AddLegend(prms LegData[])");
                foreach (LegendData leg in newLegends)
                {
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

                            if (!isDupe)
                            {
                                Legends.Add(leg);
                                Dbug.Log($"Added Lgd :: {leg}");
                            }
                            else
                            {
                                bool edited = dupedLegData.AddKeyDefinition(leg[0]);
                                if (edited)
                                    Dbug.Log($"Edited Lgd :: {dupedLegData.ToStringLengthy()}");
                                else
                                {
                                    bool isPartial = leg.ToStringLengthy() != dupedLegData.ToStringLengthy();
                                    Dbug.Log($"Rejected {(isPartial ? "partial " : "")}duplicate :: {leg}");
                                }
                            }
                            addedLegendQ = true;
                        }
                }
                Dbug.EndLogging();
            }
            return addedLegendQ;
        }
        public bool AddSummary(params SummaryData[] newSummaries)
        {
            bool addedSummaryQ = false;
            if (newSummaries.HasElements())
            {
                if (disableAddDbug)
                    Dbug.DeactivateNextLogSession();
                Dbug.StartLogging("ResLibrary.AddSummary(SumData[])");
                foreach (SummaryData sum in newSummaries)
                {
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

                            if (!isDupe)
                            {
                                addedSummaryQ = true;
                                Summaries.Add(sum);
                                Dbug.Log($"Added Smry :: {sum.ToStringShortened()}");
                            }
                            else Dbug.Log($"Rejected duplicate :: {sum.ToStringShortened()}");
                        }
                }
                Dbug.EndLogging();
            }
            return addedSummaryQ;
        }        
        public void Integrate(ResLibrary other, out ResLibIntegrationInfo[] resLibIntegrationInfoDock)
        {
            Dbug.StartLogging("ResLibrary.Integrate(ResLib)");
            resLibIntegrationInfoDock = null;
            if (other != null)
            {
                Dbug.LogPart("Other ResLibrary is instantiated; ");
                if (other.IsSetup())
                {
                    Dbug.Log("Instance is also setup -- integrating libraries; ");

                    // add resCons
                    AddContent(other.Contents.ToArray());
                    if (rliInfoDock.HasElements())
                        resLibIntegrationInfoDock = rliInfoDock.ToArray();

                    // add legend
                    AddLegend(other.Legends.ToArray());

                    // add summary
                    AddSummary(other.Summaries.ToArray());
                }
                else Dbug.Log("Other ResLibrary is not setup; ");
            }
            else Dbug.Log("Other ResLibrary is null; ");
            Dbug.EndLogging();
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
            Dbug.SingleLog("ResLibrary.ClearLibrary()", "ResLibrary's data has been cleared (reset)");
            _contents = new List<ResContents>();
            _legends = new List<LegendData>();
            _summaries = new List<SummaryData>();
        }
        // all the method ideas below will be combined into this single method (for beyond this, they may not be utilized)
        //  bool RemoveContent(ResCon rcToRemove)
        //  bool RemoveContent(int shelfID)
        //  bool RemoveLegend(string key)  // Necessary?? Contents and summaries would be removed, but does the legend *have* to be removed?
        //  bool RemoveSummary(VerNum versionNum)
        public bool RevertToVersion(VerNum verReversion)
        {
            Dbug.StartLogging("ResLibrary.RevertToVersion(VerNum)");
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
                        Dbug.LogPart($"Retrieved latest library version ({currVer}), and reversion version ({verReversion}); ");
                        if (currVer.AsNumber > verReversion.AsNumber)
                        {
                            Dbug.Log("Fetching array of versions to unload; ");
                            Dbug.LogPart("- Versions to unload ::");
                            List<VerNum> unloadableVers = new();
                            for (int cvx = currVer.AsNumber; cvx > verReversion.AsNumber; cvx--)
                            {
                                VerNum unloadableVerNum = new VerNum(cvx);
                                if (unloadableVerNum.HasValue())
                                {
                                    unloadableVers.Add(unloadableVerNum);
                                    Dbug.LogPart($"  {unloadableVerNum.ToStringNums()}");
                                }
                            }
                            Dbug.Log("; Proceeding to revert versions;");

                            // version unloading loop here                            
                            for (int ulx = 0; ulx < unloadableVers.Count; ulx++)
                            {
                                VerNum verToUnload = unloadableVers[ulx];
                                Dbug.Log($"Unloading -- Version {verToUnload} contents; ");

                                bool firstUnload = ulx == 0;
                                int countExempted = 0, countExempted2 = 0;
                                List<ResContents> contentsCopy = new();
                                List<LegendData> legendsCopy = new();
                                List<SummaryData> summariesCopy = new();

                                // INDENT MAJOR
                                Dbug.NudgeIndent(true);


                                // -- REMOVE CONTENTS --
                                Dbug.Log("ResContents; ");
                                Dbug.NudgeIndent(true);
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
                                    Dbug.Log($"Copied [{contentsCopy.Count}] ResContents from {(firstUnload ? "main contents" : "remaining contents")}; ");
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
                                                                Dbug.Log($". Removed ConAddits :: {caToRemove} [from {resCon}]");
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
                                                                Dbug.Log($". Removed ConChanges :: {ccToRemove} [from {resCon}]");
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
                                                Dbug.Log($". Exempted :: {resCon}");
                                            }
                                        }
                                    }
                                    Dbug.Log($"[{countExempted}] ResContents (and [{countExempted2}] ConAddits/ConChanges) were removed, [{remainingContents.Count}] remain; ");
                                }
                                else Dbug.Log("There are no ResContents to remove; ");
                                Dbug.NudgeIndent(false);


                                // -- REMOVE LEGENDS --
                                countExempted = 0;
                                Dbug.Log("LegendDatas; ");
                                Dbug.NudgeIndent(true);
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
                                    Dbug.Log($"Copied [{legendsCopy.Count}] Legend Datas from {(firstUnload ? "main contents" : "remaining contents")}; ");
                                    foreach (LegendData legDat in legendsCopy)
                                    {
                                        if (legDat.IsSetup())
                                        {
                                            if (!legDat.VersionIntroduced.Equals(verToUnload))
                                                remainingLegends.Add(legDat);
                                            else
                                            {
                                                countExempted++;
                                                Dbug.Log($". Exempted Legd :: {legDat}");
                                            }
                                        }
                                    }
                                    Dbug.Log($"[{countExempted}] Legend Datas were removed, [{remainingLegends.Count}] remain; ");
                                }
                                else Dbug.Log("There are no Legend Datas to remove; ");
                                Dbug.NudgeIndent(false);


                                // -- REMOVE SUMMARIES --
                                countExempted2 = 0;
                                Dbug.Log("SummaryDatas; ");
                                Dbug.NudgeIndent(true);
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
                                    Dbug.Log($"Copied [{summariesCopy.Count}] Summary Datas from {(firstUnload ? "main contents" : "remaining contents")}; ");
                                    foreach (SummaryData sumDat in summariesCopy)
                                    {
                                        if (sumDat.IsSetup())
                                            if (!sumDat.SummaryVersion.Equals(verToUnload))
                                                remainingSummaries.Add(sumDat);
                                            else
                                            {
                                                countExempted2++;
                                                Dbug.Log($". Exempted Smry :: {sumDat.ToStringShortened()}");    
                                            }
                                    }
                                    Dbug.Log($"[{countExempted2}] Summaries Datas were removed, [{remainingSummaries.Count}] remain; ");
                                }
                                else Dbug.Log("There are no Summary Datas to remove; ");
                                Dbug.NudgeIndent(false);


                                // UNINDENT MAJOR
                                Dbug.NudgeIndent(false);
                            }

                            revertedQ = true;
                        }
                        else Dbug.Log("Version to revert to is ahead of or equal to current version; ");
                    }
                    else Dbug.Log("Could not retrieve latest library version; ");


                    // if reversion successfull, set contents, legends, and summaries to remaining data
                    // else, no changes
                    if (revertedQ)
                    {
                        Dbug.LogPart("Version reversion complete; Updating library with remaining contents");
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
                            Dbug.LogPart($"; Verified? {revertedQ}");
                        }
                        Dbug.Log("; ");
                    }
                }
                else Dbug.Log("Received an invalid version to revert to; ");
            }
            else Dbug.Log($"ResLibrary is not setup; ");
            Dbug.EndLogging();
            return revertedQ;
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
            bool prevDADbg = disableAddDbug;
            disableAddDbug = true;

            if (_prevContents != null)
                prevSelf.AddContent(_prevContents.ToArray());
            if (_prevLegends != null)
                prevSelf.AddLegend(_prevLegends.ToArray());
            if (_prevSummaries != null)
                prevSelf.AddSummary(_prevSummaries.ToArray());
            
            disableAddDbug = prevDADbg;
            return prevSelf;
        }



        /// -
        // DATA HANDLING METHODS
        protected override bool EncodeToSharedFile()
        {
            Dbug.StartLogging("ResLibrary.EncodeToSharedFile()");
            bool encodedQ = true;
            
            if (IsSetup())
            {
                Dbug.LogPart("ResLibrary is setup, creating stamp: ");
                bool noIssue = true;
                // 0th verify saving of data
                if (Base.FileWrite(false, stampRLSavedDataTag, stampRLSavedData))
                    Dbug.LogPart("[STAMPED]; ");
                else Dbug.LogPart("[ ? ? ? ] (no stamp); ");
                Dbug.Log("Proceeding with encoding; ");

                // 1st encode contents
                Dbug.Log($"Encoding [{Contents.Count}] ResContents; ");
                for (int rcix = 0; rcix < Contents.Count && noIssue; rcix++)
                {
                    ResContents resCon = Contents[rcix];
                    Dbug.LogPart($"+ Encoding :: {resCon}");

                    if (resCon.IsSetup())
                    {
                        noIssue = Base.FileWrite(false, resCon.ShelfID.ToString(), resCon.EncodeGroups());
                        Dbug.Log($"{Base.ConditionalText(noIssue, "", "[!issue]")}; ");

                        if (noIssue)
                        {
                            Dbug.NudgeIndent(true);
                            foreach (string rcLine in resCon.EncodeGroups())
                                if (rcLine.IsNotNE())
                                    Dbug.Log($"tag [{resCon.ShelfID}]| {rcLine}");
                            Dbug.NudgeIndent(false);
                        }
                    }
                    else Dbug.Log("; Skipped -- Was not setup; ");
                }

                // 2nd encode legends
                if (noIssue)
                {
                    Dbug.Log($"Encoding [{Legends.Count}] Legend Datas; ");
                    List<string> resLibLegendsLines = new();

                    Dbug.NudgeIndent(true);
                    foreach (LegendData legDat in Legends)
                    {
                        if (legDat.IsSetup())
                        {
                            resLibLegendsLines.Add(legDat.Encode());
                            Dbug.Log($"{legDat.Encode()}");
                        }
                        else Dbug.Log($"Skipped ({legDat}) -- was not setup; ");
                    }
                    Dbug.NudgeIndent(false);

                    noIssue = Base.FileWrite(false, legDataTag, resLibLegendsLines.ToArray());
                    Dbug.Log($"{Base.ConditionalText(noIssue, $"Successfully encoded legend datas (with tag '{legDataTag}')", "Failed to encode legend datas")};");
                }

                // 3rd encode summaries
                if (noIssue)
                {
                    Dbug.Log($"Encoding [{Summaries.Count}] Summary Datas; ");
                    List<string> resLibSummaryLines = new();

                    Dbug.NudgeIndent(true);
                    foreach (SummaryData sumDat in Summaries)
                    {
                        if (sumDat.IsSetup())
                        {
                            resLibSummaryLines.Add(sumDat.Encode());
                            Dbug.Log($"{sumDat.Encode()}");
                        }
                        else Dbug.Log($"Skipped ({sumDat.ToStringShortened()}) -- was not setup; ");
                    }
                    Dbug.NudgeIndent(false);

                    noIssue = Base.FileWrite(false, sumDataTag, resLibSummaryLines.ToArray());
                    Dbug.Log($"{Base.ConditionalText(noIssue, $"Successfully encoded summary datas (with tag '{sumDataTag}')", "Failed to encode summary datas")};");
                }

                Dbug.Log($"Encoded ResLibrary? {noIssue}");
                encodedQ = noIssue;
            }
            else Dbug.Log("Not enough data within ResLibrary to proceed with encoding; ");

            if (encodedQ)
                SetPreviousSelf();

            Dbug.EndLogging();
            return encodedQ;
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.StartLogging("ResLibrary.DecodeFromSharedFile()");
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
                    Dbug.Log("Decoding ResContents; ");
                    Contents = new List<ResContents>();

                    /// if true, will show the full result of decoding the ResCon instance (detailed result output)
                    const int noDataTimeout = 25;                    
                    int lastFetchedIx = -1;
                    int shelfNum = 0;
                    Dbug.NudgeIndent(true);
                    for (int lx = 0; lx - noDataTimeout <= lastFetchedIx; lx++)
                    {
                        if (Base.FileRead(lx.ToString(), out string[] rawRCData))
                        {
                            if (rawRCData.HasElements())
                            {
                                if (lastFetchedIx + 1 < lx)
                                    Dbug.Log("; Timeout restart; ");

                                bool dbugGroupCondition = showDecodedLine || expandDecodeDebug;
                                if (dbugGroupCondition)
                                {
                                    Dbug.Log($"Data with tag [{lx}] retrieved; ");
                                    Dbug.NudgeIndent(true);
                                }
                               
                                for (int rlx = 0; rlx < rawRCData.Length && showDecodedLine; rlx++)
                                    Dbug.Log($"L{rlx + 1}| {rawRCData[rlx]}");

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
                                    Dbug.Log($"Decoded ResCon instance :: {decodedRC}; ");
                                    shelfNum++;
                                    lastFetchedIx = lx;

                                    if (expandDecodeDebug)
                                    {
                                        Dbug.NudgeIndent(true);
                                        Dbug.Log($">> {decodedRC.ConBase}");
                                        if (decodedRC.ConAddits.HasElements())
                                        {
                                            Dbug.LogPart(">> ");
                                            for (int xca = 0; xca < decodedRC.ConAddits.Count; xca++)
                                                Dbug.LogPart($"{decodedRC.ConAddits[xca]}{(xca + 1 < decodedRC.ConAddits.Count ? "  //  " : "")}");
                                            Dbug.Log("..");
                                        }
                                        if (decodedRC.ConChanges.HasElements())
                                        {
                                            Dbug.LogPart(">> ");
                                            for (int xcc = 0; xcc < decodedRC.ConChanges.Count; xcc++)
                                                Dbug.LogPart($"{decodedRC.ConChanges[xcc]}{(xcc + 1 < decodedRC.ConChanges.Count ? "  //  " : "")}");
                                            Dbug.Log("  ..");
                                        }
                                        Dbug.NudgeIndent(false);
                                    }
                                }
                                else Dbug.Log("ResCon instance could not be decoded; ");
                                if (dbugGroupCondition) 
                                    Dbug.NudgeIndent(false);
                            }
                            else
                            {
                                if (lastFetchedIx + 1 == lx || lastFetchedIx == lx)
                                    Dbug.LogPart($"No data retrieved; Timing out: {noDataTimeout - (lx - lastFetchedIx)}");
                                else Dbug.LogPart($" {noDataTimeout - (lx - lastFetchedIx)}");
                            }
                        }
                    }
                    Dbug.Log("; Timeout end; ");
                    Dbug.NudgeIndent(false);
                }

                // legends decode
                if (!Legends.HasElements())
                { /// wrapping
                    Dbug.LogPart("Decoding Legend Data; ");
                    Legends = new List<LegendData>();

                    if (Base.FileRead(legDataTag, out string[] legendsData))
                    {
                        if (legendsData.HasElements())
                        {
                            int countLine = 1;
                            Dbug.Log($"Fetched [{legendsData.Length}] lines of legend data; ");
                            foreach (string legData in legendsData)
                            {
                                if (expandDecodeDebug)
                                    Dbug.Log($"L{countLine}| {legData}");

                                Dbug.NudgeIndent(true);
                                LegendData decodedLegd = new();
                                if (decodedLegd.Decode(legData))
                                {
                                    Legends.Add(decodedLegd);
                                    Dbug.Log($"Decoded Legend :: {decodedLegd.ToStringLengthy()}; ");
                                }
                                else Dbug.Log($"Legend Data could not be decoded{(!expandDecodeDebug ? $" :: source ({legData})" : "")};");
                                Dbug.NudgeIndent(false);
                                countLine++;
                            }
                        }
                        else Dbug.Log("Recieved no legend data; ");
                    }
                    else Dbug.Log($"Could not read from file; Issue :: {Tools.GetRecentWarnError(false, false)}");
                }

                // summaries decode
                if (!Summaries.HasElements())
                { /// wrapping
                    Dbug.LogPart("Decoding Summary Data; ");
                    Summaries = new List<SummaryData>();

                    if (Base.FileRead(sumDataTag, out string[] summariesData))
                    {
                        if (summariesData.HasElements())
                        {
                            Dbug.Log($"Fetched [{summariesData.Length}] lines of summary data;");
                            int countLine = 1;
                            foreach (string sumData in summariesData)
                            {
                                if (expandDecodeDebug)
                                    Dbug.Log($"L{countLine}| {sumData}");
                                
                                Dbug.NudgeIndent(true);
                                SummaryData decodedSmry = new();
                                if (decodedSmry.Decode(sumData))
                                {
                                    Summaries.Add(decodedSmry);
                                    Dbug.Log($"Decoded Summary :: {decodedSmry.ToStringShortened()}; ");
                                }
                                else Dbug.Log($"Summary Data could not be decoded{(!expandDecodeDebug ? $" :: source ({sumData})" : "")}; ");
                                Dbug.NudgeIndent(false);

                                countLine++;
                            }
                        }
                        else Dbug.Log("Recieved no summary data; ");
                    }
                    else Dbug.Log($"Could not read from file; Issue :: {Tools.GetRecentWarnError(false, false)}");
                }
            }      
            else Dbug.Log("ResLibrary has no saved data to decode; ");
            Dbug.EndLogging();

            SetPreviousSelf();
            return IsSetup() || noDataButIsOkay;
        }
        #endregion
    }
}
