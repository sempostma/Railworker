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
    public class RandomContainerGenerator
    {
        private RWLibrary rwLib;
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

        public async Task Test()
        {
            var basePath = Path.Combine(rwLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");

            var compositions = Composition.FromJson(ReadFile("ContainerCombination.Compositions.json"));
            var skins = RandomSkin.FromJson(ReadFile("ContainerCombination.RandomSkins.json"));
            var containers45 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.45_HC.json"));

            var comp45 = compositions.First(c => c.Id == "45ft_hc_1")!;

            Console.WriteLine("Creating composition: " + comp45.Name);

            var firstSkin = skins.First()!;

            var composedImage = new Image<Rgba32>(2048, 2048);

            var count = Math.Min(firstSkin.Skins.Count, 36);

            var cancel = new CancellationTokenSource();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancel.Token
            };

            var container45NameFromTexture = new Regex(@"GW_45FT_(.+?)[\\/]Childs[\\/]textures[\\/]45_([^\.]+)");

            List<Image<Rgba32>> imagesForChatGPT = new List<Image<Rgba32>>();

            imagesForChatGPT.AddRange(Enumerable.Repeat<Image<Rgba32>>(null, count));
            
            await Parallel.ForEachAsync(Enumerable.Range(0, count), parallelOptions, async (i, cToken) =>
            {
                int baseX = i % 4 * 512;
                int baseY = (i / 4) * 227;

                var texture = firstSkin.Skins[i].Texture;
                var inputFile = Path.Combine(basePath, texture);

                Console.WriteLine("projecting: " + texture + $" ({i + 1}/{count})");

                var image = await rwLib.TgPcDxLoader.LoadTgPcDx(inputFile);
                var tempPath = Path.GetTempPath();
                var tempFilename = Path.Combine(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-input.png");
                var outputFilename = Path.Combine(tempPath, "RWLib", "Tgpcdx", Path.ChangeExtension(texture, null) + "-output.png");
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilename)!);

                image.Mutate(x => x.Resize(512, 512));

                image.SaveAsPng(tempFilename);

                File.Copy(tempFilename, outputFilename, true);
                //if (File.Exists(outputFilename) == false)
                //{
                //    await RunWaifu2XCommand(tempFilename, outputFilename);
                //}

                image = Image.Load<Rgba32>(outputFilename);

                foreach (var projection in comp45.Projections)
                {
                    Console.WriteLine("Projecting " + projection.Name);

                    Image<Rgba32> cutOutRegion = image.Clone(ctx => ctx.Crop(new Rectangle(
                        projection.SourceBbox.X,
                        image.Height - (projection.SourceBbox.Y + projection.SourceBbox.Height),
                        projection.SourceBbox.Width,
                        projection.SourceBbox.Height
                    )));

                    if (projection.Name == "side")
                    {
                        imagesForChatGPT[i] = cutOutRegion;
                    }

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

            var outputFilename = "result.png";
            var outputMetadataFilename = Path.ChangeExtension(outputFilename, "metadata.json");

            Console.WriteLine("Saving metadata...");
            File.WriteAllText(outputMetadataFilename, JsonSerializer.Serialize(skins));

            Console.WriteLine("Saving LUA config...");
            var outputLuaConfig = Path.ChangeExtension(outputFilename, "lua");
            WriteLuaConfig(outputLuaConfig, firstSkin);

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

        private Task<int> RunWaifu2XCommand(string inputFilename, string outputFilename)
        {
            var processInfo = new ProcessStartInfo();
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            processInfo.FileName = "C:\\Users\\Gebruiker\\Downloads\\waifu2x-caffe\\waifu2x-caffe\\waifu2x-caffe-cui.exe";

            var scaleRatio = "0.5";
            var noiseLevel = "2";

            processInfo.Arguments = $"-i \"{inputFilename}\" -o \"{outputFilename}\" --scale_ratio {scaleRatio} --noise_level {noiseLevel}";

            var process = new Process();
            process.StartInfo = processInfo;
            process.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) =>
            {
                Console.WriteLine("[waifu2x] info: " + process.StandardOutput.ToString());
                if (process.ExitCode == 0)
                {
                }
                else
                {
                    Console.WriteLine("[waifu2x] error: " + process.StandardOutput.ToString());
                }

                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            return tcs.Task;
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
