using System;
using System.Collections.Generic;
using Stitch.MMCIFItems;

namespace Stitch {
    public class MMCIFParser {
        int index;
        readonly string content;
        readonly InputNameSpace.Tokenizer.Counter counter;

        MMCIFParser(ParsedFile file) {
            index = 0;
            content = string.Join("\n", file.Lines);
            counter = new InputNameSpace.Tokenizer.Counter(file);
        }

        /// <summary> Tokenize the given file into the custom key value file format. </summary>
        /// <param name="file">The file to tokenize. </param>
        /// <returns> If everything went smoothly a list with all top level key value pairs, otherwise a list of error messages. </returns>
        public static ParseResult<DataBlock> Parse(ParsedFile file) {
            var pointer = new MMCIFParser(file);

            pointer.TrimCommentsAndWhitespace();
            return pointer.ParseDataBlock();
        }

        ParseResult<DataBlock> ParseDataBlock() {
            if (!StartsWith("data_"))
                return new ParseResult<DataBlock>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Data Block not opened", "A data block should be opened with 'data_'."));
            var block = new DataBlock();
            block.Name = ParseIdentifier();
            while (index != content.Length) {
                TrimCommentsAndWhitespace();
                var item = ParseDataItemOrSaveFrame();
                if (item.IsOk())
                    block.Items.Add(item.Unwrap());
                else if (index != content.Length)
                    return new ParseResult<DataBlock>(item.Messages);
            }
            return new ParseResult<DataBlock>(block);
        }

        ParseResult<Item> ParseDataItemOrSaveFrame() {
            var start = counter.GetPosition();
            if (StartsWith("save_")) {
                var frame = new SaveFrame();
                frame.Name = ParseIdentifier();
                while (true) {
                    var item = ParseDataItem();
                    if (item.IsOk()) frame.Items.Add(item.Unwrap());
                    else break;
                }
                if (StartsWith("save_")) {
                    return new ParseResult<Item>(frame);
                } else {
                    return new ParseResult<Item>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "SaveFrame was not closed", "No matching 'save_' could be found, this is necessary to properly close the SaveFrame."));
                }
            } else {
                return ParseDataItem().Map(v => (Item)v);
            }
        }
        ParseResult<DataItem> ParseDataItem() {
            TrimCommentsAndWhitespace();
            var start = counter.GetPosition();
            if (StartsWith("loop_")) {
                var loop = new Loop();
                var temp_values = new List<Value>();
                TrimCommentsAndWhitespace();
                while (StartsWith('_')) {
                    loop.Header.Add(ParseIdentifier());
                    TrimCommentsAndWhitespace();
                }
                while (true) {
                    var value = ParseValue();
                    if (value.IsOk()) temp_values.Add(value.Unwrap());
                    else break;
                }
                var temp_values_ar = temp_values.ToArray();
                var columns = loop.Header.Count;
                if (temp_values_ar.Length % columns == 0) {
                    for (int i = 0; i < temp_values_ar.Length / columns; i++) {
                        loop.Data.Add(temp_values_ar.SubArray(i * columns, columns));
                    }
                } else {
                    return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Loop with incorrect number of data items", $"A loop should have a number of data items which is divisible by the number of columns, but here there are {temp_values.Count % columns} values left."));
                }
                return new ParseResult<DataItem>(loop);
            } else if (StartsWith("_")) {
                var name = ParseIdentifier();
                var value = ParseValue();
                if (value.IsOk()) return new ParseResult<DataItem>(new SingleItem(name, value.Unwrap()));
                else return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "No valid value", "A single data item should contain a value."));
            } else {
                return new ParseResult<DataItem>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "No valid data item", "A data item should be a tag with a value or a loop."));
            }
        }
        ParseResult<Value> ParseValue() {
            TrimCommentsAndWhitespace();
            var start = counter.GetPosition();
            if (index == content.Length) return new ParseResult<Value>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Empty value", "No text left when expecting a value."));
            else if (StartsWith("global_", false)
                    || StartsWith("data_", false)
                    || StartsWith("loop_", false)
                    || StartsWith("save_", false)
                    || StartsWith("stop_", false))
                return new ParseResult<Value>(new InputNameSpace.ErrorMessage(counter.GetPosition(), "Use of reserved word", "global_, data_, loop_, save_, and stop_ are reserved words."));
            else if (StartsWith('.')) {
                index += 1;
                counter.NextColumn();
                return new ParseResult<Value>(new Inapplicable()); // Technically it could also be the start of a number.
            } else if (StartsWith('?')) {
                index += 1;
                counter.NextColumn();
                return new ParseResult<Value>(new Unknown());
            } else if (StartsWith('\'')) {
                return ParseEnclosed('\'').Map<Value>(s => new Text(s));
            } else if (StartsWith('\"')) {
                return ParseEnclosed('\"').Map<Value>(s => new Text(s));
            } else if (StartsWith(';')) {
                return ParseMultilineString().Map<Value>(s => new Text(s));
            } else if (IsOrdinary(content[index])) {
                var num_start = new InputNameSpace.Tokenizer.Counter(counter);
                var text = ParseIdentifier();
                var number = ParseNumeric(text, num_start);
                if (number.IsOk())
                    return number;
                else
                    return new ParseResult<Value>(new Text(text));
            } else {
                return new ParseResult<Value>(new InputNameSpace.ErrorMessage(start, "Invalid value", "No valid value could be found here."));
            }
        }

        string ParseIdentifier() {
            var to_remove = 0;
            for (var i = index; i < content.Length; i++) {
                var c = content[i];
                if (char.IsWhiteSpace(c)) {
                    var identifier1 = content.Substring(index, to_remove);
                    index += to_remove;
                    counter.NextColumn(to_remove);
                    return identifier1;
                }
                to_remove++;
            }
            var identifier = content.Substring(index, to_remove);
            index += to_remove;
            counter.NextColumn(to_remove);
            return identifier;
        }

        ParseResult<Value> ParseNumeric(string content, InputNameSpace.Tokenizer.Counter counter) {
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
        void TrimCommentsAndWhitespace() {
            while (true) {
                Trim();
                if (index == content.Length) return;
                if (StartsWith('#')) SkipLine();
                else return;
            }
        }
        ParseResult<string> ParseMultilineString() {
            var to_remove = 1;
            var eol = false;
            var start = counter.GetPosition();
            for (var i = index; i < content.Length; i++) {
                var c = content[i];
                if (eol && c == ';') {
                    var trimmed = content.Substring(index + 1, to_remove);
                    index += to_remove + 1;
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

        ParseResult<string> ParseEnclosed(char pattern) {
            var to_remove = 1; // first is pattern
            var start = counter.GetPosition();
            for (var i = index; i < content.Length; i++) {
                var c = content[i];
                if (c == pattern) {
                    var trimmed = content.Substring(index + 1, to_remove);
                    to_remove++;
                    counter.NextColumn(to_remove);
                    index += to_remove;
                    return new ParseResult<string>(trimmed);
                } else if (c == '\n' || c == '\r') {
                    counter.NextColumn(to_remove);
                    index += to_remove;
                    return new ParseResult<string>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Invalid enclosing", $"The element was enclosed by {pattern} but the closing delimiter was not found on the same line."));
                }
                to_remove++;
            }
            counter.NextColumn(to_remove);
            index += to_remove;
            return new ParseResult<string>(new InputNameSpace.ErrorMessage(new FileRange(start, counter.GetPosition()), "Invalid enclosing", $"The element was enclosed by {pattern} but the closing delimiter was not found before the end of the file."));
        }

        bool StartsWith(string pattern, bool consume = true) {
            var found = false;
            for (int offset = 0; offset <= pattern.Length && index + offset < content.Length; offset++) {
                if (offset == pattern.Length) {
                    found = true;
                    break;
                }
                if (content[index + offset] != pattern[offset]) {
                    break;
                }
            }
            if (found) {
                if (consume) {
                    index += pattern.Length;
                    counter.NextColumn(pattern.Length);
                }
                return true;
            } else {
                return false;
            }
        }

        bool StartsWith(char pattern) {
            if (content[index] == pattern) {
                index += 1;
                counter.NextColumn();
                return true;
            } else {
                return false;
            }
        }

        /// Test if the character is an ordinary character, one which can start a line in a multiline string
        static bool IsOrdinary(char c) {
            if ("#$\'\"_[]; \t".Contains(c)) return false;
            // rust is_ascii_graphic: Checks if the value is an ASCII graphic character: U+0021 '!' ..= U+007E '~'
            return (int)c >= 0x21 && (int)c <= 0x7E;
        }


        void Trim() {
            int count;
            for (count = index; count < content.Length && Char.IsWhiteSpace(content[count]); count++) {
                if (content[count] == '\n') counter.NextLine();
                else counter.NextColumn();
            }
            index = count;
        }

        /// <summary> Consumes a whole line of the string. </summary>
        /// <param name="content">The string.</param>
        void SkipLine() {
            int next_newline = FindNextNewLine();

            if (next_newline > 0) {
                index = next_newline;
                Trim();
            } else {
                index = content.Length;
            }
        }

        /// <summary> To find the next newline, this needs to be written by hand instead of using "String.IndexOf()" because that gives weird behavior in .NET Core. </summary>
        /// <param name="content">The string to search in.</param>
        /// <returns>The position of the next newline '\n' or -1 if none could be found.</returns>
        int FindNextNewLine() {
            for (int pos = index; pos < content.Length; pos++) {
                if (content[pos] == '\n') {
                    return pos;
                }
            }
            return -1;
        }
    }
}