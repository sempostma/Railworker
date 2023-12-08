using ComprehensiveRailworksArchiveNetwork.Tasks;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class AddonVersion
    {
        public VersionNumber VersionNumber { get; set; }
        public List<Dependency> Dependencies { get; set; }
        public List<string> Changes { get; set; }
        public List<ReadmeFile> ReadmeFiles { get; set; }

        /// <summary>
        /// Ideally an addon version should only include one RWP file or one Exe file but you never know what weird things exist in userland so keep it as it is.
        /// </summary>
        public List<RWPFile> RWPFiles { get; set; }
        public List<ExeFile> InstallerFiles { get; set; }

        public List<InstallationTask> PreInstallationTask { get; set; }
        public List<InstallationTask> PostInstallationTask { get; set; }
        public bool PendingApproval { get; set; } = false;
        public bool Submitted { get; set; } = false;
    }
}