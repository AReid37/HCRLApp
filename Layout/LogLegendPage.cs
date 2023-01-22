using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp.Layout
{
    internal static class LogLegendPage
    {
        static ResLibrary _resLibrary;
        static readonly char subMenuUnderline = '"';

        public static void GetResourceLibraryReference(ResLibrary mainLibrary)
        {
            _resLibrary = mainLibrary;
        }
        public static void OpenPage()
        {
            Program.LogState("Log Legend View");
            Clear();
            Title("Version Log Glossary (Legend)", subMenuUnderline, 1);
            FormatLine("A dictionary of all the legend keys and definitions collected from version logs.", ForECol.Accent);
            NewLine();

            bool noLibraryData = true;
            // legend glossary
            if (_resLibrary != null)
            {
                if (_resLibrary.IsSetup())
                {
                    noLibraryData = false;

                    // gather data
                    LegendData[] sortedLegendData = null;
                    VerNum latestVersion = VerNum.None;
                    if (sortedLegendData == null)
                    { /// wrapping
                        List<string> sortedKeys = new();
                        foreach (LegendData legDat in _resLibrary.Legends)
                            sortedKeys.Add(legDat.Key);
                        sortedKeys = sortedKeys.ToArray().SortWords();

                        sortedLegendData = new LegendData[sortedKeys.Count];
                        for (int kix = 0; kix < sortedKeys.Count; kix++)
                        {
                            string key = sortedKeys[kix];
                            LegendData matchingLegDat = null;
                            for (int mlgx = 0; mlgx < _resLibrary.Legends.Count && matchingLegDat == null; mlgx++)
                            {
                                if (key == _resLibrary.Legends[mlgx].Key)
                                    matchingLegDat = _resLibrary.Legends[mlgx];
                            }
                            if (matchingLegDat != null)
                            {
                                sortedLegendData[kix] = matchingLegDat;
                                //Dbug.SingleLog("--", $"Sorted @ix{kix} --> {matchingLegDat}");
                            }
                        }

                        _resLibrary.GetVersionRange(out _, out latestVersion);
                    }

                    // print table
                    if (sortedLegendData.HasElements())
                    {
                        /// table setup
                        Table2Division tDivSize = Table2Division.KCSmall;
                        TableRowDivider(true);
                        TableRowDivider(subMenuUnderline, false, GetPrefsForeColor(ForECol.Accent));

                        Program.ToggleFormatUsageVerification();

                        /// table header
                        HoldNextListOrTable();
                        Table(tDivSize, "Version And Key", '|', "Definitions");
                        Format(LatestTablePrintText.ToUpper(), ForECol.Heading2);
                        TableRowDivider(false);

                        /// table contents                        
                        foreach (LegendData tableLeg in sortedLegendData)
                        {
                            const string backtick = "`";
                            const string replaceBacktick = DataHandlerBase.Sep;

                            string tabKeyInfo = $"{tableLeg.VersionIntroduced.ToStringNums()} {tableLeg.Key}".Replace(backtick, replaceBacktick);
                            string tabDat1Info = tableLeg.DefinitionsString.Replace(backtick, replaceBacktick);
                            
                            HoldNextListOrTable();                            
                            Table(tDivSize, tabKeyInfo, '|', tabDat1Info);
                            Highlight(true, LatestTablePrintText.Replace("\n","").Replace(replaceBacktick, backtick), $" {tableLeg.Key} ", tableLeg[0]);
                            if (HSNL(0, 2) - 2 == 0)
                                HorizontalRule('-');
                        }
                        Program.ToggleFormatUsageVerification();
                    }
                    Format($"{Ind14}All Log Legends from latest library version ({latestVersion}). ", ForECol.Accent);
                    Pause();
                }
            }
            // no legend glossary
            if (noLibraryData)
            {
                Format($"{Ind24}The library shelves are empty. No legend keys and definitions found.", ForECol.Normal);
                Pause();
            }
        }
    }
}
