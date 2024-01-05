using RWLib.Scenario;
using RWLib.Tracks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RWLib
{
    public class RWRouteLoader : RWLibraryDependent
    {
        public RWSerializer serializer;
        internal RWRouteLoader(RWLibrary rWLib, RWSerializer rwSerializer) : base(rWLib)
        {
            this.serializer = rwSerializer;
        }

        public class OpenFileResult {
            public string? FileName { get; set; } 
            public Stream? Stream { get; set; }
        }

        public OpenFileResult OpenFile(string filename, bool returnFilename)
        {
            var sections = filename.Replace(rWLib.options.TSPath, "")
                .Split(Path.DirectorySeparatorChar)
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .ToList();

            if (sections[0] != "Content" || sections[1] != "Routes") throw new FileNotFoundException($"Invalid filename. Filename '{filename}' should start with Content\\Routes");

            string guid = sections[2];
            string path = String.Join(Path.DirectorySeparatorChar, sections.Skip(3));

            string routeDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", guid);
            filename = Path.Combine(routeDir, path);

            if (File.Exists(filename))
            {
                if (returnFilename) return new OpenFileResult { FileName = filename };
                else return new OpenFileResult { Stream = File.OpenRead(filename) };
            }

            if (Directory.Exists(routeDir))
            {
                var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);

                foreach (var apFile in apFiles)
                {
                    ZipArchive zip = ZipFile.OpenRead(apFile);

                    var entry = zip.GetEntry(path.Replace('\\', '/'));

                    if (entry != null)
                    {
                        if (returnFilename) return new OpenFileResult { FileName = this.serializer.ExtractToTemporaryFile(entry) };
                        else return new OpenFileResult { Stream = entry.Open() };
                    }
                }
            }

            throw new FileNotFoundException($"Could not find file '{filename}' as file or in an .ap file");
        }

        public struct ListDirectoryEntryResult
        {
            public required string Name { get; set; }
            public ZipArchiveEntry? ZipArchiveEntry { get; set; }
        }

        public enum OpenFiles { OnlyInAp, Never }

        public IEnumerable<ListDirectoryEntryResult> ListFiles(string directory, bool recursive = false)
        {
            if (Directory.Exists(directory))
            {
                foreach (var name in Directory.GetFiles(directory))
                {
                    yield return new ListDirectoryEntryResult { Name = name };
                }
            } else
            {
                string routesDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes");
                string subPath = Path.GetRelativePath(routesDir, directory);
                var sections = subPath.Split('\\');
                var routeGuid = sections[0];
                var routeDir = Path.Combine(routesDir, routeGuid);

                if (Directory.Exists(routeDir))
                {
                    var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);
                    var relativePath = Path.GetRelativePath(routeDir, directory).Replace('\\', '/').Trim('/');
                    var dirNestingLevel = relativePath.Split('/').Length;

                    foreach (var apFile in apFiles)
                    {
                        ZipArchive zip = ZipFile.OpenRead(apFile);

                        var entries = zip.Entries.Where(x =>
                        {
                            var isDirectory = x.FullName.EndsWith('/');
                            if (isDirectory) return false;
                            if (recursive) return x.FullName.StartsWith(relativePath);
                            else return x.FullName.StartsWith(relativePath) && x.FullName.Trim('/').Split('/').Length - 1 == dirNestingLevel;
                        });

                        foreach (var entry in entries)
                        {
                            yield return new ListDirectoryEntryResult { Name = entry.Name, ZipArchiveEntry = entry };
                        }
                    }
                }
            }
        }

        public IAsyncEnumerable<RWConsist> LoadConsists(RWScenario scenario)
        {
            return LoadConsists(scenario.routeGuid, scenario.guid);
        }

        public async IAsyncEnumerable<RWConsist> LoadConsists(string routeGuid, string scenarioGuid)
        {
            var scenarioDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", routeGuid, "Scenarios", scenarioGuid);
            var scenarioBinPath = Path.Combine(scenarioDir, "Scenario.bin");

            var scenarioBinFile = OpenFile(scenarioBinPath, true).FileName!;
            var scenarioDocument = await serializer.DeserializeWithSerzExe(scenarioBinFile);

            foreach (var consistElement in scenarioDocument.Root!.Element("Record")!.Elements())
            {
                yield return new RWConsist(routeGuid, scenarioGuid, consistElement, rWLib);
            }
        }

        public IAsyncEnumerable<RWScenario> LoadScenarios(RWRoute route)
        {
            return LoadScenarios(route.guid);
        }

        public async IAsyncEnumerable<RWScenario> LoadScenarios(string routeGuid)
        {
            var routeDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", routeGuid);
            var scenariosDir = Path.Combine(routeDir, "Scenarios");
            var listOfFoundGuid = new HashSet<String>();

            if (Directory.Exists(scenariosDir))
            {
                var scenarios = Directory.GetDirectories(scenariosDir);

                foreach (var scenarioDir in scenarios)
                {
                    var guid = new DirectoryInfo(scenarioDir).Name;

                    var scenarioPropertiesFilename = Path.Combine(scenarioDir, "ScenarioProperties.Xml");
                    if (File.Exists(scenarioPropertiesFilename) == false) continue;

                    if (listOfFoundGuid.Contains(guid)) continue;
                    listOfFoundGuid.Add(guid);

                    XDocument document;

                    document = await LoadXMLSafe(scenarioPropertiesFilename);

                    yield return new RWScenario(document, guid, routeGuid, rWLib);
                }
            }

            var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);

            foreach (var apFile in apFiles)
            {
                ZipArchive zip = ZipFile.OpenRead(apFile);

                var scenarioPropertyEntries = zip.Entries.Where(e => e.FullName.StartsWith("Scenarios\\") && e.Name == "ScenarioProperties.Xml");

                foreach (var entry in scenarioPropertyEntries)
                {
                    var guid = entry.FullName.Split("\\")[1];

                    if (listOfFoundGuid.Contains(guid)) continue;
                    listOfFoundGuid.Add(guid);

                    XDocument document;

                    try
                    {
                        document = await XDocument.LoadAsync(entry.Open(), LoadOptions.None, CancellationToken.None);
                    }
                    catch (XmlException)
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            document = ParseXMLSafe(reader.ReadToEnd());
                        }
                    }

                    yield return new RWScenario(document, guid, routeGuid, rWLib);
                }
            }
        }

        public async Task<RWRoute?> LoadSingleRoute(string routeGuid)
        {
            var routeDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes", routeGuid);

            XDocument? document = null;

            var routePropertiesFilename = Path.Combine(routeDir, "RouteProperties.Xml");

            if (File.Exists(routePropertiesFilename))
            {
                document = await LoadXMLSafe(routePropertiesFilename);
            }
            else
            {
                var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);

                foreach (var apFile in apFiles)
                {
                    ZipArchive zip = ZipFile.OpenRead(apFile);
                    var entry = zip.GetEntry("RouteProperties.Xml");
                    if (entry != null)
                    {
                        try
                        {
                            document = await XDocument.LoadAsync(entry.Open(), LoadOptions.None, CancellationToken.None);
                            break;
                        }
                        catch (XmlException)
                        {
                            using (StreamReader reader = new StreamReader(entry.Open()))
                            {
                                document = ParseXMLSafe(reader.ReadToEnd());
                                break;
                            }
                        }
                    }
                }
            }

            if (document == null)
            {
                // route does not have a RouteProperties.Xml file so it's not valid.
                // skip it

                return null;
            }

            return new RWRoute(document, routeGuid, rWLib);
        }

        public async IAsyncEnumerable<RWRoute> LoadRoutes()
        {
            var routesDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes");

            var routes = Directory.GetDirectories(routesDir);

            foreach (var routeDir in routes)
            {
                var guid = new DirectoryInfo(routeDir).Name;

                var route = await LoadSingleRoute(guid);

                if (route == null)
                {
                    // route does not have a RouteProperties.Xml file so it's not valid.
                    // skip it

                    continue;
                }
                else
                {
                    yield return route;
                }
            }
        }
    }
}
