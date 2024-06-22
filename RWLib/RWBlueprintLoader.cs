using RWLib.Exceptions;
using RWLib.Interfaces;
using RWLib.RWBlueprints;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using RWLib.Scenario;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RWLib
{
    public class RWBlueprintLoader : RWLibraryDependent
    {
        private RWSerializer serializer;

        internal RWBlueprintLoader(RWLibrary rWLibrary, RWSerializer serializer) : base(rWLibrary)
        {
            this.serializer = serializer;
        }

        public RWBlueprint FromXDocument(RWBlueprintID blueprintID, XDocument xDocument, RWBlueprint.RWBlueprintContext? context = null)
        {
            XElement blueprint = xDocument.Root!.Element("Blueprint")!.Elements().First()!;

            switch (blueprint.Name.ToString())
            {
                case "cWagonBlueprint":
                    return new RWWagonBlueprint(blueprintID, blueprint, rWLib, context);

                case "cEngineBlueprint":
                    return new RWEngineBlueprint(blueprintID, blueprint, rWLib, context);

                case "cReskinBlueprint":
                    return new RWReskinBlueprint(blueprintID, blueprint, rWLib, context);

                case "cTenderBlueprint":
                    return new RWTenderBlueprint(blueprintID, blueprint, rWLib, context);

                case "cConsistBlueprint":
                    return new RWConsistBlueprint(blueprintID, blueprint, rWLib, context);

                case "cConsistFragmentBlueprint":
                    return new RWConsistFragmentBlueprint(blueprintID,
                                                          blueprint,
                                                          rWLib,
                                                          context);

                case "cEngineSimBlueprint":
                    var engineSimulation = blueprint
                        .Element("EngineSimComponent")!
                        .Element("cEngineSimComponentBlueprint")!
                        .Element("SubSystem")!
                        .Elements().First();

                    switch(engineSimulation.Name.ToString()) {
                        case "EngineSimulation-cDieselElectricSubSystemBlueprint":
                            return new RWDieselElectricEngineSimulationBlueprint(blueprintID, blueprint, rWLib, context);

                        case "EngineSimulation-cElectricSubSystemBlueprint":
                            return new RWElectricEngineSimulationBlueprint(blueprintID, blueprint, rWLib, context);

                        default:
                            throw new NotImplementedException("Engine simulation type is not implemented");
                    }

                default:
                    return new RWUnknownBlueprint(blueprintID, blueprint, rWLib, context);
            }
        }

        private ZipArchiveEntry? LookupFileInAp(string filename)
        {
            var path = Path.GetRelativePath(rWLib.options.TSPath, filename);
            if (path == null) throw new ArgumentException($"Invalid Filename relative to TS direcotry \"{rWLib.options.TSPath}\": {filename}");
            var sections = path.Split(Path.DirectorySeparatorChar);
            if (sections.Length < 3)
            {
                return null;
            }
            var provider = sections[1];
            var product = sections[2];
            var blueprintIdPath = String.Join('/', sections.Skip(3));
            var productDir = Path.Combine(rWLib.options.TSPath, "Assets", provider, product);
            if (Directory.Exists(productDir) == false) return null;
            List<string> apFiles = Directory.GetFiles(productDir, "*.ap", SearchOption.TopDirectoryOnly).ToList();

            foreach (var apFile in apFiles)
            {
                var apFilePath = Path.Combine(productDir, apFile);
                ZipArchive zip = ZipFile.OpenRead(apFilePath);

                var entry = zip.GetEntry(blueprintIdPath.Replace('\\', '/'));

                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }
        
        public bool DoesFileExist(string filename)
        {
            var f = filename;
            if (Path.IsPathRooted(f) == false)
            {
                f = Path.Combine(rWLib.options.TSPath, "Assets", f);
            }
            return File.Exists(f) || DoesFileExistInAp(f);
        }

        private bool DoesFileExistInAp(string filename)
        {
            var result = LookupFileInAp(filename);
            return result == null ? false : true;
        }

        public async IAsyncEnumerable<RWPreloadEntry> FromPreload(RWConsistBlueprintAbstract preload, bool flipped = false)
        {
            var entries = preload.Entries;
            if (flipped) entries = entries.Reverse();

            foreach (var entry in entries)
            {
                RWBlueprint? entryBlueprint = null;
                bool fnf = false;
                try
                {
                    entryBlueprint = await FromBlueprintID(entry.BlueprintName);
                } 
                catch (FileNotFoundException)
                {
                    fnf = true;
                }
                if (fnf)
                {
                    yield return new RWPreloadEntryNotFound { 
                        Flipped = flipped ^ entry.Flipped, 
                        BlueprintName = entry.BlueprintName 
                    };
                }
                if (entryBlueprint == null) continue;

                if (entryBlueprint is RWConsistFragmentBlueprint)
                {
                    var fragment = (RWConsistFragmentBlueprint)entryBlueprint;

                    await foreach (var fragmentEntry in FromPreload(fragment, flipped ^ entry.Flipped))
                    {
                        yield return fragmentEntry;
                    }
                }
                else
                {
                    yield return new RWPreloadEntryFound { 
                        Blueprint = entryBlueprint, 
                        Flipped = flipped ^ entry.Flipped, 
                        BlueprintName = entry.BlueprintName 
                    };
                }
            }
        }

        public async Task<RWBlueprint> FromFilename(string filename)
        {
            var path = Path.GetRelativePath(rWLib.options.TSPath, filename);
            var sections = path.Split(Path.DirectorySeparatorChar);
            var provider = sections[1];
            var product = sections[2];
            var blueprintIdPath = String.Join('/', sections.Skip(3));
            XDocument document;
            RWBlueprint.RWBlueprintContext context;
            var blueprintId = new RWBlueprintID(provider, product, blueprintIdPath);

            var isCustomSerz = serializer is RWSerializer;
            Stream? stream;

            if (File.Exists(filename))
            {
                context = new RWBlueprint.RWBlueprintContext { InApFile = RWBlueprint.RWBlueprintContext.IsInApFile.No };
                stream = isCustomSerz ? File.OpenRead(filename) : null;
            }
            else
            {
                var archive = LookupFileInAp(filename);
                if (archive == null)
                {
                    throw new FileNotFoundException(filename + " not found");
                }
                if (isCustomSerz == false) filename = serializer.ExtractToTemporaryFile(archive);
                context = new RWBlueprint.RWBlueprintContext { InApFile = RWBlueprint.RWBlueprintContext.IsInApFile.Yes };
                stream = isCustomSerz ? archive.Open() : null;
            }
            if (isCustomSerz) document = await ((RWSerializer)serializer).Deserialize(stream!);
            else document = await serializer.Deserialize(filename);
            return FromXDocument(blueprintId, document, context);
        }

        public async Task<RWBlueprint> FromBlueprintID(RWBlueprintID blueprintID)
        {
            string blueprintPath = blueprintID.GetRelativeFilePathFromAssetsFolder().Replace('/', System.IO.Path.DirectorySeparatorChar);
            blueprintPath = Path.ChangeExtension(blueprintPath, "bin");
            string filename = Path.Combine(rWLib.options.TSPath, "Assets", blueprintPath);
            return await FromFilename(filename);
        }

        public async IAsyncEnumerable<RWBlueprint> ScanDirectory(string directory, IProgress<int> progress, CancellationTokenSource cancellationTokenSource)
        {
            var token = cancellationTokenSource.Token;
            var startDateTime = DateTime.Now;

            var assetsPath = Path.Combine(rWLib.options.TSPath, "Assets");
            var relativePath = Path.GetRelativePath(assetsPath, directory);
            var sections = relativePath.Split(Path.DirectorySeparatorChar);
            var provider = sections.Length >= 1 ? sections[0] : "";
            var product = sections.Length >= 2 ? sections[1] : "";
            var hasProductPath = sections.Length >= 2;
            var productPathOrTopDir = Path.Combine(assetsPath, provider, product);

            var totalStopWatch = Stopwatch.StartNew();
            long totalFromXDocNanoSecs = 0;
            long fileEnumeratorNanos = 0;
            long apFilesIndexingNanos = 0;
            long totalNanos = 0;
            long serializationNanos = 0;

            Action? updateProgress = null;

            int _binProgress = 0;
            IProgress<int> binProgress = new Progress<int>(nthFile =>
            {
                _binProgress = nthFile;
                updateProgress!();
            });

            int _apProgress = 0;
            IProgress<int> apProgress = new Progress<int>(nthApEntry =>
            {
                _apProgress = nthApEntry;
                updateProgress!();
            });

            var binBlock = new TransformBlock<(string, int), RWBlueprint?>(async item =>
            {
                (string binPath, int index) = item;
                try
                {
                    Log.Verbose("Try: {0}", binPath);
                    var filename = Path.Combine(rWLib.options.TSPath, "Assets", binPath);
                    var serializationStopWatch = Stopwatch.StartNew();
                    var document = await serializer.Deserialize(filename);
                    serializationStopWatch.Stop();
                    Interlocked.Add(ref serializationNanos, serializationStopWatch.Elapsed.Nanoseconds);
                    var context = new RWBlueprint.RWBlueprintContext { InApFile = RWBlueprint.RWBlueprintContext.IsInApFile.No };
                    var blueprintId = RWBlueprintID.FromFilenameRelativeToAssetsDirectory(binPath);
                    var fromXDocStopWatch = Stopwatch.StartNew();
                    var blueprint = FromXDocument(blueprintId, document, context);
                    fromXDocStopWatch.Stop();
                    Interlocked.Add(ref totalFromXDocNanoSecs, fromXDocStopWatch.Elapsed.Nanoseconds);
                    binProgress.Report(index);
                    return blueprint;
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to parse {0}: {1}, stack trace: {2}", binPath, e.Message, e.StackTrace ?? "");
                }
                return null;
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = 10
            });

            var fileSet = new HashSet<string>();
            int fileCounter = 0;

            var fileEnumeratorTask = new Task(() =>
            {
                var fileEnumeratorStopWatch = Stopwatch.StartNew();
                var fileEnumerator = Directory.EnumerateFiles(directory, "*.bin", SearchOption.AllDirectories);
                foreach (var file in fileEnumerator)
                {
                    var relativeFilePath = Path.GetRelativePath(assetsPath, file);
                    if (relativeFilePath == null) continue;
                    fileSet.Add(relativeFilePath);
                    binBlock.Post((relativeFilePath, fileCounter++));
                }
                fileEnumeratorStopWatch.Stop();
                fileEnumeratorNanos = fileEnumeratorStopWatch.Elapsed.Nanoseconds;
                binBlock.Complete();
                progress.Report(1);
            });

            var apBlock = new TransformBlock<(string, ZipArchiveEntry, int), RWBlueprint?>(async item =>
            {
                (string apFilename, ZipArchiveEntry entry, int index) = item;
                var binPath = Path.Combine(apFilename, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                try
                { 
                    var serializationStopWatch = Stopwatch.StartNew();
                    XDocument document;
                    if (serializer is RWSerializer)
                    {
                        var stream = entry.Open();
                        document = await ((RWSerializer)serializer).Deserialize(stream);
                    }
                    else
                    {
                        string filename = serializer.ExtractToTemporaryFile(entry);
                        document = await serializer.Deserialize(filename);
                    }
                    serializationStopWatch.Stop();
                    Interlocked.Add(ref serializationNanos, serializationStopWatch.Elapsed.Nanoseconds);
                    var context = new RWBlueprint.RWBlueprintContext { InApFile = RWBlueprint.RWBlueprintContext.IsInApFile.Yes };
                    var blueprintId = RWBlueprintID.FromFilenameRelativeToAssetsDirectory(binPath);
                    var fromXDocStopWatch = Stopwatch.StartNew();
                    var blueprint = FromXDocument(blueprintId, document, context);
                    fromXDocStopWatch.Stop();
                    Interlocked.Add(ref totalFromXDocNanoSecs, fromXDocStopWatch.Elapsed.Nanoseconds);
                    fromXDocStopWatch.Stop();
                    return blueprint;
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to parse {0}/{1}: {2}, stack trace: {3}", apFilename, binPath, e.Message, e.StackTrace!);
                }
                return null;
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = token
            });

            int apArchivesCounter = 0;
            var indexingOfApFiles = new Task(async () =>
            {
                var apFilesIndexingStopWatch = Stopwatch.StartNew();
                List<string> apFiles = Directory.GetFiles(productPathOrTopDir, "*.ap", hasProductPath ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories).ToList();
                foreach (var apFile in apFiles)
                {
                    if (apFile == null) continue;
                    var apFilePath = Path.Combine(productPathOrTopDir, apFile);
                    var apFileRelativePath = Path.GetRelativePath(assetsPath, apFilePath);

                    try
                    {
                        var archive = ZipFile.OpenRead(apFile);
                        await fileEnumeratorTask; // do not start processing ap files because we first need to know all bin filenames because any duplicate file in ap files need to be ignored.
                        foreach (var entry in archive.Entries)
                        {
                            var binPath = Path.Combine(apFileRelativePath, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                            if (fileSet.Contains(binPath)) continue;
                            apBlock.Post((apFileRelativePath, entry, apArchivesCounter++));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to read .ap file: {0}. Error: {1}, Error stack: {2}", apFile, ex.Message, ex.StackTrace ?? "");
                    }
                }
                apFilesIndexingStopWatch.Stop();
                apFilesIndexingNanos = apFilesIndexingStopWatch.Elapsed.Nanoseconds;
                apBlock.Complete();
                progress.Report(2);
            });

            updateProgress = new Action(() =>
            {
                double p = 0;
                if (fileEnumeratorTask.IsCompleted && indexingOfApFiles.IsCompleted)
                {
                    var totalItems = apArchivesCounter + fileCounter;
                    var totalProgress = _binProgress + _apProgress;
                    if (totalItems > 0) p = (double)totalProgress / totalItems;
                    else p = 1;
                }
                double baseAmount = 2;
                var total = baseAmount + p * 98;
                var totalI = (int)Math.Round(total);
                progress.Report(totalI);
            });

            fileEnumeratorTask.Start();
            indexingOfApFiles.Start();

            int loopCounter = 0;
            do
            {
                loopCounter++;
                token.ThrowIfCancellationRequested();
                var binBlockAvail = await binBlock.OutputAvailableAsync();
                if (binBlockAvail)
                {
                    while (binBlock.TryReceive(out var result))
                    {
                        if (result != null) yield return result;
                    }
                }
                var apBlockAvail = await apBlock.OutputAvailableAsync();
                if (apBlockAvail)
                {
                    while (binBlock.TryReceive(out var result))
                    {
                        if (result != null) yield return result;
                    }
                }
                if (apBlockAvail == false && binBlockAvail == false)
                {
                    break;
                }
            } while (true);

            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total time seconds: {totalStopWatch.Elapsed.Seconds}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total 'FromXDocument' seconds: {((double)totalFromXDocNanoSecs / 1000000000).ToString()}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total file enumerator seconds: {((double)fileEnumeratorNanos / 1000000000).ToString()}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total ap indexing seconds: {((double)apFilesIndexingNanos / 1000000000).ToString()}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total serz seconds: {((double)serializationNanos / 1000000000).ToString()}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total thread seconds: {((double)totalNanos / 1000000000).ToString()}");
            rWLib.options.Logger?.Log(RWLogType.Debug, $"Total wait loop iterations: {loopCounter.ToString()}");

            await fileEnumeratorTask; // this should always be finished at this point but include anyway
            await indexingOfApFiles; // this should always be finished at this point but include anyway
            await binBlock.Completion;
            await apBlock.Completion;
        }

        public async Task<RWConsistBlueprint> CreateConsistBlueprint(string provider, string product, string path, RWConsist consist, bool reversed)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var blueprintId = new RWBlueprintID(provider, product, path);

            var combinedPath = blueprintId.CombinedPath;
            combinedPath = Path.ChangeExtension(combinedPath, ".bin");
            var absolutePath = Path.Combine(rWLib.TSPath, "Assets", combinedPath);

            if (File.Exists(absolutePath))
            {
                throw new FileAlreadyExistsException(combinedPath);
            }

            var decleration = new XDeclaration("1.0", "utf-8", null);
            var newConsistBlueprintXDocument = new XDocument(decleration);
            var newConsistBlueprintXDocumentRoot = new XElement("cBlueprintLoader");
            var blueprintElement = new XElement("Blueprint");
            newConsistBlueprintXDocumentRoot.Add(blueprintElement);
            newConsistBlueprintXDocument.Add(newConsistBlueprintXDocumentRoot);
            newConsistBlueprintXDocumentRoot.Add(new XAttribute(XNamespace.Xmlns + "d", RWUtils.KujuNamspace));
            newConsistBlueprintXDocumentRoot.Add(new XAttribute(RWUtils.KujuNamspace + "version", "1.0"));
            var consistBlueprint = new XElement("cConsistBlueprint");
            blueprintElement.Add(consistBlueprint);
            var consistBlueprintEntry = new XElement("ConsistEntry");
            consistBlueprint.Add(consistBlueprintEntry);

            var id = 1;
            var idx = 0;

            RWDisplayName locoName = RWDisplayName.FromString(product);
            int locoIdx = 0;

            foreach (var vehicle in consist.Vehicles)
            {
                var consistEntry = new XElement("cConsistEntry");
                consistEntry.Add(new XAttribute(RWUtils.KujuNamspace + "id", "" + id++));

                var blueprintName = new XElement("BlueprintName");
                consistEntry.Add(blueprintName);

                var vehBlueprintId = vehicle.BlueprintID.ToXml();
                blueprintName.Add(vehBlueprintId);

                var flipped = new XElement("Flipped");
                flipped.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));

                var isFlipped = reversed ^ vehicle.railVehicle.Descendants("Flipped").FirstOrDefault()?.Value == "1";

                flipped.Value = isFlipped ? "eTrue" : "eFalse";

                consistEntry.Add(flipped);
                consistBlueprintEntry.Add(consistEntry);

                var blueprint = await rWLib.BlueprintLoader.FromBlueprintID(vehicle.BlueprintID);
                var blueprintRailvehicle = blueprint as IRWRailVehicleBlueprint;

                //if (blueprintRailvehicle != null) {
                //    if (vehicle.railVehicle.Descendants("cEngine").FirstOrDefault() != null)
                //    {
                //        locoName = blueprintRailvehicle.DisplayName;
                //        locoIdx = idx;
                //    }
                //}

                idx++;
            }

            var locoNameX = new XElement("LocoName");
            locoNameX.Add(locoName.ToXml());
            consistBlueprint.Add(locoNameX);

            var displayName = new XElement("DisplayName");
            var displayNameX = RWDisplayName.FromString(name).ToXml();
            displayName.Add(displayNameX);
            consistBlueprint.Add(displayName);

            var engineType = new XElement("EngineType");
            engineType.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            engineType.Value = "Electric";
            consistBlueprint.Add(engineType);

            var eraStartYear = new XElement("EraStartYear");
            eraStartYear.Add(new XAttribute(RWUtils.KujuNamspace + "type", "sUInt32"));
            eraStartYear.Value = "1850";
            consistBlueprint.Add(eraStartYear);

            var eraEndYear = new XElement("EraEndYear");
            eraEndYear.Add(new XAttribute(RWUtils.KujuNamspace + "type", "sUInt32"));
            eraEndYear.Value = "2050";
            consistBlueprint.Add(eraEndYear);

            var drivingEngineIndex = new XElement("DrivingEngineIndex");
            drivingEngineIndex.Add(new XAttribute(RWUtils.KujuNamspace + "type", "sUInt32"));
            drivingEngineIndex.Value = "" + locoIdx;
            consistBlueprint.Add(drivingEngineIndex);

            var validBuildAndDriveRoutes = new XElement("ValidBuildAndDriveRoutes");
            consistBlueprint.Add(validBuildAndDriveRoutes);

            var drivableConsist = new XElement("DrivableConsist");
            drivableConsist.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            drivableConsist.Value = locoName == null ? "eFalse" : "eTrue";
            consistBlueprint.Add(drivableConsist);

            var consistType = new XElement("ConsistType");
            consistType.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            consistType.Value = "eConsistTypePassengerCommuter";
            consistBlueprint.Add(consistType);

            var hasPantograph = new XElement("HasPantograph");
            hasPantograph.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            hasPantograph.Value = "eTrue";
            consistBlueprint.Add(hasPantograph);

            var has3rdRailShoe = new XElement("Has3rdRailShoe");
            has3rdRailShoe.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            has3rdRailShoe.Value = "eFalse";
            consistBlueprint.Add(has3rdRailShoe);

            var requires4thRail = new XElement("Requires4thRail");
            requires4thRail.Add(new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"));
            requires4thRail.Value = "eFalse";
            consistBlueprint.Add(requires4thRail);

            return new RWConsistBlueprint(blueprintId, newConsistBlueprintXDocumentRoot, rWLib);
        }
    }
}
