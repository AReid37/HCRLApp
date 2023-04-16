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
                    const string sComment = "//", sEscape = "&00;", sKeyw_if = "if", sKeyw_repeat = "repeat", sKeyw_else = "else";
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

                SYNTAX                :  OUTCOME
                ----------------------:------------------------------------------------------------
                  // text             :  Line comment. Must be placed at the start of the line.
                                      :  Commenting renders a line imparsable.
                ----------------------:------------------------------------------------------------
                  text                :  Code. Anything that is not commented is code and is
                                      :  parsable on steam log generation.
                ----------------------:------------------------------------------------------------
                  "text"              :  Plain text. Represents any text that will be parsed into
                                      :  the generated steam log.
                ----------------------:------------------------------------------------------------
                  &00;                :  Escape character. Used within plain text to print double
                                      :  quote character (").
                ----------------------:------------------------------------------------------------
                  {text}              :  Library reference. References a value based on the
                                      :  information received from a submitted version log.
                                      :    Refer to 'Library Reference' below for more information.
                ----------------------:------------------------------------------------------------
                  $text               :  Steam format reference. References a styling element to
                                      :  use against plain text or another value when generating
                                      :  steam log.
                                      :    Refer to 'Steam Format References' below for more
                                      :  information.
                ----------------------:------------------------------------------------------------
                  if # = #:           :  Keyword. Must be placed at the start of the line.
                                      :    A control command that compares two values for a true or
                                      :  false condition. If the condition is 'true' then the
                                      :  line's remaining data will be parsed into the formatting
                                      :  string.
                                      :    The operator '=' compares two values to be equal. The
                                      :  operator '!=' compares two values to be unequal.
                ----------------------:------------------------------------------------------------
                  else:               :  Keyword. Must be placed at the start of the line. Must be
                                      :  placed following an 'if' keyword line.
                                      :    A control command that will parse the line's remaining
                                      :  data when the condition of a preceding 'if' command is
                                      :  false.
                ----------------------:------------------------------------------------------------
                  repeat #:           :  Keyword. Must be placed at the start of the line.
                                      :    A control command that repeats a line's remaining data
                                      :  '#' number of times. An incrementing number from one to
                                      :  given number '#' will replace any occuring '#' in the
                                      :  line's remaining data.
                ----------------------:------------------------------------------------------------


                # L I B R A R Y   R E F E R E N C E S
                `    Library reference values are provided by the information obtained from the
                ▌    version log submitted for steam log generation.

                SYNTAX                :  OUTCOME
                ----------------------:------------------------------------------------------------
                  {Version}           :  Value. Gets the log version number (ex 1.00).
                ----------------------:------------------------------------------------------------
                  {AddedCount}        :  Value. Gets the number of added item entries available.
                ----------------------:------------------------------------------------------------
                  {Added:#,prop}      :  Value Array. Gets value 'prop' from one-based added entry
                                      :  number '#'.
                                      :    Values for 'prop': ids, name.
                ----------------------:------------------------------------------------------------
                  {AdditCount}        :  Value. Gets the number of additional item entries
                                      :  available.
                ----------------------:------------------------------------------------------------
                  {Addit:#,prop}      :  Value Array. Gets value 'prop' from one-based additional
                                      :  entry number '#'.
                                      :    Values for 'prop': ids, optionalName, relatedContent
                                      :  (related content name), relatedID.
                ----------------------:------------------------------------------------------------
                  {TTA}               :  Value. Gets the number of total textures/contents added.
                ----------------------:------------------------------------------------------------
                  {UpdatedCount}      :  Value. Gets the number of updated item entries available.
                ----------------------:------------------------------------------------------------
                  {Updated:#,prop}    :  Value Array. Gets value 'prop' from one-based updated
                                      :  entry number '#'.
                                      :    Values for 'prop': changeDesc, id, name.
                ----------------------:------------------------------------------------------------
                  {LegendCount}       :  Value. Gets the number of legend entries available.
                ----------------------:------------------------------------------------------------
                  {Legend:#,prop}     :  Value Array. Gets value 'prop' from one-based legend entry
                                      :  number '#'.
                                      :    Values for 'prop': definition, key, keyNum (unique
                                      :  number based on legend key).
                                      :    Using a plain text value for '#' will implicitly convert
                                      :  and replace the text into a 'keyNum' value after an edit.
                ----------------------:------------------------------------------------------------
                  {SummaryCount}      :  Value. Gets the number of summary parts available.
                ----------------------:------------------------------------------------------------
                  {Summary:#}         :  Value Array. Gets the value for one-based summary part
                                      :  number '#'.
                ----------------------:------------------------------------------------------------


                # S T E A M   F O R M A T   R E F E R E N C E S
                `    Steam format references are styling element calls that will affect the look
                ▌    of any text or value placed after it on log generation.
                ▌    Simple command references may be combined with other simple commands unless
                ▌    otherwise unpermitted.
                ▌    Complex commands require a text or value to be placed in a described
                ▌    parameter surrounded by single quote characters (').

                SYNTAX                :  OUTCOME
                ----------------------:------------------------------------------------------------
                  $h                  :  Simple command. Header text. Must be placed at the start
                                      :  of the line. May not be combined with other simple
                                      :  commands.
                                      :    There are three levels of header text. The header level
                                      :  follows the number of 'h's in reference. Example, a level
                                      :  three header text is '$hhh'.
                ----------------------:------------------------------------------------------------
                  $b                  :  Simple command. Bold text.
                ----------------------:------------------------------------------------------------
                  $u                  :  Simple command. Underlined text.
                ----------------------:------------------------------------------------------------
                  $i                  :  Simple command. Italicized text.
                ----------------------:------------------------------------------------------------
                  $s                  :  Simple command. Strikethrough text.
                ----------------------:------------------------------------------------------------
                  $sp                 :  Simple command. Spoiler text.
                ----------------------:------------------------------------------------------------
                  $np                 :  Simple command. No parse. Doesn't parse steam format tags
                                      :  when generating steam log.
                ----------------------:------------------------------------------------------------
                  $c                  :  Simple command. Code text. Fixed width font, preserves
                                      :  space.
                ----------------------:------------------------------------------------------------
                  $hr                 :  Simple command. Horizontal rule. Must be placed on its own
                                      :  line. May not be combined with other simple commands.
                ----------------------:------------------------------------------------------------
                  $nl                 :  Simple command. New line.
                ----------------------:------------------------------------------------------------
                  $url='link':'name'  :  Complex command. Must be placed on its own line.
                                      :    Creates a website link by using URL address 'link' to
                                      :  create a hyperlink text described as 'name'.
                ----------------------:------------------------------------------------------------
                  $list[or]           :  Complex command. Must be placed on its own line.
                                      :    Starts a list block. The optional parameter within
                                      :  square brackets, 'or', will initiate an ordered (numbered)
                                      :  list. Otherwise, an unordered list is initiated.
                ----------------------:------------------------------------------------------------
                  $*                  :  Simple command. Must be placed on its own line.
                                      :    Used within a list block to create a list item. Simple
                                      :  commands may follow to style the list item value or text.
                ----------------------:------------------------------------------------------------
                                      :  Complex command. Must be placed on its own line.
                $q='author':'quote'   :
                                      :    Generates a quote block that will reference an 'author'
                                      :  and display their original text 'quote'.
                ----------------------:------------------------------------------------------------
                  $table[nb,ec]       :  Complex command. Must be placed on its own line.
                                      :    Starts a table block. There are two optional parameters
                                      :  within square brackets: parameter 'nb' will generate a
                                      :  table with no borders, parameter 'ec' will generate a
                                      :  table with equal cells.
                ----------------------:------------------------------------------------------------
                  $th='clm1','clm2'   :  Complex command. Must be placed on its own line.
                                      :    Used within a table block to create a table header row.
                                      :  Separate multiple columns of data with ','. Must follow
                                      :  immediately after a table block has started.
                ----------------------:------------------------------------------------------------
                  $td='clm1','clm2'   :  Complex command. Must be placed on its own line.
                                      :    Used within a table block to create a table data row.
                                      :  Separate multiple columns of data with ','.
                ----------------------:------------------------------------------------------------


                # S Y N T A X   E X C E P T I O N S
                SYNTAX                :  OUTCOME
                ----------------------:------------------------------------------------------------
                  if # = #: $text     :  A (complex) steam format reference may be preceded by any
                                      :  keyword: 'if', 'else' or 'repeat'.
                ----------------------:------------------------------------------------------------
                  else: if # = #:     :  The keyword 'else' may precede the keyword 'if'. This 'if'
                                      :  keyword will trigger a following 'else' keyword line.
                ----------------------:------------------------------------------------------------
                  repeat#: if # = #:  :  The keyword 'repeat' may precede the keyword 'if'. This
                                      :  'if' keyword cannot trigger an 'else' keyword line.
                ----------------------:------------------------------------------------------------
                -- Also : if can follow after if (only once) : triggers else for second 'if'
            

             */

            errors ??= new List<SFormatterInfo>();
            errors.Clear();

            if (lineData.HasElements())
            {
                for (int lx = 0; lx < lineData.Length; lx++)
                {
                    string line = lineData[lx];
                    string noPlainLine = RemovePlainText(line);
                    int lineNum = lx + 1;

                    // ~~  GENERAL SYNTAX  ~~
                    /** GENERAL SYNTAX AND EXCEPTIONS - revised and errors debrief
                        SYNTAX                :  OUTCOME
                        ----------------------:------------------------------------------------------------
                            // text             :  Line comment. Must be placed at the start of the line.
                                                :  Commenting renders a line imparsable.
                        ----------------------:------------------------------------------------------------
                            text                :  Code. Anything that is not commented is code and is
                                                :  parsable on steam log generation.
                        ----------------------:------------------------------------------------------------
                            "text"              :  Plain text. Represents any text that will be parsed into
                                                :  the generated steam log.
                        ----------------------:------------------------------------------------------------
                            &00;                :  Escape character. Used within plain text to print double
                                                :  quote character (").
                        ----------------------:------------------------------------------------------------
                            {text}              :  Library reference. References a value based on the
                                                :  information received from a submitted version log.
                                                :    Refer to 'Library Reference' below for more information.
                        ----------------------:------------------------------------------------------------
                            $text               :  Steam format reference. References a styling element to
                                                :  use against plain text or another value when generating
                                                :  steam log.
                                                :    Refer to 'Steam Format References' below for more
                                                :  information.
                        ----------------------:------------------------------------------------------------
                            if # = #:           :  Keyword. Must be placed at the start of the line.
                                                :    A control command that compares two values for a true or
                                                :  false condition. If the condition is 'true' then the
                                                :  line's remaining data will be parsed into the formatting
                                                :  string.
                                                :    The operator '=' compares two values to be equal. The
                                                :  operator '!=' compares two values to be unequal.
                        ----------------------:------------------------------------------------------------
                            else:               :  Keyword. Must be placed at the start of the line. Must be
                                                :  placed following an 'if' keyword line.
                                                :    A control command that will parse the line's remaining
                                                :  data when the condition of a preceding 'if' command is
                                                :  false.
                        ----------------------:------------------------------------------------------------
                            repeat #:           :  Keyword. Must be placed at the start of the line.
                                                :    A control command that repeats a line's remaining data
                                                :  '#' number of times. An incrementing number from one to
                                                :  given number '#' will replace any occuring '#' in the
                                                :  line's remaining data.
                        ----------------------:------------------------------------------------------------

                    
                    # S Y N T A X   E X C E P T I O N S
                        SYNTAX                :  OUTCOME
                        ----------------------:------------------------------------------------------------
                            if # = #: $text     :  (unapplicable to general syntax checking)
                        ----------------------:------------------------------------------------------------
                            else: if # = #:     :  The keyword 'else' may precede the keyword 'if'. This 'if'
                                                :  keyword will trigger a following 'else' keyword line.
                        ----------------------:------------------------------------------------------------
                            repeat#: if # = #:  :  The keyword 'repeat' may precede the keyword 'if'. This
                                                :  'if' keyword cannot trigger an 'else' keyword line.
                        ----------------------:------------------------------------------------------------
                        -- Also : if can follow after if (only once) : triggers else for second 'if'


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
                            if :    --> Unexpected token ':'
                    
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
                            

                        IF KEYWORD
                        - [G013]     First comparable value expected
                            . Occurs when the first value of the condition is missing or is not a value
                                if          --> First comparable value expected
                                if :        --> First comparable value expected
                                if =        --> First comparable value expected
                        - [G014]     Operator expected
                            . Occurs when the operator following the first value of the condition is missing
                                if "a"      --> Operator expected 
                        - [G015]     Unidentified operator
                            . Occurs when an operator (either '=' or '!=') does not follow after the first value
                                if "a" :    --> Unidentified operator
                        - [G016]     Second comparable value expected
                            . Occurs when the second value of the condition is missing or is not a value
                                if "a" =    --> Second comparable value expected

                        ELSE KEYWORD
                        - [G017]     Missing preceding 'if' control line
                            . Occurs when an else keyword line does not follow immediately after an 'if' or 'else if' line
                                if "a" = "b":
                                "smoke"
                                else:       --> Missing preceding 'if' control line

                        REPEAT KEYWORD
                        - [G018]     First value expected
                            . Occurs when a valid value does not follow after a 'repeat' keyword
                                repeat ""   --> First value expected
                                repeat $h:  --> First value expected
                        - [G019]     First value is an invalid number
                            . Occurs when number that follows after a 'repeat' keyword is less than or equal to '1'
                                repeat -23: --> First value is an invalid number
                                repeat 1:   --> First value is an invalid number
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
                               if :    --> Unexpected token ':'             ****/
                        for (int codeix = 0; codeix < 8; codeix++)
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
                                    if (plainOnly.CountOccuringCharacter('&') != plainOnly.CountOccuringCharacter(';') && plainOnly.Contains(";"))
                                    {
                                        if (plainOnly.CountOccuringCharacter('&') > plainOnly.CountOccuringCharacter(';'))
                                            token = "&"; /// Ex| &;& 
                                        else token = ";"; /// Ex| &;;
                                    }
                                    /// the no plain errors take precedence over plain only errors
                                    if (noPlainLine.Contains("&"))
                                    {
                                        if (noPlainLine.SnippetText("&", ";", false, true).IsNotNE())
                                            token = "&"; /// Ex| &;   |  &  ;
                                        else if (noPlainLine.SnippetText(";", "&", false, true).IsNotNE())
                                            token = ";"; /// Ex| ; &  |  ;&
                                        else token = "&"; /// Ex| &
                                    }
                                    else if (noPlainLine.Contains(";"))
                                        token = ";"; /// Ex| ;
                                    break;

                                /// for library reference 
                                case 3:
                                    if (noPlainLine.CountOccuringCharacter('{') != noPlainLine.CountOccuringCharacter('}'))
                                    {
                                        if (noPlainLine.CountOccuringCharacter('{') > noPlainLine.CountOccuringCharacter('}'))
                                            token = "{";  /// Ex: {l} {
                                        else token = "}"; /// Ex: {l}}
                                    }
                                    else /// IF countChar('{') == countChar('}')
                                    {
                                        if (noPlainLine.CountOccuringCharacter('{') != 0)
                                        {
                                            if (noPlainLine.SnippetText("{", "}", false, true).IsEW())
                                                token = "}"; /// Ex: {  }     why? The library reference expects reference name
                                        }
                                    }
                                    break;

                                /// for steam format reference 
                                case 4:
                                    /// IF has steam char: ...; ELSE IF brackets only: ...;
                                    if (noPlainLine.CountOccuringCharacter('$') != 0)
                                    {
                                        if (noPlainLine.Contains("$ "))
                                            token = "$";  /// Ex:  $ h   -or-   "p" $ "h"
                                        else if (noPlainLine.Contains("[") && noPlainLine.Contains("]"))
                                        {
                                            string boxBracks = noPlainLine.SnippetText("[", "]", true, true);
                                            string noSpaceNoPlain = noPlainLine.SquishSpaces();
                                            if (noSpaceNoPlain.Contains("$[") || noSpaceNoPlain.Contains("[$") || noSpaceNoPlain.Contains("$]") || noSpaceNoPlain.Contains("]$"))
                                                token = boxBracks.IsNotNE() ? "[" : "]";
                                        }
                                        else if (noPlainLine.Contains("[") && noPlainLine.SnippetText("$", "[").IsEW())
                                            token = "["; /// Ex|  $[  |  $ [  |  [$ 
                                        else if (noPlainLine.Contains("]") && noPlainLine.SnippetText("$", "]").IsEW())
                                            token = "]"; /// Ex|  ]$  |  $]   |  $ ]
                                        else if (noPlainLine.EndsWith("$"))
                                            token = "$"; /// Ex:  q $ 
                                    }
                                    else if (noPlainLine.CountOccuringCharacter('[') != 0 || noPlainLine.CountOccuringCharacter(']') != 0)
                                    {
                                        if (noPlainLine.CountOccuringCharacter('[') != 0 && noPlainLine.CountOccuringCharacter(']') == 0)
                                            token = "["; /// Ex| [  |  [[
                                        else if (noPlainLine.CountOccuringCharacter('[') == 0 && noPlainLine.CountOccuringCharacter(']') != 0)
                                            token = "]"; /// Ex| ]  |  ]]
                                        else
                                        {
                                            string boxBracks = noPlainLine.SnippetText("[", "]", true);
                                            if (boxBracks.IsNE())
                                                boxBracks = noPlainLine.SnippetText("]", "[", true);
                                            if (boxBracks.IsNotNE())
                                                token = boxBracks[0].ToString(); /// Ex|  []  |  [s]  |  ][  |  ] kjt [  
                                        }
                                    }
                                    break;

                                /// keyword 'if'
                                case 5:
                                    if (noPlainLine.Contains("if"))
                                    {
                                        if (noPlainLine.SnippetText("if", ":").IsNEW())
                                        {
                                            if (noPlainLine.Contains(':'))
                                                token = ":"; /// Ex:   if    :  -or-   if:   -or-  :if
                                            else token = "if"; /// Ex: if "no"  -or-  "no" if
                                        }                                        
                                    }                                    
                                    break;

                                /// keyword 'else'
                                case 6:
                                    if (noPlainLine.Contains("else"))
                                    {
                                        if (!noPlainLine.Contains("else:"))
                                        {
                                            if (!noPlainLine.Contains(":"))
                                                token = "else";
                                            else
                                            {
                                                string snipElse = noPlainLine.SnippetText("else", ":", true);
                                                token = snipElse.StartsWith("else")? snipElse : ":"; /// Ex| else "no"  |  else "you" :  |  else :  | : else
                                            }

                                        }
                                    }
                                    break;

                                /// keyword 'repeat'
                                case 7:
                                    if (noPlainLine.Contains("repeat"))
                                    {
                                        if (noPlainLine.SnippetText("repeat", ":").IsNEW())
                                        {
                                            if (noPlainLine.Contains(':'))
                                                token = ":"; /// Ex:   repeat:  -or-  repeat  :   -or-   :repeat
                                            else token = "repeat"; /// Ex: repeat "you"  -or-   you "repeat"
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
                                "&01;"  --> Unidentified escape character '&01;'

                         ****/
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
                                        string plainPart = line.SnippetText("\"", line[^1].ToString(), true, true);
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
                                                string possibleEsc = plainLine.SnippetText("&", ";", false, true);
                                                if (possibleEsc.IsNE())
                                                    token = "&;"; /// Ex| "&;"
                                                else if (possibleEsc.IsNEW() || possibleEsc != "00")
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
                                {bunk}  --> Unidentified library reference                         
                         */
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
                                    if (noPlainLine.SnippetText("{", "}", false, true).IsNotNEW())
                                    {
                                        string[] possibleParts = new string[1] { $"{noPlainLine.SnippetText("{", "}", false, true)}}}" };
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
                                            
                                            for (int lrsx = 0; lrsx < libRefsStart.Length && !foundMatchQ; lrsx++)
                                                foundMatchQ = possiblePart.StartsWith(libRefsStart[lrsx]);

                                            if (!foundMatchQ)
                                            {
                                                foundNonStartMatchingQ = true;
                                                token = possiblePart.SnippetText("{", "}", true, true);
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
                                $op     --> Unidentified steam format reference                         
                         */
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
                                        errorMessage = "Empty steam format reference";
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
                                                    string partToken = possibleParts[ppx].SnippetText("$", " ", true, true);
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
                [G012]      Misplaced keyword 'if'
                [G013]      First comparable value expected
                [G014]      Operator expected
                [G015]      Unidentified operator
                [G016]      Second comparable value expected
                [G017]      Missing preceding 'if' control line
                [G018]      First value expected
                [G019]      First value is an invalid number

             */
                        
            
            // METHODS
            /// Surely this doesn't need to feel the open air...
            static void AddToErrorList(int lineNum, string errCode, string errMessage)
            {
                SFormatterInfo error = new(lineNum, errCode, errMessage);
                if (error.IsSetup())
                    errors.Add(error);
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
                    //if (!nPlainQ && line[px] != '"')
                    //    newLine += line[px];

                    if (line[px] != '"')
                        if ((!nPlainQ && !invertQ) || (nPlainQ && invertQ))
                            newLine += line[px];
                }
                newLine = newLine.Trim();
            }
            return newLine;
        }

        public static void TestSyntaxCheckTools()
        {
            Dbug.StartLogging("Testing Check Syntax Tools");
            for (int x = 0; x < 3; x++)
            {
                string toolName = x switch
                {
                    0 => "Remove Plain Text",
                    1 => "Snippet Text (promoted to 'Extension.cs')",
                    2 => "Squish Spaces",
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
                    ///     parameters:  startWith["c"]  endWith["k"/"c"]   include[true/false]     endAfterStart[true/false]   lastOfEnd[true/false]
                    1 => new List<string>()
                    {
                        "c - k",
                        "abcdefghijklmnop",
                        "camp swistenook rock",
                        "kant celp",
                        "ck",
                        "kloop coop knew",
                        "kent cart scopik"
                    },

                    /// squish spaces
                    2 => new List<string>()
                    {
                        "o n e w o r d",
                        "slap: thy face (very hard)!"
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
                            Dbug.LogPart($"[c,k,inc] '{test.SnippetText("c", "k", true)}'  |  ");
                            Dbug.LogPart($"[c,k,exc] '{test.SnippetText("c", "k", false)}'  |  ");
                            Dbug.LogPart($"[c,k,inc,endAft] '{test.SnippetText("c", "k", true, true)}'  |  ");
                            Dbug.LogPart($"[c,k,exc,endAft] '{test.SnippetText("c", "k", false, true)}'  |  ");
                            Dbug.LogPart($"[c,c,inc,endAft] '{test.SnippetText("c", "c", true, true)}'  |  ");
                            Dbug.LogPart($"[c,k,inc,endAft,lastEnd] '{test.SnippetText("c", "k", true, true, true)}'");
                            break;

                        case 2:
                            Dbug.LogPart($"'{SquishSpaces(test)}'");
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
