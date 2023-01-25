using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using Stitch.RunParameters;
using System.Text.RegularExpressions;

namespace StitchTest {
    [TestClass]
    public class PhylogeneticTreeTest {
        //VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK

        /// <summary> Test a small sample tree to see it out group rooting works </summary>
        [TestMethod]
        public void TestSmall() {
            var alp = ScoringMatrix.Default();
            // A VKA(FE)AL(Q)
            // B VKAW W AL W
            // C VKAW(V)AL W
            var sequences = new List<(string, ReadFormat.General)> { ("A", new ReadFormat.Simple(AminoAcid.FromString("VKAFEALQ", alp).Unwrap())), ("B", new ReadFormat.Simple(AminoAcid.FromString("VKAWWALW", alp).Unwrap())), ("C", new ReadFormat.Simple(AminoAcid.FromString("VKAWVALW", alp).Unwrap())) };
            var tree = PhylogeneticTree.CreateTree(sequences, false);
            var out_group_tree = PhylogeneticTree.CreateTree(sequences, true);
            Console.WriteLine('\r' + tree.ToString(false, true, false));
            Console.WriteLine(out_group_tree.ToString(false, true, false));
            Assert.AreEqual("((A, B), C)", tree.BracketsNotation()); // un rooted, this is how it comes out
            Assert.AreEqual("((C, B), A)", out_group_tree.BracketsNotation()); // out group rooted it comes out as (A, (B, C)), although a bit rotated
        }
    }
}