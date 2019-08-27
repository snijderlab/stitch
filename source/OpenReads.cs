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
        /// with high enough confidence to be used contrary their low gloabl confidence,
        /// lower will be discarded. </param>
        /// <param name="peaksformat"> The peaksformat to use, this depends on the 
        /// version of peaks used to generate the CSV's. </param>
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
    /// <summary>
    /// A class to hold all metadata handling in one place.
    /// </summary>
    public static class MetaData
    {
        /// <summary>
        /// The interface which proper metadata instances should implement.
        /// </summary>
        public abstract class IMetaData
        {
            /// <summary>
            /// The Identifier of the originating file
            /// </summary>
            public FileIdentifier File;
            /// <summary>
            /// To generate (an) HTML element(s) from this MetaData
            /// </summary>
            /// <returns>A string containing the MetaData</returns>
            public abstract string ToHTML();
            /// <summary>
            /// To create an instance
            /// </summary>
            /// <param name="file">The identifier of the originating file</param>
            public IMetaData(FileIdentifier file)
            {
                File = file;
            }
        }
        /// <summary>
        /// A metadata instance to contain no metadata so reads without metadata can also be handeled.
        /// </summary>
        public class None : IMetaData
        {
            /// <summary>
            /// Create a new None MetaData
            /// </summary>
            public None(FileIdentifier file) : base(file) { }
            /// <summary>
            /// Returns None MetaData to HTML (which is always "")
            /// </summary>
            /// <returns>""</returns>
            public override string ToHTML()
            {
                return File.ToHTML();
            }
        }
        /// <summary> A struct to hold metainformation from fasta data. </summary>
        public class Fasta : IMetaData
        {
            /// <summary>
            /// The identifier from the fasta file.
            /// </summary>
            public string Identifier;
            /// <summary>
            /// To create a new metadata instance with this metadata.
            /// </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="file">The originating file.</param>
            public Fasta(string identifier, FileIdentifier file)
                : base(file)
            {
                this.Identifier = identifier;
            }
            /// <summary> Generate HTML with all metainformation from the fasta data. </summary>
            /// <returns> Returns an HTML string with the metainformation. </returns>
            public override string ToHTML()
            {
                return $"<h2>Meta Information from fasta</h2>\n<h3>Identifier</h3>\n<p>{Identifier}</p>{File.ToHTML()}";
            }
        }
        /// <summary> A struct to hold metainformation from PEAKS data. </summary>
        public class Peaks : IMetaData
        {
            /// <summary> The Fraction number of the peptide. </summary>
            public string Fraction = null;
            /// <summary> The source file out of wich the peptide was generated. </summary>
            public string Source_File = null;
            /// <summary> The feature of the peptide. </summary>
            public string Feature = null;
            /// <summary> The scan identifier of the peptide. </summary>
            public string ScanID = null;
            /// <summary> The sequence with modifications of the peptide. </summary>
            public string Original_tag = null;
            /// <summary> The sequence without modifications of the peptide. </summary>
            public string Cleaned_sequence = null;
            /// <summary> The confidence score of the peptide. </summary>
            public int Confidence = -1;
            /// <summary> m/z of the peptide. </summary>
            public double Mass_over_charge = -1;
            /// <summary> z of the peptide. </summary>
            public int Charge = -1;
            /// <summary> Retention time of the peptide. </summary>
            public double Retention_time = -1;
            /// <summary> Area of the peak of the peptide.</summary>
            public double Area = -1;
            /// <summary> Mass of the peptide.</summary>
            public double Mass = -1;
            /// <summary> PPM of the peptide. </summary>
            public double Parts_per_million = -1;
            /// <summary> Posttranslational Modifications of the peptide. </summary>
            public string Post_translational_modifications = null;
            /// <summary> Local confidence scores of the peptide. </summary>
            public int[] Local_confidence = null;
            /// <summary> Fragmentation mode used to generate the peptide. </summary>
            public string Fragmentation_mode = null;
            /// <summary> Other scans giving the same sequence. </summary>
            public List<string> Other_scans = null;
            /// <summary> Create a PeaksMeta struct based on a CSV line in PEAKS format. </summary>
            /// <param name="line"> The CSV line to parse. </param>
            /// <param name="separator"> The separator used in CSV. </param>
            /// <param name="decimalseparator"> The separator used in decimals. </param>
            /// <param name="pf">FileFormat of the PEAKS file</param>
            /// <param name="file">Identifier for the originating file</param>
            public Peaks(string line, char separator, char decimalseparator, FileFormat.Peaks pf, FileIdentifier file)
                : base(file)
            {
                try
                {
                    char current_decimal_separator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray()[0];
                    string[] fields = line.Split(separator);

                    // Assign all values
                    if (pf.fraction >= 0) Fraction = fields[pf.fraction];
                    if (pf.source_file >= 0) Source_File = fields[pf.source_file];
                    if (pf.feature >= 0) Feature = fields[pf.feature];
                    if (pf.scan >= 0) ScanID = fields[pf.scan];
                    if (pf.peptide >= 0)
                    {
                        Original_tag = fields[pf.peptide];
                        Cleaned_sequence = new string(Original_tag.Where(x => Char.IsUpper(x) && Char.IsLetter(x)).ToArray());
                    }
                    if (pf.alc >= 0) Confidence = Convert.ToInt32(fields[pf.alc].Replace(decimalseparator, current_decimal_separator));
                    if (pf.mz >= 0) Mass_over_charge = Convert.ToDouble(fields[pf.mz].Replace(decimalseparator, current_decimal_separator));
                    if (pf.z >= 0) Charge = Convert.ToInt32(fields[pf.z].Replace(decimalseparator, current_decimal_separator));
                    if (pf.rt >= 0) Retention_time = Convert.ToDouble(fields[pf.rt].Replace(decimalseparator, current_decimal_separator));
                    if (pf.area >= 0)
                    {
                        try
                        {
                            Area = Convert.ToDouble(fields[pf.area].Replace(decimalseparator, current_decimal_separator));
                        }
                        catch
                        {
                            Area = -1;
                        }
                    }
                    if (pf.mass >= 0) Mass = Convert.ToDouble(fields[pf.mass].Replace(decimalseparator, current_decimal_separator));
                    if (pf.ppm >= 0) Parts_per_million = Convert.ToDouble(fields[pf.ppm].Replace(decimalseparator, current_decimal_separator));
                    if (pf.ptm >= 0) Post_translational_modifications = fields[pf.ptm];
                    if (pf.local_confidence >= 0)
                    {
                        Local_confidence = fields[pf.local_confidence].Split(" ".ToCharArray()).ToList().Select(x => Convert.ToInt32(x)).ToArray();
                        if (Local_confidence.Length != Cleaned_sequence.Length)
                            throw new Exception("The length of the sequence and amount of local score do not match.");
                    }
                    if (pf.mode >= 0) Fragmentation_mode = fields[pf.mode];

                    // Initialise list
                    Other_scans = new List<string>();
                }
                catch (Exception e)
                {
                    throw new Exception($"ERROR: Could not parse this line into Peaks format.\nLINE: {line}\nERROR MESSAGE: {e.Message}\n{e.StackTrace}");
                }
            }
            /// <summary> Generate HTML with all metainformation from the PEAKS data. </summary>
            /// <returns> Returns an HTML string with the metainformation. </returns>
            public override string ToHTML()
            {
                var output = new StringBuilder();
                output.Append("<h2>Meta Information from PEAKS</h2>");

                // Look for each field if it is defined, otherwise leave it out
                if (ScanID != null)
                    output.Append($"<h3>Scan Identifier</h3>\n<p>{ScanID}</p>");

                if (Original_tag != null && Local_confidence != null)
                {
                    output.Append($"<h3>Original Sequence (length={Original_tag.Length})</h3>\n<div class='original-sequence' style='--max-value:100'>");
                    int original_offset = 0;

                    for (int i = 0; i < Cleaned_sequence.Length; i++)
                    {
                        output.Append($"<div><div class='coverage-depth-wrapper'><span class='coverage-depth-bar' style='--value:{Local_confidence[i]}'></span></div><p>{Cleaned_sequence[i]}</p>");

                        if (original_offset < Original_tag.Length - 2 && Original_tag[original_offset + 1] == '(')
                        {
                            output.Append("<p class='modification'>");
                            original_offset += 2;
                            while (Original_tag[original_offset] != ')')
                            {
                                output.Append(Original_tag[original_offset]);
                                original_offset++;
                                if (original_offset > Original_tag.Length - 2)
                                {
                                    break;
                                }
                            }
                            output.Append("</p>");
                        }
                        output.Append("</div>");
                        original_offset++;
                    }
                    output.Append("</div>");
                }
                if (Post_translational_modifications != null)
                    output.Append($"<h3>Posttranslational Modifications</h3>\n<p>{Post_translational_modifications}</p>");

                if (Source_File != null)
                    output.Append($"<h3>Source File</h3>\n<p>{Source_File}</p>");

                if (Fraction != null)
                    output.Append($"<h3>Fraction</h3>\n<p>{Fraction}</p>");

                if (Feature != null)
                    output.Append($"<h3>Scan Feature</h3>\n<p>{Feature}</p>");

                if (Confidence >= 0)
                    output.Append($"<h3>Confidence score</h3>\n<p>{Confidence}</p>");

                if (Mass_over_charge >= 0)
                    output.Append($"<h3>Mass Charge Ratio</h3>\n<p>{Mass_over_charge}</p>");

                if (Mass >= 0)
                    output.Append($"<h3>Mass</h3>\n<p>{Mass}</p>");

                if (Charge >= 0)
                    output.Append($"<h3>Charge</h3>\n<p>{Charge}</p>");

                if (Retention_time >= 0)
                    output.Append($"<h3>Retention Time</h3>\n<p>{Retention_time}</p>");

                if (Area >= 0)
                    output.Append($"<h3>Area</h3>\n<p>{Area}</p>");

                if (Parts_per_million >= 0)
                    output.Append($"<h3>Parts Per Million</h3>\n<p>{Parts_per_million}</p>");

                if (Fragmentation_mode != null)
                    output.Append($"<h3>Fragmentation Mode</h3>\n<p>{Fragmentation_mode}</p>");

                if (Other_scans.Count() > 0)
                    output.Append($"<h3>Also found in scans</h3>\n<p>{Other_scans.Aggregate("", (a, b) => (a + " " + b))}</p>");

                output.Append(File.ToHTML());

                return output.ToString();
            }
        }
        /// <summary>
        /// A identifier for a file, to hold information about where reads originate from.
        /// </summary>
        public class FileIdentifier
        {
            /// <value>The absolute path to the file</value>
            public string Path { get { return path; } set { path = System.IO.Path.GetFullPath(value); } }
            string path;
            /// <value>The name or identifier given to the file</value>
            public string Name;
            /// <summary>
            /// Creating a new FileIdentifier
            /// </summary>
            /// <param name="path_input">The path to the file, can be a relative path</param>
            /// <param name="name">The identifier given to the file</param>
            public FileIdentifier(string path_input, string name)
            {
                path = System.IO.Path.GetFullPath(path_input);
                Name = name;
            }
            /// <summary>
            /// To create a blank instance of FileIdentifier
            /// </summary>
            public FileIdentifier()
            {
                path = "";
                Name = "";
            }
            /// <summary>
            /// To generate HTML for use in the metadata sidebar in the HTML report
            /// </summary>
            /// <returns>A string containing the HTML</returns>
            public string ToHTML()
            {
                return $"<h3>Originating file identifier</h3>\n<p>{Name}</p>\n<h3>Originating file path</h3>\n<p>{Path}</p>";
            }
        }
    }
    /// <summary>
    /// To contain definitions for file formats
    /// </summary>
    public class FileFormat
    {
        /// <summary>
        /// To contain all options for PEAKS file formats
        /// </summary>
        public class Peaks
        {
            /// <summary>
            /// The position of this column in this peaks file format version.
            /// </summary>
            public int fraction, source_file, feature, scan, peptide, tag_length, alc, length, mz, z, rt, area, mass, ppm, ptm, local_confidence, tag, mode = -1;
            /// <summary>
            /// An older version of a PEAKS export
            /// </summary>
            /// <returns>The fileformat</returns>
            public static FileFormat.Peaks OldFormat()
            {
                var pf = new FileFormat.Peaks();
                pf.scan = 0;
                pf.peptide = 1;
                pf.tag_length = 2;
                pf.alc = 3;
                pf.length = 4;
                pf.mz = 5;
                pf.z = 6;
                pf.rt = 7;
                pf.area = 8;
                pf.mass = 9;
                pf.ppm = 10;
                pf.ptm = 11;
                pf.local_confidence = 12;
                pf.tag = 13;
                pf.mode = 14;
                return pf;
            }
            /// <summary>
            /// A newer version of a PEAKS export
            /// </summary>
            /// <returns>The fileformat</returns>
            public static FileFormat.Peaks NewFormat()
            {
                var pf = new FileFormat.Peaks();
                pf.fraction = 0;
                pf.source_file = 1;
                pf.feature = 2;
                pf.peptide = 3;
                pf.scan = 4;
                pf.tag_length = 5;
                pf.alc = 6;
                pf.length = 7;
                pf.mz = 8;
                pf.z = 9;
                pf.rt = 10;
                pf.area = 11;
                pf.mass = 12;
                pf.ppm = 13;
                pf.ptm = 14;
                pf.local_confidence = 15;
                pf.tag = 16;
                pf.mode = 17;
                return pf;
            }
            /// <summary>
            /// An custom version of a PEAKS fileformat 
            /// </summary>
            /// <returns>The fileformat</returns>
            public static FileFormat.Peaks CustomFormat(int fraction, int source_file, int feature, int scan, int peptide, int tag_length, int alc, int length, int mz, int z, int rt, int area, int mass, int ppm, int ptm, int local_confidence, int tag, int mode)
            {
                var pf = new FileFormat.Peaks();
                pf.fraction = fraction;
                pf.source_file = source_file;
                pf.feature = feature;
                pf.peptide = peptide;
                pf.scan = scan;
                pf.tag_length = tag_length;
                pf.alc = alc;
                pf.length = length;
                pf.mz = mz;
                pf.z = z;
                pf.rt = rt;
                pf.area = area;
                pf.mass = mass;
                pf.ppm = ppm;
                pf.ptm = ptm;
                pf.local_confidence = local_confidence;
                pf.tag = tag;
                pf.mode = mode;
                return pf;
            }
        }
    }
}