using System;
using System.Collections.Generic;
using ConsoleFormat;

namespace HCResourceLibraryApp.DataHandling
{
    public class Preferences : DataHandlerBase
    {
        #region props / fields
        const Color defNormal = Color.Gray, defHighlight = Color.Yellow, defAccent = Color.DarkGray, defCorrection = Color.Green, defIncorrection = Color.Red, defWarning = Color.Yellow, defHeading1 = Color.White, defHeading2 = Color.Gray, defInput = Color.Yellow;
        Color _normal, _highlight, _accent, _correction, _incorrection, _warning, _heading1, _heading2, _input;

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
        //      line -> ??

        internal override bool EncodeToSharedFile()
        {
            List<string> prefDataLines = new List<string>();

            // compile the data
            /// color
            prefDataLines.Add($"{Normal.Encode()} {Highlight.Encode()} {Accent.Encode()} {Correction.Encode()} {Incorrection.Encode()} {Warning.Encode()} {Heading1.Encode()} {Heading2.Encode()} {Input.Encode()}".Replace(" ", AstSep));
            /// dimensions
            /// tbd...

            // encode data
            return Base.FileWrite(false, commonFileTag, prefDataLines.ToArray());
        }


        // detecting "changes made" could just be a comparision between values of two different Preferences objects
    }
}
