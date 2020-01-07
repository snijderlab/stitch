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
            var sequences = new List<(int, string)>();

            foreach (var node in paths)
            {
                var path = node.Item2;
                int score = 0;
                var id = new StringBuilder();
                var sequence = new StringBuilder();
                for (int index = 0; index < path.Count(); index++)
                {
                    id.Append(index);
                    score += CalculateScore(condensed_graph[index]);
                    if (index == path.Count() - 1)
                    {
                        sequence.Append(AminoAcid.ArrayToString(condensed_graph[index].Sequence.ToArray()));
                    }
                    else
                    {
                        sequence.Append(condensed_graph[index].Sequence[0].ToString());
                        id.Append("-");
                    }
                }
                sequences.Add((score, $">{id} score:{score}\n{sequence}"));
            }

            // Filter and sort the lines
            //sequences = sequences.FindAll(i => i.Item1 >= MinScore);
            //sequences.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            
            var buffer = new StringBuilder();
            foreach (var line in sequences)
            {
                buffer.AppendLine(line.Item2);
            }

            return buffer.ToString().Trim();
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