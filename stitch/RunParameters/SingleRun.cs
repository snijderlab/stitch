using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;

namespace Stitch {
    namespace RunParameters {
        /// <summary> All parameters for a single run. </summary>
        public class SingleRun {
            /// <summary> The name of this run. </summary>
            public string Runname = "";
            public string RawDataDirectory = null;
            public bool LoadRawData = false;
            public int MaxNumberOfCPUCores = Environment.ProcessorCount;

            public RunParameters.InputData Input = new InputData();

            /// <summary> The alphabet used in this run. </summary>
            public AlphabetParameter Alphabet;

            /// <summary> The template(s) used in this run. </summary>
            public TemplateMatchingParameter TemplateMatching;
            public RecombineParameter Recombine;

            /// <summary> The reports to be generated. </summary>
            public ReportParameter Report;
            public ProgressBar ProgressBar;
            public ParsedFile BatchFile;
            public RunVariables runVariables;

            /// <summary> Create a new empty single run parameter set. </summary>
            public SingleRun() { }

            /// <summary> To display the main parameters of this run in a string, mainly for error tracking and debugging purposes. </summary>
            /// <returns>The main parameters.</returns>
            public string Display() {
                return $"\tRunname\t\t: {Runname}";
            }

            /// <summary> Runs this run. Runs the assembly, and generates the reports. </summary>
            public void Calculate() {
                Template.AmbiguityThreshold = TemplateMatching.AmbiguityThreshold;
                // Template Matching
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var segments = RunTemplateMatching();

                stopWatch.Stop();

                var recombined_segment = new List<Segment>();

                // Recombine
                if (Recombine != null) {
                    var recombine_sw = new Stopwatch();
                    recombine_sw.Start();

                    RunRecombine(segments, recombined_segment);

                    recombine_sw.Stop();
                }

                // Raw data
                Dictionary<string, List<AnnotatedSpectrumMatch>> fragments = null;
                if (this.LoadRawData) {
                    fragments = Fragmentation.GetSpectra(Input.Data.Cleaned);
                    ProgressBar.Update();
                }

                var parameters = new ReportInputParameters(Input.Data.Cleaned, segments, recombined_segment, this.BatchFile, this.runVariables, this.Runname, fragments);

                // If there is an expected outcome present to answers here
                if (runVariables.ExpectedResult.Count > 0) {
                    GenerateBenchmarkOutput(parameters);
                } else {
                    // Generate the "base" folder path, reuse this later to enforce that all results end up in the same base folder
                    var folder = Report.Folder != null ? ReportParameter.CreateName(this, Report.Folder) : null;
                    // Generate the report(s)
                    foreach (var report in Report.Files) {
                        switch (report) {
                            case Report.HTML h:
                                var html_report = new HTMLReport(parameters, MaxNumberOfCPUCores, h);
                                html_report.Save(h.CreateName(folder, this));
                                break;
                            case Report.FASTA f:
                                var fasta_report = new FASTAReport(parameters, f.MinimalScore, f.OutputType, MaxNumberOfCPUCores);
                                fasta_report.Save(f.CreateName(folder, this));
                                break;
                            case Report.CSV c:
                                var csv_report = new CSVReport(parameters, c.OutputType, MaxNumberOfCPUCores);
                                csv_report.Save(c.CreateName(folder, this));
                                break;
                            case Report.JSON j:
                                var json_report = new JSONReport(parameters, MaxNumberOfCPUCores);
                                json_report.Save(j.CreateName(folder, this));
                                break;
                        }
                    }
                }

                // Did reports
                if (ProgressBar != null) ProgressBar.Update();
            }

            void GenerateBenchmarkOutput(ReportInputParameters parameters) {
                string JSONBlock(string name, string unit, string value, string extra = null) {
                    if (extra != null)
                        return $",{{\"name\":\"{name}\",\"unit\":\"{unit}\",\"value\":{value},\"extra\":\"{extra}\"}}";
                    else
                        return $",{{\"name\":\"{name}\",\"unit\":\"{unit}\",\"value\":{value}}}";
                }
                var buffer = new StringBuilder();
                IEnumerable<(string, Template)> templates = null;
                if (parameters.RecombinedSegment != null)
                    templates = parameters.Groups.Zip(parameters.RecombinedSegment).Where(s => s.First.Item1.ToLower() != "decoy").SelectMany(s => s.Second.Templates.Where(t => t.Recombination != null).Select(t => (s.First.Item1, t)));
                else
                    templates = parameters.Groups.Where(s => s.Item1.ToLower() != "decoy").SelectMany(g => g.Item2.SelectMany(s => s.Templates.Select(t => (g.Item1, t))));

                // See if the number of results match up
                if (templates.Count() != runVariables.ExpectedResult.Count) {
                    Console.Error.WriteLine($"Number of results ({templates.Count()}) not equal to number of expected results ({runVariables.ExpectedResult.Count}).");
                    return;
                } else {
                    // Give the scoring result for each result
                    foreach (var (expected, (group, result)) in runVariables.ExpectedResult.Zip(templates)) {
                        var expected_read = new Read.Simple(AminoAcid.FromString(expected, result.Parent.Alphabet).Unwrap());
                        var actual_read = new Read.Simple(result.ConsensusSequence().Item1.SelectMany(i => i.Sequence).ToArray());
                        var match = new Alignment(expected_read, actual_read, result.Parent.Alphabet, AlignmentType.Local);
                        var id = HTMLNameSpace.CommonPieces.GetAsideIdentifier(result.MetaData, true);
                        buffer.Append(JSONBlock($"{Runname}/{group}/{id} - Score", "Score", match.Score.ToString(), match.VeryShortPath()));
                        buffer.Append(JSONBlock($"{Runname}/{group}/{id} - Identity", "Percent", (match.PercentIdentity() * 100).ToString("G3")));
                    }

                    // Add this information to the file, appending where needed while keeping the format correct. Note: the list will not be closed this will need to be done afterwards.
                    const string file_name = "benchmark-output.json";
                    if (File.Exists(file_name))
                        File.AppendAllText(file_name, buffer.ToString());
                    else
                        File.WriteAllText(file_name, "[" + buffer.ToString().TrimStart(','));
                }
            }

            List<(string, List<Segment>)> RunTemplateMatching() {
                // Initialise the matches list with empty lists for every input read
                var matches = new List<List<(int GroupIndex, int SegmentIndex, int TemplateIndex, Alignment Match)>>(Input.Data.Cleaned.Count);
                for (int i = 0; i < Input.Data.Cleaned.Count; i++) matches.Add(new List<(int, int, int, Alignment)>());

                var segments = new List<(string, List<Segment>)>();
                for (int i = 0; i < TemplateMatching.Segments.Count; i++) {
                    var current_group = new List<Segment>();
                    for (int j = 0; j < TemplateMatching.Segments[i].Segments.Count; j++) {
                        var segment = TemplateMatching.Segments[i].Segments[j];
                        var alph = segment.Alphabet ?? TemplateMatching.Alphabet;
                        current_group.Add(new Segment(segment.Templates, alph, segment.Name, segment.CutoffScore == 0 ? TemplateMatching.CutoffScore : segment.CutoffScore, j, TemplateMatching.ForceGermlineIsoleucine, segment.Scoring));
                    }
                    segments.Add((TemplateMatching.Segments[i].Name, current_group));
                }

                var jobs = new List<(int segmentID, int innerSegmentID)>();

                for (int i = 0; i < TemplateMatching.Segments.Count; i++)
                    for (int j = 0; j < TemplateMatching.Segments[i].Segments.Count; j++)
                        jobs.Add((i, j));

                void ExecuteJob((int, int) job) {
                    var (i, j) = job;
                    var segment1 = segments[i].Item2[j];

                    // These contain all matches for all Input reads (outer list) for all templates (inner list) with the score
                    var local_matches = segment1.Match(Input.Data.Cleaned);

                    for (int read = 0; read < Input.Data.Cleaned.Count; read++)
                        matches[read].AddRange(local_matches[read].Select(a => (i, j, a.TemplateIndex, a.Match)));
                }

                Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = MaxNumberOfCPUCores }, job => ExecuteJob(job));

                // Save the progress, finished TemplateMatching
                if (ProgressBar != null) ProgressBar.Update();

                // Filter matches if Forced
                EnforceUnique(matches, TemplateMatching.EnforceUnique);

                // Add all matches to the right templates
                foreach (var row in matches) {
                    var unique = row.Count == 1;
                    foreach (var match in row) {
                        segments[match.GroupIndex].Item2[match.SegmentIndex].Templates[match.TemplateIndex].AddMatch(match.Match, unique);
                    }
                }

                foreach (var group in segments) {
                    foreach (var segment in group.Item2) {
                        if (segment.Hierarchy == null) continue;
                        segment.ScoreHierarchy = new PhylogeneticTree.ProteinHierarchyTree(segment.Hierarchy, segment.Templates.SelectMany(t => t.Matches).ToList());
                    }
                }

                return segments;
            }

            void RunRecombine(List<(string, List<Segment>)> segments, List<Segment> recombined_segment) {
                var matches = new List<List<(int GroupIndex, int, int TemplateIndex, Alignment Match)>>(Input.Data.Cleaned.Count);
                for (int i = 0; i < Input.Data.Cleaned.Count; i++) matches.Add(new List<(int, int, int, Alignment)>());

                var alph = Recombine.Alphabet ?? TemplateMatching.Alphabet;
                var name_filter = new NameFilter();
                bool forceGermlineIsoleucine = HelperFunctionality.EvaluateTrilean(Recombine.ForceGermlineIsoleucine, TemplateMatching.ForceGermlineIsoleucine);

                int offset = 0;
                for (int segment_group_index = 0; segment_group_index < segments.Count; segment_group_index++) {
                    var segment_group = segments[segment_group_index];
                    if (segment_group.Item1.ToLower() == "decoy") { offset += 1; continue; };
                    // Create a list for every segment with the top n highest scoring templates.
                    // By having the lowest of these always at the first position at shuffling in
                    // a new high scoring template to its right position this snippet is still
                    // approximately O(n) in respect to the segment. Worst case O(top_n * l_segment)
                    var top = new List<List<Template>>();
                    var decoy = new List<Template>();
                    foreach (var db in segment_group.Item2) {
                        db.Templates.Sort((a, b) => b.Score.CompareTo(a.Score));
                        top.Add(db.Templates.Take(Recombine.N).ToList());
                        // Add all missed templates (score too low) to the decoy set if the Decoy option is turned on
                        if (Recombine.Decoy)
                            foreach (var template in db.Templates.Skip(Recombine.N))
                                decoy.Add(new Template(template.Name, template.Sequence, template.MetaData, db, forceGermlineIsoleucine, new TemplateLocation(-1, decoy.Count)));
                    }

                    // Recombine high scoring templates
                    // https://rosettacode.org/wiki/Cartesian_product_of_two_or_more_lists#C.23
                    IEnumerable<IEnumerable<Template>> empty = new[] { Enumerable.Empty<Template>() };

                    var combinations = top.Aggregate(
                        empty,
                        (accumulator, sequence) =>
                        from acc in accumulator
                        from item in sequence
                        select acc.Concat(new[] { item }));

                    var recombined_segment_group = new Segment(new List<Template>(), alph, "Recombined Segment", Recombine.CutoffScore);
                    recombined_segment_group.SegmentJoiningScores = CreateRecombinationTemplates(combinations, Recombine.Order[segment_group_index - offset], alph, recombined_segment_group, name_filter);
                    if (Recombine.Decoy) recombined_segment_group.Templates.AddRange(decoy);

                    var local_matches = recombined_segment_group.Match(Input.Data.Cleaned);

                    for (int read = 0; read < Input.Data.Cleaned.Count; read++)
                        matches[read].AddRange(local_matches[read].Select(a => (segment_group_index, 0, a.TemplateIndex, a.Match)));

                    recombined_segment.Add(recombined_segment_group);
                }

                // Aggregate all decoy sets from template matching
                var general_decoy = new List<Template>();
                foreach (var seg_group in segments) {
                    // Groups called "Decoy" will be added in full
                    if (seg_group.Item1.ToLower() == "decoy")
                        foreach (var segment in seg_group.Item2)
                            foreach (var template in segment.Templates)
                                general_decoy.Add(new Template(template.Name, template.Sequence, template.MetaData, segment, forceGermlineIsoleucine, new TemplateLocation(-1, general_decoy.Count)));

                    // If a segment is added in the outer scope called "Decoy" it is added as well
                    if (string.IsNullOrEmpty(seg_group.Item1))
                        foreach (var segment in seg_group.Item2)
                            if (segment.Name.ToLower() == "decoy")
                                foreach (var template in segment.Templates)
                                    general_decoy.Add(new Template(template.Name, template.Sequence, template.MetaData, segment, forceGermlineIsoleucine, new TemplateLocation(-1, general_decoy.Count)));
                }
                if (general_decoy.Count > 0) {
                    var segment = new Segment(general_decoy, alph, "General Decoy Segment", Recombine.CutoffScore);
                    var local_matches = segment.Match(Input.Data.Cleaned);

                    for (int read = 0; read < Input.Data.Cleaned.Count; read++)
                        matches[read].AddRange(local_matches[read].Select(a => (recombined_segment.Count, 0, a.TemplateIndex, a.Match)));

                    recombined_segment.Add(segment);
                }

                // Filter matches if Forced
                EnforceUnique(matches, Recombine.EnforceUnique.Unwrap(TemplateMatching.EnforceUnique));

                // Add all matches to the right templates
                foreach (var row in matches) {
                    var unique = row.Count == 1;
                    foreach (var match in row)
                        recombined_segment[match.GroupIndex].Templates[match.TemplateIndex].AddMatch(match.Match, unique);
                }

                // Did recombination
                if (ProgressBar != null) ProgressBar.Update();
            }

            // Also known as the CDR joining step
            List<(int Group, int Index, Alignment EndAlignment, Read.IRead SeqA, Read.IRead SeqB, Read.IRead Result, int Overlap)> CreateRecombinationTemplates(IEnumerable<IEnumerable<Stitch.Template>> combinations, List<RecombineOrder.OrderPiece> order, ScoringMatrix alphabet, Segment parent, NameFilter name_filter) {
                var recombined_templates = new List<Template>();
                var scores = new List<(int Group, int Index, Alignment EndAlignment, Read.IRead SeqA, Read.IRead SeqB, Read.IRead Result, int Overlap)>();

                for (int i = 0; i < combinations.Count(); i++) {
                    var sequence = combinations.ElementAt(i);
                    var s = new List<AminoAcid>();
                    var s_doc = new List<double>();
                    var t = new List<Template>();
                    var join = false;
                    foreach (var element in order) {
                        if (element.GetType() == typeof(RecombineOrder.Gap)) {
                            // When the templates are aligned with a gap (a * in the Order definition) the overlap between the two templates is found
                            // and removed from the Template sequence for the recombine round.
                            join = true;
                        } else {
                            var index = ((RecombineOrder.Template)element).Index;
                            var seq_consensus = sequence.ElementAt(index).ConsensusSequence();
                            var seq = seq_consensus.Item1.Zip(seq_consensus.Item2).SelectMany(i => i.First.Sequence);
                            var seq_doc = seq_consensus.Item1.Zip(seq_consensus.Item2).SelectMany(i => Enumerable.Repeat(i.Second, i.First.Length));
                            if (join) {
                                const int padding = 5;
                                // When the templates are aligned with a gap (a * in the Order definition) the overlap between the two templates is found
                                // and removed from the Template sequence for the recombine round.
                                join = false;
                                var inserted_cdr_s = s.Count;
                                var inserted_cdr_seq = seq.Count();
                                s = s.TakeWhile(a => a.Character != 'X').ToList();
                                seq = seq.SkipWhile(a => a.Character == 'X').ToList();
                                inserted_cdr_s -= s.Count;
                                inserted_cdr_seq -= seq.Count();
                                inserted_cdr_s = 20 - inserted_cdr_s;
                                inserted_cdr_seq = 20 - inserted_cdr_seq;
                                s_doc = s_doc.SkipLast(inserted_cdr_s).ToList();
                                seq_doc = seq_doc.Skip(inserted_cdr_seq).ToList();
                                var aligned_template = new Alignment(
                                    new Read.Simple(s.TakeLast(inserted_cdr_s + padding).ToArray(), null, null, "R", s_doc.TakeLast(inserted_cdr_s + padding).ToArray()),
                                    new Read.Simple(seq.Take(inserted_cdr_seq + padding).ToArray(), null, null, "R", seq_doc.Take(inserted_cdr_seq + padding).ToArray()),
                                    alphabet, AlignmentType.EndAlignment);
                                var original_s = new Read.Simple(s.ToArray(), null, null, "R", s_doc.ToArray());
                                var original_seq = new Read.Simple(seq.ToArray(), null, null, "R", seq_doc.ToArray());
                                var overlap = 0;

                                if (aligned_template.Score > 0) {
                                    s = s.SkipLast(aligned_template.LenA).ToList();
                                    // Add the consensus of the alignment
                                    // Gaps are inserted when its supporting score is bigger than the minimal supporting score of the amino acids on both side on the other sequence
                                    // Matches are chosen based on the depth of coverage of both, if the DOC is the same an 'X' is inserted if defined otherwise the first is chosen
                                    // Special cases are chosen based on the depth of coverage, if the DOC is the same the first is chosen.
                                    int pos_a = aligned_template.StartA;
                                    int pos_b = aligned_template.StartB;
                                    foreach (var step in aligned_template.Path) {
                                        if (step.StepA == 0) {
                                            var la = aligned_template.ReadA.Sequence.PositionalScore.Length - pos_a - 1;
                                            var doc_a = la == 0 ? 0 : aligned_template.ReadA.Sequence.PositionalScore.SubArray(pos_a, Math.Min(2, la)).Min();
                                            var doc_b = aligned_template.ReadB.Sequence.PositionalScore[pos_b];
                                            if (doc_b >= doc_a) {
                                                s.Add(aligned_template.ReadB.Sequence.Sequence[pos_b]);
                                                overlap++;
                                            }
                                        } else if (step.StepB == 0) {
                                            var doc_a = aligned_template.ReadA.Sequence.PositionalScore[pos_a];
                                            var lb = aligned_template.ReadB.Sequence.PositionalScore.Length - pos_b - 1;
                                            var doc_b = lb == 0 ? 0 : aligned_template.ReadB.Sequence.PositionalScore.SubArray(pos_b, Math.Min(2, lb)).Min();
                                            if (doc_a >= doc_b) {
                                                s.Add(aligned_template.ReadA.Sequence.Sequence[pos_a]);
                                                overlap++;
                                            }
                                        } else if (step.StepA == 1 && step.StepB == 1) {
                                            if (aligned_template.ReadA.Sequence.Sequence[pos_a] == aligned_template.ReadB.Sequence.Sequence[pos_b]) {
                                                s.Add(aligned_template.ReadA.Sequence.Sequence[pos_a]);
                                            } else {
                                                var doc_a = aligned_template.ReadA.Sequence.PositionalScore[pos_a];
                                                var doc_b = aligned_template.ReadB.Sequence.PositionalScore[pos_b];
                                                if (doc_a == doc_b) {
                                                    s.Add(alphabet.Contains('X') ? new AminoAcid(alphabet, 'X') : aligned_template.ReadA.Sequence.Sequence[pos_a]);
                                                } else if (doc_a > doc_b) {
                                                    s.Add(aligned_template.ReadA.Sequence.Sequence[pos_a]);
                                                } else {
                                                    s.Add(aligned_template.ReadB.Sequence.Sequence[pos_b]);
                                                }
                                            }
                                            overlap++;
                                        } else {
                                            var doc_a = aligned_template.ReadA.Sequence.PositionalScore.SubArray(pos_a, step.StepA).Average();
                                            var doc_b = aligned_template.ReadB.Sequence.PositionalScore.SubArray(pos_b, step.StepB).Average();
                                            if (doc_a == doc_b) {
                                                s.AddRange(aligned_template.ReadA.Sequence.Sequence.SubArray(pos_a, step.StepA));
                                                overlap += step.StepA;
                                            } else if (doc_a > doc_b) {
                                                s.AddRange(aligned_template.ReadA.Sequence.Sequence.SubArray(pos_a, step.StepA));
                                                overlap += step.StepA;
                                            } else {
                                                s.AddRange(aligned_template.ReadB.Sequence.Sequence.SubArray(pos_b, step.StepB));
                                                overlap += step.StepB;
                                            }
                                        }
                                        pos_a += step.StepA;
                                        pos_b += step.StepB;
                                    }
                                    s.AddRange(seq.Skip(aligned_template.LenB));
                                } else {
                                    // When no good overlap is found just paste them one after the other
                                    s.Add(new AminoAcid(alphabet, alphabet.GapChar));
                                    s.AddRange(seq);
                                }
                                sequence.ElementAt(index).Overlap = overlap;
                                var final_s = new Read.Simple(s.ToArray(), null, null, "R", s_doc.ToArray());
                                scores.Add((i, index, aligned_template, original_s, original_seq, final_s, overlap));
                            } else {
                                s.AddRange(seq);
                                s_doc.AddRange(seq_doc);
                            }
                            t.Add(sequence.ElementAt(((RecombineOrder.Template)element).Index));
                        }
                    }
                    recombined_templates.Add(
                        new Template(
                            "recombined",
                            s.ToArray(),
                            new Read.Simple(s.ToArray(), null, name_filter, $"REC-{parent.Index}-{i + 1}"),
                            parent,
                            HelperFunctionality.EvaluateTrilean(Recombine.ForceGermlineIsoleucine, TemplateMatching.ForceGermlineIsoleucine),
                            new RecombinedTemplateLocation(i), t));
                }
                parent.Templates = recombined_templates;
                return scores;
            }

            static void EnforceUnique(List<List<(int, int, int, Alignment Match)>> matches, double unique_threshold) {
                if (matches == null || unique_threshold == 0.0) return;
                for (int read_index = 0; read_index < matches.Count; read_index++) {
                    var best = new List<(int, int, int, Alignment Match)>();
                    var best_score = 0;
                    if (matches[read_index] == null) continue;
                    for (int template_index = 0; template_index < matches[read_index].Count; template_index++) {
                        var match = matches[read_index][template_index];
                        if (match.Match.Score > best_score) {
                            best = best.Where(m => m.Match.Score >= match.Match.Score * unique_threshold).ToList();
                            best.Add(match);
                            best_score = match.Match.Score;
                        } else if (match.Match.Score >= best_score * unique_threshold) {
                            best.Add(match);
                        }
                    }
                    if (best.Count == 1) best[0].Item4.Unique = true;
                    matches[read_index].Clear();
                    matches[read_index].AddRange(best);
                }
            }
        }
    }
}
