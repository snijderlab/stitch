using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
        /// <summary> The method that will be run if the code is run from the command line. 
        /// This exists because the code needs to be tested. </summary>
        static void Main()
        {
            var test = new Assembler(8, 6);
            //test.SetAlphabet({('L', 'I', 1, false), ('K', 'Q', 1, true)});
            test.OpenReads("examples/005/reads.txt");
            Console.WriteLine("Now starting on the assembly");
            test.Assemble();
            test.OutputGraph("examples/005/graph.dot");
            test.OutputReport();
        }
    }
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <summary> The reads fed into the Assembler, as opened by OpenReads. </summary>
        List<AminoAcid[]> reads = new List<AminoAcid[]>();
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
            set { kmer_length = value; }
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
            set { minimum_homology = value; }
        }
        /// <summary> The limit to include edges when filtering on highest edges. It will be used to include 
        /// not only the highest but (depending on the value) a couple more edges. </summary>
        private int edge_include_limit;
        /// <summary> To contain meta information about how the program ran to make informed decisions on 
        /// how to choose the values of variables and to aid in debugging. </summary>
        private MetaInformation meta_data;
        /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can 
        /// be used in an efficient way </summary>
        private struct AminoAcid
        {
            /// <summary> The Assembler used to create the AminoAcd, used to get the information of the alphabet. </summary>
            private Assembler parent;
            /// <summary> The code (index of the char in the alpabet array of the parent). </summary>
            private int code;
            /// <summary> The code (index of the char in the alpabet array of the parent). Gives only a Get option. 
            /// The only way to change it is in the creator. </summary>
            /// <value> The code of this AminoAcid. </value>
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
            /// <returns> Returns the character of this AminoAcid (based on the alphabet) as a string. </returns>
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
                return this.code.GetHashCode();
            }
            /// <summary> Calculating homology, using the scoring matrix of the parent Assembler. </summary>
            /// <remarks> Depending on which rules are put into the scoring matrix the order in which this 
            /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to 
            /// <c>b.Homology(a)</c>. </remarks>
            /// <param name="right"> The other AminoAcid to use. </param>
            /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
            /// See <see cref="Assembler.scoring_matrix"/> for the scoring matrix.
            /// See <see cref="Assembler.SetAlphabet"/> on how to change the scoring matrix.
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
            /// <returns> Returns the homology bewteen the two aminoacid arrays. </returns>
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
            /// <summary> The member to store the sequence information in. </summary>
            private AminoAcid[] sequence;
            /// <summary> The sequence of the Node. Only has a getter. </summary>
            /// <value> The sequence of this node. </value>
            public AminoAcid[] Sequence { get { return sequence; } }
            /// <summary> The member to store the multiplicity (amount of k-mers which 
            /// result in the same (k-1)-mers in. </summary>
            private int multiplicity;
            /// <summary> The multiplicity, amount of k-mers which 
            /// result in the same (k-1)-mers, of the Node. Only has a getter. </summary>
            /// <value> The amount of time an equal (k-1)-mer was found in the set of 
            /// generated k-mers, this tells something about the amount of reads that 
            /// have the overlap over this sequence, and about the amount of equal
            /// parts of the sequence in other places of the protein. </value>
            public int Multiplicity { get { return multiplicity; } }
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
            /// <summary> Highest score yet for forward edges, used in filtering only the highest edges. </summary>
            private int max_forward_score;
            /// <summary> Highest score yet for backward edges, used in filtering only the highest edges. </summary>
            private int max_backward_score;
            /// <summary> The limit to include edges when filtering on highest edges. It will be used to include 
            /// not only the highest but (depending on the value) a couple more edges. </summary>
            private int edge_include_limit;

            /// <summary> The creator of Nodes. </summary>
            /// <param name="seq"> The sequence of this Node. </param>
            /// <param name="multi"> The multiplicity of this Node. </param>
            /// <param name="edge_include_limit_input"> The limit to include edges when filtering. </param>
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
                if (score > max_backward_score)
                {
                    max_backward_score = score;
                    backwardEdges.Add((target, score1, score2));
                    filterBackwardEdges();
                }
            }
            /// <summary> Filters the forward edges based on the highest score found yet and the edge include limit. </summary>
            /// See <see cref="Assembler.edge_include_limit"/> for the edge include limit.
            /// See <see cref="Node.max_forward_score"/> for the highest score.
            private void filterForwardEdges()
            {
                //Console.Write($"Filtered forward edges from {forwardEdges.Count} to ");
                forwardEdges = forwardEdges.Where(i => i.Item2 + i.Item3 >= max_forward_score - edge_include_limit).ToList();
                //Console.Write($"{forwardEdges.Count}.");
            }
            /// <summary> Filters the backward edges based on the highest score found yet and the edge include limit. </summary>
            /// See <see cref="Assembler.edge_include_limit"/> for the edge include limit.
            /// See <see cref="Node.max_backward_score"/> for the highest score.
            private void filterBackwardEdges()
            {
                //Console.Write($"Filtered forward edges from {backwardEdges.Count} to ");
                backwardEdges = backwardEdges.Where(i => i.Item2 + i.Item3 >= max_backward_score - edge_include_limit).ToList();
                //Console.Write($"{backwardEdges.Count}.");
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
            /// <summary> The index this node. The index is defined as the index in the adjecency list of the de Bruijn graph. </summary>
            public int Index;
            public int ForwardIndex;
            public int BackwardIndex;
            /// <summary> Whether or not this node is visited yet. </summary>
            public bool Visited;
            /// <summary> The sequence of this node. It is the longest constant sequence to be 
            /// found in the de Bruijn graph starting at the Index. See <see cref="CondensedNode.Index"/></summary>
            public List<AminoAcid> Sequence;
            /// <summary> The list of forward edges, defined as the indexes in the de Bruijn graph. </summary>
            public List<int> ForwardEdges;
            /// <summary> The list of backward edges, defined as the indexes in the de Bruijn graph. </summary>
            public List<int> BackwardEdges;
            /// <summary> Creates a condensed node to be used in the condensed graph. </summary>
            /// <param name="sequence"> The sequence of this node. See <see cref="CondensedNode.Sequence"/></param>
            /// <param name="index"> The index of the node, the index in the de Bruijn graph. See <see cref="CondensedNode.Index"/></param>
            /// <param name="forward_edges"> The forward edges from this node (indexes). See <see cref="CondensedNode.ForwardEdges"/></param>
            /// <param name="backward_edges"> The backward edges from this node (indexes). See <see cref="CondensedNode.BackwardEdges"/></param>
            public CondensedNode(List<AminoAcid> sequence, int index, int forward_index, int backward_index, List<int> forward_edges, List<int> backward_edges)
            {
                Sequence = sequence;
                Index = index;
                ForwardIndex = forward_index;
                BackwardIndex = backward_index;
                ForwardEdges = forward_edges;
                BackwardEdges = backward_edges;
                Visited = false;
            }
        }

        /// <summary> The creator, to set up the default values. Also sets the standard alphabet. </summary>
        /// <param name="kmer_length_input"> The lengths of the k-mers. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        /// <param name="edge_include_limit_input"> The limit to include edges when filtering. </param>
        public Assembler(int kmer_length_input, int minimum_homology_input, int edge_include_limit_input = 0)
        {
            kmer_length = kmer_length_input;
            minimum_homology = minimum_homology_input;
            edge_include_limit = edge_include_limit_input;

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

            meta_data.kmin1_mers_raw = kmin1_mers_raw.Count;

            var kmin1_mers = new List<ValueTuple<AminoAcid[], int>>();

            foreach (var kmin1_mer in kmin1_mers_raw) {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++) {
                    if (AminoAcid.ArrayEquals(kmin1_mer, kmin1_mers[i].Item1)) {
                        kmin1_mers[i] = (kmin1_mers[i].Item1, kmin1_mers[i].Item2 + 1); // Update multiplicity
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) kmin1_mers.Add((kmin1_mer, 1)); 
            }
            
            meta_data.kmin1_mers = kmin1_mers.Count;

            // Create a node for every possible (k-1)-mer

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
                    prefix_matches[i] = AminoAcid.ArrayHomology(graph[i].Sequence, prefix);
                    postfix_matches[i] = AminoAcid.ArrayHomology(graph[i].Sequence, postfix);
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
                                graph[i].AddForwardEdge(j, prefix_matches[j], postfix_matches[j]);
                                graph[j].AddBackwardEdge(i, prefix_matches[i], postfix_matches[i]);
                            }
                        }
                    }
                }
            });

            meta_data.graph_time = stopWatch.ElapsedMilliseconds - meta_data.pre_time;
            Console.WriteLine($"Built graph");

            // Print graph

            for (int i = 0; i < graph.Length; i++) {
                var node = graph[i];
                Console.WriteLine($"Node: Seq: {AminoAcid.ArrayToString(node.Sequence)} Index: {i} Forward edges: {node.ForwardEdges.Count()} {node.ForwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())} Backward edges: {node.BackwardEdges.Count()} {node.BackwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");
            }

            // Create a condensed graph to keep the information

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
                    
                    // Debug purposes can be deleted later
                    int forward_node_index = i;
                    int backward_node_index = i;

                    // Walk forwards until a multifurcartion in the path is found or the end is reached
                    while (forward_node.ForwardEdges.Count == 1 && forward_node.BackwardEdges.Count <= 1)
                    {
                        forward_node.Visited = true;
                        forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2)); // Last amino acid of the sequence
                        forward_node_index = forward_node.ForwardEdges[0].Item1;
                        forward_node = graph[forward_node_index];
                    } 
                    forward_sequence.Add(forward_node.Sequence.ElementAt(kmer_length - 2));
                    forward_node.Visited = true;

                    if (forward_node.ForwardEdges.Count() > 0) {
                        forward_nodes = (from node in forward_node.ForwardEdges select node.Item1).ToList();
                    }                  

                    // Walk backwards
                    while (backward_node.ForwardEdges.Count <= 1 && backward_node.BackwardEdges.Count == 1)
                    {
                        backward_node.Visited = true;
                        backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                        backward_node_index = backward_node.BackwardEdges[0].Item1;
                        backward_node = graph[backward_node_index];
                    }
                    backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                    backward_node.Visited = true;

                    if (backward_node.BackwardEdges.Count() > 0) {
                        backward_nodes = (from node in backward_node.BackwardEdges select node.Item1).ToList();
                    }  

                    Console.WriteLine($"\n==Sequences:\nBackward: {AminoAcid.ArrayToString(backward_sequence.ToArray())} last element {AminoAcid.ArrayToString(backward_node.Sequence)}\nForward: {AminoAcid.ArrayToString(forward_sequence.ToArray())} last element {AminoAcid.ArrayToString(forward_node.Sequence)}\nStart node: {AminoAcid.ArrayToString(start_node.Sequence)}");

                    // Build the final sequence
                    backward_sequence.Reverse();
                    backward_sequence.AddRange(start_node.Sequence.SubArray(1, kmer_length - 3));
                    backward_sequence.AddRange(forward_sequence);

                    Console.WriteLine($"Result: {AminoAcid.ArrayToString(backward_sequence.ToArray())}");
                    Console.WriteLine($"Stopped because \nforward node {forward_node_index} had {forward_node.ForwardEdges.Count} forward edges and {forward_node.BackwardEdges.Count} backward edges\nbackward node {backward_node_index} had {backward_node.ForwardEdges.Count} forward edges and {backward_node.BackwardEdges.Count} backward edges");
                    Console.WriteLine($"Forward edges - forwards: {forward_node.ForwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");
                    Console.WriteLine($"Forward edges - backwards: {forward_node.BackwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");
                    Console.WriteLine($"Backward edges - forwards: {backward_node.ForwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");
                    Console.WriteLine($"Backward edges - backwards: {backward_node.BackwardEdges.Aggregate<(int, int, int), string>("", (a, b) => a + " " + b.ToString())}");

                    condensed_graph.Add(new CondensedNode(backward_sequence, i, forward_node_index, backward_node_index, forward_nodes, backward_nodes));
                }
            }

            // Update the condensed graph to point to elements in the condensed graph instead of to elements in the de Bruijn graph
            int node_index = 0;
            foreach (var node in condensed_graph) {
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
                if (node.BackwardEdges.Where(a => a != node_index).Count() == 1) {
                    node.Sequence = node.Sequence.Skip(kmer_length - 1).ToList();
                }
                if (node.ForwardEdges.Where(a => a != node_index).Count() == 1) {
                    node.Sequence = node.Sequence.Take(node.Sequence.Count() - kmer_length + 1).ToList();
                }
                node_index++;
            }

            // Print the condensed graph
            foreach (var node in condensed_graph) {
                Console.WriteLine($"Node: Seq: {AminoAcid.ArrayToString(node.Sequence.ToArray())} Index: {node.Index} FWIndex: {node.ForwardIndex} BWIndex: {node.BackwardIndex} Forward edges: {node.ForwardEdges.Count()} {node.ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + b.ToString())} Backward edges: {node.BackwardEdges.Count()} {node.BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + b.ToString())}");
            }

            meta_data.path_time = stopWatch.ElapsedMilliseconds - meta_data.graph_time - meta_data.pre_time;

            stopWatch.Stop();
            meta_data.sequence_filter_time = stopWatch.ElapsedMilliseconds - meta_data.path_time - meta_data.graph_time - meta_data.pre_time;
            meta_data.sequences = condensed_graph.Count();//sequences.Count;
            meta_data.total_time = stopWatch.ElapsedMilliseconds;
        }
        /// <summary> Creates a dot file to be used in graphviz to generate a nice plot. </summary>
        /// <param name="filename"> The file to output to. </param>
        public void OutputGraph(string filename = "graph.dot") {
            // Generate a dot file to use in graphviz
            var buffer = new StringBuilder();

            buffer.AppendLine("digraph {\n\tnode [fontname=\"Roboto\", shape=cds, fontcolor=\"blue\", color=\"blue\"];\n\tgraph [rankdir=\"LR\"];\n\t edge [arrowhead=vee, color=\"blue\"];\n");

            for (int i = 0; i < condensed_graph.Count(); i++) {
                if (condensed_graph[i].BackwardEdges.Count() > 0) {
                    buffer.AppendLine($"\ti{i} [label=\"" + AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray()) + "\"]");
                } else {
                    buffer.AppendLine($"\ti{i} [label=\"" + AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray()) + "\", style=filled, fillcolor=\"blue\", fontcolor=\"white\"]");
                }
                foreach (var fwe in condensed_graph[i].ForwardEdges) {
                    buffer.AppendLine($"\ti{i} -> i{fwe}");
                }
            }

            buffer.AppendLine("}");

            // Write .dot to a file
            
            StreamWriter sw = File.CreateText(filename);
            sw.Write(buffer.ToString());
            sw.Close();

            // Generate PNG and SVG files

            try
            {
                Console.WriteLine(Path.ChangeExtension(Path.GetFullPath(filename), "png"));
                Process.Start("dot", "-Tpng " + Path.GetFullPath(filename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(filename), "png") + "\"" );
                Process.Start("dot", "-Tsvg " + Path.GetFullPath(filename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(filename), "svg") + "\"" );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary> Outputs some information about the assembly the help validate the output of the assembly. </summary>
        public void OutputReport()
        {
            Console.WriteLine($"\n= General information =");
            Console.WriteLine($"Number of reads: {meta_data.reads}");
            Console.WriteLine($"K (length of k-mer): {kmer_length}");
            Console.WriteLine($"Minimum homology: {minimum_homology}");
            Console.WriteLine($"Number of k-mers: {meta_data.kmers}");
            Console.WriteLine($"Number of (k-1)-mers: {meta_data.kmin1_mers}");
            Console.WriteLine($"Number of duplicate (k-1)-mers: {meta_data.kmin1_mers_raw - meta_data.kmin1_mers}");
            Console.WriteLine($"Number of sequences found: {meta_data.sequences}");
            if (graph == null)
            {
                Console.WriteLine("No graph build (yet)");
            }
            else
            {
                Console.WriteLine($"\n= de Bruijn Graph information =");
                Console.WriteLine($"Number of nodes: {graph.Length}");
                long number_edges = graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
                Console.WriteLine($"Number of edges: {number_edges}");
                Console.WriteLine($"Mean Connectivity: {(double) number_edges / graph.Length}");
                Console.WriteLine($"Highest Connectivity: {graph.Aggregate(0D, (a, b) => (a > b.EdgesCount()) ? (double) a : (double) b.EdgesCount()) / 2D}");

                Console.WriteLine($"\n= Condensed Graph information =");
                Console.WriteLine($"Number of nodes: {condensed_graph.Count()}");
                number_edges = condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count() ) / 2L;
                Console.WriteLine($"Number of edges: {number_edges}");
                Console.WriteLine($"Mean Connectivity: {(double) number_edges / condensed_graph.Count()}");
                Console.WriteLine($"Highest Connectivity: {condensed_graph.Aggregate(0D, (a, b) => (a > b.ForwardEdges.Count() + b.BackwardEdges.Count()) ? a : (double) b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2D}");
                Console.WriteLine($"\n= Runtime information =");
                Console.WriteLine($"The program took {meta_data.total_time} ms to assemble the sequence");
                Console.WriteLine("With this breakup of times");
                Console.WriteLine($"  {meta_data.pre_time} ms for pre work (creating k-mers and k-1-mers)");
                Console.WriteLine($"  {meta_data.graph_time} ms for linking the graph");
                Console.WriteLine($"  {meta_data.path_time} ms for finding the paths through the graph");
                Console.WriteLine($"  {meta_data.sequence_filter_time} ms for filtering the sequences to only keep the useful");
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
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static IList<T> Clone<T>(this IList<T> listToClone) where T: ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
}