using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using AssemblyNameSpace;

namespace SystematicTest
{
    class Test
    {
        static void Main()
        {
            var alphabet = new Alphabet("examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 1);
            var original_sequences = new Dictionary<string, AminoAcid[]>();

            foreach (var file in Directory.GetFiles("examples/systematictest/sequences"))
            {
                if (file != "Mix-all.txt")
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var fi = new MetaData.FileIdentifier(file, name);
                    var reads = OpenReads.Fasta(fi);
                    var sequence = StringToSequence(reads[0].Item1 + reads[1].Item1, alphabet);
                    original_sequences.Add(name, sequence);
                }
            }

            var results = new List<(string Type, string Variant, string Number, string K, int Score, string CIGAR)>();

            foreach (var file in Directory.GetFiles("systematictest/data"))
            {
                if (file.EndsWith(".fasta"))
                {
                    var name = Path.GetFileNameWithoutExtension(file).Substring(0, 10);
                    var pieces = Path.GetFileNameWithoutExtension(file).Split("-");
                    var fi = new MetaData.FileIdentifier(file, name);
                    var reads = OpenReads.Fasta(fi);
                    var sequence = StringToSequence(reads[0].Item1, alphabet);
                    var match = HelperFunctionality.SmithWaterman(original_sequences[name], sequence, alphabet);
                    results.Add((pieces[0], pieces[1], pieces[2], pieces[3], match.Score, match.Alignment.CIGAR()));
                }
            }

            foreach (var line in results)
            {
                Console.WriteLine($"{line}");
            }
        }

        /// <summary>
        /// Gets the sequence in AminoAcids from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        static AminoAcid[] StringToSequence(string input, Alphabet alphabet)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alphabet, input[i]);
            }
            return output;
        }
    }
}