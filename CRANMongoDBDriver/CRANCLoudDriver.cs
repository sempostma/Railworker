using ComprehensiveRailworksArchiveNetwork;
using ComprehensiveRailworksArchiveNetwork.Drivers;
using ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem;

namespace CRANMongoDBDriver
{
    public class CRANCLoudDriver : IDriver
    {
        public Task<CreateAddonResult> CreateAddon(Addon addon)
        {
            throw new NotImplementedException();
        }

        public Task<Addon> SaveAddon(Addon newAddon)
        {
            throw new NotImplementedException();
        }

        public Task<Author> SaveAuthor(Author author)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Addon> SearchForAddons(string query, SearchOptions searchOptions)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Author> SearchForAuthors(string v, SearchOptions searchOptions)
        {
            throw new NotImplementedException();
        }
    }
}
