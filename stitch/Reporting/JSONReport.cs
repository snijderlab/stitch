using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;


namespace Stitch {
    /// <summary> A JSON report. </summary> 
    public class JSONReport : Report {

        /// <summary> To retrieve all metadata. </summary>
        public JSONReport(ReportInputParameters parameters, int maxThreads) : base(parameters, maxThreads) {
        }

        /// <summary> Creates a JSON file with a score for each path through the graph. The lines will be sorted and the lines can be filtered for a minimal score. </summary>
        /// <returns>A string containing the file.</returns>
        public override string Create() {
            var options = new JsonSerializerOptions {
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                Converters = {
                    new AminoAcidArrayConverter(),
                    new AminoAcidListConverter()
                }
            };
#pragma warning disable IL2026
            return JsonSerializer.Serialize(Parameters, options);
        }
    }

    class AminoAcidArrayConverter :
        JsonConverter<AminoAcid[]> {
        public override AminoAcid[] Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            AminoAcid[] sequence,
            JsonSerializerOptions options) {
            writer.WriteStartObject();
            JsonSerializer.Serialize(writer, AminoAcid.ArrayToString(sequence), options);
            writer.WriteEndObject();
        }
    }

    class AminoAcidListConverter :
        JsonConverter<List<AminoAcid>> {
        public override List<AminoAcid> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            List<AminoAcid> sequence,
            JsonSerializerOptions options) {
            writer.WriteStartObject();
            JsonSerializer.Serialize(writer, AminoAcid.ArrayToString(sequence), options);
            writer.WriteEndObject();
        }
    }
}