using System;
using System.Collections.Generic;
using System.Linq;
using static Stitch.HelperFunctionality;
using System.Text.Json.Serialization;


namespace Stitch
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
        public readonly ReadMetaData.IMetaData MetaData;

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

        [JsonIgnore]
        /// <summary>
        /// The parent segment, needed to get the settings for scoring, alphabet etc
        /// </summary>
        public readonly Segment Parent;

        /// <summary>
        /// To keep track on how this template was segment joined, this number is the number of characters to remove from the front after all 'X's have been trimmed.
        /// Only makes sense in recombined templates.
        /// </summary>
        public int Overlap = 0;

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the enclosing Segment, <see cref="Name"/>.</param>
        /// <param name="seq">The sequence, <see cref="Sequence"/>.</param>
        /// <param name="meta">The metadata, <see cref="MetaData"/>.</param>
        /// <param name="alphabet">The alphabet, <see cref="Alphabet"/>.</param>
        /// <param name="location">The location, <see cref="Location"/>.</param>
        /// <param name="recombination">The recombination, if recombined otherwise null, <see cref="Recombination"/>.</param>
        public Template(string name, AminoAcid[] seq, ReadMetaData.IMetaData meta, Segment parent, bool forceGermlineIsoleucine, TemplateLocation location = null, List<Template> recombination = null)
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

                // Pre computes a hash code based on the actual sequence of the gap
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

            for (int match_index = 0; match_index < Matches.Count; match_index++)
            {
                start = Matches[match_index].StartTemplatePosition;
                length = Matches[match_index].LengthOnTemplate;
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
                        level_lookup[match_index] = l;
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    levels.Add(new List<(int Start, int Length)> { (start, length) });
                    level_lookup[match_index] = levels.Count - 1;
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

            for (int match_index = 0; match_index < Matches.Count; match_index++)
            {
                match = Matches[match_index];
                // Start at StartTemplatePosition and StartQueryPosition
                int template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                bool gap = false;
                int level = level_lookup[match_index];

                for (int piece_index = 0; piece_index < match.Alignment.Count; piece_index++)
                {
                    piece = match.Alignment[piece_index];
                    var inseq = piece_index < match.Alignment.Count - 1 && piece_index > 0;

                    if (piece is SequenceMatch.Match m)
                    {
                        for (int i = 0; i < m.Length && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            var contigid = match.Index;
                            // Add this ID to the list
                            var in_sequence = inseq // In the middle of the pieces
                                           || (i < m.Length - 1) // Not the last AA
                                           || (piece_index < match.Alignment.Count - 1 && i == m.Length - 1); // With a piece after this one the last AA is in the sequence

                            output[template_pos].Sequences[level] = (match_index, seq_pos + 1, match.MetaData.PositionalScore.Length > 0 ? match.MetaData.PositionalScore[seq_pos] : 1.0, contigid);
                            if (!gap) output[template_pos].Gaps[level] = (match_index, new None(), new double[0], contigid, in_sequence);

                            template_pos++;
                            seq_pos++;
                            gap = false;
                        }
                    }
                    else if (piece is SequenceMatch.Insertion gc)
                    {
                        // Try to add this sequence or update the count
                        gap = true;
                        int len = Math.Min(gc.Length, match.QuerySequence.Length - seq_pos - 1);

                        IGap sub_seq = new Gap(match.QuerySequence.SubArray(seq_pos, len));
                        double[] cov = new double[len];
                        if (match.MetaData.PositionalScore.Length >= seq_pos + len)
                            cov = match.MetaData.PositionalScore.SubArray(seq_pos, len);

                        var contigid = match.Index;

                        seq_pos += len;
                        int pos = Math.Max(Math.Min(template_pos - 1, output.Count - 1), 0);
                        output[pos].Gaps[level] = (match_index, sub_seq, cov, contigid, inseq);
                    }
                    else if (piece is SequenceMatch.Deletion gt)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gt.Length && template_pos < output.Count; i++)
                        {
                            output[template_pos].Sequences[level] = (match_index, -1, 1, -1); //TODO: figure out the best score for a gap in a path
                            output[template_pos].Gaps[level] = (match_index, new None(), new double[0], -1, true);
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
        /// <returns>The sequence match containing the result</returns>
        public SequenceMatch AlignConsensusWithTemplate()
        {
            if (Recombination != null)
                return HelperFunctionality.SmithWaterman(this.Recombination.SelectMany(a => a.Sequence).ToArray(), this.ConsensusSequence().Item1.ToArray(), Parent.Alphabet);
            else
                return HelperFunctionality.SmithWaterman(this.Sequence, this.ConsensusSequence().Item1.ToArray(), Parent.Alphabet);
        }

        /// <summary>
        /// The annotated consensus sequence given as an array with the length of the consensus sequence.
        /// </summary>
        private Annotation[] ConsensusSequenceAnnotationCache = null;
        public Annotation[] ConsensusSequenceAnnotation()
        {
            if (ConsensusSequenceAnnotationCache != null) return ConsensusSequenceAnnotationCache;

            var match = this.AlignConsensusWithTemplate();
            var annotation = new List<Annotation>(match.LengthOnQuery);

            List<(Annotation, string)> annotated = null;
            if (this.Recombination != null)
            {
                annotated = this.Recombination.Aggregate(new List<(Annotation, string)>(), (acc, item) =>
            {
                var x_start = item.ConsensusSequence().Item1.TakeWhile(a => a.Character == 'X').Count();
                var main_sequence = item.ConsensusSequence().Item1.Skip(x_start).TakeWhile(a => a.Character != 'X').Count();
                var sequence = AminoAcid.ArrayToString(item.ConsensusSequence().Item1.Skip(x_start).Take(main_sequence).ToArray());
                acc.AddRange(item.ConsensusSequenceAnnotation().Skip(x_start).Take(main_sequence).Skip(item.Overlap).Zip(sequence).Select((a) => (a.First, a.Second.ToString())));
                return acc;
            });
            }
            else
            {
                if (this.MetaData is ReadMetaData.Fasta meta)
                    if (meta.AnnotatedSequence != null)
                        annotated = meta.AnnotatedSequence;
            }

            Annotation GetClasses(int position)
            {
                if (annotated == null) return Annotation.None;
                int pos = -1;
                for (int i = 0; i < annotated.Count; i++)
                {
                    pos += annotated[i].Item2.Length;
                    if (pos >= position + match.StartTemplatePosition)
                        return annotated[i].Item1;
                }
                return Annotation.None;
            }

            var columns = new List<(char Template, char Query, char Difference, string Class)>();
            int query_pos = 0;
            int template_pos = 0;
            foreach (var piece in match.Alignment)
            {
                switch (piece)
                {
                    case SequenceMatch.Match m:
                        for (int i = 0; i < m.Length; i++)
                        {
                            annotation.Add(GetClasses(query_pos));
                            query_pos++;
                            template_pos++;
                        }
                        break;
                    case SequenceMatch.Insertion q:
                        for (int i = 0; i < q.Length; i++)
                        {
                            annotation.Add(GetClasses(query_pos));
                            query_pos++;
                        }
                        break;
                    case SequenceMatch.Deletion t:
                        template_pos += t.Length;
                        break;
                    default:
                        break;
                }
            }
            ConsensusSequenceAnnotationCache = annotation.ToArray();
            return ConsensusSequenceAnnotationCache;
        }

        /// <summary>
        /// Find the ambiguous positions in this sequence an the support for the connections between these positions in the placed reads.
        /// Only searches for support one node at a time.
        /// </summary>
        /// <returns>A list of AmbiguityNodes containing the Position and Support for the connection to the next node.</returns>
        public static double AmbiguityThreshold = -1;
        AmbiguityNode[] SequenceAmbiguityAnalysisCache = null;
        public AmbiguityNode[] SequenceAmbiguityAnalysis()
        {
            if (SequenceAmbiguityAnalysisCache != null) return SequenceAmbiguityAnalysisCache;
            var consensus = this.CombinedSequence();
            AmbiguityNode[] ambiguous;
            try
            {
                ambiguous = consensus.Select((position, index) =>
                {
                    if (position.AminoAcids.Count() == 0) return -1;
                    else if (position.AminoAcids.Values.Max() < Template.AmbiguityThreshold * position.AminoAcids.Values.Sum()) return index;
                    else return -1;
                }).Where(p => p != -1).Select(i => new AmbiguityNode(i)).ToArray();
            }
            catch (System.InvalidOperationException)
            {
                return new AmbiguityNode[0]; // For some stupid reason the .ToArray() throws an exception when no elements are present, while an empty array is totally valid.
            }

            for (int i = 0; i < ambiguous.Length - 1; i++)
            {
                var position = ambiguous[i].Position;
                var next_position = ambiguous[i + 1].Position;

                foreach (var peptide in this.Matches)
                {
                    var this_aa = peptide.GetAtTemplateIndex(position);
                    var next_aa = peptide.GetAtTemplateIndex(next_position);
                    if ((this_aa != null && next_aa != null))
                        ambiguous[i].UpdateSupport(this_aa.Value, next_aa.Value, peptide.MetaData.Intensity, peptide.MetaData.Identifier);
                }
            }

            var backtracked = new AmbiguityNode[ambiguous.Length];
            if (ambiguous.Length > 0) backtracked[0] = ambiguous[0];

            for (int i = 1; i < ambiguous.Length; i++)
                backtracked[i] = ambiguous[i].BacktrackSupport(ambiguous[i - 1]);

            SequenceAmbiguityAnalysisCache = backtracked;
            return ambiguous;
        }

        public struct AmbiguityNode
        {
            /// <summary> The position in the consensus sequence for this ambiguity node. </summary>
            public int Position { get; private set; }

            /// <summary> All support to the next node. </summary>
            public Dictionary<(AminoAcid, AminoAcid), double> Support { get; private set; }

            /// <summary> All Identifiers of reads that support this ambiguous node. </summary>
            public HashSet<string> SupportingReads { get; private set; }

            /// <summary>
            /// Create a new ambiguity node on the given position.
            /// </summary>
            /// <param name="position">The consensus sequence position for this node.</param>
            public AmbiguityNode(int position)
            {
                this.Position = position;
                this.Support = new();
                this.SupportingReads = new();
            }

            /// <summary>
            /// Add new support to this node. THis will not overwrite any previously added support.
            /// </summary>
            /// <param name="here">The aminoacid at this location.</param>
            /// <param name="next">The aminoacid at the next node.</param>
            /// <param name="Intensity">The Intensity of the supporting read.</param>
            /// <param name="Identifier">The Identifier of the supporting read.</param>
            public void UpdateSupport(AminoAcid here, AminoAcid next, double Intensity, string Identifier)
            {
                var key = (here, next);
                if (this.Support.ContainsKey(key))
                {
                    this.Support[key] += Intensity;
                }
                else
                {
                    this.Support[key] = Intensity;
                }
                this.SupportingReads.Add(Identifier);
            }

            /// <summary>
            /// Update the support for this node based on the previous node. And return the updated node as the result.
            /// </summary>
            /// <param name="previous">The previous node.</param>
            /// <returns>A new node with the information from this node plus the backtracked information from the previous node.</returns>
            public AmbiguityNode BacktrackSupport(AmbiguityNode previous)
            {
                var output = new AmbiguityNode(this.Position);
                output.Support = new Dictionary<(AminoAcid, AminoAcid), double>(this.Support);
                output.SupportingReads = this.SupportingReads.Union(previous.SupportingReads).ToHashSet();
                return output;
            }
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