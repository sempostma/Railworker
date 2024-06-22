using System.Xml.Linq;
using RWLib.RWBlueprints.Components;

namespace RWLib.RWBlueprints
{
    public class RWConsistFragmentBlueprint : RWConsistBlueprintAbstract
    {
        public RWConsistFragmentBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }
    }
}