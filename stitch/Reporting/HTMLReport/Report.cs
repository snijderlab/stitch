using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlGenerator;
using HTMLNameSpace;
using static HTMLNameSpace.CommonPieces;
using static Stitch.HelperFunctionality;

namespace Stitch {
    /// <summary> An HTML report. </summary>
    public class HTMLReport : Report {
        /// <summary> The name of the assets folder </summary>
        public string AssetsFolderName;
        string FullAssetsFolderName;
        public RunParameters.Report.HTML ReportParameter;

        public HTMLReport(ReportInputParameters Parameters, int maxThreads, RunParameters.Report.HTML reportParameter) : base(Parameters, maxThreads) {
            ReportParameter = reportParameter;
        }

        public override string Create() {
            throw new Exception("HTML reports should be generated using the 'Save' function.");
        }

        /// <summary> Generates a list of asides for details viewing. </summary>
        public void CreateAsides() {
            var job_buffer = new List<(AsideType, int, int, int)>();

            // Read Asides
            for (int i = 0; i < Parameters.Input.Count; i++) {
                job_buffer.Add((AsideType.Read, -1, -1, i));
            }
            // Template Tables Asides
            if (Parameters.Groups != null) {
                for (int i = 0; i < Parameters.Groups.Count; i++)
                    for (int j = 0; j < Parameters.Groups[i].Item2.Count; j++)
                        for (int k = 0; k < Parameters.Groups[i].Item2[j].Templates.Count; k++)
                            job_buffer.Add((AsideType.Template, i, j, k));
            }
            // Recombination Table Asides
            if (Parameters.RecombinedSegment != null) {
                for (int i = 0; i < Parameters.RecombinedSegment.Count; i++)
                    for (int j = 0; j < Parameters.RecombinedSegment[i].Templates.Count; j++)
                        job_buffer.Add((AsideType.RecombinedTemplate, i, -1, j));
            }

            if (MaxThreads > 1) {
                Parallel.ForEach(
                    job_buffer,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (a, _) => CreateAndSaveAside(a.Item1, a.Item2, a.Item3, a.Item4)
                );
            } else {
                foreach (var (t, i3, i2, i1) in job_buffer) {
                    CreateAndSaveAside(t, i3, i2, i1);
                }
            }
        }

        static object ErrorPrintingLock = new Object();
        void CreateAndSaveAside(AsideType aside, int index3, int index2, int index1) {
            try {
                HtmlBuilder inner_html;

                ReadFormat.General metadata;
                switch (aside) {
                    case AsideType.Read:
                        inner_html = HTMLAsides.CreateReadAside(Parameters.Input[index1], Parameters.Groups, Parameters.RecombinedSegment, AssetsFolderName);
                        metadata = Parameters.Input[index1];
                        break;
                    case AsideType.Template:
                        var template = Parameters.Groups[index3].Item2[index2].Templates[index1];
                        inner_html = HTMLAsides.CreateTemplateAside(template, AsideType.Template, AssetsFolderName, Parameters.Input.Count);
                        metadata = template.MetaData;
                        break;
                    case AsideType.RecombinedTemplate:
                        var rTemplate = Parameters.RecombinedSegment[index3].Templates[index1];
                        inner_html = HTMLAsides.CreateTemplateAside(rTemplate, AsideType.RecombinedTemplate, AssetsFolderName, Parameters.Input.Count);
                        metadata = rTemplate.MetaData;
                        break;
                    default:
                        throw new InvalidEnumArgumentException("Tried to generated an aside page for a non existent type of aside. Please report this error.");
                };
                var location = new List<string>() { AssetsFolderName, GetAsideName(aside) + "s" };
                var home_location = GetLinkToFolder(new List<string>(), location) + AssetsFolderName + ".html";
                var id = GetAsideIdentifier(metadata);
                var link = GetLinkToFolder(location, new List<string>());
                var full_path = Path.Join(Path.GetDirectoryName(FullAssetsFolderName), link) + id.Replace(':', '-') + ".html";

                var html = new HtmlBuilder();
                html.UnsafeContent("<!DOCTYPE html>");
                html.Open(HtmlTag.html, "lang='en-GB' xml:lang='en-GB'");
                html.Add(CreateHeader("Details " + id, location));
                html.Open(HtmlTag.body, "class='details' onload='Setup()'");
                html.OpenAndClose(HtmlTag.a, $"href='{home_location}' class='overview-link'", "Overview");
                html.OpenAndClose(HtmlTag.a, "href='#' id='back-button' class='overview-link' style='display:none;' onclick='GoBack()'", "Undefined");
                html.Add(inner_html);
                html.Close(HtmlTag.body);
                html.Close(HtmlTag.html);

                SaveAndCreateDirectories(full_path, html.ToString());
            } catch (Exception e) {
                lock (ErrorPrintingLock) {
                    InputNameSpace.ErrorMessage.PrintException(e, false);
                    throw new Exception("Exception raised in creation of aside. See above message for more details.");
                }
            }
        }

        HtmlBuilder CreateCDROverview(string id, List<Segment> segments) {
            var cdr1_reads = new List<(ReadFormat.General MetaData, ReadFormat.General Template, string Sequence, bool Unique)>();
            var cdr2_reads = new List<(ReadFormat.General MetaData, ReadFormat.General Template, string Sequence, bool Unique)>();
            var cdr3_reads = new List<(ReadFormat.General MetaData, ReadFormat.General Template, string Sequence, bool Unique)>();
            int total_templates = 0;
            bool found_cdr_region = false;

            foreach (var template in segments.SelectMany(a => a.Templates)) {
                var positions = new Dictionary<Annotation, (int Start, int Length)>();
                int cumulative = 0;
                int position = 0;
                if (template.MetaData is ReadFormat.Fasta fasta && fasta != null) {
                    foreach (var piece in fasta.AnnotatedSequence) {
                        if (piece.Type.IsAnyCDR()) {
                            if (positions.ContainsKey(piece.Type)) {
                                var values = positions[piece.Type];
                                positions[piece.Type] = (values.Item1, values.Item2 + cumulative + piece.Sequence.Length);
                            } else {
                                positions.Add(piece.Type, (position, piece.Sequence.Length));
                            }
                            cumulative = 0;
                            total_templates++;
                        } else {
                            cumulative += piece.Sequence.Length;
                        }
                        position += piece.Sequence.Length;
                    }
                }

                if (positions.ContainsKey(Annotation.CDR1)) { // V-segment
                    found_cdr_region = true;
                    foreach (var read in template.Matches) {
                        foreach (var (group, cdr) in positions) {
                            if (read.StartA < cdr.Start + cdr.Length && read.StartA + read.LenA > cdr.Start) {
                                var piece = (read.ReadB, template.MetaData, AminoAcid.ArrayToString(read.GetQuerySubMatch(cdr.Start, cdr.Length)), read.Unique);
                                switch (group) {
                                    case Annotation.CDR1:
                                        cdr1_reads.Add(piece);
                                        break;
                                    case Annotation.CDR2:
                                        cdr2_reads.Add(piece);
                                        break;
                                    case Annotation.CDR3:
                                        cdr3_reads.Add(piece);
                                        break;
                                }
                            }
                        }
                    }
                } else if (positions.ContainsKey(Annotation.CDR3)) { // J-segment
                    found_cdr_region = true;
                    var cdr = positions[Annotation.CDR3];

                    foreach (var read in template.Matches) {
                        if (read.StartA < cdr.Start + cdr.Length && read.StartA + read.LenA > cdr.Start) {
                            cdr3_reads.Add((read.ReadB, template.MetaData, AminoAcid.ArrayToString(read.GetQuerySubMatch(cdr.Start, cdr.Length)), read.Unique));
                        }
                    }
                }
            }

            if (!found_cdr_region) return new HtmlBuilder(); // Do not create a collapsable segment if no CDR region could be found in the templates

            string extend(string sequence, int size) {
                if (sequence.Length < size) {
                    int pos = (int)Math.Ceiling((double)sequence.Length / 2);
                    return sequence.Substring(0, pos) + new string('~', size - sequence.Length) + sequence.Substring(pos);
                } else if (sequence.Length > size) {
                    if (sequence.StartsWith('.'))
                        return sequence.Substring(sequence.Length - size);
                    else if (sequence.EndsWith('.'))
                        return sequence.Substring(0, size);
                }
                return sequence;
            }

            if (cdr1_reads.Count > 0) {
                int cdr1_length = Math.Min(11, cdr1_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr1_reads.Count; i++) {
                    cdr1_reads[i] = (cdr1_reads[i].MetaData, cdr1_reads[i].Template, extend(cdr1_reads[i].Sequence, cdr1_length), cdr1_reads[i].Unique);
                }
            }
            if (cdr2_reads.Count > 0) {
                int cdr2_length = Math.Min(11, cdr2_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr2_reads.Count; i++) {
                    cdr2_reads[i] = (cdr2_reads[i].MetaData, cdr2_reads[i].Template, extend(cdr2_reads[i].Sequence, cdr2_length), cdr2_reads[i].Unique);
                }
            }
            if (cdr3_reads.Count > 0) {
                int cdr3_length = Math.Min(13, cdr3_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr3_reads.Count; i++) {
                    cdr3_reads[i] = (cdr3_reads[i].MetaData, cdr3_reads[i].Template, extend(cdr3_reads[i].Sequence, cdr3_length), cdr3_reads[i].Unique);
                }
            }

            var html = new HtmlBuilder();
            html.OpenAndClose(HtmlTag.p, "", "All reads matching any Template within the CDR regions are listed here. These all stem from the alignments made in the TemplateMatching step.");
            if (cdr1_reads.Count == 0 && cdr2_reads.Count == 0 && cdr3_reads.Count == 0)
                html.OpenAndClose(HtmlTag.p, "", "No CDR reads could be placed.");
            else {
                html.Open(HtmlTag.div, "class='cdr-tables'");
                if (cdr1_reads.Count > 0) html.Add(HTMLTables.CDRTable(cdr1_reads, AssetsFolderName, "CDR1", Parameters.Input.Count, total_templates));
                if (cdr2_reads.Count > 0) html.Add(HTMLTables.CDRTable(cdr2_reads, AssetsFolderName, "CDR2", Parameters.Input.Count, total_templates));
                if (cdr3_reads.Count > 0) html.Add(HTMLTables.CDRTable(cdr3_reads, AssetsFolderName, "CDR3", Parameters.Input.Count, total_templates));
                html.Close(HtmlTag.div);
            }
            var outer = new HtmlBuilder();
            outer.Collapsible(id, new HtmlBuilder("CDR regions"), html);
            return outer;
        }

        private HtmlBuilder CreateHeader(string title, List<string> location) {
            var link = GetLinkToFolder(new List<string>() { AssetsFolderName }, location);
            var assets_folder = link;
            if (Parameters.runVariables.LiveServer != -1)
                link = $"http://localhost:{Parameters.runVariables.LiveServer}/assets/";
            var html = new HtmlBuilder();
            html.Open(HtmlTag.head);
            html.Empty(HtmlTag.meta, "charset='utf-8'");
            html.Empty(HtmlTag.meta, "name='viewport' content='width=device-width, initial-scale=1.0'");
            html.Empty(HtmlTag.meta, "name='google' content='notranslate'");
            html.Empty(HtmlTag.meta, "http-equiv='Content-Language' content='en'");
            html.Empty(HtmlTag.link, $"rel='icon' href='{assets_folder}favicon.ico' type='image/x-icon'");
            html.OpenAndClose(HtmlTag.title, "", title + " | Stitch");
            html.Open(HtmlTag.style);
            html.UnsafeContent($@"@font-face {{
  font-family: Roboto;
  src: url({link}Roboto-Regular.ttf);
  font-weight: normal;
}}
@font-face {{
  font-family: Roboto;
  src: url({link}Roboto-Medium.ttf);
  font-weight: 500;
}}
@font-face {{
  font-family: 'Roboto Mono';
  src: url({link}RobotoMono-VariableFont.ttf);
  font-weight: 100 700;
  font-style: normal;
}}");
            html.Close(HtmlTag.style);
            html.Open(HtmlTag.script);
            html.UnsafeContent($"assets_folder = '{AssetsFolderName}';");
            html.Close(HtmlTag.script);
            html.OpenAndClose(HtmlTag.script, $"src='{link}script.js'", "");
            html.Empty(HtmlTag.link, $"rel='stylesheet' href='{link}styles.css'");
            html.Close(HtmlTag.head);
            return html;
        }

        private HtmlBuilder BatchFileHTML() {
            if (BatchFile != null) {
                string Render(string line) {
                    if (line.Trim().StartsWith('-'))
                        return $"<span class='comment'>{line}</span><br>";

                    var open = Regex.Match(line, @"^(\s*)([\w ]+)(\s*)(-|:)>$");
                    if (open.Success)
                        return $"{open.Groups[1]}<span class='id'>{open.Groups[2]}</span>{open.Groups[3]}<span class='op'>{open.Groups[4]}&gt;</span><br>";

                    var single = Regex.Match(line, @"^(\s*)([\w ]+)(\s*):(.+)$");
                    if (single.Success)
                        return $"{single.Groups[1]}<span class='id'>{single.Groups[2]}</span>{single.Groups[3]}<span class='op'>:</span><span class='value'>{single.Groups[4]}</span><br>";

                    var close = Regex.Match(line, @"^(\s*)<(-|:)$");
                    if (close.Success)
                        return $"{close.Groups[1]}<span class='op'>&lt;{close.Groups[2]}</span><br>";

                    var include = Regex.Match(line, @"^(\s*)include!\((.*)\)$");
                    if (include.Success)
                        return $"{include.Groups[1]}<span class='directive'>include</span><span class='op'>!(</span><span class='value'>{include.Groups[2]}</span><span class='op'>)</span><br>";

                    return line + "<br>";
                }

                var html = new HtmlBuilder();
                var bf = BatchFile;
                html.Open(HtmlTag.code);
                html.OpenAndClose(HtmlTag.i, $"title='SHA256 checksum: {bf.Identifier.Checksum}'", bf.Identifier.Path);
                html.Empty(HtmlTag.br);
                foreach (var line in bf.Lines) html.UnsafeContent(Render(line.TrimEnd()));
                html.Close(HtmlTag.code);

                foreach (var file in IncludedFiles) {
                    html.Open(HtmlTag.code);
                    html.OpenAndClose(HtmlTag.i, $"title='SHA256 checksum: {file.Identifier.Checksum}'", file.Identifier.Path);
                    html.Empty(HtmlTag.br);
                    foreach (var line in file.Lines) html.UnsafeContent(Render(line.TrimEnd()));
                    html.Close(HtmlTag.code);
                }

                return html;
            } else {
                return new HtmlBuilder(HtmlTag.em, "No BatchFile");
            }
        }

        /// <summary> Create the overview section of the main page. </summary>
        private HtmlBuilder CreateOverview() {
            var html = new HtmlBuilder();
            if (Parameters.RecombinedSegment.Count != 0) {
                if (Parameters.RecombinedSegment.Count <= 3) {
                    // If the number of groups is small show details about the best scoring recombined template for each group
                    for (int group = 0; group < Parameters.Groups.Count && group < Parameters.RecombinedSegment.Count; group++) {
                        if (Parameters.Groups[group].Item1.ToLower() == "decoy") continue;
                        var template = Parameters.RecombinedSegment[group].Templates[0];
                        var (seq, doc) = template.ConsensusSequence();
                        html.Open(HtmlTag.h2);
                        html.OpenAndClose(HtmlTag.a, $"href='{GetAsideRawLink(template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName)}' target='_blank'", Parameters.Groups[group].Item1);
                        html.Close(HtmlTag.h2);
                        html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(seq.SelectMany(i => i.Sequence)));
                        html.Open(HtmlTag.div, "class='doc-plot'");
                        html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(doc), new HtmlGenerator.HtmlBuilder("Depth of Coverage"), null, null, 10, template.ConsensusSequenceAnnotation()));
                        html.Close(HtmlTag.div);
                        html.OpenAndClose(HtmlTag.h3, "", "Best scoring segments");
                        html.Open(HtmlTag.p);

                        for (int segment = 0; segment < Parameters.Groups[group].Item2.Count; segment++) {
                            var seg = Parameters.Groups[group].Item2[segment];
                            if (seg.Templates.Count > 0)
                                html.Add(GetAsideLinkHtml(seg.Templates[0].MetaData, AsideType.Template, AssetsFolderName));
                        }
                        html.Close(HtmlTag.p);
                    }
                } else {
                    // If the number of groups is large show a table with each highest scoring and highest area recombined template
                    html.OpenAndClose(HtmlTag.h2, "", "Overview of all recombined segments");
                    html.Open(HtmlTag.table, "class='wide-table'");
                    html.Open(HtmlTag.tr);
                    html.OpenAndClose(HtmlTag.th, "", "Segment");
                    html.OpenAndClose(HtmlTag.th, "", "Highest Score");
                    html.TagWithHelp(HtmlTag.th, "Score", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateScore));
                    html.TagWithHelp(HtmlTag.th, "Order", new HtmlBuilder(HtmlTag.p, HTMLHelp.Order));
                    html.OpenAndClose(HtmlTag.th, "", "Highest Area");
                    html.TagWithHelp(HtmlTag.th, "Area", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateTotalArea));
                    html.TagWithHelp(HtmlTag.th, "Order", new HtmlBuilder(HtmlTag.p, HTMLHelp.Order));
                    html.Close(HtmlTag.tr);
                    for (int group = 0; group < Parameters.Groups.Count && group < Parameters.RecombinedSegment.Count; group++) {
                        if (Parameters.Groups[group].Item1.ToLower() == "decoy") continue;
                        var highest_score = Parameters.RecombinedSegment[group].Templates.MaxBy(t => t.Score);
                        var highest_area = Parameters.RecombinedSegment[group].Templates.MaxBy(t => t.TotalArea);
                        html.Open(HtmlTag.tr);
                        html.OpenAndClose(HtmlTag.td, "class='center'", Parameters.Groups[group].Item1);
                        html.OpenAndClose(HtmlTag.td, "class='center'", CommonPieces.GetAsideLinkHtml(highest_score.MetaData, AsideType.RecombinedTemplate, AssetsFolderName));
                        html.OpenAndClose(HtmlTag.td, "class='center'", highest_score.Score.ToString("G4"));
                        html.Open(HtmlTag.td);
                        var first = true;
                        foreach (var seg in highest_score.Recombination) {
                            if (!first) html.Content(" → ");
                            first = false;
                            html.Add(GetAsideLinkHtml(seg.MetaData, AsideType.Template, AssetsFolderName));
                        }
                        html.Close(HtmlTag.td);

                        html.OpenAndClose(HtmlTag.td, "class='center'", CommonPieces.GetAsideLinkHtml(highest_area.MetaData, AsideType.RecombinedTemplate, AssetsFolderName));
                        html.OpenAndClose(HtmlTag.td, "class='center'", highest_area.TotalArea.ToString("G4"));
                        html.Open(HtmlTag.td);
                        first = true;
                        foreach (var seg in highest_score.Recombination) {
                            if (!first) html.Content(" → ");
                            first = false;
                            html.Add(GetAsideLinkHtml(seg.MetaData, AsideType.Template, AssetsFolderName));
                        }
                        html.Close(HtmlTag.td);
                        html.Close(HtmlTag.tr);
                    }
                    html.Close(HtmlTag.table);
                }
            } else {
                html.OpenAndClose(HtmlTag.h2, "", "Overview of all segments");
                if (Parameters.Groups.Count <= 3) {
                    // If the number of groups is small show details about the highest scoring template for each segment
                    for (int group = 0; group < Parameters.Groups.Count; group++) {
                        html.OpenAndClose(HtmlTag.h3, "", Parameters.Groups[group].Name);

                        for (int segment = 0; segment < Parameters.Groups[group].Segments.Count; segment++) {
                            var template = Parameters.Groups[group].Segments[segment].Templates[0];
                            var (seq, doc) = template.ConsensusSequence();
                            html.Open(HtmlTag.h3);
                            html.OpenAndClose(HtmlTag.a, "", CommonPieces.GetAsideLinkHtml(template.MetaData, AsideType.Template, AssetsFolderName));
                            html.Close(HtmlTag.h3);
                            html.Open(HtmlTag.div, "class='doc-plot'");
                            html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(doc), new HtmlGenerator.HtmlBuilder("Depth of Coverage"), null, null, 10, template.ConsensusSequenceAnnotation()));
                            html.Close(HtmlTag.div);
                        }
                    }
                } else {
                    // If the number of groups is large show a table with each highest scoring and highest area template
                    for (int group = 0; group < Parameters.Groups.Count; group++) {
                        if (Parameters.Groups[group].Name.ToLower() == "decoy") continue;
                        html.OpenAndClose(HtmlTag.h3, "", Parameters.Groups[group].Name);
                        html.Open(HtmlTag.table, "class='wide-table'");
                        html.Open(HtmlTag.tr);
                        html.OpenAndClose(HtmlTag.th, "", "Segment");
                        html.OpenAndClose(HtmlTag.th, "", "Highest Score");
                        html.TagWithHelp(HtmlTag.th, "Score", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateScore));
                        html.OpenAndClose(HtmlTag.th, "", "Highest Area");
                        html.TagWithHelp(HtmlTag.th, "Area", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateTotalArea));
                        html.Close(HtmlTag.tr);
                        for (int segment = 0; segment < Parameters.Groups[group].Segments.Count; segment++) {
                            var highest_score = Parameters.Groups[group].Segments[segment].Templates.MaxBy(t => t.Score);
                            var highest_area = Parameters.Groups[group].Segments[segment].Templates.MaxBy(t => t.TotalArea);
                            html.Open(HtmlTag.tr);
                            html.OpenAndClose(HtmlTag.td, "class='center'", Parameters.Groups[group].Segments[segment].Name);
                            html.OpenAndClose(HtmlTag.td, "class='center'", CommonPieces.GetAsideLinkHtml(highest_score.MetaData, AsideType.Template, AssetsFolderName));
                            html.OpenAndClose(HtmlTag.td, "class='center'", highest_score.Score.ToString("G4"));

                            html.OpenAndClose(HtmlTag.td, "class='center'", CommonPieces.GetAsideLinkHtml(highest_area.MetaData, AsideType.Template, AssetsFolderName));
                            html.OpenAndClose(HtmlTag.td, "class='center'", highest_area.TotalArea.ToString("G4"));
                            html.Close(HtmlTag.tr);
                        }
                        html.Close(HtmlTag.table);
                    }
                }
            }
            return html;
        }

        private HtmlBuilder CreateSegmentJoining(int group) {
            const int padding = 3;
            var html = new HtmlBuilder();
            foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores) {
                var A = Parameters.Groups[group].Item2[set.Index - 1];
                var B = Parameters.Groups[group].Item2[set.Index];
                html.OpenAndClose(HtmlTag.h2, "", $"{A.Name} * {B.Name}");
                var aligned = set.EndAlignment.Aligned().Aligned.Split('\n');
                var seqA = AminoAcid.ArrayToString(set.SeqA.Sequence.AminoAcids.SubArray(set.SeqA.Sequence.Length - set.EndAlignment.LenA - padding, padding));
                var seqB = AminoAcid.ArrayToString(set.SeqB.Sequence.AminoAcids.SubArray(set.EndAlignment.LenB, padding));
                var bars_a = new HtmlBuilder();
                var bars_b = new HtmlBuilder();
                var max_doc = new double[] {
                    set.EndAlignment.ReadA.Sequence.PositionalScore.SubArray(set.EndAlignment.StartA, set.EndAlignment.LenA).Max(0),
                    set.EndAlignment.ReadB.Sequence.PositionalScore.SubArray(set.EndAlignment.StartB, set.EndAlignment.LenB).Max(0),
                    set.SeqA.Sequence.PositionalScore.SubArray(set.SeqA.Sequence.Length - set.EndAlignment.LenA - padding, padding).Max(0),
                    set.SeqB.Sequence.PositionalScore.SubArray(set.EndAlignment.LenB, padding).Max(0),
                }.Max();
                bars_a.Open(HtmlTag.div, $"class='joining a' style='--max:{max_doc}'");
                bars_b.Open(HtmlTag.div, $"class='joining b' style='--max:{max_doc}'");
                var pos_a = set.EndAlignment.StartA;
                var pos_b = set.EndAlignment.StartB;
                for (int i = 0; i < padding; i++) {
                    bars_a.OpenAndClose(HtmlTag.span, $"class='bar' style='--doc:{set.SeqA.Sequence.PositionalScore[set.SeqA.Sequence.Length - set.EndAlignment.LenA - padding + i]}'", "");
                }
                foreach (var item in set.EndAlignment.Path) {
                    bars_a.OpenAndClose(HtmlTag.span, $"class='bar' style='--doc:{set.EndAlignment.ReadA.Sequence.PositionalScore[pos_a]}'", "");
                    bars_b.OpenAndClose(HtmlTag.span, $"class='bar' style='--doc:{set.EndAlignment.ReadB.Sequence.PositionalScore[pos_b]}'", "");
                }
                for (int i = 0; i < padding; i++) {
                    bars_b.OpenAndClose(HtmlTag.span, $"class='bar' style='--doc:{set.SeqB.Sequence.PositionalScore[set.EndAlignment.LenB + i]}'", "");
                }
                bars_a.Close(HtmlTag.div);
                bars_b.Close(HtmlTag.div);

                html.Add(bars_a);
                html.OpenAndClose(HtmlTag.pre, "class='seq'", $"...{seqA}{aligned[0]}\n   {new string(' ', padding)}{aligned[1]}{seqB}..."); // The seq B starts exactly 3 chars into seq A plus the padding for '...'
                html.Add(bars_b);
                html.OpenAndClose(HtmlTag.p, "", $"Best overlap ({set.EndAlignment.LenA}, {set.EndAlignment.LenB}) with score {set.EndAlignment.Score} which results in the following sequence:");
                html.OpenAndClose(HtmlTag.pre, "class='seq'", $"...{AminoAcid.ArrayToString(set.Result.Sequence.AminoAcids.SubArray(set.SeqA.Sequence.Length - set.EndAlignment.LenA - padding, set.Overlap + padding * 2))}..."); // The seq B starts exactly 3 chars into seq A plus the padding for '...'
            }
            return html;
        }

        private HtmlBuilder CreateMain() {
            var inner_html = new HtmlBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);
            var total_reads = Parameters.Input.Count;
            var total_area = total_reads == 0 ? 0 : Parameters.Input.Select(r => r.TotalArea).Sum();

            if (Parameters.Groups != null)
                for (int group = 0; group < Parameters.Groups.Count; group++) {
                    var group_html = new HtmlBuilder();
                    var id = Parameters.Groups[group].Item1.ToLower().Replace(' ', '-');

                    if (Parameters.RecombinedSegment.Count != 0) {
                        if (id == "decoy" && Parameters.Groups.Count > Parameters.RecombinedSegment.Count) continue;

                        var recombined = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).ToList();
                        var decoy = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).ToList();

                        if (recombined.Count > 0)
                            group_html.Collapsible(id + "-recombination", new HtmlBuilder("Recombination Table"), HTMLTables.CreateSegmentTable(id + "-recombination", recombined, null, AsideType.RecombinedTemplate, AssetFolderName, total_reads, total_area, true));

                        if (decoy.Count > 0)
                            group_html.Collapsible(id + "-recombination-decoy", new HtmlBuilder("Recombination Decoy"), HTMLTables.CreateSegmentTable(id + "-recombination-decoy", decoy, null, AsideType.RecombinedTemplate, AssetFolderName, total_reads, total_area, true));

                        if (Parameters.RecombinedSegment[group].SegmentJoiningScores.Count > 0)
                            group_html.Collapsible(id + "-segment-joining", new HtmlBuilder("Segment joining"), CreateSegmentJoining(group));
                    }

                    group_html.Add(HTMLTables.CreateTemplateTables(Parameters.Groups[group].Item2, AssetFolderName, total_reads, total_area));

                    group_html.Add(CreateCDROverview(id + "-cdr", Parameters.Groups[group].Item2));

                    if (Parameters.Groups.Count == 1)
                        inner_html.Add(group_html);
                    else
                        inner_html.Collapsible(id, new HtmlBuilder(Parameters.Groups[group].Item1), group_html);
                }

            inner_html.Collapsible("reads", new HtmlBuilder("Reads Table"), HTMLTables.CreateReadsTable(Parameters.Input, AssetFolderName));
            inner_html.Collapsible("batchfile", new HtmlBuilder("Batch File"), BatchFileHTML());

            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var html = new HtmlBuilder();
            html.UnsafeContent("<!DOCTYPE html>");
            html.Open(HtmlTag.html, "lang='en-GB' xml:lang='en-GB'");
            html.Add(CreateHeader(Parameters.Runname, new List<string>()));
            html.Open(HtmlTag.body, "onload='Setup()' class='report'");
            html.Open(HtmlTag.div, "class='report'");
            html.OpenAndClose(HtmlTag.h1, "", "Stitch Interactive Report Run: " + Parameters.Runname);
            html.OpenAndClose(HtmlTag.p, "", "Generated at " + timestamp);
            html.Add(GetWarnings());
            html.OpenAndClose(HtmlTag.div, "class='overview'", CreateOverview());
            html.Add(inner_html);
            html.Add(Docs());
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.footer);
            html.Open(HtmlTag.p);
            html.Content("Made by the Snijderlab in 2019-2023, the project is open source at ");
            html.OpenAndClose(HtmlTag.a, "href='https://www.github.com/snijderlab/stitch' target='_blank'", "github.com/snijderlab/stitch");
            html.Content(" licensed under the ");
            html.OpenAndClose(HtmlTag.a, "href='https://choosealicense.com/licenses/mit/' target='_blank'", "MIT license");
            html.Content(".");
            html.Close(HtmlTag.p);
            html.Open(HtmlTag.p);
            html.Content("Version: ");
            html.OpenAndClose(HtmlTag.span, "class='version'", version);
            html.Content(" please mention this if you send in a bug report.");
            html.Close(HtmlTag.p);
            html.Close(HtmlTag.footer);
            html.Close(HtmlTag.body);
            html.Close(HtmlTag.html);
            return html;
        }

        HtmlBuilder GetWarnings() {
            var html = new HtmlBuilder();
            if (Parameters.Groups == null) return html;
            // High decoy scores
            int max_decoy_score = 0;
            int max_normal_score = 0;
            for (int group = 0; group < Parameters.Groups.Count; group++) {
                if (Parameters.Groups[group].Item1.ToLower() == "decoy") {
                    max_decoy_score = Parameters.Groups[group].Item2.Select(s => s.Templates.Select(t => t.Score).Max()).Max();
                } else {
                    max_normal_score = Math.Max(max_normal_score, Parameters.Groups[group].Item2.Select(s => s.Templates.Select(t => t.Score).Max()).Max());

                    if (Parameters.RecombinedSegment.Count != 0) {
                        var decoy_scores = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).Select(t => t.Score);
                        if (decoy_scores.Count() > 0) {
                            var decoy = decoy_scores.Max();
                            var normal = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).Select(t => t.Score).Max();

                            // Generate specific warnings
                            if (decoy > normal * 0.5)
                                html.UnsafeContent(CommonPieces.RecombineHighDecoyWarning(Parameters.Groups[group].Item1));
                        }
                    }
                }
            }

            // Generate the general warning
            if (max_decoy_score > max_normal_score * 0.5)
                html.UnsafeContent(CommonPieces.TemplateHighDecoyWarning());

            // Segment joining
            if (Parameters.RecombinedSegment.Count != 0)
                for (int group = 0; group < Parameters.Groups.Count; group++)
                    foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores)
                        if (set.EndAlignment.LenB == 0) {
                            var A = Parameters.Groups[group].Item2[set.Index - 1];
                            var B = Parameters.Groups[group].Item2[set.Index];
                            html.UnsafeContent(CommonPieces.Warning("Ineffective segment joining", $"<p>The segment joining between {A.Name} and {B.Name} did not find a good solution, look into the specific report to see if this influences the validity of the results.</p>"));
                        }
            return html;
        }

        HtmlBuilder Docs() {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.p, "");
            html.Content("Answers to common questions can be found here. If anything is unclear, or you miss any features please reach out to the authors, all information can be found on the ");
            html.OpenAndClose(HtmlTag.a, "href='https://www.github.com/snijderlab/stitch' target='_blank'", "repository");
            html.Content(".");
            html.Close(HtmlTag.p);
            var export = new HtmlBuilder();
            export.OpenAndClose(HtmlTag.p, "", @"If the graphs are needed in a vector graphics format the whole page can be printed to a pdf. To do this print
             the page to a pdf file and save the generated file. These files can be imported in most vector graphics editors.
              It is best to turn on the background graphics and turn off any headers, besides this setting the margins smaller
              and using landscape or portrait could enhance the results. See the below picture for the options. If you want to
              be able to edit the text as text in Adobe Illustrator we had the best results using Firefox > Print > Save as PDF.");
            export.OpenAndClose(HtmlTag.span, "onclick='window.print()' class='info-link' style='font-size:120%;margin-bottom:1em;'", "Or click here to print");
            export.Empty(HtmlTag.img, $"src='{AssetsFolderName}/export_pdf_example.png' alt='Screenshot of the operation of printing to a PDF in chrome with some extra options that could be beneficial.'");
            html.Collapsible("docs-export-svg", new HtmlBuilder("Export Graphs to Vector Graphics"), export);
            html.Collapsible("docs-share", new HtmlBuilder("Sharing this report"),
            new HtmlBuilder(HtmlTag.p, @$"To share the HTML report with someone else the html file with its accompanying folder (with the same name) can
             be zipped and sent to anyone having a modern browser. This is quite easy to do in Windows as you can select the file
             (eg `report-monoclonal.html`) and the folder (eg `report-monoclonal`) by holding control and clicking on both. Then
             making a zip file can be done by right clicking and selecting `Send to` > `Compressed (zipped) folder` in Windows 10
             or `Compress to zip file` in Windows 11. The recipient can then unzip the folder and make full use of all
             interactivity as provided by the report."
            ));
            var ions = new HtmlBuilder();
            ions.OpenAndClose(HtmlTag.p, "", @"The Roepstorff, Fohlman, Johnson ion nomenclature is used. This is the common way of naming ions most
            people will be familiar with. But we use two special ions, w and d also called satellite ions, which are not commonly used. These form by
            cleavages of the side chain and are thus specially suited for the disambiguation of Leucine and Isoleucine. In the overview below the mass
            differences and ions formed are displayed. The d ion is formed by fragmentation of the side chain of an a ion. The w ion is formed by side
            chain fragmentation of a z ion. Because isoleucine and threonine are doubly substituted at the beta carbon these two amino acids form two
            different w/d ions. THese ions are not formed with all fragmentation techniques though, Stitch only searches for d ions in the first amino
            acids with CID/HCD/PQD data, and only searches for w ions in ETD/ECD/EThcD/ETciD data.");
            ions.Empty(HtmlTag.img, $"src='{AssetsFolderName}/ion_overview.svg' alt='Overview of ion fragmentation with the special fragments from w and d ions annotated.'");
            ions.OpenAndClose(HtmlTag.a, "href='http://www.matrixscience.com/help/fragmentation_help.html' target='_blank' style='max-width:500px;display:block;'", "Find more information here.");
            html.Collapsible("docs-ions", new HtmlBuilder("Ion nomenclature"), ions);
            var outer = new HtmlBuilder();
            outer.Collapsible("docs", new HtmlBuilder("Documentation"), html);
            return outer;
        }

        void CopyAssets() {
            var executable_folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            void CopyAssetsFile(string name, string directory = "assets") {
                var source = Path.Join(executable_folder, directory, name);
                if (File.Exists(source)) {
                    try {
                        File.Copy(source, Path.Join(FullAssetsFolderName, name), true);
                    } catch (Exception e) {
                        new InputNameSpace.ErrorMessage(source, "Could not copy asset", e.Message, "", true).Print();
                    }
                } else
                    new InputNameSpace.ErrorMessage(source, "Could not find asset", "Please make sure the file exists. The HTML will be generated but may be less useful", "", true).Print();
            }

            CopyAssetsFile("export_pdf_example.png", "images");
            CopyAssetsFile("ion_overview.svg", "images");
            CopyAssetsFile("favicon.ico", "images");
            if (Parameters.runVariables.LiveServer != -1) return;
            CopyAssetsFile("styles.css");
            CopyAssetsFile("script.js");
            CopyAssetsFile("Roboto-Regular.ttf");
            CopyAssetsFile("Roboto-Medium.ttf");
            CopyAssetsFile("RobotoMono-VariableFont.ttf");
        }

        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        public async new void Save(string filename) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            FullAssetsFolderName = Path.Join(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            AssetsFolderName = Path.GetFileNameWithoutExtension(filename);

            Directory.CreateDirectory(FullAssetsFolderName);

            Task t = Task.Run(() => CopyAssets());

            try {
                var html = CreateMain().ToString();
                CreateAsides();

                stopwatch.Stop();
                html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
                SaveAndCreateDirectories(filename, html);
            } catch (Exception e) {
                InputNameSpace.ErrorMessage.PrintException(e);
                return;
            }

            await t;

            if (Parameters.runVariables.AutomaticallyOpen) {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(Parameters.runVariables.LiveServer != -1 ? $"http://localhost:{Parameters.runVariables.LiveServer}/results/" + Directory.GetParent(filename).Name + "/" + Path.GetFileName(filename) : filename) {
                    UseShellExecute = true
                };
                p.Start();
            }
        }
    }
}