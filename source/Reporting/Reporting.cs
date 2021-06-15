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
        public readonly List<(string Sequence, MetaData.IMetaData MetaData)> Input;
        public readonly List<(string, List<Segment>)> Segments;
        public readonly List<Segment> RecombinedSegment;
        public readonly ParsedFile BatchFile;
        public readonly string Runname;
        public ReportInputParameters(List<(string, MetaData.IMetaData)> input, List<(string, List<Segment>)> segments = null, List<Segment> recombinedsegment = null, ParsedFile batchFile = null, string runname = "Runname")
        {
            Input = input;
            Segments = segments;
            RecombinedSegment = recombinedsegment;
            BatchFile = batchFile;
            Runname = runname;
        }
    }
    /// <summary>
    /// To be a basepoint for any reporting options, handling all the metadata.
    /// </summary>
    public abstract class Report
    {
        protected readonly int MaxThreads;
        public readonly ParsedFile BatchFile;
        public readonly ReportInputParameters Parameters;
        /// <summary>
        /// To create a report, gets all metadata.
        /// </summary>
        /// /// <param name="parameters">The parameters for this report.</param>
        public Report(ReportInputParameters parameters, int max_threads)
        {
            MaxThreads = max_threads;
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
            buffer = buffer.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
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

                File.AppendAllText(filename, buffer);
            }
            else
            {
                new InputNameSpace.ErrorMessage(filename, "File could not be saved", $"The drive '{drive}:\\' is not mounted.");
            }
        }
    }
}