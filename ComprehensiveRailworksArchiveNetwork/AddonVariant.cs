using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class AddonVariant
    {
        public Guid Guid { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public List<AddonVersion> Versions { get; set; }
    }
}