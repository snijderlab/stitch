using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using HtmlGenerator;

namespace Stitch
{
    /// <summary> A class to hold all metadata handling in one place. </summary> 
    public struct LocalSequence
    {

        /// <summary> The sequence of this read. </summary>
        public AminoAcid[] Sequence { get; private set; }

        /// <summary> All changes made to the sequence of this read with their reasoning. </summary>
        public List<(int Offset, AminoAcid[] Old, AminoAcid[] New, string Reason)> SequenceChanges = new();

        public int Length { get => Sequence.Length; }

        /// <summary> Create a new sequence changing context. </summary>
        /// <param name="sequence"> The original sequence, will be cloned. </param>
        public LocalSequence(AminoAcid[] sequence)
        {
            Sequence = sequence.ToArray();
        }

        /// <summary> Create a new sequence changing context. </summary>
        /// <param name="read"> The read with the original sequence, will be updated to contain a pointer to this local sequence. </param>
        /// <param name="template"> The template where this read is placed. </param>
        public LocalSequence(Read.IRead read, Read.IRead template)
        {
            Sequence = read.Sequence.Sequence.ToArray();
            read.SequenceChanges.Add((template, this));
        }

        /// <summary> Update the sequence. </summary>
        /// <param name="offset"> The start of the change. </param>
        /// <param name="delete"> The number of aminoacids to remove (use the same number as the changed amino acids to do a direct modification). </param>
        /// <param name="change"> The new aminoacids to introduce. </param>
        /// <param name="reason"> The reasoning for the change, used to review the changes as a human. </param>
        public void UpdateSequence(int offset, int delete, AminoAcid[] change, string reason)
        {
            this.SequenceChanges.Add((offset, this.Sequence.Skip(offset).Take(delete).ToArray(), change, reason));
            this.Sequence = this.Sequence.Take(offset).Concat(change).Concat(this.Sequence.Skip(offset + delete)).ToArray();
        }

        /// <summary> Render that changes of this sequencing changing context in a nice and user accessible manner. </summary>
        public HtmlBuilder RenderChangedSequence()
        {
            var html = new HtmlBuilder();
            if (SequenceChanges.Count == 0) return html;
            html.OpenAndClose(HtmlTag.h3, "", "Changes to the peptide sequence");
            var rev = SequenceChanges.ToList();
            rev.Reverse();
            foreach (var change in rev)
            {
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
    }
}