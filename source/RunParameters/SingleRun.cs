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
            public List<(string, MetaData.IMetaData)> Input;

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

            readonly bool Assemble;

            /// <summary>
            /// To create a single run with a single dataparameter as input. With assembly.
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
            public SingleRun(int id, string runname, List<(string, MetaData.IMetaData)> input, int k, int duplicateThreshold, int minimalHomology, bool reverse, AlphabetParameter alphabet, TemplateMatchingParameter templateMatching, RecombineParameter recombine, ReportParameter report, ParsedFile batchfile, ProgressBar bar = null)
            {
                Assemble = true;
                ID = id;
                Runname = runname;
                Input = input;
                K = k;
                DuplicateThreshold = duplicateThreshold;
                MinimalHomology = minimalHomology;
                Reverse = reverse;
                Alphabet = alphabet;
                TemplateMatching = templateMatching;
                Recombine = recombine;
                Report = report.Files;
                BatchFile = batchfile;
                progressBar = bar;
            }

            /// <summary>
            /// To create a single run with a single dataparameter as input. Without assembly
            /// </summary>
            /// <param name="id">The ID of the run.</param>
            /// <param name="runname">The name of the run.</param>
            /// <param name="input">The input data to be run.</param>
            /// <param name="template">The templates to be used.</param>
            /// <param name="recombine">The recombination, if needed.</param>
            /// <param name="report">The report(s) to be generated.</param>
            public SingleRun(int id, string runname, List<(string, MetaData.IMetaData)> input, TemplateMatchingParameter templateMatching, RecombineParameter recombine, ReportParameter report, ParsedFile batchfile, ProgressBar bar = null)
            {
                Assemble = false;
                ID = id;
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
                return $"\tRunname\t\t: {Runname}\n\tID: {ID}";
            }

            /// <summary>
            /// Runs this run. Runs the assembly, and generates the reports.
            /// </summary>
            public void Calculate(int max_threads = 1)
            {
                try
                {
                    Assembler assm = null;
                    if (Assemble)
                    {
                        var alphabet = new Alphabet(Alphabet);
                        assm = new Assembler(K, DuplicateThreshold, MinimalHomology, Reverse, alphabet);

                        assm.GiveReads(Input);

                        assm.Assemble();

                        // Did assembly
                        if (progressBar != null) progressBar.Update();
                    }

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

                            if (Assemble) database1.Match(assm.GetAllPaths(), max_threads, HelperFunctionality.EvaluateTrilean(database.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));
                            else database1.Match(Input, max_threads, HelperFunctionality.EvaluateTrilean(database.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));

                            if (Assemble && HelperFunctionality.EvaluateTrilean(database.IncludeShortReads, TemplateMatching.IncludeShortReads))
                            {
                                var reads = new List<(string, MetaData.IMetaData)>(assm.shortReads.Count());
                                for (int k = 0; k < assm.shortReads.Count; k++) reads.Add((AminoAcid.ArrayToString(assm.shortReads[k].Sequence), assm.shortReads[k].MetaData));
                                database1.Match(reads, max_threads, HelperFunctionality.EvaluateTrilean(database.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));
                            }

                            current_group.Add(database1);
                        }
                        databases.Add((TemplateMatching.Databases[i].Name, current_group));
                    }

                    stopWatch.Stop();
                    if (Assemble) assm.meta_data.template_matching_time = stopWatch.ElapsedMilliseconds;

                    List<TemplateDatabase> recombined_database = new List<TemplateDatabase>();
                    List<TemplateDatabase> read_templates = new List<TemplateDatabase>();

                    // Recombine
                    if (Recombine != null)
                    {
                        Stopwatch recombine_sw = new Stopwatch();
                        recombine_sw.Start();

                        if (!Assemble && Recombine.Alphabet == null)
                        {
                            new InputNameSpace.ErrorMessage("No alphabet defined", "Please make define an alphabet for recombination if no assembly takes place", "").Print();
                            return;
                        }
                        var alph = Recombine.Alphabet != null ? new Alphabet(Recombine.Alphabet) : assm.alphabet;

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
                            if (Assemble) recombined_database_group.Match(assm.GetAllPaths(), max_threads, force);
                            else recombined_database_group.Match(Input, max_threads, force);

                            if (Assemble && HelperFunctionality.EvaluateTrilean(Recombine.IncludeShortReads, TemplateMatching.IncludeShortReads))
                            {
                                var reads = new List<(string, MetaData.IMetaData)>(assm.shortReads.Count);
                                for (int i = 0; i < assm.shortReads.Count; i++) reads.Add((AminoAcid.ArrayToString(assm.shortReads[i].Sequence), assm.shortReads[i].MetaData));
                                recombined_database_group.Match(reads, max_threads, HelperFunctionality.EvaluateTrilean(Recombine.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate));
                            }

                            recombined_database.Add(recombined_database_group);

                            recombine_sw.Stop();

                            // Did recombination + databases
                            //if (progressBar != null) progressBar.Update();

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
                                //if (progressBar != null) progressBar.Update();
                            }
                        }
                    }

                    var parameters = new ReportInputParameters(assm, Input, databases, recombined_database, read_templates, this.BatchFile, this.Runname);

                    // Generate the report(s)
                    foreach (var report in Report)
                    {
                        switch (report)
                        {
                            case Report.HTML h:
                                var htmlreport = new HTMLReport(parameters, h.UseIncludedDotDistribution, max_threads);
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
