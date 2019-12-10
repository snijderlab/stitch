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

        /// <summary> Set the alphabet of the assembler. </summary>
        /// <param name="rules"> A list of rules implemented as tuples containing the chars to connect, 
        /// the value to put into the matrix and whether or not the rule should be bidirectional (the value
        ///  in the matrix is the same both ways). </param>
        /// <param name="diagonals_value"> The value to place on the diagonals of the matrix. </param>
        /// <param name="input"> The alphabet to use, it will be iterated over from the front to the back so
        /// the best case scenario has the most used characters at the front of the string. </param>
        public Alphabet(List<ValueTuple<char, char, int, bool>> rules = null, int diagonals_value = 1, string input = "LSAEGVRKTPDINQFYHMCWOU")
        {
            alphabet = input.ToCharArray();

            scoring_matrix = new int[alphabet.Length, alphabet.Length];

            // Only set the diagonals to te given value
            for (int i = 0; i < alphabet.Length; i++) scoring_matrix[i, i] = diagonals_value;

            // Use the rules to 
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    scoring_matrix[getIndexInAlphabet(rule.Item1), getIndexInAlphabet(rule.Item2)] = rule.Item3;
                    if (rule.Item4) scoring_matrix[getIndexInAlphabet(rule.Item2), getIndexInAlphabet(rule.Item1)] = rule.Item3;
                }
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
        /// <summary> Set the alphabet based on data in csv format. </summary>
        /// <param name="data"> The csv data. </param>
        /// <param name="type"> To indicate if the data is data or a path to data </param>
        public Alphabet(string data, AlphabetParamType type)
        {
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
                    array.Add(line.Split(new char[] { ';', ',' }).Select(x => x.Trim(new char[] { ' ', '\n', '\r', '\t', '-', '.' })).ToArray());
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