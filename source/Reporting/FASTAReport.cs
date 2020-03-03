using System.Collections.Generic;
using System.Text;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A FASTA report.
    /// </summary>
    class FASTAReport : Report
    {
        readonly int MinScore;
        readonly RunParameters.Report.FastaOutputType OutputType;

        /// <summary>
        /// To retrieve all metadata.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="minscore">The minimal score needed to be included in the file.</param>
        public FASTAReport(ReportInputParameters parameters, int minscore, RunParameters.Report.FastaOutputType outputType) : base(parameters)
        {
            MinScore = minscore;
            OutputType = outputType;
        }

        /// <summary>
        /// Creates a FASTA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score.
        /// </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create()
        {
            var sequences = new List<(int, string)>();

            if (OutputType == RunParameters.Report.FastaOutputType.Paths)
            {
                foreach (var path in Paths)
                {
                    sequences.Add((path.Score, $">{path.Identifiers} score:{path.Score}\n{AminoAcid.ArrayToString(path.Sequence)}"));
                }
            }
            else
            {
                foreach (var template in RecombinedDatabase.Templates)
                {
                    sequences.Add((template.Score, $">{template.Location.TemplateIndex} score:{template.Score}\n{HelperFunctionality.ConsensusSequence(template)}"));
                }
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