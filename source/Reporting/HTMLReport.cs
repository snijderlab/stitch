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

        string CreateTemplateTables(List<Segment> segments, int templateGroup)
        {
            var output = new StringBuilder();

            for (var i = 0; i < segments.Count(); i++)
            {
                var item = segments[i];
                output.Append(Collapsible($"Segment {item.Name}", CreateTemplateTable(item.Templates, templateGroup, i, AsideType.Template, true), $"{templateGroup}-"));
            }

            return output.ToString();
        }

        string CreateTemplateTable(List<Template> templates, int templateGroup, int templateIndex, AsideType type, bool header = false)
        {
            var buffer = new StringBuilder();
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            var groupIdentifier = $"{templateGroup}-{templateIndex}";

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            if (header)
                buffer.Append(TableHeader(templates, groupIdentifier));

            string unique = "";
            if (displayUnique) unique = $@"
<th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 5, 'number')"" class=""smallcell"">Unique Score</th>
<th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 6, 'number')"" class=""smallcell"">Unique Matches</th>
<th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 7, 'number')"" class=""smallcell"">Unique Area</th>
";

            buffer.AppendLine($@"<table id=""template-table-{type}-{templateIndex}-{templateGroup}"" class=""widetable"">
<tr>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 0, 'id')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 1, 'number')"" class=""smallcell"">Length</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 2, 'number')"" class=""smallcell"" data-sortorder=""desc"">Score</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 3, 'number')"" class=""smallcell"">Matches</th>
    <th onclick=""sortTable('template-table-{type}-{templateIndex}-{templateGroup}', 4, 'number')"" class=""smallcell"">Total Area</th>
    {unique}
</tr>");

            (double, double, double, double, double, double) max_values = (Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue);
            foreach (var template in templates)
            {
                max_values = (
                    Math.Max(max_values.Item1, template.Score),
                    Math.Max(max_values.Item2, template.Matches.Count()),
                    Math.Max(max_values.Item3, template.TotalArea),
                    Math.Max(max_values.Item4, template.UniqueScore),
                    Math.Max(max_values.Item5, template.UniqueMatches),
                    Math.Max(max_values.Item6, template.TotalUniqueArea)
                    );
            }

            string id, link;
            for (int i = 0; i < templates.Count(); i++)
            {
                id = GetAsideIdentifier(templates[i].MetaData);
                link = GetAsideLink(templates[i].MetaData, type);
                if (displayUnique) unique = $@"
<td class=""center bar"" style=""--relative-value:{templates[i].UniqueScore / max_values.Item4}"">{templates[i].UniqueScore}</td>
<td class=""center bar"" style=""--relative-value:{templates[i].UniqueMatches / max_values.Item5}"">{templates[i].UniqueMatches}</td>
<td class=""center bar"" style=""--relative-value:{templates[i].TotalUniqueArea / max_values.Item6}"">{templates[i].TotalUniqueArea.ToString("G3")}</td>
";
                buffer.AppendLine($@"<tr id=""{groupIdentifier}-{id}"">
    <td class=""center"">{link}</td>
    <td class=""center"">{templates[i].Sequence.Length}</td>
    <td class=""center bar"" style=""--relative-value:{templates[i].Score / max_values.Item1}"">{templates[i].Score}</td>
    <td class=""center bar"" style=""--relative-value:{templates[i].Matches.Count() / max_values.Item2}"">{templates[i].Matches.Count()}</td>
    <td class=""center bar"" style=""--relative-value:{templates[i].TotalArea / max_values.Item3}"">{templates[i].TotalArea.ToString("G3")}</td>
    {unique}
</tr>");
            }

            buffer.AppendLine("</table>");
            CultureInfo.CurrentCulture = culture;

            return buffer.ToString();
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
            string annotated = "";
            string title = "Segment";
            switch (type)
            {
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null)
                    {
                        var order = template.Recombination.Aggregate(
                            "",
                            (acc, seg) => acc + " → " + GetAsideLink(seg.MetaData, AsideType.Template, location)
                        ).Substring(3);
                        based = $"<h2>Order</h2><p>{order}</p>";
                    }
                    title = "Recombined Template";
                    try
                    {
                        var regions = HelperFunctionality.AnnotateDomains(AminoAcid.ArrayToString(consensus_sequence));
                        annotated = $@"<h2>Annotated consensus sequence</h2>
                    <div class='annotated'>
                        <span class='fr1'><p>FR1</p><p class='aside-seq'>{regions[0]}</p>
                        <span class='cdr1'><p>CDR1</p><p class='aside-seq'>{regions[1]}</p>
                        <span class='fr2'><p>FR2</p><p class='aside-seq'>{regions[2]}</p>
                        <span class='cdr2'><p>CDR2</p><p class='aside-seq'>{regions[3]}</p>
                        <span class='fr3'><p>FR3</p><p class='aside-seq'>{regions[4]}</p>
                        <span class='cdr3'><p>CDR3</p><p class='aside-seq'>{regions[5]}</p>
                        <span class='constant'><p>Constant region</p><p class='aside-seq'>{regions[6]}</p>
                    </div>";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not find regions in consensus sequence for {GetAsideIdentifier(superindex, index, i, type, true)}\n{e.Message}");
                    }
                    break;
                default:
                    break;
            }


            return $@"<div id=""{id}"" class=""info-block template-info"">
    <h1>{title} {GetAsideIdentifier(superindex, index, i, type, true)}</h1>
    <h2>Consensus Sequence</h2>
    <p class='aside-seq'>{AminoAcid.ArrayToString(consensus_sequence)}</p>
    {annotated}
    <h2>Sequence Consensus Overview</h2>
    {SequenceConsensusOverview(template)}
    <div class='docplot'><h2>Depth Of Coverage of the Consensus Sequence</h2>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(consensus_doc))}</div>
    <h2>Scores</h2>
    {CreateTemplateTable(new List<Template> { template }, superindex, index, type)}
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

        public (string Alignment, List<double> DepthOfCoverage) CreateTemplateAlignment(Template template, string id, List<string> location)
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
            int read_offset = Parameters.Input.Count();

            for (int i = 0; i < totalsequences + 1; i++)
            {
                lines[i] = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>();
            }

            for (int template_pos = 0; template_pos < alignedSequences.Count(); template_pos++)
            {
                var (Sequences, Gaps) = alignedSequences[template_pos];
                lines[0].Add((template.Sequence[template_pos].ToString(), -1, -1, AsideType.Read));
                double depth = 0;

                // Add the aligned amino acid
                for (int i = 0; i < Sequences.Length; i++)
                {
                    int index = Sequences[i].SequencePosition;
                    depth += Sequences[i].CoverageDepth;

                    if (index == -1)
                    {
                        lines[i + 1].Add((gapchar.ToString(), -1, -1, AsideType.Read));
                    }
                    else if (index == 0)
                    {
                        lines[i + 1].Add((nonbreakingspace.ToString(), -1, -1, AsideType.Read));
                    }
                    else
                    {
                        var type = AsideType.Read;
                        var idx = Sequences[i].MatchIndex;

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
                lines[0].Add((new string(gapchar, max_length), -1, -1, AsideType.Read));

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

                    var type = AsideType.Read;
                    var idx = index;

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

                    var positions = new List<(int index, int position, int length)>[aligned.Length];

                    for (int i = 1; i < aligned.Length; i++)
                    {
                        int index = lines[i][alignedindex].Index;
                        int position = lines[i][alignedindex].SequencePosition;
                        int additionallength = 0;
                        int additionalindex = 1;
                        positions[i] = new List<(int index, int position, int length)>();

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
                                positions[i].Add((index, position, additionallength));
                                index = thisindex;
                                position = thisposition;
                                break;
                            }

                            additionallength += lines[0][alignedindex + additionalindex].Sequence.Length;
                            additionalindex++;
                        }

                        if (index >= 0)
                            positions[i].Add((index, position, blocklength));
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
                        if (positions[i].Count() > 0)
                        {
                            alignblock.Append("<span class=\"align-link\">");
                            int offset = 0;
                            foreach (var piece in positions[i])
                            {
                                var rid = "none";
                                var name = GetAsideName(AsideType.Read);
                                var unique = "";
                                try
                                {
                                    var meta = template.Matches[piece.index].MetaData;
                                    if (template.Matches[piece.index].Unique) unique = " unique";
                                    rid = meta.EscapedIdentifier;
                                }
                                catch { }
                                string path = GetLinkToFolder(new List<string>() { AssetsFolderName, name + "s" }, location) + rid.Replace(':', '-') + ".html?pos=" + piece.position;
                                if (aligned[i].Length > block * blocklength + offset)
                                {
                                    // Get the block of sequence for this piece, determine if there are leading or trailing spaces and add empty text for those
                                    var seq = aligned[i].Substring(block * blocklength + offset, Math.Max(Math.Min(Math.Min(piece.length, aligned[i].Length - block * blocklength - offset), blocklength - offset), 0));
                                    var length = seq.Length;
                                    seq = seq.TrimStart(nonbreakingspace);
                                    if (length > seq.Length)
                                        alignblock.Append(string.Concat(Enumerable.Repeat("&nbsp;", length - seq.Length)));
                                    length = seq.Length;
                                    seq = seq.TrimEnd(nonbreakingspace);

                                    alignblock.Append($"<a href=\"{path}\" class=\"align-link{unique}\" onmouseover=\"AlignmentDetails({template.Matches[piece.index].Index})\" onmouseout=\"AlignmentDetailsClear()\">{seq}</a>");

                                    if (length > seq.Length)
                                        alignblock.Append(string.Concat(Enumerable.Repeat("&nbsp;", length - seq.Length)));
                                }
                                offset = piece.length;
                            }
                            alignblock.Append("</span>");
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

            var unique = "";
            if (template.ForcedOnSingleTemplate) unique = "<tr><td>Unique</td><td>" + (match.Unique ? "Yes" : "No") + "</td></tr>";

            var peaks = "";
            if (match.MetaData is MetaData.Peaks p) peaks = $"<tr><td>Peaks ALC</td><td>{p.DeNovoScore}</td></tr>";

            var docplot = "";
            if (match.MetaData.PositionalScore.Count() != 0) docplot = $"<tr><td>{doctitle}</td><td class='docplot'>{HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(match.MetaData.PositionalScore.SubArray(match.StartQueryPosition, match.TotalMatches).Select(a => (double)a).ToList(), match.StartQueryPosition))}</td></tr>";

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
            {peaks}
            {docplot}
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
            const int height = 25;
            const int fontsize = 10;
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

                bool placed = false;
                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    if ((double)item.Value / sum > threshold)
                    {
                        var size = (item.Value / sum * height / fontsize * 0.75).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        sequence_logo_buffer.Append($"<span style='font-size:{size}em'>{item.Key}</span>");
                        placed = true;
                    }
                }
                if (!placed)
                    sequence_logo_buffer.Append($"<span style='font-size:{fontsize / 10}em'>_</span>");

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
                    case AsideType.Read: SaveAside(CreateReadAside(index1), AsideType.Read, -1, -1, index1); break;
                    case AsideType.Template: SaveTemplateAside(Parameters.Segments[index3].Item2[index2].Templates[index1], AsideType.Template, index3, index2, index1); break;
                    case AsideType.RecombinedTemplate: SaveTemplateAside(Parameters.RecombinedSegment[index3].Templates[index1], AsideType.RecombinedTemplate, index3, index2, index1); break;
                };
            }

            // Read Asides
            for (int i = 0; i < reads.Count(); i++)
            {
                jobbuffer.Add((AsideType.Read, -1, -1, i));
            }
            // Template Tables Asides
            if (Parameters.Segments != null)
            {
                for (int i = 0; i < Parameters.Segments.Count(); i++)
                    for (int j = 0; j < Parameters.Segments[i].Item2.Count(); j++)
                        for (int k = 0; k < Parameters.Segments[i].Item2[j].Templates.Count(); k++)
                            jobbuffer.Add((AsideType.Template, i, j, k));
            }
            // Recombination Table Asides
            if (Parameters.RecombinedSegment != null)
            {
                for (int i = 0; i < Parameters.RecombinedSegment.Count(); i++)
                    for (int j = 0; j < Parameters.RecombinedSegment[i].Templates.Count(); j++)
                        jobbuffer.Add((AsideType.RecombinedTemplate, i, -1, j));
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

        /// <summary>An enum to save what type of detail aside it is.</summary>
        enum AsideType { Read, Template, RecombinedTemplate }
        string GetAsidePrefix(AsideType type)
        {
            switch (type)
            {
                case AsideType.Read:
                    return "R";
                case AsideType.Template:
                    return "T";
                case AsideType.RecombinedTemplate:
                    return "RC";
            }
            throw new ArgumentException("Invalid AsideType in GetAsidePrefix.");
        }
        string GetAsideName(AsideType type)
        {
            switch (type)
            {
                case AsideType.Read:
                    return "read";
                case AsideType.Template:
                    return "template";
                case AsideType.RecombinedTemplate:
                    return "recombined-template";
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
                metadata = reads_metadata[index3];
            else if (type == AsideType.Template)
                metadata = Parameters.Segments[index1].Item2[index2].Templates[index3].MetaData;
            else if (type == AsideType.RecombinedTemplate)
                metadata = Parameters.RecombinedSegment[index1].Templates[index3].MetaData;

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

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideIdentifier(MetaData.IMetaData metadata, bool humanvisible = false)
        {
            if (humanvisible) return metadata.Identifier;
            else return metadata.EscapedIdentifier;
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

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        string GetAsideLink(MetaData.IMetaData metadata, AsideType type, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(metadata);
            if (id == null) throw new Exception("ID is null");
            string classname = GetAsideName(type);
            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, classname + "s" }, location) + id.Replace(':', '-') + ".html";
            return $"<a href=\"{path}\" class=\"info-link {classname}-link\">{ GetAsideIdentifier(metadata, true)}</a>";
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

        string PointsTableHeader(List<Template> templates, string identifier)
        {
            if (templates.Select(a => a.Score).Sum() == 0)
            {
                return "";
            }

            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            var data = new List<(string, List<(string, List<double>)>)>();

            foreach (var template in templates)
            {
                var group = data.FindIndex(a => a.Item1 == template.MetaData.ClassIdentifier);
                if (group == -1)
                {
                    data.Add((template.MetaData.ClassIdentifier, new List<(string, List<double>)>()));
                    group = data.Count() - 1;
                }
                if (displayUnique)
                    data[group].Item2.Add((template.MetaData.Identifier, new List<double> { template.Score, template.Matches.Count(), template.TotalArea, template.UniqueScore, template.UniqueMatches, template.TotalUniqueArea }));
                else
                    data[group].Item2.Add((template.MetaData.Identifier, new List<double> { template.Score, template.Matches.Count(), template.TotalArea }));
            }

            List<string> header;
            if (displayUnique)
                header = new List<string> { "Score", "Matches", "Area", "UniqueScore", "UniqueMatches", "UniqueArea" };
            else
                header = new List<string> { "Score", "Matches", "Area" };

            return $@"
<div class='table-header'><div>
    {HTMLGraph.GroupedPointGraph(data, header, identifier)}</div></div>";
        }

        string TableHeader(List<Template> templates, string groupIdentifier = "")
        {
            if (templates.Count() > 15) return PointsTableHeader(templates, groupIdentifier);
            if (templates.Count() > 5) return BarTableHeader(templates);
            return $"";
        }

        string BarTableHeader(List<Template> templates)
        {
            if (templates.Select(a => a.Score).Sum() == 0)
            {
                return "";
            }

            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);

            var typedata = new Dictionary<string, (double MaxScore, double TotalScore, double UniqueMaxScore, double UniqueTotalScore, int Num, int Matches, int UniqueMatches, double Area, double UniqueArea)>(templates.Count);
            foreach (var item in templates)
            {
                if (typedata.ContainsKey(item.Class))
                {
                    var data = typedata[item.Class];
                    if (data.MaxScore < item.Score) data.MaxScore = item.Score;
                    data.TotalScore += item.Score;
                    if (data.UniqueMaxScore < item.Score) data.UniqueMaxScore = item.UniqueScore;
                    data.UniqueTotalScore += item.UniqueScore;
                    data.Num += 1;
                    data.Matches += item.Matches.Count();
                    data.UniqueMatches += item.UniqueMatches;
                    data.Area += item.TotalArea;
                    data.UniqueArea += item.TotalUniqueArea;
                    typedata[item.Class] = data;
                }
                else
                {
                    typedata.Add(item.Class, (item.Score, item.Score, item.UniqueScore, item.UniqueScore, 1, item.Matches.Count(), item.UniqueMatches, item.TotalArea, item.TotalUniqueArea));
                }
            }

            var scoreData = new List<(string, List<double>)>(typedata.Count);
            var areaData = new List<(string, List<double>)>(typedata.Count);
            foreach (var (type, data) in typedata)
            {
                var scoreList = new List<double> { data.MaxScore, data.TotalScore / data.Num };
                var areaList = new List<double> { data.Matches, data.Area };
                if (displayUnique)
                {
                    scoreList.Add(data.UniqueMaxScore);
                    scoreList.Add(data.UniqueTotalScore / data.Num);
                    areaList.Add(data.UniqueMatches);
                    areaList.Add(data.UniqueArea);
                }
                scoreData.Add((type, scoreList));
                areaData.Add((type, areaList));
            }

            var scoreLabels = new List<(string, uint)> { ("Max Score", 0), ("Average Score", 0) };
            var areaLabels = new List<(string, uint)> { ("Matches", 0), ("Total Area", 1) };
            if (displayUnique)
            {
                scoreLabels.Add(("Unique Max Score", 0));
                scoreLabels.Add(("Unique Average Score", 0));
                areaLabels.Add(("Unique Matches", 0));
                areaLabels.Add(("Unique Total Area", 1));
            }

            return $@"
<div class='table-header full'>
    <div>
    <h3>Scores per type</h3>
    {HTMLGraph.GroupedBargraph(scoreData, scoreLabels)}
</div>
<div>
    <h3>Area per type</h3>
    {HTMLGraph.GroupedBargraph(areaData, areaLabels)}
</div></div>";
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
ReadPrefix = '{GetAsidePrefix(AsideType.Read)}';
TemplatePrefix = '{GetAsidePrefix(AsideType.Template)}';
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
                            buffer.Append(GetAsideLink(seg.Templates[0].MetaData, AsideType.Template));
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
                        buffer.Append($"<h3>{seg.Name}</h3>{TableHeader(seg.Templates, seg.Name)}");
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
                        groupbuffer += Collapsible("Recombination Table", CreateTemplateTable(recombined, -1, group, AsideType.RecombinedTemplate, true), group.ToString());
                        if (decoy.Count() > 0)
                            groupbuffer += Collapsible("Recombination Decoy", CreateTemplateTable(decoy, -1, group, AsideType.RecombinedTemplate, true), group.ToString() + "_decoy");
                    }

                    groupbuffer += CreateTemplateTables(Parameters.Segments[group].Item2, group);

                    if (Parameters.Segments.Count() == 1)
                        innerbuffer.Append(groupbuffer);
                    else
                        innerbuffer.Append(Collapsible(Parameters.Segments[group].Item1, groupbuffer));
                }

            innerbuffer.Append(Collapsible("Reads Table", CreateReadsTable()));
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