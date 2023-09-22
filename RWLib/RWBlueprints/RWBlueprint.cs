using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public abstract class RWBlueprint
    {
        public XElement xml;

        public RWBlueprint(XElement blueprint)
        {
            this.xml = blueprint;
        }

        public string XMLElementName { get => xml.Name.ToString(); }

        public string Name => xml.Element("Name")!.Value.ToString();
    }
}
