using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    
    public class Dependency
    {
        public string AddonGuid { get; set; }
        public Guid? ForceVariantGuid { get; set; }
        public bool IsVariantForce => ForceVariantGuid != null;
        public int? ForceMajorVersion { get; set; }
        public bool IsMajorVersionForced => ForceMajorVersion != null;
        public int? ForceMinorVersion { get; set; }
        public bool IsMinorVersionForced => ForceMinorVersion != null;
        public int? ForcePatchVersion { get; set; }
        public bool IsPatchVersionForced => ForcePatchVersion != null;
    }
}