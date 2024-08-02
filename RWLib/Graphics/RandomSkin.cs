using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Graphics
{
    public class RandomSkin
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("skins")]
        public List<SkinTexture> Skins { get; set; } = new List<SkinTexture>();

        public static List<RandomSkin> FromJson(string jsonString)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<List<RandomSkin>>(jsonString, options)!;
        }

        public class SkinTexture
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = ""; // Derived from the file name
            [JsonPropertyName("name")]
            public string Name { get; set; } = ""; // From Chat GPT or manually
            [JsonPropertyName("rarity")]
            public int Rarity { get; set; } = 0; // A value from 0 to 100
            [JsonPropertyName("group")]
            public string Group { get; set; } = "";
            [JsonPropertyName("texture")]
            public string Texture { get; set; } = "";
        }
    }
}
