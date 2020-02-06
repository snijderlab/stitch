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
    /// A FASTA report.
    /// </summary>
    class FASTAReport : Report
    {
        readonly int MinScore;

        /// <summary>
        /// To retrieve all metadata.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="minscore">The minimal score needed to be included in the file.</param>
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

            foreach (var path in Paths)
            {
                sequences.Add((path.Score, $">{path.Identifiers} score:{path.Score}\n{AminoAcid.ArrayToString(path.Sequence)}"));
            }

            // Filter and sort the lines
            sequences = sequences.FindAll(i => i.Item1 >= MinScore);
            sequences.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            var buffer = new StringBuilder();
            foreach (var line in sequences)
            {
                buffer.AppendLine(line.Item2);
            }

            return buffer.ToString().Trim();
        }
    }
}