//(string Alignment, List<double> DepthOfCoverage) CreateTemplateAlignment(Template template, string id, List<string> location)

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
    public class HTMLReport_Test
    {
        [TestMethod]
        public void SingleMatchTemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(null, new NameFilter());
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<(string, ReadMetaData.IMetaData)> { ("EVQLVESGGGLVQPGGSLRL", meta) },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<(string, ReadMetaData.IMetaData)> { ("EVQLVESGGGLVQPGGSLRL", meta) });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(1.0, 20).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void MultiMatchTemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(null, new NameFilter());
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<(string, ReadMetaData.IMetaData)> { ("EVQLVESGGG", meta) },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<(string, ReadMetaData.IMetaData)> { ("EVQLV", meta), ("ESGGG", meta), ("EVQ", meta), ("LVES", meta), ("GGG", meta) });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(2.0, 10).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void SingleAATemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(null, new NameFilter());
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<(string, ReadMetaData.IMetaData)> { ("E", meta) },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var matches = segment.Match(new List<(string, ReadMetaData.IMetaData)> { ("E", meta) });
            Assert.IsTrue(matches.All(m => m.All(m => m.TemplateIndex == 0)));
            foreach (var (_, match) in matches.SelectMany(m => m)) segment.Templates[0].AddMatch(match);
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(1.0, 1).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void NoAATemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(null, new NameFilter());
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<(string, ReadMetaData.IMetaData)> { ("", meta) },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var doc = segment.Templates[0].ConsensusSequence().Item2;
            var doc_expected = Enumerable.Repeat(0.0, 0).ToList();
            CompareDOC(doc, doc_expected);
        }

        void CompareDOC(List<double> actual, List<double> expected)
        {
            Assert.IsNotNull(actual);
            Assert.IsNotNull(expected);
            Assert.AreEqual(actual.Count, expected.Count);

            Console.Write("\n");
            foreach (var doc in actual) Console.Write($" {doc}");
            Console.Write("\n");
            foreach (var doc in expected) Console.Write($" {doc}");

            for (int index = 0; index < actual.Count; index++)
                Assert.AreEqual(actual[index], expected[index]);
        }

        AminoAcid[] StringToSequence(string input, Alphabet alp)
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