using System;
using System.Collections.Generic;
using static HCResourceLibraryApp.Layout.PageBase;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.DataHandling;
using static System.Net.WebRequestMethods;

namespace HCResourceLibraryApp.Layout
{
    internal static class GenSteamLogPage
    {
        static ResLibrary _resLibrary;
        static readonly char subMenuUnderline = '+';

        public static void GetResourceLibraryReference(ResLibrary mainLibrary)
        {
            _resLibrary = mainLibrary;
        }
        public static void OpenPage()
        {
            /// How does this get setup?
            /// MAIN PAGE
            ///     -----------
            ///     Title and desc
            ///     Parameter View (shows what needs to be done before generating steam log)
            ///     --
            ///     Menu
            ///     > Opt1: Log Version Number  -> Button and Prompt
            ///     > Opt2: Steam Log Formatter -> Page
            ///     > Opt3: Generate Steam Log -> Page
            ///         -- Review parameters, generate steam log ('preview' and 'source code' views)
            ///     -----------
            ///     
            /// STEAM LOG FORMATTER
            /// Full-on acts like an IDE
            ///     -----------
            ///     Title, Desc
            ///     > Opt1: Edit Formatting -> Page
            ///         -- Edited line by line. Accompanied with editing info (syntax, language rules)
            ///             ^^ Choose line to edit as '{Add/Edit/Delete}{LineNumber}'
            ///             ^^ Type line right into line then enter a phrase to finish editing.
            ///             ^^ After edit end, color-coding and syntax checks will provide any errors, issues or so-on under the line if any
            ///         -- Has a history, color-codes elements
            ///     > Opt2: ?
            ///     ----------
            ///     
            /// 
            ///     -- Link to Steam Text Formatting --
            ///     https://steamcommunity.com/comment/Guide/formattinghelp
            ///     

            bool exitSteamGenPage = false;
            do
            {
                Program.LogState("Generate Steam Log");
                Clear();
                Title("Generate Steam Log", cTHB, 1);
                FormatLine($"{Ind24}Facilitates generation of a version log using Steam's formatting rules.", ForECol.Accent);
                NewLine();

                // preview parameters here
                TextLine("--- PARAMETERS REVIEW ---\n Version Log x.xx\n Using formatting 'format1'\n---\n");

                /// should i switch to tableFormMenu for this?
                bool validMenuKey = ListFormMenu(out string genMenuKey, "Generation Menu", null, $"Choose parameter to edit >> ", "a~d", true, $"Log Version,Steam Log Formatter,Generate Steam Log,{exitPagePhrase}".Split(','));
                MenuMessageQueue(!validMenuKey, false, null);

                if (validMenuKey)
                {
                    switch (genMenuKey)
                    {
                        case "a":
                            SubPage_LogVersion();
                            break;

                        case "b":
                            SubPage_SteamFormatter();
                            break;

                        case "c":
                            SubPage_GenerateSteamLog();
                            break;

                        case "d":
                            exitSteamGenPage = true;
                            break;
                    }
                }

            } while (!exitSteamGenPage);
        }

        // TO BE DONE
        static void SubPage_LogVersion()
        {
            Program.LogState("Generate Steam Log|Log Version (WIP)");
            TextLine("\n\n-- Openned 'Log Version' subpage --");
            Pause();
        }
        // TO BE DONE
        static void SubPage_SteamFormatter()
        {
            Program.LogState("Generate Steam Log|Steam Formatter (WIP)");
            TextLine("\n\n-- Openned 'Steam Formatter' subpage --");
            Pause();

            /** FORMATTING PLANNING, PROBABLY A CRAP TONNE
            -------------------------------------- 
            Sec1 - THE FORMATTER LANGUAGE
            -----
            Language direction, behavior, properties
                - An object-oriented language.
                - The language will be tailored towards the data available within the resource library and formatting rules of steam
                - The language parses in linear fashion, from first line to last line without skipping or returning
                - Case-sensitive language
                - No expressive error handling (ei, crashes) on use; provides error messages with syntax issues during edit. 

            Language Syntax
            > General
                syntax          outcome
                _________________________________
                // abc123       line comment
                abc123          code
                "abc123"        plain text
                {abc123}        library reference
                {{              plain text '{'
                }}              plain text '}'
                $abc123         steam formatting reference
                $$              plain text '$'
                if#=#|          keyword, control; compares two given number to be equal. Prints following line if condition is true (numbers are equal). Must be placed at beginning of line
                repeat#|        keyword, control; repeats line '#' times incrementing from zero to given number '#'. Any occuring '#' in following line is replaced with this incrementing number. Must be placed at beginning of line, except when line starts with 'if#=#|' syntax
            

            > Specific to LIBRARY
                - Sources information from resource library via targetted "Log Version"

                syntax              outcome
                _________________________________
                {Version}           single; fetches version numbers only (ex 1.00)
                {AddedCount}        single; fetches number of added items
                {Added:#,prop}      array; access property from zero-based Added entry '#'. Properties: name, ids
                {AdditCount}        single; fetches number of additional items
                {Addit:#,prop}      array; access property from zero-bassed Additional entry '#'. Properties: ids, optionalName, relatedID, relatedContent
                {TTA}               single; fetches the number of total textures/contents added
                {UpdatedCount}      single; fetches the number of updated items
                {Updated:#,prop}    array; access property from zero-based Updated entry '#'. Properties: id, name, changeDesc
                {LegendCount}       single; fetches the number of legends used in version log
                {Legend:#,prop}     array; access property from zero-based Legend entry '#'. Properties; key, definition, keyNum
                {SummaryCount}      single; fetches the number of summaries in given version log
                {Summary:#}         array; access summary part from zero-based Summary entry '#'


            > Specific to STEAM FORMAT RULES
                - https://steamcommunity.com/comment/Guide/formattinghelp
                - Applies to a line, must be at the beginning of line. Any plain text must follow the 'plain text' syntax ("")

                syntax          outcome
                _________________________________
                $nl             next line / new line
                $h1             header text
                $b              bold text
                $u              underline
                $i              italics
                $s              strikethrough text
                $sp             spoiler text
                $np             no parse, doesn't parse format tags
                $hr             horizontal rule
                $url=abc:abc    website link (url={link}:{linkName})
                $list[or]       starts/ends a list (or - ordered)
                $*              ^ list item
                $q="abc":"abc"  quoted text (q={author}:{quotedText})
                $c              code text; fixed witdth font, preserves space
                $table[nb,ec]   starts/ends a table (nb - no border, ec - equal cells)
                $th="abc","abc" ^ adds a table header. Separate columns with ','
                $td="abc","abc" ^ adds a table row. Separate columns with ','
                

            --------------------------------------
            Sec2 - ACTING AS AN IDE
            -----
            > Color coding
                data type       foreEcolor      
                ---------------------------------
                comment         Accent
                code            Normal
                keyword         Correction
                plain text      Input
                reference       Highlight
                erorr message   Incorrection

            > Error checking and messaging
                ...

             **/
        }
        // TO BE DONE
        static void SubPage_GenerateSteamLog()
        {
            Program.LogState("Generate Steam Log|Log Generation (WIP)");
            TextLine("\n\n-- Openned 'Generate Steam Log' subpage --");
            Pause();
        }
    }
}
