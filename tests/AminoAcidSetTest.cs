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
    public class AminoAcidSet_Test {
        readonly Alphabet alp;
        public AminoAcidSet_Test() {
            alp = new Alphabet("*;A;B\nA;1;0\nB;0;1", Alphabet.AlphabetParamType.Data, 12, 1);
        }

        [TestMethod]
        public void Creation() {
            var aa = new AminoAcid[] { new AminoAcid(alp, 'A'), new AminoAcid(alp, 'A') };
            var set = new AminoAcidSet(aa);
            Console.WriteLine(set);
            Assert.AreEqual(0b000001_000001u, set.Value);

            aa = new AminoAcid[] { new AminoAcid(alp, 'B'), new AminoAcid(alp, 'B') };
            set = new AminoAcidSet(aa);
            Console.WriteLine(set);
            Assert.AreEqual(0b000010_000010u, set.Value);
        }

        [TestMethod]
        public void Sort() {
            var ab = new AminoAcid[] { new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B') };
            var ba = new AminoAcid[] { new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A') };
            Assert.AreEqual(ab.ToSortedAminoAcidSet(), ba.ToSortedAminoAcidSet());
        }

        [TestMethod]
        public void TooBig() {
            var perfect = new AminoAcid[10] { new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B') };
            var too_big = new AminoAcid[11] { new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A'), new AminoAcid(alp, 'B'), new AminoAcid(alp, 'A') };
            perfect.ToSortedAminoAcidSet();
            Assert.ThrowsException<ArgumentException>(() => too_big.ToSortedAminoAcidSet());
        }
    }
}