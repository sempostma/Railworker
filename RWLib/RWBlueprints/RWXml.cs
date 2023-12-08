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
        public XElement xml;
        public RWLibrary lib;

        public RWXml(XElement blueprint, RWLibrary lib)
        {
            this.lib = lib;
            xml = blueprint;
        }

        public string XMLElementName { get => xml.Name.ToString(); }
    }
}
