using ComprehensiveRailworksArchiveNetwork;
using ComprehensiveRailworksArchiveNetwork.Drivers.FileSystem;
using RWLib;
using System.IO;

namespace Railworker.Core
{
    internal class AppGlobals
    {
        public Logger Logger { get; set; }
        public IDriver CRANDriver { get; set; }

        internal AppGlobals(Logger logger)
        {
            string filename = Path.Combine(Directory.GetCurrentDirectory(), "CRAN.xml");

            Logger = logger;
            CRANDriver = new FileSystemDriver(filename);
        }
    }
}