using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Stitch {
    namespace InputNameSpace {
        public class ErrorMessage {
            readonly Position? start_position;
            readonly Position? end_position;
            readonly ParsedFile File;
            readonly string shortDescription = "";
            List<string> Notes = new List<string>();
            List<string> Help = new List<string>();
            readonly string subject = "";
            readonly uint contextLines = 0;
            public bool Warning { get; private set; }
            public ErrorMessage(string sub, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 2) {
                subject = sub;
                shortDescription = shortD;
                if (!String.IsNullOrWhiteSpace(longD)) Notes.Add(longD);
                if (!String.IsNullOrWhiteSpace(help)) Help.Add(help);
                Warning = warning;
                File = new ParsedFile();
                this.contextLines = contextLines;
            }
            public ErrorMessage(ParsedFile file, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 2) {
                shortDescription = shortD;
                if (!String.IsNullOrWhiteSpace(longD)) Notes.Add(longD);
                if (!String.IsNullOrWhiteSpace(help)) Help.Add(help);
                Warning = warning;
                File = file;
                this.contextLines = contextLines;
            }
            public ErrorMessage(Position pos, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 2) {
                start_position = pos;
                shortDescription = shortD;
                if (!String.IsNullOrWhiteSpace(longD)) Notes.Add(longD);
                if (!String.IsNullOrWhiteSpace(help)) Help.Add(help);
                Warning = warning;
                File = pos.File;
                this.contextLines = contextLines;
            }
            public ErrorMessage(FileRange range, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 2) {
                start_position = range.Start;
                end_position = range.End;
                shortDescription = shortD;
                if (!String.IsNullOrWhiteSpace(longD)) Notes.Add(longD);
                if (!String.IsNullOrWhiteSpace(help)) Help.Add(help);
                Warning = warning;
                File = range.File;
                this.contextLines = contextLines;
            }
            public static ErrorMessage DuplicateValue(FileRange range) {
                var output = new ErrorMessage(range, "Duplicate parameter definition", "A value for this property was already defined.", "", true);
                return output;
            }
            public static ErrorMessage MissingParameter(FileRange range, string parameter) {
                return new ErrorMessage(range, $"Missing parameter: {parameter}", "", "", false);
            }
            public static ErrorMessage UnknownKey(FileRange range, string context, string options) {
                return new ErrorMessage(range, "Unknown key", $"Unknown key in {context} definition.", $"Valid options are: {options}.", true);
            }
            public override string ToString() {
                // Header
                var name = Warning ? "Warning" : "Error";
                var header = $">> {name}: {shortDescription}\n";

                // Location
                string location;
                if (!string.IsNullOrEmpty(subject)) {
                    location = $"\n   | {subject}\n\n";
                } else if (string.IsNullOrEmpty(File.Identifier.Path)) {
                    location = "";
                } else if (!start_position.HasValue) {
                    location = $"File: {File.Identifier.Path}\n";
                } else if (!end_position.HasValue) {
                    var line_number = (start_position.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[start_position.Value.Line];
                    var pos = new string(' ', start_position.Value.Column - 1) + "^^^";
                    var context1 = start_position.Value.Line > 1 ? $"{start}{File.Lines[start_position.Value.Line - 1]}\n" : "";
                    var context2 = start_position.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[start_position.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Identifier.Path}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                } else if (start_position.Value.Line == end_position.Value.Line) {
                    var line_number = (start_position.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[start_position.Value.Line];
                    var pos = new string(' ', Math.Max(0, start_position.Value.Column - 1)) + new string('^', Math.Max(1, end_position.Value.Column - start_position.Value.Column));
                    var context1 = start_position.Value.Line > 1 ? $"{start}{File.Lines[start_position.Value.Line - 1]}\n" : "";
                    var context2 = end_position.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[end_position.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Identifier.Path}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                } else {
                    var line_number = (end_position.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var context1 = start_position.Value.Line > 1 ? $"{start}{File.Lines[start_position.Value.Line - 1]}\n" : "";
                    var context2 = end_position.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[end_position.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Identifier.Path}\n\n{context1}";

                    for (int i = start_position.Value.Line; i <= end_position.Value.Line; i++) {
                        var line = File.Lines[i];
                        var number = (i + 1).ToString().PadRight(line_number.Length + 1);
                        location += $"{number}| {line}\n";
                    }
                    location += $"{context2}\n";
                }

                // Body
                var body = "";
                foreach (var note in Notes) body += note + "\n";
                foreach (var help in Help) body += help + "\n";

                return header + location + body;
            }
            public ErrorMessage AddNote(string note) {
                Notes.Add(note);
                return this;
            }
            public ErrorMessage AddHelp(string help) {
                Help.Add(help);
                return this;
            }
            public void Print() {
                var defaultColour = Console.ForegroundColor;

                // Header
                Console.ForegroundColor = Warning ? ConsoleColor.Blue : ConsoleColor.Red;
                var name = Warning ? "Warning" : "Error";
                Console.Write($"\n{name}: ");
                Console.ForegroundColor = defaultColour;
                Console.WriteLine(shortDescription);

                // Location
                if (!string.IsNullOrEmpty(subject)) // Pre given location
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("\n   │ ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write(subject + "\n");
                } else if (string.IsNullOrEmpty(File.Identifier.Path)) // No location
                  {
                } else if (!start_position.HasValue) // Only a file
                  {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("  --> ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"{File.Identifier.Path}\n");
                } else // A location in a file
                  {
                    var endline = !end_position.HasValue ? start_position.Value.Line : end_position.Value.Line;
                    var number_width = endline < File.Lines.Length - 1 ? (endline + 1).ToString().Length : (start_position.Value.Line + 1).ToString().Length;
                    var line_number = (start_position.Value.Line + 1).ToString().PadRight(number_width + 1, ' ');
                    var spacing = new string(' ', number_width + 3);
                    var line = File.Lines[start_position.Value.Line];

                    void print_line(int line_index) {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write((line_index + 1).ToString().PadRight(number_width + 1, ' '));
                        Console.Write("│ ");
                        Console.ForegroundColor = defaultColour;
                        Console.Write(File.Lines[line_index]);
                        Console.Write("\n");
                    }

                    void print_empty(bool newline, char border = '│') {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(new string(' ', number_width + 1));
                        Console.Write(border);
                        Console.ForegroundColor = defaultColour;
                        if (newline) Console.Write('\n');
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(new string(' ', number_width + 1));
                    Console.Write("╭── ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"{File.Identifier.Path}:{start_position.Value.Line + 1}:{start_position.Value.Column + 1}\n");
                    print_empty(true);

                    for (int i = (int)contextLines; i > 0; i--)
                        if (start_position.Value.Line - i > 0) print_line(start_position.Value.Line - i);

                    if (!end_position.HasValue || start_position.Value.Line > end_position.Value.Line) // Single position
                    {
                        var pos = new string(' ', start_position.Value.Column) + "───";
                        print_line(start_position.Value.Line);
                        print_empty(false, '·');
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    } else if (start_position.Value.Line == end_position.Value.Line) // Single line
                      {
                        var pos = new string(' ', Math.Max(0, start_position.Value.Column)) + new string('─', Math.Max(1, end_position.Value.Column - start_position.Value.Column));
                        print_line(start_position.Value.Line);
                        print_empty(false, '·');
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    } else // Multiline
                      {
                        for (int i = start_position.Value.Line; i <= end_position.Value.Line; i++) {
                            line = File.Lines[i];
                            var number = (i + 1).ToString().PadRight(number_width + 1, ' ');
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write($"{number}│");
                            Console.ForegroundColor = defaultColour;
                            Console.Write($" {line}\n");
                        }
                    }
                    for (int i = 1; i <= contextLines; i++)
                        if (endline + i < File.Lines.Length) print_line(endline + i);
                    print_empty(true, '╵');
                }

                // Body
                foreach (var note in Notes) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("note");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(": " + note);
                }
                foreach (var help in Help) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("help");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(": " + help);
                }
                Console.WriteLine("");
            }

            /// <summary> Print an exception in a nice way. </summary>
            /// <param name="e">The exception</param>
            public static void PrintException(Exception e, bool last = true) {
                if (e is RunTimeException rte) {
                    rte.ErrorMessage.Print();
                    if (rte.InnerException != null) PrintRawException(rte.InnerException);
                } else {
                    PrintRawException(e);
                }

                if (last) {
                    var defaultColour = Console.ForegroundColor;
                    Console.WriteLine("Please include this entire error if you open a bug report.");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("Stitch version: ");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                    Console.ForegroundColor = defaultColour;
                    Console.Write("Open bug reports here: ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("https://github.com/snijderlab/stitch/issues/new");
                    Console.ForegroundColor = defaultColour;
                }
            }

            private static void PrintRawException(Exception e) {
                ProgressBar.Off = true;
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n>> Error: {e.Message}");

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" --> Stacktrace:");
                Console.ForegroundColor = defaultColour;

                foreach (var l in e.StackTrace.Split(" at ")) {
                    var line = l.Trim();
                    if (String.IsNullOrWhiteSpace(line)) continue;

                    // Regex to match stacktrace lines. path.<name>?function[generic]?(arguments) in path\name:line number
                    var full_pieces = Regex.Match(line, @"^\s*(.+\.)((?:<.+>)?)([_|\w\d]+)((?:\[.+\])?)\((.*)\)(?: in (.+\\)([^\\]+):line (.+))?\s*(--- End of stack trace from previous location ---)?\s*$");
                    if (full_pieces.Success) {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(full_pieces.Groups[6].Length > 0 ? "╷ " : "| ");
                        Console.ForegroundColor = defaultColour;
                        Console.Write(full_pieces.Groups[1]);
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        if (full_pieces.Groups[2].Length > 0) {
                            Console.Write("<");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write(full_pieces.Groups[2].Value.Substring(1, full_pieces.Groups[2].Length - 2));
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            Console.Write(">");
                        }
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(full_pieces.Groups[3]);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(full_pieces.Groups[4]);
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write("(");
                        Console.ForegroundColor = defaultColour;
                        if (full_pieces.Groups[5].Length > 30)
                            Console.Write("...");
                        else
                            Console.Write(full_pieces.Groups[5]);
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write(")");
                        if (full_pieces.Groups[6].Length > 0) {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write("\n╰─ ");
                            var path_pieces = full_pieces.Groups[6].Value.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToList();
                            var index = path_pieces.IndexOf("stitch");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(string.Join(Path.DirectorySeparatorChar, path_pieces.Take(index)) + Path.DirectorySeparatorChar);
                            Console.ForegroundColor = defaultColour;
                            Console.Write(string.Join(Path.DirectorySeparatorChar, path_pieces.Skip(index)) + Path.DirectorySeparatorChar);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write(full_pieces.Groups[7]);
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(":line ");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(full_pieces.Groups[8]);
                        }
                        if (full_pieces.Groups[9].Length > 0) {
                            Console.Write("\n");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(" --> End of stacktrace from previous location.");
                            Console.ForegroundColor = defaultColour;
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("| ");
                        Console.ForegroundColor = defaultColour;
                        Console.Write(line.Trim());
                    }
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine("");
                }
                Console.ForegroundColor = defaultColour;
            }
        }
    }
}