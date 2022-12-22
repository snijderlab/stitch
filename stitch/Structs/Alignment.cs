using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stitch {

    public enum AlignmentType {
        /// <summary> Align both sequences fully to each other, there can be no dangling patches of sequence on any side of any sequence. </summary>
        Global,
        /// <summary> Align the read (sequence B, 2nd argument) fully to the template (sequence A, 1st argument) only the template can have dangling patches of sequence at any side.</summary>
        GlobalForB,
        /// <summary> Align the best matching patches of both sequences, both can have dangling patches of sequence at any side. </summary>
        Local,
        /// <summary> Align the ends of two sequences, the left sequence (1st argument) can have dangling patches of sequence on the left, but has to be fully placed on the right while the right sequence (2nd argument) works the other way around. </summary>
        EndAlignment,
    }

    public struct AlignmentPiece {
        public int Score;
        public sbyte LocalScore;
        public byte StepA;
        public byte StepB;

        public AlignmentPiece() {
            this.Score = 0;
            this.LocalScore = 0;
            this.StepA = 0;
            this.StepB = 0;
        }

        public AlignmentPiece(int score, sbyte local_score, byte step_a, byte step_b) {
            this.Score = score;
            this.LocalScore = local_score;
            this.StepA = step_a;
            this.StepB = step_b;
        }

        public string ShortPath() {
            return (this.StepA, this.StepB) switch {
                (0, 0) => "*",
                (0, 1) => "I",
                (1, 0) => "D",
                (1, 1) => "M",
                _ => $"S[{this.StepA},{this.StepB}]",
            };
        }
    }

    public class Alignment {
        public readonly int Score;
        public readonly List<AlignmentPiece> Path;
        public readonly int StartA;
        public readonly int StartB;
        public readonly ReadFormat.Read ReadA;
        public readonly int ReadAIndex;
        public readonly ReadFormat.Read ReadB;
        public bool Unique;
        public readonly int LenA;
        public readonly int LenB;
        public readonly int Identical;
        public readonly int MisMatches;
        public readonly int Similar;
        public readonly int GapInA;
        public readonly int GapInB;

        public Alignment(ReadFormat.Read read_a, ReadFormat.Read read_b, ScoringMatrix alphabet, AlignmentType type, int readAIndex = 0) {
            var seq_a = read_a.Sequence.AminoAcids;
            var seq_b = read_b.Sequence.AminoAcids;
            this.ReadA = read_a;
            this.ReadAIndex = readAIndex;
            this.ReadB = read_b;
            var matrix = new AlignmentPiece[seq_a.Length + 1, seq_b.Length + 1];
            (int score, int index_a, int index_b) high = (0, 0, 0);

            // Set up the gaps for the B side, used to guide the path back to (0,0) when the actual alignment already ended at the B side.
            if (type == AlignmentType.Global || type == AlignmentType.GlobalForB || type == AlignmentType.EndAlignment) {
                for (int b = 0; b <= seq_b.Length; b++) {
                    matrix[0, b] = new AlignmentPiece(b == 0 ? 0 : b == 1 ? alphabet.GapStartPenalty : alphabet.GapStartPenalty + (b - 1) * alphabet.GapExtendPenalty,
                    b == 0 ? (sbyte)0 : b == 1 ? (sbyte)alphabet.GapStartPenalty : (sbyte)alphabet.GapExtendPenalty,
                    0,
                    b == 0 ? (byte)0 : (byte)1);
                }
            }

            // Set up starting gaps for the A side, used to guide the path back to (0,0) when the actual alignment already ended at the A side.
            if (type == AlignmentType.Global) {
                for (int a = 0; a <= seq_a.Length; a++) {
                    matrix[a, 0] = new AlignmentPiece(a == 0 ? 0 : a == 1 ? alphabet.GapStartPenalty : alphabet.GapStartPenalty + (a - 1) * alphabet.GapExtendPenalty,
                    a == 0 ? (sbyte)0 : a == 1 ? (sbyte)alphabet.GapStartPenalty : (sbyte)alphabet.GapExtendPenalty,
                    a == 0 ? (byte)0 : (byte)1,
                    0);
                }
            }

            // Fill the matrix with the best move for each position
            for (int index_a = 1; index_a <= seq_a.Length; index_a++) {
                for (int index_b = 1; index_b <= seq_b.Length; index_b++) {
                    AlignmentPiece value = new AlignmentPiece();
                    var changed = false;
                    // List all possible moves
                    for (byte len_a = 0; len_a <= alphabet.Size; len_a++) {
                        for (byte len_b = 0; len_b <= alphabet.Size; len_b++) {
                            if ((len_a == 0 && len_b != 1) || (len_b == 0 && len_a != 1) || (len_a > index_a) || (len_b > index_b))
                                continue; // Skip combined gaps, the point 0,0, and too big steps (outside bounds)

                            var previous = matrix[index_a - len_a, index_b - len_b];
                            sbyte score = len_a == 0 || len_b == 0
                                ? (len_a == 0 && previous.StepA == 0
                                   || len_b == 0 && previous.StepB == 0
                                   || len_a == 0 && seq_a[index_a - 1] == alphabet.GapChar
                                   || len_b == 0 && seq_b[index_b - 1] == alphabet.GapChar
                                   ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty)
                                : alphabet.Score(seq_a.SubSpan(index_a - len_a, len_a), seq_b.SubSpan(index_b - len_b, len_b));

                            if (score == 0)
                                continue; // Skip undefined pairs

                            var total_score = previous.Score + (int)score;
                            if (value.Score < total_score || !changed && type != AlignmentType.Local) {
                                value = new AlignmentPiece(total_score, score, len_a, len_b);
                                changed = true;
                            }
                        }
                    }
                    if (value.Score > high.score)
                        high = (value.Score, index_a, index_b);
                    if (value.Score < 0 && type == AlignmentType.Local)
                        value = new AlignmentPiece(0, 0, 0, 0);
                    matrix[index_a, index_b] = value;
                }
            }

            // Find the correct starting point for crawl back
            if (type == AlignmentType.Global) {
                high = (matrix[seq_a.Length, seq_b.Length].Score, seq_a.Length, seq_b.Length);
            } else if (type == AlignmentType.GlobalForB) {
                var value = Enumerable.Range(0, seq_a.Length + 1).Select(index => (index, matrix[index, seq_b.Length].Score)).MaxBy(v => v.Item2);
                high = (value.Item2, value.index, seq_b.Length);
            } else if (type == AlignmentType.EndAlignment) {
                var value = Enumerable.Range(0, seq_b.Length + 1).Select(index => (index, matrix[seq_a.Length, index].Score)).MaxBy(v => v.Item2);
                high = (value.Item2, seq_a.Length, value.index);
            }
            this.Score = high.score;

            // Do the crawl back
            this.Path = new List<AlignmentPiece>();
            while (!(high.index_a == 0 && high.index_b == 0)) {
                var value = matrix[high.index_a, high.index_b];
                if (value.StepA == 0 && value.StepB == 0) break; // Catch the end for a Local alignment
                high = (0, high.index_a - value.StepA, high.index_b - value.StepB);
                Path.Add(value);
            }
            Path.Reverse();
            this.StartA = high.index_a;
            this.StartB = high.index_b;

            // Calculate some statistics
            this.LenA = 0;
            this.LenB = 0;
            this.Identical = 0;
            this.MisMatches = 0;
            this.Similar = 0;
            this.GapInA = 0;
            this.GapInB = 0;
            foreach (var piece in Path) {
                if (piece.StepA == 1 && piece.StepB == 1) {
                    if (ReadA.Sequence.AminoAcids[this.LenA + this.StartA] == ReadB.Sequence.AminoAcids[this.LenB + this.StartB])
                        this.Identical += 1;
                    else
                        this.MisMatches += 1;
                }
                if (piece.StepA != 0 && piece.StepB != 0) this.Similar += 1;
                if (piece.StepA == 0) this.GapInA += 1;
                if (piece.StepB == 0) this.GapInB += 1;
                this.LenA += piece.StepA;
                this.LenB += piece.StepB;
            }
        }

        public string ShortPath() {
            return String.Join("", this.Path.Select(p => p.ShortPath()));
        }

        public string VeryShortPath() {
            var items = this.Path.Select(i => i.ShortPath());
            var builder = new StringBuilder();
            var previous = items.Count();
            while (items.Count() > 0) {
                var selected = items.First();
                items = items.SkipWhile(i => i == selected);
                var current = items.Count();
                builder.Append($"{previous - current}{selected}");
                previous = current;
            }
            return builder.ToString();
        }

        public (string Aligned, string Scores) Aligned() {
            var blocks = " ▁▂▃▄▅▆▇█".ToCharArray();
            var blocks_neg = " ▔▔▔▀▀▀▀█".ToCharArray();
            var str_a = new StringBuilder();
            var str_b = new StringBuilder();
            var str_blocks = new StringBuilder();
            var str_blocks_neg = new StringBuilder();
            var loc_a = StartA;
            var loc_b = StartB;

            foreach (var piece in Path) {
                var l = Math.Max(piece.StepA, piece.StepB);
                if (piece.StepA == 0) {
                    str_a.Append(new string('-', l));
                } else {
                    str_a.Append(AminoAcid.ArrayToString(this.ReadA.Sequence.AminoAcids.SubArray(loc_a, piece.StepA)).PadLeft(l, '·'));
                }
                if (piece.StepB == 0) {
                    str_b.Append(new string('-', l));
                } else {
                    str_b.Append(AminoAcid.ArrayToString(this.ReadB.Sequence.AminoAcids.SubArray(loc_b, piece.StepB)).PadLeft(l, '·'));
                }
                str_blocks.Append(piece.LocalScore < 0 || piece.LocalScore >= blocks.Length ? new string(' ', l) : new string(blocks[piece.LocalScore], l));
                str_blocks_neg.Append(piece.LocalScore >= 0 || -piece.LocalScore >= blocks_neg.Length ? new string(' ', l) : new string(blocks_neg[-piece.LocalScore], l));
                loc_a += piece.StepA;
                loc_b += piece.StepB;
            }

            return ($"{str_a}\n{str_b}", $"{str_blocks}\n{str_blocks_neg}");
        }

        public string Summary() {
            var aligned = Aligned();
            return $"score: {Score}\nidentity: {PercentIdentity():P2}\npath: {ShortPath()}\nstart: ({StartA}, {StartB})\naligned:\n{aligned.Aligned}\n{aligned.Scores}";
        }

        public double PercentIdentity() {
            return (double)this.Identical / (double)this.LenA;
        }

        public AminoAcid? GetAtTemplateIndex(int position) {
            if (position < this.StartA || position > this.StartA + this.LenA) return null;
            int pos_a = this.StartA;
            int pos_b = this.StartB;
            if (position == this.StartA) return this.ReadB.Sequence.AminoAcids[pos_b];

            foreach (var piece in this.Path) {
                pos_a += piece.StepA;
                pos_b += piece.StepB;
                if (pos_a >= position) return this.ReadB.Sequence.AminoAcids[Math.Min(pos_b, this.ReadB.Sequence.Length - 1)]; // Match, deletion
            }

            return null;
        }

        public AminoAcid[] GetQuerySubMatch(int start_position, int length) {
            var output = new List<AminoAcid>();
            int pos_a = this.StartA;
            int pos_b = this.StartB;

            foreach (var piece in this.Path) {
                if (pos_a > start_position && pos_a <= start_position + length)
                    output.AddRange(this.ReadB.Sequence.AminoAcids.SubArray(pos_b, piece.StepB)); // TODO: this does not handle sets at the boundaries gracefully.
                pos_a += piece.StepA;
                pos_b += piece.StepB;
            }

            return output.ToArray();
        }
    }
}