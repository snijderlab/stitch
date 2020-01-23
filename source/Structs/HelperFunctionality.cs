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
    static public class HelperFunctionality
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
                throw new Exception($"SubArray Exception length {length} index {index}");
            }
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
                                    string tseq = "";
                                    if (reverse)
                                    {
                                        tseq = seq_rev.Substring(offset, lengthpatch);
                                        score = GetPositionScore(ref template, ref tseq, alphabet, firsthit + 1);
                                        possibilities.Add((score, new ReadPlacement(tseq, firsthit + 1, identifier, seq_rev.Substring(0, offset), seq_rev.Substring(offset + lengthpatch))));
                                    }
                                    tseq = seq.Substring(offset, lengthpatch);
                                    score = GetPositionScore(ref template, ref tseq, alphabet, firsthit);
                                    possibilities.Add((score, new ReadPlacement(tseq, firsthit, identifier, seq.Substring(0, offset), seq.Substring(offset + lengthpatch))));
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
                score += alphabet.scoring_matrix[alphabet.getIndexInAlphabet(template[position + i]), alphabet.getIndexInAlphabet(read[i])];
            }
            return score;
        }
        /// <summary>Do a local alignment based on the SmithWaterman algorithm of two sequences. </summary>
        /// <param name="template">The template sequence to use.</param>
        /// <param name="query">The query sequence to use.</param>
        static public SequenceMatch SmithWaterman(ICollection<AminoAcid> template, ICollection<AminoAcid> query, int right_id, Alphabet alphabet)
        {
            var score_matrix = new (int, Direction)[template.Count + 1, query.Count + 1]; // Default value of 0
            int max_value = 0;
            (int, int) max_index = (0, 0);

            int tem_pos, query_pos;

            for (tem_pos = 1; tem_pos <= template.Count; tem_pos++)
            {
                for (query_pos = 1; query_pos <= query.Count; query_pos++)
                {
                    bool gap = alphabet.IsGap(template.ElementAt(tem_pos - 1)) || alphabet.IsGap(query.ElementAt(query_pos - 1));
                    int score = template.ElementAt(tem_pos - 1).Homology(query.ElementAt(query_pos - 1));
                    // Calculate the score for the current position
                    int a = score_matrix[tem_pos - 1, query_pos - 1].Item1 + score; // Match
                    int b = score_matrix[tem_pos, query_pos - 1].Item1 - (score_matrix[tem_pos, query_pos - 1].Item2 == Direction.GapQuery ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty); // GapRight
                    int c = score_matrix[tem_pos - 1, query_pos].Item1 - (score_matrix[tem_pos - 1, query_pos].Item2 == Direction.GapTemplate ? alphabet.GapExtendPenalty : alphabet.GapStartPenalty); // GapLeft
                    int d = 0;

                    if (a > b && a > c && a > d)
                        score_matrix[tem_pos, query_pos] = (a, Direction.Match);
                    else if (b > c && b > d)
                        score_matrix[tem_pos, query_pos] = (b, Direction.GapQuery);
                    else if (c > d)
                        score_matrix[tem_pos, query_pos] = (c, Direction.GapTemplate);
                    else
                        score_matrix[tem_pos, query_pos] = (d, Direction.NoMatch);

                    // Keep track of the maximal value
                    if (score_matrix[tem_pos, query_pos].Item1 > max_value)
                    {
                        max_value = score_matrix[tem_pos, query_pos].Item1;
                        max_index = (tem_pos, query_pos);
                    }
                }
            }

            // Traceback
            var match_list = new List<SequenceMatch.MatchPiece>();

            tem_pos = max_index.Item1;
            query_pos = max_index.Item2;
            bool found_end = false;

            while (!found_end)
            {
                switch (score_matrix[tem_pos, query_pos].Item2)
                {
                    case Direction.Match:
                        match_list.Add(new SequenceMatch.Match(1));
                        tem_pos--;
                        query_pos--;
                        break;
                    case Direction.GapTemplate:
                        match_list.Add(new SequenceMatch.GapTemplate(1));
                        tem_pos--;
                        break;
                    case Direction.GapQuery:
                        match_list.Add(new SequenceMatch.GapContig(1));
                        query_pos--;
                        break;
                    case Direction.NoMatch:
                        found_end = true;
                        break;
                }
            }
            match_list.Reverse();

            var match = new SequenceMatch(tem_pos, query_pos, max_value, match_list, query, right_id);
            return match;
        }

        enum Direction { NoMatch, GapTemplate, GapQuery, Match }
    }
    /// <summary>A class to save a match of two sequences in a space efficient way, based on CIGAR strings.</summary>
    public class SequenceMatch
    {
        /// <summary>The position on the template where the match begins.</summary>
        public int StartTemplatePosition;
        public int StartQueryPosition;
        public int Score;
        public List<MatchPiece> Alignment;
        public ICollection<AminoAcid> Sequence;
        public int SequenceID;
        public int Length
        {
            get
            {
                return Alignment.Aggregate(0, (a, b) => a + b.count);
            }
        }
        public SequenceMatch(int tpos, int qpos, int s, List<MatchPiece> m, ICollection<AminoAcid> seq, int seqid)
        {
            StartTemplatePosition = tpos;
            StartQueryPosition = qpos;
            Score = s;
            Alignment = m;
            Sequence = seq;
            SequenceID = seqid;
            simplify();
        }
        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append($"SequenceMatch< starting at template: {StartTemplatePosition}, starting at query: {StartQueryPosition}, score: {Score}, match: ");
            foreach (var m in Alignment)
            {
                buffer.Append(m.ToString());
            }
            buffer.Append(" >");
            return buffer.ToString();
        }
        public abstract class MatchPiece
        {
            public int count;
            public MatchPiece(int c)
            {
                count = c;
            }
            public override string ToString()
            {
                return $"{count}None";
            }
        }
        public class Match : MatchPiece
        {
            public Match(int c) : base(c) { }
            public override string ToString()
            {
                return $"{count}M";
            }
        }
        public class GapTemplate : MatchPiece
        {
            public GapTemplate(int c) : base(c) { }
            public override string ToString()
            {
                return $"{count}D";
            }
        }
        public class GapContig : MatchPiece
        {
            public GapContig(int c) : base(c) { }
            public override string ToString()
            {
                return $"{count}I";
            }
        }
        void simplify()
        {
            MatchPiece lastElement = null;
            int count = Alignment.Count();
            int i = 0;
            while (i < count)
            {
                if (lastElement != null && lastElement.GetType() == Alignment[i].GetType())
                {
                    Alignment[i].count += Alignment[i - 1].count;
                    Alignment.RemoveAt(i - 1);
                    i--;
                    count--;
                }
                lastElement = Alignment[i];
                i++;
            }
        }
        public int TotalMatches()
        {
            int sum = 0;
            foreach (var m in Alignment)
            {
                if (m.GetType() == typeof(SequenceMatch.Match)) sum += m.count;
            }
            return sum;
        }
    }
}