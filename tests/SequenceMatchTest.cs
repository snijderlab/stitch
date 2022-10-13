using Stitch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace StitchTest
{
    [TestClass]
    public class SequenceMatchTest
    {
        AminoAcid[] StringToSequence(string input, Alphabet alp)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alp, input[i]);
            }
            return output;
        }
        SequenceMatch CreateTestData()
        {
            var alp = new Alphabet("*;A;B;.\nA;1;0;-1\nB;0;1;-1\n.;-1;-1;0", Alphabet.AlphabetParamType.Data, 12, 1);
            // 012345 6789012  3456
            // AAAABA-ABABBAA--AAAA
            // AAAA-ABABA--AABBAAAA
            var template_sequence = StringToSequence("AAAABAABABBAAAAAA", alp);
            var query_sequence = StringToSequence("AAAAABABAAABBAAAA", alp);
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
                template_sequence,
                query_sequence,
                null,
                0);
        }

        [TestMethod]
        public void GetGapAtTemplateIndexTest()
        {
            var sm = CreateTestData();
            var gaps = "";
            for (int i = 0; i < sm.LengthOnTemplate; i++)
            {
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
        public void GetAtTemplateIndexTest()
        {
            var sm = CreateTestData();
            var gaps = "";
            for (int i = 0; i < sm.LengthOnTemplate; i++)
            {
                gaps += $", {i}: {sm.GetAtTemplateIndex(i)}";
            }
            Console.WriteLine(gaps);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(5).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(6).Value.Character);
            Assert.AreEqual('B', sm.GetAtTemplateIndex(7).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(11).Value.Character);
            Assert.AreEqual('A', sm.GetAtTemplateIndex(13).Value.Character);
        }
    }
}