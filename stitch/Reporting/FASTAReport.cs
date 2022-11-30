using System.Collections.Generic;
using System.Text;
using System.Linq;
using static System.Math;

namespace Stitch {
    /// <summary> A FASTA report. </summary>
    public class FASTAReport : Report {
        readonly int MinScore;
        readonly RunParameters.Report.OutputType OutputType;

        /// <summary> To retrieve all metadata. </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="min_score">The minimal score needed to be included in the file.</param>
        public FASTAReport(ReportInputParameters parameters, int min_score, RunParameters.Report.OutputType outputType, int maxThreads) : base(parameters, maxThreads) {
            MinScore = min_score;
            OutputType = outputType;
        }

        /// <summary> Creates a FASTA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score. </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create() {
            var sequences = new List<(double, string)>();

            if (OutputType == RunParameters.Report.OutputType.Recombine) {
                sequences.Capacity = Max(sequences.Capacity, Parameters.RecombinedSegment.Select(a => a.Templates.Count).Sum());
                foreach (var template in Parameters.RecombinedSegment.SelectMany(a => a.Templates)) {
                    if (template.Score >= MinScore)
                        sequences.Add((template.Score, $">{template.MetaData.Identifier} score:{template.Score}\n{AminoAcid.ArrayToString(template.ConsensusSequence().Item1.SelectMany(i => i.Sequence))}"));
                }
            } else // TemplateMatching
              {
                sequences.Capacity = Max(sequences.Capacity, Parameters.Groups.Select(a => a.Item2.Count).Sum());
                foreach (var (group, dbs) in Parameters.Groups) {
                    foreach (var template in dbs.SelectMany(a => a.Templates)) {
                        if (template.Score >= MinScore)
                            sequences.Add((template.Score, $">{template.MetaData.Identifier} id:{group}-{template.Location.TemplateIndex} score:{template.Score}\n{AminoAcid.ArrayToString(template.ConsensusSequence().Item1.SelectMany(i => i.Sequence))}"));
                    }
                }
            }

            // Filter and sort the lines
            sequences.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            var buffer = new StringBuilder();
            foreach (var line in sequences) {
                buffer.AppendLine(line.Item2);
            }

            return buffer.ToString().Trim();
        }
    }
}