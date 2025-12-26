using System.Xml.Linq;
using RWLib.Scenario;

namespace RWLib
{
    internal class RWStopAtDestinationDriverInstruction : RWDriverInstruction
    {
        public RWStopAtDestinationDriverInstruction(XElement xElement, RWLibrary lib) : base(xElement, lib)
        {
        }
    }
}