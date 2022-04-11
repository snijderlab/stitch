using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;

namespace BatchFilesTestNameSpace
{
    [TestClass]
    public class BatchFile_Test
    {
        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestPublicExamples()
        {
            var cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Globals.Root);
            foreach (var file in Directory.GetFiles("batchfiles"))
            {
                try
                {
                    AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(file);
                }
                catch
                {
                    Console.WriteLine($"At file {file}");
                    throw;
                }
            }
            Directory.SetCurrentDirectory(cwd);
        }

        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestSmallExamples()
        {
            var cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Globals.Root);
            foreach (var file in Directory.GetFiles("tests/batchfiles/test_files"))
            {
                try
                {
                    if (file.EndsWith(".txt"))
                        AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(file);
                }
                catch
                {
                    Console.WriteLine($"At file {file}");
                    throw;
                }
            }
            Directory.SetCurrentDirectory(cwd);
        }
    }
}