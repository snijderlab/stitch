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

namespace AssemblyNameSpace
{
    public static class Fragmentation
    {
        public static Dictionary<string, AnnotatedSpectrumMatch[]> GetSpectra(IEnumerable<ReadMetaData.Peaks> peptides, string directory)
        {
            var fragments = new Dictionary<string, AnnotatedSpectrumMatch[]>(peptides.Count());

            foreach (var group in peptides.GroupBy(m => m.Source_File))
            {
                ThermoRawFile raw_file = new ThermoRawFile();
                raw_file.Open(directory + Path.DirectorySeparatorChar + group.Key);
                raw_file.SetCurrentController(ThermoRawFile.CONTROLLER_MS, 1);

                foreach (var meta in group)
                {
                    var spectra = new List<AnnotatedSpectrumMatch>(meta.Other_scans.Count + 1);
                    string transformedPeaksPeptide = meta.Original_tag.Replace("(", "[").Replace(")", "]");

                    foreach (var scan in meta.Other_scans.Concat(Enumerable.Repeat(meta.ScanID, 1)))
                    {
                        // Get the information to find this peptide
                        int scan_number = int.Parse(scan.Split(":").Last());

                        double[] mzs;
                        float[] intensities;

                        // check whether we have an ms2 scan to identify
                        var filter = raw_file.GetFilterForScanNum(scan_number);

                        // retrieve the precursor; this doesn't always work because thermo doesn't store the m/z or charge info in some cases
                        var precursor = raw_file.GetPrecursorScanInfoForScan(scan_number);
                        if (precursor.MonoIsotopicMass < 100 || precursor.ChargeState < 2)
                            continue;

                        var tic = raw_file.GetScanHeaderInfoForScanNum(scan_number).Tic;

                        // determine the type of fragmentation used
                        var model = PeptideFragment.GetFragmentModel(filter.Fragmentation);
                        raw_file.GetMassListFromScanNum(scan_number, false, out mzs, out intensities);

                        // Default set to 20, should be lower for BU
                        model.tolerance.Value = 5;

                        // Centroid the data
                        Centroid[] spectrum;
                        if (filter.Format == Spectrum.ScanModeType.Centroid)
                            spectrum = CentroidDetection.ConvertCentroids(mzs, intensities, Orbitrap.ToleranceForResolution(17500));
                        else
                        {
                            // Retrieve the noise function for signal-to-noise calculations
                            spectrum = CentroidDetection.Process(mzs, intensities, raw_file.GetNoiseDistribution(scan_number), new CentroidDetection.Settings());
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

                        var hl_precursor = new PrecursorInfo
                        {
                            Mz = filter.ParentMass,
                            Fragmentation = filter.Fragmentation,
                            FragmentationEnergy = filter.FragmentationEnergy,
                            RawFile = raw_file.GetFilename(),
                            ScanNumber = scan_number,
                        };
                        var asm = new AnnotatedSpectrumMatch(new SpectrumContainer(spectrum, hl_precursor, toleranceInPpm: 20), peptide, matchedFragments);

                        spectra.Add(asm);
                    }
                    fragments.Add(meta.EscapedIdentifier, spectra.ToArray());
                }
            }
            return fragments;
        }
    }
}