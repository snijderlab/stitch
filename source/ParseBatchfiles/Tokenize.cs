using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyNameSpace
{
    namespace InputNameSpace
    {
        public static class Tokenizer
        {
            public static List<KeyValue> Tokenize(ParsedFile file)
            {
                var parsed = new List<KeyValue>();
                var counter = new Counter(file);
                var content = string.Join("\n", file.Lines);

                while (content.Length > 0)
                {
                    var outcome = TokenizeHelper.MainArgument(content, counter);
                    if (outcome.Item1 != null) parsed.Add(outcome.Item1);
                    content = outcome.Item2;
                }
                return parsed;
            }

            /// <summary>
            /// To keep track of the location of the parsehead.
            /// </summary>
            public class Counter
            {
                public int Line;
                public int Column;
                public ParsedFile File;

                public Counter(ParsedFile file)
                {
                    Line = 0;
                    Column = 1;
                    File = file;
                }

                public Counter(Position pos)
                {
                    Line = pos.Line;
                    Column = pos.Column;
                    File = pos.File;
                }

                public void NextLine()
                {
                    Line += 1;
                    Column = 1;
                }

                public void NextColumn(int steps = 1)
                {
                    Column += steps;
                }

                public Position GetPosition()
                {
                    return new Position(Line, Column, File);
                }
            }

            /// <summary>
            /// A class with functionality for tokenizing.
            /// </summary>
            static class TokenizeHelper
            {
                /// <summary>
                /// Parse a single 'line' in the batchfile consisting of a single argument, possibly with comments and newlines.
                /// </summary>
                /// <returns>The status.</returns>
                public static (KeyValue, string) MainArgument(string content, Counter counter)
                {
                    ParseHelper.Trim(ref content, counter);

                    if (content[0] == '-')
                    {
                        // This line is a comment, skip it
                        ParseHelper.SkipLine(ref content, counter);
                        return (null, content);
                    }
                    else
                    {
                        return Argument(content, counter);
                    }
                }

                /// <summary>
                /// Parse a single 'line' in the batchfile consisting of a single argument, without comments and newlines.
                /// </summary>
                /// <returns>The status.</returns>
                static (KeyValue, string) Argument(string content, Counter counter)
                {
                    // This is a parameter line, get the name
                    ParseHelper.Trim(ref content, counter);
                    (string name, FileRange range) = ParseHelper.Name(ref content, counter);

                    // Find if it is a single or multiple valued parameter
                    if (content[0] == ':' && content[1] == '>')
                    {
                        return MultilineSingleParameter(content, name, counter, range);
                    }
                    else if (content[0] == ':')
                    {
                        return SingleParameter(content, name, counter, range);
                    }
                    else if (content[0] == '-' && content[1] == '>')
                    {
                        return MultiParameter(content, name, counter, range);
                    }
                    else
                    {
                        new ErrorMessage(range, "Name not followed by delimeter", "", "A name was read, but thereafter a value should be provided starting with a delimeter ':', ':>' or '->'. Or if the statement was meant as a comment a hyphen '-' should start the line.").Print();
                        throw new ParseException("");
                    }
                }

                /// <summary>
                /// Single parameter on a single line.
                /// </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static (KeyValue, string) SingleParameter(string content, string name, Counter counter, FileRange namerange)
                {
                    content = content.Remove(0, 1);
                    counter.NextColumn();
                    ParseHelper.Trim(ref content, counter);
                    //Get the single value of the parameter
                    (string value, FileRange range) = ParseHelper.Value(ref content, counter);
                    return (new KeyValue(name, value, new KeyRange(namerange, counter.GetPosition()), range), content);
                }

                /// <summary>
                /// Single parameter on multiple lines.
                /// </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static (KeyValue, string) MultilineSingleParameter(string content, string name, Counter counter, FileRange namerange)
                {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    Position startvalue = counter.GetPosition();
                    //Get the single value of the parameter
                    string value = ParseHelper.UntilSequence(ref content, "<:", counter);
                    Position endkey = counter.GetPosition();
                    Position endvalue = new Position(endkey.Line, endkey.Column - 2, counter.File);
                    return (new KeyValue(name, value.Trim(), new KeyRange(namerange, endkey), new FileRange(startvalue, endvalue)), content);
                }

                /// <summary>
                /// Multiparameter.
                /// </summary>
                /// <param name="content">The string to be parsed.</param>
                /// <param name="name">The name of the parameter.</param>
                /// <returns>The status.</returns>
                static (KeyValue, string) MultiParameter(string content, string name, Counter counter, FileRange namerange)
                {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    Position startvalue = counter.GetPosition();
                    Position endvalue = counter.GetPosition();
                    // Now get the multiple values
                    var values = new List<KeyValue>();

                    while (true)
                    {
                        if (content.Length == 0)
                        {
                            new ErrorMessage(new FileRange(namerange.Start, counter.GetPosition()), "Could not find the end of the multiparameter", "", "Make sure to end the parameter with '<-'.").Print();
                            throw new ParseException("");
                        }
                        if (content[0] == '<' && content[1] == '-')
                        {
                            // This is the end of the multiple valued parameter
                            content = content.Remove(0, 2);
                            counter.NextColumn(2);
                            Position endkey = counter.GetPosition();
                            ParseHelper.Trim(ref content, counter);
                            return (new KeyValue(name, values, new KeyRange(namerange, endkey), new FileRange(startvalue, endvalue)), content);
                        }
                        else
                        {
                            ParseHelper.Trim(ref content, counter);
                            if (content[0] == '-' && content[1] != '>')
                            {
                                ParseHelper.SkipLine(ref content, counter);
                                continue;
                            }

                            // Match the inner parameter
                            (string innername, FileRange innerrange) = ParseHelper.Name(ref content, counter);

                            // Find if it is a single line or multiple line valued inner parameter
                            if (content[0] == ':' && content[1] == '>')
                            {
                                var outcome = MultilineSingleParameter(content, innername, counter, innerrange);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == ':')
                            {
                                var outcome = SingleParameter(content, innername, counter, innerrange);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-' && content[1] == '>')
                            {
                                var outcome = MultiParameter(content, innername, counter, innerrange);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-')
                            {
                                ParseHelper.SkipLine(ref content, counter);
                            }
                            else
                            {
                                new ErrorMessage(innerrange, "Name not followed by delimeter", "", "A name was read, but thereafter a value should be provided starting with a delimeter ':', ':>' or '->'. Or if the statement was meant as a comment a hyphen '-' should start the line.").Print();
                                throw new ParseException("");
                            }
                            endvalue = counter.GetPosition();
                            ParseHelper.Trim(ref content, counter);
                        }
                    }
                }
            }

            /// <summary>
            /// A class with helper functionality for parsing.
            /// </summary>
            public static class ParseHelper
            {
                /// <summary>
                /// Consumes a whole line of the string.
                /// </summary>
                /// <param name="content">The string.</param>
                public static void SkipLine(ref string content, Counter counter)
                {
                    int nextnewline = FindNextNewLine(ref content);

                    if (nextnewline > 0)
                    {
                        content = content.Remove(0, nextnewline);
                        Trim(ref content, counter);
                    }
                    else
                    {
                        content = "";
                    }
                }

                /// <summary>
                /// To find the next newline, this needs to be written by hand instead of using "String.IndexOf()" because that gives weird behavior in .NET Core.
                /// </summary>
                /// <param name="content">The string to search in.</param>
                /// <returns>The position of the next newline '\n' or -1 if none could be found.</returns>
                public static int FindNextNewLine(ref string content)
                {
                    for (int pos = 0; pos < content.Length; pos++)
                    {
                        if (content[pos] == '\n')
                        {
                            return pos;
                        }
                    }
                    return -1;
                }

                /// <summary>
                /// Consumes a name from the start of the string.
                /// </summary>
                /// <param name="content">The string.</param>
                /// <returns>The name.</returns>
                public static (string, FileRange) Name(ref string content, Counter counter)
                {
                    ParseHelper.Trim(ref content, counter);
                    Position start = counter.GetPosition();
                    var name = new StringBuilder();

                    int count = 0;
                    while (count < content.Length && (Char.IsLetterOrDigit(content[count]) || content[count] == ' ' || content[count] == '\t'))
                    {
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

                    if (name_str == "")
                    {
                        new ErrorMessage(name_range, "Empty name", "").Print();
                        //    throw new ParseException("");
                    }

                    return (name_str, name_range);
                }

                /// <summary>
                /// Consumes a value from the start of the string.
                /// </summary>
                /// <param name="content">The string and range.</param>
                /// <returns>The value.</returns>
                public static (string, FileRange) Value(ref string content, Counter counter)
                {
                    string result;
                    Position start = counter.GetPosition();
                    Position end; int nextnewline = FindNextNewLine(ref content);

                    if (nextnewline > 0)
                    {
                        result = content.Substring(0, nextnewline).TrimEnd(); // Save whitespace in front to make position work
                        content = content.Remove(0, nextnewline);
                        end = new Position(start.Line, start.Column + result.Length, counter.File);
                        result = result.TrimStart(); // Remove whitespace in front
                        Trim(ref content, counter);
                    }
                    else
                    {
                        result = content.TrimEnd(); // Save whitespace in front to make position work
                        content = "";
                        end = new Position(start.Line, start.Column + result.Length, counter.File);
                        result = result.TrimStart(); // Remove whitespace in front
                    }

                    return (result, new FileRange(start, end));
                }
                public static void Trim(ref string content, Counter counter)
                {
                    int count = 0;
                    while (count < content.Length && Char.IsWhiteSpace(content[count]))
                    {
                        if (content[count] == '\n') counter.NextLine();
                        else counter.NextColumn();
                        count++;
                    }
                    content = content.Remove(0, count);
                }

                /// <summary>
                /// Consumes the string until it find the sequence.
                /// </summary>
                /// <param name="content">The string.</param>
                /// <param name="sequence">The sequence to find.</param>
                /// <param name="counter">The counter to keep track of the location</param>
                /// <returns>The consumed part of the string.</returns>
                public static string UntilSequence(ref string content, string sequence, Counter counter)
                {
                    int nextnewline = -1;
                    bool found = false;
                    var contentarray = content.ToCharArray();
                    for (int pos = 0; pos <= contentarray.Length - sequence.Length && !found; pos++)
                    {
                        for (int offset = 0; offset <= sequence.Length; offset++)
                        {
                            if (offset == sequence.Length)
                            {
                                nextnewline = pos;
                                found = true;
                                break;
                            }
                            if (contentarray[pos + offset] != sequence[offset])
                            {
                                break;
                            }
                        }
                        // Not equal on this position so adjust counter
                        if (contentarray[pos] == '\n') counter.NextLine();
                        else counter.NextColumn();
                    }

                    string value;
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
                /// Consumes the string until it find one of the sequences
                /// </summary>
                /// <param name="content">The string, assumed not to have newlines before the first occurrence of a sequence</param>
                /// <param name="sequence">The sequences to find</param>
                /// <returns>The consumed part of the string</returns>
                public static string UntilOneOf(ref string content, char[] sequence, Counter counter)
                {
                    var dict = new HashSet<char>(sequence);
                    var pos = -1;
                    for (int i = 0; i < content.Length; i++)
                    {
                        if (dict.Contains(content[i]))
                        {
                            pos = i;
                            break;
                        }
                    }

                    if (pos == -1)
                    {
                        counter.NextColumn(content.Length);
                        var backup = content;
                        content = "";
                        return backup;
                    }
                    else
                    {
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