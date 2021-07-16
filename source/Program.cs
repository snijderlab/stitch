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
using System.Net;

namespace AssemblyNameSpace
{
    /// <summary> The main class which is the entry point from the command line. </summary>
    public class ToRunWithCommandLine
    {
        public const string VersionString = "0.0.0";
        static readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary> The entry point. </summary>
        static int Main()
        {
            Console.CancelKeyPress += HandleUserAbort;

            List<string> ParseArgs()
            {
                string args = Environment.CommandLine.Trim();
                string current_arg = "";
                var output = new List<string>();
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case ' ':
                            output.Add(current_arg.Trim());
                            current_arg = "";
                            break;
                        case '\'':
                            if (current_arg != "")
                                output.Add(current_arg.Trim());
                            current_arg = "";
                            int next = args.IndexOf('\'', i + 1);
                            output.Add(args.Substring(i + 1, next - i - 1).Trim());
                            i = next + 1;
                            break;
                        case '\"':
                            if (current_arg != "")
                                output.Add(current_arg.Trim());
                            current_arg = "";
                            int nextd = args.IndexOf('\"', i + 1);
                            output.Add(args.Substring(i + 1, nextd - i - 1).Trim());
                            i = nextd + 1;
                            break;
                        default:
                            current_arg += args[i];
                            break;
                    }
                }
                if (current_arg != "")
                    output.Add(current_arg.Trim());

                return output;
            }

            // Retrieve the name of the batch file to run or file to clean
            var args = ParseArgs();
            string filename = "";
            string output_filename = "";

            if (args.Count() <= 1 || args[1] == "help" || args[1] == "?")
            {
                Console.WriteLine(@"Please provide as the first and only argument the path to the batch file to be run.

Or use any of the following commands:
* clean - removes duplicates and incomplete sequences from fasta
    1   [path] the fasta file to clean 
    2?  [path] possibly followed by the new name for the cleaned file (if missing it will overwrite the old file)
eg: assembler.exe clean Homo_sapiens_IGHV.fasta

* annotate - parses the sequences from an IMGT html file and creates annotated fasta files
    1   [path] the html file to parse the annotated sequences from 
    2?  [path] possibly followed by the new name for the cleaned file (if missing it will overwrite the old file).
eg: assembler.exe annotate Homo_sapiens_IGHV.html Homo_sapiens_IGHV.fasta
note: for an example IMGT html file see: http://www.imgt.org/IMGTrepertoire/Proteins/proteinDisplays.php?species=human&latin=Homo%20sapiens&group=IGHV

* download - creates annotated fasta files for the given species
    1   [string] the latin name of the species to download (as used by IMGT: http://www.imgt.org/IMGTrepertoire/Proteins/)
    2?  [string] the segments to download, multiple can be downloaded in one file by combining with a comma ','
        default: ""IGHV IGKV,IGLV IGHJ IGKJ,IGLJ IGKC,IGLC""
eg: assembler.exe download ""Homo sapiens""
note: IGHC is not included as this is not present in a usefull form in the IMGT database, get from uniprot for the best results");
                return 2;
            }

            try
            {
                filename = args[1];
                if (filename == "clean")
                {
                    filename = args[2];
                    if (args.Count() > 3)
                        output_filename = args[3];
                    else
                        output_filename = filename;
                    CleanFasta(filename, output_filename);
                }
                else if (filename == "annotate")
                {
                    filename = args[2];
                    if (args.Count() > 3)
                        output_filename = args[3];
                    else
                        output_filename = filename;
                    GenerateAnnotatedTemplate(filename, output_filename);
                }
                else if (filename == "download")
                {
                    if (args.Count() > 3)
                        DownloadSpecies(args[2], args[3]);
                    DownloadSpecies(args[2]);
                }
                else
                {
                    filename = args[1];
                    RunBatchFile(filename);
                }
            }
            catch (ParseException)
            {
                return 3;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}\nSTACKTRACE: {e.StackTrace}\nSOURCE: {e.Source}\nTARGET: {e.TargetSite}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Run the given batch file
        /// </summary>
        /// <param name="filename"></param>
        public static void RunBatchFile(string filename)
        {
            var bar = new ProgressBar();
            bar.Start(4); // Max steps, can be turned down if no Recombination is done
            var inputparams = ParseCommandFile.Batch(filename, false);
            bar.Update();

            var bars = 3; // Parse + TemplateMatching + Report
            if (inputparams.Recombine != null)
                bars += 1;
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
            var reads = OpenReads.Fasta(namefilter, new MetaData.FileIdentifier(path, "name", null), new Regex("^[^|]*\\|([^|]*)\\*\\d\\d\\|")).ReturnOrFail();
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

        /// <summary>
        /// Download a set of templates for a mammalian organism assuming the same structure as Homo sapiens.
        /// </summary>
        /// <param name="name"></param>
        static void DownloadSpecies(string name, string segments = "IGHV IGKV,IGLV IGHJ IGKJ,IGLJ IGKC,IGLC")
        {
            var basename = $"http://www.imgt.org/3Dstructure-DB/cgi/DomainDisplay-include.cgi?species={name.Replace(" ", "%20")}&groups=";
            WebClient client = new WebClient();
            foreach (var segment in segments.Split(' '))
            {
                try
                {
                    Console.WriteLine(segment);
                    client.DownloadFile(basename + segment, "temp.html");
                    GenerateAnnotatedTemplate("temp.html", name.Replace(' ', '_') + "_" + segment + ".fasta");
                }
                catch
                {
                    Console.WriteLine("   Not available");
                }
            }
            File.Delete("temp.html");
        }

        /// <summary> Cleans the given fasta file by deleting duplicates and removing sequences tagged as 'partial'. </summary>
        static void GenerateAnnotatedTemplate(string filename, string output, bool remove_gaps = true)
        {
            string content = InputNameSpace.ParseHelper.GetAllText(InputNameSpace.ParseHelper.GetFullPath(filename).ReturnOrFail()).ReturnOrFail();
            var namefilter = new NameFilter();
            var writer = new StreamWriter(output);

            content = content.Substring(content.IndexOf("<table class=\"tableseq\">"));
            content = content.Substring(0, content.IndexOf("</table>"));
            content = content.Substring(content.IndexOf("</pre></td>\n") + 12);
            while (content.StartsWith("</tr>\n<tr>\n"))
            {
                content = content.Substring(content.IndexOf("</tr>\n<tr>\n") + 11);
                int newline = content.IndexOf('\n');
                var line = content.Substring(0, newline);
                content = content.Substring(newline + 1);

                if (line == "<td class=\"separegroup\"></td>") continue;

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
                    else if (input.StartsWith(' ') || (remove_gaps && input.StartsWith('.')))
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq, result);
                    }
                    else if (input.StartsWith(Alphabet.StopCodon))
                    {
                        return null; // Contains a stop codon so is not a valid sequence
                    }
                    else
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq + input[0], result);
                    }
                }

                var final_sequence = ParseSequence(sequence.Replace("<b>", "").Replace("</b>", ""), new List<string>(), "", new List<(List<string> classes, string seq)>());
                if (final_sequence == null)
                    continue;
                var typed = new List<(string Type, string Sequence)>();
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
                var compressed = new List<(string Type, string Sequence)>();
                (string Type, string Sequence) last = ("", "");
                foreach (var piece in typed)
                {
                    if (piece.Type == last.Type)
                    {
                        last = (last.Type, last.Sequence + piece.Sequence);
                    }
                    else
                    {
                        compressed.Add(last);
                        last = piece;
                    }
                }
                compressed.Add(last);

                int cdr = 1;
                bool cdr_found = false;
                for (int i = 0; i < compressed.Count; i++)
                {
                    if (compressed[i].Type == "CDR")
                    {
                        compressed[i] = ("CDR" + cdr.ToString(), compressed[i].Sequence);
                        cdr_found = true;
                    }
                    else if (compressed[i].Type == "" && cdr_found)
                    {
                        cdr++;
                        cdr_found = false;
                    }
                }

                // J segments always start with the last pieces of CDR3 but this is not properly annotated in the HTML files
                if (J)
                    compressed[0] = ("CDR3", compressed[0].Item2);

                writer.WriteLine(">" + pieces[2]);
                foreach (var piece in compressed)
                {
                    if (piece.Item1 == "")
                        writer.Write(piece.Item2);
                    else
                        writer.Write($"({piece.Item1} {piece.Item2})");
                }
                writer.Write("\n");
            }
            writer.Flush();
            writer.Close();
        }
    }
}