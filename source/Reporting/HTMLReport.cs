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

namespace AssemblyNameSpace
{
    /// <summary>
    /// An HTML report.
    /// </summary>
    public class HTMLReport : Report
    {
        /// <summary>
        /// Indicates if the program should use the included Dot (graphviz) distribution.
        /// </summary>
        readonly bool UseIncludedDotDistribution;

        /// <summary>
        /// The name of the assets folder
        /// </summary>
        string AssetsFolderName;
        string FullAssetsFolderName;

        public HTMLReport(ReportInputParameters Parameters, bool useincludeddotdistribution, int max_threads) : base(Parameters, max_threads)
        {
            UseIncludedDotDistribution = useincludeddotdistribution;
        }

        /// <summary> Creates a dot file and uses it in graphviz to generate a nice plot. Generates an extended and a simple variant. </summary>
        (string, string) CreateGraph()
        {
            // Generate a dot file to use in graphviz
            var buffer = new StringBuilder();
            var simplebuffer = new StringBuilder();

            string header = "digraph {\n\tnode [fontname=\"Roboto\", shape=cds, fontcolor=\"blue\", color=\"blue\"];\n\tgraph [rankdir=\"LR\"];\n\t edge [arrowhead=vee, color=\"blue\"];\n";
            buffer.AppendLine(header);
            simplebuffer.AppendLine(header);
            string style;

            for (int i = 0; i < Parameters.Assembler.condensed_graph.Count(); i++)
            {
                //Test if it is a starting node
                if (Parameters.Assembler.condensed_graph[i].BackwardEdges.Count() == 0 || (Parameters.Assembler.condensed_graph[i].BackwardEdges.Count() == 1 && Parameters.Assembler.condensed_graph[i].BackwardEdges.ToArray()[0] == i))
                {
                    style = ", style=filled, fillcolor=\"blue\", fontcolor=\"white\"";
                }
                else style = "";

                string id = GetAsideIdentifier(i, AsideType.Contig);

                buffer.AppendLine($"\t{id} [label=\"{AminoAcid.ArrayToString(Parameters.Assembler.condensed_graph[i].Sequence.ToArray())}\"{style}]");

                simplebuffer.AppendLine($"\t{id} [label=\"{id}\"{style}]");

                foreach (var fwe in Parameters.Assembler.condensed_graph[i].ForwardEdges)
                {
                    string fid = GetAsideIdentifier(fwe, AsideType.Contig);
                    buffer.AppendLine($"\t{id} -> {fid}");
                    simplebuffer.AppendLine($"\t{id} -> {fid}");
                }
            }

            buffer.AppendLine("}");
            simplebuffer.AppendLine("}");

            // Generate SVG files of the graph
            try
            {
                Process svg = new Process()
                {
                    StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/bin/dot.exe" : "dot", "-Tsvg")
                    {
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };

                Process simplesvg = new Process()
                {
                    StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/bin/dot.exe" : "dot", "-Tsvg")
                    {
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };

                svg.Start();
                simplesvg.Start();

                StreamWriter svgwriter = svg.StandardInput;
                svgwriter.WriteLine(buffer.ToString());
                svgwriter.Write(0x4); // End of input
                svgwriter.Close();

                StreamWriter simplesvgwriter = simplesvg.StandardInput;
                simplesvgwriter.WriteLine(simplebuffer.ToString());
                simplesvgwriter.Write(0x4); // End of input
                simplesvgwriter.Close();

                var svggraph = svg.StandardOutput.ReadToEnd();
                if (svggraph == "")
                {
                    var svgstderr = svg.StandardError.ReadToEnd();
                    Console.WriteLine("EXTENDED SVG ERROR: " + svgstderr);
                }

                var simplesvggraph = simplesvg.StandardOutput.ReadToEnd();
                if (simplesvggraph == "")
                {
                    var simplesvgstderr = simplesvg.StandardError.ReadToEnd();
                    Console.WriteLine("SIMPLE SVG ERROR: " + simplesvgstderr);
                }

                string c = GetAsidePrefix(AsideType.Contig);
                // Give the filled nodes (start node) the correct class
                svggraph = Regex.Replace(svggraph, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
                // Give the nodes the correct ID
                svggraph = Regex.Replace(svggraph, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>([A-Z][0-9]+)</title>", "id=\"node-$2\" class=\"$1\" onclick=\"Select('$2')\">");
                // Strip all <title> tags
                svggraph = Regex.Replace(svggraph, "<title>[^<]*</title>", "");

                // Give the filled nodes (start node) the correct class
                simplesvggraph = Regex.Replace(simplesvggraph, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
                // Give the nodes the correct ID
                simplesvggraph = Regex.Replace(simplesvggraph, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>([A-Z][0-9]+)</title>", $"id=\"simple-node-$2\" class=\"$1\" onclick=\"Select('$2')\">");
                // Strip all <title> tags
                simplesvggraph = Regex.Replace(simplesvggraph, "<title>[^<]*</title>", "");

                // Add all paths as classes to the nodes
                for (int i = 0; i < Parameters.Assembler.condensed_graph.Count(); i++)
                {
                    string extra_classes;
                    try
                    {
                        extra_classes = AllPathsContaining(Parameters.Assembler.condensed_graph[i].Index).Aggregate("", (a, b) => a + " " + GetAsideIdentifier(b.Index, AsideType.Path)).Substring(1);
                    }
                    catch
                    {
                        extra_classes = "";
                    }
                    svggraph = svggraph.Replace($"id=\"node-{GetAsideIdentifier(i, AsideType.Contig)}\" class=\"", $"id=\"node-{GetAsideIdentifier(i, AsideType.Contig)}\" class=\"{extra_classes} ");
                    simplesvggraph = simplesvggraph.Replace($"id=\"simple-node-{GetAsideIdentifier(i, AsideType.Contig)}\" class=\"", $"id=\"simple-node-{GetAsideIdentifier(i, AsideType.Contig)}\" class=\"{extra_classes} ");
                }

                return (svggraph, simplesvggraph);
            }
            catch (Win32Exception)
            {
                if (UseIncludedDotDistribution)
                    new InputNameSpace.ErrorMessage("", "Could not start Dot", "Please make sure Dot (Graphviz) is installed and added to your PATH.").Print();
                else
                    new InputNameSpace.ErrorMessage("", "Could not start Dot", "Make sure you execute this program when the assets folder is accessible.", "Dot is included for Windows in the assets folder.").Print();
                return ("<p>Could not start Dot</p>", "<p>Could not start Dot</p>");
            }
            catch (Exception e)
            {
                throw new Exception("Unexpected exception when trying call dot to build graph: " + e.Message + e.StackTrace);
            }
        }

        /// <summary> Create HTML with all reads in a table. With annotations for sorting the table. </summary>
        /// <returns> Returns an HTML string. </returns>
        string CreateReadsTable()
        {
            var buffer = new StringBuilder();

            buffer.Append(TableHeader("reads", reads.Select(a => (double)a.Count())));

            buffer.AppendLine(@"<table id=""reads-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('reads-table', 0, 'string')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('reads-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('reads-table', 2, 'number')"" class=""smallcell"">Sequence Length</th>
</tr>");
            string id, link;

            for (int i = 0; i < reads.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Read);
                link = GetAsideLink(i, AsideType.Read);
                buffer.AppendLine($@"<tr id=""reads-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{reads[i]}</td>
    <td class=""center"">{reads[i].Length}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        /// <summary> Returns a table containing all the contigs of a alignment. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateContigsTable()
        {
            var buffer = new StringBuilder();

            buffer.Append(TableHeader("contigs", Parameters.Assembler.condensed_graph.Select(a => (double)a.Sequence.Count()), Parameters.Assembler.condensed_graph.Select(a => a.TotalArea)));

            buffer.AppendLine(@"<table id=""contigs-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('contigs-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('contigs-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('contigs-table', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('contigs-table', 3, 'string')"" class=""smallcell"">Forks to</th>
    <th onclick=""sortTable('contigs-table', 4, 'string')"" class=""smallcell"">Forks from</th>
    <th onclick=""sortTable('contigs-table', 5, 'string')"">Based on</th>
</tr>");
            string id, link;

            for (int i = 0; i < Parameters.Assembler.condensed_graph.Count(); i++)
            {
                var item = Parameters.Assembler.condensed_graph[i];
                id = GetAsideIdentifier(i, AsideType.Contig);
                link = GetAsideLink(i, AsideType.Contig);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(item.Sequence.ToArray())}</td>
    <td class=""center"">{item.Sequence.Count()}</td>
    <td class=""center"">{item.ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Contig))}</td>
    <td class=""center"">{item.BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Contig))}</td>
    <td>{item.UniqueOrigins.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Read))}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        string CreateTemplateTables(List<TemplateDatabase> databases, int templateGroup)
        {
            var buffer = new List<(int, string)>();

            Parallel.ForEach(
                databases.Select((item, index) => (index, item)),
                new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                set => buffer.Add((set.index, Collapsible($"Template Matching {set.item.Name}", CreateTemplateTable(set.item.Templates, templateGroup, set.index, AsideType.Template, true))))
            );

            buffer.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            var output = new StringBuilder();

            foreach (var line in buffer)
            {
                output.Append(line.Item2);
            }

            return output.ToString();
        }

        string CreateTemplateTable(List<Template> templates, int templateGroup, int templateIndex, AsideType type, bool header = false)
        {
            var buffer = new StringBuilder();
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            if (header) buffer.Append(TableHeader(templates));
            string unique = "";
            if (displayUnique) unique = $"<th onclick=\"sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 6, 'number')\" class=\"smallcell\">Unique Area</th>";

            buffer.AppendLine($@"<table id=""template-table-{type}-{templateIndex}"" class=""widetable"">
<tr>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 1, 'string')"">Consensus Sequence</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 3, 'number')"" class=""smallcell"">Score</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 4, 'number')"" class=""smallcell"">Reads</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 5, 'number')"" class=""smallcell"">Total Area</th>
    {unique}
</tr>");

            string id, link;
            for (int i = 0; i < templates.Count(); i++)
            {
                id = GetAsideIdentifier(templateGroup, templateIndex, i, type);
                link = GetAsideLink(templateGroup, templateIndex, i, type);
                if (displayUnique) unique = $"<td class=\"center\">{templates[i].TotalUniqueArea.ToString("G3", new CultureInfo("en-GB"))}</td>";
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(templates[i].ConsensusSequence().Item1)}</td>
    <td class=""center"">{templates[i].Sequence.Length}</td>
    <td class=""center"">{templates[i].Score}</td>
    <td class=""center"">{templates[i].Matches.Count()}</td>
    <td class=""center"">{templates[i].TotalArea.ToString("G3", new CultureInfo("en-GB"))}</td>
    {unique}
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        string CreatePathsTable()
        {
            var buffer = new StringBuilder();

            buffer.Append(TableHeader("paths", Parameters.Paths.Select(a => (double)a.Sequence.Count()), Parameters.Paths.Select(a => a.MetaData.TotalArea)));

            buffer.AppendLine(@"<table id=""paths-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('paths-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('paths-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('paths-table', 2, 'number')"" class=""smallcell"">Length</th>
</tr>");
            string id, link;

            for (int i = 0; i < Parameters.Paths.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Path);
                link = GetAsideLink(i, AsideType.Path);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(Parameters.Paths[i].Sequence)}</td>
    <td class=""center"">{Parameters.Paths[i].Sequence.Length}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        /// <summary> Returns an aside for details viewing of a contig. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateContigAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Contig);
            var location = new List<string>() { AssetsFolderName, GetAsideName(AsideType.Contig) + "s" };
            string prefix = "";
            if (Parameters.Assembler.condensed_graph[i].Prefix != null) prefix = AminoAcid.ArrayToString(Parameters.Assembler.condensed_graph[i].Prefix.ToArray());
            string suffix = "";
            if (Parameters.Assembler.condensed_graph[i].Suffix != null) suffix = AminoAcid.ArrayToString(Parameters.Assembler.condensed_graph[i].Suffix.ToArray());

            var readsalignment = CreateReadsAlignment(Parameters.Assembler.condensed_graph[i], location);

            return $@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig {GetAsideIdentifier(i, AsideType.Contig, true)}</h1>
    <h2>Sequence (length={Parameters.Assembler.condensed_graph[i].Sequence.Count()})</h2>
    <p class=""aside-seq""><span class='prefix'>{prefix}</span>{AminoAcid.ArrayToString(Parameters.Assembler.condensed_graph[i].Sequence.ToArray())}<span class='suffix'>{suffix}</span></p>
    <h2>Total Area</h2>
    <p>{Parameters.Assembler.condensed_graph[i].TotalArea}</p>
    <h2>Reads Alignment</h4>
    {readsalignment.Item1}
    <h2>Based on</h2>
    <p>{Parameters.Assembler.condensed_graph[i].UniqueOrigins.Aggregate("", (a, b) => a + " " + GetAsideLink(b, AsideType.Read, location))}</p>
</div>";
        }

        /// <summary> Returns an aside for details viewing of a read. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateReadAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Read);
            string meta = reads_metadata[i].ToHTML();
            return $@"<div id=""{id}"" class=""info-block read-info"">
    <h1>Read {GetAsideIdentifier(i, AsideType.Read, true)}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{reads[i]}</p>
    <h2>Sequence Length</h2>
    <p>{reads[i].Length}</p>
    {meta}
</div>";
        }

        /// <summary>
        /// Creates an aside for a path.
        /// </summary>
        /// <param name="i">The index.</param>
        /// <returns>valid HTML.</returns>
        string CreatePathAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Path);
            var location = new List<string>() { AssetsFolderName, GetAsideName(AsideType.Path) + "s" };
            var sb = new StringBuilder();
            var templateString = "";

            // Find the highest scoring templates to link back to
            if (Parameters.TemplateDatabases != null)
            {
                const int number = 10;
                var best_templates = new List<(int, int, int, int)>();
                int cutoff = 0;

                // Pick each database group
                for (int l = 0; l < Parameters.TemplateDatabases.Count(); l++)
                { // Pick each database
                    for (int k = 0; k < Parameters.TemplateDatabases[l].Item2.Count(); k++)
                    { // Pick each template
                        for (int j = 0; j < Parameters.TemplateDatabases[l].Item2[k].Templates.Count(); j++)
                        {
                            var match = Parameters.TemplateDatabases[l].Item2[k].Templates[j].Matches.Find(sm => sm.Index == i);
                            if (match != null && (match.Score > cutoff || best_templates.Count() < number))
                            {
                                best_templates.Add((l, k, j, match.Score));
                                best_templates.Sort((a, b) => b.Item3.CompareTo(a.Item3));

                                int count = best_templates.Count();
                                if (count > number) best_templates.RemoveRange(10, count - number);

                                cutoff = best_templates.Last().Item3;
                            }
                        }
                    }
                }

                foreach (var tem in best_templates)
                {
                    sb.Append($"<tr><td>{tem.Item4}</td><td>{GetAsideLink(tem.Item1, tem.Item2, tem.Item3, AsideType.Template, location)}</td></tr>");
                }

                templateString = sb.ToString().Length == 0 ? "" : $"<h2>Top {number} templates</h2><table><tr><th>Score</th><th>Template</th></tr>{sb}</table>";
            }

            sb.Clear();
            var maxCoverage = new List<double>();
            foreach (var node in Parameters.Paths[i].Nodes)
            {
                var core = CreateReadsAlignmentCore(node, location);
                sb.Append(core.Item1);
                maxCoverage.Add(core.Item2);
            }

            return $@"<div id=""{id}"" class=""info-block read-info path-info"">
    <h1>Path {GetAsideIdentifier(i, AsideType.Path, true)}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(Parameters.Paths[i].Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{Parameters.Paths[i].Sequence.Length}</p>
    <h2>Total Area</h2>
    <p>{Parameters.Paths[i].MetaData.TotalArea}</p>
    <h2>Path</h2>
    <p>{Parameters.Paths[i].Nodes.Aggregate("", (a, b) => a + " → " + GetAsideLink(b.Index, AsideType.Contig, location)).Substring(3)}</p>
    <h2>Alignment</h2>
    <div class=""reads-alignment"" style=""--max-value:{maxCoverage.Max()}"">
    {sb}
    </div>
    {templateString}
</div>";
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateTemplateAside(AsideType type, Template template, int superindex, int index, int i)
        {
            string id = GetAsideIdentifier(superindex, index, i, type);
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };
            var alignment = CreateTemplateAlignment(template, id, location);
            var (consensus_sequence, consensus_doc) = template.ConsensusSequence();

            string meta = "";
            if (template.MetaData != null && type == AsideType.Template)
            {
                meta = template.MetaData.ToHTML();
            }

            string based = "";
            switch (type)
            {
                case AsideType.ReadAlignment:
                    based = $"<h2>Based on</h2><p>{GetAsideLink(i, AsideType.RecombinedTemplate, location)}</p>";
                    break;
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null)
                        based = $"<h2>Order</h2><p>{template.Recombination.Aggregate("", (a, b) => a + " → " + GetAsideLink(superindex, b.Location.TemplateDatabaseIndex, b.Location.TemplateIndex, AsideType.Template, location)).Substring(3)}</p>";
                    break;
                default:
                    break;
            }

            string unique = "";
            if (template.ForcedOnSingleTemplate)
            {
                unique = $@"<h2>Unique Matches</h2>
    <p>{template.UniqueMatches}</p>
    <h2>Unique Area</h2>
    <p>{template.TotalUniqueArea}</p>
    <h2>Unique Score</h2>
    <p>{template.UniqueScore}</p>";
            }

            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {GetAsideIdentifier(superindex, index, i, type, true)}</h1>
    <h2>Consensus Sequence</h2>
    <p class='aside-seq'>{AminoAcid.ArrayToString(consensus_sequence)}</p>
    <h2>Sequence Consensus Overview</h2>
    {SequenceConsensusOverview(template)}
    <div class='docplot'><h2>Depth Of Coverage of the Consensus Sequence</h2>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(consensus_doc))}</div>
    <h2>Sequence Length</h2>
    <p>{template.Sequence.Length}</p>
    <h2>Total Matches</h2>
    <p>{template.Matches.Count()}</p>
    <h2>Total Area</h2>
    <p>{template.TotalArea}</p>
    <h2>Score</h2>
    <p>{template.Score}</p>
    {unique}
    {based}
    {alignment.Alignment}
    {CreateTemplateGraphs(template, alignment.DepthOfCoverage)}
    <h2>Template Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    {meta}
</div>";
        }

        string CreateTemplateGraphs(Template template, List<double> DepthOfCoverage)
        {
            if (template.Matches.Count == 0) return "";
            var buffer = new StringBuilder();
            buffer.Append("<h3>Graphs</h3><div class='template-graphs'>");

            buffer.Append($"<div class='docplot'><h3>Depth Of Coverage over the template</h3>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(DepthOfCoverage))}</div>");

            buffer.Append($"<div class='docplot'><h3>Depth Of Coverage over the template (Log10)</h3>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(DepthOfCoverage.Select(a => a == 0 ? 0 : Math.Log10(a)).ToList()))}</div>");

            if (template.ForcedOnSingleTemplate && template.UniqueMatches > 0)
            {
                // Histogram of Scores
                var scores = HTMLGraph.GroupedHistogram(new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.Score).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.Score).ToList(), "Unique") });
                buffer.Append($"<div><h3>Histogram of Score</h3>{scores}</div>");

                // Histogram of Length On Template
                var lengths = HTMLGraph.GroupedHistogram(new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.LengthOnTemplate).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.LengthOnTemplate).ToList(), "Unique") });
                buffer.Append($"<div><h3>Histogram of Length on Template</h3>{lengths}</div>");
            }
            else
            {
                // Histogram of Scores
                buffer.Append($"<div><h3>Histogram of Score</h3>{HTMLGraph.Histogram(template.Matches.Select(a => (double)a.Score).ToList())}</div>");

                // Histogram of Length On Template
                buffer.Append($"<div><h3>Histogram of Length on Template</h3>{HTMLGraph.Histogram(template.Matches.Select(a => (double)a.LengthOnTemplate).ToList())}</div>");
            }

            // Histogram of coverage, coverage per position excluding gaps
            buffer.Append($"<div><h3>Histogram of Coverage</h3>{HTMLGraph.Histogram(template.CombinedSequence().Select(a => a.AminoAcids.Values.Sum()).ToList())}<i>Excludes gaps in reference to the template sequence</i></div>");

            buffer.Append("</div>");
            return buffer.ToString();
        }

        (string Alignment, List<double> DepthOfCoverage) CreateTemplateAlignment(Template template, string id, List<string> location)
        {
            var buffer = new StringBuilder();
            var alignedSequences = template.AlignedSequences();

            if (alignedSequences.Count() == 0)
                return ("", new List<double>());

            buffer.Append("<h2>Alignment</h2>");

            // Loop over aligned
            // For each position: (creates List<string[]>, per position, per sequence + templatesequence)
            // Convert AA to string (fill in with gapchar)
            // Convert Gap to string (get max length, align all gaps, fill in with spaces)

            // Convert to lines: (creates List<string>)
            // Combine horizontally
            var totalsequences = alignedSequences[0].Sequences.Count();
            var lines = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>[totalsequences + 1];
            const char gapchar = '-';
            const char nonbreakingspace = '\u00A0';
            var depthOfCoverage = new List<double>();
            int read_offset = Parameters.Assembler != null ? Parameters.Assembler.reads.Count() : Parameters.Input.Count();

            for (int i = 0; i < totalsequences + 1; i++)
            {
                lines[i] = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>();
            }

            for (int template_pos = 0; template_pos < alignedSequences.Count(); template_pos++)
            {
                var (Sequences, Gaps) = alignedSequences[template_pos];
                lines[0].Add((template.Sequence[template_pos].ToString(), -1, -1, AsideType.Path));
                double depth = 0;

                // Add the aligned amino acid
                for (int i = 0; i < Sequences.Length; i++)
                {
                    int index = Sequences[i].SequencePosition;
                    depth += Sequences[i].CoverageDepth;

                    if (index == -1)
                    {
                        lines[i + 1].Add((gapchar.ToString(), -1, -1, AsideType.Path));
                    }
                    else if (index == 0)
                    {
                        lines[i + 1].Add((nonbreakingspace.ToString(), -1, -1, AsideType.Path));
                    }
                    else
                    {
                        var type = AsideType.Read;
                        var idx = Sequences[i].MatchIndex;
                        if (template.Matches[Sequences[i].MatchIndex].MetaData is MetaData.Path)
                        {
                            type = AsideType.Path;
                        }

                        lines[i + 1].Add((template.Matches[Sequences[i].MatchIndex].QuerySequence[index - 1].ToString(), idx, index - 1, type));
                    }
                }

                depthOfCoverage.Add(depth);

                // Add the gap
                // TODO: Unaligned for now: the gaps are not aligned to each other, this could make it more good looking
                int max_length = 0;
                // Get the max length of the gaps 
                for (int i = 0; i < Gaps.Length; i++)
                {
                    if (Gaps[i].Gap != null && Gaps[i].Gap.ToString().Length > max_length)
                    {
                        max_length = Gaps[i].Gap.ToString().Length;
                    }
                }
                // Add gap to the template
                lines[0].Add((new string(gapchar, max_length), -1, -1, AsideType.Path));

                var depthGap = new List<double[]>();
                // Add gap to the lines
                for (int i = 0; i < Gaps.Length; i++)
                {
                    string seq;
                    if (Gaps[i].Gap == null)
                    {
                        seq = "";
                        depthGap.Add(Enumerable.Repeat(0.0, max_length).ToArray());
                    }
                    else
                    {
                        seq = Gaps[i].Gap.ToString();
                        var d = new double[max_length];
                        Gaps[i].CoverageDepth.CopyTo(d, max_length - Gaps[i].CoverageDepth.Length);
                        depthGap.Add(d);
                    }

                    char padchar = nonbreakingspace;
                    if (Gaps[i].InSequence) padchar = gapchar;

                    var index = Gaps[i].ContigID == -1 ? -1 : Gaps[i].MatchIndex;

                    var type = AsideType.Path;
                    var idx = index;
                    if (Gaps[i].MatchIndex >= 0 && !(template.Matches[Gaps[i].MatchIndex].MetaData is MetaData.Path))
                    {
                        type = AsideType.Read;
                    }

                    lines[i + 1].Add((seq.PadRight(max_length, padchar), idx, Sequences[i].SequencePosition - 1, type));
                }
                var depthGapCombined = new double[max_length];
                foreach (var d in depthGap)
                {
                    depthGapCombined = depthGapCombined.ElementwiseAdd(d);
                }
                depthOfCoverage.AddRange(depthGapCombined.Select(a => (double)a));
            }

            var aligned = new string[alignedSequences[0].Sequences.Count() + 1];
            var types = new List<AsideType>[alignedSequences[0].Sequences.Count() + 1];

            for (int i = 0; i < alignedSequences[0].Sequences.Count() + 1; i++)
            {
                StringBuilder sb = new StringBuilder();
                var typ = new List<AsideType>();

                foreach ((var text, _, _, var type) in lines[i])
                {
                    sb.Append(text);
                    typ.AddRange(Enumerable.Repeat(type, text.Length));
                }

                types[i] = typ;
                aligned[i] = sb.ToString();
            }

            buffer.AppendLine($"<div class=\"reads-alignment\" style=\"--max-value:{depthOfCoverage.Max()}\">");

            // Create the front overhanging reads block
            var frontoverhangbuffer = new StringBuilder();
            bool frontoverhang = false;
            frontoverhangbuffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            frontoverhangbuffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");

            for (int i = 1; i < aligned.Count(); i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition != 0 && match.StartTemplatePosition == 0)
                {
                    frontoverhang = true;
                    frontoverhangbuffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(0, match.StartQueryPosition))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    frontoverhangbuffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }

            if (frontoverhang)
            {
                buffer.Append(frontoverhangbuffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Chop it up, add numbers etc
            const int blocklength = 5;

            if (aligned.Length > 0)
            {
                int alignedindex = 0;
                int alignedlength = 0;
                for (int block = 0; block * blocklength < aligned[0].Length; block++)
                {
                    // Get the right id's to generate the right links
                    while (alignedlength < block * blocklength && alignedindex + 1 < lines[0].Count())
                    {
                        alignedlength += lines[0][alignedindex].Sequence.Length;
                        alignedindex++;
                    }

                    var indices = new int[aligned.Length];
                    var positions = new int[aligned.Length];

                    for (int i = 1; i < aligned.Length; i++)
                    {
                        int index = lines[i][alignedindex].Index;
                        int position = lines[i][alignedindex].SequencePosition;
                        int additionallength = 0;
                        int additionalindex = 1;

                        while (alignedlength + additionallength < (block + 1) * blocklength && alignedindex + additionalindex < lines[0].Count())
                        {
                            int thisindex = lines[i][alignedindex + additionalindex].Index;
                            int thisposition = lines[i][alignedindex + additionalindex].SequencePosition;

                            if (index == -1)
                            {
                                index = thisindex;
                                position = thisposition;
                            }
                            else if (thisindex != -1 && thisindex != index)
                            {
                                // If two reads are on this patch just set the link to none.
                                index = -2;
                                position = -2;
                                break;
                            }

                            additionallength += lines[0][alignedindex + additionalindex].Sequence.Length;
                            additionalindex++;
                        }

                        indices[i] = index;
                        positions[i] = position;
                    }


                    // Add the sequence and the number to tell the position
                    string number = "";
                    if (aligned[0].Length - block * blocklength >= blocklength)
                    {
                        number = ((block + 1) * blocklength).ToString();
                        number = string.Concat(Enumerable.Repeat("&nbsp;", blocklength - number.Length)) + number;
                    }
                    buffer.Append($"<div class='align-block'><p><span class=\"number\">{number}</span><br><span class=\"seq\">{aligned[0].Substring(block * blocklength, Math.Min(blocklength, aligned[0].Length - block * blocklength))}</span><br>");

                    StringBuilder alignblock = new StringBuilder();
                    for (int i = 1; i < aligned.Length; i++)
                    {
                        string result = "";
                        if (indices[i] >= 0)
                        {
                            var rid = "none";
                            var name = GetAsideName(AsideType.Read);
                            var unique = "";
                            try
                            {
                                var meta = template.Matches[indices[i]].MetaData;
                                if (template.Matches[indices[i]].Unique) unique = " unique";
                                rid = meta.EscapedIdentifier;
                                if (meta.GetType() == typeof(MetaData.Path)) name = GetAsideName(AsideType.Path);
                            }
                            catch { }
                            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, name + "s" }, location) + rid.Replace(':', '-') + ".html?pos=" + positions[i];
                            if (aligned[i].Length > block * blocklength) result = $"<a href=\"{path}\" class=\"align-link{unique}\" onmouseover=\"AlignmentDetails({template.Matches[indices[i]].Index})\" onmouseout=\"AlignmentDetailsClear()\">{aligned[i].Substring(block * blocklength, Math.Min(blocklength, aligned[i].Length - block * blocklength))}</a>";
                        }
                        else if (indices[i] == -2) // Clashing sequences remove link but display sequence
                        {
                            if (aligned[i].Length > block * blocklength) result = $"<a href=\"#\" class=\"align-link clash\">{aligned[i].Substring(block * blocklength, Math.Min(blocklength, aligned[i].Length - block * blocklength))}</a>";
                        }
                        alignblock.Append(result);
                        alignblock.Append("<br>");
                    }
                    buffer.Append(alignblock.ToString().TrimEnd("<br>"));
                    buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");

                    for (int i = block * blocklength; i < block * blocklength + Math.Min(blocklength, depthOfCoverage.Count() - block * blocklength); i++)
                    {
                        buffer.Append($"<span class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'></span>");
                    }
                    buffer.Append("</div></div>");
                }
            }

            // Create the end overhanging reads block
            var endoverhangbuffer = new StringBuilder();
            bool endoverhang = false;
            endoverhangbuffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"end-overhang-toggle-{id}\"/><label for=\"end-overhang-toggle-{id}\">");
            endoverhangbuffer.AppendFormat("<div class='align-block overhang-block end-overhang'><p><span class='end-overhang-spacing'></span>");
            for (int i = 1; i < aligned.Count(); i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition + match.TotalMatches < match.QuerySequence.Length && match.StartTemplatePosition + match.TotalMatches == match.TemplateSequence.Length)
                {
                    endoverhang = true;
                    endoverhangbuffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + match.TotalMatches, match.QuerySequence.Length - match.StartQueryPosition - match.TotalMatches))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    endoverhangbuffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }
            if (endoverhang)
            {
                buffer.Append(endoverhangbuffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Index menus
            buffer.Append("<div id='index-menus'>");
            foreach (var match in template.Matches)
            {
                buffer.Append(AlignmentDetails(match, template));
            }

            buffer.AppendLine("</div></div>");
            return (buffer.ToString(), depthOfCoverage);
        }

        string AlignmentDetails(SequenceMatch match, Template template)
        {
            var doctitle = "Positional Score";
            var type = "Read";

            if (match.MetaData is MetaData.Path)
            {
                doctitle = "Depth of Coverage";
                type = "Path";
            }

            var unique = "";
            if (template.ForcedOnSingleTemplate) unique = "<tr><td>Unique</td><td>" + (match.Unique ? "Yes" : "No") + "</td></tr>";

            return $@"
    <div class='alignment-details' id='alignment-details-{match.Index}'>
        <h4>{match.MetaData.Identifier}</h4>
        <table>
            <tr>
                <td>Type</td>
                <td>{type}</td>
            </tr>
            <tr>
                <td>Score</td>
                <td>{match.Score}</td>
            </tr>
            <tr>
                <td>Total Area</td>
                <td>{match.MetaData.TotalArea:G4}</td>
            </tr>
            <tr>
                <td>Length on Template</td>
                <td>{match.LengthOnTemplate}</td>
            </tr>
            <tr>
                <td>Position on Template</td>
                <td>{match.StartTemplatePosition}</td>
            </tr>
            <tr>
                <td>Start on {type}</td>
                <td>{match.StartQueryPosition}</td>
            </tr>
            <tr>
                <td>Length of {type}</td>
                <td>{match.QuerySequence.Length}</td>
            </tr>
            {unique}
            <tr>
                <td>{doctitle}</td>
                <td class='docplot'>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(match.MetaData.PositionalScore.SubArray(match.StartQueryPosition, match.TotalMatches).Select(a => (double)a).ToList(), match.StartQueryPosition))}</td>
            </tr>
            <tr>
                <td>Alignment graphic</td>
                <td>{SequenceMatchGraphic(match)}</td>
            </tr>
        </table>
    </div>";
        }

        string SequenceMatchGraphic(SequenceMatch match)
        {
            var buffer = new StringBuilder("<div class='sequence-match-graphic'>");
            var id = "none";
            foreach (var piece in match.Alignment)
            {
                if (piece is SequenceMatch.Match)
                    id = "match";
                else if (piece is SequenceMatch.GapInTemplate)
                    id = "gap-in-template";
                else if (piece is SequenceMatch.GapInQuery)
                    id = "gap-in-query";

                buffer.Append($"<span class='{id}' style='flex-grow:{piece.Length}'></span>");
            }
            buffer.Append("</div>");
            return buffer.ToString();
        }

        string SequenceConsensusOverview(Template template)
        {
            const double threshold = 0.3;
            const int height = 50;
            const int fontsize = 20;
            var consensus_sequence = template.CombinedSequence();

            var sequence_logo_buffer = new StringBuilder();

            sequence_logo_buffer.Append($"<div class='sequence-logo' style='--sequence-logo-height:{height}px;--sequence-logo-fontsize:{fontsize}px;'>");
            for (int i = 0; i < consensus_sequence.Count(); i++)
            {
                sequence_logo_buffer.Append("<div class='sequence-logo-position'>");
                // Get the highest chars
                double sum = 0;
                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    sum += item.Value;
                }

                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    if ((double)item.Value / sum > threshold)
                    {
                        var size = (item.Value / sum * height / fontsize * 0.75).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        sequence_logo_buffer.Append($"<span style='font-size:{size}em'>{item.Key}</span>");
                    }
                }
                sequence_logo_buffer.Append("</div><div class='sequence-logo-position sequence-logo-gap'>");
                // Get the highest chars
                int gap_sum = 0;
                foreach (var item in consensus_sequence[i].Gaps)
                {
                    gap_sum += item.Value.Count;
                }

                foreach (var item in consensus_sequence[i].Gaps)
                {
                    if ((double)item.Value.Count / gap_sum > threshold)
                    {
                        var size = ((double)item.Value.Count / gap_sum * height / fontsize * 0.75).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

                        if (item.Key == (Template.IGap)new Template.None())
                        {
                            sequence_logo_buffer.Append($"<span style='font-size:{size}em'>*</span>");
                        }
                        else
                        {
                            sequence_logo_buffer.Append($"<span style='font-size:{size}em'>{item.Key}</span>");
                        }
                    }
                }
                sequence_logo_buffer.Append("</div>");
            }
            sequence_logo_buffer.Append("</div>");
            return sequence_logo_buffer.ToString();
        }

        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        void CreateAsides()
        {
            var jobbuffer = new List<(AsideType, int, int, int)>();

            void ExecuteJob(AsideType aside, int index3, int index2, int index1)
            {
                switch (aside)
                {
                    case AsideType.Path: SaveAside(CreatePathAside(index1), AsideType.Path, -1, -1, index1); break;
                    case AsideType.Contig: SaveAside(CreateContigAside(index1), AsideType.Contig, -1, -1, index1); break;
                    case AsideType.Read: SaveAside(CreateReadAside(index1), AsideType.Read, -1, -1, index1); break;
                    case AsideType.Template: SaveTemplateAside(Parameters.TemplateDatabases[index3].Item2[index2].Templates[index1], AsideType.Template, index3, index2, index1); break;
                    case AsideType.RecombinedTemplate: SaveTemplateAside(Parameters.RecombinedDatabase[index3].Templates[index1], AsideType.RecombinedTemplate, index3, index2, index1); break;
                    case AsideType.ReadAlignment: SaveTemplateAside(Parameters.ReadAlignment[index3].Templates[index1], AsideType.ReadAlignment, index3, index2, index1); break;
                };
            }

            if (Parameters.Assembler != null)
            {
                // Path Asides
                for (int i = 0; i < Parameters.Paths.Count(); i++)
                {
                    jobbuffer.Add((AsideType.Path, -1, -1, i));
                }
                // Contigs Asides
                for (int i = 0; i < Parameters.Assembler.condensed_graph.Count(); i++)
                {
                    jobbuffer.Add((AsideType.Contig, -1, -1, i));
                }
            }
            // Read Asides
            for (int i = 0; i < reads.Count(); i++)
            {
                jobbuffer.Add((AsideType.Read, -1, -1, i));
            }
            // Template Tables Asides
            if (Parameters.TemplateDatabases != null)
            {
                for (int i = 0; i < Parameters.TemplateDatabases.Count(); i++)
                    for (int j = 0; j < Parameters.TemplateDatabases[i].Item2.Count(); j++)
                        for (int k = 0; k < Parameters.TemplateDatabases[i].Item2[j].Templates.Count(); k++)
                            jobbuffer.Add((AsideType.Template, i, j, k));
            }
            // Recombination Table Asides
            if (Parameters.RecombinedDatabase != null)
            {
                for (int i = 0; i < Parameters.RecombinedDatabase.Count(); i++)
                    for (int j = 0; j < Parameters.RecombinedDatabase[i].Templates.Count(); j++)
                        jobbuffer.Add((AsideType.RecombinedTemplate, i, -1, j));
            }
            // Reads Alignment Table Asides
            if (Parameters.ReadAlignment != null)
            {
                for (int i = 0; i < Parameters.ReadAlignment.Count(); i++)
                    for (int j = 0; j < Parameters.ReadAlignment[i].Templates.Count(); j++)
                        jobbuffer.Add((AsideType.ReadAlignment, i, -1, j));
            }
            if (MaxThreads > 1)
            {
                Parallel.ForEach(
                    jobbuffer,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (a, _) => ExecuteJob(a.Item1, a.Item2, a.Item3, a.Item4)
                );
            }
            else
            {
                foreach (var (t, i3, i2, i1) in jobbuffer)
                {
                    ExecuteJob(t, i3, i2, i1);
                }
            }
        }

        void SaveTemplateAside(Template template, AsideType type, int index3, int index2, int index1)
        {
            SaveAside(CreateTemplateAside(type, template, index3, index2, index1), type, index3, index2, index1);
        }

        void SaveAside(string content, AsideType type, int index3, int index2, int index1)
        {
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };
            var homelocation = GetLinkToFolder(new List<string>(), location) + AssetsFolderName + ".html";
            var id = GetAsideIdentifier(index3, index2, index1, type);
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

        /// <summary> Create a reads alignment to display in the sidebar. </summary>
        /// <returns> Returns an HTML string. </returns>
        (string, List<int>) CreateReadsAlignment(CondensedNode node, List<string> location)
        {
            var placed = node.Alignment;
            var sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());

            var buffer = new StringBuilder();
            buffer.AppendLine("<div class=\"reads-alignment\">");

            // Create the front overhanging reads block
            string id = GetAsideIdentifier(node.Index, AsideType.Contig);
            buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            buffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");

            foreach (var line in placed)
            {
                string result = "<br>";
                foreach (var read in line)
                {
                    if (read.StartPosition == 0 && read.StartOverhang != "")
                    {
                        string rid = GetAsideIdentifier(read.Identifier, AsideType.Read);
                        result = $"<a href=\"#{rid}\" class='text align-link'>{read.StartOverhang}</a><span class='symbol'>...</span><br>";
                    }
                }
                buffer.Append(result);
            }
            buffer.AppendLine($"</p></div></label></div>");

            // Add the core alignment
            var core = CreateReadsAlignmentCore(node, location);
            buffer.AppendLine(core.Item1);

            // Create the end overhanging reads block
            buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"end-overhang-toggle-{id}\"/><label for=\"end-overhang-toggle-{id}\">");
            buffer.AppendFormat("<div class='align-block overhang-block end-overhang'><p><span class='end-overhang-spacing'></span>");
            foreach (var line in placed)
            {
                string result = "<br>";
                foreach (var read in line)
                {
                    if (read.EndPosition == sequence.Length && read.EndOverhang != "")
                    {
                        string rid = GetAsideIdentifier(read.Identifier, AsideType.Read);
                        string path = GetLinkToFolder(new List<string>() { AssetsFolderName, GetAsideName(AsideType.Read) + "s" }, location) + rid.Replace(':', '-') + ".html";
                        result = $"<a href=\"{path}\" class='text align-link'>{read.EndOverhang}</a><span class='symbol'>...</span><br>";
                    }
                }
                buffer.Append(result);
            }
            buffer.AppendLine($"</p></div></label></div>");

            // End the reads alignment div
            buffer.AppendLine("</div>");

            return (buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{core.Item2}'>"), node.UniqueOrigins);
        }
        (string, double) CreateReadsAlignmentCore(CondensedNode node, List<string> location)
        {
            var placed = node.Alignment;
            var depthOfCoverage = node.DepthOfCoverageFull;
            var sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            int prefixoffset = node.Prefix.Count();

            var buffer = new StringBuilder();

            const int blocklength = 5;

            // Create the main blocks of the sequence alignment
            for (int pos = 0; pos <= sequence.Length / blocklength; pos++)
            {
                // Add the sequence and the number to tell the position
                string number = "";
                string last = "";
                if (sequence.Length - pos * blocklength >= blocklength)
                {
                    number = ((pos + 1) * blocklength).ToString();
                    number = string.Concat(Enumerable.Repeat("&nbsp;", blocklength - number.Length)) + number;
                }
                else
                {
                    last = " last";
                }
                buffer.Append($"<div class='align-block{last}'><p><span class=\"number\">{number}</span><br><span class=\"seq{last}\">{sequence.Substring(pos * blocklength, Math.Min(blocklength, sequence.Length - pos * blocklength))}</span><br>");

                // Add every line in order
                foreach (var line in placed)
                {
                    for (int i = pos * blocklength; i < pos * blocklength + Math.Min(blocklength, sequence.Length - pos * blocklength); i++)
                    {
                        string result = "&nbsp;";
                        foreach (var read in line)
                        {
                            if (i + prefixoffset >= read.StartPosition && i + prefixoffset < read.EndPosition)
                            {
                                string rid = GetAsideIdentifier(read.Identifier, AsideType.Read);
                                string path = GetLinkToFolder(new List<string>() { AssetsFolderName, GetAsideName(AsideType.Read) + "s" }, location) + rid.Replace(':', '-') + ".html";
                                result = $"<a href=\"{path}\" class=\"align-link\">{read.Sequence[i - read.StartPosition + prefixoffset]}</a>";
                            }
                        }
                        buffer.Append(result);
                    }
                    buffer.Append("<br>");
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = pos * blocklength; i < pos * blocklength + Math.Min(blocklength, sequence.Length - pos * blocklength); i++)
                {
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }

            return (buffer.ToString(), depthOfCoverage.Max());
        }

        /// <summary>An enum to save what type of detail aside it is.</summary>
        enum AsideType { Contig, Read, Template, Path, RecombinedTemplate, ReadAlignment }
        string GetAsidePrefix(AsideType type)
        {
            switch (type)
            {
                case AsideType.Contig:
                    return "C";
                case AsideType.Read:
                    return "R";
                case AsideType.Template:
                    return "T";
                case AsideType.Path:
                    return "P";
                case AsideType.RecombinedTemplate:
                    return "RC";
                case AsideType.ReadAlignment:
                    return "RA";
            }
            throw new ArgumentException("Invalid AsideType in GetAsidePrefix.");
        }
        string GetAsideName(AsideType type)
        {
            switch (type)
            {
                case AsideType.Contig:
                    return "contig";
                case AsideType.Read:
                    return "read";
                case AsideType.Template:
                    return "template";
                case AsideType.Path:
                    return "path";
                case AsideType.RecombinedTemplate:
                    return "recombined-template";
                case AsideType.ReadAlignment:
                    return "read-alignment";
            }
            throw new ArgumentException("Invalid AsideType in GetAsideName.");
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container.</summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index, AsideType type, bool humanvisible = false)
        {
            return GetAsideIdentifier(-1, -1, index, type, humanvisible);
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container.</summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index1, int index2, AsideType type, bool humanvisible = false)
        {
            return GetAsideIdentifier(-1, index1, index2, type, humanvisible);
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="index1">The index in the supercontainer of the supercontrainer. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index3">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index1, int index2, int index3, AsideType type, bool humanvisible = false)
        {
            // Try to use the identifiers as retrieved from the metadata
            MetaData.IMetaData metadata = null;
            if (type == AsideType.Read)
            {
                metadata = reads_metadata[index3];
            }
            else if (type == AsideType.Template)
            {
                metadata = Parameters.TemplateDatabases[index1].Item2[index2].Templates[index3].MetaData;
            }
            if (metadata != null && metadata.Identifier != null)
            {
                if (humanvisible) return metadata.Identifier;
                else return metadata.EscapedIdentifier;
            }

            string pre = GetAsidePrefix(type);

            string i1;
            if (index1 == -1) i1 = "";
            else i1 = $"{index1}:";

            string i2;
            if (index2 == -1) i2 = "";
            else i2 = $"{index2}:";

            string i3;
            if (index3 < 9999) i3 = $"{index3:D4}";
            else i3 = $"{index3}";

            return $"{pre}{i1}{i2}{i3}";
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A valid HTML link.</returns>
        string GetAsideLink(int index, AsideType type, List<string> location = null)
        {
            return GetAsideLink(-1, -1, index, type, location);
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns> A valid HTML link.</returns>
        string GetAsideLink(int index1, int index2, AsideType type, List<string> location = null)
        {
            return GetAsideLink(-1, index1, index2, type, location);
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index1">The index in the supercontainer of the supercontainer. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index3">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns> A valid HTML link.</returns>
        string GetAsideLink(int index1, int index2, int index3, AsideType type, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(index1, index2, index3, type);
            if (id == null) throw new Exception("ID is null");
            string classname = GetAsideName(type);
            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, classname + "s" }, location) + id.Replace(':', '-') + ".html";
            return $"<a href=\"{path}\" class=\"info-link {classname}-link\">{ GetAsideIdentifier(index1, index2, index3, type, true)}</a>";
        }

        string GetLinkToFolder(List<string> target, List<string> location)
        {
            int i = 0;
            for (; i < target.Count() && i < location.Count(); i++)
            {
                if (target[i] != location[i]) break;
            }
            var pieces = new List<string>(location.Count() + target.Count() - 2 * i);
            pieces.AddRange(Enumerable.Repeat("..", location.Count() - i));
            pieces.AddRange(target.Skip(i));
            return string.Join("/", pieces.ToArray()) + "/";
        }

        /// <summary>
        /// Create a collapsible region to be used as a main tab in the report.
        /// </summary>
        /// <param name="name">The name to display.</param>
        /// <param name="content">The content.</param>
        string Collapsible(string name, string content, string extra_id = "")
        {
            string id = (name + extra_id).ToLower().Replace(" ", "-") + "-collapsible";
            return $@"<input type=""checkbox"" id=""{id}""/>
<label for=""{id}"">{name}</label>
<div class=""collapsable"">{content}</div>";
        }

        string TableHeader(List<Template> templates)
        {
            if (templates.Select(a => a.Score).Sum() == 0)
            {
                return "";
            }

            string classname = "";
            string extended = "";
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);

            if (templates[0].Parent.ClassChars > 0)
            {
                var typedata = new Dictionary<string, (double, double, int, double, double)>(templates.Count);
                foreach (var item in templates)
                {
                    if (typedata.ContainsKey(item.Class))
                    {
                        var data = typedata[item.Class];
                        if (data.Item1 < item.Score) data.Item1 = item.Score;
                        data.Item2 += item.Score;
                        data.Item3 += 1;
                        data.Item4 += item.TotalArea;
                        data.Item5 += item.TotalUniqueArea;
                        typedata[item.Class] = data;
                    }
                    else
                    {
                        typedata.Add(item.Class, (item.Score, item.Score, 1, item.TotalArea, item.TotalUniqueArea));
                    }
                }

                var totalData = new List<(string, List<double>)>(typedata.Count);
                foreach (var (type, data) in typedata)
                {
                    var list = new List<double> { data.Item1, data.Item2 / data.Item3, data.Item3, data.Item4 };
                    if (displayUnique) list.Add(data.Item5);
                    totalData.Add((type, list));
                }

                classname = " full";
                var labels = new List<(string, uint)> { ("Max Score", 0), ("Average Score", 0), ("Number of Reads", 1), ("Total Area", 2) };
                if (displayUnique) labels.Add(("Unique Area", 3));

                extended = $@"
<div>
    <h3>Per Type</h3>
    {HTMLGraph.GroupedBargraph(totalData, labels)}
</div>";
            }

            return $@"
<div class='table-header{classname}'>
    <div>
        <h3>Score</h3>
        {HTMLGraph.Histogram(templates.Select(a => (double)a.Score).ToList())}
    </div>
    {extended}
</div>";
        }

        string TableHeader(string identifier, IEnumerable<double> lengths, IEnumerable<double> area = null)
        {
            string extended = "";
            if (area != null) extended = $"<div><h3>Area</h3>{HTMLGraph.Histogram(area.ToList())}</div>";

            return $@"
<div class='table-header-{identifier}'>
    <div>
        <h3>Length</h3>
        {HTMLGraph.Histogram(lengths.ToList())}
    </div>
    {extended}
</div>";
        }

        /// <summary> Returns some meta information about the assembly the help validate the output of the assembly. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string MetaInformation()
        {
            //TODO needs time for recombined template matching
            long number_edges = Parameters.Assembler.graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
            long number_edges_condensed = Parameters.Assembler.condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L;

            string template_matching = "";
            if (Parameters.TemplateDatabases != null && Parameters.TemplateDatabases.Count() > 0)
            {
                template_matching = $@"<div class=""template-matching"" style=""flex:{Parameters.Assembler.meta_data.template_matching_time}"">
    <p>Template Matching</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Template Matching</span>
        <span class=""runtime-time"">{Parameters.Assembler.meta_data.template_matching_time} ms</span>
        <span class=""runtime-desc"">Matching the contigs to the given templates</span>
    </div>
</div>";
            }

            string html = $@"
<h3>General information</h3>
<table>
<tr><td>Runname</td><td>{Parameters.Runname}</td></tr>
<tr><td>Assemblerversion</td><td>{ToRunWithCommandLine.VersionString}</td></tr>
<tr><td>K (length of k-mer)</td><td>{Parameters.Assembler.kmer_length}</td></tr>
<tr><td>Minimum homology</td><td>{Parameters.Assembler.Minimum_homology}</td></tr>
<tr><td>Duplicate Threshold</td><td>{Parameters.Assembler.duplicate_threshold}</td></tr>
<tr><td>Reverse</td><td>{Parameters.Assembler.reverse}</td></tr>
<tr><td>Alphabet</td><td>{Parameters.Assembler.alphabet}</td></tr>
<tr><td>Number of reads</td><td>{Parameters.Assembler.meta_data.reads}</td></tr>
<tr><td>Number of k-mers</td><td>{Parameters.Assembler.meta_data.kmers}</td></tr>
<tr><td>Number of (k-1)-mers</td><td>{Parameters.Assembler.meta_data.kmin1_mers}</td></tr>
<tr><td>Number of duplicate (k-1)-mers</td><td>{Parameters.Assembler.meta_data.kmin1_mers_raw - Parameters.Assembler.meta_data.kmin1_mers}</td></tr>
<tr><td>Number of contigs found</td><td>{Parameters.Assembler.meta_data.sequences}</td></tr>
<tr><td>Number of paths found</td><td>{Parameters.Paths.Count()}</td></tr>
</table>

<h3>de Bruijn Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{Parameters.Assembler.graph.Length}</td></tr>
<tr><td>Number of edges</td><td>{number_edges}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double)number_edges / Parameters.Assembler.graph.Length:F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{Parameters.Assembler.graph.Aggregate(0D, (a, b) => (a > b.EdgesCount()) ? (double)a : (double)b.EdgesCount()) / 2D}</td></tr>
</table>

<h3>Condensed Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{Parameters.Assembler.condensed_graph.Count()}</td></tr>
<tr><td>Number of edges</td><td>{number_edges_condensed}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double)number_edges_condensed / Parameters.Assembler.condensed_graph.Count():F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{Parameters.Assembler.condensed_graph.Aggregate(0D, (a, b) => (a > b.ForwardEdges.Count() + b.BackwardEdges.Count()) ? a : (double)b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2D}</td></tr>
<tr><td>Average sequence length</td><td>{Parameters.Assembler.condensed_graph.Aggregate(0D, (a, b) => (a + b.Sequence.Count())) / Parameters.Assembler.condensed_graph.Count():F3}</td></tr>
<tr><td>Total sequence length</td><td>{Parameters.Assembler.condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()))}</td></tr>
</table>

<h3>Runtime information</h3>
<p>Total computation time: {Parameters.Assembler.meta_data.total_time + Parameters.Assembler.meta_data.drawingtime + Parameters.Assembler.meta_data.template_matching_time} ms</p>
<div class=""runtime"">
<div class=""pre-work"" style=""flex:{Parameters.Assembler.meta_data.pre_time}"">
    <p>Pre</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Pre work</span>
        <span class=""runtime-time"">{Parameters.Assembler.meta_data.pre_time} ms</span>
        <span class=""runtime-desc"">Work done on generating k-mers and (k-1)-mers.</span>
    </div>
</div>
<div class=""linking-graph"" style=""flex:{Parameters.Assembler.meta_data.graph_time}"">
    <p>Linking</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Linking graph</span>
        <span class=""runtime-time"">{Parameters.Assembler.meta_data.graph_time} ms</span>
        <span class=""runtime-desc"">Work done to build the de Bruijn graph.</span>
    </div>
</div>
<div class=""finding-paths"" style=""flex:{Parameters.Assembler.meta_data.path_time}"">
    <p>Path</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Finding paths</span>
        <span class=""runtime-time"">{Parameters.Assembler.meta_data.path_time} ms</span>
        <span class=""runtime-desc"">Work done to find the paths through the graph.</span>
    </div>
</div>
<div class=""drawing"" style=""flex:{Parameters.Assembler.meta_data.drawingtime}"">
    <p>Drawing</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Drawing the graphs</span>
        <span class=""runtime-time"">{Parameters.Assembler.meta_data.drawingtime} ms</span>
        <span class=""runtime-desc"">Work done by graphviz (dot) to draw the graphs.</span>
    </div>
</div>
{template_matching}
<div class=""drawing"" style=""flex:REPORTGENERATETIME"">
    <p>Report</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Creating this report</span>
        <span class=""runtime-time"">REPORTGENERATETIME ms</span>
        <span class=""runtime-desc"">The time needed to create this report.</span>
    </div>
</div>
</div>";

            return html;
        }

        public override string Create()
        {
            throw new Exception("HTML reports should be generated using the 'Save' function.");
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
ContigPrefix = '{GetAsidePrefix(AsideType.Contig)}';
ReadPrefix = '{GetAsidePrefix(AsideType.Read)}';
TemplatePrefix = '{GetAsidePrefix(AsideType.Template)}';
PathPrefix = '{GetAsidePrefix(AsideType.Path)}';
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
                buffer.Append($"<pre><i>{bf.Filename}</i>");
                foreach (var line in bf.Lines) buffer.AppendLine(line);
                buffer.Append("</pre>");
                return buffer.ToString();
            }
            else
            {
                return "<em>No BatchFile</em>";
            }
        }

        private string CreateMain()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var innerbuffer = new StringBuilder();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);

            if (Parameters.TemplateDatabases != null)
                for (int group = 0; group < Parameters.TemplateDatabases.Count(); group++)
                {
                    var groupbuffer = "";

                    if (Parameters.ReadAlignment.Count() != 0)
                        groupbuffer += Collapsible("Read Alignment Table", CreateTemplateTable(Parameters.ReadAlignment[group].Templates, -1, group, AsideType.ReadAlignment, true), group.ToString());

                    if (Parameters.RecombinedDatabase.Count() != 0)
                        groupbuffer += Collapsible("Recombination Table", CreateTemplateTable(Parameters.RecombinedDatabase[group].Templates, -1, group, AsideType.RecombinedTemplate, true), group.ToString());

                    groupbuffer += CreateTemplateTables(Parameters.TemplateDatabases[group].Item2, group);

                    if (Parameters.TemplateDatabases.Count() == 1)
                        innerbuffer.Append(groupbuffer);
                    else
                        innerbuffer.Append(Collapsible(Parameters.TemplateDatabases[group].Item1, groupbuffer));
                }

            if (Parameters.Assembler != null)
            {
                Parameters.Assembler.meta_data.drawingtime = stopwatch.ElapsedMilliseconds;

                innerbuffer.Append(Collapsible("Graph", $"<img src='{AssetFolderName}/graph.svg' alt='The De Bruijn graph as resulting from the assembler.'>"));
                innerbuffer.Append(Collapsible("Simplified Graph", $"<img src='{AssetFolderName}/simplified-graph.svg' alt='The De Bruijn graph as resulting from the assembler. But now with the contig ids instead of the sequences.'>"));
                innerbuffer.Append(Collapsible("Paths Table", CreatePathsTable()));
                innerbuffer.Append(Collapsible("Contigs Table", CreateContigsTable()));
                innerbuffer.Append(Collapsible("Meta Information", MetaInformation()));
            }

            innerbuffer.Append(Collapsible("Reads Table", CreateReadsTable()));
            innerbuffer.Append(Collapsible("Batch File", BatchFileHTML()));

            var html = $@"<html>
{CreateHeader("Report Protein Sequence Run", new List<string>())}
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

 {innerbuffer}

<div class=""footer"">
    <p>Code written in 2019-2020</p>
    <p>Made by the Hecklab</p>
</div>

</div>
</div>
</body>";
            return html;
        }

        async void CopyAssets()
        {
            await Task.Run(() =>
            {
                var excutablefolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                void CopyAssetsFile(string name)
                {
                    var source = Path.Join(excutablefolder, "assets", name);
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
                    {
                        new InputNameSpace.ErrorMessage(source, "Could not find asset", "Please make sure the file exists. The HTML will be generated but may be less useful", "", true).Print();
                    }
                }

                CopyAssetsFile("styles.css");
                CopyAssetsFile("script.js");
                CopyAssetsFile("Roboto-Regular.ttf");
                CopyAssetsFile("Roboto-Medium.ttf");
                CopyAssetsFile("RobotoMono-Regular.ttf");
                CopyAssetsFile("RobotoMono-Medium.ttf");

                if (Parameters.Assembler != null)
                {
                    string svg, simplesvg;
                    (svg, simplesvg) = CreateGraph();
                    File.WriteAllText(Path.Join(FullAssetsFolderName, "graph.svg"), svg);
                    File.WriteAllText(Path.Join(FullAssetsFolderName, "simplified-graph.svg"), simplesvg);
                }
            });
        }

        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        public new void Save(string filename)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            FullAssetsFolderName = Path.Join(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            AssetsFolderName = Path.GetFileNameWithoutExtension(filename);

            Directory.CreateDirectory(FullAssetsFolderName);
            Directory.CreateDirectory(Path.Join(FullAssetsFolderName, "paths"));

            CopyAssets();

            var html = CreateMain();
            CreateAsides();

            stopwatch.Stop();
            html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds - (Parameters.Assembler != null ? Parameters.Assembler.meta_data.drawingtime : 0)}");
            SaveAndCreateDirectories(filename, html);
        }
    }
}