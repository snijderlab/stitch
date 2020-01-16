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
    namespace InputNameSpace
    {
        public static class Tokenizer
        {
            public static List<KeyValue> Tokenize(string content)
            {
                var parsed = new List<KeyValue>();
                while (content.Length > 0)
                {
                    var outcome = TokenizeHelper.MainArgument(content);
                    if (outcome.Item1 != null) parsed.Add(outcome.Item1);
                    content = outcome.Item2;
                }
                return parsed;
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
                public static (KeyValue, string) MainArgument(string content)
                {
                    switch (content.First())
                    {
                        case '-':
                            // This line is a comment, skip it
                            ParseHelper.SkipLine(ref content);
                            return (null, content);
                        case '\n':
                            //skip
                            return (null, content.Trim());
                        default:
                            return Argument(content);
                    }
                }
                /// <summary>
                /// Parse a single 'line' in the batchfile consisting of a single argument, without comments and newlines 
                /// </summary>
                /// <returns>The status</returns>
                static (KeyValue, string) Argument(string content)
                {
                    // This is a parameter line, get the name
                    var name = ParseHelper.Name(ref content);

                    // Find if it is a single or multiple valued parameter
                    if (content[0] == ':' && content[1] == '>')
                    {
                        return MultilineSingleParameter(content, name);
                    }
                    else if (content[0] == ':')
                    {
                        return SingleParameter(content, name);
                    }
                    else if (content[0] == '-' && content[1] == '>')
                    {
                        return MultiParameter(content, name);
                    }
                    else
                    {
                        throw new ParseException($"Parameter {name.ToString()} should be followed by an delimiter (':', ':>' or '->')");
                    }
                }
                /// <summary>
                /// Single parameter on a single line
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) SingleParameter(string content, string name)
                {
                    content = content.Remove(0, 1).Trim();
                    //Get the single value of the parameter
                    string value = ParseHelper.Value(ref content);
                    return (new KeyValue(name, value), content);
                }
                /// <summary>
                /// Single parameter on multiple lines
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) MultilineSingleParameter(string content, string name)
                {
                    content = content.Remove(0, 2).Trim();
                    //Get the single value of the parameter
                    string value = ParseHelper.UntilSequence(ref content, "<:");
                    return (new KeyValue(name, value.Trim()), content);
                }
                /// <summary>
                /// Multiparameter
                /// </summary>
                /// <param name="content">The string to be parsed</param>
                /// <param name="name">The name of the parameter</param>
                /// <returns>The status</returns>
                static (KeyValue, string) MultiParameter(string content, string name)
                {
                    content = content.Remove(0, 2).Trim();
                    // Now get the multiple values
                    var values = new List<KeyValue>();

                    while (true)
                    {
                        if (content[0] == '<' && content[1] == '-')
                        {
                            // This is the end of the multiple valued parameter
                            content = content.Remove(0, 2).Trim();
                            return (new KeyValue(name, values), content);
                        }
                        else
                        {
                            // Match the inner parameter
                            var innername = ParseHelper.Name(ref content);

                            // Find if it is a single line or multiple line valued inner parameter
                            if (content[0] == ':' && content[1] == '>')
                            {
                                var outcome = MultilineSingleParameter(content, innername);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == ':')
                            {
                                var outcome = SingleParameter(content, innername);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-' && content[1] == '>')
                            {
                                var outcome = MultiParameter(content, innername);
                                values.Add(outcome.Item1);
                                content = outcome.Item2;
                            }
                            else if (content[0] == '-')
                            {
                                ParseHelper.SkipLine(ref content);
                            }
                            else
                            {
                                throw new ParseException($"Parameter {innername} in {name} should be followed by an delimiter (':', ':>' or '->')");
                            }
                            content = content.Trim();
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
                public static void SkipLine(ref string content)
                {
                    int nextnewline = FindNextNewLine(ref content);
                    if (nextnewline > 0)
                    {
                        content = content.Remove(0, nextnewline).Trim();
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
                /// <summary>
                /// Consumes a value from the start of the string
                /// </summary>
                /// <param name="content">The string</param>
                /// <returns>The value</returns>
                public static string Value(ref string content)
                {
                    string result = "";
                    int nextnewline = FindNextNewLine(ref content);
                    if (nextnewline > 0)
                    {
                        result = content.Substring(0, nextnewline).Trim();
                        content = content.Remove(0, nextnewline).Trim();
                    }
                    else
                    {
                        result = content.Trim();
                        content = "";
                    }
                    return result;
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