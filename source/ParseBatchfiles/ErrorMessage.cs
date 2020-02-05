using System;

namespace AssemblyNameSpace
{
    namespace InputNameSpace
    {
        public class ErrorMessage
        {
            Position startposition = new Position(0, 1, new ParsedFile());
            Position endposition = new Position(0, 1, new ParsedFile());
            ParsedFile File;
            string shortDescription = "";
            string longDescription = "";
            string helpDescription = "";
            string subject = "";
            public bool Warning { get; private set; }
            public ErrorMessage(string sub, string shortD, string longD = "", string help = "")
            {
                subject = sub;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = false;
                File = new ParsedFile();
            }
            public ErrorMessage(Position pos, string shortD, string longD = "", string help = "")
            {
                startposition = pos;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = false;
                File = pos.File;
            }
            public ErrorMessage(Range range, string shortD, string longD = "", string help = "")
            {
                startposition = range.Start;
                endposition = range.End;
                shortDescription = shortD;
                longDescription = longD;
                helpDescription = help;
                Warning = false;
                File = range.File;
            }
            public static ErrorMessage DuplicateValue(Range range)
            {
                var output = new ErrorMessage(range, "Duplicate parameter definition", "A value for this property was already defined.");
                output.Warning = true;
                return output;
            }
            public static ErrorMessage MissingParameter(Range range, string parameter)
            {
                return new ErrorMessage(range, $"Missing parameter: {parameter}");
            }
            public static ErrorMessage UnknownKey(Range range, string context, string options)
            {
                return new ErrorMessage(range, "Unknown key", $"Unknown key in {context} definition", $"Valid options are: {options}");
            }
            public override string ToString()
            {
                // Header
                var name = Warning ? "Warning" : "Error";
                var header = $">> {name}: {shortDescription}\n";

                // Location
                string location = "";
                if (subject != "")
                {
                    location = $"\n   | {subject}\n\n";
                }
                else if (File.Filename == "")
                {
                    location = "";
                }
                else if (endposition == new Position(0, 1, new ParsedFile()))
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + "^^^";
                    location = $"File: {File.Filename}\n{start}\n{line_number} | {line}\n{start}{pos}\n{start}\n";
                }
                else if (startposition.Line == endposition.Line)
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + new string('^', endposition.Column - startposition.Column);
                    location = $"File: {File.Filename}\n{start}\n{line_number} | {line}\n{start}{pos}\n{start}\n";
                }
                else
                {
                    var line_number = endposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    location = $"File: {File.Filename}\n{start}\n";

                    for (int i = startposition.Line; i <= endposition.Line; i++)
                    {
                        var line = File.Lines[i];
                        var number = i.ToString().PadRight(line_number.Length + 1);
                        location += $"{number}| {line}\n";
                    }
                    location += $"{start}\n";
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
                var primary_colour = Warning ? ConsoleColor.Blue : ConsoleColor.Red;

                // Header
                Console.ForegroundColor = primary_colour;
                var name = Warning ? "Warning" : "Error";
                Console.WriteLine($">> {name}: {shortDescription}");
                Console.ForegroundColor = defaultColour;

                // Location
                if (subject != "")
                {
                    Console.WriteLine($"\n   | {subject}\n");
                }
                else if (File.Filename == "")
                {
                }
                else if (endposition == new Position(0, 1, new ParsedFile()))
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + "^^^";
                    Console.Write($"File: {File.Filename}\n{start}\n{line_number} | {line}\n{start}");
                    Console.ForegroundColor = primary_colour;
                    Console.Write(pos);
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"\n{start}\n");
                }
                else if (startposition.Line == endposition.Line)
                {
                    var line_number = startposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    var line = File.Lines[startposition.Line];
                    var pos = new string(' ', startposition.Column - 1) + new string('^', endposition.Column - startposition.Column);
                    Console.Write($"File: {File.Filename}\n{start}\n{line_number} | {line}\n{start}");
                    Console.ForegroundColor = primary_colour;
                    Console.Write(pos);
                    Console.ForegroundColor = defaultColour;
                    Console.Write($"\n{start}\n");
                }
                else
                {
                    var line_number = endposition.Line.ToString();
                    var spacing = new string(' ', line_number.Length + 1);
                    var start = $"{spacing}| ";
                    Console.Write($"File: {File.Filename}\n{start}\n");

                    for (int i = startposition.Line; i <= endposition.Line; i++)
                    {
                        var line = File.Lines[i];
                        var number = i.ToString().PadRight(line_number.Length + 1);
                        Console.Write($"{number}| {line}\n");
                    }
                    Console.Write($"{start}\n");
                }

                // Body
                if (longDescription != "")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(longDescription);
                    Console.ForegroundColor = defaultColour;
                }
                if (helpDescription != "") Console.WriteLine(helpDescription);
                Console.WriteLine("");
            }
        }
    }
}