using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyNameSpace
{
    /// <summary> A Class to be able to run the code from the commandline. </summary>
    class ToRunWithCommandLine
    {
        static void Main()
        {
            var test = new Assembler();
            test.OpenReads();
            Console.WriteLine("Now starting on the assembly");
            test.Assemble();
            test.GraphInfo();
        }
    }
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <value> The reads fed into the Assembler, as opened by OpenReads. </value>
        List<AminoAcid[]> reads = new List<AminoAcid[]>();
        /// <value> The De Bruijn graph used by the Assembler. </value>
        Node[] graph;
        /// <value> The length of the chunks used to create the De Bruijn graph. Private member where it is stored. </value>
        private int chunk_length;
        /// <value> The length of the chunks used to create the De Bruijn graph. Get and Set is public. </value>
        public int Chunk_length
        {
            get { return chunk_length; }
            set { chunk_length = value; }
        }
        /// <value> The matrix used for scoring of the alignment between two characters in the alphabet. 
        /// As such this matrix is rectangular. </value>
        private int[,] scoring_matrix;
        /// <value> The alphabet used for alignment. The default value is all the amino acids in order of
        /// natural obundance in prokaryotes to make finding the right amino acid a little bit faster. </value>
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
            /// <remarks> Implemented as a shortcicuiting loop with the equals operator (==). </remarks>
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
        /// <summary> A struct for Nodes in the graph. </summary>
        private struct Node
        {
            /// <value> The member to store the sequence information in. </value>
            private AminoAcid[] sequence;
            /// <value> The sequence of the Node. Only has a getter. </value>
            public AminoAcid[] Sequence { get { return sequence; } }
            /// <value> The member to store the multiplicity (amount of chunks which 
            /// result in the same overlaps) in. </value>
            private int multiplicity;
            /// <value> The multiplicity, amount of chunks which 
            /// result in the same overlaps, of the Node. Only has a getter. </value>
            public int Multiplicity { get { return multiplicity; } }
            /// <value> The list of edges of this Node. The tuples contain the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. </value>
            private List<ValueTuple<int, int, int>> edges;

            /// <summary> The creator of Nodes. </summary>
            /// <param name="seq"> The sequence of this Node. </param>
            /// <param name="multi"> The multiplicity of this Node. </param>
            /// <remarks> It will initialize the edges list. </remarks>
            public Node(AminoAcid[] seq, int multi)
            {
                sequence = seq;
                multiplicity = multi;
                edges = new List<ValueTuple<int, int, int>>();
            }

            /// <summary> To visit the node to keep track how many times it was visited </summary>
            public void Visit()
            {
                // I should add a new member "visited" to count the visits and not remove this information.
                multiplicity -= 1;
            }

            /// <summary> To add an edge to the Node. </summary>
            /// <param name="target"> The index of the Node where this edge goes to. </param>
            /// <param name="score1"> The homology of the edge with the first Node. </param>
            /// <param name="score2"> The homology of the edge with the second Node. </param>
            public void AddEdge(int target, int score1, int score2)
            {
                edges.Add((target, score1, score2));
            }
            /// <summary> Retrieves the edge at the specified index. </summary>
            /// <param name="i"> The index. </param>
            /// <returns> It returns the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. </returns>
            public ValueTuple<int, int, int> EdgeAt(int i)
            {
                return edges[i];
            }
            /// <summary> To check if the Node has edges. </summary>
            public bool HasEdges()
            {
                return edges.Count > 0;
            }
            /// <summary> To get the amount of edges. </summary>
            /// <remarks> O(1) runtime </remarks>
            public int EdgesCount()
            {
                return edges.Count;
            }
            /// <summary> Gets the edge with the highest total homology of all 
            /// edges in this Node. </summary>
            /// <returns> It returns the index 
            /// of the Node where the edge goes to, the homology with the first Node 
            /// and the homology with the second Node in this order. </returns>
            /// <exception cref="Exception"> It will result in an Exception if the Node has no edges. </exception>
            public ValueTuple<int, int, int> MaxEdge()
            {
                if (!HasEdges())
                    throw new Exception("Cannot give an edge if this node has no edges");
                var output = edges[0];
                int value = output.Item2 + output.Item3;
                int max = value;
                for (int i = 1; i < edges.Count; i++)
                {
                    value = edges[i].Item2 + edges[i].Item3;
                    if (value > max)
                    {
                        max = value;
                        output = edges[i];
                    }
                }
                return output;
            }
        }
        /// <summary> The creator, to set up the default values. Also sets the alphabet. </summary>
        /// <param name="chunk_length_input"> The lengths of the chunks. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        public Assembler(int chunk_length_input = 5, int minimum_homology_input = 3)
        {
            chunk_length = chunk_length_input;
            minimum_homology = minimum_homology_input;

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
        ///  in the matrix is the same boths ways). </param>
        /// <param name="diagonals_value"> The value to place on the diagonals of the matrix. </param>
        /// <param name="input"> The alphabet to use, it will be iterated over from the front to the back so
        /// the best case scenario has the most used characters at the front of the string. </param>
        public void SetAlphabet(List<ValueTuple<char, char, int, bool>> rules = null, int diagonals_value = 1, string input = "LSAEGVRKTPDINQFYHMCWOU")
        {
            alphabet = input.ToCharArray();

            scoring_matrix = new int[alphabet.Length, alphabet.Length];

            // Only set the diagonals to te given value
            for (int i = 0; i < alphabet.Length; i++) scoring_matrix[i, i] = diagonals_value;
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
        public void OpenReads(string input_file = "examples/001/reads.txt")
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

            Console.WriteLine($"Number of reads: {reads_string.Count}");

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
        }
        /// <summary> Assemble the reads into the graph, this is logically (one of) the last metods to 
        /// run on an Assembler, all settings should be defined before running this. </summary>
        public void Assemble()
        {
            // Generate all chunks
            // All chunks of length (chunk_length)

            var chunks = new List<AminoAcid[]>();

            foreach (AminoAcid[] read in reads)
            {
                if (read.Length > chunk_length)
                {
                    for (int i = 0; i < read.Length - chunk_length; i++)
                    {
                        AminoAcid[] chunk = read.SubArray(i, chunk_length);
                        chunks.Add(chunk);
                        chunks.Add(chunk.Reverse().ToArray()); //Also add the reverse
                    }
                }
                else if (read.Length == chunk_length)
                {
                    chunks.Add(read);
                }
                else
                {
                    Console.WriteLine($"A read is no long enough: {AminoAcid.ArrayToString(read)}");
                }
            }

            Console.WriteLine($"Number of chunks: {chunks.Count}");

            // Building the graph
            // Generating all overlaps

            var overlaps_raw = new List<AminoAcid[]>();

            chunks.ForEach(chunk =>
            {
                overlaps_raw.Add(chunk.SubArray(0, chunk_length - 1));
                overlaps_raw.Add(chunk.SubArray(1, chunk_length - 1));
            });

            var overlaps = new List<ValueTuple<AminoAcid[], int>>();

            overlaps_raw.GroupBy(i => i).ToList().ForEach(overlap =>
            {
                overlaps.Add((overlap.Key, overlaps.Count()));
            });

            Console.WriteLine($"Number of overlaps: {overlaps.Count}");

            // Create a node for every possible overlap (one amino acid shifted)

            // Implement the graph as a adjecency list (aray)
            graph = new Node[overlaps.Count];

            int index = 0;
            overlaps.ForEach(overlap =>
            {
                graph[index] = new Node(overlap.Item1, overlap.Item2);
                index++;
            });

            // Connect the nodes based on the chunks

            chunks.ForEach(chunk =>
            {
                for (int i = 0; i < graph.Length; i++)
                {
                    int first_homology = AminoAcid.ArrayHomology(graph[i].Sequence, chunk.SubArray(0, chunk_length - 1));
                    if (first_homology > minimum_homology)
                    {
                        for (int j = 0; j < graph.Length; j++)
                        {
                            int second_homology = AminoAcid.ArrayHomology(graph[j].Sequence, chunk.SubArray(1, chunk_length - 1));
                            if (i != j && second_homology > minimum_homology)
                            {
                                Console.WriteLine("Add edge");
                                graph[i].AddEdge(j, first_homology, second_homology);
                            }
                        }
                    }
                }
            });

            Console.WriteLine("Built graph");

            // Finding paths

            var sequences = new List<AminoAcid[]>();


            // Try for every node to walk as far as possible to find the seqence
            for (int i = 0; i < graph.Length; i++)
            {
                var current_node = graph[i];

                if (current_node.Multiplicity > 0)
                {
                    List<AminoAcid> sequence = current_node.Sequence.SubArray(0, chunk_length - 2).ToList();

                    while (current_node.Multiplicity > 0)
                    {
                        //Console.WriteLine($"Found node with connectivity {current_node.EdgesCount()} maximum homoloy {(current_node.HasEdges() ? current_node.MaxEdge().ToString() : "none")}");

                        sequence.Add(current_node.Sequence.ElementAt(chunk_length - 2));
                        current_node.Visit();
                        if (current_node.HasEdges())
                        {
                            current_node = graph[current_node.MaxEdge().Item1];
                        }
                        else
                        {
                            break;
                        }
                    }

                    sequences.Add(sequence.ToArray());
                }
            }

            // Returning output

            Console.WriteLine("-- Sequences --");
            foreach (AminoAcid[] sequence in sequences)
            {
                Console.WriteLine(AminoAcid.ArrayToString(sequence));
            }
        }
        /// <summary> Outputs some information about the graph the help validate the output of the graph. </summary>
        public void GraphInfo()
        {
            if (graph == null)
            {
                Console.WriteLine("No graph build (yet)");
                return;
            }
            Console.WriteLine($"Number of nodes: {graph.Length}");
            Console.WriteLine($"Number of edges: {graph.Aggregate(0.0, (a, b) => a + b.EdgesCount())}");
            Console.WriteLine($"Mean Connectivity: {graph.Aggregate(0.0, (a, b) => a + b.EdgesCount()) / graph.Length}");
            Console.WriteLine($"Highest Connectivity: {graph.Aggregate(0.0, (a, b) => (a > b.EdgesCount()) ? a : b.EdgesCount())}");
        }
    }
    /// <summary> A class to store exension methods to help in the process of coding. </summary>
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