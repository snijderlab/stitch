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
    public class Alphabet_Test
    {
        readonly Alphabet alp;
        public Alphabet_Test()
        {
            alp = new Alphabet("*;A;B\nA;1;0\nB;0;1", Alphabet.AlphabetParamType.Data, 12, 1);
        }
        [DataRow('A', 'B')]
        [DataRow('B', 'A')]
        [DataTestMethod]
        public void InvariantNotEqual(char x, char y)
        {
            int a = alp.GetIndexInAlphabet(x);
            int b = alp.GetIndexInAlphabet(y);
            Assert.AreNotEqual(a, b);
        }
        [DataRow('A', 'A')]
        [DataRow('B', 'B')]
        [DataTestMethod]
        public void InvariantEqual(char x, char y)
        {
            int a = alp.GetIndexInAlphabet(x);
            int b = alp.GetIndexInAlphabet(y);
            Assert.AreEqual(a, b);
        }
        [TestMethod]
        public void InvariantNotInAlphabet()
        {
            string input = "abcdefghijklmnopqrstuvwxyzCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            foreach (char c in input)
            {
                Assert.ThrowsException<ArgumentException>(() => alp.GetIndexInAlphabet(c));
            }
        }
        [TestMethod]
        public void NegativeAlphabet()
        {
            var alp = new Alphabet("*;A;B\nA;1;-1\nB;-1;1", Alphabet.AlphabetParamType.Data, 12, 1);
            Assert.AreEqual(-1, alp.ScoringMatrix[alp.GetIndexInAlphabet('A'), alp.GetIndexInAlphabet('B')]);
        }
        [DataRow("*;A;B\nA;1;0", "Missing row")]
        [DataRow("*;A;B\nA;1;0\nB;0;1\nC;0;0", "Extra row")]
        [DataRow("*;A;B\nA;1;0;0\nB;0;1;0\nC;0;0;1", "Missing Column")]
        [DataRow("*;A;B\nA;1;0\nB;o;1", "Non Integer value")]
        [DataRow("*;A;B\nA:1;0\nB;0;1", "Missing cell, ':' instead of ';'")]
        [DataRow("*;A;B\nA;1;0\nB;0:1", "Missing cell, ':' instead of ';'")]
        [DataRow("*;A;B\nA;1;0\nB;0;", "Missing value")]
        [DataRow("*;A;B\nA;1;0\nB;;1", "Missing value")]
        [DataRow("*;A;B\nA;1\nB;0;1", "Missing cell")]
        [DataTestMethod]
        public void InvariantNotValidAlphabet(string a, string msg)
        {
            Assert.ThrowsException<ParseException>(() => new Alphabet(a, Alphabet.AlphabetParamType.Data, 12, 1), msg);
        }
        [TestMethod]
        public void OpenViaFile()
        {
            //Expect to be running inside the /bin/debug/netcoreapp2.2 folder
            Alphabet alp2 = new Alphabet(@"../../../testalphabet.csv", Alphabet.AlphabetParamType.Path, 12, 1);
            string input = "AB";
            foreach (char c in input)
            {
                Assert.AreEqual(alp.GetIndexInAlphabet(c), alp2.GetIndexInAlphabet(c));
            }
        }
        /// <summary>
        /// All alphabets given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestExamples()
        {
            var path = Globals.Root + "alphabets";
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (file.EndsWith(".csv"))
                {
                    Console.Write(file);
                    new Alphabet(file, Alphabet.AlphabetParamType.Path, 12, 1);
                }
            }
        }
    }
}
