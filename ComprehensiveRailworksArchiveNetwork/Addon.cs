using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class Addon
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Author Author { get; set; }
        public List<Collaborators> Credits { get; set; }
        public List<AddonVariant> Variants { get; set; }
    }
}