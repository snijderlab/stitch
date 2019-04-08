using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

/// <summary> This is a project to build a piece of software that is able to rebuild a protein sequence
/// from reads of a massspectrometer. 
/// The software is build by Douwe Schulte and was started on 25-03-2019.
/// It is build in collaboration with and under supervision of Joost Snijder,
/// from the group "Massspectrometry and Proteomics" at the university of Utrecht. </summary>

namespace AssemblyNameSpace
{
    /// <summary> A Class to be able to run the code from the commandline. To be able to test it easily. 
    /// This will be rewritten when the code is moved to its new repository </summary>
    class ToRunWithCommandLine
    {
        static void Main()
        {
            var test = new Assembler(8, 5);
            //test.SetAlphabet({('L', 'I', 1, false), ('K', 'Q', 1, true)});
            test.OpenReads("examples/006/reads.txt");
            Console.WriteLine("Now starting on the assembly");
            test.Assemble();
            test.OutputReport();
        }
    }
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <value> The reads fed into the Assembler, as opened by OpenReads. </value>
        List<AminoAcid[]> reads = new List<AminoAcid[]>();
        /// <value> The De Bruijn graph used by the Assembler. </value>
        Node[] graph;
        /// <value> The condensed graph used to store the output of the assembly. </value>
        List<CondensedNode> condensed_graph;
        /// <value> The length of the k-mers used to create the De Bruijn graph. Private member where it is stored. </value>
        private int kmer_length;
        /// <value> The length of the k-mers used to create the De Bruijn graph. Get and Set is public. </value>
        public int Kmer_length
        {
            get { return kmer_length; }
            set { kmer_length = value; }
        }
        /// <value> The matrix used for scoring of the alignment between two characters in the alphabet. 
        /// As such this matrix is rectangular. </value>
        private int[,] scoring_matrix;
        /// <value> The alphabet used for alignment. The default value is all the amino acids in order of
        /// natural abundance in prokaryotes to make finding the right amino acid a little bit faster. </value>
        private char[] alphabet;
        /// <value> The private member to store the minimum homology value in. </value>
        private int minimum_homology;
        /// <value> The minimum homology value of an edge to include it in the graph. Lowering the limit 
        /// could result in a longer sequence retrieved from the algorithm but would also greatly increase
        /// the computational cost of the calculation. </value>
        public int Minimum_homology
        {
            get { return minimum_homology; }
            set { minimum_homology = value; }
        }
        private int edge_include_limit;
        private MetaInformation meta_data;
        /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can 
        /// be used in an efficient way </summary>
        private struct AminoAcid
        {
            /// <value> The Assembler used to create the AminoAcd, used to get the information of the alphabet. </value>
            private Assembler parent;
            /// <value> The code (index of the char in the alpabet array of the parent). </value>
            private int code;
            /// <value> The code (index of the char in the alpabet array of the parent). Gives only a Get option. 
            /// The only way to change it is in the creator. </value>
            public int Code
            {
                get
                {
                    return code;
                }
            }

            /// <summary> The creator of AminoAcids. </summary>
            /// <param name="asm"> The Assembler that this AminoAcid is used in, to get the alphabet. </param>
            /// <param name="input"> The character to store in this AminoAcid. </param>
            /// <returns> Returns a reference to the new AminoAcid. </returns>
            public AminoAcid(Assembler asm, char input)
            {
                parent = asm;
                code = asm.getIndexInAlphabet(input);
            }
            /// <summary> Will create a string of this AminoAcid. Consiting of the character used to 
            /// create this AminoAcid. </summary>
            public override string ToString()
            {
                return parent.alphabet[code].ToString();
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
            public override bool Equals(object obj)
            {
                return obj is AminoAcid && this == (AminoAcid)obj;
            }
            /// <summary> To check for equality of arrays of AminoAcids. </summary>
            /// <remarks> Implemented as a shortcircuiting loop with the equals operator (==). </remarks>
            /// <param name="left"> The first object to check equality with. </param>
            /// <param name="right"> The second object to check equality with. </param>
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
            public static bool operator ==(AminoAcid x, AminoAcid y)
            {
                return x.Code == y.Code;
            }
            /// <summary> To check for inequality of AminoAcids. </summary>
            /// <remarks> Implemented as a a reverse of the equals operator. </remarks>
            public static bool operator !=(AminoAcid x, AminoAcid y)
            {
                return !(x == y);
            }
            /// <summary> To get a hashcode for this AminoAcid. </summary>
            public override int GetHashCode()
            {
                return this.code.GetHashCode();
            }
            /// <summary> Calculating homology, using the scoring matrix of the parent Assembler. </summary>
            /// <remarks> Depending on which rules are put into the scoring matrix the order in which this 
            /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to 
            /// <c>b.Homology(a)</c>. </remarks>
            public int Homology(AminoAcid right)
            {
                return parent.scoring_matrix[this.Code, right.Code];
            }
            /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix 
            /// of the parent Assembler. </summary>
            /// <remarks> Two arrays of different length will result in a value of 0. This function loops
            /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
            /// <param name="left"> The first object to calculate homology with. </param>
            /// <param name="right"> The second object to calculate homology with. </param>
            public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right)
            {
                int score = 0;
                if (left.Length != right.Length)
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
            /// <value> The member to store the sequence information in. </value>
            private AminoAcid[] sequence;
            /// <value> The sequence of the Node. Only has a getter. </value>
            public AminoAcid[] Sequence { get { return sequence; } }
            /// <value> The member to store the multiplicity (amount of k-mers which 
            /// result in the same (k-1)-mers in. </value>
            private int multiplicity;
            /// <value> The multiplicity, amount of k-mers which 
            /// result in the same (k-1)-mers, of the Node. Only has a getter. </value>
            public int Multiplicity { get { return multiplicity; } }
            /// <value> The list of edges from this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. The private 
            /// member to store the list. </value>
            private List<ValueTuple<int, int, int>> forwardEdges;
            /// <value> The list of edges from this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. Only has a getter. </value>
            public List<ValueTuple<int, int, int>> ForwardEdges { get { return forwardEdges; } }
            /// <value> The list of edges to this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. The private 
            /// member to store the list. </value>
            private List<ValueTuple<int, int, int>> backwardEdges;
            /// <value> The list of edges to this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. Only has a getter. </value>
            public List<ValueTuple<int, int, int>> BackwardEdges { get { return backwardEdges; } }
            /// <value> Wether or not this node is visited yet. </value>
            public bool Visited;
            private int max_forward_score;
            private int max_backward_score;
            private int edge_include_limit;

            /// <summary> The creator of Nodes. </summary>
            /// <param name="seq"> The sequence of this Node. </param>
            /// <param name="multi"> The multiplicity of this Node. </param>
            /// <remarks> It will initialize the edges list. </remarks>
            public Node(AminoAcid[] seq, int multi, int edge_include_limit_input)
            {
                sequence = seq;
                multiplicity = multi;
                forwardEdges = new List<ValueTuple<int, int, int>>();
                backwardEdges = new List<ValueTuple<int, int, int>>();
                Visited = false;
                max_forward_score = 0;
                max_backward_score = 0;
                edge_include_limit = edge_include_limit_input;
            }

            /// <summary> To add a forward edge to the Node. Wil only be added if the score is high enough. </summary>
            /// <param name="target"> The index of the Node where this edge goes to. </param>
            /// <param name="score1"> The homology of the edge with the first Node. </param>
            /// <param name="score2"> The homology of the edge with the second Node. </param>
            public void AddForwardEdge(int target, int score1, int score2)
            {
                int score = score1 + score2;
                if (score <= max_forward_score && score >= max_forward_score - edge_include_limit)
                {
                    forwardEdges.Add((target, score1, score2));
                    return;
                }
                if (score > max_forward_score)
                {
                    max_forward_score = score;
                    forwardEdges.Add((target, score1, score2));
                    filterForwardEdges();
                }

            }
            /// <summary> To add a backward edge to the Node. </summary>
            /// <param name="target"> The index of the Node where this edge comes from. </param>
            /// <param name="score1"> The homology of the edge with the first Node. </param>
            /// <param name="score2"> The homology of the edge with the second Node. </param>
            public void AddBackwardEdge(int target, int score1, int score2)
            {
                int score = score1 + score2;
                if (score <= max_backward_score && score >= max_backward_score - edge_include_limit)
                {
                    backwardEdges.Add((target, score1, score2));
                    return;
                }
                if (score > max_backward_score)
                {
                    max_backward_score = score;
                    backwardEdges.Add((target, score1, score2));
                    filterBackwardEdges();
                }
            }
            private void filterForwardEdges()
            {
                Console.Write($"Filtered forward edges from {forwardEdges.Count} to ");
                forwardEdges = forwardEdges.Where(i => i.Item2 + i.Item3 >= max_forward_score - edge_include_limit).ToList();
                Console.Write($"{forwardEdges.Count}.");
            }
            private void filterBackwardEdges()
            {
                //Console.Write($"Filtered forward edges from {backwardEdges.Count} to ");
                backwardEdges = backwardEdges.Where(i => i.Item2 + i.Item3 >= max_backward_score - edge_include_limit).ToList();
                //Console.Write($"{backwardEdges.Count}.");
            }
            /// <summary> To check if the Node has forward edges. </summary>
            public bool HasForwardEdges()
            {
                return forwardEdges.Count > 0;
            }
            /// <summary> To check if the Node has backward edges. </summary>
            public bool HasBackwardEdges()
            {
                return backwardEdges.Count > 0;
            }
            /// <summary> To get the amount of edges (forward and backward). </summary>
            /// <remarks> O(1) runtime </remarks>
            public int EdgesCount()
            {
                return forwardEdges.Count + backwardEdges.Count;
            }
            /// <summary> Gets the forward edge with the highest total homology of all 
            /// edges in this Node. </summary>
            /// <returns> It returns the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. </returns>
            /// <exception cref="Exception"> It will result in an Exception if the Node has no forward edges. </exception>
            public ValueTuple<int, int, int> MaxForwardEdge()
            {
                if (!HasForwardEdges())
                    throw new Exception("Cannot give an edge if this node has no edges");
                var output = forwardEdges[0];
                int value = output.Item2 + output.Item3;
                int max = value;
                for (int i = 0; i < forwardEdges.Count; i++)
                {
                    value = forwardEdges[i].Item2 + forwardEdges[i].Item3;
                    if (value > max)
                    {
                        max = value;
                        output = forwardEdges[i];
                    }
                }
                return output;
            }
            /// <summary> Gets the backward edge with the highest total homology of all 
            /// edges in this Node. </summary>
            /// <returns> It returns the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. </returns>
            /// <exception cref="Exception"> It will result in an Exception if the Node has no backward edges. </exception>
            public ValueTuple<int, int, int> MaxBackwardEdge()
            {
                if (!HasBackwardEdges())
                    throw new Exception("Cannot give an edge if this node has no edges");
                var output = backwardEdges[0];
                int value = output.Item2 + output.Item3;
                int max = value;
                for (int i = 0; i < backwardEdges.Count; i++)
                {
                    value = backwardEdges[i].Item2 + backwardEdges[i].Item3;
                    if (value > max)
                    {
                        max = value;
                        output = backwardEdges[i];
                    }
                }
                return output;
            }
        }
        /// <summary> A struct to hold meta information about the assembly to keep it organised 
        /// and to report back to the user. </summary>
        private struct MetaInformation
        {
            public long total_time, pre_time, graph_time, path_time, sequence_filter_time;
            public int reads, kmers, kmin1_mers, sequences;
        }
        /// <summary> Nodes in the condensed graph with a variable sequence length. </summary>
        private class CondensedNode
        {
            public int Index;
            public bool Visited;
            public List<AminoAcid> Sequence;
            public List<ValueTuple<int, CondensedNode>> ForwardEdges;
            public List<ValueTuple<int, CondensedNode>> BackwardEdges;
            public CondensedNode(List<AminoAcid> sequence, int index)
            {
                Sequence = sequence;
                Index = index;
                ForwardEdges = new List<(int, CondensedNode)>();
                BackwardEdges = new List<(int, CondensedNode)>();
                Visited = false;
            }
        }

        /// <summary> The creator, to set up the default values. Also sets the alphabet. </summary>
        /// <param name="kmer_length_input"> The lengths of the k-mers. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        public Assembler(int kmer_length_input, int minimum_homology_input, int edge_include_limit_input = 0)
        {
            kmer_length = kmer_length_input;
            minimum_homology = minimum_homology_input;
            edge_include_limit = edge_include_limit_input;

            SetAlphabet();
        }
        /// <summary> Find the index of the given character in the alphabet. </summary>
        /// <param name="c"> The character to lot op. </param>
        private int getIndexInAlphabet(char c)
        {
            for (int i = 0; i < alphabet.Length; i++)
            {
                if (c == alphabet[i])
                {
                    return i;
                }
            }
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
        /// <summary> To open a file with reads (should always be run before trying to assemble). 
        /// It will save the reads in the current Assembler object. </summary>
        /// <param name="input_file"> The path to the file to read from. </param>
        public void OpenReads(string input_file)
        {
            // Getting input
            // For now just use a minimal implementation, reads separated b whitespace

            if (!File.Exists(input_file))
                throw new Exception("The specified file does not exist, file asked for: " + input_file);

            List<string> lines = File.ReadLines(input_file).ToList();
            List<string> reads_string = new List<string>();

            lines.ForEach(line =>
            {
                if (line[0] != '#')
                    reads_string.AddRange(line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            });

            reads = new List<AminoAcid[]>();
            AminoAcid[] acids;
            char[] chars;

            foreach (string read in reads_string)
            {
                acids = new AminoAcid[read.Length];
                chars = read.ToCharArray();

                for (int i = 0; i < chars.Length; i++)
                {
                    acids[i] = new AminoAcid(this, chars[i]);
                }

                reads.Add(acids);
            }
            meta_data.reads = reads.Count;
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

            var kmers = new List<AminoAcid[]>();

            foreach (AminoAcid[] read in reads)
            {
                if (read.Length > kmer_length)
                {
                    for (int i = 0; i < read.Length - kmer_length + 1; i++)
                    {
                        AminoAcid[] kmer = read.SubArray(i, kmer_length);
                        kmers.Add(kmer);
                        kmers.Add(kmer.Reverse().ToArray()); //Also add the reverse
                    }
                }
                else if (read.Length == kmer_length)
                {
                    kmers.Add(read);
                    kmers.Add(read.Reverse().ToArray()); //Also add the reverse
                }
                else
                {
                    Console.WriteLine($"A read is no long enough: {AminoAcid.ArrayToString(read)}");
                }
            }
            meta_data.kmers = kmers.Count;

            // Building the graph
            // Generating all (k-1)-mers

            var kmin1_mers_raw = new List<AminoAcid[]>();

            kmers.ForEach(kmer =>
            {
                kmin1_mers_raw.Add(kmer.SubArray(0, kmer_length - 1));
                kmin1_mers_raw.Add(kmer.SubArray(1, kmer_length - 1));
            });

            var kmin1_mers = new List<ValueTuple<AminoAcid[], int>>();

            /* kmin1_mers_raw.GroupBy(i => i).ToList().ForEach(kmin1_mer =>
            {
                kmin1_mers.Add((kmin1_mer.Key, kmin1_mers.Count()));
            }); */

            kmin1_mers = (from ele in kmin1_mers_raw group ele by ele into ele_group select (ele_group.Key, ele_group.Count())).ToList();
            // TODO should save the amount of duplicates
            meta_data.kmin1_mers = kmin1_mers.Count;

            // Create a node for every possible (k-1)-mer (one amino acid shifted)

            // Implement the graph as a adjacency list (array)
            graph = new Node[kmin1_mers.Count];

            int index = 0;
            kmin1_mers.ForEach(kmin1_mer =>
            {
                graph[index] = new Node(kmin1_mer.Item1, kmin1_mer.Item2, edge_include_limit);
                index++;
            });

            meta_data.pre_time = stopWatch.ElapsedMilliseconds;

            // Connect the nodes based on the k-mers

            // Initialize the array to save computations
            int k = 0;
            int[] prefix_matches = new int[graph.Length];
            int[] postfix_matches = new int[graph.Length];
            AminoAcid[] prefix, postfix;

            kmers.ForEach(kmer =>
            {
                k++;
                if (k % 100 == 0)
                {
                    Console.WriteLine($"Adding edges... added {k} k-mers");
                }

                prefix = kmer.SubArray(0, kmer_length - 1);
                postfix = kmer.SubArray(1, kmer_length - 1);

                //Console.WriteLine("Computing pre and post fixes");

                // Precompute the homology with every node to speed up computation
                for (int i = 0; i < graph.Length; i++)
                {
                    // Find the homology of the prefix and postfix
                    int first_homology = AminoAcid.ArrayHomology(graph[i].Sequence, prefix);
                    int second_homology = AminoAcid.ArrayHomology(graph[i].Sequence, postfix);

                    // Save the results in the arrays
                    prefix_matches[i] = first_homology;
                    postfix_matches[i] = second_homology;
                }

                //Console.WriteLine("Computed pre and post fixes");

                // Test for pre and post fixes for every node
                for (int i = 0; i < graph.Length; i++)
                {
                    if (prefix_matches[i] >= minimum_homology)
                    {
                        //Console.WriteLine($"Found a prefix match at {i} with score {prefix_matches[i]} and postfix score {postfix_matches[i]}");
                        for (int j = 0; j < graph.Length; j++)
                        {
                            if (i != j && postfix_matches[j] >= minimum_homology)
                            {
                                graph[i].AddForwardEdge(j, prefix_matches[i], postfix_matches[i]);
                                graph[j].AddBackwardEdge(i, prefix_matches[i], postfix_matches[i]);
                            }
                        }
                    }
                }
            });

            meta_data.graph_time = stopWatch.ElapsedMilliseconds - meta_data.pre_time;
            Console.WriteLine($"Built graph");

            // Create a condensed graph to keep the information

            condensed_graph = new List<CondensedNode>();
            //Queue graph_building_queue = new Queue();

            for (int i = 0; i < graph.Length; i++)
            {
                var start_node = graph[i];

                if (!start_node.Visited)
                {
                    var forward_node = start_node;
                    var backward_node = start_node;

                    List<AminoAcid> forward_sequence = new List<AminoAcid>();
                    List<AminoAcid> backward_sequence = new List<AminoAcid>();

                    List<int> forward_nodes = new List<int>();
                    List<int> backward_nodes = new List<int>();

                    bool forward_found = false;
                    bool backward_found = false;

                    // Walk forwards
                    while (forward_node.ForwardEdges.Count == 1 && forward_node.BackwardEdges.Count == 1)
                    {
                        forward_node.Visited = true;
                        forward_found = true;
                        forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2));
                        forward_node = graph[forward_node.ForwardEdges[0].Item1];
                    } 
                    forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2));

                    if (forward_node.ForwardEdges.Count() > 1) {
                        forward_nodes = (from node in forward_node.ForwardEdges select node.Item1).ToList();
                    }
                    if (forward_node.BackwardEdges.Count() > 1) {
                        forward_nodes = (from node in forward_node.BackwardEdges select node.Item1).ToList();
                    }                    

                    // Walk backwards
                    while (backward_node.ForwardEdges.Count == 1 && backward_node.BackwardEdges.Count == 1)
                    {
                        backward_node.Visited = true;
                        backward_found = true;
                        backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                        backward_node = graph[backward_node.ForwardEdges[0].Item1];
                    }
                    backward_sequence.Add(backward_node.Sequence.ElementAt(0));

                    if (backward_node.ForwardEdges.Count() > 1) {
                        backward_nodes = (from node in backward_node.ForwardEdges select node.Item1).ToList();
                    }
                    if (backward_node.BackwardEdges.Count() > 1) {
                        backward_nodes = (from node in backward_node.BackwardEdges select node.Item1).ToList();
                    }  

                    Console.WriteLine($"Sequences:\nBackward: {AminoAcid.ArrayToString(backward_sequence.ToArray())} last element {AminoAcid.ArrayToString(backward_node.Sequence)}\nForward: {AminoAcid.ArrayToString(forward_sequence.ToArray())} last element {AminoAcid.ArrayToString(forward_node.Sequence)}\nStart node: {AminoAcid.ArrayToString(start_node.Sequence)}");

                    backward_sequence.Reverse();
                    backward_sequence.AddRange(start_node.Sequence.SubArray(1, kmer_length - 3));
                    backward_sequence.AddRange(forward_sequence);

                    Console.WriteLine($"Result: {AminoAcid.ArrayToString(backward_sequence.ToArray())}");
                    Console.WriteLine($"Stopped because \nforward node had {forward_node.ForwardEdges.Count} forward edges and {forward_node.BackwardEdges.Count} backward edges\nbackward node had {backward_node.ForwardEdges.Count} forward edges and {backward_node.BackwardEdges.Count} backward edges");
                    Console.WriteLine($"Forward edges: {forward_node.ForwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");
                    Console.WriteLine($"Backward edges: {backward_node.BackwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");

                    condensed_graph.Add(new CondensedNode(backward_sequence, i));
                }
            }

            /* // Finding paths
            var sequences_raw = new List<AminoAcid[]>();

            // Try for every node to walk as far as possible to find the sequence
            for (int i = 0; i < graph.Length; i++)
            {
                var start_node = graph[i];
                var current_node = start_node;

                if (!current_node.CanVisit)
                {
                    List<AminoAcid> sequence = new List<AminoAcid>(); //current_node.Sequence.ToList();
                    int amount_found = 0;

                    // Forward
                    while (!current_node.CanVisit)
                    {
                        amount_found++;
                        sequence.Add(current_node.Sequence.ElementAt(kmer_length - 2));
                        current_node.Visit();
                        //Console.WriteLine($"Is this node visited now? {current_node.Visited}");
                        if (current_node.HasForwardEdges())
                        {
                            current_node = graph[current_node.MaxForwardEdge().Item1];
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Backward
                    var forward_node = current_node;

                    var backward_sequence = new List<AminoAcid>();

                    current_node = start_node;

                    current_node.UnVisit();

                    while (!current_node.CanVisit)
                    {
                        amount_found++;
                        backward_sequence.Add(current_node.Sequence.ElementAt(0));
                        current_node.Visit();
                        if (current_node.HasBackwardEdges())
                        {
                            current_node = graph[current_node.MaxBackwardEdge().Item1];
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (amount_found > 2)
                    {
                        // Add the forward sequence to the backward sequence to rebuild the whole sequence
                        //Console.WriteLine($"Sequences:\nBackward: {AminoAcid.ArrayToString(backward_sequence.ToArray())} last element {AminoAcid.ArrayToString(current_node.Sequence)}\nForward: {AminoAcid.ArrayToString(sequence.ToArray())} last element {AminoAcid.ArrayToString(forward_node.Sequence)}\nStart node: {AminoAcid.ArrayToString(start_node.Sequence)}");
                        backward_sequence.Reverse();
                        backward_sequence.AddRange(start_node.Sequence.SubArray(1, kmer_length - 3));
                        backward_sequence.AddRange(sequence);
                        //Console.WriteLine($"Result: {AminoAcid.ArrayToString(backward_sequence.ToArray())}");

                        // Save the sequence
                        sequences_raw.Add(backward_sequence.ToArray());
                    }
                    else
                    {
                        Console.WriteLine($"Could not find any nodes connected to: {AminoAcid.ArrayToString(current_node.Sequence)}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipped node {i} in finding sequences.");
                }
            } */

            meta_data.path_time = stopWatch.ElapsedMilliseconds - meta_data.graph_time - meta_data.pre_time;

            // Filtering of the sequences
/* 
            var sequences = new List<AminoAcid[]>();

            sequences_raw.Sort((x, y) => y.Length.CompareTo(x.Length));

            foreach (AminoAcid[] sequence in sequences_raw)
            {
                bool save = true;
                foreach (AminoAcid[] saved_sequence in sequences)
                {
                    // If this sequence is already in the saved sequences, do not save it
                    if (sequence.Length == saved_sequence.Length)
                    {
                        if (AminoAcid.ArrayEquals(sequence, saved_sequence) || AminoAcid.ArrayEquals(sequence.Reverse().ToArray(), saved_sequence))
                        {
                            save = false;
                            break;
                        }

                    }
                    // If this sequence is a subsequence of another saved sequence, do not save it
                    else if (sequence.Length < saved_sequence.Length)
                    {
                        var sequence_reverse = sequence.Reverse().ToArray();
                        for (int i = 0; i < saved_sequence.Length - sequence.Length + 1; i++)
                        {
                            var subarray = saved_sequence.SubArray(i, sequence.Length);
                            if (AminoAcid.ArrayEquals(sequence, subarray) || AminoAcid.ArrayEquals(sequence_reverse, subarray))
                            {
                                save = false;
                                break;
                            }
                        }
                    }
                }
                if (save) sequences.Add(sequence);
            } */

            // Returning output

            stopWatch.Stop();
            meta_data.sequence_filter_time = stopWatch.ElapsedMilliseconds - meta_data.path_time - meta_data.graph_time - meta_data.pre_time;
            meta_data.sequences = 0;//sequences.Count;
            meta_data.total_time = stopWatch.ElapsedMilliseconds;

            /* Console.WriteLine("-- Sequences --");

            // Find the maximum length (for testing purposes)
            //List<int> lengths = sequences.Select(seq => (int) seq.Count()).ToList();
            //int max_length = lengths.Max();
            //int max_index = lengths.IndexOf(max_length);
            //Console.WriteLine(AminoAcid.ArrayToString(sequences[max_index]));

            // Return all sequences
            foreach (AminoAcid[] sequence in sequences)
            {
                Console.WriteLine(AminoAcid.ArrayToString(sequence));
            } */
        }
        /// <summary> Outputs some information about the assembly the help validate the output of the assembly. </summary>
        public void OutputReport()
        {
            Console.WriteLine($"= General information =");
            Console.WriteLine($"Number of reads: {meta_data.reads}");
            Console.WriteLine($"K (length of k-mer): {kmer_length}");
            Console.WriteLine($"Minimum homology: {minimum_homology}");
            Console.WriteLine($"Number of k-mers: {meta_data.kmers}");
            Console.WriteLine($"Number of (k-1)-mers: {meta_data.kmin1_mers}");
            Console.WriteLine($"Number of sequences found: {meta_data.sequences}");
            if (graph == null)
            {
                Console.WriteLine("No graph build (yet)");
            }
            else
            {
                Console.WriteLine($"= Graph information =");
                Console.WriteLine($"Number of nodes: {graph.Length}");
                long number_edges = graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
                Console.WriteLine($"Number of edges: {number_edges}");
                Console.WriteLine($"Mean Connectivity: {number_edges / graph.Length}");
                Console.WriteLine($"Highest Connectivity: {graph.Aggregate(0L, (a, b) => (a > b.EdgesCount()) ? a : b.EdgesCount()) / 2L}");
                Console.WriteLine($"= Runtime information =");
                Console.WriteLine($"The program took {meta_data.total_time} ms to assemble the sequence");
                Console.WriteLine("With this breakup of times");
                Console.WriteLine($"  {meta_data.pre_time} ms for pre work (creating k-mers and k-1-mers)");
                Console.WriteLine($"  {meta_data.graph_time} ms for linking the graph");
                Console.WriteLine($"  {meta_data.path_time} ms for finding the paths through the graph");
                Console.WriteLine($"  {meta_data.sequence_filter_time} ms for filtering the sequences to only keep the usefull");
            }
        }
    }
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    static class HelperFunctionality
    {
        /// <summary> To copy a subarray to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created subarray. </param>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}