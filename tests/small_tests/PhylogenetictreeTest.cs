using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;
using System.Text.RegularExpressions;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class PhylogeneticTreeTest
    {
        //VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK

        /// <summary>
        /// Test a small sample tree to see it outgroup rooting works
        /// </summary>
        [TestMethod]
        public void TestSmall()
        {
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var sequences = new List<(string, AminoAcid[])>{("A", AminoAcid.FromString("VKAFEALQ", alp)), ("B", AminoAcid.FromString("VKAWWALQ", alp)), ("C", AminoAcid.FromString("VKAWVALQ", alp))};
            var tree = PhylogeneticTree.CreateTree(sequences, alp, false);
            var outgroup_tree = PhylogeneticTree.CreateTree(sequences, alp, true);
            Assert.AreEqual("((A, B), C)", tree.BracketsNotation()); // unrooted, this is how it comes out
            Assert.AreEqual("((C, B), A)", outgroup_tree.BracketsNotation()); // outgroup rooted it comes out as (A, (B, C)), although a bit rotated
        }
    }
}