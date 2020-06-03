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

        public string Class
        {
            get
            {
                if (Parent.ClassChars == -1) return "";
                return MetaData.Identifier.Substring(0, Math.Min(Parent.ClassChars, MetaData.Identifier.Length));
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

        public double TotalArea = 0;

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
        public readonly TemplateDatabase Parent;

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
                        TotalArea += match.MetaData.TotalArea;
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
            var levels = new List<List<(int Start, int Length)>>(Matches.Count());
            var level_lookup = new int[Matches.Count()];
            int start, length;
            bool placed, could_be_placed;

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                start = Matches[matchindex].StartTemplatePosition;
                length = Matches[matchindex].LengthOnTemplate;
                placed = false;

                for (int l = 0; l < levels.Count(); l++)
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
                    level_lookup[matchindex] = levels.Count() - 1;
                }
            }

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((new (int MatchIndex, int SequencePosition, double CoverageDepth, int ContigID)[levels.Count()], new (int, IGap, double[], int, bool)[levels.Count()]));
                for (int j = 0; j < levels.Count(); j++)
                {
                    output[i].Sequences[j] = (-2, 0, 0, -1);
                    output[i].Gaps[j] = (-2, new None(), new double[0], -1, false);
                }
            }

            SequenceMatch match;
            SequenceMatch.MatchPiece piece;

            for (int matchindex = 0; matchindex < Matches.Count(); matchindex++)
            {
                match = Matches[matchindex];
                // Start at StartTemplatePosition and StartQueryPosition
                int template_pos = match.StartTemplatePosition;
                int seq_pos = match.StartQueryPosition;
                bool gap = false;
                int level = level_lookup[matchindex];

                for (int pieceindex = 0; pieceindex < match.Alignment.Count(); pieceindex++)
                {
                    piece = match.Alignment[pieceindex];
                    var inseq = pieceindex < match.Alignment.Count() - 1 && pieceindex > 0 ? true : false;

                    if (piece is SequenceMatch.Match m)
                    {
                        for (int i = 0; i < m.Length && template_pos < Sequence.Length && seq_pos < match.QuerySequence.Length; i++)
                        {
                            var contigid = match.Index;
                            if (match.MetaData is MetaData.Path mp) contigid = mp.ContigID[seq_pos];
                            // Add this ID to the list
                            var in_sequence = inseq // In the middle of the pieces
                                           || (i < m.Length - 1) // Not the last AA
                                           || (pieceindex < match.Alignment.Count() - 1 && i == m.Length - 1); // With a piece after this one the last AA is in the sequence

                            output[template_pos].Sequences[level] = (matchindex, seq_pos + 1, match.MetaData.PositionalScore[seq_pos], contigid);
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
                        if (match.MetaData is MetaData.Path mp) contigid = mp.ContigID[seq_pos - 1];

                        seq_pos += len;
                        output[Math.Max(0, template_pos - 1)].Gaps[level] = (matchindex, sub_seq, cov, contigid, inseq);
                    }
                    else if (piece is SequenceMatch.GapInTemplate gt)
                    {
                        // Skip to the next section
                        for (int i = 0; i < gt.Length && template_pos < output.Count(); i++)
                        {
                            output[template_pos].Sequences[level] = (matchindex, -1, 1, -1); //TODO: figure out the best score for a gap in a path
                            output[template_pos].Gaps[level] = (matchindex, new None(), new double[0], -1, true);
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
        private List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> combinedSequenceCache = null;
        public List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)> CombinedSequence()
        {
            if (combinedSequenceCache != null) return combinedSequenceCache;

            var output = new List<(AminoAcid Template, Dictionary<AminoAcid, double> AminoAcids, Dictionary<IGap, (int Count, double[] CoverageDepth)> Gaps)>()
            {
                Capacity = Sequence.Length
            };

            // Add all the positions
            for (int i = 0; i < Sequence.Length; i++)
            {
                output.Add((Sequence[i], new Dictionary<AminoAcid, double>(), new Dictionary<IGap, (int, double[])>()));
            }

            var alignedSequences = AlignedSequences();
            bool placed;
            AminoAcid aa;
            IGap key;

            for (int i = 0; i < Sequence.Length; i++)
            {
                // Create the aminoacid dictionary
                placed = false;
                foreach (var option in alignedSequences[i].Sequences)
                {
                    if (option.SequencePosition != 0)
                    {
                        if (option.SequencePosition == -1)
                        {
                            aa = new AminoAcid(Parent.Alphabet, Alphabet.GapChar);
                        }
                        else
                        {
                            aa = Matches[option.MatchIndex].QuerySequence[option.SequencePosition - 1];
                            placed = true;
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
                if (!placed)
                {
                    output[i].AminoAcids.Add(Sequence[i], 1);
                }

                // Create the gap dictionary
                foreach (var option in alignedSequences[i].Gaps)
                {
                    if (option.Gap == null || !option.InSequence) continue;

                    if (option.Gap == (IGap)new None())
                    {
                        if (output[i].Gaps.ContainsKey(new None()))
                        {
                            output[i].Gaps[new None()] = (output[i].Gaps[new None()].Count + 1, new double[0]);
                        }
                        else
                        {
                            output[i].Gaps.Add(new None(), (1, new double[0]));
                        }
                    }
                    else
                    {
                        key = option.Gap;

                        if (output[i].Gaps.ContainsKey(key))
                        {
                            double[] cov;
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
        string ConsensusSequenceCache = null;
        public string ConsensusSequence()
        {
            if (ConsensusSequenceCache != null) return ConsensusSequenceCache;

            var consensus = new StringBuilder();
            var combinedSequence = CombinedSequence();
            string options = "";
            double max;
            List<Template.IGap> max_gap = new List<IGap>();

            for (int i = 0; i < combinedSequence.Count; i++)
            {
                // Get the highest chars
                options = "";
                max = 0;
                //bool containsIsoleucine = false;
                //bool containsD = false;

                foreach (var item in combinedSequence[i].AminoAcids)
                {
                    //if (!containsIsoleucine && item.Key.ToString() == "I") containsIsoleucine = true;
                    //if (!containsD && item.Key.ToString() == "D") containsD = true;
                    if (item.Value > max)
                    {
                        options = item.Key.ToString();
                        max = item.Value;
                    }
                    else if (item.Value == max)
                    {
                        options += item.Key.ToString();
                    }
                }

                if (options.Length > 1 && options.Contains(combinedSequence[i].Template.Char))
                {
                    consensus.Append(combinedSequence[i].Template.Char);
                }
                else
                {
                    // Choose I over L and D over N or just the first option
                    //if (options[0] == 'L' && containsIsoleucine)
                    //    consensus.Append('I');
                    //else if (options[0] == 'N' && containsD)
                    //    consensus.Append('D');
                    //else
                    consensus.Append(options[0]);
                }

                // Get the highest gap
                max_gap.Clear();
                max = 0;

                foreach (var item in combinedSequence[i].Gaps)
                {
                    if (item.Value.Count > max)
                    {
                        max_gap.Clear();
                        max_gap.Add(item.Key);
                        max = item.Value.Count;
                    }
                    else if (item.Value.Count == max)
                    {
                        max_gap.Add(item.Key);
                    }
                }

                if (max_gap.Count >= 1 && max_gap[0].GetType() != typeof(Template.None))
                {
                    consensus.Append(max_gap[0].ToString());
                }
            }
            ConsensusSequenceCache = consensus.ToString();
            return ConsensusSequenceCache;
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