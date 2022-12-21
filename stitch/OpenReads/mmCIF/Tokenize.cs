using System;
using System.Collections.Generic;
using System.Text;
using static Stitch.InputNameSpace.Tokenizer;

namespace Stitch {
    namespace MMCIFNameSpace {
        public static class MMCIFTokenizer {
            /// <summary> Tokenize the given file into the custom key value file format. </summary>
            /// <param name="file">The file to tokenize. </param>
            /// <returns> If everything went smoothly a list with all top level key value pairs, otherwise a list of error messages. </returns>
            public static ParseResult<DataBlock> Tokenize(ParsedFile file) {
                var counter = new Counter(file);
                var content = string.Join("\n", file.Lines);

                TokenizeHelper.TrimCommentsAndWhitespace(ref content, counter);
                return TokenizeHelper.ParseDataBlock(ref content, counter);
            }

            /// <summary> A class with functionality for tokenizing. </summary>
            static class TokenizeHelper {
                public static ParseResult<DataBlock> ParseDataBlock(ref string content, Counter counter) {
                    if (!StartsWith(ref content, counter, "data_"))
                        return new ParseResult<DataBlock>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Data Block not opened", "A data block should be opened with 'data_'."));
                    var block = new DataBlock();
                    block.Name = ParseIdentifier(ref content, counter);
                    while (content.Length > 0) {
                        TrimCommentsAndWhitespace(ref content, counter);
                        var item = ParseDataItemOrSaveFrame(ref content, counter);
                        if (item.IsOk())
                            block.Items.Add(item.Unwrap());
                        else
                            return new ParseResult<DataBlock>(item.Messages);
                    }
                    return new ParseResult<DataBlock>(block);
                }

                public static ParseResult<Item> ParseDataItemOrSaveFrame(ref string content, Counter counter) {
                    var start = counter.GetPosition();
                    if (StartsWith(ref content, counter, "save_")) {
                        var frame = new SaveFrame();
                        frame.Name = ParseIdentifier(ref content, counter);
                        while (true) {
                            var item = ParseDataItem(ref content, counter);
                            if (item.IsOk()) frame.Items.Add(item.Unwrap());
                            else break;
                        }
                        if (StartsWith(ref content, counter, "save_")) {
                            return new ParseResult<Item>(frame);
                        } else {
                            return new ParseResult<Item>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "SaveFrame was not closed", "No matching 'save_' could be found, this is necessary to properly close the SaveFrame."));
                        }
                    } else {
                        return ParseDataItem(ref content, counter).Map(v => (Item)v);
                    }
                }
                public static ParseResult<DataItem> ParseDataItem(ref string content, Counter counter) {
                    TrimCommentsAndWhitespace(ref content, counter);
                    var start = counter.GetPosition();
                    if (StartsWith(ref content, counter, "loop_")) {
                        var loop = new Loop();
                        var temp_values = new List<Value>();
                        TrimCommentsAndWhitespace(ref content, counter);
                        while (content.StartsWith('_')) {
                            loop.Header.Add(ParseIdentifier(ref content, counter));
                            TrimCommentsAndWhitespace(ref content, counter);
                        }
                        while (true) {
                            var value = ParseValue(ref content, counter);
                            if (value.IsOk()) temp_values.Add(value.Unwrap());
                            else break;
                        }
                        var columns = loop.Header.Count;
                        if (temp_values.Count % columns == 0) {
                            for (int i = 0; i < temp_values.Count / columns; i++) {
                                loop.Data.Add(temp_values.SubList(i * columns, columns));
                            }
                        } else {
                            return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Loop with incorrect number of data items", $"A loop should have a number of data items which is divisible by the number of columns, but here there are {temp_values.Count % columns} values left."));
                        }
                        return new ParseResult<DataItem>(loop);
                    } else if (StartsWith(ref content, counter, "_")) {
                        var name = ParseIdentifier(ref content, counter);
                        var value = ParseValue(ref content, counter);
                        if (value.IsOk()) return new ParseResult<DataItem>(new Single(name, value.Unwrap()));
                        else return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "No valid value", "A single data item should contain a value."));
                    } else {
                        return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "No valid data item", "A data item should be a tag with a value or a loop."));
                    }
                }
                public static ParseResult<Value> ParseValue(ref string content, Counter counter) {
                    TrimCommentsAndWhitespace(ref content, counter);
                    var start = counter.GetPosition();
                    if (content.Length == 0) return new ParseResult<Value>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Empty value", "No text left when expecting a value."));
                    else if (content.StartsWith("global_") || content.StartsWith("data_") || content.StartsWith("loop_") || content.StartsWith("save_") || content.StartsWith("stop_"))
                        return new ParseResult<Value>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Use of reserved word", "global_, data_, loop_, save_, and stop_ are reserved words."));
                    else if (content.StartsWith('.')) {
                        content = content.Substring(1);
                        counter.NextColumn();
                        return new ParseResult<Value>(new Inapplicable()); // Technically it could also be the start of a number.
                    } else if (content.StartsWith('?')) {
                        content = content.Substring(1);
                        counter.NextColumn();
                        return new ParseResult<Value>(new Unknown());
                    } else if (content.StartsWith('\'')) {
                        return ParseEnclosed(ref content, counter, '\'').Map<Value>(s => new Text(s));
                    } else if (content.StartsWith('\"')) {
                        return ParseEnclosed(ref content, counter, '\"').Map<Value>(s => new Text(s));
                    } else if (content.StartsWith(';')) {
                        return ParseMultilineString(ref content, counter).Map<Value>(s => new Text(s));
                    } else if (IsOrdinary(content[0])) {
                        var num_start = new Counter(counter);
                        var text = ParseIdentifier(ref content, counter);
                        var number = ParseNumeric(text, num_start);
                        if (number.IsOk())
                            return number;
                        else
                            return new ParseResult<Value>(new Text(ParseIdentifier(ref content, counter)));
                    } else {
                        return new ParseResult<Value>(new InputNameSpace.ErrorMessage(start, "Invalid value", "No valid value could be found here."));
                    }
                }

                public static string ParseIdentifier(ref string content, Counter counter) {
                    var to_remove = 0;
                    foreach (char c in content) {
                        if (char.IsWhiteSpace(c)) {
                            var identifier1 = content.Substring(0, to_remove);
                            content = content.Substring(to_remove);
                            counter.NextColumn(to_remove);
                            return identifier1;
                        }
                        to_remove++;
                    }
                    var identifier = content.Substring(0, to_remove);
                    content = content.Substring(to_remove);
                    counter.NextColumn(to_remove);
                    return identifier;
                }

                public static ParseResult<Value> ParseNumeric(string content, Counter counter) {
                    var to_remove = 0;

                    // Get number
                    while (to_remove < content.Length && (char.IsDigit(content[to_remove]) || ".+-eE".Contains(content[to_remove]))) {
                        to_remove += 1;
                    }
                    var number = 0.0;
                    if (!double.TryParse(content.Substring(0, to_remove), out number)) {
                        return new ParseResult<Value>(new InputNameSpace.ErrorMessage(new FileRange(counter.GetPosition(), new Position(counter.Line, counter.Column + to_remove, counter.File)), "Invalid number", "Not a number, but a number was expected."));
                    }

                    var uncertainty = new Option<uint>();
                    if (content.Length > to_remove && content[to_remove] == '(') {
                        to_remove++;
                        var uncertainty_length = 0;
                        while (content[to_remove + uncertainty_length] != ')') uncertainty_length++;
                        uint uncertainty_number = 0;
                        if (!uint.TryParse(content.Substring(to_remove, uncertainty_length), out uncertainty_number)) {
                            return new ParseResult<Value>(new InputNameSpace.ErrorMessage(new FileRange(new Position(counter.Line, counter.Column + to_remove, counter.File), new Position(counter.Line, counter.Column + to_remove + uncertainty_length, counter.File)), "Invalid uncertainty number", "Not a number, but a number was expected."));
                        }
                        uncertainty = new Option<uint>(uncertainty_number);
                        to_remove++;
                    }

                    counter.NextColumn(to_remove);
                    content = content.Substring(to_remove);
                    return new ParseResult<Value>(uncertainty.Map<Value>(u => new NumericWithUncertainty(number, u), () => new Numeric(number)));
                }
                public static void TrimCommentsAndWhitespace(ref string content, Counter counter) {
                    while (true) {
                        Trim(ref content, counter);
                        if (content.Length == 0) return;
                        if (content.StartsWith('#')) SkipLine(ref content, counter);
                        else return;
                    }
                }
                public static ParseResult<string> ParseMultilineString(ref string content, Counter counter) {
                    var to_remove = 1;
                    var eol = false;
                    var start = counter.GetPosition();
                    foreach (char c in content) {
                        if (eol && c == ';') {
                            var trimmed = content.Substring(1, to_remove);
                            content = content.Substring(to_remove + 1);
                            counter.NextColumn();
                            return new ParseResult<string>(trimmed);
                        } else if (c == '\n') {
                            counter.NextLine();
                            to_remove += 1;
                            eol = true;
                        } else {
                            to_remove += 1;
                            counter.NextColumn();
                            eol = false;
                        }
                    }
                    return new ParseResult<string>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Multiline string not closed", "A multiline string should be closed by '<eol>;'"));
                }

                public static ParseResult<string> ParseEnclosed(ref string content, Counter counter, char pattern) {
                    var to_remove = 1; // first is pattern
                    var start = counter.GetPosition();
                    foreach (char c in content) {
                        if (c == pattern) {
                            var trimmed = content.Substring(1, to_remove);
                            to_remove++;
                            counter.NextColumn(to_remove);
                            content = content.Substring(to_remove);
                            return new ParseResult<string>(trimmed);
                        } else if (c == '\n' || c == '\r') {
                            counter.NextColumn(to_remove);
                            content = content.Substring(to_remove);
                            return new ParseResult<string>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Invalid enclosing", $"The element was enclosed by {pattern} but the closing delimiter was not found on the same line."));
                        }
                        to_remove++;
                    }
                    counter.NextColumn(to_remove);
                    content = content.Substring(to_remove);
                    return new ParseResult<string>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Invalid enclosing", $"The element was enclosed by {pattern} but the closing delimiter was not found before the end of the file."));
                }

                public static bool StartsWith(ref string content, Counter counter, string pattern) {
                    if (content.ToLower().StartsWith(pattern)) {
                        content = content.Substring(pattern.Length);
                        counter.NextColumn(pattern.Length);
                        return true;
                    } else {
                        return false;
                    }
                }

                /// Test if the character is an ordinary character, one which can start a line in a multiline string
                public static bool IsOrdinary(char c) {
                    if ("#$\'\"_[]; \t".Contains(c)) return false;
                    // rust is_ascii_graphic: Checks if the value is an ASCII graphic character: U+0021 '!' ..= U+007E '~'
                    return (int)c >= 0x21 && (int)c <= 0x7E;
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