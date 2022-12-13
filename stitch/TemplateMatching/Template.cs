using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using static Stitch.HelperFunctionality;


namespace Stitch {
    /// <summary> Saves a template and its alignment with the given matches. </summary>
    public class Template {
        /// <summary> The name of the containing Segment. <see cref="Segment.Name"/> </summary>
        public readonly string Name;

        /// <summary> The class of the Template, eg "IGHV3" for "IGHV3-66". <see cref="IMetaData.ClassIdentifier"/> </summary>
        public string Class {
            get {
                return MetaData.ClassIdentifier;
            }
        }

        /// <summary> The sequence of this template </summary>
        public readonly AminoAcid[] Sequence;

        /// <summary> Metadata for this template </summary>
        public readonly Read.IRead MetaData;

        /// <summary> The score for this template </summary>
        public int Score {
            get {
                if (Parent.Scoring == RunParameters.ScoringParameter.Absolute) return score;
                else return (int)Math.Round((double)score / Sequence.Length);
            }
        }
        int score;

        /// <summary> To signify if this template was used in a match run which used EnforceUnique. </summary>
        public bool ForcedOnSingleTemplate;

        /// <summary> To signify if the germline Isoleucines should be copied to the consensus sequence. </summary>
        public bool ForceGermlineIsoleucine;

        /// <summary> The unique score for this template. </summary>
        public int UniqueScore {
            get {
                if (Parent.Scoring == RunParameters.ScoringParameter.Absolute) return uniqueScore;
                else return (int)Math.Round((double)uniqueScore / Sequence.Length);
            }
        }
        int uniqueScore;

        /// <summary> The total area for this template. </summary>
        public double TotalArea = 0;
        /// <summary> The total area for uniquely placed reads for this template. </summary>
        public double TotalUniqueArea = 0;
        /// <summary> The total uniquely placed reads on this template. </summary>
        public int UniqueMatches = 0;

        /// <summary> The list of matches on this template. </summary>
        public List<Alignment> Matches;

        /// <summary> If this template is recombinated these are the templates it consists of. </summary>
        public readonly List<Template> Recombination;

        /// <summary> The location this template resides (index in the containing Segment and its location). </summary>
        public readonly TemplateLocation Location;

        [JsonIgnore]
        /// <summary> The parent segment, needed to get the settings for scoring, alphabet etc. </summary>
        public readonly Segment Parent;

        /// <summary> To keep track on how this template was segment joined, this number is the number of characters to remove from the front after all 'X's have been trimmed.
        /// Only makes sense in recombined templates. </summary>
        public int Overlap = 0;

        /// <summary> Creates a new template. </summary>
        /// <param name="name">The name of the enclosing Segment, <see cref="Name"/>.</param>
        /// <param name="seq">The sequence, <see cref="Sequence"/>.</param>
        /// <param name="meta">The metadata, <see cref="MetaData"/>.</param>
        /// <param name="alphabet">The alphabet, <see cref="Alphabet"/>.</param>
        /// <param name="location">The location, <see cref="Location"/>.</param>
        /// <param name="recombination">The recombination, if recombined otherwise null, <see cref="Recombination"/>.</param>
        public Template(string name, AminoAcid[] seq, Read.IRead meta, Segment parent, bool forceGermlineIsoleucine, TemplateLocation location = null, List<Template> recombination = null) {
            Name = name;
            Sequence = seq;
            MetaData = meta;
            score = 0;
            Matches = new List<Alignment>();
            Recombination = recombination;
            Location = location;
            Parent = parent;
            ForceGermlineIsoleucine = forceGermlineIsoleucine;
        }

        /// <summary> Adds a new match to the list of matches, if the score is above the cutoff. </summary>
        /// <param name="match">The match to add</param>
        /// <param name="unique">To signify if this read is only placed here (EnforceUnique) or that it is a normal placement.</param>
        public void AddMatch(Alignment match, bool unique = false) {
            lock (Matches) {
                if (match.Score >= Parent.CutoffScore * Math.Sqrt(match.ReadB.Sequence.Length)) {
                    score += match.Score;
                    TotalArea += match.ReadB.TotalArea;
                    Matches.Add(match);

                    if (unique) {
                        this.ForcedOnSingleTemplate = true;
                        match.Unique = true;
                        uniqueScore += match.Score;
                        TotalUniqueArea += match.ReadB.TotalArea;
                        UniqueMatches++;
                    }
                }
            }
        }
        /// <summary> Contains possibilities for a gap. </summary>
        public interface IGap { }

        /// <summary> No gap </summary>
        public struct None : IGap {
            public override string ToString() { return ""; }
            public override int GetHashCode() { return 397; }
            public override bool Equals(object obj) { return obj is None; }
            public static bool operator ==(None a, object obj) { return a.Equals(obj); }
            public static bool operator !=(None a, object obj) { return !a.Equals(obj); }
        }

        /// <summary> A gap </summary>
        public struct Gap : IGap {
            /// <summary> The sequence of this gap </summary>
            public readonly AminoAcid[] Sequence;
            int hashCode;

            /// <summary> Creates a new Gap </summary>
            /// <param name="sequence">The sequence of this gap, <see cref="Sequence"/>.</param>
            public Gap(AminoAcid[] sequence) {
                Sequence = sequence;

                // Pre computes a hash code based on the actual sequence of the gap
                int hash = 1217;
                if (sequence.Length != 0) {
                    int pos = 0;
                    for (int i = 0; i < 5; i++) {
                        hash ^= Sequence[pos].GetHashCode();
                        hash += 11;
                        pos = ((pos + i) * 653) % Sequence.Length;
                    }
                }
                hashCode = hash;
            }

            public override string ToString() {
                return AminoAcid.ArrayToString(Sequence);
            }

            public override int GetHashCode() {
                return hashCode;
            }

            // Equality is defined by the equality of the sequences
            public override bool Equals(object obj) {
                if (obj is Gap other && this.Sequence.Length == other.Sequence.Length) {
                    return AminoAcid.ArrayEquals(this.Sequence, other.Sequence);
                } else {
                    return false;
                }
            }
            public static bool operator ==(Gap a, object obj) { return a.Equals(obj); }
            public static bool operator !=(Gap a, object obj) { return !a.Equals(obj); }
        }

        public struct SequenceOption {
            public readonly AminoAcid[] Sequence;
            public readonly int Length;

            public SequenceOption(AminoAcid[] sequence, int length) {
                Sequence = sequence;
                Length = length;
            }

            public override bool Equals(object obj) {
                return obj is SequenceOption option && this.Length == option.Length && AminoAcid.ArrayEquals(this.Sequence, option.Sequence);
            }

            public override int GetHashCode() {
                return this.Length + Sequence.Aggregate(0, (acc, item) => (int)item.Character + acc * 97) * 7877;
            }

            public static bool operator ==(SequenceOption a, SequenceOption b) {
                return a.Equals(b);
            }

            public static bool operator !=(SequenceOption a, SequenceOption b) {
                return !a.Equals(b);
            }
        }

        /// <summary> Returns the combined sequence or aminoacid variety per position in the alignment. </summary>
        /// <returns>A list of tuples. The first item is a dictionary with the aminoacid variance for this position, with counts. The second item contains a dictionary with the gap variety, with counts.</returns>
        private List<(AminoAcid Template, Dictionary<SequenceOption, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> combinedSequenceCache = null;
        public List<(AminoAcid Template, Dictionary<SequenceOption, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> CombinedSequence() {
            if (combinedSequenceCache != null) return combinedSequenceCache;

            var output = new List<(AminoAcid Template, Dictionary<SequenceOption, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)>() {
                Capacity = Sequence.Length + 1
            };

            foreach (var amino in Sequence) {
                var position = (amino, new Dictionary<SequenceOption, double>(), new Dictionary<IGap, (int, double[])>());
                output.Add(position);
            }

            foreach (var alignment in Matches) {
                var pos_a = alignment.StartA;
                var pos_b = alignment.StartB;
                var insertion = new List<AminoAcid>();

                foreach (var piece in alignment.Path) {
                    if (alignment.LenA == 0) {
                        insertion.Add(alignment.ReadB.Sequence.Sequence[pos_b]); // StepB is 1 so this works
                    } else {
                        // Handle gaps
                        var positional_score = alignment.ReadB.Sequence.PositionalScore;
                        IGap gap = new None();
                        if (pos_b > positional_score.Length) Console.WriteLine($"Too big {pos_b} {positional_score.Length} {alignment.ReadA.Identifier} {alignment.ReadB.Identifier}");
                        var gap_cov = new double[] { pos_b == positional_score.Length ? positional_score.Last() : positional_score[pos_b] };
                        if (insertion.Count != 0) {
                            gap = new Gap(insertion.ToArray());
                            gap_cov = positional_score.SubArray(pos_b - insertion.Count, insertion.Count).ToArray();
                        }
                        var position = output[Math.Min(pos_a, output.Count - 1)].Gaps;
                        if (position.ContainsKey(gap)) {
                            if (position[gap].CoverageDepth != null)
                                gap_cov = position[gap].CoverageDepth.ElementwiseAdd(gap_cov);

                            position[gap] = (position[gap].Count + 1, gap_cov);
                        } else {
                            position.Add(gap, (1, gap_cov));
                        }
                        insertion.Clear();

                        // Handle normal sequences
                        var option = new SequenceOption(alignment.ReadB.Sequence.Sequence.SubArray(pos_b, piece.StepB), piece.StepA);
                        var cov = piece.StepB == 0 ? 0 : positional_score.SubArray(pos_b, piece.StepB).Average();

                        if (output[Math.Min(pos_a, output.Count - 1)].AminoAcids.ContainsKey(option)) {
                            output[Math.Min(pos_a, output.Count - 1)].AminoAcids[option] += cov;
                        } else {
                            output[Math.Min(pos_a, output.Count - 1)].AminoAcids.Add(option, cov);
                        }
                    }
                    pos_a += piece.StepA;
                    pos_b += piece.StepB;
                }

                // Handle possible remaining gap
                if (insertion.Count != 0) {
                    Console.WriteLine($"WARNING: gap left after full alignment: {AminoAcid.ArrayToString(insertion)} {alignment.ReadB.Identifier} on {alignment.ReadA.Identifier}");
                    //var gap = new Gap(insertion.ToArray());
                    //var gap_cov = alignment.ReadB.Sequence.PositionalScore.SubArray(pos_b - insertion.Count, insertion.Count).ToArray();
                    //var position = output.Last().Gaps;
                    //if (position.ContainsKey(gap)) {
                    //    if (position[gap].CoverageDepth != null)
                    //        gap_cov = position[gap].CoverageDepth.ElementwiseAdd(gap_cov);
                    //
                    //    position[gap] = (position[gap].Count + 1, gap_cov);
                    //} else {
                    //    position.Add(gap, (1, gap_cov));
                    //}
                    //insertion.Clear();
                }
            }
            combinedSequenceCache = output;
            return output;
        }
        (List<SequenceOption>, List<double>) ConsensusSequenceCache = (null, null);
        public (List<SequenceOption>, List<double>) ConsensusSequence() {
            if (ConsensusSequenceCache != (null, null)) return ConsensusSequenceCache;

            var consensus = new List<SequenceOption>();
            var doc = new List<double>();
            var combinedSequence = CombinedSequence();
            var options = new List<SequenceOption>();
            double max;
            double coverage;
            List<Template.IGap> max_gap = new List<IGap>();

            for (int i = 0; i < combinedSequence.Count; i++) {
                // Get the highest chars
                options.Clear();
                max = 0;
                coverage = 0;

                foreach (var item in combinedSequence[i].AminoAcids) {
                    coverage += item.Value;
                    if (item.Value > max) {
                        options.Clear();
                        options.Add(item.Key);
                        max = item.Value;
                    } else if (item.Value == max) {
                        options.Add(item.Key);
                    }
                }

                var template = new SequenceOption(new AminoAcid[] { combinedSequence[i].Template }, 1);
                if (options.Count == 1 && options[0].Length == 1 && options[0].Sequence.Length == 1 && options[0].Sequence[0].Character == this.Parent.Alphabet.GapChar) {
                    // Do not add gaps, as those are not part of the final sequence
                } else if (options.Count > 1 && options.Contains(template)) {
                    consensus.Add(template);
                    doc.Add(coverage);
                } else if (ForceGermlineIsoleucine && options.Count > 0 && options[0].Length == 1 && options[0].Sequence.Length == 1 && options[0].Sequence[0].Character == 'L' && combinedSequence[i].Template.Character == 'I') {
                    consensus.Add(template);
                    doc.Add(coverage);
                } else if (options.Count > 0) {
                    consensus.Add(options[0]);
                    doc.Add(coverage);
                } else {
                    // There is no data from reads so take the template sequence
                    consensus.Add(template);
                    doc.Add(coverage);
                }

                // Get the highest gap
                max_gap.Clear();
                max = 0;
                var gap_coverage = new double[0];

                foreach (var item in combinedSequence[i].Gaps) {
                    if (item.Value.Count > max) {
                        max_gap.Clear();
                        max_gap.Add(item.Key);
                        max = item.Value.Count;
                        gap_coverage = item.Value.CoverageDepth;
                    } else if (item.Value.Count == max) {
                        max_gap.Add(item.Key);
                    }
                }

                if (max_gap.Count >= 1 && max_gap[0].GetType() != typeof(Template.None)) {
                    consensus.AddRange(((Template.Gap)max_gap[0]).Sequence.Select(s => new SequenceOption(new AminoAcid[] { s }, 1)));
                    doc.AddRange(gap_coverage);
                }
            }
            ConsensusSequenceCache = (consensus, doc);
            return ConsensusSequenceCache;
        }

        /// <summary> Align the consensus sequence of this Template to its original sequence, in the case of a recombined sequence align with the original sequences of its templates. </summary>
        /// <returns>The sequence match containing the result</returns>
        public Alignment AlignConsensusWithTemplate() {
            var consensus = new Read.Simple(this.ConsensusSequence().Item1.SelectMany(i => i.Sequence).ToArray());
            if (Recombination != null)
                return new Alignment(new Read.Simple(this.Recombination.SelectMany(a => a.Sequence).ToArray()), consensus, Parent.Alphabet, AlignmentType.Global);
            else
                return new Alignment(this.MetaData, consensus, Parent.Alphabet, AlignmentType.Global);
        }

        /// <summary> The annotated consensus sequence given as an array with the length of the consensus sequence. </summary>
        private Annotation[] ConsensusSequenceAnnotationCache = null;
        public Annotation[] ConsensusSequenceAnnotation() {
            if (ConsensusSequenceAnnotationCache != null) return ConsensusSequenceAnnotationCache;

            var match = this.AlignConsensusWithTemplate();
            var annotation = new List<Annotation>(this.Sequence.Length);

            List<(Annotation, string)> annotated = null;
            if (this.Recombination != null) {
                annotated = this.Recombination.Aggregate(new List<(Annotation, string)>(), (acc, item) => {
                    var x_start = item.ConsensusSequence().Item1.SelectMany(i => i.Sequence).TakeWhile(a => a.Character == 'X').Count();
                    var main_sequence = item.ConsensusSequence().Item1.SelectMany(i => i.Sequence).Skip(x_start).TakeWhile(a => a.Character != 'X').Count();
                    var sequence = AminoAcid.ArrayToString(item.ConsensusSequence().Item1.SelectMany(i => i.Sequence).Skip(x_start).Take(main_sequence).ToArray());
                    acc.AddRange(item.ConsensusSequenceAnnotation().Skip(x_start).Take(main_sequence).Skip(item.Overlap).Zip(sequence).Select((a) => (a.First, a.Second.ToString())));
                    return acc;
                });
            } else {
                if (this.MetaData is Read.Fasta meta)
                    if (meta.AnnotatedSequence != null)
                        annotated = meta.AnnotatedSequence;
            }

            Annotation GetClasses(int position) {
                if (annotated == null) return Annotation.None;
                int pos = -1;
                for (int i = 0; i < annotated.Count; i++) {
                    pos += annotated[i].Item2.Length;
                    if (pos >= position + match.StartA)
                        return annotated[i].Item1;
                }
                return Annotation.None;
            }

            var columns = new List<(char Template, char Query, char Difference, string Class)>();
            int pos_a = 0;
            int pos_b = 0;
            foreach (var piece in match.Path) {
                if (piece.StepA != 0) {
                    annotation.AddRange(Enumerable.Repeat(GetClasses(pos_a), piece.StepA));
                }
                pos_a += piece.StepA;
                pos_b += piece.StepB;
            }
            ConsensusSequenceAnnotationCache = annotation.ToArray();
            return ConsensusSequenceAnnotationCache;
        }

        /// <summary> Find the ambiguous positions in this sequence an the support for the connections between these positions in the placed reads.
        /// Only searches for support one node at a time. </summary>
        /// <returns>A list of AmbiguityNodes containing the Position and Support for the connection to the next node.</returns>
        public static double AmbiguityThreshold = -1;
        AmbiguityNode[] SequenceAmbiguityAnalysisCache = null;
        public AmbiguityNode[] SequenceAmbiguityAnalysis() {
            // Check the cache
            if (SequenceAmbiguityAnalysisCache != null) return SequenceAmbiguityAnalysisCache;

            var consensus = this.CombinedSequence();
            AmbiguityNode[] ambiguous;

            // Find all ambiguous positions
            var a = consensus.Select((position, index) => {
                if (position.AminoAcids.Count() == 0) return -1;
                else if (position.AminoAcids.Values.Max() < Template.AmbiguityThreshold * position.AminoAcids.Values.Sum()) return index;
                else return -1;
            }).Where(p => p != -1).Select(i => new AmbiguityNode(i));
            if (a.Count() == 0)
                return new AmbiguityNode[0];
            else
                ambiguous = a.ToArray();

            // Add all reads that support ambiguous positions
            for (int i = 0; i < ambiguous.Length - 1; i++) {
                foreach (var peptide in this.Matches) {
                    var this_aa = peptide.GetAtTemplateIndex(ambiguous[i].Position);
                    if (this_aa == null) continue;

                    var path = new List<AminoAcid>();
                    for (int offset = 1; i + offset < ambiguous.Length; offset++) {
                        var position = ambiguous[i + offset].Position;
                        var next_aa = peptide.GetAtTemplateIndex(position);
                        if (next_aa != null)
                            path.Add(next_aa.Value);
                    }
                    if (path.Count > 0) {
                        var reverse_path = new List<AminoAcid>(path);
                        reverse_path.Reverse();
                        reverse_path.Add(this_aa.Value);
                        for (int j = 0; j < reverse_path.Count - 1; j++) {
                            ambiguous[i + reverse_path.Count - 1 - j].UpdateHigherOrderSupportBackward(reverse_path[j], peptide.ReadB.Intensity, peptide.ReadB.Identifier, reverse_path.Skip(j + 1).ToArray());
                        }
                        ambiguous[i].UpdateHigherOrderSupportForward(this_aa.Value, peptide.ReadB.Intensity, peptide.ReadB.Identifier, path.ToArray());
                    }
                }
            }

            // Simplify all trees
            foreach (var position in ambiguous)
                foreach (var tree in position.SupportTrees) {
                    tree.Value.Backward.Simplify();
                    tree.Value.Forward.Simplify();
                }

            // Set the cache
            SequenceAmbiguityAnalysisCache = ambiguous;
            return ambiguous;
        }
    }

    /// <summary> The location of a template, in its Segment and its location </summary>
    public class TemplateLocation {
        /// <summary> The location of the <see cref="Segment"/>, see <see cref="Segment.Index"/>. </summary>
        public readonly int SegmentIndex;

        /// <summary> The index of the <see cref="Template"/>, defined as the index in the containing Segment list of Templates, see <see cref="Segment.Templates"/>. </summary>
        public readonly int TemplateIndex;

        /// <summary> Creates a new TemplateLocation </summary>
        /// <param name="segmentIndex"> The Segment index, see <see cref="SegmentIndex"/>.</param>
        /// <param name="templateIndex"> The Template index, see <see cref="TemplateIndex"/>.</param>
        public TemplateLocation(int segmentIndex, int templateIndex) {
            SegmentIndex = segmentIndex;
            TemplateIndex = templateIndex;
        }
    }

    /// <summary> The location of a recombined template, as there is only one list of recombined templates only one index has to be saved. </summary>
    public class RecombinedTemplateLocation : TemplateLocation {
        public RecombinedTemplateLocation(int templateIndex) : base(-1, templateIndex) { }
    }
}