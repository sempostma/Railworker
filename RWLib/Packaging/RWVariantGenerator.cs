using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RWLib.Packaging
{
    public class RWVariantGenerator : RWLibraryDependent
    {
        private RWSerializer serializer;

        public RWVariantGenerator(RWLibrary rWLib, RWSerializer serializer) : base(rWLib)
        {
            this.serializer = serializer;
        }

        public void ApplyMatrixTransformation(XDocument xmlDoc, int matrixIdx, float deltaAmount)
        {
            var oldXValue = float.Parse(xmlDoc.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[matrixIdx].Value, CultureInfo.InvariantCulture);
            var newXValue = (oldXValue + deltaAmount).ToString(CultureInfo.InvariantCulture);
            xmlDoc.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[matrixIdx].Value = newXValue;
        }

        public void ApplyContainerMatrixTransformation(XElement child, int matrixIdx, float deltaAmount)
        {
            var oldXValue = float.Parse(child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value, CultureInfo.InvariantCulture);
            var newXValue = (oldXValue + deltaAmount).ToString(CultureInfo.InvariantCulture);
            child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value = newXValue;
        }

        public async Task CreateCargoBlueprints(List<FileItem> variants, XDocument cargoBlueprint, string destinationPathFormat)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var childOrig = cargoBlueprint.Descendants("cEntityContainerBlueprint-sChild").First()!;
            childOrig.Remove();

            foreach (var variant in variants)
            {
                var template = new XDocument(cargoBlueprint);

                template.Descendants("Name").First().Value = variant.Name;
                template.Descendants("English").First().Value = variant.Name;

                var children = template.Descendants("cEntityContainerBlueprint").First().Descendants("Children").First();

                foreach (var cargo in variant.Cargo)
                {
                    var type = cargo.Filename.Split('/')[0];
                    string product = "";
                    string provider = "";
                    string pathFormat = "";
                    string filename = String.Join('\\', cargo.Filename.Split('/').Skip(1));

                    switch (type)
                    {
                        case "Afirus":
                            provider = "Afirus";
                            product = "ContainerPack";
                            pathFormat = "RailNetwork\\Interactive\\{0}.xml";
                            break;
                        case "NewS":
                            provider = "Kuju";
                            product = "RailSimulator";
                            pathFormat = "RailNetwork\\Interactive\\{0}.xml";
                            break;
                        case "RSItalia":
                            provider = "RSItalia";
                            product = "Addon";
                            pathFormat = "RailNetwork\\interactive\\container\\{0}.xml";
                            break;
                        default:
                            throw new Exception("Unknown format");
                    }

                    var child = new XElement(childOrig);

                    child.Descendants("ChildName").First().Value = cargo.Name;

                    child.Descendants("Provider").First().Value = provider;
                    child.Descendants("Product").First().Value = product;
                    child.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(pathFormat, Path.ChangeExtension(filename, null));

                    var xMoveAxisIdx = 12;
                    ApplyContainerMatrixTransformation(child, xMoveAxisIdx, cargo.MoveX);

                    var yMoveAxis = 13;
                    ApplyContainerMatrixTransformation(child, yMoveAxis, cargo.MoveY);

                    var zMoveAxisIdx = 14;
                    ApplyContainerMatrixTransformation(child, zMoveAxisIdx, cargo.MoveZ);

                    var xScaleAxisIdx = 0;
                    ApplyContainerMatrixTransformation(child, xScaleAxisIdx, cargo.ScaleX);

                    var yScaleAxisIdx = 5;
                    ApplyContainerMatrixTransformation(child, yScaleAxisIdx, cargo.ScaleY);

                    var zScaleAxisIdx = 10;
                    ApplyContainerMatrixTransformation(child, zScaleAxisIdx, cargo.ScaleZ);

                    children.Add(child);
                }

                var destinationPath = String.Format(destinationPathFormat, variant.Filename);
                var tempPath = await rWLib.Serializer.SerializeWithSerzExe(template);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Move(tempPath, destinationPath, true);
            }
        }

        public async Task CreateVariants(List<FileItem> variants, List<WagonType> wagonTypes, string provider, string product, string containerPathFormat, string wagonNameFormat, string destinationPathFormat)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            foreach (var wagonType in wagonTypes)
            {
                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = 10
                };

                var filteredVariants = variants.Where(v => v.FilterWagonType.Count == 0 || v.FilterWagonType.Contains(wagonType.Type));

                await Parallel.ForEachAsync(filteredVariants, parallelOptions, async (container, _) =>
                {
                    string containerName = Path.GetFileNameWithoutExtension(container.Filename);
                    string containerNiceName = container.Name;

                    foreach (var t in wagonType.BlueprintTemplates)
                    {
                        var xml = new XDocument(t.XDocument);
                        var label = t.Label;

                        string wagonName = String.Format(wagonNameFormat, wagonType.Type, containerNiceName, label);
                        string filenamePart = String.IsNullOrWhiteSpace(containerName) ? "LEER" : containerName;
                        xml.Descendants("Name").First().Value = wagonName;
                        xml.Descendants("English").First().Value = wagonName;

                        xml.Descendants("GeometryID").First().Value = t.GeoFileName;
                        xml.Descendants("CollisionGeometryID").First().Value = t.GeoFileName;

                        xml.Descendants("CargoBlueprintID").First().Descendants("Provider").First().Value = provider;
                        xml.Descendants("CargoBlueprintID").First().Descendants("Product").First().Value = product;
                        xml.Descendants("CargoBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, Path.Combine(Path.GetDirectoryName(container.Filename) ?? "", containerName));

                        if (container.Mass > 53500)
                        {
                            throw new InvalidDataException("Mass cannot exceed 30480");
                        }

                        xml.Descendants("cContainerCargoDef").First().Descendants("MassInKg").First().Value = container.Mass.ToString(CultureInfo.InvariantCulture);

                        var xMoveAxisIdx = 12;
                        ApplyMatrixTransformation(xml, xMoveAxisIdx, container.MoveX);

                        var yMoveAxis = 13;
                        ApplyMatrixTransformation(xml, yMoveAxis, container.MoveY);

                        var zMoveAxisIdx = 14;
                        ApplyMatrixTransformation(xml, zMoveAxisIdx, container.MoveZ);

                        var xScaleAxisIdx = 0;
                        ApplyMatrixTransformation(xml, xScaleAxisIdx, container.ScaleX);

                        var yScaleAxisIdx = 5;
                        ApplyMatrixTransformation(xml, yScaleAxisIdx, container.ScaleY);

                        var zScaleAxisIdx = 10;
                        ApplyMatrixTransformation(xml, zScaleAxisIdx, container.ScaleZ);

                        var destinationPath = String.Format(destinationPathFormat, wagonType.Type, filenamePart, label) + ".bin";
                        destinationPath = Path.Combine(rWLib.TSPath, "Assets", destinationPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        var tempPath = await rWLib.Serializer.SerializeWithSerzExe(xml);

                        File.Move(tempPath, destinationPath, true);
                    }
                });
            }
        }


    }
}
