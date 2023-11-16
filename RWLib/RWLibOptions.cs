using System.Diagnostics;

namespace RWLib
{
    public class RWLibOptions
    {
        public string TSPath { get; }
        public string SerzExePath { get; }
        public IRWLogger Logger { get; }

        public RWLibOptions()
        {
            Logger = new DefaultLogger();
            TSPath = RWUtils.GetTSPathFromSteamAppInRegistry();
            SerzExePath = Path.Combine(TSPath, "serz.exe");
        }

        public RWLibOptions(IRWLogger logger)
        {
            Logger = logger;
            TSPath = RWUtils.GetTSPathFromSteamAppInRegistry();
            SerzExePath = Path.Combine(TSPath, "serz.exe");
        }

        public RWLibOptions(IRWLogger logger, string tsPath)
        {
            Logger = logger;
            TSPath = tsPath;
            SerzExePath = Path.Combine(tsPath, "serz.exe");
        }

        public RWLibOptions(IRWLogger logger, string tsPath, string serzExePath)
        {
            Logger = logger;
            TSPath = tsPath;
            SerzExePath = serzExePath;
        }

        public RWLibOptions(string tsPath)
        {
            Logger = new DefaultLogger();
            TSPath = tsPath;
            SerzExePath = Path.Combine(tsPath, "serz.exe");
        }

        public RWLibOptions(string tsPath, string serzExePath)
        {
            Logger = new DefaultLogger();
            TSPath = tsPath;
            SerzExePath = serzExePath;
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