using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class AddonVariant
    {
        public required Guid Guid { get; set; }
        public required string Label { get; set; }
        public required string Description { get; set; }
        public required List<AddonVersion> Versions { get; set; }
    }
}