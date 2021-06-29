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
    /// To contain all logic for the reading of reads out of files.
    /// </summary>
    public static class OpenReads
    {
        /// <summary> To open a file with reads. It assumes a very basic format,
        /// namely sequences separated with newlines
        /// with the possibility to specify comments as lines starting with a
        /// specific character (standard '#').  </summary>
        /// <param name="filter"> The namefilter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The file to read from. </param>
        /// <param name="commentChar"> The character comment lines start with. </param>
        /// <returns> A list of all reads found. </returns>
        public static ParseResult<List<(string, MetaData.IMetaData)>> Simple(NameFilter filter, MetaData.FileIdentifier inputFile, char commentChar = '#')
        {
            var outeither = new ParseResult<List<(string, MetaData.IMetaData)>>();

            var possiblecontent = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possiblecontent.HasFailed())
            {
                outeither.Messages.AddRange(possiblecontent.Messages);
                return outeither;
            }

            var reads = new List<(string, MetaData.IMetaData)>();
            outeither.Value = reads;

            var lines = possiblecontent.ReturnOrFail().Split('\n');

            foreach (var line in lines)
            {
                if (line.Length == 0) continue;
                if (line[0] != commentChar)
                    reads.Add((line.Trim(), new MetaData.Simple(inputFile, filter)));
            }

            return outeither;
        }

        /// <summary> To open a file with reads. the file should be in fasta format
        /// so identifiers on a single line starting with '>' followed by an arbitrary
        /// number of lines with sequences. Because sometimes programs output the length
        /// of a line after every line this is stripped away.  </summary>
        /// <param name="filter"> The namefilter to use to filter the name of the reads. </param>
        /// <param name="inputFile"> The path to the file to read from. </param>
        /// <param name="parseIdentifier"> The regex to determine how to parse the identifier from the fasta header. </param>
        /// <returns> A list of all reads found with their identifiers. </returns>
        public static ParseResult<List<(string Sequence, MetaData.IMetaData MetaData)>> Fasta(NameFilter filter, MetaData.FileIdentifier inputFile, Regex parseIdentifier)
        {
            var outeither = new ParseResult<List<(string, MetaData.IMetaData)>>();

            var possiblecontent = InputNameSpace.ParseHelper.GetAllText(inputFile);

            if (possiblecontent.HasFailed())
            {
                outeither.Messages.AddRange(possiblecontent.Messages);
                return outeither;
            }

            var reads = new List<(string, MetaData.IMetaData)>();
            outeither.Value = reads;

            var lines = possiblecontent.ReturnOrFail().Split('\n').ToArray();
            var parsefile = new ParsedFile(inputFile.Path, lines);

            string identifierLine = "";
            int identifierLineNumber = 1;
            var sequence = new StringBuilder();
            int linenumber = 0;

            foreach (var line in lines)
            {
                if (line.Length == 0) continue;
                if (line[0] == '>')
                {
                    if (identifierLine != "")
                    {
                        var match = parseIdentifier.Match(identifierLine);
                        if (match.Success)
                        {
                            if (match.Groups.Count == 3)
                            {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new MetaData.Fasta(match.Groups[1].Value, identifierLine, inputFile, filter, match.Groups[2].Value)));
                            }
                            else if (match.Groups.Count == 2)
                            {
                                reads.Add(ParseAnnotatedFasta(sequence.ToString(), new MetaData.Fasta(match.Groups[1].Value, identifierLine, inputFile, filter)));
                            }
                            else
                            {
                                outeither.AddMessage(new InputNameSpace.ErrorMessage(
                                    parseIdentifier.ToString(),
                                    "Identifier Regex has invalid number of capturing groups",
                                    "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                                    )
                                );
                                return outeither;
                            }
                        }
                        else
                        {
                            outeither.AddMessage(new InputNameSpace.ErrorMessage(
                                new FileRange(new Position(identifierLineNumber, 1, parsefile), new Position(identifierLineNumber, identifierLine.Length, parsefile)),
                                "Header line does not match RegEx",
                                "This headerline does not match the RegEx given to parse the identifier."
                                )
                            );
                        }
                    }
                    identifierLine = line.Substring(1).Trim();
                    sequence = new StringBuilder();
                    identifierLineNumber = linenumber;
                }
                else
                {
                    sequence.Append(line.Trim().ToArray());
                }
                linenumber++;
            }
            if (identifierLine != "")
            {
                // Flush last sequence to list
                var match = parseIdentifier.Match(identifierLine);

                if (match.Success)
                {
                    if (match.Groups.Count == 3)
                    {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new MetaData.Fasta(match.Groups[1].Value, identifierLine, inputFile, filter, match.Groups[2].Value)));
                    }
                    else if (match.Groups.Count == 2)
                    {
                        reads.Add(ParseAnnotatedFasta(sequence.ToString(), new MetaData.Fasta(match.Groups[1].Value, identifierLine, inputFile, filter)));
                    }
                    else
                    {
                        outeither.AddMessage(new InputNameSpace.ErrorMessage(
                            parseIdentifier.ToString(),
                            "Identifier Regex has invalid number of capturing groups",
                            "The regex to parse the identifier for Fasta headers should contain one or two capturing groups."
                            )
                        );
                        return outeither;
                    }
                }
                else
                {
                    outeither.AddMessage(new InputNameSpace.ErrorMessage(
                        new FileRange(new Position(identifierLineNumber, 1, parsefile), new Position(identifierLineNumber, identifierLine.Length, parsefile)),
                        "Header line does not match RegEx",
                        "This headerline does not match the RegEx given to parse the identifier."
                        )
                    );
                }
            }

            return outeither;
        }

        static (string, MetaData.IMetaData) ParseAnnotatedFasta(string line, MetaData.IMetaData metaData)
        {
            var plain_sequence = new StringBuilder();
            var annotated = new List<(string, string)>();
            string current_seq = "";
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '(')
                {
                    if (current_seq != "")
                    {
                        annotated.Add(("", current_seq));
                        current_seq = "";
                    }
                    i += 1;
                    int space = line.IndexOf(' ', i);
                    int close = line.IndexOf(')', i);
                    var seq = line.Substring(space + 1, close - space - 1);
                    annotated.Add((line.Substring(i, space - i), seq));
                    plain_sequence.Append(seq);
                    i = close;
                }
                else
                {
                    current_seq += line[i];
                    plain_sequence.Append(line[i]);
                }
            }
            if (current_seq != "")
                annotated.Add(("", current_seq));

            ((MetaData.Fasta)metaData).AnnotatedSequence = annotated;
            return (plain_sequence.ToString(), metaData);
        }

        /// <summary> Open a PEAKS CSV file, filter the reads based on the given parameters and save the reads to be used in assembly. </summary>
        /// <param name="filter">The namefilter to use to filter the name of the reads.</param>
        /// <param name="peaks">The peaks settings to use</param>
        /// <param name="local">If defined the local peaks parameters to use</param>
        public static ParseResult<List<(string, MetaData.IMetaData)>> Peaks(NameFilter filter, RunParameters.Input.Peaks peaks, RunParameters.Input.InputLocalParameters local = null)
        {
            var outeither = new ParseResult<List<(string, MetaData.IMetaData)>>();

            var peaksparameters = local == null ? new RunParameters.Input.PeaksParameters(false) : local.Peaks;
            if (peaksparameters.CutoffALC == -1) peaksparameters.CutoffALC = peaks.Parameter.CutoffALC;
            if (peaksparameters.LocalCutoffALC == -1) peaksparameters.LocalCutoffALC = peaks.Parameter.LocalCutoffALC;
            if (peaksparameters.MinLengthPatch == -1) peaksparameters.MinLengthPatch = peaks.Parameter.MinLengthPatch;

            var possiblecontent = InputNameSpace.ParseHelper.GetAllText(peaks.File);

            if (possiblecontent.HasFailed())
            {
                outeither.Messages.AddRange(possiblecontent.Messages);
                return outeither;
            }

            List<string> lines = possiblecontent.ReturnOrFail().Split('\n').ToList();
            var reads = new List<(string, MetaData.IMetaData)>();
            var parsefile = new ParsedFile(peaks.File.Name, lines.ToArray());

            outeither.Value = reads;

            // Parse each line, and filter for score or local patch
            for (int linenumber = 1; linenumber < parsefile.Lines.Length; linenumber++)
            {
                var parsed = MetaData.Peaks.ParseLine(parsefile, linenumber, peaks.Separator, peaks.DecimalSeparator, peaks.FileFormat, peaks.File, filter);

                if (parsed.HasOnlyWarnings()) continue;

                outeither.Messages.AddRange(parsed.Messages);

                if (parsed.HasFailed())
                {
                    if (linenumber < 3)
                    {
                        // If the first real line already has errors it is very likely that the peaks format is chosen wrong so it should not overload the user with errors
                        outeither.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parsefile), "Parsing stopped", "See above error messages for errors.", "Maybe try another version of the PEAKS format.", true));
                        outeither.PrintMessages();
                        Environment.Exit(1);
                        break;
                    }
                    continue;
                }

                var meta = parsed.ReturnOrFail();

                if (meta.Confidence >= peaksparameters.CutoffALC)
                {
                    if (reads.Where(x => x.Item1 == meta.Cleaned_sequence).Count() == 0)
                    {
                        reads.Add((meta.Cleaned_sequence, meta));
                    }
                    else
                    {
                        int pos = reads.FindIndex(x => x.Item1 == meta.Cleaned_sequence);
                        ((MetaData.Peaks)reads[pos].Item2).Other_scans.Add(meta.ScanID);
                    }
                }
                // Find local patches of high enough confidence
                else if (peaksparameters.LocalCutoffALC != -1 && peaksparameters.MinLengthPatch != -1)
                {
                    bool patch = false;
                    int startpos = 0;
                    for (int i = 0; i < meta.Local_confidence.Length; i++)
                    {
                        if (!patch && meta.Local_confidence[i] >= peaksparameters.LocalCutoffALC)
                        {
                            // Found a potential starting position
                            startpos = i;
                            patch = true;
                        }
                        else if (patch && meta.Local_confidence[i] < peaksparameters.LocalCutoffALC)
                        {
                            // Ends a patch
                            patch = false;
                            if (i - startpos >= peaksparameters.MinLengthPatch)
                            {
                                // Long enough use it for assembly
                                char[] chunk = new char[i - startpos];

                                for (int j = startpos; j < i; j++)
                                {
                                    chunk[j - startpos] = meta.Cleaned_sequence[j];
                                }

                                reads.Add((new string(chunk), meta.Clone()));
                            }
                        }
                    }
                }
            }
            return outeither;
        }

        /// <summary>
        /// Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list.
        /// </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static List<(string Sequence, MetaData.IMetaData MetaData)> CleanUpInput(List<(string Sequence, MetaData.IMetaData MetaData)> reads)
        {
            return CleanUpInput(new List<List<(string Sequence, MetaData.IMetaData MetaData)>> { reads });
        }

        /// <summary>
        /// Cleans up a list of input reads by removing duplicates and squashing it into a single dimension list.
        /// </summary>
        /// <param name="reads"> The input reads to clean up. </param>
        public static List<(string Sequence, MetaData.IMetaData MetaData)> CleanUpInput(List<List<(string Sequence, MetaData.IMetaData MetaData)>> reads)
        {
            var filtered = new Dictionary<string, MetaData.IMetaData>();

            foreach (var set in reads)
            {
                foreach (var read in set)
                {
                    if (filtered.ContainsKey(read.Sequence))
                    {
                        filtered[read.Sequence].Intensity += read.MetaData.Intensity;
                        // TODO: Trace the dual origins
                    }
                    else
                    {
                        filtered.Add(read.Sequence, read.MetaData);
                    }
                }
            }

            return filtered.Select(a => (a.Key, a.Value)).ToList();
        }
    }
}