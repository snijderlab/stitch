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
        /// The alignment of the match, consisting of <see cref="MatchPiece">s.
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
        public MetaData.IMetaData MetaData;

        /// <summary>
        /// The total amount of (mis)matching aminoacids in the alignment
        /// </summary>
        public readonly int TotalMatches;

        /// <summary>
        /// The total length on the template (matches + gaps in query)
        /// </summary>
        public readonly int LengthOnTemplate;

        public readonly int Index;

        public SequenceMatch(int startTemplatePosition, int startQueryPosition, int score, List<MatchPiece> alignment, AminoAcid[] templateSequence, AminoAcid[] querySequence, MetaData.IMetaData metadata, int index)
        {
            StartTemplatePosition = startTemplatePosition;
            StartQueryPosition = startQueryPosition;
            Score = score;
            Alignment = alignment;
            TemplateSequence = templateSequence;
            QuerySequence = querySequence;
            MetaData = metadata;
            Index = index;
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
                buffer.Append("\n");
                buffer.Append(seq2.Substring(i * block, Math.Min(block, seq2.Length - i * block)));
                //buffer.Append($"{new string(' ', 2 + block - Math.Min(block, seq2.Length - i * block))}{i * block + Math.Min(block, seq2.Length - i * block) + StartQueryPosition}\n");
                buffer.Append("\n");
                if (i != blocks)
                {
                    buffer.Append("\n");
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Simplifies the MatchList, so combines MatchPieces of the same kind which are in sequence with each other
        /// </summary>
        void Simplify()
        {
            MatchPiece lastElement = null;
            int count = Alignment.Count();
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
        /// A gap in the template
        /// </summary>
        public class GapInTemplate : MatchPiece
        {
            public GapInTemplate(int c) : base(c) { }
            override protected string Identifier() { return "D"; }
        }

        /// <summary>
        /// A gap in the query
        /// </summary>
        public class GapInQuery : MatchPiece
        {
            public GapInQuery(int c) : base(c) { }
            override protected string Identifier() { return "I"; }
        }
    }
}