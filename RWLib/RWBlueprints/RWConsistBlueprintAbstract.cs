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
    public abstract class RWConsistBlueprintAbstract : RWBlueprint
    {
        public RWConsistBlueprintAbstract(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }

        public class RWConsistEntry
        {
            public required RWBlueprintID BlueprintName { get; set; }
            public required bool Flipped { get; set; }
        }

        public IEnumerable<RWConsistEntry> Entries
        {
            get
            {
                foreach (var entry in Xml.Descendants("cConsistEntry"))
                {
                    var blueprintNameXml = entry.Element("BlueprintName")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
                    var blueprintName = RWBlueprintID.FromXML(blueprintNameXml);
                    var flipped = entry.Element("Flipped")!.Value == "eTrue";

                    yield return new RWConsistEntry { BlueprintName = blueprintName, Flipped = flipped };
                }
            }
        }
    }
}
