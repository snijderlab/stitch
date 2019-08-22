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
                            throw new Exception($"A name should be followed by an delimiter (':' or '->'). Name: {name.ToString()} Content left: {content}");
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
                        //skip for now
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
                                    settings.Cutoffscore = Convert.ToInt32(setting.GetValue());
                                    break;
                                case "localcutoffscore":
                                    settings.LocalCutoffscore = Convert.ToInt32(setting.GetValue());
                                    break;
                                case "minlengthpatch":
                                    settings.MinLengthPatch = Convert.ToInt32(setting.GetValue());
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
                                        throw new Exception("Unknown file format for Peaks (choose 'old' or 'new')");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown key in PEAKS definition: {setting.Name}");
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
                                    throw new Exception($"Unknown key in Reads definition: {setting.Name}");
                            }
                        }

                        output.Input.Add(rsettings);
                        break;

                    case "k":
                        if (pair.Value is Multiple)
                        {
                            var ksettings = new Range();

                            foreach (var setting in pair.GetValues())
                            {
                                switch (setting.Name)
                                {
                                    case "start":
                                        ksettings.Start = Convert.ToInt32(setting.GetValue());
                                        break;
                                    case "end":
                                        ksettings.End = Convert.ToInt32(setting.GetValue());
                                        break;
                                    case "step":
                                        ksettings.Step = Convert.ToInt32(setting.GetValue());
                                        break;
                                    default:
                                        throw new Exception($"Unknown key in K definition: {setting.Name}");
                                }
                            }
                            output.K = ksettings;
                            break;
                        }
                        else
                        {
                            try
                            {
                                output.K = new AssemblyNameSpace.Single(Convert.ToInt32(pair.GetValue()));
                            }
                            catch
                            {
                                var values = new List<int>();
                                foreach (string value in pair.GetValue().Split(",".ToCharArray()))
                                {
                                    values.Add(Convert.ToInt32(value));
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
                            output.MinHomology.Add(new AssemblyNameSpace.Simple(Convert.ToInt32(pair.GetValue())));
                        }
                        catch
                        {
                            output.MinHomology.Add(new AssemblyNameSpace.Calculation(pair.GetValue()));
                        }
                        break;
                    case "reverse":
                        switch (pair.GetValue().ToLower())
                        {
                            case "true":
                                output.Reverse = new One(true);
                                break;
                            case "false":
                                output.Reverse = new One(false);
                                break;
                            case "both":
                                output.Reverse = new Both();
                                break;
                            default:
                                throw new Exception($"Unknown option in Reverse definition: {pair.GetValue()}");
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
                                    throw new Exception($"Unknown key in Alphabet definition: {setting.Name}");
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
                                    throw new Exception($"Unknown key in HTML definition: {setting.Name}");
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
                                    throw new Exception($"Unknown key in CSV definition: {setting.Name}");
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
                                    throw new Exception($"Unknown key in FASTQ definition: {setting.Name}");
                            }
                        }
                        output.Report.Add(fsettings);
                        break;
                    default:
                        throw new Exception($"Unknown key {pair.Name}");
                }
            }

            if (output.DuplicateThreshold.Count() == 0) {
                output.DuplicateThreshold.Add(new Calculation("K-1"));
            }
            if (output.MinHomology.Count() == 0) {
                output.MinHomology.Add(new Calculation("K-1"));
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
        }
        class KeyValue
        {
            public string Name;
            public Value Value;
            public KeyValue(string name, string value)
            {
                Name = name;
                Value = new Single(value);
            }
            public KeyValue(string name, List<KeyValue> values)
            {
                Name = name;
                Value = new Multiple(values);
            }
            public string GetValue()
            {
                if (Value is Single)
                {
                    return ((Single)Value).Value;
                }
                else
                {
                    throw new Exception($"This KeyValue pair has multiple values. Key {Name}");
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
                    throw new Exception($"This KeyValue pair has a single value. Key {Name} Value {GetValue()}");
                }
            }
        }
        abstract class Value { }
        class Single : Value
        {
            public string Value;
            public Single(string value)
            {
                Value = value;
            }
        }
        class Multiple : Value
        {
            public List<KeyValue> Values;
            public Multiple(List<KeyValue> values)
            {
                Values = values;
            }
        }
    }
}