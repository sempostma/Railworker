
namespace ComprehensiveRailworksArchiveNetwork.Tasks
{
    public class ExecuteBat : InstallationTask
    {
        public bool DeleteAfterwards { get; set; }
        public string FilePathRelativeToAssetsFolder { get; set; }
    }
}
