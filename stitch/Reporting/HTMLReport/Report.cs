using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;
using HTMLNameSpace;
using static HTMLNameSpace.CommonPieces;

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

        public HTMLReport(ReportInputParameters Parameters, int maxThreads) : base(Parameters, maxThreads) { }

        public override string Create()
        {
            throw new Exception("HTML reports should be generated using the 'Save' function.");
        }

        /// <summary> Generates a list of asides for details viewing. </summary>
        public async void CreateAsides()
        {
            var jobbuffer = new List<(AsideType, int, int, int)>();

            // Read Asides
            for (int i = 0; i < Parameters.Input.Count; i++)
            {
                jobbuffer.Add((AsideType.Read, -1, -1, i));
            }
            // Template Tables Asides
            if (Parameters.Segments != null)
            {
                for (int i = 0; i < Parameters.Segments.Count; i++)
                    for (int j = 0; j < Parameters.Segments[i].Item2.Count; j++)
                        for (int k = 0; k < Parameters.Segments[i].Item2[j].Templates.Count; k++)
                            jobbuffer.Add((AsideType.Template, i, j, k));
            }
            // Recombination Table Asides
            if (Parameters.RecombinedSegment != null)
            {
                for (int i = 0; i < Parameters.RecombinedSegment.Count; i++)
                    for (int j = 0; j < Parameters.RecombinedSegment[i].Templates.Count; j++)
                        jobbuffer.Add((AsideType.RecombinedTemplate, i, -1, j));
            }
            var taskList = new List<Task>();
            if (MaxThreads > 1)
            {
                Parallel.ForEach(
                    jobbuffer,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (a, _) => taskList.Add(CreateAndSaveAside(a.Item1, a.Item2, a.Item3, a.Item4))
                );
            }
            else
            {
                foreach (var (t, i3, i2, i1) in jobbuffer)
                {
                    taskList.Add(CreateAndSaveAside(t, i3, i2, i1));
                }
            }
            await Task.WhenAll(taskList);
        }

        Task CreateAndSaveAside(AsideType aside, int index3, int index2, int index1)
        {
            var buffer = new StringBuilder();
            var innerbuffer = new StringBuilder();

            ReadMetaData.IMetaData metadata = new ReadMetaData.Simple(null, null);
            switch (aside)
            {
                case AsideType.Read:
                    HTMLAsides.CreateReadAside(innerbuffer, Parameters.Input[index1]);
                    metadata = Parameters.Input[index1].MetaData;
                    break;
                case AsideType.Template:
                    var template = Parameters.Segments[index3].Item2[index2].Templates[index1];
                    HTMLAsides.CreateTemplateAside(innerbuffer, template, AsideType.Template, AssetsFolderName, Parameters.Input.Count);
                    metadata = template.MetaData;
                    break;
                case AsideType.RecombinedTemplate:
                    var rTemplate = Parameters.RecombinedSegment[index3].Templates[index1];
                    HTMLAsides.CreateTemplateAside(innerbuffer, rTemplate, AsideType.RecombinedTemplate, AssetsFolderName, Parameters.Input.Count);
                    metadata = rTemplate.MetaData;
                    break;
            };
            var location = new List<string>() { AssetsFolderName, GetAsideName(aside) + "s" };
            var homelocation = GetLinkToFolder(new List<string>(), location) + AssetsFolderName + ".html";
            var id = GetAsideIdentifier(metadata);
            var link = GetLinkToFolder(location, new List<string>());
            var fullpath = Path.Join(Path.GetDirectoryName(FullAssetsFolderName), link) + id.Replace(':', '-') + ".html";

            buffer.Append("<html>");
            buffer.Append(CreateHeader("Details " + id, location));
            buffer.Append("<body class='details' onload='Setup()'>");
            buffer.Append($"<a href='{homelocation}' class='overview-link'>Overview</a><a href='#' id='back-button' class='overview-link' style='display:none;' onclick='GoBack()'>Undefined</a>");
            buffer.Append(innerbuffer.ToString());
            buffer.Append("</body></html>");

            return SaveAndCreateDirectoriesAsync(fullpath, buffer.ToString());
        }

        void CreateCDROverview(string id, StringBuilder buffer, List<Segment> segments)
        {
            var cdr1_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr2_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr3_reads = new List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)>();
            int total_templates = 0;

            foreach (var template in segments.SelectMany(a => a.Templates))
            {
                var positions = new Dictionary<string, (int Start, int Length)>();
                int cumulative = 0;
                int position = 0;
                if (template.MetaData is ReadMetaData.Fasta fasta && fasta != null)
                {
                    foreach (var piece in fasta.AnnotatedSequence)
                    {
                        if (piece.Type.StartsWith("CDR"))
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

                if (positions.ContainsKey("CDR1"))
                { // V-segment

                    foreach (var read in template.Matches)
                    {
                        foreach (var (group, cdr) in positions)
                        {
                            if (read.StartTemplatePosition < cdr.Start + cdr.Length && read.StartTemplatePosition + read.LengthOnTemplate > cdr.Start)
                            {
                                var piece = (read.MetaData, template.MetaData, read.GetQuerySubMatch(cdr.Start, cdr.Length), read.Unique);
                                switch (group)
                                {
                                    case "CDR1":
                                        cdr1_reads.Add(piece);
                                        break;
                                    case "CDR2":
                                        cdr2_reads.Add(piece);
                                        break;
                                    case "CDR3":
                                        cdr3_reads.Add(piece);
                                        break;
                                }
                            }
                        }
                    }
                }
                else if (positions.ContainsKey("CDR3"))
                { // J-segment
                    var cdr = positions["CDR3"];

                    foreach (var read in template.Matches)
                    {
                        if (read.StartTemplatePosition < cdr.Start + cdr.Length && read.StartTemplatePosition + read.LengthOnTemplate > cdr.Start)
                        {
                            cdr3_reads.Add((read.MetaData, template.MetaData, read.GetQuerySubMatch(cdr.Start, cdr.Length), read.Unique));
                        }
                    }
                }
            }

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

            var innerbuffer = new StringBuilder();
            innerbuffer.AppendLine("<p>All reads matching any Template within the CDR regions are listed here. These all stem from the alignments made in the TemplateMatching step.</p>");
            innerbuffer.AppendLine("<div class='cdr-tables'>");
            HTMLTables.CDRTable(innerbuffer, cdr1_reads, AssetsFolderName, "CDR1", Parameters.Input.Count, total_templates);
            HTMLTables.CDRTable(innerbuffer, cdr2_reads, AssetsFolderName, "CDR2", Parameters.Input.Count, total_templates);
            HTMLTables.CDRTable(innerbuffer, cdr3_reads, AssetsFolderName, "CDR3", Parameters.Input.Count, total_templates);
            innerbuffer.AppendLine("</div>");

            buffer.Append(CommonPieces.Collapsible(id, "CDR regions", innerbuffer.ToString()));
        }

        private string CreateHeader(string title, List<string> location)
        {
            var link = GetLinkToFolder(new List<string>() { AssetsFolderName }, location);
            return $@"<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>{title}</title>
<style>
@font-face {{
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
  font-family: Roboto Mono;
  src: url({link}RobotoMono-Regular.ttf);
  font-weight: normal;
}}
@font-face {{
  font-family: Roboto Mono;
  src: url({link}RobotoMono-Medium.ttf);
  font-weight: 500;
}}
</style>
<script>
assetsfolder = '{AssetsFolderName}';
</script>
<script src='{link}script.js'></script>
<link rel='stylesheet' href='{link}styles.css'>
</head>";
        }

        private string BatchFileHTML()
        {
            if (BatchFile != null)
            {
                var buffer = new StringBuilder();
                var bf = BatchFile;
                buffer.Append($"<pre class='source-code'><i>{bf.Filename}</i>\n");
                foreach (var line in bf.Lines) buffer.AppendLine(line.TrimEnd());
                buffer.Append("</pre>");
                return buffer.ToString();
            }
            else
            {
                return "<em>No BatchFile</em>";
            }
        }

        private string CreateOverview()
        {
            var buffer = new StringBuilder();
            if (Parameters.RecombinedSegment.Count != 0)
            {
                for (int group = 0; group < Parameters.Segments.Count && group < Parameters.RecombinedSegment.Count; group++)
                {
                    if (Parameters.Segments[group].Item1.ToLower() == "decoy") continue;
                    var (seq, doc) = Parameters.RecombinedSegment[group].Templates[0].ConsensusSequence();
                    buffer.Append($"<h2>{Parameters.Segments[group].Item1}</h2><p class='aside-seq'>{AminoAcid.ArrayToString(seq)}</p><div class='docplot'>");
                    HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(doc), "Depth of Coverage");
                    buffer.Append("</div><h3>Best scoring segments</h3><p>");

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count; segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        if (seg.Templates.Count > 0)
                            buffer.Append(GetAsideLink(seg.Templates[0].MetaData, AsideType.Template, AssetsFolderName));
                    }
                    buffer.Append("</p>");
                }
            }
            else
            {
                for (int group = 0; group < Parameters.Segments.Count; group++)
                {
                    buffer.Append($"<h2>{Parameters.Segments[group].Item1}</h2>");

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count; segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        buffer.Append($"<h3>{seg.Name}</h3>");
                        HTMLTables.TableHeader(buffer, seg.Templates, Parameters.Input.Count);
                    }
                }
            }
            return buffer.ToString();
        }

        private string CreateSegmentJoining(int group)
        {
            var innerbuffer = new StringBuilder();
            foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores)
            {
                var A = Parameters.Segments[group].Item2[set.Index - 1];
                var B = Parameters.Segments[group].Item2[set.Index];
                innerbuffer.Append($"<h2>{A.Name} * {B.Name}</h2>");
                var seqA = AminoAcid.ArrayToString(set.SeqA.SubArray(set.SeqA.Length - set.Score.Best.Position - 3, 3 + set.Score.Best.Position));
                var seqB = AminoAcid.ArrayToString(set.SeqB.Take(3 + set.Score.Best.Position).ToArray());
                innerbuffer.Append($"<pre class='seq'>...{seqA}\n      {seqB}...</pre>"); // The seq B starts exactly 3 chars into seq A plus the padding for '...'
                innerbuffer.Append($"<p>Best overlap {set.Score.Best.Position} with score {set.Score.Best.Score}</p>");

                HTMLGraph.Bargraph(innerbuffer, set.Score.Scores.Select(s => (s.Item1.ToString(), (double)s.Item2)).ToList(), "Other overlaps", 2);
            }
            return innerbuffer.ToString();
        }

        private string CreateMain()
        {
            var innerbuffer = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);

            if (Parameters.Segments != null)
                for (int group = 0; group < Parameters.Segments.Count; group++)
                {
                    var groupbuffer = new StringBuilder();
                    var id = Parameters.Segments[group].Item1.ToLower().Replace(' ', '-');

                    if (Parameters.RecombinedSegment.Count != 0)
                    {
                        if (id == "decoy" && Parameters.Segments.Count > Parameters.RecombinedSegment.Count) continue;
                        var recombined = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).ToList();
                        var decoy = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).ToList();
                        groupbuffer.Append(Collapsible(id + "-recombination", "Recombination Table", HTMLTables.CreateSegmentTable(id + "-recombination", recombined, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true)));
                        if (decoy.Count > 0)
                            groupbuffer.Append(Collapsible(id + "-recombination-decoy", "Recombination Decoy", HTMLTables.CreateSegmentTable(id + "-recombination-decoy", decoy, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true)));

                        if (Parameters.RecombinedSegment[group].SegmentJoiningScores.Count > 0)
                            groupbuffer.Append(Collapsible(id + "-segment-joining", "Segment joining", CreateSegmentJoining(group)));
                    }

                    groupbuffer.Append(HTMLTables.CreateTemplateTables(Parameters.Segments[group].Item2, AssetFolderName, Parameters.Input.Count));

                    CreateCDROverview(id + "-cdr", groupbuffer, Parameters.Segments[group].Item2);

                    if (Parameters.Segments.Count == 1)
                        innerbuffer.Append(groupbuffer);
                    else
                        innerbuffer.Append(Collapsible(id, Parameters.Segments[group].Item1, groupbuffer.ToString()));
                }

            innerbuffer.Append(Collapsible("reads", "Reads Table", HTMLTables.CreateReadsTable(Parameters.Input, AssetFolderName)));
            innerbuffer.Append(Collapsible("batchfile", "Batch File", BatchFileHTML()));

            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            var html = $@"<html>
{CreateHeader($"Stitch: {Parameters.Runname}", new List<string>())}
<body onload=""Setup()"">
<div class=""report"">
<h1>Stitch Interactive Report Run: {Parameters.Runname}</h1>
<p>Generated at {timestamp}</p>

 {GetWarnings()}
 <div class='overview'>{CreateOverview()}</div>
 {innerbuffer}
 {Docs()}

<div class=""footer"">
    <p>Made by the Snijderlab in 2019-2021, the project is open source at <a href='https://www.github.com/snijderlab/stitch'>github.com/snijderlab/stitch</a> licensed under the <a href='https://choosealicense.com/licenses/mit/'>MIT license</a>.</p>
    <p>Version: <span style='color:var(--color-dark);'>{version}</span> please mention this is any bug reports.</p>
</div>

</div>
</div>
</body>";
            return html;
        }

        string GetWarnings()
        {
            var buffer = new StringBuilder();
            if (Parameters.Segments != null)
            {
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

                                if (decoy > normal * 0.5)
                                    buffer.AppendLine(CommonPieces.RecombineHighDecoyWarning(Parameters.Segments[group].Item1));
                            }
                        }
                    }
                }
                if (max_decoy_score > max_normal_score * 0.5)
                    buffer.AppendLine(CommonPieces.TemplateHighDecoyWarning());

                // Segment joining
                if (Parameters.RecombinedSegment.Count != 0)
                    for (int group = 0; group < Parameters.Segments.Count; group++)
                        foreach (var set in Parameters.RecombinedSegment[group].SegmentJoiningScores)
                            if (set.Score.Best.Position == 0)
                            {
                                var A = Parameters.Segments[group].Item2[set.Index - 1];
                                var B = Parameters.Segments[group].Item2[set.Index];
                                buffer.AppendLine(CommonPieces.Warning("Ineffective segment joining", $"<p>The segment joining between {A.Name} and {B.Name} did not find a good solution, look into the specific report to see if this influences the validity of the results.</p>"));
                            }
            }
            return buffer.ToString();
        }

        string Docs()
        {
            var docs = new StringBuilder();
            docs.Append(
                @"<p>Answers to common questions can be found here. If anything is unclear, or you miss any features please reach
                 out to the authors, all information can be found on the <a href='https://www.github.com/snijderlab/stitch'>repository</a>.</p>");
            docs.Append(Collapsible("docs-export-svg", "Export Graphs to Vector Graphics",
            @$"<p>If the graphs are needed in a vector graphics format the whole page can be printed to a pdf. To do this print
             the page to a pdf file and save the generated file. These files can be imported in most vector graphics editors.
              It is best to turn on the background graphics and turn off any headers, besides this setting the margins smaller
              and using landscape or portrait could enhance the results. See the below picture for the options.</p>
              <span onclick='window.print()' class='info-link' style='font-size:120%;margin-bottom:1em;'>Or click here to print</span>
              <img src='{AssetsFolderName}/export_pdf_example.png' alt='Screenshot of the operation of printing to a PDF in chrome
               with some extra options that could be beneficial.'/>"
            ));
            docs.Append(Collapsible("docs-share", "Sharing this report",
            @$"<p>To share the HTML report with someone else the html file with its accompanying folder (with the same name) can
             be zipped and sent to anyone having a modern browser. This is quite easy to do in Windows as you can select the file
             (eg `report-monoclonal.html`) and the folder (eg `report-monoclonal`) by holding control and clicking on both. Then 
             making a zip file can be done by right clicking and selecting `Send to` > `Compressed (zipped) folder` in Windows 10
             or `Compress to zip file` in Windows 11. The recipient can then unzip the folder and make full use of all 
             interactivity as provided by the report.</p>"
            ));
            return Collapsible("docs", "Documentation", docs.ToString());
        }

        void CopyAssets()
        {
            var executable_folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            void CopyAssetsFile(string name)
            {
                var source = Path.Join(executable_folder, "assets", name);
                if (File.Exists(source))
                {
                    try
                    {
                        File.Copy(source, Path.Join(FullAssetsFolderName, name), true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                    new InputNameSpace.ErrorMessage(source, "Could not find asset", "Please make sure the file exists. The HTML will be generated but may be less useful", "", true).Print();
            }

            CopyAssetsFile("styles.css");
            CopyAssetsFile("script.js");
            CopyAssetsFile("Roboto-Regular.ttf");
            CopyAssetsFile("Roboto-Medium.ttf");
            CopyAssetsFile("RobotoMono-Regular.ttf");
            CopyAssetsFile("RobotoMono-Medium.ttf");
            CopyAssetsFile("export_pdf_example.png");
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
                var html = CreateMain();
                CreateAsides();

                stopwatch.Stop();
                html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
                SaveAndCreateDirectories(filename, html);
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await t;
        }
    }
}