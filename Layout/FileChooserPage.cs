using System;
using System.Collections.Generic;
using System.IO;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.Layout
{
    public class FileChooserPage
    {
        static readonly char subMenuUnderline = '|';
        static string _selectedItem, _ext;
        static FileChooserType _itemType;
        static int iniCurTop, iniCurLeft;

        /// <summary>The file or folder selected post-browsing.</summary>
        public static string SelectedItem
        {
            get => _selectedItem;
            //get
            //{
            //    string selectedItem = null;
            //    if (_selectedItem.IsNotNEW())
            //    {
            //        selectedItem = _selectedItem;
            //        if (!IsEnterFileChooserPageQueued())
            //            _selectedItem = null;
            //    }
            //    return selectedItem;
            //}
            private set => _selectedItem = value;
        }
        public static string Extension
        {
            get => _ext;
            set => _ext = value.IsNotNEW() ? value.Replace(" ", "") : null;
        }
        /// <summary>Options on whether to select files only, directories only, or both.</summary>
        public static FileChooserType ItemType
        {
            get => _itemType;
            set => _itemType = value; 
        }


        /// <param name="startingDir">Choose a folder / directory to begin browsing from.</param>
        public static void OpenPage(string startingDir = null)
        {
            bool exitFileChooserPageQ = false;
            if (IsEnterFileChooserPageQueued())
            {
                Dbg.StartLogging("FileChooserPage.OpenPage()", out int fcpx);

                SelectedItem = null;
                HSNLPrint(2, 4);

                GetCursorPosition();
                const string nextPage = ">", prevPage = "<";
                const string prevDirKey = @"..\", fileKey = "[f]", dirKey = "[D]", errKey = "[!]";
                const int pageIndexMin = 0;
                int maxFilesPerPage = 10 + HSNL(0, 10), pageIndex = pageIndexMin;
                int maxNameLength = 70 + WSLL(0, 50);

                DirectoryInfo startDir;
                bool dirExistsQ = true;
                string currDirectory = @"C:\";
                if (startingDir.IsNotNEW())
                {
                    startDir = new DirectoryInfo(startingDir);
                    if (startDir.Exists)
                        currDirectory = startDir.FullName;
                    dirExistsQ = startDir.Exists;
                }


                Dbg.Log(fcpx, $"Prepared File Browsing Info; Starting Directory :: [{currDirectory}], Exists? {dirExistsQ}; ");
                Dbg.Log(fcpx, $"Proceeding to browsing;   //   target [{ItemType}], iniCursorPos (Top,Left) [{iniCurTop}, {iniCurLeft}], maxItems/page [{maxFilesPerPage}], maxNameLen [{maxNameLength}];");
                int dbgCycleCount = 0;
                do
                {
                    dbgCycleCount++;
                    Dbg.LogPart(fcpx, $"Cycle #{dbgCycleCount}  //  ");
                    /// destroyPerLine; used to track character distance on each line so that they may be 'destroyed' perfectly, per end of cycle
                    List<int> destroyPerLine = new();

                    SetCursorPosition();
                    Title("File Chooser", subMenuUnderline, 0);
                    NoteDestroyLen(true);

                    List<string> currDirItems = new();
                    List<bool> currDirItemsIsDirQ = new();
                    DirectoryInfo currDir = new(currDirectory);
                    bool hasParentDirQ = false;
                    string directoryName = "";

                    // -- DIRECTORY DISPLAY --
                    /// fetch directory information
                    if (currDir.Exists)
                    {
                        Dbg.LogPart(fcpx, $"Using Dir [{currDir.FullName}] -- ");
                        directoryName = currDir.FullName;
                        if (currDir.Parent != null)
                        {
                            DirectoryInfo currDirParent = currDir.Parent;
                            currDirItems.Add(currDirParent.FullName);
                            currDirItemsIsDirQ.Add(true);
                            hasParentDirQ = true;
                        }

                        EnumerationOptions eo_dirIgnoreInaccessible = new();
                        eo_dirIgnoreInaccessible.IgnoreInaccessible = true;

                        bool noAccessDirsQ = false, noAccessFilesQ = false;
                        DirectoryInfo[] subDirs = new DirectoryInfo[0];
                        FileInfo[] dirFiles = new FileInfo[0];
                        try
                        {
                            subDirs = currDir.GetDirectories();
                        }
                        catch { noAccessDirsQ = true; }; /// try to get sub-dirs
                        try
                        {
                            dirFiles = currDir.GetFiles();
                        } 
                        catch { noAccessFilesQ = true; }; /// try to get files

                        if (subDirs.HasElements())
                        {
                            foreach (DirectoryInfo subDir in subDirs)
                            {
                                currDirItems.Add(subDir.FullName);
                                currDirItemsIsDirQ.Add(true);
                            }
                        }
                        else if (noAccessDirsQ)
                        {
                            currDirItems.Add($"{errKey} Directory's sub-directories could not be fetched (Access denied).");
                            currDirItemsIsDirQ.Add(true);
                        }

                        if (dirFiles.HasElements())
                        {
                            foreach (FileInfo dirFile in dirFiles)
                            {
                                currDirItems.Add(dirFile.FullName);
                                currDirItemsIsDirQ.Add(false);
                            }
                        }
                        else if (noAccessFilesQ)
                        {
                            currDirItems.Add($"{errKey} Directory's files could not be fetched (Access denied).");
                            currDirItemsIsDirQ.Add(false);
                        }

                        Dbg.LogPart(fcpx, $"Dir Exists, fetched [{subDirs.Length}] sub-dirs and [{dirFiles.Length}] files, has parent dir? [{hasParentDirQ}]");
                    }
                    Dbg.Log(fcpx, "; ");
                    Dbg.NudgeIndent(fcpx, true);


                    /// display directory information and menu
                    List<string> validOpts = new();
                    if (directoryName.IsNotNEW())
                    {
                        /// directory name
                        //ChangeNextHighlightColors(ForECol.Accent, ForECol.Normal);
                        Highlight(true, directoryName.Clamp(maxNameLength, @"\..", currDir.Name, false), currDir.Name);
                        NewLine();
                        NoteDestroyLen(true);
                        NoteDestroyLen();

                        if (currDirItems.HasElements())
                        {
                            int totalPages = (currDirItems.Count / maxFilesPerPage) + (currDirItems.Count % maxFilesPerPage != 0 ? 1 : 0);
                            int dxIni = 0 + (pageIndex * maxFilesPerPage);
                            Dbg.Log(fcpx, $"Directory Name [{currDir.Name}], total Pages [{totalPages}], page Num [{pageIndex + 1}], initial index [{dxIni}]; ");
                            FormatLine($"Page {pageIndex + 1} of {totalPages}.", ForECol.Accent);
                            NoteDestroyLen(true);

                            for (int dx = dxIni; dx < currDirItems.Count && dx - dxIni < maxFilesPerPage; dx++)
                            {
                                string itemFullName = currDirItems[dx];
                                string itemName = itemFullName.Replace(directoryName, @"~\").Replace(@"\\", @"\").Clamp(maxNameLength, "...");
                                bool isDirQ = currDirItemsIsDirQ[dx];
                                bool isValidOptQ = false, isErrorMsgQ = false;
                                int itemNum = dx - dxIni + 1;
                                
                                /// for parent directory
                                if (directoryName.StartsWith(itemFullName) && isDirQ)
                                    itemName = prevDirKey;
                                /// for error messages
                                if (itemFullName.StartsWith(errKey))
                                    isErrorMsgQ = true;

                                /// validating option numbers
                                if ((ItemType == FileChooserType.Files || ItemType == FileChooserType.All) && !isDirQ)
                                    isValidOptQ = true;
                                if ((ItemType == FileChooserType.Folders || ItemType == FileChooserType.All) && isDirQ)
                                    isValidOptQ = true;

                                /// print number and lable of each item whether file or folder
                                Dbg.LogPart(fcpx, $" > Item @{dx}  ::  ");
                                if (!isErrorMsgQ)
                                {
                                    Dbg.LogPart(fcpx, $"#[{itemNum}], name [{itemName}], isDirQ [{isDirQ}], validQ [{isValidOptQ}], fullName [{itemFullName}]");
                                    Format($"{Ind24}{itemNum,-2} ", isValidOptQ ? ForECol.Normal : ForECol.Accent);
                                    Format($"{(isDirQ ? dirKey : fileKey)} ", ForECol.Accent);
                                    Format($"{itemName}{(isDirQ ? @"\" : "")}", ForECol.Highlight);                                    
                                    Dbg.Log(fcpx, "; ");
                                }
                                else
                                {
                                    Format($"{Ind24}-- ", ForECol.Accent);
                                    Format(itemFullName, ForECol.Incorrection);
                                    Dbg.Log(fcpx, $"Error message recieved :: [{itemFullName}]; ");
                                    itemNum = -1;
                                }
                                NoteDestroyLen();
                                NewLine();

                                if (itemNum > 0)
                                    validOpts.Add(itemNum.ToString());
                            }
                        }
                        else
                        {
                            Dbg.Log(fcpx, "Current directory is empty, completely empty; ");
                            Format("This Directory Is Empty. Browsing cancelled.", ForECol.Incorrection);
                            NoteDestroyLen();
                            Pause();
                            exitFileChooserPageQ = true;
                        }
                    }
                    else
                    {
                        Dbg.Log(fcpx, "Could not fetch information on current directory; ");
                        Format("Failed to obtain information on current directory. Browsing cancelled.", ForECol.Incorrection);
                        NoteDestroyLen();
                        Pause();
                        exitFileChooserPageQ = true;
                    }


                    /// input; navigation and submission
                    if (!exitFileChooserPageQ)
                    {
                        int maxPageIndex = (currDirItems.Count / maxFilesPerPage) + (currDirItems.Count % maxFilesPerPage != 0 ? 0 : -1);

                        NewLine();
                        NoteDestroyLen();

                        FormatLine($"{Ind14}Use '{prevPage}' and '{nextPage}' to navigate pages. Press [Enter] to exit page.", ForECol.Accent);
                        Format($"{Ind14}Select number of item to submit >> ");
                        NoteDestroyLen(true);
                        NoteDestroyLen(true);

                        string input = StyledInput("##");
                        bool isValidQ = MenuOptions(input, out short optNum, validOpts.ToArray());
                        if (isValidQ && int.TryParse(input, out _))
                        {
                            int itemIndex = pageIndex * maxFilesPerPage + optNum;
                            string itemFullName = currDirItems[itemIndex];
                            bool isDirQ = currDirItemsIsDirQ[itemIndex];
                            string itemName = (itemFullName.Replace(currDirectory, $"{(hasParentDirQ ? "..\\" : "")}{currDir.Name}\\") + (isDirQ ? "\\" : "")).Replace(@"\\", @"\").Clamp(maxNameLength, "...");

                            bool isValidTypeQ = false;
                            if ((ItemType == FileChooserType.Files || ItemType == FileChooserType.All) && !isDirQ)
                                isValidTypeQ = true;
                            if ((ItemType == FileChooserType.Folders || ItemType == FileChooserType.All) && isDirQ)
                                isValidTypeQ = true;

                            Dbg.Log(fcpx, $"Selected item @{itemIndex}  ::  isDir [{isDirQ}], name [{itemName}], fullName [{itemFullName}]; ");
                            if (isValidTypeQ)
                            {
                                Dbg.LogPart(fcpx, "Item is VALID; ");
                                NewLine();
                                NoteDestroyLen();

                                FormatLine($"{Ind24}Selected item: ");
                                NoteDestroyLen(true);
                                Format($"\t{itemName}", ForECol.Highlight);
                                FormatLine(".");
                                NoteDestroyLen(true);

                                string dirEnterText = isDirQ ? " (Entering this directory)" : "";
                                Confirmation($"{Ind24}Confirm selected item? ", true, out bool yesNo, $"{Ind34}Selected item confirmed.", $"{Ind34}Selected item discarded{(dirEnterText)}.");
                                NoteDestroyLen(true);
                                NoteDestroyLen(true);

                                Dbg.LogPart(fcpx, $"Item confirmed? [{yesNo}]");
                                if (yesNo)
                                {
                                    SelectedItem = itemFullName;
                                    exitFileChooserPageQ = true;
                                    Dbg.LogPart(fcpx, "; Exiting File Chooser page, an item was selected");
                                }
                                else if (isDirQ)
                                    isValidTypeQ = false; /// enter given dir on decline
                                Dbg.Log(fcpx, "; ");
                            }
                            
                            if (!isValidTypeQ)
                            {
                                Dbg.LogPart(fcpx, $"Item invalidated; ");
                                if (isDirQ)
                                {
                                    currDirectory = itemFullName;
                                    pageIndex = pageIndexMin;
                                    Dbg.LogPart(fcpx, "Item is a directory; Page will be refreshed with items from selected directory; ");
                                }
                            }
                        }
                        else
                        {
                            Dbg.LogPart(fcpx, "Invalid option (NaN); ");
                            if (input == nextPage || input == prevPage)
                            {
                                Dbg.LogPart(fcpx, "Navigating pages: ");
                                if (maxPageIndex > pageIndexMin)
                                {
                                    if (input == nextPage && pageIndex < maxPageIndex)
                                    {
                                        pageIndex += 1;
                                        Dbg.LogPart(fcpx, " to next");
                                    }
                                    if (input == prevPage && pageIndex > pageIndexMin)
                                    {
                                        pageIndex -= 1;
                                        Dbg.LogPart(fcpx, " to previous");
                                    }
                                }
                                else Dbg.Log(fcpx, " no pages to navigate");
                            }
                            else if (input.IsNE())
                            {
                                exitFileChooserPageQ = true;
                                Dbg.LogPart(fcpx, "Exiting file chooser page");
                            }
                            Dbg.Log(fcpx, "; ");
                        }
                    }


                    /// destroy sub-page
                    #region destroySubPage
                    SetCursorPosition();
                    if (destroyPerLine.HasElements())
                    {
                        Dbg.LogPart(fcpx, "- Destroying previous prints ::");

                        int bufferWidth = Console.BufferWidth;
                        const char desChar = ' '; // DBG'cLS'   OG' '
                        foreach (int destLen in destroyPerLine)
                        {
                            Dbg.LogPart(fcpx, $" {destLen}");
                            string destroyStr = desChar.ToString();
                            for (int dsx = 0; dsx < destLen - 1; dsx++)
                                destroyStr += desChar.ToString();
                            Text(destroyStr, GetPrefsForeColor(ForECol.Accent));

                            if (destLen < bufferWidth)
                                NewLine();
                        }
                        Dbg.Log(fcpx, "; ");
                    }
                    Wait(0.1f);
                    //Pause();
                    #endregion

                    Dbg.NudgeIndent(fcpx, false);



                    // LOCAL METHOD - for destroyPerLineList
                    void NoteDestroyLen(bool useBufferWidth = false)
                    {
                        int charDist;
                        if (useBufferWidth)
                            charDist = Console.BufferWidth;
                        else charDist = Console.CursorLeft;
                        destroyPerLine.Add(charDist);
                        //Dbg.LogPart(fcpx, $"NDL({charDist}); ");
                    }
                }
                while (!exitFileChooserPageQ);

                /// page ending stuff
                SetCursorPosition(iniCurTop, iniCurLeft);
                Wait(0.1f);

                Dbg.LogPart(fcpx, "Returned cursor to original position");
                if (SelectedItem.IsNotNEW())
                {
                    Dbg.LogPart(fcpx, "; Printing final item path (as input)");
                    TextLine(SelectedItem, GetPrefsForeColor(ForECol.InputColor));
                    //FormatLine(SelectedItem, ForECol.InputColor);
                }
                else NewLine();
                Dbg.Log(fcpx, "; ");

                Dbg.EndLogging(fcpx);
                UnqueueEnterFileChooserPage();                
            }
        }
        public static void SetInitialCursorPos()
        {
            iniCurTop = Console.CursorTop;
            iniCurLeft = Console.CursorLeft;

            //Format(cLS.ToString(), ForECol.Accent);
        }
    }
}
