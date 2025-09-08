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
        [JsonPropertyName("kind")]
        // Groups with the same kind can share eachother skins when filling empty spots
        public string? Kind { get; set; } = null;

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

        public void FillAndOrderSkins(List<RandomSkinGroup> relatedGroups)
        {
            var relatedSkins = relatedGroups
                .Select(x => x.RandomSkins)
                .SelectMany(x => x)
                .Where(x => x.Composition == this.Composition && x.Id == this.Id)
                .SelectMany(x => x.Skins)
                .OrderBy(x => {
                    var alreadyHas = this.Skins.Any(s => s.Texture == x.Texture);
                    return alreadyHas ? 1 : 0; // prioritize skins that are not already in the list
                })
                .ToList();

            var skins = Skins.OrderByDescending(x => x.Rarity).ToList();

            var duplicates = skins.GroupBy(x => x.Texture)
                .Where(g => !String.IsNullOrEmpty(g.First().Texture) && g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                Console.WriteLine("Found a duplicate in " + Id + ": " + duplicate.Key);
                throw new InvalidDataException("Duplicate found in " + Id + ": " + duplicate.Key);
            }

            while (skins.Count < FullSkinsAmount)
            {
                Console.WriteLine(this.Id + " Composition is not fully filled. The remaining space will be filled with duplicates.");
                if (relatedSkins.Count == 0 && skins.Count == 0)
                {
                    throw new InvalidDataException("No skins available to fill the composition");
                }
                if (relatedSkins.Count > 0)
                {
                    Console.WriteLine("Filling from related skins: " + relatedSkins.Count + " available");
                }
                var dups = skins.ToArray();
                skins.AddRange(relatedSkins);
                skins.AddRange(dups);
                if (skins.Count > FullSkinsAmount)
                {
                    skins.RemoveRange(FullSkinsAmount, skins.Count - FullSkinsAmount);
                }
            }

            if (skins.Count > FullSkinsAmount)
            {
                Console.WriteLine("More skins: " + skins.Count + " found than the maximum allowed: " + FullSkinsAmount);
                throw new InvalidDataException("More skins found than the maximum allowed");
            }

            skins = skins.OrderByDescending(x => x.Rarity).ToList();
            this.Skins = skins;
        }
    }
}
