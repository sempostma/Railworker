// See https://aka.ms/new-console-template for more information

using BrakePercentageCalculator;
using RWLib;
using RWLib.SerzClone;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using RWLib.Tracks;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RWLib.Interfaces;

class Logger : IRWLogger
{
    public void Log(RWLogType type, string message)
    {
        if (type == RWLogType.Verbose) return;
        Console.WriteLine($"[{type.ToString()}] ${message}");
    }
}

class Program {

    public static void Main(string[] args)
    {
        //ParseBinWithCustomFunction().Wait();
        ScanAssetDirectory().Wait();
    }

    public static async Task ParseBinWithCustomFunction()
    {
        var stream = File.OpenRead("E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains\\RailSimulator\\RailVehicles\\EMUs\\Stadler Flirt 3\\Cameras\\Stadler Flirt3 CabCam SUWEX.bin");
        var parser = new BinToObj(stream);
        var objToXml = new ObjToXml();

        await foreach (var node in parser.Run())
        {
            objToXml.Push(node);
        };
        var result = objToXml.Finish();
        Console.WriteLine("Done");
    }

    public static string DetermineDisplayName(RWDisplayName? displayName)
    {
        if (displayName == null) return "Uknown name";
        var listInOrderOfPriority = new string[] {
                displayName.En,
                displayName.Fr,
                displayName.It,
                displayName.De,
                displayName.Es,
                displayName.Nl,
                displayName.Pl,
                displayName.Ru,
                displayName.Other,
                displayName.Key,
                "Uknown name"
            };

        return listInOrderOfPriority.Where(x => String.IsNullOrWhiteSpace(x) == false).First();
    }

    public static async Task ScanAssetDirectory()
    {
        try
        {
            RWLibrary rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true, Logger = new Logger() });
            var rWSerializer = rwLib.Serializer;
            RWBlueprintLoader rWBlueprintLoader = rwLib.BlueprintLoader;
            RWRouteLoader rwRouteLoader = rwLib.RouteLoader;
            RWTracksBinParser rwTracksBinParser = rwLib.TracksBinParser;

            var directory = "E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains";
            Console.WriteLine("Press any key to cancel...");
            var progressBar = new ProgressBar();
            //var progressBar = new Progress<int>();
            var cancellationToken = new CancellationTokenSource();
            var scanTask = async () =>
            {
                try
                {
                    await foreach (var item in rwLib.BlueprintLoader.ScanDirectory(directory, progressBar, cancellationToken))
                    {
                        if (item.HasRenderComponent && item.RenderComponent.DoesGeometryGeoPcdxExist == false)
                        {
                            // has missing gepocdx
                            progressBar.Pause();
                            Console.WriteLine("missing geopcdx for: " + item.RenderComponent.GeometryFilename.ToString());
                            progressBar.Resume();
                        }

                        var filter = item.XMLElementName == "cConsistBlueprint"
                            || item.XMLElementName == "cEngineBlueprint"
                            || item.XMLElementName == "cWagonBlueprint"
                            || item.XMLElementName == "cReskinBlueprint"
                            || item.XMLElementName == "cTenderBlueprint"
                            || item.XMLElementName == "cConsistFragmentBlueprint";

                        var displayName = item.xml.Descendants("DisplayName").FirstOrDefault();

                        if (filter && displayName != null)
                        {
                            //Console.WriteLine($"{item.Provider}/{item.Product}/{item.BlueprintIDPath} [{DetermineDisplayName(new RWDisplayName(displayName))}] [InApFile={item.Context.InApFile}]");
                        }
                    }

                } catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            };
            await scanTask(); // wait for it to finish
            progressBar.Dispose();
            Console.WriteLine("Done!");
            cancellationToken.Cancel();
        } catch(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public static async Task RouteToSvg()
    {
        RWLibrary rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true });
        var rWSerializer = rwLib.Serializer;
        RWBlueprintLoader rWBlueprintLoader = rwLib.BlueprintLoader;
        RWRouteLoader rwRouteLoader = rwLib.RouteLoader;
        RWTracksBinParser rwTracksBinParser = rwLib.TracksBinParser;

        //var result = rwTracksBinParser.GetTrackTiles("249f6618-0435-4b0b-8bbf-960642dfea1e"); // test trak
        //var result = rwTracksBinParser.GetTrackTiles("00000050-0000-0000-0000-000000002014"); // academy
        //var result = rwTracksBinParser.GetTrackTiles("be9a4c53-66e4-44c1-8fe0-c40b82bb2005"); // karwankenbahn
        var result = rwTracksBinParser.GetTrackTiles("3b763f87-554b-422b-b151-fe90e3f9768c"); // schafhausen constancen

        double xMin = double.MaxValue;
        double yMin = double.MaxValue;

        double xMax = double.MinValue;
        double yMax = double.MinValue;

        StringBuilder svg = new StringBuilder();

        foreach (var tile in result)
        {
            await foreach (var item in rwTracksBinParser.ProcessTrackTile(tile))
            {

                foreach (var curve in item.Curves)
                {
                    var startX = curve.Position.X;
                    var startZ = curve.Position.Z;

                    var angle = curve.Atan2; // radians
                    var endX = startX + Math.Cos(angle) * curve.Length;
                    var endY = startZ + Math.Sin(angle) * curve.Length;

                    if (startX < xMin) xMin = startX;
                    if (startX > xMax) xMax = startX;

                    if (startZ < yMin) yMin = startZ;
                    if (startZ > yMax) yMax = startZ;

                    if (endX < xMin) xMin = endX;
                    if (endX > xMax) xMax = endX;

                    if (endY < yMin) yMin = endY;
                    if (endY > yMax) yMax = endY;

                    switch (curve)
                    {
                        case CurveStraight curveStraight:
                            svg.AppendLine($"<line x1=\"{startX.ToString(CultureInfo.InvariantCulture)}\" y1=\"{startZ.ToString(CultureInfo.InvariantCulture)}\" x2=\"{endX.ToString(CultureInfo.InvariantCulture)}\" y2=\"{endY.ToString(CultureInfo.InvariantCulture)}\" stroke=\"white\" />");
                            break;

                        case CurveArc curveArc:
                            var endPosition = curveArc.GetEndPosition();

                            var rad = curveArc.Radius.ToString(CultureInfo.InvariantCulture);
                            var circle = curveArc.GetReferenceCircleCenter();
                            var cx = circle.X.ToString(CultureInfo.InvariantCulture);
                            var cy = circle.Z.ToString(CultureInfo.InvariantCulture);

                            //svg.AppendLine($"<circle fill=\"none\" stroke=\"blue\" cx=\"{cx}\" cy=\"{cy}\" r=\"{rad}\" />");

                            svg.Append($"<Path fill=\"none\" d=\"M{startX.ToString(CultureInfo.InvariantCulture)} {startZ.ToString(CultureInfo.InvariantCulture)}");
                            //svg.Append($" L{endPosition.X.ToString(CultureInfo.InvariantCulture)} {endPosition.Z.ToString(CultureInfo.InvariantCulture)}");
                            svg.Append($" A{rad} {rad} {0} {0} {(curveArc.Sign > 0 ? 0 : 1)} {endPosition.X.ToString(CultureInfo.InvariantCulture)} {endPosition.Z.ToString(CultureInfo.InvariantCulture)}");
                            //svg.Append($" L{cx} {cy}");
                            svg.Append($"\" stroke=\"red\"/>");
                            svg.AppendLine();
                            break;

                        case CurveEasement curveEasement:
                            List<string> list = new List<string>();
                            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(curveEasement))
                            {
                                string name = descriptor.Name;
                                object? value = descriptor.GetValue(curveEasement);

                                list.Add($"{name}: {value}");
                            }

                            for (int i = 0; i <= curveEasement.Length; i += 10)
                            {
                                var endPosition2 = curveEasement.EstimatePositionAt(i);

                                var endX2 = endPosition2.X.ToString(CultureInfo.InvariantCulture);
                                var endY2 = endPosition2.Z.ToString(CultureInfo.InvariantCulture);

                                svg.AppendLine($"<line data-props=\"{String.Join(", ", list)}\" x1=\"{startX.ToString(CultureInfo.InvariantCulture)}\" y1=\"{startZ.ToString(CultureInfo.InvariantCulture)}\" x2=\"{endX2}\" y2=\"{endY2}\" stroke=\"{(curveEasement.TraversalSign > 0 ? "purple" : "blue")}\" />");

                                startX = endPosition2.X;
                                startZ = endPosition2.Z;
                            }

                            var endPosition3 = curveEasement.EstimatePositionAt(curveEasement.Length);
                            var endX3 = endPosition3.X.ToString(CultureInfo.InvariantCulture);
                            var endY3 = endPosition3.Z.ToString(CultureInfo.InvariantCulture);

                            svg.AppendLine($"<line data-props=\"{String.Join(", ", list)}\" x1=\"{startX.ToString(CultureInfo.InvariantCulture)}\" y1=\"{startZ.ToString(CultureInfo.InvariantCulture)}\" x2=\"{endX3}\" y2=\"{endY3}\" stroke=\"{(curveEasement.TraversalSign > 0 ? "purple" : "blue")}\" />");

                            break;

                        default:
                            break;
                    }
                }
            }
        }

        xMin -= 100;
        yMin -= 100;
        xMax += 100;
        yMax += 100;

        var height = (yMax - yMin).ToString(CultureInfo.InvariantCulture);
        var width = (xMax - xMin).ToString(CultureInfo.InvariantCulture);

        string svgFinal = $"<svg width=\"{width}\" height=\"{height}\" style=\"background-color:black\" viewBox=\"{xMin.ToString(CultureInfo.InvariantCulture)} {yMin.ToString(CultureInfo.InvariantCulture)} {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">";
        svgFinal += "\n" + svg.ToString();
        svgFinal += "\n" + "</svg>";

        File.WriteAllText("output.svg", svgFinal);
        Console.WriteLine("Done");
    }

    public static async Task Run()
    {
        RWLibrary rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true });
        var rWSerializer = rwLib.Serializer;
        RWBlueprintLoader rWBlueprintLoader = rwLib.BlueprintLoader;
        RWRouteLoader rwRouteLoader = rwLib.RouteLoader;
        RWTracksBinParser rwTracksBinParser = rwLib.TracksBinParser;

        //var result = await rwRouteLoader.LoadSingleRoute("249f6618-0435-4b0b-8bbf-960642dfea1e"); // test trak
        //var result = await rwRouteLoader.LoadSingleRoute("00000050-0000-0000-0000-000000002014"); // academy
        //var result = await rwRouteLoader.LoadSingleRoute("be9a4c53-66e4-44c1-8fe0-c40b82bb2005"); // karwankenbahn
        var result = await rwRouteLoader.LoadSingleRoute("3b763f87-554b-422b-b151-fe90e3f9768c"); // schafhausen constancen

        var tiles = rwTracksBinParser.GetTrackTiles(result!.guid);
        var geoJsonAdapter = new GeoJsonAdapter(result.RouteOrigin, new GeoJsonAdapter.GeoJsonAdapterOptions
        {
            serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
            }
        });
        var destinationStream = File.OpenWrite("output.geo.json");
        var geoJsonStreamWriter = new GeoJsonStreamWriter(destinationStream, geoJsonAdapter);

        foreach (var tile in tiles)
        {
            await foreach (var item in rwTracksBinParser.ProcessTrackTile(tile))
            {
                foreach (var feature in geoJsonAdapter.ProcessRWTrackRibbon(item))
                {
                    await geoJsonStreamWriter.Write(feature);
                }
                
            }
        }

        await geoJsonStreamWriter.Finish();

        Console.WriteLine("Done");

        //while (true)
        //{
        //    Console.WriteLine("Please paste the full Path to your scenario (you can copy it from File Explorer):");
        //    String? line = Console.ReadLine();

        //    if (line == null) break;

        //    var sections = line.TrimEnd('\\', '/').Split('\\', '/').ToArray();

        //    string scenarioGuid = sections[sections.Length - 1];
        //    string routeGuid = sections[sections.Length - 3];
        //    string tsPath = String.Join("\\", sections.SkipLast(5));
        //    string serzPath = Path.Combine(tsPath, "serz.exe");

        //    RWLibrary rwLib = new RWLibrary(new RWLibOptions(new Logger(), tsPath, serzPath));

        //    var serializer = rwLib.CreateSerializer();
        //    var routeLoader = rwLib.CreateRouteLoader(serializer);
        //    var blueprintLoader = rwLib.CreateBlueprintLoader(serializer);

        //    int count = 0;
        //    double totalMass = 0;
        //    double totalBrakingWeight = 0;

        //    await foreach (var consist in routeLoader.LoadConsists(routeGuid, scenarioGuid))
        //    {
        //        if (consist.IsPlayer)
        //        {
        //            foreach (var consistVehicle in consist.Vehicles)
        //            {
        //                var vehicle = await blueprintLoader.FromBlueprintID(consistVehicle.BlueprintID) as IRWRailVehicleBlueprint;
        //                if (vehicle == null) continue;

        //                IBrakeAssembly airBrake;

        //                double mass = consistVehicle.Component.TotalMass;

        //                if (vehicle is RWEngineBlueprint)
        //                {
        //                    var simFileBlueprintId = (vehicle as RWEngineBlueprint)!.EngineSimulationBlueprint;
        //                    var blueprint = await blueprintLoader.FromBlueprintID(simFileBlueprintId);
        //                    var simFile = blueprint as IRWEngineSimulationBlueprint;

        //                    if (simFile == null)
        //                    {
        //                        throw new NotImplementedException("Only electric locomotives are supported at this time");
        //                    }

        //                    airBrake = simFile!.TrainBrakeAssembly;
        //                }
        //                else if (vehicle is RWWagonBlueprint)
        //                {
        //                    airBrake = (vehicle as RWWagonBlueprint)!.RailVehicleComponent.TrainBrakeAssembly;
        //                }
        //                else
        //                {
        //                    throw new NotImplementedException("This vehicle type is not yet implemented");
        //                }

        //                var airBrakePercentage = airBrake == null ? 0 : (airBrake as AirBrakeSimulation)!.MaxForcePercentOfVehicleWeight;
        //                var brakingWeight = mass * (airBrakePercentage / 100);

        //                Console.WriteLine($"{Math.Round(mass, 1)}t {Math.Round(airBrakePercentage)}% BRH {vehicle.Name}");

        //                count++;
        //                totalMass += mass;
        //                totalBrakingWeight += brakingWeight;
        //            }

        //            break;
        //        }
        //    }

        //    var brh = totalBrakingWeight / totalMass * 100;

        //    Console.WriteLine($"Total Mass: {Math.Round(totalMass)}t");
        //    Console.WriteLine($"Braking Weight: {Math.Round(totalBrakingWeight)}t");
        //    Console.WriteLine($"BRH: {Math.Round(brh)}%");

        //    var brakePercentageStr = Console.ReadLine() ?? "100%";

        //    brakePercentageStr = brakePercentageStr.Replace("%", "");
        //    double brakePercentage = Convert.ToDouble(brakePercentageStr);

        //    Console.WriteLine($"Adjusted BRH: {Math.Round(brh * (brakePercentage / 100))}%");
        //}
    }
}

