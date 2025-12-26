using System.Xml.Linq;

namespace RWLib.Scenario
{
    public abstract class RWDriverInstruction : RWXml
    {
        public RWDriverInstruction(XElement xElement, RWLibrary lib) : base(xElement, lib)
        {
        }
    }
}