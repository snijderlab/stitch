using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HtmlGenerator;

namespace Stitch {
    /// <summary> A class to hold all metadata handling in one place. </summary>
    public class LocalSequence {

        /// <summary> The sequence of this read. </summary>
        public AminoAcid[] Sequence { get; private set; }
        AminoAcid[] OriginalSequence;
        public List<SequenceMatch.MatchPiece> Alignment;

        /// <summary> All changes made to the sequence of this read with their reasoning. </summary>
        public List<(int Offset, AminoAcid[] Old, AminoAcid[] New, string Reason)> Changes = new();

        /// <summary> Returns the positional score for this read, so for every position the confidence.
        /// The exact meaning differs for all read types but overall it is used in the depth of coverage calculations. </summary>
        public double[] PositionalScore { get; private set; }

        public int Length { get => Sequence.Length; }
        /// <summary> The total amount of (mis)matching aminoacids in the alignment </summary>
        public int TotalMatches { get; private set; }
        /// <summary> The total length on the template (matches + gaps in query) </summary>
        public int LengthOnTemplate { get; private set; }
        /// <summary> The total length on the query (matches + gaps in template) </summary>
        public int LengthOnQuery { get; private set; }

        /// <summary> Create a new sequence changing context. </summary>
        /// <param name="sequence"> The original sequence, will be cloned. </param>
        public LocalSequence(AminoAcid[] sequence, double[] positional_score, List<SequenceMatch.MatchPiece> alignment = null) {
            OriginalSequence = sequence.ToArray();
            Sequence = sequence.ToArray();
            PositionalScore = positional_score;
            Alignment = alignment ?? new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(Sequence.Length) };
            UpdateLengths();
        }

        /// <summary> Create a new sequence changing context. </summary>
        /// <param name="read"> The read with the original sequence, will be updated to contain a pointer to this local sequence. </param>
        /// <param name="template"> The template where this read is placed. </param>
        public LocalSequence(Read.IRead read, Read.IRead template, List<SequenceMatch.MatchPiece> alignment = null) {
            OriginalSequence = read.Sequence.Sequence.ToArray();
            Sequence = read.Sequence.Sequence.ToArray();
            PositionalScore = read.Sequence.PositionalScore.ToArray();
            read.SequenceChanges.Add((template, this));
            Alignment = alignment ?? new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(Sequence.Length) };
            UpdateLengths();
        }

        /// <summary> Update the sequence. </summary>
        /// <param name="offset"> The start of the change. </param>
        /// <param name="delete"> The number of aminoacids to remove (use the same number as the changed amino acids to do a direct modification). </param>
        /// <param name="change"> The new aminoacids to introduce. </param>
        /// <param name="reason"> The reasoning for the change, used to review the changes as a human. </param>
        public void UpdateSequence(int offset, int delete, AminoAcid[] change, string reason) {
            // TODO update the LengthOnTemplate ETC here, but these are not part of the LocalSequence so find a way to make these work together.
            this.Changes.Add((offset, this.Sequence.Skip(offset).Take(delete).ToArray(), change, reason));
            this.Sequence = this.Sequence.Take(offset).Concat(change).Concat(this.Sequence.Skip(offset + delete)).ToArray();
            if (PositionalScore.Length != 0) {
                var to_delete = this.PositionalScore.Skip(offset).Take(delete);
                var average_score = to_delete.Count() == 0 ? 0.0 : to_delete.Average();
                this.PositionalScore = this.PositionalScore.Take(offset).Concat(Enumerable.Repeat(average_score, change.Length)).Concat(this.PositionalScore.Skip(offset + delete)).ToArray();
            }
            OverWriteAlignment(offset, delete, change.Length);
        }

        public bool SetPositionalScore(double[] positional_score) {
            if (positional_score.Length != this.Sequence.Length) {
                return false;
            } else {
                PositionalScore = positional_score;
                return true;
            }
        }

        public bool UpdatePositionalScore(double[] positional_score, int original_weight) {
            if (positional_score.Length != PositionalScore.Length) return false;
            for (int i = 0; i < this.PositionalScore.Length; i++) {
                this.PositionalScore[i] = (this.PositionalScore[i] * original_weight + positional_score[i]) / (original_weight + 1);
            }
            return true;
        }

        /// <summary> Render that changes of this sequencing changing context in a nice and user accessible manner. </summary>
        public HtmlBuilder RenderToHtml() {
            var html = new HtmlBuilder();
            if (Changes.Count == 0) return html;
            html.OpenAndClose(HtmlTag.h3, "", "Changes to the peptide sequence");
            html.OpenAndClose(HtmlTag.p, "", HelperFunctionality.CIGAR(AlignmentWithOriginal()));
            html.Open(HtmlTag.div, "class='seq'");
            var position = 0;
            foreach (var set in ChangeProfile()) {
                if (set.Item1) html.OpenAndClose(HtmlTag.span, "class='changed'", AminoAcid.ArrayToString(HelperFunctionality.SubArray(Sequence, position, set.Item2)));
                else html.Content(AminoAcid.ArrayToString(HelperFunctionality.SubArray(Sequence, position, set.Item2)));
                position += set.Item2;
            }
            html.Close(HtmlTag.div);
            var rev = Changes.ToList();
            rev.Reverse();
            foreach (var change in rev) {
                html.Open(HtmlTag.p, "class='changed-sequence'");
                html.OpenAndClose(HtmlTag.span, "class='seq old'", AminoAcid.ArrayToString(change.Old));
                html.Content("→");
                html.OpenAndClose(HtmlTag.span, "class='seq new'", AminoAcid.ArrayToString(change.New));
                html.OpenAndClose(HtmlTag.span, "class='reason'", change.Reason);
                html.OpenAndClose(HtmlTag.span, "class='offset'", $" (Position: {change.Offset + 1})");
                html.Close(HtmlTag.p);
            }
            return html;
        }

        public (bool Changed, int Length)[] ChangeProfile() {
            IEnumerable<bool> changed = new bool[OriginalSequence.Length];
            foreach (var change in Changes) {
                changed = changed.Take(change.Offset).Concat(Enumerable.Repeat(true, change.New.Length)).Concat(changed.Skip(change.Offset + change.Old.Length));
            }
            var output = new List<(bool, int)>();
            var last = changed.First();
            var length = 1;
            foreach (var position in changed.Skip(1)) {
                if (position == last) {
                    length += 1;
                } else {
                    output.Add((last, length));
                    last = position;
                    length = 1;
                }
            }
            output.Add((last, length));
            return output.ToArray();
        }

        public List<SequenceMatch.MatchPiece> AlignmentWithOriginal() {
            var alignment = new List<SequenceMatch.MatchPiece> { new SequenceMatch.Match(OriginalSequence.Length) };
            //var match = new SequenceMatch(0, 0, 0, alignment, this., null, 0);
            //foreach (var change in Changes) match.OverWriteAlignment(change.Offset, change.Old.Length, change.New.Length);
            return alignment;
        }

        /// <summary> Overwrite an alignment with a new match piece at the given template location. </summary>
        /// <param name="alignment"> The original alignment. </param>
        /// <param name="insert"> The number of places to insert. </param>
        /// <param name="offset"> The template offset to place it at. </param>
        /// <param name="delete"> The number of positions to delete where the insertion is placed. </param>
        void OverWriteAlignment(int offset, int delete, int insert) {
            // This needs to insert insertions at the correct places.
            int pos = 0;
            int to_delete = 0;
            bool placed = false;
            for (int i = 0; i < this.Alignment.Count; i++) {
                SequenceMatch.MatchPiece piece = this.Alignment[i];
                if (placed && to_delete == 0) break;
                if (!(piece is SequenceMatch.Insertion)) {
                    if (to_delete != 0) {
                        var deleted = Math.Min(piece.Length, to_delete);
                        piece.Length = piece.Length - deleted;
                        to_delete -= deleted;
                    }
                    if (pos <= offset && pos + piece.Length > offset) {
                        this.Alignment.Insert(i + 1, new SequenceMatch.Match(Math.Min(delete, insert)));
                        var n = 2;
                        if (delete > insert)
                            this.Alignment.Insert(i + n, new SequenceMatch.Deletion(delete - insert));
                        else if (insert > delete)
                            this.Alignment.Insert(i + n, new SequenceMatch.Insertion(insert - delete));
                        else n = 1;

                        var original_length = piece.Length;
                        var length = offset - pos;
                        piece.Length = Math.Max(0, length);
                        if (original_length - piece.Length < delete) to_delete = delete - (original_length - piece.Length);
                        else if (original_length - piece.Length != delete) {
                            n += 1;
                            this.Alignment.Insert(i + n, piece.Copy(original_length - piece.Length - delete));
                        }
                        placed = true;
                        pos += original_length;
                        i += n - 1; // Skip past the placed insertion
                    } else {
                        pos += piece.Length;
                    }
                } else if (to_delete != 0) {
                    piece.Length = 0; // Remove all insertions if inserting anything here.
                }
            }
            SequenceMatch.Simplify(ref Alignment);
            UpdateLengths();
        }

        void UpdateLengths() {
            // Set up Length on * again
            int sum1 = 0;
            int sum2 = 0;
            int sum3 = 0;
            foreach (var m in Alignment) {
                if (m is SequenceMatch.Match match) {
                    sum1 += match.Length;
                }
                if (m is SequenceMatch.Insertion gc) sum2 += gc.Length;
                if (m is SequenceMatch.Deletion gt) sum3 += gt.Length;
            }
            TotalMatches = sum1;
            LengthOnTemplate = sum1 + sum2;
            LengthOnQuery = sum1 + sum3;
        }
    }
}