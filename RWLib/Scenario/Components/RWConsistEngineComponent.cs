using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RWLib.Scenario.Interfaces;

namespace RWLib.Scenario.Components
{
    public class RWConsistEngineComponent : IConsistVehicleComponent
    {
        private XElement railVehicleComponent;

        public double TotalMass => Convert.ToDouble(railVehicleComponent.Element("TotalMass")!.Value.ToString());

        public RWConsistEngineComponent(XElement railVehicle)
        {
            railVehicleComponent = railVehicle;
        }
    }
}
