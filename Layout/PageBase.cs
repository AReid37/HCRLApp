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
                Console.SetBufferSize(tWidth, 500);
                //Console.SetWindowSize(120, 30);
                //Console.SetBufferSize(120, 9001);

                // minimal customization
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
        public static void Format(string text, ForECol foreElementCol)
        {
            if (text.IsNotNE())
            {
                WordWrap(text);
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                }
                Text(text, GetPrefsForeColor(foreElementCol));
            }
        }
        public static void FormatLine(string text, ForECol foreElementCol)
        {
            if (text.IsNotNE())
            {
                WordWrap(text);
                if (VerifyFormatUsage)
                {
                    Text(FormatUsageKey, Color.DarkGray);
                    if (text.Contains("\n"))
                        text = text.Replace("\n", $"\n{FormatUsageKey}");
                }
                TextLine(text, GetPrefsForeColor(foreElementCol));
            }
        }
        // public for testing purposes
        public static string WordWrap(string text)
        {
            /// from 'left' to 'right - 1' char spaces
            return text;
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
    }
}
