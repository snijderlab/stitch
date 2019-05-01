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
            int testnumber = 7;
            var test = new Assembler(6,5);
            //test.SetAlphabet({('L', 'I', 1, false), ('K', 'Q', 1, true)});
            test.OpenReads($"examples/{testnumber:D3}/reads.txt");
            Console.WriteLine("Now starting on the assembly");
            test.Assemble();
            test.OutputGraph($"examples/{testnumber:D3}/graph.dot");
            test.OutputGraph($"examples/{testnumber:D3}/simplegraph.dot", Assembler.Mode.Simple);
            test.CreateReport($"examples/{testnumber:D3}/report.html");
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
            /// <summary> Highest score yet for forward edges, used in filtering only the highest edges. </summary>
            private int max_forward_score;
            /// <summary> Highest score yet for backward edges, used in filtering only the highest edges. </summary>
            private int max_backward_score;
            /// <summary> The limit to include edges when filtering on highest edges. It will be used to include 
            /// not only the highest but (depending on the value) a couple more edges. </summary>
            private int edge_include_limit;

            /// <summary> The creator of Nodes. </summary>
            /// <param name="seq"> The sequence of this Node. </param>
            /// <param name="origin"> The origin(s) of this (k-1)-mer. </param>
            /// <param name="edge_include_limit_input"> The limit to include edges when filtering. </param>
            /// <remarks> It will initialize the edges list. </remarks>
            public Node(AminoAcid[] seq, List<int> origin, int edge_include_limit_input)
            {
                sequence = seq;
                origins = origin;
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

            var kmers = new List<(AminoAcid[], int)>();
            AminoAcid[] read;

            for (int r = 0; r < reads.Count(); r++)
            {
                read = reads[r];
                if (read.Length > kmer_length)
                {
                    for (int i = 0; i < read.Length - kmer_length + 1; i++)
                    {
                        AminoAcid[] kmer = read.SubArray(i, kmer_length);
                        kmers.Add((kmer, r));
                        kmers.Add((kmer.Reverse().ToArray(), r)); //Also add the reverse
                    }
                }
                else if (read.Length == kmer_length)
                {
                    kmers.Add((read, r));
                    kmers.Add((read.Reverse().ToArray(), r)); //Also add the reverse
                }
                else
                {
                    Console.WriteLine($"A read is not long enough: {AminoAcid.ArrayToString(read)}");
                }
            }
            meta_data.kmers = kmers.Count;

            // Building the graph
            // Generating all (k-1)-mers

            var kmin1_mers_raw = new List<(AminoAcid[], int)>();

            kmers.ForEach(kmer =>
            {
                kmin1_mers_raw.Add((kmer.Item1.SubArray(0, kmer_length - 1), kmer.Item2));
                kmin1_mers_raw.Add((kmer.Item1.SubArray(1, kmer_length - 1), kmer.Item2));
            });

            meta_data.kmin1_mers_raw = kmin1_mers_raw.Count;

            var kmin1_mers = new List<ValueTuple<AminoAcid[], List<int>>>();

            foreach (var kmin1_mer in kmin1_mers_raw) {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++) {
                    if (AminoAcid.ArrayEquals(kmin1_mer.Item1, kmin1_mers[i].Item1)) {
                        kmin1_mers[i].Item2.Add(kmin1_mer.Item2); // Update multiplicity
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) kmin1_mers.Add((kmin1_mer.Item1, new List<int>(kmin1_mer.Item2))); 
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

                prefix = kmer.Item1.SubArray(0, kmer_length - 1);
                postfix = kmer.Item1.SubArray(1, kmer_length - 1);

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

                    HashSet<int> origins = new HashSet<int>(start_node.Origins);
                    
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
                    }
                    backward_sequence.Add(backward_node.Sequence.ElementAt(0));
                    backward_node.Visited = true;
                    foreach (int o in backward_node.Origins) origins.Add(o);

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

                    List<int> originslist = origins.ToList();
                    originslist.Sort();
                    condensed_graph.Add(new CondensedNode(backward_sequence, i, forward_node_index, backward_node_index, forward_nodes, backward_nodes, originslist));
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
                    //TODO commented this out because test 5 gave empty sequences, need to find that cause
                    //node.Sequence = node.Sequence.Skip(kmer_length - 1).ToList();
                }
                if (node.ForwardEdges.Where(a => a != node_index).Count() == 1) {
                    //TODO commented this out because test 5 gave empty sequences, need to find that cause
                    //node.Sequence = node.Sequence.Take(node.Sequence.Count() - kmer_length + 1).ToList();
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
        /// <summary> An enum to input the type to generate. </summary>
        public enum Mode {
            /// <summary> Will generate with extended information, can become a bit cluttered with large datasets. </summary>
            Extended,
            /// <summary> Will generate with condesed information, will give easier overview but details have to be looked up. </summary>
            Simple}
        /// <summary> Creates a dot file and uses it in graphviz to generate a nice plot. </summary>
        /// <param name="filename"> The file to output to. </param>
        /// <param name="mode"> The mode to use to generate the graph. </param>
        public void OutputGraph(string filename = "graph.dot", Mode mode = Mode.Extended) {
            // Generate a dot file to use in graphviz
            var buffer = new StringBuilder();

            buffer.AppendLine("digraph {\n\tnode [fontname=\"Roboto\", shape=cds, fontcolor=\"blue\", color=\"blue\"];\n\tgraph [rankdir=\"LR\"];\n\t edge [arrowhead=vee, color=\"blue\"];\n");
            string label, style;

            for (int i = 0; i < condensed_graph.Count(); i++) {
                if (mode == Mode.Extended) label = AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray());
                else label = GetCondensedNodeLink(i);
                if (condensed_graph[i].BackwardEdges.Count() == 0) style = ", style=filled, fillcolor=\"blue\", fontcolor=\"white\"";
                else style = "";
                buffer.AppendLine($"\ti{i} [label=\"{label}\"{style}]");

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
        public string CreateReadsTable() 
        {
             var buffer = new StringBuilder();

            buffer.AppendLine(@"<table id=""reads-table"">
<tr>
    <th onclick=""sortTable('reads-table', 0, 'id')"">Identifier</th>
    <th onclick=""sortTable('reads-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('reads-table', 2, 'number')"">Sequence Length</th>
</tr>");
            string id;

            for (int i = 0; i < reads.Count(); i++) {
                id = $"R{i:D4}";
                buffer.AppendLine($@"<tr>
    <td><a href=""#{id}"">{id}</a></td>
    <td class=""seq"">{AminoAcid.ArrayToString(reads[i])}</td>
    <td>{AminoAcid.ArrayToString(reads[i]).Count()}</td>
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

            buffer.AppendLine(@"<table id=""contigs-table"">
<tr>
    <th onclick=""sortTable('contigs-table', 0, 'id')"">Identifier</th>
    <th onclick=""sortTable('contigs-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('contigs-table', 2, 'number')"">Length</th>
    <th onclick=""sortTable('contigs-table', 3, 'string')"">Forks to</th>
    <th onclick=""sortTable('contigs-table', 4, 'string')"">Forks from</th>
    <th onclick=""sortTable('contigs-table', 5, 'string')"">Based on</th>
</tr>");
            string id;

            for (int i = 0; i < condensed_graph.Count(); i++) {
                id = GetCondensedNodeLink(i);
                buffer.AppendLine($@"<tr>
    <td>{id}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}</td>
    <td>{condensed_graph[i].Sequence.Count()}</td>
    <td>{condensed_graph[i].ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
    <td>{condensed_graph[i].BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
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
                buffer.AppendLine($@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig: {id}</h1>
    <h2>Sequence</h2>
    <p>{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}</p>
    <h2>Sequence Length</h2>
    <p>{condensed_graph[i].Sequence.Count()}</p>
    <h2>Based on</h2>
    <p>{condensed_graph[i].Origins.Aggregate<int, string>("", (a, b) => a + " " + GetReadLink(b))}</p>
    <h2>Reads Alignment</h4>
    <pre>Not implemented yet</pre>
</div>");
            }
            for (int i = 0; i < reads.Count(); i++) {
                id = $"R{i:D4}";
                buffer.AppendLine($@"<div id=""{id}"" class=""info-block read-info"">
    <h1>Read: {id}</h1>
    <h2>Sequence</h2>
    <p>{AminoAcid.ArrayToString(reads[i])}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(reads[i]).Count()}</p>
</div>");
            }

            return buffer.ToString();
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
<html>
<body>
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
<tr><td>Mean Connectivity</td><td>{(double) number_edges / graph.Length}</td></tr>
<tr><td>Highest Connectivity</td><td>{graph.Aggregate(0D, (a, b) => (a > b.EdgesCount()) ? (double) a : (double) b.EdgesCount()) / 2D}</td></tr>
</table>

<h3>Condensed Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{condensed_graph.Count()}</td></tr>
<tr><td>Number of edges</td><td>{number_edges_condensed}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double) number_edges_condensed / condensed_graph.Count()}</td></tr>
<tr><td>Highest Connectivity</td><td>{condensed_graph.Aggregate(0D, (a, b) => (a > b.ForwardEdges.Count() + b.BackwardEdges.Count()) ? a : (double) b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2D}</td></tr>
</table>

<h3>Runtime information</h3>
<table>
<tr><td>Total time</td><td>{meta_data.total_time} ms</td></tr>
<tr><td>Pre work</td><td>{meta_data.pre_time} ms</td></tr>
<tr><td>Linking graph</td><td>{meta_data.graph_time} ms</td></tr>
<tr><td>Finding paths</td><td>{meta_data.path_time} ms</td></tr>
<tr><td>Filtering sequences</td><td>{meta_data.sequence_filter_time} ms</td></tr>
</table>
</body>
</html>";

            return html;
        }
        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        /// <param name="filename"> The path / filename to store the report in and where to find the graph.svg </param>
        public void CreateReport(string filename = "report.html") {
            string graphpath = Path.GetDirectoryName(Path.GetFullPath(filename)).ToString() + "\\graph.svg";
            string svg = "<p>Graph not found, searched at:" + graphpath + "</p>";
            if (File.Exists(graphpath)) svg = File.ReadAllText(graphpath);

            // Could also be done as an img, but that is much less nice
            // <img src='graph.png' alt='Extended graph of the results' srcset='graph.svg'>

            string simplegraphpath = Path.GetDirectoryName(Path.GetFullPath(filename)).ToString() + "\\simplegraph.svg";
            string simplesvg = "<p>Simple graph not found, searched at:" + simplegraphpath + "</p>";
            if (File.Exists(simplegraphpath)) simplesvg = File.ReadAllText(simplegraphpath);

            string stylesheet = "/* Could not find the stylesheet */";
            if (File.Exists("styles.css")) stylesheet = File.ReadAllText("styles.css");

            string script = "// Could not find the script";
            if (File.Exists("script.js")) script = File.ReadAllText("script.js");

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string html = $@"<html>
<head>
<title>Report Protein Sequence Run</title>
<style>
{stylesheet}
</style>
<script>
{script}
</script>
</head>
<body>
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>
<h2>Graph</h2>
{svg}
<h2>Simplified graph</h2>
{simplesvg}
<h2>Table</h2>
{CreateContigsTable()}
<h2>Reads Table</h2>
{CreateReadsTable()}
<h2>Meta Information<h2>
{HTMLMetaInformation()}
</div>
<div class=""aside"">
{CreateAsides()}
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
    }
}