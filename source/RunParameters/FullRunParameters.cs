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
        /// To give an 'api' for calling the program.
        /// </summary>
        public class FullRunParameters
        {
            /// <summary>
            /// The name of this run.
            /// </summary>
            public string Runname;

            /// <summary>
            /// The type of this run.
            /// </summary>
            public RuntypeValue Runtype;

            /// <summary>
            /// Determines the maximum number of CPU cores to be used.
            /// </summary>
            public int MaxNumberOfCPUCores;

            /// <summary>
            /// The input for this run
            /// </summary>
            public RunParameters.Input.InputParameters Input;

            /// <summary>
            /// Sets the parameters for the assembly
            /// </summary>
            public AssemblerParameter Assembly;

            /// <summary>
            /// The parameters for the template matching
            /// </summary>
            public TemplateMatchingParameter TemplateMatching;

            /// <summary>
            /// The recombine parameters (if given).
            /// </summary>
            public RecombineParameter Recombine;

            /// <summary>
            /// The report(s) to be generated for this run.
            /// </summary>
            public ReportParameter Report;

            /// <summary>
            /// To save the original batchfile
            /// </summary>
            public ParsedFile BatchFile;

            /// <summary>
            /// A blank instance for the RunParameters with defaults and initialization.
            /// </summary>
            public FullRunParameters()
            {
                Runname = "";
                Runtype = RuntypeValue.Group;
                MaxNumberOfCPUCores = Environment.ProcessorCount;

                Input = null;
                Assembly = null;
                TemplateMatching = null;
                Recombine = null;
                Report = null;
            }

            /// <summary>
            /// Creates a list of all single runs contained in this run.abstract TO be ran in parallel.
            /// </summary>
            /// <returns>All single runs.</returns>
            public List<SingleRun> CreateRuns(ProgressBar bar = null)
            {
                var output = new List<SingleRun>();

                var reverselist = new List<bool>();
                switch (Assembly.Reverse)
                {
                    case ReverseValue.True:
                        reverselist.Add(true);
                        break;
                    case ReverseValue.False:
                        reverselist.Add(false);
                        break;
                    case ReverseValue.Both:
                        reverselist.Add(true);
                        reverselist.Add(false);
                        break;
                }

                var klist = new List<int>();
                switch (Assembly.K)
                {
                    case K.Single s:
                        klist.Add(s.Value);
                        break;
                    case K.Multiple m:
                        klist.AddRange(m.Values);
                        break;
                    case K.Range r:
                        int v = r.Start;
                        while (v <= r.End)
                        {
                            klist.Add(v);
                            v += r.Step;
                        }
                        break;
                }

                int id = 0;
                foreach (var minimalHomology in Assembly.MinimalHomology)
                {
                    foreach (var duplicateThreshold in Assembly.DuplicateThreshold)
                    {
                        foreach (var reverse in reverselist)
                        {
                            foreach (var k in klist)
                            {
                                if (Runtype == RuntypeValue.Group)
                                {
                                    id++;
                                    output.Add(new SingleRun(id, Runname, Assembly.Input.Data.Cleaned, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, Assembly.Alphabet, TemplateMatching, Recombine, Report, BatchFile, bar));
                                }
                                else
                                {
                                    foreach (var input in Assembly.Input.Data.Raw)
                                    {
                                        id++;
                                        output.Add(new SingleRun(id, Runname, OpenReads.CleanUpInput(input), k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, Assembly.Alphabet, TemplateMatching, Recombine, Report, BatchFile, bar));
                                    }
                                }
                            }
                        }
                    }
                }
                return output;
            }
        }
    }
}