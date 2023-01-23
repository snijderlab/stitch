using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Stitch {

    /// <summary> Auto generate version and help flags. </summary>
    public struct CLIBuilder {
        string Title;
        string Description;
        List<Subcommand> Subcommands;
        List<IArgument> Arguments;

        public CLIBuilder(string title, string description, List<Subcommand> subcommands, List<IArgument> arguments) {
            Title = title;
            Description = description;
            Subcommands = subcommands;
            Arguments = arguments;
            Arguments.Add(new Argument<bool>("help", new Option<string>("h"), new Option<bool>(false), "Print help information"));
            Arguments.Add(new Argument<bool>("version", new Option<string>("v"), new Option<bool>(false), "Print version information"));
            Arguments.Sort((a, b) => a.GetKey().CompareTo(b.GetKey()));
            Subcommands.Sort((a, b) => a.Key.CompareTo(b.Key));
        }

        public Dictionary<string, (Type, object)> Parse(string args) {
            bool printUsage = false;
            string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string exe = Assembly.GetExecutingAssembly().GetName().Name;
            var defaultColour = Console.ForegroundColor;

            // Split the args in the correct pieces
            var split_args = new List<string>();
            var current_arg = new StringBuilder();
            bool escaped = false;
            bool first = true;
            foreach (var c in args) {
                if (c == '"') {
                    escaped = !escaped;
                } else if (c == ' ' && !escaped) {
                    var temp = current_arg.ToString();
                    if (!string.IsNullOrWhiteSpace(temp) && !first) split_args.Add(temp);
                    current_arg.Clear();
                    first = false;
                } else {
                    current_arg.Append(c);
                }
            }
            var str = current_arg.ToString();
            if (!string.IsNullOrWhiteSpace(str) && !first) split_args.Add(str);

            // Parse the pieces
            var output = new Dictionary<string, (Type, object)>();
            var result = new ParseResult<Dictionary<string, (Type, object)>>(output);
            if (split_args.Count >= 1 && Subcommands.Any(s => s.Key == split_args[0])) {
                var system = Subcommands.Find(s => s.Key == split_args[0]);
                var res = system.Parse(exe, split_args);
                if (res.IsOk(result))
                    output.Add(system.Key, (typeof(Dictionary<string, (Type, object)>), res.Unwrap()));
            } else {
                for (int i = 0; i < split_args.Count; i++) {
                    var arg = split_args[i];
                    IArgument argument = null;
                    string data = "";
                    if (arg.StartsWith("--")) {
                        argument = Arguments.Find(a => a.GetFlag().Map(s => s == arg.Substring(2)).Unwrap(false));
                        if (argument != null && argument.GetArgumentType() != typeof(bool) && i + 1 < split_args.Count) {
                            data = split_args[i + 1];
                            i++;
                        }
                    } else if (arg.StartsWith("-")) {
                        argument = Arguments.Find(a => a.GetShortFlag().Map(s => s == arg.Substring(1)).Unwrap(false));
                        if (argument != null && argument.GetArgumentType() != typeof(bool) && i + 1 < split_args.Count) {
                            data = split_args[i + 1];
                            i++;
                        }
                    } else {
                        argument = Arguments.Find(a => !a.IsHandled() && a.GetDefaultValue().IsNone());
                        data = arg;
                    }
                    if (argument == null) {
                        if (arg != data)
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Flag does not exist"));
                        else {
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Too many arguments passed"));
                            printUsage = true;
                        }
                    } else if (argument.IsHandled()) {
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Cannot pass an argument twice"));
                    } else {
                        argument.MakeHandled();
                        var parsed = argument.Parse(data);
                        if (parsed.IsOk(result)) {
                            output.Add(argument.GetKey(), parsed.Unwrap());
                        }
                    }
                }
            }
            // Check for missing arguments and fill with the defaults
            var missing = new List<string>();
            foreach (var arg in Arguments) {
                if (!output.ContainsKey(arg.GetKey())) {
                    if (arg.GetDefaultValue().IsSome()) {
                        output.Add(arg.GetKey(), arg.GetDefaultValue().Unwrap());
                    } else {
                        missing.Add(arg.GetKey());
                        printUsage = true;
                    }
                }
            }
            // Print the usage
            if (printUsage) {
                if (missing.Count > 0) {
                    Console.WriteLine("The following required arguments were not provided:");
                    foreach (var mis in missing) {
                        Console.WriteLine($"\t<{mis}>");
                    }
                }
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());
                PrettyPrintHeader("USAGE");
                Console.Write("\t[OPTIONS]");
                foreach (var req in required) {
                    Console.Write($" <{req.GetKey()}>");
                }
                Console.WriteLine("\n\nFor more information try --help");
                Environment.Exit(0);
            }
            // Print Help
            if ((bool)output["help"].Item2 == true) {
                Console.WriteLine($"{Title} {version}");
                Console.WriteLine(Description);
                PrettyPrintHeader("SUBCOMMANDS");
                foreach (var sys in Subcommands) {
                    Console.WriteLine($"\t{sys.Key}\t{sys.Description}");
                }
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());

                PrettyPrintHeader("USAGE");
                Console.Write($"\t{exe} [SUBCOMMAND] <or> [OPTIONS]");
                foreach (var req in required) {
                    Console.Write($" <{req.GetKey()}>");
                }
                Console.WriteLine();
                if (required.Count() > 0) {
                    PrettyPrintHeader("ARGS");
                    foreach (var req in required) {
                        PrettyPrintArg(req);
                    }
                }
                var options = Arguments.Where(a => a.GetDefaultValue().IsSome());
                PrettyPrintHeader("OPTIONS");
                foreach (var opt in options) {
                    PrettyPrintOption(opt);
                }
                Environment.Exit(0);
            }
            // Print version
            if ((bool)output["version"].Item2 == true) {
                Console.WriteLine($"{Title} {version}");
                Environment.Exit(0);
            }

            if (result.IsErr()) {
                result.PrintMessages();
                Environment.Exit(1);
            }

            return output;
        }

        public static void PrettyPrintHeader(string text) {
            var defaultColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n{text}:");
            Console.ForegroundColor = defaultColour;
            Console.WriteLine();
        }
        public static void PrettyPrintOption(IArgument opt) {
            var defaultColour = Console.ForegroundColor;
            Console.Write('\t');
            Console.ForegroundColor = ConsoleColor.Blue;
            if (opt.GetShortFlag().IsSome()) {
                Console.Write($"-{opt.GetShortFlag().Unwrap()}");
                Console.ForegroundColor = defaultColour;
                Console.Write(", ");
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            Console.Write($"--{opt.GetFlag().Unwrap()}");
            Console.ForegroundColor = defaultColour;
            Console.Write($"\n\t\t{opt.GetDescription()}");
            if (opt.GetArgumentType() != typeof(bool)) Console.Write($" [default: {opt.GetDefaultValue().Unwrap().Item2}]");
            Console.WriteLine();
        }
        public static void PrettyPrintArg(IArgument opt) {
            var defaultColour = Console.ForegroundColor;
            Console.Write('\t');
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"<{opt.GetKey()}>\t");
            Console.ForegroundColor = defaultColour;
            Console.WriteLine(opt.GetDescription());
        }
    }

    public struct Subcommand {
        public string Key;
        public string Description;
        List<IArgument> Arguments;

        public Subcommand(string key, string description, List<IArgument> arguments) {
            Key = key.ToLower();
            Description = description;
            Arguments = arguments;
            Arguments.Add(new Argument<bool>("help", new Option<string>("h"), new Option<bool>(false), "Print help information"));
            Arguments.Sort((a, b) => a.GetKey().CompareTo(b.GetKey()));
        }

        public ParseResult<Dictionary<string, (Type, object)>> Parse(string exe, List<string> args) {
            bool printUsage = false;
            var defaultColour = Console.ForegroundColor;
            var output = new Dictionary<string, (Type, object)>();
            var result = new ParseResult<Dictionary<string, (Type, object)>>(output);
            for (int i = 1; i < args.Count; i++) {
                var arg = args[i];
                IArgument argument = null;
                string data = "";
                if (arg.StartsWith("--")) {
                    argument = Arguments.Find(a => a.GetFlag().Map(s => s == arg.Substring(2)).Unwrap(false));
                    if (argument != null && argument.GetArgumentType() != typeof(bool) && i + 1 < args.Count) {
                        data = args[i + 1];
                        i++;
                    }
                } else if (arg.StartsWith("-")) {
                    argument = Arguments.Find(a => a.GetShortFlag().Map(s => s == arg.Substring(1)).Unwrap(false));
                    if (argument != null && argument.GetArgumentType() != typeof(bool) && i + 1 < args.Count) {
                        data = args[i + 1];
                        i++;
                    }
                } else {
                    argument = Arguments.Find(a => (!a.IsHandled()) && a.GetDefaultValue().IsNone());
                    data = arg;
                }
                if (argument == null) {
                    if (arg != data)
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Flag does not exist"));
                    else {
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Too many arguments passed"));
                        printUsage = true;
                    }
                } else if (argument.IsHandled()) {
                    result.AddMessage(new InputNameSpace.ErrorMessage(arg, "Cannot pass an argument twice"));
                } else {
                    argument.MakeHandled();
                    var parsed = argument.Parse(data);
                    if (parsed.IsOk(result)) {
                        output.Add(argument.GetKey(), parsed.Unwrap());
                    }
                }
            }
            // Check for missing arguments and fill with the defaults
            var missing = new List<string>();
            foreach (var arg in Arguments) {
                if (!output.ContainsKey(arg.GetKey())) {
                    if (arg.GetDefaultValue().IsSome()) {
                        output.Add(arg.GetKey(), arg.GetDefaultValue().Unwrap());
                    } else {
                        missing.Add(arg.GetKey());
                        printUsage = true;
                    }
                }
            }
            // Print Help
            if ((bool)output["help"].Item2 == true) {
                Console.WriteLine($"{exe} {Key}");
                Console.WriteLine(Description);
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());
                CLIBuilder.PrettyPrintHeader("USAGE");
                Console.Write($"\t{exe} [OPTIONS]");
                foreach (var req in required) {
                    Console.Write($" <{req.GetKey()}>");
                }
                Console.WriteLine();
                if (required.Count() > 0) {
                    CLIBuilder.PrettyPrintHeader("ARGS");
                    foreach (var req in required) {
                        CLIBuilder.PrettyPrintArg(req);
                    }
                }
                var options = Arguments.Where(a => a.GetDefaultValue().IsSome());
                CLIBuilder.PrettyPrintHeader("OPTIONS");
                foreach (var opt in options) {
                    CLIBuilder.PrettyPrintOption(opt);
                }
                Environment.Exit(0);
            }
            // Print the usage
            if (printUsage) {
                if (missing.Count > 0) {
                    Console.WriteLine("The following required arguments were not provided:");
                    foreach (var mis in missing) {
                        Console.WriteLine($"\t<{mis}>");
                    }
                }
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());
                CLIBuilder.PrettyPrintHeader("USAGE");
                Console.Write($"\t{Key} [OPTIONS]");
                foreach (var req in required) {
                    Console.Write($" <{req.GetKey()}>");
                }
                Console.WriteLine("\n\nFor more information try --help");
            }
            return result;
        }
    }

    public interface IArgument {
        public string GetKey();
        public Option<string> GetFlag();
        public Option<string> GetShortFlag();
        public ParseResult<(Type, object)> Parse(string input);
        public Option<(Type, object)> GetDefaultValue();
        public Type GetArgumentType();
        public void MakeHandled();
        public bool IsHandled();
        public string GetDescription();
    }

    public struct Argument<T> : IArgument where T : IConvertible {
        /// <summary> The key also is the long flag if the argument is required.</summary>
        public string Key;
        /// <summary> ShortFlag only is active if the argument is required. </summary>
        public Option<string> ShortFlag;
        /// <summary> If there is no default the argument is required to pass. </summary>
        public Option<T> DefaultValue;
        public string Description;
        public bool Handled;

        public Argument(string key, Option<string> shortFlag, Option<T> defaultValue, string description) {
            if (key.StartsWith("-")) throw new ArgumentException("Argument Keys cannot start with hyphens, to prevent parsing errors.");
            if (key.Contains(" ")) throw new ArgumentException("Argument Keys cannot contain spaces.");
            if (shortFlag.IsSome() && defaultValue.IsNone()) throw new ArgumentException("Cannot have both a short flag and required argument.");
            Key = key.ToLower();
            ShortFlag = shortFlag;
            DefaultValue = defaultValue;
            Description = description;
            Handled = false;
        }

        public Type GetArgumentType() {
            return typeof(T);
        }
        public string GetKey() {
            return Key;
        }

        public Option<string> GetFlag() {
            if (DefaultValue.IsSome()) return new Option<string>(Key);
            else return new Option<string>();
        }
        public Option<string> GetShortFlag() {
            if (DefaultValue.IsSome()) return ShortFlag;
            else return new Option<string>();
        }

        public ParseResult<(Type, object)> Parse(string input) {
            try {
                if (typeof(T) == typeof(bool)) return new ParseResult<(Type, object)>((typeof(T), true));
                var p = (T)Convert.ChangeType(input, typeof(T));
                return new ParseResult<(Type, object)>((typeof(T), p));
            } catch {
                return new ParseResult<(Type, object)>(new InputNameSpace.ErrorMessage(input, "Not the correct type", $"Expected a {typeof(T)} but this argument could not be parsed as that type."));
            }
        }
        public Option<(Type, object)> GetDefaultValue() {
            return DefaultValue.Map(d => (typeof(T), (object)d));
        }

        public void MakeHandled() {
            Handled = true;
        }

        public bool IsHandled() {
            return Handled;
        }

        public string GetDescription() {
            return Description;
        }
    }
}