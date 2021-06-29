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
    }
}