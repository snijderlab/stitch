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
    /// <summary>To save all parameters for the generation of a report in one place</summary>
    struct ReportInputParameters {
        public Assembler assembler;
        public RunParameters.SingleRun singleRun;
        public List<TemplateDatabase> templateDatabases;
        public ReportInputParameters(Assembler assm, RunParameters.SingleRun run, List<TemplateDatabase> databases) {
            assembler = assm;
            singleRun = run;
            templateDatabases = databases;
        }
    }
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
        /// The uncondensed graph.
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
        /// Possibly the reads from PEAKS used in the run.
        /// </summary>
        protected List<MetaData.IMetaData> reads_metadata;
        /// <summary>
        /// The alphabet used in the assembly
        /// </summary>
        protected Alphabet alphabet;
        /// <summary>
        /// The runparameters
        /// </summary>
        protected RunParameters.SingleRun singleRun;
        protected List<TemplateDatabase> templates;
        /// <summary>
        /// To create a report, gets all metadata.
        /// </summary>
        /// <param name="parameters">The parameters for this report.</param>
        public Report(ReportInputParameters parameters)
        {
            condensed_graph = parameters.assembler.condensed_graph;
            graph = parameters.assembler.graph;
            meta_data = parameters.assembler.meta_data;
            reads = parameters.assembler.reads;
            reads_metadata = parameters.assembler.reads_metadata;
            alphabet = parameters.assembler.alphabet;
            singleRun = parameters.singleRun;
            templates = parameters.templateDatabases;
        }
        /// <summary>
        /// Creates a report, has to be implemented by all reports.
        /// </summary>
        /// <returns>A string containing the report.</returns>
        public abstract string Create();
        /// <summary>
        /// Saves the Report created with Create to a file.
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

            Console.WriteLine(simplebuffer.ToString());

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
        
        string CreateTemplateTable() {
            var buffer = new StringBuilder();

            buffer.AppendLine(@"<table id=""template-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('template-table', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('template-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('template-table', 2, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('template-table', 3, 'number')"" class=""smallcell"">Score</th>
</tr>");
            string id, link;

            for (int i = 0; i < templates[0].templates.Count(); i++)
            {
                id = GetAsideIdentifier(i, AsideType.Template);
                link = GetAsideLink(i, AsideType.Template);
                buffer.AppendLine($@"<tr id=""table-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{AminoAcid.ArrayToString(templates[0].templates[i].Item1.ToArray())}</td>
    <td class=""center"">{templates[0].templates[i].Item1.Count()}</td>
    <td class=""center"">TBD</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }
        /// <summary> Returns an aside for details viewing of a contig. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateContigAside(int i) {
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
        string CreateReadAside(int i) {
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
        string CreateTemplateAside(int i) {
            string id = GetAsideIdentifier(i, AsideType.Template);
            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>Template {id}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(templates[0].templates[i].Item1)}</p>
    <h2>Sequence Length</h2>
    <p>{AminoAcid.ArrayToString(templates[0].templates[i].Item1).Count()}</p>
    {templates[0].templates[i].Item2.ToHTML()}
</div>";
        }
        /// <summary> Returns a list of asides for details viewing. </summary>
        /// <returns> A string containing valid HTML ready to paste into an HTML file. </returns>
        string CreateAsides()
        {
            var buffer = new StringBuilder();

            for (int i = 0; i < condensed_graph.Count(); i++)
            {
                buffer.AppendLine(CreateContigAside(i));
            }
            for (int i = 0; i < reads.Count(); i++)
            {
                buffer.AppendLine(CreateReadAside(i));
            }
            if (templates.Count() > 0) {
                for (int i = 0; i < templates[0].templates.Count(); i++) {
                    buffer.AppendLine(CreateTemplateAside(i));
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
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, lookup, node.Origins, alphabet, true);
            sequence = AminoAcid.ArrayToString(node.Sequence.ToArray());
            int prefixoffset = node.Prefix.Count();

            // Delete matches at prefix and suffix
            positions = positions.Where(a => a.EndPosition > prefixoffset && a.StartPosition < sequence.Length + prefixoffset).ToList();
            //  Update the overhang at the start and end
            positions = positions.Select(a => {
                if (a.StartPosition < prefixoffset) {
                    a.StartOverhang += a.Sequence.Substring(0, prefixoffset - a.StartPosition);
                }
                if (a.EndPosition > prefixoffset + sequence.Length ) {
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
                   if (read.StartPosition == 0 && read.StartOverhang != "") {
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
                } else {
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
                   if (read.EndPosition == sequence.Length && read.EndOverhang != "") {
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
        enum AsideType {Contig, Read, Template}
        string GetAsidePrefix(AsideType type) {
            switch (type) {
                case AsideType.Contig:
                    return "C";
                case AsideType.Read:
                    return "R";
                case AsideType.Template:
                    return "T";
            }
            throw new Exception("Invalid AsideType in GetAsidePrefix.");
        }
        /// <summary>To generate an identifier ready for use in the HTML page.</summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(int index, AsideType type) {
            string pre = GetAsidePrefix(type);
            if (index < 9999) {
                return $"{pre}{index:D4}";
            } else {
                return $"{pre}{index}";
            }
        }
        /// <summary> Returns a link to the given aside. </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns> A string to be used where humans can see it. </returns>
        string GetAsideLink(int index, AsideType type)
        {
            string id = GetAsideIdentifier(index, type);
            string classname = "";
            if (type == AsideType.Contig) classname = "contig";
            if (type == AsideType.Read) classname = "read";
            if (type == AsideType.Template) classname = "template";
            return $"<a href=\"#{id}\" class=\"info-link {classname}-link\">{id}</a>";
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
{script}
</script>
</head>
<body onload=""Setup()"">
<div class=""report"">
<h1>Report Protein Sequence Run</h1>
<p>Generated at {timestamp}</p>

<input type=""checkbox"" id=""template-table-collapsable""/>
<label for=""template-table-collapsable"">Template Table</label>
<div class=""collapsable"">{CreateTemplateTable()}</div>

<input type=""checkbox"" id=""graph-collapsable""/>
<label for=""graph-collapsable"">Graph</label>
<div class=""collapsable"">{svg}</div>

<input type=""checkbox"" id=""simple-graph-collapsable""/>
<label for=""simple-graph-collapsable"">Simplified Graph</label>
<div class=""collapsable"">{simplesvg}</div>

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
        /// <summary>
        /// To retrieve all metadata
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public CSVReport(ReportInputParameters parameters) : base(parameters) { }
        public override string Create()
        {
            return "";
        }
        /// <summary>
        /// Prepares the file to be used for a CSV report
        /// </summary>
        /// <param name="filename">The path to the file</param>
        public void PrepareCSVFile(string filename)
        {
            StreamWriter sw = File.CreateText(filename);
            string link = singleRun.Report.Where(a => a is RunParameters.Report.HTML).Count() > 0 ? "Hyperlink(s) to the report(s);" : "";
            sw.Write($"sep=;\nID;Data file;Alphabet;K-mer length;Minimal Homology;Duplicate Threshold;Reads;Total nodes;Average Sequence Length;Average depth of coverage;Mean Connectivity;Total time;{link}\n");
            sw.Close();
        }
        /// <summary>
        /// The key to get access to write to the CSV file
        /// </summary>
        static object CSVKey = new Object();

        /// <summary> Fill metainformation in a CSV line and append it to the given file. </summary>
        /// <param name="ID">ID of the run to recognize it in the CSV file. </param>
        /// <param name="filename"> The file to which to append the CSV line to. </param>
        public void CreateCSVLine(string ID, string filename)
        {
            // HYPERLINK
            int totallength = condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()));
            int totalreadslength = reads.Aggregate(0, (a, b) => a + b.Length) * (singleRun.Reverse ? 2 : 1);
            int totalnodes = condensed_graph.Count();
            string data = singleRun.Input.Count() == 1 ? singleRun.Input[0].File.Name : "Group";
            string link = singleRun.Report.Where(a => a is RunParameters.Report.HTML).Count() > 0 ? singleRun.Report.Where(a => a is RunParameters.Report.HTML).Aggregate("", (a, b) => (a + "=HYPERLINK(\"" + Path.GetFullPath(b.CreateName(singleRun)) + "\");")) : "";
            string line = $"{ID};{data};{singleRun.Alphabet.Name};{singleRun.K};{singleRun.MinimalHomology};{singleRun.DuplicateThreshold};{meta_data.reads};{totalnodes};{(double)totallength / totalnodes};{(double)totalreadslength / totallength};{(double)condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L / condensed_graph.Count()};{meta_data.total_time};{link}\n";

            // To account for multithreading and multiple workers trying to append to the file at the same time
            // This will block any concurrent access
            lock (CSVKey)
            {
                if (File.Exists(filename))
                {
                    File.AppendAllText(filename, line);
                }
                else
                {
                    PrepareCSVFile(filename);
                    File.AppendAllText(filename, line);
                }
            }
        }
    }
    class FASTAReport : Report
    {
        int MinScore;
        /// <summary>
        /// To retrieve all metadata
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="minscore">The minimal score needed to be included in the file</param>
        public FASTAReport(ReportInputParameters parameters, int minscore) : base(parameters)
        {
            MinScore = minscore;
        }
        /// <summary>
        /// Creates a FASTA file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score.
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
            Dictionary<int, string> lookup = node.UniqueOrigins.Select(x => (x, AminoAcid.ArrayToString(reads[x]))).ToDictionary(item => item.Item1, item => item.Item2);
            var positions = HelperFunctionality.MultipleSequenceAlignmentToTemplate(sequence, lookup, node.Origins, alphabet, true);

            // Calculate the score by calculating the total read length (which maps to a location on the contig)
            int score = 0;
            for (int pos = 0; pos < sequence.Length; pos++)
            {
                foreach (var read in positions)
                {
                    if (pos >= read.StartPosition && pos < read.EndPosition)
                    {
                        score++;
                    }
                }
            }

            return score;
        }
    }
}