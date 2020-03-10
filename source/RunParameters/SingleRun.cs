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
        /// All parameters for a single run.
        /// </summary>
        public class SingleRun
        {
            /// <summary>
            /// The unique numeric ID of this run.
            /// </summary>
            public int ID;

            /// <summary>
            /// THe name of this run.
            /// </summary>
            public string Runname;

            /// <summary>
            /// The input data for this run. A runtype of 'Separate' will result in only one input data in this list.
            /// </summary>
            public List<List<(string, MetaData.IMetaData)>> Input;

            /// <summary>
            /// The value of K used in this run.
            /// </summary>
            public int K;

            /// <summary>
            /// The value of MinimalHomology used in this run.
            /// </summary>
            public int MinimalHomology;

            /// <summary>
            /// The value of DuplicateThreshold used in this run.
            /// </summary>
            public int DuplicateThreshold;

            /// <summary>
            /// The value of Reverse used in this run.
            /// </summary>
            public bool Reverse;

            /// <summary>
            /// The alphabet used in this run.
            /// </summary>
            public AlphabetValue Alphabet;

            /// <summary>
            /// The template(s) used in this run.
            /// </summary>
            public List<DatabaseValue> Template;
            public RecombineValue Recombine;

            /// <summary>
            /// The reports to be generated.
            /// </summary>
            public List<Report.Parameter> Report;
            readonly ProgressBar progressBar;

            /// <summary>
            /// To create a single run with a single dataparameter as input.
            /// </summary>
            /// <param name="id">The ID of the run.</param>
            /// <param name="runname">The name of the run.</param>
            /// <param name="input">The input data to be run.</param>
            /// <param name="k">The value of K.</param>
            /// <param name="duplicateThreshold">The value of DuplicateThreshold.</param>
            /// <param name="minimalHomology">The value of MinimalHomology.</param>
            /// <param name="reverse">The value of Reverse.</param>
            /// <param name="alphabet">The alphabet to be used.</param>
            /// <param name="template">The templates to be used.</param>
            /// <param name="recombine">The recombination, if needed.</param>
            /// <param name="report">The report(s) to be generated.</param>
            public SingleRun(int id, string runname, List<(string, MetaData.IMetaData)> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<DatabaseValue> template, RecombineValue recombine, List<Report.Parameter> report, ProgressBar bar = null)
            {
                ID = id;
                Runname = runname;
                Input = new List<List<(string, MetaData.IMetaData)>> { input };
                K = k;
                DuplicateThreshold = duplicateThreshold;
                MinimalHomology = minimalHomology;
                Reverse = reverse;
                Alphabet = alphabet;
                Template = template;
                Recombine = recombine;
                Report = report;
                progressBar = bar;
            }

            /// <summary>
            /// To create a single run with a multiple dataparameters as input.
            /// </summary>
            /// <param name="id">The ID of the run.</param>
            /// <param name="runname">The name of the run.</param>
            /// <param name="input">The input data to be run.</param>
            /// <param name="k">The value of K.</param>
            /// <param name="duplicateThreshold">The value of DuplicateThreshold.</param>
            /// <param name="minimalHomology">The value of MinimalHomology.</param>
            /// <param name="reverse">The value of Reverse.</param>
            /// <param name="alphabet">The alphabet to be used.</param>
            /// <param name="template">The templates to be used.</param>
            /// <param name="recombine">The recombination, if needed.</param>
            /// <param name="report">The report(s) to be generated.</param>
            public SingleRun(int id, string runname, List<List<(string, MetaData.IMetaData)>> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetValue alphabet, List<DatabaseValue> template, RecombineValue recombine, List<Report.Parameter> report, ProgressBar bar = null)
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
                progressBar = bar;
            }

            /// <summary>
            /// To display the main parameters of this run in a string, mainly for error tracking and debugging purposes.
            /// </summary>
            /// <returns>The main parameters.</returns>
            public string Display()
            {
                return $"\tRunname\t\t: {Runname}\n\tK\t\t: {K}\n\tMinimalHomology\t: {MinimalHomology}\n\tReverse\t\t: {Reverse}\n\tAlphabet\t: {Alphabet.Alphabet}\n\tTemplate\t: {Template.Aggregate("", (a, b) => a + " " + b.Name)}";
            }

            /// <summary>
            /// Runs this run. Runs the assembly, and generates the reports.
            /// </summary>
            public void Calculate(int max_threads = 1)
            {
                try
                {
                    var alphabet = new Alphabet(Alphabet);
                    var assm = new Assembler(K, DuplicateThreshold, MinimalHomology, Reverse, alphabet);

                    // Retrieve the input
                    foreach (var input in Input)
                    {
                        assm.GiveReads(input);
                    }

                    assm.Assemble();

                    // Did assembly
                    if (progressBar != null) progressBar.Update();

                    // Templates
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var databases = new List<TemplateDatabase>();
                    for (int i = 0; i < Template.Count(); i++)
                    {
                        var template = Template[i];
                        var alph = new Alphabet(template.Alphabet);

                        var database1 = new TemplateDatabase(template.Templates, alph, template.Name, template.CutoffScore, i);
                        database1.Match(assm.GetAllPathsMultipleReads(), max_threads);

                        databases.Add(database1);
                    }

                    stopWatch.Stop();
                    assm.meta_data.template_matching_time = stopWatch.ElapsedMilliseconds;

                    ReportInputParameters parameters;

                    // Recombine
                    if (Recombine != null)
                    {
                        Stopwatch recombine_sw = new Stopwatch();
                        recombine_sw.Start();

                        var rec_databases = new List<TemplateDatabase>();
                        var alph = Recombine.Alphabet != null ? new Alphabet(Recombine.Alphabet) : alphabet;

                        for (int i = 0; i < Recombine.Databases.Count(); i++)
                        {
                            var template = Recombine.Databases[i];

                            var database1 = new TemplateDatabase(template.Templates, alph, template.Name, Recombine.CutoffScore, i);
                            database1.Match(assm.GetAllPathsMultipleReads(), max_threads);

                            rec_databases.Add(database1);
                        }

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

                        // Recombine high scoring templates
                        // https://rosettacode.org/wiki/Cartesian_product_of_two_or_more_lists#C.23
                        IEnumerable<IEnumerable<Template>> empty = new[] { Enumerable.Empty<Template>() };

                        var combinations = top.Aggregate(
                            empty,
                            (accumulator, sequence) =>
                            from acc in accumulator
                            from item in sequence
                            select acc.Concat(new[] { item }));

                        var recombined_templates = new List<Template>();
                        for (int i = 0; i < combinations.Count(); i++)
                        {
                            var sequence = combinations.ElementAt(i);
                            var s = new List<AminoAcid>();
                            var t = new List<Template>();
                            foreach (var element in Recombine.Order)
                            {
                                if (element.GetType() == typeof(RecombineOrder.Gap))
                                {
                                    s.Add(new AminoAcid(alph, '*'));
                                }
                                else
                                {
                                    s.AddRange(sequence.ElementAt(((RecombineOrder.Template)element).Index).Sequence);
                                    t.Add(sequence.ElementAt(((RecombineOrder.Template)element).Index));
                                }
                            }
                            recombined_templates.Add(new Template("recombined", s.ToArray(), new MetaData.None(new MetaData.FileIdentifier("nowhere", "")), alph, Recombine.CutoffScore, new RecombinedTemplateLocation(i), t));
                        }

                        var recombined_database = new TemplateDatabase(recombined_templates, alph, "Recombined Database", Recombine.CutoffScore);

                        recombined_database.Match(assm.GetAllPathsMultipleReads(), max_threads);

                        recombine_sw.Stop();

                        parameters = new ReportInputParameters(assm, this, databases, recombined_database, rec_databases);
                    }
                    else
                    {
                        parameters = new ReportInputParameters(assm, this, databases);
                    }

                    // Did recombination + databases
                    if (progressBar != null) progressBar.Update();

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
                                var fastareport = new FASTAReport(parameters, f.MinimalScore, f.OutputType);
                                fastareport.Save(f.CreateName(this));
                                break;
                        }
                    }

                    // Did reports
                    if (progressBar != null) progressBar.Update();
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
