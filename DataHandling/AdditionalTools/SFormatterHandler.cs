using System;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.DataHandling
{
    public static class SFormatterHandler
    {
        /** SFormattterHandler planning
            
            Fields/Props
            - 

            Methods
            - void ColorCode(str fLine)
            - SFI[] CheckSyntax(str fLine, str prevFLine = null)

            where 'SFI' is SFormatterInfo.cs for returning error message
         */

        // METHODS
        public static void ColorCode(string fLine, bool useNativeQ = false, bool newLineQ = false)
        {
            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("SFormatterHandler.ColorCode()");
            Color colComment = useNativeQ ? Color.DarkGray : GetPrefsForeColor(ForECol.Accent);
            Color colEscape = useNativeQ ? Color.Gray : GetPrefsForeColor(ForECol.Accent);
            Color colOperator = useNativeQ ? Color.Gray : GetPrefsForeColor(ForECol.Accent);
            Color colCode = useNativeQ ? Color.White : GetPrefsForeColor(ForECol.Normal);
            Color colKeyword = useNativeQ ? Color.Magenta : GetPrefsForeColor(ForECol.Correction);
            Color colPlaint = useNativeQ ? Color.Yellow : GetPrefsForeColor(ForECol.InputColor);
            Color colRef = useNativeQ ? Color.Blue : GetPrefsForeColor(ForECol.Highlight);
            /// don't need one for errors

            /** General syntax and color coding snippets
                > General
                    Handled by SFormatterHandler.cs
                    syntax          outcome
                    _________________________________
                    // abc123       line comment. Must be placed at beginning of line
                    abc123          code
                    "abc123"        plain text
                    &00;            plain text '"'
                    {abc123}        library reference
                    $abc123         steam formatting reference
                    if # = #:       keyword, control; compares two given values to be equal. Prints following line if condition is true (values are equal). Placed at start of line.
                    else:           keyword, control; Prints following line if the condition of an immediately preceding 'if # = #' is false (values are not equal). Placed at start of line.
                    repeat #:       keyword, control; repeats line '#' times incrementing from one to given number '#'. Any occuring '#' in following line is replaced with this incrementing number. Placed at start of line.

                > Color coding
                    - Color codes apply to every line of code after the user has finished editing.
                    - Color coding colors and their typing
                        data type       foreEcolor          nativeColor    
                        ------------------------------------------------
                        comment         Accent              DarkGray
                        escape          Accent              Gray
                        operator        Accent              Gray
                        code            Normal              White
                        keyword         Correction          Magenta
                        plain text      Input               Yellow
                        reference       Highlight           Blue
                        error (msg)     Incorrection        Red      <-- this one is likely ignored...possibly       
             */

            // the color coding happens here, ofc...
            Program.ToggleFormatUsageVerification();
            if (fLine.IsNotNEW())
            {
                const string sComment = "//", sKeyw_if = "if", sKeyw_repeat = "repeat", sKeyw_else = "else";
                const char cPlain = '"', cEscStart = '&', cEscEnd = ';', cOpEqual = '=', cRefSteam = '$', cRefLibOpen = '{', cRefLibClose = '}';

                Dbug.Log($"Recieved '{fLine}'; Numbering types; ");
                /// numbering
                // TYPE NUMBERS (in order of precedence) :: code[0] comment[1] plaint[2] escape[3] reference[4] keyword[5] operator[6]
                string numberedCopy = "", fBatched = "";
                char prevFChar = '\0';
                bool hit1stNonSpaceQ = false, justHitNonSpaceQ = false;
                const int noIx = -1;
                int typeNum = 0, prevTypeNum = 0, ixLastBatched = 0, ixKeyWordEnd = noIx;
                bool isCommentQ = false, nPlainTQ = false, nEscQ = false, nKeyWordQ = false, nRefQ = false, isLibRefQ = false;

                Dbug.Log($"LEGEND :: Hit 1st Non-space '>>' (Just  '>|'); InPlaintTextBlock 'pl'; InEscapeBlock 'esc'; InKeywordBlock 'kw'; Operator 'op'; Reference (Library 'rfl', Steam 'rfs'); ");
                Dbug.NudgeIndent(true);
                for (int fx = 0; fx < fLine.Length; fx++)
                {
                    char fChar = fLine[fx]; //fx < fLine.Length ? fLine[fx] : '\0';
                    bool isSpaceChar = fChar == ' ', isEndQ = fx + 1 >= fLine.Length;
                    Dbug.LogPart($"prevChar = '{(prevFChar.IsNotNull() ? prevFChar : "")}'  char = '{(fChar.IsNotNull() ? fChar : "")}' | ");

                    if (!hit1stNonSpaceQ)
                    {
                        hit1stNonSpaceQ = !isSpaceChar;
                        if (hit1stNonSpaceQ)
                            justHitNonSpaceQ = true;
                        Dbug.LogPart($"{(hit1stNonSpaceQ ? ">|" : "")} ");
                    }
                    else Dbug.LogPart($">> ");

                    /// IF found first non-space char: identify types; ELSE default type as 'code[0]'
                    if (hit1stNonSpaceQ)
                    {
                        /// comment[1]
                        if (fChar == '/' && justHitNonSpaceQ)
                        {
                            Dbug.LogPart(" comment --> ");
                            if (GetStringFromChars(fx, 2) == sComment)
                            {
                                isCommentQ = true;
                                Dbug.LogPart("is comment line. No other types can be identified; ");
                            }
                        }
                        if (isCommentQ)
                            typeNum = 1;

                        // other identifications
                        if (!isCommentQ)
                        {
                            typeNum = 0;

                            /// operator[6]
                            if (fChar == cOpEqual)
                            {
                                typeNum = 6;
                                Dbug.LogPart($"op; ");
                            }    

                            /// keywords[5]                            
                            if (!nKeyWordQ && justHitNonSpaceQ)
                            {
                                Dbug.LogPart("Keyword (x3) --> ");
                                string theKeyword = "";
                                /// IF ...: keyword 'if'; ELSE IF ...: keyword 'else'; ELSE IF ...: keyword 'repeat'; 
                                if (GetStringFromChars(fx, sKeyw_if.Length) == sKeyw_if)
                                    theKeyword = sKeyw_if;
                                else if (GetStringFromChars(fx, sKeyw_else.Length) == sKeyw_else)
                                    theKeyword = sKeyw_else;
                                else if (GetStringFromChars(fx, sKeyw_repeat.Length) == sKeyw_repeat)
                                    theKeyword = sKeyw_repeat;

                                if (theKeyword.IsNotNE())
                                {
                                    nKeyWordQ = true;
                                    ixKeyWordEnd = fx + theKeyword.Length;
                                    //Dbug.LogPart($"ID'd keyword '{nKeyWord}' (ends @ix{ixKeyWordEnd}); ");
                                }
                            }
                            if (nKeyWordQ)
                            {
                                nKeyWordQ = ixKeyWordEnd > fx;
                                if (nKeyWordQ)
                                {
                                    typeNum = 5;
                                    Dbug.LogPart($"kw; ");
                                }
                            }

                            /// references[4]
                            if (nRefQ)
                            {
                                typeNum = 4;
                                Dbug.LogPart($"{(isLibRefQ ? "rfl" : "rfs")}; ");
                            }
                            if (!nRefQ)
                            {
                                /// library references
                                if (fChar == cRefLibOpen)
                                {
                                    isLibRefQ = true;
                                    nRefQ = true;
                                    typeNum = 4;
                                    Dbug.LogPart("rfl; ");
                                }
                                /// steam references
                                else if (fChar == cRefSteam)
                                {
                                    nRefQ = true;
                                    typeNum = 4;
                                    Dbug.LogPart("rfs; ");
                                }
                            }
                            else if (nRefQ)
                            {
                                /// library references
                                if (fChar == cRefLibClose)
                                {
                                    isLibRefQ = false;
                                    nRefQ = false;
                                }
                                /// steam references
                                else if (isSpaceChar)
                                    nRefQ = false;
                            }

                            /// plainText[2]
                            if (nPlainTQ)
                            {
                                typeNum = 2;
                                Dbug.LogPart("pl; ");
                            }
                            if (fChar == cPlain)
                            {
                                if (!nPlainTQ)
                                {
                                    Dbug.LogPart("pl; ");
                                    typeNum = 2;
                                }
                                nPlainTQ = !nPlainTQ;
                            }

                            /// escape[3]
                            if (nPlainTQ)
                            {
                                if (fChar == cEscStart)
                                    nEscQ = true;

                                if (nEscQ)
                                {
                                    Dbug.LogPart("esc; ");
                                    typeNum = 3;
                                }

                                if (fChar == cEscEnd)
                                    nEscQ = false;                                
                            }
                        }
                    }
                    
                    /// formatting happens here (printing in appropriate color)
                    numberedCopy += typeNum.ToString();
                    if (isEndQ || prevTypeNum != typeNum)
                    {
                        Dbug.LogPart($"Batch (in type #{prevTypeNum}) -> ");
                        fBatched = GetStringFromChars(ixLastBatched, fx - ixLastBatched);
                        ixLastBatched = fx;

                        // RE: TYPE NUMBERS (in order of precedence) :: code[0] comment[1] plaint[2] escape[3] reference[4] keyword[5] operator[6]
                        /// the first loop (the 'fBatched' value) // the second loop (when 'isEndQ', print last character appropriately
                        int batchTypeNum = prevTypeNum;
                        for (int rbx = 0; rbx < (isEndQ? 2 : 1); rbx++)
                        {
                            if (rbx != 0)
                            {
                                fBatched = fChar.ToString();
                                batchTypeNum = typeNum;
                                Dbug.LogPart($"Mini-batch (in type #{batchTypeNum}) -> '{fBatched}'");
                            }

                            Color batchColor = batchTypeNum switch
                            {
                                0 => colCode,
                                1 => colComment,
                                2 => colPlaint,
                                3 => colEscape,
                                4 => colRef,
                                5 => colKeyword,
                                6 => colOperator,
                                _ => colCode
                            };
                            Format(fBatched, batchColor, isEndQ && newLineQ && rbx != 0);
                        }
                        
                    }
                    Dbug.Log($"  |  typeNum = {typeNum}; ");

                    // just before end of loop
                    prevFChar = fChar;
                    prevTypeNum = typeNum;
                    justHitNonSpaceQ = false;

                    // METHOD
                    /// returns a string with 'length' length from 'fLine' starting at 'startIndex'. Returns remaind if length exceeds 'fLine.Length' 
                    /// Also partly logs "GetStr(@{startIndex},{length})[ret:'{getStr}']; "
                    string GetStringFromChars(int startIndex, int length)
                    {
                        string getStr = "";
                        for (int gx = startIndex; gx < (startIndex + length) && gx < fLine.Length; gx++)
                            getStr += fLine[gx];
                        Dbug.LogPart($"GetStr(@{startIndex},{length})[ret:'{getStr}']; ");
                        return getStr;
                    }
                }
                Dbug.NudgeIndent(false);
                Dbug.Log($"Final number copy :: {numberedCopy}; ");

                // for now
                //Format(numberedCopy);
            }
            Program.ToggleFormatUsageVerification();
            Dbug.EndLogging();
        }
    }
}
