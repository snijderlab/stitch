//(string Alignment, List<double> DepthOfCoverage) CreateTemplateAlignment(Template template, string id, List<string> location)

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
    public class HTMLReport_Test
    {
        [TestMethod]
        public void SingleMatchTemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(new ReadMetaData.FileIdentifier(), new NameFilter());
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
            var buffer = new StringBuilder();
            var doc = HTMLNameSpace.HTMLAsides.CreateTemplateAlignment(buffer, segment.Templates[0], "id", new List<string>(), "");
            var doc_expected = Enumerable.Repeat(1.0, 20).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void MultiMatchTemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(new ReadMetaData.FileIdentifier(), new NameFilter());
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
            var buffer = new StringBuilder();
            var doc = HTMLNameSpace.HTMLAsides.CreateTemplateAlignment(buffer, segment.Templates[0], "id", new List<string>(), "");
            var doc_expected = Enumerable.Repeat(2.0, 10).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void SingleAATemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(new ReadMetaData.FileIdentifier(), new NameFilter());
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
            var buffer = new StringBuilder();
            var doc = HTMLNameSpace.HTMLAsides.CreateTemplateAlignment(buffer, segment.Templates[0], "id", new List<string>(), "");
            var doc_expected = Enumerable.Repeat(1.0, 1).ToList();
            CompareDOC(doc, doc_expected);
        }

        [TestMethod]
        public void NoAATemplateAlignment()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var meta = (ReadMetaData.IMetaData)new ReadMetaData.Simple(new ReadMetaData.FileIdentifier("empty", "empty", null), new NameFilter());
            //SCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR
            var segment = new Segment(
                new List<(string, ReadMetaData.IMetaData)> { ("", meta) },
                alp,
                "segment",
                1.0, // CutoffScore
                0, // Index
                true
                );
            var buffer = new StringBuilder();
            var doc = HTMLNameSpace.HTMLAsides.CreateTemplateAlignment(buffer, segment.Templates[0], "id", new List<string>(), "");
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

        [TestClass]
        public class HTMLBuilderRemoveElementsFromEnd
        {
            [TestMethod]
            public void RemoveAll()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.OpenAndClose("div", "id='0'", "");
                html.OpenAndClose("div", "id='1'", "");
                html.OpenAndClose("div", "id='2'", "");
                html.OpenAndClose("div", "id='3'", "");
                html.AssertOpen(new string[0]);
                html.UnsafeRemoveElementsFromEnd(4);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "");
            }
            [TestMethod]
            public void RemoveHalf()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.OpenAndClose("div", "id='0'", "");
                html.OpenAndClose("div", "id='1'", "");
                html.OpenAndClose("div", "id='2'", "");
                html.OpenAndClose("div", "id='3'", "");
                html.AssertOpen(new string[0]);
                html.UnsafeRemoveElementsFromEnd(2);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "<div id='0'></div><div id='1'></div>");
            }
            [TestMethod]
            public void RemoveSingle()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.Empty("input", "id='0'");
                html.Empty("input", "id='1'");
                html.Empty("input", "id='2'");
                html.Empty("input", "id='3'");
                html.AssertOpen(new string[0]);
                html.UnsafeRemoveElementsFromEnd(2);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "<input id='0'/><input id='1'/>");
            }
            [TestMethod]
            public void RemoveNothing()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.Empty("input", "id='0'");
                html.Empty("input", "id='1'");
                html.Empty("input", "id='2'");
                html.Empty("input", "id='3'");
                html.AssertOpen(new string[0]);
                var content = html.ToString();
                html.UnsafeRemoveElementsFromEnd(0);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == content);
            }
            [TestMethod]
            public void RemoveHierarchy()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.Open("div", "id='0'");
                html.Open("div", "id='1'");
                html.Open("div", "id='2'");
                html.Open("div", "id='3'");
                html.CloseAll();
                html.AssertOpen(new string[0]);
                html.UnsafeRemoveElementsFromEnd(2);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "<div id='0'><div id='1'>");
            }
            [TestMethod]
            public void RemoveMixed()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.Open("div", "id='0'");
                html.Open("div", "id='1'");
                html.OpenAndClose("div", "id='2'", "");
                html.Empty("input", "id='3'");
                html.CloseAll();
                html.AssertOpen(new string[0]);
                html.UnsafeRemoveElementsFromEnd(2);
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "<div id='0'><div id='1'>");
            }
            [TestMethod]
            public void RemoveLong()
            {
                var html = new HTMLNameSpace.HTMLBuilder();
                html.Open("div", "id='0'");
                for (int i = 0; i < 11; i++)
                {
                    html.OpenAndClose("a", "href='#' class='text align-link'", "");
                    html.OpenAndClose("span", "class='symbol'", "");
                    html.Empty("br");
                }
                html.AssertOpen(new string[1] { "div" });
                html.UnsafeRemoveElementsFromEnd(33);
                html.CloseAll();
                html.AssertOpen(new string[0] { });
                Console.WriteLine(html.ToString());
                Assert.IsTrue(html.ToString() == "<div id='0'></div>");
            }
        }
    }

}