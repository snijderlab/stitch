using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stitch;
using HeckLib.chemistry;
using System.Text.Json;
using HtmlGenerator;

namespace HTMLNameSpace {
    public static class HTMLGraph {
        public static List<(string, double)> AnnotateDOCData(List<double> data, int offset = 0, bool skipOnMagnitude = false) {
            int label = HelperFunctionality.RoundToHumanLogicalFactor(data.Count / 10);
            if (label == 0) label = 1;
            var annotated = new List<(string, double)>();
            for (int i = offset; i < data.Count + offset; i++)
                annotated.Add(((skipOnMagnitude && (i % Math.Ceiling(Math.Log10(i)) == 0 || i == 1)) || (!skipOnMagnitude && (i % label == 0)) ? $"{i:G3}" : "", data[i - offset]));

            return annotated;
        }

        public static HtmlBuilder Histogram(List<double> data, HtmlBuilder title, HtmlBuilder help = null, HtmlBuilder data_help = null, int bins = 10) {
            var html = new HtmlBuilder();
            if (data.Count == 0) {
                html.OpenAndClose(HtmlTag.em, "", "No data.");
                return html;
            }
            double min = data.Min();
            double max = data.Max();
            if (max == min) bins = 1;
            double step = (max - min) / bins;

            var labelled = new (string, double)[bins];

            double low = min;
            for (int i = 0; i < bins; i++) {
                labelled[i] = ($"{low:G3}-{low + step:G3}", 0);
                low += step;
            }

            foreach (var item in data) {
                int bin = (int)Math.Floor((item - min) / step);

                if (bin > bins - 1) bin = bins - 1;
                else if (bin < 0) bin = 0;

                labelled[bin].Item2++;
            }

            return Bargraph(labelled.ToList(), title, help, data_help);
        }

        public static HtmlBuilder Bargraph(List<(string Label, double Value)> data, HtmlBuilder title = null, HtmlBuilder help = null, HtmlBuilder data_help = null, int factor = 2, HelperFunctionality.Annotation[] annotation = null, string title_string = null) {
            var html = new HtmlBuilder();
            if (data.Count == 0) {
                html.OpenAndClose(HtmlTag.em, "", "No data.");
                return html;
            }
            var dataBuffer = new StringBuilder("Label\tValue");

            html.Open(HtmlTag.div, "class='graph'");
            if (title != null)
                if (help != null) {
                    html.Open(HtmlTag.h2);
                    html.Add(title);
                    html.UserHelp("Bargraph", help);
                    html.Close(HtmlTag.h2);
                } else
                    html.OpenAndClose(HtmlTag.h2, "class='title'", title);

            if (title != null) // Copy data should not appear in the alignment read detail index menus
                html.CopyData((title_string ?? title.ToString()) + " (TSV)", data_help);

            double max = Math.Ceiling(data.Select(a => a.Value).Max() / factor) * factor;
            double min = Math.Ceiling(data.Select(a => a.Value).Min() / factor) * factor;

            if (min < 0) {
                max = Math.Max(max, 0); // Make sure to not start graphs below zero as this breaks the layout
                html.Open(HtmlTag.div, $"class='histogram negative' oncontextmenu='CopyGraphData()' aria-hidden='true' style='grid-template-rows:{max / (max - min) * 150}px {min / (min - max) * 150}px 1fr'");
                // Y axis
                html.Open(HtmlTag.span, "class='y-axis'");
                html.OpenAndClose(HtmlTag.span, "class='max'", max.ToString("G3"));
                html.OpenAndClose(HtmlTag.span, "class='min'", min.ToString("G3"));
                html.Close(HtmlTag.span);
                html.OpenAndClose(HtmlTag.span, "class='empty'");

                // Data
                for (int i = 0; i < data.Count; i++) {
                    var set = data[i];
                    var Class = annotation != null && i < annotation.Length && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                    if (set.Value >= 0) {
                        html.OpenAndClose(HtmlTag.span, $"class='b{Class}' style='height:{set.Value / max:P2}'");
                        html.OpenAndClose(HtmlTag.span, "class='empty'");
                        html.OpenAndClose(HtmlTag.span, "class='l'", set.Label);
                    } else {
                        html.OpenAndClose(HtmlTag.span, "class='empty'");
                        html.OpenAndClose(HtmlTag.span, $"class='b negative' style='height:{set.Value / min:P2}'");
                        html.OpenAndClose(HtmlTag.span, "class='l'", set.Label);
                    }
                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value:G3}");
                }
            } else {
                min = 0; // always start graphs at 0
                html.Open(HtmlTag.div, "class='histogram' oncontextmenu='CopyGraphData()' aria-hidden='true'");

                // Y axis
                html.Open(HtmlTag.span, "class='y-axis'");
                html.OpenAndClose(HtmlTag.span, "class='max'", max.ToString("G3"));
                html.OpenAndClose(HtmlTag.span, "class='min'", "0");
                html.Close(HtmlTag.span);
                html.OpenAndClose(HtmlTag.span, "class='empty'");

                // Data
                for (int i = 0; i < data.Count; i++) {
                    var set = data[i];
                    var Class = annotation != null && i < annotation.Length && annotation[i] != HelperFunctionality.Annotation.None ? " " + annotation[i].ToString() : "";
                    html.OpenAndClose(HtmlTag.span, $"class='b{Class}' style='height:{set.Value / max:P2}'");
                    html.OpenAndClose(HtmlTag.span, "class='l'", set.Label);

                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value:G3}");
                }

            }
            html.Close(HtmlTag.div);
            if (title != null)
                html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", dataBuffer.ToString());
            html.Close(HtmlTag.div);
            return html;
        }

        /// <summary> Creates a grouped bargraph
        /// a = la, b = lb, c = lc
        /// (a,b,c) (a,b,c) (a,b,c)
        /// A lA    B lB    C lC </summary>
        /// <param name="data">The data plus label per point on the x axis. ((lA, (a,b,c)), ...)</param>
        /// <param name="header">The labels for each group on each point. ((la, d), ...)</param>
        /// <returns></returns>
        public static HtmlBuilder GroupedBargraph(List<(string Label, List<double> Dimensions)> data, List<(string Label, uint Dimension)> header, string title, int factor = 2, bool baseYMinOnData = false) {
            var html = new HtmlBuilder();
            if (data.Count == 0 || data.Any(a => a.Dimensions.Count == 0)) {
                html.OpenAndClose(HtmlTag.em, "", "No data, or a dataset contains no data.");
                return html;
            }

            int dimensions = header.Count;
            double[] max_values = new double[dimensions];
            double[] min_values = new double[dimensions];

            Array.Fill(max_values, Double.MinValue);
            Array.Fill(min_values, Double.MaxValue);

            foreach ((_, var dims) in data) {
                if (dims.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({dims.Count}) as the rest ({dimensions}).");
                for (int i = 0; i < dimensions; i++) {
                    if (dims[i] > max_values[i]) max_values[i] = dims[i];
                    if (dims[i] < min_values[i]) min_values[i] = dims[i];
                }
            }

            int dimensionDimensions = (int)header.Select(a => a.Dimension).Max();
            double[] dimensionMax = new double[dimensionDimensions + 1];
            double[] dimensionMin = new double[dimensionDimensions + 1];

            for (int i = 0; i < dimensions; i++) {
                var dimensionIndex = header[i].Dimension;

                if (max_values[i] > dimensionMax[dimensionIndex]) dimensionMax[dimensionIndex] = max_values[i];
                if (min_values[i] > dimensionMin[dimensionIndex]) dimensionMin[dimensionIndex] = max_values[i];
            }

            for (int i = 0; i < dimensionDimensions; i++)
                dimensionMax[i] = Math.Ceiling(dimensionMax[i] / factor) * factor;

            if (!baseYMinOnData) Array.Fill(dimensionMin, 0);

            // Create Legend
            var dataBuffer = new StringBuilder("Group");
            html.Open(HtmlTag.div, "class='graph'");
            html.OpenAndClose(HtmlTag.h2, "class='title'", title);
            html.Open(HtmlTag.div, "class='histogram-header'");

            for (int i = 0; i < dimensions; i++) {
                html.OpenAndClose(HtmlTag.span, "", header[i].Label);
                dataBuffer.Append($"\t\"{header[i].Label}\"");
            }

            // Create Graph
            html.Close(HtmlTag.div);
            html.CopyData(title + " (TSV)");
            html.Open(HtmlTag.div, "class='histogram grouped' aria-hidden='true' oncontextmenu='CopyGraphData()'");
            foreach (var set in data) {
                html.Open(HtmlTag.span, "class='group'");
                dataBuffer.Append($"\n\"{set.Label}\"");

                // Create Bars
                for (int i = 0; i < dimensions; i++) {
                    var dimensionIndex = header[i].Dimension;
                    string height = ((set.Dimensions[i] - dimensionMin[dimensionIndex]) / (dimensionMax[dimensionIndex] - dimensionMin[dimensionIndex]) * 100).ToString();
                    html.OpenAndClose(HtmlTag.span, $"class='bar' style='height:{height}%'");
                    dataBuffer.Append($"\t{set.Dimensions[i]}");
                }

                // Create Tooltip
                html.Open(HtmlTag.span, "class='tooltip'");
                html.Content(set.Label);
                for (int i = 0; i < dimensions; i++)
                    html.OpenAndClose(HtmlTag.span, "class='dim'", set.Dimensions[i].ToString("G3"));

                // Create Label
                html.Close(HtmlTag.span);
                html.Close(HtmlTag.span);
                html.OpenAndClose(HtmlTag.span, "class='label'", set.Label);
            }

            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", dataBuffer.ToString());
            html.Close(HtmlTag.div);
            return html;
        }

        static int graph_counter = 0;
        /// <summary> Generates a grouped point graph, with a multiple values per point which will be linearly normalised to fit the same range. </summary>
        /// <param name="data"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static HtmlBuilder GroupedPointGraph(List<(string GroupLabel, List<(ReadFormat.General Label, List<double> Values)> Points)> data, List<string> header, string title, HtmlBuilder help, HtmlBuilder data_help, CommonPieces.AsideType asideType, string AssetsFolderName, List<string> location) {
            var html = new HtmlBuilder();
            if (data.Count == 0 || data.Any(a => a.Points.Count == 0)) {
                html.OpenAndClose(HtmlTag.em, "", "No data, or a dataset contains no data.");
                return html;
            }
            graph_counter++;
            string identifier = $"graph-{graph_counter}";
            int dimensions = header.Count;
            double[] max_values = new double[dimensions];
            double[] min_values = new double[dimensions];

            Array.Fill(max_values, Double.MinValue);
            Array.Fill(min_values, 0);

            foreach ((_, var group) in data) {
                foreach ((_, var values) in group) {
                    if (values.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({values.Count}) as the rest ({dimensions}).");
                    for (int i = 0; i < dimensions; i++) {
                        if (values[i] > max_values[i]) max_values[i] = values[i];
                        if (values[i] < min_values[i]) min_values[i] = values[i];
                    }
                }
            }

            // Create Legend
            var dataBuffer = new StringBuilder("Group\tPoint");

            html.Open(HtmlTag.div, $"class='graph point-graph' oncontextmenu='CopyGraphData()' aria-hidden='true'");
            html.TagWithHelp(HtmlTag.h2, title, help);
            html.CopyData(title + " (TSV)", data_help);

            for (int i = 0; i < dimensions; i++) {
                var check = i < 3 ? " checked " : "";
                html.Empty(HtmlTag.input, $"type='checkbox' class='show-data-{i}' id='{identifier}-show-data-{i}'{check}");
                html.OpenAndClose(HtmlTag.label, $"for='{identifier}-show-data-{i}'", header[i]);
                dataBuffer.Append($"\t\"{header[i]}\"");
            }

            html.Open(HtmlTag.div, "class='plot'");
            html.Open(HtmlTag.div, "class='y-axis'");
            html.OpenAndClose(HtmlTag.span, "class='max'", "100%");
            html.OpenAndClose(HtmlTag.span, "class='title'", "Linear Relative Value");
            html.OpenAndClose(HtmlTag.span, "class='min'", "0%");
            html.Close(HtmlTag.div);

            // Create Graph
            foreach (var group in data) {
                html.Open(HtmlTag.div, $"class='group' style='flex-grow:{group.Points.Count}'");

                foreach (var point in group.Points) {
                    var link = CommonPieces.GetAsideRawLink(point.Label, asideType, AssetsFolderName, location);
                    dataBuffer.Append($"\n\"{group.GroupLabel}\"\t{point.Label.Identifier}");
                    html.Open(HtmlTag.a, $"href='{link}' class='values'");
                    // Create Points
                    for (int i = 0; i < dimensions; i++) {
                        html.OpenAndClose(HtmlTag.span, $"class='point' style='--x:{(point.Values[i] - min_values[i]) / (max_values[i] - min_values[i])}'");
                        dataBuffer.Append($"\t{point.Values[i]}");
                    }
                    html.Close(HtmlTag.a);
                    html.Open(HtmlTag.span, "class='label'");
                    html.OpenAndClose(HtmlTag.a, $"href='{link}'", point.Label.Identifier);
                    html.Close(HtmlTag.span);
                }

                // Create Tooltip
                //buffer.Append($"<span class='tooltip'>{group.Label}");
                //for (int i = 0; i < dimensions; i++)
                //    buffer.Append($"<span class='dim'>{group.Dimensions[i]:G3}</span>");
                if (group.Points.Count > 2)
                    html.OpenAndClose(HtmlTag.span, "class='group-label'", group.GroupLabel);
                html.Close(HtmlTag.div);
            }

            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", dataBuffer.ToString());
            html.Close(HtmlTag.div);
            return html;
        }

        /// <summary> Render the given tree into a cladogram representation with circles with fill representing the scoring for each node. </summary>
        /// <param name="id"> Unique ID. </param>
        /// <param name="tree"> The tree to render. </param>
        /// <param name="templates"> The list of templates at the basis of this tree. </param>
        /// <param name="type"> The type of the leaf nodes. </param>
        /// <param name="AssetsFolderName"> The path to the assets folder from the current HTML. </param>
        /// <returns></returns>
        public static HtmlBuilder RenderTree(string id, PhylogeneticTree.ProteinHierarchyTree tree, List<Template> templates, CommonPieces.AsideType type, string AssetsFolderName, bool displayArea) {
            var html = new HtmlBuilder();
            const double xf = 30; // Width of the graph in pixels, do not forget to update the CSS when updating this value. The tree will be squeezed in the x dimension if the screen is not wide enough or just cap at this width if the screen is wide.
            const double yf = 22;   // Height of the labels
            const double radius = 10;  // Radius of the score circles
            const double stroke = 2;  // Stroke of the circles and tree lines, do not forget to update the CSS when updating this value.
            const double text_width = 100; // The width of the border around the leaf text

            (int, int, int, int, double, double) unpack((int, int, int, int, double, double, string) data) {
                return (data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6);
            }

            var pos = 0.0;
            var y_pos_tree = tree.DataTree.ReverseRemodel<(double Y, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) Scores, ReadFormat.General MetaData)>(
                (t, value) => ((t.Left.Value.Item2.Value.Y + t.Right.Value.Item2.Value.Y) / 2, unpack(value), null),
                leaf => {
                    pos += 1.0;
                    return (pos, unpack(leaf), templates.Find(t => t.MetaData.Identifier == leaf.Name).MetaData);
                });

            var columns = 0;
            var pos_tree = y_pos_tree.Remodel<(double X, double Y, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) Scores, ReadFormat.General MetaData)>((t, depth) => {
                if (depth > columns) columns = depth;
                return ((double)depth, t.Value.Y, t.Value.Scores, t.Value.MetaData);
            });

            var max_x = xf * (columns + 1);
            var max = tree.DataTree.Fold((0, 0, 0, 0, 0.0, 0.0), (acc, value) => (Math.Max(value.Score, acc.Item1), Math.Max(value.UniqueScore, acc.Item2), Math.Max(value.Matches, acc.Item3), Math.Max(value.UniqueMatches, acc.Item4), Math.Max(value.Area, acc.Item5), Math.Max(value.UniqueArea, acc.Item6)));

            html.Open(HtmlTag.div, "class='phylogenetic-tree'");
            html.UnsafeContent(CommonPieces.UserHelp("Tree", HTMLHelp.Tree.ToString()));

            var button_names = displayArea ? new string[] { "Score", "Matches", "Area" } : new string[] { "Score", "Matches" };
            for (int i = 0; i < button_names.Length; i++) {
                var check = i == 0 ? " checked " : "";
                html.Empty(HtmlTag.input, $"type='radio' class='show-data-{i}' name='{id}' id='{id}-{i}'{check}");
                html.OpenAndClose(HtmlTag.label, $"for='{id}-{i}'", button_names[i]);
            }
            html.OpenAndClose(HtmlTag.p, "class='legend'", "Cumulative value of all children (excluding unique)");
            html.OpenAndClose(HtmlTag.p, "class='legend unique'", "Cumulative value for unique matches");
            html.UnsafeContent(CommonPieces.CopyData("Tree (JSON)", HTMLHelp.TreeData.ToString()));
            html.Open(HtmlTag.div, "class='container'");
            html.Open(HtmlTag.div, $"class='tree' style='max-width:{max_x + radius + text_width}px'");
            var svg = new SvgBuilder();
            svg.Open(SvgTag.svg, $"viewBox='0 0 {max_x + radius + text_width} {((int)pos + 1) * yf}' width='100%' height='{((int)pos + 1) * yf}px' preserveAspectRatio='none'");

            string GetScores((int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) value, (int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea) max, bool unique) {
                double Normalise(double value) {
                    return double.IsNaN(value) ? 0.0 : value;
                }

                (double, double, double) values;
                if (!unique)
                    values = (Normalise((double)value.Score / max.Score), Normalise((double)value.Matches / max.Matches), Normalise((double)value.Area / max.Area));
                else
                    values = (Normalise((double)value.UniqueScore / max.UniqueScore), Normalise((double)value.UniqueMatches / max.UniqueMatches), Normalise((double)value.UniqueArea / max.UniqueArea));

                return $"--score:{values.Item1};--matches:{values.Item2};" + (displayArea ? $"--area:{values.Item3};" : "");
            }

            string DisplayInt(int value, int max) {
                if (double.IsNaN(max) || max == 0)
                    return $"{value:G3}";
                else
                    return $"{value:G3} ({(double)value / max:P})";
            }

            string DisplayDouble(double value, double max) {
                if (double.IsNaN(max) || max == 0)
                    return $"{value:G3}";
                else
                    return $"{value:G3} ({value / max:P})";
            }

            string DisplayStatDouble(string name, double value, double max) {
                return $"{name}: {DisplayDouble(value, max)}";
            }

            string DisplayStatInt(string name, int value, int max) {
                return $"{name}: {DisplayInt(value, max)}";
            }

            string DisplayStatDoubleUnique(string name, double value, double max, double unique, double unique_max) {
                return $"{name}: {DisplayDouble(value, max)} Unique: {DisplayDouble(unique, unique_max)}";
            }

            string DisplayStatIntUnique(string name, int value, int max, int unique, int unique_max) {
                return $"{name}: {DisplayInt(value, max)} Unique: {DisplayInt(unique, unique_max)}";
            }

            pos_tree.Apply(t => {
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
                svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-0'", DisplayStatInt("Score", t.Value.Scores.Score, max.Item1));
                svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-1'", DisplayStatInt("Matches", t.Value.Scores.Matches, max.Item3));
                if (displayArea) svg.OpenAndClose(SvgTag.text, $"x={x + radius + stroke * 2}px y={y}px class='info info-2'", DisplayStatDouble("Area", t.Value.Scores.Area, max.Item5));
                svg.Close(SvgTag.g);
            }, leaf => {
                var x = leaf.X * xf;
                var y = leaf.Y * yf;
                var end = max_x - radius - stroke;
                svg.Open(SvgTag.g);
                if (leaf.X != columns) svg.OpenAndClose(SvgTag.line, $"x1={x + stroke / 2}px y1={y}px x2={end - radius}px y2={y}px");
                svg.OpenAndClose(SvgTag.path, $"d='M {end} {y + radius} A {radius} {radius} 0 0 1 {end} {y - radius}' class='value' style='{GetScores(leaf.Scores, max, false)}'");
                svg.OpenAndClose(SvgTag.path, $"d='M {end} {y - radius} A {radius} {radius} 0 0 1 {end} {y + radius}' class='value unique' style='{GetScores(leaf.Scores, max, true)}'");
                svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-0' style='text-anchor:end'", DisplayStatIntUnique("Score", leaf.Scores.Score, max.Item1, leaf.Scores.UniqueScore, max.Item2));
                svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-1' style='text-anchor:end'", DisplayStatIntUnique("Matches", leaf.Scores.Matches, max.Item3, leaf.Scores.UniqueMatches, max.Item4));
                if (displayArea) svg.OpenAndClose(SvgTag.text, $"x={end - radius - stroke * 2}px y={y}px class='info info-2' style='text-anchor:end'", DisplayStatDoubleUnique("Area", leaf.Scores.Area, max.Item5, leaf.Scores.UniqueArea, max.Item6));
                svg.Open(SvgTag.a, $"class='info-link' id='tree-leaf-{CommonPieces.GetAsideIdentifier(leaf.MetaData, false)}' href='{CommonPieces.GetAsideRawLink(leaf.MetaData, type, AssetsFolderName)}' target='_blank'");
                svg.OpenAndClose(SvgTag.rect, $"x={max_x + radius}px y={y - yf / 2 + stroke}px width={text_width}px height={yf - stroke * 2}px rx=3.2px");
                svg.OpenAndClose(SvgTag.text, $"x={max_x + radius + stroke * 2}px y={y + 1}px", CommonPieces.GetAsideIdentifier(leaf.MetaData, true));
                svg.Close(SvgTag.a);
                svg.Close(SvgTag.g);
            });

            var json = tree.DataTree.Fold((value, ad, a, bd, b) => {
                var obj = new JsonObject();
                obj.Keys.Add("left", a);
                obj.Keys.Add("leftDistance", new JsonNumber(ad));
                obj.Keys.Add("right", b);
                obj.Keys.Add("rightDistance", new JsonNumber(bd));
                obj.Keys.Add("score", new JsonNumber(value.Score));
                obj.Keys.Add("matches", new JsonNumber(value.Matches));
                if (displayArea) obj.Keys.Add("area", new JsonNumber(value.Area));
                return obj;
            }, value => {
                var obj = new JsonObject();
                obj.Keys.Add("name", new JsonString(value.Name));
                obj.Keys.Add("score", new JsonNumber(value.Score));
                obj.Keys.Add("uniqueScore", new JsonNumber(value.UniqueScore));
                obj.Keys.Add("matches", new JsonNumber(value.Matches));
                obj.Keys.Add("uniqueMatches", new JsonNumber(value.UniqueMatches));
                if (displayArea) obj.Keys.Add("area", new JsonNumber(value.Area));
                if (displayArea) obj.Keys.Add("uniqueArea", new JsonNumber(value.UniqueArea));
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

            return html;
        }

        public static HtmlBuilder DensityCurve(List<double> data, HtmlBuilder title, HtmlBuilder help = null, HtmlBuilder data_help = null) {
            if (data.Count == 0) return new HtmlBuilder();
            data.Sort();
            var min_value = data.Min();
            var max_value = data.Max();
            const int STEPS = 256;
            var mean = data.Average();
            var stdev = Math.Sqrt(data.Select(point => Math.Pow(mean - point, 2)).Sum() / data.Count());
            var iqr = data.TakeLast(data.Count() / 2).Average() - data.Take(data.Count() / 2).Average();
            double h = 0.9 * Math.Min(stdev, iqr / 1.34) * Math.Pow(data.Count(), -1 / 5);
            var densities = new double[STEPS];

            double gaussian_kernel(double x) {
                return 1 / Math.Sqrt(2.0 * Math.PI) * Math.Exp(-Math.Pow(x, 2) / 2);
            }

            double KDE(double x) {
                return 1 / (data.Count * h) * data.Select(i => gaussian_kernel((x - i) / h)).Sum();
            }

            for (int i = 0; i < STEPS; i++) {
                densities[i] = KDE(min_value + (max_value - min_value) / (STEPS - 1) * i);
            }
            var max_density = densities.Max();
            var path = new StringBuilder();
            for (int i = 0; i < STEPS; i++) {
                if (i != 0) path.Append(" L ");
                path.Append($"{(double)i} {(max_density - densities[i]) / max_density * 100}");
            }
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='graph'");
            if (title != null)
                if (help != null) {
                    html.Open(HtmlTag.h2);
                    html.Add(title);
                    html.UserHelp("Bargraph", help);
                    html.Close(HtmlTag.h2);
                } else
                    html.OpenAndClose(HtmlTag.h2, "class='title'", title);
            html.Open(HtmlTag.div, "class='histogram density' oncontextmenu='CopyGraphData()' aria-hidden='true'");

            // Y axis
            html.OpenAndClose(HtmlTag.span, "class='y-axis'");
            //html.OpenAndClose(HtmlTag.span, "class='max'", densities.Max().ToString("G3"));
            //html.OpenAndClose(HtmlTag.span, "class='min'", "0");
            //html.Close(HtmlTag.span);
            html.OpenAndClose(HtmlTag.span, "class='empty'");

            var svg = new SvgBuilder();
            svg.Open(SvgTag.svg, $"viewbox='0 -1 {STEPS - 1} 100' preserveAspectRatio='none'");
            svg.OpenAndClose(SvgTag.path, $"class='line' d='M {path}'");
            svg.OpenAndClose(SvgTag.path, $"class='volume' d='M 0 100 L {path} L {STEPS - 1} 100 Z'");
            svg.Close(SvgTag.svg);
            html.Add(svg);
            html.Open(HtmlTag.span, "class='x-axis'");
            html.OpenAndClose(HtmlTag.span, "class='min'", min_value.ToString());
            html.OpenAndClose(HtmlTag.span, "class='middle'", ((min_value * 3 + max_value) / 4).ToString());
            html.OpenAndClose(HtmlTag.span, "class='middle'", ((min_value + max_value) / 2).ToString());
            html.OpenAndClose(HtmlTag.span, "class='middle'", ((min_value + max_value * 3) / 4).ToString());
            html.OpenAndClose(HtmlTag.span, "class='max'", max_value.ToString());
            html.Close(HtmlTag.span);

            html.Close(HtmlTag.div);
            // graph data if (title != null)
            // graph data     html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", dataBuffer.ToString());
            html.Close(HtmlTag.div);

            return html;
        }
    }
}