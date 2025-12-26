using RWLib;
using RWLib.Interfaces;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace RailworkerMegaFreightPack1
{
    public class STConsistGenerator
    {
        private class Logger : IRWLogger
        {
            public void Log(RWLogType type, string message)
            {
                Console.WriteLine("{0}: {1}", type.ToString(), message);
            }
        }

        private RWLibrary rwLib;
        private XDocument cargoReskinTemplate;
        private XDocument consistTemplate;
        private XDocument wagonReskinTemplate;

        // Dictionary mapping cargo types to their base blueprint names
        // Add more entries here when new cargo types are added
        private static readonly Dictionary<string, string> CargoTypeToBaseBlueprintMap = new Dictionary<string, string>
        {
            { "45ft", "45ft_hc_bechi_st" },
            { "20ft_mixed", "20ft_mixed_st_1" }
        };

        public class CargoVariant
        {
            public string Name { get; set; } = "";
            public string AutoNumber { get; set; } = "";
            public string Filename { get; set; } = "";
        }

        public class WagonReskin
        {
            [System.Text.Json.Serialization.JsonPropertyName("texture")]
            public string Texture { get; set; } = "";
            
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = "";
            
            [System.Text.Json.Serialization.JsonPropertyName("shortName")]
            public string ShortName { get; set; } = "";
        }

        public STConsistGenerator()
        {
            this.rwLib = new RWLibrary(new RWLibOptions { Logger = new Logger() });
            this.cargoReskinTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("STConsist.CargoReskin.xml"));
            this.consistTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("STConsist.SggmrssConsist45.xml"));
            this.wagonReskinTemplate = rwLib.Serializer.ParseXMLSafe(ReadFile("STConsist.WagonReskin.xml"));
        }

        private string ReadFile(string filename)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = $"RailworkerMegaFreightPack1.Resources.{filename.Replace("/", ".").Replace("\\", ".")}";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Resource not found: {resourceName}");
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Step 1: Generate cargo reskin blueprints for all 45ft variants
        /// </summary>
        public async Task GenerateCargoReskins()
        {
            Console.WriteLine("Generating Cargo Reskin Blueprints...");

            // Read the RandomSkins.json to get 45ft cargo variants
            var randomSkinsJson = ReadFile("RandomSkins.RandomSkins.json");
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var randomSkins = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(randomSkinsJson, options)!;

            if (!randomSkins.ContainsKey("KI 45ft HC"))
            {
                throw new Exception("KI 45ft HC section not found in RandomSkins.json");
            }

            var cargoVariants = randomSkins["KI 45ft HC"];
            var outputDir = Path.Combine(rwLib.TSPath, "Assets", "AlexAfirus", "ST Sggmrss90 Consist");
            Directory.CreateDirectory(outputDir);

            // Determine the base blueprint name for 45ft cargo
            string baseBlueprintName = CargoTypeToBaseBlueprintMap["45ft"];

            foreach (var variant in cargoVariants)
            {
                var filename = variant["filename"].ToString()!.Replace(".bin", "");
                var name = variant["name"].ToString()!;

                // Skip the base blueprint itself (it should already exist as a .igs file)
                if (filename == baseBlueprintName)
                {
                    Console.WriteLine($"Skipping base blueprint: {filename}");
                    continue;
                }

                // Skip Spanish (sp) variants
                if (filename.Contains("_sp"))
                {
                    Console.WriteLine($"Skipping Spanish variant: {filename}");
                    continue;
                }

                Console.WriteLine($"Generating cargo reskin for: {name} ({filename})");

                // Clone the template
                var reskinDoc = new XDocument(cargoReskinTemplate);
                var ns = reskinDoc.Root!.GetDefaultNamespace();

                // Update display name
                var displayNameNode = reskinDoc.Descendants(ns + "English").First();
                displayNameNode.Value = $"[Alex95] ST Consist 45ft {name}";

                // Update blueprint name
                var nameNode = reskinDoc.Descendants(ns + "Name").First();
                nameNode.Value = $"[Alex95] ST Consist 45ft {name}";

                // Update the base blueprint reference to use the ST version
                var blueprintIdNode = reskinDoc.Descendants(ns + "ReskinAssetBpId")
                    .Descendants(ns + "BlueprintID")
                    .First();
                blueprintIdNode.Value = $"{baseBlueprintName}.xml";

                // Update texture ID to point to the correct texture in KI 45ft HC folder
                var textureIdNode = reskinDoc.Descendants(ns + "TextureID").First();
                textureIdNode.Value = $"AlexAfirus\\KI 45ft HC\\Textures\\[00]{filename}";

                // Save the reskin blueprint as .bin
                await SaveBinFile(reskinDoc, $"{filename}.bin");
            }

            Console.WriteLine("Cargo reskin generation complete!");
        }

        /// <summary>
        /// Step 2: Generate consist blueprints for each cargo variant (both loaded and empty)
        /// </summary>
        public async Task GenerateConsistBlueprints()
        {
            Console.WriteLine("Generating Consist Blueprints...");

            // Read the RandomSkins.json to get 45ft cargo variants
            var randomSkinsJson = ReadFile("RandomSkins.RandomSkins.json");
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var randomSkins = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(randomSkinsJson, options)!;

            var cargoVariants = randomSkins["KI 45ft HC"];
            var outputDir = Path.Combine(rwLib.TSPath, "Assets", "AlexAfirus", "ST Sggmrss90 Consist");
            Directory.CreateDirectory(outputDir);

            // Generate loaded consists for each cargo
            foreach (var variant in cargoVariants)
            {
                var filename = variant["filename"].ToString()!.Replace(".bin", "");
                var name = variant["name"].ToString()!;
                
                // Skip Spanish (sp) variants
                if (filename.Contains("_sp"))
                {
                    Console.WriteLine($"Skipping Spanish variant consist: {filename}");
                    continue;
                }
                
                // Get autonumber file if specified in JSON
                string? autonumberFile = null;
                if (variant.ContainsKey("autoNumber"))
                {
                    var autoNumberPath = variant["autoNumber"].ToString()!;
                    autonumberFile = Path.GetFileNameWithoutExtension(autoNumberPath);
                }

                Console.WriteLine($"Generating consist for cargo: {name} (autonumber: {autonumberFile ?? "none"})");

                // Clone the template
                var consistDoc = new XDocument(consistTemplate);
                var ns = consistDoc.Root!.GetDefaultNamespace();

                // Update display name
                var displayNameNode = consistDoc.Descendants(ns + "English").First();
                displayNameNode.Value = $"90' ST Consist {name}";

                // Update blueprint name
                var nameNode = consistDoc.Descendants(ns + "Name").First();
                nameNode.Value = $"90' ST Consist {name}";

                // Update the child cargo reference
                var blueprintIdNode = consistDoc.Descendants(ns + "BlueprintID")
                    .Where(n => n.Value == "45ft_st_1.xml")
                    .FirstOrDefault();

                if (blueprintIdNode != null)
                {
                    blueprintIdNode.Value = $"{filename}.xml";
                }

                // Update autonumbering CSV reference if specified
                if (autonumberFile != null)
                {
                    var autonumberNode = consistDoc.Descendants(ns + "AutoNumberFile").FirstOrDefault();
                    if (autonumberNode != null)
                    {
                        autonumberNode.Value = $"AlexAfirus\\ST Sggmrss90 Consist\\AutoNumber\\{autonumberFile}.csv";
                    }
                }

                // Save the consist blueprint as .bin
                await SaveBinFile(consistDoc, $"sggmrss_consist_st_{filename}.bin");
            }

            // Generate empty consist
            Console.WriteLine("Generating empty consist...");
            var emptyConsistDoc = new XDocument(consistTemplate);
            var emptyNs = emptyConsistDoc.Root!.GetDefaultNamespace();

            // Update display name for empty
            var emptyDisplayNameNode = emptyConsistDoc.Descendants(emptyNs + "English").First();
            emptyDisplayNameNode.Value = "90' ST Consist Empty";

            var emptyNameNode = emptyConsistDoc.Descendants(emptyNs + "Name").First();
            emptyNameNode.Value = "90' ST Consist Empty";

            // Change geometry to non-45ft version
            var geometryIdNode = emptyConsistDoc.Descendants(emptyNs + "GeometryID").First();
            geometryIdNode.Value = "AlexAfirus\\ST Sggmrss90 Consist\\[00]sggmrss_st_consist";

            // Remove children from ContainerComponent
            var childrenNode = emptyConsistDoc.Descendants(emptyNs + "Children").First();
            childrenNode.RemoveNodes();

            // Clear script name
            var scriptNameNode = emptyConsistDoc.Descendants(emptyNs + "ScriptComponent")
                .Descendants(emptyNs + "Name").First();
            scriptNameNode.Value = "";

            await SaveBinFile(emptyConsistDoc, "sggmrss_consist_st_empty.bin");

            Console.WriteLine("Consist blueprint generation complete!");
        }

        /// <summary>
        /// Step 3: Generate wagon company reskin blueprints for each consist
        /// Creates reskin blueprints that reference base consists and override wagon textures
        /// </summary>
        public async Task GenerateWagonReskins()
        {
            Console.WriteLine("Generating Wagon Company Reskin Blueprints...");

            // Read wagon reskins
            var reskinsJson = ReadFile("Sggmrss.Reskins.json");
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var reskinsData = JsonSerializer.Deserialize<Dictionary<string, List<WagonReskin>>>(reskinsJson, options)!;
            var wagonReskins = reskinsData["reskins"];

            // Read cargo variants
            var randomSkinsJson = ReadFile("RandomSkins.RandomSkins.json");
            var randomSkins = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(randomSkinsJson, options)!;
            var cargoVariants = randomSkins["KI 45ft HC"];

            foreach (var reskin in wagonReskins)
            {
                var product = reskin.Texture.Split('\\')[0];
                Console.WriteLine($"Processing wagon reskin: {reskin.Name} ({product})");

                // Company-specific folder name starting with "ST"
                var companyFolder = $"ST Sggmrss90 {reskin.Name}";

                // Generate reskin blueprint for each cargo variant
                foreach (var variant in cargoVariants)
                {
                    var filename = variant["filename"].ToString()!.Replace(".bin", "");
                    var name = variant["name"].ToString()!;

                    // Skip Spanish (sp) variants
                    if (filename.Contains("_sp"))
                    {
                        continue;
                    }

                    // Clone the wagon reskin template
                    var reskinDoc = new XDocument(wagonReskinTemplate);
                    var ns = reskinDoc.Root!.GetDefaultNamespace();

                    // Update display name
                    var displayNameNode = reskinDoc.Descendants(ns + "English").First();
                    displayNameNode.Value = $"90' ST {reskin.ShortName} {name}";

                    var nameNode = reskinDoc.Descendants(ns + "Name").First();
                    nameNode.Value = $"90' ST {reskin.ShortName} {name}";

                    // Update base blueprint reference
                    var blueprintIdNode = reskinDoc.Descendants(ns + "ReskinAssetBpId")
                        .Descendants(ns + "BlueprintID")
                        .First();
                    blueprintIdNode.Value = $"sggmrss_consist_st_{filename}.xml";

                    // Update texture override to point to company-specific wagon texture
                    var textureIdNode = reskinDoc.Descendants(ns + "cReskinBlueprint-sTextureEntry")
                        .Descendants(ns + "TextureID").First();
                    textureIdNode.Value = $"AlexAfirus\\{product}\\Textures\\[00]wagon";

                    // Save reskin blueprint to company folder
                    await SaveBinFile(reskinDoc, $"sggmrss_consist_st_{filename}_{reskin.ShortName.ToLower()}.bin", companyFolder);
                }

                // Generate empty variant reskin for this company
                var emptyReskinDoc = new XDocument(wagonReskinTemplate);
                var emptyNs = emptyReskinDoc.Root!.GetDefaultNamespace();

                var emptyDisplayNameNode = emptyReskinDoc.Descendants(emptyNs + "English").First();
                emptyDisplayNameNode.Value = $"90' ST {reskin.ShortName} Empty";

                var emptyNameNode = emptyReskinDoc.Descendants(emptyNs + "Name").First();
                emptyNameNode.Value = $"90' ST {reskin.ShortName} Empty";

                var emptyBlueprintIdNode = emptyReskinDoc.Descendants(emptyNs + "ReskinAssetBpId")
                    .Descendants(emptyNs + "BlueprintID")
                    .First();
                emptyBlueprintIdNode.Value = "sggmrss_consist_st_empty.xml";

                var emptyTextureIdNode = emptyReskinDoc.Descendants(emptyNs + "cReskinBlueprint-sTextureEntry")
                    .Descendants(emptyNs + "TextureID").First();
                emptyTextureIdNode.Value = $"AlexAfirus\\{product}\\Textures\\[00]wagon";

                await SaveBinFile(emptyReskinDoc, $"sggmrss_consist_st_empty_{reskin.ShortName.ToLower()}.bin", companyFolder);

                Console.WriteLine($"  Completed reskins for: {reskin.Name}");
            }

            Console.WriteLine("Wagon reskin generation complete!");
        }

        /// <summary>
        /// Serialize XML to .bin file and save to Assets folder
        /// </summary>
        private async Task SaveBinFile(XDocument doc, string filename, string? customFolder = null)
        {
            // Serialize XML to binary using serz.exe
            var binPath = await rwLib.Serializer.SerializeWithSerzExe(doc);
            
            // Move to final location in Assets folder
            var folder = customFolder ?? "ST Sggmrss90 Consist";
            var finalPath = Path.Combine(rwLib.TSPath, "Assets", "AlexAfirus", folder, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.Move(binPath, finalPath, true);
            
            // Clean up any temporary XML files created by serz.exe
            var xmlPath = Path.ChangeExtension(binPath, ".xml");
            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
            }
            
            // Also check for files with trailing underscore (serz.exe sometimes creates these)
            var xmlPathUnderscore = xmlPath.Replace(".xml", "_.xml");
            if (File.Exists(xmlPathUnderscore))
            {
                File.Delete(xmlPathUnderscore);
            }
            
            Console.WriteLine($"  Saved: {folder}/{filename}");
        }

        /// <summary>
        /// Run all generation steps
        /// </summary>
        public async Task GenerateAll()
        {
            Console.WriteLine("=== Starting ST Consist Generation ===\n");

            await GenerateCargoReskins();
            Console.WriteLine();

            await GenerateConsistBlueprints();
            Console.WriteLine();

            await GenerateWagonReskins();
            Console.WriteLine();

            Console.WriteLine("=== ST Consist Generation Complete ===");
        }
    }
}