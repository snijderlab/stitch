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

namespace GenerateTestsNS {
    /// <summary> A class to run this code from the commandline. </summary>
    class ToRunWithCommandLine {
        /// <summary> The list with all information for all tasks that have to be done. </summary>
        static List<(string, string, List<(string, string, double)>, double[], bool)> inputQueue = new List<(string, string, List<(string, string, double)>, double[], bool)>();
        /// <summary> The Main function which is run if the code is compiled. </summary>
        static void Main() {
            //GenerateTests.GenerateTest("Test mAB\\IgG1-K-001.txt", "Generated\\reads-IgG1-K-001-Aspecific-NotMissed.txt", new List<(string, string, double)>{("Aspecific", "nonspecific", 0.002)}, new double[]{0.5, 0.75, 1.0}, 5, 40, false);
            GenerateRuns();
        }
        /// <summary> A function to generate a lot of tests in bulk. It first creates a list of all tests to be generated.
        /// Then processes this list in parallel to generate these tests. </summary>
        static void GenerateRuns() 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var prots = new List<(string, string, double)> {("Trypsin", "(?<=K|R)(?!P)", 1.0), ("Chymotrypsin", "(?<=W|Y|F|M|L)", 1.0), ("LysC", "(?<=K)", 1.0), ("Aspecific", "nonspecific", 0.007), ("Alfalytic protease", "(?<=T|A|S|V)", 1.0)};
            var percents = new double[] {1.0, 0.75, 0.5, 0.25};
            
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

            // Excecute all tasks in parrallel
            Parallel.ForEach(inputQueue, (i) => worker(i));

            stopwatch.Stop();
            Console.WriteLine($"Generated {count*percents.Length} samples in {stopwatch.ElapsedMilliseconds} ms");
        }
        /// <summary> The function to operate on the list of tasks to by run in parallel. </summary>
        /// <param name="workItem"> The task to perform. </param>
        static void worker((string, string, List<(string, string, double)>, double[], bool) workItem)
        {
            Console.WriteLine("Starting on: " + workItem.Item2);
            GenerateTests.GenerateTest(workItem.Item1, workItem.Item2, workItem.Item3, workItem.Item4, 5, 40, workItem.Item5);
        }
    }
    /// <summary> A library class to have all functions needen to generate reads based on sequences in batch. </summary>
    class GenerateTests {
        /// <summary> Generates tests based on a sequence in the given file (in fasta format). It reads in the sequences
        /// from the file. And passes the work onto GenerateTest.</summary>
        /// <param name="fastafilename">The filename of the file to get the sequence(s) from.</param>
        /// <param name="filename">The base filename of where to save the generated reads to.</param>
        /// <param name="proteases">The proteases to be used for digestion.</param>
        /// <param name="percents">The different percentages to be used for generating the tests.</param>
        /// <param name="minlength">The minimal length of generated reads.</param>
        /// <param name="maxlength">The maximal length of generated reads. Will discard any reads longer than this.</param>
        /// <param name="missedcleavages">Whether or not missed cleavages should be allowed. 
        /// Be carefull this behaviour is not implemented very well.</param>
        public static void GenerateTest(
            string fastafilename, 
            string filename, 
            List<(string, string, double)> proteases, 
            double[] percents, 
            int minlength, 
            int maxlength, 
            bool missedcleavages) 
        {
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

            // Pass the sequence into the other function
            GenerateTest(seqs, filename, proteases, percents, minlength, maxlength, missedcleavages, fastafilename);
        }
        /// <summary> Generates tests based on a sequences.</summary>
        /// <param name="sequences">The sequences to be digested.</param>
        /// <param name="filename">The base filename of where to save the generated reads to.</param>
        /// <param name="proteases">The proteases to be used for digestion.</param>
        /// <param name="percents">The different percentages to be used for generating the tests.</param>
        /// <param name="minlength">The minimal length of generated reads.</param>
        /// <param name="maxlength">The maximal length of generated reads. Will discard any reads longer than this.</param>
        /// <param name="missedcleavages">Whether or not missed cleavages should be allowed. 
        /// Be carefull this behaviour is not implemented very well.</param>
        /// <param name="fastafilename">The filename of the file where the sequences originated from. Solely used for naming the tests.</param>
        public static void GenerateTest(
            List<(string, string)> sequences, 
            string filename, 
            List<(string, string, double)> proteases, 
            double[] percents, 
            int minlength, 
            int maxlength, 
            bool missedcleavages, 
            string fastafilename = null) 
        {
            // Create as much buffers s needed for all files
            var buffers = new StringBuilder[percents.Length];
            for (int i = 0; i < percents.Length; i++)
            {
                buffers[i] = new StringBuilder();
                // Save the file where the sequences came from if it was set
                if (fastafilename != null) buffers[i].AppendLine($"# generate_tests\\{fastafilename}");
            }

            Random random = new Random();

            // Loop through all given parameters to generate all the reads
            foreach (var sequence in sequences) {
                foreach(var protease in proteases) {
                    // Generate the full set of reads 
                    var peptides = new List<string>();

                    if (protease.Item2 == "nonspecific") {
                        // Generate all reads with the given restrictions on the length
                        for (int j = 0; j < sequence.Item2.Length; j++) {
                            string buff = "";
                            for (int k = 0; j+k < sequence.Item2.Length && k < maxlength; k++) {
                                buff += sequence.Item2[j+k];
                                if (k > minlength) {
                                    peptides.Add(buff);
                                } 
                            }
                        }
                    }
                    else {
                        // Digest the sequence based on the given rules
                        peptides = Regex.Split(sequence.Item2, protease.Item2).Where(x => x.Length < maxlength && x.Length > minlength).ToList();
                        
                        if (missedcleavages) {
                            var amountofpeptides = peptides.Count();
                            for (int j = 0; j < amountofpeptides - 1; j++) {
                                peptides.Add(peptides[j].ToString() + peptides[j+1].ToString());
                            }
                        }
                    }

                    // Generate the different subsets based on the percentages given
                    for (int i = 0; i < percents.Length; i++) {
                        buffers[i].AppendLine($"# Generated sample - {sequence.Item1} - {protease.Item1} - {percents[i]*100}%");
                        foreach (var pep in peptides) {
                            if (random.NextDouble() < percents[i]*protease.Item3) {
                                buffers[i].AppendLine(pep.ToString());
                            }
                        }
                    }
                }
            }            

            // Save the reads to the correct files
            for (int i = 0; i < percents.Length; i++) {
                string finalfilename = Path.ChangeExtension(Path.GetFullPath(filename), "");
                finalfilename = new string(finalfilename.Take(finalfilename.Length - 1).ToArray()) + $"-{percents[i]*100:F2}.txt";
                
                StreamWriter sw = File.CreateText(finalfilename);
                sw.Write(buffers[i].ToString());
                sw.Close();
            }
        }
    }
}