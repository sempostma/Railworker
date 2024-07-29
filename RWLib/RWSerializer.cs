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

namespace RWLib
{
    public class RWSerializer : RWLibraryDependent
    {
        public RWSerializer(RWLibrary rWLib) : base(rWLib)
        {
        }

        public async Task<string> SerializeWithSerzExe(XDocument xml)
        {
            var hashCode = rWLib.options.Cache != null ? xml.GetFastDeepConsistentHash() : 0;
            var cacheKey = $"RWSerializer/SerializeWithSerzExe/{hashCode}";
            var cacheEntry = rWLib.options.Cache?.GetEntry(cacheKey);
            if (cacheEntry?.IsStillRelevant(1) == true)
            {
                if (File.Exists(cacheEntry.PersistentValue))
                {
                    return cacheEntry.PersistentValue;
                } else
                {
                    rWLib.options.Cache?.PurgeEntry(cacheKey);
                }
            }

            var tempPath = Path.GetTempPath();
            var serzTempDir = Path.Combine(tempPath, "RWLib", "SerzTemp");
            Directory.CreateDirectory(serzTempDir); // ensure directory
            String tempFile;

            var cancellationToken = new CancellationToken();
            
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.OmitXmlDeclaration = false;
            settings.Encoding = new UTF8Encoding(false);
            settings.NewLineHandling = NewLineHandling.None;
            settings.Async = true;

            tempFile = Path.Combine(serzTempDir, Convert.ToString(Random.Shared.Next(), 16) + ".xml");
            using (var writeStream = File.OpenWrite(tempFile))
            {
                using (XmlWriter writer = XmlWriter.Create(writeStream, settings))
                {
                    await xml.WriteToAsync(writer, cancellationToken);
                }
            }

            var exitCode = await RunProcess(tempFile);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Failed to run serz.exe for file '{tempFile}'");
            }

            var binFile = Path.ChangeExtension(tempFile, "bin");

            var bytes = File.ReadAllBytes(binFile);

            var emptyFileBytes = new byte[] { 0x53, 0x45, 0x52, 0x5A, 0x00, 0x00, 0x01, 0x00 };

            if (bytes.Length == emptyFileBytes.Length)
            {
                bool matches = true;
                for (int i = 0; i < emptyFileBytes.Length; i++)
                {
                    matches = matches && emptyFileBytes[i] == bytes[i];
                }
                if (matches)
                {
                    throw new InvalidDataException("This is not a valid Railworks XML file because serz.exe failed to produce any output. Make sure the format of the file is correct and you did not change the structure of the file. You can find the xml output here: " + tempFile);
                }
            }

            if (rWLib.options.Cache != null)
            {
                rWLib.options.Cache.StoreCacheEntry(cacheKey, 1, binFile);
            }

            return binFile;
        }

        public async Task<XDocument> Deserialize(Stream stream)
        {
            var binToObj = new BinToObj(stream);
            var objToXml = new ObjToXml();
            try
            {
                await foreach (var node in binToObj.Run())
                {
                    objToXml.Push(node);
                }
            } catch(Exception ex)
            {
                rWLib.options.Logger.Log(Interfaces.RWLogType.Error, "Partial Xml result: " + objToXml.Finish().ToString());
                throw ex;
            }
            return objToXml.Finish();
        }

        public async Task<XDocument> Deserialize(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return await Deserialize(stream);
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

            var xmlFile = Path.ChangeExtension(tempFile, "Xml");

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

        private static void ComputeHash(XElement element, ref int hash)
        {
            if (element == null)
            {
                return;
            }

            // Combine hash with element name
            hash = hash * 31 + element.Name.GetHashCode();

            // Combine hash with attributes
            foreach (var attr in element.Attributes().OrderBy(a => a.Name.ToString()))
            {
                hash = hash * 31 + attr.Name.GetHashCode();
                hash = hash * 31 + attr.Value.GetHashCode();
            }

            // Recursively combine hash with child elements
            foreach (var node in element.Nodes())
            {
                if (node is XElement e)
                {
                    ComputeHash(e, ref hash);
                }
                else if (node is XText t)
                {
                    hash = hash * 31 + t.Value.GetHashCode();
                }
            }
        }
    }
}

