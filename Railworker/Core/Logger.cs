using RWLib.Interfaces;
using System;
using System.IO;

namespace Railworker.Core
{
    public class Logger : IRWLogger
    {
        public void Log(RWLogType type, string message)
        {
            string contents = $"[{type}] {message}";
            File.AppendAllText("debug.log", contents);
            System.Diagnostics.Debug.WriteLine(contents);
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
