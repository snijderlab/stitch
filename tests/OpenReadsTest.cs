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
    public class OpenReads_Test
    {
        /// <summary>
        /// All readsfiles given as examples should be valid
        /// </summary>
        [DataRow(@"examples/001/reads.txt")]
        [DataRow(@"examples/002/reads.txt")]
        [DataRow(@"examples/003/reads.txt")]
        [DataRow(@"examples/004/reads.txt")]
        [DataRow(@"examples/005/reads.txt")]
        [DataRow(@"examples/006/reads.txt")]
        [DataRow(@"examples/007/reads.txt")]
        [DataRow(@"examples/010/reads.txt")]
        [DataRow(@"examples/011/reads-IgG2-K-002-all-25,00.txt")]
        [DataRow(@"examples/011/reads-IgG2-K-002-all-50,00.txt")]
        [DataRow(@"examples/011/reads-IgG2-K-002-all-75,00.txt")]
        [DataRow(@"examples/011/reads-IgG2-K-002-all-100,00.txt")]
        [DataTestMethod]
        public void ExampleSimpleFiles(string file)
        {
            var namefilter = new NameFilter();
            OpenReads.Simple(namefilter, new MetaData.FileIdentifier(Globals.Root + file, ""));
        }
        [DataRow("examples/009/reads.fasta")]
        [DataTestMethod]
        public void ExampleFastaFiles(string file)
        {
            var namefilter = new NameFilter();
            OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(Globals.Root + file, ""), new Regex("(.*)"));
        }
        [DataRow(@"examples/008/Herceptin_ETHCD.csv")]
        [DataRow(@"examples/008/Herceptin_HCD.csv")]
        [DataTestMethod]
        public void ExamplePeaksFiles(string file)
        {
            var namefilter = new NameFilter();
            OpenReads.Peaks(namefilter, new MetaData.FileIdentifier(Globals.Root + file, ""), FileFormat.Peaks.PeaksX(), new AssemblyNameSpace.RunParameters.Input.PeaksParameters());
        }
        /// <summary>
        /// All templates given as examples should be valid FASTA files
        /// </summary>
        [TestMethod]
        public void TestExamples()
        {
            var path = Globals.Root + @"examples/templates";
            var files = Directory.GetFiles(path);
            var namefilter = new NameFilter();
            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine(file);
                    OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(file, ""), new Regex("(.*)"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"At file {file}");
                    Console.WriteLine($"{e.Message}");
                }
            }
        }
    }
}