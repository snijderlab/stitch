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
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-GB");

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
                    var regions = new List<double>();

                    // Tried to get the match/del/insert numbers per region, does not fully work yet!
                    if (name == "IgG1-K-001")
                    {
                        var pos = match.StartTemplatePosition;
                        var region = 0;
                        var positions = new List<int> { 14, 33, 50, 58, 96, 111, 120, 453, 453 + 26, 453 + 32, 453 + 49, 453 + 56, 453 + 90, 453 + 99, 453 + 109, 453 + 215 };//99, 107, 123, 453, 549, 562, 668 };
                        var nmatch = 0;
                        var del = 0;
                        var insert = 0;

                        foreach (var piece in match.Alignment)
                        {
                            int res = piece.Length;
                            while (region < positions.Count - 1 && pos + res >= positions[region])
                            {
                                var c = positions[region] - pos;
                                int skip = c;
                                if (c < 0)
                                {
                                    skip = positions[region];
                                    c = 0;
                                }
                                switch (piece)
                                {
                                    case SequenceMatch.Match _:
                                        nmatch += c;
                                        break;
                                    case SequenceMatch.GapInQuery _:
                                        insert += c;
                                        break;
                                    case SequenceMatch.GapInTemplate _:
                                        del += c;
                                        break;
                                }
                                regions.Add((double)nmatch / (positions[region] - (region == 0 ? 0 : positions[region - 1])));
                                region += 1;
                                nmatch = 0;
                                del = 0;
                                insert = 0;
                                res -= c;
                                pos += skip;
                            }
                            switch (piece)
                            {
                                case SequenceMatch.Match _:
                                    nmatch += res;
                                    break;
                                case SequenceMatch.GapInQuery _:
                                    insert += res;
                                    break;
                                case SequenceMatch.GapInTemplate _:
                                    del += res;
                                    break;
                            }
                            pos += res;
                        }
                        regions.Add((double)nmatch / (positions[region] - (region == 0 ? 0 : positions[region - 1])));

                        Console.WriteLine(Path.GetFileNameWithoutExtension(file));
                        foreach (var r in regions)
                        {
                            Console.WriteLine($"{r}");
                        }

                        Console.WriteLine("\n");
                    }

                    var line = new List<string> { pieces[1], pieces[2], pieces[3], pieces[4], pieces[5].Split(',')[0], pieces[6], pieces[7], match.Score.ToString(), match.StartTemplatePosition.ToString(), match.Alignment.CIGAR(), $"=HYPERLINK(\"C:\\Users\\douwe\\source\\repos\\research-project-amino-acid-alignment\\systematictest\\data\\{Path.GetFileNameWithoutExtension(file)}.html\"; \"HTML\")" };
                    foreach (var r in regions)
                    {
                        line.Add(r.ToString());
                    }
                    results.Add(line);
                }
            }

            var sb = new StringBuilder();
            sb.Append("sep=|\nType|Variant|Number|Proteases|Percentage|Alphabet|K|Score|StartPosition|Alignment|Link\n");

            foreach (var line in results)
            {
                foreach (var column in line)
                {
                    sb.Append(column);
                    sb.Append('|');
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