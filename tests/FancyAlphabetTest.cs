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
        public void Index() {
            var a = AminoAcid.FromString("A", alphabet).Unwrap();
            var c = AminoAcid.FromString("C", alphabet).Unwrap();
            var aa = AminoAcid.FromString("AA", alphabet).Unwrap();
            var aaa = AminoAcid.FromString("AAA", alphabet).Unwrap();
            Assert.AreNotEqual(0, alphabet.Index(a));
            Assert.AreNotEqual(0, alphabet.Index(c));
            Assert.AreNotEqual(0, alphabet.Index(aa));
            Assert.AreNotEqual(0, alphabet.Index(aaa));
            Assert.AreEqual(alphabet.Index(a), alphabet.Index(a));
            Assert.AreNotEqual(alphabet.Index(a), alphabet.Index(c));
            Assert.AreEqual(alphabet.Index(aa), alphabet.Index(aa));
            Assert.AreEqual(alphabet.Index(aaa), alphabet.Index(aaa));
            Assert.AreNotEqual(alphabet.Index(a), alphabet.Index(aaa));
            Assert.AreNotEqual(alphabet.Index(aa), alphabet.Index(aaa));
        }

        [TestMethod]
        public void Similarity() {
            Assert.AreEqual(5, alphabet.Score(AA("L"), AA("I")));
            Assert.AreEqual(5, alphabet.Score(AA("I"), AA("L")));
            Assert.AreEqual(5, alphabet.Score(AA("N"), AA("GG")));
            Assert.AreEqual(5, alphabet.Score(AA("GG"), AA("N")));
        }

        [TestMethod]
        public void Nonidentity() {
            Assert.AreEqual(0, alphabet.Score(AA("AA"), AA("CC")));
            Assert.AreEqual(0, alphabet.Score(AA("Q"), AA("GG")));
            Assert.AreEqual(0, alphabet.Score(AA("QA"), AA("AVA")));
            Assert.AreEqual(0, alphabet.Score(AA("Q"), AA("AVA")));
        }

        [TestMethod]
        public void Switched() {
            Assert.AreEqual(4, alphabet.Score(AA("AC"), AA("CA")));
            Assert.AreEqual(4, alphabet.Score(AA("QA"), AA("AQ")));
            Assert.AreEqual(6, alphabet.Score(AA("AAV"), AA("AVA")));
            Assert.AreEqual(6, alphabet.Score(AA("VAA"), AA("AAV")));
        }
    }
}