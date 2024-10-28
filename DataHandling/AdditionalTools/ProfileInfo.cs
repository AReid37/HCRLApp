using System;
using System.Collections.Generic;
using HCResourceLibraryApp.Layout;

namespace HCResourceLibraryApp.DataHandling
{
    public struct ProfileInfo
    {
        public string profileID, profileName;
        /// <summary>3-digit strring representing indexing of <see cref="ForECol"/> enum as options 0 through 8.</summary>
        public string profileStyleKey;
        /// <summary>Profile short description. Limit of <see cref="ProfileHandler.profDescLim"/>. May be null.</summary>
        public string profileDescription;
        public ProfileIcon profileIcon;


        public ProfileInfo(string profID, string profName, ProfileIcon profIcon, string profStyleKey, string profDesc)
        {
            profileID = null;
            profileName = null;
            profileIcon = ProfileIcon.StandardUserIcon;
            profileStyleKey = null;
            profileDescription = null;

            if (profID.IsNotNEW() && profName.IsNotNEW() && profStyleKey.IsNotNEW() && profDesc.IsNotEW())
            {
                profileID = profID;
                profileName = profName;
                profileIcon = profIcon;
                profileStyleKey = profStyleKey;
                profileDescription = profDesc;
            }            
        }


        public string EncodeProfileInfo()
        {
            // Syntax: {profID}%{profName}%{profIcon}%{profStyleKey}%{profDesc}
            //         - profDesc, may be null
            string encodeString = "";
            const string sep = DataHandlerBase.Sep;
            if (IsSetupQ())
                encodeString = $"{profileID}{sep}{profileName}{sep}{profileIcon}{sep}{profileStyleKey}{sep}{profileDescription}";
            return encodeString;
        }
        public bool DecodeProfileInfo(string profileInfoString)
        {
            // Syntax: {profID}%{profName}%{profIcon}%{profStyleKey}%{profDesc}
            //         - profDesc, may be null
            bool decodedInfoQ = false;
            if (profileInfoString.IsNotNEW())
            {
                string[] profInfos = profileInfoString.Split(DataHandlerBase.Sep);
                if (profInfos.HasElements(5))
                {
                    for (int px = 0; px < profInfos.Length; px++)
                    {
                        string profInfoPiece = profInfos[px];
                        switch (px)
                        {
                            // prof ID
                            case 0:
                                profileID = profInfoPiece;
                                break;
                            // prof Name
                            case 1:
                                profileName = profInfoPiece;
                                break;
                            // prof Icon
                            case 2:
                                Enum.TryParse(typeof(ProfileIcon), profInfoPiece, out object profIcon);
                                profileIcon = (ProfileIcon)profIcon;
                                break;
                            // prof style key
                            case 3:
                                profileStyleKey = profInfoPiece;
                                break;
                            // prof desc
                            case 4:
                                if (profInfoPiece.IsNotEW())
                                    profileDescription = profInfoPiece;
                                break;
                        }
                    }
                }

                decodedInfoQ = IsSetupQ();
            }

            return decodedInfoQ;
        }
        /// <summary>Has this instance of <see cref="ProfileInfo"/> been initialized with the appropriate information?</summary>
        /// <returns>A boolean stating whether the profile ID, profile name, and profile description has been given values. Description may be null.</returns>
        public readonly bool IsSetupQ()
        {
            return profileID.IsNotNEW() && profileName.IsNotNEW() && profileStyleKey.IsNotNEW() && profileDescription.IsNotEW();
        }
        public override string ToString()
        {
            return $"PI:{EncodeProfileInfo().Clamp(80, "...")}".Replace(DataHandlerBase.Sep, ";");
        }
    }
}
