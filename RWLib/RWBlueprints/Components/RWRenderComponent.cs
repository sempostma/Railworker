using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWRenderComponent : RWXml
    {
        public enum RenderShadowType { eShadowType_Blobby }
        public enum RenderViewType { ExternalView }

        public RWRenderComponent(XElement blueprint, RWLibrary lib) : base(blueprint, lib)
        {

        }

        public RWBlueprintID PrimaryNamedTextureSet { get
            {
                var element = Xml.Element("PrimaryNamedTextureSet")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
                return RWBlueprintID.FromXML(element);
         } }

        public RWBlueprintID SecondaryNamedTextureSet
        {
            get
            {
                var element = Xml.Element("SecondaryNamedTextureSet")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
                return RWBlueprintID.FromXML(element);
            }
        }

        public string GeometryID => Xml.Element("GeometryID")?.Value ?? "";
        public string GeometryFilename => FormatGeoFilename(GeometryID);
        public string CollisionGeometryID => Xml.Element("GeometryID")?.Value ?? "";
        public string CollisionGeometryFilename => FormatGeoFilename(CollisionGeometryID);
        public bool Pickable => Xml.Element("GeometryID")?.Value == "eTrue";
        public bool CastsShadows => Xml.Element("GeometryID")?.Value == "eTrue";
        public RenderShadowType ShadowType => Enum.Parse<RenderShadowType>(Xml.Element("ShadowType")!.Value);
        public RenderViewType ViewType => Enum.Parse<RenderViewType>(Xml.Element("ViewType")!.Value);
        public bool Palettised => Xml.Element("Palettised")?.Value == "eTrue";
        public int Palette0Index => (int)Xml.Element("Palette0Index")!;
        public int Palette1Index => (int)Xml.Element("Palette1Index")!;
        public int Palette2Index => (int)Xml.Element("Palette2Index")!;
        //public int HeatHaze => (int)Xml.Element("HeatHaze")!;
        //public int TexText => (int)Xml.Element("TexText")!;
        //public int ProjectedLightElement => (int)Xml.Element("ProjectedLightElement")!;
        //public int HeatHaze => (int)Xml.Element("HeatHaze")!;
        public bool Instancable => (bool)Xml.Element("Instancable")!;
        public RWDetailLevelGEnerationRange DetailLevelGenerationRange => new RWDetailLevelGEnerationRange(
            Xml.Element("DetailLevelGenerationRange")!,
            lib
        );
        //public int AnimSet => (int)Xml.Element("AnimSet")!;

        public bool DoesGeometryGeoPcdxExist => lib.BlueprintLoader.DoesFileExist(GeometryFilename);
        public bool DoesCollisionGeometryGeoPcdxExist => lib.BlueprintLoader.DoesFileExist(CollisionGeometryFilename);

        private string FormatGeoFilename(string filename)
        {
            return filename.Replace("[00]", "") + ".GeoPcDx";
        }
    }
}
