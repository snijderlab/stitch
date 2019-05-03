using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Chemistry;

// Usable proteases
// "ArgC","AspN","AspN + nterm Glu","Caspase 1",
// "Caspase 2","Caspase 3","Caspase 4","Caspase 5",
// "Caspase 6","Caspase 7","Caspase 8","Caspase 9",
// "Caspase 10","Chymotrypsin high specificity","Chymotrypsin low specificity",
// "Clostridiopeptidase B","CNBr","Enterokinase","Factor Xa",
// "Formic acid","Glutamyl endopeptidase","GranzymeB","Hydroxylamine",
// "Iodosobenzoic acid","LysC","Neutrophil elastase","NTCB","Pepsin pH >2",
// "Pepsin pH 1.3","Proline-endopeptidase","Proteinase K",
// "Staphylococcal peptidase I","Thermolysin","Thrombin",
// "Trypsin/P","Elastase"

// Our: "Trypsin/P", "Chymotrypsin low specificity", alp, "Thermolysin", gluc, "LysC"

namespace GenerateTestsNS {
    class ToRunWithCommandLine {
        static void Main() {
            Console.WriteLine("Generate samples");
            GenerateTests.GenerateTest("EKQLGCTYLMKLPEVAAGVQSARFSVEDSHTIDNPGNRIL", "test.txt", new string[] {"Trypsin"}, 1.0, 2, 5, 40);
        }
    }
    class GenerateTests {
        public static void GenerateTest(string sequence, string filename, string[] proteases, double percent, int maxmissedcleavages, int minlength, int maxlength) {
            var peptides = new List<Peptide>();
            
            foreach(var protease in proteases) {
                var prot = Protease.Proteases.Select(x => x.Title == protease);
                if (prot != null) peptides.AddRange(prot.Digest(sequence, maxmissedcleavages, minlength).Where(x => x.Length < maxlength).ToList());
                else throw new Exception($"Protease \"{protease}\" could not be found");
            }

            var buffer = new StringBuilder();

            buffer.AppendLine($"# Generated sample - {proteases.ToString()}, {percent*100}%");

            Random random = new Random();

            foreach (var pep in peptides) {
                if (random.NextDouble() < percent) {
                    buffer.AppendLine(pep.ToString());
                }
            }

            StreamWriter sw = File.CreateText(filename);
            sw.Write(buffer.ToString());
            sw.Close();
        }
    }
}