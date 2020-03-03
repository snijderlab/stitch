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
        readonly Alphabet alphabet;
        public List<Template> Templates;
        readonly double cutoffScore;
        /// <summary>
        /// Create a new TemplateDatabase based on the reads found in the given file.
        /// </summary>
        /// <param name="file">The file to open</param>
        /// <param name="type">The type of the file</param>
        /// <param name="alp">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        public TemplateDatabase(MetaData.FileIdentifier file, RunParameters.InputType type, Alphabet alp, string name, double _cutoffScore, int index)
        {
            Name = name;
            Index = index;
            cutoffScore = _cutoffScore;
            alphabet = alp;
            Templates = new List<Template>();

            List<(string, MetaData.IMetaData)> sequences;

            if (type == RunParameters.InputType.Reads)
            {
                sequences = OpenReads.Simple(file);
            }
            else if (type == RunParameters.InputType.Fasta)
            {
                sequences = OpenReads.Fasta(file);
            }
            else
            {
                throw new Exception($"The type {type} is not a valid type for a template database file (file: {file})");
            }

            for (int i = 0; i < sequences.Count(); i++)
            {
                var pair = sequences[i];
                var parsed = StringToSequence(pair.Item1);
                Templates.Add(new Template(name, parsed, pair.Item2, alphabet, cutoffScore, new TemplateLocation(index, i)));
            }
        }
        /// <summary>
        /// Create a new TemplateDatabase based on the templates provided.
        /// </summary>
        /// <param name="templates">The templates</param>
        /// <param name="alp">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        public TemplateDatabase(ICollection<Template> templates, Alphabet alp, string name, double _cutoffScore)
        {
            Name = name;
            cutoffScore = _cutoffScore;
            alphabet = alp;
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
                output[i] = new AminoAcid(alphabet, input[i]);
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
                (s, _) => s.Item1.AddMatch(HelperFunctionality.SmithWaterman(s.Item1.Sequence, s.Item2.Sequence, alphabet, s.Item2))
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
                    tem.AddMatch(HelperFunctionality.SmithWaterman(tem.Sequence, sequences[i].Sequence, alphabet));
                    x++;
                }
                y++;
            }
        }
        /// <summary>
        /// Match the given sequences to the database. Saves the results in this instance of the database.
        /// </summary>
        /// <param name="sequences">The sequences to match with</param>
        public void Match(List<List<AminoAcid>> sequences)
        {
            int y = 0;
            foreach (var tem in Templates)
            {
                int x = 0;
                for (int i = 0; i < sequences.Count(); i++)
                {
                    tem.AddMatch(HelperFunctionality.SmithWaterman(tem.Sequence, sequences[i].ToArray(), alphabet));
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