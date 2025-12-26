using RWLib.RWBlueprints.Components;
using System.Xml.Linq;

namespace RWLib.RWBlueprints
{
    public class RWTenderBlueprint : RWBlueprint
    {
        public RWTenderBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprintId, blueprint, lib, context)
        {
        }

        public RWRenderComponent RenderComponent { get => new RWRenderComponent(Xml.Element("RenderComponent")!.Element("cAnimObjectRenderBlueprint")!, lib); }
        public bool HasRenderComponent => Xml.Element("RenderComponent")?.Element("cAnimObjectRenderBlueprint") != null;
    }
}