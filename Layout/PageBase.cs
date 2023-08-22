using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;
using System.Diagnostics;

namespace HCResourceLibraryApp.Layout
{
    /// <summary>Foreground Element Color</summary>
    public enum ForECol
    {
        Normal,
        Highlight, 
        Accent,
        Correction, 
        Incorrection, 
        Warning,
        Heading1, 
        Heading2,
        InputColor
    }

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
        static bool _isMenuMessageInQueue, _isWarningMenuMessageQ, _enableWordWrapQ = true, _holdWrapIndentQ;
        static ForECol? _normalAlt, _highlightAlt;


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


        // PROPERTIES
        public static bool VerifyFormatUsage { get; set; }
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
                Dbug.DeactivateNextLogSession();
                Dbug.StartLogging("PageBase.WordWrap(str)");
                Dbug.LogPart($"Source :: {(_wrapSource == 0 ? "Format()" : (_wrapSource == 1 ? "FormatLine()" : "overload Format()"))}  //  ");
                Dbug.Log($"Received text :: {text.Replace("\n", "\\n").Replace("\t", "\\t")}");

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
                    Dbug.Log($"Set and held a wrapping indent position :: {_wrapIndentHold}; ");
                }
                else if (!holdIndentQ && _wrapIndentHold != wrapUnholdNum)
                {
                    Dbug.Log($"Released held wrapping indent position :: {_wrapIndentHold}; ");
                    _wrapIndentHold = wrapUnholdNum;
                }


                // -- If requires wrapping, WRAP! --
                if (wrapBuffer - wrapStartPos <= text.Length || text.Contains("\n"))
                {
                    // -- Prep text to wrap --
                    text = text.Replace("\n", newLineReplace).Replace("\t", tabReplace);
                    Dbug.LogPart($"Wrapping Text: {wrapBuffer} spaces starting at '{wrapStartPos}'; ");
                    Dbug.Log($"Replaced any newline escape characters (\\n) with '{newLineReplace}'; Replaced any 'tab' escape characters (\\t) with 8 spaces; ");


                    // -- Separate words into bits --
                    List<string> textsToWrap = new();
                    string partTexts = "";
                    bool wasSpaceCharQ = false;
                    Dbug.Log("Separating text into wrappable pieces :: ");
                    Dbug.NudgeIndent(true);
                    Dbug.LogPart($" >|");
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
                                Dbug.LogPart($"{partTexts}{(isEndQ ? "" : "|")}");
                                partTexts = "";
                            }
                        }
                        partTexts += c.ToString();
                        wasSpaceCharQ = isSpaceCharQ;
                    }
                    Dbug.Log($"|< ");
                    Dbug.NudgeIndent(false);


                    // -- It's time to WRAP! --
                    Dbug.Log("Words have been separated: proceeding to wrap words; ");
                    if (textsToWrap.HasElements())
                    { /// wrapping ... as to hide within...

                        string wrappedText = "";
                        int currWrapPos = wrapStartPos;
                        int wrapIndentLevel = 0;
                        //Dbug.Log($"Extra info :: currWrapPos = {wrapStartPos}, helpWrapPos = {_wrapIndentHold};");

                        Dbug.NudgeIndent(true);
                        Dbug.LogPart("> :|");
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
                                    Dbug.LogPart($" [Ind{wrapIndentLevel}8] ");
                                }
                                /// this won't run anyway, I'm sure
                                //else
                                //{
                                //    wrapIndentLevel = currWrapPos.Clamp(0, WordWrapIndentLim);
                                //    Dbug.LogPart($" [LimInd{wrapIndentLevel}8] ");
                                //}
                            }
                            else if (isStartQ && _wrapIndentHold != wrapUnholdNum)
                            {
                                wrapIndentLevel = _wrapIndentHold.Clamp(0, WordWrapIndentLim);
                                Dbug.LogPart($" [HldInd{wrapIndentLevel}8] ");
                            }


                            /// IF printing this text will not exceed buffer width AND text to print is not newline: print text; ELSE...
                            if (currWrapPos + wText.Length < wrapBuffer && !wText.Contains(newLineReplace))
                            {
                                wrappedText += wText;
                                Dbug.LogPart($"{wText}");
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

                                            Dbug.Log("|-> ");
                                            Dbug.LogPart($" ->|{wText}");
                                        }
                                        else
                                        {
                                            wrappedText += wText;

                                            Dbug.LogPart($"|{wText}|-- ");
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
                                                    Dbug.LogPart($" =>|{subTextIxRange}{wTSubText}");
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
                                                        Dbug.Log($"{subTextIxRange}{wTSubText}|=> ");
                                                        wrappedText += $"{wTSubText}";
                                                    }
                                                    else
                                                    {
                                                        Dbug.Log($" =>|{subTextIxRange}{wTSubText}|=> ");
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

                                        Dbug.Log("|>> ");
                                        Dbug.LogPart($" >>|{wText}");
                                        if (VerifyFormatUsage)
                                            wrappedText += $"{WordWrapNewLineKey}{wText}";
                                        else wrappedText += $"\n{wText}";
                                    }
                                    else //if (wText.EndsWith(newLineReplace))
                                    {
                                        currWrapPos = 0;
                                        string fltWText = wText.Replace(newLineReplace, "");
                                        Dbug.Log($"{fltWText}|<> ");

                                        wText = wrapIndText;
                                        if (fltWText.IsEW() && fltWText.CountOccuringCharacter(' ') > 0)
                                            wText += fltWText;
                                        Dbug.LogPart($" <>|{wText}");

                                        if (VerifyFormatUsage)
                                            wrappedText += $"{fltWText}{WordWrapNewLineKey}{wText}";
                                        else wrappedText += $"{fltWText}\n{wText}";
                                    }
                                }
                            }
                            if (!isEndQ)
                                Dbug.LogPart("|");
                            currWrapPos += wText.Length;
                        }
                        Dbug.Log("|: <");
                        Dbug.NudgeIndent(false);

                        // return wrapped text
                        Dbug.Log($"Finshed wrapping text :: {wrappedText.Replace("\n", $"{newLineReplace.Trim()}{cRHB}")}");
                        text = wrappedText;

                        Dbug.Log("LEGEND ///  Word divider  |  //  Start|End  > :|: <  //  Wrap  ->  (Just Fits  --)  //  Break'N'Wrap  =>  //  NewLine  >>  (with word  <>)  /// END LEGEND");
                    }
                }
                else Dbug.Log($"This text does not require wrapping: '{wrapBuffer - wrapStartPos - text.Length}' character spaces remain after printing this text.");

                Dbug.EndLogging();
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
            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("PageBase.Highlight(str, params str[])");
            
            Dbug.Log($"Recieved --> text (\"{text}\"); highlightedTexts (has elements? {highlightedTexts.HasElements()})");
            if (text.IsNotNEW() && highlightedTexts.HasElements())
            {
                short highlightIndex = 0;
                foreach (string highText in highlightedTexts)
                {
                    Dbug.LogPart($".  |highlightedText (@ index #{highlightIndex}) --> ");
                    if (highText.IsNotNE())
                    {
                        Dbug.LogPart($"{highText}  //  ");
                        if (text.Contains(highText))
                        {
                            text = text.Replace(highText, $"<{highlightIndex}>");                            
                            Dbug.Log($"result text --> \"{text}\"");
                        }
                        else Dbug.Log("no change.");
                    }
                    else Dbug.Log("null or empty highlightedText~");
                    highlightIndex++;
                }

                string[] textWords = null;
                if (textWords == null)
                { /// wrapping
                    //Dbug.LogPart("Compiling words ::  ");
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
                    //Dbug.Log("  --> Done");

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
                    Dbug.LogPart($".. |");

                    string word = textWords[c];
                    bool end = c + 1 == textWords.Length;
                    Dbug.LogPart($"assessing word '{word}' || ");
                    if (word.IsNotNE())
                    {
                        // IF word is or contains subkey, check for matching highlight word or phrase and replace subkey with it
                        if (word.Contains('<') && word.Contains('>'))
                        {
                            Dbug.LogPart("format highlight word >> ");
                            for (int hli = 0; hli < highlightedTexts.Length; hli++)
                            {
                                string subKey = $"<{hli}>";
                                string replacePhrase = highlightedTexts[hli];                                
                                if (word.Contains(subKey))
                                {
                                    Dbug.LogPart($"Replace '{subKey}' with '{replacePhrase}'  //  result -->  ");
                                    // <0> == <0>?
                                    if (word == subKey)
                                    {
                                        if (!end || !newLine)
                                            Format(replacePhrase, fecHighlight);
                                        else FormatLine(replacePhrase, fecHighlight);
                                        Dbug.LogPart($"'[{replacePhrase}]'");
                                    }

                                    // <0>, == <0>?
                                    else
                                    {
                                        Dbug.LogPart("'");

                                        string[] splitWord = word.Split(subKey);
                                        for (int wx = 0; wx < splitWord.Length; wx++)
                                        {
                                            bool wordEndQ = splitWord.Length == wx + 1;

                                            string partWord = splitWord[wx];
                                            if (partWord.IsNotNE())
                                            {
                                                Format(partWord, fecNormal);
                                                Dbug.LogPart(partWord);
                                            }

                                            if (!wordEndQ)
                                            {
                                                Format(replacePhrase, fecHighlight);
                                                Dbug.LogPart($"[{replacePhrase}]");
                                            }

                                            if (wordEndQ)
                                                if (newLine && end)
                                                    NewLine();
                                        }
                                        Dbug.LogPart("'");
                                    }
                                }
                            }
                            Dbug.Log($"  // (on newline? {end && newLine})");
                        }

                        else
                        {
                            if (!end || !newLine)
                                Format(word, fecNormal);
                            else FormatLine(word, fecNormal);

                            Dbug.Log($"format normal word >> '{word}' (on newline? {end && newLine})");
                        }

                    }
                    else Dbug.Log("null or empty word~");
                }
            }


            Dbug.EndLogging();
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
            string input = Input(placeholder, _preferencesRef.Input);
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
                //Dbug.SingleLog("HomePage.HSNL(int, int)", $"Got min|max values ({minimumNL}|{maximumNL}) and range ({range}); Solved for range factor ({rangeFactor * 100:0.0}%); Returned sensitive number of newlines: {numOfNewLines};");
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
                //Dbug.SingleLog("HomePage.HSNLPrint(int, int)", $"Got min|max values ({minimumNL}|{maximumNL}) and range ({range}); Solved for range factor ({rangeFactor * 100:0.0}%); Returned sensitive number of newlines: {numOfNewLines};");

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
        /// <summary>Triggers a queued incorection message for input validation. Also include a Pause().</summary>
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
        /// <param name="resultNum">Number-based result key matching the bracketed number preceding a selected option. Is <c>-1</c> if the selected option is invalid.</param>
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

            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("Table Form Menu Debug");
            if (options.HasElements() && titleText.IsNotNEW())
            {
                // build menu keys
                Dbug.Log($"Recieved --> '{options.Length}' options; '{titleText}' as menu title; '{titleUnderline}' as underline; '{prompt}' as prompt; '{placeholder}' as placeholder; '{optionColumns}' as number of option columns");
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
                Dbug.LogPart($"Prep --> Filtered options count is '{optionsFltrd.Count}'; Generated option keys [{optNumKeys}]; Edited placeholder? ");
                if (placeholder.IsNotNEW())
                {
                    Dbug.LogPart($"{placeholder.Length > 50}");
                    if (placeholder.Length > 50)
                        placeholder = placeholder.Remove(50);
                }
                Dbug.LogPart($"; Edited number of option columns? {!optionColumns.IsWithin(2, 4)} [{optionColumns} -> {optionColumns.Clamp(2, 4)}]");
                if (!optionColumns.IsWithin(2, 4))
                    optionColumns = optionColumns.Clamp(2, 4);
                Dbug.Log(";");

                // print menu & validate menu options
                Dbug.LogPart("Printing --> ");
                if (optionsFltrd.HasElements())
                {
                    Dbug.Log($"Title spans window width? {titleSpanWidthQ}; No. option columns: {optionColumns}; Using default prompt? {prompt.IsNEW()} ");

                    if (titleSpanWidthQ)
                        Title(titleText, titleUnderline.IsNotNull() ? titleUnderline.Value : DefaultTitleUnderline, 0);
                    else Title(titleText, titleUnderline.IsNotNull() ? titleUnderline.Value : DefaultTitleUnderline);
                    MenuMessageTrigger();

                    // table here
                    string[] tableInfo = new string[optionColumns];
                    Dbug.Log("Building options table");
                    Dbug.NudgeIndent(true);
                    for (int tbIx = 0; tbIx < optionsFltrd.Count; tbIx++)
                    {
                        int tbIxPlus1 = tbIx + 1;
                        bool endOpts = tbIxPlus1 >= optionsFltrd.Count;
                        bool printTable = tbIxPlus1 % optionColumns == 0; // 1%2 2%2; 
                        Dbug.LogPart($"Opt#{tbIx} -->  End? {endOpts};  Print Table? {printTable} [{tbIxPlus1 % optionColumns}]  //  ");

                        string columnOption = $"[{tbIxPlus1}] {optionsFltrd[tbIx]}";
                        tableInfo[tbIx % optionColumns] = columnOption;
                        Dbug.Log($"Setting {nameof(tableInfo)}[ix-{tbIx % optionColumns}] to option '{columnOption}'");

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
                            Dbug.Log($".. Printed table with [{optionColumns}] columns. Reset {nameof(tableInfo)}  //  Cause --> Print Table? {printTable};  Options End? {endOpts}");
                        }
                    }
                    Dbug.NudgeIndent(false);

                    Format(prompt.IsNotNEW() ? prompt : $"{Ind24}Select option >> ", ForECol.Normal);

                    /// validate menu opts
                    valid = MenuOptions(StyledInput(placeholder), out resultNum, optNumKeys.Split(' '));
                    if (valid)
                        resultNum += 1;

                    Dbug.LogPart($"Input recieved [{LastInput}] (colored in '{GetPrefsForeColor(ForECol.InputColor)}');  Valid [{valid}];  Result Number [{resultNum}]");
                }
                Dbug.Log(" --> DONE");
            }
            Dbug.EndLogging();

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

            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("List Form Menu Debug");
            if (options.HasElements() && titleText.IsNotNEW())
            {
                // build menu keys
                Dbug.Log($"Recieved --> '{options.Length}' options; '{titleText}' as menu title; '{titleUnderline}' as underline; '{prompt}' as prompt; '{placeholder}' as placeholder.");
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
                Dbug.LogPart($"Prep --> Filtered options count is '{optionsFltrd.Count}'; Generated option keys [{optKeys}]; Edited placeholder? ");
                if (placeholder.IsNotNEW())
                {
                    Dbug.LogPart($"{placeholder.Length > 50}");
                    if (placeholder.Length > 50)
                        placeholder = placeholder.Remove(51);
                }
                Dbug.Log(";");


                // print menu & validate menu options
                Dbug.LogPart("Printing --> ");
                if (optionsFltrd.HasElements())
                {
                    Dbug.LogPart($"Indent List Options? {indentOptsQ}; Using default prompt? {prompt.IsNEW()}  //  ");

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
                    Dbug.LogPart($"Input recieved [{LastInput}] (colored in '{GetPrefsForeColor(ForECol.InputColor)}');  Valid [{valid}];  Result Key [{resultKey}]");
                }
                Dbug.Log(" --> DONE");
            }
            Dbug.EndLogging();
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
            if (_isMenuMessageInQueue)
            {
                FormatLine(_menuMessage, _isWarningMenuMessageQ ? ForECol.Warning: ForECol.Incorrection);
                _isMenuMessageInQueue = false;
                _isWarningMenuMessageQ = false;
                _menuMessage = null;
            }
        }
        public static bool ColorMenu(string titleText, out Color color, params Color[] exempt)
        {
            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("PageBase.ColorMenu");

            // prep
            Dbug.LogPart($"Using default menu title? {titleText.IsNEW()}; Exempting any options? {exempt.HasElements()} // ");
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
            Dbug.Log($"Table built, resulting menu options [{menuOptions}]");

            // menu validation
            if (input.Contains(exemptKey))
                input = tDiv.ToString();
            bool valid = MenuOptions(input, out short optNum, menuOptions.Split(' '));
            int ix1 = optNum / 4, ix2 = optNum % 4;
            color = valid? tableCols[ix1, ix2] : Color.Black;
            Dbug.Log($"Recieved input [{input}]; Valid input? {valid};  Result Color [#{optNum} ({ix1}, {ix2}) -> '{color}'{(color == Color.Black ? $" ({colBlackRename})" : "")}]");
            Dbug.EndLogging();

            return valid;
        }



        // -- Other --
        /// <summary>Halts program for some time (in seconds) [Range 0, 10].</summary>
        public static void Wait(float seconds)
        {
            int milliSeconds = (int)(seconds.Clamp(0, 10) * 1000);
            Dbug.DeactivateNextLogSession();
            Dbug.StartLogging("PageBase.Wait()");            
            Dbug.LogPart($"Waiting for {milliSeconds}ms // ");

            Stopwatch watch = Stopwatch.StartNew();
            Dbug.LogPart($"Start time: {watch.Elapsed.TotalMilliseconds}ms");
            while (watch.ElapsedMilliseconds < milliSeconds)
                /* Do nothing but loop, kek */;
            Dbug.Log($"-- End time: {watch.Elapsed.TotalMilliseconds}ms // Waiting complete.");
            Dbug.EndLogging();
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
    }
}
