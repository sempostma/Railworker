// See https://aka.ms/new-console-template for more information
using RWLib;
using RWLib.Interfaces;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;


class Logger : IRWLogger
{
    public void Log(RWLogType type, string message)
    {
        if (type == RWLogType.Verbose) return;
        Console.WriteLine($"[{type.ToString()}] ${message}");
    }
}

public class FileItem
{
    public string filename { get; set; } = "";
    public string name { get; set; } = "";
    public float moveX { get; set; } = 0;
    public float moveY { get; set; } = 0;
    public float moveZ { get; set; } = 0;
    public float scaleX { get; set; } = 0;
    public float scaleY { get; set; } = 0;
    public float scaleZ { get; set; } = 0;
    public int mass { get; set; } = 15000;
    public List<FileItem> cargo { get; set; } = new List<FileItem>();
}

class WagonType
{
    public string Type { get; set; } = "";
    public string InGameType { get; set; } = "";
    // you normally only need to pass in 1. Except when you compile a wagon type like sggrss, which has 2 geo types because it has 2 variants (A and B)
    public List<string> GeoFileNames { get; set; } = new List<string>();
    public bool KeepAutoNumbering { get; set; } = false;
}

class Program
{
    private static RWLibrary rwLib;

    static (string, string, string, string)[] dtgWagonTypes = {
        ("DB", "DB", "SGGRSS_A", "SGGRSS_B"),
        //("Blue", "Blue", "SGGRSS_A_blue", "SGGRSS_B_blue"),
        ("Rusty", "Rusty", "SGGRSS_A_rusty", "SGGRSS_B_rusty"),
        //("Orange", "Orange", "SGGRSS_A_orange", "SGGRSS_B_orange"),
    };

    static (string, string, string, string, bool)[] wzWagonTypes = {
        ("Blue", "CBRail", "SGGRSS_A", "SGGRSS_B", true),
        ("GATX", "GATX", "SGGRSS_A", "SGGRSS_B", true),
        ("Grey", "Ermewa", "SGGRSS_A", "SGGRSS_B", true),
        ("Orange", "Wascosa", "SGGRSS_A", "SGGRSS_B", true),
        ("DB", "DB", "SGGRSS_A", "SGGRSS_B", true),
        ("Rusty", "Rusty", "SGGRSS_A", "SGGRSS_B", false),
    };

    public static void Main(string[] args)
    {
        rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true, Logger = new Logger() });
        Run().Wait();
    }

    public static async Task Run()
    {
        await GenerateWaggonZVariants();
    }

    public static async Task GenerateWaggonZVariants()
    {
        bool includeRSitalia = false;
        bool includeNewSContainers = true;
        bool includeRailStudioRgsVariants = false;
        bool includeExtraWaggonzContainers = false;
        bool includeEmptyVariant = false;
        bool includeWaggonzDefaultContainers = false;
        bool includeScandanavian = false;
        bool includeAfirusContainers = false;

        try
        {
            Console.WriteLine("Generating WaggonZ variants!");

            // For newS containers
            if (includeNewSContainers)
            {
                Console.WriteLine("Generating NewS variants");
                await CreateWaggonZVariants("WaggonZ.NewSContainers.json", "Kuju", "RailSimulator", "RailNetwork\\Interactive\\{0}.xml");
            }

            // For RSItalia containers
            if (includeRSitalia)
            {
                Console.WriteLine("Generating RSItalia variants");
                await CreateWaggonZVariants("WaggonZ.RSItaliaContainers.json", "RSItalia", "Addon", "RailNetwork\\interactive\\container\\{0}.xml");
            }

            // SGGRSS Extra wagons expansion
            if (includeExtraWaggonzContainers)
            {
                Console.WriteLine("Generating WaggonZ variants");
                await CreateWaggonZVariants("WaggonZ.WaggonzExtraContainerPack.json", "CW", "BLS - Lotschbergbahn", "Scenery\\Misc\\{0}.xml");
            }

            // Scandanavian extra wagons (poor quality)
            if (includeScandanavian)
            {
                Console.WriteLine("Generating Scandanavian variants");
                await CreateWaggonZVariants("WaggonZ.OcvnContainers.json", "Ocvn", "Containers", "RailNetwork\\{0}.xml");
            }

            // SGGRSS WaggonZ default wagons expansion
            if (includeWaggonzDefaultContainers)
            {
                Console.WriteLine("Generating WaggonZ Default variants");
                await CreateWaggonZVariants("WaggonZ.WaggonzDefaultContainers.json", "Waggonz", "Addon", "RailNetwork\\Interactive\\{0}.xml");
            }

            // SGGRSS Railstudio Rgs containers
            if (includeRailStudioRgsVariants)
            {
                Console.WriteLine("Generating RailStudio Rgs variants");
                await CreateWaggonZVariants("WaggonZ.RailStudioRgsContainers.json", "RailStudio", "RailVehicles", "Freight\\Cargo\\Containere\\{0}.xml");
            }

            if (includeEmptyVariant)
            {
                Console.WriteLine("Generating Empty variants");
                await CreateWaggonZVariants("WaggonZ.EmptyVariant.json", "", "", "");
            }

            if (includeAfirusContainers)
            {
                Console.WriteLine("Generating Afirus variants");
                await CreateAfirusContainerCargos();
                await CreateWaggonZVariants("WaggonZ.AfirusContainerPack.json", "Afirus", "ContainerPack", "RailNetwork\\Cargo\\sggrss\\{0}.xml");
            }

            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public static async Task GenerateDTGVariants()
    {
        bool includeRSitalia = false;
        bool includeNewSContainers = false;
        bool includeWaggonzContainers = false;
        bool includeWaggonzDefaultContainers = true;
        bool includeEmptyVariant = false;

        try
        {
            Console.WriteLine("Hello, World!");

            // For newS containers
            if (includeNewSContainers)
            {
                Console.WriteLine("Generating NewS variants");
                await CreateDTGVariants("DTG.NewSContainers.json", "Kuju", "RailSimulator", "RailNetwork\\Interactive\\{0}.xml");
            }

            // For RSItalia containers
            if (includeRSitalia)
            {
                Console.WriteLine("Generating RSItalia variants");
                await CreateDTGVariants("DTG.RSItaliaContainers.json", "RSItalia", "Addon", "RailNetwork\\interactive\\container\\{0}.xml");
            }

            // SGGRSS Extra wagons expansion
            if (includeWaggonzContainers)
            {
                Console.WriteLine("Generating WaggonZ variants");
                await CreateDTGVariants("DTG.WaggonzExtraContainerPack.json", "CW", "BLS - Lotschbergbahn", "Scenery\\Misc\\{0}.xml");
            }

            // SGGRSS default Waggonz wagons expansion
            if (includeWaggonzDefaultContainers)
            {
                Console.WriteLine("Generating WaggonZ Default variants");
                await CreateDTGVariants("DTG.WaggonzDefaultContainers.json", "Waggonz", "Addon", "RailNetwork\\Interactive\\{0}.xml");
            }

            if (includeEmptyVariant) 
            {
                Console.WriteLine("Generating Empty variants");
                await CreateDTGVariants("DTG.EmptyVariant.json", "", "", "");
            }

            Console.WriteLine("Done!");
        } catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public static String ReadFile(String embeddedResource)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var files = assembly.GetManifestResourceNames();
        var resource = assembly.GetName().Name + ".Resources." + embeddedResource;
        var stream = assembly.GetManifestResourceStream(resource);
        if (stream == null) throw new FileNotFoundException($"Unable to get embedded resource {resource}. All files: ${files.ToArray()}");
        using (StreamReader reader = new StreamReader(stream))
        {
            string file = reader.ReadToEnd(); //Make string equal to full file
            return file;
        }
    }

    public static void applyMatrixTransformation(XDocument xmlDoc, int matrixIdx, float deltaAmount)
    {
        var oldXValue = float.Parse(xmlDoc.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[matrixIdx].Value, CultureInfo.InvariantCulture);
        var newXValue = (oldXValue + deltaAmount).ToString(CultureInfo.InvariantCulture);
        xmlDoc.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[matrixIdx].Value = newXValue;
    }

    public static void applyContainerMatrixTransformation(XElement child, int matrixIdx, float deltaAmount)
    {
        var oldXValue = float.Parse(child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value, CultureInfo.InvariantCulture);
        var newXValue = (oldXValue + deltaAmount).ToString(CultureInfo.InvariantCulture);
        child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value = newXValue;
    }

    public static async Task CreateAfirusContainerCargos()
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var variants = JsonSerializer.Deserialize<List<FileItem>>(ReadFile("WaggonZ.AfirusContainerPack.json"), options)!;
        var templateOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("WaggonZ.AfirusCargoBlueprint.xml"));
        var childOrig = templateOrig.Descendants("cEntityContainerBlueprint-sChild").First()!;
        childOrig.Remove();

        foreach (var variant in variants)
        {
            var template = new XDocument(templateOrig);

            template.Descendants("Name").First().Value = variant.name;
            template.Descendants("English").First().Value = variant.name;

            var children = template.Descendants("cEntityContainerBlueprint").First().Descendants("Children").First();

            foreach (var cargo in variant.cargo)
            {
                var type = cargo.filename.Split('/')[0];
                string product = "";
                string provider = "";
                string pathFormat = "";
                string filename = String.Join('\\', cargo.filename.Split('/').Skip(1));

                switch(type)
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

                child.Descendants("ChildName").First().Value = cargo.name;

                child.Descendants("Provider").First().Value = provider;
                child.Descendants("Product").First().Value = product;
                child.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(pathFormat, Path.ChangeExtension(filename, null));

                var xMoveAxisIdx = 12;
                applyContainerMatrixTransformation(child, xMoveAxisIdx, cargo.moveX);

                var yMoveAxis = 13;
                applyContainerMatrixTransformation(child, yMoveAxis, cargo.moveY);

                var zMoveAxisIdx = 14;
                applyContainerMatrixTransformation(child, zMoveAxisIdx, cargo.moveZ);

                var xScaleAxisIdx = 0;
                applyContainerMatrixTransformation(child, xScaleAxisIdx, cargo.scaleX);

                var yScaleAxisIdx = 5;
                applyContainerMatrixTransformation(child, yScaleAxisIdx, cargo.scaleY);

                var zScaleAxisIdx = 10;
                applyContainerMatrixTransformation(child, zScaleAxisIdx, cargo.scaleZ);

                children.Add(child);
            }

            var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "Afirus", "ContainerPack", "RailNetwork", "Cargo", "sggrss", variant.filename + ".bin");
            var tempPath = await rwLib.Serializer.SerializeWithSerzExe(template);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Move(tempPath, destinationPath, true);
        }
    }

    public static async Task CreateWaggonZVariants(string jsonFile, string provider, string product, string containerPathFormat)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var containers = JsonSerializer.Deserialize<List<FileItem>>(ReadFile(jsonFile), options);
        var templateAOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("WaggonZ.SGGRSS_A.xml"));
        var templateBOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("WaggonZ.SGGRSS_B.xml"));

        foreach (var (wagonType, inGameType, a, b, keepAutoNumbering) in wzWagonTypes)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };
            await Parallel.ForEachAsync(containers, parallelOptions, async (container, _) =>
            {
                string containerName = Path.GetFileNameWithoutExtension(container.filename);
                string containerNiceName = container.name;

                var templateA = new XDocument(templateAOrig);
                var templateB = new XDocument(templateBOrig);

                foreach (var t in new[] { ("A", templateA), ("B", templateB) })
                {
                    string wagonName = $"SGGRSS-{wagonType}-{containerNiceName}-{t.Item1}";
                    string filenamePart = String.IsNullOrWhiteSpace(containerName) ? "LEER" : containerName;
                    string wagonFilename = $"[Afirus] SGGRSS-{wagonType}-{filenamePart}-{t.Item1}";
                    t.Item2.Descendants("Name").First().Value = wagonName;
                    t.Item2.Descendants("English").First().Value = wagonName;

                    var geo = "Waggonz\\Addon\\RailVehicles\\Freight\\sggrss\\" + wagonType + "\\[00]" + (t.Item1 == "A" ? a : b);
                    t.Item2.Descendants("GeometryID").First().Value = geo;
                    t.Item2.Descendants("CollisionGeometryID").First().Value = geo;

                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("Provider").First().Value = provider;
                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("Product").First().Value = product;
                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, containerName);

                    if (container.mass > 53500)
                    {
                        throw new InvalidDataException("Mass cannot exceed 30480");
                    }

                    t.Item2.Descendants("cContainerCargoDef").First().Descendants("MassInKg").First().Value = container.mass.ToString(CultureInfo.InvariantCulture);

                    var xMoveAxisIdx = 12;
                    applyMatrixTransformation(t.Item2, xMoveAxisIdx, container.moveX);

                    var yMoveAxis = 13;
                    applyMatrixTransformation(t.Item2, yMoveAxis, container.moveY);

                    var zMoveAxisIdx = 14;
                    applyMatrixTransformation(t.Item2, zMoveAxisIdx, container.moveZ);

                    var xScaleAxisIdx = 0;
                    applyMatrixTransformation(t.Item2, xScaleAxisIdx, container.scaleX);

                    var yScaleAxisIdx = 5;
                    applyMatrixTransformation(t.Item2, yScaleAxisIdx, container.scaleY);

                    var zScaleAxisIdx = 10;
                    applyMatrixTransformation(t.Item2, zScaleAxisIdx, container.scaleZ);

                    var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "Waggonz", "Addon", "RailVehicles", "Freight", "sggrss", wagonType, wagonFilename + ".bin");
                    var tempPath = await rwLib.Serializer.SerializeWithSerzExe(t.Item2);

                    File.Move(tempPath, destinationPath, true);
                }
            });
        }
    }

    public static async Task CreateDTGVariants(string jsonFile, string provider, string product, string containerPathFormat)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var containers = JsonSerializer.Deserialize<List<FileItem>>(ReadFile(jsonFile), options);
        var templateAOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("DTG.SGGRSS_A.xml"));
        var templateBOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("DTG.SGGRSS_B.xml"));

        foreach (var (wagonType, inGameType, a, b) in dtgWagonTypes)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };
            await Parallel.ForEachAsync(containers, parallelOptions, async (container, _) => 
            {
                string containerName = Path.GetFileNameWithoutExtension(container.filename);
                string containerNiceName = container.name;

                var templateA = new XDocument(templateAOrig);
                var templateB = new XDocument(templateBOrig);

                foreach (var t in new[] { ("A", templateA), ("B", templateB) })
                {
                    string wagonName = $"SGGRSS {inGameType} {containerNiceName} {t.Item1}";
                    string wagonFilename = $"[Afirus] {wagonName}";
                    t.Item2.Descendants("Name").First().Value = wagonName;
                    t.Item2.Descendants("English").First().Value = wagonName;

                    var geo = "DTG\\CologneKoblenz\\RailVehicles\\Freight\\SGGRSS\\[00]" + (t.Item1 == "A" ? a : b);
                    t.Item2.Descendants("GeometryID").First().Value = geo;
                    t.Item2.Descendants("CollisionGeometryID").First().Value = geo;

                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("Provider").First().Value = provider;
                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("Product").First().Value = product;
                    t.Item2.Descendants("CargoBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, containerName);

                    var yAxisIdx = 14;
                    var oldYValue = float.Parse(t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[yAxisIdx].Value, CultureInfo.InvariantCulture);
                    var newYValue = (oldYValue + container.moveZ).ToString(CultureInfo.InvariantCulture);
                    t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[yAxisIdx].Value = newYValue;

                    var xAxisIdx = 12;
                    var oldXValue = float.Parse(t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[xAxisIdx].Value, CultureInfo.InvariantCulture);
                    var newXValue = (oldXValue + container.moveX).ToString(CultureInfo.InvariantCulture);
                    t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[xAxisIdx].Value = newXValue;

                    var xScaleAxisIdx = 0;
                    var oldXScaleValue = float.Parse(t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[xScaleAxisIdx].Value, CultureInfo.InvariantCulture);
                    var newXScaleValue = (oldXScaleValue + container.scaleX).ToString(CultureInfo.InvariantCulture);
                    t.Item2.Descendants("cContainerCargoDef").First().Descendants("Element").First().Elements().ToArray()[xScaleAxisIdx].Value = newXScaleValue;

                    var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "DTG", "CologneKoblenz", "RailVehicles", "Freight", "SGGRSS", wagonFilename + ".bin");
                    var tempPath = await rwLib.Serializer.SerializeWithSerzExe(t.Item2);

                    File.Move(tempPath, destinationPath, true);
                }
            });
        }
    }

}