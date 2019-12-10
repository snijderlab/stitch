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
        /// To give an 'api' for calling the program
        /// </summary>
        public class FullRunParameters
        {
            /// <summary>
            /// The name of this run
            /// </summary>
            public string Runname;
            /// <summary>
            /// The type of this run
            /// </summary>
            public RuntypeValue Runtype;
            /// <summary>
            /// The inputs for this run
            /// </summary>
            public List<Input.Parameter> DataParameters;
            /// <summary>
            /// The K or values of K for this run
            /// </summary>
            public K.KValue K;
            /// <summary>
            /// The value of Reverse for this run
            /// </summary>
            public ReverseValue Reverse;
            /// <summary>
            /// The value for the MinimalHomology
            /// </summary>
            public List<KArithmetic> MinimalHomology;
            /// <summary>
            /// The value for the duplicatethreshold
            /// </summary>
            public List<KArithmetic> DuplicateThreshold;
            /// <summary>
            /// The alphabet(s) to be used in this run
            /// </summary>
            public List<AlphabetValue> Alphabet;
            /// <summary>
            /// The template(s) to be used in this run
            /// </summary>
            public List<TemplateValue> Template;
            /// <summary>
            /// The report(s) to be generated for this run
            /// </summary>
            public List<Report.Parameter> Report;
            /// <summary>
            /// A blank instance for the RunParameters with defaults and initialization
            /// </summary>
            public FullRunParameters()
            {
                Runname = "";
                Runtype = RuntypeValue.Group;
                DataParameters = new List<Input.Parameter>();
                Reverse = ReverseValue.False;
                MinimalHomology = new List<KArithmetic>();
                DuplicateThreshold = new List<KArithmetic>();
                Alphabet = new List<AlphabetValue>();
                Template = new List<TemplateValue>();
                Report = new List<Report.Parameter>();
            }
            /// <summary>
            /// Creates a list of all single runs contained in this run.abstract TO be ran in parallel.
            /// </summary>
            /// <returns>All single runs</returns>
            public List<SingleRun> CreateRuns()
            {
                var output = new List<SingleRun>();

                var reverselist = new List<bool>();
                switch (Reverse)
                {
                    case ReverseValue.True:
                        reverselist.Add(true);
                        break;
                    case ReverseValue.False:
                        reverselist.Add(false);
                        break;
                    case ReverseValue.Both:
                        reverselist.Add(true);
                        reverselist.Add(false);
                        break;
                }

                var klist = new List<int>();
                switch (K)
                {
                    case K.Single s:
                        klist.Add(s.Value);
                        break;
                    case K.Multiple m:
                        klist.AddRange(m.Values);
                        break;
                    case K.Range r:
                        int v = r.Start;
                        while (v <= r.End)
                        {
                            klist.Add(v);
                            v += r.Step;
                        }
                        break;
                }

                int id = 0;
                foreach (var minimalHomology in MinimalHomology)
                {
                    foreach (var duplicateThreshold in DuplicateThreshold)
                    {
                        foreach (var alphabet in Alphabet)
                        {
                            foreach (var reverse in reverselist)
                            {
                                foreach (var k in klist)
                                {
                                    if (Runtype == RuntypeValue.Group)
                                    {
                                        id++;
                                        output.Add(new SingleRun(id, Runname, DataParameters, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Template, Report));
                                    }
                                    else
                                    {
                                        foreach (var input in DataParameters)
                                        {
                                            id++;
                                            output.Add(new SingleRun(id, Runname, input, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Template, Report));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return output;
            }
        }
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
        /// A value possibly based on the value of K
        /// </summary>
        public class KArithmetic
        {
            /// <summary>
            /// Shows a maximal bracketed version of the expression
            /// </summary>
            /// <returns>The expression</returns>
            public string Value { get { return expression.Show(); } }
            /// <summary>
            /// The expression
            /// </summary>
            Arithmetic.Expression expression;
            /// <summary>
            /// To retrieve the value of the expression, given this value of K
            /// </summary>
            /// <param name="k">The value of K</param>
            /// <returns>The value of the expression</returns>
            public int GetValue(int k)
            {
                return expression.Solve(k);
            }
            /// <summary>
            /// To generate a KArithmetic, parses the given string immediately to be sure to be error free if it succeeds.
            /// </summary>
            /// <param name="value">The expression</param>
            public KArithmetic(string value)
            {
                expression = Parse(value);
            }
            /// <summary>
            /// To contain Arithmetic stuffs
            /// </summary>
            class Arithmetic
            {
                /// <summary>
                /// A general class for expressions
                /// </summary>
                public abstract class Expression
                {
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public abstract int Solve(int k);
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public abstract string Show();
                }
                /// <summary>
                /// An expression with an operator
                /// </summary>
                public class Operator : Expression
                {
                    /// <summary>
                    /// The operator
                    /// </summary>
                    OpType type;
                    /// <summary>
                    /// The left hand side
                    /// </summary>
                    Expression left;
                    /// <summary>
                    /// The right hand side
                    /// </summary>
                    Expression right;
                    /// <summary>
                    /// Creates an operator expression
                    /// </summary>
                    /// <param name="type_input">The operator</param>
                    /// <param name="lhs">The left hand side</param>
                    /// <param name="rhs">The right hand side</param>
                    public Operator(OpType type_input, Expression lhs, Expression rhs)
                    {
                        type = type_input;
                        left = lhs;
                        right = rhs;
                    }
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        switch (type)
                        {
                            case OpType.Minus:
                                return left.Solve(k) - right.Solve(k);
                            case OpType.Add:
                                return left.Solve(k) + right.Solve(k);
                            case OpType.Times:
                                return left.Solve(k) * right.Solve(k);
                            case OpType.Divide:
                                return left.Solve(k) / right.Solve(k);
                            default:
                                throw new ParseException($"An unkown operator type is signalled while solving the calculation {type.ToString()}");
                        }
                    }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        string op = "";
                        switch (type)
                        {
                            case OpType.Minus:
                                op = "-";
                                break;
                            case OpType.Add:
                                op = "+";
                                break;
                            case OpType.Times:
                                op = "*";
                                break;
                            case OpType.Divide:
                                op = "/";
                                break;
                            default:
                                throw new ParseException($"An unkown operator type is signalled while solving the calculation {type.ToString()}");
                        }
                        return "(" + left.Show() + op + right.Show() + ")";
                    }
                }
                /// <summary>
                /// The possible operators
                /// </summary>
                public enum OpType
                {
                    /// <summary> - </summary>
                    Minus,
                    /// <summary> + </summary>
                    Add,
                    /// <summary> * </summary>
                    Times,
                    /// <summary> / </summary>
                    Divide
                }
                /// <summary>
                /// An expression containing only the variable K
                /// </summary>
                public class K : Expression
                {
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        return k;
                    }
                    /// <summary>
                    /// Creates a new instance
                    /// </summary>
                    public K() { }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        return "K";
                    }
                }
                /// <summary>
                /// An expression containing a constant
                /// </summary>
                public class Constant : Expression
                {
                    /// <summary>
                    /// The constant
                    /// </summary>
                    public int Value;
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        return Value;
                    }
                    /// <summary>
                    /// Creates a new instance
                    /// </summary>
                    /// <param name="value">The value of the constant</param>
                    public Constant(int value)
                    {
                        Value = value;
                    }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        return Value.ToString();
                    }
                }
            }
            /// <summary>
            /// Parses a string into an expression
            /// </summary>
            /// <param name="input">The string to parse</param>
            /// <returns>The expression (if successfull)</returns>
            Arithmetic.Expression Parse(string input)
            {
                input = input.Trim();
                // Scan for low level operators
                if (input.Contains('+'))
                {
                    int pos = input.IndexOf('+');
                    return new Arithmetic.Operator(Arithmetic.OpType.Add, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                if (input.Contains('-'))
                {
                    int pos = input.IndexOf('-');
                    return new Arithmetic.Operator(Arithmetic.OpType.Minus, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                // Scan for high level operators
                if (input.Contains('*'))
                {
                    int pos = input.IndexOf('*');
                    return new Arithmetic.Operator(Arithmetic.OpType.Times, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                if (input.Contains('/'))
                {
                    int pos = input.IndexOf('/');
                    return new Arithmetic.Operator(Arithmetic.OpType.Divide, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                // Scan for constants and K's
                if (input.ToLower() == "k")
                {
                    return new Arithmetic.K();
                }
                try
                {
                    return new Arithmetic.Constant(Convert.ToInt32(input));
                }
                catch
                {
                    throw new ParseException($"The following could not be parsed into a calculation based on K: {input}");
                }
            }
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
            /// The alphabet to be used for template matching
            /// </summary>
            public AlphabetValue Alphabet;
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
        /// <summary>
        /// All parameters for a single run
        /// </summary>
        public class SingleRun
        {
            /// <summary>
            /// The unique numeric ID of this run
            /// </summary>
            public int ID;
            /// <summary>
            /// THe name of this run
            /// </summary>
            public string Runname;
            /// <summary>
            /// The input data for this run. A runtype of 'Separate' will result in only one input data in this list.
            /// </summary>
            public List<Input.Parameter> Input;
            /// <summary>
            /// The value of K used in this run
            /// </summary>
            public int K;
            /// <summary>
            /// The value of MinimalHomology used in this run
            /// </summary>
            public int MinimalHomology;
            /// <summary>
            /// The value of DuplicateThreshold used in this run
            /// </summary>
            public int DuplicateThreshold;
            /// <summary>
            /// The value of Reverse used in this run
            /// </summary>
            public bool Reverse;
            /// <summary>
            /// The alphabet used in this run
            /// </summary>
            public AlphabetValue Alphabet;
            /// <summary>
            /// The template(s) used in this run
            /// </summary>
            public List<TemplateValue> Template;
            /// <summary>
            /// The reports to be generated
            /// </summary>
            public List<Report.Parameter> Report;
            /// <summary>
            /// To create a single run with a single dataparameter as input
            /// </summary>
            /// <param name="id">The ID of the run</param>
            /// <param name="runname">The name of the run</param>
            /// <param name="input">The input data to be run</param>
            /// <param name="k">The value of K</param>
            /// <param name="duplicateThreshold">The value of DuplicateThreshold</param>
            /// <param name="minimalHomology">The value of MinimalHomology</param>
            /// <param name="reverse">The value of Reverse</param>
            /// <param name="alphabet">The alphabet to be used</param>
            /// <param name="template">The templates to be used</param>
            /// <param name="report">The report(s) to be generated</param>
            public SingleRun(int id, string runname, Input.Parameter input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<TemplateValue> template, List<Report.Parameter> report)
            {
                ID = id;
                Runname = runname;
                Input = new List<Input.Parameter> { input };
                K = k;
                DuplicateThreshold = duplicateThreshold;
                MinimalHomology = minimalHomology;
                Reverse = reverse;
                Alphabet = alphabet;
                Template = template;
                Report = report;
            }
            /// <summary>
            /// To create a single run with a multiple dataparameters as input
            /// </summary>
            /// <param name="id">The ID of the run</param>
            /// <param name="runname">The name of the run</param>
            /// <param name="input">The input data to be run</param>
            /// <param name="k">The value of K</param>
            /// <param name="duplicateThreshold">The value of DuplicateThreshold</param>
            /// <param name="minimalHomology">The value of MinimalHomology</param>
            /// <param name="reverse">The value of Reverse</param>
            /// <param name="alphabet">The alphabet to be used</param>
            /// <param name="template">The templates to be used</param>
            /// <param name="report">The report(s) to be generated</param>
            public SingleRun(int id, string runname, List<Input.Parameter> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<TemplateValue> template, List<Report.Parameter> report)
            {
                ID = id;
                Runname = runname;
                Input = input;
                K = k;
                DuplicateThreshold = duplicateThreshold;
                MinimalHomology = minimalHomology;
                Reverse = reverse;
                Alphabet = alphabet;
                Template = template;
                Report = report;
            }
            /// <summary>
            /// To display the main parameters of this run in a string, mainly for error tracking and debugging purposes.
            /// </summary>
            /// <returns>The main parameters</returns>
            public string Display()
            {
                return $"\tRunname\t\t: {Runname}\n\tInput\t\t:{Input.Aggregate("", (a,b) => a + " " + b.File.Name)}\n\tK\t\t: {K}\n\tMinimalHomology\t: {MinimalHomology}\n\tReverse\t\t: {Reverse.ToString()}\n\tAlphabet\t: {Alphabet.Name}\n\tTemplate\t: {Template.Aggregate("", (a,b) => a + " " + b.Name)}";
            }
            /// <summary>
            /// Runs this run.abstract Runs the assembly, and generates the reports.
            /// </summary>
            public void Calculate()
            {
                try
                {
                    var alphabet = new Alphabet(Alphabet.Data, AssemblyNameSpace.Alphabet.AlphabetParamType.Data);
                    var assm = new Assembler(K, DuplicateThreshold, MinimalHomology, Reverse, alphabet);

                    // Retrieve the input
                    foreach (var input in Input)
                    {
                        switch (input)
                        {
                            case Input.Peaks p:
                                assm.GiveReads(OpenReads.Peaks(p.File, p.Cutoffscore, p.LocalCutoffscore, p.FileFormat, p.MinLengthPatch, p.Separator, p.DecimalSeparator));
                                break;
                            case Input.Reads r:
                                assm.GiveReads(OpenReads.Simple(r.File));
                                break;
                            case Input.FASTA f:
                                assm.GiveReads(OpenReads.Fasta(f.File));
                                break;
                        }
                    }
                    
                    assm.Assemble();
                    var databases = new List<TemplateDatabase>();
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    foreach (var template in Template) {
                        Console.WriteLine($"Working on Template {template.Name}");
                        var alph = template.Alphabet == null ? new Alphabet(template.Alphabet.Data, AssemblyNameSpace.Alphabet.AlphabetParamType.Data) : alphabet;
                        var database = new TemplateDatabase(template.Path, template.Name, alph);
                        database.Match(assm.condensed_graph);
                        databases.Add(database);
                    }

                    stopWatch.Stop();
                    assm.meta_data.template_matching_time = stopWatch.ElapsedMilliseconds;

                    ReportInputParameters parameters = new ReportInputParameters(assm, this, databases);

                    // Generate the report(s)
                    foreach (var report in Report)
                    {
                        switch (report)
                        {
                            case Report.HTML h:
                                var htmlreport = new HTMLReport(parameters, h.UseIncludedDotDistribution);
                                htmlreport.Save(h.CreateName(this));
                                break;
                            case Report.CSV c:
                                var csvreport = new CSVReport(parameters);
                                csvreport.CreateCSVLine(c.GetID(this), c.Path);
                                break;
                            case Report.FASTA f:
                                var fastareport = new FASTAReport(parameters, f.MinimalScore);
                                fastareport.Save(f.CreateName(this));
                                break;
                        }
                    }

                    Console.WriteLine($"Finished run: {ID}");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + e.Message);
                    Console.ResetColor();
                    Console.WriteLine("STACKTRACE: " + e.StackTrace);
                    Console.WriteLine("RUNPARAMETERS:\n" + Display());
                }
            }
        }
    }
}