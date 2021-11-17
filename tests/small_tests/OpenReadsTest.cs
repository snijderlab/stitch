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
        /// All templates given as examples should be valid FASTA files
        /// </summary>
        [TestMethod]
        public void TestExamples()
        {
            var path = Globals.Root + @"templates";
            var files = Directory.GetFiles(path);
            var namefilter = new NameFilter();
            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine(file);
                    OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(file, "", null), new Regex("(.*)"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"At file {file}");
                    Console.WriteLine($"{e.Message}");
                }
            }
        }

        /// <summary>
        /// Test the reading of annotated fasta files
        /// </summary>
        [TestMethod]
        public void TestAnnotation()
        {
            var file = Globals.Root + @"templates/Homo_sapiens_IGHV.fasta";
            var namefilter = new NameFilter();
            var reads = OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(file, "", null), new Regex("(.*)")).ReturnOrFail();
            var meta = (MetaData.Fasta)reads[0].MetaData;
            Assert.AreEqual("IGHV1-2", meta.Identifier);
            foreach (var part in meta.AnnotatedSequence)
            {
                Console.WriteLine($"{part.Type}: {part.Sequence}");
            }
            Assert.AreEqual(13, meta.AnnotatedSequence.Count);
        }
    }
}