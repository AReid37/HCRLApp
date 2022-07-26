using System;
using ConsoleFormat;
using HCResourceLibraryApp.DataHandling;

namespace HCResourceLibraryApp
{
    // THE ENTRANCE POINT, THE CONTROL ROOM
    class Program
    {
        static void Main()
        {
            Tools.DisableWarnError = DisableWE.None;
            Base.TextLine("Hello, High Contrast Resource Library App!");

            DataHandlerBase datahandler = new DataHandlerBase();
            Preferences prefs = new Preferences();
            datahandler.SaveToFile(prefs);
        }
    }
}
