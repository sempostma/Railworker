using RWLib.RWBlueprints.Components;
using System.Xml.Linq;

namespace RWLib.RWBlueprints
{
    internal class RWTenderBlueprint : RWBlueprint
    {
        public RWTenderBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }
    }
}