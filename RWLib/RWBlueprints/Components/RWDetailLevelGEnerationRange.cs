using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWDetailLevelGEnerationRange : RWXml
    {
        public RWDetailLevelGEnerationRange(XElement blueprint, RWLibrary lib) : base(blueprint, lib)
        {
        }

        public int HighestLevel => (int)xml.Element("cSceneryRenderBlueprint-sDetailLevelGenerationRange")!.Element("HighestLevel_1isHighest")!;
        public int LowestLevel => (int)xml.Element("cSceneryRenderBlueprint-sDetailLevelGenerationRange")!.Element("LowestLevel_10isLowest")!;
    }
}
