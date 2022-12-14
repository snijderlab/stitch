using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;

namespace Stitch {
    /// <summary> To contain an alphabet with scoring matrix to score pairs of amino acids </summary>
    public class ScoringMatrix {
        [JsonIgnore]
        /// <summary> The matrix used for scoring of the alignment between two characters in the alphabet.
        /// As such this matrix is rectangular. </summary>
        sbyte[,] Matrix;

        /// <summary> The position for each possible amino acid in the ScoringMatrix for fast lookups. </summary>
        readonly public Dictionary<char, int> PositionInScoringMatrix;

        /// <summary> The penalty for opening a gap in an alignment </summary>
        public readonly sbyte GapStartPenalty;

        /// <summary> The penalty for extending a gap in an alignment </summary>
        public readonly sbyte GapExtendPenalty;

        /// <summary> The maximum size of checked patches. </summary>
        public readonly int Size;

        /// <summary> The char that represents a gap </summary>
        public readonly char GapChar;

        /// <summary> The char that represents a stop codon, where translation will stop. </summary>
        public const char StopCodon = '*';
        int AlphabetSize;
        public readonly int Swap;
        public readonly int SymmetricScore;
        public readonly int AsymmetricScore;

        public sbyte Score(AminoAcid[] a, AminoAcid[] b) {
            return Matrix[Index(a), Index(b)];
        }
        public int Index(AminoAcid[] data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d.Character] + 1;
            }
            return index;
        }

        public sbyte Score(Span<AminoAcid> a, Span<AminoAcid> b) {
            return Matrix[Index(a), Index(b)];
        }
        public int Index(Span<AminoAcid> data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d.Character] + 1;
            }
            return index;
        }
        public sbyte Score(IEnumerable<char> a, IEnumerable<char> b) {
            return Matrix[Index(a), Index(b)];
        }

        void SetScore(IEnumerable<char> a, IEnumerable<char> b, sbyte score) {
            Matrix[Index(a), Index(b)] = score;
        }

        int Index(IEnumerable<char> data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d] + 1;
            }
            return index;
        }

        public static ScoringMatrix IdentityMatrix(List<char> alphabet, List<(sbyte score, List<List<List<char>>> sets)> symmetric_similar, List<(sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets)> asymmetric_similar, sbyte identity, sbyte mismatch, sbyte gap_start, sbyte gap_extend, sbyte swap, int size, char gap_char) {
            var matrix = new sbyte[alphabet.Count, alphabet.Count];
            for (int x = 0; x < alphabet.Count; x++)
                for (int y = 0; y < alphabet.Count; y++)
                    matrix[x, y] = x == y ? identity : mismatch;
            return new ScoringMatrix(matrix, alphabet, symmetric_similar, asymmetric_similar, gap_start, gap_extend, swap, size, gap_char);
        }

        public ScoringMatrix(sbyte[,] matrix, List<char> alphabet, List<(sbyte score, List<List<List<char>>> sets)> symmetric_similar, List<(sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets)> asymmetric_similar, sbyte gap_start, sbyte gap_extend, sbyte swap, int size, char gap_char) {
            if (matrix.GetLength(0) != alphabet.Count || matrix.GetLength(1) != alphabet.Count) throw new ArgumentException("Matrix size not fitting for given alphabet size");
            this.AlphabetSize = alphabet.Count;
            this.GapStartPenalty = gap_start;
            this.GapExtendPenalty = gap_extend;
            this.GapChar = gap_char;
            this.Size = size;
            var matrix_size = HelperFunctionality.IntPow(AlphabetSize + 1, (uint)size) + 1;
            this.Matrix = new sbyte[matrix_size, matrix_size];
            this.PositionInScoringMatrix = alphabet.Select((item, index) => (item, index)).ToDictionary(item => item.item, item => item.index);
            this.Swap = swap;

            for (int x = 0; x < alphabet.Count; x++) {
                for (int y = 0; y < alphabet.Count; y++) {
                    this.Matrix[x + 1, y + 1] = matrix[x, y];
                }
            }

            // Set all symmetric similar sets, used for iso mass in the normal case
            foreach (var super_set in symmetric_similar) {
                foreach (var set in super_set.sets) {
                    foreach ((var a, var b) in set.Where(s => s.Count <= size).Variations()) {
                        var perms_a = a.Permutations();
                        var perms_b = b.Permutations();
                        foreach (var perm_a in perms_a) {
                            foreach (var perm_b in perms_b) {
                                this.SetScore(perm_a, perm_b, super_set.score);
                            }
                        }
                    }
                }
            }

            // Set all asymmetric similar sets, used for modifications in the normal case
            foreach (var super_set in asymmetric_similar) {
                foreach (var set in super_set.sets) {
                    foreach (var a in set.from.Where(s => s.Count <= size)) {
                        foreach (var b in set.to.Where(s => s.Count <= size)) {
                            var perms_a = a.Permutations();
                            var perms_b = b.Permutations();
                            foreach (var perm_a in perms_a) {
                                foreach (var perm_b in perms_b) {
                                    this.SetScore(perm_a, perm_b, super_set.score);
                                }
                            }
                        }
                    }
                }
            }

            if (swap != 0) {
                // Set all swap scores by taking all possible combinations whit at least two different items.
                // Generate all possible permutations for these combinations.
                for (int len = size; len >= 2; len--) {
                    foreach (var set in alphabet.Combinations(len).Where(s => s.Any(i => i != s.First()))) {
                        foreach (var perm in set.Permutations()) {
                            this.SetScore(set, perm, (sbyte)(len * (int)swap));
                            this.SetScore(perm, set, (sbyte)(len * (int)swap));
                        }
                    }
                }
            }
        }

        public static ScoringMatrix Default() {
            return IdentityMatrix(
                "ARNDCQEGHILKMFPSTWYVBZX.*".ToList(),
                new List<(sbyte score, List<List<List<char>>> sets)>{(5, new List<List<List<char>>>{
                    new List<List<char>>{
                        new List<char>{
                            'I'
                        },
                        new List<char> {
                            'L'
                        }
                    },
                    new List<List<char>>{
                        new List<char>{
                            'G','G'
                        },
                        new List<char> {
                            'N'
                        }
                    },
                    new List<List<char>>{
                        new List<char>{
                            'E','V'
                        },
                        new List<char> {
                            'D','L'
                        },
                        new List<char> {
                            'D','I'
                        }
                    }
                })},
                new List<(sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets)>{(3, new List<(List<List<char>>, List<List<char>>)> {
                    (new List<List<char>>{
                        new List<char> {
                            'Q'
                        }
                    }, new List<List<char>>{
                        new List<char>{
                            'E'
                            }
                    })
                })}, 8, -1, -5, -5, 2, 3, '.'
            );
        }

        public static ScoringMatrix TestMatrix() {
            return IdentityMatrix(
                "ABC.*".ToList(),
                new List<(sbyte score, List<List<List<char>>> sets)> { (5, new List<List<List<char>>>()) },
                new List<(sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets)> { (3, new List<(List<List<char>>, List<List<char>>)>()) }, 1, 0, -12, -1, 0, 1, '.'
            );
        }

        public bool Contains(AminoAcid a) {
            return PositionInScoringMatrix.ContainsKey(a.Character);
        }
        public bool Contains(char a) {
            return PositionInScoringMatrix.ContainsKey(a);
        }

        public String Debug() {
            return $"Alphabet: {String.Join(' ', this.PositionInScoringMatrix.Keys)}";
        }
    }
}