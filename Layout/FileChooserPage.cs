using System;
using System.Collections.Generic;
using System.IO;
using ConsoleFormat;
using static ConsoleFormat.Base;
using static ConsoleFormat.Minimal;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Layout.PageBase;

namespace HCResourceLibraryApp.Layout
{
    public enum FileChooserType
    {
        /// <summary>Only select file items.</summary>
        Files, 
        /// <summary>Only select directory (folder) items.</summary>
        Folders,
        /// <summary>Select either directory (folder) or file items.</summary>
        All
    }

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
                Dbug.StartLogging("FileChooserPage.OpenPage()");

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


                Dbug.Log($"Prepared File Browsing Info; Starting Directory :: [{currDirectory}], Exists? {dirExistsQ}; ");
                Dbug.Log($"Proceeding to browsing;   //   target [{ItemType}], iniCursorPos (Top,Left) [{iniCurTop}, {iniCurLeft}], maxItems/page [{maxFilesPerPage}], maxNameLen [{maxNameLength}];");
                int dbgCycleCount = 0;
                do
                {
                    dbgCycleCount++;
                    Dbug.LogPart($"Cycle #{dbgCycleCount}  //  ");
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
                        Dbug.LogPart($"Using Dir [{currDir.FullName}] -- ");
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

                        Dbug.LogPart($"Dir Exists, fetched [{subDirs.Length}] sub-dirs and [{dirFiles.Length}] files, has parent dir? [{hasParentDirQ}]");
                    }
                    Dbug.Log("; ");
                    Dbug.NudgeIndent(true);


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
                            Dbug.Log($"Directory Name [{currDir.Name}], total Pages [{totalPages}], page Num [{pageIndex + 1}], initial index [{dxIni}]; ");
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
                                Dbug.LogPart(" > Item @{dx}  ::  ");
                                if (!isErrorMsgQ)
                                {
                                    Dbug.LogPart($"#[{itemNum}], name [{itemName}], isDirQ [{isDirQ}], validQ [{isValidOptQ}], fullName [{itemFullName}]");
                                    Format($"{Ind24}{itemNum,-2} ", isValidOptQ ? ForECol.Normal : ForECol.Accent);
                                    Format($"{(isDirQ ? dirKey : fileKey)} ", ForECol.Accent);
                                    Format($"{itemName}{(isDirQ ? @"\" : "")}", ForECol.Highlight);                                    
                                    Dbug.Log("; ");
                                }
                                else
                                {
                                    Format($"{Ind24}-- ", ForECol.Accent);
                                    Format(itemFullName, ForECol.Incorrection);
                                    Dbug.Log($"Error message recieved :: [{itemFullName}]; ");
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
                            Dbug.Log("Current directory is empty, completely empty; ");
                            Format("This Directory Is Empty. Browsing cancelled.", ForECol.Incorrection);
                            NoteDestroyLen();
                            Pause();
                            exitFileChooserPageQ = true;
                        }
                    }
                    else
                    {
                        Dbug.Log("Could not fetch information on current directory; ");
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

                            Dbug.Log($"Selected item @{itemIndex}  ::  isDir [{isDirQ}], name [{itemName}], fullName [{itemFullName}]; ");
                            if (isValidTypeQ)
                            {
                                Dbug.LogPart("Item is VALID; ");
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

                                Dbug.LogPart($"Item confirmed? [{yesNo}]");
                                if (yesNo)
                                {
                                    SelectedItem = itemFullName;
                                    exitFileChooserPageQ = true;
                                    Dbug.LogPart("; Existing File Chooser page, an item was selected");
                                }
                                else if (isDirQ)
                                    isValidTypeQ = false; /// enter given dir on decline
                                Dbug.Log("; ");
                            }
                            
                            if (!isValidTypeQ)
                            {
                                Dbug.LogPart($"Item invalidated; ");
                                if (isDirQ)
                                {
                                    currDirectory = itemFullName;
                                    pageIndex = pageIndexMin;
                                    Dbug.LogPart("Item is a directory; Page will be refreshed with items from selected directory; ");
                                }
                            }
                        }
                        else
                        {
                            Dbug.LogPart("Invalid option (NaN); ");
                            if (input == nextPage || input == prevPage)
                            {
                                Dbug.LogPart("Navigating pages: ");
                                if (maxPageIndex > pageIndexMin)
                                {
                                    if (input == nextPage && pageIndex < maxPageIndex)
                                    {
                                        pageIndex += 1;
                                        Dbug.LogPart(" to next");
                                    }
                                    if (input == prevPage && pageIndex > pageIndexMin)
                                    {
                                        pageIndex -= 1;
                                        Dbug.LogPart(" to previous");
                                    }
                                }
                                else Dbug.Log(" no pages to navigate");
                            }
                            else if (input.IsNE())
                            {
                                exitFileChooserPageQ = true;
                                Dbug.LogPart("Exiting file chooser page");
                            }
                            Dbug.Log("; ");
                        }
                    }


                    /// destroy sub-page
                    #region destroySubPage
                    SetCursorPosition();
                    if (destroyPerLine.HasElements())
                    {
                        Dbug.LogPart("- Destroying previous prints ::");

                        int bufferWidth = Console.BufferWidth;
                        const char desChar = ' '; // DBG'cLS'   OG' '
                        foreach (int destLen in destroyPerLine)
                        {
                            Dbug.LogPart($" {destLen}");
                            string destroyStr = desChar.ToString();
                            for (int dsx = 0; dsx < destLen - 1; dsx++)
                                destroyStr += desChar.ToString();
                            Text(destroyStr, GetPrefsForeColor(ForECol.Accent));

                            if (destLen < bufferWidth)
                                NewLine();
                        }
                        Dbug.Log("; ");
                    }
                    Wait(0.1f);
                    //Pause();
                    #endregion

                    Dbug.NudgeIndent(false);



                    // LOCAL METHOD - for destroyPerLineList
                    void NoteDestroyLen(bool useBufferWidth = false)
                    {
                        int charDist;
                        if (useBufferWidth)
                            charDist = Console.BufferWidth;
                        else charDist = Console.CursorLeft;
                        destroyPerLine.Add(charDist);
                        //Dbug.LogPart($"NDL({charDist}); ");
                    }
                }
                while (!exitFileChooserPageQ);

                /// page ending stuff
                SetCursorPosition(iniCurTop, iniCurLeft);
                Wait(0.1f);

                Dbug.LogPart("Returned cursor to original position");
                if (SelectedItem.IsNotNEW())
                {
                    Dbug.LogPart("; Printing final item path (as input)");
                    TextLine(SelectedItem, GetPrefsForeColor(ForECol.InputColor));
                    //FormatLine(SelectedItem, ForECol.InputColor);
                }
                else NewLine();
                Dbug.Log("; ");

                Dbug.EndLogging();
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
