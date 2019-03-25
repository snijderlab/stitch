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
        /// <value> The lengh of the chunks used to create the De Bruijn graph. </value>
        private int chunk_length;
        public int Chunk_length
        {
            get { return chunk_length; }
            set { chunk_length = value; }
        }
        private int[,] scoring_matrix;
        private char[] alphabet;
        private int minimum_homology;
        public int Minimum_homology
        {
            get { return minimum_homology; }
            set { minimum_homology = value; }
        }
        // A struct to function as a wrapper for AminoAcid information, so custom alphabets can be used in an efficiënt way
        private struct AminoAcid
        {
            // The Assembler used to create the AminoAcd, used to get the information of the alphabet
            private Assembler parent;
            // The code (index of the char in the alpabet array of the parent)
            private int code;
            public int Code
            {
                get
                {
                    return code;
                }
            }

            // The creator 
            public AminoAcid(Assembler asm, char input)
            {
                parent = asm;
                code = asm.getIndexInAlphabet(input);
            }
            // Some default functions to integrate normal behaviour
            public override string ToString()
            {
                return parent.alphabet[code].ToString();
            }
            public static string ArrayToString(AminoAcid[] array)
            {
                var builder = new StringBuilder();
                foreach (AminoAcid aa in array)
                {
                    builder.Append(aa.ToString());
                }
                return builder.ToString();
            }
            public override bool Equals(object obj)
            {
                return obj is AminoAcid && this == (AminoAcid)obj;
            }
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
            public static bool operator ==(AminoAcid x, AminoAcid y)
            {
                return x.Code == y.Code;
            }
            public static bool operator !=(AminoAcid x, AminoAcid y)
            {
                return !(x == y);
            }
            public override int GetHashCode()
            {
                return this.code.GetHashCode();
            }
            // Calculating homology, using the scoring matrix of the parent Assembler
            public int Homology(AminoAcid right)
            {
                return parent.scoring_matrix[this.Code, right.Code];
            }
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
        // A struct for Nodes in the graph
        private struct Node
        {
            // Members and poperties to hold the information of the Node
            private AminoAcid[] sequence;
            public AminoAcid[] Sequence { get { return sequence; } }
            private int multiplicity;
            public int Multiplicity { get { return multiplicity; } }
            private List<ValueTuple<int, int, int>> edges;

            // The creator
            public Node(AminoAcid[] seq, int multi)
            {
                sequence = seq;
                multiplicity = multi;
                edges = new List<ValueTuple<int, int, int>>();
            }

            // To visit the node to keep track how many times it was visited
            public void Visit()
            {
                multiplicity -= 1;
            }

            // Methods to interact with the edges
            public void AddEdge(int target, int score1, int score2)
            {
                edges.Add((target, score1, score2));
            }
            public ValueTuple<int, int, int> EdgeAt(int i)
            {
                return edges[i];
            }
            public bool HasEdges()
            {
                return edges.Count > 0;
            }
            public int EdgesCount()
            {
                return edges.Count;
            }
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
        // The creator, to set up the default values
        public Assembler(int chunk_length_input = 5, int minimum_homology_input = 3)
        {
            // Sest the chunk length and sets the alpabet usng the defaults

            chunk_length = chunk_length_input;
            minimum_homology = minimum_homology_input;

            SetAlphabet();
        }
        // Find the index of the given character in the alphabet
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
        // Set the alphabet of the assembler
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
        // To open a file with reads (should always be run before trying to assemble)
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
        // Assemble the reads into the graph, this is logically (one of) the last metods to run on an Assembler, all settings should be defined before running this.
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
        // Outputs some information about the graph the help validate the output of the graph
        public void GraphInfo() {
            if (graph == null) {
                Console.WriteLine("No graph build (yet)");
                return;
            }
            Console.WriteLine($"Number of nodes: {graph.Length}");
            Console.WriteLine($"Number of edges: {graph.Aggregate(0.0, (a, b) => a + b.EdgesCount())}");
            Console.WriteLine($"Mean Connectivity: {graph.Aggregate(0.0, (a, b) => a + b.EdgesCount()) / graph.Length}");
            Console.WriteLine($"Highest Connectivity: {graph.Aggregate(0.0, (a, b) => (a > b.EdgesCount()) ? a : b.EdgesCount() )}");
        }
    }
    // A class to store exension methods to help in the process of coding
    static class HelperFunctionality
    {
        // To copy a subarray to a new array
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}