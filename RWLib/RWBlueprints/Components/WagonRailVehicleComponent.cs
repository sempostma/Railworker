using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class WagonRailVehicleComponent : IRailVehicleComponent
    {
        public XElement wagonRailVehicleComponent;

        public IBrakeAssembly TrainBrakeAssembly { get => GetTrainBrakeAssembly(); }

        public double Mass => Convert.ToDouble(wagonRailVehicleComponent.Element("Mass")!.Value);

        public WagonRailVehicleComponent(XElement wagonRailVehicleComponent)
        {
            this.wagonRailVehicleComponent = wagonRailVehicleComponent;
        }

        private IBrakeAssembly GetTrainBrakeAssembly()
        {
            var trainBrakes = wagonRailVehicleComponent.Element("TrainBrakeAssembly")!.Elements();

            foreach (var brake in trainBrakes)
            {
                switch(brake.Name.ToString())
                {
                    case "EngineSimulation-cTrainAirBrakeBlueprint":
                        var airBrake = brake.Element("BrakeType")!
                                .Element("EngineSimulation-cTrainAirBrakeDataBlueprint")!;

                         return new AirBrakeSimulation(airBrake);
                    default:
                        break;
                }
            }

            throw new InvalidDataException("Wagon rail vehicle component file does not contain an air brake component.");
        }
    }
}
