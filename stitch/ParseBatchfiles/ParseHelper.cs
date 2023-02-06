using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stitch.RunParameters;

namespace Stitch {
    /// <summary> To contain all input functionality </summary>
    namespace InputNameSpace {
        /// <summary> A class with helper functionality for parsing </summary>
        static class ParseHelper {
            /// <summary> Split a line by a separator and return the trimmed pieces and their position as a FileRange. </summary>
            /// <param name="separator">The separator to use.</param>
            /// <param name="linenumber">The linenumber.</param>
            /// <param name="parse_file">The file where the line should be taken from.</param>
            public static List<(string Text, FileRange Pos)> SplitLine(char separator, int linenumber, ParsedFile parse_file) {
                var results = new List<(string, FileRange)>();
                var last_pos = 0;
                var line = parse_file.Lines[linenumber];

                // Find the fields on this line
                for (int pos = 0; pos < line.Length; pos++) {
                    if (line[pos] == separator) {
                        results.Add((
                            line.Substring(last_pos, pos - last_pos).Trim(),
                            new FileRange(new Position(linenumber, last_pos + 1, parse_file), new Position(linenumber, pos + 1, parse_file))
                        ));
                        last_pos = pos + 1;
                    }
                }

                results.Add((
                    line.Substring(last_pos, line.Length - last_pos).Trim(),
                    new FileRange(new Position(linenumber, last_pos, parse_file), new Position(linenumber, Math.Max(0, line.Length - 1), parse_file))
                ));
                return results;
            }

            /// <summary> Converts a string to an int, while it generates meaningful error messages for the end user. </summary>
            /// <returns>If successful: the number (int32)</returns>
            public static ParseResult<int> ConvertToInt(string input, FileRange pos) {
                var result = 0;
                if (int.TryParse(input, out result)) {
                    return new ParseResult<int>(result);
                } else {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseResult<int>(new ErrorMessage(pos, "Not a valid number", msg));
                }
            }

            /// <summary> Converts a string to an int, while it generates meaningful error messages for the end user. </summary>
            /// <returns>If successful: the number (int32)</returns>
            public static ParseResult<int> ParseInt(KeyValue item) {
                var input = item.GetValue();
                if (input.IsErr()) return new ParseResult<int>(input.Messages);
                return ConvertToInt(input.Unwrap(), item.ValueRange);
            }

            /// <summary> Converts a string to an int, while it generates meaningful error messages for the end user. </summary>
            /// <returns>If successful: the number (int32)</returns>
            public static ParseResult<T> ParseEnum<T>(KeyValue item) where T : struct, System.Enum {
                var input = item.GetValue();
                if (input.IsErr()) return new ParseResult<T>(input.Messages);
                object result;
                if (Enum.TryParse(typeof(T), input.Value, true, out result)) {
                    return new ParseResult<T>((T)result);
                } else {
                    var best_match = Enum.GetNames(typeof(T)).Select(o => (o, HelperFunctionality.SmithWatermanStrings(o, input.Value))).OrderByDescending(o => o.Item2).First().Item1;
                    return new ParseResult<T>(new ErrorMessage(item.ValueRange, "Not a valid option", $"Did you mean: \"{best_match}\"?", $"Valid options are: {string.Join(", ", Enum.GetNames(typeof(T)).Select(n => $"\"{n}\""))}"));
                }
            }

            /// <summary> Converts a string to an int, while it generates meaningful error messages for the end user. </summary>
            /// <returns>If successful: the number (double)</returns>
            public static ParseResult<double> ParseDouble(KeyValue item) {
                var input = item.GetValue();
                if (input.IsErr()) return new ParseResult<double>(input.Messages);
                return ConvertToDouble(input.Unwrap(), item.ValueRange);
            }

            /// <summary> Converts a string to a double, while it generates meaningful error messages for the end user. </summary>
            /// <param name="input">The string to be converted to a double.</param>
            /// <returns>If successful: the number (double)</returns>
            public static ParseResult<double> ConvertToDouble(string input, FileRange pos) {
                var result = 0.0;
                if (double.TryParse(input, out result)) {
                    return new ParseResult<double>(result);
                } else {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseResult<double>(new ErrorMessage(pos, "Not a valid number", msg));
                }
            }

            public static ParseResult<char> ParseChar(KeyValue item) {
                var result = new ParseResult<char>();
                var value = item.GetValue();
                if (value.IsOk(result)) {
                    if (value.Unwrap().Length != 1)
                        result.AddMessage(new ErrorMessage(item.ValueRange, "Invalid Character", "A character should be a single character"));
                    else
                        result.Value = value.Unwrap().First();
                }
                return result;
            }

            public static ParseResult<bool> ParseBool(KeyValue setting, string context, bool def = false) {
                var output = new ParseResult<bool>(def);
                var res = setting.GetValue();
                if (res.IsOk(output)) {
                    switch (res.Unwrap().ToLower()) {
                        case "true":
                            output.Value = true;
                            break;
                        case "false":
                            output.Value = false;
                            break;
                        default:
                            output.AddMessage(new ErrorMessage(setting.ValueRange, "Incorrect Boolean definition", "Valid options are: 'True' and 'False'."));
                            break;
                    }
                }
                return output;
            }

            public static void CheckDuplicate<T>(ParseResult<T> outEither, KeyValue item, string value) {
                if (!string.IsNullOrWhiteSpace(value)) outEither.AddMessage(ErrorMessage.DuplicateValue(item.KeyRange.Name));
            }
            public static void CheckDuplicate<T, U>(ParseResult<T> outEither, KeyValue item, List<U> value) {
                if (value != null && value.Count > 0) outEither.AddMessage(ErrorMessage.DuplicateValue(item.KeyRange.Name));
            }
            public static void CheckDuplicate<T, U>(ParseResult<T> outEither, KeyValue item, U value) {
                if (value != null) outEither.AddMessage(ErrorMessage.DuplicateValue(item.KeyRange.Name));
            }

            /// <summary> A number range which can be open ended or closed. </summary>
            public readonly struct NumberRange<T> where T : IComparable<T> {
                readonly T Min;
                readonly T Max;
                readonly bool OpenRange;

                private NumberRange(T min, T max, bool open) {
                    Min = min;
                    Max = max;
                    OpenRange = open;
                }

                /// <summary> Create an open ended number range, essentially placing the maximum number at the boundaries of the underlying type. </summary>
                /// <param name="min">The minimal value.</param>
                /// <returns>The new range struct.</returns>
                public static NumberRange<T> Open(T min) {
                    return new NumberRange<T>(min, min, true);
                }
                /// <summary> Create an closed number range. </summary>
                /// <param name="min">The minimal value.</param>
                /// <param name="max">The maximal value.</param>
                /// <returns>The new range struct.</returns>
                public static NumberRange<T> Closed(T min, T max) {
                    return new NumberRange<T>(min, max, false);
                }

                /// <summary> Check if the given number is in this range. </summary>
                /// <param name="num"> The number to check. </param>
                /// <returns> An int in the same way as CompareTo. </returns>
                public int InRange(T num) {
                    if (num.CompareTo(Min) < 0) return -1;
                    if (!OpenRange && num.CompareTo(Max) > 0) return 1;
                    return 0;
                }

                public override string ToString() {
                    if (OpenRange)
                        return $"{Min}..";
                    else
                        return $"{Min}..{Max}";
                }
            }
            /// <summary> Restrict the given result to within a certain range. Works for any IComparable. </summary>
            /// <param name="value">The value to restrict.</param>
            /// <param name="range">The range to restrict the variable to.</param>
            /// <param name="position">The fileRange to generate nice errors.</param>
            /// <typeparam name="T">The type of the value, has to be IComparable.</typeparam>
            /// <returns>The same result, possible with more error messages.</returns>
            public static ParseResult<T> RestrictRange<T>(this ParseResult<T> value, NumberRange<T> range, FileRange position) where T : IComparable<T> {
                T v;
                if (value.TryGetValue(out v)) {
                    var res = range.InRange(v);
                    if (res < 0)
                        value.AddMessage(new ErrorMessage(position, "Value outside range", $"The value was below the minimal value. The valid range is [{range}]"));
                    else if (res > 0)
                        value.AddMessage(new ErrorMessage(position, "Value outside range", $"The value was above the maximal value. The valid range is [{range}]"));
                }

                return value;
            }
            public static ParseResult<InputData.InputParameters> ParseInputParameters(KeyValue key) {
                var outEither = new ParseResult<InputData.InputParameters>();
                var output = new InputData.InputParameters();

                new LocalParams<InputData.InputParameters>("Input", new List<(string, Action<InputData.InputParameters, KeyValue>)>{
                    ("Peaks", (output, pair) => {
                        new LocalParams<InputData.Peaks>("Peaks", new List<(string, Action<InputData.Peaks, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Path);
                                settings.File.Path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("RawDataDirectory", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.RawDataDirectory);
                                    settings.RawDataDirectory = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("XleDisambiguation", (settings, value) => {
                                settings.XleDisambiguation = ParseHelper.ParseBool(value, "XleDisambiguation").UnwrapOrDefault(outEither, settings.XleDisambiguation);}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                        }, (settings, value) => {
                            var peaks = ParseHelper.GetPeaksSettings(value, false, settings);
                            outEither.Messages.AddRange(peaks.Messages);
                            return peaks.Value;
                        }).Parse(pair, settings => {
                            if (string.IsNullOrWhiteSpace(settings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(settings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(settings);
                        });
                    }),
                    ("Reads", (output, pair) => {
                        new LocalParams<InputData.Reads>("Reads", new List<(string, Action<InputData.Reads, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Path);
                                settings.File.Path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");})
                        }).Parse(pair, reads => {
                            if (string.IsNullOrWhiteSpace(reads.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(reads.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(reads);
                        });
                    }                        ),
                    ("Novor", (output, pair) => {
                        string name = null;
                        new LocalParams<InputData.Novor>("Novor", new List<(string, Action<InputData.Novor, KeyValue>)>{
                            ("Denovo Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.DeNovoFile);
                                settings.DeNovoFile = new ReadFormat.FileIdentifier(ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, ""), "", value);}),
                            ("PSMS Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.PSMSFile);
                                settings.PSMSFile = new ReadFormat.FileIdentifier(ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, ""), "", value);}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, name);
                                name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("Cutoff", (settings, value) => {
                                settings.Cutoff = (uint)ParseHelper.ParseInt(value).RestrictRange(NumberRange<int>.Closed(0, 100), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                            ("Separator", (settings, value) => {
                                settings.Separator = ParseChar(value).UnwrapOrDefault(outEither, ',');})
                        }).Parse(pair, novor => {
                            if (novor.DeNovoFile == null && novor.PSMSFile == null) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "DeNovo Path OR PSMS Path"));
                            if (string.IsNullOrWhiteSpace(name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));
                            if (novor.DeNovoFile != null) novor.DeNovoFile.Name = name;
                            if (novor.PSMSFile != null) novor.PSMSFile.Name = name;
                            output.Files.Add(novor);
                        });
                    }),
                    ("Fasta", (output, pair) => {
                        new LocalParams<InputData.FASTA>("Fasta", new List<(string, Action<InputData.FASTA, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Path);
                                settings.File.Path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("Identifier", (settings, value) => {
                                settings.Identifier = ParseHelper.ParseRegex(value).UnwrapOrDefault(outEither, null);})
                        }).Parse(pair, fasta => {
                            if (string.IsNullOrWhiteSpace(fasta.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(fasta.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(fasta);
                        });
                    }),
                    ("MMCIF", (output, pair) => {
                        new LocalParams<InputData.MMCIF>("MMCIF", new List<(string, Action<InputData.MMCIF, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Path);
                                settings.File.Path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("MinLength", (settings, value) => {
                                settings.MinLength = (uint)ParseHelper.ParseInt(value).RestrictRange(NumberRange<int>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 5);}),
                            ("CutoffALC", (settings, value) => {
                                settings.CutoffALC = (uint)ParseHelper.ParseInt(value).RestrictRange(NumberRange<int>.Closed(0, 100), value.ValueRange).UnwrapOrDefault(outEither, 5);}),
                        }).Parse(pair, mmcif => {
                            if (string.IsNullOrWhiteSpace(mmcif.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(mmcif.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(mmcif);
                        });
                    }),
                    ("Casanovo", (output, pair) => {
                        new LocalParams<InputData.Casanovo>("Casanovo", new List<(string, Action<InputData.Casanovo, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Path);
                                settings.File.Path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");}),
                            ("Name", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.File.Name);
                                settings.File.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("CutoffScore", (settings, value) => {
                                settings.CutoffScore = ParseHelper.ParseDouble(value).UnwrapOrDefault(outEither, 0.0);}),
                        }).Parse(pair, casanovo => {
                            if (string.IsNullOrWhiteSpace(casanovo.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(casanovo.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(casanovo);
                        });
                    }),
                    ("Folder", (output, pair) => {
                        // Parse files one by one
                        var folder_path = "";
                        FileRange? folder_range = null;
                        var starts_with = "";
                        var identifier = new Regex(".*");
                        bool recursive = false;

                        var peaks_settings = new InputData.Peaks();
                        new LocalParams<int>("Folder", new List<(string, Action<int, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, folder_path);
                                    folder_path = ParseHelper.GetFullPath(value).UnwrapOrDefault(outEither, "");
                                    folder_range = value.ValueRange;}),
                            ("StartsWith", (settings, value) => {
                                CheckDuplicate(outEither, value, starts_with);
                                    starts_with = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("Identifier", (settings, value) => {
                                identifier = ParseHelper.ParseRegex(value).UnwrapOrDefault(outEither, null);}),
                            ("Recursive", (settings, value) => {
                                recursive = ParseHelper.ParseBool(value, "Recursive").UnwrapOrDefault(outEither, false);}),
                        }, (settings, value) => {
                            var peaks = ParseHelper.GetPeaksSettings(value, false, peaks_settings);
                            outEither.Messages.AddRange(peaks.Messages);
                            return peaks.Value;
                        }).Parse(pair, settings => {
                            if (!folder_range.HasValue) {
                                outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                                return;
                            }
                            var files = GetAllFilesPrivate(folder_path, recursive);

                            if (files.Item1.Length != 0) {
                                foreach (var file in files.Item1) {
                                    if (!Path.GetFileName(file).StartsWith(starts_with)) continue;

                                    var fileId = new ReadFormat.FileIdentifier() { Name = Path.GetFileNameWithoutExtension(file), Path = ParseHelper.GetFullPath(file).UnwrapOrDefault(outEither, "") };

                                    if (file.EndsWith(".fasta"))
                                        output.Files.Add(new InputData.FASTA() { File = fileId, Identifier = identifier });
                                    else if (file.EndsWith(".txt"))
                                        output.Files.Add(new InputData.Reads() { File = fileId });
                                    else if (file.EndsWith(".csv"))
                                        output.Files.Add(new InputData.Peaks() { File = fileId, FileFormat = peaks_settings.FileFormat, Parameter = peaks_settings.Parameter });
                                    else
                                        continue;
                                }
                            } else {
                                outEither.AddMessage(new ErrorMessage(folder_range.Value, files.Item2, files.Item3, files.Item4));
                            }
                        });
                    }),
                }).Parse(key, output => outEither.Value = output);
                return outEither;
            }
            /// <param name="global">The global InputParameters, if specified, otherwise null.</param>
            public static ParseResult<bool> PrepareInput(NameFilter name_filter, KeyValue key, InputData Input, InputData.InputParameters GlobalInput, ScoringMatrix alphabet, string RawDataDirectory) {
                var result = new ParseResult<bool>();
                result.Value = true;

                if (GlobalInput == null && Input.Parameters == null) {
                    result.AddMessage(new ErrorMessage(key.KeyRange.Name, "Missing Input", "No Input is provided either local or global.", "Please provide local input, using the 'Input' parameter, or provide global input, using the 'Input' parameter in the global scope."));
                    return result;
                }
                if (GlobalInput != null && Input.Parameters != null) {
                    result.AddMessage(new ErrorMessage(key.KeyRange.Name, "Duplicate Input", "Both local and global input is provided.", "Please provide only local or only global input."));
                    return result;
                }

                var input = GlobalInput ?? Input.Parameters;

                foreach (var file in input.Files) {
                    var reads = file switch {
                        InputData.Peaks peaks => OpenReads.Peaks(name_filter, peaks, alphabet, Input.LocalParameters, RawDataDirectory),
                        InputData.FASTA fasta => OpenReads.Fasta(name_filter, fasta.File, fasta.Identifier, alphabet),
                        InputData.Reads simple => OpenReads.Simple(name_filter, simple.File, alphabet),
                        InputData.Novor novor => OpenReads.Novor(name_filter, novor, alphabet),
                        InputData.MMCIF mmcif => OpenReads.MMCIF(name_filter, mmcif, alphabet),
                        InputData.Casanovo casanovo => OpenReads.Casanovo(name_filter, casanovo, alphabet),
                        _ => throw new ArgumentException("An unknown input format was provided to PrepareInput")
                    };
                    result.Messages.AddRange(reads.Messages);
                    if (!reads.IsErr()) Input.Data.Raw.Add(reads.Unwrap());
                }

                Input.Data.Cleaned = OpenReads.CleanUpInput(Input.Data.Raw, alphabet, name_filter).UnwrapOrDefault(result, new());

                return result;
            }
            public static ParseResult<TemplateMatchingParameter> ParseTemplateMatching(NameFilter nameFilter, KeyValue key) {
                var outEither = new ParseResult<TemplateMatchingParameter>();

                var settings = new LocalParams<TemplateMatchingParameter>("TemplateMatching", new List<(string, Action<TemplateMatchingParameter, KeyValue>)>{
                    ("CutoffScore", (settings, value) => {
                        settings.CutoffScore = ParseHelper.ParseDouble(value).RestrictRange(NumberRange<double>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Alphabet", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Alphabet);
                            settings.Alphabet = ParseHelper.ParseAlphabet(value).UnwrapOrDefault(outEither, null);}),
                    ("AmbiguityThreshold", (settings, value) => {
                            settings.AmbiguityThreshold = ParseHelper.ParseDouble(value).RestrictRange(NumberRange<double>.Closed(0.0, 1.0), value.ValueRange).UnwrapOrDefault(outEither, 0.5);}),
                    ("EnforceUnique", (settings, value) => {
                            var v = 1.0;
                            var boolean = ParseBool(value, "EnforceUnique");
                            var number = ParseDouble(value);
                            if (boolean.IsOk()) {
                                v = boolean.Unwrap() ? 1.0 : 0.0;
                            } else if (number.IsOk()) {
                                v = number.RestrictRange(NumberRange<double>.Closed(0.0, 1.0), value.ValueRange).UnwrapOrDefault(outEither, 1.0);
                            } else {
                                outEither.AddMessage(new ErrorMessage(value.ValueRange, "Incorrect EnforceUnique definition", "Expected a boolean (True/False) or a number."));
                            }
                            settings.EnforceUnique = v;}),
                    ("EnforceUniqueLocalised", (settings, value) => {
                        settings.EnforceUniqueLocalised = ParseHelper.ParseBool(value, "EnforceUniqueLocalised").UnwrapOrDefault(outEither, settings.EnforceUniqueLocalised);}),
                    ("ForceGermlineIsoleucine", (settings, value) => {
                        settings.ForceGermlineIsoleucine = ParseBool(value, "ForceGermlineIsoleucine").UnwrapOrDefault(outEither, true);}),
                    ("BuildTree", (settings, value) => {
                        settings.BuildTree = ParseBool(value, "BuildTree").UnwrapOrDefault(outEither, true);}),
                    ("Segments", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Segments);
                            var outer_children = new List<SegmentValue>();
                            foreach (var segment in value.GetValues().UnwrapOrDefault(outEither, new())) {
                                if (segment.Name == "segment") {
                                    var segment_value = ParseHelper.ParseSegment(nameFilter, segment, settings.Alphabet, false).UnwrapOrDefault(outEither, new());

                                    // Check to see if the name is valid
                                    if (outer_children.Select(db => db.Name).Contains(segment_value.Name))
                                        outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names have to be unique."));
                                    if (segment_value.Name.Contains('*'))
                                        outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names cannot contain '*'."));
                                    outer_children.Add(segment_value);
                                } else {
                                    var children = new List<SegmentValue>();
                                    foreach (var sub_segment in segment.GetValues().UnwrapOrDefault(outEither, new())) {
                                        var segment_value = ParseHelper.ParseSegment(nameFilter, sub_segment, settings.Alphabet, false);
                                        if (segment_value.IsOk(outEither)) {
                                            var segment_object = segment_value.Unwrap();
                                            // Check to see if the name is valid
                                            if (children.Select(db => db.Name).Contains(segment_object.Name))
                                                outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names have to be unique, within their scope."));
                                            if (segment_object.Name.Contains('*'))
                                                outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names cannot contain '*'."));
                                            children.Add(segment_object);
                                        }
                                    }
                                    settings.Segments.Add((segment.OriginalName, children));
                                }
                            }
                            if (outer_children.Count > 0) settings.Segments.Add(("", outer_children));}),
                }).Parse(key, settings => {
                    if (settings.Segments.Count > 1)
                        foreach (var db in settings.Segments)
                            if (string.IsNullOrEmpty(db.Name))
                                outEither.AddMessage(new ErrorMessage(key.KeyRange.Full, "Single segments in grouped segment list", "You cannot define a single segment when there are also segment groups defined."));

                    if (settings.Alphabet == null)
                        outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Name, "Alphabet"));

                    if (settings.Segments.Count == 0)
                        outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Name, "Any segment"));

                    outEither.Value = settings;
                });
                return outEither;
            }

            public static ParseResult<(RecombineParameter, List<KeyValue>, KeyValue)> ParseRecombine(KeyValue key) {
                var outEither = new ParseResult<(RecombineParameter, List<KeyValue>, KeyValue)>();

                var order = new List<KeyValue>();
                KeyValue readAlignmentKey = null;

                var settings = new LocalParams<RecombineParameter>("Recombined", new List<(string, Action<RecombineParameter, KeyValue>)>{
                    ("N", (settings, value) => {
                        settings.N = ParseHelper.ParseInt(value).RestrictRange(NumberRange<int>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Order", (settings, value) => {
                        CheckDuplicate(outEither, value, order);
                            if (value.IsSingle()) order.Add(value);
                            else
                                foreach (var group in value.GetValues().UnwrapOrDefault(outEither, new()))
                                    order.Add(group);}),
                    ("CutoffScore", (settings, value) => {
                        settings.CutoffScore = ParseHelper.ParseDouble(value).RestrictRange(NumberRange<double>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Alphabet", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Alphabet);
                            settings.Alphabet = ParseHelper.ParseAlphabet(value).UnwrapOrDefault(outEither, null);}),
                    ("EnforceUnique", (settings, value) => {
                            var v = 1.0;
                            var boolean = ParseBool(value, "EnforceUnique");
                            var number = ParseDouble(value);
                            if (boolean.IsOk()) {
                                v = boolean.Unwrap() ? 1.0 : 0.0;
                            } else if (number.IsOk()) {
                                v = number.RestrictRange(NumberRange<double>.Closed(0.0, 1.0), value.ValueRange).UnwrapOrDefault(outEither, 1.0);
                            } else {
                                outEither.AddMessage(new ErrorMessage(value.ValueRange, "Incorrect EnforceUnique definition", "Expected a boolean (True/False) or a number."));
                            }
                            settings.EnforceUnique = new Option<double>(v);}),
                    ("EnforceUniqueLocalised", (settings, value) => {
                        settings.EnforceUniqueLocalised = ParseHelper.ParseBool(value, "EnforceUniqueLocalised").UnwrapOrDefault(outEither, settings.EnforceUniqueLocalised);}),
                    ("ForceGermlineIsoleucine", (settings, value) => {
                        settings.ForceGermlineIsoleucine = ParseBool(value, "ForceGermlineIsoleucine").UnwrapOrDefault(outEither, true) ? Trilean.True : Trilean.False;}),
                    ("Decoy", (settings, value) => {
                        settings.Decoy = ParseBool(value, "Decoy").UnwrapOrDefault(outEither, false);}),
                }).Parse(key, settings => {
                    if (order == null)
                        outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Order"));

                    outEither.Value = (settings, order, readAlignmentKey);
                });
                return outEither;
            }

            public static ParseResult<ReportParameter> ParseReport(KeyValue key) {
                var outEither = new ParseResult<ReportParameter>();

                var output = new LocalParams<ReportParameter>("Report", new List<(string, Action<ReportParameter, KeyValue>)>{
                    ("Folder", (output, pair) => {
                        CheckDuplicate(outEither, pair, output.Folder);
                        output.Folder = ParseHelper.GetFullPath(pair).UnwrapOrDefault(outEither, "");}),
                    ("Html", (output, pair) => {
                        new LocalParams<RunParameters.Report.HTML>("Html", new List<(string, Action<RunParameters.Report.HTML, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.Path);
                                settings.Path = value.GetValue().UnwrapOrDefault(outEither, "");}),
                        }).Parse(pair, html => {
                            if (string.IsNullOrWhiteSpace(html.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            output.Files.Add(html);
                        }).IsOk(outEither);}),
                    ("Json", (output, pair) => {
                        var j_settings = new LocalParams<RunParameters.Report.JSON>("Json", new List<(string, Action<RunParameters.Report.JSON, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.Path);
                                settings.Path = value.GetValue().UnwrapOrDefault(outEither, "");}),
                        }).Parse(pair, json => {
                            if (string.IsNullOrWhiteSpace(json.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));

                            output.Files.Add(json);
                        });}),
                    ("Fasta", (output, pair) => {
                        new LocalParams<RunParameters.Report.FASTA>("Fasta", new List<(string, Action<RunParameters.Report.FASTA, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.Path);
                                settings.Path = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("MinimalScore", (settings, value) => {
                                settings.MinimalScore = ParseHelper.ParseInt(value).RestrictRange(NumberRange<int>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                            ("OutputType", (settings, value) => {
                                settings.OutputType = ParseHelper.ParseEnum<RunParameters.Report.OutputType>(value).UnwrapOrDefault(outEither, 0);}),
                        }).Parse(pair, fasta => {
                            if (string.IsNullOrWhiteSpace(fasta.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));

                            output.Files.Add(fasta);
                        });}),
                    ("CSV", (output, pair) => {
                        new LocalParams<RunParameters.Report.CSV>("CSV", new List<(string, Action<RunParameters.Report.CSV, KeyValue>)>{
                            ("Path", (settings, value) => {
                                CheckDuplicate(outEither, value, settings.Path);
                                settings.Path = value.GetValue().UnwrapOrDefault(outEither, "");}),
                            ("OutputType", (settings, value) => {
                                settings.OutputType = ParseHelper.ParseEnum<RunParameters.Report.OutputType>(value).UnwrapOrDefault(outEither, 0);}),
                        }).Parse(pair, csv => {
                            if (string.IsNullOrWhiteSpace(csv.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));

                            output.Files.Add(csv);
                        });}),
                }).Parse(key, report => {
                    if (report.Folder == null)
                        report.Folder = Directory.GetCurrentDirectory();
                    outEither.Value = report;
                });
                return outEither;
            }
            public static ParseResult<ScoringMatrix> ParseAlphabet(KeyValue key) {
                var outEither = new ParseResult<ScoringMatrix>();
                var identity = ("", 0, 0);
                var symmetric_sets = new List<(sbyte, List<List<List<char>>>)>();
                var asymmetric_sets = new List<(sbyte, List<(List<List<char>>, List<List<char>>)>)>();
                (char[], sbyte[,]) result;
                KeyValue path_Setting = null;

                new LocalParams<AlphabetParameter>("Alphabet", new List<(string, Action<AlphabetParameter, KeyValue>)>{
                    ("Path", (settings, value) => {
                        if (settings.Alphabet != null || path_Setting != null) outEither.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                            path_Setting = value;}),
                    ("Data", (settings, value) => {
                        if (settings.Alphabet != null || path_Setting != null) outEither.AddMessage(ErrorMessage.DuplicateValue(value.KeyRange.Name));
                            var res = value.GetValue();
                            if (res.IsOk(outEither)) {
                                var data_content = res.Unwrap().Split("\n");
                                var counter = new Tokenizer.Counter(value.ValueRange.Start);
                                result = ParseAlphabetData(new ParsedFile(".", data_content, "Inline Alphabet data", value), counter).UnwrapOrDefault(outEither, new());
                                settings.Alphabet = result.Item1;
                                settings.ScoringMatrix = result.Item2;
                            }}),
                    ("Name", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Name);
                            settings.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                    ("GapStartPenalty", (settings, value) => {
                        settings.GapStart = (sbyte)-ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0);
                            outEither.AddMessage(new ErrorMessage(value.KeyRange, "GapStartPenalty is deprecated", "Use `GapStart` instead, with the inverse value.", $"GapStart: {settings.GapStart}", true));}),
                    ("GapStart", (settings, value) => {
                        settings.GapStart = (sbyte)ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("GapExtendPenalty", (settings, value) => {
                        settings.GapExtend = (sbyte)-ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0);
                            outEither.AddMessage(new ErrorMessage(value.KeyRange, "GapExtendPenalty is deprecated", "Use `GapExtend` instead, with the inverse value.", $"GapExtend: {settings.GapExtend}", true));}),
                    ("GapExtend", (settings, value) => {
                        settings.GapExtend = (sbyte)ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Characters", (settings, value) => {
                        CheckDuplicate(outEither, value, identity.Item1);
                            identity = (value.GetValue().UnwrapOrDefault(outEither, ""), identity.Item2, identity.Item3);}),
                    ("Identity", (settings, value) => {
                        identity = (identity.Item1, ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0), identity.Item3);}),
                    ("MisMatch", (settings, value) => {
                        identity = (identity.Item1, identity.Item2, ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0));}),
                    ("PatchLength", (settings, value) => {
                        settings.PatchLength = ParseInt(value).RestrictRange(NumberRange<int>.Closed(0, 10), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Swap", (settings, value) => {
                        settings.Swap = (sbyte)ParseInt(value).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), value.ValueRange).UnwrapOrDefault(outEither, 0);}),
                    ("Symmetric sets", (settings, value) => {
                        sbyte score = 0;
                            var sets = new List<List<List<char>>>();
                            foreach (var inner in value.GetValues().UnwrapOrDefault(outEither, new List<KeyValue>())) {
                                switch (inner.Name) {
                                    case "score":
                                        score = (sbyte)ParseInt(inner).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), inner.ValueRange).UnwrapOrDefault(outEither, 0);
                                        break;
                                    case "sets":
                                        sets = inner.GetValue().UnwrapOrDefault(outEither, "").Split('\n').Select(s => {
                                            if (s.Trim().StartsWith('-')) return new();
                                            return s.Split('-', 2).First().Split(',').Select(s1 => s1.Trim().ToCharArray().ToList()).ToList();
                                        }).ToList();
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(inner.KeyRange.Name, "Symmetric Sets", "'Score', 'Sets'"));
                                        break;
                                }
                            }
                            if (score == 0) outEither.AddMessage(ErrorMessage.MissingParameter(value.ValueRange, "Score"));
                            if (sets.Count == 0) outEither.AddMessage(ErrorMessage.MissingParameter(value.ValueRange, "Sets"));
                            symmetric_sets.Add((score, sets));}),
                    ("Asymmetric sets", (settings, value) => {
                        sbyte a_score = 0;
                            var a_sets = new List<(List<List<char>>, List<List<char>>)>();
                            foreach (var inner in value.GetValues().UnwrapOrDefault(outEither, new List<KeyValue>())) {
                                switch (inner.Name) {
                                    case "score":
                                        a_score = (sbyte)ParseInt(inner).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), inner.ValueRange).UnwrapOrDefault(outEither, 0);
                                        break;
                                    case "sets":
                                        a_sets = inner.GetValue().UnwrapOrDefault(outEither, "").Split('\n').Select(s => {
                                            if (s.Trim().StartsWith('-')) return (new(), new());
                                            var temp = s.Split("->", 2).Select(s0 => s0.Split('-', 2).First().Split(',').Select(s1 => s1.Trim().ToCharArray().ToList()).ToList()).ToList();
                                            return (temp[0], temp[1]);
                                        }).ToList();
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(inner.KeyRange.Name, "Asymmetric Sets", "'Score', 'Sets'"));
                                        break;
                                }
                            }
                            if (a_score == 0) outEither.AddMessage(ErrorMessage.MissingParameter(value.ValueRange, "Score"));
                            if (a_sets.Count == 0) outEither.AddMessage(ErrorMessage.MissingParameter(value.ValueRange, "Sets"));
                            asymmetric_sets.Add((a_score, a_sets));}),
                }).Parse(key, settings => {
                    if (String.IsNullOrEmpty(identity.Item1)) {
                        if (path_Setting != null) {
                            var all_text = GetAllText(path_Setting);

                            if (all_text.IsOk(outEither)) {
                                var content = all_text.UnwrapOrDefault(outEither, "").Split("\n");
                                var id = new ParsedFile(GetFullPath(path_Setting).UnwrapOrDefault(outEither, ""), content, settings.Name, key);
                                var counter = new Tokenizer.Counter(id);
                                result = ParseAlphabetData(id, counter).UnwrapOrDefault(outEither, new());
                                settings.Alphabet = result.Item1;
                                settings.ScoringMatrix = result.Item2;
                            } else {
                                outEither.Value = null;
                                return; // The path is wrong so just give up
                            }
                        }
                    } else {
                        settings.Alphabet = identity.Item1.ToCharArray();
                    }

                    // Detect erroneous set definitions
                    foreach (var super_set in symmetric_sets)
                        foreach (var set in super_set.Item2)
                            foreach (var seq in set)
                                foreach (var aa in seq)
                                    if (!settings.Alphabet.Contains(aa)) {
                                        outEither.AddMessage(new ErrorMessage(String.Join("", seq), "AminoAcid not in Alphabet", "The given set contains characters that are not included in the given alphabet."));
                                        break;
                                    }
                    foreach (var super_set in asymmetric_sets)
                        foreach (var set in super_set.Item2)
                            foreach (var collection in new List<List<char>>[] { set.Item1, set.Item2 })
                                foreach (var seq in collection)
                                    foreach (var aa in seq)
                                        if (!settings.Alphabet.Contains(aa)) {
                                            outEither.AddMessage(new ErrorMessage(String.Join("", seq), "AminoAcid not in Alphabet", "The given set contains characters that are not included in the given alphabet."));
                                            break;
                                        }
                    if (outEither.IsErr()) return;

                    if (String.IsNullOrEmpty(identity.Item1)) {
                        if (settings.ScoringMatrix == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Data or Path"));
                        if (!outEither.IsErr())
                            outEither.Value = new ScoringMatrix(settings.ScoringMatrix, settings.Alphabet.ToList(), symmetric_sets, asymmetric_sets, settings.GapStart, settings.GapExtend, settings.Swap, settings.PatchLength, '.');
                    } else {
                        if (!outEither.IsErr())
                            outEither.Value = ScoringMatrix.IdentityMatrix(settings.Alphabet.ToList(), symmetric_sets, asymmetric_sets, (sbyte)identity.Item2, (sbyte)identity.Item3, settings.GapStart, settings.GapExtend, settings.Swap, settings.PatchLength, '.');
                    }
                });
                return outEither;
            }
            public static ParseResult<(char[], sbyte[,])> ParseAlphabetData(ParsedFile file, Tokenizer.Counter counter) {
                var outEither = new ParseResult<(char[], sbyte[,])>();

                int rows = file.Lines.Length;
                var cells = new List<(Position, List<(string, FileRange)>)>();

                for (int i = 0; i < file.Lines.Length; i++) {
                    var start_line = counter.GetPosition();
                    var split_line = new List<(string, FileRange)>();
                    var line = file.Lines[i];
                    Tokenizer.ParseHelper.Trim(ref line, counter);

                    while (!string.IsNullOrEmpty(line)) {
                        if (line[0] == ';' || line[0] == ',') {
                            line = line.Remove(0, 1);
                            counter.NextColumn();
                        } else {
                            var start = counter.GetPosition();
                            var cell = Tokenizer.ParseHelper.UntilOneOf(ref line, new char[] { ';', ',' }, counter);
                            var range = new FileRange(start, counter.GetPosition());
                            split_line.Add((cell, range));
                        }
                        Tokenizer.ParseHelper.Trim(ref line, counter);
                    }
                    cells.Add((start_line, split_line));
                    counter.NextLine();
                }

                int columns = cells[0].Item2.Count;

                for (int line = 0; line < rows; line++) {
                    if (rows > cells[line].Item2.Count) {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {rows - cells[line].Item2.Count} column(s) missing on this row."));
                    } else if (rows < cells[line].Item2.Count) {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {cells[line].Item2.Count - rows} additional column(s) on this row."));
                    }
                }

                var alphabetBuilder = new StringBuilder();
                foreach (var element in cells[0].Item2.Skip(1)) {
                    alphabetBuilder.Append(element.Item1);
                }
                var alphabet = alphabetBuilder.ToString().Trim().ToCharArray();

                if (!alphabet.Contains('.')) {
                    outEither.AddMessage(new ErrorMessage(counter.File, "GapChar missing", $"The Gap '.' is missing in the alphabet definition.", "", true));
                }

                var scoring_matrix = new sbyte[columns - 1, columns - 1];

                for (int i = 0; i < columns - 1; i++) {
                    for (int j = 0; j < columns - 1; j++) {
                        try {
                            scoring_matrix[i, j] = (sbyte)ConvertToInt(cells[i + 1].Item2[j + 1].Item1, cells[i + 1].Item2[j + 1].Item2).RestrictRange(NumberRange<int>.Closed(sbyte.MinValue, sbyte.MaxValue), cells[i + 1].Item2[j + 1].Item2).UnwrapOrDefault(outEither, 0);
                        } catch (ArgumentOutOfRangeException) {
                            // Invalid amount of cells will already be pointed out
                            //outEither.AddMessage(new ErrorMessage(cells[i + 1].Item1, "Cell out of range", $"Cell {i},{j} out of range."));
                        }
                    }
                }
                outEither.Value = (alphabet, scoring_matrix);
                return outEither;
            }
            public static ParseResult<Regex> ParseRegex(KeyValue node) {
                var outEither = new ParseResult<Regex>();
                try {
                    var res = node.GetValue();
                    if (res.IsOk(outEither)) {
                        outEither.Value = new Regex(res.Unwrap().Trim());

                        if (outEither.Value.GetGroupNumbers().Length <= 1) {
                            outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx is invalid", "The given RegEx has no capturing groups.", "To parse an identifier from the fasta header a capturing group (enclosed in parentheses '()') should be present enclosing the identifier. Example: '\\s*(\\w*)'"));
                        } else if (outEither.Value.GetGroupNumbers().Length > 3) {
                            outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx could be wrong", "The given RegEx has a lot of capturing groups, only the first two will be used.", "", true));
                        }
                    }
                } catch (ArgumentException) {
                    outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx is invalid", "The given Regex could not be parsed.", "See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference for a reference."));
                }
                return outEither;
            }
            /// <summary> Parses a Template </summary>
            /// <param name="key">The KeyValue to parse</param>
            /// <param name="extended">To determine if it is an extended (free standing) template or a template in a recombination definition</param>
            public static ParseResult<SegmentValue> ParseSegment(NameFilter name_filter, KeyValue key, ScoringMatrix backup_alphabet, bool extended) {
                // Parse files one by one
                var file_path = "";
                KeyValue file_pos = key;

                var peaks_settings = new InputData.Peaks();

                var outEither = new ParseResult<SegmentValue>();

                new LocalParams<SegmentValue>("Segment", new List<(string, Action<SegmentValue, KeyValue>)>{
                    ("Path", (settings, value) => {
                        CheckDuplicate(outEither, value, file_path);
                        file_path = GetFullPath(value).UnwrapOrDefault(outEither, "");
                        file_pos = value;}),
                    ("Name", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Name);
                        settings.Name = value.GetValue().UnwrapOrDefault(outEither, "");}),
                    ("CutoffScore", (settings, value) => {
                        if (extended) settings.CutoffScore = ParseHelper.ParseDouble(value).RestrictRange(NumberRange<double>.Open(0), value.ValueRange).UnwrapOrDefault(outEither, 0);
                        else outEither.AddMessage(new ErrorMessage(value.KeyRange.Name, "CutoffScore cannot be defined here", "Inside a template in the templates list of a recombination a CutoffScore should not be defined."));}),
                    ("Alphabet", (settings, value) => {
                        CheckDuplicate(outEither, value, settings.Alphabet);
                        if (!extended) {
                            outEither.AddMessage(new ErrorMessage(value.KeyRange.Name, "Alphabet cannot be defined here", "Inside a template in the templates list of a recombination an alphabet can not be defined."));
                        } else {
                            settings.Alphabet = ParseHelper.ParseAlphabet(value).UnwrapOrDefault(outEither, null);
                        }}),
                    ("Identifier", (settings, value) => {
                        settings.Identifier = ParseHelper.ParseRegex(value).UnwrapOrDefault(outEither, null);}),
                    ("Scoring", (settings, value) => {
                        settings.Scoring =  ParseHelper.ParseEnum<ScoringParameter>(key).UnwrapOrDefault(outEither, ScoringParameter.Absolute);}),
                    ("GapHead", (settings, value) => {
                        settings.GapHead = ParseBool(value, "GapHead").UnwrapOrDefault(outEither, false);}),
                    ("GapTail", (settings, value) => {
                        settings.GapTail = ParseBool(value, "GapTail").UnwrapOrDefault(outEither, false);}),
                }, (settings, value) => {
                    var peaks = GetPeaksSettings(value, true, peaks_settings);
                    outEither.Messages.AddRange(peaks.Messages);
                    return peaks.Value;
                }).Parse(key, settings => {
                    if (settings.Name == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Name"));
                    if (String.IsNullOrWhiteSpace(file_path)) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Path"));
                    if (extended && settings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Alphabet"));
                    if ((settings.Alphabet ?? backup_alphabet) == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Alphabet"));
                    if (outEither.IsErr()) return;

                    // Open the file
                    var fileId = new ReadFormat.FileIdentifier(ParseHelper.GetFullPath(file_path).UnwrapOrDefault(outEither, ""), settings.Name, file_pos);

                    var folder_reads = new ParseResult<List<ReadFormat.General>>();
                    var alphabet = settings.Alphabet ?? backup_alphabet;

                    if (file_path.EndsWith(".fasta"))
                        folder_reads = OpenReads.Fasta(name_filter, fileId, settings.Identifier, alphabet);
                    else if (file_path.EndsWith(".txt"))
                        folder_reads = OpenReads.Simple(name_filter, fileId, alphabet);
                    else if (file_path.EndsWith(".csv")) {
                        peaks_settings.File = fileId;
                        folder_reads = OpenReads.Peaks(name_filter, peaks_settings, alphabet);
                    } else if (file_path.EndsWith(".mztab")) {
                        var casanovo_settings = new InputData.Casanovo();
                        casanovo_settings.File = fileId;
                        folder_reads = OpenReads.Casanovo(name_filter, casanovo_settings, alphabet);
                    } else if (file_path.EndsWith(".mmcif")) {
                        var mmcif_settings = new InputData.MMCIF();
                        mmcif_settings.File = fileId;
                        folder_reads = OpenReads.MMCIF(name_filter, mmcif_settings, alphabet);
                    } else
                        outEither.AddMessage(new ErrorMessage(file_pos.ValueRange, "Invalid file format", "The file should be of .txt, .fasta, .csv, .mztab, or .mmcif type."));

                    outEither.Messages.AddRange(folder_reads.Messages);
                    if (!folder_reads.IsErr()) settings.Templates = folder_reads.Unwrap();
                    outEither.Value = settings;
                });
                return outEither;
            }
            public static ParseResult<bool> GetPeaksSettings(KeyValue setting, bool with_prefix, InputData.Peaks peaks_settings) {
                var outEither = new ParseResult<bool>(true);
                var name = setting.Name;

                if (with_prefix && !name.StartsWith("peaks")) {
                    outEither.Value = false;
                    return outEither;
                }

                if (with_prefix) name = name.Substring(5);

                switch (name) {
                    case "format":
                        new LocalParams<InputData.Peaks>("Format", new List<(string, Action<InputData.Peaks, KeyValue>)>{
                            ("Old", (settings, value) => {
                                peaks_settings.FileFormat = PeaksFileFormat.OldFormat();}),
                            ("X", (settings, value) => {
                                peaks_settings.FileFormat = PeaksFileFormat.PeaksX();}),
                            ("X+", (settings, value) => {
                                peaks_settings.FileFormat = PeaksFileFormat.PeaksXPlus();}),
                        }, peaks_settings).ParseSingular(setting).IsOk(outEither);
                        break;
                    case "separator":
                        peaks_settings.Separator = ParseChar(setting).UnwrapOrDefault(outEither, ',');
                        break;
                    case "decimalseparator":
                        peaks_settings.DecimalSeparator = ParseChar(setting).UnwrapOrDefault(outEither, '.');
                        break;
                    default:
                        var (parameters, success) = GetLocalPeaksParameters(setting, with_prefix, peaks_settings.Parameter).UnwrapOrDefault(outEither, (new(true), true));
                        peaks_settings.Parameter = parameters;

                        if (!success)
                            outEither.Value = false;
                        break;
                }

                return outEither;
            }
            public static ParseResult<(InputData.PeaksParameters, bool)> GetLocalPeaksParameters(KeyValue setting, bool with_prefix, InputData.PeaksParameters parameters) {
                var outEither = new ParseResult<(InputData.PeaksParameters, bool)>();
                outEither.Value = (parameters, true);
                var name = setting.Name;

                if (with_prefix && !name.StartsWith("peaks")) {
                    outEither.Value = (parameters, false);
                    return outEither;
                }

                if (with_prefix) name = name.Substring(5);

                switch (name) {
                    case "cutoffalc":
                        parameters.CutoffALC = ParseInt(setting).RestrictRange(NumberRange<int>.Closed(0, 100), setting.ValueRange).UnwrapOrDefault(outEither, 0);
                        break;
                    case "localcutoffalc":
                        parameters.LocalCutoffALC = ParseInt(setting).RestrictRange(NumberRange<int>.Closed(0, 100), setting.ValueRange).UnwrapOrDefault(outEither, 0);
                        break;
                    case "minlengthpatch":
                        parameters.MinLengthPatch = ParseHelper.ParseInt(setting).RestrictRange(NumberRange<int>.Open(0), setting.ValueRange).UnwrapOrDefault(outEither, 0);
                        break;
                    default:
                        outEither.Value = (parameters, false);
                        break;
                }

                return outEither;
            }

            public static ParseResult<string> GetFullPath(KeyValue setting) {
                var outEither = new ParseResult<string>();
                var res = setting.GetValue();
                if (res.IsOk(outEither)) {
                    var path = GetFullPathPrivate(res.Unwrap());

                    if (string.IsNullOrEmpty(path.Item2)) {
                        outEither.Value = Path.GetFullPath(path.Item1);
                    } else {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, path.Item1, path.Item2));
                    }
                }
                return outEither;
            }
            public static ParseResult<string> GetFullPath(string path) {
                var outEither = new ParseResult<string>();
                var res = GetFullPathPrivate(path);

                if (string.IsNullOrEmpty(res.Item2)) {
                    outEither.Value = Path.GetFullPath(res.Item1);
                } else {
                    outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2));
                }
                return outEither;
            }
            static (string, string) GetFullPathPrivate(string path) {
                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                    return ("Invalid path", "The path contains invalid characters.");
                } else if (string.IsNullOrWhiteSpace(path)) {
                    return ("Invalid path", "The path is empty.");
                }
                {
                    try {
                        if (path.StartsWith("\"") && path.EndsWith("\"")) {
                            path = path.Substring(1, path.Length - 2);
                        }
                        return (Path.GetFullPath(path), "");
                    } catch (ArgumentException) {
                        return ("Invalid path", "The path cannot be found.");
                    } catch (System.Security.SecurityException) {
                        return ("Invalid path", "The file could not be opened because of a lack of required permissions.");
                    } catch (NotSupportedException) {
                        return ("Invalid path", "The path contains a colon ':' not part of a volume identifier.");
                    } catch (PathTooLongException) {
                        return ("Invalid path", "The path length exceeds the system defined width.");
                    } catch (Exception e) {
                        return ("Invalid path", $"Unknown exception occurred when reading path: {e.Message}.");
                    }
                }
            }
            static (string[], string, string, string) GetAllFilesPrivate(string path, bool recursive) {
                var try_path = GetFullPathPrivate(path);

                try {
                    var option = SearchOption.TopDirectoryOnly;
                    if (recursive) option = SearchOption.AllDirectories;
                    return (Directory.GetFiles(try_path.Item1, "*", option), "", "", "");
                } catch (ArgumentException) {
                    return (Array.Empty<string>(), "Invalid path", "The path contains invalid characters.", "");
                } catch (UnauthorizedAccessException) {
                    return (Array.Empty<string>(), "Invalid path", "The file could not be opened because of a lack of required permissions.", "");
                } catch (PathTooLongException) {
                    return (Array.Empty<string>(), "Invalid path", "The path length exceeds the system defined width.", "");
                } catch (DirectoryNotFoundException) {
                    try {
                        var pieces = try_path.Item1.Split(new char[] { '\\', '/' });
                        var drive = pieces[0].Split(':')[0];
                        if (Directory.GetLogicalDrives().Contains($"{drive}:\\")) {
                            string current_path = $"{drive}:\\";
                            for (int i = 1; i < pieces.Length - 1; i++) {
                                string next_path = current_path + pieces[i] + Path.DirectorySeparatorChar;

                                if (!Directory.Exists(next_path)) {
                                    var directories = Directory.GetDirectories(current_path);
                                    var extra = "";

                                    if (directories.Length == 0) extra = "\nThere are no subfolders in this folder.";
                                    else if (directories.Length == 1) extra = $"\nThe only subfolder is '{directories[0]}'.";
                                    else {
                                        int max_value = 0;
                                        string max_name = "";
                                        foreach (var dir in directories) {
                                            int score = HelperFunctionality.SmithWatermanStrings(dir, pieces[i]);
                                            if (score > max_value) {
                                                max_name = Path.GetFileName(dir);
                                                max_value = score;
                                            }
                                        }
                                        extra = $"\nDid you mean '{max_name}'?";
                                    }

                                    return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The folder '{pieces[i]}' does not exist in '{pieces[i - 1]}'.{extra}");
                                }
                                current_path = next_path;
                            }
                            // Will likely be never used because that would raise a FileNotFoundException
                            return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The file '{pieces[^1]}' does not exist in '{current_path}'.");
                        } else {
                            return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The drive '{drive}:\\' is not mounted.");
                        }
                    } catch {
                        return (Array.Empty<string>(), "Could not open file", "The path cannot be found, possibly on an unmapped drive.", "");
                    }
                } catch (IOException) {
                    return (Array.Empty<string>(), "Invalid path", "The path is a file name or a network error has occurred.", "");
                } catch (Exception e) {
                    return (Array.Empty<string>(), "Invalid path", $"Unknown exception occurred when reading path: {e.Message}.", "");
                }
            }
            public static ParseResult<string> GetAllText(KeyValue setting) {
                var outEither = new ParseResult<string>();
                var item = setting.GetValue();
                if (item.IsOk(outEither)) {
                    var res = GetAllTextPrivate(item.Unwrap());

                    if (string.IsNullOrEmpty(res.Item2)) outEither.Value = res.Item1;
                    else outEither.AddMessage(new ErrorMessage(setting.ValueRange, res.Item1, res.Item2, res.Item3));
                }
                return outEither;
            }
            public static ParseResult<string> GetAllText(ReadFormat.FileIdentifier file) {
                if (file.Origin != null) return GetAllText(file.Origin);
                else return GetAllText(file.Path);
            }
            public static ParseResult<string> GetAllText(string path) {
                var outEither = new ParseResult<string>();

                var res = GetAllTextPrivate(path);

                if (string.IsNullOrEmpty(res.Item2)) outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            public static ParseResult<string> TestFileExists(string path) {
                var outEither = new ParseResult<string>();

                var res = TestReadFile(path);

                if (string.IsNullOrEmpty(res.Item2)) outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            static (string, string, string) TestReadFile(string path) {
                var try_path = GetFullPathPrivate(path);

                if (string.IsNullOrEmpty(try_path.Item2)) {
                    if (Directory.Exists(try_path.Item1)) {
                        return ("Could not open file", "The file given is a directory.", "");
                    } else {
                        try {
                            File.OpenRead(try_path.Item1).Close();
                            return (try_path.Item1, "", "");
                        } catch (DirectoryNotFoundException) {
                            try {
                                var pieces = try_path.Item1.Split(new char[] { '\\', '/' });
                                var drive = pieces[0].Split(':')[0];
                                if (Directory.GetLogicalDrives().Contains($"{drive}:\\")) {
                                    string current_path = $"{drive}:\\";
                                    for (int i = 1; i < pieces.Length - 1; i++) {
                                        string next_path = current_path + pieces[i] + Path.DirectorySeparatorChar;

                                        if (!Directory.Exists(next_path)) {
                                            var directories = Directory.GetDirectories(current_path);
                                            var extra = "";

                                            if (directories.Length == 0) extra = "\nThere are no subfolders in this folder.";
                                            else if (directories.Length == 1) extra = $"\nThe only subfolder is '{directories[0]}'.";
                                            else {
                                                int max_value = 0;
                                                string max_name = "";
                                                foreach (var dir in directories) {
                                                    int score = HelperFunctionality.SmithWatermanStrings(dir, pieces[i]);
                                                    if (score > max_value) {
                                                        max_name = Path.GetFileName(dir);
                                                        max_value = score;
                                                    }
                                                }
                                                extra = $"\nDid you mean '{max_name}'?";
                                            }

                                            return ("Could not open file", "The path cannot be found.", $"The folder '{pieces[i]}' does not exist in '{pieces[i - 1]}'.{extra}");
                                        }
                                        current_path = next_path;
                                    }
                                    // Will likely be never used because that would raise a FileNotFoundException
                                    return ("Could not open file", "The path cannot be found.", $"The file '{pieces[^1]}' does not exist in '{current_path}'.");
                                } else {
                                    return ("Could not open file", "The path cannot be found.", $"The drive '{drive}:\\' is not mounted.");
                                }
                            } catch {
                                return ("Could not open file", "The path cannot be found, possibly on an unmapped drive.", "");
                            }
                        } catch (FileNotFoundException) {
                            int max_value = 0;
                            string max_name = "";
                            string name = Path.GetFileName(try_path.Item1);

                            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(try_path.Item1))) {
                                int score = HelperFunctionality.SmithWatermanStrings(file, name);
                                if (score > max_value) {
                                    max_name = Path.GetFileName(file);
                                    max_value = score;
                                }
                            }

                            return ("Could not open file", "The specified file could not be found.", $"Did you mean '{max_name}'?");
                        } catch (IOException) {
                            return ("Could not open file", "An IO error occurred while opening the file.", "Make sure it is not opened in another program, like Excel.");
                        } catch (UnauthorizedAccessException) {
                            return ("Could not open file", "Unauthorised access.", "Make sure you have the right permissions to open this file.");
                        } catch (System.Security.SecurityException) {
                            return ("Could not open file", "The caller does not have the required permission.", "Make sure you have the right permissions to open this file.");
                        }
                    }
                } else {
                    return (try_path.Item1, try_path.Item2, "");
                }
            }

            static (string, string, string) GetAllTextPrivate(string path) {
                var res = TestReadFile(path);
                if (string.IsNullOrEmpty(res.Item2))
                    res.Item1 = File.ReadAllText(res.Item1);
                return res;
            }
        }
    }
}