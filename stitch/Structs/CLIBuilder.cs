using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Stitch {

    /// <summary> A data structure to keep a command line interface for automatic parsing, default values, required arguments, help, versions flag and much more. </summary>
    public struct CLIBuilder {
        /// <summary> The title of the program. </summary>
        string Title;
        /// <summary> The description of the program for the generated documentation. </summary>
        string Description;
        /// <summary> The list of possible subcommands. </summary>
        List<Subcommand> Subcommands;
        /// <summary> The lust of possible arguments. </summary>
        List<IArgument> Arguments;

        /// <summary>
        /// Create a new CLI builder.
        /// </summary>
        /// <param name="title">The title of the program.</param>
        /// <param name="description">The description of the program for the generated documentation.</param>
        /// <param name="subcommands">The list of possible subcommands.</param>
        /// <param name="arguments">The lust of possible arguments.</param>
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

        /// <summary>
        /// Parse the arguments into a dictionary of the arguments. To allow for the immediate return in the correct type each argument is presented as a tuple of (Type, object).
        /// </summary>
        public Dictionary<string, (Type, object)> Parse(string args) {
            bool printUsage = false;
            // Get the metadata of this program for generated docs
            string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string exe = Assembly.GetExecutingAssembly().GetName().Name;
            var defaultColour = Console.ForegroundColor;

            // Split the args in the correct pieces taking care of double quotes as escape characters
            var split_args = SplitArgs(args);

            // Parse the pieces
            var output = new Dictionary<string, (Type, object)>();
            var result = new ParseResult<Dictionary<string, (Type, object)>>(output);
            if (split_args.Count >= 1 && Subcommands.Any(s => s.Key == split_args[0].Item1)) {
                var system = Subcommands.Find(s => s.Key == split_args[0].Item1);
                var res = system.Parse(exe, split_args);
                if (res.IsOk(result))
                    output.Add(system.Key, (typeof(Dictionary<string, (Type, object)>), res.Unwrap()));
            } else {
                for (int i = 0; i < split_args.Count; i++) {
                    var arg = split_args[i];
                    IArgument argument = null;
                    (string, FileRange) data = ("", arg.Item2);
                    if (arg.Item1.StartsWith("--")) {
                        argument = Arguments.Find(a => a.GetFlag().Map(s => s == arg.Item1.Substring(2)).Unwrap(false));
                        if (argument != null && argument.GetArgumentType() != typeof(bool)) {
                            if (i + 1 < split_args.Count) {
                                data = split_args[i + 1];
                                i++;
                            } else {
                                result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Missing argument for flag"));
                            }
                        }
                    } else if (arg.Item1.StartsWith("-")) {
                        argument = Arguments.Find(a => a.GetShortFlag().Map(s => s == arg.Item1.Substring(1)).Unwrap(false));
                        if (argument != null && argument.GetArgumentType() != typeof(bool)) {
                            if (i + 1 < split_args.Count) {
                                data = split_args[i + 1];
                                i++;
                            } else {
                                result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Missing argument for flag"));
                            }
                        }
                    } else {
                        argument = Arguments.Find(a => !a.IsHandled() && a.GetDefaultValue().IsNone());
                        data = arg;
                    }
                    if (argument == null) {
                        if (arg.Item1 != data.Item1)
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Flag does not exist"));
                        else {
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Too many arguments passed"));
                            printUsage = true;
                        }
                    } else if (argument.IsHandled()) {
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Cannot pass an argument twice"));
                    } else {
                        argument.MakeHandled();
                        if (result.IsOk()) {
                            var parsed = argument.Parse(data);
                            if (parsed.IsOk(result)) {
                                output.Add(argument.GetKey(), parsed.Unwrap());
                            }
                        }
                    }
                }
            }
            printUsage = output.Count == 0 ? true : printUsage;
            // Check for missing arguments and fill with the defaults
            var missing = new List<IArgument>();
            foreach (var argument in Arguments) {
                if (!output.ContainsKey(argument.GetKey())) {
                    if (argument.GetDefaultValue().IsSome()) {
                        output.Add(argument.GetKey(), argument.GetDefaultValue().Unwrap());
                    } else {
                        missing.Add(argument);
                        printUsage = true;
                    }
                }
            }
            // Print the usage
            if (printUsage && !(bool)output["help"].Item2 == true && result.IsOk()) {
                if (missing.Count > 0) {
                    Console.WriteLine("The following required arguments were not provided:");
                    foreach (var mis in missing) {
                        PrettyPrintArg(mis);
                    }
                }
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());
                PrettyPrintHeader("USAGE");
                Console.WriteLine($"\t{exe} [SUBCOMMAND] <or> [OPTIONS]");
                PrettyPrintHeader("EXAMPLE USAGE");
                Console.WriteLine($"\t{exe} run batchfiles/monoclonal.txt --open");
                Console.WriteLine("\nFor more information try --help");
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

        /// <summary> Split an argument string into its constituent parts taking care of enclosing quotes ". </summary>
        private static List<(string, FileRange)> SplitArgs(string args) {
            var split_args = new List<(string, FileRange)>();
            var full_line = new ParsedFile(new ReadFormat.FileIdentifier("CLI", "CLI", null), new string[] { args });
            var current_arg = new StringBuilder();
            bool escaped = false;
            bool first = true;
            int start = 0;
            for (var i = 0; i < args.Length; i++) {
                var c = args[i];
                if (c == '"') {
                    escaped = !escaped;
                } else if (c == ' ' && !escaped) {
                    var temp = current_arg.ToString();
                    if (!string.IsNullOrWhiteSpace(temp) && !first) split_args.Add((temp, new FileRange(new Position(0, start + 1, full_line), new Position(0, i + 1, full_line))));
                    current_arg.Clear();
                    first = false;
                    start = i + 1;
                } else {
                    current_arg.Append(c);
                }
            }
            var str = current_arg.ToString();
            if (!string.IsNullOrWhiteSpace(str) && !first) split_args.Add((str, new FileRange(new Position(0, start + 1, full_line), new Position(0, args.Length + 1, full_line))));
            return split_args;
        }
        /// <summary> Make a nice header with the given title. </summary>
        public static void PrettyPrintHeader(string text) {
            var defaultColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n{text}:");
            Console.ForegroundColor = defaultColour;
            Console.WriteLine();
        }
        /// <summary> Make a nice print of the given optional argument. </summary>
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
        /// <summary> Make a nice print of the given required argument. </summary>
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

        public ParseResult<Dictionary<string, (Type, object)>> Parse(string exe, List<(string, FileRange)> args) {
            bool printUsage = false;
            var defaultColour = Console.ForegroundColor;
            var output = new Dictionary<string, (Type, object)>();
            var result = new ParseResult<Dictionary<string, (Type, object)>>(output);
            for (int i = 1; i < args.Count; i++) {
                var arg = args[i];
                IArgument argument = null;
                (string, FileRange) data = ("", arg.Item2);
                if (arg.Item1.StartsWith("--")) {
                    argument = Arguments.Find(a => a.GetFlag().Map(s => s == arg.Item1.Substring(2)).Unwrap(false));
                    if (argument != null && argument.GetArgumentType() != typeof(bool)) {
                        if (i + 1 < args.Count) {
                            data = args[i + 1];
                            i++;
                        } else {
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Missing argument for flag"));
                        }
                    }
                } else if (arg.Item1.StartsWith("-")) {
                    argument = Arguments.Find(a => a.GetShortFlag().Map(s => s == arg.Item1.Substring(1)).Unwrap(false));
                    if (argument != null && argument.GetArgumentType() != typeof(bool)) {
                        if (i + 1 < args.Count) {
                            data = args[i + 1];
                            i++;
                        } else {
                            result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Missing argument for flag"));
                        }
                    }
                } else {
                    argument = Arguments.Find(a => (!a.IsHandled()) && a.GetDefaultValue().IsNone());
                    data = arg;
                }
                if (argument == null) {
                    if (arg.Item1 != data.Item1)
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Flag does not exist"));
                    else {
                        result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Too many arguments passed"));
                        printUsage = true;
                    }
                } else if (argument.IsHandled()) {
                    result.AddMessage(new InputNameSpace.ErrorMessage(arg.Item2, "Cannot pass an argument twice"));
                } else {
                    argument.MakeHandled();
                    if (result.IsOk()) {
                        var parsed = argument.Parse(data);
                        if (parsed.IsOk(result)) {
                            output.Add(argument.GetKey(), parsed.Unwrap());
                        }
                    }
                }
            }
            // Check for missing arguments and fill with the defaults
            var missing = new List<IArgument>();
            foreach (var arg in Arguments) {
                if (!output.ContainsKey(arg.GetKey())) {
                    if (arg.GetDefaultValue().IsSome()) {
                        output.Add(arg.GetKey(), arg.GetDefaultValue().Unwrap());
                    } else {
                        missing.Add(arg);
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
                        CLIBuilder.PrettyPrintArg(mis);
                    }
                }
                var required = Arguments.Where(a => a.GetDefaultValue().IsNone());
                CLIBuilder.PrettyPrintHeader("USAGE");
                Console.Write($"\t{exe} {Key} [OPTIONS]");
                foreach (var req in required) {
                    Console.Write($" <{req.GetKey()}>");
                }
                Console.WriteLine("\n\nFor more information try --help");

                // Print any error that also popped up to prevent having to rerun the command many times before all errors can be found.
                result.PrintMessages();

                // Done
                Environment.Exit(0);
            }
            return result;
        }
    }

    public interface IArgument {
        /// <summary> Get the name of the argument. </summary>
        public string GetKey();
        /// <summary> Get the flag of the argument to be matched against the provided flag by the users, without the preceding two hyphens. </summary>
        public Option<string> GetFlag();
        /// <summary> Get the short flag of the argument to be matched against the provided flag by the users, without the preceding hyphen. </summary>
        public Option<string> GetShortFlag();
        /// <summary> Parse the given text in the given location into the inner type of this argument. </summary>
        public ParseResult<(Type, object)> Parse((string, FileRange) input);
        /// <summary> Get the default value for this argument. </summary>
        public Option<(Type, object)> GetDefaultValue();
        /// <summary> Get the inner type of this argument. </summary>
        public Type GetArgumentType();
        /// <summary> Record that this argument has already been used, needed for duplicate definition detection and missing required argument detection. </summary>
        public void MakeHandled();
        /// <summary> See if this argument is already handled. </summary>
        public bool IsHandled();
        /// <summary> Get the description of this argument for the generation of documentation. </summary>
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

        public ParseResult<(Type, object)> Parse((string, FileRange) input) {
            try {
                if (typeof(T) == typeof(bool)) return new ParseResult<(Type, object)>((typeof(T), true));
                var p = (T)Convert.ChangeType(input.Item1, typeof(T));
                return new ParseResult<(Type, object)>((typeof(T), p));
            } catch {
                return new ParseResult<(Type, object)>(new InputNameSpace.ErrorMessage(input.Item2, "Not the correct type", $"Expected a {typeof(T)} but this argument could not be parsed as that type."));
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