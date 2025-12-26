using Railworker.Core;
using Railworker.Language;
using RWLib.RWBlueprints.Components;
using RWLib.Scenario;

namespace Railworker
{
    public class Consist
    {
        public required RWConsist RWConsist { get; set; }

        public string Name => RWConsist.Driver != null ? Utilities.DetermineDisplayName(RWConsist.ServiceName) : Language.Resources.loose_consist;
        public bool IsPlayer { get => RWConsist.IsPlayer; }
    }
}