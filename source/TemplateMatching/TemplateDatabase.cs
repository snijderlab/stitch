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
using System.ComponentModel;


namespace AssemblyNameSpace
{
    public class TemplateDatabase
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Alphabet Alphabet;
        public List<Template> Templates;
        public readonly double CutoffScore;
        public readonly RunParameters.ScoringParameter Scoring;
        /// <summary>
        /// Create a new TemplateDatabase based on the reads found in the given file.
        /// </summary>
        /// <param name="sequences">The reads to generate templates from</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        /// <param name="cutoffScore">The cutoffscore for a path to be aligned to a template</param>
        /// <param name="index">The index of this template for cross reference purposes</param>
        /// <param name="scoring">The scoring behaviour to use in this database</param>
        public TemplateDatabase(List<(string, MetaData.IMetaData)> sequences, Alphabet alphabet, string name, double cutoffScore, int index, RunParameters.ScoringParameter scoring = RunParameters.ScoringParameter.Absolute)
        {
            Name = name;
            Index = index;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = new List<Template>();
            Scoring = scoring;

            for (int i = 0; i < sequences.Count(); i++)
            {
                var pair = sequences[i];
                var parsed = StringToSequence(pair.Item1);
                Templates.Add(new Template(name, parsed, pair.Item2, this, new TemplateLocation(index, i)));
            }
        }
        /// <summary>
        /// Create a new TemplateDatabase based on the templates provided.
        /// </summary>
        /// <param name="templates">The templates</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        /// <param name="cutoffScore">The cutoffscore for a path to be aligned to a template</param>
        public TemplateDatabase(ICollection<Template> templates, Alphabet alphabet, string name, double cutoffScore)
        {
            Name = name;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = templates.ToList();
        }
        /// <summary>
        /// Gets the sequence in AminoAcids from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(Alphabet, input[i]);
            }
            return output;
        }
        /// <summary>
        /// Match the given sequences to the database. Saves the results in this instance of the database.
        /// </summary>
        /// <param name="sequences">The sequences to match with</param>
        public void Match(List<GraphPath> sequences, int max_threads = 1)
        {
            if (max_threads == 1)
            {
                MatchSerial(sequences);
            }
            else
            {
                MatchParallel(sequences, max_threads);
            }
        }
        /// <summary>
        /// Match the given sequences to the database. Saves the results in this instance of the database.
        /// </summary>
        /// <param name="sequences">The sequences to match with</param>
        void MatchParallel(List<GraphPath> sequences, int max_threads)
        {
            var runs = new List<(Template, GraphPath)>();

            foreach (var tem in Templates)
            {
                for (int i = 0; i < sequences.Count(); i++)
                {
                    runs.Add((tem, sequences[i]));
                }
            }

            Parallel.ForEach(
                runs,
                new ParallelOptions { MaxDegreeOfParallelism = max_threads },
                (s, _) => s.Item1.AddMatch(HelperFunctionality.SmithWaterman(s.Item1.Sequence, s.Item2.Sequence, Alphabet, s.Item2))
            );
        }
        void MatchSerial(List<GraphPath> sequences)
        {
            int y = 0;
            foreach (var tem in Templates)
            {
                int x = 0;
                for (int i = 0; i < sequences.Count(); i++)
                {
                    tem.AddMatch(HelperFunctionality.SmithWaterman(tem.Sequence, sequences[i].Sequence, Alphabet, sequences[i]));
                    x++;
                }
                y++;
            }
        }
        /// <summary>
        /// Create a string summary of a template database.
        /// </summary>
        public override string ToString()
        {
            return $"TemplateDatabase {Name} with {Templates.Count()} templates in total";
        }
    }
}