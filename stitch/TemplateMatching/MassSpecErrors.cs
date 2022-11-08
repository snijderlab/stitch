using System;
using System.Collections.Generic;
using System.Linq;
using HeckLib.chemistry;
using Stitch;


namespace Stitch {
    public static class MassSpecErrors {
        readonly static string[] IsoMassSets = new string[]{
            "I,L",
            "N,GG",
            "Q,AG",
            "AV,GL,GI",
            "AN,QG,AGG",
            "LS,IS,TV",
            "AM,CV",
            "NV,AAA,GGV",
            "NT,QS,AGS,GGT",
            "LN,IN,QV,AGV,GGL,GGI",
            "DL,DI,EV",
            "QT,AAS,AGT",
            "AY,FS",
            "LQ,IQ,AAV,AGL,AGI",
            "NQ,ANG,QGG,AGGG",
            "KN,GGK",
            "EN,DQ,ADG,EGG",
            "DK,AAT,GSV",
            "MN,AAC,GGM",
            "AS,GT",
            "AAL,AAI,GVV",
            "QQ,AAN,AQG",
            "EQ,AAD,AEG",
            "EK,ASV,GLS,GIS,GTV",
            "MQ,AGM,CGV",
            "AAQ,NGV,AAAG,GGGV"};
        readonly static (MSErrorType Type, string Prior, string Posterior)[] Modifications = new (MSErrorType, string, string)[] {
            (MSErrorType.Deamidation, "N", "D"),
            (MSErrorType.Deamidation, "Q", "E"),
        };

        public const int MaxLength = 4;
        public static Dictionary<AminoAcidSet, HashSet<(MSErrorType Type, HashSet<AminoAcidSet> Set)>> EqualMasses(Alphabet alphabet) {
            var output = new Dictionary<AminoAcidSet, HashSet<(MSErrorType Type, HashSet<AminoAcidSet> Set)>>();

            void Add(AminoAcidSet key, MSErrorType type, HashSet<AminoAcidSet> set) {
                if (output.ContainsKey(key)) {
                    output[key].Add((type, set));
                } else {
                    output.Add(key, new HashSet<(MSErrorType, HashSet<AminoAcidSet>)> { (type, set) });
                }
            }
            void AddSingle(AminoAcidSet key, MSErrorType type, AminoAcidSet set) {
                Add(key, type, new HashSet<AminoAcidSet> { set });
            }

            var combinations = IsoMassSets.Select(l => l.Split(',').Select(s => new AminoAcidSet(AminoAcid.FromString(s, alphabet).Unwrap())));

            foreach (var group in combinations) {
                foreach (var element in group) {
                    Add(element, MSErrorType.Isomass, group.Where(e => e != element).ToHashSet());
                }
            }

            foreach (var rule in Modifications) {
                AddSingle(new AminoAcidSet(AminoAcid.FromString(rule.Prior, alphabet).Unwrap()), rule.Type, new AminoAcidSet(AminoAcid.FromString(rule.Posterior, alphabet).Unwrap()));
            }

            return output;
        }

        /// <summary> Sort an array and return the new sorted array. </summary>
        /// <param name="data"> The old array. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array which is the sorted variant of the input array. </returns>
        public static T[] Sort<T>(this IEnumerable<T> data) {
            var temp = data.ToList();
            temp.Sort();
            return temp.ToArray();
        }

        /// <summary> Create a sorted amino acid set from data. </summary>
        /// <param name="data"> The old array. </param>
        /// <returns> Returns an amino acid set which contains the data sorted. </returns>
        public static AminoAcidSet ToSortedAminoAcidSet(this IEnumerable<AminoAcid> data) {
            return new AminoAcidSet(data.Sort());
        }

        public static string Description(this MSErrorType error) {
            return new string[]{
                "Isomass, these sets of amino acids have the same mass but the new sequence is the same as the germline and so more probable.",
                "Amidation, a common post translational modification where the C terminal carboxyl is replaced by an amide group. (-0.98402Δ)",
                "Deamidation, a common post translational modification where an amide group is converted to a carboxyl. (+0.98402Δ)"
            }[(int)error];
        }
    }

    public enum MSErrorType {
        Isomass,
        Amidation,
        Deamidation
    }



    public struct AminoAcidSet : IComparable, IEquatable<AminoAcidSet> {
        public readonly uint Value = 0;
        const int width = 6;

        public AminoAcidSet(AminoAcid[] set) {
            if (set.Length > 10) throw new ArgumentException("AminoAcidSets cannot be generated for set with more than 10 elements.");
            for (int i = 0; i < set.Length; i++) {
                uint index = set[i].Alphabet.GetIndexInAlphabet(set[i].Character) < 0 ? 0 : (uint)set[i].Alphabet.GetIndexInAlphabet(set[i].Character) + 1u;
                uint element = index << (i * width);
                Value = Value | element;
            }
        }

        public int CompareTo(object obj) {
            return obj != null && obj is AminoAcidSet set ? this.Value.CompareTo(set.Value) : 0;
        }

        public bool Equals(AminoAcidSet other) {
            return this.Value == other.Value;
        }

        public static bool operator ==(AminoAcidSet left, AminoAcidSet right) {
            return left.Equals(right);
        }

        public static bool operator !=(AminoAcidSet left, AminoAcidSet right) {
            return !left.Equals(right);
        }

        public override bool Equals(object obj) {
            return obj != null && obj is AminoAcidSet set && this.Equals(set);
        }

        public override int GetHashCode() {
            return this.Value.GetHashCode();
        }

        public override string ToString() {
            string output = "";
            for (int i = 0; i < 10; i++) {
                var index = (this.Value >> (i * width)) & ((1 << width) - 1);
                output += ' ';
                output += index.ToString();
            }
            output += ": " + this.Value.ToString();
            return output;
        }
    }
}