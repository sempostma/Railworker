using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.RWBlueprints.Interfaces
{
    public interface IRailVehicle
    {
        public IRailVehicleComponent RailVehicleComponent { get; }
        public String Name { get; }
    }
}
