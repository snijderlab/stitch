using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;

namespace AssemblyNameSpace
{
    /// <summary>
    /// An HTML report.
    /// </summary>
    class HTMLReport : Report
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

        public HTMLReport(ReportInputParameters parameters, bool useincludeddotdistribution, int max_threads) : base(parameters, max_threads)
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

            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                //Test if it is a starting node
                if (condensed_graph[i].BackwardEdges.Count() == 0 || (condensed_graph[i].BackwardEdges.Count() == 1 && condensed_graph[i].BackwardEdges.ToArray()[0] == i))
                {
                    style = ", style=filled, fillcolor=\"blue\", fontcolor=\"white\"";
                }
                else style = "";

                string id = GetAsideIdentifier(i, AsideType.Contig);

                buffer.AppendLine($"\t{id} [label=\"{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}\"{style}]");

                simplebuffer.AppendLine($"\t{id} [label=\"{id}\"{style}]");

                foreach (var fwe in condensed_graph[i].ForwardEdges)
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
                for (int i = 0; i < condensed_graph.Count(); i++)
                {
                    string extra_classes;
                    try
                    {
                        extra_classes = AllPathsContaining(condensed_graph[i].Index).Aggregate("", (a, b) => a + " " + GetAsideIdentifier(b.Index, AsideType.Path)).Substring(1);
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
                throw new Exception("");
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
    <td class=""seq"">{AminoAcid.ArrayToString(reads[i])}</td>
    <td class=""center"">{AminoAcid.ArrayToString(reads[i]).Count()}</td>
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

            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Contig);
                link = GetAsideLink(i, AsideType.Contig);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}</td>
    <td class=""center"">{condensed_graph[i].Sequence.Count()}</td>
    <td class=""center"">{condensed_graph[i].ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Contig))}</td>
    <td class=""center"">{condensed_graph[i].BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Contig))}</td>
    <td>{condensed_graph[i].UniqueOrigins.Aggregate<int, string>("", (a, b) => a + " " + GetAsideLink(b, AsideType.Read))}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        string CreateTemplateTables()
        {
            var buffer = new StringBuilder();

            for (int i = 0; i < singleRun.Template.Count(); i++)
            {
                buffer.AppendLine(Collapsible($"Template Matching {singleRun.Template[i].Name}", CreateTemplateTable(databases[i].Templates, i, AsideType.Template)));
            }

            return buffer.ToString();
        }

        string CreateTemplateTable(List<Template> templates, int templateIndex, AsideType type, bool header = false)
        {
            var buffer = new StringBuilder();

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            if (header) buffer.Append(TableHeader(templates));

            buffer.AppendLine($@"<table id=""template-table-{type}-{templateIndex}"" class=""widetable"">
<tr>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 3, 'number')"" class=""smallcell"">Score</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 4, 'number')"" class=""smallcell"">Reads</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}', 5, 'number')"" class=""smallcell"">Total Area</th>
</tr>");

            string id, link;
            for (int i = 0; i < templates.Count(); i++)
            {
                id = GetAsideIdentifier(templateIndex, i, type);
                link = GetAsideLink(templateIndex, i, type);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{templates[i].ConsensusSequence()}</td>
    <td class=""center"">{templates[i].Sequence.Length}</td>
    <td class=""center"">{templates[i].Score}</td>
    <td class=""center"">{templates[i].Matches.Count()}</td>
    <td class=""center"">{templates[i].TotalArea.ToString("G3", new CultureInfo("en-GB"))}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        string CreatePathsTable()
        {
            var buffer = new StringBuilder();

            buffer.AppendLine(@"<table id=""paths-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('paths-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('paths-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('paths-table', 2, 'number')"" class=""smallcell"">Length</th>
</tr>");
            string id, link;

            for (int i = 0; i < Paths.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Path);
                link = GetAsideLink(i, AsideType.Path);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(Paths[i].Sequence)}</td>
    <td class=""center"">{Paths[i].Sequence.Length}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        /// <summary>
        /// Creates tables for all databases used in recombination.
        /// </summary>
        string CreateRecombinationDatabaseTables()
        {
            var buffer = new StringBuilder();

            int index = -1;
            foreach (var database in RecombinationDatabases)
            {
                index++;
                buffer.AppendLine(Collapsible($"Recombination Database {database.Name}", CreateTemplateTable(database.Templates, index, AsideType.RecombinationDatabase, true)));
            }

            return buffer.ToString();
        }

        /// <summary> Returns an aside for details viewing of a contig. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateContigAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Contig);
            var location = new List<string>() { AssetsFolderName, GetAsideName(AsideType.Contig) + "s" };
            string prefix = "";
            if (condensed_graph[i].Prefix != null) prefix = AminoAcid.ArrayToString(condensed_graph[i].Prefix.ToArray());
            string suffix = "";
            if (condensed_graph[i].Suffix != null) suffix = AminoAcid.ArrayToString(condensed_graph[i].Suffix.ToArray());

            var readsalignment = CreateReadsAlignment(condensed_graph[i], location);

            return $@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig {GetAsideIdentifier(i, AsideType.Contig, true)}</h1>
    <h2>Sequence (length={condensed_graph[i].Sequence.Count()})</h2>
    <p class=""aside-seq""><span class='prefix'>{prefix}</span>{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}<span class='suffix'>{suffix}</span></p>
    <h2>Reads Alignment</h4>
    {readsalignment.Item1}
    <h2>Based on</h2>
    <p>{condensed_graph[i].UniqueOrigins.Aggregate("", (a, b) => a + " " + GetAsideLink(b, AsideType.Read, location))}</p>
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
    <p class=""aside-seq"">{AminoAcid.ArrayToString(reads[i])}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(reads[i]).Count()}</p>
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
            const int number = 10;
            var best_templates = new List<(int, int, int)>();
            int cutoff = 0;
            //Pick each database
            for (int k = 0; k < databases.Count(); k++)
            { // Pick each template
                for (int j = 0; j < databases[k].Templates.Count(); j++)
                {
                    var match = databases[k].Templates[j].Matches[i];
                    if (match.Score > cutoff || best_templates.Count() < number)
                    {
                        best_templates.Add((k, j, match.Score));
                        best_templates.Sort((a, b) => b.Item3.CompareTo(a.Item3));

                        int count = best_templates.Count();
                        if (count > number) best_templates.RemoveRange(10, count - number);

                        cutoff = best_templates.Last().Item3;
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var tem in best_templates)
            {
                sb.Append($"<tr><td>{tem.Item3}</td><td>{GetAsideLink(tem.Item1, tem.Item2, AsideType.Template, location)}</td></tr>");
            }

            var templateString = sb.ToString().Length == 0 ? "" : $"<h2>Top 10 templates</h2><table><tr><th>Score</th><th>Template</th></tr>{sb}</table>";

            sb.Clear();
            var maxCoverage = new List<int>();
            foreach (var node in Paths[i].Nodes)
            {
                var core = CreateReadsAlignmentCore(node, location);
                sb.Append(core.Item1);
                maxCoverage.Add(core.Item2);
            }

            return $@"<div id=""{id}"" class=""info-block read-info path-info"">
    <h1>Path {GetAsideIdentifier(i, AsideType.Path, true)}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(Paths[i].Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{Paths[i].Sequence.Length}</p>
    <h2>Path</h2>
    <p>{Paths[i].Nodes.Aggregate("", (a, b) => a + " → " + GetAsideLink(b.Index, AsideType.Contig, location)).Substring(3)}</p>
    <h2>Alignment</h2>
    <div class=""reads-alignment"" style=""--max-value:{maxCoverage.Max()}"">
    {sb}
    </div>
    {templateString}
</div>";
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateTemplateAside(AsideType type, Template template, int index, int i)
        {
            string id = GetAsideIdentifier(index, i, type);
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };
            var alignment = CreateTemplateAlignment(template, id, location);

            string meta = "";
            if (template.MetaData != null && (type == AsideType.RecombinedTemplate || type == AsideType.Template))
            {
                meta = template.MetaData.ToHTML();
            }

            string based = "";
            switch (type)
            {
                case AsideType.ReadAlignment:
                    based = $"<h2>Based on</h2><p>{GetAsideLink(i, AsideType.RecombinedTemplate, location)}</p>";
                    break;
                case AsideType.RecombinationDatabase:
                    based = $"<h2>Out of database</h2><p>{template.Name}</p>";
                    break;
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null)
                        based = $"<h2>Order</h2><p>{template.Recombination.Aggregate("", (a, b) => a + " → " + GetAsideLink(b.Location.TemplateDatabaseIndex, b.Location.TemplateIndex, AsideType.RecombinationDatabase, location)).Substring(3)}</p>";
                    break;
                default:
                    break;
            }

            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {GetAsideIdentifier(index, i, type, true)}</h1>
    {alignment.ConsensusSequence}
    {alignment.SequenceLogo}
    <h2>Sequence Length</h2>
    <p>{template.Sequence.Length}</p>
    <h2>Total Area</h2>
    <p>{template.TotalArea}</p>
    <h2>Score</h2>
    <p>{template.Score}</p>
    {based}
    {alignment.Alignment}
    <h2>Template Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    {meta}
</div>";
        }

        (string Alignment, string ConsensusSequence, string SequenceLogo) CreateTemplateAlignment(Template template, string id, List<string> location)
        {
            var buffer = new StringBuilder();
            var alignedSequences = template.AlignedSequences();

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
            int read_offset = singleRun.Input.Count();

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
                // TODO: Unaligned for now
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
                            try
                            {
                                var meta = template.Matches[indices[i]].MetaData;
                                rid = meta.EscapedIdentifier;
                                if (meta.GetType() == typeof(MetaData.Path)) name = GetAsideName(AsideType.Path);
                            }
                            catch { }
                            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, name + "s" }, location) + rid.Replace(':', '-') + ".html?pos=" + positions[i];
                            if (aligned[i].Length > block * blocklength) result = $"<a href=\"{path}\" class=\"align-link\">{aligned[i].Substring(block * blocklength, Math.Min(blocklength, aligned[i].Length - block * blocklength))}</a>";
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
            buffer.AppendLine("</div>");

            var cons_string = $"<h2>Consensus Sequence</h2><p class='aside-seq'>{template.ConsensusSequence()}</p>";

            // Sequence logo
            const double threshold = 0.3;
            const int height = 50;
            const int fontsize = 20;
            var consensus_sequence = template.CombinedSequence();

            var sequence_logo_buffer = new StringBuilder();

            sequence_logo_buffer.Append($"<h2>Sequence Logo</h2><div class='sequence-logo' style='--sequence-logo-height:{height}px;--sequence-logo-fontsize:{fontsize}px;'>");
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

            // Display matches table
            buffer.Append("<h2>Matches Table</h2><table><tr><th>ID</th><th>Score</th><th>Match Length</th></tr>");
            foreach (var match in template.Matches)
            {
                buffer.AppendLine($"<tr><td>{GetAsideLink(match.Index, AsideType.Path, location)}</td><td>{match.Score}</td><td>{match.TotalMatches}</td></tr>");
            }
            buffer.Append("</table>");

            return (buffer.ToString(), cons_string, sequence_logo_buffer.ToString());
        }

        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        void CreateAsides()
        {
            var jobbuffer = new List<(AsideType, int, int)>();

            void ExecuteJob(AsideType aside, int index2, int index1)
            {
                switch (aside)
                {
                    case AsideType.Path: SaveAside(CreatePathAside(index1), AsideType.Path, -1, index1); break;
                    case AsideType.Contig: SaveAside(CreateContigAside(index1), AsideType.Contig, -1, index1); break;
                    case AsideType.Read: SaveAside(CreateReadAside(index1), AsideType.Read, -1, index1); break;
                    case AsideType.Template: SaveTemplateAside(databases[index2].Templates[index1], AsideType.Template, index2, index1); break;
                    case AsideType.RecombinedTemplate: SaveTemplateAside(RecombinedDatabase.Templates[index1], AsideType.RecombinedTemplate, -1, index1); break;
                    case AsideType.RecombinationDatabase: SaveTemplateAside(RecombinationDatabases[index2].Templates[index1], AsideType.RecombinationDatabase, index2, index1); break;
                    case AsideType.ReadAlignment: SaveTemplateAside(ReadAlignment.Templates[index1], AsideType.ReadAlignment, -1, index1); break;
                };
            }

            // Path Asides
            for (int i = 0; i < Paths.Count(); i++)
            {
                jobbuffer.Add((AsideType.Path, -1, i));
            }
            // Contigs Asides
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                jobbuffer.Add((AsideType.Contig, -1, i));
            }
            // Read Asides
            for (int i = 0; i < reads.Count(); i++)
            {
                jobbuffer.Add((AsideType.Read, -1, i));
            }
            // Template Tables Asides
            for (int t = 0; t < databases.Count(); t++)
            {
                for (int i = 0; i < databases[t].Templates.Count(); i++)
                {
                    jobbuffer.Add((AsideType.Template, t, i));
                }
            }
            // Recombination Table Asides
            if (RecombinedDatabase != null)
            {
                for (int i = 0; i < RecombinedDatabase.Templates.Count(); i++)
                {
                    jobbuffer.Add((AsideType.RecombinedTemplate, -1, i));
                }

                // Recombination Databases Tables Asides
                for (int t = 0; t < RecombinationDatabases.Count(); t++)
                {
                    for (int i = 0; i < RecombinationDatabases[t].Templates.Count(); i++)
                    {
                        jobbuffer.Add((AsideType.RecombinationDatabase, t, i));
                    }
                }
            }
            // Reads Alignment Table Asides
            if (ReadAlignment != null)
            {
                for (int i = 0; i < ReadAlignment.Templates.Count(); i++)
                {
                    jobbuffer.Add((AsideType.ReadAlignment, -1, i));
                }
            }
            if (MaxThreads > 1)
            {
                Parallel.ForEach(
                    jobbuffer,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (a, _) => ExecuteJob(a.Item1, a.Item2, a.Item3)
                );
            }
            else
            {
                foreach (var (t, i2, i1) in jobbuffer)
                {
                    ExecuteJob(t, i2, i1);
                }
            }
        }

        void SaveTemplateAside(Template template, AsideType type, int index1, int index2)
        {
            SaveAside(CreateTemplateAside(type, template, index1, index2), type, index1, index2);
        }

        void SaveAside(string content, AsideType type, int index1, int index2)
        {
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };
            var homelocation = GetLinkToFolder(new List<string>(), location) + AssetsFolderName + ".html";
            var id = GetAsideIdentifier(index1, index2, type);
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
        (string, int) CreateReadsAlignmentCore(CondensedNode node, List<string> location)
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
        enum AsideType { Contig, Read, Template, Path, RecombinedTemplate, RecombinationDatabase, ReadAlignment }
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
                case AsideType.RecombinationDatabase:
                    return "RT";
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
                case AsideType.RecombinationDatabase:
                    return "recombination-database";
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
            return GetAsideIdentifier(-1, index, type, humanvisible);
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index1, int index2, AsideType type, bool humanvisible = false)
        {
            // Try to use the identifiers as retrieved from the metadata
            MetaData.IMetaData metadata = null;
            if (type == AsideType.Read)
            {
                metadata = reads_metadata[index2];
            }
            else if (type == AsideType.Template)
            {
                metadata = databases[index1].Templates[index2].MetaData;
            }
            else if (type == AsideType.RecombinationDatabase)
            {
                metadata = RecombinationDatabases[index1].Templates[index2].MetaData;
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
            if (index2 < 9999) i2 = $"{index2:D4}";
            else i2 = $"{index2}";

            return $"{pre}{i1}{i2}";
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A valid HTML link.</returns>
        string GetAsideLink(int index, AsideType type, List<string> location = null)
        {
            return GetAsideLink(-1, index, type, location);
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns> A valid HTML link.</returns>
        string GetAsideLink(int index1, int index2, AsideType type, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(index1, index2, type);
            if (id == null) throw new Exception("ID is null");
            string classname = GetAsideName(type);
            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, classname + "s" }, location) + id.Replace(':', '-') + ".html";
            return $"<a href=\"{path}\" class=\"info-link {classname}-link\">{ GetAsideIdentifier(index1, index2, type, true)}</a>";
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
        string Collapsible(string name, string content)
        {
            string id = name.ToLower().Replace(" ", "-") + "-collapsible";
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

            if (templates[0].Parent.ClassChars > 0)
            {
                var typedata = new Dictionary<string, (double, double, int, double)>(templates.Count);
                foreach (var item in templates)
                {
                    if (typedata.ContainsKey(item.Class))
                    {
                        var data = typedata[item.Class];
                        if (data.Item1 < item.Score) data.Item1 = item.Score;
                        data.Item2 += item.Score;
                        data.Item3 += 1;
                        data.Item4 += item.TotalArea;
                        typedata[item.Class] = data;
                    }
                    else
                    {
                        typedata.Add(item.Class, (item.Score, item.Score, 1, item.TotalArea));
                    }
                }

                var totalData = new List<(string, List<double>)>(typedata.Count);
                foreach (var (type, data) in typedata)
                {
                    totalData.Add((type, new List<double> { data.Item1, data.Item2 / data.Item3, data.Item3, data.Item4 }));
                }
                classname = " full";
                extended = $@"
<div>
    <h3>Per Type</h3>
    {HTMLGraph.GroupedBargraph(totalData, new List<(string, uint)> { ("Max Score", 0), ("Average Score", 0), ("Number of Reads", 1), ("Total Area", 2) })}
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

        /// <summary> Returns some meta information about the assembly the help validate the output of the assembly. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string MetaInformation()
        {
            //TODO needs time for recombined template matching
            long number_edges = graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
            long number_edges_condensed = condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L;

            string template_matching = "";
            if (singleRun.Template.Count() > 0)
            {
                template_matching = $@"<div class=""template-matching"" style=""flex:{meta_data.template_matching_time}"">
    <p>Template Matching</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Template Matching</span>
        <span class=""runtime-time"">{meta_data.template_matching_time} ms</span>
        <span class=""runtime-desc"">Matching the contigs to the given templates</span>
    </div>
</div>";
            }

            string html = $@"
<h3>General information</h3>
<table>
<tr><td>Runname</td><td>{singleRun.Runname}</td></tr>
<tr><td>Assemblerversion</td><td>{ToRunWithCommandLine.VersionString}</td></tr>
<tr><td>K (length of k-mer)</td><td>{singleRun.K}</td></tr>
<tr><td>Minimum homology</td><td>{singleRun.MinimalHomology}</td></tr>
<tr><td>Duplicate Threshold</td><td>{singleRun.DuplicateThreshold}</td></tr>
<tr><td>Reverse</td><td>{singleRun.Reverse}</td></tr>
<tr><td>Alphabet</td><td>{singleRun.Alphabet.Name}</td></tr>
<tr><td>Number of reads</td><td>{meta_data.reads}</td></tr>
<tr><td>Number of k-mers</td><td>{meta_data.kmers}</td></tr>
<tr><td>Number of (k-1)-mers</td><td>{meta_data.kmin1_mers}</td></tr>
<tr><td>Number of duplicate (k-1)-mers</td><td>{meta_data.kmin1_mers_raw - meta_data.kmin1_mers}</td></tr>
<tr><td>Number of contigs found</td><td>{meta_data.sequences}</td></tr>
<tr><td>Number of paths found</td><td>{Paths.Count()}</td></tr>
</table>

<h3>de Bruijn Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{graph.Length}</td></tr>
<tr><td>Number of edges</td><td>{number_edges}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double)number_edges / graph.Length:F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{graph.Aggregate(0D, (a, b) => (a > b.EdgesCount()) ? (double)a : (double)b.EdgesCount()) / 2D}</td></tr>
</table>

<h3>Condensed Graph information</h3>
<table>
<tr><td>Number of nodes</td><td>{condensed_graph.Count()}</td></tr>
<tr><td>Number of edges</td><td>{number_edges_condensed}</td></tr>
<tr><td>Mean Connectivity</td><td>{(double)number_edges_condensed / condensed_graph.Count():F3}</td></tr>
<tr><td>Highest Connectivity</td><td>{condensed_graph.Aggregate(0D, (a, b) => (a > b.ForwardEdges.Count() + b.BackwardEdges.Count()) ? a : (double)b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2D}</td></tr>
<tr><td>Average sequence length</td><td>{condensed_graph.Aggregate(0D, (a, b) => (a + b.Sequence.Count())) / condensed_graph.Count():F3}</td></tr>
<tr><td>Total sequence length</td><td>{condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()))}</td></tr>
</table>

<h3>Runtime information</h3>
<p>Total computation time: {meta_data.total_time + meta_data.drawingtime + meta_data.template_matching_time} ms</p>
<div class=""runtime"">
<div class=""pre-work"" style=""flex:{meta_data.pre_time}"">
    <p>Pre</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Pre work</span>
        <span class=""runtime-time"">{meta_data.pre_time} ms</span>
        <span class=""runtime-desc"">Work done on generating k-mers and (k-1)-mers.</span>
    </div>
</div>
<div class=""linking-graph"" style=""flex:{meta_data.graph_time}"">
    <p>Linking</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Linking graph</span>
        <span class=""runtime-time"">{meta_data.graph_time} ms</span>
        <span class=""runtime-desc"">Work done to build the de Bruijn graph.</span>
    </div>
</div>
<div class=""finding-paths"" style=""flex:{meta_data.path_time}"">
    <p>Path</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Finding paths</span>
        <span class=""runtime-time"">{meta_data.path_time} ms</span>
        <span class=""runtime-desc"">Work done to find the paths through the graph.</span>
    </div>
</div>
<div class=""drawing"" style=""flex:{meta_data.drawingtime}"">
    <p>Drawing</p>
    <div class=""runtime-hover"">
        <span class=""runtime-title"">Drawing the graphs</span>
        <span class=""runtime-time"">{meta_data.drawingtime} ms</span>
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

        private string CreateMain()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            meta_data.drawingtime = stopwatch.ElapsedMilliseconds;

            string recombinationtable = "";
            if (RecombinedDatabase != null)
            {
                recombinationtable = Collapsible("Recombination Table", CreateTemplateTable(RecombinedDatabase.Templates, -1, AsideType.RecombinedTemplate, true));
                recombinationtable += CreateRecombinationDatabaseTables();
            }

            string readalignmenttable = "";
            if (ReadAlignment != null)
            {
                readalignmenttable = Collapsible("Read Alignment Table", CreateTemplateTable(ReadAlignment.Templates, -1, AsideType.ReadAlignment, true));
            }

            var AssetFolderName = Path.GetFileName(FullAssetsFolderName);

            var html = $@"<html>
{CreateHeader("Report Protein Sequence Run", new List<string>())}
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

 {readalignmenttable}
 {recombinationtable}
 {CreateTemplateTables()}
 {Collapsible("Graph", $"<img src='{AssetFolderName}/graph.svg' alt='The De Bruijn graph as resulting from the assembler.'>")}
 {Collapsible("Simplified Graph", $"<img src='{AssetFolderName}/simplified-graph.svg' alt='The De Bruijn graph as resulting from the assembler. But now with the contig ids instead of the sequences.'>")}
 {Collapsible("Paths Table", CreatePathsTable())}
 {Collapsible("Contigs Table", CreateContigsTable())}
 {Collapsible("Reads Table", CreateReadsTable())}
 {Collapsible("Meta Information", MetaInformation())}

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

                string svg, simplesvg;
                (svg, simplesvg) = CreateGraph();
                File.WriteAllText(Path.Join(FullAssetsFolderName, "graph.svg"), svg);
                File.WriteAllText(Path.Join(FullAssetsFolderName, "simplified-graph.svg"), simplesvg);
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
            html = html.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds - meta_data.drawingtime}");
            SaveAndCreateDirectories(filename, html);
        }
    }
}