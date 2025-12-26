using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class AirBrakeSimulation : IBrakeAssembly
    {
        public XElement airBrakeSimulation;

        public double MaxForcePercentOfVehicleWeight { get => Convert.ToDouble(airBrakeSimulation.Element("MaxForcePercentOfVehicleWeight")!.Value); }

        public AirBrakeSimulation(XElement airBrakeSimulation)
        {
            this.airBrakeSimulation = airBrakeSimulation;
        }
    }
}
