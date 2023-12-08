using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;
using RWLib.SerzClone;
using System.IO;

namespace RWLib
{
    public class RWSerializer : RWLibraryDependent
    {
        public RWSerializer(RWLibrary rWLib) : base(rWLib)
        {
        }

        public async Task<XDocument> Deserialize(Stream stream)
        {
            var binToObj = new BinToObj(stream);
            var objToXml = new ObjToXml();
            await foreach (var node in binToObj.Run())
            {
                objToXml.Push(node);
            }
            return objToXml.Finish();
        }

        public Task<XDocument> Deserialize(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return Deserialize(stream);
            }
        }

        public async Task<XDocument> DeserializeWithSerzExe(string filename)
        {
            var tempPath = Path.GetTempPath();
            var serzTempDir = Path.Combine(tempPath, "RWLib", "SerzTemp");
            Directory.CreateDirectory(serzTempDir); // ensure directory
            String tempFile;

            if (filename.StartsWith(tempPath))
            {
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
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.UseShellExecute = false;

            processInfo.FileName = rWLib.options.SerzExePath;
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
    }
}

