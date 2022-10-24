using System;
using System.Collections.Generic;
using System.Linq;
using HeckLib.chemistry;
using Stitch;


namespace Stitch
{
    public static class MassSpecErrors
    {
        public const int MaxLength = 2;
        public static Dictionary<AminoAcidSet, HashSet<AminoAcidSet>> EqualMasses(Alphabet alphabet)
        {
            var output = new Dictionary<AminoAcidSet, HashSet<AminoAcidSet>>();

            for (int size = 1; size <= MaxLength; size++)
            {
                // Create all AA combinations within MaxLength options
                // https://rosettacode.org/wiki/Cartesian_product_of_two_or_more_lists#C.23
                IEnumerable<IEnumerable<(AminoAcid, double)>> empty = new[] { Enumerable.Empty<(AminoAcid, double)>() };
                double LookupAA(char a)
                {
                    if (!HeckLib.chemistry.AminoAcid.Exists(a)) return -1;
                    return Math.Round(HeckLib.chemistry.AminoAcid.Get(a).MonoIsotopicWeight, 4);
                }

                var all_amino_acids = alphabet.PositionInScoringMatrix.Keys.Select(a => (new AminoAcid(alphabet, a), LookupAA(a))).Where(a => a.Item2 != -1);

                var combinations = Enumerable.Repeat(all_amino_acids, size).Aggregate(
                    empty,
                    (accumulator, sequence) =>
                    from acc in accumulator
                    from item in sequence
                    select acc.Concat(new[] { item })).Select(c =>
                {
                    return (c.Select(a => a.Item1).Sort(), c.Select(a => a.Item2).Sum());
                }
                    ).GroupBy(a => a.Item2);

                foreach (var group in combinations)
                {
                    var unique = group.Unique((a, b) => AminoAcid.ArrayEquals(a.Item1, b.Item1));

                    if (unique.Count() > 1)
                        foreach (var element in unique)
                        {
                            var key = new AminoAcidSet(element.Item1);
                            var set = unique.Where(e => !AminoAcid.ArrayEquals(e.Item1, element.Item1)).Select(e => new AminoAcidSet(e.Item1)).ToHashSet();
                            var set_str = string.Join(", ", set.Select(e => e.Value));
                            Console.WriteLine($"{AminoAcid.ArrayToString(element.Item1)} {key} -> ({set_str})");
                            output.Add(key, set);
                        }
                }
            }

            return output;
        }

        /// <summary> Sort an array and return the new sorted array. </summary>
        /// <param name="data"> The old array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array which is the sorted variant of the input array. </returns>
        public static T[] Sort<T>(this IEnumerable<T> data)
        {
            var temp = data.ToList();
            temp.Sort();
            return temp.ToArray();
        }

        /// <summary> Create a sorted amino acid set from data. </summary>
        /// <param name="data"> The old array. </param>
        /// <returns> Returns an amino acid set which contains the data sorted. </returns>
        public static AminoAcidSet ToSortedAminoAcidSet(this IEnumerable<AminoAcid> data)
        {
            return new AminoAcidSet(data.Sort());
        }
    }

    public struct AminoAcidSet : IComparable, IEquatable<AminoAcidSet>
    {
        public readonly uint Value = 0;
        const int width = 6;

        public AminoAcidSet(AminoAcid[] set)
        {
            if (set.Length > 10) throw new ArgumentException("AminoAcidSets cannot be generated for set with more then 10 elements.");
            for (int i = 0; i < set.Length; i++)
            {
                var element = set[i].Index << (i * width);
                Value = Value | element;
            }
        }

        public int CompareTo(object obj)
        {
            return obj != null && obj is AminoAcidSet set ? this.Value.CompareTo(set.Value) : 0;
        }

        public bool Equals(AminoAcidSet other)
        {
            return this.Value == other.Value;
        }

        public static bool operator ==(AminoAcidSet left, AminoAcidSet right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AminoAcidSet left, AminoAcidSet right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is AminoAcidSet set && this.Equals(set);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override string ToString()
        {
            string output = "";
            for (int i = 0; i < 10; i++)
            {
                var index = (this.Value >> (i * width)) & (width - 1);
                output += ' ';
                output += index.ToString();
            }
            output += ": " + this.Value.ToString();
            return output;
        }
    }
}