using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Stitch.HelperFunctionality;

namespace Stitch
{
    /// <summary> To contain all logic for the reading of reads out of files. </summary> 
    public static class OpenReads
    {
        /// <summary> To open a file with reads. It assumes a very basic format,
        /// namely sequences separated with newlines
        /// with the possibility to specify comments as lines starting with a
        /// specific character (standard '#').  </summary>
        /// <param name="filter"> The name filter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The file to read from. </param>
        /// <param name="commentChar"> The character comment lines start with. </param>
        /// <returns> A list of all reads found. </returns>
        public static ParseResult<List<(string, ReadMetaData.IMetaData)>> Simple(NameFilter filter, ReadMetaData.FileIdentifier inputFile, char commentChar = '#')
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possible_content.IsOk(out_either))
            {
                var parsed = new ParsedFile(inputFile, possible_content.Unwrap().Split('\n'));
                var reads = new List<(string, ReadMetaData.IMetaData)>();
                out_either.Value = reads;

                for (int line = 0; line < parsed.Lines.Length; line++)
                {
                    var content = parsed.Lines[line];
                    if (content.Length == 0) continue;
                    if (content[0] != commentChar)
                        reads.Add((content.Trim(), new ReadMetaData.Simple(new FileRange(new Position(line, 0, parsed), new Position(line, 0, parsed)), filter)));
                }
            }
            return out_either;
        }

        /// <summary> To open a file with reads. the file should be in fasta format
        /// so identifiers on a single line starting with '>' followed by an arbitrary
        /// number of lines with sequences. Because sometimes programs output the length
        /// of a line after every line this is stripped away.  </summary>
        /// <param name="filter"> The name filter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The path to the file to read from. </param>
        /// <param name="parseIdentifier"> The regex to determine how to parse the identifier from the fasta header. </param>
        /// <returns> A list of all reads found with their identifiers. </returns>
        public static ParseResult<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> Fasta(NameFilter filter, ReadMetaData.FileIdentifier inputFile, Regex parseIdentifier)
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possible_content.IsErr())
            {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<(string, ReadMetaData.IMetaData)>();
            out_either.Value = reads;

            var lines = possible_content.Unwrap().Split('\n').ToArray();
            var parse_file = new ParsedFile(inputFile, lines);

            string identifierLine = "";
            int identifierLineNumber = 1;
            var sequence = new StringBuilder();
            int linenumber = 0;
            var start_pos = new Position(linenumber, 0, parse_file);

            foreach (var line in lines)
            {
                var end_pos = new Position(linenumber, line.Length, parse_file);
                var range = new FileRange(start_pos, end_pos);
                if (line.Length == 0) continue;
                if (line[0] == '>')
                {
                    if (!string.IsNullOrEmpty(identifierLine))
                    {
                        var match = parseIdentifier.Match(identifierLine);
                        var identifier_line_range = new FileRange(new Position(identifierLineNumber, 1, parse_file), new Position(identifierLineNumber, identifierLine.Length, parse_file));
                        if (match.Success)
                        {
                            if (match.Groups.Count == 3)
                            {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadMetaData.Fasta(match.Groups[1].Value, identifierLine, range, filter, match.Groups[2].Value), identifierLineNumber, parse_file).UnwrapOrDefault(out_either, new()));
                            }
                            else if (match.Groups.Count == 2)
                            {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadMetaData.Fasta(match.Groups[1].Value, identifierLine, range, filter), identifierLineNumber, parse_file).UnwrapOrDefault(out_either, new()));
                            }
                            else
                            {
                                out_either.AddMessage(new InputNameSpace.ErrorMessage(
                                    identifier_line_range,
                                    "Identifier Regex has invalid number of capturing groups",
                                    "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                                    )
                                );
                                return out_either;
                            }
                        }
                        else
                        {
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
                }
                else
                {
                    sequence.Append(line.ToArray());
                }
                linenumber++;
            }
            if (!string.IsNullOrEmpty(identifierLine))
            {
                var end_pos = new Position(linenumber, identifierLine.Length, parse_file);
                var range = new FileRange(start_pos, end_pos);
                // Flush last sequence to list
                var match = parseIdentifier.Match(identifierLine);
                var identifier_line_range = new FileRange(new Position(identifierLineNumber, 1, parse_file), new Position(identifierLineNumber, identifierLine.Length, parse_file));

                if (match.Success)
                {
                    if (match.Groups.Count == 3)
                    {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadMetaData.Fasta(match.Groups[1].Value, identifierLine, range, filter, match.Groups[2].Value), identifierLineNumber, parse_file).UnwrapOrDefault(out_either, new()));
                    }
                    else if (match.Groups.Count == 2)
                    {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new ReadMetaData.Fasta(match.Groups[1].Value, identifierLine, range, filter), identifierLineNumber, parse_file).UnwrapOrDefault(out_either, new()));
                    }
                    else
                    {
                        out_either.AddMessage(new InputNameSpace.ErrorMessage(
                            identifier_line_range,
                            "Identifier Regex has invalid number of capturing groups",
                            "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                            )
                        );
                        return out_either;
                    }
                }
                else
                {
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
        private static string RemoveWhitespace(string input)
        {
            return remove_whitespace.Replace(input, "");
        }
        private static readonly Regex check_amino_acids = new Regex("[^ACDEFGHIKLMNOPQRSTUVWY]", RegexOptions.IgnoreCase);
        static ParseResult<(string, ReadMetaData.IMetaData)> ParseAnnotatedFasta(string line, ReadMetaData.IMetaData metaData, int identifier_line_number, ParsedFile file)
        {

            var out_either = new ParseResult<(string, ReadMetaData.IMetaData)>();
            var plain_sequence = new StringBuilder();
            var annotated = new List<(HelperFunctionality.Annotation, string)>();
            string current_seq = "";
            int cdr1 = 0;
            int cdr2 = 0;
            int cdr3 = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '(')
                {
                    if (!string.IsNullOrEmpty(current_seq))
                    {
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
                    if (annotation.IsAnyCDR())
                    {
                        if (!(annotated_count >= 3 && annotated[annotated_count - 3].Item1 == annotation && annotated[annotated_count - 2].Item1 == Annotation.PossibleGlycan))
                        {
                            if (annotation == Annotation.CDR1)
                            {
                                cdr1 += 1;
                            }
                            else if (annotation == Annotation.CDR2)
                            {
                                cdr2 += 1;
                            }
                            else if (annotation == Annotation.CDR3)
                            {
                                cdr3 += 1;
                            }
                        }
                    }
                }
                else if (!Char.IsWhiteSpace(line[i]))
                {
                    current_seq += line[i];
                    plain_sequence.Append(line[i]);
                }
            }
            if (!string.IsNullOrEmpty(current_seq.Trim()))
                annotated.Add((Annotation.None, current_seq));
            var sequence = plain_sequence.ToString();
            var invalid_chars = check_amino_acids.Matches(sequence);
            if (invalid_chars.Count > 0)
            {
                out_either.AddMessage(new InputNameSpace.ErrorMessage(
                    new Position(identifier_line_number, 1, file),
                    "Sequence contains invalid characters",
                    $"This sequence contains the following invalid characters: \"{invalid_chars.Aggregate("", (acc, m) => acc + m.Value)}\"."
                    )
                );
            }
            foreach (var group in new List<(Annotation, int)> { (Annotation.CDR1, cdr1), (Annotation.CDR2, cdr2), (Annotation.CDR3, cdr3) })
            {
                if (group.Item2 > 1)
                {
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

            ((ReadMetaData.Fasta)metaData).AnnotatedSequence = annotated;
            out_either.Value = (sequence, metaData);
            return out_either;
        }

        /// <summary> Open a PEAKS CSV file, filter the reads based on the given parameters and save the reads to be used in assembly. </summary>
        /// <param name="filter">The name filter to use to filter the name of the reads.</param>
        /// <param name="peaks">The peaks settings to use</param>
        /// <param name="local">If defined the local peaks parameters to use</param>
        public static ParseResult<List<(string, ReadMetaData.IMetaData)>> Peaks(NameFilter filter, RunParameters.InputData.Peaks peaks, RunParameters.InputData.InputLocalParameters local = null)
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

            var peaks_parameters = local == null ? new RunParameters.InputData.PeaksParameters(false) : local.Peaks;
            if (peaks_parameters.CutoffALC == -1) peaks_parameters.CutoffALC = peaks.Parameter.CutoffALC;
            if (peaks_parameters.LocalCutoffALC == -1) peaks_parameters.LocalCutoffALC = peaks.Parameter.LocalCutoffALC;
            if (peaks_parameters.MinLengthPatch == -1) peaks_parameters.MinLengthPatch = peaks.Parameter.MinLengthPatch;

            var possible_content = InputNameSpace.ParseHelper.GetAllText(peaks.File);

            if (possible_content.IsErr())
            {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            List<string> lines = possible_content.Unwrap().Split('\n').ToList();
            var reads = new List<(string, ReadMetaData.IMetaData)>();
            var parse_file = new ParsedFile(peaks.File, lines.ToArray());

            out_either.Value = reads;

            // Parse each line, and filter for score or local patch
            for (int linenumber = 1; linenumber < parse_file.Lines.Length; linenumber++)
            {
                var parsed = ReadMetaData.Peaks.ParseLine(parse_file, linenumber, peaks.Separator, peaks.DecimalSeparator, peaks.FileFormat, filter);

                if (parsed.IsOk(out_either))
                {
                    var meta = parsed.Unwrap();
                    if (meta == null) continue; // Ignore empty lines

                    if (meta.Confidence >= peaks_parameters.CutoffALC)
                    {
                        reads.Add((meta.Cleaned_sequence, meta));
                    }
                    // Find local patches of high enough confidence
                    else if (peaks_parameters.LocalCutoffALC != -1 && peaks_parameters.MinLengthPatch != -1)
                    {
                        bool patch = false;
                        int start_pos = 0;
                        for (int i = 0; i < meta.Local_confidence.Length; i++)
                        {
                            if (!patch && meta.Local_confidence[i] >= peaks_parameters.LocalCutoffALC)
                            {
                                // Found a potential starting position
                                start_pos = i;
                                patch = true;
                            }
                            else if (patch && meta.Local_confidence[i] < peaks_parameters.LocalCutoffALC)
                            {
                                // Ends a patch
                                patch = false;
                                if (i - start_pos >= peaks_parameters.MinLengthPatch)
                                {
                                    // Long enough use it for assembly
                                    char[] chunk = new char[i - start_pos];

                                    for (int j = start_pos; j < i; j++)
                                    {
                                        chunk[j - start_pos] = meta.Cleaned_sequence[j];
                                    }

                                    reads.Add((new string(chunk), meta.Clone()));
                                }
                            }
                        }
                    }
                }
                else if (linenumber < 3)
                {
                    // If the first real line already has errors it is very likely that the peaks format is chosen wrong so it should not overload the user with errors
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Parsing stopped", "See above error messages for errors.", "Maybe try another version of the PEAKS format.", true));
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
        public static ParseResult<List<(string, ReadMetaData.IMetaData)>> Novor(NameFilter filter, RunParameters.InputData.Novor novor)
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();
            var output = new List<(string, ReadMetaData.IMetaData)>();
            out_either.Value = output;

            if (novor.DeNovoFile != null)
            {
                output.AddRange(ParseNovorDeNovo(filter, novor.DeNovoFile, novor.Separator, novor.Cutoff).UnwrapOrDefault(out_either, new()));
            }
            if (novor.PSMSFile != null)
            {
                output.AddRange(ParseNovorPSMS(filter, novor.PSMSFile, novor.Separator, novor.Cutoff).UnwrapOrDefault(out_either, new()));
            }

            return out_either;
        }

        /// <summary> Read a Novor.cloud `denovo.csv` file. </summary>
        /// <param name="filter">The name filter.</param>
        /// <param name="file">The file to open.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="cutoff">The score cutoff to use.</param>
        /// <returns></returns>
        static ParseResult<List<(string, ReadMetaData.IMetaData)>> ParseNovorDeNovo(NameFilter filter, ReadMetaData.FileIdentifier file, char separator, uint cutoff)
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(file);

            if (possible_content.IsErr())
            {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<(string, ReadMetaData.IMetaData)>();
            out_either.Value = reads;

            var lines = possible_content.Unwrap().Split('\n');
            var parse_file = new ParsedFile(file, lines);
            var linenumber = -1;

            foreach (var line in lines)
            {
                linenumber += 1;
                if (String.IsNullOrWhiteSpace(line)) continue;
                var split = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);
                if (split[0].Text.ToLower() == "fraction") continue; // Header line
                if (split.Count != 10)
                {
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
                while (peptide.Length > 0)
                {
                    var parts = peptide.Split('(', 2);
                    if (parts.Length == 2)
                    {
                        final_peptide += parts[0];
                        peptide = parts[1].Split(')', 2).Last();
                    }
                    else
                    { // Length is 1 because no separator was found
                        final_peptide += parts[0];
                        peptide = "";
                    }
                }
                if (score >= cutoff)
                    reads.Add((final_peptide, new ReadMetaData.NovorDeNovo(range, filter, fraction, scan, mz, z, score, mass, error, peptide, db_sequence)));
            }

            return out_either;
        }

        /// <summary> Read a Novor.cloud `psms.csv` file. </summary>
        /// <param name="filter">The name filter.</param>
        /// <param name="file">The file to open.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="cutoff">The score cutoff to use.</param>
        /// <returns></returns>
        static ParseResult<List<(string, ReadMetaData.IMetaData)>> ParseNovorPSMS(NameFilter filter, ReadMetaData.FileIdentifier file, char separator, uint cutoff)
        {
            var out_either = new ParseResult<List<(string, ReadMetaData.IMetaData)>>();

            var possible_content = InputNameSpace.ParseHelper.GetAllText(file);

            if (possible_content.IsErr())
            {
                out_either.Messages.AddRange(possible_content.Messages);
                return out_either;
            }

            var reads = new List<(string, ReadMetaData.IMetaData)>();
            out_either.Value = reads;

            var lines = possible_content.Unwrap().Split('\n');
            var parse_file = new ParsedFile(file, lines);
            var linenumber = -1;

            foreach (var line in lines)
            {
                linenumber += 1;
                if (String.IsNullOrWhiteSpace(line)) continue;
                var split = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);
                if (split[0].Text.ToLower() == "id") continue; // Header line
                if (split.Count != 10)
                {
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
                while (peptide.Length > 0)
                {
                    var parts = peptide.Split('(', 2);
                    if (parts.Length == 2)
                    {
                        final_peptide += parts[0];
                        peptide = parts[1].Split(')', 2).Last();
                    }
                    else
                    { // Length is 1 because no separator was found
                        final_peptide += parts[0];
                        peptide = "";
                    }
                }
                if (score >= cutoff)
                    reads.Add((final_peptide, new ReadMetaData.NovorPSMS(range, filter, fraction, scan, mz, z, score, mass, error, peptide, id, proteins)));
            }

            return out_either;
        }

        /// <summary> Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list. </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static ParseResult<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> CleanUpInput(List<(string Sequence, ReadMetaData.IMetaData MetaData)> reads, Alphabet alp, NameFilter filter)
        {
            return CleanUpInput(new List<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> { reads }, alp, filter);
        }

        /// <summary> Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list. </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static ParseResult<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> CleanUpInput(List<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> reads, Alphabet alp, NameFilter filter)
        {
            var filtered = new Dictionary<string, ReadMetaData.IMetaData>();
            var out_either = new ParseResult<List<(string Sequence, ReadMetaData.IMetaData MetaData)>>();

            foreach (var set in reads)
            {
                foreach (var read in set)
                {
                    if (filtered.ContainsKey(read.Sequence))
                    {
                        if (filtered[read.Sequence] is ReadMetaData.Combined c)
                        {
                            c.Children.Add(read.MetaData);
                        }
                        else
                        {
                            filtered[read.Sequence] = new ReadMetaData.Combined(filter, new List<ReadMetaData.IMetaData> { filtered[read.Sequence], read.MetaData });
                        }
                    }
                    else
                    {
                        for (int i = 0; i < read.Sequence.Length; i++)
                        {
                            if (!alp.PositionInScoringMatrix.ContainsKey(read.Sequence[i]))
                                out_either.AddMessage(new InputNameSpace.ErrorMessage(read.MetaData.FileRange.Value, "Invalid sequence", "This sequence contains an invalid aminoacid."));
                        }

                        filtered.Add(read.Sequence, read.MetaData);
                    }
                }
            }

            out_either.Value = filtered.Select(a => (a.Key, a.Value)).ToList();
            return out_either;
        }
    }
}