using System.Collections.Generic;

namespace Stitch {
    public class ExtraArguments {
        public readonly bool AutomaticallyOpen;
        public readonly int LiveServer;
        public readonly string[] ExpectedResult;
        public ExtraArguments() {
            AutomaticallyOpen = false;
            ExpectedResult = new string[0];
        }
        public ExtraArguments(bool open, int live, string[] expectedResult) {
            AutomaticallyOpen = open;
            LiveServer = live;
            ExpectedResult = expectedResult;
        }
    }
}