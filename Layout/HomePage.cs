using System;
using System.Collections.Generic;
using HCResourceLibraryApp.Layout;
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
        const bool rigDefaultCols = true; // set to 'true' to use RGB instead of defaults (H1,Nor,Acc)

        public static void OpenPage()
        {
            Program.LogState("Title Page");
            bool riggedStyleIndexQ = rigStyleSheetIndex != -1;
            bool colorsAsRGBq = riggedStyleIndexQ && rigDefaultCols;
            bool foolProofPrintingQ = !Program.isDebugVersionQ || riggedStyleIndexQ; /// since it doesn't print right on release for some reason
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

                /// 6: 'HCRLA' acronym design similar to Terraria logo with a layered surface background
                new string[]
                {
                /// |-                          :_                  -|-                   _:                        -|++
                    HSNL(0, 20) < 5 ? null :
                    "  .                                       ..                                           ..         ",
                    HSNL(0, 20) < 3 ? null :
                    " .....        ...                     ........                                        ......      ",
                    HSNL(0, 20) < 3 ? null :
                    "            ..   ...                                     ..                                       ",
                    "          ......... .... . |             7                ...  7      |    ...                    ",
                    "                           |  7  .3    3 33  3 33 3   3.       33 3   |   ..  ..                  ",
                    "                           |  32 3223 322223.3222223 3222   3 322223  | ..      ....              ",
                    "                           | 32 22  232   2232     222  23 7332    2  ....   ..    .... .. .      ",
                    "    ....                   | 200  1022 0000 2200001  200 2 322 00012  |   .... ......             ",
                    "              1121         | 210  00  100001  100001  10 222  1000002 |                           ",
                    "             112122        |2200  00  01  00  00  00  10      00  01 2|                           ",
                    "             121222        |2 001000  00      011100  00      010011 2|                           ",
                    "             122222        |2 000001  00      11100   01      000111 2|                           ",
                    "              1772         |2 00  01  10  11  11 000  000     10  11 2|                           ",
                    "               67          |2 01  11  000001  11  10  001111  00  11 2|   -               3       ",
                    "     3      12 66          | 200  11   1000   11  00 2 11111  01  112 |  77-      7.  777777      ",
                    "      7 77 127267 22  7777 | 22  2   22    22   22  2 2     2   222 2 |  6-   7  777777.. .77.    ",
                    "     7777.  226663722  ...77  222 222 22222  222722277.2222272222  22 7 26   7777777.7.  . .. ..  ",
                    "  77777. 7.    66722   . ...77.           777.7.77 .. 7  .....     777 2667 .      ..             ",
                    " 777.          66          .. ..7..    77777..            .  ...77777.  66     7 2                ",
                    "7        -     667      -            77 .         2             .77..  766    722                 ",
                    HSNL(0, 20) < 3 ? null :
                    "  .     -7     666 2    7       2                 7                    666 -  67                  ",
                    "         -6  27676767  6-6 67  66 6    2      - 7 67 66 7  7-  63 76 7666667 67  67               ",
                    HSNL(0, 20) < 3 ? null :
                    "   -  67  67 76677666666666666666666  762  6 -67676666666666666666666666666666666667 67- 7        ",
                    HSNL(0, 20) < 3 ? null :
                    "   7777666666666666676777776666777666666666666666667677777776677666777776677777667666666677737    ",
                    HSNL(0, 20) < 3 ? null :
                    " 777  77767766677777 7 77 777777 77777667777776766777777 77777 7777777  777   77777776677 777 77  ",
                    HSNL(0, 20) < 5 ? null :
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7   ",
                    HSNL(0, 20) < 5 ? null :
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7      ",
                    HSNL(0, 20) < 7 ? null :
                    "     7           7      7              7      7                    7                  7     7     ",
                /// |-                          :_                  -|-                   _:                        -|++

                /// Plains to logo alignment measures
                /// 
                /// Combined measures ::
                /// |-                          :_                  -|-                   _:                        -|++    (++ to help with narrow width centering)
                /// 
                ///                             |_                  -|-                   _|
                /// |-                                              -|-                                             -|
                /// 
                /// |_                  -|-                   _|    
                /// |_                                        _|    63-22 = 41 chars width      20c ea side | 1c center
                /// |-                                              -|-                                             -|   
                /// |-                                                                                              -|   117-22 = 95 chars width       47c ea side | 1c center
                },

                /// INKING IDS CONDENSED
                ///     0~3 (Primary Color) {title} [0 Full, 1 Dark, 2 Medium, 3 Light]
                ///     4~7 (Secondary Col) {sbttl} [4 Full, 5 Dark, 6 Medium, 7 Light]
                ///     8~11 (Tertiary Col) {touch} [8 Full, 9 Dark, - Medium, . Light]

                /** ***** STYLE NO.6 - INKING HISTORY *****
                ----------
                Ch7: 2nd plains layer rendered
                    "                           |             7                     7      |                         ",
                    "                           |  7  .3    3 33  3 33 3   3.       33 3   |                         ",
                    "                           |  32 3223 322223.3222223 3222   3 322223  |                         ",
                    "                           | 32 22  232   2232     222  23 7332    2  |                         ",
                    "                           | 200  1022 0000 2200001  200 2 322 00012  |                         ",
                    "              1121         | 210  00  100001  100001  10 222  1000002 |                         ",
                    "             112122        |2200  00  01  00  00  00  10      00  01 2|                         ",
                    "             121222        |2 001000  00      011100  00      010011 2|                         ",
                    "             122222        |2 000001  00      11100   01      000111 2|                         ",
                    "              1772         |2 00  01  10  11  11 000  000     10  11 2|                         ",
                    "               67          |2 01  11  000001  11  10  001111  00  11 2|   -               3     ",
                    "     3      12 66          | 200  11   1000   11  00 2 11111  01  112 |  77-      7.  777777    ",
                    "      7 77 127267 22  7777 | 22  2   22    22   22  2 2     2   222 2 |  6-   7  777777.. .77.  ",
                    "     7777.  226663722  ...77  222 222 22222  222722277.2222272222  22 7 26   7777777.7.  . .. ..",
                    "  77777. 7.    66722   . ...77.           777.7.77 .. 7  .....     777 2667 .      ..           ",
                    " 777.          66          .. ..7..    77777..            .  ...77777.  66     7 2              ",
                    "7        -     667      -            77 .         2             .77..  766    722               ",
                    "  .     -7     666 2    7       2                 7                    666 -  67                ",
                    "         -6  27676767  6-6 67  66 6    2      - 7 67 66 7  7-  63 76 7666667 67  67             ",
                    "   -  67  67 76677666666666666666666  762  6 -67676666666666666666666666666666666667 67- 7      ",
                    "   7777666666666666676777776666777666666666666666667677777776677666777776677777667666666677737  ",
                    " 777  77767766677777 7 77 777777 77777667777776766777777 77777 7777777  777   77777776677 777 77",
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7 ",
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7    ",
                    "     7           7      7              7      7                    7                  7     7   ",
                
                
                
                
                
                
                
                
                ----------
                Ch6: logo and 1st layer with tree, stump & flowers
                    "                           |             7                     7      |                         ",
                    "                           |  7  .3    3 33  3 33 3   3.       33 3   |                         ",
                    "                           |  32 3223 322223.3222223 3222   3 322223  |                         ",
                    "                           | 32 22  232   2232     222  23 7332    2  |                         ",
                    "                           | 200  1022 0000 2200001  200 2 322 00012  |                         ",
                    "              2233         | 210  00  100001  100001  10 222  1000002 |                         ",
                    "             223233        |2200  00  01  00  00  00  10      00  01 2|                         ",
                    "             232333        |2 001000  00      011100  00      010011 2|                         ",
                    "             233333        |2 000001  00      11100   01      000111 2|                         ",
                    "              2773         |2 00  01  10  11  11 000  000     10  11 2|                         ",
                    "               67          |2 01  11  000001  11  10  001111  00  11 2|   -                     ",
                    "            23 66          | 200  11   1000   11  00 2 11111  01  112 |  77.                    ",
                    "           237367 33       | 22  2   22    22   22  2 2     2   222 2 |  6.                     ",
                    "            336663733      |  222 222 22222  222 222   22222 2222  22 | 36                      ",
                    "               66733                                                   3667                     ",
                    "               66                                                       66     7 3              ",
                    "         .     667      .                         3                    766    733               ",
                    "        .7     666 3    7       3                 7                    666 .  67                ",
                    "         .6  37676767  6.6 77  66 7    3      . 7 77 66 7  7.  63 77 7666667 67  67             ",
                    "   .  77  67 76677666666666666666666  763  6 .77676666666666666666666666666666666667 67. 7      ",
                    "   7777666666666666676777776666777666666666666666667677777776677666777776677777667666666677737  ",
                    " 777  77767766677777 7 77 777777 77777667777776766777777 77777 7777777  777   77777776677 777 77",
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7 ",
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7    ",
                    "     7           7      7              7      7                    7                  7     7   ",

                
                
                
                
                
                ----------
                Ch5: Logo and flowery 1st plains
                    "                           |             7                     7      |                         ",
                    "                           |  7  .3    3 33  3 33 3   3.       33 3   |                         ",
                    "                           |  32 3223 322223.3222223 3222   3 322223  |                         ",
                    "                           | 32 22  232   2232     222  23 7332    2  |                         ",
                    "                           | 200  1022 0000 2200001  200 2 322 00012  |                         ",
                    "                           | 210  00  100001  100001  10 222  1000002 |                         ",
                    "                           |2200  00  01  00  00  00  10      00  01 2|                         ",
                    "                           |2 001000  00      011100  00      010011 2|                         ",
                    "                           |2 000001  00      11100   01      000111 2|                         ",
                    "                           |2 00  01  10  11  11 000  000     10  11 2|                         ",
                    "                           |2 01  11  000001  11  10  001111  00  11 2|                         ",
                    "                           | 200  11   1000   11  00 2 11111  01  112 |                         ",
                    "                           | 22  2   22    22   22  2 2     2   222 2 |                         ",
                    "                           |  222 222 22222  222 222   22222 2222  22 |                         ",
                    "                                                                                                ",
                    "                                                                               7 3              ",
                    "         .              .                         3                           733               ",
                    "        .7              7                         7                        .  67                ",
                    "         .6  3     67  6.6 77  66 7    3      . 7 77 66 7  7   63  77 6   77 67  67             ",
                    "   .  7   67 66 7 666666666666666666  763  6 .77676666666666666666666666666666666667 67. 7      ",
                    "   7777666666666666676777776666777666666666666666667677777776677666777776677777667666666677737  ",
                    " 777  77767766677777 7 77 777777 77777667777776766777777 77777 7777777  777   77777776677 777 77",
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7 ",
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7    ",
                    "     7           7      7              7      7                    7                  7     7   ",
                
                
                
                
                
                
                ----------
                Ch4: Combined logo and 1st-layer grassy plain
                    "                           |             7                     7      |                         ",
                    "                           |  7  .3    3 33  3 33 3   3.       33 3   |                         ",
                    "                           |  32 3223 322223.3222223 3222   3 322223  |                         ",
                    "                           | 32 22  232   2232     222  23 7332    2  |                         ",
                    "                           | 200  1022 0000 2200001  200 2 322 00012  |                         ",
                    "                           | 210  00  100001  100001  10 222  1000002 |                         ",
                    "                           |2200  00  01  00  00  00  10      00  01 2|                         ",
                    "                           |2 001000  00      011100  00      010011 2|                         ",
                    "                           |2 000001  00      11100   01      000111 2|                         ",
                    "                           |2 00  01  10  11  11 000  000     10  11 2|                         ",
                    "                           |2 01  11  000001  11  10  001111  00  11 2|                         ",
                    "                           | 200  11   1000   11  00 2 11111  01  112 |                         ",
                    "                           | 22  2   22    22   22  2 2     2   222 2 |                         ",
                    "                           |  222 222 22222  222 222   22222 2222  22 |                         ",
                    "                                                                                                ",
                    "                                                                                                ",
                    "                  666666666666666666              666666666666666666666666666666666             ",
                    "   7777666666666666677777777667777766666666666666667677777776677667777776777777667666666677777  ",
                    " 777  77777766677777 7 77 777777 7 777667777776766777777 77777 7777777  777   77777776677 777 77",
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7 ",
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7    ",
                



                 
                ----------
                Ch3b: 1st-layer grassy plain 
                    "                  666666666666666666              666666666666666666666666666666666             ",
                    "   7777666666666666677777777667777766666666666666667677777776677667777776777777667666666677777  ",
                    " 777  77777766677777 7 77 777777 7 777667777776766777777 77777 7777777  777   77777776677 777 77",
                    "7    7 77777777777 77   77  777   7 7777777777777777 77   77    777         7      7777777  7 7 ",
                    "   7    7   77  7    7        7       7   7 77   7       7    7  7        7       7 77  7  7    ",
                
                
                
                
                
                ----------
                Ch3: Added flowers and grass on border tops, letters have variation texture
                    "             7                     7      ",
                    "  7  .3    3 33  3 33 3   3.       33 3   ",
                    "  32 3223 322223.3222223 3222   3 322223  ",
                    " 32 22  232   2232     222  23 7332    2  ",
                    " 200  1022 0000 2200001  200 2 322 00012  ",
                    " 210  00  100001  100001  10 222  1000002 ",
                    "2200  00  01  00  00  00  10      00  01 2",
                    "2 001000  00      011100  00      010011 2",
                    "2 000001  00      11100   01      000111 2",
                    "2 00  01  10  11  11 000  000     10  11 2",
                    "2 01  11  000001  11  10  001111  00  11 2",
                    " 200  11   1000   11  00 2 11111  01  112 ",
                    " 22  2   22    22   22  2 2     2   222 2 ",
                    "  222 222 22222  222 222   22222 2222  22 ",
                
                
                

                
                ----------
                Ch2: Letters are spotted and bordered
                    "   2  22   2222  222222   222      2222   ",
                    "  2 22  2 2   22 2     222  2     2    2  ",
                    " 200  1022 0000 2200001  200 2  22 00012  ",
                    " 210  00  100001  100001  10 222  1000002 ",
                    "2200  00  01  00  00  00  10      00  01 2",
                    "2 001000  00      011100  00      010011 2",
                    "2 000001  00      11100   01      000111 2",
                    "2 00  01  10  11  11 000  000     10  11 2",
                    "2 01  11  000001  11  10  001111  00  11 2",
                    " 200  11   1000   11  00 2 11111  01  112 ",
                    " 22  2   22    22   22  2 2     2   222 2 ",
                    "  222 222 22222  222 222   22222 2222  22 ",


                
                
                
                ----------
                Ch1: Settled 'HCRLA' font design
                    "  00  00   0000   00000   00       0000   ",
                    "  00  00  000000  000000  00      000000  ",
                    "  00  00  00  00  00  00  00      00  00  ",
                    "  000000  00      000000  00      000000  ",
                    "  000000  00      00000   00      000000  ",
                    "  00  00  00  00  00 000  000     00  00  ",
                    "  00  00  000000  00  00  000000  00  00  ",
                    "  00  00   0000   00  00   00000  00  00  ",
                
                
                
                ----------
                Ch0: First try
                    "  ",
                    "  00     200   ",
                    " 200      01   ",
                    "  01      00   ",
                    "  102    201   ",
                    "  1000100100   ",
                    "  0010010001   ",
                    "  103    200   ",
                    "  01      10   ",
                    "  00      01   ",
                    "  10      00   ",
                    "  ",

                ******************* */


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

                // pre-style prints
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

                    case 6:
                        HSNLPrint(0, 2);
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
                            int padding = (bufferWidth - lineWidth) / 2 - (foolProofPrintingQ ? 1 : 0); //(int)((bufferWidth - lineWidth) / 4f);
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
                                if (csix == 0 && foolProofPrintingQ)
                                    Text(" "); // Text(">");

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
                                    //if (csix + 1 < styleLine.Length)
                                    if (csix < styleLine.Length)
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

                    case 6:
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
    }
}
