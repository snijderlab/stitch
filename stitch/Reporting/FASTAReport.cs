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
            this.MinScore = min_score;
            this.OutputType = outputType;
        }

        /// <summary> Creates a FASTA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score. </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create() {
            var sequences = new List<(double, string)>();

            if (this.OutputType == RunParameters.Report.OutputType.Recombine) {
                sequences.EnsureCapacity(this.Parameters.RecombinedSegment.Select(a => a.Templates.Count).Sum());
                foreach (var template in this.Parameters.RecombinedSegment.SelectMany(a => a.Templates)) {
                    if (template.Score >= this.MinScore) {
                        var doc = System.String.Join(',', template.ConsensusSequence().Item2.Select(i => i.ToString("F2")));
                        sequences.Add((template.Score, $">{template.MetaData.Identifier} score:{template.Score} depth_of_coverage:{doc}\n{AminoAcid.ArrayToString(template.ConsensusSequence().Item1.SelectMany(i => i.Sequence))}"));
                    }
                }
            } else // TemplateMatching
              {
                sequences.EnsureCapacity(this.Parameters.Groups.Select(a => a.Segments.Count).Sum());
                foreach (var (group, dbs) in this.Parameters.Groups) {
                    foreach (var template in dbs.SelectMany(a => a.Templates)) {
                        if (template.Score >= this.MinScore) {
                            var doc = System.String.Join(',', template.ConsensusSequence().Item2.Select(i => i.ToString("F2")));
                            sequences.Add((template.Score, $">{template.MetaData.Identifier} id:{group}-{template.Location.TemplateIndex} score:{template.Score} depth_of_coverage:{doc}\n{AminoAcid.ArrayToString(template.ConsensusSequence().Item1.SelectMany(i => i.Sequence))}"));
                        }
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