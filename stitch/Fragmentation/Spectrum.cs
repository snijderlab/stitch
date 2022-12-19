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
        public static Dictionary<string, List<AnnotatedSpectrumMatch>> GetSpectra(IEnumerable<Read.IRead> peptides) {
            var fragments = new Dictionary<string, List<AnnotatedSpectrumMatch>>(peptides.Count());
            var scans = peptides.SelectMany(p => p.ScanNumbers.Select(s => (p.EscapedIdentifier, s.RawFile, s.Scan, s.OriginalTag)));

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
                    string transformedPeaksPeptide = scan.OriginalTag.Replace("(", "[").Replace(")", "]");
                    // Get the information to find this peptide

                    double[] mzs;
                    float[] intensities;

                    // check whether we have an ms2 scan to identify
                    var filter = raw_file.GetFilterForScanNum(scan.Scan);

                    // retrieve the precursor; this doesn't always work because thermo doesn't store the m/z or charge info in some cases
                    var precursor = raw_file.GetPrecursorScanInfoForScan(scan.Scan);
                    if (precursor.MonoIsotopicMass < 100 || precursor.ChargeState < 2)
                        continue;

                    var tic = raw_file.GetScanHeaderInfoForScanNum(scan.Scan).Tic;

                    // determine the type of fragmentation used
                    var model = PeptideFragment.GetFragmentModel(filter.Fragmentation);
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
                    IsotopePattern[] isotopes = IsotopePatternDetection.Process(spectrum, new IsotopePatternDetection.Settings { MaxCharge = (short)precursor.ChargeState });
                    var spectrumDeiso = SpectrumUtils.Deisotope(spectrum, isotopes, (short)precursor.ChargeState, true);

                    var spectrumTopX = SpectrumUtils.TopX(spectrum, 40, 100, out int[] ranks);
                    var spectrumDeisoTopX = SpectrumUtils.TopX(spectrum, 40, 100, out int[] ranks2);

                    string sequence = FastaParser.ParseProForma(transformedPeaksPeptide, Modification.Parse(), out Modification n_term, out Modification c_term, out Modification[] modifications);

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
                        RawFile = raw_file.GetFilename(),
                        ScanNumber = scan.Scan,
                    };
                    var asm = new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: 20), peptide, matchedFragments);

                    if (fragments.ContainsKey(scan.EscapedIdentifier)) {
                        fragments[scan.EscapedIdentifier].Add(asm);
                    } else {
                        fragments[scan.EscapedIdentifier] = new List<AnnotatedSpectrumMatch> { asm };
                    }
                }
            }
            return fragments;
        }
    }
}