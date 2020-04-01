using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace AssemblyNameSpace
{
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    public static class HelperFunctionality
    {
        /// <summary> To copy a subarray to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created subarray. </param>
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
        /// <summary> To copy a subarray to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created subarray. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static int[] ElementwiseAdd(this int[] data, int[] that)
        {
            if (data == null) return that;
            if (that == null) return data;
            if (data.Length != that.Length) throw new ArgumentException("To do an elementwiseAdd the two arrays should be the same length.");
            int[] result = new int[data.Length];
            Array.Copy(data, 0, result, 0, data.Length);
            for (int i = 0; i < that.Length; i++)
            {
                result[i] += that[i];
            }
            return result;
        }
        public static string TrimEnd(this string input, string suffixToRemove)
        {
            while (input != null && suffixToRemove != null && input.EndsWith(suffixToRemove))
            {
                input = input.Substring(0, input.Length - suffixToRemove.Length);
            }
            return input;
        }
        public struct ReadPlacement
        {
            public string Sequence;
            public int StartPosition;
            public int EndPosition;
            public int Identifier;
            public string StartOverhang;
            public string EndOverhang;
            public ReadPlacement(string sequence, int startposition, int identifier)
            {
                Sequence = sequence;
                StartPosition = startposition;
                EndPosition = startposition + sequence.Length;
                Identifier = identifier;
                StartOverhang = "";
                EndOverhang = "";
            }
            public ReadPlacement(string sequence, int startposition, int identifier, string startoverhang, string endoverhang)
            {
                Sequence = sequence;
                StartPosition = startposition;
                EndPosition = startposition + sequence.Length;
                Identifier = identifier;
                StartOverhang = startoverhang;
                EndOverhang = endoverhang;
            }
        }

        /// <summary> This aligns a list of sequences to a template sequence based on the alphabet. </summary>
        /// <returns> Returns a list of tuples with the sequences as first item, startingposition as second item,
        /// end position as third item and identifier from the given list as fourth item. </returns>
        /// <remark> This code does not account for small defects in reads, it will only align perfect matches
        /// and it will only align matches tha fit entirely inside the template sequence (no overhang at the start or end). </remark>
        /// <param name="template"> The template to match against. </param>
        /// <param name="sequences"> The sequences to match with. </param>
        /// <param name="reverse"> Whether or not the alignment should also be done in reverse direction. </param>
        /// <param name="alphabet"> The alphabet to be used. </param>
        public static List<ReadPlacement> MultipleSequenceAlignmentToTemplate(string template, Dictionary<int, string> sequences, List<List<int>> positions, Alphabet alphabet, int k, bool reverse = false)
        {
            // Keep track of all places already covered
            var result = new List<ReadPlacement>();

            // Loop through all to be placed reads
            foreach (var current in sequences)
            {
                int identifier = current.Key;
                string seq = current.Value;
                string seq_rev = string.Concat(seq.Reverse());
                int firsthit = -1;
                int lasthit = -1;

                // Try to find the first (and last) position it is in the originslist
                for (int i = 0; i < positions.Count(); i++)
                {
                    bool hit = false;
                    for (int j = 0; j < positions[i].Count(); j++)
                    {
                        if (positions[i][j] == identifier)
                        {
                            if (firsthit == -1) firsthit = i;
                            lasthit = i;
                            hit = true;
                            break;
                        }
                    }
                    if (!hit || i == positions.Count() - 1)
                    {
                        if (firsthit >= 0 && lasthit >= 0)
                        {
                            lasthit += k - 1; // Because the last hit still has a length of k
                            // Find the placement of the read in this patch

                            int lengthpatch = lasthit - firsthit;

                            if (lengthpatch == seq.Length)
                            {
                                // Determine forwards or backwards
                                if (reverse)
                                {
                                    int score_fw = GetPositionScore(ref template, ref seq, alphabet, firsthit);
                                    int score_bw = GetPositionScore(ref template, ref seq_rev, alphabet, firsthit + 1);
                                    if (score_fw >= score_bw) result.Add(new ReadPlacement(seq, firsthit, identifier));
                                    else result.Add(new ReadPlacement(seq_rev, firsthit, identifier));
                                }
                                else
                                {
                                    result.Add(new ReadPlacement(seq, firsthit, identifier));
                                }

                            }
                            else if (lengthpatch < seq.Length)
                            {
                                // Offset, score, reverse
                                var possibilities = new List<(int, ReadPlacement)>();

                                for (int offset = 0; offset < seq.Length - lengthpatch + 1; offset++)
                                {
                                    int score = 0;
                                    string template_seq = "";
                                    if (reverse)
                                    {
                                        template_seq = seq_rev.Substring(offset, lengthpatch);
                                        score = GetPositionScore(ref template, ref template_seq, alphabet, firsthit + 1);
                                        possibilities.Add((score, new ReadPlacement(template_seq, firsthit + 1, identifier, seq_rev.Substring(0, offset), seq_rev.Substring(offset + lengthpatch))));
                                    }
                                    template_seq = seq.Substring(offset, lengthpatch);
                                    score = GetPositionScore(ref template, ref template_seq, alphabet, firsthit);
                                    possibilities.Add((score, new ReadPlacement(template_seq, firsthit, identifier, seq.Substring(0, offset), seq.Substring(offset + lengthpatch))));
                                }

                                var best = possibilities.First();
                                foreach (var option in possibilities)
                                {
                                    if (option.Item1 > best.Item1) best = option;
                                }
                                result.Add(best.Item2);
                            }
                            else
                            {
                                // The patch is bigger than the sequence??? how that??
                                throw new Exception($"While aligning read {seq} onto contig {template} the read seems to be shorter than the length of the match between the read and contig. (read length: {seq.Length}, length patch: {lengthpatch}).");
                            }

                            firsthit = -1;
                            lasthit = -1;
                        }
                    }
                }
            }

            return result;
        }

        static int GetPositionScore(ref string template, ref string read, Alphabet alphabet, int position)
        {
            int score = 0;
            for (int i = 0; i < read.Length && i < template.Length - position; i++)
            {
                score += alphabet.ScoringMatrix[alphabet.GetIndexInAlphabet(template[position + i]), alphabet.GetIndexInAlphabet(read[i])];
            }
            return score;
        }

        /// <summary>
        /// Gets the sequence in AminoAcids from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        public static AminoAcid[] StringToSequence(string input, Alphabet alphabet)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alphabet, input[i]);
            }
            return output;
        }

        /// <summary>Do a local alignment based on the SmithWaterman algorithm of two sequences. </summary>
        /// <param name="template">The template sequence to use.</param>
        /// <param name="query">The query sequence to use.</param>
        public static SequenceMatch SmithWaterman(AminoAcid[] template, AminoAcid[] query, Alphabet alphabet, MetaData.IMetaData metadata = null, int index = 0)
        {
            int[] score_matrix = new int[(template.Length + 1) * (query.Length + 1)];
            int[] direction_matrix = new int[(template.Length + 1) * (query.Length + 1)];
            Span<int> indices_template = stackalloc int[template.Length];
            Span<int> indices_query = stackalloc int[query.Length];

            int rowsize = query.Length + 1;

            // Cache the indices as otherwise even dictionary lookups will become costly
            for (int i = 0; i < template.Length; i++)
            {
                indices_template[i] = alphabet.PositionInScoringMatrix[template[i].Char];
            }
            for (int i = 0; i < query.Length; i++)
            {
                indices_query[i] = alphabet.PositionInScoringMatrix[query[i].Char];
            }

            int max_value = 0;
            int max_index_t = 0;
            int max_index_q = 0;

            int tem_pos, query_pos, score, a, b, c, bpos, cpos, value;
            Direction direction;
            bool gap;
            char gap_char = Alphabet.GapChar;

            for (tem_pos = 1; tem_pos <= template.Length; tem_pos++)
            {
                for (query_pos = 1; query_pos <= query.Length; query_pos++)
                {
                    gap = template[tem_pos - 1].Char == gap_char || query[query_pos - 1].Char == gap_char;

                    // Calculate the score for the current position
                    if (gap)
                        a = score_matrix[rowsize * (tem_pos - 1) + query_pos - 1]; // Match Gap, 0 penalty
                    else
                    {
                        score = alphabet.ScoringMatrix[indices_template[tem_pos - 1], indices_query[query_pos - 1]];
                        a = score_matrix[rowsize * (tem_pos - 1) + query_pos - 1] + score; // Match
                    }

                    bpos = rowsize * tem_pos + query_pos - 1;

                    b = score_matrix[bpos] - ((direction_matrix[bpos] == (int)Direction.GapInQuery || direction_matrix[bpos] == (int)Direction.MatchGap) ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty);

                    cpos = rowsize * (tem_pos - 1) + query_pos;

                    c = score_matrix[cpos] - ((direction_matrix[cpos] == (int)Direction.GapInTemplate || direction_matrix[cpos] == (int)Direction.MatchGap) ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty);

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
                        direction = Direction.GapInQuery;
                    }
                    else if (!gap && c > 0)
                    {
                        value = c;
                        direction = Direction.GapInTemplate;
                    }
                    else
                    {
                        value = 0;
                        direction = Direction.NoMatch;
                    }

                    score_matrix[rowsize * tem_pos + query_pos] = value;
                    direction_matrix[rowsize * tem_pos + query_pos] = (int)direction;

                    // Keep track of the maximal value
                    if (value > max_value)
                    {
                        max_value = value;
                        max_index_t = tem_pos;
                        max_index_q = query_pos;
                    }
                }
            }

            // Traceback
            // TODO: Adjust the score on each position based on the DOC, to create a fairer score
            var match_list = new List<SequenceMatch.MatchPiece>();

            while (true)
            {
                switch (direction_matrix[rowsize * max_index_t + max_index_q])
                {
                    case (int)Direction.Match:
                        match_list.Add(new SequenceMatch.Match(1));
                        max_index_t--;
                        max_index_q--;
                        break;
                    case (int)Direction.MatchGap:
                        match_list.Add(new SequenceMatch.Match(1)); // TODO: Maybe Introduce the right gap?
                        max_index_t--;
                        max_index_q--;
                        break;
                    case (int)Direction.GapInTemplate:
                        match_list.Add(new SequenceMatch.GapInTemplate(1));
                        max_index_t--;
                        break;
                    case (int)Direction.GapInQuery:
                        match_list.Add(new SequenceMatch.GapInQuery(1));
                        max_index_q--;
                        break;
                    case (int)Direction.NoMatch:
                        goto END_OF_CRAWL; // I am hopefull this compiles to a single jump instruction, which would be more efficient than a bool variable which is checked every loop iteration
                        break;
                }
            }

        END_OF_CRAWL:
            match_list.Reverse();

            var match = new SequenceMatch(max_index_t, max_index_q, max_value, match_list, template, query, metadata, index);
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

                    b = score_matrix[tem_pos, query_pos - 1].Item1 - ((score_matrix[tem_pos, query_pos - 1].Item2 == Direction.GapInQuery || score_matrix[tem_pos, query_pos - 1].Item2 == Direction.MatchGap) ? 1 : 4);
                    c = score_matrix[tem_pos - 1, query_pos].Item1 - ((score_matrix[tem_pos - 1, query_pos].Item2 == Direction.GapInTemplate || score_matrix[tem_pos - 1, query_pos].Item2 == Direction.MatchGap) ? 1 : 4);

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

        enum Direction { NoMatch, GapInTemplate, GapInQuery, Match, MatchGap }

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
            const long sectime = 1000;
            const long mintime = 60 * sectime;
            const long htime = 60 * mintime;

            long hours = elapsedMilliseconds / htime;
            long minutes = elapsedMilliseconds / mintime - hours * 60;
            long seconds = elapsedMilliseconds / sectime - hours * 60 * 60 - minutes * 60;
            long milliseconds = elapsedMilliseconds - hours * htime - minutes * mintime - seconds * sectime;

            if (hours > 0)
                return $"{hours}:{minutes:D2} h";
            if (minutes > 0)
                return $"{minutes}:{seconds:D2} m";
            if (seconds > 0)
                return $"{seconds,2}.{milliseconds / 100:D1} s";
            return $"{milliseconds,3} ms";
        }
    }
}