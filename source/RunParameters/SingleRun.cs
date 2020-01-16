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
            public RecombineValue Recombine;
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
            /// <param name="recombine">The recombination, if needed</param>
            /// <param name="report">The report(s) to be generated</param>
            public SingleRun(int id, string runname, Input.Parameter input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<TemplateValue> template, RecombineValue recombine, List<Report.Parameter> report)
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
                Recombine = recombine;
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
            /// <param name="recombine">The recombination, if needed</param>
            /// <param name="report">The report(s) to be generated</param>
            public SingleRun(int id, string runname, List<Input.Parameter> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<TemplateValue> template, RecombineValue recombine, List<Report.Parameter> report)
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
                Recombine = recombine;
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
            /// Runs this run. Runs the assembly, and generates the reports.
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

                    // Templates
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var databases = new List<TemplateDatabase>();
                    foreach (var template in Template)
                    {
                        var alph = template.Alphabet != null ? new Alphabet(template.Alphabet.Data, AssemblyNameSpace.Alphabet.AlphabetParamType.Data) : alphabet;
                        Console.WriteLine($"Working on Template {template.Name}");

                        var database1 = new TemplateDatabase(template.Path, template.Type, template.Name, alph);
                        database1.MatchParallel(assm.GetAllPathSequences());

                        databases.Add(database1);
                    }

                    stopWatch.Stop();
                    assm.meta_data.template_matching_time = stopWatch.ElapsedMilliseconds;

                    // Recombine
                    if (Recombine != null)
                    {
                        Stopwatch recombine_sw = new Stopwatch();
                        recombine_sw.Start();

                        Console.WriteLine("Working on recombination");
                        var rec_databases = new List<TemplateDatabase>();
                        var alph = Recombine.Alphabet != null ? new Alphabet(Recombine.Alphabet.Data, AssemblyNameSpace.Alphabet.AlphabetParamType.Data) : alphabet;

                        foreach (var template in Recombine.Templates)
                        {
                            Console.WriteLine($"Working on Template {template.Name}");

                            var database1 = new TemplateDatabase(template.Path, template.Type, template.Name, alph);
                            database1.MatchParallel(assm.GetAllPathSequences());

                            rec_databases.Add(database1);
                        }

                        Console.WriteLine("Finished first round of template matching");

                        // Create a list for every database with the top n highest scoring templates. 
                        // By having the lowest of these always at the first position at shuffling in 
                        // a new high scoring template to its right position this snippet is still 
                        // approximately O(n) in respect to the database. Worst case O(top_n * l_database)
                        var top = new List<List<Template>>();
                        foreach (var db in rec_databases)
                        {
                            var templates = new LinkedList<Template>();
                            var first = true;
                            foreach (var temp in db.Templates)
                            {
                                if (first)
                                {
                                    templates.AddFirst(temp);
                                    first = false;
                                    continue;
                                }

                                if (temp.Score > templates.First().Score)
                                {
                                    bool found = false;
                                    var current = templates.First();

                                    for (var it = templates.First; it != null; it = it.Next)
                                    {
                                        if (temp.Score < it.Value.Score)
                                        {
                                            templates.AddBefore(it, temp);
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found)
                                    {
                                        templates.AddLast(temp);
                                    }

                                    if (templates.Count() > Recombine.N) templates.RemoveFirst();
                                }

                            }
                            top.Add(templates.ToList());
                        }

                        Console.WriteLine("Found all top <n> templates");

                        // Recombine high scoring templates
                        // https://rosettacode.org/wiki/Cartesian_product_of_two_or_more_lists#C.23
                        IEnumerable<IEnumerable<Template>> empty = new[] { Enumerable.Empty<Template>() };

                        var combinations = top.Aggregate(
                            empty,
                            (accumulator, sequence) =>
                            from acc in accumulator
                            from item in sequence
                            select acc.Concat(new[] { item }));

                        Console.WriteLine($"Found {combinations.Count()} combinations, with {combinations.First().Count()} elements in the first one");

                        var recombined_templates = new List<Template>();
                        foreach (var sequence in combinations)
                        {
                            var s = new List<AminoAcid>();
                            foreach (var element in Recombine.Order)
                            {
                                if (element.GetType() == typeof(RecombineOrder.Gap))
                                {
                                    s.Add(new AminoAcid(alph, '*'));
                                }
                                else
                                {
                                    s.AddRange(sequence.ElementAt(((RecombineOrder.Template)element).Index).Sequence);
                                }
                            }
                            recombined_templates.Add(new Template(s.ToArray(), new MetaData.None(new MetaData.FileIdentifier("nowhere", ""))));
                        }

                        var recombined_database = new TemplateDatabase(recombined_templates, alph);

                        Console.WriteLine("Created templates for second round");

                        recombined_database.MatchParallel(assm.GetAllPathSequences());

                        Console.WriteLine("Finished second round of template matching");

                        recombine_sw.Stop();
                        Console.WriteLine($"Finished Recombination {recombine_sw.ElapsedMilliseconds} ms");
                    }

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