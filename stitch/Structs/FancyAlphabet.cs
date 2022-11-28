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
        readonly Dictionary<char, int> PositionInScoringMatrix;

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

        public sbyte Score(AminoAcid[] a, AminoAcid[] b) {
            return ScoringMatrix[Index(a), Index(b)];
        }

        public int Index(AminoAcid[] data) {
            var index = 0;
            foreach (var d in data) {
                index *= AlphabetSize;
                index += PositionInScoringMatrix[d.Character];
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
                index += PositionInScoringMatrix[d];
            }
            return index;
        }

        public static FancyAlphabet IdentityMatrix(List<char> alphabet, (sbyte score, List<List<List<char>>> sets) symmetric_similar, (sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets) asymmetric_similar, sbyte identity, sbyte mismatch, sbyte gap_start, sbyte gap_extend, sbyte swap, int size) {
            alphabet.Insert(0, ' ');
            var matrix = new sbyte[alphabet.Count, alphabet.Count];
            for (int x = 0; x < alphabet.Count; x++)
                for (int y = 0; y < alphabet.Count; y++)
                    matrix[x, y] = x == y ? identity : mismatch;
            return new FancyAlphabet(matrix, alphabet, symmetric_similar, asymmetric_similar, gap_start, gap_extend, swap, size);
        }

        public FancyAlphabet(sbyte[,] matrix, List<char> alphabet, (sbyte score, List<List<List<char>>> sets) symmetric_similar, (sbyte score, List<(List<List<char>> from, List<List<char>> to)> sets) asymmetric_similar, sbyte gap_start, sbyte gap_extend, sbyte swap, int size) {
            if (matrix.GetLength(0) != alphabet.Count || matrix.GetLength(1) != alphabet.Count) throw new ArgumentException("Matrix size not fitting for given alphabet size");
            this.AlphabetSize = alphabet.Count;
            this.GapStartPenalty = gap_start;
            this.GapExtendPenalty = gap_extend;
            this.Size = size;
            this.ScoringMatrix = new sbyte[HelperFunctionality.IntPow(AlphabetSize + 1, (uint)size), HelperFunctionality.IntPow(AlphabetSize + 1, (uint)size)];
            this.PositionInScoringMatrix = alphabet.Select((item, index) => (item, index)).ToDictionary(item => item.item, item => item.index);

            for (int x = 0; x < alphabet.Count; x++) {
                for (int y = 0; y < alphabet.Count; y++) {
                    this.ScoringMatrix[x, y] = matrix[x, y];
                }
            }

            // Set all symmetric similar sets, used for iso mass in the normal case
            foreach (var set in symmetric_similar.sets) {
                foreach ((var a, var b) in set.Where(s => s.Count <= size).Variations()) {
                    foreach (var perm_a in a.Permutations()) {
                        foreach (var perm_b in b.Permutations()) {
                            this.SetScore(perm_a, perm_b, symmetric_similar.score);
                        }
                    }
                }
            }

            // Set all asymmetric similar sets, used for modifications in the normal case
            foreach (var set in asymmetric_similar.sets) {
                foreach (var a in set.from.Where(s => s.Count <= size)) {
                    foreach (var b in set.to.Where(s => s.Count <= size)) {
                        foreach (var perm_a in a.Permutations()) {
                            foreach (var perm_b in b.Permutations()) {
                                this.SetScore(perm_a, perm_b, asymmetric_similar.score);
                            }
                        }
                    }
                }
            }

            if (swap != 0) {
                // Set all swap scores by taking all possible combinations whit at least two different items.
                // Generate all possible permutations for these combinations.
                for (int len = 2; len <= size; len++) {
                    foreach (var set in alphabet.Combinations(len).Where(s => !s.All(i => i == s.First()))) {
                        foreach (var perm1 in set.Permutations()) {
                            foreach (var perm2 in set.Permutations()) {
                                if (perm1 == perm2) continue;
                                this.SetScore(perm1, perm2, (sbyte)(set.Count * (int)swap));
                            }
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