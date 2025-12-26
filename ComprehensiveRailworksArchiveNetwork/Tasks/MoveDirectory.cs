using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ComprehensiveRailworksArchiveNetwork.Tasks
{
    public class MoveDirectory : InstallationTask
    {
        public bool Overwrite { get; set; }
        public string OriginDirectoryPathRelativeToAssetsFolder { get; set; }
        public string DestinationDirectoryPathRelativeToAssetsFolder { get; set; }
    }
}
