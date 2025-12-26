using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RWLib.Scenario.Interfaces;

namespace RWLib.Scenario.Components
{
    public class RWConsistWagonComponent : IConsistVehicleComponent
    {
        public double TotalMass => Convert.ToDouble(railVehicleComponent.Element("TotalMass")!.Value.ToString());

        public XElement railVehicleComponent;

        public RWConsistWagonComponent(XElement railVehicle)
        {
            railVehicleComponent = railVehicle;
        }
    }
}
