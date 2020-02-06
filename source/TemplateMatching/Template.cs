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
    public class Template
    {
        public readonly string Name;
        public readonly AminoAcid[] Sequence;
        public readonly MetaData.IMetaData MetaData;
        public int Score { get; private set; }
        public List<SequenceMatch> Matches;
        public readonly Alphabet Alphabet;
        readonly double cutoffScore;
        public readonly List<Template> Recombination;
        public readonly TemplateLocation Location;
        public Template(string name, AminoAcid[] seq, MetaData.IMetaData meta, Alphabet alphabet, double _cutoffScore, TemplateLocation location = null, List<Template> recombination = null)
        {
            Name = name;
            Sequence = seq;
            MetaData = meta;
            Score = 0;
            Matches = new List<SequenceMatch>();
            Alphabet = alphabet;
            cutoffScore = _cutoffScore;
            Recombination = recombination;
            Location = location;
        }
        public void AddMatch(SequenceMatch match)
        {
            if (match.Score >= cutoffScore * Math.Sqrt(match.QuerySequence.Length))
            {
                Score += match.Score;
                Matches.Add(match);
            }
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
        /// <summary>
        /// Gets the placement of the sequences associated with this template.
        /// </summary>
        /// <returns>A list with tuples for each position in the original sequence. The first item is an array of tuples with all sequences on this position (matchindex) and the position on this sequence + 1 (or -1 if there is a gap, so 0 if outside bounds). The second item is an array of all gaps after this position, containing both the matchindex and sequence. </returns>
        public List<((int MatchIndex, int SequencePosition)[] Sequences, (int MatchIndex, IGap Gap)[] Gaps)> AlignedSequences()
        {
            var output = new List<((int MatchIndex, int SequencePosition)[] Sequences, (int MatchIndex, IGap Gap)[] Gaps)>()
            {
                Capacity = Sequence.Length
            };
            //Console.WriteLine($"The total sequence is {Sequence.Length} aa");

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new (int, int)[Matches.Count()], new (int, IGap)[Matches.Count()]));
            }

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                var match = Matches[matchindex];
                // Start at StartTemplatePosition and StartQueryPosition
                var template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                //Console.WriteLine($"This match is {match.TotalMatches()} matches long and {match.Sequence.Count()} aa {match}");

                foreach (var piece in match.Alignment)
                {
                    //Console.WriteLine($"at pos {template_pos}:{seq_pos}");
                    if (piece is SequenceMatch.Match m)
                    {
                        if (seq_pos == -1) seq_pos = match.StartQueryPosition;
                        //Console.WriteLine($"Found a match of {m.count} aa");
                        for (int i = 0; i < m.count && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            // Add this ID to the list
                            output[template_pos].Sequences[matchindex] = (matchindex, seq_pos + 1);
                            output[template_pos].Gaps[matchindex] = (matchindex, new None());

                            template_pos++;
                            seq_pos++;
                        }
                    }
                    else if (piece is SequenceMatch.GapTemplate gt)
                    {
                        if (template_pos < output.Count())
                        {
                            // Try to add this sequence or update the count
                            int len = Math.Min(gt.count, match.QuerySequence.Length - seq_pos);
                            IGap sub_seq;
                            if (len <= 0)
                            {
                                sub_seq = new None();
                            }
                            else
                            {
                                sub_seq = new Gap(match.QuerySequence.SubArray(seq_pos - 1, len));
                            }
                            seq_pos += gt.count;
                            output[template_pos].Gaps[matchindex] = (matchindex, sub_seq);
                        }
                    }
                    else if (piece is SequenceMatch.GapContig gc)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gc.count && template_pos < output.Count(); i++)
                        {
                            output[template_pos].Sequences[matchindex] = (matchindex, -1);
                            output[template_pos].Gaps[matchindex] = (matchindex, new None());
                            template_pos++;
                        }
                    }
                }
            }
            return output;
        }
        /// <summary>
        /// Returns the combined sequence or aminoacid variety per position in the alignment.
        /// </summary>
        /// <returns>A list of tuples. The first item is a dictionary with the aminoacid variance for this position, with counts. The second item contains a dictionary with the gap variety, with counts.</returns>
        public List<(Dictionary<AminoAcid, int>, Dictionary<IGap, int>)> CombinedSequence()
        {
            var output = new List<(Dictionary<AminoAcid, int>, Dictionary<IGap, int>)>()
            {
                Capacity = Sequence.Length
            };

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new Dictionary<AminoAcid, int>(), new Dictionary<IGap, int>()));
            }

            var alignedSequences = AlignedSequences();

            for (int i = 0; i < Sequence.Length; i++)
            {
                // Create the aminoacid dictionary
                foreach (var option in alignedSequences[i].Sequences)
                {
                    if (option.SequencePosition != 0)
                    {
                        AminoAcid aa;
                        int score;
                        if (option.SequencePosition == -1)
                        {
                            score = 1; //TODO: figure out the best score for a gap in a path
                            aa = new AminoAcid(Alphabet, Alphabet.alphabet[Alphabet.GapIndex]);
                        }
                        else
                        {
                            score = Matches[option.MatchIndex].Path == null ? 1 : Matches[option.MatchIndex].Path.DepthOfCoverage[option.SequencePosition - 1];
                            aa = Matches[option.MatchIndex].QuerySequence[option.SequencePosition - 1];
                        }

                        if (output[i].Item1.ContainsKey(aa))
                        {
                            output[i].Item1[aa] += score;
                        }
                        else
                        {
                            output[i].Item1.Add(aa, score);
                        }
                    }
                }
                // Create the gap dictionary
                foreach (var option in alignedSequences[i].Gaps)
                {
                    IGap key;
                    if (option.Gap == null)
                    {
                        key = new None();
                    }
                    else
                    {
                        key = option.Gap;
                    }
                    if (output[i].Item2.ContainsKey(key))
                    {
                        output[i].Item2[key] += 1;
                    }
                    else
                    {
                        output[i].Item2.Add(key, 1);
                    }
                }
            }

            return output;
        }
    }
    public class TemplateLocation
    {
        public readonly int TemplateDatabaseIndex;
        public readonly int TemplateIndex;
        public TemplateLocation(int templateDatabaseIndex, int templateIndex)
        {
            TemplateDatabaseIndex = templateDatabaseIndex;
            TemplateIndex = templateIndex;
        }
    }
    public class RecombinedTemplateLocation : TemplateLocation
    {
        public RecombinedTemplateLocation(int templateIndex) : base(-1, templateIndex) { }
    }
}