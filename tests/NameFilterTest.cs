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
    public class NameFilter_Test
    {
        [DataRow("abcd")]
        [DataRow("dfgh")]
        [DataRow("yuop")]
        [DataRow("lkjs")]
        [DataRow("a")]
        [DataRow("t")]
        [DataRow("abcdefghijklmnopqrstuvwxyz")]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [DataRow("0123456789")]
        [DataRow("~")]
        [DataRow("@")]
        [DataRow("#")]
        [DataRow("$")]
        [DataRow("€")]
        [DataRow("^")]
        [DataRow("&")]
        [DataRow("(")]
        [DataRow(")")]
        [DataRow("-")]
        [DataRow("_")]
        [DataRow("=")]
        [DataRow("+")]
        [DataRow("[")]
        [DataRow("]")]
        [DataRow("{")]
        [DataRow("}")]
        [DataRow(";")]
        [DataRow("'")]
        [DataRow(",")]
        [DataRow("`")]
        [DataTestMethod]
        public void PreservesNormalName(string name)
        {
            var filter = new NameFilter();
            var result = filter.EscapeIdentifier(name);
            Assert.AreEqual(name, result.Item1);
            Assert.AreEqual(name, result.Item2.Name);
            Assert.AreEqual(1, result.Item3);
            Assert.AreEqual(1, result.Item2.Count);
            Assert.AreEqual(null, result.Item2.Left);
            Assert.AreEqual(null, result.Item2.Right);
        }

        [TestMethod]
        public void HandlesDuplicates()
        {
            var filter = new NameFilter();
            var names = new List<(string, int)> { ("abcd", 1), ("hello", 1), ("abcd", 2), ("world", 1), ("abcd", 3) };
            foreach (var (name, count) in names)
            {
                var result = filter.EscapeIdentifier(name);
                Assert.AreEqual(count, result.Item3);
            }
        }

        [DataRow("F3:8900.67", "F3_8900_67")]
        [DataRow("F3*8900%67", "F3_8900_67")]
        [DataRow("F3?8900\"67!", "F3_8900_67_")]
        [DataTestMethod]
        public void MakesSafeName(string name, string safe_variant)
        {
            var filter = new NameFilter();
            var result = filter.EscapeIdentifier(name);
            Assert.AreEqual(safe_variant, result.Item1);
        }
    }
}
