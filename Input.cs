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
    static class ParseCommandFile
    {
        public static RunParameters Batch(string path)
        {
            string content = File.ReadAllText(path).Trim();
            var parsed = new List<KeyValue>();
            while (content.Length > 0)
            {
                Console.WriteLine($"{content.Length} chars to do still");
                switch (content.First())
                {
                    case '-':
                        ParseHelper.SkipLine(ref content);
                        break;
                    case '\n':
                        //skip
                        content = content.Trim();
                        break;
                    default:
                        var name = ParseHelper.Name(ref content);

                        if (content[0] == ':')
                        {
                            content = content.Remove(0, 1).Trim();
                            string value = ParseHelper.Value(ref content);
                            parsed.Add(new KeyValue(name, value));
                        }
                        else if (content[0] == '-' && content[1] == '>')
                        {
                            content = content.Remove(0, 2).Trim();
                            // Now get multiple values
                            var values = new List<KeyValue>();

                            while (true)
                            {

                                if (content[0] == '-')
                                {
                                    ParseHelper.SkipLine(ref content);
                                }
                                else if (content[0] == '<' && content[1] == '-')
                                {
                                    content = content.Remove(0, 2).Trim();
                                    parsed.Add(new KeyValue(name, values));
                                    break;
                                }
                                else
                                {
                                    var innername = ParseHelper.Name(ref content);

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
            // Now all key values are saved in 'parsed'
            var output = new RunParameters();

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
                        break;
                    case "runtype":
                        switch (pair.GetValue().ToLower())
                        {
                            case "separate":
                                output.Runtype = RuntypeValue.Separate;
                                break;
                            case "group":
                                output.Runtype = RuntypeValue.Group;
                                break;
                            default:
                                throw new ParseException($"Unknown option for Runtype: {pair.GetValue()}");
                        }
                        break;
                    case "peaks":
                        var settings = new Peaks();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    settings.Path = setting.GetValue();
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
                                    settings.Name = setting.GetValue();
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
                                        throw new ParseException("Unknown file format for Peaks (choose 'old' or 'new')");
                                    }
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in PEAKS definition: {setting.Name}");
                            }
                        }

                        output.Input.Add(settings);
                        break;

                    case "reads":
                        var rsettings = new Reads();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    rsettings.Path = setting.GetValue();
                                    break;
                                case "name":
                                    rsettings.Name = setting.GetValue();
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in Reads definition: {setting.Name}");
                            }
                        }

                        output.Input.Add(rsettings);
                        break;

                    case "k":
                        if (!pair.IsSingle())
                        {
                            var ksettings = new Range();

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
                                        throw new ParseException($"Unknown key in K definition: {setting.Name}");
                                }
                            }
                            if (ksettings.Start != 0 && ksettings.End != 0)
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
                                output.K = new AssemblyNameSpace.Single(ParseHelper.ConvertToInt(pair.GetValue(), "single K value"));
                            }
                            catch
                            {
                                var values = new List<int>();
                                foreach (string value in pair.GetValue().Split(",".ToCharArray()))
                                {
                                    values.Add(ParseHelper.ConvertToInt(value, "multiple K values"));
                                }
                                output.K = new AssemblyNameSpace.Multiple(values.ToArray());
                            }
                        }
                        break;
                    case "duplicatethreshold":
                        try
                        {
                            output.DuplicateThreshold.Add(new AssemblyNameSpace.Simple(Convert.ToInt32(pair.GetValue())));
                        }
                        catch
                        {
                            output.DuplicateThreshold.Add(new AssemblyNameSpace.Calculation(pair.GetValue()));
                        }
                        break;
                    case "minimalhomology":
                        try
                        {
                            output.MinimalHomology.Add(new AssemblyNameSpace.Simple(Convert.ToInt32(pair.GetValue())));
                        }
                        catch
                        {
                            output.MinimalHomology.Add(new AssemblyNameSpace.Calculation(pair.GetValue()));
                        }
                        break;
                    case "reverse":
                        switch (pair.GetValue().ToLower())
                        {
                            case "true":
                                output.Reverse = ReverseValue.True;
                                break;
                            case "false":
                                output.Reverse = ReverseValue.False;
                                break;
                            case "both":
                                output.Reverse = ReverseValue.Both;
                                break;
                            default:
                                throw new ParseException($"Unknown option in Reverse definition: {pair.GetValue()}");
                        }
                        break;
                    case "alphabet":
                        var asettings = new AlphabetValue();

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
                                    throw new ParseException($"Unknown key in Alphabet definition: {setting.Name}");
                            }
                        }
                        output.Alphabet.Add(asettings);
                        break;
                    case "html":
                        var hsettings = new HTML();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    hsettings.Path = setting.GetValue();
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in HTML definition: {setting.Name}");
                            }
                        }
                        output.Report.Add(hsettings);
                        break;
                    case "csv":
                        var csettings = new CSV();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    csettings.Path = setting.GetValue();
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in CSV definition: {setting.Name}");
                            }
                        }
                        output.Report.Add(csettings);
                        break;
                    case "fastq":
                        var fsettings = new FASTQ();

                        foreach (var setting in pair.GetValues())
                        {
                            switch (setting.Name)
                            {
                                case "path":
                                    fsettings.Path = setting.GetValue();
                                    break;
                                default:
                                    throw new ParseException($"Unknown key in FASTQ definition: {setting.Name}");
                            }
                        }
                        output.Report.Add(fsettings);
                        break;
                    default:
                        throw new ParseException($"Unknown key {pair.Name}");
                }
            }

            if (output.DuplicateThreshold.Count() == 0)
            {
                output.DuplicateThreshold.Add(new Calculation("K-1"));
            }
            if (output.MinimalHomology.Count() == 0)
            {
                output.MinimalHomology.Add(new Calculation("K-1"));
            }

            return output;
        }
        static class ParseHelper
        {
            public static void SkipLine(ref string content)
            {
                int nextnewline = content.IndexOf("\n");
                if (nextnewline > 0)
                {
                    content = content.Remove(0, nextnewline).Trim();
                }
                else
                {
                    content = "";
                }
            }
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
            public static string Value(ref string content)
            {
                return UntilSequence(ref content, "\n");
            }
            public static string UntilSequence(ref string content, string sequence)
            {
                int nextnewline = content.IndexOf(sequence);
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
                    throw new ParseException($"Some unkown ParseException occured while '{input}' was cnverted to an int32, this should be a number in the context of {origin}.");
                }
            }
        }
        class KeyValue
        {
            public string Name;
            ValueType Value;
            public KeyValue(string name, string value)
            {
                Name = name;
                Value = new Single(value);
            }
            public KeyValue(string name, List<KeyValue> values)
            {
                Name = name;
                Value = new KeyValue.Multiple(values);
            }
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
            public bool IsSingle()
            {
                return Value is Single;
            }

            abstract class ValueType { }
            class Single : ValueType
            {
                public string Value;
                public Single(string value)
                {
                    Value = value;
                }
            }
            class Multiple : ValueType
            {
                public List<KeyValue> Values;
                public Multiple(List<KeyValue> values)
                {
                    Values = values;
                }
            }
        }
    }
    class ParseException : Exception
    {
        public ParseException(string msg)
            : base(msg) { }
    }
}