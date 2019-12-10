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
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <summary> The reads fed into the Assembler, as opened by OpenReads. </summary>
        public List<AminoAcid[]> reads = new List<AminoAcid[]>();
        /// <summary> The meta information as delivered by PEAKS. By definition every index in this list matches 
        /// with the index in reads. When the data was not imported via PEAKS this list is null.</summary>
        public List<MetaData.IMetaData> reads_metadata = null;
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
        /// <summary>
        /// The alphabet used
        /// </summary>
        public Alphabet alphabet;
        private bool reverse;
        /// <summary> The creator, to set up the default values. Also sets the standard alphabet. </summary>
        /// <param name="kmer_length_input"> The lengths of the k-mers. </param>
        /// <param name="minimum_homology_input"> The minimum homology needed to be inserted in the graph as an edge. <see cref="Minimum_homology"/> </param>
        /// <param name="duplicate_threshold_input"> The minimum homology score between two reads needed to be viewed as duplicates.</param>
        /// <param name="should_reverse"> To indicate if the assembler should include all reads in reverse or not.</param>
        /// <param name="alphabet_input"> The alphabet to be used.</param>
        public Assembler(int kmer_length_input, int duplicate_threshold_input, int minimum_homology_input, bool should_reverse, Alphabet alphabet_input)
        {
            kmer_length = kmer_length_input;
            minimum_homology = minimum_homology_input < 0 ? kmer_length - 1 : minimum_homology_input;
            duplicate_threshold = duplicate_threshold_input;
            reverse = should_reverse;
            meta_data = new MetaInformation();
            alphabet = alphabet_input;
        }
        /// <summary>
        /// Give a list of reads to the assembler
        /// </summary>
        public void GiveReads(List<(string, MetaData.IMetaData)> reads_i)
        {
            reads = reads_i.Select(x => StringToSequence(x.Item1)).ToList();
            reads_metadata = reads_i.Select(x => x.Item2).ToList();
            meta_data.reads = reads.Count();
        }
        /// <summary>
        /// Gets the sequence in AminoAcids from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alphabet, input[i]);
            }
            return output;
        }
        /// <summary> Assemble the reads into the graph, this is logically (one of) the last methods to 
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

            // Building the graph
            // Generating all (k-1)-mers

            var kmin1_mers_raw = new List<(AminoAcid[], int)>();

            kmers.ForEach(kmer =>
            {
                kmin1_mers_raw.Add((kmer.Item1.SubArray(0, kmer_length - 1), kmer.Item2));
                kmin1_mers_raw.Add((kmer.Item1.SubArray(1, kmer_length - 1), kmer.Item2));
            });

            meta_data.kmin1_mers_raw = kmin1_mers_raw.Count;

            // All kmin1_mers, the sequence and its origins
            var kmin1_mers = new List<(AminoAcid[], List<int>)>();

            foreach (var kmin1_mer in kmin1_mers_raw)
            {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++)
                {
                    // Find if it is already in the list
                    if (AminoAcid.ArrayHomology(kmin1_mer.Item1, kmin1_mers[i].Item1) >= duplicate_threshold)
                    {
                        if (!kmin1_mers[i].Item2.Contains(kmin1_mer.Item2)) kmin1_mers[i].Item2.Add(kmin1_mer.Item2); // Update origins
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

                    List<int> forward_nodes = new List<int>();
                    List<int> backward_nodes = new List<int>();

                    // Debug purposes can be deleted later
                    int forward_node_index = i;
                    int backward_node_index = i;

                    List<int> forward_indices = new List<int>();
                    List<int> backward_indices = new List<int>();

                    // Walk forwards until a multifurcartion in the path is found or the end is reached
                    while (forward_node.ForwardEdges.Count == 1 && forward_node.BackwardEdges.Count <= 1)
                    {
                        forward_indices.Add(forward_node_index);
                        forward_node.Visited = true;
                        forward_node_index = forward_node.ForwardEdges[0].Item1;

                        forward_node = graph[forward_node_index];

                        // To account for possible cycles 
                        if (forward_node_index == i) break;
                    }
                    forward_indices.Add(forward_node_index);
                    forward_node.Visited = true;

                    if (forward_node.ForwardEdges.Count() > 0)
                    {
                        forward_nodes = (from node in forward_node.ForwardEdges select node.Item1).ToList();
                    }

                    // Walk backwards
                    while (backward_node.ForwardEdges.Count <= 1 && backward_node.BackwardEdges.Count == 1)
                    {
                        backward_indices.Add(backward_node_index);
                        backward_node.Visited = true;
                        backward_node_index = backward_node.BackwardEdges[0].Item1;

                        backward_node = graph[backward_node_index];

                        // To account for possible cycles 
                        if (backward_node_index == i) break;
                    }
                    backward_indices.Add(backward_node_index);
                    backward_node.Visited = true;
                    if (backward_node.BackwardEdges.Count() > 0)
                    {
                        backward_nodes = (from node in backward_node.BackwardEdges select node.Item1).ToList();
                    }

                    backward_indices.Reverse();
                    forward_indices.RemoveAt(0);
                    backward_indices.AddRange(forward_indices);

                    var sequence = new List<AminoAcid>();
                    List<List<int>> origins = new List<List<int>>();

                    foreach (var kmin1_mer_index in backward_indices) {
                        sequence.Add(graph[kmin1_mer_index].Sequence[0]);
                        origins.Add(graph[kmin1_mer_index].Origins);
                    }
                    sequence.AddRange(graph[backward_indices.Last()].Sequence.SubArray(1, kmer_length-2));

                    condensed_graph.Add(new CondensedNode(sequence, i, forward_node_index, backward_node_index, forward_nodes, backward_nodes, origins));
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
                        node.Prefix = node.Sequence.Take(kmer_length - 1).ToList();
                        node.Sequence = node.Sequence.Skip(kmer_length - 1).ToList();
                    }
                    if (node.ForwardEdges.Count() == 1)
                    {
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
        /// <summary>
        /// Gets all paths starting for all subgraphs
        /// </summary>
        /// <returns>A list with for each starting node a tuple with its index and all possible paths (as a list of indices)</returns>
        public List<(int, List<List<int>>)> GetAllPaths() {
            var opts = new List<(int, List<List<int>>)>();
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];

                // Test if this is a starting node
                if (node.BackwardEdges.Count() == 0 || (node.BackwardEdges.Count() == 1 && node.BackwardEdges[0] == node_index))
                {
                    opts.Add((node_index, GetPaths(node_index, new List<int>())));
                }
            }
            return opts;
        }
        /// <summary>
        /// Gets all paths starting from the given node.
        /// </summary>
        /// <param name="node_index">The node to start from</param>
        /// <param name="indices">The list of all indices of the path up to the start node</param>
        /// <returns>A list with all possible paths (as a list of indices)</returns>
        List<List<int>> GetPaths(int node_index, List<int> indices)
        {
            // Update all paths and scores
            var node = condensed_graph[node_index];
            indices.Add(node_index);

            if (node.ForwardEdges.Count() == 0)
            {
                // End of the sequences, create the output
                return new List<List<int>>{indices};
            }
            else
            {
                var opts = new List<List<int>>();
                // Follow all branches
                foreach (var next in node.ForwardEdges)
                {
                    if (indices.Contains(next))
                    {
                        // Cycle: end the following of the path and generate the output
                        opts.Add(indices);
                    }
                    else
                    {
                        // Follow the sequence
                        opts.AddRange(GetPaths(next, new List<int>(indices)));
                    }
                }
                return opts;
            }
        }
    }
}