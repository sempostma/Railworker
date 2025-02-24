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
        [JsonPropertyName("basePath")]
        public string BasePath { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("projections")]
        public List<Projection> Projections { get; set; } = new List<Projection>();
        [JsonPropertyName("fullSkinsAmount")]
        public int FullSkinsAmount { get; set; } = 0;
        [JsonPropertyName("composedImageWidth")]
        public int ComposedImageWidth { get; set; } = 2048;
        [JsonPropertyName("composedImageHeight")]
        public int ComposedImageHeight { get; set; } = 2048;
        [JsonPropertyName("inputImageResizeWidth")]
        public int InputImageResizeWidth { get; set; } = 512;
        [JsonPropertyName("inputImageResizeHeight")]
        public int InputImageResizeHeight { get; set; } = 512;

        // This is the x offset used to shift the area that is being written to once all the projections are finished and a new skin is processed
        [JsonPropertyName("stylusXInterval")]
        public int StylusXInterval { get; set; } = 512;
        [JsonPropertyName("composedImageColumns")]
        public int ComposedImageColumns { get; set; } = 4;
        [JsonPropertyName("composedImageRows")]
        public int ComposedImageRows { get; set; } = 4;

        // This is the y offset used to shift the area that is being written to once all the projections are finished and a new skin is processed
        [JsonPropertyName("stylusYInterval")]
        public int StylusYInterval { get; set; } = 227;

        [JsonPropertyName("waifu2xScaleRatio")]
        public string Waifu2xScaleRatio { get; set; } = "0.5";
        [JsonPropertyName("waifu2xNoiseLevel")]
        public string Waifu2xNoiseLevel { get; set; } = "2";
        [JsonPropertyName("waifu2xEnabled")]
        public bool Waifu2xEnabled = false;
        [JsonPropertyName("outputScaleX")]
        public float OutputScaleX { get; set; } = 1.0f;
        [JsonPropertyName("outputScaleY")]
        public float OutputScaleY { get; set; } = 1.0f;

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
