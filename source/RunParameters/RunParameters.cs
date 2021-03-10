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
        public class Input
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
            public InputData Data = new InputData();

            public class InputData
            {
                /// <summary>
                /// The inputs for this run.
                /// </summary>
                public List<List<(string Sequence, MetaData.IMetaData MetaData)>> Raw = new List<List<(string, MetaData.IMetaData)>>();
                public List<(string Sequence, MetaData.IMetaData MetaData)> Cleaned = new List<(string, MetaData.IMetaData)>();
            }

            public class InputParameters
            {
                public List<Input.Parameter> Files = new List<Input.Parameter>();

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
                public MetaData.FileIdentifier File = new MetaData.FileIdentifier();

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
        public class DatabaseValue
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
            /// The templates of this database
            /// </summary>
            public List<(string, MetaData.IMetaData)> Templates = new List<(string, MetaData.IMetaData)>();
            /// <summary>
            /// The scoring system of this database, whether it will use Absolute (scores are just added up) or relative (scores are divided by the length of the template).
            /// </summary>
            public ScoringParameter Scoring = ScoringParameter.Absolute;

            /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
            public Regex Identifier = new Regex("(.*)");
            public int ClassChars = -1;
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
            public bool ForceOnSingleTemplate = true;

            /// <summary>
            /// The templates themselves. Grouped by their template group.
            /// </summary>
            public List<(String Name, List<DatabaseValue> Databases)> Databases = new List<(String, List<DatabaseValue>)>();
        }

        /// <summary>
        /// To contain all parameters for recombination of Databases.
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
            public Trilean ForceOnSingleTemplate = Trilean.Unspecified;

            /// <summary>
            /// The amount of templates to recombine from the highest scoring Databases.
            /// </summary>
            public int N = 0;

            /// <summary>
            /// The order in which the templates are to be recombined. The outer list contains the template matching groups in the same order as in the template matching definition.
            /// </summary>
            public List<List<RecombineOrder.OrderPiece>> Order = new List<List<RecombineOrder.OrderPiece>>();
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
                    return "*";
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
                public string CreateName(SingleRun r)
                {
                    var output = new StringBuilder(Path);

                    output.Replace("{alph}", r.Alphabet != null ? r.Alphabet.Name : "NoAlphabet");
                    output.Replace("{name}", r.Runname);
                    output.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                    output.Replace("{time}", DateTime.Now.ToString("hh-mm-ss"));
                    output.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd@hh-mm-ss"));

                    return output.ToString();
                }
            }

            /// <summary>
            /// To indicate to return an HTML report.
            /// </summary>
            public class HTML : Parameter { }

            /// <summary>
            /// The type sequences in the fasta to give as output
            /// </summary>
            public enum FastaOutputType { TemplateMatches, Recombine }

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
                public FastaOutputType OutputType = FastaOutputType.TemplateMatches;
            }
        }
    }
}