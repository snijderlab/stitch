using System.Collections.Generic;
using System.Linq;

// Lex/tokenize a CIF file into its constituent parts.

namespace Stitch {
    namespace MMCIFItems {
        public struct DataBlock {
            public string Name;
            public List<Item> Items;

            public DataBlock() {
                Name = "";
                Items = new List<Item>();
            }

            public string Debug() {
                return $"DataBlock({Name}, [{string.Join(", ", Items.Select(i => i.Debug()))}])";
            }
        }

        public interface Item {
            string Debug();
        }

        public struct SaveFrame : Item {
            public string Name;
            public List<DataItem> Items;

            public SaveFrame(string name, List<DataItem> items) {
                Name = name;
                Items = items;
            }

            public string Debug() {
                return $"Item::SaveFrame({Name}, [{string.Join(", ", Items.Select(i => i.Debug()))}])";
            }
        }

        public interface DataItem : Item {

        }

        public struct SingleItem : DataItem {
            public string Name;
            public Value Content;

            public SingleItem(string name, Value value) {
                Name = name;
                Content = value;
            }

            public string Debug() {
                return $"DataItem::Single({Name}, {Content.Debug()})";
            }
        }

        public struct Loop : DataItem {
            public List<string> Header;
            public List<List<Value>> Data;

            public Loop() {
                Header = new List<string>();
                Data = new List<List<Value>>();
            }

            public string Debug() {
                return $"DataItem::Loop([{string.Join(", ", Header)}], [{string.Join(", ", Data.Select(d => "[" + string.Join(", ", d.Select(i => i.Debug())) + "]"))}])";
            }
        }

        public interface Value {
            string AsText();
            string Debug();
        }

        public struct Inapplicable : Value {
            public string AsText() { return "."; }
            public string Debug() { return "Value::Inapplicable"; }
        }
        public struct Unknown : Value {
            public string AsText() { return "?"; }
            public string Debug() { return "Value::Unknown"; }
        }
        public struct Numeric : Value {
            public double Value;
            public Numeric(double value) {
                Value = value;
            }
            public string AsText() { return Value.ToString(); }
            public string Debug() { return $"Value::Numeric({Value.ToString()})"; }
        }

        public struct NumericWithUncertainty : Value {
            public double Value;
            public uint Uncertainty;

            public NumericWithUncertainty(double value, uint uncertainty) {
                Value = value;
                Uncertainty = uncertainty;
            }
            public string AsText() { return $"{Value}({Uncertainty})"; }
            public string Debug() { return $"Value::NumericWithUncertainty({Value}, {Uncertainty})"; }
        }
        public struct Text : Value {
            public string Value;

            public Text(string value) {
                Value = value;
            }
            public string AsText() { return Value; }
            public string Debug() { return $"Value::Text({Value})"; }
        }

    }
}