using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AssemblyNameSpace.InputNameSpace;
using System.Text;
using System.Text.RegularExpressions;
using AssemblyNameSpace.RunParameters;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A class with options to parse a batch file.
    /// </summary>
    public static class ParseCommandFile
    {
        /// <summary>
        /// Parses a batch file and retrieves the 
        /// </summary>
        /// <param name="path">The path to the batch file.</param>
        /// <returns>The runparameters as specified in the file.</returns>
        public static FullRunParameters Batch(string path, bool languageServer = false)
        {
            var output = new FullRunParameters();
            var outEither = new ParseResult<FullRunParameters>(output);
            var namefilter = new NameFilter();

            // Get the contents
            string batchfilecontent = ParseHelper.GetAllText(path).ReturnOrFail().Replace("\t", "    "); // Remove tabs

            // Set the working directory to the directory of the batchfile
            var original_working_directory = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            }

            // Save the batchfile for use in the construction of error messages
            var batchfile = new ParsedFile(path, batchfilecontent.Split('\n'));
            output.BatchFile = batchfile;

            // Tokenize the file, into a key value pair tree
            var parsed = InputNameSpace.Tokenizer.Tokenize(batchfile);

            // Now all key value pairs are saved in 'parsed'
            // Now parse the key value pairs into RunParameters

            bool versionspecified = false;
            List<KeyValue> order_groups = null;
            KeyValue readAlignmentKey = null;

            // Match every possible key to the corresponding action
            foreach (var pair in parsed)
            {
                switch (pair.Name)
                {
                    case "runname":
                        output.Runname = pair.GetValue();
                        break;
                    case "version":
                        var version = ParseHelper.ConvertToDouble(pair.GetValue(), pair.ValueRange).GetValue(outEither);
                        if (version < 1.0)
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "Batchfile versions below '1.0' (prerelease versions) are deprecated, please change to version '1.x'."));
                        }
                        if (version >= 2.0)
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "This version of Stitch cannot handle batchfiles major version 2.0 or higher, please change to version '1.x'."));
                        }
                        versionspecified = true;
                        break;
                    case "maxcores":
                        output.MaxNumberOfCPUCores = ParseHelper.ConvertToInt(pair.GetValue(), pair.ValueRange).GetValue(outEither);
                        break;
                    case "input":
                        if (output.Input.Parameters != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.Input.Parameters = ParseHelper.ParseInputParameters(pair).GetValue(outEither);
                        break;
                    case "templatematching":
                        if (output.TemplateMatching != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                        output.TemplateMatching = ParseHelper.ParseTemplateMatching(namefilter, pair).GetValue(outEither);
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
            if (string.IsNullOrWhiteSpace(output.Runname)) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Runname"));
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
                        var order_string = order.GetValue();
                        // Create a new counter
                        var order_counter = new InputNameSpace.Tokenizer.Counter(order.ValueRange.Start);
                        var order_output = new List<RunParameters.RecombineOrder.OrderPiece>();

                        while (!string.IsNullOrEmpty(order_string))
                        {
                            InputNameSpace.Tokenizer.ParseHelper.Trim(ref order_string, order_counter);

                            var match = false;

                            for (int j = 0; j < group.Segments.Count; j++)
                            {
                                var template = group.Segments[j];
                                if (order_string.StartsWith(template.Name))
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
            if (output.Input != null) outEither.Messages.AddRange(ParseHelper.PrepareInput(namefilter, null, output.Input, null, new Alphabet(output.TemplateMatching.Alphabet)).Messages);

            // Check if there is a version specified
            if (!versionspecified)
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

            if (languageServer)
            {
                foreach (var msg in outEither.Messages)
                {
                    msg.OutputForLanguageServer();
                }
                return null;
            }
            return outEither.ReturnOrFail();
        }
    }
    /// <summary>
    /// An exception to indicate some error while parsing the batch file
    /// </summary>
    public class ParseException : Exception
    {
        /// <summary>
        /// To create a ParseException
        /// </summary>
        /// <param name="msg">The message for this Exception</param>
        public ParseException(string msg)
            : base(msg) { }
    }
    /// <summary>To save a result of a parse action, the value or a errormessage. </summary>
    public class ParseResult<T>
    {
        public T Value;
        public List<ErrorMessage> Messages = new();
        public ParseResult(T t)
        {
            Value = t;
        }
        public ParseResult(ErrorMessage error)
        {
            Messages.Add(error);
        }
        public ParseResult(List<ErrorMessage> errors)
        {
            Messages.AddRange(errors);
        }
        public ParseResult() { }
        public bool HasFailed()
        {
            foreach (var msg in Messages)
            {
                if (!msg.Warning) return true;
            }
            return false;
        }
        public bool HasOnlyWarnings()
        {
            if (Messages.Count == 0) return false;
            foreach (var msg in Messages)
            {
                if (!msg.Warning) return false;
            }
            return true;
        }
        public T ReturnOrFail()
        {
            if (this.HasFailed())
            {
                PrintMessages();

                throw new ParseException("");
            }
            else
            {
                foreach (var msg in Messages)
                {
                    msg.Print();
                }

                return Value;
            }
        }
        public T ReturnOrDefault(T def)
        {
            if (this.HasFailed()) return def;
            else return this.Value;
        }
        public T GetValue<TOut>(ParseResult<TOut> fail)
        {
            fail.Messages.AddRange(Messages);
            return Value;
        }
        public void AddMessage(ErrorMessage failMessage)
        {
            Messages.Add(failMessage);
        }

        public void PrintMessages()
        {
            if (this.HasFailed())
            {
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"There were {Messages.Count} error(s) while parsing.\n");
                Console.ForegroundColor = defaultColour;

                foreach (var msg in Messages)
                {
                    msg.Print();
                }
            }
        }
    }
    /// <summary>
    /// To contain all input functionality
    /// </summary>
    namespace InputNameSpace
    {
        /// <summary>
        /// A class with helper functionality for parsing
        /// </summary>
        static class ParseHelper
        {
            /// <summary>
            /// Split a line by a separator and return the trimmed pieces and their position as a FileRange.
            /// </summary>
            /// <param name="separator">The separator to use.</param>
            /// <param name="linenumber">The linenumber.</param>
            /// <param name="parsefile">The file where the line should be taken from.</param>
            public static List<(string Text, FileRange Pos)> SplitLine(char separator, int linenumber, ParsedFile parsefile)
            {
                var results = new List<(string, FileRange)>();
                var lastpos = 0;
                var line = parsefile.Lines[linenumber];

                // Find the fields on this line
                for (int pos = 0; pos < line.Length; pos++)
                {
                    if (line[pos] == separator)
                    {
                        results.Add((
                            line.Substring(lastpos, pos - lastpos).Trim(),
                            new FileRange(new Position(linenumber, lastpos + 1, parsefile), new Position(linenumber, pos + 1, parsefile))
                        ));
                        lastpos = pos + 1;
                    }
                }

                results.Add((
                    line.Substring(lastpos, line.Length - lastpos).Trim(),
                    new FileRange(new Position(linenumber, lastpos, parsefile), new Position(linenumber, Math.Max(0, line.Length - 1), parsefile))
                ));
                return results;
            }
            /// <summary>
            /// Converts a string to an int, while it generates meaningfull error messages for the end user.
            /// </summary>
            /// <param name="input">The string to be converted to an int.</param>
            /// <returns>If successfull: the number (int32)</returns>
            public static ParseResult<int> ConvertToInt(string input, FileRange pos)
            {
                try
                {
                    return new ParseResult<int>(Convert.ToInt32(input));
                }
                catch (FormatException)
                {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseResult<int>(new ErrorMessage(pos, "Not a valid number", msg));
                }
                catch (OverflowException)
                {
                    return new ParseResult<int>(new ErrorMessage(pos, "Outside bounds"));
                }
                catch
                {
                    return new ParseResult<int>(new ErrorMessage(pos, "Unknown exception", "This is not a valid number and an unkown exception occurred."));
                }
            }
            /// <summary>
            /// Converts a string to a double, while it generates meaningfull error messages for the end user.
            /// </summary>
            /// <param name="input">The string to be converted to a double.</param>
            /// <returns>If successfull: the number (double)</returns>
            public static ParseResult<double> ConvertToDouble(string input, FileRange pos)
            {
                try
                {
                    return new ParseResult<double>(Convert.ToDouble(input, new System.Globalization.CultureInfo("en-US")));
                }
                catch (FormatException)
                {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseResult<double>(new ErrorMessage(pos, "Not a valid number", msg));
                }
                catch (OverflowException)
                {
                    return new ParseResult<double>(new ErrorMessage(pos, "Outside bounds"));
                }
                catch
                {
                    return new ParseResult<double>(new ErrorMessage(pos, "Unknown exception", "This is not a valid number and an unkown exception occurred."));
                }
            }
            public static ParseResult<InputData.InputParameters> ParseInputParameters(KeyValue key)
            {
                var outEither = new ParseResult<InputData.InputParameters>();
                var output = new InputData.InputParameters();

                foreach (var pair in key.GetValues())
                {
                    switch (pair.Name)
                    {
                        case "peaks":
                            var settings = new InputData.Peaks();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(settings.File.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        settings.File.Path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                        break;
                                    case "name":
                                        if (!string.IsNullOrWhiteSpace(settings.File.Name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        settings.File.Name = setting.GetValue();
                                        break;
                                    default:
                                        var peaks = ParseHelper.GetPeaksSettings(setting, false, settings);
                                        outEither.Messages.AddRange(peaks.Messages);

                                        if (peaks.Value == false)
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "PEAKS", "'Path', 'CutoffScore', 'LocalCutoffscore', 'MinLengthPatch', 'Name', 'Separator', 'DecimalSeparator' and 'Format'"));

                                        break;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(settings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(settings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(settings);
                            break;

                        case "reads":
                            var rsettings = new InputData.Reads();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(rsettings.File.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        rsettings.File.Path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                        break;
                                    case "name":
                                        if (!string.IsNullOrWhiteSpace(rsettings.File.Name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        rsettings.File.Name = setting.GetValue();
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Reads", "'Path' and 'Name'"));
                                        break;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(rsettings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(rsettings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(rsettings);
                            break;

                        case "novor":
                            var novor_settings = new InputData.Novor();
                            string name = null;
                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "denovo path":
                                        if (novor_settings.DeNovoFile != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        novor_settings.DeNovoFile = new ReadMetaData.FileIdentifier(ParseHelper.GetFullPath(setting).GetValue(outEither), "", setting);
                                        break;
                                    case "psms path":
                                        if (novor_settings.PSMSFile != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        novor_settings.PSMSFile = new ReadMetaData.FileIdentifier(ParseHelper.GetFullPath(setting).GetValue(outEither), "", setting);
                                        break;
                                    case "name":
                                        if (!string.IsNullOrWhiteSpace(name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        name = setting.GetValue();
                                        break;
                                    case "separator":
                                        if (setting.GetValue().Length != 1)
                                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                                        else
                                            novor_settings.Separator = setting.GetValue().First();
                                        break;
                                    case "cutoff":
                                        var value = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        if (value < 0)
                                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Cutoff range", "The minimal value for the Novor cutoff is 0."));
                                        else if (value > 100)
                                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Cutoff range", "The maximal value for the Novor cutoff is 100."));
                                        else
                                            novor_settings.Cutoff = (uint)value;
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Novor", "'Path', 'Name' and 'Separator'"));
                                        break;
                                }
                            }

                            if (novor_settings.DeNovoFile == null && novor_settings.PSMSFile == null) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "DeNovo Path OR PSMS Path"));
                            if (string.IsNullOrWhiteSpace(name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));
                            if (novor_settings.DeNovoFile != null) novor_settings.DeNovoFile.Name = name;
                            if (novor_settings.PSMSFile != null) novor_settings.PSMSFile.Name = name;
                            output.Files.Add(novor_settings);
                            break;

                        case "fasta":
                            var fastasettings = new InputData.FASTA();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(fastasettings.File.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        fastasettings.File.Path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                        break;
                                    case "name":
                                        if (!string.IsNullOrWhiteSpace(fastasettings.File.Name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        fastasettings.File.Name = setting.GetValue();
                                        break;
                                    case "identifier":
                                        fastasettings.Identifier = ParseHelper.ParseRegex(setting).GetValue(outEither);
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "FASTAInput", "'Path' and 'Name'"));
                                        break;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(fastasettings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            if (string.IsNullOrWhiteSpace(fastasettings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                            output.Files.Add(fastasettings);
                            break;
                        case "folder":
                            // Parse files one by one
                            var folder_path = "";
                            FileRange? folder_range = null;
                            var startswith = "";
                            var identifier = new Regex(".*");
                            bool recursive = false;

                            var peaks_settings = new InputData.Peaks();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(folder_path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        folder_path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                        folder_range = setting.ValueRange;
                                        break;
                                    case "startswith":
                                        if (!string.IsNullOrWhiteSpace(startswith)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        startswith = setting.GetValue();
                                        break;
                                    case "identifier":
                                        identifier = ParseHelper.ParseRegex(setting).GetValue(outEither);
                                        break;
                                    case "recursive":
                                        recursive = ParseHelper.ParseBool(setting, "Recursive").GetValue(outEither);
                                        break;
                                    default:
                                        var peaks = ParseHelper.GetPeaksSettings(setting, true, peaks_settings);
                                        outEither.Messages.AddRange(peaks.Messages);

                                        if (peaks.Value == false)
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Folder", "'Path' and 'StartsWith'"));

                                        break;
                                }
                            }

                            if (!folder_range.HasValue)
                            {
                                outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                                break;
                            }

                            var files = GetAllFilesPrivate(folder_path, recursive);

                            if (files.Item1.Length != 0)
                            {
                                foreach (var file in files.Item1)
                                {
                                    if (!Path.GetFileName(file).StartsWith(startswith)) continue;

                                    var fileId = new ReadMetaData.FileIdentifier() { Name = Path.GetFileNameWithoutExtension(file), Path = ParseHelper.GetFullPath(file).GetValue(outEither) };

                                    if (file.EndsWith(".fasta"))
                                        output.Files.Add(new InputData.FASTA() { File = fileId, Identifier = identifier });
                                    else if (file.EndsWith(".txt"))
                                        output.Files.Add(new InputData.Reads() { File = fileId });
                                    else if (file.EndsWith(".csv"))
                                        output.Files.Add(new InputData.Peaks() { File = fileId, FileFormat = peaks_settings.FileFormat, Parameter = peaks_settings.Parameter });
                                    else
                                        continue;
                                }
                            }
                            else
                            {
                                outEither.AddMessage(new ErrorMessage(folder_range.Value, files.Item2, files.Item3, files.Item4));
                            }

                            break;
                        default:
                            outEither.AddMessage(ErrorMessage.UnknownKey(pair.KeyRange.Name, "Input", "'Peaks', 'Reads', 'Fasta', 'Novor' or 'Folder'"));
                            break;
                    }
                }

                outEither.Value = output;
                return outEither;
            }
            /// <param name="global">The global InputParameters, if specified, otherwise null.</param>
            public static ParseResult<bool> PrepareInput(NameFilter namefilter, KeyValue key, InputData Input, InputData.InputParameters GlobalInput, Alphabet alp)
            {
                var result = new ParseResult<bool>();
                result.Value = true;

                if (GlobalInput == null && Input.Parameters == null)
                {
                    result.AddMessage(new ErrorMessage(key.KeyRange.Name, "Missing Input", "No Input is provided either local or global.", "Please provide local input, using the 'Input' parameter, or provide global input, using the 'Input' parameter in the global scope."));
                    return result;
                }
                if (GlobalInput != null && Input.Parameters != null)
                {
                    result.AddMessage(new ErrorMessage(key.KeyRange.Name, "Duplicate Input", "Both local and global input is provided.", "Please provide only local or only global input."));
                    return result;
                }

                var input = GlobalInput ?? Input.Parameters;

                foreach (var file in input.Files)
                {
                    var reads = file switch
                    {
                        InputData.Peaks peaks => OpenReads.Peaks(namefilter, peaks, Input.LocalParameters),
                        InputData.FASTA fasta => OpenReads.Fasta(namefilter, fasta.File, fasta.Identifier),
                        InputData.Reads simple => OpenReads.Simple(namefilter, simple.File),
                        InputData.Novor novor => OpenReads.Novor(namefilter, novor),
                        _ => throw new ArgumentException("An unkown inputformat was provided to PrepareInput")
                    };
                    result.Messages.AddRange(reads.Messages);
                    if (!reads.HasFailed()) Input.Data.Raw.Add(reads.ReturnOrFail());
                }

                Input.Data.Cleaned = OpenReads.CleanUpInput(Input.Data.Raw, alp).GetValue(result);

                return result;
            }
            public static ParseResult<TemplateMatchingParameter> ParseTemplateMatching(NameFilter nameFilter, KeyValue key)
            {
                var outEither = new ParseResult<TemplateMatchingParameter>();
                var output = new TemplateMatchingParameter();

                foreach (var setting in key.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "cutoffscore":
                            output.CutoffScore = ParseHelper.ConvertToDouble(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        case "segments":
                            if (output.Segments.Count != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            var outer_children = new List<SegmentValue>();
                            foreach (var segment in setting.GetValues())
                            {
                                if (segment.Name == "segment")
                                {
                                    var segmentvalue = ParseHelper.ParseSegment(nameFilter, segment, false).GetValue(outEither);

                                    // Check to see if the name is valid
                                    if (outer_children.Select(db => db.Name).Contains(segmentvalue.Name))
                                        outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names have to be unique."));
                                    if (segmentvalue.Name.Contains('*'))
                                        outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names cannot contain '*'."));
                                    outer_children.Add(segmentvalue);
                                }
                                else
                                {
                                    var children = new List<SegmentValue>();
                                    foreach (var sub_segment in segment.GetValues())
                                    {
                                        var segmentvalue = ParseHelper.ParseSegment(nameFilter, sub_segment, false).GetValue(outEither);

                                        // Check to see if the name is valid
                                        if (children.Select(db => db.Name).Contains(segmentvalue.Name))
                                            outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names have to be unique, within their scope."));
                                        if (segmentvalue.Name.Contains('*'))
                                            outEither.AddMessage(new ErrorMessage(segment.KeyRange.Full, "Invalid name", "Segment names cannot contain '*'."));
                                        children.Add(segmentvalue);
                                    }
                                    output.Segments.Add((segment.OriginalName, children));
                                }
                            }
                            if (outer_children.Count > 0) output.Segments.Add(("", outer_children));
                            break;
                        case "alphabet":
                            if (output.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            output.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            break;
                        case "enforceunique":
                            output.EnforceUnique = ParseBool(setting, "EnforceUnique").GetValue(outEither);
                            break;
                        case "forcegermlineisoleucine":
                            output.ForceGermlineIsoleucine = ParseBool(setting, "ForceGermlineIsoleucine").GetValue(outEither);
                            break;
                        default:
                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "TemplateMatching", "'CutoffScore', 'Segments', 'Alphabet', 'EnforceUnique', and 'ForceGermlineIsoleucine'"));
                            break;
                    }
                }

                if (output.Segments.Count > 1)
                    foreach (var db in output.Segments)
                        if (string.IsNullOrEmpty(db.Name))
                            outEither.AddMessage(new ErrorMessage(key.KeyRange.Full, "Single segments in grouped segment list", "You cannot define a single segment when there are also segment groups defined."));

                if (output.Alphabet == null)
                    outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Name, "Alphabet"));

                if (output.Segments.Count == 0)
                    outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Name, "Any segment"));

                outEither.Value = output;
                return outEither;
            }
            public static ParseResult<(RecombineParameter, List<KeyValue>, KeyValue)> ParseRecombine(KeyValue key)
            {
                var outEither = new ParseResult<(RecombineParameter, List<KeyValue>, KeyValue)>();
                var output = new RecombineParameter();

                var order = new List<KeyValue>();
                KeyValue readAlignmentKey = null;

                foreach (var setting in key.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "n":
                            output.N = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        case "order":
                            if (order.Count != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            if (setting.IsSingle()) order.Add(setting);
                            else
                                foreach (var group in setting.GetValues())
                                    order.Add(group);
                            break;
                        case "cutoffscore":
                            output.CutoffScore = ParseHelper.ConvertToDouble(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        case "alphabet":
                            if (output.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            output.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            break;
                        case "enforceunique":
                            output.EnforceUnique = ParseBool(setting, "EnforceUnique").GetValue(outEither) ? Trilean.True : Trilean.False;
                            break;
                        case "forcegermlineisoleucine":
                            output.ForceGermlineIsoleucine = ParseBool(setting, "ForceGermlineIsoleucine").GetValue(outEither) ? Trilean.True : Trilean.False;
                            break;
                        case "decoy":
                            output.Decoy = ParseBool(setting, "Decoy").GetValue(outEither);
                            break;
                        default:
                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Recombine", "'N', 'Order', 'CutoffScore', 'Alphabet', 'Decoy', 'EnforceUnique', and 'ForceGermlineIsoleucine'"));
                            break;
                    }
                }

                if (order == null)
                    outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Order"));

                outEither.Value = (output, order, readAlignmentKey);
                return outEither;
            }

            public static ParseResult<ReportParameter> ParseReport(KeyValue key)
            {
                var outEither = new ParseResult<ReportParameter>();
                var output = new ReportParameter();

                foreach (var pair in key.GetValues())
                {
                    switch (pair.Name)
                    {
                        case "folder":
                            if (output.Folder != null) outEither.AddMessage(ErrorMessage.DuplicateValue(pair.KeyRange.Name));
                            output.Folder = Path.GetFullPath(pair.GetValue());
                            break;
                        case "html":
                            var hsettings = new RunParameters.Report.HTML();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(hsettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        hsettings.Path = setting.GetValue();
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "HTML", "'Path' and 'DotDistribution'"));
                                        break;
                                }
                            }
                            if (string.IsNullOrWhiteSpace(hsettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            output.Files.Add(hsettings);
                            break;
                        case "fasta":
                            var fsettings = new RunParameters.Report.FASTA();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(fsettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        fsettings.Path = setting.GetValue();
                                        break;
                                    case "minimalscore":
                                        fsettings.MinimalScore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    case "outputtype":
                                        switch (setting.GetValue().ToLower())
                                        {
                                            case "templatematching":
                                                fsettings.OutputType = RunParameters.Report.OutputType.TemplateMatches;
                                                break;
                                            case "recombine":
                                                fsettings.OutputType = RunParameters.Report.OutputType.Recombine;
                                                break;
                                            default:
                                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "FASTA OutputType", "'TemplateMatching' and 'Recombine'"));
                                                break;
                                        }
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "FASTA", "'Path', 'MinimalScore' and 'OutputType'"));
                                        break;
                                }
                            }
                            if (string.IsNullOrWhiteSpace(fsettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            output.Files.Add(fsettings);
                            break;
                        case "csv":
                            var csettings = new RunParameters.Report.CSV();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "path":
                                        if (!string.IsNullOrWhiteSpace(csettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        csettings.Path = setting.GetValue();
                                        break;
                                    case "outputtype":
                                        switch (setting.GetValue().ToLower())
                                        {
                                            case "templatematching":
                                                csettings.OutputType = RunParameters.Report.OutputType.TemplateMatches;
                                                break;
                                            case "recombine":
                                                csettings.OutputType = RunParameters.Report.OutputType.Recombine;
                                                break;
                                            default:
                                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "CSV OutputType", "'TemplateMatching' and 'Recombine'"));
                                                break;
                                        }
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "CSV", "'Path' and 'OutputType"));
                                        break;
                                }
                            }
                            if (string.IsNullOrWhiteSpace(csettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                            output.Files.Add(csettings);
                            break;
                        default:
                            outEither.AddMessage(ErrorMessage.UnknownKey(pair.KeyRange.Name, "Report", "'HTML', 'FASTA' and 'CSV'"));
                            break;
                    }
                }
                if (output.Folder == null)
                    output.Folder = Directory.GetCurrentDirectory();
                outEither.Value = output;
                return outEither;
            }
            public static ParseResult<AlphabetParameter> ParseAlphabet(KeyValue key)
            {
                var asettings = new AlphabetParameter();
                var outEither = new ParseResult<AlphabetParameter>(asettings);

                if (key.GetValues().Count == 0)
                {
                    outEither.AddMessage(new ErrorMessage(key.KeyRange.Full, "No arguments", "No arguments are supplied with the Alphabet definition."));
                    return outEither;
                }

                (char[], int[,]) result;

                foreach (var setting in key.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            if (asettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            var alltext = GetAllText(setting);

                            if (alltext.HasFailed())
                            {
                                alltext.GetValue(outEither);
                                return outEither;
                            }

                            var content = alltext.GetValue(outEither).Split("\n");
                            var counter = new Tokenizer.Counter(new ParsedFile(GetFullPath(setting).GetValue(outEither), content));
                            result = ParseAlphabetData(content, counter).GetValue(outEither);
                            asettings.Alphabet = result.Item1;
                            asettings.ScoringMatrix = result.Item2;

                            break;
                        case "data":
                            if (asettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            var data_content = setting.GetValue().Split("\n");
                            var data_counter = new Tokenizer.Counter(setting.ValueRange.Start);
                            result = ParseAlphabetData(data_content, data_counter).GetValue(outEither);
                            asettings.Alphabet = result.Item1;
                            asettings.ScoringMatrix = result.Item2;
                            break;
                        case "name":
                            if (!string.IsNullOrEmpty(asettings.Name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            asettings.Name = setting.GetValue();
                            break;
                        case "gapstartpenalty":
                            asettings.GapStartPenalty = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        case "gapextendpenalty":
                            asettings.GapExtendPenalty = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        default:
                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Alphabet", "'Path', 'Data', 'Name', 'GapStartPenalty' and 'GapExtendPenalty'"));
                            break;
                    }
                }

                if (asettings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Name"));
                if (asettings.ScoringMatrix == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Data or Path"));

                return outEither;
            }
            public static (char[], int[,]) ParseAlphabetData(string data, Alphabet.AlphabetParamType type)
            {
                var lines = data.Split('\n');
                var file = new ParsedFile("Inline alphabet", lines);
                if (type == Alphabet.AlphabetParamType.Path)
                {
                    lines = GetAllText(data).ReturnOrFail().Split('\n');
                    file = new ParsedFile(data, lines);
                }
                var counter = new Tokenizer.Counter(file);
                return ParseAlphabetData(lines, counter).ReturnOrFail();
            }
            public static ParseResult<(char[], int[,])> ParseAlphabetData(string[] lines, Tokenizer.Counter counter)
            {
                var outEither = new ParseResult<(char[], int[,])>();

                int rows = lines.Length;
                var cells = new List<(Position, List<(string, FileRange)>)>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var startline = counter.GetPosition();
                    var splitline = new List<(string, FileRange)>();
                    var line = lines[i];
                    Tokenizer.ParseHelper.Trim(ref line, counter);

                    while (!string.IsNullOrEmpty(line))
                    {
                        if (line[0] == ';' || line[0] == ',')
                        {
                            line = line.Remove(0, 1);
                            counter.NextColumn();
                        }
                        else
                        {
                            var start = counter.GetPosition();
                            var cell = Tokenizer.ParseHelper.UntilOneOf(ref line, new char[] { ';', ',' }, counter);
                            var range = new FileRange(start, counter.GetPosition());
                            splitline.Add((cell, range));
                        }
                        Tokenizer.ParseHelper.Trim(ref line, counter);
                    }
                    cells.Add((startline, splitline));
                    counter.NextLine();
                }

                int columns = cells[0].Item2.Count;

                for (int line = 0; line < rows; line++)
                {
                    if (rows > cells[line].Item2.Count)
                    {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {rows - cells[line].Item2.Count} column(s) missing on this row."));
                    }
                    else if (rows < cells[line].Item2.Count)
                    {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {cells[line].Item2.Count - rows} additional column(s) on this row."));
                    }
                }

                var alphabetBuilder = new StringBuilder();
                foreach (var element in cells[0].Item2.Skip(1))
                {
                    alphabetBuilder.Append(element.Item1);
                }
                var alphabet = alphabetBuilder.ToString().Trim().ToCharArray();

                if (!alphabet.Contains(Alphabet.GapChar))
                {
                    outEither.AddMessage(new ErrorMessage(counter.File, "GapChar missing", $"The Gap '{Alphabet.GapChar}' is missing in the alpabet definition.", "", true));
                }

                var scoring_matrix = new int[columns - 1, columns - 1];

                for (int i = 0; i < columns - 1; i++)
                {
                    for (int j = 0; j < columns - 1; j++)
                    {
                        try
                        {
                            scoring_matrix[i, j] = ConvertToInt(cells[i + 1].Item2[j + 1].Item1, cells[i + 1].Item2[j + 1].Item2).GetValue(outEither);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Invalid amount of cells will already be pointed out
                            //outEither.AddMessage(new ErrorMessage(cells[i + 1].Item1, "Cell out of range", $"Cell {i},{j} out of range."));
                        }
                    }
                }
                outEither.Value = (alphabet, scoring_matrix);
                return outEither;
            }
            public static ParseResult<Regex> ParseRegex(KeyValue node)
            {
                var outEither = new ParseResult<Regex>();
                try
                {
                    outEither.Value = new Regex(node.GetValue().Trim());

                    if (outEither.Value.GetGroupNumbers().Length <= 1)
                    {
                        outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx is invalid", "The given RegEx has no caputuring groups.", "To parse an identifier from the fasta header a capturing group (enclosed in parentheses '()') should be present enclosing the identifier. Example: '\\s*(\\w*)'"));
                    }
                    else if (outEither.Value.GetGroupNumbers().Length > 3)
                    {
                        outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx could be wrong", "The given RegEx has a lot of capturing groups, only the first two will be used.", "", true));
                    }
                }
                catch (ArgumentException)
                {
                    outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx is invalid", "The given Regex could not be parsed.", "See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference for a reference."));
                }
                return outEither;
            }
            /// <summary>
            /// Parses a Template
            /// </summary>
            /// <param name="node">The KeyValue to parse</param>
            /// <param name="extended">To determine if it is an extended (free standing) template or a template in a recombination definition</param>
            public static ParseResult<SegmentValue> ParseSegment(NameFilter namefilter, KeyValue node, bool extended)
            {
                // Parse files one by one
                var file_path = "";
                KeyValue file_pos = node;

                var peaks_settings = new InputData.Peaks();

                var tsettings = new SegmentValue();
                var outEither = new ParseResult<SegmentValue>(tsettings);

                foreach (var setting in node.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            if (!string.IsNullOrEmpty(file_path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            file_path = GetFullPath(setting).GetValue(outEither);
                            file_pos = setting;
                            break;
                        case "name":
                            if (tsettings.Name != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            tsettings.Name = setting.GetValue();
                            break;
                        case "cutoffscore":
                            if (extended) tsettings.CutoffScore = ParseHelper.ConvertToDouble(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            else outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "CutoffScore cannot be defined here", "Inside a template in the templates list of a recombination a CutoffScore should not be defined."));
                            break;
                        case "alphabet":
                            if (tsettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            if (!extended)
                            {
                                outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Alphabet cannot be defined here", "Inside a template in the templates list of a recombination an alphabet can not be defined."));
                            }
                            else
                            {
                                tsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            }
                            break;
                        case "identifier":
                            tsettings.Identifier = ParseHelper.ParseRegex(setting).GetValue(outEither);
                            break;
                        case "scoring":
                            var scoring = setting.GetValue().ToLower();
                            if (scoring == "absolute")
                            {
                                tsettings.Scoring = ScoringParameter.Absolute;
                            }
                            else if (scoring == "relative")
                            {
                                tsettings.Scoring = ScoringParameter.Relative;
                            }
                            else
                            {
                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "Scoring", "'Absolute' or 'Relative'"));
                            }
                            break;
                        case "gaphead":
                            tsettings.GapHead = ParseBool(setting, "GapHead").GetValue(outEither);
                            break;
                        case "gaptail":
                            tsettings.GapTail = ParseBool(setting, "GapTail").GetValue(outEither);
                            break;
                        default:
                            var peaks = GetPeaksSettings(setting, true, peaks_settings);
                            outEither.Messages.AddRange(peaks.Messages);

                            if (peaks.Value == false)
                            {
                                var options = "'Path', 'Type', 'Name', 'Alphabet', 'Scoring', and all PEAKS format parameters";
                                if (!extended) options = "'Path', 'Type', 'Name' and 'Scoring'";
                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Template", options));
                            }
                            break;
                    }
                }

                if (tsettings.Name == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Name"));
                if (file_path == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Path"));
                if (extended && tsettings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Alphabet"));

                // Open the file
                var fileId = new ReadMetaData.FileIdentifier(ParseHelper.GetFullPath(file_path).GetValue(outEither), tsettings.Name, file_pos);

                var folder_reads = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

                if (file_path.EndsWith(".fasta"))
                    folder_reads = OpenReads.Fasta(namefilter, fileId, tsettings.Identifier);
                else if (file_path.EndsWith(".txt"))
                    folder_reads = OpenReads.Simple(namefilter, fileId);
                else if (file_path.EndsWith(".csv"))
                {
                    peaks_settings.File = fileId;
                    folder_reads = OpenReads.Peaks(namefilter, peaks_settings);
                }
                else
                    outEither.AddMessage(new ErrorMessage(file_pos.ValueRange, "Invalid fileformat", "The file should be of .txt, .fasta or .csv type."));

                outEither.Messages.AddRange(folder_reads.Messages);
                if (!folder_reads.HasFailed()) tsettings.Templates = folder_reads.ReturnOrFail();

                return outEither;
            }
            public static ParseResult<bool> GetPeaksSettings(KeyValue setting, bool withprefix, InputData.Peaks peaks_settings)
            {
                var outEither = new ParseResult<bool>(true);
                var name = setting.Name;

                if (withprefix && !name.StartsWith("peaks"))
                {
                    outEither.Value = false;
                    return outEither;
                }

                if (withprefix) name = name.Substring(5);

                switch (name)
                {
                    case "format":
                        switch (setting.GetValue().ToLower())
                        {
                            case "old":
                                peaks_settings.FileFormat = FileFormat.Peaks.OldFormat();
                                break;
                            case "x":
                                peaks_settings.FileFormat = FileFormat.Peaks.PeaksX();
                                break;
                            case "x+":
                                peaks_settings.FileFormat = FileFormat.Peaks.PeaksXPlus();
                                break;
                            default:
                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "PEAKS Format", "'Old', 'X' and 'X+'"));
                                break;
                        }
                        break;
                    case "separator":
                        if (setting.GetValue().Length != 1)
                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                        else
                            peaks_settings.Separator = setting.GetValue().First();
                        break;
                    case "decimalseparator":
                        if (setting.GetValue().Length != 1)
                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                        else
                            peaks_settings.DecimalSeparator = setting.GetValue().First();
                        break;
                    default:
                        var (parameters, success) = GetLocalPeaksParameters(setting, withprefix, peaks_settings.Parameter).GetValue(outEither);
                        peaks_settings.Parameter = parameters;

                        if (success == false)
                            outEither.Value = false;
                        break;
                }

                return outEither;
            }
            public static ParseResult<(InputData.PeaksParameters, bool)> GetLocalPeaksParameters(KeyValue setting, bool withprefix, InputData.PeaksParameters parameters)
            {
                var outEither = new ParseResult<(InputData.PeaksParameters, bool)>();
                outEither.Value = (parameters, true);
                var name = setting.Name;

                if (withprefix && !name.StartsWith("peaks"))
                {
                    outEither.Value = (parameters, false);
                    return outEither;
                }

                if (withprefix) name = name.Substring(5);

                switch (name)
                {
                    case "cutoffalc":
                        parameters.CutoffALC = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    case "localcutoffalc":
                        parameters.LocalCutoffALC = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    case "minlengthpatch":
                        parameters.MinLengthPatch = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    default:
                        outEither.Value = (parameters, false);
                        break;
                }

                return outEither;
            }
            public static ParseResult<bool> ParseBool(KeyValue setting, string context, bool def = false)
            {
                var output = new ParseResult<bool>(def);
                switch (setting.GetValue().ToLower())
                {
                    case "true":
                        output.Value = true;
                        break;
                    case "false":
                        output.Value = false;
                        break;
                    default:
                        output.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, context, "'True' or 'False'"));
                        break;
                }
                return output;
            }
            public static ParseResult<string> GetFullPath(KeyValue setting)
            {
                var outEither = new ParseResult<string>();
                var res = GetFullPathPrivate(setting.GetValue());

                if (string.IsNullOrEmpty(res.Item2))
                {
                    outEither.Value = Path.GetFullPath(res.Item1);
                }
                else
                {
                    outEither.AddMessage(new ErrorMessage(setting.ValueRange, res.Item1, res.Item2));
                }
                return outEither;
            }
            public static ParseResult<string> GetFullPath(string path)
            {
                var outEither = new ParseResult<string>();
                var res = GetFullPathPrivate(path);

                if (string.IsNullOrEmpty(res.Item2))
                {
                    outEither.Value = Path.GetFullPath(res.Item1);
                }
                else
                {
                    outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2));
                }
                return outEither;
            }
            static (string, string) GetFullPathPrivate(string path)
            {
                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    return ("Invalid path", "The path contains invalid characters.");
                }
                else if (string.IsNullOrWhiteSpace(path))
                {
                    return ("Invalid path", "The path is empty.");
                }
                {
                    try
                    {
                        if (path.StartsWith("\"") && path.EndsWith("\""))
                        {
                            path = path.Substring(1, path.Length - 2);
                        }
                        return (Path.GetFullPath(path), "");
                    }
                    catch (ArgumentException)
                    {
                        return ("Invalid path", "The path cannot be found.");
                    }
                    catch (System.Security.SecurityException)
                    {
                        return ("Invalid path", "The file could not be opened because of a lack of required permissions.");
                    }
                    catch (NotSupportedException)
                    {
                        return ("Invalid path", "The path contains a colon ':' not part of a volume identifier.");
                    }
                    catch (PathTooLongException)
                    {
                        return ("Invalid path", "The path length exceeds the system defined width.");
                    }
                    catch (Exception e)
                    {
                        return ("Invalid path", $"Unknown exception occurred when reading path: {e.Message}.");
                    }
                }
            }
            static (string[], string, string, string) GetAllFilesPrivate(string path, bool recursive)
            {
                var trypath = GetFullPathPrivate(path);
                Console.WriteLine("FullPath: " + path + " / " + trypath);

                try
                {
                    var option = SearchOption.TopDirectoryOnly;
                    if (recursive) option = SearchOption.AllDirectories;
                    return (Directory.GetFiles(trypath.Item1, "*", option), "", "", "");
                }
                catch (ArgumentException)
                {
                    return (Array.Empty<string>(), "Invalid path", "The path contains invalid characters.", "");
                }
                catch (UnauthorizedAccessException)
                {
                    return (Array.Empty<string>(), "Invalid path", "The file could not be opened because of a lack of required permissions.", "");
                }
                catch (PathTooLongException)
                {
                    return (Array.Empty<string>(), "Invalid path", "The path length exceeds the system defined width.", "");
                }
                catch (DirectoryNotFoundException)
                {
                    try
                    {
                        var pieces = trypath.Item1.Split(new char[] { '\\', '/' });
                        var drive = pieces[0].Split(':')[0];
                        if (Directory.GetLogicalDrives().Contains($"{drive}:\\"))
                        {
                            string currentpath = $"{drive}:\\";
                            for (int i = 1; i < pieces.Length - 1; i++)
                            {
                                string nextpath = currentpath + pieces[i] + "\\";

                                if (!Directory.Exists(nextpath))
                                {
                                    var directories = Directory.GetDirectories(currentpath);
                                    var extra = "";

                                    if (directories.Length == 0) extra = "\nThere are no subfolders in this folder.";
                                    else if (directories.Length == 1) extra = $"\nThe only subfolder is '{directories[0]}'.";
                                    else
                                    {
                                        int maxvalue = 0;
                                        string maxname = "";
                                        foreach (var dir in directories)
                                        {
                                            int score = HelperFunctionality.SmithWatermanStrings(dir, pieces[i]);
                                            if (score > maxvalue)
                                            {
                                                maxname = Path.GetFileName(dir);
                                                maxvalue = score;
                                            }
                                        }
                                        extra = $"\nDid you mean '{maxname}'?";
                                    }

                                    return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The folder '{pieces[i]}' does not exist in '{pieces[i - 1]}'.{extra}");
                                }
                                currentpath = nextpath;
                            }
                            // Will likely be never used because that would raise a FileNotFoundException
                            return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The file '{pieces[^1]}' does not exist in '{currentpath}'.");
                        }
                        else
                        {
                            return (Array.Empty<string>(), "Could not open file", "The path cannot be found.", $"The drive '{drive}:\\' is not mounted.");
                        }
                    }
                    catch
                    {
                        return (Array.Empty<string>(), "Could not open file", "The path cannot be found, possibly on an unmapped drive.", "");
                    }
                }
                catch (IOException)
                {
                    return (Array.Empty<string>(), "Invalid path", "The path is a file name or a network error has occurred.", "");
                }
                catch (Exception e)
                {
                    return (Array.Empty<string>(), "Invalid path", $"Unknown exception occurred when reading path: {e.Message}.", "");
                }
            }
            public static ParseResult<string> GetAllText(KeyValue setting)
            {
                var outEither = new ParseResult<string>();

                var res = GetAllTextPrivate(setting.GetValue());

                if (string.IsNullOrEmpty(res.Item2)) outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(setting.ValueRange, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            public static ParseResult<string> GetAllText(ReadMetaData.FileIdentifier file)
            {
                if (file.Origin != null) return GetAllText(file.Origin);
                else return GetAllText(file.Path);
            }
            public static ParseResult<string> GetAllText(string path)
            {
                var outEither = new ParseResult<string>();

                var res = GetAllTextPrivate(path);

                if (string.IsNullOrEmpty(res.Item2)) outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            static (string, string, string) GetAllTextPrivate(string path)
            {
                var trypath = GetFullPathPrivate(path);

                if (string.IsNullOrEmpty(trypath.Item2))
                {
                    if (Directory.Exists(trypath.Item1))
                    {
                        return ("Could not open file", "The file given is a directory.", "");
                    }
                    else
                    {
                        try
                        {
                            return (File.ReadAllText(trypath.Item1), "", "");
                        }
                        catch (DirectoryNotFoundException)
                        {
                            try
                            {
                                var pieces = trypath.Item1.Split(new char[] { '\\', '/' });
                                var drive = pieces[0].Split(':')[0];
                                if (Directory.GetLogicalDrives().Contains($"{drive}:\\"))
                                {
                                    string currentpath = $"{drive}:\\";
                                    for (int i = 1; i < pieces.Length - 1; i++)
                                    {
                                        string nextpath = currentpath + pieces[i] + "\\";

                                        if (!Directory.Exists(nextpath))
                                        {
                                            var directories = Directory.GetDirectories(currentpath);
                                            var extra = "";

                                            if (directories.Length == 0) extra = "\nThere are no subfolders in this folder.";
                                            else if (directories.Length == 1) extra = $"\nThe only subfolder is '{directories[0]}'.";
                                            else
                                            {
                                                int maxvalue = 0;
                                                string maxname = "";
                                                foreach (var dir in directories)
                                                {
                                                    int score = HelperFunctionality.SmithWatermanStrings(dir, pieces[i]);
                                                    if (score > maxvalue)
                                                    {
                                                        maxname = Path.GetFileName(dir);
                                                        maxvalue = score;
                                                    }
                                                }
                                                extra = $"\nDid you mean '{maxname}'?";
                                            }

                                            return ("Could not open file", "The path cannot be found.", $"The folder '{pieces[i]}' does not exist in '{pieces[i - 1]}'.{extra}");
                                        }
                                        currentpath = nextpath;
                                    }
                                    // Will likely be never used because that would raise a FileNotFoundException
                                    return ("Could not open file", "The path cannot be found.", $"The file '{pieces[^1]}' does not exist in '{currentpath}'.");
                                }
                                else
                                {
                                    return ("Could not open file", "The path cannot be found.", $"The drive '{drive}:\\' is not mounted.");
                                }
                            }
                            catch
                            {
                                return ("Could not open file", "The path cannot be found, possibly on an unmapped drive.", "");
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            int maxvalue = 0;
                            string maxname = "";
                            string name = Path.GetFileName(trypath.Item1);

                            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(trypath.Item1)))
                            {
                                int score = HelperFunctionality.SmithWatermanStrings(file, name);
                                if (score > maxvalue)
                                {
                                    maxname = Path.GetFileName(file);
                                    maxvalue = score;
                                }
                            }

                            return ("Could not open file", "The specified file could not be found.", $"Did you mean '{maxname}'?");
                        }
                        catch (IOException)
                        {
                            return ("Could not open file", "An IO error occurred while opening the file.", "Make sure it is not opened in another program, like Excel.");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return ("Could not open file", "Unauthorised access.", "Make sure you have the right permissions to open this file.");
                        }
                        catch (System.Security.SecurityException)
                        {
                            return ("Could not open file", "The caller does not have the required permission.", "Make sure you have the right permissions to open this file.");
                        }
                    }
                }
                else
                {
                    return (trypath.Item1, trypath.Item2, "");
                }
            }
        }
    }
}