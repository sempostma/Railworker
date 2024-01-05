using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class Addon
    {
        public required Guid Guid { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required Author Author { get; set; }
        public required List<Collaborators> Credits { get; set; }
        public required List<AddonVariant> Variants { get; set; }
    }
}