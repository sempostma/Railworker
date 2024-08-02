using RWLib.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Graphics
{
    public class Composition
    {
        public class Bbox
        {
            [JsonPropertyName("x")]
            public int X { get; set; } = 0;
            [JsonPropertyName("y")]
            public int Y { get; set; } = 0;
            [JsonPropertyName("width")]
            public int Width { get; set; } = 0;
            [JsonPropertyName("height")]
            public int Height { get; set; } = 0;
            [JsonPropertyName("rotate")]
            public string Rotate { get; set; } = "None"; // Rotate90, Rotate180, Rotate270
        }

        public class Projection
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
            [JsonPropertyName("sourceBbox")]
            public Bbox SourceBbox { get; set; } = new Bbox();
            [JsonPropertyName("destBbox")]
            public Bbox DestBbox { get; set; } = new Bbox();
        }

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("projections")]
        public List<Projection> Projections { get; set; } = new List<Projection>();

        public static List<Composition> FromJson(string jsonString)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<List<Composition>>(jsonString, options)!;
        }
    }
}
