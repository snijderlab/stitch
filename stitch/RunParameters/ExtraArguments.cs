using System.Collections.Generic;

namespace Stitch {
    public class ExtraArguments {
        public readonly bool AutomaticallyOpen;
        public readonly string LiveServer;
        public readonly List<string> ExpectedResult;
        public ExtraArguments() {
            AutomaticallyOpen = false;
            ExpectedResult = new List<string>();
        }
        public ExtraArguments(bool open, string live, List<string> expectedResult) {
            AutomaticallyOpen = open;
            LiveServer = live;
            ExpectedResult = expectedResult;
        }
    }
}