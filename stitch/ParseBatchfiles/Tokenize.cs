using System;
using System.Collections.Generic;
using System.Text;

namespace Stitch {
    namespace InputNameSpace {
        public static class Tokenizer {
            /// <summary> Tokenize the given file into the custom key value file format. </summary>
            /// <param name="file">The file to tokenize. </param>
            /// <returns> If everything went smoothly a list with all top level key value pairs, otherwise a list of error messages. </returns>
            public static ParseResult<List<KeyValue>> Tokenize(ParsedFile file) {
                var output = new ParseResult<List<KeyValue>>();
                var parsed = new List<KeyValue>();
                output.Value = parsed;
                var counter = new Counter(file);
                var content = string.Join("\n", file.Lines);

                while (content.Length > 0) {
                    var outcome = TokenizeHelper.MainArgument(content, counter);
                    if (outcome.IsOk(output)) {
                        if (outcome.Unwrap().Item1 != null) parsed.Add(outcome.Unwrap().Item1);
                        content = outcome.Unwrap().Item2;
                    } else {
                        return output;
                    }
                }
                return output;
            }

            /// <summary> To keep track of the location of the parse head. </summary>
            public class Counter {
                /// <summary> The current line number (0-based). </summary>
                public int Line;

                /// <summary> The current column number (1-based). </summary>
                public int Column;

                /// <summary> The file the parse head is located at. </summary>
                public ParsedFile File;

                /// <summary> Create a new counter at the start of the given file. </summary>
                /// <param name="file"> The file to keep track of the position in. </param>
                public Counter(ParsedFile file) {
                    Line = 0;
                    Column = 1;
                    File = file;
                }

                /// <summary> Create a new counter at the given position. </summary>
                /// <param name="pos"> The starting position and file for this counter. </param>
                public Counter(Position pos) {
                    Line = pos.Line;
                    Column = pos.Column;
                    File = pos.File;
                }

                /// <summary> Create a new counter at the given position. </summary>
                /// <param name="counter"> The starting position and file for this counter. </param>
                public Counter(Counter counter) {
                    Line = counter.Line;
                    Column = counter.Column;
                    File = counter.File;
                }

                /// <summary> Go to the next line, set all numbers correctly. </summary>
                public void NextLine() {
                    Line += 1;
                    Column = 1;
                }

                /// <summary> Go forward the given number of characters. </summary>
                /// <param name="steps"> The number of character to go forward, defaults to 1. </param>
                public void NextColumn(int steps = 1) {
                    Column += steps;
                }

                /// <summary> Get the current position of the counter. </summary>
                /// <returns> The position as a Position. </returns>
                public Position GetPosition() {
                    return new Position(Line, Column, File);
                }
            }

            /// <summary> A class with functionality for tokenizing. </summary>
            static class TokenizeHelper {
                /// <summary> Parse a single 'line' in the batchfile consisting of a single argument, possibly with comments and newlines. </summary>
                /// <returns>The status.</returns>
                public static ParseResult<(KeyValue, string)> MainArgument(string content, Counter counter) {
                    ParseHelper.Trim(ref content, counter);

                    if (content[0] == '-') {
                        // This line is a comment, skip it
                        ParseHelper.SkipLine(ref content, counter);
                        return new ParseResult<(KeyValue, string)>((null, content));
                    } else {
                        return Argument(content, counter);
                    }
                }

                /// <summary> Parse a single 'line' in the batchfile consisting of a single argument, without comments and newlines. </summary>
                /// <returns>The status.</returns>
                static ParseResult<(KeyValue, string)> Argument(string content, Counter counter) {
                    // This is a parameter line, get the name
                    ParseHelper.Trim(ref content, counter);
                    var outEither = new ParseResult<(KeyValue, string)>();
                    (string name, FileRange range) = ParseHelper.Name(ref content, counter).UnwrapOrDefault(outEither, ("", new FileRange(counter.GetPosition(), counter.GetPosition())));

                    // Find if it is a single or multiple valued parameter
                    if (content[0] == ':' && content[1] == '>') {
                        outEither.Value = MultilineSingleParameter(content, name, counter, range);
                    } else if (content[0] == ':') {
                        outEither.Value = SingleParameter(content, name, counter, range);
                    } else if (content[0] == '-' && content[1] == '>') {
                        outEither.Value = MultiParameter(content, name, counter, range).UnwrapOrDefault(outEither, new());
                    } else if (content[0] == '<' && content[1] == '-') {
                        return new ParseResult<(KeyValue, string)>(new ErrorMessage(range, "Unmatched closing delimiter", "There is a closing delimiter detected that was not expected.", "Make sure the number of closing delimiters is correct and your groups are opened correctly."));
                    } else {
                        outEither.AddMessage(new ErrorMessage(range, "Name not followed by delimiter", "", "A name was read, but thereafter a value should be provided starting with a delimiter ':', ':>' or '->'. Or if the statement was meant as a comment a hyphen '-' should start the line."));
                    }
                    return outEither;
                }

                /// <summary> Single parameter on a single line. </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static (KeyValue, string) SingleParameter(string content, string name, Counter counter, FileRange name_range) {
                    content = content.Remove(0, 1);
                    counter.NextColumn();
                    ParseHelper.Trim(ref content, counter);
                    //Get the single value of the parameter
                    (string value, FileRange range) = ParseHelper.Value(ref content, counter);
                    return (new KeyValue(name, value, new KeyRange(name_range, counter.GetPosition()), range), content);
                }

                /// <summary> Single parameter on multiple lines. </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static (KeyValue, string) MultilineSingleParameter(string content, string name, Counter counter, FileRange name_range) {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    Position start_value = counter.GetPosition();
                    //Get the single value of the parameter
                    string value = ParseHelper.UntilSequence(ref content, "<:", counter);
                    Position end_key = counter.GetPosition();
                    Position end_value = new Position(end_key.Line, end_key.Column - 2, counter.File);
                    return (new KeyValue(name, value.Trim(), new KeyRange(name_range, end_key), new FileRange(start_value, end_value)), content);
                }

                /// <summary> Multi parameter. </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static ParseResult<(KeyValue, string)> MultiParameter(string content, string name, Counter counter, FileRange name_range) {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    Position start_value = counter.GetPosition();
                    Position end_value = counter.GetPosition();
                    // Now get the multiple values
                    var outEither = new ParseResult<(KeyValue, string)>();
                    var values = new List<KeyValue>();

                    while (true) {
                        if (content.Length == 0) {
                            return new ParseResult<(KeyValue, string)>(new ErrorMessage(new FileRange(name_range.Start, counter.GetPosition()), "Could not find the end of the multi parameter", "", "Make sure to end the parameter with '<-'."));
                        }
                        if (content[0] == '<' && content[1] == '-') {
                            // This is the end of the multiple valued parameter
                            content = content.Remove(0, 2);
                            counter.NextColumn(2);
                            Position end_key = counter.GetPosition();
                            ParseHelper.Trim(ref content, counter);
                            outEither.Value = (new KeyValue(name, values, new KeyRange(name_range, end_key), new FileRange(start_value, end_value)), content);
                            return outEither;
                        } else {
                            ParseHelper.Trim(ref content, counter);
                            if (content[0] == '-' && content[1] != '>') {
                                ParseHelper.SkipLine(ref content, counter);
                                continue;
                            }

                            // Match the inner parameter
                            (string inner_name, FileRange inner_range) = ParseHelper.Name(ref content, counter).UnwrapOrDefault(outEither, new());

                            // Find if it is a single line or multiple line valued inner parameter
                            if (content[0] == ':' && content[1] == '>') {
                                var outcome = MultilineSingleParameter(content, inner_name, counter, inner_range);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            } else if (content[0] == ':') {
                                var outcome = SingleParameter(content, inner_name, counter, inner_range);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            } else if (content[0] == '-' && content[1] == '>') {
                                var outcome = MultiParameter(content, inner_name, counter, inner_range);
                                if (outcome.IsOk(outEither)) {
                                    values.Add(outcome.Unwrap().Item1);
                                    content = outcome.Unwrap().Item2;
                                } else {
                                    return outEither;
                                }
                            } else if (content[0] == '-') {
                                ParseHelper.SkipLine(ref content, counter);
                            } else {
                                return new ParseResult<(KeyValue, string)>(new ErrorMessage(inner_range, "Name not followed by delimiter", "", "A name was read, but thereafter a value should be provided starting with a delimiter ':', ':>' or '->'. Or if the statement was meant as a comment a hyphen '-' should start the line."));
                            }
                            end_value = counter.GetPosition();
                            ParseHelper.Trim(ref content, counter);
                        }
                    }
                }
            }

            /// <summary> A class with helper functionality for parsing. </summary>
            public static class ParseHelper {
                /// <summary> Consumes a whole line of the string. </summary>
                /// <param name="content">The string.</param>
                public static void SkipLine(ref string content, Counter counter) {
                    int next_newline = FindNextNewLine(ref content);

                    if (next_newline > 0) {
                        content = content.Remove(0, next_newline);
                        Trim(ref content, counter);
                    } else {
                        content = "";
                    }
                }

                /// <summary> To find the next newline, this needs to be written by hand instead of using "String.IndexOf()" because that gives weird behavior in .NET Core. </summary>
                /// <param name="content">The string to search in.</param>
                /// <returns>The position of the next newline '\n' or -1 if none could be found.</returns>
                public static int FindNextNewLine(ref string content) {
                    for (int pos = 0; pos < content.Length; pos++) {
                        if (content[pos] == '\n') {
                            return pos;
                        }
                    }
                    return -1;
                }

                /// <summary> Consumes a name from the start of the string. </summary>
                /// <param name="content">The string.</param>
                /// <returns>The name.</returns>
                public static ParseResult<(string, FileRange)> Name(ref string content, Counter counter) {
                    ParseHelper.Trim(ref content, counter);
                    Position start = counter.GetPosition();
                    var name = new StringBuilder();
                    var output = new ParseResult<(string, FileRange)>();

                    int count = 0;
                    while (count < content.Length && (Char.IsLetterOrDigit(content[count]) || content[count] == ' ' || content[count] == '\t')) {
                        name.Append(content[count]);
                        counter.NextColumn();
                        count++;
                    }
                    content = content.Remove(0, count);

                    var name_str = name.ToString();

                    Position greedy = counter.GetPosition();
                    Position end = new Position(greedy.Line, greedy.Column + name_str.TrimEnd().Length - name_str.Length, counter.File);
                    ParseHelper.Trim(ref content, counter);

                    name_str = name_str.Trim();

                    var name_range = new FileRange(start, end);

                    if (string.IsNullOrEmpty(name_str)) {
                        output.AddMessage(new ErrorMessage(name_range, "Empty name", "", "", true));
                    }
                    output.Value = (name_str, name_range);

                    return output;
                }

                /// <summary> Consumes a value from the start of the string. </summary>
                /// <param name="content">The string and range.</param>
                /// <returns>The value.</returns>
                public static (string, FileRange) Value(ref string content, Counter counter) {
                    string result;
                    Position start = counter.GetPosition();
                    Position end; int next_newline = FindNextNewLine(ref content);

                    if (next_newline > 0) {
                        result = content.Substring(0, next_newline).TrimEnd(); // Save whitespace in front to make position work
                        content = content.Remove(0, next_newline);
                        end = new Position(start.Line, start.Column + result.Length, counter.File);
                        result = result.TrimStart(); // Remove whitespace in front
                        Trim(ref content, counter);
                    } else {
                        result = content.TrimEnd(); // Save whitespace in front to make position work
                        content = "";
                        end = new Position(start.Line, start.Column + result.Length, counter.File);
                        result = result.TrimStart(); // Remove whitespace in front
                    }

                    return (result, new FileRange(start, end));
                }
                public static void Trim(ref string content, Counter counter) {
                    int count = 0;
                    while (count < content.Length && Char.IsWhiteSpace(content[count])) {
                        if (content[count] == '\n') counter.NextLine();
                        else counter.NextColumn();
                        count++;
                    }
                    content = content.Remove(0, count);
                }

                /// <summary> Consumes the string until it find the sequence. </summary>
                /// <param name="content">The string.</param>
                /// <param name="sequence">The sequence to find.</param>
                /// <param name="counter">The counter to keep track of the location</param>
                /// <returns>The consumed part of the string.</returns>
                public static string UntilSequence(ref string content, string sequence, Counter counter) {
                    int next_newline = -1;
                    bool found = false;
                    var content_array = content.ToCharArray();
                    for (int pos = 0; pos <= content_array.Length - sequence.Length && !found; pos++) {
                        for (int offset = 0; offset <= sequence.Length; offset++) {
                            if (offset == sequence.Length) {
                                next_newline = pos;
                                found = true;
                                break;
                            }
                            if (content_array[pos + offset] != sequence[offset]) {
                                break;
                            }
                        }
                        // Not equal on this position so adjust counter
                        if (content_array[pos] == '\n') counter.NextLine();
                        else counter.NextColumn();
                    }

                    string value;
                    if (next_newline > 0) {
                        value = content.Substring(0, next_newline).Trim();
                        content = content.Remove(0, next_newline + sequence.Length).Trim();
                    } else {
                        value = content.Trim();
                        content = "";
                    }
                    return value;
                }
                /// <summary> Consumes the string until it find one of the sequences </summary>
                /// <param name="content">The string, assumed not to have newlines before the first occurrence of a sequence</param>
                /// <param name="sequence">The sequences to find</param>
                /// <returns>The consumed part of the string</returns>
                public static string UntilOneOf(ref string content, char[] sequence, Counter counter) {
                    var dict = new HashSet<char>(sequence);
                    var pos = -1;
                    for (int i = 0; i < content.Length; i++) {
                        if (dict.Contains(content[i])) {
                            pos = i;
                            break;
                        }
                    }

                    if (pos == -1) {
                        counter.NextColumn(content.Length);
                        var backup = content;
                        content = "";
                        return backup;
                    } else {
                        var value = content.Substring(0, pos);
                        counter.NextColumn(value.Length);
                        content = content.Remove(0, pos);
                        return value;
                    }
                }
            }
        }
    }
}