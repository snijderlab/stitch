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
        public static Dictionary<string, PeptideSpectrum[]> GetSpectra(IEnumerable<ReadMetaData.Peaks> peptides, string directory)
        {
            var fragments = new Dictionary<string, PeptideSpectrum[]>(peptides.Count());

            foreach (var group in peptides.GroupBy(m => m.Source_File))
            {
                ThermoRawFile raw_file = new ThermoRawFile();
                raw_file.Open(directory + Path.DirectorySeparatorChar + group.Key);
                raw_file.SetCurrentController(ThermoRawFile.CONTROLLER_MS, 1);

                foreach (var meta in group)
                {
                    var spectra = new List<PeptideSpectrum>(meta.Other_scans.Count + 1);
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
                        spectra.Add(new PeptideSpectrum(scan, matchedFragments, spectrum, peptide_fragments));
                    }
                    fragments.Add(meta.EscapedIdentifier, spectra.ToArray());
                }
            }
            return fragments;
        }

        public static List<string> GetFragmentTypes(this PeptideFragment peptide)
        {
            var output = new List<string>();
            if ((peptide.FragmentType & PeptideFragment.ION_A) != 0) output.Add("a");
            if ((peptide.FragmentType & PeptideFragment.ION_B) != 0) output.Add("b");
            if ((peptide.FragmentType & PeptideFragment.ION_C) != 0) output.Add("c");
            if ((peptide.FragmentType & PeptideFragment.ION_D) != 0) output.Add("c");
            if ((peptide.FragmentType & PeptideFragment.ION_U) != 0) output.Add("u");
            if ((peptide.FragmentType & PeptideFragment.ION_V) != 0) output.Add("v");
            if ((peptide.FragmentType & PeptideFragment.ION_W) != 0) output.Add("w");
            if ((peptide.FragmentType & PeptideFragment.ION_X) != 0) output.Add("x");
            if ((peptide.FragmentType & PeptideFragment.ION_Y) != 0) output.Add("y");
            if ((peptide.FragmentType & PeptideFragment.ION_Z) != 0) output.Add("z");
            if ((peptide.FragmentType & PeptideFragment.ION_INTERNALFRAGMENT) != 0) output.Add("internal");
            if ((peptide.FragmentType & PeptideFragment.ION_PRECURSOR) != 0) output.Add("precursor");
            if ((peptide.FragmentType & PeptideFragment.ION_IMMONIUM) != 0) output.Add("immonium");
            if ((peptide.FragmentType & PeptideFragment.ION_UNASSIGNED) != 0) output.Add("unassigned");
            if ((peptide.FragmentType & PeptideFragment.ION_DIAGNOSTIC) != 0) output.Add("diagnostic");
            if ((peptide.FragmentType & PeptideFragment.ION_NONE) != 0) output.Add("none");
            if ((peptide.FragmentType & PeptideFragment.ION_END_OF_LIST) != 0) output.Add("end");
            return output;
        }

        public class PeptideSpectrum
        {
            public readonly string ScanID;
            public readonly (HeckLib.chemistry.PeptideFragment Fragment, HeckLib.Centroid Centroid)[] MatchedFragments;
            public readonly HeckLib.chemistry.PeptideFragment[] TheoreticalFragments;

            public PeptideSpectrum(string scanID, HeckLib.chemistry.PeptideFragment[] matchedFragments, HeckLib.Centroid[] matchedPeaks, HeckLib.chemistry.PeptideFragment[] theoreticalFragments)
            {
                ScanID = scanID;
                MatchedFragments = matchedFragments.Zip(matchedPeaks).ToArray();
                TheoreticalFragments = theoreticalFragments;
            }
        }
    }
}