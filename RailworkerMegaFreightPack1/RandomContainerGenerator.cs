﻿﻿﻿﻿﻿using RWLib;
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
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using System.Buffers.Text;
using System.Threading.Channels;
using System.Threading;
using System.Linq.Expressions;

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
        private List<Composition> compositions;
        private List<RandomSkinGroup> randomSkinGroups;
        private List<FileItem> containers45;
        private UICCodeGenerator codeGenerator = new UICCodeGenerator();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private bool createTextures = false;
        private bool createRvNumbers = false;
        private bool createThumbnails = false;
        private int thumbnailWidth = 512;
        
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
            randomSkinGroups = RandomSkinGroup.FromJson(ReadFile("ContainerCombination.RandomSkins.json"));
            containers45 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.45_HC.json"));
            compositions = Composition.FromJson(ReadFile("ContainerCombination.Compositions.json"));
        }

        public async Task Build(CancellationToken cToken)
        {
            var catalogGenerator = new ContainerCatalogGenerator();

            foreach (var randomskinGroup in randomSkinGroups)
            {
                // TODO Remove
                // if (randomskinGroup.Id.StartsWith("782tt") == false) continue;

                Console.WriteLine("Creating randomskin: " + randomskinGroup.Id);

                var compositions = this.compositions.Where(x => randomskinGroup.RandomSkins.Select(y => y.Composition).Contains(x.Id)).ToList()!;

                if (compositions.GroupBy(x => x.ComposedImageHeight).Count() > 1)
                    throw new InvalidDataException("Compositions have different heights: " + String.Join(", ", compositions.Select(x => x.ComposedImageHeight)));
                if (compositions.GroupBy(x => x.ComposedImageWidth).Count() > 1)
                    throw new InvalidDataException("Compositions have different widths: " + String.Join(", ", compositions.Select(x => x.ComposedImageWidth)));

                var outputFilename = randomskinGroup.Id;

                catalogGenerator.GenerateHtml(randomskinGroup, compositions);

                var composedImageWidth = compositions.First().ComposedImageWidth;
                var composedImageHeight = compositions.First().ComposedImageHeight;

                var composedImage = new Image<Rgba32>(composedImageWidth, composedImageHeight);

                var tasks = CreateTasks(randomskinGroup, composedImage, cToken);

                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = 10, // prevent accessing the same file at once
                    CancellationToken = cToken
                };

                //var container45NameFromTexture = new Regex(@"GW_45FT_(.+?)[\\/]Childs[\\/]textures[\\/]45_([^\.]+)");
                Directory.CreateDirectory(Path.Join("thumbnails", randomskinGroup.Id));

                await Parallel.ForEachAsync(tasks, parallelOptions, async (generatotor, cToken) => {
                    await generatotor.Build(cToken);
                    if (generatotor.Thumbnail == null) return;
                    await generatotor.Thumbnail.SaveAsPngAsync(
                        Path.Join("thumbnails", randomskinGroup.Id, generatotor.CargoNumber + ".png")
                    );
                });

                Console.WriteLine("Adding island margins...");

                AddPixelmarginsWherePossible(composedImage);

                Console.WriteLine("Saving result...");

                var outputTextureFilename = Path.ChangeExtension(outputFilename, ".png");
                composedImage.SaveAsPng(outputTextureFilename);

                Console.WriteLine("Creating autonumbering...");

                var autoNumbering = new List<string>();

                var skins = randomskinGroup.RandomSkins.SelectMany(x => x.Skins).ToList();
                var smallestRarity = skins.Min(x => x.Rarity);
                if (smallestRarity < 1) throw new InvalidDataException("Rarity is less then 1");

                var rarities = skins.Select(x => x.Rarity / smallestRarity).ToArray();

                try
                {
                    // We used to use rarity as a multiplier but now we use it order it so we can reuse the autonumbering accross different skins
                    for (int i = 0; i < skins.Count(); i++)
                    {
                        var amountOfSkins = (((float)skins.Count() - i) / skins.Count()) * 4.0;
                        for (int j = 0; j < amountOfSkins; j++)
                        {
                            var uid = codeGenerator.Next("33", "84", "4962");
                            autoNumbering.Add("0,0," + uid + "_" + ((i + 1).ToString("D2")));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to create autonumbering for: " + randomskinGroup.Id);
                    Console.WriteLine(ex.ToString());
                }

                var autonumberingFilename = Path.ChangeExtension(outputFilename, ".csv");
                File.WriteAllLines(autonumberingFilename, autoNumbering);

                Console.WriteLine("Done");
                Console.WriteLine();
            }

            File.WriteAllText("catalog.html", catalogGenerator.ToString());

        }

        class ComposedTextureGenerator
        {
            public required RWLibrary RWLib { get; set; }
            public required int CargoNumber { get; set; }
            public required RandomSkin.SkinTexture Texture { get; set; }
            public required Composition Composition { get; set; }
            public required Image<Rgba32> ComposedImage { get; set; }
            public required int BaseX { get; set; }
            public required int BaseY { get; set; }
            public Image<Rgba32>? Thumbnail { get; private set; } = null;

            public async Task Build(CancellationToken cancellationToken)
            {
                var basePath = Path.Combine(RWLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");
                basePath = String.IsNullOrEmpty(Composition.BasePath) ? basePath : Path.Combine(RWLib.TSPath, "Assets", Composition.BasePath);

                var texture = Texture.Texture;
                if (String.IsNullOrEmpty(texture)) return;
                var inputFile = Path.Combine(basePath, texture);

                Image<Rgba32> image;
                if (texture.EndsWith(".TgPcDx"))
                {
                    try
                    {
                        image = await RWLib.TgPcDxLoader.LoadTgPcDx(inputFile);
                    } catch (Exception ex) {
                        throw ex;
                    }
                }
                else if (texture.EndsWith(".dds"))
                {
                    var ddsDecoder = new BcDecoder();
                    using (var stream = File.OpenRead(inputFile))
                    {
                        image = await ddsDecoder.DecodeToImageRgba32Async(stream);
                    }
                }
                else
                {
                    image = await Image.LoadAsync<Rgba32>(inputFile);
                }

                var tempPath = Path.GetTempPath();
                var tempFilename = Path.Join(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-input.png");
                var outputFilename = Path.Join(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-output.png");
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilename)!);

                image.SaveAsPng(tempFilename);

                cancellationToken.ThrowIfCancellationRequested();

                bool useWaifu = Composition.Waifu2xEnabled;

                if (useWaifu)
                {
                    if (File.Exists(outputFilename) == false)
                    {
                        try
                        {
                            await RunWaifu2XCommand(tempFilename, outputFilename, cancellationToken, Composition.Waifu2xScaleRatio, Composition.Waifu2xNoiseLevel);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("error happened while processing: " + outputFilename);
                            throw ex;
                        }
                    }
                }
                else
                {
                    File.Copy(tempFilename, outputFilename, true);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                image = Image.Load<Rgba32>(outputFilename);
                float inputRatioX = (float)image.Width / Composition.InputImageResizeWidth;
                float inputRatioY = (float)image.Height / Composition.InputImageResizeHeight;

                cancellationToken.ThrowIfCancellationRequested();

                var averageDownscale = (int)Math.Round((
                    Composition.Projections.Average(a => (float)a.SourceBbox.Width / a.DestBbox.Width / Composition.OutputScaleX)
                    + Composition.Projections.Average(a => (float) a.SourceBbox.Height / a.DestBbox.Height / Composition.OutputScaleY)
                ) / 2f);

                if (averageDownscale < 1) averageDownscale = 1;

                Thumbnail = new Image<Rgba32>(
                    Composition.StylusXInterval * averageDownscale,
                    Composition.StylusYInterval * averageDownscale
                );

                // Create composed image
                foreach (var projection in Composition.Projections)
                {
                    var cropRect = new Rectangle(
                        projection.SourceBbox.X,
                        (int)(image.Height / inputRatioY) - (projection.SourceBbox.Y + projection.SourceBbox.Height),
                        projection.SourceBbox.Width,
                        projection.SourceBbox.Height
                    );

                    cropRect = new Rectangle(
                        (int)(cropRect.X * inputRatioX),
                        (int)(cropRect.Y * inputRatioY),
                        (int)(cropRect.Width * inputRatioX),
                        (int)(cropRect.Height * inputRatioY)
                    );

                    Console.WriteLine($"Projecting {projection.Name}");
                    Image<Rgba32> cutOutRegion;

                    try
                    {
                        cutOutRegion = image.Clone(ctx => ctx.Crop(cropRect));
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var rotation = (RotateMode)Enum.Parse(typeof(RotateMode), projection.DestBbox.Rotate);

                    if (rotation != RotateMode.None) cutOutRegion.Mutate(ctx => ctx.Rotate(rotation));

                    cancellationToken.ThrowIfCancellationRequested();

                    var destWidth = (int)(projection.DestBbox.Width * Composition.OutputScaleX);
                    var destHeight = (int)(projection.DestBbox.Height * Composition.OutputScaleX);

                    var scaledX = (int)(projection.DestBbox.X * Composition.OutputScaleX);
                    var scaledY = (int)((projection.DestBbox.Y + projection.DestBbox.Height) * Composition.OutputScaleY);

                    // Thumbnail
                    var thumbnailCutout = cutOutRegion.Clone();
                    thumbnailCutout.Mutate(ctx => ctx.Resize(destWidth * averageDownscale, destHeight * averageDownscale));
                    Thumbnail.Mutate(ctx => ctx.DrawImage(thumbnailCutout, new Point(scaledX * averageDownscale, Thumbnail.Height - scaledY * averageDownscale), 1f));

                    cutOutRegion.Mutate(ctx => ctx.Resize(destWidth, destHeight, KnownResamplers.NearestNeighbor));

                    cancellationToken.ThrowIfCancellationRequested();

                    var destX = scaledX + BaseX;
                    var destY = Composition.ComposedImageHeight - (scaledY + BaseY);

                    ComposedImage.Mutate(ctx => ctx.DrawImage(cutOutRegion, new Point(destX, destY), 1f));

                    cancellationToken.ThrowIfCancellationRequested();
                }

                Thumbnail.Mutate(ctx => ctx.Resize(512, 0));
                image.Dispose();
            }

            private async Task<int> RunWaifu2XCommand(string inputFilename, string outputFilename, CancellationToken cancellationToken, string scaleRatio = "0.5", string noiseLevel = "2.0")
            {
                await semaphoreSlim.WaitAsync();
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var processInfo = new ProcessStartInfo();
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    processInfo.RedirectStandardError = true;
                    processInfo.RedirectStandardOutput = true;

                    processInfo.FileName = "C:\\Users\\Gebruiker\\Downloads\\waifu2x-caffe\\waifu2x-caffe\\waifu2x-caffe-cui.exe";

                    processInfo.Arguments = $"-i \"{inputFilename}\" -o \"{outputFilename}\" --scale_ratio {scaleRatio} --noise_level {noiseLevel}";

                    var process = new Process();
                    process.StartInfo = processInfo;
                    process.EnableRaisingEvents = true;

                    var registration = cancellationToken.Register(() =>
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    });

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

                    registration.Dispose();
                    return result;
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
        }

        private IEnumerable<ComposedTextureGenerator> CreateTasks(RandomSkinGroup randomSkinGroup, Image<Rgba32> ComposedImage, CancellationToken cancellationToken)
        {
            var x = 0;
            var y = 0;

            var cargoNumber = 1;

            foreach (var randomSkin in randomSkinGroup.RandomSkins)
            {
                var composition = compositions.FirstOrDefault(x => x.Id == randomSkin.Composition);
                if (composition == null) throw new InvalidDataException("Could not find composition: " + randomSkin.Composition);

                var skins = randomSkin.Skins.OrderByDescending(x => x.Rarity).ToList();

                var duplicates = skins.GroupBy(x => x.Texture)
                    .Where(g => !String.IsNullOrEmpty(g.First().Texture) && g.Count() > 1);

                foreach (var duplicate in duplicates)
                {
                    Console.WriteLine("Found a duplicate in " + randomSkin.Id + ": " + duplicate.Key);
                    throw new InvalidDataException("Duplicate found in " + randomSkin.Id + ": " + duplicate.Key);
                }

                while (skins.Count < randomSkin.FullSkinsAmount)
                {
                    Console.WriteLine("Composition is not fully filled. The remaining space will be filled with duplicates.");
                    skins.AddRange(skins.ToArray());
                    if (skins.Count > randomSkin.FullSkinsAmount)
                    {
                        skins.RemoveRange(randomSkin.FullSkinsAmount, skins.Count - randomSkin.FullSkinsAmount);
                    }
                }

                if (skins.Count > randomSkin.FullSkinsAmount)
                {
                    Console.WriteLine("More skins: " + skins.Count + " found than the maximum allowed: " + randomSkin.FullSkinsAmount);
                    throw new InvalidDataException("More skins found than the maximum allowed");
                }

                var stackIndex = 0;
                for (int i = 0; i < skins.Count; i++)
                {
                    var skin = skins[i];
                    var stackOffset = composition.StylusYInterval * stackIndex;

                    yield return new ComposedTextureGenerator
                    {
                        RWLib = rwLib,
                        Texture = skin,
                        Composition = composition,
                        ComposedImage = ComposedImage,
                        BaseX = x,
                        BaseY = y + stackOffset,
                        CargoNumber = cargoNumber++
                    };

                    if (++stackIndex >= randomSkin.Stacked)
                    {
                        x += composition.StylusXInterval;
                        if (x >= composition.StylusXInterval * composition.ComposedImageColumns)
                        {
                            x = 0;
                            y += composition.StylusYInterval * randomSkin.Stacked;
                        }
                        stackIndex = 0;
                    }
                }
            }
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
