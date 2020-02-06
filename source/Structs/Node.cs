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
    /// <summary> Nodes in the graph with a sequence length of K-1. </summary>
    public class Node
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

        /// <summary> The creator of Nodes. </summary>
        /// <param name="seq"> The sequence of this Node. </param>
        /// <param name="origin"> The origin(s) of this (k-1)-mer. </param>
        /// <remarks> It will initialize the edges list. </remarks>
        public Node(AminoAcid[] seq, List<int> origin)
        {
            sequence = seq;
            origins = origin;
            forwardEdges = new List<ValueTuple<int, int, int>>();
            backwardEdges = new List<ValueTuple<int, int, int>>();
            Visited = false;
        }

        /// <summary> To add a forward edge to the Node. Wil only be added if the score is high enough. </summary>
        /// <param name="target"> The index of the Node where this edge goes to. </param>
        /// <param name="score1"> The homology of the edge with the first Node. </param>
        /// <param name="score2"> The homology of the edge with the second Node. </param>
        public void AddForwardEdge(int target, int score1, int score2)
        {
            bool inlist = false;
            foreach (var edge in forwardEdges)
            {
                if (edge.Item1 == target)
                {
                    inlist = true;
                    break;
                }
            }
            if (!inlist) forwardEdges.Add((target, score1, score2));
            return;
        }

        /// <summary> To add a backward edge to the Node. </summary>
        /// <param name="target"> The index of the Node where this edge comes from. </param>
        /// <param name="score1"> The homology of the edge with the first Node. </param>
        /// <param name="score2"> The homology of the edge with the second Node. </param>
        public void AddBackwardEdge(int target, int score1, int score2)
        {
            bool inlist = false;
            foreach (var edge in backwardEdges)
            {
                if (edge.Item1 == target)
                {
                    inlist = true;
                    break;
                }
            }
            if (!inlist) backwardEdges.Add((target, score1, score2));
            return;
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
        /// <remarks> O(1) runtime. </remarks>
        /// <returns> The amount of edges (forwards and backwards). </returns>
        public int EdgesCount()
        {
            return forwardEdges.Count + backwardEdges.Count;
        }
    }

}