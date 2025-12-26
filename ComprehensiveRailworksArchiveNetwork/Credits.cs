using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    
    public class Collaborator
    {
        public required string CreditsText { get; set; }
        public required Author Author { get;set; }
    }
}