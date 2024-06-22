using ComprehensiveRailworksArchiveNetwork.Tasks;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class AddonVersion
    {
        public required VersionNumber VersionNumber { get; set; }
        public required List<Dependency> Dependencies { get; set; }
        public required List<string> Changes { get; set; }
        public required List<ReadmeFile> ReadmeFiles { get; set; }

        /// <summary>
        /// Ideally an addon version should only include one RWP file or one Exe file but you never know what weird things exist in userland so keep it as it is.
        /// </summary>
        public required List<RWPFile> RWPFiles { get; set; }
        public required List<ExeFile> InstallerFiles { get; set; }
        public required List<string> FileList { get; set; }
        public required string Url { get; set; }

        [XmlArrayItem(typeof(CopyDirectory))]
        [XmlArrayItem(typeof(CopyFile))]
        [XmlArrayItem(typeof(ExecuteBat))]
        [XmlArrayItem(typeof(MoveDirectory))]
        [XmlArrayItem(typeof(MoveFile))]
        public required List<InstallationTask> PreInstallationTask { get; set; }

        [XmlArrayItem(typeof(CopyDirectory))]
        [XmlArrayItem(typeof(CopyFile))]
        [XmlArrayItem(typeof(ExecuteBat))]
        [XmlArrayItem(typeof(MoveDirectory))]
        [XmlArrayItem(typeof(MoveFile))]
        public required List<InstallationTask> PostInstallationTask { get; set; }
        public required bool PendingApproval { get; set; } = false;
        public required bool Submitted { get; set; } = false;
    }
}