using ComprehensiveRailworksArchiveNetwork.Drivers;
using ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem;

namespace ComprehensiveRailworksArchiveNetwork
{
    public interface IDriver
    {
        Task<CreateAddonResult> CreateAddon(Addon addon);
        IAsyncEnumerable<Addon> SearchForAddons(string query, SearchOptions searchOptions);
    }
}