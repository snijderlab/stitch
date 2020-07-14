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
    public struct ReportInputParameters
    {
        public readonly Assembler Assembler;
        public readonly List<TemplateDatabase> TemplateDatabases;
        public readonly TemplateDatabase RecombinedDatabase;
        public readonly List<TemplateDatabase> RecombinationDatabases;
        public readonly TemplateDatabase ReadAlignment;
        public readonly ParsedFile BatchFile;
        public readonly List<GraphPath> Paths;
        public readonly string Runname;
        public ReportInputParameters(Assembler assm, List<TemplateDatabase> databases = null, TemplateDatabase recombineddatabase = null, List<TemplateDatabase> recombinationdatabases = null, TemplateDatabase readAlignment = null, ParsedFile batchFile = null, string runname = "Runname")
        {
            Assembler = assm;
            TemplateDatabases = databases;
            RecombinedDatabase = recombineddatabase;
            RecombinationDatabases = recombinationdatabases;
            ReadAlignment = readAlignment;
            BatchFile = batchFile;
            Runname = runname;
            Paths = Assembler.GetAllPaths();
        }
    }
    /// <summary>
    /// To be a basepoint for any reporting options, handling all the metadata.
    /// </summary>
    public abstract class Report
    {
        /// <summary>
        /// The reads used as input in the run.
        /// </summary>
        protected List<AminoAcid[]> reads;
        /// <summary>
        /// Possibly the reads from PEAKS used in the run.
        /// </summary>
        protected List<MetaData.IMetaData> reads_metadata;
        protected readonly int MaxThreads;
        public readonly ParsedFile BatchFile;
        public readonly ReportInputParameters Parameters;
        /// <summary>
        /// To create a report, gets all metadata.
        /// </summary>
        /// /// <param name="parameters">The parameters for this report.</param>
        public Report(ReportInputParameters parameters, int max_threads)
        {
            reads = parameters.Assembler.reads;
            reads_metadata = parameters.Assembler.reads_metadata;
            MaxThreads = max_threads;

            if (parameters.ReadAlignment != null)
            {
                reads.AddRange(parameters.Assembler.shortReads.Select(a => a.Item1));
                reads_metadata.AddRange(parameters.Assembler.shortReads.Select(a => a.Item2));
            }

            BatchFile = parameters.BatchFile;
            Parameters = parameters;
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var buffer = Create();
            stopwatch.Stop();
            buffer = buffer.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds - Parameters.Assembler.meta_data.drawingtime}");
            SaveAndCreateDirectories(filename, buffer);
        }

        protected void SaveAndCreateDirectories(string filename, string buffer)
        {
            var pieces = filename.Split(new char[] { '\\', '/' });
            var drive = pieces[0].Split(':')[0];

            if (Directory.GetLogicalDrives().Contains($"{drive}:\\"))
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetParent(filename).FullName);
                }
                catch
                {
                    new InputNameSpace.ErrorMessage(filename, "Directory could not be created", $"The directory '{Directory.GetParent(filename).FullName}' could not be created.").Print();
                }

                StreamWriter sw = File.CreateText(filename);
                sw.Write(buffer);
                sw.Close();
            }
            else
            {
                new InputNameSpace.ErrorMessage(filename, "File could not be saved", $"The drive '{drive}:\\' is not mounted.");
            }
        }
        /// <summary>
        /// Retrieves all paths containing the specified condensed node id.
        /// </summary>
        /// <param name="id">The id of the specified node</param>
        /// <returns>A list of all path ids</returns>
        protected List<GraphPath> AllPathsContaining(int id)
        {
            var output = new List<GraphPath>();
            foreach (var path in Parameters.Paths)
            {
                foreach (var node in path.Nodes)
                {
                    if (node.Index == id)
                    {
                        output.Add(path);
                        break;
                    }
                }
            }
            return output;
        }
    }
}