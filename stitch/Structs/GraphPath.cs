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
        public readonly AminoAcid[] Sequence;
        public readonly double[] DepthOfCoverage;
        public readonly int[] ContigID;
        public readonly double Score;
        public int Index;
        public readonly ReadMetaData.IMetaData MetaData;
        /// <summary>
        /// Creates a basic graph path
        /// </summary>
        /// <param name="Sequence"></param>
        public GraphPath(List<AminoAcid> sequence, ReadMetaData.IMetaData metaData, int index = -1)
        {
            Index = index;
            Sequence = sequence.ToArray();
            if (metaData != null)
                DepthOfCoverage = metaData.PositionalScore;
            else
                DepthOfCoverage = Enumerable.Repeat(1.0, sequence.Count).ToArray();
            Score = DepthOfCoverage.Sum();
            ContigID = Enumerable.Repeat(-1, sequence.Count).ToArray();
            MetaData = metaData;
        }
    }
}