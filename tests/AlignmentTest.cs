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
            var alphabet = ScoringMatrix.Default();
            Assert.IsTrue(alphabet.Contains('A'));
            Console.WriteLine(alphabet.Debug());
            var read_a = new Read.Simple(AminoAcid.FromString("ACCGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACCGW", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(40, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMMMM", result.ShortPath());
        }

        [TestMethod]
        public void Insertion() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("ACGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACFGW", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(27, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMIMM", result.ShortPath());
            Assert.AreEqual("2M1I2M", result.VeryShortPath());
        }

        [TestMethod]
        public void Deletion() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("ACFGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("ACGW", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Assert.AreEqual(27, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMDMM", result.ShortPath());
        }

        [TestMethod]
        public void Isomass() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("AFNW", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(29, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMS[2,1]M", result.ShortPath());
        }

        [TestMethod]
        public void Switched() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("AGFGW", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(28, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MS[2,2]MM", result.ShortPath());
        }

        [TestMethod]
        public void Local() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Local);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(24, result.Score);
            Assert.AreEqual(1, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMM", result.ShortPath());
        }

        [TestMethod]
        public void Global() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.Global);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(13, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("DMMMMD", result.ShortPath());
        }

        [TestMethod]
        public void GlobalForB() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("AFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FGGD", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.GlobalForB);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(23, result.Score);
            Assert.AreEqual(1, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("MMMM", result.ShortPath());
        }

        [TestMethod]
        public void StartWithIsomass() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("DLVQL", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("EVVQL", alphabet).Unwrap());
            var result = new Alignment(read_a, read_b, alphabet, AlignmentType.GlobalForB);
            Console.WriteLine(result.Summary());
            Assert.AreEqual(29, result.Score);
            Assert.AreEqual(0, result.StartA);
            Assert.AreEqual(0, result.StartB);
            Assert.AreEqual("S[2,2]MMM", result.ShortPath());
        }

        [TestMethod]
        public void TestEndAlignment() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("MELSSLRSEDTAVYYCARAFGGEW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FNEWAEYFQHWGQGTLVTVSS", alphabet).Unwrap());
            var result = Alignment.EndAlignment(read_a.Sequence.Sequence, read_b.Sequence.Sequence, alphabet, 6);
            Console.WriteLine($"Best Alignment: {result.Best.Score} at {result.Best.Position}");
            foreach (var set in result.Scores) {
                Console.WriteLine($"\t{set.Score} at {set.Position}");
            }
            Assert.AreEqual(5, result.Best.Position);
            Assert.AreEqual(24, result.Best.Score);
        }

        [TestMethod]
        public void TestEndAlignmentWithGap() {
            var alphabet = ScoringMatrix.Default();
            var read_a = new Read.Simple(AminoAcid.FromString("MELSSLRSEDTAVYYCARAFGGW", alphabet).Unwrap());
            var read_b = new Read.Simple(AminoAcid.FromString("FNEWAEYFQHWGQGTLVTVSS", alphabet).Unwrap());
            var result = Alignment.EndAlignment(read_a.Sequence.Sequence, read_b.Sequence.Sequence, alphabet, 6);
            Console.WriteLine($"Best Alignment: {result.Best.Score} at {result.Best.Position}");
            foreach (var set in result.Scores) {
                Console.WriteLine($"\t{set.Score} at {set.Position}");
            }
            Assert.AreEqual(4, result.Best.Position);
            Assert.AreEqual(16, result.Best.Score);
        }
    }
}