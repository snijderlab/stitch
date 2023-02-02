using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using Stitch.RunParameters;

namespace StitchTest {
    [TestClass]
    public class EnforceUnique_Test {
        [TestMethod]
        public void OverlapsTest() {
            var parts = new List<(int, int)> { (5, 10), (20, 40) };
            Assert.IsFalse(EnforceUnique.Fits((0, 5), parts));
            Assert.IsFalse(EnforceUnique.Fits((10, 20), parts));
            Assert.IsFalse(EnforceUnique.Fits((11, 19), parts));
            Assert.IsFalse(EnforceUnique.Fits((40, 41), parts));
            Assert.IsTrue(EnforceUnique.Fits((5, 10), parts));
            Assert.IsTrue(EnforceUnique.Fits((20, 40), parts));
            Assert.IsTrue(EnforceUnique.Fits((22, 24), parts));
            Assert.IsTrue(EnforceUnique.Fits((38, 40), parts));
        }
    }
}