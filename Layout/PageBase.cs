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
                >> (bool compactQ, params Col exclude)
                << (ret Col color)
                
            
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
        static readonly string FormatUsageKey = "`";
        const char DefaultTitleUnderline = cTHB;
        static string _menuMessage, _incorrectionMessage;
        static bool _isMenuMessageInQueue, _isWarningMenuMessageQ;


        // PUBLIC
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


        // PROPERTIES
        public static bool VerifyFormatUsage { private get; set; }
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
                Console.SetBufferSize(tWidth, 9001);
                //Console.SetWindowSize(120, 30);
                //Console.SetBufferSize(120, 9001);

                // minimal customization
                CustomizeMinimal(MinimalMethod.List, _preferencesRef.Normal, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.Important, _preferencesRef.Heading1, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.Table, _preferencesRef.Normal, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.Title, _preferencesRef.Heading1, _preferencesRef.Accent);
                CustomizeMinimal(MinimalMethod.HorizontalRule, _preferencesRef.Accent, _preferencesRef.Accent);
            }
        }
        public static Color GetForeColor(ForECol fftype)
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
        public static void Format(string text, ForECol foreElementCol)
        {
            if (text.IsNotNE())
            {
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                }
                Text(text, GetForeColor(foreElementCol));
            }
        }
        public static void FormatLine(string text, ForECol foreElementCol)
        {
            if (text.IsNotNE())
            {
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                }
                TextLine(text, GetForeColor(foreElementCol));
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
            Dbug.StartLogging("PageBase.Highlight(str, params str[])");
            Dbug.Log($"Recieved --> text (\"{text}\"); highlightedTexts (has elements? {highlightedTexts.HasElements()})");
            if (text.IsNotNEW() && highlightedTexts.HasElements())
            {

                //List<short> insertIndexes = new List<short>();
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

                string[] textWords = text.Split(' ');
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
                                            Format(replacePhrase, ForECol.Highlight);
                                        else FormatLine(replacePhrase, ForECol.Highlight);
                                        Dbug.LogPart($"'[{replacePhrase}]'");
                                    }

                                    // <0>, == <0>?
                                    else
                                    {
                                        Dbug.LogPart("'");

                                        string[] splitWord = word.Split(subKey);
                                        if (splitWord[0].IsNotNE())
                                        {
                                            Format(splitWord[0], ForECol.Normal);
                                            Dbug.LogPart($"{splitWord[0]}");
                                        }

                                        if (!splitWord[1].IsNotNE() && end && newLine)
                                            FormatLine(replacePhrase, ForECol.Highlight);
                                        else Format(replacePhrase, ForECol.Highlight);
                                        Dbug.LogPart($"[{replacePhrase}]");

                                        if (splitWord[1].IsNotNE())
                                        {
                                            if (!end || !newLine)
                                                Format(splitWord[1], ForECol.Normal);
                                            else FormatLine(splitWord[1], ForECol.Normal);
                                            Dbug.LogPart($"{splitWord[1]}");
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
                                Format(word, ForECol.Normal);
                            else FormatLine(word, ForECol.Normal);
                            Dbug.Log($"format normal word >> '{word}' (on newline? {end && newLine})");
                        }

                        if (!end)
                            Text(" ");
                    }
                    else Dbug.Log("null or empty word~");
                }
            }
            Dbug.EndLogging();
        }
        /// <summary>Input placeholder character limit: 50 characters. <br></br>Placeholder color cannot be changed.</summary>
        public static string StyledInput(string placeholder)
        {
            string input = Input(placeholder, _preferencesRef.Input);
            return input;
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
        /// <summary>Triggers a queued incorection message for input validation.</summary>
        /// <param name="pretext">Precedes the incorrection message.</param>
        public static void IncorrectionMessageTrigger(string pretext)
        {
            if (_incorrectionMessage.IsNotNEW())
            {
                if (pretext.IsNotNEW())
                    Format($"{pretext}{_incorrectionMessage}", ForECol.Incorrection);
                else Format(_incorrectionMessage, ForECol.Incorrection);
                Pause();
            }
        }
        /// <summary>A confirmation prompt for yes and no</summary>
        /// <param name="prompt"></param>
        /// <param name="yesNo">Is <c>true</c> if "yes" to prompt, and is <c>false</c> if "no" to prompt.</param>
        /// <param name="yesMsg">A confirmation message following after user's "yes" input.</param>
        /// <param name="noMsg">A confirmation message following after user's "no" input.</param>
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
        ///     number-based; always returns characters where xER >= 0
        /// </summary>
        /// <param name="resultNum"></param>
        /// <param name="titleText"></param>
        /// <param name="titleUnderline"></param>
        /// <param name="clearPageQ"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool TableFormMenu(out short resultNum, string titleText, char titleUnderline, params string[] options)
        {
            resultNum = 0;
            bool valid = false;
            return valid;
        }
        /// <summary>
        ///     Creates an options menu in the form of a list. Includes menu validation.
        /// </summary>
        /// <param name="resultKey">String-based result key. Is <c>null</c> if the selected option is invalid.</param>
        /// <param name="titleText">REQUIRED.</param>
        /// <param name="titleUnderline">If <c>null</c>, will use the <see cref="cTHB"/> ('▀') as an underline.</param>
        /// <param name="prompt">Prompt line to urge selection of an opiton. If <c>null</c>, will use a default prompt.</param>
        /// <param name="placeholder">Pretext denoting the desired input or ushering for an input. If <c>null</c>, will use a default placeholder. Limit of 50 characters.</param>
        /// <param name="indentOptsQ">Whether to indent the list optitons.</param>
        /// <param name="clearPageQ"></param>
        /// <param name="options">REQUIRED.</param>
        /// <returns>A boolean representing the validation of the option selected from the list menu.</returns>
        public static bool ListFormMenu(out string resultKey, string titleText, char? titleUnderline, string prompt, string placeholder, bool indentOptsQ,  params string[] options)
        {
            resultKey = null;
            bool valid = false;

            Dbug.StartLogging("List Form Menu Debug");
            if (options.HasElements() && titleText.IsNotNE())
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
                    Dbug.LogPart($"Indent List Options? {indentOptsQ}; Using default prompt? {!prompt.IsNotNEW()}  //  ");

                    // print menu
                    Title(titleText, titleUnderline.HasValue ? titleUnderline.Value : DefaultTitleUnderline);
                    MenuMessageTrigger();

                    if (!indentOptsQ)
                        HoldNextListOrTable();
                    List(OrderType.Ordered_Alphabetical_LowerCase, optionsFltrd.ToArray());
                    if (!indentOptsQ)
                        Format(LatestListPrintText.Replace("\t",""), ForECol.Normal);

                    Format(prompt.IsNotNEW() ? prompt : $"{Ind24}Select option >> ", ForECol.Normal);

                    // validate menu options
                    valid = MenuOptions(Input(placeholder, GetForeColor(ForECol.InputColor)), out resultKey, optKeys.Split(' '));
                    Dbug.LogPart($"Input recieved [{LastInput}] (colored in '{GetForeColor(ForECol.InputColor)}');  Valid [{valid}];  Result Key [{resultKey}]");
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



        // -- Other --
        /// <summary>Halts program for some time [Range 0, 10].</summary>
        public static void Wait(float seconds)
        {
            int milliSeconds = (int)(seconds.Clamp(0, 10) * 1000);
            //Dbug.StartLogging("PageBase.Wait()");
            Dbug.LogPart($"Waiting for {milliSeconds}ms // ");

            Stopwatch watch = Stopwatch.StartNew();
            Dbug.LogPart($"Start time: {watch.Elapsed.TotalMilliseconds}ms");
            while (watch.ElapsedMilliseconds < milliSeconds)
                ;
            Dbug.Log($"-- End time: {watch.Elapsed.TotalMilliseconds}ms // Waiting complete.");
        }
    }
}
