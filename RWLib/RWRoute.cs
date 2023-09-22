using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public class RWRoute
    {
        public XDocument routeProperties;
        public string guid;

        public RWRoute(XDocument routeProperties, string guid)
        {
            this.routeProperties = routeProperties;
            this.guid = guid;
        }
    }
}
