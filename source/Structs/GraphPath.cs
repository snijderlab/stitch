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
    /// <summary> Paths in the graph, consisting of multiple CondensedNodes. </summary>
    public class GraphPath
    {
        public readonly List<CondensedNode> Nodes;
        public readonly AminoAcid[] Sequence;
        public readonly int[] DepthOfCoverage;
        public readonly int[] ContigID;
        public readonly int Score;
        public int Index;
        public readonly MetaData.IMetaData MetaData;
        public string Identifiers
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var node in Nodes)
                {
                    sb.Append($"-{node.Index}");
                }
                return sb.ToString().Remove(0, 1);
            }
        }
        public GraphPath(ICollection<CondensedNode> nodes, int index)
        {
            Nodes = nodes.ToList();
            Index = index;

            int totallength = Nodes.Aggregate(0, (a, b) => a + b.Sequence.Count());
            var list = new List<AminoAcid>() { Capacity = totallength };
            var depth = new List<int>() { Capacity = totallength };
            var id = new List<int>() { Capacity = totallength };

            foreach (var node in Nodes)
            {
                list.AddRange(node.Sequence);
                depth.AddRange(node.DepthOfCoverage);
                id.AddRange(Enumerable.Repeat(node.Index, node.Sequence.Count()));
            }
            Sequence = list.ToArray();
            DepthOfCoverage = depth.ToArray();
            Score = depth.Sum();
            ContigID = id.ToArray();
            MetaData = new MetaData.Path(this);
        }
        /// <summary>
        /// Creates a basic graphpath, mainly for testing purposes but also used for included short reads
        /// </summary>
        /// <param name="Sequence"></param>
        public GraphPath(List<AminoAcid> sequence, MetaData.IMetaData metaData = null, int index = -1)
        {
            Nodes = new List<CondensedNode>();
            Index = index;
            MetaData = metaData;
            if (metaData == null)
                MetaData = new MetaData.Path(this);

            Sequence = sequence.ToArray();
            DepthOfCoverage = Enumerable.Repeat(1, sequence.Count).ToArray();
            Score = sequence.Count;
            ContigID = Enumerable.Repeat(-1, sequence.Count).ToArray();
        }
    }

}