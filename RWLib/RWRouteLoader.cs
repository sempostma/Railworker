using System;
using System.Collections.Generic;
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

        private class OpenFileResult {
            public string? FileName { get; set; }
            public Stream? Stream { get; set; }
        }

        private OpenFileResult OpenFile(string filename, bool returnFilename)
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

                    var entry = zip.GetEntry(path);

                    if (entry != null)
                    {
                        if (returnFilename) return new OpenFileResult { FileName = this.serializer.ExtractToTemporaryFile(entry) };
                        else return new OpenFileResult { Stream = entry.Open() };
                    }
                }
            }

            throw new FileNotFoundException($"Could not find file '{filename}' as file or in an .ap file");
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
            var scenarioDocument = await serializer.Deserialize(scenarioBinFile);

            foreach (var consistElement in scenarioDocument.Root!.Element("Record")!.Elements())
            {
                yield return new RWConsist(routeGuid, scenarioGuid, consistElement);
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

                    if (listOfFoundGuid.Contains(guid)) continue;
                    listOfFoundGuid.Add(guid);

                    XDocument document;

                    var scenarioPropertiesFilename = Path.Combine(scenarioDir, "ScenarioProperties.xml");

                    document = await LoadXMLSafe(scenarioPropertiesFilename);

                    yield return new RWScenario(document, guid, routeGuid);
                }
            }

            var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);

            foreach (var apFile in apFiles)
            {
                ZipArchive zip = ZipFile.OpenRead(apFile);

                var scenarioPropertyEntries = zip.Entries.Where(e => e.FullName.StartsWith("Scenarios\\") && e.Name == "ScenarioProperties.xml");

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

                    yield return new RWScenario(document, guid, routeGuid);
                }
            }
        }

        public async IAsyncEnumerable<RWRoute> LoadRoutes()
        {
            var routesDir = Path.Combine(rWLib.options.TSPath, "Content", "Routes");

            var routes = Directory.GetDirectories(routesDir);

            foreach (var routeDir in routes)
            {
                var guid = new DirectoryInfo(routeDir).Name;

                XDocument document;

                var routePropertiesFilename = Path.Combine(routeDir, "RouteProperties.xml");

                if (File.Exists(routePropertiesFilename)) {
                    document = await LoadXMLSafe(routePropertiesFilename);
                } 
                else
                {
                    var apFiles = Directory.GetFiles(routeDir, "*.ap", SearchOption.TopDirectoryOnly);

                    XDocument? routeProperties = null;

                    foreach (var apFile in apFiles)
                    {
                        ZipArchive zip = ZipFile.OpenRead(apFile);
                        var entry = zip.GetEntry("RouteProperties.xml");
                        if (entry != null)
                        {
                            try
                            {
                                routeProperties = await XDocument.LoadAsync(entry.Open(), LoadOptions.None, CancellationToken.None);
                                break;
                            }
                            catch (XmlException)
                            {
                                using (StreamReader reader = new StreamReader(entry.Open()))
                                {
                                    routeProperties = ParseXMLSafe(reader.ReadToEnd());
                                    break;
                                }
                            }
                        }
                    }

                    if (routeProperties == null)
                    {
                        // route does not have a RouteProperties.xml file so it's not valid.
                        // skip it

                        continue;
                    }

                    yield return new RWRoute(routeProperties, guid);
                }
            }
        }
    }
}
