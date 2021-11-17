using System;
using System.Text;

namespace AssemblyNameSpace
{
    namespace InputNameSpace
    {
        public class ErrorMessage
        {
            readonly Position? startposition;
            readonly Position? endposition;
            readonly ParsedFile File;
            readonly string shortDescription = "";
            readonly string longDescription = "";
            readonly string helpDescription = "";
            readonly string subject = "";
            readonly uint contextLines = 0;
            public bool Warning { get; private set; }
            public ErrorMessage(string sub, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 1)
            {
                subject = sub;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = new ParsedFile();
                this.contextLines = contextLines;
            }
            public ErrorMessage(ParsedFile file, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 1)
            {
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = file;
                this.contextLines = contextLines;
            }
            public ErrorMessage(Position pos, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 1)
            {
                startposition = pos;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = pos.File;
                this.contextLines = contextLines;
            }
            public ErrorMessage(FileRange range, string shortD, string longD = "", string help = "", bool warning = false, uint contextLines = 1)
            {
                startposition = range.Start;
                endposition = range.End;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = range.File;
                this.contextLines = contextLines;
            }
            public static ErrorMessage DuplicateValue(FileRange range)
            {
                var output = new ErrorMessage(range, "Duplicate parameter definition", "A value for this property was already defined.", "", true, 2);
                return output;
            }
            public static ErrorMessage MissingParameter(FileRange range, string parameter)
            {
                return new ErrorMessage(range, $"Missing parameter: {parameter}", "", "", false, 0);
            }
            public static ErrorMessage UnknownKey(FileRange range, string context, string options)
            {
                return new ErrorMessage(range, "Unknown key", $"Unknown key in {context} definition.", $"Valid options are: {options}.", false, 0);
            }
            public override string ToString()
            {
                // Header
                var name = Warning ? "Warning" : "Error";
                var header = $">> {name}: {shortDescription}\n";

                // Location
                string location;
                if (!string.IsNullOrEmpty(subject))
                {
                    location = $"\n   | {subject}\n\n";
                }
                else if (string.IsNullOrEmpty(File.Filename))
                {
                    location = "";
                }
                else if (!startposition.HasValue)
                {
                    location = $"File: {File.Filename}\n";
                }
                else if (!endposition.HasValue)
                {
                    var line_number = (startposition.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Value.Line];
                    var pos = new string(' ', startposition.Value.Column - 1) + "^^^";
                    var context1 = startposition.Value.Line > 1 ? $"{start}{File.Lines[startposition.Value.Line - 1]}\n" : "";
                    var context2 = startposition.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[startposition.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                }
                else if (startposition.Value.Line == endposition.Value.Line)
                {
                    var line_number = (startposition.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Value.Line];
                    var pos = new string(' ', Math.Max(0, startposition.Value.Column - 1)) + new string('^', Math.Max(1, endposition.Value.Column - startposition.Value.Column));
                    var context1 = startposition.Value.Line > 1 ? $"{start}{File.Lines[startposition.Value.Line - 1]}\n" : "";
                    var context2 = endposition.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[endposition.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                }
                else
                {
                    var line_number = (endposition.Value.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var context1 = startposition.Value.Line > 1 ? $"{start}{File.Lines[startposition.Value.Line - 1]}\n" : "";
                    var context2 = endposition.Value.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[endposition.Value.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}";

                    for (int i = startposition.Value.Line; i <= endposition.Value.Line; i++)
                    {
                        var line = File.Lines[i];
                        var number = (i + 1).ToString().PadRight(line_number.Length + 1);
                        location += $"{number}| {line}\n";
                    }
                    location += $"{context2}\n";
                }

                // Body
                var body = "";
                if (!string.IsNullOrEmpty(longDescription)) body += longDescription + "\n";
                if (!string.IsNullOrEmpty(helpDescription)) body += helpDescription + "\n";

                return header + location + body;
            }
            public void Print()
            {
                var defaultColour = Console.ForegroundColor;

                // Header
                Console.ForegroundColor = Warning ? ConsoleColor.Blue : ConsoleColor.Red;
                var name = Warning ? "Warning" : "Error";
                Console.WriteLine($">> {name}: {shortDescription}");
                Console.ForegroundColor = defaultColour;

                // Location
                if (!string.IsNullOrEmpty(subject)) // Pregiven location
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("\n   | ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write(subject + "\n");
                }
                else if (string.IsNullOrEmpty(File.Filename)) // No location
                {
                }
                else if (!startposition.HasValue) // Only a file
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("  --> ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"{File.Filename}\n");
                }
                else // A location in a file
                {
                    var endline = !endposition.HasValue ? startposition.Value.Line : endposition.Value.Line;
                    var number_width = endline < File.Lines.Length - 1 ? (endline + 1).ToString().Length : (startposition.Value.Line + 1).ToString().Length;
                    var line_number = (startposition.Value.Line + 1).ToString().PadRight(number_width + 1, ' ');
                    var spacing = new string(' ', number_width + 3);
                    var line = File.Lines[startposition.Value.Line];

                    void print_line(int lineindex)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write((lineindex + 1).ToString().PadRight(number_width + 1, ' '));
                        Console.Write("| ");
                        Console.ForegroundColor = defaultColour;
                        Console.Write(File.Lines[lineindex]);
                        Console.Write("\n");
                    }

                    void print_empty(bool newline)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(new string(' ', number_width + 1));
                        Console.Write("|");
                        Console.ForegroundColor = defaultColour;
                        if (newline) Console.Write('\n');
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("  --> ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"{File.Filename}:{startposition.Value.Line + 1}:{startposition.Value.Column + 1}\n");
                    print_empty(true);

                    for (int i = (int)contextLines; i > 0; i--)
                        if (startposition.Value.Line - i > 0) print_line(startposition.Value.Line - i);

                    if (!endposition.HasValue || startposition.Value.Line > endposition.Value.Line) // Single position
                    {
                        var pos = new string(' ', startposition.Value.Column) + "^^^";
                        print_line(startposition.Value.Line);
                        print_empty(false);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    }
                    else if (startposition.Value.Line == endposition.Value.Line) // Single line
                    {
                        var pos = new string(' ', Math.Max(0, startposition.Value.Column)) + new string('^', Math.Max(1, endposition.Value.Column - startposition.Value.Column));
                        print_line(startposition.Value.Line);
                        print_empty(false);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    }
                    else // Multiline
                    {
                        for (int i = startposition.Value.Line; i <= endposition.Value.Line; i++)
                        {
                            line = File.Lines[i];
                            var number = (i + 1).ToString().PadRight(number_width + 1, ' ');
                            Console.Write($"{number}| {line}\n");
                        }
                    }
                    for (int i = 1; i <= contextLines; i++)
                        if (endline + i < File.Lines.Length) print_line(endline + i);
                    print_empty(true);
                }

                // Body
                if (!string.IsNullOrEmpty(longDescription))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("note");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(": " + longDescription);
                }
                if (!string.IsNullOrEmpty(helpDescription))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("help");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(": " + helpDescription);
                }
                Console.WriteLine("");
            }

            public void OutputForLanguageServer()
            {
                var buffer = new StringBuilder();
                var name = Warning ? "Warning" : "Error";
                buffer.Append($"{name}");

                // Location
                if (!string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(File.Filename) || !startposition.HasValue) // Only a file
                {
                    buffer.Append($"\t1 1");
                }
                else if (!endposition.HasValue || startposition.Value.Line > endposition.Value.Line) // Single position
                {
                    buffer.Append($"\t{startposition.Value.Line + 1} {startposition.Value.Column}");
                }
                else if (startposition.Value.Line == endposition.Value.Line) // Single line
                {
                    buffer.Append($"\t{startposition.Value.Line + 1} {startposition.Value.Column} {endposition.Value.Column}");
                }
                else // Multiline
                {
                    buffer.Append($"\t{startposition.Value.Line + 1} {startposition.Value.Column} {endposition.Value.Line + 1} {endposition.Value.Column}");
                }

                buffer.Append($"\t{shortDescription}\t{longDescription.Replace('\n', ' ')}\t{helpDescription.Replace('\n', ' ')}");

                Console.WriteLine(buffer);
            }
        }
    }
}