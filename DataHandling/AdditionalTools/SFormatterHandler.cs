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
            - pv list SFI errors

            Methods
            - void ColorCode(str fLine)
            - void CheckSyntax(str[]     lineData)
            - SFI[]     GetErrors(int lineNum) 

            where 'SFI' is SFormatterInfo.cs for returning error message
         */

        static List<SFormatterInfo> errors;
        public static int ErrorCount
        {
            get
            {
                int errCount = 0;
                if (errors.HasElements())
                    errCount = errors.Count;
                return errCount;
            }
        }

        // METHODS
        public static void ColorCode(string fLine, bool useNativeQ = false, bool newLineQ = false, bool isErrorMsg = false)
        {
            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("SFormatterHandler.ColorCode()");
            Color colComment = useNativeQ ? Color.DarkGray : GetPrefsForeColor(ForECol.Accent);
            Color colEscape = useNativeQ ? Color.Gray : GetPrefsForeColor(ForECol.Accent);
            Color colOperator = useNativeQ ? Color.DarkGray : GetPrefsForeColor(ForECol.Accent);
            Color colCode = useNativeQ ? Color.White : GetPrefsForeColor(ForECol.Normal);
            Color colKeyword = useNativeQ ? Color.Magenta : GetPrefsForeColor(ForECol.Correction);
            Color colPlaint = useNativeQ ? Color.Yellow : GetPrefsForeColor(ForECol.InputColor);
            Color colRef = useNativeQ ? Color.Blue : GetPrefsForeColor(ForECol.Highlight);
            Color colErr = useNativeQ ? Color.Red : GetPrefsForeColor(ForECol.Incorrection);

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
            if (!isErrorMsg)
            {
                if (fLine.IsNotNEW())
                {
                    const string sComment = "//", sEsc1 = "&00;", sEsc2 = "&01;", sKeyw_if = "if", sKeyw_repeat = "repeat", sKeyw_else = "else", sKeyw_jump = "jump", sKeyw_next = "next";
                    const char cPlain = '"', cEscStart = '&', cEscEnd = ';', cOpEqual = '=', cOpUnequal = '!', cOpGreat = '>', cOpLess = '<', cRefSteam = '$', cRefLibOpen = '{', cRefLibClose = '}', cRepKey = '#';

                    Dbug.Log($"Recieved '{fLine}'; Numbering types; ");
                    /// numbering
                    // TYPE NUMBERS (in order of precedence) :: code[0]     comment[1]     plaint[2]     escape[3]     reference[4]     keyword[5]     operator[6]
                    string numberedCopy = "", fBatched;
                    char prevFChar = '\0';
                    bool hit1stNonSpaceQ = false, justHitNonSpaceQ = false;
                    const int noIx = -1;
                    int typeNum = 0, prevTypeNum = 0, ixLastBatched = 0, ixKeyWordEnd = noIx;
                    bool isCommentQ = false, nPlainTQ = false, nEscQ = false, nKeyWordQ = false, nRefQ = false, isLibRefQ = false, keywRepeatExistsQ = false;
                    bool enableMethodPartLogging = true;

                    Dbug.Log($"LEGEND :: Hit 1st Non-space '>>' (Just  '>|'); InPlaintTextBlock 'pl'; InEscapeBlock 'esc'; InKeywordBlock 'kw'; Operator 'op'; Reference (Library 'rfl', Steam 'rfs'); ");
                    Dbug.NudgeIndent(true);
                    for (int fx = 0; fx < fLine.Length; fx++)
                    {
                        char fChar = fLine[fx]; //fx < fLine.Length ? fLine[fx]     : '\0';
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
                                if (fChar == cOpEqual || fChar == cOpGreat || fChar == cOpLess)
                                {
                                    typeNum = 6;
                                    Dbug.LogPart($"op; ");
                                }
                                if (fChar == cOpUnequal && GetStringFromChars(fx, 2) == $"{cOpUnequal}{cOpEqual}")
                                {
                                    typeNum = 6;
                                    Dbug.LogPart($"op; ");
                                }

                                /// keywords[5]                                
                                if (!nKeyWordQ /*&& justHitNonSpaceQ*/)
                                {   /// they used to be only identified up front, but anywhere they still are (perhaps not agreeing with syntax though)
                                    if (!justHitNonSpaceQ && enableMethodPartLogging)
                                        enableMethodPartLogging = false;
                                    else Dbug.LogPart("Keyword (x5) --> ");

                                    string theKeyword = "";
                                    /// IF ...: keyword 'if'; ELSE IF ...: keyword 'else'; ELSE IF ...: keyword 'repeat'; 
                                    if (GetStringFromChars(fx, sKeyw_if.Length) == sKeyw_if)
                                        theKeyword = sKeyw_if;
                                    else if (GetStringFromChars(fx, sKeyw_else.Length) == sKeyw_else)
                                        theKeyword = sKeyw_else;
                                    else if (GetStringFromChars(fx, sKeyw_repeat.Length) == sKeyw_repeat)
                                    {
                                        theKeyword = sKeyw_repeat;
                                        keywRepeatExistsQ = true;
                                    }
                                    else if (GetStringFromChars(fx, sKeyw_jump.Length) == sKeyw_jump)
                                        theKeyword = sKeyw_jump;
                                    else if (GetStringFromChars(fx, sKeyw_next.Length) == sKeyw_next)
                                        theKeyword = sKeyw_next;

                                    if (!justHitNonSpaceQ && !enableMethodPartLogging)
                                        enableMethodPartLogging = true;

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
                                    {
                                        if (GetStringFromChars(fx, sEsc1.Length) == sEsc1 || GetStringFromChars(fx, sEsc2.Length) == sEsc2)
                                            nEscQ = true;
                                    }

                                    if (nEscQ)
                                    {
                                        Dbug.LogPart("esc; ");
                                        typeNum = 3;
                                    }

                                    if (fChar == cEscEnd)
                                        nEscQ = false;
                                }
                                
                                /// repeat key (as operator[6]) [over all others types]
                                if (fChar == cRepKey)
                                {
                                    if (keywRepeatExistsQ || !nPlainTQ)
                                    {
                                        Dbug.LogPart("op (repKey); ");
                                        typeNum = 6;
                                    }
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
                            for (int rbx = 0; rbx < (isEndQ ? 2 : 1); rbx++)
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
                            if (enableMethodPartLogging)
                                Dbug.LogPart($"GetStr(@{startIndex},{length})[ret:'{getStr}']; ");
                            return getStr;
                        }
                    }
                    Dbug.NudgeIndent(false);
                    Dbug.Log($"Final number copy :: {numberedCopy}; ");

                    // for now
                    //Format(numberedCopy);
                }
            }
            else if (isErrorMsg && fLine.IsNotNEW())
            {
                Format(fLine, colErr, newLineQ);
            }
            Program.ToggleFormatUsageVerification();
            Dbug.EndLogging();
        }
        public static void CheckSyntax(string[] lineData)
        {
            /** ERROR CHECKING from original formatting language plan
             > Error checking and messaging
                - Error checking occurs where syntax is to be followed and an argument is expected.
                    . Examples where error checking occurs is for code, proper references, keywords and their placementse
                    . Any line that starts with a comments remains unparsed; no error checking, it is ignorable
                - Error messaging occurs where expected syntax is not followed
                    . Each error message will be identified with a unique three-digit number and a token signifitying the type of error: G - General, R - Reference (Library and Steam Format specific). Every error has a unique three-digit number regardless of type. 'G001' and 'R001' cannot simultaneously exist, they are both '001'
                    . Each error message will be displayed below the line of code with those errors (see 'Editing Area Concept')
             */
            /** FORMATTER LANGUAGE SYNTAX (full revised help page)
                
                # G E N E R A L   S Y N T A X
                `    This functional language is case-sensitive.
                ▌    A value describes any input that derives from: a number, plain text, or (the property of) a library reference.

                SYNTAX                          :  OUTCOME
                --------------------------------:------------------------------------------------------------------------------------------
                  // text                       :  Line comment. Must be placed at the start of the line. Commenting renders a line
                                                :  imparsable.
                --------------------------------:------------------------------------------------------------------------------------------
                  text                          :  Code. Anything that is not commented is code and is parsable on steam log generation.
                --------------------------------:------------------------------------------------------------------------------------------
                  "text"                        :  Plain text. Represents any text that will be parsed into the generated steam log.
                --------------------------------:------------------------------------------------------------------------------------------
                  &00;                          :  Escape character. Used within plain text to print double quote character (").
                --------------------------------:------------------------------------------------------------------------------------------
                  {text}                        :  Library reference. References a value based on the information received from a submitted
                                                :  version log.
                                                :    Refer to 'Library Reference' below for more information.
                --------------------------------:------------------------------------------------------------------------------------------
                  $text                         :  Steam format reference. References a styling element to use against plain text or
                                                :  another value when generating steam log.
                                                :    Refer to 'Steam Format References' below for more information.
                --------------------------------:------------------------------------------------------------------------------------------
                  if # = #;                     :  Keyword. Must be placed at the start of the line.
                                                :    A control command that compares two values for a true or false condition. If the
                                                :  condition is 'true' then the line's remaining data will be parsed into the formatting
                                                :  string.
                                                :    The operator '=' compares two values to be equal. The operator '!=' compares two
                                                :  values to be unequal.
                --------------------------------:------------------------------------------------------------------------------------------
                  else;                         :  Keyword. Must be placed at the start of the line. Must be placed following an 'if'
                                                :  keyword line.
                                                :    A control command that will parse the line's remaining data when the condition of a
                                                :  preceding 'if' command is false.
                --------------------------------:------------------------------------------------------------------------------------------
                  repeat #;                     :  Keyword. Must be placed at the start of the line.
                                                :    A control command that repeats a line's remaining data '#' number of times. An
                                                :  incrementing number from one to given number '#' will replace any occuring '#' in the
                                                :  line's remaining data.
                --------------------------------:------------------------------------------------------------------------------------------
                  jump #;                       :  Keyword. Can only be placed following an 'if' or 'else' keyword.
                                                :    A control command the allows the parser to skip ahead to a given line. Only numbers
                                                :  are accepted as a value.
                --------------------------------:------------------------------------------------------------------------------------------
                  next;                         :  Keyword. Can only be placed following an 'if', 'else' or 'repeat' keyword.
                                                :    A control command that allows the combination of its line and the next line. The next
                                                :  line may not contain any keywords.
                --------------------------------:------------------------------------------------------------------------------------------


                # L I B R A R Y   R E F E R E N C E S
                `    Library reference values are provided by the information obtained from the version log submitted for steam log generation.
                ▌    Values returned from library references are as plain text.

                SYNTAX                          :  OUTCOME
                --------------------------------:------------------------------------------------------------------------------------------
                  {Version}                     :  Value. Gets the log version number (ex 1.00).
                --------------------------------:------------------------------------------------------------------------------------------
                  {AddedCount}                  :  Value. Gets the number of added item entries available.
                --------------------------------:------------------------------------------------------------------------------------------
                  {Added:#,prop}                :  Value Array. Gets value 'prop' from one-based added entry number '#'.
                                                :    Values for 'prop': ids, name.
                --------------------------------:------------------------------------------------------------------------------------------
                  {AdditCount}                  :  Value. Gets the number of additional item entries available.
                --------------------------------:------------------------------------------------------------------------------------------
                  {Addit:#,prop}                :  Value Array. Gets value 'prop' from one-based additional entry number '#'.
                                                :    Values for 'prop': ids, optionalName, relatedContent (related content name),
                                                :  relatedID.
                --------------------------------:------------------------------------------------------------------------------------------
                  {TTA}                         :  Value. Gets the number of total textures/contents added.
                --------------------------------:------------------------------------------------------------------------------------------
                  {UpdatedCount}                :  Value. Gets the number of updated item entries available.
                --------------------------------:------------------------------------------------------------------------------------------
                  {Updated:#,prop}              :  Value Array. Gets value 'prop' from one-based updated entry number '#'.
                                                :    Values for 'prop': changeDesc, id, relatedContent.
                --------------------------------:------------------------------------------------------------------------------------------
                  {LegendCount}                 :  Value. Gets the number of legend entries available.
                --------------------------------:------------------------------------------------------------------------------------------
                  {Legend:#,prop}               :  Value Array. Gets value 'prop' from one-based legend entry number '#'.
                                                :    Values for 'prop': definition, key
                --------------------------------:------------------------------------------------------------------------------------------
                  {SummaryCount}                :  Value. Gets the number of summary parts available.
                --------------------------------:------------------------------------------------------------------------------------------
                  {Summary:#}                   :  Value Array. Gets the value for one-based summary part number '#'.
                --------------------------------:------------------------------------------------------------------------------------------


                # S T E A M   F O R M A T   R E F E R E N C E S
                `    Steam format references are styling element calls that will affect the look of any text or value placed after it on
                ▌    log generation.
                ▌    Simple command references may be combined with other simple commands unless otherwise unpermitted. Simple commands
                ▌    affect only one value that follows them.
                ▌    Complex commands require a text or value to be placed in a described parameter surrounded by single quote characters
                ▌    (').

                SYNTAX                          :  OUTCOME
                --------------------------------:------------------------------------------------------------------------------------------
                  $h                            :  Simple command. Header text. Must be placed at the start of the line. May not be
                                                :  combined with other simple commands.
                                                :    There are three levels of header text. The header level follows the number of 'h's in
                                                :  reference. Example, a level three header text is '$hhh'.
                --------------------------------:------------------------------------------------------------------------------------------
                  $b                            :  Simple command. Bold text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $u                            :  Simple command. Underlined text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $i                            :  Simple command. Italicized text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $s                            :  Simple command. Strikethrough text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $sp                           :  Simple command. Spoiler text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $np                           :  Simple command. No parse. Doesn't parse steam format tags when generating steam log.
                --------------------------------:------------------------------------------------------------------------------------------
                  $c                            :  Simple command. Code text. Fixed width font, preserves space.
                --------------------------------:------------------------------------------------------------------------------------------
                  $hr                           :  Simple command. Horizontal rule. Must be placed on its own line. May not be combined
                                                :  with other simple commands.
                --------------------------------:------------------------------------------------------------------------------------------
                  $nl                           :  Simple command. New line.
                --------------------------------:------------------------------------------------------------------------------------------
                  $d                            :  Simple command. Indent.
                                                :    There are four indentation levels which relates to the number of 'd's in reference.
                                                :  Example, a level 2 indent is '$dd'.
                                                :    An indentation is the equivalent of two spaces (' 'x2).
                --------------------------------:------------------------------------------------------------------------------------------
                  $r                            :  Simple command. Regular. Used to forcefully demark the end of preceding simple commands.
                --------------------------------:------------------------------------------------------------------------------------------

                  $url='link':'name'            :  Complex command. Must be placed on its own line.
                                                :    Creates a website link by using URL address 'link' to create a hyperlink text
                                                :  described as 'name'.
                --------------------------------:------------------------------------------------------------------------------------------
                  $list[or]                     :  Complex command. Must be placed on its own line.
                                                :    Starts a list block. The optional parameter within square brackets, 'or', will
                                                :  initiate an ordered (numbered) list. Otherwise, an unordered list is initiated.
                --------------------------------:------------------------------------------------------------------------------------------
                  $*                            :  Simple command. Must be placed on its own line.
                                                :    Used within a list block to create a list item. Simple commands may follow to style
                                                :  the list item value or text.
                --------------------------------:------------------------------------------------------------------------------------------
                  $q='author':'quote'           :  Complex command. Must be placed on its own line.
                                                :    Generates a quote block that will reference an 'author' and display their original
                                                :  text 'quote'.
                --------------------------------:------------------------------------------------------------------------------------------
                  $table[nb,ec]                 :  Complex command. Must be placed on its own line.
                                                :    Starts a table block. There are two optional parameters within square brackets:
                                                :  parameter 'nb' will generate a table with no borders, parameter 'ec' will generate a
                                                :  table with equal cells.
                --------------------------------:------------------------------------------------------------------------------------------
                  $th='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                :    Used within a table block to create a table header row. Separate multiple columns of
                                                :  data with ','. Must follow immediately after a table block has started.
                --------------------------------:------------------------------------------------------------------------------------------
                  $td='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                :    Used within a table block to create a table data row. Separate multiple columns of
                                                :  data with ','.
                --------------------------------:------------------------------------------------------------------------------------------


                # S Y N T A X   E X C E P T I O N S
                SYNTAX                          :  OUTCOME
                --------------------------------:------------------------------------------------------------------------------------------
                  if # = #; if # = #;           :  The keyword 'if' may precede the keyword 'if' once more. The second 'if' may trigger a
                                                :  following 'else' keyword line.
                --------------------------------:------------------------------------------------------------------------------------------
                  else: if # = #;               :  The keyword 'else' may precede the keyword 'if'. This 'if' keyword may trigger a
                                                :  following 'else' keyword line.
                --------------------------------:------------------------------------------------------------------------------------------
                  repeat#: if # = #;            :  The keyword 'repeat' may precede the keyword 'if'. This 'if' keyword cannot trigger an
                                                :  'else' keyword line.
                --------------------------------:------------------------------------------------------------------------------------------
            
             */

            errors ??= new List<SFormatterInfo>();
            errors.Clear();
            bool identifyErrorStatesQ = true && Program.isDebugVersionQ;
            if (lineData.HasElements()) 
            {
                for (int lx = 0; lx < lineData.Length; lx++)
                {
                    string line = lineData[lx], prevLine = "", nextLine = "", prev2ndLine = "", next2ndLine = "";
                    /// next lines and previous lines ignore comments (source lines only)
                    if (lx > 0)
                    {
                        /// searches until 1st previous source line
                        int backPedalCount = 1;
                        while (lx - backPedalCount >= 0 && prevLine.IsNEW())
                        {
                            string aPreviousLine = lineData[lx - backPedalCount];
                            if (!aPreviousLine.TrimStart().StartsWith("//"))
                                prevLine = aPreviousLine;
                            backPedalCount++;
                        }
                        /// searches until 2nd previous source line
                        if (prevLine.IsNotNEW())
                        {
                            while (lx - backPedalCount >= 0 && prev2ndLine.IsNEW())
                            {
                                string aPreviousLine = lineData[lx - backPedalCount];
                                if (!aPreviousLine.TrimStart().StartsWith("//"))
                                    prev2ndLine = aPreviousLine;
                                backPedalCount++;
                            }
                        }

                        //prevLine = lineData[lx - 1];
                        //if (lx > 1)
                        //    prev2ndLine = lineData[lx - 2];
                    }
                    if (lx + 1 < lineData.Length)
                    {
                        /// searches until 1st next source line
                        int frontPedalCount = 1;
                        while (lx + frontPedalCount < lineData.Length && nextLine.IsNEW())
                        {
                            string aNextLine = lineData[lx + frontPedalCount];
                            if (!aNextLine.TrimStart().StartsWith("//"))
                                nextLine = aNextLine;
                            frontPedalCount++;
                        }
                        /// searches until 2nd next source line
                        if (nextLine.IsNotNEW())
                        {
                            while (lx + frontPedalCount < lineData.Length && next2ndLine.IsNEW())
                            {
                                string aNextLine = lineData[lx + frontPedalCount];
                                if (!aNextLine.TrimStart().StartsWith("//"))
                                    next2ndLine = aNextLine;
                                frontPedalCount++;
                            }
                        }

                        //nextLine = lineData[lx + 1];
                        //if (lx + 2 < lineData.Length)
                        //    next2ndLine = lineData[lx + 2];
                    }                    
                    string noPlainLine = RemovePlainText(line);
                    int lineNum = lx + 1;
                    List<string> unexpectedTokenII = new();


                    // ~~  GENERAL SYNTAX  ~~
                    /** GENERAL SYNTAX AND EXCEPTIONS - revised and errors debrief
                        `    This functional language is case-sensitive.
                        ▌    A value describes any input that derives from: a number, plain text, or (the property of) a library reference.

                        SYNTAX                          :  OUTCOME
                        --------------------------------:------------------------------------------------------------------------------------------
                          // text                       :  Line comment. Must be placed at the start of the line. Commenting renders a line
                                                        :  imparsable.
                        --------------------------------:------------------------------------------------------------------------------------------
                          text                          :  Code. Anything that is not commented is code and is parsable on steam log generation.
                        --------------------------------:------------------------------------------------------------------------------------------
                          "text"                        :  Plain text. Represents any text that will be parsed into the generated steam log.
                        --------------------------------:------------------------------------------------------------------------------------------
                          &00;                          :  Escape character. Used within plain text to print double quote character (").
                        --------------------------------:------------------------------------------------------------------------------------------
                          {text}                        :  Library reference. References a value based on the information received from a submitted
                                                        :  version log.
                                                        :    Refer to 'Library Reference' below for more information.
                        --------------------------------:------------------------------------------------------------------------------------------
                          $text                         :  Steam format reference. References a styling element to use against plain text or
                                                        :  another value when generating steam log.
                                                        :    Refer to 'Steam Format References' below for more information.
                        --------------------------------:------------------------------------------------------------------------------------------
                          if # = #:                     :  Keyword. Must be placed at the start of the line.
                                                        :    A control command that compares two values for a true or false condition. If the
                                                        :  condition is 'true' then the line's remaining data will be parsed into the formatting
                                                        :  string.
                                                        :    The operator '=' compares two values to be equal. The operator '!=' compares two
                                                        :  values to be unequal.
                        --------------------------------:------------------------------------------------------------------------------------------
                          else:                         :  Keyword. Must be placed at the start of the line. Must be placed following an 'if'
                                                        :  keyword line.
                                                        :    A control command that will parse the line's remaining data when the condition of a
                                                        :  preceding 'if' command is false.
                        --------------------------------:------------------------------------------------------------------------------------------
                          repeat #:                     :  Keyword. Must be placed at the start of the line.
                                                        :    A control command that repeats a line's remaining data '#' number of times. An
                                                        :  incrementing number from one to given number '#' will replace any occuring '#' in the
                                                        :  line's remaining data.
                        --------------------------------:------------------------------------------------------------------------------------------
                          jump #;                       :  Keyword. Can only be placed following an 'if' or 'else' keyword.
                                                        :    A control command the allows the parser to skip ahead to a given line. Only numbers
                                                        :  are accepted as a value.
                        --------------------------------:------------------------------------------------------------------------------------------
                          next;                         :  Keyword. Can only be placed following an 'if', 'else' or 'repeat' keyword.
                                                        :    A control command that allows the combination of its line and the next line. The next
                                                        :  line may not contain any keywords.
                        --------------------------------:------------------------------------------------------------------------------------------

                    
                    # S Y N T A X   E X C E P T I O N S
                        SYNTAX                          :  OUTCOME
                        --------------------------------:------------------------------------------------------------------------------------------
                          if # = #: $text               :  A (complex) steam format reference may be preceded by any keyword: 'if', 'else' or
                                                        :  'repeat'.
                        --------------------------------:------------------------------------------------------------------------------------------
                          if # = #: if # = #:           :  The keyword 'if' may precede the keyword 'if' once more. The second 'if' may trigger a
                                                        :  following 'else' keyword line.
                        --------------------------------:------------------------------------------------------------------------------------------
                          else: if # = #:               :  The keyword 'else' may precede the keyword 'if'. This 'if' keyword may trigger a
                                                        :  following 'else' keyword line.
                        --------------------------------:------------------------------------------------------------------------------------------
                          repeat#: if # = #:            :  The keyword 'repeat' may precede the keyword 'if'. This 'if' keyword cannot trigger an
                                                        :  'else' keyword line.
                        --------------------------------:------------------------------------------------------------------------------------------

                        NOTE :: The end control command character has been changed from ':' to ';'


                    GENERAL SYNTAX ERRORS
                    """""""""""""""""""""
                    Error code token: 'G'

                    COMMENTS
                    - (no error messages)
                        
                    CODE
                    - [G000]     Unexpected token '{token}'
                        . Accompanies most errors where general syntax is not followed.
                            jam     --> Unexpected token 'jam'
                            $       --> Unexpected token '$'
                            if :    --> Unexpected token ';'
                    
                    PLAIN TEXT
                    - [G001]     Empty plain text value
                        . Occurs when a plain text does not contain any data within its double quotations
                            ""      --> Empty plain text value
                    - [G002]     Closing double quotation expected
                        . Occurs when a plain text closing double quation is missing to signify end of plain text.
                            "       --> Closing double quotation expected
                            "bed    --> Closing double quotation expected
                     
                    ESCAPE CHARACTER
                    - [G003]     Unidentified escape character '{text}'
                        . Occurs when the plain text escape character does not follow the right format (&00;)
                            "&0;"   --> Unidentified escape character '&0;'
                            "&01;"  --> Unidentified escape character '&01;'

                    LIBRARY REFERENCE
                    - [G004]     Closing curly bracket expected
                        . Occurs when a library reference closing curly bracket is missing to signify end of the library reference
                            {       --> Closing curly bracket expected
                            {bunk   --> Closing curly bracket expected
                    - [G005]     Empty library reference
                        . Occurs when there is no reference within the curly brackets of a library reference.
                            {}      --> Empty library reference                    
                    - [G006]     Unidentified library reference
                        . Occurs when the library reference invalid
                            {bunk}  --> Unidentified library reference

                    STEAM FORMAT REFERENCE
                    - [G007]     Empty steam format reference
                        . Occurs when there is no reference following the dollar sign of a steam format reference
                            $       --> Empty steam format reference
                    - [G008]     Unidentified steam format reference
                        . Occurs when the steam format reference invalid
                            $op     --> Unidentified steam format reference

                    KEYWORDS
                    - [G009]     Closing semicolon expected
                        . Occurs when a keyword is missing a colon to signify the end of their command
                            if "a" = "a"    --> Closing colon expected
                            else            --> Closing colon expected
                            repeat 4        --> Closing colon expected
                    - [G010]     Keyword '{keyword}' expected at beginning
                        . Occurs when keywords are not placed at the beginning of the line
                            "and" else;     --> Keyword 'else' expected at beginning
                    - [G011]     Missing line to execute after keyword
                        . Occurs when nothing follows after a complete control command
                            if "a" = "a";   --> Missing line to execute after keyword
                            else;           --> Missing line to execute after keyword
                            repeat 4;       --> Missing line to execute after keyword
                    - [G012]     Misplaced keyword '{keyword}'
                        . Occurs when a line starting with a complete keyword contains an 'if' or 'jump' keyword that does not follow immediately after it
                            if "a" != "a"; "stop it" if;    --> Misplaced keyword 'if'
                            else; "waterbucket" if;         --> Misplaced keyword 'if'
                            repeat 4; "stinky" jump 3;      --> Misplaced keyword 'jump'
                            if "u" != "a"; "cute" next;     --> Misplaced keyword 'next'
                    - [G020]    Exceeded keyword limit per line
                        . Occurs when a line contains more than two keywords
                            else; if 1 != 0; jump 3;        --> Exceeded keyword limit per line
                    

                        IF KEYWORD
                        - [G013]     First comparable value expected
                            . Occurs when the first value of the condition is missing or is not a value
                                if          --> First comparable value expected
                                if ;        --> First comparable value expected
                                if =        --> First comparable value expected
                        - [G014]     Operator expected
                            . Occurs when the operator following the first value of the condition is missing
                                if "a"      --> Operator expected 
                        - [G015]     Unidentified operator
                            . Occurs when a valid operator does not follow after the first value
                                if "a" ;    --> Unidentified operator
                        - [G016]     Second comparable value expected
                            . Occurs when the second value of the condition is missing or is not a value
                                if "a" =    --> Second comparable value expected
                        - [G081]    Operator '{operator}' only compares numeric values
                            . Occurs when any operator including '<' or '>' is used with a non-numerical value (pure number or valid library reference)

                        ELSE KEYWORD
                        - [G017]     Missing preceding 'if' control line
                            . Occurs when an else keyword line does not follow immediately after an 'if' or 'else if' line
                                if "a" = "b";
                                "smoke"
                                else;       --> Missing preceding 'if' control line

                        REPEAT KEYWORD
                        - [G018]     First value expected
                            . Occurs when a valid value does not follow after a 'repeat' keyword
                                repeat ""   --> First value expected
                                repeat $h;  --> First value expected
                        - [G019]     First value is an invalid number
                            . Occurs when number that follows after a 'repeat' keyword is less than or equal to '1'
                                repeat -23; --> First value is an invalid number
                                repeat 1;   --> First value is an invalid number

                        JUMP KEYWORD
                        - [G021]    Line number expected
                            . Occurs when a value does not follow after a 'jump' keyword or is not a pure number
                                jump ;      --> Line number expected
                                jump "2";   --> Line number expected
                        - [G022]    Line number must follow after line '{lineNum}'
                            . Occurs when number that follows after 'jump' keyword is less than or equal to its current line
                                "This line 1"
                                jump 1;         --> Line number must follow after line '{lineNum}' 
                        - [G023]    Jump keyword must precede an appropriate keyword
                            . Occurs when a line containing a 'jump' keyword does not start with an 'if' or 'else' keyword
                                jump 1;             --> Missing first 'if' or 'else' keyword
                                repeat 2; jump 1    --> Missing first 'if' or 'else' keyword

                        NEXT KEYWORD
                        - [G076]    Next keyword must precede an appropriate keyword
                            . Occurs when a line containing a 'next' keyword does not start with an 'if', 'else', or 'repeat' keyword
                        - [G077]    Next keyword requires a following code line to function
                            . Occurs when a there is not a following line after a 'next' keyword line
                        - [G078]    Next keyword line cannot be followed by another keyword line
                            . Occurs when a line following a 'next' keyword line contains any keyword

                    ****/
                    if (!line.TrimStart().StartsWith("//"))
                    { /// section wrapping
                        const string tokenTag = "[token]";
                        string errorCode, errorMessage;
                        string token;

                        /** CODE errors
                       - [G000]     Unexpected token '{token}'
                           . Accompanies most errors where general syntax is not followed.
                               jam     --> Unexpected token 'jam'
                               $       --> Unexpected token '$'
                               if ;    --> Unexpected token ';'             ****/
                        for (int codeix = 0; codeix < 10; codeix++)
                        {
                            errorCode = "G000";
                            errorMessage = $"Unexpected token '{tokenTag}'";
                            token = null;

                            switch (codeix)
                            {
                                /// for comment
                                case 0:
                                    if (noPlainLine.CountOccuringCharacter('/') != 0)
                                        token = "/"; /// Ex: "the" /   -or-  / "comment not"         an actual comment line is not checked for syntax
                                    break;

                                /// for plain text
                                case 1:
                                    if (line.CountOccuringCharacter('"') % 2 != 0)
                                        token = "\""; /// Ex: "butter   -or-   butter"
                                    break;

                                /// for escape character (outside of plain text)
                                case 2:
                                    string plainOnly = RemovePlainText(line, true);
                                    if (plainOnly.CountOccuringCharacter('&') != plainOnly.CountOccuringCharacter(';') && plainOnly.Contains("&") && plainOnly.Contains(";"))
                                    {
                                        if (plainOnly.CountOccuringCharacter('&') > plainOnly.CountOccuringCharacter(';'))
                                            token = "&" + ID(0); /// Ex| &;& 
                                        else token = ";" + ID(20); /// Ex| &;;
                                    }
                                    /// the no plain errors take precedence over plain only errors
                                    if (noPlainLine.Contains("&"))
                                    {
                                        if (noPlainLine.SnippetText("&", ";", Snip.EndAft).IsNotNE())
                                            token = "&" + ID(1); /// Ex| &;   |  &  ;
                                        else if (noPlainLine.SnippetText(";", "&", Snip.EndAft).IsNotNE())
                                            token = ";" + ID(21); /// Ex| ; &  |  ;&
                                        else token = "&" + ID(2); /// Ex| &
                                    }
                                    else if (noPlainLine.SquishSpaces().StartsWith(";"))
                                        token = ";" + ID(22); /// Ex| ;
                                    break;

                                /// for library reference 
                                case 3:
                                    if (noPlainLine.CountOccuringCharacter('{') != noPlainLine.CountOccuringCharacter('}'))
                                    {
                                        if (noPlainLine.CountOccuringCharacter('{') > noPlainLine.CountOccuringCharacter('}'))
                                            token = "{" + ID(0);  /// Ex: {l} {
                                        else token = "}" + ID(0); /// Ex: {l}}
                                    }
                                    else /// IF countChar('{') == countChar('}')
                                    {
                                        if (noPlainLine.CountOccuringCharacter('{') != 0)
                                        {
                                            //if (noPlainLine.SnippetText("{", "}", Snip.EndAft).IsEW())
                                            if (noPlainLine.SquishSpaces().Contains("{}"))
                                                token = "}" + ID(1); /// Ex: {  }     why? The library reference expects reference name
                                            else
                                            {
                                                string[] libRefs = noPlainLine.LineBreak('{');
                                                for (int lrx = 0; lrx < libRefs.Length; lrx++)
                                                {
                                                    string libRef = $"{libRefs[lrx].SnippetText("{", "}", Snip.Inc)}";
                                                    if (libRef.Contains(":") || libRef.Contains(","))
                                                    {
                                                        if (libRef.CountOccuringCharacter(':') > libRef.CountOccuringCharacter(','))
                                                        {
                                                            if (libRef.CountOccuringCharacter(':') > 1)
                                                                token = ":" + ID(0); /// Ex|  {::,}  |  {:}
                                                        }
                                                        else if (libRef.CountOccuringCharacter(':') < libRef.CountOccuringCharacter(','))
                                                            token = "," + ID(0); /// Ex| {:,,}  |  {,}
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;

                                /// for steam format reference 
                                case 4:
                                    /// IF has steam char: ...; ELSE IF brackets only: ...;
                                    if (noPlainLine.CountOccuringCharacter('$') != 0)
                                    {
                                        if (noPlainLine.Contains("$ "))
                                            token = "$" + ID(0);  /// Ex:  $ h   -or-   "p" $ "h"
                                        else if (noPlainLine.Contains("[") && noPlainLine.Contains("]"))
                                        {
                                            string boxBracks = noPlainLine.SnippetText("[", "]", Snip.Inc, Snip.EndAft);
                                            string noSpaceNoPlain = noPlainLine.SquishSpaces();
                                            if (noSpaceNoPlain.Contains("$[") || noSpaceNoPlain.Contains("[$") || noSpaceNoPlain.Contains("$]") || noSpaceNoPlain.Contains("]$"))
                                                token = (boxBracks.IsNotNE() ? "[" : "]") + ID(0); /// Ex| (well, take a look at the conditions, eh?)
                                            else
                                            {
                                                string[] refParts = noPlainLine.LineBreak('$');
                                                for (int rpx = 0; rpx < refParts.Length && token.IsNE(); rpx++)
                                                {
                                                    string refPart = $"{refParts[rpx]}";
                                                    if (refPart.CountOccuringCharacter('[') + refPart.CountOccuringCharacter(']') > 2)
                                                    {
                                                        if (refPart.CountOccuringCharacter('[') >= refPart.CountOccuringCharacter(']'))
                                                            token = "[" + ID(4); /// 
                                                        else token = "]" + ID(4); /// Ex|  $list[] [  ]  |   $table[[]
                                                    }
                                                }
                                            }
                                        }
                                        else if (noPlainLine.Contains("[") && noPlainLine.SnippetText("$", "[").IsEW())
                                            token = "[" + ID(1); /// Ex|  $[  |  $ [  |  [$ 
                                        else if (noPlainLine.Contains("]") && noPlainLine.SnippetText("$", "]").IsEW())
                                            token = "]" + ID(1); /// Ex|  ]$  |  $]   |  $ ]
                                        else if (noPlainLine.EndsWith("$"))
                                            token = "$" + ID(1); /// Ex:  q $ 
                                        else if (noPlainLine.SquishSpaces().Contains("$$"))
                                            token = "$" + ID(2);
                                    }
                                    else if (noPlainLine.CountOccuringCharacter('[') != 0 || noPlainLine.CountOccuringCharacter(']') != 0)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('[') != 0 && noPlainLine.CountOccuringCharacter(']') == 0)
                                            token = "[" + ID(2); /// Ex| [  |  [[
                                        else if (noPlainLine.CountOccuringCharacter('[') == 0 && noPlainLine.CountOccuringCharacter(']') != 0)
                                            token = "]" + ID(2); /// Ex| ]  |  ]]
                                        else
                                        {
                                            string boxBracks = noPlainLine.SnippetText("[", "]", Snip.Inc);
                                            if (boxBracks.IsNE())
                                                boxBracks = noPlainLine.SnippetText("]", "[", Snip.Inc);
                                            if (boxBracks.IsNotNE())
                                                token = boxBracks[0].ToString() + ID(3); /// Ex|  []  |  [s]  |  ][  |  ] kjt [  
                                        }
                                    }
                                    break;

                                /// keyword 'if'
                                case 5:
                                    if (noPlainLine.Contains("if"))
                                    {
                                        string noPlainAfterColon = line.RemovePlainTextAfter(';');
                                        if (noPlainAfterColon.SnippetText("if", ";", Snip.EndAft).IsNEW())
                                        {
                                            if (noPlainAfterColon.Contains(';'))
                                                token = ";" + ID(0); /// Ex:   if    ;  -or-   if;   -or-  ;if
                                            else token = "if"; /// Ex: if "no"  -or-  "no" if
                                        }
                                        else
                                        { 
                                            /// for secondary 'if'
                                            string colonToColonSnip = noPlainLine.SnippetText(";", ";", Snip.EndAft);
                                            if (colonToColonSnip.IsNotNEW())
                                            {
                                                noPlainAfterColon = line.RemovePlainTextAfter(';', true);
                                                colonToColonSnip = noPlainAfterColon.SnippetText(";", ";", Snip.Inc, Snip.EndAft);

                                                if (colonToColonSnip.Contains("if") && colonToColonSnip.SnippetText("if", ";", Snip.EndAft).IsNEW())
                                                    token = ";" + ID(1); /// Ex| if 1=1; if  ;
                                            }
                                            else if (noPlainLine.CountOccuringCharacter(';') > 1)
                                                token = ";" + ID(2); /// Ex| if 0=1; ;  |  if 1=1; "what";
                                        }
                                    }                                    
                                    break;

                                /// keyword 'else'
                                case 6:
                                    if (noPlainLine.Contains("else"))
                                    {
                                        if (!noPlainLine.Contains("else;"))
                                        {
                                            if (!noPlainLine.Contains(";"))
                                                token = "else" + ID(0); /// Ex| else
                                            else
                                            {
                                                string snipElse = line.RemovePlainTextAfter(';', true, true).SnippetText("else", ";", Snip.Inc);
                                                token = snipElse.StartsWith("else")? snipElse + ID(1) : ";" + ID(3); /// Ex| else "no"  |  else "you" ;  |  else ;  | ; else
                                            }

                                        }
                                        else
                                        {
                                            string snipElseToLastColon = noPlainLine.SnippetText("else", ";", Snip.EndLast);
                                            if (snipElseToLastColon != null)
                                                if (snipElseToLastColon.CountOccuringCharacter(';') > 0 && !snipElseToLastColon.Contains("if") && !snipElseToLastColon.Contains("jump") && !snipElseToLastColon.Contains("next"))
                                                    token = ";" + ID(4); /// Ex| else;; ; |  else; repeat 4; |
                                        }
                                    }
                                    break;

                                /// keyword 'repeat'
                                case 7:
                                    if (noPlainLine.Contains("repeat"))
                                    {
                                        if (noPlainLine.SnippetText("repeat", ";").IsNEW())
                                        {
                                            string snipWithPlain = line.Replace("\"", "").SnippetText("repeat", ";");

                                            if (snipWithPlain.IsNEW())
                                            {
                                                if (noPlainLine.Contains(';'))
                                                    token = ";" + ID(5); /// Ex:   repeat;  -or-  repeat  ;   -or-   ;repeat
                                                else token = "repeat"; /// Ex: repeat "you"
                                            }
                                        }
                                        else
                                        {
                                            string snipRepToLastColon = noPlainLine.SnippetText("repeat", ";", Snip.EndLast);
                                            if (snipRepToLastColon != null)
                                                if (snipRepToLastColon.CountOccuringCharacter(';') > 0 && noPlainLine.SnippetText(";", ";", Snip.EndAft).IsNEW())
                                                    token = ";" + ID(6); /// Ex| repeat 4; ;;  |  repeat 5;; # "R"
                                        }
                                    }
                                    else if (noPlainLine.Contains("#"))
                                    {
                                        if (prevLine.IsNotNE())
                                        {
                                            if (!prevLine.RemovePlainText().Contains("next;"))
                                                token = "#" + ID(1); /// Ex| else; #
                                        }
                                        else
                                            token = "#" + ID(0); /// Ex| else; #
                                    }
                                    break;

                                /// keyword 'jump'
                                case 8:
                                    if (noPlainLine.Contains("jump"))
                                    {
                                        if (noPlainLine.SnippetText("jump", ";", Snip.EndAft).IsNEW())
                                        {
                                            if (noPlainLine.Contains(';'))
                                                token = ";" + ID(7); /// Ex:   jump;  -or-  jump ;   -or-   ;jump
                                            else token = "jump"; /// Ex: jump "you"
                                        }
                                        else
                                        {
                                            string snipRepToLastColon = noPlainLine.SnippetText("jump", ";", Snip.EndLast);
                                            if (snipRepToLastColon != null)
                                                if (snipRepToLastColon.CountOccuringCharacter(';') > 0 && noPlainLine.SnippetText(";", ";", Snip.EndAft).IsNEW())
                                                    token = ";" + ID(8); /// Ex| jump 4; ;;  |  jump 5;; # "R"
                                        }
                                    }
                                    break;

                                /// keyword 'next'
                                case 9:
                                    if (noPlainLine.Contains("next"))
                                    {
                                        if (!noPlainLine.Contains("next;"))
                                        {
                                            if (!noPlainLine.Contains(";"))
                                                token = "next" + ID(0);
                                            else
                                            {
                                                string snipElse = line.RemovePlainTextAfter(';', true, true).SnippetText("next", ";", Snip.Inc);
                                                token = snipElse.StartsWith("next") ? snipElse + ID(1) : ";" + ID(9); /// Ex| next "no"  |  next "you" ;  |  next ;  | ; next
                                            }

                                        }
                                        else
                                        {
                                            string snipRepToLastColon = noPlainLine.SnippetText("next", ";", Snip.EndLast);
                                            if (snipRepToLastColon != null)
                                                if (snipRepToLastColon.CountOccuringCharacter(';') > 0 && noPlainLine.SnippetText(";", ";", Snip.EndAft).IsNEW())
                                                    token = ";" + ID(10); /// Ex| next 4; ;;  |  next 5;; # "R"
                                        }
                                    }
                                    break;
                            }

                            if (token.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage.Replace(tokenTag, token));
                        }

                        /** PLAIN TEXT and ESCAPE CHARACTER errors
                        == pt ==
                        - [G001]     Empty plain text value
                            . Occurs when a plain text does not contain any data within its double quotations
                                ""      --> Empty plain text value
                        - [G002]     Closing double quotation expected
                            . Occurs when a plain text closing double quation is missing to signify end of plain text.
                                "       --> Closing double quotation expected
                                "bed    --> Closing double quotation expected           
                        
                         == esc ==
                        - [G003]     Unidentified escape character '{text}'
                            . Occurs when the plain text escape character does not follow the right format
                                "&0;"   --> Unidentified escape character '&0;'
                                "&02;"  --> Unidentified escape character '&02;'       ****/
                        for (int plainx = 0; plainx < 3; plainx++)
                        {
                            errorMessage = null;
                            errorCode = null;

                            switch (plainx)
                            {
                                /// plain text - empty
                                case 0:
                                    if (line.Contains('"'))
                                    {
                                        if (line.Contains("\"\""))
                                        {
                                            errorMessage = "Empty plain text value";
                                            errorCode = "G001";  /// Ex| ""
                                        }
                                    }                        
                                    break;

                                /// plain text - missing ending double quotation
                                case 1:
                                    if (line.CountOccuringCharacter('"') % 2 == 1)
                                    {
                                        errorMessage = "Closing double quotation expected";
                                        errorCode = "G002"; /// Ex|  "butter  |  shea "butter  |  "shea "butter"
                                    }
                                    break;

                                /// escape character - unidentified
                                case 2:
                                    if (line.Contains('"'))
                                    {
                                        errorMessage = $"Unidentified escape character '{tokenTag}'";
                                        token = null;
                                        string plainLine = RemovePlainText(line, true);
                                        if (plainLine.IsNotNEW())
                                            if (plainLine.Contains('&') && plainLine.Contains(';'))
                                            {
                                                errorCode = "G003";
                                                string[] possibleEscapes = plainLine.LineBreak('&');
                                                foreach (string possibleEsc in possibleEscapes)
                                                {
                                                    if (possibleEsc.Contains('&') && possibleEsc.Contains(";"))
                                                    {
                                                        string partPEsc = possibleEsc.SnippetText("&", ";", Snip.EndAft);
                                                        if (partPEsc.IsNE())
                                                            token = "&;"; /// Ex| "&;"
                                                        else if (partPEsc != "00" && partPEsc != "01")
                                                            token = $"&{partPEsc};"; /// Ex| "& ;"  |  "&02;"  |  "&34;"  |  "&ab;" 
                                                    }
                                                }
                                            }

                                        if (token.IsNotNE())
                                            errorMessage = errorMessage.Replace(tokenTag, token);
                                        else errorMessage = null;
                                    }
                                    break;
                            }

                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** LIBRARY REFERENCE errors
                        - [G004]     Closing curly bracket expected
                            . Occurs when a library reference closing curly bracket is missing to signify end of the library reference
                                {       --> Closing curly bracket expected
                                {bunk   --> Closing curly bracket expected
                        - [G005]     Empty library reference
                            . Occurs when there is no reference within the curly brackets of a library reference.
                                {}      --> Empty library reference                    
                        - [G006]     Unidentified library reference
                            . Occurs when the library reference is invalid
                                {bunk}  --> Unidentified library reference     *****/
                        for (int libx = 0; libx < 3; libx++)
                        {
                            errorMessage = null;
                            errorCode = null;

                            switch (libx)
                            {
                                /// closing curly expected
                                case 0:
                                    if (noPlainLine.Contains("{"))
                                    {
                                        if (noPlainLine.CountOccuringCharacter('{') > noPlainLine.CountOccuringCharacter('}'))
                                        {
                                            errorCode = "G004";
                                            errorMessage = "Closing curly bracket expected"; /// Ex|  {   |  {stomp  |  {and}  { 
                                        }
                                    }
                                    break;

                                /// empty reference
                                case 1:
                                    if (noPlainLine.SquishSpaces().Contains("{}"))
                                    {
                                        errorCode = "G005";
                                        errorMessage = "Empty library reference"; /// Ex| {}  |  {  }
                                    }
                                    break;

                                /// unidentified reference
                                case 2:
                                    if (noPlainLine.SnippetText("{", "}", Snip.EndAft).IsNotNEW())
                                    {
                                        string[] possibleParts = new string[1] { $"{noPlainLine.SnippetText("{", "}", Snip.EndAft)}}}" };
                                        if (noPlainLine.CountOccuringCharacter('{') > 1)
                                            possibleParts = noPlainLine.Split('{', StringSplitOptions.RemoveEmptyEntries);

                                        string[] libRefsStart = new string[]
                                        {
                                            /** lib references list (only need to start a certain way. Any further creeps into non-general errors territory
                                                {Version}
                                                {AddedCount}
                                                {Added:#,prop}
                                                {AdditCount}
                                                {Addit:#,prop}
                                                {TTA}
                                                {UpdatedCount}
                                                {Updated:#,prop}
                                                {LegendCount}
                                                {Legend:#,prop}
                                                {SummaryCount}
                                                {Summary:#}
                                             */

                                            "{Version", "{AddedCount", "{Added", "{AdditCount", "{Addit", "{TTA", 
                                            "{UpdatedCount", "{Updated", "{LegendCount", "{Legend", "{SummaryCount", "{Summary"
                                        };
                                        bool foundNonStartMatchingQ = false;

                                        token = null;
                                        for (int ppx = 0; ppx < possibleParts.Length && !foundNonStartMatchingQ; ppx++)
                                        {
                                            string possiblePart = $"{{{possibleParts[ppx]}"; /// "{" + "thisthing}whateverfollows...
                                            bool foundMatchQ = false;
                                            
                                            if (!possiblePart.SquishSpaces().Contains("{}"))
                                            {
                                                for (int lrsx = 0; lrsx < libRefsStart.Length && !foundMatchQ; lrsx++)
                                                    foundMatchQ = possiblePart.StartsWith(libRefsStart[lrsx]);

                                                if (!foundMatchQ)
                                                {
                                                    foundNonStartMatchingQ = true;
                                                    token = possiblePart.SnippetText("{", "}", Snip.Inc, Snip.EndAft);
                                                }
                                            }
                                        }

                                        if (foundNonStartMatchingQ && token.IsNotNE())
                                        {
                                            errorCode = "G006";
                                            errorMessage = $"Unidentified library reference '{token}'"; /// Ex| {stamper}  |  {TTA} {grumpy}
                                        }
                                    }
                                    break;
                            }

                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** STEAM FORMAT REFERENCE errors
                        - [G007]     Empty steam format reference
                            . Occurs when there is no reference following the dollar sign of a steam format reference
                                $       --> Empty steam format reference
                        - [G008]     Unidentified steam format reference
                            . Occurs when the steam format reference invalid
                                $op     --> Unidentified steam format reference             *****/
                        for (int sfx = 0; sfx < 2; sfx++)
                        {
                            errorMessage = null;
                            errorCode = null;

                            switch (sfx)
                            {
                                case 0:
                                    if (noPlainLine.Contains("$ ") || noPlainLine.EndsWith("$") || noPlainLine.SquishSpaces().Contains("$$"))
                                    {
                                        errorCode = "G007";
                                        errorMessage = "Empty steam format reference"; /// Ex| $  |  $ t   |  $ $u
                                    }
                                    break;

                                case 1:
                                    if (noPlainLine.Contains("$"))
                                    {
                                        string[] possibleParts = noPlainLine.Split('$');
                                        string[] steamRefsStart = new string[]
                                        {
                                            /** steam format references list (only need to start a certain way. Any further creeps into non-general errors territory
                                                
                                                  $h  $hh  $hhh
                                                  $b  $u   $i
                                                  $s  $sp  $np
                                                  $c  $hr  $nl
                                                  $d  $dd  $ddd  $dddd
                                                  $url='link':'name'
                                                  $list[or]
                                                  $*
                                                  $q='author':'quote'
                                                  $table[nb,ec]
                                                  $th='clm1','clm2'
                                                  $td='clm1','clm2'
                                             */

                                            "$h ", "$hh ", "$hhh ", "$b ", "$u ", "$i ",
                                            "$s ", "$sp ", "$np ", "$c ", "$hr", "$nl ", 
                                            "$d ", "$dd ", "$ddd ", "$dddd ", "$r ",
                                            "$url=", "$list[", "$* ", "$q=", "$table[", "$th=", "$td="
                                        };
                                        bool foundNonStartMatchingQ = false;

                                        token = null;
                                        const int stringClampDist = 7;
                                        for (int ppx = 0; ppx < possibleParts.Length && !foundNonStartMatchingQ; ppx++)
                                        {
                                            string possiblePart = $"${possibleParts[ppx].Clamp(stringClampDist)}".Trim(); /// $table[nb] --> $table  |  $url='goof.ca' --> $url='
                                            bool foundMatchQ = false;

                                            if (ppx != 0 && !possiblePart.Contains("$ ") && !possiblePart.EndsWith("$"))
                                            {
                                                for (int srsx = steamRefsStart.Length - 1; srsx >= 0 && !foundMatchQ; srsx--) /// length = 5  | srsx: 4 3 2 1 0
                                                {
                                                    string steamRefS = steamRefsStart[srsx];

                                                    if (possiblePart.Length > steamRefS.Length)
                                                        foundMatchQ = possiblePart.StartsWith(steamRefS);
                                                    else if (possiblePart.Length == steamRefS.Length)
                                                        foundMatchQ = possiblePart.Equals(steamRefS);
                                                    else if (possiblePart.Length < steamRefS.Length)
                                                        foundMatchQ = possiblePart.StartsWith(steamRefS.Trim());
                                                }

                                                if (!foundMatchQ)
                                                {
                                                    foundNonStartMatchingQ = true;
                                                    string partToken = possibleParts[ppx].SnippetText("$", " ", Snip.Inc, Snip.EndAft);
                                                    if (partToken.IsNotNEW())
                                                        token = partToken.Clamp(stringClampDist);
                                                    else token = possiblePart;
                                                }
                                            }
                                        }

                                        if (foundNonStartMatchingQ && token.IsNotNEW())
                                        {
                                            errorCode = "G008";
                                            errorMessage = $"Unidentified steam format reference '{token}'"; /// Ex| $412  |  $tible  |  $urll
                                        }                                        
                                    }
                                    break;                                    
                            }

                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** KEYWORDS errors
                        KEYWORDS
                        - [G009]     Closing colon expected
                            . Occurs when a keyword is missing a colon to signify the end of their command
                                if "a" = "a"    --> Closing colon expected
                                else            --> Closing colon expected
                                repeat 4        --> Closing colon expected
                        - [G010]     Keyword '{keyword}' expected at beginning
                            . Occurs when keywords are not placed at the beginning of the line
                                "and" else;     --> Keyword 'else' expected at beginning
                        - [G011]     Missing line to execute after keyword
                            . Occurs when nothing follows after a complete control command
                                if "a" = "a";   --> Missing line to execute after keyword
                                else;           --> Missing line to execute after keyword
                                repeat 4;       --> Missing line to execute after keyword
                        - [G012]     Misplaced keyword '{keyword}'
                            . Occurs when a line starting with a complete keyword contains an 'if' or 'jump' keyword that does not follow immediately after it
                                if "a" != "a"; "stop it" if;    --> Misplaced keyword 'if'
                                else; "waterbucket" if;         --> Misplaced keyword 'if'
                                repeat 4; "stinky" jump 3;          --> Misplaced keyword 'if' 
                        - [G020]    Exceeded keyword limit per line
                            . Occurs when a line contains more than two keywords
                                else; if 1 != 0; jump 3;        --> Exceeded keyword limit per line
                            

                            IF KEYWORD
                            - [G013]     First comparable value expected
                                . Occurs when the first value of the condition is missing or is not a value
                                    if          --> First comparable value expected
                                    if ;        --> First comparable value expected
                                    if =        --> First comparable value expected
                            - [G014]     Operator expected
                                . Occurs when the operator following the first value of the condition is missing
                                    if "a"      --> Operator expected 
                            - [G015]     Unidentified operator
                                . Occurs when an operator (either '=' or '!=') does not follow after the first value
                                    if "a" ;    --> Unidentified operator
                            - [G016]     Second comparable value expected
                                . Occurs when the second value of the condition is missing or is not a value
                                    if "a" =    --> Second comparable value expected
                            - [G081]    Operator '{operator}' only compares numeric values
                                . Occurs when any operator including '<' or '>' is used with a non-numerical value (pure number or valid library reference)

                            ELSE KEYWORD
                            - [G017]     Missing preceding 'if' control line
                                . Occurs when an else keyword line does not follow immediately after an 'if' or 'else if' line
                                    if "a" = "b";
                                    "smoke"
                                    else;       --> Missing preceding 'if' control line

                            REPEAT KEYWORD
                            - [G018]     First value expected
                                . Occurs when a valid value does not follow after a 'repeat' keyword
                                    repeat ""   --> First value expected
                                    repeat $h;  --> First value expected
                            - [G019]     First value is an invalid number
                                . Occurs when number that follows after a 'repeat' keyword is less than or equal to '1'
                                    repeat -23; --> First value is an invalid number
                                    repeat 1;   --> First value is an invalid number

                            JUMP KEYWORD
                            - [G021]    Line number expected
                                . Occurs when a value does not follow after a 'jump' keyword or is not a pure number
                                    jump ;      --> Line number expected
                                    jump "2";   --> Line number expected
                            - [G022]    Line number must follow after line '{lineNum}'
                                . Occurs when number that follows after 'jump' keyword is less than or equal to its current line
                                    "This line 1"
                                    jump 1;         --> Line number must follow after line '{lineNum}' 
                            - [G023]    Missing first 'if' or 'else' keyword
                                . Occurs when a line containing a 'jump' keyword does not start with an 'if' or 'else' keyword
                                    jump 1;             --> Missing first 'if' or 'else' keyword
                                    repeat 2; jump 1    --> Missing first 'if' or 'else' keyword          
                            - [G079]    Jump keyword expected at ending
                                . Occurs when a jump keyword is not at the end of the line
                         
                            NEXT KEYWORD
                            - [G076]    Next keyword must precede an appropriate keyword
                                . Occurs when a line containing a 'next' keyword does not start with an 'if', 'else', or 'repeat' keyword
                            - [G077]    Next keyword requires a following code line to function
                                . Occurs when a there is not a following line after a 'next' keyword line
                            - [G078]    Next keyword line cannot be followed by another keyword line
                                . Occurs when a line following a 'next' keyword line contains any keyword    
                            - [G080]    Next keyword expected at ending
                                . Occurs when a jump keyword is not at the end of the line
                         *****/
                        for (int kx = 0; kx < 6; kx++)
                        {
                            errorMessage = null;
                            errorCode = null;

                            switch (kx)
                            {
                                case 0:
                                    /** General Keyword errors
                                     - [G009]     Closing colon expected
                                        . Occurs when a keyword is missing a colon to signify the end of their command
                                            if "a" = "a"    --> Closing colon expected
                                            else            --> Closing colon expected
                                            repeat 4        --> Closing colon expected
                                    - [G010]     Keyword '{keyword}' expected at beginning
                                        . Occurs when keywords are not placed at the beginning of the line
                                            "and" else:     --> Keyword 'else' expected at beginning
                                    - [G011]     Missing line to execute after keyword
                                        . Occurs when nothing follows after a complete control command
                                            if "a" = "a":   --> Missing line to execute after keyword
                                            else:           --> Missing line to execute after keyword
                                            repeat 4:       --> Missing line to execute after keyword
                                    - [G012]     Misplaced keyword 'if'
                                        . Occurs when a line starting with a complete keyword contains an 'if' keyword that does not follow immediately after it
                                            if "a" != "a": "stop it" if:    --> Misplaced keyword 'if'
                                            else: "waterbucket" if:         --> Misplaced keyword 'if'
                                            repeat 4: "stinky" if:          --> Misplaced keyword 'if' 
                                    - [G020]    Exceeded keyword limit per line
                                        . Occurs when a line contains more than two keywords
                                            else; if 1 != 0; jump 3;        --> Exceeded keyword limit per line
                                     ****/
                                    for (int gkx = 0; gkx < 6; gkx++)
                                    {
                                        errorMessage = null;
                                        errorCode = null;

                                        switch (gkx)
                                        {
                                            /// closing semicolon expected
                                            case 0:
                                                int keywordCount = CountKeywordsInLine(noPlainLine);                                             
                                                if (keywordCount > noPlainLine.CountOccuringCharacter(';'))
                                                {
                                                    errorCode = "G009";
                                                    errorMessage = "Closing semicolon expected"; /// Ex|  if 1 = 2  |  else  |  repeat 7  |  jump 2  |  if 0 = 0; jump 2
                                                }
                                                break;

                                            /// keyword at beginning
                                            case 1:
                                                token = null;
                                                if (noPlainLine.Contains("if") && !line.StartsWith("if"))
                                                {
                                                    /// the following extra due to the 'second if' exceptions
                                                    if (!line.StartsWith("else") && !line.StartsWith("repeat"))
                                                        token = "if"; /// Ex| "plant" if  |  $b if
                                                }
                                                else if ((noPlainLine.Contains("else") && !line.StartsWith("else")) || noPlainLine.SnippetText("else", "else", Snip.Inc, Snip.EndAft).IsNotNE())
                                                    token = "else"; /// Ex| "plant" else  |  if ..; else;  |  else else;
                                                else if ((noPlainLine.Contains("repeat") && !line.StartsWith("repeat")) || noPlainLine.SnippetText("repeat", "repeat", Snip.Inc, Snip.EndAft).IsNotNE())
                                                    token = "repeat"; /// Ex| "plant" repeat |  if ..; repeat;  |  repeat; repeat
                                                if (token.IsNotNE())
                                                {
                                                    errorCode = "G010";
                                                    errorMessage = $"Keyword '{token}' expected at beginning";
                                                }
                                                break;

                                            /// missing execution line
                                            case 2:
                                                if (noPlainLine.Contains(";"))
                                                {
                                                    string snipControl = null;
                                                    if (line.StartsWith("if"))
                                                        snipControl = line.SnippetText("if", ";", Snip.All);
                                                    else if (line.StartsWith("else"))
                                                        snipControl = line.SnippetText("else", ";", Snip.All);
                                                    else if (line.StartsWith("repeat"))
                                                        snipControl = line.SnippetText("repeat", ";", Snip.All);

                                                    if (snipControl.IsNotNEW())
                                                        if (line.Replace(snipControl, "").IsNEW())
                                                        {
                                                            if (!snipControl.Contains("jump") && !snipControl.Contains("next"))
                                                            {
                                                                errorCode = "G011";
                                                                errorMessage = "Missing line to execute after keyword";
                                                                /// Ex| if 0=0; | if 0=0; if 1!=2; |  else; | else; if 1=1; |  repeat 2; | repeat 4; if 1 = #;
                                                            }
                                                        }
                                                }
                                                break;

                                            /// misplaced '{keyword}'
                                            case 3:
                                                if (noPlainLine.Contains(";"))
                                                {
                                                    string snipFullControl = null, snip1stControl = null;
                                                    if (line.StartsWith("if"))
                                                    {
                                                        snipFullControl = line.SnippetText("if", ";", Snip.Inc, Snip.End2nd);
                                                        snip1stControl = line.SnippetText("if", ";", Snip.Inc);
                                                    }
                                                    else if (line.StartsWith("else"))
                                                    {
                                                        snipFullControl = line.SnippetText("else", ";", Snip.Inc, Snip.End2nd);
                                                        snip1stControl = line.SnippetText("else", ";", Snip.Inc);
                                                    }
                                                    else if (line.StartsWith("repeat"))
                                                    {
                                                        snipFullControl = line.SnippetText("repeat", ";", Snip.Inc, Snip.End2nd);
                                                        snip1stControl = line.SnippetText("repeat", ";", Snip.Inc);
                                                    }

                                                    if (snipFullControl.IsNotNEW() && snip1stControl.IsNotNEW())
                                                        if (snipFullControl.Contains(snip1stControl) && snipFullControl.Length > snip1stControl.Length)
                                                        {
                                                            string remainingControl = snipFullControl.Substring(snip1stControl.Length);
                                                            string secondControl = remainingControl.SnippetText("if", ";", Snip.Inc, Snip.EndAft);
                                                            if (secondControl.IsNE())
                                                                secondControl = remainingControl.SnippetText("jump", ";", Snip.Inc, Snip.EndAft);
                                                            if (secondControl.IsNE())
                                                                secondControl = remainingControl.SnippetText("next", ";", Snip.Inc, Snip.EndAft);

                                                            bool misplacedIfQ = !remainingControl.SquishSpaces().StartsWith("if") && remainingControl.Contains("if");
                                                            bool misplacedJumpQ = !remainingControl.SquishSpaces().StartsWith("jump") && remainingControl.Contains("jump");
                                                            bool misplacedNextQ = !remainingControl.SquishSpaces().StartsWith("next") && remainingControl.Contains("next");
                                                            if ((misplacedIfQ || misplacedJumpQ || misplacedNextQ) && secondControl.IsNotNEW())
                                                            {
                                                                errorCode = "G012";
                                                                errorMessage = $"Misplaced keyword '{secondControl}'";
                                                            }
                                                        }
                                                }
                                                break;

                                            /// keyword limit exceeded
                                            case 4:
                                                int countKeywords = CountKeywordsInLine(noPlainLine);
                                                if (countKeywords > 2)
                                                {
                                                    bool triggerG020q = true;
                                                    string snipJump = noPlainLine.SnippetText("jump", ";", Snip.Inc, Snip.EndAft);
                                                    string snipNext = noPlainLine.SnippetText("next", ";", Snip.Inc, Snip.EndAft);

                                                    if (countKeywords == 3)
                                                    {
                                                        if (snipJump.IsNotNEW())
                                                            if (line.TrimEnd().EndsWith(snipJump))
                                                                triggerG020q = false;
                                                        if (snipNext.IsNotNEW() && triggerG020q)
                                                            if (line.TrimEnd().EndsWith(snipNext))
                                                                triggerG020q = false;
                                                    }

                                                    if (triggerG020q)
                                                    {
                                                        errorCode = "G020";
                                                        errorMessage = "Exceeded keyword limit per line";
                                                    }
                                                }
                                                break;
                                        }

                                        if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;

                                case 1:
                                    /** IF keyword errors
                                     - [G013]     First comparable value expected
                                        . Occurs when the first value of the condition is missing or is not a value
                                            if          --> First comparable value expected
                                            if ;        --> First comparable value expected
                                            if =        --> First comparable value expected
                                    - [G014]     Operator expected
                                        . Occurs when the operator following the first value of the condition is missing
                                            if "a"      --> Operator expected 
                                    - [G015]     Unidentified operator
                                        . Occurs when a valid operator does not follow after the first value
                                            if "a" ;    --> Unidentified operator
                                    - [G016]     Second comparable value expected
                                        . Occurs when the second value of the condition is missing or is not a value
                                            if "a" =    --> Second comparable value expected
                                    - [G081]    Operator '{operator}' only compares numeric values
                                        . Occurs when any operator including '<' or '>' is used with a non-numerical value (pure number or valid library reference)
                                     ****/
                                    /// some required setup
                                    List<string> snipIfs = new List<string>();
                                    if (noPlainLine.Contains(";"))
                                    {
                                        string snip1stIf = null, snip2ndIf = "";
                                        string[] keywordParts = line.RemoveFromPlainText(';', true, '\n').LineBreak(';', true);

                                        if (keywordParts.HasElements())
                                        {
                                            if (keywordParts[0].RemovePlainText().Contains("if"))
                                                snip1stIf = keywordParts[0];

                                            if (keywordParts.HasElements(2))
                                                if (keywordParts[1].RemovePlainText().Contains("if"))
                                                    snip2ndIf = keywordParts[1];
                                        }

                                        if (snip1stIf.IsNotNEW())
                                            snipIfs.Add(snip1stIf);
                                        if (snip2ndIf.IsNotNEW())
                                            snipIfs.Add(snip2ndIf);
                                    }
                                    for (int fx = 0; fx < 5 && snipIfs.HasElements(); fx++)
                                    {
                                        foreach (string snipIf in snipIfs)
                                        {
                                            errorMessage = null;
                                            errorCode = null;

                                            string noPlainSnipIf = snipIf.RemovePlainText();
                                            switch (fx)
                                            {
                                                /// first value missing
                                                case 0:                                                    
                                                    if (!IsValidValue(snipIf, true))
                                                    {
                                                        errorCode = "G013";
                                                        errorMessage = "First comparable value expected";
                                                    }
                                                    break;

                                                /// operator expected
                                                case 1:           
                                                    if (!noPlainLine.Contains("=") && !noPlainSnipIf.Contains("!") && !noPlainSnipIf.Contains(">") && !noPlainSnipIf.Contains("<"))
                                                    {
                                                        errorCode = "G014";
                                                        errorMessage = "Operator expected";
                                                    }
                                                    break;

                                                /// invalid operator
                                                case 2:
                                                    // string[] validOpts = new string[] { "=", "!=", "<=", "<", ">=", ">="};
                                                    bool triggerG015q = true;
                                                    if (noPlainSnipIf.Contains("="))
                                                    {
                                                        bool oneEqualSign = noPlainSnipIf.CountOccuringCharacter('=') == 1;
                                                        if (noPlainSnipIf.Contains("!=") && noPlainSnipIf.CountOccuringCharacter('!') == 1 && oneEqualSign)
                                                            triggerG015q = false;
                                                        else if (noPlainSnipIf.Contains("<=") && noPlainSnipIf.CountOccuringCharacter('<') == 1 && oneEqualSign)
                                                            triggerG015q = false;
                                                        else if (noPlainSnipIf.Contains(">=") && noPlainSnipIf.CountOccuringCharacter('>') == 1 && oneEqualSign)
                                                            triggerG015q = false;
                                                        else if (oneEqualSign)
                                                            triggerG015q = false;
                                                    }
                                                    else if (!noPlainSnipIf.Contains("!"))
                                                    {
                                                        if (noPlainSnipIf.Contains("<") && !noPlainSnipIf.Contains(">") && noPlainSnipIf.CountOccuringCharacter('<') == 1)
                                                            triggerG015q = false;
                                                        if (noPlainSnipIf.Contains(">") && !noPlainSnipIf.Contains("<") && noPlainSnipIf.CountOccuringCharacter('>') == 1)
                                                            triggerG015q = false;
                                                    }

                                                    if (triggerG015q)
                                                    {
                                                        errorCode = "G015";
                                                        errorMessage = "Unidentified operator";
                                                    }
                                                    break;

                                                /// second value missing
                                                case 3:
                                                    if (!IsValidValue(snipIf, false))
                                                    {
                                                        errorCode = "G016";
                                                        errorMessage = "Second comparable value expected";
                                                    }
                                                    break;

                                                /// operator only compares numeric values
                                                case 4:
                                                    bool numValuesOnlyQ = noPlainSnipIf.Contains(">") || noPlainSnipIf.Contains("<");
                                                    string supposedOp = "";
                                                    if (noPlainSnipIf.Contains(">="))
                                                        supposedOp = ">=";
                                                    else if (noPlainSnipIf.Contains("<="))
                                                        supposedOp = "<=";
                                                    else if (noPlainSnipIf.Contains(">"))
                                                        supposedOp = ">";
                                                    else if (noPlainSnipIf.Contains("<"))
                                                        supposedOp = "<";
                                                    
                                                    if (numValuesOnlyQ && supposedOp.IsNotNE())
                                                    {
                                                        if (!IsValidValue(snipIf, true) || !IsValidValue(snipIf, false))
                                                        {
                                                            errorCode = "G081";
                                                            errorMessage = $"Operator '{supposedOp}' only compares numeric values";
                                                        }
                                                    }
                                                    break;
                                            }

                                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                                AddToErrorList(lineNum, errorCode, errorMessage);
                                        }


                                        bool IsValidValue(string snipIf, bool val1Q)
                                        {
                                            string[] validValues = new string[]
                                            {
                                                /** Valid values
                                                    Numerics (0-9)
                                                    Plain text "anything"
                                                    Library References (match with start)
                                                        {Version}
                                                        {AddedCount}
                                                        {Added:#,prop}
                                                        {AdditCount}
                                                        {Addit:#,prop}
                                                        {TTA}
                                                        {UpdatedCount}
                                                        {Updated:#,prop}
                                                        {LegendCount}
                                                        {Legend:#,prop}
                                                        {SummaryCount}
                                                        {Summary:#}
                                                    Repeat Replacement (#)
                                                    */

                                                "{Version", "{AddedCount", "{Added", "{AdditCount", "{Addit", "{TTA",
                                                "{UpdatedCount", "{Updated", "{LegendCount", "{Legend", "{SummaryCount", "{Summary"
                                            };
                                            int countValid = 0;
                                            if (snipIf.IsNotNE())
                                            {
                                                string valueSnip;
                                                bool numValuesOnlyQ = snipIf.RemovePlainText().Contains(">") || snipIf.RemovePlainText().Contains("<");

                                                /// get values
                                                if (val1Q)
                                                {
                                                    valueSnip = snipIf.SnippetText("if", "=");
                                                    if (valueSnip.IsNE())
                                                    {
                                                        valueSnip = snipIf.SnippetText("if", ">");
                                                        if (valueSnip.IsNE())
                                                            valueSnip = snipIf.SnippetText("if", "<");
                                                        if (valueSnip.IsNE())
                                                            valueSnip = snipIf.SnippetText("if", ";");
                                                    }                                                    
                                                }
                                                else
                                                {
                                                    valueSnip = snipIf.SnippetText("=", ";");
                                                    if (valueSnip.IsNE())
                                                    {
                                                        valueSnip = snipIf.SnippetText(">", ";");
                                                        if (valueSnip.IsNE())
                                                            valueSnip = snipIf.SnippetText("<", ";");
                                                    }                                                        
                                                }

                                                /// filter op chars
                                                if (!valueSnip.IsNE())
                                                {
                                                    if (valueSnip.EndsWith("!") || valueSnip.EndsWith("<") || valueSnip.EndsWith(">") || valueSnip.EndsWith("="))
                                                        valueSnip = valueSnip[..^1];
                                                    if (valueSnip.StartsWith("!") || valueSnip.StartsWith("<") || valueSnip.StartsWith(">") || valueSnip.StartsWith("="))
                                                        valueSnip = valueSnip[..1];
                                                }

                                                /// validate values
                                                if (valueSnip.IsNotNEW())
                                                {
                                                    for (int vvx = 0; vvx < validValues.Length; vvx++)
                                                    {
                                                        if (valueSnip.Trim().StartsWith("{"))
                                                        {
                                                            if (!numValuesOnlyQ)
                                                            {
                                                                if (valueSnip.Trim().StartsWith(validValues[vvx]))
                                                                    countValid++;
                                                            }
                                                            else
                                                            {
                                                                if (validValues[vvx].EndsWith("Count"))
                                                                {
                                                                    if (valueSnip.Trim().StartsWith(validValues[vvx]))
                                                                        countValid++;
                                                                }
                                                            }
                                                        }
                                                        else if (valueSnip.Contains('\"'))
                                                        {
                                                            if (!numValuesOnlyQ)
                                                                countValid++;
                                                            else if (int.TryParse(valueSnip.RemovePlainText(true), out _))
                                                                countValid++;
                                                        }
                                                        else if (valueSnip.Contains("#"))
                                                            countValid++;
                                                        else
                                                        {
                                                            if (int.TryParse(valueSnip, out _))
                                                                countValid++;
                                                        }
                                                    }
                                                }
                                            }

                                            return countValid != 0;
                                        }
                                    }
                                    break;

                                case 2:
                                    /** ELSE keyword errors
                                     - [G017]     Missing preceding 'if' control line
                                        . Occurs when an else keyword line does not follow immediately after an 'if' or 'else if' line
                                            if "a" = "b";
                                            "smoke"
                                            else;       --> Missing preceding 'if' control line
                                     ****/
                                    bool anElseIssue = false;
                                    if (noPlainLine.StartsWith("else"))
                                    {
                                        if (prevLine.IsNE())
                                            anElseIssue = true; /// Ex| [L1]else; |
                                        else
                                        {
                                            string noPlainPrevLine = prevLine.RemovePlainText();
                                            if (noPlainPrevLine.SnippetText("if", ";", Snip.Inc, Snip.EndAft).IsNE() || prevLine.StartsWith("repeat"))
                                                anElseIssue = true;

                                            if (noPlainPrevLine.StartsWith("if") && anElseIssue)
                                                anElseIssue = false;
                                        }
                                    }
                                    if (anElseIssue)
                                    {
                                        errorCode = "G017";
                                        errorMessage = "Missing preceding 'if' control line";
                                    }
                                    break;

                                case 3:
                                    /** REPEAT keyword errors
                                     - [G018]     First value expected
                                        . Occurs when a valid value does not follow after a 'repeat' keyword
                                            repeat ""   --> First value expected
                                            repeat $h;  --> First value expected
                                    - [G019]     First value is an invalid number
                                        . Occurs when number that follows after a 'repeat' keyword is less than or equal to '1'
                                            repeat -23; --> First value is an invalid number
                                            repeat 1;   --> First value is an invalid number
                                     ****/
                                    for (int repx = 0; repx < 2; repx++)
                                    {
                                        errorMessage = null;
                                        errorCode = null;

                                        switch (repx)
                                        {
                                            /// first value expected
                                            case 0:
                                                if (line.StartsWith("repeat"))
                                                {
                                                    if (line.SnippetText("repeat", ";", Snip.EndAft).IsNEW())
                                                    {
                                                        errorCode = "G018";
                                                        errorMessage = "First value expected";
                                                    }
                                                }
                                                break;

                                            /// first value NavN
                                            case 1:
                                                if (line.StartsWith("repeat"))
                                                {
                                                    string snipRepeat = line.SnippetText("repeat", ";", Snip.EndAft);
                                                    if (snipRepeat.IsNotNEW())
                                                    {
                                                        string[] validValue = new string[]
                                                        {
                                                            /** VALID REPEAT VALUES
                                                                Pure numerics (0~9)             ---  [x] not any more...
                                                                Plain text numerics "0"~"9"    
                                                                Library References ending in 'Count' and 'TTA'
                                                                    {AddedCount}
                                                                    {AdditCount}
                                                                    {TTA}
                                                                    {UpdatedCount}
                                                                    {LegendCount}
                                                                    {SummaryCount}                                                             
                                                             */
                                                            "{AddedCount}", "{AdditCount}", "{TTA}", "{UpdatedCount}", "{LegendCount}", "{SummaryCount}",
                                                        };

                                                        bool invalidValue;
                                                        /// valid lib references
                                                        if (snipRepeat.Contains("{"))
                                                        {
                                                            int countNonMatch = 0;
                                                            for (int vvx = 0; vvx < validValue.Length; vvx++)
                                                                if (snipRepeat.Trim() != validValue[vvx])
                                                                    countNonMatch++;
                                                            invalidValue = countNonMatch == validValue.Length;

                                                            if (invalidValue)
                                                                snipRepeat = line.SnippetText("{", "}", Snip.Inc);
                                                        }
                                                        /// numbers
                                                        else
                                                        {
                                                            if (int.TryParse(snipRepeat, out int num))
                                                                invalidValue = num < 1;
                                                            else invalidValue = true;
                                                        }

                                                        if (invalidValue)
                                                        {
                                                            errorCode = "G019";
                                                            errorMessage = $"First value{(identifyErrorStatesQ ? $" {snipRepeat.Trim()}" : "")} is an invalid number";
                                                            /// Ex| repeat "w";  |  repeat r;  |  repeat {Summary,1};  |  repeat 0
                                                        }
                                                    }
                                                }
                                                break;
                                        }

                                        if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;

                                case 4:
                                    /** JUMP keyword errors
                                    - [G021]    Line number expected
                                        . Occurs when a value does not follow after a 'jump' keyword or is not a pure number
                                            jump ;      --> Line number expected
                                            jump "2";   --> Line number expected
                                    - [G022]    Line number must follow after line '{lineNum}'
                                        . Occurs when number that follows after 'jump' keyword is less than or equal to its current line
                                            "This line 1"
                                            jump 1;         --> Line number must follow after line '{lineNum}' 
                                    - [G023]    Jump keyword must precede an appropriate keyword
                                        . Occurs when a line containing a 'jump' keyword does not start with an 'if' or 'else' keyword
                                            jump 1;             --> Missing first 'if' or 'else' keyword
                                            repeat 2; jump 1    --> Missing first 'if' or 'else' keyword    
                                    - [G079]    Jump keyword expected at ending
                                        . Occurs when a jump keyword is not at the end of the line          ******/
                                    for (int jx = 0; jx < 4; jx++)
                                    {
                                        errorMessage = null;
                                        errorCode = null;

                                        string snipJump = noPlainLine.SnippetText("jump", ";", Snip.Inc, Snip.EndAft);
                                        switch (jx)
                                        {
                                            /// expect line num
                                            case 0:
                                                if (noPlainLine.Contains("jump"))
                                                {
                                                    bool noLineNumQ = true;
                                                    if (snipJump.IsNotNEW())
                                                    {
                                                        string value1 = snipJump.SnippetText("jump", ";");
                                                        if (int.TryParse(value1, out _))
                                                            noLineNumQ = false;
                                                    }

                                                    if (noLineNumQ)
                                                    {
                                                        errorCode = "G021";
                                                        errorMessage = "Line number expected"; /// Ex|  jump "4";  |  jump w;  |  jump {AddedCount} |  jump ;
                                                    }
                                                }
                                                break;

                                            /// line num <= jump line
                                            case 1:
                                                if (noPlainLine.Contains("jump") && snipJump.IsNotNEW())
                                                {
                                                    if (int.TryParse(snipJump.SnippetText("jump", ";"), out int jumpLineNum))
                                                        if (jumpLineNum <= lineNum)
                                                        {
                                                            errorCode = "G022";
                                                            errorMessage = $"Line number must follow after line '{lineNum}'"; /// Ex| [L1] jump 1; |  [L2] jump 1;
                                                        }
                                                }
                                                break;

                                            /// missing 'if' or 'else'
                                            case 2:
                                                if (noPlainLine.Contains("jump") && !line.StartsWith("if") && !line.StartsWith("else"))
                                                {
                                                    errorCode = "G023";
                                                    errorMessage = "Jump keyword must precede an appropriate keyword"; /// Ex| repeat 3; jump 2; | jump; else;  |  jump;
                                                    //errorMessage = "Missing first 'if' or 'else' keyword";
                                                }
                                                break;

                                            /// end of line
                                            case 3:
                                                if (noPlainLine.Contains("jump") && snipJump.IsNotNEW())
                                                {
                                                    if (!line.TrimEnd().EndsWith(snipJump))
                                                    {
                                                        errorCode = "G079";
                                                        errorMessage = "Jump keyword expected at ending";
                                                    }
                                                }
                                                break;

                                        }

                                        if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;

                                case 5:
                                    /** NEXT KEYWORD errors
                                    - [G076]    Next keyword must precede an appropriate keyword
                                        . Occurs when a line containing a 'next' keyword does not start with an 'if', 'else', or 'repeat' keyword
                                    - [G077]    Next keyword requires a following code line to function
                                        . Occurs when a there is not a following line after a 'next' keyword line
                                    - [G078]    Next keyword line cannot be followed by another keyword line
                                        . Occurs when a line following a 'next' keyword line contains any keyword 
                                    - [G080]    Next keyword expected at ending
                                        . Occurs when a jump keyword is not at the end of the line      *****/
                                    for (int nx = 0; nx < 4; nx++)
                                    {
                                        errorCode = null;
                                        errorMessage = null;

                                        string snipNext = noPlainLine.SnippetText("next", ";", Snip.Inc, Snip.EndAft);
                                        if (snipNext.IsNotNE())
                                        {
                                            if (!line.StartsWith("if") && !line.StartsWith("else") && !line.StartsWith("repeat") && nx == 0)
                                            {
                                                errorCode = "G076";
                                                errorMessage = "Next keyword must precede an appropriate keyword";
                                            }
                                            else
                                            {
                                                if (nx == 1)
                                                {
                                                    bool triggerG077q = nextLine.IsNEW();
                                                    if (nextLine.IsNotNEW())
                                                        triggerG077q = nextLine.TrimStart().StartsWith("//");

                                                    if (triggerG077q)
                                                    {
                                                        errorCode = "G077";
                                                        errorMessage = "Next keyword requires a following code line to function";
                                                    }
                                                }
                                                else if (CountKeywordsInLine(nextLine) > 0 && nx == 2)
                                                {
                                                    errorCode = "G078";
                                                    errorMessage = "Next keyword line cannot be followed by another keyword line";
                                                }
                                                else if (nx == 3 && !line.TrimEnd().EndsWith(snipNext))
                                                {
                                                    errorCode = "G080";
                                                    errorMessage = "Next keyword expected at ending";
                                                }
                                            }
                                        }

                                        if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;
                            }

                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);

                            // METHOD FOR KEYWORDS
                            static int CountKeywordsInLine(string theLine)
                            {
                                int keywordCount = 0;
                                for (int ckx = 0; ckx < 5; ckx++)
                                {
                                    keywordCount += ckx switch
                                    {
                                        0 => theLine.Contains("if") ? theLine.Replace("if","\n").CountOccuringCharacter('\n') : 0,
                                        1 => theLine.Contains("else") ? 1 : 0,
                                        2 => theLine.Contains("repeat") ? 1 : 0,
                                        3 => theLine.Contains("jump") ? 1 : 0,
                                        4 => theLine.Contains("next") ? 1 : 0,
                                        _ => 0
                                    };
                                }
                                return keywordCount;
                            }
                        }

                    }



                    // ~~  LIBRARY REFERENCE SYNTAX  ~~                    
                    /** LIBRARY REFERENCE SYNTAX - revised and errors debrief
                        # L I B R A R Y   R E F E R E N C E S
                        `    Library reference values are provided by the information obtained from the version log submitted for steam log generation.
                        ▌    Values returned from library references are as plain text.

                        SYNTAX                          :  OUTCOME
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Version}                     :  Value. Gets the log version number (ex 1.00).
                        --------------------------------:------------------------------------------------------------------------------------------
                            {AddedCount}                  :  Value. Gets the number of added item entries available.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Added:#,prop}                :  Value Array. Gets value 'prop' from one-based added entry number '#'.
                                                        :    Values for 'prop': ids, name.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {AdditCount}                  :  Value. Gets the number of additional item entries available.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Addit:#,prop}                :  Value Array. Gets value 'prop' from one-based additional entry number '#'.
                                                        :    Values for 'prop': ids, optionalName, relatedContent (related content name),
                                                        :  relatedID.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {TTA}                         :  Value. Gets the number of total textures/contents added.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {UpdatedCount}                :  Value. Gets the number of updated item entries available.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Updated:#,prop}              :  Value Array. Gets value 'prop' from one-based updated entry number '#'.
                                                        :    Values for 'prop': changeDesc, id, relatedContent.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {LegendCount}                 :  Value. Gets the number of legend entries available.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Legend:#,prop}               :  Value Array. Gets value 'prop' from one-based legend entry number '#'.
                                                        :    Values for 'prop': definition, key
                        --------------------------------:------------------------------------------------------------------------------------------
                            {SummaryCount}                :  Value. Gets the number of summary parts available.
                        --------------------------------:------------------------------------------------------------------------------------------
                            {Summary:#}                   :  Value Array. Gets the value for one-based summary part number '#'.
                        --------------------------------:------------------------------------------------------------------------------------------


                        LIBRARY SYNTAX ERRORS
                        """""""""""""""""""""
                        Error code token 'R'

                        VERSION
                            - (no errors)

                        ADDED COUNT
                            - (no errors)

                        ADDED ARRAY
                            - [R024] Added entry number and property expected
                                . Occurs when the remainder of the Added syntax is missing.
                                    {Added}     --> Added entry number and property expected
                                    {Added:4,}  --> Added entry number and property expected
                            - [R025] Invalid Added entry number
                                . Occurs when the added entry number is neither '#' or a number greater than zero (>0).
                                    {Added:0,name}  --> Invalid Added entry number
                            - [R026] Invalid Added property
                                . Occurs when the value for 'prop' is not any of the following: ids, name.
                                    {Added:2,prop}  --> Invalid Added property

                        ADDIT COUNT
                            - (no errors)

                        ADDIT ARRAY
                            - [R027] Addit entry number and property expected
                                . Occurs when the remainder of the Addit syntax is missing.
                            - [R028] Invalid Addit entry number
                                . Occurs when the addit entry number is neither '#' or a number greater than zero (>0)
                                    {Addit:,ids}    --> Invalid Addit entry number
                            - [R029] Invalid Addit property
                                . Occurs when the value for 'prop' is not any of the following: ids, optionalName, relatedContent, relatedID
                                    {Addit:1,optName}   --> Invalid Addit property

                        TTA
                            - (no errors)
                        
                        UPDATED COUNT
                            - (no errors)

                        UPDATED ARRAY
                            - [R030] Updated entry number and property expected
                                . Occurs when the remainder of the Updated syntax is missing.
                            - [R031] Invalid Updated entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)
                                    {Updated:*,name}    --> Invalid Updated entry number
                            - [R032] Invalid Updated property
                                . Occurs when the value for 'prop' is not any of the following: changeDesc, id, relatedContent.
                                    {Updated:3,ids}     --> Invalid Updated property

                        LEGEND COUNT
                            - (no errors)

                        LEGEND ARRAY
                            - [R033] Legend entry number and property expected
                                . Occurs when the remainder of the Legend syntax is missing.
                            - [R034] Invalid Legend entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)
                                    {Legend:key,4}      --> Invalid Legend entry number
                            - [R035] Invalid Legend property
                                . Occurs when the value for 'prop' is not any of the following: definition, key
                                    {Legend:key,4}  --> Invalid Legend property

                        SUMMARY COUNT
                            - (no errors)

                        SUMMARY ARRAY
                            - [R036] Summary entry number expected
                                . Occurs when the remainder of the Summary syntax is missing.
                                    {Summary}   --> Summary entry number expected
                            - [R037] Invalid Summary entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)

                     */
                    if (!line.TrimStart().StartsWith("//"))
                    { /// section wrapping
                        string errorCode, errorMessage;

                        /** ADDED ARRAY errors
                            - [R024] Added entry number and property expected
                                . Occurs when the remainder of the Added syntax is missing.
                                    {Added}     --> Added entry number and property expected
                                    {Added:4,}  --> Added entry number and property expected
                            - [R025] Invalid Added entry number
                                . Occurs when the added entry number is neither '#' or a number greater than zero (>0).
                                    {Added:0,name}  --> Invalid Added entry number
                            - [R026] Invalid Added property
                                . Occurs when the value for 'prop' is not any of the following: ids, name.
                                    {Added:2,prop}  --> Invalid Added property                    *******/
                        for (int addx = 0; addx < 3; addx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            bool triggerR024q = false;

                            string[] noPlainParts = noPlainLine.LineBreak('{');
                            foreach (string noPP in noPlainParts)
                            {
                                if (noPP.SnippetText("{Added", "}", Snip.Inc, Snip.EndAft).IsNotNE())
                                {
                                    string addedArgs = noPP.SnippetText("{Added", "}", Snip.EndAft);
                                    if (!addedArgs.Equals("Count"))
                                    {
                                        /// IF ..: R024; ELSE R025 or R026
                                        if (addedArgs.IsNEW() && addx == 0)
                                            triggerR024q = true; /// Ex| {Added}  |  {Added  }
                                        else
                                        {
                                            addedArgs = noPP.SnippetText("{Added", "}", Snip.Inc, Snip.EndAft);
                                            string argEntNum = addedArgs.SnippetText(":", ","), argProp = addedArgs.SnippetText(",", "}");
                                            if (argEntNum.IsNotNEW())
                                            {
                                                bool validEntNum = int.TryParse(argEntNum, out int entNum);
                                                validEntNum = validEntNum && entNum > 0;

                                                if (!validEntNum && !argEntNum.SquishSpaces().Equals("#") && addx == 1)
                                                {
                                                    errorCode = "R025";
                                                    errorMessage = "Invalid Added entry number";
                                                }
                                            }

                                            if (argProp.IsNotNEW())
                                            {
                                                argProp = argProp.Trim();
                                                if (!argProp.Equals("name") && !argProp.Equals("ids") && addx == 2)
                                                {
                                                    errorCode = "R026";
                                                    errorMessage = "Invalid Added property";
                                                }
                                            }

                                            if ((argProp.IsNEW() || argEntNum.IsNEW()) && addx == 0)
                                                triggerR024q = true; /// Ex| {Added:4,} |  {Added: , }  |  {Added:,name}
                                        }
                                    }
                                }

                                if (triggerR024q)
                                {
                                    errorCode = "R024";
                                    errorMessage = "Added entry number and property expected";
                                }

                            }

                            if (errorCode.IsNotNEW())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** ADDIT ARRAY errors
                            - [R027] Addit entry number and property expected
                                . Occurs when the remainder of the Addit syntax is missing.
                            - [R028] Invalid Addit entry number
                                . Occurs when the addit entry number is neither '#' or a number greater than zero (>0)
                                    {Addit:,ids}    --> Invalid Addit entry number
                            - [R029] Invalid Addit property
                                . Occurs when the value for 'prop' is not any of the following: ids, optionalName, relatedContent, relatedID
                                    {Addit:1,optName}   --> Invalid Addit property                ********/
                        for (int adtx = 0; adtx < 3; adtx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            bool triggerR027q = false;

                            string[] noPlainParts = noPlainLine.LineBreak('{');
                            foreach (string noPP in noPlainParts)
                            {
                                if (noPP.SnippetText("{Addit", "}", Snip.Inc, Snip.EndAft).IsNotNE())
                                {
                                    string additArgs = noPP.SnippetText("{Addit", "}", Snip.EndAft);
                                    if (!additArgs.Equals("Count"))
                                    {
                                        /// IF ..: R027; ELSE R028 or R029
                                        if (additArgs.IsNEW() && adtx == 0)
                                            triggerR027q = true; /// Ex| {Addit}  |  {Addit  }
                                        else
                                        {
                                            additArgs = noPP.SnippetText("{Addit", "}", Snip.Inc, Snip.EndAft);
                                            string argEntNum = additArgs.SnippetText(":", ","), argProp = additArgs.SnippetText(",", "}");
                                            if (argEntNum.IsNotNEW())
                                            {
                                                bool validEntNum = int.TryParse(argEntNum, out int entNum);
                                                validEntNum = validEntNum && entNum > 0;

                                                if (!validEntNum && !argEntNum.SquishSpaces().Equals("#") && adtx == 1)
                                                {
                                                    errorCode = "R028";
                                                    errorMessage = "Invalid Addit entry number";
                                                }
                                            }

                                            if (argProp.IsNotNEW())
                                            {
                                                argProp = argProp.Trim();
                                                if (!argProp.Equals("optionalName") && !argProp.Equals("ids") && !argProp.Equals("relatedContent") && !argProp.Equals("relatedID") && adtx == 2)
                                                {
                                                    errorCode = "R029";
                                                    errorMessage = "Invalid Addit property";
                                                }
                                            }

                                            if ((argProp.IsNEW() || argEntNum.IsNEW()) && adtx == 0)
                                                triggerR027q = true; /// Ex| {Addit:4,} |  {Addit: , }  |  {Addit:,name}
                                        }
                                    }
                                }

                                if (triggerR027q)
                                {
                                    errorCode = "R027";
                                    errorMessage = "Addit entry number and property expected";
                                }
                            }

                            if (errorCode.IsNotNEW())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** UPDATED ARRAY errors
                            - [R030] Updated entry number and property expected
                                . Occurs when the remainder of the Updated syntax is missing.
                            - [R031] Invalid Updated entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)
                                    {Updated:*,name}    --> Invalid Updated entry number
                            - [R032] Invalid Updated property
                                . Occurs when the value for 'prop' is not any of the following: changeDesc, id, relatedContent.
                                    {Updated:3,ids}     --> Invalid Updated property                ********/
                        for (int updx = 0; updx < 3; updx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            bool triggerR030q = false;

                            string[] noPlainParts = noPlainLine.LineBreak('{');
                            foreach (string noPP in noPlainParts)
                            {
                                if (noPP.SnippetText("{Updated", "}", Snip.Inc, Snip.EndAft).IsNotNE())
                                {
                                    string updatedArgs = noPP.SnippetText("{Updated", "}", Snip.EndAft);
                                    if (!updatedArgs.Equals("Count"))
                                    {
                                        /// IF ..: R030; ELSE R031 or R032
                                        if (updatedArgs.IsNEW() && updx == 0)
                                            triggerR030q = true; /// Ex| {Updated}  |  {Updated  }
                                        else
                                        {
                                            updatedArgs = noPP.SnippetText("{Updated", "}", Snip.Inc, Snip.EndAft);
                                            string argEntNum = updatedArgs.SnippetText(":", ","), argProp = updatedArgs.SnippetText(",", "}");
                                            if (argEntNum.IsNotNEW())
                                            {
                                                bool validEntNum = int.TryParse(argEntNum, out int entNum);
                                                validEntNum = validEntNum && entNum > 0;

                                                if (!validEntNum && !argEntNum.SquishSpaces().Equals("#") && updx == 1)
                                                {
                                                    errorCode = "R031";
                                                    errorMessage = "Invalid Updated entry number";
                                                }
                                            }

                                            if (argProp.IsNotNEW())
                                            {
                                                argProp = argProp.Trim();
                                                if (!argProp.Equals("id") && !argProp.Equals("relatedContent") && !argProp.Equals("changeDesc") && updx == 2)
                                                {
                                                    errorCode = "R032";
                                                    errorMessage = "Invalid Updated property";
                                                }
                                            }

                                            if ((argProp.IsNEW() || argEntNum.IsNEW()) && updx == 0)
                                                triggerR030q = true; /// Ex| {Updataed:4,} |  {Updated: , }  |  {Updated:,name}
                                        }
                                    }
                                }

                                if (triggerR030q)
                                {
                                    errorCode = "R030";
                                    errorMessage = "Updated entry number and property expected";
                                }
                            }

                            if (errorCode.IsNotNEW())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** LEGEND ARRAY errors
                            - [R033] Legend entry number and property expected
                                . Occurs when the remainder of the Legend syntax is missing.
                            - [R034] Invalid Legend entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)
                                    {Legend:key,4}      --> Invalid Legend entry number
                            - [R035] Invalid Legend property
                                . Occurs when the value for 'prop' is not any of the following: definition, key
                                    {Legend:key,4}  --> Invalid Legend property                ********/
                        for (int legx = 0; legx < 3; legx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            bool triggerR033q = false;

                            string[] noPlainParts = noPlainLine.LineBreak('{');
                            foreach (string noPP in noPlainParts)
                            {
                                if (noPP.SnippetText("{Legend", "}", Snip.Inc, Snip.EndAft).IsNotNE())
                                {
                                    string legendArgs = noPP.SnippetText("{Legend", "}", Snip.EndAft);
                                    if (!legendArgs.Equals("Count"))
                                    {
                                        /// IF ..: R033; ELSE R034 or R035
                                        if (legendArgs.IsNEW() && legx == 0)
                                            triggerR033q = true; /// Ex| {Legend}  |  {Legend  }
                                        else
                                        {
                                            legendArgs = noPP.SnippetText("{Legend", "}", Snip.Inc, Snip.EndAft);
                                            string argEntNum = legendArgs.SnippetText(":", ","), argProp = legendArgs.SnippetText(",", "}");
                                            if (argEntNum.IsNotNEW())
                                            {
                                                bool validEntNum = int.TryParse(argEntNum, out int entNum);
                                                validEntNum = validEntNum && entNum > 0;

                                                if (!validEntNum && !argEntNum.SquishSpaces().Equals("#") && legx == 1)
                                                {
                                                    errorCode = "R034";
                                                    errorMessage = "Invalid Legend entry number";
                                                }
                                            }

                                            if (argProp.IsNotNEW())
                                            {
                                                argProp = argProp.Trim();
                                                if (!argProp.Equals("key") && !argProp.Equals("definition") && legx == 2)
                                                {
                                                    errorCode = "R035";
                                                    errorMessage = "Invalid Legend property";
                                                }
                                            }

                                            if ((argProp.IsNEW() || argEntNum.IsNEW()) && legx == 0)
                                                triggerR033q = true; /// Ex| {Legend:4,} |  {Legend: , }  |  {Legend:,name}
                                        }
                                    }
                                }

                                if (triggerR033q)
                                {
                                    errorCode = "R033";
                                    errorMessage = "Legend entry number and property expected";
                                }
                            }

                            if (errorCode.IsNotNEW())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** SUMMARY ARRAY errors
                            - [R036] Summary entry number expected
                                . Occurs when the remainder of the Summary syntax is missing.
                                    {Summary}   --> Summary entry number expected
                            - [R037] Invalid Summary entry number
                                . Occurs when the entry number is neither '#' or a number greater than zero (>0)     ********/
                        for (int sumx = 0; sumx < 2; sumx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            bool triggerR036q = false;

                            string[] noPlainParts = noPlainLine.LineBreak('{');
                            foreach (string noPP in noPlainParts)
                            {
                                if (noPP.SnippetText("{Summary", "}", Snip.Inc, Snip.EndAft).IsNotNE())
                                {
                                    string summaryArgs = noPP.SnippetText("{Summary", "}", Snip.EndAft);
                                    if (!summaryArgs.Equals("Count"))
                                    {
                                        /// IF ..: R036; ELSE R037
                                        if (summaryArgs.IsNEW() && sumx == 0)
                                            triggerR036q = true; /// Ex| {Summary}  |  {Summary  }                            
                                        else if (summaryArgs.Contains(":"))
                                        {
                                            summaryArgs = summaryArgs.Substring(1);
                                            bool validEntNum = int.TryParse(summaryArgs, out int entNum);
                                            validEntNum = validEntNum && entNum > 0;
                                            if (!validEntNum && !summaryArgs.SquishSpaces().Equals("#") && sumx == 1)
                                            {
                                                errorCode = "R037";
                                                errorMessage = "Invalid Summary entry number";
                                            }

                                            if (summaryArgs.IsNEW() && sumx == 0)
                                                triggerR036q = true; /// Ex| {Summary:}
                                        }
                                    }
                                }

                                if (triggerR036q)
                                {
                                    errorCode = "R036";
                                    errorMessage = "Summary entry number expected";
                                }
                            }

                            if (errorCode.IsNotNEW())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                    }



                    // ~~  STEAM FORMAT REFERENCE SYNTAX  ~~
                    /** STEAM FORMAT REFERENCE SYNTAX - revised and errors debrief
                    # S T E A M   F O R M A T   R E F E R E N C E S
                    `    Steam format references are styling element calls that will affect the look of any text or value placed after it on
                    ▌    log generation.
                    ▌    Simple command references may be combined with other simple commands unless otherwise unpermitted. Simple commands
                    ▌    affect only one value that follows them.
                    ▌    Complex commands require a text or value to be placed in a described parameter surrounded by single quote characters
                    ▌    (').

                    SYNTAX                          :  OUTCOME
                    --------------------------------:------------------------------------------------------------------------------------------
                        $h                            :  Simple command. Header text. Must be placed at the start of the line. May not be
                                                    :  combined with other simple commands.
                                                    :    There are three levels of header text. The header level follows the number of 'h's in
                                                    :  reference. Example, a level three header text is '$hhh'.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $b                            :  Simple command. Bold text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $u                            :  Simple command. Underlined text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $i                            :  Simple command. Italicized text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $s                            :  Simple command. Strikethrough text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $sp                           :  Simple command. Spoiler text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $np                           :  Simple command. No parse. Doesn't parse steam format tags when generating steam log.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $c                            :  Simple command. Code text. Fixed width font, preserves space.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $hr                           :  Simple command. Horizontal rule. Must be placed on its own line. May not be combined
                                                    :  with other simple commands.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $nl                           :  Simple command. New line.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $d                            :  Simple command. Indent.
                                                    :    There are four indentation levels which relates to the number of 'd's in reference.
                                                    :  Example, a level 2 indent is '$dd'.
                                                    :    An indentation is the equivalent of two spaces (' 'x2).
                    --------------------------------:------------------------------------------------------------------------------------------
                        $r                            :  Simple command. Regular. Used to forcefully demark the end of preceding simple commands.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $url='link':'name'            :  Complex command. Must be placed on its own line.
                                                    :    Creates a website link by using URL address 'link' to create a hyperlink text
                                                    :  described as 'name'.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $list[or]                     :  Complex command. Must be placed on its own line.
                                                    :    Starts a list block. The optional parameter within square brackets, 'or', will
                                                    :  initiate an ordered (numbered) list. Otherwise, an unordered list is initiated.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $*                            :  Simple command. Must be placed on its own line.
                                                    :    Used within a list block to create a list item. Simple commands may follow to style
                                                    :  the list item value or text.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $q='author':'quote'           :  Complex command. Must be placed on its own line.
                                                    :    Generates a quote block that will reference an 'author' and display their original
                                                    :  text 'quote'.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $table[nb,ec]                 :  Complex command. Must be placed on its own line.
                                                    :    Starts a table block. There are two optional parameters within square brackets:
                                                    :  parameter 'nb' will generate a table with no borders, parameter 'ec' will generate a
                                                    :  table with equal cells.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $th='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                    :    Used within a table block to create a table header row. Separate multiple columns of
                                                    :  data with ','. Must follow immediately after a table block has started.
                    --------------------------------:------------------------------------------------------------------------------------------
                        $td='clm1','clm2'             :  Complex command. Must be placed on its own line.
                                                    :    Used within a table block to create a table data row. Separate multiple columns of
                                                    :  data with ','.
                    --------------------------------:------------------------------------------------------------------------------------------
                     

                    STEAM FORMAT SYNTAX ERRORS
                    """"""""""""""""""""""""""
                    Error code token 'R'

                    SIMPLE COMMANDS
                        - [R038] Missing value to format
                            . Occurs when a single or combination of simple command references is empty
                                $i      --> Missing value to format
                                $sp     --> Missing value to format
                        - [R039] Invalid value to format: '{val}'
                            . Occurs when a value following a single or combination of simple command references is neither plain text nor library reference
                                $s w        --> Invalid value to format: 'w'
                                $h {CTA}    --> Invalid value to format: '{CTA}'

                        HEADING
                            - [R040] Heading element expected at beginning
                                . Occurs when the heading element is not placed at the beginning of the line
                                    else; $h "Water"    --> Heading element expected at beginning
                            - [R041] Heading element cannot be combined with other commands
                                . Occurs when the heading element is followed by other simple commands
                                    $h $i "Waterleaf"   --> Heading element cannot be combined with other commands 

                        BOLD
                            - (no errors)
                    
                        UNDERLINE
                            - (no errors)
                    
                        ITALICIZE
                            - (no errors)
                    
                        STRIKETHROUGH
                            - (no errors)
                    
                        SPOILER
                            - (no errors)
                    
                        NO PARSE
                            - (no errors)
                    
                        CODE TEXT
                            - (no errors)
                    
                        HORIZONTAL RULE
                            Note :: R038 and R039 does not apply to this element
                            - [R042] Horizontal Rule element expected to be on its own line
                                . Occurs when the horizontal rule element is not the only item on its line
                                    $hr "Slap"  --> Horizontal Rule element expected to be on its own line
                                    $hr $nl     --> Horizontal Reul element expected to be on its own line
                    
                        NEWLINE
                            Note :: R038 and R039 does not apply to this element
                            - (no errors)

                        INDENT
                            Note :: R038 and R039 does not apply to this element
                            - (no errors)

                        REGULAR
                            - (no errors)

                        LIST ITEM
                            - [R043] List Item element expected at beginning
                                . Occurs when the list item element is not placed at the beginning of the line
                            - [R044] List Item element is not within a list block
                                . Occurs when the list item element is not preceded by another list element line or a list block line
                                    $h "Boom"
                                    $* "Boom"    --> List Item element is not within a list block
                    
                    URL
                        - [R045] URL element expected to be on its own line 
                            . Occurs when a url element is not the only steam format reference on its line
                                $url="www.Bt.ca":"Boot" $i "here"   -->  URL element expected to be on its own line 
                        - [R046] URL element assignment operator expected
                            . Occurs when a url element is not followed by the '=' operator
                                $url    --> URL element assignment operator expected
                        - [R047] Empty URL element
                            . Occurs when a url element does not have any values following after the operator
                                $url=   --> Empty URL element
                        - [R048] URL value for link expected
                            . Occurs when the value for 'link' is missing or invalid.
                                $url=wtq        --> URL value for link expected
                                $url= : "Bun"   --> URL value for link expected
                        - [R049] URL value for name expected
                            . Occurs when the value for 'name' is missing or invalid. 
                                $url= "n"           --> URL value for name expected
                                $url = "smak" : w   --> URL value for name expected
                    
                    LIST
                        - [R050] List element expected to be on its own line
                            . Occurs when a list element is not the only item on its line
                        - [R051] List element parameter brackets expected
                            . Occurs when the list element is missing its parameter brackets
                                $list       --> List element parameter brackets expected
                                $list[      --> List element parameter brackets expected
                        - [R052] Invalid List element parameter '{param}'
                            . Occurs when the value within the parameter brackets is not 'or'
                                $list[o]    --> Invalid List element parameter 'o'
                        - [R053] List element must contain at least one list item
                            . Occurs when the list element line is not followed by a list item line
                                $list[]     
                                "Next"      --> List element must contain at least one list item
                    
                    QUOTE
                        - [R054] Quote element expected to be on its own line
                            . Occurs when a quote element is not the only steam format reference on its line
                                $q= "McNugger" : "Nothing's better than a nugger burger" $hr    --> Quote element expected to be on its own line
                        - [R055] Quote element assignment operator expected
                            . Occurs when a quote element is not followed by the '=' operator
                        - [R056] Empty Quote element 
                            . Occurs when a quote element does not have any values following after the operator
                        - [R057] Quote value for author expected
                            . Occurs when the value for 'auther' is missing or invalid
                        - [R058] Quote value for qoute expected
                            . Occurs when the value for 'quote' is missing or invalid
                    
                    TABLE
                        - [R059] Table element expected to be on its own line
                            . Occurs when a table element is not the only item on its line
                                $table[] $i "Stap it!"  --> Table element expected to be on its own line
                        - [R060] Table element parameter brackets expected
                            . Occurs when the table element is missing its parameter brackets
                        - [R061] Invalid Table element parameter '{param}'
                            . Occurs when the value within the parameter brackets does not 'nb' or 'ec'
                        - [R062] Table element only accepts two or less parameters
                            . Occurs when multiple values withn the parameter brackets surpass a total of two
                        - [R063] Table element must contain at least one table row
                            . Occurs when the table element line is not followed by a table header or table data line
                    
                    TABLE HEADER
                        - [R064] Table Header element expected to be on its own line
                            . Occurs when a table header element is not the only steam format reference on its line
                        - [R065] Table Header element assignment operator expected
                            . Occurs when a table header element is not followed by the '=' operator
                        - [R066] Table Header element expected after table block line
                            . Occurs when a table header element line is not preceded by a table block line
                        - [R067] Table Header element is not within a table block
                            . Occurs when a table header element line is not preceded by a table block line or table data line
                        - [R068] Empty Table Header element
                            . Occurs when a table header element does not have any values following after the operator
                        - [R069] Table Header value '{val}' is an invalid value
                            . Occurs when a table header column value is an invalid value
                                $th= www, tyi   --> Table Header value 'www' is an invalid value
                    
                    TABLE DATA
                        - [R070] Table Data element expected to be on its own line
                            . Occurs when a table data element is not the only steam format reference on its line
                        - [R071] Table Data element assignment operator expected
                            . Occurs when a table data element is not followed by the '=' operator
                        - [R072] Table Data element is not within a table block
                            . Occurs when a table data element line is not preceded by a table block line or a table header line
                        - [R073] Empty Table Data element
                            . Occurs when a table data element does not have any values following after the operator
                        - [R074] Table Data element does not match column count of preceding rows
                            . Occurs when a table data element's column count mismatches that of a preceding table data or table header line
                        - [R075] Table Data value '{val}' is an invalid value
                            . Occurs when a table data column value is an invalid value
                                $td= "Sweet", sp    --> Table Data value 'sp' is an invalid value

                     **/
                    if (!line.TrimStart().StartsWith("//"))
                    { /// section wrapping
                        //const string tokenTag = "[token]";
                        string errorCode, errorMessage;
                        string token;

                        /** SIMPLE COMMANDS errors
                        SIMPLE COMMANDS
                        - [R038] Missing value to format
                            . Occurs when a single or combination of simple command references is empty
                                $i      --> Missing value to format
                                $sp     --> Missing value to format
                        - [R039] Invalid value to format: '{val}'
                            . Occurs when a value following a single or combination of simple command references is neither plain text nor library reference
                                $s w        --> Invalid value to format: 'w'
                                $h {CTA}    --> Invalid value to format: '{CTA}'

                            HEADING
                                - [R040] Heading element expected at beginning
                                    . Occurs when the heading element is not placed at the beginning of the line
                                        else; $h "Water"    --> Heading element expected at beginning
                                - [R041] Heading element cannot be combined with other commands
                                    . Occurs when the heading element is followed by other simple commands
                                        $h $i "Waterleaf"   --> Heading element cannot be combined with other commands 

                            BOLD
                                - (no errors)
                    
                            UNDERLINE
                                - (no errors)
                    
                            ITALICIZE
                                - (no errors)
                    
                            STRIKETHROUGH
                                - (no errors)
                    
                            SPOILER
                                - (no errors)
                    
                            NO PARSE
                                - (no errors)
                    
                            CODE TEXT
                                - (no errors)
                    
                            HORIZONTAL RULE
                                Note :: R038 and R039 does not apply to this element
                                - [R042] Horizontal Rule element expected to be on its own line
                                    . Occurs when the horizontal rule element is not the only item on its line
                                        $hr "Slap"  --> Horizontal Rule element expected to be on its own line
                                        $hr $nl     --> Horizontal Reul element expected to be on its own line
                    
                            NEWLINE
                                Note :: R038 and R039 does not apply to this element
                                - (no errors)

                            LIST ITEM
                                - [R043] List Item element expected at beginning
                                    . Occurs when the list item element is not placed at the beginning of the line
                                - [R044] List Item element is not within a list block 
                                    . Occurs when the list item element is not preceded by another list element line or a list block line 
                                        $h "Boom"
                                        $* "Boom"    --> List Item element is not within a list block
                         */
                        for (int scx = 0; scx < 4; scx++)
                        {
                            switch (scx)
                            {
                                /** General Simple Command errors
                                - [R038] Missing value to format
                                    . Occurs when a single or combination of simple command references are
                                        $i      --> Missing value to format
                                        $sp     --> Missing value to format
                                - [R039] Invalid value to format: '{val}'
                                    . Occurs when a value following a single or combination of simple command references is neither plain text nor library reference
                                        $s w        --> Invalid value to format: 'w'
                                        [x] $h {CTA}    --> Invalid value to format: '{CTA}'

                                    HORIZONTAL RULE -- Note :: R038 and R039 does not apply to this element
                                    NEWLINE -- Note :: R038 and R039 does not apply to this element                         ******/
                                case 0:
                                    for (int gscx = 0; gscx < 2; gscx++)
                                    {
                                        errorCode = null;
                                        errorMessage = null;

                                        if (noPlainLine.Contains('$'))
                                        {
                                            string[] theRefs = line.RemoveFromPlainText('$').LineBreak('$');
                                            for (int trx = 0; trx < theRefs.Length; trx++)
                                            {
                                                string theRefPart = theRefs[trx];
                                                if (!theRefPart.StartsWith("$hr") && !theRefPart.StartsWith("$nl") && !theRefPart.StartsWith("$d") && !theRefPart.Contains("[") && !theRefPart.Contains("="))
                                                {
                                                    bool triggerR038q = false;
                                                    string snipRef = $"{theRefPart.RemoveFromPlainText(' ').SnippetText("$", " ", Snip.Inc, Snip.EndAft)}";
                                                    if (snipRef.IsNotNE())
                                                    {
                                                        if (snipRef.Length < theRefPart.Length)
                                                        {
                                                            string fValue = theRefPart.Substring(snipRef.Length);
                                                            if (fValue.IsNotNEW())
                                                            {
                                                                if (!IsValidSFValue(fValue) && gscx == 1)
                                                                {
                                                                    errorCode = "R039";
                                                                    errorMessage = $"Invalid value to format: '{fValue}'";
                                                                }
                                                            }
                                                            /// not triggering
                                                            //else if (gscx == 0)
                                                            //{
                                                            //    triggerR038q = true;
                                                            //    snipRef += ID(2);
                                                            //}
                                                        }
                                                        else if (trx + 1 == theRefs.Length && gscx == 0)
                                                        {
                                                            triggerR038q = true;
                                                            snipRef += ID(0);
                                                        }
                                                    }
                                                    else if (theRefPart.Contains("$") && gscx == 0)
                                                    {
                                                        triggerR038q = true;
                                                        snipRef = theRefPart + ID(1);
                                                    }

                                                    if (triggerR038q)
                                                    {
                                                        errorCode = "R038";
                                                        errorMessage = "Missing value to format" + (ID(0).IsNE() ? "" : $" (after '{snipRef.Trim()}')");
                                                    }
                                                }
                                            }
                                        }                                        

                                        if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;

                                /** HEADING errors
                                - [R040] Heading element expected at beginning
                                    . Occurs when the heading element is not placed at the beginning of the line
                                        else; $h "Water"    --> Heading element expected at beginning
                                - [R041] Heading element cannot be combined with other commands
                                    . Occurs when the heading element is followed by other simple commands
                                        $h $i "Waterleaf"   --> Heading element cannot be combined with other commands          ******/
                                case 1:
                                    for (int hx = 0; hx < 2; hx++)
                                    {
                                        errorCode = null;
                                        errorMessage = null;

                                        if (noPlainLine.Contains('$'))
                                        {
                                            if (noPlainLine.Contains("$h"))
                                            {
                                                string theHeaderRef = noPlainLine.SnippetText("$h", " ", Snip.Inc);
                                                if (theHeaderRef.IsNE())
                                                    theHeaderRef = noPlainLine.Replace("$hr", "").SnippetText("$", "h", Snip.Inc, Snip.EndLast);

                                                if (theHeaderRef.IsNotNE())
                                                {
                                                    theHeaderRef = theHeaderRef.Trim();
                                                    if (theHeaderRef.Equals("$h") || theHeaderRef.Equals("$hh") || theHeaderRef.Equals("$hhh"))
                                                    {
                                                        if (!line.StartsWith(theHeaderRef) && hx == 0)
                                                        {
                                                            errorCode = "R040";
                                                            errorMessage = "Heading element expected at beginning";
                                                        }

                                                        if (noPlainLine.CountOccuringCharacter('$') > 1 && hx == 1)
                                                        {
                                                            errorCode = "R041";
                                                            errorMessage = "Heading element cannot be combined with other commands";
                                                        }
                                                    }
                                                }            
                                            }
                                        }

                                        if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;

                                /** HORIZONTAL RULE errors
                                Note :: R038 does not apply to this element
                                - [R042] Horizontal Rule element expected to be on its own line
                                    . Occurs when the horizontal rule element is not the only item on its line
                                        $hr "Slap"  --> Horizontal Rule element expected to be on its own line
                                        $hr $nl     --> Horizontal Reul element expected to be on its own line               ******/
                                case 2:
                                    if (noPlainLine.Contains("$hr"))
                                    {
                                        if (!line.TrimStart().Equals("$hr"))
                                        {
                                            errorCode = "R042";
                                            errorMessage = "Horizontal Rule element expected to be on its own line";

                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                        }
                                    }
                                    break;

                                /** LIST ITEM errors
                                - [R043] List Item element expected at beginning
                                    . Occurs when the list item element is not placed at the beginning of the line
                                - [R044] List Item element is not within a list block
                                    . Occurs when the list item element is not preceded by another list element line or a list block line
                                        $h "Boom"
                                        $* "Boom"    --> List Item element is not within a list block                    ******/
                                case 3:
                                    for (int lix = 0; lix < 2; lix++)
                                    {
                                        errorCode = null;
                                        errorMessage = null;

                                        if (noPlainLine.Contains("$*"))
                                        {
                                            if (!line.TrimStart().StartsWith("$*") && lix == 0)
                                            {
                                                errorCode = "R043";
                                                errorMessage = "List Item element expected at beginning";
                                            }

                                            if (lix == 1)
                                            {
                                                bool triggerR044q = false;
                                                if (!prevLine.StartsWith("$*") && !prevLine.StartsWith("$list"))
                                                {
                                                    triggerR044q = true;
                                                    if (prevLine.RemovePlainText().Contains("next"))
                                                    {
                                                        if (prevLine.TrimEnd().EndsWith(prevLine.RemovePlainText().SnippetText("next", ";", Snip.Inc)))
                                                            triggerR044q = !prev2ndLine.StartsWith("$*") && !prev2ndLine.StartsWith("$list");
                                                    }
                                                }

                                                if (triggerR044q)
                                                {
                                                    errorCode = "R044";
                                                    errorMessage = "List Item element is not within a list block";
                                                }
                                            }                                            
                                        }

                                        if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;
                            }
                        }

                        /** URL errors
                        - [R045] URL element expected to be on its own line 
                            . Occurs when a url element is not the only steam format reference on its line (or is not at beginning)
                                $url="www.Bt.ca":"Boot" $i "here"   -->  URL element expected to be on its own line 
                        - [R046] URL element assignment operator expected
                            . Occurs when a url element is not followed by the '=' operator
                                $url    --> URL element assignment operator expected
                        - [R047] Empty URL element
                            . Occurs when a url element does not have any values following after the operator
                                $url=   --> Empty URL element
                        - [R048] URL value for link expected
                            . Occurs when the value for 'link' is missing or invalid.
                                $url=wtq        --> URL value for link expected
                                $url= : "Bun"   --> URL value for link expected
                        - [R049] URL value for name expected
                            . Occurs when the value for 'name' is missing or invalid. 
                                $url= "n"           --> URL value for name expected
                                $url = "smak" : w   --> URL value for name expected          ********/
                        for (int ulx = 0; ulx < 5; ulx++)
                        {
                            errorCode = null;
                            errorMessage = null;

                            if (noPlainLine.Contains("$url"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$url"))
                                {
                                    if (noPlainLine.CountOccuringCharacter('=') == 1 && line.StartsWith("$url="))
                                    {
                                        string urlArgs = line.Substring("$url=".Length);
                                        if (urlArgs.IsNotNEW())
                                        {
                                            bool triggerR048q = false;
                                            if (urlArgs.Contains(":"))
                                            {
                                                string[] urlParts = urlArgs.RemoveFromPlainText(':').Split(':');
                                                if (urlParts.Length == 2)
                                                {
                                                    if (!IsValidSFValue(urlParts[0]))
                                                        triggerR048q = true;
                                                    else if (!IsValidSFValue(urlParts[1]) && ulx == 4)
                                                    {
                                                        errorCode = "R049";
                                                        errorMessage = "URL value for name expected";
                                                    }
                                                }
                                                else if (ulx == 3)
                                                    unexpectedTokenII.Add(":" + ID(48));
                                            }
                                            else triggerR048q = true;

                                            if (triggerR048q && ulx == 3)
                                            {
                                                errorCode = "R048";
                                                errorMessage = "URL value for link expected";
                                            }
                                        }
                                        else if (ulx == 2)
                                        {
                                            errorCode = "R047";
                                            errorMessage = "Empty URL element";
                                        }
                                    }
                                    else if (ulx == 1)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('=') > 1)
                                            unexpectedTokenII.Add("=" + ID(46));
                                        else
                                        {
                                            errorCode = "R046";
                                            errorMessage = "URL element assignment operator expected";
                                        }
                                    }
                                }
                                else if (ulx == 0)
                                {
                                    errorCode = "R045";
                                    errorMessage = "URL element expected to be on its own line";
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** LIST errors
                        - [R050] List element expected to be on its own line
                            . Occurs when a list element is not the only item on its line
                        - [R051] List element parameter brackets expected
                            . Occurs when the list element is missing its parameter brackets
                                $list       --> List element parameter brackets expected
                                $list[      --> List element parameter brackets expected
                        - [R052] Invalid List element parameter '{param}'
                            . Occurs when the value within the parameter brackets is not 'or'
                                $list[o]    --> Invalid List element parameter 'o'
                        - [R053] List element must contain at least one list item
                            . Occurs when the list element line is not followed by a list item line
                                $list[]     
                                "Next"      --> List element must contain at least one list item   *******/
                        for (int ltx = 0; ltx < 4; ltx++)
                        {
                            errorCode = null;
                            errorMessage = null;

                            if (noPlainLine.Contains("$list"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$list"))
                                {
                                    string snipParams = line.SnippetText("[", "]", Snip.Inc);
                                    if (snipParams.IsNotNEW() && line.StartsWith("$list["))
                                    {
                                        if (!snipParams.Equals("[]") && !snipParams.Equals("[or]") && ltx == 2)
                                        {
                                            errorCode = "R052";
                                            errorMessage = $"Invalid List element parameter '{snipParams.SnippetText("[", "]")}'";
                                        }
                                    }
                                    else if (ltx == 1)
                                    {
                                        errorCode = "R051";
                                        errorMessage = "List element parameter brackets expected";
                                    }
                                }
                                else if (ltx == 0)
                                {
                                    errorCode = "R050";
                                    errorMessage = "List element expected to be on its own line";
                                }


                                if (line.StartsWith("$list") && ltx == 3)
                                {
                                    bool triggerR053q = nextLine.IsNEW();
                                    if (!nextLine.StartsWith("$*"))
                                    {
                                        triggerR053q = true;
                                        if (nextLine.RemovePlainText().Contains("next"))
                                            if (nextLine.TrimEnd().EndsWith(nextLine.RemovePlainText().SnippetText("next", ";", Snip.Inc)))
                                                triggerR053q = !next2ndLine.StartsWith("$*");
                                    }

                                    if (triggerR053q)
                                    {
                                        errorCode = "R053";
                                        errorMessage = "List element must contain at least one list item";
                                    }
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** QUOTE errors
                        - [R054] Quote element expected to be on its own line
                            . Occurs when a quote element is not the only steam format reference on its line (or is not at beginning)
                                $q= "McNugger" : "Nothing's better than a nugger burger" $hr    --> Quote element expected to be on its own line
                        - [R055] Quote element assignment operator expected
                            . Occurs when a quote element is not followed by the '=' operator
                        - [R056] Empty Quote element 
                            . Occurs when a quote element does not have any values following after the operator
                        - [R057] Quote value for author expected
                            . Occurs when the value for 'auther' is missing or invalid
                        - [R058] Quote value for qoute expected
                            . Occurs when the value for 'quote' is missing or invalid           ******/
                        for (int qtx = 0; qtx < 5; qtx++)
                        {
                            errorCode = null;
                            errorMessage = null;

                            if (noPlainLine.Contains("$q"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$q"))
                                {
                                    if (noPlainLine.CountOccuringCharacter('=') == 1 && line.StartsWith("$q="))
                                    {
                                        string quoteArgs = line.Substring("$q=".Length);
                                        if (quoteArgs.IsNotNEW())
                                        {
                                            bool triggerR057q = false;
                                            if (quoteArgs.Contains(":"))
                                            {
                                                string[] quoteParts = quoteArgs.RemoveFromPlainText(':').Split(':');
                                                if (quoteParts.Length == 2)
                                                {
                                                    if (!IsValidSFValue(quoteParts[0]))
                                                        triggerR057q = true;
                                                    else if (!IsValidSFValue(quoteParts[1]) && qtx == 4)
                                                    {
                                                        errorCode = "R058";
                                                        errorMessage = "Quote value for quote expected";
                                                    }
                                                }
                                                else if (qtx == 3)
                                                    unexpectedTokenII.Add(":" + ID(57));
                                            }
                                            else triggerR057q = true;

                                            if (triggerR057q && qtx == 3)
                                            {
                                                errorCode = "R057";
                                                errorMessage = "Quote value for author expected";
                                            }
                                        }
                                        else if (qtx == 2)
                                        {
                                            errorCode = "R056";
                                            errorMessage = "Empty Quote element";
                                        }
                                    }
                                    else if (qtx == 1)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('=') > 1)
                                            unexpectedTokenII.Add("=" + ID(55));
                                        else
                                        {
                                            errorCode = "R055";
                                            errorMessage = "Quote element assignment operator expected";
                                        }
                                    }
                                }
                                else if (qtx == 0)
                                {
                                    errorCode = "R054";
                                    errorMessage = "Quote element expected to be on its own line";
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** TABLE errors
                        - [R059] Table element expected to be on its own line
                            . Occurs when a table element is not the only item on its line
                                $table[] $i "Stap it!"  --> Table element expected to be on its own line
                        - [R060] Table element parameter brackets expected
                            . Occurs when the table element is missing its parameter brackets
                        - [R061] Invalid Table element parameter '{param}'
                            . Occurs when the value within the parameter brackets does not 'nb' or 'ec'
                        - [R062] Table element only accepts two or less parameters
                            . Occurs when multiple values withn the parameter brackets surpass a total of two
                        - [R063] Table element must contain at least one table row
                            . Occurs when the table element line is not followed by a table header or table data line        *****/
                        for (int tbx = 0; tbx < 5; tbx++)
                        {
                            errorCode = null;
                            errorMessage = null;
                            token = null;

                            if (noPlainLine.Contains("$table"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$table"))
                                {
                                    string snipParams = line.SnippetText("[", "]", Snip.Inc);
                                    if (snipParams.IsNotNEW() && line.StartsWith("$table["))
                                    {
                                        string snipParamArgs = line.SnippetText("[", "]");
                                        if (snipParamArgs.IsNotNEW())
                                        {
                                            bool triggerR061q = false;
                                            if (snipParamArgs.Contains(","))
                                            {
                                                if (snipParamArgs.CountOccuringCharacter(',') == 1)
                                                {
                                                    string[] pArgs = snipParamArgs.Split(',');
                                                    if (pArgs[0].IsNEW() || pArgs[1].IsNEW())
                                                        unexpectedTokenII.Add("," + ID(62));
                                                    if (!pArgs[0].Equals("nb") && !pArgs[0].Equals("ec"))
                                                    {
                                                        triggerR061q = true;
                                                        token = pArgs[0];
                                                    }
                                                    else if (!pArgs[1].Equals("nb") && !pArgs[1].Equals("ec"))
                                                    {
                                                        triggerR061q = true;
                                                        token = pArgs[1];
                                                    }
                                                }
                                                else if (tbx == 3)
                                                {
                                                    errorCode = "R062";
                                                    errorMessage = "Table element only accepts two or less parameters";
                                                }
                                            }
                                            else
                                            {
                                                if (!snipParamArgs.Equals("nb") && !snipParamArgs.Equals("ec"))
                                                {
                                                    triggerR061q = true;
                                                    token = snipParamArgs;
                                                }    
                                            }

                                            if (triggerR061q && token.IsNotNE() && tbx == 2)
                                            {
                                                errorCode = "R061";
                                                errorMessage = $"Invalid Table element parameter '{token}'";
                                            }
                                        }

                                    }
                                    else if (tbx == 1)
                                    {
                                        errorCode = "R060";
                                        errorMessage = "Table element parameter brackets expected";
                                    }
                                }
                                else if (tbx == 0)
                                {
                                    errorCode = "R059";
                                    errorMessage = "Table element expected to be on its own line";
                                }
                            }

                            if (line.StartsWith("$table") && tbx == 4)
                            {
                                bool triggerR063q = nextLine.IsNEW();
                                if (!nextLine.StartsWith("$td") && !nextLine.StartsWith("$th"))
                                {
                                    triggerR063q = true;
                                    if (nextLine.RemovePlainText().Contains("next"))
                                        if (nextLine.TrimEnd().EndsWith(nextLine.RemovePlainText().SnippetText("next", ";", Snip.Inc)))
                                            triggerR063q = !next2ndLine.StartsWith("$td") && !next2ndLine.StartsWith("$th");
                                }

                                if (triggerR063q)
                                {
                                    errorCode = "R063";
                                    errorMessage = "Table element must contain at least one table row";
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** TABLE HEADER errors
                        - [R064] Table Header element expected to be on its own line
                            . Occurs when a table header element is not the only steam format reference on its line (or is not at beginning)
                        - [R065] Table Header element assignment operator expected
                            . Occurs when a table header element is not followed by the '=' operator
                        - [R066] Table Header element expected after table block line
                            . Occurs when a table header element line is not preceded by a table block line 
                        - [R067] Table Header element is not within a table block
                            . Occurs when a table header element line is not preceded by a table block line or table data line
                        - [R068] Empty Table Header element
                            . Occurs when a table header element does not have any values following after the operator
                        - [R069] Table Header value '{val}' is an invalid value
                            . Occurs when a table header column value is an invalid value
                                $th= www, tyi   --> Table Header value 'www' is an invalid value                *****/
                        for (int thx = 0; thx < 6; thx++)
                        {
                            errorCode = null;
                            errorMessage = null;

                            if (noPlainLine.Contains("$th"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$th"))
                                {
                                    if (noPlainLine.CountOccuringCharacter('=') == 1 && line.StartsWith("$th="))
                                    {
                                        string thArgs = line.Substring("$th=".Length);
                                        if (thArgs.IsNotNEW())
                                        {
                                            string[] thClms = thArgs.RemoveFromPlainText(',').Split(',');
                                            string invalidValue = null;
                                            for (int tx = 0; tx < thClms.Length && invalidValue == null; tx++)
                                            {
                                                if (!IsValidSFValue(thClms[tx]))
                                                    invalidValue = thClms[tx] + (ID(0).IsNE() ? "" : $"(ix{tx})");
                                            }

                                            if (invalidValue != null && thx == 5)
                                            {
                                                errorCode = "R069";
                                                errorMessage = $"Table Header value '{invalidValue}' is an invalid value";
                                            }
                                        }
                                        else if (thx == 4)
                                        {
                                            errorCode = "R068";
                                            errorMessage = "Empty Table Header element";
                                        }
                                    }
                                    else if (thx == 1)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('=') > 1)
                                            unexpectedTokenII.Add("=" + ID(65));
                                        else
                                        {
                                            errorCode = "R065";
                                            errorMessage = "Table Header element assignment operator expected";
                                        }
                                    }
                                }
                                else if (thx == 0)
                                {
                                    errorCode = "R064";
                                    errorMessage = "Table Header element expected to be on its own line";
                                }
                            }

                            if (noPlainLine.StartsWith("$th"))
                            {
                                bool triggerR066q = prevLine.IsNEW();
                                bool triggerR067q = prevLine.IsNEW();
                                if (prevLine.IsNotNEW())
                                {
                                    triggerR066q = !prevLine.StartsWith("$table");
                                    triggerR067q = !prevLine.StartsWith("$table") && !prevLine.StartsWith("$td") && !prevLine.StartsWith("$th");

                                    if (triggerR067q && triggerR066q)
                                    {
                                        if (prevLine.RemovePlainText().Contains("next"))
                                            if (prevLine.TrimEnd().EndsWith(prevLine.RemovePlainText().SnippetText("next", ";", Snip.Inc)))
                                            {
                                                triggerR066q = !prev2ndLine.StartsWith("$table");
                                                triggerR067q = !prev2ndLine.StartsWith("$table") && !prev2ndLine.StartsWith("$td") && !prev2ndLine.StartsWith("$th");
                                            }
                                    }
                                }

                                if (triggerR066q && thx == 2)
                                {
                                    errorCode = "R066";
                                    errorMessage = "Table Header element expected after table block line";
                                }
                                if (triggerR067q && thx == 3)
                                {
                                    errorCode = "R067";
                                    errorMessage = "Table Header element is not within a table block";
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                        /** TABLE DATA errors
                        - [R070] Table Data element expected to be on its own line 
                            . Occurs when a table data element is not the only steam format reference on its line (or is not at beginning)
                        - [R071] Table Data element assignment operator expected
                            . Occurs when a table data element is not followed by the '=' operator
                        - [R072] Table Data element is not within a table block  
                            . Occurs when a table data element line is not preceded by a table block line or a table header line 
                        - [R073] Empty Table Data element
                            . Occurs when a table data element does not have any values following after the operator
                        - [R074] Table Data element does not match column count of preceding rows
                            . Occurs when a table data element's column count mismatches that of a preceding table data or table header line
                        - [R075] Table Data value '{val}' is an invalid value
                            . Occurs when a table data column value is an invalid value
                                $td= "Sweet", sp    --> Table Data value 'sp' is an invalid value                  *****/
                        for (int tdx = 0; tdx < 6; tdx++)
                        {
                            errorCode = null;
                            errorMessage = null;

                            if (noPlainLine.Contains("$td"))
                            {
                                if (noPlainLine.CountOccuringCharacter('$') == 1 && line.StartsWith("$td"))
                                {
                                    if (noPlainLine.CountOccuringCharacter('=') == 1 && line.StartsWith("$td="))
                                    {
                                        string tdArgs = $" {line.Substring("$td=".Length)} ";
                                        if (tdArgs.IsNotNEW())
                                        {
                                            if (prevLine.IsNotNE() && tdx == 4)
                                            {
                                                int countPrevArgs = 0;
                                                string checkArgsLine = prevLine;
                                                if (!prevLine.StartsWith("$td=") && !prevLine.StartsWith("$th=") && prevLine.RemovePlainText().Contains("next"))
                                                    checkArgsLine = prev2ndLine;

                                                if (checkArgsLine.StartsWith("$td=") || checkArgsLine.StartsWith("$th="))
                                                {
                                                    string txArgs = $" {checkArgsLine.Substring("$t?=".Length).RemovePlainText()} ";
                                                    if (txArgs.IsNotNE())
                                                    {
                                                        int countPrevLibRefCommas = txArgs.CountOccuringCharacter(':');
                                                        countPrevArgs = txArgs.CountOccuringCharacter(',') + 1 - countPrevLibRefCommas;
                                                    }
                                                }

                                                int countLibRefCommas = tdArgs.RemovePlainText().CountOccuringCharacter(':');
                                                int countThisArgs = tdArgs.RemovePlainText().CountOccuringCharacter(',') + 1 - countLibRefCommas;
                                                if (countPrevArgs != 0 && countPrevArgs != countThisArgs)
                                                {
                                                    errorCode = "R074";
                                                    errorMessage = "Table Data element does not match column count of preceding rows";
                                                    if (identifyErrorStatesQ)
                                                        errorMessage += $" ({countPrevArgs}vs{countThisArgs})";
                                                }
                                            }

                                            /// discern from library ref arrays and plain text
                                            string tdArgsFlt = "";
                                            bool nPlainQ = false, nLibRef = false;
                                            for (int tx = 0; tx < tdArgs.Length; tx++)
                                            {
                                                char tc = tdArgs[tx];
                                                if (tc == '"')
                                                    nPlainQ = !nPlainQ;
                                                if (!nPlainQ)
                                                {
                                                    if (tc == '{')
                                                        nLibRef = true;
                                                    if (tc == '}')
                                                        nLibRef = false;
                                                }

                                                if (!nPlainQ && !nLibRef && tc == ',')
                                                    tdArgsFlt += "\n";
                                                else tdArgsFlt += tc;
                                            }

                                            string[] tdClms = tdArgsFlt.Split('\n');
                                            string invalidValue = null;
                                            for (int tx = 0; tx < tdClms.Length && invalidValue == null; tx++)
                                            {
                                                if (!IsValidSFValue(tdClms[tx]))
                                                    invalidValue = tdClms[tx] + (ID(0).IsNE() ? "" : $"(ix{tx})");
                                            }

                                            if (invalidValue != null && tdx == 5)
                                            {
                                                errorCode = "R075";
                                                errorMessage = $"Table data value '{invalidValue}' is an invalid value";
                                            }
                                        }
                                        else if (tdx == 3)
                                        {
                                            errorCode = "R073";
                                            errorMessage = "Empty Table Data element";
                                        }
                                    }
                                    else if (tdx == 1)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('=') > 1)
                                            unexpectedTokenII.Add("=" + ID(71));
                                        else
                                        {
                                            errorCode = "R071";
                                            errorMessage = "Table Data element assignment operator expected";
                                        }
                                    }
                                }
                                else if (tdx == 0)
                                {
                                    errorCode = "R070";
                                    errorMessage = "Table Data element expected to be on its own line";
                                }
                            }

                            if (noPlainLine.StartsWith("$td"))
                            {
                                bool triggerR072q = prevLine.IsNEW();
                                if (!prevLine.StartsWith("$table") && !prevLine.StartsWith("$th") && !prevLine.StartsWith("$td"))
                                {
                                    triggerR072q = true;
                                    if (prevLine.RemovePlainText().Contains("next"))
                                        if (prevLine.TrimEnd().EndsWith(prevLine.RemovePlainText().SnippetText("next", ";", Snip.Inc)))
                                            triggerR072q = !prev2ndLine.StartsWith("$table") && !prev2ndLine.StartsWith("$th") && !prev2ndLine.StartsWith("$td");
                                }

                                if (triggerR072q && tdx == 2)
                                {
                                    errorCode = "R072";
                                    errorMessage = "Table Data element is not within a table block";
                                }
                            }

                            if (errorCode.IsNotNE() && errorMessage.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }


                        // METHOD FOR STEAM FORMAT
                        static bool IsValidSFValue(string value)
                        {
                            /*  Valid steam format values
                             *      -> Plain text ("txt")
                             *      -> Library references ({lr})
                             *      -> Repeat replacement character (#)   [X] not anymore...
                             */

                            bool isValid = false;
                            if (value.IsNotNEW())
                            {
                                value = value.Trim();
                                string getLibRef = $"{value.SnippetText("{", "}", Snip.Inc)}";
                                string getPlain = $"{value.SnippetText("\"", "\"", Snip.Inc, Snip.EndAft)}";

                                if (value.StartsWith(getPlain) && getPlain.IsNotNEW())
                                    isValid = true;
                                else if (value.StartsWith(getLibRef) && getLibRef.IsNotNEW())
                                    isValid = true;
                                //else if (value.Equals("#") && !noRepKeyQ)
                                //    isValid = true;
                                /// other syntax checkers will take care any of other errors (such as incorrect lib refs)
                            }
                            return isValid;
                        }
                    }



                    // unexpected token II
                    /** {RE.} CODE errors
                           - [G000]     Unexpected token '{token}'
                               . Accompanies most errors where general syntax is not followed.
                                   jam     --> Unexpected token 'jam'
                                   $       --> Unexpected token '$'
                                   if ;    --> Unexpected token ';'             ****/                    
                    if (unexpectedTokenII.HasElements())
                    {
                        foreach (string unexToken in unexpectedTokenII)
                            AddToErrorList(lineNum, "G000" + ID(1), $"Unexpected token '{unexToken}'");
                    }                    
                    if (noPlainLine.IsNotNE() && !line.TrimStart().StartsWith("//"))
                    { /// wrapping
                        string[] validPPs = new string[]
                        {
                            /// general code
                            "#", "if", "else", "repeat", "jump", "next", "=", "!=",
                            /// library refs
                            "{",
                            /// steam format refs
                            "$"
                        };

                        int countKeywordSemiColons = noPlainLine.CountOccuringCharacter(';');
                        string[] noPlainParts = noPlainLine.Split(' ');
                        bool firstPartQ = true;
                        foreach (string noPP in noPlainParts)
                        {
                            if (noPP.IsNotNE())
                            {
                                bool isValidPP = false;
                                for (int vx = 0; vx < validPPs.Length && !isValidPP; vx++)
                                {
                                    isValidPP = noPP.StartsWith(validPPs[vx]);
                                    if (!isValidPP && countKeywordSemiColons > 0)
                                        isValidPP = int.TryParse(noPP.Replace(';', ' '), out _);

                                    /// comment specific
                                    if (!isValidPP && firstPartQ)
                                        isValidPP = noPP.StartsWith("//");
                                    /// keyword specific
                                    if (!isValidPP && countKeywordSemiColons > 0)
                                    {
                                        if (noPlainLine.StartsWith("if") || noPlainLine.StartsWith("else") || noPlainLine.StartsWith("repeat"))
                                            if (noPlainLine.Contains("if"))
                                            {
                                                string validOps = "= != > >= < <= ";
                                                isValidPP = validOps.Contains($"{noPP} ");
                                            }
                                    }
                                    /// complex steam format specific
                                    if (!isValidPP && noPlainLine.StartsWith("$"))
                                        isValidPP = noPP.Contains(":") || noPP.Contains(",");
                                }

                                if (noPP.Contains(';'))
                                {
                                    /// keyword specific II
                                    if (!isValidPP)
                                        isValidPP = noPP.Equals(";");
                                    countKeywordSemiColons--;
                                }

                                if (!isValidPP)
                                    AddToErrorList(lineNum, "G000" + ID(2), $"Unexpected token '{noPP}'");
                                firstPartQ = false;
                            }
                        }
                    }
                    
                }

            }


            /** ERROR CODES AND MESSAGES COLLECTION
                
                ErrCode     ErrMessage
                -------     -------------------
                [G000]      Unexpected token '{token}'
                [G001]      Empty plain text value
                [G002]      Closing double quotation expected      
                [G003]      Unidentified escape character '{text}'
                [G004]      Closing curly bracket expected
                [G005]      Empty library reference                
                [G006]      Unidentified library reference
                [G007]      Empty steam format reference
                [G008]      Unidentified steam format reference
                [G009]      Closing colon expected
                [G010]      Keyword '{keyword}' expected at beginning
                [G011]      Missing line to execute after keyword
                [G012]      Misplaced keyword '{keyword}'
                [G013]      First comparable value expected
                [G014]      Operator expected
                [G015]      Unidentified operator
                [G016]      Second comparable value expected
                [G017]      Missing preceding 'if' control line
                [G018]      First value expected
                [G019]      First value is an invalid number
                [G020]      Exceeded keyword limit per line
                [G021]      Line number expected
                [G022]      Line number must follow after line '{lineNum}'
                [G023]      Jump keyword must precede an appropriate keyword
                [G076]      Next keyword must precede an appropriate keyword
                [G077]      Next keyword requires a following code line to function
                [G078]      Next keyword line cannot be followed by another keyword line
                [G079]      Jump keyword expected at ending
                [G080]      Next keyword expected at ending
                ---
                [R024]      Added entry number and property expected
                [R025]      Invalid Added entry number
                [R026]      Invalid Added property
                [R027]      Addit entry number and property expected
                [R028]      Invalid Addit entry number
                [R029]      Invalid Addit property
                [R030]      Updated entry number and property expected
                [R031]      Invalid Updated entry number
                [R032]      Invalid Updated property
                [R033]      Legend entry number and property expected
                [R034]      Invalid Legend entry number
                [R035]      Invalid Legend property
                [R036]      Summary entry number expected
                [R037]      Invalid Summary entry number
                ---
                [R038]      Missing value to format
                [R039]      Invalid value to format: '{val}'
                [R040]      Heading element expected at beginning
                [R041]      Heading element cannot be combined with other commands
                [R042]      Horizontal Rule element expected to be on its own line
                [R043]      List Item element expected at beginning
                [R044]      List Item element is not within a list block
                [R045]      URL element expected to be on its own line 
                [R046]      URL element assignment operator expected
                [R047]      Empty URL element
                [R048]      URL value for link expected
                [R049]      URL value for name expected
                [R050]      List element expected to be on its own line
                [R051]      List element parameter brackets expected
                [R052]      Invalid List element parameter '{param}'
                [R053]      List element must contain at least one list item
                [R054]      Quote element expected to be on its own line
                [R055]      Quote element assignment operator expected
                [R056]      Empty Quote element 
                [R057]      Quote value for author expected
                [R058]      Quote value for qoute expected
                [R059]      Table element expected to be on its own line
                [R060]      Table element parameter brackets expected
                [R061]      Invalid Table element parameter '{param}'
                [R062]      Table element only accepts two or less parameters
                [R063]      Table element must contain at least one table row
                [R064]      Table Header element expected to be on its own line
                [R065]      Table Header element assignment operator expected
                [R066]      Table Header element expected after table block line
                [R067]      Table Header element is not within a table block
                [R068]      Empty Table Header element
                [R069]      Table Header value '{val}' is an invalid value
                [R070]      Table Data element expected to be on its own line
                [R071]      Table Data element assignment operator expected
                [R072]      Table Data element is not within a table block
                [R073]      Empty Table Data element
                [R074]      Table Data element does not match column count of preceding rows
                [R075]      Table Data value '{val}' is an invalid value

             */


            // METHODS
            /// Surely these don't need to feel the open air...
            static void AddToErrorList(int lineNum, string errCode, string errMessage)
            {
                SFormatterInfo error = new(lineNum, errCode, errMessage);
                if (error.IsSetup())
                {
                    bool isDupeQ = false;
                    foreach (SFormatterInfo sfi in errors)
                    {
                        if (sfi.Equals(error))
                        {
                            isDupeQ = true;
                            break;
                        }
                    }
                    if (!isDupeQ)
                        errors.Add(error);
                }
            }
            string ID(int num)
            {
                string id = "";
                if (num >= 0 && identifyErrorStatesQ)
                    id = $" {cRHB}{num}";
                return id;
            }
        }
        public static SFormatterInfo[] GetErrors(int lineNum)
        {
            List<SFormatterInfo> lineErrors = new();
            if (errors.HasElements() && lineNum > 0)
            {
                foreach (SFormatterInfo error in errors)
                    if (error.IsSetup() && error.lineNumber == lineNum)
                        lineErrors.Add(error);
            }
            return lineErrors.ToArray();
        }


        #region CheckSyntaxToolMethods
        /// <summary>
        ///     Removes all space characters from a given string.
        /// </summary>
        public static string SquishSpaces(this string line)
        {
            string squishedLine = "";
            if (line.IsNotNEW())
                foreach (char c in line)
                {
                    if (c != ' ')
                        squishedLine += c.ToString();
                }
            return squishedLine;
        }
        /// <summary>Removes any plain text fields from a line.</summary>
        /// <param name="invertQ">If <c>true</c>, will only fetch plain text fields without inclusion of double quotations.</param>
        public static string RemovePlainText(this string line, bool invertQ = false)
        {
            string newLine = line.IsNE() ? "" : line;
            bool nPlainQ = false;
            if (line.IsNotNE())
            {
                newLine = "";
                for (int px = 0; px < line.Length; px++)
                {
                    if (line[px] == '"')
                        nPlainQ = !nPlainQ;

                    if (line[px] != '"')
                        if ((!nPlainQ && !invertQ) || (nPlainQ && invertQ))
                            newLine += line[px];
                }
                if (!invertQ)
                    newLine = newLine.Trim(' ');
            }
            return newLine;
        }
        /// <summary>Removes any plain text fields from a line following a given character.</summary>
        /// <param name="after">The character to hit within line before removing plain text fields. Cannnot be '"'.</param>
        /// <param name="secondAfterQ">If <c>true</c>, will remove plain text fields after the second <paramref name="after"/> character is hit.</param>
        /// <param name="noAfterInPlainQ">If <c>true</c>, will remove any occurences of <paramref name="after"/> within any unfiltered plain text fields.</param>
        public static string RemovePlainTextAfter(this string line, char after, bool secondAfterQ = false, bool noAfterInPlainQ = false)
        {
            string newLine = line.IsNE() ? "" : line;
            if (after == '"')
                after = '\0';
            bool nPlainQ = false;
            int afterCharHits = 0;
            if (line.IsNotNE() && after.IsNotNull())
            {
                newLine = "";
                for (int px = 0; px < line.Length; px++)
                {
                    bool hasHitAfterChar = secondAfterQ ? afterCharHits >= 2 : afterCharHits >= 1;

                    if (line[px] == '"')
                        nPlainQ = !nPlainQ;

                    if (hasHitAfterChar)
                    {
                        if (line[px] != '"' && !nPlainQ)
                            newLine += line[px];
                    }
                    else
                    {
                        if (noAfterInPlainQ)
                        {
                            if (!(nPlainQ && line[px] == after))
                                newLine += line[px];
                        }
                        else newLine += line[px];
                    }

                    if (!nPlainQ)
                        if (line[px] == after)
                            afterCharHits++;
                }
                newLine = newLine.Trim();
            }
            return newLine;
        }
        /// <summary>Produces an array of strings deriving from <paramref name="line"/> that start with or end with <paramref name="c"/> dependent on the value of <paramref name="breakEndQ"/>. </summary>
        /// <param name="line">The line containing <paramref name="c"/> to break apart.</param>
        /// <param name="c">The character that determines where to split <paramref name="line"/>.</param>
        /// <param name="breakEndQ">If <c>true</c>, will ensure that each line break ends with <paramref name="c"/>.</param>
        /// <param name="ignoreCinPlainQ">If <c>true</c> will only split at any <paramref name="c"/> that is not within a plain text field. <paramref name="c"/> cannot be (") when this argument is <c>true</c>.</param>
        /// <returns>An array of strings broken apart at and starting or ending with <paramref name="c"/> dependent on <paramref name="breakEndQ"/>. Returns <see cref="Array.Empty{T}"/> if <paramref name="line"/> or <paramref name="c"/> is null, empty, or whitespace. Returns a single-element array if <paramref name="c"/> is not contained in <paramref name="line"/>.</returns>
        public static string[] LineBreak(this string line, char c, bool breakEndQ = false, bool ignoreCinPlainQ = true)
        {
            List<string> lineBreaks = new();
            if (line.IsNotNEW() && c.IsNotNull())
            {
                /// manual split
                List<string> partLines = new();
                bool nPlainQ = false;
                string linePart = "";
                for (int spx = 0; spx < line.Length; spx++)
                {
                    char lc = line[spx];

                    if (lc == '"')
                        nPlainQ = !nPlainQ;

                    if (lc == c)
                    {
                        if (ignoreCinPlainQ && nPlainQ)
                            linePart += lc;
                        else
                        {
                            if (c == '"' && ignoreCinPlainQ)
                                linePart += lc;
                            else
                            {
                                partLines.Add(linePart);
                                linePart = "";
                            }
                        }
                    }
                    else linePart += lc;

                    if (spx + 1 == line.Length)
                        partLines.Add(linePart);
                }

                /// line breaking
                for (int px = 0; px < partLines.Count; px++)
                {
                    string lineBreak = partLines[px];
                    if (px > 0 && !breakEndQ)
                        lineBreak = c.ToString() + lineBreak;
                    if (px + 1 < partLines.Count && breakEndQ)
                        lineBreak = lineBreak + c.ToString();
                    
                    if (lineBreak.IsNotNE())
                        lineBreaks.Add(lineBreak);
                }
            }
            return lineBreaks.ToArray();
        }
        /// <summary>Removes any occuring character '<paramref name="c"/>' within a plain text field.</summary>
        /// <param name="c">The character to remove from or to replace with <paramref name="replacement"/> in plain text fields.</param>
        public static string RemoveFromPlainText(this string line, char c, bool caseSensitivityQ = true, char replacement = '\0')
        {
            string newLine = line.IsNE() ? "" : line;
            string rep = replacement.IsNotNull() ? replacement.ToString() : "";
            bool nPlainQ = false;
            if (line.IsNotNE() && c.IsNotNull())
            {
                newLine = "";
                for (int px = 0; px < line.Length; px++)
                {
                    if (line[px] == '"')
                        nPlainQ = !nPlainQ;

                    if (nPlainQ)
                    {
                        if (!caseSensitivityQ)
                        {
                            if (line[px].ToString().ToLower() == c.ToString().ToLower())
                                newLine += rep;
                            else newLine += line[px];
                        }
                        else
                        {
                            if (line[px] == c)
                                newLine += rep;
                            else newLine += line[px];
                        }
                    }
                    else newLine += line[px];
                }
                //newLine = newLine.Trim();
            }
            return newLine;
        }

        public static void TestSyntaxCheckTools()
        {
            Dbug.StartLogging("Testing Check Syntax Tools");
            for (int x = 0; x < 6; x++)
            {
                string toolName = x switch
                {
                    0 => "Remove Plain Text",
                    1 => "Snippet Text (promoted to 'Extension.cs')",
                    2 => "Squish Spaces",
                    3 => "Remove Plain Text After",
                    4 => "Line Break",
                    5 => "Remove From Plain Text",
                    _ => null
                };
                List<string> tests = x switch
                {
                    /// remove plain text
                    ///     parameters:  invert[true/false]
                    0 => new List<string>()
                    {
                        "This is \"this really is\" dumb \"as funck\"",
                        "There is no more \"after this thing",
                        "\"Starting off strong\" but what now"
                    },

                    /// snippet text 
                    ///     parameters:  startWith["c"]  endWith["k"/"c"]   Snip[inc/endAft/End2nd/EndLast/All]
                    1 => new List<string>()
                    {
                        "c - k",
                        "bcdefghijkl",
                        "camp swoonk rock",
                        "kant celp",
                        "ckck_kck",
                        "kloop coop knew",
                        "kent cart scopik"
                    },

                    /// squish spaces
                    2 => new List<string>()
                    {
                        "o n e w o r d",
                        "slap: thy face (very hard)!"
                    },

                    /// remove plain text after
                    ///     parameters: after['.']  secondAfter[true/false]  noAftInPlain[true/false]
                    3 => new List<string>()
                    {
                        "\"No.\" n \"Yes.\" n \"Maybe.\" ",
                        "What. \"In.\" The. \"Funking\" Duck. Bro",
                        "\"This \"Might. \"Not \"Be.\" A.\" Pain.",
                        "\"Send. Help. Please.\""
                    },

                    /// line break
                    ///     parameters: c['/']   breakEndQ[true/false]      ignoreCInPlain[true/false]
                    4 => new List<string>()
                    {
                        "No break in line, should be empty just fine",
                        "Break once / Break no more",
                        "There / are multiple/ breaks in /here",
                        "/start it/ and/ end it/",
                        "To/ Break/ in \"plain/ depends/ on \"/ a certain/ thing"
                    },

                    /// remove from plain text
                    ///     parameters: c['a']      caseSensitive[true/false]     replacement['\0','*']
                    5 => new List<string>()
                    {
                        "Sleek cruiser \"Sleek cruiser\"",
                        "Amazing as asparagus \"Amazing as asparagus\"",
                    },

                    _ => new List<string>()
                };


                Dbug.Log($"Testing :: {toolName}");
                Dbug.NudgeIndent(true);
                foreach (string test in tests)
                {
                    Dbug.LogPart($"Input: '{test}'    //    Output: ");
                    switch (x)
                    {
                        case 0:
                            Dbug.LogPart($"'{RemovePlainText(test)}'  |  ");
                            Dbug.LogPart($"[invert] '{RemovePlainText(test, true)}'");
                            break;

                        case 1:
                            Dbug.LogPart($"[c,k,in] '{test.SnippetText("c", "k", Snip.Inc)}'  |  ");
                            Dbug.LogPart($"[c,k,ex] '{test.SnippetText("c", "k")}'  |  ");
                            Dbug.LogPart($"[c,k,in,aftr] '{test.SnippetText("c", "k", Snip.Inc, Snip.EndAft)}'  |  ");
                            Dbug.Log($"[c,k,ex,aftr] '{test.SnippetText("c", "k", Snip.EndAft)}' ...");
                            Dbug.LogPart($"                                 ... ");
                            Dbug.LogPart($"[c,c,in,aftr] '{test.SnippetText("c", "c", Snip.Inc, Snip.EndAft)}'  |  ");
                            Dbug.LogPart($"[c,c,in] '{test.SnippetText("c", "c", Snip.Inc)}'  |  ");
                            Dbug.LogPart($"[c,k,in,2nd] '{test.SnippetText("c", "k", Snip.Inc, Snip.End2nd)}'  |  ");
                            Dbug.LogPart($"[c,k,ex,2nd] '{test.SnippetText("c", "k", Snip.End2nd)}'  |  ");
                            Dbug.LogPart($"[c,k,in,aftr,last] '{test.SnippetText("c", "k", Snip.All)}'");
                            break;

                        case 2:
                            Dbug.LogPart($"'{SquishSpaces(test)}'");
                            break;

                        case 3:
                            Dbug.LogPart($"[.,1st] '{test.RemovePlainTextAfter('.')}'  |  ");
                            Dbug.LogPart($"[.,2nd] '{test.RemovePlainTextAfter('.', true)}'  |  ");
                            Dbug.LogPart($"[.,1st,noAft] '{test.RemovePlainTextAfter('.', false, true)}'  |  ");
                            Dbug.LogPart($"[.,2nd,noAft] '{test.RemovePlainTextAfter('.', true, true)}'");
                            break;

                        case 4:
                            string[] lBrEndFalse = test.LineBreak('/');
                            if (lBrEndFalse.HasElements())
                            {
                                Dbug.LogPart("--> [/,brStrt,ignC] ");
                                foreach (string lbref in lBrEndFalse)
                                    Dbug.LogPart($" '{lbref}' ");
                            }
                            else Dbug.LogPart("--> No output");

                            Dbug.LogPart("   |   ");
                            string[] lBrEndTrue = test.LineBreak('/', true);
                            if (lBrEndTrue.HasElements())
                            {
                                Dbug.LogPart("--> [/,brEnd,ignoreC] ");
                                foreach (string lbret in lBrEndTrue)
                                    Dbug.LogPart($" '{lbret}' ");
                            }
                            else Dbug.LogPart("--> No output");

                            Dbug.LogPart("   |   ");
                            string[] lBrInPlain = test.LineBreak('/', true, false);
                            if (lBrInPlain.HasElements())
                            {
                                Dbug.LogPart("--> [/,brEnd,inC] ");
                                foreach (string lbret in lBrInPlain)
                                    Dbug.LogPart($" '{lbret}' ");
                            }
                            else Dbug.LogPart("--> No output");
                            break;

                        case 5:
                            Dbug.LogPart($"[a,caseSen] '{test.RemoveFromPlainText('a')}'  |  ");
                            Dbug.LogPart($"[a,caseIns] '{test.RemoveFromPlainText('a', false)}'  |  ");
                            Dbug.LogPart($"[a,caseIns,*] '{test.RemoveFromPlainText('a', false, '*')}'");
                            break;
                    }
                    Dbug.Log("; ");
                }
                Dbug.NudgeIndent(false);
            }
            Dbug.EndLogging();
        }
        #endregion
    }
}
