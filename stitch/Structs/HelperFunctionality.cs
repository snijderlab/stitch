using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stitch
{
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    public static class HelperFunctionality
    {
        /// <summary> To copy a sub array to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            try
            {
                T[] result = new T[length];
                Array.Copy(data, index, result, 0, length);
                return result;
            }
            catch
            {
                throw new ArgumentException($"SubArray Exception length {length} index {index} on an array of length {data.Length}");
            }
        }

        /// <summary> Create a hash set from data with a given comparer. </summary>
        /// <param name="data"> The data. </param>
        /// <param name="comparer"> The comparer function, should return true when two element are equal. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new hash set with only the unique elements from the data as determined by the comparer function. </returns>
        public static HashSet<T> Unique<T>(this IEnumerable<T> data, Func<T, T, bool> comparer)
        {
            var temp = new HashSet<T>();
            foreach (var element in data)
                if (temp.Count() == 0 || temp.All(e => !comparer(element, e)))
                    temp.Add(element);
            return temp;
        }

        /// <summary> To copy a sub array to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created sub array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static double[] ElementwiseAdd(this double[] data, double[] that)
        {
            if (data == null) return that;
            if (that == null) return data;
            if (data.Length != that.Length) throw new ArgumentException("To do an elementwiseAdd the two arrays should be the same length.");
            double[] result = new double[data.Length];
            Array.Copy(data, 0, result, 0, data.Length);
            for (int i = 0; i < that.Length; i++)
            {
                result[i] += that[i];
            }
            return result;
        }
        public static string TrimEnd(this string input, string suffixToRemove)
        {
            if (input == null || suffixToRemove == null || string.IsNullOrEmpty(input) || input.Length < suffixToRemove.Length) return input;
            if (input == suffixToRemove) return "";
            int location = input.Length - suffixToRemove.Length;
            while (location > 0 && string.CompareOrdinal(input, location, suffixToRemove, 0, suffixToRemove.Length) == 0)
            {
                location -= suffixToRemove.Length;
            }
            return input.Remove(location);
        }
        public static string TrimStart(this string input, string prefixToRemove)
        {
            if (input == null || prefixToRemove == null || string.IsNullOrEmpty(input) || input.Length < prefixToRemove.Length) return input;
            if (input == prefixToRemove) return "";
            int location = 0;
            while (location < input.Length - 1 - prefixToRemove.Length && string.CompareOrdinal(input, location, prefixToRemove, 0, prefixToRemove.Length) == 0)
            {
                location += prefixToRemove.Length;
            }
            return input.Remove(location);
        }

        /// <summary> Apply a function to a 2D array modifying it in place. </summary>
        /// <param name="data"> The 2D array. </param>
        /// <param name="f"> The function to determine the new values, based on the index (x, y) given to the function. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns nothing, but the array is modified. </returns>
        public static void IndexMap<T>(this T[,] data, Func<int, int, T> f)
        {
            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    data[i, j] = f(i, j);
        }

        /// <summary> Apply a function to an array modifying it in place. </summary>
        /// <param name="data"> The array. </param>
        /// <param name="f"> The function to determine the new values, based on the index given to the function. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns nothing, but the array is modified. </returns>
        public static void IndexMap<T>(this T[] data, Func<int, T> f)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = f(i);
        }

        /// <summary>Do a local alignment based on the SmithWaterman algorithm of two sequences. </summary>
        /// <param name="template">The template sequence to use.</param>
        /// <param name="query">The query sequence to use.</param>
        public static SequenceMatch SmithWaterman(Read.IRead template, Read.IRead query, Alphabet alphabet, int index = 0, int templateIndex = -1)
        {
            int[] score_matrix = new int[(template.Sequence.Length + 1) * (query.Sequence.Length + 1)];
            int[] direction_matrix = new int[(template.Sequence.Length + 1) * (query.Sequence.Length + 1)];
            Span<int> indices_template = template.Sequence.Length <= 1024 ? stackalloc int[template.Sequence.Length] : new int[template.Sequence.Length];
            Span<int> indices_query = query.Sequence.Length <= 1024 ? stackalloc int[query.Sequence.Length] : new int[query.Sequence.Length];

            int row_size = query.Sequence.Length + 1;

            // Cache the indices as otherwise even dictionary lookups will become costly
            for (int i = 0; i < template.Sequence.Length; i++)
            {
                indices_template[i] = alphabet.PositionInScoringMatrix[template.Sequence[i].Character];
            }
            for (int i = 0; i < query.Sequence.Length; i++)
            {
                indices_query[i] = alphabet.PositionInScoringMatrix[query.Sequence[i].Character];
            }

            int max_value = 0;
            int max_index_t = 0;
            int max_index_q = 0;
            int[,] alphabet_scores = alphabet.ScoringMatrix;

            int tem_pos, query_pos, score, a, b, c, bpos, cpos, value;
            Direction direction;
            bool gap;
            char gap_char = Alphabet.GapChar;

            for (tem_pos = 1; tem_pos <= template.Sequence.Length; tem_pos++)
            {
                for (query_pos = 1; query_pos <= query.Sequence.Length; query_pos++)
                {
                    gap = template.Sequence[tem_pos - 1].Character == gap_char || query.Sequence[query_pos - 1].Character == gap_char;

                    // Calculate the score for the current position
                    if (gap)
                        a = score_matrix[row_size * (tem_pos - 1) + query_pos - 1]; // Match Gap, 0 penalty
                    else
                    {
                        //The following line is the most time consuming in this whole function, maybe cache the matrix?? - now test it
                        score = alphabet_scores[indices_template[tem_pos - 1], indices_query[query_pos - 1]];
                        a = score_matrix[row_size * (tem_pos - 1) + query_pos - 1] + score; // Match
                    }

                    bpos = row_size * tem_pos + query_pos - 1;

                    b = score_matrix[bpos] - ((direction_matrix[bpos] == (int)Direction.Insertion || direction_matrix[bpos] == (int)Direction.MatchGap) ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty);

                    cpos = row_size * (tem_pos - 1) + query_pos;

                    c = score_matrix[cpos] - ((direction_matrix[cpos] == (int)Direction.Deletion || direction_matrix[cpos] == (int)Direction.MatchGap) ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty);

                    if (a > b && a > c && a > 0)
                    {
                        if (gap)
                        {
                            value = a;
                            direction = Direction.MatchGap;
                        }
                        else
                        {
                            value = a;
                            direction = Direction.Match;
                        }
                    }
                    else if (!gap && b > c && b > 0)
                    {
                        value = b;
                        direction = Direction.Insertion;
                    }
                    else if (!gap && c > 0)
                    {
                        value = c;
                        direction = Direction.Deletion;
                    }
                    else
                    {
                        value = 0;
                        direction = Direction.NoMatch;
                    }

                    score_matrix[row_size * tem_pos + query_pos] = value;
                    direction_matrix[row_size * tem_pos + query_pos] = (int)direction;

                    // Keep track of the maximal value
                    if (value > max_value)
                    {
                        max_value = value;
                        max_index_t = tem_pos;
                        max_index_q = query_pos;
                    }
                }
            }

            // Trace back
            var match_list = new List<SequenceMatch.MatchPiece>();

            while (true)
            {
                switch (direction_matrix[row_size * max_index_t + max_index_q])
                {
                    case (int)Direction.Match:
                        match_list.Add(new SequenceMatch.Match(1));
                        max_index_t--;
                        max_index_q--;
                        break;
                    case (int)Direction.MatchGap:
                        match_list.Add(new SequenceMatch.Match(1));
                        max_index_t--;
                        max_index_q--;
                        break;
                    case (int)Direction.Deletion:
                        match_list.Add(new SequenceMatch.Deletion(1));
                        max_index_t--;
                        break;
                    case (int)Direction.Insertion:
                        match_list.Add(new SequenceMatch.Insertion(1));
                        max_index_q--;
                        break;
                    case (int)Direction.NoMatch:
                        goto END_OF_CRAWL; // I am hopeful this compiles to a single jump instruction, which would be more efficient than a bool variable which is checked every loop iteration
                        break;
                }
            }

        END_OF_CRAWL:
            match_list.Reverse();

            var match = new SequenceMatch(max_index_t, max_index_q, max_value, match_list, template, query, index, templateIndex);
            return match;
        }

        public static int SmithWatermanStrings(string template, string query)
        {
            var score_matrix = new (int, Direction)[template.Length + 1, query.Length + 1]; // Default value of 0
            int[] indices_template = new int[template.Length];
            int[] indices_query = new int[query.Length];

            // Cache the indices as otherwise even dictionary lookups will become costly
            for (int i = 0; i < template.Length; i++)
            {
                indices_template[i] = (int)template[i];
            }
            for (int i = 0; i < query.Length; i++)
            {
                indices_query[i] = (int)query[i];
            }

            int max_value = 0;

            int tem_pos, query_pos, score, a, b, c;

            for (tem_pos = 1; tem_pos <= template.Length; tem_pos++)
            {
                for (query_pos = 1; query_pos <= query.Length; query_pos++)
                {
                    // Calculate the score for the current position
                    score = indices_template[tem_pos - 1] == indices_query[query_pos - 1] ? 1 : 0;
                    a = score_matrix[tem_pos - 1, query_pos - 1].Item1 + score; // Match

                    b = score_matrix[tem_pos, query_pos - 1].Item1 - ((score_matrix[tem_pos, query_pos - 1].Item2 == Direction.Insertion || score_matrix[tem_pos, query_pos - 1].Item2 == Direction.MatchGap) ? 1 : 4);
                    c = score_matrix[tem_pos - 1, query_pos].Item1 - ((score_matrix[tem_pos - 1, query_pos].Item2 == Direction.Deletion || score_matrix[tem_pos - 1, query_pos].Item2 == Direction.MatchGap) ? 1 : 4);

                    if (a > b && a > c && a > 0)
                    {
                        score_matrix[tem_pos, query_pos] = (a, Direction.Match);
                    }
                    else
                        score_matrix[tem_pos, query_pos] = (0, Direction.NoMatch);

                    // Keep track of the maximal value
                    if (score_matrix[tem_pos, query_pos].Item1 > max_value)
                    {
                        max_value = score_matrix[tem_pos, query_pos].Item1;
                    }
                }
            }

            return max_value;
        }

        /// <summary> End align two sequences </summary>
        /// <param name="template">The front sequence</param>
        /// <param name="query">The tail sequence</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="maxOverlap">The maximal length of the overlap</param>
        /// <returns>A tuple with the best position and its score</returns>
        public static ((int Position, int Score) Best, List<(int Position, int Score)> Scores) EndAlignment(AminoAcid[] template, AminoAcid[] query, Alphabet alphabet, int maxOverlap)
        {
            var scores = new List<(int, int)>();
            for (int i = 1; i < maxOverlap && i < query.Length && i < template.Length; i++)
            {
                var score = AminoAcid.ArrayHomology(template.TakeLast(i).ToArray(), query.Take(i).ToArray(), alphabet) - (2 * i);
                scores.Add((i, score));
            }
            if (scores.Count == 0) return ((0, 0), scores);

            var best = scores[0];
            foreach (var item in scores)
                if (item.Item2 > best.Item2) best = item;
            return (best, scores);
        }

        public enum Annotation { None, CDR, CDR1, CDR2, CDR3, Conserved, PossibleGlycan, Other }

        public static bool IsAnyCDR(this Annotation annotation)
        {
            return annotation == Annotation.CDR || annotation == Annotation.CDR1 || annotation == Annotation.CDR2 || annotation == Annotation.CDR3;
        }

        public static Annotation ParseAnnotation(string Type)
        {
            Type = Type.ToLower().Trim();
            switch (Type)
            {
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

        public static string CIGAR(this ICollection<SequenceMatch.MatchPiece> match)
        {
            StringBuilder sb = new StringBuilder();
            foreach (SequenceMatch.MatchPiece element in match)
            {
                sb.Append(element.ToString());
            }
            return sb.ToString();
        }

        public static string DisplayTime(long elapsedMilliseconds)
        {
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

        public static int RoundToHumanLogicalFactor(int input)
        {
            if (input == 0) return 0;

            int factor = (int)Math.Floor(Math.Log10(input)) * 10;
            int abs = Math.Abs(input);

            if (factor == 0) return input;
            else if (abs < 10) factor = 2;
            else if (abs < 50) factor = 5;

            return input / factor * factor;
        }

        public static bool EvaluateTrilean(RunParameters.Trilean trilean, bool baseValue)
        {
            return trilean switch
            {
                RunParameters.Trilean.True => true,
                RunParameters.Trilean.False => false,
                RunParameters.Trilean.Unspecified => baseValue,
                _ => throw new ArgumentException("Tried to evaluate a trilean type which does not exist.")
            };
        }

        public static bool EvaluateTrilean(RunParameters.Trilean trilean1, RunParameters.Trilean trilean2, bool baseValue)
        {
            return trilean1 switch
            {
                RunParameters.Trilean.True => true,
                RunParameters.Trilean.False => false,
                RunParameters.Trilean.Unspecified => EvaluateTrilean(trilean2, baseValue),
                _ => throw new ArgumentException("Tried to evaluate a trilean type which does not exist.")
            };
        }

        public static AminoAcid[] GenerateRandomSequence(Alphabet alphabet, int length)
        {
            var output = new AminoAcid[length];
            Random random = new Random(42);
            var count = alphabet.PositionInScoringMatrix.Count;
            var values = alphabet.PositionInScoringMatrix.Keys;
            for (int i = 0; i < length; i++)
            {
                var element = values.ElementAt(random.Next(count));
                output[i] = new AminoAcid(alphabet, element);
            }
            return output;
        }
    }
}