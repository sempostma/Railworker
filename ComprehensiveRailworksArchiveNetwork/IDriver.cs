using ComprehensiveRailworksArchiveNetwork.Drivers;
using ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem;

namespace ComprehensiveRailworksArchiveNetwork
{
    public interface IDriver
    {
        Task<CreateAddonResult> CreateAddon(Addon addon);
        Task<Addon> SaveAddon(Addon newAddon);
        Task<Author> SaveAuthor(Author author);
        IAsyncEnumerable<Addon> SearchForAddons(string query, SearchOptions searchOptions);
        IAsyncEnumerable<Author> SearchForAuthors(string v, SearchOptions searchOptions);
    }
}