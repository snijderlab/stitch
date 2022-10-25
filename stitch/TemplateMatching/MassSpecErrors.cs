using System;
using System.Collections.Generic;
using System.Linq;
using HeckLib.chemistry;
using Stitch;


namespace Stitch
{
    public static class MassSpecErrors
    {
        readonly static string[] Preset = new string[]{
            "I,L",
            "N,GG",
            "Q,AG",
            "AV,GL,GI",
            "AN,QG,AGG",
            "LS,TV",
            "AM,CV",
            "NV,AAA,GGV",
            "NT,QS,AGS,GGT",
            "NC,CGG",
            "NL,NI,QV,AGV,GGL,GGI",
            "DL,DI,EV",
            "QT,AAS,AGT",
            "AY,FS",
            "QL,QI,AAV,AGL,AGI",
            "NQ,ANG,QGG,AGGG",
            "NK,GGK",
            "NE,DQ,ADG,EGG",
            "DK,AAT,GSV",
            "NM,AAC,GGM",
            "ASS,GST",
            "AS,GT",
            "AAL,AAI,GVV",
            "QQ,AAN,AQG",
            "EQ,AAD,AEG",
            "EK,ASV,GLS,GIS,GTV",
            "QM,AGM,CGV",
            "AAQ,NGV,AAAG,GGGV"};

        public const int MaxLength = 4;
        public static Dictionary<AminoAcidSet, HashSet<AminoAcidSet>> EqualMasses(Alphabet alphabet)
        {
            var output = new Dictionary<AminoAcidSet, HashSet<AminoAcidSet>>();

            var combinations = Preset.Select(l => l.Split(',').Select(s => new AminoAcidSet(AminoAcid.FromString(s, alphabet).Unwrap())));

            foreach (var group in combinations)
            {
                foreach (var element in group)
                {
                    output.Add(element, group.Where(e => e != element).ToHashSet());
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
                uint element = (set[i].Index + 1u) << (i * width);
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
                var index = (this.Value >> (i * width)) & ((1 << width) - 1);
                output += ' ';
                output += index.ToString();
            }
            output += ": " + this.Value.ToString();
            return output;
        }
    }
}