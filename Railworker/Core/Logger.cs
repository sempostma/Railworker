using RWLib.Interfaces;
using System;
using System.IO;
using System.Threading;

namespace Railworker.Core
{
    public class Logger : IRWLogger
    {
        private const int NumberOfRetries = 100;
        private const int DelayOnRetry = 50;

        public void Log(RWLogType type, string message)
        {
            string contents = $"[{type}] {message}";
            System.Diagnostics.Debug.WriteLine(contents);

            for (int i=1; i <= NumberOfRetries; ++i) {
                try {
                    File.AppendAllText("debug.log", contents);
                    break; // When done we can break loop
                }
                catch (IOException e) when(i <= NumberOfRetries)
                {
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    Thread.Sleep(DelayOnRetry);
                }
            }
        }

        public void Debug(string message)
        {
            Log(RWLogType.Debug, message);
        }
        public void Warning(string message)
        {
            Log(RWLogType.Warning, message);
        }
        public void Error(string message)
        {
            Log(RWLogType.Error, message);
        }
        public void Error(Exception ex)
        {
            if (ex.StackTrace != null)
            {
                Log(RWLogType.Error, ex.Message + " [stack trace: " + ex.StackTrace + "]");
            }
            else
            {
                Log(RWLogType.Error, ex.Message);
            }
        }
    }
}
