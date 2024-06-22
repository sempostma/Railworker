using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RWLib
{
    public class RWPreloadEntry
    {
        public virtual bool Found { get; }
        public required RWBlueprintID BlueprintName { get; set; }
        public required bool Flipped { get; set; }
    }

    public class RWPreloadEntryFound : RWPreloadEntry
    {
        [XmlIgnore]
        [JsonIgnore]
        public required RWBlueprint Blueprint { get; set; }
        public override bool Found { get; } = true;
    }
    public class RWPreloadEntryNotFound : RWPreloadEntry
    {
        public override bool Found { get; } = false;
    }
}
