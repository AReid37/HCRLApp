using System;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Extensions;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>A ...</summary>
    public class ProfileHandler
    {
        /** PLANNING - Profile Handler
        * 
        * FIELDS / PROPERTIES
        *  - cnst str profTag = "prf"
        *  - str profInfoFile = @"hcd\profiles.txt"
        *  - int activeProfileIx
        *  - pbl str[] allProfiles
        *  - pbl str currProfileID
        *  - pbl str currProfileName
        *  - pbl DataHandlerBase dataHandler
        *  - pbl Preferences preferences
        *  - pbl LogDecoder logDecoder
        *  - pbl ContentValidator contentValidator
        *  - pbl ResLibrary resourceLibrary
        *  - pbl SFormatterData formatterData
        *  - pbl BugIdeaData bugIdeaData
        * 
        * 
        * CONSTRUCTORS
        * - pbl ProfileHandler()
        * - pbl ProfileHandler(params[] DataHandler dataHandlers)
        * 
        * 
        * METHODS
        * - pbl bl CreateProfile()
        * - pbl vd SwitchProfile(PH profile)
        * - pbl bl DeleteProfile(PH profile)
        * - pbl PH[] FetchProfiles()
        * - pbl bl LoadProfile(str profID)
        * - pbl bl SaveCurrentProfile()
        * - pbl bl SetCurrentProfileName() 
        * - pbl bl ChangesMade()
        * - pbl bl IsSetup() 
        * - pbl ovr ToString()
        * - str ProfileID()
        * 
        */

        #region Fields / Props
        // PRIVATE
        const int profIDLim = 5, profNameMin = 3, profNameLim = 30, profDescLim = 120;
        const string profTag = "prf", profIconTag = "i:", profStyleKeyDefualt = "602";
        public const string NoProfID = "-1000";
        /// <summary>A file that stores information on the existing profile folders and which one was most recently active.</summary>
        string ProfilesFile = !Program.isDebugVersionQ ? @"hcd\profiles.txt" : @"C:\Users\ntrc2\Pictures\High Contrast Textures\HCRLA\hcd-tests\profiles.txt";
        string _currProfileID, _currProfileName, _currProfDescription, _currProfStyleKey, _prevCurrProfileID, _prevCurrProfileName, _prevCurrDescription, _prevCurrProfStyleKey;
        ProfileIcon _currProfIcon, _prevCurrProfIcon;
        List<string> _allProfiles, _prevAllProfiles;
        

        // PUBLIC
        /// <summary>A 5-digit unique number that specifies the currently active profile.</summary>
        public string CurrProfileID
        {
            get
            {
                string currProfID = null;
                if (_currProfileID.IsNotEW())
                    currProfID = _currProfileID;
                return currProfID;
            }
            private set
            {
                if (value.IsNotNEW())
                    _currProfileID = value;
            }
        }
        /// <summary>The name of the currently active profile.</summary>
        public string CurrProfileName
        {
            get
            {
                string currProfName = null;
                if (_currProfileName.IsNotEW())
                    currProfName = _currProfileName;
                return currProfName;
            }
            set
            {
                if (value.IsNotNEW())
                    _currProfileName = value;
            }
        }
        /// <summary>Icon style of the currently active profile.</summary>
        public ProfileIcon CurrProfileIcon
        {
            get => _currProfIcon;
            set => _currProfIcon = value;
        }
        /// <summary>
        /// A 3-digit string that determines the order of the colors to use for profile icon colors. Ranges from 0~8 representing <see cref="ForECol"/> as color options.
        /// </summary>
        public string CurrProfileStyleKey
        {
            get => _currProfStyleKey;
            set => _currProfStyleKey = value;
        }
        /// <summary>A short description of the purpose of the profile, user generated.</summary>
        public string CurrProfileDescription
        {
            get => _currProfDescription;
            set
            {
                if (value.IsNotEW())
                    _currProfDescription = value;
            }
        }
        /// <summary>
        /// For Profile Selection. Each entry contains :: profile ID, profile Name, profile Icon Name, profile Style Key, profile Description, and library content count.
        /// </summary>
        public List<ProfileInfo> AllProfiles;
        

        public DataHandlerBase dataHandler;
        public Preferences preferences;
        public LogDecoder logDecoder;
        public ContentValidator contentValidator;
        public ResLibrary resourceLibrary;
        public SFormatterData formatterData;
        public BugIdeaData bugIdeaData;
        #endregion



        // CONSTRUCTORS
        /// <summary>Initializes a ProfileHandler instance with default initialization of data handlers.</summary>
        public ProfileHandler()
        {
            // initialize
            CurrProfileID = NoProfID;
            CurrProfileName = $"Profile{CurrProfileID}";
            CurrProfileIcon = ProfileIcon.StandardUserIcon;
            CurrProfileStyleKey = profStyleKeyDefualt;
            CurrProfileDescription = null;
            AllProfiles = new List<ProfileInfo>();

            dataHandler = new DataHandlerBase();
            preferences = new Preferences();
            logDecoder = new LogDecoder();
            contentValidator = new ContentValidator();
            resourceLibrary = new ResLibrary();
            formatterData = new SFormatterData();
            bugIdeaData = new BugIdeaData();
        }
        /// <summary>Initializes a ProfileHandler instance with data handlers with already existing information. Profile integration from previous versions.</summary>
        /// <param name="dataHandlers">
        ///     The index of the DataHandlers should be arranged in the following order:<br></br>
        ///     <see cref="DataHandlerBase"/>, <see cref="Preferences"/>, <see cref="LogDecoder"/>, <see cref="ContentValidator"/>, <see cref="ResLibrary"/>, <see cref="SFormatterData"/>, <see cref="BugIdeaData"/>. <br></br>
        ///     All are required; there must be a total of 7 data handlers.
        /// </param>
        public ProfileHandler(params DataHandlerBase[] dataHandlers)
        {
            // initialize
            CurrProfileID = ProfileID();
            CurrProfileName = $"Profile{CurrProfileID}";
            CurrProfileIcon = ProfileIcon.StandardUserIcon;
            CurrProfileStyleKey = profStyleKeyDefualt;
            CurrProfileDescription = null;
            AllProfiles = new List<ProfileInfo>();

            dataHandler = new DataHandlerBase();
            preferences = new Preferences();
            logDecoder = new LogDecoder();
            contentValidator = new ContentValidator();
            resourceLibrary = new ResLibrary();
            formatterData = new SFormatterData();
            bugIdeaData = new BugIdeaData();

            if (dataHandlers.HasElements(7))
            {
                // Each DataHandler (general) is checked to not be null and be setup, otherwise the related field remains empty.
                for (int dx = 0; dx < 7; dx++)
                {
                    DataHandlerBase generalDataHandler = dataHandlers[dx];
                    if (generalDataHandler != null)
                    {
                        if (generalDataHandler.IsSetup())
                        {
                            switch (dx)
                            {
                                case 0:
                                    dataHandler = generalDataHandler;
                                    break;

                                case 1:
                                    preferences = (Preferences)generalDataHandler;
                                    break;

                                case 2:
                                    logDecoder = (LogDecoder)generalDataHandler;
                                    break;

                                case 3:
                                    contentValidator = (ContentValidator)generalDataHandler;
                                    break;

                                case 4:
                                    resourceLibrary = (ResLibrary)generalDataHandler;
                                    break;

                                case 5: 
                                    formatterData = (SFormatterData)generalDataHandler;
                                    break;

                                case 6:
                                    bugIdeaData = (BugIdeaData)generalDataHandler;
                                    break;
                            }
                        }
                    }                    
                }
            }
        }



        #region Methods
        /** A little Profile Functionality Breakdown
         *      Fetch Profiles  -  Gets the array of profiles that have been saved from profiles.txt. Also auto-loads previously active profile data
         *      Create Profile  -  Generates a Profile Folder of "Profile-{ProfileID}" and saves all data handler information to that folder
         *      Delete Profile  -  Removes the Profile Folder of "ProfileID" and all contents within
         *      Load Profile    -  Uses DataHandlerBase to get information for a profile by providing it the associated profile folder location to read from
         *      Save Profile    -  Uses DataHandlerBase to save information for a profile by providing it the associated profile folder location to write to
         *      Switch Profile  -  Changes the Profile Folder accessible to DataHandlerBase by the Profile ID
         */

        // PUBLIC METHODS //
        /// <summary>Reads from location of <see cref="ProfilesFile"/> and arranges any saved profile information into the <see cref="AllProfiles"/> list. <br></br> Will generate an empty file if it does not exist. Additionally responsible for auto-loading profile based on activity.</summary>
        public void FetchProfiles()
        {
            // tbd... 1st...
        }



        // PRIVATE METHODS //
        string ProfileID()
        {
            /// profile ID; A 5-digit unique number for an active profile instance.
            string profID = GenerateID();
            if (AllProfiles.HasElements())
            {
                for (int px = 0; px < AllProfiles.Count; px++)
                {
                    ProfileInfo pInfo = AllProfiles[px];
                    if (pInfo.profileID == profID)
                    {
                        /// if a duplicate is found. Reassign profile ID and restart checks for any dupes.
                        profID = GenerateID();
                        px = 0;
                    }
                }
            }
            return profID;


            /// internal method
            static string GenerateID()
            {
                int idNum = Random(0, 99999);
                string pID = idNum.ToString();
                pID = pID.PadLeft(5).Replace(" ", "0");
                return pID;
            }
        }
        #endregion

    }
}
