using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Stitch;
using static HTMLNameSpace.CommonPieces;
using System.Collections.ObjectModel;
using HtmlGenerator;

namespace HTMLNameSpace {
    static class HTMLTables {
        static int table_counter = 0;

        /// <summary> Create HTML with all reads in a table. With annotations for sorting the table. </summary>
        /// <returns> Returns an HTML string. </returns>
        public static HtmlBuilder CreateReadsTable(ReadOnlyCollection<ReadFormat.General> reads, string AssetsFolderName) {
            var html = new HtmlBuilder();

            html.Add(TableHeader("reads", reads.Select(a => (double)a.Sequence.Length)));
            html.Open(HtmlTag.table, "id='reads-table' class='wide-table'");
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.th, "onclick='sortTable(\"reads-table\", 0, \"string\")' class='small-cell'", "Identifier");
            html.OpenAndClose(HtmlTag.th, "onclick='sortTable(\"reads-table\", 1, \"string\")'", "Sequence");
            html.OpenAndClose(HtmlTag.th, "onclick='sortTable(\"reads-table\", 2, \"number\")' class='small-cell'", "Sequence Length");
            html.Close(HtmlTag.tr);

            string id;

            for (int i = 0; i < reads.Count; i++) {
                id = GetAsideIdentifier(reads[i]);
                html.Open(HtmlTag.tr, $"id='reads-{id}'");
                html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(reads[i], AsideType.Read, AssetsFolderName));
                html.OpenAndClose(HtmlTag.td, "class='seq'", AminoAcid.ArrayToString(reads[i].Sequence.AminoAcids));
                html.OpenAndClose(HtmlTag.td, "class='center'", reads[i].Sequence.Length.ToString());
                html.Close(HtmlTag.tr);
            }

            html.Close(HtmlTag.table);

            return html;
        }

        public static HtmlBuilder CreateTemplateTables(List<Segment> segments, string AssetsFolderName, int total_reads) {
            var html = new HtmlBuilder();

            for (var i = 0; i < segments.Count; i++) {
                var item = segments[i];
                html.Collapsible(item.Name, new HtmlBuilder($"Segment {item.Name}"), CreateSegmentTable(item.Name, item.Templates, item.ScoreHierarchy, AsideType.Template, AssetsFolderName, total_reads, true));
            }

            return html;
        }

        public static HtmlBuilder CreateSegmentTable(string name, List<Template> templates, PhylogeneticTree.ProteinHierarchyTree tree, AsideType type, string AssetsFolderName, int total_reads, bool header = false) {
            table_counter++;
            var html = new HtmlBuilder();
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            bool displayOrder = templates.Exists(a => a.Recombination != null);
            bool displayArea = templates.Any(item => item.TotalArea > 0 || item.TotalUniqueArea > 0);
            int order_factor = displayOrder ? 1 : 0;
            var table_id = $"table-{table_counter}";

            templates.Sort((a, b) => b.Score.CompareTo(a.Score));

            string SortOn(int column, string type) {
                if (templates.Count > 1)
                    return $"onclick=\"sortTable('{table_id}', {column}, '{type}')\" ";
                else
                    return "";
            }

            if (header)
                html.Add(TableHeader(templates, total_reads));

            if (tree != null)
                html.Collapsible(name + "-tree", new HtmlBuilder("Tree"), HTMLGraph.RenderTree($"tree-{table_counter}", tree, templates, type, AssetsFolderName, displayArea), HtmlBuilder.CollapsibleState.Open);

            var table_buffer = new HtmlBuilder();
            table_buffer.Open(HtmlTag.table, $"id='{table_id}' class='wide-table'");
            table_buffer.Open(HtmlTag.tr);
            table_buffer.TagWithHelp(HtmlTag.th, "Identifier", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateIdentifier), "small-cell", SortOn(0, "id"));
            table_buffer.TagWithHelp(HtmlTag.th, "Length", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateLength), "small-cell", SortOn(1, "number"));
            if (displayOrder) table_buffer.TagWithHelp(HtmlTag.th, "Order", new HtmlBuilder(HtmlTag.p, HTMLHelp.Order), "small-cell", SortOn(2, "id"));
            table_buffer.TagWithHelp(HtmlTag.th, "Score", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateScore), "small-cell", SortOn(2 + order_factor, "number") + " data-sort-order='desc'");
            table_buffer.TagWithHelp(HtmlTag.th, "Matches", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateMatches), "small-cell", SortOn(3 + order_factor, "number"));
            if (displayArea) table_buffer.TagWithHelp(HtmlTag.th, "Total Area", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateTotalArea), "small-cell", SortOn(4 + order_factor, "number"));
            if (displayUnique) {
                table_buffer.TagWithHelp(HtmlTag.th, "Unique Score", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateUniqueScore), "small-cell", SortOn(5 + order_factor, "number"));
                table_buffer.TagWithHelp(HtmlTag.th, "Unique Matches", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateUniqueMatches), "small-cell", SortOn(6 + order_factor, "number"));
                if (displayArea) table_buffer.TagWithHelp(HtmlTag.th, "Unique Area", new HtmlBuilder(HtmlTag.p, HTMLHelp.TemplateUniqueArea), "small-cell", SortOn(7 + order_factor, "number"));
            }
            table_buffer.Close(HtmlTag.tr);

            (double, double, double, double, double, double) max_values = (Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue, Double.MinValue);
            foreach (var template in templates) {
                max_values = (
                    Math.Max(max_values.Item1, template.Score),
                    Math.Max(max_values.Item2, template.Matches.Count),
                    Math.Max(max_values.Item3, template.TotalArea),
                    Math.Max(max_values.Item4, template.UniqueScore),
                    Math.Max(max_values.Item5, template.UniqueMatches),
                    Math.Max(max_values.Item6, template.TotalUniqueArea)
                    );
            }

            string id;
            var doc_buffer = new HtmlBuilder();
            for (int i = 0; i < templates.Count; i++) {
                id = GetAsideIdentifier(templates[i].MetaData);

                table_buffer.Open(HtmlTag.tr, $"id='{table_id}-{id}'");
                table_buffer.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(templates[i].MetaData, type, AssetsFolderName));
                table_buffer.OpenAndClose(HtmlTag.td, "class='center'", templates[i].Sequence.Length.ToString());
                if (displayOrder) {
                    table_buffer.Open(HtmlTag.td);
                    var first = true;
                    foreach (var seg in templates[i].Recombination) {
                        if (!first) table_buffer.Content(" → ");
                        first = false;
                        table_buffer.Add(GetAsideLinkHtml(seg.MetaData, AsideType.Template, AssetsFolderName));
                    }
                    table_buffer.Close(HtmlTag.td);
                }
                double vd(double v1, double v2) {
                    return (v2 == 0 ? 0 : v1 / v2);
                }
                table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].Score, max_values.Item1)}'", templates[i].Score.ToString("G4"));
                table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].Matches.Count, max_values.Item2)}'", templates[i].Matches.Count.ToString("G4"));
                if (displayArea) table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].TotalArea, max_values.Item3)}'", templates[i].TotalArea.ToString("G4"));
                if (displayUnique) {
                    table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].UniqueScore, max_values.Item4)}'", templates[i].UniqueScore.ToString("G4"));
                    table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].UniqueMatches, max_values.Item5)}'", templates[i].UniqueMatches.ToString("G4"));
                    if (displayArea) table_buffer.OpenAndClose(HtmlTag.td, $"class='center bar' style='--relative-value:{vd(templates[i].TotalUniqueArea, max_values.Item6)}'", templates[i].TotalUniqueArea.ToString("G4"));
                }
                table_buffer.Close(HtmlTag.tr);

                doc_buffer.Open(HtmlTag.div, "class='doc-plot'");
                doc_buffer.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(templates[i].ConsensusSequence().Item2), GetAsideLinkHtml(templates[i].MetaData, type, AssetsFolderName), null, null, 10, templates[i].ConsensusSequenceAnnotation(), templates[i].MetaData.Identifier));
                doc_buffer.Close(HtmlTag.div);
            }

            table_buffer.Close(HtmlTag.table);
            html.Collapsible(name + "-table", new HtmlBuilder("Table"), table_buffer, templates.Count < 5 ? HtmlBuilder.CollapsibleState.Open : HtmlBuilder.CollapsibleState.Closed);
            html.Collapsible(name + "-docs", new HtmlBuilder("Depth Of Coverage Overview"), doc_buffer, templates.Count < 5 ? HtmlBuilder.CollapsibleState.Open : HtmlBuilder.CollapsibleState.Closed);
            CultureInfo.CurrentCulture = culture;

            return html;
        }

        public static HtmlBuilder CDRTable(List<(ReadFormat.General MetaData, ReadFormat.General Template, string Sequence, bool Unique)> CDRs, string AssetsFolderName, string title, int total_reads, int total_templates) {
            table_counter++;
            var table_id = $"table-{table_counter}";

            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='cdr-group'");
            html.OpenAndClose(HtmlTag.h2, "", title);
            html.Open(HtmlTag.div, "class='table-header-columns'");

            var matched = CDRs.Select(a => a.MetaData.EscapedIdentifier).Distinct().Count();
            var templates = CDRs.Select(a => a.Template.EscapedIdentifier).Distinct().Count();
            var unique = CDRs.Where(a => a.Unique == true).Select(a => a.MetaData.EscapedIdentifier).Distinct().Count();

            html.OpenAndClose(HtmlTag.p, "class='text-header'", $"{matched} ({(double)matched / total_reads:P2} of all input reads) distinct reads were matched on {templates} ({(double)templates / total_templates:P2} of all templates with CDRs) distinct templates, of these {unique} ({(double)unique / matched:P2} of all matched reads) were matched uniquely (on a single template). The consensus sequence is shown below.");
            html.Open(HtmlTag.p);

            var diversity = new List<Dictionary<(string, int), double>>();

            foreach (var row in CDRs) {
                for (int i = 0; i < row.Sequence.Length; i++) {
                    if (i >= diversity.Count) diversity.Add(new Dictionary<(string, int), double>());
                    if (row.Sequence[i] != '.') { // TODO: it cannot know the correct gap char here
                        if (diversity[i].ContainsKey((row.Sequence[i].ToString(), 1)))
                            diversity[i][(row.Sequence[i].ToString(), 1)] = diversity[i][(row.Sequence[i].ToString(), 1)] + 1;
                        else
                            diversity[i].Add((row.Sequence[i].ToString(), 1), 1);
                    }
                }
            }

            html.Add(SequenceConsensusOverview(diversity));

            string SortOn(int column, string type) {
                return $"onclick=\"sortTable('{table_id}', {column}, '{type}')\"";
            }
            html.Close(HtmlTag.p);
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.table, $"id='{table_id}'");
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.th, $"{SortOn(1, "id")} class='small-cell'", "Identifier");
            html.OpenAndClose(HtmlTag.th, $"{SortOn(2, "string")} class='small-cell'", "Sequence");
            html.OpenAndClose(HtmlTag.th, $"{SortOn(0, "id")} class='small-cell'", "Template");
            html.Close(HtmlTag.tr);

            var CDRs_deduplicated = CDRs.GroupBy(cdr => cdr.Sequence).OrderBy(group => group.Key);

            foreach (var group in CDRs_deduplicated) {
                var row = group.First();
                var id = GetAsideIdentifier(row.MetaData);

                html.Open(HtmlTag.tr, $"id='{table_id}-{id}'");
                html.Open(HtmlTag.td, "class='center'");
                foreach (var g in group.Select(item => item.MetaData).Distinct())
                    html.Add(GetAsideLinkHtml(g, AsideType.Read, AssetsFolderName));
                html.Close(HtmlTag.td);
                html.OpenAndClose(HtmlTag.td, "class='seq'", row.Sequence.Replace('~', '.')); // TODO: it cannot know the correct gap char here
                html.Open(HtmlTag.td, "class='center'");
                foreach (var g in group.Select(item => item.Template).Distinct())
                    html.Add(GetAsideLinkHtml(g, AsideType.Template, AssetsFolderName));
                html.Close(HtmlTag.td);
                html.Close(HtmlTag.tr);
            }

            html.Close(HtmlTag.table);
            html.Close(HtmlTag.div);
            return html;
        }

        public static HtmlBuilder SequenceConsensusOverview(List<Dictionary<(string, int), double>> diversity, string title = null, HtmlBuilder help = null, HelperFunctionality.Annotation[] annotation = null, int[] ambiguous = null, int[] gaps = null) {
            const double threshold = 0.05;
            const int height = 35;
            const int font_size = 30;

            var html = new HtmlBuilder();
            var data_buffer = new StringBuilder();
            html.Open(HtmlTag.div, "class='graph'");
            if (title != null)
                if (help != null)
                    html.TagWithHelp(HtmlTag.h2, title, help);
                else
                    html.OpenAndClose(HtmlTag.h2, $"class='title'", title);
            if (title != null) // Bad way of only doing this in the asides and not in the CDR tables
                html.CopyData(title + " (TSV)", new HtmlBuilder(HtmlTag.p, HTMLHelp.SequenceConsensusOverviewData));
            html.Open(HtmlTag.div, $"class='sequence-logo' style='--sequence-logo-height:{height}px;--sequence-logo-font-size:{font_size}px;'");
            var offsets = new double[10];
            for (int i = 0; i < diversity.Count; i++) {
                var offset = offsets[0];
                var Class = annotation != null && i < annotation.Length && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                var ambiguous_position = ambiguous != null && ambiguous.Contains(i) ? $" ambiguous a{i}" : "";
                var click = ambiguous != null && ambiguous.Contains(i) ? $" onclick='HighlightAmbiguous(\"a{i}\", {gaps.Take(i + 1).Sum() + i})'" : "";
                html.Open(HtmlTag.div, $"class='sequence-logo-position{Class}{ambiguous_position}'{click}");

                double sum = diversity[i].Values.Sum();
                var sorted = diversity[i].ToList();
                sorted.Sort((a, b) => {
                    var res = a.Key.Item2.CompareTo(b.Key.Item2);
                    if (res == 0) return a.Value.CompareTo(b.Value);
                    return res;
                });
                data_buffer.Append($"{i}");


                bool placed = false;
                foreach (var item in sorted) {
                    if (item.Key.Item1 != "~" && (double)item.Value / sum > threshold) {
                        var size = (item.Value / sum * (font_size - offset)).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        var inverse_size = (sum / item.Value).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                        var translate = item.Key.Item2 <= 4 ? new int[] { 0, 0, 25, 33 }[item.Key.Item2] : 0;
                        html.OpenAndClose(HtmlTag.span, $"style='font-size:{size:G3}px;transform:scaleX({inverse_size:G3}) translateX({translate}%)'", item.Key.Item1);
                        placed = true;
                        if (item.Key.Item2 > 1) {
                            for (int j = 1; j < item.Key.Item2; j++) {
                                offsets[j] += item.Value / sum * (font_size - offset);
                            }
                        }
                    }
                    data_buffer.Append($"\t{item.Key.Item1}\t{item.Key.Item2}\t{item.Value.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB")):G3}");
                }
                if (!placed)
                    html.OpenAndClose(HtmlTag.span, "", ".");
                if (offset != 0)
                    html.OpenAndClose(HtmlTag.span, $"style='font-size:{offset:G3}px;opacity:0'", "A");

                html.Close(HtmlTag.div);
                data_buffer.Append("\n");
                // Cycle the offsets
                for (int j = 1; j < offsets.Length; j++) {
                    offsets[j - 1] = offsets[j];
                }
                offsets[offsets.Length - 1] = 0;
            }
            html.Close(HtmlTag.div);
            if (title == null) // Bad way of only doing this in the CDR tables and not in the asides
                html.CopyData("Sequence Consensus Overview (TSV)", new HtmlBuilder(HtmlTag.p, HTMLHelp.SequenceConsensusOverviewData));
            html.OpenAndClose(HtmlTag.textarea, $"class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close(HtmlTag.div);
            return html;
        }

        public static HtmlBuilder TableHeader(List<Template> templates, int totalReads) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='table-header'");
            if (templates.Count > 15) html.Add(PointsTableHeader(templates));
            else if (templates.Count > 5) html.Add(BarTableHeader(templates));
            html.Add(TextTableHeader(templates, totalReads));
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder TableHeader(string identifier, IEnumerable<double> lengths, IEnumerable<double> area = null) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, $"class='table-header-{identifier}'");
            html.Add(HTMLGraph.Histogram(lengths.ToList(), new HtmlBuilder("Length distribution")));
            if (area != null) {
                html.Close(HtmlTag.div);
                html.Open(HtmlTag.div);
                html.Add(HTMLGraph.Histogram(area.ToList(), new HtmlBuilder("Area distribution")));
            }
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder PointsTableHeader(List<Template> templates) {
            if (templates.Select(a => a.Score).Sum() == 0)
                return new HtmlBuilder();

            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            var data = new List<(string, List<(string, List<double>)>)>();

            foreach (var template in templates) {
                var group = data.FindIndex(a => a.Item1 == template.MetaData.ClassIdentifier);
                if (group == -1) {
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

            return HTMLGraph.GroupedPointGraph(data, header, "Overview of scores", new HtmlBuilder(HtmlTag.p, HTMLHelp.OverviewOfScores), null);
        }

        static HtmlBuilder BarTableHeader(List<Template> templates) {
            var html = new HtmlBuilder();
            if (templates.Select(a => a.Score).Sum() == 0)
                return html;

            bool displayUnique = templates.Exists(a => a.ForcedOnSingleTemplate);
            bool displayArea = templates.Any(item => item.TotalArea > 0 || item.TotalUniqueArea > 0);

            var type_data = new Dictionary<string, (double MaxScore, double TotalScore, double UniqueMaxScore, double UniqueTotalScore, int Num, int Matches, int UniqueMatches, double Area, double UniqueArea)>(templates.Count);
            foreach (var item in templates) {
                if (type_data.ContainsKey(item.Class)) {
                    var data = type_data[item.Class];
                    if (data.MaxScore < item.Score) data.MaxScore = item.Score;
                    data.TotalScore += item.Score;
                    if (data.UniqueMaxScore < item.Score) data.UniqueMaxScore = item.UniqueScore;
                    data.UniqueTotalScore += item.UniqueScore;
                    data.Num += 1;
                    data.Matches += item.Matches.Count;
                    data.UniqueMatches += item.UniqueMatches;
                    data.Area += item.TotalArea;
                    data.UniqueArea += item.TotalUniqueArea;
                    type_data[item.Class] = data;
                } else {
                    type_data.Add(item.Class, (item.Score, item.Score, item.UniqueScore, item.UniqueScore, 1, item.Matches.Count, item.UniqueMatches, item.TotalArea, item.TotalUniqueArea));
                }
            }

            var scoreData = new List<(string, List<double>)>(type_data.Count);
            var areaData = new List<(string, List<double>)>(type_data.Count);
            foreach (var (type, data) in type_data) {
                var scoreList = new List<double> { data.MaxScore, data.TotalScore / data.Num };
                var areaList = displayArea ? new List<double> { data.Matches, data.Area } : new List<double> { data.Matches };
                if (displayUnique) {
                    scoreList.Add(data.UniqueMaxScore);
                    scoreList.Add(data.UniqueTotalScore / data.Num);
                    areaList.Add(data.UniqueMatches);
                    if (displayArea) areaList.Add(data.UniqueArea);
                }
                scoreData.Add((type, scoreList));
                areaData.Add((type, areaList));
            }

            var scoreLabels = new List<(string, uint)> { ("Max Score", 0), ("Average Score", 0) };
            var areaLabels = displayArea ? new List<(string, uint)> { ("Matches", 0), ("Total Area", 1) } : new List<(string, uint)> { ("Matches", 0) };
            if (displayUnique) {
                scoreLabels.Add(("Unique Max Score", 0));
                scoreLabels.Add(("Unique Average Score", 0));
                areaLabels.Add(("Unique Matches", 0));
                if (displayArea) areaLabels.Add(("Unique Total Area", 1));
            }

            html.Open(HtmlTag.div);
            html.Add(HTMLGraph.GroupedBargraph(scoreData, scoreLabels, "Scores per group"));
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div);
            html.Add(HTMLGraph.GroupedBargraph(areaData, areaLabels, displayArea ? "Area per group" : "Matches per group"));
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder TextTableHeader(List<Template> templates, int total_reads) {
            var set = new HashSet<string>();
            var unique_set = new HashSet<string>();
            foreach (var template in templates) {
                set.UnionWith(template.Matches.Select(a => a.ReadB.EscapedIdentifier));
                unique_set.UnionWith(template.Matches.Where(a => a.Unique == true).Select(a => a.ReadB.EscapedIdentifier));
            }
            var html = new HtmlBuilder();
            html.OpenAndClose(HtmlTag.p, "class='text-header'", $"Reads matched {set.Count} ({(double)set.Count / total_reads:P2} of all input reads) of these {unique_set.Count} ({(double)unique_set.Count / set.Count:P2} of all matched reads) were matched uniquely.");
            return html;
        }
    }
}