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
        public readonly int Score;
        public readonly int Index;
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

            foreach (var node in Nodes)
            {
                list.AddRange(node.Sequence);
                depth.AddRange(node.DepthOfCoverage);
            }
            Sequence = list.ToArray();
            DepthOfCoverage = depth.ToArray();
            Score = depth.Sum();
        }
    }

}