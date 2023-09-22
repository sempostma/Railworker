using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class EngineRailVehicleComponent : IRailVehicleComponent
    {
        public XElement engineRailVehicleComponent;

        public double Mass => Convert.ToDouble(engineRailVehicleComponent.Element("Mass")!.Value);

        public EngineRailVehicleComponent(XElement engineRailVehicleComponent)
        {
            if (engineRailVehicleComponent == null)
            {
                throw new ArgumentNullException("Arugment can not be null");
            }

            this.engineRailVehicleComponent = engineRailVehicleComponent;
        }
    }
}
