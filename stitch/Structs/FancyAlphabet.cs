using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;

namespace Stitch {
    /// <summary> To contain an alphabet with scoring matrix to score pairs of amino acids </summary>
    public class FancyAlphabet {
        [JsonIgnore]
        /// <summary> The matrix used for scoring of the alignment between two characters in the alphabet.
        /// As such this matrix is rectangular. </summary>
        sbyte[,] ScoringMatrix;

        /// <summary> The position for each possible amino acid in the ScoringMatrix for fast lookups. </summary>
        readonly public Dictionary<char, int> PositionInScoringMatrix;

        /// <summary> The penalty for opening a gap in an alignment </summary>
        public readonly sbyte GapStartPenalty;

        /// <summary> The penalty for extending a gap in an alignment </summary>
        public readonly sbyte GapExtendPenalty;

        /// <summary> The maximum size of checked patches. </summary>
        public readonly int Size;

        /// <summary> The char that represents a gap </summary>
        public const char GapChar = '.';

        /// <summary> The char that represents a stop codon, where translation will stop. </summary>
        public const char StopCodon = '*';
        int AlphabetSize;
        public readonly int Swap;
        public readonly int SymmetricScore;
        public readonly int AsymmetricScore;

        public sbyte Score(AminoAcid[] a, AminoAcid[] b) {
            return ScoringMatrix[Index(a), Index(b)];
        }

        public int Index(AminoAcid[] data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d.Character] + 1;
            }
            return index;
        }

        void SetScore(IEnumerable<char> a, IEnumerable<char> b, sbyte score) {
            ScoringMatrix[Index(a), Index(b)] = score;
        }

        int Index(IEnumerable<char> data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d] + 1;
            }
            return index;
        }

        public static FancyAlphabet IdentityMatrix(List<char> alphabet, (sbyte score, List<List<List<char>>> sets) symmetric_similar, (sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets) asymmetric_similar, sbyte identity, sbyte mismatch, sbyte gap_start, sbyte gap_extend, sbyte swap, int size) {
            var matrix = new sbyte[alphabet.Count + 1, alphabet.Count + 1];
            for (int x = 0; x <= alphabet.Count; x++)
                for (int y = 0; y <= alphabet.Count; y++)
                    matrix[x, y] = x == y ? identity : mismatch;
            return new FancyAlphabet(matrix, alphabet, symmetric_similar, asymmetric_similar, gap_start, gap_extend, swap, size);
        }

        public FancyAlphabet(sbyte[,] matrix, List<char> alphabet, (sbyte score, List<List<List<char>>> sets) symmetric_similar, (sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets) asymmetric_similar, sbyte gap_start, sbyte gap_extend, sbyte swap, int size) {
            if (matrix.GetLength(0) != alphabet.Count + 1 || matrix.GetLength(1) != alphabet.Count + 1) throw new ArgumentException("Matrix size not fitting for given alphabet size");
            this.AlphabetSize = alphabet.Count;
            this.GapStartPenalty = gap_start;
            this.GapExtendPenalty = gap_extend;
            this.Size = size;
            this.ScoringMatrix = new sbyte[HelperFunctionality.IntPow(AlphabetSize + 1, (uint)size), HelperFunctionality.IntPow(AlphabetSize + 1, (uint)size)];
            this.PositionInScoringMatrix = alphabet.Select((item, index) => (item, index)).ToDictionary(item => item.item, item => item.index);
            this.Swap = swap;
            this.SymmetricScore = symmetric_similar.score;
            this.AsymmetricScore = asymmetric_similar.score;

            for (int x = 0; x <= alphabet.Count; x++) {
                for (int y = 0; y <= alphabet.Count; y++) {
                    this.ScoringMatrix[x, y] = matrix[x, y];
                }
            }

            // Set all symmetric similar sets, used for iso mass in the normal case
            foreach (var set in symmetric_similar.sets) {
                foreach ((var a, var b) in set.Where(s => s.Count <= size).Variations()) {
                    var perms_a = a.Permutations();
                    var perms_b = b.Permutations();
                    foreach (var perm_a in perms_a) {
                        foreach (var perm_b in perms_b) {
                            this.SetScore(perm_a, perm_b, symmetric_similar.score);
                        }
                    }
                }
            }

            // Set all asymmetric similar sets, used for modifications in the normal case
            foreach (var set in asymmetric_similar.sets) {
                foreach (var a in set.from.Where(s => s.Count <= size)) {
                    foreach (var b in set.to.Where(s => s.Count <= size)) {
                        var perms_a = a.Permutations();
                        var perms_b = b.Permutations();
                        foreach (var perm_a in perms_a) {
                            foreach (var perm_b in perms_b) {
                                this.SetScore(perm_a, perm_b, asymmetric_similar.score);
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
                        //Console.WriteLine(string.Join("", set) + ": " + string.Join(",", set.Permutations().Select(s => string.Join("", s))));
                        foreach (var perm in set.Permutations()) {
                            this.SetScore(set, perm, (sbyte)(len * (int)swap));
                            this.SetScore(perm, set, (sbyte)(len * (int)swap));
                        }
                    }
                }
            }
        }

        public static FancyAlphabet Default() {
            return IdentityMatrix(
                "ARNDCQEGHILKMFPSTWYVBZX.*".ToList(),
                (5, new List<List<List<char>>>{
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
                    }
                }),
                (3, new List<(List<List<char>>, List<List<char>>)> {
                    (new List<List<char>>{
                        new List<char> {
                            'Q'
                        }
                    }, new List<List<char>>{
                        new List<char>{
                            'E'
                            }
                    })
                }), 8, -1, -5, -5, 2, 3
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