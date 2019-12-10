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
            var kar = new KArithmetic("K-2");
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
            var kar = new KArithmetic("K+2");
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
            var kar = new KArithmetic("K*2");
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
            var kar = new KArithmetic("K/2");
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
            var kar = new KArithmetic("K*2+2/4-7");
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
            var kar = new KArithmetic("K*2+2/4-7*K+8");
            Assert.AreEqual(kar.GetValue(k), k*2+2/4-7*k+8);
        }
        [TestMethod]
        public void EquivalentK() {
            var k1 = new KArithmetic("K*2");
            var k2 = new KArithmetic("k*2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void NastyWhiteSpace() {
            var k1 = new KArithmetic("\rK*\t2    ");
            var k2 = new KArithmetic("   k\n* 2");
            Assert.AreEqual(k1.GetValue(2), k2.GetValue(2));
        }
        [TestMethod]
        public void OtherLetters() {
            foreach (var a in "abcdefghijlmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ") {
                Assert.ThrowsException<ParseException>(() => new KArithmetic($"{a}*2"));
            }
        }
        [TestMethod]
        public void InvalidOperators() {
            foreach (var a in "~!@#$%^&<>?\\") {
                Assert.ThrowsException<ParseException>(() => new KArithmetic($"k{a}2"));
            }
        }
        [TestMethod]
        public void MissingValue() {
            foreach (var a in "+-*/") {
                Assert.ThrowsException<ParseException>(() => new KArithmetic($"k{a}"));
                Assert.ThrowsException<ParseException>(() => new KArithmetic($"{a}2"));
            }
        }
        [TestMethod]
        public void DoubleOperator() {
            foreach (var a in "+-*/") {
                foreach (var b in "+-*/") {
                    Assert.ThrowsException<ParseException>(() => new KArithmetic($"k{a}{b}2"));
                }
            }
        }
        [TestMethod]
        public void Brackets() {
            var opts = new List<(char, char)>{('(', ')'), ('{', '}'), ('[', ']'), ('<', '>')};
            foreach (var a in opts) {
                Assert.ThrowsException<ParseException>(() => new KArithmetic($"{a.Item1}k-2{a.Item2}*2"));
            }
        }
    }
}
