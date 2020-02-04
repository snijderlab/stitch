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
        public int[,] scoring_matrix;
        /// <summary> The alphabet used for alignment. The default value is all the amino acids in order of
        /// natural abundance in prokaryotes to make finding the right amino acid a little bit faster. </summary>
        public char[] alphabet;
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
        private const char GapChar = '*';
        public readonly int GapIndex;
        /// <summary> Find the index of the given character in the alphabet. </summary>
        /// <param name="c"> The character to look up. </param>
        /// <returns> The index of the character in the alphabet or -1 if it is not in the alphabet. </returns>
        public int getIndexInAlphabet(char c)
        {
            for (int i = 0; i < alphabet.Length; i++)
            {
                if (c == alphabet[i])
                {
                    return i;
                }
            }
            Console.WriteLine($"Could not find '{c}' in the alphabet: '{alphabet}'");
            return -1;
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
        public Alphabet(RunParameters.AlphabetValue alphabetValue) : this(alphabetValue.Data, AlphabetParamType.Data, alphabetValue.GapStartPenalty, alphabetValue.GapExtendPenalty) {}
        /// <summary> Create a new Alphabet </summary>
        /// <param name="data"> The csv data. </param>
        /// <param name="type"> To indicate if the data is data or a path to data </param>
        /// <param name="gap_start_penalty">The penalty for opening a gap in an alignment</param>
        /// <param name="gap_extend_penalty">The penalty for extending a gap in an alignment</param>
        public Alphabet(string data, AlphabetParamType type, int gap_start_penalty, int gap_extend_penalty)
        {
            GapStartPenalty = gap_start_penalty;
            GapExtendPenalty = gap_extend_penalty;

            if (type == AlphabetParamType.Path)
            {
                try
                {
                    data = File.ReadAllText(data);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not open the alphabetfile: {data}; {e.Message}");
                }
            }
            var input = data.Split('\n');
            int rows = input.Length;
            List<string[]> array = new List<string[]>();

            foreach (string line in input)
            {
                if (line != "")
                    array.Add(line.Split(new char[] { ';', ',' }).Select(x => x.Trim(new char[] { ' ', '\n', '\r', '\t', '.' })).ToArray());
            }

            int columns = array[0].Length;

            for (int line = 0; line < rows; line++)
            {
                if (rows != array[line].Length)
                {
                    throw new ParseException($"The amount of rows ({rows}) is not equal to the amount of columns ({array[line].Length}) for line {line + 1}.");
                }
            }

            alphabet = String.Join("", array[0].SubArray(1, columns - 1)).ToCharArray();

            if (!alphabet.Contains(GapChar)) {
                alphabet = alphabet.Concat(new char[] {GapChar}).ToArray();
            }
            GapIndex = getIndexInAlphabet(GapChar);

            scoring_matrix = new int[columns - 1, columns - 1];

            for (int i = 0; i < columns - 1; i++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    try
                    {
                        scoring_matrix[i, j] = Int32.Parse(array[i + 1][j + 1]);
                    }
                    catch
                    {
                        throw new ParseException($"The reading on the alphabet file was not successfull, because at column {i} and row {j} the value ({array[i + 1][j + 1]}) is not a valid integer.");
                    }
                }
            }
        }
    }
    
}