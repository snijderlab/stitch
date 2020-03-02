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

            var results = new List<List<string>>();

            foreach (var file in Directory.GetFiles("systematictest/data"))
            {
                if (file.EndsWith(".fasta"))
                {
                    var name = Path.GetFileNameWithoutExtension(file).Substring(6, 10);
                    var pieces = Path.GetFileNameWithoutExtension(file).Split("-");
                    var fi = new MetaData.FileIdentifier(file, name);
                    var reads = OpenReads.Fasta(fi);
                    var sequence = StringToSequence(reads[0].Item1, alphabet);
                    var match = HelperFunctionality.SmithWaterman(original_sequences[name], sequence, alphabet);

                    // Tried to get the match/del/insert numbers per region, does not fully work yet!
                    if (name == "IgG1-K-001")
                    {
                        var regions = new List<(int, int, int)>();
                        var pos = match.StartTemplatePosition;
                        var region = 0;
                        var positions = new List<int> { 99, 107, 123, 453, 549, 562, 668 };
                        var nmatch = 0;
                        var del = 0;
                        var insert = 0;

                        foreach (var piece in match.Alignment)
                        {
                            if (piece.Length + pos >= positions[region])
                            {
                                var c = positions[region] - pos;
                                switch (piece)
                                {
                                    case SequenceMatch.Match _:
                                        nmatch += c;
                                        break;
                                    case SequenceMatch.GapQuery _:
                                        del += c;
                                        break;
                                    case SequenceMatch.GapTemplate _:
                                        insert += c;
                                        break;
                                }
                                regions.Add((nmatch, del, insert));
                                region += 1;
                                nmatch = 0;
                                del = 0;
                                insert = 0;
                                c = piece.Length - c;
                                switch (piece)
                                {
                                    case SequenceMatch.Match _:
                                        nmatch += c;
                                        break;
                                    case SequenceMatch.GapQuery _:
                                        del += c;
                                        break;
                                    case SequenceMatch.GapTemplate _:
                                        insert += c;
                                        break;
                                }
                                pos += piece.Length;
                            }
                            else
                            {
                                switch (piece)
                                {
                                    case SequenceMatch.Match _:
                                        nmatch += piece.Length;
                                        break;
                                    case SequenceMatch.GapQuery _:
                                        del += piece.Length;
                                        break;
                                    case SequenceMatch.GapTemplate _:
                                        insert += piece.Length;
                                        break;
                                }
                                pos += piece.Length;
                            }
                        }

                        foreach (var r in regions)
                        {
                            Console.WriteLine($"{r}");
                        }
                    }

                    results.Add(new List<string> { pieces[1], pieces[2], pieces[3], pieces[4], pieces[5].Split(',')[0], pieces[6], pieces[7], match.Score.ToString(), match.StartTemplatePosition.ToString(), match.Alignment.CIGAR() });
                }
            }

            var sb = new StringBuilder();
            sb.Append("sep=;\n");

            foreach (var line in results)
            {
                foreach (var column in line)
                {
                    sb.Append(column);
                    sb.Append(';');
                }
                sb.Append('\n');
            }

            File.WriteAllText("systematictest/results.csv", sb.ToString());
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