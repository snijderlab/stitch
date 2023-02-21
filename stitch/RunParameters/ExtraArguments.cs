using System.Collections.Generic;

namespace Stitch {
    public class ExtraArguments {
        public readonly bool AutomaticallyOpen;
        public readonly int LiveServer;
        public readonly string[] ExpectedResult;
        public readonly bool Quiet;
        public ExtraArguments() {
            AutomaticallyOpen = false;
            ExpectedResult = new string[0];
            Quiet = false;
        }
        public ExtraArguments(bool open, int live, string[] expectedResult, bool quiet) {
            AutomaticallyOpen = open;
            LiveServer = live;
            ExpectedResult = expectedResult;
            Quiet = quiet;
        }
    }
}