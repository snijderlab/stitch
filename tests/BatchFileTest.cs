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
    public class BatchFile_Test
    {
        /// <summary>
        /// All batchfiles given as examples should be valid
        /// </summary>
        [TestMethod]
        public void TestExamples() {
            var path = @"../../../../examples/batchfiles";
            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                ParseCommandFile.Batch(file);
            }
        }
    }
}