using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;

namespace RWLib
{
    public class RWSerializer : RWLibraryDependent
    {
        internal RWSerializer(RWLibrary rWLib) : base(rWLib)
        {
        }

        private String RandomFileName()
        {
            var tempPath = Path.GetTempPath();
            var serzTempDir = Path.Combine(tempPath, "RWLib", "SerzTemp");
            Directory.CreateDirectory(serzTempDir); // ensure directory
            return Path.Combine(serzTempDir, Convert.ToString(Random.Shared.Next(), 16) + ".bin");
        }

        public String ExtractToTemporaryFile(ZipArchiveEntry entry)
        {
            var randomFilename = RandomFileName();
            entry.ExtractToFile(randomFilename);
            return randomFilename;
        }

        public async Task<XDocument> Deserialize(string filename)
        {
            var tempPath = Path.GetTempPath();
            var serzTempDir = Path.Combine(tempPath, "RWLib", "SerzTemp");
            Directory.CreateDirectory(serzTempDir); // ensure directory
            String tempFile;

            if (filename.StartsWith(tempPath)) {
                tempFile = filename;
            } 
            else
            {
                tempFile = Path.Combine(serzTempDir, Convert.ToString(Random.Shared.Next(), 16) + ".bin");
                await RWUtils.CopyFileAsync(filename, tempFile);
            }

            var exitCode = await RunProcess(tempFile);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Failed to run serz.exe for file '{tempFile}'");
            }

            var xmlFile = Path.ChangeExtension(tempFile, "xml");

            return await LoadXMLSafe(xmlFile);
        }

        private Task<int> RunProcess(string filename)
        {
            var processInfo = new ProcessStartInfo();
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.UseShellExecute = false;

            processInfo.FileName = Path.Combine(rWLib.options.TSPath, "serz.exe");
            processInfo.Arguments = '"' + filename + '"';

            var process = new Process();
            process.StartInfo = processInfo;
            process.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            return tcs.Task;
        }
    }
}