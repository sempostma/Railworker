using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public abstract class RWXml
    {
        [JsonIgnore]
        public XElement Xml { get; set; }
        [JsonIgnore]
        public RWLibrary lib;

        public RWXml(XElement blueprint, RWLibrary lib)
        {
            this.lib = lib;
            Xml = blueprint;
        }

        public string XMLElementName { get => Xml.Name.ToString(); }
    }
}
