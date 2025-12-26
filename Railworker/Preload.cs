using Railworker.Core;
using RWLib.RWBlueprints;
using static RWLib.RWBlueprints.RWConsistBlueprintAbstract;
using System.Collections.Generic;

namespace Railworker
{
    public class Preload
    {
        public required RWConsistBlueprint RWConsist { get; set; }
    }
}