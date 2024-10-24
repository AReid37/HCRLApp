using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    public struct ProfileInfo
    {
        public string profileID, profileName, profileStyleKey, profileDescription;
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


        public bool IsSetupQ()
        {
            return profileID.IsNotNEW() && profileName.IsNotNEW() && profileStyleKey.IsNotNEW() && profileDescription.IsNotEW();
        }
    }
}
