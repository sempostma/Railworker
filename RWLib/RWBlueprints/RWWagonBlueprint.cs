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
    public class RWWagonBlueprint : RWBlueprint, IRWRailVehicleBlueprint
    {
        public WagonRailVehicleComponent RailVehicleComponent { get => new WagonRailVehicleComponent(this.xml.Element("RailVehicleComponent")!.Element("cWagonComponentBlueprint")!); }
        IRailVehicleComponent IRWRailVehicleBlueprint.RailVehicleComponent => RailVehicleComponent;

        public RWWagonBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
            
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
