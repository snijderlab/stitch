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
            /// A parameter to save an input file.
            /// </summary>
            public abstract class Parameter
            {
                /// <summary>
                /// The identifier of the file.
                /// </summary>
                public MetaData.FileIdentifier File;

                /// <summary>
                /// Creates a blank file identifer.
                /// </summary>
                public Parameter()
                {
                    File = new MetaData.FileIdentifier();
                }
            }

            /// <summary>
            /// A dataparameter for PEAKS input files.
            /// </summary>
            public class Peaks : Parameter
            {
                /// <summary>
                /// The cutoffscore.
                /// </summary>
                public int Cutoffscore;

                /// <summary>
                /// The cutoff score for patches.
                /// </summary>
                public int LocalCutoffscore;

                /// <summary>
                /// The file format of the PEAKS file.
                /// </summary>
                public FileFormat.Peaks FileFormat;

                /// <summary>
                /// The minimal length of a patch.
                /// </summary>
                public int MinLengthPatch;

                /// <summary>
                /// The separator used in CSV.
                /// </summary>
                public char Separator;

                /// <summary>
                /// The decimal separator used.
                /// </summary>
                public char DecimalSeparator;

                /// <summary>
                /// Fills in the default values.
                /// </summary>
                public Peaks()
                    : base()
                {
                    Cutoffscore = 99;
                    LocalCutoffscore = 90;
                    FileFormat = AssemblyNameSpace.FileFormat.Peaks.PeaksX();
                    MinLengthPatch = 3;
                    Separator = ',';
                    DecimalSeparator = '.';
                }
            }

            /// <summary>
            /// A parameter for simple reads files.
            /// </summary>
            public class Reads : Parameter
            {
                /// <summary>
                /// Fills in default values.
                /// </summary>
                public Reads() : base() { }
            }

            /// <summary>
            /// A parameter for FASTA reads files.
            /// </summary>
            public class FASTA : Parameter
            {

                /// <summary>
                /// Fills in default values.
                /// </summary>
                public FASTA() : base() { }

                /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
                public Regex Identifier = new Regex("(.*)");
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

        public class AssemblerParameter
        {
            public InputParameter Input;

            /// <summary>
            /// The K or values of K for this run.
            /// </summary>
            public K.KValue K;

            /// <summary>
            /// The value of Reverse for this run.
            /// </summary>
            public ReverseValue Reverse;

            /// <summary>
            /// The value for the MinimalHomology.
            /// </summary>
            public List<KArithmetic> MinimalHomology;

            /// <summary>
            /// The value for the duplicatethreshold.
            /// </summary>
            public List<KArithmetic> DuplicateThreshold;

            /// <summary>
            /// The alphabets to be used in this run.
            /// </summary>
            public AlphabetParameter Alphabet;

            public AssemblerParameter()
            {
                Input = null;
                Reverse = ReverseValue.False;
                MinimalHomology = new List<KArithmetic>();
                DuplicateThreshold = new List<KArithmetic>();
                Alphabet = null;
            }
        }

        public class InputParameter
        {
            /// <summary>
            /// The inputs for this run.
            /// </summary>
            public List<List<(string Sequence, MetaData.IMetaData MetaData)>> Data;
            public List<(string Sequence, MetaData.IMetaData MetaData)> CleanedData;

            public InputParameter()
            {
                Data = new List<List<(string, MetaData.IMetaData)>>();
            }
        }

        public class ReportParameter
        {
            /// <summary>
            /// The report(s) to be generated for this run.
            /// </summary>
            public List<Report.Parameter> Files;

            public ReportParameter()
            {
                Files = new List<Report.Parameter>();
            }
        }

        public class ReadAlignmentParameter
        {
            public InputParameter Input;
            public double CutoffScore = 0;
            public AlphabetParameter Alphabet;
            public bool ForceOnSingleTemplate = false;

            public ReadAlignmentParameter()
            {
                Input = null;
                Alphabet = null;
            }
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

        /// <summary>
        /// An input for a template.
        /// </summary>
        public class DatabaseValue
        {
            /// <summary>
            /// The name for this template, to recognize it.
            /// </summary>
            public string Name;

            /// <summary>
            /// The alphabet to be used for template matching.
            /// </summary>
            public AlphabetParameter Alphabet;

            /// <summary>
            /// The average score needed for a path to be included in the alignment with a template.
            /// </summary>
            public double CutoffScore = 0;

            /// <summary>
            /// The templates of this database
            /// </summary>
            public List<(string, MetaData.IMetaData)> Templates = new List<(string, MetaData.IMetaData)>();

            /// <summary>
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public bool IncludeShortReads = true;

            /// <summary>
            /// The scoring system of this database, whether it will use Absolute (scores are just added up) or relative (scores are divided by the length of the template).
            /// </summary>
            public ScoringParameter Scoring = ScoringParameter.Absolute;

            /// <summary> To parse the identifier from the headerstring in the fasta file </summary>
            public Regex Identifier = new Regex("(.*)");
            public int ClassChars = -1;
        }

        public enum ScoringParameter { Absolute, Relative }

        /// <summary>
        /// To contain all parameters for recombination of Databases.
        /// </summary>
        public class RecombineParameter
        {
            /// <summary>
            /// The amount of templates to recombine from the highest scoring Databases.
            /// </summary>
            public int N;

            /// <summary>
            /// The templates to recombine.
            /// </summary>
            public List<DatabaseValue> Databases = new List<DatabaseValue>();

            /// <summary>
            /// The alphabet to be used for all templates.
            /// </summary>
            public AlphabetParameter Alphabet;

            /// <summary>
            /// The order in which the templates are to be recombined.
            /// </summary>
            public List<RecombineOrder.OrderPiece> Order = new List<RecombineOrder.OrderPiece>();

            /// <summary>
            /// The average score needed for a path to be included in the alignment with a template.
            /// </summary>
            public double CutoffScore = 0;

            /// <summary>
            /// To determine if short reads (&lt;K) should be added back to the recombination database after assembly
            /// </summary>
            public bool IncludeShortReads = true;
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
                public string Path;

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