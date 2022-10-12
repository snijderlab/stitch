using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using HTMLNameSpace;
using static HTMLNameSpace.CommonPieces;
using static AssemblyNameSpace.HelperFunctionality;
using HtmlGenerator;

namespace AssemblyNameSpace
{
    /// <summary>
    /// An HTML report.
    /// </summary>
    public class HTMLReport : Report
    {
        /// <summary>
        /// The name of the assets folder
        /// </summary>
        public string AssetsFolderName;
        string FullAssetsFolderName;
        public RunParameters.Report.HTML ReportParameter;

        public HTMLReport(ReportInputParameters Parameters, int maxThreads, RunParameters.Report.HTML reportParameter) : base(Parameters, maxThreads)
        {
            ReportParameter = reportParameter;
        }

        public override string Create()
        {
            throw new Exception("HTML reports should be generated using the 'Save' function.");
        }

        /// <summary> Generates a list of asides for details viewing. </summary>
        public void CreateAsides()
        {
            var job_buffer = new List<(AsideType, int, int, int)>();

            // Read Asides
            for (int i = 0; i < Parameters.Input.Count; i++)
            {
                job_buffer.Add((AsideType.Read, -1, -1, i));
            }
            // Template Tables Asides
            if (Parameters.Segments != null)
            {
                for (int i = 0; i < Parameters.Segments.Count; i++)
                    for (int j = 0; j < Parameters.Segments[i].Item2.Count; j++)
                        for (int k = 0; k < Parameters.Segments[i].Item2[j].Templates.Count; k++)
                            job_buffer.Add((AsideType.Template, i, j, k));
            }
            // Recombination Table Asides
            if (Parameters.RecombinedSegment != null)
            {
                for (int i = 0; i < Parameters.RecombinedSegment.Count; i++)
                    for (int j = 0; j < Parameters.RecombinedSegment[i].Templates.Count; j++)
                        job_buffer.Add((AsideType.RecombinedTemplate, i, -1, j));
            }

            if (MaxThreads > 1)
            {
                Parallel.ForEach(
                    job_buffer,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (a, _) => CreateAndSaveAside(a.Item1, a.Item2, a.Item3, a.Item4)
                );
            }
            else
            {
                foreach (var (t, i3, i2, i1) in job_buffer)
                {
                    CreateAndSaveAside(t, i3, i2, i1);
                }
            }
        }

        void CreateAndSaveAside(AsideType aside, int index3, int index2, int index1)
        {
            try
            {
                HtmlBuilder inner_html;

                ReadMetaData.IMetaData metadata = new ReadMetaData.Simple(null, null);
                switch (aside)
                {
                    case AsideType.Read:
                        inner_html = HTMLAsides.CreateReadAside(Parameters.Input[index1], Parameters.Segments, Parameters.RecombinedSegment, AssetsFolderName, Parameters.Fragments);
                        metadata = Parameters.Input[index1].MetaData;
                        break;
                    case AsideType.Template:
                        var template = Parameters.Segments[index3].Item2[index2].Templates[index1];
                        inner_html = HTMLAsides.CreateTemplateAside(template, AsideType.Template, AssetsFolderName, Parameters.Input.Count, Parameters.AmbiguityThreshold);
                        metadata = template.MetaData;
                        break;
                    case AsideType.RecombinedTemplate:
                        var rTemplate = Parameters.RecombinedSegment[index3].Templates[index1];
                        inner_html = HTMLAsides.CreateTemplateAside(rTemplate, AsideType.RecombinedTemplate, AssetsFolderName, Parameters.Input.Count, Parameters.AmbiguityThreshold);
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
                html.Open(HtmlTag.html, "lang='en-GB'");
                html.Add(CreateHeader("Details " + id, location));
                html.Open(HtmlTag.body, "class='details' onload='Setup()'");
                html.OpenAndClose(HtmlTag.a, $"href='{home_location}' class='overview-link'", "Overview");
                html.OpenAndClose(HtmlTag.a, "href='#' id='back-button' class='overview-link' style='display:none;' onclick='GoBack()'", "Undefined");
                html.Add(inner_html);
                html.Close(HtmlTag.body);
                html.Close(HtmlTag.html);

                SaveAndCreateDirectories(full_path, html.ToString());
            }
            catch (Exception e)
            {
                InputNameSpace.ErrorMessage.PrintException(e);
                throw new Exception("Exception raised in creation of aside. See above message for more details.");
            }
        }

        HtmlBuilder CreateCDROverview(string id, List<Segment> segments)
        {
            var cdr1_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr2_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr3_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            int total_templates = 0;
            bool found_cdr_region = false;

            foreach (var template in segments.SelectMany(a => a.Templates))
            {
                var positions = new Dictionary<Annotation, (int Start, int Length)>();
                int cumulative = 0;
                int position = 0;
                if (template.MetaData is ReadMetaData.Fasta fasta && fasta != null)
                {
                    foreach (var piece in fasta.AnnotatedSequence)
                    {
                        if (piece.Type.IsAnyCDR())
                        {
                            if (positions.ContainsKey(piece.Type))
                            {
                                var values = positions[piece.Type];
                                positions[piece.Type] = (values.Item1, values.Item2 + cumulative + piece.Sequence.Length);
                            }
                            else
                            {
                                positions.Add(piece.Type, (position, piece.Sequence.Length));
                            }
                            cumulative = 0;
                            total_templates++;
                        }
                        else
                        {
                            cumulative += piece.Sequence.Length;
                        }
                        position += piece.Sequence.Length;
                    }
                }

                if (positions.ContainsKey(Annotation.CDR1))
                { // V-segment
                    found_cdr_region = true;
                    foreach (var read in template.Matches)
                    {
                        foreach (var (group, cdr) in positions)
                        {
                            if (read.StartTemplatePosition < cdr.Start + cdr.Length && read.StartTemplatePosition + read.LengthOnTemplate > cdr.Start)
                            {
                                var piece = (read.MetaData, template.MetaData, read.GetQuerySubMatch(cdr.Start, cdr.Length), read.Unique);
                                switch (group)
                                {
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
                }
                else if (positions.ContainsKey(Annotation.CDR3))
                { // J-segment
                    found_cdr_region = true;
                    var cdr = positions[Annotation.CDR3];

                    foreach (var read in template.Matches)
                    {
                        if (read.StartTemplatePosition < cdr.Start + cdr.Length && read.StartTemplatePosition + read.LengthOnTemplate > cdr.Start)
                        {
                            cdr3_reads.Add((read.MetaData, template.MetaData, read.GetQuerySubMatch(cdr.Start, cdr.Length), read.Unique));
                        }
                    }
                }
            }

            if (!found_cdr_region) return new HtmlBuilder(); // Do not create a collapsable segment if no CDR region could be found in the templates

            string extend(string sequence, int size)
            {
                if (sequence.Length < size)
                {
                    int pos = (int)Math.Ceiling((double)sequence.Length / 2);
                    return sequence.Substring(0, pos) + new string('~', size - sequence.Length) + sequence.Substring(pos);
                }
                else if (sequence.Length > size)
                {
                    if (sequence.StartsWith('.'))
                        return sequence.Substring(sequence.Length - size);
                    else if (sequence.EndsWith('.'))
                        return sequence.Substring(0, size);
                }
                return sequence;
            }

            if (cdr1_reads.Count > 0)
            {
                int cdr1_length = Math.Min(11, cdr1_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr1_reads.Count; i++)
                {
                    cdr1_reads[i] = (cdr1_reads[i].MetaData, cdr1_reads[i].Template, extend(cdr1_reads[i].Sequence, cdr1_length), cdr1_reads[i].Unique);
                }
            }
            if (cdr2_reads.Count > 0)
            {
                int cdr2_length = Math.Min(11, cdr2_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr2_reads.Count; i++)
                {
                    cdr2_reads[i] = (cdr2_reads[i].MetaData, cdr2_reads[i].Template, extend(cdr2_reads[i].Sequence, cdr2_length), cdr2_reads[i].Unique);
                }
            }
            if (cdr3_reads.Count > 0)
            {
                int cdr3_length = Math.Min(13, cdr3_reads.Select(a => a.Sequence.Length).Max());
                for (int i = 0; i < cdr3_reads.Count; i++)
                {
                    cdr3_reads[i] = (cdr3_reads[i].MetaData, cdr3_reads[i].Template, extend(cdr3_reads[i].Sequence, cdr3_length), cdr3_reads[i].Unique);
                }
            }

            var html = new HtmlBuilder();
            html.OpenAndClose(HtmlTag.p, "", "All reads matching any Template within the CDR regions are listed here. These all stem from the alignments made in the TemplateMatching step.");
            if (cdr1_reads.Count == 0 && cdr2_reads.Count == 0 && cdr3_reads.Count == 0)
                html.OpenAndClose(HtmlTag.p, "", "No CDR reads could be placed.");
            else
            {
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

        private HtmlBuilder CreateHeader(string title, List<string> location)
        {
            var link = GetLinkToFolder(new List<string>() { AssetsFolderName }, location);
            var assets_folder = link;
            if (!String.IsNullOrEmpty(Parameters.runVariables.LiveServer))
                link = $"http://localhost:{Parameters.runVariables.LiveServer}/assets/";
            var html = new HtmlBuilder();
            html.Open(HtmlTag.head);
            html.Empty(HtmlTag.meta, "charset='utf-8'");
            html.Empty(HtmlTag.meta, "name='viewport' content='width=device-width, initial-scale=1.0'");
            html.Empty(HtmlTag.link, $"rel='icon' href='{assets_folder}favicon.ico' type='image/x-icon'");
            html.OpenAndClose(HtmlTag.title, "", title + " | Stitch");
            html.OpenAndClose(HtmlTag.style, "", $@"@font-face {{
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
  src: url({link}RobotoMono-Regular.ttf);
  font-weight: normal;
}}
@font-face {{
  font-family: 'Roboto Mono';
  src: url({link}RobotoMono-Medium.ttf);
  font-weight: 500;
}}");
            html.OpenAndClose(HtmlTag.script, "", $"assets_folder = '{AssetsFolderName}';");
            html.OpenAndClose(HtmlTag.script, $"src='{link}script.js'", "");
            html.Empty(HtmlTag.link, $"rel='stylesheet' href='{link}styles.css'");
            html.Close(HtmlTag.head);
            return html;
        }

        private HtmlBuilder BatchFileHTML()
        {
            if (BatchFile != null)
            {
                string Render(string line)
                {
                    if (line.Trim().StartsWith('-'))
                        return $"<span class='comment'>{line}</span>";

                    var open = Regex.Match(line, @"^(\s*)([\w ]+)(\s*)->$");
                    if (open.Success)
                        return $"{open.Groups[1]}<span class='id'>{open.Groups[2]}</span>{open.Groups[3]}<span class='op'>-&gt;</span>";

                    var single = Regex.Match(line, @"^(\s*)([\w ]+)(\s*):(.+)$");
                    if (single.Success)
                        return $"{single.Groups[1]}<span class='id'>{single.Groups[2]}</span>{single.Groups[3]}<span class='op'>:</span><span class='value'>{single.Groups[4]}</span>";

                    var close = Regex.Match(line, @"^(\s*)<-$");
                    if (close.Success)
                        return $"{close.Groups[1]}<span class='op'>&lt;-</span>";

                    return line;
                }

                var html = new HtmlBuilder();
                var bf = BatchFile;
                html.Open(HtmlTag.code);
                html.OpenAndClose(HtmlTag.i, "", bf.Filename);
                foreach (var line in bf.Lines) html.UnsafeContent(Render(line.TrimEnd()));
                html.Close(HtmlTag.code);
                return html;
            }
            else
            {
                return new HtmlBuilder(HtmlTag.em, "No BatchFile");
            }
        }

        private HtmlBuilder CreateOverview()
        {
            var html = new HtmlBuilder();
            if (Parameters.RecombinedSegment.Count != 0)
            {
                for (int group = 0; group < Parameters.Segments.Count && group < Parameters.RecombinedSegment.Count; group++)
                {
                    if (Parameters.Segments[group].Item1.ToLower() == "decoy") continue;
                    var template = Parameters.RecombinedSegment[group].Templates[0];
                    var (seq, doc) = template.ConsensusSequence();
                    html.Open(HtmlTag.h2);
                    html.OpenAndClose(HtmlTag.a, $"href='{GetAsideRawLink(template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName)}' target='_blank'", Parameters.Segments[group].Item1);
                    html.Close(HtmlTag.h2);
                    html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(seq));
                    html.Open(HtmlTag.div, "class='doc-plot'");
                    html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(doc), new HtmlGenerator.HtmlBuilder("Depth of Coverage"), null, null, 10, template.ConsensusSequenceAnnotation()));
                    html.Close(HtmlTag.div);
                    html.OpenAndClose(HtmlTag.h3, "", "Best scoring segments");
                    html.Open(HtmlTag.p);

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count; segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        if (seg.Templates.Count > 0)
                            html.Add(GetAsideLinkHtml(seg.Templates[0].MetaData, AsideType.Template, AssetsFolderName));
                    }
                    html.Close(HtmlTag.p);
                }
            }
            else
            {
                for (int group = 0; group < Parameters.Segments.Count; group++)
                {
                    html.OpenAndClose(HtmlTag.h2, "", Parameters.Segments[group].Item1);

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count; segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        html.OpenAndClose(HtmlTag.h3, "", seg.Name);
                        html.Add(HTMLTables.TableHeader(seg.Templates, Parameters.Input.Count));
                    }
                }
            }
            return html;
        }

        private HtmlBuilder CreateSegmentJoining(int group)
        {
            var html = new HtmlBuilder();
            foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores)
            {
                var A = Parameters.Segments[group].Item2[set.Index - 1];
                var B = Parameters.Segments[group].Item2[set.Index];
                html.OpenAndClose(HtmlTag.h2, "", $"{A.Name} * {B.Name}");
                var seqA = AminoAcid.ArrayToString(set.SeqA.SubArray(set.SeqA.Length - set.Score.Best.Position - 3, 3 + set.Score.Best.Position));
                var seqB = AminoAcid.ArrayToString(set.SeqB.Take(3 + set.Score.Best.Position).ToArray());
                html.OpenAndClose(HtmlTag.pre, "class='seq'", $"...{A.Name}\n      {B.Name}"); // The seq B starts exactly 3 chars into seq A plus the padding for '...'
                html.OpenAndClose(HtmlTag.p, "", $"Best overlap {set.Score.Best.Position} with score {set.Score.Best.Score}");

                html.Add(HTMLGraph.Bargraph(set.Score.Scores.Select(s => (s.Item1.ToString(), (double)s.Item2)).ToList(), new HtmlGenerator.HtmlBuilder("Other overlaps"), new HtmlGenerator.HtmlBuilder(HtmlGenerator.HtmlTag.p, HTMLHelp.SegmentJoining)));
            }
            return html;
        }

        private HtmlBuilder CreateMain()
        {
            var inner_html = new HtmlBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);

            if (Parameters.Segments != null)
                for (int group = 0; group < Parameters.Segments.Count; group++)
                {
                    var group_html = new HtmlBuilder();
                    var id = Parameters.Segments[group].Item1.ToLower().Replace(' ', '-');

                    if (Parameters.RecombinedSegment.Count != 0)
                    {
                        if (id == "decoy" && Parameters.Segments.Count > Parameters.RecombinedSegment.Count) continue;
                        var recombined = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).ToList();
                        var decoy = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).ToList();
                        group_html.Collapsible(id + "-recombination", new HtmlBuilder("Recombination Table"), HTMLTables.CreateSegmentTable(id + "-recombination", recombined, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true));
                        if (decoy.Count > 0)
                            group_html.Collapsible(id + "-recombination-decoy", new HtmlBuilder("Recombination Decoy"), HTMLTables.CreateSegmentTable(id + "-recombination-decoy", decoy, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true));

                        if (Parameters.RecombinedSegment[group].SegmentJoiningScores.Count > 0)
                            group_html.Collapsible(id + "-segment-joining", new HtmlBuilder("Segment joining"), CreateSegmentJoining(group));
                    }

                    group_html.Add(HTMLTables.CreateTemplateTables(Parameters.Segments[group].Item2, AssetFolderName, Parameters.Input.Count));

                    group_html.Add(CreateCDROverview(id + "-cdr", Parameters.Segments[group].Item2));

                    if (Parameters.Segments.Count == 1)
                        inner_html.Add(group_html);
                    else
                        inner_html.Collapsible(id, new HtmlBuilder(Parameters.Segments[group].Item1), group_html);
                }

            inner_html.Collapsible("reads", new HtmlBuilder("Reads Table"), HTMLTables.CreateReadsTable(Parameters.Input, AssetFolderName));
            inner_html.Collapsible("batchfile", new HtmlBuilder("Batch File"), BatchFileHTML());

            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var html = new HtmlBuilder();
            html.UnsafeContent("<!DOCTYPE html>");
            html.Open(HtmlTag.html, "lang='en-GB'");
            html.Add(CreateHeader(Parameters.Runname, new List<string>()));
            html.Open(HtmlTag.body, "onload='Setup()'");
            html.Open(HtmlTag.div, "class='report'");
            html.OpenAndClose(HtmlTag.h1, "", "Stitch Interactive Report Run: " + Parameters.Runname);
            html.OpenAndClose(HtmlTag.p, "", "Generated at " + timestamp);
            html.Add(GetWarnings());
            html.OpenAndClose(HtmlTag.div, "class='overview'", CreateOverview());
            html.Add(inner_html);
            html.Add(Docs());
            html.Open(HtmlTag.footer);
            html.Open(HtmlTag.p);
            html.Content("Made by the Snijderlab in 2019-2022, the project is open source at ");
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
            html.Close(HtmlTag.div);
            html.Close(HtmlTag.body);
            html.Close(HtmlTag.html);
            return html;
        }

        HtmlBuilder GetWarnings()
        {
            var html = new HtmlBuilder();
            if (Parameters.Segments == null) return html;
            // High decoy scores
            int max_decoy_score = 0;
            int max_normal_score = 0;
            for (int group = 0; group < Parameters.Segments.Count; group++)
            {
                if (Parameters.Segments[group].Item1.ToLower() == "decoy")
                {
                    max_decoy_score = Parameters.Segments[group].Item2.Select(s => s.Templates.Select(t => t.Score).Max()).Max();
                }
                else
                {
                    max_normal_score = Math.Max(max_normal_score, Parameters.Segments[group].Item2.Select(s => s.Templates.Select(t => t.Score).Max()).Max());

                    if (Parameters.RecombinedSegment.Count != 0)
                    {
                        var decoy_scores = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).Select(t => t.Score);
                        if (decoy_scores.Count() > 0)
                        {
                            var decoy = decoy_scores.Max();
                            var normal = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).Select(t => t.Score).Max();

                            // Generate specific warnings
                            if (decoy > normal * 0.5)
                                html.UnsafeContent(CommonPieces.RecombineHighDecoyWarning(Parameters.Segments[group].Item1));
                        }
                    }
                }
            }

            // Generate the general warning
            if (max_decoy_score > max_normal_score * 0.5)
                html.UnsafeContent(CommonPieces.TemplateHighDecoyWarning());

            // Segment joining
            if (Parameters.RecombinedSegment.Count != 0)
                for (int group = 0; group < Parameters.Segments.Count; group++)
                    foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores)
                        if (set.Score.Best.Position == 0)
                        {
                            var A = Parameters.Segments[group].Item2[set.Index - 1];
                            var B = Parameters.Segments[group].Item2[set.Index];
                            html.UnsafeContent(CommonPieces.Warning("Ineffective segment joining", $"<p>The segment joining between {A.Name} and {B.Name} did not find a good solution, look into the specific report to see if this influences the validity of the results.</p>"));
                        }
            return html;
        }

        HtmlBuilder Docs()
        {
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
              and using landscape or portrait could enhance the results. See the below picture for the options.");
            export.OpenAndClose(HtmlTag.span, "onclick='window.print()' class='info-link' style='font-size:120%;margin-bottom:1em;'", "Or click here to print");
            export.Empty(HtmlTag.img, "src='{AssetsFolderName}/export_pdf_example.png' alt='Screenshot of the operation of printing to a PDF in chrome with some extra options that could be beneficial.'");
            html.Collapsible("docs-export-svg", new HtmlBuilder("Export Graphs to Vector Graphics"), export);
            html.Collapsible("docs-share", new HtmlBuilder("Sharing this report"),
            new HtmlBuilder(HtmlTag.p, @$"To share the HTML report with someone else the html file with its accompanying folder (with the same name) can
             be zipped and sent to anyone having a modern browser. This is quite easy to do in Windows as you can select the file
             (eg `report-monoclonal.html`) and the folder (eg `report-monoclonal`) by holding control and clicking on both. Then 
             making a zip file can be done by right clicking and selecting `Send to` > `Compressed (zipped) folder` in Windows 10
             or `Compress to zip file` in Windows 11. The recipient can then unzip the folder and make full use of all 
             interactivity as provided by the report."
            ));
            var outer = new HtmlBuilder();
            outer.Collapsible("docs", new HtmlBuilder("Documentation"), html);
            return outer;
        }

        void CopyAssets()
        {
            var executable_folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            void CopyAssetsFile(string name, string directory = "assets")
            {
                var source = Path.Join(executable_folder, directory, name);
                if (File.Exists(source))
                {
                    try
                    {
                        File.Copy(source, Path.Join(FullAssetsFolderName, name), true);
                    }
                    catch (Exception e)
                    {
                        new InputNameSpace.ErrorMessage(source, "Could not copy asset", e.Message, "", true).Print();
                    }
                }
                else
                    new InputNameSpace.ErrorMessage(source, "Could not find asset", "Please make sure the file exists. The HTML will be generated but may be less useful", "", true).Print();
            }

            CopyAssetsFile("export_pdf_example.png", "images");
            CopyAssetsFile("favicon.ico", "images");
            if (!String.IsNullOrEmpty(Parameters.runVariables.LiveServer)) return;
            CopyAssetsFile("styles.css");
            CopyAssetsFile("script.js");
            CopyAssetsFile("Roboto-Regular.ttf");
            CopyAssetsFile("Roboto-Medium.ttf");
            CopyAssetsFile("RobotoMono-Regular.ttf");
            CopyAssetsFile("RobotoMono-Medium.ttf");
        }

        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        public async new void Save(string filename)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            FullAssetsFolderName = Path.Join(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            AssetsFolderName = Path.GetFileNameWithoutExtension(filename);

            Directory.CreateDirectory(FullAssetsFolderName);

            Task t = Task.Run(() => CopyAssets());

            try
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
                var html = CreateMain().ToString();
                CreateAsides();

                stopwatch.Stop();
                html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
                SaveAndCreateDirectories(filename, html);
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }
            catch (Exception e)
            {
                InputNameSpace.ErrorMessage.PrintException(e);
                return;
            }

            await t;

            if (Parameters.runVariables.AutomaticallyOpen)
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(!String.IsNullOrEmpty(Parameters.runVariables.LiveServer) ? $"http://localhost:{Parameters.runVariables.LiveServer}/results/" + Directory.GetParent(filename).Name + "/" + Path.GetFileName(filename) : filename)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
        }
    }
}