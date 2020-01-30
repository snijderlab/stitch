using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
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
            InputNameSpace.BatchFile.Name = path;
            InputNameSpace.BatchFile.Content = batchfilecontent.Split('\n');

            // Tokenize the file, into a key value pair tree
            var parsed = InputNameSpace.Tokenizer.Tokenize(batchfilecontent);

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
                                    settings.File.Name = setting.GetValue();
                                    break;
                                case "separator":
                                    settings.Separator = setting.GetValue().First();
                                    break;
                                case "decimalseparator":
                                    settings.DecimalSeparator = setting.GetValue().First();
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
                                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in PEAKS Format definition", "Valid options are: 'Old' and 'New'."));
                                    }
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in PEAKS definition", "Valid options are: 'Path', 'CutoffScore', 'LocalCutoffscore', 'MinLengthPatch', 'Name', 'Separator', 'DecimalSeparator' and 'Format'."));
                                    break;
                            }
                        }

                        output.DataParameters.Add(settings);
                        break;

                    case "reads":
                        var rsettings = new RunParameters.Input.Reads();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    rsettings.File.Path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                    break;
                                case "name":
                                    rsettings.File.Name = setting.GetValue();
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in Reads definition", "Valid options are: 'Path' and 'Name'."));
                                    break;
                            }
                        }

                        output.DataParameters.Add(rsettings);
                        break;

                    case "fastainput":
                        var fastasettings = new RunParameters.Input.FASTA();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    fastasettings.File.Path = ParseHelper.GetFullPath(setting).GetValue(outEither);
                                    break;
                                case "name":
                                    fastasettings.File.Name = setting.GetValue();
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in FASTAInput definition", "Valid options are: 'Path' and 'Name'."));
                                    break;
                            }
                        }

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
                                        ksettings.Start = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    case "end":
                                        ksettings.End = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    case "step":
                                        ksettings.Step = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                        break;
                                    default:
                                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in K definition", "Valid options are: 'Start', 'End' and 'Step'."));
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
                        output.DuplicateThreshold.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse(pair.GetValue(), pair.ValueRange).GetValue(outEither)));
                        break;
                    case "minimalhomology":
                        output.MinimalHomology.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse(pair.GetValue(), pair.ValueRange).GetValue(outEither)));
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
                                outEither.AddMessage(new ErrorMessage(pair.KeyRange.Name, "Unknown key", "Unknown key in Reverse definition", "Valid options are: 'True', 'False' and 'Both'."));
                                break;
                        }
                        break;
                    case "alphabet":
                        output.Alphabet.Add(ParseHelper.ParseAlphabet(pair).GetValue(outEither));
                        break;
                    case "template":
                        output.Template.Add(ParseHelper.ParseTemplate(pair.GetValues(), true).GetValue(outEither));
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
                                    order = setting;
                                    break;
                                case "templates":
                                    foreach (var template in setting.GetValues())
                                    {
                                        if (template.Name == "template")
                                        {
                                            var templatevalue = ParseHelper.ParseTemplate(template.GetValues(), false).GetValue(outEither);
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
                                            outEither.AddMessage(new ErrorMessage(template.KeyRange.Name, "Unknown key", "Unknown key in Templates definition", "Valid options are: 'Template'."));
                                        }
                                    }
                                    break;
                                case "alphabet":
                                    recsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in Recombine definition", "Valid options are: 'N', 'Order', 'Templates' and 'Alphabet'."));
                                    break;
                            }
                        }

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
                            outEither.AddMessage(new ErrorMessage(pair.KeyRange.Full, "Order undefined", "No definition for 'Order' provided in Recombine"));
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
                                    hsettings.Path = setting.GetValue();
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
                                        outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in HTML DotDistribution definition", "Valid options are: 'Global' and 'Included'."));
                                    }
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in HTML definition", "Valid options are: 'Path' and 'DotDistribution'."));
                                    break;
                            }
                        }
                        output.Report.Add(hsettings);
                        break;
                    case "csv":
                        var csettings = new RunParameters.Report.CSV();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    csettings.Path = setting.GetValue();
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in CSV definition", "Valid options are: 'Path'."));
                                    break;
                            }
                        }
                        output.Report.Add(csettings);
                        break;
                    case "fasta":
                        var fsettings = new RunParameters.Report.FASTA();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    fsettings.Path = setting.GetValue();
                                    break;
                                case "minimalscore":
                                    fsettings.MinimalScore = ParseHelper.ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                                    break;
                                default:
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in FASTA definition", "Valid options are: 'Path' and 'MinimalScore'."));
                                    break;
                            }
                        }
                        output.Report.Add(fsettings);
                        break;
                    default:
                        outEither.AddMessage(new ErrorMessage(pair.KeyRange.Name, "Unknown key", "Unknown key in definition"));
                        break;
                }
            }

            // Generate defaults
            if (output.DuplicateThreshold.Count() == 0)
            {
                output.DuplicateThreshold.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse("K-1", new Range(new Position(1, 1), new Position(1, 1))).GetValue(outEither)));
            }
            if (output.MinimalHomology.Count() == 0)
            {
                output.MinimalHomology.Add(new RunParameters.KArithmetic(RunParameters.KArithmetic.TryParse("K-1", new Range(new Position(1, 1), new Position(1, 1))).GetValue(outEither)));
            }

            // Check if there is a version specified
            if (!versionspecified)
            {
                outEither.AddMessage(new ErrorMessage(new Position(0, 1), "No version specified", "There is no version specified for the batch file; This is needed to handle different versions in different ways."));
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
            if (Messages.Count() == 0)
            {
                return false;
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
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"There were {Messages.Count()} error(s) while parsing '{BatchFile.Name}'.\n");
                Console.ForegroundColor = defaultColour;

                foreach (var msg in Messages)
                {
                    msg.Print();
                }

                throw new ParseException("");
            }
            else
            {
                return Value;
            }
        }
        public T GetValue<Tout>(ParseEither<Tout> fail)
        {
            if (!this.HasFailed())
            {
                return Value;
            }
            else
            {
                fail.Messages.AddRange(Messages);
                return Value;
            }
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
        static class BatchFile
        {
            public static string Name = "";
            public static string[] Content = new string[1];
        }
        public class ErrorMessage
        {
            Position startposition = new Position(0, 0);
            Position endposition = new Position(0, 0);
            string shortDescription = "";
            string longDescription = "";
            string helpDescription = "";
            string subject = "";
            public ErrorMessage(string sub, string shortD, string longD = "", string help = "")
            {
                subject = sub;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
            }
            public ErrorMessage(Position pos, string shortD, string longD = "", string help = "")
            {
                startposition = pos;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
            }
            public ErrorMessage(Range range, string shortD, string longD = "", string help = "")
            {
                startposition = range.Start;
                endposition = range.End;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
            }
            public override string ToString()
            {
                // Header
                var header = $">> Error: {shortDescription}\n";

                // Location
                string location = "";
                if (subject != "")
                {
                    location = $"\n   | {subject}\n\n";
                }
                else if (endposition == new Position(0, 0))
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = BatchFile.Content[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + "^^^";
                    location = $"File: {BatchFile.Name}\n{start}\n{line_number} | {line}\n{start}{pos}\n{start}\n";
                }
                else if (startposition.Line == endposition.Line)
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = BatchFile.Content[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + new string('^', endposition.Column - startposition.Column);
                    location = $"File: {BatchFile.Name}\n{start}\n{line_number} | {line}\n{start}{pos}\n{start}\n";
                }
                else
                {
                    var line_number = endposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    location = $"File: {BatchFile.Name}\n{start}\n";

                    for (int i = startposition.Line; i <= endposition.Line; i++)
                    {
                        var line = BatchFile.Content[i];
                        var number = i.ToString().PadRight(line_number.Length + 1);
                        location += $"{number}| {line}\n";
                    }
                    location += $"{start}\n";
                }

                // Body
                var body = "";
                if (longDescription != "") body += longDescription + "\n";
                if (helpDescription != "") body += helpDescription + "\n";

                return header + location + body;
            }
            public void Print()
            {
                var defaultColour = Console.ForegroundColor;

                // Header
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($">> Error: {shortDescription}");
                Console.ForegroundColor = defaultColour;

                // Location
                if (subject != "")
                {
                    Console.WriteLine($"\n   | {subject}\n");
                }
                else if (endposition == new Position(0, 0))
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = BatchFile.Content[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + "^^^";
                    Console.Write($"File: {BatchFile.Name}\n{start}\n{line_number} | {line}\n{start}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(pos);
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"\n{start}\n");
                }
                else if (startposition.Line == endposition.Line)
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = BatchFile.Content[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + new string('^', endposition.Column - startposition.Column);
                    Console.Write($"File: {BatchFile.Name}\n{start}\n{line_number} | {line}\n{start}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(pos);
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"\n{start}\n");
                }
                else
                {
                    var line_number = endposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    Console.Write($"File: {BatchFile.Name}\n{start}\n");

                    for (int i = startposition.Line; i <= endposition.Line; i++)
                    {
                        var line = BatchFile.Content[i];
                        var number = i.ToString().PadRight(line_number.Length + 1);
                        Console.Write($"{number}| {line}\n");
                    }
                    Console.Write($"{start}\n");
                }

                // Body
                if (longDescription != "")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(longDescription);
                    Console.ForegroundColor = defaultColour;
                }
                if (helpDescription != "") Console.WriteLine(helpDescription);
                Console.WriteLine("");
            }
        }
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
                            asettings.Data = GetAllText(setting).GetValue(outEither);
                            break;
                        case "data":
                            asettings.Data = setting.GetValue();
                            break;
                        case "name":
                            asettings.Name = setting.GetValue();
                            break;
                        case "gapstartpenalty":
                            asettings.GapStartPenalty = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        case "gapextendpenalty":
                            asettings.GapExtendPenalty = ConvertToInt(setting.GetValue(), setting.ValueRange).GetValue(outEither);
                            break;
                        default:
                            outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unknown key in Alphabet definition", "Valid options are: 'Path', 'Data', 'Name', 'GapStartPenalty' and 'GapExtendPenalty'."));
                            break;
                    }
                }

                if (asettings.Name == null)
                {
                    outEither.AddMessage(new ErrorMessage(key.KeyRange.Full, "Name of Alphabet not defined", "", "Consider giving the Alphabet a name by using 'Name'."));
                }
                if (asettings.Data == null)
                {
                    outEither.AddMessage(new ErrorMessage(key.KeyRange.Full, "Data of Alphabet not defined", "", "Consider giving the Alphabet data by using 'Data' or 'Path'."));
                }

                return outEither;
            }
            public static ParseEither<RunParameters.TemplateValue> ParseTemplate(List<KeyValue> values, bool alphabet)
            {
                var tsettings = new RunParameters.TemplateValue();
                var outEither = new ParseEither<RunParameters.TemplateValue>(tsettings);

                foreach (var setting in values)
                {
                    switch (setting.Name)
                    {
                        case "path":
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
                                    outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unkown key", "Unkown key in InputType definition in Template definition", "Valid options are: 'Reads' and 'Fasta'."));
                                    break;
                            }
                            break;
                        case "name":
                            tsettings.Name = setting.GetValue();
                            break;
                        case "alphabet":
                            if (!alphabet)
                            {
                                outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Alphabet cannot be defined here", "Inside a template in the templates list of a recombination an alphabet should not be defined."));
                            }
                            else
                            {
                                tsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            }
                            break;
                        default:
                            var tail = "'Path', 'Type', 'Name' and 'Alphabet'";
                            if (!alphabet) tail = "'Path', 'Type' and 'Name'";
                            outEither.AddMessage(new ErrorMessage(setting.KeyRange.Name, "Unknown key", "Unkown key in Template definition.", $"Valid options are: {tail}"));
                            break;
                    }
                }

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