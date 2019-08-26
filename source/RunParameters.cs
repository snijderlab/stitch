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
    /// <summary>
    /// To give an 'api' for calling the program
    /// </summary>
    public class RunParameters
    {
        public string Runname;
        public RuntypeValue Runtype;
        public List<DataParameter> Input;
        public KValue K;
        public ReverseValue Reverse;
        public List<KArithmatic> MinimalHomology;
        public List<KArithmatic> DuplicateThreshold;
        public List<AlphabetValue> Alphabet;
        public List<ReportParameter> Report;
        public RunParameters()
        {
            Runname = "";
            Input = new List<DataParameter>();
            Reverse = ReverseValue.True;
            MinimalHomology = new List<KArithmatic>();
            DuplicateThreshold = new List<KArithmatic>();
            Alphabet = new List<AlphabetValue>();
            Report = new List<ReportParameter>();
        }
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
                case Single s:
                    klist.Add(s.Value);
                    break;
                case Multiple m:
                    klist.AddRange(m.Values);
                    break;
                case Range r:
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
                                    output.Add(new SingleRun(id, Runname, Input, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Report));
                                }
                                else
                                {
                                    foreach (var input in Input)
                                    {
                                        id++;
                                        output.Add(new SingleRun(id, Runname, input, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Report));
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
    public enum RuntypeValue
    {
        Separate,
        Group
    }
    public abstract class DataParameter
    {
        public string Name;
    }
    public class Peaks : DataParameter
    {
        public string Path;
        public int Cutoffscore;
        public int LocalCutoffscore;
        public FileFormat.Peaks FileFormat;
        public int MinLengthPatch;
        public char Separator;
        public char DecimalSeparator;
        public Peaks()
        {
            Cutoffscore = 99;
            LocalCutoffscore = 90;
            FileFormat = AssemblyNameSpace.FileFormat.Peaks.NewFormat();
            MinLengthPatch = 3;
            Separator = ',';
            DecimalSeparator = '.';
        }
    }
    public class Reads : DataParameter
    {
        public string Path;
        public Reads() { }
    }
    public abstract class KValue { }
    public class Single : KValue
    {
        public int Value;
        public Single(int value)
        {
            Value = value;
        }
    }
    public class Multiple : KValue
    {
        public int[] Values;
        public Multiple(int[] values)
        {
            Values = values;
        }
    }
    public class Range : KValue
    {
        public int Start;
        public int End;
        public int Step;
        public Range()
        {
            Step = 1;
        }
    }
    public enum ReverseValue
    {
        True,
        False,
        Both
    }
    public abstract class KArithmatic
    {
        public abstract int GetValue(int k);
    }
    public class Simple : KArithmatic
    {
        public int Value;
        public override int GetValue(int k)
        {
            return Value;
        }
        public Simple(int value)
        {
            Value = value;
        }
    }
    public class Calculation : KArithmatic
    {
        public string Value;
        public override int GetValue(int k)
        {
            var expression = Value.ToLower();
            if (expression[0] == 'K' && expression[1] == '-')
            {
                return k - Convert.ToInt32(expression.Remove(0, 2).Trim());
            }
            if (expression[0] == 'K' && expression[1] == '+')
            {
                return k + Convert.ToInt32(expression.Remove(0, 2).Trim());
            }
            throw new Exception("Calculation not supported yet");
            //return 0; // Have to insert logic to calculate value
        }
        public Calculation(string value)
        {
            Value = value;
        }
    }
    public class AlphabetValue
    {
        public string Data; //Prelookup paths to get the data
        public string Name;
    }
    public abstract class ReportParameter
    {
        public string Path;
        public string CreateName(SingleRun r)
        {
            var output = new StringBuilder(Path);

            output.Replace("{id}", r.ID.ToString());
            output.Replace("{k}", r.K.ToString());
            output.Replace("{mh}", r.MinimalHomology.ToString());
            output.Replace("{dt}", r.DuplicateThreshold.ToString());
            output.Replace("{alph}", r.Alphabet.Name);
            output.Replace("{data}", r.Input.Count() == 1 ? r.Input[0].Name : "Group");
            output.Replace("{name}", r.Runname);
            output.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            output.Replace("{time}", DateTime.Now.ToString("hh-mm-ss"));
            output.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd@hh-mm-ss"));

            return output.ToString();
        }
    }
    public class HTML : ReportParameter { }
    public class CSV : ReportParameter
    {
        public string GetID(SingleRun r)
        {
            throw new Exception("Creating ID's for CSV not supported yet");
        }
    }
    public class FASTA : ReportParameter {
        public int MinimalScore = 0;
     }
    public class SingleRun
    {
        public int ID;
        public string Runname;
        public List<DataParameter> Input;
        public int K;
        public int MinimalHomology;
        public int DuplicateThreshold;
        public bool Reverse;
        public AlphabetValue Alphabet;
        public List<ReportParameter> Report;
        public SingleRun(int id, string runname, DataParameter input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<ReportParameter> report)
        {
            ID = id;
            Runname = runname;
            Input = new List<DataParameter> { input };
            K = k;
            DuplicateThreshold = duplicateThreshold;
            MinimalHomology = minimalHomology;
            Reverse = reverse;
            Alphabet = alphabet;
            Report = report;
        }
        public SingleRun(int id, string runname, List<DataParameter> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<ReportParameter> report)
        {
            ID = id;
            Runname = runname;
            Input = input;
            K = k;
            DuplicateThreshold = duplicateThreshold;
            MinimalHomology = minimalHomology;
            Reverse = reverse;
            Alphabet = alphabet;
            Report = report;
        }
        public string Display()
        {
            return $@"  Runname     : {Runname}
    Input       : {Input.ToString()}
    K           : {K}
    MinimalHomology    : {MinimalHomology}
    Reverse     : {Reverse.ToString()}
    Alphabet    : {Alphabet.ToString()}";
        }
        public void Calculate()
        {
            try
            {
                var assm = new Assembler(K, DuplicateThreshold, MinimalHomology, Reverse);
                AssemblyNameSpace.Alphabet.SetAlphabetData(Alphabet.Data);
                foreach (var input in Input)
                {
                    switch (input)
                    {
                        case Peaks p:
                            assm.GiveReadsPeaks(OpenReads.Peaks(p.Path, p.Cutoffscore, p.LocalCutoffscore, p.FileFormat, p.MinLengthPatch, p.Name, p.Separator, p.DecimalSeparator));
                            break;
                        case Reads r:
                            assm.GiveReads(OpenReads.Simple(r.Path));
                            break;
                    }
                }
                assm.Assemble();
                foreach (var report in Report)
                {
                    switch (report)
                    {
                        case HTML h:
                            var htmlreport = new HTMLReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads);
                            htmlreport.Save(h.CreateName(this));
                            break;
                        case CSV c:
                            var csvreport = new CSVReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads);
                            csvreport.CreateCSVLine(c.GetID(this), c.Path);
                            break;
                        case FASTA f:
                            var fastareport = new FASTAReport(assm.condensed_graph, assm.graph, assm.meta_data, assm.reads, assm.peaks_reads, f.MinimalScore);
                            fastareport.Save(f.CreateName(this));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Console.WriteLine("STACKTRACE: " + e.StackTrace + "\nRUNPARAMETERS:\n" + Display());
            }
        }
    }
}