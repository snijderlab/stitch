using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyNameSpace
{
    static class HTMLGraph
    {
        public static List<(string, double)> AnnotateDOCData(List<double> data, int offset = 0, int factor = 10)
        {
            int label = HelperFunctionality.RoundToHumanLogicalFactor(data.Count / 10);
            if (label == 0) label = 1;
            var annotated = new List<(string, double)>();
            for (int i = offset; i < data.Count + offset; i++)
                annotated.Add((i % label == 0 ? $"{i:G3}" : "", data[i]));

            return annotated;
        }
        public static string GroupedHistogram(List<(List<double> Data, string Label)> data, int bins = 10)
        {
            if (data.Count == 0 || data.Any(a => a.Item1.Count == 0)) return "<em>No data, or a dataset contains no data.</em>";
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

            return GroupedBargraph(labeled.ToList(), data.Select(a => (a.Label, (uint)0)).ToList());
        }

        public static string Histogram(List<double> data, int bins = 10)
        {
            if (data.Count == 0) return "<em>No data.</em>";
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

            return Bargraph(labeled.ToList());
        }

        public static string Bargraph(List<(string, double)> data, int factor = 2, bool baseYMinOnData = false)
        {
            if (data.Count == 0) return "<em>No data.</em>";
            var buffer = new StringBuilder();
            buffer.Append("<div class='histogram'>");

            double max = Math.Ceiling(data.Select(a => a.Item2).Max() / factor) * factor;
            double min = 0;
            if (baseYMinOnData) min = data.Select(a => a.Item2).Min();

            // Y axis
            buffer.Append($"<span class='yaxis'><span class='max'>{max:G3}</span><span class='min'>{min:G3}</span></span><span class='empty'></span>");

            // Data
            foreach (var set in data)
            {
                string height = ((set.Item2 - min) / max * 100).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                buffer.Append($"<span class='bar' style='height:{height}%'><span>{set.Item2:G3}</span></span><span class='label'>{set.Item1}</span>");
            }

            buffer.Append("</div>");
            return buffer.ToString();
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
        public static string GroupedBargraph(List<(string Label, List<double> Dimensions)> data, List<(string Label, uint Dimension)> header, int factor = 2, bool baseYMinOnData = false)
        {
            if (data.Count == 0 || data.Any(a => a.Dimensions.Count == 0)) return "<em>No data, or a dataset contains no data.</em>";
            int dimensions = header.Count();
            double[] maxvalues = new double[dimensions];
            double[] minvalues = new double[dimensions];

            Array.Fill(maxvalues, Double.MinValue);
            Array.Fill(minvalues, Double.MaxValue);

            foreach ((_, var dims) in data)
            {
                if (dims.Count() != dimensions) throw new ArgumentException($"Row does not have the correct amount of dimensions ({dims.Count()}) as the rest ({dimensions}).");
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
            var buffer = new StringBuilder();
            buffer.Append("<div class='histogram-header'>");
            for (int i = 0; i < dimensions; i++)
                buffer.Append($"<span>{header[i].Label}</span>");

            // Create Graph
            buffer.Append("</div><div class='histogram grouped'>");
            foreach (var set in data)
            {
                buffer.Append($"<span class='group'>");

                // Create Bars
                for (int i = 0; i < dimensions; i++)
                {
                    var dimensionIndex = header[i].Dimension;
                    string height = ((set.Dimensions[i] - dimensionMin[dimensionIndex]) / (dimensionMax[dimensionIndex] - dimensionMin[dimensionIndex]) * 100).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                    buffer.Append($"<span class='bar' style='height:{height}%'></span>");
                }

                //Create Tooltip
                buffer.Append($"<span class='tooltip'>{set.Label}");
                for (int i = 0; i < dimensions; i++)
                    buffer.Append($"<span class='dim'>{set.Dimensions[i]:G3}</span>");

                // Create Label
                buffer.Append($"</span></span><span class='label'>{set.Label}</span>");
            }

            buffer.Append("</div>");
            return buffer.ToString();
        }
    }
}