using PdfSharp.Pdf.Content.Objects;
using RWLib;
using RWLib.Exceptions;
using RWLib.Interfaces;
using RWLib.Packaging;
using RWLib.RWBlueprints.Components;
using RWLib.Scenario;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static RailworkerMegaFreightPack1.AfirusSggmrssGenerator;
using static RailworkerMegaFreightPack1.Utilities;

namespace RailworkerMegaFreightPack1
{
    public class AfirusSggmrssGenerator
    {
        private class Logger : IRWLogger
        {
            public void Log(RWLogType type, string message)
            {
                Console.WriteLine("{0}: {1}", type.ToString(), message);
            }
        }

        private RWLibrary rwLib;
        private XDocument aTemplate;
        private XDocument bTemplate;
        private XDocument reskinATemplate;
        private XDocument reskinBTemplate;
        private string CompanyLong = "Wascosa";
        private string Company = "CH-WASCO";
        private string CompanyShort = "WAS";
        private string SnakeCase = "ch_wascosa";

        private Dictionary<string, bool> autonumberingCache = new Dictionary<string, bool>();

        public class WagonVariation { 
            public string CompanyLong { get; set; }
            public string Company {  get; set; }
            public string CompanyShort { get; set; }

            public string SnakeCase { get; set; }
        }

        public class Reskin
        {
            [JsonPropertyName("texture")]
            public string Texture { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("shortName")]
            public string ShortName { get; set; }
        }

        public List<string> companies = new List<string> { "WAS" };

        public Dictionary<string, List<FileItem>> randomSkins;

        public AfirusSggmrssGenerator()
        {
            this.rwLib = new RWLibrary(new RWLibOptions { Logger = new Logger() });
            this.aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            this.bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            this.reskinATemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A_Reskin.xml"));
            this.reskinBTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B_Reskin.xml"));

            var randomSkinsJson = ReadFile("RandomSkins.RandomSkins.json");
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            randomSkins = JsonSerializer.Deserialize<Dictionary<string, List<FileItem>>>(randomSkinsJson, options)!;
        }

        public async Task GenerateVariants()
        {
            try
            {
                await GenerateRandomSkinVariants();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        class IndexItem
        {
            [JsonPropertyName("binPath")]
            public required string BinPath { get; set; }
            [JsonPropertyName("niceName")]
            public required string NiceName { get; set; }
        }

        private List<WagonType> CreateWagonTypes(XDocument aTemplate, XDocument bTemplate, List<String> companies, string geoName, MatrixTransformable? matrixTransformable = null, float bSideZOffset = 0)
        {
            matrixTransformable = matrixTransformable ?? new MatrixTransformable();
            return companies.Select(componay =>
                new WagonType
                {
                    InGameType = String.Join("", new Regex(@".+?-").Replace(componay, "").Take(4)),
                    Type = componay,
                    KeepAutoNumbering = true,
                    BlueprintTemplates = (new List<string> { "a", "b" }).Select(side =>
                        new WagonType.BlueprintTemplate()
                        {
                            GeoFileName = $"AlexAfirus\\KI Sggmrss90\\{geoName}_{side}",
                            Label = side.ToUpper(),
                            InvertZ = side == "b",
                            XDocument = side == "a" ? aTemplate : bTemplate,
                            MoveX = matrixTransformable.MoveX,
                            MoveY = matrixTransformable.MoveY,
                            MoveZ = matrixTransformable.MoveZ + (side == "b" ? bSideZOffset : 0),
                            ScaleX = matrixTransformable.ScaleX,
                            ScaleY = matrixTransformable.ScaleY,
                            ScaleZ = matrixTransformable.ScaleZ,
                            RotateX = matrixTransformable.RotateX,
                            RotateY = matrixTransformable.RotateY + (side == "b" ? 180 : 0),
                            RotateZ = matrixTransformable.RotateZ,
                        }).ToList()
                }).ToList();
        }

        public async Task GenerateReskinBlueprints()
        {
            Console.WriteLine("Generating Reskin Blueprints!");

            var reskinsJson = ReadFile("Sggmrss.Reskins.json");
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            Dictionary<string, List<Reskin>> reskinsDic = JsonSerializer.Deserialize<Dictionary<string, List<Reskin>>>(reskinsJson, options)!;

            var reskins = reskinsDic["reskins"];

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "", new MatrixTransformable
            {
                MoveZ = 0,
                MoveY = 1.170185f,
                ScaleZ = 0
            });

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };

            await Parallel.ForEachAsync(reskins, parallelOptions, async (reskin, _) =>
            {
                var textureSource = Path.Combine(rwLib.TSPath, "Source", "AlexAfirus", reskin.Texture);
                var textureDestination = Path.Combine(rwLib.TSPath, "Assets", "AlexAfirus", reskin.Texture);
                textureDestination = Path.ChangeExtension(textureDestination, ".TgPcDx");

                await rwLib.ImageDecoder.ConvertToTgPcDx(textureSource, textureDestination);

                foreach (var cargo in randomSkins)
                {
                    foreach (var cargoVariation in cargo.Value)
                    {
                        foreach (var wagon in new string[] { "A", "B" })
                        {
                            var xml = new XDocument(wagon == "A" ? reskinATemplate : reskinBTemplate);

                            var containerNiceName = cargoVariation.Name.Replace("KI ", "").Replace("ft", "");
                            var containerFilename = containerNiceName.ToLower().Replace(" ", "_");
                            string wagonName = String.Format("90' KI {0} {1} {2}", reskin.ShortName, containerNiceName, wagon);
                            xml.Descendants("Name").First().Value = wagonName;
                            xml.Descendants("English").First().Value = wagonName;
                            string wagonFilename = "sggmrss_" + containerFilename + "_" + wagon.ToLower() + ".bin";

                            var rootWagon = "sggmrss_" + containerFilename + "_" + wagon.ToLower() + ".xml";
                            xml.Descendants("ReskinAssetBpId").First().Descendants("BlueprintID").First().Value = rootWagon;

                            var textureIdPath = Path.Join("AlexAfirus", reskin.Texture).Split('\\');
                            var textureId = String.Join("\\", textureIdPath.SkipLast(1).Concat(new string[] { "[00]" + Path.ChangeExtension(textureIdPath.Last(), null) }));

                            xml.Descendants("TextureEntry").First().Descendants("TextureID").First().Value = textureId;

                            var productName = reskin.Texture.Split("\\").First();

                            var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "AlexAfirus", productName, wagonFilename);
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            var tempPath = await rwLib.Serializer.SerializeWithSerzExe(xml);
                            File.Copy(tempPath, destinationPath, true);
                        }
                    }
                }
            });
        }

        private async Task GenerateRandomSkinVariants()
        {
            Console.WriteLine("Generating Other Variants!");

            foreach (var set in randomSkins)
            {
                var is45ftHC = set.Key == "KI 45ft HC";
                var geoFile = is45ftHC ? "sggmrss_45" : "sggmrss";

                List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, geoFile, new MatrixTransformable
                {
                    MoveZ = 0,
                    MoveY = 1.170185f,
                    ScaleZ = 0
                });

                set.Value.ForEach(x => {
                    x.CargoAsChild = true;
                    x.ChildName = "Cargo_A";
                    x.AutoNumber = Path.Combine("AlexAfirus\\KI Sggmrss90\\AutoNumber", x.AutoNumber);

                    // compile autonumbering
                    if (autonumberingCache.ContainsKey(x.AutoNumber)) return;

                    var csvFilename = Path.Combine(rwLib.TSPath, "Source", x.AutoNumber + ".csv");
                    var xDoc = rwLib.CreateDCSV(csvFilename);
                    var dcsvFilename = Path.Combine(rwLib.TSPath, "Assets", x.AutoNumber + ".dcsv");
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = new UTF8Encoding(false); // The false means, do not emit the BOM.
                    settings.Indent = true;
                    using (XmlWriter w = XmlWriter.Create(dcsvFilename, settings))
                    {
                        xDoc.Save(w);
                    }
                });

                await rwLib.VariantGenerator.CreateVariants(
                    set.Value,
                    sggmrssWagons,
                    "AlexAfirus",
                    set.Key,
                    "{0}.xml",
                    "90' KI {0} {1} {2}",
                    "AlexAfirus\\KI Sggmrss90\\sggmrss_{3}_{2}"
                );
            }

            Console.WriteLine("Done!");
        }

        public async Task CreatePreloadBlueprint()
        {
            var fileNames = Directory.EnumerateFiles(
                Path.Combine(rwLib.TSPath, "Assets\\Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\CH-WASCO\\Alex95"),
                "*.bin",
                SearchOption.AllDirectories
            );

            var wagonsGroupedByCompant = fileNames
                 .GroupBy(x => x.Split('_')[1]);

            var provider = "Afirus";
            var product = "Sggmrss";

            foreach (var company in wagonsGroupedByCompant)
            {
                var wagons = new List<RWBlueprintID>();
                var componyL = company.ToList();

                for (var i = 0; i < componyL.Count; i++)
                {
                    var w = componyL[i];
                    if (i % 4 == 0 || i % 4 == 3)
                    {
                        var relativeFilePath = Path.GetRelativePath(Path.Combine(rwLib.TSPath, "Assets"), w);
                        wagons.Add(RWBlueprintID.FromFilenameRelativeToAssetsDirectory(relativeFilePath));
                    }
                }

                if (wagons.Count % 2 == 1)
                {
                    var relativeFilePath = Path.GetRelativePath(Path.Combine(rwLib.TSPath, "Assets"), componyL[1]);
                    wagons.Add(RWBlueprintID.FromFilenameRelativeToAssetsDirectory(relativeFilePath));
                }

                var path = System.IO.Path.ChangeExtension(System.IO.Path.Combine("Preload", company.Key), ".bin");

                var consistBlueprint = await rwLib!.BlueprintLoader.CreateConsistBlueprint(
                    provider,
                    product,
                    path,
                    wagons,
                    true
                );

                var result = await rwLib.Serializer.SerializeWithSerzExe(consistBlueprint.Xml.Document!);

                var combinedPath = consistBlueprint.BlueprintId.CombinedPath;
                var filepath = System.IO.Path.Combine(rwLib.TSPath, "Assets", combinedPath);

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath)!);
                System.IO.File.Move(result, filepath, true);
            }
        }
    }
}
