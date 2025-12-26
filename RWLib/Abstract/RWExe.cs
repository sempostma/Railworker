using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;
using RWLib.SerzClone;
using System.IO;
using System;
using RWLib.Extensions;

namespace RWLib.Abstract
{
    public class RWExe : RWLibraryDependent
    {
        public RWExe(RWLibrary rWLib) : base(rWLib)
        {
        }

        protected Task<int> RunProcess(string exePath, string filename)
        {
            return RunProcess(exePath, new []{ '"' + filename + '"' });
        }

        protected Task<int> RunProcess(string exePath, IEnumerable<string> arguments)
        {
            var processInfo = new ProcessStartInfo();
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            processInfo.FileName = exePath;
            processInfo.Arguments = String.Join(" ", arguments);
            processInfo.WorkingDirectory = rWLib.TSPath;

            var process = new Process();
            process.StartInfo = processInfo;
            process.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                if (process.ExitCode != 0)
                {
                    Console.WriteLine(processInfo.FileName + " " + processInfo.Arguments);
                    Console.WriteLine("Error: " + process.StandardError.ReadToEnd());
                }
                process.Dispose();
            };

            process.Start();
            return tcs.Task;
        }

        protected string RandomFileName(string extensionWithDot)
        {
            var tempPath = Path.GetTempPath();
            var serzTempDir = Path.Combine(tempPath, "RWLib", "RWExe");
            Directory.CreateDirectory(serzTempDir); // ensure directory
            return Path.Combine(serzTempDir, Convert.ToString(Random.Shared.Next(), 16) + extensionWithDot);
        }
    }
}

