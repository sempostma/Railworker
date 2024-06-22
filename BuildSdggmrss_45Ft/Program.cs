// See https://aka.ms/new-console-template for more information
using RWLib;
using RWLib.Interfaces;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
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
            Console.WriteLine("Generating Afirus Variants");

            await CreateVariants("Afirus_Containers.json", "Afirus", "ContainerPack", "RailNetwork\\Interactive\\45ft_hc_pw\\{0}.xml",
                new[] { ("Sand", "Sdggmrss_Back_Sand_Con40", "Sdggmrss_Front_Sand_Con40") }, "45ft");

            Console.WriteLine("Generating Cargo Blueprints");

            // the issue is that FCon has a seperate Geo

            //await CreateAfirusContainerCargos();

            //Console.WriteLine("Generating 20ft Afirus Variants");

            //await CreateVariants("Afirus_Containers20ftx2.json", "Afirus", "ContainerPack", "RailNetwork\\Cargo\\sdggmrss\\{0}.xml",
            //    new[] { ("Sand", "Sdggmrss_Back_Sand_FCon", "Sdggmrss_Front_Sand_FCon") }, "20ftx2");

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
        var child = xmlDoc.Descendants("cEntityContainerBlueprint-sChild").Where(child => child.Element("ChildName")?.Value == "A").First();
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
        var variants = JsonSerializer.Deserialize<List<FileItem>>(ReadFile("Afirus_Containers20ftx2.json"), options)!;
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

            var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "Afirus", "ContainerPack", "RailNetwork", "Cargo", "sdggmrss", variant.filename + ".bin");
            var tempPath = await rwLib.Serializer.SerializeWithSerzExe(template);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Move(tempPath, destinationPath, true);
        }
    }

    public static async Task CreateVariants(string jsonFile, string provider, string product, string containerPathFormat, (string, string, string)[] wagonTypes, string label)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var containers = JsonSerializer.Deserialize<List<FileItem>>(ReadFile(jsonFile), options);
        var templateBackOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("Sdggmrss_Back.xml"));
        var templateFrontOrig = rwLib.Serializer.ParseXMLSafe(ReadFile("Sdggmrss_Front.xml"));

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
                    string wagonName = $"vR Sdggmrss {t.Item1} [{label} {containerNiceName}] by Afirus";
                    string wagonFilename = $"Sdggmrss_{t.Item1}_Sand_{Path.GetFileNameWithoutExtension(container.filename)}".Replace(' ', '_').ToLower();
                    t.Item2.Descendants("Name").First().Value = wagonName;

                    foreach (var translation in t.Item2.Descendants("Localisation-cUserLocalisedString").First().Descendants())
                    {
                        if (translation.Name == "Other") continue;
                        if (translation.Name == "Key") continue;
                        translation.Value = wagonName;
                    }

                    var geo = "virtualRailroads\\vR_Sdggmrss\\Frachtw\\GW\\[00]" + (t.Item1 == "Back" ? backGeo : frontGeo);
                    t.Item2.Descendants("GeometryID").First().Value = geo;
                    t.Item2.Descendants("CollisionGeometryID").First().Value = geo;

                    var secondChildElement = t.Item2.Descendants("cEntityContainerBlueprint-sChild").Skip(1).First();

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
                    applyMatrixTransformation(t.Item2, zMoveAxisIdx, container.moveZ);

                    //var xScaleAxisIdx = 0;
                    //ApplyMatrixTransformation(t.Item2, xScaleAxisIdx, container.ScaleX);

                    //var yScaleAxisIdx = 5;
                    //ApplyMatrixTransformation(t.Item2, yScaleAxisIdx, container.ScaleY);

                    //var zScaleAxisIdx = 10;
                    //ApplyMatrixTransformation(t.Item2, zScaleAxisIdx, container.ScaleZ);

                    var destinationPath = Path.Combine(rwLib.TSPath, "Assets", "VirtualRailroads", "vR_Sdggmrss", "Frachtw", "[Afirus]GW", wagonFilename + ".bin");
                    var tempPath = await rwLib.Serializer.SerializeWithSerzExe(t.Item2);

                    File.Move(tempPath, destinationPath, true);
                }
            });
        }
    }
}