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
                var element = xml.Element("PrimaryNamedTextureSet")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
                return RWBlueprintID.FromXML(element);
         } }

        public RWBlueprintID SecondaryNamedTextureSet
        {
            get
            {
                var element = xml.Element("SecondaryNamedTextureSet")!.Element("iBlueprintLibrary-cAbsoluteBlueprintID")!;
                return RWBlueprintID.FromXML(element);
            }
        }

        public string GeometryID => xml.Element("GeometryID")?.Value ?? "";
        public string GeometryFilename => FormatGeoFilename(GeometryID);
        public string CollisionGeometryID => xml.Element("GeometryID")?.Value ?? "";
        public string CollisionGeometryFilename => FormatGeoFilename(CollisionGeometryID);
        public bool Pickable => xml.Element("GeometryID")?.Value == "eTrue";
        public bool CastsShadows => xml.Element("GeometryID")?.Value == "eTrue";
        public RenderShadowType ShadowType => Enum.Parse<RenderShadowType>(xml.Element("ShadowType")!.Value);
        public RenderViewType ViewType => Enum.Parse<RenderViewType>(xml.Element("ViewType")!.Value);
        public bool Palettised => xml.Element("Palettised")?.Value == "eTrue";
        public int Palette0Index => (int)xml.Element("Palette0Index")!;
        public int Palette1Index => (int)xml.Element("Palette1Index")!;
        public int Palette2Index => (int)xml.Element("Palette2Index")!;
        //public int HeatHaze => (int)xml.Element("HeatHaze")!;
        //public int TexText => (int)xml.Element("TexText")!;
        //public int ProjectedLightElement => (int)xml.Element("ProjectedLightElement")!;
        //public int HeatHaze => (int)xml.Element("HeatHaze")!;
        public bool Instancable => (bool)xml.Element("Instancable")!;
        public RWDetailLevelGEnerationRange DetailLevelGenerationRange => new RWDetailLevelGEnerationRange(
            xml.Element("DetailLevelGenerationRange")!,
            lib
        );
        //public int AnimSet => (int)xml.Element("AnimSet")!;

        public bool DoesGeometryGeoPcdxExist => lib.BlueprintLoader.DoesFileExist(GeometryFilename);
        public bool DoesCollisionGeometryGeoPcdxExist => lib.BlueprintLoader.DoesFileExist(CollisionGeometryFilename);

        private string FormatGeoFilename(string filename)
        {
            return filename.Replace("[00]", "") + ".GeoPcDx";
        }
    }
}
