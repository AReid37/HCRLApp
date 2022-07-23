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
    }
}
