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
    namespace RunParameters
    {
        /// <summary>
        /// To contain parameters for the input of data.
        /// </summary>
        public class InputData
        {
            /// <summary>
            /// To contain overrules of the global input parameters
            /// </summary>
            public InputLocalParameters LocalParameters = null;

            /// <summary>
            /// To contain a local definition of the input
            /// </summary>
            public InputParameters Parameters = null;

            /// <summary>
            /// To contain the input data itself
            /// </summary>
            public ActualData Data = new ActualData();

            public class ActualData
            {
                /// <summary>
                /// The inputs for this run.
                /// </summary>
                public List<List<(string Sequence, ReadMetaData.IMetaData MetaData)>> Raw = new List<List<(string, ReadMetaData.IMetaData)>>();
                public List<(string Sequence, ReadMetaData.IMetaData MetaData)> Cleaned = new List<(string, ReadMetaData.IMetaData)>();
            }

            public class InputParameters
            {
                public List<RunParameters.InputData.Parameter> Files = new List<Parameter>();

                public string Display()
                {
                    var buf = new StringBuilder();
                    buf.AppendLine("InputParameters ->");
                    foreach (var file in Files)
                    {
                        buf.AppendLine(file.Display());
                    }
                    buf.Append("<-");
                    return buf.ToString();
                }
            }

            public class InputLocalParameters
            {
                public PeaksParameters Peaks = null;

                public string Display()
                {
                    if (Peaks == null) return "";
                    return $"InputLocalParameters ->\n{Peaks.Display()}\n<-";
                }
            }

            public string Display()
            {
                var output = "Input ->\n";
                if (LocalParameters != null) output += LocalParameters.Display();
                if (Parameters != null) output += Parameters.Display();
                output += "<-";
                return output;
            }

            /// <summary>
            /// A parameter to save an input file.
            /// </summary>
            public abstract class Parameter
            {
                /// <summary>
                /// The identifier of the file.
                /// </summary>
                public ReadMetaData.FileIdentifier File = new ReadMetaData.FileIdentifier();

                public abstract string Display();
            }

            /// <summary>
            /// A dataparameter for PEAKS input files.
            /// </summary>
            public class Peaks : Parameter
            {
                public PeaksParameters Parameter = new PeaksParameters(true);

                /// <summary>
                /// The file format of the PEAKS file.
                /// </summary>
                public FileFormat.Peaks FileFormat = AssemblyNameSpace.FileFormat.Peaks.PeaksX();
                public char Separator = ',';
                public char DecimalSeparator = '.';

                public override string Display()
                {
                    return $"Peaks ->\n{File.Display()}\n{Parameter.Display()}\nFileFormat: {FileFormat.name}\nSeparator: {Separator}\nDecimalSeparator: {DecimalSeparator}\n<-";
                }
            }

            /// <summary>
            /// A parameter for simple reads files.
            /// </summary>
            public class Reads : Parameter
            {
                public override string Display()
                {
                    return $"Simple ->\n{File.Display()}\n<-";
                }
            }

            /// <summary>
            /// A parameter for FASTA reads files.
            /// </summary>
            public class FASTA : Parameter
            {
                /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
                public Regex Identifier = new Regex("(.*)");

                public override string Display()
                {
                    return $"FASTA ->\n{File.Display()}\nIdentifier: {Identifier}\n<-";
                }
            }

            /// <summary>
            /// A parameter for Novor reads files.
            /// </summary>
            public class Novor : Parameter
            {
                /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
                public char Separator = ',';
                public ReadMetaData.FileIdentifier DeNovoFile = null;
                public ReadMetaData.FileIdentifier PSMSFile = null;
                public uint Cutoff = 0;

                public override string Display()
                {
                    var db = new StringBuilder();
                    db.AppendLine("Novor ->");
                    var name = "";
                    if (DeNovoFile != null)
                    {
                        db.AppendLine($"DeNovo Path:{DeNovoFile.Path}");
                        name = DeNovoFile.Name;
                    }
                    if (PSMSFile != null)
                    {
                        db.AppendLine($"PSMS Path:{PSMSFile.Path}");
                        name = PSMSFile.Name;
                    }
                    db.AppendLine($"Name:{name}");
                    db.AppendLine($"Separator:{Separator}");
                    db.AppendLine($"Cutoff:{Cutoff}\n<-");
                    return db.ToString();
                }
            }

            public class PeaksParameters
            {
                public int CutoffALC;
                public int LocalCutoffALC;
                public int MinLengthPatch;

                public PeaksParameters(bool defaultValues)
                {
                    CutoffALC = defaultValues ? 90 : -1;
                    LocalCutoffALC = -1;
                    MinLengthPatch = -1;
                }

                public string Display()
                {
                    return $"Peaks ->\n\tCutoffALC: {CutoffALC}\n\tLocalCutoffALC: {LocalCutoffALC}\n\tMinLengthPatch: {MinLengthPatch}\n<-";
                }
            }
        }

        /// <summary>
        /// Like a boolean but with a third option 'Unspecified' to represent three states of a system.
        /// </summary>
        public enum Trilean { True, False, Unspecified }

        public enum ScoringParameter { Absolute, Relative }

        /// <summary>
        /// An input for an alphabet.
        /// </summary>
        public class AlphabetParameter
        {
            /// <summary>
            /// The data, Paths should be looked up to find the data.
            /// </summary>
            public int[,] ScoringMatrix;

            /// <summary>
            /// The name for this alphabet, to recognize it.
            /// </summary>
            public char[] Alphabet;

            /// <summary>
            /// The penalty for opening a gap in an alignment.
            /// </summary>
            public int GapStartPenalty = 12;

            /// <summary>
            /// The penalty for extending a gap in an alignment.
            /// </summary>
            public int GapExtendPenalty = 1;
            public string Name = "";

            public string Display()
            {
                return $"Alphabet ->\nName: {Name}\nGapStartPenalty: {GapStartPenalty}\nGapExtendPenalty: {GapExtendPenalty}\n<-";
            }
        }

        /// <summary>
        /// An input for a template.
        /// </summary>
        public class SegmentValue
        {
            /// <summary>
            /// The alphabet to be used for all templates.
            /// </summary>
            public AlphabetParameter Alphabet = null;

            /// <summary>
            /// The average score needed for a path to be included in the alignment with a template.
            /// </summary>
            public double CutoffScore = 0;

            /// <summary>
            /// The name for this template, to recognize it.
            /// </summary>
            public string Name = null;

            /// <summary>
            /// The templates of this segment
            /// </summary>
            public List<(string, ReadMetaData.IMetaData)> Templates = new List<(string, ReadMetaData.IMetaData)>();
            /// <summary>
            /// The scoring system of this segment, whether it will use Absolute (scores are just added up) or relative (scores are divided by the length of the template).
            /// </summary>
            public ScoringParameter Scoring = ScoringParameter.Absolute;

            /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
            public Regex Identifier = new Regex("(.*)");
            public bool GapTail = false;
            public bool GapHead = false;
        }

        public class TemplateMatchingParameter
        {
            /// <summary>
            /// The alphabet to be used for all templates.
            /// </summary>
            public AlphabetParameter Alphabet = null;

            /// <summary>
            /// The average score needed for a path to be included in the alignment with a template.
            /// </summary>
            public double CutoffScore = 10;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public bool EnforceUnique = true;

            /// <summary>
            /// To force consensus Leucines to Isoleucine if the germline has an Isoleucine on that position.
            /// </summary>
            public bool ForceGermlineIsoleucine = true;

            /// <summary>
            /// The templates themselves. Grouped by their template group.
            /// </summary>
            public List<(String Name, List<SegmentValue> Segments)> Segments = new List<(String, List<SegmentValue>)>();
        }

        /// <summary>
        /// To contain all parameters for recombination of Segments.
        /// </summary>
        public class RecombineParameter
        {
            /// <summary>
            /// The alphabet to be used for all templates.
            /// </summary>
            public AlphabetParameter Alphabet = null;

            /// <summary>
            /// The average score needed for a path to be included in the alignment with a template.
            /// </summary>
            public double CutoffScore = 10;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public Trilean EnforceUnique = Trilean.Unspecified;

            /// <summary>
            /// To force consensus Leucines to Isoleucine if the germline has an Isoleucine on that position.
            /// </summary>
            public Trilean ForceGermlineIsoleucine = Trilean.Unspecified;

            /// <summary>
            /// The amount of templates to recombine from the highest scoring Segments.
            /// </summary>
            public int N = 0;

            /// <summary>
            /// The order in which the templates are to be recombined. The outer list contains the template matching groups in the same order as in the template matching definition.
            /// </summary>
            public List<List<RecombineOrder.OrderPiece>> Order = new List<List<RecombineOrder.OrderPiece>>();

            /// <summary>
            /// To determine if an automatic decoy segment has to be set up. This segment will contain all unused templates from template matching to remove background from the recombination step.
            /// </summary>
            public bool Decoy = false;
        }

        namespace RecombineOrder
        {
            /// <summary>
            /// An abstract class to contain the order of templates.
            /// </summary>
            public abstract class OrderPiece
            {
                public abstract bool IsGap();
                public abstract string Display();
            }

            /// <summary>
            /// Introduce a gap in the recombined templates.
            /// </summary>
            public class Gap : OrderPiece
            {
                public Gap() { }

                public override string Display()
                {
                    return Alphabet.GapChar.ToString();
                }

                public override bool IsGap()
                {
                    return true;
                }
            }

            /// <summary>
            /// Introduce a template in the recombined templates.
            /// </summary>
            public class Template : OrderPiece
            {
                /// <summary>
                /// The index in the Templates list of the enclosing RecombineValue.
                /// </summary>
                public int Index;
                public Template(int i)
                {
                    Index = i;
                }

                public override string Display()
                {
                    return Index.ToString();
                }

                public override bool IsGap()
                {
                    return false;
                }
            }
        }

        public class ReportParameter
        {
            /// <summary>
            /// The report(s) to be generated for this run.
            /// </summary>
            public List<Report.Parameter> Files = new List<Report.Parameter>();
            /// <summary>
            /// The base folder where all generated reports will be stored (or at least relative to) 
            /// if undefined it is interpreted as the folder the batchfile is saved in.
            /// </summary>
            public String Folder = null;

            /// <summary>
            /// Generates a (unique) name based on the given template.
            /// </summary>
            /// <param name="r">The values for the parameters.</param>
            /// <param name="input">The path template.</param>
            /// <returns>A name.</returns>
            public static string CreateName(SingleRun r, String input)
            {
                var output = new StringBuilder(input);

                output.Replace("{alph}", r.Alphabet != null ? r.Alphabet.Name : "NoAlphabet");
                output.Replace("{name}", r.Runname);
                output.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                output.Replace("{time}", DateTime.Now.ToString("HH-mm-ss"));
                output.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd@HH-mm-ss"));

                return output.ToString();
            }
        }

        /// <summary>
        /// To contain parameters for reporting.
        /// </summary>
        public class Report
        {
            /// <summary>
            /// A parameter to define how to report the results.
            /// </summary>
            public abstract class Parameter
            {
                /// <summary>
                /// The path to save the result to.
                /// </summary>
                public string Path = null;

                /// <summary>
                /// Generates a (unique) name based on the given template.
                /// </summary>
                /// <param name="r">The values for the parameters.</param>
                /// <returns>A name.</returns>
                public string CreateName(String folder, SingleRun r)
                {
                    if (folder != null)
                        return System.IO.Path.GetFullPath(ReportParameter.CreateName(r, Path), folder);
                    else
                        return ReportParameter.CreateName(r, Path);
                }
            }

            /// <summary>
            /// To indicate to return an HTML report.
            /// </summary>
            public class HTML : Parameter { }

            /// <summary>
            /// The type sequences in the fasta to give as output
            /// </summary>
            public enum OutputType { TemplateMatches, Recombine }

            /// <summary>
            /// To indicate to return a FASTA report.
            /// </summary>
            public class FASTA : Parameter
            {
                /// <summary>
                /// The minimal score needed to be included.
                /// </summary>
                public int MinimalScore = 0;

                /// <summary>
                /// The outputtype of the sequences
                /// </summary>
                public OutputType OutputType = OutputType.TemplateMatches;
            }

            /// <summary>
            /// To indicate to return a CSV report.
            /// </summary>
            public class CSV : Parameter
            {
                /// <summary>
                /// The outputtype of the sequences
                /// </summary>
                public OutputType OutputType = OutputType.TemplateMatches;
            }
        }
    }
}