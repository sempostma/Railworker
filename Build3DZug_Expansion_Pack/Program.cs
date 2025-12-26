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
    public string brandFilter { get; set; } = "";
    public float moveX { get; set; } = 0;
    public float moveY { get; set; } = 0;
    public float moveZ { get; set; } = 0;
    public float scaleX { get; set; } = 0;
    public float scaleY { get; set; } = 0;
    public float scaleZ { get; set; } = 0;
    public int mass { get; set; } = 15000;
    public List<FileItem> cargo { get; set; } = new List<FileItem>();
}

class Program
{
    private static RWLibrary rwLib;

    static (string, string)[] wagonTypes = {
        ("BOX", "sggnss"),
        ("CZ-MT", "sggnss_CZ-MT"),
        ("D-ERSA", "sggnss_D-ERSA"),
        ("D-MT", "sggnss_D-MT"),
        ("D-SRA", "sggnss_D-SRA"),
        ("D-VTGCH", "sggnss_D-VTGCH"),
        ("SK-EXTRA", "sggnss_SK_EXRA"),
        ("SK-NACCO", "sggnss_SK_NACCO"),
        ("SK-STBAT", "sggnss_SK_STBAT"),
    };

    public static void Main(string[] args)
    {
        rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true, Logger = new Logger() });
        Run().Wait();
    }

    public static async Task Run()
    {
        await GenerateVariants();
    }

    public static async Task GenerateVariants()
    {
        bool includeAfirusContainers = true;

        try
        {
            Console.WriteLine("Generating 3DZug KI variants!");

            if (includeAfirusContainers)
            {
                Console.WriteLine("Generating Afirus variants");
                await CreateAfirusContainerCargos();
                await Create3DZugKIVariants("AfirusContainerPack.json", "Afirus", "ContainerPack01", "RailNetwork\\Cargo\\sggnss\\{0}.xml");
            }

            Console.WriteLine("Done!");
        }
        catch (Exception ex)
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
        var variants = JsonSerializer.Deserialize<List<FileItem>>(ReadFile("AfirusContainerPack.json"), options)!;
        var templateOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("AfirusCargoBlueprint.xml"));
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
                    case "RailStudio":
                        provider = "RailStudio";
                        product = "RailVehicles";
                        pathFormat = "Freight\\Cargo\\Containere\\{0}.xml";
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

            var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "Afirus", "ContainerPack01", "RailNetwork", "Cargo", "sggnss", variant.filename + ".bin");
            var tempPath = await rwLib.Serializer.SerializeWithSerzExe(template);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Move(tempPath, destinationPath, true);
        }
    }

    public static async Task Create3DZugKIVariants(string jsonFile, string provider, string product, string containerPathFormat)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var containers = JsonSerializer.Deserialize<List<FileItem>>(ReadFile(jsonFile), options);
        var templateAOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggnss.xml"));

        foreach (var (brandName, geoFile) in wagonTypes)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };
            await Parallel.ForEachAsync(containers, parallelOptions, async (container, _) =>
            {
                if (!String.IsNullOrEmpty(container.brandFilter) && container.brandFilter != brandName) return;

                string containerName = Path.GetFileNameWithoutExtension(container.filename);
                string containerNiceName = container.name;

                var template = new XDocument(templateAOrig);

                string wagonName = $"3dz KI1 Sggnss {brandName} {containerNiceName}";
                string filenamePart = containerName;
                string wagonFilename = $"sggnss_beladen_{filenamePart}_{brandName}";
                template.Descendants("Name").First().Value = wagonName;
                template.Descendants("English").First().Value = wagonName;

                var geo = "3DZUG\\3dz_KIPack\\RailVehicles\\Cargo\\[00]" + geoFile;
                template.Descendants("GeometryID").First().Value = geo;
                //template.Descendants("CollisionGeometryID").First().Value = geo;

                var cargoChildElement = template.Descendants("cEntityContainerBlueprint-sChild").Skip(1).First(x => x.Element("ChildName")!.Value == "Cargo");

                cargoChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("Provider").First().Value = provider;
                cargoChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("Product").First().Value = product;
                cargoChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, Path.GetFileNameWithoutExtension(container.filename));

                if (container.mass > 53500)
                {
                    throw new InvalidDataException("Mass cannot exceed 30480");
                }

                var xMoveAxisIdx = 12;
                applyContainerMatrixTransformation(cargoChildElement, xMoveAxisIdx, container.moveX);

                var yMoveAxis = 13;
                applyContainerMatrixTransformation(cargoChildElement, yMoveAxis, container.moveY);

                var zMoveAxisIdx = 14;
                applyContainerMatrixTransformation(cargoChildElement, zMoveAxisIdx, container.moveZ);

                var xScaleAxisIdx = 0;
                applyContainerMatrixTransformation(cargoChildElement, xScaleAxisIdx, container.scaleX);

                var yScaleAxisIdx = 5;
                applyContainerMatrixTransformation(cargoChildElement, yScaleAxisIdx, container.scaleY);

                var zScaleAxisIdx = 10;
                applyContainerMatrixTransformation(cargoChildElement, zScaleAxisIdx, container.scaleZ);

                var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "3DZUG", "3dz_KIPack", "RailVehicles", "Cargo", "[Afirus]GW", wagonFilename + ".bin");
                var tempPath = await rwLib.Serializer.SerializeWithSerzExe(template);

                File.Move(tempPath, destinationPath, true);
            });
        }
    }
}