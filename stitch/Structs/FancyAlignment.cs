using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stitch {

    public enum AlignmentType {
        Global,
        GlobalForB,
        Local
    }

    public struct AlignmentPiece {
        public int score;
        public sbyte local_score;
        public byte step_a;
        public byte step_b;

        public AlignmentPiece() {
            this.score = 0;
            this.local_score = 0;
            this.step_a = 0;
            this.step_b = 0;
        }

        public AlignmentPiece(int score, sbyte local_score, byte step_a, byte step_b) {
            this.score = score;
            this.local_score = local_score;
            this.step_a = step_a;
            this.step_b = step_b;
        }

        public string Short() {
            return (this.step_a, this.step_b) switch {
                (0, 1) => "I",
                (1, 0) => "D",
                (1, 1) => "M",
                _ => $"S[{this.step_a},{this.step_b}]",
            };
        }
    }

    public class FancyAlignment {
        public readonly int score;
        public readonly List<AlignmentPiece> path;
        public readonly int start_a;
        public readonly int start_b;
        public readonly Read.IRead read_a;
        public readonly Read.IRead read_b;

        public FancyAlignment(Read.IRead read_a, Read.IRead read_b, FancyAlphabet alphabet, AlignmentType type) {
            var seq_a = read_a.Sequence.Sequence;
            var seq_b = read_b.Sequence.Sequence;
            this.read_a = read_a;
            this.read_b = read_b;
            var matrix = new AlignmentPiece[seq_a.Length + 1, seq_b.Length + 1];
            (int score, int index_a, int index_b) high = (0, 0, 0);

            // Set up the gaps for the B side, used to guide the path back to (0,0) when the actual alignment already ended at the B side.
            if (type == AlignmentType.Global || type == AlignmentType.GlobalForB) {
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
            var values = new List<AlignmentPiece>(alphabet.Size * alphabet.Size + 2);
            for (int index_a = 1; index_a <= seq_a.Length; index_a++) {
                for (int index_b = 1; index_b <= seq_b.Length; index_b++) {
                    values.Clear(); // Reuse the values memory
                                    // List all possible moves
                    for (byte len_a = 0; len_a <= alphabet.Size; len_a++) {
                        for (byte len_b = 0; len_b <= alphabet.Size; len_b++) {
                            if (len_a == 0 && len_b != 1 || len_b == 0 && len_a != 1 || len_a > index_a || len_b > index_b)
                                continue; // Skip combined gaps, the point 0,0, and too big steps (outside bounds)

                            var previous = matrix[index_a - len_a, index_b - len_b];
                            sbyte score = len_a == 0 || len_b == 0
                                ? (previous.step_a == 0 || previous.step_b == 0 ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty)
                                : alphabet.Score(seq_a.SubArray(index_a - len_a, len_a), seq_b.SubArray(index_b - len_b, len_b));

                            if (score == 0)
                                continue; // Skip undefined pairs

                            values.Add(new AlignmentPiece(previous.score + (int)score, score, len_a, len_b));
                        }
                    }
                    // Select the best move
                    var value = values.MaxBy(v => v.score);
                    if (value.score > high.score)
                        high = (value.score, index_a, index_b);
                    matrix[index_a, index_b] = value;
                }
            }

            // Find the correct starting point for crawl back
            if (type == AlignmentType.Global) {
                high = (matrix[seq_a.Length, seq_b.Length].score, seq_a.Length, seq_b.Length);
            } else if (type == AlignmentType.Global) {
                var value = Enumerable.Range(0, seq_a.Length + 1).Select(index => (index, matrix[index, seq_b.Length].score)).MaxBy(v => v.Item2);
                high = (value.Item2, value.index, seq_b.Length);
            }
            this.score = high.score;

            // Do the crawl back
            this.path = new List<AlignmentPiece>();
            while (!(high.index_a == 0 && high.index_b == 0)) {
                var value = matrix[high.index_a, high.index_b];
                if (value.step_a == 0 && value.step_b == 0) break; // Catch the end for a Local alignment
                high = (0, high.index_a - value.step_a, high.index_b - value.step_b);
                path.Add(value);
            }
            path.Reverse();
            this.start_a = high.index_a;
            this.start_b = high.index_b;
        }

        public string Short() {
            return String.Join("", this.path.Select(p => p.Short()));
        }

        string Aligned() {
            var blocks = " ▁▂▃▄▅▆▇█".ToCharArray();
            var blocks_neg = " ▔▔▔▀▀▀▀█".ToCharArray();
            var str_a = new StringBuilder();
            var str_b = new StringBuilder();
            var str_blocks = new StringBuilder();
            var str_blocks_neg = new StringBuilder();
            var loc_a = start_a;
            var loc_b = start_b;

            foreach (var piece in path) {
                var l = Math.Max(piece.step_a, piece.step_b);
                if (piece.step_a == 0) {
                    str_a.Append(new string('-', l));
                } else {
                    str_a.Append(AminoAcid.ArrayToString(this.read_a.Sequence.Sequence.SubArray(loc_a, piece.step_a)).PadLeft(l, '-'));
                }
                if (piece.step_b == 0) {
                    str_b.Append(new string('-', l));
                } else {
                    str_b.Append(AminoAcid.ArrayToString(this.read_b.Sequence.Sequence.SubArray(loc_b, piece.step_b)).PadLeft(l, '-'));
                }
                str_blocks.Append(piece.local_score < 0 ? new string(' ', l) : new string(blocks[piece.local_score], l));
                str_blocks_neg.Append(piece.local_score >= 0 ? new string(' ', l) : new string(blocks_neg[-piece.local_score], l));
                loc_a += piece.step_a;
                loc_b += piece.step_b;
            }

            return $"{str_a}\n{str_b}\n{str_blocks}\n{str_blocks_neg}";
        }

        public string Summary() {
            return $"score: {score}\npath: {Short()}\nstart: ({start_a}, {start_b})\naligned:\n{Aligned()}";
        }
    }
}