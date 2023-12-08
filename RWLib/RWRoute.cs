using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RWLib
{
    public class RWRoute : RWXml
    {
        public XDocument routeProperties;
        public string guid;

        public RWRoute(XDocument routeProperties, string guid, RWLibrary lib) : base(routeProperties.Root!, lib)
        {
            this.routeProperties = routeProperties;
            this.guid = guid;
        }

        public RWRouteOrigin RouteOrigin { 
            get => new RWRouteOrigin(routeProperties.Root
                ?.Element("MapProjection")
                ?.Element("cMapProjectionOwner")
                ?.Element("MapProjection")
                ?.Element("cUTMMapProjection")!, lib); 
        }

        public RWDisplayName? DisplayName
        {
            get
            {
                var element = xml.Descendants("DisplayName").FirstOrDefault();
                if (element == null) return null;
                else return new RWDisplayName(element!);
            }
        }
    }
}
