using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stitch;

namespace StitchTest {
    [TestClass]
    public class SequenceMatchTest {
        SequenceMatch CreateTestData() {
            var alp = new Alphabet("*;A;B;.\nA;1;0;-1\nB;0;1;-1\n.;-1;-1;0", Alphabet.AlphabetParamType.Data, 12, 1);
            // 012345 6789012  3456
            // AAAABA-ABABBAA--AAAA
            // AAAA-ABABA--AABBAAAA
            var template = new Read.Simple(AminoAcid.FromString("AAAABAABABBAAAAAA", alp).Unwrap());
            var query = new Read.Simple(AminoAcid.FromString("AAAAABABAAABBAAAA", alp).Unwrap());
            return new SequenceMatch(
                0,
                0,
                1,
                new List<SequenceMatch.MatchPiece> {
                    new SequenceMatch.Match(4),
                    new SequenceMatch.Deletion(1),
                    new SequenceMatch.Match(1),
                    new SequenceMatch.Insertion(1),
                    new SequenceMatch.Match(3),
                    new SequenceMatch.Deletion(2),
                    new SequenceMatch.Match(2),
                    new SequenceMatch.Insertion(2),
                    new SequenceMatch.Match(4), },
                template,
                query,
                0);
        }

        [TestMethod]
        public void GetGapAtTemplateIndexTest() {
            var sm = CreateTestData();
            var gaps = "";
            for (int i = 0; i < sm.QuerySequence.LengthOnTemplate; i++) {
                gaps += $", {i}: {sm.GetGapAtTemplateIndex(i)}";
            }
            Console.WriteLine(gaps);
            Assert.AreEqual(0, sm.GetGapAtTemplateIndex(5));
            Assert.AreEqual(1, sm.GetGapAtTemplateIndex(6));
            Assert.AreEqual(0, sm.GetGapAtTemplateIndex(7));
            Assert.AreEqual(0, sm.GetGapAtTemplateIndex(11));
            Assert.AreEqual(2, sm.GetGapAtTemplateIndex(13));
        }

        [TestMethod]
        public void GetAtTemplateIndexTest() {
            var sm = CreateTestData();
            var gaps = "";
            for (int i = 0; i < sm.QuerySequence.LengthOnTemplate; i++) {
                gaps += $", {i}: {sm.GetAtTemplateIndex(i)}";
            }
            Console.WriteLine(gaps);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(5).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(6).Value.Character);
            Assert.AreEqual('B', sm.GetAtTemplateIndex(7).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(11).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(13).Value.Character);
        }

        [TestMethod]
        public void GetAtTemplateIndexTestOffset() {
            var sm = CreateTestData();
            sm = new SequenceMatch(150, sm.StartQueryPosition, sm.Score, sm.QuerySequence.Alignment, sm.Template, sm.Query, sm.Index, sm.TemplateIndex);
            var gaps = "";
            for (int i = 0; i < sm.QuerySequence.LengthOnTemplate; i++) {
                gaps += $", {i}: {sm.GetAtTemplateIndex(150 + i)}";
            }
            Console.WriteLine(gaps);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(150 + 5).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(150 + 6).Value.Character);
            Assert.AreEqual('B', sm.GetAtTemplateIndex(150 + 7).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(150 + 11).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(150 + 13).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(150 + sm.QuerySequence.LengthOnTemplate - 1).Value.Character);
            Assert.AreEqual(null, sm.GetAtTemplateIndex(150 + sm.QuerySequence.LengthOnTemplate));
            Assert.AreEqual(null, sm.GetAtTemplateIndex(150 + sm.QuerySequence.LengthOnTemplate + 1));
        }

        [TestMethod]
        public void GetAtTemplateIndexTestReal() {
            Alphabet blosum = new Alphabet(Globals.Root + @"alphabets/blosum62_X1.csv", Alphabet.AlphabetParamType.Path, 12, 1);
            var query = new Read.Simple(AminoAcid.FromString("VDNALQSGGGSQESVTEQDDK", blosum).Unwrap());
            query.Sequence.UpdateSequence(8, 2, new AminoAcid[] { new AminoAcid(blosum, 'N') }, "Isomass");
            query.Sequence.UpdateSequence(16, 1, new AminoAcid[] { new AminoAcid(blosum, 'E') }, "Deamidation");
            var template = new Read.Simple(AminoAcid.FromString("DIQMTQSPSSLSASVGDRVTITCRASAGISNAVAWYQQKPGKAPKLLLYSASFLYSGVPSRFSGSRSGTDYTLTISSLQPEDFATYYCQQHYTTPWTFGQGTKVEIKRTVAAPSVFIFPPSDEQLKSGTASVVCLLNNFYPREAKVQWKVDNALQSGNSQESVTEQDSKDSTYSLSSTLTLSKADYEKHKVYACEVTHQGLSSPVTKSFNRGEC", blosum).Unwrap());
            var sm = new SequenceMatch(149, 0, 75, new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(9), new SequenceMatch.Insertion(1), new SequenceMatch.Match(11) }, template, query, 644, 0);
            for (int i = 0; i < sm.QuerySequence.LengthOnTemplate; i++) {
                Console.Write($", {i}: {sm.GetAtTemplateIndex(149 + i)}");
            }
            Console.WriteLine("");
            Assert.AreEqual('V', sm.GetAtTemplateIndex(149 + 0).Value.Character);
            Assert.AreEqual('N', sm.GetAtTemplateIndex(149 + 8).Value.Character);
            Assert.AreEqual('E', sm.GetAtTemplateIndex(149 + 16).Value.Character);
            Assert.AreEqual('K', sm.GetAtTemplateIndex(149 + sm.QuerySequence.LengthOnTemplate - 1).Value.Character);
            Assert.AreEqual(null, sm.GetAtTemplateIndex(149 + sm.QuerySequence.LengthOnTemplate));
            Assert.AreEqual(null, sm.GetAtTemplateIndex(149 + sm.QuerySequence.LengthOnTemplate + 1));
        }

        [TestMethod]
        public void SimplifyTestJoining() {
            var sm = CreateTestData();
            sm.QuerySequence.Alignment[1] = new SequenceMatch.Match(1);
            sm.QuerySequence.Alignment[6] = new SequenceMatch.Insertion(2);
            sm.Simplify();
            Assert.AreEqual("6M1I3M2D4I4M", HelperFunctionality.CIGAR(sm.QuerySequence.Alignment));
        }

        [TestMethod]
        public void SimplifyTestRemoveEmpty() {
            var sm = CreateTestData();
            sm.QuerySequence.Alignment[1] = new SequenceMatch.Insertion(0);
            sm.QuerySequence.Alignment[7] = new SequenceMatch.Deletion(0);
            sm.Simplify();
            Assert.AreEqual("5M1I3M2D6M", HelperFunctionality.CIGAR(sm.QuerySequence.Alignment));
        }

        [TestMethod]
        public void TestOverwriting() {
            var sm = CreateTestData();
            Assert.AreEqual("4M1D1M1I3M2D2M2I4M", HelperFunctionality.CIGAR(sm.QuerySequence.Alignment));
            //SequenceMatch.OverWriteAlignment(ref sm.Alignment, 5, 2, 3);
            Assert.AreEqual("4M1D2M1I2M2D2M2I4M", HelperFunctionality.CIGAR(sm.QuerySequence.Alignment));
            //SequenceMatch.OverWriteAlignment(ref sm.Alignment, 10, 2, 2);
            Assert.AreEqual("4M1D2M1I2M1D3M2I4M", HelperFunctionality.CIGAR(sm.QuerySequence.Alignment));
        }

        [TestMethod]
        public void TestOverwritingComplex() {
            var sm = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(12) };
            Assert.AreEqual("12M", HelperFunctionality.CIGAR(sm));
            //SequenceMatch.OverWriteAlignment(ref sm, 5, 2, 5);
            Assert.AreEqual("7M3I5M", HelperFunctionality.CIGAR(sm));
            //SequenceMatch.OverWriteAlignment(ref sm, 1, 5, 2);
            Assert.AreEqual("3M3D1M3I5M", HelperFunctionality.CIGAR(sm));
        }
    }
}