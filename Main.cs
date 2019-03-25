using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyNameSpace
{
    class ToRunWithCommandLine
    {
        static void Main()
        {
            new Assembly();
        }
    }
    public class Assembly
    {
        public Assembly(string input_file = "examples/001/reads.txt", int chunk_length = 5)
        {

            // Getting input
            // For now just use a minimal implementation, reads separated b whitespace

            if (!File.Exists(input_file))
                throw new Exception("The specified file does not exist, file asked for: " + input_file);

            List<string> lines = File.ReadLines(input_file).ToList();
            List<string> reads = new List<string>();

            lines.ForEach(line =>
            {
                if (line[0] != '#')
                    reads.AddRange(line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            });

            Console.WriteLine($"Number of reads: {reads.Count}");

            // Generate all chunks
            // All chunks of length (chunk_length)

            var chunks = new List<string>();

            reads.ForEach(read =>
            {
                if (read.Length > chunk_length)
                {
                    for (int i = 0; i < read.Length - chunk_length; i++)
                    {
                        string chunk = read.Substring(i, chunk_length);
                        chunks.Add(chunk);
                        chunks.Add(Reverse(chunk)); //Also add the reverse
                    }
                }
                else if (read.Length == chunk_length)
                {
                    chunks.Add(read);
                }
                else
                {
                    Console.WriteLine($"A read is no long enough: {read}");
                }
            });

            Console.WriteLine($"Number of chunks: {chunks.Count}");

            // Building the graph
            // Generating all overlaps

            var overlaps_raw = new List<string>();

            chunks.ForEach(chunk =>
            {
                overlaps_raw.Add(chunk.Substring(0, chunk_length - 1));
                overlaps_raw.Add(chunk.Substring(1, chunk_length - 1));
            });

            var overlaps = new List<ValueTuple<string, int>>();

            overlaps_raw.GroupBy(i => i).ToList().ForEach(overlap =>
            {
                overlaps.Add((overlap.Key, overlaps.Count()));
            });

            Console.WriteLine($"Number of overlaps: {overlaps.Count}");

            // Create a node for every possible overlap (one amino acid shifted)

            // Implement the graph as a adjecency list (aray)
            var graph = new ValueTuple<string, int, List<int>>[overlaps.Count];

            int index = 0;
            overlaps.ForEach(overlap =>
            {
                graph[index] = (overlap.Item1, overlap.Item2, new List<int>());
                index++;
            });

            // Connect the nodes based on the chunks

            chunks.ForEach(chunk =>
            {
                for (int i = 0; i < graph.Length; i++)
                {
                    if (graph[i].Item1 == chunk.Substring(0, chunk_length - 1))
                    {
                        for (int j = 0; j < graph.Length; j++)
                        {
                            if (i != j && graph[j].Item1 == chunk.Substring(1, chunk_length - 1))
                            {
                                graph[i].Item3.Add(j);
                            }
                        }
                    }
                }
            });

            Console.WriteLine("Build graph");

            // Finding paths

            var sequences = new HashSet<string>();


            // Try for every node to walk as far as possible to find the seqence
            for (int i = 0; i < graph.Length; i++)
            {
                var current_node = graph[i];

                if (current_node.Item2 > 0)
                {
                    string sequence = current_node.Item1.Substring(0, chunk_length - 1);

                    while (current_node.Item2 > 0)
                    {
                        sequence += current_node.Item1.Substring(chunk_length - 2);
                        current_node.Item2 -= 1;
                        if (current_node.Item3.Count > 0)
                        {
                            current_node = graph[current_node.Item3[0]];
                        }
                    }

                    sequences.Add(sequence);
                }
            }

            // Returning output

            Console.WriteLine("-- Sequences --");
            foreach (string sequence in sequences)
            {
                Console.WriteLine(sequence);
            }
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}