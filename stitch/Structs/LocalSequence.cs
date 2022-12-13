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
        public LocalSequence(AminoAcid[] sequence, double[] positional_score) {
            if (sequence.Length != positional_score.Length) throw new ArgumentException($"Unequal length of arguments when generating local sequence. seq {sequence.Length} pos_score {positional_score}.");
            OriginalSequence = sequence.ToArray();
            Sequence = sequence.ToArray();
            PositionalScore = positional_score;
        }

        /// <summary> Create a new sequence changing context. </summary>
        /// <param name="read"> The read with the original sequence, will be updated to contain a pointer to this local sequence. </param>
        /// <param name="template"> The template where this read is placed. </param>
        public LocalSequence(Read.IRead read, Read.IRead template) {
            OriginalSequence = read.Sequence.Sequence.ToArray();
            Sequence = read.Sequence.Sequence.ToArray();
            PositionalScore = read.Sequence.PositionalScore.ToArray();
            read.SequenceChanges.Add((template, this));
        }

        /// <summary> Update the sequence. </summary>
        /// <param name="offset"> The start of the change. </param>
        /// <param name="delete"> The number of aminoacids to remove (use the same number as the changed amino acids to do a direct modification). </param>
        /// <param name="change"> The new aminoacids to introduce. </param>
        /// <param name="reason"> The reasoning for the change, used to review the changes as a human. </param>
        public void UpdateSequence(int offset, int delete, AminoAcid[] change, string reason) {
            this.Changes.Add((offset, this.Sequence.Skip(offset).Take(delete).ToArray(), change, reason));
            this.Sequence = this.Sequence.Take(offset).Concat(change).Concat(this.Sequence.Skip(offset + delete)).ToArray();
            if (PositionalScore.Length != 0) {
                var to_delete = this.PositionalScore.Skip(offset).Take(delete);
                var average_score = to_delete.Count() == 0 ? 0.0 : to_delete.Average();
                this.PositionalScore = this.PositionalScore.Take(offset).Concat(Enumerable.Repeat(average_score, change.Length)).Concat(this.PositionalScore.Skip(offset + delete)).ToArray();
            }
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
    }
}