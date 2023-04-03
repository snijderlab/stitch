using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;
using static Stitch.Fragmentation;
using static Stitch.HelperFunctionality;

namespace Stitch {
    /// <summary> To contain all logic for the reading of reads out of files. </summary>
    public static class OpenReads {
        /// <summary> To open a file with reads. It assumes a very basic format, namely sequences separated with newlines with the possibility to specify comments as lines starting with a specific character (standard '#'). </summary>
        /// <param name="filter"> The name filter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The file to read from. </param>
        /// <param name="commentChar"> The character comment lines start with. </param>
        /// <returns> A list of all reads found. </returns>
        public static ParseResult<List<ReadFormat.General>> Simple(NameFilter filter, ReadFormat.FileIdentifier inputFile, ScoringMatrix alphabet, char commentChar = '#') {
            var out_either = new ParseResult<List<ReadFormat.General>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possible_content.IsOk(out_either)) {
                var parsed = possible_content.Unwrap();
                var reads = new List<ReadFormat.General>();
                out_either.Value = reads;

                for (int line = 0; line < parsed.Lines.Length; line++) {
                    var content = parsed.Lines[line];
                    if (content.Length == 0) continue;
                    if (content[0] != commentChar) {
                        var range = new FileRange(new Position(line, 0, parsed), new Position(line, 0, parsed));
                        reads.Add(new ReadFormat.Simple(AminoAcid.FromString(content.Trim(), alphabet, range).UnwrapOrDefault(out_either, new AminoAcid[0]), range, filter));
                    }
                }
            }
            return out_either;
        }

        /// <summary> To open a file with reads. the file should be in fasta format so identifiers on a single line starting with '>' followed by an arbitrary number of lines with sequences. Because sometimes programs output the length of a line after every line this is stripped away. </summary>
        /// <param name="filter"> The name filter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The path to the file to read from. </param>
        /// <param name="parseIdentifier"> The regex to determine how to parse the identifier from the fasta header. </param>
        /// <returns> A list of all reads found with their identifiers. </returns>
        public static ParseResult<List<ReadFormat.General>> Fasta(NameFilter filter, ReadFormat.FileIdentifier inputFile, Regex parseIdentifier, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possible_content.IsErr()) {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<ReadFormat.General>();
            out_either.Value = reads;

            var parse_file = possible_content.Unwrap();
            var lines = parse_file.Lines;

            string identifierLine = "";
            int identifierLineNumber = 1;
            var sequence = new StringBuilder();
            int linenumber = 0;
            var start_pos = new Position(linenumber, 0, parse_file);

            foreach (var line in lines) {
                var end_pos = new Position(linenumber, line.Length, parse_file);
                var range = new FileRange(start_pos, end_pos);
                if (line.Length == 0) continue;
                if (line[0] == '>') {
                    if (!string.IsNullOrEmpty(identifierLine)) {
                        var match = parseIdentifier.Match(identifierLine);
                        var identifier_line_range = new FileRange(new Position(identifierLineNumber, 1, parse_file), new Position(identifierLineNumber, identifierLine.Length, parse_file));
                        if (match.Success) {
                            if (match.Groups.Count == 3) {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadFormat.Fasta(null, match.Groups[1].Value, identifierLine, range, filter, match.Groups[2].Value), identifierLineNumber, parse_file, alphabet).UnwrapOrDefault(out_either, null));
                            } else if (match.Groups.Count == 2) {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadFormat.Fasta(null, match.Groups[1].Value, identifierLine, range, filter), identifierLineNumber, parse_file, alphabet).UnwrapOrDefault(out_either, null));
                            } else {
                                out_either.AddMessage(new InputNameSpace.ErrorMessage(
                                    identifier_line_range,
                                    "Identifier Regex has invalid number of capturing groups",
                                    "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                                    )
                                );
                                return out_either;
                            }
                        } else {
                            out_either.AddMessage(new InputNameSpace.ErrorMessage(
                                identifier_line_range,
                                "Header line does not match RegEx",
                                "This header line does not match the RegEx given to parse the identifier."
                                )
                            );
                        }
                    }
                    identifierLine = line.Substring(1).Trim();
                    sequence = new StringBuilder();
                    identifierLineNumber = linenumber;
                    start_pos = end_pos;
                } else {
                    sequence.Append(line.ToArray());
                }
                linenumber++;
            }
            if (!string.IsNullOrEmpty(identifierLine)) {
                var end_pos = new Position(linenumber, identifierLine.Length, parse_file);
                var range = new FileRange(start_pos, end_pos);
                // Flush last sequence to list
                var match = parseIdentifier.Match(identifierLine);
                var identifier_line_range = new FileRange(new Position(identifierLineNumber, 1, parse_file), new Position(identifierLineNumber, identifierLine.Length, parse_file));

                if (match.Success) {
                    if (match.Groups.Count == 3) {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadFormat.Fasta(null, match.Groups[1].Value, identifierLine, range, filter, match.Groups[2].Value), identifierLineNumber, parse_file, alphabet).UnwrapOrDefault(out_either, null));
                    } else if (match.Groups.Count == 2) {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadFormat.Fasta(null, match.Groups[1].Value, identifierLine, range, filter), identifierLineNumber, parse_file, alphabet).UnwrapOrDefault(out_either, null));
                    } else {
                        out_either.AddMessage(new InputNameSpace.ErrorMessage(
                            identifier_line_range,
                            "Identifier Regex has invalid number of capturing groups",
                            "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                            )
                        );
                        return out_either;
                    }
                } else {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(
                        identifier_line_range,
                        "Header line does not match RegEx",
                        "This header line does not match the RegEx given to parse the identifier."
                        )
                    );
                }
            }

            return out_either;
        }

        private static readonly Regex remove_whitespace = new Regex(@"\s+");
        private static string RemoveWhitespace(string input) {
            return remove_whitespace.Replace(input, "");
        }
        private static readonly Regex check_amino_acids = new Regex("[^ACDEFGHIKLMNOPQRSTUVWY]", RegexOptions.IgnoreCase);
        static ParseResult<ReadFormat.General> ParseAnnotatedFasta(string line, ReadFormat.General metaData, int identifier_line_number, ParsedFile file, ScoringMatrix alphabet) {
            var out_either = new ParseResult<ReadFormat.General>();
            var plain_sequence = new StringBuilder();
            var annotated = new List<(HelperFunctionality.Annotation, string)>();
            string current_seq = "";
            int cdr1 = 0;
            int cdr2 = 0;
            int cdr3 = 0;
            for (int i = 0; i < line.Length; i++) {
                if (line[i] == '(') {
                    if (!string.IsNullOrEmpty(current_seq)) {
                        annotated.Add((Annotation.None, current_seq));
                        current_seq = "";
                    }
                    i += 1;
                    int space = line.IndexOf(' ', i);
                    int close = line.IndexOf(')', i);
                    var seq = RemoveWhitespace(line.Substring(space + 1, close - space - 1));
                    var annotation = HelperFunctionality.ParseAnnotation(RemoveWhitespace(line.Substring(i, space - i)));
                    annotated.Add((annotation, seq));
                    plain_sequence.Append(seq);
                    i = close;
                    var annotated_count = annotated.Count;
                    if (annotation.IsAnyCDR()) {
                        if (!(annotated_count >= 3 && annotated[annotated_count - 3].Item1 == annotation && annotated[annotated_count - 2].Item1 == Annotation.PossibleGlycan)) {
                            if (annotation == Annotation.CDR1) {
                                cdr1 += 1;
                            } else if (annotation == Annotation.CDR2) {
                                cdr2 += 1;
                            } else if (annotation == Annotation.CDR3) {
                                cdr3 += 1;
                            }
                        }
                    }
                } else if (!Char.IsWhiteSpace(line[i])) {
                    current_seq += line[i];
                    plain_sequence.Append(line[i]);
                }
            }
            if (!string.IsNullOrEmpty(current_seq.Trim()))
                annotated.Add((Annotation.None, current_seq));
            var sequence = plain_sequence.ToString();
            var invalid_chars = check_amino_acids.Matches(sequence);
            if (invalid_chars.Count > 0) {
                out_either.AddMessage(new InputNameSpace.ErrorMessage(
                    new Position(identifier_line_number, 1, file),
                    "Sequence contains invalid characters",
                    $"This sequence contains the following invalid characters: \"{invalid_chars.Aggregate("", (acc, m) => acc + m.Value)}\"."
                    )
                );
            }
            foreach (var group in new List<(Annotation, int)> { (Annotation.CDR1, cdr1), (Annotation.CDR2, cdr2), (Annotation.CDR3, cdr3) }) {
                if (group.Item2 > 1) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(
                    new Position(identifier_line_number, 1, file),
                    "Sequence contains multiple copies of the same CDR",
                    $"Any template can at max only contain one copy of each CDR, but this template contains {group.Item2} copies of {group.Item1}.",
                    "Make sure there are no copy/paste errors.",
                    true
                    )
                );
                }
            }

            ((ReadFormat.Fasta)metaData).AnnotatedSequence = annotated;
            metaData.Sequence = new ReadSequence(AminoAcid.FromString(sequence, alphabet).UnwrapOrDefault(out_either, new AminoAcid[0]), Enumerable.Repeat(metaData.Intensity, sequence.Length).ToArray());
            out_either.Value = metaData;
            return out_either;
        }

        /// <summary> Open a PEAKS CSV file, filter the reads based on the given parameters and save the reads to be used in assembly. </summary>
        /// <param name="filter">The name filter to use to filter the name of the reads.</param>
        /// <param name="peaks">The peaks settings to use</param>
        /// <param name="local">If defined the local peaks parameters to use</param>
        public static ParseResult<List<ReadFormat.General>> Peaks(NameFilter filter, RunParameters.InputData.Peaks peaks, ScoringMatrix alphabet, RunParameters.InputData.InputLocalParameters local = null, string GlobalRawDataDirectory = null) {
            var out_either = new ParseResult<List<ReadFormat.General>>();
            var match_ions = new List<(int ScanNumber, IASM ASM)>();
            if (peaks.DeNovoMatchIons != null) {
                match_ions = InputNameSpace.ParseHelper.GetAllText(peaks.DeNovoMatchIons).Map(f => Fragmentation.LoadPeaksSpectra(f).UnwrapOrDefault(out_either, new())).UnwrapOrDefault(out_either, new());
            }

            var peaks_parameters = local == null ? new RunParameters.InputData.PeaksParameters(false) : local.Peaks;
            if (peaks_parameters.CutoffALC == -1) peaks_parameters.CutoffALC = peaks.Parameter.CutoffALC;
            if (peaks_parameters.LocalCutoffALC == -1) peaks_parameters.LocalCutoffALC = peaks.Parameter.LocalCutoffALC;
            if (peaks_parameters.MinLengthPatch == -1) peaks_parameters.MinLengthPatch = peaks.Parameter.MinLengthPatch;

            var possible_content = InputNameSpace.ParseHelper.GetAllText(peaks.File);

            if (possible_content.IsErr()) {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            string[] lines = possible_content.Unwrap().Lines;
            var reads = new List<ReadFormat.General>();
            var parse_file = possible_content.Unwrap();

            out_either.Value = reads;

            // Parse each line, and filter for score or local patch
            for (int linenumber = 1; linenumber < parse_file.Lines.Length; linenumber++) {
                var parsed = ReadFormat.Peaks.ParseLine(parse_file, linenumber, peaks.Separator, peaks.DecimalSeparator, peaks.FileFormat, filter, alphabet, peaks.RawDataDirectory ?? GlobalRawDataDirectory, peaks.XleDisambiguation);

                if (parsed.IsOk(out_either)) {
                    var meta = parsed.Unwrap();
                    if (meta == null) continue; // Ignore empty lines

                    if (meta.Confidence >= peaks_parameters.CutoffALC) {
                        foreach (var scan in meta.ScanNumbers)
                            foreach (var asm in match_ions)
                                if (scan.Scan == asm.ScanNumber && (asm.ASM is SingleASM sim && Path.GetFileName(scan.RawFile) == Path.GetFileName(sim.Match.Spectrum.Precursor.RawFile)))
                                    meta.SupportingSpectra.Add(asm.ASM);
                        reads.Add(meta);
                    }
                    // Find local patches of high enough confidence
                    else if (peaks_parameters.LocalCutoffALC != -1 && peaks_parameters.MinLengthPatch != -1) {
                        bool patch = false;
                        int start_pos = 0;
                        for (int i = 0; i < meta.Sequence.PositionalScore.Length; i++) {
                            if (!patch && meta.Sequence.PositionalScore[i] * 100 >= peaks_parameters.LocalCutoffALC) {
                                // Found a potential starting position
                                start_pos = i;
                                patch = true;
                            } else if (patch && meta.Sequence.PositionalScore[i] * 100 < peaks_parameters.LocalCutoffALC) {
                                // Ends a patch
                                patch = false;
                                if (i - start_pos >= peaks_parameters.MinLengthPatch) {
                                    // Long enough use it for assembly
                                    char[] chunk = new char[i - start_pos];

                                    for (int j = start_pos; j < i; j++) {
                                        chunk[j - start_pos] = meta.Sequence.AminoAcids[j].Character;
                                    }

                                    var clone = meta.Clone();
                                    clone.Sequence = new ReadSequence(AminoAcid.FromString(new string(chunk), alphabet, meta.FileRange).UnwrapOrDefault(out_either, new AminoAcid[0]), meta.Sequence.PositionalScore.Skip(start_pos).Take(i - start_pos).ToArray());
                                    reads.Add(clone);
                                }
                            }
                        }
                    }
                } else if (linenumber < 3) {
                    // If the first real line already has errors it is very likely that the peaks format is chosen wrong so it should not overload the user with errors
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Parsing stopped", "See above error messages for errors.", "Maybe try another version of the PEAKS format.", true));
                    return out_either;
                }
            }
            return out_either;
        }

        /// <summary> Open a MaxNovo tsv file, filter the reads based on the given parameters and save the reads to be used in assembly. </summary>
        /// <param name="filter">The name filter to use to filter the name of the reads.</param>
        /// <param name="max_novo">The MaxNovo settings to use</param>
        public static ParseResult<List<ReadFormat.General>> MaxNovo(NameFilter filter, RunParameters.InputData.MaxNovo max_novo, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(max_novo.File);

            if (possible_content.IsErr()) {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            string[] lines = possible_content.Unwrap().Lines;
            var reads = new List<ReadFormat.General>();
            var parse_file = possible_content.Unwrap();

            out_either.Value = reads;

            // Parse each line, and filter for score or local patch
            for (int linenumber = 1; linenumber < parse_file.Lines.Length; linenumber++) {
                var parsed = ReadFormat.MaxNovo.ParseLine(parse_file, linenumber, filter, alphabet, max_novo.RawDataDirectory, max_novo.XleDisambiguation, max_novo.FixedModification);

                if (parsed.IsOk(out_either)) {
                    var meta = parsed.Unwrap();
                    if (meta == null) continue; // Ignore empty lines

                    if (meta.Sequence.Length >= max_novo.MinLength && meta.Score >= max_novo.CutoffScore) {
                        reads.Add(meta);
                    }
                } else if (linenumber < 3) {
                    // If the first real line already has errors it is very likely that the peaks format is chosen wrong so it should not overload the user with errors
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Parsing stopped", "See above error messages for errors.", "", true));
                    return out_either;
                }
            }
            return out_either;
        }

        /// <summary> To open a file with reads. It uses the Novor file format. Which is a
        /// character separated file format with a defined column ordering.  </summary>
        /// <param name="filter"> The name filter to use to filter the name of the reads. </param>
        /// <param name="novor"> The novor input parameter. </param>
        /// <returns> A list of all reads found. </returns>
        public static ParseResult<List<ReadFormat.General>> Novor(NameFilter filter, RunParameters.InputData.Novor novor, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();
            var output = new List<ReadFormat.General>();
            out_either.Value = output;

            if (novor.DeNovoFile != null) {
                output.AddRange(ParseNovorDeNovo(filter, novor.DeNovoFile, novor.Separator, novor.Cutoff, alphabet).UnwrapOrDefault(out_either, new()));
            }
            if (novor.PSMSFile != null) {
                output.AddRange(ParseNovorPSMS(filter, novor.PSMSFile, novor.Separator, novor.Cutoff, alphabet).UnwrapOrDefault(out_either, new()));
            }

            return out_either;
        }

        /// <summary> Read a Novor.cloud `denovo.csv` file. </summary>
        /// <param name="filter">The name filter.</param>
        /// <param name="file">The file to open.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="cutoff">The score cutoff to use.</param>
        /// <returns></returns>
        static ParseResult<List<ReadFormat.General>> ParseNovorDeNovo(NameFilter filter, ReadFormat.FileIdentifier file, char separator, uint cutoff, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(file);

            if (possible_content.IsErr()) {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<ReadFormat.General>();
            out_either.Value = reads;

            string[] lines = possible_content.Unwrap().Lines;
            var parse_file = possible_content.Unwrap();
            var linenumber = -1;

            foreach (var line in lines) {
                linenumber += 1;
                if (String.IsNullOrWhiteSpace(line)) continue;
                var split = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);
                if (split[0].Text.ToLower() == "fraction") continue; // Header line
                if (split.Count != 10) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Incorrect number of columns", $"Incorrect number of columns, expected 10 columns according to the Novor file format. Got {split.Count} fields."));
                    continue;
                }

                var range = new FileRange(new Position(linenumber, 0, parse_file), new Position(linenumber, line.Length, parse_file));
                var fraction = split[0].Text;
                var scan = InputNameSpace.ParseHelper.ConvertToInt(split[1].Text, split[1].Pos).UnwrapOrDefault(out_either, -1);
                var mz = InputNameSpace.ParseHelper.ConvertToDouble(split[2].Text, split[2].Pos).UnwrapOrDefault(out_either, -1);
                var z = InputNameSpace.ParseHelper.ConvertToInt(split[3].Text, split[3].Pos).UnwrapOrDefault(out_either, -1);
                var score = InputNameSpace.ParseHelper.ConvertToDouble(split[4].Text, split[4].Pos).UnwrapOrDefault(out_either, -1);
                var mass = InputNameSpace.ParseHelper.ConvertToDouble(split[5].Text, split[5].Pos).UnwrapOrDefault(out_either, -1);
                var error = InputNameSpace.ParseHelper.ConvertToDouble(split[6].Text, split[6].Pos).UnwrapOrDefault(out_either, -1);
                var original_peptide = split[8].Text;
                var db_sequence = split[9].Text;
                var peptide = (string)original_peptide.Clone();
                var final_peptide = "";
                while (peptide.Length > 0) {
                    var parts = peptide.Split('(', 2);
                    if (parts.Length == 2) {
                        final_peptide += parts[0];
                        peptide = parts[1].Split(')', 2).Last();
                    } else { // Length is 1 because no separator was found
                        final_peptide += parts[0];
                        peptide = "";
                    }
                }
                if (score >= cutoff)
                    reads.Add(new ReadFormat.NovorDeNovo(AminoAcid.FromString(final_peptide, alphabet, range).UnwrapOrDefault(out_either, new AminoAcid[0]), range, filter, fraction, scan, mz, z, score, mass, error, peptide, db_sequence));
            }

            return out_either;
        }

        /// <summary> Read a Novor.cloud `psms.csv` file. </summary>
        /// <param name="filter">The name filter.</param>
        /// <param name="file">The file to open.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="cutoff">The score cutoff to use.</param>
        /// <returns></returns>
        static ParseResult<List<ReadFormat.General>> ParseNovorPSMS(NameFilter filter, ReadFormat.FileIdentifier file, char separator, uint cutoff, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(file);

            if (possible_content.IsErr()) {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<ReadFormat.General>();
            out_either.Value = reads;

            string[] lines = possible_content.Unwrap().Lines;
            var parse_file = possible_content.Unwrap();
            var linenumber = -1;

            foreach (var line in lines) {
                linenumber += 1;
                if (String.IsNullOrWhiteSpace(line)) continue;
                var split = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);
                if (split[0].Text.ToLower() == "id") continue; // Header line
                if (split.Count != 10) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Incorrect number of columns", $"Incorrect number of columns, expected 10 columns according to the Novor file format. Got {split.Count} fields."));
                    continue;
                }

                var range = new FileRange(new Position(linenumber, 0, parse_file), new Position(linenumber, line.Length, parse_file));
                var id = split[0].Text;
                var fraction = split[1].Text;
                var scan = InputNameSpace.ParseHelper.ConvertToInt(split[2].Text, split[2].Pos).UnwrapOrDefault(out_either, -1);
                var mz = InputNameSpace.ParseHelper.ConvertToDouble(split[3].Text, split[3].Pos).UnwrapOrDefault(out_either, -1);
                var z = InputNameSpace.ParseHelper.ConvertToInt(split[4].Text, split[4].Pos).UnwrapOrDefault(out_either, -1);
                var score = InputNameSpace.ParseHelper.ConvertToDouble(split[5].Text, split[5].Pos).UnwrapOrDefault(out_either, -1);
                var mass = InputNameSpace.ParseHelper.ConvertToDouble(split[6].Text, split[6].Pos).UnwrapOrDefault(out_either, -1);
                var error = InputNameSpace.ParseHelper.ConvertToDouble(split[7].Text, split[7].Pos).UnwrapOrDefault(out_either, -1);
                var proteins = InputNameSpace.ParseHelper.ConvertToInt(split[8].Text, split[8].Pos).UnwrapOrDefault(out_either, -1);
                var original_peptide = split[9].Text;
                var peptide = (string)original_peptide.Clone();
                var final_peptide = "";
                while (peptide.Length > 0) {
                    var parts = peptide.Split('(', 2);
                    if (parts.Length == 2) {
                        final_peptide += parts[0];
                        peptide = parts[1].Split(')', 2).Last();
                    } else { // Length is 1 because no separator was found
                        final_peptide += parts[0];
                        peptide = "";
                    }
                }
                if (score >= cutoff)
                    reads.Add(new ReadFormat.NovorPSMS(AminoAcid.FromString(final_peptide, alphabet, range).UnwrapOrDefault(out_either, new AminoAcid[0]), range, filter, fraction, scan, mz, z, score, mass, error, peptide, id, proteins));
            }

            return out_either;
        }

        public static ParseResult<List<ReadFormat.General>> MMCIF(NameFilter filter, RunParameters.InputData.MMCIF mmcif, ScoringMatrix alphabet) {
            var out_either = new ParseResult<List<ReadFormat.General>>();
            string[] AMINO_ACIDS = new string[]{
    "ALA", "ARG", "ASH", "ASN", "ASP", "ASX", "CYS", "CYX", "GLH", "GLN", "GLU", "GLY", "HID",
    "HIE", "HIM", "HIP", "HIS", "ILE", "LEU", "LYN", "LYS", "MET", "PHE", "PRO", "SER", "THR",
    "TRP", "TYR", "VAL", "SEC", "PYL","UNK"
            };
            char[] AMINO_ACIDS_SHORT = new char[]{
    'A', 'R', 'N', 'N', 'D', 'B', 'C', 'C', 'Q', 'Q', 'E', 'G', 'H', 'H', 'H', 'H', 'H', 'I', 'L',
    'K', 'K', 'M', 'F', 'P', 'S', 'T', 'W', 'Y', 'V', 'U', 'O','X'
            };

            var loaded_file = InputNameSpace.ParseHelper.GetAllText(mmcif.File);

            if (loaded_file.IsErr()) {
                out_either.Messages.AddRange(loaded_file.Messages);
                return out_either;
            }
            var lexed = MMCIFParser.Parse(loaded_file.Unwrap());
            var file_range = new FileRange(new Position(0, 0, loaded_file.Unwrap()), new Position(0, 0, loaded_file.Unwrap()));

            if (lexed.IsErr())
                return new ParseResult<List<ReadFormat.General>>(lexed.Messages);

            foreach (var item in lexed.Unwrap().Items) {
                if (item is MMCIFItems.Loop loop && loop.Header.Contains("atom_site.group_PDB")) {
                    var chain = loop.Header.FindIndex(i => i == "atom_site.label_asym_id");
                    var auth_chain = loop.Header.FindIndex(i => i == "atom_site.auth_asym_id");
                    var residue = loop.Header.FindIndex(i => i == "atom_site.label_comp_id");
                    var residue_num = loop.Header.FindIndex(i => i == "atom_site.label_seq_id");
                    var confidence_score = loop.Header.FindIndex(i => i == "atom_site.B_iso_or_equiv");
                    if (chain == -1) return new ParseResult<List<ReadFormat.General>>(new InputNameSpace.ErrorMessage("", "Could not find chain column", "mmCIF file does not contain the column '_atom_site.label_asym_id'"));
                    if (auth_chain == -1) return new ParseResult<List<ReadFormat.General>>(new InputNameSpace.ErrorMessage("", "Could not find auth chain column", "mmCIF file does not contain the column '_atom_site.auth_asym_id'"));
                    if (residue == -1) return new ParseResult<List<ReadFormat.General>>(new InputNameSpace.ErrorMessage("", "Could not find residue column", "mmCIF file does not contain the column '_atom_site.label_comp_id'"));
                    if (residue_num == -1) return new ParseResult<List<ReadFormat.General>>(new InputNameSpace.ErrorMessage("", "Could not find residue number column", "mmCIF file does not contain the column '_atom_site.label_seq_id'"));
                    if (confidence_score == -1) return new ParseResult<List<ReadFormat.General>>(new InputNameSpace.ErrorMessage("", "Could not find B factor column", "mmCIF file does not contain the column '_atom_site.B_iso_or_equiv'"));

                    var sequences = new List<(string, string, string Seq, double[] Confidence)>();
                    var current_chain = "";
                    var current_auth_chain = "";
                    var current_sequence = "";
                    var current_num = "";
                    var local_confidence = new List<double>();

                    foreach (var row in loop.Data) {
                        if (row[chain].AsText() != current_chain) {
                            if (current_sequence.Length >= mmcif.MinLength && local_confidence.Average() * 100 >= mmcif.CutoffALC) {
                                sequences.Add((current_chain, current_auth_chain, current_sequence, local_confidence.ToArray()));
                            }
                            local_confidence.Clear();
                            current_chain = row[chain].AsText();
                            current_auth_chain = row[auth_chain].AsText();
                            current_sequence = "";
                        }
                        if (row[residue_num].AsText() == current_num)
                            continue;
                        current_num = row[residue_num].AsText();
                        var aa = Array.IndexOf(AMINO_ACIDS, row[residue].AsText());
                        if (aa != -1) {
                            current_sequence += AMINO_ACIDS_SHORT[aa];
                            local_confidence.Add(row[confidence_score].AsNumber().Unwrap(0.0) / 100.0);
                        } else {
                            out_either.AddMessage(new InputNameSpace.ErrorMessage($"Residue {row[residue].AsText()} in chain {current_chain}", "Not an AminoAcid", "This residue is not an amino acid.", "", true));
                        }
                    }
                    if (current_sequence.Length > 0 && current_sequence.Length >= mmcif.MinLength && local_confidence.Average() * 100 >= mmcif.CutoffALC) {
                        sequences.Add((current_chain, current_auth_chain, current_sequence, local_confidence.ToArray()));
                    }

                    var output = new List<ReadFormat.General>(sequences.Count);
                    foreach (var sequence in sequences) {
                        var read = AminoAcid.FromString(sequence.Seq, alphabet).Map(s => new ReadFormat.ModelAngeloRead(s, sequence.Confidence, file_range, filter, sequence.Item1, sequence.Item2));
                        if (read.IsOk())
                            output.Add(read.Unwrap());
                        else
                            out_either.AddMessage(new InputNameSpace.ErrorMessage($"{sequence}", "Not a valid sequence", "There where aminoacids in this sequence that are not part of the used alphabet."));
                    }

                    if (out_either.IsOk()) {
                        out_either.Value = output;
                    }
                    return out_either;
                }
            }

            out_either.AddMessage(new InputNameSpace.ErrorMessage(mmcif.File.ToString(), "Could not find the atomic data loop", "The mandatory atomic data loop could not be found in this mmCIF file."));
            return out_either;
        }

        public static ParseResult<List<ReadFormat.General>> Casanovo(NameFilter filter, RunParameters.InputData.Casanovo casanovo, ScoringMatrix alphabet) {
            var reads = new List<ReadFormat.General>();
            var out_either = new ParseResult<List<ReadFormat.General>>(reads);
            var loaded_file = InputNameSpace.ParseHelper.GetAllText(casanovo.File);

            if (loaded_file.IsErr()) {
                out_either.Messages.AddRange(loaded_file.Messages);
                return out_either;
            }
            var maybe_lexed = ParseMzTab.MzTabFile.Parse(loaded_file.Unwrap());
            var file_range = new FileRange(new Position(0, 0, loaded_file.Unwrap()), new Position(0, 0, loaded_file.Unwrap()));

            if (maybe_lexed.IsErr())
                return new ParseResult<List<ReadFormat.General>>(maybe_lexed.Messages);

            var lexed = maybe_lexed.Unwrap();
            var mzTabFile = lexed.Item1;

            var raw_files = new List<string>();
            for (int i = 1; ; i++) {
                var key = $"ms_run[{i}]-location";
                if (mzTabFile.MetaData.ContainsKey(key)) {
                    var data = mzTabFile.MetaData[key];
                    var path = data.Content;
                    if (casanovo.RawDataDirectory != null) {
                        var reconstituted_name = (String.IsNullOrEmpty(casanovo.RawDataDirectory) ? "./" : casanovo.RawDataDirectory + (casanovo.RawDataDirectory.EndsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar)) + Path.GetFileName(path);
                        path = InputNameSpace.ParseHelper.GetFullPath(reconstituted_name, null, "RawFile", new(), data.Location).Map(f => f.Path).UnwrapOrDefault(out_either, "");
                    }
                    raw_files.Add(path);
                } else {
                    break;
                }
            }

            var seq_idx = mzTabFile.ProteinSectionHeader.IndexOf("sequence");
            var search_idx = mzTabFile.ProteinSectionHeader.IndexOf("search_engine");
            var score_idx = mzTabFile.ProteinSectionHeader.IndexOf("search_engine_score[1]");
            var id_idx = mzTabFile.ProteinSectionHeader.IndexOf("PSM_ID");
            var spectra_idx = mzTabFile.ProteinSectionHeader.IndexOf("spectra_ref");
            var maybe_conf_idx = mzTabFile.ProteinSectionHeader.IndexOf("opt_ms_run[1]_aa_scores");
            var charge_idx = mzTabFile.ProteinSectionHeader.IndexOf("charge");
            var mz_e_idx = mzTabFile.ProteinSectionHeader.IndexOf("exp_mass_to_charge");
            var mz_t_idx = mzTabFile.ProteinSectionHeader.IndexOf("calc_mass_to_charge");

            if (seq_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'sequence' column is mandatory in the mzTab file definition."));
            if (id_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'PSM_ID' column is mandatory in the mzTab file definition."));
            if (search_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'search_engine' column is mandatory in the mzTab file definition."));
            if (score_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'search_engine_score[1]' column is mandatory in the mzTab file definition when peptides are searched."));
            if (spectra_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'spectra_ref' column is mandatory in the mzTab file definition."));
            if (charge_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'charge' column is mandatory in the mzTab file definition."));
            if (mz_e_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'exp_mass_to_charge' column is mandatory in the mzTab file definition."));
            if (mz_t_idx == -1) out_either.AddMessage(new InputNameSpace.ErrorMessage(mzTabFile.ProteinSectionHeaderLocation, "Missing column in PSM table", "The 'calc_mass_to_charge' column is mandatory in the mzTab file definition."));

            if (out_either.IsErr())
                return out_either;

            foreach (var (range, row) in lexed.Item2) {
                // Skip any non Casanovo read (or combined with other software) and skip reads without a score.
                if (row[search_idx].Content.Contains("MS:1003281") && !row[search_idx].Content.Contains('|') && row[score_idx].Content.ToLower() != "nan") {
                    var original_sequence = HelperFunctionality.FromSloppyProForma(row[seq_idx].Content);
                    if (original_sequence.IsNone()) {
                        //out_either.AddMessage(new InputNameSpace.ErrorMessage(range, $"Invalid sequence definition", "The given sequence could not be made into a valid ProForma sequence.", "", true));
                        // Casanovo makes so many incorrect sequences these should just be ignored
                        continue;
                    }
                    var filtered_sequence = original_sequence.Unwrap().Aggregate("", (acc, i) => char.IsLetter(i) ? acc + i : acc);
                    var sequence = AminoAcid.FromString(filtered_sequence, alphabet, row[seq_idx].Location).UnwrapOrDefault(out_either, new AminoAcid[0]);
                    var score = InputNameSpace.ParseHelper.ConvertToDouble(row[score_idx].Content, row[score_idx].Location).UnwrapOrDefault(out_either, 0.0);
                    var id = InputNameSpace.ParseHelper.ConvertToInt(row[id_idx].Content, row[id_idx].Location).UnwrapOrDefault(out_either, 0);
                    var confidence = maybe_conf_idx == -1 ? new double[0] : ParseMzTab.SubString.Split(row[maybe_conf_idx].Content, ',', row[maybe_conf_idx].Location.Start).Select(i => InputNameSpace.ParseHelper.ConvertToDouble(i.Content, i.Location).UnwrapOrDefault(out_either, 0.0)).ToArray();
                    var charge = InputNameSpace.ParseHelper.ConvertToInt(row[charge_idx].Content, row[charge_idx].Location).UnwrapOrDefault(out_either, 0);
                    var mz_e = InputNameSpace.ParseHelper.ConvertToDouble(row[mz_e_idx].Content, row[mz_e_idx].Location).UnwrapOrDefault(out_either, 0);
                    var mz_t = InputNameSpace.ParseHelper.ConvertToDouble(row[mz_t_idx].Content, row[mz_t_idx].Location).UnwrapOrDefault(out_either, 0);
                    var error_ppm = Math.Abs(mz_e - mz_t) / mz_t * 1e6;
                    var spectra_ref = row[spectra_idx].Content; // ms_run[1]:index=0
                    var scan_id = InputNameSpace.ParseHelper.ConvertToInt(spectra_ref.Split('=', 2)[1], row[spectra_idx].Location).UnwrapOrDefault(out_either, 0);
                    var colon_idx = spectra_ref.IndexOf(':');
                    var source_file_idx = InputNameSpace.ParseHelper.ConvertToInt(spectra_ref.Substring(7, colon_idx - 1 - 7), row[spectra_idx].Location).UnwrapOrDefault(out_either, -1);
                    var source_file = source_file_idx == -1 && source_file_idx <= raw_files.Count ? "Error while finding source file index" : raw_files[source_file_idx - 1];

                    if (score >= casanovo.CutoffScore && (casanovo.FilterPPM == -1 || error_ppm <= casanovo.FilterPPM))
                        reads.Add(new ReadFormat.Casanovo(sequence, score, confidence, range, filter, original_sequence.Unwrap(), id, row[search_idx].Content, charge, mz_e, mz_t, scan_id, source_file, casanovo.XleDisambiguation, casanovo.FragmentationMethod));
                }
            }

            return out_either;
        }

        /// <summary> Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list. </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static ParseResult<List<ReadFormat.General>> CleanUpInput(List<ReadFormat.General> reads, ScoringMatrix alp, NameFilter filter) {
            return CleanUpInput(new List<List<ReadFormat.General>> { reads }, alp, filter);
        }

        /// <summary> Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list. </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static ParseResult<List<ReadFormat.General>> CleanUpInput(List<List<ReadFormat.General>> reads, ScoringMatrix alp, NameFilter filter) {
            var filtered = new Dictionary<string, ReadFormat.General>();
            var out_either = new ParseResult<List<ReadFormat.General>>();

            foreach (var set in reads) {
                foreach (var read in set) {
                    if (filtered.ContainsKey(AminoAcid.ArrayToString(read.Sequence.AminoAcids))) {
                        if (filtered[AminoAcid.ArrayToString(read.Sequence.AminoAcids)] is ReadFormat.Combined c) {
                            c.AddChild(read);
                        } else {
                            filtered[AminoAcid.ArrayToString(read.Sequence.AminoAcids)] = new ReadFormat.Combined(read.Sequence.AminoAcids, filter, new List<ReadFormat.General> { filtered[AminoAcid.ArrayToString(read.Sequence.AminoAcids)], read });
                        }
                    } else {
                        for (int i = 0; i < read.Sequence.Length; i++) {
                            if (!alp.Contains(read.Sequence.AminoAcids[i].Character))
                                out_either.AddMessage(new InputNameSpace.ErrorMessage(read.FileRange.Value, "Invalid sequence", "This sequence contains an invalid aminoacid."));
                        }

                        filtered.Add(AminoAcid.ArrayToString(read.Sequence.AminoAcids), read);
                    }
                }
            }

            out_either.Value = filtered.Select(a => a.Value).ToList();
            return out_either;
        }
    }
}