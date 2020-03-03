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
        /// To retrieve all metadata.
        /// </summary>
        /// <param name="assm">The assembler.</param>
        /// <param name="run">The runparameters.</param>
        /// <param name="useincludeddotdistribution">Indicates if the program should use the included Dot (graphviz) distribution.</param>
        public HTMLReport(ReportInputParameters parameters, bool useincludeddotdistribution) : base(parameters)
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
                if (condensed_graph[i].BackwardEdges.Count() == 0 || (condensed_graph[i].BackwardEdges.Count() == 1 && condensed_graph[i].BackwardEdges[0] == i))
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
                svggraph = Regex.Replace(svggraph, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>([A-Z][0-9]+)</title>", $"id=\"node-$2\" class=\"$1\" onclick=\"Select('$2')\">");
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
                throw new Exception($"Could not start Dot of the Graphviz software. Please make sure it is installed and added to your PATH if you run Graphviz globally. Or make sure you execute this program when the assets folder is accessible.");
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
    <th onclick=""sortTable('reads-table', 0, 'id')"" class=""smallcell"">Identifier</th>
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
                buffer.AppendLine(Collapsible($"Template Matching {singleRun.Template[i].Name}", CreateTemplateTable(i)));
            }

            return buffer.ToString();
        }

        string CreateTemplateTable(int templateIndex)
        {
            var buffer = new StringBuilder();

            buffer.AppendLine($@"<table id=""template-table-{templateIndex}"" class=""widetable"">
<tr>
    <th onclick=""sortTable('template-table-{templateIndex}', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('template-table-{templateIndex}', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('template-table-{templateIndex}', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('template-table-{templateIndex}', 3, 'number')"" class=""smallcell"">Score</th>
</tr>");
            string id, link;

            var sorted = templates[templateIndex].Templates;
            sorted.Sort((a, b) => b.Score.CompareTo(a.Score));

            for (int i = 0; i < sorted.Count(); i++)
            {
                id = GetAsideIdentifier(templateIndex, i, AsideType.Template);
                link = GetAsideLink(templateIndex, i, AsideType.Template);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(sorted[i].Sequence)}</td>
    <td class=""center"">{sorted[i].Sequence.Length}</td>
    <td class=""center"">{sorted[i].Score}</td>
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

        string CreateRecombinationTable()
        {
            var buffer = new StringBuilder();

            buffer.AppendLine($@"<table id=""recombination-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('recombination-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('recombination-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('recombination-table', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('recombination-table', 3, 'number')"" class=""smallcell"">Score</th>
</tr>");
            string id, link;

            var sorted = RecombinedDatabase.Templates;
            sorted.Sort((a, b) => b.Score.CompareTo(a.Score));

            for (int i = 0; i < sorted.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Recombination);
                link = GetAsideLink(i, AsideType.Recombination);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(sorted[i].Sequence)}</td>
    <td class=""center"">{sorted[i].Sequence.Length}</td>
    <td class=""center"">{sorted[i].Score}</td>
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
                var innerbuffer = new StringBuilder();

                innerbuffer.AppendLine($@"<table id=""recombination-table-{database.Name}"" class=""widetable"">
<tr>
    <th onclick=""sortTable('recombination-table-{database.Name}', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('recombination-table-{database.Name}', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('recombination-table-{database.Name}', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('recombination-table-{database.Name}', 3, 'number')"" class=""smallcell"">Score</th>
</tr>");
                string id, link;

                var sorted = database.Templates;
                sorted.Sort((a, b) => b.Score.CompareTo(a.Score));

                for (int i = 0; i < sorted.Count(); i++)
                {
                    id = GetAsideIdentifier(index, i, AsideType.RecombinationTemplate);
                    link = GetAsideLink(index, i, AsideType.RecombinationTemplate);
                    innerbuffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(sorted[i].Sequence)}</td>
    <td class=""center"">{sorted[i].Sequence.Length}</td>
    <td class=""center"">{sorted[i].Score}</td>
</tr>");
                }

                innerbuffer.AppendLine("</table>");

                buffer.AppendLine(Collapsible($"Recombination Database {database.Name}", innerbuffer.ToString()));
            }

            return buffer.ToString();
        }

        /// <summary> Returns an aside for details viewing of a contig. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateContigAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Contig);
            string prefix = "";
            if (condensed_graph[i].Prefix != null) prefix = AminoAcid.ArrayToString(condensed_graph[i].Prefix.ToArray());
            string suffix = "";
            if (condensed_graph[i].Suffix != null) suffix = AminoAcid.ArrayToString(condensed_graph[i].Suffix.ToArray());

            var readsalignment = CreateReadsAlignment(condensed_graph[i]);

            return $@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig {id}</h1>
    <h2>Sequence (length={condensed_graph[i].Sequence.Count()})</h2>
    <p class=""aside-seq""><span class='prefix'>{prefix}</span>{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}<span class='suffix'>{suffix}</span></p>
    <h2>Reads Alignment</h4>
    {readsalignment.Item1}
    <h2>Based on</h2>
    <p>{readsalignment.Item2.Aggregate("", (a, b) => a + " " + GetAsideLink(b, AsideType.Read))}</p>
</div>";
        }

        /// <summary> Returns an aside for details viewing of a read. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateReadAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Read);
            string meta = reads_metadata[i].ToHTML();
            return $@"<div id=""{id}"" class=""info-block read-info"">
    <h1>Read {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(reads[i])}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(reads[i]).Count()}</p>
    {meta}
</div>";
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateTemplateAside(int templateIndex, int i)
        {
            string id = GetAsideIdentifier(templateIndex, i, AsideType.Template);
            var template = templates[templateIndex].Templates[i];

            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{template.Sequence.Length}</p>
    <h2>Score</h2>
    <p>{template.Score}</p>
    {CreateTemplateAlignment(template, id)}
    {template.MetaData.ToHTML()}
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
            const int number = 10;
            var best_templates = new List<(int, int, int)>();
            int cutoff = 0;
            //Pick each database
            for (int k = 0; k < templates.Count(); k++)
            { // Pick each template
                for (int j = 0; j < templates[k].Templates.Count(); j++)
                {
                    var match = templates[k].Templates[j].Matches[i];
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
                sb.Append($"<tr><td>{tem.Item3}</td><td>{GetAsideLink(tem.Item1, tem.Item2, AsideType.Template)}</td></tr>");
            }

            var templateString = sb.ToString().Length == 0 ? "" : $"<h2>Top 10 templates</h2><table><tr><th>Score</th><th>Template</th></tr>{sb}</table>";

            sb.Clear();
            var maxCoverage = new List<int>();
            foreach (var node in Paths[i].Nodes)
            {
                var core = CreateReadsAlignmentCore(node);
                sb.Append(core.Item1);
                maxCoverage.Add(core.Item2);
            }

            return $@"<div id=""{id}"" class=""info-block read-info path-info"">
    <h1>Path {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(Paths[i].Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{Paths[i].Sequence.Length}</p>
    <h2>Path</h2>
    <p>{Paths[i].Nodes.Aggregate("", (a, b) => a + " → " + GetAsideLink(b.Index, AsideType.Contig)).Substring(3)}</p>
    <h2>Alignment</h2>
    <div class=""reads-alignment"" style=""--max-value:{maxCoverage.Max()}"">
    {sb}
    </div>
    {templateString}
</div>";
        }

        /// <summary> Returns an aside for details viewing of a recombination. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateRecombinationAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Recombination);
            var template = RecombinedDatabase.Templates[i];

            string order = "";
            if (template.Recombination != null)
            {
                order = $"<h2>Order</h2><p>{template.Recombination.Aggregate("", (a, b) => a + " → " + GetAsideLink(b.Location.TemplateDatabaseIndex, b.Location.TemplateIndex, AsideType.RecombinationTemplate)).Substring(3)}</p>";
            }

            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{template.Sequence.Length}</p>
    <h2>Score</h2>
    <p>{template.Score}</p>
    {order}
    {CreateTemplateAlignment(template, id)}
</div>";
        }

        /// <summary> Returns an aside for details viewing of a recombination database. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateRecombinationDatabaseAside(int index, int i)
        {
            string id = GetAsideIdentifier(index, i, AsideType.RecombinationTemplate);
            var template = RecombinationDatabases[index].Templates[i];

            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    <h2>Sequence Length</h2>
    <p>{template.Sequence.Length}</p>
    <h2>Out of database</h2>
    <p>{template.Name}</p>
    <h2>Score</h2>
    <p>{template.Score}</p>
    {CreateTemplateAlignment(template, id)}
    {template.MetaData.ToHTML()}
</div>";
        }

        string CreateTemplateAlignment(Template template, string id)
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

            var lines = new StringBuilder[template.Matches.Count() + 1];
            const char gapchar = '-';
            var depthOfCoverage = new List<int>();

            for (int i = 0; i < template.Matches.Count() + 1; i++)
            {
                lines[i] = new StringBuilder();
            }

            for (int template_pos = 0; template_pos < alignedSequences.Count(); template_pos++)
            {
                var (Sequences, Gaps) = alignedSequences[template_pos];
                lines[0].Append(template.Sequence[template_pos]);
                int depth = 0;

                // Add the aligned amino acid
                for (int i = 0; i < template.Matches.Count(); i++)
                {
                    int index = Sequences[i].SequencePosition;
                    depth += Sequences[i].CoverageDepth;

                    if (index == -1)
                    {
                        lines[i + 1].Append(gapchar);
                    }
                    else if (index == 0)
                    {
                        lines[i + 1].Append("\u00A0"); // Non breaking space
                    }
                    else
                    {
                        lines[i + 1].Append(template.Matches[i].QuerySequence[index - 1]);
                    }
                }

                depthOfCoverage.Add(depth);

                // Add the gap
                // TODO: Unaligned for now
                int max_length = 0;
                // Get the max length of the gaps 
                for (int i = 0; i < template.Matches.Count(); i++)
                {
                    if (Gaps[i].Gap != null && Gaps[i].Gap.ToString().Length > max_length)
                    {
                        max_length = Gaps[i].Gap.ToString().Length;
                    }
                }
                // Add gap to the template
                lines[0].Append(new string(gapchar, max_length));

                var depthGap = new List<int[]>();
                // Add gap to the lines
                for (int i = 0; i < template.Matches.Count(); i++)
                {
                    string seq;
                    if (Gaps[i].Gap == null)
                    {
                        seq = "";
                        depthGap.Add(Enumerable.Repeat(0, max_length).ToArray());
                    }
                    else
                    {
                        seq = Gaps[i].Gap.ToString();
                        var d = new int[max_length];
                        Gaps[i].CoverageDepth.CopyTo(d, max_length - Gaps[i].CoverageDepth.Length);
                        depthGap.Add(d);
                    }
                    lines[i + 1].Append(seq.PadRight(max_length, gapchar));
                }
                var depthGapCombined = new int[max_length];
                foreach (var d in depthGap)
                {
                    depthGapCombined = depthGapCombined.ElementwiseAdd(d);
                }
                depthOfCoverage.AddRange(depthGapCombined);
            }

            var aligned = new string[template.Matches.Count() + 1];

            for (int i = 0; i < template.Matches.Count() + 1; i++)
            {
                aligned[i] = lines[i].ToString();
            }

            buffer.AppendLine($"<div class=\"reads-alignment\" style=\"--max-value:{depthOfCoverage.Max()}\">");

            // Create the front overhanging reads block
            var frontoverhangbuffer = new StringBuilder();
            bool frontoverhang = false;
            frontoverhangbuffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            frontoverhangbuffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");
            for (int i = 1; i < aligned.Count(); i++)
            {
                string rid = GetAsideIdentifier(i - 1, AsideType.Path);
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition != 0 && match.StartTemplatePosition == 0)
                {
                    frontoverhang = true;
                    frontoverhangbuffer.Append($"<a href=\"#{rid}\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(0, match.StartQueryPosition))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    frontoverhangbuffer.Append($"<a href=\"#{rid}\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }
            frontoverhangbuffer.AppendLine($"</p></div></label></div>");
            if (frontoverhang) buffer.Append(frontoverhangbuffer.ToString());

            // Chop it up, add numbers etc
            const int blocklength = 5;

            if (aligned.Length > 0)
            {
                for (int block = 0; block <= aligned[0].Length / blocklength; block++)
                {
                    // Add the sequence and the number to tell the position
                    string number = "";
                    if (aligned[0].Length - block * blocklength >= blocklength)
                    {
                        number = ((block + 1) * blocklength).ToString();
                        number = string.Concat(Enumerable.Repeat("&nbsp;", blocklength - number.Length)) + number;
                    }
                    buffer.Append($"<div class='align-block'><p><span class=\"number\">{number}</span><br><span class=\"seq\">{aligned[0].Substring(block * blocklength, Math.Min(blocklength, aligned[0].Length - block * blocklength))}</span><br>");
                    for (int i = 1; i < aligned.Length; i++)
                    {
                        string rid = GetAsideIdentifier(template.Matches[i - 1].Path.Index, AsideType.Path);
                        string result = "";
                        if (aligned[i].Length > block * blocklength) result = $"<a href=\"#{rid}\" class=\"align-link\">{aligned[i].Substring(block * blocklength, Math.Min(blocklength, aligned[i].Length - block * blocklength))}</a>";
                        buffer.Append(result);
                        buffer.Append("<br>");
                    }
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
                string rid = GetAsideIdentifier(i - 1, AsideType.Path);
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition + match.TotalMatches < match.QuerySequence.Length && match.StartTemplatePosition + match.TotalMatches == match.TemplateSequence.Length)
                {
                    endoverhang = true;
                    endoverhangbuffer.Append($"<a href=\"#{rid}\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + match.TotalMatches, match.QuerySequence.Length - match.StartQueryPosition - match.TotalMatches))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    endoverhangbuffer.Append($"<a href=\"#{rid}\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }
            endoverhangbuffer.AppendLine($"</p></div></label></div>");
            if (endoverhang) buffer.Append(endoverhangbuffer.ToString());

            // Display Consensus Sequence
            /*var consensus = new StringBuilder();
            var consensus_sequence = template.CombinedSequence();

            for (int i = 0; i < consensus_sequence.Count(); i++)
            {
                // Get the highest chars
                string options = "";
                int max = 0;
                foreach (var item in consensus_sequence[i].AminoAcids)
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
                foreach (var item in consensus_sequence[i].Gaps)
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
                if (max_gap.Count() > 1)
                {
                    consensus.Append("(");
                    foreach (var item in max_gap)
                    {
                        consensus.Append(item.ToString());
                        consensus.Append("/");
                    }
                    consensus.Append(")");
                }
                else if (max_gap.Count() == 1)
                {
                    consensus.Append(max_gap[0].ToString());
                }
                else
                {
                    consensus.Append("_");
                }
            }*/
            var consensus_sequence = template.CombinedSequence();
            buffer.AppendLine("</div><h2>Consensus Sequence</h2><p style='word-break: break-all;'>");
            buffer.AppendLine(HelperFunctionality.ConsensusSequence(template));
            buffer.AppendLine("</p>");

            // Sequence logo
            const double threshold = 0.1;
            const int height = 50;
            const int fontsize = 20;

            buffer.Append($"<div class='sequence-logo' style='--sequence-logo-height:{height}px;--sequence-logo-fontsize:{fontsize}px;'>");
            for (int i = 0; i < consensus_sequence.Count(); i++)
            {
                buffer.Append("<div class='sequence-logo-position'>");
                // Get the highest chars
                int sum = 0;
                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    sum += item.Value;
                }

                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    if ((double)item.Value / sum > threshold)
                    {
                        buffer.Append($"<span style='transform: scaleY({(double)item.Value / sum * height / fontsize})'>{item.Key}</span>");
                    }
                }
                buffer.Append("</div>");
                /*// Get the highest gap
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
                }*/
            }
            buffer.Append("</div>");

            // Display matches table
            buffer.Append("<h2>Matches Table</h2><table><tr><th>ID</th><th>Score</th><th>Match Length</th></tr>");
            foreach (var match in template.Matches)
            {
                buffer.AppendLine($"<tr><td>{GetAsideLink(match.Path.Index, AsideType.Path)}</td><td>{match.Score}</td><td>{match.TotalMatches}</td></tr>");
            }
            buffer.Append("</table>");

            return buffer.ToString();
        }

        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateAsides()
        {
            var buffer = new StringBuilder();

            // Path Asides
            for (int i = 0; i < Paths.Count(); i++)
            {
                buffer.AppendLine(CreatePathAside(i));
            }
            // Contigs Asides
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                buffer.AppendLine(CreateContigAside(i));
            }
            // Read Asides
            for (int i = 0; i < reads.Count(); i++)
            {
                buffer.AppendLine(CreateReadAside(i));
            }
            // Template Tables Asides
            for (int t = 0; t < templates.Count(); t++)
            {
                for (int i = 0; i < templates[t].Templates.Count(); i++)
                {
                    buffer.AppendLine(CreateTemplateAside(t, i));
                }
            }
            // Recombination Table Asides
            if (RecombinedDatabase != null)
            {
                for (int i = 0; i < RecombinedDatabase.Templates.Count(); i++)
                {
                    buffer.AppendLine(CreateRecombinationAside(i));
                }

                // Recombination Databases Tables Asides
                for (int t = 0; t < RecombinationDatabases.Count(); t++)
                {
                    for (int i = 0; i < RecombinationDatabases[t].Templates.Count(); i++)
                    {
                        buffer.AppendLine(CreateRecombinationDatabaseAside(t, i));
                    }
                }
            }

            return buffer.ToString();
        }

        /// <summary> Create a reads alignment to display in the sidebar. </summary>
        /// <returns> Returns an HTML string. </returns>
        (string, List<int>) CreateReadsAlignment(CondensedNode node)
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
            var core = CreateReadsAlignmentCore(node);
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
                        result = $"<a href=\"#{rid}\" class='text align-link'>{read.EndOverhang}</a><span class='symbol'>...</span><br>";
                    }
                }
                buffer.Append(result);
            }
            buffer.AppendLine($"</p></div></label></div>");

            // End the reads alignment div
            buffer.AppendLine("</div>");

            return (buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{core.Item2}'>"), node.UniqueOrigins);
        }
        (string, int) CreateReadsAlignmentCore(CondensedNode node)
        {
            var placed = node.Alignment;
            var depthOfCoverage = node.DepthOfCoverageFull;
            var sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            int prefixoffset = node.Prefix.Count();

            var buffer = new StringBuilder();

            const int bucketsize = 5;

            // Create the main blocks of the sequence alignment
            for (int pos = 0; pos <= sequence.Length / bucketsize; pos++)
            {
                // Add the sequence and the number to tell the position
                string number = "";
                string last = "";
                if (sequence.Length - pos * bucketsize >= bucketsize)
                {
                    number = ((pos + 1) * bucketsize).ToString();
                    number = string.Concat(Enumerable.Repeat("&nbsp;", bucketsize - number.Length)) + number;
                }
                else
                {
                    last = " last";
                }
                buffer.Append($"<div class='align-block{last}'><p><span class=\"number\">{number}</span><br><span class=\"seq{last}\">{sequence.Substring(pos * bucketsize, Math.Min(bucketsize, sequence.Length - pos * bucketsize))}</span><br>");

                // Add every line in order
                foreach (var line in placed)
                {
                    for (int i = pos * bucketsize; i < pos * bucketsize + Math.Min(bucketsize, sequence.Length - pos * bucketsize); i++)
                    {
                        string result = "&nbsp;";
                        foreach (var read in line)
                        {
                            if (i + prefixoffset >= read.StartPosition && i + prefixoffset < read.EndPosition)
                            {
                                string rid = GetAsideIdentifier(read.Identifier, AsideType.Read);
                                result = $"<a href=\"#{rid}\" class=\"align-link\">{read.Sequence[i - read.StartPosition + prefixoffset]}</a>";
                            }
                        }
                        buffer.Append(result);
                    }
                    buffer.Append("<br>");
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = pos * bucketsize; i < pos * bucketsize + Math.Min(bucketsize, sequence.Length - pos * bucketsize); i++)
                {
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }

            return (buffer.ToString(), depthOfCoverage.Max());
        }

        /// <summary>An enum to save what type of detail aside it is.</summary>
        enum AsideType { Contig, Read, Template, Path, Recombination, RecombinationTemplate }
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
                case AsideType.Recombination:
                    return "RC";
                case AsideType.RecombinationTemplate:
                    return "RT";
            }
            throw new Exception("Invalid AsideType in GetAsidePrefix.");
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container.</summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index, AsideType type)
        {
            return GetAsideIdentifier(-1, index, type);
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index1, int index2, AsideType type)
        {
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
        string GetAsideLink(int index, AsideType type)
        {
            return GetAsideLink(-1, index, type);
        }

        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index1">The index in the supercontainer of the container. A value of -1 removes the index in the supercontainer.</param>
        /// <param name="index2">The index in the container of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns> A valid HTML link.</returns>
        string GetAsideLink(int index1, int index2, AsideType type)
        {
            string id = GetAsideIdentifier(index1, index2, type);
            string classname = "";
            if (type == AsideType.Contig) classname = "contig";
            if (type == AsideType.Read) classname = "read";
            if (type == AsideType.Template) classname = "template";
            if (type == AsideType.Path) classname = "path";
            if (type == AsideType.Recombination) classname = "recombination";
            if (type == AsideType.RecombinationTemplate) classname = "recombination-template";
            return $"<a href=\"#{id}\" class=\"info-link {classname}-link\">{id}</a>";
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

        /// <summary> Returns some meta information about the assembly the help validate the output of the assembly. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string MetaInformation()
        {
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
<tr><td>Alphabet</td><td>{singleRun.Alphabet.Alphabet}</td></tr>
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

        /// <summary> Creates an HTML report to view the results and metadata. </summary>
        public override string Create()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string svg, simplesvg;
            (svg, simplesvg) = CreateGraph();

            string stylesheet = "/* Could not find the stylesheet */";
            if (File.Exists("assets/styles.css")) stylesheet = File.ReadAllText("assets/styles.css");
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Could not find the styles.css file. Please make sure the 'assets' folder is accessible. The HTML report is generated but the looks are not great.");
                Console.ResetColor();
            }

            string script = "// Could not find the script";
            if (File.Exists("assets/script.js")) script = File.ReadAllText("assets/script.js");
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Could not find the script.js file. Please make sure the 'assets' folder is accessible. The HTML report is generated but is not very interactive.");
                Console.ResetColor();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            stopwatch.Stop();
            meta_data.drawingtime = stopwatch.ElapsedMilliseconds;

            string recombinationtable = "";
            if (RecombinedDatabase != null)
            {
                recombinationtable = Collapsible("Recombination Table", CreateRecombinationTable());
                recombinationtable += CreateRecombinationDatabaseTables();
            }

            string html = $@"<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Report Protein Sequence Run</title>
<style>
{stylesheet}
</style>
<script>
ContigPrefix = '{GetAsidePrefix(AsideType.Contig)}';
ReadPrefix = '{GetAsidePrefix(AsideType.Read)}';
TemplatePrefix = '{GetAsidePrefix(AsideType.Template)}';
PathPrefix = '{GetAsidePrefix(AsideType.Path)}';
{script}
</script>
</head>
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>
<div class=""js-settings"">
    <p title=""Could help make the report feel more snappy, especially with not so powerfull devices."">Hover effects</p>
    <label class=""js-toggle"">
        <input type=""checkbox"" onchange=""toggleHover()"" checked>
        <span class=""slider"">
    </label>
</div>

 {recombinationtable}
 {CreateTemplateTables()}
 {Collapsible("Graph", svg)}
 {Collapsible("Simplified Graph", simplesvg)}
 {Collapsible("Paths Table", CreatePathsTable())}
 {Collapsible("Contigs Table", CreateContigsTable())}
 {Collapsible("Reads Table", CreateReadsTable())}
 {Collapsible("Meta Information", MetaInformation())}

<div class=""footer"">
    <p>Code written in 2019-2020</p>
    <p>Made by the Hecklab</p>
</div>

</div>
<div class=""aside-handle"" id=""aside-handle"">
<span class=""handle"">&lt;&gt;</span>
</div>
<div class=""aside"" id=""aside"">
<div class=""aside-wrapper"">
{CreateAsides()}
</div>
</div>
</body>";
            return html;
        }
    }
}