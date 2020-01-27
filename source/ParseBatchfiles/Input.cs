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
            if (!File.Exists(path))
            {
                throw new ParseException("The specified batch file does not exist.");
            }
            // Set the working directory to the directory of the batchfile
            var original_working_directory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(path));

            // Get the contents
            string content = File.ReadAllText(Path.GetFileName(path)).Trim();

            // Tokenize the file, into a key value pair tree
            var parsed = InputNameSpace.Tokenizer.Tokenize(content);

            // Now all key value pairs are saved in 'parsed'
            // Now parse the key value pairs into RunParameters

            var output = new RunParameters.FullRunParameters();
            var outEither = new ParseEither<RunParameters.FullRunParameters>(output);

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
                            outEither.AddMessage($"The specified version '{pair.GetValue()}' does not exist. In 'Version' {pair.Position}");
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
                                outEither.AddMessage($"Unknown option for Runtype: {pair.GetValue()} {pair.Position}, the options are: 'Group' and 'Separate'.");
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
                                    settings.File.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "cutoffscore":
                                    settings.Cutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks Cutoffscore", setting.Position).GetValue(outEither);
                                    break;
                                case "localcutoffscore":
                                    settings.LocalCutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks LocalCutoffscore", setting.Position).GetValue(outEither);
                                    break;
                                case "minlengthpatch":
                                    settings.MinLengthPatch = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks MinLengthPatch", setting.Position).GetValue(outEither);
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
                                        outEither.AddMessage($"Unknown file format for Peaks {setting.Position}, the options are: 'Old' and 'New'.");
                                    }
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in PEAKS definition: {setting.Name} {setting.Position}, the options are: 'Path', 'CutoffScore', 'LocalCutoffscore', 'MinLengthPatch', 'Name', 'Separator', 'DecimalSeparator' and 'Format'.");
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
                                    rsettings.File.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "name":
                                    rsettings.File.Name = setting.GetValue();
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in Reads definition: {setting.Name} {setting.Position}, the options are: 'Path' and 'Name'.");
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
                                    fastasettings.File.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "name":
                                    fastasettings.File.Name = setting.GetValue();
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in FASTAInput definition: {setting.Name} {setting.Position}, the options are: 'Path' and 'Name'.");
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
                                        ksettings.Start = ParseHelper.ConvertToInt(setting.GetValue(), "range K Start", setting.Position).GetValue(outEither);
                                        break;
                                    case "end":
                                        ksettings.End = ParseHelper.ConvertToInt(setting.GetValue(), "range K End", setting.Position).GetValue(outEither);
                                        break;
                                    case "step":
                                        ksettings.Step = ParseHelper.ConvertToInt(setting.GetValue(), "range K Step", setting.Position).GetValue(outEither);
                                        break;
                                    default:
                                        outEither.AddMessage($"Unknown key in K definition: {setting.Name} {setting.Position}, the options are: 'Start', 'End' and 'Step'.");
                                        break;
                                }
                            }
                            if (ksettings.Start > 0 && ksettings.End > 0)
                            {
                                output.K = ksettings;
                            }
                            else
                            {
                                outEither.AddMessage($"A range of K should be set with a start and an end value. At 'K' {pair.Position}");
                            }
                            break;
                        }
                        else
                        {
                            try
                            {
                                output.K = new RunParameters.K.Single(ParseHelper.ConvertToInt(pair.GetValue(), "single K value", pair.Position).GetValue(outEither));
                            }
                            catch
                            {
                                var values = new List<int>();
                                foreach (string value in pair.GetValue().Split(",".ToCharArray()))
                                {
                                    values.Add(ParseHelper.ConvertToInt(value, "multiple K values", pair.Position).GetValue(outEither));
                                }
                                output.K = new RunParameters.K.Multiple(values.ToArray());
                            }
                        }
                        break;
                    case "duplicatethreshold":
                        output.DuplicateThreshold.Add(new RunParameters.KArithmetic(pair.GetValue()));
                        break;
                    case "minimalhomology":
                        output.MinimalHomology.Add(new RunParameters.KArithmetic(pair.GetValue()));
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
                                outEither.AddMessage($"Unknown option in Reverse definition: {pair.GetValue()} {pair.Position}, the options are: 'True', 'False' and 'Both'.");
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
                        if (output.Recombine != null) outEither.AddMessage($"Cannot have multiple definitions of Recombine. At {pair.Position}.");

                        var recsettings = new RunParameters.RecombineValue();
                        KeyValue order = null;

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "n":
                                    recsettings.N = ParseHelper.ConvertToInt(setting.GetValue(), "number of top hits to take from the templates <n>", setting.Position).GetValue(outEither);
                                    break;
                                case "order":
                                    order = setting;
                                    break;
                                case "templates":
                                    foreach (var template in setting.GetValues())
                                    {
                                        if (template.Name == "template")
                                        {
                                            recsettings.Templates.Add(ParseHelper.ParseTemplate(template.GetValues(), false).GetValue(outEither));
                                        }
                                        else
                                        {
                                            outEither.AddMessage($"Unknown key in Templates {template.Position} definition: {template.Name}, only 'Template' is valid");
                                        }
                                    }
                                    break;
                                case "alphabet":
                                    recsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in Recombine definition: {setting.Name} {setting.Position}, the options are 'N', 'Order', 'Templates' and 'Alphabet'");
                                    break;
                            }
                        }

                        // Test if the template names are unique and valid
                        var template_names = new List<string>();
                        foreach (var template in recsettings.Templates)
                        {
                            if (template_names.Contains(template.Name))
                            {
                                outEither.AddMessage($"Templates in Recombine should have a unique name, the name '{template.Name}' is not unique");
                            }
                            if (template.Name.Contains('*'))
                            {
                                outEither.AddMessage($"Names of Templates in Recombine cannot contain a '*' as this will be confusing, the name '{template.Name}' is not valid");
                            }
                            template_names.Add(template.Name);
                        }

                        // Parse the order
                        if (order != null)
                        {
                            var order_string = order.GetValue();
                            while (order_string != "")
                            {
                                order_string = order_string.Trim();
                                var match = false;
                                for (int i = 0; i < recsettings.Templates.Count(); i++)
                                {
                                    var template = recsettings.Templates[i];
                                    if (order_string.StartsWith(template.Name))
                                    {
                                        order_string = order_string.Remove(0, template.Name.Length);
                                        recsettings.Order.Add(new RunParameters.RecombineOrder.Template(i));
                                        match = true;
                                        break;
                                    }
                                }
                                if (match) continue;

                                if (order_string.StartsWith('*'))
                                {
                                    order_string = order_string.Remove(0, 1);
                                    recsettings.Order.Add(new RunParameters.RecombineOrder.Gap());
                                }
                                else
                                {
                                    outEither.AddMessage($"Invalid Order definition in Recombine {order.Position}, cannot proceed past '{order_string}', the only valid options are a name of a template (as defined in 'Templates'), a gap (defined as '*') or whitespace");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            outEither.AddMessage($"No definition for the order in Recombine {pair.Position}");
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
                                        outEither.AddMessage($"Unknown option in HTML DotDistribution {setting.Position} definition: {setting.Name}, the options are: 'Global' and 'Included'.");
                                    }
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in HTML definition: {setting.Name} {setting.Position}, the options are: 'Path' and 'DotDistribution'.");
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
                                    outEither.AddMessage($"Unknown key in CSV definition: {setting.Name} {setting.Position}, the option is: 'Path'");
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
                                    fsettings.Path = Path.GetFullPath(setting.GetValue());
                                    break;
                                case "minimalscore":
                                    fsettings.MinimalScore = ParseHelper.ConvertToInt(setting.GetValue(), "minimalscore of FASTA report", setting.Position).GetValue(outEither);
                                    break;
                                default:
                                    outEither.AddMessage($"Unknown key in FASTA definition: {setting.Name} {setting.Position}, the options are: 'Path' and 'MinimalScore'.");
                                    break;
                            }
                        }
                        output.Report.Add(fsettings);
                        break;
                    default:
                        outEither.AddMessage($"Unknown key {pair.Name} {pair.Position}");
                        break;
                }
            }

            // Generate defaults
            if (output.DuplicateThreshold.Count() == 0)
            {
                output.DuplicateThreshold.Add(new RunParameters.KArithmetic("K-1"));
            }
            if (output.MinimalHomology.Count() == 0)
            {
                output.MinimalHomology.Add(new RunParameters.KArithmetic("K-1"));
            }

            // Check if there is a version specified
            if (!versionspecified)
            {
                outEither.AddMessage($"There is no version specified for the batch file; This is needed to handle different versions in different ways.");
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
    public class ParseEither<T>
    {
        public T Value = default(T);
        public string Message = "";
        public ParseEither(T t)
        {
            Value = t;
        }
        public ParseEither(string s)
        {
            Message = s;
        }
        public ParseEither() { }
        public bool HasFailed()
        {
            if (Message == "")
            {
                return false;
            }
            return true;
        }
        public ParseEither<Tout> Do<Tout>(Func<T, Tout> func, string failMessage = "")
        {
            if (!this.HasFailed())
            {
                return new ParseEither<Tout>(func(this.Value));
            }
            else
            {
                var msg = this.Message + (failMessage == "" ? "" : "\n" + failMessage);
                return new ParseEither<Tout>(msg);
            }
        }
        public T ReturnOrFail()
        {
            if (this.HasFailed())
            {
                throw new ParseException(Message);
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
                fail.Message = (fail.Message == "" ? "" : fail.Message + "\n") + this.Message;
                return Value;
            }
        }
        public void AddMessage(string failMessage)
        {
            this.Message = (this.Message == "" ? "" : this.Message + "\n") + (failMessage == "" ? "" : failMessage);
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
            /// <param name="origin">The place where the string originates from, to be included in error messages.</param>
            /// <returns>If successfull: the number (int32)</returns>
            public static ParseEither<int> ConvertToInt(string input, string origin, Position pos)
            {
                try
                {
                    return new ParseEither<int>(Convert.ToInt32(input));
                }
                catch (FormatException)
                {
                    return new ParseEither<int>($"The value '{input}' is not a valid number, this should be a number in the context of {origin} {pos}.");
                }
                catch (OverflowException)
                {
                    return new ParseEither<int>($"The value '{input}' is outside the bounds of an int32, in the context of {origin} {pos}.");
                }
                catch
                {
                    return new ParseEither<int>($"Some unkown Exception occurred while '{input}' was converted to an int32, this should be a number in the context of {origin} {pos}.");
                }
            }
            public static ParseEither<RunParameters.AlphabetValue> ParseAlphabet(KeyValue key)
            {
                var asettings = new RunParameters.AlphabetValue();
                var outEither = new ParseEither<RunParameters.AlphabetValue>(asettings);

                if (key.GetValues().Count() == 0)
                {
                    outEither.AddMessage($"There should always be arguments defined for an Alphabet. At position {key.Position}.");
                    return outEither;
                }

                foreach (var setting in key.GetValues())
                {
                    switch (setting.Name)
                    {
                        case "path":
                            try
                            {
                                asettings.Data = File.ReadAllText(setting.GetValue());
                            }
                            catch (System.IO.FileNotFoundException)
                            {
                                outEither.AddMessage($"The file: {setting.GetValue()} could not be found. Defined at 'path' at {setting.Position}.");
                            }
                            catch (Exception e)
                            {
                                outEither.AddMessage($"Exception while reading file: {setting.GetValue()}. Defined at 'path' at {setting.Position}.\n{e.Message}");
                            }
                            break;
                        case "data":
                            asettings.Data = setting.GetValue();
                            break;
                        case "name":
                            asettings.Name = setting.GetValue();
                            break;
                        case "gapstartpenalty":
                            asettings.GapStartPenalty = ConvertToInt(setting.GetValue(), "gapstartpenalty in alphabet definition", setting.Position).GetValue(outEither);
                            break;
                        case "gapextendpenalty":
                            asettings.GapExtendPenalty = ConvertToInt(setting.GetValue(), "gapextendpenalty in alphabet definition", setting.Position).GetValue(outEither);
                            break;
                        default:
                            outEither.AddMessage($"Unknown key in Alphabet definition: {setting.Name} {setting.Position}, the options are 'Path', 'Data', 'Name', 'GapStartPenalty' and 'GapExtendPenalty'.");
                            break;
                    }
                }

                if (asettings.Name == null)
                {
                    outEither.AddMessage($"The name of an Alphabet should always be defined. At position {key.Position}.");
                }
                if (asettings.Data == null)
                {
                    outEither.AddMessage($"The data of an Alphabet should always be defined, either as raw data or as a path. At position {key.Position}.");
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
                            tsettings.Path = Path.GetFullPath(setting.GetValue());
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
                                    outEither.AddMessage($"Unknown option in InputType definition: {setting.GetValue()} {setting.Position}, the options are 'Reads' and 'Fasta'");
                                    break;
                            }
                            break;
                        case "name":
                            tsettings.Name = setting.GetValue();
                            break;
                        case "alphabet":
                            if (!alphabet)
                            {
                                outEither.AddMessage($"Alphabet cannot be defined here: {setting.Name} {setting.Position}, the options are 'Path', 'Type' and 'Name'");
                            }
                            else
                            {
                                tsettings.Alphabet = ParseHelper.ParseAlphabet(setting).GetValue(outEither);
                            }
                            break;
                        default:
                            var tail = "'Path', 'Type', 'Name' and 'Alphabet'";
                            if (!alphabet) tail = "'Path', 'Type' and 'Name'";
                            outEither.AddMessage($"Unknown key in Template definition: {setting.Name} {setting.Position}, the options are {tail}");
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
        }
    }
}