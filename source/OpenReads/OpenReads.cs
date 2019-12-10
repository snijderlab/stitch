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
        /// namely sequences separated with whitespace (space, tab or newline)
        /// with the possibility to specify comments as lines starting with a 
        /// specific character (standard '#').  </summary>
        /// <param name="input_file"> The file to read from. </param>
        /// <param name="comment_char"> The character comment lines start with. </param>
        /// <returns> A list of all reads found. </returns>
        public static List<(string, MetaData.IMetaData)> Simple(MetaData.FileIdentifier input_file, char comment_char = '#')
        {
            var reads = new List<(string, MetaData.IMetaData)>();

            if (!File.Exists(input_file.Path))
                throw new Exception($"The specified file does not exist: {input_file.Path}");

            List<string> lines;
            try
            {
                lines = File.ReadLines(input_file.Path).ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"The specified file could not be read: {e.Message}");
            }

            foreach (var line in lines)
            {
                if (line[0] != comment_char)
                    reads.Add((line.Trim(), new MetaData.None(input_file)));
            }

            return reads;
        }
        /// <summary> To open a file with reads. the file should be in fasta format
        /// so identifiers on a single line starting with '>' followed by an arbitrary
        /// number of lines with sequences. Because sometimes programs output the length
        /// of a line after every line this is stripped away.  </summary>
        /// <param name="input_file"> The path to the file to read from. </param>
        /// <returns> A list of all reads found with their identifiers. </returns>
        public static List<(string, MetaData.IMetaData)> Fasta(MetaData.FileIdentifier input_file)
        {
            var reads = new List<(string, MetaData.IMetaData)>();

            if (!File.Exists(input_file.Path))
                throw new Exception($"The specified file does not exist: {input_file}");

            List<string> lines;
            try
            {
                lines = File.ReadLines(input_file.Path).ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"The specified CSV file {input_file.Name} at {input_file.Path} could not be read: {e.Message}");
            }
            string identifier = "";
            var sequence = new StringBuilder();

            foreach (var line in lines)
            {
                if (line[0] == '>')
                {
                    if (identifier != "")
                    {
                        // Flush last sequence to list
                        reads.Add((sequence.ToString(), new MetaData.Fasta(identifier, input_file)));
                    }
                    identifier = line.Substring(1).Trim();
                    sequence = new StringBuilder();
                }
                else
                {
                    sequence.Append(line.Trim().Where(x => Char.IsLetter(x)).ToArray());
                }
            }
            if (identifier != "")
            {
                // Flush last sequence to list
                reads.Add((sequence.ToString(), new MetaData.Fasta(identifier, input_file)));
            }

            return reads;
        }
        /// <summary> Open a PEAKS CSV file and save the reads to be used in assembly. </summary>
        /// <param name="input_file"> Path to the CSV file. </param>
        /// <param name="cutoffscore"> Score used to filter peptides, lower will be discarded. </param>
        /// <param name="localcutoffscore"> Score used to filter patches in peptides 
        /// with high enough confidence to be used contrary their low global confidence,
        /// lower will be discarded. </param>
        /// <param name="peaksformat"> The peaksformat to use, this depends on the 
        /// version of peaks used to generate the CSVs. </param>
        /// <param name="min_length_patch"> The minimal length of a patch. </param>
        /// <param name="separator"> CSV separator used. </param>
        /// <param name="decimalseparator"> Separator used in decimals. </param>
        /// <returns> A list of all reads found with their metadata. </returns>
        public static List<(string, MetaData.IMetaData)> Peaks(MetaData.FileIdentifier input_file, int cutoffscore, int localcutoffscore, FileFormat.Peaks peaksformat, int min_length_patch, char separator = ',', char decimalseparator = '.')
        {
            if (!File.Exists(input_file.Path))
                throw new Exception("The specified file does not exist, file asked for: " + input_file);

            List<string> lines = File.ReadLines(input_file.Path).ToList();
            var reads = new List<(string, MetaData.IMetaData)>();

            int linenumber = 0;
            foreach (var line in lines)
            {
                linenumber++;
                if (linenumber != 1)
                {
                    try
                    {
                        var meta = new MetaData.Peaks(line, separator, decimalseparator, peaksformat, input_file);
                        if (meta.Confidence >= cutoffscore)
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
                        else
                        {
                            bool patch = false;
                            int startpos = 0;
                            for (int i = 0; i < meta.Local_confidence.Length; i++)
                            {
                                if (!patch && meta.Local_confidence[i] >= localcutoffscore)
                                {
                                    // Found a potential starting position
                                    startpos = i;
                                    patch = true;
                                }
                                else if (patch && meta.Local_confidence[i] < localcutoffscore)
                                {
                                    // Ends a patch
                                    patch = false;
                                    if (i - startpos >= min_length_patch)
                                    {
                                        // Long enough use it for assembly
                                        char[] chunk = new char[i - startpos];

                                        for (int j = startpos; j < i; j++)
                                        {
                                            chunk[j - startpos] = meta.Cleaned_sequence[j];
                                        }

                                        reads.Add((new string(chunk), meta));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR while importing from PEAKS CSV {input_file.Name} at {input_file.Path} on line {linenumber}\n{e.Message}");
                    }
                }
            }
            return reads;
        }
    }
    
}