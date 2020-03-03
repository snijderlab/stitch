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
    /// <summary>
    /// Saves a template and its alignment with the given matches.
    /// </summary>
    public class Template
    {
        /// <summary>
        /// The name of the containing TemplateDatabase. <see cref="TemplateDatabase.Name"/>
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The sequence of this template
        /// </summary>
        public readonly AminoAcid[] Sequence;

        /// <summary>
        /// Metadata for this template
        /// </summary>
        public readonly MetaData.IMetaData MetaData;

        /// <summary>
        /// The score for this template
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// The list of matches on this template
        /// </summary>
        public List<SequenceMatch> Matches;

        /// <summary>
        /// The alphabet of this template. TODO is this still needed?
        /// </summary>
        public readonly Alphabet Alphabet;

        /// <summary>
        /// The cutoff score to filter matches to be added to this template
        /// </summary>
        readonly double cutoffScore;

        /// <summary>
        /// If this template is recombinated this are the templates it consists of.
        /// </summary>
        public readonly List<Template> Recombination;

        /// <summary>
        /// The location this template resides (index in the containing TemplateDatabase and its location)
        /// </summary>
        public readonly TemplateLocation Location;

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the enclosing TemplateDatabase, <see cref="Name"/>.</param>
        /// <param name="seq">The sequence, <see cref="Sequence"/>.</param>
        /// <param name="meta">The metadata, <see cref="MetaData"/>.</param>
        /// <param name="alphabet">The alphabet, <see cref="Alphabet"/>.</param>
        /// <param name="_cutoffScore">The cutoffScore, <see cref="cutoffScore"/>.</param>
        /// <param name="location">The location, <see cref="Location"/>.</param>
        /// <param name="recombination">The recombination, if recombined otherwise null, <see cref="Recombination"/>.</param>
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

        /// <summary>
        /// Adds a new match to the list of matches, if the score is above the cutoff
        /// </summary>
        /// <param name="match">The match to add</param>
        public void AddMatch(SequenceMatch match)
        {
            lock (Matches)
            {
                if (match != null)
                {
                    if (match.Score >= cutoffScore * Math.Sqrt(match.QuerySequence.Length))
                    {
                        Score += match.Score;
                        Matches.Add(match);
                        if (Matches.Count() > 1)
                            Matches.Sort((a, b) => b.TotalMatches.CompareTo(a.TotalMatches)); // So the longest match will be at the top
                    }
                }
            }
        }
        /// <summary>
        /// Contains possibilities for a gap.
        /// </summary>
        public interface IGap { }

        /// <summary>
        /// No gap
        /// </summary>
        public struct None : IGap
        {
            public override string ToString() { return ""; }
        }

        /// <summary>
        /// A gap
        /// </summary>
        public struct Gap : IGap
        {
            /// <summary>
            /// The sequence of this gap
            /// </summary>
            public readonly AminoAcid[] Sequence;

            /// <summary>
            /// Creates a new Gap
            /// </summary>
            /// <param name="sequence">The sequence of this gap, <see cref="Sequence"/>.</param>
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
        /// <returns>A list with tuples for each position in the original sequence. </returns>
        public List<((int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, int[] CoverageDepth, int ContigID)[] Gaps)> AlignedSequences()
        {
            var output = new List<((int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, int[] CoverageDepth, int ContigID)[] Gaps)>()
            {
                Capacity = Sequence.Length
            };

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new (int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[Matches.Count()], new (int, IGap, int[], int)[Matches.Count()]));
            }

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                var match = Matches[matchindex];
                // Start at StartTemplatePosition and StartQueryPosition
                var template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;

                foreach (var piece in match.Alignment)
                {
                    if (piece is SequenceMatch.Match m)
                    {
                        if (seq_pos == -1) seq_pos = match.StartQueryPosition;
                        for (int i = 0; i < m.Length && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            // Add this ID to the list
                            output[template_pos].Sequences[matchindex] = (matchindex, seq_pos + 1, match.Path.DepthOfCoverage[seq_pos], match.Path.ContigID[seq_pos]);
                            output[template_pos].Gaps[matchindex] = (matchindex, new None(), new int[0], match.Path.ContigID[seq_pos]);

                            template_pos++;
                            seq_pos++;
                        }
                    }
                    else if (piece is SequenceMatch.GapQuery gq)
                    {
                        if (template_pos < output.Count())
                        {
                            // Try to add this sequence or update the count
                            int len = Math.Min(gq.Length, match.QuerySequence.Length - seq_pos);
                            IGap sub_seq;
                            int[] cov;
                            if (len <= 0)
                            {
                                sub_seq = new None();
                                cov = new int[0];
                            }
                            else
                            {
                                sub_seq = new Gap(match.QuerySequence.SubArray(seq_pos - 1, len));
                                cov = match.Path.DepthOfCoverage.SubArray(seq_pos - 1, len);
                            }
                            seq_pos += gq.Length;
                            output[template_pos].Gaps[matchindex] = (matchindex, sub_seq, cov, match.Path.ContigID[seq_pos - 1]);
                        }
                    }
                    else if (piece is SequenceMatch.GapTemplate gt)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gt.Length && template_pos < output.Count(); i++)
                        {
                            output[template_pos].Sequences[matchindex] = (matchindex, -1, 1, -1); //TODO: figure out the best score for a gap in a path
                            output[template_pos].Gaps[matchindex] = (matchindex, new None(), new int[0], -1);
                            template_pos++;
                        }
                    }
                }
            }
            // TODO the list should be squatted/compressed to use the minimal amount of lines

            // Filter DOC for duplicate entries
            foreach (var (Sequences, Gaps) in output)
            {
                // Filter aminoAcids
                for (int outer = 0; outer < Sequences.Length; outer++)
                {
                    if (Sequences[outer].CoverageDepth == 0) continue;

                    for (int inner = 0; inner < Sequences.Length; inner++)
                    {
                        if (inner == outer) continue;
                        if (Sequences[outer].ContigID == Sequences[inner].ContigID && Sequences[outer].SequencePosition == Sequences[inner].SequencePosition)
                            Sequences[inner].CoverageDepth = 0;
                    }
                }

                // Filter Gaps
                for (int outer = 0; outer < Gaps.Length; outer++)
                {
                    if (Gaps[outer].CoverageDepth == null || Gaps[outer].CoverageDepth == new int[Gaps[outer].Gap.ToString().Length]) continue;

                    for (int inner = 0; inner < Gaps.Length; inner++)
                    {
                        if (inner == outer) continue;
                        if (Gaps[outer].ContigID == Gaps[inner].ContigID && Gaps[outer].CoverageDepth == Gaps[inner].CoverageDepth)
                            Gaps[inner].CoverageDepth = new int[Gaps[inner].Gap.ToString().Length];
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Returns the combined sequence or aminoacid variety per position in the alignment.
        /// </summary>
        /// <returns>A list of tuples. The first item is a dictionary with the aminoacid variance for this position, with counts. The second item contains a dictionary with the gap variety, with counts.</returns>
        public List<(AminoAcid Template, Dictionary<AminoAcid, int> AminoAcids, Dictionary<IGap, (int Count, int[] CoverageDepth)> Gaps)> CombinedSequence()
        {
            var output = new List<(AminoAcid Template, Dictionary<AminoAcid, int> AminoAcids, Dictionary<IGap, (int Count, int[] CoverageDepth)> Gaps)>()
            {
                Capacity = Sequence.Length
            };

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((Sequence[i], new Dictionary<AminoAcid, int>(), new Dictionary<IGap, (int, int[])>()));
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
                        if (option.SequencePosition == -1)
                        {
                            aa = new AminoAcid(Alphabet, Alphabet.GapChar);
                        }
                        else
                        {
                            aa = Matches[option.MatchIndex].QuerySequence[option.SequencePosition - 1];
                        }

                        if (output[i].AminoAcids.ContainsKey(aa))
                        {
                            output[i].AminoAcids[aa] += option.CoverageDepth;
                        }
                        else
                        {
                            output[i].AminoAcids.Add(aa, option.CoverageDepth);
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
                    if (output[i].Gaps.ContainsKey(key))
                    {
                        int[] cov;
                        if (output[i].Gaps[key].CoverageDepth == null)
                        {
                            cov = option.CoverageDepth;
                        }
                        else
                        {
                            cov = output[i].Gaps[key].CoverageDepth.ElementwiseAdd(option.CoverageDepth);
                        }
                        output[i].Gaps[key] = (output[i].Gaps[key].Count + 1, cov);
                    }
                    else
                    {
                        output[i].Gaps.Add(key, (1, option.CoverageDepth));
                    }
                }
            }

            return output;
        }
    }

    /// <summary>
    /// The location of a template, in its TemplateDatabase and its location
    /// </summary>
    public class TemplateLocation
    {
        /// <summary>
        /// The location of the <see cref="TemplateDatabase"/>, see <see cref="TemplateDatabase.Index"/>.
        /// </summary>
        public readonly int TemplateDatabaseIndex;

        /// <summary>
        /// The index of the <see cref="Template"/>, defined as the index in the containing TemplateDatabase list of Templates, see <see cref="TemplateDatabase.Templates"/>.
        /// </summary>
        public readonly int TemplateIndex;

        /// <summary>
        /// Creates a new TemplateLocation
        /// </summary>
        /// <param name="templateDatabaseIndex"> The TemplateDatabase index, see <see cref="TemplateDatabaseIndex"/>.</param>
        /// <param name="templateIndex"> The Template index, see <see cref="TemplateIndex"/>.</param>
        public TemplateLocation(int templateDatabaseIndex, int templateIndex)
        {
            TemplateDatabaseIndex = templateDatabaseIndex;
            TemplateIndex = templateIndex;
        }
    }

    /// <summary>
    /// The location of a recombined template, as there is only one list of recombined templates only one index has to be saved.
    /// </summary>
    public class RecombinedTemplateLocation : TemplateLocation
    {
        public RecombinedTemplateLocation(int templateIndex) : base(-1, templateIndex) { }
    }
}