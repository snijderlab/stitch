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
using static HTMLNameSpace.Common;

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

            MetaData.IMetaData metadata = new MetaData.Simple(null, null);
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

        void CreateCDROverview(StringBuilder buffer, List<Segment> segments)
        {
            var cdr1_reads = new List<(MetaData.IMetaData MetaData, MetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr2_reads = new List<(MetaData.IMetaData MetaData, MetaData.IMetaData Template, string Sequence, bool Unique)>();
            var cdr3_reads = new List<(MetaData.IMetaData MetaData, MetaData.IMetaData Template, string Sequence, bool Unique)>();
            int total_templates = 0;

            foreach (var template in segments.SelectMany(a => a.Templates))
            {
                var positions = new Dictionary<string, (int Start, int Length)>();
                int cumulative = 0;
                int position = 0;
                if (template.MetaData is MetaData.Fasta fasta && fasta != null)
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

            buffer.Append(Common.Collapsible("CDR regions", innerbuffer.ToString()));
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
                foreach (var line in bf.Lines) buffer.AppendLine(line);
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

        private string CreateMain()
        {
            var innerbuffer = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);

            if (Parameters.Segments != null)
                for (int group = 0; group < Parameters.Segments.Count; group++)
                {
                    var groupbuffer = new StringBuilder();

                    if (Parameters.RecombinedSegment.Count != 0)
                    {
                        if (Parameters.Segments[group].Item1.ToLower() == "decoy" && Parameters.Segments.Count > Parameters.RecombinedSegment.Count) continue;
                        var recombined = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).ToList();
                        var decoy = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).ToList();
                        groupbuffer.Append(Collapsible("Recombination Table", HTMLTables.CreateSegmentTable(recombined, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true)));
                        if (decoy.Count > 0)
                            groupbuffer.Append(Collapsible("Recombination Decoy", HTMLTables.CreateSegmentTable(decoy, null, AsideType.RecombinedTemplate, AssetFolderName, Parameters.Input.Count, true)));
                    }

                    groupbuffer.Append(HTMLTables.CreateTemplateTables(Parameters.Segments[group].Item2, AssetFolderName, Parameters.Input.Count));

                    CreateCDROverview(groupbuffer, Parameters.Segments[group].Item2);

                    if (Parameters.Segments.Count == 1)
                        innerbuffer.Append(groupbuffer);
                    else
                        innerbuffer.Append(Collapsible(Parameters.Segments[group].Item1, groupbuffer.ToString()));
                }

            innerbuffer.Append(Collapsible("Reads Table", HTMLTables.CreateReadsTable(Parameters.Input, AssetFolderName)));
            innerbuffer.Append(Collapsible("Batch File", BatchFileHTML()));

            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            var html = $@"<html>
{CreateHeader("Report Protein Sequence Run", new List<string>())}
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

 <div class='overview'>{CreateOverview()}</div>
 {innerbuffer}

<div class=""footer"">
    <p>Code written in 2019-2021</p>
    <p>Made by the Hecklab</p>
    <p>Version: {version}</p>
</div>

</div>
</div>
</body>";
            return html;
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
                // TODO: Make these async or something, create the job and only execute it when scattered over a thread pool
                // TODO: move CreateAsides to this class again and pass a single StringBuilder for each resulting HTML page
                var html = CreateMain();
                CreateAsides();

                stopwatch.Stop();
                html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
                SaveAndCreateDirectories(filename, html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await t;
        }
    }
}