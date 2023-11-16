// See https://aka.ms/new-console-template for more information

using RWLib;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using System.Diagnostics;

class Logger : IRWLogger
{
    public void Log(RWLogType type, string message)
    {
        Console.WriteLine($"[{type.ToString()}] ${message}");
    }
}

class Program {

    public static void Main(string[] args)
    {
        Run().Wait();
    }

    public static async Task Run()
    {
        while (true)
        {
            Console.WriteLine("Please paste the full path to your scenario (you can copy it from File Explorer):");
            String? line = Console.ReadLine();

            if (line == null) break;

            var sections = line.TrimEnd('\\', '/').Split('\\', '/').ToArray();

            string scenarioGuid = sections[sections.Length - 1];
            string routeGuid = sections[sections.Length - 3];
            string tsPath = "\\\\EsstudioNAS\\SecureBackups\\Railworks 2";
            string serzPath = "C:\\Users\\sempo\\Documents\\RWTest\\serz.exe";

            RWLibrary rwLib = new RWLibrary(new RWLibOptions(new Logger(), tsPath, serzPath));

            var serializer = rwLib.CreateSerializer();
            var routeLoader = rwLib.CreateRouteLoader(serializer);
            var blueprintLoader = rwLib.CreateBlueprintLoader(serializer);

            int count = 0;
            double totalMass = 0;
            double totalBrakingWeight = 0;

            await foreach (var consist in routeLoader.LoadConsists(routeGuid, scenarioGuid))
            {
                if (consist.IsPlayer)
                {
                    foreach (var consistVehicle in consist.Vehicles)
                    {
                        var vehicle = await blueprintLoader.FromBlueprintID(consistVehicle.BlueprintID) as IRailVehicle;
                        if (vehicle == null) continue;

                        IBrakeAssembly airBrake;

                        double mass = consistVehicle.Component.TotalMass;

                        if (vehicle is RWEngineBlueprint)
                        {
                            var simFileBlueprintId = (vehicle as RWEngineBlueprint)!.EngineSimulationBlueprint;
                            var blueprint = await blueprintLoader.FromBlueprintID(simFileBlueprintId);
                            var simFile = blueprint as IRWEngineSimulationBlueprint;

                            if (simFile == null)
                            {
                                throw new NotImplementedException("Only electric locomotives are supported at this time");
                            }

                            airBrake = simFile!.TrainBrakeAssembly;
                        }
                        else if (vehicle is RWWagonBlueprint)
                        {
                            airBrake = (vehicle as RWWagonBlueprint)!.RailVehicleComponent.TrainBrakeAssembly;
                        }
                        else
                        {
                            throw new NotImplementedException("This vehicle type is not yet implemented");
                        }

                        var airBrakePercentage = airBrake == null ? 0 : (airBrake as AirBrakeSimulation)!.MaxForcePercentOfVehicleWeight;
                        var brakingWeight = mass * (airBrakePercentage / 100);

                        Console.WriteLine($"{Math.Round(mass, 1)}t {Math.Round(airBrakePercentage)}% BRH {vehicle.Name}");

                        count++;
                        totalMass += mass;
                        totalBrakingWeight += brakingWeight;
                    }

                    break;
                }
            }

            var brh = totalBrakingWeight / totalMass * 100;

            Console.WriteLine($"Total Mass: {Math.Round(totalMass)}t");
            Console.WriteLine($"Braking Weight: {Math.Round(totalBrakingWeight)}t");
            Console.WriteLine($"BRH: {Math.Round(brh)}%");

            var brakePercentageStr = Console.ReadLine() ?? "100%";

            brakePercentageStr = brakePercentageStr.Replace("%", "");
            double brakePercentage = Convert.ToDouble(brakePercentageStr);

            Console.WriteLine($"Adjusted BRH: {Math.Round(brh * (brakePercentage / 100))}%");
        }
    }
}

