using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Stitch.InputNameSpace;
using Stitch.RunParameters;

namespace Stitch
{
    /// <summary> A class with options to parse a batch file. </summary> 
    public static class ParseCommandFile
    {
        /// <summary> Parses a batch file and retrieves the run parameters or fails with an exception. </summary>
        /// <param name="path"> The path to the batch file. </param>
        /// <returns> The run parameters as specified in the file. </returns>
        public static FullRunParameters Batch(string path)
        {
            var output = new FullRunParameters();
            var outEither = new ParseResult<FullRunParameters>(output);
            var name_filter = new NameFilter();

            // Get the contents
            string batchfile_content = ParseHelper.GetAllText(path).Unwrap().Replace("\t", "    "); // Remove tabs, in tabs vs spaces obviously go for spaces ;-)

            // Set the working directory to the directory of the batchfile
            var original_working_directory = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            }

            // Save the batchfile for use in the construction of error messages
            var batchfile = new ParsedFile(path, batchfile_content.Split('\n'), "Batchfile", null);
            output.BatchFile = batchfile;

            // Tokenize the file, into a key value pair tree
            var parsed = InputNameSpace.Tokenizer.Tokenize(batchfile).Unwrap();

            // Now all key value pairs are saved in 'parsed'
            // Now parse the key value pairs into RunParameters

            bool version_specified = false;
            List<KeyValue> order_groups = null;
            KeyValue readAlignmentKey = null;

            // Match every possible key to the corresponding action
            foreach (var pair in parsed)
            {
                switch (pair.Name)
                {
                    case "runname":
                        output.Runname = pair.GetValue().GetValueOrDefault(outEither, "");
                        break;
                    case "rawdatadirectory":
                        if (output.RawDataDirectory != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.RawDataDirectory = ParseHelper.GetFullPath(pair).GetValue(outEither);
                        if (!Directory.Exists(output.RawDataDirectory))
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "Could not find RawDataDirectory.", "Execution will continue, but the spectra will be missing from all reports.", "", true));
                            output.RawDataDirectory = null;
                        }
                        break;
                    case "version":
                        var version = ParseHelper.ConvertToDouble(pair).GetValue(outEither);
                        if (version < 1.0)
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "Batchfile versions below '1.0' (pre release versions) are deprecated, please change to version '1.x'."));
                        }
                        if (version >= 2.0)
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "This version of Stitch cannot handle batchfiles major version 2.0 or higher, please change to version '1.x'."));
                        }
                        version_specified = true;
                        break;
                    case "maxcores":
                        output.MaxNumberOfCPUCores = ParseHelper.ConvertToInt(pair).RestrictRange(ParseHelper.NumberRange<int>.Open(0), pair.ValueRange).GetValue(outEither);
                        break;
                    case "input":
                        if (output.Input.Parameters != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.Input.Parameters = ParseHelper.ParseInputParameters(pair).GetValue(outEither);
                        break;
                    case "templatematching":
                        if (output.TemplateMatching != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.TemplateMatching = ParseHelper.ParseTemplateMatching(name_filter, pair).GetValue(outEither);
                        break;
                    case "recombine":
                        if (output.Recombine != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        (output.Recombine, order_groups, readAlignmentKey) = ParseHelper.ParseRecombine(pair).GetValue(outEither);
                        break;
                    case "report":
                        if (output.Report != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.Report = ParseHelper.ParseReport(pair).GetValue(outEither);
                        break;
                    default:
                        outEither.AddMessage(ErrorMessage.UnknownKey(pair.KeyRange.Name, "batchfile", "'Runname', 'Version', 'MaxCores', 'Input', 'TemplateMatching', 'Recombine' or 'Report'"));
                        break;
                }
            }

            var def_position = new Position(0, 1, batchfile);
            var def_range = new FileRange(def_position, def_position);

            // Detect missing parameters
            if (string.IsNullOrWhiteSpace(output.Runname)) output.Runname = Path.GetFileNameWithoutExtension(path);
            if (output.Recombine != null && output.TemplateMatching == null) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "TemplateMatching"));
            if (output.Report == null || output.Report.Files.Count == 0) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Any report parameter"));
            else
            {
                // Test validity of FASTA output type
                foreach (var report in output.Report.Files)
                {
                    if (report is RunParameters.Report.FASTA fa)
                    {
                        if (fa.OutputType == RunParameters.Report.OutputType.Recombine && output.Recombine == null)
                        {
                            outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Recombine, because FASTA output was set to 'Recombine'"));
                        }
                    }
                }
            }

            // Parse Recombination order
            if (output.Recombine != null && output.TemplateMatching != null)
            {
                foreach (var group in output.TemplateMatching.Segments)
                {
                    if (group.Name.ToLower() == "decoy")
                        continue;
                    else if (!order_groups.Exists(o => o.OriginalName == group.Name))
                        outEither.AddMessage(new ErrorMessage(batchfile, "Missing order definition for template group", $"For group \"{group.Name}\" there is no corresponding order definition.", "If there is a definition make sure it is written exactly the same and with the same casing."));
                    else
                    {
                        var order = order_groups.Find(o => o.OriginalName == group.Name);
                        var order_res = order.GetValue();
                        // Create a new counter
                        var order_counter = new InputNameSpace.Tokenizer.Counter(order.ValueRange.Start);
                        var order_output = new List<RunParameters.RecombineOrder.OrderPiece>();
                        if (order_res.IsOk(outEither))
                        {
                            var order_string = order_res.Unwrap();
                            while (!string.IsNullOrEmpty(order_string))
                            {
                                InputNameSpace.Tokenizer.ParseHelper.Trim(ref order_string, order_counter);

                                var match = false;

                                for (int j = 0; j < group.Segments.Count; j++)
                                {
                                    var template = group.Segments[j];
                                    var len = template.Name.Length;
                                    if (order_string.StartsWith(template.Name) && (order_string.Length == len || Char.IsWhiteSpace(order_string[len]) || order_string[len] == '*'))
                                    {
                                        order_string = order_string.Remove(0, template.Name.Length);
                                        order_counter.NextColumn(template.Name.Length);
                                        order_output.Add(new RunParameters.RecombineOrder.Template(j));
                                        match = true;
                                        break;
                                    }

                                }
                                if (match) continue;

                                if (order_string.StartsWith('*'))
                                {
                                    order_string = order_string.Remove(0, 1);
                                    order_counter.NextColumn();
                                    order_output.Add(new RunParameters.RecombineOrder.Gap());
                                }
                                else
                                {
                                    outEither.AddMessage(new ErrorMessage(new FileRange(order_counter.GetPosition(), order.ValueRange.End), "Invalid order", "Valid options are a name of a template, a gap ('*') or whitespace."));
                                    break;
                                }
                            }
                        }

                        output.Recombine.Order.Add(order_output);
                    }
                }
            }

            if (output.Recombine != null && output.Recombine.Order.Count != 0)
            {
                if (output.TemplateMatching.Segments.Count
                    - (output.TemplateMatching.Segments.Exists(group => string.IsNullOrEmpty(group.Name)) ? 1 : 0)
                    - (output.TemplateMatching.Segments.Exists(group => group.Name.ToLower() == "decoy") ? 1 : 0)
                    != output.Recombine.Order.Count)
                {
                    outEither.AddMessage(new ErrorMessage(def_range, "Invalid segment groups definition", $"The number of order definitions ({output.Recombine.Order.Count}) should equal the number of segment groups ({output.TemplateMatching.Segments.Count})."));
                }
                else
                {
                    int offset = 0;
                    for (int i = 0; i < output.TemplateMatching.Segments.Count; i++)
                    {
                        if (output.TemplateMatching.Segments[i].Name.ToLower() == "decoy") { offset += 1; continue; };
                        int index = i - offset;
                        var order = order_groups[index];
                        int last = -2;
                        foreach (var piece in output.Recombine.Order[index])
                        {
                            if (piece.IsGap())
                            {
                                if (last == -1)
                                    outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "Gaps cannot follow consecutively."));
                                else if (last == -2)
                                    outEither.AddMessage(new ErrorMessage(new FileRange(order.ValueRange.Start, order.ValueRange.End), "Invalid order", "An order definition cannot start with a gap (*)."));
                                else
                                {
                                    output.TemplateMatching.Segments[i].Segments[last].GapTail = true;
                                    last = -1;
                                }
                            }
                            else
                            {
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
            if (output.Input != null) outEither.Messages.AddRange(ParseHelper.PrepareInput(name_filter, null, output.Input, null, new Alphabet(output.TemplateMatching.Alphabet)).Messages);

            // Check if there is a version specified
            if (!version_specified)
            {
                outEither.AddMessage(new ErrorMessage(def_range, "No version specified", "There is no version specified for the batch file; This is needed to handle different versions in different ways."));
            }

            // Reset the working directory
            Directory.SetCurrentDirectory(original_working_directory);

            if (output.TemplateMatching != null)
            {
                foreach (var db in output.TemplateMatching.Segments.SelectMany(group => group.Segments))
                {
                    if (db.Templates != null)
                    {
                        for (var i = 0; i < db.Templates.Count; i++)
                        {
                            var read = db.Templates[i];
                            if (db.GapTail)
                            {
                                read.Item1 += "XXXXXXXXXXXXXXXXXXXX";
                                if (read.Item2 is ReadMetaData.Fasta meta)
                                    meta.AnnotatedSequence[^1] = (meta.AnnotatedSequence[^1].Type, meta.AnnotatedSequence[^1].Sequence + "XXXXXXXXXXXXXXXXXXXX");
                            }
                            if (db.GapHead)
                            {
                                read.Item1 = $"XXXXXXXXXXXXXXXXXXXX{read.Item1}";
                                if (read.Item2 is ReadMetaData.Fasta meta)
                                    meta.AnnotatedSequence[0] = (meta.AnnotatedSequence[0].Type, "XXXXXXXXXXXXXXXXXXXX" + meta.AnnotatedSequence[0].Sequence);
                            }
                            db.Templates[i] = read;
                        }
                    }
                }
            }
            return outEither.Unwrap();
        }
    }
    /// <summary> An exception to indicate some error while parsing the batch file </summary> 
    public class ParseException : Exception
    {
        /// <summary> To create a ParseException </summary>
        /// <param name="msg">The message for this Exception</param>
        public ParseException(string msg)
            : base(msg) { }
    }
}
