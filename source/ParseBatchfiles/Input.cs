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
                            throw new ParseException("The specified version does not exist");
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
                                throw new ParseException($"Unknown option for Runtype: {pair.GetValue()}, the options are: 'Group' and 'Separate'.");
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
                                    settings.Cutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks Cutoffscore");
                                    break;
                                case "localcutoffscore":
                                    settings.LocalCutoffscore = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks LocalCutoffscore");
                                    break;
                                case "minlengthpatch":
                                    settings.MinLengthPatch = ParseHelper.ConvertToInt(setting.GetValue(), "Peaks MinLengthPatch");
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
                                        throw new ParseException("Unknown file format for Peaks, the options are: 'Old' and 'New'.");
                                    }
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in PEAKS definition: {setting.Name}, the options are: 'Path', 'CutoffScore', 'LocalCutoffscore', 'MinLengthPatch', 'Name', 'Separator', 'DecimalSeparator' and 'Format'.");
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
                                    throw new ParseException($"Unknown key in Reads definition: {setting.Name}, the options are: 'Path' and 'Name'.");
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
                                    throw new ParseException($"Unknown key in FASTAInput definition: {setting.Name}, the options are: 'Path' and 'Name'.");
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
                                        ksettings.Start = ParseHelper.ConvertToInt(setting.GetValue(), "range K Start");
                                        break;
                                    case "end":
                                        ksettings.End = ParseHelper.ConvertToInt(setting.GetValue(), "range K End");
                                        break;
                                    case "step":
                                        ksettings.Step = ParseHelper.ConvertToInt(setting.GetValue(), "range K Step");
                                        break;
                                    default:
                                        throw new ParseException($"Unknown key in K definition: {setting.Name}, the options are: 'Start', 'End' and 'Step'.");
                                }
                            }
                            if (ksettings.Start > 0 && ksettings.End > 0)
                            {
                                output.K = ksettings;
                            }
                            else
                            {
                                throw new ParseException("A range of K should be set with a start and an end value");
                            }
                            break;
                        }
                        else
                        {
                            try
                            {
                                output.K = new RunParameters.K.Single(ParseHelper.ConvertToInt(pair.GetValue(), "single K value"));
                            }
                            catch
                            {
                                var values = new List<int>();
                                foreach (string value in pair.GetValue().Split(",".ToCharArray()))
                                {
                                    values.Add(ParseHelper.ConvertToInt(value, "multiple K values"));
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
                                throw new ParseException($"Unknown option in Reverse definition: {pair.GetValue()}, the options are: 'True', 'False' and 'Both'.");
                        }
                        break;
                    case "alphabet":
                        output.Alphabet.Add(ParseHelper.ParseAlphabet(pair.GetValues()));
                        break;
                    case "template":
                        output.Template.Add(ParseHelper.ParseTemplate(pair.GetValues(), true));
                        break;
                    case "recombine":
                        var recsettings = new RunParameters.RecombineValue();
                        string order = "";

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "n":
                                    recsettings.N = ParseHelper.ConvertToInt(setting.GetValue(), "number of top hits to take from the templates <n>");
                                    break;
                                case "order":
                                    order = setting.GetValue();
                                    break;
                                case "templates":
                                    foreach (var template in setting.GetValues())
                                    {
                                        if (template.Name != "template")
                                            throw new ParseException($"Unknown key in Recombine - Templates definition: {setting.Name}, only 'Template' is valid");

                                        recsettings.Templates.Add(ParseHelper.ParseTemplate(template.GetValues(), false));
                                    }
                                    break;
                                case "alphabet":
                                    recsettings.Alphabet = ParseHelper.ParseAlphabet(setting.GetValues());
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in Recombine definition: {setting.Name}, the options are 'N', 'Order', 'Templates' and 'Alphabet'");
                            }
                        }

                        // Test if the template names are unique and valid
                        var template_names = new List<string>();
                        foreach (var template in recsettings.Templates)
                        {
                            if (template_names.Contains(template.Name))
                            {
                                throw new ParseException($"Templates in Recombine should have a unique name, the name '{template.Name}' is not unique");
                            }
                            if (template.Name.Contains('*'))
                            {
                                throw new ParseException($"Names of Templates in Recombine cannot contain a '*' as this will be confusing, the name '{template.Name}' is not valid");
                            }
                            template_names.Add(template.Name);
                        }

                        // Parse the order
                        while (order != "")
                        {
                            order = order.Trim();
                            var match = false;
                            for (int i = 0; i < recsettings.Templates.Count(); i++)
                            {
                                var template = recsettings.Templates[i];
                                if (order.StartsWith(template.Name))
                                {
                                    order = order.Remove(0, template.Name.Length);
                                    recsettings.Order.Add(new RunParameters.RecombineOrder.Template(i));
                                    match = true;
                                    break;
                                }
                            }
                            if (match) continue;

                            if (order.StartsWith('*'))
                            {
                                order = order.Remove(0, 1);
                                recsettings.Order.Add(new RunParameters.RecombineOrder.Gap());
                            }
                            else
                            {
                                throw new ParseException($"Invalid Order definition in Recombine, cannot proceed past '{order}', the only valid options are a name of a template (as defined in 'Templates'), a gap (defined as '*') or whitespace");
                            }
                        }

                        //output.Recombine.Add(recsettings);
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
                                        throw new ParseException($"Unknown option in HTML DotDistribution definition: {setting.Name}, the options are: 'Global' and 'Included'.");
                                    }
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in HTML definition: {setting.Name}, the options are: 'Path' and 'DotDistribution'.");
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
                                    throw new ParseException($"Unknown key in CSV definition: {setting.Name}, the option is: 'Path'");
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
                                    fsettings.MinimalScore = ParseHelper.ConvertToInt(setting.GetValue(), "minimalscore of FASTA report");
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in FASTA definition: {setting.Name}, the options are: 'Path' and 'MinimalScore'.");
                            }
                        }
                        output.Report.Add(fsettings);
                        break;
                    default:
                        throw new ParseException($"Unknown key {pair.Name}");
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
                throw new ParseException($"There is no version specified for the batch file; This is needed to handle different versions in different ways.");
            }

            // Reset the working directory
            Directory.SetCurrentDirectory(original_working_directory);
            return output;
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
            public static int ConvertToInt(string input, string origin)
            {
                try
                {
                    return Convert.ToInt32(input);
                }
                catch (FormatException)
                {
                    throw new ParseException($"The value '{input}' is not a valid number, this should be a number in the context of {origin}.");
                }
                catch (OverflowException)
                {
                    throw new ParseException($"The value '{input}' is outside the bounds of an int32, in the context of {origin}.");
                }
                catch
                {
                    throw new ParseException($"Some unkown ParseException occurred while '{input}' was converted to an int32, this should be a number in the context of {origin}.");
                }
            }
            public static RunParameters.AlphabetValue ParseAlphabet(List<KeyValue> values)
            {
                var asettings = new RunParameters.AlphabetValue();

                foreach (var setting in values)
                {
                    switch (setting.Name)
                    {
                        case "path":
                            asettings.Data = File.ReadAllText(setting.GetValue());
                            break;
                        case "data":
                            asettings.Data = setting.GetValue();
                            break;
                        case "name":
                            asettings.Name = setting.GetValue();
                            break;
                        default:
                            throw new ParseException($"Unknown key in Alphabet definition: {setting.Name}, the options are 'Path', 'Data', and 'Name'");
                    }
                }

                return asettings;
            }

            public static RunParameters.TemplateValue ParseTemplate(List<KeyValue> values, bool alphabet)
            {
                var tsettings = new RunParameters.TemplateValue();

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
                                    throw new ParseException($"Unknown option in InputType definition: {setting.GetValue()}, the options are 'Reads' and 'Fasta'");
                            }
                            break;
                        case "name":
                            tsettings.Name = setting.GetValue();
                            break;
                        case "alphabet":
                            if (!alphabet) throw new ParseException($"Alphabet cannot be defined here: {setting.Name}, the options are 'Path', 'Type' and 'Name'");
                            tsettings.Alphabet = ParseHelper.ParseAlphabet(setting.GetValues());
                            break;
                        default:
                            var tail = "'Path', 'Type', 'Name' and 'Alphabet'";
                            if (!alphabet) tail = "'Path', 'Type' and 'Name'";
                            throw new ParseException($"Unknown key in Template definition: {setting.Name}, the options are {tail}");
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

                return tsettings;
            }
        }
    }
}