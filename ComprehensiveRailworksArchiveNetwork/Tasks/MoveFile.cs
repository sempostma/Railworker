using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ComprehensiveRailworksArchiveNetwork.Tasks
{
    public class MoveFile : InstallationTask
    {
        public bool Overwrite { get; set; }
        public string OriginFilePathRelativeToAssetsFolder { get; set; }
        public string DestinationFilePathRelativeToAssetsFolder { get; set; }
    }
}
