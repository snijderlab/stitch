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

namespace Stitch {
    public static class Fragmentation {
        public struct ASM {
            public AnnotatedSpectrumMatch Match;
            /// <summary> The average number of matches for the same fragments but with a randomised shift
            /// divided by the number of matches with the actual fragments.</summary>
            public double FDRFractionGeneral;
            /// <summary> The average number of matches for the same fragments but with a randomised shift
            /// divided by the number of matches with the actual fragments. Only the Xle positions w and d
            /// ions are counted.</summary>
            public double FDRFractionSpecific;
            public double SpecificExpectationPerPosition;

            public ASM(AnnotatedSpectrumMatch match, double general, double specific, double expectation) {
                Match = match;
                FDRFractionGeneral = general;
                FDRFractionSpecific = specific;
                SpecificExpectationPerPosition = expectation;
            }
        }

        /// <summary> Finds the supporting spectra for all reads and saves it in the reads themselves. </summary>
        /// <param name="peptides">All peptides to find the spectra for.</param>
        /// <param name="directory">The directory in which to search for the raw data files.</param>
        public static void GetSpectra(IEnumerable<ReadFormat.General> peptides, bool xle_disambiguation) {
            var scans = peptides.SelectMany(p => p.ScanNumbers.Select(s => (p.EscapedIdentifier, s.RawFile, s.Scan, s.OriginalTag, s.XleDisambiguation, p)));

            foreach (var group in scans.GroupBy(m => m.RawFile)) {
                ThermoRawFile raw_file = new ThermoRawFile();
                var raw_file_path = group.Key;
                var correct_path = InputNameSpace.ParseHelper.TestFileExists(raw_file_path);
                if (!correct_path.IsErr()) {
                    try {
                        raw_file.Open(raw_file_path);
                        raw_file.SetCurrentController(ThermoRawFile.CONTROLLER_MS, 1);
                    } catch (Exception exception) {
                        throw new RunTimeException(
                            new InputNameSpace.ErrorMessage(raw_file_path, "Could not open raw data file", "The shown raw file could not be opened. See the error below for more information."),
                            exception);
                    }
                } else {
                    throw new RunTimeException(
                        correct_path.Messages.First().AddHelp("Make sure you are trying to open the correct raw data for this dataset."));
                }

                foreach (var scan in group) {
                    // Get the information to find this peptide
                    double[] mzs;
                    float[] intensities;

                    // check whether we have an ms2 scan to identify
                    var filter = raw_file.GetFilterForScanNum(scan.Scan);

                    // retrieve the precursor; this doesn't always work because thermo doesn't store the m/z or charge info in some cases
                    var precursor = raw_file.GetPrecursorScanInfoForScan(scan.Scan);
                    if (precursor.MonoIsotopicMass < 100 || precursor.ChargeState < 2)
                        continue;

                    // determine the type of fragmentation used
                    var model = new PeptideFragment.FragmentModel(PeptideFragment.GetFragmentModel(filter.Fragmentation));
                    if (scan.XleDisambiguation && (filter.Fragmentation == Spectrum.FragmentationType.EThcD || filter.Fragmentation == Spectrum.FragmentationType.ETciD || filter.Fragmentation == Spectrum.FragmentationType.ETD || filter.Fragmentation == Spectrum.FragmentationType.ECD)) {
                        model.W = new PeptideFragment.FragmentRange {
                            MinPos = 1,
                            MaxPos = PeptideFragment.SEQUENCELENGTH - 1,
                            HigherCharges = true,
                            MassShifts = PeptideFragment.MASSSHIFT_WATERLOSS | PeptideFragment.MASSSHIFT_AMMONIALOSS
                        };
                    }
                    if (scan.XleDisambiguation && (filter.Fragmentation == Spectrum.FragmentationType.CID || filter.Fragmentation == Spectrum.FragmentationType.HCD || filter.Fragmentation == Spectrum.FragmentationType.PQD)) {
                        model.D = new PeptideFragment.FragmentRange {
                            MinPos = 1,
                            MaxPos = 2,
                            HigherCharges = true,
                            MassShifts = PeptideFragment.MASSSHIFT_NEUTRALLOSS | PeptideFragment.MASSSHIFT_WATERLOSS | PeptideFragment.MASSSHIFT_AMMONIALOSS
                        };
                    }

                    raw_file.GetMassListFromScanNum(scan.Scan, false, out mzs, out intensities);

                    // Default set to 20, should be lower for BU
                    model.tolerance.Value = 5;

                    // Centroid the data
                    Centroid[] spectrum;
                    if (filter.Format == Spectrum.ScanModeType.Centroid)
                        spectrum = CentroidDetection.ConvertCentroids(mzs, intensities, Orbitrap.ToleranceForResolution(17500));
                    else {
                        // Retrieve the noise function for signal-to-noise calculations
                        spectrum = CentroidDetection.Process(mzs, intensities, raw_file.GetNoiseDistribution(scan.Scan), new CentroidDetection.Settings());
                    }

                    string transformedPeaksPeptide = scan.OriginalTag.Replace("(", "[").Replace(")", "]");
                    string sequence = FastaParser.ParseProForma(transformedPeaksPeptide, Modification.Parse(), out Modification n_term, out Modification c_term, out Modification[] modifications);

                    // Do Xle disambiguation, only if this is actually used and useful
                    if (scan.XleDisambiguation && sequence.Any(c => c == 'L' || c == 'I' || c == 'J')) {
                        // Generate sequences with only L and only I to scan for support
                        string pureL = new String(sequence.Select(c => c == 'I' || c == 'J' ? 'L' : c).ToArray());
                        string pureI = new String(sequence.Select(c => c == 'L' || c == 'J' ? 'I' : c).ToArray());

                        var asmL = GetASM(pureL, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, new PeptideFragment.FragmentModel(model), spectrum, filter);
                        var asmI = GetASM(pureI, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, new PeptideFragment.FragmentModel(model), spectrum, filter);

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
                    var asm = GetASMWithFDR(sequence, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, new PeptideFragment.FragmentModel(model), spectrum, filter);

                    scan.p.SupportingSpectra.Add(asm);
                }
            }
        }

        static AnnotatedSpectrumMatch GetASM(string sequence, Modification n_term, Modification c_term, Modification[] modifications, string RawFile, int ScanNumber, ThermoRawFile.PrecursorInfo precursor, PeptideFragment.FragmentModel model, Centroid[] spectrum, ThermoRawFile.FilterLine filter) {
            Peptide peptide = new Peptide(sequence, n_term, c_term, modifications);

            // If not deisotoped, should be charge of the precursor
            short maxCharge = 1;
            maxCharge = (short)precursor.ChargeState;
            PeptideFragment[] peptide_fragments = PeptideFragment.Generate(peptide, maxCharge, model);
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, maxCharge, spectrum, peptide_fragments, model.tolerance, model.IsotopeError);

            var hl_precursor = new PrecursorInfo {
                Mz = filter.ParentMass,
                Fragmentation = filter.Fragmentation,
                FragmentationEnergy = filter.FragmentationEnergy,
                RawFile = RawFile,
                ScanNumber = ScanNumber,
            };
            return new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: (int)model.tolerance.Value), peptide, matchedFragments);
        }

        static ASM GetASMWithFDR(string sequence, Modification n_term, Modification c_term, Modification[] modifications, string RawFile, int ScanNumber, ThermoRawFile.PrecursorInfo precursor, PeptideFragment.FragmentModel model, Centroid[] spectrum, ThermoRawFile.FilterLine filter) {
            Peptide peptide = new Peptide(sequence, n_term, c_term, modifications);

            // If not deisotoped, should be charge of the precursor
            short maxCharge = 1;
            maxCharge = (short)precursor.ChargeState;
            PeptideFragment[] peptide_fragments = PeptideFragment.Generate(peptide, maxCharge, model);
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, maxCharge, spectrum, peptide_fragments, model.tolerance, model.IsotopeError);

            var hl_precursor = new PrecursorInfo {
                Mz = filter.ParentMass,
                Fragmentation = filter.Fragmentation,
                FragmentationEnergy = filter.FragmentationEnergy,
                RawFile = RawFile,
                ScanNumber = ScanNumber,
            };
            var res = new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: (int)model.tolerance.Value), peptide, matchedFragments);

            var actual_generic = matchedFragments.Count(i => i != null);
            var actual_specific = matchedFragments.Count(i => i != null && (i.Letter == 'I' || i.Letter == 'L') && (i.FragmentType == PeptideFragment.ION_W || i.FragmentType == PeptideFragment.ION_D));
            var total_general = 0;
            var total_specific = 0;
            var steps = 0;

            for (var shift = 5; shift <= 25; shift += 1) {
                if (shift == 0) continue;
                var step = GetCounts(peptide, peptide_fragments, shift + 0.10411, maxCharge, model, spectrum);
                var neg_step = GetCounts(peptide, peptide_fragments, -shift - 0.10411, maxCharge, model, spectrum);
                total_general += step.DetectedGeneral + neg_step.DetectedGeneral;
                total_specific += step.DetectedSpecific + neg_step.DetectedSpecific;
                steps += 2;
            }

            return new ASM(res, (double)total_general / steps / actual_generic, (double)total_specific / steps / actual_specific, (double)total_specific / steps / sequence.Count(i => i == 'I' || i == 'L'));
        }

        static (int DetectedGeneral, int DetectedSpecific) GetCounts(Peptide peptide, PeptideFragment[] fragments, double shift, short maxCharge, PeptideFragment.FragmentModel model, Centroid[] spectrum) {
            var shifted_fragments = fragments.Select(f => { var p = new PeptideFragment(f); p.Mz += shift; return p; }).ToArray();
            var matchedFragments = SpectrumUtils.MatchFragments(peptide, maxCharge, spectrum, shifted_fragments, model.tolerance, model.IsotopeError);
            return (matchedFragments.Count(i => i != null), matchedFragments.Count(i => i != null && (i.Letter == 'I' || i.Letter == 'L') && (i.FragmentType == PeptideFragment.ION_W || i.FragmentType == PeptideFragment.ION_D)));
        }
    }
}