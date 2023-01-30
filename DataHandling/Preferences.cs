using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    public enum DimHeight
    {
        /// <summary>40% height scale.</summary>
        Squished,
        /// <summary>50% height scale.</summary>
        Short,
        /// <summary>60% height scale.</summary>
        Normal,
        /// <summary>80% height scale.</summary>
        Tall,
        /// <summary>100% height scale.</summary>
        Fill
    }
    public enum DimWidth
    {
        /// <summary>40% width scale.</summary>
        Thin,
        /// <summary>50% width scale.</summary>
        Slim,
        /// <summary>60% width scale.</summary>
        Normal,
        /// <summary>80% width scale.</summary>
        Broad,
        /// <summary>100% width scale.</summary>
        Fill
    }

    public class Preferences : DataHandlerBase
    {
        #region props / fields
        Preferences previousSelf;
        const Color defNormal = Color.Gray, defHighlight = Color.Yellow, defAccent = Color.DarkGray, defCorrection = Color.Green, defIncorrection = Color.Red, defWarning = Color.Yellow, defHeading1 = Color.White, defHeading2 = Color.Gray, defInput = Color.Yellow;
        const DimHeight defHeightScale = DimHeight.Normal;
        const DimWidth defWidthScale = DimWidth.Normal;
        Color _normal, _highlight, _accent, _correction, _incorrection, _warning, _heading1, _heading2, _input;
        DimHeight _dimHeightScale;
        DimWidth _dimWidthScale;


        // PROPS
        // -- colors --
        /// <summary>
        ///     A quick way of getting and setting the foreground element colors by their index.
        /// </summary>
        /// <param name="colIndex">Foreground element indicies ::  0,Nor - 1,Hig - 2,Acc - 3,Cor - 4,Inc - 5,War - 6,He1 - 7,He2 - 8,Inp.</param>
        /// <returns>The color of the appropriate foreground element.</returns>
        public Color this[int colIndex]
        {
            get
            {
                colIndex = colIndex.Clamp(0, 8);
                Color elementColor = colIndex switch
                {
                    0 => Normal,
                    1 => Highlight,
                    2 => Accent,
                    3 => Correction,
                    4 => Incorrection,
                    5 => Warning,
                    6 => Heading1,
                    7 => Heading2,
                    8 => Input,
                    _ => Color.Black // should never be hit
                };
                return elementColor;
            }
            set
            {
                colIndex = colIndex.Clamp(0, 8);
                switch (colIndex)
                {
                    case 0:
                        Normal = value;
                        break;

                    case 1:
                        Highlight = value;
                        break;

                    case 2:
                        Accent = value;
                        break;

                    case 3:
                        Correction = value;
                        break;

                    case 4:
                        Incorrection = value;
                        break;

                    case 5:
                        Warning = value;
                        break;

                    case 6:
                        Heading1 = value;
                        break;

                    case 7:
                        Heading2 = value;
                        break;

                    case 8:
                        Input = value;
                        break;
                }
            }
        }
        public Color Normal
        {
            get => _normal;
            set => _normal = value.Equals(Color.Black) ? defNormal : value;
        }
        public Color Highlight
        {
            get => _highlight;
            set => _highlight = value.Equals(Color.Black) ? defHighlight : value;
        }
        public Color Accent
        {
            get => _accent;
            set => _accent = value.Equals(Color.Black) ? defAccent : value;
        }
        public Color Correction
        {
            get => _correction;
            set => _correction = value.Equals(Color.Black) ? defCorrection : value;
        }
        public Color Incorrection
        {
            get => _incorrection;
            set => _incorrection = value.Equals(Color.Black) ? defIncorrection : value;
        }
        public Color Warning
        {
            get => _warning;
            set => _warning = value.Equals(Color.Black) ? defWarning : value;
        }
        public Color Heading1
        {
            get => _heading1;
            set => _heading1 = value.Equals(Color.Black) ? defHeading1 : value;
        }
        public Color Heading2
        {
            get => _heading2;
            set => _heading2 = value.Equals(Color.Black) ? defHeading2 : value;
        }
        public Color Input
        {
            get => _input;
            set => _input = value.Equals(Color.Black) ? defInput : value;
        }
        // -- dims --
        public DimHeight HeightScale
        {
            get => _dimHeightScale;
            set => _dimHeightScale = value;
        }
        public DimWidth WidthScale
        {
            get => _dimWidthScale;
            set => _dimWidthScale = value;
        }
        #endregion

        public Preferences() : base()
        {
            commonFileTag = "pref";
            // initialize
            Normal = Color.Black;
            Highlight = Color.Black;
            Accent = Color.Black;
            Correction = Color.Black;
            Incorrection = Color.Black;
            Warning = Color.Black;
            Heading1 = Color.Black;
            Heading2 = Color.Black;
            Input = Color.Black;
            HeightScale = defHeightScale;
            WidthScale = defWidthScale;

            previousSelf = (Preferences)this.MemberwiseClone();
        }

        // FILE SYNTAX - PREFERENCES
        //  Colors (1 line)
        //  Dimensions (1 line)
        //
        //  Colors Syntax
        //      tag ->  {commontag}
        //      line -> {nor}*{hig}*{acc}*{cor}*{inc}*{war}*{hd1}*{hd2}*{inp}
        //  Dimensions Syntax
        //      tag ->  {commontag}
        //      line -> {hei}*{wid}

        protected override bool EncodeToSharedFile()
        {
            Dbug.IgnoreNextLogSession();
            Dbug.StartLogging("Preferences.EncodeToSharedFile()");
            List<string> prefDataLines = new List<string>();

            // compile the data
            /// color
            prefDataLines.Add($"{Normal.Encode()} {Highlight.Encode()} {Accent.Encode()} {Correction.Encode()} {Incorrection.Encode()} {Warning.Encode()} {Heading1.Encode()} {Heading2.Encode()} {Input.Encode()}".Replace(" ", Sep));
            Dbug.Log($"L1 Enc  //  tag [{commonFileTag}]  //  {prefDataLines[0]}");
            /// dimensions
            prefDataLines.Add($"{HeightScale}{Sep}{WidthScale}");
            Dbug.Log($"L2 Enc  //  tag [{commonFileTag}]  //  {prefDataLines[1]}");
            Dbug.EndLogging();

            // set previous self to current self
            previousSelf = (Preferences)this.MemberwiseClone();

            // encode data
            return Base.FileWrite(false, commonFileTag, prefDataLines.ToArray());
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.IgnoreNextLogSession();
            Dbug.StartLogging("Preferences.DecodeFromSharedFile()");
            bool decodedPrefsDataQ = Base.FileRead(commonFileTag, out string[] prefsDataLines);

            Dbug.Log($"Fetching file data (using tag '{commonFileTag}')  //  Successfully read from file? {decodedPrefsDataQ};  {nameof(prefsDataLines)} has elements? {prefsDataLines.HasElements()}");
            if (decodedPrefsDataQ && prefsDataLines.HasElements())
            {
                for (int line = 0; line < prefsDataLines.Length && decodedPrefsDataQ; line++)
                {
                    string dataLine = prefsDataLines[line];
                    Dbug.Log($"Decoding  L{line + 1}| {dataLine}");
                    Dbug.NudgeIndent(true);

                    switch (line)
                    {
                        // color
                        case 0:
                            Dbug.LogPart(">> Decoding Color -->  ");
                            string[] colorsText = dataLine.Split(Sep);
                            if (colorsText.HasElements())
                            {
                                for (int ctIx = 0; ctIx < colorsText.Length && decodedPrefsDataQ; ctIx++)
                                {
                                    string clsTxt = colorsText[ctIx];
                                    bool parsedColor = clsTxt.Decode(out Color foreColor);

                                    string foreColName = ctIx switch
                                    {
                                        0 => "Normal",
                                        1 => "Highlight",
                                        2 => "Accent",
                                        3 => "Correction",
                                        4 => "Incorrection",
                                        5 => "Warning",
                                        6 => "Heading1",
                                        7 => "Heading2",
                                        8 => "Input",
                                        _ => null
                                    };

                                    Dbug.Log($"Parsed color for '{foreColName}'? {parsedColor} [got '{foreColor}']");
                                    if (parsedColor)
                                    {
                                        switch (ctIx)
                                        {
                                            case 0:
                                                Normal = foreColor;
                                                break;

                                            case 1:
                                                Highlight = foreColor;
                                                break;

                                            case 2:
                                                Accent = foreColor;
                                                break;

                                            case 3:
                                                Correction = foreColor;
                                                break;

                                            case 4:
                                                Incorrection = foreColor;
                                                break;

                                            case 5:
                                                Warning = foreColor;
                                                break;

                                            case 6:
                                                Heading1 = foreColor;
                                                break;

                                            case 7:
                                                Heading2 = foreColor;
                                                break;

                                            case 8:
                                                Input = foreColor;
                                                break;
                                        }
                                    }
                                    decodedPrefsDataQ = parsedColor;
                                }
                            }
                            Dbug.Log("<< End Decoding Color");
                            break;

                        // dimensions
                        case 1:
                            Dbug.Log(">> Decoding Window Dims -->  ");
                            string[] dimsText = dataLine.Split(Sep);
                            if (dimsText.HasElements())
                            {
                                for (int dimIx = 0; dimIx < dimsText.Length; dimIx++)
                                {
                                    string dimText = dimsText[dimIx];
                                    Dbug.Log($"Parsing for '{(dimIx == 0 ? "Height" : "Width")} Scale' -->  Recieved value '{dimText}'");

                                    if (dimText.IsNotNEW())
                                    {
                                        // height 
                                        if (dimIx == 0)
                                        {
                                            DimHeight[] dimsH = (DimHeight[])Enum.GetValues(typeof(DimHeight));
                                            if (dimsH.HasElements())
                                            {
                                                bool parseHeight = false;
                                                for (int dh = 0; dh < dimsH.Length && !parseHeight; dh++)
                                                {
                                                    parseHeight = dimText == dimsH[dh].ToString();
                                                    if (parseHeight)
                                                        HeightScale = dimsH[dh];

                                                    Dbug.LogPart($"-> '{dimsH[dh]}'? [{(parseHeight ? "T": "f")}];  ");
                                                }
                                            }
                                        }
                                        // width
                                        if (dimIx == 1)
                                        {
                                            DimWidth[] dimsW = (DimWidth[])Enum.GetValues(typeof(DimWidth));
                                            if (dimsW.HasElements())
                                            {
                                                bool parsedWidth = false;
                                                for (int dw = 0; dw < dimsW.Length && !parsedWidth; dw++)
                                                {
                                                    parsedWidth = dimText == dimsW[dw].ToString();
                                                    if (parsedWidth)
                                                        WidthScale = dimsW[dw];

                                                    Dbug.LogPart($"-> '{dimsW[dw]}'? [{(parsedWidth ? "T" : "f")}];  ");
                                                }
                                            }
                                        }
                                    }
                                    Dbug.Log($" .. End parsing for '{(dimIx == 0 ? "Height" : "Width")} Scale'");
                                }
                            }
                            Dbug.Log("<< End Decoding Window Dims");
                            break;
                    }
                    Dbug.NudgeIndent(false);
                }

                // decodeFromSharedFile --> previousSelf must become a new MemberwiseClone from decoded data
                previousSelf = (Preferences)this.MemberwiseClone();
                Dbug.Log($"Cloned preferences data to 'previousSelf'; Confirmed to be the same? {!ChangesMade()}");
            }
            Dbug.Log($"End decoding for Preferences -- successful decoding? {decodedPrefsDataQ}");
            Dbug.EndLogging();
            return decodedPrefsDataQ;
        }

        public override bool ChangesMade()
        {
            // detecting "changes made" could just be a comparision between values of two different Preferences objects
            bool endChecks = false;
            bool changesMade = false;
            for (int checkNum = 0; !endChecks; checkNum++)
            {
                changesMade = checkNum switch
                {
                    0 => Normal != previousSelf.Normal,
                    1 => Highlight != previousSelf.Highlight,
                    2 => Accent != previousSelf.Accent,
                    3 => Correction != previousSelf.Correction,
                    4 => Incorrection != previousSelf.Incorrection,
                    5 => Warning != previousSelf.Warning,
                    6 => Heading1 != previousSelf.Heading1,
                    7 => Heading2 != previousSelf.Heading2,
                    8 => Input != previousSelf.Input,
                    9 => HeightScale != previousSelf.HeightScale,
                    10 => WidthScale != previousSelf.WidthScale,
                    _ => false
                };

                if (changesMade || checkNum >= 10)
                    endChecks = true;
            }
            return changesMade;
        }
        /// <summary>Compares another <see cref="Preferences"/> instance to this instance for similarity in values.</summary>
        /// <returns>A boolean representing whether the values of the compared preferences matches those of this instance.</returns>
        public bool Equals(Preferences other)
        {
            bool endChecks = false;
            int countNoMatch = 0;
            for (int checkMatch = 0; !endChecks; checkMatch++)
            {
                bool match = checkMatch switch
                {
                    0 => Normal == other.Normal,
                    1 => Highlight == other.Highlight,
                    2 => Accent == other.Accent,
                    3 => Correction == other.Correction,
                    4 => Incorrection == other.Incorrection,
                    5 => Warning == other.Warning,
                    6 => Heading1 == other.Heading1,
                    7 => Heading2 == other.Heading2,
                    8 => Input == other.Input,
                    9 => HeightScale == other.HeightScale,
                    10 => WidthScale == other.WidthScale,
                    _ => false
                };
                if (!match)
                    countNoMatch++;

                if (checkMatch >= 10)
                    endChecks = true;
            }
            return countNoMatch == 0;
        }
        public Preferences ShallowCopy()
        {
            return (Preferences)this.MemberwiseClone();
        }        

        public void GetScreenDimensions(out int trueHeight, out int trueWidth)
        {
            int maxHeight = Console.LargestWindowHeight;
            int maxWidth = Console.LargestWindowWidth;

            const int rfw = 4, rfh = 2; // rounding factor
            float heightScale = HeightScale.GetScaleFactorH(), widthScale = WidthScale.GetScaleFactorW();
            trueHeight = ((int)(maxHeight * heightScale / rfh)) * rfh; // always a (smaller) multiple of rounding factor
            trueWidth = ((int)(maxWidth * widthScale / rfw)) * rfw; // always a (smaller) multiple of rounding factor
        }
        public static void GetScreenDimensions(out int trueHeight, out int trueWidth, DimHeight height, DimWidth width)
        {
            int maxHeight = Console.LargestWindowHeight;
            int maxWidth = Console.LargestWindowWidth;

            trueHeight = (int)(maxHeight * height.GetScaleFactorH());
            trueWidth = (int)(maxWidth * width.GetScaleFactorW());
        }

    }
}
