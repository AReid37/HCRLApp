using System;
using System.Collections.Generic;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Relays information regarding connections of loose contents during library integrations.</summary>
    public struct ResLibIntegrationInfo
    {
        public readonly bool isConnectedQ;
        public readonly RCFetchSource infoType;
        public readonly string adtOptName, adtDataIDs, updShortDesc, updDataID;
        public readonly string connectionName, connectionDataID;

        public ResLibIntegrationInfo(RCFetchSource type, string dataIDs, string nameOrDesc, string connectedName, string connectedDataID)
        {
            infoType = type;
            adtOptName = null;
            adtDataIDs = null;
            updDataID = null;
            updShortDesc = null;

            connectionDataID = connectedDataID;
            connectionName = connectedName;
            isConnectedQ = connectionName.IsNotNE() && connectionDataID.IsNotNE();

            switch (infoType)
            {
                case RCFetchSource.ConAdditionals:
                    adtDataIDs = dataIDs;
                    adtOptName = nameOrDesc;
                    break;

                case RCFetchSource.ConChanges:
                    updDataID = dataIDs;
                    updShortDesc = nameOrDesc;
                    break;
            }
        }

        /// <returns>A boolean relaying whether there is an info type, relevant info has bee provided for type, and info of the connecting base content has been provided.</returns>
        public bool IsSetup()
        {
            return !infoType.IsNone() && ((adtOptName.IsNotNE() && adtDataIDs.IsNotNE()) || (updDataID.IsNotNE() && updShortDesc.IsNotNE())) && connectionName.IsNotNE() && connectionDataID.IsNotNE();
        }
    }
}
