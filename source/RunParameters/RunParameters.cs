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
        /// The type of an input file
        /// </summary>
        public enum InputType { Detect, Reads, Fasta, Peaks }
        /// <summary>
        /// To contain parameters for the input of data
        /// </summary>
        public class Input
        {
            /// <summary>
            /// A parameter to save an input file.
            /// </summary>
            public abstract class Parameter
            {
                /// <summary>
                /// The identifier of the file
                /// </summary>
                public MetaData.FileIdentifier File;
                /// <summary>
                /// Creates a blank file identifer
                /// </summary>
                public Parameter()
                {
                    File = new MetaData.FileIdentifier();
                }
            }
            /// <summary>
            /// A dataparameter for PEAKS input files
            /// </summary>
            public class Peaks : Parameter
            {
                /// <summary>
                /// The cutoffscore 
                /// </summary>
                public int Cutoffscore;
                /// <summary>
                /// The cuttoffscore for patches
                /// </summary>
                public int LocalCutoffscore;
                /// <summary>
                /// The file format of the PEAKS file
                /// </summary>
                public FileFormat.Peaks FileFormat;
                /// <summary>
                /// The minimal length of a patch
                /// </summary>
                public int MinLengthPatch;
                /// <summary>
                /// The separator used in CSV
                /// </summary>
                public char Separator;
                /// <summary>
                /// The decimal separator used
                /// </summary>
                public char DecimalSeparator;
                /// <summary>
                /// Fills in the default values
                /// </summary>
                public Peaks()
                    : base()
                {
                    Cutoffscore = 99;
                    LocalCutoffscore = 90;
                    FileFormat = AssemblyNameSpace.FileFormat.Peaks.NewFormat();
                    MinLengthPatch = 3;
                    Separator = ',';
                    DecimalSeparator = '.';
                }
            }
            /// <summary>
            /// A parameter for simple reads files
            /// </summary>
            public class Reads : Parameter
            {
                /// <summary>
                /// Fills in default values
                /// </summary>
                public Reads() : base() { }
            }
            /// <summary>
            /// A parameter for FASTA reads files
            /// </summary>
            public class FASTA : Parameter
            {
                /// <summary>
                /// Fills in default values
                /// </summary>
                public FASTA() : base() { }
            }
        }
        /// <summary>
        /// To contain options for values of K
        /// </summary>
        public class K
        {
            /// <summary>
            /// A value for K
            /// </summary>
            public abstract class KValue { }
            /// <summary>
            /// A single value for K
            /// </summary>
            public class Single : KValue
            {
                /// <summary>
                /// The value of K
                /// </summary>
                public int Value;
                /// <summary>
                /// Sets the value
                /// </summary>
                /// <param name="value">The value</param>
                public Single(int value)
                {
                    Value = value;
                }
            }
            /// <summary>
            /// Multiple values for K, will be run in different SingleRuns
            /// </summary>
            public class Multiple : KValue
            {
                /// <summary>
                /// The values
                /// </summary>
                public int[] Values;
                /// <summary>
                /// Sets the values
                /// </summary>
                /// <param name="values">The values</param>
                public Multiple(int[] values)
                {
                    Values = values;
                }
            }
            /// <summary>
            /// A range of values for K, will be run in different SingleRuns
            /// </summary>
            public class Range : KValue
            {
                /// <summary>
                /// The start of the range (included)
                /// </summary>
                public int Start;
                /// <summary>
                /// The end of the range (if an integral amount of steps from the start it is included)
                /// </summary>
                public int End;
                /// <summary>
                /// The size of the steps, default is 1
                /// </summary>
                public int Step;
                /// <summary>
                /// Sets the default stepsize
                /// </summary>
                public Range()
                {
                    Step = 1;
                }
            }
        }
        /// <summary>
        /// The possible values for Reverse
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
        /// An input for an alphabet
        /// </summary>
        public class AlphabetValue
        {
            /// <summary>
            /// The data, Paths should be looked up to find the data
            /// </summary>
            public string Data;
            /// <summary>
            /// The name for this alphabet, to recognize it
            /// </summary>
            public string Name;
            /// <summary>
            /// The penalty for opening a gap in an alignment
            /// </summary>
            public int GapStartPenalty = 12;
            /// <summary>
            /// The penalty for extending a gap in an alignment
            /// </summary>
            public int GapExtendPenalty = 1;
        }
        /// <summary>
        /// An input for a template
        /// </summary>
        public class TemplateValue
        {
            /// <summary>
            /// The path to the file
            /// </summary>
            public string Path;
            /// <summary>
            /// The name for this template, to recognize it
            /// </summary>
            public string Name;
            /// <summary>
            /// The type of the file
            /// </summary>
            public InputType Type;
            /// <summary>
            /// The alphabet to be used for template matching
            /// </summary>
            public AlphabetValue Alphabet;
            public double CutoffScore = 0;
        }
        /// <summary>
        /// To contain all parameters for recombination of templates
        /// </summary>
        public class RecombineValue
        {
            /// <summary>
            /// The amount of templates to recombine from the highest scoring templates
            /// </summary>
            public int N;
            /// <summary>
            /// The templates to recombine
            /// </summary>
            public List<TemplateValue> Templates = new List<TemplateValue>();
            /// <summary>
            /// The alphabet to be used for all templates
            /// </summary>
            public AlphabetValue Alphabet;
            /// <summary>
            /// The order in which the templates are to be recombined
            /// </summary>
            public List<RecombineOrder.OrderPiece> Order = new List<RecombineOrder.OrderPiece>();
            public double CutoffScore = 0;
        }

        namespace RecombineOrder
        {
            /// <summary>
            /// An abstract class to contain the order of templates
            /// </summary>
            public abstract class OrderPiece { }
            /// <summary>
            /// Introduce a gap in the recombined templates
            /// </summary>
            public class Gap : OrderPiece
            {
                public Gap() { }
            }
            /// <summary>
            /// Introduce a template in the recombined templates
            /// </summary>
            public class Template : OrderPiece
            {
                /// <summary>
                /// The index in the Templates list of the enclosing RecombineValue
                /// </summary>
                public int Index;
                public Template(int i)
                {
                    Index = i;
                }
            }
        }

        /// <summary>
        /// To contain parameters for reporting
        /// </summary>
        public class Report
        {
            /// <summary>
            /// A parameter to define how to report the results
            /// </summary>
            public abstract class Parameter
            {
                /// <summary>
                /// The path to save the result to
                /// </summary>
                public string Path;
                /// <summary>
                /// Generates a (unique) name based on the given template
                /// </summary>
                /// <param name="r">The values for the parameters</param>
                /// <returns>A name</returns>
                public string CreateName(SingleRun r)
                {
                    var output = new StringBuilder(Path);

                    output.Replace("{id}", r.ID.ToString());
                    output.Replace("{k}", r.K.ToString());
                    output.Replace("{mh}", r.MinimalHomology.ToString());
                    output.Replace("{dt}", r.DuplicateThreshold.ToString());
                    output.Replace("{alph}", r.Alphabet.Name);
                    output.Replace("{data}", r.Input.Count() == 1 ? r.Input[0].File.Name : "Group");
                    output.Replace("{name}", r.Runname);
                    output.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                    output.Replace("{time}", DateTime.Now.ToString("hh-mm-ss"));
                    output.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd@hh-mm-ss"));

                    return output.ToString();
                }
            }
            /// <summary>
            /// To indicate to return an HTML report
            /// </summary>
            public class HTML : Parameter
            {
                /// <summary>
                /// To indicate if the included Dot distribution should be used
                /// </summary>
                public bool UseIncludedDotDistribution = true;
            }
            /// <summary>
            /// To indicate to return a CSV report
            /// </summary>
            public class CSV : Parameter
            {
                /// <summary>
                /// To get an ID for a CSV line
                /// </summary>
                /// <param name="r">The values of the parameters for this run</param>
                /// <returns>An ID</returns>
                public string GetID(SingleRun r)
                {
                    return r.ID.ToString();
                }
            }
            /// <summary>
            /// To indicate to return a FASTA report
            /// </summary>
            public class FASTA : Parameter
            {
                /// <summary>
                /// The minimal score needed to be included
                /// </summary>
                public int MinimalScore = 0;
            }
        }
    }
}