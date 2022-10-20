using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using HtmlGenerator;

namespace Stitch
{
    /// <summary> A class to hold all metadata handling in one place. </summary> 
    public static class ReadMetaData
    {
        /// <summary> To save metadata of a read/path of which could be used in calculations to determine the likelihood
        ///  of certain assignments or for quality control or ease of use for humans. </summary>
        public abstract class IMetaData
        {
            /// <summary> The Identifier of the originating file. </summary> 
            public FileIdentifier File { get => FileRange != null ? FileRange.Value.File.Identifier : new FileIdentifier(); }
            public readonly FileRange? FileRange;

            /// <summary> The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier. </summary> 
            public string Identifier { get; protected set; }

            /// <summary> The Identifier of the read as the original, with possibly a number at the end if multiple reads had this same identifier. </summary> 
            public string ClassIdentifier { get; protected set; }

            /// <summary> The Identifier of the read escaped for use in filenames. </summary> 
            public string EscapedIdentifier { get; protected set; }

            /// <summary> Returns the positional score for this read, so for every position the confidence.
            /// The exact meaning differs for all read types but overall it is used in the depth of coverage calculations. </summary> 
            public virtual double[] PositionalScore { get { return new double[0]; } }

            /// <summary> Returns the overall intensity for this read. It is used to determine which read to 
            /// choose if multiple reads exist at the same spot. </summary> 
            public virtual double Intensity { get { return intensity; } set { intensity = value; } }
            double intensity = 1.0;

            /// <summary> Contains the total area as measured by mass spectrometry to be able to report this back to the user 
            /// and help him/her get a better picture of the validity of the data. </summary> 
            public double TotalArea = 0;

            /// <summary> Contains the information needed to find this metadata in a raw file. </summary> 
            public virtual List<(string RawFile, int Scan, string OriginalTag)> ScanNumbers { get; protected set; } = new List<(string, int, string)>();

            /// <summary> To generate a HTML representation of this metadata for use in the HTML report. </summary> 
            /// <returns>An HtmlBuilder containing the MetaData.</returns>
            public abstract HtmlBuilder ToHTML();

            protected NameFilter nameFilter;

            /// <summary> To create the base metadata for a read. </summary> 
            /// <param name="file">The identifier of the originating file.</param>
            /// <param name="id">The identifier of the read.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public IMetaData(FileRange? file, string id, NameFilter filter, string classId = null)
            {
                nameFilter = filter;
                FileRange = file;
                Identifier = id;
                ClassIdentifier = classId ?? Identifier;

                if (filter != null)
                {
                    var (escaped_id, bst, count) = filter.EscapeIdentifier(Identifier);
                    if (count <= 1)
                    {
                        EscapedIdentifier = escaped_id;
                    }
                    else
                    {
                        Identifier = $"{Identifier}_{count:D3}";
                        EscapedIdentifier = $"{escaped_id}_{count:D3}";
                    }
                }
            }
        }

        /// <summary> A metadata instance to contain no metadata so reads without metadata can also be handled. </summary>
        public class Simple : IMetaData
        {
            /// <summary> Create a new Simple MetaData. </summary> 
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            /// <param name="identifier">The identifier for this read, does not have to be unique, the name filter will enforce that.</param>
            public Simple(FileRange? file, NameFilter filter, string identifier = "R") : base(file, identifier, filter) { }

            /// <summary> Returns Simple MetaData to HTML. </summary> 
            public override HtmlBuilder ToHTML()
            {
                return File.ToHTML();
            }
        }

        /// <summary> A class to hold meta information from fasta data. </summary>
        public class Fasta : IMetaData
        {
            /// <summary> The identifier from the fasta file. </summary> 
            public readonly string FastaHeader;
            public List<(HelperFunctionality.Annotation Type, string Sequence)> AnnotatedSequence = null;

            /// <summary> To create a new metadata instance with this metadata. </summary> 
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="fastaHeader">The header for this read as in the fasta file, without the '>'.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            public Fasta(string identifier, string fastaHeader, FileRange? file, NameFilter filter, string classIdentifier = null)
                : base(file, identifier, filter, classIdentifier)
            {
                this.FastaHeader = fastaHeader;
            }

            /// <summary> Generate HTML with all meta information from the fasta data. </summary>
            /// <returns> Returns an HTML string with the meta information. </returns>
            public override HtmlBuilder ToHTML()
            {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from fasta");
                html.OpenAndClose(HtmlTag.h3, "", "Identifier");
                html.OpenAndClose(HtmlTag.p, "", Identifier);
                html.OpenAndClose(HtmlTag.h3, "", "Fasta header");
                html.OpenAndClose(HtmlTag.p, "", FastaHeader);
                html.Add(File.ToHTML());
                return html;
            }
        }

        /// <summary> A struct to hold meta information from PEAKS data. </summary>
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

            /// <summary> The intensity of this read, find out how it should be handled if it if later updated. </summary> 
            double intensity = 1;
            public override double Intensity
            {
                get
                {
                    if (Area != 0 && nameFilter != null && nameFilter.MinimalPeaksArea != Double.MaxValue && nameFilter.MaximalPeaksArea != Double.MinValue)
                    {
                        return (Math.Log10(Area) - nameFilter.MinimalPeaksArea) / (nameFilter.MaximalPeaksArea - nameFilter.MinimalPeaksArea);
                    }
                    return 1;
                }
                set { if (!double.IsNaN(value)) intensity = value; }
            }

            public override List<(string, int, string)> ScanNumbers
            {
                get
                {
                    var output = new List<(string, int, string)>();
                    foreach (var scan in ScanID.Split(' ').Select(s => int.Parse(s.Split(':').Last())))
                        output.Add((Source_File, scan, Original_tag));
                    return output;
                }
            }

            /// <summary> Posttranslational Modifications of the peptide. </summary>
            public string Post_translational_modifications = null;

            /// <summary> Local confidence scores of the peptide. Determined as a fraction based on the local confidence of the total intensity of this read. </summary>
            public int[] Local_confidence = null;
            public override double[] PositionalScore { get { return Local_confidence.Select(a => (double)a / 100).ToArray(); } }

            /// <summary> Fragmentation mode used to generate the peptide. </summary>
            public string Fragmentation_mode = null;

            /// <summary> To create a new metadata instance with this metadata. </summary> 
            /// <param name="identifier">The fasta identifier.</param>
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            private Peaks(FileRange? file, string identifier, NameFilter filter) : base(file, identifier, filter) { }

            /// <summary> Tries to create a PeaksMeta struct based on a CSV line in PEAKS format. </summary>
            /// <param name="parseFile"> The file to parse, this contains the full file bu only the designated line will be parsed. </param>
            /// <param name="linenumber"> The index of the line to be parsed. </param>
            /// <param name="separator"> The separator used in CSV. </param>
            /// <param name="decimalseparator"> The separator used in decimals. </param>
            /// <param name="pf">FileFormat of the PEAKS file.</param>
            /// <param name="file">Identifier for the originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier.</param>
            /// <returns>A ParseResult with the peaks metadata instance and/or the errors. </returns>
            public static ParseResult<Peaks> ParseLine(ParsedFile parse_file, int linenumber, char separator, char decimalseparator, FileFormat.Peaks pf, NameFilter filter)
            {
                var result = new ParseResult<Peaks>();
                var range = new FileRange(new Position(linenumber, 0, parse_file), new Position(linenumber, parse_file.Lines[linenumber].Length, parse_file));

                char current_decimal_separator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray()[0];

                var fields = InputNameSpace.ParseHelper.SplitLine(separator, linenumber, parse_file);

                if (String.IsNullOrWhiteSpace(parse_file.Lines[linenumber]))
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(new Position(linenumber, 1, parse_file), "Line is empty", "", "", true));
                    return result;
                }

                if (fields.Count < 5)
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(range, $"Line has too low amount of fields ({fields.Count})", "Make sure the used separator is correct.", ""));
                    return result;
                }

                // Some helper functions
                int ConvertToInt(int pos)
                {
                    return InputNameSpace.ParseHelper.ConvertToInt(fields[pos].Text.Replace(decimalseparator, current_decimal_separator), fields[pos].Pos).UnwrapOrDefault(-1);
                }

                double ConvertToDouble(int pos)
                {
                    return InputNameSpace.ParseHelper.ConvertToDouble(fields[pos].Text.Replace(decimalseparator, current_decimal_separator), fields[pos].Pos).UnwrapOrDefault(-1);
                }

                bool CheckFieldExists(int pos)
                {
                    if (pos > fields.Count - 1)
                    {
                        result.AddMessage(new InputNameSpace.ErrorMessage(range, "Line too short", $"Line misses field {pos} while this is necessary in this peaks format."));
                        return false;
                    }
                    return true;
                }

                if (!(pf.scan >= 0 && CheckFieldExists(pf.scan)))
                {
                    result.AddMessage(new InputNameSpace.ErrorMessage(range, "Missing identifier", "Each Peaks line needs a ScanID to use as an identifier"));
                    return result;
                }
                var peaks = new Peaks(range, fields[pf.scan].Text, filter);
                result.Value = peaks;

                // Get all the properties of this peptide and save them in the MetaData 
                if (pf.fraction >= 0 && CheckFieldExists(pf.fraction))
                    peaks.Fraction = fields[pf.fraction].Text;

                if (pf.source_file >= 0 && CheckFieldExists(pf.source_file))
                    peaks.Source_File = fields[pf.source_file].Text;

                if (pf.feature >= 0 && CheckFieldExists(pf.feature))
                    peaks.Feature = fields[pf.feature].Text;

                if (pf.scan >= 0 && CheckFieldExists(pf.scan))
                    peaks.ScanID = fields[pf.scan].Text;

                if (pf.peptide >= 0 && CheckFieldExists(pf.peptide))
                {
                    peaks.Original_tag = fields[pf.peptide].Text;
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
                    peaks.PredictedRetentionTime = fields[pf.predicted_rt].Text;

                if (pf.area >= 0 && CheckFieldExists(pf.area))
                    peaks.Area = ConvertToDouble(pf.area);

                if (pf.mass >= 0 && CheckFieldExists(pf.mass))
                    peaks.Mass = ConvertToDouble(pf.mass);

                if (pf.ppm >= 0 && CheckFieldExists(pf.ppm))
                    peaks.Parts_per_million = ConvertToDouble(pf.ppm);

                if (pf.ptm >= 0 && CheckFieldExists(pf.ptm))
                    peaks.Post_translational_modifications = fields[pf.ptm].Text;

                if (pf.local_confidence >= 0 && CheckFieldExists(pf.local_confidence))
                {
                    try
                    {
                        peaks.Local_confidence = fields[pf.local_confidence].Text.Split(" ".ToCharArray()).ToList().Select(x => Convert.ToInt32(x)).ToArray();
                        if (peaks.Local_confidence.Length != peaks.Cleaned_sequence.Length)
                            result.AddMessage(new InputNameSpace.ErrorMessage(fields[pf.local_confidence].Pos, "Local confidence invalid", "The length of the local confidence is not equal to the length of the sequence"));
                    }
                    catch
                    {
                        result.AddMessage(new InputNameSpace.ErrorMessage(fields[pf.local_confidence].Pos, "Local confidence invalid", "One of the confidences is not a number"));
                    }
                }

                if (pf.mode >= 0 && CheckFieldExists(pf.mode))
                    peaks.Fragmentation_mode = fields[pf.mode].Text;

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

            public ReadMetaData.Peaks Clone()
            {
                var meta = new ReadMetaData.Peaks(this.FileRange, this.Identifier, this.nameFilter);
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
                meta.Parts_per_million = this.Parts_per_million;
                meta.PredictedRetentionTime = new string(this.PredictedRetentionTime);
                meta.Retention_time = this.Retention_time;
                meta.ScanID = new string(this.ScanID);
                meta.Source_File = new string(this.Source_File);
                return meta;
            }

            /// <summary> Generate HTML with all meta information from the PEAKS data. </summary>
            /// <returns> Returns an HTML string with the meta information. </returns>
            public override HtmlBuilder ToHTML()
            {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from PEAKS");

                // Look for each field if it is defined, otherwise leave it out
                if (ScanID != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Scan Identifier");
                    html.OpenAndClose(HtmlTag.p, "", ScanID.ToString());
                }

                // Create a display of the sequence with local confidence and modifications (if present)
                if (Original_tag != null && Local_confidence != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", $"Original sequence (length={Original_tag.Length})");
                    html.Open(HtmlTag.div, "class='original-sequence' style='--max-value:100'");
                    int original_offset = 0;

                    for (int i = 0; i < Cleaned_sequence.Length; i++)
                    {
                        html.Open(HtmlTag.div);
                        html.Open(HtmlTag.div, "class='coverage-depth-wrapper'");
                        html.OpenAndClose(HtmlTag.span, $"class='coverage-depth-bar' style='--value:{Local_confidence[i]}'", "");
                        html.Close(HtmlTag.div);
                        html.OpenAndClose(HtmlTag.p, "", Cleaned_sequence[i].ToString());

                        if (original_offset < Original_tag.Length - 2 && Original_tag[original_offset + 1] == '(')
                        {
                            html.Open(HtmlTag.p, "class='modification'");
                            original_offset += 2;
                            while (Original_tag[original_offset] != ')')
                            {
                                html.Content(Original_tag[original_offset].ToString());
                                original_offset++;
                                if (original_offset > Original_tag.Length - 2)
                                {
                                    break;
                                }
                            }
                            html.Close(HtmlTag.p);
                        }
                        html.Close(HtmlTag.div);
                        original_offset++;
                    }
                    html.Close(HtmlTag.div);
                }

                if (Post_translational_modifications != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Posttranslational Modifications");
                    html.OpenAndClose(HtmlTag.p, "", Post_translational_modifications);
                }

                if (Source_File != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Source File");
                    html.OpenAndClose(HtmlTag.p, "", Source_File);
                }

                if (Fraction != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Fraction");
                    html.OpenAndClose(HtmlTag.p, "", Fraction);
                }

                if (Feature != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Scan Feature");
                    html.OpenAndClose(HtmlTag.p, "", Feature);
                }

                if (DeNovoScore >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "De Novo Score");
                    html.OpenAndClose(HtmlTag.p, "", DeNovoScore.ToString());
                }

                if (Confidence >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "ConfidenceScore");
                    html.OpenAndClose(HtmlTag.p, "", Confidence.ToString());
                }

                if (Mass_over_charge >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "m/z");
                    html.OpenAndClose(HtmlTag.p, "", Mass_over_charge.ToString());
                }

                if (Mass >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Mass");
                    html.OpenAndClose(HtmlTag.p, "", Mass.ToString());
                };

                if (Charge >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Charge");
                    html.OpenAndClose(HtmlTag.p, "", Charge.ToString());
                }

                if (Retention_time >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Retention Time");
                    html.OpenAndClose(HtmlTag.p, "", Retention_time.ToString());
                }

                if (PredictedRetentionTime != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Predicted Retention Time");
                    html.OpenAndClose(HtmlTag.p, "", PredictedRetentionTime.ToString());
                }

                if (Area >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Area");
                    html.OpenAndClose(HtmlTag.p, "", Area.ToString("G4"));
                }

                if (Parts_per_million >= 0)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Parts Per Million");
                    html.OpenAndClose(HtmlTag.p, "", Parts_per_million.ToString());
                }

                if (Fragmentation_mode != null)
                {
                    html.OpenAndClose(HtmlTag.h3, "", "Fragmentation mode");
                    html.OpenAndClose(HtmlTag.p, "", Fragmentation_mode);
                }

                html.Add(File.ToHTML());

                return html;
            }
        }

        public class Combined : IMetaData
        {

            /// <summary> Returns the positional score for this read, so for every position the confidence.
            /// In this case it is defined as the average positional score for all children reads. </summary> 
            public override double[] PositionalScore
            {
                get
                {
                    var scores = Children.Select(c => c.PositionalScore).Aggregate(new List<(int, double)>(), (acc, l) =>
                    {
                        if (acc.Count < l.Length)
                        {
                            acc.AddRange(Enumerable.Repeat((0, 0.0), l.Length - acc.Count));
                        }
                        for (int i = 0; i < l.Length; i++)
                        {
                            acc[i] = (acc[i].Item1 + 1, acc[i].Item2 + l[i]);
                        }
                        return acc;
                    });
                    var output = new double[scores.Count];
                    for (int i = 0; i < scores.Count; i++)
                    {
                        output[i] = scores[i].Item2 / scores[i].Item1;
                    }
                    return output;
                }
            }

            /// <summary> Returns the overall intensity for this read. It is used to determine which read to 
            /// choose if multiple reads exist at the same spot. </summary> 
            public override double Intensity { get => Children.Average(m => m.Intensity); }

            /// <summary> Contains the total area as measured by mass spectrometry to be able to report this back to the user 
            /// and help him/her get a better picture of the validity of the data. </summary> 
            public new double TotalArea { get => Children.Sum(m => m.TotalArea); }
            public override List<(string RawFile, int Scan, string OriginalTag)> ScanNumbers
            {
                get => Children.SelectMany(c => c.ScanNumbers).ToList();
            }

            public List<IMetaData> Children = new List<IMetaData>();

            /// <summary> To generate a HTML representation of this metadata for use in the HTML report. </summary> 
            /// <returns>A string containing the MetaData.</returns>
            public override HtmlBuilder ToHTML()
            {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Meta Information from Multiple reads");
                html.OpenAndClose(HtmlTag.h3, "", "Number of combined reads");
                html.OpenAndClose(HtmlTag.p, "", Children.Count().ToString());
                html.OpenAndClose(HtmlTag.h3, "", "Intensity");
                html.OpenAndClose(HtmlTag.p, "", Intensity.ToString());
                html.OpenAndClose(HtmlTag.h3, "", "TotalArea");
                html.OpenAndClose(HtmlTag.p, "", TotalArea.ToString("G4"));
                html.OpenAndClose(HtmlTag.h3, "", "PositionalScore");
                html.Add(HTMLNameSpace.HTMLGraph.Bargraph(HTMLNameSpace.HTMLGraph.AnnotateDOCData(PositionalScore.Select(a => (double)a).ToList()), new HtmlGenerator.HtmlBuilder("Positional Score"), null, null, 1));
                foreach (var child in Children)
                {
                    html.Add(child.ToHTML());
                }
                return html;
            }

            public Combined(NameFilter filter, List<IMetaData> children) : base(null, "Combined", filter)
            {
                Children = children;
            }
        }

        /// <summary> A metadata instance to contain no metadata so reads without metadata can also be handled. </summary>
        public abstract class Novor : IMetaData
        {
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
            public string Sequence;

            /// <summary> The intensity of this read </summary> 
            double intensity = 1;
            public override double Intensity
            {
                get { return 1 + Score / 100; }
                set { if (!double.IsNaN(value)) intensity = value; }
            }

            /// <summary> Create a new Novor MetaData. </summary> 
            /// <param name="file">The originating file.</param>
            /// <param name="filter">The NameFilter to use and filter the identifier_.</param>
            public Novor(FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string sequence) : base(file, "N", filter)
            {
                this.Fraction = fraction;
                this.Scan = scan;
                this.MZ = mz;
                this.Z = z;
                this.Score = score;
                this.Mass = mass;
                this.Error = error;
                this.Sequence = sequence;
            }

            /// <summary> Returns Simple MetaData to HTML. </summary> 
            public override HtmlBuilder ToHTML()
            {
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
                html.OpenAndClose(HtmlTag.p, "", Sequence);
                html.Add(File.ToHTML());
                return html;
            }
        }

        public class NovorDeNovo : Novor
        {
            /// <summary> The database sequence with possible modifications. </summary> 
            public string DBSequence;
            public NovorDeNovo(FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string sequence, string databaseSequence)
            : base(file, filter, fraction, scan, mz, z, score, mass, error, sequence)
            {
                this.DBSequence = databaseSequence;
            }

            public override HtmlBuilder ToHTML()
            {
                var html = base.ToHTML();
                html.OpenAndClose(HtmlTag.h3, "", "DBSequence");
                html.OpenAndClose(HtmlTag.p, "", DBSequence);
                return html;
            }
        }

        public class NovorPSMS : Novor
        {
            /// <summary> The read ID. </summary> 
            public string ID;
            /// <summary> The read ID. </summary> 
            public int Proteins;
            public NovorPSMS(FileRange file, NameFilter filter, string fraction, int scan, double mz, int z, double score, double mass, double error, string sequence, string id, int proteins)
            : base(file, filter, fraction, scan, mz, z, score, mass, error, sequence)
            {
                this.ID = id;
                this.Proteins = proteins;
            }

            public override HtmlBuilder ToHTML()
            {
                var html = base.ToHTML();
                html.OpenAndClose(HtmlTag.h3, "", "ID");
                html.OpenAndClose(HtmlTag.p, "", ID);
                html.OpenAndClose(HtmlTag.h3, "", "Proteins");
                html.OpenAndClose(HtmlTag.p, "", Proteins.ToString());
                return html;
            }
        }

        /// <summary> A identifier for a file, to hold information about where reads originate from. </summary>
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

            /// <summary> Creating a new FileIdentifier. </summary> 
            /// <param name="pathInput">The path to the file, can be a relative path.</param>
            /// <param name="name">The identifier given to the file.</param>
            /// <param name="origin">The place where is path is defined in a batchfile or derivatives.</param>
            public FileIdentifier(string pathInput, string name, InputNameSpace.KeyValue origin)
            {
                path = System.IO.Path.GetFullPath(pathInput);
                Name = name;
                RefersToFile = true;
                Origin = origin;
            }

            /// <summary> To create a blank instance of FileIdentifier. </summary> 
            public FileIdentifier()
            {
                path = "";
                Name = "";
                RefersToFile = false;
            }

            /// <summary> To generate HTML for use in the metadata sidebar in the HTML report. </summary> 
            /// <returns>A string containing the HTML.</returns>
            public HtmlBuilder ToHTML()
            {
                if (!RefersToFile) return new HtmlBuilder();
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlTag.h2, "", "Originating File");
                html.OpenAndClose(HtmlTag.h3, "", "Originating file identifier");
                html.OpenAndClose(HtmlTag.p, "", Name);
                html.OpenAndClose(HtmlTag.h3, "", "Originating file path");
                html.OpenAndClose(HtmlTag.a, $"href='file:///{path}' target='_blank'", Path);
                return html;
            }

            public string Display()
            {
                return $"Path: {path}\nName: {Name}";
            }

            public override bool Equals(object obj)
            {
                if (obj is FileIdentifier that)
                {
                    return this.Path == that.Path && this.Name == that.Name && this.Origin == that.Origin;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return 23 + 397 * this.Path.GetHashCode() + 17 * 397 * this.Name.GetHashCode() + 17 * 17 * 397 * this.Origin.GetHashCode();
            }
        }
    }
}