using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class OpenReads_Test
    {
        /// <summary>
        /// All readsfiles given as examples should be valid
        /// </summary>
        [DataRow(@"../../../../examples/001/reads.txt")]
        [DataRow(@"../../../../examples/002/reads.txt")]
        [DataRow(@"../../../../examples/003/reads.txt")]
        [DataRow(@"../../../../examples/004/reads.txt")]
        [DataRow(@"../../../../examples/005/reads.txt")]
        [DataRow(@"../../../../examples/006/reads.txt")]
        [DataRow(@"../../../../examples/007/reads.txt")]
        [DataRow(@"../../../../examples/010/reads.txt")]
        [DataRow(@"../../../../examples/011/reads-IgG2-K-002-all-25,00.txt")]
        [DataRow(@"../../../../examples/011/reads-IgG2-K-002-all-50,00.txt")]
        [DataRow(@"../../../../examples/011/reads-IgG2-K-002-all-75,00.txt")]
        [DataRow(@"../../../../examples/011/reads-IgG2-K-002-all-100,00.txt")]
        [DataTestMethod]
        public void ExampleSimpleFiles(string file) {
            OpenReads.Simple(new MetaData.FileIdentifier(file, ""));
        }
        [DataRow(@"../../../../examples/009/reads.fasta")]
        [DataTestMethod]
        public void ExampleFastaFiles(string file) {
            OpenReads.Fasta(new MetaData.FileIdentifier(file, ""));
        }
        [DataRow(@"../../../../examples/008/Herceptin_ETHCD.csv")]
        [DataRow(@"../../../../examples/008/Herceptin_HCD.csv")]
        [DataTestMethod]
        public void ExamplePeaksFiles(string file) {
            OpenReads.Peaks(new MetaData.FileIdentifier(file, ""), 99, 90, FileFormat.Peaks.NewFormat(), 5);
        }
        /// <summary>
        /// All templates given as examples should be valid FASTA files
        /// </summary>
        [TestMethod]
        public void TestExamples() {
            var path = @"../../../../examples/templates";
            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                Console.WriteLine(file);
                OpenReads.Fasta(new MetaData.FileIdentifier(file, ""));
            }
        }
    }
}