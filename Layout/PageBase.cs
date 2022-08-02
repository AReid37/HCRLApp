using System;
using System.Collections.Generic;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;

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
            > Highlight(text, highlightedText)
            > ChooseColorMenu(...)
            
           Menu.builds
            > Table Form Menu(...) [number-based]
                >> (str titleText, str titleUnderline, bl clearPageQ, params str options) 
                << (ret bl valid // out shr resultNum)
            > List Form Menu(...) [string-based]
                >> (str titleText, str titleUnderline, bl clearPageQ, params str options)
                << (ret bl valid // out str resultKey)
            > Navigation Bar(...)
        */

        #region fields / props
        static Preferences _preferencesRef;
        static readonly string FormatUsageKey = "`";

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

        public static bool VerifyFormatUsage { private get; set; }
        #endregion


        // -- Text Formatting --
        public static void ApplyPreferencesReference(Preferences preference)
        {
            _preferencesRef = preference;
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
                    Text(FormatUsageKey, Color.DarkGray);
                Text(text, GetForeColor(foreElementCol));
            }
        }
        public static void FormatLine(string text, ForECol foreElementCol)
        {
            if (text.IsNotNE())
            {
                if (VerifyFormatUsage)
                    Text(FormatUsageKey, Color.DarkGray);
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
                //Dbug.SetIndent(1);
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
                //Dbug.SetIndent(0);

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
        /// ChooseColorMenu(...)
        

        // -- Menu Builds --
    }
}
