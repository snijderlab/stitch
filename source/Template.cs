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
using System.ComponentModel;


namespace AssemblyNameSpace
{
    class TemplateDatabase {
        Alphabet alphabet;
        public List<(AminoAcid[], MetaData.IMetaData)> templates;
        public TemplateDatabase(string path, string name, Alphabet alp) {
            var sequences = OpenReads.Fasta(new MetaData.FileIdentifier(path, name));
            alphabet = alp;
            templates = new List<(AminoAcid[], MetaData.IMetaData)>();

            foreach (var pair in sequences) {
                var parsed = StringToSequence(pair.Item1);
                templates.Add((parsed, pair.Item2));
            }
        }
        /// <summary>
        /// Gets the sequence in AminoAcids from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alphabet, input[i]);
            }
            return output;
        }
        public void Match(List<CondensedNode> condensed_graph) {
            var sequences = new List<AminoAcid[]>();
            foreach (var node in condensed_graph) {
                sequences.Add(node.Sequence.ToArray());
            }

            var scores = new int[condensed_graph.Count(), templates.Count()];

            int x = 0;
            foreach (var seq in sequences) {
                int y = 0;
                foreach (var tem in templates) {
                    scores[x, y] = HelperFunctionality.SmithWaterman(seq, tem.Item1);
                    y++;
                }
                x++;
            }

            var buffer = new StringBuilder();
            buffer.AppendLine("Rows: Contigs, Columns: Templates");
            int a = condensed_graph.Count();
            int b = templates.Count();
            for (int i = 0; i < a; i++) {
                for (int j = 0; j < b; j++) {
                    buffer.Append(scores[i,j]);
                    if (j != b-1) buffer.Append(",");
                }
                buffer.Append("\n");
            }
            Console.WriteLine(buffer.ToString());
            Console.WriteLine(AminoAcid.ArrayToString(condensed_graph[6].Sequence.ToArray()));
            Console.WriteLine(AminoAcid.ArrayToString(templates[132].Item1));
        }
    }
}