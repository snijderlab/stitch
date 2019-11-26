using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssemblyNameSpace;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class Alphabet_Test
    {
        Alphabet alp;
        public Alphabet_Test()
        {
            alp = new Alphabet("*;A;B\nA;1;0\nB;0;1", Alphabet.AlphabetParamType.Data);
        }
        [DataRow('A', 'B')]
        [DataRow('B', 'A')]
        [DataTestMethod]
        public void InvariantNotEqual(char x, char y)
        {
            int a = alp.getIndexInAlphabet(x);
            int b = alp.getIndexInAlphabet(y);
            Assert.AreNotEqual(a, b);
        }
        [DataRow('A', 'A')]
        [DataRow('B', 'B')]
        [DataTestMethod]
        public void InvariantEqual(char x, char y)
        {
            int a = alp.getIndexInAlphabet(x);
            int b = alp.getIndexInAlphabet(y);
            Assert.AreEqual(a, b);
        }
        [TestMethod]
        public void InvariantNotInAlphabet()
        {
            string input = "abcdefghijklmnopqrstuvwxyzCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            foreach (char c in input)
            {
                int a = alp.getIndexInAlphabet(c);
                Assert.AreEqual(a, -1);
            }
        }
        [DataRow("*;A;B\nA;1;0", "Missing row")]
        [DataRow("*;A;B\nA;1;0\nB;0;1\nC;0;0", "Extra row")]
        [DataRow("*;A;B\nA;1;0;0\nB;0;1;0\nC;0;0;1", "Missing Column")]
        [DataRow("*;A;B\nA;1;0\nB;o;1", "Non Integer value")]
        [DataRow("*;A;B\nA;1;0\nB;;1", "Missing value")]
        [DataTestMethod]
        public void InvariantNotValidAlphabet(string a, string msg)
        {
            Assert.ThrowsException<ParseException>(() => new Alphabet(a, Alphabet.AlphabetParamType.Data), msg);
        }
        [TestMethod]
        public void OpenViaFile()
        {
            //Expect to be running inside the /bin/debug/netcoreapp2.2 folder
            Alphabet alp2 = new Alphabet(@"../../../testalphabet.csv", Alphabet.AlphabetParamType.Path);
            string input = "AB";
            foreach (char c in input)
            {
                Assert.AreEqual(alp.getIndexInAlphabet(c), alp2.getIndexInAlphabet(c));
            }
        }
    }

    [TestClass]
    public class SmithWaterman_Test
    {
        Alphabet alp;
        public SmithWaterman_Test()
        {
            alp = new Alphabet("*;A;B;C;D\nA;1;0;0;0\nB;0;1;0;0\nC;0;0;1;0\nD;0;0;0;1", Alphabet.AlphabetParamType.Data);
        }
        [DataRow("ACBCABCABCBBCBCAC", "CABCDABC")]
        [DataRow("ACBCABCABCBBCBCAC", "CABCDBBC")]
        [DataTestMethod]
        public void SingleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            Assert.AreEqual(5, HelperFunctionality.SmithWaterman(a, b));
        }
        [DataRow("ACACACA", "ACABCBACA")]
        [DataRow("ACACACA", "ACBACBACA")]
        [DataTestMethod]
        public void DoubleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            Assert.AreEqual(3, HelperFunctionality.SmithWaterman(a, b));
        }
        [DataRow("ACACACA", "ACABACA")]
        [DataRow("ACACACA", "ACACBCA")]
        [DataTestMethod]
        public void Mismatch(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            Assert.AreEqual(5, HelperFunctionality.SmithWaterman(a, b));
        }
        [DataRow("ABBA")]
        [DataRow("AAABBA")]
        [DataRow("AAAAAAAAA")]
        [DataRow("AAABBAAAABBA")]
        [DataTestMethod]
        public void EqualSequences(string x)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(x);
            Assert.AreEqual(x.Length, HelperFunctionality.SmithWaterman(a, b));
        }
        [DataRow("ACBDCADBCADCBADCCABCDAA", "DBCADCB")]
        [DataRow("ACBDCADBCADCBADCCABCDAA", "DCBADCC")]
        [DataTestMethod]
        public void EqualPart(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            Assert.AreEqual(7, HelperFunctionality.SmithWaterman(a, b));
        }
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alp, input[i]);
            }
            return output;
        }
    }
}
