using System.Collections.Generic;
namespace AssemblyNameSpace
{
    public class RunVariables
    {
        public readonly bool AutomaticallyOpen;
        public readonly bool LiveServer;
        public readonly List<string> ExpectedResult;
        public RunVariables()
        {
            AutomaticallyOpen = false;
            ExpectedResult = new List<string>();
        }
        public RunVariables(bool open, bool live, List<string> expectedResult)
        {
            AutomaticallyOpen = open;
            LiveServer = live;
            ExpectedResult = expectedResult;
        }
    }
}