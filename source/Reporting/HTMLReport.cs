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
    /// An HTML report
    /// </summary>
    class HTMLReport : Report
    {
        /// <summary>
        /// Indicates if the program should use the included Dot (graphviz) distribution.
        /// </summary>
        bool UseIncludedDotDistribution;
        /// <summary>
        /// To retrieve all metadata
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
                Process svg = new Process();
                svg.StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/bin/dot.exe" : "dot", "-Tsvg");// " + Path.GetFullPath(filename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(filename), "svg") + "\"");
                svg.StartInfo.RedirectStandardError = true;
                svg.StartInfo.RedirectStandardInput = true;
                svg.StartInfo.RedirectStandardOutput = true;
                svg.StartInfo.UseShellExecute = false;

                Process simplesvg = new Process();
                simplesvg.StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/bin/dot.exe" : "dot", "-Tsvg");// " + Path.GetFullPath(simplefilename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(simplefilename), "svg") + "\"");
                simplesvg.StartInfo.RedirectStandardError = true;
                simplesvg.StartInfo.RedirectStandardInput = true;
                simplesvg.StartInfo.RedirectStandardOutput = true;
                simplesvg.StartInfo.UseShellExecute = false;

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
                for (int i = 0; i < condensed_graph.Count(); i++) {
                    string extra_classes = AllPathsContaining(i).Aggregate("", (a, b) => a + " " + GetAsideIdentifier(b, AsideType.Path)).Substring(1);
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
                throw new Exception("Unexpected exception when trying call dot to build graph: " + e.Message);
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
                buffer.AppendLine($@"<input type=""checkbox"" id=""template-table-collapsable""/>
<label for=""template-table-collapsable"">Template Matching {singleRun.Template[i].Name}</label>
<div class=""collapsable"">{CreateTemplateTable(i)}</div>");
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

            for (int i = 0; i < PathsSequences.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Path);
                link = GetAsideLink(i, AsideType.Path);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(PathsSequences[i].ToArray())}</td>
    <td class=""center"">{PathsSequences[i].Count()}</td>
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
    <h2>Alignment</h2>
    {CreateTemplateAlignment(templateIndex, i)}
    {template.MetaData.ToHTML()}
</div>";
        }
        /// <summary>
        /// Creates an aside for a path 
        /// </summary>
        /// <param name="i">The index</param>
        /// <returns>valid HTML</returns>
        string CreatePathAside(int i)
        {
            string id = GetAsideIdentifier(i, AsideType.Path);
            return $@"<div id=""{id}"" class=""info-block read-info path-info"">
    <h1>Path {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(PathsSequences[i])}</p>
    <h2>Sequence Length</h2>
    <p>{PathsSequences[i].Count()}</p>
    <h2>Sequence Identifiers</h2>
    <p>{paths[i].Item2.Aggregate("", (a, b) => a + " -> " + GetAsideLink(b, AsideType.Contig)).Substring(4)}</p>
    <h2>High scoring templates</h2>
    <p>TO BE DONE</p>
</div>";
        }
        string CreateTemplateAlignment(int templateIndex, int ind)
        {
            StringBuilder buff = new StringBuilder();
            foreach (var match in templates[templateIndex].Templates[ind].Matches)
            {
                buff.AppendLine($"{match.Score}\t{(double)match.Score / match.Length}\t{match.Length}\t{match.TotalMatches()}");
            }
            var file = File.AppendText(@"score_templates.txt");
            file.Write(buff.ToString());
            file.Close();

            var template = templates[templateIndex].Templates[ind];

            var buffer = new StringBuilder();
            var placement = new List<List<bool>>();

            buffer.Append("<p>");

            // Create lists with values 'true' for every position where an aminoacid will be showed
            // First create list with the right startgap and amount of 'true's to add the gaps later
            placement.Add(Enumerable.Repeat(true, template.Sequence.Length).ToList());

            int match_index = 0;
            foreach (var match in template.Matches)
            {
                //Console.WriteLine(GetAsideIdentifier(match_index, AsideType.Contig));
                //Console.WriteLine(match);
                //Console.WriteLine(AminoAcid.ArrayToString(condensed_graph[match_index].Sequence.ToArray()));
                var positions = Enumerable.Repeat(false, match.StartTemplatePosition).ToList(); // Startoverhang
                positions.AddRange(Enumerable.Repeat(true, match.TotalMatches())); // Main sequence
                positions.AddRange(Enumerable.Repeat(false, Math.Max(0, template.Sequence.Length - match.StartTemplatePosition - match.TotalMatches()))); // End overhang
                placement.Add(positions);
                match_index++;
            }

            buffer.Append("</p>");

            // Add the gaps in the sequences
            // Gaps in the template should introduce a gap in all placements
            // Gaps in the contig only introduces a gap in the current placement
            for (int i = 1; i <= template.Matches.Count(); i++)
            {
                var match = template.Matches[i - 1];
                if (match.Alignment.Count() == 1) continue;
                int index = match.StartTemplatePosition;
                foreach (var piece in match.Alignment)
                {
                    if (piece.GetType() == typeof(SequenceMatch.Match))
                    {
                        index += piece.count;
                    }
                    else if (piece.GetType() == typeof(SequenceMatch.GapTemplate))
                    {
                        int pos = PositionOfNthTrue(placement[i], index);
                        for (int j = 0; j <= template.Matches.Count(); j++)
                        {
                            if (i != j)
                            {
                                for (int n = 0; n < piece.count; n++)
                                {
                                    //Console.WriteLine($"{place} {pos} {n}");
                                    if (pos >= placement[j].Count()) placement[j].Add(false);
                                    else placement[j].Insert(pos, false);
                                }
                            }
                        }
                    }
                    else if (piece.GetType() == typeof(SequenceMatch.GapContig))
                    {
                        int pos = PositionOfNthTrue(placement[i], index);
                        for (int n = 0; n < piece.count; n++)
                        {
                            placement[i].Insert(pos, false);
                        }
                    }
                }
            }

            // To save all sequences of all reads and the template
            var sequences = new List<(string, string, string)>();

            // Find the start and endoverhang
            int startoverhang = -1;
            for (int i = 0; i < placement[0].Count(); i++)
            {
                if (placement[0][i] == true)
                {
                    startoverhang = i;
                    break;
                }
            }
            if (startoverhang == -1)
            {
                throw new Exception("While creating the Template Aside for the HTML report an exception occurred: startoverhang is invalid. Please inform someone who works on this project.");
            }

            int endoverhang = -1;
            for (int i = 0; i < placement[0].Count(); i++)
            {
                if (placement[0][placement[0].Count() - i - 1] == true)
                {
                    endoverhang = i;
                    break;
                }
            }
            if (startoverhang == -1)
            {
                throw new Exception("While creating the Template Aside for the HTML report an exception occurred: endoverhang is invalid. Please inform someone who works on this project.");
            }

            // Add the template
            StringBuilder sbt = new StringBuilder();
            string template_seq = AminoAcid.ArrayToString(template.Sequence);
            int template_index = 0;
            for (int i = startoverhang; i < placement[0].Count() - endoverhang; i++)
            {
                if (placement[0][i] == true)
                {
                    sbt.Append(template_seq[template_index]);
                    template_index++;
                }
                else
                {
                    sbt.Append("-");
                }
            }
            sequences.Add(("", sbt.ToString(), ""));

            // Add all the other sequences
            for (int i = 1; i < template.Matches.Count(); i++)
            {
                StringBuilder sb = new StringBuilder();
                string seq = template.Matches[i].Sequence; //AminoAcid.ArrayToString(condensed_graph[i - 1].Sequence.ToArray());
                int seq_index = 0;//template.Matches[i - 1].StartQueryPosition;
                int max_seq = seq.Length;//template.Matches[i - 1].TotalMatches();

                for (int j = startoverhang; j < placement[0].Count() - endoverhang; j++)
                {
                    if (j < placement[i].Count() && placement[i][j] == true)
                    {
                        sb.Append(seq[seq_index]);
                        seq_index++;
                    }
                    else
                    {
                        if (seq_index == template.Matches[i - 1].StartQueryPosition || seq_index > max_seq - 1)
                        {
                            sb.Append("_");
                        }
                        else
                        {
                            sb.Append("-");
                        }
                    }
                }
                string full_seq = sb.ToString();
                int l1 = full_seq.Length - startoverhang - endoverhang;
                //Console.WriteLine($"(0, {startoverhang}) ({startoverhang}, {l1}) ({l1+startoverhang}, {endoverhang})");
                sequences.Add((full_seq.Substring(0, startoverhang), full_seq.Substring(startoverhang, l1), full_seq.Substring(l1 + startoverhang, endoverhang)));
            }

            // New alignment in the same style as the reads alignment
            buffer.AppendLine("<div class=\"reads-alignment\">");

            int max_depth = 0;
            const int bucketsize = 5;

            // Create scores block
            buffer.AppendLine($"<div class='align-block' style='line-height: 11px;margin-top: 100px;font-size: 17px;'>");
            foreach (var match in template.Matches)
            {
                // TODO Also show the score somewhere
                //string rid = GetAsideIdentifier(i - 1, AsideType.Contig);
                buffer.Append($"<a href=\"#\" class='text align-link'>S{match.Score} L{match.Length} E{50000 * match.Length / Math.Pow(2, (0.252 * match.Score - Math.Log(0.035)) / Math.Log(2))}</a><br>");
            }
            buffer.AppendLine("</div>");

            // Create the front overhanging reads block
            string id = GetAsideIdentifier(template_index, ind, AsideType.Template);
            buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            buffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");
            for (int i = 1; i < sequences.Count(); i++)
            {
                // TODO Also show the score somewhere
                string rid = GetAsideIdentifier(i - 1, AsideType.Path);
                buffer.Append($"<a href=\"#{rid}\" class='text align-link'>{sequences[i].Item1}</a><span class='symbol'>...</span><br>");
            }
            buffer.AppendLine($"</p></div></label></div>");

            // Create the main blocks of the sequence alignment
            int seq_length = sequences[0].Item2.Length;
            var sequence = sequences[0].Item2;
            for (int pos = 0; pos <= seq_length / bucketsize; pos++)
            {
                // Add the sequence and the number to tell the position
                string number = "";
                string last = "";
                if (seq_length - pos * bucketsize >= bucketsize)
                {
                    number = ((pos + 1) * bucketsize).ToString();
                    number = String.Concat(Enumerable.Repeat("&nbsp;", bucketsize - number.Length)) + number;
                }
                else
                {
                    last = " last";
                }
                buffer.Append($"<div class='align-block{last}'><p><span class=\"number\">{number}</span><br><span class=\"seq{last}\">{sequence.Substring(pos * bucketsize, Math.Min(bucketsize, sequence.Length - pos * bucketsize))}</span><br>");

                int[] depth = new int[bucketsize];
                // Add every line in order
                for (int i = 1; i < sequences.Count(); i++)
                {
                    string rid = GetAsideIdentifier(i - 1, AsideType.Path);
                    string result = "";
                    if (sequences[i].Item2.Length > pos * bucketsize) result = $"<a href=\"#{rid}\" class=\"align-link\">{sequences[i].Item2.Substring(pos * bucketsize, Math.Min(bucketsize, sequences[i].Item2.Length - pos * bucketsize))}</a>";
                    buffer.Append(result);
                    buffer.Append("<br>");
                    // TODO Update depth
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = 0; i < Math.Min(bucketsize, sequence.Length - pos * bucketsize); i++)
                {
                    if (depth[i] > max_depth) max_depth = depth[i];
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depth[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }

            // Create the end overhanging reads block
            buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"end-overhang-toggle-{id}\"/><label for=\"end-overhang-toggle-{id}\">");
            buffer.AppendFormat("<div class='align-block overhang-block end-overhang'><p><span class='end-overhang-spacing'></span>");
            for (int i = 1; i < sequences.Count(); i++)
            {
                string rid = GetAsideIdentifier(i - 1, AsideType.Path);
                buffer.Append($"<a href=\"#{rid}\" class='text align-link'>{sequences[i].Item3}</a><span class='symbol'>...</span><br>");
            }
            buffer.AppendLine($"</p></div></label></div>");

            // End the reads alignment div
            buffer.AppendLine("</div>");

            return buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{max_depth}'>");
        }
        int PositionOfNthTrue(List<bool> list, int n)
        {
            int sum = 0;
            int i;
            for (i = 0; i < list.Count() && sum < n; i++)
            {
                if (list[i]) sum++;
            }
            return i;
        }
        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateAsides()
        {
            var buffer = new StringBuilder();

            for (int i = 0; i < paths.Count(); i++)
            {
                buffer.AppendLine(CreatePathAside(i));
            }
            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                buffer.AppendLine(CreateContigAside(i));
            }
            for (int i = 0; i < reads.Count(); i++)
            {
                buffer.AppendLine(CreateReadAside(i));
            }
            if (templates.Count() > 0)
            {
                for (int t = 0; t < templates.Count(); t++)
                {
                    for (int i = 0; i < templates[t].Templates.Count(); i++)
                    {
                        buffer.AppendLine(CreateTemplateAside(t, i));
                    }
                }
            }

            return buffer.ToString();
        }
        /// <summary> Create a reads alignment to display in the sidebar. </summary>
        /// <returns> Returns an HTML string. </returns>
        (string, List<int>) CreateReadsAlignment(CondensedNode node)
        {
            string sequence = AminoAcid.ArrayToString(node.Prefix.ToArray()) + AminoAcid.ArrayToString(node.Sequence.ToArray()) + AminoAcid.ArrayToString(node.Suffix.ToArray());
            Dictionary<int, string> lookup = node.UniqueOrigins.Select(x => (x, AminoAcid.ArrayToString(reads[x]))).ToDictionary(item => item.Item1, item => item.Item2);
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, lookup, node.Origins, alphabet, singleRun.K, true);
            sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            int prefixoffset = node.Prefix.Count();

            // Delete matches at prefix and suffix
            positions = positions.Where(a => a.EndPosition > prefixoffset && a.StartPosition < sequence.Length + prefixoffset).ToList();
            //  Update the overhang at the start and end
            positions = positions.Select(a =>
            {
                if (a.StartPosition < prefixoffset)
                {
                    a.StartOverhang += a.Sequence.Substring(0, prefixoffset - a.StartPosition);
                }
                if (a.EndPosition > prefixoffset + sequence.Length)
                {
                    a.EndOverhang += a.Sequence.Substring(a.EndPosition - prefixoffset - sequence.Length);
                }
                return a;
            }).ToList();
            List<int> uniqueorigins = positions.Select(a => a.Identifier).ToList();

            // Find a bit more efficient packing of reads on the sequence
            var placed = new List<List<HelperFunctionality.ReadPlacement>>();
            foreach (var current in positions)
            {
                bool fit = false;
                for (int i = 0; i < placed.Count() && !fit; i++)
                {
                    // Find if it fits in this row
                    bool clashes = false;
                    for (int j = 0; j < placed[i].Count() && !clashes; j++)
                    {
                        if ((current.StartPosition + 1 > placed[i][j].StartPosition && current.StartPosition - 1 < placed[i][j].EndPosition)
                         || (current.EndPosition + 1 > placed[i][j].StartPosition && current.EndPosition - 1 < placed[i][j].EndPosition)
                         || (current.StartPosition - 1 < placed[i][j].StartPosition && current.EndPosition + 1 > placed[i][j].EndPosition))
                        {
                            clashes = true;
                        }
                    }
                    if (!clashes)
                    {
                        placed[i].Add(current);
                        fit = true;
                    }
                }
                if (!fit)
                {
                    placed.Add(new List<HelperFunctionality.ReadPlacement> { current });
                }
            }

            var buffer = new StringBuilder();
            buffer.AppendLine("<div class=\"reads-alignment\">");

            int max_depth = 0;
            const int bucketsize = 5;

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

            // Create the main blocks of the sequence alignment
            for (int pos = 0; pos <= sequence.Length / bucketsize; pos++)
            {
                // Add the sequence and the number to tell the position
                string number = "";
                string last = "";
                if (sequence.Length - pos * bucketsize >= bucketsize)
                {
                    number = ((pos + 1) * bucketsize).ToString();
                    number = String.Concat(Enumerable.Repeat("&nbsp;", bucketsize - number.Length)) + number;
                }
                else
                {
                    last = " last";
                }
                buffer.Append($"<div class='align-block{last}'><p><span class=\"number\">{number}</span><br><span class=\"seq{last}\">{sequence.Substring(pos * bucketsize, Math.Min(bucketsize, sequence.Length - pos * bucketsize))}</span><br>");

                int[] depth = new int[bucketsize];
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
                                depth[i - pos * bucketsize]++;
                            }
                        }
                        buffer.Append(result);
                    }
                    buffer.Append("<br>");
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = 0; i < Math.Min(bucketsize, sequence.Length - pos * bucketsize); i++)
                {
                    if (depth[i] > max_depth) max_depth = depth[i];
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depth[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }

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


            return (buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{max_depth}'>"), uniqueorigins);
        }
        /// <summary>An enum to save what type of detail aside it is.</summary>
        enum AsideType { Contig, Read, Template, Path }
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

            string i1 = "";
            if (index1 == -1) i1 = "";
            else i1 = $"{index1}:";

            string i2 = "";
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
            return $"<a href=\"#{id}\" class=\"info-link {classname}-link\">{id}</a>";
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
<tr><td>Assemblerversion</td><td>{ToRunWithCommandLine.VERSIONNUMBER}</td></tr>
<tr><td>K (length of k-mer)</td><td>{singleRun.K}</td></tr>
<tr><td>Minimum homology</td><td>{singleRun.MinimalHomology}</td></tr>
<tr><td>Duplicate Threshold</td><td>{singleRun.DuplicateThreshold}</td></tr>
<tr><td>Reverse</td><td>{singleRun.Reverse}</td></tr>
<tr><td>Alphabet</td><td>{singleRun.Alphabet.Name}</td></tr>
<tr><td>Number of reads</td><td>{meta_data.reads}</td></tr>
<tr><td>Number of k-mers</td><td>{meta_data.kmers}</td></tr>
<tr><td>Number of (k-1)-mers</td><td>{meta_data.kmin1_mers}</td></tr>
<tr><td>Number of duplicate (k-1)-mers</td><td>{meta_data.kmin1_mers_raw - meta_data.kmin1_mers}</td></tr>
<tr><td>Number of sequences found</td><td>{meta_data.sequences}</td></tr>
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

            string html = $@"<html>
<head>
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
    <p title=""Could help make the report feel more snappy, especially with lower powered devices."">Hover effects</p>
    <label class=""js-toggle"">
        <input type=""checkbox"" onchange=""togglejs()"" checked>
        <span class=""slider"">
    </label>
</div>

{CreateTemplateTables()}

<input type=""checkbox"" id=""graph-collapsable""/>
<label for=""graph-collapsable"">Graph</label>
<div class=""collapsable"">{svg}</div>

<input type=""checkbox"" id=""simple-graph-collapsable""/>
<label for=""simple-graph-collapsable"">Simplified Graph</label>
<div class=""collapsable"">{simplesvg}</div>

<input type=""checkbox"" id=""paths-table-collapsable""/>
<label for=""paths-table-collapsable"">Paths Table</label>
<div class=""collapsable"">{CreatePathsTable()}</div>

<input type=""checkbox"" id=""table-collapsable""/>
<label for=""table-collapsable"">Contigs Table</label>
<div class=""collapsable"">{CreateContigsTable()}</div>

<input type=""checkbox"" id=""reads-table-collapsable""/>
<label for=""reads-table-collapsable"">Reads Table</label>
<div class=""collapsable"">{CreateReadsTable()}</div>

<input type=""checkbox"" id=""meta-collapsable""/>
<label for=""meta-collapsable"">Meta Information</label>
<div class=""collapsable meta-collapsable"">{MetaInformation()}</div>

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