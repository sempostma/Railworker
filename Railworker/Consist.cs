using RWLib.Scenario;

namespace Railworker
{
    public class Consist
    {
        public required RWConsist RWConsist { get; set; }

        public bool IsPlayer { get => RWConsist.IsPlayer; }
    }
}