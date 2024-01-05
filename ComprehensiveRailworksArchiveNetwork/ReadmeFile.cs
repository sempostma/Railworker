using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    
    public class ReadmeFile : AddonFile
    {
        public enum FileType { PDF, TXT, Word }

        public FileType ReadmeType { get; set; }

        public required string OptionalStoragePathInManuals { get; set; }
    }
}