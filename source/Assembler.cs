using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace AssemblyNameSpace
{
    /// <summary> The Class with all code to assemble Peptide sequences. </summary>
    public class Assembler
    {
        /// <summary> The reads fed into the Assembler, as opened by OpenReads. </summary>
        public List<AminoAcid[]> reads = new List<AminoAcid[]>();

        /// <summary> The reads that were shorter than the kmer_length, to be able to reuse if needed. </summary>
        public List<(AminoAcid[] Sequence, MetaData.IMetaData MetaData)> shortReads = new List<(AminoAcid[], MetaData.IMetaData)>();

        /// <summary> The meta information as delivered by PEAKS. By definition every index in this list matches 
        /// with the index in reads. When the data was not imported via PEAKS this list is null.</summary>
        public List<MetaData.IMetaData> reads_metadata = null;

        /// <summary> The De Bruijn graph used by the Assembler. </summary>
        public Node[] graph;

        /// <summary> The condensed graph used to store the output of the assembly. </summary>
        public List<CondensedNode> condensed_graph;

        /// <summary> The length of the k-mers used to create the De Bruijn graph. </summary>
        public readonly int kmer_length;


        /// <summary> The private member to store the minimum homology value in. </summary>
        private readonly int minimum_homology;

        /// <summary> The minimum homology value of an edge to include it in the graph. Lowering the limit 
        /// could result in a longer sequence retrieved from the algorithm but would also greatly increase
        /// the computational cost of the calculation. </summary>
        /// <value> The minimum homology before including an edge in the graph. </value>
        public int Minimum_homology
        {
            get { return minimum_homology; }
        }
        public readonly int duplicate_threshold;

        /// <summary> To contain meta information about how the program ran to make informed decisions on 
        /// how to choose the values of variables and to aid in debugging. </summary>
        public MetaInformation meta_data;

        /// <summary>
        /// The alphabet used
        /// </summary>
        public Alphabet alphabet;

        public readonly bool reverse;

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
            reads = new List<AminoAcid[]>();
            reads_metadata = new List<MetaData.IMetaData>();
        }

        /// <summary>
        /// Give a list of reads to the assembler
        /// </summary>
        public void GiveReads(List<(string, MetaData.IMetaData)> reads_i)
        {
            reads.AddRange(reads_i.Select(x => StringToSequence(x.Item1)));
            reads_metadata.AddRange(reads_i.Select(x => x.Item2));
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

        /// <summary>
        /// Generates all k-mers of length (kmer_length).
        /// </summary>
        List<(AminoAcid[] Sequence, int ReadIndex, int ReadOffset)> GenerateKMers()
        {
            var kmers = new List<(AminoAcid[] Sequence, int ReadIndex, int ReadOffset)>();
            AminoAcid[] read;

            for (int r = 0; r < reads.Count(); r++)
            {
                read = reads[r];
                if (read.Length > kmer_length)
                {
                    for (int i = 0; i < read.Length - kmer_length + 1; i++)
                    {
                        AminoAcid[] kmer = read.SubArray(i, kmer_length);
                        kmers.Add((kmer, r, i));
                        if (reverse) kmers.Add((kmer.Reverse().ToArray(), r, i)); //Also add the reverse
                    }
                }
                else if (read.Length == kmer_length)
                {
                    kmers.Add((read, r, 0));
                    if (reverse) kmers.Add((read.Reverse().ToArray(), r, 0)); //Also add the reverse
                }
                else
                {
                    // Only used if IncludeShortReads is set to true
                    shortReads.Add((read, reads_metadata[r]));
                }
            }
            meta_data.kmers = kmers.Count;
            return kmers;
        }

        /// <summary>
        /// Generates all (k-1)-mers.
        /// </summary>
        /// <param name="kmers"> The kmers to base the (k-1)-mers on. </param>
        List<(AminoAcid[] Sequence, List<(int ReadIndex, int ReadOffset)> SupportingReads)> GenerateKMin1Mers(List<(AminoAcid[] Sequence, int ReadIndex, int ReadOffset)> kmers)
        {
            var kmin1_mers_raw = new List<(AminoAcid[] Sequence, int ReadIndex, int ReadOffset)>();

            kmers.ForEach(kmer =>
            {
                kmin1_mers_raw.Add((kmer.Sequence.SubArray(0, kmer_length - 1), kmer.ReadIndex, kmer.ReadOffset));
                kmin1_mers_raw.Add((kmer.Sequence.SubArray(1, kmer_length - 1), kmer.ReadIndex, kmer.ReadOffset));
            });

            meta_data.kmin1_mers_raw = kmin1_mers_raw.Count;

            // All kmin1_mers, the sequence and its origins
            var kmin1_mers = new List<(AminoAcid[] Sequence, List<(int ReadIndex, int ReadOffset)> SupportingReads)>();

            foreach (var kmin1_mer in kmin1_mers_raw)
            {
                bool inlist = false;
                for (int i = 0; i < kmin1_mers.Count(); i++)
                {
                    // Find if it is already in the list
                    if (AminoAcid.ArrayHomology(kmin1_mer.Sequence, kmin1_mers[i].Sequence) >= duplicate_threshold)
                    {
                        if (!kmin1_mers[i].SupportingReads.Select(a => a.ReadIndex).Contains(kmin1_mer.ReadIndex)) kmin1_mers[i].SupportingReads.Add((kmin1_mer.ReadIndex, kmin1_mer.ReadOffset)); // Update origins
                        inlist = true;
                        break;
                    }
                }
                if (!inlist) kmin1_mers.Add((kmin1_mer.Sequence, new List<(int, int)> { (kmin1_mer.ReadIndex, kmin1_mer.ReadOffset) }));
            }

            meta_data.kmin1_mers = kmin1_mers.Count;
            return kmin1_mers;
        }

        /// <summary>
        /// Generate a De Bruijn graph based on the kmers and kmin1_mers.
        /// </summary>
        /// <param name="kmers"> The kmers to use. </param>
        /// <param name="kmin1_mers"> The kmin1_mers to use. </param>
        /// <returns> The graph as an adjacency list. </returns>
        Node[] GenerateGraph(List<(AminoAcid[] Sequence, int ReadIndex, int ReadOffset)> kmers, List<(AminoAcid[] Sequence, List<(int ReadIndex, int ReadOffset)> SupportingReads)> kmin1_mers)
        {
            graph = new Node[kmin1_mers.Count];

            int index = 0;
            kmin1_mers.ForEach(kmin1_mer =>
            {
                graph[index] = new Node(kmin1_mer.Sequence, kmin1_mer.SupportingReads.Select(a => a.ReadIndex).ToList());
                index++;
            });

            // Connect the nodes based on the k-mers

            // Initialize the array to save computations
            int[] prefix_matches = new int[graph.Length];
            int[] suffix_matches = new int[graph.Length];
            AminoAcid[] prefix, suffix;

            kmers.ForEach(kmer =>
            {
                prefix = kmer.Sequence.SubArray(0, kmer_length - 1);
                suffix = kmer.Sequence.SubArray(1, kmer_length - 1);

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

            return graph;
        }

        /// <summary>
        /// Generates the condensed graph
        /// </summary>
        /// <param name="graph"> The De Bruijn graph </param>
        List<CondensedNode> GenerateCondensedGraph(Node[] graph)
        {
            var result = new List<CondensedNode>();

            for (int i = 0; i < graph.Length; i++)
            {
                // Start at every node, if it is not visited yet try to build a path from it 
                var start_node = graph[i];

                if (!start_node.Visited)
                {
                    var forward_node = start_node;
                    var backward_node = start_node;
                    var prev_forward_node = start_node;
                    var prev_backward_node = start_node;

                    List<int> forward_nodes = new List<int>();
                    List<int> backward_nodes = new List<int>();

                    // Debug purposes can be deleted later
                    int forward_node_index = i;
                    int backward_node_index = i;

                    List<int> forward_indices = new List<int>();
                    List<int> backward_indices = new List<int>();

                    // Walk forwards until a multifurcartion in the path is found or the end is reached
                    while (forward_node.ForwardEdges.Count == 1 && (forward_node_index == i || forward_node.BackwardEdges.Count <= 1))
                    {
                        forward_indices.Add(forward_node_index);
                        forward_node.Visited = true;
                        forward_node_index = forward_node.ForwardEdges[0].NodeIndex;

                        prev_forward_node = forward_node;
                        forward_node = graph[forward_node_index];

                        // To account for possible cycles 
                        if (forward_node_index == i) break;
                    }

                    if (forward_node_index == i || forward_node.BackwardEdges.Count() <= 1)
                    {
                        forward_indices.Add(forward_node_index);
                        forward_node.Visited = true;

                        if (forward_node.ForwardEdges.Count() > 0)
                        {
                            forward_nodes = (from node in forward_node.ForwardEdges select node.NodeIndex).ToList();
                        }
                    }
                    else if (prev_forward_node.ForwardEdges.Count() > 0)
                    {
                        forward_nodes = (from node in prev_forward_node.ForwardEdges select node.NodeIndex).ToList();
                    }


                    // Walk backwards
                    while ((backward_node_index == i || backward_node.ForwardEdges.Count <= 1) && backward_node.BackwardEdges.Count == 1)
                    {
                        backward_indices.Add(backward_node_index);
                        backward_node.Visited = true;
                        backward_node_index = backward_node.BackwardEdges[0].NodeIndex;

                        prev_backward_node = backward_node;
                        backward_node = graph[backward_node_index];

                        // To account for possible cycles 
                        if (backward_node_index == i) break;
                    }

                    if (backward_node_index == i || backward_node.ForwardEdges.Count() <= 1)
                    {
                        backward_indices.Add(backward_node_index);
                        backward_node.Visited = true;

                        if (backward_node.BackwardEdges.Count() > 0)
                        {
                            backward_nodes = (from node in backward_node.BackwardEdges select node.NodeIndex).ToList();
                        }
                    }
                    else if (prev_backward_node.BackwardEdges.Count() > 0)
                    {
                        backward_nodes = (from node in prev_backward_node.BackwardEdges select node.NodeIndex).ToList();
                    }

                    backward_indices.Reverse();
                    forward_indices.RemoveAt(0);
                    backward_indices.AddRange(forward_indices);

                    var sequence = new List<AminoAcid>();
                    List<List<int>> origins = new List<List<int>>();

                    foreach (var kmin1_mer_index in backward_indices)
                    {
                        sequence.Add(graph[kmin1_mer_index].Sequence[0]);
                        origins.Add(graph[kmin1_mer_index].Origins);
                    }
                    sequence.AddRange(graph[backward_indices.Last()].Sequence.SubArray(1, kmer_length - 2));

                    // I should handle back/forward index differently for 1 (k-1)mer condensed nodes. (set it to null?)
                    result.Add(new CondensedNode(sequence, result.Count(), forward_node_index, backward_node_index, forward_nodes, backward_nodes, origins, backward_indices, TotalArea(origins)));
                }
            }

            return result;
        }

        /// <summary> Assemble the reads into the graph. </summary>
        public void Assemble()
        {
            // Start the stopwatch to be able to say how long the program ran
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var kmers = GenerateKMers();

            var kmin1_mers = GenerateKMin1Mers(kmers);

            meta_data.pre_time = stopWatch.ElapsedMilliseconds;

            var graph = GenerateGraph(kmers, kmin1_mers);

            meta_data.graph_time = stopWatch.ElapsedMilliseconds - meta_data.pre_time;

            condensed_graph = GenerateCondensedGraph(graph);

            RemovePreAndSuffixes();

            meta_data.path_time = stopWatch.ElapsedMilliseconds - meta_data.graph_time - meta_data.pre_time;

            stopWatch.Stop();
            meta_data.sequence_filter_time = stopWatch.ElapsedMilliseconds - meta_data.path_time - meta_data.graph_time - meta_data.pre_time;
            meta_data.sequences = condensed_graph.Count();
            meta_data.total_time = stopWatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Gets all paths in all subgraphs, also to be described as all possible sequences for all peptides in the graph
        /// </summary>
        /// <returns>A list with all possible paths</returns>
        public List<GraphPath> GetAllPaths()
        {
            var opts = new List<(int, List<int>)>();
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];

                // Test if this is a starting node
                if (node.BackwardEdges.Count() == 0 || (node.BackwardEdges.Count() == 1 && node.BackwardEdges.ToArray()[0] == node_index))
                {
                    var paths = GetPaths(node_index, new List<int>());
                    foreach (var path in paths)
                    {
                        opts.Add((node_index, path));
                    }
                }
            }

            var result = new List<GraphPath>() { Capacity = opts.Count() };

            for (int i = 0; i < opts.Count(); i++)
            {
                var nodes = opts[i].Item2.Select(index => condensed_graph[index]).ToList();
                result.Add(new GraphPath(nodes, i));
            }

            return result;
        }

        /// <summary>
        /// Gets all paths in all subgraphs, also to be described as all possible sequences for all peptides in the graph
        /// </summary>
        /// <returns>A list with all possible paths</returns>
        public List<GraphPath> GetAllPathsMultipleReads()
        {
            var opts = new List<(int, List<int>)>();
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];

                // Test if this is a starting node
                if (node.BackwardEdges.Count() == 0 || (node.BackwardEdges.Count() == 1 && node.BackwardEdges.ToArray()[0] == node_index))
                {
                    var paths = GetPaths(node_index, new List<int>());
                    foreach (var path in paths)
                    {
                        opts.Add((node_index, path));
                    }
                }
            }

            var result = new List<GraphPath>() { Capacity = opts.Count() };

            for (int i = 0; i < opts.Count(); i++)
            {
                var nodes = opts[i].Item2.Select(index => condensed_graph[index]).ToList();
                if (nodes[0].Origins.Sum(x => x.Sum()) > nodes[0].Origins.Count) result.Add(new GraphPath(nodes, i));
            }

            return result;
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
                return new List<List<int>> { indices };
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
                        // TODO: Should this also include the next node? To really indicate (in the sequence) that it is a cycle
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

        double TotalArea(List<List<int>> origins)
        {
            double area = 0;
            foreach (var column in origins)
            {
                foreach (int node in column)
                {
                    foreach (int read in graph[node].Origins)
                        area += reads_metadata[read].TotalArea;
                }
            }
            return area;
        }

        void RemovePreAndSuffixes()
        {
            // Update the condensed graph to point to elements in the condensed graph instead of to elements in the de Bruijn graph
            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];
                List<int> forward = new List<int>(node.ForwardEdges);
                List<int> backward = new List<int>(node.BackwardEdges);
                node.ForwardEdges.Clear();
                node.BackwardEdges.Clear();

                for (int condensed_index = 0; condensed_index < condensed_graph.Count(); condensed_index++)
                {
                    var cnode = condensed_graph[condensed_index];
                    foreach (var fwe in forward)
                    {
                        if (cnode.GraphIndices.Contains(fwe))
                        {
                            node.ForwardEdges.Add(condensed_index);
                            break;
                        }
                    }
                    foreach (var bwe in backward)
                    {
                        if (cnode.GraphIndices.Contains(bwe))
                        {
                            node.BackwardEdges.Add(condensed_index);
                            break;
                        }
                    }
                }
            }

            // Remove overlaps
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                var node = condensed_graph[i];
                // Remove the part of the sequence that overlaps with the previous or next node
                if (!node.BackwardEdges.Contains(i) && !node.ForwardEdges.Contains(i))
                {
                    if (node.BackwardEdges.Count() == 1 && node.Sequence.Count() >= kmer_length - 2)
                    {
                        node.Prefix = node.Sequence.Take(kmer_length - 2).ToList();
                        node.Sequence = node.Sequence.Skip(kmer_length - 2).ToList();
                    }
                    if (node.ForwardEdges.Count() == 1 && node.Sequence.Count() >= kmer_length - 2)
                    {
                        node.Suffix = node.Sequence.Skip(node.Sequence.Count() - kmer_length + 2).ToList();
                        node.Sequence = node.Sequence.Take(node.Sequence.Count() - kmer_length + 2).ToList();
                    }
                    if (node.Sequence.Count() == 0)
                    {
                        // Remove
                        var bwe = node.BackwardEdges;
                        var fwe = node.ForwardEdges;
                        // Link the forward and backward edges and remove references to this item
                        foreach (var b in bwe)
                        {
                            foreach (var f in fwe)
                            {
                                condensed_graph[b].ForwardEdges.Add(f);
                                condensed_graph[f].BackwardEdges.Add(b);
                            }
                            condensed_graph[b].ForwardEdges.Remove(node.Index);
                        }
                        foreach (var f in fwe)
                        {
                            condensed_graph[f].BackwardEdges.Remove(node.Index);
                        }
                        condensed_graph[i] = null;
                    }
                }
            }

            // Remove repetitive pre/suffixes which are not yet removed
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                var node = condensed_graph[i];
                if (node == null) continue;
                if (node.BackwardEdges.Count() > 1 && node.Sequence.Count() >= kmer_length - 2)
                {
                    // A node with multiple incoming but without a prefix removed
                    var pattern = node.Sequence.Take(kmer_length - 2).ToArray();
                    bool should_remove = true;
                    bool useful = false;

                    foreach (var b in node.BackwardEdges)
                    {
                        var bnode = condensed_graph[b];
                        if (bnode == null || bnode.Prefix.Count() + bnode.Sequence.Count() + bnode.Suffix.Count() < kmer_length - 2)
                        {
                            should_remove = false;
                            break;
                        }
                        else if (bnode.Suffix.Count() > 0)
                        {
                            if (AminoAcid.ArrayHomology(pattern, bnode.Suffix.ToArray()) < 1)
                            {
                                should_remove = false;
                                break;
                            }
                        }
                        else
                        {
                            useful = true;
                            var bnode_full = new List<AminoAcid>();
                            bnode_full.AddRange(bnode.Prefix);
                            bnode_full.AddRange(bnode.Sequence);
                            if (AminoAcid.ArrayHomology(pattern, bnode_full.GetRange(bnode_full.Count() - kmer_length + 2, kmer_length - 2).ToArray()) < 1)
                            {
                                should_remove = false;
                                break;
                            }
                        }
                    }

                    if (should_remove && useful)
                    {
                        node.Prefix = pattern.ToList();
                        node.Sequence = node.Sequence.Skip(kmer_length - 2).ToList();

                        foreach (var b in node.BackwardEdges)
                        {
                            var bnode = condensed_graph[b];
                            if (bnode.Suffix.Count() > 0)
                            {
                                bnode.Suffix = new List<AminoAcid>();
                                bnode.Sequence.AddRange(pattern);
                            }
                        }

                        if (node.Sequence.Count() == 0)
                        {
                            condensed_graph[i] = null;
                            continue;
                        }
                    }
                }
                if (node.ForwardEdges.Count() > 1 && node.Sequence.Count() >= kmer_length - 2)
                {
                    // A node with multiple incoming but without a prefix removed
                    var pattern = node.Sequence.GetRange(node.Sequence.Count() - kmer_length + 2, kmer_length - 2).ToArray();
                    bool should_remove = true;
                    bool useful = false;

                    foreach (var f in node.ForwardEdges)
                    {
                        var fnode = condensed_graph[f];
                        if (fnode == null || fnode.Prefix.Count() + fnode.Sequence.Count() + fnode.Suffix.Count() < kmer_length - 2)
                        {
                            should_remove = false;
                            break;
                        }
                        else if (fnode.Prefix.Count() > 0)
                        {
                            if (AminoAcid.ArrayHomology(pattern, fnode.Prefix.ToArray()) < 1)
                            {
                                should_remove = false;
                                break;
                            }
                        }
                        else
                        {
                            useful = true;
                            var fnode_full = new List<AminoAcid>();
                            fnode_full.AddRange(fnode.Sequence);
                            fnode_full.AddRange(fnode.Suffix);
                            if (AminoAcid.ArrayHomology(pattern, fnode_full.GetRange(0, kmer_length - 2).ToArray()) < 1)
                            {
                                should_remove = false;
                                break;
                            }
                        }
                    }

                    if (should_remove && useful)
                    {
                        node.Suffix = pattern.ToList();
                        node.Sequence = node.Sequence.Take(node.Sequence.Count() - kmer_length + 2).ToList();

                        foreach (var f in node.ForwardEdges)
                        {
                            var fnode = condensed_graph[f];
                            if (fnode.Prefix.Count() > 0)
                            {
                                fnode.Prefix = new List<AminoAcid>();
                                fnode.Sequence.InsertRange(0, pattern);
                            }
                        }

                        if (node.Sequence.Count() == 0)
                        {
                            condensed_graph[i] = null;
                            continue;
                        }
                    }
                }
            }

            // Redo indexing
            // Get new index and remove empty elements
            var lookup = new Dictionary<int, int>();
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                if (condensed_graph[i] == null)
                {
                    condensed_graph.RemoveAt(i);
                    i--;
                }
                else
                {
                    lookup.Add(condensed_graph[i].Index, i);
                }
            }
            // Update indices
            foreach (var node in condensed_graph)
            {
                node.Index = lookup[node.Index];

                var bwe = new HashSet<int>(node.BackwardEdges);
                node.BackwardEdges = new HashSet<int>();
                foreach (var b in bwe)
                {
                    if (lookup.ContainsKey(b))
                        node.BackwardEdges.Add(lookup[b]);
                }

                var fwe = new HashSet<int>(node.ForwardEdges);
                node.ForwardEdges = new HashSet<int>();
                foreach (var f in fwe)
                {
                    if (lookup.ContainsKey(f))
                        node.ForwardEdges.Add(lookup[f]);
                }
            }

            // Calculate all reads alignments for all condensed nodes
            foreach (var node in condensed_graph)
            {
                node.CalculateReadsAlignment(reads, alphabet, kmer_length);
            }
        }
    }
}