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
    public class KArithmetic_Test {
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleMinus(int k) {
            var kar = parse("K-2");
            Assert.AreEqual(kar.GetValue(k), k-2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimplePlus(int k) {
            var kar = parse("K+2");
            Assert.AreEqual(kar.GetValue(k), k+2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleTimes(int k) {
            var kar = parse("K*2");
            Assert.AreEqual(kar.GetValue(k), k*2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void SimpleDivide(int k) {
            var kar = parse("K/2");
            Assert.AreEqual(kar.GetValue(k), k/2);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void Complex_01(int k) {
            var kar = parse("K*2+2/4-7");
            Assert.AreEqual(kar.GetValue(k), k*2+2/4-7);
        }
        [DataRow(-10)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(25)]
        [DataTestMethod]
        public void Complex_02(int k) {
            var kar = parse("K*2+2/4-7*K+8");
            Assert.AreEqual(kar.GetValue(k), k*2+2/4-7*k+8);
        }
        [TestMethod]
        public void EquivalentK() {
            var k1 = parse("K*2");
            var k2 = parse("k*2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void NastyWhiteSpace() {
            var k1 = parse("\rK*\t2    ");
            var k2 = parse("   k\n* 2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void OtherLetters() {
            foreach (var a in "abcdefghijlmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ") {
                Assert.ThrowsException<ParseException>(() => parse($"{a}*2"));
            }
        }
        [TestMethod]
        public void InvalidOperators() {
            foreach (var a in "~!@#$%^&<>?\\") {
                Assert.ThrowsException<ParseException>(() => parse($"k{a}2"));
            }
        }
        [TestMethod]
        public void MissingValue() {
            foreach (var a in "+-*/") {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => parse($"k{a}")); // ArgumentOutOfRangeException because internally it tries to create fancy errormessages but that gives this error.
                Assert.ThrowsException<ParseException>(() => parse($"{a}2"));
            }
        }
        [TestMethod]
        public void DoubleOperator() {
            foreach (var a in "+-*/") {
                foreach (var b in "+-*/") {
                    Assert.ThrowsException<ParseException>(() => parse($"k{a}{b}2"));
                }
            }
        }
        [TestMethod]
        public void Brackets() {
            var opts = new List<(char, char)>{('(', ')'), ('{', '}'), ('[', ']'), ('<', '>')};
            foreach (var a in opts) {
                Assert.ThrowsException<ParseException>(() => parse($"{a.Item1}k-2{a.Item2}*2"));
            }
        }
        public KArithmetic parse(string s) {
            return new KArithmetic(KArithmetic.TryParse(s, new Range(new Position(1, 1), new Position(1, 1))).ReturnOrFail());
        }
    }
}
