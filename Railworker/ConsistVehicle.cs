using RWLib.Scenario;

namespace Railworker
{
    public class ConsistVehicle : ViewModel
    {
        public enum ScenarioVehicleExistance
        {
            Exists,
            Missing,
            MissingButInPreset,
            Unknown
        }

        public required RWConsistVehicle RWConsistVehicle { get; set; }

        public string Path => RWConsistVehicle.BlueprintID.CombinedPath;

        public string Name => RWConsistVehicle.Name;
    }
}