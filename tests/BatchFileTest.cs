using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;

namespace StitchTest
{
    [TestClass]
    public class BatchFile_Test
    {
        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [DataTestMethod]
        [DataRow("basic.txt")]
        [DataRow("monoclonal.txt")]
        [DataRow("polyclonal.txt")]
        public void TestPublicExamples(string file)
        {
            try
            {
                AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(Globals.Root + "batchfiles/" + file, new RunVariables());
            }
            catch (Exception e)
            {
                AssemblyNameSpace.InputNameSpace.ErrorMessage.PrintException(e);
                Console.WriteLine($"At file {file}");
                throw;
            }
        }

        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestSmallExamples()
        {
            Console.WriteLine($"Root folder: {Path.GetFullPath(Globals.Root)}");
            foreach (var file in Directory.GetFiles(Globals.Root + "tests/test_files"))
            {
                try
                {
                    if (file.EndsWith(".txt"))
                        AssemblyNameSpace.ToRunWithCommandLine.RunBatchFile(file, new RunVariables());
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