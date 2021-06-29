using System;
using System.Text;

namespace AssemblyNameSpace
{
    namespace InputNameSpace
    {
        public class ErrorMessage
        {
            readonly Position startposition;
            readonly Position endposition;
            readonly ParsedFile File;
            readonly string shortDescription = "";
            readonly string longDescription = "";
            readonly string helpDescription = "";
            readonly string subject = "";
            readonly uint contextLines = 0;
            public bool Warning { get; private set; }
            public ErrorMessage(string sub, string shortD, string longD = "", string help = "", bool warning = false, uint context_lines = 1)
            {
                subject = sub;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = new ParsedFile();
                contextLines = context_lines;
            }
            public ErrorMessage(ParsedFile file, string shortD, string longD = "", string help = "", bool warning = false, uint context_lines = 1)
            {
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = file;
                contextLines = context_lines;
            }
            public ErrorMessage(Position pos, string shortD, string longD = "", string help = "", bool warning = false, uint context_lines = 1)
            {
                startposition = pos;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = pos.File;
                contextLines = context_lines;
            }
            public ErrorMessage(FileRange range, string shortD, string longD = "", string help = "", bool warning = false, uint context_lines = 1)
            {
                startposition = range.Start;
                endposition = range.End;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = warning;
                File = range.File;
                contextLines = context_lines;
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
                if (subject != "")
                {
                    location = $"\n   | {subject}\n\n";
                }
                else if (File.Filename == "")
                {
                    location = "";
                }
                else if (startposition == null)
                {
                    location = $"File: {File.Filename}\n";
                }
                else if (endposition == null)
                {
                    var line_number = (startposition.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + "^^^";
                    var context1 = startposition.Line > 1 ? $"{start}{File.Lines[startposition.Line - 1]}\n" : "";
                    var context2 = startposition.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[startposition.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                }
                else if (startposition.Line == endposition.Line)
                {
                    var line_number = (startposition.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', Math.Max(0, startposition.Column - 1)) + new string('^', Math.Max(1, endposition.Column - startposition.Column));
                    var context1 = startposition.Line > 1 ? $"{start}{File.Lines[startposition.Line - 1]}\n" : "";
                    var context2 = endposition.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[endposition.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}{line_number} | {line}\n{start}{pos}\n{context2}\n";
                }
                else
                {
                    var line_number = (endposition.Line + 1).ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var context1 = startposition.Line > 1 ? $"{start}{File.Lines[startposition.Line - 1]}\n" : "";
                    var context2 = endposition.Line < File.Lines.Length - 1 ? $"{start}{File.Lines[endposition.Line + 1]}\n" : "";
                    location = $"File: {File.Filename}\n\n{context1}";

                    for (int i = startposition.Line; i <= endposition.Line; i++)
                    {
                        var line = File.Lines[i];
                        var number = (i + 1).ToString().PadRight(line_number.Length + 1);
                        location += $"{number}| {line}\n";
                    }
                    location += $"{context2}\n";
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
                Console.ForegroundColor = Warning ? ConsoleColor.Blue : ConsoleColor.Red;
                var name = Warning ? "Warning" : "Error";
                Console.WriteLine($">> {name}: {shortDescription}");
                Console.ForegroundColor = defaultColour;

                // Location
                if (subject != "") // Pregiven location
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("\n   | ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write(subject + "\n");
                }
                else if (File.Filename == "") // No location
                {
                }
                else if (startposition == null) // Only a file
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("  --> ");
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"{File.Filename}\n");
                }
                else // A location in a file
                {
                    var endline = endposition == null ? startposition.Line : endposition.Line;
                    var number_width = endline < File.Lines.Length - 1 ? (endline + 1).ToString().Length : (startposition.Line + 1).ToString().Length;
                    var line_number = (startposition.Line + 1).ToString().PadRight(number_width + 1, ' ');
                    var spacing = new string(' ', number_width + 3);
                    var line = File.Lines[startposition.Line];

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
                    Console.Write($"{File.Filename}:{startposition.Line + 1}:{startposition.Column + 1}\n");
                    print_empty(true);

                    for (int i = (int)contextLines; i > 0; i--)
                        if (startposition.Line - i > 0) print_line(startposition.Line - i);

                    if (endposition == null || startposition.Line > endposition.Line) // Single position
                    {
                        var pos = new string(' ', startposition.Column) + "^^^";
                        print_line(startposition.Line);
                        print_empty(false);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    }
                    else if (startposition.Line == endposition.Line) // Single line
                    {
                        var pos = new string(' ', Math.Max(0, startposition.Column)) + new string('^', Math.Max(1, endposition.Column - startposition.Column));
                        print_line(startposition.Line);
                        print_empty(false);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(pos + "\n");
                        Console.ForegroundColor = defaultColour;
                    }
                    else // Multiline
                    {
                        for (int i = startposition.Line; i <= endposition.Line; i++)
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
                if (longDescription != "")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("note");
                    Console.ForegroundColor = defaultColour;
                    Console.WriteLine(": " + longDescription);
                }
                if (helpDescription != "")
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
                if (subject != "" || File.Filename == "" || startposition == null) // Only a file
                {
                    buffer.Append($"\t1 1");
                }
                else if (endposition == null || startposition.Line > endposition.Line) // Single position
                {
                    buffer.Append($"\t{startposition.Line + 1} {startposition.Column}");
                }
                else if (startposition.Line == endposition.Line) // Single line
                {
                    buffer.Append($"\t{startposition.Line + 1} {startposition.Column} {endposition.Column}");
                }
                else // Multiline
                {
                    buffer.Append($"\t{startposition.Line + 1} {startposition.Column} {endposition.Line + 1} {endposition.Column}");
                }

                buffer.Append($"\t{shortDescription}\t{longDescription.Replace('\n', ' ')}\t{helpDescription.Replace('\n', ' ')}");

                Console.WriteLine(buffer);
            }
        }
    }
}