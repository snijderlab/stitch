using System;

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
            /// The directory which contains all raw files.
            /// </summary>
            public string RawDataDirectory = null;

            /// <summary>
            /// Determines the maximum number of CPU cores to be used.
            /// </summary>
            public int MaxNumberOfCPUCores = Environment.ProcessorCount;

            /// <summary>
            /// The input for this run
            /// </summary>
            public RunParameters.InputData Input = new InputData();

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
            public SingleRun CreateRun(RunVariables variables, ProgressBar bar = null)
            {
                return new SingleRun(Runname, Input.Data.Cleaned, TemplateMatching, Recombine, Report, BatchFile, MaxNumberOfCPUCores, variables, RawDataDirectory, bar);
            }
        }
    }
}