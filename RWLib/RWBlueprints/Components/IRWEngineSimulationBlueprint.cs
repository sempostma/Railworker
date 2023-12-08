using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.RWBlueprints.Components
{
    public interface IRWEngineSimulationBlueprint
    {
        public IBrakeAssembly TrainBrakeAssembly { get; }
    }
}
