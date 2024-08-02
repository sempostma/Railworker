using RWLib;
using RWLib.Packaging;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using static RailworkerMegaFreightPack1.Utilities;

namespace RailworkerMegaFreightPack1
{
    public class CTSgnsGenerator
    {
        private RWLibrary rwLib;

        public CTSgnsGenerator()
        {
            this.rwLib = new RWLibrary();
        }

        public async Task GenerateVariants()
        {
            try
            {
                await Generate45ftVariants();
                await Generate20ftVariants();
                await GenerateCT20ftVariants();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private List<WagonType> CreateWagonTypes(XDocument template, string label)
        {
            return new List<WagonType>()
                {
                    new WagonType
                    {
                        InGameType = "Brown",
                        Type = "brown",
                        KeepAutoNumbering = true,
                        BlueprintTemplates = new List<WagonType.BlueprintTemplate>
                        {
                            new WagonType.BlueprintTemplate()
                            {
                                GeoFileName = "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[00]NS Sgns loaded",
                                Label = label,
                                XDocument = template
                            }
                        }
                    },
                    new WagonType
                    {
                        InGameType = "Grey",
                        Type = "grey",
                        KeepAutoNumbering = true,
                        BlueprintTemplates = new List<WagonType.BlueprintTemplate>
                        {
                            new WagonType.BlueprintTemplate()
                            {
                                GeoFileName = "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[00]NS Sgns grey",
                                Label = label,
                                XDocument = template
                            }
                        }
                    },
                    new WagonType
                    {
                        InGameType = "Red",
                        Type = "red",
                        KeepAutoNumbering = true,
                        BlueprintTemplates = new List<WagonType.BlueprintTemplate>
                        {
                            new WagonType.BlueprintTemplate()
                            {
                                GeoFileName = "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[00]NS Sgns red",
                                Label = label,
                                XDocument = template
                            }
                        }
                    }
            };
        }

        private async Task Generate45ftVariants()
        {
            Console.WriteLine("Generating 45ft Variants!");

            var container45 = FileItem.FromJson(ReadFile("CT_Sgns.Afirus_Containers1x45ft.json"));
            var template45 = rwLib.Serializer.ParseXMLSafe(ReadFile("CT_Sgns.Sgns45ft.xml"));

            List<WagonType> sgnsWagons = CreateWagonTypes(template45, "45ft");

            await rwLib.VariantGenerator.CreateVariants(
                container45,
                sgnsWagons,
                "Afirus",
                "ContainerPack",
                "RailNetwork\\Interactive\\{0}.xml",
                "CT NS Sgns {0} 45ft {1} by Afirus",
                "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[Afirus]GW\\NS Sgns {0} {1}"
            );

            Console.WriteLine("Done!");

        }

        private async Task Generate20ftVariants()
        {
            Console.WriteLine("Generating TT 3x Variants");

            var container20 = FileItem.FromJson(ReadFile("CT_Sgns.Afirus_Containers3x20ft.json"));
            var template20 = rwLib.Serializer.ParseXMLSafe(ReadFile("CT_Sgns.Sgns20ftx3.xml"));

            List<WagonType> sgnsWagons = CreateWagonTypes(template20, "20ft");


            await rwLib.VariantGenerator.CreateVariants(
                    container20,
                    sgnsWagons,
                    "Afirus",
                    "ContainerPack",
                    "RailNetwork\\Interactive\\{0}.xml",
                    "CT NS Sgns {0} 20ftx3 {1} by Afirus",
                    "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[Afirus]GW\\NS Sgns {0} {1}"
                );

            Console.WriteLine("Done!");
        }

        private async Task GenerateCT20ftVariants()
        {
            Console.WriteLine("Generating CT 3x Variants");

            var container20 = FileItem.FromJson(ReadFile("CT_Sgns.CT_Tanktainers3x20ft.json"));
            var template20 = rwLib.Serializer.ParseXMLSafe(ReadFile("CT_Sgns.Sgns20ftx3.xml"));

            List<WagonType> sgnsWagons = CreateWagonTypes(template20, "20ft");


            await rwLib.VariantGenerator.CreateVariants(
                    container20,
                    sgnsWagons,
                    "ChrisTrains",
                    "RailSimulator",
                    "RailVehicles\\Freight\\NS Sgns\\{0}.xml",
                    "CT NS Sgns {0} 20ftx3 {1}",
                    "ChrisTrains\\RailSimulator\\RailVehicles\\Freight\\NS Sgns\\[Afirus]GW\\NS Sgns {0} {1}"
                );

            Console.WriteLine("Done!");
        }
    }
}
