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
    /// <summary>
    /// A CSV report.
    /// </summary>
    class CSVReport : Report
    {
        /// <summary>
        /// To retrieve all metadata.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public CSVReport(ReportInputParameters parameters, int max_threads) : base(parameters, max_threads) { }
        public override string Create()
        {
            return "";
        }

        /// <summary>
        /// Prepares the file to be used for a CSV report.
        /// </summary>
        /// <param name="filename">The path to the file.</param>
        public void PrepareCSVFile(string filename)
        {
            StreamWriter sw = File.CreateText(filename);
            string link = singleRun.Report.Where(a => a is RunParameters.Report.HTML).Count() > 0 ? "Hyperlink(s) to the report(s);" : "";
            sw.Write($"sep=;\nID;Data file;Alphabet;K-mer length;Minimal Homology;Duplicate Threshold;Reads;Total nodes;Average Sequence Length;Average depth of coverage;Mean Connectivity;Total time;{link}\n");
            sw.Close();
        }

        /// <summary>
        /// The key to get access to write to the CSV file.
        /// </summary>
        static readonly object CSVKey = new Object();

        /// <summary> Fill metainformation in a CSV line and append it to the given file. </summary>
        /// <param name="ID">ID of the run to recognize it in the CSV file. </param>
        /// <param name="filename"> The file to which to append the CSV line to. </param>
        public void CreateCSVLine(string ID, string filename)
        {
            // HYPERLINK
            int totallength = condensed_graph.Aggregate(0, (a, b) => (a + b.Sequence.Count()));
            int totalreadslength = reads.Aggregate(0, (a, b) => a + b.Length) * (singleRun.Reverse ? 2 : 1);
            int totalnodes = condensed_graph.Count();
            string data = singleRun.Input.Count() == 1 ? singleRun.Input[0].Item2.File.Name : "Group";
            string link = singleRun.Report.Where(a => a is RunParameters.Report.HTML).Count() > 0 ? singleRun.Report.Where(a => a is RunParameters.Report.HTML).Aggregate("", (a, b) => (a + "=HYPERLINK(\"" + Path.GetFullPath(b.CreateName(singleRun)) + "\");")) : "";
            string line = $"{ID};{data};{singleRun.Alphabet.Alphabet};{singleRun.K};{singleRun.MinimalHomology};{singleRun.DuplicateThreshold};{meta_data.reads};{totalnodes};{(double)totallength / totalnodes};{(double)totalreadslength / totallength};{(double)condensed_graph.Aggregate(0L, (a, b) => a + b.ForwardEdges.Count() + b.BackwardEdges.Count()) / 2L / condensed_graph.Count()};{meta_data.total_time};{link}\n";

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
}