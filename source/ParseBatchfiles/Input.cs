using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AssemblyNameSpace.InputNameSpace;
using System.Text;
using System.Text.RegularExpressions;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A class with options to parse a batch file.
    /// </summary>
    public static class ParseCommandFile
    {
        /// <summary>
        /// Parses a batch file and retrieves the runparameters.
        /// </summary>
        /// <param name="path">The path to the batch file.</param>
        /// <returns>The runparameters as specified in the file.</returns>
        public static RunParameters.FullRunParameters Batch(string path)
        {
            var output = new RunParameters.FullRunParameters();
            var outEither = new ParseEither<RunParameters.FullRunParameters>(output);

            // Get the contents
            string batchfilecontent = ParseHelper.GetAllText(path).ReturnOrFail().Replace("\t", "    "); // Remove tabs

            // Set the working directory to the directory of the batchfile
            var original_working_directory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(path));

            // Save the batchfile for use in the construction of error messages
            var batchfile = new ParsedFile(path, batchfilecontent.Split('\n'));

            // Tokenize the file, into a key value pair tree
            var parsed = InputNameSpace.Tokenizer.Tokenize(batchfile);

            // Now all key value pairs are saved in 'parsed'
            // Now parse the key value pairs into RunParameters

            bool versionspecified = false;

            // Match every possible key to the corresponding action
            foreach (var pair in parsed)
            {
                switch (pair.Name)
                {
                    case "runname":
                        output.Runname = pair.GetValue();
                        break;
                    case "version":
                        if (pair.GetValue() != "0")
                        {
                            outEither.AddMessage(new ErrorMessage(pair.ValueRange, "Specified Version does not exist"));
                        }
                        versionspecified = true;
                        break;
                    case "maxcores":
                        output.MaxNumberOfCPUCores = ParseHelper.ConvertToInt(pair.GetValue(), pair.ValueRange).GetValue(outEither);
                        break;
                    case "runtype":
                        switch (pair.GetValue().ToLower())
                        {
                            case "separate":
                                output.Runtype = RunParameters.RuntypeValue.Separate;
                                break;
                            case "group":
                                output.Runtype = RunParameters.RuntypeValue.Group;
                                break;
                            default:
                                outEither.AddMessage(new ErrorMessage(pair.KeyRange.Name, "Unknown option", "Unknown option in Runtype definition", "Valid options are: 'Group' and 'Separate'."));
                                break;
                        }
                        break;
                    case "peaks":
                        var settings = new RunParameters.Input.Peaks();

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

                        var reads_peaks = OpenReads.Peaks(settings.File, settings.Cutoffscore, settings.LocalCutoffscore, settings.FileFormat, settings.MinLengthPatch, settings.Separator, settings.DecimalSeparator);
                        outEither.Messages.AddRange(reads_peaks.Messages);
                        if (!reads_peaks.HasFailed()) output.DataParameters.Add(reads_peaks.ReturnOrFail());
                        break;

                    case "reads":
                        var rsettings = new RunParameters.Input.Reads();

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

                        var reads_simple = OpenReads.Simple(rsettings.File);
                        outEither.Messages.AddRange(reads_simple.Messages);
                        if (!reads_simple.HasFailed()) output.DataParameters.Add(reads_simple.ReturnOrFail());
                        break;

                    case "fastainput":
                        var fastasettings = new RunParameters.Input.FASTA();

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

                        var reads_fasta = OpenReads.Fasta(fastasettings.File, fastasettings.Identifier);
                        outEither.Messages.AddRange(reads_fasta.Messages);
                        if (!reads_fasta.HasFailed()) output.DataParameters.Add(reads_fasta.ReturnOrFail());
                        break;
                    case "folder":
                        // Parse files one by one
                        var folder_path = "";
                        var startswith = "";
                        var identifier = new Regex(".*");

                        var peaks_settings = new RunParameters.Input.Peaks();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    if (!string.IsNullOrWhiteSpace(folder_path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    folder_path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                    break;
                                case "startswith":
                                    if (!string.IsNullOrWhiteSpace(startswith)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    startswith = setting.GetValue();
                                    break;
                                case "identifier":
                                    identifier = ParseHelper.ParseRegex(setting).GetValue(outEither);
                                    break;
                                default:
                                    var peaks = ParseHelper.GetPeaksSettings(setting, true, peaks_settings);
                                    outEither.Messages.AddRange(peaks.Messages);

                                    if (peaks.Value == false)
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Folder", "'Path' and 'StartsWith'"));

                                    break;
                            }
                        }

                        foreach (var file in Directory.GetFiles(folder_path))
                        {
                            if (!Path.GetFileName(file).StartsWith(startswith)) continue;

                            var fileId = new MetaData.FileIdentifier() { Name = Path.GetFileNameWithoutExtension(file), Path = ParseHelper.GetFullPath(file).GetValue(outEither) };

                            ParseEither<List<(string, MetaData.IMetaData)>> folder_reads;

                            if (file.EndsWith(".fasta"))
                                folder_reads = OpenReads.Fasta(fileId, identifier);
                            else if (file.EndsWith(".txt"))
                                folder_reads = OpenReads.Simple(fileId);
                            else if (file.EndsWith(".csv"))
                                folder_reads = OpenReads.Peaks(fileId, peaks_settings.Cutoffscore, peaks_settings.LocalCutoffscore, peaks_settings.FileFormat, peaks_settings.MinLengthPatch, peaks_settings.Separator, peaks_settings.DecimalSeparator);
                            else
                                continue;

                            outEither.Messages.AddRange(folder_reads.Messages);
                            if (!folder_reads.HasFailed()) output.DataParameters.Add(folder_reads.ReturnOrFail());
                        }

                        break;
                    case "k":
                        if (!pair.IsSingle())
                        {
                            var ksettings = new RunParameters.K.Range();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "start":
                                        if (ksettings.Start != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        ksettings.Start = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    case "end":
                                        if (ksettings.End != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                        ksettings.End = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    case "step":
                                        ksettings.Step = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "K", "'Start', 'End' and 'Step'"));
                                        break;
                                }
                            }
                            if (ksettings.Start > 0 && ksettings.End > 0)
                            {
                                output.K = ksettings;
                            }
                            else
                            {
                                outEither.AddMessage(new ErrorMessage(pair.KeyRange.Full, "Invalid range", "A range should be set with a start and end value"));
                            }
                            break;
                        }
                        else
                        {
                            var kvalue = ParseHelper.ConvertToInt(pair.GetValue(), pair.ValueRange);
                            if (!kvalue.HasFailed())
                            {
                                // Single K value
                                output.K = new RunParameters.K.Single(kvalue.GetValue(outEither));
                            }
                            else
                            {
                                // Multiple K values
                                var values = new List<int>();
                                foreach (string value in pair.GetValue().Split(",".ToCharArray()))
                                {
                                    values.Add(ParseHelper.ConvertToInt(value, pair.ValueRange).GetValue(outEither));
                                }
                                output.K = new RunParameters.K.Multiple(values.ToArray());
                            }
                        }
                        break;
                    case "duplicatethreshold":
                        output.DuplicateThreshold.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse(pair.GetValue(), pair.ValueRange, batchfile).GetValue(outEither)));
                        break;
                    case "minimalhomology":
                        output.MinimalHomology.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse(pair.GetValue(), pair.ValueRange, batchfile).GetValue(outEither)));
                        break;
                    case "reverse":
                        switch (pair.GetValue().ToLower())
                        {
                            case "true":
                                output.Reverse = RunParameters.ReverseValue.True;
                                break;
                            case "false":
                                output.Reverse = RunParameters.ReverseValue.False;
                                break;
                            case "both":
                                output.Reverse = RunParameters.ReverseValue.Both;
                                break;
                            default:
                                outEither.AddMessage(ErrorMessage.UnknownKey(pair.KeyRange.Name, "Reverse", "'True', 'False' and 'Both'"));
                                break;
                        }
                        break;
                    case "alphabet":
                        output.Alphabet.Add(ParseHelper.ParseAlphabet(pair).GetValue(outEither));
                        break;
                    case "database":
                        output.Template.Add(ParseHelper.ParseDatabase(pair, true).GetValue(outEither));
                        break;
                    case "recombine":
                        if (output.Recombine != null) outEither.AddMessage(new ErrorMessage(pair.KeyRange.Start, "Multiple definitions", "Cannot have multiple definitions of Recombine"));

                        var recsettings = new RunParameters.RecombineValue();
                        KeyValue order = null;
                        var database_names = new List<string>();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "n":
                                    recsettings.N = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "order":
                                    if (order != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    order = setting;
                                    break;
                                case "cutoffscore":
                                    recsettings.CutoffScore = ParseHelper.ConvertToDouble(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "databases":
                                    if (database_names.Count() != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    foreach (var database in setting.GetValues())
                                    {
                                        if (database.Name == "database")
                                        {
                                            var databasevalue = ParseHelper.ParseDatabase(database, false).GetValue(outEither);
                                            recsettings.Databases.Add(databasevalue);

                                            // CHeck to see if the name is valid
                                            if (database_names.Contains(databasevalue.Name))
                                            {
                                                outEither.AddMessage(new ErrorMessage(database.KeyRange.Full, "Invalid name", "Database names have to be unique."));
                                            }
                                            if (databasevalue.Name.Contains('*'))
                                            {
                                                outEither.AddMessage(new ErrorMessage(database.KeyRange.Full, "Invalid name", "Database names cannot contain '*'."));
                                            }
                                            database_names.Add(databasevalue.Name);
                                        }
                                        else
                                        {
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Databases", "'Database'"));
                                        }
                                    }
                                    break;
                                case "alphabet":
                                    if (recsettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    recsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                                    break;
                                case "includeshortreads":
                                    switch (setting.GetValue().ToLower())
                                    {
                                        case "true":
                                            recsettings.IncludeShortReads = true;
                                            break;
                                        case "false":
                                            recsettings.IncludeShortReads = false;
                                            break;
                                        default:
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "IncludeShortReads", "'True' or 'False'"));
                                            break;
                                    }

                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Recombine", "'N', 'Order', 'Databases' and 'Alphabet'"));
                                    break;
                            }
                        }

                        if (recsettings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Alphabet"));
                        if (database_names.Count() == 0) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Databases"));

                        // Parse the order
                        if (order != null)
                        {
                            var order_string = order.GetValue();
                            // Create a new counter
                            var order_counter = new InputNameSpace.Tokenizer.Counter(order.ValueRange.Start);

                            while (order_string != "")
                            {
                                InputNameSpace.Tokenizer.ParseHelper.Trim(ref order_string, order_counter);

                                var match = false;
                                for (int i = 0; i < recsettings.Databases.Count(); i++)
                                {
                                    var template = recsettings.Databases[i];
                                    if (order_string.StartsWith(template.Name))
                                    {
                                        order_string = order_string.Remove(0, template.Name.Length);
                                        order_counter.NextColumn(template.Name.Length);
                                        recsettings.Order.Add(new RunParameters.RecombineOrder.Template(i));
                                        match = true;
                                        break;
                                    }
                                }
                                if (match) continue;

                                if (order_string.StartsWith('*'))
                                {
                                    order_string = order_string.Remove(0, 1);
                                    order_counter.NextColumn();
                                    recsettings.Order.Add(new RunParameters.RecombineOrder.Gap());
                                }
                                else
                                {
                                    outEither.AddMessage(new ErrorMessage(new FileRange(order_counter.GetPosition(), order.ValueRange.End), "Invalid order", "Valid options are a name of a template, a gap ('*') or whitespace."));
                                    break;
                                }
                            }
                        }
                        else
                        {
                            outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Order"));
                        }

                        output.Recombine = recsettings;
                        break;
                    case "html":
                        var hsettings = new RunParameters.Report.HTML();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    if (!string.IsNullOrWhiteSpace(hsettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    hsettings.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "dotdistribution":
                                    if (setting.GetValue().ToLower() == "global")
                                    {
                                        hsettings.UseIncludedDotDistribution = false;
                                    }
                                    else if (setting.GetValue().ToLower() == "included")
                                    {
                                        hsettings.UseIncludedDotDistribution = true;
                                    }
                                    else
                                    {
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "HTML DotDistribution", "'Global' and 'Included'"));
                                    }
                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "HTML", "'Path' and 'DotDistribution'"));
                                    break;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(hsettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                        output.Report.Add(hsettings);
                        break;
                    case "csv":
                        var csettings = new RunParameters.Report.CSV();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    if (!string.IsNullOrWhiteSpace(csettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    csettings.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "CSV", "'Path'"));
                                    break;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(csettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                        output.Report.Add(csettings);
                        break;
                    case "fasta":
                        var fsettings = new RunParameters.Report.FASTA();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    if (!string.IsNullOrWhiteSpace(fsettings.Path)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    fsettings.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "minimalscore":
                                    fsettings.MinimalScore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "outputtype":
                                    switch (setting.GetValue().ToLower())
                                    {
                                        case "paths":
                                            fsettings.OutputType = RunParameters.Report.FastaOutputType.Paths;
                                            break;
                                        case "consensussequence":
                                            fsettings.OutputType = RunParameters.Report.FastaOutputType.ConsensusSequence;
                                            break;
                                        default:
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "FASTA OutputType", "'Paths' and 'ConsensusSequence'"));
                                            break;
                                    }
                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "FASTA", "'Path', 'MinimalScore' and 'OutputType'"));
                                    break;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(fsettings.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                        output.Report.Add(fsettings);
                        break;
                    default:
                        outEither.AddMessage(ErrorMessage.UnknownKey(pair.KeyRange.Name, "", ""));
                        break;
                }
            }

            Position def_position = new Position(0, 1, new ParsedFile());
            FileRange def_range = new FileRange(def_position, def_position);

            // Generate defaults
            if (output.DuplicateThreshold.Count() == 0)
            {
                output.DuplicateThreshold.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse("K-1", def_range, new ParsedFile()).GetValue(outEither)));
            }
            if (output.MinimalHomology.Count() == 0)
            {
                output.MinimalHomology.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse("K-1", def_range, new ParsedFile()).GetValue(outEither)));
            }

            // Detect missing parameters
            if (string.IsNullOrWhiteSpace(output.Runname)) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Runname"));
            if (output.Report.Count() == 0) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Any report parameter"));
            if (output.DataParameters.Count() == 0) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Any input parameter"));
            if (output.Alphabet.Count() == 0) outEither.AddMessage(ErrorMessage.MissingParameter(def_range, "Alphabet"));

            // Check if there is a version specified
            if (!versionspecified)
            {
                outEither.AddMessage(new ErrorMessage(new Position(0, 1, new ParsedFile()), "No version specified", "There is no version specified for the batch file; This is needed to handle different versions in different ways."));
            }

            // Reset the working directory
            Directory.SetCurrentDirectory(original_working_directory);
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
    public class ParseEither<T>
    {
        public T Value;
        public List<ErrorMessage> Messages = new List<ErrorMessage>();
        public ParseEither(T t)
        {
            Value = t;
        }
        public ParseEither(ErrorMessage error)
        {
            Messages.Add(error);
        }
        public ParseEither(List<ErrorMessage> errors)
        {
            Messages.AddRange(errors);
        }
        public ParseEither() { }
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
            if (Messages.Count() == 0) return false;
            foreach (var msg in Messages)
            {
                if (!msg.Warning) return false;
            }
            return true;
        }
        public ParseEither<Tout> Do<Tout>(Func<T, Tout> func, ErrorMessage failMessage = null)
        {
            if (!this.HasFailed())
            {
                return new ParseEither<Tout>(func(this.Value));
            }
            else
            {
                if (failMessage != null) this.Messages.Add(failMessage);
                return new ParseEither<Tout>(Messages);
            }
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
        public T GetValue<Tout>(ParseEither<Tout> fail)
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
                Console.WriteLine($"There were {Messages.Count()} error(s) while parsing.\n");
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
            /// Converts a string to an int, while it generates meaningfull error messages for the end user.
            /// </summary>
            /// <param name="input">The string to be converted to an int.</param>
            /// <returns>If successfull: the number (int32)</returns>
            public static ParseEither<int> ConvertToInt(string input, FileRange pos)
            {
                try
                {
                    return new ParseEither<int>(Convert.ToInt32(input));
                }
                catch (FormatException)
                {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseEither<int>(new ErrorMessage(pos, "Not a valid number", msg));
                }
                catch (OverflowException)
                {
                    return new ParseEither<int>(new ErrorMessage(pos, "Outside bounds"));
                }
                catch
                {
                    return new ParseEither<int>(new ErrorMessage(pos, "Unknown exception", "This is not a valid number and an unkown exception occurred."));
                }
            }
            /// <summary>
            /// Converts a string to a double, while it generates meaningfull error messages for the end user.
            /// </summary>
            /// <param name="input">The string to be converted to a double.</param>
            /// <returns>If successfull: the number (double)</returns>
            public static ParseEither<double> ConvertToDouble(string input, FileRange pos)
            {
                try
                {
                    return new ParseEither<double>(Convert.ToDouble(input, new System.Globalization.CultureInfo("en-US")));
                }
                catch (FormatException)
                {
                    string msg = "";
                    if (input.IndexOfAny("iIloO".ToCharArray()) != -1) msg = "It contains characters which visually resemble digits.";
                    return new ParseEither<double>(new ErrorMessage(pos, "Not a valid number", msg));
                }
                catch (OverflowException)
                {
                    return new ParseEither<double>(new ErrorMessage(pos, "Outside bounds"));
                }
                catch
                {
                    return new ParseEither<double>(new ErrorMessage(pos, "Unknown exception", "This is not a valid number and an unkown exception occurred."));
                }
            }
            public static ParseEither<RunParameters.AlphabetValue> ParseAlphabet(KeyValue key)
            {
                var asettings = new RunParameters.AlphabetValue();
                var outEither = new ParseEither<RunParameters.AlphabetValue>(asettings);

                if (key.GetValues().Count() == 0)
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
                            //if (data != "") outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            //data = setting.GetValue();
                            if (asettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            var data_content = setting.GetValue().Split("\n");
                            var data_counter = new Tokenizer.Counter(setting.ValueRange.Start);
                            result = ParseAlphabetData(data_content, data_counter).GetValue(outEither);
                            asettings.Alphabet = result.Item1;
                            asettings.ScoringMatrix = result.Item2;
                            break;
                        case "name":
                            if (asettings.Name != "") outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
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
            public static ParseEither<(char[], int[,])> ParseAlphabetData(string[] lines, Tokenizer.Counter counter)
            {
                var outEither = new ParseEither<(char[], int[,])>();

                int rows = lines.Length;
                var cells = new List<(Position, List<(string, FileRange)>)>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var startline = counter.GetPosition();
                    var splitline = new List<(string, FileRange)>();
                    var line = lines[i];
                    Tokenizer.ParseHelper.Trim(ref line, counter);

                    while (line != "")
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

                int columns = cells[0].Item2.Count();

                for (int line = 0; line < rows; line++)
                {
                    if (rows > cells[line].Item2.Count())
                    {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {rows - cells[line].Item2.Count()} column(s) missing on this row."));
                    }
                    else if (rows < cells[line].Item2.Count())
                    {
                        outEither.AddMessage(new ErrorMessage(cells[line].Item1, "Invalid amount of columns", $"There are {cells[line].Item2.Count() - rows} additional column(s) on this row."));
                    }
                }

                var alphabetBuilder = new StringBuilder();
                foreach (var element in cells[0].Item2.Skip(1))
                {
                    alphabetBuilder.Append(element.Item1);
                }
                var alphabet = alphabetBuilder.ToString().ToCharArray();

                if (!alphabet.Contains('*')) // TODO: use the right variables
                {
                    outEither.AddMessage(new ErrorMessage(counter.File, "GapChar missing", "The Gap '*' is missing in the alpabet definition.", "", true));
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
            public static ParseEither<Regex> ParseRegex(KeyValue node)
            {
                var outEither = new ParseEither<Regex>();
                try
                {
                    outEither.Value = new Regex(node.GetValue().Trim());

                    if (outEither.Value.GetGroupNumbers().Length <= 1)
                    {
                        outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx is invalid", "The given RegEx has no caputuring groups.", "To parse an identifier from the fasta header a capturing group (enclosed in parentheses '()') should be present enclosing the identifier. Example: '\\s*(\\w*)'"));
                    }
                    else if (outEither.Value.GetGroupNumbers().Length > 2)
                    {
                        outEither.AddMessage(new ErrorMessage(node.ValueRange, "RegEx could be wrong", "The given RegEx has a lot of capturing groups, only the first one will be used.", "", true));
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
            public static ParseEither<RunParameters.DatabaseValue> ParseDatabase(KeyValue node, bool extended)
            {
                // Parse files one by one
                var file_path = "";
                FileRange file_pos = node.ValueRange;

                var peaks_settings = new RunParameters.Input.Peaks();

                var tsettings = new RunParameters.DatabaseValue();
                var outEither = new ParseEither<RunParameters.DatabaseValue>(tsettings);

                foreach (var setting in node.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            if (file_path != "") outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            file_path = GetFullPath(setting).GetValue(outEither);
                            file_pos = setting.ValueRange;
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
                        case "includeshortreads":
                            if (!extended)
                                outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "IncludeShortReads cannot be defined here", "Inside a template in the templates list of a recombination IncludeShortReads can not be defined."));
                            else
                            {
                                switch (setting.GetValue().ToLower())
                                {
                                    case "true":
                                        tsettings.IncludeShortReads = true;
                                        break;
                                    case "false":
                                        tsettings.IncludeShortReads = false;
                                        break;
                                    default:
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "IncludeShortReads", "'True' or 'False'"));
                                        break;
                                }
                            }

                            break;
                        case "scoring":
                            var scoring = setting.GetValue().ToLower();
                            if (scoring == "absolute")
                            {
                                tsettings.Scoring = RunParameters.ScoringParameter.Absolute;
                            }
                            else if (scoring == "relative")
                            {
                                tsettings.Scoring = RunParameters.ScoringParameter.Relative;
                            }
                            else
                            {
                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.ValueRange, "Scoring", "'Absolute' or 'Relative'"));
                            }
                            break;

                        default:
                            var peaks = GetPeaksSettings(setting, true, peaks_settings);
                            outEither.Messages.AddRange(peaks.Messages);

                            if (peaks.Value == false)
                            {
                                var options = "'Path', 'Type', 'Name', 'Alphabet', 'IncludeShortReads', 'Scoring' and all PEAKS format parameters";
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
                var fileId = new MetaData.FileIdentifier() { Name = tsettings.Name, Path = ParseHelper.GetFullPath(file_path).GetValue(outEither) };

                ParseEither<List<(string, MetaData.IMetaData)>> folder_reads = new ParseEither<List<(string, MetaData.IMetaData)>>();

                if (file_path.EndsWith(".fasta"))
                    folder_reads = OpenReads.Fasta(fileId, tsettings.Identifier);
                else if (file_path.EndsWith(".txt"))
                    folder_reads = OpenReads.Simple(fileId);
                else if (file_path.EndsWith(".csv"))
                    folder_reads = OpenReads.Peaks(fileId, peaks_settings.Cutoffscore, peaks_settings.LocalCutoffscore, peaks_settings.FileFormat, peaks_settings.MinLengthPatch, peaks_settings.Separator, peaks_settings.DecimalSeparator);
                else
                    outEither.AddMessage(new ErrorMessage(file_pos, "Invalid fileformat", "The file should be of .txt, .fasta or .csv type."));

                outEither.Messages.AddRange(folder_reads.Messages);
                if (!folder_reads.HasFailed()) tsettings.Templates = folder_reads.ReturnOrFail();

                return outEither;
            }
            public static ParseEither<bool> GetPeaksSettings(KeyValue setting, bool withprefix, RunParameters.Input.Peaks peaks_settings)
            {
                var outEither = new ParseEither<bool>(true);
                var name = setting.Name;

                if (withprefix && !name.StartsWith("peaks"))
                {
                    outEither.Value = false;
                    return outEither;
                }

                if (withprefix) name = name.Substring(5);

                switch (name)
                {
                    case "cutoffscore":
                        peaks_settings.Cutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    case "localcutoffscore":
                        peaks_settings.LocalCutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    case "minlengthpatch":
                        peaks_settings.MinLengthPatch = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                        break;
                    case "separator":
                        if (setting.GetValue().Length != 1)
                        {
                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                        }
                        else
                        {
                            peaks_settings.Separator = setting.GetValue().First();
                        }
                        break;
                    case "decimalseparator":
                        if (setting.GetValue().Length != 1)
                        {
                            outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                        }
                        else
                        {
                            peaks_settings.DecimalSeparator = setting.GetValue().First();
                        }
                        break;
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
                    default:
                        outEither.Value = false;
                        break;
                }

                return outEither;
            }
            public static ParseEither<string> GetFullPath(KeyValue setting)
            {
                var outEither = new ParseEither<string>();
                var res = GetFullPathPrivate(setting.GetValue());

                if (res.Item2 == "")
                {
                    outEither.Value = Path.GetFullPath(res.Item1);
                }
                else
                {
                    outEither.AddMessage(new ErrorMessage(setting.ValueRange, res.Item1, res.Item2));
                }
                return outEither;
            }
            public static ParseEither<string> GetFullPath(string path)
            {
                var outEither = new ParseEither<string>();
                var res = GetFullPathPrivate(path);

                if (res.Item2 == "")
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
            public static ParseEither<string> GetAllText(KeyValue setting)
            {
                var outEither = new ParseEither<string>();

                var res = GetAllTextPrivate(setting.GetValue());

                if (res.Item2 == "") outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(setting.ValueRange, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            public static ParseEither<string> GetAllText(string path)
            {
                var outEither = new ParseEither<string>();

                var res = GetAllTextPrivate(path);

                if (res.Item2 == "") outEither.Value = res.Item1;
                else outEither.AddMessage(new ErrorMessage(path, res.Item1, res.Item2, res.Item3));

                return outEither;
            }
            static (string, string, string) GetAllTextPrivate(string path)
            {
                var trypath = GetFullPathPrivate(path);

                if (trypath.Item2 == "")
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

                                            if (directories.Count() == 0) extra = "\nThere are no subfolders in this folder.";
                                            else if (directories.Count() == 1) extra = $"\nThe only subfolder is '{directories[0]}'.";
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
                                    return ("Could not open file", "The path cannot be found.", $"The file '{pieces[pieces.Length - 1]}' does not exist in '{currentpath}'.");
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
                            return ("Could not open file", "An IO error occurred while opening the file.", "Make sure it is not opened in another program.");
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