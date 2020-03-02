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
            /// The inputs for this run.
            /// </summary>
            public List<Input.Parameter> DataParameters;

            /// <summary>
            /// The K or values of K for this run.
            /// </summary>
            public K.KValue K;

            /// <summary>
            /// The value of Reverse for this run.
            /// </summary>
            public ReverseValue Reverse;

            /// <summary>
            /// The value for the MinimalHomology.
            /// </summary>
            public List<KArithmetic> MinimalHomology;

            /// <summary>
            /// The value for the duplicatethreshold.
            /// </summary>
            public List<KArithmetic> DuplicateThreshold;

            /// <summary>
            /// The alphabet(s) to be used in this run.
            /// </summary>
            public List<AlphabetValue> Alphabet;

            /// <summary>
            /// The template(s) to be used in this run.
            /// </summary>
            public List<TemplateValue> Template;

            /// <summary>
            /// The report(s) to be generated for this run.
            /// </summary>
            public List<Report.Parameter> Report;

            /// <summary>
            /// The recombine parameters (if given).
            /// </summary>
            public RecombineValue Recombine;

            /// <summary>
            /// A blank instance for the RunParameters with defaults and initialization.
            /// </summary>
            public FullRunParameters()
            {
                Runname = "";
                Runtype = RuntypeValue.Group;
                DataParameters = new List<Input.Parameter>();
                Reverse = ReverseValue.False;
                MinimalHomology = new List<KArithmetic>();
                DuplicateThreshold = new List<KArithmetic>();
                Alphabet = new List<AlphabetValue>();
                Template = new List<TemplateValue>();
                Report = new List<Report.Parameter>();
                Recombine = null;
            }

            /// <summary>
            /// Creates a list of all single runs contained in this run.abstract TO be ran in parallel.
            /// </summary>
            /// <returns>All single runs.</returns>
            public List<SingleRun> CreateRuns(ProgressBar bar = null)
            {
                var output = new List<SingleRun>();

                var reverselist = new List<bool>();
                switch (Reverse)
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
                switch (K)
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
                foreach (var minimalHomology in MinimalHomology)
                {
                    foreach (var duplicateThreshold in DuplicateThreshold)
                    {
                        foreach (var alphabet in Alphabet)
                        {
                            foreach (var reverse in reverselist)
                            {
                                foreach (var k in klist)
                                {
                                    if (Runtype == RuntypeValue.Group)
                                    {
                                        id++;
                                        output.Add(new SingleRun(id, Runname, DataParameters, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Template, Recombine, Report, bar));
                                    }
                                    else
                                    {
                                        foreach (var input in DataParameters)
                                        {
                                            id++;
                                            output.Add(new SingleRun(id, Runname, input, k, duplicateThreshold.GetValue(k), minimalHomology.GetValue(k), reverse, alphabet, Template, Recombine, Report, bar));
                                        }
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