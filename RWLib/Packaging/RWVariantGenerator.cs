using ProjNet.CoordinateSystems.Transformations;
using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
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

        public Matrix4x4 ReadMatrix(XElement matrixRoot)
        {
            var components = matrixRoot.Elements().Select(x => float.Parse(x.Value, CultureInfo.InvariantCulture)).ToArray();
            return new Matrix4x4(
                    components[0],
                    components[1],
                    components[2],
                    components[3],
                    components[4],
                    components[5],
                    components[6],
                    components[7],
                    components[8],
                    components[9],
                    components[10],
                    components[11],
                    components[12],
                    components[13],
                    components[14],
                    components[15]
            );
        }

        public void ApplyMatrix(XElement matrixRoot, Matrix4x4 matrix)
        {
            var components = matrixRoot.Elements().ToArray();
            components[0].Value = matrix.M11.ToString("F", CultureInfo.InvariantCulture);
            components[1].Value = matrix.M12.ToString("F", CultureInfo.InvariantCulture);
            components[2].Value = matrix.M13.ToString("F", CultureInfo.InvariantCulture);
            components[3].Value = matrix.M14.ToString("F", CultureInfo.InvariantCulture);
            components[4].Value = matrix.M21.ToString("F", CultureInfo.InvariantCulture);
            components[5].Value = matrix.M22.ToString("F", CultureInfo.InvariantCulture);
            components[6].Value = matrix.M23.ToString("F", CultureInfo.InvariantCulture);
            components[7].Value = matrix.M24.ToString("F", CultureInfo.InvariantCulture);
            components[8].Value = matrix.M31.ToString("F", CultureInfo.InvariantCulture);
            components[9].Value = matrix.M32.ToString("F", CultureInfo.InvariantCulture);
            components[10].Value = matrix.M33.ToString("F", CultureInfo.InvariantCulture);
            components[11].Value = matrix.M34.ToString("F", CultureInfo.InvariantCulture);
            components[12].Value = matrix.M41.ToString("F", CultureInfo.InvariantCulture);
            components[13].Value = matrix.M42.ToString("F", CultureInfo.InvariantCulture);
            components[14].Value = matrix.M43.ToString("F", CultureInfo.InvariantCulture);
            components[15].Value = matrix.M44.ToString("F", CultureInfo.InvariantCulture);
        }

        public void RunMatrixTransformations(XElement matrixRoot, MatrixTransformable item)
        {
            var matrix = ReadMatrix(matrixRoot);

            matrix = matrix

                 * Matrix4x4.CreateScale(1 + item.ScaleX, 1 + item.ScaleY, 1 + item.ScaleZ)
                 * Matrix4x4.CreateRotationX(item.RotateX * MathF.PI / 180.0f)
                 * Matrix4x4.CreateRotationY(item.RotateY * MathF.PI / 180.0f)
                 * Matrix4x4.CreateRotationZ(item.RotateZ * MathF.PI / 180.0f)
                 * Matrix4x4.CreateTranslation(item.MoveX, item.MoveY, item.MoveZ);

            ApplyMatrix(matrixRoot, matrix);
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
                            product = "ContainerPack01";
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

                    var matrixRoot = child.Descendants("Element").First();
                    RunMatrixTransformations(matrixRoot, cargo);

                    children.Add(child);
                }

                var destinationPath = String.Format(destinationPathFormat, variant.Filename);
                var tempPath = await rWLib.Serializer.SerializeWithSerzExe(template);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(tempPath, destinationPath, true); // use copy to enable cache
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
                    try
                    {
                        string containerFilename = Path.GetFileNameWithoutExtension(container.Filename);
                        string containerNiceName = container.Name;

                        foreach (var t in wagonType.BlueprintTemplates)
                        {
                            var xml = new XDocument(t.XDocument);
                            var label = t.Label;

                            string wagonName = String.Format(wagonNameFormat, wagonType.InGameType, containerNiceName, label);
                            string filenamePart = String.IsNullOrWhiteSpace(containerFilename) ? "LEER" : containerFilename;
                            xml.Descendants("Name").First().Value = wagonName;
                            xml.Descendants("English").First().Value = wagonName;

                            xml.Descendants("GeometryID").First().Value = t.GeoFileName;
                            //xml.Descendants("CollisionGeometryID").First().Value = t.GeoFileName;

                            if (!String.IsNullOrWhiteSpace(container.AutoNumber))
                            {
                                xml.Descendants("CsvFile").First().Value = container.AutoNumber;
                            }

                            int childId = 1300;

                            if (container.CargoAsChild)
                            {
                                // all cargos together
                                var cargos = container.Cargo.Count > 0 ? container.Cargo : [container];
                                var children = xml.Descendants("cEntityContainerBlueprint").First().Descendants("Children").First();

                                foreach (var cargo in cargos)
                                {
                                    string containerFilenameC = Path.GetFileNameWithoutExtension(cargo.Filename);
                                    var path = String.Format(containerPathFormat, Path.Combine(Path.GetDirectoryName(cargo.Filename) ?? "", containerFilenameC));

                                    var child = CreateChildTemplateXMl(
                                        childId++,
                                        String.IsNullOrEmpty(cargo.ChildName) ? cargo.Name : cargo.ChildName,
                                        new RWBlueprintID(
                                            provider,
                                            product,
                                            path
                                        )
                                    );

                                    var matrixRoot = child.Descendants("Element").First();
                                    RunMatrixTransformations(matrixRoot, cargo.AddTransformable(t).InvertZAxis(t.InvertZ));

                                    var massElement = xml.Descendants("RailVehicleComponent").First().Descendants("Mass").First();

                                    var massInTons = float.Parse(massElement.Value, CultureInfo.InvariantCulture) + (container.Mass / 1000);

                                    massElement.Value = massInTons.ToString(CultureInfo.InvariantCulture);

                                    children.Add(child);
                                }
                            }
                            else
                            {
                                xml.Descendants("CargoBlueprintID").First().Descendants("Provider").First().Value = provider;
                                xml.Descendants("CargoBlueprintID").First().Descendants("Product").First().Value = product;
                                xml.Descendants("CargoBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, Path.Combine(Path.GetDirectoryName(container.Filename) ?? "", containerFilename));

                                if (container.Mass > 53500)
                                {
                                    throw new InvalidDataException("Mass cannot exceed 30480");
                                }

                                xml.Descendants("cContainerCargoDef").First().Descendants("MassInKg").First().Value = container.Mass.ToString(CultureInfo.InvariantCulture);

                                var matrixRoot = xml.Descendants("cContainerCargoDef").First().Descendants("Element").First();
                                RunMatrixTransformations(matrixRoot, container.AddTransformable(t).InvertZAxis(t.InvertZ));
                            }

                            var niceNameFileSystemFriendly = containerNiceName.Replace(' ', '_').ToLower();
                            var labelLowerCase = label.ToLower();
                            var destinationPath = String.Format(destinationPathFormat, wagonType.Type, filenamePart, label.ToLower(), niceNameFileSystemFriendly) + ".bin";
                            destinationPath = Path.Combine(rWLib.TSPath, "Assets", destinationPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            var tempPath = await rWLib.Serializer.SerializeWithSerzExe(xml);

                            try
                            {
                                File.Copy(tempPath, destinationPath, true); // Use copy to enable cache
                            } catch(Exception ex)
                            {
                                rWLib.options.Logger.Log(RWLib.Interfaces.RWLogType.Error, ex.ToString() + ": " + tempPath + " -> " + destinationPath);
                            }
                        }
                    } catch(Exception ex)
                    {
                        rWLib.options.Logger.Log(RWLib.Interfaces.RWLogType.Error, ex.ToString());
                    }
                });
            }
        }

        private static IEnumerable<XElement> CreateMatrixComponentsXML()
        {
            for (int i = 0; i < 16; i++)
            {
                string value = "0";
                if (i == 0 || i == 5 || i == 10 || i == 15) value = "1";
                yield return new XElement("e",
                                new XAttribute(RWUtils.KujuNamspace + "type", "sFloat32"),
                                new XAttribute(RWUtils.KujuNamspace + "precision", "string"),
                                value
                            );
            }
        }

        public static XElement CreateChildTemplateXMl(int childId, string name, RWBlueprintID blueprint)
        {
            return new XElement("cEntityContainerBlueprint-sChild",
                new XAttribute(RWUtils.KujuNamspace + "id", childId),
                new XElement("ChildName",
                    new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"),
                    name
                ),
                new XElement("BlueprintID",
                    new XElement("iBlueprintLibrary-cAbsoluteBlueprintID",
                        new XElement("BlueprintSetID",
                            new XElement("iBlueprintLibrary-cBlueprintSetID",
                                new XElement("Provider",
                                    new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"), blueprint.Provider),
                                new XElement("Product",
                                    new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"), blueprint.Product)
                            )
                        ),
                        new XElement("BlueprintID",
                            new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"), blueprint.Path)
                    )
                ),
                new XElement("Matrix",
                    new XElement("cHcRMatrix4x4",
                        new XElement("Element",
                            CreateMatrixComponentsXML()
                        )
                    )
                ),
                new XElement("ParentNodeName",
                    new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"),
                    ""
                )
            );
        }
    }
}
