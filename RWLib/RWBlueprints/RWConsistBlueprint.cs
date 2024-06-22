using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RWLib.RWBlueprint;
using System.Xml.Linq;

namespace RWLib.RWBlueprints
{
    public class RWConsistBlueprint : RWConsistBlueprintAbstract
    {
        public RWConsistBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }
    }
}
