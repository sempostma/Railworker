using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Packaging
{
    public class FileItem : MatrixTransformable
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("mass")]
        public int Mass { get; set; } = 15000;
        [JsonPropertyName("cargo")]
        public List<FileItem> Cargo { get; set; } = new List<FileItem>();

        [JsonPropertyName("filterWagonType")]
        public List<string> FilterWagonType { get; set; } = new List<string>();
        [JsonPropertyName("cargoAsChild")]
        public bool CargoAsChild { get; set; } = false;

        [JsonPropertyName("childName")]
        public string ChildName { get; set; } = "";

        [JsonPropertyName("autoNumber")]
        public string AutoNumber { get; set; } = "";

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
