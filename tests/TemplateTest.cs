using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using Stitch.RunParameters;

namespace StitchTest {
    [TestClass]
    public class Template_Test {
        [TestMethod]
        public void Consensus() {
            var sc = ScoringMatrix.Default();
            var seq = AminoAcid.FromString("WGGWNWJLIJLI", sc).Unwrap();
            var template_met = new ReadFormat.Simple(seq);
            var parent = new Segment(new List<ReadFormat.General> { template_met }, sc, "test_seg", 0.0, 0, false);
            var template = new Template("test", seq, template_met, parent, false);
            template.AddMatch(new Alignment(template.MetaData, new ReadFormat.Simple(AminoAcid.FromString("WNWGGWJJJJII", sc).Unwrap()), sc, AlignmentType.ReadAlign));
            template.AddMatch(new Alignment(template.MetaData, new ReadFormat.Simple(AminoAcid.FromString("WNWGGWJJJJIL", sc).Unwrap()), sc, AlignmentType.ReadAlign));
            template.AddMatch(new Alignment(template.MetaData, new ReadFormat.Simple(AminoAcid.FromString("WNWGGWJJJLIL", sc).Unwrap()), sc, AlignmentType.ReadAlign));
            template.AddMatch(new Alignment(template.MetaData, new ReadFormat.Simple(AminoAcid.FromString("WNWGGWJJJILL", sc).Unwrap()), sc, AlignmentType.ReadAlign));
            var consensus = template.ConsensusSequence();
            var cons_seq = AminoAcid.ArrayToString(consensus.Item1.SelectMany(a => a.Sequence));
            Console.WriteLine();
            foreach (var pos in template.CombinedSequence()) {
                Console.Write($"{pos.Template}: ");
                foreach (var opt in pos.AminoAcids) {
                    Console.Write($"{AminoAcid.ArrayToString(opt.Key.Sequence)} {opt.Value}, ");
                }
                Console.WriteLine();
            }
            Assert.AreEqual("WNWGGWJJJJIL", cons_seq);
        }
    }
}