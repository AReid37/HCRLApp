using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    public enum DimHeight
    {
        /// <summary>20% height scale.</summary>
        Squished,
        /// <summary>40% height scale.</summary>
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
        /// <summary>20% width scale.</summary>
        Thin,
        /// <summary>40% width scale.</summary>
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
        Color _normal, _highlight, _accent, _correction, _incorrection, _warning, _heading1, _heading2, _input;
        DimHeight _dimHeightScale;
        const DimHeight defHeightScale = DimHeight.Normal;
        DimWidth _dimWidthScale;
        const DimWidth defWidthScale = DimWidth.Normal;


        // PROPS
        // -- colors --
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
            List<string> prefDataLines = new List<string>();

            // compile the data
            /// color
            prefDataLines.Add($"{Normal.Encode()} {Highlight.Encode()} {Accent.Encode()} {Correction.Encode()} {Incorrection.Encode()} {Warning.Encode()} {Heading1.Encode()} {Heading2.Encode()} {Input.Encode()}".Replace(" ", AstSep));
            /// dimensions
            prefDataLines.Add($"{HeightScale}{AstSep}{WidthScale}");

            // encode data
            return Base.FileWrite(false, commonFileTag, prefDataLines.ToArray());
        }
        protected override bool DecodeFromSharedFile()
        {
            Dbug.StartLogging("Preferences.DecodeFromSharedFile()");
            bool decodedPrefsDataQ = Base.FileRead(commonFileTag, out string[] prefsDataLines);

            Dbug.Log($"Fetching file data (using tag '{commonFileTag}')  //  Successfully read from file? {decodedPrefsDataQ};  {nameof(prefsDataLines)} has elements? {prefsDataLines.HasElements()}");
            if (decodedPrefsDataQ && prefsDataLines.HasElements())
            {
                for (int line = 0; line < prefsDataLines.Length && decodedPrefsDataQ; line++)
                {
                    string dataLine = prefsDataLines[line];
                    Dbug.Log($"Decoding  L{line + 1}| {dataLine}");
                    Dbug.SetIndent(1);

                    switch (line)
                    {
                        // color
                        case 0:
                            Dbug.LogPart(">> Decoding Color -->  ");
                            string[] colorsText = dataLine.Split(AstSep);
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
                            string[] dimsText = dataLine.Split(AstSep);
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
                    Dbug.SetIndent(0);
                }

                
            }
            Dbug.Log($"End decoding for Preferences -- successful decoding? {decodedPrefsDataQ}");
            Dbug.EndLogging();
            return decodedPrefsDataQ;
        }
        // decodeFromSharedFile --> previousSelf must become a new MemberwiseClone from decoded data

        public bool ChangesMade()
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


        public void GetScreenDimensions(out int trueHeight, out int trueWidth)
        {
            int maxHeight = Console.LargestWindowHeight;
            int maxWidth = Console.LargestWindowWidth;

            float heightScale = HeightScale.GetScaleFactorH(), widthScale = WidthScale.GetScaleFactorW();
            trueHeight = (int)(maxHeight * heightScale);
            trueWidth = (int)(maxWidth * widthScale);
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
