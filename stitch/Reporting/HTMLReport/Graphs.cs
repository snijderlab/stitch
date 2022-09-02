using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyNameSpace;

namespace HTMLNameSpace
{
    static class HTMLGraph
    {
        public static List<(string, double)> AnnotateDOCData(List<double> data, int offset = 0)
        {
            int label = HelperFunctionality.RoundToHumanLogicalFactor(data.Count / 10);
            if (label == 0) label = 1;
            var annotated = new List<(string, double)>();
            for (int i = offset; i < data.Count + offset; i++)
                annotated.Add((i % label == 0 ? $"{i:G3}" : "", data[i - offset]));

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

            var labeled = new (string, List<double>)[bins];

            double low = min;
            for (int i = 0; i < bins; i++)
            {
                labeled[i] = ($"{low:G3}-{low + step:G3}", Enumerable.Repeat(0.0, data.Count).ToList());
                low += step;
            }

            for (int setindex = 0; setindex < data.Count; setindex++)
            {
                foreach (var item in data[setindex].Data)
                {
                    int bin = (int)Math.Floor((item - min) / step);

                    if (bin > bins - 1) bin = bins - 1;
                    else if (bin < 0) bin = 0;

                    labeled[bin].Item2[setindex]++;
                }
            }

            GroupedBargraph(buffer, labeled.ToList(), data.Select(a => (a.Label, (uint)0)).ToList(), title);
        }

        public static void Histogram(StringBuilder buffer, List<double> data, string title, int bins = 10)
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

            var labeled = new (string, double)[bins];

            double low = min;
            for (int i = 0; i < bins; i++)
            {
                labeled[i] = ($"{low:G3}-{low + step:G3}", 0);
                low += step;
            }

            foreach (var item in data)
            {
                int bin = (int)Math.Floor((item - min) / step);

                if (bin > bins - 1) bin = bins - 1;
                else if (bin < 0) bin = 0;

                labeled[bin].Item2++;
            }

            Bargraph(buffer, labeled.ToList(), title);
        }

        public static void Bargraph(StringBuilder buffer, List<(string Label, double Value)> data, string title = null, string help = null, int factor = 2)
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
            buffer.Append("<div class='copy-data' onclick='CopyGraphData()'>Copy Data</div>");

            double max = Math.Ceiling(data.Select(a => a.Value).Max() / factor) * factor;
            double min = Math.Ceiling(data.Select(a => a.Value).Min() / factor) * factor;

            if (min < 0)
            {
                max = Math.Max(max, 0); // Make sure to not start graphs below zero as this breaks the layout
                buffer.Append($"<div class='histogram negative' oncontextmenu='CopyGraphData()' style='grid-template-rows:{max / (max - min) * 150}px {min / (min - max) * 150}px 1fr'>");
                // Y axis
                buffer.Append($"<span class='yaxis'><span class='max'>{max:G3}</span><span class='min'>{min:G3}</span></span><span class='empty'></span>");

                // Data
                foreach (var set in data)
                {
                    if (set.Value >= 0)
                        buffer.Append($"<span class='bar' style='height:{set.Value / max * 100}%'><span>{set.Value:G3}</span></span><span class='empty'></span><span class='label'>{set.Label}</span>");
                    else
                        buffer.Append($"<span class='empty'></span><span class='bar negative' style='height:{set.Value / min * 100}%'><span>{set.Value:G3}</span></span><span class='label'>{set.Label}</span>");
                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value}");
                }
            }
            else
            {
                min = 0; // always start graphs at 0 
                buffer.Append("<div class='histogram' oncontextmenu='CopyGraphData()'>");

                // Y axis
                buffer.Append($"<span class='yaxis'><span class='max'>{max:G3}</span><span class='min'>0</span></span><span class='empty'></span>");

                // Data
                foreach (var set in data)
                {
                    string height = (set.Value / max * 100).ToString();
                    buffer.Append($"<span class='bar' style='height:{height}%'><span>{set.Value:G3}</span></span><span class='label'>{set.Label}</span>");
                    dataBuffer.Append($"\n\"{set.Label}\"\t{set.Value}");
                }

            }
            buffer.Append($"</div><textarea type='text' class='graph-data' aria-hidden='true'>{dataBuffer.ToString()}</textarea></div>");
        }

        static void NegativeBargraph(StringBuilder buffer, List<(string Label, double Value)> data, double max, double min)
        {

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
            double[] maxvalues = new double[dimensions];
            double[] minvalues = new double[dimensions];

            Array.Fill(maxvalues, Double.MinValue);
            Array.Fill(minvalues, Double.MaxValue);

            foreach ((_, var dims) in data)
            {
                if (dims.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({dims.Count}) as the rest ({dimensions}).");
                for (int i = 0; i < dimensions; i++)
                {
                    if (dims[i] > maxvalues[i]) maxvalues[i] = dims[i];
                    if (dims[i] < minvalues[i]) minvalues[i] = dims[i];
                }
            }

            int dimensionDimensions = (int)header.Select(a => a.Dimension).Max();
            double[] dimensionMax = new double[dimensionDimensions + 1];
            double[] dimensionMin = new double[dimensionDimensions + 1];

            for (int i = 0; i < dimensions; i++)
            {
                var dimensionIndex = header[i].Dimension;

                if (maxvalues[i] > dimensionMax[dimensionIndex]) dimensionMax[dimensionIndex] = maxvalues[i];
                if (minvalues[i] > dimensionMin[dimensionIndex]) dimensionMin[dimensionIndex] = maxvalues[i];
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
            buffer.Append("</div><div class='copy-data' onclick='CopyGraphData()'>Copy Data</div><div class='histogram grouped' oncontextmenu='CopyGraphData()'>");
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

            buffer.Append($"</div><textarea type='text' class='graph-data' aria-hidden='true'>{dataBuffer.ToString()}</textarea></div>");
        }

        static int graph_counter = 0;
        /// <summary>
        /// Generates a grouped point graph, with a multiple values per point which will be linearly normalised to fit the same range.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static void GroupedPointGraph(StringBuilder buffer, List<(string GroupLabel, List<(string Label, List<double> Values)> Points)> data, List<string> header, string title)
        {
            if (data.Count == 0 || data.Any(a => a.Points.Count == 0))
            {
                buffer.Append("<em>No data, or a dataset contains no data.</em>");
                return;
            }
            graph_counter++;
            string identifier = $"graph-{graph_counter}";
            int dimensions = header.Count;
            double[] maxvalues = new double[dimensions];
            double[] minvalues = new double[dimensions];

            Array.Fill(maxvalues, Double.MinValue);
            Array.Fill(minvalues, Double.MaxValue);

            foreach ((_, var group) in data)
            {
                foreach ((_, var values) in group)
                {
                    if (values.Count != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({values.Count}) as the rest ({dimensions}).");
                    for (int i = 0; i < dimensions; i++)
                    {
                        if (values[i] > maxvalues[i]) maxvalues[i] = values[i];
                        if (values[i] < minvalues[i]) minvalues[i] = values[i];
                    }
                }
            }

            // Create Legend
            var dataBuffer = new StringBuilder("Group\tPoint");

            buffer.Append($"<div class='graph point-graph' oncontextmenu='CopyGraphData()'><h2 class='title'>{title}</h2>");
            for (int i = 0; i < dimensions; i++)
            {
                var check = i < 3 ? " checked " : "";
                buffer.Append($"<input type='checkbox' class='showdata-{i}' id='{identifier}-showdata-{i}'{check}/>");
                buffer.Append($"<label for='{identifier}-showdata-{i}'>{header[i]}</label>");
                dataBuffer.Append($"\t\"{header[i]}\"");
            }

            buffer.Append("<div class='copy-data' onclick='CopyGraphData()'>Copy Data</div><div class='plot'><div class='yaxis'><span class='max'>100%</span><span class='title'>Linear Relative Value</span><span class='min'>0%</span></div>");
            // Create Graph
            foreach (var group in data)
            {
                buffer.Append($"<div class='group' style='flex-grow:{group.Points.Count}'>");

                foreach (var point in group.Points)
                {
                    dataBuffer.Append($"\n\"{group.GroupLabel}\"\t{point.Label}");
                    buffer.Append($"<a href='#{identifier}-{point.Label}' class='values'>");
                    // Create Points
                    for (int i = 0; i < dimensions; i++)
                    {
                        buffer.Append($"<span class='point' style='--x:{(point.Values[i] - minvalues[i]) / (maxvalues[i] - minvalues[1])}'></span>");
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

            buffer.Append($"</div><textarea type='text' class='graph-data' aria-hidden='true'>{dataBuffer.ToString()}</textarea></div>");
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
            var buffer = new StringBuilder();
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

            buffer.Append($"<div class='phylogenetictree'>");

            var button_names = new string[] { "Score", "Matches", "Area" };
            for (int i = 0; i < button_names.Length; i++)
            {
                var check = i == 0 ? " checked " : "";
                buffer.Append($"<input type='radio' class='showdata-{i}' name='{id}' id='{id}-{i}'{check}/>");
                buffer.Append($"<label for='{id}-{i}'>{button_names[i]}</label>");
            }
            buffer.Append("<p class='legend'>Cumulative value of all children (excluding unique)</p><p class='legend unique'>Cumulative value for unique matches</p>");
            buffer.Append($"<div class='container'><div class='tree' style='max-width:{max_x + radius + text_width}px'><svg viewBox='0 0 {max_x + radius + text_width} {((int)pos + 1) * yf}' width='100%' height='{((int)pos + 1) * yf}px' preserveAspectRatio='none'>");

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
                buffer.Append("<g>");
                buffer.Append($"<line x1={x}px y1={ly}px x2={x}px y2={y - radius}px />");
                buffer.Append($"<line x1={x}px y1={y + radius}px x2={x}px y2={ry}px />");
                buffer.Append($"<line x1={x - stroke / 2}px y1={ly}px x2={x1}px y2={ly}px />");
                buffer.Append($"<line x1={x - stroke / 2}px y1={ry}px x2={x1}px y2={ry}px />");
                buffer.Append($"<circle cx={x}px cy={y}px r={radius}px class='value' style='{GetScores(t.Value.Scores, max, false)}'/>");
                buffer.Append($"<text x={x + radius + stroke * 2}px y={y}px class='info info-0'>Score: {t.Value.Scores.Score} ({(double)t.Value.Scores.Score / max.Item1:P})</text>");
                buffer.Append($"<text x={x + radius + stroke * 2}px y={y}px class='info info-1'>Matches: {t.Value.Scores.Matches} ({(double)t.Value.Scores.Matches / max.Item3:P})</text>");
                buffer.Append($"<text x={x + radius + stroke * 2}px y={y}px class='info info-2'>Area: {t.Value.Scores.Area:G3} ({(double)t.Value.Scores.Area / max.Item5:P})</text>");
                buffer.Append("</g>");
            }, leaf =>
            {
                var x = leaf.X * xf;
                var y = leaf.Y * yf;
                var end = max_x - radius - stroke;
                buffer.Append("<g>");
                if (leaf.X != columns) buffer.Append($"<line x1={x + stroke / 2}px y1={y}px x2={end - radius}px y2={y}px />");
                buffer.Append($"<path d='M {end} {y + radius} A {radius} {radius} 0 0 1 {end} {y - radius}' class='value' style='{GetScores(leaf.Scores, max, false)}'/>");
                buffer.Append($"<path d='M {end} {y - radius} A {radius} {radius} 0 0 1 {end} {y + radius}' class='value unique' style='{GetScores(leaf.Scores, max, true)}'/>");
                buffer.Append($"<text x={end - radius - stroke * 2}px y={y}px class='info info-0' style='text-anchor:end'>Score: {leaf.Scores.Score} ({(double)leaf.Scores.Score / max.Item1:P}) Unique: {leaf.Scores.UniqueScore} ({(double)leaf.Scores.UniqueScore / max.Item2:P})</text>");
                buffer.Append($"<text x={end - radius - stroke * 2}px y={y}px class='info info-1' style='text-anchor:end'>Matches: {leaf.Scores.Matches} ({(double)leaf.Scores.Matches / max.Item3:P}) Unique: {leaf.Scores.UniqueMatches} ({(double)leaf.Scores.UniqueMatches / max.Item4:P})</text>");
                buffer.Append($"<text x={end - radius - stroke * 2}px y={y}px class='info info-2' style='text-anchor:end'>Area: {leaf.Scores.Area:G3} ({(double)leaf.Scores.Area / max.Item5:P}) Unique: {leaf.Scores.UniqueArea:G3} ({(double)leaf.Scores.UniqueArea / max.Item6:P})</text>");
                buffer.Append($"<a class='info-link' href='{CommonPieces.GetAsideRawLink(leaf.MetaData, type, AssetsFolderName)}' target='_blank'>");
                buffer.Append($"<rect x={max_x + radius}px y={y - yf / 2 + stroke}px width={text_width}px height={yf - stroke * 2}px rx=3.2px></rect>");
                buffer.Append($"<text x={max_x + radius + stroke * 2}px y={y + 1}px>{CommonPieces.GetAsideIdentifier(leaf.MetaData, true)}</text>");
                buffer.Append("</a></g>");
            });

            buffer.Append("</svg></div>");//<div class='names'>");
            //tree.OriginalTree.Apply(t => { }, name => buffer.Append(CommonPieces.GetAsideLink(templates.Find(t => t.MetaData.Identifier == name).MetaData, type, AssetsFolderName)));
            buffer.Append("</div></div>");//</div>");

            return buffer.ToString();
        }
    }
}