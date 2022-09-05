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
            Console.WriteLine($"Root folder: {Path.GetFullPath(Globals.Root)}");
            foreach (var file in Directory.GetFiles(Globals.Root + "batchfiles"))
            {
                try
                {
                    AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(file, new RunVariables(false));
                }
                catch
                {
                    Console.WriteLine($"At file {file}");
                    throw;
                }
            }
        }

        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestSmallExamples()
        {
            Console.WriteLine($"Root folder: {Path.GetFullPath(Globals.Root)}");
            foreach (var file in Directory.GetFiles(Globals.Root + "tests/batchfiles/test_files"))
            {
                try
                {
                    if (file.EndsWith(".txt"))
                        AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(file, new RunVariables(false));
                }
                catch
                {
                    Console.WriteLine($"At file {file}");
                    throw;
                }
            }
        }
    }
}