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

namespace AssemblyNameSpace
{
    /// <summary>
    /// To be a basepoint for any reporting options, handling all the metadata.
    /// </summary>
    abstract class Report
    {
        /// <summary>
        /// The condensed graph.
        /// </summary>
        protected List<CondensedNode> condensed_graph;
        /// <summary>
        /// The noncondesed graph.
        /// </summary>
        protected Node[] graph;
        /// <summary>
        /// The metadata of the run.
        /// </summary>
        protected MetaInformation meta_data;
        /// <summary>
        /// The reads used as input in the run.
        /// </summary>
        protected List<AminoAcid[]> reads;
        /// <summary>
        /// Possebly the reads from PEAKS used in the run.
        /// </summary>
        protected List<MetaData.Peaks> peaks_reads;
        /// <summary>
        /// To create a report, gets all metadata.
        /// </summary>
        /// <param name="condensed_graph_input">The condesed graph.</param>
        /// <param name="graph_input">The noncondesed graph.</param>
        /// <param name="meta_data_input">The metadata.</param>
        /// <param name="reads_input">The reads.</param>
        /// <param name="peaks_reads_input">Possibly the PEAKS reads.</param>
        public Report(List<CondensedNode> condensed_graph_input, Node[] graph_input, MetaInformation meta_data_input, List<AminoAcid[]> reads_input, List<MetaData.Peaks> peaks_reads_input)
        {
            condensed_graph = condensed_graph_input;
            graph = graph_input;
            meta_data = meta_data_input;
            reads = reads_input;
            peaks_reads = peaks_reads_input;
        }
        /// <summary>
        /// Creates a report, has to be implemented by all reports.
        /// </summary>
        /// <returns>A string containing the report.</returns>
        public abstract string Create();
        /// <summary>
        /// Saves the Report cretaed with Create to a file.
        /// </summary>
        /// <param name="filename">The path to save the to.</param>
        public void Save(string filename)
        {
            StreamWriter sw = File.CreateText(filename);
            sw.Write(Create());
            sw.Close();
        }
    }
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
        /// <param name="condensed_graph_input">The condesed graph.</param>
        /// <param name="graph_input">The noncondensed graph.</param>
        /// <param name="meta_data_input">The metadata.</param>
        /// <param name="reads_input">The reads.</param>
        /// <param name="peaks_reads_input">Possibly the PEAKS reads.</param>
        /// <param name="useincludeddotdistribution">Indicates if the program should use the included Dot (graphviz) distribution.</param>
        public HTMLReport(List<CondensedNode> condensed_graph_input, Node[] graph_input, MetaInformation meta_data_input, List<AminoAcid[]> reads_input, List<MetaData.Peaks> peaks_reads_input, bool useincludeddotdistribution) : base(condensed_graph_input, graph_input, meta_data_input, reads_input, peaks_reads_input)
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

                buffer.AppendLine($"\ti{i} [label=\"{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}\"{style}]");
                simplebuffer.AppendLine($"\ti{i} [label=\"I{i:D4}\"{style}]");

                foreach (var fwe in condensed_graph[i].ForwardEdges)
                {
                    buffer.AppendLine($"\ti{i} -> i{fwe}");
                    simplebuffer.AppendLine($"\ti{i} -> i{fwe}");
                }
            }

            buffer.AppendLine("}");
            simplebuffer.AppendLine("}");

            // Generate SVG files of the graph
            try
            {
                Process svg = new Process();
                svg.StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/dot.exe" : "dot", "-Tsvg");// " + Path.GetFullPath(filename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(filename), "svg") + "\"");
                svg.StartInfo.RedirectStandardError = true;
                svg.StartInfo.RedirectStandardInput = true;
                svg.StartInfo.RedirectStandardOutput = true;
                svg.StartInfo.UseShellExecute = false;

                Process simplesvg = new Process();
                simplesvg.StartInfo = new ProcessStartInfo(UseIncludedDotDistribution ? "assets/Dot/dot.exe" : "dot", "-Tsvg");// " + Path.GetFullPath(simplefilename) + " -o \"" + Path.ChangeExtension(Path.GetFullPath(simplefilename), "svg") + "\"");
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
                simplesvgwriter.WriteLine(buffer.ToString());
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

                return (svggraph, simplesvggraph);
            }
            catch (Exception e)
            {
                throw new Exception("Unexpected Expection when trying call dot to build graph: " + e.Message);
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
            string id;

            for (int i = 0; i < reads.Count(); i++)
            {
                id = GetReadLink(i);
                buffer.AppendLine($@"<tr id=""reads-table-r{i}"">
    <td class=""center"">{id}</td>
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
            string id;

            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                id = GetCondensedNodeLink(i);
                buffer.AppendLine($@"<tr id=""table-i{i}"">
    <td class=""center"">{id}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}</td>
    <td class=""center"">{condensed_graph[i].Sequence.Count()}</td>
    <td class=""center"">{condensed_graph[i].ForwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
    <td class=""center"">{condensed_graph[i].BackwardEdges.Aggregate<int, string>("", (a, b) => a + " " + GetCondensedNodeLink(b))}</td>
    <td>{condensed_graph[i].Origins.Aggregate<int, string>("", (a, b) => a + " " + GetReadLink(b))}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateAsides()
        {
            var buffer = new StringBuilder();
            string id;

            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                id = $"I{i:D4}";
                string prefix = "";
                if (condensed_graph[i].Prefix != null) prefix = AminoAcid.ArrayToString(condensed_graph[i].Prefix.ToArray());
                string suffix = "";
                if (condensed_graph[i].Suffix != null) suffix = AminoAcid.ArrayToString(condensed_graph[i].Suffix.ToArray());

                buffer.AppendLine($@"<div id=""{id}"" class=""info-block contig-info"">
    <h1>Contig: {id}</h1>
    <h2>Sequence (length={condensed_graph[i].Sequence.Count()})</h2>
    <p class=""aside-seq""><span class='prefix'>{prefix}</span>{AminoAcid.ArrayToString(condensed_graph[i].Sequence.ToArray())}<span class='suffix'>{suffix}</span></p>
    <h2>Reads Alignment</h4>
    {CreateReadsAlignment(condensed_graph[i])}
    <h2>Based on</h2>
    <p>{condensed_graph[i].Origins.Aggregate<int, string>("", (a, b) => a + " " + GetReadLink(b))}</p>
</div>");
            }
            for (int i = 0; i < reads.Count(); i++)
            {
                id = $"R{i:D4}";
                string meta = peaks_reads == null ? "" : peaks_reads[i].ToHTML();
                buffer.AppendLine($@"<div id=""{id}"" class=""info-block read-info"">
    <h1>Read: {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(reads[i])}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(reads[i]).Count()}</p>
    {meta}
</div>");
            }

            return buffer.ToString();
        }
        /// <summary> Create a reads alignment to display in the sidebar. </summary>
        /// <returns> Returns an HTML string. </returns>
        string CreateReadsAlignment(CondensedNode node)
        {
            string sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            List<(string, int)> reads_array = node.Origins.Select(x => (AminoAcid.ArrayToString(reads[x]), x)).ToList();
            var positions = new Queue<(string, int, int, int)>(HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, reads_array, true));

            // Find a bit more efficient packing of reads on the sequence
            var placed = new List<List<(string, int, int, int)>>();
            while (positions.Count() > 0)
            {
                var current = positions.Dequeue();
                bool fit = false;
                for (int i = 0; i < placed.Count() && !fit; i++)
                {
                    // Find if it fits in this row
                    bool clashes = false;
                    for (int j = 0; j < placed[i].Count() && !clashes; j++)
                    {
                        if ((current.Item2 + 1 > placed[i][j].Item2 && current.Item2 - 1 < placed[i][j].Item3)
                         || (current.Item3 + 1 > placed[i][j].Item2 && current.Item3 - 1 < placed[i][j].Item3)
                         || (current.Item2 - 1 < placed[i][j].Item2 && current.Item3 + 1 > placed[i][j].Item3))
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
                    placed.Add(new List<(string, int, int, int)> { current });
                }
            }

            var buffer = new StringBuilder();
            buffer.AppendLine("<div class=\"reads-alignment\">");

            int max_depth = 0;
            const int bucketsize = 5;
            for (int pos = 0; pos <= sequence.Length / bucketsize; pos++)
            {
                // Add the sequence and the number to tell the position
                string number = ((pos + 1) * bucketsize).ToString();
                buffer.Append($"<div class='align-block'><p><span class=\"number\">{String.Concat(Enumerable.Repeat("&nbsp;", bucketsize - number.Length))}{number}</span><br><span class=\"seq\">{sequence.Substring(pos * bucketsize, Math.Min(bucketsize, sequence.Length - pos * bucketsize))}</span><br>");

                int[] depth = new int[bucketsize];
                // Add every niveau in order
                foreach (var line in placed)
                {
                    for (int i = pos * bucketsize; i < pos * bucketsize + bucketsize; i++)
                    {
                        string result = "&nbsp;";
                        foreach (var read in line)
                        {
                            if (i >= read.Item2 && i < read.Item3)
                            {
                                result = $"<a href=\"#R{read.Item4:D4}\" class=\"align-link\">{read.Item1[i - read.Item2]}</a>";
                                depth[i - pos * bucketsize]++;
                            }
                        }
                        buffer.Append(result);
                    }
                    buffer.Append("<br>");
                }
                buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");
                for (int i = 0; i < bucketsize; i++)
                {
                    if (depth[i] > max_depth) max_depth = depth[i];
                    buffer.Append($"<span class='coverage-depth-bar' style='--value:{depth[i]}'></span>");
                }
                buffer.Append("</div></div>");
            }
            buffer.AppendLine("</div>");

            return buffer.ToString().Replace("<div class=\"reads-alignment\">", $"<div class='reads-alignment' style='--max-value:{max_depth}'>");
        }
        /// <summary> Returns the string representation of the human friendly identifier of a node. </summary>
        /// <param name="index"> The index in the condensed graph of the condensed node. </param>
        /// <returns> A string to be used where humans can see it. </returns>
        string GetCondensedNodeLink(int index)
        {
            return $"<a href=\"#I{index:D4}\" class=\"info-link contig-link\">I{index:D4}</a>";
        }
        /// <summary> Returns the string representation of the human friendly identifier of a read. </summary>
        /// <param name="index"> The index in the readslist. </param>
        /// <returns> A string to be used where humans can see it. </returns>
        string GetReadLink(int index)
        {
            return $"<a href=\"#R{index:D4}\" class=\"info-link read-link\">R{index:D4}</a>";
        }
        /// <summary> Returns some meta information about the assembly the help validate the output of the assembly. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string MetaInformation()
        {
            long number_edges = graph.Aggregate(0L, (a, b) => a + b.EdgesCount()) / 2L;
            long number_edges_condensed = condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L;

            string html = $@"
<h3>General information</h3>
<table>
<tr><td>Number of reads</td><td>{meta_data.reads}</td></tr>
<tr><td>K (length of k-mer)</td><td>{meta_data.kmer_length}</td></tr>
<tr><td>Minimum homology</td><td>{meta_data.minimum_homology}</td></tr>
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
<p>Total time: {meta_data.total_time + meta_data.drawingtime} ms</p>
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

            // Give the filled nodes (start node) the correct class
            svg = Regex.Replace(svg, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
            // Give the nodes the correct ID
            svg = Regex.Replace(svg, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>i([0-9]+)</title>", "id=\"node$2\" class=\"$1\" onclick=\"Select('I', $2)\">");
            // Strip all <title> tags
            svg = Regex.Replace(svg, "<title>[^<]*</title>", "");

            // Give the filled nodes (start node) the correct class
            simplesvg = Regex.Replace(simplesvg, "class=\"node\"(>\\s*<title>[^<]*</title>\\s*<polygon fill=\"blue\")", "class=\"node start-node\"$1");
            // Give the nodes the correct ID
            simplesvg = Regex.Replace(simplesvg, "id=\"node[0-9]+\" class=\"([a-z\\- ]*)\">\\s*<title>i([0-9]+)</title>", "id=\"simple-node$2\" class=\"$1\" onclick=\"Select('I', $2)\">");
            // Strip all <title> tags
            simplesvg = Regex.Replace(simplesvg, "<title>[^<]*</title>", "");

            string stylesheet = "// Could not find the stylesheet";
            if (File.Exists("assets/styles.css")) stylesheet = File.ReadAllText("assets/styles.css");

            string script = "// Could not find the script";
            if (File.Exists("assets/script.js")) script = File.ReadAllText("assets/script.js");

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
{script}
</script>
</head>
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

<input type=""checkbox"" id=""graph-collapsable""/>
<label for=""graph-collapsable"">Graph</label>
<div class=""collapsable"">{svg}</div>

<input type=""checkbox"" id=""simple-graph-collapsable""/>
<label for=""simple-graph-collapsable"">Simplified Graph</label>
<div class=""collapsable"">{simplesvg}</div>

<input type=""checkbox"" id=""table-collapsable""/>
<label for=""table-collapsable"">Table</label>
<div class=""collapsable"">{CreateContigsTable()}</div>

<input type=""checkbox"" id=""reads-table-collapsable""/>
<label for=""reads-table-collapsable"">Reads Table</label>
<div class=""collapsable"">{CreateReadsTable()}</div>

<input type=""checkbox"" id=""meta-collapsable""/>
<label for=""meta-collapsable"">Meta Information</label>
<div class=""collapsable meta-collapsable"">{MetaInformation()}</div>

<div class=""footer"">
    <p>Code written in 2019</p>
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
    class CSVReport : Report
    {
        public CSVReport(List<CondensedNode> condensed_graph_input, Node[] grap_input, MetaInformation meta_data_input, List<AminoAcid[]> reads_input, List<MetaData.Peaks> peaks_reads_input) : base(condensed_graph_input, grap_input, meta_data_input, reads_input, peaks_reads_input) { }
        public override string Create()
        {
            return "";
        }
        /// <summary> Fill metainformation in a CSV line and append it to the given file. </summary>
        /// <param name="ID">ID of the run to recognise it in the CSV file. </param>
        /// <param name="filename"> The file to which to append the CSV line to. </param>
        /// <param name="path_to_template"> The path to the original fasta file, to get extra information. </param>
        /// <param name="extra"> Extra field to fill in own information. Created for holding the alphabet. </param>
        /// <param name="path_to_report"> The path to the report to add a hyperlink to the CSV file. </param>
        public void CreateCSVLine(string ID, string filename = "report.csv", string path_to_template = null, string extra = "", string path_to_report = "", bool extended = false)
        {
            // If the original sequence is known, calculate the coverage
            string coverage = "";
            if (path_to_template != null && path_to_template != "")
            {
                // Get the sequences
                var fastafile = File.ReadAllText(path_to_template);
                var raw_sequences = Regex.Split(fastafile, ">");
                var seqs = new List<string>();

                foreach (string seq in raw_sequences)
                {
                    var seq_lines = seq.Split("\n".ToCharArray());
                    string sequence = "";
                    for (int i = 1; i < seq_lines.Length; i++)
                    {
                        sequence += seq_lines[i].Trim("\r\n\t 0123456789".ToCharArray());
                    }
                    if (sequence != "") seqs.Add(sequence);
                }

                // Calculate the coverage
                string[] reads_array = reads.Select(x => AminoAcid.ArrayToString(x)).ToArray();
                string[] contigs_array = condensed_graph.Select(x => AminoAcid.ArrayToString(x.Sequence.ToArray())).ToArray();

                if (seqs.Count() == 2 && !extended)
                {
                    var coverage_reads_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[0], reads_array);
                    var coverage_reads_light = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[1], reads_array);
                    var coverage_contigs_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[0], contigs_array);
                    var coverage_contigs_light = HelperFunctionality.MultipleSequenceAlignmentToTemplate(seqs[1], contigs_array);

                    coverage = $"{coverage_reads_heavy.Item1};{coverage_reads_heavy.Item2};{coverage_reads_light.Item1};{coverage_reads_light.Item2};{coverage_contigs_heavy.Item1};{coverage_contigs_heavy.Item2};{coverage_contigs_light.Item1};{coverage_contigs_light.Item2};";
                }
                else if (seqs.Count() == 2 && extended)
                {
                    // Filter only assembled contigs
                    contigs_array = condensed_graph.Where(n => n.Origins.Count() > 1).Select(n => AminoAcid.ArrayToString(n.Sequence.ToArray())).ToArray();

                    var coverage_reads_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[0], reads_array);
                    var coverage_reads_light = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[1], reads_array);
                    var coverage_contigs_heavy = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[0], contigs_array);
                    var coverage_contigs_light = HelperFunctionality.MultipleSequenceAlignmentToTemplateExtended(seqs[1], contigs_array);

                    // Create list of unique mapped peptides
                    List<string> correct_contigs = new List<string>(coverage_contigs_heavy.Item2);
                    foreach (var x in coverage_contigs_light.Item2)
                    {
                        if (!correct_contigs.Contains(x)) correct_contigs.Add(x);
                    }

                    coverage = $"{coverage_contigs_heavy.Item1};{coverage_contigs_light.Item1};{coverage_contigs_heavy.Item3};{coverage_contigs_light.Item3};{contigs_array.Count()};{correct_contigs.Count()};{coverage_reads_heavy.Item1};{coverage_reads_light.Item1};{coverage_reads_heavy.Item3};{coverage_reads_light.Item3};{condensed_graph.Aggregate("", (a, b) => a + b.Sequence.Count() + "|")};";
                }
                else
                {
                    Console.WriteLine($"Not an antibody fasta file: {path_to_template}");
                }
            }

            // If the path to the report is known create a hyperlink
            string link = "";
            if (path_to_report != "" && !extended)
            {
                link = $"=HYPERLINK(\"{path_to_report}\");";
            }

            int totallength = condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()));
            int totalnodes = condensed_graph.Count();
            string line = $"{ID};{extra};{meta_data.reads};{meta_data.kmer_length};{meta_data.minimum_homology};{totalnodes};{(double)totallength / totalnodes};{totallength};{(double)condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L / condensed_graph.Count()};{meta_data.total_time};{meta_data.drawingtime};{coverage}{link}\n";

            if (File.Exists(filename))
            {
                // To account for multithreading and multiple workers trying to append to the file at the same time
                // This will keep trying to append the line until it worked
                bool stuck = true;
                while (stuck)
                {
                    try
                    {
                        File.AppendAllText(filename, line);
                        stuck = false;
                    }
                    catch
                    {
                        // try again
                    }
                }
            }
            else
            {
                StreamWriter sw = File.CreateText(filename);
                sw.Write(line);
                sw.Close();
            }
        }
    }
    class FASTAReport : Report
    {
        int MinScore;
        public FASTAReport(List<CondensedNode> condensed_graph_input, Node[] grap_input, MetaInformation meta_data_input, List<AminoAcid[]> reads_input, List<MetaData.Peaks> peaks_reads_input, int minscore) : base(condensed_graph_input, grap_input, meta_data_input, reads_input, peaks_reads_input)
        {
            MinScore = minscore;
        }
        /// <summary>
        /// Creates a FATSA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score.
        /// </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create()
        {
            var buffer = new StringBuilder();
            var sequences = new List<(int, string)>();

            for (int node_index = 0; node_index < condensed_graph.Count(); node_index++)
            {
                var node = condensed_graph[node_index];

                // Test if this is a starting node
                if (node.BackwardEdges.Count() == 0 || (node.BackwardEdges.Count() == 1 && node.BackwardEdges[0] == node_index))
                {
                    GetPaths(node_index, "", 0, sequences, new List<int>());
                }
            }

            // Filter and sort the lines
            sequences = sequences.FindAll(i => i.Item1 >= MinScore);
            sequences.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            foreach (var line in sequences)
            {
                buffer.AppendLine(line.Item2);
            }

            return buffer.ToString();
        }
        /// <summary>
        /// Gets all paths starting from the given node.
        /// </summary>
        /// <param name="node_index">The node to start from</param>
        /// <param name="currentpath">The sequences up to the start node</param>
        /// <param name="currentscore">The score up to the start node</param>
        /// <param name="output">The list containing all lines, here the output of the function will be aggregated</param>
        /// <param name="indices">The list of all indices of the path up to the start node</param>
        /// <returns>Nothing, see output for the output</returns>
        void GetPaths(int node_index, string currentpath, int currentscore, List<(int, string)> output, List<int> indices)
        {
            // Update all paths and scores
            var node = condensed_graph[node_index];
            string nextpath = currentpath + AminoAcid.ArrayToString(node.Sequence.ToArray());
            int nextscore = currentscore + CalculateScore(node);
            indices.Add(node_index);

            if (node.ForwardEdges.Count() == 0)
            {
                // End of the sequences, create the output
                // Create the ID of the path (indices of all contigs)
                string id = indices.Aggregate("", (b, a) => $"{b}-{a.ToString()}").Substring(1);

                output.Add((nextscore, $">{id} score:{nextscore}\n{nextpath}"));
            }
            else
            {
                // Follow all branches
                foreach (var next in node.ForwardEdges)
                {
                    if (indices.Contains(next))
                    {
                        // Cycle: end the following of the path and generate the output
                        // Create the ID of the path (indices of all contigs)
                        string id = indices.Aggregate("", (b, a) => $"{b}-{a.ToString()}").Substring(1);

                        output.Add((nextscore, $">{id}-|{next.ToString()}| score:{nextscore}\n{nextpath}"));
                    }
                    else
                    {
                        // Follow the sequence
                        GetPaths(next, nextpath, nextscore, output, new List<int>(indices));
                    }
                }
            }
        }
        /// <summary> Create a reads alignment and calculates depth of coverage. </summary>
        /// <param name="node">The node to calculate the score of</param>
        /// <returns> Returns a score per base. </returns>
        int CalculateScore(CondensedNode node)
        {
            // Align the reads used for this sequence to the sequence
            string sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            List<(string, int)> reads_array = node.Origins.Select(x => (AminoAcid.ArrayToString(reads[x]), x)).ToList();
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, reads_array, true);

            // Calculate the score by calculating the total read length (which maps to a location on the contig)
            int score = 0;
            for (int pos = 0; pos < sequence.Length; pos++)
            {
                foreach (var read in positions)
                {
                    if (pos >= read.Item2 && pos < read.Item3)
                    {
                        score++;
                    }
                }
            }

            return score;
        }
    }
}