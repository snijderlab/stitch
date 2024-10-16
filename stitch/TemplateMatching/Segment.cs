using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Stitch {
    public class Segment {
        public readonly int Index;
        public readonly string Name;
        public readonly ScoringMatrix Alphabet;
        public List<Template> Templates;
        public readonly double CutoffScore;
        [JsonIgnore]
        public PhylogeneticTree.Tree<string> Hierarchy = null;
        public List<(int Group, int Index, Alignment EndAlignment, ReadFormat.General SeqA, ReadFormat.General SeqB, ReadFormat.General Result, int Overlap)> SegmentJoiningScores = new();
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
        public Segment(List<ReadFormat.General> sequences, ScoringMatrix alphabet, string name, double cutoffScore, int index, bool forceGermlineIsoleucine, RunParameters.ScoringParameter scoring = RunParameters.ScoringParameter.Absolute) {
            Name = name;
            Index = index;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = new List<Template>();
            Scoring = scoring;

            for (int i = 0; i < sequences.Count; i++) {
                var meta = sequences[i];
                Templates.Add(new Template(name, meta.Sequence.AminoAcids, meta, this, forceGermlineIsoleucine, new TemplateLocation(index, i)));
            }
        }

        /// <summary> Create a new Segment based on the templates provided. </summary>
        /// <param name="templates">The templates</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <param name="name">The name for this segment</param>
        /// <param name="cutoffScore">The cutoffscore for a path to be aligned to a template</param>
        public Segment(ICollection<Template> templates, ScoringMatrix alphabet, string name, double cutoffScore) {
            Name = name;
            CutoffScore = cutoffScore;
            Alphabet = alphabet;
            Templates = templates.ToList();
        }

        /// <summary> Match the given sequences to the segment. Saves the results in this instance of the segment. </summary>
        /// <param name="sequences">The sequences to match with</param>
        public List<List<(int TemplateIndex, Alignment Match)>> Match(List<ReadFormat.General> sequences) {
            var output = new List<List<(int TemplateIndex, Alignment Match)>>(sequences.Count);
            for (int j = 0; j < sequences.Count; j++) {
                var row = new List<(int TemplateIndex, Alignment Match)>(Templates.Count);
                for (int i = 0; i < Templates.Count; i++) {
                    row.Add((i, new Alignment(Templates[i].MetaData, sequences[j], Alphabet, AlignmentType.ReadAlign, i)));
                }
                output.Add(row);
            }
            return output;
        }

        /// <summary> Create a string summary of a template segment. </summary>
        public override string ToString() {
            return $"Segment {Name} with {Templates.Count} templates in total";
        }
    }
}