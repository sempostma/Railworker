using RWLib;

namespace Railworker
{
    public class Vehicle : ViewModel
    {
        public required RWBlueprint RWBlueprint { get; set; }

        public enum VehicleType
        {
            Unknown,
            Engine,
            Wagon,
            Tender
        }
    }
}