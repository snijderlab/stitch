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
            public string Runname = "";

            /// <summary>
            /// Determines the maximum number of CPU cores to be used.
            /// </summary>
            public int MaxNumberOfCPUCores = Environment.ProcessorCount;

            /// <summary>
            /// The input for this run
            /// </summary>
            public RunParameters.Input.InputParameters Input = null;

            /// <summary>
            /// The parameters for the template matching
            /// </summary>
            public TemplateMatchingParameter TemplateMatching = null;

            /// <summary>
            /// The recombine parameters (if given).
            /// </summary>
            public RecombineParameter Recombine = null;

            /// <summary>
            /// The report(s) to be generated for this run.
            /// </summary>
            public ReportParameter Report = null;

            /// <summary>
            /// To save the original batchfile
            /// </summary>
            public ParsedFile BatchFile = null;

            /// <summary>
            /// Creates the run
            /// </summary>
            public SingleRun CreateRun(ProgressBar bar = null)
            {
                var input = new RunParameters.Input();
                InputNameSpace.ParseHelper.PrepareInput(new NameFilter(), null, input, Input).ReturnOrFail();

                // Finalise all metadata names
                foreach (var set in input.Data.Raw)
                {
                    foreach (var read in set)
                    {
                        read.MetaData.FinaliseIdentifier();
                    }
                }

                return new SingleRun(Runname, input.Data.Cleaned, TemplateMatching, Recombine, Report, BatchFile, bar);
            }
        }
    }
}