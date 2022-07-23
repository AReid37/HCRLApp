using System;
using ConsoleFormat;

namespace HCResourceLibraryApp
{
    // THE ENTRANCE POINT, THE CONTROL ROOM
    class Program
    {
        static void Main()
        {
            Tools.DisableWarnError = DisableWE.None;
            Base.TextLine("Hello, High Contrast Resource Library App!");
        }
    }
}
