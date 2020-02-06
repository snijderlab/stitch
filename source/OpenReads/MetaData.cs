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
            /// The Identifier of the originating file.
            /// </summary>
            public FileIdentifier File;

            /// <summary>
            /// To generate (an) HTML element(s) from this MetaData.
            /// </summary>
            /// <returns>A string containing the MetaData.</returns>
            public abstract string ToHTML();

            /// <summary>
            /// To create an instance.
            /// </summary>
            /// <param name="file">The identifier of the originating file.</param>
            public IMetaData(FileIdentifier file)
            {
                File = file;
            }
        }

        /// <summary>
        /// A metadata instance to contain no metadata so reads without metadata can also be handled.
        /// </summary>
        public class None : IMetaData
        {
            /// <summary>
            /// Create a new None MetaData.
            /// </summary>
            public None(FileIdentifier file) : base(file) { }

            /// <summary>
            /// Returns None MetaData to HTML (which is always "").
            /// </summary>
            /// <returns>"".</returns>
            public override string ToHTML()
            {
                return File.ToHTML();
            }
        }

        /// <summary> A class to hold metainformation from fasta data. </summary>
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

            /// <summary> The source file out of which the peptide was generated. </summary>
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
            /// <param name="pf">FileFormat of the PEAKS file.</param>
            /// <param name="file">Identifier for the originating file.</param>
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

                    // Initialize list
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
            /// <value>The absolute path to the file.</value>
            public string Path { get { return path; } set { path = System.IO.Path.GetFullPath(value); } }
            string path;

            /// <value>The name or identifier given to the file.</value>
            public string Name;

            /// <summary>
            /// Creating a new FileIdentifier.
            /// </summary>
            /// <param name="path_input">The path to the file, can be a relative path.</param>
            /// <param name="name">The identifier given to the file.</param>
            public FileIdentifier(string path_input, string name)
            {
                path = System.IO.Path.GetFullPath(path_input);
                Name = name;
            }

            /// <summary>
            /// To create a blank instance of FileIdentifier.
            /// </summary>
            public FileIdentifier()
            {
                path = "";
                Name = "";
            }

            /// <summary>
            /// To generate HTML for use in the metadata sidebar in the HTML report.
            /// </summary>
            /// <returns>A string containing the HTML.</returns>
            public string ToHTML()
            {
                return $"<h2>Originating File</h2><h3>Originating file identifier</h3>\n<p>{Name}</p>\n<h3>Originating file path</h3>\n<p>{Path}</p>";
            }
        }
    }
}