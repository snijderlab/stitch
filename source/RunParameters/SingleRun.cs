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
                return $"\tRunname\t\t: {Runname}\n\tInput\t\t:{Input.Aggregate("", (a, b) => a + " " + b.File.Name)}\n\tK\t\t: {K}\n\tMinimalHomology\t: {MinimalHomology}\n\tReverse\t\t: {Reverse.ToString()}\n\tAlphabet\t: {Alphabet.Name}\n\tTemplate\t: {Template.Aggregate("", (a, b) => a + " " + b.Name)}";
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

                    foreach (var template in Template)
                    {
                        var alph = template.Alphabet != null ? new Alphabet(template.Alphabet.Data, AssemblyNameSpace.Alphabet.AlphabetParamType.Data) : alphabet;
                        Console.WriteLine($"Working on Template {template.Name}");
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