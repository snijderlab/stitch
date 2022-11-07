using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stitch {
    /// <summary>A class to save a match of two sequences in a space efficient way, based on CIGAR strings.</summary>
    public class SequenceMatch {
        /// <summary>The position on the template where the match begins.</summary>
        public readonly int StartTemplatePosition;

        /// <summary> The position on the query sequence where the match begins </summary>
        public readonly int StartQueryPosition;

        /// <summary> The score of the match </summary>
        public readonly int Score;

        /// <summary> The alignment of the match, consisting of <see cref="MatchPiece">MatchPieces</see>. </summary>
        public readonly List<MatchPiece> Alignment;

        /// <summary> The query to align. </summary>
        public readonly Read.IRead Query;
        public LocalSequence QuerySequence;

        /// <summary> The template to align. </summary>
        public readonly Read.IRead Template;

        /// <summary> The total amount of (mis)matching aminoacids in the alignment </summary>
        public readonly int TotalMatches;
        /// <summary> The total length on the template (matches + gaps in query) </summary>
        public readonly int LengthOnTemplate;
        /// <summary> The total length on the query (matches + gaps in template) </summary>
        public readonly int LengthOnQuery;

        public readonly int Index;
        /// <summary> The index of the Template sequence if available. </summary>
        public readonly int TemplateIndex;
        public bool Unique;

        public SequenceMatch(int startTemplatePosition, int startQueryPosition, int score, List<MatchPiece> alignment, Read.IRead template, Read.IRead query, int index, int templateIndex = -1) {
            StartTemplatePosition = startTemplatePosition;
            StartQueryPosition = startQueryPosition;
            Score = score;
            Alignment = alignment;
            Template = template;
            Query = query;
            QuerySequence = new LocalSequence(query, template);
            Index = index;
            TemplateIndex = templateIndex;
            Simplify();

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

        /// <summary> Visualises this SequenceMatch, with a very simple visualisation of the alignment </summary>
        public override string ToString() {
            var buffer = new StringBuilder();
            var buffer1 = new StringBuilder();
            var buffer2 = new StringBuilder();
            buffer.Append($"SequenceMatch:\n\tStarting at template: {StartTemplatePosition}\n\tStarting at query: {StartQueryPosition}\n\tScore: {Score}\n\tMatch: {Alignment.CIGAR()}\n\n");
            int tem_pos = StartTemplatePosition;
            int query_pos = StartQueryPosition;
            string tSeq = AminoAcid.ArrayToString(Template.Sequence.Sequence);
            string qSeq = AminoAcid.ArrayToString(QuerySequence.Sequence);

            if (tem_pos != 0 || query_pos != 0) {
                if (tem_pos != 0) buffer1.Append("... ");
                else buffer1.Append("    ");
                if (query_pos != 0) buffer2.Append("... ");
                else buffer2.Append("    ");
            }

            foreach (MatchPiece element in Alignment) {
                switch (element) {
                    case Match match:
                        buffer1.Append(tSeq.Substring(tem_pos, match.Length));
                        buffer2.Append(qSeq.Substring(query_pos, match.Length));
                        tem_pos += match.Length;
                        query_pos += match.Length;
                        break;
                    case Insertion gapC:
                        buffer1.Append(new string('-', gapC.Length));
                        buffer2.Append(qSeq.Substring(query_pos, gapC.Length));
                        query_pos += gapC.Length;
                        break;
                    case Deletion gapT:
                        buffer1.Append(tSeq.Substring(tem_pos, gapT.Length));
                        buffer2.Append(new string('-', gapT.Length));
                        tem_pos += gapT.Length;
                        break;
                }
            }

            if (tem_pos != tSeq.Length || query_pos != qSeq.Length) {
                if (tem_pos != tSeq.Length) buffer1.Append(" ...");
                else buffer1.Append("    ");
                if (query_pos != qSeq.Length) buffer2.Append(" ...");
                else buffer2.Append("    ");
            }

            var seq1 = buffer1.ToString();
            var seq2 = buffer2.ToString();
            const int block = 80;
            var blocks = seq1.Length / block + (seq1.Length % block == 0 ? 0 : 1);

            for (int i = 0; i < blocks; i++) {
                buffer.Append(seq1.Substring(i * block, Math.Min(block, seq1.Length - i * block)));
                //buffer.Append($"{new string(' ', 2 + block - Math.Min(block, seq2.Length - i * block))}{i * block + Math.Min(block, seq1.Length - i * block) + StartTemplatePosition}\n");
                buffer.Append('\n');
                buffer.Append(seq2.Substring(i * block, Math.Min(block, seq2.Length - i * block)));
                //buffer.Append($"{new string(' ', 2 + block - Math.Min(block, seq2.Length - i * block))}{i * block + Math.Min(block, seq2.Length - i * block) + StartQueryPosition}\n");
                buffer.Append('\n');
                if (i != blocks) {
                    buffer.Append('\n');
                }
            }

            return buffer.ToString();
        }

        /// <summary> Finds if the whole match is only based on Xs in the template </summary>
        /// <returns></returns>
        public bool AllGap() {
            int tem_pos = StartTemplatePosition;
            int query_pos = StartQueryPosition;

            foreach (MatchPiece element in Alignment) {
                switch (element) {
                    case Match match:
                        if (!Template.Sequence.Sequence.SubArray(tem_pos, match.Length).All(a => a.Character == 'X')) return false;
                        tem_pos += match.Length;
                        query_pos += match.Length;
                        break;
                    case Insertion gapC:
                        query_pos += gapC.Length;
                        break;
                    case Deletion gapT:
                        tem_pos += gapT.Length;
                        break;
                }
            }

            return true;
        }

        /// <summary> Simplifies the MatchList, so combines MatchPieces of the same kind which are in sequence with each other </summary>
        public void Simplify() {
            MatchPiece lastElement = null;
            int count = Alignment.Count;
            int i = 0;
            while (i < count) {
                if (lastElement != null && lastElement.GetType() == Alignment[i].GetType()) {
                    Alignment[i].Length += Alignment[i - 1].Length;
                    Alignment.RemoveAt(i - 1);
                    i--;
                    count--;
                } else if (Alignment[i].Length == 0) {
                    Alignment.RemoveAt(i);
                    i--;
                    count--;
                }
                lastElement = i < 0 ? null : Alignment[i];
                i++;
            }
        }

        /// <summary> Get a part of the query sequence as indicated by positions on the template sequence. </summary>
        /// <param name="startTemplatePosition">The start position on the template.</param>
        /// <param name="lengthOnTemplate">The length on the template of the part of the query sequence needed.</param>
        /// <returns></returns>
        public string GetQuerySubMatch(int startTemplatePosition, int lengthOnTemplate) {
            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;
            var buf = new StringBuilder();

            if (pos > startTemplatePosition) buf.Append(new string(Alphabet.GapChar, pos - startTemplatePosition));

            foreach (var piece in this.Alignment) {
                if (pos > startTemplatePosition + lengthOnTemplate) break;
                if (piece is SequenceMatch.Match ma) {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos + piece.Length > startTemplatePosition) {
                        int dif = pos < startTemplatePosition ? startTemplatePosition - pos : 0; // Skip all AA before the interesting sequence
                        int length = Math.Min(Math.Min(Math.Min(startTemplatePosition + lengthOnTemplate - pos, lengthOnTemplate), this.QuerySequence.Sequence.Length - q_pos - dif), piece.Length - dif);
                        buf.Append(AminoAcid.ArrayToString(this.QuerySequence.Sequence.SubArray(q_pos + dif, length)));
                    }

                    pos += piece.Length;
                    q_pos += piece.Length;
                } else if (piece is SequenceMatch.Deletion gt) {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos + piece.Length > startTemplatePosition) {
                        int length = Math.Min(pos + piece.Length - startTemplatePosition, lengthOnTemplate);
                        buf.Append(new string(Alphabet.GapChar, length));
                    }

                    pos += piece.Length;
                } else if (piece is SequenceMatch.Insertion gc) {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos > startTemplatePosition) {
                        buf.Append(AminoAcid.ArrayToString(this.QuerySequence.Sequence.SubArray(q_pos, piece.Length)));
                    }

                    q_pos += piece.Length;
                }
            }

            if (pos < startTemplatePosition + lengthOnTemplate) buf.Append(new string(Alphabet.GapChar, startTemplatePosition + lengthOnTemplate - pos));

            return buf.ToString();
        }

        (int Matches, int MisMatches, int GapInQuery, int GapInTemplate)? scores = null;
        /// <summary> Get a detailed breakdown of the scores of this match. </summary>
        /// <returns> A tuple with 4 counts, the first is the number of exact matches, the second 
        /// is the number of not exact matches (or mismatches), the third is the number of 
        /// GapInQuery, and the fourth is the number of GapInTemplate.</returns>
        public (int Matches, int MisMatches, int GapInQuery, int GapInTemplate) GetDetailedScores() {
            if (scores != null) return scores.Value;
            (int Matches, int MisMatches, int GapInQuery, int GapInTemplate) output = (0, 0, 0, 0);
            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;

            foreach (var piece in this.Alignment) {
                if (piece is SequenceMatch.Match ma) {
                    for (int i = 0; i < ma.Length; i++) {
                        if (Template.Sequence.Sequence[pos + i] == QuerySequence.Sequence[q_pos + i])
                            output.Matches += 1;
                        else
                            output.MisMatches += 1;
                    }

                    pos += piece.Length;
                    q_pos += piece.Length;
                } else if (piece is SequenceMatch.Deletion) {
                    output.GapInTemplate += piece.Length;
                    pos += piece.Length;
                } else if (piece is SequenceMatch.Insertion) {
                    output.GapInQuery += piece.Length;
                    q_pos += piece.Length;
                }
            }

            scores = output;
            return output;
        }

        /// <summary> Get the aminoacid of the query sequence at the given template sequence position. </summary>
        /// <param name="templatePosition">The position to get the AminoAcid from.</param>
        /// <returns>The AminoAcid or null if it could not be found.</returns>
        public AminoAcid? GetAtTemplateIndex(int templatePosition) {
            if (templatePosition < this.StartTemplatePosition || templatePosition > this.StartTemplatePosition + this.LengthOnTemplate) return null;

            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;

            foreach (var piece in this.Alignment) {
                if (piece is SequenceMatch.Match ma) {
                    pos += piece.Length;
                    q_pos += piece.Length;
                    if (pos > templatePosition) return this.QuerySequence.Sequence[q_pos - (pos - templatePosition)];
                } else if (piece is SequenceMatch.Deletion) {
                    pos += piece.Length;
                    if (pos > templatePosition) return this.QuerySequence.Sequence[q_pos];
                } else if (piece is SequenceMatch.Insertion) {
                    q_pos += piece.Length;
                }
            }

            return null;
        }
        /// <summary> Get the aminoacid of the query sequence at the given template sequence position. </summary>
        /// <param name="templatePosition">The position to get the AminoAcid from.</param>
        /// <returns>The AminoAcid or null if it could not be found.</returns>
        public int GetGapAtTemplateIndex(int templatePosition) {
            if (templatePosition < this.StartTemplatePosition || templatePosition > this.StartTemplatePosition + this.LengthOnTemplate) return 0;

            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;

            foreach (var piece in this.Alignment) {
                if (piece is SequenceMatch.Match ma) {
                    pos += piece.Length;
                    q_pos += piece.Length;
                    if (pos > templatePosition) return 0;
                } else if (piece is SequenceMatch.Deletion) {
                    pos += piece.Length;
                    if (pos > templatePosition) return 0;
                } else if (piece is SequenceMatch.Insertion) {
                    q_pos += piece.Length;
                    if (pos == templatePosition) return piece.Length;
                }
            }

            return 0;
        }


        /// <summary> Represents a piece of a match between two sequences </summary>
        public abstract class MatchPiece {
            /// <summary> The length of this piece (amount of AminoAcids) </summary> 
            public int Length;

            /// <summary> Creates a new piece </summary> 
            /// <param name="length">The length</param>
            public MatchPiece(int length) {
                if (length < 0) throw new ArgumentException("The length of a sequence match piece cannot be less then zero.");
                Length = length;
            }

            /// <summary> The Identifier to put in the string representation </summary> 
            abstract protected string Identifier();

            /// <summary> The CIGAR representation of this piece </summary> 
            public override string ToString() {
                return $"{Length}{Identifier()}";
            }
        }

        /// <summary> A (mis)match </summary>
        public class Match : MatchPiece {
            public Match(int c) : base(c) { }
            override protected string Identifier() { return "M"; }
        }

        /// <summary> A gap in respect to the Template. 
        /// Can be seen as a deletion in respect to the template sequence.
        /// `TEMPLATETEMPLATE`
        /// `QUERY......QUERY` </summary>
        public class Deletion : MatchPiece {
            public Deletion(int c) : base(c) { }
            override protected string Identifier() { return "D"; }
        }

        /// <summary> A gap in respect to the Query.
        /// Can be seen as an insertion in respect to the template sequence.
        /// `TEM....TEMPLATE`
        /// `QUERYQUERYQUERY` </summary>
        public class Insertion : MatchPiece {
            public Insertion(int c) : base(c) { }
            override protected string Identifier() { return "I"; }
        }
    }
}