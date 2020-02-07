using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class KArithmetic_Test
    {
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleMinus(int k)
        {
            var kar = Parse("K-2");
            Assert.AreEqual(kar.GetValue(k), k - 2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimplePlus(int k)
        {
            var kar = Parse("K+2");
            Assert.AreEqual(kar.GetValue(k), k + 2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleTimes(int k)
        {
            var kar = Parse("K*2");
            Assert.AreEqual(kar.GetValue(k), k * 2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleDivide(int k)
        {
            var kar = Parse("K/2");
            Assert.AreEqual(kar.GetValue(k), k / 2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void Complex_01(int k)
        {
            var kar = Parse("K*2+2/4-7");
            Assert.AreEqual(kar.GetValue(k), k * 2 + 2 / 4 - 7);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void Complex_02(int k)
        {
            var kar = Parse("K*2+2/4-7*K+8");
            Assert.AreEqual(kar.GetValue(k), k * 2 + 2 / 4 - 7 * k + 8);
        }
        [TestMethod]
        public void EquivalentK()
        {
            var k1 = Parse("K*2");
            var k2 = Parse("k*2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void NastyWhiteSpace()
        {
            var k1 = Parse("\rK*\t2    ");
            var k2 = Parse("   k\n* 2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void OtherLetters()
        {
            foreach (var a in "abcdefghijlmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ")
            {
                Assert.ThrowsException<ParseException>(() => Parse($"{a}*2"));
            }
        }
        [TestMethod]
        public void InvalidOperators()
        {
            foreach (var a in "~!@#$%^&<>?\\")
            {
                Assert.ThrowsException<ParseException>(() => Parse($"k{a}2"));
            }
        }
        [TestMethod]
        public void MissingValue()
        {
            foreach (var a in "+-*/")
            {
                Assert.ThrowsException<ParseException>(() => Parse($"{a}2"));
            }
        }
        [TestMethod]
        public void DoubleOperator()
        {
            foreach (var a in "+-*/")
            {
                foreach (var b in "+-*/")
                {
                    Assert.ThrowsException<ParseException>(() => Parse($"k{a}{b}2"));
                }
            }
        }
        [TestMethod]
        public void Brackets()
        {
            var opts = new List<(char, char)> { ('(', ')'), ('{', '}'), ('[', ']'), ('<', '>') };
            foreach (var a in opts)
            {
                Assert.ThrowsException<ParseException>(() => Parse($"{a.Item1}k-2{a.Item2}*2"));
            }
        }
        public KArithmetic Parse(string s)
        {
            var def_position = new Position(0, 1, new ParsedFile());
            return new KArithmetic(KArithmetic.TryParse(s, new Range(def_position, def_position), new ParsedFile()).ReturnOrFail());
        }
    }
}
