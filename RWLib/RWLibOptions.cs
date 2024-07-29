using System.Diagnostics;
using RWLib.Interfaces;

namespace RWLib
{
    public class RWLibOptions
    {
        public string TSPath { set; get; } = RWUtils.GetTSPathFromSteamAppInRegistry();
        public string SerzExePath { set; get; }
        public IRWLogger Logger { set; get; } = new DefaultLogger();
        public bool UseCustomSerz { set; get; } = false;
        public RWCachingSystem? Cache { set;get; } = null;

        public RWLibOptions()
        {
            SerzExePath = Path.Combine(TSPath, "serz.exe");
        }

        private class DefaultLogger : IRWLogger
        {
            public void Log(RWLogType type, string message)
            {
                // do nothing
            }
        }
    }
}