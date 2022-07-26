using System;
using System.Collections;
using ConsoleFormat;

namespace HCResourceLibraryApp
{
    public static class Extensions
    {
        /// <summary>Checks if a string value is not null, empty, or whitespaced.</summary>
        public static bool HasValue(this string s)
        {
            bool hasVal = false;
            if (s != null)
                if (s != "")
                    if (s.Replace(" ", "").Length != 0)
                        hasVal = true;
            //System.Diagnostics.Debug.WriteLine($"'{s}' has value? {hasVal}");
            return hasVal;
        }
        public static bool HasElements(this ICollection col)
        {
            bool hasElements = false;
            if (col != null)
                if (col.Count != 0)
                    hasElements = true;
            return hasElements;
        }

        public static string Encode(this Color c)
        {
            /// IS THIS REALLY OKAY?
            /// Yes! Take a look:
            ///     Red -> Rd
            ///     Maroon -> Mrn
            ///     Yellow -> Yllw
            ///     Orange -> Orng
            ///     Green -> Grn
            ///     Forest -> Frst
            ///     Cyan -> Cyn
            ///     Teal -> Tl
            ///     Blue -> Bl
            ///     NavyBlue -> NvyBl
            ///     Magenta -> Mgnt
            ///     Purple -> Prpl
            ///     White -> Wht
            ///     Gray -> Gry
            ///     DarkGray -> DrkGry
            ///     Black -> Blck
            /// No dupes
            string newC = c.ToString().Replace("a", "").Replace("e", "").Replace("i", "").Replace("o", "").Replace("u", "");
            return newC;
        }
        public static bool Decode(this string s, out Color c)
        {
            bool parsed = false;
            c = Color.Black;
            if (s.HasValue())
            {
                Color[] colors = (Color[])Enum.GetValues(typeof(Color));
                if (colors.HasElements())
                {
                    for (int i = 0; i < colors.Length && !parsed; i++)
                        if (colors[i].Encode() == s)
                        {
                            c = colors[i];
                            parsed = true;
                        }
                }
            }

            Dbug.SingleLog("Extensions.Decode(this s, out c)", $"Parsed [{parsed}]; string [{s}], returned color [{c}]");
            return parsed;
        }
    }
}
