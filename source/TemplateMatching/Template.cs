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
        /// The name of the containing Segment. <see cref="Segment.Name"/>
        /// </summary>
        public readonly string Name;

        public string Class
        {
            get
            {
                return MetaData.ClassIdentifier;
            }
        }

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
        /// To signify if this template was used in a match run which used EnforceUnique.
        /// </summary>
        public bool ForcedOnSingleTemplate;
        /// <summary>
        /// To signify if the germline Isoleucines should be copied to the consensus sequence.
        /// </summary>
        public bool ForceGermlineIsoleucine;

        /// <summary>
        /// The unique score for this template
        /// </summary>
        public int UniqueScore
        {
            get
            {
                if (Parent.Scoring == RunParameters.ScoringParameter.Absolute) return uniqueScore;
                else return (int)Math.Round((double)uniqueScore / Sequence.Length);
            }
        }
        int uniqueScore;

        public double TotalArea = 0;
        public double TotalUniqueArea = 0;
        public int UniqueMatches = 0;

        /// <summary>
        /// The list of matches on this template
        /// </summary>
        public List<SequenceMatch> Matches;

        /// <summary>
        /// If this template is recombinated these are the templates it consists of.
        /// </summary>
        public readonly List<Template> Recombination;

        /// <summary>
        /// The location this template resides (index in the containing Segment and its location)
        /// </summary>
        public readonly TemplateLocation Location;

        /// <summary>
        /// The parent segment, needed to get the settings for scoring, alphabet etc
        /// </summary>
        public readonly Segment Parent;

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the enclosing Segment, <see cref="Name"/>.</param>
        /// <param name="seq">The sequence, <see cref="Sequence"/>.</param>
        /// <param name="meta">The metadata, <see cref="MetaData"/>.</param>
        /// <param name="alphabet">The alphabet, <see cref="Alphabet"/>.</param>
        /// <param name="location">The location, <see cref="Location"/>.</param>
        /// <param name="recombination">The recombination, if recombined otherwise null, <see cref="Recombination"/>.</param>
        public Template(string name, AminoAcid[] seq, MetaData.IMetaData meta, Segment parent, bool forceGermlineIsoleucine, TemplateLocation location = null, List<Template> recombination = null)
        {
            Name = name;
            Sequence = seq;
            MetaData = meta;
            score = 0;
            Matches = new List<SequenceMatch>();
            Recombination = recombination;
            Location = location;
            Parent = parent;
            ForceGermlineIsoleucine = forceGermlineIsoleucine;
        }

        /// <summary>
        /// Adds a new match to the list of matches, if the score is above the cutoff
        /// </summary>
        /// <param name="match">The match to add</param>
        /// <param name="unique">To signify if this read is only placed here (EnforceUnique) or that it is a normal placement.</param>
        public void AddMatch(SequenceMatch match, bool unique = false)
        {
            lock (Matches)
            {
                if (match.Score >= Parent.CutoffScore * Math.Sqrt(match.QuerySequence.Length) && !match.AllGap())
                {
                    score += match.Score;
                    TotalArea += match.MetaData.TotalArea;
                    Matches.Add(match);

                    if (unique)
                    {
                        this.ForcedOnSingleTemplate = true;
                        match.Unique = true;
                        uniqueScore += match.Score;
                        TotalUniqueArea += match.MetaData.TotalArea;
                        UniqueMatches++;
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
            public static bool operator ==(None a, object obj) { return a.Equals(obj); }
            public static bool operator !=(None a, object obj) { return !a.Equals(obj); }
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
                // TODO: Find out if a full sequence based hash is necessary
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
            public static bool operator ==(Gap a, object obj) { return a.Equals(obj); }
            public static bool operator !=(Gap a, object obj) { return !a.Equals(obj); }
        }

        /// <summary>
        /// Gets the placement of the sequences associated with this template.
        /// </summary>
        /// <returns>A list with tuples for each position in the original sequence. </returns>
        private List<((int MatchIndex, int SequencePosition, double CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, double[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)> alignedSequencesCache = null;
        public List<((int MatchIndex, int SequencePosition, double CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, double[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)> AlignedSequences()
        {
            if (alignedSequencesCache != null) return alignedSequencesCache;

            Matches.Sort((a, b) => b.TotalMatches.CompareTo(a.TotalMatches)); // So the longest match will be at the top

            var output = new List<((int MatchIndex, int SequencePosition, double CoverageDepth, int ContigID)[] Sequences, (int MatchIndex, IGap Gap, double[] CoverageDepth, int ContigID, bool InSequence)[] Gaps)>(Sequence.Length);

            // Get levels (compress into somewhat lower amount of lines)
            var levels = new List<List<(int Start, int Length)>>(Matches.Count);
            var level_lookup = new int[Matches.Count];
            int start, length;
            bool placed, could_be_placed;

            for (int matchindex = 0; matchindex < Matches.Count; matchindex++)
            {
                start = Matches[matchindex].StartTemplatePosition;
                length = Matches[matchindex].LengthOnTemplate;
                placed = false;

                for (int l = 0; l < levels.Count; l++)
                {
                    could_be_placed = true;
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
                    level_lookup[matchindex] = levels.Count - 1;
                }
            }

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new (int MatchIndex, int SequencePosition, double CoverageDepth, int ContigID)[levels.Count], new (int, IGap, double[], int, bool)[levels.Count]));
                for (int j = 0; j < levels.Count; j++)
                {
                    output[i].Sequences[j] = (-2, 0, 0, -1);
                    output[i].Gaps[j] = (-2, new None(), new double[0], -1, false);
                }
            }

            SequenceMatch match;
            SequenceMatch.MatchPiece piece;

            for (int matchindex = 0; matchindex < Matches.Count; matchindex++)
            {
                match = Matches[matchindex];
                // Start at StartTemplatePosition and StartQueryPosition
                int template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                bool gap = false;
                int level = level_lookup[matchindex];

                for (int pieceindex = 0; pieceindex < match.Alignment.Count; pieceindex++)
                {
                    piece = match.Alignment[pieceindex];
                    var inseq = pieceindex < match.Alignment.Count - 1 && pieceindex > 0;

                    if (piece is SequenceMatch.Match m)
                    {
                        for (int i = 0; i < m.Length && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            var contigid = match.Index;
                            // Add this ID to the list
                            var in_sequence = inseq // In the middle of the pieces
                                           || (i < m.Length - 1) // Not the last AA
                                           || (pieceindex < match.Alignment.Count - 1 && i == m.Length - 1); // With a piece after this one the last AA is in the sequence

                            output[template_pos].Sequences[level] = (matchindex, seq_pos + 1, match.MetaData.PositionalScore.Length > 0 ? match.MetaData.PositionalScore[seq_pos] : 1.0, contigid);
                            if (!gap) output[template_pos].Gaps[level] = (matchindex, new None(), new double[0], contigid, in_sequence);

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
                        double[] cov = match.MetaData.PositionalScore.SubArray(seq_pos, len);

                        var contigid = match.Index;

                        seq_pos += len;
                        int pos = Math.Max(Math.Min(template_pos - 1, output.Count - 1), 0);
                        output[pos].Gaps[level] = (matchindex, sub_seq, cov, contigid, inseq);
                    }
                    else if (piece is SequenceMatch.GapInTemplate gt)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gt.Length && template_pos < output.Count; i++)
                        {
                            output[template_pos].Sequences[level] = (matchindex, -1, 1, -1); //TODO: figure out the best score for a gap in a path
                            output[template_pos].Gaps[level] = (matchindex, new None(), new double[0], -1, true);
                            template_pos++;
                        }
                        gap = false;
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
        private List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> combinedSequenceCache = null;
        public List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> CombinedSequence()
        {
            if (combinedSequenceCache != null) return combinedSequenceCache;

            var output = new List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)>()
            {
                Capacity = Sequence.Length
            };

            var alignedSequences = AlignedSequences();
            AminoAcid aa;
            IGap key;

            for (int i = 0; i < Sequence.Length; i++)
            {
                (AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps) position = (Sequence[i], new Dictionary<AminoAcid, double>(), new Dictionary<IGap, (int, double[])>());
                // Create the aminoacid dictionary
                foreach (var option in alignedSequences[i].Sequences)
                {
                    if (option.SequencePosition != 0)
                    {
                        if (option.SequencePosition == -1) // If there is a deletion in the placed read in respect to the template
                            aa = new AminoAcid(Parent.Alphabet, Alphabet.GapChar);
                        else
                            aa = Matches[option.MatchIndex].QuerySequence[option.SequencePosition - 1];

                        if (position.AminoAcids.ContainsKey(aa))
                            position.AminoAcids[aa] += option.CoverageDepth;
                        else
                            position.AminoAcids.Add(aa, option.CoverageDepth);
                    }
                }

                // Create the gap dictionary
                foreach (var option in alignedSequences[i].Gaps)
                {
                    if (option.Gap == null || !option.InSequence) continue;

                    if (option.Gap == (IGap)new None())
                    {
                        if (position.Gaps.ContainsKey(new None()))
                            position.Gaps[new None()] = (position.Gaps[new None()].Count + 1, new double[0]);
                        else
                            position.Gaps.Add(new None(), (1, new double[0]));
                    }
                    else
                    {
                        key = option.Gap;

                        if (position.Gaps.ContainsKey(key))
                        {
                            double[] cov;
                            if (position.Gaps[key].CoverageDepth == null)
                                cov = option.CoverageDepth;
                            else
                                cov = position.Gaps[key].CoverageDepth.ElementwiseAdd(option.CoverageDepth);

                            position.Gaps[key] = (position.Gaps[key].Count + 1, cov);
                        }
                        else
                        {
                            position.Gaps.Add(key, (1, option.CoverageDepth));
                        }
                    }
                }
                output.Add(position);
            }
            combinedSequenceCache = output;
            return output;
        }
        (List<AminoAcid>, List<double>) ConsensusSequenceCache = (null, null);
        public (List<AminoAcid>, List<double>) ConsensusSequence()
        {
            if (ConsensusSequenceCache != (null, null)) return ConsensusSequenceCache;

            var consensus = new List<AminoAcid>();
            var doc = new List<double>();
            var combinedSequence = CombinedSequence();
            List<AminoAcid> options = new List<AminoAcid>();
            double max;
            double coverage;
            List<Template.IGap> max_gap = new List<IGap>();

            for (int i = 0; i < combinedSequence.Count; i++)
            {
                // Get the highest chars
                options.Clear();
                max = 0;
                coverage = 0;

                foreach (var item in combinedSequence[i].AminoAcids)
                {
                    coverage += item.Value;
                    if (item.Value > max)
                    {
                        options.Clear();
                        options.Add(item.Key);
                        max = item.Value;
                    }
                    else if (item.Value == max)
                    {
                        options.Add(item.Key);
                    }
                }

                if (options.Count == 1 && options[0].Character == Alphabet.GapChar)
                {
                    // Do not add gaps, as those are not part of the final sequence
                }
                else if (options.Count > 1 && options.Contains(combinedSequence[i].Template))
                {
                    consensus.Add(combinedSequence[i].Template);
                    doc.Add(coverage);
                }
                else if (ForceGermlineIsoleucine && options.Count > 0 && options[0].Character == 'L' && combinedSequence[i].Template.Character == 'I')
                {
                    consensus.Add(combinedSequence[i].Template);
                    doc.Add(coverage);
                }
                else if (options.Count > 0)
                {
                    consensus.Add(options[0]);
                    doc.Add(coverage);
                }
                else
                {
                    // There is no data from reads so take the template sequence
                    consensus.Add(combinedSequence[i].Template);
                    doc.Add(coverage);
                }

                // Get the highest gap
                max_gap.Clear();
                max = 0;
                var gap_coverage = new double[0];

                foreach (var item in combinedSequence[i].Gaps)
                {
                    if (item.Value.Count > max)
                    {
                        max_gap.Clear();
                        max_gap.Add(item.Key);
                        max = item.Value.Count;
                        gap_coverage = item.Value.CoverageDepth;
                    }
                    else if (item.Value.Count == max)
                    {
                        max_gap.Add(item.Key);
                    }
                }

                if (max_gap.Count >= 1 && max_gap[0].GetType() != typeof(Template.None))
                {
                    consensus.AddRange(((Template.Gap)max_gap[0]).Sequence);
                    doc.AddRange(gap_coverage);
                }
            }
            ConsensusSequenceCache = (consensus, doc);
            return ConsensusSequenceCache;
        }

        /// <summary>
        /// Align the consensus sequence of this Template to its original sequence, in the case of a recombined sequence align with the original sequences of its templates.
        /// </summary>
        /// <returns>The sequencematch containing the result</returns>
        public SequenceMatch AlignConsensusWithTemplate()
        {
            if (Recombination != null)
                return HelperFunctionality.SmithWaterman(this.Recombination.SelectMany(a => a.Sequence).ToArray(), this.ConsensusSequence().Item1.ToArray(), Parent.Alphabet);
            else
                return HelperFunctionality.SmithWaterman(this.Sequence, this.ConsensusSequence().Item1.ToArray(), Parent.Alphabet);
        }
    }

    /// <summary>
    /// The location of a template, in its Segment and its location
    /// </summary>
    public class TemplateLocation
    {
        /// <summary>
        /// The location of the <see cref="Segment"/>, see <see cref="Segment.Index"/>.
        /// </summary>
        public readonly int SegmentIndex;

        /// <summary>
        /// The index of the <see cref="Template"/>, defined as the index in the containing Segment list of Templates, see <see cref="Segment.Templates"/>.
        /// </summary>
        public readonly int TemplateIndex;

        /// <summary>
        /// Creates a new TemplateLocation
        /// </summary>
        /// <param name="segmentIndex"> The Segment index, see <see cref="SegmentIndex"/>.</param>
        /// <param name="templateIndex"> The Template index, see <see cref="TemplateIndex"/>.</param>
        public TemplateLocation(int segmentIndex, int templateIndex)
        {
            SegmentIndex = segmentIndex;
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