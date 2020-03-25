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
        public int Score
        {
            get
            {
                if (Parent.Scoring == RunParameters.ScoringParameter.Absolute) return score;
                else return (int)Math.Round((double)score / Sequence.Length);
            }
        }
        int score;

        /// <summary>
        /// The list of matches on this template
        /// </summary>
        public List<SequenceMatch> Matches;

        /// <summary>
        /// If this template is recombinated this are the templates it consists of.
        /// </summary>
        public readonly List<Template> Recombination;

        /// <summary>
        /// The location this template resides (index in the containing TemplateDatabase and its location)
        /// </summary>
        public readonly TemplateLocation Location;

        /// <summary>
        /// The parent database, needed to get the settings for scoring, alphabet etc
        /// </summary>
        private readonly TemplateDatabase Parent;

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the enclosing TemplateDatabase, <see cref="Name"/>.</param>
        /// <param name="seq">The sequence, <see cref="Sequence"/>.</param>
        /// <param name="meta">The metadata, <see cref="MetaData"/>.</param>
        /// <param name="alphabet">The alphabet, <see cref="Alphabet"/>.</param>
        /// <param name="location">The location, <see cref="Location"/>.</param>
        /// <param name="recombination">The recombination, if recombined otherwise null, <see cref="Recombination"/>.</param>
        public Template(string name, AminoAcid[] seq, MetaData.IMetaData meta, TemplateDatabase parent, TemplateLocation location = null, List<Template> recombination = null)
        {
            Name = name;
            Sequence = seq;
            MetaData = meta;
            score = 0;
            Matches = new List<SequenceMatch>();
            Recombination = recombination;
            Location = location;
            Parent = parent;
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
                    if (match.Score >= Parent.CutoffScore * Math.Sqrt(match.QuerySequence.Length))
                    {
                        score += match.Score;
                        Matches.Add(match);
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
            public override int GetHashCode() { return 397; }
            public override bool Equals(object obj) { return obj is None; }
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
            int hashCode;

            /// <summary>
            /// Creates a new Gap
            /// </summary>
            /// <param name="sequence">The sequence of this gap, <see cref="Sequence"/>.</param>
            public Gap(AminoAcid[] sequence)
            {
                Sequence = sequence;

                // Precomputes a hashcode based on the actual sequence of the gap
                int hash = 1217;
                int pos = 0;
                for (int i = 0; i < 5; i++)
                {
                    hash ^= Sequence[pos].GetHashCode();
                    hash += 11;
                    pos = ((pos + i) * 653) % Sequence.Length;
                }
                hashCode = hash;
            }

            public override string ToString()
            {
                return AminoAcid.ArrayToString(Sequence);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            // Equality is defined by the equality of the sequences
            public override bool Equals(object obj)
            {
                if (obj is Gap aa && this.Sequence.Length == aa.Sequence.Length)
                {
                    return AminoAcid.ArrayEquals(this.Sequence, aa.Sequence);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the placement of the sequences associated with this template.
        /// </summary>
        /// <returns>A list with tuples for each position in the original sequence. </returns>
        private List<((int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, int[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)> alignedSequencesCache = null;
        public List<((int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, int[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)> AlignedSequences()
        {
            if (alignedSequencesCache != null) return alignedSequencesCache;

            Matches.Sort((a, b) => b.TotalMatches.CompareTo(a.TotalMatches)); // So the longest match will be at the top

            var output = new List<((int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, int[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)>()
            {
                Capacity = Sequence.Length
            };

            // Get levels (compress into somewhat lower amount of lines)
            var levels = new List<List<(int Start, int Length)>>();
            var level_lookup = new int[Matches.Count()];

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                var start = Matches[matchindex].StartTemplatePosition;
                var length = Matches[matchindex].LengthOnTemplate;
                bool placed = false;

                for (int l = 0; l < levels.Count(); l++)
                {
                    bool could_be_placed = true;
                    // Try to determine if it clashes
                    foreach ((var str, var len) in levels[l])
                    {
                        if ((start < str && start + length + 1 > str) || (start < str + len + 1 && start + length + 1 > str))
                        {
                            could_be_placed = false;
                            break;
                        }
                    }

                    if (could_be_placed)
                    {
                        levels[l].Add((start, length));
                        level_lookup[matchindex] = l;
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    levels.Add(new List<(int Start, int Length)> { (start, length) });
                    level_lookup[matchindex] = levels.Count() - 1;
                }
            }

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new (int MatchIndex, int SequencePosition, int CoverageDepth, int ContigID)[levels.Count()], new (int, IGap, int[], int, bool)[levels.Count()]));
                for (int j = 0; j < levels.Count(); j++)
                {
                    output[i].Sequences[j] = (-2, 0, 0, -1);
                    output[i].Gaps[j] = (-2, new None(), new int[0], -1, false);
                }
            }

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                var match = Matches[matchindex];
                // Start at StartTemplatePosition and StartQueryPosition
                var template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                bool gap = false;
                int level = level_lookup[matchindex];

                for (int pieceindex = 0; pieceindex < match.Alignment.Count(); pieceindex++)
                {
                    var piece = match.Alignment[pieceindex];
                    var inseq = pieceindex < match.Alignment.Count() - 1 && pieceindex > 0 ? true : false;

                    if (piece is SequenceMatch.Match m)
                    {
                        for (int i = 0; i < m.Length && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            // Add this ID to the list
                            var in_sequence = inseq // In the middle of the pieces
                                           || (i < m.Length - 1) // Not the last AA
                                           || (pieceindex < match.Alignment.Count() - 1 && i == m.Length - 1); // With a piece after this one the last AA is in the sequence

                            output[template_pos].Sequences[level] = (matchindex, seq_pos + 1, match.Path.DepthOfCoverage[seq_pos], match.Path.ContigID[seq_pos]);
                            if (!gap) output[template_pos].Gaps[level] = (matchindex, new None(), new int[0], match.Path.ContigID[seq_pos], in_sequence);

                            template_pos++;
                            seq_pos++;
                            gap = false;
                        }
                    }
                    else if (piece is SequenceMatch.GapInQuery gc)
                    {
                        // Try to add this sequence or update the count
                        gap = true;
                        int len = Math.Min(gc.Length, match.QuerySequence.Length - seq_pos - 1);

                        IGap sub_seq = new Gap(match.QuerySequence.SubArray(seq_pos, len));
                        int[] cov = match.Path.DepthOfCoverage.SubArray(seq_pos, len);

                        seq_pos += len;
                        output[Math.Max(0, template_pos - 1)].Gaps[level] = (matchindex, sub_seq, cov, match.Path.ContigID[seq_pos - 1], inseq);
                    }
                    else if (piece is SequenceMatch.GapInTemplate gt)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gt.Length && template_pos < output.Count(); i++)
                        {
                            output[template_pos].Sequences[level] = (matchindex, -1, 1, -1); //TODO: figure out the best score for a gap in a path
                            output[template_pos].Gaps[level] = (matchindex, new None(), new int[0], -1, true);
                            template_pos++;
                        }
                        gap = false;
                    }
                }
            }

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
                    if (Gaps[outer].CoverageDepth == null) continue;

                    for (int inner = 0; inner < Gaps.Length; inner++)
                    {
                        if (inner == outer) continue;
                        if (Gaps[outer].ContigID == Gaps[inner].ContigID && Gaps[outer].Gap == Gaps[inner].Gap)
                            Gaps[inner].CoverageDepth = null;
                    }
                }
            }
            alignedSequencesCache = output;
            return output;
        }

        /// <summary>
        /// Returns the combined sequence or aminoacid variety per position in the alignment.
        /// </summary>
        /// <returns>A list of tuples. The first item is a dictionary with the aminoacid variance for this position, with counts. The second item contains a dictionary with the gap variety, with counts.</returns>
        private List<(AminoAcid Template, Dictionary<AminoAcid, int> AminoAcids, Dictionary<IGap, (int Count, int[] CoverageDepth)> Gaps)> combinedSequenceCache = null;
        public List<(AminoAcid Template, Dictionary<AminoAcid, int> AminoAcids, Dictionary<IGap, (int Count, int[] CoverageDepth)> Gaps)> CombinedSequence()
        {
            if (combinedSequenceCache != null) return combinedSequenceCache;

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
                            aa = new AminoAcid(Parent.Alphabet, Alphabet.GapChar);
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
                    if (option.Gap == null || !option.InSequence) continue;

                    if (option.Gap == (IGap)new None())
                    {
                        if (output[i].Gaps.ContainsKey(new None()))
                        {
                            output[i].Gaps[new None()] = (output[i].Gaps[new None()].Count + 1, new int[0]);
                        }
                        else
                        {
                            output[i].Gaps.Add(new None(), (1, new int[0]));
                        }
                    }
                    else
                    {
                        key = option.Gap;

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
            }
            combinedSequenceCache = output;
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