using RWLib;
using System.Diagnostics;

namespace UnitTests
{
    internal class UnitTestLogger : IRWLogger
    {
        public void Log(RWLogType type, string message)
        {
            Debug.WriteLine($"[{type.ToString()}] ${message}");
        }
    }
}