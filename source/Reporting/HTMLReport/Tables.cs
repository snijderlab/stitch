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
using static HTMLNameSpace.Common;

namespace HTMLNameSpace
{
    static class HTMLTables
    {
        /// <summary> Create HTML with all reads in a table. With annotations for sorting the table. </summary>
        /// <returns> Returns an HTML string. </returns>
        public static string CreateReadsTable(List<(string, MetaData.IMetaData)> reads, string AssetsFolderName)
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

            for (int i = 0; i < reads.Count(); i++)
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

        public static string CreateTemplateTables(List<Segment> segments, string AssetsFolderName)
        {
            var output = new StringBuilder();

            for (var i = 0; i < segments.Count(); i++)
            {
                var item = segments[i];
                output.Append(Collapsible($"Segment {item.Name}", CreateTemplateTable(item.Templates, AsideType.Template, AssetsFolderName, true)));
            }

            return output.ToString();
        }


        static int table_counter = 0;
        public static string CreateTemplateTable(List<Template> templates, AsideType type, string AssetsFolderName, bool header = false)
        {
            table_counter++;
            var buffer = new StringBuilder();
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            var table_id = $"table-{table_counter}";

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            string SortOn(int column, string type)
            {
                if (templates.Count() > 1)
                    return $"onclick=\"sortTable('{table_id}', {column}, '{type}')\" ";
                else
                    return "";
            }

            if (header)
                TableHeader(buffer, templates);

            string unique = "";
            if (displayUnique) unique = $@"
<th {SortOn(5, "number")}class=""smallcell"">Unique Score</th>
<th {SortOn(6, "number")}class=""smallcell"">Unique Matches</th>
<th {SortOn(7, "number")}class=""smallcell"">Unique Area</th>
";

            buffer.AppendLine($@"<table id=""{table_id}"" class=""widetable"">
<tr>
    <th {SortOn(0, "id")}class=""smallcell"">Identifier</th>
    <th {SortOn(1, "number")}class=""smallcell"">Length</th>
    <th {SortOn(2, "number")}class=""smallcell"" data-sortorder=""desc"">Score</th>
    <th {SortOn(3, "number")}class=""smallcell"">Matches</th>
    <th {SortOn(4, "number")}class=""smallcell"">Total Area</th>
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
                link = GetAsideLink(templates[i].MetaData, type, AssetsFolderName);
                if (displayUnique) unique = $@"
<td class=""center bar"" style=""--relative-value:{templates[i].UniqueScore / max_values.Item4}"">{templates[i].UniqueScore}</td>
<td class=""center bar"" style=""--relative-value:{templates[i].UniqueMatches / max_values.Item5}"">{templates[i].UniqueMatches}</td>
<td class=""center bar"" style=""--relative-value:{templates[i].TotalUniqueArea / max_values.Item6}"">{templates[i].TotalUniqueArea.ToString("G3")}</td>
";
                buffer.AppendLine($@"<tr id=""{table_id}-{id}"">
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
        static void TableHeader(StringBuilder buffer, string identifier, IEnumerable<double> lengths, IEnumerable<double> area = null)
        {
            buffer.Append($"<div class='table-header-{identifier}'><div><h3>Length</h3>");
            HTMLGraph.Histogram(buffer, lengths.ToList());
            if (area != null)
            {
                buffer.Append($"</div><div><h3>Area</h3>");
                HTMLGraph.Histogram(buffer, area.ToList());
            }
            buffer.Append("</div></div>");
        }

        public static void TableHeader(StringBuilder buffer, List<Template> templates)
        {
            if (templates.Count() > 15) PointsTableHeader(buffer, templates);
            if (templates.Count() > 5) BarTableHeader(buffer, templates);
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

            buffer.Append("<div class='table-header'><div>");
            HTMLGraph.GroupedPointGraph(buffer, data, header);
            buffer.Append("</div></div>");
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

            buffer.Append("<div class='table-header full'><div><h3>Scores per type</h3>");
            HTMLGraph.GroupedBargraph(buffer, scoreData, scoreLabels);
            buffer.Append("</div><div><h3>Area per type</h3>");
            HTMLGraph.GroupedBargraph(buffer, areaData, areaLabels);
            buffer.Append("</div></div>");
        }
    }
}