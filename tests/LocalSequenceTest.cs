using Stitch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace StitchTest {
    [TestClass]
    public class LocalSequenceTest {

        readonly Alphabet alp;
        public LocalSequenceTest() {
            alp = new Alphabet("*;A;B\nA;1;0\nB;0;1", Alphabet.AlphabetParamType.Data, 12, 1);
        }

        [TestMethod]
        public void TestChanged() {
            var ls = new LocalSequence(AminoAcid.FromString("AAAAAAAAAAAA", alp).Unwrap(), new double[12] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
            ls.UpdateSequence(5, 3, AminoAcid.FromString("BBB", alp).Unwrap(), "Fun");
            ls.UpdateSequence(9, 3, AminoAcid.FromString("BBB", alp).Unwrap(), "Fun");
            Assert.AreEqual("AAAAABBBABBB", AminoAcid.ArrayToString(ls.Sequence));
            bool[] actual = ls.ChangeProfile();
            bool[] expected = new bool[12] { false, false, false, false, false, true, true, true, false, true, true, true };
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i], actual[i], $"At position {i}");
            }

            var actual1 = ls.AlignmentWithOriginal();
            var expected1 = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(12) };
            Assert.AreEqual(HelperFunctionality.CIGAR(expected1), HelperFunctionality.CIGAR(actual1));
        }

        [TestMethod]
        public void TestChangedDifferentLengths() {
            var ls = new LocalSequence(AminoAcid.FromString("AAAAAAAAAAAA", alp).Unwrap(), new double[12] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
            ls.UpdateSequence(5, 2, AminoAcid.FromString("BBB", alp).Unwrap(), "Fun");
            ls.UpdateSequence(10, 1, AminoAcid.FromString("BBB", alp).Unwrap(), "Fun");
            Assert.AreEqual("AAAAABBBAABBBAA", AminoAcid.ArrayToString(ls.Sequence));
            bool[] actual = ls.ChangeProfile();
            bool[] expected = new bool[15] { false, false, false, false, false, true, true, true, false, false, true, true, true, false, false };
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i], actual[i], $"At position {i}");
            }

            var actual1 = ls.AlignmentWithOriginal();
            var expected1 = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(7), new SequenceMatch.Insertion(1), new SequenceMatch.Match(3), new SequenceMatch.Insertion(2), new SequenceMatch.Match(2) };
            Assert.AreEqual(HelperFunctionality.CIGAR(expected1), HelperFunctionality.CIGAR(actual1));
        }
    }
}