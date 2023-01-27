using System.Collections.Generic;
using System.Linq;
using Stitch.InputNameSpace;

// Lex/tokenize a CIF file into its constituent parts.

namespace Stitch {
    namespace ParseMzTab {
        public struct IntermediateRepresentation {
            public (SubString, SubString)[] MetaData;
            public SubString[] ProteinSectionHeader;
            public SubString[,] ProteinSectionData;

            /// <summary> Tokenize the given file into the custom key value file format. </summary>
            /// <param name="file">The file to tokenize. </param>
            /// <returns> If everything went smoothly a list with all top level key value pairs, otherwise a list of error messages. </returns>
            public static ParseResult<IntermediateRepresentation> Lex(ParsedFile file) {
                var pointer = new InputNameSpace.Tokenizer.Counter(file);
                var aggregator = new IntermediateRepresentation();
                var outEither = new ParseResult<IntermediateRepresentation>(aggregator);
                var metaData = new List<(SubString, SubString)>();
                var header = new SubString[0];
                var tableData = new List<SubString[]>();

                foreach (var line in file.Lines) {
                    if (line.Length != 0) {
                        var keyword = line.Split('\t', 2)[0];
                        var pos = pointer.GetPosition();
                        var point = new FileRange(new Position(pos.Line, 1, pos.File), new Position(pos.Line, 4, pos.File));
                        switch (keyword) {
                            case "MTD":
                                var data = SubString.Split(line, '\t', pointer.GetPosition(), 3);
                                metaData.Add((data[1], data[2]));
                                break;
                            case "PSH":
                                if (header.Length != 0) {
                                    outEither.AddMessage(new ErrorMessage(point, "Cannot have multiple table headers", "PSH lines can only occur once in an mzTab document"));
                                }
                                header = SubString.Split(line, '\t', pointer.GetPosition()).ToArray();
                                break;
                            case "PSM":
                                if (header.Length == 0) {
                                    outEither.AddMessage(new ErrorMessage(point, "Missing PSH line", "A header line (PSH) for this PSM table must be placed before the PSM table lines."));
                                    return outEither;
                                }
                                var values = SubString.Split(line, '\t', pointer.GetPosition());
                                if (values.Count != header.Length) {
                                    var f_pos = pointer.GetPosition();
                                    outEither.AddMessage(new ErrorMessage(point, "Incorrect data line", $"Table line has incorrect number of columns, expected {header.Length} found {values.Count}."));
                                } else {
                                    tableData.Add(values.ToArray());
                                }
                                break;
                            case "PRH":
                            case "PRT":
                            case "PEH":
                            case "PET":
                            case "SMH":
                            case "SML":
                            case "COM":
                                //ignore
                                break;
                            default:
                                outEither.AddMessage(new ErrorMessage(point, "Unrecognised mzTab line start code", "mzTab files can only start with the following codes: 'MTD', 'PSH', 'PSM', 'PRH', 'PRT', 'PEH', 'PET', 'SMH', 'SML', 'COM'"));
                                break;
                        }

                    }
                    pointer.NextLine();
                }
                return outEither;
            }
        }

        public struct SubString {
            public string Content;
            public FileRange Location;

            public SubString(string content, FileRange location) {
                Content = content;
                Location = location;
            }

            public static List<SubString> Split(string line, char pattern, Position pos, int max_occurrence = -1) {
                var output = new List<SubString>();
                var i_start = 0;
                var i_end = 0;
                max_occurrence -= 1; // Adjust for the element it always makes at the end
                for (var i = 0; i < line.Length; i++) {
                    var c = line[i];
                    if (c == pattern && max_occurrence != 0) {
                        max_occurrence -= 1; // Just assume we have less than 2 * 2 ^ 32 fields I guess, there is no saturating sub in C#, meaning that after going beyond the int.MinValue it will count from int.MaxValue to 0 and then stop.
                        if (i_end - i_start > 0) {
                            output.Add(new SubString(line.Substring(i_start, i_end), new FileRange(new Position(pos.Line, i_start, pos.File), new Position(pos.Line, i_end, pos.File))));
                            i_start = i + 1;
                            i_end = i + 1;
                        } else {
                            output.Add(new SubString("", new FileRange(new Position(pos.Line, i_start, pos.File), new Position(pos.Line, System.Math.Max(i_start, i_end), pos.File))));
                            i_start = i + 1;
                            i_end = i + 1;
                        }
                    } else if (char.IsWhiteSpace(c) && i_end == i_start) {
                        i_start++;
                        i_end++;
                    } else if (char.IsWhiteSpace(c)) {
                        // Do not update anything is some text follows later the i_end will be updated
                    } else {
                        i_end++;
                    }
                }
                if (i_end - i_start > 0) {
                    output.Add(new SubString(line.Substring(i_start, i_end), new FileRange(new Position(pos.Line, i_start, pos.File), new Position(pos.Line, i_end, pos.File))));
                }
                return output;
            }
        }
    }
}