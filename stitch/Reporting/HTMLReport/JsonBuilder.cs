using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTMLNameSpace {
    /// <summary> Helper interface to provide the option to build and stringify Json. </summary> 
    public interface IJsonNode {
        public void ToString(StringBuilder buffer) {
            buffer.Append("undefined");
        }
    }

    public struct JsonList : IJsonNode {
        public List<IJsonNode> List = new();
        public JsonList() { }
        public JsonList(IEnumerable<IJsonNode> list) {
            if (list != null) List = list.ToList();
        }

        public void ToString(StringBuilder buffer) {
            buffer.Append("[");
            bool first = true;
            foreach (var item in List) {
                if (!first) buffer.Append(",");
                item.ToString(buffer);
                first = false;
            }
            buffer.Append("]");
        }
    }

    public struct JsonObject : IJsonNode {
        public Dictionary<string, IJsonNode> Keys = new();
        public JsonObject() { }
        public JsonObject(Dictionary<string, IJsonNode> keys) {
            if (keys != null) Keys = keys;
        }

        public void ToString(StringBuilder buffer) {
            buffer.Append("{");
            bool first = true;
            foreach (var item in Keys) {
                if (!first) buffer.Append(",");
                buffer.Append($"\"{item.Key}\":");
                item.Value.ToString(buffer);
                first = false;
            }
            buffer.Append("}");
        }
    }

    public struct JsonString : IJsonNode {
        public string Text;
        public JsonString(string text = "") {
            Text = text;
        }

        public void ToString(StringBuilder buffer) {
            buffer.Append($"\"{Text}\"");
        }
    }

    public struct JsonNumber : IJsonNode {
        public double Number;
        public JsonNumber(double num = 0.0) {
            Number = num;
        }

        public void ToString(StringBuilder buffer) {
            buffer.Append(Number);
        }
    }
}