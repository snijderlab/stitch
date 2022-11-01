using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Stitch
{
    public class Segment
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Alphabet Alphabet;
        public List<Template> Templates;
        public readonly double CutoffScore;
        [JsonIgnore]
        public readonly PhylogeneticTree.Tree<string> Hierarchy;
        public List<(int Group, int Index, ((int Position, int Score) Best, List<(int Position, int Score)> Scores) Score, AminoAcid[] SeqA, AminoAcid[] SeqB)> SegmentJoiningScores = new();
        [JsonIgnore]
        public PhylogeneticTree.ProteinHierarchyTree ScoreHierarchy;
        public readonly RunParameters.ScoringParameter Scoring;
        /// <summary> Create a new Segment based on the reads found in the given file. </summary>
        /// <param name="sequences">The reads to generate templates from</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="name">The name for this segment</param>
        /// <param name="cutoffScore">The cutoffscore for a path to be aligned to a template</param>
        /// <param name="index">The index of this template for cross reference purposes</param>
        /// <param name="scoring">The scoring behaviour to use in this segment</param>
        public Segment(List<Read.IRead> sequences, Alphabet alphabet, string name, double cutoffScore, int index, bool forceGermlineIsoleucine, RunParameters.ScoringParameter scoring = RunParameters.ScoringParameter.Absolute)
        {
            Name = name;
            Index = index;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = new List<Template>();
            Scoring = scoring;

            for (int i = 0; i < sequences.Count; i++)
            {
                var meta = sequences[i];
                Templates.Add(new Template(name, meta.Sequence.Sequence, meta, this, forceGermlineIsoleucine, new TemplateLocation(index, i)));
            }

            try
            {
                Hierarchy = PhylogeneticTree.CreateTree(Templates.Select(a => (a.MetaData.Identifier, a.MetaData)).ToList(), Alphabet);
            }
            catch (Exception e)
            {
                (new InputNameSpace.ErrorMessage(name, "Error rendering tree", "The tree will not be available but the program will continue. Please report this including your batchfile and used templates.", "", true)).Print();
                InputNameSpace.ErrorMessage.PrintException(e);
            }
        }
        /// <summary> Create a new Segment based on the templates provided. </summary>
        /// <param name="templates">The templates</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="name">The name for this segment</param>
        /// <param name="cutoffScore">The cutoffscore for a path to be aligned to a template</param>
        public Segment(ICollection<Template> templates, Alphabet alphabet, string name, double cutoffScore)
        {
            Name = name;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = templates.ToList();
        }

        /// <summary> Gets the sequence in AminoAcids from a string </summary>
        /// <param name="input">The input string</param>
        /// <returns>The sequence in AminoAcids</returns>
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(Alphabet, input[i]);
            }
            return output;
        }
        /// <summary> Match the given sequences to the segment. Saves the results in this instance of the segment. </summary>
        /// <param name="sequences">The sequences to match with</param>
        public List<List<(int TemplateIndex, SequenceMatch Match)>> Match(List<Read.IRead> sequences)
        {
            var output = new List<List<(int TemplateIndex, SequenceMatch Match)>>(sequences.Count);
            for (int j = 0; j < sequences.Count; j++)
            {
                var row = new List<(int TemplateIndex, SequenceMatch Match)>(Templates.Count);
                for (int i = 0; i < Templates.Count; i++)
                {
                    row.Add((i, HelperFunctionality.SmithWaterman(Templates[i].MetaData, sequences[j], Alphabet, j, i)));
                }
                output.Add(row);
            }
            return output;
        }

        /// <summary> Create a string summary of a template segment. </summary>
        public override string ToString()
        {
            return $"Segment {Name} with {Templates.Count} templates in total";
        }
    }
}