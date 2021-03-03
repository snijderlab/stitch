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

                // Template Matching
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var databases = RunTemplateMatching();

                stopWatch.Stop();

                List<TemplateDatabase> recombined_database = new List<TemplateDatabase>();
                var matches = new List<List<(int GroupIndex, int, int TemplateIndex, SequenceMatch Match)>>(Input.Count());
                for (int i = 0; i < Input.Count(); i++) matches.Add(new List<(int, int, int, SequenceMatch)>());

                List<TemplateDatabase> read_templates = new List<TemplateDatabase>();
                int read_template_number = (Recombine != null && Recombine.ReadAlignment != null) ? Recombine.ReadAlignment.Input.Data.Cleaned.Count() : 0;
                var read_matches = new List<List<(int GroupIndex, int, int TemplateIndex, SequenceMatch Match)>>(read_template_number);
                for (int i = 0; i < read_template_number; i++) read_matches.Add(new List<(int, int, int, SequenceMatch)>());

                // Recombine
                if (Recombine != null)
                {
                    Stopwatch recombine_sw = new Stopwatch();
                    recombine_sw.Start();

                    RunRecombine(databases, matches, recombined_database);

                    recombine_sw.Stop();

                    // Do ReadAlignment
                    if (Recombine.ReadAlignment != null)
                        RunReadAlign(read_matches, read_templates, recombined_database, max_threads);
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

            List<(string, List<TemplateDatabase>)> RunTemplateMatching()
            {
                // Initialise the matches list with empty lists for every input read
                var matches = new List<List<(int GroupIndex, int DatabaseIndex, int TemplateIndex, SequenceMatch Match)>>(Input.Count());
                for (int i = 0; i < Input.Count(); i++) matches.Add(new List<(int, int, int, SequenceMatch)>());

                var databases = new List<(string, List<TemplateDatabase>)>();
                for (int i = 0; i < TemplateMatching.Databases.Count(); i++)
                {
                    var current_group = new List<TemplateDatabase>();
                    for (int j = 0; j < TemplateMatching.Databases[i].Databases.Count(); j++)
                    {
                        var database = TemplateMatching.Databases[i].Databases[j];
                        var alph = new Alphabet(database.Alphabet ?? TemplateMatching.Alphabet);

                        var database1 = new TemplateDatabase(database.Templates, alph, database.Name, database.CutoffScore == 0 ? TemplateMatching.CutoffScore : database.CutoffScore, j, database.Scoring, database.ClassChars);

                        // These contain all matches for all Input reads (outer list) for all templates (inner list) with the score
                        var local_matches = database1.Match(Input);

                        for (int read = 0; read < Input.Count(); read++)
                            matches[read].AddRange(local_matches[read].Select(a => (i, j, a.Item1, a.Item2)));

                        current_group.Add(database1);
                    }
                    databases.Add((TemplateMatching.Databases[i].Name, current_group));

                    // Save the progress, finished a group
                    if (progressBar != null) progressBar.Update();
                }
                // Filter matches if Forced
                if (TemplateMatching.ForceOnSingleTemplate)
                    ForceOnSingleTemplate(matches);

                // Add all matches to the right templates
                foreach (var row in matches)
                {
                    var unique = row.Count() == 1;
                    foreach (var match in row)
                    {
                        databases[match.GroupIndex].Item2[match.DatabaseIndex].Templates[match.TemplateIndex].AddMatch(match.Match, unique);
                    }
                }

                return databases;
            }

            void RunRecombine(List<(string, List<TemplateDatabase>)> databases, List<List<(int GroupIndex, int, int TemplateIndex, SequenceMatch Match)>> matches, List<TemplateDatabase> recombined_database)
            {
                var alph = new Alphabet(Recombine.Alphabet ?? TemplateMatching.Alphabet);

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
                    CreateRecombinationTemplates(combinations, database_group_index, alph, recombined_database_group);

                    var local_matches = recombined_database_group.Match(Input);

                    for (int read = 0; read < Input.Count(); read++)
                        matches[read].AddRange(local_matches[read].Select(a => (database_group_index, 0, a.Item1, a.Item2)));

                    recombined_database.Add(recombined_database_group);

                    // Did recombination + databases
                    if (progressBar != null) progressBar.Update();
                }

                // Filter matches if Forced
                if (HelperFunctionality.EvaluateTrilean(Recombine.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate))
                    ForceOnSingleTemplate(matches);


                // Add all matches to the right templates
                foreach (var row in matches)
                {
                    var unique = row.Count() == 1;
                    foreach (var match in row)
                    {
                        recombined_database[match.GroupIndex].Templates[match.TemplateIndex].AddMatch(match.Match, unique);
                    }
                }
            }

            void RunReadAlign(List<List<(int GroupIndex, int, int TemplateIndex, SequenceMatch Match)>> read_matches, List<TemplateDatabase> read_templates, List<TemplateDatabase> recombined_database, int max_threads)
            {
                for (int database_group_index = 0; database_group_index < recombined_database.Count(); database_group_index++)
                {
                    List<(string, MetaData.IMetaData)> templates = new List<(string, MetaData.IMetaData)>(recombined_database[database_group_index].Templates.Count());
                    var namefilter = new NameFilter();

                    Parallel.ForEach(
                        recombined_database[database_group_index].Templates,
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

                    var input = Recombine.ReadAlignment.Input.Data.Cleaned ?? Input;

                    var read_templates_group = new TemplateDatabase(templates, new Alphabet(Recombine.ReadAlignment.Alphabet), "ReadAlignDatabase", Recombine.ReadAlignment.CutoffScore, 0);

                    var read_local_matches = read_templates_group.Match(input);

                    for (int read = 0; read < input.Count(); read++)
                        read_matches[read].AddRange(read_local_matches[read].Select(a => (database_group_index, 0, a.Item1, a.Item2)));

                    read_templates.Add(read_templates_group);

                    // Did readalign
                    if (progressBar != null) progressBar.Update();
                }

                // Filter matches if Forced
                if (HelperFunctionality.EvaluateTrilean(Recombine.ReadAlignment.ForceOnSingleTemplate, Recombine.ForceOnSingleTemplate, TemplateMatching.ForceOnSingleTemplate))
                    ForceOnSingleTemplate(read_matches);

                // Add all matches to the right templates
                foreach (var row in read_matches)
                {
                    var unique = row.Count() == 1;
                    foreach (var match in row)
                    {
                        read_templates[match.GroupIndex].Templates[match.TemplateIndex].AddMatch(match.Match, unique);
                    }
                }
            }

            void CreateRecombinationTemplates(System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<AssemblyNameSpace.Template>> combinations, int database_group_index, Alphabet alphabet, TemplateDatabase parent)
            {
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
                                var aligned_template = HelperFunctionality.EndAlignment(s.ToArray(), seq.ToArray(), alphabet, 20);
                                // When no good overlap is found just paste them one after the other
                                if (aligned_template.Score >= 0)
                                    s.AddRange(seq.Skip(aligned_template.Position));
                                else
                                {
                                    s.Add(new AminoAcid(alphabet, '*'));
                                    s.AddRange(seq);
                                }
                            }
                            else
                            {
                                s.AddRange(seq);
                            }
                            t.Add(sequence.ElementAt(((RecombineOrder.Template)element).Index));
                        }
                    }
                    recombined_templates.Add(new Template("recombined", s.ToArray(), new MetaData.Simple(new MetaData.FileIdentifier(), namefilter, "REC"), parent, new RecombinedTemplateLocation(i), t));
                }
                foreach (var read in recombined_templates) read.MetaData.FinaliseIdentifier();

                parent.Templates = recombined_templates;
            }

            void ForceOnSingleTemplate(List<List<(int, int, int, SequenceMatch Match)>> matches)
            {
                for (int read_index = 0; read_index < matches.Count(); read_index++)
                {
                    var best = new List<(int, int, int, SequenceMatch)>();
                    var best_score = 0;
                    for (int template_index = 0; template_index < matches[read_index].Count(); template_index++)
                    {
                        var match = matches[read_index][template_index];
                        if (match.Match.Score > best_score)
                        {
                            best.Clear();
                            best.Add(match);
                            best_score = match.Match.Score;
                        }
                        else if (match.Match.Score == best_score)
                        {
                            best.Add(match);
                        }
                    }
                    matches[read_index].Clear();
                    matches[read_index].AddRange(best);
                }
            }
        }
    }
}
