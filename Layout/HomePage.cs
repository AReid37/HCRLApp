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
        static ForECol primaryCol = ForECol.Heading1, secondaryCol = ForECol.Normal, tertiaryCol = ForECol.Accent;
        const int rigStyleSheetIndex = -1; // set to -1 for no rigging

        public static void OpenPage()
        {
            Program.LogState("Title Page");
            bool riggedStyleIndexQ = rigStyleSheetIndex != -1;
            bool colorsAsRGBq = riggedStyleIndexQ && true;
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
            /// INKING IDS CONDENSED
            ///     0~3 (Primary Color) {title} [0 Full, 1 Dark, 2 Medium, 3 Light]
            ///     4~7 (Secondary Col) {sbttl} [4 Full, 5 Dark, 6 Medium, 7 Light]
            ///     8~11 (Tertiary Col) {touch} [8 Full, 9 Dark, - Medium, . Light]
            ///     
            HPInk[] inkList;
            if (!colorsAsRGBq)
                inkList = new HPInk[inkCount]
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
            else 
                inkList = new HPInk[inkCount]
                {
                    // primary
                    new HPInk(0, Color.Red),
                    new HPInk(1, Color.Red, Dither.Dark),
                    new HPInk(2, Color.Red, Dither.Medium),
                    new HPInk(3, Color.Red, Dither.Light), 

                    // secondary
                    new HPInk(4, Color.Green),
                    new HPInk(5, Color.Green, Dither.Dark),
                    new HPInk(6, Color.Green, Dither.Medium),
                    new HPInk(7, Color.Green, Dither.Light),

                    // tertiary
                    new HPInk(8, Color.Blue),
                    new HPInk(9, Color.Blue, Dither.Dark),
                    new HPInk(10, Color.Blue, Dither.Medium),
                    new HPInk(11, Color.Blue, Dither.Light)
                };

            // styles within this list
            List<string[]> styleSheet = new List<string[]>
            {
                // NOTE :: null or empty lines are filtered out in final display
                /// 0: for old title design
                new string[]
                {
                    //"0123 4567 89-.",
                    ""
                },

                /// 1: simple acronym design with half star on sides
                new string[]
                {
                    "   0  0 0000 000  0     000   ",
                    " 3 0  0 0    0  0 0    0  0 3 ",
                    "32 0000 0    000  0    0000 23",
                    " 3 0  0 0    0  0 0    0  0 3 ",
                    "   0  0 0000 0  0 0000    0   ",
                },

                /// 2: 'HC' in two merging blocks followed by 3D like 'R.L.A'
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

                    " ",
                    HSNL(0,1) == 1 ? " " : null,
                    /// 'R.L.A' with a 3D-like effect
                    "  88.     8.       8.    ",
                    "  8.8.    8.      8.8.   ",
                    "..88-.. ..8-..  ..888-.. ",
                    "  8.8.    8.      8.8.   ",
                    "  8.8. 9. 888. 9. 8.8. 9.",
                },

                /// 3: 'HC' in similar shape to resource pack icon surrounded with an epic screen crack effect
                new string[]
                {
                // bounds
                //  <|                                  |         |                                  |>
                    HSNL(0,10) < 4 ? null :
                    "                                        7.                                       ",
                    HSNL(0,10) < 2 ? null :
                    "                                7    77711..   .                                 ",
                    HSNL(0,10) < 1 ? null :
                    "                           7      7 7761 12...  ..    .                          ",
                    "                         7    777216761  12...212.-..       .                    ",
                    "                 7   77   77776721 1661  12.211 12.---...    .                   ",
                    "              7       777776677221  11    111  122--...  ..                      ", // ~
                    "                  77 777676622111    1         122222---.-.... ..    .           ",
                    "                77 777 7777211   1       00009  111212-.... .   ...              ",
                    "          7  77  777676676662211   50 50 09999     1 12----.-..-.....            ",
                    "            7   777 7777776766221  50 50 09      11212--.-.... ...    .  .       ",
                    "     7   7 777 7777677766676666621 50000 09   11122-.----------.---..-.... ..  . ", // ~~ m i d
                    " 7 77 7777677666776666676666662211 50550 09    12-----.---..-....-... ... .      ", // ~~ m i d
                    "         77 777  777777666662211   50 50 00009 122--....... ....  .       .      ",
                    "   7  77  7   7776777666776666221  50 50 99999 12--.---..- ..... .. .            ",
                    "          7    777  7777777766621  55 55      12-.... ..  .. .    .              ",
                    "        7    77 7777776776666221 11            12---.-... .                      ",
                    "                  7 77 777766621121  1     111  12-....       ..     .           ", // ~
                    "          7   77      77 7776767221 161   12.21112..      .                      ",
                    "                    77     7777621 16611 12..   .                                ",
                    HSNL(0,10) < 1 ? null :
                    "                            7  772167761 12...     .                             ",
                    HSNL(0,10) < 2 ? null :
                    "                              7 777   771..                                      ",
                    HSNL(0,10) < 4 ? null :
                    "                                        7                                        ",
                //  <|                                  |         |                                  |>
                // bounds
                },                
                
                /// 4: 'HCRLA' projecting from an opened book
                new string[]
                {

                    HSNL(0, 10) < 1 ? null :
                    "        .                          .               ",
                    HSNL(0, 10) < 1 ? null :
                    "                3     .      3              .      ",
                    "   .        .   .      888  .      7  .            ",
                    "      .   7    8888.  .8--8    8        .    3     ",
                    "              .8---73  888- . -8  .             .  ",
                    "     .  8. 8   8-.  7. 8--8.  -8.     -88  .       ",
                    "  3     8- 8-. 8-..    8..8. .-8.. . -8.-8         ",
                    "    .  .8888- .8888.  .-..-. .-8888. -8888  .      ",
                    "     . .8--8-. .----. .. ..  .----.  -8--8.        ",
                    "       .8-.8-.. ....   .. . .. .. 3 .-8.-8.        ",
                    "        .-..- ..  . . 3 ..   ..     .-..-.         ",                    
                    "      3    . .. .   ..   .  .      ... ..    377   ",
                    "       |        . .   . .  .  666..666     |  37   ",
                    "   7 7 |       6666.6666 .6666 .   73 666  | 37    ",
                    "    773|   6666 7    .  66  .     73    6  |       ",
                    "     33|  6      3      .6               6 |       ",
                    HSNL(0, 10) < 1 ? null :
                    "       |  6                       6666   6 |   7   ",
                    "       | 6       6666     6   666666666666 |  3    ",
                    "       | 6   666666666666 6666666665555555 |       ",
                    "       | 6666655555556666666665555577777   |       ",
                    "       | 555557777775555566655577777       |       ",
                    HSNL(0, 10) < 1 ? null :
                    "       |             7775555577            |       ",
                    HSNL(0, 10) < 2 ? null :
                    "                                                   ",
                },

                /// 5: 'HC' with a crack behind them similar to resource pack icon, HC/RLA are above/below in morse code
                new string[]
                {
                    HSNL(0, 10) < 2 ? null :
                    "3 3 3 3     333 3 333 3",
                    HSNL(0, 10) < 2 ? null :
                    " ",
                    HSNL(0, 10) < 4 ? null :
                    " ",
                    HSNL(0, 10) < 1 ? null :
                    "                          3",
                    "                         3 ",
                    "               ........223 ",
                    "   7777       .00000009.3  ",
                    "   75007 777  .00000009.   ",
                    "   7500775007 .0099999-.   ",
                    "   7500775007 .009.....    ",
                    "   7500775007 .009. 23     ",
                    "   7500775007 .009.23      ",
                    "   7500555007 .009.3       ",
                    "   75000000072.009.   3    ",
                    "   75007750072.009. 33     ",
                    "   75007750073.009.        ",
                    "   7500775007 .009.....    ",
                    "   7500775007 .00000009.   ",
                    " 337500775007 .00000009.   ",
                    "   7555775007 .9999999-.   ",
                    "  23777 76557  ........    ",
                    " 33      777               ",
                    HSNL(0, 10) < 1 ? null :
                    "3                          ",
                    HSNL(0, 10) < 4 ? null :
                    " ",
                    HSNL(0, 10) < 2 ? null :
                    " ",
                    HSNL(0, 10) < 2 ? null :
                    "3 333 3     3 333 3 3     3 333",
                },                

                /// INKING IDS CONDENSED
                ///     0~3 (Primary Color) {title} [0 Full, 1 Dark, 2 Medium, 3 Light]
                ///     4~7 (Secondary Col) {sbttl} [4 Full, 5 Dark, 6 Medium, 7 Light]
                ///     8~11 (Tertiary Col) {touch} [8 Full, 9 Dark, - Medium, . Light]

                /** ***** STYLE NO.5 - INKING HISTORY *****
                ------------
                Ch1: 'HC' with borders and glow
                    "                           ",
                    "               ........    ",
                    "   7777       .00000009.   ",
                    "   75007 777  .00000009.   ",
                    "   7500775007 .0099999-.   ",
                    "   7500775007 .009.....    ",
                    "   7500775007 .009.        ",
                    "   7500775007 .009.        ",
                    "   7500555007 .009.        ",
                    "   7500000007 .009.        ",
                    "   7500775007 .009.        ",
                    "   7500775007 .009         ",
                    "   7500775007 .009.....    ",
                    "   7500775007 .00000009.   ",
                    "   7500775007 .00000009.   ",
                    "   7555775007 .9999999-.   ",
                    "    777 76557  ........    ",
                    "         777               ",
                    "                           ",


                ------------
                Ch0: 'HC' with borders                 
                    "                   ",
                    "           00000009",
                    "500        00000009",
                    "500  500   0099999-",
                    "500  500   009     ",
                    "500  500   009     ",
                    "50055500   009     ",
                    "50000000   009     ",
                    "500  500   009     ",
                    "500  500   009     ",
                    "500  500   00000009",
                    "500  500   00000009",
                    "555  500   9999999-",
                    "     655           ",
                    "                   ",

                 *********************/

                /// 6: 'HCRLA' acronym design similar to Terraria logo with a layered surface background

                /// 7: simple acronym design within a layered cave perspective
                /// NOTE :: up to 7 designs maximum

                /// n: test style clipping and centering
                #region
                new string[]
                {
                    "    2222                      00                      2222    ",
                    "3333    8888                00  00                8888    3333",
                    "            4444          00      00          4444            ",
                    "                8888444400    --    0044448888                ",
                    // ^above for clipping (neg. padding) -- below for center aligning (pos. padding)
                    " ",
                    "8888            0000            8888",
                    "    4444      00    00      4444    ",
                    "        884400   --   004488        ",
                },
                #endregion
            };

            /// selecting style by index here
            int styleIx = Extensions.Random(0, styleSheet.Count - 2);
            if (styleIx == 0 && !riggedStyleIndexQ) // avoid basic
                styleIx = Extensions.Random(0, styleSheet.Count - 2);
            if (riggedStyleIndexQ)
                styleIx = rigStyleSheetIndex.Clamp(0, styleSheet.Count);

            
            // ALL PRINTING HERE
            if (styleSheet.HasElements() && inkList.HasElements(inkCount))
            {
                /// for some reason, Minimal method custom colors don't fully apply on start up until i do this...
                FormatLine("Hello, High Contrast Resource Library App!", ForECol.Normal);
                Clear();

                // prestyle prints
                switch (styleIx)
                {
                    case 0:
                        NewLine(HSNL(10, 20));
                        break;

                    case 1:
                        HSNLPrint(3, 10);
                        HorizontalRule(cTHB, HSNL(2, 4));
                        break;

                    case 2:
                        HSNLPrint(0, 4);
                        HorizontalRule(cTHB, HSNL(0, 2));
                        break;

                    case 3:
                        HorizontalRule(cBHB, HSNL(0, 4));
                        break;

                    case 4:
                        NewLine(HSNL(1, 5));
                        break;

                    case 5:
                        HSNLPrint(0, 3);
                        break;

                    default: break;
                }

                // style print
                string[] chosenStyle = styleSheet[styleIx];
                if (chosenStyle.HasElements())
                {
                    /// clean up style sheet received
                    List<string> filteredChosenStyle = new();
                    foreach (string lineOfStyle in chosenStyle)
                    {
                        if (lineOfStyle.IsNotNE())
                            filteredChosenStyle.Add(lineOfStyle);
                    }
                    chosenStyle = filteredChosenStyle.ToArray();

                    /// print filtered style sheet
                    for (int csi = 0; csi < chosenStyle.Length; csi++)
                    {
                        bool lineIsAsWideAsBufferQ = false;
                        bool endOfStyleQ = csi + 1 == chosenStyle.Length;
                        if (chosenStyle[csi].IsNotNE())
                        { /// wrapping... somewhat
                            chosenStyle[csi] = chosenStyle[csi].Replace("\n", "");

                            // center align style                            
                            int bufferWidth = Console.WindowWidth;
                            int lineWidth = chosenStyle[csi].Length * 2;
                            int padding = (bufferWidth - lineWidth) / 2; //(int)((bufferWidth - lineWidth) / 4f);
                            string styleLine;
                            /// indent
                            if (padding > 0)
                            {
                                styleLine = chosenStyle[csi].PadLeft((lineWidth + padding) / 2);
                            }
                            /// clip
                            else
                            {
                                int csLen = chosenStyle[csi].Length;
                                styleLine = chosenStyle[csi].Substring(-padding / 2, csLen + padding);
                            }
                            int trueLineWidth = styleLine.TrimEnd().Length * 2;

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
                                //else DrawPixel(Color.Black);
                                else
                                {
                                    if (csix + 1 < styleLine.Length)
                                        DrawPixel(Color.Black);
                                        //Text("  ");
                                    //else Text(":");
                                }

                            }

                            //lineIsAsWideAsBufferQ = bufferWidth <= lineWidth;
                            lineIsAsWideAsBufferQ = bufferWidth <= trueLineWidth;

                            /// a bit of debug
                            //NewLine();
                            //if (bufferWidth <= lineWidth)
                            //    Text(" [BW <= LW] ", Color.Red);
                            //if (bufferWidth <= trueLineWidth)
                            //    Text(" [BW <= TLW] ", Color.Maroon);
                            //if (endOfStyleQ)
                            //    Text(" [END] ", Color.Magenta);
                            //NewLine();
                        }

                        //if (chosenStyle[csi] != null)
                        if (chosenStyle[csi] != null && (!lineIsAsWideAsBufferQ || endOfStyleQ))
                            NewLine();
                            //TextLine("n", Color.Orange);
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

                    case 3:
                        HSNLPrint(0, 4);
                        HorizontalRule(cTHB, HSNL(0, 4).Clamp(0, 1));
                        break;

                    case 4:
                        HSNLPrint(0, 3);
                        HorizontalRule(cTHB, HSNL(0, 2));
                        break;

                    case 5:
                        NewLine();
                        HSNLPrint(0, 3);
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
