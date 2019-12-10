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
using System.ComponentModel;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A FASTA report
    /// </summary>
    class FASTAReport : Report
    {
        int MinScore;
        /// <summary>
        /// To retrieve all metadata
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="minscore">The minimal score needed to be included in the file</param>
        public FASTAReport(ReportInputParameters parameters, int minscore) : base(parameters)
        {
            MinScore = minscore;
        }
        /// <summary>
        /// Creates a FASTA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score.
        /// </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create()
        {
            var buffer = new StringBuilder();
            var sequences = new List<(int, string)>();

            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];

                // Test if this is a starting node
                if (node.BackwardEdges.Count() == 0 || (node.BackwardEdges.Count() == 1 && node.BackwardEdges[0] == node_index))
                {
                    GetPaths(node_index, "", 0, sequences, new List<int>());
                }
            }

            // Filter and sort the lines
            sequences = sequences.FindAll(i => i.Item1 >= MinScore);
            sequences.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            foreach (var line in sequences)
            {
                buffer.AppendLine(line.Item2);
            }

            return buffer.ToString();
        }
        /// <summary>
        /// Gets all paths starting from the given node.
        /// </summary>
        /// <param name="node_index">The node to start from</param>
        /// <param name="currentpath">The sequences up to the start node</param>
        /// <param name="currentscore">The score up to the start node</param>
        /// <param name="output">The list containing all lines, here the output of the function will be aggregated</param>
        /// <param name="indices">The list of all indices of the path up to the start node</param>
        /// <returns>Nothing, see output for the output</returns>
        void GetPaths(int node_index, string currentpath, int currentscore, List<(int, string)> output, List<int> indices)
        {
            // Update all paths and scores
            var node = condensed_graph[node_index];
            string nextpath = currentpath + AminoAcid.ArrayToString(node.Sequence.ToArray());
            int nextscore = currentscore + CalculateScore(node);
            indices.Add(node_index);

            if (node.ForwardEdges.Count() == 0)
            {
                // End of the sequences, create the output
                // Create the ID of the path (indices of all contigs)
                string id = indices.Aggregate("", (b, a) => $"{b}-{a.ToString()}").Substring(1);

                output.Add((nextscore, $">{id} score:{nextscore}\n{nextpath}"));
            }
            else
            {
                // Follow all branches
                foreach (var next in node.ForwardEdges)
                {
                    if (indices.Contains(next))
                    {
                        // Cycle: end the following of the path and generate the output
                        // Create the ID of the path (indices of all contigs)
                        string id = indices.Aggregate("", (b, a) => $"{b}-{a.ToString()}").Substring(1);

                        output.Add((nextscore, $">{id}-|{next.ToString()}| score:{nextscore}\n{nextpath}"));
                    }
                    else
                    {
                        // Follow the sequence
                        GetPaths(next, nextpath, nextscore, output, new List<int>(indices));
                    }
                }
            }
        }
        /// <summary> Create a reads alignment and calculates depth of coverage. </summary>
        /// <param name="node">The node to calculate the score of</param>
        /// <returns> Returns a score per base. </returns>
        int CalculateScore(CondensedNode node)
        {
            // Align the reads used for this sequence to the sequence
            string sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            Dictionary<int, string> lookup = node.UniqueOrigins.Select(x => (x, AminoAcid.ArrayToString(reads[x]))).ToDictionary(item => item.Item1, item => item.Item2);
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, lookup, node.Origins, alphabet, singleRun.K, true);

            // Calculate the score by calculating the total read length (which maps to a location on the contig)
            int score = 0;
            for (int pos = 0; pos < sequence.Length; pos++)
            {
                foreach (var read in positions)
                {
                    if (pos >= read.StartPosition && pos < read.EndPosition)
                    {
                        score++;
                    }
                }
            }

            return score;
        }
    }
}