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
    /// <summary> Nodes in the condensed graph with a variable sequence length. </summary>
    public class CondensedNode
    {
        /// <summary> The index this node. The index is defined as the index of the startnode in the condensed node list. </summary>
        public int Index;

        /// <summary> The index of the last node (going from back to forth). To build the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
        public int ForwardIndex;

        /// <summary> The index of the first node (going from back to forth). To build the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
        public int BackwardIndex;

        /// <summary> Whether or not this node is visited yet. </summary>
        public bool Visited;

        /// <summary> The sequence of this node. It is the longest constant sequence to be
        /// found in the de Bruijn graph starting at the Index. See <see cref="CondensedNode.Index"/>.</summary>
        public List<AminoAcid> Sequence;

        /// <summary>
        /// The possible prefix sequence before the sequence, trimmed off in creating the condensed graph.
        /// </summary>
        public List<AminoAcid> Prefix;

        /// <summary>
        /// The possible suffix sequence after the sequence, trimmed off in creating the condensed graph.
        /// </summary>
        public List<AminoAcid> Suffix;

        /// <summary> The list of forward edges, defined as the indexes in the de Bruijn graph. </summary>
        public List<int> ForwardEdges;

        /// <summary> The list of backward edges, defined as the indexes in the de Bruijn graph. </summary>
        public List<int> BackwardEdges;

        /// <summary> The origins where the (k-1)-mers used for this sequence come from. Defined as the index in the list with reads. </summary>
        public List<List<int>> Origins;
        public List<List<HelperFunctionality.ReadPlacement>> Alignment;
        public int[] DepthOfCoverageFull;
        public int[] DepthOfCoverage;
        public List<int> UniqueOrigins
        {
            get
            {
                var output = new List<int>();
                foreach (var outer in Origins)
                {
                    foreach (var inner in outer)
                    {
                        if (!output.Contains(inner)) output.Add(inner);
                    }
                }
                output.Sort();
                return output;
            }
        }

        /// <summary> Creates a condensed node to be used in the condensed graph. </summary>
        /// <param name="sequence"> The sequence of this node. See <see cref="CondensedNode.Sequence"/>.</param>
        /// <param name="index"> The index of the node, the index in the de condensed graph. See <see cref="CondensedNode.Index"/>.</param>
        /// <param name="forward_index"> The index of the last node of the sequence (going from back to forth). See <see cref="CondensedNode.ForwardIndex"/>.</param>
        /// <param name="backward_index"> The index of the first node of the sequence (going from back to forth). See <see cref="CondensedNode.BackwardIndex"/>.</param>
        /// <param name="forward_edges"> The forward edges from this node (indexes). See <see cref="CondensedNode.ForwardEdges"/>.</param>
        /// <param name="backward_edges"> The backward edges from this node (indexes). See <see cref="CondensedNode.BackwardEdges"/>.</param>
        /// <param name="origins"> The origins where the (k-1)-mers used for this sequence come from. See <see cref="CondensedNode.Origins"/>.</param>
        public CondensedNode(List<AminoAcid> sequence, int index, int forward_index, int backward_index, List<int> forward_edges, List<int> backward_edges, List<List<int>> origins)
        {
            Sequence = sequence;
            Index = index;
            ForwardIndex = forward_index;
            BackwardIndex = backward_index;
            ForwardEdges = forward_edges;
            BackwardEdges = backward_edges;
            Origins = origins;
            Visited = false;
            Suffix = new List<AminoAcid>();
            Prefix = new List<AminoAcid>();
        }

        /// <summary>
        /// Retrieves an optimal placement of the reads to this node. And saves it for later use.
        /// </summary>
        /// <param name="reads"> All the reads of the assembly (the indices should be the same as in the UniqueOrigins).</param>
        /// <param name="alphabet">The alphabet for the alignment.</param>
        /// <param name="K">The K for the alignment.</param>
        public List<List<HelperFunctionality.ReadPlacement>> CalculateReadsAlignment(List<AminoAcid[]> reads, Alphabet alphabet, int K)
        {
            string sequence = AminoAcid.ArrayToString(Prefix) + AminoAcid.ArrayToString(Sequence) + AminoAcid.ArrayToString(Suffix);
            Dictionary<int, string> lookup = UniqueOrigins.Select(x => (x, AminoAcid.ArrayToString(reads[x]))).ToDictionary(item => item.x, item => item.Item2);
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, lookup, Origins, alphabet, K, true);
            sequence = AminoAcid.ArrayToString(Sequence);
            int prefixoffset = Prefix.Count();

            // Delete matches at prefix and suffix
            positions = positions.Where(a => a.EndPosition > prefixoffset && a.StartPosition < sequence.Length + prefixoffset).ToList();
            //  Update the overhang at the start and end
            positions = positions.Select(a =>
            {
                if (a.StartPosition < prefixoffset)
                {
                    a.StartOverhang += a.Sequence.Substring(0, prefixoffset - a.StartPosition);
                }
                if (a.EndPosition > prefixoffset + sequence.Length)
                {
                    a.EndOverhang += a.Sequence.Substring(a.EndPosition - prefixoffset - sequence.Length);
                }
                return a;
            }).ToList();
            List<int> uniqueorigins = positions.Select(a => a.Identifier).ToList();

            // Find a bit more efficient packing of reads on the sequence
            var placed = new List<List<HelperFunctionality.ReadPlacement>>();
            foreach (var current in positions)
            {
                bool fit = false;
                for (int i = 0; i < placed.Count() && !fit; i++)
                {
                    // Find if it fits in this row
                    bool clashes = false;
                    for (int j = 0; j < placed[i].Count() && !clashes; j++)
                    {
                        if ((current.StartPosition + 1 > placed[i][j].StartPosition && current.StartPosition - 1 < placed[i][j].EndPosition)
                         || (current.EndPosition + 1 > placed[i][j].StartPosition && current.EndPosition - 1 < placed[i][j].EndPosition)
                         || (current.StartPosition - 1 < placed[i][j].StartPosition && current.EndPosition + 1 > placed[i][j].EndPosition))
                        {
                            clashes = true;
                        }
                    }
                    if (!clashes)
                    {
                        placed[i].Add(current);
                        fit = true;
                    }
                }
                if (!fit)
                {
                    placed.Add(new List<HelperFunctionality.ReadPlacement> { current });
                }
            }
            Alignment = placed;
            CalculateDepthOfCoverageFull();
            CalculateDepthOfCoverage();
            return placed;
        }

        /// <summary>
        /// Retrieves the depth of coverage for each position of the reads to this node.
        /// </summary>
        void CalculateDepthOfCoverageFull()
        {
            var placement = Alignment;
            int sequenceLength = Prefix.Count() + Sequence.Count() + Suffix.Count();
            int[] depthOfCoverage = new int[sequenceLength];

            foreach (var row in placement)
            {
                foreach (var read in row)
                {
                    for (int i = read.StartPosition; i < read.StartPosition + read.Sequence.Length; i++)
                    {
                        depthOfCoverage[i]++;
                    }
                }
            }
            DepthOfCoverageFull = depthOfCoverage;
        }

        /// <summary>
        /// Retrieves the depth of coverage for each position of the reads to this node.
        /// </summary>
        void CalculateDepthOfCoverage()
        {
            var depthOfCoverage = DepthOfCoverageFull.SubArray(Prefix.Count(), Sequence.Count());
            DepthOfCoverage = depthOfCoverage;
        }
    }

}