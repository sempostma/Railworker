using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public abstract class RWBlueprint : RWXml
    {
        public class RWBlueprintContext
        {
            public enum IsInApFile { Unknown, Yes, No }

            public IsInApFile InApFile { get; set; } = IsInApFile.Unknown;
            public string ApPath { get; internal set; } = "";
        }

        protected RWBlueprint(RWBlueprintID blueprintId, XElement blueprint, RWLibrary lib, RWBlueprintContext? context = null) : base(blueprint, lib)
        {
            this.BlueprintId = blueprintId;
            this.Context = context ?? new RWBlueprintContext();
        }

        public string XmlPath
        {
            get
            {
                return String.Format("{0}\\{1}\\{2}", Provider, Product, BlueprintId);
            }
        }

        public RWBlueprintID BlueprintId { get; protected set; }
        public RWBlueprintContext Context { get; }
        public string Provider => BlueprintId.Provider;
        public string Product => BlueprintId.Product;
        public string BlueprintIDPath => BlueprintId.Path;

        public RWRenderComponent RenderComponent { get => new RWRenderComponent(Xml.Element("RenderComponent")!.Element("cAnimObjectRenderBlueprint")!, lib); }
        public bool HasRenderComponent => Xml.Element("RenderComponent")?.Element("cAnimObjectRenderBlueprint") != null;

    }
}
