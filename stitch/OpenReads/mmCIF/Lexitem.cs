using System;
using System.Collections.Generic;
using System.Text;

namespace Stitch {
    namespace MMCIFNameSpace {
        public struct DataBlock {
            public string Name;
            public List<Item> Items;
        }

        public interface Item { }

        public struct SaveFrame : Item {
            public string Name;
            public List<DataItem> Items;

            public SaveFrame(string name, List<DataItem> items) {
                Name = name;
                Items = items;
            }
        }

        public interface DataItem : Item {

        }

        public struct Single : DataItem {
            public string Name;
            public Value Content;

            public Single(string name, Value value) {
                Name = name;
                Content = value;
            }
        }

        public struct Loop : DataItem {
            public List<string> Header;
            public List<List<Value>> Data;

            public Loop() {
                Header = new List<string>();
                Data = new List<List<Value>>();
            }
        }

        public interface Value {
            string AsText();
        }

        public struct Inapplicable : Value {
            public string AsText() { return "."; }
        }
        public struct Unknown : Value {
            public string AsText() { return "?"; }
        }
        public struct Numeric : Value {
            public double Value;
            public Numeric(double value) {
                Value = value;
            }
            public string AsText() { return Value.ToString(); }
        }

        public struct NumericWithUncertainty : Value {
            public double Value;
            public uint Uncertainty;

            public NumericWithUncertainty(double value, uint uncertainty) {
                Value = value;
                Uncertainty = uncertainty;
            }
            public string AsText() { return $"{Value}({Uncertainty})"; }
        }
        public struct Text : Value {
            public string Value;

            public Text(string value) {
                Value = value;
            }
            public string AsText() { return Value; }
        }

    }
}