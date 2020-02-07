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
                    sequences.Add((template.Score, $">{template.Location.TemplateIndex} score:{template.Score}\n{ConsensusSequence(template)}"));
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

        string ConsensusSequence(Template template)
        {
            // Display Consensus Sequence
            var consensus = new StringBuilder();
            var consensus_sequence = template.CombinedSequence();

            for (int i = 0; i < consensus_sequence.Count; i++)
            {
                // Get the highest chars
                string options = "";
                int max = 0;
                foreach (var item in consensus_sequence[i].Item1)
                {
                    if (item.Value > max)
                    {
                        options = item.Key.ToString();
                        max = item.Value;
                    }
                    else if (item.Value == max)
                    {
                        options += item.Key.ToString();
                    }
                }
                if (options.Length > 1)
                {
                    consensus.Append("(");
                    consensus.Append(options);
                    consensus.Append(")");
                }
                else if (options.Length == 1)
                {
                    consensus.Append(options);
                }
                else
                {
                    consensus.Append("_");
                }
                // Get the highest gap
                List<Template.IGap> max_gap = new List<Template.IGap> { new Template.None() };
                int max_gap_score = 0;
                foreach (var item in consensus_sequence[i].Item2)
                {
                    if (item.Value.Count > max_gap_score)
                    {
                        max_gap = new List<Template.IGap> { item.Key };
                        max_gap_score = item.Value.Count;
                    }
                    else if (item.Value.Count == max)
                    {
                        max_gap.Add(item.Key);
                    }
                }
                if (max_gap.Count > 1)
                {
                    consensus.Append("(");
                    foreach (var item in max_gap)
                    {
                        consensus.Append(item.ToString());
                        consensus.Append("/");
                    }
                    consensus.Append(")");
                }
                else if (max_gap.Count == 1)
                {
                    consensus.Append(max_gap[0].ToString());
                }
                else
                {
                    consensus.Append("_");
                }
            }
            return consensus.ToString();
        }
    }
}