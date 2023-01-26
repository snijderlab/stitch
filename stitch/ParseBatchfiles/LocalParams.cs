using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stitch.RunParameters;
using HtmlGenerator;

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

            public ParseResult<T> Parse(KeyValue key, Action<T> post_processing) {
                var value = key.GetValues();
                if (value.IsOk()) {
                    var res = Parse(value.Value);
                    if (res.IsOk()) {
                        post_processing(res.Value);
                    }
                    return res;
                } else {
                    return new ParseResult<T>(value.Messages);
                }
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
                        var best_match = Options.Select(o => (o.Name, HelperFunctionality.SmithWatermanStrings(o.Name.ToLower(), value.Name))).OrderByDescending(s => s.Item2).First().Name;
                        outEither.AddMessage(ErrorMessage.UnknownKey(value.KeyRange.Name, Name, Options.Aggregate("", (acc, o) => $"{acc}, \"{o.Name}\"").Substring(2), best_match));
                    }
                }
                outEither.Value = Aggregator;
                return outEither;
            }

            public HtmlBuilder BuildDocs(int level, string id) {
                var html = new HtmlBuilder();
                html.OpenAndClose(HtmlBuilder.H(level), $"id='{id}{Name}'", Name);
                foreach (var opt in Options) {
                    html.OpenAndClose(HtmlBuilder.H(level + 1), $"id='{id}{Name}{opt.Name}'", opt.Name);
                }
                return html;
            }
        }
    }
}