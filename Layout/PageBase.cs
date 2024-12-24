using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;
using System.Diagnostics;

namespace HCResourceLibraryApp.Layout
{
    public static class PageBase
    {
        /* WHAT SHOULD PAGE BASE INCLUDE?
           Special.unicode.characters
            > public fields for the special unicode characters:
                (3) Light/Medium/Dark Shade
                (4) Right/Left/Top/Bottom Half Block
          
           Text.formatting
            > Format(text, formatType) 
            > Highlight(str text, params str highlightedText)
            > ChooseColorMenu(...) [number-based]
                >> (str titleText)
                << (ret bl valid // out Col color)
                
            
           Menu.builds
            > Table Form Menu(...) [number-based]
                >> (str titleText, chr titleUnderline, bool titleSpanWidthQ, str prompt, str placeholder, shr optionColumns, params str options) 
                << (ret bl valid // out shr resultNum)
            > List Form Menu(...) [string-based]
                >> (str titleText, chr titleUnderline, str prompt, str placeholder, bl indentOptsQ, params str options)
                << (ret bl valid // out str resultKey)
            > Navigation Bar(...)
        */

        #region fields / props
        // PRIVATE \ PROTECTED
        static Preferences _preferencesRef;
        const string FormatUsageKey = "`", WordWrapUsageKey = "▌";
        const string WordWrapNewLineKey = "▌\\W/▐"; // ▌\W/▐
        const int WordWrapIndentLim = 8, wrapUnholdNum = -1; // equivalent of one '\t'
        static int _wrapIndentHold = wrapUnholdNum, _wrapSource = 0, _cursorLeft, _cursorTop;
        const char DefaultTitleUnderline = cTHB;
        static string _menuMessage, _incorrectionMessage;
        static bool _isMenuMessageInQueue, _isWarningMenuMessageQ, _enableWordWrapQ = true, _holdWrapIndentQ, _enterBugIdeaPageQ, _enterFileChooserPageQ, _allowFileChooserPageQ;
        static ForECol? _normalAlt, _highlightAlt;
        static bool _enableBarQ, _showPercentQ, _hideNodeQ;
        static int _barWidth, _barPosLeft, _barPosTop, _iniCursorLeft, _iniCursorTop, _taskNum;
        static float _taskCount;
        static ForECol _barCol, _barNodeCol;


        // PUBLIC
        public const int PageSizeLimit = 725; /// OG"500" 
        /// <summary>Light Shade (░).</summary>
        public const char cLS = '\x2591';
        /// <summary>Medium Shade (▒).</summary>
        public const char cMS = '\x2592';
        /// <summary>Dark Shade (▓).</summary>
        public const char cDS = '\x2593';
        /// <summary>Right Half Block (▐).</summary>
        public const char cRHB = '\x2590';
        /// <summary>Left Half Block (▌).</summary>
        public const char cLHB = '\x258c';
        /// <summary>Top Half Block (▀).</summary>
        public const char cTHB = '\x2580';
        /// <summary>Bottom Half Block (▄).</summary>
        public const char cBHB = '\x2584';
        public const string exitPagePhrase = "Return to Main Menu";
        public const string exitSubPagePhrase = "Return";
        public const string openBugIdeaPagePhrase = "@dbi";
        public const string openFileChooserPhrase = "@bws";
        public static float TaskCount
        {
            get => _taskCount;
            set => _taskCount = (int)value > 0 ? (int)value : 1;
        }
        public static int TaskNum
        {
            get => _taskNum;
            set => _taskNum = value;
        }

        // PROPERTIES
        public static bool VerifyFormatUsage { get; set; }
        public static bool WithinBugIdeaPageQ { get; set; }
        #endregion


        // -- Text Formatting --
        public static void GetPreferencesReference(Preferences preference)
        {
            _preferencesRef = preference;
        }

        #region Suppressant for Window Size and Buffer Size Edits to Console
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        #endregion
        public static void ApplyPreferences()
        {
            if (_preferencesRef != null)
            {
                // window dimensions customization
                /// tbd...
                _preferencesRef.GetScreenDimensions(out int tHeight, out int tWidth);
                Console.SetWindowSize(tWidth, tHeight);
                //Console.SetBufferSize(tWidth, 9001); /// this, while the library search page is in WIP version
                Console.SetBufferSize(tWidth, PageSizeLimit);

                // minimal customization
                ClearMinimalCustomization(MinimalMethod.All);
                CustomizeMinimal(MinimalMethod.List, _preferencesRef.Normal, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.Important, _preferencesRef.Heading2, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.Table, _preferencesRef.Normal, _preferencesRef.Normal);
                CustomizeMinimal(MinimalMethod.Title, _preferencesRef.Heading1, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.HorizontalRule, _preferencesRef.Accent, _preferencesRef.Accent);
            }
        }
        public static Color GetPrefsForeColor(ForECol fftype)
        {
            Preferences prefs = _preferencesRef == null ? new Preferences() : _preferencesRef;
            Color col;
            switch (fftype)
            {
                case ForECol.Normal:
                    col = prefs.Normal;
                    break;

                case ForECol.Highlight:
                    col = prefs.Highlight;
                    break;

                case ForECol.Accent:
                    col = prefs.Accent;
                    break;

                case ForECol.Correction:
                    col = prefs.Correction;
                    break;

                case ForECol.Incorrection:
                    col = prefs.Incorrection;
                    break;

                case ForECol.Warning:
                    col = prefs.Warning;
                    break;

                case ForECol.Heading1:
                    col = prefs.Heading1;
                    break;

                case ForECol.Heading2:
                    col = prefs.Heading2;
                    break;

                case ForECol.InputColor:
                    col = prefs.Input;
                    break;

                default:
                    col = Color.Black;
                    break;
            }
            return col;
        }
        public static void Format(string text, ForECol foreElementCol = ForECol.Normal)
        {
            if (text.IsNotNE())
            {
                _wrapSource = 0;
                text = WordWrap(text, _holdWrapIndentQ);
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);                    
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                    if (text.Contains(WordWrapNewLineKey))
                        text = text.Replace(WordWrapNewLineKey, $"\n{WordWrapUsageKey}");
                }
                Text(text, GetPrefsForeColor(foreElementCol));
            }
        }
        public static void FormatLine(string text, ForECol foreElementCol = ForECol.Normal)
        {
            if (text.IsNotNE())
            {
                _wrapSource = 1;
                text = WordWrap(text, _holdWrapIndentQ);
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);                    
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                    if (text.Contains(WordWrapNewLineKey))
                        text = text.Replace(WordWrapNewLineKey, $"\n{WordWrapUsageKey}");
                }
                TextLine(text, GetPrefsForeColor(foreElementCol));
            }
        }
        /// <summary>Specifically for <see cref="SFormatterHandler"/> methods. Will retain the indentation level across multiple prints.</summary>
        public static void Format(string text, Color color, bool newLine = false)
        {
            if (text.IsNotNE())
            {
                _wrapSource = 2;
                text = WordWrap(text, true);
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                    if (text.Contains(WordWrapNewLineKey))
                        text = text.Replace(WordWrapNewLineKey, $"\n{WordWrapUsageKey}");
                }
                if (newLine)
                    TextLine(text, color);
                else Text(text, color);
            }
        }
        // public for testing purposes
        public static string WordWrap(string text, bool holdIndentQ = false)
        {
            /// from 'left' to 'right - 1' char spaces
            if (text.IsNotNE() && _enableWordWrapQ)
            {
                Dbg.StartLogging("PageBase.WordWrap()", out int pgbsx);
                Dbg.ToggleThreadOutputOmission(pgbsx);
                Dbg.LogPart(pgbsx, $"Source :: {(_wrapSource == 0 ? "Format()" : (_wrapSource == 1 ? "FormatLine()" : "overload Format()"))}  //  ");
                Dbg.Log(pgbsx, $"Received text :: {text.Replace("\n", "\\n").Replace("\t", "\\t")}");

                // -- First determine if word requires wrapping --
                int wrapBuffer = Console.BufferWidth - 1;
                int wrapStartPos = Console.CursorLeft;
                const string newLineReplace = "▌nL ", tabReplace = "        ";

                // wrap indent holding, for SFormatterHandler multilines
                string copyText = text.Replace("\n", newLineReplace).Replace("\t", tabReplace);
                if (copyText.Length > WordWrapIndentLim + 2)
                    copyText = copyText.Remove(WordWrapIndentLim + 1);
                if (holdIndentQ && _wrapIndentHold == wrapUnholdNum)
                {
                    string trimmedCopyText = (copyText.TrimStart().IsNE() ? "" : copyText.TrimStart());
                    int spaceIndent = copyText.Length - trimmedCopyText.Length;

                    _wrapIndentHold = (spaceIndent == 0 /*&& wrapStartPos.IsWithin(0, WordWrapIndentLim)*/ ? wrapStartPos : spaceIndent).Clamp(0, WordWrapIndentLim);
                    //_wrapIndentHold = Extensions.CountOccuringCharacter(copyText, ' ').Clamp(0, WordWrapIndentLim);
                    Dbg.Log(pgbsx, $"Set and held a wrapping indent position :: {_wrapIndentHold}; ");
                }
                else if (!holdIndentQ && _wrapIndentHold != wrapUnholdNum)
                {
                    Dbg.Log(pgbsx, $"Released held wrapping indent position :: {_wrapIndentHold}; ");
                    _wrapIndentHold = wrapUnholdNum;
                }


                // -- If requires wrapping, WRAP! --
                if (wrapBuffer - wrapStartPos <= text.Length || text.Contains("\n"))
                {
                    // -- Prep text to wrap --
                    text = text.Replace("\n", newLineReplace).Replace("\t", tabReplace);
                    Dbg.LogPart(pgbsx, $"Wrapping Text: {wrapBuffer} spaces starting at '{wrapStartPos}'; ");
                    Dbg.Log(pgbsx, $"Replaced any newline escape characters (\\n) with '{newLineReplace}'; Replaced any 'tab' escape characters (\\t) with 8 spaces; ");


                    // -- Separate words into bits --
                    List<string> textsToWrap = new();
                    string partTexts = "";
                    bool wasSpaceCharQ = false;
                    Dbg.Log(pgbsx, "Separating text into wrappable pieces :: ");
                    Dbg.NudgeIndent(pgbsx, true);
                    Dbg.LogPart(pgbsx, $" >|");
                    for (int tx = 0; tx < text.Length; tx++)
                    {
                        char c = text[tx];
                        bool isStartQ = tx == 0, isEndQ = tx + 1 == text.Length;
                        bool isSpaceCharQ = c == ' ';

                        if (!isStartQ)
                        {
                            if ((wasSpaceCharQ && !isSpaceCharQ) || isEndQ)
                            {
                                if (isEndQ)
                                    partTexts += c.ToString();

                                textsToWrap.Add(partTexts);
                                Dbg.LogPart(pgbsx, $"{partTexts}{(isEndQ ? "" : "|")}");
                                partTexts = "";
                            }
                        }
                        partTexts += c.ToString();
                        wasSpaceCharQ = isSpaceCharQ;
                    }
                    Dbg.Log(pgbsx, $"|< ");
                    Dbg.NudgeIndent(pgbsx, false);


                    // -- It's time to WRAP! --
                    Dbg.Log(pgbsx, "Words have been separated: proceeding to wrap words; ");
                    if (textsToWrap.HasElements())
                    { /// wrapping ... as to hide within...

                        string wrappedText = "";
                        int currWrapPos = wrapStartPos;
                        int wrapIndentLevel = 0;
                        //Dbg.Log(pgbsx, $"Extra info :: currWrapPos = {wrapStartPos}, helpWrapPos = {_wrapIndentHold};");

                        Dbg.NudgeIndent(pgbsx, true);
                        Dbg.LogPart(pgbsx, "> :|");
                        for (int wx = 0; wx < textsToWrap.Count; wx++)
                        {
                            bool isStartQ = wx == 0, isEndQ = wx + 1 == textsToWrap.Count;
                            string wText = textsToWrap[wx];

                            /// determine wrapping indent level here
                            if (isStartQ && currWrapPos < WordWrapIndentLim && _wrapIndentHold == wrapUnholdNum)
                            {
                                if (wText.Replace(" ", "").IsNE())
                                {
                                    wrapIndentLevel = Extensions.CountOccuringCharacter(wText, ' ').Clamp(0, WordWrapIndentLim);
                                    Dbg.LogPart(pgbsx, $" [Ind{wrapIndentLevel}8] ");
                                }
                                /// this won't run anyway, I'm sure
                                //else
                                //{
                                //    wrapIndentLevel = currWrapPos.Clamp(0, WordWrapIndentLim);
                                //    Dbg.LogPart(pgbsx, $" [LimInd{wrapIndentLevel}8] ");
                                //}
                            }
                            else if (isStartQ && _wrapIndentHold != wrapUnholdNum)
                            {
                                wrapIndentLevel = _wrapIndentHold.Clamp(0, WordWrapIndentLim);
                                Dbg.LogPart(pgbsx, $" [HldInd{wrapIndentLevel}8] ");
                            }


                            /// IF printing this text will not exceed buffer width AND text to print is not newline: print text; ELSE...
                            if (currWrapPos + wText.Length < wrapBuffer && !wText.Contains(newLineReplace))
                            {
                                wrappedText += wText;
                                Dbg.LogPart(pgbsx, $"{wText}");
                            }
                            /// IF ...; ELSE wrap text to next line OR print newline
                            else
                            {
                                string wrapIndText = "";
                                if (wrapIndentLevel > 0)
                                {
                                    for (int wix = 0; wix < wrapIndentLevel; wix++)
                                        wrapIndText += " ";
                                }

                                /// IF text is not newline (...); ELSE print newline
                                if (!wText.Contains(newLineReplace))
                                {
                                    /// IF text fits within buffer width: Normal word wrap; ELSE wrap and break word
                                    if (wText.Length < wrapBuffer)
                                    {
                                        bool justFitsQ = wText.Length + currWrapPos == wrapBuffer && isEndQ;

                                        if (!justFitsQ)
                                            wText = wrapIndText + wText;

                                        currWrapPos = 0;
                                        if (!justFitsQ)
                                        {
                                            if (VerifyFormatUsage)
                                                wrappedText += $"{WordWrapNewLineKey}{wText}";
                                            else wrappedText += $"\n{wText}";

                                            Dbg.Log(pgbsx, "|-> ");
                                            Dbg.LogPart(pgbsx, $" ->|{wText}");
                                        }
                                        else
                                        {
                                            wrappedText += wText;

                                            Dbg.LogPart(pgbsx, $"|{wText}|-- ");
                                        }
                                    }
                                    else
                                    {
                                        int remainingSpace = wrapBuffer - currWrapPos;
                                        int breakNWrapCount = ((wText.Length - remainingSpace) / (wrapBuffer - wrapIndentLevel)) + 1;
                                        for (int wlx = 0; wlx <= breakNWrapCount; wlx++)
                                        {
                                            bool isRemainderQ = wlx == 0, isBreakEndQ = false;
                                            int wTSubIndex = isRemainderQ ? 0 : remainingSpace + ((wlx - 1) * wrapBuffer) - ((wlx - 1) * wrapIndentLevel);
                                            int wTSubLen = isRemainderQ ? remainingSpace : wrapBuffer - wrapIndentLevel;
                                            string wTSubText = isRemainderQ ? "" : wrapIndText;

                                            /// IF text length is greater than splitIndex: 
                                            ///     (IF text after splitIndex is under splitLength: get remaining text; ELSE substring text from splitIndex at splitLength length);
                                            if (wText.Length > wTSubIndex)
                                            {
                                                if (wText.Length - wTSubIndex - 1 < wTSubLen)
                                                {
                                                    isBreakEndQ = true;
                                                    wTSubText += wText.Substring(wTSubIndex);
                                                    /// for debug vv
                                                    wTSubLen = wText.Length - wTSubIndex - 1;
                                                }
                                                else wTSubText += wText.Substring(wTSubIndex, wTSubLen);
                                            }

                                            /// IF obtained subText: 
                                            ///     (IF last break: print last of text and continue regular wrapping;
                                            ///     ELSE (IF first break: print remainder of text and continue wrap-n-break; ELSE print part of text and continue wrap-n-break) )
                                            if (wTSubText.IsNotNE())
                                            {
                                                string subTextIxRange = $"[ix{wTSubIndex}~{wTSubIndex + wTSubLen - 1}]";
                                                if (isBreakEndQ)
                                                {
                                                    Dbg.LogPart(pgbsx, $" =>|{subTextIxRange}{wTSubText}");
                                                    if (VerifyFormatUsage)
                                                        wrappedText += $"{WordWrapNewLineKey}{wTSubText}";
                                                    else wrappedText += $"\n{wTSubText}";

                                                    currWrapPos = wTSubLen;
                                                    wText = "";
                                                }
                                                else
                                                {
                                                    if (isRemainderQ)
                                                    {
                                                        Dbg.Log(pgbsx, $"{subTextIxRange}{wTSubText}|=> ");
                                                        wrappedText += $"{wTSubText}";
                                                    }
                                                    else
                                                    {
                                                        Dbg.Log(pgbsx, $" =>|{subTextIxRange}{wTSubText}|=> ");
                                                        if (VerifyFormatUsage)
                                                            wrappedText += $"{WordWrapNewLineKey}{wTSubText}";
                                                        else wrappedText += $"\n{wTSubText}";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    /// IF text to wrap is only newline: simulate newline; ELSE simulate newline and print remaining texts
                                    if (wText.Equals(newLineReplace))
                                    {
                                        currWrapPos = 0;
                                        wText = wrapIndText;

                                        Dbg.Log(pgbsx, "|>> ");
                                        Dbg.LogPart(pgbsx, $" >>|{wText}");
                                        if (VerifyFormatUsage)
                                            wrappedText += $"{WordWrapNewLineKey}{wText}";
                                        else wrappedText += $"\n{wText}";
                                    }
                                    else //if (wText.EndsWith(newLineReplace))
                                    {
                                        currWrapPos = 0;
                                        string fltWText = wText.Replace(newLineReplace, "");
                                        Dbg.Log(pgbsx, $"{fltWText}|<> ");

                                        wText = wrapIndText;
                                        if (fltWText.IsEW() && fltWText.CountOccuringCharacter(' ') > 0)
                                            wText += fltWText;
                                        Dbg.LogPart(pgbsx, $" <>|{wText}");

                                        if (VerifyFormatUsage)
                                            wrappedText += $"{fltWText}{WordWrapNewLineKey}{wText}";
                                        else wrappedText += $"{fltWText}\n{wText}";
                                    }
                                }
                            }
                            if (!isEndQ)
                                Dbg.LogPart(pgbsx, "|");
                            currWrapPos += wText.Length;
                        }
                        Dbg.Log(pgbsx, "|: <");
                        Dbg.NudgeIndent(pgbsx, false);

                        // return wrapped text
                        Dbg.Log(pgbsx, $"Finshed wrapping text :: {wrappedText.Replace("\n", $"{newLineReplace.Trim()}{cRHB}")}");
                        text = wrappedText;

                        Dbg.Log(pgbsx, "LEGEND ///  Word divider  |  //  Start|End  > :|: <  //  Wrap  ->  (Just Fits  --)  //  Break'N'Wrap  =>  //  NewLine  >>  (with word  <>)  /// END LEGEND");
                    }
                }
                else Dbg.Log(pgbsx, $"This text does not require wrapping: '{wrapBuffer - wrapStartPos - text.Length}' character spaces remain after printing this text.");

                Dbg.EndLogging(pgbsx);
            }
            return text;
        }
        /// <param name="holdQ">Default value: <c>false</c>.</param>
        public static void HoldWrapIndent(bool holdQ)
        {
            _holdWrapIndentQ = holdQ;
        }
        public static void ToggleWordWrappingFeature(bool? enabledQ = null)
        {
            if (enabledQ.HasValue)
                _enableWordWrapQ = enabledQ.Value;
            else
            {
                _enableWordWrapQ = !_enableWordWrapQ;
            }
        }
        /// <summary>
        ///     Highlights words or phrases within a larger text.
        /// </summary>
        /// <param name="newLine">Add new at the end of text of highlights?</param>
        /// <param name="text">The text.</param>
        /// <param name="highlightedTexts">Cannot be null. Characters, words, or phrases within the text to highlight.</param>
        public static void Highlight(bool newLine, string text, params string[] highlightedTexts)
        {
            Dbg.StartLogging("PageBase.Highlight()", out int pgbsx);
            Dbg.ToggleThreadOutputOmission(pgbsx);

            Dbg.Log(pgbsx, $"Recieved --> text (\"{text}\"); highlightedTexts (has elements? {highlightedTexts.HasElements()})");
            if (text.IsNotNEW() && highlightedTexts.HasElements())
            {
                short highlightIndex = 0;
                foreach (string highText in highlightedTexts)
                {
                    Dbg.LogPart(pgbsx, $".  |highlightedText (@ index #{highlightIndex}) --> ");
                    if (highText.IsNotNE())
                    {
                        Dbg.LogPart(pgbsx, $"{highText}  //  ");
                        if (text.Contains(highText))
                        {
                            text = text.Replace(highText, $"<{highlightIndex}>");                            
                            Dbg.Log(pgbsx, $"result text --> \"{text}\"");
                        }
                        else Dbg.Log(pgbsx, "no change.");
                    }
                    else Dbg.Log(pgbsx, "null or empty highlightedText~");
                    highlightIndex++;
                }

                string[] textWords = null;
                if (textWords == null)
                { /// wrapping
                    //Dbg.LogPart(pgbsx, "Compiling words ::  ");
                    List<string> testWordsCompile = new();
                    string wordBuild = "";
                    bool readingSpaces = false;
                    foreach (char character in text)
                    {
                        if (character != ' ' && readingSpaces)
                        {
                            testWordsCompile.Add(wordBuild);
                            wordBuild = character.ToString();
                            readingSpaces = false;
                        }
                        else
                        {
                            wordBuild += character;
                            readingSpaces = character == ' ';
                        }
                    }
                    /// get the last word in
                    if (wordBuild.IsNotNE())
                        testWordsCompile.Add(wordBuild);
                    //Dbg.Log(pgbsx, "  --> Done");

                    textWords = testWordsCompile.ToArray();
                }


                /// set custom colors if possible
                ForECol fecNormal = ForECol.Normal, fecHighlight = ForECol.Highlight;
                if (_normalAlt.HasValue || _highlightAlt.HasValue)
                {
                    if (_normalAlt.HasValue)
                        fecNormal = _normalAlt.Value;
                    if (_highlightAlt.HasValue)
                        fecHighlight = _highlightAlt.Value;

                    _normalAlt = null;
                    _highlightAlt = null;
                }

                /// print and highlight words
                for (int c = 0; c < textWords.Length; c++)
                {
                    Dbg.LogPart(pgbsx, $".. |");

                    string word = textWords[c];
                    bool end = c + 1 == textWords.Length;
                    Dbg.LogPart(pgbsx, $"assessing word '{word}' || ");
                    if (word.IsNotNE())
                    {
                        // IF word is or contains subkey, check for matching highlight word or phrase and replace subkey with it
                        if (word.Contains('<') && word.Contains('>'))
                        {
                            Dbg.LogPart(pgbsx, "format highlight word >> ");
                            for (int hli = 0; hli < highlightedTexts.Length; hli++)
                            {
                                string subKey = $"<{hli}>";
                                string replacePhrase = highlightedTexts[hli];                                
                                if (word.Contains(subKey))
                                {
                                    Dbg.LogPart(pgbsx, $"Replace '{subKey}' with '{replacePhrase}'  //  result -->  ");
                                    // <0> == <0>?
                                    if (word == subKey)
                                    {
                                        if (!end || !newLine)
                                            Format(replacePhrase, fecHighlight);
                                        else FormatLine(replacePhrase, fecHighlight);
                                        Dbg.LogPart(pgbsx, $"'[{replacePhrase}]'");
                                    }

                                    // <0>, == <0>?
                                    else
                                    {
                                        Dbg.LogPart(pgbsx, "'");

                                        string[] splitWord = word.Split(subKey);
                                        for (int wx = 0; wx < splitWord.Length; wx++)
                                        {
                                            bool wordEndQ = splitWord.Length == wx + 1;

                                            string partWord = splitWord[wx];
                                            if (partWord.IsNotNE())
                                            {
                                                Format(partWord, fecNormal);
                                                Dbg.LogPart(pgbsx, partWord);
                                            }

                                            if (!wordEndQ)
                                            {
                                                Format(replacePhrase, fecHighlight);
                                                Dbg.LogPart(pgbsx, $"[{replacePhrase}]");
                                            }

                                            if (wordEndQ)
                                                if (newLine && end)
                                                    NewLine();
                                        }
                                        Dbg.LogPart(pgbsx, "'");
                                    }
                                }
                            }
                            Dbg.Log(pgbsx, $"  // (on newline? {end && newLine})");
                        }

                        else
                        {
                            if (!end || !newLine)
                                Format(word, fecNormal);
                            else FormatLine(word, fecNormal);

                            Dbg.Log(pgbsx, $"format normal word >> '{word}' (on newline? {end && newLine})");
                        }

                    }
                    else Dbg.Log(pgbsx, "null or empty word~");
                }
            }


            Dbg.EndLogging(pgbsx);
        }
        /// <summary>Temporarily changes the foreground element colors used on the next call to <see cref="Highlight(bool, string, string[])"/></summary>
        public static void ChangeNextHighlightColors(ForECol normalAlt, ForECol highlightAlt)
        {
            _normalAlt = normalAlt;
            _highlightAlt = highlightAlt;
        }
        /// <summary>Input placeholder character limit: 50 characters. <br></br>Placeholder color cannot be changed.</summary>
        public static string StyledInput(string placeholder)
        {
            /// setup file chooser page
            if (!IsEnterFileChooserPageQueued() && _allowFileChooserPageQ)
                FileChooserPage.SetInitialCursorPos();

            string input = Input(placeholder, _preferencesRef.Input);
            Wait(0.1f); // this may help with the accidental skips...

            /// queue bug / idea page
            if (input == openBugIdeaPagePhrase && !IsEnterBugIdeaPageQueued())
            {
                QueueEnterBugIdeaPage();
                Format($"{Ind34}[Bug/Idea Page Queued] Enter input for previous prompt: ", ForECol.Accent);
                input = Input(placeholder, _preferencesRef.Input);
            }
            /// queue file chooser page
            if (input == openFileChooserPhrase && !IsEnterFileChooserPageQueued() && _allowFileChooserPageQ)
            {
                QueueEnterFileChooserPage();
            }
            return input;
        }
        /// <summary>Height Sensitive New Line.</summary>
        /// <returns>A newline number between a given range dependent on Height Scale of console window.</returns>
        public static int HSNL(int minimumNL, int maximumNL)
        {
            int numOfNewLines = 0;
            if (minimumNL <= maximumNL && _preferencesRef != null)
            {
                float range = maximumNL - minimumNL;
                /// x -> variable prefs height factor
                /// n -> minimum prefs height factor
                /// m -> maximum prefs height factor
                /// rngFac --> range factor (must be value between 0 and 1)
                /// 
                /// Formula synthesis
                ///     > m - n = fixed factor range (denominator)
                ///     > x - n = variable range (numerator)
                ///     > rngFac = (x - n) / (m - n)
                float rangeFactor = (_preferencesRef.HeightScale.GetScaleFactorH() - DimHeight.Squished.GetScaleFactorH()) / (DimHeight.Fill.GetScaleFactorH() - DimHeight.Squished.GetScaleFactorH());
                numOfNewLines = minimumNL + (int)(rangeFactor * range);
                //Dbg.SingleLog("HomePage.HSNL(int, int)", $"Got min|max values ({minimumNL}|{maximumNL}) and range ({range}); Solved for range factor ({rangeFactor * 100:0.0}%); Returned sensitive number of newlines: {numOfNewLines};");
            }
            return numOfNewLines;
        }
        /// <summary>Height Sensitive New Line Print. 
        /// Retrieves newline number between a given range dependent on Height Scale of application window and prints those newlines to console.</summary>
        public static void HSNLPrint(int minimumNL, int maximumNL)
        {
            if (minimumNL <= maximumNL && _preferencesRef != null)
            {
                float range = maximumNL - minimumNL;
                /// x -> variable prefs height factor
                /// n -> minimum prefs height factor
                /// m -> maximum prefs height factor
                /// rngFac --> range factor (must be value between 0 and 1)
                /// 
                /// Formula synthesis
                ///     > m - n = fixed factor range (denominator)
                ///     > x - n = variable range (numerator)
                ///     > rngFac = (x - n) / (m - n)
                float rangeFactor = (_preferencesRef.HeightScale.GetScaleFactorH() - DimHeight.Squished.GetScaleFactorH()) / (DimHeight.Fill.GetScaleFactorH() - DimHeight.Squished.GetScaleFactorH());
                int numOfNewLines = minimumNL + (int)(rangeFactor * range);
                //Dbg.SingleLog("HomePage.HSNLPrint(int, int)", $"Got min|max values ({minimumNL}|{maximumNL}) and range ({range}); Solved for range factor ({rangeFactor * 100:0.0}%); Returned sensitive number of newlines: {numOfNewLines};");

                NewLine(numOfNewLines);
            }
        }
        /// <summary>Width Sensitive Line Length.</summary>
        /// <returns>A line length number between a given range dependent on Width Scale of console window.</returns>
        public static int WSLL(int minimumLL, int maximumLL)
        {
            int lineLengthNum = 0;
            if (minimumLL <= maximumLL && _preferencesRef != null)
            {
                float range = maximumLL - minimumLL;
                /// x -> variable prefs width factor
                /// n -> minimum prefs width factor
                /// m -> maximum prefs width factor
                /// rngFac --> range factor (must be value between 0 and 1)
                /// 
                /// Formula synthesis
                ///     > m - n = fixed factor range (denominator)
                ///     > x - n = variable range (numerator)
                ///     > rngFac = (x - n) / (m - n)
                float rangeFactor = (_preferencesRef.WidthScale.GetScaleFactorW() - DimWidth.Thin.GetScaleFactorW()) / (DimWidth.Fill.GetScaleFactorW() - DimWidth.Thin.GetScaleFactorW());
                lineLengthNum = minimumLL + (int)(rangeFactor * range);
            }
            return lineLengthNum;
        }



        // -- -- Validations -- --
        /// <summary>Queues an incorrection message for input validations</summary>
        /// <param name="message">If <c>null</c>, will clear incorrection messages.</param>
        public static void IncorrectionMessageQueue(string message)
        {
            if (message.IsNEW())
                _incorrectionMessage = null;
            else _incorrectionMessage = message;
        }
        /// <summary>Triggers a queued incorrection message for input validation. Also includes a Pause().</summary>
        /// <param name="pretext">Precedes the incorrection message once given a value.</param>
        /// <param name="postText">Follows the incorrection message once given a value.</param>
        public static void IncorrectionMessageTrigger(string pretext, string postText)
        {
            if (_incorrectionMessage.IsNotNEW())
            {
                //if (pretext.IsNotNEW())
                //    Format($"{pretext}{_incorrectionMessage}", ForECol.Incorrection);
                //else Format(_incorrectionMessage, ForECol.Incorrection);
                Format($"{pretext}{_incorrectionMessage}{postText}", ForECol.Incorrection);
                Pause();
            }
        }
        /// <summary>A confirmation prompt for yes and no</summary>
        /// <param name="prompt"></param>
        /// <param name="yesNo">Is <c>true</c> if "yes" to prompt, and is <c>false</c> if "no" to prompt.</param>
        /// <param name="yesMsg">A confirmation message following after user's "yes" input.</param>
        /// <param name="noMsg">A confirmation message following after user's "no" input.</param>
        /// <param name="longPlaceholder">If <c>true</c>, will use "yes/no" as the placeholder instead of "y/n".</param>
        /// <returns>A bool stating whether the recieved input was valid.</returns>
        public static bool Confirmation(string prompt, bool longPlaceholder, out bool yesNo, string yesMsg, string noMsg)
        {
            bool validInput = false;
            yesNo = false;

            if (prompt.IsNEW())
                prompt = $"{Ind24}Are you certain of your choice?";

            Format($"{prompt}", ForECol.Warning);
            string input = StyledInput(!longPlaceholder ? "y/n" : "yes/no");

            if (input.IsNotNEW())
            {
                input = input.ToLower();
                if (input.Equals("yes") || input.Equals("y"))
                {
                    yesNo = true;
                    validInput = true;
                }
                if (input.Equals("no") || input.Equals("n"))
                {
                    yesNo = false;
                    validInput = true;
                }
            }

            if (validInput && yesMsg.IsNotNEW() && noMsg.IsNotNEW())
            {
                Format(yesNo ? yesMsg : noMsg, yesNo ? ForECol.Correction : ForECol.Incorrection);
                Pause();
            }

            return validInput;
        }
        /// <summary>A confirmation prompt for yes and no</summary>
        /// <param name="prompt"></param>
        /// <param name="yesNo">Is <c>true</c> if "yes" to prompt, and is <c>false</c> if "no" to prompt.</param>
        /// <param name="longPlaceholder">If <c>true</c>, will use "yes/no" as the placeholder instead of "y/n".</param>
        /// <returns>A bool stating whether the recieved input was valid.</returns>
        public static bool Confirmation(string prompt, bool longPlaceholder, out bool yesNo)
        {
            bool validInput = Confirmation(prompt, longPlaceholder, out yesNo, null, null);
            return validInput;
        }
        public static void ConfirmationResult(bool yesNoCondition, string pretext, string yesMsg, string noMsg)
        {
            if (yesMsg.IsNEW())
                yesMsg = $"{Ind34}Program action confirmed.";
            if (noMsg.IsNEW())
                noMsg = $"{Ind34}Program action denied.";

            Format($"{pretext}{(yesNoCondition ? yesMsg : noMsg)}", yesNoCondition ? ForECol.Correction : ForECol.Incorrection);
            Pause();
        }




        // -- Menu Builds --
        /// <summary>
        ///     Creates an options menu in the form of a table. Includes menu validation.
        /// </summary>
        /// <param name="resultNum">One-based numerical result key matching the bracketed number preceding a selected option. Is <c>-1</c> if the selected option is invalid.</param>
        /// <param name="titleText">REQUIRED.</param>
        /// <param name="titleUnderline">If <c>null</c>, will use the <see cref="cTHB"/> ('▀') as an underline.</param>
        /// <param name="titleSpanWidthQ">If <c>true</c>, will have the title of the menu span the width of the program window.</param>
        /// <param name="prompt">Prompt line to urge selection of an option. If <c>null</c>, will use a default prompt.</param>
        /// <param name="placeholder">Pretext denoting the desired input or ushering for an input. If <c>null</c>, will use a default placeholder. Limit of 50 characters.</param>
        /// <param name="optionColumns">Determines how many columns the table will be divided into for the menu options. Range [2, 4].</param>
        /// <param name="options">REQUIRED.</param>
        /// <returns></returns>
        public static bool TableFormMenu(out short resultNum, string titleText, char? titleUnderline, bool titleSpanWidthQ, string prompt, string placeholder, short optionColumns, params string[] options)
        {
            resultNum = 0;
            bool valid = false;

            Dbg.StartLogging("PageBase.TableFormMenu()", out int pgbsx);
            Dbg.ToggleThreadOutputOmission(pgbsx);
            if (options.HasElements() && titleText.IsNotNEW())
            {
                // build menu keys
                Dbg.Log(pgbsx, $"Recieved --> '{options.Length}' options; '{titleText}' as menu title; '{titleUnderline}' as underline; '{prompt}' as prompt; '{placeholder}' as placeholder; '{optionColumns}' as number of option columns");
                string optNumKeys = "";
                int countInvalidOpts = 0;
                List<string> optionsFltrd = new List<string>();
                for (int t = 0; t < options.Length; t++)
                {
                    if (options[t].IsNotNEW())
                    {
                        optionsFltrd.Add(options[t].Trim());
                        optNumKeys += $"{t + 1 - countInvalidOpts} ";
                    }
                    else countInvalidOpts++;
                }

                // filter and prep data
                optNumKeys = optNumKeys.Trim();
                Dbg.LogPart(pgbsx, $"Prep --> Filtered options count is '{optionsFltrd.Count}'; Generated option keys [{optNumKeys}]; Edited placeholder? ");
                if (placeholder.IsNotNEW())
                {
                    Dbg.LogPart(pgbsx, $"{placeholder.Length > 50}");
                    if (placeholder.Length > 50)
                        placeholder = placeholder.Remove(50);
                }
                Dbg.LogPart(pgbsx, $"; Edited number of option columns? {!optionColumns.IsWithin(2, 4)} [{optionColumns} -> {optionColumns.Clamp(2, 4)}]");
                if (!optionColumns.IsWithin(2, 4))
                    optionColumns = optionColumns.Clamp(2, 4);
                Dbg.Log(pgbsx, ";");

                // print menu & validate menu options
                Dbg.LogPart(pgbsx, "Printing --> ");
                if (optionsFltrd.HasElements())
                {
                    Dbg.Log(pgbsx, $"Title spans window width? {titleSpanWidthQ}; No. option columns: {optionColumns}; Using default prompt? {prompt.IsNEW()} ");

                    if (titleSpanWidthQ)
                        Title(titleText, titleUnderline.IsNotNull() ? titleUnderline.Value : DefaultTitleUnderline, 0);
                    else Title(titleText, titleUnderline.IsNotNull() ? titleUnderline.Value : DefaultTitleUnderline);
                    MenuMessageTrigger();

                    // table here
                    string[] tableInfo = new string[optionColumns];
                    Dbg.Log(pgbsx, "Building options table");
                    Dbg.NudgeIndent(pgbsx, true);
                    for (int tbIx = 0; tbIx < optionsFltrd.Count; tbIx++)
                    {
                        int tbIxPlus1 = tbIx + 1;
                        bool endOpts = tbIxPlus1 >= optionsFltrd.Count;
                        bool printTable = tbIxPlus1 % optionColumns == 0; // 1%2 2%2; 
                        Dbg.LogPart(pgbsx, $"Opt#{tbIx} -->  End? {endOpts};  Print Table? {printTable} [{tbIxPlus1 % optionColumns}]  //  ");

                        string columnOption = $"[{tbIxPlus1}] {optionsFltrd[tbIx]}";
                        tableInfo[tbIx % optionColumns] = columnOption;
                        Dbg.Log(pgbsx, $"Setting {nameof(tableInfo)}[ix-{tbIx % optionColumns}] to option '{columnOption}'");

                        if (printTable || endOpts)
                        {
                            const char tableDiv = ' ';
                            switch (optionColumns)
                            {
                                case 2:
                                    Table(Table2Division.Even, tableInfo[0], tableDiv, tableInfo[1]);
                                    break;

                                case 3:
                                    Table(Table3Division.Even, tableInfo[0], tableDiv, tableInfo[1], tableInfo[2]);
                                    break;

                                case 4:
                                    Table(Table4Division.Even, tableInfo[0], tableDiv, tableInfo[1], tableInfo[2], tableInfo[3]);
                                    break;
                            }

                            tableInfo = new string[optionColumns];
                            Dbg.Log(pgbsx, $".. Printed table with [{optionColumns}] columns. Reset {nameof(tableInfo)}  //  Cause --> Print Table? {printTable};  Options End? {endOpts}");
                        }
                    }
                    Dbg.NudgeIndent(pgbsx, false);

                    Format(prompt.IsNotNEW() ? prompt : $"{Ind24}Select option >> ", ForECol.Normal);

                    /// validate menu opts
                    valid = MenuOptions(StyledInput(placeholder), out resultNum, optNumKeys.Split(' '));
                    if (valid)
                        resultNum += 1;

                    Dbg.LogPart(pgbsx, $"Input recieved [{LastInput}] (colored in '{GetPrefsForeColor(ForECol.InputColor)}');  Valid [{valid}];  Result Number [{resultNum}]");
                }
                Dbg.Log(pgbsx, " --> DONE");
            }
            Dbg.EndLogging(pgbsx);

            return valid;
        }
        /// <summary>
        ///     Creates an options menu in the form of a list. Includes menu validation.
        /// </summary>
        /// <param name="resultKey">Lowercase, alphabetic-based result key matching the bulleted letter preceding an option. Is <c>null</c> if the selected option is invalid.</param>
        /// <param name="titleText">REQUIRED.</param>
        /// <param name="titleUnderline">If <c>null</c>, will use the <see cref="cTHB"/> ('▀') as an underline.</param>
        /// <param name="prompt">Prompt line to urge selection of an option. If <c>null</c>, will use a default prompt.</param>
        /// <param name="placeholder">Pretext denoting the desired input or ushering for an input. If <c>null</c>, will use a default placeholder. Limit of 50 characters.</param>
        /// <param name="indentOptsQ">Whether to indent the list optitons.</param>
        /// <param name="clearPageQ"></param>
        /// <param name="options">REQUIRED.</param>
        /// <returns>A boolean representing the validation of the option selected from the list menu.</returns>
        public static bool ListFormMenu(out string resultKey, string titleText, char? titleUnderline, string prompt, string placeholder, bool indentOptsQ,  params string[] options)
        {
            resultKey = null;
            bool valid = false;

            Dbg.StartLogging("PageBase.ListFormMenu()", out int pgbsx);
            Dbg.ToggleThreadOutputOmission(pgbsx);
            if (options.HasElements() && titleText.IsNotNEW())
            {
                // build menu keys
                Dbg.Log(pgbsx, $"Recieved --> '{options.Length}' options; '{titleText}' as menu title; '{titleUnderline}' as underline; '{prompt}' as prompt; '{placeholder}' as placeholder.");
                string optKeys = "";
                int countInvalidOpts = 0;
                List<string> optionsFltrd = new List<string>();
                for (int i = 0; i < options.Length; i++)
                {
                    if (options[i].IsNotNEW())
                    {
                        optionsFltrd.Add(options[i].Trim());
                        optKeys += $"{IntToAlphabet(i - countInvalidOpts).ToString().ToLower()} ";
                    }
                    else countInvalidOpts++;
                }

                // filter and prep data
                optKeys = optKeys.Trim();
                Dbg.LogPart(pgbsx, $"Prep --> Filtered options count is '{optionsFltrd.Count}'; Generated option keys [{optKeys}]; Edited placeholder? ");
                if (placeholder.IsNotNEW())
                {
                    Dbg.LogPart(pgbsx, $"{placeholder.Length > 50}");
                    if (placeholder.Length > 50)
                        placeholder = placeholder.Remove(51);
                }
                Dbg.Log(pgbsx, ";");


                // print menu & validate menu options
                Dbg.LogPart(pgbsx, "Printing --> ");
                if (optionsFltrd.HasElements())
                {
                    Dbg.LogPart(pgbsx, $"Indent List Options? {indentOptsQ}; Using default prompt? {prompt.IsNEW()}  //  ");

                    // print menu
                    Title(titleText, titleUnderline.IsNotNull() ? titleUnderline.Value : DefaultTitleUnderline);
                    MenuMessageTrigger();

                    if (!indentOptsQ)
                        HoldNextListOrTable();
                    List(OrderType.Ordered_Alphabetical_LowerCase, optionsFltrd.ToArray());
                    if (!indentOptsQ)
                        Format(LatestListPrintText.Replace("\t",""), ForECol.Normal);

                    Format(prompt.IsNotNEW() ? prompt : $"{Ind24}Select option >> ", ForECol.Normal);

                    // validate menu options
                    valid = MenuOptions(StyledInput(placeholder), out resultKey, optKeys.Split(' '));
                    Dbg.LogPart(pgbsx, $"Input recieved [{LastInput}] (colored in '{GetPrefsForeColor(ForECol.InputColor)}');  Valid [{valid}];  Result Key [{resultKey}]");
                }
                Dbg.Log(pgbsx, " --> DONE");
            }
            Dbg.EndLogging(pgbsx);
            return valid;
        }
        public static void MenuMessageQueue(bool trueCondition, bool isWarning, string incorrectionMessage)
        {
            if (trueCondition)
            {
                if (!incorrectionMessage.IsNotNEW() && !isWarning)
                    incorrectionMessage = "Invalid option selected.";

                if (incorrectionMessage.IsNotNEW())
                {
                    _isMenuMessageInQueue = true;
                    _isWarningMenuMessageQ = isWarning;
                    if (isWarning)
                        _menuMessage = "[!] ";
                    else _menuMessage = "[X] ";
                    _menuMessage += incorrectionMessage.Trim();
                }
            }
        }
        static void MenuMessageTrigger()
        {
            if (_isMenuMessageInQueue && !WithinBugIdeaPageQ)
            {
                FormatLine(_menuMessage, _isWarningMenuMessageQ ? ForECol.Warning: ForECol.Incorrection);
                _isMenuMessageInQueue = false;
                _isWarningMenuMessageQ = false;
                _menuMessage = null;
            }
        }
        public static bool ColorMenu(string titleText, out Color color, params Color[] exempt)
        {
            Dbg.StartLogging("PageBase.ColorMenu()", out int pgbsx);
            Dbg.ToggleThreadOutputOmission(pgbsx);

            // prep
            Dbg.LogPart(pgbsx, $"Using default menu title? {titleText.IsNEW()}; Exempting any options? {exempt.HasElements()} // ");
            const char tDiv = '*';
            const string exemptKey = "-", nRep = "%%";// "nRep" meaning "number Replace" \\ replaced by optionBullet "#|"
            const string colBlackRename = "Default";
            if (titleText.IsNEW())
                titleText = $"Color Menu";
            Color[,] tableCols =
            {
                { Color.Red, Color.Yellow, Color.Green, Color.White },
                { Color.Maroon, Color.Orange, Color.Forest, Color.Gray },
                { Color.Cyan, Color.Blue, Color.Magenta, Color.DarkGray },
                { Color.Teal, Color.NavyBlue, Color.Purple, Color.Black }
            };

            // build table menu
            string menuOptions = "";
            Title(titleText, '.', 1);
            FormatLine("Choose a color or option by their associated number from the table below.", ForECol.Accent);
            for (int rowIx = 0; rowIx < tableCols.GetLength(0) && true; rowIx++)
            {
                HoldNextListOrTable();
                Table(Table4Division.Even, $"{nRep}{tableCols[rowIx, 0]}", tDiv, $"{nRep}{tableCols[rowIx, 1]}", $"{nRep}{tableCols[rowIx, 2]}", $"{nRep}{tableCols[rowIx, 3]}");
                if (LatestTablePrintText.IsNotNEW())
                {
                    string[] tableData = LatestTablePrintText.Split($"  {tDiv}  "); /// extra spaces because of Table()'s formatting
                    if (tableData.HasElements())
                        if (tableData.Length >= 4)
                        {
                            for (int clmIx = 0; clmIx < 4; clmIx++)
                            {
                                Color optCol = tableCols[rowIx, clmIx];
                                string optBulletChar = $"{rowIx * 4 + clmIx + 1}";
                                if (exempt.HasElements())
                                {
                                    foreach(Color exemptedCol in exempt)
                                        if (exemptedCol == optCol)
                                        {
                                            optBulletChar = exemptKey;
                                        }
                                }

                                string optionBullet = $"{optBulletChar, -2}|";
                                menuOptions += optionBullet.Remove(2).Trim() + " ";
                                string cTxt = $"{cLHB}{tableData[clmIx].Replace(nRep, "")}";

                                if (optCol == Color.Black)
                                {
                                    optCol = GetPrefsForeColor(ForECol.Normal);
                                    cTxt = cTxt.Replace("Black", colBlackRename).Replace(cLHB, '\0');
                                }

                                Format(optionBullet, ForECol.Normal);
                                Text(cTxt, optCol);
                            }
                        }
                }
                //NewLine();
            }
            
            menuOptions = menuOptions.Trim();
            Format($"{Ind24}Selection >> #", ForECol.Normal);
            string input = StyledInput(null);
            Dbg.Log(pgbsx, $"Table built, resulting menu options [{menuOptions}]");

            // menu validation
            if (input.Contains(exemptKey))
                input = tDiv.ToString();
            bool valid = MenuOptions(input, out short optNum, menuOptions.Split(' '));
            int ix1 = optNum / 4, ix2 = optNum % 4;
            color = valid? tableCols[ix1, ix2] : Color.Black;
            Dbg.Log(pgbsx, $"Recieved input [{input}]; Valid input? {valid};  Result Color [#{optNum} ({ix1}, {ix2}) -> '{color}'{(color == Color.Black ? $" ({colBlackRename})" : "")}]");
            Dbg.EndLogging(pgbsx);

            return valid;
        }



        // -- Other --
        /// <summary>Halts program for some time (in seconds) [Range 0, 10].</summary>
        public static void Wait(float seconds)
        {
            int milliSeconds = (int)(seconds.Clamp(0, 10) * 1000);
            string dbgLog = $"Waiting for {milliSeconds}ms // ";

            Stopwatch watch = Stopwatch.StartNew();
            dbgLog += $"Start time: {watch.Elapsed.TotalMilliseconds}ms";
            while (watch.ElapsedMilliseconds < milliSeconds)
                /* Do nothing but loop, kek */;

            dbgLog += $"-- End time: {watch.Elapsed.TotalMilliseconds}ms // Waiting complete.";
            Dbg.SingleLog("PageBase.Wait()", dbgLog, true);
        }
        /// <summary>Gets and saves cursor position: top and left positions which can be adjusted using <paramref name="alterTop"/> and <paramref name="alterLeft"/>.</summary>
        public static void GetCursorPosition(int alterTop = 0, int alterLeft = 0)
        {
            _cursorTop = (Console.CursorTop + alterTop).Clamp(0, Console.BufferHeight);
            _cursorLeft = (Console.CursorLeft + alterLeft).Clamp(0, Console.BufferWidth);
        }
        /// <summary>Sets cursor position: top and left positions. If <paramref name="top"/> or <paramref name="left"/> is given a value greater than or equal to <c>0</c>, their value will be used instead of the values saved from <see cref="GetCursorPosition"/></summary>
        public static void SetCursorPosition(int top = -1, int left = -1)
        {
            Console.CursorTop = top >= 0 ? top : _cursorTop;
            Console.CursorLeft = left >= 0 ? left : _cursorLeft;
        }
        public static void QueueEnterBugIdeaPage()
        {
            _enterBugIdeaPageQ = true;
        }
        public static bool IsEnterBugIdeaPageQueued()
        {
            return _enterBugIdeaPageQ == true;
        }
        public static void UnqueueEnterBugIdeaPage()
        {
            _enterBugIdeaPageQ = false;
        }
        /// <summary>Initializes a progress bar instance with display preferences. Can only be initialized once until updated to end.</summary>
        /// <remarks>A Progress bar displays realtime completion of a process. Includes a demarked start and end node and a progressively 'loading' bar.</remarks>
        /// <param name="showPercentQ">Whether to display the progress percentage (0% ~ 100%) within the 'loading' progress bar.</param>
        /// <param name="hideNodesQ">Whether to demark the start and end nodes of the progress bar.</param>
        /// <param name="cursorShiftH">How far left (negative value) or right (positive value) to shift the initial position of progress bar from cursor position. Clamped range [-10, 10].</param>
        /// <param name="cursorShiftV">How far up (negative value) or down (positive value) to shift the initial position of progress bar from cursor position. Clamped range [-1, 1].</param>
        /// <param name="barWidth">Determines width of progress bar (includes demarked start and end nodes). Clamped range [10, 50]</param>
        /// <param name="barCol">Color of 'loading' progress bar.</param>
        /// <param name="barNodesCol">Color of progress bar start and end nodes. Also determines color of 'unprogressed' displayed percentage.</param>
        public static void ProgressBarInitialize(bool showPercentQ = false, bool hideNodesQ = false, int barWidth = 20, int cursorShiftH = 0, int cursorShiftV = 0, ForECol barCol = ForECol.Correction, ForECol barNodesCol = ForECol.Normal)
        {
            if (!_enableBarQ)
            {
                _showPercentQ = showPercentQ;
                _hideNodeQ = hideNodesQ;

                _iniCursorLeft = Console.CursorLeft;
                _iniCursorTop = Console.CursorTop;

                _barPosLeft = (_iniCursorLeft + cursorShiftH.Clamp(-10, 10)).Clamp(0, Console.BufferWidth);
                _barPosTop = (_iniCursorTop + cursorShiftV.Clamp(-1, 1)).Clamp(0, PageSizeLimit);
                _barWidth = barWidth.Clamp(10, 50);

                _barCol = barCol;
                _barNodeCol = barNodesCol;
                _enableBarQ = true;

                TaskCount = 0;
                TaskNum = 0;
            }
        }
        /// <summary>Prints and updates an initialized progress bar instance to display <paramref name="percentage"/>.</summary>
        /// <remarks>A Progress bar displays realtime completion of a process. Includes a demarked start and end node and a progressively 'loading' bar.</remarks>
        /// <param name="percentage">The quantified loading progress as a float value between 0 and 1 (clamped). The progress bar is deinitialized once <paramref name="percentage"/> hits value of 1.</param>
        /// <param name="destroyOnEndQ">Will remove the progress bar when <paramref name="percentage"/> hits a value of 1.</param>
        /// <param name="forceEndQ">Used to forcibly abort updating the progress bar and allowing for reinitializing of progress bar information. Trigger Once.</param>
        /// <param name="state">Appends a value to declare the status of the progress bar; what stage of a process the progress bar signifies. Is always printed in <see cref="ForECol.Accent"/>. Maximum of 30 characters.</param>
        public static void ProgressBarUpdate(float percentage, bool destroyOnEndQ = false, bool forceEndQ = false, string state = null)
        {
            if (_enableBarQ)
            {
                // setup
                percentage = percentage.Clamp(0f, 1f);
                Console.CursorLeft = _barPosLeft;
                Console.CursorTop = _barPosTop;

                const int maxStateLen = 30;
                const char dst = ' '; /// DBG'-'   OG' '
                float barCount = _hideNodeQ ? _barWidth : _barWidth - 2;
                float barStep = 1f / barCount;

                // string printing -- A) progress Bar   B) state
                string printFull = "", printEmpty = "", percent = $"{percentage * 100 : 0}%", destroyText = "", printState = "";
                for (int bx = 0; bx < barCount; bx++)
                {
                    const char defFill = cDS, defEmpty = cLS;
                    char barChar = defFill;
                    float barValue = (bx + 1) * barStep;

                    /// determines when to print percent text (centered)
                    if (_showPercentQ)
                    {
                        int startPercentPrint = (int)((barCount / 2) - (percent.Length / 2)) - 1; /// (barMidway - halfTextLength) - toIndexNum
                        if (bx > startPercentPrint && bx - startPercentPrint < percent.Length)
                            barChar = percent[bx - startPercentPrint];
                    }

                    /// IF ...: print full; ELSE (IF bar char: print empty; ELSE print percent text);
                    if (barValue <= percentage)
                        printFull += barChar;
                    else printEmpty += barChar == defFill ? defEmpty : barChar;

                    destroyText += dst;
                }
                if (state.IsNotNEW())
                {
                    /// start at negative number for the extra spaces before state text
                    for (int sx = -1; sx < maxStateLen; sx++)
                    {
                        if (sx < state.Length && sx >= 0)
                            printState += state[sx];
                        else printState += dst;

                        destroyText += dst;
                    }
                }


                // print progress bar
                if (printFull.IsNotNE() || printEmpty.IsNotNE())
                {/// wrap
                    if (!_hideNodeQ)
                    {
                        Format(cRHB.ToString(), _barNodeCol);
                        destroyText += $"{dst}{dst}";
                    }

                    if (printFull.IsNotNE())
                        Format(printFull, _barCol);
                    if (printEmpty.IsNotNE())
                        Format(printEmpty, _barNodeCol);

                    if (!_hideNodeQ)
                        Format(cLHB.ToString(), _barNodeCol);

                    if (printState.IsNotNEW())
                        Format(printState, ForECol.Accent);
                }

                // end and disable if complete  -- destroy and reset cursor if applicable
                if (percentage >= 1 || forceEndQ)
                {
                    _enableBarQ = false;
                    //Text(" END", Color.DarkGray);

                    if (destroyOnEndQ)
                    {
                        Console.CursorLeft = _barPosLeft;
                        Console.CursorTop = _barPosTop;
                        Format(destroyText);

                        Console.CursorLeft = _iniCursorLeft;
                        Console.CursorTop = _iniCursorTop;
                    }
                }

                /// mostly for checking placement within processes
                if (Program.isDebugVersionQ)
                    Wait(0.075f);
            }            
        }
        public static void QueueEnterFileChooserPage()
        {
            _enterFileChooserPageQ = true;
        }
        public static bool IsEnterFileChooserPageQueued()
        {
            return _enterFileChooserPageQ == true;
        }
        public static void UnqueueEnterFileChooserPage()
        {
            _enterFileChooserPageQ = false;
        }
        public static void ToggleFileChooserPage(bool enableQ)
        {
            _allowFileChooserPageQ = enableQ;
        }
    }
}
