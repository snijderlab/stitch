using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A FASTA report.
    /// </summary>
    public class FASTAReport : Report
    {
        readonly int MinScore;
        readonly RunParameters.Report.FastaOutputType OutputType;

        /// <summary>
        /// To retrieve all metadata.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="minscore">The minimal score needed to be included in the file.</param>
        public FASTAReport(ReportInputParameters parameters, int minscore, RunParameters.Report.FastaOutputType outputType, int max_threads) : base(parameters, max_threads)
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
            var sequences = new List<(double, string)>();

            if (OutputType == RunParameters.Report.FastaOutputType.Assembly)
            {
                sequences.Capacity = Parameters.Paths.Count;
                foreach (var path in Parameters.Paths)
                {
                    if (path.Score >= MinScore)
                        sequences.Add((path.Score, $">{path.Identifiers} score:{path.Score}\n{AminoAcid.ArrayToString(path.Sequence)}"));
                }
            }
            else if (OutputType == RunParameters.Report.FastaOutputType.Recombine)
            {
                sequences.Capacity = Parameters.RecombinedDatabase.Select(a => a.Templates.Count).Sum();
                foreach (var template in Parameters.RecombinedDatabase.SelectMany(a => a.Templates))
                {
                    if (template.Score >= MinScore)
                        sequences.Add((template.Score, $">{template.Location.TemplateIndex} score:{template.Score}\n{template.ConsensusSequence()}"));
                }
            }
            else
            {
                sequences.Capacity = Parameters.ReadAlignment.Select(a => a.Templates.Count).Sum();
                foreach (var template in Parameters.ReadAlignment.SelectMany(a => a.Templates))
                {
                    if (template.Score >= MinScore)
                        sequences.Add((template.Score, $">{template.Location.TemplateIndex} score:{template.Score}\n{template.ConsensusSequence()}"));
                }
            }

            // Filter and sort the lines
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