using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using Stitch.RunParameters;

namespace StitchTest
{
    [TestClass]
    public class Position_Test
    {
        [DataRow("", "")]
        [DataRow("A", "A")]
        [DataRow("KAOSPJPJKklksadjoijdlkjaslkdj", "KAOSPJPJKklksadjoijdlkjaslkdj")]
        [DataTestMethod]
        public void EqualFiles(string a, string b)
        {
            var fa = new ParsedFile(a, new string[0]);
            var fb = new ParsedFile(b, new string[0]);
            Assert.AreEqual(fa, fb);
        }
        [DataRow("", " ")]
        [DataRow("A", "a")]
        [DataRow("KAOSPJPJKklksadjoijdlkjaslkdj", "KAOSPJPJKklksadjoijdlkjaslkdj_")]
        [DataTestMethod]
        public void NotEqualFiles(string a, string b)
        {
            var fa = new ParsedFile(a, new string[0]);
            var fb = new ParsedFile(b, new string[0]);
            Assert.AreNotEqual(fa, fb);
        }
        [TestMethod]
        public void EqualToItself()
        {
            var f = new ParsedFile();
            Assert.AreEqual(f, f);
        }
        [TestMethod]
        public void EqualStartingpoint()
        {
            var fa = new ParsedFile();
            var fb = new ParsedFile();
            Assert.AreEqual(fa, fb);
        }
    }
}