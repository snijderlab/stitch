//(string Alignment, List<double> DepthOfCoverage) CreateTemplateAlignment(Template template, string id, List<string> location)

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
    public class HTMLReport_Test {
        ScoringMatrix alp;

        public HTMLReport_Test() {
            alp = ScoringMatrix.Default();
        }

        Read.IRead Read(string sequence) {
            return (Read.IRead)new Read.Simple(AminoAcid.FromString(sequence, alp).Unwrap(), null, new NameFilter());
        }

        [TestMethod]
        public void SingleMatchTemplateAlignment() {
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<Read.IRead> { Read("EVQLVESGGGLVQPGGSLRL") },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<Read.IRead> { Read("EVQLVESGGGLVQPGGSLRL") });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(1.0, 20).ToList();
            CompareDOC(doc_expected, doc);
        }

        [TestMethod]
        public void MultiMatchTemplateAlignment() {
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<Read.IRead> { Read("EVQLVESGGG") },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<Read.IRead> { Read("EVQLV"), Read("ESGGG"), Read("EVQ"), Read("LVES"), Read("GGG") });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(2.0, 10).ToList();
            CompareDOC(doc_expected, doc);
        }

        [TestMethod]
        public void SingleAATemplateAlignment() {
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<Read.IRead> { Read("E") },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<Read.IRead> { Read("E") });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(1.0, 1).ToList();
            CompareDOC(doc_expected, doc);
        }

        [TestMethod]
        public void NoAATemplateAlignment() {
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<Read.IRead> { Read("") },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(0.0, 0).ToList();
            CompareDOC(doc_expected, doc);
        }

        void CompareDOC(List<double> actual, List<double> expected) {
            Assert.IsNotNull(actual);
            Assert.IsNotNull(expected);
            Assert.AreEqual(actual.Count, expected.Count, "Assume same length");

            Console.Write("\n");
            foreach (var doc in actual) Console.Write($" {doc}");
            Console.Write("\n");
            foreach (var doc in expected) Console.Write($" {doc}");

            for (int index = 0; index < actual.Count; index++)
                Assert.AreEqual(actual[index], expected[index]);
        }
    }

}