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
    /// <summary> The main class which is the entry point from the command line </summary>
    class ToRunWithCommandLine
    {
        public const string VERSIONNUMBER = "0.0.0";
        /// <summary> The entry point. </summary>
        static void Main()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

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
            var runs = inputparams.CreateRuns();

            string pluralsuffix = runs.Count() > 1 ? "s" : "";
            Console.WriteLine($"Read the file, it will now start working on the {runs.Count()} run{pluralsuffix} to be done.");
            Parallel.ForEach(runs, (i) => i.Calculate());

            stopwatch.Stop();
            Console.WriteLine($"Assembled all {runs.Count()} run{pluralsuffix} in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}