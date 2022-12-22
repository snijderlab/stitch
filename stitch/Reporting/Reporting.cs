using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;

namespace Stitch {
    /// <summary>To save all parameters for the generation of a report in one place</summary>
    public readonly struct ReportInputParameters {
        public readonly ReadOnlyCollection<ReadFormat.General> Input;
        public readonly ReadOnlyCollection<(string Name, List<Segment> Segments)> Groups;
        public readonly ReadOnlyCollection<Segment> RecombinedSegment;
        public readonly ParsedFile BatchFile;
        public readonly ExtraArguments runVariables;
        public readonly string Runname;
        [JsonIgnore]
        public readonly Dictionary<string, List<AnnotatedSpectrumMatch>> Fragments;
        public ReportInputParameters(List<ReadFormat.General> input, List<(string, List<Segment>)> segments, List<Segment> recombined_segment, ParsedFile batchFile, ExtraArguments variables, string runname, Dictionary<string, List<AnnotatedSpectrumMatch>> fragments) {
            Input = input.AsReadOnly();
            Groups = segments.AsReadOnly();
            RecombinedSegment = recombined_segment.AsReadOnly();
            BatchFile = batchFile;
            runVariables = variables;
            Runname = runname;
            Fragments = fragments;
        }
    }
    /// <summary> To be a base point for any reporting options, handling all the metadata. </summary>
    public abstract class Report {
        protected readonly int MaxThreads;
        public readonly ParsedFile BatchFile;
        public readonly ReportInputParameters Parameters;
        /// <summary> To create a report, gets all metadata. </summary>
        /// /// <param name="parameters">The parameters for this report.</param>
        public Report(ReportInputParameters parameters, int maxThreads) {
            MaxThreads = maxThreads;
            BatchFile = parameters.BatchFile;
            Parameters = parameters;
        }
        /// <summary> Creates a report, has to be implemented by all reports. </summary>
        /// <returns>A string containing the report.</returns>
        public abstract string Create();
        /// <summary> Saves the Report created with Create to a file. </summary>
        /// <param name="filename">The path to save the to.</param>
        public void Save(string filename) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var buffer = Create();
            stopwatch.Stop();
            buffer = buffer.Replace("REPORTGENERATETIME", $"{stopwatch.ElapsedMilliseconds}");
            SaveAndCreateDirectories(filename, buffer);
        }

        protected void SaveAndCreateDirectories(string filename, string buffer) {
            try {
                Directory.CreateDirectory(Directory.GetParent(filename).FullName);
            } catch {
                new InputNameSpace.ErrorMessage(filename, "Directory could not be created", $"The directory '{Directory.GetParent(filename).FullName}' could not be created.").Print();
                throw new Exception("Could not save file, see error message above");
            }

            File.WriteAllText(filename, buffer);
        }
    }
}