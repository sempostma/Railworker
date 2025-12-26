using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public class RWEngineBlueprint : RWBlueprint, IRWRailVehicleBlueprint
    {
        public EngineRailVehicleComponent RailVehicleComponent { get => new EngineRailVehicleComponent(this.Xml.Element("RailVehicleComponent")!.Element("cEngineComponentBlueprint")!); }
        IRailVehicleComponent IRWRailVehicleBlueprint.RailVehicleComponent => RailVehicleComponent;

        public RWBlueprintID EngineSimulationBlueprint => GetEngineSimulationBlueprint();

        public RWEngineBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }

        private RWBlueprintID GetEngineSimulationBlueprint()
        {
            var blueprintXML = this.Xml.Element("EngineSimulationContainer")!.Element("cEngineSimContainerBlueprint")!.Element("EngineSimFile")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;

            return RWBlueprintID.FromXML(blueprintXML);
        }

        public string Name => Xml.Element("Name")!.Value.ToString();

        public RWDisplayName DisplayName
        {
            get
            {
                var element = Xml.Descendants("DisplayName").FirstOrDefault();
                if (element == null) return null;
                else return new RWDisplayName(element!);
            }
        }

        public RWRenderComponent RenderComponent { get => new RWRenderComponent(Xml.Element("RenderComponent")!.Element("cAnimObjectRenderBlueprint")!, lib); }
        public bool HasRenderComponent => Xml.Element("RenderComponent")?.Element("cAnimObjectRenderBlueprint") != null;
    }
}
