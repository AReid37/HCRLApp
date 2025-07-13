using System;
using System.Collections;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp
{
    public static class Extensions
    {
        // GENERAL  --> STRINGS and CHARS
        #region general_stringsNChars
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
        /// <summary>Checks if a string value is not empty or whitespace.</summary>
        public static bool IsNotEW(this string s)
        {
            bool hasVal = false;
            if (s == null)
                hasVal = true;
            else
            {
                if (s != "")
                    if (s.Replace(" ", "").Length != 0)
                        hasVal = true;
            }
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
        /// <summary>Checks if a string value is empty or whistespaced.</summary>
        public static bool IsEW(this string s)
        {
            return !s.IsNotEW();
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
        /// <returns>An integer representing the number of occurences of the character <paramref name="c"/> within this string.</returns>
        public static int CountOccuringCharacter(this string s, char c)
        {
            int counts = 0;
            if (s.IsNotNE() && c != '\0')
            {
                foreach (char cs in s)
                    if (cs == c)
                        counts++;
            }
            return counts;
        }
        /// <summary>Limits the length of a string and uses <paramref name="clampingSuffix"/> to signify this restriction where applicable.</summary>
        /// <param name="maxLength">Must be greater than or equal to 5.</param>
        /// <param name="clampingSuffix">Maximum of 3 characters.</param>
        /// <returns>A clamped string. The string remains unclamped if it is too short, null, or empty.</returns>
        public static string Clamp(this string s, int maxLength, string clampingSuffix = null)
        {
            const int clampSymMaxLen = 3, minimumClampLength = 5;
            string fullStr = s;
            
            maxLength = Clamp(maxLength, minimumClampLength, maxLength);
            if (s.IsNotNEW())
            {
                /// ensure clamping suffix is within max length
                int clampSymLen = 0;
                if (clampingSuffix.IsNotNEW())
                {
                    if (!clampingSuffix.Length.IsWithin(0, clampSymMaxLen))
                        clampingSuffix = clampingSuffix.Remove(clampSymMaxLen);
                    clampSymLen = clampingSuffix.Length;
                }
                
                /// clamp string based on max length and clamp symbol
                if (s.Length > maxLength - clampSymLen)
                {
                    s = s.Remove(maxLength - clampSymLen);
                    fullStr = $"{s}{clampingSuffix}";
                }
            }
            return fullStr;
        }
        /// <summary>Limits the length of strings surrouding <paramref name="focusedWord"/> based on the value of <paramref name="focusRightQ"/> and using <paramref name="clampingSuffix"/> to signify this restriction where applicable.</summary>
        /// <param name="distance">Must be greater than or equal to 5.</param>
        /// <param name="clampingSuffix">Maximum of 3 characters.</param>
        /// <param name="focusedWord">Word to clamp around (only the first case-sensitive instance is focused). </param>
        /// <param name="focusRightQ">
        ///     Clamps to the left or right of <paramref name="focusedWord"/> dependent on boolean value: If <c>true</c>, will remove any strings to the left and clamp any strings to the right.
        ///     <br></br>If <c>null</c>, will equally clamp left and right of the <paramref name="focusedWord"/>.
        ///     <br></br>The relation '<c>TRUE|NULL|FALSE</c>' can be seen as '<c>START|CENTER|END</c>' with regards to <paramref name="focusedWord"/>'s position in final string.
        /// </param>        
        public static string Clamp(this string s, int distance, string clampingSuffix, string focusedWord, bool? focusRightQ)
        {
            const int minimumDistance = 5, clampSymMaxLen = 3, nonIndex = -1;
            string fullStr = s;

            distance = Clamp(distance, minimumDistance, distance);
            if (s.IsNotNEW() && focusedWord.IsNotNEW())
            {
                // ensure clamping suffix is within max length
                int clampSymLen = 0;
                if (clampingSuffix.IsNotNE())
                {
                    if (!clampingSuffix.Length.IsWithin(0, clampSymMaxLen))
                        clampingSuffix = clampingSuffix.Remove(clampSymMaxLen);
                    clampSymLen = clampingSuffix.Length;
                }

                // find first instance of focused word and get left and right string sides
                int minimumDistanceToClamp = minimumDistance - clampSymLen;
                string[] splitStrings = null;
                if (s.Length - focusedWord.Length > minimumDistanceToClamp)
                { /// IF 'string length -minus- focused word' length is greater than 'the minimum distance to clamp (either side of string)': ...
                    
                    /// this grabs the first instance of focused word
                    int fWordLen = focusedWord.Length;
                    int fWordStartIx = nonIndex;
                    for (int fx = 0; fx <= s.Length - fWordLen && fWordStartIx == nonIndex; fx++)
                    {
                        string sPart = s.Substring(fx, fWordLen);
                        if (sPart == focusedWord)
                            fWordStartIx = fx;
                    }

                    /// gets the left and right and clamps or removes them based on value of nullable boolean 'focus...Q'
                    if (fWordStartIx != nonIndex)
                    {
                        /// Example
                        ///     s = "this is not the end"  
                        ///     focusedWord = "not"
                        ///     clampingSuffix = "..."
                        ///     ---
                        ///     clampSymLen = 3
                        ///     ---
                        ///     s.Length >= focusedWord.Length + ((5 - clampSymLen) * 2)  -> ??
                        ///     [19] >= [3] + ((5 - [3]) * 2)  -> ??
                        ///     19 >= 3 + (2 * 2)  --> ??
                        ///     19 >= 7 --> TRUE
                        ///     ---
                        ///     fWordLen = 3
                        ///     fWordIx = 8
                        ///     ---
                        ///     fLeft  should equal "this is "
                        ///     fRight should equal " the end"
                        ///     ...
                        ///     fLeft == s.Substring(0, fWordIx)
                        ///     fLeft == s.Substring(0, [8])
                        ///     fLeft == "this is "
                        ///               01234567    -> Length of '8' are indicies '0 ~ 7'
                        ///     fLeft == "this is "  is TRUE
                        ///     ....
                        ///     fRight == s.Substring(fWordIx + fWordLen + 1)
                        ///     fRight == s.Substring([8] + [3])
                        ///     fRight == s.Substring(11)
                        ///     fRight ==           " the end"
                        ///              "this is not" 
                        ///               0123456789
                        ///                    10 + 01  -> This substring starts at index '11' and continues towards the end of string
                        ///     fRight == " the end"   is TRUE
                        ///     
                        string fLeft = s.Substring(0, fWordStartIx);
                        string fRight = s.Substring(fWordStartIx + fWordLen);

                        /// is 'true' (focus left) if focus is 'null' or 'false'
                        bool clampLeft = !focusRightQ.HasValue;
                        /// is 'true' (focus right) if focus is 'null' or 'true'
                        bool clampRight = !focusRightQ.HasValue; 
                        if (focusRightQ.HasValue)
                        {
                            clampLeft = !focusRightQ.Value;
                            clampRight = focusRightQ.Value;
                        }

                        /// IF focus left: (IF left string can be clamped: clamp string and add suffix before); ELSE remove left string 
                        if (clampLeft)
                        {
                            if (fLeft.Length > distance - clampSymLen)
                            {
                                fLeft = fLeft.Substring(fLeft.Length - distance + clampSymLen);
                                fLeft = $"{clampingSuffix}{fLeft}";
                            }
                        }
                        else fLeft = "";
                        /// IF focus right: (IF right string can be clamped: clamp string and add suffix after); ELSE remove right string
                        if (clampRight)
                        {
                            if (fRight.Length > distance - clampSymLen)
                            {
                                fRight = fRight.Remove(distance - clampSymLen);
                                fRight = $"{fRight}{clampingSuffix}";
                            }
                        }
                        else fRight = "";

                        /// set value for left and right
                        splitStrings = new string[2] { fLeft, fRight };
                    }
                }

                // set clamped full string and return
                if (splitStrings.HasElements(2))
                    fullStr = $"{splitStrings[0]}{focusedWord}{splitStrings[1]}";
            }
            return fullStr;
        }
        /// <summary>
        ///   Recieves an array of numeric data IDs (as strings) then condenses any sequences of numbers into ranges (Ex. '0 1 2 4' becomes '0~2 4'). The numbers may be sorted before being condensed into ranges dependent on <paramref name="sortWordsQ"/>.
        /// </summary>
        /// <returns>A condensed string of numbers with number ranges. Is empty if no numbers were given.</returns>
        public static string CreateNumericDataIDRanges(string[] numbers, bool sortWordsQ = true)
        {
            string rangedNumbers = "";
            if (numbers.HasElements())
            {
                Dbg.StartLogging("Extensions.CreateNumericDataIDRanges(...)", out int tx);
                Dbg.ToggleThreadOutputOmission(tx);

                Dbg.Log(tx, $"Recieved '{numbers.Length}' numbers to create ranges from; Removing null/empty entries; ");
                List<string> fltNumbers = new();
                foreach (string numT in numbers)
                    if (numT.IsNotNEW())
                        fltNumbers.Add(numT);

                if (fltNumbers.HasElements() && sortWordsQ)
                    fltNumbers = fltNumbers.ToArray().SortWords();
                Dbg.Log(tx, $"Filtered down to '{fltNumbers.Count}' numbers; {(sortWordsQ? "Sorted numbers; " : "")}Creating Ranges; ");
                Dbg.Log(tx, $"LEGEND :: Range Enter '{{' -- Range Exit '}}' -- Base Num '@#' --  End Range Num '!' -- Last Num Ends Range '>' -- Incompatibility Range Break '*' -- Range Indicators [' .]");

                string baseNumT = null, baseOddNumT = null;
                bool withinRangeQ = false, withinOddRangeQ = false;
                for (int rx = 0; rx < fltNumbers.Count; rx++)
                {
                    string currNumT = fltNumbers[rx];
                    string prevNumT = rx > 0 ? fltNumbers[rx - 1] : null;
                    bool lastNumberQ = rx + 1 == fltNumbers.Count;

                    string numToPrint = "";
                    bool parsedCurrNum = int.TryParse(currNumT, out int currNum);
                    bool parsedPrevNum = int.TryParse(prevNumT, out int prevNum);

                    /// Normal Numbers
                    /// 5 6
                    if (parsedCurrNum && parsedPrevNum)
                    {
                        //Dbg.LogPart("\\");

                        /// IF this num is next in sequence; ELSE this num is beyond sequence 
                        if (prevNum + 1 == currNum)
                        {
                            /// is not within range |
                            if (!withinRangeQ)
                            {
                                Dbg.LogPart(tx, $" {{");
                                withinRangeQ = true;
                                baseNumT = prevNum.ToString();

                                Dbg.LogPart(tx, $"@{baseNumT}'{currNumT}");

                                /// IF there is preceding sequences AND there is base sequence number: remove last-printed number in sequence (iow. remove base number)
                                if (rangedNumbers.IsNotNE() && baseNumT.IsNotNE())
                                    rangedNumbers = rangedNumbers.Remove((rangedNumbers.Length - baseNumT.Length - 1).Clamp(0, rangedNumbers.Length - 1));

                                /// IF more numbers follow: print base sequence number; ELSE print base sequence number and this/last number
                                if (!lastNumberQ)
                                    numToPrint = baseNumT;
                                else
                                {
                                    Dbg.LogPart(tx, $"!}}> ");
                                    numToPrint = $"{baseNumT},{currNumT}";
                                }
                            }
                            /// is within range |
                            else
                            {
                                /// IF more numbers follow: don't print number; ELSE print sequence-ending/last number
                                if (!lastNumberQ)
                                    Dbg.LogPart(tx, $"'{currNumT}");
                                else
                                {
                                    Dbg.LogPart(tx, $"'{currNumT}!}}> ");
                                    numToPrint = $"~{currNumT}";
                                }
                            }
                        }
                        else
                        {
                            /// IF is within range (IF sequence of 2: print as normal; IF number to print unset, sequence of 3+: print sequence-ending number)
                            if (withinRangeQ)
                            {
                                if (int.TryParse(baseNumT, out int baseNum))
                                    if (baseNum + 1 == prevNum)
                                        numToPrint = $",{prevNumT},";
                                if (numToPrint.IsNE())
                                    numToPrint = $"~{prevNumT},";

                                Dbg.LogPart(tx, $"!}}");
                            }

                            Dbg.LogPart(tx, $" {currNumT}");
                            numToPrint += $"{currNumT},";

                            withinRangeQ = false;
                            baseNumT = null;
                        }
                    }
                    /// 'Odd' Numbers
                    /// 6_2 7-wet
                    else
                    {
                        //Dbg.LogPart("/");

                        /// IF is within range; (IF current num is odd AND previous is normal; print sequence-ending number, start odd number sequences)
                        if (withinRangeQ)
                        {
                            // .. 4 (5 5_0) 6 ..
                            if (!parsedCurrNum && parsedPrevNum)
                            {
                                if (int.TryParse(baseNumT, out int baseNum))
                                    if (baseNum + 1 == prevNum)
                                        numToPrint = $",{prevNumT},";
                                if (numToPrint.IsNE())
                                    numToPrint = $"~{prevNumT},";
                                Dbg.LogPart(tx, $"!}}*");

                                //Dbg.LogPart($" {currNumT}");
                                //numToPrint += $"{currNumT},";

                                withinRangeQ = false;
                                baseNumT = null;
                            }
                        }

                        /// IF is not within range
                        if (!withinRangeQ)
                        {
                            bool parsedPrevOddNum = TryGetEndingNumber(prevNumT, out string prevBaseT, out int prevEndNum);
                            bool parsedCurrOddNum = TryGetEndingNumber(currNumT, out string currBaseT, out int currEndNum);

                            // .. 0 (1_0 1_1) 2 ..
                            bool justPrintQ = true;
                            if (!parsedCurrNum && !parsedPrevNum)
                            {
                                /// IF parsed/fetched this and previous odd numbers
                                if (parsedPrevOddNum && parsedCurrOddNum)
                                {
                                    /// IF previous odd num and this odd num have the same preceding numbers/text: ...; ELSE print sequence ending number, end odd number sequence
                                    if (prevBaseT == currBaseT)
                                    {
                                        justPrintQ = false;
                                        /// IF this num is next in odd sequence; ELSE this num is beyond odd sequence 
                                        if (prevEndNum + 1 == currEndNum)
                                        {
                                            /// is not within odd range; ELSE is within odd range
                                            if (!withinOddRangeQ)
                                            {
                                                Dbg.LogPart(tx, $" {{");
                                                withinOddRangeQ = true;
                                                baseOddNumT = prevNumT;

                                                Dbg.LogPart(tx, $"@{prevNumT}.{currNumT}");

                                                /// IF there is preceding sequences AND there is odd base sequence number: remove last-printed number in sequence (iow. remove odd base number)
                                                if (rangedNumbers.IsNotNE() && baseOddNumT.IsNotNE())
                                                    rangedNumbers = rangedNumbers.Remove((rangedNumbers.Length - baseOddNumT.Length - 1).Clamp(0, rangedNumbers.Length - 1));

                                                /// IF more odd numbers follow: print base sequence number; ELSE print base sequence number and this/last odd number
                                                if (!lastNumberQ)
                                                    numToPrint = baseOddNumT;
                                                else
                                                {
                                                    Dbg.LogPart(tx, $"!}}> ");
                                                    numToPrint = $"{baseOddNumT},{currNumT}";
                                                }
                                            }
                                            else
                                            {
                                                /// IF more numbers follow: don't print number; ELSE print sequence-ending/last number
                                                if (!lastNumberQ)
                                                    Dbg.LogPart(tx, $".{currNumT}");
                                                else
                                                {
                                                    Dbg.LogPart(tx, $".{currNumT}!}}> ");
                                                    numToPrint = $"~{currNumT}";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            /// IF is within odd range (IF sequence of 2: print as normal; IF number to print unset, sequence of 3+: print odd sequence-ending number)
                                            if (withinOddRangeQ)
                                            {
                                                if (TryGetEndingNumber(baseOddNumT, out _, out int baseEndNum))
                                                    if (baseEndNum + 1 == prevEndNum)
                                                        numToPrint = $",{prevNumT},";
                                                if (numToPrint.IsNE())
                                                    numToPrint = $"~{prevNumT},";

                                                Dbg.LogPart(tx, $"!}}");
                                            }

                                            Dbg.LogPart(tx, $" {currNumT}");
                                            numToPrint += $"{currNumT},";

                                            withinOddRangeQ = false;
                                            baseOddNumT = null;
                                        }
                                    }
                                    else
                                    {
                                        if (withinOddRangeQ)
                                        {
                                            if (TryGetEndingNumber(baseOddNumT, out _, out int baseEndNum))
                                                if (baseEndNum + 1 == prevEndNum)
                                                    numToPrint = $",{prevNumT},";
                                            if (numToPrint.IsNE())
                                                numToPrint = $"~{prevNumT},";

                                            Dbg.LogPart(tx, $"!}}**");
                                        }

                                        /// // since 'justPrint' isn't disabled here, the following lines can be commented out
                                        /// Dbg.LogPart($" {currNumT}");
                                        /// numToPrint += $"{currNumT},";

                                        withinOddRangeQ = false;
                                        baseOddNumT = null;
                                    }
                                }
                            }

                            /// IF just print next number: (IF is within odd range: end odd range)
                            if (justPrintQ)
                            {
                                if (withinOddRangeQ)
                                {
                                    if (TryGetEndingNumber(baseOddNumT, out _, out int baseEndNum))
                                        if (baseEndNum + 1 == prevEndNum)
                                            numToPrint = $",{prevNumT},";
                                    if (numToPrint.IsNE())
                                        numToPrint = $"~{prevNumT},";

                                    Dbg.LogPart(tx, $"!}}*");

                                    //withinOddRangeQ = false;
                                    //baseOddNumT = null;
                                }

                                withinOddRangeQ = false;
                                baseOddNumT = null;

                                Dbg.LogPart(tx, $" {currNumT}");
                                numToPrint += $"{currNumT},";
                            }
                        }
                        
                    }

                    if (numToPrint.IsNotNE())
                        rangedNumbers += numToPrint;
                }
                Dbg.Log(tx, " // End");

                if (rangedNumbers.IsNotNE())
                    rangedNumbers = rangedNumbers.Replace(",", " ").Trim();
                Dbg.Log(tx, $"Result Range Numbers: {rangedNumbers}");
                Dbg.EndLogging(tx);
            }
            return rangedNumbers;

            // method
            bool TryGetEndingNumber(string str, out string numBase, out int endNum)
            {
                numBase = null;
                string endingNumStr = str;
                if (str.IsNotNE())
                {
                    endingNumStr = "";
                    bool halt = false;
                    for (int sx = str.Length - 1; sx >= 0 && !halt; sx--)
                    {
                        if (!LogDecoder.IsNumberless(str[sx].ToString()))
                            endingNumStr = str[sx].ToString() + endingNumStr;
                        else halt = true;
                    }

                    // the ending number cannot be the same as the whole (recieved) number
                    if (endingNumStr.IsNotNE())
                    {
                        if (endingNumStr == str.Trim())
                            endingNumStr = null;
                        else
                        {
                            endingNumStr = endingNumStr.Trim();
                            numBase = str.Remove(str.Length - endingNumStr.Length);
                        }
                    }
                }
                return int.TryParse(endingNumStr, out endNum);
            }
        }
        /// <summary>
        ///   Receives a string of numeric data IDs and a group-separating string that will seperate the given numeric range into different sequences of numbers. Each sequence of numbers is separately condensed into ranges by <see cref="CreateNumericDataIDRanges(string[], bool)"/> after being split into individual numbers using <paramref name="splitChar"/>. The number groups may be sorted before being condensed into ranges dependent on <paramref name="sortWordsQ"/>.
        /// </summary>
        /// <returns>An array of groups of condensed strings of numbers with numeric ranges. An empty array is returned if any parameters do not contain a valid value.</returns>
        public static string[] CreateNumericDataIDRanges(string numbers, string groupSplitter, char splitChar, bool sortWordsQ = true)
        {
            List<string> numberRanges = new();
            if (numbers.IsNotNEW() && groupSplitter.IsNotNEW() && splitChar.IsNotNull())
            {
                if (numbers.Contains(groupSplitter))
                {
                    string[] numRanges = numbers.Split(groupSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (numRanges.HasElements())
                        foreach (string aNumRange in numRanges)
                            numberRanges.Add(CreateNumericDataIDRanges(aNumRange.Split(splitChar), sortWordsQ));
                }
            }
            return numberRanges.ToArray();
        }
        /// <summary>Fetches a snippet of data within a line containing <paramref name="startingWith"/> and <paramref name="endingWith"/> and returns anything that is between them.</summary>     
        /// <param name="snipOpts">Snippet options that will affect the outcome of the returned snippet. Many options may be combined.</param>
        /// <returns>A snippet of <paramref name="line"/> between two other strings. Is <c>null</c> if <paramref name="startingWith"/> does not exist, and <see cref="String.Empty"/> when <paramref name="endingWith"/> comes before <paramref name="startingWith"/> or does not exist after it.</returns>
        public static string SnippetText(this string line, string startingWith, string endingWith, params Snip[] snipOpts)
        {
            /// options setup here
            bool includeStartEndQ = false, endAfterStartQ = false, end2ndQ = false, lastOfEndQ = false;
            if (snipOpts.HasElements())
            {
                foreach (Snip snipOpt in snipOpts)
                {
                    if (snipOpt == Snip.Inc || snipOpt == Snip.All)
                        includeStartEndQ = true;
                    if (snipOpt == Snip.EndAft || snipOpt == Snip.All || snipOpt == Snip.End2nd)
                        endAfterStartQ = true;
                    if (snipOpt == Snip.End2nd)
                        end2ndQ = true;
                    if (snipOpt == Snip.EndLast || snipOpt == Snip.All)
                        lastOfEndQ = true;
                }
            }

            /// snipping heree
            string snippet = null;
            if (line.IsNotNEW() && startingWith.IsNotNE() && endingWith.IsNotNE())
            {
                int indexStart = line.IndexOf(startingWith);
                int indexEnd;

                if (indexStart != -1)
                {
                    if (endAfterStartQ)
                    {
                        if (lastOfEndQ)
                            indexEnd = line.Substring(indexStart + startingWith.Length).LastIndexOf(endingWith);
                        else
                            indexEnd = line.Substring(indexStart + startingWith.Length).IndexOf(endingWith);

                        if (indexEnd != -1)
                        {
                            indexEnd += indexStart + startingWith.Length;

                            if (!lastOfEndQ && end2ndQ && indexEnd + 1 < line.Length)
                            {
                                int indexEnd1st = indexEnd;
                                int indexEndNext = line.Substring(indexEnd1st + endingWith.Length).IndexOf(endingWith);

                                if (indexEndNext != -1)
                                    indexEnd = indexEndNext + indexEnd1st + endingWith.Length;
                            }
                        }
                        //else indexEnd = 0;
                    }
                    else
                    {
                        if (lastOfEndQ)
                            indexEnd = line.LastIndexOf(endingWith);
                        else indexEnd = line.IndexOf(endingWith);
                    }
                    if (includeStartEndQ && indexEnd != -1)
                        indexEnd += endingWith.Length;

                    if (indexStart < indexEnd)
                    {
                        if (includeStartEndQ)
                        {
                            snippet = line.Substring(indexStart, indexEnd - indexStart);
                        }
                        else
                        {
                            int othStartIx = indexStart + startingWith.Length, othEndIx = indexEnd - endingWith.Length + 1;

                            if (othStartIx < othEndIx)
                                snippet = line.Substring(othStartIx, othEndIx - othStartIx);
                            else snippet = "";
                        }
                    }
                    else snippet = "";
                }
            }
            return snippet;
        }
        #endregion

        // GENERAL --> COLLECTIONS
        /// <summary>Checks if a collection (array, list) is not null and has at least one element.</summary>
        public static bool HasElements(this ICollection col)
        {
            bool hasElements = false;
            if (col != null)
                if (col.Count != 0)
                    hasElements = true;
            return hasElements;
        }        
        /// <summary>Checks if a collection (array, list) is not null, and contains at least the <paramref name="minimum"/> number of elements.</summary>
        /// <param name="minimum">Cannot be a negative number.</param>
        public static bool HasElements(this ICollection col, int minimum)
        {
            if (minimum < 0)
                minimum = 0;

            bool hasElements = false;
            if (col != null)
                if (col.Count >= minimum)
                    hasElements = true;
            return hasElements;
        }

        // GENERAL --> NUMERICS
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

        // GENERAL --> ADVANCED
        /// <summary>
        ///     Sorts all words and characters (alphabets, numerics, symbols) and returns a list of them in symbolic then alphanumeric order. <c>Null</c> or <c>Empty/Whitespaced</c> elements are skipped in sorting.
        /// </summary>
        /// <remarks>Sorting groups precedence: symbols, numbers, letters</remarks>
        public static List<string> SortWords(this string[] words)
        {
            /** PLANNING
            We've done this twice already so let's get right into it...

            ------------
            MAJOR PLAN SUMMARY
                - Give each character a comparison score (integer)
                    - The score determines the order in which the characters have precedence over the other. 
                    - The desired order is symbols, numbers, then letters.
                - When there are two or more numbers to compare consecutively, use numeric congregation

            Example sort
            Recieved:
                fool    |   16  25  25  22  -34 -34
                ibis    |   19  12  19  29  -34 -34
                mag!x   |   23  11  17  -31 34  -34
                i_flip  |   19  -23 16  22  19  26
                256     |   2   5   6   -34 -34 -34
                u94     |   30  9   4   -34 -34 -34
                u945    |   30  9   4   5   -34 -34
            Sorting:
                C1  =>  nothing to compare 'fool'
                C2  =>  comparing 'ibis' to 'fool' (as this|other)  ::  18|15   ::  'ibis' after 'fool'
                C3  =>  comparing 'mag!x to 'fool' (as this|other)  ::  22|15   ::  'mag!x' after 'fool'
                    =>  comparing 'mag!x to 'ibis' (as this|other)  ::  
                ...
            Result:
                word    | compared numbers
                ___________________________
                256     |   2
                fool    |   16
                i_flip  |   19  -23
                ibis    |   19  12
                mag!x   |   23
                u94     |   31  -1  94      (-1 as 10)
                u945    |   31  -1  -1      (-1 as 10, -1 as 100)


            --------
            CHARACTERS AND SCORES            
            Symbols
                CHAR |  n   ~   `   !   @   #   $   %   ^   &   *   _   -   +   =   (   )   [   ]   {   }   <   >   \   |   /   ;   :   '   "   .   ,   ?   s
                SCORE|  -34 -33 -32 -31 -30 -29 -28 -27 -26 -25 -24 -23 -22 -21 -20 -19 -18 -17 -16 -15 -14 -13 -12 -11 -10 -9  -8  -7  -6  -5  -4  -3  -2  10
                    :: s (space character [' '])   n (non-scorable/null character ['\0'])
            Numerics
                CHAR |  eX  0   1   2   3   4   5   6   7   8   9       
                SCORE|  -1  0   1   2   3   4   5   6   7   8   9
                    :: eX (Indicator for resulting vacancy from numeric congregation) 
            Alphabetics
                CHAR |  a   b   c   d   e   f   g   h   i   j   k   l   m   n   o   p   q   r   s   t   u   v   w   x   y   z
                SCORE|  11  12  13  14  15  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36


            --------
            NUMERIC CONGREGATION
            12      |   -1  12      
            2.89    |   2   -4  -1  89
            1.29    |   1   -4  -1  29
            5.6a    |   5   -4  6   10

            What is Numeric Congregation and how does it work?
                - When comparing numbers that are beyond the single-digit scoring capabilities of the characters and scores table, the number are treated as normal numbers and are compared in that sense. The (-1) indicates that the number once there should be joined with the next number in the array (if array elements read '2', then '6', then they will be joined as '26'), and that they must be compared against 10 to the power of consecutive vacancies (10^2, 10^3, ...)
                - Note that numeric congregation only happens in preparation to comparisons (when the words become arrays of numbers) (to determine order)
            ***/

            Dbg.StartLogging("Extensions.SortWords(this str[])", out int tx);
            Dbg.ToggleThreadOutputOmission(tx);
            Dbg.Log(tx, $"Recieved words to sort... are there really any words to sort? {(words.HasElements() ? "Yes" : "No")}");
            List<string> orderedWords = new();

            if (words.HasElements())
            {
                // filter words and find longest word
                int lengthiestWordLength = 0, nullWordsCount = 0;
                List<string> filteredWords = new List<string>();

                foreach (string word in words)
                {
                    if (word.IsNotNEW())
                    {
                        string word2 = word.Replace("\0", "").Trim();
                        filteredWords.Add(word2);

                        if (word2.Length > lengthiestWordLength)
                            lengthiestWordLength = word2.Length;
                    }
                    else nullWordsCount++;
                }

                Dbg.Log(tx, $"Completed filtering the array of words (removed NEWs, replaced \\0, trimmed) --> Original word count [{words.Length}]; Filtered word count[{filteredWords.Count}]; Lengthiest word length [{lengthiestWordLength}]; Skipped words count [{nullWordsCount}]");

                if (filteredWords.HasElements())
                {
                    // filtered words to score arrays
                    const char replaceNullScore = '`';
                    Dbg.Log(tx, $"Creating words scoring list for [{filteredWords.Count}] words at [{lengthiestWordLength}] maximum array length each [where {replaceNullScore} is null score {CharScore('\0')}]");
                    Dbg.NudgeIndent(tx, true);
                    const int vacancyScore = -1;
                    List<int[]> scoredWords = new List<int[]>(filteredWords.Count);
                    for (int fltwIx = 0; fltwIx < filteredWords.Count; fltwIx++)
                    {
                        string fltWord = filteredWords[fltwIx];
                        Dbg.LogPart(tx, $"Scoring word @index-{fltwIx} '{fltWord}' -->  [");

                        int[] scoredWordArr = new int[lengthiestWordLength];
                        int congregatedNumber = vacancyScore;
                        for (int cIx = 0; cIx < lengthiestWordLength; cIx++)
                        {
                            int cScore;
                            char c = '\0';
                            if (cIx < fltWord.Length)
                            {
                                c = fltWord[cIx];
                                cScore = CharScore(c);

                                /// THINKING AND INKING IT OUT
                                /// word: -85.3f
                                ///     ix  chr     score
                                ///     0 | -       -22
                                ///     1 | 8       -1
                                ///     Process::
                                ///         cScore = 8
                                ///         if ({Cs8}8.IsWithin(0, 9) [true; 0 < 8 < 9] && (ix)1 + 1 {2} < fltWord.Length (6) [true])
                                ///             if ({Cs5}5.IsWithin(0, 9) [true; 0 < 5 < 9])
                                ///                 (congNum == -1) --> congNum = 8;
                                ///                 cScore = -1;
                                ///             conCongCount++ = 1;
                                ///     2 | 5       85
                                ///     Process::
                                ///         cScore = 5
                                ///         if ({Cs5}5.IsWithin(0, 9) [true; 0 < 5 < 9] && (ix)2 + 1 {3} < fltWord.Length (6) [true])
                                ///             if ({Cs.}-4.IsWithin(0,9) [false; -4 < 0])
                                ///             else
                                ///             (conNum{8} != -1 [true])
                                ///                 cScore --> (10 * 1 * 8) + 5 = 85;
                                ///             conCongCount++ = 2;
                                ///     3 | .       -4
                                ///     4 | 3       3
                                ///     Process:: 
                                ///         cScore = 3
                                ///         if ({Cs3}3.IsWithin(0, 9) [true; 0 < 3 < 9] && (ix)4 + 1 {5} < fltWord.Length (6) [true])
                                ///             if ({Csf}15.IsWithin(0, 9) [false; 9 < 15]
                                ///             else (conNum != -1 [false]) 
                                ///                 (nothing)
                                ///             conCongCount++ = 1;
                                ///     5 | f       15
                                ///
                                /// 
                                /// without processes
                                ///     0 | -       -22
                                ///     1 | 8       -1
                                ///     2 | 5       85
                                ///     3 | .       -4
                                ///     4 | 3       3
                                ///     5 | f       16
                                ///     
                                /// Result:
                                ///     -85.3f -->  [-22 -1 85 -4 3 16]
                                ///
                                // for number congregation   -->   if (this char is single-digit number)
                                if (cScore.IsWithin(0, 9))
                                {
                                    char nextC = '\0';
                                    if (cIx + 1 < fltWord.Length)
                                        nextC = fltWord[cIx + 1];

                                    // if (next char is single-digit number)
                                    if (CharScore(nextC).IsWithin(0, 9))
                                    {
                                        if (congregatedNumber == vacancyScore)
                                            congregatedNumber = cScore;
                                        else congregatedNumber = (10 * congregatedNumber) + cScore;

                                        cScore = vacancyScore;
                                    }
                                    else
                                    {
                                        if (congregatedNumber != vacancyScore)
                                            cScore = (10 * congregatedNumber) + cScore;
                                    }
                                }
                                else
                                    congregatedNumber = vacancyScore;
                            }
                            else cScore = CharScore('\0');

                            // set score to array
                            //Dbg.LogPart($" {cScore}|{(c.IsNotNull()? c : ' ')} ");
                            Dbg.LogPart(tx, $"{(cScore == CharScore('\0') ? replaceNullScore.ToString() : cScore.ToString())} ");
                            scoredWordArr[cIx] = cScore;
                        }
                        scoredWords.Add(scoredWordArr);
                        Dbg.Log(tx, "]");
                    }
                    Dbg.NudgeIndent(tx, false);


                    // Sort into order
                    Dbg.Log(tx, $"Comparing scores of {filteredWords.Count} filtered words and sorting them into order;");
                    Dbg.NudgeIndent(tx, true);
                    List<int> wordOrderByIndex = new List<int>();
                    const int skipIntervalsDefault = 7;
                    int skipInterval = (filteredWords.Count / (skipIntervalsDefault * skipIntervalsDefault)).Clamp(skipIntervalsDefault, (int)Math.Pow(skipIntervalsDefault, 3));
                    /** skipIntervals - For quickly sorting large word groups
                            When the word array grows larger than double the skipInterval number, we start jumping over other comparisons until we fine a place where the word can be inserted. When the placement is found, we backpedal and find where in the last that were skipped the word being compared is to be placed.

                        skipInterval value :: minimum 7, unless list is way past 343 items, to a maximum of 343 items per skip
                     */
                    for (int thisIx = 0; thisIx < filteredWords.Count; thisIx++)
                    {
                        string thisWord = filteredWords[thisIx];
                        int[] thisWordArr = scoredWords[thisIx];
                        bool skippingAllowedQ = wordOrderByIndex.Count > skipInterval * 2;

                        Dbg.LogPart(tx, $"Sorting word at ix#{thisIx} '{thisWord}' as score array --> [");
                        foreach (int score in thisWordArr) { Dbg.LogPart(tx, $"{score} "); }
                        Dbg.Log(tx, $"]{(skippingAllowedQ ? $" (skipping-by-{skipInterval}s)" : "")}");

                        // words to compare...
                        Dbg.NudgeIndent(tx, true);
                        if (wordOrderByIndex.HasElements())
                        {
                            const int noInsertIndex = -1;
                            int insertIndex = noInsertIndex;
                            for(int wobIx = 0; wobIx < wordOrderByIndex.Count && insertIndex == noInsertIndex; wobIx++)
                            {
                                int otherIx = wordOrderByIndex[wobIx];
                                string otherWord = filteredWords[otherIx];
                                int[] otherWordArr = scoredWords[otherIx];
                                bool noSkippingToEndq = wobIx + skipInterval + 1 >= wordOrderByIndex.Count;
                                Dbg.LogPart(tx, $"Comparing '{thisWord}' to '{otherWord}' (as this#|other#) --> "); // left off here


                                /// So, score comparisons -- how we doin it?
                                /// --------
                                /// Without numeric congregation
                                /// -> within loop getting scores in parallel
                                ///     if (thisScore < otherScore)
                                ///         endLoop & this before other (insert)
                                ///     else if (thisScore == otherScore)
                                ///         try again (compare next scores)
                                ///     else (thisScore > otherScore)
                                ///         endLoop & this after other (next word)
                                ///         
                                /// --------
                                /// With numeric congregation
                                /// Question: How does a congregated number compare to non-numeric scores?
                                ///         es | 14 28  {2nd}           e$a | 14 -28 10  {1st}
                                ///         89 | -1 89  {1st}           e32 | 14 -1  32  {2nd}
                                ///       A: numbers still come before letters, and symbols before numbers
                                /// 
                                /// -> within loop getting scores in parallel
                                /// Where 'thisVacNum' and 'otherVacNum' are the comparison numbers for vacancies (10 ^ (numOfConsecutiveVacancies))
                                /// 
                                ///     if (thisScore < otherScore)
                                ///         if (thisScore == vacancy) 
                                ///             if (otherScore < thisVacNum)
                                ///                 endLoop & this after other (next word)          -1(10)|0~9  -1(100)|10~99   ... [Path.A1]
                                ///             else if (otherScore >= thisVacNum)
                                ///                 endLoop & this before other (insert)            -1(10)|10^  -1(100)|100^ ...    [Path.A2]
                                ///         else (thisScore != vacancy)
                                ///             endLoop & this before other (insert)                18|22   -32|-23     -2|-1   [Path.A3]
                                /// 
                                ///     else if (thisScore == otherScore)
                                ///         if (thisScore == vacancy)
                                ///             if (thisVacNum < otherVacNum)
                                ///                 endLoop & this before other (insert)            10|100  [Path.B1] {never hit}
                                ///             else if (thisVacNum > otherVacNum)
                                ///                 endLoop & this after other (next word)          100|10  [Path.B2] {never hit}
                                ///             else (thisVacNum == otherVacNum)                    
                                ///                 try again (compare next scores)                 10|10   100|100
                                ///         else (thisScore != vacancy)
                                ///             try again (compare next scores)                     12|12   -10|-10
                                ///         
                                ///     else (thisScore > otherScore)
                                ///         if (thisScore == vacancy)                           
                                ///             endLoop & this after other (next word)              -1|-2v  [Path.C1]
                                ///         else (thisScore != vacancy)
                                ///             if (otherScore == vacancy)
                                ///                 if (thisScore < otherVacNum)                   
                                ///                     endLoop & this before other (insert)        0~9|-1(10)  10~99|-1(100)   ... [Path.C2]
                                ///                 else (thisScore >= otherVacNum)
                                ///                     endLoop & this after other (next word)      10^|-1(10)  100^|-1(100) ...    [Path.C3]
                                ///             else (otherScore != vacancy)
                                ///                 endLoop & this after other (next word)          18|15   -18|-23     0|-1    [Path.C4]
                                ///         

                                bool endComparisons = false;
                                int thisC3, otherC3 = thisC3 = 0; // C3 as 'Consecutive Congregation Count'
                                for (int csIx = 0; csIx < lengthiestWordLength && !endComparisons; csIx++)
                                {
                                    int thisScore = thisWordArr[csIx];
                                    int otherScore = otherWordArr[csIx];

                                    // consecutive congregation counters
                                    if (thisScore == vacancyScore)
                                        thisC3++;
                                    else thisC3 = 0;
                                    if (otherScore == vacancyScore)
                                        otherC3++;
                                    else otherC3 = 0;

                                    int thisVacNum = (int)Math.Pow(10, thisC3);
                                    int otherVacNum = (int)Math.Pow(10, otherC3);

                                    Dbg.LogPart(tx, $" {thisScore}{(thisScore == vacancyScore? $"({thisVacNum})" : "")}|"); //  x(a)|y(b)
                                    Dbg.LogPart(tx, $"{otherScore}{(otherScore == vacancyScore? $"({otherVacNum})" : "")} ");

                                    string dbgCodePathName = "n/a";
                                    /// With numCong : 1st part ---
                                    ///     if (thisScore < otherScore)
                                    ///         if (thisScore == vacancy) 
                                    ///             if (otherScore < thisVacNum)
                                    ///                 endLoop & this after other (next word)          -1(10)|0~9  -1(100)|10~99   ... [Path.A1]
                                    ///             else if (otherScore >= thisVacNum)
                                    ///                 endLoop & this before other (insert)            -1(10)|10^  -1(100)|100^ ...    [Path.A2]
                                    ///         else (thisScore != vacancy)
                                    ///             endLoop & this before other (insert)                18|22   -32|-23     -2|-1   [Path.A3]
                                    /// 
                                    if (thisScore < otherScore)
                                    {
                                        if (thisScore == vacancyScore)
                                        {
                                            if (otherScore < thisVacNum)
                                            {
                                                endComparisons = true;
                                                dbgCodePathName = "Path.A1";
                                            }
                                            else // otherScore >= thisVacNum
                                            {
                                                endComparisons = true;
                                                insertIndex = wobIx;
                                                dbgCodePathName = "Path.A2";
                                            }
                                        }
                                        else // thisScore != vacancy
                                        {
                                            endComparisons = true;
                                            insertIndex = wobIx;
                                            dbgCodePathName = "Path.A3";
                                        }
                                    }

                                    /// With numCong : 2nd part ---
                                    ///     else if (thisScore == otherScore)
                                    ///         if (thisScore == vacancy)
                                    ///             if (thisVacNum < otherVacNum)
                                    ///                 endLoop & this before other (insert)            10|100  [Path.B1] {never hit}
                                    ///             else if (thisVacNum > otherVacNum)
                                    ///                 endLoop & this after other (next word)          100|10  [Path.B2] {never hit}
                                    ///             else (thisVacNum == otherVacNum)                    
                                    ///                 try again (compare next scores)                 10|10   100|100
                                    ///         else (thisScore != vacancy)
                                    ///             try again (compare next scores)                     12|12   -10|-10
                                    ///         
                                    else if (thisScore == otherScore)
                                    {
                                        if (thisScore == vacancyScore)
                                        {
                                            if (thisVacNum < otherVacNum)
                                            {
                                                endComparisons = true;
                                                insertIndex = wobIx;
                                                dbgCodePathName = "Path.B1 (WHAT!??)";
                                            }
                                            else if (thisVacNum > otherVacNum)
                                            {
                                                endComparisons = true;
                                                dbgCodePathName = "Path.B2 (WHAT!??)";
                                            }
                                            // else (thisVacNum == otherVacNum) [do nothing]
                                        }
                                        //else  (thisScore != vacancy) [do nothing]
                                    }

                                    /// With numCong : 3rd part ---
                                    ///     else (thisScore > otherScore)
                                    ///         if (thisScore == vacancy)                           
                                    ///             endLoop & this after other (next word)              -1|-2v  [Path.C1]
                                    ///         else (thisScore != vacancy)
                                    ///             if (otherScore == vacancy)
                                    ///                 if (thisScore < otherVacNum)                   
                                    ///                     endLoop & this before other (insert)        0~9|-1(10)  10~99|-1(100)   ... [Path.C2]
                                    ///                 else (thisScore >= otherVacNum)
                                    ///                     endLoop & this after other (next word)      10^|-1(10)  100^|-1(100) ...    [Path.C3]
                                    ///             else (otherScore != vacancy)
                                    ///                 endLoop & this after other (next word)          18|15   -18|-23     0|-1    [Path.C4]
                                    ///                 
                                    else // thisScore > otherScore
                                    {
                                        if (thisScore == vacancyScore)
                                        {
                                            endComparisons = true;
                                            dbgCodePathName = "Path.C1";
                                        }
                                        else // (thisScore != vacancy)
                                        {
                                            if (otherScore == vacancyScore)
                                            {
                                                if (thisScore < otherVacNum)
                                                {
                                                    endComparisons = true;
                                                    insertIndex = wobIx;
                                                    dbgCodePathName = "Path.C2";
                                                }
                                                else // (thisScore >= otherVacNum)
                                                {
                                                    endComparisons = true;
                                                    dbgCodePathName = "Path.C3";
                                                }
                                            }
                                            else // (otherScore != vacancy)
                                            {
                                                endComparisons = true;
                                                dbgCodePathName = "Path.C4";
                                            }
                                        }
                                    }

                                    dbgCodePathName = dbgCodePathName.Replace("Path.", "p");
                                    if (endComparisons)
                                        Dbg.LogPart(tx, true ? $"[{dbgCodePathName}] " : "");
                                }


                                if (endComparisons)
                                {
                                    /// IF skipping allowed: 
                                    ///     IF can insert word: backpedal
                                    ///     ELSE (IF too close to end of words: release skipping do normal sorting relay; ELSE skip words)
                                    /// ELSE normal sorting relay
                                    if (skippingAllowedQ)
                                    {
                                        if (insertIndex != noInsertIndex)
                                        {
                                            wobIx -= skipInterval + 1;
                                            if (wobIx < 0)
                                                wobIx = -1; /// it's gonna be plus one'd anyway.

                                            skippingAllowedQ = false;
                                            insertIndex = noInsertIndex;
                                            Dbg.Log(tx, $" --> <!> {{backpedal {skipInterval + 1}}}");
                                        }
                                        else
                                        {
                                            if (noSkippingToEndq)
                                            {
                                                skippingAllowedQ = false;
                                                Dbg.Log(tx, " --> <X> {cannot skip to end}");
                                            }
                                            else
                                            {
                                                wobIx += skipInterval;
                                                Dbg.Log(tx, $" --> <X> {{skip {skipInterval}}}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (insertIndex != noInsertIndex)
                                            Dbg.Log(tx, " --> <O>");
                                        else Dbg.Log(tx, " --> <X>");
                                    }
                                }
                            }

                            // inserts \ additions here
                            if (insertIndex == noInsertIndex)
                            {
                                wordOrderByIndex.Add(thisIx); /// add to end of list
                                Dbg.Log(tx, $"Added '{thisWord}' to end of sorting list.");
                            }
                            else
                            {
                                wordOrderByIndex.Insert(insertIndex, thisIx); /// insert into list
                                Dbg.Log(tx, $"Inserted '{thisWord}' (ix#{thisIx}) at index [{insertIndex}] of sorting list.");
                            }
                        }
                        /// no words to compare; add to sort list
                        else
                        {
                            Dbg.Log(tx, "No words to compare with. Adding to order list.");
                            wordOrderByIndex.Add(thisIx);
                        }

                        Dbg.LogPart(tx, $"End sorting ix#{thisIx}  //  Current sorting order list :: [");
                        foreach (int sortOrderNum in wordOrderByIndex) 
                        {
                            Dbg.LogPart(tx, $"{Base.SurroundText($"{sortOrderNum}", Base.ConditionalText(sortOrderNum == thisIx, "*", " ")).Trim()} ");
                            //Dbg.LogPart($"{sortOrderNum} "); 
                        }
                        Dbg.Log(tx, "]");
                        Dbg.NudgeIndent(tx, false);
                    }
                    Dbg.NudgeIndent(tx, false);


                    // compile ordered words array
                    if (wordOrderByIndex.HasElements())
                    {
                        Dbg.Log(tx, "Sorting complete! Returning array of sorted words; Adding (as [index#]word)");
                        Dbg.NudgeIndent(tx, true);
                        orderedWords = new();
                        foreach (int wobix in wordOrderByIndex)
                        {
                            string wordToAdd = filteredWords[wobix];
                                Dbg.LogPart(tx, $"[{orderedWords.Count}]{wordToAdd} // ");
                            orderedWords.Add(wordToAdd);
                        }
                        Dbg.Log(tx, " --> DONE!");
                        Dbg.NudgeIndent(tx, false);
                    }
                }

                /// nothing to sort (post-filter)
                else
                {
                    orderedWords.AddRange(words);
                    Dbg.Log(tx, "Returning array of words as is (no filtered words)");
                }
            }

            /// nothing to sort
            else
            {
                orderedWords.AddRange(words);
                Dbg.Log(tx, "Returning array of words as is (no words)");
            }

            Dbg.EndLogging(tx);
            return orderedWords;
        }
        /**
            <summary>SCORING:
            <br>- Non-scorable/null character ::// <c>-34</c></br>
            <br>- Symbols :: ~`!@#$%^&#38;*_-+=()[]{}&lt;&gt;\|/;:'".,? // <c>Range: [-33,-2]</c></br>
            <br>- Numerics :: 0123456789 // <c>Range: [0,9]</c></br>
            <br>- Space Character ::// <c>10</c></br>
            <br>- Alphabetics :: abcdefghijklmnopqrstuvwxyz // <c>Range: [11,36]</c></br>            
            </summary>
         */
        public static int CharScore(char c)
        {
            /** PLANNING - CHARACTERS AND SCORES EXCERPT
            --------
            CHARACTERS AND SCORES            
            Symbols
                CHAR |  n   ~   `   !   @   #   $   %   ^   &   *   _   -   +   =   (   )   [   ]   {   }   <   >   \   |   /   ;   :   '   "   .   ,   ?   s
                SCORE|  -34 -33 -32 -31 -30 -29 -28 -27 -26 -25 -24 -23 -22 -21 -20 -19 -18 -17 -16 -15 -14 -13 -12 -11 -10 -9  -8  -7  -6  -5  -4  -3  -2  10
                    :: s (space character [' '])   n (non-scorable/null character ['\0'])
            Numerics
                CHAR |  eX  0   1   2   3   4   5   6   7   8   9       
                SCORE|  -1  0   1   2   3   4   5   6   7   8   9
                    :: eX (Indicator for resulting vacancy from numeric congregation) 
            Alphabetics
                CHAR |  a   b   c   d   e   f   g   h   i   j   k   l   m   n   o   p   q   r   s   t   u   v   w   x   y   z
                SCORE|  11  12  13  14  15  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36
            ***/
            const int nullScore = -34;
            int score = nullScore;

            if (c.IsNotNull())
            {
                string chars = "~`!@#$%^&*_-+=()[]{}<>\\|/;:'\".,?" + "~0123456789" + " " + "abcdefghijklmnopqrstuvwxyz";
                chars = chars.ToLower();
                c = c.ToString().ToLower()[0];

                if (chars.Contains(c.ToString()))
                {
                    bool stopSearch = false;
                    char[] characters = chars.ToCharArray();
                    for (int cix = 0; cix < characters.Length && !stopSearch; cix++)
                    {
                        score++;
                        if (c == characters[cix])
                            stopSearch = true;
                    }
                }
            }
            return score;
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

            //Dbg.SingleLog("Extensions.Decode(this s, out c)", $"Parsed [{parsed}]; string [{s}], returned color [{c}]");
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

        // RESOURCE CONTENTS
        public static bool IsNone(this RCFetchSource rcfs)
        {
            return rcfs == RCFetchSource.None;
        }
    }
}
