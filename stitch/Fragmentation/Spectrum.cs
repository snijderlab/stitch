using HeckLib;
using HeckLib.chemistry;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;
using HeckLib.io.fasta;
using HeckLib.masspec;
using HeckLibRawFileThermo;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;
using HtmlGenerator;
using HTMLNameSpace;
using static Stitch.InputNameSpace.ParseHelper;
using Stitch.InputNameSpace;

namespace Stitch {
    public static class Fragmentation {
        public interface IASM {
            public HtmlBuilder ToHtml(ReadFormat.General MetaData, int additional_id);
        }

        public struct FdrASM : IASM {
            public AnnotatedSpectrumMatch Match;
            /// <summary> If true all FDR measures are set. </summary>
            public bool FDRSet;
            /// <summary> The average number of matches for the same fragments but with a randomised shift
            /// divided by the number of matches with the actual fragments.</summary>
            public double FDRFractionGeneral;
            /// <summary> The average number of matches for the same fragments but with a randomised shift
            /// divided by the number of matches with the actual fragments. Only the Xle positions w and d
            /// ions are counted.</summary>
            public double FDRFractionSpecific;
            public double SpecificExpectationPerPosition;
            public int FoundSatelliteIons;
            public int PossibleSatelliteIons;
            public string MgfTitle;

            public HtmlBuilder ToHtml(ReadFormat.General MetaData, int additional_id) {
                var html = new HtmlBuilder();
                html.Add(Graph.RenderSpectrum(this.Match, new HtmlBuilder(HtmlTag.p, HTMLHelp.HecklibSpectrum), null, AminoAcid.ArrayToString(MetaData.Sequence.AminoAcids), additional_id));

                var id = this.Match.Spectrum.ScanNumber.ToString();
                var details = new HtmlBuilder();
                details.Open(HtmlTag.table);
                void Row(string name, HtmlBuilder help, string value) {
                    details.Open(HtmlTag.tr);
                    details.Open(HtmlTag.td);
                    details.TagWithHelp(HtmlTag.p, name, help);
                    details.Close(HtmlTag.td);
                    details.OpenAndClose(HtmlTag.td, "", value);
                    details.Close(HtmlTag.tr);
                }
                var matched = this.Match.FragmentMatches.Count(f => f != null);
                var total = this.Match.FragmentMatches.Count();
                Row("Matched peaks", new HtmlBuilder(HTMLHelp.SpectrumMatchedPeaks), $"{matched} ({(double)matched / total:P2} of {total})");
                Row("FDR", new HtmlBuilder(HTMLHelp.SpectrumGeneralFDR), $"{this.FDRFractionGeneral:P2}");
                Row("Satellite FDR", new HtmlBuilder(HTMLHelp.SpectrumSatelliteFDR), double.IsNaN(this.FDRFractionSpecific) ? "-" : $"{this.FDRFractionSpecific:P2}");
                Row("PSM Score", new HtmlBuilder(HTMLHelp.SpectrumScore), this.Match.Score().Score.ToString("G3"));
                details.Close(HtmlTag.table);
                if (!string.IsNullOrEmpty(MgfTitle)) details.OpenAndClose(HtmlTag.p, "", $"MGF title: {MgfTitle}");
                html.Collapsible("spectrum-details-" + id, new HtmlBuilder("Spectrum Details"), details);
                return html;
            }
        }

        public struct SingleASM : IASM {
            public AnnotatedSpectrumMatch Match;

            public SingleASM(AnnotatedSpectrumMatch match) {
                this.Match = match;
            }

            public HtmlBuilder ToHtml(ReadFormat.General MetaData, int additional_id) {
                return Graph.RenderSpectrum(this.Match, new HtmlBuilder(HtmlTag.p, HTMLHelp.PeaksSpectrum), null, null, additional_id);
            }
        }


        /// <summary> Finds the supporting spectra for all reads and saves it in the reads themselves. </summary>
        /// <param name="peptides">All peptides to find the spectra for.</param>
        public static void GetSpectra(IEnumerable<ReadFormat.General> peptides) {
            bool isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            bool osx_error_shown = false;

            var scans = peptides.SelectMany(p => p.ScanNumbers.Select(s => (p.EscapedIdentifier, s.RawFile, s.Scan, s.ProForma, s.FragmentationHint, s.XleDisambiguation, p)));
            var possibleModifications = Modification.Parse();
            possibleModifications.Add("Oxidation_W", new Modification(Modification.PositionType.anywhere, Modification.TerminusType.none, 15.99940000000));

            foreach (var group in scans.GroupBy(m => m.RawFile)) {
                ThermoRawFile raw_file = new ThermoRawFile();
                List<(PrecursorInfo Precursor, Centroid[] Spectrum, string Title)> spectra = new List<(PrecursorInfo, Centroid[], string)>();
                var raw_file_path = group.Key;
                var correct_path = InputNameSpace.ParseHelper.TestFileExists(new ReadFormat.FileIdentifier(raw_file_path, "RawFile", new()), false);

                if (!correct_path.IsErr()) {
                    if (group.Key.EndsWith(".raw")) {
                        if (isOSX) {
                            if (!osx_error_shown) {
                                new InputNameSpace.ErrorMessage(raw_file_path, "Cannot open raw files on OSX", "The ThermoFisher library used does not support OSX.", "MGF files do work on OSX.", true).Print();
                                osx_error_shown = true;
                            }
                            continue;
                        } else {
                            try {
                                raw_file.Open(raw_file_path);
                                raw_file.SetCurrentController(ThermoRawFile.CONTROLLER_MS, 1);
                            } catch (Exception exception) {
                                throw new RunTimeException(
                                    new InputNameSpace.ErrorMessage(raw_file_path, "Could not open raw data file", "The shown raw file could not be opened. See the error below for more information."),
                                    exception);
                            }
                        }
                    } else if (group.Key.EndsWith(".mgf")) {
                        try {
                            HeckLibRawFileMgf.MgfRawFile.Parse(raw_file_path, delegate (Centroid[] spectrum, PrecursorInfo precursor, string title, Spectrum.MassAnalyzer massanalyzer) {
                                spectra.Add((precursor, spectrum, title));
                            });
                        } catch (Exception exception) {
                            throw new RunTimeException(
                                new InputNameSpace.ErrorMessage(raw_file_path, "Could not open mgf data file", "The shown raw file could not be opened. See the error below for more information."),
                                exception);
                        }
                    }
                } else {
                    throw new RunTimeException(
                        correct_path.Messages.First().AddHelp("Make sure you are trying to open the correct raw data for this dataset."));
                }

                foreach (var scan in group) {
                    Centroid[] spectrum = new Centroid[0];
                    PrecursorInfo precursor = null;
                    string MgfTitle = null;
                    if (group.Key.EndsWith(".raw")) {
                        // Get the information to find this peptide
                        double[] mzs;
                        float[] intensities;

                        // check whether we have an ms2 scan to identify
                        var filter = raw_file.GetFilterForScanNum(scan.Scan);

                        // retrieve the precursor; this doesn't always work because thermo doesn't store the m/z or charge info in some cases
                        var tr_precursor = raw_file.GetPrecursorScanInfoForScan(scan.Scan);
                        precursor = new PrecursorInfo {
                            Mz = filter.ParentMass,
                            Fragmentation = filter.Fragmentation,
                            FragmentationEnergy = filter.FragmentationEnergy,
                            RawFile = raw_file.GetFilename(),
                            ScanNumber = scan.Scan,
                            Charge = (short)tr_precursor.ChargeState,
                        };
                        if (tr_precursor.MonoIsotopicMass < 100 || tr_precursor.ChargeState < 2)
                            continue;

                        raw_file.GetMassListFromScanNum(scan.Scan, false, out mzs, out intensities);

                        // Centroid the data
                        if (filter.Format == Spectrum.ScanModeType.Centroid)
                            spectrum = CentroidDetection.ConvertCentroids(mzs, intensities, Orbitrap.ToleranceForResolution(17500));
                        else {
                            // Retrieve the noise function for signal-to-noise calculations
                            spectrum = CentroidDetection.Process(mzs, intensities, raw_file.GetNoiseDistribution(scan.Scan), new CentroidDetection.Settings());
                        }
                    } else if (group.Key.EndsWith(".mgf")) {
                        // retrieve the precursor; this doesn't always work because thermo doesn't store the m/z or charge info in some cases
                        if (scan.Scan >= spectra.Count) {
                            new ErrorMessage(group.Key, "MGF scan out of bounds", $"The scan number {scan.Scan} does not exist in this file.", "", true).Print();
                            continue;
                        }

                        precursor = spectra[scan.Scan].Precursor;
                        precursor.ScanNumber = scan.Scan;
                        spectrum = spectra[scan.Scan].Spectrum;
                        MgfTitle = spectra[scan.Scan].Title;

                        // determine the type of fragmentation used
                        var fragmentation_method = scan.FragmentationHint.Map<Spectrum.FragmentationType>(f => f, () => Spectrum.FragmentationType.All);
                        precursor.Fragmentation = fragmentation_method;
                    }

                    var model = PeptideFragment.GetFragmentModel(precursor.Fragmentation);
                    if (scan.XleDisambiguation && (precursor.Fragmentation == Spectrum.FragmentationType.EThcD || precursor.Fragmentation == Spectrum.FragmentationType.ETciD || precursor.Fragmentation == Spectrum.FragmentationType.ETD || precursor.Fragmentation == Spectrum.FragmentationType.ECD)) {
                        model.W = new PeptideFragment.FragmentRange {
                            MinPos = 1,
                            MaxPos = PeptideFragment.SEQUENCELENGTH - 1,
                            HigherCharges = true,
                            MassShifts = PeptideFragment.MASSSHIFT_WATERLOSS | PeptideFragment.MASSSHIFT_AMMONIALOSS
                        };
                    }
                    if (scan.XleDisambiguation && (precursor.Fragmentation == Spectrum.FragmentationType.CID || precursor.Fragmentation == Spectrum.FragmentationType.HCD || precursor.Fragmentation == Spectrum.FragmentationType.PQD)) {
                        model.D = new PeptideFragment.FragmentRange {
                            MinPos = 1,
                            MaxPos = 2,
                            HigherCharges = true,
                            MassShifts = PeptideFragment.MASSSHIFT_NEUTRALLOSS | PeptideFragment.MASSSHIFT_WATERLOSS | PeptideFragment.MASSSHIFT_AMMONIALOSS
                        };
                    }
                    // Default set to 20, same as default in Peaks
                    model.tolerance.Value = 20;

                    string sequence = ""; Modification n_term; Modification c_term; Modification[] modifications;
                    try {
                        sequence = FastaParser.ParseProForma(scan.ProForma, possibleModifications, out n_term, out c_term, out modifications);
                    } catch {
                        new ErrorMessage(scan.ProForma, "Could not parse pro forma sequence", $"The program will continue but the spectra will be missing for this peptide ({scan.p.Identifier}).", "", true).Print();
                        continue;
                    }
                    sequence = new String(sequence.Select(c => c == 'B' ? 'N' : c).ToArray()); // B represents N|D so just replace it with N for the use in Hecklib

                    // Do Xle disambiguation, only if this is actually used and useful
                    if (scan.XleDisambiguation && sequence.Any(c => c == 'L' || c == 'I' || c == 'J')) {
                        // Generate sequences with only L and only I to scan for support
                        string pureL = new String(sequence.Select(c => c == 'I' || c == 'J' ? 'L' : c).ToArray());
                        string pureI = new String(sequence.Select(c => c == 'L' || c == 'J' ? 'I' : c).ToArray());

                        var asmL = GetASM(pureL, n_term, c_term, modifications, precursor, new PeptideFragment.FragmentModel(model), spectrum);
                        var asmI = GetASM(pureI, n_term, c_term, modifications, precursor, new PeptideFragment.FragmentModel(model), spectrum);

                        // For each Xle see which version is more supported
                        var builder = new StringBuilder();
                        for (int i = 0; i < pureI.Length; i++) {
                            if (pureI[i] == 'I') {
                                var supportL = asmL.FragmentMatches.Count(f => f != null && (f.FragmentType == PeptideFragment.ION_W || f.FragmentType == PeptideFragment.ION_D) && f.Position == i);
                                var supportI = asmI.FragmentMatches.Count(f => f != null && (f.FragmentType == PeptideFragment.ION_W || f.FragmentType == PeptideFragment.ION_D) && f.Position == i);
                                if (supportL == 0 && supportI == 0) {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'J')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('J') }, "No support for either Leucine or Isoleucine based on side chain ions");
                                } else if (supportI > supportL) {
                                    builder.Append('I');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'I')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('I') }, $"Support for Isoleucine based on side chain ions ({supportI} for I {supportL} for L)");
                                } else if (supportL > supportI) {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'L')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('L') }, $"Support for Leucine based on side chain ions ({supportL} for L {supportI} for I)");
                                } else {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'J')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('J') }, $"Equal support for both Leucine and Isoleucine based on side chain ions ({supportI} ions for both)");
                                }
                            } else {
                                builder.Append(sequence[i]);
                            }
                        }
                        // Assemble the final sequence with the disambiguated positions
                        sequence = builder.ToString();
                    }
                    string noJ = new String(sequence.Select(c => c == 'J' ? 'L' : c).ToArray());
                    var asm = GetASMWithFDR(noJ, n_term, c_term, modifications, precursor, new PeptideFragment.FragmentModel(model), spectrum, MgfTitle);

                    scan.p.SupportingSpectra.Add(asm);
                }
            }
        }

        static AnnotatedSpectrumMatch GetASM(string sequence, Modification n_term, Modification c_term, Modification[] modifications, PrecursorInfo hl_precursor, PeptideFragment.FragmentModel model, Centroid[] spectrum) {
            Peptide peptide = new Peptide(sequence, n_term, c_term, modifications);

            // If not deisotoped, should be charge of the precursor
            PeptideFragment[] peptide_fragments = PeptideFragment.Generate(peptide, hl_precursor.Charge, model);
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, hl_precursor.Charge, spectrum, peptide_fragments, model.tolerance, model.IsotopeError);

            return new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: (int)model.tolerance.Value), peptide, matchedFragments);
        }

        static FdrASM GetASMWithFDR(string sequence, Modification n_term, Modification c_term, Modification[] modifications, PrecursorInfo hl_precursor, PeptideFragment.FragmentModel model, Centroid[] spectrum, string MgfTitle) {
            Peptide peptide = new Peptide(sequence, n_term, c_term, modifications);

            // If not deisotoped, should be charge of the precursor
            PeptideFragment[] peptide_fragments = PeptideFragment.Generate(peptide, hl_precursor.Charge, model);
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, hl_precursor.Charge, spectrum, peptide_fragments, model.tolerance, model.IsotopeError);

            var res = new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: (int)model.tolerance.Value), peptide, matchedFragments);

            var actual_generic = matchedFragments.Count(i => i != null);
            var actual_specific = matchedFragments.Count(i => i != null && (i.Letter == 'I' || i.Letter == 'L') && (i.FragmentType == PeptideFragment.ION_W || i.FragmentType == PeptideFragment.ION_D));
            var total_general = 0;
            var total_specific = 0;
            var steps = 0;

            for (var shift = 5; shift <= 25; shift += 1) {
                if (shift == 0) continue;
                var step = GetCounts(peptide, peptide_fragments, shift + 0.10411, hl_precursor.Charge, model, spectrum);
                var neg_step = GetCounts(peptide, peptide_fragments, -shift - 0.10411, hl_precursor.Charge, model, spectrum);
                total_general += step.DetectedGeneral + neg_step.DetectedGeneral;
                total_specific += step.DetectedSpecific + neg_step.DetectedSpecific;
                steps += 2;
            }

            return new FdrASM() {
                Match = res,
                FDRSet = true,
                FDRFractionGeneral = (double)total_general / steps / actual_generic,
                FDRFractionSpecific = (double)total_specific / steps / actual_specific,
                SpecificExpectationPerPosition = (double)total_specific / steps / sequence.Count(i => i == 'I' || i == 'L'),
                FoundSatelliteIons = actual_specific,
                PossibleSatelliteIons = peptide_fragments.Count(i => (i.Letter == 'I' || i.Letter == 'L') && (i.FragmentType == PeptideFragment.ION_W || i.FragmentType == PeptideFragment.ION_D)),
                MgfTitle = MgfTitle,
            };
        }

        static (int DetectedGeneral, int DetectedSpecific) GetCounts(Peptide peptide, PeptideFragment[] fragments, double shift, short maxCharge, PeptideFragment.FragmentModel model, Centroid[] spectrum) {
            var shifted_fragments = fragments.Select(f => { var p = new PeptideFragment(f); p.Mz += shift; return p; }).ToArray();
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, maxCharge, spectrum, shifted_fragments, model.tolerance, model.IsotopeError);
            return (matchedFragments.Count(i => i != null), matchedFragments.Count(i => i != null && (i.Letter == 'I' || i.Letter == 'L') && (i.FragmentType == PeptideFragment.ION_W || i.FragmentType == PeptideFragment.ION_D)));
        }

        public static ParseResult<List<(int ScanNumber, IASM ASM)>> LoadPeaksSpectra(ParsedFile file, Dictionary<string, Modification> possibleModifications = null) {
            var counter = new InputNameSpace.Tokenizer.Counter(file);
            var output = new List<(int ScanNumber, IASM ASM)>();
            var outEither = new ParseResult<List<(int ScanNumber, IASM ASM)>>();
            var temp = new List<(string Ion, int Pos, double Mz, double TheoreticalMz, double MzError, double Intensity, short Charge)>();
            var temp_meta = (ScanNumber: -1, FullScanNumber: "", Sequence: "", Mz: 0.0, RawFile: "");
            for (int line_number = 1; line_number < file.Lines.Length; line_number++) {
                var pieces = SplitLine(',', line_number, file);
                if (pieces.Count == 1) continue;
                if (pieces.Count < 17) {
                    outEither.AddMessage(new InputNameSpace.ErrorMessage(new Position(line_number, 1, file), "Missing columns", $"Needs at least 16 columns but only found {pieces.Count}"));
                    continue;
                }
                //Peptide,Length,mass,m/z,RT,Predicted RT,z,Fraction,scan,Source File,ion,pos,ion m/z,theo m/z,m/z error,ion intensity,ion charge,modification
                // 0      1      2    3   4  5            6 7        8    9           10  11  12      13       14        15            16         17
                var num = pieces[8].Text;
                if (num != temp_meta.FullScanNumber && !String.IsNullOrEmpty(temp_meta.FullScanNumber)) {
                    output.Add((temp_meta.ScanNumber, BuildASMFromTemp(temp, temp_meta, possibleModifications ?? Modification.Parse())));
                    temp.Clear();
                    temp_meta = (-1, "", "", 0.0, "");
                }
                if (String.IsNullOrEmpty(temp_meta.FullScanNumber)) {
                    var trimmed = ConvertToInt(pieces[8].Text.Split(':').Last(), pieces[8].Pos).UnwrapOrDefault(outEither, 0);
                    temp_meta = (trimmed, num, HelperFunctionality.FromSloppyProForma(pieces[0].Text).Unwrap().Modified, ConvertToDouble(pieces[3]).UnwrapOrDefault(outEither, 0.0), pieces[9].Text);
                }
                temp.Add((
                    pieces[10].Text,
                    ConvertToInt(pieces[11]).UnwrapOrDefault(outEither, 0),
                    ConvertToDouble(pieces[12]).UnwrapOrDefault(outEither, 0.0),
                    ConvertToDouble(pieces[13]).UnwrapOrDefault(outEither, 0.0),
                    ConvertToDouble(pieces[14]).UnwrapOrDefault(outEither, 0.0),
                    ConvertToDouble(pieces[15]).UnwrapOrDefault(outEither, 0.0),
                    (short)ConvertToInt(pieces[16]).UnwrapOrDefault(outEither, 0)
                     ));
            }
            outEither.Value = output;
            return outEither;
        }

        static IASM BuildASMFromTemp(List<(string Ion, int SeriesNumber, double Mz, double TheoreticalMz, double MzError, double Intensity, short Charge)> data, (int ScanNumber, string FullScanNumber, string Sequence, double Mz, string RawFile) meta, Dictionary<string, Modification> possibleModifications) {
            var centroids = data.Select(p => new Centroid() { Charge = p.Charge, Mz = p.Mz, Intensity = (float)p.Intensity, MinMz = (float)(p.Mz - p.MzError), MaxMz = (float)(p.Mz + p.MzError), Resolution = 0, SignalToNoise = 0 }).ToArray();
            var precursor = new PrecursorInfo() { Mz = meta.Mz, ScanNumber = meta.ScanNumber, RawFile = meta.RawFile };
            string sequence = FastaParser.ParseProForma(meta.Sequence, possibleModifications, out Modification n_term, out Modification c_term, out Modification[] modifications);
            var peptide = new Peptide(sequence, n_term, c_term, modifications);
            var matches = data.Select(p => {
                var fragment = PeptideFragment.IonFromString(p.Ion.Substring(0, 1));
                var term = Proteomics.Terminus.N;
                if (fragment == PeptideFragment.ION_W || fragment == PeptideFragment.ION_X || fragment == PeptideFragment.ION_Y || fragment == PeptideFragment.ION_Z) term = Proteomics.Terminus.C;
                var position = term == Proteomics.Terminus.N ? p.SeriesNumber - 1 : sequence.Length - p.SeriesNumber;
                return new PeptideFragment(0, sequence[position], position, p.SeriesNumber, p.TheoreticalMz, p.Charge, fragment, 0);
            }).ToArray();
            return new SingleASM(new AnnotatedSpectrumMatch(
                new SpectrumContainer(
                    centroids, precursor), peptide, matches));
        }
    }
}