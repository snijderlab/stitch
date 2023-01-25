using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stitch.RunParameters;

namespace Stitch {
    /// <summary> To contain all input functionality </summary>
    namespace InputNameSpace {
        struct LocalParams<T> where T : new() {
            string Name;
            List<(string Name, Action<T, KeyValue> Action)> Options;
            T Aggregator;

            public LocalParams(string name, List<(string, Action<T, KeyValue>)> options) {
                Name = name;
                Options = options;
                Aggregator = new();
            }

            public ParseResult<T> Parse(List<KeyValue> input) {
                var outEither = new ParseResult<T>();
                foreach (var value in input) {
                    bool found = false;
                    for (var i = 0; i < Options.Count && !found; i++) {
                        var option = Options[i];
                        if (value.Name == option.Name.ToLower()) {
                            option.Action(Aggregator, value);
                            found = true;
                        }
                    }
                    if (!found) {
                        outEither.AddMessage(ErrorMessage.UnknownKey(value.KeyRange.Name, Name, Options.Aggregate("", (acc, o) => $"{acc}, \"{o.Name}\"").Substring(2)));
                    }
                }
                outEither.Value = Aggregator;
                return outEither;
            }
        }
    }
}