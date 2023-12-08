using System.Xml.Linq;

namespace RWLib.Scenario
{
    public class RWConsist
    {
        public string routeGuid;
        public string scenarioGuid;
        public XElement consistElement;
        private RWLibrary lib;

        public bool IsPlayer => consistElement.Element("Driver")?.Element("cDriver")?.Element("PlayerDriver")?.Value == "1";
        public bool IsLooseConsist => consistElement.Element("Driver")?.Element("cDriver") == null;
        public RWDriver Driver => new Lazy<RWDriver>(() => new RWDriver(consistElement.Element("Driver")?.Element("cDriver")!, lib)).Value;

        public RWConsist(string routeGuid, string scenarioGuid, XElement consistElement, RWLibrary lib)
        {
            this.routeGuid = routeGuid;
            this.scenarioGuid = scenarioGuid;
            this.consistElement = consistElement;
            this.lib = lib;
        }

        public string Id { get => consistElement.Attribute(RWUtils.KujuNamspace + "id")!.ToString(); }
        public IEnumerable<RWConsistVehicle> Vehicles { get => GetVehicles(); }

        private IEnumerable<RWConsistVehicle> GetVehicles()
        {
            var railVehicles = consistElement.Element("RailVehicles")!.Elements();
            foreach (var railVehicle in railVehicles)
            {
                if (railVehicle.Name.ToString() != "cOwnedEntity")
                {
                    throw new InvalidOperationException($"Invalid consist in scenario '{scenarioGuid}', route '{routeGuid}'");
                }
                else
                {
                    yield return new RWConsistVehicle(routeGuid, scenarioGuid, Id, railVehicle);
                }
            }
        }
    }
}