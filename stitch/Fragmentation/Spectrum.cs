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
        /// <summary> Create a dictionary of all spectra with the escaped identifier of the top level metadata construct as key. </summary>
        /// <param name="peptides">All peptides to find the spectra for.</param>
        /// <param name="directory">The directory in which to search for the raw data files.</param>
        /// <returns></returns>
        public static Dictionary<string, List<AnnotatedSpectrumMatch>> GetSpectra(IEnumerable<ReadFormat.General> peptides, bool xle_disambiguation) {
            var fragments = new Dictionary<string, List<AnnotatedSpectrumMatch>>(peptides.Count());
            var scans = peptides.SelectMany(p => p.ScanNumbers.Select(s => (p.EscapedIdentifier, s.RawFile, s.Scan, s.OriginalTag, p)));

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
                    var model = new PeptideFragment.FragmentModel(PeptideFragment.GetFragmentModel(filter.Fragmentation)) {
                        W = new PeptideFragment.FragmentRange {
                            MinPos = 1,
                            MaxPos = PeptideFragment.SEQUENCELENGTH - 1,
                            HigherCharges = true,
                            MassShifts = PeptideFragment.MASSSHIFT_WATERLOSS | PeptideFragment.MASSSHIFT_AMMONIALOSS
                        }
                    };
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

                    // Deisotope: not strictly necessary
                    //IsotopePattern[] isotopes = IsotopePatternDetection.Process(spectrum, new IsotopePatternDetection.Settings { MaxCharge = (short)precursor.ChargeState });
                    //var spectrumDeiso = SpectrumUtils.Deisotope(spectrum, isotopes, (short)precursor.ChargeState, true);
                    //var spectrumTopX = SpectrumUtils.TopX(spectrum, 40, 100, out int[] ranks);
                    //var spectrumDeisoTopX = SpectrumUtils.TopX(spectrum, 40, 100, out int[] ranks2);

                    string transformedPeaksPeptide = scan.OriginalTag.Replace("(", "[").Replace(")", "]");
                    string sequence = FastaParser.ParseProForma(transformedPeaksPeptide, Modification.Parse(), out Modification n_term, out Modification c_term, out Modification[] modifications);
                    if (xle_disambiguation) {
                        string pureL = new String(sequence.Select(c => c == 'I' ? 'L' : c).ToArray());
                        string pureI = new String(sequence.Select(c => c == 'L' ? 'I' : c).ToArray());

                        var asmL = GetASM(pureL, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, model, spectrum, filter);
                        var asmI = GetASM(pureI, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, model, spectrum, filter);

                        var builder = new StringBuilder();
                        for (int i = 0; i < pureI.Length; i++) {
                            if (pureI[i] == 'I') {
                                var supportL = asmL.FragmentMatches.FirstOrDefault(f => f != null && f.FragmentType == PeptideFragment.ION_W && f.Position == pureI.Length - i); // w ions always have C terminal position
                                var supportI = asmI.FragmentMatches.FirstOrDefault(f => f != null && f.FragmentType == PeptideFragment.ION_W && f.Position == pureI.Length - i);
                                var d = default(PeptideFragment);
                                if (supportL == d && supportI == d) {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'J')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('J') }, "No w ion support for either L or I");
                                } else if (supportL == d) {
                                    builder.Append('I');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'I')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('I') }, $"Support for I based on w ions {supportI}");
                                } else if (supportI == d) {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'L')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('L') }, $"Support for L based on w ions {supportL}");
                                } else {
                                    builder.Append('L');
                                    if (scan.p.Sequence.AminoAcids[i].Character != 'J')
                                        scan.p.Sequence.UpdateSequence(i, 1, new AminoAcid[] { new AminoAcid('J') }, "w Ion support for both L and I");
                                }
                            } else {
                                builder.Append(sequence[i]);
                            }
                        }
                        sequence = builder.ToString();
                    }
                    var asm = GetASM(sequence, n_term, c_term, modifications, raw_file.GetFilename(), scan.Scan, precursor, model, spectrum, filter);

                    if (fragments.ContainsKey(scan.EscapedIdentifier)) {
                        fragments[scan.EscapedIdentifier].Add(asm);
                    } else {
                        fragments[scan.EscapedIdentifier] = new List<AnnotatedSpectrumMatch> { asm };
                    }
                }
            }
            return fragments;
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
    }
}