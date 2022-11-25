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
    public class FancyAlphabet_Test {
        public FancyAlphabet alphabet;

        public FancyAlphabet_Test() {
            alphabet = FancyAlphabet.Default();
        }

        public AminoAcid[] AA(string input) {
            return AminoAcid.FromString(input, alphabet).Unwrap();
        }

        [TestMethod]
        public void Identity() {
            var a = AminoAcid.FromString("A", alphabet).Unwrap();
            var aa = AminoAcid.FromString("AA", alphabet).Unwrap();
            var aaa = AminoAcid.FromString("AAA", alphabet).Unwrap();
            Assert.AreEqual(8, alphabet.Score(a, a));
            Assert.AreEqual(0, alphabet.Score(aa, aa));
            Assert.AreEqual(0, alphabet.Score(aaa, aaa));
        }

        [TestMethod]
        public void Similarity() {
            Assert.AreEqual(5, alphabet.Score(AA("L"), AA("I")));
            Assert.AreEqual(5, alphabet.Score(AA("I"), AA("L")));
            Assert.AreEqual(5, alphabet.Score(AA("N"), AA("GG")));
            Assert.AreEqual(5, alphabet.Score(AA("GG"), AA("N")));
        }
    }
}