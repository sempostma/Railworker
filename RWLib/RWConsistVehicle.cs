using System.Xml.Linq;

namespace RWLib
{
    public class RWConsistVehicle
    {
        public string routeGuid;
        public string scenarioGuid;
        public string consistId;
        public XElement railVehicle;

        public RWBlueprintID BlueprintID { get => GetBlueprintID(); }
        public IConsistVehicleComponent Component { get => GetConsistVehicleComponent(); }

        public RWConsistVehicle(string routeGuid, string scenarioGuid, string consistId, XElement railVehicle)
        {
            this.routeGuid = routeGuid;
            this.scenarioGuid = scenarioGuid;
            this.consistId = consistId;
            this.railVehicle = railVehicle;
        }

        private RWBlueprintID GetBlueprintID()
        {
            XElement blueprintRoot = railVehicle.Element("BlueprintID")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
            XElement blueprintProviderSet = blueprintRoot.Element("BlueprintSetID")!.Element("iBlueprintLibrary-cBlueprintSetID")!;
            string provider = blueprintProviderSet.Element("Provider")!.Value.ToString();
            string product = blueprintProviderSet.Element("Product")!.Value.ToString();
            string path = blueprintRoot.Element("BlueprintID")!.Value.ToString();

            return new RWBlueprintID(provider, product, path);
        }

        private IConsistVehicleComponent GetConsistVehicleComponent()
        {
            if (this.railVehicle.Element("Component")!.Element("cWagon") != null)
            {
                return new RWConsistWagonComponent(this.railVehicle.Element("Component")!.Element("cWagon")!);
            } 
            else if (this.railVehicle.Element("Component")!.Element("cEngine") != null)
            {
                return new RWConsistEngineComponent(this.railVehicle.Element("Component")!.Element("cEngine")!);
            }
            else
            {
                throw new NotImplementedException("Consist vehicle type is not implemented");
            }
        }
    }
}