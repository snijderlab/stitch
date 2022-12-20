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
        public readonly Read.IRead ReadA;
        public readonly int ReadAIndex;
        public readonly Read.IRead ReadB;
        public bool Unique;
        public readonly int LenA;
        public readonly int LenB;
        public readonly int Identical;
        public readonly int MisMatches;
        public readonly int Similar;
        public readonly int GapInA;
        public readonly int GapInB;

        public Alignment(Read.IRead read_a, Read.IRead read_b, ScoringMatrix alphabet, AlignmentType type, int readAIndex = 0) {
            var seq_a = read_a.Sequence.Sequence;
            var seq_b = read_b.Sequence.Sequence;
            this.ReadA = read_a;
            this.ReadAIndex = readAIndex;
            this.ReadB = read_b;
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
            //var values = new List<AlignmentPiece>(alphabet.Size * alphabet.Size + 2);
            for (int index_a = 1; index_a <= seq_a.Length; index_a++) {
                for (int index_b = 1; index_b <= seq_b.Length; index_b++) {
                    //values.Clear(); // Reuse the values memory
                    AlignmentPiece value = new AlignmentPiece();
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
                            if (value.Score < total_score)
                                value = new AlignmentPiece(total_score, score, len_a, len_b);
                        }
                    }
                    // Select the best move
                    //var value = values.MaxBy(v => v.Score);
                    if (value.Score > high.score)
                        high = (value.Score, index_a, index_b);
                    matrix[index_a, index_b] = value;
                }
            }

            // Find the correct starting point for crawl back
            if (type == AlignmentType.Global) {
                high = (matrix[seq_a.Length, seq_b.Length].Score, seq_a.Length, seq_b.Length);
            } else if (type == AlignmentType.GlobalForB) {
                var value = Enumerable.Range(0, seq_a.Length + 1).Select(index => (index, matrix[index, seq_b.Length].Score)).MaxBy(v => v.Item2);
                high = (value.Item2, value.index, seq_b.Length);
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
                    if (ReadA.Sequence.Sequence[this.LenA + this.StartA] == ReadB.Sequence.Sequence[this.LenB + this.StartB])
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

        string Aligned() {
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
                    str_a.Append(AminoAcid.ArrayToString(this.ReadA.Sequence.Sequence.SubArray(loc_a, piece.StepA)).PadLeft(l, '·'));
                }
                if (piece.StepB == 0) {
                    str_b.Append(new string('-', l));
                } else {
                    str_b.Append(AminoAcid.ArrayToString(this.ReadB.Sequence.Sequence.SubArray(loc_b, piece.StepB)).PadLeft(l, '·'));
                }
                str_blocks.Append(piece.LocalScore < 0 || piece.LocalScore >= blocks.Length ? new string(' ', l) : new string(blocks[piece.LocalScore], l));
                str_blocks_neg.Append(piece.LocalScore >= 0 || -piece.LocalScore >= blocks_neg.Length ? new string(' ', l) : new string(blocks_neg[-piece.LocalScore], l));
                loc_a += piece.StepA;
                loc_b += piece.StepB;
            }

            return $"{str_a}\n{str_b}\n{str_blocks}\n{str_blocks_neg}";
        }

        public string Summary() {
            return $"score: {Score}\nidentity: {PercentIdentity():P2}\npath: {ShortPath()}\nstart: ({StartA}, {StartB})\naligned:\n{Aligned()}";
        }

        public double PercentIdentity() {
            return (double)this.Identical / (double)this.LenA;
        }

        public AminoAcid? GetAtTemplateIndex(int position) {
            if (position < this.StartA || position > this.StartA + this.LenA) return null;
            int pos_a = this.StartA;
            int pos_b = this.StartB;
            if (position == this.StartA) return this.ReadB.Sequence.Sequence[pos_b];

            foreach (var piece in this.Path) {
                pos_a += piece.StepA;
                pos_b += piece.StepB;
                if (pos_a >= position) return this.ReadB.Sequence.Sequence[Math.Min(pos_b, this.ReadB.Sequence.Sequence.Length - 1)]; // Match, deletion
            }

            return null;
        }

        public AminoAcid[] GetQuerySubMatch(int start_position, int length) {
            var output = new List<AminoAcid>();
            int pos_a = this.StartA;
            int pos_b = this.StartB;

            foreach (var piece in this.Path) {
                if (pos_a > start_position && pos_a <= start_position + length)
                    output.AddRange(this.ReadB.Sequence.Sequence.SubArray(pos_b, piece.StepB)); // TODO: this does not handle sets at the boundaries gracefully.
                pos_a += piece.StepA;
                pos_b += piece.StepB;
            }

            return output.ToArray();
        }

        /// <summary> End align two sequences </summary>
        /// <param name="template">The front sequence</param>
        /// <param name="query">The tail sequence</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="maxOverlap">The maximal length of the overlap</param>
        /// <returns>A tuple with the best position and its score</returns>
        public static ((int Position, int Score) Best, List<(int Position, int Score)> Scores) EndAlignment(AminoAcid[] template, AminoAcid[] query, ScoringMatrix alphabet, int maxOverlap) {
            var scores = new List<(int, int)>();
            for (int i = 1; i < maxOverlap && i < query.Length && i < template.Length; i++) {
                var score = new Alignment(new Read.Simple(template.TakeLast(i).ToArray()), new Read.Simple(query.Take(i).ToArray()), alphabet, AlignmentType.Global);
                //AminoAcid.ArrayHomology(template.TakeLast(i).ToArray(), query.Take(i).ToArray(), alphabet) - (2 * i);
                scores.Add((i, score.Score));
            }
            if (scores.Count == 0) return ((0, 0), scores);

            var best = scores[0];
            foreach (var item in scores)
                if (item.Item2 > best.Item2) best = item;
            return (best, scores);
        }
    }
}