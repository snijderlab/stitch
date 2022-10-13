using System.Collections.Generic;

namespace Stitch
{
    public class RunVariables
    {
        public readonly bool AutomaticallyOpen;
        public readonly string LiveServer;
        public readonly List<string> ExpectedResult;
        public RunVariables()
        {
            AutomaticallyOpen = false;
            ExpectedResult = new List<string>();
        }
        public RunVariables(bool open, string live, List<string> expectedResult)
        {
            AutomaticallyOpen = open;
            LiveServer = live;
            ExpectedResult = expectedResult;
        }
    }
}