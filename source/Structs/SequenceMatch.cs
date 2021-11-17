using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyNameSpace
{
    /// <summary>A class to save a match of two sequences in a space efficient way, based on CIGAR strings.</summary>
    public class SequenceMatch
    {
        /// <summary>The position on the template where the match begins.</summary>
        public readonly int StartTemplatePosition;

        /// <summary>
        /// The position on the query sequence where the match begins
        /// </summary>
        public readonly int StartQueryPosition;

        /// <summary>
        /// The score of the match
        /// </summary>
        public readonly int Score;

        /// <summary>
        /// The alignment of the match, consisting of <see cref="MatchPiece">MatchPieces</see>.
        /// </summary>
        public readonly List<MatchPiece> Alignment;

        /// <summary>
        /// The sequence of the template
        /// </summary>
        public AminoAcid[] TemplateSequence;

        /// <summary>
        /// The sequence of the query
        /// </summary>
        public AminoAcid[] QuerySequence;

        /// <summary>
        /// If provided the path that is aligned.
        /// </summary>
        public ReadMetaData.IMetaData MetaData;

        /// <summary>
        /// The total amount of (mis)matching aminoacids in the alignment
        /// </summary>
        public readonly int TotalMatches;
        /// <summary>
        /// The total length on the template (matches + gaps in query)
        /// </summary>
        public readonly int LengthOnTemplate;

        public readonly int Index;
        /// <summary> The index of the Template sequence if available. </summary>
        public readonly int TemplateIndex;
        public bool Unique;

        public SequenceMatch(int startTemplatePosition, int startQueryPosition, int score, List<MatchPiece> alignment, AminoAcid[] templateSequence, AminoAcid[] querySequence, ReadMetaData.IMetaData metadata, int index, int templateIndex = -1)
        {
            StartTemplatePosition = startTemplatePosition;
            StartQueryPosition = startQueryPosition;
            Score = score;
            Alignment = alignment;
            TemplateSequence = templateSequence;
            QuerySequence = querySequence;
            MetaData = metadata;
            Index = index;
            TemplateIndex = templateIndex;
            Simplify();

            int sum1 = 0;
            int sum2 = 0;
            foreach (var m in Alignment)
            {
                if (m is SequenceMatch.Match match)
                {
                    sum1 += match.Length;
                    sum2 += match.Length;
                }
                if (m is SequenceMatch.GapInQuery gc) sum2 += gc.Length;
            }
            TotalMatches = sum1;
            LengthOnTemplate = sum2;
        }

        /// <summary>
        /// Visualises this SequenceMatch, with a very simple visualisation of the alignment
        /// </summary>
        public override string ToString()
        {
            var buffer = new StringBuilder();
            var buffer1 = new StringBuilder();
            var buffer2 = new StringBuilder();
            buffer.Append($"SequenceMatch:\n\tStarting at template: {StartTemplatePosition}\n\tStarting at query: {StartQueryPosition}\n\tScore: {Score}\n\tMatch: {Alignment.CIGAR()}\n\n");
            int tem_pos = StartTemplatePosition;
            int query_pos = StartQueryPosition;
            string tSeq = AminoAcid.ArrayToString(TemplateSequence);
            string qSeq = AminoAcid.ArrayToString(QuerySequence);

            if (tem_pos != 0 || query_pos != 0)
            {
                if (tem_pos != 0) buffer1.Append("... ");
                else buffer1.Append("    ");
                if (query_pos != 0) buffer2.Append("... ");
                else buffer2.Append("    ");
            }

            foreach (MatchPiece element in Alignment)
            {
                switch (element)
                {
                    case Match match:
                        buffer1.Append(tSeq.Substring(tem_pos, match.Length));
                        buffer2.Append(qSeq.Substring(query_pos, match.Length));
                        tem_pos += match.Length;
                        query_pos += match.Length;
                        break;
                    case GapInQuery gapC:
                        buffer1.Append(new string('-', gapC.Length));
                        buffer2.Append(qSeq.Substring(query_pos, gapC.Length));
                        query_pos += gapC.Length;
                        break;
                    case GapInTemplate gapT:
                        buffer1.Append(tSeq.Substring(tem_pos, gapT.Length));
                        buffer2.Append(new string('-', gapT.Length));
                        tem_pos += gapT.Length;
                        break;
                }
            }

            if (tem_pos != tSeq.Length || query_pos != qSeq.Length)
            {
                if (tem_pos != tSeq.Length) buffer1.Append(" ...");
                else buffer1.Append("    ");
                if (query_pos != qSeq.Length) buffer2.Append(" ...");
                else buffer2.Append("    ");
            }

            var seq1 = buffer1.ToString();
            var seq2 = buffer2.ToString();
            const int block = 80;
            var blocks = seq1.Length / block + (seq1.Length % block == 0 ? 0 : 1);

            for (int i = 0; i < blocks; i++)
            {
                buffer.Append(seq1.Substring(i * block, Math.Min(block, seq1.Length - i * block)));
                //buffer.Append($"{new string(' ', 2 + block - Math.Min(block, seq2.Length - i * block))}{i * block + Math.Min(block, seq1.Length - i * block) + StartTemplatePosition}\n");
                buffer.Append('\n');
                buffer.Append(seq2.Substring(i * block, Math.Min(block, seq2.Length - i * block)));
                //buffer.Append($"{new string(' ', 2 + block - Math.Min(block, seq2.Length - i * block))}{i * block + Math.Min(block, seq2.Length - i * block) + StartQueryPosition}\n");
                buffer.Append('\n');
                if (i != blocks)
                {
                    buffer.Append('\n');
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Finds if the whole match is only based on Xs in the template
        /// </summary>
        /// <returns></returns>
        public bool AllGap()
        {
            int tem_pos = StartTemplatePosition;
            int query_pos = StartQueryPosition;

            foreach (MatchPiece element in Alignment)
            {
                switch (element)
                {
                    case Match match:
                        if (!TemplateSequence.SubArray(tem_pos, match.Length).All(a => a.Character == 'X')) return false;
                        tem_pos += match.Length;
                        query_pos += match.Length;
                        break;
                    case GapInQuery gapC:
                        query_pos += gapC.Length;
                        break;
                    case GapInTemplate gapT:
                        tem_pos += gapT.Length;
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Simplifies the MatchList, so combines MatchPieces of the same kind which are in sequence with each other
        /// </summary>
        void Simplify()
        {
            MatchPiece lastElement = null;
            int count = Alignment.Count;
            int i = 0;
            while (i < count)
            {
                if (lastElement != null && lastElement.GetType() == Alignment[i].GetType())
                {
                    Alignment[i].Length += Alignment[i - 1].Length;
                    Alignment.RemoveAt(i - 1);
                    i--;
                    count--;
                }
                lastElement = Alignment[i];
                i++;
            }
        }

        /// <summary>
        /// Get a part of the query sequence as indicated by positions on the template sequence.
        /// </summary>
        /// <param name="startTemplatePosition">The start position on the template.</param>
        /// <param name="lengthOnTemplate">The length on the template of the part of the query sequence needed.</param>
        /// <returns></returns>
        public string GetQuerySubMatch(int startTemplatePosition, int lengthOnTemplate)
        {
            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;
            var buf = new StringBuilder();

            if (pos > startTemplatePosition) buf.Append(new string(Alphabet.GapChar, pos - startTemplatePosition));

            foreach (var piece in this.Alignment)
            {
                if (pos > startTemplatePosition + lengthOnTemplate) break;
                if (piece is SequenceMatch.Match ma)
                {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos + piece.Length > startTemplatePosition)
                    {
                        int dif = pos < startTemplatePosition ? startTemplatePosition - pos : 0; // Skip all AA before the interesting sequence
                        int length = Math.Min(Math.Min(Math.Min(startTemplatePosition + lengthOnTemplate - pos, lengthOnTemplate), this.QuerySequence.Length - q_pos - dif), piece.Length - dif);
                        buf.Append(AminoAcid.ArrayToString(this.QuerySequence.SubArray(q_pos + dif, length)));
                    }

                    pos += piece.Length;
                    q_pos += piece.Length;
                }
                else if (piece is SequenceMatch.GapInTemplate gt)
                {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos + piece.Length > startTemplatePosition)
                    {
                        int length = Math.Min(pos + piece.Length - startTemplatePosition, lengthOnTemplate);
                        buf.Append(new string(Alphabet.GapChar, length));
                    }

                    pos += piece.Length;
                }
                else if (piece is SequenceMatch.GapInQuery gc)
                {
                    if (pos < startTemplatePosition + lengthOnTemplate && pos > startTemplatePosition)
                    {
                        buf.Append(AminoAcid.ArrayToString(this.QuerySequence.SubArray(q_pos, piece.Length)));
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
        public (int Matches, int MisMatches, int GapInQuery, int GapInTemplate) GetDetailedScores()
        {
            if (scores != null) return scores.Value;
            (int Matches, int MisMatches, int GapInQuery, int GapInTemplate) output = (0, 0, 0, 0);
            int pos = this.StartTemplatePosition;
            int q_pos = this.StartQueryPosition;

            foreach (var piece in this.Alignment)
            {
                if (piece is SequenceMatch.Match ma)
                {
                    for (int i = 0; i < ma.Length; i++)
                    {
                        if (TemplateSequence[pos + i] == QuerySequence[q_pos + i])
                            output.Matches += 1;
                        else
                            output.MisMatches += 1;
                    }

                    pos += piece.Length;
                    q_pos += piece.Length;
                }
                else if (piece is SequenceMatch.GapInTemplate)
                {
                    output.GapInTemplate += piece.Length;
                    pos += piece.Length;
                }
                else if (piece is SequenceMatch.GapInQuery)
                {
                    output.GapInQuery += piece.Length;
                    q_pos += piece.Length;
                }
            }

            scores = output;
            return output;
        }


        /// <summary>
        /// Represents a piece of a match between two sequences
        /// </summary>
        public abstract class MatchPiece
        {
            /// <summary>
            /// The length of this piece (amount of AminoAcids)
            /// </summary>
            public int Length;

            /// <summary>
            /// Creates a new piece
            /// </summary>
            /// <param name="length">The length</param>
            public MatchPiece(int length)
            {
                Length = length;
            }

            /// <summary>
            /// The Identifier to put in the string representation
            /// </summary>
            abstract protected string Identifier();

            /// <summary>
            /// The CIGAR representation of this piece
            /// </summary>
            public override string ToString()
            {
                return $"{Length}{Identifier()}";
            }
        }

        /// <summary>
        /// A (mis)match
        /// </summary>
        public class Match : MatchPiece
        {
            public Match(int c) : base(c) { }
            override protected string Identifier() { return "M"; }
        }

        /// <summary>
        /// A gap in respect to the Template. 
        /// Can be seen as a deletion in respect to the template sequence.
        /// `TEMPLATETEMPLATE`
        /// `QUERY......QUERY`
        /// </summary>
        public class GapInTemplate : MatchPiece
        {
            public GapInTemplate(int c) : base(c) { }
            override protected string Identifier() { return "D"; }
        }

        /// <summary>
        /// A gap in respect to the Query.
        /// Can be seen as an insertion in respect to the template sequence.
        /// `TEM....TEMPLATE`
        /// `QUERYQUERYQUERY`
        /// </summary>
        public class GapInQuery : MatchPiece
        {
            public GapInQuery(int c) : base(c) { }
            override protected string Identifier() { return "I"; }
        }
    }
}