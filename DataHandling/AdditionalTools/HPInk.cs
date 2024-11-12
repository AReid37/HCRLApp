using ConsoleFormat;
using System;

namespace HCResourceLibraryApp.Layout
{
    /// <summary>Home Page Ink. A simple struct to help with color management for displays.</summary>
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

        public override string ToString()
        {
            string dither = "none";
            if (inkPattern.HasValue)
                dither = inkPattern.Value.ToString();
            string str = $"HPInk :: id [{id}]   col [{inkCol}]   dither [{dither}]";
            return str;
        }
    }
}
