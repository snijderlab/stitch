using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AssemblyNameSpace.InputNameSpace;

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
                                case "cutoffscore":
                                    settings.Cutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "localcutoffscore":
                                    settings.LocalCutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "minlengthpatch":
                                    settings.MinLengthPatch = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                case "name":
                                    if (!string.IsNullOrWhiteSpace(settings.File.Name)) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    settings.File.Name = setting.GetValue();
                                    break;
                                case "separator":
                                    if (setting.GetValue().Length != 1) {
                                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                                    } else {
                                        settings.Separator = setting.GetValue().First();
                                    }
                                    break;
                                case "decimalseparator":
                                    if (setting.GetValue().Length != 1) {
                                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid Character", "The Character should be of length 1"));
                                    } else {
                                        settings.DecimalSeparator = setting.GetValue().First();
                                    }
                                    break;
                                case "format":
                                    if (setting.GetValue().ToLower() == "old")
                                    {
                                        settings.FileFormat = FileFormat.Peaks.OldFormat();
                                    }
                                    else if (setting.GetValue().ToLower() == "new")
                                    {
                                        settings.FileFormat = FileFormat.Peaks.NewFormat();
                                    }
                                    else
                                    {
                                        outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "PEAKS Format", "'Old' and 'New'"));
                                    }
                                    break;
                                default:
                                outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "PEAKS", "'Path', 'CutoffScore', 'LocalCutoffscore', 'MinLengthPatch', 'Name', 'Separator', 'DecimalSeparator' and 'Format'"));
                                    break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(settings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                        if (string.IsNullOrWhiteSpace(settings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                        output.DataParameters.Add(settings);
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

                        output.DataParameters.Add(rsettings);
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
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "FASTAInput", "'Path' and 'Name'"));
                                    break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(fastasettings.File.Path)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Path"));
                        if (string.IsNullOrWhiteSpace(fastasettings.File.Name)) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Name"));

                        output.DataParameters.Add(fastasettings);
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
                    case "template":
                        output.Template.Add(ParseHelper.ParseTemplate(pair, true).GetValue(outEither));
                        break;
                    case "recombine":
                        if (output.Recombine != null) outEither.AddMessage(new ErrorMessage(pair.KeyRange.Start, "Multiple definitions", "Cannot have multiple definitions of Recombine"));

                        var recsettings = new RunParameters.RecombineValue();
                        KeyValue order = null;
                        var template_names = new List<string>();

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
                                case "templates":
                                    if (template_names.Count() != 0) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    foreach (var template in setting.GetValues())
                                    {
                                        if (template.Name == "template")
                                        {
                                            var templatevalue = ParseHelper.ParseTemplate(template, false).GetValue(outEither);
                                            recsettings.Templates.Add(templatevalue);

                                            // CHeck to see if the name is valid
                                            if (template_names.Contains(templatevalue.Name))
                                            {
                                                outEither.AddMessage(new ErrorMessage(template.KeyRange.Full, "Invalid name", "Template names have to be unique."));
                                            }
                                            if (templatevalue.Name.Contains('*'))
                                            {
                                                outEither.AddMessage(new ErrorMessage(template.KeyRange.Full, "Invalid name", "Template names cannot contain '*'."));
                                            }
                                            template_names.Add(templatevalue.Name);
                                        }
                                        else
                                        {
                                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Templates", "'Template'"));
                                        }
                                    }
                                    break;
                                case "alphabet":
                                    if (recsettings.Alphabet != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                                    recsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Recombine", "'N', 'Order', 'Templates' and 'Alphabet'"));
                                    break;
                            }
                        }

                        if (recsettings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Alphabet"));
                        if (template_names.Count() == 0) outEither.AddMessage(ErrorMessage.MissingParameter(pair.KeyRange.Full, "Templates"));

                        // Parse the order
                        if (order != null)
                        {
                            var order_string = order.GetValue();
                            // Create a new counter
                            var order_counter = new InputNameSpace.Tokenizer.Counter();
                            order_counter.Line = order.ValueRange.Start.Line;
                            order_counter.Column = order.ValueRange.Start.Column;

                            while (order_string != "")
                            {
                                InputNameSpace.Tokenizer.ParseHelper.Trim(ref order_string, order_counter);

                                var match = false;
                                for (int i = 0; i < recsettings.Templates.Count(); i++)
                                {
                                    var template = recsettings.Templates[i];
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
                                    outEither.AddMessage(new ErrorMessage(new Range(order_counter.GetPosition(), order.ValueRange.End), "Invalid order", "Valid options are a name of a template, a gap ('*') or whitespace."));
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
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "FASTA", "'Path' and 'MinimalScore'"));
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
            Range def_range = new Range(def_position, def_position);

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
        public T Value = default(T);
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
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"There were {Messages.Count()} error(s) while parsing.\n");
                Console.ForegroundColor = defaultColour;

                foreach (var msg in Messages)
                {
                    msg.Print();
                }

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
        public T GetValue<Tout>(ParseEither<Tout> fail)
        {
            fail.Messages.AddRange(Messages);
            return Value;
        }
        public void AddMessage(ErrorMessage failMessage)
        {
            Messages.Add(failMessage);
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
            public static ParseEither<int> ConvertToInt(string input, Range pos)
            {
                try
                {
                    return new ParseEither<int>(Convert.ToInt32(input));
                }
                catch (FormatException)
                {

                    return new ParseEither<int>(new ErrorMessage(pos, "Not a valid number"));
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
            public static ParseEither<double> ConvertToDouble(string input, Range pos)
            {
                try
                {
                    return new ParseEither<double>(Convert.ToDouble(input, new System.Globalization.CultureInfo("en-US")));
                }
                catch (FormatException)
                {

                    return new ParseEither<double>(new ErrorMessage(pos, "Not a valid number"));
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

                foreach (var setting in key.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            if (asettings.Data != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            asettings.Data = GetAllText(setting).GetValue(outEither);
                            break;
                        case "data":
                            if (asettings.Data != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            asettings.Data = setting.GetValue();
                            break;
                        case "name":
                            if (asettings.Name != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
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

                if (asettings.Name == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Name"));
                if (asettings.Data == null) outEither.AddMessage(ErrorMessage.MissingParameter(key.KeyRange.Full, "Data or Path"));

                return outEither;
            }
            /// <summary>
            /// Parses a Template
            /// </summary>
            /// <param name="node">The KeyValue to parse</param>
            /// <param name="extended">To determine if it is an extended (free standing) template or a template in a recombination definition</param>
            public static ParseEither<RunParameters.TemplateValue> ParseTemplate(KeyValue node, bool extended)
            {
                var tsettings = new RunParameters.TemplateValue();
                var outEither = new ParseEither<RunParameters.TemplateValue>(tsettings);

                foreach (var setting in node.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            if (tsettings.Path != null) outEither.AddMessage(ErrorMessage.DuplicateValue(setting.KeyRange.Name));
                            tsettings.Path = GetFullPath(setting).GetValue(outEither);
                            break;
                        case "type":
                            switch (setting.GetValue().ToLower())
                            {
                                case "reads":
                                    tsettings.Type = RunParameters.InputType.Reads;
                                    break;
                                case "fasta":
                                    tsettings.Type = RunParameters.InputType.Fasta;
                                    break;
                                default:
                                    outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Template InputType", "'Reads' and 'Fasta'"));
                                    break;
                            }
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
                                outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Alphabet cannot be defined here", "Inside a template in the templates list of a recombination an alphabet should not be defined."));
                            }
                            else
                            {
                                tsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            }
                            break;
                        default:
                            var options = "'Path', 'Type', 'Name' and 'Alphabet'";
                            if (!extended) options = "'Path', 'Type' and 'Name'";
                            outEither.AddMessage(ErrorMessage.UnknownKey(setting.KeyRange.Name, "Template", options));
                            break;
                    }
                }

                if (tsettings.Name == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Name"));
                if (tsettings.Path == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Data or Path"));
                if (extended && tsettings.Alphabet == null) outEither.AddMessage(ErrorMessage.MissingParameter(node.KeyRange.Full, "Alphabet"));

                // Try to detect the type of the file
                if (tsettings.Type == RunParameters.InputType.Detect)
                {
                    if (tsettings.Path.EndsWith("fasta"))
                    {
                        tsettings.Type = RunParameters.InputType.Fasta;
                    }
                    else
                    {
                        tsettings.Type = RunParameters.InputType.Reads;
                    }
                }

                return outEither;
            }
            public static ParseEither<string> GetFullPath(KeyValue setting)
            {
                var outEither = new ParseEither<string>();
                string path = setting.GetValue();

                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid path", "The path contains invalid characters."));
                }
                else if (string.IsNullOrWhiteSpace(path))
                {
                    outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid path", "The path is empty."));
                }
                {
                    try
                    {
                        outEither.Value = Path.GetFullPath(setting.GetValue());
                    }
                    catch (ArgumentException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid path", "The path cannot be found."));
                    }
                    catch (System.Security.SecurityException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Invalid path", "The file could not be opened because of a lack of required permissions."));
                    }
                    catch (NotSupportedException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Full, "Invalid path", "The path contains a colon ':' not part of a volume identifier."));
                    }
                    catch (PathTooLongException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Full, "Invalid path", "The path length exceeds the system defined width."));
                    }
                    catch (Exception e)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Full, "Invalid path", $"Unknown exception occurred when reading path: {e.Message}."));
                    }
                }
                return outEither;
            }
            public static ParseEither<string> GetFullPath(string path)
            {
                var outEither = new ParseEither<string>();

                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The path contains invalid characters."));
                }
                else if (string.IsNullOrWhiteSpace(path))
                {
                    outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The path is empty."));
                }
                {
                    try
                    {
                        outEither.Value = Path.GetFullPath(path);
                    }
                    catch (ArgumentException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The path cannot be found."));
                    }
                    catch (System.Security.SecurityException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The file could not be opened because of a lack of required permissions."));
                    }
                    catch (NotSupportedException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The path contains a colon ':' not part of a volume identifier."));
                    }
                    catch (PathTooLongException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Invalid path", "The path length exceeds the system defined width."));
                    }
                    catch (Exception e)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Invalid path", $"Unknown exception occurred when reading path: {e.Message}."));
                    }
                }
                return outEither;
            }
            public static ParseEither<string> GetAllText(KeyValue setting)
            {
                var outEither = new ParseEither<string>();
                var trypath = GetFullPath(setting);

                if (trypath.HasFailed())
                {
                    outEither = new ParseEither<string>(trypath.Messages);
                }
                else if (Directory.Exists(trypath.Value))
                {
                    outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "The file given is a directory."));
                }
                else
                {
                    try
                    {
                        outEither.Value = File.ReadAllText(trypath.Value);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "The path cannot be found, possibly on an unmapped drive."));
                    }
                    catch (FileNotFoundException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "The specified file could not be found."));
                    }
                    catch (IOException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "An IO error occurred while opening the file.", "Make sure it is not opened in another program."));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "Unauthorised access.", "Make sure you have the right permissions to open this file."));
                    }
                    catch (System.Security.SecurityException)
                    {
                        outEither.AddMessage(new ErrorMessage(setting.ValueRange, "Could not open file", "The caller does not have the required permission.", "Make sure you have the right permissions to open this file."));
                    }
                }

                return outEither;
            }
            public static ParseEither<string> GetAllText(string path)
            {
                var outEither = new ParseEither<string>();
                var trypath = GetFullPath(path);

                if (trypath.HasFailed())
                {
                    Console.WriteLine("FAILED");
                    outEither = new ParseEither<string>(trypath.Messages);
                }
                else if (Directory.Exists(trypath.Value))
                {
                    outEither.AddMessage(new ErrorMessage(path, "Could not open file", "The file given is a directory."));
                }
                else
                {
                    try
                    {
                        outEither.Value = File.ReadAllText(trypath.Value);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Could not open file", "The path cannot be found, possibly on an unmapped drive."));
                    }
                    catch (FileNotFoundException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Could not open file", "The specified file could not be found."));
                    }
                    catch (IOException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Could not open file", "An IO error occurred while opening the file.", "Make sure it is not opened in another program."));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Could not open file", "Unauthorised access.", "Make sure you have the right permissions to open this file."));
                    }
                    catch (System.Security.SecurityException)
                    {
                        outEither.AddMessage(new ErrorMessage(path, "Could not open file", "The caller does not have the required permission.", "Make sure you have the right permissions to open this file."));
                    }
                }

                return outEither;
            }
        }
    }
}