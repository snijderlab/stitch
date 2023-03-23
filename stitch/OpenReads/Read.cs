using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using HtmlGenerator;
using System.Text.Json.Serialization;
using System.IO;
using static Stitch.Fragmentation;
using System.Text;

namespace Stitch {
    /// <summary> A class to hold all metadata handling in one place. </summary>
    public static class ReadFormat {
        /// <summary> To save metadata of a read/path of which could be used in calculations to determine the likelihood
        ///  of certain assignments or for quality control or ease of use for humans. </summary>
        public abstract class General {
            /// <summary> The sequence of this read. </summary>
            public ReadSequence Sequence { get; set; }

            /// <summary> The Identifier of the originating file. </summary>
            public FileIdentifier File { get => FileRange != null ? FileRange.Value.File.Identifier : new FileIdentifier(); }
            public readonly FileRange? FileRange;

            /// <summary> The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier. </summary>
            public string Identifier { get; protected set; }

            /// <summary> The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier. </summary>
            public string ClassIdentifier { get; protected set; }

            /// <summary> The Identifier of the read escaped for use in filenames. </summary>
            public string EscapedIdentifier { get; protected set; }

            /// <summary> Returns the overall intensity for this read. It is used to determine which read to
            /// choose if multiple reads exist at the same spot. </summary>
            public virtual double Intensity { get { return intensity; } set { intensity = value; } }
            double intensity = 1.0;

            /// <summary> Contains the total area as measured by mass spectrometry to be able to report this back to the user
            /// and help him/her get a better picture of the validity of the data. </summary>
            public double TotalArea = 0;

            /// <summary> Contains the information needed to find this metadata in a raw file. </summary>
            public virtual List<(string RawFile, int Scan, string ProForma, bool XleDisambiguation)> ScanNumbers { get; protected set; } = new List<(string, int, string, bool)>();

            /// <summary> To generate a HTML representation of this metadata for use in the HTML report. </summary>
            /// <returns> An HtmlBuilder containing the MetaData. </returns>
            public abstract HtmlBuilder ToHTML();
            public List<IASM> SupportingSpectra = new();

            protected NameFilter nameFilter;

            /// <summary> To create the base metadata for a read. </summary>
            /// <param name="file">The identifier of the originating file.</param>
            /// <param name="id">The identifier of the read.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public General(AminoAcid[] sequence, FileRange? file, string id, NameFilter filter, string classId = null, double[] positional_score = null) {
                Sequence = new ReadSequence(sequence == null ? new AminoAcid[0] : sequence, positional_score ?? Enumerable.Repeat(Intensity, (sequence == null ? 0 : sequence.Length)).ToArray());
                nameFilter = filter;
                FileRange = file;
                Identifier = id;
                ClassIdentifier = classId ?? Identifier;

                if (filter != null) {
                    var (escaped_id, bst, count) = filter.EscapeIdentifier(Identifier);
                    if (count <= 1) {
                        EscapedIdentifier = escaped_id;
                    } else {
                        Identifier = $"{Identifier}_{count:D3}";
                        EscapedIdentifier = $"{escaped_id}_{count:D3}";
                    }
                }
            }

            /// <summary> Parse the original tag as kind of pro forma (using brackets '(' or without brackets altogether) and present any modifications alongside the local confidence.</summary>
            /// <param name="read"></param>
            /// <param name="original_tag"></param>
            /// <param name="html"></param>
            static protected void AnnotatedLocalScore(General read, string original_tag, HtmlBuilder html) {
                // Create a display of the sequence with local confidence and modifications (if present)
                html.OpenAndClose(HtmlTag.h3, "", $"Original sequence");
                html.Open(HtmlTag.div, "class='original-sequence' style='--max-value:100'");
                int original_offset = 0;

                bool IsInner(char c) {
                    return char.IsDigit(c) || c == '.' || c == '+' || c == '-' || c == ')';
                }
                bool IsStart(char c) {
                    return c == '(' || c == '+' || c == '-';
                }

                // Show N terminal modifications
                if (original_offset < original_tag.Length - 1 && IsStart(original_tag[original_offset])) {
                    html.Open(HtmlTag.div, $"style='--value:0'");
                    html.OpenAndClose(HtmlTag.p, "", "⚬");
                    html.Open(HtmlTag.p, "class='modification'");
                    var temp = original_offset;
                    original_offset += 1; // skip past the detected number
                    while (original_offset < original_tag.Length && IsInner(original_tag[original_offset])) {
                        original_offset++;
                    }
                    html.Content(original_tag.Substring(temp, original_offset - temp).Trim(new char[] { '(', ')' }));
                    html.Close(HtmlTag.p);
                    html.Close(HtmlTag.div);
                }

                for (int i = 0; i < read.Sequence.Length; i++) {
                    html.Open(HtmlTag.div, $"style='--value:{read.Sequence.PositionalScore[i] * 100}'");
                    html.OpenAndClose(HtmlTag.p, "", read.Sequence.AminoAcids[i].ToString());

                    if (original_offset < original_tag.Length - 1 && IsStart(original_tag[original_offset + 1])) {
                        html.Open(HtmlTag.p, "class='modification'");
                        var temp = original_offset + 1;
                        original_offset += 2; // skip past the detected number
                        while (original_offset < original_tag.Length && IsInner(original_tag[original_offset])) {
                            original_offset++;
                        }
                        html.Content(original_tag.Substring(temp, original_offset - temp).Trim(new char[] { '(', ')' }));
                        html.Close(HtmlTag.p);
                    } else {
                        original_offset++;
                    }
                    html.Close(HtmlTag.div);
                }

                // Show C terminal modifications
                if (original_offset < original_tag.Length - 1 && IsStart(original_tag[original_offset])) {
                    html.Open(HtmlTag.div, $"style='--value:0'");
                    html.OpenAndClose(HtmlTag.p, "", "⚬");
                    html.Open(HtmlTag.p, "class='modification'");
                    var temp = original_offset;
                    original_offset += 1; // skip past the detected number
                    while (original_offset < original_tag.Length && IsInner(original_tag[original_offset])) {
                        original_offset++;
                    }
                    html.Content(original_tag.Substring(temp, original_offset - temp).Trim(new char[] { '(', ')' }));
                    html.Close(HtmlTag.p);
                    html.Close(HtmlTag.div);
                }
                html.Close(HtmlTag.div);
            }
        }

        /// <summary> A metadata instance to contain no metadata so reads without metadata can also be handled. </summary>
        public class Simple : General {
            /// <summary> Create a new Simple MetaData. </summary>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            /// <param name="identifier">The identifier for this read, does not have to be unique, the name filter will enforce that.</param>
            public Simple(AminoAcid[] sequence, FileRange? file = null, NameFilter filter = null, string identifier = "R", double[] doc = null) : base(sequence, file, identifier, filter) {
                if (doc != null)
                    this.Sequence.SetPositionalScore(doc);
            }

            /// <summary> Returns Simple MetaData to HTML. </summary>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.Add(File.ToHTML());
                return html;
            }
        }

        /// <summary> A class to hold meta information from fasta data. </summary>
        public class Fasta : General {
            /// <summary> The identifier from the fasta file. </summary>
            public readonly string FastaHeader;
            public List<(HelperFunctionality.Annotation Type, string Sequence)> AnnotatedSequence = null;

            /// <summary> To create a new metadata instance with this metadata. </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="fastaHeader">The header for this read as in the fasta file, without the '>'.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            public Fasta(AminoAcid[] sequence, string identifier, string fastaHeader, FileRange? file, NameFilter filter, string classIdentifier = null)
                : base(sequence, file, identifier, filter, classIdentifier) {
                this.FastaHeader = fastaHeader;
            }

            /// <summary> Generate HTML with all meta information from the fasta data. </summary>
            /// <returns> Returns an HTML string with the meta information. </returns>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from fasta");
                html.OpenAndClose(HtmlTag.h3, "", "Identifier");
                html.OpenAndClose(HtmlTag.p, "", Identifier);
                html.OpenAndClose(HtmlTag.h3, "", "Fasta header");
                html.OpenAndClose(HtmlTag.p, "", FastaHeader);
                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());
                return html;
            }
        }

        /// <summary> A struct to hold meta information from PEAKS data. </summary>
        public class Peaks : General {
            /// <summary> The Fraction number of the peptide. </summary>
            public string Fraction = null;

            /// <summary> The source file out of which the peptide was generated. </summary>
            public string SourceFile = null;

            /// <summary> The feature of the peptide. </summary>
            public string Feature = null;

            /// <summary> The scan identifier of the peptide. </summary>
            public string ScanID = null;

            /// <summary> The sequence with modifications of the peptide. </summary>
            public string OriginalTag = null;

            /// <summary> The DeNovoScore as reported by PEAKS 10.5 </summary>
            public int DeNovoScore = -1;

            /// <summary> The confidence score of the peptide. </summary>
            public int Confidence = -1;

            /// <summary> m/z of the peptide. </summary>
            public double MassOverCharge = -1;

            /// <summary> z of the peptide. </summary>
            public int Charge = -1;

            /// <summary> Retention time of the peptide. </summary>
            public double RetentionTime = -1;

            /// <summary> Predicted retention time of the peptide. </summary>
            public string PredictedRetentionTime = null;

            /// <summary> Area of the peak of the peptide.</summary>
            public double Area = -1;

            /// <summary> Mass of the peptide.</summary>
            public double Mass = -1;

            /// <summary> PPM of the peptide. </summary>
            public double PartsPerMillion = -1;

            public bool XleDisambiguation;

            /// <summary> The intensity of this read, find out how it should be handled if it if later updated. </summary>
            double intensity = 1;
            public override double Intensity {
                get {
                    if (Area != 0 && nameFilter != null && nameFilter.MinimalPeaksArea != Double.MaxValue && nameFilter.MaximalPeaksArea != Double.MinValue) {
                        return (Math.Log10(Area) - nameFilter.MinimalPeaksArea) / (nameFilter.MaximalPeaksArea - nameFilter.MinimalPeaksArea);
                    }
                    return 1;
                }
                set { if (!double.IsNaN(value)) intensity = value; }
            }

            public override List<(string RawFile, int Scan, string ProForma, bool XleDisambiguation)> ScanNumbers {
                get {
                    var output = new List<(string, int, string, bool)>();
                    foreach (var scan in ScanID.Split(' ').Select(s => int.Parse(s.Split(':').Last())))
                        output.Add((SourceFile, scan, OriginalTag, XleDisambiguation));
                    return output;
                }
            }

            /// <summary> Posttranslational Modifications of the peptide. </summary>
            public string Post_translational_modifications = null;

            /// <summary> Fragmentation mode used to generate the peptide. </summary>
            public string FragmentationMode = null;

            /// <summary> To create a new metadata instance with this metadata. </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            private Peaks(AminoAcid[] sequence, FileRange? file, string identifier, NameFilter filter, double[] positional_score = null) : base(sequence, file, identifier, filter, null, positional_score) { }

            /// <summary> Tries to create a PeaksMeta struct based on a CSV line in PEAKS format. </summary>
            /// <param name="parseFile"> The file to parse, this contains the full file bu only the designated line will be parsed. </param>
            /// <param name="linenumber"> The index of the line to be parsed. </param>
            /// <param name="separator"> The separator used in CSV. </param>
            /// <param name="decimalseparator"> The separator used in decimals. </param>
            /// <param name="pf">FileFormat of the PEAKS file.</param>
            /// <param name="file">Identifier for the originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            /// <returns>A ParseResult with the peaks metadata instance and/or the errors. </returns>
            public static ParseResult<Peaks> ParseLine(ParsedFile parse_file, int linenumber, char separator, char decimalseparator, PeaksFileFormat pf, NameFilter filter, ScoringMatrix alphabet, string RawDataDirectory, bool xleDisambiguation) {
                var out_either = new ParseResult<Peaks>();
                var range = new FileRange(new Position(linenumber, 0, parse_file), new Position(linenumber, parse_file.Lines[linenumber].Length, parse_file));

                char current_decimal_separator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray()[0];

                var fields = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);

                if (String.IsNullOrWhiteSpace(parse_file.Lines[linenumber])) return out_either; // Ignore empty lines

                if (fields.Count < 5) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(range, $"Line has too low amount of fields ({fields.Count})", "Make sure the used separator is correct.", ""));
                    return out_either;
                }

                // Some helper functions
                int ConvertToInt(int pos) {
                    return InputNameSpace.ParseHelper.ConvertToInt(fields[pos].Text.Replace(decimalseparator, current_decimal_separator), fields[pos].Pos).UnwrapOrDefault(out_either, -1);
                }

                double ConvertToDouble(int pos) {
                    return InputNameSpace.ParseHelper.ConvertToDouble(fields[pos].Text.Replace(decimalseparator, current_decimal_separator), fields[pos].Pos).UnwrapOrDefault(out_either, -1);
                }

                bool CheckFieldExists(int pos) {
                    if (pos > fields.Count - 1) {
                        out_either.AddMessage(new InputNameSpace.ErrorMessage(range, "Line too short", $"Line misses field {pos} while this is necessary in this peaks format."));
                        return false;
                    }
                    return true;
                }

                if (!(pf.scan >= 0 && CheckFieldExists(pf.scan))) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(range, "Missing identifier", "Each Peaks line needs a ScanID to use as an identifier"));
                    return out_either;
                }
                string original_peptide;
                AminoAcid[] sequence;
                if (pf.peptide >= 0 && CheckFieldExists(pf.peptide)) {
                    original_peptide = fields[pf.peptide].Text;
                    sequence = AminoAcid.FromString(new string(original_peptide.Where(x => Char.IsUpper(x) && Char.IsLetter(x)).ToArray()), alphabet, range).UnwrapOrDefault(out_either, new AminoAcid[0]);
                } else {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(range, "Missing identifier", "Each Peaks line needs a Peptide to use as an identifier"));
                    return out_either;
                }

                var peaks = new Peaks(sequence, range, fields[pf.scan].Text, filter);
                out_either.Value = peaks;
                peaks.OriginalTag = HelperFunctionality.FromSloppyProForma(original_peptide);
                peaks.XleDisambiguation = xleDisambiguation;

                // Get all the properties of this peptide and save them in the MetaData
                if (pf.fraction >= 0 && CheckFieldExists(pf.fraction))
                    peaks.Fraction = fields[pf.fraction].Text;

                if (pf.source_file >= 0 && CheckFieldExists(pf.source_file))
                    peaks.SourceFile = (string.IsNullOrWhiteSpace(RawDataDirectory) ? "" : RawDataDirectory + (RawDataDirectory.EndsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar)) + fields[pf.source_file].Text;

                if (pf.feature >= 0 && CheckFieldExists(pf.feature))
                    peaks.Feature = fields[pf.feature].Text;

                if (pf.scan >= 0 && CheckFieldExists(pf.scan))
                    peaks.ScanID = fields[pf.scan].Text;

                if (pf.de_novo_score >= 0 && CheckFieldExists(pf.de_novo_score))
                    peaks.DeNovoScore = ConvertToInt(pf.de_novo_score);

                if (pf.alc >= 0 && CheckFieldExists(pf.alc))
                    peaks.Confidence = ConvertToInt(pf.alc);

                if (pf.mz >= 0 && CheckFieldExists(pf.mz))
                    peaks.MassOverCharge = ConvertToDouble(pf.mz);

                if (pf.z >= 0 && CheckFieldExists(pf.z))
                    peaks.Charge = ConvertToInt(pf.z);

                if (pf.rt >= 0 && CheckFieldExists(pf.rt))
                    peaks.RetentionTime = ConvertToDouble(pf.rt);

                if (pf.predicted_rt >= 0 && CheckFieldExists(pf.predicted_rt))
                    peaks.PredictedRetentionTime = fields[pf.predicted_rt].Text;

                if (pf.area >= 0 && CheckFieldExists(pf.area))
                    peaks.Area = ConvertToDouble(pf.area);

                if (pf.mass >= 0 && CheckFieldExists(pf.mass))
                    peaks.Mass = ConvertToDouble(pf.mass);

                if (pf.ppm >= 0 && CheckFieldExists(pf.ppm))
                    peaks.PartsPerMillion = ConvertToDouble(pf.ppm);

                if (pf.ptm >= 0 && CheckFieldExists(pf.ptm))
                    peaks.Post_translational_modifications = fields[pf.ptm].Text;

                if (pf.local_confidence >= 0 && CheckFieldExists(pf.local_confidence)) {
                    try {
                        if (!peaks.Sequence.SetPositionalScore(fields[pf.local_confidence].Text.Split(" ".ToCharArray()).ToList().Select(x => Convert.ToInt32(x) / 100.0).ToArray()))
                            out_either.AddMessage(new InputNameSpace.ErrorMessage(fields[pf.local_confidence].Pos, "Local confidence invalid", "The length of the local confidence is not equal to the length of the sequence"));
                    } catch {
                        out_either.AddMessage(new InputNameSpace.ErrorMessage(fields[pf.local_confidence].Pos, "Local confidence invalid", "One of the confidences is not a number"));
                    }
                }

                if (pf.mode >= 0 && CheckFieldExists(pf.mode))
                    peaks.FragmentationMode = fields[pf.mode].Text;

                // Calculate intensity
                if (peaks.Area != 0) {
                    var pArea = Math.Log10(peaks.Area);
                    if (pArea < filter.MinimalPeaksArea) filter.MinimalPeaksArea = pArea;
                    if (pArea > filter.MaximalPeaksArea) filter.MaximalPeaksArea = pArea;
                }

                peaks.TotalArea = peaks.Area;

                return out_either;
            }

            public Stitch.ReadFormat.Peaks Clone() {
                var meta = new Stitch.ReadFormat.Peaks(this.Sequence.AminoAcids, this.FileRange, this.Identifier, this.nameFilter, this.Sequence.PositionalScore);
                meta.Area = this.Area;
                meta.Charge = this.Charge;
                meta.Confidence = this.Confidence;
                meta.DeNovoScore = this.DeNovoScore;
                meta.Feature = new string(this.Feature);
                meta.Fraction = new string(this.Fraction);
                meta.FragmentationMode = new string(this.FragmentationMode);
                meta.intensity = this.intensity;
                meta.Mass = this.Mass;
                meta.MassOverCharge = this.MassOverCharge;
                meta.OriginalTag = new string(this.OriginalTag);
                meta.PartsPerMillion = this.PartsPerMillion;
                meta.PredictedRetentionTime = new string(this.PredictedRetentionTime);
                meta.RetentionTime = this.RetentionTime;
                meta.ScanID = new string(this.ScanID);
                meta.SourceFile = new string(this.SourceFile);
                return meta;
            }

            /// <summary> Generate HTML with all meta information from the PEAKS data. </summary>
            /// <returns> Returns an HTML string with the meta information. </returns>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from PEAKS");

                // Look for each field if it is defined, otherwise leave it out
                if (ScanID != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Scan Identifier");
                    html.OpenAndClose(HtmlTag.p, "", ScanID.ToString());
                }

                // Create a display of the sequence with local confidence and modifications (if present)
                if (OriginalTag != null && Sequence.PositionalScore != null) {
                    General.AnnotatedLocalScore(this, OriginalTag, html);
                }

                if (Post_translational_modifications != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Posttranslational Modifications");
                    html.OpenAndClose(HtmlTag.p, "", Post_translational_modifications);
                }

                if (SourceFile != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Source File");
                    html.OpenAndClose(HtmlTag.p, "", SourceFile);
                }

                if (Fraction != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Fraction");
                    html.OpenAndClose(HtmlTag.p, "", Fraction);
                }

                if (Feature != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Scan Feature");
                    html.OpenAndClose(HtmlTag.p, "", Feature);
                }

                if (DeNovoScore >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "De Novo Score");
                    html.OpenAndClose(HtmlTag.p, "", DeNovoScore.ToString());
                }

                if (Confidence >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "ConfidenceScore");
                    html.OpenAndClose(HtmlTag.p, "", Confidence.ToString());
                }

                if (MassOverCharge >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "m/z");
                    html.OpenAndClose(HtmlTag.p, "", MassOverCharge.ToString());
                }

                if (Mass >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "Mass");
                    html.OpenAndClose(HtmlTag.p, "", Mass.ToString());
                };

                if (Charge >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "Charge");
                    html.OpenAndClose(HtmlTag.p, "", Charge.ToString());
                }

                if (RetentionTime >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "Retention Time");
                    html.OpenAndClose(HtmlTag.p, "", RetentionTime.ToString());
                }

                if (PredictedRetentionTime != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Predicted Retention Time");
                    html.OpenAndClose(HtmlTag.p, "", PredictedRetentionTime.ToString());
                }

                if (Area >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "Area");
                    html.OpenAndClose(HtmlTag.p, "", Area.ToString("G4"));
                }

                if (PartsPerMillion >= 0) {
                    html.OpenAndClose(HtmlTag.h3, "", "Parts Per Million");
                    html.OpenAndClose(HtmlTag.p, "", PartsPerMillion.ToString());
                }

                if (FragmentationMode != null) {
                    html.OpenAndClose(HtmlTag.h3, "", "Fragmentation mode");
                    html.OpenAndClose(HtmlTag.p, "", FragmentationMode);
                }

                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());

                return html;
            }
        }

        /// <summary> A struct to hold meta information from MaxNovo data. </summary>
        public class MaxNovo : General {

            /// <summary> The source file out of which the peptide was generated. </summary>
            public string SourceFile = null;

            /// <summary> The scan identifier of the peptide. </summary>
            public uint ScanID;

            /// <summary> The sequence with modifications of the peptide. </summary>
            public string SequenceWithModifications = null;

            /// <summary> The sequence expression with modifications of the peptide. </summary>
            public string SequenceExpression = null;

            /// <summary> The Score as reported </summary>
            public double Score = -1;

            /// <summary> Mass of the peptide.</summary>
            public double Mass = -1;

            /// <summary> m/z of the peptide. </summary>
            public double MassOverCharge = -1;

            /// <summary> z of the peptide. </summary>
            public int Charge = -1;

            /// <summary> Retention time of the peptide. </summary>
            public double RetentionTime = -1;

            /// <summary> base peak intensity of the peak of the peptide, comparable to Peaks area?.</summary>
            public double BasePeakIntensity = -1;

            /// <summary> The fragmentation mode used. </summary>
            public string FragmentationMode = null;

            /// <summary> The MaxNovo experiment name. </summary>
            public string Experiment = null;

            public bool XleDisambiguation;

            /// <summary> The intensity of this read. </summary>
            double intensity = 1;
            public override double Intensity {
                get {
                    return intensity;
                }
                set { if (!double.IsNaN(value)) intensity = value; }
            }

            public override List<(string RawFile, int Scan, string ProForma, bool XleDisambiguation)> ScanNumbers {
                get {
                    return new List<(string RawFile, int Scan, string ProForma, bool XleDisambiguation)> { (SourceFile, (int)ScanID, SequenceWithModifications, XleDisambiguation) };
                }
            }

            /// <summary> To create a new empty metadata instance with this metadata. </summary>
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            private MaxNovo(AminoAcid[] sequence, FileRange? file, string identifier, NameFilter filter) : base(sequence, file, identifier, filter) { }

            /// <summary> Tries to create a MaxNovo struct based on a CSV line in MaxNovo format. </summary>
            /// <param name="parseFile"> The file to parse, this contains the full file bu only the designated line will be parsed. </param>
            /// <param name="linenumber"> The index of the line to be parsed. </param>
            /// <param name="separator"> The separator used in CSV. </param>
            /// <param name="decimalseparator"> The separator used in decimals. </param>
            /// <param name="pf">FileFormat of the MaxNovo file.</param>
            /// <param name="file">Identifier for the originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            /// <returns>A ParseResult with the MaxNovo metadata instance or the errors. </returns>
            public static ParseResult<MaxNovo> ParseLine(ParsedFile parse_file, int linenumber, NameFilter filter, ScoringMatrix scoring_matrix, string RawDataDirectory, bool xleDisambiguation) {
                var out_either = new ParseResult<MaxNovo>();
                var range = new FileRange(new Position(linenumber, 0, parse_file), new Position(linenumber, parse_file.Lines[linenumber].Length, parse_file));

                var fields = InputNameSpace.ParseHelper.SplitLine('\t', linenumber, parse_file);

                if (String.IsNullOrWhiteSpace(parse_file.Lines[linenumber])) return out_either; // Ignore empty lines

                if (fields.Count != 80) {
                    out_either.AddMessage(new InputNameSpace.ErrorMessage(range, $"Line has too low amount of fields ({fields.Count})", "", ""));
                    return out_either;
                }

                // Some helper functions
                int ConvertToInt(int pos) {
                    return InputNameSpace.ParseHelper.ConvertToInt(fields[pos].Text, fields[pos].Pos).UnwrapOrDefault(out_either, -1);
                }

                uint ConvertToUint(int pos) {
                    return InputNameSpace.ParseHelper.ConvertToUint(fields[pos].Text, fields[pos].Pos).UnwrapOrDefault(out_either, 0);
                }

                double ConvertToDouble(int pos) {
                    return InputNameSpace.ParseHelper.ConvertToDouble(fields[pos].Text, fields[pos].Pos).UnwrapOrDefault(out_either, -1);
                }

                var sequence = SimplifiedProFormaFromExpression(fields[41].Text);
                AminoAcid[] aa_sequence = AminoAcid.FromString(sequence.PureAA, scoring_matrix, fields[41].Pos).UnwrapOrDefault(out_either, new AminoAcid[0]);
                var scan = ConvertToUint(1);

                var output = new MaxNovo(aa_sequence, range, $"MN:{scan}", filter);
                out_either.Value = output;
                output.SequenceExpression = fields[41].Text;
                output.SequenceWithModifications = sequence.Modifications;
                output.XleDisambiguation = xleDisambiguation;
                output.ScanID = scan;
                output.RetentionTime = ConvertToDouble(2);
                output.BasePeakIntensity = ConvertToDouble(7);
                output.MassOverCharge = ConvertToDouble(16);
                output.Mass = ConvertToDouble(17);
                output.Charge = ConvertToInt(18);
                output.FragmentationMode = fields[21].Text;
                output.Score = ConvertToDouble(52);
                output.Experiment = fields[37].Text;
                output.SourceFile = (String.IsNullOrEmpty(RawDataDirectory) ? "./" : RawDataDirectory + (RawDataDirectory.EndsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar)) + fields[0].Text + ".raw";

                return out_either;
            }

            static (string PureAA, string Modifications) SimplifiedProFormaFromExpression(string expression) {
                var pure_aa = new StringBuilder();
                var modifications = new StringBuilder();
                bool inside_modification = false;
                var open_parentheses = 0; // ()
                var open_braces = 0; // {}
                var must_include = 0;

                for (int index = 0; index < expression.Length; index++) {
                    switch (expression[index]) {
                        case '(':
                            if (open_parentheses == 0) {
                                if (must_include == open_braces) modifications.Append('[');
                            } else {
                                if (must_include == open_braces) modifications.Append('(');
                            }
                            open_parentheses += 1;
                            inside_modification = true;
                            break;
                        case ')':
                            open_parentheses -= 1;
                            if (open_parentheses == 0) {
                                inside_modification = false;
                                if (must_include == open_braces) modifications.Append(']');
                            } else
                                if (must_include == open_braces) modifications.Append(')');
                            break;
                        case '[':
                            if (inside_modification && must_include == open_braces) modifications.Append('[');
                            break;
                        case ']':
                            if (inside_modification && must_include == open_braces) modifications.Append(']');
                            break;
                        case '{':
                            if (string.Compare(expression, index, "{L|I}", 0, 5) == 0) {
                                modifications.Append('J');
                                if (!inside_modification) pure_aa.Append('J');
                                index += 4; // Last 1 will be done by for header
                                break;
                            }
                            if (must_include == open_braces)
                                must_include += 1;
                            open_braces += 1;
                            break;
                        case '}':
                            open_braces -= 1;
                            break;
                        case '|':
                            if (must_include == open_braces)
                                must_include -= 1;
                            break;
                        default:
                            if (must_include == open_braces) {
                                modifications.Append(expression[index]);
                                if (!inside_modification) pure_aa.Append(expression[index]);
                            }
                            break;
                    }
                }

                if (modifications.ToString().Contains("[]"))
                    Console.WriteLine($"To {modifications} From {expression}");

                return (pure_aa.ToString(), modifications.ToString());
            }

            /// <summary> Generate HTML with all meta information from the MaxNovo data. </summary>
            /// <returns> Returns an HTML string with the meta information. </returns>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from MaxNovo");

                html.OpenAndClose(HtmlTag.h3, "", "Scan Identifier");
                html.OpenAndClose(HtmlTag.p, "", ScanID.ToString());

                General.AnnotatedLocalScore(this, SequenceWithModifications, html);

                html.OpenAndClose(HtmlTag.h3, "", "Ambiguous sequence expression");
                html.OpenAndClose(HtmlTag.p, "", SequenceExpression);

                html.OpenAndClose(HtmlTag.h3, "", "Source File");
                html.OpenAndClose(HtmlTag.p, "", SourceFile);

                html.OpenAndClose(HtmlTag.h3, "", "Score");
                html.OpenAndClose(HtmlTag.p, "", Score.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "m/z");
                html.OpenAndClose(HtmlTag.p, "", MassOverCharge.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "Mass");
                html.OpenAndClose(HtmlTag.p, "", Mass.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "Charge");
                html.OpenAndClose(HtmlTag.p, "", Charge.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "Retention Time");
                html.OpenAndClose(HtmlTag.p, "", RetentionTime.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "Area");
                html.OpenAndClose(HtmlTag.p, "", BasePeakIntensity.ToString("G4"));

                html.OpenAndClose(HtmlTag.h3, "", "Fragmentation mode");
                html.OpenAndClose(HtmlTag.p, "", FragmentationMode);

                html.OpenAndClose(HtmlTag.h3, "", "Experiment");
                html.OpenAndClose(HtmlTag.p, "", Experiment);

                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());

                return html;
            }
        }

        public class Combined : General {
            /// <summary> Returns the overall intensity for this read. It is used to determine which read to
            /// choose if multiple reads exist at the same spot. </summary>
            public override double Intensity { get => Children.Count == 0 ? 0.0 : Children.Average(m => m.Intensity); }

            /// <summary> Contains the total area as measured by mass spectrometry to be able to report this back to the user
            /// and help him/her get a better picture of the validity of the data. </summary>
            public new double TotalArea { get => Children.Count == 0 ? 0.0 : Children.Sum(m => m.TotalArea); }
            public override List<(string RawFile, int Scan, string ProForma, bool XleDisambiguation)> ScanNumbers {
                get => Children.SelectMany(c => c.ScanNumbers).ToList();
            }

            public readonly List<General> Children = new List<General>();

            /// <summary> To generate a HTML representation of this metadata for use in the HTML report. </summary>
            /// <returns>A string containing the MetaData.</returns>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from Multiple reads");
                html.OpenAndClose(HtmlTag.h3, "", "Number of combined reads");
                html.OpenAndClose(HtmlTag.p, "", Children.Count.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Intensity");
                html.OpenAndClose(HtmlTag.p, "", Intensity.ToString("G4"));
                html.OpenAndClose(HtmlTag.h3, "", "TotalArea");
                html.OpenAndClose(HtmlTag.p, "", TotalArea.ToString("G4"));
                html.Add(Sequence.RenderToHtml());
                html.Add(HTMLNameSpace.HTMLGraph.Bargraph(HTMLNameSpace.HTMLGraph.AnnotateDOCData(Sequence.PositionalScore.Select(a => (double)a).ToList()), new HtmlGenerator.HtmlBuilder("Positional Score"), null, null, 1));
                foreach (var child in Children) {
                    html.Add(child.ToHTML());
                }
                return html;
            }

            public Combined(AminoAcid[] sequence, NameFilter filter, List<General> children) : base(sequence, null, "Combined", filter) {
                Children = children;
                this.SupportingSpectra.AddRange(children.SelectMany(c => c.SupportingSpectra));
            }

            public void AddChild(General read) {
                Children.Add(read);
                Sequence.UpdatePositionalScore(read.Sequence.PositionalScore, Children.Count - 1);
            }
        }

        /// <summary> A metadata instance to contain no metadata so reads without metadata can also be handled. </summary>
        public abstract class Novor : General {
            /// <summary> The fraction where this peptide was found. </summary>
            public string Fraction;
            /// <summary> The scan number of this peptide. </summary>
            public int Scan;
            /// <summary> The M over Z value of this peptide. </summary>
            public double MZ;
            /// <summary> The Z (or charge) of this peptide. </summary>
            public int Z;
            /// <summary> The Novor score of this peptide (0-100) </summary>
            public double Score;
            /// <summary> The mass of this peptide. </summary>
            public double Mass;
            /// <summary> The error for this peptide in ppm. </summary>
            public double Error;
            /// <summary> The original sequence with possible modifications. </summary>
            public string OriginalSequence;

            /// <summary> The intensity of this read </summary>
            double intensity = 1;
            public override double Intensity {
                get { return Score / 100; }
                set { if (!double.IsNaN(value)) intensity = value; }
            }

            /// <summary> Create a new Novor MetaData. </summary>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public Novor(AminoAcid[] sequence, FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string original_sequence) : base(sequence, file, "N", filter) {
                this.Fraction = fraction;
                this.Scan = scan;
                this.MZ = mz;
                this.Z = z;
                this.Score = score;
                this.Mass = mass;
                this.Error = error;
                this.OriginalSequence = original_sequence;
            }

            /// <summary> Returns Simple MetaData to HTML. </summary>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from Novor");
                html.OpenAndClose(HtmlTag.h3, "", "Fraction");
                html.OpenAndClose(HtmlTag.p, "", Fraction);
                html.OpenAndClose(HtmlTag.h3, "", "Scan");
                html.OpenAndClose(HtmlTag.p, "", Scan.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "m/z");
                html.OpenAndClose(HtmlTag.p, "", MZ.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Charge");
                html.OpenAndClose(HtmlTag.p, "", Z.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Score");
                html.OpenAndClose(HtmlTag.p, "", Score.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Mass");
                html.OpenAndClose(HtmlTag.p, "", Mass.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Error");
                html.OpenAndClose(HtmlTag.p, "", Error.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Original Sequence");
                html.OpenAndClose(HtmlTag.p, "", OriginalSequence);
                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());
                return html;
            }
        }

        public class NovorDeNovo : Novor {
            /// <summary> The database sequence with possible modifications. </summary>
            public string DBSequence;
            public NovorDeNovo(AminoAcid[] sequence, FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string original_sequence, string databaseSequence)
            : base(sequence, file, filter, fraction, scan, mz, z, score, mass, error, original_sequence) {
                this.DBSequence = databaseSequence;
            }

            public override HtmlBuilder ToHTML() {
                var html = base.ToHTML();
                html.OpenAndClose(HtmlTag.h3, "", "DBSequence");
                html.OpenAndClose(HtmlTag.p, "", DBSequence);
                return html;
            }
        }

        public class NovorPSMS : Novor {
            /// <summary> The read ID. </summary>
            public string ID;
            /// <summary> The read ID. </summary>
            public int Proteins;
            public NovorPSMS(AminoAcid[] sequence, FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string original_sequence, string id, int proteins)
            : base(sequence, file, filter, fraction, scan, mz, z, score, mass, error, original_sequence) {
                this.ID = id;
                this.Proteins = proteins;
            }

            public override HtmlBuilder ToHTML() {
                var html = base.ToHTML();
                html.OpenAndClose(HtmlTag.h3, "", "ID");
                html.OpenAndClose(HtmlTag.p, "", ID);
                html.OpenAndClose(HtmlTag.h3, "", "Proteins");
                html.OpenAndClose(HtmlTag.p, "", Proteins.ToString());
                return html;
            }
        }

        /// <summary> A metadata instance to contain reads from a structural source (mmCIF files). </summary>
        public class ModelAngeloRead : General {
            /// <summary> The original chain name (_atom_site.label_asym_id). </summary>
            public string ChainName;
            /// <summary> The original chain name (_atom_site.auth_asym_id). </summary>
            public string AuthChainName;

            /// <summary> Create a new structural read MetaData. </summary>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public ModelAngeloRead(AminoAcid[] sequence, double[] confidence, FileRange file, NameFilter filter, string chain_name, string auth_chain_name) : base(sequence, file, "S", filter) {
                this.ChainName = chain_name;
                this.AuthChainName = auth_chain_name;
                this.Sequence.SetPositionalScore(confidence);
                this.Intensity = confidence.Average();
            }

            /// <summary> Returns Simple MetaData to HTML. </summary>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from a structural read");
                html.OpenAndClose(HtmlTag.h3, "", "Chain Name");
                html.OpenAndClose(HtmlTag.p, "", this.ChainName);
                html.TagWithHelp(HtmlTag.h3, "Auth Chain Name", new HtmlBuilder(HTMLNameSpace.HTMLHelp.AuthChainName));
                html.OpenAndClose(HtmlTag.p, "", this.AuthChainName);

                // Create a display of the sequence with local confidence
                if (Sequence.PositionalScore != null) {
                    General.AnnotatedLocalScore(this, AminoAcid.ArrayToString(this.Sequence.AminoAcids), html);
                }

                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());
                return html;
            }
        }

        /// <summary> A metadata instance to contain reads from a casanovo denovo run. </summary>
        public class Casanovo : General {
            /// <summary> The sequence with modifications of the peptide. </summary>
            public string OriginalSequence;
            public int PSM_ID;
            public string SearchEngine;
            public double Score;
            public int Charge;
            public double ExperimentalMz;
            public double TheoreticalMz;
            public string SpectraRef;


            /// <summary> Create a new casanovo read MetaData. </summary>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public Casanovo(AminoAcid[] sequence, double score, double[] confidence, FileRange range, NameFilter filter, string original_sequence, int psm_id, string search_engine, int charge, double experimental_mz, double theoretical_mz, string spectra_ref) : base(sequence, range, $"C{psm_id}", filter) {
                this.OriginalSequence = original_sequence;
                this.Sequence.SetPositionalScore(confidence);
                this.Intensity = 0.5 + score / 2;
                this.Score = score;
                this.PSM_ID = psm_id;
                this.SearchEngine = search_engine;
                this.Charge = charge;
                this.ExperimentalMz = experimental_mz;
                this.TheoreticalMz = theoretical_mz;
                this.SpectraRef = spectra_ref;
            }

            /// <summary> Returns casanovo MetaData to HTML. </summary>
            public override HtmlBuilder ToHTML() {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information");

                // Create a display of the sequence with local confidence
                if (Sequence.PositionalScore != null)
                    General.AnnotatedLocalScore(this, OriginalSequence, html);

                html.OpenAndClose(HtmlTag.h3, "", "Score");
                html.OpenAndClose(HtmlTag.p, "", this.Score.ToString("G4"));

                html.OpenAndClose(HtmlTag.h3, "", "Search Engine");
                html.OpenAndClose(HtmlTag.p, "", this.SearchEngine);

                html.OpenAndClose(HtmlTag.h3, "", "Charge");
                html.OpenAndClose(HtmlTag.p, "", this.Charge.ToString());

                html.OpenAndClose(HtmlTag.h3, "", "Mz");
                html.OpenAndClose(HtmlTag.p, "", $"{this.TheoreticalMz:G4} (theoretical) {this.ExperimentalMz:G4} (experimental)");

                html.OpenAndClose(HtmlTag.h3, "", "Error");
                html.OpenAndClose(HtmlTag.p, "", $"{Math.Abs(this.TheoreticalMz - this.ExperimentalMz):G4} (Th) {Math.Abs(this.TheoreticalMz - this.ExperimentalMz) / this.TheoreticalMz * 1e6:G4} (ppm)");

                html.OpenAndClose(HtmlTag.h3, "", "Spectra");
                html.OpenAndClose(HtmlTag.p, "", this.SpectraRef);

                html.Add(Sequence.RenderToHtml());
                html.Add(File.ToHTML());
                return html;
            }
        }

        /// <summary> A identifier for a file, to hold information about where reads originate from. </summary>
        public class FileIdentifier {
            /// <summary>The absolute path to the file.</summary>
            public string Path { get { return path; } set { path = System.IO.Path.GetFullPath(value); } }
            string path;

            /// <summary>The name or identifier given to the file.</summary>
            public string Name;

            public InputNameSpace.KeyValue Origin;

            /// <summary> Creating a new FileIdentifier. </summary>
            /// <param name="pathInput">The path to the file, can be a relative path.</param>
            /// <param name="name">The identifier given to the file.</param>
            /// <param name="origin">The place where is path is defined in a batchfile or derivatives.</param>
            public FileIdentifier(string pathInput, string name, InputNameSpace.KeyValue origin) {
                path = System.IO.Path.GetFullPath(pathInput);
                Name = name;
                Origin = origin;
            }

            /// <summary> To create a blank instance of FileIdentifier. </summary>
            public FileIdentifier() {
                path = "";
                Name = "";
            }

            /// <summary> To generate HTML for use in the metadata sidebar in the HTML report. </summary>
            /// <returns>A string containing the HTML.</returns>
            public HtmlBuilder ToHTML() {
                if (String.IsNullOrEmpty(path)) return new HtmlBuilder();
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h3, "", "Originating file");
                html.Open(HtmlTag.p, "");
                html.Content(Name + " ");
                html.OpenAndClose(HtmlTag.a, $"href='file:///{path}' target='_blank'", Path);
                html.Close(HtmlTag.p);
                return html;
            }

            public override string ToString() {
                return $"Path: {path}\nName: {Name}";
            }

            public override bool Equals(object obj) {
                if (obj is FileIdentifier that) {
                    return this.Path == that.Path && this.Name == that.Name && this.Origin == that.Origin;
                }
                return false;
            }

            public override int GetHashCode() {
                return 23 + 397 * this.Path.GetHashCode() + 17 * 397 * this.Name.GetHashCode() + 17 * 17 * 397 * this.Origin.GetHashCode();
            }
        }
    }
}