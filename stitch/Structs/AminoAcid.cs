using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Stitch {
    /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can
    /// be used in an efficient way. </summary>
    public struct AminoAcid : IComparable, IEquatable<AminoAcid> {
        /// <summary> The code (index of the char in the alphabet array of the parent).
        /// The only way to change it is in the creator. </summary>
        /// <value> The code of this AminoAcid. </value>
        public readonly char Character;

        /// <summary> The creator of AminoAcids. </summary>
        /// <param name="alphabet"> The alphabet used. </param>
        /// <param name="input"> The character to store in this AminoAcid. </param>
        public AminoAcid(Alphabet alphabet, char input) {
            if (alphabet.GetIndexInAlphabet(input) == -1) throw new ArgumentException("Invalid AminoAcid");
            Character = input;
        }

        /// <summary> The creator of AminoAcids. </summary>
        /// <param name="alphabet"> The alphabet used. </param>
        /// <param name="input"> The character to store in this AminoAcid. </param>
        public AminoAcid(ScoringMatrix alphabet, char input) {
            if (!alphabet.Contains(input)) throw new ArgumentException("Invalid AminoAcid");
            Character = input;
        }

        /// <summary> The unsafe creator of AminoAcids. </summary>
        /// <param name="input"> The character to store in this AminoAcid. </param>
        AminoAcid(char input) {
            Character = input;
        }

        /// <summary> Create a new aminoacid while creating nice error messages if this was not possible. </summary>
        /// <param name="alphabet"> The alphabet to use. </param>
        /// <param name="input"> The amino acid as a char. </param>
        /// <param name="position"> If possible the location where this amino acids was defined to create nicer error messages. </param>
        /// <param name="fallback_context"> If possible a fallback to the above position to use if the position could not be given. </param>
        /// <returns> The aminoacid or an error message. </returns>
        public static ParseResult<AminoAcid> TryCreate(Alphabet alphabet, char input, FileRange? position = null, string fallback_context = null) {
            if (alphabet.GetIndexInAlphabet(input) == -1)
                if (position is FileRange fr)
                    return new ParseResult<AminoAcid>(new InputNameSpace.ErrorMessage(fr, "AminoAcid not in alphabet", $"The aminoacid '{input}' does not exist in the used alphabet, make sure this amino acid is correct and the correct alphabet is chosen."));
                else
                    return new ParseResult<AminoAcid>(new InputNameSpace.ErrorMessage(fallback_context ?? input.ToString(), "AminoAcid not in alphabet", $"The aminoacid '{input}' does not exist in the used alphabet, make sure this amino acid is correct and the correct alphabet is chosen."));
            else
                return new ParseResult<AminoAcid>(new AminoAcid(alphabet, input));
        }

        /// <summary> Create a new aminoacid while creating nice error messages if this was not possible. </summary>
        /// <param name="alphabet"> The alphabet to use. </param>
        /// <param name="input"> The amino acid as a char. </param>
        /// <param name="position"> If possible the location where this amino acids was defined to create nicer error messages. </param>
        /// <param name="fallback_context"> If possible a fallback to the above position to use if the position could not be given. </param>
        /// <returns> The aminoacid or an error message. </returns>
        public static ParseResult<AminoAcid> TryCreate(ScoringMatrix alphabet, char input, FileRange? position = null, string fallback_context = null) {
            if (!alphabet.Contains(input))
                if (position is FileRange fr)
                    return new ParseResult<AminoAcid>(new InputNameSpace.ErrorMessage(fr, "AminoAcid not in alphabet", $"The aminoacid '{input}' does not exist in the used alphabet, make sure this amino acid is correct and the correct alphabet is chosen."));
                else
                    return new ParseResult<AminoAcid>(new InputNameSpace.ErrorMessage(fallback_context ?? input.ToString(), "AminoAcid not in alphabet", $"The aminoacid '{input}' does not exist in the used alphabet, make sure this amino acid is correct and the correct alphabet is chosen."));
            else
                return new ParseResult<AminoAcid>(new AminoAcid(input));
        }

        /// <summary> Will create a string of this AminoAcid. Consisting of the character used to
        /// create this AminoAcid. </summary>
        /// <returns> Returns the character of this AminoAcid (based on the alphabet) as a string. </returns>
        public override string ToString() {
            return Character.ToString();
        }

        /// <summary> Will create a string of a collection of AminoAcids. </summary>
        /// <param name="collection"> The collection to create a string from. </param>
        /// <returns> Returns the string of the collection. </returns>
        public static string ArrayToString(IEnumerable<AminoAcid> collection) {
            var builder = new StringBuilder();
            foreach (AminoAcid aa in collection) {
                builder.Append(aa.ToString());
            }
            return builder.ToString();
        }

        /// <summary> Create an array of aminoacids from the given string. </summary>
        /// <param name="input"> The string to parse. </param>
        /// <param name="alphabet"> The alphabet to use. </param>
        /// <param name="position"> If possible the position where this sequence was defined to provide nicer error messages. </param>
        /// <returns> The array or a nice error message. </returns>
        public static ParseResult<AminoAcid[]> FromString(string input, Alphabet alphabet, FileRange? position = null) {
            var outEither = new ParseResult<AminoAcid[]>();
            AminoAcid[] output = new AminoAcid[input.Length];
            outEither.Value = output;
            for (int i = 0; i < input.Length; i++) {
                output[i] = TryCreate(alphabet, input[i], position, input).UnwrapOrDefault(outEither, new AminoAcid(alphabet, Stitch.Alphabet.GapChar));
            }
            return outEither;
        }

        /// <summary> Create an array of aminoacids from the given string. </summary>
        /// <param name="input"> The string to parse. </param>
        /// <param name="alphabet"> The alphabet to use. </param>
        /// <param name="position"> If possible the position where this sequence was defined to provide nicer error messages. </param>
        /// <returns> The array or a nice error message. </returns>
        public static ParseResult<AminoAcid[]> FromString(string input, ScoringMatrix alphabet, FileRange? position = null) {
            var outEither = new ParseResult<AminoAcid[]>();
            AminoAcid[] output = new AminoAcid[input.Length];
            outEither.Value = output;
            for (int i = 0; i < input.Length; i++) {
                output[i] = TryCreate(alphabet, input[i], position, input).UnwrapOrDefault(outEither, new AminoAcid(Stitch.Alphabet.GapChar));
            }
            return outEither;
        }

        /// <summary> Implement sorting for aminoacids, sort on alphabetical order of the used characters. </summary>
        /// <param name="obj"> The object to compare against. </param>
        /// <returns> Th alphabetical sort order for this AA vs the other AA, otherwise 0. </returns>
        public int CompareTo(object obj) {
            return obj != null && obj is AminoAcid aa ? this.Character.CompareTo(aa.Character) : 0;
        }

        /// <summary> To check for equality of the AminoAcids. Will return false if the object is not an AminoAcid. </summary>
        /// <param name="other"> The object to check equality with. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public override bool Equals(object obj) {
            return obj is AminoAcid aa && this.Equals(aa);
        }

        /// <summary> To check for equality of the AminoAcids. Will return false if the object is not an AminoAcid. </summary>
        /// <param name="other"> The object to check equality with. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public bool Equals(AminoAcid other) {
            return this.Character == other.Character;
        }

        public static bool operator ==(AminoAcid a, object obj) { return a.Equals(obj); }
        public static bool operator !=(AminoAcid a, object obj) { return !a.Equals(obj); }

        /// <summary> To check for equality of arrays of AminoAcids. </summary>
        /// <remarks> Implemented as a short circuiting loop with the equals operator (==). </remarks>
        /// <param name="left"> The first object to check equality with. </param>
        /// <param name="right"> The second object to check equality with. </param>
        /// <returns> Returns true when the aminoacid arrays are equal. </returns>
        public static bool ArrayEquals(AminoAcid[] left, AminoAcid[] right) {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++) {
                if (!left[i].Equals(right[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool ArrayEquals(ICollection<AminoAcid> left, ICollection<AminoAcid> right) {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++) {
                if (!left.ElementAt(i).Equals(right.ElementAt(i))) {
                    return false;
                }
            }
            return true;
        }

        /// <summary> To get a hash code for this AminoAcid. </summary>
        /// <returns> Returns the hash code of the AminoAcid. </returns>
        public override int GetHashCode() {
            return 7559 ^ (Character.GetHashCode() * 13);
        }

        /// <summary> Calculates homology between this and another AminoAcid, using the given alphabet.
        /// See <see cref="Alphabet"/>. </summary>
        /// <remarks> Depending on which rules are put into the scoring matrix the order in which this
        /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to
        /// <c>b.Homology(a)</c>. </remarks>
        /// <param name="right"> The other AminoAcid to use. </param>
        /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
        public int Homology(AminoAcid right, Alphabet alphabet) {
            try {
                return alphabet.ScoringMatrix[alphabet.PositionInScoringMatrix[this.Character], alphabet.PositionInScoringMatrix[right.Character]];
            } catch {
                (new InputNameSpace.ErrorMessage($"{this.Character} or {right.Character}", "Amino Acid could not be found", "Got an error while looking up the homology for these aminoacids probably there is one (or more) character that is not valid.")).Print();
                throw;
            }
        }

        /// <summary> Calculates homology between this and another AminoAcid, using the given alphabet.
        /// See <see cref="Alphabet"/>. </summary>
        /// <remarks> Depending on which rules are put into the scoring matrix the order in which this
        /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to
        /// <c>b.Homology(a)</c>. </remarks>
        /// <param name="right"> The other AminoAcid to use. </param>
        /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
        public int Homology(AminoAcid right, ScoringMatrix alphabet) {
            try {
                return alphabet.Score(new AminoAcid[] { this }, new AminoAcid[] { right });
            } catch {
                (new InputNameSpace.ErrorMessage($"{this.Character} or {right.Character}", "Amino Acid could not be found", "Got an error while looking up the homology for these aminoacids probably there is one (or more) character that is not valid.")).Print();
                throw;
            }
        }

        /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix
        /// of the parent Assembler. </summary>
        /// <remarks> Two arrays of different length will result in a value of 0. This function loops
        /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
        /// <param name="left"> The first object to calculate homology with. </param>
        /// <param name="right"> The second object to calculate homology with. </param>
        /// <returns> Returns the homology between the two aminoacid arrays. </returns>
        public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right, Alphabet alphabet = null) {
            int score = 0;
            if (left.Length != right.Length)
                return 0;
            for (int i = 0; i < left.Length; i++) {
                score += left[i].Homology(right[i], alphabet);
            }
            return score;
        }

        /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix
        /// of the parent Assembler. </summary>
        /// <remarks> Two arrays of different length will result in a value of 0. This function loops
        /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
        /// <param name="left"> The first object to calculate homology with. </param>
        /// <param name="right"> The second object to calculate homology with. </param>
        /// <returns> Returns the homology between the two aminoacid arrays. </returns>
        public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right, ScoringMatrix alphabet = null) {
            int score = 0;
            if (left.Length != right.Length)
                return 0;
            for (int i = 0; i < left.Length; i++) {
                score += left[i].Homology(right[i], alphabet);
            }
            return score;
        }
    }
}