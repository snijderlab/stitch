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
        /// <summary> The index this node. The index is defined as the index of the startnode in the adjacency list of the de Bruijn graph. </summary>
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
        /// <param name="index"> The index of the node, the index in the de Bruijn graph. See <see cref="CondensedNode.Index"/>.</param>
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
    }

}