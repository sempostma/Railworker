using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public class RWBlueprintID
    {
        public string provider;
        public string product;
        public string path;

        public RWBlueprintID(string provider, string product, string path)
        {
            this.provider = provider;
            this.product = product;
            this.path = path;
        }

        public string GetRelativeFilePathFromAssetsFolder()
        {
            return Path.Combine(provider, product, path);
        }

        public static RWBlueprintID FromXML(XElement blueprintXML)
        {
            XElement blueprintProviderSet = blueprintXML.Element("BlueprintSetID")!.Element("iBlueprintLibrary-cBlueprintSetID")!;
            string provider = blueprintProviderSet.Element("Provider")!.Value.ToString();
            string product = blueprintProviderSet.Element("Product")!.Value.ToString();
            string path = blueprintXML.Element("BlueprintID")!.Value.ToString();

            return new RWBlueprintID(provider, product, path);
        }
    }
}
