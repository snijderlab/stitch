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
        /// To save metadata of a read/path of which could be used in calculations to determine the likelyhood
        ///  of certain assignments or for quality control or ease of use for humans.
        /// </summary>
        public abstract class IMetaData
        {
            /// <summary>
            /// The Identifier of the originating file.
            /// </summary>
            public readonly FileIdentifier File;

            protected string identifier;
            /// <summary>
            /// The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier.
            /// </summary>
            public string Identifier { get { return identifier; } }

            protected string classIdentifier;
            /// <summary>
            /// The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier.
            /// </summary>
            public string ClassIdentifier { get { return classIdentifier; } }

            protected string escapedIdentifier;
            /// <summary>
            /// The Identifier of the read escaped for use in filenames.
            /// </summary>
            public string EscapedIdentifier { get { return escapedIdentifier; } }

            /// <summary>
            /// Returns the positional score for this read, so for every position the confidence.
            /// The exact meaning differs for all read types but overall it is used in the depth of coverage calculations.
            /// </summary>
            public virtual double[] PositionalScore { get { return new double[0]; } }

            /// <summary>
            /// Returns the overall intensity for this read. It is used to determine which read to 
            /// choose if multiple reads exist at the same spot.
            /// </summary>
            public virtual double Intensity { get { return intensity; } set { intensity = value; } }
            double intensity = 1.0;

            /// <summary>
            /// Contains the total area as measured by mass spectrometry to be able to report this back to the user 
            /// and help him/her get a better picture of the validity of the data.
            /// </summary>
            public double TotalArea = 0;

            /// <summary>
            /// To generate a HTML representation of this metadata for use in the HTML report.
            /// </summary>
            /// <returns>A string containing the MetaData.</returns>
            public abstract string ToHTML();

            protected NameFilter nameFilter;

            /// <summary>
            /// To create the base metadata for a read.
            /// </summary>
            /// <param name="file">The identifier of the originating file.</param>
            /// <param name="identifier_">The identifier of the read.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public IMetaData(FileIdentifier file, string identifier_, NameFilter filter, string classIdentifier_ = null)
            {
                nameFilter = filter;
                File = file;
                identifier = identifier_;
                classIdentifier = classIdentifier_ ?? identifier_;

                if (filter != null)
                {
                    var (escaped_id, bst, count) = filter.EscapeIdentifier(identifier);
                    if (count <= 1)
                    {
                        escapedIdentifier = escaped_id;
                    }
                    else
                    {
                        identifier = $"{identifier}_{count:D3}";
                        escapedIdentifier = $"{escaped_id}_{count:D3}";
                    }
                }
            }
        }

        /// <summary>
        /// A metadata instance to contain no metadata so reads without metadata can also be handled.
        /// </summary>
        public class Simple : IMetaData
        {
            /// <summary>
            /// Create a new Simple MetaData.
            /// </summary>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            /// <param name="identifier">The identifier for this read, does not have to be unique, the namefilter will enforce that.</param>
            public Simple(FileIdentifier file, NameFilter filter, string identifier = "R") : base(file, identifier, filter) { }

            /// <summary>
            /// Returns Simple MetaData to HTML.
            /// </summary>
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
            public readonly string FastaHeader;
            public List<(string Type, string Sequence)> AnnotatedSequence = null;

            /// <summary>
            /// To create a new metadata instance with this metadata.
            /// </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="fastaHeader">The header for this read as in the fasta file, without the '>'.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            public Fasta(string identifier, string fastaHeader, FileIdentifier file, NameFilter filter, string classIdentifier = null)
                : base(file, identifier, filter, classIdentifier)
            {
                this.FastaHeader = fastaHeader;
            }

            /// <summary> Generate HTML with all metainformation from the fasta data. </summary>
            /// <returns> Returns an HTML string with the metainformation. </returns>
            public override string ToHTML()
            {
                return $"<h2>Meta Information from fasta</h2>\n<h3>Identifier</h3>\n<p>{Identifier}</p>\n<h3>Fasta header</h3>\n<p>{FastaHeader}</p>{File.ToHTML()}";
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

            /// <summary> The DeNovoScore as reported by PEAKS 10.5 </summary>
            public int DeNovoScore = -1;

            /// <summary> The confidence score of the peptide. </summary>
            public int Confidence = -1;

            /// <summary> m/z of the peptide. </summary>
            public double Mass_over_charge = -1;

            /// <summary> z of the peptide. </summary>
            public int Charge = -1;

            /// <summary> Retention time of the peptide. </summary>
            public double Retention_time = -1;

            /// <summary> Predicted retention time of the peptide. </summary>
            public string PredictedRetentionTime = null;

            /// <summary> Area of the peak of the peptide.</summary>
            public double Area = -1;

            /// <summary> Mass of the peptide.</summary>
            public double Mass = -1;

            /// <summary> PPM of the peptide. </summary>
            public double Parts_per_million = -1;

            /// <summary>
            /// The intensity of this read, find out how it should be handled if it if later updated. #TODO
            /// </summary>
            double intensity = 1;
            public override double Intensity
            {
                get
                {
                    if (Area != 0 && nameFilter != null && nameFilter.MinimalPeaksArea != Double.MaxValue && nameFilter.MaximalPeaksArea != Double.MinValue)
                    {
                        return 1 + (Math.Log10(Area) - nameFilter.MinimalPeaksArea) / (nameFilter.MaximalPeaksArea - nameFilter.MinimalPeaksArea);
                    }
                    return 1;
                }
                set { if (value != double.NaN) intensity = value; }
            }

            /// <summary> Posttranslational Modifications of the peptide. </summary>
            public string Post_translational_modifications = null;

            /// <summary> Local confidence scores of the peptide. Determined as a fraction based on the local confidence of the total intensity of this read. </summary>
            public int[] Local_confidence = null;
            public override double[] PositionalScore { get { return Local_confidence.Select(a => (double)a / 100 * Intensity).ToArray(); } }

            /// <summary> Fragmentation mode used to generate the peptide. </summary>
            public string Fragmentation_mode = null;

            /// <summary> Other scans giving the same sequence. </summary>
            public List<string> Other_scans = null;

            /// <summary>
            /// To create a new metadata instance with this metadata.
            /// </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            private Peaks(FileIdentifier file, string identifier, NameFilter filter) : base(file, identifier, filter) { }

            /// <summary> Tries to create a PeaksMeta struct based on a CSV line in PEAKS format. </summary>
            /// <param name="parseFile"> The file to parse, this contains the full file bu only the designated line will be parsed. </param>
            /// <param name="linenumber"> The index of the to be parsed. </param>
            /// <param name="separator"> The separator used in CSV. </param>
            /// <param name="decimalseparator"> The separator used in decimals. </param>
            /// <param name="pf">FileFormat of the PEAKS file.</param>
            /// <param name="file">Identifier for the originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            /// <returns>A ParseResult with the peaks metadata instance and/or the errors. </returns>
            public static ParseResult<Peaks> ParseLine(ParsedFile parsefile, int linenumber, char separator, char decimalseparator, FileFormat.Peaks pf, FileIdentifier file, NameFilter filter)
            {
                var result = new ParseResult<Peaks>();

                char current_decimal_separator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray()[0];

                List<string> fields = new List<string>();
                List<FileRange> positions = new List<FileRange>();
                int lastpos = 0;
                string line = parsefile.Lines[linenumber];

                if (String.IsNullOrWhiteSpace(line))
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parsefile), "Line is empty", "", "", true));
                    return result;
                }

                // Find the fields on this line
                for (int pos = 0; pos < line.Length; pos++)
                {
                    if (line[pos] == separator)
                    {
                        fields.Add(line.Substring(lastpos, pos - lastpos));
                        positions.Add(new FileRange(new Position(linenumber, lastpos + 1, parsefile), new Position(linenumber, pos + 1, parsefile)));
                        lastpos = pos + 1;
                    }
                }

                fields.Add(line.Substring(lastpos, line.Length - lastpos));
                positions.Add(new FileRange(new Position(linenumber, lastpos, parsefile), new Position(linenumber, line.Length - 1, parsefile)));

                if (fields.Count < 5)
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parsefile), $"Line has too low amount of fields ({fields.Count})", "Make sure the used separator is correct.", ""));
                    return result;
                }

                // Some helper functions
                int ConvertToInt(int pos)
                {
                    return InputNameSpace.ParseHelper.ConvertToInt(fields[pos].Replace(decimalseparator, current_decimal_separator), positions[pos]).ReturnOrDefault(-1);
                }

                double ConvertToDouble(int pos)
                {
                    return InputNameSpace.ParseHelper.ConvertToDouble(fields[pos].Replace(decimalseparator, current_decimal_separator), positions[pos]).ReturnOrDefault(-1);
                }

                bool CheckFieldExists(int pos)
                {
                    if (pos > fields.Count() - 1)
                    {
                        result.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parsefile), "Line too short", $"Line misses field {pos} while this is necessary in this peaks format."));
                        return false;
                    }
                    return true;
                }

                if (!(pf.scan >= 0 && CheckFieldExists(pf.scan)))
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parsefile), "Missing identifier", "Each Peaks line needs a ScanID to use as an identifier"));
                    return result;
                }
                var peaks = new Peaks(file, fields[pf.scan], filter);
                result.Value = peaks;

                // Get all the properties of this peptide and save them in the MetaData 
                if (pf.fraction >= 0 && CheckFieldExists(pf.fraction))
                    peaks.Fraction = fields[pf.fraction];

                if (pf.source_file >= 0 && CheckFieldExists(pf.source_file))
                    peaks.Source_File = fields[pf.source_file];

                if (pf.feature >= 0 && CheckFieldExists(pf.feature))
                    peaks.Feature = fields[pf.feature];

                if (pf.scan >= 0 && CheckFieldExists(pf.scan))
                    peaks.ScanID = fields[pf.scan];

                if (pf.peptide >= 0 && CheckFieldExists(pf.peptide))
                {
                    peaks.Original_tag = fields[pf.peptide];
                    peaks.Cleaned_sequence = new string(peaks.Original_tag.Where(x => Char.IsUpper(x) && Char.IsLetter(x)).ToArray());
                }

                if (pf.de_novo_score >= 0 && CheckFieldExists(pf.de_novo_score))
                    peaks.DeNovoScore = ConvertToInt(pf.de_novo_score);

                if (pf.alc >= 0 && CheckFieldExists(pf.alc))
                    peaks.Confidence = ConvertToInt(pf.alc);

                if (pf.mz >= 0 && CheckFieldExists(pf.mz))
                    peaks.Mass_over_charge = ConvertToDouble(pf.mz);

                if (pf.z >= 0 && CheckFieldExists(pf.z))
                    peaks.Charge = ConvertToInt(pf.z);

                if (pf.rt >= 0 && CheckFieldExists(pf.rt))
                    peaks.Retention_time = ConvertToDouble(pf.rt);

                if (pf.predicted_rt >= 0 && CheckFieldExists(pf.predicted_rt))
                    peaks.PredictedRetentionTime = fields[pf.predicted_rt];

                if (pf.area >= 0 && CheckFieldExists(pf.area))
                    peaks.Area = ConvertToDouble(pf.area);

                if (pf.mass >= 0 && CheckFieldExists(pf.mass))
                    peaks.Mass = ConvertToDouble(pf.mass);

                if (pf.ppm >= 0 && CheckFieldExists(pf.ppm))
                    peaks.Parts_per_million = ConvertToDouble(pf.ppm);

                if (pf.ptm >= 0 && CheckFieldExists(pf.ptm))
                    peaks.Post_translational_modifications = fields[pf.ptm];

                if (pf.local_confidence >= 0 && CheckFieldExists(pf.local_confidence))
                {
                    try
                    {
                        peaks.Local_confidence = fields[pf.local_confidence].Split(" ".ToCharArray()).ToList().Select(x => Convert.ToInt32(x)).ToArray();
                        if (peaks.Local_confidence.Length != peaks.Cleaned_sequence.Length)
                            result.AddMessage(new InputNameSpace.ErrorMessage(positions[pf.local_confidence], "Local confidence invalid", "The length of the local confidence is not equal to the length of the sequence"));
                    }
                    catch
                    {
                        result.AddMessage(new InputNameSpace.ErrorMessage(positions[pf.local_confidence], "Local confidence invalid", "One of the confidences is not a number"));
                    }
                }

                if (pf.mode >= 0 && CheckFieldExists(pf.mode))
                    peaks.Fragmentation_mode = fields[pf.mode];

                // Initialize other scans list
                peaks.Other_scans = new List<string>();

                // Calculate intensity
                if (peaks.Area != 0)
                {
                    var pArea = Math.Log10(peaks.Area);
                    if (pArea < filter.MinimalPeaksArea) filter.MinimalPeaksArea = pArea;
                    if (pArea > filter.MaximalPeaksArea) filter.MaximalPeaksArea = pArea;
                }

                peaks.TotalArea = peaks.Area;

                return result;
            }

            public MetaData.Peaks Clone()
            {
                var meta = new MetaData.Peaks(this.File, this.identifier, this.nameFilter);
                meta.Area = this.Area;
                meta.Charge = this.Charge;
                meta.Cleaned_sequence = new string(this.Cleaned_sequence);
                meta.Confidence = this.Confidence;
                meta.DeNovoScore = this.DeNovoScore;
                meta.Feature = new string(this.Feature);
                meta.Fraction = new string(this.Fraction);
                meta.Fragmentation_mode = new string(this.Fragmentation_mode);
                meta.intensity = this.intensity;
                meta.Local_confidence = (int[])this.Local_confidence.Clone();
                meta.Mass = this.Mass;
                meta.Mass_over_charge = this.Mass_over_charge;
                meta.Original_tag = new string(this.Original_tag);
                meta.Other_scans = new List<string>(this.Other_scans);
                meta.Parts_per_million = this.Parts_per_million;
                meta.PredictedRetentionTime = new string(this.PredictedRetentionTime);
                meta.Retention_time = this.Retention_time;
                meta.ScanID = new string(this.ScanID);
                meta.Source_File = new string(this.Source_File);
                return meta;
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

                // Create a display of the sequence with local confidence and modifications (if present)
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

                if (DeNovoScore >= 0)
                    output.Append($"<h3>De Novo Score</h3>\n<p>{DeNovoScore}</p>");

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

                if (PredictedRetentionTime != null)
                    output.Append($"<h3>Predicted Retention Time</h3>\n<p>{PredictedRetentionTime}</p>");

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
            /// <summary>The absolute path to the file.</summary>
            public string Path { get { return path; } set { path = System.IO.Path.GetFullPath(value); } }
            string path;

            /// <summary>The name or identifier given to the file.</summary>
            public string Name;

            /// <summary> To signify if this FileIdentifier points to a file or to nothing. </summary>
            bool RefersToFile;

            public InputNameSpace.KeyValue Origin;

            /// <summary>
            /// Creating a new FileIdentifier.
            /// </summary>
            /// <param name="path_input">The path to the file, can be a relative path.</param>
            /// <param name="name">The identifier given to the file.</param>
            /// <param name="origin">The place where is path is defined in a batchfile or derivatives.</param>
            public FileIdentifier(string path_input, string name, InputNameSpace.KeyValue origin)
            {
                path = System.IO.Path.GetFullPath(path_input);
                Name = name;
                RefersToFile = true;
                Origin = origin;
            }

            /// <summary>
            /// To create a blank instance of FileIdentifier.
            /// </summary>
            public FileIdentifier()
            {
                path = "";
                Name = "";
                RefersToFile = false;
            }

            /// <summary>
            /// To generate HTML for use in the metadata sidebar in the HTML report.
            /// </summary>
            /// <returns>A string containing the HTML.</returns>
            public string ToHTML()
            {
                if (!RefersToFile) return "";
                return $"<h2>Originating File</h2><h3>Originating file identifier</h3>\n<p>{Name}</p>\n<h3>Originating file path</h3>\n<a href='file:///{path}' target='_blank'>{Path}</a>";
            }

            public string Display()
            {
                return $"Path: {path}\nName: {Name}";
            }
        }
    }
}