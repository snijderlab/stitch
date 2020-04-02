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
            var sequences = new List<(int, string)>();

            if (OutputType == RunParameters.Report.FastaOutputType.Assembly)
            {
                sequences.Capacity = Paths.Count;
                foreach (var path in Paths)
                {
                    if (path.Score >= MinScore)
                        sequences.Add((path.Score, $">{path.Identifiers} score:{path.Score}\n{AminoAcid.ArrayToString(path.Sequence)}"));
                }
            }
            else if (OutputType == RunParameters.Report.FastaOutputType.Recombine)
            {
                sequences.Capacity = RecombinedDatabase.Templates.Count;
                foreach (var template in RecombinedDatabase.Templates)
                {
                    if (template.Score >= MinScore)
                        sequences.Add((template.Score, $">{template.Location.TemplateIndex} score:{template.Score}\n{template.ConsensusSequence()}"));
                }
            }
            else
            {
                sequences.Capacity = ReadAlignment.Templates.Count;
                foreach (var template in ReadAlignment.Templates)
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