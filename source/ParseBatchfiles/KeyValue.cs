using System.Collections.Generic;

namespace AssemblyNameSpace
{
    namespace InputNameSpace
    {
        /// <summary>
        /// A class to save key value trees.
        /// </summary>
        public class KeyValue
        {
            /// <summary>
            /// The name of a key.
            /// </summary>
            public string Name;

            /// <summary>
            /// The value for this key.
            /// </summary>
            ValueType Value;
            public readonly KeyRange KeyRange;
            public readonly Range ValueRange;

            /// <summary>
            /// Create a new single valued key.
            /// </summary>
            /// <param name="name">The name of the key.</param>
            /// <param name="value">The value of the key.</param>
            public KeyValue(string name, string value, KeyRange key_r, Range value_r)
            {
                Name = name.ToLower();
                Value = new Single(value);
                KeyRange = key_r;
                ValueRange = value_r;
            }

            /// <summary>
            /// Create a new multiple valued key.
            /// </summary>
            /// <param name="name">The name of the key.</param>
            /// <param name="values">The list of KeyValue tree(s) that are the value of this key.</param>
            public KeyValue(string name, List<KeyValue> values, KeyRange key_r, Range value_r)
            {
                Name = name.ToLower();
                Value = new KeyValue.Multiple(values);
                KeyRange = key_r;
                ValueRange = value_r;
            }

            /// <summary>
            /// Tries to get a single value from this key, otherwise fails with an error message for the end user.
            /// </summary>
            /// <returns>The value of the KeyValue.</returns>
            public string GetValue()
            {
                if (Value is Single)
                {
                    return ((Single)Value).Value;
                }
                else
                {
                    throw new ParseException($"Parameter {Name} {KeyRange} has multiple values but should have a single value.");
                }
            }

            /// <summary>
            /// Tries to get tha values from this key, only succeeds if this KeyValue is multiple valued, otherwise fails with an error message for the end user.
            /// </summary>
            /// <returns>The values of this KeyValue.</returns>
            public List<KeyValue> GetValues()
            {
                if (Value is Multiple)
                {
                    return ((Multiple)Value).Values;
                }
                else
                {
                    throw new ParseException($"Parameter {Name} {KeyRange} has a single value but should have multiple values. Value {GetValue()}");
                }
            }

            /// <summary>
            /// To test if this is a single valued KeyValue.
            /// </summary>
            /// <returns>A bool indicating that.</returns>
            public bool IsSingle()
            {
                return Value is Single;
            }

            /// <summary>
            /// An abstract class to represent possible values for a KeyValue.
            /// </summary>
            abstract class ValueType { }

            /// <summary>
            /// A ValueType for a single valued KeyValue.
            /// </summary>
            class Single : ValueType
            {
                /// <summary>
                /// The value.
                /// </summary>
                public string Value;

                /// <summary>
                /// To create a single value.
                /// </summary>
                /// <param name="value">The value.</param>
                public Single(string value)
                {
                    Value = value.Trim();
                }
            }

            /// <summary>
            /// A ValueType to contain multiple values.
            /// </summary>
            class Multiple : ValueType
            {
                /// <summary>
                /// The list of values.
                /// </summary>
                public List<KeyValue> Values;

                /// <summary>
                /// To create a multiple value.
                /// </summary>
                /// <param name="values">The values.</param>
                public Multiple(List<KeyValue> values)
                {
                    Values = values;
                }
            }
        }
    }
}