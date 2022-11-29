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
    public class FancyAlignment_Test {
        [TestMethod]
        public void Equal() {
            var alphabet = FancyAlphabet.Default();
            Assert.IsTrue(alphabet.Contains('A'));
            Console.WriteLine(alphabet.Debug());
            var read_a = new Read.Simple(AminoAcid.FromString("ACCGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACCGW", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(40, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMMMM", result.ShortPath());
        }

        [TestMethod]
        public void Insertion() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("ACGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACFGW", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(27, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMIMM", result.ShortPath());
        }

        [TestMethod]
        public void Deletion() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("ACFGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACGW", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(27, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMDMM", result.ShortPath());
        }

        [TestMethod]
        public void Isomass() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("AFNW", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(29, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMS[2,1]M", result.ShortPath());
        }

        [TestMethod]
        public void Switched() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("AGFGW", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(28, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MS[2,2]MM", result.ShortPath());
        }

        [TestMethod]
        public void Local() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(24, result.score);
            Assert.AreEqual(1, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMM", result.ShortPath());
        }

        [TestMethod]
        public void Global() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.Global);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(13, result.score);
            Assert.AreEqual(0, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("DMMMMD", result.ShortPath());
        }

        [TestMethod]
        public void GlobalForB() {
            var alphabet = FancyAlphabet.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new FancyAlignment(read_a, read_b, alphabet, AlignmentType.GlobalForB);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(23, result.score);
            Assert.AreEqual(1, result.start_a);
            Assert.AreEqual(0, result.start_b);
            Assert.AreEqual("MMMM", result.ShortPath());
        }
    }
}