using RWLib;
using RWLib.Graphics;
using RWLib.Interfaces;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RailworkerMegaFreightPack1
{
    public static class Scripts
    {
        public class RndSkinConf
        {
            public required int MaxFilesPerSkin { get; set; }
            public required String CompositionId { get; set; }
            public required string RandomSkinId { get; set; }
            public required string Name { get; set; }
            public required string Directory { get; set; }
        }

        static RWLibrary Library = new RWLibrary(new RWLibOptions { Logger = Utilities.ConsoleLogger });

        static string ProductDirectory = Path.Combine(Library.TSPath, "Assets\\Alex95\\ContainerPack01");

        static string ContainerDirectories = Path.Combine(ProductDirectory, "RailNetwork\\Interactive");

        static RndSkinConf Container45 = new RndSkinConf { 
            Name = "45ft_HC",
            MaxFilesPerSkin = 36,
            CompositionId = "45ft_hc_1",
            RandomSkinId = "45ft_hc_{0}",
            Directory = Path.Combine(ContainerDirectories, "45_HC")
        };

        static RndSkinConf Container30WAB = new RndSkinConf
        {
            Name = "30_WAB",
            MaxFilesPerSkin = 36,
            CompositionId = "30_wab_{0}",
            RandomSkinId = "30_wab_{0}",
            Directory = Path.Combine(ContainerDirectories, "30_WAB")
        };

        public static RandomSkin ProcessBatch(RndSkinConf conf, int count, string label, List<(String, List<String>)> values)
        {
            string prefix = String.IsNullOrEmpty(label) ? "" : label + "_";

            return new RandomSkin()
            {
                Composition = String.Format(conf.CompositionId, label),
                Id = String.Format(conf.RandomSkinId, count),
                Name = String.Join(", ", values.Select(x => x.Item1)),
                Skins = values.SelectMany((x, i) =>
                {
                    return x.Item2.Select((y, j) =>
                    {
                        return new RandomSkin.SkinTexture
                        {
                            Group = prefix + x.Item1,
                            Id = prefix + x.Item1 + " " + (j + 1).ToString(),
                            Name = prefix + x.Item1 + " " + (j + 1).ToString(),
                            Rarity = 100,
                            Texture = Path.GetRelativePath(ProductDirectory, y)
                        };
                    });
                }).ToList()
            };
        }

        public static async Task CreateRandomSkins()
        {
            Process45TgpcdxFiles();
            Process30WabTgpcdxFiles();
        }

        public static void Process45TgpcdxFiles()
        {
            // Get all the directories with the format GW_45FT_<ILU Code>
            var directories = Directory.GetDirectories(Container45.Directory, "GW_45FT_*").OrderBy(d => d).ToList();

            var skins = new List<RandomSkin>();
            var currentSkin = new RandomSkin();
            var queue = new List<(String, List<String>)>();
            int count = 1;

            foreach (var directory in directories)
            {
                var iluCode = Path.GetFileName(directory).Replace("GW_45FT_", "");

                // Get all .tgpcdx files recursively within the directory
                var tgpcdxFiles = Directory.GetFiles(Path.Combine(Container45.Directory, directory), "*.tgpcdx", SearchOption.AllDirectories).ToList();
                var queueCount = queue.Sum(x => x.Item2.Count);

                if (queueCount + tgpcdxFiles.Count > Container45.MaxFilesPerSkin) {
                    skins.Add(ProcessBatch(Container45, count++, "", queue));
                    Console.WriteLine($"Generated {Container45.Name}: {skins.Last().Name}, count: {skins.Last().Skins.Count}");
                    queue.Clear();
                }

                queue.Add((iluCode, tgpcdxFiles));
            }

            skins.Add(ProcessBatch(Container45, count++, "", queue));
            queue.Clear();

            Console.WriteLine($"Generated last {Container45.Name}: {skins.Last().Name}, count: {skins.Last().Skins.Count}");

            File.WriteAllText($"RandomSkins{Container45.Name}.json", JsonSerializer.Serialize(skins, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static async Task ConvertToPNGsForPreview()
        {
            var basePath = Path.Combine(Library.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive\\45_HC");

            var cancel = new CancellationTokenSource();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancel.Token
            };

            //var container45NameFromTexture = new Regex(@"GW_45FT_(.+?)[\\/]Childs[\\/]textures[\\/]45_([^\.]+)");

            var allContainers = Directory.EnumerateFiles(basePath, "*.TgPcDx", SearchOption.AllDirectories);

            await Parallel.ForEachAsync(allContainers, parallelOptions, async (path, cToken) =>
            {
                var inputFile = Path.Combine(basePath, path);

                Console.WriteLine("Converting: " + inputFile);

                var image = await Library.TgPcDxLoader.LoadTgPcDx(inputFile);
                var outputFilename = Path.ChangeExtension(inputFile, ".png");

                image.SaveAsPng(outputFilename);

            });

            Console.WriteLine("Done");
        }


        public static void Process30WabTgpcdxFiles()
        {
            // Get all .tgpcdx files in the 30_WAB directory
            var tgpcdxFiles = Directory.GetFiles(Container30WAB.Directory, "*.tgpcdx", SearchOption.AllDirectories).OrderBy(d => d).ToList();

            var skins = new List<RandomSkin>();
            var queue = new List<(String, List<String>)>();
            int count = 1;
            string size = "";

            foreach (var file in tgpcdxFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length < 2 ) continue;
                var differentSize = !String.IsNullOrEmpty(size) && size != parts[0];
                size = parts[0];
                var iluCode = parts[1];

                var queueCount = queue.Sum(x => x.Item2.Count);

                if (queueCount + 1 > Container30WAB.MaxFilesPerSkin || differentSize)
                {
                    skins.Add(ProcessBatch(Container30WAB, count++, size, queue));
                    Console.WriteLine($"Generated {Container30WAB.Name}: {skins.Last().Name}, count: {skins.Last().Skins.Count}");
                    queue.Clear();
                }

                var fileGroup = queue.FirstOrDefault(q => q.Item1 == iluCode);
                if (fileGroup == default)
                {
                    queue.Add((iluCode, new List<string> { file }));
                }
                else
                {
                    fileGroup.Item2.Add(file);
                }
            }

            if (queue.Any())
            {
                skins.Add(ProcessBatch(Container30WAB, count++, size, queue));
                Console.WriteLine($"Generated last {Container30WAB.Name}: {skins.Last().Name}, count: {skins.Last().Skins.Count}");
            }

            File.WriteAllText($"RandomSkins{Container30WAB.Name}.json", JsonSerializer.Serialize(skins, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
