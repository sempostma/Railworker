using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWDieselElectricEngineSimulationBlueprint : RWBlueprint, IRWEngineSimulationBlueprint
    {
        public IBrakeAssembly TrainBrakeAssembly { get => GetTrainBrakeAssembly(); }

        public RWDieselElectricEngineSimulationBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }

        private IBrakeAssembly GetTrainBrakeAssembly()
        {
            var trainBrakes = Xml
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
