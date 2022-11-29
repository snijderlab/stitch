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
            var a = alphabet.Index(AminoAcid.FromString("A", alphabet).Unwrap());
            var c = alphabet.Index(AminoAcid.FromString("C", alphabet).Unwrap());
            var aa = alphabet.Index(AminoAcid.FromString("AA", alphabet).Unwrap());
            var ac = alphabet.Index(AminoAcid.FromString("AC", alphabet).Unwrap());
            var aaa = alphabet.Index(AminoAcid.FromString("AAA", alphabet).Unwrap());
            Console.WriteLine($"A{a} C{c} AA{aa} AC{ac} AAA{aaa}");
            Assert.AreNotEqual(0, a);
            Assert.AreNotEqual(0, c);
            Assert.AreNotEqual(0, aa);
            Assert.AreNotEqual(0, ac);
            Assert.AreNotEqual(0, aaa);
            Assert.AreEqual(a, a);
            Assert.AreNotEqual(a, c);
            Assert.AreEqual(aa, aa);
            Assert.AreEqual(aaa, aaa);
            Assert.AreNotEqual(a, aaa);
            Assert.AreNotEqual(aa, aaa);
            Assert.AreNotEqual(ac, aaa);
            Assert.AreNotEqual(ac, aa);
            Assert.AreNotEqual(ac, a);
        }

        [TestMethod]
        public void Similarity() {
            Assert.AreEqual(5, alphabet.Score(AA("L"), AA("I")));
            Assert.AreEqual(5, alphabet.Score(AA("I"), AA("L")));
            Assert.AreEqual(5, alphabet.Score(AA("N"), AA("GG")));
            Assert.AreEqual(5, alphabet.Score(AA("GG"), AA("N")));
        }

        [TestMethod]
        public void NonIdentity() {
            Assert.AreEqual(0, alphabet.Score(AA("AA"), AA("CC")));
            Assert.AreEqual(0, alphabet.Score(AA("Q"), AA("GG")));
            Assert.AreEqual(0, alphabet.Score(AA("QA"), AA("AVA")));
            Assert.AreEqual(0, alphabet.Score(AA("Q"), AA("AVA")));
        }

        [TestMethod]
        public void Switched() {
            var a = alphabet.Score(AA("AC"), AA("CA"));
            var b = alphabet.Score(AA("QA"), AA("AQ"));
            var c = alphabet.Score(AA("AAV"), AA("AVA"));
            var d = alphabet.Score(AA("VAA"), AA("AAV"));
            Console.WriteLine($"A{a} B{b} C{c} D{d}");
            Assert.AreEqual(4, a);
            Assert.AreEqual(4, b);
            Assert.AreEqual(6, c);
            Assert.AreEqual(6, d);
        }
    }
}