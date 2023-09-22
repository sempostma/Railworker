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
    public class RWWagonBlueprint : RWBlueprint, IRailVehicle
    {
        public WagonRailVehicleComponent RailVehicleComponent { get => new WagonRailVehicleComponent(this.xml.Element("RailVehicleComponent")!.Element("cWagonComponentBlueprint")!); }
        IRailVehicleComponent IRailVehicle.RailVehicleComponent => RailVehicleComponent;

        public RWWagonBlueprint(XElement xElement) : base(xElement)
        {
            
        }
    }
}
