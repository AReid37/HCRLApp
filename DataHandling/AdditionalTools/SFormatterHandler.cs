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
                    const string sComment = "//", sEscape = "&00;", sKeyw_if = "if", sKeyw_repeat = "repeat", sKeyw_else = "else", sKeyw_jump = "jump";
                    const char cPlain = '"', cEscStart = '&', cEscEnd = ';', cOpEqual = '=', cOpUnequal = '!', cRefSteam = '$', cRefLibOpen = '{', cRefLibClose = '}';

                    Dbug.Log($"Recieved '{fLine}'; Numbering types; ");
                    /// numbering
                    // TYPE NUMBERS (in order of precedence) :: code[0]     comment[1]     plaint[2]     escape[3]     reference[4]     keyword[5]     operator[6]
                    string numberedCopy = "", fBatched;
                    char prevFChar = '\0';
                    bool hit1stNonSpaceQ = false, justHitNonSpaceQ = false;
                    const int noIx = -1;
                    int typeNum = 0, prevTypeNum = 0, ixLastBatched = 0, ixKeyWordEnd = noIx;
                    bool isCommentQ = false, nPlainTQ = false, nEscQ = false, nKeyWordQ = false, nRefQ = false, isLibRefQ = false;
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
                                if (fChar == cOpEqual)
                                {
                                    typeNum = 6;
                                    Dbug.LogPart($"op; ");
                                }
                                if (fChar == cOpUnequal && GetStringFromChars(fx, 2) == cOpUnequal.ToString() + cOpEqual.ToString())
                                {
                                    typeNum = 6;
                                    Dbug.LogPart($"op; ");
                                }

                                /// keywords[5]                                
                                if (!nKeyWordQ /*&& justHitNonSpaceQ*/)
                                {   /// they used to be only identified up front, but anywhere they still are (perhaps not agreeing with syntax though)
                                    if (!justHitNonSpaceQ && enableMethodPartLogging)
                                        enableMethodPartLogging = false;
                                    else Dbug.LogPart("Keyword (x3) --> ");

                                    string theKeyword = "";
                                    /// IF ...: keyword 'if'; ELSE IF ...: keyword 'else'; ELSE IF ...: keyword 'repeat'; 
                                    if (GetStringFromChars(fx, sKeyw_if.Length) == sKeyw_if)
                                        theKeyword = sKeyw_if;
                                    else if (GetStringFromChars(fx, sKeyw_else.Length) == sKeyw_else)
                                        theKeyword = sKeyw_else;
                                    else if (GetStringFromChars(fx, sKeyw_repeat.Length) == sKeyw_repeat)
                                        theKeyword = sKeyw_repeat;
                                    else if (GetStringFromChars(fx, sKeyw_jump.Length) == sKeyw_jump)
                                        theKeyword = sKeyw_jump;

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
                                    if (fChar == cEscStart && GetStringFromChars(fx, sEscape.Length) == sEscape)
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
                    . Each error message will be identified with a unique three-digit number and a token signifitying the type of error: G - General, R - Reference (Library and Steam Format specific). Every error has a unique four-digit number regardless of type. 'G001' and 'R001' cannot simultaneously exist, they are both '001'
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
                                                :    Values for 'prop': changeDesc, id, name.
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

            bool identifyErrorStatesQ = true;
            if (lineData.HasElements())
            {
                for (int lx = 0; lx < lineData.Length; lx++)
                {
                    string line = lineData[lx], prevLine = null;
                    if (lx > 0)
                        prevLine = lineData[lx - 1];
                    string noPlainLine = RemovePlainText(line);
                    int lineNum = lx + 1;

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
                        for (int codeix = 0; codeix < 9; codeix++)
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
                                        else token = ";" + ID(10); /// Ex| &;;
                                    }
                                    /// the no plain errors take precedence over plain only errors
                                    if (noPlainLine.Contains("&"))
                                    {
                                        if (noPlainLine.SnippetText("&", ";", Snip.EndAft).IsNotNE())
                                            token = "&" + ID(1); /// Ex| &;   |  &  ;
                                        else if (noPlainLine.SnippetText(";", "&", Snip.EndAft).IsNotNE())
                                            token = ";" + ID(11); /// Ex| ; &  |  ;&
                                        else token = "&" + ID(2); /// Ex| &
                                    }
                                    else if (noPlainLine.SquishSpaces().StartsWith(";"))
                                        token = ";" + ID(12); /// Ex| ;
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
                                            if (noPlainLine.SnippetText("{", "}", Snip.EndAft).IsEW())
                                                token = "}" + ID(1); /// Ex: {  }     why? The library reference expects reference name
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
                                        }
                                        else if (noPlainLine.Contains("[") && noPlainLine.SnippetText("$", "[").IsEW())
                                            token = "[" + ID(1); /// Ex|  $[  |  $ [  |  [$ 
                                        else if (noPlainLine.Contains("]") && noPlainLine.SnippetText("$", "]").IsEW())
                                            token = "]" + ID(1); /// Ex|  ]$  |  $]   |  $ ]
                                        else if (noPlainLine.EndsWith("$"))
                                            token = "$" + ID(1); /// Ex:  q $ 
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
                                                string snipElse = noPlainLine.SnippetText("else", ";", Snip.Inc);
                                                token = snipElse.StartsWith("else")? snipElse + ID(1) : ";" + ID(3); /// Ex| else "no"  |  else "you" ;  |  else ;  | ; else
                                            }

                                        }
                                        else
                                        {
                                            string snipElseToLastColon = noPlainLine.SnippetText("else", ";", Snip.EndLast);
                                            if (snipElseToLastColon != null)
                                                if (snipElseToLastColon.CountOccuringCharacter(';') > 0 && !snipElseToLastColon.Contains("if") && !snipElseToLastColon.Contains("jump"))
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
                                        token = "#"; /// Ex| else; #
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
                            . Occurs when the plain text escape character does not follow the right format (&00;)
                                "&0;"   --> Unidentified escape character '&0;'
                                "&01;"  --> Unidentified escape character '&01;'       ****/
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
                                        string plainPart = line.SnippetText("\"", line[^1].ToString(), Snip.Inc, Snip.EndAft);
                                        if (line.EndsWith(plainPart) && plainPart.IsNotNE())
                                        {
                                            errorMessage = "Closing double quotation expected";
                                            errorCode = "G002"; /// Ex|  "butter  |  shea "butter
                                        }
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
                                                string possibleEsc = plainLine.SnippetText("&", ";", Snip.EndAft);
                                                if (possibleEsc.IsNE())
                                                    token = "&;"; /// Ex| "&;"
                                                else if (possibleEsc != "00")
                                                    token = $"&{possibleEsc};"; /// Ex| "& ;"  |  "&01;"  |  "&34;"  |  "&ab;" 
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
                                    if (noPlainLine.Contains("$ ") || noPlainLine.EndsWith("$"))
                                    {
                                        errorCode = "G007";
                                        errorMessage = "Empty steam format reference"; /// Ex| $  |  $ t
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
                                                  $url='link':'name'
                                                  $list[or]
                                                  $*
                                                  $q='author':'quote'
                                                  $table[nb,ec]
                                                  $th='clm1','clm2'
                                                  $td='clm1','clm2'
                                             */

                                            "$h ", "$hh ", "$hhh ", "$b ", "$u ", "$i ",
                                            "$s ", "$sp ", "$np ", "$c ", "$hr", "$nl ", "$url=",
                                            "$list[", "$* ", "$q=", "$table[", "$th=", "$td="
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
                                    repeat 2; jump 1    --> Missing first 'if' or 'else' keyword                                    *****/
                        for (int kx = 0; kx < 5; kx++)
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
                                    for (int gkx = 0; gkx < 5; gkx++)
                                    {
                                        errorMessage = null;
                                        errorCode = null;

                                        switch (gkx)
                                        {
                                            /// closing colon expected
                                            case 0:
                                                int keywordCount = 0;
                                                for (int ckx = 0; ckx < 4; ckx++)
                                                {
                                                    keywordCount += ckx switch
                                                    {
                                                        0 => noPlainLine.Contains("if") ? 1 : 0,
                                                        1 => noPlainLine.Contains("else") ? 1 : 0,
                                                        2 => noPlainLine.Contains("repeat") ? 1 : 0,
                                                        3 => noPlainLine.Contains("jump") ? 1 : 0,
                                                        _ => 0
                                                    };
                                                }
                                                if (keywordCount > noPlainLine.CountOccuringCharacter(';'))
                                                {
                                                    errorCode = "G009";
                                                    errorMessage = "Closing colon expected"; /// Ex|  if 1 = 2  |  else  |  repeat 7  |  jump 2  |  if 0 = 0; jump 2
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
                                                            if (!snipControl.Contains("jump"))
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

                                                            bool misplacedIfQ = !remainingControl.SquishSpaces().StartsWith("if") && remainingControl.Contains("if");
                                                            bool misplacedJumpQ = !remainingControl.SquishSpaces().StartsWith("jump") && remainingControl.Contains("jump");
                                                            if ((misplacedIfQ || misplacedJumpQ) && secondControl.IsNotNEW())
                                                            {
                                                                errorCode = "G012";
                                                                errorMessage = $"Misplaced keyword '{secondControl}'";
                                                            }
                                                        }
                                                }
                                                break;

                                            /// keyword limit exceeded
                                            case 4:
                                                int countKeywords = 0;
                                                for (int ckx = 0; ckx < 4; ckx++)
                                                {
                                                    countKeywords += ckx switch
                                                    {
                                                        0 => noPlainLine.Contains("if") ? 1 : 0,
                                                        1 => noPlainLine.Contains("else") ? 1 : 0,
                                                        2 => noPlainLine.Contains("repeat") ? 1 : 0,
                                                        3 => noPlainLine.Contains("jump") ? 1 : 0,
                                                        _ => 0
                                                    };
                                                }
                                                if (countKeywords > 2)
                                                {
                                                    errorCode = "G020";
                                                    errorMessage = "Exceeded keyword limit per line";
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
                                        . Occurs when an operator (either '=' or '!=') does not follow after the first value
                                            if "a" ;    --> Unidentified operator
                                    - [G016]     Second comparable value expected
                                        . Occurs when the second value of the condition is missing or is not a value
                                            if "a" =    --> Second comparable value expected
                                     ****/
                                    /// some required setup
                                    List<string> snipIfs = new List<string>();
                                    if (noPlainLine.Contains(";"))
                                    {
                                        string snip1stIf = null, snip2ndIf = "";
                                        string plainAfterColon = line.RemovePlainTextAfter(';', true, true);

                                        if (line.StartsWith("if"))
                                        {
                                            string snipFullControl = plainAfterColon.SnippetText("if", ";", Snip.Inc, Snip.End2nd);
                                            snip1stIf = snipFullControl.SnippetText("if", ";", Snip.Inc);
                                            if (snip1stIf.IsNotNE())
                                                snip2ndIf = snipFullControl.Substring(snip1stIf.Length).SnippetText("if", ";", Snip.Inc);
                                        }
                                        else if (line.StartsWith("else"))
                                        {
                                            string snipFullControl = line.SnippetText("else", ";", Snip.Inc, Snip.End2nd);
                                            string snip1stControl = line.SnippetText("else", ";", Snip.Inc);
                                            if (snip1stControl.IsNotNE())
                                                snip2ndIf = snipFullControl.Substring(snip1stControl.Length).SnippetText("if", ";", Snip.Inc);
                                        }
                                        else if (line.StartsWith("repeat"))
                                        {
                                            string snipFullControl = line.SnippetText("repeat", ";", Snip.Inc, Snip.End2nd);
                                            string snip1stControl = line.SnippetText("repeat", ";", Snip.Inc);
                                            if (snip1stControl.IsNotNE())
                                                snip2ndIf = snipFullControl.Substring(snip1stControl.Length).SnippetText("if", ";", Snip.Inc);
                                        }

                                        if (snip1stIf.IsNotNEW())
                                            snipIfs.Add(snip1stIf);
                                        if (snip2ndIf.IsNotNEW())
                                            snipIfs.Add(snip2ndIf);
                                    }
                                    for (int fx = 0; fx < 4 && snipIfs.HasElements(); fx++)
                                    {
                                        foreach (string snipIf in snipIfs)
                                        {
                                            errorMessage = null;
                                            errorCode = null;

                                            switch (fx)
                                            {
                                                /// first value missing
                                                case 0:
                                                    int countValid = 0;
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
                                                    // above used between this case (0) and case 3
                                                    string value1Snip = snipIf.SnippetText("if", "=");
                                                    if (value1Snip.IsNE())
                                                        value1Snip = snipIf.SnippetText("if", ";");
                                                    else if (value1Snip.EndsWith("!"))
                                                        value1Snip = value1Snip[..^1];

                                                    for (int vvx = 0; vvx < validValues.Length; vvx++)
                                                    {
                                                        if (value1Snip.Trim().StartsWith("{"))
                                                        {
                                                            if (value1Snip.Trim().StartsWith(validValues[vvx]))
                                                                countValid++;
                                                        }
                                                        else if (value1Snip.Contains('\"') || value1Snip.Contains("#"))
                                                            countValid++;
                                                        else
                                                        {
                                                            if (int.TryParse(value1Snip, out _))
                                                                countValid++;
                                                        }
                                                    }
                                                    if (countValid == 0)
                                                    {
                                                        errorCode = "G013";
                                                        errorMessage = "First comparable value expected";
                                                    }
                                                    break;

                                                /// operator expected
                                                case 1:           
                                                    if (!snipIf.RemovePlainText().Contains("=") && !snipIf.RemovePlainText().Contains("!"))
                                                    {
                                                        errorCode = "G014";
                                                        errorMessage = "Operator expected";
                                                    }
                                                    break;

                                                /// invalid operator
                                                case 2:
                                                    string snipOperator = snipIf.RemovePlainText().SnippetText("=", ";", Snip.Inc);
                                                    string snipOp2 = snipIf.RemovePlainText().SnippetText("!", ";", Snip.Inc);
                                                    if (snipOp2.IsNotNEW())
                                                    {
                                                        if (snipOperator.IsNEW())
                                                            snipOperator = snipOp2;
                                                        else if (snipOperator.Length < snipOp2.Length)
                                                            snipOperator = snipOp2;
                                                    }
                                                    if (snipOperator != null)
                                                    {
                                                        if (snipOperator.CountOccuringCharacter('=') > 1 || snipOperator.CountOccuringCharacter('!') > 1 || snipOperator.Contains("=!"))
                                                        {
                                                            errorCode = "G015";
                                                            errorMessage = "Unidentified operator";
                                                        }
                                                    }
                                                    break;

                                                /// second value missing
                                                case 3:
                                                    countValid = 0;
                                                    validValues = new string[]
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
                                                    string value2Snip = snipIf.SnippetText("=", ";");
                                                    if (value2Snip.IsNotNE())
                                                    {
                                                        for (int vvx = 0; vvx < validValues.Length; vvx++)
                                                        {
                                                            if (value2Snip.Trim().StartsWith("{"))
                                                            {
                                                                if (value2Snip.Trim().StartsWith(validValues[vvx]))
                                                                    countValid++;
                                                            }
                                                            else if (value2Snip.Contains('\"') || value2Snip.Contains("#"))
                                                                countValid++;
                                                            else
                                                            {
                                                                if (int.TryParse(value2Snip, out _))
                                                                    countValid++;
                                                            }
                                                        }
                                                    }
                                                    if (countValid == 0)
                                                    {
                                                        errorCode = "G016";
                                                        errorMessage = "Second comparable value expected";
                                                    }
                                                    break;
                                            }

                                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                                AddToErrorList(lineNum, errorCode, errorMessage);
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
                                                                Pure numerics (0~9)
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

                                                        bool invalidValue = false;
                                                        /// plain text nums
                                                        if (snipRepeat.Contains('"'))
                                                        {
                                                            if (int.TryParse(snipRepeat.RemovePlainText(true), out int num))
                                                                invalidValue = num < 1;
                                                            else invalidValue = true;
                                                        }
                                                        /// valid lib references
                                                        else if (snipRepeat.Contains("{"))
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
                                    - [G023]    Missing first 'if' or 'else' keyword
                                        . Occurs when a line containing a 'jump' keyword does not start with an 'if' or 'else' keyword
                                            jump 1;             --> Missing first 'if' or 'else' keyword
                                            repeat 2; jump 1    --> Missing first 'if' or 'else' keyword    ******/
                                    for (int jx = 0; jx < 3; jx++)
                                    {
                                        errorMessage = null;
                                        errorCode = null;

                                        string snipJump = line.SnippetText("jump", ";", Snip.Inc, Snip.EndAft);
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
                                                    errorMessage = "Missing first 'if' or 'else' keyword"; /// Ex| repeat 3; jump 2; | jump; else;  |  jump;
                                                }
                                                break;
                                        }

                                        if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                            AddToErrorList(lineNum, errorCode, errorMessage);
                                    }
                                    break;
                            }

                            if (errorMessage.IsNotNE() && errorCode.IsNotNE())
                                AddToErrorList(lineNum, errorCode, errorMessage);
                        }

                    }



                    // ~~  LIBRARY REFERENCE SYNTAX  ~~
                    if (!line.TrimStart().StartsWith("//"))
                    { /// section wrapping
                        //const string tokenTag = "[token]";
                        //string errorMessage;
                        //string token;

                    }



                    // ~~  STEAM FORMAT REFERENCE SYNTAX  ~~
                    if (!line.TrimStart().StartsWith("//"))
                    { /// section wrapping
                        //const string tokenTag = "[token]";
                        //string errorMessage;
                        //string token;

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
                [G023]      Missing first 'if' or 'else' keyword

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
                    id = $" {num}";
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
        static string SquishSpaces(this string line)
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
        static string RemovePlainText(this string line, bool invertQ = false)
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
                newLine = newLine.Trim();
            }
            return newLine;
        }
        /// <summary>Removes any plain text fields from a line following a given character.</summary>
        /// <param name="after">The character to hit within line before removing plain text fields. Cannnot be '"'.</param>
        /// <param name="secondAfterQ">If <c>true</c>, will remove plain text fields after the second <paramref name="after"/> character is hit.</param>
        /// <param name="noAfterInPlainQ">If <c>true</c>, will remove any occurences of <paramref name="after"/> within any unfiltered plain text fields.</param>
        static string RemovePlainTextAfter(this string line, char after, bool secondAfterQ = false, bool noAfterInPlainQ = false)
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

        public static void TestSyntaxCheckTools()
        {
            Dbug.StartLogging("Testing Check Syntax Tools");
            for (int x = 0; x < 4; x++)
            {
                string toolName = x switch
                {
                    0 => "Remove Plain Text",
                    1 => "Snippet Text (promoted to 'Extension.cs')",
                    2 => "Squish Spaces",
                    3 => "Remove Plain Text After",
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
