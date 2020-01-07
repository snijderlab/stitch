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
    struct ReportInputParameters
    {
        public Assembler assembler;
        public RunParameters.SingleRun singleRun;
        public List<TemplateDatabase> templateDatabases;
        public ReportInputParameters(Assembler assm, RunParameters.SingleRun run, List<TemplateDatabase> databases)
        {
            assembler = assm;
            singleRun = run;
            templateDatabases = databases;
        }
    }
    /// <summary>
    /// To be a basepoint for any reporting options, handling all the metadata.
    /// </summary>
    abstract class Report
    {
        /// <summary>
        /// The condensed graph.
        /// </summary>
        protected List<CondensedNode> condensed_graph;
        /// <summary>
        /// The uncondensed graph.
        /// </summary>
        protected Node[] graph;
        /// <summary>
        /// The metadata of the run.
        /// </summary>
        protected MetaInformation meta_data;
        /// <summary>
        /// The reads used as input in the run.
        /// </summary>
        protected List<AminoAcid[]> reads;
        /// <summary>
        /// Possibly the reads from PEAKS used in the run.
        /// </summary>
        protected List<MetaData.IMetaData> reads_metadata;
        /// <summary>
        /// The alphabet used in the assembly
        /// </summary>
        protected Alphabet alphabet;
        /// <summary>
        /// The runparameters
        /// </summary>
        protected RunParameters.SingleRun singleRun;
        protected List<TemplateDatabase> templates;
        protected List<(int, List<int>)> paths;
        /// <summary>
        /// To create a report, gets all metadata.
        /// </summary>
        /// <param name="parameters">The parameters for this report.</param>
        public Report(ReportInputParameters parameters)
        {
            condensed_graph = parameters.assembler.condensed_graph;
            graph = parameters.assembler.graph;
            meta_data = parameters.assembler.meta_data;
            reads = parameters.assembler.reads;
            reads_metadata = parameters.assembler.reads_metadata;
            alphabet = parameters.assembler.alphabet;
            singleRun = parameters.singleRun;
            templates = parameters.templateDatabases;
            paths = parameters.assembler.GetAllPaths();
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
            buffer = buffer.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
            StreamWriter sw = File.CreateText(filename);
            sw.Write(buffer);
            sw.Close();
        }
    }
}