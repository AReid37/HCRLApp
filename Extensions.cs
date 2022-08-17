using System;
using System.Collections;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp
{
    public static class Extensions
    {
        // GENERAL
        /// <summary>Checks if a string value is not null, empty, or whitespaced.</summary>
        public static bool IsNotNEW(this string s)
        {
            bool hasVal = false;
            if (s != null)
                if (s != "")
                    if (s.Replace(" ", "").Length != 0)
                        hasVal = true;
            //System.Diagnostics.Debug.WriteLine($"'{s}' has value? {hasVal}");
            return hasVal;
        }
        /// <summary>Checks if a string value is not null or empty.</summary>
        public static bool IsNotNE(this string s)
        {
            bool hasVal = false;
            if (s != null)
                if (s != "")
                    hasVal = true;
            return hasVal;
        }
        /// <summary>Checks if a string value is null or empty.</summary>
        public static bool IsNE(this string s)
        {
            return !s.IsNotNE();
        }
        /// <summary>Checks if a string value is null, empty, or whitespaced.</summary>
        public static bool IsNEW(this string s)
        {
            return !s.IsNotNEW();
        }
        public static bool IsNotNull(this char c)
        {
            return c != '\0';
        }
        public static bool IsNotNull(this char? c)
        {
            bool notNull = c.HasValue;
            if (notNull)
                notNull = c != '\0';
            return notNull;
        }
        /// <summary>Checks if a collection (array, list) is not null and has at least one element.</summary>
        public static bool HasElements(this ICollection col)
        {
            bool hasElements = false;
            if (col != null)
                if (col.Count != 0)
                    hasElements = true;
            return hasElements;
        }        
        public static int Clamp(this int i, int valueA, int valueB)
        {
            return valueA <= valueB ? Math.Clamp(i, valueA, valueB) : Math.Clamp(i, valueB, valueA);
        }
        public static short Clamp(this short s, short valueA, short valueB)
        {
            return valueA <= valueB ? Math.Clamp(s, valueA, valueB) : Math.Clamp(s, valueB, valueA);
        }
        public static float Clamp(this float f, float valueA, float valueB)
        {
            return valueA <= valueB ? Math.Clamp(f, valueA, valueB) : Math.Clamp(f, valueB, valueA);
        }
        public static bool IsWithin(this int i, int valueA, int valueB)
        {
            // A greater than B ?  ret (A >= i >= B)  else  ret(A <= i <= B)
            return valueA >= valueB ? (valueA >= i && i >= valueB) : (valueA <= i && i <= valueB);
        }
        public static bool IsWithin(this short s, short valueA, short valueB)
        {
            return valueA >= valueB ? (valueA >= s && s >= valueB) : (valueA <= s && s <= valueB);
        }
        /// <remarks>Inclusive of both Ranges (<paramref name="rangeA"/> and <paramref name="rangeB"/>)</remarks>
        public static int Random(int rangeA, int rangeB)
        {
            Random rnd = new Random();
            return rangeA <= rangeB ? rnd.Next(rangeA, rangeB + 1) : rnd.Next(rangeB, rangeA + 1);
        }

        // PRERFERENCES
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
            if (s.IsNotNEW())
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

            //Dbug.SingleLog("Extensions.Decode(this s, out c)", $"Parsed [{parsed}]; string [{s}], returned color [{c}]");
            return parsed;
        }
        public static float GetScaleFactorH(this DimHeight dh)
        {
            float heightScale = dh switch
            {
                DimHeight.Squished => 0.4f,
                DimHeight.Short => 0.5f,
                DimHeight.Normal => 0.6f,
                DimHeight.Tall => 0.8f,
                DimHeight.Fill => 1.0f,
                _ => 0.0f
            };
            return heightScale;
        }
        public static float GetScaleFactorW(this DimWidth dw)
        {
            float widthScale = dw switch
            {
                DimWidth.Thin => 0.4f,
                DimWidth.Slim => 0.5f,
                DimWidth.Normal => 0.6f,
                DimWidth.Broad => 0.8f,
                DimWidth.Fill => 1.0f,
                _ => 0.0f
            };
            return widthScale;
        }
    }
}
