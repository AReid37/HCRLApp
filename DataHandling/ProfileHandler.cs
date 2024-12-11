using System;
using System.Collections.Generic;
using ConsoleFormat;
using HCResourceLibraryApp.Layout;
using static HCResourceLibraryApp.Extensions;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>An instance treated as static to oversee the arrangement of profile-specific information.</summary>
    public static class ProfileHandler
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
        * CONSTRUCTORS [X]    -- class changed to 'static'
        * - pbl ProfileHandler()
        * - pbl ProfileHandler(params[] DataHandler dataHandlers)
        *   vv
        *   'pbl ProfileHandler' replaced as 'pbl stc vd Initialize'
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
        const int profCountLim = 7, profIDLim = 5, profNameMin = 3, profNameLim = 30, profDescLim = 120;
        const string profFileStamp = "- Profiles Info List -", profTag = "prf", profStyleKeyDefault = "602";
        public const string NoProfID = "-1000";
        /// <summary>A file that stores information on the existing profile folders and which one was most recently active.</summary>
        static string ProfilesFile = !Program.isDebugVersionQ ? @"hcd\profiles.txt" : @"C:\Users\ntrc2\Pictures\High Contrast Textures\HCRLA\hcd-tests\profiles.txt";
        static string _currProfileID, _currProfileName, _currProfDescription, _currProfStyleKey, _prevCurrProfileID, _prevCurrProfileName, _prevCurrProfDescription, _prevCurrProfStyleKey;
        static ProfileIcon _currProfIcon, _prevCurrProfIcon;
        static List<ProfileInfo> _allProfiles, _prevAllProfiles;
        static bool _initializedQ, _externalProfileDetectedQ;


        // PUBLIC
        public const int ProfileNameMinimum = profNameMin;
        public const int ProfileNameLimit = profNameLim;
        public const int ProfileDescriptionLimit = profDescLim;

        /// <summary>A 5-digit unique number that specifies the currently active profile.</summary>
        public static string CurrProfileID
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
                {
                    if (_currProfileID.IsNotNEW())
                        _prevCurrProfileID = _currProfileID;
                    _currProfileID = value;
                }
            }
        }
        /// <summary>The name of the currently active profile.</summary>
        public static string CurrProfileName
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
        public static ProfileIcon CurrProfileIcon
        {
            get => _currProfIcon;
            set => _currProfIcon = value; /// previous assigned with SetPreviousSelf()
        }
        /// <summary>
        /// A 3-digit string that determines the order of the colors to use for profile icon colors. Ranges from 0~8 representing <see cref="ForECol"/> as color options.
        /// </summary>
        public static string CurrProfileStyleKey
        {
            get
            {
                string currProfStyleKey = profStyleKeyDefault;
                if (_currProfStyleKey.IsNotNEW())
                    currProfStyleKey = _currProfStyleKey;
                return currProfStyleKey;
            }
            set
            {
                if (value.IsNotEW())
                    _currProfStyleKey = value; /// previous assigned with SetPreviousSelf()
            }
        }
        /// <summary>A short description of the purpose of the profile, user generated.</summary>
        public static string CurrProfileDescription
        {
            get => _currProfDescription;
            set
            {
                if (value.IsNotNEW())
                    _currProfDescription = value; /// previous assigned with SetPreviousSelf()
                else _currProfDescription = null;
            }
        }
        /// <summary>
        /// For Profile Selection. Each entry contains :: profile ID, profile name, profile icon name, profile style key, and profile description.
        /// </summary>
        public static List<ProfileInfo> AllProfiles
        {
            get => _allProfiles;
            set => _allProfiles = value; /// previous assigned with SetPreviousSelf()
        }
        /// <summary>
        ///     A number that determines how many more profiles may be created by the difference of <see cref="profCountLim"/> and the count of <see cref="AllProfiles"/>
        /// </summary>
        public static int RemainingProfileSpacesLeft
        {
            get
            {
                int remainder = profCountLim;
                if (AllProfiles.HasElements())
                    remainder -= AllProfiles.Count;
                return remainder;
            }
        }
        public static bool ExternalProfileDetectedQ { get => _externalProfileDetectedQ; }


        public static DataHandlerBase dataHandler;
        public static Preferences preferences;
        public static LogDecoder logDecoder;
        public static ContentValidator contentValidator;
        public static ResLibrary resourceLibrary;
        public static SFormatterData formatterData;
        public static BugIdeaData bugIdeaData;
        #endregion



        #region Methods
        /** A little Profile Functionality Breakdown
         *      Fetch Profiles  -  Gets the array of profiles that have been saved from profiles.txt. Also auto-loads previously active profile data
         *      Create Profile  -  Generates a Profile Folder of "Profile-{ProfileID}" and saves all data handler information to that folder
         *      Delete Profile  -  Removes the Profile Folder of "ProfileID" and all contents within
         *      Load Profile    -  Uses DataHandlerBase to get information for a profile by providing it the associated profile folder location to read from
         *      Save Profile    -  Uses DataHandlerBase to save information for a profile by providing it the associated profile folder location to write to
         *      Switch Profile  -  Changes the Profile Folder accessible to DataHandlerBase by the Profile ID
         *      
         *      ProfilesFile.txt layout
         *      -> L1 - profile file header stamp
         *      -> L2 - active profile ID  %%%  profile count
         *      -> L3+ - profile info string  {}
         *      
         *      
         *      Saving Location differences
         *      - Non-profile   -> user/hcd/data.txt
         *      - Profile       -> user/hcd/profID/data.txt
         *          NOTE :: non-profile data will not be deleted but can no longer be accessed through the application (active profile override)
         */

        // PUBLIC METHODS //
        /// <summary>Initializes a ProfileHandler instance with default initialization of data handlers.</summary>
        public static void Initialize()
        {
            // initialize
            CurrProfileID = NoProfID;
            CurrProfileName = $"Profile{CurrProfileID}";
            CurrProfileIcon = ProfileIcon.StandardUserIcon;
            CurrProfileStyleKey = profStyleKeyDefault;
            CurrProfileDescription = null;
            AllProfiles = new List<ProfileInfo>();            

            _initializedQ = true;
            AdoptDataHandlers();
            SetPreviousSelf();
        }
        /// <summary>
        ///     OUTDATED PERHAPS
        ///     Initializes a ProfileHandler instance with data handlers with already existing information. Profile integration from previous versions.
        /// </summary>
        /// <param name="dataHandlers">
        ///     The index of the DataHandlers should be arranged in the following order:<br></br>
        ///     <see cref="DataHandlerBase"/>, <see cref="Preferences"/>, <see cref="LogDecoder"/>, <see cref="ContentValidator"/>, <see cref="ResLibrary"/>, <see cref="SFormatterData"/>, <see cref="BugIdeaData"/>. <br></br>
        ///     All are required; there must be a total of 7 data handlers.
        /// </param>
        public static void Initialize(params DataHandlerBase[] dataHandlers)
        {
            // initialize
            CurrProfileID = ProfileID();
            CurrProfileName = $"Profile{CurrProfileID}";
            CurrProfileIcon = ProfileIcon.StandardUserIcon;
            CurrProfileStyleKey = profStyleKeyDefault;
            CurrProfileDescription = null;
            AllProfiles = new List<ProfileInfo>();

            _initializedQ = true;
            AdoptDataHandlers(dataHandlers);
            SetPreviousSelf();
        }
        


        /// <summary>
        /// Reads from location of <see cref="ProfilesFile"/> and arranges any saved profile information into the <see cref="AllProfiles"/> list. <br></br> Will generate an empty file if it does not exist. Additionally responsible for auto-loading the most recently active profile.
        /// </summary>
        /// <remarks>Also responsible for generating the profiles file if one doesn't exist.</remarks>
        public static bool FetchProfiles()
        {
            bool noIssues = true;

            /// i dunno, what do if can't set location? nothin'
            if (IsInitialized() && Base.SetFileLocation(ProfilesFile))
            {
                noIssues = Base.FileRead(null, out string[] profileDataLines);

                // IF profiles file exists: get profile information; ELSE create profiles file.
                if (noIssues)
                {
                    /// At least 3 elements because profile info starts on line 3 of file
                    /// L1  -> profFileStamp
                    /// L2  -> activeProfID
                    /// L3+ -> profileInfos 
                    if (profileDataLines.HasElements(3))
                    {
                        // fetch profile information \ determine last active profile
                        ProfileInfo lastActiveProfile = new();
                        string lastActiveProfID = null;
                        for (int pfx = 1; pfx < profileDataLines.Length; pfx++)
                        {       
                            string profInfoLine = profileDataLines[pfx];                            
                            bool lastActiveQ = false;

                            // IF...: get active profile ID; ELSE decode profile infos;
                            if (pfx == 1)
                                lastActiveProfID = profInfoLine;
                            else
                            {
                                ProfileInfo profInfo = new ProfileInfo();
                                if (profInfoLine.IsNotNEW())
                                {
                                    profInfo.DecodeProfileInfo(profInfoLine);
                                    lastActiveQ = profInfo.profileID == lastActiveProfID;
                                }

                                if (profInfo.IsSetupQ())
                                    AllProfiles.Add(profInfo);

                                // determine profile to autoload here
                                if (!lastActiveProfile.IsSetupQ() && lastActiveQ)
                                    lastActiveProfile = profInfo;
                            }
                        }

                        // auto-load last active profile (auto-switch to profile)
                        SwitchProfile(lastActiveProfID);
                    }
                }
                else
                {
                    noIssues = Base.FileWrite(true, null, profFileStamp);
                }

                SetPreviousSelf();
            }

            return noIssues;
        }
        /// <summary>
        ///     Changes the current profile by searching through profiles list by ID, initializing profile variables with current profiles, and loading related library data.
        /// </summary>
        /// <param name="profileID"></param>
        /// <param name="retainProfileDataQ">If <c>true</c>, will disable profile loading after switching (retains data handler information for copying to new profile).</param>
        public static void SwitchProfile(string profileID, bool retainProfileDataQ = false)
        {
            if (IsInitialized())
            {
                // find the profile with matching ID
                ProfileInfo profileToActivate = new();
                if (AllProfiles.HasElements() && profileID.IsNotNEW())
                {
                    for (int pix = 0; pix < AllProfiles.Count && !profileToActivate.IsSetupQ(); pix++)
                    {
                        ProfileInfo profInfo = AllProfiles[pix];
                        if (profInfo.profileID == profileID)
                            profileToActivate = profInfo;
                    }
                }

                // activate profile: switch handler profile info to match it, load profile data
                if (profileToActivate.IsSetupQ())
                {
                    CurrProfileID = profileToActivate.profileID;
                    CurrProfileName = profileToActivate.profileName;
                    CurrProfileIcon = profileToActivate.profileIcon;
                    CurrProfileStyleKey = profileToActivate.profileStyleKey;
                    CurrProfileDescription = profileToActivate.profileDescription;
                    SetPreviousSelf();

                    // for disabling profile loading after switching (retains dataHandler info)
                    DataHandlerBase.SetProfileDirectory(profileToActivate.profileID);
                    if (!retainProfileDataQ)
                        LoadProfile();
                }
            }            
        }
        /// <summary>
        ///     Adds a new profile to the list of profiles while under the limit of <see cref="RemainingProfileSpacesLeft"/>.
        /// </summary>
        /// <param name="newProfileInfo"><c>Not require to have a Profile ID</c>. A profile ID will be generated during creation.</param>
        /// <param name="allowDuplication">If <c>true</c>, will create a new profile that copies the currently active profile's library specific information.</param>
        /// <returns>A boolean representing the success of creating and adding the new profile to the list of profiles.</returns>
        /// <remarks>Used in combination after <see cref="AdoptDataHandlers(DataHandlerBase[])"/> to integrate an external profile from previous versions of the application.</remarks>
        public static bool CreateProfile(ProfileInfo newProfileInfo, bool allowDuplication = false)
        {
            bool createdProfQ = false;
            if (IsInitialized() && RemainingProfileSpacesLeft > 0)
            {
                newProfileInfo.profileID = ProfileID();
                if (newProfileInfo.IsSetupQ())
                {
                    createdProfQ = true;
                    AllProfiles.Add(newProfileInfo);

                    if (!allowDuplication)
                        AdoptDataHandlers();

                    SwitchProfile(newProfileInfo.profileID, allowDuplication);
                }
            }
            return createdProfQ;
        }
        /// <summary>
        ///     Updates the information of the current profile. Applies changes to every profile value except Profile ID.
        /// </summary>
        /// <param name="profileChangesInfo">The changes of the current profile.</param>
        /// <returns>A boolean reperesenting the success of updating the current profile's info.</returns>
        public static bool UpdateProfile(ProfileInfo profileChangesInfo)
        {
            bool updatedProfQ = false;
            if (IsInitialized() && profileChangesInfo.IsSetupQ())
            {
                if (CurrProfileID == profileChangesInfo.profileID)
                {
                    SetPreviousSelf();

                    CurrProfileName = profileChangesInfo.profileName;
                    CurrProfileIcon = profileChangesInfo.profileIcon;
                    CurrProfileStyleKey = profileChangesInfo.profileStyleKey;
                    CurrProfileDescription = profileChangesInfo.profileDescription;

                    GetCurrentProfile(out int profIndex);
                    if (profIndex.IsWithin(0, AllProfiles.Count))
                        AllProfiles[profIndex] = profileChangesInfo;

                    updatedProfQ = true;
                }
            }
            return updatedProfQ;
        }
        // tbd...  on curr prof
        public static bool DeleteProfile()
        {
            bool deletedProfQ = false;
            // tbd...

            /// cannot delete if there is only 1 profile (sucks to suck, there's no going back)
            return deletedProfQ;
        }
        /// <summary>
        ///     Only fetches the library data of the current profile. <see cref="FetchProfiles"/> already handles getting information on all profiles.
        /// </summary>
        /// <returns>A boolean representing the success of loading profile specific data.</returns>
        public static bool LoadProfile()
        { 
            bool noIssues = false;
            if (IsInitialized())
            {
                AdoptDataHandlers();
                // the FetchProfiles() method already handles getting info from the profile file.
                noIssues = dataHandler.LoadFromFile(preferences, logDecoder, contentValidator, resourceLibrary, formatterData, bugIdeaData);

                // checks for 1st time profile system use and success of loading data (indicates external profile specific info / pre-profile)
                if (CurrProfileID == NoProfID && noIssues)
                    _externalProfileDetectedQ = true;
            }
            return noIssues;
        }
        /// <summary>
        ///     Saves the information of all profiles and then saves the library data of the current profile.
        /// </summary>
        /// <returns>A booleaon representing the success of saving profiles info and profile specific data.</returns>
        public static bool SaveProfile()
        {
            bool noIssues = false;
            /// two parts
            /// 1. save profile info to profile file
            /// 2. regular save to hcrla Data file
            /// 

            if (IsInitialized())
            {
                // 1. Save All Profiles Info
                if (AllProfiles.HasElements())
                {
                    List<string> profilesSaveInfo = new();
                    /** ProfilesFile.txt layout
                     *  -> L1 - profile file header stamp
                     *  -> L2 - active profile ID  %%%  profile count
                     *  -> L3+ - profile info string  {}
                     */

                    for (int pfx = -2; pfx < AllProfiles.Count; pfx++)
                    {
                        // L1  stamp
                        if (pfx == -2)
                            profilesSaveInfo.Add(profFileStamp);

                        // L2  active profile ID
                        if (pfx == -1)
                            profilesSaveInfo.Add(CurrProfileID);

                        // L3+ profiles info
                        if (pfx >= 0)
                        {
                            ProfileInfo profInfoToSave = AllProfiles[pfx];
                            if (profInfoToSave.IsSetupQ())
                                profilesSaveInfo.Add(profInfoToSave.EncodeProfileInfo());
                        }
                    }

                    if (profilesSaveInfo.HasElements(3) && Base.SetFileLocation(ProfilesFile))
                    {
                        noIssues = Base.FileWrite(true, null, profilesSaveInfo.ToArray());
                        SetPreviousSelf();
                    }

                }

                // 2. Regular Library Save
                if (noIssues)
                {
                    noIssues = dataHandler.SaveToFile(preferences, logDecoder, contentValidator, resourceLibrary, formatterData, bugIdeaData);
                }
            }
            return noIssues;
        }
        /// <returns> A <see cref="ProfileInfo"/> instance with no profile id, a random profile name, default icon <see cref="ProfileIcon.StandardUserIcon"/> of style <see cref="profStyleKeyDefault"/> and no description.
        /// </returns>
        public static ProfileInfo GetDefaultProfile()
        {
            return new ProfileInfo(NoProfID, $"Profile{Random(0, 99),3}", ProfileIcon.StandardUserIcon, profStyleKeyDefault, null);
        }
        /// <param name="indexOfCurr">Returns a value representing the index at which the current profile can be found within profiles list. Is lesser than <c>0</c> if no profiles are found.</param>
        /// <returns> A <see cref="ProfileInfo"/> instance within <see cref="AllProfiles"/> that has a matches with <see cref="CurrProfileID"/>, thus the current profile info.
        /// <br></br>Instance will return <c>false</c> on <see cref="ProfileInfo.IsSetupQ()"/> when a profile cannot be found (no current profile).
        /// </returns>
        public static ProfileInfo GetCurrentProfile(out int indexOfCurr)
        {
            ProfileInfo currProf = new();
            indexOfCurr = -1;
            if (AllProfiles.HasElements())
            {
                for (int px = 0; px < AllProfiles.Count && !currProf.IsSetupQ(); px++)
                {
                    ProfileInfo thisProfInfo = AllProfiles[px];
                    if (thisProfInfo.IsSetupQ())
                    {
                        if (thisProfInfo.profileID == CurrProfileID)
                        {
                            currProf = thisProfInfo;
                            indexOfCurr = px;
                        }
                    }
                }
            }
            return currProf;
        }
        /// <summary>
        ///     Compares the current profile against its previous self to detect any changes, then compares all profiles against a previous save to detect any changes.
        /// </summary>
        /// <returns>A boolean representing the difference of two current profile states and comparison against all available profiles.</returns>
        public static bool ChangesMade()
        {
            bool changesMadeQ = false;
            if (IsInitialized())
            {
                for (int px = 0; px < 6 && !changesMadeQ; px++)
                {
                    switch (px)
                    {
                        case 0: // basically always false
                            changesMadeQ = _currProfileID != _prevCurrProfileID;
                            break;

                        case 1:
                            changesMadeQ = _currProfileName != _prevCurrProfileName;
                            break;

                        case 2:
                            changesMadeQ = _currProfIcon != _prevCurrProfIcon;
                            break;

                        case 3:
                            changesMadeQ = _currProfStyleKey != _prevCurrProfStyleKey;
                            break;

                        case 4:
                            changesMadeQ = _currProfDescription != _prevCurrProfDescription;
                            break;

                        case 5:
                            changesMadeQ = _allProfiles.HasElements() != _prevAllProfiles.HasElements();
                            if (_allProfiles.HasElements() && !changesMadeQ)
                            {
                                changesMadeQ = _allProfiles.Count != _prevAllProfiles.Count;
                                if (!changesMadeQ)
                                {
                                    for (int apx = 0; apx < _allProfiles.Count && !changesMadeQ; apx++)
                                        changesMadeQ = !_allProfiles[apx].AreEquals(_prevAllProfiles[apx]);
                                }
                            }
                            break;
                    }

                }
            }
            return changesMadeQ;
        }


        // PRIVATE METHODS //
        static bool IsInitialized()
        {
            return _initializedQ;
        }
        /// <summary>Clears all data handler instances if <c>null</c>, otherwise data handlers are assigned to the <paramref name="dataHandlers"/> provided.</summary>
        /// <param name="dataHandlers">
        ///     The index of the DataHandlers should be arranged in the following order:<br></br>
        ///     <see cref="DataHandlerBase"/>, <see cref="Preferences"/>, <see cref="LogDecoder"/>, <see cref="ContentValidator"/>, <see cref="ResLibrary"/>, <see cref="SFormatterData"/>, <see cref="BugIdeaData"/>. <br></br>
        ///     All are required; there must be a total of 7 data handlers.
        /// </param>
        static void AdoptDataHandlers(params DataHandlerBase[] dataHandlers)
        {
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
        


        static string ProfileID()
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
                pID = pID.PadLeft(profIDLim).Replace(" ", "0");
                return pID;
            }
        }
        static void SetPreviousSelf()
        {
            _prevCurrProfileID = _currProfileID; // realistically, this never changes for a profile instance...
            _prevCurrProfileName = _currProfileName;
            _prevCurrProfIcon = _currProfIcon;
            _prevCurrProfStyleKey = _currProfStyleKey;
            _prevCurrProfDescription = _currProfDescription;

            if (_prevAllProfiles.HasElements())
                _prevAllProfiles.Clear();
            else _prevAllProfiles = new List<ProfileInfo>();
            _prevAllProfiles.AddRange(_allProfiles.ToArray());
        }        
        #endregion

    }
}
