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
using AssemblyNameSpace;
using static HTMLNameSpace.CommonPieces;
using System.Collections.ObjectModel;

namespace HTMLNameSpace
{
    static class HTMLTables
    {
        static int table_counter = 0;

        /// <summary> Create HTML with all reads in a table. With annotations for sorting the table. </summary>
        /// <returns> Returns an HTML string. </returns>
        public static string CreateReadsTable(ReadOnlyCollection<(string, ReadMetaData.IMetaData)> reads, string AssetsFolderName)
        {
            var buffer = new StringBuilder();

            TableHeader(buffer, "reads", reads.Select(a => (double)a.Item1.Length));

            buffer.AppendLine(@"<table id=""reads-table"" class=""widetable"">
<tr>
    <th onclick=""sortTable('reads-table', 0, 'string')"" class=""smallcell"">Identifier</th>
    <th onclick=""sortTable('reads-table', 1, 'string')"">Sequence</th>
    <th onclick=""sortTable('reads-table', 2, 'number')"" class=""smallcell"">Sequence Length</th>
</tr>");
            string id, link;

            for (int i = 0; i < reads.Count; i++)
            {
                id = GetAsideIdentifier(reads[i].Item2);
                link = GetAsideLink(reads[i].Item2, AsideType.Read, AssetsFolderName);
                buffer.AppendLine($@"<tr id=""reads-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{reads[i].Item1}</td>
    <td class=""center"">{reads[i].Item1.Length}</td>
</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        public static string CreateTemplateTables(List<Segment> segments, string AssetsFolderName, int total_reads)
        {
            var output = new StringBuilder();

            for (var i = 0; i < segments.Count; i++)
            {
                var item = segments[i];
                output.Append(Collapsible(item.Name, $"Segment {item.Name}", CreateSegmentTable(item.Name, item.Templates, item.ScoreHierarchy, AsideType.Template, AssetsFolderName, total_reads, true)));
            }

            return output.ToString();
        }

        public static string CreateSegmentTable(string name, List<Template> templates, PhylogeneticTree.ProteinHierarchyTree tree, AsideType type, string AssetsFolderName, int total_reads, bool header = false)
        {
            table_counter++;
            var buffer = new StringBuilder();
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            bool displayOrder = templates.Exists(a => a.Recombination != null);
            int order_factor = displayOrder ? 1 : 0;
            var table_id = $"table-{table_counter}";

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            string SortOn(int column, string type)
            {
                if (templates.Count > 1)
                    return $"onclick=\"sortTable('{table_id}', {column}, '{type}')\" ";
                else
                    return "";
            }

            if (header)
                TableHeader(buffer, templates, total_reads);

            if (tree != null)
                buffer.AppendLine(Collapsible(name + "-tree", "Tree", HTMLGraph.RenderTree($"tree-{table_counter}", tree, templates, type, AssetsFolderName), CollapsibleState.Open));


            string unique = "";
            if (displayUnique) unique =
                CommonPieces.TagWithHelp("th", "Unique Score", HTMLHelp.TemplateUniqueScore, "smallcell", SortOn(5 + order_factor, "number")) +
                CommonPieces.TagWithHelp("th", "Unique Matches", HTMLHelp.TemplateUniqueMatches, "smallcell", SortOn(6 + order_factor, "number")) +
                CommonPieces.TagWithHelp("th", "Unique Area", HTMLHelp.TemplateUniqueArea, "smallcell", SortOn(7 + order_factor, "number"));

            string order = "";
            if (displayOrder) order = CommonPieces.TagWithHelp("th", "Order", HTMLHelp.Order, "smallcell", SortOn(2, "id"));

            var table_buffer = new StringBuilder();
            table_buffer.AppendLine($@"<table id=""{table_id}"" class=""widetable"">
<tr>
    {CommonPieces.TagWithHelp("th", "Identifier", HTMLHelp.TemplateIdentifier, "smallcell", SortOn(0, "id"))}
    {CommonPieces.TagWithHelp("th", "Length", HTMLHelp.TemplateLength, "smallcell", SortOn(1, "number"))}
    {order}
    {CommonPieces.TagWithHelp("th", "Score", HTMLHelp.TemplateScore, "smallcell", SortOn(2 + order_factor, "number") + " data-sortorder='desc'")}
    {CommonPieces.TagWithHelp("th", "Matches", HTMLHelp.TemplateMatches, "smallcell", SortOn(3 + order_factor, "number"))}
    {CommonPieces.TagWithHelp("th", "Total Area", HTMLHelp.TemplateTotalArea, "smallcell", SortOn(4 + order_factor, "number"))}
    {unique}
</tr>");

            (double, double, double, double, double, double) max_values = (Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue);
            foreach (var template in templates)
            {
                max_values = (
                    Math.Max(max_values.Item1, template.Score),
                    Math.Max(max_values.Item2, template.Matches.Count),
                    Math.Max(max_values.Item3, template.TotalArea),
                    Math.Max(max_values.Item4, template.UniqueScore),
                    Math.Max(max_values.Item5, template.UniqueMatches),
                    Math.Max(max_values.Item6, template.TotalUniqueArea)
                    );
            }

            string id, link;
            var doc_buffer = new StringBuilder();
            for (int i = 0; i < templates.Count; i++)
            {
                id = GetAsideIdentifier(templates[i].MetaData);
                link = GetAsideLink(templates[i].MetaData, type, AssetsFolderName);
                if (displayUnique) unique = $@"
<td class='center bar' style='--relative-value:{templates[i].UniqueScore / max_values.Item4}'>{templates[i].UniqueScore}</td>
<td class='center bar' style='--relative-value:{templates[i].UniqueMatches / max_values.Item5}'>{templates[i].UniqueMatches}</td>
<td class='center bar' style='--relative-value:{templates[i].TotalUniqueArea / max_values.Item6}'>{templates[i].TotalUniqueArea:G3}</td>
";
                if (displayOrder)
                {
                    var order_html = templates[i].Recombination.Aggregate(
                            "",
                            (acc, seg) => acc + " → " + GetAsideLink(seg.MetaData, AsideType.Template, AssetsFolderName)
                        ).Substring(3);
                    order = $"<td>{order_html}</td>";
                }
                table_buffer.AppendLine($@"<tr id='{table_id}-{id}'>
    <td class='center'>{link}</td>
    <td class='center'>{templates[i].Sequence.Length}</td>
    {order}
    <td class='center bar' style='--relative-value:{templates[i].Score / max_values.Item1}'>{templates[i].Score}</td>
    <td class='center bar' style='--relative-value:{templates[i].Matches.Count / max_values.Item2}'>{templates[i].Matches.Count}</td>
    <td class='center bar' style='--relative-value:{templates[i].TotalArea / max_values.Item3}'>{templates[i].TotalArea:G3}</td>
    {unique}
</tr>");
                doc_buffer.Append($"<div class='docplot'>");
                HTMLGraph.Bargraph(doc_buffer, HTMLGraph.AnnotateDOCData(templates[i].ConsensusSequence().Item2), GetAsideLink(templates[i].MetaData, type, AssetsFolderName), null, null, 10, templates[i].ConsensusSequenceAnnotation());
                doc_buffer.AppendLine("</div>");
            }

            table_buffer.AppendLine("</table>");
            buffer.AppendLine(Collapsible(name + "-table", "Table", table_buffer.ToString(), templates.Count < 5 ? CollapsibleState.Open : CollapsibleState.Closed));
            buffer.AppendLine(Collapsible(name + "-docs", "Depth Of Coverage Overview", doc_buffer.ToString(), templates.Count < 5 ? CollapsibleState.Open : CollapsibleState.Closed));
            CultureInfo.CurrentCulture = culture;

            return buffer.ToString();
        }

        public static void CDRTable(StringBuilder buffer, List<(ReadMetaData.IMetaData MetaData, ReadMetaData.IMetaData Template, string Sequence, bool Unique)> cdrs, string AssetsFolderName, string title, int total_reads, int total_templates)
        {
            table_counter++;
            var table_id = $"table-{table_counter}";

            buffer.AppendLine($"<div class='cdr-group'><h2>{title}</h2><div class='table-header-columns'>");

            var matched = cdrs.Select(a => a.MetaData.EscapedIdentifier).Distinct().Count();
            var templates = cdrs.Select(a => a.Template.EscapedIdentifier).Distinct().Count();
            var unique = cdrs.Where(a => a.Unique == true).Select(a => a.MetaData.EscapedIdentifier).Distinct().Count();

            buffer.Append($"<p class='text-header'>{matched} ({(double)matched / total_reads:P2} of all input reads) distinct reads were matched on {templates} ({(double)templates / total_templates:P2} of all templates with CDRs) distinct templates, of these {unique} ({(double)unique / matched:P2} of all matched reads) were matched uniquely (on a single template).</p><p>Consensus: ");

            var diversity = new List<Dictionary<string, double>>();

            foreach (var row in cdrs)
            {
                for (int i = 0; i < row.Sequence.Length; i++)
                {
                    if (i >= diversity.Count) diversity.Add(new Dictionary<string, double>());
                    if (row.Sequence[i] != Alphabet.GapChar)
                    {
                        if (diversity[i].ContainsKey(row.Sequence[i].ToString()))
                            diversity[i][row.Sequence[i].ToString()] += 1;
                        else
                            diversity[i].Add(row.Sequence[i].ToString(), 1);
                    }
                }
            }

            SequenceConsensusOverview(buffer, diversity);

            string SortOn(int column, string type)
            {
                return $"onclick=\"sortTable('{table_id}', {column}, '{type}')\"";
            }

            buffer.AppendLine($@"</p></div><table id=""{table_id}"">
<tr>
    <th {SortOn(1, "id")} class=""smallcell"">Identifier</th>
    <th {SortOn(2, "string")} class=""smallcell"">Sequence</th>
    <th {SortOn(0, "id")} class=""smallcell"">Template</th>
</tr>");

            var cdrs_deduplicated = cdrs.GroupBy(cdr => cdr.Sequence).OrderBy(group => group.Key);

            foreach (var group in cdrs_deduplicated)
            {
                var row = group.First();
                var id = GetAsideIdentifier(row.MetaData);
                var link = group.Select(item => item.MetaData).Distinct().Aggregate("", (acc, item) => acc + GetAsideLink(item, AsideType.Template, AssetsFolderName));
                var link_template = group.Select(item => item.Template).Distinct().Aggregate("", (acc, item) => acc + GetAsideLink(item, AsideType.Template, AssetsFolderName));
                buffer.AppendLine($@"<tr id=""{table_id}-{id}"">
    <td class=""center"">{link}</td>
    <td class=""seq"">{row.Sequence.Replace('~', Alphabet.GapChar)}</td>
    <td class=""center"">{link_template}</td>
</tr>");
            }

            buffer.AppendLine("</table></div>");
        }

        public static void SequenceConsensusOverview(StringBuilder buffer, List<Dictionary<string, double>> diversity, string title = null, string help = null, HelperFunctionality.Annotation[] annotation = null)
        {
            const double threshold = 0.05;
            const int height = 35;
            const int fontsize = 30;

            var data_buffer = new StringBuilder();
            buffer.Append("<div class='graph'>");
            if (title != null)
                if (help != null)
                    buffer.Append(CommonPieces.TagWithHelp("h2", title, help));
                else
                    buffer.Append($"<h2 class='title'>{title}</h2>");
            if (title != null) // Bad way of only doing this in the asides and not in the CDR tables
                buffer.Append(CommonPieces.CopyData(title + " (TSV)", HTMLHelp.SequenceConsensusOverviewData));
            buffer.Append($"<div class='sequence-logo' style='--sequence-logo-height:{height}px;--sequence-logo-fontsize:{fontsize}px;'>");
            for (int i = 0; i < diversity.Count; i++)
            {
                var Class = annotation != null && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                buffer.Append($"<div class='sequence-logo-position{Class}'>");

                double sum = diversity[i].Values.Sum();
                var sorted = diversity[i].ToList();
                sorted.Sort((a, b) => a.Value.CompareTo(b.Value));
                data_buffer.Append($"{i}");

                bool placed = false;
                foreach (var item in sorted)
                {
                    if (item.Key != "~" && (double)item.Value / sum > threshold)
                    {
                        var size = (item.Value / sum * fontsize).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        var inverse_size = (sum / item.Value).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        buffer.Append($"<span style='font-size:{size:G3}px;transform:scaleX({inverse_size:G3})'>{item.Key}</span>");
                        placed = true;
                    }
                    data_buffer.Append($"\t{item.Key}\t{item.Value.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB")):G3}");
                }
                if (!placed)
                    buffer.Append($"<span>.</span>");

                buffer.Append("</div>");
                data_buffer.Append("\n");
            }
            buffer.Append("</div>");
            if (title == null) // Bad way of only doing this in the CDR tables and not in the asides
                buffer.Append(CopyData("Sequence Consensus Overview (TSV)", HTMLHelp.SequenceConsensusOverviewData));
            buffer.Append($"<textarea type='text' class='graph-data' aria-hidden='true'>{data_buffer.ToString()}</textarea></div>");
        }

        public static void TableHeader(StringBuilder buffer, List<Template> templates, int totalReads)
        {
            buffer.Append("<div class='table-header'>");
            if (templates.Count > 15) PointsTableHeader(buffer, templates);
            else if (templates.Count > 5) BarTableHeader(buffer, templates);
            TextTableHeader(buffer, templates, totalReads);
            buffer.Append("</div>");
        }

        static void TableHeader(StringBuilder buffer, string identifier, IEnumerable<double> lengths, IEnumerable<double> area = null)
        {
            buffer.Append($"<div class='table-header-{identifier}'>");
            HTMLGraph.Histogram(buffer, lengths.ToList(), "Length distribution");
            if (area != null)
            {
                buffer.Append($"</div><div>");
                HTMLGraph.Histogram(buffer, area.ToList(), "Area distribution");
            }
            buffer.Append("</div>");
        }

        static void PointsTableHeader(StringBuilder buffer, List<Template> templates)
        {
            if (templates.Select(a => a.Score).Sum() == 0)
                return;

            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            var data = new List<(string, List<(string, List<double>)>)>();

            foreach (var template in templates)
            {
                var group = data.FindIndex(a => a.Item1 == template.MetaData.ClassIdentifier);
                if (group == -1)
                {
                    data.Add((template.MetaData.ClassIdentifier, new List<(string, List<double>)>()));
                    group = data.Count - 1;
                }
                if (displayUnique)
                    data[group].Item2.Add((template.MetaData.Identifier, new List<double> { template.Score, template.Matches.Count, template.TotalArea, template.UniqueScore, template.UniqueMatches, template.TotalUniqueArea }));
                else
                    data[group].Item2.Add((template.MetaData.Identifier, new List<double> { template.Score, template.Matches.Count, template.TotalArea }));
            }

            List<string> header;
            if (displayUnique)
                header = new List<string> { "Score", "Matches", "Area", "UniqueScore", "UniqueMatches", "UniqueArea" };
            else
                header = new List<string> { "Score", "Matches", "Area" };

            HTMLGraph.GroupedPointGraph(buffer, data, header, "Overview of scores", HTMLHelp.OverviewOfScores, null);
        }

        static void BarTableHeader(StringBuilder buffer, List<Template> templates)
        {
            if (templates.Select(a => a.Score).Sum() == 0)
                return;

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
                    data.Matches += item.Matches.Count;
                    data.UniqueMatches += item.UniqueMatches;
                    data.Area += item.TotalArea;
                    data.UniqueArea += item.TotalUniqueArea;
                    typedata[item.Class] = data;
                }
                else
                {
                    typedata.Add(item.Class, (item.Score, item.Score, item.UniqueScore, item.UniqueScore, 1, item.Matches.Count, item.UniqueMatches, item.TotalArea, item.TotalUniqueArea));
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

            buffer.Append("<div>");
            HTMLGraph.GroupedBargraph(buffer, scoreData, scoreLabels, "Scores per group");
            buffer.Append("</div><div>");
            HTMLGraph.GroupedBargraph(buffer, areaData, areaLabels, "Area per group");
            buffer.Append("</div>");
        }

        static void TextTableHeader(StringBuilder buffer, List<Template> templates, int total_reads)
        {
            var set = new HashSet<string>();
            var unique_set = new HashSet<string>();
            foreach (var template in templates)
            {
                set.UnionWith(template.Matches.Select(a => a.MetaData.EscapedIdentifier));
                unique_set.UnionWith(template.Matches.Where(a => a.Unique == true).Select(a => a.MetaData.EscapedIdentifier));
            }
            buffer.Append($"<p class='text-header'>Reads matched {set.Count} ({(double)set.Count / total_reads:P2} of all input reads) of these {unique_set.Count} ({(double)unique_set.Count / set.Count:P2} of all matched reads) were matched uniquely.</p>");
        }
    }
}