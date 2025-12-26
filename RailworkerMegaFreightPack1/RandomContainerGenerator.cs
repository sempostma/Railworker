using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Emgu.CV;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
﻿﻿﻿﻿﻿using RWLib;
using RWLib.Graphics;
using RWLib.Interfaces;
using RWLib.Packaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using static RailworkerMegaFreightPack1.Utilities;

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
        private List<Composition> compositionsx1;
        private List<Composition> compositionsx2;
        private List<RandomSkinGroup> randomSkinGroups;
        private List<FileItem> containers45;
        private UICCodeGenerator codeGenerator = new UICCodeGenerator();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private bool createTextures = false;
        private bool createRvNumbers = false;
        private bool createThumbnails = false;
        private int thumbnailWidth = 512;
        private bool generateThumbnails = true;
        private bool generateAutonumberings = true;
        private bool generateMappings = false;
        private bool generateCatalog = true;
        private bool generateDDS = true;
        private bool generatePNG = true;
        private bool build4K = true;

        private Dictionary<string, Dictionary<string, int>> memoryList = new Dictionary<string, Dictionary<string, int>>();
        private List<string> backgroundResources;
        Dictionary<string, List<FileItem>> randomSkins;

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
            randomSkins = FileItem.FromJsonDictionary(ReadFile("RandomSkins.RandomSkins.json"));
            randomSkinGroups = RandomSkinGroup.FromJson(ReadFile("ContainerCombination.RandomSkins.json"));
            containers45 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.45_HC.json"));
            compositionsx1 = Composition.FromJson(ReadFile("ContainerCombination.Compositions.json"));
            compositionsx2 = Composition.FromJson(ReadFile("ContainerCombination.Compositions.json"));
            compositionsx2.ForEach(c =>
            {
                c.ComposedImageWidth *= 2;
                c.ComposedImageHeight *= 2;
                c.OutputScaleX *= 2;
                c.OutputScaleY *= 2;
                c.StylusXInterval *= 2;
                c.StylusYInterval *= 2;
                c.Id += "_x2";
            });
            backgroundResources = Utilities.FindResources("RandomSkins.Backgrounds").ToList();
        }

        public async Task Build(CancellationToken cToken)
        {
            await BuildForComposition(build4K ? compositionsx2 : compositionsx1, cToken);
        }

        public async Task BuildForComposition(List<Composition> compositionList, CancellationToken cToken)
        {
            var catalogGenerator = new ContainerCatalogGenerator();

            var deferredTasks = new List<Task>();

            foreach (var randomskinGroup in randomSkinGroups)
            {
                // TODO Remove
                //if (randomskinGroup.Id.StartsWith("40") == false) continue;
                //if (randomskinGroup.Id.StartsWith("40ft_hc") == false) continue;
                //if (randomskinGroup.Id.StartsWith("20ft_mixed") == false) continue;
                //if (randomskinGroup.Id.StartsWith("20ft") == false) continue;
                //if (randomskinGroup.Id.StartsWith("30ft") == false) continue;
                //if (randomskinGroup.Id.StartsWith("45ft_sp") == false) continue;

                var relatedGroups = randomSkinGroups.Where(x => x.Id != randomskinGroup.Id && x.Kind == randomskinGroup.Kind && x.Kind != null).ToList();

                Console.WriteLine("Creating randomskin: " + randomskinGroup.Id);

                var compositions = compositionList.Where(x => randomskinGroup.RandomSkins.Select(y => y.Composition).Any(y => x.Id.StartsWith(y))).ToList()!;

                if (compositions.GroupBy(x => x.ComposedImageHeight).Count() > 1)
                    throw new InvalidDataException("Compositions have different heights: " + String.Join(", ", compositions.Select(x => x.ComposedImageHeight)));
                if (compositions.GroupBy(x => x.ComposedImageWidth).Count() > 1)
                    throw new InvalidDataException("Compositions have different widths: " + String.Join(", ", compositions.Select(x => x.ComposedImageWidth)));

                var outputFilename = randomskinGroup.Id;

                var randomSkinCargoInfo = randomSkins.FirstOrDefault(x => x.Key == randomskinGroup?.Destination?.Split("\\").Skip(1).FirstOrDefault());
                
                if (randomSkinCargoInfo.Value == null)
                {
                    Console.WriteLine("Could not find cargo info for: " + randomskinGroup.Id + " in " + randomskinGroup.Destination);
                    continue;
                }
                
                var skinName = randomSkinCargoInfo.Value.Where(x => x.Filename == randomskinGroup.Id + ".bin" || x.Cargo?.Any(x => x.Filename == randomskinGroup.Id + ".bin") == true).FirstOrDefault()?.Name;

                if (skinName == null)
                {
                    Console.WriteLine("Could not find cargo info for: " + randomskinGroup.Id + " in " + randomskinGroup.Destination);
                    continue;
                }

                foreach (var randomSkin in randomskinGroup.RandomSkins)
                {
                    randomSkin.FillAndOrderSkins(relatedGroups);
                }

                if (generateCatalog)
                {
                    catalogGenerator.GenerateHtml(randomskinGroup, randomSkinCargoInfo, skinName, compositionList);
                }

                var composedImageWidth = compositions.First().ComposedImageWidth;
                var composedImageHeight = compositions.First().ComposedImageHeight;

                var composedImage = new Image<Rgba32>(composedImageWidth, composedImageHeight);

                if (randomskinGroup.Background != null)
                {
                    var backgroundResource = backgroundResources.Single(x => x.StartsWith("RandomSkins.Backgrounds." + randomskinGroup.Background));
                    var resource = OpenFile(backgroundResource);
                    var tempDir = Path.Combine(Path.GetTempPath(), "RailworkerMegaFreightPack1");
                    Directory.CreateDirectory(tempDir); // ensure directory
                    var path = Path.Combine(tempDir, Convert.ToString(Random.Shared.Next(), 16) + ".png");
                    using (var image = new MagickImage(resource))
                    {
                        await image.WriteAsync(path);
                    }
                    var backgroundImage = await rwLib.ImageDecoder.FromFilename(path);
                    backgroundImage.Mutate(backgroundImage => backgroundImage.Resize(composedImageWidth, composedImageHeight));
                    composedImage.Mutate(ctx => ctx.DrawImage(backgroundImage, new Point(0, 0), 1f));
                }

                var tasks = CreateTasks(randomskinGroup, compositions, composedImage, cToken).ToList();

                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = 10, // prevent accessing the same file at once
                    CancellationToken = cToken
                };

                var pngEncoder = new PngEncoder
                {
                    ColorType = PngColorType.Rgb,
                    CompressionLevel = PngCompressionLevel.BestCompression,
                };

                var jpegEncoder = new JpegEncoder
                {
                    Quality = 85
                };

                if (generateMappings)
                {
                    foreach (var rndExample in tasks.GroupBy(task => task.RandomSkin.Id))
                    {
                        var sk = rndExample.First();
                        (await sk.DrawMapping(false, cToken)).SaveAsPng(sk.RandomSkin.Id + "-mapping.png", pngEncoder);
                        (await sk.DrawMapping(true, cToken)).SaveAsPng(sk.RandomSkin.Id + "-mapping-example.png", pngEncoder);
                    }
                }

                if (generateThumbnails) {
                    //var container45NameFromTexture = new Regex(@"GW_45FT_(.+?)[\\/]Childs[\\/]textures[\\/]45_([^\.]+)");
                    Directory.CreateDirectory("thumbnails");
                    Directory.CreateDirectory(Path.Join("thumbnails", randomskinGroup.Id));
                }

                await Parallel.ForEachAsync(tasks, parallelOptions, async (generatotor, cToken) =>
                {
                    await generatotor.Build(cToken);
                    if (generateThumbnails && generatotor.Thumbnail != null)
                    {
                        await generatotor.Thumbnail.SaveAsJpegAsync(
                            Path.Join("thumbnails", randomskinGroup.Id, generatotor.CargoNumber + ".jpg"),
                            jpegEncoder
                        );
                    }
                });

                Console.WriteLine("Adding island margins...");

                AddPixelmarginsWherePossible(composedImage);

                Console.WriteLine("Saving result...");

                if (generatePNG)
                {
                    deferredTasks.Add(Task.Run(async () =>
                    {
                        var outputTextureFilename = Path.ChangeExtension(outputFilename, ".png");
                        await composedImage.SaveAsPngAsync(outputTextureFilename, pngEncoder);
                        if (randomskinGroup.Destination != null && randomskinGroup.Destination.EndsWith(".png"))
                            File.Copy(outputTextureFilename, Path.Combine(rwLib.TSPath, "Source", randomskinGroup.Destination), true);
                    }));
                }

                if (generateDDS)
                {
                    deferredTasks.Add(Task.Run(async () => { 
                        // TODO: Discover the correct output options/header by exporting with TS tools and use those settings to output and then check if the blueprint editor is able to create a valid tgpcdx file with the DDS
                        var outputDDSTextureFilename = Path.ChangeExtension(outputFilename, ".dds");
                        var ddsEncoder = new BcEncoder();
                        ddsEncoder.OutputOptions.GenerateMipMaps = true;
                        ddsEncoder.OutputOptions.Quality = CompressionQuality.Fast;
                        ddsEncoder.OutputOptions.DdsBc1WriteAlphaFlag = true;
                        ddsEncoder.OutputOptions.Format = CompressionFormat.Bc1;
                        ddsEncoder.OutputOptions.FileFormat = OutputFileFormat.Dds; //Change to Dds for a dds file.
                        using (var stream = File.Open(outputDDSTextureFilename, FileMode.Create))
                        {
                            var file = await ddsEncoder.EncodeToDdsAsync(composedImage, cToken);
                            file.Write(stream);
                        }
                        if (randomskinGroup.Destination != null && randomskinGroup.Destination.EndsWith(".dds"))
                            File.Copy(outputDDSTextureFilename, Path.Combine(rwLib.TSPath, "Source", randomskinGroup.Destination), true);
                    }));
                }

                if (generateAutonumberings)
                {
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
                                autoNumbering.Add("0,0," + uid + ":" + ((i + 1).ToString("D2")));
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
                }

                Console.WriteLine("Done");
                Console.WriteLine();
            }

            PrintSummaryOverview();

            foreach (var task in deferredTasks)
            {
                task.Wait();
            }

            if (generateCatalog)
            {
                File.WriteAllText("catalog.html", catalogGenerator.ToString());
                
                // Generate catalog PDF with working internal links
                await ManualGenerator.ConvertHtmlToPdfAsync("catalog.html", "Catalog.pdf");
            }
        }

        private void PrintSummaryOverview()
        {
            Console.WriteLine("Generator result summary:");
            Console.WriteLine();


            foreach (var textureBaseDirectory in memoryList)
            {
                var basePath = Path.Combine(rwLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");
                basePath = String.IsNullOrEmpty(textureBaseDirectory.Key) ? basePath : Path.Combine(rwLib.TSPath, "Assets", textureBaseDirectory.Key);

                var summaryOfMostUsed = textureBaseDirectory.Value.Where(x => x.Value > 1).OrderByDescending(x => x.Value).Take(10);

                Console.WriteLine("These cargos are used more than once (first 10):");

                foreach (var item in summaryOfMostUsed)
                {
                    Console.WriteLine($"{item.Key}: {item.Value}");
                }

                int missing = 0;

                foreach (var file in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
                {
                    var relativeFile = Path.GetRelativePath(basePath, file);
                    var filenameWithoutExtensions = Path.ChangeExtension(relativeFile, null);

                    if (textureBaseDirectory.Value.ContainsKey(filenameWithoutExtensions) == false)
                    {
                        //if (missing > 10)
                        //{
                        //    Console.WriteLine($"More than 10 missing texture files, skipping the rest...");
                        //    break;
                        //}
                        Console.WriteLine($"Unused texture file: {relativeFile}");
                        missing++;
                    }
                }
                
            }

            Console.WriteLine("End of result summary");
        }

    public class ComposedTextureGenerator
        {
            public required RWLibrary RWLib { get; set; }
            public required int CargoNumber { get; set; }
            public required RandomSkin.SkinTexture Texture { get; set; }
            public required Composition Composition { get; set; }
            public required Image<Rgba32> ComposedImage { get; set; }
            public required int BaseX { get; set; }
            public required int BaseY { get; set; }
            public required RandomSkin RandomSkin { get; set; }

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
                        var tempImage = await RWLib.TgPcDxLoader.LoadTgPcDx(inputFile);
                        image = tempImage.CloneAs<Rgba32>();
                    } catch (Exception ex) {
                        throw ex;
                    }
                }
                else if (texture.EndsWith(".dds"))
                {
                    var ddsDecoder = new BcDecoder();
                    using (var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var tempImage = await ddsDecoder.DecodeToImageRgba32Async(stream);
                        image = tempImage.CloneAs<Rgba32>();
                    }
                }
                else
                {
                    image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputFile);
                }

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

                Thumbnail.Mutate(ctx => ctx.Resize(0, 120));
                image.Dispose();

                
            }

            public async Task<Image<Rgba32>> DrawMapping(bool includeTexture = false, CancellationToken? cancellationToken = null)
            {
                var basePath = Path.Combine(RWLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");
                basePath = String.IsNullOrEmpty(Composition.BasePath) ? basePath : Path.Combine(RWLib.TSPath, "Assets", Composition.BasePath);

                var texture = Texture.Texture;
                if (String.IsNullOrEmpty(texture)) throw new Exception("Texture cannot be null when drawing a mapping");
                var inputFile = Path.Combine(basePath, texture);

                Image<Rgba32> image;
                if (texture.EndsWith(".TgPcDx"))
                {
                    try
                    {
                        image = await RWLib.TgPcDxLoader.LoadTgPcDx(inputFile);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else if (texture.EndsWith(".dds"))
                {
                    var ddsDecoder = new BcDecoder();
                    using (var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        image = await ddsDecoder.DecodeToImageRgba32Async(stream);
                    }
                }
                else
                {
                    image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputFile);
                }

                if (!includeTexture)
                {
                    image.Mutate(x => x.Clear(Color.Black));
                }

                float inputRatioX = (float)image.Width / Composition.InputImageResizeWidth;
                float inputRatioY = (float)image.Height / Composition.InputImageResizeHeight;

                cancellationToken?.ThrowIfCancellationRequested();

                var averageDownscale = (int)Math.Round((
                    Composition.Projections.Average(a => (float)a.SourceBbox.Width / a.DestBbox.Width / Composition.OutputScaleX)
                    + Composition.Projections.Average(a => (float)a.SourceBbox.Height / a.DestBbox.Height / Composition.OutputScaleY)
                ) / 2f);

                if (averageDownscale < 1) averageDownscale = 1;

                var queue = new Queue<Color>(new[] {
                    Color.Red,
                    Color.Green,
                    Color.Blue,
                    Color.Orange,
                    Color.Orchid,
                    Color.Pink,
                    Color.Yellow,
                });

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
                        image.Mutate(image => image.Draw(queue.Dequeue(), 3, cropRect));
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    cancellationToken?.ThrowIfCancellationRequested();

                    var rotation = (RotateMode)Enum.Parse(typeof(RotateMode), projection.DestBbox.Rotate);

                    if (rotation != RotateMode.None) cutOutRegion.Mutate(ctx => ctx.Rotate(rotation));

                    cancellationToken?.ThrowIfCancellationRequested();

                    var destWidth = (int)(projection.DestBbox.Width);
                    var destHeight = (int)(projection.DestBbox.Height);

                    var scaledX = (int)(projection.DestBbox.X);
                    var scaledY = (int)((projection.DestBbox.Y + projection.DestBbox.Height));

                    cutOutRegion.Mutate(ctx => ctx.Resize(destWidth, destHeight, KnownResamplers.NearestNeighbor));

                    cancellationToken?.ThrowIfCancellationRequested();

                    cancellationToken?.ThrowIfCancellationRequested();
                }

                return image;
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

        private IEnumerable<ComposedTextureGenerator> CreateTasks(RandomSkinGroup randomSkinGroup, List<Composition> compositions, Image<Rgba32> ComposedImage, CancellationToken cancellationToken)
        {
            var x = 0;
            var y = 0;

            var cargoNumber = 1;

            foreach (var randomSkin in randomSkinGroup.RandomSkins)
            {
                var composition = compositions.FirstOrDefault(x => x.Id.StartsWith(randomSkin.Composition));
                if (composition == null) throw new InvalidDataException("Could not find composition: " + randomSkin.Composition);

                if (memoryList.ContainsKey(composition.BasePath) == false) {
                    memoryList.Add(composition.BasePath, new Dictionary<string, int>());
                }
                var doneList = memoryList[composition.BasePath];

                var skins = randomSkin.Skins;

                var stackIndex = 0;
                for (int i = 0; i < skins.Count; i++)
                {
                    var skin = skins[i];
                    var stackOffset = composition.StylusYInterval * stackIndex;

                    if (!String.IsNullOrEmpty(skin.Texture))
                    {

                        if (skin.Name.Contains(skin.Group) && skin.Id.Contains(skin.Group) == false)
                        {
                            // not really an error but helps checking for mistyped group names
                            Console.WriteLine("The skin group name does not occur in the skin name or in the skin id.");
                        }

                        var doneKey = Path.ChangeExtension(skin.Texture, null);

                        if (doneList.ContainsKey(doneKey) == false)
                        {
                            doneList.Add(doneKey, 0);
                        } else
                        {
                            // noop
                            doneList[doneKey] = doneList[doneKey];
                        }
                        doneList[doneKey]++;

                        yield return new ComposedTextureGenerator
                        {
                            RWLib = rwLib,
                            Texture = skin,
                            Composition = composition,
                            ComposedImage = ComposedImage,
                            BaseX = x,
                            BaseY = y + stackOffset,
                            CargoNumber = cargoNumber++,
                            RandomSkin = randomSkin
                        };
                    }

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

        public static void AddPixelmarginsWherePossible(Image<Rgba32> image)
        {
            // Create a copy of the image
            using (Image<Rgba32> blurredImage = image.Clone())
            {
                // Apply a Gaussian blur to the copied image
                blurredImage.Mutate(ctx => ctx.GaussianBlur(1)); // Adjust blur radius as needed

                // Use the original image as a mask, and composite the blurred image over it
                image.Mutate(ctx => ctx.DrawImage(blurredImage, new SixLabors.ImageSharp.GraphicsOptions
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
