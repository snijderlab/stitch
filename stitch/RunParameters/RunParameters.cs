using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Stitch {
    namespace RunParameters {
        /// <summary> To contain parameters for the input of data. </summary>
        public class InputData {
            /// <summary> To contain overrules of the global input parameters </summary>
            public InputLocalParameters LocalParameters = null;

            /// <summary> To contain a local definition of the input </summary>
            public InputParameters Parameters = null;

            /// <summary> To contain the input data itself </summary>
            public ActualData Data = new ActualData();

            public class ActualData {
                /// <summary> The inputs for this run. </summary>
                public List<List<ReadFormat.General>> Raw = new();
                public List<ReadFormat.General> Cleaned = new();
            }

            public class InputParameters {
                public List<RunParameters.InputData.Parameter> Files = new List<Parameter>();
            }

            public class InputLocalParameters {
                public PeaksParameters Peaks = null;
            }

            /// <summary> A parameter to save an input file. </summary>
            public abstract class Parameter {
                /// <summary> The identifier of the file. </summary>
                public ReadFormat.FileIdentifier File = new ReadFormat.FileIdentifier();
            }

            /// <summary> A data parameter for PEAKS input files. </summary>
            public class Peaks : Parameter {
                public PeaksParameters Parameter = new PeaksParameters(true);

                /// <summary> The file format of the PEAKS file. </summary>
                public PeaksFileFormat FileFormat = PeaksFileFormat.PeaksX();
                public string RawDataDirectory = null;
                public ReadFormat.FileIdentifier DeNovoMatchIons = null;
                public char Separator = ',';
                public char DecimalSeparator = '.';
                public bool XleDisambiguation = false;
            }

            /// <summary> A data parameter for MaxNovo input files. </summary>
            public class MaxNovo : Parameter {
                public string RawDataDirectory = null;
                public bool XleDisambiguation = false;
                public double CutoffScore = 0.0;
                public int MinLength = 0;
                public List<(char, double)> FixedModification = new List<(char, double)>();
            }
            /// <summary> A data parameter for pNovo input files. </summary>
            public class pNovo : Parameter {
                public string RawDataDirectory = null;
                public ReadFormat.FileIdentifier ParamFile = null;
                public bool XleDisambiguation = false;
                public double CutoffScore = 0.0;
                public List<(char Find, char Replace, double Shift, string Name)> Modifications = new();
                public HeckLib.masspec.Spectrum.FragmentationType FragmentationMethod = HeckLib.masspec.Spectrum.FragmentationType.All;
            }

            /// <summary> A parameter for simple reads files. </summary>
            public class Reads : Parameter {
            }

            /// <summary> A parameter for FASTA reads files. </summary>
            public class FASTA : Parameter {
                /// <summary> To parse the identifier from the header string in the fasta file </summary>
                public Regex Identifier = new Regex("(.*)");
            }

            /// <summary> A parameter for Novor reads files. </summary>
            public class Novor : Parameter {
                /// <summary> To parse the identifier from the header string in the fasta file </summary>
                public char Separator = ',';
                public ReadFormat.FileIdentifier DeNovoFile = null;
                public ReadFormat.FileIdentifier PSMSFile = null;
                public string RawFile = null;
                public bool XleDisambiguation = false;
                public uint CutoffScore = 0;
            }

            public class MMCIF : Parameter {
                public uint MinLength = 5;
                public uint CutoffALC = 0;
            }

            public class Casanovo : Parameter {
                public double CutoffScore = 0.0;
                public int FilterPPM = -1;
                public string RawDataDirectory = null;
                public bool XleDisambiguation = false;
                public HeckLib.masspec.Spectrum.FragmentationType FragmentationMethod = HeckLib.masspec.Spectrum.FragmentationType.All;
            }

            public class PeaksParameters {
                public int CutoffALC;
                public int LocalCutoffALC;
                public int MinLengthPatch;

                public PeaksParameters(bool defaultValues) {
                    CutoffALC = defaultValues ? 90 : -1;
                    LocalCutoffALC = -1;
                    MinLengthPatch = -1;
                }
            }
        }

        /// <summary> Like a boolean but with a third option 'Unspecified' to represent three states of a system. </summary>
        public enum Trilean { True, False, Unspecified }

        public enum ScoringParameter { Absolute, Relative }

        /// <summary> An input for an alphabet. </summary>
        public class AlphabetParameter {
            /// <summary> The data, Paths should be looked up to find the data. </summary>
            public sbyte[,] ScoringMatrix;

            /// <summary> The name for this alphabet, to recognize it. </summary>
            public char[] Alphabet;

            /// <summary> The penalty for opening a gap in an alignment. </summary>
            public sbyte GapStart = -12;

            /// <summary> The penalty for extending a gap in an alignment. </summary>
            public sbyte GapExtend = -1;
            /// <summary> The maximum length of a patch in alignment, 1 gives normal SW/NW alignment. </summary>
            public int PatchLength = 1;
            /// <summary> The score per residue in a swap, eg {QA} vs {AQ} with swap 2 would give a score of 4. </summary>
            public sbyte Swap = 0;
            public string Name = "";
        }

        /// <summary> An input for a template. </summary>
        public class SegmentValue {
            /// <summary> The alphabet to be used for all templates. </summary>
            public ScoringMatrix Alphabet = null;

            /// <summary> The average score needed for a path to be included in the alignment with a template. </summary>
            public double CutoffScore = 0;

            /// <summary> The name for this template, to recognize it. </summary>
            public string Name = null;

            /// <summary> The templates of this segment </summary>
            public List<ReadFormat.General> Templates = new();
            /// <summary> The scoring system of this segment, whether it will use Absolute (scores are just added up) or relative (scores are divided by the length of the template). </summary>
            public ScoringParameter Scoring = ScoringParameter.Absolute;

            /// <summary> To parse the identifier from the header string in the fasta file </summary>
            public Regex Identifier = new Regex("(.*)");
            public uint GapTail = 0;
            public uint GapHead = 0;
        }

        public class TemplateMatchingParameter {
            /// <summary> The alphabet to be used for all templates. </summary>
            public ScoringMatrix Alphabet = null;

            /// <summary> The average score needed for a path to be included in the alignment with a template. </summary>
            public double CutoffScore = 10;

            /// <summary> Whether or not reads/paths will be forced to a single template. </summary>
            public double EnforceUnique = 1.0;

            /// <summary> This will enforce unique , but allows a read to be placed uniquely multiple times with different parts of its sequence. </summary>
            public bool EnforceUniqueLocalised = true;

            /// <summary> To force consensus Leucines to Isoleucine if the germline has an Isoleucine on that position. </summary>
            public bool ForceGermlineIsoleucine = true;

            /// <summary> Turns the tree generation on or off. </summary>
            public bool BuildTree = true;

            /// <summary> The threshold which determines if a position is seen as ambiguous. Saved as fraction. </summary>
            public double AmbiguityThreshold = 0.75;

            /// <summary> The templates themselves. Grouped by their template group. </summary>
            public List<(String Name, List<SegmentValue> Segments)> Segments = new List<(String, List<SegmentValue>)>();
        }

        /// <summary> To contain all parameters for recombination of Segments. </summary>
        public class RecombineParameter {
            /// <summary> The alphabet to be used for all templates. </summary>
            public ScoringMatrix Alphabet = null;

            /// <summary> The average score needed for a path to be included in the alignment with a template. </summary>
            public double CutoffScore = 10;

            /// <summary> Whether or not reads/paths will be forced to a single template. </summary>
            public Option<double> EnforceUnique = new Option<double>();

            /// <summary> This will enforce unique , but allows a read to be placed uniquely multiple times with different parts of its sequence. </summary>
            public bool EnforceUniqueLocalised = false;

            /// <summary> To force consensus Leucines to Isoleucine if the germline has an Isoleucine on that position. </summary>
            public Trilean ForceGermlineIsoleucine = Trilean.Unspecified;

            /// <summary> The amount of templates to recombine from the highest scoring Segments. </summary>
            public int N = 1;

            /// <summary> The order in which the templates are to be recombined. The outer list contains the template matching groups in the same order as in the template matching definition. </summary>
            public List<List<RecombineOrder.OrderPiece>> Order = new List<List<RecombineOrder.OrderPiece>>();

            /// <summary> To determine if an automatic decoy segment has to be set up. This segment will contain all unused templates from template matching to remove background from the recombination step. </summary>
            public bool Decoy = false;
        }

        namespace RecombineOrder {
            /// <summary> An abstract class to contain the order of templates. </summary>
            public abstract class OrderPiece {
                public abstract bool IsGap();
            }

            /// <summary> Introduce a gap in the recombined templates. </summary>
            public class Gap : OrderPiece {
                public Gap() { }

                public override bool IsGap() {
                    return true;
                }
            }

            /// <summary> Introduce a template in the recombined templates. </summary>
            public class Template : OrderPiece {
                /// <summary> The index in the Templates list of the enclosing RecombineValue. </summary>
                public int Index;
                public Template(int i) {
                    Index = i;
                }

                public override bool IsGap() {
                    return false;
                }
            }
        }

        public class ReportParameter {
            /// <summary> The report(s) to be generated for this run. </summary>
            public List<Report.Parameter> Files = new List<Report.Parameter>();
            /// <summary> The base folder where all generated reports will be stored (or at least relative to)
            /// if undefined it is interpreted as the folder the batchfile is saved in. </summary>
            public String Folder = null;

            /// <summary> Generates a (unique) name based on the given template. </summary>
            /// <param name="r">The values for the parameters.</param>
            /// <param name="input">The path template.</param>
            /// <returns>A name.</returns>
            public static string CreateName(Run r, String input) {
                var output = new StringBuilder(input);

                output.Replace("{alph}", r.Alphabet != null ? r.Alphabet.Name : "NoAlphabet");
                output.Replace("{name}", r.Runname);
                output.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                output.Replace("{time}", DateTime.Now.ToString("HH-mm-ss"));
                output.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd@HH-mm-ss"));

                return output.ToString();
            }
        }

        /// <summary> To contain parameters for reporting. </summary>
        public class Report {
            /// <summary> A parameter to define how to report the results. </summary>
            public abstract class Parameter {
                /// <summary> The path to save the result to. </summary>
                public string Path = null;

                /// <summary> Generates a (unique) name based on the given template. </summary>
                /// <param name="r">The values for the parameters.</param>
                /// <returns>A name.</returns>
                public string CreateName(String folder, Run r) {
                    if (string.IsNullOrEmpty(folder))
                        return ReportParameter.CreateName(r, Path);
                    else
                        return System.IO.Path.GetFullPath(ReportParameter.CreateName(r, Path), folder);
                }
            }

            /// <summary> To indicate to return an HTML report. </summary>
            public class HTML : Parameter {
            }

            /// <summary> To indicate to return an JSON report. </summary>
            public class JSON : Parameter {
            }

            /// <summary> To indicate to return an FabLab report. </summary>
            public class FabLab : Parameter {
            }

            /// <summary> The type sequences in the fasta to give as output </summary>
            public enum OutputType { TemplateMatching, Recombine }

            /// <summary> To indicate to return a FASTA report. </summary>
            public class FASTA : Parameter {
                /// <summary> The minimal score needed to be included. </summary>
                public int MinimalScore = 0;

                /// <summary> The output type of the sequences </summary>
                public OutputType OutputType = OutputType.TemplateMatching;
            }

            /// <summary> To indicate to return a CSV report. </summary>
            public class CSV : Parameter {
                /// <summary> The output type of the sequences </summary>
                public OutputType OutputType = OutputType.TemplateMatching;
            }
        }
    }
}