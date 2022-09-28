using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyNameSpace;
using HeckLib.chemistry;
using System.Text.Json;
using HtmlGenerator;

namespace HTMLNameSpace
{
    static class HTMLGraph
    {
        public static List<(string, double)> AnnotateDOCData(List<double> data, int offset = 0, bool skipOnMagnitude = false)
        {
            int label = HelperFunctionality.RoundToHumanLogicalFactor(data.Count / 10);
            if (label == 0) label = 1;
            var annotated = new List<(string, double)>();
            for (int i = offset; i < data.Count + offset; i++)
                annotated.Add(((skipOnMagnitude && (i % Math.Ceiling(Math.Log10(i)) == 0 || i == 1)) || (!skipOnMagnitude && (i % label == 0)) ? $"{i:G3}" : "", data[i - offset]));

            return annotated;
        }
        public static void GroupedHistogram(StringBuilder buffer, List<(List<double> Data, string Label)> data, string title, int bins = 10)
        {
            if (data.Count == 0 || data.Any(a => a.Item1.Count == 0))
            {
                buffer.Append("<em>No data, or a dataset contains no data.</em>");
                return;
            }

            double min = data.Select(a => a.Data.Min()).Min();
            double max = data.Select(a => a.Data.Max()).Max();

            if (max == min) bins = 1;
            double step = (max - min) / bins;

            var labelled = new (string, List<double>)[bins];

            double low = min;
            for (int i = 0; i < bins; i++)
            {
                labelled[i] = ($"{low:G3}-{low + step:G3}", Enumerable.Repeat(0.0, data.Count).ToList());
                low += step;
            }

            for (int set_index = 0; set_index < data.Count; set_index++)
            {
                foreach (var item in data[set_index].Data)
                {
                    int bin = (int)Math.Floor((item - min) / step);

                    if (bin > bins - 1) bin = bins - 1;
                    else if (bin < 0) bin = 0;

                    labelled[bin].Item2[set_index]++;
                }
            }

            GroupedBargraph(buffer, labelled.ToList(), data.Select(a => (a.Label, (uint)0)).ToList(), title);
        }

        public static void Histogram(StringBuilder buffer, List<double> data, string title, string help = null, string data_help = null, int bins = 10)
        {
            if (data.Count == 0)
            {
                buffer.Append("<em>No data.</em>");
                return;
            }
            double min = data.Min();
            double max = data.Max();
            if (max == min) bins = 1;
            double step = (max - min) / bins;

            var labelled = new (string, double)[bins];

            double low = min;
            for (int i = 0; i < bins; i++)
            {
                labelled[i] = ($"{low:G3}-{low + step:G3}", 0);
                low += step;
            }

            foreach (var item in data)
            {
                int bin = (int)Math.Floor((item - min) / step);

                if (bin > bins - 1) bin = bins - 1;
                else if (bin < 0) bin = 0;

                labelled[bin].Item2++;
            }

            Bargraph(buffer, labelled.ToList(), title, help, data_help);
        }

        public static void Bargraph(StringBuilder buffer, List<(string Label, double Value)> data, string title = null, string help = null, string data_help = null, int factor = 2, HelperFunctionality.Annotation[] annotation = null)
        {
            if (data.Count == 0)
            {
                buffer.Append("<em>No data.</em>");
                return;
            }
            var dataBuffer = new StringBuilder("Label\tValue");

            buffer.Append("<div class='graph'>");
            if (title != null)
                if (help != null)
                    buffer.Append(CommonPieces.TagWithHelp("h2", title, help));
                else
                    buffer.Append($"<h2 class='title'>{title}</h2>");

            if (title != null) // Copy data should not appear in the alignment read detail index menus
                buffer.Append(CommonPieces.CopyData(title + " (TSV)", data_help));

            double max = Math.Ceiling(data.Select(a => a.Value).Max() / factor) * factor;
            double min = Math.Ceiling(data.Select(a => a.Value).Min() / factor) * factor;

            if (min < 0)
            {
                max = Math.Max(max, 0); // Make sure to not start graphs below zero as this breaks the layout
                buffer.Append($"<div class='histogram negative' oncontextmenu='CopyGraphData()' aria-hidden='true' style='grid-template-rows:{max / (max - min) * 150}px {min / (min - max) * 150}px 1fr'>");
                // Y axis
                buffer.Append($"<span class='y-axis'><span class='max'>{max:G3}</span><span class='min'>{min:G3}</span></span><span class='empty'></span>");

                // Data
                for (int i = 0; i < data.Count; i++)
                {
                    var set = data[i];
                    var Class = annotation != null && i < annotation.Length && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                    if (set.Value >= 0)
                        buffer.Append($"<span class='bar{Class}' style='height:{set.Value / max:P2}'><span>{set.Value:G3}</span></span><span class='empty'></span><span class='label'>{set.Label}</span>");
                    else
                        buffer.Append($"<span class='empty'></span><span class='bar negative' style='height:{set.Value / min:P2}'><span>{set.Value:G3}</span></span><span class='label'>{set.Label}</span>");
                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value:G3}");
                }
            }
            else
            {
                min = 0; // always start graphs at 0 
                buffer.Append("<div class='histogram' oncontextmenu='CopyGraphData()' aria-hidden='true'>");

                // Y axis
                buffer.Append($"<span class='y-axis'><span class='max'>{max:G3}</span><span class='min'>0</span></span><span class='empty'></span>");

                // Data
                for (int i = 0; i < data.Count; i++)
                {
                    var set = data[i];
                    var Class = annotation != null && i < annotation.Length && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                    buffer.Append($"<span class='bar{Class}' style='height:{set.Value / max:P2}'><span>{set.Value:G3}</span></span><span class='label'>{set.Label}</span>");
                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value:G3}");
                }

            }
            buffer.Append("</div>");
            if (title != null)
                buffer.Append($"<textarea class='graph-data hidden' aria-hidden='true'>{dataBuffer.ToString()}</textarea>");
            buffer.Append("</div>");
        }

        /// <summary>
        /// Creates a grouped bargraph
        /// a = la, b = lb, c = lc
        /// (a,b,c) (a,b,c) (a,b,c)
        /// A lA    B lB    C lC
        /// </summary>
        /// <param name="data">The data plus label per point on the x axis. ((lA, (a,b,c)), ...)</param>
        /// <param name="header">The labels for each group on each point. ((la, d), ...)</param>
        /// <returns></returns>
        public static void GroupedBargraph(StringBuilder buffer, List<(string Label, List<double> Dimensions)> data, List<(string Label, uint Dimension)> header, string title, int factor = 2, bool baseYMinOnData = false)
        {
            if (data.Count == 0 || data.Any(a => a.Dimensions.Count == 0))
            {
                buffer.Append("<em>No data, or a dataset contains no data.</em>");
                return;
            }

            int dimensions = header.Count;
            double[] max_values = new double[dimensions];
            double[] min_values = new double[dimensions];

            Array.Fill(max_values, Double.MinValue);
            Array.Fill(min_values, Double.MaxValue);

            foreach ((_, var dims) in data)
            {
                if (dims.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({dims.Count}) as the rest ({dimensions}).");
                for (int i = 0; i < dimensions; i++)
                {
                    if (dims[i] > max_values[i]) max_values[i] = dims[i];
                    if (dims[i] < min_values[i]) min_values[i] = dims[i];
                }
            }

            int dimensionDimensions = (int)header.Select(a => a.Dimension).Max();
            double[] dimensionMax = new double[dimensionDimensions + 1];
            double[] dimensionMin = new double[dimensionDimensions + 1];

            for (int i = 0; i < dimensions; i++)
            {
                var dimensionIndex = header[i].Dimension;

                if (max_values[i] > dimensionMax[dimensionIndex]) dimensionMax[dimensionIndex] = max_values[i];
                if (min_values[i] > dimensionMin[dimensionIndex]) dimensionMin[dimensionIndex] = max_values[i];
            }

            for (int i = 0; i < dimensionDimensions; i++)
                dimensionMax[i] = Math.Ceiling(dimensionMax[i] / factor) * factor;

            if (!baseYMinOnData) Array.Fill(dimensionMin, 0);

            // Create Legend
            var dataBuffer = new StringBuilder("Group");

            buffer.Append($"<div class='graph'><h2 class='title'>{title}</h2><div class='histogram-header'>");
            for (int i = 0; i < dimensions; i++)
            {
                buffer.Append($"<span>{header[i].Label}</span>");
                dataBuffer.Append($"\t\"{header[i].Label}\"");
            }

            // Create Graph
            buffer.Append("</div>" + CommonPieces.CopyData(title + " (TSV)") + "<div class='histogram grouped' aria-hidden='true' oncontextmenu='CopyGraphData()'>");
            foreach (var set in data)
            {
                buffer.Append($"<span class='group'>");
                dataBuffer.Append($"\n\"{set.Label}\"");

                // Create Bars
                for (int i = 0; i < dimensions; i++)
                {
                    var dimensionIndex = header[i].Dimension;
                    string height = ((set.Dimensions[i] - dimensionMin[dimensionIndex]) / (dimensionMax[dimensionIndex] - dimensionMin[dimensionIndex]) * 100).ToString();
                    buffer.Append($"<span class='bar' style='height:{height}%'></span>");
                    dataBuffer.Append($"\t{set.Dimensions[i]}");
                }

                // Create Tooltip
                buffer.Append($"<span class='tooltip'>{set.Label}");
                for (int i = 0; i < dimensions; i++)
                    buffer.Append($"<span class='dim'>{set.Dimensions[i]:G3}</span>");

                // Create Label
                buffer.Append($"</span></span><span class='label'>{set.Label}</span>");
            }

            buffer.Append($"</div><textarea class='graph-data hidden' aria-hidden='true'>{dataBuffer.ToString()}</textarea></div>");
        }

        static int graph_counter = 0;
        /// <summary>
        /// Generates a grouped point graph, with a multiple values per point which will be linearly normalised to fit the same range.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static void GroupedPointGraph(StringBuilder buffer, List<(string GroupLabel, List<(string Label, List<double> Values)> Points)> data, List<string> header, string title, string help, string data_help)
        {
            if (data.Count == 0 || data.Any(a => a.Points.Count == 0))
            {
                buffer.Append("<em>No data, or a dataset contains no data.</em>");
                return;
            }
            graph_counter++;
            string identifier = $"graph-{graph_counter}";
            int dimensions = header.Count;
            double[] max_values = new double[dimensions];
            double[] min_values = new double[dimensions];

            Array.Fill(max_values, Double.MinValue);
            Array.Fill(min_values, Double.MaxValue);

            foreach ((_, var group) in data)
            {
                foreach ((_, var values) in group)
                {
                    if (values.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({values.Count}) as the rest ({dimensions}).");
                    for (int i = 0; i < dimensions; i++)
                    {
                        if (values[i] > max_values[i]) max_values[i] = values[i];
                        if (values[i] < min_values[i]) min_values[i] = values[i];
                    }
                }
            }

            // Create Legend
            var dataBuffer = new StringBuilder("Group\tPoint");

            buffer.Append($"<div class='graph point-graph' oncontextmenu='CopyGraphData()' aria-hidden='true'>" + CommonPieces.TagWithHelp("h2", title, help));
            for (int i = 0; i < dimensions; i++)
            {
                var check = i < 3 ? " checked " : "";
                buffer.Append($"<input type='checkbox' class='show-data-{i}' id='{identifier}-show-data-{i}'{check}/>");
                buffer.Append($"<label for='{identifier}-show-data-{i}'>{header[i]}</label>");
                dataBuffer.Append($"\t\"{header[i]}\"");
            }

            buffer.Append(CommonPieces.CopyData(title + " (TSV)", data_help) + "<div class='plot'><div class='y-axis'><span class='max'>100%</span><span class='title'>Linear Relative Value</span><span class='min'>0%</span></div>");
            // Create Graph
            foreach (var group in data)
            {
                buffer.Append($"<div class='group' style='flex-grow:{group.Points.Count}'>");

                foreach (var point in group.Points)
                {
                    dataBuffer.Append($"\n\"{group.GroupLabel}\"\t{point.Label}");
                    buffer.Append($"<a href='#{identifier}_{point.Label}' class='values'>");
                    // Create Points
                    for (int i = 0; i < dimensions; i++)
                    {
                        buffer.Append($"<span class='point' style='--x:{(point.Values[i] - min_values[i]) / (max_values[i] - min_values[1])}'></span>");
                        dataBuffer.Append($"\t{point.Values[i]}");
                    }
                    buffer.Append($"</a><span class='label'><a href='#{identifier}-{point.Label}'>{point.Label}</a></span>");
                }

                // Create Tooltip
                //buffer.Append($"<span class='tooltip'>{group.Label}");
                //for (int i = 0; i < dimensions; i++)
                //    buffer.Append($"<span class='dim'>{group.Dimensions[i]:G3}</span>");
                if (group.Points.Count > 2)
                    buffer.Append($"<span class='group-label'>{group.GroupLabel}</span>");
                buffer.Append("</div>");
            }

            buffer.Append($"</div><textarea class='graph-data hidden' aria-hidden='true'>{dataBuffer.ToString()}</textarea></div>");
        }

        /// <summary>
        /// Render the given tree into a cladogram representation with circles with fill representing the scoring for each node.
        /// </summary>
        /// <param name="id"> Unique ID. </param>
        /// <param name="tree"> The tree to render. </param>
        /// <param name="templates"> The list of templates at the basis of this tree. </param>
        /// <param name="type"> The type of the leaf nodes. </param>
        /// <param name="AssetsFolderName"> The path to the assets folder from the current HTML. </param>
        /// <returns></returns>
        public static string RenderTree(string id, PhylogeneticTree.ProteinHierarchyTree tree, List<Template> templates, CommonPieces.AsideType type, string AssetsFolderName)
        {
            var html = new HtmlBuilder();
            const double xf = 30; // Width of the graph in pixels, do not forget to update the CSS when updating this value. The tree will be squeezed in the x dimension if the screen is not wide enough or just cap at this width if the screen is wide.
            const double yf = 22;   // Height of the labels
            const double radius = 10;  // Radius of the score circles
            const double stroke = 2;  // Stroke of the circles and tree lines, do not forget to update the CSS when updating this value.
            const double text_width = 100; // The width of the border around the leaf text

            (int, int, int, int, double, double) unpack((int, int, int, int, double, double, string) data)
            {
                return (data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6);
            }

            var pos = 0.0;
            var y_pos_tree = tree.DataTree.ReverseRemodel<(double Y, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) Scores, ReadMetaData.IMetaData MetaData)>(
                (t, value) => ((t.Left.Value.Item2.Value.Y + t.Right.Value.Item2.Value.Y) / 2, unpack(value), null),
                leaf =>
                {
                    pos += 1.0;
                    return (pos, unpack(leaf), templates.Find(t => t.MetaData.Identifier == leaf.Name).MetaData);
                });

            var columns = 0;
            var pos_tree = y_pos_tree.Remodel<(double X, double Y, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) Scores, ReadMetaData.IMetaData MetaData)>((t, depth) =>
            {
                if (depth > columns) columns = depth;
                return ((double)depth, t.Value.Y, t.Value.Scores, t.Value.MetaData);
            });

            var max_x = xf * (columns + 1);
            var max = tree.DataTree.Fold((0, 0, 0, 0, 0.0, 0.0), (acc, value) => (Math.Max(value.Score, acc.Item1), Math.Max(value.UniqueScore, acc.Item2), Math.Max(value.Matches, acc.Item3), Math.Max(value.UniqueMatches, acc.Item4), Math.Max(value.Area, acc.Item5), Math.Max(value.UniqueArea, acc.Item6)));

            html.Open(HtmlTag.div, "class='phylogenetic-tree'");
            html.UnsafeContent(CommonPieces.UserHelp("Tree", HTMLHelp.Tree));

            var button_names = new string[] { "Score", "Matches", "Area" };
            for (int i = 0; i < button_names.Length; i++)
            {
                var check = i == 0 ? " checked " : "";
                html.Empty(HtmlTag.input, $"type='radio' class='show-data-{i}' name='{id}' id='{id}-{i}'{check}");
                html.OpenAndClose(HtmlTag.label, $"for='{id}-{i}'", button_names[i]);
            }
            html.OpenAndClose(HtmlTag.p, "class='legend'", "Cumulative value of all children (excluding unique)");
            html.OpenAndClose(HtmlTag.p, "class='legend unique'", "Cumulative value for unique matches");
            html.UnsafeContent(CommonPieces.CopyData("Tree (JSON)", HTMLHelp.TreeData));
            html.Open(HtmlTag.div, "class='container'");
            html.Open(HtmlTag.div, $"class='tree' style='max-width:{max_x + radius + text_width}px'");
            var svg = new SvgBuilder();
            svg.Open(SvgTag.svg, $"viewBox='0 0 {max_x + radius + text_width} {((int)pos + 1) * yf}' width='100%' height='{((int)pos + 1) * yf}px' preserveAspectRatio='none'");

            string GetScores((int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) value, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) max, bool unique)
            {
                double Normalise(double value)
                {
                    return double.IsNaN(value) ? 0.0 : value;
                }

                var values = (0.0, 0.0, 0.0);
                if (!unique)
                    values = (Normalise((double)value.Score / max.Score), Normalise((double)value.Matches / max.Matches), Normalise((double)value.Area / max.Area));
                else
                    values = (Normalise((double)value.UniqueScore / max.UniqueScore), Normalise((double)value.UniqueMatches / max.UniqueMatches), Normalise((double)value.UniqueArea / max.UniqueArea));

                return $"--score:{values.Item1};--matches:{values.Item2};--area:{values.Item3};";
            }

            pos_tree.Apply(t =>
            {
                var x = t.Value.X * xf + radius + stroke;
                var x1 = (t.Value.X + 1) * xf + stroke / 2;
                if (t.Value.X == columns - 1) x1 += xf - radius * 2 - stroke;
                var y = t.Value.Y * yf;
                var ly = t.Left.Value.Item2.Value.Y * yf;
                var ry = t.Right.Value.Item2.Value.Y * yf;
                svg.Open(SvgTag.g);
                svg.OpenAndClose(SvgTag.line, $"x1={x}px y1={ly}px x2={x}px y2={y - radius}px");
                svg.OpenAndClose(SvgTag.line, $"x1={x}px y1={y + radius}px x2={x}px y2={ry}px");
                svg.OpenAndClose(SvgTag.line, $"x1={x - stroke / 2}px y1={ly}px x2={x1}px y2={ly}px");
                svg.OpenAndClose(SvgTag.line, $"x1={x - stroke / 2}px y1={ry}px x2={x1}px y2={ry}px");
                svg.OpenAndClose(SvgTag.circle, $"cx={x}px cy={y}px r={radius}px class='value' style='{GetScores(t.Value.Scores, max, false)}'");
                svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-0'", $"Score: {t.Value.Scores.Score} ({(double)t.Value.Scores.Score / max.Item1:P})");
                svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-1'", $"Matches: {t.Value.Scores.Matches} ({(double)t.Value.Scores.Matches / max.Item3:P})");
                svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-2'", $"Area: {t.Value.Scores.Area:G3} ({(double)t.Value.Scores.Area / max.Item5:P})");
                svg.Close(SvgTag.g);
            }, leaf =>
            {
                var x = leaf.X * xf;
                var y = leaf.Y * yf;
                var end = max_x - radius - stroke;
                svg.Open(SvgTag.g);
                if (leaf.X != columns) svg.OpenAndClose(SvgTag.line, $"x1={x + stroke / 2}px y1={y}px x2={end - radius}px y2={y}px");
                svg.OpenAndClose(SvgTag.path, $"d='M {end} {y + radius} A {radius} {radius} 0 0 1 {end} {y - radius}' class='value' style='{GetScores(leaf.Scores, max, false)}'");
                svg.OpenAndClose(SvgTag.path, $"d='M {end} {y - radius} A {radius} {radius} 0 0 1 {end} {y + radius}' class='value unique' style='{GetScores(leaf.Scores, max, true)}'");
                svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-0' style='text-anchor:end'", $"Score: {leaf.Scores.Score} ({(double)leaf.Scores.Score / max.Item1:P}) Unique: {leaf.Scores.UniqueScore} ({(double)leaf.Scores.UniqueScore / max.Item2:P})");
                svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-1' style='text-anchor:end'", $"Area: {leaf.Scores.Area:G3} ({(double)leaf.Scores.Area / max.Item5:P}) Unique: {leaf.Scores.UniqueArea:G3} ({(double)leaf.Scores.UniqueArea / max.Item6:P})");
                svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-2' style='text-anchor:end'", $"Matches: {leaf.Scores.Matches} ({(double)leaf.Scores.Matches / max.Item3:P}) Unique: {leaf.Scores.UniqueMatches} ({(double)leaf.Scores.UniqueMatches / max.Item4:P})");
                svg.Open(SvgTag.a, $"class='info-link' id='tree-leaf-{CommonPieces.GetAsideIdentifier(leaf.MetaData, false)}' href='{CommonPieces.GetAsideRawLink(leaf.MetaData, type, AssetsFolderName)}' target='_blank'");
                svg.OpenAndClose(SvgTag.rect, $"x={max_x + radius}px y={y - yf / 2 + stroke}px width={text_width}px height={yf - stroke * 2}px rx=3.2px");
                svg.OpenAndClose(SvgTag.text, $"x={max_x + radius + stroke * 2}px y={y + 1}px", CommonPieces.GetAsideIdentifier(leaf.MetaData, true));
                svg.Close(SvgTag.a);
                svg.Close(SvgTag.g);
            });

            var json = tree.DataTree.Fold((value, ad, a, bd, b) =>
            {
                var obj = new JsonObject();
                obj.Keys.Add("left", a);
                obj.Keys.Add("leftDistance", new JsonNumber(ad));
                obj.Keys.Add("right", b);
                obj.Keys.Add("rightDistance", new JsonNumber(bd));
                obj.Keys.Add("score", new JsonNumber(value.Score));
                obj.Keys.Add("matches", new JsonNumber(value.Matches));
                obj.Keys.Add("area", new JsonNumber(value.Area));
                return obj;
            }, value =>
            {
                var obj = new JsonObject();
                obj.Keys.Add("name", new JsonString(value.Name));
                obj.Keys.Add("score", new JsonNumber(value.Score));
                obj.Keys.Add("uniqueScore", new JsonNumber(value.UniqueScore));
                obj.Keys.Add("matches", new JsonNumber(value.Matches));
                obj.Keys.Add("uniqueMatches", new JsonNumber(value.UniqueMatches));
                obj.Keys.Add("area", new JsonNumber(value.Area));
                obj.Keys.Add("uniqueArea", new JsonNumber(value.UniqueArea));
                return obj;
            });
            var data_buffer = new StringBuilder();
            json.ToString(data_buffer);
            svg.Close(SvgTag.svg);
            html.Add(svg);
            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close(HtmlTag.div);

            return html.ToString();
        }

        /// <summary>
        /// Render a spectrum of a peptide. Displays a legend, an overview of the peptide and its fragments, and the full spectrum.
        /// </summary>
        /// <param name="sequence">The sequence of the peptide.</param>
        /// <param name="spectrum">The spectrum to display.</param>
        /// <returns>HTML with title included.</returns>
        /*
        public static string RenderSpectrum(string sequence, Fragmentation.PeptideSpectrum spectrum)
        {
            var html = new HtmlBuilder();
            var data_buffer = new StringBuilder();
            html.Open(HtmlTag.div, "class='spectrum'");
            html.UnsafeContent(CommonPieces.TagWithHelp("h2", "Spectrum " + spectrum.ScanID, HTMLHelp.Spectrum));
            html.UnsafeContent(CommonPieces.CopyData($"Spectrum {spectrum.ScanID} (TSV)"));
            html.Open(HtmlTag.div, "class='legend'");
            html.OpenAndClose(HtmlTag.span, "class='title'", "Ion legend");
            html.OpenAndClose(HtmlTag.span, "class='ion A'", "A");
            html.OpenAndClose(HtmlTag.span, "class='ion B'", "B");
            html.OpenAndClose(HtmlTag.span, "class='ion C'", "C");
            html.OpenAndClose(HtmlTag.span, "class='ion W'", "W");
            html.OpenAndClose(HtmlTag.span, "class='ion X'", "X");
            html.OpenAndClose(HtmlTag.span, "class='ion Y'", "Y");
            html.OpenAndClose(HtmlTag.span, "class='ion Z'", "Z");
            html.OpenAndClose(HtmlTag.span, "class='other'", "Other");
            var id = spectrum.ScanID.Replace(':', '_');
            html.Empty("input", $"id='{id}_unassigned' type='checkbox' checked class='unassigned'");
            html.OpenAndClose("label", $"for='{id}_unassigned' class='unassigned'", "Unassigned");
            html.Open("label", $"class='label'");
            html.Content("Ion");
            html.OpenAndClose(Html"sup", "", "Charge");
            html.OpenAndClose(Html"sub", "style='margin-left:-6ch;margin-right:.5rem;'", "Position");
            html.Content("Show for top:");
            html.OpenAndClose("input", $"id='{id}_label' type='range' min='0' max='100' value='100'");
            html.OpenAndClose("input", $"id='{id}_label_value' type='number' min='0' max='100' value='100'");
            html.Content("%");
            html.Close("label");
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='peptide'");

            var fragment_overview = new HashSet<string>[sequence.Length];
            for (int i = 0; i < sequence.Length; i++) fragment_overview[i] = new HashSet<string>();
            var max_mz = 0.0;
            var max_intensity = 0.0;
            var max_intensity_unassigned = 0.0;
            var unassigned_threshold = 0.02 * spectrum.MatchedFragments.Select(s => s.Centroid.Intensity).Max();

            foreach (var (fragment, centroid) in spectrum.MatchedFragments)
            {
                if (fragment == null || fragment.Position == -1)
                {
                    if (centroid.Intensity > unassigned_threshold)
                    {
                        max_mz = Math.Max(max_mz, centroid.Mz);
                        max_intensity_unassigned = Math.Max(max_intensity_unassigned, centroid.Intensity);
                    }
                }
                else
                {
                    max_mz = Math.Max(max_mz, centroid.Mz);
                    max_intensity = Math.Max(max_intensity, centroid.Intensity);
                    var position = fragment.Position - 1;
                    if (fragment.Terminus == Proteomics.Terminus.C) position = sequence.Length - position - 1;
                    fragment_overview[position].Add(PeptideFragment.IonToString(fragment.FragmentType));
                }
            }
            max_mz *= 1.01;
            max_intensity *= 1.01;
            max_intensity_unassigned *= 1.01;
            max_intensity_unassigned = Math.Max(max_intensity, max_intensity_unassigned);

            // Display the full sequence from N to C terminus with its fragments annotated
            for (int i = 0; i < sequence.Length; i++)
            {
                html.Open("span", $"data-pos='{i + 1}'");
                html.Content(sequence[i].ToString());
                foreach (var fragment_type in fragment_overview[i])
                {
                    html.OpenAndClose("span", $"class='corner {fragment_type}'", "");
                }
                html.Close("span");
            }
            html.Close("div");

            html.Open("div", "class='canvas-wrapper unassigned label' aria-hidden='true'");

            html.Open("div", "class='y-axis'");
            html.OpenAndClose("span", "", "0");
            html.OpenAndClose("span", "class='assigned'", (max_intensity / 4).ToString("G3"));
            html.OpenAndClose("span", "class='assigned'", (max_intensity / 2).ToString("G3"));
            html.OpenAndClose("span", "class='assigned'", (3 * max_intensity / 4).ToString("G3"));
            html.OpenAndClose("span", "class='assigned last'", max_intensity.ToString("G3"));
            html.OpenAndClose("span", "class='unassigned'", (max_intensity_unassigned / 4).ToString("G3"));
            html.OpenAndClose("span", "class='unassigned'", (max_intensity_unassigned / 2).ToString("G3"));
            html.OpenAndClose("span", "class='unassigned'", (3 * max_intensity_unassigned / 4).ToString("G3"));
            html.OpenAndClose("span", "class='unassigned last'", max_intensity_unassigned.ToString("G3"));
            html.Close("div");

            html.Open("div", $"class='canvas' style='--min-mz:0;--max-mz:{max_mz};--max-intensity:{max_intensity};--max-intensity-unassigned:{max_intensity_unassigned};' data-initial-max-mz='{max_mz}'");
            html.OpenAndClose("span", "class='selection' hidden='true'");
            html.OpenAndClose("div", "class='zoom-out'", "Zoom Out");

            data_buffer.AppendLine("Mz\tCharge\tIntensity\tFragmentType\tMassShift\tPosition");

            foreach (var (fragment, centroid) in spectrum.MatchedFragments)
            {
                if (fragment == null)
                {
                    if (centroid.Intensity > unassigned_threshold)
                    {
                        html.OpenAndClose("span", $"class='peak unassigned' style='--mz:{centroid.Mz};--intensity:{centroid.Intensity};'");
                        data_buffer.AppendLine($"{centroid.Mz}\t{centroid.Charge}\t{centroid.Intensity}\t-\t-\t-");
                    }
                }
                else
                {
                    var ion = PeptideFragment.IonToString(fragment.FragmentType).Replace(' ', '-');
                    var shift = PeptideFragment.MassShiftToString(fragment.MassShift).Replace(' ', '-').Replace(' ', '-');
                    var normal_ion = (fragment.FragmentType == PeptideFragment.ION_A || fragment.FragmentType == PeptideFragment.ION_B || fragment.FragmentType == PeptideFragment.ION_C || fragment.FragmentType == PeptideFragment.ION_X || fragment.FragmentType == PeptideFragment.ION_Y || fragment.FragmentType == PeptideFragment.ION_Z);
                    if (fragment.Position == -1 || !normal_ion)
                    {
                        html.Open("span", $"class='peak {ion} {shift} label' style='--mz:{fragment.Mz};--intensity:{centroid.Intensity};'");
                        html.OpenAndClose("span", $"class='special' style='--content:\"{ion} {shift}\"'", "*");
                        html.Close("span");
                        data_buffer.AppendLine($"{centroid.Mz}\t{centroid.Charge}\t{centroid.Intensity}\t{PeptideFragment.IonToString(fragment.FragmentType)}\t{PeptideFragment.MassShiftToString(fragment.MassShift)}\t-");
                    }
                    else
                    {
                        var position = fragment.Position;
                        if (fragment.Terminus == Proteomics.Terminus.C) position = sequence.Length - position + 1;
                        html.Open("span", $"class='peak {ion} {shift} label' style='--mz:{fragment.Mz};--intensity:{centroid.Intensity};' data-pos='{position}'");
                        html.Open("span", "");
                        html.Content(ion);
                        html.OpenAndClose("sup", "", fragment.Charge.ToString());
                        html.OpenAndClose("sub", "", fragment.Position.ToString());
                        html.Close("span");
                        html.Close("span");
                        data_buffer.AppendLine($"{centroid.Mz}\t{centroid.Charge}\t{centroid.Intensity}\t{PeptideFragment.IonToString(fragment.FragmentType)}\t{PeptideFragment.MassShiftToString(fragment.MassShift)}\t{fragment.Position}");
                    }
                }
            }

            html.Close("div");

            html.Open("div", "class='x-axis'");
            html.OpenAndClose("span", "", "0");
            html.OpenAndClose("span", "class='1_4'", (max_mz / 4).ToString("F0"));
            html.OpenAndClose("span", "", (max_mz / 2).ToString("F0"));
            html.OpenAndClose("span", "class='3_4'", (3 * max_mz / 4).ToString("F0"));
            html.OpenAndClose("span", "", max_mz.ToString("F0"));
            html.Close("div");

            html.Close("div");
            html.OpenAndClose("textarea", "class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close("div");
            return html.ToString();
        }
        */
    }
}