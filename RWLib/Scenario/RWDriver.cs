using RWLib.RWBlueprints.Components;
using System.Xml.Linq;

namespace RWLib.Scenario
{
    public class RWDriver : RWXml
    {
        public enum ServiceClassType
        {
            Special = 0,
            LightEngine = 1,
            ExpressPassenger = 2,
            StoppingPassenger = 3,
            HighSpeedFreight = 4,
            StandardFreight = 5,
            LowSpeedFreight = 6,
            OtherFreight = 7,
            EmptyStock = 9,
            International = 10
        }

        public RWDriver(XElement xElement, RWLibrary lib) : base(xElement, lib)
        {
        }

        public bool PlayerDriver => (bool?)Xml.Element("PlayerDriver") ?? false;
        public RWDisplayName ServiceName => new RWDisplayName(Xml.Element("ServiceName")!);
        public double StartTime => (double?)Xml.Element("StartTime") ?? 0;
        public double StartSpeed => (double?)Xml.Element("StartSpeed") ?? 0;
        public double EndSpeed => (double?)Xml.Element("EndSpeed") ?? 0;
        public double ExpectedPerformance => (double?)Xml.Element("ExpectedPerformance") ?? 0;
        public ServiceClassType ServiceClass => (ServiceClassType)((int?)Xml.Element("ServiceClass") ?? 0);
        public bool PlayerControlled => (bool?)Xml.Element("PlayerControlled") ?? false;
        public string PriorPathingStatus => Xml.Element("PriorPathingStatus")?.Value ?? "";
        public string PathingStatus => Xml.Element("PathingStatus")?.Value ?? "";
        public double RepathIn => (double?)Xml.Element("RepathIn") ?? 0;
        public double ForcedRepath => (double?)Xml.Element("ForcedRepath") ?? 0;
        public bool OffPath => (bool?)Xml.Element("OffPath") ?? false;
        public double StartTriggerDistanceFromPlayerSquared => (double?)Xml.Element("StartTriggerDistanceFromPlayerSquared") ?? 0;
        public bool UnloadedAtStart => (bool?)Xml.Element("UnloadedAtStart") ?? false;

        private IEnumerable<RWDriverInstruction> GetDriverInstructions()
        {
            var element = Xml.Element("DriverInstructionContainer")!.Element("cDriverInstructionContainer")!.Element("DriverInstruction")!;

            foreach (var instruction in element.Elements())
            {
                var xmlElementName = instruction.Name.ToString();
                switch (xmlElementName)
                {
                    case "cStopAtDestinations":
                        {
                            var stopAtDestination = new RWStopAtDestinationDriverInstruction(instruction, lib);
                            yield return stopAtDestination;
                            break;
                        }

                }
            }
        }

        public IEnumerable<RWDriverInstruction> DriverInstruction => GetDriverInstructions();

    }
}