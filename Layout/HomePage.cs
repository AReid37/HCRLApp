using System;
using System.Collections.Generic;
using ConsoleFormat;
using static ConsoleFormat.Minimal;
using static ConsoleFormat.Base;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.Layout
{
    internal static class HomePage
    {
        const int rigStyleSheetIndex = -1; // set to -1 for no rigging

        public static void OpenPage()
        {
            bool riggedStyleIndexQ = rigStyleSheetIndex != -1;
            const int inkCount = 10;
            /// INKING IDS
            ///     0~3 -> Primary Color (ex. title)
            ///         0 :: Pattern Full
            ///         1 :: Pattern Dark
            ///         2 :: Pattern Medium
            ///         3 :: Pattern Light
            ///     4~6 -> Secondary Color (ex. subtitle)
            ///         4 :: Pattern Full
            ///         5 :: Pattern Medium
            ///         6 :: Pattern Light
            ///     7~9 -> Tertiary Color (ex. touch ups)
            ///         7 :: Pattern Full
            ///         8 :: Pattern Medium
            ///         9 :: Pattern Light
            HPInk[] inkList = new HPInk[inkCount]
            {
                // primary
                new HPInk(0, GetPrefsForeColor(ForECol.Heading1)), 
                new HPInk(1, GetPrefsForeColor(ForECol.Heading1), Dither.Dark), 
                new HPInk(2, GetPrefsForeColor(ForECol.Heading1), Dither.Medium), 
                new HPInk(3, GetPrefsForeColor(ForECol.Heading1), Dither.Light), 

                // secondary
                new HPInk(4, GetPrefsForeColor(ForECol.Normal)), 
                new HPInk(5, GetPrefsForeColor(ForECol.Normal), Dither.Medium), 
                new HPInk(6, GetPrefsForeColor(ForECol.Normal), Dither.Light), 

                // tertiary
                new HPInk(7, GetPrefsForeColor(ForECol.Accent)),
                new HPInk(8, GetPrefsForeColor(ForECol.Accent), Dither.Medium),
                new HPInk(9, GetPrefsForeColor(ForECol.Accent), Dither.Light)
            };

            // styles within this list
            List<string[]> styleSheet = new List<string[]>
            {
                /// for old title design
                new string[0],

                /// test style clipping - cosine wave with demarked start and end
                #region
                //new string[]
                //{
                //    "000               001               011               122              2",
                //    "2  012         012   012         012   012         012   012         012",
                //    "2     012   012         012   012         012   012         012   012  2",
                //    "2        012               012               012               012     2",
                //},
                #endregion

                /// simple acronym design with half star on sides
                new string[]
                {
                    "   0  0 0000 000  0     000   ",
                    " 3 0  0 0    0  0 0    0  0 3 ",
                    "32 0000 0    000  0    0000 23",
                    " 3 0  0 0    0  0 0    0  0 3 ",
                    "   0  0 0000 0  0 0000    0   ",
                }
            };
            int styleIx = Extensions.Random(0, styleSheet.Count - 1);
            if (styleIx == 0 && !riggedStyleIndexQ) // avoid basic
                styleIx = Extensions.Random(0, styleSheet.Count - 1);
            if (riggedStyleIndexQ)
                styleIx = rigStyleSheetIndex.Clamp(0, styleSheet.Count);

            Program.LogState("Title Page");
            if (styleSheet.HasElements() && inkList.HasElements(inkCount))
            {
                // prestyle prints
                switch (styleIx)
                {
                    case 0:
                        NewLine(10);
                        break;

                    case 1:
                        NewLine(3);
                        HorizontalRule(cTHB, 2);
                        break;

                    default: break;
                }

                // style print
                string[] chosenStyle = styleSheet[styleIx];
                if (chosenStyle.HasElements())
                {
                    for (int csi = 0; csi < chosenStyle.Length; csi++)
                    {
                        if (chosenStyle[csi].IsNotNE())
                        {
                            // center align style                            
                            int bufferWidth = Console.WindowWidth;
                            int lineWidth = chosenStyle[csi].Length * 2;
                            int padding = (bufferWidth - lineWidth) / 4;
                            string styleLine;
                            /// indent
                            if (padding > 0)
                            {
                                styleLine = chosenStyle[csi].PadLeft(padding + (lineWidth / 2));
                            }   
                            /// clip
                            else
                            {
                                int csLen = chosenStyle[csi].Length;
                                styleLine = chosenStyle[csi].Substring(-padding + 2, csLen + (padding * 2) - 2);
                            }


                            // print style
                            for (int csix = 0; csix < styleLine.Length; csix++)
                            {
                                HPInk? inkToUse = GetInk(styleLine[csix].ToString());
                                if (inkToUse.HasValue)
                                {
                                    if (inkToUse.Value.inkPattern.HasValue)
                                        DrawPixel(Color.Black, inkToUse.Value.inkPattern.Value, inkToUse.Value.inkCol);
                                    else DrawPixel(inkToUse.Value.inkCol);
                                }
                                else DrawPixel(Color.Black);
                            }
                        }
                        NewLine();
                    }
                }

                // post style prints
                switch (styleIx)
                {
                    case 0:
                        Title("H i g h   C o n t r a s t", cBHB, 0);
                        Title("  Resource Library  App  ", cTHB, 3);
                        break;

                    case 1:
                        NewLine(2);
                        Title("High Constrast Resource Library App", cBHB, 2);
                        break;

                    default: break;
                }
            }

            // static methods
            HPInk? GetInk(string str)
            {
                HPInk? inkToUse = null;
                if (int.TryParse(str, out int hpID))
                {
                    foreach (HPInk loadedInk in inkList)
                    {
                        if (loadedInk.id == hpID)
                        {
                            inkToUse = loadedInk;
                            break;
                        }
                    }
                }
                return inkToUse;
            }
        }

        public static void OldTitlePage()
        {
            Program.LogState("Title Page");
            NewLine(10);
            Title("H i g h   C o n t r a s t", cBHB, 0);
            Title("  Resource Library  App  ", cTHB, 3);
        }


        // NESTED STRUCT
        public struct HPInk
        {
            public int id;
            public Color inkCol;
            public Dither? inkPattern;

            public HPInk(int colID, Color inkColor)
            {
                id = colID;
                inkCol = inkColor;
                inkPattern = null;
            }
            public HPInk(int colID, Color inkColor, Dither? pattern)
            {
                id = colID;
                inkCol = inkColor;
                inkPattern = pattern;
            }
        }
    }
}
