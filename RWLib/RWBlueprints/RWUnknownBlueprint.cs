using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RWLib.RWBlueprints.Components;

namespace RWLib.RWBlueprints
{
    public class RWUnknownBlueprint : RWBlueprint
    {
        public RWUnknownBlueprint(RWBlueprintID blueprintID, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintID, blueprint, lib, context)
        {

        }
    }
}
