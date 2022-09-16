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
using System.Collections.ObjectModel;

namespace AssemblyNameSpace
{
    /// <summary>To save all parameters for the generation of a report in one place</summary>
    public readonly struct ReportInputParameters
    {
        public readonly ReadOnlyCollection<(string Sequence, ReadMetaData.IMetaData MetaData)> Input;
        public readonly ReadOnlyCollection<(string, List<Segment>)> Segments;
        public readonly ReadOnlyCollection<Segment> RecombinedSegment;
        public readonly ParsedFile BatchFile;
        public readonly RunVariables runVariables;
        public readonly string Runname;

        public readonly Dictionary<ReadMetaData.Peaks, HeckLib.chemistry.PeptideFragment[]> Fragments;
        public ReportInputParameters(List<(string, ReadMetaData.IMetaData)> input, List<(string, List<Segment>)> segments, List<Segment> recombinedsegment, ParsedFile batchFile, RunVariables variables, string runname, Dictionary<ReadMetaData.Peaks, HeckLib.chemistry.PeptideFragment[]> fragments)
        {
            Input = input.AsReadOnly();
            Segments = segments.AsReadOnly();
            RecombinedSegment = recombinedsegment.AsReadOnly();
            BatchFile = batchFile;
            runVariables = variables;
            Runname = runname;
            Fragments = fragments;
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
        public Report(ReportInputParameters parameters, int maxThreads)
        {
            MaxThreads = maxThreads;
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
            try
            {
                Directory.CreateDirectory(Directory.GetParent(filename).FullName);
            }
            catch
            {
                new InputNameSpace.ErrorMessage(filename, "Directory could not be created", $"The directory '{Directory.GetParent(filename).FullName}' could not be created.").Print();
                throw new Exception("Could not save file, see error message above");
            }

            File.WriteAllText(filename, buffer);
        }

        protected Task SaveAndCreateDirectoriesAsync(string filename, string buffer)
        {
            try
            {
                Directory.CreateDirectory(Directory.GetParent(filename).FullName);
            }
            catch
            {
                new InputNameSpace.ErrorMessage(filename, "Directory could not be created", $"The directory '{Directory.GetParent(filename).FullName}' could not be created.").Print();
                throw new Exception("Could not save file, see error message above");
            }

            if (File.Exists(filename))
            {
                new InputNameSpace.ErrorMessage(filename, "File already exists", "All filenames are supposed to be unique to prevent data races.").Print();
                throw new Exception("Could not save file, see error message above");
            }

            return File.WriteAllTextAsync(filename, buffer);
        }
    }
}