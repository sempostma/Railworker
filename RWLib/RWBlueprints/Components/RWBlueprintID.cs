using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Components
{
    public class RWBlueprintID
    {
        public string Provider { get; set; }
        public string Product { get; set; }
        public string Path { get; set; }

        public RWBlueprintID(string provider, string product, string path)
        {
            Provider = provider;
            Product = product;
            Path = path;
        }

        public string CombinedPath => GetRelativeFilePathFromAssetsFolder();

        public string GetRelativeFilePathFromAssetsFolder()
        {
            return System.IO.Path.Combine(Provider, Product, Path.Replace("/",  "\\"));
        }

        public static RWBlueprintID FromXML(XElement blueprintXML)
        {
            XElement blueprintProviderSet = blueprintXML.Element("BlueprintSetID")!.Element("iBlueprintLibrary-cBlueprintSetID")!;
            string provider = blueprintProviderSet.Element("Provider")!.Value.ToString();
            string product = blueprintProviderSet.Element("Product")!.Value.ToString();
            string path = blueprintXML.Element("BlueprintID")!.Value.ToString();

            return new RWBlueprintID(provider, product, path);
        }

        public static RWBlueprintID FromFilenameRelativeToAssetsDirectory(string filename)
        {
            var sections = filename.Split(System.IO.Path.DirectorySeparatorChar);
            var provider = sections.Length >= 1 ? sections[0] : "";
            var product = sections.Length >= 2 ? sections[1] : "";
            var hasProductPath = sections.Length >= 2;
            var productPath = string.Join('/', sections.Skip(2));
            return new RWBlueprintID(provider, product, productPath);
        }

        public override string ToString()
        {
            return GetRelativeFilePathFromAssetsFolder();
        }
    }
}
