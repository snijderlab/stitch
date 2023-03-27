using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Stitch.InputNameSpace;
using Stitch.RunParameters;

namespace Stitch {
    /// <summary> A class with options to parse a batch file. </summary>
    public static class ParseCommandFile {
        public static readonly LocalParams<Run> BatchFileParser = new LocalParams<Run>("Batchfile", new List<(string, Action<ParseResult<Run>, KeyValue>)> {
            ("Runname", (output, value) => {
                output.Value.Runname = value.GetValue().UnwrapOrDefault(output, "");}),
            ("RawDataDirectory", (output, value) => {
                if (output.Value.RawDataDirectory != null) output.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                output.AddMessage(new ErrorMessage(value.KeyRange, "Outer scope RawDataDirectory is deprecated", "To allow for more logical information grouping, combinations of multiple datasets, and flexibility with other input file formats the `RawDataDirectory` should be used on Input definitions instead.", "See RawDataDirectory under Input parameters.", true));
                output.Value.RawDataDirectory = ParseHelper.GetFullPath(value).UnwrapOrDefault(output, "");
                output.Value.LoadRawData = true;
                if (!Directory.Exists(output.Value.RawDataDirectory)) {
                    output.AddMessage(new ErrorMessage(value.ValueRange, "Could not find RawDataDirectory.", "Execution will continue, but the spectra will be missing from all reports.", "", true));
                    output.Value.RawDataDirectory = null;
                }}),
            ("Version", (output, value) => {
                var version = ParseHelper.ParseDouble(value).UnwrapOrDefault(output, 1.0);
                if (version < 1.0) {
                    output.AddMessage(new ErrorMessage(value.ValueRange, "Batchfile versions below '1.0' (pre release versions) are deprecated, please update your batchfile to version '1.x'."));
                }
                if (version >= 2.0) {
                    output.AddMessage(new ErrorMessage(value.ValueRange, "This version of Stitch cannot handle batchfiles major version 2.0 or higher."));
                }
                output.Value.VersionSpecified = true;
            }),
            ("MaxCores", (output, value) => {
                output.Value.MaxNumberOfCPUCores = ParseHelper.ParseInt(value).RestrictRange(ParseHelper.NumberRange<int>.Open(0), value.ValueRange).UnwrapOrDefault(output, new());
            }),
            ("Input", (output, value) => {
                if (output.Value.Input.Parameters != null) output.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                output.Value.Input.Parameters = ParseHelper.ParseInputParameters(value).UnwrapOrDefault(output, new());
            }),
            ("TemplateMatching", (output, value) => {
                if (output.Value.TemplateMatching != null) output.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                output.Value.TemplateMatching = ParseHelper.ParseTemplateMatching(output.Value.nameFilter, value).UnwrapOrDefault(output, new());
                if (output.Value.TemplateMatching == null || output.Value.TemplateMatching.Alphabet == null) output.Unwrap();
            }),
            ("Recombine", (output, value) => {
                if (output.Value.Recombine != null) output.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                (output.Value.Recombine, output.Value.order_groups, output.Value.readAlignmentKey) = ParseHelper.ParseRecombine(value).UnwrapOrDefault(output, new());
                if (output.Value.Recombine == null) output.Unwrap();
            }),
            ("Report", (output, value) => {
                if (output.Value.Report != null) output.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                output.Value.Report = ParseHelper.ParseReport(value).UnwrapOrDefault(output, null);
                if (output.Value.Report == null) output.Unwrap();
            }),
        });

        /// <summary> Parses a batch file and retrieves the run parameters or fails with an exception. </summary>
        /// <param name="path"> The path to the batch file. </param>
        /// <returns> The run parameters as specified in the file. </returns>
        public static Run Batch(string path) {
            var output = new Run();
            var outEither = new ParseResult<Run>(output);
            path = ParseHelper.GetFullPath(path, null).Unwrap();

            // Get the contents
            string batchfile_content = ParseHelper.GetAllText(path).Unwrap().Replace("\t", "    "); // Remove tabs, in tabs vs spaces obviously go for spaces ;-)

            // Set the working directory to the directory of the batchfile
            var original_working_directory = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(path))) {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            }

            // Save the batchfile for use in the construction of error messages
            var batchfile = new ParsedFile(path, batchfile_content.Split('\n'), "Batchfile", null);
            // Tokenize the file, into a key value pair tree
            var batchfile_file = InputNameSpace.Tokenizer.Tokenize(batchfile).Unwrap();
            output.BatchFile = batchfile;
            output.IncludedFiles = batchfile_file.Item2;

            var via_local_params = BatchFileParser.Parse(output, batchfile_file.Item1, (args) => {
                var output = outEither.Value;
                var def_position = new Position(0, 1, batchfile);
                var def_range = new FileRange(def_position, def_position);

                // Detect missing parameters
                if (string.IsNullOrWhiteSpace(output.Runname)) output.Runname = Path.GetFileNameWithoutExtension(path);
                if (output.TemplateMatching == null) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "TemplateMatching"));
                if (output.Report == null || output.Report.Files.Count == 0) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Any report parameter"));
                else {
                    // Test validity of FASTA output type
                    if (output.Recombine == null) {
                        foreach (var report in output.Report.Files) {
                            if (report is RunParameters.Report.FASTA fa) {
                                if (fa.OutputType == RunParameters.Report.OutputType.Recombine) {
                                    outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Recombine, because FASTA output was set to 'Recombine'"));
                                }
                            }
                            if (report is RunParameters.Report.CSV csv) {
                                if (csv.OutputType == RunParameters.Report.OutputType.Recombine) {
                                    outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Recombine, because CSV output was set to 'Recombine'"));
                                }
                            }
                        }
                    }
                }

                output.LoadRawData = output.LoadRawData || output.Input.Parameters.Files.Any(f => {
                    if (f is InputData.Peaks p) {
                        return !string.IsNullOrWhiteSpace(p.RawDataDirectory);
                    } else if (f is InputData.MaxNovo m) {
                        return !string.IsNullOrWhiteSpace(m.RawDataDirectory);
                    } else if (f is InputData.Casanovo c) {
                        return !string.IsNullOrWhiteSpace(c.RawDataDirectory);
                    } else { return false; }
                });

                if (outEither.IsErr()) outEither.Unwrap(); // Quit running further if any breaking changes where detected before this point. As further analysis depends on certain properties of the data.

                // Parse Recombination order
                if (output.Recombine != null && output.TemplateMatching != null) {
                    foreach (var group in output.TemplateMatching.Segments) {
                        if (group.Name.ToLower() == "decoy")
                            continue;
                        else if (!output.order_groups.Exists(o => o.OriginalName == group.Name))
                            outEither.AddMessage(new ErrorMessage(batchfile, "Missing order definition for template group", $"For group \"{group.Name}\" there is no corresponding order definition.", "If there is a definition make sure it is written exactly the same and with the same casing."));
                        else {
                            var order = output.order_groups.Find(o => o.OriginalName == group.Name);
                            var order_res = order.GetValue();
                            // Create a new counter
                            var order_counter = new InputNameSpace.Tokenizer.Counter(order.ValueRange.Start);
                            var order_output = new List<RunParameters.RecombineOrder.OrderPiece>();
                            if (order_res.IsOk(outEither)) {
                                var order_string = order_res.Unwrap();
                                while (!string.IsNullOrEmpty(order_string)) {
                                    InputNameSpace.Tokenizer.ParseHelper.Trim(ref order_string, order_counter);

                                    var match = false;

                                    for (int j = 0; j < group.Segments.Count; j++) {
                                        var template = group.Segments[j];
                                        var len = template.Name.Length;
                                        if (order_string.StartsWith(template.Name) && (order_string.Length == len || Char.IsWhiteSpace(order_string[len]) || order_string[len] == '*')) {
                                            order_string = order_string.Remove(0, template.Name.Length);
                                            order_counter.NextColumn(template.Name.Length);
                                            order_output.Add(new RunParameters.RecombineOrder.Template(j));
                                            match = true;
                                            break;
                                        }

                                    }
                                    if (match) continue;

                                    if (order_string.StartsWith('*')) {
                                        order_string = order_string.Remove(0, 1);
                                        order_counter.NextColumn();
                                        order_output.Add(new RunParameters.RecombineOrder.Gap());
                                    } else {
                                        outEither.AddMessage(new ErrorMessage(new FileRange(order_counter.GetPosition(), order.ValueRange.End), "Invalid order", "Valid options are a name of a template, a gap ('*') or whitespace."));
                                        break;
                                    }
                                }
                            }

                            output.Recombine.Order.Add(order_output);
                        }
                    }
                }

                if (output.Recombine != null && output.Recombine.Order.Count != 0) {
                    if (output.TemplateMatching.Segments.Count
                        - (output.TemplateMatching.Segments.Exists(group => string.IsNullOrEmpty(group.Name)) ? 1 : 0)
                        - (output.TemplateMatching.Segments.Exists(group => group.Name.ToLower() == "decoy") ? 1 : 0)
                        != output.Recombine.Order.Count) {
                        outEither.AddMessage(new ErrorMessage(def_range, "Invalid segment groups definition", $"The number of order definitions ({output.Recombine.Order.Count}) should equal the number of segment groups ({output.TemplateMatching.Segments.Count})."));
                    } else {
                        int offset = 0;
                        for (int i = 0; i < output.TemplateMatching.Segments.Count; i++) {
                            if (output.TemplateMatching.Segments[i].Name.ToLower() == "decoy") { offset += 1; continue; };
                            int index = i - offset;
                            var order = output.order_groups[index];
                            int last = -2;
                            foreach (var piece in output.Recombine.Order[index]) {
                                if (piece.IsGap()) {
                                    if (last == -1)
                                        outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "Gaps cannot follow consecutively."));
                                    else if (last == -2)
                                        outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "An order definition cannot start with a gap (*)."));
                                    else {
                                        output.TemplateMatching.Segments[i].Segments[last].GapTail = true;
                                        last = -1;
                                    }
                                } else {
                                    var db = ((RunParameters.RecombineOrder.Template)piece).Index;
                                    if (last == -1)
                                        output.TemplateMatching.Segments[i].Segments[db].GapHead = true;
                                    last = db;
                                }
                            }
                            if (last == -1)
                                outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "An order definition cannot end with a gap (*)."));
                            if (last == -2)
                                outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "An order definition cannot be empty."));
                        }
                    }
                }

                // Propagate alphabets
                if (output.TemplateMatching != null && output.Recombine != null && output.Recombine.Alphabet == null) output.Recombine.Alphabet = output.TemplateMatching.Alphabet;

                // Prepare the input
                if (output.Input != null && output.TemplateMatching.Alphabet != null) outEither.Messages.AddRange(ParseHelper.PrepareInput(output.nameFilter, null, output.Input, null, output.TemplateMatching.Alphabet, output.RawDataDirectory).Messages);

                // Check if there is a version specified
                if (!output.VersionSpecified) {
                    outEither.AddMessage(new ErrorMessage(def_range, "No version specified", "There is no version specified for the batch file; This is needed to handle different versions in different ways."));
                }

                // Reset the working directory
                Directory.SetCurrentDirectory(original_working_directory);

                if (output.TemplateMatching != null) {
                    var alphabet = output.TemplateMatching.Alphabet;
                    foreach (var db in output.TemplateMatching.Segments.SelectMany(group => group.Segments)) {
                        if (db.Templates != null) {
                            for (var i = 0; i < db.Templates.Count; i++) {
                                var read = db.Templates[i];
                                const int TAIL_LENGTH = 20;
                                if (db.GapTail) {
                                    read.Sequence.UpdateSequence(read.Sequence.Length, 0, Enumerable.Repeat(new AminoAcid(alphabet, 'X'), TAIL_LENGTH).ToArray(), "GapTail");
                                    if (read is ReadFormat.Fasta meta)
                                        meta.AnnotatedSequence[^1] = (meta.AnnotatedSequence[^1].Type, meta.AnnotatedSequence[^1].Sequence + new string('X', TAIL_LENGTH));
                                }
                                if (db.GapHead) {
                                    read.Sequence.UpdateSequence(0, 0, Enumerable.Repeat(new AminoAcid(alphabet, 'X'), TAIL_LENGTH).ToArray(), "GapHead");
                                    if (read is ReadFormat.Fasta meta)
                                        meta.AnnotatedSequence[0] = (meta.AnnotatedSequence[0].Type, new string('X', TAIL_LENGTH) + meta.AnnotatedSequence[0].Sequence);
                                }
                                db.Templates[i] = read;
                            }
                        }
                    }
                }
            });
            outEither.Messages.AddRange(via_local_params.Messages);

            return outEither.Unwrap();
        }
    }
    /// <summary> An exception to indicate some error while parsing the batch file </summary>
    public class ParseException : Exception {
        /// <summary> To create a ParseException </summary>
        /// <param name="msg">The message for this Exception</param>
        public ParseException(string msg)
            : base(msg) { }
    }
}
