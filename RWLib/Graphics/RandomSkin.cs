using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Graphics
{
    public class RandomSkinGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("randomSkins")]
        public List<RandomSkin> RandomSkins { get; set; } = new List<RandomSkin>();

        public static List<RandomSkinGroup> FromJson(string jsonString)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<List<RandomSkinGroup>>(jsonString, options)!;
        }
    }

    public class RandomSkin
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("composition")]
        public string Composition { get; set; } = "";

        [JsonPropertyName("skins")]
        public List<SkinTexture> Skins { get; set; } = new List<SkinTexture>();

        [JsonPropertyName("fullSkinsAmount")]
        public int FullSkinsAmount { get; set; } = 0;
        [JsonPropertyName("stacked")]
        public int Stacked { get; set; } = 1;

        public class SkinTexture
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = ""; // Derived from the file name
            [JsonPropertyName("name")]
            public string Name { get; set; } = ""; // From Chat GPT or manually
            [JsonPropertyName("rarity")]
            public int Rarity { get; set; } = 1; // A value from 0 to 100
            [JsonPropertyName("group")]
            public string Group { get; set; } = "";
            [JsonPropertyName("texture")]
            public string Texture { get; set; } = "";
        }
    }
}
