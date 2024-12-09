using RWLib;
using RWLib.Graphics;
using RWLib.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static RailworkerMegaFreightPack1.Utilities;
using BCnEncoder.Encoder;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;
using RWLib.Packaging;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Collections;
using System.Runtime.InteropServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.Concurrent;
using SixLabors.ImageSharp.Formats.Png;

namespace RailworkerMegaFreightPack1
{
    public class UICCodeGenerator()
    {
        private Dictionary<string, int> rvCounter = new Dictionary<string, int>();

        public string Next(string typeIndicator, string countryCode, string vehicleType)
        {
            string key = CreateKey(typeIndicator, countryCode, vehicleType);

            int count = rvCounter.GetValueOrDefault(key, 0);
            count++;
            rvCounter[key] = count;

            return RWUICWagonNumber.FromDigits(typeIndicator, countryCode, vehicleType, count.ToString())
                .ToString(RWUICWagonNumber.Format.Plain);
        }

        private static string CreateKey(string typeIndicator, string countryCode, string vehicleId)
        {
            return String.Join(":", new { typeIndicator, countryCode, vehicleId });
        }
    }

    public class RandomContainerGenerator
    {
        private RWLibrary rwLib;
        private UICCodeGenerator codeGenerator = new UICCodeGenerator();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private class Logger : IRWLogger
        {
            public void Log(RWLogType type, string message)
            {
                Console.WriteLine("{0}: {1}", type.ToString(), message);
            }
        }

        public RandomContainerGenerator()
        {
            rwLib = new RWLibrary(new RWLibOptions { Logger = new Logger() });
        }

        public async Task Build()
        {
            var compositions = Composition.FromJson(ReadFile("ContainerCombination.Compositions.json"));
            var randomSkins = RandomSkin.FromJson(ReadFile("ContainerCombination.RandomSkins.json"));
            var containers45 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.45_HC.json"));

            foreach(var randomskin in randomSkins)
            {
                var composition = compositions.FirstOrDefault(x => x.Id == randomskin.Composition);
                if (composition == null) throw new InvalidDataException("Could not find composition: " + randomskin.Composition);

                await BuildRandomSkin(randomskin, composition);
            }
        }

        public async Task BuildRandomSkin(RandomSkin randomSkin, Composition composition)
        {
            var basePath = Path.Combine(rwLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");

            Console.WriteLine("Creating composition: " + composition.Name);

            var composedImage = new Image<Rgba32>(2048, 2048);

            var count = Math.Min(randomSkin.Skins.Count, 36);

            var cancel = new CancellationTokenSource();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancel.Token
            };

            //var container45NameFromTexture = new Regex(@"GW_45FT_(.+?)[\\/]Childs[\\/]textures[\\/]45_([^\.]+)");

            await Parallel.ForEachAsync(Enumerable.Range(0, count), parallelOptions, async (i, cToken) =>
            {
                int baseX = i % 4 * 512;
                int baseY = (i / 4) * 227;

                var texture = randomSkin.Skins[i].Texture;
                var inputFile = Path.Combine(basePath, texture);

                Console.WriteLine("projecting: " + texture + $" ({i + 1}/{count})");

                var image = await rwLib.TgPcDxLoader.LoadTgPcDx(inputFile);
                var tempPath = Path.GetTempPath();
                var tempFilename = Path.Join(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-input.png");
                var outputFilename = Path.Join(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-output.png");
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilename)!);

                image.SaveAsPng(tempFilename);

                bool useWaifu = false;

                if (useWaifu)
                {
                    if (File.Exists(outputFilename) == false)
                    {
                        await RunWaifu2XCommand(tempFilename, outputFilename);
                    }
                } else
                {
                    File.Copy(tempFilename, outputFilename, true);
                }

                image = Image.Load<Rgba32>(outputFilename);
                image.Mutate(x => x.Resize(512, 512));

                foreach (var projection in composition.Projections)
                {
                    Console.WriteLine("Projecting " + projection.Name);

                    Image<Rgba32> cutOutRegion = image.Clone(ctx => ctx.Crop(new Rectangle(
                        projection.SourceBbox.X,
                        image.Height - (projection.SourceBbox.Y + projection.SourceBbox.Height),
                        projection.SourceBbox.Width,
                        projection.SourceBbox.Height
                    )));

                    var rotation = (RotateMode)Enum.Parse(typeof(RotateMode), projection.DestBbox.Rotate);

                    if (rotation != RotateMode.None) cutOutRegion.Mutate(ctx => ctx.Rotate(rotation));

                    cutOutRegion.Mutate(ctx => ctx.Resize(projection.DestBbox.Width, projection.DestBbox.Height));

                    var destX = projection.DestBbox.X + baseX;
                    var destY = 2048 - (projection.DestBbox.Y + baseY + projection.DestBbox.Height);

                    composedImage.Mutate(ctx => ctx.DrawImage(cutOutRegion, new Point(destX, destY), 1f));
                }
            });

            Console.WriteLine("Adding island margins...");

            AddPixelmarginsWherePossible(composedImage);

            Console.WriteLine("Saving result...");

            var outputFilename = randomSkin.Id + ".png";
            var outputMetadataFilename = Path.ChangeExtension(outputFilename, "metadata.json");
            composedImage.SaveAsPng(outputFilename);

            Console.WriteLine("Saving metadata...");
            File.WriteAllText(outputMetadataFilename, JsonSerializer.Serialize(randomSkin));

            Console.WriteLine("Creating autonumbering...");

            var autoNumbering = new List<string>();

            var smallestRarity = randomSkin.Skins.Min(x => x.Rarity);
            if (smallestRarity < 1) throw new InvalidDataException("Rarity is less then 1");

            var rarities = randomSkin.Skins.Select(x => x.Rarity / smallestRarity).ToArray();

            for (int i = 0; i < randomSkin.Skins.Count; i++)
            {
                var rarity = rarities[i];
                for (int j = 0; j < rarity; j++)
                {
                    autoNumbering.Add("0,0," + codeGenerator.Next("33", "84", "4962"));
                }
            }

            var autonumberingFilename = Path.ChangeExtension(outputFilename, ".csv");
            File.WriteAllLines(autonumberingFilename, autoNumbering);

            Console.WriteLine("Saving LUA config...");
            var outputLuaConfig = Path.ChangeExtension(outputFilename, "lua");
            WriteLuaConfig(outputLuaConfig, randomSkin);

            Console.WriteLine("Done");
            Console.WriteLine();
        }

        private void WriteLuaConfig(String destinationFile, RandomSkin rSkin)
        {
            var lua = new StringBuilder();
            lua.AppendLine("--- config name: " + rSkin.Name);
            lua.AppendLine("return {");

            var map = rSkin.Skins.Select(skin =>
            {
                return "    {name = \"" + skin.Name + "\", group = \"" + skin.Group + "\", rarity = " + skin.Rarity.ToString() + "}";
            });

            lua.AppendLine(String.Join(",\n", map));
            lua.AppendLine("}");

            File.WriteAllText(destinationFile, lua.ToString());
        }

        private async Task<int> RunWaifu2XCommand(string inputFilename, string outputFilename)
        {
            await semaphoreSlim.WaitAsync();
            try {
                var processInfo = new ProcessStartInfo();
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;

                processInfo.FileName = "C:\\Users\\Gebruiker\\Downloads\\waifu2x-caffe\\waifu2x-caffe\\waifu2x-caffe-cui.exe";

                var scaleRatio = "1";
                var noiseLevel = "2";

                processInfo.Arguments = $"-i \"{inputFilename}\" -o \"{outputFilename}\" --scale_ratio {scaleRatio} --noise_level {noiseLevel}";

                var process = new Process();
                process.StartInfo = processInfo;
                process.EnableRaisingEvents = true;

                var tcs = new TaskCompletionSource<int>();
                process.Exited += (sender, args) =>
                {
                    Console.WriteLine("[waifu2x] info: " + process.StandardOutput.ReadToEnd());
                    if (process.ExitCode == 0)
                    {
                    }
                    else
                    {
                        Console.WriteLine("[waifu2x] error: " + process.StandardOutput.ReadToEnd());
                    }

                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                };

                process.Start();

                var result = await tcs.Task;
                return result;
            } finally
            {
                semaphoreSlim.Release();
            }
        }

        static void AddPixelmarginsWherePossible(Image<Rgba32> image)
        {
            // Create a copy of the image
            using (Image<Rgba32> blurredImage = image.Clone())
            {
                // Apply a Gaussian blur to the copied image
                blurredImage.Mutate(ctx => ctx.GaussianBlur(1)); // Adjust blur radius as needed

                // Use the original image as a mask, and composite the blurred image over it
                image.Mutate(ctx => ctx.DrawImage(blurredImage, new GraphicsOptions
                {
                    BlendPercentage = 1f,
                    AlphaCompositionMode = PixelAlphaCompositionMode.DestAtop // Ensures transparent pixels are replaced
                }));

                // Make fully opaque
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Rgba32 pixel = image[x, y];
                        pixel.A = 255; // Set alpha to fully opaque
                        image[x, y] = pixel; // Update the image with the modified pixel
                    }
                }
            }
        }
    }
}
