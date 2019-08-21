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

// Build using .NET Core
// https://opensource.com/article/17/5/cross-platform-console-apps

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
    /// <summary> A Class to be able to run the code from the commandline. To be able to test it easily. 
    /// This will be rewritten when the code is moved to its new repository </summary>
    class ToRunWithCommandLine
    {
        /// <summary> The method that will be run if the code is run from the command line. </summary>
        static void Main()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string filename = Environment.CommandLine.Split(" ".ToCharArray(), 2)[1].Trim();
            RunParameters inputparams = ParseCommandFile.Batch(filename);
            Console.WriteLine("Parsed file");
            var runs = inputparams.CreateRuns();

            Console.WriteLine($"Read the file, it will now start working on the {runs.Count()} run(s) to be done.");
            Parallel.ForEach(runs, (i) => i.Calculate());

            stopwatch.Stop();
            Console.WriteLine($"Assembled all in {stopwatch.ElapsedMilliseconds} ms");

        }
    }
}