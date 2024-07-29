using RWLib;
using RWLib.Exceptions;
using RWLib.Interfaces;
using RWLib.Packaging;
using RWLib.RWBlueprints.Components;
using RWLib.Scenario;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        private readonly List<string> companies = new() { "CH-WASCO", "NL-EUWAG", "D-AAEC", "CZ-GTS" };

        public AfirusSggmrssGenerator()
        {
            this.rwLib = new RWLibrary(new RWLibOptions { Logger = new Logger() });
        }

        public async Task GenerateVariants()
        {
            try
            {
                //await Generate45ftVariants();
                //await Generate20COILVariants();
                //await Generate24COILVariants();
                //await Generate20_FTVariants();
                //await Generate20_FT_OTVariants();
                //await GenerateWABVariants();
                //await Generate30_WABVariants();
                //await Generate24WABVariants();
                await Generate30_SILOVariants();
                await Generate20TANKVariants();
                await Generate40TANKVariants();
                await Generate782TANKVariants();
                await Generate7X5TANKVariants();
                await GenerateFlatVariants();
                //await GenerateWAB45Variants(); // This has duplicate containers which are already present in the 45ft variant
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

        public async Task CorrectGeopcdxReference()
        {
            var assetsFolder = Path.Combine(rwLib.TSPath, "Assets");
            var source = Path.Combine(assetsFolder, "Alex95", "ContainerPack01Source");
            var binFiles = Directory.EnumerateFiles(source, "*.bin", SearchOption.AllDirectories);

            List<IndexItem> files = new List<IndexItem>();

            foreach (var binFile in binFiles)
            {
                try
                {
                    var blueprint = await rwLib.BlueprintLoader.FromFilename(binFile);
                    var geometryIdElement = blueprint.Xml.Descendants("GeometryID").First();

                    var goeFilename = Path.ChangeExtension(Path.GetFileName(geometryIdElement.Value.Replace("[00]", "")), ".GeoPcDx");
                    var geoFile = Path.Combine(Path.GetDirectoryName(binFile)!, goeFilename);

                    if (File.Exists(geoFile) == false)
                    {
                        Console.WriteLine(geoFile + " does not exist.");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(geoFile.Replace("ContainerPack01Source", "ContainerPack01"))!);

                    File.Copy(geoFile, geoFile.Replace("ContainerPack01Source", "ContainerPack01"), true);

                    var geometryId = Path.GetRelativePath(assetsFolder, geoFile);
                    var geoDir = Path.GetDirectoryName(geometryId)!;
                    geometryId = Path.ChangeExtension(Path.GetFileName(geoFile), null);
                    geometryId = Path.Combine(geoDir, "[00]" + geometryId);

                    geometryIdElement.Value = geometryId.Replace("ContainerPack01Source", "ContainerPack01");

                    var decleration = new XDeclaration("1.0", "utf-8", null);
                    var doc = new XDocument(decleration);
                    var root = new XElement("cBlueprintLoader");
                    var blueprintElement = new XElement("Blueprint");
                    root.Add(blueprintElement);
                    doc.Add(root);
                    root.Add(new XAttribute(XNamespace.Xmlns + "d", RWUtils.KujuNamspace));
                    root.Add(new XAttribute(RWUtils.KujuNamspace + "version", "1.0"));

                    blueprintElement.Add(blueprint.Xml);
                    var resultTempFileName = await rwLib.Serializer.SerializeWithSerzExe(doc);

                    var destinationBinFile = binFile.Replace("ContainerPack01Source", "ContainerPack01");

                    files.Add(new IndexItem
                    {
                        BinPath = Path.GetRelativePath(source.Replace("ContainerPack01Source", "ContainerPack01"), destinationBinFile),
                        NiceName = blueprint.Xml.Descendants("Name").First().Value
                    });

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationBinFile)!);
                    File.WriteAllBytes(destinationBinFile, File.ReadAllBytes(resultTempFileName));

                    Console.WriteLine("Done: " + destinationBinFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error for " + binFile + ": " + e.ToString());
                }

            };

            File.WriteAllText(Path.Combine(source.Replace("ContainerPack01Source", "ContainerPack01"), "Index.json"), JsonSerializer.Serialize(files));

            // Copying all texture files

            foreach (var sourceTexture in Directory.EnumerateFiles(source, "*.TgPcDx", SearchOption.AllDirectories))
            {
                var destinationTexture = sourceTexture.Replace("ContainerPack01Source", "ContainerPack01");
                Directory.CreateDirectory(Path.GetDirectoryName(destinationTexture)!);
                File.Copy(sourceTexture, destinationTexture, true);
            }
        }

        private List<WagonType> CreateWagonTypes(XDocument aTemplate, XDocument bTemplate, List<String> companies, string size, MatrixTransformable? matrixTransformable = null, float bSideZOffset = 0)
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
                            GeoFileName = $"Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{componay}\\[00]sggmrss_{size.Filename().ToLower()}_{side}",
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
                            RotateY = matrixTransformable.RotateY,
                            RotateZ = matrixTransformable.RotateZ,
                        }).ToList()
                }).ToList();
        }

        private async Task Generate45ftVariants()
        {
            Console.WriteLine("Generating 45ft Variants!");

            var container45 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.45_FT.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "45", new MatrixTransformable
            {
                MoveZ = -0.415f,
                MoveY = -0.83f,
                ScaleZ = -0.01f
            });

            container45.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container45,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} C45 {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\45_FT\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate20COILVariants()
        {
            Console.WriteLine("Generating 20_COIL Variants!");

            var container2coil = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.20_COIL.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f,
                RotateY = 90f
            });

            container2coil.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container2coil,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 20 COIL {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\20_COIL\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate24COILVariants()
        {
            Console.WriteLine("Generating 24_COIL Variants!");

            var container2coil = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.24_COIL.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "21", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f,
                RotateY = 90f
            });

            container2coil.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container2coil,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 24 COIL {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\24_COIL\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate24WABVariants()
        {
            Console.WriteLine("Generating 24 WAB Variants!");

            var container2coil = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.24_WAB.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            container2coil.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container2coil,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 24 COIL {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\24_WAB\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task GenerateWABVariants()
        {
            Console.WriteLine("Generating WAB Variants!");

            var container2coil = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.WAB.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "45", new MatrixTransformable
            {
                MoveY = 1.17525f,
                MoveZ = -4.579059f,
                MoveX = -0.08f,
                RotateY = 90f
            }, bSideZOffset: 1.27731f);

            container2coil.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container2coil,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\WAB\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate20_FTVariants()
        {
            Console.WriteLine("Generating 20 FT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.20_FT.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.169f,
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 20 {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\20_FT\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate20_FT_OTVariants()
        {
            Console.WriteLine("Generating 20 FT OT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.20_OT.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.169f,
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 20 OT {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\20_OT\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate30_WABVariants()
        {
            Console.WriteLine("Generating 30 WAB Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.30_WAB.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "30", new MatrixTransformable
            {
                MoveZ = -2.348f,
                MoveY = 1.169f + 1.22f,
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 30 WAB {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\30_WAB\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate30_SILOVariants()
        {
            Console.WriteLine("Generating 30 SILO Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.30_SILO.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "30", new MatrixTransformable
            {
                MoveZ = -2.348f,
                MoveY = 0.32f,
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 30 {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\30_WAB\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate782TANKVariants()
        {
            Console.WriteLine("Generating 782 TT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.7_82_TANK.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 782 TT {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\782_TANK\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate7X5TANKVariants()
        {
            Console.WriteLine("Generating 715 TT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.7_X5_TANK.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\7X5_TANK\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate20TANKVariants()
        {
            Console.WriteLine("Generating 20 TT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.20_Tank.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 20 TT {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\Tank\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task Generate40TANKVariants()
        {
            Console.WriteLine("Generating 40 TT Variants!");

            var container20 = FileItem.FromJson(ReadFile("Malex95_ContainerPack01.40_Tank.json"));
            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons = CreateWagonTypes(aTemplate, bTemplate, companies, "40", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            container20.ForEach(x => {
                x.CargoAsChild = true;
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                container20,
                sggmrssWagons,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 40 TT {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\Tank\\sggmrss_{3}_{2}"
            );

            Console.WriteLine("Done!");
        }

        private async Task GenerateFlatVariants()
        {
            Console.WriteLine("Generating Flat Variants!");

            var aTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.A.xml"));
            var bTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss.B.xml"));

            List<WagonType> sggmrssWagons20 = CreateWagonTypes(aTemplate, bTemplate, companies, "20", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                new List<FileItem>
                {
                    new FileItem
                {
                    Filename = "RailNetwork\\Interactive\\Flat\\20FT_CRXU.bin",
                    Name = "CRXU 1",
                    FilterWagonType = [
                        "CH-WASCO"
                    ],
                    CargoAsChild = true
                }
                },
                sggmrssWagons20,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 20 F {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\20_Flat\\sggmrss_{3}_{2}"
            );

            var flats = new List<FileItem>
            {
                new FileItem
                {
                    Filename = "RailNetwork\\Interactive\\Flat\\40FT_CAIU.bin",
                    Name = "CAIU 1",
                    FilterWagonType = [
                        "CH-WASCO"
                    ],
                    CargoAsChild = true
                },
                new FileItem
                {
                    Filename = "RailNetwork\\Interactive\\Flat\\40FT_SANU.bin",
                    Name = "SANU 1",
                    FilterWagonType = [
                        "CH-WASCO"
                    ],
                    CargoAsChild = true
                }
            };

            List<WagonType> sggmrssWagons40 = CreateWagonTypes(aTemplate, bTemplate, companies, "40", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                flats,              
                sggmrssWagons40,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 40 F {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\40_Flat\\sggmrss_{3}_{2}"
            );

            List<WagonType> sggmrssWagon7150 = CreateWagonTypes(aTemplate, bTemplate, companies, "21", new MatrixTransformable
            {
                MoveZ = -1.73192f + -3.048f,
                MoveY = 1.4f + 0.065f,
                RotateY = 90f
            });

            await rwLib.RWVariantGenerator.CreateVariants(
                new List<FileItem>
                {
                new FileItem
                {
                    Filename = "RailNetwork\\Interactive\\Flat\\7_15FT_NIZZ.bin",
                    Name = "NIZZ 1",
                    FilterWagonType = [
                        "CH-WASCO"
                    ],
                    CargoAsChild = true
                }
                },
                sggmrssWagons20,
                "Alex95",
                "ContainerPack01",
                "{0}.xml",
                "90' KI {0} 715 F {1} {2}",
                "Afirus\\Sggmrss\\RailVehicles\\Freight\\Default\\{0}\\Alex95\\715_Flat\\sggmrss_{3}_{2}"
            );

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
