using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace AssemblyNameSpace
{
    /// <summary>
    /// A class with options to parse a batch file.
    /// </summary>
    static class ParseCommandFile
    {
        /// <summary>
        /// Parses a batch file and retrieves the runparameters.
        /// </summary>
        /// <param name="path">The path to the batch file.</param>
        /// <returns>The runparameters as specified in the file.</returns>
        public static RunParameters.FullRunParameters Batch(string path)
        {
            if (!File.Exists(path)) {
                throw new ParseException("The specified batch file does not exist.");
            }
            // Get the contents
            string content = File.ReadAllText(path).Trim();

            // Tokenize the file, into a key value pair tree
            var parsed = new List<KeyValue>();
            while (content.Length > 0)
            {
                switch (content.First())
                {
                    case '-':
                        // This line is a comment, skip it
                        ParseHelper.SkipLine(ref content);
                        break;
                    case '\n':
                        //skip
                        content = content.Trim();
                        break;
                    default:
                        // This is a parameter line, get the name
                        var name = ParseHelper.Name(ref content);

                        // Find if it is a single or multiple valued parameter
                        if (content[0] == ':')
                        {
                            content = content.Remove(0, 1).Trim();
                            //Get the single value of the parameter
                            string value = ParseHelper.Value(ref content);
                            parsed.Add(new KeyValue(name, value));
                        }
                        else if (content[0] == '-' && content[1] == '>')
                        {
                            content = content.Remove(0, 2).Trim();
                            // Now get the multiple values
                            var values = new List<KeyValue>();

                            while (true)
                            {

                                if (content[0] == '-')
                                {
                                    // A comment skip it
                                    ParseHelper.SkipLine(ref content);
                                }
                                else if (content[0] == '<' && content[1] == '-')
                                {
                                    // This is the end of the multiple valued parameter
                                    content = content.Remove(0, 2).Trim();
                                    parsed.Add(new KeyValue(name, values));
                                    break;
                                }
                                else
                                {
                                    // Match the inner parameter
                                    var innername = ParseHelper.Name(ref content);

                                    // Find if it is a single line or multiple line valued inner parameter
                                    if (content[0] == ':')
                                    {
                                        content = content.Remove(0, 1).Trim();
                                        string value = ParseHelper.Value(ref content);
                                        values.Add(new KeyValue(innername, value));
                                    }
                                    else if (content[0] == '-' && content[1] == '>')
                                    {
                                        content = content.Remove(0, 2).Trim();
                                        string value = ParseHelper.UntilSequence(ref content, "<-");
                                        values.Add(new KeyValue(innername, value));
                                    }

                                    content = content.Trim();
                                }
                            }
                        }
                        else
                        {
                            throw new ParseException($"Parameter {name.ToString()} should be followed by an delimiter (':' or '->')");
                        }
                        break;
                }
            }

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
                                    settings.File.Path = setting.GetValue();
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
                                    rsettings.File.Path = setting.GetValue();
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
                                    fastasettings.File.Path = setting.GetValue();
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
                        var asettings = new RunParameters.AlphabetValue();

                        foreach (var setting in pair.GetValues())
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
                        output.Alphabet.Add(asettings);
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
                                    fsettings.Path = setting.GetValue();
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

            return output;
        }
        /// <summary>
        /// A class with helper functionality for parsing
        /// </summary>
        static class ParseHelper
        {
            /// <summary>
            /// Consumes a whole line of the string
            /// </summary>
            /// <param name="content">The string</param>
            public static void SkipLine(ref string content)
            {
                int nextnewline = FindNextNewLine(ref content);
                if (nextnewline > 0)
                {
                    content = content.Remove(0, nextnewline).Trim();
                }
                else
                {
                    content = "";
                }
            }
            /// <summary>
            /// To find the next newline, this needs to be written by hand instead of using "String.IndexOf()" because that gives weird behavior in .NET Core
            /// </summary>
            /// <param name="content">The string to search in</param>
            /// <returns>The position of the next newline ('\n' or '\r') or -1 if none could be found</returns>
            public static int FindNextNewLine(ref string content) {
                for (int pos = 0; pos < content.Length; pos++) {
                    if (content[pos] == '\n' || content[pos] == '\r') {
                        return pos;
                    }
                }
                return -1;
            }
            /// <summary>
            /// Consumes a name from the start of the string
            /// </summary>
            /// <param name="content">The string</param>
            /// <returns>The name</returns>
            public static string Name(ref string content)
            {
                var name = new StringBuilder();
                while (Char.IsLetterOrDigit(content[0]) || content[0] == ' ')
                {
                    name.Append(content[0]);
                    content = content.Remove(0, 1);
                }
                content = content.Trim();
                return name.ToString().ToLower().Trim();
            }
            /// <summary>
            /// Consumes a value from the start of the string
            /// </summary>
            /// <param name="content">The string</param>
            /// <returns>The value</returns>
            public static string Value(ref string content)
            {
                string result = "";
                int nextnewline = FindNextNewLine(ref content);
                if (nextnewline > 0)
                {
                    result = content.Substring(0, nextnewline).Trim();
                    content = content.Remove(0, nextnewline).Trim();
                }
                else
                {
                    result = content.Trim();
                    content = "";
                }
                return result;
            }
            /// <summary>
            /// Consumes the string until it find the sequence
            /// </summary>
            /// <param name="content">The string</param>
            /// <param name="sequence">The sequence to find</param>
            /// <returns>The consumed part of the string</returns>
            public static string UntilSequence(ref string content, string sequence)
            {
                int nextnewline = -1;
                bool found = false;
                var contentarray = content.ToCharArray();
                for (int pos = 0; pos <= contentarray.Length - sequence.Length && !found; pos++) {
                    for (int offset = 0; offset <= sequence.Length; offset++ ) {
                        if (offset == sequence.Length) {
                            nextnewline = pos;
                            found = true;
                            break;
                        }
                        if (contentarray[pos+offset] != sequence[offset]) {
                            break;
                        }
                    }
                }

                string value = "";
                if (nextnewline > 0)
                {
                    value = content.Substring(0, nextnewline).Trim();
                    content = content.Remove(0, nextnewline + sequence.Length).Trim();
                }
                else
                {
                    value = content.Trim();
                    content = "";
                }
                return value;
            }
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
        }
        /// <summary>
        /// A class to save key value trees
        /// </summary>
        class KeyValue
        {
            /// <summary>
            /// The name of a key
            /// </summary>
            public string Name;
            /// <summary>
            /// The value for this key
            /// </summary>
            ValueType Value;
            /// <summary>
            /// Create a new single valued key
            /// </summary>
            /// <param name="name">The name of the key</param>
            /// <param name="value">The value of the key</param>
            public KeyValue(string name, string value)
            {
                Name = name.ToLower();
                Value = new Single(value);
            }
            /// <summary>
            /// Create a new multiple valued key
            /// </summary>
            /// <param name="name">The name of the key</param>
            /// <param name="values">The list of KeyValue tree(s) that are the value of this key.</param>
            public KeyValue(string name, List<KeyValue> values)
            {
                Name = name.ToLower();
                Value = new KeyValue.Multiple(values);
            }
            /// <summary>
            /// Tries to get a single value from this key, otherwise fails with an error message for the end user
            /// </summary>
            /// <returns>The value of the KeyValue</returns>
            public string GetValue()
            {
                if (Value is Single)
                {
                    return ((Single)Value).Value;
                }
                else
                {
                    throw new ParseException($"Parameter {Name} has multiple values but should have a single value.");
                }
            }
            /// <summary>
            /// Tries to get tha values from this key, only succeeds if this KeyValue is multiple valued, otherwise fails with an error message for the end user
            /// </summary>
            /// <returns>The values of this KeyValue</returns>
            public List<KeyValue> GetValues()
            {
                if (Value is Multiple)
                {
                    return ((Multiple)Value).Values;
                }
                else
                {
                    throw new ParseException($"Parameter {Name} has a single value but should have multiple values. Value {GetValue()}");
                }
            }
            /// <summary>
            /// To test if this is a single valued KeyValue
            /// </summary>
            /// <returns>A bool indicating that.</returns>
            public bool IsSingle()
            {
                return Value is Single;
            }
            /// <summary>
            /// An abstract class to represent possible values for a KeyValue
            /// </summary>
            abstract class ValueType { }
            /// <summary>
            /// A ValueType for a single valued KeyValue
            /// </summary>
            class Single : ValueType
            {
                /// <summary>
                /// The value
                /// </summary>
                public string Value;
                /// <summary>
                /// To create a single value
                /// </summary>
                /// <param name="value">The value</param>
                public Single(string value)
                {
                    Value = value;
                }
            }
            /// <summary>
            /// A ValueType to contain multiple values
            /// </summary>
            class Multiple : ValueType
            {
                /// <summary>
                /// The list of values
                /// </summary>
                public List<KeyValue> Values;
                /// <summary>
                /// To create a multiple value
                /// </summary>
                /// <param name="values">The values</param>
                public Multiple(List<KeyValue> values)
                {
                    Values = values;
                }
            }
        }
    }
    /// <summary>
    /// An exception to indicate some error while parsing the batch file
    /// </summary>
    class ParseException : Exception
    {
        /// <summary>
        /// To create a ParseException
        /// </summary>
        /// <param name="msg">The message for this Exception</param>
        public ParseException(string msg)
            : base(msg) { }
    }
}