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
    /// <summary> This is a project to build a piece of software that is able to rebuild a protein sequence
    /// from reads of a massspectrometer.
    /// The software is build by Douwe Schulte and was started on 25-03-2019.
    /// It is build in collaboration with and under supervision of Joost Snijder,
    /// from the group "Massspectrometry and Proteomics" at the university of Utrecht. </summary>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }

    /// <summary> The main class which is the entry point from the command line. </summary>
    class ToRunWithCommandLine
    {
        public const string VersionString = "0.0.0";

        /// <summary> The entry point. </summary>
        static void Main()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.CancelKeyPress += delegate
            {
                var def = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("== Aborted based on user input ==");
                Console.ForegroundColor = def;
                Console.WriteLine($"Total time ran {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}.");
            };

            // Retrieve the name of the batch file to run
            string filename = "";
            try
            {
                filename = Environment.CommandLine.Split(" ".ToCharArray(), 2)[1].Trim();
            }
            catch
            {
                Console.WriteLine("Please provide as the first and only argument the path to the batch file to be run.");
                return;
            }

            // Try to parse the batch file
            var inputparams = new RunParameters.FullRunParameters();
            try
            {
                inputparams = ParseCommandFile.Batch(filename);
            }
            catch (ParseException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"{e.Message}");
                Console.ResetColor();
                Console.WriteLine("The program now terminates.");
                return;
            }
            Console.WriteLine("Parsed file");

            var bar = new ProgressBar();
            var runs = inputparams.CreateRuns(bar);

            string pluralsuffix = runs.Count() > 1 ? "s" : "";
            Console.WriteLine($"Read the file, it will now start working on the {runs.Count()} run{pluralsuffix} to be done.");

            bar.Start(runs.Count() * 3);

            if (runs.Count() == 1)
            {
                runs[0].Calculate(Environment.ProcessorCount);
            }
            else if (runs.Count() < Environment.ProcessorCount)
            {
                Parallel.ForEach(
                    runs,
                    new ParallelOptions { MaxDegreeOfParallelism = runs.Count() },
                    (i) => i.Calculate((int)Math.Round((double)Environment.ProcessorCount / runs.Count())));
            }
            else
            {
                Parallel.ForEach(runs, (i) => i.Calculate(1));
            }

            stopwatch.Stop();
            Console.WriteLine($"Assembled all {runs.Count()} run{pluralsuffix} in {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}");
        }
    }
}