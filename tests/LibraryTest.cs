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
    public class Library_Test
    {
        [TestMethod]
        public void Test()
        {
            var tags = new List<string> { "VTVP", "VVTVP", "VVSSLSFL", "TVP", "TVVP", "VTVVP", "VSSLSFL", "SSLSFL", "SLSFL", "WSV", "LSFL", "PCM", "THS", "SFL", "SWC", "SAW", "SWA" };
            var alphabet = new Alphabet(Globals.Root + "examples/alphabets/common_errors_alphabet.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var assembler = new Assembler(4, 3, 3, false, alphabet);
            var nameFilter = new NameFilter();
            var reads = tags.Select(x => (x, (MetaData.IMetaData)new MetaData.Simple(new MetaData.FileIdentifier(), nameFilter))).ToList();
            foreach (var read in reads)
                read.Item2.FinaliseIdentifier();

            assembler.GiveReads(reads);
            assembler.Assemble();
            new HTMLReport(new ReportInputParameters(assembler), true, Environment.ProcessorCount).Save(System.IO.Path.GetTempPath() + "/test.html");
            var paths = assembler.GetAllPathsMultipleReads().Select(x => AminoAcid.ArrayToString(x.Sequence)).ToArray();
        }
    }
}