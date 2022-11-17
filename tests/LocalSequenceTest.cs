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
            var actual = ls.ChangeProfile();
            var expected = new (bool, int)[] { (false, 5), (true, 3), (false, 1), (true, 3) };
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
            var actual = ls.ChangeProfile();
            var expected = new (bool, int)[] { (false, 5), (true, 3), (false, 2), (true, 3), (false, 2) };
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i], actual[i], $"At position {i}");
            }

            var actual1 = ls.AlignmentWithOriginal();
            var expected1 = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(7), new SequenceMatch.Insertion(1), new SequenceMatch.Match(3), new SequenceMatch.Insertion(2), new SequenceMatch.Match(2) };
            Assert.AreEqual(HelperFunctionality.CIGAR(expected1), HelperFunctionality.CIGAR(actual1));
        }

        [TestMethod]
        public void TestChangedRealWorld() {
            Alphabet blosum = new Alphabet(Globals.Root + @"alphabets/blosum62_X1.csv", Alphabet.AlphabetParamType.Path, 12, 1);
            var ls = new LocalSequence(AminoAcid.FromString("DLQLVESNGLVQP", blosum).Unwrap(), new double[13] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
            ls.UpdateSequence(0, 2, AminoAcid.FromString("EV", blosum).Unwrap(), "Fun");
            ls.UpdateSequence(7, 1, AminoAcid.FromString("GG", blosum).Unwrap(), "Fun");
            Assert.AreEqual("EVQLVESGGGLVQP", AminoAcid.ArrayToString(ls.Sequence));
            var actual = ls.ChangeProfile();
            var expected = new (bool, int)[] { (true, 2), (false, 5), (true, 2), (false, 5) };
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i], actual[i], $"At position {i}");
            }

            //var actual1 = ls.AlignmentWithOriginal();
            //var expected1 = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(8), new SequenceMatch.Insertion(1), new SequenceMatch.Match(5) };
            //Assert.AreEqual(HelperFunctionality.CIGAR(expected1), HelperFunctionality.CIGAR(actual1));
        }
    }
}