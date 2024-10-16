using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;

namespace StitchTest {
    [TestClass]
    public class AmbiguityTree_Test {
        readonly static ScoringMatrix alp = ScoringMatrix.Default();
        readonly static AminoAcid A = new AminoAcid(alp, 'A');
        readonly static AminoAcid B = new AminoAcid(alp, 'B');
        readonly static AminoAcid C = new AminoAcid(alp, 'C');

        [TestMethod]
        public void SimplePath() {
            // A → A → A → A → A
            //       ↘ B ↗

            var root = new AmbiguityTreeNode(A);
            root.AddPath(new List<AminoAcid>() { A, A, B, B }, 1.0);
            root.AddPath(new List<AminoAcid>() { A, B, B, B }, 2.0);
            Assert.AreEqual("1-2-2-2", root.Topology());
            root.Simplify();
            Assert.AreEqual("1-2-2-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithMultiple() {
            // A ↘
            // B → A → A
            // C ↗

            var root = new AmbiguityTreeNode(A);
            root.AddPath(new List<AminoAcid>() { A, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { B, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { C, A, A }, 1.0);
            Assert.AreEqual("3-3-3", root.Topology());
            root.Simplify();
            Assert.AreEqual("3-3-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithComplexPath() {
            // C ↘
            // A → A ↘
            //   ↗
            // B → B → A → A

            var root = new AmbiguityTreeNode(A);
            root.AddPath(new List<AminoAcid>() { A, A, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { B, B, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { C, A, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { B, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { B }, 1.0);
            Assert.AreEqual("3-4-3-3", root.Topology());
            root.Simplify();
            Assert.AreEqual("3-4-2-1", root.Topology());
        }

        [TestMethod]
        public void JoinWithComplexPathWithPreamble() {
            // * → * → * → C ↘
            // * → * → * → A → A ↘
            // * → * → * → B → B → A → A

            var root = new AmbiguityTreeNode(A);
            root.AddPath(new List<AminoAcid>() { A, B, C, A, A, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { B, C, A, B, B, A, A }, 1.0);
            root.AddPath(new List<AminoAcid>() { C, A, B, C, A, A, A }, 1.0);
            Assert.AreEqual("3-3-3-3-3-3-3", root.Topology());
            root.Simplify();
            Assert.AreEqual("3-3-3-3-3-2-1", root.Topology());
        }
    }
}