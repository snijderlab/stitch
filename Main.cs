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
using static AssemblyNameSpace.OpenReads;

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
            //var assm = new Assembler(8, 7);
            //assm.GiveReadsPeaks(OpenReads.Peaks(@"C:\Users\Douwe\Downloads\de novo peptides.csv", 90, 99, FileFormat.Peaks.OldFormat(), 8));
            //assm.Assemble();
            //assm.CreateReport();
            //assm.SetAlphabet("Fusion alphabet.csv");
            //assm.OpenReads(@"generate_tests\Generated\reads-mix-perfect.txt");
            //assm.OpenReads(@"generate_tests\Generated\reads-IgG1-K-001-Trypsin,Chymotrypsin,Alfalytic protease-100,00.txt");
            //assm.OpenReadsPeaks(@"Z:\users\5803969\Benchmarking\190520_MAb-denovo_F1_HCD_Herc_001_DENOVO_7\de novo peptides.csv", 99, 95);
            /*assm.OpenReadsPeaks(new List<(string, string)>{
                (@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\190528_F1_131-2a_ETHCD_01_DENOVO_7\de novo peptides.csv", "ETHCD"),
                (@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\190528_F1_131-2a_HCD_01_DENOVO_7\de novo peptides.csv", "HCD")
                }, 99, 90, Assembler.PeaksFormat.NewFormat());*/
            //for (int i = 0; i < assm.reads.Count(); i++) {
            //    Console.WriteLine(assm.reads[i]);
            //    Console.WriteLine(assm.peaks_reads[i].ToHTML());
            //}   
            //assm.Assemble();
            //assm.CreateReport("report.html");
            //Console.WriteLine($"Percentage coverage: {HelperFunctionality.MultipleSequenceAlignmentToTemplate("QVQLVESGGGVVQPGRSLRLSCAASGFSFSNYGMHWVRQAPGKGLEWVALIWYDGSNEDYTDSVKGRFTISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVFPLAPSSKSTSGGTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSSLGTQTYICNVNHKPSNTKVDKRVEPKSCDKTHTCPPCPAPELLGGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSHEDPEVKFNWYVDGVEVHNAKTKPREEQYNSTYRVVSVLTVLHQDWLNGKEYKCKVSNKALPAPIEKTISKAKGQPREPQVYTLPPSREEMTKNQVSLTCLVKGFYPSDIAVEWESNGQPENNYKTTPPVLDSDGSFFLYSKLTVDKSRWQQGNVFSCSVMHEALHNHYTQKSLSLSPGK", assm.reads.Select(x => Assembler.AminoAcid.ArrayToString(x)).ToArray())}");
            //RunGenerated();
            //RunContigsLengthBatch();
            RunExperimentalBatch();
        }
         static void RunExperimentalBatch() {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Experimental results\runs.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nAntibody;Fragmentation method;K;Minimal Homology;Contigs;Avg sequence length;Total Sequence length;mean Connectivity;Total runtime;Drawing time;Contigs Coverage Heavy;Contigs Coverage Light;Contigs Coverage Depth Heavy;Contigs Coverage Depth Light;Total Mapped Contigs;Correct Contigs;Reads Coverage Heavy;Reads Coverage Light;Reads Coverage Depth Heavy;Reads Coverage Depth Light;Correct Contigs Coverage Heavy;Correct Contigs Coverage Light;Correct Contigs Coverage Depth Heavy;Correct Contigs Coverage Depth Light;Contigs Length\n");
            sw.Close();

            int count = 0;
            foreach (var dir in Directory.GetDirectories(@"Z:\users\5803969\Benchmarking\190529_MAbs-denovo_PEAKS_export\")) {
                var file = dir + @"\de novo peptides.csv";
                string sequencefile = null;
                if (file.Contains("1446")) {
                    sequencefile = @"generate_tests\Experimental results\1446.txt";
                }
                if (file.Contains("Her")) {
                    sequencefile = @"generate_tests\Experimental results\her.txt";
                }
                if (file.Contains("CAMP")) {
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
        static void RunContigsLengthBatch() {
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
        static void RunDepthCoverageBatch() {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string csvfile = @"generate_tests\Results\runs-contigs-length-batch.csv";

            // Write the correct header to the CSV file
            StreamWriter sw = File.CreateText(csvfile);
            sw.Write("sep=;\nID;Type;Test;Proteases;Percentage;Alphabet;Reads;K;Minimum Homology;Contigs;Avg Sequence length (per contig);Total sequence length;Mean Connectivity;Total runtime;Drawing Time;Reads Coverage Heavy; Reads Correct Heavy;Reads Coverage Light;Reads Correct Light;Contigs Coverage Heavy; Contigs Correct Heavy; Contigs Coverage Light; Contigs Correct Light; Link; Contigs Length\n");
            sw.Close();

            int count = 0;
            foreach (var file in Directory.GetFiles(@"generate_tests\Generated")) {
                if (!file.Contains("Mix")) {
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
            foreach (var file in Directory.GetFiles(@"generate_tests\Generated")) {
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
            try {
                Console.WriteLine("Starting on: " + workItem.Item3);
                int min_score = workItem.Item2 < 0 ? workItem.Item1-1 : workItem.Item2;
                var assm = new Assembler(workItem.Item1, min_score, min_score, workItem.Item8);
                assm.SetAlphabet(workItem.Item6);
                if (workItem.Item2 == -2) {
                    assm.GiveReadsPeaks(OpenReads.Peaks(workItem.Item3, 99, 90, FileFormat.Peaks.NewFormat(), workItem.Item1));
                } else {
                    assm.GiveReads(OpenReads.Simple(workItem.Item3));
                }
                assm.Assemble();
                assm.CreateReport(workItem.Item4);
                // Add the meta information to the CSV file
                assm.CreateCSVLine(workItem.Item3, workItem.Item5, workItem.Item7, workItem.Item6 + (workItem.Item8 ? " - Reverse" : " - No Reverse"), Path.GetFullPath(workItem.Item4), workItem.Item2 < 0);
            } catch (Exception e) {
                bool stuck = true;
                string line = $"{workItem.Item3};{workItem.Item1};{workItem.Item2};Error: {e.Message}";

                while (stuck) {
                    try {
                        // Add the error to the CSV file
                        File.AppendAllText(workItem.Item5, line);
                        stuck = false;
                    } catch {
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
        /// <summary> A counter to give all generated graphs a unique filename. Invaluable in batch processing. </summary>
        static int counter = 0;
        /// <summary> The reads fed into the Assembler, as opened by OpenReads. </summary>
        public List<AminoAcid[]> reads = new List<AminoAcid[]>();
        /// <summary> The meta information as delivered by PEAKS. By definition every index in this list matches 
        /// with the index in reads. When the data was not imported via PEAKS this list is null.</summary>
        public List<MetaData.Peaks> peaks_reads = null;
        /// <summary> The De Bruijn graph used by the Assembler. </summary>
        Node[] graph;
        /// <summary> The condensed graph used to store the output of the assembly. </summary>
        List<CondensedNode> condensed_graph;
        /// <summary> The length of the k-mers used to create the De Bruijn graph. Private member where it is stored. </summary>
        private int kmer_length;
        /// <summary> The length of the k-mers used to create the De Bruijn graph. Get and Set is public. </summary>
        /// <value> The length of the k-mers. </value>
        public int Kmer_length
        {
            get { return kmer_length; }
        }
        /// <summary> The matrix used for scoring of the alignment between two characters in the alphabet. 
        /// As such this matrix is rectangular. </summary>
        private int[,] scoring_matrix;
        /// <summary> The alphabet used for alignment. The default value is all the amino acids in order of
        /// natural abundance in prokaryotes to make finding the right amino acid a little bit faster. </summary>
        private char[] alphabet;
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
        private MetaInformation meta_data;
        /// <summary>
        /// To determine if reads should also be added in reverse.
        /// </summary>
        private bool reverse;
        /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can 
        /// be used in an efficient way </summary>
        public struct AminoAcid
        {
            /// <summary> The Assembler used to create the AminoAcd, used to get the information of the alphabet. </summary>
            private Assembler parent;
            /// <summary> The code (index of the char in the alpabet array of the parent).
            /// The only way to change it is in the creator. </summary>
            /// <value> The code of this AminoAcid. </value>
            public int Code;

            /// <summary> The creator of AminoAcids. </summary>
            /// <param name="asm"> The Assembler that this AminoAcid is used in, to get the alphabet. </param>
            /// <param name="input"> The character to store in this AminoAcid. </param>
            /// <returns> Returns a reference to the new AminoAcid. </returns>
            public AminoAcid(Assembler asm, char input)
            {
                parent = asm;
                Code = asm.getIndexInAlphabet(input);
            }
            /// <summary> Will create a string of this AminoAcid. Consiting of the character used to 
            /// create this AminoAcid. </summary>
            /// <returns> Returns the character of this AminoAcid (based on the alphabet) as a string. </returns>
            public override string ToString()
            {
                return parent.alphabet[Code].ToString();
            }
            /// <summary> Will create a string of an array of AminoAcids. </summary>
            /// <param name="array"> The array to create a string from. </param>
            /// <returns> Returns the string of the array. </returns>
            public static string ArrayToString(AminoAcid[] array)
            {
                var builder = new StringBuilder();
                foreach (AminoAcid aa in array)
                {
                    builder.Append(aa.ToString());
                }
                return builder.ToString();
            }
            /// <summary> To check for equality of the AminoAcids. Will return false if the object is not an AminoAcid. </summary>
            /// <remarks> Implemented as the equals operator (==). </remarks>
            /// <param name="obj"> The object to check equality with. </param>
            /// <returns> Returns true when the Amino Acids are equal. </returns>
            public override bool Equals(object obj)
            {
                return obj is AminoAcid && this == (AminoAcid)obj;
            }
            /// <summary> To check for equality of arrays of AminoAcids. </summary>
            /// <remarks> Implemented as a short circuiting loop with the equals operator (==). </remarks>
            /// <param name="left"> The first object to check equality with. </param>
            /// <param name="right"> The second object to check equality with. </param>
            /// <returns> Returns true when the aminoacid arrays are equal. </returns>
            public static bool ArrayEquals(AminoAcid[] left, AminoAcid[] right)
            {
                if (left.Length != right.Length)
                    return false;
                for (int i = 0; i < left.Length; i++)
                {
                    if (left[i] != right[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            /// <summary> To check for equality of AminoAcids. </summary>
            /// <remarks> Implemented as a check for equality of the Code of both AminoAcids. </remarks>
            /// <param name="x"> The first AminoAcid to test. </param>
            /// <param name="y"> The second AminoAcid to test. </param>
            /// <returns> Returns true when the Amino Acids are equal. </returns>
            public static bool operator ==(AminoAcid x, AminoAcid y)
            {
                return x.Code == y.Code;
            }
            /// <summary> To check for inequality of AminoAcids. </summary>
            /// <remarks> Implemented as a a reverse of the equals operator. </remarks>
            /// <param name="x"> The first AminoAcid to test. </param>
            /// <param name="y"> The second AminoAcid to test. </param>
            /// <returns> Returns false when the Amino Acids are equal. </returns>
            public static bool operator !=(AminoAcid x, AminoAcid y)
            {
                return !(x == y);
            }
            /// <summary> To get a hashcode for this AminoAcid. </summary>
            /// <returns> Returns the hascode of the AminoAcid. </returns>
            public override int GetHashCode()
            {
                return this.Code.GetHashCode();
            }
            /// <summary> Calculating homology, using the scoring matrix of the parent Assembler. 
            /// See <see cref="Assembler.scoring_matrix"/> for the scoring matrix.
            /// See <see cref="Assembler.SetAlphabet"/> on how to change the scoring matrix.</summary>
            /// <remarks> Depending on which rules are put into the scoring matrix the order in which this 
            /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to 
            /// <c>b.Homology(a)</c>. </remarks>
            /// <param name="right"> The other AminoAcid to use. </param>
            /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
            public int Homology(AminoAcid right)
            {
                try {
                    return parent.scoring_matrix[this.Code, right.Code];
                } catch (Exception e) {
                    Console.WriteLine($"Got an error for this code {this.Code} and that code {right.Code}");
                    throw e;
                }
            }
            /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix 
            /// of the parent Assembler. </summary>
            /// <remarks> Two arrays of different length will result in a value of 0. This function loops
            /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
            /// <param name="left"> The first object to calculate homology with. </param>
            /// <param name="right"> The second object to calculate homology with. </param>
            /// <returns> Returns the homology bewteen the two aminoacid arrays. </returns>
            public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right)
            {
                int score = 0;
                if (left.Length != right.Length)
                    // Throw exception?
                    return 0;
                for (int i = 0; i < left.Length; i++)
                {
                    score += left[i].Homology(right[i]);
                }
                return score;
            }
        }
        /// <summary> Nodes in the graph with a sequence length of K-1. </summary>
        private class Node
        {
            /// <summary> The member to store the sequence information in. </summary>
            private AminoAcid[] sequence;

            /// <summary> The sequence of the Node. Only has a getter. </summary>
            /// <value> The sequence of this node. </value>
            public AminoAcid[] Sequence { get { return sequence; } }

            /// <summary> Where the (k-1)-mer sequence comes from. </summary>
            private List<int> origins;

            /// <summary> The indexes of the reads where this (k-1)-mere originated from. </summary>
            /// <value> A list of indexes of the list of reads. </value>
            public List<int> Origins { get { return origins; } }

            /// <summary> The list of edges from this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. The private 
            /// member to store the list. </summary>
            private List<ValueTuple<int, int, int>> forwardEdges;

            /// <summary> The list of edges going from this node. </summary>
            /// <value> The list of edges from this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. Only has a getter. </value>
            public List<ValueTuple<int, int, int>> ForwardEdges { get { return forwardEdges; } }

            /// <summary> The list of edges to this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. The private 
            /// member to store the list. </summary>
            private List<ValueTuple<int, int, int>> backwardEdges;

            /// <summary> The list of edges going to this node. </summary>
            /// <value> The list of edges to this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. Only has a getter. </value>
            public List<ValueTuple<int, int, int>> BackwardEdges { get { return backwardEdges; } }

            /// <summary> Whether or not this node is visited yet. </summary>
            public bool Visited;

            /// <summary> The creator of Nodes. </summary>
            /// <param name="seq"> The sequence of this Node. </param>
            /// <param name="origin"> The origin(s) of this (k-1)-mer. </param>
            /// <remarks> It will initialize the edges list. </remarks>
            public Node(AminoAcid[] seq, List<int> origin)
            {
                sequence = seq;
                origins = origin;
                forwardEdges = new List<ValueTuple<int, int, int>>();
                backwardEdges = new List<ValueTuple<int, int, int>>();
                Visited = false;
            }

            /// <summary> To add a forward edge to the Node. Wil only be added if the score is high enough. </summary>
            /// <param name="target"> The index of the Node where this edge goes to. </param>
            /// <param name="score1"> The homology of the edge with the first Node. </param>
            /// <param name="score2"> The homology of the edge with the second Node. </param>
            public void AddForwardEdge(int target, int score1, int score2)
            {
                bool inlist = false;
                foreach (var edge in forwardEdges) {
                    if (edge.Item1 == target) {
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) forwardEdges.Add((target, score1, score2));
                return;
            }
            /// <summary> To add a backward edge to the Node. </summary>
            /// <param name="target"> The index of the Node where this edge comes from. </param>
            /// <param name="score1"> The homology of the edge with the first Node. </param>
            /// <param name="score2"> The homology of the edge with the second Node. </param>
            public void AddBackwardEdge(int target, int score1, int score2)
            {
                bool inlist = false;
                foreach (var edge in backwardEdges) {
                    if (edge.Item1 == target) {
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) backwardEdges.Add((target, score1, score2));
                return;
            }
            /// <summary> To check if the Node has forward edges. </summary>
            /// <returns> Returns true if the node has forward edges. </returns>
            public bool HasForwardEdges()
            {
                return forwardEdges.Count > 0;
            }
            /// <summary> To check if the Node has backward edges. </summary>
            /// <returns> Returns true if the node has backward edges. </returns>
            public bool HasBackwardEdges()
            {
                return backwardEdges.Count > 0;
            }
            /// <summary> To get the amount of edges (forward and backward). </summary>
            /// <remarks> O(1) runtime </remarks>
            /// <returns> The amount of edges (forwards and backwards). </returns>
            public int EdgesCount()
            {
                return forwardEdges.Count + backwardEdges.Count;
            }
        }
        /// <summary> A struct to hold meta information about the assembly to keep it organised 
        /// and to report back to the user. </summary>
        private struct MetaInformation
        {
            /// <summary> The total time needed to run Assemble(). See <see cref="Assembler.Assemble"/></summary>
            public long total_time;
            /// <summary> The needed to do the pre work, creating k-mers and (k-1)-mers. See <see cref="Assembler.Assemble"/></summary>
            public long pre_time;
            /// <summary> The time needed the build the graph. See <see cref="Assembler.Assemble"/></summary>
            public long graph_time;
            /// <summary> The time needed to find the path through the de Bruijn graph. See <see cref="Assembler.Assemble"/></summary>
            public long path_time;
            /// <summary> The time needed to filter the sequences. See <see cref="Assembler.Assemble"/></summary>
            public long sequence_filter_time;
             /// <summary> The time needed to draw the graphs. See <see cref="Assembler.OutputGraph"/></summary>
            public long drawingtime;
            /// <summary> The amount of reads used by the program. See <see cref="Assembler.Assemble"/>. See <see cref="Assembler.OpenReads"/></summary>
            public int reads;
            /// <summary> The amount of k-mers generated. See <see cref="Assembler.Assemble"/></summary>
            public int kmers;
            /// <summary> The amount of (k-1)-mers generated. See <see cref="Assembler.Assemble"/></summary>
            public int kmin1_mers;
            /// <summary> The amount of (k-1)-mers generated, before removing all duplicates. See <see cref="Assembler.Assemble"/></summary>
            public int kmin1_mers_raw;
            /// <summary> The number of sequences found. See <see cref="Assembler.Assemble"/></summary>
            public int sequences;
        }
        /// <summary> Nodes in the condensed graph with a variable sequence length. </summary>
        private class CondensedNode
        {
            /// <summary> The index this node. The index is defined as the index of the startnode in the adjecency list of the de Bruijn graph. </summary>
            public int Index;
            /// <summary> The index of the last node (going from back to forth). To buid the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
            public int ForwardIndex;
            /// <summary> The index of the first node (going from back to forth). To buid the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
            public int BackwardIndex;
            /// <summary> Whether or not this node is visited yet. </summary>
            public bool Visited;
            /// <summary> The sequence of this node. It is the longest constant sequence to be 
            /// found in the de Bruijn graph starting at the Index. See <see cref="CondensedNode.Index"/></summary>
            public List<AminoAcid> Sequence;
            public List<AminoAcid> Prefix;
            public List<AminoAcid> Suffix;
            /// <summary> The list of forward edges, defined as the indexes in the de Bruijn graph. </summary>
            public List<int> ForwardEdges;
            /// <summary> The list of backward edges, defined as the indexes in the de Bruijn graph. </summary>
            public List<int> BackwardEdges;
            /// <summary> The origins where the (k-1)-mers used for this sequence come from. Defined as the index in the list with reads. </summary>
            public List<int> Origins;
            /// <summary> Creates a condensed node to be used in the condensed graph. </summary>
            /// <param name="sequence"> The sequence of this node. See <see cref="CondensedNode.Sequence"/></param>
            /// <param name="index"> The index of the node, the index in the de Bruijn graph. See <see cref="CondensedNode.Index"/></param>
            /// <param name="forward_index"> The index of the last node of the sequence (going from back to forth). See <see cref="CondensedNode.ForwardIndex"/></param>
            /// <param name="backward_index"> The index of the first node of the sequence (going from back to forth). See <see cref="CondensedNode.BackwardIndex"/></param>
            /// <param name="forward_edges"> The forward edges from this node (indexes). See <see cref="CondensedNode.ForwardEdges"/></param>
            /// <param name="backward_edges"> The backward edges from this node (indexes). See <see cref="CondensedNode.BackwardEdges"/></param>
            /// <param name="origins"> The origins where the (k-1)-mers used for this sequence come from. See <see cref="CondensedNode.Origins"/></param>
            public CondensedNode(List<AminoAcid> sequence, int index, int forward_index, int backward_index, List<int> forward_edges, List<int> backward_edges, List<int> origins)
            {
                Sequence = sequence;
                Index = index;
                ForwardIndex = forward_index;
                BackwardIndex = backward_index;
                ForwardEdges = forward_edges;
                BackwardEdges = backward_edges;
                Origins = origins;
                Visited = false;
            }
        }

        /// <summary> The creator, to set up the default values. Also sets the standard alphabet. </summary>
        /// <param name="kmer_length_input"> The lengths of the k-mers. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        public Assembler(int kmer_length_input, int duplicate_threshold_input, int minimum_homology_input = -1, bool should_reverse = true)
        {
            kmer_length = kmer_length_input;
            minimum_homology = minimum_homology_input < 0 ? kmer_length - 1 : minimum_homology_input;
            duplicate_threshold = duplicate_threshold_input;
            reverse = should_reverse;

            SetAlphabet();
        }
        /// <summary> Find the index of the given character in the alphabet. </summary>
        /// <param name="c"> The character to look op. </param>
        /// <returns> The index of the character in the alphabet or -1 if it is not in the alphabet. </returns>
        private int getIndexInAlphabet(char c)
        {
            for (int i = 0; i < alphabet.Length; i++)
            {
                if (c == alphabet[i])
                {
                    return i;
                }
            }
            Console.WriteLine($"Could not find '{c}' in the alphabet: '{alphabet}'");
            return -1;
        }

        /// <summary> Set the alphabet of the assembler. </summary>
        /// <param name="rules"> A list of rules implemented as tuples containing the chars to connect, 
        /// the value to put into the matrix and whether or not the rule should be bidirectional (the value
        ///  in the matrix is the same both ways). </param>
        /// <param name="diagonals_value"> The value to place on the diagonals of the matrix. </param>
        /// <param name="input"> The alphabet to use, it will be iterated over from the front to the back so
        /// the best case scenario has the most used characters at the front of the string. </param>
        public void SetAlphabet(List<ValueTuple<char, char, int, bool>> rules = null, int diagonals_value = 1, string input = "LSAEGVRKTPDINQFYHMCWOU")
        {
            alphabet = input.ToCharArray();

            scoring_matrix = new int[alphabet.Length, alphabet.Length];

            // Only set the diagonals to te given value
            for (int i = 0; i < alphabet.Length; i++) scoring_matrix[i, i] = diagonals_value;

            // Use the rules to 
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    scoring_matrix[getIndexInAlphabet(rule.Item1), getIndexInAlphabet(rule.Item2)] = rule.Item3;
                    if (rule.Item4) scoring_matrix[getIndexInAlphabet(rule.Item2), getIndexInAlphabet(rule.Item1)] = rule.Item3;
                }
            }
        }
        /// <summary> Set the alphabet based on a CSV file. </summary>
        /// <param name="filename"> Name of the file. </param>
        public void SetAlphabet(string filename) {
            string[] input = new string[]{};
            try {
                input = File.ReadAllLines(filename);
            } catch {
                throw new Exception($"Could not open the file: {filename}");
            }
            int rows = input.Length; 
            List<string[]> array = new List<string[]>();

            foreach (string line in input) {
                array.Add(line.Split(new char[]{';',','}).Select(x => x.Trim(new char[]{' ','\n','\r','\t','-','.'})).ToArray());
            }

            int columns = array[0].Length;

            if (rows != columns) {
                throw new Exception($"The amount of rows ({rows}) is not equal to the amount of columns ({columns}).");
            } else {
                alphabet = String.Join("", array[0].SubArray(1, columns-1)).ToCharArray();
                scoring_matrix = new int[columns - 1, columns - 1];
                bool succesful = true;

                for (int i = 0; i < columns - 1; i++) {
                    for (int j = 0; j < columns - 1; j++) {
                        try {
                            scoring_matrix[i, j] = Int32.Parse(array[i+1][j+1]);
                        } catch {
                            succesful = false;
                        }
                    }
                }

                if (!succesful) {
                    throw new Exception("The reading on the alphabet file was not succesfull, see stdout for detailed error messages.");
                }
            }
        }
        public void GiveReads(List<string> reads_i) {
            reads = reads_i.Select(x => StringToSequence(x)).ToList();
        }
        public void GiveReadsPeaks(List<(string, MetaData.Peaks)> reads_i) {
            reads = reads_i.Select(x => StringToSequence(x.Item1)).ToList();
            peaks_reads = reads_i.Select(x => x.Item2).ToList();
        }
        AminoAcid[] StringToSequence(string input) {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++) {
                output[i] = new AminoAcid(this, input[i]);
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

            foreach (var kmin1_mer in kmin1_mers_raw) {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++) {
                    // TODO Deduplicate using alphabet
                    if (AminoAcid.ArrayHomology(kmin1_mer.Item1, kmin1_mers[i].Item1) >= duplicate_threshold) {
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

                    if (forward_node.ForwardEdges.Count() > 0) {
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

                    if (backward_node.BackwardEdges.Count() > 0) {
                        backward_nodes = (from node in backward_node.BackwardEdges select node.Item1).ToList();
                    }  

                    // Build the final sequence
                    backward_sequence.Reverse();
                    backward_sequence.AddRange(start_node.Sequence.SubArray(1, kmer_length - 3));
                    backward_sequence.AddRange(forward_sequence);

                    List<int> originslist = new List<int>();
                    foreach (int origin in origins) {
                        if (!originslist.Contains(origin)) {
                            originslist.Add(origin);
                        }
                    }
                    originslist.Sort();
                    condensed_graph.Add(new CondensedNode(backward_sequence, i, forward_node_index, backward_node_index, forward_nodes, backward_nodes, originslist));
                }
            }

            // Update the condensed graph to point to elements in the condensed graph instead of to elements in the de Bruijn graph
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++) {
                var node = condensed_graph[node_index];
                List<int> forward = new List<int>(node.ForwardEdges);
                node.ForwardEdges.Clear();
                foreach (var FWE in forward) {
                    foreach (var BWE in graph[FWE].BackwardEdges) {
                        for (int node2 = 0; node2 < condensed_graph.Count(); node2++) {
                            if (BWE.Item1 == condensed_graph[node2].BackwardIndex) {
                                bool inlist = false;
                                foreach (var e in node.ForwardEdges) {
                                    if (e == node2) {
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
                foreach (var BWE in backward) {
                    foreach (var FWE in graph[BWE].ForwardEdges) {
                        for (int node2 = 0; node2 < condensed_graph.Count(); node2++) {
                            if (FWE.Item1 == condensed_graph[node2].ForwardIndex) {
                                bool inlist = false;
                                foreach (var e in node.BackwardEdges) {
                                    if (e == node2) {
                                        inlist = true;
                                        break;
                                    }
                                }
                                if (!inlist) node.BackwardEdges.Add(node2);
                            }
                        }
                    }
                }
                if (!node.BackwardEdges.Contains(node_index) && !node.ForwardEdges.Contains(node_index)) {
                    if (node.BackwardEdges.Count() == 1) {
                        //TODO commented this out because test 5 gave empty sequences, need to find that cause
                        node.Prefix = node.Sequence.Take(kmer_length - 1).ToList();
                        node.Sequence = node.Sequence.Skip(kmer_length - 1).ToList();
                    }
                    if (node.ForwardEdges.Count() == 1) {
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
        /// <summary> Creates a dot file and uses it in graphviz to generate a nice plot. Generates an extended and a simple variant. </summary>
        /// <param name="filename"> The file to output to. </param>
        public void OutputGraph(string filename = "graph.dot") {
            // Generate a dot file to use in graphviz
            var buffer = new StringBuilder();
            var simplebuffer = new StringBuilder();

            string header = "digraph {\n\tnode [fontname=\"Roboto\", shape=cds, fontcolor=\"blue\", color=\"blue\"];\n\tgraph [rankdir=\"LR\"];\n\t edge [arrowhead=vee, color=\"blue\"];\n";
            buffer.AppendLine(header);
            simplebuffer.AppendLine(header);
            string style;

            for (int i = 0; i < condensed_graph.Count(); i++) {
                if (condensed_graph[i].BackwardEdges.Count() == 0) style = ", style=filled, fillcolor=\"blue\", fontcolor=\"white\"";
                else style = "";

                buffer.AppendLine($"\ti{i} [label=\"{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}\"{style}]");
                simplebuffer.AppendLine($"\ti{i} [label=\"I{i:D4}\"{style}]");

                foreach (var fwe in condensed_graph[i].ForwardEdges) {
                    buffer.AppendLine($"\ti{i} -> i{fwe}");
                    simplebuffer.AppendLine($"\ti{i} -> i{fwe}");
                }
            }

            buffer.AppendLine("}");
            simplebuffer.AppendLine("}");

            // Write .dot to a file

            string path = Path.ChangeExtension(filename, "");
            string simplefilename = new string(path.Take(path.Length -1).ToArray()) + "-simple.dot";
            
            StreamWriter sw = File.CreateText(filename);
            StreamWriter swsimple = File.CreateText(simplefilename);

            sw.Write(buffer.ToString());
            swsimple.Write(simplebuffer.ToString());

            sw.Close();
            swsimple.Close();

            // Generate SVG files of the graph
            try
            {
                Process svg = new Process();
                svg.StartInfo = new ProcessStartInfo("dot", "-Tsvg \"" + Path.GetFullPath(filename) + "\" -o \"" + Path.ChangeExtension(Path.GetFullPath(filename), "svg") + "\"" );
                svg.StartInfo.RedirectStandardError = true;
                svg.StartInfo.UseShellExecute = false;

                Process simplesvg = new Process();
                simplesvg.StartInfo = new ProcessStartInfo("dot", "-Tsvg \"" + Path.GetFullPath(simplefilename) + "\" -o \"" + Path.ChangeExtension(Path.GetFullPath(simplefilename), "svg") + "\"" );
                simplesvg.StartInfo.RedirectStandardError = true;
                simplesvg.StartInfo.UseShellExecute = false;
                
                svg.Start();
                simplesvg.Start();

                svg.WaitForExit();
                simplesvg.WaitForExit();

                var svgstderr = svg.StandardError.ReadToEnd();
                if (svgstderr != "") {
                    Console.WriteLine("EXTENDED SVG ERROR: " + svgstderr);
                }
                var simplesvgstderr = simplesvg.StandardError.ReadToEnd();
                if (simplesvgstderr != "") {
                    Console.WriteLine("SIMPLE SVG ERROR: " + simplesvgstderr);
                }
                svg.Kill();
                simplesvg.Kill();
            }
            catch (Exception e)
            {
                //Console.WriteLine("Generic Expection when trying call dot to build graph: " + e.Message);
            }
        }
        /// <summary> Create HTML with all reads in a table. With annotations for sorting the table. </summary>
        /// <returns> Returns an HTML string. </returns>
        public string CreateReadsTable() 
        {
             var buffer = new StringBuilder();

            buffer.AppendLine(@"<table id=""reads-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('reads-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('reads-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('reads-table', 2, 'number')"" class=""smallcell"">Sequence Length</th>
</tr>");
            string id;

            for (int i = 0; i < reads.Count(); i++) {
                id = GetReadLink(i);
                buffer.AppendLine($@"<tr id=""reads-table-r{i}"">
    <td class=""center"">{id}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(reads[i])}</td>
    <td class=""center"">{AminoAcid.ArrayToString(reads[i]).Count()}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        /// <summary> Returns a table containing all the contigs of a alignment. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        public string CreateContigsTable()
        {
            var buffer = new StringBuilder();

            buffer.AppendLine(@"<table id=""contigs-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('contigs-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('contigs-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('contigs-table', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('contigs-table', 3, 'string')"" class=""smallcell"">Forks to</th>
    <th onclick=""sortTable('contigs-table', 4, 'string')"" class=""smallcell"">Forks from</th>
    <th onclick=""sortTable('contigs-table', 5, 'string')"">Based on</th>
</tr>");
            string id;

            for (int i = 0; i < condensed_graph.Count(); i++) {
                id = GetCondensedNodeLink(i);
                buffer.AppendLine($@"<tr id=""table-i{i}"">
    <td class=""center"">{id}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}</td>
    <td class=""center"">{condensed_graph[i].Sequence.Count()}</td>
    <td class=""center"">{condensed_graph[i].ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
    <td class=""center"">{condensed_graph[i].BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
    <td>{condensed_graph[i].Origins.Aggregate<int, string>("", (a, b) => a + " " + GetReadLink(b))}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        public string CreateAsides()
        {
            var buffer = new StringBuilder();
            string id;

            for (int i = 0; i < condensed_graph.Count(); i++) {
                id = $"I{i:D4}";
                string prefix = "";
                if (condensed_graph[i].Prefix != null) prefix = AminoAcid.ArrayToString(condensed_graph[i].Prefix.ToArray());
                string suffix = "";
                if (condensed_graph[i].Suffix != null) suffix = AminoAcid.ArrayToString(condensed_graph[i].Suffix.ToArray());

                buffer.AppendLine($@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig: {id}</h1>
    <h2>Sequence (length={condensed_graph[i].Sequence.Count()})</h2>
    <p class=""aside-seq""><span class='prefix'>{prefix}</span>{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}<span class='suffix'>{suffix}</span></p>
    <h2>Reads Alignment</h4>
    {CreateReadsAlignment(condensed_graph[i])}
    <h2>Based on</h2>
    <p>{condensed_graph[i].Origins.Aggregate<int, string>("", (a, b) => a + " " + GetReadLink(b))}</p>
</div>");
            }
            for (int i = 0; i < reads.Count(); i++) {
                id = $"R{i:D4}";
                string meta = peaks_reads == null ? "" : peaks_reads[i].ToHTML();
                buffer.AppendLine($@"<div id=""{id}"" class=""info-block read-info"">
    <h1>Read: {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(reads[i])}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(reads[i]).Count()}</p>
    {meta}
</div>");
            }

            return buffer.ToString();
        }
        /// <summary> Create a reads alignment to display in the sidebar. </summary>
        /// <returns> Returns an HTML string. </returns>
        private string CreateReadsAlignment(CondensedNode node) {
            string sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            List<(string, int)> reads_array = node.Origins.Select(x => (AminoAcid.ArrayToString(reads[x]), x)).ToList();
            var positions = new Queue<(string, int, int, int)>(HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, reads_array, alphabet, scoring_matrix, true));

            // Find a bit more efficient packing of reads on the sequence
            var placed = new List<List<(string, int, int, int)>>();
            while (positions.Count() > 0) {
                var current = positions.Dequeue();
                bool fit = false;
                for (int i = 0; i < placed.Count() && !fit; i++) {
                    // Find if it fits in this row
                    bool clashes = false;
                    for (int j = 0; j < placed[i].Count() && !clashes; j++) {
                        if ((current.Item2 + 1 > placed[i][j].Item2 && current.Item2 - 1 < placed[i][j].Item3) 
                         || (current.Item3 + 1 > placed[i][j].Item2 && current.Item3 - 1 < placed[i][j].Item3) 
                         || (current.Item2 - 1 < placed[i][j].Item2 && current.Item3 + 1 > placed[i][j].Item3)) {
                             clashes = true;
                        }
                    }
                    if (!clashes) {
                        placed[i].Add(current);
                        fit = true;
                    }
                }
                if (!fit) {
                    placed.Add(new List<(string, int, int, int)>{current});
                }
            }

            var buffer = new StringBuilder();
            buffer.AppendLine("<div class=\"reads-alignment\">");

            int max_depth = 0;
            const int bucketsize = 5;
            for (int pos = 0; pos <= sequence.Length / bucketsize; pos++) {
                // Add the sequence and the number to tell the position
                string number = ((pos+1)*bucketsize).ToString();
                buffer.Append($"<div class='align-block'><p><span class=\"number\">{String.Concat(Enumerable.Repeat("&nbsp;", bucketsize-number.Length))}{number}</span><br><span class=\"seq\">{sequence.Substring(pos*bucketsize, Math.Min(bucketsize, sequence.Length - pos*bucketsize))}</span><br>");
                
                int[] depth = new int[bucketsize];
                // Add every niveau in order
                foreach (var line in placed) {
                    for (int i = pos*bucketsize; i < pos*bucketsize+bucketsize; i++) {
                        string result = "&nbsp;";
                        foreach(var read in line) {
                            if (i >= read.Item2 && i < read.Item3) {
                                result = $"<a href=\"#R{read.Item4:D4}\" class=\"align-link\">{read.Item1[i - read.Item2]}</a>";
                                depth[i-pos*bucketsize]++;
                            }
                        }
                        buffer.Append(result);
                    }
                    buffer.Append("<br>");
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = 0; i < bucketsize; i++) {
                    if (depth[i] > max_depth) max_depth = depth[i];
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depth[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }
            buffer.AppendLine("</div>");

            return buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{max_depth}'>");
        }
        /// <summary> Returns the string representation of the human friendly identifier of a node. </summary>
        /// <param name="index"> The index in the condensed graph of the condensed node. </param>
        /// <returns> A string to be used where humans can see it. </returns>
        private string GetCondensedNodeLink(int index) {
            return $"<a href=\"#I{index:D4}\" class=\"info-link contig-link\">I{index:D4}</a>";
        }
        /// <summary> Returns the string representation of the human friendly identifier of a read. </summary>
        /// <param name="index"> The index in the readslist. </param>
        /// <returns> A string to be used where humans can see it. </returns>
        private string GetReadLink(int index) {
            return $"<a href=\"#R{index:D4}\" class=\"info-link read-link\">R{index:D4}</a>";
        }
        /// <summary> Returns some meta information about the assembly the help validate the output of the assembly. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        public string HTMLMetaInformation()
        {
            long number_edges = graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
            long number_edges_condensed = condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count() ) / 2L;

            string html = $@"
<h3>General information</h3>
<table>
<tr><td>Number of reads</td><td>{meta_data.reads}</td></tr>
<tr><td>K (length of k-mer)</td><td>{kmer_length}</td></tr>
<tr><td>Minimum homology</td><td>{minimum_homology}</td></tr>
<tr><td>Number of k-mers</td><td>{meta_data.kmers}</td></tr>
<tr><td>Number of (k-1)-mers</td><td>{meta_data.kmin1_mers}</td></tr>
<tr><td>Number of duplicate (k-1)-mers</td><td>{meta_data.kmin1_mers_raw - meta_data.kmin1_mers}</td></tr>
<tr><td>Number of sequences found</td><td>{meta_data.sequences}</td></tr>
</table>

<h3>de Bruijn Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{graph.Length}</td></tr>
<tr><td>Number of edges</td><td>{number_edges}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double) number_edges / graph.Length:F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{graph.Aggregate(0D, (a, b) => (a > b.EdgesCount()) ? (double) a : (double) b.EdgesCount()) / 2D}</td></tr>
</table>

<h3>Condensed Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{condensed_graph.Count()}</td></tr>
<tr><td>Number of edges</td><td>{number_edges_condensed}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double) number_edges_condensed / condensed_graph.Count():F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{condensed_graph.Aggregate(0D, (a, b) => (a > b.ForwardEdges.Count() + b.BackwardEdges.Count()) ? a : (double) b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2D}</td></tr>
<tr><td>Average sequence length</td><td>{condensed_graph.Aggregate(0D, (a, b) => (a + b.Sequence.Count()))/condensed_graph.Count():F3}</td></tr>
<tr><td>Total sequence length</td><td>{condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()))}</td></tr>
</table>

<h3>Runtime information</h3>
<p>Total time: {meta_data.total_time + meta_data.drawingtime} ms</p>
<div class=""runtime"">
<div class=""pre-work"" style=""flex:{meta_data.pre_time}"">
    <p>Pre</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Pre work</span>
        <span class=""runtime-time"">{meta_data.pre_time} ms</span>
        <span class=""runtime-desc"">Work done on generating k-mers and (k-1)-mers.</span>
    </div>
</div>
<div class=""linking-graph"" style=""flex:{meta_data.graph_time}"">
    <p>Linking</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Linking graph</span>
        <span class=""runtime-time"">{meta_data.graph_time} ms</span>
        <span class=""runtime-desc"">Work done to build the de Bruijn graph.</span>
    </div>
</div>
<div class=""finding-paths"" style=""flex:{meta_data.path_time}"">
    <p>Path</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Finding paths</span>
        <span class=""runtime-time"">{meta_data.path_time} ms</span>
        <span class=""runtime-desc"">Work done to find the paths through the graph.</span>
    </div>
</div>
<div class=""drawing"" style=""flex:{meta_data.drawingtime}"">
    <p>Drawing</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Drawing the graphs</span>
        <span class=""runtime-time"">{meta_data.drawingtime} ms</span>
        <span class=""runtime-desc"">Work done by graphviz (dot) to draw the graphs.</span>
    </div>
</div>
</div>";

            return html;
        }
        /// <summary> Fill metainformation in a CSV line and append it to the given file. </summary>
        /// <param name="ID">ID of the run to recognise it in the CSV file. </param>
        /// <param name="filename"> The file to which to append the CSV line to. </param>
        /// <param name="path_to_template"> The path to the original fasta file, to get extra information. </param>
        /// <param name="extra"> Extra field to fill in own information. Created for holding the alphabet. </param>
        /// <param name="path_to_report"> The path to the report to add a hyperlink to the CSV file. </param>
        public void CreateCSVLine(string ID, string filename = "report.csv", string path_to_template = null, string extra = "", string path_to_report = "", bool extended = false) {
            // If the original sequence is known, calculate the coverage
            string coverage = "";
            if (path_to_template != null) {
                // Get the sequences
                var fastafile = File.ReadAllText(path_to_template);
                var raw_sequences = Regex.Split(fastafile, ">");
                var seqs = new List<string> ();

                foreach (string seq in raw_sequences) {
                    var seq_lines = seq.Split("\n".ToCharArray());
                    string sequence = "";
                    for (int i = 1; i < seq_lines.Length; i++) {
                        sequence += seq_lines[i].Trim("\r\n\t 0123456789".ToCharArray());
                    }
                    if (sequence != "") seqs.Add(sequence);
                }

                // Calculate the coverage
                string[] reads_array = reads.Select(x => Assembler.AminoAcid.ArrayToString(x)).ToArray();
                string[] contigs_array = condensed_graph.Select(x => Assembler.AminoAcid.ArrayToString(x.Sequence.ToArray())).ToArray();

                if (seqs.Count() == 2 && !extended) {
                    var coverage_reads_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[0], reads_array);
                    var coverage_reads_light = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[1], reads_array);
                    var coverage_contigs_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[0], contigs_array);
                    var coverage_contigs_light = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[1], contigs_array);

                    coverage = $"{coverage_reads_heavy.Item1};{coverage_reads_heavy.Item2};{coverage_reads_light.Item1};{coverage_reads_light.Item2};{coverage_contigs_heavy.Item1};{coverage_contigs_heavy.Item2};{coverage_contigs_light.Item1};{coverage_contigs_light.Item2};";
                } else if (seqs.Count() == 2 && extended) {
                    // Filter only assembled contigs
                    contigs_array = condensed_graph.Where(n => n.Origins.Count() > 1).Select(n => (n.Prefix != null ? AminoAcid.ArrayToString(n.Prefix.ToArray()) : "" ) + AminoAcid.ArrayToString(n.Sequence.ToArray()) + (n.Suffix != null ? AminoAcid.ArrayToString(n.Suffix.ToArray()) : "")).ToArray();

                    var coverage_reads_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[0], reads_array);
                    var coverage_reads_light = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[1], reads_array);
                    var coverage_contigs_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[0], contigs_array);
                    var coverage_contigs_light = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[1], contigs_array);

                    // Create list of unique mapped peptides
                    List<string> correct_contigs = new List<string>(coverage_contigs_heavy.Item2);
                    foreach (var x in coverage_contigs_light.Item2) {
                        if (!correct_contigs.Contains(x)) correct_contigs.Add(x);
                    }

                    var coverage_correct_contigs_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[0], correct_contigs.ToArray());
                    var coverage_correct_contigs_light = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[1], correct_contigs.ToArray());

                    coverage = $"{coverage_contigs_heavy.Item1};{coverage_contigs_light.Item1};{coverage_contigs_heavy.Item3};{coverage_contigs_light.Item3};{contigs_array.Count()};{correct_contigs.Count()};{coverage_reads_heavy.Item1};{coverage_reads_light.Item1};{coverage_reads_heavy.Item3};{coverage_reads_light.Item3};{coverage_correct_contigs_heavy.Item1};{coverage_correct_contigs_light.Item1};{coverage_correct_contigs_heavy.Item3};{coverage_correct_contigs_light.Item3};{condensed_graph.Aggregate("", (a, b) => a + b.Sequence.Count() + "|")};";
                } else {
                    Console.WriteLine($"Not an antibody fasta file: {path_to_template}");
                }
            }

            // If the path to the report is known create a hyperlink
            string link = "";
            if (path_to_report != "" && !extended) {
                link = $"=HYPERLINK(\"{path_to_report}\");";
            }
            
            int totallength = condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()));
            int totalnodes = condensed_graph.Count();
            string line = $"{ID};{extra};{meta_data.reads};{kmer_length};{minimum_homology};{totalnodes};{(double) totallength/ totalnodes};{totallength};{(double) condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count() ) / 2L / condensed_graph.Count()};{meta_data.total_time};{meta_data.drawingtime};{coverage}{link}\n";
            
            if (File.Exists(filename)) {
                // TO account for multithreading and multiple workers trying to append to the file at the same time
                bool stuck = true;
                while (stuck) {
                    try {
                        File.AppendAllText(filename, line);
                        stuck = false;
                    } catch {
                        // try again
                    }
                }
            } else {
                StreamWriter sw = File.CreateText(filename);
                sw.Write(line);
                sw.Close();
            }
        }
        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        /// <param name="filename"> The path / filename to store the report in and where to find the graph.svg </param>
        public void CreateReport(string filename = "report.html") {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fullpath = filename;
            try {
                fullpath = Path.GetFullPath(filename);
            } catch {
                //
            }

            string graphoutputpath = Path.GetDirectoryName(fullpath).ToString() + $"\\graph-{Interlocked.Increment(ref counter)}.dot";

            // Console.WriteLine(graphoutputpath);
            OutputGraph(graphoutputpath);

            string graphpath = Path.ChangeExtension(graphoutputpath, "svg");
            string svg = "<p>Graph not found, searched at:" + graphpath + "</p>";
            if (File.Exists(graphpath)) {
                svg = File.ReadAllText(graphpath);
                // Give the filled nodes (start node) the correct class
                svg = Regex.Replace(svg, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
                // Give the nodes the correct ID
                svg = Regex.Replace(svg, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>i([0-9]+)</title>", "id=\"node$2\" class=\"$1\" onclick=\"Select('I', $2)\">");
                // Strip all <title> tags
                svg = Regex.Replace(svg, "<title>[^<]*</title>", "");
            }

            // Could also be done as an img, but that is much less nice
            // <img src='graph.png' alt='Extended graph of the results' srcset='graph.svg'>

            string simplegraphpath = new string(Path.ChangeExtension(graphoutputpath, "").Take(Path.ChangeExtension(graphoutputpath, "").Length - 1).ToArray()) + "-simple.svg";
            string simplesvg = "<p>Simple graph not found, searched at:" + simplegraphpath + "</p>";
            if (File.Exists(simplegraphpath)) {
                simplesvg = File.ReadAllText(simplegraphpath);
                // Give the filled nodes (start node) the correct class
                simplesvg = Regex.Replace(simplesvg, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
                // Give the nodes the correct ID
                simplesvg = Regex.Replace(simplesvg, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>i([0-9]+)</title>", "id=\"simple-node$2\" class=\"$1\" onclick=\"Select('I', $2)\">");
                // Strip all <title> tags
                simplesvg = Regex.Replace(simplesvg, "<title>[^<]*</title>", "");
            }

            string stylesheet = "/* Could not find the stylesheet */";
            if (File.Exists("styles.css")) stylesheet = File.ReadAllText("styles.css");

            string script = "// Could not find the script";
            if (File.Exists("script.js")) script = File.ReadAllText("script.js");

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            stopwatch.Stop();
            meta_data.drawingtime = stopwatch.ElapsedMilliseconds;

            string html = $@"<html>
<head>
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Report Protein Sequence Run</title>
<style>
{stylesheet}
</style>
<script>
{script}
</script>
</head>
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

<input type=""checkbox"" id=""graph-collapsable""/>
<label for=""graph-collapsable"">Graph</label>
<div class=""collapsable"">{svg}</div>

<input type=""checkbox"" id=""simple-graph-collapsable""/>
<label for=""simple-graph-collapsable"">Simplified Graph</label>
<div class=""collapsable"">{simplesvg}</div>

<input type=""checkbox"" id=""table-collapsable""/>
<label for=""table-collapsable"">Table</label>
<div class=""collapsable"">{CreateContigsTable()}</div>

<input type=""checkbox"" id=""reads-table-collapsable""/>
<label for=""reads-table-collapsable"">Reads Table</label>
<div class=""collapsable"">{CreateReadsTable()}</div>

<input type=""checkbox"" id=""meta-collapsable""/>
<label for=""meta-collapsable"">Meta Information</label>
<div class=""collapsable meta-collapsable"">{HTMLMetaInformation()}</div>

<div class=""footer"">
    <p>Code written in 2019</p>
    <p>Made by the Hecklab</p>
</div>

</div>
<div class=""aside-handle"" id=""aside-handle"">
<span class=""handle"">&lt;&gt;</span>
</div>
<div class=""aside"" id=""aside"">
<div class=""aside-wrapper"">
{CreateAsides()}
</div>
</div>
</body>";
            StreamWriter sw = File.CreateText(filename);
            sw.Write(html);
            sw.Close();
        }
    }
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    static class HelperFunctionality
    {
        /// <summary> To copy a subarray to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created subarray. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        /// <summary> This aligns a list of sequences to a template sequence based on character identity. </summary>
        /// <returns> Returns a ValueTuple with the coverage as the first item (as a Double) and the amount of 
        /// correctly aligned reads as an Int as the second item. </returns>
        /// <remark> This code does not account for small defects in reads, it will only align perfect matches 
        /// and it will only align matches tha fit entirely inside the template sequence (no overhang at the start or end). </remark>
        /// <param name="template"> The template to match against. </param>
        /// <param name="sequences"> The sequences to match with. </param>
        public static (double, int) MultipleSequenceAlignmentToTemplate(string template, string[] sequences) 
        {
            // Keep track of all places already covered
            bool[] covered = new bool[template.Length];
            int correct_reads = 0;

            foreach (string seq in sequences) {
                bool found = false;
                for (int i = 0; i < template.Length; i++) {
                    // Try aligning on this position
                    bool hit = true;
                    for (int j = 0; j < seq.Length && hit; j++) {
                        if (i+j > template.Length - 1 || template[i+j] != seq[j]) {
                            hit = false;
                        }
                    }

                    // The alignment succeeded, record this in the covered array
                    if (hit) {
                        for (int k = 0; k < seq.Length; k++) {
                            covered[i+k] = true;
                        }
                        found = true;
                    }
                }
                if (found == true) {
                    correct_reads += 1;
                }
            }

            int hits = 0;
            foreach (bool b in covered) {
                if (b) hits++;
            }

            return ((double)hits/covered.Length, correct_reads);
        }
        /// <summary> This aligns a list of sequences to a template sequence based on character identity. </summary>
        /// <returns> Returns a ValueTuple with the coverage as the first item (as a Double) and the  
        /// correctly aligned reads as an array as the second item and the depth of coverage as the third item. </returns>
        /// <remark> This code does not account for small defects in reads, it will only align perfect matches 
        /// and it will only align matches tha fit entirely inside the template sequence (no overhang at the start or end). </remark>
        /// <param name="template"> The template to match against. </param>
        /// <param name="sequences"> The sequences to match with. </param>
        public static (double, string[], double) MultipleSequenceAlignmentToTemplateExtended(string template, string[] sequences) 
        {
            // Keep track of all places already covered
            bool[] covered = new bool[template.Length];
            int total_placed_reads_length = 0;
            List<string> placed_sequences = new List<string>();

            foreach (string seq in sequences) {
                bool found = false;
                for (int i = 0; i < template.Length; i++) {
                    // Try aligning on this position
                    bool hit = true;
                    for (int j = 0; j < seq.Length && hit; j++) {
                        if (i+j > template.Length - 1 || template[i+j] != seq[j]) {
                            hit = false;
                        }
                    }

                    // The alignment succeeded, record this in the covered array
                    if (hit) {
                        for (int k = 0; k < seq.Length; k++) {
                            covered[i+k] = true;
                        }
                        found = true;
                        total_placed_reads_length += seq.Length;
                    }
                }
                if (found == true) {
                    placed_sequences.Add(seq);
                }
            }

            int hits = 0;
            foreach (bool b in covered) {
                if (b) hits++;
            }

            return ((double)hits/covered.Length, placed_sequences.ToArray(), (double)total_placed_reads_length / template.Length);
        }
        /// <summary> This aligns a list of sequences to a template sequence based on the alphabet given. </summary>
        /// <returns> Returns a list of tuples with the sequences as first item, startinposition as second item, 
        /// end position as third item and identifier from the given list as fourth item. </returns>
        /// <remark> This code does not account for small defects in reads, it will only align perfect matches 
        /// and it will only align matches tha fit entirely inside the template sequence (no overhang at the start or end). </remark>
        /// <param name="template"> The template to match against. </param>
        /// <param name="sequences"> The sequences to match with. </param>
        /// <param name="alphabet"> The alphabet used to match (when none is given defaults to pure character identity). </param>
        /// <param name="scoring_matrix"> The scoring matrix used in conjunction with the alphabet to match (when none is given defaults to pure character identity). </param>
        /// <param name="reverse"> Whether or not the alignment should also be done in reverse direction. </param>
        public static List<(string, int, int, int)> MultipleSequenceAlignmentToTemplate(string template, List<(string, int)> sequences, char[] alphabet, int[,] scoring_matrix, bool reverse = false) 
        {
            // Keep track of all places already covered
            var result = new List<(string, int, int, int)>();

            foreach (var current in sequences) {
                string seq = current.Item1;
                for (int i = -seq.Length + 1; i < template.Length; i++) {
                    // Try aligning on this position
                    bool hit = true;
                    int score_fw_t = 0;
                    int score_bw_t = 0;
                    for (int j = 0; j < seq.Length && hit; j++) {
                        if (i+j >= template.Length) {
                            break;
                        }
                        if (i+j >= 0) {
                            //Console.WriteLine($"i {i} j{j} l{seq.Length} t{template.Length}");
                            int score_fw = scoring_matrix[Array.IndexOf(alphabet, template[i+j]), Array.IndexOf(alphabet, seq[j])];
                            int score_bw = scoring_matrix[Array.IndexOf(alphabet, template[i+j]), Array.IndexOf(alphabet, seq[seq.Length-j-1])];
                            score_fw_t += score_fw;
                            score_bw_t += score_bw;

                            if (score_fw < 1 && score_bw < 1) {
                                hit = false;
                            }
                        }
                    }

                    // The alignment succeeded
                    if (hit) {
                        if (score_fw_t >= score_bw_t && score_fw_t > 2) result.Add((seq, i, i+seq.Length, current.Item2));
                        else if (score_bw_t > 2) result.Add((new string(seq.Reverse().ToArray()), i, i+seq.Length, current.Item2));
                    }
                }
            }

            return result;
        }
    }
}