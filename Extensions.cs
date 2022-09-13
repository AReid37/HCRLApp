using System;
using System.Collections;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;
using HCResourceLibraryApp.Layout;

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
        public static string Clamp(this string s, int maxLength, string clampingSuffix)
        {
            const int clampSymMaxLen = 3, minimumClampLength = 5;
            string fullStr = s;
            if (s.IsNotNEW() && maxLength >= minimumClampLength)
            {
                int clampSymLen = 0;
                if (clampingSuffix.IsNotNEW())
                {
                    if (!clampingSuffix.Length.IsWithin(0, clampSymMaxLen))
                        clampingSuffix = clampingSuffix.Remove(3);
                    clampSymLen = clampingSuffix.Length;
                }

                if (s.Length > maxLength - clampSymLen)
                {
                    s = s.Remove(maxLength - clampSymLen);
                    fullStr = $"{s}{clampingSuffix}";
                }
            }
            return fullStr;
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

            Dbug.IgnoreNextLogSession();
            Dbug.StartLogging("Extensions.SortWords(this str[])");
            Dbug.Log($"Recieved words to sort... are there really any words to sort? {(words.HasElements() ? "Yes" : "No")}");
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
                Dbug.Log($"Completed filtering the array of words (removed NEWs, replaced \\0, trimmed) --> Original word count [{words.Length}]; Filtered word count[{filteredWords.Count}]; Lengthiest word length [{lengthiestWordLength}]; Skipped words count [{nullWordsCount}]");

                if (filteredWords.HasElements())
                {
                    // filtered words to score arrays
                    const char replaceNullScore = '`';
                    Dbug.Log($"Creating words scoring list for [{filteredWords.Count}] words at [{lengthiestWordLength}] maximum array length each [where {replaceNullScore} is null score {CharScore('\0')}]");
                    Dbug.NudgeIndent(true);
                    const int vacancyScore = -1;
                    List<int[]> scoredWords = new List<int[]>(filteredWords.Count);
                    for (int fltwIx = 0; fltwIx < filteredWords.Count; fltwIx++)
                    {
                        string fltWord = filteredWords[fltwIx];
                        Dbug.LogPart($"Scoring word @index-{fltwIx} '{fltWord}' -->  [");

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
                            //Dbug.LogPart($" {cScore}|{(c.IsNotNull()? c : ' ')} ");
                            Dbug.LogPart($"{(cScore == CharScore('\0') ? replaceNullScore.ToString() : cScore.ToString())} ");
                            scoredWordArr[cIx] = cScore;
                        }
                        scoredWords.Add(scoredWordArr);
                        Dbug.Log("]");
                    }
                    Dbug.NudgeIndent(false);


                    // Sort into order
                    Dbug.Log($"Comparing scores of {filteredWords.Count} filtered words and sorting them into order;");
                    Dbug.NudgeIndent(true);
                    List<int> wordOrderByIndex = new List<int>();
                    for (int thisIx = 0; thisIx < filteredWords.Count; thisIx++)
                    {
                        string thisWord = filteredWords[thisIx];
                        int[] thisWordArr = scoredWords[thisIx];

                        Dbug.LogPart($"Sorting word at ix#{thisIx} '{thisWord}' as score array --> [");
                        foreach (int score in thisWordArr) { Dbug.LogPart($"{score} "); }
                        Dbug.Log("]");

                        // words to compare...
                        Dbug.NudgeIndent(true);
                        if (wordOrderByIndex.HasElements())
                        {
                            const int noInsertIndex = -1;
                            int insertIndex = noInsertIndex;
                            for(int wobIx = 0; wobIx < wordOrderByIndex.Count && insertIndex == noInsertIndex; wobIx++)
                            {
                                int otherIx = wordOrderByIndex[wobIx];
                                string otherWord = filteredWords[otherIx];
                                int[] otherWordArr = scoredWords[otherIx];
                                Dbug.LogPart($"Comparing '{thisWord}' to '{otherWord}' (as this#|other#) --> "); // left off here


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

                                    Dbug.LogPart($" {thisScore}{(thisScore == vacancyScore? $"({thisVacNum})" : "")}|"); //  x(a)|y(b)
                                    Dbug.LogPart($"{otherScore}{(otherScore == vacancyScore? $"({otherVacNum})" : "")} ");

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
                                        Dbug.LogPart(true ? $"[{dbgCodePathName}] " : "");
                                }

                                if (endComparisons)
                                {
                                    if (insertIndex != noInsertIndex)
                                        Dbug.Log(" --> <O>");
                                    else Dbug.Log(" --> <X>");
                                }
                            }

                            // inserts \ additions here
                            if (insertIndex == noInsertIndex)
                            {
                                wordOrderByIndex.Add(thisIx); /// add to end of list
                                Dbug.Log($"Added '{thisWord}' to end of sorting list.");
                            }
                            else
                            {
                                wordOrderByIndex.Insert(insertIndex, thisIx); /// insert into list
                                Dbug.Log($"Inserted '{thisWord}' (ix#{thisIx}) at index [{insertIndex}] of sorting list.");
                            }
                        }
                        /// no words to compare; add to sort list
                        else
                        {
                            Dbug.Log("No words to compare with. Adding to order list.");
                            wordOrderByIndex.Add(thisIx);
                        }

                        Dbug.LogPart($"End sorting ix#{thisIx}  //  Current sorting order list :: [");
                        foreach (int sortOrderNum in wordOrderByIndex) 
                        {
                            Dbug.LogPart($"{Base.SurroundText($"{sortOrderNum}", Base.ConditionalText(sortOrderNum == thisIx, "*", " ")).Trim()} ");
                            //Dbug.LogPart($"{sortOrderNum} "); 
                        }
                        Dbug.Log("]");
                        Dbug.NudgeIndent(false);
                    }
                    Dbug.NudgeIndent(false);


                    // compile ordered words array
                    if (wordOrderByIndex.HasElements())
                    {
                        Dbug.Log("Sorting complete! Returning array of sorted words; Adding (as [index#]word)");
                        Dbug.NudgeIndent(true);
                        orderedWords = new();
                        foreach (int wobix in wordOrderByIndex)
                        {
                            string wordToAdd = filteredWords[wobix];
                                Dbug.LogPart($"[{orderedWords.Count}]{wordToAdd} // ");
                            orderedWords.Add(wordToAdd);
                        }
                        Dbug.Log(" --> DONE!");
                        Dbug.NudgeIndent(false);
                    }
                }

                /// nothing to sort (post-filter)
                else
                {
                    orderedWords.AddRange(words);
                    Dbug.Log("Returning array of words as is (no filtered words)");
                }
            }

            /// nothing to sort
            else
            {
                orderedWords.AddRange(words);
                Dbug.Log("Returning array of words as is (no words)");
            }

            Dbug.EndLogging();
            return orderedWords;


            /// static methods?
            static int CharScore(char c)
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

        // RESOURCE CONTENTS
        public static bool Equals(this RCFetchSource rcfs, RCFetchSource other)
        {
            return rcfs == other;
        }
        public static bool IsNone(this RCFetchSource rcfs)
        {
            return rcfs == RCFetchSource.None;
        }
    }
}
