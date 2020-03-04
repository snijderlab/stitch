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
            var original_positions = new Dictionary<string, int[]>();

            // See positions.txt
            original_positions.Add("IgG1-K-001", new int[16] { 25, 33, 50, 58, 96, 109, 120, 453, 479, 485, 502, 507, 543, 552, 562, 668 });
            original_positions.Add("IgG1-K-002", new int[16] { 25, 33, 50, 58, 96, 109, 120, 451, 477, 483, 500, 505, 541, 550, 560, 665 });
            original_positions.Add("IgG1-L-001", new int[16] { 25, 33, 50, 58, 96, 109, 120, 452, 477, 485, 502, 506, 541, 552, 562, 668 });
            original_positions.Add("IgG1-L-002", new int[16] { 25, 33, 50, 58, 96, 109, 120, 450, 473, 482, 499, 502, 538, 549, 559, 667 });
            original_positions.Add("IgG2-K-001", new int[16] { 25, 33, 50, 58, 96, 107, 118, 444, 470, 476, 493, 496, 532, 542, 552, 658 });
            original_positions.Add("IgG2-K-002", new int[16] { 25, 35, 52, 59, 97, 107, 118, 444, 470, 477, 494, 497, 533, 542, 552, 659 });
            original_positions.Add("IgG2-L-001", new int[16] { 25, 33, 50, 58, 96, 109, 120, 450, 475, 484, 501, 504, 540, 551, 561, 667 });
            original_positions.Add("IgG2-L-002", new int[16] { 25, 33, 50, 58, 96, 109, 120, 456, 481, 489, 506, 509, 545, 556, 566, 672 });
            original_positions.Add("IgG4-K-001", new int[16] { 25, 33, 50, 58, 96, 103, 114, 441, 467, 473, 490, 493, 529, 538, 548, 655 });
            original_positions.Add("IgG4-K-002", new int[16] { 25, 35, 52, 59, 97, 110, 121, 448, 474, 480, 497, 500, 536, 544, 554, 661 });
            original_positions.Add("IgG4-L-001", new int[16] { 25, 33, 50, 58, 96, 109, 120, 449, 474, 480, 497, 500, 536, 547, 557, 663 });

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

                    // Get the coverage per region
                    var pos = match.StartTemplatePosition;
                    var totalmatch = 0;
                    var region = 0;
                    var positions = original_positions[name];
                    var nmatch = 0;
                    var del = 0;
                    var insert = 0;

                    foreach (var piece in match.Alignment)
                    {
                        int res = piece.Length;
                        while (region < positions.Length - 1 && pos + res >= positions[region])
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
                            totalmatch += nmatch;
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
                    totalmatch += nmatch;
                    regions.Add((double)nmatch / (positions[region] - (region == 0 ? 0 : positions[region - 1])));
                    regions.Add((double)totalmatch / positions[positions.Length - 1]);

                    var line = new List<string> { pieces[1], pieces[2], pieces[3], pieces[4], pieces[5].Split(',')[0], pieces[6], pieces[7], match.Score.ToString(), match.StartTemplatePosition.ToString(), match.Alignment.CIGAR(), $"=HYPERLINK(\"C:\\Users\\douwe\\source\\repos\\research-project-amino-acid-alignment\\systematictest\\data\\{Path.GetFileNameWithoutExtension(file)}.html\"; \"HTML\")" };
                    foreach (var r in regions)
                    {
                        line.Add(Math.Min(1, r).ToString());
                    }
                    results.Add(line);
                }
            }

            var sb = new StringBuilder();
            sb.Append("sep=|\nType|Variant|Number|Proteases|Percentage|Alphabet|K|Score|StartPosition|Alignment|Link|F1|CDR1|F2|CDR2|F3|CDR3|F4|C HC|F1|CDR1|F2|CDR2|F3|CDR3|F4|C LC|Total\n");

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