using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stitch {
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    public static class HelperFunctionality {
        /// <summary> To copy a sub array to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length) {
            try {
                T[] result = new T[length];
                Array.Copy(data, index, result, 0, length);
                return result;
            } catch {
                throw new ArgumentException($"SubArray Exception length {length} index {index} on an array of length {data.Length}");
            }
        }

        /// <summary> To copy a sub array to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static List<T> SubList<T>(this List<T> data, int index, int length) {
            try {
                T[] result = new T[length];
                Array.Copy(data.ToArray(), index, result, 0, length);
                return result.ToList();
            } catch {
                throw new ArgumentException($"SubList Exception length {length} index {index} on a list of length {data.Count}");
            }
        }

        /// <summary> To create a span to the given slice in an array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static Span<T> SubSpan<T>(this T[] data, int index, int length) {
            try {
                return new Span<T>(data, index, length);
            } catch {
                throw new ArgumentException($"SubArray Exception length {length} index {index} on an array of length {data.Length}");
            }
        }

        /// <summary> To copy a sub array to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static double[] ElementwiseAdd(this double[] data, double[] that) {
            if (data == null) return that;
            if (that == null) return data;
            if (data.Length != that.Length) throw new ArgumentException("To do an elementwiseAdd the two arrays should be the same length.");
            double[] result = new double[data.Length];
            Array.Copy(data, 0, result, 0, data.Length);
            for (int i = 0; i < that.Length; i++) {
                result[i] += that[i];
            }
            return result;
        }

        /// <summary> Apply a function to a 2D array modifying it in place. </summary>
        /// <param name="data"> The 2D array. </param>
        /// <param name="f"> The function to determine the new values, based on the index (x, y) given to the function. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns nothing, but the array is modified. </returns>
        public static void IndexMap<T>(this T[,] data, Func<int, int, T> f) {
            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    data[i, j] = f(i, j);
        }

        /// <summary> Apply a function to an array modifying it in place. </summary>
        /// <param name="data"> The array. </param>
        /// <param name="f"> The function to determine the new values, based on the index given to the function. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns nothing, but the array is modified. </returns>
        public static void IndexMap<T>(this T[] data, Func<int, T> f) {
            for (int i = 0; i < data.Length; i++)
                data[i] = f(i);
        }

        /// <summary>
        /// string.Reverse().ToString() will always result in `System Enumerable+ReverseIterator`1` (or similar) so for anybody that actually wants a reversed string use the below.
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The reverse</returns>
        public static string ReverseString(this string str) {
            var chars = str.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary> Generate all variations of a two sized selection for the given sequence while leaving out the (A, A) case. Uses Equals inside.</summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        public static IEnumerable<(T, T)> Variations<T>(this IEnumerable<T> data) {
            return data.SelectMany(a => data.Where(b => !a.Equals(b)).Select(b => (a, b)));
        }

        /// <summary> Generate all variations of a len sized selection for the given sequence while reusing already selected options. </summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        public static IEnumerable<List<T>> Variations<T>(this IEnumerable<T> data, int len) {
            var sets = data.Select(a => new List<T> { a });
            for (int i = 1; i < len; i++) {
                sets = sets.SelectMany(a => data.Select(b => new List<T>(a) { b }));
            }
            return sets;
        }

        /// <summary> Generate all combinations of a len sized selection for the given sequence while reusing already selected options. </summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        public static IEnumerable<List<T>> Combinations<T>(this IEnumerable<T> data, int len) {
            IEnumerable<List<T>> Recurse(List<T> set, IEnumerable<T> data, int len) {
                return data.
                    Select((item, index) => (item, data.Skip(index))).
                    SelectMany(v => len == 1 ?
                        new List<List<T>> { new List<T>(set) { v.item } } :
                        Recurse(new List<T>(set) { v.item }, v.Item2, len - 1));
            }
            return data.SelectMany(a => Recurse(new List<T> { a }, data, len - 1));
        }

        /// <summary> Generate all permutations for the given sequence while not choosing the same item again case. Uses Equals inside.</summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> data) {
            IEnumerable<IEnumerable<T>> Recurse(IEnumerable<T> data, bool[] chosen, List<T> result) {
                if (chosen.All(i => i)) return new List<List<T>>() { result };
                return data.Select((v, i) => (v, i)).Where((v) => !chosen[v.Item2]).SelectMany((v) => {
                    var new_chosen = chosen.ToArray();
                    var new_result = result.ToList();
                    new_result.Add(v.Item1);
                    new_chosen[v.Item2] = true;
                    return Recurse(data, new_chosen, new_result);
                });
            }

            return Recurse(data, new bool[data.Count()], new List<T>());
        }

        /// <summary> Trim a path to remove all outer wrapping ' and ". </summary>
        public static string TrimPath(this string path) {
            path = path.StartsWith('\"') && path.EndsWith('\"') ? path.Substring(1, path.Length - 2) : path;
            path = path.StartsWith('\'') && path.EndsWith('\'') ? path.Substring(1, path.Length - 2) : path;
            return path;
        }

        /// <summary> Integer exponentiation: https://stackoverflow.com/a/383596/5779120. </summary>
        public static int IntPow(int x, uint pow) {
            int ret = 1;
            while (pow != 0) {
                if ((pow & 1) == 1)
                    ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }

        /// <summary>
        /// https://stackoverflow.com/a/9775057/5779120
        /// </summary>
        /// <param name="arrays"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[,] CreateRectangularArray<T>(this IEnumerable<T[]> arrays) {
            int minorLength = arrays.Count() == 0 ? 0 : arrays.ElementAt(0).Length;
            T[,] ret = new T[arrays.Count(), minorLength];
            for (int i = 0; i < arrays.Count(); i++) {
                var array = arrays.ElementAt(i);
                if (array.Length != minorLength) {
                    throw new ArgumentException("All arrays must be the same length");
                }
                for (int j = 0; j < minorLength; j++) {
                    ret[i, j] = array[j];
                }
            }
            return ret;
        }

        public static int IndexOf<T>(this T[] arr, T element) {
            return Array.IndexOf(arr, element);
        }

        public static double Max(this IEnumerable<double> data, double fallback) {
            if (data.Count() > 0)
                return data.Max();
            else
                return fallback;
        }

        public static string ElementWiseToString<T>(this IEnumerable<T> data) {
            return "[" + data.Aggregate("", (acc, i) => acc + i.ToString() + ", ") + "]";
        }

        public static int SmithWatermanStrings(string template, string query) {
            var score_matrix = new (int, Direction)[template.Length + 1, query.Length + 1]; // Default value of 0
            int[] indices_template = new int[template.Length];
            int[] indices_query = new int[query.Length];

            // Cache the indices as otherwise even dictionary lookups will become costly
            for (int i = 0; i < template.Length; i++) {
                indices_template[i] = (int)template[i];
            }
            for (int i = 0; i < query.Length; i++) {
                indices_query[i] = (int)query[i];
            }

            int max_value = 0;

            int tem_pos, query_pos, score, a, b, c;

            for (tem_pos = 1; tem_pos <= template.Length; tem_pos++) {
                for (query_pos = 1; query_pos <= query.Length; query_pos++) {
                    // Calculate the score for the current position
                    score = indices_template[tem_pos - 1] == indices_query[query_pos - 1] ? 1 : 0;
                    a = score_matrix[tem_pos - 1, query_pos - 1].Item1 + score; // Match

                    b = score_matrix[tem_pos, query_pos - 1].Item1 - ((score_matrix[tem_pos, query_pos - 1].Item2 == Direction.Insertion || score_matrix[tem_pos, query_pos - 1].Item2 == Direction.MatchGap) ? 1 : 4);
                    c = score_matrix[tem_pos - 1, query_pos].Item1 - ((score_matrix[tem_pos - 1, query_pos].Item2 == Direction.Deletion || score_matrix[tem_pos - 1, query_pos].Item2 == Direction.MatchGap) ? 1 : 4);

                    if (a > b && a > c && a > 0) {
                        score_matrix[tem_pos, query_pos] = (a, Direction.Match);
                    } else
                        score_matrix[tem_pos, query_pos] = (0, Direction.NoMatch);

                    // Keep track of the maximal value
                    if (score_matrix[tem_pos, query_pos].Item1 > max_value) {
                        max_value = score_matrix[tem_pos, query_pos].Item1;
                    }
                }
            }

            return max_value;
        }

        public enum Annotation { None, CDR, CDR1, CDR2, CDR3, Conserved, PossibleGlycan, Other }

        public static bool IsAnyCDR(this Annotation annotation) {
            return annotation == Annotation.CDR || annotation == Annotation.CDR1 || annotation == Annotation.CDR2 || annotation == Annotation.CDR3;
        }

        public static Annotation ParseAnnotation(string Type) {
            Type = Type.ToLower().Trim();
            switch (Type) {
                case "":
                    return Annotation.None;
                case "cdr":
                    return Annotation.CDR;
                case "cdr1":
                    return Annotation.CDR1;
                case "cdr2":
                    return Annotation.CDR2;
                case "cdr3":
                    return Annotation.CDR3;
                case "conserved":
                    return Annotation.Conserved;
                case "glycosylationsite":
                    return Annotation.PossibleGlycan;
                default:
                    return Annotation.Other;
            }
        }

        enum Direction { NoMatch, Deletion, Insertion, Match, MatchGap }

        public static string DisplayTime(long elapsedMilliseconds) {
            const long sec_time = 1000;
            const long min_time = 60 * sec_time;
            const long htime = 60 * min_time;

            long hours = elapsedMilliseconds / htime;
            long minutes = elapsedMilliseconds / min_time - hours * 60;
            long seconds = elapsedMilliseconds / sec_time - hours * 60 * 60 - minutes * 60;
            long milliseconds = elapsedMilliseconds - hours * htime - minutes * min_time - seconds * sec_time;

            if (hours > 0)
                return $"{hours}:{minutes:D2} h";
            if (minutes > 0)
                return $"{minutes}:{seconds:D2} m";
            if (seconds > 0)
                return $"{seconds,2}.{milliseconds / 100:D1} s";
            return $"{milliseconds,3} ms";
        }

        public static int RoundToHumanLogicalFactor(int input) {
            if (input == 0) return 0;

            int factor = (int)Math.Floor(Math.Log10(input)) * 10;
            int abs = Math.Abs(input);

            if (factor == 0) return input;
            else if (abs < 10) factor = 2;
            else if (abs < 50) factor = 5;

            return input / factor * factor;
        }

        public static bool EvaluateTrilean(RunParameters.Trilean trilean, bool baseValue) {
            return trilean switch {
                RunParameters.Trilean.True => true,
                RunParameters.Trilean.False => false,
                RunParameters.Trilean.Unspecified => baseValue,
                _ => throw new ArgumentException("Tried to evaluate a trilean type which does not exist.")
            };
        }

        public static AminoAcid[] GenerateRandomSequence(ScoringMatrix alphabet, int length) {
            var output = new AminoAcid[length];
            Random random = new Random(42);
            var count = alphabet.PositionInScoringMatrix.Count;
            var values = alphabet.PositionInScoringMatrix.Keys;
            for (int i = 0; i < length; i++) {
                var element = values.ElementAt(random.Next(count));
                output[i] = new AminoAcid(alphabet, element);
            }
            return output;
        }

        /// <summary> Parse the sloppy ProForma like syntax of the input files into actual pro forma sequences. </summary>
        public static Option<(string Modified, string Bare)> FromSloppyProForma(string sequence) {
            // Break into chunks with correct number of closed braces
            var chunks = new List<string>();
            int depth = 0;
            int last_index = 0;
            bool in_modification = false;
            // Skip the first and last char, those are always '_'
            for (int index = 0; index < sequence.Length; index++) {
                switch (sequence[index]) {
                    case '_':
                        if (depth == 0) {
                            if (index - last_index > 0)
                                chunks.Add(sequence.Substring(last_index, index - last_index)); // Not including this char
                            last_index = index + 1;
                        }
                        break;
                    case '+':
                    case '-':
                        if (depth == 0) {
                            if (index - last_index > 0)
                                if (in_modification)
                                    return new Option<(string, string)>(); // Invalid sequence
                                else
                                    chunks.Add(sequence.Substring(last_index, index - last_index)); // Not including this char
                            last_index = index;
                            in_modification = true;
                        }
                        break;
                    case '(':
                        if (depth == 0) {
                            if (index - last_index > 0)
                                chunks.Add(sequence.Substring(last_index, index - last_index)); // Not including this char
                            last_index = index;
                        }
                        depth += 1;
                        break;
                    case ')':
                        depth -= 1;
                        if (depth == 0) {
                            if (index - last_index > 0)
                                chunks.Add(sequence.Substring(last_index, index - last_index + 1)); // Including this char
                            last_index = index + 1;
                        }
                        break;
                    case '[':
                        if (depth == 0) {
                            if (index - last_index > 0)
                                chunks.Add(sequence.Substring(last_index, index - last_index)); // Not including this char
                            last_index = index;
                        }
                        depth += 1;
                        break;
                    case ']':
                        depth -= 1;
                        if (depth == 0) {
                            if (index - last_index > 0)
                                chunks.Add(sequence.Substring(last_index, index - last_index + 1)); // Including this char
                            last_index = index + 1;
                        }
                        break;
                    default:
                        if (depth == 0) {
                            if (char.IsDigit(sequence[index]) && !in_modification) {
                                if (depth == 0) {
                                    if (index - last_index > 0)
                                        chunks.Add(sequence.Substring(last_index, index - last_index)); // Not including this char
                                    last_index = index;
                                    in_modification = true;
                                }
                            } else if (!char.IsDigit(sequence[index]) && sequence[index] != '.' && in_modification) {
                                if (depth == 0) {
                                    if (index - last_index > 0)
                                        chunks.Add('[' + sequence.Substring(last_index, index - last_index) + ']'); // Not including this char
                                    last_index = index;
                                }
                                in_modification = false;
                            }
                        }
                        break;
                }
            }
            if (sequence.Length - last_index > 0)
                if (in_modification)
                    chunks.Add('[' + sequence.Substring(last_index, sequence.Length - last_index) + ']');
                else
                    chunks.Add(sequence.Substring(last_index, sequence.Length - last_index));

            var aa = new StringBuilder(sequence.Length);
            var final_sequence = new StringBuilder(sequence.Length);
            var skip = 0;
            for (var index = 0; index < chunks.Count; index++) {
                var chunk = chunks[index];
                if (chunk[0] == '(') {
                    final_sequence.Append($"[{chunk.Substring(1, chunk.Length - 2)}]");
                } else {
                    final_sequence.Append(chunk.Substring(skip, chunk.Length - skip));
                    aa.Append(chunk.Substring(skip, chunk.Length - skip));
                }
                if (index == 0 && "([-+0123456789".Contains(chunk[0])) {
                    final_sequence.Append('-');
                }
            }
            return new Option<(string, string)>((final_sequence.ToString(), aa.ToString()));
        }
    }
}