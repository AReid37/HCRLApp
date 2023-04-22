
namespace HCResourceLibraryApp.DataHandling
{
    public class SfdGroupInfo
    {
        public int startLineNum;
        public int endLineNum;
        public string groupName;
        public bool isExpandedQ;

        public SfdGroupInfo() { }
        public SfdGroupInfo(string name, int startLine, int endLine)
        {
            isExpandedQ = true;
            groupName = name;
            if (startLine < endLine)
            {
                startLineNum = startLine;
                endLineNum = endLine;
            }
            else
            {
                startLineNum = endLine;
                endLineNum = startLine;
            }
        }

        public string Encode()
        {
            /// Syntax: {startLine}*{endLine}*{groupName}*{expandedQ}
            string giEncode = "";
            if (IsSetup())
                giEncode = $"{startLineNum}{DataHandlerBase.Sep}{endLineNum}{DataHandlerBase.Sep}{groupName}{DataHandlerBase.Sep}{isExpandedQ}";
            return giEncode;
        }
        public bool Decode(string dataLine)
        {
            /// Syntax: {startLine}*{endLine}*{groupName}*{expandedQ}
            bool hasDecodedQ = false;
            if (dataLine.IsNotNEW())
            {
                string[] groupData = dataLine.Split(DataHandlerBase.Sep);
                if (groupData.HasElements(4))
                {
                    /// start line && end line
                    if (int.TryParse(groupData[0], out int startLine) && int.TryParse(groupData[1], out int endLine))
                    {
                        startLineNum = startLine;
                        endLineNum = endLine;
                    }
                    /// group name 
                    if (groupData[2].IsNotNEW())
                        groupName = groupData[2];
                    /// isExpanded?
                    if (bool.TryParse(groupData[3], out bool expandedQ))
                        isExpandedQ = expandedQ;

                    hasDecodedQ = true;
                }
            }
            return hasDecodedQ;
        }
        /// <summary>Compares two instances for similarities against: setup state, start line number, end line number, group name.</summary>
        public bool Equals(SfdGroupInfo group)
        {
            bool areEquals = IsSetup() == group.IsSetup();
            for (int ax = 0; ax < 3 && areEquals; ax++)
            {
                areEquals = ax switch
                {
                    0 => startLineNum == group.startLineNum,
                    1 => endLineNum == group.endLineNum,
                    2 => groupName == group.groupName,
                    _ => false
                };
            }
            return areEquals;
        }
        /// <returns>A boolean stating if a group name, a starting line number, and an ending line number has been provided.</returns>
        public bool IsSetup()
        {
            return groupName.IsNotNE() && startLineNum < endLineNum;
        }
        public override string ToString()
        {
            return $"SfdGI :: '{(groupName.IsNotNE() ? groupName : "N/a")}', {startLineNum}~{endLineNum}, {(isExpandedQ ? "expd" : "clsp")}";
        }
    }
}
