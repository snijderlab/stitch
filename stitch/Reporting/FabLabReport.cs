using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using HTMLNameSpace;
using System.Linq;
using System.Text;

namespace Stitch {
    /// <summary> A JSON report. </summary>
    public class FabLabReport : Report {

        /// <summary> To retrieve all metadata. </summary>
        public FabLabReport(ReportInputParameters parameters, int maxThreads) : base(parameters, maxThreads) {
        }

        /// <summary> Creates a JSON file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score. </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create() {
            var json = new JsonList();

            foreach (var (tm, rec) in Parameters.Groups.Zip(Parameters.RecombinedSegment)) {
                json.List.Add(new JsonObject(
                    new Dictionary<string, IJsonNode>{
                        {"group", new JsonString(tm.Name)},
                        {"template_matching", new JsonList(
                            tm.Segments.Select(segment => (IJsonNode) new JsonObject(new Dictionary<string, IJsonNode>{
                                {"segment", new JsonString(segment.Name)},
                                {"children", new JsonList(segment.Templates.Select(
                                    template => (IJsonNode) new JsonObject(new Dictionary<string, IJsonNode>{
                                    {"template", new JsonString(template.MetaData.Identifier)},
                                    {"coverage", new JsonList(template.CombinedSequence().Select(
                                        position => (IJsonNode) new JsonList(
                                            position.AminoAcids.Select(
                                                aa => (IJsonNode) new JsonList(
                                                    new List<IJsonNode>{
                                                        new JsonString(AminoAcid.ArrayToString(aa.Key.Sequence)),
                                                        new JsonNumber(aa.Value),
                                                    }
                                                )
                                                )
                                        ))
                                    )},
                                })))},
                            }))
                        )},
                        {"recombine", new JsonList(rec.Templates.Select(
                            template =>(IJsonNode) new JsonObject(new Dictionary<string, IJsonNode>{
                                {"template", new JsonString(template.MetaData.Identifier)},
                                {"order", new JsonList(template.Recombination == null ? new List<IJsonNode>() : template.Recombination.Select(t => (IJsonNode) new JsonString(t.Name)))},
                                {"coverage", new JsonList(template.CombinedSequence().Select(
                                    position => (IJsonNode) new JsonList(
                                        position.AminoAcids.Select(
                                            aa => (IJsonNode) new JsonList(
                                                new List<IJsonNode>{
                                                    new JsonString(AminoAcid.ArrayToString(aa.Key.Sequence)),
                                                    new JsonNumber(aa.Value),
                                                }
                                            )
                                        )
                                    ))
                                )},
                            })
                        ))},
                    }
                ));
            }

            var buffer = new StringBuilder();
            json.ToString(buffer);
            return buffer.ToString();
        }
    }
}