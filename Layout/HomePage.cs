using System;
using System.Collections.Generic;
using ConsoleFormat;
using static ConsoleFormat.Minimal;
using static ConsoleFormat.Base;
using static HCResourceLibraryApp.Layout.PageBase;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp.Layout
{
    internal static class HomePage
    {
        public static ForECol primaryCol = ForECol.Heading1, secondaryCol = ForECol.Normal, tertiaryCol = ForECol.Accent;
        const int rigStyleSheetIndex = -1; // set to -1 for no rigging

        public static void OpenPage()
        {
            Program.LogState("Title Page");
            bool riggedStyleIndexQ = rigStyleSheetIndex != -1;
            const int inkCount = 12;
            const string _10thInkKey = "-", _11thInkKey = ".";
            /// INKING IDS
            ///     0~3 -> Primary Color (ex. title)
            ///         0 :: Pattern Full
            ///         1 :: Pattern Dark
            ///         2 :: Pattern Medium
            ///         3 :: Pattern Light
            ///     4~7 -> Secondary Color (ex. subtitle)
            ///         4 :: Pattern Full
            ///         5 :: Pattern Dark
            ///         6 :: Pattern Medium
            ///         7 :: Pattern Light
            ///     8~11 -> Tertiary Color (ex. touch ups)
            ///         8 :: Pattern Full
            ///         9 :: Pattern Dark
            ///         10 (-) :: Pattern Medium
            ///         11 (.) :: Pattern Light
            ///         
            HPInk[] inkList = new HPInk[inkCount]
            {
                // primary
                new HPInk(0, GetPrefsForeColor(primaryCol)), 
                new HPInk(1, GetPrefsForeColor(primaryCol), Dither.Dark), 
                new HPInk(2, GetPrefsForeColor(primaryCol), Dither.Medium), 
                new HPInk(3, GetPrefsForeColor(primaryCol), Dither.Light), 

                // secondary
                new HPInk(4, GetPrefsForeColor(secondaryCol)), 
                new HPInk(5, GetPrefsForeColor(secondaryCol), Dither.Dark), 
                new HPInk(6, GetPrefsForeColor(secondaryCol), Dither.Medium),
                new HPInk(7, GetPrefsForeColor(secondaryCol), Dither.Light),

                // tertiary
                new HPInk(8, GetPrefsForeColor(tertiaryCol)),
                new HPInk(9, GetPrefsForeColor(tertiaryCol), Dither.Dark),
                new HPInk(10, GetPrefsForeColor(tertiaryCol), Dither.Medium),
                new HPInk(11, GetPrefsForeColor(tertiaryCol), Dither.Light)
            };

            // styles within this list
            List<string[]> styleSheet = new List<string[]>
            {
                /// for old title design
                new string[]
                {
                    //"0123 4567 89-.",
                    ""
                },

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
                },

                /// 'HC' in two merging blocks followed by 3D like 'R.L.A'
                new string[]
                {
                    /// 'HC' within two block-like objects merging together                    
                    " 1111111111 5555  5555",
                    "1222222232357  7557665",
                    "1333 3   317    7 6765",
                    "13  0  0  37   4447 75",
                    "13  0  0    7  47 675 ",
                    "13  0000    3 747  75 ",
                    "13  0  0   7 7 4     5",
                    "13  0  0 3 5 7 444  75",
                    "13        377 7      5",
                    "133 3    3157 55   755",
                    " 1111111111 55  55555 ",

                    "",
                    HSNL(0,1) == 1 ? "" : null,
                    /// 'R.L.A' with a 3D-like effect
                    "  88.     8.       8.    ",
                    "  8.8.    8.      8.8.   ",
                    "..88-.. ..8-..  ..888-.. ",
                    "  8.8.    8.      8.8.   ",
                    "  8.8. 9. 888. 9. 8.8. 9.",
                },
            };
            int styleIx = Extensions.Random(0, styleSheet.Count - 1);
            if (styleIx == 0 && !riggedStyleIndexQ) // avoid basic
                styleIx = Extensions.Random(0, styleSheet.Count - 1);
            if (riggedStyleIndexQ)
                styleIx = rigStyleSheetIndex.Clamp(0, styleSheet.Count);

            if (styleSheet.HasElements() && inkList.HasElements(inkCount))
            {
                /// for some reason, Minimal method custom colors don't fully apply on start up until i do this...
                FormatLine("Hello, High Contrast Resource Library App!", ForECol.Normal);
                Clear();

                // prestyle prints
                switch (styleIx)
                {
                    case 0:
                        NewLine(10);
                        break;

                    case 1:
                        HSNLPrint(3, 10);
                        HorizontalRule(cTHB, HSNL(2, 4));
                        break;

                    case 2:
                        HSNLPrint(0, 4);
                        HorizontalRule(cTHB, HSNL(0, 2));
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

                        if (chosenStyle[csi] != null)
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
                        NewLine(HSNL(2, 4));
                        Title("High Constrast Resource Library App", cBHB, 2);
                        break;

                    case 2:
                        HSNLPrint(0, 4);
                        HorizontalRule(cBHB, HSNL(0,2));
                        break;

                    default: break;
                }
            }


            // static methods
            HPInk? GetInk(string str)
            {
                HPInk? inkToUse = null;
                if (str != null)
                {
                    /// replace for other numbers (10+)
                    if (str == _10thInkKey)
                        str = "10";
                    else if (str == _11thInkKey)
                        str = "11";
                }

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
