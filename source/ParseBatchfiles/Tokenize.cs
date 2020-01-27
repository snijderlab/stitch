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
    /// <summary>
    /// To keep track of a location of a token
    /// </summary>
    public class Position
    {
        public readonly int Line;
        public readonly int Column;
        public Position(int l, int c)
        {
            Line = l;
            Column = c;
        }
        public override string ToString()
        {
            return $"Position:{Line},{Column}";
        }
    }

    namespace InputNameSpace
    {
        public static class Tokenizer
        {
            public static List<KeyValue> Tokenize(string content)
            {
                var parsed = new List<KeyValue>();
                var counter = new Counter();

                while (content.Length > 0)
                {
                    var outcome = TokenizeHelper.MainArgument(content, counter);
                    if (outcome.Item1 != null) parsed.Add(outcome.Item1);
                    content = outcome.Item2;
                }
                return parsed;
            }
            /// <summary>
            /// To keep track of the location of the parsehead
            /// </summary>
            public class Counter
            {
                public int Line;
                public int Column;

                public Counter()
                {
                    Line = 0;
                    Column = 1;
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
                    return new Position(Line, Column);
                }
            }

            /// <summary>
            /// A class with functionality for tokenizing
            /// </summary>
            static class TokenizeHelper
            {
                /// <summary>
                /// Parse a single 'line' in the batchfile consisting of a single argument, possibly with comments and newlines 
                /// </summary>
                /// <returns>The status</returns>
                public static (KeyValue, string) MainArgument(string content, Counter counter)
                {
                    switch (content.First())
                    {
                        case '-':
                            // This line is a comment, skip it
                            ParseHelper.SkipLine(ref content, counter);
                            return (null, content);
                        case '\n':
                            //skip
                            ParseHelper.Trim(ref content, counter);
                            return (null, content);
                        default:
                            return Argument(content, counter);
                    }
                }
                /// <summary>
                /// Parse a single 'line' in the batchfile consisting of a single argument, without comments and newlines 
                /// </summary>
                /// <returns>The status</returns>
                static (KeyValue, string) Argument(string content, Counter counter)
                {
                    // This is a parameter line, get the name
                    ParseHelper.Trim(ref content, counter);
                    var pos = counter.GetPosition();
                    var name = ParseHelper.Name(ref content, counter);

                    // Find if it is a single or multiple valued parameter
                    if (content[0] == ':' && content[1] == '>')
                    {
                        return MultilineSingleParameter(content, name, counter, pos);
                    }
                    else if (content[0] == ':')
                    {
                        return SingleParameter(content, name, counter, pos);
                    }
                    else if (content[0] == '-' && content[1] == '>')
                    {
                        return MultiParameter(content, name, counter, pos);
                    }
                    else
                    {
                        throw new ParseException($"Parameter '{name.ToString()}' {pos} should be followed by an delimiter (':', ':>' or '->')");
                    }
                }
                /// <summary>
                /// Single parameter on a single line
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) SingleParameter(string content, string name, Counter counter, Position pos)
                {
                    content = content.Remove(0, 1);
                    counter.NextColumn();
                    ParseHelper.Trim(ref content, counter);
                    //Get the single value of the parameter
                    string value = ParseHelper.Value(ref content, counter);
                    return (new KeyValue(name, value, pos), content);
                }
                /// <summary>
                /// Single parameter on multiple lines
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) MultilineSingleParameter(string content, string name, Counter counter, Position pos)
                {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    //Get the single value of the parameter
                    string value = ParseHelper.UntilSequence(ref content, "<:");
                    return (new KeyValue(name, value.Trim(), pos), content);
                }
                /// <summary>
                /// Multiparameter
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) MultiParameter(string content, string name, Counter counter, Position pos)
                {
                    content = content.Remove(0, 2);
                    counter.NextColumn(2);
                    ParseHelper.Trim(ref content, counter);
                    // Now get the multiple values
                    var values = new List<KeyValue>();

                    while (true)
                    {
                        if (content[0] == '<' && content[1] == '-')
                        {
                            // This is the end of the multiple valued parameter
                            content = content.Remove(0, 2);
                            counter.NextColumn(2);
                            ParseHelper.Trim(ref content, counter);
                            return (new KeyValue(name, values, pos), content);
                        }
                        else
                        {
                            // Match the inner parameter
                            var innerpos = counter.GetPosition();
                            var innername = ParseHelper.Name(ref content, counter);

                            // Find if it is a single line or multiple line valued inner parameter
                            if (content[0] == ':' && content[1] == '>')
                            {
                                var outcome = MultilineSingleParameter(content, innername, counter, innerpos);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == ':')
                            {
                                var outcome = SingleParameter(content, innername, counter, innerpos);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-' && content[1] == '>')
                            {
                                var outcome = MultiParameter(content, innername, counter, innerpos);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-')
                            {
                                ParseHelper.SkipLine(ref content, counter);
                            }
                            else
                            {
                                throw new ParseException($"Parameter '{innername}' in '{name}' {counter.GetPosition()} should be followed by an delimiter (':', ':>' or '->')");
                            }
                            ParseHelper.Trim(ref content, counter);
                        }
                    }
                }
            }
            /// <summary>
            /// A class with helper functionality for parsing
            /// </summary>
            static class ParseHelper
            {
                /// <summary>
                /// Consumes a whole line of the string
                /// </summary>
                /// <param name="content">The string</param>
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
                /// To find the next newline, this needs to be written by hand instead of using "String.IndexOf()" because that gives weird behavior in .NET Core
                /// </summary>
                /// <param name="content">The string to search in</param>
                /// <returns>The position of the next newline ('\n' or '\r') or -1 if none could be found</returns>
                public static int FindNextNewLine(ref string content)
                {
                    for (int pos = 0; pos < content.Length; pos++)
                    {
                        if (content[pos] == '\n' || content[pos] == '\r')
                        {
                            return pos;
                        }
                    }
                    return -1;
                }
                /// <summary>
                /// Consumes a name from the start of the string
                /// </summary>
                /// <param name="content">The string</param>
                /// <returns>The name</returns>
                public static string Name(ref string content, Counter counter)
                {
                    ParseHelper.Trim(ref content, counter);
                    var name = new StringBuilder();
                    while (Char.IsLetterOrDigit(content[0]) || content[0] == ' ')
                    {
                        name.Append(content[0]);
                        content = content.Remove(0, 1);
                        counter.NextColumn();
                    }
                    ParseHelper.Trim(ref content, counter);
                    return name.ToString().ToLower().Trim();
                }
                /// <summary>
                /// Consumes a value from the start of the string
                /// </summary>
                /// <param name="content">The string</param>
                /// <returns>The value</returns>
                public static string Value(ref string content, Counter counter)
                {
                    string result = "";
                    int nextnewline = FindNextNewLine(ref content);
                    if (nextnewline > 0)
                    {
                        result = content.Substring(0, nextnewline).Trim();
                        content = content.Remove(0, nextnewline);
                        Trim(ref content, counter);
                    }
                    else
                    {
                        result = content.Trim();
                        content = "";
                    }
                    return result;
                }
                public static void Trim(ref string content, Counter counter)
                {
                    while (content != "" && Char.IsWhiteSpace(content[0]))
                    {
                        if (content[0] == '\n') counter.NextLine();
                        else counter.NextColumn();
                        content = content.Remove(0, 1);
                    }
                }
                /// <summary>
                /// Consumes the string until it find the sequence
                /// </summary>
                /// <param name="content">The string</param>
                /// <param name="sequence">The sequence to find</param>
                /// <returns>The consumed part of the string</returns>
                public static string UntilSequence(ref string content, string sequence)
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
                    }

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
        }
    }
}