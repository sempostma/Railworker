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
    public class RWEngineBlueprint : RWBlueprint, IRailVehicle
    {
        public EngineRailVehicleComponent RailVehicleComponent { get => new EngineRailVehicleComponent(this.xml.Element("RailVehicleComponent")!.Element("cEngineComponentBlueprint")!); }
        IRailVehicleComponent IRailVehicle.RailVehicleComponent => RailVehicleComponent;

        public RWBlueprintID EngineSimulationBlueprint => GetEngineSimulationBlueprint();

        public RWEngineBlueprint(XElement xElement) : base(xElement)
        {
        }

        private RWBlueprintID GetEngineSimulationBlueprint()
        {
            var blueprintXML = this.xml.Element("EngineSimulationContainer")!.Element("cEngineSimContainerBlueprint")!.Element("EngineSimFile")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;

            return RWBlueprintID.FromXML(blueprintXML);
        }
    }
}
