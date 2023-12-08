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
        public EngineRailVehicleComponent RailVehicleComponent { get => new EngineRailVehicleComponent(this.xml.Element("RailVehicleComponent")!.Element("cEngineComponentBlueprint")!); }
        IRailVehicleComponent IRWRailVehicleBlueprint.RailVehicleComponent => RailVehicleComponent;

        public RWBlueprintID EngineSimulationBlueprint => GetEngineSimulationBlueprint();

        public RWEngineBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }

        private RWBlueprintID GetEngineSimulationBlueprint()
        {
            var blueprintXML = this.xml.Element("EngineSimulationContainer")!.Element("cEngineSimContainerBlueprint")!.Element("EngineSimFile")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;

            return RWBlueprintID.FromXML(blueprintXML);
        }

        public string Name => xml.Element("Name")!.Value.ToString();

        public RWDisplayName DisplayName
        {
            get
            {
                var element = xml.Descendants("DisplayName").FirstOrDefault();
                if (element == null) return null;
                else return new RWDisplayName(element!);
            }
        }
    }
}
