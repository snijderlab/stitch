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
    /// <summary>
    /// To contain an alphabet with scoring matrix to score pairs of amino acids
    /// </summary>
    public class Alphabet
    {
        /// <summary> The matrix used for scoring of the alignment between two characters in the alphabet. 
        /// As such this matrix is rectangular. </summary>
        public readonly int[,] ScoringMatrix;

        /// <summary>
        /// The position for each possible amino acid in the ScoringMatrix for fast lookups.
        /// </summary>
        public readonly Dictionary<char, int> PositionInScoringMatrix;

        /// <summary>
        /// The penalty for opening a gap in an alignment
        /// </summary>
        public readonly int GapStartPenalty;

        /// <summary>
        /// The penalty for extending a gap in an alignment
        /// </summary>
        public readonly int GapExtendPenalty;

        /// <summary>
        /// The char that represents a gap
        /// </summary>
        public const char GapChar = '.';

        /// <summary>
        /// The char that represents a stopcodon, where translation will stop.
        /// </summary>
        public const char StopCodon = '*';

        /// <summary> Find the index of the given character in the alphabet. </summary>
        /// <param name="c"> The character to look up. </param>
        /// <returns> The index of the character in the alphabet or -1 if it is not in the alphabet. </returns>
        public int GetIndexInAlphabet(char c)
        {
            try
            {
                return PositionInScoringMatrix[c];
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"The char '{c}' could not be found in this alphabet.");
            }
        }

        /// <summary>
        /// To indicate if the given string is data or a path to the data
        /// </summary>
        public enum AlphabetParamType
        {
            /// <summary> It is the data itself. </summary>
            Data,
            /// <summary> It is a path to a file containing the data. </summary>
            Path
        }

        /// <summary>
        /// Create a new Alphabet
        /// </summary>
        /// <param name="alphabetValue">The RunParameter to use</param>
        /// <returns></returns>
        public Alphabet(RunParameters.AlphabetParameter alphabetValue) : this(alphabetValue.Alphabet, alphabetValue.ScoringMatrix, alphabetValue.GapStartPenalty, alphabetValue.GapExtendPenalty) { }

        /// <summary> Create a new Alphabet </summary>
        /// <param name="data"> The csv data. </param>
        /// <param name="type"> To indicate if the data is data or a path to data </param>
        /// <param name="gap_start_penalty">The penalty for opening a gap in an alignment</param>
        /// <param name="gap_extend_penalty">The penalty for extending a gap in an alignment</param>
        public Alphabet(string data, AlphabetParamType type, int gap_start_penalty, int gap_extend_penalty)
        {
            GapStartPenalty = gap_start_penalty;
            GapExtendPenalty = gap_extend_penalty;

            var result = InputNameSpace.ParseHelper.ParseAlphabetData(data, type);
            var alphabet = result.Item1;
            ScoringMatrix = result.Item2;

            PositionInScoringMatrix = new Dictionary<char, int>();
            for (int i = 0; i < alphabet.Length; i++)
            {
                PositionInScoringMatrix.Add(alphabet[i], i);
            }
        }

        public Alphabet(char[] alphabet, int[,] data, int gap_start_penalty, int gap_extend_penalty)
        {
            GapStartPenalty = gap_start_penalty;
            GapExtendPenalty = gap_extend_penalty;
            ScoringMatrix = data;

            PositionInScoringMatrix = new Dictionary<char, int>();
            for (int i = 0; i < alphabet.Length; i++)
            {
                PositionInScoringMatrix.Add(alphabet[i], i);
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.AppendLine($"Alphabet");
            foreach (char c in PositionInScoringMatrix.Keys)
            {
                buffer.Append(c);
            }
            buffer.Append($"\nWith gap {GapChar}\n");
            for (int x = 0; x < ScoringMatrix.GetLength(0); x++)
            {
                for (int y = 0; y < ScoringMatrix.GetLength(1); y++)
                {
                    buffer.Append($"{ScoringMatrix[x, y],4}");
                }
                buffer.Append("\n");
            }
            buffer.Append("\n");
            return buffer.ToString();
        }
    }
}