using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using static Stitch.Template;

namespace StitchTest
{
    [TestClass]
    public class AmbiguityTree_Test
    {
        static Alphabet alp = new Alphabet("*;A;B;C;.\nA;1;0;0;-1\nB;0;1;0;-1\nC;0;0;1,-1\n.;-1;-1;-1;0", Alphabet.AlphabetParamType.Data, 12, 1);
        static AminoAcid a = new AminoAcid(alp, 'A');
        static AminoAcid b = new AminoAcid(alp, 'B');
        static AminoAcid c = new AminoAcid(alp, 'C');

        [TestMethod]
        public void SimplePath()
        {
            // A → A → A → A → A
            //       ↘ B ↗

            var root = new AmbiguityTreeNode(a);
            root.AddPath(new List<AminoAcid>() { a, a, b, b }, 1.0);
            root.AddPath(new List<AminoAcid>() { a, b, b, b }, 2.0);
            Console.WriteLine($"BEFORE\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("1-2-2-2", root.Topology());
            root.Simplify();
            Console.WriteLine($"AFTER\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("1-2-2-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithMultiple()
        {
            // A ↘
            // B → A → A
            // C ↗

            var root = new AmbiguityTreeNode(a);
            root.AddPath(new List<AminoAcid>() { a, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { b, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { c, a, a }, 1.0);
            Console.WriteLine($"BEFORE\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-3-3", root.Topology());
            root.Simplify();
            Console.WriteLine($"AFTER\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-3-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithComplexPath()
        {
            // C ↘
            // A → A ↘
            //   ↗
            // B → B → A → A

            var root = new AmbiguityTreeNode(a);
            root.AddPath(new List<AminoAcid>() { a, a, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { b, b, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { c, a, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { b, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { b }, 1.0);
            Console.WriteLine($"BEFORE\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-4-3-3", root.Topology());
            root.Simplify();
            Console.WriteLine($"AFTER\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-4-2-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithComplexPathWithPreamble()
        {
            // * → * → * → C ↘
            // * → * → * → A → A ↘
            // * → * → * → B → B → A → A

            var root = new AmbiguityTreeNode(a);
            root.AddPath(new List<AminoAcid>() { a, b, c, a, a, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { b, c, a, b, b, a, a }, 1.0);
            root.AddPath(new List<AminoAcid>() { c, a, b, c, a, a, a }, 1.0);
            Console.WriteLine($"BEFORE\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-3-3-3-3-3-3", root.Topology());
            root.Simplify();
            Console.WriteLine($"AFTER\nflowchart LR;\n{root.Mermaid()}");
            Assert.AreEqual("3-3-3-3-3-2-1", root.Topology());
        }
    }
}