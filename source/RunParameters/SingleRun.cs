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
            /// THe name of this run.
            /// </summary>
            public string Runname;

            /// <summary>
            /// The input data for this run. A runtype of 'Separate' will result in only one input data in this list.
            /// </summary>
            public List<(string, MetaData.IMetaData)> Input;

            /// <summary>
            /// The alphabet used in this run.
            /// </summary>
            public AlphabetParameter Alphabet;

            /// <summary>
            /// The template(s) used in this run.
            /// </summary>
            public TemplateMatchingParameter TemplateMatching;
            public RecombineParameter Recombine;

            /// <summary>
            /// The reports to be generated.
            /// </summary>
            public List<Report.Parameter> Report;
            readonly ProgressBar progressBar;
            public readonly ParsedFile BatchFile;

            /// <summary>
            /// To create a single run with a single dataparameter as input. Without assembly
            /// </summary>
            /// <param name="id">The ID of the run.</param>
            /// <param name="runname">The name of the run.</param>
            /// <param name="input">The input data to be run.</param>
            /// <param name="template">The templates to be used.</param>
            /// <param name="recombine">The recombination, if needed.</param>
            /// <param name="report">The report(s) to be generated.</param>
            public SingleRun(string runname, List<(string, MetaData.IMetaData)> input, TemplateMatchingParameter templateMatching, RecombineParameter recombine, ReportParameter report, ParsedFile batchfile, ProgressBar bar = null)
            {
                Runname = runname;
                Input = input;
                TemplateMatching = templateMatching;
                Recombine = recombine;
                Report = report.Files;
                BatchFile = batchfile;
                progressBar = bar;
            }

            /// <summary>
            /// To display the main parameters of this run in a string, mainly for error tracking and debugging purposes.
            /// </summary>
            /// <returns>The main parameters.</returns>
            public string Display()
            {
                return $"\tRunname\t\t: {Runname}";
            }

            /// <summary>
            /// Runs this run. Runs the assembly, and generates the reports.
            /// </summary>
            public void Calculate(int max_threads = 1)
            {
                try
                {
                    // Template Matching
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var databases = new List<(string, List<TemplateDatabase>)>();
                    for (int i = 0; i < TemplateMatching.Databases.Count(); i++)
                    {
                        var current_group = new List<TemplateDatabase>();
                        for (int j = 0; j < TemplateMatching.Databases[i].Databases.Count(); j++)
                        {
                            var database = TemplateMatching.Databases[i].Databases[j];
                            var alph = new Alphabet(database.Alphabet ?? TemplateMatching.Alphabet);

                            var database1 = new TemplateDatabase(database.Templates, alph, database.Name, database.CutoffScore == 0 ? TemplateMatching.CutoffScore : database.CutoffScore, j, database.Scoring, database.ClassChars);

                            database1.Match(Input, max_threads, HelperFunctionality.EvaluateTrilean(database.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));

                            current_group.Add(database1);
                        }
                        databases.Add((TemplateMatching.Databases[i].Name, current_group));
                    }

                    stopWatch.Stop();

                    List<TemplateDatabase> recombined_database = new List<TemplateDatabase>();
                    List<TemplateDatabase> read_templates = new List<TemplateDatabase>();

                    // Recombine
                    if (Recombine != null)
                    {
                        Stopwatch recombine_sw = new Stopwatch();
                        recombine_sw.Start();

                        if (Recombine.Alphabet == null && TemplateMatching.Alphabet == null)
                        {
                            new InputNameSpace.ErrorMessage("No alphabet defined", "Please make define an alphabet for recombination if no alphabet for template matching is defined takes place", "").Print();
                            return;
                        }
                        var alph = Recombine.Alphabet != null ? new Alphabet(Recombine.Alphabet) : new Alphabet(TemplateMatching.Alphabet);

                        for (int database_group_index = 0; database_group_index < databases.Count(); database_group_index++)
                        {
                            var database_group = databases[database_group_index];
                            // Create a list for every database with the top n highest scoring templates.
                            // By having the lowest of these always at the first position at shuffling in
                            // a new high scoring template to its right position this snippet is still
                            // approximately O(n) in respect to the database. Worst case O(top_n * l_database)
                            var top = new List<List<Template>>();
                            foreach (var db in database_group.Item2)
                            {
                                db.Templates.Sort((a, b) => b.Score.CompareTo(a.Score));
                                top.Add(db.Templates.Take(Recombine.N).ToList());
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

                            var recombined_database_group = new TemplateDatabase(new List<Template>(), alph, "Recombined Database", Recombine.CutoffScore);
                            var recombined_templates = new List<Template>();
                            var namefilter = new NameFilter();

                            for (int i = 0; i < combinations.Count(); i++)
                            {
                                var sequence = combinations.ElementAt(i);
                                var s = new List<AminoAcid>();
                                var t = new List<Template>();
                                var join = false;
                                foreach (var element in Recombine.Order[database_group_index])
                                {
                                    if (element.GetType() == typeof(RecombineOrder.Gap))
                                    {
                                        // When the templates are aligned with a gap (a * in the Order definition) the overlap between the two templates is found 
                                        // and removed from the Template sequence for the recombine round.
                                        join = true;
                                    }
                                    else
                                    {
                                        var seq = sequence.ElementAt(((RecombineOrder.Template)element).Index).ConsensusSequence().Item1;
                                        if (join)
                                        {
                                            // When the templates are aligned with a gap (a * in the Order definition) the overlap between the two templates is found 
                                            // and removed from the Template sequence for the recombine round.
                                            join = false;
                                            s = s.TakeWhile(a => a.Char != 'X').ToList();
                                            seq = seq.SkipWhile(a => a.Char == 'X').ToList();
                                            var aligned_template = HelperFunctionality.EndAlignment(s.ToArray(), seq.ToArray(), recombined_database_group.Alphabet, 20);
                                            // When no good overlap is found just paste them one after the other
                                            if (aligned_template.Item2 >= 0)
                                                s.AddRange(seq.Skip(aligned_template.Item1));
                                            else
                                                s.AddRange(seq);
                                        }
                                        else
                                        {
                                            s.AddRange(seq);
                                        }
                                        t.Add(sequence.ElementAt(((RecombineOrder.Template)element).Index));
                                    }
                                }
                                recombined_templates.Add(new Template("recombined", s.ToArray(), new MetaData.Simple(new MetaData.FileIdentifier(), namefilter, "REC"), recombined_database_group, new RecombinedTemplateLocation(i), t));
                            }
                            foreach (var read in recombined_templates) read.MetaData.FinaliseIdentifier();

                            recombined_database_group.Templates = recombined_templates;

                            var force = HelperFunctionality.EvaluateTrilean(Recombine.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate);
                            recombined_database_group.Match(Input, max_threads, force);

                            recombined_database.Add(recombined_database_group);

                            recombine_sw.Stop();

                            // Did recombination + databases
                            if (progressBar != null) progressBar.Update();

                            if (Recombine.ReadAlignment != null)
                            {
                                List<(string, MetaData.IMetaData)> templates = new List<(string, MetaData.IMetaData)>(recombined_database_group.Templates.Count());

                                Parallel.ForEach(
                                    recombined_database_group.Templates,
                                    new ParallelOptions { MaxDegreeOfParallelism = max_threads },
                                    (s, _) => templates.Add((
                                        AminoAcid.ArrayToString(s.ConsensusSequence().Item1),
                                        (MetaData.IMetaData)new MetaData.Simple(new MetaData.FileIdentifier(), namefilter, "RT")))
                                );

                                Parallel.ForEach(
                                    templates,
                                    new ParallelOptions { MaxDegreeOfParallelism = max_threads },
                                    (s, _) => s.Item2.FinaliseIdentifier()
                                );

                                var read_templates_group = new TemplateDatabase(templates, new Alphabet(Recombine.ReadAlignment.Alphabet), "ReadAlignDatabase", Recombine.ReadAlignment.CutoffScore, 0);

                                read_templates_group.Match(Recombine.ReadAlignment.Input.Data.Cleaned, max_threads, HelperFunctionality.EvaluateTrilean(Recombine.ReadAlignment.ForceOnSingleTemplate, Recombine.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));

                                read_templates.Add(read_templates_group);

                                // Did readalign
                                if (progressBar != null) progressBar.Update();
                            }
                        }
                    }

                    var input = Input;
                    if (Recombine != null && Recombine.ReadAlignment != null)
                        input.AddRange(Recombine.ReadAlignment.Input.Data.Cleaned);

                    var parameters = new ReportInputParameters(input, databases, recombined_database, read_templates, this.BatchFile, this.Runname);

                    // Generate the report(s)
                    foreach (var report in Report)
                    {
                        switch (report)
                        {
                            case Report.HTML h:
                                var htmlreport = new HTMLReport(parameters, max_threads);
                                htmlreport.Save(h.CreateName(this));
                                break;
                            case Report.FASTA f:
                                var fastareport = new FASTAReport(parameters, f.MinimalScore, f.OutputType, max_threads);
                                fastareport.Save(f.CreateName(this));
                                break;
                        }
                    }

                    // Did reports
                    if (progressBar != null) progressBar.Update();
                }
                catch (Exception e)
                {
                    var msg = $"ERROR: {e.Message}\nSTACKTRACE: {e.StackTrace}\nRUNPARAMETERS:\n{Display()}";
                    throw new Exception(msg, e);
                }
            }
        }
    }
}
