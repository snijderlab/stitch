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
    /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can 
    /// be used in an efficient way </summary>
    public struct AminoAcid
    {
        /// <summary> The code (index of the char in the alpabet array of the parent).
        /// The only way to change it is in the creator. </summary>
        /// <value> The code of this AminoAcid. </value>
        public readonly int Code;
        /// <summary>
        /// The alphabet used
        /// </summary>
        public Alphabet alphabet;

        /// <summary> The creator of AminoAcids. </summary>
        /// <param name="alphabet_input"> The alphabet used. </param>
        /// <param name="input"> The character to store in this AminoAcid. </param>
        public AminoAcid(Alphabet alphabet_input, char input)
        {
            alphabet = alphabet_input;
            Code = alphabet.getIndexInAlphabet(input);
        }
        /// <summary> Will create a string of this AminoAcid. Consiting of the character used to 
        /// create this AminoAcid. </summary>
        /// <returns> Returns the character of this AminoAcid (based on the alphabet) as a string. </returns>
        public override string ToString()
        {
            return alphabet.alphabet[Code].ToString();
        }
        /// <summary> Will create a string of a collection of AminoAcids. </summary>
        /// <param name="collaction"> The collaction to create a string from. </param>
        /// <returns> Returns the string of the collaction. </returns>
        public static string ArrayToString(ICollection<AminoAcid> collection)
        {
            var builder = new StringBuilder();
            foreach (AminoAcid aa in collection)
            {
                builder.Append(aa.ToString());
            }
            return builder.ToString();
        }
        /// <summary> To check for equality of the AminoAcids. Will return false if the object is not an AminoAcid. </summary>
        /// <remarks> Implemented as the equals operator (==). </remarks>
        /// <param name="obj"> The object to check equality with. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public override bool Equals(object obj)
        {
            return obj is AminoAcid && this == (AminoAcid)obj;
        }
        /// <summary> To check for equality of arrays of AminoAcids. </summary>
        /// <remarks> Implemented as a short circuiting loop with the equals operator (==). </remarks>
        /// <param name="left"> The first object to check equality with. </param>
        /// <param name="right"> The second object to check equality with. </param>
        /// <returns> Returns true when the aminoacid arrays are equal. </returns>
        public static bool ArrayEquals(AminoAcid[] left, AminoAcid[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary> To check for equality of AminoAcids. </summary>
        /// <remarks> Implemented as a check for equality of the Code of both AminoAcids. </remarks>
        /// <param name="x"> The first AminoAcid to test. </param>
        /// <param name="y"> The second AminoAcid to test. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public static bool operator ==(AminoAcid x, AminoAcid y)
        {
            return x.Code == y.Code;
        }
        /// <summary> To check for inequality of AminoAcids. </summary>
        /// <remarks> Implemented as a a reverse of the equals operator. </remarks>
        /// <param name="x"> The first AminoAcid to test. </param>
        /// <param name="y"> The second AminoAcid to test. </param>
        /// <returns> Returns false when the Amino Acids are equal. </returns>
        public static bool operator !=(AminoAcid x, AminoAcid y)
        {
            return !(x == y);
        }
        /// <summary> To get a hashcode for this AminoAcid. </summary>
        /// <returns> Returns the hashcode of the AminoAcid. </returns>
        public override int GetHashCode()
        {
            return this.Code.GetHashCode();
        }
        /// <summary> Calculating homology, using the scoring matrix of the parent Assembler. 
        /// See <see cref="Alphabet.scoring_matrix"/> for the scoring matrix.
        /// See <see cref="Alphabet(string, Alphabet.AlphabetParamType)"/> on how to change the scoring matrix.</summary>
        /// <remarks> Depending on which rules are put into the scoring matrix the order in which this 
        /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to 
        /// <c>b.Homology(a)</c>. </remarks>
        /// <param name="right"> The other AminoAcid to use. </param>
        /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
        public int Homology(AminoAcid right)
        {
            try
            {
                return alphabet.scoring_matrix[this.Code, right.Code];
            }
            catch (Exception e)
            {
                Console.WriteLine($"Got an error while looking up the homology for this code {this.Code} and that code {right.Code}, probably there is one (or more) character that is not valid");
                throw e;
            }
        }
        /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix 
        /// of the parent Assembler. </summary>
        /// <remarks> Two arrays of different length will result in a value of 0. This function loops
        /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
        /// <param name="left"> The first object to calculate homology with. </param>
        /// <param name="right"> The second object to calculate homology with. </param>
        /// <returns> Returns the homology between the two aminoacid arrays. </returns>
        public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right)
        {
            int score = 0;
            if (left.Length != right.Length)
                // Throw exception?
                return 0;
            for (int i = 0; i < left.Length; i++)
            {
                score += left[i].Homology(right[i]);
            }
            return score;
        }
    }
}