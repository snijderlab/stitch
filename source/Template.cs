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
    class TemplateDatabase
    {
        Alphabet alphabet;
        public List<Template> Templates;
        public TemplateDatabase(string path, string name, Alphabet alp)
        {
            var sequences = OpenReads.Fasta(new MetaData.FileIdentifier(path, name));
            alphabet = alp;
            Templates = new List<Template>();

            foreach (var pair in sequences)
            {
                var parsed = StringToSequence(pair.Item1);
                Templates.Add(new Template(parsed, pair.Item2));
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
        public void Match(List<CondensedNode> condensed_graph)
        {
            var sequences = new List<AminoAcid[]>();
            foreach (var node in condensed_graph)
            {
                sequences.Add(node.Sequence.ToArray());
            }

            int y = 0;
            foreach (var tem in Templates)
            {
                int x = 0;
                foreach (var seq in sequences)
                {
                    tem.AddMatch(HelperFunctionality.SmithWaterman(seq, tem.Sequence));
                    x++;
                }
                y++;
            }
        }
    }
    public class Template
    {
        public AminoAcid[] Sequence;
        public MetaData.IMetaData MetaData;
        public int Score;
        public readonly List<SequenceMatch> Matches;
        public Template(AminoAcid[] seq, MetaData.IMetaData meta)
        {
            Sequence = seq;
            MetaData = meta;
            Score = -1;
            Matches = new List<SequenceMatch>();
        }
        public void AddMatch(SequenceMatch match) {
            Score += match.Score;
            Matches.Add(match);
        }
    }
}