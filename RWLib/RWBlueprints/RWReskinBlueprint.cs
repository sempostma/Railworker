using System.Xml.Linq;
using RWLib.RWBlueprints.Components;

namespace RWLib.RWBlueprints
{
    internal class RWReskinBlueprint : RWBlueprint
    {
        public RWReskinBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }
    }
}