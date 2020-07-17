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
        /// To dictate how this run should behave.
        /// </summary>
        public enum RuntypeValue
        {
            /// <summary>
            /// Dictates that all input files should be run separate from each other.
            /// </summary>
            Separate,

            /// <summary>
            /// Dictates that all input files should be run in one group, with all information aggregated.
            /// </summary>
            Group
        }

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
            public InputData Data = null;

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
            }

            public class InputLocalParameters
            {
                public PeaksParameters Peaks;
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
            }

            /// <summary>
            /// A dataparameter for PEAKS input files.
            /// </summary>
            public class Peaks : Parameter
            {
                public PeaksParameters Parameter = new PeaksParameters();

                /// <summary>
                /// The file format of the PEAKS file.
                /// </summary>
                public FileFormat.Peaks FileFormat = AssemblyNameSpace.FileFormat.Peaks.PeaksX();
            }

            /// <summary>
            /// A parameter for simple reads files.
            /// </summary>
            public class Reads : Parameter { }

            /// <summary>
            /// A parameter for FASTA reads files.
            /// </summary>
            public class FASTA : Parameter
            {
                /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
                public Regex Identifier = new Regex("(.*)");
            }

            public class PeaksParameters
            {
                public char Separator = ',';
                public char DecimalSeparator = '.';
                public int CutoffALC = 95;
                public int LocalCutoffALC = 90;
                public int MinLengthPatch = 8;
            }
        }

        /// <summary>
        /// To contain options for values of K.
        /// </summary>
        public class K
        {
            /// <summary>
            /// A value for K.
            /// </summary>
            public abstract class KValue { }

            /// <summary>
            /// A single value for K.
            /// </summary>
            public class Single : KValue
            {
                /// <summary>
                /// The value of K.
                /// </summary>
                public int Value;

                /// <summary>
                /// Sets the value.
                /// </summary>
                /// <param name="value">The value.</param>
                public Single(int value)
                {
                    Value = value;
                }
            }

            /// <summary>
            /// Multiple values for K, will be run in different SingleRuns.
            /// </summary>
            public class Multiple : KValue
            {
                /// <summary>
                /// The values.
                /// </summary>
                public int[] Values;

                /// <summary>
                /// Sets the values.
                /// </summary>
                /// <param name="values">The values.</param>
                public Multiple(int[] values)
                {
                    Values = values;
                }
            }

            /// <summary>
            /// A range of values for K, will be run in different SingleRuns.
            /// </summary>
            public class Range : KValue
            {
                /// <summary>
                /// The start of the range (included).
                /// </summary>
                public int Start;

                /// <summary>
                /// The end of the range (if an integral amount of steps from the start it is included).
                /// </summary>
                public int End;

                /// <summary>
                /// The size of the steps, default is 1.
                /// </summary>
                public int Step;

                /// <summary>
                /// Sets the default stepsize.
                /// </summary>
                public Range()
                {
                    Step = 1;
                }
            }
        }

        /// <summary>
        /// Like a boolean but with a third option 'Unspecified' to represent three states of a system.
        /// </summary>
        public enum Trilean { True, False, Unspecified }

        public enum ScoringParameter { Absolute, Relative }

        /// <summary>
        /// The possible values for Reverse.
        /// </summary>
        public enum ReverseValue
        {
            /// <summary>
            /// Turns on Reverse
            /// </summary>
            True,

            /// <summary>
            /// Turns off reverse
            /// </summary>
            False,

            /// <summary>
            /// Runs both with and without Reverse in different SingleRuns
            /// </summary>
            Both
        }

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
        }

        public class AssemblerParameter
        {
            /// <summary>
            /// To contain a local definition of the input
            /// </summary>
            public Input Input = null;

            /// <summary>
            /// The K or values of K for this run.
            /// </summary>
            public K.KValue K = null;

            /// <summary>
            /// The value of Reverse for this run.
            /// </summary>
            public ReverseValue Reverse = ReverseValue.False;

            /// <summary>
            /// The value for the MinimalHomology.
            /// </summary>
            public List<KArithmetic> MinimalHomology = new List<KArithmetic>();

            /// <summary>
            /// The value for the duplicatethreshold.
            /// </summary>
            public List<KArithmetic> DuplicateThreshold = new List<KArithmetic>();

            /// <summary>
            /// The alphabets to be used in this run.
            /// </summary>
            public AlphabetParameter Alphabet = null;
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
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public Trilean IncludeShortReads = Trilean.Unspecified;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public Trilean ForceOnSingleTemplate = Trilean.Unspecified;

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
            public double CutoffScore = 0;

            /// <summary>
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public bool IncludeShortReads = true;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public bool ForceOnSingleTemplate = false;

            /// <summary>
            /// The templates themselves.
            /// </summary>
            public List<DatabaseValue> Databases = new List<DatabaseValue>();
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
            public double CutoffScore = 0;

            /// <summary>
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public Trilean IncludeShortReads = Trilean.Unspecified;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public Trilean ForceOnSingleTemplate = Trilean.Unspecified;

            /// <summary>
            /// The amount of templates to recombine from the highest scoring Databases.
            /// </summary>
            public int N = 0;

            /// <summary>
            /// The order in which the templates are to be recombined.
            /// </summary>
            public List<RecombineOrder.OrderPiece> Order = new List<RecombineOrder.OrderPiece>();

            /// <summary>
            /// The parameters for the read alignment, if the step is to be taken
            /// </summary>
            public ReadAlignmentParameter ReadAlignment = null;
        }

        namespace RecombineOrder
        {
            /// <summary>
            /// An abstract class to contain the order of templates.
            /// </summary>
            public abstract class OrderPiece { }

            /// <summary>
            /// Introduce a gap in the recombined templates.
            /// </summary>
            public class Gap : OrderPiece
            {
                public Gap() { }
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
            }
        }

        public class ReadAlignmentParameter
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
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public Trilean IncludeShortReads = Trilean.Unspecified;

            /// <summary>
            /// Whether or not reads/paths will be forced to a single template.
            /// </summary>
            public Trilean ForceOnSingleTemplate = Trilean.Unspecified;

            public Input Input;
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

                    output.Replace("{id}", r.ID.ToString());
                    output.Replace("{k}", r.K.ToString());
                    output.Replace("{mh}", r.MinimalHomology.ToString());
                    output.Replace("{dt}", r.DuplicateThreshold.ToString());
                    output.Replace("{alph}", r.Alphabet.Name);
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
            public class HTML : Parameter
            {
                /// <summary>
                /// To indicate if the included Dot distribution should be used.
                /// </summary>
                public bool UseIncludedDotDistribution = true;
            }

            /// <summary>
            /// To indicate to return a CSV report.
            /// </summary>
            public class CSV : Parameter
            {
                /// <summary>
                /// To get an ID for a CSV line.
                /// </summary>
                /// <param name="r">The values of the parameters for this run.</param>
                /// <returns>An ID.</returns>
                public string GetID(SingleRun r)
                {
                    return r.ID.ToString();
                }
            }

            /// <summary>
            /// The type sequences in the fasta to give as output
            /// </summary>
            public enum FastaOutputType { Assembly, Recombine, ReadsAlign }

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
                public FastaOutputType OutputType = FastaOutputType.Assembly;
            }
        }
    }
}