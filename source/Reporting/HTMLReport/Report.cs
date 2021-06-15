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

        public HTMLReport(ReportInputParameters Parameters, int max_threads) : base(Parameters, max_threads) { }

        public override string Create()
        {
            throw new Exception("HTML reports should be generated using the 'Save' function.");
        }

        void SaveAside((string content, AsideType type, MetaData.IMetaData metaData) set)
        {
            (var content, var type, var metaData) = set;
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };
            var homelocation = GetLinkToFolder(new List<string>(), location) + AssetsFolderName + ".html";
            var id = GetAsideIdentifier(metaData);
            var link = GetLinkToFolder(location, new List<string>());
            var fullpath = Path.Join(Path.GetDirectoryName(FullAssetsFolderName), link) + id.Replace(':', '-') + ".html";

            StringBuilder buffer = new StringBuilder();
            buffer.Append("<html>");
            buffer.Append(CreateHeader("Details " + id, location));
            buffer.Append("<body class='details' onload='Setup()'>");
            buffer.Append($"<a href='{homelocation}' class='overview-link'>Overview</a><a href='#' id='back-button' class='overview-link' style='display:none;' onclick='GoBack()'>Undefined</a>");
            buffer.Append(content);
            buffer.Append("</body></html>");

            SaveAndCreateDirectories(fullpath, buffer.ToString());
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
            if (Parameters.RecombinedSegment.Count() != 0)
            {
                for (int group = 0; group < Parameters.Segments.Count() && group < Parameters.RecombinedSegment.Count(); group++)
                {
                    if (Parameters.Segments[group].Item1.ToLower() == "decoy") continue;
                    var (seq, doc) = Parameters.RecombinedSegment[group].Templates[0].ConsensusSequence();
                    buffer.Append($"<h2>{Parameters.Segments[group].Item1}</h2><p class='aside-seq'>{AminoAcid.ArrayToString(seq)}</p><div class='docplot'>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(doc))}</div><h3>Best scoring segments</h3><p>");

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count(); segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        if (seg.Templates.Count() > 0)
                            buffer.Append(GetAsideLink(seg.Templates[0].MetaData, AsideType.Template, AssetsFolderName));
                    }
                    buffer.Append("</p>");
                }
            }
            else
            {
                for (int group = 0; group < Parameters.Segments.Count(); group++)
                {
                    buffer.Append($"<h2>{Parameters.Segments[group].Item1}</h2>");

                    for (int segment = 0; segment < Parameters.Segments[group].Item2.Count(); segment++)
                    {
                        var seg = Parameters.Segments[group].Item2[segment];
                        buffer.Append($"<h3>{seg.Name}</h3>{HTMLTables.TableHeader(seg.Templates)}");
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
                for (int group = 0; group < Parameters.Segments.Count(); group++)
                {
                    var groupbuffer = "";

                    if (Parameters.RecombinedSegment.Count() != 0)
                    {
                        if (Parameters.Segments[group].Item1.ToLower() == "decoy" && Parameters.Segments.Count() > Parameters.RecombinedSegment.Count()) continue;
                        var recombined = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination != null).ToList();
                        var decoy = Parameters.RecombinedSegment[group].Templates.FindAll(t => t.Recombination == null).ToList();
                        groupbuffer += Collapsible("Recombination Table", HTMLTables.CreateTemplateTable(recombined, AsideType.RecombinedTemplate, AssetFolderName, true));
                        if (decoy.Count() > 0)
                            groupbuffer += Collapsible("Recombination Decoy", HTMLTables.CreateTemplateTable(decoy, AsideType.RecombinedTemplate, AssetFolderName, true));
                    }

                    groupbuffer += HTMLTables.CreateTemplateTables(Parameters.Segments[group].Item2, AssetFolderName);

                    if (Parameters.Segments.Count() == 1)
                        innerbuffer.Append(groupbuffer);
                    else
                        innerbuffer.Append(Collapsible(Parameters.Segments[group].Item1, groupbuffer));
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
                var html = CreateMain();
                foreach (var set in HTMLAsides.CreateAsides(Parameters, AssetsFolderName, MaxThreads))
                {
                    SaveAside(set);
                }

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