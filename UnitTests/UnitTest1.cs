using NUnit.Framework;
using RWLib;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests
{
    public class UnitTest1
    {
        private RWLibrary rwLib;

        [SetUp]
        public void Setup()
        {
            this.rwLib = new RWLibrary(new RWLibOptions { Logger = new UnitTestLogger() });
        }

        [Test]
        public async Task TestSerializer()
        {
            var result = await rwLib.Serializer.Deserialize("E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains\\RailSimulator\\RailVehicles\\Locomotives\\Diesel\\NS Class 6400\\Engine\\Version DB Cargo\\NS Class 6400 DB Cargo.bin");

            Assert.NotNull(result.Root);
        }

        [Test]
        public async Task TestBlueprintLoader()
        {
            var result = await rwLib.BlueprintLoader.FromFilename("E:\\SteamLibrary\\steamapps\\common\\RailWorks\\Assets\\ChrisTrains\\RailSimulator\\RailVehicles\\Locomotives\\Diesel\\NS Class 6400\\Engine\\Version DB Cargo\\NS Class 6400 DB Cargo.bin");

            Assert.AreEqual(result.XMLElementName, "cEngineBlueprint");
        }

        [Test]
        public async Task TestRouteLoader()
        {
            var routeLoader = rwLib.RouteLoader;

            var result = routeLoader.LoadRoutes();

            await foreach (var route in result)
            {
                Assert.AreEqual(route.routeProperties.Root!.Name.ToString(), "cRouteProperties");

                break;
            }
        }

        [Test]
        public async Task TestRouteLoaderScenarioLoading()
        {
            var routeLoader = rwLib.RouteLoader;

            var routes = routeLoader.LoadRoutes();

            await foreach (var route in routes)
            {
                var scenarios = routeLoader.LoadScenarios(route.guid);

                await foreach (var scenario in scenarios)
                {
                    Assert.AreEqual(scenario.scenarioProperties.Root!.Name.ToString(), "cScenarioProperties");

                    break;
                }

                break;
            }
        }

        [Test]
        public async Task TestRoutLoaderConsistLoading()
        {
            var routeLoader = rwLib.RouteLoader;

            var routes = routeLoader.LoadRoutes();

            await foreach (var route in routes)
            {
                var scenarios = routeLoader.LoadScenarios(route);

                await foreach (var scenario in scenarios)
                {
                    var consists = routeLoader.LoadConsists(scenario);

                    await foreach (var consist in consists)
                    {
                        Assert.AreEqual("cConsist", consist.consistElement.Name.ToString());

                        break;
                    }

                    break;
                }

                break;
            }
        }


        [Test]
        public async Task TestRoutLoaderConsistVehicleLoading()
        {
            var routeLoader = rwLib.RouteLoader;
            var blueprintLoader = rwLib.BlueprintLoader;

            var routes = routeLoader.LoadRoutes();

            await foreach (var route in routes)
            {
                var scenarios = routeLoader.LoadScenarios(route);

                await foreach (var scenario in scenarios)
                {
                    var consists = routeLoader.LoadConsists(scenario);

                    await foreach (var consist in consists)
                    {
                        foreach (var consistVehicle in consist.Vehicles)
                        {
                            var blueprint = await blueprintLoader.FromBlueprintID(consistVehicle.BlueprintID);

                            if (blueprint is IRWRailVehicleBlueprint)
                            {
                                //var railVehicleBlueprint = blueprint as IRWRailVehicleBlueprint;
                                //var brake = railVehicleBlueprint!.RailVehicleComponent.TrainBrakeAssembly.First();

                                //if (brake is AirBrakeSimulation)
                                //{
                                //    var airBrakeBlueprint = brake as AirBrakeSimulation;
                                //    var maxPercent = airBrakeBlueprint!.MaxForcePercentOfVehicleWeight;

                                //    Assert.AreEqual(maxPercent, 80.0d);
                                //}

                                Assert.Pass();
                            }
                            else
                            {
                                Assert.Fail("Blueprint is not a RailVehicle");
                            }

                            break;
                        }

                        break;
                    }

                    break;
                }

                break;
            }
        }
    }
}