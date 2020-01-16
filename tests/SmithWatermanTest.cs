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
            var r = HelperFunctionality.SmithWaterman(x, y, 0, alp);
            Assert.AreEqual("SequenceMatch< starting at template: 1, starting at query: 0, score: 10, match: 6M1I6M >", r.ToString());
        }
        [DataRow("ACACCACCACCA", "ACACDCACCDACCA")]
        [DataRow("ABBCABAAABCA", "ABBCDABAADABCA")]
        [DataTestMethod]
        public void DoubleGap(string x, string y)
        {
            var r =  HelperFunctionality.SmithWaterman(x, y, 0, alp);
            Assert.AreEqual("SequenceMatch< starting at template: 0, starting at query: 0, score: 8, match: 4M1I4M1I4M >", r.ToString());
        }
        [DataRow("ACACACA", "ACABACA")]
        [DataRow("ACACACA", "ACACBCA")]
        [DataTestMethod]
        public void Mismatch(string x, string y)
        {
            var r = HelperFunctionality.SmithWaterman(x, y, 0, alp);
            Assert.AreEqual("SequenceMatch< starting at template: 0, starting at query: 0, score: 6, match: 7M >", r.ToString());
        }
        [DataRow("ABBA")]
        [DataRow("AAABBA")]
        [DataRow("AAAAAAAAA")]
        [DataRow("AAABBAAAABBA")]
        [DataTestMethod]
        public void EqualSequences(string x)
        {
            var r = HelperFunctionality.SmithWaterman(x, x, 0, alp);
            Assert.AreEqual($"SequenceMatch< starting at template: 0, starting at query: 0, score: {x.Length}, match: {x.Length}M >", r.ToString());
        }
        [DataRow("BABACBAAABCB", "CBAAA")]
        [DataRow("CBABABACCBCA", "ABACC")]
        [DataTestMethod]
        public void EqualPart(string x, string y)
        {
            var r = HelperFunctionality.SmithWaterman(x, y, 0, alp);
            Assert.AreEqual($"SequenceMatch< starting at template: 0, starting at query: 4, score: 5, match: 5M >", r.ToString());
        }
        [TestMethod]
        public void MismatchAndGap()
        {
            var a = "ABBAABBCBABAACCBBAAB";
            var b = "BBCBDABCACC";
            var r = HelperFunctionality.SmithWaterman(a, b, 0, alp);
            Assert.AreEqual($"SequenceMatch< starting at template: 1, starting at query: 5, score: 7, match: 4M1I6M >", r.ToString());
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
