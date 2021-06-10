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

            // Retrieve the name of the batch file to run or file to clean
            string filename = "";
            string output_filename = "";
            bool clean = false;
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
                else
                {
                    filename = string.Join(' ', Environment.CommandLine.Split(" ".ToCharArray()).Skip(1)).Trim();
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

            inputparams.CreateRun(bar).Calculate(inputparams.MaxNumberOfCPUCores);
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
    }
}