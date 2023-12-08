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

        public bool PlayerDriver => (bool?)xml.Element("PlayerDriver") ?? false;
        public RWDisplayName ServiceName => new RWDisplayName(xml.Element("ServiceName")!);
        public double StartTime => (double?)xml.Element("StartTime") ?? 0;
        public double StartSpeed => (double?)xml.Element("StartSpeed") ?? 0;
        public double EndSpeed => (double?)xml.Element("EndSpeed") ?? 0;
        public double ExpectedPerformance => (double?)xml.Element("ExpectedPerformance") ?? 0;
        public ServiceClassType ServiceClass => (ServiceClassType)((int?)xml.Element("ServiceClass") ?? 0);
        public bool PlayerControlled => (bool?)xml.Element("PlayerControlled") ?? false;
        public string PriorPathingStatus => xml.Element("PriorPathingStatus")?.Value ?? "";
        public string PathingStatus => xml.Element("PathingStatus")?.Value ?? "";
        public double RepathIn => (double?)xml.Element("RepathIn") ?? 0;
        public double ForcedRepath => (double?)xml.Element("ForcedRepath") ?? 0;
        public bool OffPath => (bool?)xml.Element("OffPath") ?? false;
        public double StartTriggerDistanceFromPlayerSquared => (double?)xml.Element("StartTriggerDistanceFromPlayerSquared") ?? 0;
        public bool UnloadedAtStart => (bool?)xml.Element("UnloadedAtStart") ?? false;

        private IEnumerable<RWDriverInstruction> GetDriverInstructions()
        {
            var element = xml.Element("DriverInstructionContainer")!.Element("cDriverInstructionContainer")!.Element("DriverInstruction")!;

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