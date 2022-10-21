using System;
using System.Collections.Generic;
using System.Linq;
using HeckLib.chemistry;


namespace Stitch
{
    public static class MassSpecErrors
    {
        public const int MaxLength = 1;
        public static Dictionary<AminoAcid[], AminoAcid[][]> EqualMasses(Alphabet alphabet)
        {
            // Create all AA combinations within MaxLength options
            // https://rosettacode.org/wiki/Cartesian_product_of_two_or_more_lists#C.23
            IEnumerable<IEnumerable<(AminoAcid, double)>> empty = new[] { Enumerable.Empty<(AminoAcid, double)>() };
            double LookupAA(char a)
            {
                if (!HeckLib.chemistry.AminoAcid.Exists(a)) return -1;
                return HeckLib.chemistry.AminoAcid.Get(a).MonoIsotopicWeight;
            }

            var all_amino_acids = alphabet.PositionInScoringMatrix.Keys.Select(a => (new AminoAcid(alphabet, a), LookupAA(a)));

            var combinations = Enumerable.Repeat(all_amino_acids, MassSpecErrors.MaxLength).Aggregate(
                empty,
                (accumulator, sequence) =>
                from acc in accumulator
                from item in sequence
                select acc.Concat(new[] { item })).Select(c =>
            {
                var aa = c.Select(a => a.Item1).ToList();
                aa.Sort();
                return (aa.ToArray(), c.Select(a => a.Item2).Sum());
            }
                ).GroupBy(a => a.Item2);

            var output = new Dictionary<AminoAcid[], AminoAcid[][]>();

            foreach (var group in combinations)
                if (group.Count() > 1)
                    foreach (var element in group)
                        output.Add(element.Item1, group.Select(e => e.Item1).Where(e => !AminoAcid.ArrayEquals(e, element.Item1)).ToArray());

            return output;
        }

    }
}