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

class Program
{
    private static RWLibrary rwLib;

    static (string, string, string)[] wagonTypes = {
        ("Beige", "Sggmrss_Back_Beige", "Sggmrss_Front_Beige"),
        ("Braun", "Sggmrss_Back_Braun", "Sggmrss_Front_Braun"),
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
        try
        {
            Console.WriteLine("Generating Afirus 45ft HC PW Variants");

            await CreateVariants("Afirus_Containers.json", "Afirus", "ContainerPack", "RailNetwork\\Interactive\\45ft_hc_pw\\{0}.xml", "Assets\\VirtualRailroads\\vR_Sggmrss\\Frachtw\\[Afirus]GW\\{0}", "45ft C");

            Console.WriteLine("Generating Cargo Blueprints");

            await CreateAfirusContainerCargos();

            Console.WriteLine("Generating 20ftx2 variants");

            await CreateVariants("Afirus_Containers_20ftx2.json", "Afirus", "ContainerPack", "RailNetwork\\Cargo\\sggmrss\\{0}.xml", "Assets\\VirtualRailroads\\vR_Sggmrss\\Frachtw\\[Afirus]GW\\{0}", "20ft x2");

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
        var child = xmlDoc.Descendants("cEntityContainerBlueprint-sChild").Where(child => child.Element("ChildName")?.Value == "Main").First();
        var oldXValue = float.Parse(child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value, CultureInfo.InvariantCulture);
        var newXValue = (oldXValue + deltaAmount).ToString(CultureInfo.InvariantCulture);
        child.Descendants("Element").First().Elements().ToArray()[matrixIdx].Value = newXValue;
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
        var variants = JsonSerializer.Deserialize<List<FileItem>>(ReadFile("Afirus_Containers_20ftx2.json"), options)!;
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

            var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "Afirus", "ContainerPack", "RailNetwork", "Cargo", "sggmrss", variant.filename + ".bin");
            var tempPath = await rwLib.Serializer.SerializeWithSerzExe(template);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Move(tempPath, destinationPath, true);
        }
    }

    public static async Task CreateVariants(string jsonFile, string provider, string product, string containerPathFormat, string destinationPathFormat, string label)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var containers = JsonSerializer.Deserialize<List<FileItem>>(ReadFile(jsonFile), options);
        var templateBackOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss_Back.xml"));
        var templateFrontOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("Sggmrss_Front.xml"));

        foreach (var (wagonType, backGeo, frontGeo) in wagonTypes)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };
            await Parallel.ForEachAsync(containers, parallelOptions, async (container, _) =>
            {
                string containerName = container.name.Replace(' ', '_');
                string containerNiceName = container.name;

                var templateA = new XDocument(templateBackOrig);
                var templateB = new XDocument(templateFrontOrig);

                foreach (var t in new[] { ("Back", templateA), ("Front", templateB) })
                {
                    string side = (t.Item1 == "Back" ? "A" : "B");
                    string wagonName = $"vR Sggmrss [{label} {wagonType} {containerNiceName} {side}] by Afirus";
                    string wagonFilename = $"Sggmrss_{t.Item1}_{wagonType}_{label}_{containerName}".Replace(' ', '_');
                    t.Item2.Descendants("Name").First().Value = wagonName;

                    foreach (var translation in t.Item2.Descendants("Localisation-cUserLocalisedString").First().Descendants())
                    {
                        if (translation.Name == "Other") continue;
                        if (translation.Name == "Key") continue;
                        translation.Value = wagonName;
                    }

                    var geo = "virtualRailroads\\vR_Sggmrss\\Frachtw\\[00]" + (t.Item1 == "Back" ? backGeo : frontGeo);
                    t.Item2.Descendants("GeometryID").First().Value = geo;
                    t.Item2.Descendants("CollisionGeometryID").First().Value = geo;

                    var secondChildElement = t.Item2.Descendants("cEntityContainerBlueprint-sChild").First(x => x.Element("ChildName").Value == "Main");

                    secondChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("Provider").First().Value = provider;
                    secondChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("Product").First().Value = product;
                    secondChildElement.Descendants("iBlueprintLibrary-cAbsoluteBlueprintID").First().Descendants("BlueprintID").First().Value = String.Format(containerPathFormat, Path.GetFileNameWithoutExtension(container.filename));

                    if (container.mass > 53500)
                    {
                        throw new InvalidDataException("Mass cannot exceed 30480");
                    }

                    //t.Item2.Descendants("cContainerCargoDef").First().Descendants("MassInKg").First().Value = container.Mass.ToString(CultureInfo.InvariantCulture);

                    var xMoveAxisIdx = 12;
                    applyMatrixTransformation(t.Item2, xMoveAxisIdx, container.moveX);

                    var yMoveAxis = 13;
                    applyMatrixTransformation(t.Item2, yMoveAxis, container.moveY);

                    var zMoveAxisIdx = 14;
                    float moveZ = container.moveZ;
                    if (side == "B" && jsonFile == "Sffggmrrss_Trailers.json") moveZ = 0.815f + -moveZ;
                    if (side == "B" && jsonFile == "Afirus_Containers.json") moveZ = 0.035f + -moveZ;
                    if (side == "B" && jsonFile == "Afirus_Containers_20ftx2.json") moveZ = 3.445f + -moveZ;
                    
                    applyMatrixTransformation(t.Item2, zMoveAxisIdx, moveZ);

                    var xScaleAxisIdx = 0;
                    applyMatrixTransformation(t.Item2, xScaleAxisIdx, container.scaleX);

                    var yScaleAxisIdx = 5;
                    applyMatrixTransformation(t.Item2, yScaleAxisIdx, container.scaleY);

                    var zScaleAxisIdx = 10;
                    applyMatrixTransformation(t.Item2, zScaleAxisIdx, container.scaleZ);

                    var destinationPath = Path.Combine(rwLib.TSPath, String.Format(destinationPathFormat, wagonFilename + ".bin"));
                    var tempPath = await rwLib.Serializer.SerializeWithSerzExe(t.Item2);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    File.Move(tempPath, destinationPath, true);
                }
            });
        }
    }
}