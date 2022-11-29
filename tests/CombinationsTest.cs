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
    public class Combinations_Test {
        [TestMethod]
        public void Combinations2() {
            var data = new List<char> { 'A', 'B', 'C' };
            var combinations = data.Combinations(2).Select(s => string.Join("", s)).ToArray();
            foreach (var option in combinations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("AA", combinations[0]);
            Assert.AreEqual("AB", combinations[1]);
            Assert.AreEqual("AC", combinations[2]);
            Assert.AreEqual("BA", combinations[3]);
            Assert.AreEqual("BB", combinations[4]);
            Assert.AreEqual("BC", combinations[5]);
            Assert.AreEqual("CA", combinations[6]);
            Assert.AreEqual("CB", combinations[7]);
            Assert.AreEqual("CC", combinations[8]);
        }

        [TestMethod]
        public void Combinations3() {
            var data = new List<char> { 'A', 'B', 'C' };
            var combinations = data.Combinations(3).Select(s => string.Join("", s)).ToArray();
            foreach (var option in combinations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("AAA", combinations[0]);
            Assert.AreEqual("AAB", combinations[1]);
            // More
        }

        [TestMethod]
        public void Variations1() {
            var data = new List<char> { 'A', 'B', 'C' };
            var variations = data.Variations().Select(s => s.Item1.ToString() + s.Item2.ToString()).ToArray();
            foreach (var option in variations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("AB", variations[0]);
            Assert.AreEqual("AC", variations[1]);
            Assert.AreEqual("BA", variations[2]);
            Assert.AreEqual("BC", variations[3]);
            Assert.AreEqual("CA", variations[4]);
            Assert.AreEqual("CB", variations[5]);
        }

        [TestMethod]
        public void Variations2() {
            var data = new List<char> { 'A', 'B', 'C' };
            var variations = data.Variations(2).Select(s => string.Join("", s)).ToArray();
            foreach (var option in variations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("AA", variations[0]);
            Assert.AreEqual("AB", variations[1]);
            Assert.AreEqual("AC", variations[2]);
            Assert.AreEqual("BA", variations[3]);
            Assert.AreEqual("BB", variations[4]);
            Assert.AreEqual("BC", variations[5]);
            Assert.AreEqual("CA", variations[6]);
            Assert.AreEqual("CB", variations[7]);
            Assert.AreEqual("CC", variations[8]);
        }

        [TestMethod]
        public void Variations3() {
            var data = new List<char> { 'A', 'B', 'C' };
            var variations = data.Variations(3).Select(s => string.Join("", s)).ToArray();
            foreach (var option in variations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("AAA", variations[0]);
            Assert.AreEqual("AAB", variations[1]);
            Assert.AreEqual("AAC", variations[2]);
        }

        [TestMethod]
        public void Permutations() {
            var data = new List<char> { 'A', 'B', 'C' };
            var permutations = data.Permutations().Select(s => string.Join("", s)).ToArray();
            Console.WriteLine($"Found {permutations.Length} permutations");
            foreach (var option in permutations) {
                Console.Write($",{option}");
            }
            Assert.AreEqual("ABC", permutations[0]);
            Assert.AreEqual("ACB", permutations[1]);
            Assert.AreEqual("BAC", permutations[2]);
            Assert.AreEqual("BCA", permutations[3]);
            Assert.AreEqual("CAB", permutations[4]);
            Assert.AreEqual("CBA", permutations[5]);
        }
    }
}