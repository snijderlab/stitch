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
    class TemplateDatabase
    {
        public readonly string Name;
        Alphabet alphabet;
        public List<Template> Templates;
        /// <summary>
        /// Create a new TemplateDatabase based on the reads found in the given file.
        /// </summary>
        /// <param name="file">The file to open</param>
        /// <param name="type">The type of the file</param>
        /// <param name="alp">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        public TemplateDatabase(MetaData.FileIdentifier file, RunParameters.InputType type, Alphabet alp, string name)
        {
            Name = name;
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

            alphabet = alp;
            Templates = new List<Template>();

            foreach (var pair in sequences)
            {
                var parsed = StringToSequence(pair.Item1);
                Templates.Add(new Template(parsed, pair.Item2));
            }
        }
        /// <summary>
        /// Create a new TemplateDatabase based on the templates provided.
        /// </summary>
        /// <param name="templates">The templates</param>
        /// <param name="alp">The alphabet to use</param>
        /// <param name="name">The name for this templatedatabase</param>
        public TemplateDatabase(ICollection<Template> templates, Alphabet alp, string name)
        {
            Name = name;
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
        public void Match(List<List<AminoAcid>> sequences)
        {
            int y = 0;
            foreach (var tem in Templates)
            {
                int x = 0;
                for (int i = 0; i < sequences.Count(); i++)
                {
                    tem.AddMatch(HelperFunctionality.SmithWaterman(tem.Sequence, sequences[i].ToArray(), i, alphabet));
                    x++;
                }
                y++;
            }
        }
        /// <summary>
        /// Match the given sequences to the database. Saves the results in this instance of the database.
        /// </summary>
        /// <param name="sequences">The sequences to match with</param>
        public void MatchParallel(List<List<AminoAcid>> sequences)
        {
            var runs = new List<(Template, AminoAcid[], int)>();

            foreach (var tem in Templates)
            {
                for (int i = 0; i < sequences.Count(); i++)
                {
                    runs.Add((tem, sequences[i].ToArray(), i));
                }
            }

            Parallel.ForEach(runs, (s, _) => s.Item1.AddMatch(HelperFunctionality.SmithWaterman(s.Item1.Sequence, s.Item2, s.Item3, alphabet)));
        }
        /// <summary>
        /// Create a string summary of a template database.
        /// </summary>
        public override string ToString()
        {
            return $"TemplateDatabase {Name} with {Templates.Count()} templates in total";
        }
    }
    public class Template
    {
        public readonly AminoAcid[] Sequence;
        public readonly MetaData.IMetaData MetaData;
        public int Score { get; private set; }
        public List<SequenceMatch> Matches;
        public Template(AminoAcid[] seq, MetaData.IMetaData meta)
        {
            Sequence = seq;
            MetaData = meta;
            Score = -1;
            Matches = new List<SequenceMatch>();
        }
        public void AddMatch(SequenceMatch match)
        {
            Score += match.Score;
            Matches.Add(match);
        }
        public List<(Dictionary<AminoAcid, int>, Dictionary<IGap, int>)> CombinedSequence()
        {
            var output = new List<(Dictionary<AminoAcid, int>, Dictionary<IGap, int>)>();
            output.Capacity = Sequence.Length;
            //Console.WriteLine($"The total sequence is {Sequence.Length} aa");

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new Dictionary<AminoAcid, int>(), new Dictionary<IGap, int>()));
            }

            foreach (var match in Matches)
            {
                // Start at StartTemplatePosition and StartQueryPosition
                var template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                //Console.WriteLine($"This match is {match.TotalMatches()} matches long and {match.Sequence.Count()} aa {match}");

                foreach (var piece in match.Alignment)
                {
                    //Console.WriteLine($"at pos {template_pos}:{seq_pos}");
                    if (piece is SequenceMatch.Match m)
                    {
                        //Console.WriteLine($"Found a match of {m.count} aa");
                        for (int i = 0; i < m.count && template_pos < Sequence.Length && seq_pos < match.Sequence.Count(); i++)
                        {
                            // Try to add this AminoAcid or update the count
                            AminoAcid key;
                            try {
                                key = match.Sequence.ElementAt(seq_pos);
                            } catch {
                                //Console.WriteLine($"Exception: at seq pos {seq_pos}");
                                throw new Exception("");
                            }
                            if (output[template_pos].Item1.ContainsKey(key))
                            {
                                output[template_pos].Item1[key] += 1;
                            }
                            else
                            {
                                output[template_pos].Item1.Add(key, 1);
                            }
                            // Save that there is no gap
                            if (i != m.count - 1)
                            {
                                if (output[template_pos].Item2.ContainsKey(new None()))
                                {
                                    output[template_pos].Item2[new None()] += 1;
                                }
                                else
                                {
                                    output[template_pos].Item2.Add(new None(), 1);
                                }
                            }

                            template_pos++;
                            seq_pos++;
                        }
                    }
                    else if (piece is SequenceMatch.GapTemplate gt)
                    {
                        // Try to add this sequence or update the count
                        var sub_seq = new Gap(match.Sequence.ToArray().SubArray(seq_pos, Math.Min(gt.count, match.Sequence.Count() - seq_pos - 1)));
                        seq_pos += gt.count;
                        if (output[template_pos].Item2.ContainsKey(sub_seq))
                        {
                            output[template_pos].Item2[sub_seq] += 1;
                        }
                        else
                        {
                            output[template_pos].Item2.Add(sub_seq, 1);
                        }
                    }
                    else if (piece is SequenceMatch.GapContig gc)
                    {
                        // Skip to the next section
                        template_pos += gc.count;
                    }
                }
            }
            return output;
        }
        public interface IGap { }
        public struct None : IGap
        {
            public override string ToString() { return ""; }
        }
        public struct Gap : IGap
        {
            public readonly AminoAcid[] Sequence;
            public Gap(AminoAcid[] sequence)
            {
                Sequence = sequence;
            }
            public override string ToString()
            {
                return AminoAcid.ArrayToString(Sequence);
            }
        }
    }
}