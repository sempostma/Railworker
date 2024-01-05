using ComprehensiveRailworksArchiveNetwork.Tasks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem
{
    public class FileSystemDriver : IDriver
    {
        private string filename;

        public FileSystemDriver(string filename)
        {
            this.filename = filename;
            if (File.Exists(filename) == false)
            {
                var collection = CreateBrandNewCollection();
                StoreCollection(collection);
            }
        }

        private AddonCollection CreateBrandNewCollection()
        {
            var collection = new AddonCollection
            {
                Addons = new List<Addon>()
            };
            return collection;
        }

        private void StoreCollection(AddonCollection collection)
        {
            XmlSerializer ser = new XmlSerializer(typeof(AddonCollection));
            using (var stream = File.OpenWrite(filename))
            {
                ser.Serialize(stream, collection);
            }
        }

        private AddonCollection ReadCollection()
        {
            XmlSerializer ser = new XmlSerializer(typeof(AddonCollection));
            using (TextReader reader = new StreamReader(filename))
            {
                var addonCollection = ser.Deserialize(reader) as AddonCollection;
                if (addonCollection == null) return CreateBrandNewCollection();
                else return addonCollection;
            }
        }

        public async IAsyncEnumerable<Addon> SearchForAddons(string query, SearchOptions searchOptions)
        {
            var collection = ReadCollection();

            foreach (var addon in collection.Addons)
            {
                if (addon.Name.ToLower().Contains(query.ToLower()))
                {
                    yield return addon;
                }
            }

            StoreCollection(collection);
        }

        public async Task<CreateAddonResult> CreateAddon(Addon addon)
        {
            var collection = ReadCollection();
            collection.Addons.Add(addon);

            StoreCollection(collection);

            return new CreateAddonResult { Success = true };
        }
    }
}
