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
            RunExperimentalBatch();
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
                assm.SetAlphabet(workItem.Item6);
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
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <summary> The reads fed into the Assembler, as opened by OpenReads. </summary>
        public List<AminoAcid[]> reads = new List<AminoAcid[]>();
        /// <summary> The meta information as delivered by PEAKS. By definition every index in this list matches 
        /// with the index in reads. When the data was not imported via PEAKS this list is null.</summary>
        public List<MetaData.Peaks> peaks_reads = null;
        /// <summary> The De Bruijn graph used by the Assembler. </summary>
        public Node[] graph;
        /// <summary> The condensed graph used to store the output of the assembly. </summary>
        public List<CondensedNode> condensed_graph;
        /// <summary> The length of the k-mers used to create the De Bruijn graph. Private member where it is stored. </summary>
        private int kmer_length;
        /// <summary> The length of the k-mers used to create the De Bruijn graph. Get and Set is public. </summary>
        /// <value> The length of the k-mers. </value>
        public int Kmer_length
        {
            get { return kmer_length; }
        }
        /// <summary> The private member to store the minimum homology value in. </summary>
        private int minimum_homology;
        /// <summary> The minimum homology value of an edge to include it in the graph. Lowering the limit 
        /// could result in a longer sequence retrieved from the algorithm but would also greatly increase
        /// the computational cost of the calculation. </summary>
        /// <value> The minimum homology before including an edge in the graph. </value>
        public int Minimum_homology
        {
            get { return minimum_homology; }
        }
        private int duplicate_threshold;
        /// <summary> To contain meta information about how the program ran to make informed decisions on 
        /// how to choose the values of variables and to aid in debugging. </summary>
        public MetaInformation meta_data;
        /// <summary> The creator, to set up the default values. Also sets the standard alphabet. </summary>
        /// <param name="kmer_length_input"> The lengths of the k-mers. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        public Assembler(int kmer_length_input, int duplicate_threshold_input, int minimum_homology_input = -1, bool should_reverse = true)
        {
            kmer_length = kmer_length_input;
            minimum_homology = minimum_homology_input < 0 ? kmer_length - 1 : minimum_homology_input;
            duplicate_threshold = duplicate_threshold_input;
            reverse = should_reverse;
            meta_data = new MetaInformation();
            meta_data.kmer_length = kmer_length;
            meta_data.minimum_homology = minimum_homology;

            Alphabet.SetAlphabet();
        }
        public void GiveReads(List<(string, MetaData.None)> reads_i)
        {
            reads = reads_i.Select(x => StringToSequence(x.Item1)).ToList();
        }
        public void GiveReadsPeaks(List<(string, MetaData.Peaks)> reads_i)
        {
            reads = reads_i.Select(x => StringToSequence(x.Item1)).ToList();
            peaks_reads = reads_i.Select(x => x.Item2).ToList();
        }
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(input[i]);
            }
            return output;
        }
        /// <summary> Assemble the reads into the graph, this is logically (one of) the last metods to 
        /// run on an Assembler, all settings should be defined before running this. </summary>
        public void Assemble()
        {
            // Start the stopwatch to be able to say how long the program ran
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Generate all k-mers
            // All k-mers of length (kmer_length)

            var kmers = new List<(AminoAcid[], int)>();
            AminoAcid[] read;

            Console.WriteLine($"Found {meta_data.reads} reads");

            for (int r = 0; r < reads.Count(); r++)
            {
                read = reads[r];
                if (read.Length > kmer_length)
                {
                    for (int i = 0; i < read.Length - kmer_length + 1; i++)
                    {
                        AminoAcid[] kmer = read.SubArray(i, kmer_length);
                        kmers.Add((kmer, r));
                        if (reverse) kmers.Add((kmer.Reverse().ToArray(), r)); //Also add the reverse
                    }
                }
                else if (read.Length == kmer_length)
                {
                    kmers.Add((read, r));
                    if (reverse) kmers.Add((read.Reverse().ToArray(), r)); //Also add the reverse
                }
            }
            meta_data.kmers = kmers.Count;
            Console.WriteLine($"Found {meta_data.kmers} kmers");

            // Building the graph
            // Generating all (k-1)-mers

            var kmin1_mers_raw = new List<(AminoAcid[], int)>();

            kmers.ForEach(kmer =>
            {
                kmin1_mers_raw.Add((kmer.Item1.SubArray(0, kmer_length - 1), kmer.Item2));
                kmin1_mers_raw.Add((kmer.Item1.SubArray(1, kmer_length - 1), kmer.Item2));
            });

            meta_data.kmin1_mers_raw = kmin1_mers_raw.Count;
            Console.WriteLine($"Found {meta_data.kmin1_mers_raw} raw (k-1)-mers");

            var kmin1_mers = new List<(AminoAcid[], List<int>)>();

            foreach (var kmin1_mer in kmin1_mers_raw)
            {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++)
                {
                    // TODO Deduplicate using alphabet
                    if (AminoAcid.ArrayHomology(kmin1_mer.Item1, kmin1_mers[i].Item1) >= duplicate_threshold)
                    {
                        kmin1_mers[i].Item2.Add(kmin1_mer.Item2); // Update origins
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) kmin1_mers.Add((kmin1_mer.Item1, new List<int>(kmin1_mer.Item2)));
            }

            meta_data.kmin1_mers = kmin1_mers.Count;
            Console.WriteLine($"Found {meta_data.kmin1_mers} (k-1)-mers");

            // Create a node for every possible (k-1)-mer

            // Implement the graph as a adjacency list (array)
            graph = new Node[kmin1_mers.Count];

            int index = 0;
            kmin1_mers.ForEach(kmin1_mer =>
            {
                graph[index] = new Node(kmin1_mer.Item1, kmin1_mer.Item2);
                index++;
            });

            meta_data.pre_time = stopWatch.ElapsedMilliseconds;

            // Connect the nodes based on the k-mers

            // Initialize the array to save computations
            int[] prefix_matches = new int[graph.Length];
            int[] suffix_matches = new int[graph.Length];
            AminoAcid[] prefix, suffix;

            kmers.ForEach(kmer =>
            {
                prefix = kmer.Item1.SubArray(0, kmer_length - 1);
                suffix = kmer.Item1.SubArray(1, kmer_length - 1);

                // Precompute the homology with every node to speed up computation
                for (int i = 0; i < graph.Length; i++)
                {
                    // Find the homology of the prefix and suffix
                    prefix_matches[i] = AminoAcid.ArrayHomology(graph[i].Sequence, prefix);
                    suffix_matches[i] = AminoAcid.ArrayHomology(graph[i].Sequence, suffix);
                }

                // Test for pre and post fixes for every node
                for (int i = 0; i < graph.Length; i++)
                {
                    if (prefix_matches[i] >= minimum_homology)
                    {
                        for (int j = 0; j < graph.Length; j++)
                        {
                            if (i != j && suffix_matches[j] >= minimum_homology)
                            {
                                graph[i].AddForwardEdge(j, prefix_matches[j], suffix_matches[j]);
                                graph[j].AddBackwardEdge(i, prefix_matches[i], suffix_matches[i]);
                            }
                        }
                    }
                }
            });

            meta_data.graph_time = stopWatch.ElapsedMilliseconds - meta_data.pre_time;

            // Create a condensed graph

            condensed_graph = new List<CondensedNode>();

            for (int i = 0; i < graph.Length; i++)
            {
                // Start at every node, if it is not visited yet try to build a path from it 
                var start_node = graph[i];

                if (!start_node.Visited)
                {
                    var forward_node = start_node;
                    var backward_node = start_node;

                    List<AminoAcid> forward_sequence = new List<AminoAcid>();
                    List<AminoAcid> backward_sequence = new List<AminoAcid>();

                    List<int> forward_nodes = new List<int>();
                    List<int> backward_nodes = new List<int>();

                    List<int> origins = new List<int>(start_node.Origins);

                    // TODO check why equal contigs could possible be in the graph twice (based on alphabet but NOT identity)

                    // Debug purposes can be deleted later
                    int forward_node_index = i;
                    int backward_node_index = i;

                    // Walk forwards until a multifurcartion in the path is found or the end is reached
                    while (forward_node.ForwardEdges.Count == 1 && forward_node.BackwardEdges.Count <= 1)
                    {
                        forward_node.Visited = true;
                        forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2)); // Last amino acid of the sequence
                        forward_node_index = forward_node.ForwardEdges[0].Item1;
                        foreach (int o in forward_node.Origins) origins.Add(o);

                        forward_node = graph[forward_node_index];

                        // To account for possible cycles 
                        if (forward_node_index == i) break;
                    }
                    forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2));
                    forward_node.Visited = true;
                    foreach (int o in forward_node.Origins) origins.Add(o);

                    if (forward_node.ForwardEdges.Count() > 0)
                    {
                        forward_nodes = (from node in forward_node.ForwardEdges select node.Item1).ToList();
                    }

                    // Walk backwards
                    while (backward_node.ForwardEdges.Count <= 1 && backward_node.BackwardEdges.Count == 1)
                    {
                        backward_node.Visited = true;
                        backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                        backward_node_index = backward_node.BackwardEdges[0].Item1;
                        foreach (int o in backward_node.Origins) origins.Add(o);

                        backward_node = graph[backward_node_index];

                        // To account for possible cycles 
                        if (backward_node_index == i) break;
                    }
                    backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                    backward_node.Visited = true;
                    foreach (int o in backward_node.Origins) origins.Add(o);

                    if (backward_node.BackwardEdges.Count() > 0)
                    {
                        backward_nodes = (from node in backward_node.BackwardEdges select node.Item1).ToList();
                    }

                    // Build the final sequence
                    backward_sequence.Reverse();
                    backward_sequence.AddRange(start_node.Sequence.SubArray(1, kmer_length - 3));
                    backward_sequence.AddRange(forward_sequence);

                    List<int> originslist = new List<int>();
                    foreach (int origin in origins)
                    {
                        if (!originslist.Contains(origin))
                        {
                            originslist.Add(origin);
                        }
                    }
                    originslist.Sort();
                    condensed_graph.Add(new CondensedNode(backward_sequence, i, forward_node_index, backward_node_index, forward_nodes, backward_nodes, originslist));
                }
            }

            // Update the condensed graph to point to elements in the condensed graph instead of to elements in the de Bruijn graph
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];
                List<int> forward = new List<int>(node.ForwardEdges);
                node.ForwardEdges.Clear();
                foreach (var FWE in forward)
                {
                    foreach (var BWE in graph[FWE].BackwardEdges)
                    {
                        for (int node2 = 0; node2 < condensed_graph.Count(); node2++)
                        {
                            if (BWE.Item1 == condensed_graph[node2].BackwardIndex)
                            {
                                bool inlist = false;
                                foreach (var e in node.ForwardEdges)
                                {
                                    if (e == node2)
                                    {
                                        inlist = true;
                                        break;
                                    }
                                }
                                if (!inlist) node.ForwardEdges.Add(node2);
                            }
                        }
                    }
                }
                List<int> backward = new List<int>(node.BackwardEdges);
                node.BackwardEdges.Clear();
                foreach (var BWE in backward)
                {
                    foreach (var FWE in graph[BWE].ForwardEdges)
                    {
                        for (int node2 = 0; node2 < condensed_graph.Count(); node2++)
                        {
                            if (FWE.Item1 == condensed_graph[node2].ForwardIndex)
                            {
                                bool inlist = false;
                                foreach (var e in node.BackwardEdges)
                                {
                                    if (e == node2)
                                    {
                                        inlist = true;
                                        break;
                                    }
                                }
                                if (!inlist) node.BackwardEdges.Add(node2);
                            }
                        }
                    }
                }
                if (!node.BackwardEdges.Contains(node_index) && !node.ForwardEdges.Contains(node_index))
                {
                    if (node.BackwardEdges.Count() == 1)
                    {
                        //TODO commented this out because test 5 gave empty sequences, need to find that cause
                        node.Prefix = node.Sequence.Take(kmer_length - 1).ToList();
                        node.Sequence = node.Sequence.Skip(kmer_length - 1).ToList();
                    }
                    if (node.ForwardEdges.Count() == 1)
                    {
                        //TODO commented this out because test 5 gave empty sequences, need to find that cause
                        node.Suffix = node.Sequence.Skip(node.Sequence.Count() - kmer_length + 1).ToList();
                        node.Sequence = node.Sequence.Take(node.Sequence.Count() - kmer_length + 1).ToList();
                    }
                }
            }

            meta_data.path_time = stopWatch.ElapsedMilliseconds - meta_data.graph_time - meta_data.pre_time;

            stopWatch.Stop();
            meta_data.sequence_filter_time = stopWatch.ElapsedMilliseconds - meta_data.path_time - meta_data.graph_time - meta_data.pre_time;
            meta_data.sequences = condensed_graph.Count();
            meta_data.total_time = stopWatch.ElapsedMilliseconds;
        }
    }
}