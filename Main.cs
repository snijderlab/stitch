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
using System.Globalization;

namespace AssemblyNameSpace
{
    /// <summary> This is a project to build a piece of software that is able to rebuild a protein sequence
    /// from reads of a massspectrometer. 
    /// The software is build by Douwe Schulte and was started on 25-03-2019.
    /// It is build in collaboration with and under supervision of Joost Snijder,
    /// from the group "Massspectrometry and Proteomics" at the university of Utrecht. </summary>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
    /// <summary> A Class to be able to run the code from the commandline. To be able to test it easily. 
    /// This will be rewritten when the code is moved to its new repository </summary>
    class ToRunWithCommandLine
    {
        /// <summary> The list of all tasks to be done. To be able to run them in parallel. </summary>
        static List<(int, int, string, string, string, string, string, bool)> inputQueue = new List<(int, int, string, string, string, string, string, bool)>();
        /// <summary> The method that will be run if the code is run from the command line. </summary>
        static void Main()
        {
            var assm = new Assembler(8, 7);
            //assm.GiveReadsPeaks(OpenReads.Peaks(@"C:\Users\Douwe\Downloads\de novo peptides.csv", 90, 99, FileFormat.Peaks.OldFormat(), 8));
            //assm.Assemble();
            //assm.CreateReport();
            //assm.SetAlphabet("Fusion alphabet.csv");
            //assm.OpenReads(@"generate_tests\Generated\reads-mix-perfect.txt");
            assm.GiveReads(OpenReads.Simple(@"generate_tests\Generated\reads-IgG1-K-001-Trypsin,Chymotrypsin,Alfalytic protease-100,00.txt"));
            //assm.OpenReadsPeaks(@"Z:\users\5803969\Benchmarking\190520_MAb-denovo_F1_HCD_Herc_001_DENOVO_7\de novo peptides.csv", 99, 95);
            /*assm.OpenReadsPeaks(new List<(string, string)>{
                (@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\190528_F1_131-2a_ETHCD_01_DENOVO_7\de novo peptides.csv", "ETHCD"),
                (@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\190528_F1_131-2a_HCD_01_DENOVO_7\de novo peptides.csv", "HCD")
                }, 99, 90, Assembler.PeaksFormat.NewFormat());*/
            //for (int i = 0; i < assm.reads.Count(); i++) {
            //    Console.WriteLine(assm.reads[i]);
            //    Console.WriteLine(assm.peaks_reads[i].ToHTML());
            //}   
            assm.Assemble();
            var htmlreport = new HTMLReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads);
            htmlreport.Save("report.html");
            //assm.CreateReport("report.html");
            //Console.WriteLine($"Percentage coverage: {HelperFunctionality.MultipleSequenceAlignmentToTemplate("QVQLVESGGGVVQPGRSLRLSCAASGFSFSNYGMHWVRQAPGKGLEWVALIWYDGSNEDYTDSVKGRFTISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVFPLAPSSKSTSGGTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSSLGTQTYICNVNHKPSNTKVDKRVEPKSCDKTHTCPPCPAPELLGGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSHEDPEVKFNWYVDGVEVHNAKTKPREEQYNSTYRVVSVLTVLHQDWLNGKEYKCKVSNKALPAPIEKTISKAKGQPREPQVYTLPPSREEMTKNQVSLTCLVKGFYPSDIAVEWESNGQPENNYKTTPPVLDSDGSFFLYSKLTVDKSRWQQGNVFSCSVMHEALHNHYTQKSLSLSPGK", assm.reads.Select(x => Assembler.AminoAcid.ArrayToString(x)).ToArray())}");
            //RunGenerated();
            //RunContigsLengthBatch();
            //RunExperimentalBatch();
        }
        static void RunExperimentalBatch()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Experimental results\runs.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nAntibody;Fragmentation method;K;Minimal Homology;Contigs;Avg sequence length;Total Sequence length;mean Connectivity;Total runtime;Drawing time;Contigs Coverage Heavy;Contigs Coverage Light;Contigs Coverage Depth Heavy;Contigs Coverage Depth Light;Total Mapped Contigs;Correct Contigs;Reads Coverage Heavy;Reads Coverage Light;Reads Coverage Depth Heavy;Reads Coverage Depth Light;Correct Contigs Coverage Heavy;Correct Contigs Coverage Light;Correct Contigs Coverage Depth Heavy;Correct Contigs Coverage Depth Light;Contigs Length\n");
            sw.Close();

            int count = 0;
            foreach (var dir in Directory.GetDirectories(@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\"))
            {
                var file = dir + @"\de novo peptides.csv";
                string sequencefile = null;
                if (file.Contains("1446"))
                {
                    sequencefile = @"generate_tests\Experimental results\1446.txt";
                }
                if (file.Contains("Her"))
                {
                    sequencefile = @"generate_tests\Experimental results\her.txt";
                }
                if (file.Contains("CAMP"))
                {
                    sequencefile = @"generate_tests\Experimental results\camp.txt";
                }
                count++;
                for (int k = 6; k <= 10; k++) {
                    inputQueue.Add((k, -2, file, @"generate_tests\Experimental results\" + Path.GetFileName(Path.GetDirectoryName(file)) + $"-{k}-Default alphabet-Contigs Length-Reverse.html", csvfile, "Fusion alphabet.csv", sequencefile, true));
                    inputQueue.Add((k, -2, file, @"generate_tests\Experimental results\" + Path.GetFileName(Path.GetDirectoryName(file)) + $"-{k}-Default alphabet-Contigs Length-No Reverse.html", csvfile, "Fusion alphabet.csv", sequencefile, false));
                }
            }

            // Run all tasks in parallel
            Parallel.ForEach(inputQueue, (i) => worker(i));

            stopwatch.Stop();
            Console.WriteLine($"Assembled {count} files in {stopwatch.ElapsedMilliseconds} ms");
        }
        static void RunContigsLengthBatch()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Results_contigs_length\runs-contigs-length-batch.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nID;Type;Test;Proteases;Percentage;Alphabet;Reads;K;Minimum Homology;Contigs;Avg Sequence length (per contig);Total sequence length;Mean Connectivity;Total runtime;Drawing Time;Reads Coverage Heavy; Reads Correct Heavy;Reads Coverage Light;Reads Correct Light;Contigs Coverage Heavy; Contigs Correct Heavy; Contigs Coverage Light; Contigs Correct Light; Link; Contigs Length\n");
            sw.Close();

            int count = 0;
            foreach (var file in Directory.GetFiles(@"generate_tests\Generated")) {
                if (!file.Contains("Mix") && !file.Contains("mix")) {
                    count++;
                    for (int k = 5; k <= 15; k++) {
                        inputQueue.Add((k, -1, file, @"generate_tests\Results_contigs_length\" + Path.GetFileNameWithoutExtension(file) + $"-{k}-Default alphabet-Contigs Length.html", csvfile, "Default alphabet.csv", File.ReadAllLines(file)[0].Trim("# \t\n\r".ToCharArray()),true));
                        inputQueue.Add((k, -1, file, @"generate_tests\Results_contigs_length\" + Path.GetFileNameWithoutExtension(file) + $"-{k}-Common errors alphabet-Contigs Length.html", csvfile, "Common errors alphabet.csv", File.ReadAllLines(file)[0].Trim("# \t\n\r".ToCharArray()), true));
                    }
                }
            }

            // Run all tasks in parallel
            Parallel.ForEach(inputQueue, (i) => worker(i));

            stopwatch.Stop();
            Console.WriteLine($"Assembled {count} files in {stopwatch.ElapsedMilliseconds} ms");
        }
        static void RunDepthCoverageBatch()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Results\runs-contigs-length-batch.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nID;Type;Test;Proteases;Percentage;Alphabet;Reads;K;Minimum Homology;Contigs;Avg Sequence length (per contig);Total sequence length;Mean Connectivity;Total runtime;Drawing Time;Reads Coverage Heavy; Reads Correct Heavy;Reads Coverage Light;Reads Correct Light;Contigs Coverage Heavy; Contigs Correct Heavy; Contigs Coverage Light; Contigs Correct Light; Link; Contigs Length\n");
            sw.Close();

            int count = 0;
            foreach (var file in Directory.GetFiles(@"generate_tests\Generated"))
            {
                if (!file.Contains("Mix"))
                {
                    count++;
                    for (int k = 5; k <= 15; k++) {
                        inputQueue.Add((k, -1, file, @"generate_tests\Results\" + Path.GetFileNameWithoutExtension(file) + $"-{k}-Default alphabet-Contigs Length.html", csvfile, "examples\\Default alphabet.csv", "", true));
                    }
                }
            }

            // Run all tasks in parallel
            Parallel.ForEach(inputQueue, (i) => worker(i));

            stopwatch.Stop();
            Console.WriteLine($"Assembled {count} files in {stopwatch.ElapsedMilliseconds} ms");
        }
        /// <summary> To run a batch of assemblies in parallel. </summary>
        static void RunGenerated()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Results\runs.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nID;Type;Test;Proteases;Percentage;Alphabet;Reads;K;Minimum Homology;Contigs;Avg Sequence length (per contig);Total sequence length;Mean Connectivity;Total runtime;Drawing Time;Reads Coverage Heavy; Reads Correct Heavy;Reads Coverage Light;Reads Correct Light;Contigs Coverage Heavy; Contigs Correct Heavy; Contigs Coverage Light; Contigs Correct Light; Link;\n");
            sw.Close();

            int count = 0;
            foreach (var file in Directory.GetFiles(@"generate_tests\Generated"))
            {
                count++;
                for (int k = 5; k <= 15; k++) {         
                    inputQueue.Add((k, k-1, file, @"generate_tests\Results\" + Path.GetFileNameWithoutExtension(file) + $"-{k}-Default alphabet.html", csvfile, "examples\\Default alphabet.csv", "", true));
                    inputQueue.Add((k, k-1, file, @"generate_tests\Results\" + Path.GetFileNameWithoutExtension(file) + $"-{k}-Common errors alphabet.html", csvfile, "examples\\Common errors alphabet.csv", "", true));
                }
            }

            // Run all tasks in parallel
            Parallel.ForEach(inputQueue, (i) => worker(i));

            stopwatch.Stop();
            Console.WriteLine($"Assembled {count} files in {stopwatch.ElapsedMilliseconds} ms");
        }
        /// <summary> The function to operate on the list of tasks to by run in parallel. </summary>
        /// <param name="workItem"> The task to perform. </param>
        static void worker((int, int, string, string, string, string, string, bool) workItem)
        {
            try
            {
                Console.WriteLine("Starting on: " + workItem.Item3);
                int min_score = workItem.Item2 < 0 ? workItem.Item1-1 : workItem.Item2;
                var assm = new Assembler(workItem.Item1, min_score, min_score, workItem.Item8);
                Alphabet.SetAlphabet(workItem.Item6);
                if (workItem.Item2 == -2) {
                    assm.GiveReadsPeaks(OpenReads.Peaks(workItem.Item3, 99, 90, FileFormat.Peaks.NewFormat(), workItem.Item1));
                }
                else
                {
                    assm.GiveReads(OpenReads.Simple(workItem.Item3));
                }
                assm.Assemble();
                var htmlreport = new HTMLReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads);
                htmlreport.Save(workItem.Item4);
                //assm.CreateReport(workItem.Item4);
                // Add the meta information to the CSV file
                var csvreport = new CSVReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads);
                csvreport.CreateCSVLine(workItem.Item3, workItem.Item5, workItem.Item7, workItem.Item6 + (workItem.Item8 ? " - Reverse" : " - No Reverse"), Path.GetFullPath(workItem.Item4), workItem.Item2 < 0);
            }
            catch (Exception e)
            {
                bool stuck = true;
                string line = $"{workItem.Item3};{workItem.Item1};{workItem.Item2};Error: {e.Message}";

                while (stuck)
                {
                    try
                    {
                        // Add the error to the CSV file
                        File.AppendAllText(workItem.Item5, line);
                        stuck = false;
                    }
                    catch
                    {
                        // try again
                    }
                }
                Console.WriteLine("ERROR: " + e.Message + "\nSTACKTRACE: " + e.StackTrace);
            }
        }
    }
}