using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Stitch {
    /// <summary> A FASTA report. </summary>
    public class CSVReport : Report {
        readonly RunParameters.Report.OutputType OutputType;

        /// <summary> To retrieve all metadata. </summary>
        /// <param name="parameters">The parameters.</param>
        public CSVReport(ReportInputParameters parameters, RunParameters.Report.OutputType outputType, int maxThreads) : base(parameters, maxThreads) {
            OutputType = outputType;
        }

        /// <summary> Creates a CSV file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score. </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create() {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-GB");

            var header = new List<string>() { "ReadID", "TemplateID", "GroupID", "SegmentID", "Sequence", "Score", "Unique", "StartOnTemplate", "StartOnRead", "LengthOnTemplate", "Alignment", "CDR" };
            var data = new List<List<string>>();
            var peaks = false;

            void AddLine(string group, Template template, Alignment match) {
                var annotation = template.ConsensusSequenceAnnotation();
                var cdr = false;
                // Detect it this read is part of any CDR
                for (int i = 0; i < match.LenA; i++)
                    if (annotation[match.StartA + i].IsAnyCDR()) {
                        cdr = true;
                        break;
                    }
                var row = new List<string> {
                    match.ReadB.Identifier,
                    template.MetaData.Identifier,
                    template.Name,
                    group,
                    AminoAcid.ArrayToString(match.ReadB.Sequence.Sequence),
                    match.Score.ToString(),
                    match.Unique.ToString(),
                    match.StartA.ToString(),
                    match.StartB.ToString(),
                    match.LenA.ToString(),
                    '\"' + match.ShortPath() + '\"',
                    cdr.ToString()
                    };
                if (match.ReadB is Read.Peaks) {
                    peaks = true;
                    var meta = (Read.Peaks)match.ReadB;
                    row.AddRange(new List<string>
                        {
                            meta.Fraction,
                            meta.Source_File,
                            meta.Feature,
                            meta.ScanID,
                            meta.DeNovoScore.ToString(),
                            meta.Mass_over_charge.ToString(),
                            meta.Charge.ToString(),
                            meta.Retention_time.ToString(),
                            meta.PredictedRetentionTime,
                            meta.Area.ToString(),
                            meta.Mass.ToString(),
                            meta.Parts_per_million.ToString(),
                            meta.Post_translational_modifications,
                            System.String.Join(' ', meta.Sequence.PositionalScore.Select(a => a.ToString())),
                            meta.Original_tag,
                            meta.Fragmentation_mode
                        });
                }
                data.Add(row);
            }

            if (OutputType == RunParameters.Report.OutputType.Recombine) {
                foreach (var template in Parameters.RecombinedSegment.SelectMany(a => a.Templates)) {
                    foreach (var read in template.Matches) {
                        AddLine("Recombine", template, read);
                    }
                }
            } else // TemplateMatching
              {
                foreach (var (group, dbs) in Parameters.Groups) {
                    foreach (var template in dbs.SelectMany(a => a.Templates)) {
                        foreach (var read in template.Matches) {
                            AddLine(group, template, read);
                        }
                    }
                }
            }

            if (peaks) {
                header.AddRange(new List<string> { "Fraction", "Source File", "Feature", "Scan", "Denovo Score", "m/z", "z", "RT", "Predict RT", "Area", "Mass", "ppm", "PTM", "local confidence (%)", "tag (>=0%)", "mode" });
            }

            var buffer = new StringBuilder();
            buffer.AppendJoin(',', header);
            buffer.Append('\n');
            foreach (var line in data) {
                buffer.AppendJoin(',', line);
                buffer.Append('\n');
            }

            System.Globalization.CultureInfo.CurrentCulture = culture;
            return buffer.ToString();
        }
    }
}