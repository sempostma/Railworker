using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem
{
    [XmlRoot(Namespace = "http://esstudio.nl/railworker")]
    public class AddonCollection
    {
        public List<Addon> Addons { get; set; }
    }
}
