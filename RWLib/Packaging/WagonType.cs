using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.Packaging
{
    public class WagonType
    {
        public string Type { get; set; } = "";
        public string InGameType { get; set; } = "";
        public bool KeepAutoNumbering { get; set; } = false;
        public List<BlueprintTemplate> BlueprintTemplates= new List<BlueprintTemplate>();

        public class BlueprintTemplate : MatrixTransformable
        {
            public XDocument XDocument { get; set; } = new XDocument();
            public string Label { get; set; } = "";
            public string GeoFileName { get; set; } = "";
            public bool InvertZ { get; set; }
        }
    }
}
