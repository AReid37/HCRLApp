using System;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>A tool for keeping track of changes in the Formatting Editor with resources to redo/undo an action or change.</summary>
    public struct SFormatterHistory
    {
        /** History planning
            Fields/Props
            - str actionName
            - str redoneCommand
            - str undoneCommand

            Constructor
            - SFH(str name, str redoneCmd, str undoneCmd)

            Methods
            - bl IsSetup()
        
         */

        public readonly string actionName;
        public readonly string redoneCommand;
        public readonly string undoneCommand;

        public SFormatterHistory(string eventName, string redoneCmd, string undoneCmd)
        {
            actionName = eventName;
            redoneCommand = redoneCmd;
            undoneCommand = undoneCmd;
        }

        public bool IsSetup()
        {
            return actionName.IsNotNE() && redoneCommand.IsNotNE() && undoneCommand.IsNotNE();
        }
        public override string ToString()
        {
            return $"SFH: name[{actionName}] redo[{redoneCommand.Replace("\n","\\n")}] undo[{undoneCommand.Replace("\n", "\\n")}]";
        }
    }
}
