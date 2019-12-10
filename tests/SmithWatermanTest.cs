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
    public class SmithWaterman_Test
    {
        Alphabet alp;
        public SmithWaterman_Test()
        {
            alp = new Alphabet("*;A;B;C;D\nA;1;0;0;0\nB;0;1;0;0\nC;0;0;1;0\nD;0;0;0;1", Alphabet.AlphabetParamType.Data);
        }
        [DataRow("CAAAACCCABBC", "CAAAACDCCABBC")]
        [DataRow("CCCBAAACACBB", "CCCBAADACACBB")]
        [DataTestMethod]
        public void SingleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual("SequenceMatch< starting at: 0, score: 10, match: 6M1I6M >", r.ToString());
        }
        [DataRow("ACACCACCACCA", "ACACDCACCDACCA")]
        [DataRow("ABBCABAAABCA", "ABBCDABAADABCA")]
        [DataTestMethod]
        public void DoubleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r =  HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual("SequenceMatch< starting at: 0, score: 8, match: 4M1I4M1I4M >", r.ToString());
        }
        [DataRow("ACACACA", "ACABACA")]
        [DataRow("ACACACA", "ACACBCA")]
        [DataTestMethod]
        public void Mismatch(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual("SequenceMatch< starting at: 0, score: 6, match: 7M >", r.ToString());
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
            var r = HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual($"SequenceMatch< starting at: 0, score: {x.Length}, match: {x.Length}M >", r.ToString());
        }
        [DataRow("BABACBAAABCB", "CBAAA")]
        [DataRow("CBABABACCBCA", "ABACC")]
        [DataTestMethod]
        public void EqualPart(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual($"SequenceMatch< starting at: 4, score: 5, match: 5M >", r.ToString());
        }
        [TestMethod]
        public void MismatchAndGap()
        {
            var a = StringToSequence("ABBAABBCBABAACCBBAAB");
            var b = StringToSequence("BBCBDABCACC");
            var r = HelperFunctionality.SmithWaterman(a, b);
            Assert.AreEqual($"SequenceMatch< starting at: 5, score: 7, match: 4M1I6M >", r.ToString());
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
