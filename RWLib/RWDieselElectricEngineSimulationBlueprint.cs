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
    public class RWDieselElectricEngineSimulationBlueprint : RWBlueprint, IRWEngineSimulationBlueprint
    {
        public IBrakeAssembly TrainBrakeAssembly { get => GetTrainBrakeAssembly(); }

        public RWDieselElectricEngineSimulationBlueprint(XElement xElement) : base(xElement)
        {
        }

        private IBrakeAssembly GetTrainBrakeAssembly()
        {
            var trainBrakes = this.xml
                .Element("EngineSimComponent")!
                .Element("cEngineSimComponentBlueprint")!
                .Element("SubSystem")!
                .Element("EngineSimulation-cDieselElectricSubSystemBlueprint")!
                .Element("TrainBrakeAssembly")!.Elements();

            foreach (var brake in trainBrakes)
            {
                switch (brake.Name.ToString())
                {
                    case "EngineSimulation-cTrainAirBrakeBlueprint":
                        var airBrake = brake.Element("BrakeType")!
                                .Element("EngineSimulation-cTrainAirBrakeDataBlueprint")!;

                        return new AirBrakeSimulation(airBrake);
                    default:
                        break;
                }
            }

            throw new InvalidDataException("Engine simulation file does not contain an air brake component.");
        }
    }
}
