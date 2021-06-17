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
    /// <summary> The main class which is the entry point from the command line. </summary>
    public class ToRunWithCommandLine
    {
        public const string VersionString = "0.0.0";
        static readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary> The entry point. </summary>
        static void Main()
        {
            Console.CancelKeyPress += HandleUserAbort;
            var args = Environment.CommandLine.Split(" ".ToCharArray());

            string CleanFileName(string filename)
            {
                if (filename.StartsWith('\'') && filename.EndsWith('\''))
                    return filename.Substring(1, filename.Length - 2);
                if (filename.StartsWith('\"') && filename.EndsWith('\"'))
                    return filename.Substring(1, filename.Length - 2);
                return filename;
            }

            // Retrieve the name of the batch file to run or file to clean
            string filename = "";
            string output_filename = "";
            bool clean = false;
            bool annotate = false;
            try
            {
                filename = args[1].Trim();
                if (filename == "clean")
                {
                    clean = true;
                    filename = string.Join(' ', args[2]).Trim();
                    if (args.Count() > 3)
                        output_filename = string.Join(' ', args[3]).Trim();
                    else
                        output_filename = filename;
                }
                else if (filename == "annotate")
                {
                    annotate = true;
                    filename = CleanFileName(string.Join(' ', Environment.CommandLine.Split(" ".ToCharArray()).Skip(2)).Trim());
                }
                else
                {
                    filename = CleanFileName(string.Join(' ', Environment.CommandLine.Split(" ".ToCharArray()).Skip(1)).Trim());
                }
            }
            catch
            {
                Console.WriteLine("Please provide as the first and only argument the path to the batch file to be run.\nOr give the command 'clean' followed by the fasta file to clean.");
                return;
            }

            try
            {
                if (clean)
                    CleanFasta(filename, output_filename);
                else if (annotate)
                    GenerateAnnotatedTemplate(filename, output_filename);
                else
                    RunBatchFile(filename);
            }
            catch (Exception e)
            {
                var msg = $"ERROR: {e.Message}\nSTACKTRACE: {e.StackTrace}";
                throw new Exception(msg, e);
            }
        }

        /// <summary>
        /// Run the given batch file
        /// </summary>
        /// <param name="filename"></param>
        public static void RunBatchFile(string filename)
        {
            var inputparams = ParseCommandFile.Batch(filename, false);

            var bar = new ProgressBar();
            var bars = 2; // Parse + TemplateMatching
            if (inputparams.Recombine != null)
                bars += inputparams.TemplateMatching.Segments.Count();
            bar.Start(bars);

            inputparams.CreateRun(bar).Calculate();
        }

        /// <summary>
        /// Gracefully handles user abort by printing a final message and the total time the program ran, after that it aborts.
        /// </summary>
        static void HandleUserAbort(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            var def = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("== Aborted based on user input ==");
            Console.ForegroundColor = def;
            Console.WriteLine($"Total time ran {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}.");
            Console.Out.Flush();
            Environment.Exit(1);
        }

        /// <summary> Cleans the given fasta file by deleting duplicates and removing sequences tagged as 'partial'. </summary>
        static void CleanFasta(string filename, string output)
        {
            var path = InputNameSpace.ParseHelper.GetFullPath(filename).ReturnOrFail();
            var namefilter = new NameFilter();
            var reads = OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(path, "name"), new Regex("^[^|]*\\|([^|]*)\\*\\d\\d\\|")).ReturnOrFail();
            var dict = new Dictionary<string, (string, string)>();

            // Condens all isoforms
            foreach (var read in reads)
            {
                var id = ((MetaData.Fasta)read.Item2).FastaHeader;
                if (!id.Contains("partial") && !id.Contains("/OR")) // Filter out all partial variants and /OR variants
                {
                    // Join all isoforms
                    if (dict.ContainsKey(read.Item2.Identifier))
                    {
                        dict[read.Item2.Identifier] = (dict[read.Item2.Identifier].Item1 + " " + id, read.Item1);
                    }
                    else
                    {
                        dict[read.Item2.Identifier] = (id, read.Item1);
                    }
                }
            }

            var sb = new StringBuilder();
            string EOL = Environment.NewLine;
            foreach (var (_, (id, seq)) in dict)
            {
                sb.AppendLine($">{id}{EOL}{seq}");
            }

            File.WriteAllText(InputNameSpace.ParseHelper.GetFullPath(output).ReturnOrFail(), sb.ToString());
        }

        /// <summary> Cleans the given fasta file by deleting duplicates and removing sequences tagged as 'partial'. </summary>
        static void GenerateAnnotatedTemplate(string filename, string output)
        {
            string content = InputNameSpace.ParseHelper.GetAllText(InputNameSpace.ParseHelper.GetFullPath(filename).ReturnOrFail()).ReturnOrFail();
            var namefilter = new NameFilter();

            content = content.Substring(content.IndexOf("<table class=\"tableseq\">"));
            content = content.Substring(content.IndexOf("</pre></td>\n") + 12);
            while (content.StartsWith("</tr>\n<tr>\n"))
            {
                content = content.Substring(content.IndexOf("</tr>\n<tr>\n") + 11);
                int newline = content.IndexOf('\n');
                var line = content.Substring(0, newline);
                content = content.Substring(newline + 1);

                var pieces = line.Split("</td><td>").ToArray();
                // 0 - nothing
                // 1 - Species
                // 2 - ID
                // 3 - Link Allele
                // 4 - Link AccNum
                // 5 - Segment
                // 6 - Functionality
                // 7 - Sequence
                var sequence = pieces[7].Substring(5, pieces[7].Length - 5 - 11); // Strip opening <pre> and closing </pre></td>
                Console.WriteLine(">" + pieces[2]);

                List<(List<string> classes, string seq)> ParseSequence(string input, List<string> current_classes, string current_seq, List<(List<string> classes, string seq)> result)
                {
                    if (input.Length == 0)
                    {
                        if (current_seq != "")
                            result.Add((new List<string>(current_classes), current_seq));
                        return result;
                    }
                    else if (input.StartsWith("<span"))
                    {
                        if (current_seq != "")
                            result.Add((new List<string>(current_classes), current_seq));
                        int i = 13; // Skip to classname
                        for (int j = 0; i + j < input.Length; j++)
                        {
                            if (input[i + j] == '"')
                            {
                                current_classes.Add(input.Substring(i, j));
                                i += j + 2;
                                break;
                            }
                        }
                        return ParseSequence(input.Substring(i), current_classes, "", result);
                    }
                    else if (input.StartsWith("</span>"))
                    {
                        if (current_seq != "")
                            result.Add((new List<string>(current_classes), current_seq));
                        current_classes.RemoveAt(current_classes.Count() - 1);
                        return ParseSequence(input.Substring(7), current_classes, "", result);
                    }
                    else if (input.StartsWith(' ') || input.StartsWith('.'))
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq, result);
                    }
                    else
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq + input[0], result);
                    }
                }

                var final_sequence = ParseSequence(sequence, new List<string>(), "", new List<(List<string> classes, string seq)>());
                var typed = new List<(string, string)>();
                bool J = false;
                foreach (var piece in final_sequence)
                {
                    var type = "";
                    foreach (var classname in piece.Item1)
                    {
                        switch (classname)
                        {
                            case "loop":
                                type = "CDR";
                                break;
                            case "J-motif":
                                J = true;
                                type = "Conserved";
                                break;
                            case "1st-CYS": // Used for any CYS
                            case "CONSERVED-TRP": // Used for any conserved amino acid
                                type = "Conserved";
                                break;
                            case "N-glycosylation":
                                type = "Glycosylationsite";
                                break;
                            case "mutation":
                                break;
                            default:
                                type = "UNKOWN: " + classname;
                                break;
                        };
                    }
                    typed.Add((type, piece.Item2));
                }
                var compressed = new List<(string, string)>();
                var last = ("", "");
                foreach (var piece in typed)
                {
                    if (piece.Item1 == last.Item1)
                    {
                        last = (last.Item1, last.Item2 + piece.Item2);
                    }
                    else
                    {
                        compressed.Add(last);
                        last = piece;
                    }
                }
                compressed.Add(last);
                // J segments always start with the last pieces of CDR3 but this is not properly annotated in the HTML files
                if (J)
                    compressed[0] = ("CDR", compressed[0].Item2);

                foreach (var piece in compressed)
                {
                    if (piece.Item1 == "")
                        Console.Write(piece.Item2);
                    else
                        Console.Write($"({piece.Item1} {piece.Item2})");
                }

                Console.Write("\n");

            }

            //File.WriteAllText(InputNameSpace.ParseHelper.GetFullPath(output).ReturnOrFail(), sb.ToString());
        }
    }
}