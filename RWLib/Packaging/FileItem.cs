using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Packaging
{
    public class FileItem
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("moveX")]
        public float MoveX { get; set; } = 0;
        [JsonPropertyName("moveY")]
        public float MoveY { get; set; } = 0;
        [JsonPropertyName("mozeZ")]
        public float MoveZ { get; set; } = 0;
        [JsonPropertyName("scaleX")]
        public float ScaleX { get; set; } = 0;
        [JsonPropertyName("scaleY")]
        public float ScaleY { get; set; } = 0;
        [JsonPropertyName("scaleZ")]
        public float ScaleZ { get; set; } = 0;
        [JsonPropertyName("mass")]
        public int Mass { get; set; } = 15000;
        [JsonPropertyName("cargo")]
        public List<FileItem> Cargo { get; set; } = new List<FileItem>();

        [JsonPropertyName("filterWagonType")]
        public List<string> FilterWagonType { get; set; } = new List<string>();

        public static List<FileItem> FromJson(string jsonString)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<List<FileItem>>(jsonString, options)!;
        }
    }

}
