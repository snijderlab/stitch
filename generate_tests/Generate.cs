using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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
// (?<=) C-terminal
// (?=) N-terminal
// ("Trypsin", "(?<=K|R)(?!P)")
// ("Chymotrypsin", "(?<=W|Y|F|M|L)")
// ("Alfalytic protease", "(?<=T|A|S|V)")
// ("Thermolysin", "(?=.)") => nonspecific
// ("GluC", "(?<=E)")
// ("LysC", "(?<=K)")
// ("Elastase", "(?=.)") => nonspecific

namespace GenerateTestsNS {
    class ToRunWithCommandLine {
        static BlockingCollection<(string, string, List<(string, string, double)>, double[], bool)> inputQueue = new BlockingCollection<(string, string, List<(string, string, double)>, double[], bool)>();
        static void Main() {
            //GenerateTests.GenerateTest("Test mAB\\IgG1-K-001.txt", "Generated\\reads-IgG1-K-001-Aspecific-NotMissed.txt", new List<(string, string, double)>{("Aspecific", "nonspecific", 0.002)}, new double[]{0.5, 0.75, 1.0}, 5, 40, false);
            GenerateRuns();
        }
        static void GenerateRuns() 
        {
            /*Console.WriteLine("Generate samples");
            var seqs = new List<(string, string)> {("Prot#1", "EKQLGCTYLMKLPEVAAGVQSARFSVEDSHTIDNPGNRIL"), ("Prot#1 with variable mid", "EKQLGCTYLMKLPEKLIRDVECTRSVEDSHTIDNPGNRIL")};
            //var prots = new string[] {".(?:(?<![KR](?!P)).)*"};
            var prots = new List<(string, string)> {("Trypsin", "(?<=K|R)(?!P)"), ("Chymotrypsin", "(?<=W|Y|F)")};
            var percents = new double[] {1.0, 0.5, 0.1};
            GenerateTests.GenerateTest(@"\Test mAB\190508_mAb-mix_proteases_01.fasta", "test.txt", prots, percents, 5, 40);*/
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int threadCount = 8;
            Task[] workers = new Task[threadCount];

            for (int i = 0; i < threadCount; ++i)
            {
                int workerId = i;
                Task task = new Task(() => worker(workerId));
                workers[i] = task;
                task.Start();
            }

            var prots = new List<(string, string, double)> {("Trypsin", "(?<=K|R)(?!P)", 1.0), ("Chymotrypsin", "(?<=W|Y|F|M|L)", 1.0), ("LysC", "(?<=K)", 1.0), ("Aspecific", "nonspecific", 0.007), ("Alfalytic protease", "(?<=T|A|S|V)", 1.0)};
            var percents = new double[] {1.0, 0.75, 0.5, 0.25}; //new double[] {1.0, 0.9, 0.8, 0.7, 0.6, 0.5};
            int count = 0;
            
            foreach (var file in Directory.GetFiles(@"Test mAB\")) {
                count++;
                // All proteases
                string outputpath = "Generated/reads-" + Path.GetFileNameWithoutExtension(file) + "-all.txt";
                inputQueue.Add((file, outputpath, prots, percents, false));

                // All combinations tryp + chym + wildcard
                for (int i = 2; i < prots.Count(); i++) {
                    outputpath = "Generated/reads-" + Path.GetFileNameWithoutExtension(file) + $"-Trypsin,Chymotrypsin,{prots[i].Item1}.txt";
                    inputQueue.Add((file, outputpath, new List<(string, string, double)>{prots[0], prots[1], prots[i]}, percents, false));
                }
            }
            inputQueue.CompleteAdding();
            Task.WaitAll(workers);
            stopwatch.Stop();
            Console.WriteLine($"Generated {count*percents.Length} samples in {stopwatch.ElapsedMilliseconds} ms");
        }
        static void worker(int workerId)
        {
            Console.WriteLine("Worker {0} is starting.", workerId);

            foreach (var workItem in inputQueue.GetConsumingEnumerable())
            {
                Console.WriteLine("Starting on: " + workItem.Item2);
                GenerateTests.GenerateTest(workItem.Item1, workItem.Item2, workItem.Item3, workItem.Item4, 5, 40, workItem.Item5);
            }

            Console.WriteLine("Worker {0} is stopping.", workerId);
        }
    }
    class GenerateTests {
         public static void GenerateTest(string fastafilename, string filename, List<(string, string, double)> proteases, double[] percents, int minlength, int maxlength, bool missedcleavages) {
             var fastafile = File.ReadAllText(fastafilename);
             var raw_sequences = Regex.Split(fastafile, ">");
             var seqs = new List<(string, string)> ();

             foreach (string seq in raw_sequences) {
                 var seq_lines = seq.Split("\n".ToCharArray());
                 string identifier = seq_lines[0].Trim();
                 string sequence = "";
                 for (int i = 1; i < seq_lines.Length; i++) {
                     sequence += seq_lines[i].Trim("\r\n\t 0123456789".ToCharArray());
                 }
                 if (identifier != "" && sequence != "") seqs.Add((identifier, sequence));
             }
             GenerateTest(seqs, filename, proteases, percents, minlength, maxlength, missedcleavages, fastafilename);
         }
        public static void GenerateTest(List<(string, string)> sequences, string filename, List<(string, string, double)> proteases, double[] percents, int minlength, int maxlength, bool missedcleavages, string fastafilename = null) {
            var buffers = new StringBuilder[percents.Length];
            for (int i = 0; i < percents.Length; i++)
            {
                buffers[i] = new StringBuilder();
                if (fastafilename != null) buffers[i].AppendLine($"# generate_tests\\{fastafilename}");
            }

            Random random = new Random();

            foreach (var sequence in sequences) {
                foreach(var protease in proteases) {
                    for (int i = 0; i < percents.Length; i++) {
                        buffers[i].AppendLine($"# Generated sample - {sequence.Item1} - {protease.Item1} - {percents[i]*100}%");
                        var peptides = new List<string>();

                        if (protease.Item2 == "nonspecific") {
                            for (int j = 0; j < sequence.Item2.Length; j++) {
                                string buff = "";
                                for (int k = 0; k < sequence.Item2.Length - j && k < maxlength; k++) {
                                    buff += sequence.Item2[j+k];
                                    if (k > minlength) {
                                        peptides.Add(buff);
                                    } 
                                }
                            }
                        }
                        else {
                            peptides = Regex.Split(sequence.Item2, protease.Item2).Where(x => x.Length < maxlength && x.Length > minlength).ToList();
                            var amountofpeptides = peptides.Count();
                            if (missedcleavages) {
                                for (int j = 0; j < amountofpeptides - 1; j++) {
                                    peptides.Add(peptides[j].ToString() + peptides[j+1].ToString());
                                }
                            }
                        }

                        foreach (var pep in peptides) {
                            if (random.NextDouble() < percents[i]*protease.Item3) {
                                buffers[i].AppendLine(pep.ToString());
                            }
                        }
                    }
                }
            }            

            for (int i = 0; i < percents.Length; i++) {
                string basepath = Path.ChangeExtension(Path.GetFullPath(filename), "");
                basepath = new string(basepath.Take(basepath.Length - 1).ToArray()) + $"-{percents[i]*100:F2}.txt";
                StreamWriter sw = File.CreateText(basepath);
                sw.Write(buffers[i].ToString());
                sw.Close();
            }
        }
    }
}