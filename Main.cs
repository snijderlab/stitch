using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyNameSpace
{
    class ToRunWithCommandLine
    {
        static void Main()
        {
            var test = new Assembly();
            test.OpenReads();
            Console.WriteLine("Now starting on the assembly");
            test.Assemble();
        }
    }
    public class Assembly
    {
        List<AminoAcid[]> reads = new List<AminoAcid[]>();
        private int chunk_length;
        public int Chunk_length
        {
            get { return chunk_length; }
            set { chunk_length = value; }
        }
        private int[,] scoring_matrix;
        private char[] alphabet;

        private struct AminoAcid
        {
            private Assembly parent;
            private int code;
            public int Code
            {
                get
                {
                    return code;
                }
            }
            public AminoAcid(Assembly asm, char input)
            {
                parent = asm;
                code = asm.getIndexInAlphabet(input);
            }
            public override string ToString()
            {
                return parent.alphabet[code].ToString();
            }
            public override bool Equals(object obj)
            {
                return obj is AminoAcid && this == (AminoAcid)obj;
            }
            public override int GetHashCode()
            {
                return this.code.GetHashCode();
            }
            public static bool operator ==(AminoAcid x, AminoAcid y)
            {
                return x.Code == y.Code;
            }
            public static bool operator !=(AminoAcid x, AminoAcid y)
            {
                return !(x == y);
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
        }
        private struct Node
        {
            private AminoAcid[] sequence;
            public AminoAcid[] Sequence { get { return sequence; } }
            private int multiplicity;
            public int Multiplicity { get { return multiplicity; } }
            private List<int> edges;
            public Node(AminoAcid[] seq, int multi)
            {
                sequence = seq;
                multiplicity = multi;
                edges = new List<int>();
            }
            public void AddEdge(int target)
            {
                edges.Add(target);
            }
            public int EdgeAt(int i)
            {
                return edges[i];
            }
            public void CheckOut()
            {
                multiplicity -= 1;
            }
            public bool HasEdges()
            {
                return edges.Count > 0;
            }
        }
        public Assembly(int chunk_length_input = 5)
        {
            // Sest the chunk length and sets the alpabet usng the defaults

            if (chunk_length_input > 0)
                chunk_length = chunk_length_input;

            SetAlphabet();
        }
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
        public void SetAlphabet(List<ValueTuple<char, char, int, bool>> rules = null, int diagonals_value = 1, string input = "LSAEGVRKTPDINQFYHMCWOU" )
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
            var graph = new Node[overlaps.Count];

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
                    if (AminoAcid.ArrayEquals(graph[i].Sequence, chunk.SubArray(0, chunk_length - 1)))
                    {
                        for (int j = 0; j < graph.Length; j++)
                        {
                            if (i != j && AminoAcid.ArrayEquals(graph[j].Sequence, chunk.SubArray(1, chunk_length - 1)))
                            {
                                graph[i].AddEdge(j);
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
                        sequence.Add(current_node.Sequence.ElementAt(chunk_length - 2));
                        current_node.CheckOut();
                        if (current_node.HasEdges())
                        {
                            current_node = graph[current_node.EdgeAt(0)];
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
    }
    static class HelperFunctionality
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}